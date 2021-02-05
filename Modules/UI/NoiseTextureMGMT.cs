using PlayerAndEditorGUI;
using QuizCannersUtilities;
using UnityEngine;
using static QuizCannersUtilities.ShaderProperty;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PlaytimePainter
{
    [ExecuteAlways]
    public class NoiseTextureMGMT : MonoBehaviour, IPEGI
    {
        public static NoiseTextureMGMT instance;

        private readonly ShaderKeyword _noiseTexture = new ShaderKeyword("USE_NOISE_TEXTURE");
        private readonly FloatValue _shaderTime = new FloatValue("_qcPp_Taravana_Time");
        private readonly TextureValue _noiseTextureGlobal = new TextureValue("_Global_Noise_Lookup");

        public bool enableNoise = true;
        public Texture2D prerenderedNoiseTexture;
       
        void UpdateShaderGlobal()
        {
            enableNoise = enableNoise && prerenderedNoiseTexture;
            _noiseTexture.Enabled = enableNoise;
            _noiseTextureGlobal.SetGlobal(prerenderedNoiseTexture);
        }

        public void ResetTime() => _shaderTime.GlobalValue = 0;
        void OnEnable() => UpdateShaderGlobal();
        
        void LateUpdate() 
        {
            if (_shaderTime.GlobalValue > 64)
                _shaderTime.GlobalValue = 0;
            else
                _shaderTime.SetGlobal(_shaderTime.latestValue + Time.deltaTime);
        }

        #region Inspector
        public bool Inspect()
        {
            var changed = false;

            pegi.toggleDefaultInspector(this);

            pegi.FullWindow.DocumentationClickOpen("This component will set noise texture as a global parameter. Using texture is faster then generating noise in shader.", "About Noise Texture Manager");

            pegi.nl();

            _noiseTextureGlobal.ToString().edit(90, ref prerenderedNoiseTexture).nl(ref changed);

            if (prerenderedNoiseTexture)
            {
                _noiseTexture.ToString().toggleIcon(ref enableNoise).nl(ref changed);
                if (enableNoise)
                    _noiseTextureGlobal.ToString().write_ForCopy().nl();
            }

            pegi.nl();

            "Custom Time Parameter".write_ForCopy(_shaderTime.ToString());

            pegi.FullWindow.DocumentationClick(
                "Use NoiseTextureMGMT.instance.ResetTime to reset time when all animated shaders are hiddent from the screen." +
                " Alternatively the time will be reset every 64 seconds resulting in noticible jitter");

            pegi.nl();

            if (changed)
                UpdateShaderGlobal();

            return changed;
        }
        #endregion

        void Awake() => instance = this;
    }


#if UNITY_EDITOR
    [CustomEditor(typeof(NoiseTextureMGMT))]
    public class NoiseTextureMGMTDrawer : PEGI_Inspector_Mono<NoiseTextureMGMT> { }
#endif

}
