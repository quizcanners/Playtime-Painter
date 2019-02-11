using QuizCannersUtilities;
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
            
            var val = value * 6;

            var col = Color.black;

            col.r = ApplyVeryTrickyColorConversion(val);

            val = (val + 2) % 6;

            col.g = ApplyVeryTrickyColorConversion(val);

            val = (val + 2) % 6;

            col.b = ApplyVeryTrickyColorConversion(val); 

            PainterCamera.Data.brushConfig.Color = col;

            contrastProperty.GlobalValue = ColorPickerContrast.Contrast;
            brightnessProperty.GlobalValue = ColorPickerContrast.Brightness;
            huvProperty.GlobalValue = value;

        }

        static ShaderProperty.FloatValue contrastProperty = new ShaderProperty.FloatValue("_Picker_Contrast");
        static ShaderProperty.FloatValue brightnessProperty = new ShaderProperty.FloatValue("_Picker_Brightness");
        static ShaderProperty.FloatValue huvProperty = new ShaderProperty.FloatValue("_Picker_HUV");

        public override bool UpdateFromUV(Vector2 clickUV) {
            value = (((-(clickUV-0.5f*Vector2.one)).Angle()+360) % 360) / 360f;
            UpdateBrushColor();
            debugValue = value;

            return true;
        }
    }
}
