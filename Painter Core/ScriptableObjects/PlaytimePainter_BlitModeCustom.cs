using QuizCanners.Inspect;
using UnityEngine;

namespace PainterTool
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

        void IPEGI.Inspect()
        {
            "Blit Mode Shader".ConstL().Edit(ref shader).Nl();

            "Select texture to copy from".PL().ToggleIcon(ref selectSourceTexture).Nl();
            if (selectSourceTexture)
            {
                "Source Texture".PL().Edit(ref sourceTexture).Nl();
            }

            "Using World Space Position".PL().ToggleIcon(ref usingWorldSpacePosition).Nl();

            "Uses brush color".PL().ToggleIcon(ref showColorSliders).Nl();
            "Double Buffer".PL().ToggleIcon(ref doubleBuffer).Nl();
            
            if (!shader)
            {
                "Shader field is not optional".PL().Write_Hint();
            }

        }
    }



    [PEGI_Inspector_Override(typeof(PlaytimePainter_BlitModeCustom))] internal class BlitModeCustomDrawer : PEGI_Inspector_Override { }

}
