using PlayerAndEditorGUI;
using QuizCannersUtilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlaytimePainter
{

    [TaggedType(tag)]
    public class ScreenShotCaptureModule : PainterSystemManagerModuleBase
    {
        const string tag = "ScrSht";

        public override string ClassTag => tag;
        
        public QcUtils.ScreenShootTaker screenShots = new QcUtils.ScreenShootTaker();

        #if !NO_PEGI

        public override string NameForDisplayPEGI => "HD Screen Shots";

        public override bool Inspect()
        {
            var changed = false;

        
            if (!screenShots.cameraToTakeScreenShotFrom && TexMGMT.MainCamera)
                screenShots.cameraToTakeScreenShotFrom = TexMGMT.MainCamera;
            
            screenShots.Nested_Inspect(ref changed);
            
            return changed;
        }


#endif


        }
}
