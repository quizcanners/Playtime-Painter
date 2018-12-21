using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace Playtime_Painter
{

#if PEGI

    public static class PainterPEGI_Extensions
    {

        static PainterDataAndConfig Cfg { get { return PainterCamera.Data; } }
        static PainterCamera TexMGMT { get { return PainterCamera.Inst; } }



    }
#endif
}