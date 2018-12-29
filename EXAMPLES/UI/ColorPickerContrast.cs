using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace Playtime_Painter
{
    [ExecuteInEditMode]
    public class ColorPickerContrast : MonoBehaviour, IPointerDownHandler
    {
        public static Vector2 uvClick;

        public static float Brightness { get { return uvClick.x; } set { uvClick.x = value; } }

        public static float Contrast { get { return uvClick.y; } set { uvClick.y = value; } }

        public RectTransform rectTransform;

        void OnEnable(){
            if (!rectTransform)
                rectTransform = GetComponent<RectTransform>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Vector2 localCursor;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out localCursor))
                return;

            uvClick = (localCursor / rectTransform.rect.size) + Vector2.one * 0.5f;


            Debug.Log("Clicked on {0}".F(uvClick));

          
            ColorPickerHUV.UpdateBrushColor();

            //float hw = rectTransform.rect.width * 0.5f;

            //  if (localCursor.magnitude > hw)
            //    Debug.Log("Clicked outside the area");

            //var angle = (localCursor.Angle() % 360) / 360f;

        }
    }
}