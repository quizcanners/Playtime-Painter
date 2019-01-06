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
    public class ColorPickerContrast : CoordinatePickerBase
    {

        static ColorPickerContrast inst;

        public static float Brightness { get { return inst.uvClick.x; } set { inst.uvClick.x = value; } }

        public static float Contrast { get { return inst.uvClick.y; } set { inst.uvClick.y = value; } }

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