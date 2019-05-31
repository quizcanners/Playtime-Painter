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

        public bool enableNoise = true;
        public Texture2D prerenderedNoiseTexture;
        readonly ShaderProperty.TextureValue _noiseTextureGlobal = new ShaderProperty.TextureValue("_Global_Noise_Lookup");

        void UpdateShaderGlobal()
        {
            enableNoise = enableNoise && prerenderedNoiseTexture;
            UnityUtils.SetShaderKeyword(SHADER_NOISE_TEXTURE, enableNoise);
            _noiseTextureGlobal.SetGlobal(prerenderedNoiseTexture);
        }

        void OnEnable()
        {
            UpdateShaderGlobal();
        }

        #region Inspector
#if !NO_PEGI
        public bool Inspect()
        {
            var changed = false;

            ("This component will set noise texture as a global parameter. Using texture is faster then generating noise in shader.")
                .fullWindowDocumentationClickOpen("About Noise Texture Manager");

            "Noise Texture".edit(90, ref prerenderedNoiseTexture).nl(ref changed);

            if (prerenderedNoiseTexture)
                SHADER_NOISE_TEXTURE.toggleIcon(ref enableNoise).nl(ref changed);

            if (changed)
                UpdateShaderGlobal();

            return changed;
        }
#endif
        #endregion
    }


#if UNITY_EDITOR
    [CustomEditor(typeof(NoiseTextureMGMT))]
    public class NoiseTextureMGMTDrawer : PEGI_Inspector_Mono<NoiseTextureMGMT> { }
#endif

}
