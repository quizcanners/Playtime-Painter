using QuizCanners.Inspect;
using QuizCanners.Migration;
using QuizCanners.Utils;
using UnityEngine;

namespace PainterTool.MeshEditing
{
    public class VertexUVTool : MeshToolBase, IMeshToolWithPerMeshData
    {

        public override string StdTag => "t_uv";
        
        public static VertexUVTool inst;

        public bool projectionUv;
        public Vector2 tiling = Vector2.one;

        private bool projectFront;
        private bool preciseOffset = true;
        public Vector2 offset;
        public float projectorNormalThreshold01 = 0.5f;
        private bool meshProcessors;

        #region Encode & Decode
        public override void DecodeTag(string key, CfgData data)
        {
            switch (key)
            {
                case "gtuv": projectionUv = data.ToBool(); break;
                case "offset": offset = data.ToVector2(); break;
                case "tile": tiling = data.ToVector2(); break;
                case "nrmWrap": projectorNormalThreshold01 = data.ToFloat();  break;
                case "fr": projectFront = true; break;
                case "mp": meshProcessors = true; break;
                case "po": preciseOffset = data.ToBool(); break;
            }
        }

        public override CfgEncoder Encode() => new CfgEncoder()
            .Add_Bool("gtuv", projectionUv)
            .Add("nrmWrap", projectorNormalThreshold01)
            .Add_IfTrue("fr", projectFront)
            .Add_IfTrue("mp", meshProcessors)
            .Add_Bool("po", preciseOffset);
        
        public CfgEncoder EncodePerMeshData() => new CfgEncoder()
            .Add("offset", offset)
            .Add("tile", tiling);

        #endregion

        #region Inspect
        public override string ToString() => "vertex UV";

        public override string Tooltip =>"When Starting UV Projection, entire mesh will show a preview. UVs still need to be applied manually by painting them. " +
                                         "Alternativelly it is possible to use Auto Apply Treshold to map to all using Normal Treshold. " +
                                         "It is recommended to Set all vertices Unique (with Add & Move tool) before applying UVs. And after, merge them if UVs are same." +
                                         "You project UV and adjust by dragging individual vertices (UVs will change, not the position)."  ;
        
       public override void Inspect() 
        {
            var mm = Painter.MeshManager;

            if (("Edited UV: " + mm.EditedUV + " (Click to switch)").PegiLabel().Click().Nl())
                mm.EditedUV = mm.EditedUV == 1 ? 0 : 1; // 1 - mm.editedUV;

            if (!projectionUv && "Projection UV Start".PegiLabel().Click().Nl()) {
                projectionUv = true;
                UpdateUvPreview();
            }

            if (projectionUv) 
            {
                if ("tiling".PegiLabel().Edit(ref tiling).Nl())
                    UpdateUvPreview();

                (preciseOffset ? Icon.Round : Icon.Size).Click(()=> preciseOffset = !preciseOffset);

                if (preciseOffset)
                    "offset".PegiLabel(60).Edit(ref offset, -1, 1).Nl().OnChanged(()=> UpdateUvPreview());
                else
                    "offset".PegiLabel(60).Edit(ref offset).Nl().OnChanged(() => UpdateUvPreview());

                if ("Projection UV Stop".PegiLabel().Click().Nl()) {
                    projectionUv = false;
                    EditedMesh.Dirty = true;
                }

                ("Paint on vertices where you want to apply current UV configuration. {0} " +
                    
                    "Use scroll wheel or XZ to change projection plane").F(pegi.EnvironmentNl).PegiLabel().Write_Hint();
            }

            if ("Processors".PegiLabel().IsFoldout(ref meshProcessors).Nl())  {

                if (projectionUv) {

                    if ("Auto - Project All sides".PegiLabel(toolTip: "This will change UV of the entire mesh").ClickConfirm(confirmationTag: "Auto Project").Nl())
                    {
                        projectorNormalThreshold01 = 0.5f;

                        for (int plane = 0; plane<3; plane++) 
                        {
                            MeshPainting.CurrentPlane = (GridPlane)plane;
                            projectFront = true;
                            UpdateUvPreview(true);
                            AutoProjectUVs(EditedMesh);
                            UpdateUvPreview(true);
                            projectFront = false;
                            AutoProjectUVs(EditedMesh);
                        }

                        OnDeSelectTool();
                    }

                    if ("Auto Apply Threshold".PegiLabel().Edit(ref projectorNormalThreshold01, 0.01f, 1f))
                        UpdateUvPreview(true);

                    if ((projectFront ? "front" : "back").PegiLabel().Click())
                        projectFront = !projectFront;

                    if (Icon.Done.Click("Auto apply to all"))
                        AutoProjectUVs(EditedMesh);

                    pegi.Nl();

                    "To Auto-Generate LightmapUV(UV2), exit mesh editing (disable/enable component) and click GenerateUV2 before entering/insteadOf editing".PegiLabel().Write_Hint().Nl();
                }

                pegi.Nl();

                if ("All UVs to 01 space".PegiLabel().Click())
                {
                    foreach (var point in EditedMesh.meshPoints)
                        point.UVto01Space();

                    EditedMesh.Dirty = true;
                }

                pegi.Nl();
            }


        }
     
