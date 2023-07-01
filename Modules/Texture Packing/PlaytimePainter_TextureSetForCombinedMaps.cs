using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;

namespace PainterTool.TexturePacking
{
    [CreateAssetMenu(fileName = FILE_NAME, menuName = "Playtime Painter/" + FILE_NAME)]
    internal class PlaytimePainter_TextureSetForCombinedMaps : ScriptableObject, IPEGI, IGotName
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

        public TextureMapCombineProfile Profile => Painter.Data.texturePackagingSolutions[selectedProfile];

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

        public string NameForInspector { get { return name; } set { name = value; } }

        public int selectedProfile;

        #region Inspect

        public void Inspect()
        {

            pegi.Nl();

            "Diffuse".PegiLabel("Texture that contains Color of your object. Usually used in _MainTex field.", 70).Edit( ref diffuse).Nl();
            "Height".PegiLabel("Greyscale Texture which represents displacement of your surface. Can be used for parallax effect" +
                "or height based terrain blending.", 70).Edit(ref heightMap).Nl();
            "Normal".PegiLabel("Normal map - a pinkish texture which modifies normal vector, adding a sense of relief. Normal can also be " +
                "generated from Height", 70).Edit( ref normalMap).Nl();
            "Gloss".PegiLabel("How smooth the surface is. Polished metal - is very smooth, while rubber is usually not.", 70).Edit( ref gloss).Nl();
            "Reflectivity".PegiLabel("Best used to add a feel of wear to the surface. Reflectivity blocks some of the incoming light.", 70).Edit( ref reflectivity).Nl();
            "Ambient".PegiLabel("Ambient is an approximation of how much light will fail to reach a given segment due to it's indentation in the surface. " +
            "Ambient map may look a bit similar to height map in some cases, but will more clearly outline shapes on the surface.", 70).Edit( ref ambient).Nl();
            "Last Result".PegiLabel("Whatever you produce, will be stored here, also it can be reused.", 70).Edit( ref lastProduct).Nl();


            var firstTex = GetAnyTexture();
            "width:".PegiLabel().Edit(ref width).Nl();
            "height".PegiLabel().Edit(ref height).Nl();
            if (firstTex && "Match Source".PegiLabel().Click().Nl())
            {
                width = firstTex.width;
                height = firstTex.height;
            }

            "is Color".PegiLabel().Toggle(ref isColor).Nl();

            if (!Painter.Data) 
            {
                "No Painter Config".PegiLabel().WriteWarning().Nl();
                return;
            }

            pegi.Select_Index(ref selectedProfile, Painter.Data.texturePackagingSolutions);

            if (Icon.Add.Click("New Texture Packaging Profile").Nl())
            {
                QcSharp.AddWithUniqueNameAndIndex(Painter.Data.texturePackagingSolutions);
                selectedProfile = Painter.Data.texturePackagingSolutions.Count - 1;
                Painter.Data.SetToDirty();
            }

            if ((selectedProfile < Painter.Data.texturePackagingSolutions.Count))
            {
                if (pegi.Nested_Inspect(()=> Painter.Data.texturePackagingSolutions[selectedProfile].Inspect(this)).Nl())
                    Painter.Data.SetToDirty();
            }
        }

        #endregion
    }


[PEGI_Inspector_Override(typeof(PlaytimePainter_TextureSetForCombinedMaps))] internal class TextureSetForCombinedMapsDrawer : PEGI_Inspector_Override { }


}
