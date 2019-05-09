using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using System;
using System.IO;
using QuizCannersUtilities;

namespace PlaytimePainter
{

    [TaggedType(Tag)]
    public class CombinedMapsControllerModule : PainterSystemManagerModuleBase
    {
        private const string Tag = "CmbndMpsCntrl";
        public override string ClassTag => Tag;
        
        public static CombinedMapsControllerModule _inst;

        private List<TextureSetForCombinedMaps> _textureSets = new List<TextureSetForCombinedMaps>();
        public List<TexturePackagingProfile> texturePackagingSolutions = new List<TexturePackagingProfile>();

        public override void Enable() {
            _inst = this;
        }

        public override string NameForDisplayPEGI => "Combined Maps [{0}]".F(_textureSets.Count);

        #region Encode & Decode
        public override CfgEncoder Encode() => this.EncodeUnrecognized()
            .Add("cm", _textureSets)
            .Add("tps", texturePackagingSolutions);

        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "cm": data.Decode_List(out _textureSets); break;
                case "tps": data.Decode_List(out texturePackagingSolutions); break;
                default: return false;
            }
            return true;
        }

        #endregion

        #region Inspector
        #if PEGI

        private int _browsedTextureSet = -1;
        public override bool Inspect() => "Surfaces".edit_List(ref _textureSets, ref _browsedTextureSet);
        #endif
        #endregion
    }

    public class TextureSetForCombinedMaps : PainterSystemKeepUnrecognizedCfg, IGotName {

        protected static CombinedMapsControllerModule Ctrl => CombinedMapsControllerModule._inst;

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

        public string name;

        public TexturePackagingProfile Profile => Ctrl.texturePackagingSolutions[selectedProfile];

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

        public TextureSetForCombinedMaps()
        {
            name = "Unnamed";
        }

        public string NameForPEGI { get { return name; } set { name = value; } }

        #region Encode & Decode
        public override CfgEncoder Encode() => this.EncodeUnrecognized()
            .Add_Reference("d", diffuse)
            .Add_Reference("h", heightMap)
            .Add_Reference("bump", normalMap)
            .Add_Reference("g", gloss)
            .Add_Reference("r", reflectivity)
            .Add_Reference("ao", ambient)
            .Add_Reference("lp", lastProduct)
            .Add("tw", width)
            .Add("th", height)
            .Add_Bool("col", isColor)
            .Add_String("n", name)
            .Add("sp", selectedProfile);
        
        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "d": data.Decode_Reference(ref diffuse); break;
                case "h": data.Decode_Reference(ref heightMap); break;
                case "bump": data.Decode_Reference(ref normalMap); break;
                case "g": data.Decode_Reference(ref gloss); break;
                case "r":  data.Decode_Reference(ref reflectivity); break;
                case "ao": data.Decode_Reference(ref ambient); break;
                case "lp": data.Decode_Reference(ref lastProduct); break;
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
        public override bool Inspect() {

            var changed = false;

            var id = InspectedImageMeta;

            if (InspectedPainter && id != null) {
                "Editing:".write(40);
                pegi.write(id.texture2D);
                pegi.nl();
            }

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

            if (!InspectedPainter) {
                var firstTex = GetAnyTexture();
                "width:".edit(ref width).nl(ref changed);
                "height".edit(ref height).nl(ref changed);
                if (firstTex && "Match Source".Click().nl(ref changed))
                {
                    width = firstTex.width;
                    height = firstTex.height;
                }

                "is Color".toggle(ref isColor).nl(ref changed);
            }
            
            pegi.select_Index(ref selectedProfile, Ctrl.texturePackagingSolutions);

            if (icon.Add.Click("New Texture Packaging Profile").nl()) {
                Ctrl.texturePackagingSolutions.AddWithUniqueNameAndIndex();
                selectedProfile = Ctrl.texturePackagingSolutions.Count - 1;
            }

            if ((selectedProfile < Ctrl.texturePackagingSolutions.Count))
                Ctrl.texturePackagingSolutions[selectedProfile].Inspect(this).nl(ref changed);

            return changed;
        }
#endif
        #endregion

        public int selectedProfile = 0;


    }

    public class TexturePackagingProfile : PainterSystemCfg, IGotName, IPEGI
    {
        private bool _isColor;
        public float bumpStrength = 0.1f;
        public float bumpNoiseInGlossFraction = 0.1f;
        private List<TextureChannel> _channel;
        private string _name;
        public LinearColor fillColor;
        private bool _glossNoiseFromBump;
        public bool glossNoiseFromHeight;

        public string NameForPEGI { get { return _name; } set { _name = value; } }

        public override string ToString() { return _name; }

        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "ch": data.Decode_List(out _channel); break;
                case "c": _isColor = data.ToBool(); break;
                case "n": _name = data; break;
                case "b": bumpStrength = data.ToFloat(); break;
                case "fc": fillColor = data.ToLinearColor(); break;
                default: return false;
            }
            return true;
        }

        public override CfgEncoder Encode()
        {
            var cody = new CfgEncoder()

            .Add_IfNotEmpty("ch", _channel)
            .Add_Bool("c", _isColor)
            .Add_String("n", _name)
            .Add("b", bumpStrength)
            .Add("fc", fillColor);

            return cody;
        }

        private const string FolderName = "TexSolution";


