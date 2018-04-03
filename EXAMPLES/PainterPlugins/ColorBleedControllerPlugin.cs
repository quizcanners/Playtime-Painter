using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using System;

namespace Playtime_Painter {
    
#if UNITY_EDITOR
    using UnityEditor;
    [CustomEditor(typeof(ColorBleedControllerPlugin))]
    public class ColorBleedControlsEditor : Editor {
        public override void OnInspectorGUI() {
            ef.start(serializedObject);
            ((ColorBleedControllerPlugin)target).ConfigTab_PEGI();
            ef.end();
        }
    }
#endif

    [Serializable]
    public class ColorBleedControllerPlugin : PainterManagerPluginBase  {

        public float eyeBrightness = 1;
        public float colorBleeding = 0.01f;
        public bool modifyBrightness;
        public bool colorBleed;

        public override string ToString() {
            return "Color Bleed";
        }

        public override void OnEnable() {
            Update();
        }

        public void Update() {
            Shader.SetGlobalVector("_lightControl", new Vector4(colorBleeding, 0, 0, eyeBrightness));
            UnityHelperFunctions.SetKeyword("MODIFY_BRIGHTNESS", modifyBrightness);
            UnityHelperFunctions.SetKeyword("COLOR_BLEED", colorBleed);
        }
        
        bool showHint;

        public override bool ConfigTab_PEGI() {
            bool changed = false;

            changed |= "Bleed Colors".toggle(80,ref colorBleed).nl();

            if (colorBleed)
                changed |= pegi.edit(ref colorBleeding, 0.0001f, 0.3f).nl();

            changed |= "Brightness".toggle(80,ref modifyBrightness).nl();
           
            if (modifyBrightness)
                changed |= pegi.edit(ref eyeBrightness, 0.0001f, 8f).nl();


             pegi.toggle(ref showHint, icon.Close, icon.Hint, "Show hint", 35).nl();

            if (showHint)
                "Is not a postrocess effect. It modifies Global Shader Parameter. Will only be visible on Custom shaders which containt the needed defines.".writeHint();

            if (changed)  {
                pegi.RepaintViews();
                Update();
                this.SetToDirty();
            }
            return changed;
        }
    }
}