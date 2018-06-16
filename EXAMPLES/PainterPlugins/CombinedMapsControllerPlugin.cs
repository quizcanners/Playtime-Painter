using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using System;
using UnityEngine.SceneManagement;

using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using SharedTools_Stuff;


namespace Playtime_Painter {
    namespace CombinedMaps {


#if UNITY_EDITOR 
        using UnityEditor;

#if PEGI
        [CustomEditor(typeof(CombinedMapsControllerPlugin))]
        public class CombinedMapsControllerEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                ef.start(serializedObject);
                ((CombinedMapsControllerPlugin)target).ConfigTab_PEGI();
                ef.end();
            }
        }
#endif
#endif


        public static class DefaultMapsExtensions
        {
            public static bool UpdateBumpGloss(this List<ChannelSetsForDefaultMaps> mergeSubmasks)
            {
                bool changes = false;

                if (mergeSubmasks != null)

                    //for (int i = 0; i < mergeSubmasks.Length; i++) {
                    foreach (var tmp in mergeSubmasks)
                    {
                        if (tmp.updateThis)
                        {
                            tmp.updateThis = false;
#if UNITY_EDITOR
                            tmp.Product_combinedBump = NormalMapFrom(tmp.normalStrength, 0.1f, tmp.height, tmp.normalMap, tmp.ambient, tmp.productName, tmp.Product_combinedBump);
                            if (tmp.colorTexture != null)
                                tmp.Product_colorWithAlpha = GlossToAlpha(tmp.smooth, tmp.colorTexture, tmp.productName);
#endif
                            changes = true;
                        }
                    }
                return changes;
            }


#if UNITY_EDITOR

            static Color[] srcBmp;
            static Color[] srcSm;
            static Color[] srcAmbient;
            static Color[] dst;


            static int width;
            static int height;

            static int indexFrom(int x, int y)
            {

                x %= width;
                if (x < 0) x += width;
                y %= height;
                if (y < 0) y += height;

                return y * width + x;
            }

            public static Texture2D NormalMapFrom(float strength, float diagonalPixelsCoef, Texture2D bump, Texture2D normalReady, Texture2D ambient, string name, Texture2D Result)
            {

                if (bump == null)
                {
                    Debug.Log("No bump texture");
                    return null;
                }

                float xLeft;
                float xRight;
                float yUp;
                float yDown;

                float yDelta;
                float xDelta;

                width = bump.width;
                height = bump.height;

                TextureImporter importer = bump.getTextureImporter();
                bool needReimport = importer.wasNotReadable();
                needReimport |= importer.wasNotSingleChanel();
                if (needReimport) importer.SaveAndReimport();

                if (normalReady != null)
                {
                    importer = normalReady.getTextureImporter();
                    needReimport = importer.wasNotReadable();
                    needReimport |= importer.wasWrongIsColor(false);
                    needReimport |= importer.wasMarkedAsNormal();
                    if (needReimport) importer.SaveAndReimport();
                }

                importer = ambient.getTextureImporter();
                needReimport = importer.wasNotReadable();
                needReimport |= importer.wasWrongIsColor(false);
                needReimport |= importer.wasNotSingleChanel();
                if (needReimport) importer.SaveAndReimport();

                try
                {
                    srcBmp = (normalReady != null) ? normalReady.GetPixels(width, height) : bump.GetPixels();
                    srcSm = bump.GetPixels(width, height);
                    srcAmbient = ambient.GetPixels(width, height);
                    dst = new Color[height * width];
                }
                catch (UnityException e)
                {
                    Debug.Log("couldn't read one of the textures for  " + bump.name + " " + e.ToString());
                    return null;
                }


                for (int by = 0; by < height; by++)
                {
                    for (int bx = 0; bx < width; bx++)
                    {

                        int dstIndex = indexFrom(bx, by);

                        if (normalReady)
                        {
                            dst[dstIndex].r = (srcBmp[dstIndex].r - 0.5f) * strength + 0.5f;
                            dst[dstIndex].g = (srcBmp[dstIndex].g - 0.5f) * strength + 0.5f;

                        }
                        else
                        {

                            xLeft = srcBmp[indexFrom(bx - 1, by)].a;
                            xRight = srcBmp[indexFrom(bx + 1, by)].a;
                            yUp = srcBmp[indexFrom(bx, by - 1)].a;
                            yDown = srcBmp[indexFrom(bx, by + 1)].a;

                            xDelta = (-xRight + xLeft) * strength;

                            yDelta = (-yDown + yUp) * strength;

                            dst[dstIndex].r = xDelta * Mathf.Abs(xDelta)
                                + 0.5f;
                            dst[dstIndex].g = yDelta * Mathf.Abs(yDelta)
                                + 0.5f;
                        }

                        dst[dstIndex].b = srcSm[dstIndex].a;
                        dst[dstIndex].a = srcAmbient[dstIndex].a;
                    }
                }


                if ((Result == null) || (Result.width != width) || (Result.height != height))
                    Result = bump.CreatePngSameDirectory(name + "_MASKnMAPS");

                TextureImporter resImp = Result.getTextureImporter();
                needReimport = resImp.wasClamped();
                needReimport |= resImp.wasWrongIsColor(false);
                needReimport |= resImp.wasNotReadable();
                needReimport |= resImp.hadNoMipmaps();


                if (needReimport)
                    resImp.SaveAndReimport();

                Result.SetPixels(dst);
                Result.Apply();
                Result.saveTexture();

                return Result;
            }

            public static Texture2D GlossToAlpha(Texture2D gloss, Texture2D diffuse, string newName)
            {

                if (gloss == null)
                {
                    Debug.Log("No bump texture");
                    return null;
                }

                TextureImporter ti = gloss.getTextureImporter();
                bool needReimport = ti.wasNotSingleChanel();
                needReimport |= ti.wasNotReadable();
                if (needReimport) ti.SaveAndReimport();


                ti = diffuse.getTextureImporter();
                needReimport = ti.wasAlphaNotTransparency();
                needReimport |= ti.wasNotReadable();
                if (needReimport) ti.SaveAndReimport();

                Texture2D product = diffuse.CreatePngSameDirectory(newName + "_COLOR");

                TextureImporter importer = product.getTextureImporter();
                needReimport = importer.wasNotReadable();
                needReimport |= importer.wasClamped();
                needReimport |= importer.hadNoMipmaps();
                if (needReimport)
                    importer.SaveAndReimport();


                width = gloss.width;
                height = gloss.height;
                Color[] dstColor;

                try
                {
                    dstColor = diffuse.GetPixels();
                    srcBmp = gloss.GetPixels(diffuse.width, diffuse.height);
                }
                catch (UnityException e)
                {
                    Debug.Log("couldn't read one of the textures for  " + gloss.name + " " + e.ToString());
                    return null;
                }


                int dstIndex;
                for (int by = 0; by < height; by++)
                {
                    for (int bx = 0; bx < width; bx++)
                    {
                        dstIndex = indexFrom(bx, by);
                        Color col;
                        col = dstColor[dstIndex];
                        col.a = srcBmp[dstIndex].a;
                        dstColor[dstIndex] = col;
                    }
                }

                product.SetPixels(dstColor);
                product.Apply();
                product.saveTexture();

                return product;
            }
#endif
        }

        [Serializable]
        public class CombinedMapsControllerPlugin : PainterManagerPluginBase
        {
            public static CombinedMapsControllerPlugin _inst;

            public List<TextureSetForForCombinedMaps> forCombinedMaps;
            public List<TexturePackagingProfile> texturePackagingSolutions;

            public override void OnEnable()
            {
                _inst = this;
                if (forCombinedMaps == null)
                    forCombinedMaps = new List<TextureSetForForCombinedMaps>();
                if (texturePackagingSolutions == null)
                    texturePackagingSolutions = new List<TexturePackagingProfile>();
            }

            public override string ToString()
            {
                return "Combined Maps";
            }

            #if PEGI
            [SerializeField]
            int browsedTextureSet = -1;
            public override bool ConfigTab_PEGI()
            {
                return forCombinedMaps.edit_List(ref browsedTextureSet, true);
            }
#endif
        }


        [Serializable]
        public class TextureSetForForCombinedMaps : PainterStuff
            #if PEGI
            , iGotName, iPEGI
