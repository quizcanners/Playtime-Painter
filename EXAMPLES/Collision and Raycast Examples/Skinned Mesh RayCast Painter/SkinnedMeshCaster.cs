using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace Playtime_Painter.Examples
{

    public class SkinnedMeshCaster : MonoBehaviour, IPEGI {

        public BrushConfig brush = new BrushConfig();

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

            if (painter.skinnedMeshRenderer && !brush.IsA3dBrush(painter)) {   

                painter.UpdateColliderForSkinnedMesh();

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

            var v = new StrokeVector(hit, false);
            
            brush.Paint(v,painter.SetTexTarget(brush));
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

#if PEGI


        private bool _showInfo;
        public bool Inspect()
        {
            var changed = false;

            if (icon.Question.foldout("Documentation", ref _showInfo).changes(ref changed))
            {
                ("Will cast a ray in transform.forward direction when Left Mouse Button is pressed. " +
                 "Target objects need to have PlaytimePainter component attached. Then this brush will be applied " +
                 "on texture (if not locked) selected on target PlaytimePainter component" +
                 "This Component has it's own brush configuration. Can be replaced with PainterCamera.Data.brushConfig " +
                 "to use global brush.").writeHint();
            }
            else
            {

                if ("Paint!".Click().nl())
                    Paint();

                "Last ray Cast result: {0}".F(lastShotResult).nl();

                brush.Targets_PEGI().nl(ref changed);
                brush.Mode_Type_PEGI().nl(ref changed);
                brush.ColorSliders().nl(ref changed);
            }

            return changed;
        }
#endif
    }
}