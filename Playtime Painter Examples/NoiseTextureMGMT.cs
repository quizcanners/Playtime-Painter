using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PlaytimePainter.Examples
{

    [ExecuteInEditMode]
    public class NoiseTextureMGMT : MonoBehaviour, IPEGI
    {

        public static string SHADER_NOISE_TEXTURE = "USE_NOISE_TEXTURE";

        private const string NoiseTextureName = "_Global_Noise_Lookup";

        public bool enableNoise = true;
        public Texture2D prerenderedNoiseTexture;
        readonly ShaderProperty.TextureValue _noiseTextureGlobal = new ShaderProperty.TextureValue(NoiseTextureName);

        void UpdateShaderGlobal()
        {
            enableNoise = enableNoise && prerenderedNoiseTexture;
            QcUnity.SetShaderKeyword(SHADER_NOISE_TEXTURE, enableNoise);
            _noiseTextureGlobal.SetGlobal(prerenderedNoiseTexture);
        }

        void OnEnable()
        {
            UpdateShaderGlobal();
        }

        #region Inspector
        public bool Inspect()
        {
            var changed = false;

            pegi.toggleDefaultInspector(this);

            ("This component will set noise texture as a global parameter. Using texture is faster then generating noise in shader.")
                .fullWindowDocumentationClickOpen("About Noise Texture Manager");

            pegi.nl();

            "Noise Texture".edit(90, ref prerenderedNoiseTexture).nl(ref changed);

            if (prerenderedNoiseTexture)
            {
                SHADER_NOISE_TEXTURE.toggleIcon(ref enableNoise).nl(ref changed);
                if (enableNoise)
                    NoiseTextureName.write_ForCopy().nl();
            }

            if (changed)
                UpdateShaderGlobal();

            return changed;
        }
        #endregion
    }


#if UNITY_EDITOR
    [CustomEditor(typeof(NoiseTextureMGMT))]
    public class NoiseTextureMGMTDrawer : PEGI_Inspector_Mono<NoiseTextureMGMT> { }
#endif

}
