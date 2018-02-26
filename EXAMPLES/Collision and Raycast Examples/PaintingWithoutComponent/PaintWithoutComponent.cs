﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;

// For Painting On MObjects which don't have Painter Component

namespace Painter
{

#if UNITY_EDITOR

    using UnityEditor;

    [CustomEditor(typeof(PaintWithoutComponent))]
    public class PaintWithoutComponentEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            ef.start(serializedObject);
            ((PaintWithoutComponent)target).PEGI().nl();
        }
    }
#endif



    public class PaintWithoutComponent : MonoBehaviour  {

        public BrushConfig brush = new BrushConfig();
        public int shoots = 1;
        public float spread = 0;


        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
                Paint();
        }


        void Paint() {

            RaycastHit hit;

           // bool anyHits = false;
            //bool anyRecivers = false;
            var texturesNeedUpdate = new List<imgData>();

            for (int i=0; i<shoots; i++)
            if (Physics.Raycast(new Ray(transform.position,  transform.forward + transform.right*Random.Range(-spread, spread) + transform.up * Random.Range(-spread, spread)), out hit)) {

                var reciver = hit.transform.GetComponentInParent<PaintingReciever>();

              //      anyHits = true;

                    if ((reciver != null) && (reciver.getTexture() != null)) {

                //        anyRecivers = true;

                        var rendTex = (reciver.texture.GetType() == typeof(RenderTexture)) ? (RenderTexture)reciver.texture : null;

                        if (rendTex != null)  {

                            if (reciver.skinnedMeshRenderer != null)
                                BrushTypeSphere.Paint(rendTex, reciver.gameObject, reciver.skinnedMeshRenderer, brush, hit.point, reciver.useTexcoord2);
                            else if (reciver.meshFilter != null)
                                BrushTypeSphere.Paint(rendTex, reciver.gameObject, reciver.meshFilter.sharedMesh, brush, hit.point, reciver.useTexcoord2);

                        } else if (reciver.texture.GetType() == typeof(Texture2D)) {

                            if (hit.collider.GetType() != typeof(MeshCollider))
                                Debug.Log("Can't get UV coordinates from a Non-Mesh Collider");

                            Blit_Functions.Paint(reciver.useTexcoord2 ? hit.textureCoord2 : hit.textureCoord, 1, (Texture2D)reciver.texture, Vector2.zero, Vector2.one, brush);
                            var id = reciver.texture.getImgData();

                            if (!texturesNeedUpdate.Contains(id)) texturesNeedUpdate.Add(id);

                        }
                        else Debug.Log(reciver.gameObject.name + " doesn't have any combination of paintable things setup on his PainterReciver.");
                }

            }

            foreach (var t in texturesNeedUpdate) t.SetAndApply(true); // True for Mipmaps. Best to disable mipmaps on textures and set to false 
            //Not to waste performance, don't SetAndApply after each edit, but at the end of the frame (LateUpdate maybe) and only if texture was changed.
            //Mip maps will slow things down, so best is to disable them.


         //   if (!anyHits) Debug.Log("No hits");
           // else if (!anyRecivers) Debug.Log("Attach PaintingReciever script to objects you want to Paint on.");
            
        }


#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {

            Ray ray = new Ray(transform.position, transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {

                var painter = hit.transform.GetComponentInParent<PlaytimePainter>();

                Gizmos.color = painter == null ? Color.red : Color.green;
                Gizmos.DrawLine(transform.position, hit.point);

            }
            else
            {

                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, transform.position + transform.forward);
            }
        }
#endif

        bool hint;
        public bool PEGI()
        {
            bool changed = false;

            "Bullets:".edit(ref shoots, 1, 50).nl();
            "Spread:".edit(ref spread, 0f , 1f).nl();

            if ("Fire!".Click().nl())
                Paint();

            if ("HINT".foldout(ref hint).nl()) {
                "I can paint on Painting Recivers with:".nl();
                "Mesh Collider + any Texture".nl();
                "Skinned Mesh + any Collider + Render Texture".nl();
                "Also its better to use textures without mipmaps".nl();
                "Render Texture Painting will fail if material has tiling or offset".nl();
                "Editing will be symmetrical if mesh is symmetrical".nl();
            }

            changed |= brush.BrushForTargets_PEGI().nl();
            brush._bliTMode = 0;
            brush._type = 3;

            changed |= brush.blitMode.PEGI(brush, null);
            Color col = brush.color.ToColor();
            if (pegi.edit(ref col).nl())
                brush.color.From(col);
            changed |= brush.ColorSliders_PEGI();

            if (brush.paintingRGB == false)
                pegi.writeHint("Enable RGB, disable A to use faster Brush Shader (if painting to RenderTexture).");
            return changed;
        }
    }
}