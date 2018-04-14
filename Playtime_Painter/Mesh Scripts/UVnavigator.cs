using System.Collections;
using System.Collections.Generic;
using UnityEngine;


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
        public bool GridToUVon = false;
        Vector2 textureOffset = new Vector2();

        void MeshUVediting()
        {
            if (Input.GetMouseButton(0))
            {
                Vector2 nuv = MeshManager.RoundUVs(GetHitUV(), GridNavigator.inst().UVsnapToPixelPortion * rend.material.mainTexture.width);
                MeshManager.inst.selectedUV.editedUV = nuv;


                var m = meshMGMT;
                if ((m.target == null) || (m.edMesh.vertices == null) || (m.edMesh.vertices.Count < 1)) return;

                if ((GridToUVon))
                {
                    if (m.target != null)
                        m.previewEdMesh.CopyFrom(m.edMesh);

                    if (m.selectedUV == null) m.selectedUV = m.edMesh.vertices[0].uv[0];
                    foreach (vertexpointDta v in m.previewEdMesh.vertices)
                    {
                        foreach (UVpoint uv in v.uv)
                        {
                            uv.editedUV = m.PosToUV(v.pos);
                        }
                    }

                }
            }
        }

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

        Vector2 GetHitUV()
        {
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
            MeshUVediting();

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



    /*
    public class VertexUVTool : MeshToolBase
    {
        public override string ToString() { return "vertex UV"; }

        public static VertexUVTool _inst;

        public VertexUVTool() {
            _inst = this;
        }
        
        public override void PEGI()
        {
            MeshManager tmp = mgmt;

            pegi.write("Edit UV 1:", 70);
            pegi.toggleInt (ref MeshManager.editedUV);
            pegi.newLine();

            if (meshMGMT.selectedUV != null)
                if (pegi.Click("ALL from selected")) {
                    foreach (vertexpointDta vr in meshMGMT._Mesh.vertices)
                        foreach (UVpoint uv in vr.uv)
                            uv.editedUV = meshMGMT.selectedUV.editedUV;

                    meshMGMT._Mesh.dirty = true;
                }

            pegi.newLine();

            if (meshMGMT.selectedUV != null)
                pegi.write("UV: " + (tmp.selectedUV.editedUV.x) + "," + (tmp.selectedUV.editedUV.y));

            pegi.newLine();
            if (meshMGMT.GridToUVon) {
                pegi.write("Projection size");
                if (pegi.edit(ref cfg.MeshUVprojectionSize))
                    meshMGMT.ProcessScaleChange();
            }
            pegi.newLine();

           if  (pegi.toggle(ref tmp.GridToUVon, "Grid Painting ", 90)) {
                //if (!tmp.GridToUVon)
                    //cfg._meshTool = MeshTool.uv;
                tmp.UpdatePreviewIfGridedDraw();
            }

            pegi.newLine();

            if (tmp.selectedUV != null)
                if (pegi.Click("All vert UVs from selected"))  {
                    foreach (UVpoint uv in tmp.selectedUV.vert.uv)
                        uv.editedUV = tmp.selectedUV.editedUV;

                    tmp._Mesh.dirty = true;
                }


            pegi.newLine();


        }

        public override Color vertColor
        {
            get
            {
                return Color.magenta; 
            }
        }

        public override void AssignText(MarkerWithText mrkr, vertexpointDta vpoint)
        {

         

            var pvrt = meshMGMT.GetSelectedVert();

            if ((vpoint.uv.Count > 1) || (pvrt == vpoint))
            {

                Texture tex = meshMGMT.target.meshRenderer.sharedMaterial.mainTexture;

                if (pvrt == vpoint)
                {
                    mrkr.textm.text = (vpoint.uv.Count > 1) ? ((vpoint.uv.IndexOf(meshMGMT.selectedUV) + 1).ToString() + "/" + vpoint.uv.Count.ToString() +
                        (vpoint.SmoothNormal ? "s" : "")) : "";
                    float tsize = tex == null ? 128 : tex.width;
                       mrkr.textm.text +=
                        ("uv: " + (meshMGMT.selectedUV.editedUV.x * tsize) + "," + (meshMGMT.selectedUV.editedUV.y * tsize));
                }
                else
                    mrkr.textm.text = vpoint.uv.Count.ToString() +
                        (vpoint.SmoothNormal ? "s" : "");
            }
            else mrkr.textm.text = "";
        }

        public override void MouseEventPointedVertex()
        {
         

            if (EditorInputManager.GetMouseButtonDown(0))
            {
                if ((meshMGMT.selectedUV != null) && (meshMGMT.pointedUV != null))
                {
                    meshMGMT.pointedUV.editedUV = meshMGMT.selectedUV.editedUV;
                    meshMGMT._Mesh.dirty = true;
                }

              
            }

            if ((EditorInputManager.GetMouseButtonDown(1)) && (meshMGMT.pointedUV != null) && (UVnavigator.inst() != null))
                UVnavigator.inst().CenterOnUV(meshMGMT.pointedUV.editedUV);

        }

        public override void MouseEventPointedLine()
        {
            UVpoint a = line.pnts[0];
            UVpoint b = line.pnts[1];

            if (Vector3.Distance(meshMGMT.collisionPosLocal, a.pos) < Vector3.Distance(meshMGMT.collisionPosLocal, b.pos))
                meshMGMT.AssignSelected(meshMGMT._Mesh.GetUVpointAFromLine(a.vert, b.vert));
            else
                meshMGMT.AssignSelected(meshMGMT._Mesh.GetUVpointAFromLine(b.vert, a.vert));

        }

        public override void MouseEventPointedTriangle()  {

                if (EditorInputManager.GetMouseButtonDown(0))  {
                        if (meshMGMT.GridToUVon) {
                            if (meshMGMT.selectedUV == null) meshMGMT.selectedUV = meshMGMT._Mesh.vertices[0].uv[0];

                            for (int i = 0; i < 3; i++)
                                meshMGMT.pointedTris.uvpnts[i].editedUV = meshMGMT.PosToUV(meshMGMT.pointedTris.uvpnts[i].pos);
                            meshMGMT._Mesh.dirty = true;
                        }
            }
        }

        public override void KeysEventPointedLine()
        {
            if ((KeyCode.Backspace.isDown()))
            {
                UVpoint a = line.pnts[0];
                UVpoint b = line.pnts[1];

                if (!EditorInputManager.getControlKey())
                    meshMGMT.SwapLine(a.vert, b.vert);
                else
                    meshMGMT.DeleteLine(line);

                meshMGMT._Mesh.dirty = true;
            }
        }

    } */



}