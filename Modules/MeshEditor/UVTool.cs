using PlayerAndEditorGUI;
using QuizCannersUtilities;
using UnityEngine;

namespace PlaytimePainter.MeshEditing
{
    public class VertexUVTool : MeshToolBase, IMeshToolWithPerMeshData
    {

        public override string StdTag => "t_uv";
        
        public static VertexUVTool inst;

        public bool projectionUv;
        public Vector2 tiling = Vector2.one;

        private bool projectFront;

        public Vector2 offset;
        public float projectorNormalThreshold01 = 0.5f;
        private bool meshProcessors;

        #region Encode & Decode
        public override void Decode(string key, CfgData data)
        {
            switch (key)
            {
                case "gtuv": projectionUv = data.ToBool(); break;
                case "offset": offset = data.ToVector2(); break;
                case "tile": tiling = data.ToVector2(); break;
                case "nrmWrap": projectorNormalThreshold01 = data.ToFloat();  break;
                case "fr": projectFront = true; break;
                case "mp": meshProcessors = true; break;
            }
        }

        public override CfgEncoder Encode() => new CfgEncoder()
            .Add_Bool("gtuv", projectionUv)
            .Add("nrmWrap", projectorNormalThreshold01)
            .Add_IfTrue("fr", projectFront)
            .Add_IfTrue("mp", meshProcessors);
        
        public CfgEncoder EncodePerMeshData() => new CfgEncoder()
            .Add("offset", offset)
            .Add("tile", tiling);

        #endregion

        #region Inspect
        public override string NameForDisplayPEGI()=> "vertex UV";

        public override string Tooltip =>"When Starting UV Projection, entire mesh will show a preview. UVs still need to be applied manually by painting them. " +
                                         "Alternativelly it is possible to use Auto Apply Treshold to map to all using Normal Treshold. " +
                                         "It is recommended to Set all vertices Unique (with Add & Move tool) before applying UVs. And after, merge them if UVs are same." +
                                         "You project UV and adjust by dragging individual vertices (UVs will change, not the position)."  ;
        
       public override void Inspect() {

            var changed = false;

            var mm = MeshMGMT;

            if (("Edited UV: " + mm.EditedUV + " (Click to switch)").Click().nl())
                mm.EditedUV = mm.EditedUV == 1 ? 0 : 1; // 1 - mm.editedUV;

            if (!projectionUv && "Projection UV Start".Click().nl()) {
                projectionUv = true;
                UpdateUvPreview();
            }

            if (projectionUv) {

                if ("tiling".edit(ref tiling).nl(ref changed))
                    UpdateUvPreview();

                if ("offset".edit(ref offset, -1, 1).nl(ref changed))
                    UpdateUvPreview();

                if ("Projection UV Stop".Click().nl(ref changed)) {
                    projectionUv = false;
                    EditedMesh.Dirty = true;
                }

                //"Paint on vertices where you want to apply current UV configuration".writeHint();
                //"Use scroll wheel to change projection plane".writeHint();
            }

            if ("Processors".foldout(ref meshProcessors).nl())  {

                if (projectionUv) {
                    if ("Auto Apply Threshold".edit(ref projectorNormalThreshold01, 0.01f, 1f).changes(ref changed))
                        UpdateUvPreview(true);

                    if ((projectFront ? "front" : "back").Click())
                        projectFront = !projectFront;

                    if (icon.Done.Click("Auto apply to all"))
                        AutoProjectUVs(EditedMesh);
                }

                pegi.nl();

                if ("All UVs to 01 space".Click())
                {
                    foreach (var point in EditedMesh.meshPoints)
                        point.UVto01Space();

                    EditedMesh.Dirty = true;
                }

                pegi.nl();
            }


        }
     
        #endregion

        private void UpdateUvPreview(bool useThreshold = false)
        {
            if (!projectionUv) return;

           // var m = MeshMGMT;

            if (!MeshEditorManager.target || EditedMesh.meshPoints.IsNullOrEmpty()) return;

            var prMesh = GetPreviewMesh;

            var trgPos = MeshEditorManager.targetTransform.position;
            
              if (!useThreshold) {
                  foreach (var v in prMesh.meshPoints)  {
                      var pUv = PosToUv((v.WorldPos - trgPos));
                      foreach (var uv in v.vertices)
                          uv.SharedEditedUv = pUv;
                  }
              }
              else
                  AutoProjectUVs(prMesh);

              var trg = MeshEditorManager.target;

            new MeshConstructor(prMesh, trg.MeshProfile, trg.SharedMesh).UpdateMesh<VertexDataTypes.VertexUv>();
        }

