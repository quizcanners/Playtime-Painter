using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

#if UNITY_EDITOR

namespace Playtime_Painter {

    [CustomEditor(typeof(PlaytimePainter))]
    public class PlaytimePainterClassDrawer : Editor
    {

        static PainterDataAndConfig Cfg => PainterCamera.Data;

        bool CurrentTool => PlaytimePainter.IsCurrent_Tool;

        public bool AllowEditing(PlaytimePainter targ) => (targ) && ((!targ.LockTextureEditing) || targ.IsEditingThisMesh);
        
        public bool OnEditorRayHit(RaycastHit hit, Ray ray) {

            Transform tf = hit.transform;
            PlaytimePainter pointedPainter = tf?.GetComponent<PlaytimePainter>();
            Event e = Event.current;

            bool allowRefocusing = true;

            if (painter != null)
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

                        //if ((edited == null) || (edited != pointedPainter))
                          //  allowRefocusing = true;
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

            if (painter != null) {

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
                EditorInputManager.feedMouseEvent(e);

            if (e.isMouse)
            {

                RaycastHit hit;
                bool ishit = Physics.Raycast(rayGUI, out hit);

                PlaytimePainter pp = ishit ? hit.transform.GetComponent<PlaytimePainter>() : null;

                bool refocus = OnEditorRayHit(hit, rayGUI);

                if ((L_mouseDwn) && (e.button == 0) && (refocus) && ishit)
                {

                    if ((pp != null) && (pp == painter) && (AllowEditing(painter)))
                        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

                    UnityHelperFunctions.FocusOn(hit.transform.gameObject);
                }

                if ((!navigating) && AllowEditing(painter))
                    e.Use();
            }

            if (!PlaytimePainter.IsCurrent_Tool) return;

            if ((painter != null) && (painter.textureWasChanged))
                painter.Update();

        }

        public virtual void OnSceneGUI()
        {

            if (CurrentTool && (painter != null) && (UnityEditorInternal.InternalEditorUtility.GetIsInspectorExpanded(painter) == false))
                PlaytimePainter.IsCurrent_Tool = false;

            if (AllowEditing(painter))
            {

                GridUpdate(SceneView.currentDrawingSceneView);

                if ((!navigating) && CurrentTool)
                    HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            }

            navigating = false;
        }

        public static Tool previousTool;

        static PlaytimePainter painter;

        public override void OnInspectorGUI() {

#if PEGI

            PainterCamera rtp = PainterCamera.Inst;

            var cfg = Cfg;

            painter = (PlaytimePainter)target;

            if  (painter.gameObject.IsPrefab()) {
                "Inspecting a prefab.".nl();
                return;
            }

          

            if (!cfg) {
                "No Config Detected".nl();
                if (icon.Refresh.Click()) {
                    PainterStuff.applicationIsQuitting = false;
                    if (PainterCamera.Inst)
                    PainterCamera.Inst.triedToFindPainterData = false;
                }
                return;
            }

            ef.start(serializedObject);
     

            if (!PlaytimePainter.IsCurrent_Tool) {
                if (icon.Off.Click("Click to Enable Tool", 25)) {
                    PainterDataAndConfig.toolEnabled = true;
                    CloseAllButThis(painter);
                    painter.CheckPreviewShader();
                    PlaytimePainter.HideUnityTool();
                }

                pegi.Lock_UnlockWindow(painter.gameObject);

                painter.gameObject.end();
                return;
            } else {

                if ((PlaytimePainter.IsCurrent_Tool && (painter.terrain != null) && (Application.isPlaying == false) && (UnityEditorInternal.InternalEditorUtility.GetIsInspectorExpanded(painter.terrain) == true)) ||
                    (icon.On.Click("Click to Disable Tool", 25))) {
                    PainterDataAndConfig.toolEnabled = false;
                    MeshManager.Inst.DisconnectMesh();
                    painter.SetOriginalShaderOnThis();
                    painter.UpdateOrSetTexTarget(TexTarget.Texture2D);
                    PlaytimePainter.RestoreUnityTool();
                }

                pegi.Lock_UnlockWindow(painter.gameObject);

            }

            painter.InitIfNotInited();

            ImageData image = painter.ImgData;

            Texture tex = painter.GetTextureOnMaterial();
            if ((!painter.meshEditing) && ((tex != null) && (image == null)) || ((image != null) && (tex == null)) || ((image != null) && (tex != image.texture2D) && (tex != image.CurrentTexture())))
                painter.textureWasChanged = true;

            painter.PEGI_MAIN();

            painter.gameObject.end();
#else 
               if (GUILayout.Button("Enable PEGI inspector")){
                    "Recompilation in progress ".showNotificationIn3D_Views();
                    PEGI_StylesDrawer.EnablePegi();
               }
#endif
        }

        public static bool navigating = false;

        public virtual void OnEnable() =>  navigating = true;
        
        public static bool L_mouseDwn;
        public static bool L_mouseUp;

        public void CloseAllButThis(PlaytimePainter trg)
        {
            trg.enabled = true;
            GameObject go = trg.gameObject;
            Component[] cs = go.GetComponents(typeof(Component));

            foreach (Component c in cs)
                if (c.GetType() != typeof(PlaytimePainter))
                    UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(c, false);


            UnityHelperFunctions.FocusOn(null);
            PainterCamera.refocusOnThis = go;
        }

        public Vector2 mousePosition;
        public Ray rayGUI = new Ray();

    }
#endif
        }

