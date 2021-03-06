﻿#if UNITY_EDITOR
using QuizCanners.Inspect;

namespace PlaytimePainter {

    [PEGI_Inspector_Override(typeof(PlaytimePainter))]
    internal class PlaytimePainterClassDrawer : PEGI_Inspector_Override
    {

     
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

