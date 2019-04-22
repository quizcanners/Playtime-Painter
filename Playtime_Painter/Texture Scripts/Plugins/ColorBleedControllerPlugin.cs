using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;


namespace Playtime_Painter {

    [TaggedType(tag)]
    public class ColorBleedControllerPlugin : PainterSystemManagerPluginBase  {

        const string tag = "ColBleed";
        public override string ClassTag => tag;

        private float _eyeBrightness = 1f;
        private float _colorBleeding;
        private bool _modifyBrightness;
        private bool _colorBleed;

        private readonly ShaderProperty.VectorValue _lightProperty = new ShaderProperty.VectorValue("_lightControl");

        private void UpdateShader() => _lightProperty.GlobalValue = new Vector4(_colorBleeding, 0, 0, _eyeBrightness);
        
        #region Encode & Decode

        public override CfgEncoder Encode()
        {
            var cody = this.EncodeUnrecognized();
            if (_modifyBrightness)
                cody.Add("br", _eyeBrightness);
            if (_colorBleed)
                cody.Add("bl", _colorBleeding);

            return cody;
        }

        public override bool Decode(string tg, string data) {
            switch (tg) {
                case "br": _modifyBrightness = true; _eyeBrightness = data.ToFloat(); break;
                case "bl": _colorBleed = true; _colorBleeding = data.ToFloat(); break;
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
        public override string NameForDisplayPEGI => "Bleed & Brightness";


#if PEGI

        public override string ToolTip =>
            "This is not a postprocess effect. Color Bleed and Brightness modifies Global Shader Parameter used by Custom shaders included with the asset.";            
        
        public override bool Inspect() {
            var changed = false;

            "Color Bleed".toggleIcon(ref _colorBleed, true).changes(ref changed);

            if (_colorBleed)
                "Bleed".edit(60, ref _colorBleeding, 0.0001f, 0.3f).nl(ref changed);
            else
                _colorBleeding = 0;
            
            pegi.nl();

            "Brightness".toggleIcon(ref _modifyBrightness, true).changes(ref changed);

            if (_modifyBrightness)
                "Brightness".edit(90, ref _eyeBrightness, 0.0001f, 8f).changes(ref changed);
            else
                _eyeBrightness = 1;

            pegi.nl();
            
            if (changed)  {
                UnityUtils.RepaintViews();
                UpdateShader();
                this.SetToDirty_Obj();
            }
            return changed;
        }
#endif
        #endregion
    }
}