using QuizCanners.Inspect;
using UnityEngine;

namespace PlaytimePainter
{
    [CreateAssetMenu(fileName = "Playtime Painter - Blit Mode NAME", menuName = "Playtime Painter/Blit Mode Custom", order = 0)]
    public class PlaytimePainter_BlitModeCustom : ScriptableObject, IPEGI
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



    [PEGI_Inspector_Override(typeof(PlaytimePainter_BlitModeCustom))] internal class BlitModeCustomDrawer : PEGI_Inspector_Override { }

}
