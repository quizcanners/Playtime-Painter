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

        // For best performance on Skinned Meshes use RenderTexture
        public MeshFilter meshFilter;
        public Renderer _renderer;
        public SkinnedMeshRenderer skinnedMeshRenderer;

        Renderer rendy { get { return _renderer != null ? _renderer : skinnedMeshRenderer; } }

        public Texture texture;
        public Texture2D originalTexture;
        public bool useTexcoord2;
        public bool fromRTmanager;
        Material originalMaterial;

        private void OnEnable()
        {
            if ((originalTexture!= null) && (texture!= null) && (texture.GetType() == typeof(RenderTexture)))
                PainterManager.inst.Render(originalTexture, (RenderTexture)texture);
        }

        public Texture getTexture() {
            if (texture != null) return texture;

            var rtm = RenderTexturesPool._inst;

            if (rtm!= null) {
                fromRTmanager = true;
                texture = rtm.GetOne();
                originalMaterial = rendy.sharedMaterial;
                rendy.material = Instantiate(originalMaterial);
               
                PainterManager.inst.Render(originalTexture == null ? rendy.material.mainTexture : originalTexture , (RenderTexture) texture);
                rendy.material.mainTexture = texture;
            }

            return texture;
        }

        public void Restore() {

          

            if ((fromRTmanager) && (originalMaterial!= null)) {
                fromRTmanager = false;
                rendy.sharedMaterial = originalMaterial;
                originalMaterial = null;
                RenderTexturesPool._inst.ReturnOne((RenderTexture)texture);
                texture = null;
                return;
            }

            if (texture == null)
                return;

            if (originalTexture == null)
            {
                Debug.Log("Original Texture is not defined");
                return;
            }

            if (texture.GetType() == typeof(Texture2D)) {
                ((Texture2D)texture).SetPixels(originalTexture.GetPixels());
                ((Texture2D)texture).Apply(true);
            } else 
                PainterManager.inst.Render(originalTexture, (RenderTexture)texture);

        }

        private void OnDisable() {
            if (fromRTmanager) Restore();
        }

        public bool PEGI() {

            "***** If not using RT Pool".nl();
            "Texture".edit(() => texture);
            if ("Find".Click().nl()) {
                var rendy = GetComponent<Renderer>();
                if ((rendy != null) && (rendy.sharedMaterial) && (rendy.sharedMaterial.mainTexture != null))
                    texture = rendy.sharedMaterial.mainTexture;
            }
            "Original Texture:".edit("You can use this texture to copy data to texture", () => originalTexture).nl();


          

          
            "***** For Skinned meshes ****** ".nl();

            "Skinned Mesh Renderer".edit(() => skinnedMeshRenderer).nl();

            "***** For Regular Meshes ****** ".nl();
            "Mesh Filter".edit(() => meshFilter).nl();
            "Renderer".edit(() => _renderer).nl();

            "Use second texture coordinates".toggle("If shader uses texcoord2 to display damage, this is what you want.", ref useTexcoord2).nl();



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
                    if (_renderer != null || skinnedMeshRenderer != null) {
                        icon.Done.write(25);
                        "Will paint if object has any collider".nl();
                        "Colliders should be placed close to actual mesh".nl();
                        "Otherwise brush size may be too small to reach the mesh".nl();
                    }
                    else
                        "Render Texture Painting needs Skinned Mesh or Mesh Filter to work".nl();

                    if ((originalTexture != null) && ("Undo Changes".Click().nl()))
                        Restore();
                }


            }
            else
            {

                var rtm = RenderTexturesPool._inst;

                if (rtm != null) {
                    "Render Texture Pool will be used to get texture".nl();
                    if (rendy == null)  "! Renderer needs to be Assigned.".nl();
                    else
                    {
                        icon.Done.write("Component set up properly", 25);
                        if (fromRTmanager &&  "Restore".Click())
                            Restore();
                    }
                }
                else {
                    "No Render Texture Pool found".write();
                    if ("Create".Click().nl())
                        new GameObject().AddComponent<RenderTexturesPool>().gameObject.name = "Render Texture Pool"; 
                }
            }

           

           

            return false;
        }

    }
}