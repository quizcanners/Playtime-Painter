using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using PlayerAndEditorGUI;

#if UNITY_EDITOR
using UnityEditor;
#endif

using StoryTriggerData;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;



namespace Playtime_Painter {

    namespace CombinedMaps
    {

        [Serializable]
        public class TextureSetForForCombinedMaps :iGotName, iPEGI {

            //public List<Texture2D> textures;
            public Texture2D Diffuse;
            public Texture2D Height;
            public Texture2D Normal;
            public Texture2D Gloss;
            public Texture2D Reflectivity;
            public Texture2D Ambient;

            public string name;
            public int selectedProfile = 0;
            
            public Texture2D GetTexture() {
                if (Diffuse != null) return Diffuse;
                if (Height != null) return Height;
                if (Normal != null) return Normal;
                if (Gloss != null) return Gloss;
                if (Reflectivity != null) return Reflectivity;
                if (Ambient != null) return Ambient;
              
                return null;
            }

            public TextureSetForForCombinedMaps() {
                name = "Unnamed";
            }

            static bool showProfile;

            public string Name  { get { return name; }   set { name = value; } }

            public bool PEGI() {
                return PEGI(null);
            }



            public bool PEGI(PlaytimePainter painter){
                bool changed = false;

                changed |= "Diffuse".edit(ref Diffuse).nl();
                changed |= "Height".edit(ref Height).nl();
                changed |= "Normal".edit(ref Normal).nl();
                changed |= "Gloss".edit(ref Gloss).nl();
                changed |= "Reflectivity".edit(ref Reflectivity).nl();
                changed |= "Ambient".edit(ref Ambient).nl();

                var cfg = PainterConfig.inst;

                changed |= "Packaging Profile".select(ref selectedProfile, cfg.texturePackagingSolutions).nl();

                if ((selectedProfile < cfg.texturePackagingSolutions.Count) && (cfg.texturePackagingSolutions[selectedProfile].name.foldout(ref showProfile))) 
                  
                        changed |= cfg.texturePackagingSolutions[selectedProfile].PEGI(this, painter).nl();
                
                else
                if (icon.Add.Click("New Texture Packaging Profile", 25).nl()) cfg.texturePackagingSolutions.Add(new TexturePackagingProfile());

                return changed;
            }
        }

        [Serializable]
        public class TexturePackagingProfile : abstract_STD, iGotName
        {

            public bool isColor;
            public float bumpStrength = 0.1f;
            public List<TextureChannel> channel;
            public string name;
            public linearColor singleValue;

            public string Name { get { return name; } set { name = value; } }

            public override string ToString() { return name; }

            public override void Decode(string tag, string data)
            {
                switch (tag)
                {
                    case "ch": channel = data.ToListOf_STD<TextureChannel>(); break;
                    case "c": isColor = data.ToBool(); break;
                    case "n": name = data; break;
                }
            }

            public override stdEncoder Encode()
            {
                var cody = new stdEncoder();

                cody.AddIfNotEmpty("ch", channel);
                cody.Add("c", isColor);
                cody.AddText("n", name);

                return cody;
            }

            public const string stdTag = "TexPack";

            public override string getDefaultTagName()
            {
                return stdTag;
            }


            public static TexturePackagingProfile currentPEGI;

          public override bool PEGI()
            {
                return PEGI(null, null);
            }

            public bool PEGI(TextureSetForForCombinedMaps sets, PlaytimePainter p)
            {
                pegi.nl();

                currentPEGI = this;

                bool changed = "Name".edit(ref name).nl();

                changed |= "Color texture ".toggle(ref isColor).nl();

                bool usingBumpStrength = false;
                bool usingColorSelector = false;

                for (int c = 0; c < 4; c++)
                {
                    var ch = channel[c];
                    changed |= ((ColorChanel)c).getIcon().toggle(ref ch.enabled);

                    changed |= ch.PEGI().nl();

                    usingBumpStrength |= ch.role.usingBumpStrengthSlider(ch.sourceChannel);
                    usingColorSelector |= ch.role.usingColorSelector;
                }

                if (usingBumpStrength) changed |= "Bump Strength".edit(ref bumpStrength).nl();
                if (usingColorSelector) changed |= "Color".edit(ref singleValue).nl();

                if ((sets != null)  && (p!= null))
                    if ("Combine".Click().nl())
                                    Combine(sets, p);
                
                

                return changed;
            }

