using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using System.IO;
#if UNITY_2018_1_OR_NEWER
using Unity.Collections;
using Unity.Jobs;
#endif

namespace Playtime_Painter
{

    public enum TexTarget { Texture2D, RenderTexture }

    public class ImageMeta : PainterStuffKeepUnrecognized_STD, IPEGI, IPEGI_ListInspect, IGotName, INeedAttention, ICanBeDefault_STD
    {

        #region Values
        const float bytetocol = 1f / 255f;
        public static Texture2D sampler;

        public TexTarget destination;
        public RenderTexture renderTexture;
        public Texture2D texture2D;
        public Texture other;
        public int width = 128;
        public int height = 128;
        public bool useTexcoord2;
        public bool useTexcoord2_AutoAssigned = false;
        public bool lockEditing;
        public bool isATransparentLayer;
        public bool NeedsToBeSaved => (texture2D && texture2D.SavedAsAsset()) || (renderTexture && renderTexture.SavedAsAsset());
        public bool showRecording = false;
        public bool enableUndoRedo;
        public bool pixelsDirty = false;
        public bool preserveTransparency = true;
        public bool alphaPreservePixelSet = false;
        public bool errorWhileReading = false;

        public float repaintDelay = 0.016f;
        public int numberOfTexture2Dbackups = 10;
        public int numberOfRenderTextureBackups = 10;
        public bool backupManually = false;
        public Vector2 tiling = Vector2.one;
        public Vector2 offset = Vector2.zero;
        public string SaveName = "No Name";
        public string URL = "";
        public Color[] _pixels;
        public Color clearColor = Color.black;

        public Color[] Pixels
        {
            get { if (_pixels == null) PixelsFromTexture2D(texture2D); return _pixels; }
            set { _pixels = value; }
        }

       

        #endregion

        #region SAVE IN PLAYER

        const string savedImagesFolder = "Saved Images";

        public string SaveInPlayer()
        {
            if (texture2D != null)
            {
                if (destination == TexTarget.RenderTexture)
                    RenderTexture_To_Texture2D();

                var png = texture2D.EncodeToPNG();

                string path = Path.Combine(Application.persistentDataPath, savedImagesFolder);

                Directory.CreateDirectory(path);

                string fullPath = Path.Combine(path, "{0}.png".F(SaveName));

                System.IO.File.WriteAllBytes(fullPath, png);

                string msg = string.Format("Saved {0} to {1}", SaveName, fullPath);

                Cfg.playtimeSavedTextures.Add(fullPath);
#if PEGI
                msg.showNotificationIn3D_Views();
#endif
                Debug.Log(msg);

                return fullPath;
            }

            return "Save Failed";
        }

        public void LoadInPlayer() => LoadInPlayer(SaveName);

        public void LoadInPlayer(string path)
        {
            if (File.Exists(path))
            {
                var fileData = File.ReadAllBytes(path);
                if (!texture2D)
                    texture2D = new Texture2D(2, 2);

                if (texture2D.LoadImage(fileData))
                    Init(texture2D);
#if PEGI
                else "Couldn't Load Image ".showNotificationIn3D_Views();
#endif
            }
        }

        #endregion

        #region Encode Decode

        public bool IsDefault => !NeedsToBeSaved;

        public override StdEncoder Encode()
        {
            var cody = this.EncodeUnrecognized()
            .Add_IfNotZero("dst", (int)destination)
            .Add_Reference("tex2D", texture2D)
            .Add_Reference("other", other)
            .Add("w", width)
            .Add("h", height)
            .Add_IfTrue("useUV2", useTexcoord2)
            .Add_IfTrue("Lock", lockEditing)
            .Add_IfTrue("b", backupManually)
            .Add_IfNotOne("tl", tiling)
            .Add_IfNotZero("off", offset)
            .Add_IfNotEmpty("sn", SaveName)
            .Add_IfTrue("rec", showRecording)
            .Add_IfTrue("trnsp", isATransparentLayer)
            .Add_IfTrue("bu", enableUndoRedo)
            .Add_IfTrue("tc2Auto", useTexcoord2_AutoAssigned)
            .Add_IfNotBlack("clear", clearColor)
            .Add_IfNotEmpty("URL", URL)
            .Add_IfFalse("alpha", preserveTransparency);

            if (enableUndoRedo)
                cody.Add("2dUndo", numberOfTexture2Dbackups)
                .Add("rtBackups", numberOfRenderTextureBackups);

            return cody;
        }

