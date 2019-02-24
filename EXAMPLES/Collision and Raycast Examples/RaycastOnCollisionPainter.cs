using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace Playtime_Painter.Examples
{
    
    public class RaycastOnCollisionPainter : MonoBehaviour, IPEGI
    {

        public BrushConfig brush = new BrushConfig();
        readonly List<PaintingCollision> _paintingOn = new List<PaintingCollision>();

        private PaintingCollision GetPainterFrom (GameObject go) {
  
            foreach (var col in _paintingOn)
                if (col.painter.gameObject == go) return col;

            var pp = go.GetComponent<PlaytimePainter>();

            if (!pp) return null;

            var nCol = new PaintingCollision(pp);

            _paintingOn.Add(nCol);

            return nCol;
        }

        private void OnCollisionExit(Collision collision) {
            var p = GetPainterFrom(collision.gameObject);
            if (p == null) return;
            p.vector.mouseUp = true;
            Paint(collision, p);
        }

        private void OnCollisionEnter(Collision collision) {

            var p = GetPainterFrom(collision.gameObject);
            if (p == null) return;
            p.vector.mouseDwn = true;
            Paint(collision, p);
        }

        private void OnCollisionStay(Collision collision) {
            var p = GetPainterFrom(collision.gameObject);
            if (p == null) return;
            Paint(collision, p);
        }

        private void Paint (Collision collision, PaintingCollision pCont)
        {

            if (brush.IsA3DBrush(pCont.painter)) {

                var v = pCont.vector;
                v.posTo = transform.position;
                if (v.mouseDwn) v.posFrom = v.posTo;
                brush.Paint(v, pCont.painter);
              
            } else {

                if (collision.contacts.Length <= 0) return;

                var cp = collision.contacts[0];
                
                RaycastHit hit;

                var ray = new Ray(cp.point+ cp.normal*0.1f, -cp.normal);

                if (!collision.collider.Raycast(ray, out hit, 2f)) return;

                var v = pCont.vector;
                var p = pCont.painter;

                v.uvTo = hit.textureCoord;
                if (v.mouseDwn) v.uvFrom = v.uvTo;

                brush.Paint(pCont.vector, pCont.painter.SetTexTarget(brush));
            }
        }

        #region Inspector
#if PEGI


        public bool Inspect()
        {
            var changed = false;
            
            ("During collision will try to cast ray in the direction of that collision. " +
             "If target has Playtime Painter Component this script will try to paint on it.").fullWindowDocumentationClick("How to use this?");
           
            if (Application.isPlaying)
                "Painting on {0} objects".F(_paintingOn.Count).nl();

            brush.Targets_PEGI().nl(ref changed);
            brush.Mode_Type_PEGI().nl(ref changed);
            brush.ColorSliders().nl(ref changed);
            
            return changed;
        }
        #endif
        #endregion
    }
}