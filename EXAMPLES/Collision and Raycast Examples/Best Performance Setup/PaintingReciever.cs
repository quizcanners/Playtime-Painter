using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;

namespace Playtime_Painter {

#if UNITY_EDITOR
using UnityEditor;
[CustomEditor(typeof(PaintingReciever))]
public class PaintingRecieverEditor : Editor
{
    public override void OnInspectorGUI() {
        ef.start(serializedObject);
        ((PaintingReciever)target).PEGI().nl();
            ef.end();
        }
}
#endif

    public class PaintingReciever : MonoBehaviour
    {

        public enum RendererType {regular, Skinned, Terrain }

        //public bool useTextureSpacePainting;
        // public Material editMaterial;
        public RendererType type;

        public Mesh originalMesh;
        public MeshFilter meshFilter;
        public Renderer meshRenderer;
        public SkinnedMeshRenderer skinnedMeshRenderer;

        [SerializeField]
        public int materialIndex;
        [SerializeField]
        string textureField;

        public Mesh mesh { get { return skinnedMeshRenderer != null ? skinnedMeshRenderer.sharedMesh : meshFilter != null ? meshFilter.sharedMesh : null; } }
        public Renderer rendy { get { return meshRenderer != null ? meshRenderer : skinnedMeshRenderer; } }
        public Material material { get {
                if (rendy == null) return null;
                if (materialIndex < rendy.sharedMaterials.Length)
                    return rendy.sharedMaterials[materialIndex];
                return null; }  set {
                if (materialIndex < rendy.sharedMaterials.Length)
                {
                    var mats = rendy.sharedMaterials;
                    mats[materialIndex] = value;
                    rendy.materials = mats;
                }
            }
        }
        public Texture matTex { get {
                if (material == null) return null;
                return material.HasProperty(textureField) ? material.GetTexture(textureField) : material.mainTexture; } set {
                if (material.HasProperty(textureField))
                    material.SetTexture(textureField, value);
                else material.mainTexture = value;   } }
        
        public Texture texture;
        public Texture originalTexture;
        public bool useTexcoord2;
        public bool fromRTmanager;
        public Vector2 meshUVoffset;
        public Material originalMaterial;

        private void OnEnable()  {

            if ((originalTexture!= null) && (texture!= null) && (texture.GetType() == typeof(RenderTexture)))
                PainterManager.inst.Render(originalTexture, (RenderTexture)texture);
        }

        public Texture getTexture() {
            if (texture != null)
                return texture;

            var rtm = TexturesPool._inst;

            if (material == null) {
                Debug.Log("No Material ");
                return null;
            }

            if (rtm!= null) {
                
                originalMaterial = material;

                texture = rtm.GetRenderTexture();

                fromRTmanager = true;
             
                material = Instantiate(originalMaterial);

                var tex = originalTexture == null ? matTex : originalTexture;
                if (tex != null)
                PainterManager.inst.Render( tex , (RenderTexture) texture);
                else
                    PainterManager.inst.Render(Color.black , (RenderTexture)texture);

                matTex = texture;
            }

            return texture;
        }

        public void Restore() {
            
            if ((fromRTmanager) && (originalMaterial!= null)) {
                fromRTmanager = false;
                material = originalMaterial;
                originalMaterial = null;
                TexturesPool._inst.ReturnOne((RenderTexture)texture);
                texture = null;
                return;
            }

            if (texture == null)
                return;

            if (originalTexture == null)
            {
                Debug.Log("Original Texture is not defined");
                return;
            } else if (originalTexture.GetType() != typeof(Texture2D)) {
                Debug.Log("There was no original Texture assigned to edit.");
            }

            if (texture.GetType() == typeof(Texture2D)) {
                
                ((Texture2D)texture).SetPixels(((Texture2D)originalTexture).GetPixels());
                ((Texture2D)texture).Apply(true);
            } else 
                PainterManager.inst.Render(originalTexture, (RenderTexture)texture);

        }

        private void OnDisable() {
            if (fromRTmanager) Restore();
        }

