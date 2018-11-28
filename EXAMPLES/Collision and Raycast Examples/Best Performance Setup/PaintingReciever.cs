using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using SharedTools_Stuff;


namespace Playtime_Painter.Examples {
    
    [ExecuteInEditMode]
    public class PaintingReciever : MonoBehaviour, IPEGI {

        public enum RendererType {Regular, Skinned }
        
        public RendererType type;

        public Mesh originalMesh;
        public MeshFilter meshFilter;
        public Renderer meshRenderer;
        public SkinnedMeshRenderer skinnedMeshRenderer;

        [SerializeField]
        public int materialIndex;
        [SerializeField]
        string textureField = "";

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
            if (!Application.isPlaying)
                Refresh();

            if (Application.isPlaying && (originalTexture!= null) && (texture!= null) && (texture.GetType() == typeof(RenderTexture)))
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

                texture.GetImgData().useTexcoord2 = useTexcoord2;

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

        void Refresh()
        {
            if (!meshFilter)
                meshFilter = GetComponent<MeshFilter>();

            if (!meshRenderer)
                meshRenderer = GetComponent<Renderer>();

            if (!skinnedMeshRenderer)
                skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        }
        
        private void OnDisable() {
            if (fromRTmanager) Restore();
           
        }

        #region Inspector

        [SerializeField] bool showOptional = false;
        
        #if PEGI


        public virtual bool Inspect() {

            bool changes = false;

            if (icon.Refresh.Click("Find stuff automatically"))
                Refresh();

            if ("Renderer Type:".editEnum(90, ref type).nl()) {
                if ((type == RendererType.Skinned) && (skinnedMeshRenderer == null))
                    skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
                if ((type == RendererType.Regular) && (meshFilter == null)) {
                    meshFilter = GetComponent<MeshFilter>();
                    meshRenderer = GetComponent<MeshRenderer>();
                }
            }

            switch (type) {
                case RendererType.Skinned:
                "   Skinned Mesh Renderer".edit(90, ref skinnedMeshRenderer).nl(ref changes);
                    break;

                case RendererType.Regular:
                "   Mesh Filter".edit(90, ref meshFilter).nl(ref changes);
                "   Renderer".edit(90, ref meshRenderer).nl(ref changes);
                    break;
            }

            if ((Rendy && (Rendy.sharedMaterials.Length > 1)) || materialIndex != 0) 
                "   Material".select(80, ref materialIndex, Rendy.sharedMaterials).nl();
            
            if (Material) {
                var lst = Material.MyGetTextureProperties();
                if (lst.Count > 0) 
                    "   Property".select(80, ref textureField, lst).nl();
                else textureField = "";
            }
            
            if (gameObject.isStatic && !originalMesh) {                
                "For STATIC Game Objects original mesh is needed:".writeHint();

                pegi.nl();

                if (meshFilter && icon.Search.Click("Find mesh")) 
                        originalMesh = meshFilter.sharedMesh;
            }

            if (gameObject.isStatic)
                "  Original Mesh".edit("Static objects use Combined mesh, so original one will be needed for painting", 50, ref originalMesh).nl();



            if ("  Use second texture coordinates".toggleIcon("If shader uses texcoord2 (Baked Light) to display damage, turn this ON.", ref useTexcoord2, true).nl() && texture)
                texture.GetImgData().useTexcoord2 = useTexcoord2;
            
            if (Material)
            {

                if (!Material.HasProperty(textureField) && !Material.mainTexture )
                    "No Material Property Selected and no MainTex on Material".nl();
                else
                {
                    if (texture != null)
                    {
                        if (texture.GetType() == typeof(Texture2D))
                        {
                            icon.Done.write();
                            "CPU brush will work if object has MeshCollider".nl();

                            if (originalTexture) {

                                if (originalTexture.GetType() == typeof(Texture2D)) {

                                    var ot = (Texture2D)originalTexture;
                                    var t = (Texture2D)texture;

                                    if ((ot.width == t.width) && (ot.height == ot.height)) {
                                        if (("Undo Changes".Click()).nl())
                                            Restore();
                                    }
                                    else "Original and edited texture are not of the same size".nl();
                                }
                                else "Original Texture is not a Texture 2D".nl();
                            }
                   
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

                        if (rtm != null) {
                            "Render Texture Pool will be used to get texture".nl();
                            if (Rendy == null) "! Renderer needs to be Assigned.".nl();
                            else {
                                icon.Done.write();
                                "COMPONENT SET UP CORRECTLY".write();
                                if (fromRTmanager && "Restore".Click())
                                    Restore();
                                pegi.nl();
                            }
                        }
                        else
                        {
                            "No Render Texture Pool found".write();
                            if ("Create".Click().nl())
                                (TexturesPool.Inst.gameObject.name + " created").showNotificationIn3D_Views(); 
                        }
                    }
                }
            }
            else "No material found".nl();

            if ("Advanced".foldout(ref showOptional).nl()) {

                if (texture || !MatTex)
                    "Start Texture:".edit("Copy of this texture will be modified.", 110, ref originalTexture).nl(ref changes);
                
                "Target Texture".edit("If not using Render Textures Pool", 120, ref texture);
                if (Rendy && Material && "Find".Click().nl())
                    texture = MatTex;

                if (!texture || texture.GetType() == typeof(RenderTexture)) {

                    "Mesh UV Offset".edit("Some Meshes have UV coordinates with displacement for some reason. " +
                        "If your object doesn't use a mesh collider to provide a UV offset, this will be used.", 80, ref meshUVoffset).nl();
                    if (Mesh && "Offset from Mesh".Click().nl())
                    {
                        int firstVertInSubmeshIndex = Mesh.GetTriangles(materialIndex)[0];
                        meshUVoffset = useTexcoord2 ? Mesh.uv2[firstVertInSubmeshIndex] : Mesh.uv[firstVertInSubmeshIndex];

                        meshUVoffset = new Vector2((int)meshUVoffset.x, (int)meshUVoffset.y);

                        ("Mesh Offset is " + meshUVoffset.ToString()).showNotificationIn3D_Views();
                    }
                }
            }


            return changes;
        }
        #endif
        #endregion
    }

}