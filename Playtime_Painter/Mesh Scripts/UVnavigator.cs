using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace Playtime_Painter
{

    [ExecuteInEditMode]
    public class UVnavigator : PainterStuffMono {

        public static UVnavigator inst()
        {
            if (_inst == null)
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

                MouseDwnOffset = MyMath.Lerp(MouseDwnOffset, draggedOffset, 2 * Zoom);
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

            Vector2 v2 = MeshManager.inst.selectedUV.editedUV;
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
        public override string ToString() { return "vertex UV"; }

        public static VertexUVTool _inst;

        public bool ProjectionUV = false;
        public Vector2 tiling = Vector2.one;
        public Vector2 offset;

        public override bool Decode(string tag, string data) {
            switch (tag) {
                case "gtuv": ProjectionUV = data.ToBool(); break;
                case "offset": offset = data.ToVector2(); break;
                case "tile": tiling = data.ToVector2(); break;
                default: return false;
            }
            return true;
        }

        public override stdEncoder Encode() {
            var cody = new stdEncoder();
            cody.Add("gtuv", ProjectionUV);
            cody.Add("offset", offset);
            cody.Add("tile", tiling);
            return cody;
        }

        public VertexUVTool() {
            _inst = this;
        }
        
        public override bool PEGI() {

            bool changed = false;
            
            MeshManager mm = meshMGMT;

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
                    editedMesh.dirty = true;
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

        public override string tooltip { get {
                return ProjectionUV ? "After setting scale and offset, paint this UVs on triengles. Use scroll wheel to change the direction a projection is facing." : "";
            } }

        void UpdatePreview() {

            if (ProjectionUV)
            {
                var m = meshMGMT;
                if ((m.target == null) || (m.edMesh.vertices == null) || (m.edMesh.vertices.Count < 1)) return;

                var prMesh = freshPreviewMesh;
                
                Vector3 trgPos = m.target.transform.position;
                foreach (vertexpointDta v in prMesh.vertices) {
                    var pUV = PosToUV((v.worldPos - trgPos));
                    foreach (UVpoint uv in v.uvpoints)
                        uv.sharedEditedUV = pUV;
                }

                m.target.meshFilter.sharedMesh = new MeshConstructor(prMesh, m.target.meshProfile, m.target.meshFilter.sharedMesh).Construct();
                
            }
        }

        public Vector2 PosToUV(Vector3 diff)  {

            Vector2 uv = new Vector2();
 
            switch (GridNavigator.inst().g_side) {
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

        public override void OnSelectTool() {
                UpdatePreview();
        }

        public override void OnDeSelectTool() {

            foreach (var v in editedMesh.vertices)
                v.CleanEmptyIndexes();

            if (ProjectionUV)
                meshMGMT.Redraw();
        }

        public override void OnGridChange() {
            UpdatePreview();
        }

        public override Color vertColor {
            get
            {
                return Color.magenta; 
            }
        }

        public override bool showGrid { get { return ProjectionUV; } }

        public override bool showVerticesDefault { get { return !ProjectionUV && !meshMGMT.dragging; } }

        public override bool showLines { get { return false; } }

        public override void AssignText(MarkerWithText mrkr, vertexpointDta vpoint) {
            var pvrt = meshMGMT.GetSelectedVert();

            if ((vpoint.uvpoints.Count > 1) || (pvrt == vpoint)) {

                Texture tex = meshMGMT.target.meshRenderer.sharedMaterial.mainTexture;

                if (pvrt == vpoint) {
                    mrkr.textm.text = (vpoint.uvpoints.Count > 1) ? ((vpoint.uvpoints.IndexOf(meshMGMT.selectedUV) + 1).ToString() + "/" + vpoint.uvpoints.Count.ToString() +
                        (vpoint.SmoothNormal ? "s" : "")) : "";
                    float tsize = tex == null ? 128 : tex.width;
                       mrkr.textm.text +=
                        ("uv: " + (meshMGMT.selectedUV.editedUV.x * tsize) + "," + (meshMGMT.selectedUV.editedUV.y * tsize));
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
                meshMGMT.AssignSelected(pointedUV); //pointedUV.editedUV = meshMGMT.selectedUV.editedUV;
                lastCalculatedUV = pointedUV.editedUV;
                meshMGMT.dragging = true;
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

            UVpoint a = pointedLine.pnts[0];
            UVpoint b = pointedLine.pnts[1];

            if (Vector3.Distance(meshMGMT.collisionPosLocal, a.pos) < Vector3.Distance(meshMGMT.collisionPosLocal, b.pos))
                meshMGMT.AssignSelected(editedMesh.GetUVpointAFromLine(a.vert, b.vert));
            else
                meshMGMT.AssignSelected(editedMesh.GetUVpointAFromLine(b.vert, a.vert));

            return false;

        }

        public override bool MouseEventPointedTriangle()  {

            if (EditorInputManager.GetMouseButton(0))  {
                if (ProjectionUV) {
                    if (pointedTris.sameAsLastFrame)
                        return true;

                    if (meshMGMT.selectedUV == null) meshMGMT.selectedUV = editedMesh.vertices[0].uvpoints[0];

                    Vector3 trgPos = meshMGMT.target.transform.position;
                   // float portion = 1f / Mathf.Max(0.01f, MeshUVprojectionSize);

                    for (int i = 0; i < 3; i++) 
                       pointedTris.uvpnts[i].editedUV = PosToUV(pointedTris.uvpnts[i].vert.worldPos - trgPos);

                    editedMesh.dirty = true;

                    return true;
                } 
            }
            return false;
        }

        Vector2 lastCalculatedUV;
        public override void ManageDragging()
        {

            if (pointedTris != null && selectedUV != null)
            {
                Vector2 uv = selectedUV.sharedEditedUV;
                Vector2 posUV = pointedTris.LocalPosToEditedUV(meshMGMT.collisionPosLocal);
                Vector2 newUV = uv * 2 - posUV;
                bool isChanged = newUV != lastCalculatedUV;
                lastCalculatedUV = newUV;

                if (isChanged && !EditorInputManager.GetMouseButtonUp(0))
                {
                    var prMesh = freshPreviewMesh;
                    if (prMesh.selectedUV != null) {
                        prMesh.selectedUV.sharedEditedUV = lastCalculatedUV;
                        meshMGMT.target.meshFilter.sharedMesh = new MeshConstructor(prMesh, meshMGMT.target.meshProfile, meshMGMT.target.meshFilter.sharedMesh).Construct();
                    }
                }
            }

            if (EditorInputManager.GetMouseButtonUp(0)) {

                    meshMGMT.selectedUV.sharedEditedUV = lastCalculatedUV;
                    editedMesh.dirty = true;
                    meshMGMT.dragging = false;
            }


            if (!EditorInputManager.GetMouseButton(0)) 
                meshMGMT.dragging = false;
            
        }
        
        public override void KeysEventPointedLine() {
            if ((KeyCode.Backspace.isDown()))  {
                UVpoint a = pointedLine.pnts[0];
                UVpoint b = pointedLine.pnts[1];

                if (!EditorInputManager.getControlKey())
                    meshMGMT.SwapLine(a.vert, b.vert);
                else
                    meshMGMT.DeleteLine(pointedLine);

                editedMesh.dirty = true;
            }
        }

    } 



}