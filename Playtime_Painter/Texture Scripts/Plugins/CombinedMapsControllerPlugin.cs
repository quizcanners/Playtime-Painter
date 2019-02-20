using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using System;
using System.IO;
using QuizCannersUtilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Playtime_Painter
{

    [TaggedType(Tag)]
    public class CombinedMapsControllerPlugin : PainterManagerPluginBase
    {
        private const string Tag = "CmbndMpsCntrl";
        public override string ClassTag => Tag;


        public static CombinedMapsControllerPlugin _inst;

        private List<TextureSetForForCombinedMaps> _forCombinedMaps = new List<TextureSetForForCombinedMaps>();
        public List<TexturePackagingProfile> texturePackagingSolutions = new List<TexturePackagingProfile>();

        public override void Enable()
        {
            _inst = this;
        }

        public override string NameForDisplayPEGI => "Combined Maps";

        #region Encode & Decode
        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add("cm", _forCombinedMaps)
            .Add("tps", texturePackagingSolutions);

        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "cm": data.Decode_List(out _forCombinedMaps); break;
                case "tps": data.Decode_List(out texturePackagingSolutions); break;
            default: return false;
            }
            return true;
        }

        #endregion

        #region Inspector
        #if PEGI

        private int _browsedTextureSet = -1;
        public override bool Inspect() => "Surfaces".edit_List(ref _forCombinedMaps, ref _browsedTextureSet);
        #endif
        #endregion
    }


    [Serializable]
    public class TextureSetForForCombinedMaps : PainterStuffKeepUnrecognized_STD, IGotName {

        protected static CombinedMapsControllerPlugin Ctrl => CombinedMapsControllerPlugin._inst;

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

        public TexturePackagingProfile Profile => Ctrl.texturePackagingSolutions[selectedProfile];

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
        
        public override bool Decode(string tg, string data)
        {
            switch (tg)
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


            var changed = false;

            var id = InspectedImageMeta;

            if (InspectedPainter && id != null)
            {
                "Editing:".write(40);
                pegi.write(id.texture2D);
                pegi.nl();
            }

            "Diffuse".edit("Texture that contains Color of your object. Usually used in _MainTex field.", 70, ref Diffuse).nl(ref changed);
            "Height".edit("Greyscale Texture which represents displacement of your surface. Can be used for parallax effect" +
                "or height based terrain blending.", 70, ref HeightMap).nl(ref changed);
            "Normal".edit("Noramal map - a pinkish texture which modifies normal vector, adding a sense of relief. Normal can also be " +
                "generated from Height", 70, ref NormalMap).nl(ref changed);
            "Gloss".edit("How smooth the surface is. Polished metal - is very smooth, while rubber is usually not.", 70, ref Gloss).nl(ref changed);
            "Reflectivity".edit("Best used to add a feel of wear to the surface. Reflectivity blocks some of the incoming light.", 70, ref Reflectivity).nl(ref changed);
            "Ambient".edit("Ambient is an approximation of how much light will fail to reach a given segment due to it's indentation in the surface. " +
                "Ambient map may look a bit similar to height map in some cases, but will more clearly outline shapes on the surface.", 70, ref Ambient).nl(ref changed);
            "Last Result".edit("Whatever you produce, will be stored here, also it can be reused.", 70, ref LastProduct).nl(ref changed);

            if (!InspectedPainter)
            {
                var firstTex = GetTexture();
                "width:".edit(ref width).nl(ref changed);
                "height".edit(ref height).nl(ref changed);
                if (firstTex && "Match Source".Click().nl(ref changed))
                {
                    width = firstTex.width;
                    height = firstTex.height;
                }

                "is Color".toggle(ref isColor).nl(ref changed);
            }


            pegi.select(ref selectedProfile, Ctrl.texturePackagingSolutions);

            if (icon.Add.Click("New Texture Packaging Profile", 25).nl())
            {
                Ctrl.texturePackagingSolutions.AddWithUniqueNameAndIndex();
                selectedProfile = Ctrl.texturePackagingSolutions.Count - 1;
            }

            if ((selectedProfile < Ctrl.texturePackagingSolutions.Count))
                changed |= Ctrl.texturePackagingSolutions[selectedProfile].PEGI(this).nl();

            return changed;
        }
#endif
        #endregion

        public int selectedProfile = 0;


    }

    public class TexturePackagingProfile : PainterStuffStd, IGotName, IPEGI
    {
        private bool _isColor;
        public float bumpStrength = 0.1f;
        public float bumpNoiseInGlossFraction = 0.1f;
        private List<TextureChannel> _channel;
        private string name;
        public LinearColor fillColor;
        private bool _glossNoiseFromBump;
        public bool glossNoiseFromHeight;

        public string NameForPEGI { get { return name; } set { name = value; } }

        public override string ToString() { return name; }

        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "ch": data.Decode_List(out _channel); break;
                case "c": _isColor = data.ToBool(); break;
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

            .Add_IfNotEmpty("ch", _channel)
            .Add_Bool("c", _isColor)
            .Add_String("n", name)
            .Add("b", bumpStrength)
            .Add("fc", fillColor);

            return cody;
        }

        private const string FolderName = "TexSolution";


