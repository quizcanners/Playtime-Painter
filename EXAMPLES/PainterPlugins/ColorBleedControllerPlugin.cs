using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using System;
using SharedTools_Stuff;


namespace Playtime_Painter {
    
    public class ColorBleedControllerPlugin : PainterManagerPluginBase  {

        public float eyeBrightness = 1;
        public float colorBleeding = 0.01f;
        public bool modifyBrightness;
        public bool colorBleed;

        public override string NameForPEGIdisplay => "Color Bleed";

        public override void OnEnable() {
            SetStuff();
        }

        public void SetStuff() {
            Shader.SetGlobalVector("_lightControl", new Vector4(colorBleeding, 0, 0, eyeBrightness));
        }

        #region Inspector
#if PEGI
        bool showHint;
        public override bool ConfigTab_PEGI() {
            bool changed = false;

            changed |= "Bleed Colors".toggle(80,ref colorBleed).nl();

            if (colorBleed)
                changed |= pegi.edit(ref colorBleeding, 0.0001f, 0.3f).nl();
            else
                colorBleeding = 0;

            changed |= "Brightness".toggle(80,ref modifyBrightness).nl();

            if (modifyBrightness)
                changed |= pegi.edit(ref eyeBrightness, 0.0001f, 8f).nl();
            else
                eyeBrightness = 1;
            
             pegi.toggle(ref showHint, icon.Close, icon.Hint, "Show hint", 35).nl();

            if (showHint)
                "Is not a postrocess effect. It modifies Global Shader Parameter. Will only be visible on Custom shaders which containt the needed defines.".writeHint();

            bool fog = RenderSettings.fog;
            if ("Fog".toggle(ref fog).nl()) 
                RenderSettings.fog = fog;
            
            if (fog) {
                var col = RenderSettings.fogColor;
                if ("Color".edit(ref col).nl()) 
                    RenderSettings.fogColor = col;
                var mode = RenderSettings.fogMode;
                if (mode != FogMode.ExponentialSquared)
                    "Exponential Squared is recommended".writeHint();
                if ("Mode".editEnum(ref mode).nl().nl())
                    RenderSettings.fogMode = mode;
                float density = RenderSettings.fogDensity;
                if ("Density".edit(60, ref density, 0f, 0.05f).nl())
                    RenderSettings.fogDensity = density;
            }
               

            

            if (changed)  {
                UnityHelperFunctions.RepaintViews();
                SetStuff();
                this.SetToDirty();
            }
            return changed;
        }
#endif
        #endregion
    }
}