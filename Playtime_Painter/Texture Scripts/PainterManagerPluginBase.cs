using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;


namespace Playtime_Painter
{

    [System.Serializable]
    public class PainterManagerPluginBase : PainterStuffMono {

        public virtual void OnEnable()  { }

        public virtual bool BrushConfigPEGI(ref bool overrideBlitModePEGI, BrushConfig br) { return false; }

        public virtual bool ConfigTab_PEGI() { return false; }

        public virtual bool Component_PEGI() { return false; }

        public virtual bool isA3Dbrush(PlaytimePainter pntr, BrushConfig bc, ref bool overrideOther) { return false; }

        public virtual bool PaintTexture2D(StrokeVector stroke, float brushAlpha, ImageData image, BrushConfig bc, PlaytimePainter pntr) { return false; }

        public virtual bool PaintRenderTexture(StrokeVector stroke, ImageData image, BrushConfig bc, PlaytimePainter pntr) {
          
            return false;
        }

        public virtual bool needsGrid (PlaytimePainter p) { return false; }

        public virtual Shader GetPreviewShader(PlaytimePainter p) { return null; }

        public virtual Shader GetBrushShaderDoubleBuffer(PlaytimePainter p) { return null; }

        public virtual Shader GetBrushShaderSingleBuffer(PlaytimePainter p) { return null; }
    }
}