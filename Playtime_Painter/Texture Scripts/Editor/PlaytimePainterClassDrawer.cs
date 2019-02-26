using UnityEngine;
using UnityEditor;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

#if UNITY_EDITOR

namespace Playtime_Painter {

    [CustomEditor(typeof(PlaytimePainter))]
    public class PlaytimePainterClassDrawer : PEGI_Inspector<PlaytimePainter> {
        
        public void GridUpdate(SceneView sceneView)
        {
            var e = Event.current;

            if (e.isMouse || e.type == EventType.ScrollWheel)
                EditorInputManager.FeedMouseEvent(e);
            
            if (!PlaytimePainter.IsCurrentTool)
                return;
            
            if (e.isMouse)
            {

                if (e.button == 0)
                {
                    lMouseDwn = (e.type == EventType.MouseDown) && (e.button == 0);
                    lMouseUp = (e.type == EventType.MouseUp) && (e.button == 0);
                }
                
                mousePosition = Event.current.mousePosition;

                var offScreen = (Camera.current != null && (mousePosition.x < 0 || mousePosition.y < 0
                    || mousePosition.x > Camera.current.pixelWidth ||
                       mousePosition.y > Camera.current.pixelHeight));

                if (!offScreen) {
                    rayGui = HandleUtility.GUIPointToWorldRay(mousePosition);
                    EditorInputManager.raySceneView = rayGui;
                }

            }

            FeedEvents(e);

            if (e.isMouse)
            {

                RaycastHit hit;
                var isHit = Physics.Raycast(rayGui, out hit);

                var pp = isHit ? hit.transform.GetComponent<PlaytimePainter>() : null;

                var refocus = OnEditorRayHit(hit, rayGui);

                if (lMouseDwn && e.button == 0 && refocus && isHit)
                {

                    if (pp && pp == painter && AllowEditing(painter))
                        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

                    UnityHelperFunctions.FocusOn(hit.transform.gameObject);
                }

                if ((!navigating) && AllowEditing(painter))
                    e.Use();
            }

            if (painter && painter.textureWasChanged)
                painter.ManualUpdate();
        }


        public bool AllowEditing(PlaytimePainter targetPainter) => targetPainter && (Application.isPlaying || !targetPainter.IsUiGraphicPainter) && (!targetPainter.LockTextureEditing || targetPainter.IsEditingThisMesh);
        
        public bool OnEditorRayHit(RaycastHit hit, Ray ray) {

            var tf = hit.transform;
            var pointedPainter = tf?.GetComponent<PlaytimePainter>();
            var e = Event.current;

            var allowRefocusing = true;

            if (painter)
            {
                if (painter.meshEditing)
                {

                    var edited = MeshManager.Inst.target;
                    
                    if (pointedPainter && pointedPainter != edited && pointedPainter.meshEditing
                        && !pointedPainter.SavedEditableMesh.IsNullOrEmpty() && lMouseDwn && e.button == 0) {
                        MeshManager.Inst.EditMesh(pointedPainter, false);
                        allowRefocusing = true;
                    }
                    else allowRefocusing = false;


                    if (((e.button == 1 && !MeshManager.Inst.Dragging) || e.button == 2) 
                        && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag || e.type == EventType.MouseUp))
                        navigating = true;

                    return allowRefocusing;
                }
                else
                {
                    if (lMouseDwn) PlaytimePainter.currentlyPaintedObjectPainter = null;

                    if (painter.NeedsGrid()) { pointedPainter = painter; allowRefocusing = false; }
                    
                    if (pointedPainter)
                    {
                        var st = pointedPainter.stroke;
                        st.mouseUp = lMouseUp;
                        st.mouseDwn = lMouseDwn;

                        pointedPainter.OnMouseOverSceneView(hit, e);
                    }
                }
            }

            if (lMouseUp)
                PlaytimePainter.currentlyPaintedObjectPainter = null;

            if ((e.button == 1 || e.button == 2) && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag || e.type == EventType.MouseUp))
                navigating = true;


            return allowRefocusing;
        }

        public void FeedEvents(Event e) {
            
            GridNavigator.Inst().FeedEvent(e);

            if (!painter) return;

            painter.FeedEvents(e);

            if (painter.meshEditing)
                MeshManager.Inst.UpdateInputEditorTime(e,  lMouseUp, lMouseDwn);
        }

        public virtual void OnSceneGUI()
        {

            if (PlaytimePainter.IsCurrentTool && painter && !UnityEditorInternal.InternalEditorUtility.GetIsInspectorExpanded(painter))
                PlaytimePainter.IsCurrentTool = false;

            if (AllowEditing(painter))
            {
                GridUpdate(SceneView.currentDrawingSceneView);

                if (!navigating && PlaytimePainter.IsCurrentTool)
                    HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            }

            navigating = false;
        }

        public static Tool previousTool;

        static PlaytimePainter painter;

        public static bool navigating = false;

        public virtual void OnEnable() =>  navigating = true;
        
        public static bool lMouseDwn;
        public static bool lMouseUp;
        
        public Vector2 mousePosition;
        public Ray rayGui;

        public override void OnInspectorGUI()
        {
            painter = (PlaytimePainter)target;

            base.OnInspectorGUI();
        }

    }

}

#endif

