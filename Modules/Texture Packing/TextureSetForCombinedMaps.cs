using PlayerAndEditorGUI;
using QuizCannersUtilities;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PlaytimePainter.TexturePacking
{
    [CreateAssetMenu(fileName = FILE_NAME, menuName = "Playtime Painter/" + FILE_NAME)]
    public class TextureSetForCombinedMaps : ScriptableObject, IPEGI, IGotName
    {

        public const string FILE_NAME = "Texture Set For Combined Textures";

        public Texture2D diffuse;
        public Texture2D heightMap;
        public Texture2D normalMap;
        public Texture2D gloss;
        public Texture2D reflectivity;
        public Texture2D ambient;
        public Texture2D lastProduct;

        public int width = 1024;
        public int height = 1024;

        public bool isColor;

        protected static PainterDataAndConfig Cfg => PainterCamera.Data;

        public TextureMapCombineProfile Profile => Cfg.texturePackagingSolutions[selectedProfile];

        public Texture2D GetAnyTexture()
        {
            if (diffuse) return diffuse;
            if (heightMap) return heightMap;
            if (normalMap) return normalMap;
            if (gloss) return gloss;
            if (reflectivity) return reflectivity;
            if (ambient) return ambient;
            if (lastProduct) return lastProduct;
            return null;
        }

        public string NameForPEGI { get { return name; } set { name = value; } }

        public int selectedProfile;

        #region Inspect

        public bool Inspect()
        {

            var changed = false;

            "Diffuse".edit("Texture that contains Color of your object. Usually used in _MainTex field.", 70, ref diffuse).nl(ref changed);
            "Height".edit("Greyscale Texture which represents displacement of your surface. Can be used for parallax effect" +
                "or height based terrain blending.", 70, ref heightMap).nl(ref changed);
            "Normal".edit("Normal map - a pinkish texture which modifies normal vector, adding a sense of relief. Normal can also be " +
                "generated from Height", 70, ref normalMap).nl(ref changed);
            "Gloss".edit("How smooth the surface is. Polished metal - is very smooth, while rubber is usually not.", 70, ref gloss).nl(ref changed);
            "Reflectivity".edit("Best used to add a feel of wear to the surface. Reflectivity blocks some of the incoming light.", 70, ref reflectivity).nl(ref changed);
            "Ambient".edit("Ambient is an approximation of how much light will fail to reach a given segment due to it's indentation in the surface. " +
            "Ambient map may look a bit similar to height map in some cases, but will more clearly outline shapes on the surface.", 70, ref ambient).nl(ref changed);
            "Last Result".edit("Whatever you produce, will be stored here, also it can be reused.", 70, ref lastProduct).nl(ref changed);


            var firstTex = GetAnyTexture();
            "width:".edit(ref width).nl(ref changed);
            "height".edit(ref height).nl(ref changed);
            if (firstTex && "Match Source".Click().nl(ref changed))
            {
                width = firstTex.width;
                height = firstTex.height;
            }

            "is Color".toggle(ref isColor).nl(ref changed);


            pegi.select_Index(ref selectedProfile, Cfg.texturePackagingSolutions);

            if (icon.Add.Click("New Texture Packaging Profile").nl())
            {
                QcUtils.AddWithUniqueNameAndIndex(Cfg.texturePackagingSolutions);
                selectedProfile = Cfg.texturePackagingSolutions.Count - 1;
                Cfg.SetToDirty();
            }

            if ((selectedProfile < Cfg.texturePackagingSolutions.Count))
            {
                if (Cfg.texturePackagingSolutions[selectedProfile].Inspect(this).nl(ref changed))
                    Cfg.SetToDirty();
            }

            return changed;
        }

        #endregion
    }

#if UNITY_EDITOR
[CustomEditor(typeof(TextureSetForCombinedMaps))]
public class TextureSetForCombinedMapsDrawer : PEGI_Inspector_SO<TextureSetForCombinedMaps> { }
#endif

}
