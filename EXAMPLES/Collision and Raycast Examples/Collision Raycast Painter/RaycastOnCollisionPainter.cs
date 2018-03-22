using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;



namespace Playtime_Painter
{

#if UNITY_EDITOR

    using UnityEditor;

    [CustomEditor(typeof(RaycastOnCollisionPainter))]
    public class PainterCasterEditor : Editor{

        public override void OnInspectorGUI() {
            ef.start(serializedObject);
            ((RaycastOnCollisionPainter)target).PEGI().nl();
            ef.end();
        }
    }
#endif

    public class RaycastOnCollisionPainter : MonoBehaviour
    {

        public BrushConfig brush = new BrushConfig();
        List<paintingCollision> paintingOn = new List<paintingCollision>();

        paintingCollision getFor (GameObject go) {
  
            foreach (var col in paintingOn)
                if (col.painter.gameObject == go) return col;

            PlaytimePainter pp = go.GetComponent<PlaytimePainter>();
            if (pp == null) return null;

            paintingCollision ncol = new paintingCollision(pp);
            paintingOn.Add(ncol);

            return ncol;
        }

        private void OnCollisionExit(Collision collision) {
            paintingCollision p = getFor(collision.gameObject);
            if (p == null) return;
            p.vector.mouseUp = true;
            Paint(collision, p);
        }

        private void OnCollisionEnter(Collision collision) {

            paintingCollision p = getFor(collision.gameObject);
            if (p == null) return;
            p.vector.mouseDwn = true;
            Paint(collision, p);
        }

        void OnCollisionStay(Collision collision) {
            paintingCollision p = getFor(collision.gameObject);
            if (p == null) return;
            Paint(collision, p);
        }

        void Paint (Collision collision, paintingCollision pCont)
        {
          

         

            if (pCont.painter.isPaintingInWorldSpace(brush)) {
                StrokeVector v = pCont.vector;
                v.posTo = transform.position;
                if (v.mouseDwn) v.posFrom = v.posTo;
                pCont.painter.Paint(v, brush);
            } else {

                if (collision.contacts.Length > 0) {
                    var cp = collision.contacts[0];

                   

                    RaycastHit hit;
                    Ray ray = new Ray(cp.point+ cp.normal*0.1f, -cp.normal);

                    if (collision.collider.Raycast(ray, out hit, 2f)) {

                        StrokeVector v = pCont.vector;

                        v.uvTo = hit.textureCoord;
                        if (v.mouseDwn) v.uvFrom = v.uvTo;

                        pCont.painter.SetTexTarget(brush).Paint(pCont.vector, brush);
                    }
                }
            }

        }


        public bool PEGI() {
            bool changed = false;
            ("Painting on " + paintingOn.Count + " objects").nl();

            changed |= brush.Targets_PEGI().nl();
            changed |= brush.Mode_Type_PEGI(brush.TargetIsTex2D).nl();
            changed |= brush.blitMode.PEGI(brush, null);
            Color col = brush.colorLinear.ToColor();
            if (pegi.edit(ref col).nl())
                brush.colorLinear.From(col);
            changed |= brush.ColorSliders_PEGI();
            return changed;
        }
    }
}