        public Vector2 PosToUv(Vector3 diff)
        {

            var uv = new Vector2();

            switch (GridNavigator.Inst().gSide)
            {
                case Gridside.xy:
                    uv.x = diff.x;
                    uv.y = diff.y;
                    break;
                case Gridside.xz:
                    uv.x = diff.x;
                    uv.y = diff.z;
                    break;
                case Gridside.zy:
                    uv.x = diff.z;
                    uv.y = diff.y;
                    break;
            }

            uv += offset;

            uv.Scale(tiling);

            return uv;
        }
        
        public Vector2 OffsetTileFromTriangle(PainterMesh.Triangle t) {

            Vector2 uv  = Vector2.zero;

            var trgPos = MeshEditorManager.targetTransform.position;

            var diffs = new Vector3[3];

            for (var i=0; i<3; i++)
                diffs[i] = t.vertexes[0].meshPoint.WorldPos - trgPos;

          /*  switch (GridNavigator.Inst().gSide) {
                case Gridside.xy:
                    uv.x = diff.x;
                    uv.y = diff.y;
                    break;
                case Gridside.xz:
                    uv.x = diff.x;
                    uv.y = diff.z;
                    break;
                case Gridside.zy:
                    uv.x = diff.z;
                    uv.y = diff.y;
                    break;
            }

            uv = (uv + offset);

            uv.Scale(tiling);*/

            return uv;
        }
        
        public override void OnSelectTool() => UpdateUvPreview();

        public override void OnDeSelectTool()
        {

            foreach (var v in EditedMesh.meshPoints)
                v.CleanEmptyIndexes();

            if (projectionUv)
                MeshMGMT.Redraw();
        }

        public override void OnGridChange() => UpdateUvPreview();

        public override Color VertexColor => Color.magenta;

        public override bool ShowGrid => projectionUv;

        public override bool ShowVertices => !projectionUv && !MeshMGMT.Dragging;

        public override bool ShowLines => false;

        public override void AssignText(MarkerWithText markers, PainterMesh.MeshPoint point)
        {
            var vrt = MeshMGMT.GetSelectedVertex();

            if (point.vertices.Count > 1 || vrt == point)
            {

                var tex = MeshEditorManager.target.meshRenderer.sharedMaterial.mainTexture;

                if (vrt == point)
                {
                    var text = (point.vertices.Count > 1) ? ((point.vertices.IndexOf(MeshMGMT.SelectedUv) + 1) + "/" + point.vertices.Count + (point.smoothNormal ? "s" : "")) : "";
                    float tSize = !tex ? 128 : tex.width;
                    text += ("uv: " + (MeshMGMT.SelectedUv.EditedUv.x * tSize) + "," + (MeshMGMT.SelectedUv.EditedUv.y * tSize));
                    markers.textm.text = text;
                }
                else
                    markers.textm.text = point.vertices.Count +
                        (point.smoothNormal ? "s" : "");
            }
            else markers.textm.text = "";
        }

        public override bool MouseEventPointedVertex()
        {

            if (EditorInputManager.GetMouseButtonDown(0))
            {
                MeshMGMT.AssignSelected(PointedUv); //pointedUV.editedUV = meshMGMT.selectedUV.editedUV;
                _lastCalculatedUv = PointedUv.EditedUv;
                MeshMGMT.Dragging = true;
            }

            /*
            if (EditorInputManager.GetMouseButtonDown(0))
            {
                if ((meshMGMT.selectedUV != null) && (meshMGMT.pointedUV != null))
                {
                    meshMGMT.pointedUV.editedUV = meshMGMT.selectedUV.editedUV;
                    mesh.dirty = true;
                }
            }

            if ((EditorInputManager.GetMouseButtonDown(1)) && (meshMGMT.pointedUV != null) && (UVnavigator.inst() != null))
                UVnavigator.inst().CenterOnUV(meshMGMT.pointedUV.editedUV);
                */
            /*
        if (ProjectionUV && EditorInputManager.GetMouseButton(0)) {
            Vector3 trgPos = meshMGMT.target.transform.position;
            float portion = 1f / Mathf.Max(0.01f, MeshUVprojectionSize);

            Vector2 nuv = PosToUV((vertex.worldPos - trgPos) * portion);

            for (int i = 0; i < vertex.shared_v2s.Count; i++)
                vertex.shared_v2s[i][meshMGMT.editedUV] = nuv;
        }
        */
            return false;
        }

