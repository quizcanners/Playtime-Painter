using QuizCanners.Inspect;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PlaytimePainter
{
    [CreateAssetMenu(fileName = "Playtime Painter - Blit Mode CUSTOM", menuName = "Playtime Painter/Blit Mode Custom", order = 0)]
    public class BlitModeCustom : ScriptableObject, IPEGI
    {
        public Shader shader;
        [Header("Optional")]
        public Texture sourceTexture;

        [Header("Settings")]
        public bool selectSourceTexture;
        public bool showColorSliders = true;
        public bool usingWorldSpacePosition;
        public bool doubleBuffer;

        public bool AllSetUp => shader;

        public void Inspect()
        {
            pegi.toggleDefaultInspector(this);

            "Blit Mode Shader".edit(110, ref shader).nl();

            "Select texture to copy from".toggleIcon(ref selectSourceTexture).nl();
            if (selectSourceTexture)
            {
                "Source Texture".edit(ref sourceTexture).nl();
            }

            "Using World Space Position".toggleIcon(ref usingWorldSpacePosition).nl();

            "Uses brush color".toggleIcon(ref showColorSliders).nl();
            "Double Buffer".toggleIcon(ref doubleBuffer).nl();
            
            if (!shader)
            {
                "Shader field is not optional".writeHint();
            }

        }
    }


#if UNITY_EDITOR
    [CustomEditor(typeof(BlitModeCustom))] internal class BlitModeCustomDrawer : PEGI_Inspector_Override { }
#endif
}