        public virtual bool PEGI() {

            if ("Renderer Type:".edit(() => type).nl()) {
                if ((type == RendererType.Skinned) && (skinnedMeshRenderer == null))
                    skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
                if ((type == RendererType.regular) && (meshFilter == null)) {
                    meshFilter = GetComponent<MeshFilter>();
                    meshRenderer = GetComponent<MeshRenderer>();
                }
            }

            if (type == RendererType.Terrain) {
                "Not yet ready that one".nl();
                return false;
            }

            if (type == RendererType.Skinned)
                "   Skinned Mesh Renderer".edit(() => skinnedMeshRenderer).nl();


            if (type == RendererType.regular) {
                "   Mesh Filter".edit(() => meshFilter).nl();
                "   Renderer".edit(() => meshRenderer).nl();
            }



            if ((rendy != null && (rendy.sharedMaterials.Length > 1)) || materialIndex != 0) {
                "If more then one material:".nl();
                "   Material".select(ref materialIndex, rendy.sharedMaterials).nl();
            }

           // "Material: ".write(material);
           // pegi.nl();
         //   "Texture: ".write(texture);
           // pegi.nl();

            if (material) {
                var lst = material.getTextures();
                if (lst.Count > 0) {
                    "   Property".select(ref textureField, lst).nl();
                }
                else textureField = "";
            }


            if (this.gameObject.isStatic && originalMesh == null) {
                pegi.writeWarning("Original mesh is not set.");
                pegi.newLine();
                if ((meshFilter != null) && "find mesh".Click().nl()) 
                        originalMesh = meshFilter.sharedMesh;
            }

            if (this.gameObject.isStatic) {
                "For STATIC Game Objects:".nl();
                "   Original Mesh".edit("Static objects use Combined mesh, so original one will be needed for painting", () => originalMesh).nl();
            }

            "For shaders which use Texcoord 2:".nl();
            "    Use second texture coordinates".toggle("If shader uses texcoord2 to display damage, this is what you want.", ref useTexcoord2).nl();


            if ((texture != null) || (matTex == null))
                "Original Texture:".edit("Copy of this texture will be modified.", () => originalTexture).nl();
            "If not using Render Textures Pool:".nl();
            "Texture".edit(() => texture);
            if ("Find".Click().nl())
            {
                if (rendy && material)
                    texture = matTex;
            }
            
            if (texture == null || texture.GetType() == typeof(RenderTexture)) {
                "Mesh UV Offset".edit("Some Meshes have UV coordinates with displacement for some reason. " +
                    "If your object doesn't use a mesh collider to provide a UV offset, this will be used.", 80, ref meshUVoffset).nl();
                if (mesh != null && "Offset from Mesh".Click().nl()) {
                    int firstVertInSubmeshIndex = mesh.GetTriangles(materialIndex)[0];
                    meshUVoffset = useTexcoord2 ? mesh.uv2[firstVertInSubmeshIndex] : mesh.uv[firstVertInSubmeshIndex];

                    meshUVoffset = new Vector2((int)meshUVoffset.x, (int)meshUVoffset.y);

                    ("Mesh Offset is " + meshUVoffset.ToString()).showNotification();
                }
            }

            if (material != null)
            {

                if (!material.HasProperty(textureField) && material.mainTexture == null)
                {
                    "No Material Property Selected and no MainTex on Material".nl();
                }
                else
                {
                    if (texture != null)
                    {
                        if (texture.GetType() == typeof(Texture2D))
                        {
                            icon.Done.write(25);
                            "CPU brush will work if object has MeshCollider".nl();

                            if (originalTexture != null)
                            {

                                if (originalTexture.GetType() == typeof(Texture2D))
                                {

                                    var ot = (Texture2D)originalTexture;
                                    var t = (Texture2D)texture;

                                    if ((ot.width == t.width) && (ot.height == ot.height))
                                    {

                                        if (("Undo Changes".Click()).nl())
                                        {
                                            Restore();
                                        }
                                    }
                                    else "Original and edited texture are not of the same size".nl();


                                }
                                else "Original Texture is not a Texture 2D".nl();
                            }
                            //PainterManager.inst.Render(originalTexture, (RenderTexture)texture);

                        }
                        else
                        {
                            if (rendy)
                            {
                                icon.Done.write(25);
                                "Will paint if object has any collider".nl();
                                if (skinnedMeshRenderer != null)
                                {
                                    "Colliders should be placed close to actual mesh".nl();
                                    "Otherwise brush size may be too small to reach the mesh".nl();
                                }
                            }
                            else
                                "Render Texture Painting needs Skinned Mesh or Mesh Filter to work".nl();

                            if ((originalTexture != null) && ("Undo Changes".Click().nl()))
                                Restore();
                        }


                    }
                    else
                    {

                        var rtm = TexturesPool._inst;

                        if (rtm != null)
                        {
                            "Render Texture Pool will be used to get texture".nl();
                            if (rendy == null) "! Renderer needs to be Assigned.".nl();
                            else
                            {
                                icon.Done.write("Component set up properly", 25);
                                if (fromRTmanager && "Restore".Click())
                                    Restore();
                            }
                        }
                        else
                        {
                            "No Render Texture Pool found".write();
                            if ("Create".Click().nl())
                                (TexturesPool.inst.gameObject.name + " created").showNotification(); //new GameObject().AddComponent<TexturesPool>().gameObject.name = "Texture Pool";
                        }
                    }
                }
            }
            else "No material found".nl();

           

            return false;
        }

    }

}