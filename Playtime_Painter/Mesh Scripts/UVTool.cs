using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace Playtime_Painter
{

    public class VertexUVTool : MeshToolBase
    {

        public static VertexUVTool inst;

        public bool projectionUv;
        public Vector2 tiling = Vector2.one;
        public Vector2 offset;


        #region Encode & Decode
        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "gtuv": projectionUv = data.ToBool(); break;
                case "offset": offset = data.ToVector2(); break;
                case "tile": tiling = data.ToVector2(); break;
                default: return false;
            }
            return true;
        }

        public override StdEncoder Encode()
        {
            var cody = new StdEncoder();
            cody.Add_Bool("gtuv", projectionUv);
            cody.Add("offset", offset);
            cody.Add("tile", tiling);
            return cody;
        }
        #endregion

        #region Inspect
        public override string NameForDisplayPEGI => "vertex UV";

        public override string Tooltip => projectionUv ? "After setting scale and offset, paint this UVs on triengles. Use scroll wheel to change the direction a projection is facing." : "";

        #if PEGI
        public override bool Inspect() {

            bool changed = false;

            MeshManager mm = MeshMGMT;

            if (("UV Set: " + mm.EditedUV + " (Click to switch)").Click().nl())
                mm.EditedUV = mm.EditedUV == 1 ? 0 : 1; // 1 - mm.editedUV;

            if (!projectionUv && "Projection UV Start".Click().nl())
            {
                projectionUv = true;
                UpdatePreview();
            }

            if (projectionUv) {

                if ("tiling".edit(ref tiling).nl(ref changed))
                    UpdatePreview();

                if ("offset".edit(ref offset).nl(ref changed))
                    UpdatePreview();

                if ("Projection UV Stop".Click().nl(ref changed)) {
                    projectionUv = false;
                    EditedMesh.Dirty = true;
                }

                "Paint on vertices where you want to apply current UV configuration".writeHint();
                "Use scroll wheel to change projection plane".writeHint();
            }

            return changed;

        }
        #endif
        #endregion

        private void UpdatePreview()
        {
            if (!projectionUv) return;

            var m = MeshMGMT;

            if (!m.target || (m.editedMesh.meshPoints == null) || (m.editedMesh.meshPoints.Count < 1)) return;

            var prMesh = FreshPreviewMesh;

            var trgPos = m.target.transform.position;

            foreach (var v in prMesh.meshPoints)
            {
                var pUv = PosToUv((v.WorldPos - trgPos));
                foreach (var uv in v.vertices)
                    uv.SharedEditedUV = pUv;
            }

            m.target.SharedMesh = new MeshConstructor(prMesh, m.target.MeshProfile, m.target.SharedMesh).Construct();
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

            uv = (uv + offset);

            uv.Scale(tiling);

            return uv;
        }

        public override void OnSelectTool() => UpdatePreview();

        public override void OnDeSelectTool()
        {

            foreach (var v in EditedMesh.meshPoints)
                v.CleanEmptyIndexes();

            if (projectionUv)
                MeshMGMT.Redraw();
        }

        public override void OnGridChange() => UpdatePreview();

        public override Color VertexColor => Color.magenta;

        public override bool ShowGrid => projectionUv;

        public override bool ShowVertices => !projectionUv && !MeshMGMT.Dragging;

        public override bool ShowLines => false;

        public override void AssignText(MarkerWithText markers, MeshPoint point)
        {
            var vrt = MeshMGMT.GetSelectedVertex();

            if (point.vertices.Count > 1 || vrt == point)
            {

                var tex = MeshMGMT.target.meshRenderer.sharedMaterial.mainTexture;

                if (vrt == point)
                {
                    var text = (point.vertices.Count > 1) ? ((point.vertices.IndexOf(MeshMGMT.SelectedUV) + 1) + "/" + point.vertices.Count + (point.smoothNormal ? "s" : "")) : "";
                    float tSize = !tex ? 128 : tex.width;
                    text += ("uv: " + (MeshMGMT.SelectedUV.EditedUV.x * tSize) + "," + (MeshMGMT.SelectedUV.EditedUV.y * tSize));
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
                _lastCalculatedUv = PointedUv.EditedUV;
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

            var a = PointedLine.pnts[0];
            var b = PointedLine.pnts[1];

            MeshMGMT.AssignSelected(
                Vector3.Distance(MeshMGMT.collisionPosLocal, a.Pos) <
                Vector3.Distance(MeshMGMT.collisionPosLocal, b.Pos)
                    ? EditedMesh.GetUvPointAFromLine(a.meshPoint, b.meshPoint)
                    : EditedMesh.GetUvPointAFromLine(b.meshPoint, a.meshPoint));

            return false;

        }

        public override bool MouseEventPointedTriangle()
        {
            if (!EditorInputManager.GetMouseButton(0)) return false;

            if (!projectionUv) return false;
            
            if (PointedTris.SameAsLastFrame)
                return true;

            if (MeshMGMT.SelectedUV == null) MeshMGMT.SelectedUV = EditedMesh.meshPoints[0].vertices[0];

            var trgPos = MeshMGMT.target.transform.position;
        
            for (var i = 0; i < 3; i++)
                PointedTris.vertexes[i].EditedUV = PosToUv(PointedTris.vertexes[i].meshPoint.WorldPos - trgPos);

            EditedMesh.Dirty = true;

            return true;
        }

        private Vector2 _lastCalculatedUv;

        public override void ManageDragging()
        {

            if (PointedTris != null && SelectedUv != null)
            {
                var uv = SelectedUv.SharedEditedUV;
                var posUv = PointedTris.LocalPosToEditedUV(MeshMGMT.collisionPosLocal);
                var newUv = uv * 2 - posUv;
                var isChanged = newUv != _lastCalculatedUv;
                _lastCalculatedUv = newUv;

                if (isChanged && !EditorInputManager.GetMouseButtonUp(0))
                {
                    var prMesh = FreshPreviewMesh;
                    if (prMesh.selectedUv != null)
                    {
                        prMesh.selectedUv.SharedEditedUV = _lastCalculatedUv;
                        MeshMGMT.target.SharedMesh = new MeshConstructor(prMesh, MeshMGMT.target.MeshProfile, MeshMGMT.target.SharedMesh).Construct();
                    }
                }
            }

            if (EditorInputManager.GetMouseButtonUp(0))
            {

                MeshMGMT.SelectedUV.SharedEditedUV = _lastCalculatedUv;
                EditedMesh.Dirty = true;
                MeshMGMT.Dragging = false;
            }


            if (!EditorInputManager.GetMouseButton(0))
                MeshMGMT.Dragging = false;

        }

        public override void KeysEventPointedLine()
        {
            if ((KeyCode.Backspace.IsDown()))
            {
                var a = PointedLine.pnts[0];
                var b = PointedLine.pnts[1];

                if (!EditorInputManager.Control)
                    MeshMGMT.SwapLine(a.meshPoint, b.meshPoint);
                else
                    MeshMGMT.DeleteLine(PointedLine);

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
    public class UVnavigator : PainterStuffMono {

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

                MouseDwnOffset = MyMath.Lerp_bySpeed(MouseDwnOffset, draggedOffset, 2 * Zoom);
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