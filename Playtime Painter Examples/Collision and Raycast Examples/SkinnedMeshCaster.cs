using PlayerAndEditorGUI;
using QuizCannersUtilities;
using UnityEngine;

namespace PlaytimePainter.Examples {

#pragma warning disable IDE0018 // Inline variable declaration
    public class SkinnedMeshCaster : MonoBehaviour, IPEGI {

        public Brush brush = new Brush();

        public string lastShotResult = "Never fired (Left mouse button during play)";

        private void Paint() {

            RaycastHit hit;

            if (!Physics.Raycast(new Ray(transform.position, transform.forward), out hit))
            {
                lastShotResult = "Didn't hit any colliders";
                return;
            }

            var painter = hit.transform.GetComponentInParent<PlaytimePainter>();

            if (!painter)
            {
                lastShotResult = "No painter detected on {0}".F(hit.transform.name);
                return;
            }


            painter.SetTexTarget(brush);

            var cmd = painter.PaintCommand.SetBrush(brush);
            
            if (painter.skinnedMeshRenderer && !cmd.Is3DBrush) {   

                painter.UpdateMeshCollider();

                var colliderDisabled = !painter.meshCollider.enabled;

                if (colliderDisabled)
                    painter.meshCollider.enabled = true;
                
                if (!painter.meshCollider.Raycast(new Ray(transform.position, transform.forward), out hit, 99999)) {

                    lastShotResult = "Missed updated Collider";

                    if (colliderDisabled) painter.meshCollider.enabled = false;

                    return;
                }

                if (colliderDisabled) painter.meshCollider.enabled = false;

                lastShotResult = "Updated Collider for skinned mesh and Painted";

            } else
                lastShotResult = "Painted on Object";

            var v = new Stroke(hit, false);

            painter.stroke.From(hit, false);
            
            brush.Paint(painter.PaintCommand); //v,painter.SetTexTarget(brush));
        }


#if UNITY_EDITOR
        private void OnDrawGizmosSelected() {

            RaycastHit hit;

            if (Physics.Raycast(new Ray(transform.position, transform.forward), out hit)) {

                var painter = hit.transform.GetComponentInParent<PlaytimePainter>();

                Gizmos.color = !painter ? Color.red : Color.green;
                Gizmos.DrawLine(transform.position, hit.point);

            }
            else
            {

                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, transform.position + transform.forward);
            }
        }
#endif


        void Update() {

            if (Input.GetMouseButton(0))
                Paint();

        }

        public bool Inspect()
        {
            var changed = false;

      
            ("Will cast a ray in transform.forward direction when Left Mouse Button is pressed. " +
             "Target objects need to have PlaytimePainter component attached. Then this brush will be applied " +
             "on texture (if not locked) selected on target PlaytimePainter component" +
             "This Component has it's own brush configuration. Can be replaced with PainterCamera.Data.brushConfig " +
             "to use global brush.").fullWindowDocumentationClickOpen("How to use Skinned Mesh Caster", 15);
        

            if ("Paint!".Click().nl())
                Paint();

            "Last ray Cast result: {0}".F(lastShotResult).nl();

            brush.Targets_PEGI().nl(ref changed);
            brush.Mode_Type_PEGI().nl(ref changed);
            brush.ColorSliders().nl(ref changed);
            

            return changed;
        }
    }
}