#if UNITY_EDITOR
using System;
using PlayerAndEditorGUI;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using  QuizCannersUtilities;

namespace PlaytimePainter
{
#if UNITY_2019_1_OR_NEWER
    // Tagging a class with the EditorTool attribute and no target type registers a global tool. Global tools are valid for any selection, and are accessible through the top left toolbar in the editor.
    [EditorTool(PainterDataAndConfig.ToolName)]
    class PainterAsIntegratedCustomTool : EditorTool {
        
        GUIContent m_IconContent;

        void OnEnable() {
            m_IconContent = new GUIContent() {
                image = icon.Painter.GetIcon(),
                text = PainterDataAndConfig.ToolName,
                tooltip = "Add Playtime Painter Component to objects to edit their textures/meshes"
            };
        }

        public override GUIContent toolbarIcon => m_IconContent;

        public override void OnToolGUI(EditorWindow window) {
            PlaytimePainterSceneViewEditor.OnSceneGuiCombined();


            var p = PlaytimePainterSceneViewEditor.painter;

            if (Event.current.type == EventType.ScrollWheel)
                Debug.Log("Got scroll wheel event");

            if (p) {

                p.FeedEvents(Event.current);

/*                Event e = Event.current;
                switch (e.type)
                {
                    case EventType.KeyDown:

                        

                        break;
                }*/
            }

        }
    }
#endif
    
    public static class PlaytimePainterSceneViewEditor
    {

        public static bool AllowEditing(PlaytimePainter targetPainter) =>
            targetPainter && (Application.isPlaying || !targetPainter.IsUiGraphicPainter) &&
            (!targetPainter.LockTextureEditing || targetPainter.IsEditingThisMesh);

        public static void OnSceneGuiCombined()
        {
         

            if (AllowEditing(painter))
            {

            
                GridUpdate(SceneView.currentDrawingSceneView);

                if (!navigating && PlaytimePainter.IsCurrentTool)
                    HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            } 


            navigating = false;
        }

        public static void GridUpdate(SceneView sceneView)
        {
            var e = Event.current;

            if (e.isMouse) // || e.type == EventType.ScrollWheel)
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

                var cam = Camera.current;

                var offScreen = (!cam || (mousePosition.x < 0 || mousePosition.y < 0
                                                              || mousePosition.x > cam.pixelWidth ||
                                                              mousePosition.y > cam.pixelHeight));

                if (!offScreen)
                {

                    var camTf = cam.transform;

                    EditorInputManager.centerRaySceneView = new Ray(camTf.position, camTf.forward);

                    mouseRayGui = HandleUtility.GUIPointToWorldRay(mousePosition);

                    EditorInputManager.mouseRaySceneView = mouseRayGui;

                    if (painter)
                        mouseRayGui = painter.PrepareRay(mouseRayGui);

                }

            }

            FeedEvents(e);

            if (e.isMouse)
            {

                RaycastHit hit;
                var isHit = Physics.Raycast(mouseRayGui, out hit);

                var pp = isHit ? hit.transform.GetComponent<PlaytimePainter>() : null;

                var refocus = OnEditorRayHit(hit, mouseRayGui);

                if (lMouseDwn && e.button == 0 && refocus && isHit)
                {

                    // #if !UNITY_2019_1_OR_NEWER
                    if (pp && pp == painter && AllowEditing(painter))
                        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                    //#endif

                    UnityUtils.FocusOn(hit.transform.gameObject);
                }

#if !UNITY_2019_1_OR_NEWER
                if (!navigating && AllowEditing(painter))
                    e.Use();
#endif

            }

            if (painter && painter.textureWasChanged)
                painter.ManagedUpdate();
        }

        public static bool OnEditorRayHit(RaycastHit hit, Ray ray)
        {

            var tf = hit.transform;
            var pointedPainter = tf?.GetComponent<PlaytimePainter>();
            var e = Event.current;

            var allowRefocusing = true;

            if (painter)
            {
                if (painter.meshEditing)
                {

                    var edited = MeshManager.target;

                    if (pointedPainter && pointedPainter != edited && pointedPainter.meshEditing
                        && !pointedPainter.SavedEditableMesh.IsNullOrEmpty() && lMouseDwn && e.button == 0)
                    {
                        MeshManager.Inst.EditMesh(pointedPainter, false);
                        allowRefocusing = true;
                    }
                    else allowRefocusing = false;


                    if (((e.button == 1 && !MeshManager.Inst.Dragging) || e.button == 2)
                        && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag ||
                            e.type == EventType.MouseUp))
                        navigating = true;

                    return allowRefocusing;
                }
                else
                {
                    if (lMouseDwn) PlaytimePainter.currentlyPaintedObjectPainter = null;

                    if (painter.NeedsGrid())
                    {
                        pointedPainter = painter;
                        allowRefocusing = false;
                    }

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

            if ((e.button == 1 || e.button == 2) && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag ||
                                                     e.type == EventType.MouseUp))
                navigating = true;


            return allowRefocusing;
        }

        public static void FeedEvents(Event e)
        {

            GridNavigator.Inst().FeedEvent(e);

            if (!painter) return;

            painter.FeedEvents(e);

            if (painter.meshEditing)
                MeshManager.Inst.UpdateInputEditorTime(e, lMouseUp, lMouseDwn);
        }

        public static Tool previousTool;

        public static PlaytimePainter painter;

        public static bool lMouseDwn;
        public static bool lMouseUp;

        public static Vector2 mousePosition;
        public static Ray mouseRayGui;

        public static bool navigating = false;

    }

}

#endif