using SharedTools_Stuff;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Playtime_Painter {

    public class ColorPickerHUV : MonoBehaviour, IPointerDownHandler {

        public static float value;
        
        static float ApplyVeryTrickyColorConversion(float HUEsection) =>
           (ColorPickerContrast.Contrast + Mathf.Pow(Mathf.Clamp01(2 - Mathf.Abs(HUEsection - 2)), 2.2f) * (1-ColorPickerContrast.Contrast)) 
            * ColorPickerContrast.Brightness;
        

        public static void UpdateBrushColor() {
            
            float val = value * 6;

            Color col = Color.black;

            col.r = ApplyVeryTrickyColorConversion(val);//Mathf.Pow(Mathf.Clamp01(2 - Mathf.Abs(val - 2)), 2.2f);

            val = (val + 2) % 6;

            col.g = ApplyVeryTrickyColorConversion(val);// Mathf.Pow(Mathf.Clamp01(2 - Mathf.Abs(val - 2)), 2.2f);

            val = (val + 2) % 6;

            col.b = ApplyVeryTrickyColorConversion(val); // Mathf.Pow(Mathf.Clamp01(2 - Mathf.Abs(val - 2)), 2.2f);

            // var xy = ColorPickerContrast.value;

            // col.rgb = i.texcoord.y + col.rgb * (1 - i.texcoord.y);

            // col.rgb *= i.texcoord.x;

            PainterCamera.Data.brushConfig.colorLinear.From(col);

        }


        public RectTransform rectTransform;

        void OnEnable() {
            if (!rectTransform)
                rectTransform = GetComponent<RectTransform>();
        }

        public void OnPointerDown(PointerEventData eventData) {
            Vector2 localCursor;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out localCursor))
                return;

            float hw = rectTransform.rect.width * 0.5f;

            value = (localCursor.Angle() % 360) / 360f;

            UpdateBrushColor();

        }
    }
}
