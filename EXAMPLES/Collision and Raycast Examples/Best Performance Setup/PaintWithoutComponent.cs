using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using SharedTools_Stuff;


// For Painting On MObjects which don't have Painter Component

namespace Playtime_Painter.Examples
{

    public class PaintWithoutComponent : MonoBehaviour, IPEGI
    {

        public BrushConfig brush = new BrushConfig();
        public int shoots = 1;
        public float spread = 0;


        private void Update()
        {
            if (Input.GetMouseButton(0))
                Paint();
        }

        List<ImageData> texturesNeedUpdate = new List<ImageData>();

        void Paint() {

            RaycastHit hit;

            for (int i = 0; i < shoots; i++)
                if (Physics.Raycast(new Ray(transform.position, transform.forward + transform.right * Random.Range(-spread, spread) + transform.up * Random.Range(-spread, spread)), out hit)) {

                    var recivers = hit.transform.GetComponentsInParent<PaintingReciever>();
                    PaintingReciever reciver = null;
                    //Debug.Log("Hit");
                    if (recivers.Length > 0) {

                        var submesh = 0;
                        reciver = recivers[0];

                        // IF FEW SUBMESHES
                        if (hit.collider.GetType() == typeof(MeshCollider))
                        {

                            submesh = ((MeshCollider)hit.collider).sharedMesh.GetSubmeshNumber(hit.triangleIndex);

                            if (recivers.Length > 1)
                            {

                                var mats = reciver.Renderer.materials;

                                var material = mats[submesh % mats.Length];

                                reciver = null;

                                foreach (var r in recivers)
                                    if (r.Material == material)
                                    {
                                        reciver = r;
                                        break;
                                    }
                            }
                        }
                        else
                            submesh = reciver.materialIndex;

                        // ACTUAL PAINTING

                    if (reciver != null) {
                        var tex = reciver.GetTexture();
                        if (tex != null)
                        {
                            var rendTex = (reciver.texture.GetType() == typeof(RenderTexture)) ? (RenderTexture)reciver.texture : null;

                                // WORLD SPACE BRUSH

                            if (rendTex != null)
                            {
                                    var st = new StrokeVector(hit.point)
                                    {
                                        unRepeatedUV = hit.collider.GetType() == typeof(MeshCollider) ?
                                        (reciver.useTexcoord2 ? hit.textureCoord2 : hit.textureCoord).Floor() : reciver.meshUVoffset,
                                        

                                    };

                                    if (reciver.type == PaintingReciever.RendererType.Skinned && reciver.skinnedMeshRenderer != null)
                                        BrushTypeSphere.Paint(rendTex, reciver.gameObject, reciver.skinnedMeshRenderer, brush, st, submesh);
                                    else if (reciver.type == PaintingReciever.RendererType.Regular && reciver.meshFilter != null)
                                    {
                                        var mat = reciver.Material;
                                        if (mat != null && mat.IsAtlased())
                                            BrushTypeSphere.PaintAtlased (rendTex, reciver.gameObject,
                                          reciver.originalMesh ? reciver.originalMesh : reciver.meshFilter.sharedMesh, brush, st, new List<int> { submesh }, (int)mat.GetFloat(PainterDataAndConfig.atlasedTexturesInARow));
                                        else
                                        BrushTypeSphere.Paint(rendTex, reciver.gameObject,
                                            reciver.originalMesh ? reciver.originalMesh : reciver.meshFilter.sharedMesh, brush, st, new List<int> { submesh } );
                                    }
                            }
                            // TEXTURE SPACE BRUSH
                            else if (reciver.texture.GetType() == typeof(Texture2D)) {

                                if (hit.collider.GetType() != typeof(MeshCollider))
                                    Debug.Log("Can't get UV coordinates from a Non-Mesh Collider");

                                Blit_Functions.Paint(reciver.useTexcoord2 ? hit.textureCoord2 : hit.textureCoord, 1, (Texture2D)reciver.texture, Vector2.zero, Vector2.one, brush, null);
                                var id = reciver.texture.GetImgData();
                                texturesNeedUpdate.AddIfNew(id);

                            }
                            else Debug.Log(reciver.gameObject.name + " doesn't have any combination of paintable things setup on his PainterReciver.");
                        }
                    }
                }
            }
    
        }


        void LateUpdate()
        {
            foreach (var t in texturesNeedUpdate)
                t.SetAndApply(true); // True for Mipmaps. But best to disable mipmaps on textures or set this to false 

            texturesNeedUpdate.Clear();
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()  {

            Ray ray = new Ray(transform.position, transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {

                var painter = hit.transform.GetComponentInParent<PlaytimePainter>();

                Gizmos.color = painter == null ? Color.red : Color.green;
                Gizmos.DrawLine(transform.position, hit.point);

            }
            else   {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, transform.position + transform.forward);
            }
        }
#endif
#if PEGI
        bool hint;
        public bool Inspect()
        {
            bool changed = false;

            "Bullets:".edit(50, ref shoots, 1, 50).nl(ref changed);
            "Spread:".edit(50, ref spread, 0f , 1f).nl(ref changed);

            if ("Fire!".Click().nl())
                Paint();

            if ("HINT".foldout(ref hint).nl()) {
                "I can paint on Painting Recivers with:".nl();
                "Mesh Collider + any Texture".nl();
                "Skinned Mesh + any Collider + Render Texture".nl();
                "Also its better to use textures without mipmaps".nl();
                "Render Texture Painting will fail if material has tiling or offset".nl();
                "Editing will be symmetrical if mesh is symmetrical".nl();
                "Brush type should be Sphere".nl();
            }

            brush.Targets_PEGI().nl(ref changed);
            brush.Mode_Type_PEGI().nl(ref changed);
            brush.ColorSliders_PEGI().nl(ref changed);

            if (brush.PaintingRGB == false)
                pegi.writeHint("Enable RGB, disable A to use faster Brush Shader (if painting to RenderTexture).");
            return changed;
        }
#endif
    }
}