            void Combine(TextureSetForForCombinedMaps set, PlaytimePainter p)
            {
                TextureRole.Clear();

                var id = p.curImgData;


                int size = id.width * id.height;
                var dst = id.pixels;


                if (channel[0].enabled) {
                    var ch = channel[0].sourceChannel;
                    var col = channel[0].role.GetPixels(set, id);
                    for (int i = 0; i < size; i++)
                        dst[i].r = col[i][ch];
                }

                if (channel[1].enabled) {
                    var ch = channel[1].sourceChannel;
                    var col = channel[1].role.GetPixels(set, id);
                    for (int i = 0; i < size; i++)
                        dst[i].g = col[i][ch];
                }

                if (channel[2].enabled) {
                    var ch = channel[2].sourceChannel;
                    var col = channel[2].role.GetPixels(set, id);
                    for (int i = 0; i < size; i++)
                        dst[i].b = col[i][ch];
                }

                if (channel[3].enabled) {
                    var ch = channel[3].sourceChannel;
                    var col = channel[3].role.GetPixels(set, id);
                    for (int i = 0; i < size; i++)
                        dst[i].a = col[i][ch];
                }
                
                id.SetAndApply(true);
            
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
        public class TextureChannel : abstract_STD {
            public bool enabled;
            public int sourceRole;
            public int sourceChannel;

            public TextureRole role { get { return TextureRole.all[sourceRole]; } set { sourceRole = value.index; } }

            public override void Decode(string tag, string data)
            {
                switch (tag)
                {
                    case "s": sourceRole = data.ToInt(); break;
                    case "c": sourceChannel = data.ToInt(); break;
                }
            }

            public override stdEncoder Encode()
            {
                var cody = new stdEncoder();
                cody.AddIfNotZero("s", sourceRole);
                cody.AddIfNotZero("c", sourceChannel);
                return cody;
            }

            public override bool PEGI()
            {

                bool changed = false;

                if (enabled) {

                    var rls = TextureRole.all;

                    changed |= pegi.select(ref sourceRole, rls);

                    changed |= rls[sourceRole].PEGI(ref sourceChannel, this);
                }
                pegi.newLine();

                return changed;
            }

            public const string stdTag = "TexChan";

            public override string getDefaultTagName()
            {
                return stdTag;
            }
        }


        public abstract class TextureRole
        {

            static List<TextureRole> _allRoles;
            public static List<TextureRole> all {
                get {
                    if (_allRoles == null){
                        _allRoles = new List<TextureRole>();
                        _allRoles.Add(new TextureRole_Diffuse());
                        _allRoles.Add(new TextureRole_Height());
                        _allRoles.Add(new TextureRole_Normal());
                        _allRoles.Add(new TextureRole_Result());
                        _allRoles.Add(new TextureRole_SingleValue());
                    }
                    return _allRoles;
                }
            }

            public static void Clear() {
                foreach (var r in _allRoles) r._pixels = null;
            } 

            public int index;

            public Color[] _pixels;

            public TextureRole()
            {
                index = _allRoles.Count;
            }


            public virtual bool usingBumpStrengthSlider (int sourceChannel) { return false; } 
            public virtual bool usingColorSelector { get { return false; } }
            public virtual bool isColor { get { return false; } }
            public virtual bool isSingleChannel { get { return true; } }
            public virtual List<string> channels { get { return chans; } }
            public virtual Color DefaultColor { get { return isColor ? Color.white : Color.grey; } }


            static List<string> chans = new List<string> { "R", "G", "B", "A" };

            protected void ExtractPixels (Texture2D tex, int width, int height) {

                
                if (tex != null){
                    try
                    {
                        var importer = tex.getTextureImporter();
                        bool needReimport = importer.wasNotReadable();
                       
                        needReimport |= importer.wasWrongIsColor(isColor);
                        needReimport |= importer.wasMarkedAsNormal();
                        if (isSingleChannel)
                            needReimport |= importer.wasNotSingleChanel();

                        if (needReimport)
                            importer.SaveAndReimport();

                        _pixels = tex.GetPixels(width, height);
                    } catch (Exception ex)
                    {
                        Debug.Log("Pixel extraction from "+tex.name +" failed "+ex.ToString());
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

            public virtual Color[] GetPixels(TextureSetForForCombinedMaps set, imgData id) {

                if (_pixels == null) 
                    ExtractPixels(set.Diffuse, id.width, id.height);
                   
                return _pixels;
            }



            public virtual bool PEGI(ref int selectedChannel, TextureChannel tc)
            {
                bool changed = " = ".select(20,ref selectedChannel, channels).nl();

           

                return changed;
            }

        }

        public class TextureRole_Result : TextureRole {
            public static TextureRole_Result inst;
            public override string ToString() { return "Result"; }

            public TextureRole_Result() {
                inst = this;
            }

            public override Color[] GetPixels(TextureSetForForCombinedMaps set, imgData id)
            {
                return id.pixels;
            }

        }

        public class TextureRole_SingleValue : TextureRole
        {
            public override bool isColor { get { return true; } }
            public override bool isSingleChannel { get { return false; } }
            public override bool usingColorSelector { get  { return true;  } }

            public override string ToString() { return "Value"; }

            public override Color[] GetPixels(TextureSetForForCombinedMaps set, imgData id)
            {

                if (_pixels == null)
                {
                    var col = DefaultColor;
                    var size = id.width * id.height;
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
            public override bool isSingleChannel { get { return false; } }

            public override string ToString() { return "Color"; }

            public override Color[] GetPixels(TextureSetForForCombinedMaps set , imgData id)
            {

                if (_pixels == null)
                    ExtractPixels(set.Diffuse, id.width, id.height);

                return _pixels;
            }


        }

        public class TextureRole_Height : TextureRole
        {
            public override string ToString() { return "Height"; }
            public override bool usingBumpStrengthSlider (int channel) { return channel > 1; }

            public override List<string> channels { get { return chans; } }

            static List<string> chans = new List<string> { "Height", "1 - Height", "Normal R", "Normal G" };

            public override Color[] GetPixels(TextureSetForForCombinedMaps set, imgData id)
            {

                if (_pixels == null)
                    ExtractPixels(set.Height, id.width, id.height);

                return _pixels;
            }

        }

        public class TextureRole_Normal : TextureRole
        {
            public override string ToString() { return "Normal"; }
            public override bool isSingleChannel { get { return false; } }
            public override bool usingBumpStrengthSlider(int channel) { return true; }

            public override Color[] GetPixels(TextureSetForForCombinedMaps set, imgData id)
            {

                if (_pixels == null)
                    ExtractPixels(set.Normal, id.width, id.height);

                return _pixels;
            }
        }

       

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
                            tmp.Product_combinedBump = NormalMapFrom(tmp.normalStrength, 0.1f, tmp.height, tmp.normalMap, tmp.smooth, tmp.ambient, tmp.productName, tmp.Product_combinedBump);
                            if (tmp.colorTexture != null)
                                tmp.Product_colorWithAlpha = HeightToAlpha(tmp.height, tmp.colorTexture, tmp.productName);
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

            public static Texture2D NormalMapFrom(float strength, float diagonalPixelsCoef, Texture2D bump, Texture2D normalReady, Texture2D smoothness, Texture2D ambient, string name, Texture2D Result)
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

                importer = smoothness.getTextureImporter();
                needReimport = importer.wasNotReadable();
                needReimport |= importer.wasWrongIsColor(false);
                needReimport |= importer.wasNotSingleChanel();
                if (needReimport) importer.SaveAndReimport();

                importer = ambient.getTextureImporter();
                needReimport = importer.wasNotReadable();
                needReimport |= importer.wasWrongIsColor(false);
                needReimport |= importer.wasNotSingleChanel();
                if (needReimport) importer.SaveAndReimport();

                try
                {
                    srcBmp = (normalReady != null) ? normalReady.GetPixels(width, height) : bump.GetPixels();
                    srcSm = smoothness.GetPixels(width, height);
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

            public static Texture2D HeightToAlpha(Texture2D bump, Texture2D diffuse, string newName)
            {

                if (bump == null)
                {
                    Debug.Log("No bump texture");
                    return null;
                }

                TextureImporter ti = bump.getTextureImporter();
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


                width = bump.width;
                height = bump.height;
                Color[] dstColor;

                try
                {
                    dstColor = diffuse.GetPixels();
                    srcBmp = bump.GetPixels(diffuse.width, diffuse.height);
                }
                catch (UnityException e)
                {
                    Debug.Log("couldn't read one of the textures for  " + bump.name + " " + e.ToString());
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