using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

#if UNITY_EDITOR

namespace Playtime_Painter {

    [CustomEditor(typeof(PlaytimePainter))]
    public class PlaytimePainterClassDrawer : PEGI_Editor<PlaytimePainter> {

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

                    allowRefocusing = false;

                    if (pointedPainter != null) {

                        if ((pointedPainter != edited) && (pointedPainter.meshEditing) 
                            && (pointedPainter.SavedEditableMesh != null ) && L_mouseDwn && (e.button == 0)) {
                            MeshManager.Inst.EditMesh(pointedPainter, false);
                            allowRefocusing = true;
                        }
                    }

                    if ((((e.button == 1) && (!MeshManager.Inst.Dragging))
                        || (e.button == 2)) && ((e.type == EventType.MouseDown) || (e.type == EventType.MouseDrag) || (e.type == EventType.MouseUp)))

                        navigating = true;

                    return allowRefocusing;
                }
                else
                {
                    if (L_mouseDwn) PlaytimePainter.currently_Painted_Object = null;

                    if (painter.NeedsGrid()) { pointedPainter = painter; allowRefocusing = false; }
                    
                    if (pointedPainter != null)
                    {
                        StrokeVector st = pointedPainter.stroke;
                        st.mouseUp = L_mouseUp;
                        st.mouseDwn = L_mouseDwn;

                        pointedPainter.OnMouseOver_SceneView(hit, e);
                    }

                }
            }
            if (L_mouseUp) PlaytimePainter.currently_Painted_Object = null;

            if (((e.button == 1) || (e.button == 2)) && ((e.type == EventType.MouseDown) || (e.type == EventType.MouseDrag) || (e.type == EventType.MouseUp)))
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
        
        public void GridUpdate(SceneView sceneview) {

            if (!PlaytimePainter.IsCurrent_Tool)
                return;


            Event e = Event.current;

            if (e.isMouse)
            {

                if (e.button != 0) return;

                L_mouseDwn = (e.type == EventType.MouseDown) && (e.button == 0);
                L_mouseUp = (e.type == EventType.MouseUp) && (e.button == 0);

                mousePosition = Event.current.mousePosition;

                if (Camera.current != null && (mousePosition.x < 0 || mousePosition.y < 0
                    || mousePosition.x > Camera.current.pixelWidth ||
                       mousePosition.y > Camera.current.pixelHeight))
                    return;

                rayGUI = HandleUtility.GUIPointToWorldRay(mousePosition);
                EditorInputManager.raySceneView = rayGUI;

            }

            FeedEvents(e);

            if (e.isMouse || (e.type == EventType.ScrollWheel))
                EditorInputManager.FeedMouseEvent(e);

            if (e.isMouse)
            {

                RaycastHit hit;
                bool ishit = Physics.Raycast(rayGUI, out hit);

                PlaytimePainter pp = ishit ? hit.transform.GetComponent<PlaytimePainter>() : null;

                bool refocus = OnEditorRayHit(hit, rayGUI);

                if ((L_mouseDwn) && (e.button == 0) && (refocus) && ishit)
                {

                    if ((pp) && (pp == painter) && (AllowEditing(painter)))
                        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

                    UnityHelperFunctions.FocusOn(hit.transform.gameObject);
                }

                if ((!navigating) && AllowEditing(painter))
                    e.Use();
            }

            if (!PlaytimePainter.IsCurrent_Tool) return;

            if ((painter) && (painter.textureWasChanged))
                painter.Update();

        }

        public virtual void OnSceneGUI()
        {

            if (PlaytimePainter.IsCurrent_Tool && painter && !UnityEditorInternal.InternalEditorUtility.GetIsInspectorExpanded(painter))
                PlaytimePainter.IsCurrent_Tool = false;

            if (AllowEditing(painter))
            {
                GridUpdate(SceneView.currentDrawingSceneView);

                if (!navigating && PlaytimePainter.IsCurrent_Tool)
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

    }
#endif
        }

