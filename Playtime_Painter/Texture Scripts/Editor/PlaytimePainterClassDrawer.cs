using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

#if UNITY_EDITOR

namespace Playtime_Painter {

    [CustomEditor(typeof(PlaytimePainter))]
    public class PlaytimePainterClassDrawer : SceneViewEditable<PlaytimePainter> {

        static PainterDataAndConfig Cfg { get { return PainterCamera.Data; } }

        public override bool AllowEditing(PlaytimePainter targ) {
            return (targ) && ((!targ.LockTextureEditing) || targ.IsEditingThisMesh);
        }

        public override bool OnEditorRayHit(RaycastHit hit, Ray ray) {

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

        public override void FeedEvents(Event e) {
            
            GridNavigator.Inst().FeedEvent(e);

            if (painter != null) {

                painter.FeedEvents(e);

                if (painter.meshEditing)
                MeshManager.Inst.UpdateInputEditorTime(e,  L_mouseUp, L_mouseDwn);

                
            }
        }
        
        public override void GridUpdate(SceneView sceneview) {

            base.GridUpdate(sceneview);

            if (!IsCurrentTool()) return;

            if ((painter != null) && (painter.textureWasChanged))
                painter.Update();

        }

     
    
        
        public static Tool previousTool;

        public override void OnInspectorGUI() {

#if !PEGI
             if (GUILayout.Button("Enable PEGI inspector")){
             "Recompilation in progress ".showNotification();
                PEGI_StylesDrawer.EnablePegi();
 
}
#endif




#if PEGI

            painter = (PlaytimePainter)target;

            if  (painter.gameObject.IsPrefab()) {
                "Inspecting a prefab.".nl();
                return;
            }

            PainterCamera rtp = PainterCamera.Inst;

            if (!Cfg) {
                "No Config Detected".nl();
                return;
            }

            ef.start(serializedObject);
     

            if (!PlaytimePainter.IsCurrent_Tool()) {
                if (icon.Off.Click("Click to Enable Tool", 25)) {
                    PlaytimeToolComponent.enabledTool = typeof(PlaytimePainter);
                    CloseAllButThis(painter);
                    painter.CheckPreviewShader();
                    PlaytimePainter.HideUnityTool();
                }
                painter.gameObject.end();
                return;
            } else {

                if ((IsCurrentTool() && (painter.terrain != null) && (Application.isPlaying == false) && (UnityEditorInternal.InternalEditorUtility.GetIsInspectorExpanded(painter.terrain) == true)) ||
                    (icon.On.Click("Click to Disable Tool", 25))) {
                    PlaytimeToolComponent.enabledTool = null; //customTools.Disabled;
                    MeshManager.Inst.DisconnectMesh();
                    painter.SetOriginalShaderOnThis();
                    painter.UpdateOrSetTexTarget(TexTarget.Texture2D);
                    PlaytimePainter.RestoreUnityTool();
                }
            }

            painter.InitIfNotInited();

            ImageData image = painter.ImgData;

            Texture tex = painter.GetTextureOnMaterial();
            if ((!painter.meshEditing) && ((tex != null) && (image == null)) || ((image != null) && (tex == null)) || ((image != null) && (tex != image.texture2D) && (tex != image.CurrentTexture())))
                painter.textureWasChanged = true;

            painter.PEGI_MAIN();
          

            painter.gameObject.end();
#endif
        }

    }
#endif
        }

