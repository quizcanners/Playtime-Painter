#if UNITY_EDITOR
using PlayerAndEditorGUI;
using UnityEditor;

namespace PlaytimePainter {

    [CustomEditor(typeof(PlaytimePainter))]
    public class PlaytimePainterClassDrawer : PEGI_Inspector_Mono<PlaytimePainter> {

     
        public virtual void OnSceneGUI() {

#if !UNITY_2019_1_OR_NEWER
           
            var p = PlaytimePainterSceneViewEditor.painter;

            if (PlaytimePainter.IsCurrentTool && p && !UnityEditorInternal.InternalEditorUtility.GetIsInspectorExpanded(p))
                PlaytimePainter.IsCurrentTool = false;

            PlaytimePainterSceneViewEditor.OnSceneGuiCombined();
             
#endif

        }



        public virtual void OnEnable() => PlaytimePainterSceneViewEditor.navigating = true;
        
        public override void OnInspectorGUI()
        {
            PlaytimePainterSceneViewEditor.painter = (PlaytimePainter)target;

            base.OnInspectorGUI();
        }

    }

}

#endif

