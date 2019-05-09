using UnityEngine;
using QuizCannersUtilities;

namespace Playtime_Painter.Examples
{
    [ExecuteInEditMode]
    public class ColorPickerContrast : CoordinatePickerBase
    {

        public static ColorPickerContrast inst;

        public static float Value { get { return inst.uvClick.x; } set { inst.uvClick.x = Mathf.Clamp01(value); } }

        public static float Saturation { get { return 1 - inst.uvClick.y; } set { inst.uvClick.y = Mathf.Clamp01(1 -value); } }

        public override bool UpdateFromUV(Vector2 clickUV) {

            clickUV = clickUV.Clamp01();

            ColorPickerHUV.UpdateBrushColor();
            return true;
        }
        
        protected override void OnEnable(){

            base.OnEnable();
            inst = this;
        }

    }
}