#if PEGI
        public virtual bool Inspect() => PEGI(null);

        public bool PEGI(TextureSetForForCombinedMaps sets)
        {
            var p = InspectedPainter;

            pegi.nl();

            var changed = "Name".edit(80, ref name);

            var path = Path.Combine(PainterCamera.Data.texturesFolderName, FolderName);

            if (icon.Save.Click("Will save to " + path, 25).nl())
            {
                this.SaveToAssets(path, name);
                UnityHelperFunctions.RefreshAssetDatabase();
                (name + " was saved to " + path).showNotificationIn3D_Views();
            }
            pegi.nl();

            changed |= "Color texture ".toggleIcon(ref _isColor).nl(ref changed);

            var usingBumpStrength = false;
            var usingColorSelector = false;
            var usingGlossMap = false;

            for (var c = 0; c < 4; c++)
            {
                var ch = _channel[c];
                changed |= ((ColorChanel)c).GetIcon().toggle(ref ch.enabled);

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

            if (usingBumpStrength) "Bump Strength".edit(ref bumpStrength).nl(ref changed);
            if (usingColorSelector) "Color".edit(ref fillColor).nl(ref changed);
            if (usingGlossMap)
            {

                if ((sets == null || sets.HeightMap) &&
                 "Gloss Mip -= Height Noise".toggle(ref glossNoiseFromHeight).nl(ref changed))
                    _glossNoiseFromBump = false;



                if ((sets == null || sets.NormalMap)
                    && "Gloss Mip -= Normal Noise".toggle(ref _glossNoiseFromBump).nl(ref changed))
                    glossNoiseFromHeight = false;


                if (glossNoiseFromHeight || _glossNoiseFromBump)
                    "Fraction".edit(ref bumpNoiseInGlossFraction, 0f, 40f).nl();

            }


            if (sets != null)
            {
                if ("Combine".Click().nl(ref changed))
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
            var id = p?.ImgMeta;
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
            for (var i = 1; i < tex.mipmapCount; i++)
                mips.Add(tex.GetPixels(i));

            for (var colChan = 0; colChan < 4; colChan++)
            {
                var c = _channel[colChan];
                if (!c.enabled) continue;
                
                var ch = c.sourceChannel;
                var col = c.Role.GetPixels(set, id);

                if (c.flip)
                    for (var i = 0; i < size; i++)
                        dst[i][colChan] = 1 - col[i][ch];
                else
                    for (var i = 0; i < size; i++)
                        dst[i][colChan] = col[i][ch];


                var newMips = c.Role.GetMipPixels(set, id);
                for (var m = 1; m < tex.mipmapCount; m++)
                {
                    var mLevel = mips[m - 1];
                    var newLevel = newMips[m - 1];


                    if (c.flip)
                        for (var si = 0; si < mLevel.Length; si++)
                            mLevel[si][colChan] = 1 - newLevel[si][colChan];
                    else
                        for (var si = 0; si < mLevel.Length; si++)
                            mLevel[si][colChan] = newLevel[si][colChan];

                    mips[m - 1] = mLevel;
                }
            }


            for (var i = 1; i < tex.mipmapCount; i++)
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

                var importer = set.LastProduct.GetTextureImporter();

                var needReimport = importer.WasNotReadable();
                needReimport |= importer.WasWrongIsColor(_isColor);
                needReimport |= importer.WasClamped();

                if (needReimport) importer.SaveAndReimport();
#endif
            }

            TextureRole.Clear();
        }

        public TexturePackagingProfile()
        {
            _channel = new List<TextureChannel>();
            for (var i = 0; i < 4; i++)
                _channel.Add(new TextureChannel());

            name = "unnamed";
        }
    }

    public class TextureChannel : AbstractStd, IPEGI
    {
        public bool enabled;
        public bool flip;
        private int _sourceRole;
        public int sourceChannel;

        public TextureRole Role { get { return TextureRole.All[_sourceRole]; } set { _sourceRole = value.index; } }

        #region Encode & Decode
        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "s": _sourceRole = data.ToInt(); break;
                case "c": sourceChannel = data.ToInt(); break;
                case "f": flip = data.ToBool(); break;
                default: return false;
            }
            return true;
        }

        public override StdEncoder Encode() => new StdEncoder()
            .Add_IfNotZero("s", _sourceRole)
            .Add_IfNotZero("c", sourceChannel)
            .Add_Bool("f", flip);
        

        #endregion
        
        #region Inspect
        #if PEGI

        public virtual bool Inspect()
        {

            var changed = false;

            if (enabled)
            {

                var rls = TextureRole.All;

                pegi.select(ref _sourceRole, rls).changes(ref changed);

                rls[_sourceRole].PEGI(ref sourceChannel, this).changes(ref changed);
            }
            pegi.newLine();

            return changed;
        }

        #endif
        #endregion

        public const string StdTag = "TexChan";
    }


    public abstract class TextureRole : IGotDisplayName
    {
        private static List<TextureRole> _allRoles;
        public static List<TextureRole> All =>
            _allRoles ?? (_allRoles = new List<TextureRole>
            {
                new TextureRoleDiffuse(0),
                new TextureRoleHeight(1),
                new TextureRole_Normal(2),
                new TextureRoleResult(3),
                new TextureRole_FillColor(4),
                new TextureRoleAmbient(5),
                new TextureRole_Gloss(6),
                new TextureRoleReflectivity(7)
            });

        public static void Clear()
        {
            foreach (var r in _allRoles) r.ClearPixels();
        }

        protected virtual void ClearPixels()
        {
            pixels = null;
            mipLevels = null;
        }

        public readonly int index;

        protected Color[] pixels;
        protected List<Color[]> mipLevels;

        protected TextureRole(int nIndex)
        {
            index = nIndex;
        }


        public virtual bool UsingBumpStrengthSlider(int sourceChannel) => false; 
        public virtual bool UsingColorSelector => false;
        protected virtual bool IsColor => false;
        protected virtual List<string> Channels => Chennels;
        protected virtual Color DefaultColor => IsColor ? Color.white : Color.grey;

        public abstract string NameForDisplayPEGI { get; }
        static readonly List<string> Chennels = new List<string> { "R", "G", "B", "A" };

        protected void ExtractPixels(Texture2D tex, int width, int height)
        {

            if (tex)
            {
                try
                {

#if UNITY_EDITOR
                    var importer = tex.GetTextureImporter();
                    var needReimport = importer.WasNotReadable() || importer.WasMarkedAsNormal();


                    if (needReimport)
                        importer.SaveAndReimport();
#endif
                    pixels = tex.GetPixels(width, height);
                }
                catch (Exception ex)
                {
                    Debug.Log("Pixel extraction from " + tex.name + " failed " + ex.ToString());
                    tex = null;
                }
            }

            if (tex) return;
            
            Debug.Log(this.ToString() + " texture not set, using default color.");
            var size = width * height;
            pixels = new Color[size];
            var col = DefaultColor;
            for (var i = 0; i < size; i++)
                pixels[i] = col;

        }

        public virtual Color[] GetPixels(TextureSetForForCombinedMaps set, ImageMeta id)
        {
            if (pixels == null)
                ExtractPixels(set.Diffuse, 
                    id?.width ?? set.width,
                    id?.height ?? set.height);

            return pixels;
        }

        public List<Color[]> GetMipPixels(TextureSetForForCombinedMaps set, ImageMeta id)
        {
            if (mipLevels != null) return mipLevels;
            
            var width = id?.width ?? set.width;
            var height = id?.height ?? set.height;

            mipLevels = new List<Color[]>();



            var w = width;
            var h = height;


            while (w > 1 && h > 1)
            {
                w /= 2;
                h /= 2;

                var dest = new Color[w * h];

                var dx = width / w;
                var dy = height / h;

                float pixelsPerSector = dx * dy;


                for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                {

                    var col = new Color(0, 0, 0, 0);

                    var start = y * dy * width + x * dx;

                    for (var sy = 0; sy < dy; sy++)
                    for (var sx = 0; sx < dx; sx++)
                        col += pixels[start + sy * width + sx];

                    col /= pixelsPerSector;

                    dest[y * w + x] = col;
                }


                mipLevels.Add(dest);

            }
            return mipLevels;
        }

#if PEGI

        public bool PEGI(ref int selectedChannel, TextureChannel tc)
        {
            var changed = ".".select(10, ref selectedChannel, Channels).nl();

            return changed;
        }

#endif

    }

    public class TextureRoleResult : TextureRole
    {
        private static TextureRoleResult _inst;
        public override string NameForDisplayPEGI => "Result";

        public TextureRoleResult(int index) : base(index)
        {
            _inst = this;
        }

        public override Color[] GetPixels(TextureSetForForCombinedMaps set, ImageMeta id)
        {

            if (id != null)
                return id.Pixels;

            if (pixels == null)
                ExtractPixels(set.LastProduct, set.width, set.height);

            return pixels;

        }

    }

    public class TextureRole_FillColor : TextureRole
    {
        protected override bool IsColor => true;

        public override bool UsingColorSelector => true;

        public override string NameForDisplayPEGI => "Fill Color";

        public override Color[] GetPixels(TextureSetForForCombinedMaps set, ImageMeta id)
        {
            if (pixels != null) return pixels;
            
            var noID = id == null;
            var width = noID ? set.width : id.width;
            var height = noID ? set.height : id.height;

            var col = set.Profile.fillColor.ToGamma();
            var size = width * height;
            pixels = new Color[size];
            for (var i = 0; i < size; i++)
                pixels[i] = col;

            return pixels;
        }

        public TextureRole_FillColor(int index) : base(index)
        {
        }

    }

    public class TextureRoleDiffuse : TextureRole
    {
        protected override bool IsColor => true;

        public override string NameForDisplayPEGI => "Color";

        public override Color[] GetPixels(TextureSetForForCombinedMaps set, ImageMeta id) {
            if (pixels == null)
                ExtractPixels(set.Diffuse, id?.width ?? set.width, id?.height ?? set.height);

            return pixels;
        }
        public TextureRoleDiffuse(int index) : base(index)
        {
        }

    }

    public class TextureRole_Gloss : TextureRole
    {

        public override string NameForDisplayPEGI => "Gloss";

        public override Color[] GetPixels(TextureSetForForCombinedMaps set, ImageMeta id)
        {
            if (pixels != null) return pixels;
            
            var width = id?.width ?? set.width;
            var height = id?.height ?? set.height;

            ExtractPixels(set.Gloss ? set.Gloss : set.Reflectivity, width, height);

            if (set.Profile.glossNoiseFromHeight && set.HeightMap)
                GlossMipmapsFromHeightNoise(set.HeightMap, width, height, set.Profile.bumpNoiseInGlossFraction);

            return pixels;
        }

        protected override void ClearPixels()
        {
            base.ClearPixels();
            mipLevels = null;
        }

        private void GlossMipmapsFromHeightNoise(Texture2D heightMap, int width, int height, float strength)
        {
            if (mipLevels != null) return;

            mipLevels = new List<Color[]>();

            var hpix = heightMap.GetPixels(width, height);

            var w = width;
            var h = height;


            while (w > 1 && h > 1)
            {
                w /= 2;
                h /= 2;

                var dest = new Color[w * h];

                var dx = width / w;
                var dy = height / h;

                float pixelsPerSector = dx * dy;

                for (var x = 0; x < w; x++)
                    for (var y = 0; y < h; y++)
                    {

                        float avg = 0;
                        var col = new Color(0, 0, 0, 0);

                        var start = y * dy * width + x * dx;

                        for (var sy = 0; sy < dy; sy++)
                            for (var sx = 0; sx < dx; sx++)
                            {
                                var ind = start + sy * width + sx;
                                avg += hpix[ind].a;
                                col += pixels[ind];
                            }

                        col /= pixelsPerSector;
                        avg /= pixelsPerSector;

                        float noise = 0;

                        for (var sy = 0; sy < dy; sy++)
                            for (var sx = 0; sx < dx; sx++)
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

    public class TextureRoleReflectivity : TextureRole
    {


        // public override bool sourceSingleChannel { get { return true; } }
        //  public override bool productSingleChannel { get { return true; } }

        public override string NameForDisplayPEGI => "Reflectivity";

        public override Color[] GetPixels(TextureSetForForCombinedMaps set, ImageMeta id)
        {
            if (pixels != null) return pixels;
            
            var width = id?.width ?? set.width;
            var height = id?.height ?? set.height;
            ExtractPixels(set.Reflectivity ? set.Reflectivity : set.Gloss, width, height);
            return pixels;
        }
        public TextureRoleReflectivity(int index) : base(index)
        {
        }
    }

    public class TextureRoleAmbient : TextureRole
    {

        public override string NameForDisplayPEGI => "Ambient";

        public override Color[] GetPixels(TextureSetForForCombinedMaps set, ImageMeta id)
        {
            var width = id?.width ?? set.width;
            var height = id?.height ?? set.height;
            if (pixels == null)
                ExtractPixels(set.Ambient ? set.Ambient : set.HeightMap, width, height);

            return pixels;
        }
        public TextureRoleAmbient(int index) : base(index)
        {
        }
    }

    public class TextureRoleHeight : TextureRole
    {
        public override string NameForDisplayPEGI => "Height";
        public override bool UsingBumpStrengthSlider(int channel) { return channel < 2; }

        // public override bool sourceSingleChannel { get { return true; } }

        protected override List<string> Channels => Chanals;

        private static readonly List<string> Chanals = new List<string> { "Normal R", "Normal G", "Height Greyscale", "Height Alpha" };

        private static int _width;
        private static int _height;

        private static int IndexFrom(int x, int y)
        {

            x %= _width;
            if (x < 0) x += _width;
            y %= _height;
            if (y < 0) y += _height;

            return y * _width + x;
        }

        public TextureRoleHeight(int index) : base(index)
        {
        }

        public override Color[] GetPixels(TextureSetForForCombinedMaps set, ImageMeta id)
        {
            if (pixels != null) return pixels;
            
            _width = id?.width ?? set.width;
            _height = id?.height ?? set.height;

            ExtractPixels(set.HeightMap ? set.HeightMap : set.Ambient, _width, _height);

            float xLeft;
            float xRight;
            float yUp;
            float yDown;

            float yDelta;
            float xDelta;

            float strength = set.Profile.bumpStrength;

            for (int by = 0; @by < _height; @by++)
            {
                for (int bx = 0; bx < _width; bx++)
                {

                    int dstIndex = IndexFrom(bx, @by);

                    var col = pixels[dstIndex];

                    col.b = col.grayscale;

                    xLeft = pixels[IndexFrom(bx - 1, @by)].a;
                    xRight = pixels[IndexFrom(bx + 1, @by)].a;
                    yUp = pixels[IndexFrom(bx, @by - 1)].a;
                    yDown = pixels[IndexFrom(bx, @by + 1)].a;

                    xDelta = (-xRight + xLeft) * strength;

                    yDelta = (-yDown + yUp) * strength;

                    col.r = Mathf.Clamp01(xDelta * Mathf.Abs(xDelta) + 0.5f);
                    col.g = Mathf.Clamp01(yDelta * Mathf.Abs(yDelta) + 0.5f);

                    pixels[dstIndex] = col;

                }
            }

            return pixels;
        }

    }

    public class TextureRole_Normal : TextureRole
    {
        public override string NameForDisplayPEGI => "Normal";

        public override bool UsingBumpStrengthSlider(int channel) { return true; }

        private bool _wasMarkedAsNormal;

        public TextureRole_Normal(int index) : base(index)
        {
        }

        protected override void ClearPixels()
        {
            base.ClearPixels();

#if UNITY_EDITOR
            if (tex)
            {
                var imp = tex.GetTextureImporter();
                if (imp.WasMarkedAsNormal(_wasMarkedAsNormal))
                    imp.SaveAndReimport();
            }
#endif
            _wasMarkedAsNormal = false;
        }
#if UNITY_EDITOR
        Texture2D tex;
#endif
        public override Color[] GetPixels(TextureSetForForCombinedMaps set, ImageMeta id)
        {
          
            if (pixels == null)
            {
                var width = id?.width ?? set.width;
                var height = id?.height ?? set.height;
                ExtractPixels(set.NormalMap, width, height);
            }
#if UNITY_EDITOR
            tex = set.NormalMap;
#endif
            return pixels;
        }
    }


}

