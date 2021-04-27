#if UNITY_EDITOR
using QuizCanners.Inspect;

namespace PainterTool {

    [PEGI_Inspector_Override(typeof(PainterComponent))]
    internal class PlaytimePainterClassDrawer : PEGI_Inspector_Override
    {

     /*
        public virtual void OnSceneGUI() {

#if !UNITY_2019_1_OR_NEWER
           
            var p = PlaytimePainterSceneViewEditor.painter;

            if (PlaytimePainter.IsCurrentTool && p && !UnityEditorInternal.InternalEditorUtility.GetIsInspectorExpanded(p))
                PlaytimePainter.IsCurrentTool = false;

            PlaytimePainterSceneViewEditor.OnSceneGuiCombined();
             
#endif

        }*/



        public virtual void OnEnable() => PlaytimePainterSceneViewEditor.navigating = true;
        
        public override void OnInspectorGUI()
        {
            PlaytimePainterSceneViewEditor.painter = (PainterComponent)target;

            base.OnInspectorGUI();
        }

    }

}

#endif

