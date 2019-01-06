using SharedTools_Stuff;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Playtime_Painter {

    public class ColorPickerHUV : CoordinatePickerBase
    {

        public static float value;

        public float debugValue = 0;

        static float ApplyVeryTrickyColorConversion(float HUEsection) =>
           (ColorPickerContrast.Contrast + Mathf.Pow(Mathf.Clamp01(2 - Mathf.Abs(HUEsection - 2)), 2.2f) * (1-ColorPickerContrast.Contrast)) 
            * ColorPickerContrast.Brightness;
        
        public static void UpdateBrushColor() {
            
            float val = value * 6;

            Color col = Color.black;

            col.r = ApplyVeryTrickyColorConversion(val);

            val = (val + 2) % 6;

            col.g = ApplyVeryTrickyColorConversion(val);

            val = (val + 2) % 6;

            col.b = ApplyVeryTrickyColorConversion(val); 

            PainterCamera.Data.brushConfig.colorLinear.From(col);

            Shader.SetGlobalFloat("_Picker_Contrast", ColorPickerContrast.Contrast);
            Shader.SetGlobalFloat("_Picker_Brightness", ColorPickerContrast.Brightness);
            Shader.SetGlobalFloat("_Picker_HUV", value);

        }

        public override bool UpdateFromUV(Vector2 clickUV) {
            value = (((-(clickUV-0.5f*Vector2.one)).Angle()+360) % 360) / 360f;
            UpdateBrushColor();
            debugValue = value;

            return true;
        }
    }
}
