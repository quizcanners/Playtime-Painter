using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using System;
using UnityEngine.SceneManagement;

using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using QuizCannersUtilities;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Playtime_Painter.Examples
{



    [TaggedType(tag)]
    public class CombinedMapsControllerPlugin : PainterManagerPluginBase, IPEGI
    {
        const string tag = "CmbndMpsCntrl";
        public override string ClassTag => tag;


        public static CombinedMapsControllerPlugin _inst;

        public List<TextureSetForForCombinedMaps> forCombinedMaps = new List<TextureSetForForCombinedMaps>();
        public List<TexturePackagingProfile> texturePackagingSolutions = new List<TexturePackagingProfile>();

        public override void Enable()
        {
            _inst = this;
        }

        public override string NameForPEGIdisplay => "Combined Maps";

        #region Encode & Decode
        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add("cm", forCombinedMaps)
            .Add("tps", texturePackagingSolutions);

        public override bool Decode(string tag, string data)
        {
            switch (tag)
            {
                case "cm": data.Decode_List(out forCombinedMaps); break;
                case "tps": data.Decode_List(out texturePackagingSolutions); break;
            default: return false;
            }
            return true;
        }

        #endregion

        #region Inspector
#if PEGI
        [SerializeField]
        int browsedTextureSet = -1;
        public override bool Inspect() => "Surfaces".edit_List(ref forCombinedMaps, ref browsedTextureSet);
#endif
        #endregion
    }


    [Serializable]
    public class TextureSetForForCombinedMaps : PainterStuffKeepUnrecognized_STD, IGotName, IPEGI {

        protected CombinedMapsControllerPlugin Ctrl { get { return CombinedMapsControllerPlugin._inst; } }
        
        public Texture2D Diffuse;
        public Texture2D HeightMap;
        public Texture2D NormalMap;
        public Texture2D Gloss;
        public Texture2D Reflectivity;
        public Texture2D Ambient;
        public Texture2D LastProduct;

        public int width = 1024;
        public int height = 1024;

        public bool isColor;

        public string name;

        public TexturePackagingProfile Profile { get { return Ctrl.texturePackagingSolutions[selectedProfile]; } }

        public Texture2D GetTexture()
        {
            if (Diffuse) return Diffuse;
            if (HeightMap) return HeightMap;
            if (NormalMap) return NormalMap;
            if (Gloss) return Gloss;
            if (Reflectivity) return Reflectivity;
            if (Ambient) return Ambient;
            if (LastProduct) return LastProduct;
            return null;
        }

        public TextureSetForForCombinedMaps()
        {
            name = "Unnamed";
        }

        public string NameForPEGI { get { return name; } set { name = value; } }

        #region Encode & Decode
        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add_Reference("d", Diffuse)
            .Add_Reference("h", HeightMap)
            .Add_Reference("bump", NormalMap)
            .Add_Reference("g", Gloss)
            .Add_Reference("r", Reflectivity)
            .Add_Reference("ao", Ambient)
            .Add_Reference("lp", LastProduct)
            .Add("tw", width)
            .Add("th", height)
            .Add_Bool("col", isColor)
            .Add_String("n", name)
            .Add("sp", selectedProfile);
        
        public override bool Decode(string tag, string data)
        {
            switch (tag)
            {
                case "d": data.Decode_Reference(ref Diffuse); break;
                case "h": data.Decode_Reference(ref HeightMap); break;
                case "bump": data.Decode_Reference(ref NormalMap); break;
                case "g": data.Decode_Reference(ref Gloss); break;
                case "r":  data.Decode_Reference(ref Reflectivity); break;
                case "ao": data.Decode_Reference(ref Ambient); break;
                case "lp": data.Decode_Reference(ref LastProduct); break;
                case "tw":  width = data.ToInt(); break;
                case "th": height = data.ToInt(); break;
                case "col": isColor = data.ToBool(); break;
                case "n": name = data; break;
                case "sp": selectedProfile = data.ToInt(); break;
                default: return false;
            }
            return true;
        }
        #endregion

        #region Inspect
#if PEGI
        public override bool Inspect()
        {


            bool changed = false;

            var id = InspectedImageData;

            if (InspectedPainter && id != null)
            {
                "Editing:".write(40);
                pegi.write(id.texture2D);
                pegi.nl();
            }

            changed |= "Diffuse".edit("Texture that contains Color of your object. Usually used in _MainTex field.", 70, ref Diffuse).nl();
            changed |= "Height".edit("Greyscale Texture which represents displacement of your surface. Can be used for parallax effect" +
                "or height based terrain blending.", 70, ref HeightMap).nl();
            changed |= "Normal".edit("Noraml map - a pinkish texture which modifies normal vector, adding a sense of relief. Normal can also be " +
                "generated from Height", 70, ref NormalMap).nl();
            changed |= "Gloss".edit("How smooth the surface is. Polished metal - is very smooth, while rubber is usually not.", 70, ref Gloss).nl();
            changed |= "Reflectivity".edit("Best used to add a feel of wear to the surface. Reflectivity blocks some of the incoming light.", 70, ref Reflectivity).nl();
            changed |= "Ambient".edit("Ambient is an approximation of how much light will fail to reach a given segment due to it's indentation in the surface. " +
                "Ambient map may look a bit similar to height map in some cases, but will more clearly outline shapes on the surface.", 70, ref Ambient).nl();
            changed |= "Last Result".edit("Whatever you produce, will be stored here, also it can be reused.", 70, ref LastProduct).nl();

            if (!InspectedPainter)
            {
                var frstTex = GetTexture();
                changed |= "width:".edit(ref width).nl();
                changed |= "height".edit(ref height).nl();
                if (frstTex && "Match Source".Click().nl())
                {
                    width = frstTex.width;
                    height = frstTex.height;
                }

                changed |= "is Color".toggle(ref isColor).nl();
            }


            pegi.select(ref selectedProfile, Ctrl.texturePackagingSolutions);

            if (icon.Add.Click("New Texture Packaging Profile", 25).nl())
            {
                Ctrl.texturePackagingSolutions.AddWithUniqueNameAndIndex();
                selectedProfile = Ctrl.texturePackagingSolutions.Count - 1;
            }

            if ((selectedProfile < Ctrl.texturePackagingSolutions.Count))
                changed |= Ctrl.texturePackagingSolutions[selectedProfile].PEGI(this).nl();

            currentPainter = null;



            return changed;
        }
#endif
        #endregion

        public int selectedProfile = 0;
        public static PlaytimePainter currentPainter;


    }

    [Serializable]
    public class TexturePackagingProfile : PainterStuff_STD, IGotName, IPEGI
    {

        public bool isColor;
        public float bumpStrength = 0.1f;
        public float bumpNoiseInGlossFraction = 0.1f;
        public List<TextureChannel> channel;
        public string name;
        public LinearColor fillColor;
        public bool glossNoiseFromBump;
        public bool glossNoiseFromHeight;

        public string NameForPEGI { get { return name; } set { name = value; } }

        public override string ToString() { return name; }

        public override bool Decode(string tag, string data)
        {
            switch (tag)
            {
                case "ch": data.Decode_List(out channel); break;
                case "c": isColor = data.ToBool(); break;
                case "n": name = data; break;
                case "b": bumpStrength = data.ToFloat(); break;
                case "fc": fillColor = data.ToLinearColor(); break;
                default: return false;
            }
            return true;
        }

        public override StdEncoder Encode()
        {
            var cody = new StdEncoder()

            .Add_IfNotEmpty("ch", channel)
            .Add_Bool("c", isColor)
            .Add_String("n", name)
            .Add("b", bumpStrength)
            .Add("fc", fillColor);

            return cody;
        }

        public const string folderName = "TexSolution";

        /*  public override string GetDefaultTagName()
          {
              return folderName;
          }*/

        public static TexturePackagingProfile currentPEGI;


#if PEGI
        public virtual bool Inspect() => PEGI(null);

        public bool PEGI(TextureSetForForCombinedMaps sets)
        {
            PlaytimePainter p = InspectedPainter;

            pegi.nl();

            currentPEGI = this;

            bool changed = "Name".edit(80, ref name);

            var path = PainterCamera.Data.texturesFolderName + "/" + folderName;

            if (icon.Save.Click("Will save to " + path, 25).nl())
            {
                this.SaveToAssets(path, name);
                UnityHelperFunctions.RefreshAssetDatabase();
                (name + " was saved to " + path).showNotificationIn3D_Views();
            }
            pegi.nl();

            changed |= "Color texture ".toggleIcon(ref isColor).nl();

            bool usingBumpStrength = false;
            bool usingColorSelector = false;
            bool usingGlossMap = false;

            for (int c = 0; c < 4; c++)
            {
                var ch = channel[c];
                changed |= ((ColorChanel)c).getIcon().toggle(ref ch.enabled);

                if (ch.enabled)
                {
                    if ((ch.flip ? "inverted" : "+ 0").Click("Copy as is or invert (1-X)"))
                        ch.flip = !ch.flip;

                    changed |= ch.Nested_Inspect().nl();

                    usingBumpStrength |= ch.Role.UsingBumpStrengthSlider(ch.sourceChannel);
                    usingColorSelector |= ch.Role.UsingColorSelector;
                    usingGlossMap |= ch.Role.GetType() == typeof(TextureRole_Gloss);
                }

                pegi.nl();
            }




            if (usingBumpStrength) changed |= "Bump Strength".edit(ref bumpStrength).nl();
            if (usingColorSelector) changed |= "Color".edit(ref fillColor).nl();
            if (usingGlossMap)
            {

                if ((sets == null || sets.HeightMap) &&
                 "Gloss Mip -= Height Noise".toggle(ref glossNoiseFromHeight).nl(ref changed))
                    glossNoiseFromBump = false;



                if ((sets == null || sets.NormalMap)
                    && "Gloss Mip -= Normal Noise".toggle(ref glossNoiseFromBump).nl(ref changed))
                    glossNoiseFromHeight = false;


                if (glossNoiseFromHeight || glossNoiseFromBump)
                    "Fraction".edit(ref bumpNoiseInGlossFraction, 0f, 40f).nl();

            }


            if (sets != null)
            {
                if ("Combine".Click().nl())
                    Combine(sets, p);

                if (p)
                    "You will still need to press SAVE in Painter to update original texture.".writeHint();
            }

            return changed;
        }

#endif

        void Combine(TextureSetForForCombinedMaps set, PlaytimePainter p)
        {
            TextureRole.Clear();

            int size;
            var id = p?.ImgData;
            Color[] dst;
            Texture2D tex = null;

            if (id != null)
            {
                size = id.width * id.height;
                dst = id.Pixels;
                tex = id.texture2D;
            }
            else
            {
                size = set.width * set.height;
                tex = new Texture2D(set.width, set.height, TextureFormat.ARGB32, true, set.isColor);
                dst = new Color[size];
                tex.wrapMode = TextureWrapMode.Repeat;
                tex.name = set.name;
            }

            var mips = new List<Color[]>();
            for (int i = 1; i < tex.mipmapCount; i++)
                mips.Add(tex.GetPixels(i));

            for (int colChan = 0; colChan < 4; colChan++)
            {
                var c = channel[colChan];
                if (c.enabled)
                {

                    var ch = c.sourceChannel;
                    var col = c.Role.GetPixels(set, id);

                    if (c.flip)
                        for (int i = 0; i < size; i++)
                            dst[i][colChan] = 1 - col[i][ch];
                    else
                        for (int i = 0; i < size; i++)
                            dst[i][colChan] = col[i][ch];


                    var newMips = c.Role.GetMipPixels(set, id);
                    for (int m = 1; m < tex.mipmapCount; m++)
                    {
                        var mlevel = mips[m - 1];
                        var newLevel = newMips[m - 1];


                        if (c.flip)
                            for (int si = 0; si < mlevel.Length; si++)
                                mlevel[si][colChan] = 1 - newLevel[si][colChan];
                        else
                            for (int si = 0; si < mlevel.Length; si++)
                                mlevel[si][colChan] = newLevel[si][colChan];

                        mips[m - 1] = mlevel;
                    }

                }
            }


            for (int i = 1; i < tex.mipmapCount; i++)
                tex.SetPixels(mips[i - 1], i);

            if (id != null)
            {
                id.SetAndApply(false);
                set.LastProduct = tex;
            }
            else
            {
                tex.SetPixels(dst);
                tex.Apply(false, false);
                set.LastProduct = tex;

#if UNITY_EDITOR
                set.LastProduct = tex.SaveTextureAsAsset(TexMGMTdata.texturesFolderName, ref set.name, false);

                TextureImporter importer = set.LastProduct.GetTextureImporter();

                bool needReimport = importer.WasNotReadable();
                needReimport |= importer.WasWrongIsColor(isColor);
                needReimport |= importer.WasClamped();

                if (needReimport) importer.SaveAndReimport();
#endif
            }

            TextureRole.Clear();
        }

        public TexturePackagingProfile()
        {
            channel = new List<TextureChannel>();
            for (int i = 0; i < 4; i++)
                channel.Add(new TextureChannel());

            name = "unnamed";
        }
    }

    [Serializable]
    public class TextureChannel : Abstract_STD, IPEGI
    {
        public bool enabled;
        public bool flip = false;
        public int sourceRole;
        public int sourceChannel;

        public TextureRole Role { get { return TextureRole.All[sourceRole]; } set { sourceRole = value.index; } }

        #region Encode & Decode
        public override bool Decode(string tag, string data)
        {
            switch (tag)
            {
                case "s": sourceRole = data.ToInt(); break;
                case "c": sourceChannel = data.ToInt(); break;
                case "f": flip = data.ToBool(); break;
                default: return false;
            }
            return true;
        }

        public override StdEncoder Encode() => new StdEncoder()
            .Add_IfNotZero("s", sourceRole)
            .Add_IfNotZero("c", sourceChannel)
            .Add_Bool("f", flip);
        

        #endregion
        
        #region Inspect
        #if PEGI

        public virtual bool Inspect()
        {

            bool changed = false;

            if (enabled)
            {

                var rls = TextureRole.All;

                changed |= pegi.select(ref sourceRole, rls);

                // if (!role.productSingleChannel)
                changed |= rls[sourceRole].PEGI(ref sourceChannel, this);
            }
            pegi.newLine();

            return changed;
        }

        #endif
        #endregion

        public const string stdTag = "TexChan";
    }


    public abstract class TextureRole : IGotDisplayName
    {

        static List<TextureRole> _allRoles;
        public static List<TextureRole> All
        {
            get
            {
                if (_allRoles == null)
                {
                    _allRoles = new List<TextureRole> {
                            new TextureRole_Diffuse(0),
                            new TextureRole_Height(1),
                            new TextureRole_Normal(2),
                            new TextureRole_Result(3),
                            new TextureRole_FillColor(4),
                            new TextureRole_Ambient(5),
                            new TextureRole_Gloss(6),
                            new TextureRole_Reflectivity(7)
                        };
                }
                return _allRoles;
            }
        }

        public static void Clear()
        {
            foreach (var r in _allRoles) r.ClearPixels();
        }

        public virtual void ClearPixels()
        {
            _pixels = null;
            mipLevels = null;
        }

        public int index;

        public Color[] _pixels;
        public List<Color[]> mipLevels;

        public TextureRole(int nindex)
        {
            index = nindex;
        }

        protected bool wasNormal;

        public virtual bool UsingBumpStrengthSlider(int sourceChannel) { return false; }
        public virtual bool UsingColorSelector { get { return false; } }
        public virtual bool IsColor { get { return false; } }
        public virtual List<string> Channels { get { return Chanals; } }
        public virtual Color DefaultColor { get { return IsColor ? Color.white : Color.grey; } }

        public abstract string NameForPEGIdisplay { get; }
        static readonly List<string> Chanals = new List<string> { "R", "G", "B", "A" };

        protected void ExtractPixels(Texture2D tex, int width, int height)
        {

            if (tex)
            {
                try
                {

#if UNITY_EDITOR
                    var importer = tex.GetTextureImporter();
                    bool needReimport = importer.WasNotReadable();

                    //needReimport |= importer.wasWrongIsColor(isColor);
                    if (importer.WasMarkedAsNormal())
                    {
                        wasNormal = true;
                        needReimport = true;
                    }
                    else wasNormal = false;

                    //if (sourceSingleChannel)
                    //  needReimport |= importer.wasNotSingleChanel();

                    if (needReimport)
                        importer.SaveAndReimport();
#endif
                    _pixels = tex.GetPixels(width, height);
                }
                catch (Exception ex)
                {
                    Debug.Log("Pixel extraction from " + tex.name + " failed " + ex.ToString());
                    tex = null;
                }
            }

            if (!tex)
            {
                Debug.Log(this.ToString() + " texture not set, using default color.");
                int size = width * height;
                _pixels = new Color[size];
                var col = DefaultColor;
                for (int i = 0; i < size; i++)
                    _pixels[i] = col;
            }

        }

        public virtual Color[] GetPixels(TextureSetForForCombinedMaps set, ImageData id)
        {
            if (_pixels == null)
                ExtractPixels(set.Diffuse, 
                    id == null ? set.width : id.width,
                    id == null ? set.height : id.height);

            return _pixels;
        }

        public virtual List<Color[]> GetMipPixels(TextureSetForForCombinedMaps set, ImageData id)
        {
            if (mipLevels == null)
            {

                int width = id == null ? set.width : id.width;
                int height = id == null ? set.height : id.height;

                mipLevels = new List<Color[]>();



                int w = width;
                int h = height;


                while (w > 1 && h > 1)
                {
                    w /= 2;
                    h /= 2;

                    var dest = new Color[w * h];

                    int dx = width / w;
                    int dy = height / h;

                    float pixelsPerSector = dx * dy;


                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                        {

                            var col = new Color(0, 0, 0, 0);

                            int start = y * dy * width + x * dx;

                            for (int sy = 0; sy < dy; sy++)
                                for (int sx = 0; sx < dx; sx++)
                                    col += _pixels[start + sy * width + sx];

                            col /= pixelsPerSector;

                            dest[y * w + x] = col;
                        }


                    mipLevels.Add(dest);

                }
            }
            return mipLevels;
        }

#if PEGI

        public virtual bool PEGI(ref int selectedChannel, TextureChannel tc)
        {
            bool changed = ".".select(10, ref selectedChannel, Channels).nl();

            return changed;
        }

#endif

    }

    public class TextureRole_Result : TextureRole
    {
        public static TextureRole_Result inst;
        public override string NameForPEGIdisplay => "Result";

        public TextureRole_Result(int index) : base(index)
        {
            inst = this;
        }

        public override Color[] GetPixels(TextureSetForForCombinedMaps set, ImageData id)
        {

            if (id != null)
                return id.Pixels;

            if (_pixels == null)
                ExtractPixels(set.LastProduct, set.width, set.height);

            return _pixels;

        }

    }

    public class TextureRole_FillColor : TextureRole
    {
        public override bool IsColor { get { return true; } }

        public override bool UsingColorSelector { get { return true; } }

        public override string NameForPEGIdisplay => "Fill Color";

        public override Color[] GetPixels(TextureSetForForCombinedMaps set, ImageData id)
        {
          

            if (_pixels == null)
            {
                bool noID = id == null;
                int width = noID ? set.width : id.width;
                int height = noID ? set.height : id.height;

                var col = set.Profile.fillColor.ToGamma();
                var size = width * height;
                _pixels = new Color[size];
                for (int i = 0; i < size; i++)
                    _pixels[i] = col;
            }

            return _pixels;
        }

        public TextureRole_FillColor(int index) : base(index)
        {
        }

    }

    public class TextureRole_Diffuse : TextureRole
    {
        public override bool IsColor { get { return true; } }

        public override string NameForPEGIdisplay => "Color";

        public override Color[] GetPixels(TextureSetForForCombinedMaps set, ImageData id) {
            if (_pixels == null)
                ExtractPixels(set.Diffuse, id == null ? set.width : id.width, id == null ? set.height : id.height);

            return _pixels;
        }
        public TextureRole_Diffuse(int index) : base(index)
        {
        }

    }

    public class TextureRole_Gloss : TextureRole
    {

        public override string NameForPEGIdisplay => "Gloss";

        public override Color[] GetPixels(TextureSetForForCombinedMaps set, ImageData id)
        {
          
            if (_pixels == null)
            {
                int width = id == null ? set.width : id.width;
                int height = id == null ? set.height : id.height;

                ExtractPixels(set.Gloss ? set.Gloss : set.Reflectivity, width, height);

                if (set.Profile.glossNoiseFromHeight && set.HeightMap)
                    GlossMipmapsFromHeightNoise(set.HeightMap, width, height, set.Profile.bumpNoiseInGlossFraction);
            }

            return _pixels;
        }

        public override void ClearPixels()
        {
            base.ClearPixels();
            mipLevels = null;
        }

        public void GlossMipmapsFromHeightNoise(Texture2D heightMap, int width, int height, float strength)
        {
            if (mipLevels != null) return;

            mipLevels = new List<Color[]>();

            var hpix = heightMap.GetPixels(width, height);

            int w = width;
            int h = height;


            while (w > 1 && h > 1)
            {
                w /= 2;
                h /= 2;

                var dest = new Color[w * h];

                int dx = width / w;
                int dy = height / h;

                float pixelsPerSector = dx * dy;

                for (int x = 0; x < w; x++)
                    for (int y = 0; y < h; y++)
                    {

                        float avg = 0;
                        var col = new Color(0, 0, 0, 0);

                        int start = y * dy * width + x * dx;

                        for (int sy = 0; sy < dy; sy++)
                            for (int sx = 0; sx < dx; sx++)
                            {
                                int ind = start + sy * width + sx;
                                avg += hpix[ind].a;
                                col += _pixels[ind];
                            }

                        col /= pixelsPerSector;
                        avg /= pixelsPerSector;

                        float noise = 0;

                        for (int sy = 0; sy < dy; sy++)
                            for (int sx = 0; sx < dx; sx++)
                                noise += Mathf.Abs(hpix[start + sy * width + sx].a - avg);

                        noise /= pixelsPerSector;

                        col.a = Mathf.Clamp(col.a - noise * strength, 0.01f, 0.99f);

                        dest[y * w + x] = col;
                    }


                mipLevels.Add(dest);

            }


        }
        public TextureRole_Gloss(int index) : base(index)
        {
        }
    }

    public class TextureRole_Reflectivity : TextureRole
    {


        // public override bool sourceSingleChannel { get { return true; } }
        //  public override bool productSingleChannel { get { return true; } }

        public override string NameForPEGIdisplay => "Reflectivity";

        public override Color[] GetPixels(TextureSetForForCombinedMaps set, ImageData id)
        {
         
            if (_pixels == null)
            {
                int width = id == null ? set.width : id.width;
                int height = id == null ? set.height : id.height;
                ExtractPixels(set.Reflectivity ? set.Reflectivity : set.Gloss, width, height);
            }
            return _pixels;
        }
        public TextureRole_Reflectivity(int index) : base(index)
        {
        }
    }

    public class TextureRole_Ambient : TextureRole
    {

        public override string NameForPEGIdisplay => "Ambient";

        public override Color[] GetPixels(TextureSetForForCombinedMaps set, ImageData id)
        {
            int width = id == null ? set.width : id.width;
            int height = id == null ? set.height : id.height;
            if (_pixels == null)
                ExtractPixels(set.Ambient ? set.Ambient : set.HeightMap, width, height);

            return _pixels;
        }
        public TextureRole_Ambient(int index) : base(index)
        {
        }
    }

    public class TextureRole_Height : TextureRole
    {
        public override string NameForPEGIdisplay => "Height";
        public override bool UsingBumpStrengthSlider(int channel) { return channel < 2; }

        // public override bool sourceSingleChannel { get { return true; } }

        public override List<string> Channels { get { return Chanals; } }

        static readonly List<string> Chanals = new List<string> { "Normal R", "Normal G", "Height Greyscale", "Height Alpha" };

        static int width;
        static int height;

        static int IndexFrom(int x, int y)
        {

            x %= width;
            if (x < 0) x += width;
            y %= height;
            if (y < 0) y += height;

            return y * width + x;
        }

        public TextureRole_Height(int index) : base(index)
        {
        }

        public override Color[] GetPixels(TextureSetForForCombinedMaps set, ImageData id)
        {
           
            if (_pixels == null)
            {
                width = id == null ? set.width : id.width;
                height = id == null ? set.height : id.height;

                ExtractPixels(set.HeightMap ? set.HeightMap : set.Ambient, width, height);

                float xLeft;
                float xRight;
                float yUp;
                float yDown;

                float yDelta;
                float xDelta;

                float strength = set.Profile.bumpStrength;

                for (int by = 0; by < height; by++)
                {
                    for (int bx = 0; bx < width; bx++)
                    {

                        int dstIndex = IndexFrom(bx, by);

                        var col = _pixels[dstIndex];

                        col.b = col.grayscale;

                        xLeft = _pixels[IndexFrom(bx - 1, by)].a;
                        xRight = _pixels[IndexFrom(bx + 1, by)].a;
                        yUp = _pixels[IndexFrom(bx, by - 1)].a;
                        yDown = _pixels[IndexFrom(bx, by + 1)].a;

                        xDelta = (-xRight + xLeft) * strength;

                        yDelta = (-yDown + yUp) * strength;

                        col.r = Mathf.Clamp01(xDelta * Mathf.Abs(xDelta) + 0.5f);
                        col.g = Mathf.Clamp01(yDelta * Mathf.Abs(yDelta) + 0.5f);

                        _pixels[dstIndex] = col;

                    }
                }
            }

            return _pixels;
        }

    }

    public class TextureRole_Normal : TextureRole
    {
        public override string NameForPEGIdisplay => "Normal";

        public override bool UsingBumpStrengthSlider(int channel) { return true; }

        public bool wasMarkedAsNormal = false;

        public TextureRole_Normal(int index) : base(index)
        {
        }
        
        public override void ClearPixels()
        {
            base.ClearPixels();

#if UNITY_EDITOR
            if (tex)
            {
                var imp = tex.GetTextureImporter();
                if (imp.WasMarkedAsNormal(wasMarkedAsNormal))
                    imp.SaveAndReimport();
            }
#endif
            wasMarkedAsNormal = false;
        }
#if UNITY_EDITOR
        Texture2D tex;
#endif
        public override Color[] GetPixels(TextureSetForForCombinedMaps set, ImageData id)
        {
          
            if (_pixels == null)
            {
                int width = id == null ? set.width : id.width;
                int height = id == null ? set.height : id.height;
                ExtractPixels(set.NormalMap, width, height);
            }
#if UNITY_EDITOR
            tex = set.NormalMap;
#endif
            return _pixels;
        }
    }


}