        public override bool MouseEventPointedLine()
        {

            var a = PointedLine.points[0];
            var b = PointedLine.points[1];

            MeshMGMT.AssignSelected(
                Vector3.Distance(MeshMGMT.collisionPosLocal, a.LocalPos) <
                Vector3.Distance(MeshMGMT.collisionPosLocal, b.LocalPos)
                    ? EditedMesh.GetUvPointAFromLine(a.meshPoint, b.meshPoint)
                    : EditedMesh.GetUvPointAFromLine(b.meshPoint, a.meshPoint));

            return false;

        }

        public override bool MouseEventPointedTriangle() {
            if (!EditorInputManager.GetMouseButton(0))
                return false;

            if (!projectionUv) return false;
            
            if (PointedTriangle.SameAsLastFrame)
                return true;

            if (MeshMGMT.SelectedUv == null)
                MeshMGMT.SelectedUv = EditedMesh.meshPoints[0].vertices[0];

            if (!EditorInputManager.Control) {
                var trgPos = MeshEditorManager.targetTransform.position;

                for (var i = 0; i < 3; i++) {
                    var v = PointedTriangle.vertexes[i];
                    EditedMesh.dirtyUvs |= v.SetUvIndexBy(PosToUv(v.meshPoint.WorldPos - trgPos));
                }
            }
           

            return true;
        }

        private void AutoProjectUVs(EditableMesh eMesh) {

            var trgPos = MeshEditorManager.targetTransform.position;

            var gn = GridNavigator.Inst();

            if (projectorNormalThreshold01 == 1) {
                foreach (var t in eMesh.triangles) {
                    for (var i = 0; i < 3; i++) {
                        var v = t.vertexes[i];
                        v.SetUvIndexBy(PosToUv(v.meshPoint.WorldPos - trgPos));
                    }
                }
            } else 
            foreach (var t in eMesh.triangles)
            {
                var norm = t.GetSharpNormal();

               // var pv = gn.InPlaneVector(norm);

                var perp = gn.PerpendicularToPlaneVector(norm);

                if ((Mathf.Abs(perp) < projectorNormalThreshold01) || (perp>0 != projectFront)) continue;

                for (var i = 0; i < 3; i++) {
                    var v = t.vertexes[i];

                    v.SetUvIndexBy(PosToUv(v.meshPoint.WorldPos - trgPos));
                }
            }

            eMesh.Dirty = true;
        }

        private Vector2 _lastCalculatedUv;

        public override void ManageDragging()
        {

            if (PointedTriangle != null && SelectedUv != null) {

                var uv = SelectedUv.SharedEditedUv;
                var posUv = PointedTriangle.LocalPosToEditedUv(MeshMGMT.collisionPosLocal);
                var newUv = uv * 2 - posUv;
                var isChanged = newUv != _lastCalculatedUv;
                _lastCalculatedUv = newUv;

                var trg = MeshEditorManager.target;

                if (isChanged && !EditorInputManager.GetMouseButtonUp(0))
                {
                    var prMesh = GetPreviewMesh;
                    if (prMesh.selectedUv != null)
                    {
                        prMesh.selectedUv.SharedEditedUv = _lastCalculatedUv;
                        trg.SharedMesh = new MeshConstructor(prMesh, trg.MeshProfile, trg.SharedMesh).Construct();
                    }
                }
            }

            if (EditorInputManager.GetMouseButtonUp(0)) {

                MeshMGMT.SelectedUv.SharedEditedUv = _lastCalculatedUv;
                EditedMesh.dirtyUvs = true;
                Debug.Log("Setting Dirty UV Test");
                MeshMGMT.Dragging = false;
            }


            if (!EditorInputManager.GetMouseButton(0))
                MeshMGMT.Dragging = false;

        }

        public override void KeysEventPointedLine()
        {
            if ((KeyCode.Backspace.IsDown()))
            {
                var a = PointedLine.points[0];
                var b = PointedLine.points[1];

                if (!EditorInputManager.Control)
                    EditedMesh.SwapLine(a.meshPoint, b.meshPoint);
                else
                    EditedMesh.DeleteLine(PointedLine);

                EditedMesh.Dirty = true;
            }
        }

