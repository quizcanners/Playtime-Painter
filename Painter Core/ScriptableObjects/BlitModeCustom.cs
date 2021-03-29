using PlayerAndEditorGUI;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PlaytimePainter
{
    [CreateAssetMenu(fileName = "Playtime Painter - Blit Mode CUSTOM", menuName = "Playtime Painter/Blit Mode Custom", order = 0)]
    public class BlitModeCustom : ScriptableObject, IPEGI
    {
        public bool selectSourceTexture = false;
        public bool showColorSliders = true;
        public bool usingWorldSpacePosition = false;
        public bool doubleBuffer = false;
        public Shader shader;
        public Texture sourceTexture;

        public bool AllSetUp => shader;

        public void Inspect()
        {
            var changed = false;

            "Select texture to copy from".toggleIcon(ref selectSourceTexture).nl(ref changed);
            if (selectSourceTexture)
            {
                "Source Texture".edit(ref sourceTexture).nl(ref changed);
            }

            "Using World Space Position".toggleIcon(ref usingWorldSpacePosition).nl(ref changed);

            "Uses brush color".toggleIcon(ref showColorSliders).nl(ref changed);
            "Blit Mode Shader".edit(ref shader).nl(ref changed);
            "Double Buffer".toggleIcon(ref doubleBuffer).nl(ref changed);
            
            if (!shader)
            {
                "Shader field is not optional".writeHint();
            }

        }
    }


#if UNITY_EDITOR
    [CustomEditor(typeof(BlitModeCustom))]
    public class BlitModeCustomDrawer : PEGI_Inspector_SO<BlitModeCustom> { }
#endif
}
