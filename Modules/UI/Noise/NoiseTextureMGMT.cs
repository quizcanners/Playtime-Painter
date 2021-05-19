using QuizCanners.Inspect;
using UnityEngine;
using static QuizCanners.Utils.ShaderProperty;

namespace PlaytimePainter
{
    [ExecuteAlways]
    public class NoiseTextureMGMT : MonoBehaviour, IPEGI
    {
        public static NoiseTextureMGMT instance;

        private readonly ShaderKeyword _noiseTexture = new ShaderKeyword("USE_NOISE_TEXTURE");
        private readonly TextureValue _noiseTextureGlobal = new TextureValue("_Global_Noise_Lookup");

        public bool enableNoise = true;
        public Texture2D prerenderedNoiseTexture;

        private void UpdateShaderGlobal()
        {
            enableNoise = enableNoise && prerenderedNoiseTexture;
            _noiseTexture.Enabled = enableNoise;
            _noiseTextureGlobal.SetGlobal(prerenderedNoiseTexture);
        }

      
        private void OnEnable() => UpdateShaderGlobal();

      

        #region Inspector
        public void Inspect()
        {
            var changed = pegi.ChangeTrackStart();

            pegi.FullWindow.DocumentationClickOpen("This component will set noise texture as a global parameter. Using texture is faster then generating noise in shader.", "About Noise Texture Manager");

            pegi.nl();

            "Noise Tex".edit(120, ref prerenderedNoiseTexture).nl();

            if (prerenderedNoiseTexture)
                _noiseTexture.ToString().toggleIcon(ref enableNoise, hideTextWhenTrue: true);


            if (enableNoise)
            {
                "Compile Directive and Global Texture:".nl();

                _noiseTexture.ToString().write_ForCopy(showCopyButton: true).nl();
                _noiseTextureGlobal.ToString().write_ForCopy(showCopyButton: true);
            }
            pegi.nl();

            if (changed)
                UpdateShaderGlobal();
        }
        #endregion

        private void Awake() => instance = this;
    }



    [PEGI_Inspector_Override(typeof(NoiseTextureMGMT))] internal class NoiseTextureMGMTDrawer : PEGI_Inspector_Override { }


}
