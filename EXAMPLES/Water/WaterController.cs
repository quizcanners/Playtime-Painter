using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace Playtime_Painter.Examples
{
    [ExecuteInEditMode]
    public class WaterController : ComponentSTD
    {

        ShaderProperty.VectorValue foamDynamics_Property = new ShaderProperty.VectorValue("_foamDynamics");
        ShaderProperty.VectorValue foamParameters_Property = new ShaderProperty.VectorValue("_foamParams");
        ShaderProperty.TextureValue foamTexture_Property = new ShaderProperty.TextureValue("_foam_MASK");

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
        float MyTime = 0;
        public float thickness;
        public float noise;
        public float upscale;
        public float wetAreaHeight;
       
        public void SetFoamDynamics() {
            foamDynamics_Property.GlobalValue = new Vector4(thickness, noise, upscale, (300 - thickness));
            foamTexture_Property.GlobalValue = foamMask;
        }
        
        void Update() {

            if ((Application.isPlaying) || (gameObject.IsFocused()))
                MyTime += Time.deltaTime;
            
            foamParameters.x = MyTime;
            foamParameters.y = MyTime * 0.6f;
            foamParameters.z = transform.position.y;
            foamParameters.w = wetAreaHeight;

            foamParameters_Property.GlobalValue = foamParameters;
        }

        #region Inspector
        #if PEGI
        public override bool Inspect() {
            bool changed = base.Inspect();
            if (inspectedStuff == -1)
            {
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
            }
            return changed;
        }


        #endif
        #endregion

        public override StdEncoder Encode() =>this.EncodeUnrecognized();

        public override bool Decode(string tag, string data) => false;
        
    }
}