using UnityEngine;
using UnityEditor;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

#if UNITY_EDITOR

namespace Playtime_Painter {

    [CustomEditor(typeof(PlaytimePainter))]
    public class PlaytimePainterClassDrawer : PEGI_Inspector<PlaytimePainter> {
        
        public void GridUpdate(SceneView sceneview)
        {
            Event e = Event.current;

            if (e.isMouse || e.type == EventType.ScrollWheel)
                EditorInputManager.FeedMouseEvent(e);
            
            if (!PlaytimePainter.IsCurrentTool)
                return;
            
        
            if (e.isMouse)
            {

                if (e.button == 0)
                {
                    L_mouseDwn = (e.type == EventType.MouseDown) && (e.button == 0);
                    L_mouseUp = (e.type == EventType.MouseUp) && (e.button == 0);
                }
                
                mousePosition = Event.current.mousePosition;

                bool offScreen = (Camera.current != null && (mousePosition.x < 0 || mousePosition.y < 0
                    || mousePosition.x > Camera.current.pixelWidth ||
                       mousePosition.y > Camera.current.pixelHeight));

                if (!offScreen) {
                    rayGUI = HandleUtility.GUIPointToWorldRay(mousePosition);
                    EditorInputManager.raySceneView = rayGUI;
                }

            }

            FeedEvents(e);

            if (e.isMouse)
            {

                RaycastHit hit;
                var ishit = Physics.Raycast(rayGUI, out hit);

                PlaytimePainter pp = ishit ? hit.transform.GetComponent<PlaytimePainter>() : null;

                bool refocus = OnEditorRayHit(hit, rayGUI);

                if (L_mouseDwn && e.button == 0 && refocus && ishit)
                {

                    if (pp && pp == painter && AllowEditing(painter))
                        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

                    UnityHelperFunctions.FocusOn(hit.transform.gameObject);
                }

                if ((!navigating) && AllowEditing(painter))
                    e.Use();
            }

            if (painter && painter.textureWasChanged)
                painter.Update();
        }


        public bool AllowEditing(PlaytimePainter targ) => targ && (!targ.LockTextureEditing || targ.IsEditingThisMesh);
        
        public bool OnEditorRayHit(RaycastHit hit, Ray ray) {

            Transform tf = hit.transform;
            PlaytimePainter pointedPainter = tf?.GetComponent<PlaytimePainter>();
            Event e = Event.current;

            bool allowRefocusing = true;

            if (painter)
            {
                if (painter.meshEditing)
                {

                    PlaytimePainter edited = MeshManager.Inst.target;
                    
                    if (pointedPainter && pointedPainter != edited && pointedPainter.meshEditing
                        && !pointedPainter.SavedEditableMesh.IsNullOrEmpty() && L_mouseDwn && e.button == 0) {
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
                    if (L_mouseDwn) PlaytimePainter.currentlyPaintedObjectPainter = null;

                    if (painter.NeedsGrid()) { pointedPainter = painter; allowRefocusing = false; }
                    
                    if (pointedPainter)
                    {
                        StrokeVector st = pointedPainter.stroke;
                        st.mouseUp = L_mouseUp;
                        st.mouseDwn = L_mouseDwn;

                        pointedPainter.OnMouseOver_SceneView(hit, e);
                    }

                }
            }

            if (L_mouseUp)
                PlaytimePainter.currentlyPaintedObjectPainter = null;

            if ((e.button == 1 || e.button == 2) && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag || e.type == EventType.MouseUp))
                navigating = true;


            return allowRefocusing;
        }

        public void FeedEvents(Event e) {
            
            GridNavigator.Inst().FeedEvent(e);

            if (painter) {
                painter.FeedEvents(e);
                if (painter.meshEditing)
                    MeshManager.Inst.UpdateInputEditorTime(e,  L_mouseUp, L_mouseDwn);
            }
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
        
        public static bool L_mouseDwn;
        public static bool L_mouseUp;
        
        public Vector2 mousePosition;
        public Ray rayGUI = new Ray();

        public override void OnInspectorGUI()
        {
            painter = (PlaytimePainter)target;

            base.OnInspectorGUI();
        }

    }

}

#endif

