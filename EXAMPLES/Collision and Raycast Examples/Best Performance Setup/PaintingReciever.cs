using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using SharedTools_Stuff;


namespace Playtime_Painter {



    public class PaintingReciever : MonoBehaviour, IPEGI
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

        public Mesh Mesh { get { return skinnedMeshRenderer != null ? skinnedMeshRenderer.sharedMesh : meshFilter?.sharedMesh; } }
        public Renderer Rendy { get { return meshRenderer ?? skinnedMeshRenderer; } }
        public Material Material { get {
                if (Rendy == null) return null;
                if (materialIndex < Rendy.sharedMaterials.Length)
                    return Rendy.sharedMaterials[materialIndex];
                return null; }  set {
                if (materialIndex < Rendy.sharedMaterials.Length)
                {
                    var mats = Rendy.sharedMaterials;
                    mats[materialIndex] = value;
                    Rendy.materials = mats;
                }
            }
        }
        public Texture MatTex { get {
                if (Material == null) return null;
                return Material.HasProperty(textureField) ? Material.GetTexture(textureField) : Material.mainTexture;

            } set {
                if (Material.HasProperty(textureField))
                    Material.SetTexture(textureField, value);
                else Material.mainTexture = value;   } }
        
        public Texture texture;
        public Texture originalTexture;
        public bool useTexcoord2;
        public bool fromRTmanager;
        public Vector2 meshUVoffset;
        public Material originalMaterial;

        private void OnEnable()  {

            if ((originalTexture!= null) && (texture!= null) && (texture.GetType() == typeof(RenderTexture)))
                PainterCamera.Inst.Blit(originalTexture, (RenderTexture)texture);
        }

        public Texture GetTexture() {
            if (texture != null)
                return texture;

            var rtm = TexturesPool._inst;

            if (Material == null) {
                Debug.Log("No Material ");
                return null;
            }

            if (rtm!= null) {
                
                originalMaterial = Material;

                texture = rtm.GetRenderTexture();

                fromRTmanager = true;
             
                Material = Instantiate(originalMaterial);

                var tex = originalTexture ?? MatTex;
                if (tex != null)
                    PainterCamera.Inst.Blit( tex , (RenderTexture) texture);
                else
                    PainterCamera.Inst.Render(Color.black , (RenderTexture)texture);

                MatTex = texture;
            }

            return texture;
        }

        public void Restore() {
            
            if ((fromRTmanager) && (originalMaterial!= null)) {
                fromRTmanager = false;
                Material = originalMaterial;
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
                PainterCamera.Inst.Blit(originalTexture, (RenderTexture)texture);

        }

        private void OnDisable() {
            if (fromRTmanager) Restore();
        }
        #if PEGI
        public virtual bool PEGI() {

            if ("Renderer Type:".edit(() => type, this).nl()) {
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
                "   Skinned Mesh Renderer".edit(() => skinnedMeshRenderer, this).nl();


            if (type == RendererType.regular) {
                "   Mesh Filter".edit(() => meshFilter, this).nl();
                "   Renderer".edit(() => meshRenderer, this).nl();
            }



            if ((Rendy != null && (Rendy.sharedMaterials.Length > 1)) || materialIndex != 0) {
                "If more then one material:".nl();
                "   Material".select(ref materialIndex, Rendy.sharedMaterials).nl();
            }

           // "Material: ".write(material);
           // pegi.nl();
         //   "Texture: ".write(texture);
           // pegi.nl();

            if (Material) {
                var lst = Material.MyGetTextureProperties();
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
                "   Original Mesh".edit("Static objects use Combined mesh, so original one will be needed for painting", () => originalMesh, this).nl();
            }

            "For shaders which use Texcoord 2:".nl();
            "    Use second texture coordinates".toggle("If shader uses texcoord2 to display damage, this is what you want.", ref useTexcoord2).nl();


            if ((texture != null) || (MatTex == null))
                "Original Texture:".edit("Copy of this texture will be modified.", () => originalTexture, this).nl();
            "If not using Render Textures Pool:".nl();
            "Texture".edit(() => texture, this);
            if ("Find".Click().nl())
            {
                if (Rendy && Material)
                    texture = MatTex;
            }
            
            if (texture == null || texture.GetType() == typeof(RenderTexture)) {
                "Mesh UV Offset".edit("Some Meshes have UV coordinates with displacement for some reason. " +
                    "If your object doesn't use a mesh collider to provide a UV offset, this will be used.", 80, ref meshUVoffset).nl();
                if (Mesh != null && "Offset from Mesh".Click().nl()) {
                    int firstVertInSubmeshIndex = Mesh.GetTriangles(materialIndex)[0];
                    meshUVoffset = useTexcoord2 ? Mesh.uv2[firstVertInSubmeshIndex] : Mesh.uv[firstVertInSubmeshIndex];

                    meshUVoffset = new Vector2((int)meshUVoffset.x, (int)meshUVoffset.y);

                    ("Mesh Offset is " + meshUVoffset.ToString()).showNotification();
                }
            }

            if (Material != null)
            {

                if (!Material.HasProperty(textureField) && Material.mainTexture == null)
                {
                    "No Material Property Selected and no MainTex on Material".nl();
                }
                else
                {
                    if (texture != null)
                    {
                        if (texture.GetType() == typeof(Texture2D))
                        {
                            icon.Done.write();
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
                            if (Rendy)
                            {
                                icon.Done.write();
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
                            if (Rendy == null) "! Renderer needs to be Assigned.".nl();
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
                                (TexturesPool.Inst.gameObject.name + " created").showNotification(); //new GameObject().AddComponent<TexturesPool>().gameObject.name = "Texture Pool";
                        }
                    }
                }
            }
            else "No material found".nl();

           

            return false;
        }
#endif
    }

}