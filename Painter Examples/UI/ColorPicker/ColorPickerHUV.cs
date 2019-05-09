using QuizCannersUtilities;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayerAndEditorGUI;
// Note: Some weird constants in script and shader are theme to match Unity's picker.

namespace Playtime_Painter.Examples {

    [ExecuteInEditMode]
    public class ColorPickerHUV : CoordinatePickerBase, IPEGI
    {

        public static ColorPickerHUV instance;
        
        static float Saturation { get { return ColorPickerContrast.Saturation; } set { ColorPickerContrast.Saturation = value; } }

        static float Value { get { return ColorPickerContrast.Value; } set { ColorPickerContrast.Value = value; } }
        
        public List<Graphic> graphicToShowBrushColorRGB = new List<Graphic>();
        
        public List<Graphic> graphicToShowBrushColorRGBA = new List<Graphic>();
        
        public static Color lastValue;

        public static float hue;
      
        public static void UpdateBrushColor() {

            Color col = Color.HSVToRGB(hue, Saturation, Value);

            var bc = PainterCamera.Data.brushConfig;

            col.a = bc.Color.a;

            bc.Color = col;
            
            lastValue = col;

            contrastProperty.GlobalValue = ColorPickerContrast.Saturation;
            brightnessProperty.GlobalValue = ColorPickerContrast.Value;
            huvProperty.GlobalValue = hue;

            if (instance) {
                instance.graphicToShowBrushColorRGB.TrySetColor_RGB(col);
                col.a *= col.a;
                instance.graphicToShowBrushColorRGBA.TrySetColor_RGBA(col);
            }
        }

        static ShaderProperty.FloatValue contrastProperty = new ShaderProperty.FloatValue("_Picker_Contrast");
        static ShaderProperty.FloatValue brightnessProperty = new ShaderProperty.FloatValue("_Picker_Brightness");
        static ShaderProperty.FloatValue huvProperty = new ShaderProperty.FloatValue("_Picker_HUV");

        protected override void Update()
        {
            base.Update();

            if (PainterCamera.Data) {
                var col = PainterCamera.Data.brushConfig.Color;

                if ((!ColorPickerContrast.inst || !ColorPickerContrast.inst.mouseDown) && lastValue.DistanceRgba(col)>0.002f) {
                    
                    float H;
                    float S;
                    float V;
                    Color.RGBToHSV(col, out H, out S, out V);

                    if (!H.IsNaN()) {
                        hue = H;
                        Saturation = S;
                        Value = V;

                        UpdateBrushColor();
                    }
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            instance = this;

        }

        public override bool UpdateFromUV(Vector2 clickUV) {
            var tmp = (((clickUV.YX() - 0.5f * Vector2.one).Angle() + 360) % 360) / 360f;

            if (!tmp.IsNaN()) {
                hue = tmp;
                UpdateBrushColor();
            }
            return true;
        }

#if PEGI
        public bool Inspect()
        {
            var changed = false;

            pegi.toggleDefaultInspector();
            "HUE: {0}".F(hue).nl();
            "Saturateion: {0}".F(Saturation).nl();
            "Value: {0}".F(Saturation).nl();


            return changed;
        }
#endif
    }
}
