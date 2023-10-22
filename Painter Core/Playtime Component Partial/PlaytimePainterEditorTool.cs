#if UNITY_EDITOR

using UnityEditor.EditorTools;
using QuizCanners.Inspect;
using PainterTool.MeshEditing;
using QuizCanners.Utils;
using  UnityEditor;
using UnityEngine;

namespace PainterTool
{

#pragma warning disable IDE0018 // Inline variable declaration

    // Tagging a class with the EditorTool attribute and no target type registers a global tool. Global tools are valid for any selection,
    // and are accessible through the top left toolbar in the editor.
    
    [EditorTool(SO_PainterDataAndConfig.ToolName)]
    internal class PlaytimePainterEditorTool : EditorTool {
        private GUIContent m_IconContent;

        private void OnEnable() {
            m_IconContent = new GUIContent
            {
                image = Icon.Painter.GetIcon(),
                text = SO_PainterDataAndConfig.ToolName,
                tooltip = "Add Playtime Painter Component to objects to edit their textures/meshes"
            };
        }

        public override GUIContent toolbarIcon => m_IconContent;

        public override void OnToolGUI(EditorWindow window) {
            PlaytimePainterSceneViewEditor.OnSceneGuiCombined();
            
            var p = PlaytimePainterSceneViewEditor.painter;
            
            if (p) 
                p.FeedEvents(Event.current);

        }
    }

    public static class PlaytimePainterSceneViewEditor
    {

        public static bool AllowEditing(PainterComponent targetPainter) =>
            targetPainter && (Application.isPlaying || !targetPainter.IsUiGraphicPainter) &&
            (!targetPainter.TextureEditingBlocked || targetPainter.IsEditingThisMesh);

        public static void OnSceneGuiCombined() {
         
            if (AllowEditing(painter)) {

                GridUpdate(SceneView.currentDrawingSceneView);

                if (!navigating && PainterComponent.IsCurrentTool)
                    HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            } 
            
            navigating = false;
        }

        public static void GridUpdate(SceneView sceneView)
        {
            var e = Event.current;

            if (e.isMouse) 
                PlaytimePainter_EditorInputManager.FeedMouseEvent(e);

            if (!PainterComponent.IsCurrentTool)
                return;

            if (e.isMouse) {

                if (e.button == 0) {
                    lMouseDwn = (e.type == EventType.MouseDown) && (e.button == 0);
                    lMouseUp = (e.type == EventType.MouseUp) && (e.button == 0);
                }

                mousePosition = Event.current.mousePosition;

                var cam = Camera.current;

                var offScreen = (!cam || (mousePosition.x < 0 || mousePosition.y < 0
                                                              || mousePosition.x > cam.pixelWidth ||
                                                              mousePosition.y > cam.pixelHeight));

                if (!offScreen) {

                    var camTf = cam.transform;

                    PlaytimePainter_EditorInputManager.centerRaySceneView = new Ray(camTf.position, camTf.forward);

                    mouseRayGui = HandleUtility.GUIPointToWorldRay(mousePosition);

                    PlaytimePainter_EditorInputManager.mouseRaySceneView = mouseRayGui;

                    if (painter)
                        mouseRayGui = painter.PrepareRay(mouseRayGui);

                }

            }

            FeedEvents(e);

            if (e.isMouse) {

                RaycastHit hit;
                var isHit = Physics.Raycast(mouseRayGui, out hit);

                var pp = isHit ? hit.transform.GetComponent<PainterComponent>() : null;

                OnEditorRayHit_AllowRefocusing(hit, out bool canRefocus);

                if (lMouseDwn && e.button == 0 && isHit) {
                    
                    if (pp && pp == painter && AllowEditing(painter))
                        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

                    if (canRefocus)
                    {
                        QcUnity.FocusOn(hit.transform.gameObject);
                    }
                }

                #if !UNITY_2019_1_OR_NEWER
                if (!navigating && AllowEditing(painter))
                    e.Use();
                #endif

            }

            if (painter && painter.textureWasChanged)
                painter.ManagedUpdateOnFocused();
        }

        public static void OnEditorRayHit_AllowRefocusing(RaycastHit hit, out bool allowRefocusing)
        {

            var tf = hit.transform;
            var pointedPainter = tf ? tf.GetComponent<PainterComponent>() : null;
            var e = Event.current;

            allowRefocusing = true;

            if (painter)
            {
                if (painter.meshEditing)
                {

                    var edited = MeshPainting.target;

                    if (pointedPainter && pointedPainter != edited && pointedPainter.meshEditing
                        && !pointedPainter.SavedEditableMesh.IsEmpty && lMouseDwn && e.button == 0)
                    {
                        Painter.MeshManager.EditMesh(pointedPainter, false);
                        allowRefocusing = true;
                    }
                    else allowRefocusing = false;


                    if (((e.button == 1 && !Painter.MeshManager.Dragging) || e.button == 2)
                        && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag ||
                            e.type == EventType.MouseUp))
                        navigating = true;
                }

                if (lMouseDwn)
                    PainterComponent.currentlyPaintedObjectPainter = null;

                if (painter.NeedsGrid() || painter.GlobalBrushType.IsAWorldSpaceBrush)
                {
                    pointedPainter = painter;
                    allowRefocusing = false;
                }

                if (pointedPainter)
                {
                    var st = pointedPainter.stroke;
                    st.MouseUpEvent = lMouseUp;
                    st.MouseDownEvent = lMouseDwn;

                    pointedPainter.OnMouseOverSceneView(hit);
                }
            }

            if (lMouseUp)
                PainterComponent.currentlyPaintedObjectPainter = null;

            if ((e.button == 1 || e.button == 2) && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag ||
                                                     e.type == EventType.MouseUp))
                navigating = true;

        }

        public static void FeedEvents(Event e)
        {

            MeshPainting.Grid.FeedEvent(e);

            if (!painter) return;

            painter.FeedEvents(e);

            if (painter.meshEditing)
                Painter.MeshManager.UpdateInputEditorTime(e);
        }

        public static Tool previousTool;

        public static PainterComponent painter;

        public static bool lMouseDwn;
        public static bool lMouseUp;

        public static Vector2 mousePosition;
        public static Ray mouseRayGui;

        public static bool navigating;

    }

}

#endif