        #endregion

        private void UpdateUvPreview(bool useThreshold = false)
        {
            if (!projectionUv) return;

           // var m = MeshMGMT;

            if (!MeshPainting.target || EditedMesh.meshPoints.IsNullOrEmpty()) return;

            var prMesh = GetPreviewMesh;

            var trgPos = MeshPainting.targetTransform.position;
            
              if (!useThreshold) {
                  foreach (var v in prMesh.meshPoints)  {
                      var pUv = PosToUv((v.WorldPos - trgPos));
                      foreach (var uv in v.vertices)
                          uv.SharedEditedUv = pUv;
                  }
              }
              else
                  AutoProjectUVs(prMesh);

              var trg = MeshPainting.target;

            new MeshConstructor(prMesh, trg.MeshProfile, trg.SharedMesh).UpdateMesh<VertexDataTypes.VertexUv>();
        }

        public Vector2 PosToUv(Vector3 diff)
        {

            var uv = new Vector2();

            switch (MeshPainting.CurrentPlane)
            {
                case GridPlane.xy:
                    uv.x = diff.x;
                    uv.y = diff.y;
                    break;
                case GridPlane.xz:
                    uv.x = diff.x;
                    uv.y = diff.z;
                    break;
                case GridPlane.zy:
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

            var trgPos = MeshPainting.targetTransform.position;

            var diffs = new Vector3[3];

            for (var i=0; i<3; i++)
                diffs[i] = t.vertexes[0].meshPoint.WorldPos - trgPos;

            return uv;
        }
        
        public override void OnSelectTool() => UpdateUvPreview();

        public override void OnDeSelectTool()
        {

            foreach (var v in EditedMesh.meshPoints)
                v.CleanEmptyIndexes();

            if (projectionUv)
                Painter.MeshManager.Redraw();
        }

        public override void OnGridChange() => UpdateUvPreview();

        public override Color VertexColor => Color.magenta;

        public override bool ShowGrid => projectionUv;

        public override bool ShowVertices => !projectionUv && !Painter.MeshManager.Dragging;

        public override bool ShowLines => false;

        public override void AssignText(MarkerWithText markers, PainterMesh.MeshPoint point)
        {
            var vrt = Painter.MeshManager.GetSelectedVertex();

            if (point.vertices.Count > 1 || vrt == point)
            {

                var tex = MeshPainting.target.meshRenderer.sharedMaterial.mainTexture;

                if (vrt == point)
                {
                    var text = (point.vertices.Count > 1) ? ((point.vertices.IndexOf(MeshEditorManager.SelectedUv) + 1) + "/" + point.vertices.Count + (point.smoothNormal ? "s" : "")) : "";
                    float tSize = !tex ? 128 : tex.width;
                    text += ("uv: " + (MeshEditorManager.SelectedUv.EditedUv.x * tSize) + "," + (MeshEditorManager.SelectedUv.EditedUv.y * tSize));
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

            if (PlaytimePainter_EditorInputManager.GetMouseButtonDown(0))
            {
                Painter.MeshManager.AssignSelected(PointedUv); //pointedUV.editedUV = meshMGMT.selectedUV.editedUV;
                _lastCalculatedUv = PointedUv.EditedUv;
                Painter.MeshManager.Dragging = true;
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

            var a = PointedLine.vertexes[0];
            var b = PointedLine.vertexes[1];

            Painter.MeshManager.AssignSelected(
                Vector3.Distance(MeshPainting.collisionPosLocal, a.LocalPos) <
                Vector3.Distance(MeshPainting.collisionPosLocal, b.LocalPos)
                    ? EditedMesh.GetUvPointAFromLine(a.meshPoint, b.meshPoint)
                    : EditedMesh.GetUvPointAFromLine(b.meshPoint, a.meshPoint));

            return false;

        }

        public override bool MouseEventPointedTriangle() 
        {
            if (!PlaytimePainter_EditorInputManager.GetMouseButton(0))
                return false;

            if (!projectionUv) 
                return false;
            
            if (PointedTriangle.SameAsLastFrame)
                return true;

            MeshEditorManager.SelectedUv ??= EditedMesh.meshPoints[0].vertices[0];

            if (!PlaytimePainter_EditorInputManager.Control) 
            {
                var trgPos = MeshPainting.targetTransform.position;

                for (var i = 0; i < 3; i++) 
                {
                    var v = PointedTriangle.vertexes[i];
                    var uv = PosToUv(v.meshPoint.WorldPos - trgPos);
                    var uv0 = Painter.MeshManager.EditedUV == 0 ? uv : v.GetUvSet(0);
                    var uv1 = Painter.MeshManager.EditedUV == 1 ? uv : v.GetUvSet(1);

                    PointedTriangle.vertexes[i] = v.meshPoint.GetVertexForUv(uv0, uv1);
                  // EditedMesh.dirtyUvs |= v.SetUvIndexBy(PosToUv(v.meshPoint.WorldPos - trgPos));
                }

                EditedMesh.dirtyUvs = true;
            }
           

            return true;
        }

        private void AutoProjectUVs(MeshData eMesh) {

            var trgPos = MeshPainting.targetTransform.position;

            var gn = MeshPainting.Grid;

            if (Mathf.Approximately( projectorNormalThreshold01, 1)) {
                foreach (var t in eMesh.triangles) {
                    for (var i = 0; i < 3; i++) {
                        var v = t.vertexes[i];
                        v.SetUvIndexBy(PosToUv(v.meshPoint.WorldPos - trgPos));
                    }
                }
            } else 
                foreach (var t in eMesh.triangles)
                {
                    var norm = t.GetSharpNormalWorldSpace();

                    // var pv = gn.InPlaneVector(norm);

                    var perp = gn.PerpendicularToPlaneVector(norm);

                    if ((Mathf.Abs(perp) < projectorNormalThreshold01) || (perp>0 != projectFront)) 
                        continue;

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
                var posUv = PointedTriangle.LocalPosToEditedUv(MeshPainting.collisionPosLocal);
                var newUv = uv * 2 - posUv;
                var isChanged = newUv != _lastCalculatedUv;
                _lastCalculatedUv = newUv;

                var trg = MeshPainting.target;

                if (isChanged && !PlaytimePainter_EditorInputManager.GetMouseButtonUp(0))
                {
                    var prMesh = GetPreviewMesh;
                    if (prMesh.selectedUv != null)
                    {
                        prMesh.selectedUv.SharedEditedUv = _lastCalculatedUv;
                        trg.SharedMesh = new MeshConstructor(prMesh, trg.MeshProfile, trg.SharedMesh).Construct();
                    }
                }
            }

            if (PlaytimePainter_EditorInputManager.GetMouseButtonUp(0)) {

                MeshEditorManager.SelectedUv.SharedEditedUv = _lastCalculatedUv;
                EditedMesh.dirtyUvs = true;
                Debug.Log("Setting Dirty UV Test");
                Painter.MeshManager.Dragging = false;
            }


            if (!PlaytimePainter_EditorInputManager.GetMouseButton(0))
                Painter.MeshManager.Dragging = false;

        }

        public override void KeysEventPointedLine()
        {
            if ((KeyCode.Backspace.IsDown()))
            {
                var a = PointedLine.vertexes[0];
                var b = PointedLine.vertexes[1];

                if (!PlaytimePainter_EditorInputManager.Control)
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