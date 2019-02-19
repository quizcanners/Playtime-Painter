using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace Playtime_Painter.Examples
{
    [ExecuteInEditMode]
    public class WaterController : ComponentStd
    {
        private readonly ShaderProperty.VectorValue _foamDynamicsProperty = new ShaderProperty.VectorValue("_foamDynamics");
        private readonly ShaderProperty.VectorValue _foamParametersProperty = new ShaderProperty.VectorValue("_foamParams");
        private readonly ShaderProperty.TextureValue _foamTextureProperty = new ShaderProperty.TextureValue("_foam_MASK");

        private void OnEnable()
        {

            SetFoamDynamics();
            Shader.EnableKeyword("WATER_FOAM");
        }

        private void OnDisable()
        {
            Shader.DisableKeyword("WATER_FOAM");
        }

        public Texture foamMask;

        public Vector4 foamParameters;
        private float _myTime = 0;
        public float thickness;
        public float noise;
        public float upscale;
        public float wetAreaHeight;

        private void SetFoamDynamics() {
            _foamDynamicsProperty.GlobalValue = new Vector4(thickness, noise, upscale, (300 - thickness));
            _foamTextureProperty.GlobalValue = foamMask;
        }

        private void Update() {

            if ((Application.isPlaying) || (gameObject.IsFocused()))
                _myTime += Time.deltaTime;
            
            foamParameters.x = _myTime;
            foamParameters.y = _myTime * 0.6f;
            foamParameters.z = transform.position.y;
            foamParameters.w = wetAreaHeight;

            _foamParametersProperty.GlobalValue = foamParameters;
        }

        #region Inspector
        #if PEGI
        public override bool Inspect() {
            var changed = base.Inspect();
            if (inspectedStuff != -1) return changed;
            
            "Foam".edit(70, ref foamMask).nl(ref changed);
            "Thickness:".edit(70, ref thickness, 5, 300).nl(ref changed);
            "Noise:".edit(50, ref noise, 0, 100).nl(ref changed);
            "Upscale:".edit(50, ref upscale, 1, 64).nl(ref changed);
            "Wet Area Height:".edit(50, ref wetAreaHeight, 0.1f, 10).nl(ref changed);

            if (changed) {
                SetFoamDynamics();
                UnityHelperFunctions.RepaintViews(); 
                this.SetToDirty();
            }
            
            return changed;
        }


        #endif
        #endregion

        public override StdEncoder Encode() =>this.EncodeUnrecognized();

        public override bool Decode(string tg, string data) => false;
        
    }
}