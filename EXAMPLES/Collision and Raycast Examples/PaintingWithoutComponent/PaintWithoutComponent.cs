using System.Collections;
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

        void Paint() {

            RaycastHit hit;

            bool anyHits = false;
            bool anyRecivers = false;
            var texturesNeedUpdate = new List<imgData>();

            for (int i=0; i<shoots; i++)
            if (Physics.Raycast(new Ray(transform.position,  transform.forward + transform.right*Random.Range(-spread, spread) + transform.up * Random.Range(-spread, spread)), out hit)) {

                var reciver = hit.transform.GetComponentInParent<PaintingReciever>();

                    anyHits = true;

                    if ((reciver != null) && (reciver.texture!= null)) {

                        anyRecivers = true;

                        var rendTex = (reciver.texture.GetType() == typeof(RenderTexture)) ? (RenderTexture)reciver.texture : null;

                        if (rendTex != null)  {

                            if (reciver.skinnedMesh != null)
                                BrushTypeSphere.Paint(rendTex, reciver.gameObject, reciver.skinnedMesh, brush, hit.point);
                            else if (reciver.meshFilter != null)
                                BrushTypeSphere.Paint(rendTex, reciver.gameObject, reciver.meshFilter.sharedMesh, brush, hit.point);

                        }
                        else if (reciver.texture.GetType() == typeof(Texture2D))
                        {

                            if (hit.collider.GetType() != typeof(MeshCollider))
                                Debug.Log("Can't get UV coordinates from a Non-Mesh Collider");

                            Blit_Functions.Paint(hit.textureCoord, 1, (Texture2D)reciver.texture, Vector2.zero, Vector2.one, brush);
                            var id = reciver.texture.getImgData();

                            if (!texturesNeedUpdate.Contains(id)) texturesNeedUpdate.Add(id);

                        }
                        else Debug.Log(reciver.gameObject.name + " doesn't have any combination of paintable things setup on his PainterReciver.");
                }

            }

            foreach (var t in texturesNeedUpdate) t.SetAndApply(false); // Set True for Mipmaps. 
            //Not to waste performance, don't SetAndApply after each edit, but at the end of the frame (LateUpdate maybe) and only if texture was changed.
            //Mip maps will slow things down, so best is to disable them.


            if (!anyHits) Debug.Log("No hits");
            else if (!anyRecivers) Debug.Log("Attach PaintingReciever script to objects you want to Paint on.");
            
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


        public bool PEGI()
        {
            bool changed = false;

            "Bullets:".edit(ref shoots, 1, 50).nl();
            "Spread:".edit(ref spread, 0f , 1f).nl();
            if ("Fire!".Click().nl())
                Paint();

            changed |= brush.BrushForTargets_PEGI().nl();
            changed |= brush.Mode_Type_PEGI(brush.TargetIsTex2D).nl();
            changed |= brush.currentBlitMode().PEGI(brush, null);
            Color col = brush.color.ToColor();
            if (pegi.edit(ref col).nl())
                brush.color.From(col);
            changed |= brush.ColorSliders_PEGI();
            return changed;
        }
    }
}