        public VertexUVTool()
        {
            inst = this;
        }


    }


    /*
    [ExecuteInEditMode]
    public class UVnavigator : PainterSystemMono {

        public static UVnavigator Inst()
        {
            if (!_inst)
                _inst = FindObjectOfType<UVnavigator>();

            return _inst;
        }
        public static UVnavigator _inst;

        public Renderer rend;

        public Vector2 prevOnTex;
        public Vector2 currentVec2;

        float Zoom = 1;
        Vector2 MouseDwnOffset = new Vector2(0.5f, 0.5f);
        Vector2 MouseDwnScreenPos = new Vector2();
        float MouseDwnZoom;
        bool MMouseDwn = false;
        bool RMouseDwn = false;
     
        Vector2 textureOffset = new Vector2();

        void Update()
        {
            if (MMouseDwn)
            {
                if (!Input.GetMouseButton(2)) MMouseDwn = false;
                Zoom = Mathf.Max(0.1f, MouseDwnZoom + (Input.mousePosition.x - MouseDwnScreenPos.x) * 8 / Screen.width);

                MouseDwnOffset = QcMath.LerpBySpeed(MouseDwnOffset, draggedOffset, 2 * Zoom);
            }

            float Off = -Zoom / 2;
            Vector2 resultingOffset = new Vector2(Off, Off);

            resultingOffset += MouseDwnOffset;

            textureOffset = resultingOffset;

            rend.material.SetTextureScale("_MainTex", new Vector2(Zoom, Zoom));
            rend.material.SetTextureOffset("_MainTex", textureOffset);
        }

        Vector2 draggedOffset;

        void ZoomingAndSelection()
        {

            Zoom = Mathf.Max(0.1f, Zoom - (Input.GetAxis("Mouse ScrollWheel")) * Zoom);

            if ((Input.GetMouseButtonDown(2)) || (Input.GetMouseButtonDown(1)))
            {

                draggedOffset = GetHitUV();

                if (Input.GetMouseButtonDown(2))
                {
                    MMouseDwn = true;
                    MouseDwnScreenPos = Input.mousePosition;
                    MouseDwnZoom = Zoom;
                    // draggedOffset = hit.textureCoord * Zoom + textureOffset;
                }
                else
                {
                    RMouseDwn = true;
                    //draggedOffset = hit.textureCoord * Zoom + textureOffset;
                }
                //MouseDwnOffset.x = glob.MyLerp(MouseDwnOffset.x, targetOffset.x, 1);
                //MouseDwnOffset.y = glob.MyLerp(MouseDwnOffset.y, targetOffset.y, 1);


            }
        }

        public void UpdateSamplerMaterial(Vector2 v2)
        {
            if (rend.material.mainTexture != null)
                rend.material.SetVector("_point", new Vector4(v2.x, v2.y, Zoom * 0.01f, rend.material.mainTexture.width));
        }

        public void CenterOnUV(Vector2 v2)
        {
            MouseDwnOffset = v2;//Vector2 nuv = hit.textureCoord * Zoom + textureOffset;
                                //  Vector2 v2 = MeshManager.inst.selected.v2;
                                // Debug.Log("Writing sampler " + rend.material.mainTexture.width);
            UpdateSamplerMaterial(v2);
        }

        Vector2 GetHitUV() {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
                return hit.textureCoord * Zoom + textureOffset;
            return Vector2.zero;
        }

        public bool MouseOverThisTurn = false;

        void OnMouseOver()
        {

            MouseOverThisTurn = true;

            if (RMouseDwn)
            {
                if (!Input.GetMouseButton(1)) RMouseDwn = false;
                else
                {

                    Vector2 tmp = GetHitUV();
                    MouseDwnOffset.x -= (tmp.x - draggedOffset.x) / 2;
                    MouseDwnOffset.y -= (tmp.y - draggedOffset.y) / 2;

                }
            }


#if UNITY_EDITOR
            ZoomingAndSelection();
            //MeshUVediting();

            Vector2 v2 = MeshManager.Inst.SelectedUV.EditedUV;
            UpdateSamplerMaterial(v2);
#endif
        }

        void Awake()
        {
            _inst = this;
        }

        void Start()
        {
            UpdateSamplerMaterial(Vector2.zero);

        }

    }
    */
    
}