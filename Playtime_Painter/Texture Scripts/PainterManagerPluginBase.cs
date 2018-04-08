using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;


namespace Playtime_Painter
{

    [System.Serializable]
    public class PainterManagerPluginBase : PainterStuffMono {

        public virtual void OnEnable()  { }

        public virtual bool BrushConfigPEGI() { return false; }

        public virtual bool ConfigTab_PEGI() { return false; }

        public virtual bool Component_PEGI() { return false; }

        public virtual bool PaintTexture2D(StrokeVector stroke, float brushAlpha, imgData image, BrushConfig bc) { return false; }

        public virtual bool needsGrid (PlaytimePainter p) { return false; }

        

    }
}