#if PEGI
        public virtual bool Inspect() => Inspect(null);

        public bool Inspect(TextureSetForCombinedMaps sets)
        {
            var p = InspectedPainter;

            pegi.nl();

            var changed = "Name".edit(80, ref _name);

            var path = Path.Combine(PainterCamera.Data.texturesFolderName, FolderName);

            if (icon.Save.Click("Will save to " + path).nl())
            {
                this.SaveToAssets(path, _name);
                UnityUtils.RefreshAssetDatabase();
                (_name + " was saved to " + path).showNotificationIn3D_Views();
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

                if ((sets == null || sets.heightMap) &&
                 "Gloss Mip -= Height Noise".toggle(ref glossNoiseFromHeight).nl(ref changed))
                    _glossNoiseFromBump = false;



                if ((sets == null || sets.normalMap)
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

        private void Combine(TextureSetForCombinedMaps set, PlaytimePainter p) {

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
                set.lastProduct = tex;
            }
            else
            {
                tex.SetPixels(dst);
                tex.Apply(false, false);
                set.lastProduct = tex;

#if UNITY_EDITOR
                set.lastProduct = tex.SaveTextureAsAsset(TexMGMTdata.texturesFolderName, ref set.name, false);

                var importer = set.lastProduct.GetTextureImporter();

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

            _name = "unnamed";
        }
    }

    public class TextureChannel : AbstractCfg, IPEGI
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

        public override CfgEncoder Encode() => new CfgEncoder()
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

                pegi.select_Index(ref _sourceRole, rls).changes(ref changed);

                rls[_sourceRole].Inspect(ref sourceChannel, this).changes(ref changed);
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
        protected virtual List<string> GetChannels => Chennels;
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
                    Debug.Log("Pixel extraction from {0} failed".F(tex.name, ex));
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

        public virtual Color[] GetPixels(TextureSetForCombinedMaps set, ImageMeta id)
        {
            if (pixels == null)
                ExtractPixels(set.diffuse, 
                    id?.width ?? set.width,
                    id?.height ?? set.height);

            return pixels;
        }

        public List<Color[]> GetMipPixels(TextureSetForCombinedMaps set, ImageMeta id)
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

        public bool Inspect(ref int selectedChannel, TextureChannel tc)
        {
            var changed = ".".select_Index(10, ref selectedChannel, GetChannels).nl();

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

        public override Color[] GetPixels(TextureSetForCombinedMaps set, ImageMeta id)
        {

            if (id != null)
                return id.Pixels;

            if (pixels == null)
                ExtractPixels(set.lastProduct, set.width, set.height);

            return pixels;

        }

    }

    public class TextureRole_FillColor : TextureRole
    {
        protected override bool IsColor => true;

        public override bool UsingColorSelector => true;

        public override string NameForDisplayPEGI => "Fill Color";

        public override Color[] GetPixels(TextureSetForCombinedMaps set, ImageMeta id)
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

        public override Color[] GetPixels(TextureSetForCombinedMaps set, ImageMeta id) {
            if (pixels == null)
                ExtractPixels(set.diffuse, id?.width ?? set.width, id?.height ?? set.height);

            return pixels;
        }
        public TextureRoleDiffuse(int index) : base(index)
        {
        }

    }

    public class TextureRole_Gloss : TextureRole
    {

        public override string NameForDisplayPEGI => "Gloss";

        public override Color[] GetPixels(TextureSetForCombinedMaps set, ImageMeta id)
        {
            if (pixels != null) return pixels;
            
            var width = id?.width ?? set.width;
            var height = id?.height ?? set.height;

            ExtractPixels(set.gloss ? set.gloss : set.reflectivity, width, height);

            if (set.Profile.glossNoiseFromHeight && set.heightMap)
                GlossMipmapsFromHeightNoise(set.heightMap, width, height, set.Profile.bumpNoiseInGlossFraction);

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

            var hPixels = heightMap.GetPixels(width, height);

            var w = width;
            var h = height;
            
            while (w > 1 && h > 1) {

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
                                avg += hPixels[ind].a;
                                col += pixels[ind];
                            }

                        col /= pixelsPerSector;
                        avg /= pixelsPerSector;

                        float noise = 0;

                        for (var sy = 0; sy < dy; sy++)
                            for (var sx = 0; sx < dx; sx++)
                                noise += Mathf.Abs(hPixels[start + sy * width + sx].a - avg);

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

        public override string NameForDisplayPEGI => "Reflectivity";

        public override Color[] GetPixels(TextureSetForCombinedMaps set, ImageMeta id)
        {
            if (pixels != null) return pixels;
            
            var width = id?.width ?? set.width;
            var height = id?.height ?? set.height;
            ExtractPixels(set.reflectivity ? set.reflectivity : set.gloss, width, height);
            return pixels;
        }
        public TextureRoleReflectivity(int index) : base(index)
        {
        }
    }

    public class TextureRoleAmbient : TextureRole
    {

        public override string NameForDisplayPEGI => "Ambient";

        public override Color[] GetPixels(TextureSetForCombinedMaps set, ImageMeta id)
        {
            var width = id?.width ?? set.width;
            var height = id?.height ?? set.height;
            if (pixels == null)
                ExtractPixels(set.ambient ? set.ambient : set.heightMap, width, height);

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

        protected override List<string> GetChannels => Channels;

        private static readonly List<string> Channels = new List<string> { "Normal R", "Normal G", "Height Greyscale", "Height Alpha" };

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

        public override Color[] GetPixels(TextureSetForCombinedMaps set, ImageMeta id)
        {
            if (pixels != null) return pixels;
            
            _width = id?.width ?? set.width;
            _height = id?.height ?? set.height;

            ExtractPixels(set.heightMap ? set.heightMap : set.ambient, _width, _height);

            var strength = set.Profile.bumpStrength;

            for (var bY = 0; bY < _height; bY++)
            {
                for (var bX = 0; bX < _width; bX++)
                {

                    var dstIndex = IndexFrom(bX, bY);

                    var col = pixels[dstIndex];

                    col.b = col.grayscale;

                    var xLeft = pixels[IndexFrom(bX - 1, bY)].a;
                    var xRight = pixels[IndexFrom(bX + 1, bY)].a;
                    var yUp = pixels[IndexFrom(bX, bY - 1)].a;
                    var yDown = pixels[IndexFrom(bX, bY + 1)].a;

                    var xDelta = (-xRight + xLeft) * strength;

                    var yDelta = (-yDown + yUp) * strength;

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

        public override bool UsingBumpStrengthSlider(int channel) => true; 

        #if UNITY_EDITOR
        private Texture2D _texture;
        private bool _wasMarkedAsNormal;
        #endif

        public TextureRole_Normal(int index) : base(index)
        {
        }

        protected override void ClearPixels()
        {
            base.ClearPixels();

#if UNITY_EDITOR
            if (_texture) {
                var imp = _texture.GetTextureImporter();
                if (imp.WasMarkedAsNormal(_wasMarkedAsNormal))
                    imp.SaveAndReimport();
            }

            _wasMarkedAsNormal = false;
#endif

        }

        public override Color[] GetPixels(TextureSetForCombinedMaps set, ImageMeta id)
        {
          
            if (pixels == null)
            {
                var width = id?.width ?? set.width;
                var height = id?.height ?? set.height;
                ExtractPixels(set.normalMap, width, height);
            }

            #if UNITY_EDITOR
                _texture = set.normalMap;
            #endif
            return pixels;
        }
    }
}

