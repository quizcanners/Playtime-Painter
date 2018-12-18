using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace Playtime_Painter
{

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

    public class VertexUVTool : MeshToolBase
    {

        public static VertexUVTool _inst;

        public bool ProjectionUV = false;
        public Vector2 tiling = Vector2.one;
        public Vector2 offset;

        #region Encode & Decode
        public override bool Decode(string tag, string data) {
            switch (tag) {
                case "gtuv": ProjectionUV = data.ToBool(); break;
                case "offset": offset = data.ToVector2(); break;
                case "tile": tiling = data.ToVector2(); break;
                default: return false;
            }
            return true;
        }

        public override StdEncoder Encode() {
            var cody = new StdEncoder();
            cody.Add_Bool("gtuv", ProjectionUV);
            cody.Add("offset", offset);
            cody.Add("tile", tiling);
            return cody;
        }
        #endregion

        #region Inspect
        public override string NameForPEGIdisplay => "vertex UV";

        public override string Tooltip => ProjectionUV ? "After setting scale and offset, paint this UVs on triengles. Use scroll wheel to change the direction a projection is facing." : "";

        #if PEGI
        public override bool Inspect() {

            bool changed = false;
            
            MeshManager mm = MeshMGMT;

            if (("UV Set: " + mm.editedUV + "").Click().nl())
                mm.editedUV = 1 - mm.editedUV;
            
          //  if (mm.selectedUV != null)  ("UV: " + (mm.selectedUV.editedUV.x) + "," + (mm.selectedUV.editedUV.y)).nl();

            if (!ProjectionUV && "Projection UV Start".Click().nl()) {
                    ProjectionUV = true;
                    UpdatePreview();
            }

            if (ProjectionUV)  {

                if ("tiling".edit(ref tiling).nl())
                   UpdatePreview();

                if ("offset".edit(ref offset).nl())
                    UpdatePreview();


                if ("Projection UV Stop".Click()) {
                    ProjectionUV = false;
                    EditedMesh.Dirty = true;
                }
            }

                /*
                if (mm.selectedUV != null && "All vert UVs from selected".Click().nl())
                {
                    foreach (UVpoint uv in mm.selectedUV.vert.uvpoints)
                        uv.editedUV = mm.selectedUV.editedUV;
                    mesh.dirty = true;
                }
            */
            return changed;

        }
        #endif
        #endregion

        void UpdatePreview() {

            if (ProjectionUV)
            {
                var m = MeshMGMT;
                if (!m.target || (m.edMesh.meshPoints == null) || (m.edMesh.meshPoints.Count < 1)) return;

                var prMesh = FreshPreviewMesh;
                
                Vector3 trgPos = m.target.transform.position;
                foreach (MeshPoint v in prMesh.meshPoints) {
                    var pUV = PosToUV((v.WorldPos - trgPos));
                    foreach (Vertex uv in v.uvpoints)
                        uv.SharedEditedUV = pUV;
                }

                m.target.meshFilter.sharedMesh = new MeshConstructor(prMesh, m.target.MeshProfile, m.target.meshFilter.sharedMesh).Construct();
                
            }
        }

        public Vector2 PosToUV(Vector3 diff)  {

            Vector2 uv = new Vector2();
 
            switch (GridNavigator.Inst().g_side) {
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
        
        public override void OnDeSelectTool() {

            foreach (var v in EditedMesh.meshPoints)
                v.CleanEmptyIndexes();

            if (ProjectionUV)
                MeshMGMT.Redraw();
        }

        public override void OnGridChange() => UpdatePreview();
        
        public override Color VertColor => Color.magenta; 

        public override bool ShowGrid => ProjectionUV; 

        public override bool ShowVerticesDefault => !ProjectionUV && !MeshMGMT.Dragging; 

        public override bool ShowLines => false; 

        public override void AssignText(MarkerWithText mrkr, MeshPoint vpoint) {
            var pvrt = MeshMGMT.GetSelectedVert();

            if ((vpoint.uvpoints.Count > 1) || (pvrt == vpoint)) {

                Texture tex = MeshMGMT.target.meshRenderer.sharedMaterial.mainTexture;

                if (pvrt == vpoint) {
                    mrkr.textm.text = (vpoint.uvpoints.Count > 1) ? ((vpoint.uvpoints.IndexOf(MeshMGMT.SelectedUV) + 1).ToString() + "/" + vpoint.uvpoints.Count.ToString() +
                        (vpoint.SmoothNormal ? "s" : "")) : "";
                    float tsize = !tex ? 128 : tex.width;
                       mrkr.textm.text +=
                        ("uv: " + (MeshMGMT.SelectedUV.EditedUV.x * tsize) + "," + (MeshMGMT.SelectedUV.EditedUV.y * tsize));
                }
                else
                    mrkr.textm.text = vpoint.uvpoints.Count.ToString() +
                        (vpoint.SmoothNormal ? "s" : "");
            }
            else mrkr.textm.text = "";
        }

        public override bool MouseEventPointedVertex()
        {

            if (EditorInputManager.GetMouseButtonDown(0)) {
                MeshMGMT.AssignSelected(PointedUV); //pointedUV.editedUV = meshMGMT.selectedUV.editedUV;
                lastCalculatedUV = PointedUV.EditedUV;
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

        public override bool MouseEventPointedLine() {

            Vertex a = PointedLine.pnts[0];
            Vertex b = PointedLine.pnts[1];

            if (Vector3.Distance(MeshMGMT.collisionPosLocal, a.Pos) < Vector3.Distance(MeshMGMT.collisionPosLocal, b.Pos))
                MeshMGMT.AssignSelected(EditedMesh.GetUVpointAFromLine(a.meshPoint, b.meshPoint));
            else
                MeshMGMT.AssignSelected(EditedMesh.GetUVpointAFromLine(b.meshPoint, a.meshPoint));

            return false;

        }

        public override bool MouseEventPointedTriangle()  {

            if (EditorInputManager.GetMouseButton(0))  {
                if (ProjectionUV) {
                    if (PointedTris.SameAsLastFrame)
                        return true;

                    if (MeshMGMT.SelectedUV == null) MeshMGMT.SelectedUV = EditedMesh.meshPoints[0].uvpoints[0];

                    Vector3 trgPos = MeshMGMT.target.transform.position;
                   // float portion = 1f / Mathf.Max(0.01f, MeshUVprojectionSize);

                    for (int i = 0; i < 3; i++) 
                       PointedTris.vertexes[i].EditedUV = PosToUV(PointedTris.vertexes[i].meshPoint.WorldPos - trgPos);

                    EditedMesh.Dirty = true;

                    return true;
                } 
            }
            return false;
        }

        Vector2 lastCalculatedUV;
        public override void ManageDragging()
        {

            if (PointedTris != null && SelectedUV != null)
            {
                Vector2 uv = SelectedUV.SharedEditedUV;
                Vector2 posUV = PointedTris.LocalPosToEditedUV(MeshMGMT.collisionPosLocal);
                Vector2 newUV = uv * 2 - posUV;
                bool isChanged = newUV != lastCalculatedUV;
                lastCalculatedUV = newUV;

                if (isChanged && !EditorInputManager.GetMouseButtonUp(0))
                {
                    var prMesh = FreshPreviewMesh;
                    if (prMesh.selectedUV != null) {
                        prMesh.selectedUV.SharedEditedUV = lastCalculatedUV;
                        MeshMGMT.target.meshFilter.sharedMesh = new MeshConstructor(prMesh, MeshMGMT.target.MeshProfile, MeshMGMT.target.meshFilter.sharedMesh).Construct();
                    }
                }
            }

            if (EditorInputManager.GetMouseButtonUp(0)) {

                    MeshMGMT.SelectedUV.SharedEditedUV = lastCalculatedUV;
                    EditedMesh.Dirty = true;
                    MeshMGMT.Dragging = false;
            }


            if (!EditorInputManager.GetMouseButton(0)) 
                MeshMGMT.Dragging = false;
            
        }
        
        public override void KeysEventPointedLine() {
            if ((KeyCode.Backspace.IsDown()))  {
                Vertex a = PointedLine.pnts[0];
                Vertex b = PointedLine.pnts[1];

                if (!EditorInputManager.getControlKey())
                    MeshMGMT.SwapLine(a.meshPoint, b.meshPoint);
                else
                    MeshMGMT.DeleteLine(PointedLine);

                EditedMesh.Dirty = true;
            }
        }
        
        public VertexUVTool()
        {
            _inst = this;
        }

    }
}