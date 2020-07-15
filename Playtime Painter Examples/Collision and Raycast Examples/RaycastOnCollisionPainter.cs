using System.Collections.Generic;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using UnityEngine;

namespace PlaytimePainter.Examples
{

    public class RaycastOnCollisionPainter : MonoBehaviour, IPEGI
    {

        public Brush brush = new Brush();
        readonly List<PaintingCollision> _paintingOn = new List<PaintingCollision>();

        private PaintingCollision GetPainterFrom(GameObject go)
        {

            foreach (var col in _paintingOn)
                if (col.painter.gameObject == go) return col;

            var pp = go.GetComponent<PlaytimePainter>();

            if (!pp) return null;

            var nCol = new PaintingCollision(pp);

            _paintingOn.Add(nCol);

            return nCol;
        }

        private void OnCollisionExit(Collision collision)
        {
            var p = GetPainterFrom(collision.gameObject);
            if (p == null) return;
            p.vector.MouseUpEvent = true;
            Paint(collision, p);
        }

        private void OnCollisionEnter(Collision collision)
        {

            var p = GetPainterFrom(collision.gameObject);
            if (p == null) return;
            p.vector.MouseDownEvent = true;
            Paint(collision, p);
        }

        private void OnCollisionStay(Collision collision)
        {
            var p = GetPainterFrom(collision.gameObject);
            if (p == null) return;
            Paint(collision, p);
        }

        private void Paint(Collision collision, PaintingCollision pCont)
        {

            if (pCont.painter.PaintCommand.SetBrush(brush).Is3DBrush)
            {

                var v = pCont.vector;
                v.posTo = transform.position;
                if (v.MouseDownEvent)
                    v.posFrom = v.posTo;

                var command = pCont.painter.PaintCommand;
                
                var originalStroke = command.Stroke;

                brush.Paint(command); 

                command.Stroke = originalStroke;

            }
            else
            {

                if (collision.contacts.Length <= 0) return;

                var cp = collision.contacts[0];
                
                var ray = new Ray(cp.point + cp.normal * 0.1f, -cp.normal);

                RaycastHit hit;
                if (!collision.collider.Raycast(ray, out hit, 2f)) return;

                var v = pCont.vector;

                v.uvTo = hit.textureCoord;
                if (v.MouseDownEvent) v.uvFrom = v.uvTo;

                var command = pCont.painter.PaintCommand;

                var originalVector = command.Stroke;

                command.Stroke = pCont.vector;

                pCont.painter.SetTexTarget(brush);

                brush.Paint(command);

                command.Stroke = originalVector;
            }
        }

        #region Inspector

        public bool Inspect()
        {
            var changed = false;

            pegi.FullWindowService.DocumentationClickOpen(()=> "During collision will try to cast ray in the direction of that collision. " +
                                                       "If target has Playtime Painter Component this script will try to paint on it.", "How to use Raycast On Collision");

            if (Application.isPlaying)
                "Painting on {0} objects".F(_paintingOn.Count).nl();

            brush.Targets_PEGI().nl(ref changed);
            brush.Mode_Type_PEGI().nl(ref changed);
            brush.ColorSliders().nl(ref changed);

            return changed;
        }

        #endregion
    }
}