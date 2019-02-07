using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;


namespace Playtime_Painter {

    [TaggedType(tag)]
    public class ColorBleedControllerPlugin : PainterManagerPluginBase  {

        const string tag = "ColBleed";
        public override string ClassTag => tag;

        public float eyeBrightness = 1f;
        public float colorBleeding = 0f;
        public bool modifyBrightness = false;
        public bool colorBleed = false;

        ShaderProperty.VectorValue light_Property = new ShaderProperty.VectorValue("_lightControl");

        public void UpdateShader() => light_Property.GlobalValue = new Vector4(colorBleeding, 0, 0, eyeBrightness);
        
        #region Encode & Decode

        public override StdEncoder Encode()
        {
            var cody = this.EncodeUnrecognized();
            if (modifyBrightness)
                cody.Add("br", eyeBrightness);
            if (colorBleed)
                cody.Add("bl", colorBleeding);

            return cody;
        }

        public override bool Decode(string tag, string data) {
            switch (tag) {
                case "br": modifyBrightness = true; eyeBrightness = data.ToFloat(); break;
                case "bl": colorBleed = true; colorBleeding = data.ToFloat(); break;
                default: return false;
            }
            return true;
        }

        public override void Decode(string data)
        {
            base.Decode(data);

            UpdateShader();
        }

        #endregion

        #region Inspector
        public override string NameForPEGIdisplay => "Bleed & Brightness";


#if PEGI
        bool showHint;
        public override bool Inspect() {
            bool changed = false;

            changed |= "Enable".toggleIcon(ref colorBleed, true);

            if (colorBleed)
                changed |= pegi.edit(ref colorBleeding, 0.0001f, 0.3f).nl();
            else
                colorBleeding = 0;
            pegi.nl();

            changed |= "Brightness".toggleIcon(ref modifyBrightness, true);

            if (modifyBrightness)
                changed |= pegi.edit(ref eyeBrightness, 0.0001f, 8f);
            else
                eyeBrightness = 1;

            pegi.nl();
            
        
            bool fog = RenderSettings.fog;
            if ("Fog".toggleIcon(ref fog, true)) 
                RenderSettings.fog = fog;
            
            if (fog) {
                var col = RenderSettings.fogColor;
                if ("Fog Color".edit(ref col).nl()) 
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

            pegi.toggle(ref showHint, icon.Close, icon.Hint, "Show hint", 35).nl();

            if (showHint)
                "This is not a postrocess effect. Color Bleed and Brightness modifies Global Shader Parameter used by Custom shaders included with the asset.".writeHint();



            if (changed)  {
                UnityHelperFunctions.RepaintViews();
                UpdateShader();
                this.SetToDirty_Obj();
            }
            return changed;
        }
#endif
        #endregion
    }
}