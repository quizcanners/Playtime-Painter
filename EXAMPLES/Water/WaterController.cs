using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace Playtime_Painter
{
    [ExecuteInEditMode]
    public class WaterController : ComponentSTD
    {

        //    public Texture2D foamTexture;

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
            Shader.SetGlobalVector("_foamDynamics", new Vector4(thickness, noise, upscale, (300 - thickness)));
            Shader.SetGlobalTexture("_foam_MASK", foamMask);
        }
        
        // Update is called once per frame
        void Update() {

            if ((Application.isPlaying) || (gameObject.IsFocused()))
                MyTime += Time.deltaTime;
            
            foamParameters.x = MyTime;
            foamParameters.y = MyTime * 0.6f;
            foamParameters.z = transform.position.y;
            foamParameters.w = wetAreaHeight;

            Shader.SetGlobalVector("_foamParams", foamParameters);


        }

#if PEGI
        public override bool Inspect() {
            bool changed = base.Inspect();
            if (inspectedStuff == -1)
            {
                changed |= "Foam".edit(70, ref foamMask).nl();
                changed |= "Thickness:".edit(70, ref thickness, 5, 300).nl();
                changed |= "Noise:".edit(50, ref noise, 0, 100).nl();
                changed |= "Upscale:".edit(50, ref upscale, 1, 64).nl();
                changed |= "Wet Area Height:".edit(50, ref wetAreaHeight, 0.1f, 10).nl();
                if (changed)
                {
                    SetFoamDynamics();
                    UnityHelperFunctions.RepaintViews();  // UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                    this.SetToDirty();
                }
            }
            return changed;
        }

    
#endif

        public override StdEncoder Encode() =>this.EncodeUnrecognized();

        public override bool Decode(string tag, string data) => false;
        
    }
}