#endif
        {

            protected CombinedMapsControllerPlugin ctrl { get { return CombinedMapsControllerPlugin._inst; } }

            //public List<Texture2D> textures;
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

            public TexturePackagingProfile profile { get { return ctrl.texturePackagingSolutions[selectedProfile]; } }

            public Texture2D GetTexture()
            {
                if (Diffuse != null) return Diffuse;
                if (HeightMap != null) return HeightMap;
                if (NormalMap != null) return NormalMap;
                if (Gloss != null) return Gloss;
                if (Reflectivity != null) return Reflectivity;
                if (Ambient != null) return Ambient;
                if (LastProduct != null) return LastProduct;
                return null;
            }

            public TextureSetForForCombinedMaps()
            {
                name = "Unnamed";
            }

            public string NameForPEGI { get { return name; } set { name = value; } }

            #if PEGI

            public bool PEGI()
            {


                bool changed = false;

                changed |= "Name".edit(ref name).nl();

                var id = inspectedImageData;

                if (inspectedPainter != null && id != null)
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
                var cfg = PainterConfig.inst;

                if (inspectedPainter == null)
                {
                    var frstTex = GetTexture();
                    changed |= "width:".edit(ref width).nl();
                    changed |= "height".edit(ref height).nl();
                    if (frstTex != null && "Match Source".Click().nl())
                    {
                        width = frstTex.width;
                        height = frstTex.height;
                    }

                    changed |= "is Color".toggle(ref isColor).nl();
                }

                changed |= "Packaging Profile".foldout(ref showProfile);
                if (!showProfile)
                    pegi.select(ref selectedProfile, ctrl.texturePackagingSolutions).nl();

                if ((selectedProfile < ctrl.texturePackagingSolutions.Count) && (showProfile))

                    changed |= ctrl.texturePackagingSolutions[selectedProfile].PEGI(this).nl();

                else
                if (icon.Add.Click("New Texture Packaging Profile", 25).nl())
                {
                    ctrl.texturePackagingSolutions.AddWithUniqueNameAndIndex();
                    selectedProfile = ctrl.texturePackagingSolutions.Count - 1;
                }

                currentPainter = null;



                return changed;
            }

#endif

            public bool showProfile;
            public int selectedProfile = 0;
            public static PlaytimePainter currentPainter;


        }

        [Serializable]
        public class TexturePackagingProfile : PainterStuff_STD
            #if PEGI
            , iGotName