        public override void Decode(string data)
        {
            base.Decode(data);

            if (texture2D) {
                width = texture2D.width;
                height = texture2D.height;
            }
            
        }

        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "dst": destination = (TexTarget)data.ToInt(); break;
                case "tex2D": data.Decode_Reference(ref texture2D); break;
                case "other": data.Decode_Reference(ref other); break;
                case "w": width = data.ToInt(); break;
                case "h": height = data.ToInt(); break;
                case "useUV2": useTexcoord2 = data.ToBool(); break;
                case "Lock": lockEditing = data.ToBool(); break;
                case "2dUndo": numberOfTexture2Dbackups = data.ToInt(); break;
                case "rtBackups": numberOfRenderTextureBackups = data.ToInt(); break;
                case "b": backupManually = data.ToBool(); break;
                case "tl": tiling = data.ToVector2(); break;
                case "off": offset = data.ToVector2(); break;
                case "sn": SaveName = data; break;
                case "trnsp": isATransparentLayer = data.ToBool(); break;
                case "rec": showRecording = data.ToBool(); break;
                case "bu": enableUndoRedo = data.ToBool(); break;
                case "tc2Auto": useTexcoord2_AutoAssigned = data.ToBool(); break;
                case "clear": clearColor = data.ToColor(); break;
                case "URL": URL = data; break;
                case "alpha": preserveTransparency = data.ToBool(); break;
                default: return false;
            }
            return true;
        }

        #endregion

        #region Undo & Redo
        public UndoCache cache = new UndoCache();

        public void Backup()
        {
            if (backupManually) return;

            if (enableUndoRedo)
            {
                if (destination == TexTarget.RenderTexture)
                {
                    if (numberOfRenderTextureBackups > 0)
                        cache.undo.backupRenderTexture(numberOfRenderTextureBackups, this);
                }
                else if (numberOfTexture2Dbackups > 0)
                    cache.undo.backupTexture2D(numberOfRenderTextureBackups, this);
            }

            cache.redo.Clear();

        }
        #endregion

        #region Recordings
        public List<string> recordedStrokes = new List<string>();
        public List<string> recordedStrokes_forUndoRedo = new List<string>(); // to sync strokes recording with Undo Redo
        public bool recording;

        public void StartRecording()
        {
            recordedStrokes = new List<string>();
            recordedStrokes_forUndoRedo = new List<string>();
            recording = true;
        }

        public void ContinueRecording()
        {
            StartRecording();
            recordedStrokes.AddRange(Cfg.StrokeRecordingsFromFile(SaveName));
        }

        public void SaveRecording()
        {

            var allStrokes = new StdEncoder().Add("strokes", recordedStrokes).ToString();

            StuffSaver.SaveToPersistentPath(Cfg.vectorsFolderName, SaveName, allStrokes);

            Cfg.recordingNames.Add(SaveName);

            recording = false;

        }
        #endregion

        #region Texture MGMT
        public void Resize(int size) => Resize(size, size);

        public void Resize(int newWight, int newHeight)
        {

            if (newHeight >= 8 && newHeight <= 4096 && newWight >= 8 && newWight <= 4096 && (newWight != width || newHeight != height) && texture2D)
            {

                var tmp = renderTexture;
                renderTexture = null;

                Texture2D_To_RenderTexture();

                texture2D.Resize(newWight, newHeight);

                width = newWight;

                height = newHeight;

                texture2D.CopyFrom(PainterCamera.Inst.GetDownscaledBigRt(width, height));

                PixelsFromTexture2D(texture2D);

                SetAndApply();
                
                renderTexture = tmp;

                Debug.Log("Resize Complete");
            }

        }

        public bool Contains(Texture tex)
        {
            return tex != null && ((texture2D && tex == texture2D) || (renderTexture && renderTexture == tex) || (other && tex == other));
        }

        public RenderTexture AddRenderTexture() => AddRenderTexture(width, height, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, FilterMode.Bilinear, null);

        public RenderTexture AddRenderTexture(int nwidth, int nheight, RenderTextureFormat format, RenderTextureReadWrite dataType, FilterMode filterMode, string global)
        {

            if (destination == TexTarget.RenderTexture)
                RenderTexture_To_Texture2D();


            width = nwidth;
            height = nheight;

            renderTexture = new RenderTexture(width, height, 0, format, dataType)
            {
                filterMode = filterMode,

                name = SaveName
            };

            if (!global.IsNullOrEmpty())
                Shader.SetGlobalTexture(global, renderTexture);

            if (destination == TexTarget.RenderTexture)
                Texture2D_To_RenderTexture();

            return renderTexture;
        }

        public void Texture2D_To_RenderTexture() => TextureToRenderTexture(texture2D);

        public void TextureToRenderTexture(Texture2D tex) => PainterCamera.Inst.Render(tex, this.CurrentRenderTexture(), TexMGMTdata.pixPerfectCopy);

        public void RenderTexture_To_Texture2D() => RenderTexture_To_Texture2D(texture2D);

        public Texture2D NewTexture2D()
        {

            Texture2D newTex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            
            newTex.SetPixels(Pixels);

            newTex.Apply();

            newTex.name = texture2D.name + "_A";

            texture2D = newTex;
            
            return newTex;
        }

        public void RenderTexture_To_Texture2D(Texture2D tex)
        {
            if (!texture2D)
                return;

            RenderTexture rt = renderTexture;

            if (!rt && TexMGMT.imgMetaUsingRendTex == this)
                rt = PainterCamera.Inst.GetDownscaledBigRt(width, height);

            if (!rt)
                return;

            tex.CopyFrom(rt);

            PixelsFromTexture2D(tex);

            bool converted = false;

            /* MAC: 
                    Linear Space
                        Big RT
                            Editor 
                                Linear Texture = To Linear
                                sRGB Texture = 
                            Playtime
                                Linear Texture = To Linear
                                sRGB Texture = 
                        Exclusive
                            Editor 
                                Linear Texture = 
                                sRGB Texture = 
                            Playtime
                                Linear Texture 
                                sRGB Texture = 
                    Gamma Space
                        Big RT
                            Editor 
                                Linear Texture =
                                sRGB Texture = 
                            Playtime
                                Linear Texture 
                                sRGB Texture = 
                        Exclusive
                            Editor 
                                Linear Texture = 
                                sRGB Texture = 
                            Playtime
                                Linear Texture =
                                sRGB Texture = 
            */


            if (PainterCamera.Inst.isLinearColorSpace)
            {
                if (!tex.IsColorTexture())
                {
                    converted = true;
                    PixelsToLinear();
                }

#if UNITY_2017

                if (renderTexture != null) {
                    PixelsToGamma();
                converted = true;
                }
#endif
            }


            //if (!RenderTexturePainter.inst.isLinearColorSpace)
            //pixelsToLinear ();

            if (converted)
                SetAndApply();
            else
                texture2D.Apply(true);
            // 

        }

        public void ChangeDestination(TexTarget changeTo, MaterialMeta mat, string parameter, PlaytimePainter painter)
        {

            if (changeTo != destination) {

                if (changeTo == TexTarget.RenderTexture)
                {
                    if (!renderTexture)
                        PainterCamera.Inst.ChangeBufferTarget(this, mat, parameter, painter);
                    TextureToRenderTexture(texture2D);
                }
                else
                {
                    if (!texture2D)
                        return;

                    if (!renderTexture)
                        PainterCamera.Inst.EmptyBufferTarget();
                    else
                        if (painter.initialized) // To avoid Clear to black when exiting playmode
                        RenderTexture_To_Texture2D();

                }
                destination = changeTo;
                painter.SetTextureOnMaterial(this);

            }
            else Debug.Log("Destination already Set");

        }
        
        public void SetPixelsInRAM() => texture2D.SetPixels(_pixels);
        
        public void Apply_ToGPU(bool mipmaps = true) => texture2D.Apply(mipmaps, false);

        public void SetAndApply(bool mipmaps = true) {
            if (_pixels != null) {
                SetPixelsInRAM();
                Apply_ToGPU(mipmaps);
            }
        }
        #endregion
        
        #region Pixels MGMT

        public void UnsetAlphaSavePixel() {
            if (alphaPreservePixelSet)
            {
                _pixels[0].a = 1;
                SetAndApply();
            }
        }

        public void SetAlphaSavePixel()  {

            if (preserveTransparency && Pixels[0].a == 1) {
                _pixels[0].a = 0.9f;
                alphaPreservePixelSet = true;
                SetPixel_InRAM(0, 0);
            }

        }

        public void SetPixel_InRAM(int x, int y) => texture2D.SetPixel(x, y, _pixels[PixelNo(x, y)]);
        
        public void PixelsToGamma()
        {
            var p = Pixels;
            for (int i = 0; i < p.Length; i++)
                _pixels[i] = _pixels[i].gamma;
        }

        public void PixelsToLinear()
        {
            var p = Pixels;
            for (int i = 0; i < p.Length; i++)
                _pixels[i] = _pixels[i].linear;
        }

        void UVto01(ref Vector2 uv)
        {
            uv.x %= 1;
            uv.y %= 1;
            if (uv.x < 0) uv.x += 1;
            if (uv.y < 0) uv.y += 1;
        }

        public bool Colorize(Color col, bool creatingNewTexture = false)
        {
            // When first creating texture Alpha value should not be 1 otherwise texture will be encoded to RGB and not RGBA 
            bool needsRecolorizingAfterSave = false;

#if UNITY_EDITOR
            if (creatingNewTexture && col.a == 1)
            {
                needsRecolorizingAfterSave = true;
                col.a = 0.5f;
            }
#endif

            for (int i = 0; i < Pixels.Length; i++)
                _pixels[i] = col;

            return needsRecolorizingAfterSave;
        }

        public Color SampleAT(Vector2 uv) => (destination == TexTarget.Texture2D) ? Pixel(UvToPixelNumber(uv)) : SampleRenderTexture(uv);

        public Color SampleRenderTexture(Vector2 uv)
        {

            RenderTexture curRT = RenderTexture.active;

            PainterCamera rtp = PainterCamera.Inst;
            int size = PainterCamera.RenderTextureSize / 4;
            RenderTexture.active = renderTexture ? renderTexture : rtp.GetDownscaledBigRt(size, size);

            if (!sampler) sampler = new Texture2D(8, 8);

            UVto01(ref uv);

            if (!renderTexture)
                uv.y = 1 - uv.y; // For some reason sampling is mirrored around Y axiz for BigRenderTexture (?)

            uv *= RenderTexture.active.width;

            sampler.ReadPixels(new Rect(uv.x, uv.y, 1, 1), 0, 0);

            RenderTexture.active = curRT;

            var pix = sampler.GetPixel(0, 0);

            if (PainterCamera.Inst.isLinearColorSpace)
                pix = pix.linear;

            return pix;
        }

        public void PixelsFromTexture2D(Texture2D tex, bool userClickedRetry = false) {

            if (userClickedRetry || !errorWhileReading)
            {
                try
                {
                    if (tex)
                    {
                        Pixels = tex.GetPixels();
                        width = tex.width;
                        height = tex.height;
                        errorWhileReading = false;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("Reading of {0} failed: {1}".F(tex, ex.ToString()));
                    errorWhileReading = true;
                }
            }
        }

        public Color Pixel(MyIntVec2 v)
        {
            v.x %= width;
            while (v.x < 0)
                v.x += width;

            v.y %= height;
            while (v.y < 0)
                v.y += height;

            return Pixels[((int)v.y) * width + (int)v.x];
        }

        public int PixelNo(MyIntVec2 v)
        {
            int x = v.x;
            int y = v.y;

            x %= width;
            if (x < 0)
                x += width;
            y %= height;
            if (y < 0)
                y += height;
            return y * width + x;
        }

        public int PixelNo(int x, int y)
        {
            x %= width;
            if (x < 0)
                x += width;
            y %= height;
            if (y < 0)
                y += height;
            return y * width + x;
        }

        public MyIntVec2 UvToPixelNumber(Vector2 uv) => new MyIntVec2(uv.x * width, uv.y * height);

        #endregion
        
        #region BlitJobs
#if UNITY_2018_1_OR_NEWER
        public NativeArray<Color> pixelsForJob;
        public JobHandle jobHandle;

        public bool CanUsePixelsForJob()
        {
            if (!pixelsForJob.IsCreated && _pixels != null)
            {

                pixelsForJob = new NativeArray<Color>(_pixels, Allocator.Persistent);

                TexMGMT.blitJobsActive.Add(this);

                return true;
            }

            return false;
        }

        public void CompleteJob()
        {
            if (pixelsForJob.IsCreated)
            {

                jobHandle.Complete();
                _pixels = pixelsForJob.ToArray();
                pixelsForJob.Dispose();
                TexMGMT.blitJobsActive.Remove(this);
                SetAndApply();
            }
        }
#endif
        #endregion

        #region Init

        public ImageMeta Init(int renderTextureSize)
        {
            width = renderTextureSize;
            height = renderTextureSize;
            AddRenderTexture();
            TexMGMTdata.imgMetas.Insert(0, this);
            destination = TexTarget.RenderTexture;
            return this;
        }

        public ImageMeta Init(Texture tex)
        {

            if (tex.GetType() == typeof(Texture2D))
                UseTex2D((Texture2D)tex);
            else
                 if (tex.GetType() == typeof(RenderTexture))
                UseRenderTexture((RenderTexture)tex);
            else
                other = tex;

            if (!TexMGMTdata.imgMetas.Contains(this))
                TexMGMTdata.imgMetas.Insert(0, this);
            return this;
        }
        
        public void FromRenderTextureToNewTexture2D()
        {
            texture2D = new Texture2D(width, height);
            RenderTexture_To_Texture2D();
        }

        public void From(Texture2D texture, bool userClickedRetry = false) {

            texture2D = texture;
            SaveName = texture.name;

            if (userClickedRetry || !errorWhileReading) {

                #if UNITY_EDITOR
                if (texture) {

                    var imp = texture.GetTextureImporter();
                    if (imp != null) {

                        isATransparentLayer = imp.alphaIsTransparency;

                        texture.Reimport_IfNotReadale();
                    }
                }
                #endif

                PixelsFromTexture2D(texture2D, userClickedRetry);
            }
        }
        
        void UseRenderTexture(RenderTexture rt)
        {
            renderTexture = rt;
            width = rt.width;
            height = rt.height;
            destination = TexTarget.RenderTexture;

#if UNITY_EDITOR
            string path = AssetDatabase.GetAssetPath(rt);
            if (!string.IsNullOrEmpty(path))
            {
                SaveName = rt.name;
                // saved = true;
            }
            else
#endif
               if (SaveName.IsNullOrEmpty())
                SaveName = "New img";
        }

        void UseTex2D(Texture2D tex)
        {

            From(tex);
            destination = TexTarget.Texture2D;
#if UNITY_EDITOR
            string path = AssetDatabase.GetAssetPath(tex);
            if (!path.IsNullOrEmpty())
                SaveName = tex.name;
            else
#endif
                if (SaveName.IsNullOrEmpty())
                SaveName = "New img";
        }

        #endregion

        #region Inspector
        public string NameForPEGI
        {
            get { return SaveName; }

            set { SaveName = value; }
        }
        
        #if PEGI

        bool LoadTexturePEGI(string path)
        {
            bool changed = false;

            if ("Load {0}".F(path.Substring(path.LastIndexOf("/"))).Click())
                LoadInPlayer(path);

            return changed;
        }

        int inspectedProcess = -1;
        public int inspectedStuff = -1;
     
        public override bool Inspect()
        {

            bool changed = false;

            if ("CPU blit options".conditional_enter(this.TargetIsTexture2D(), ref inspectedStuff, 0).nl())
            {
                changed |= "CPU blit repaint delay".edit("Delay for video memory update when painting to Texture2D", 140, ref repaintDelay, 0.01f, 0.5f).nl();
                
                changed |= "Don't update mipmaps:".toggleIcon("May increase performance, but your changes may not disaplay if you are far from texture.",
                    ref GlobalBrush.DontRedoMipmaps).nl();
            }

            if ("Save Textures In Game".enter(ref inspectedStuff, 1).nl()) {

                "Save Name".edit(70, ref SaveName);
                
                if (icon.Folder.Click("Open Folder with textures").nl())
                    StuffExplorer.OpenPersistentFolder(savedImagesFolder);

                if ("Save Playtime".Click("Will save to {0}/{1}".F(Application.persistentDataPath, SaveName)).nl())
                    SaveInPlayer();

                if (Cfg && Cfg.playtimeSavedTextures.Count > 0)
                    "Playtime Saved Textures".write_List(Cfg.playtimeSavedTextures, LoadTexturePEGI);
            }

            #region Processors

            int newWidth = Cfg.SelectedWidthForNewTexture(); //PainterDataAndConfig.SizeIndexToSize(PainterCamera.Data.selectedWidthIndex);
            int newHeight = Cfg.SelectedHeightForNewTexture();

            if ("Texture Processors".enter(ref inspectedStuff, 6).nl_ifFolded()) {


                "<-Return".nl(PEGI_Styles.ListLabel);

                if (errorWhileReading)
                    "There was en error reading texture pixels, can't process it".writeWarning();
                else
                {
                    if ("Resize ({0}*{1}) => ({2}*{3})".F(width, height, newWidth, newHeight).enter(ref inspectedProcess, 0).nl_ifFoldedOut())
                    {
                        "New Width ".select(60, ref PainterCamera.Data.selectedWidthIndex, PainterDataAndConfig.NewTextureSizeOptions).nl(ref changed);

                        "New Height ".select(60, ref PainterCamera.Data.selectedHeightIndex, PainterDataAndConfig.NewTextureSizeOptions).nl(ref changed);


                        if (newWidth != width || newHeight != height)
                        {

                            bool rescale = false;

                            if (newWidth <= width && newHeight <= height)
                                rescale = "Downscale".Click();
                            else if (newWidth >= width && newHeight >= height)
                                rescale = "Upscale".Click();
                            else
                                rescale = "Rescale".Click();

                            if (rescale)
                                Resize(newWidth, newHeight);
                        }
                        pegi.nl();
                    }

                    if (inspectedProcess == -1)
                    {
                        if ((newWidth != width || newHeight != height) && icon.Replace.Click("Resize").nl(ref changed))
                            Resize(newWidth, newHeight);

                        pegi.nl();
                    }

                    if ("Colorize ".enter(ref inspectedProcess, 1))
                    {

                        "Clear Color".edit(80, ref clearColor).nl();
                        if ("Clear Texture".Click().nl())
                        {
                            Colorize(clearColor);
                            SetAndApply();
                        }
                    }

                    if (inspectedProcess == -1 && icon.Refresh.Click("Apply color {0}".F(clearColor)).nl())
                    {
                        Colorize(clearColor);
                        SetAndApply();
                    }
                }

                if ("Render Buffer Debug".enter(ref inspectedProcess, 3).nl()) {
                    pegi.write(TexMGMT.bigRtPair[0], 200);
                    pegi.nl();
                    pegi.write(TexMGMT.bigRtPair[1], 200);

                    pegi.nl();
                }
            }

            #endregion

            if ("Undo Redo".toggle_enter(ref enableUndoRedo, ref inspectedStuff, 2, ref changed).nl())
            {
                
                "UNDOs: Tex2D".edit(80, ref numberOfTexture2Dbackups).changes(ref changed);
                "RendTex".edit(60, ref numberOfRenderTextureBackups).nl(ref changed);

                "Backup manually".toggleIcon(ref backupManually).nl();

                if (numberOfTexture2Dbackups > 50 || numberOfRenderTextureBackups > 50)
                    "Too big of a number will eat up lot of memory".writeWarning();

              "Creating more backups will eat more memory".writeOneTimeHint("backupIsMem");
               "This are not connected to Unity's Undo/Redo because when you run out of backups you will by accident start undoing other stuff.".writeOneTimeHint("noNativeUndo");
               "Use Z/X to undo/redo".writeOneTimeHint("ZXundoRedo");

            }

            if ("Color Schemes".toggle_enter(ref Cfg.showColorSchemes, ref inspectedStuff, 5, ref changed).nl())
            {
                if (Cfg.colorSchemes.Count == 0)
                    Cfg.colorSchemes.Add(new ColorScheme() { PaletteName = "New Color Scheme" });

                changed |= pegi.edit_List(ref Cfg.colorSchemes, ref Cfg.inspectedColorScheme);
            }

            return changed;
        }

        public bool ComponentDependent_PEGI(bool showToggles, PlaytimePainter painter)
        {
            var changed = false;

            var property = painter.GetMaterialTexturePropertyName;

            var forceOpenUTransparentLayer = false;

            var hasAlphaLayerTag = painter.Material.HasTag(PainterDataAndConfig.TransparentLayerExpected + property);

            if (!isATransparentLayer && hasAlphaLayerTag)
            {
                "Material Field {0} is a Transparent Layer ".F(property).writeHint();
                forceOpenUTransparentLayer = true;
            }

            if (showToggles || (isATransparentLayer && !hasAlphaLayerTag) || forceOpenUTransparentLayer)
                "Transparent Layer".toggleIcon(ref isATransparentLayer).nl(ref changed);

            if (showToggles) {
                if (isATransparentLayer)
                    preserveTransparency = true;
                else
                    "Preserve Transparency".toggleIcon(ref preserveTransparency).nl(ref changed);
            }

            var forceOpenUv2 = false;
            var hasUv2Tag = painter.Material.HasTag(PainterDataAndConfig.TextureSampledWithUV2 + property);

            if (!useTexcoord2 && hasUv2Tag) {

                if (!useTexcoord2_AutoAssigned)
                {
                    useTexcoord2 = true;
                    useTexcoord2_AutoAssigned = true;
                }
                else
                    "Material Field {0} is Sampled using Texture Coordinates 2 ".F(property).writeHint();
                forceOpenUv2 = true;
            }

            if (showToggles || (useTexcoord2 && !hasUv2Tag) || forceOpenUv2)
                changed |= "Use Texcoord 2".toggleIcon(ref useTexcoord2).nl();

            return changed;
        }
        
        public bool Undo_redo_PEGI()
        {
            var changed = false;

            if (cache == null) cache = new UndoCache();
            if (recordedStrokes == null) recordedStrokes = new List<string>();
            if (recordedStrokes_forUndoRedo == null) recordedStrokes_forUndoRedo = new List<string>(); // to sync strokes recording with Undo Redo

            if (cache.undo.GotData) {
                if (icon.Undo.Click("Press Z to undo (Scene View)",ref changed, 25))
                    cache.undo.ApplyTo(this);
            }
            else
                icon.UndoDisabled.write("Nothing to Undo (set number of undo frames in config)", 25);

            if (cache.redo.GotData) {
                if (icon.Redo.Click("X to Redo", ref changed ,25))
                    cache.redo.ApplyTo(this);
            }
            else
                icon.RedoDisabled.write("Nothing to Redo", 25);

            pegi.nl();

#if UNITY_EDITOR
            if (recording) {
                ("Recording... " + recordedStrokes.Count + " vectors").nl();
                "Will Save As ".edit(70, ref SaveName);

                if (icon.Close.Click("Stop, don't save", 25))
                    recording = false;
                if (icon.Done.Click("Finish & Save", 25))
                    SaveRecording();

                pegi.newLine();
            }
#endif
          
            return changed;
        }

        public bool PEGI_inList(IList list, int ind, ref int edited)
        {
            this.ToPEGIstring().write_obj(60, texture2D);
            if (this.Click_Enter_Attention())
                edited = ind;
            texture2D.ClickHighlight();

            return false;
        }

        public string NeedAttention()
        {
            if (numberOfTexture2Dbackups > 50)
                return "Too many backups";
            return null;
        }

        #endif
        #endregion

        private float _repaintTimer;
        public void Update(bool mouseUp)
        {

            if (!pixelsDirty) return;
            
            _repaintTimer -= Application.isPlaying ? Time.deltaTime : 0.016f;

            if (_repaintTimer >= 0 && !mouseUp) return;
            
            if (texture2D)
                SetAndApply(!GlobalBrush.DontRedoMipmaps);

            pixelsDirty = false;
            _repaintTimer = repaintDelay;
        }
    }

}