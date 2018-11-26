using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;



namespace Playtime_Painter.Examples
{



    public class RaycastOnCollisionPainter : MonoBehaviour, IPEGI
    {

        public BrushConfig brush = new BrushConfig();
        List<PaintingCollision> paintingOn = new List<PaintingCollision>();

        PaintingCollision GetFor (GameObject go) {
  
            foreach (var col in paintingOn)
                if (col.painter.gameObject == go) return col;

            PlaytimePainter pp = go.GetComponent<PlaytimePainter>();
            if (pp == null) return null;

            PaintingCollision ncol = new PaintingCollision(pp);
            paintingOn.Add(ncol);

            return ncol;
        }

        private void OnCollisionExit(Collision collision) {
            PaintingCollision p = GetFor(collision.gameObject);
            if (p == null) return;
            p.vector.mouseUp = true;
            Paint(collision, p);
        }

        private void OnCollisionEnter(Collision collision) {

            PaintingCollision p = GetFor(collision.gameObject);
            if (p == null) return;
            p.vector.mouseDwn = true;
            Paint(collision, p);
        }

        void OnCollisionStay(Collision collision) {
            PaintingCollision p = GetFor(collision.gameObject);
            if (p == null) return;
            Paint(collision, p);
        }

        void Paint (Collision collision, PaintingCollision pCont)
        {
          

         

            if (brush.IsA3Dbrush(pCont.painter)) {
                StrokeVector v = pCont.vector;
                v.posTo = transform.position;
                if (v.mouseDwn) v.posFrom = v.posTo;
                brush.Paint(v, pCont.painter);
            } else {

                if (collision.contacts.Length > 0) {
                    var cp = collision.contacts[0];

                   

                    RaycastHit hit;
                    Ray ray = new Ray(cp.point+ cp.normal*0.1f, -cp.normal);

                    if (collision.collider.Raycast(ray, out hit, 2f)) {

                        StrokeVector v = pCont.vector;

                        v.uvTo = hit.textureCoord;
                        if (v.mouseDwn) v.uvFrom = v.uvTo;

                       brush.Paint(pCont.vector, pCont.painter.SetTexTarget(brush));
                    }
                }
            }

        }

#if PEGI
        public bool Inspect() {
            bool changed = false;
            ("Painting on " + paintingOn.Count + " objects").nl();

            changed |= brush.Targets_PEGI().nl();
            changed |= brush.Mode_Type_PEGI().nl();
            changed |= brush.ColorSliders_PEGI();
            return changed;
        }
#endif
    }
}