#endif
        {

            public bool isColor;
            public float bumpStrength = 0.1f;
            public float bumpNoiseInGlossFraction = 0.1f;
            public List<TextureChannel> channel;
            public string name;
            public linearColor fillColor;
            public bool glossNoiseFromBump;
            public bool glossNoiseFromHeight;

            public string NameForPEGI { get { return name; } set { name = value; } }

            public override string ToString() { return name; }

            public override bool Decode(string tag, string data)
            {
                switch (tag)
                {
                    case "ch":data.DecodeInto(out channel); break;
                    case "c": isColor = data.ToBool(); break;
                    case "n": name = data; break;
                    case "b": bumpStrength = data.ToFloat(); break;
                    case "fc": fillColor = data.ToLinearColor(); break;
                    default: return false;
                }
                return true;
            }

            public override stdEncoder Encode()
            {
                var cody = new stdEncoder()

                .Add_ifNotEmpty("ch", channel)
                .Add_Bool("c", isColor)
                .Add_String("n", name)
                .Add("b", bumpStrength)
                .Add("fc", fillColor);

                return cody;
            }

            public const string folderName = "TexSolution";

            public override string GetDefaultTagName()
            {
                return folderName;
            }

            public static TexturePackagingProfile currentPEGI;


            #if PEGI
            public override bool PEGI()
            {
                return PEGI(null);
            }

            public bool PEGI(TextureSetForForCombinedMaps sets)
            {
                PlaytimePainter p = inspectedPainter;

                pegi.nl();

                currentPEGI = this;

                bool changed = "Name".edit(80,ref name);

                var path = PainterConfig.inst.texturesFolderName + "/" + folderName;

                if (icon.Save.Click("Will save to " + path, 25).nl())
                {
                    this.SaveToAssets(path, name).RefreshAssetDatabase();
                    (name + " was saved to " + path).showNotification();
                }
                pegi.newLine();

                changed |= "Color texture ".toggle(80,ref isColor).nl();

                bool usingBumpStrength = false;
                bool usingColorSelector = false;
                bool usingGlossMap = false;

                for (int c = 0; c < 4; c++)
                {
                    var ch = channel[c];
                    changed |= ((ColorChanel)c).getIcon().toggle(ref ch.enabled);

                    if (ch.enabled)
                    {
                        if ((ch.flip ? "inverted" : "+0").Click("Copy as is or invert (1-X)"))
                            ch.flip = !ch.flip;

                        changed |= ch.Nested_Inspect().nl();

                        usingBumpStrength |= ch.role.usingBumpStrengthSlider(ch.sourceChannel);
                        usingColorSelector |= ch.role.usingColorSelector;
                        usingGlossMap |= ch.role.GetType() == typeof(TextureRole_Gloss);
                    }
                    pegi.nl();
                }

                


                if (usingBumpStrength) changed |= "Bump Strength".edit(ref bumpStrength).nl();
                if (usingColorSelector) changed |= "Color".edit(ref fillColor).nl();
                if (usingGlossMap)
                {

                    if (sets == null || sets.HeightMap != null)
                    {
                        if ("Gloss Mip -= Height Noise".toggle(ref glossNoiseFromHeight).nl())
                        {
                            changed = true;
                            glossNoiseFromBump = false;
                        }
                    }

                    if ((sets == null || sets.NormalMap != null) && "Gloss Mip -= Normal Noise".toggle(ref glossNoiseFromBump).nl())
                    {
                        changed = true;
                        glossNoiseFromHeight = false;
                    }

                    if (glossNoiseFromHeight || glossNoiseFromBump)
                        "Fraction".edit(ref bumpNoiseInGlossFraction, 0f, 40f).nl();

                }


                if (sets != null)
                {
                    if ("Combine".Click().nl())
                        Combine(sets, p);

                    if (p != null)
                        "You will still need to press SAVE in Painter to update original texture.".writeHint();
                }

                return changed;
            }

#endif

            void Combine(TextureSetForForCombinedMaps set, PlaytimePainter p)
            {
                TextureRole.Clear();

                int size;
                var id = p == null ? null : p.imgData;
                Color[] dst;
                Texture2D tex = null;

                if (id != null)
                {
                    size = id.width * id.height;
                    dst = id.pixels;
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
                        var col = c.role.GetPixels(set, id);

                        if (c.flip)
                        for (int i = 0; i < size; i++)
                            dst[i][colChan] = 1-col[i][ch];
                        else 
                        for (int i = 0; i < size; i++)
                            dst[i][colChan] = col[i][ch];


                        var newMips = c.role.GetMipPixels(set, id);
                        for (int m = 1; m < tex.mipmapCount; m++)
                        {
                            var mlevel = mips[m - 1];
                            var newLevel = newMips[m - 1];


                            if (c.flip)
                            for (int si = 0; si < mlevel.Length; si++)
                                mlevel[si][colChan] = 1-newLevel[si][colChan];
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
                    set.LastProduct = tex.saveTextureAsAsset(PainterConfig.inst.texturesFolderName, ref set.name, false);

                    TextureImporter importer = set.LastProduct.getTextureImporter();

                    bool needReimport = importer.wasNotReadable();
                    needReimport |= importer.wasWrongIsColor(isColor);
                    needReimport |= importer.wasClamped();

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
        public class TextureChannel : abstract_STD
        {
            public bool enabled;
            public bool flip = false;
            public int sourceRole;
            public int sourceChannel;

            public TextureRole role { get { return TextureRole.all[sourceRole]; } set { sourceRole = value.index; } }

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

            public override stdEncoder Encode()
            {
                var cody = new stdEncoder()
                .Add_ifNotZero("s", sourceRole)
                .Add_ifNotZero("c", sourceChannel)
                .Add_Bool("f", flip);
                return cody;
            }

            #if PEGI

            public override bool PEGI()
            {

                bool changed = false;

                if (enabled)
                {

                    var rls = TextureRole.all;

                    changed |= pegi.select(ref sourceRole, rls);

                    // if (!role.productSingleChannel)
                    changed |= rls[sourceRole].PEGI(ref sourceChannel, this);
                }
                pegi.newLine();

                return changed;
            }

#endif

            public const string stdTag = "TexChan";

          
        }


        public abstract class TextureRole
        {

            static List<TextureRole> _allRoles;
            public static List<TextureRole> all
            {
                get
                {
                    if (_allRoles == null)
                    {
                        _allRoles = new List<TextureRole>();
                        _allRoles.Add(new TextureRole_Diffuse());
                        _allRoles.Add(new TextureRole_Height());
                        _allRoles.Add(new TextureRole_Normal());
                        _allRoles.Add(new TextureRole_Result());
                        _allRoles.Add(new TextureRole_FillColor());
                        _allRoles.Add(new TextureRole_Ambient());
                        _allRoles.Add(new TextureRole_Gloss());
                        _allRoles.Add(new TextureRole_Reflectivity());
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

            public TextureRole()
            {
                index = _allRoles.Count;
            }

            protected bool wasNormal;

            public virtual bool usingBumpStrengthSlider(int sourceChannel) { return false; }
            public virtual bool usingColorSelector { get { return false; } }
            public virtual bool isColor { get { return false; } }
            // public virtual bool sourceSingleChannel { get { return false; } }
            //public virtual bool productSingleChannel { get { return false; } }
            public virtual List<string> channels { get { return chans; } }
            public virtual Color DefaultColor { get { return isColor ? Color.white : Color.grey; } }


            static List<string> chans = new List<string> { "R", "G", "B", "A" };

            protected void ExtractPixels(Texture2D tex, int width, int height)
            {

                if (tex != null)
                {
                    try
                    {

#if UNITY_EDITOR
                        var importer = tex.getTextureImporter();
                        bool needReimport = importer.wasNotReadable();

                        //needReimport |= importer.wasWrongIsColor(isColor);
                        if (importer.wasMarkedAsNormal())
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

                if (tex == null)
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

                int width = id == null ? set.width : id.width;
                int height = id == null ? set.height : id.height;

                if (_pixels == null)
                    ExtractPixels(set.Diffuse, width, height);

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
                bool changed = ".".select(10, ref selectedChannel, channels).nl();

                return changed;
            }

#endif

        }

        public class TextureRole_Result : TextureRole
        {
            public static TextureRole_Result inst;
            public override string ToString() { return "Result"; }

            public TextureRole_Result()
            {
                inst = this;
            }

            public override Color[] GetPixels(TextureSetForForCombinedMaps set, ImageData id)
            {

                if (id != null) return id.pixels;

                if (_pixels == null)
                    ExtractPixels(set.LastProduct, set.width, set.height);

                return _pixels;

            }

        }

        public class TextureRole_FillColor : TextureRole
        {
            public override bool isColor { get { return true; } }

            public override bool usingColorSelector { get { return true; } }

            public override string ToString() { return "Fill Color"; }

            public override Color[] GetPixels(TextureSetForForCombinedMaps set, ImageData id)
            {
                int width = id == null ? set.width : id.width;
                int height = id == null ? set.height : id.height;

                if (_pixels == null)
                {
                    var col = set.profile.fillColor.ToGamma();
                    var size = width * height;
                    _pixels = new Color[size];
                    for (int i = 0; i < size; i++)
                        _pixels[i] = col;
                }

                return _pixels;
            }

        }

        public class TextureRole_Diffuse : TextureRole
            {
                public override bool isColor { get { return true; } }

                public override string ToString() { return "Color"; }

                public override Color[] GetPixels(TextureSetForForCombinedMaps set, ImageData id)
                {
                    int width = id == null ? set.width : id.width;
                    int height = id == null ? set.height : id.height;
                    if (_pixels == null)
                        ExtractPixels(set.Diffuse, width, height);

                    return _pixels;
                }


            }

        public class TextureRole_Gloss : TextureRole
        {

            public override string ToString() { return "Gloss"; }

            public override Color[] GetPixels(TextureSetForForCombinedMaps set, ImageData id)
            {
                int width = id == null ? set.width : id.width;
                int height = id == null ? set.height : id.height;
                if (_pixels == null)
                {
                    ExtractPixels(set.Gloss ? set.Gloss : set.Reflectivity, width, height);

                    if (set.profile.glossNoiseFromHeight && set.HeightMap != null)
                        GlossMipmapsFromHeightNoise(set.HeightMap, width, height, set.profile.bumpNoiseInGlossFraction);
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

        }

        public class TextureRole_Reflectivity : TextureRole
        {


            // public override bool sourceSingleChannel { get { return true; } }
            //  public override bool productSingleChannel { get { return true; } }

            public override string ToString() { return "Reflectivity"; }

            public override Color[] GetPixels(TextureSetForForCombinedMaps set, ImageData id)
            {
                int width = id == null ? set.width : id.width;
                int height = id == null ? set.height : id.height;
                if (_pixels == null)
                    ExtractPixels(set.Reflectivity ? set.Reflectivity : set.Gloss, width, height);

                return _pixels;
            }

        }

        public class TextureRole_Ambient : TextureRole
        {
 
            public override string ToString() { return "Ambient"; }

            public override Color[] GetPixels(TextureSetForForCombinedMaps set, ImageData id)
            {
                int width = id == null ? set.width : id.width;
                int height = id == null ? set.height : id.height;
                if (_pixels == null)
                    ExtractPixels(set.Ambient ? set.Ambient : set.HeightMap, width, height);

                return _pixels;
            }

        }

        public class TextureRole_Height : TextureRole
        {
            public override string ToString() { return "Height"; }
            public override bool usingBumpStrengthSlider(int channel) { return channel < 2; }

            // public override bool sourceSingleChannel { get { return true; } }

            public override List<string> channels { get { return chans; } }

            static List<string> chans = new List<string> { "Normal R", "Normal G", "Height Greyscale", "Height Alpha" };

            static int width;
            static int height;

            static int indexFrom(int x, int y)
            {

                x %= width;
                if (x < 0) x += width;
                y %= height;
                if (y < 0) y += height;

                return y * width + x;
            }


            public override Color[] GetPixels(TextureSetForForCombinedMaps set, ImageData id)
            {
                width = id == null ? set.width : id.width;
                height = id == null ? set.height : id.height;
                if (_pixels == null)
                {
                    ExtractPixels(set.HeightMap != null ? set.HeightMap : set.Ambient, width, height);

                    float xLeft;
                    float xRight;
                    float yUp;
                    float yDown;

                    float yDelta;
                    float xDelta;

                    float strength = set.profile.bumpStrength;

                    for (int by = 0; by < height; by++)
                    {
                        for (int bx = 0; bx < width; bx++)
                        {

                            int dstIndex = indexFrom(bx, by);

                            var col = _pixels[dstIndex];

                            col.b = col.grayscale;

                            xLeft = _pixels[indexFrom(bx - 1, by)].a;
                            xRight = _pixels[indexFrom(bx + 1, by)].a;
                            yUp = _pixels[indexFrom(bx, by - 1)].a;
                            yDown = _pixels[indexFrom(bx, by + 1)].a;

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
            public override string ToString() { return "Normal"; }

            public override bool usingBumpStrengthSlider(int channel) { return true; }

            public bool wasMarkedAsNormal = false;

            public override void ClearPixels()
            {
                base.ClearPixels();

#if UNITY_EDITOR
                if (tex != null)
                {
                    var imp = tex.getTextureImporter();
                    if (imp.wasMarkedAsNormal(wasMarkedAsNormal))
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
                int width = id == null ? set.width : id.width;
                int height = id == null ? set.height : id.height;
                if (_pixels == null)
                    ExtractPixels(set.NormalMap, width, height);
#if UNITY_EDITOR
                tex = set.NormalMap;
#endif
                return _pixels;
            }
        }

        [Serializable]
        public class ChannelSetsForDefaultMaps
        {
            public string productName;
            public Texture2D colorTexture;
            public Texture2D height;
            public Texture2D normalMap;
            public Texture2D smooth;
            public Texture2D ambient;
            public Texture2D reflectiveness;

            public Texture2D Product_colorWithAlpha;
            public Texture2D Product_combinedBump;
            public int size = 1024;
            public float normalStrength;
            public bool updateThis;

            public int packagingProfile;

            public ChannelSetsForDefaultMaps()
            {
                normalStrength = 1;
            }

            public bool PEGI()
            {
                bool changed = false;
                return changed;
            }

        }
    }

}