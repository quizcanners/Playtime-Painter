using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using System.IO;
using Unity.Collections;
using Unity.Jobs;

namespace Playtime_Painter
{

    public enum TexTarget { Texture2D, RenderTexture }

    public class ImageMeta : PainterSystemKeepUnrecognizedCfg, IPEGI_ListInspect, IGotName, INeedAttention, ICanBeDefaultCfg
    {

        #region Values

        private static Texture2D _sampler;

        public TexTarget destination;
        public RenderTexture renderTexture;
        public Texture2D texture2D;
        public Texture other;
        public int width = 128;
        public int height = 128;
        public bool useTexCoord2;
        private bool _useTexCoord2AutoAssigned;
        public bool lockEditing;
        public bool isATransparentLayer;
        public bool NeedsToBeSaved => (texture2D && texture2D.SavedAsAsset()) || (renderTexture && renderTexture.SavedAsAsset());
        public bool showRecording;
        public bool enableUndoRedo;
        public bool pixelsDirty;
        public bool preserveTransparency = true;
        private bool _alphaPreservePixelSet;
        public bool errorWhileReading;
        public bool dontRedoMipMaps;
        

        private float sdfMaxInside = 1f;
        private float sdfMaxOutside = 1f;
        private float sdfPostProcessDistance = 1f;
        

        private float _repaintDelay = 0.016f;
        private int _numberOfTexture2DBackups = 10;
        private int _numberOfRenderTextureBackups = 10;
        public bool backupManually;
        public Vector2 tiling = Vector2.one;
        public Vector2 offset = Vector2.zero;
        public string saveName = "No Name";
        public string url = "";
        private Color[] _pixels;
        private Color _clearColor = Color.black;

        public Color[] Pixels
        {
            get { if (_pixels == null) PixelsFromTexture2D(texture2D); return _pixels; }
            set { _pixels = value; }
        }

       

        #endregion

        #region SAVE IN PLAYER

        const string SavedImagesFolder = "Saved Images";

        public string SaveInPlayer()
        {
            if (texture2D == null) return "Save Failed";
            
            if (destination == TexTarget.RenderTexture)
                RenderTexture_To_Texture2D();

            var png = texture2D.EncodeToPNG();

            var path = Path.Combine(Application.persistentDataPath, SavedImagesFolder);

            Directory.CreateDirectory(path);

            var fullPath = Path.Combine(path, "{0}.png".F(saveName));

            File.WriteAllBytes(fullPath, png);

            var msg = $"Saved {saveName} to {fullPath}";

            Cfg.playtimeSavedTextures.Add(fullPath);
#if PEGI
            msg.showNotificationIn3D_Views();
#endif
            Debug.Log(msg);

            return fullPath;

        }

        public void LoadInPlayer() => LoadInPlayer(saveName);

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

        public override CfgEncoder Encode()
        {
            var cody = this.EncodeUnrecognized()
            .Add_IfNotZero("dst", (int)destination)
            .Add_Reference("tex2D", texture2D)
            .Add_Reference("other", other)
            .Add("w", width)
            .Add("h", height)
            .Add_IfTrue("useUV2", useTexCoord2)
            .Add_IfTrue("Lock", lockEditing)
            .Add_IfTrue("b", backupManually)
            .Add_IfNotOne("tl", tiling)
            .Add_IfNotZero("off", offset)
            .Add_IfNotEmpty("sn", saveName)
            .Add_IfTrue("rec", showRecording)
            .Add_IfTrue("trnsp", isATransparentLayer)
            .Add_IfTrue("bu", enableUndoRedo)
            .Add_IfTrue("tc2Auto", _useTexCoord2AutoAssigned)
            .Add_IfTrue("dumm", dontRedoMipMaps)
           
            .Add_IfNotBlack("clear", _clearColor)
            .Add_IfNotEmpty("URL", url)
            .Add_IfNotNegative("is", inspectedItems)
            .Add_IfFalse("alpha", preserveTransparency);
            
           if (sdfMaxInside != 1f)
               cody.Add("sdfMI", sdfMaxInside);
           if (sdfMaxOutside != 1f)
               cody.Add("sdfMO", sdfMaxOutside);
           if (sdfPostProcessDistance != 1f)
               cody.Add("sdfMD", sdfPostProcessDistance);
           
            if (enableUndoRedo)
                cody.Add("2dUndo", _numberOfTexture2DBackups)
                .Add("rtBackups", _numberOfRenderTextureBackups);

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
                case "useUV2": useTexCoord2 = data.ToBool(); break;
                case "Lock": lockEditing = data.ToBool(); break;
                case "2dUndo": _numberOfTexture2DBackups = data.ToInt(); break;
                case "rtBackups": _numberOfRenderTextureBackups = data.ToInt(); break;
                case "b": backupManually = data.ToBool(); break;
                case "tl": tiling = data.ToVector2(); break;
                case "off": offset = data.ToVector2(); break;
                case "sn": saveName = data; break;
                case "trnsp": isATransparentLayer = data.ToBool(); break;
                case "rec": showRecording = data.ToBool(); break;
                case "bu": enableUndoRedo = data.ToBool(); break;
                case "dumm": dontRedoMipMaps = data.ToBool(); break;
                case "tc2Auto": _useTexCoord2AutoAssigned = data.ToBool(); break;
                case "clear": _clearColor = data.ToColor(); break;
                case "URL": url = data; break;
                case "alpha": preserveTransparency = data.ToBool(); break;
                case "is": inspectedItems = data.ToInt(); break;
                case "sdfMI": sdfMaxInside = data.ToFloat(); break;
                case "sdfMO": sdfMaxOutside = data.ToFloat(); break;
                case "sdfMD": sdfPostProcessDistance = data.ToFloat(); break;
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
                    if (_numberOfRenderTextureBackups > 0)
                        cache.undo.BackupRenderTexture(_numberOfRenderTextureBackups, this);
                }
                else if (_numberOfTexture2DBackups > 0)
                    cache.undo.BackupTexture2D(_numberOfRenderTextureBackups, this);
            }

            cache.redo.Clear();

        }
        #endregion

        #region Recordings
        public List<string> recordedStrokes = new List<string>();
        public List<string> recordedStrokesForUndoRedo = new List<string>(); // to sync strokes recording with Undo Redo
        public bool recording;

        public void StartRecording()
        {
            recordedStrokes = new List<string>();
            recordedStrokesForUndoRedo = new List<string>();
            recording = true;
        }

        public void ContinueRecording()
        {
            StartRecording();
            recordedStrokes.AddRange(Cfg.StrokeRecordingsFromFile(saveName));
        }

        public void SaveRecording()
        {

            var allStrokes = new CfgEncoder().Add("strokes", recordedStrokes).ToString();

            FileSaveUtils.SaveJsonToPersistentPath(Cfg.vectorsFolderName, saveName, allStrokes);

            Cfg.recordingNames.Add(saveName);

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

                name = saveName
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

            var rt = renderTexture;

            if (!rt && TexMGMT.imgMetaUsingRendTex == this)
                rt = PainterCamera.Inst.GetDownscaledBigRt(width, height);

            if (!rt)
                return;

            tex.CopyFrom(rt);

            PixelsFromTexture2D(tex);

            var converted = false;

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

        public void ChangeDestination(TexTarget changeTo, MaterialMeta mat, ShaderProperty.TextureValue parameter, PlaytimePainter painter)
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
        
        public void SetPixelsInRam() => texture2D.SetPixels(_pixels);

        public void ApplyToTexture2D(bool mipMaps = true) => texture2D.Apply(mipMaps, false);
        
        public void SetAndApply(bool mipMaps = true) {
            if (_pixels == null) return;
            SetPixelsInRam();
            ApplyToTexture2D(mipMaps);
        }

        private void SetApplyUpdateRenderTexture(bool mipMaps = true)
        {
            SetAndApply(mipMaps);
            if (destination == TexTarget.RenderTexture)
                Texture2D_To_RenderTexture();
        }

        #endregion
        
        #region Pixels MGMT

        public void UnsetAlphaSavePixel() {
            if (_alphaPreservePixelSet)
            {
                _pixels[0].a = 1;
                SetAndApply();
            }
        }

        public void SetAlphaSavePixel()  {
            if (!preserveTransparency || !(Math.Abs(Pixels[0].a - 1) < float.Epsilon)) return;
            
            _pixels[0].a = 0.9f;
            _alphaPreservePixelSet = true;
            SetPixel_InRAM(0, 0);

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
            var needsReColorizingAfterSave = false;

#if UNITY_EDITOR
            if (creatingNewTexture && Math.Abs(col.a - 1) < float.Epsilon)
            {
                needsReColorizingAfterSave = true;
                col.a = 0.5f;
            }
#endif

            for (var i = 0; i < Pixels.Length; i++)
                _pixels[i] = col;

            return needsReColorizingAfterSave;
        }

        public Color SampleAt(Vector2 uv) => (destination == TexTarget.Texture2D) ? PixelSafe(UvToPixelNumber(uv)) : SampleRenderTexture(uv);

        private Color SampleRenderTexture(Vector2 uv)
        {

            var curRt = RenderTexture.active;

            var rtp = PainterCamera.Inst;
            const int size = PainterCamera.RenderTextureSize / 4;
            RenderTexture.active = renderTexture ? renderTexture : rtp.GetDownscaledBigRt(size, size);

            if (!_sampler) _sampler = new Texture2D(8, 8);

            UVto01(ref uv);

            if (!renderTexture)
                uv.y = 1 - uv.y; // For some reason sampling is mirrored around Y axiz for BigRenderTexture (?)

            uv *= RenderTexture.active.width;

            _sampler.ReadPixels(new Rect(uv.x, uv.y, 1, 1), 0, 0);

            RenderTexture.active = curRt;

            var pix = _sampler.GetPixel(0, 0);

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

        public Color PixelSafe(MyIntVec2 v)
        {
            v.x %= width;
            while (v.x < 0)
                v.x += width;

            v.y %= height;
            while (v.y < 0)
                v.y += height;

            return Pixels[v.y * width + v.x];
        }

        public Color PixelUnSafe(int x, int y) => Pixels[y * width + x];

        public Color SetPixelUnSafe(int x, int y, Color col) => Pixels[y * width + x] = col;
        
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

        public MyIntVec2 UvToPixelNumber(Vector2 uv) => new MyIntVec2(Mathf.Round(uv.x * width), Mathf.Round(uv.y * height));

        public MyIntVec2 UvToPixelNumber(Vector2 uv, out Vector2 pixelOffset)
        {
            uv *= new Vector2(width, height);
            var result = new MyIntVec2(Mathf.Round(uv.x), Mathf.Round(uv.y));

            pixelOffset = new Vector2(uv.x - result.x - 0.5f, uv.y - result.y - 0.5f);

            return result;
        }

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

            var t2D = tex as Texture2D;
            
            if (t2D)
                UseTex2D(t2D);
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
            saveName = texture.name;

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
                saveName = rt.name;
                // saved = true;
            }
            else
#endif
               if (saveName.IsNullOrEmpty())
                saveName = "New img";
        }

        void UseTex2D(Texture2D tex)
        {

            From(tex);
            destination = TexTarget.Texture2D;
#if UNITY_EDITOR
            string path = AssetDatabase.GetAssetPath(tex);
            if (!path.IsNullOrEmpty())
                saveName = tex.name;
            else
#endif
                if (saveName.IsNullOrEmpty())
                saveName = "New img";
        }

        #endregion

        #region Inspector
        public string NameForPEGI
        {
            get { return saveName; }

            set { saveName = value; }
        }
        
        private int _inspectedProcess = -1;
        public int inspectedItems = -1;

        #if PEGI

        private bool LoadTexturePegi(string path)
        {
            const bool changed = false;

            if ("Load {0}".F(path.Substring(path.LastIndexOf("/", StringComparison.Ordinal))).Click())
                LoadInPlayer(path);

            return changed;
        }

        public override bool Inspect()
        {

            var changed = false;

            if ("CPU blit options".conditional_enter(this.TargetIsTexture2D(), ref inspectedItems, 0).nl())
            {
                "CPU blit repaint delay".edit("Delay for video memory update when painting to Texture2D", 140, ref _repaintDelay, 0.01f, 0.5f).nl(ref changed);
                
                "Don't update mipMaps:".toggleIcon("May increase performance, but your changes may not disaplay if you are far from texture.",
                    ref dontRedoMipMaps).nl(ref changed);
            }

            if ("Save Textures In Game".enter(ref inspectedItems, 1).nl()) {

                "Save Name".edit(70, ref saveName);
                
                if (icon.Folder.Click("Open Folder with textures").nl())
                    FileExplorerUtils.OpenPersistentFolder(SavedImagesFolder);

                if ("Save Playtime".Click("Will save to {0}/{1}".F(Application.persistentDataPath, saveName)).nl())
                    SaveInPlayer();

                if (Cfg && Cfg.playtimeSavedTextures.Count > 0)
                    "Playtime Saved Textures".write_List(Cfg.playtimeSavedTextures, LoadTexturePegi);
            }

            #region Processors

            var newWidth = Cfg.SelectedWidthForNewTexture(); //PainterDataAndConfig.SizeIndexToSize(PainterCamera.Data.selectedWidthIndex);
            var newHeight = Cfg.SelectedHeightForNewTexture();

            if ("Texture Processors".enter(ref inspectedItems, 6).nl_ifFolded()) {


                "<-Return".nl(PEGI_Styles.ListLabel);

                if (errorWhileReading)
                    "There was en error reading texture pixels, can't process it".writeWarning();
                else
                {
                    if ("Resize ({0}*{1}) => ({2}*{3})".F(width, height, newWidth, newHeight).enter(ref _inspectedProcess, 0).nl_ifFoldedOut())
                    {
                        "New Width ".select(60, ref PainterCamera.Data.selectedWidthIndex, PainterDataAndConfig.NewTextureSizeOptions).nl(ref changed);

                        "New Height ".select(60, ref PainterCamera.Data.selectedHeightIndex, PainterDataAndConfig.NewTextureSizeOptions).nl(ref changed);


                        if (newWidth != width || newHeight != height)
                        {

                            bool rescale;

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

                    if (_inspectedProcess == -1)
                    {
                        if ((newWidth != width || newHeight != height) && icon.Replace.Click("Resize").nl(ref changed))
                            Resize(newWidth, newHeight);

                        pegi.nl();
                    }

                    if ("Colorize ".enter(ref _inspectedProcess, 1))
                    {

                        "Clear Color".edit(80, ref _clearColor).nl();
                        if ("Clear Texture".Click().nl())
                        {
                            Colorize(_clearColor);
                            SetApplyUpdateRenderTexture();
                        }
                    }

                    if (_inspectedProcess == -1 && icon.Refresh.Click("Apply color {0}".F(_clearColor)).nl())
                    {
                        Colorize(_clearColor);
                        SetApplyUpdateRenderTexture();
                    }
                }

                if ("Render Buffer Debug".enter(ref _inspectedProcess, 3).nl()) {
                    TexMGMT.bigRtPair[0].write(200);
                    pegi.nl();
                    TexMGMT.bigRtPair[1].write(200);

                    pegi.nl();
                }
            }

            #endregion

            if ("Enable Undo for {0}".F(NameForPEGI).toggle_enter(ref enableUndoRedo, ref inspectedItems, 2, ref changed).nl())
            {
                
                "UNDOs: Tex2D".edit(80, ref _numberOfTexture2DBackups).changes(ref changed);
                "RendTex".edit(60, ref _numberOfRenderTextureBackups).nl(ref changed);

                "Backup manually".toggleIcon(ref backupManually).nl();

                if (_numberOfTexture2DBackups > 50 || _numberOfRenderTextureBackups > 50)
                    "Too big of a number will eat up lot of memory".writeWarning();

              "Creating more backups will eat more memory".writeOneTimeHint("backupIsMem");
               "This are not connected to Unity's Undo/Redo because when you run out of backups you will by accident start undoing other operations.".writeOneTimeHint("noNativeUndo");
               "Use Z/X to undo/redo".writeOneTimeHint("ZxUndoRedo");

            }

            if ("Color Schemes".toggle_enter(ref Cfg.showColorSchemes, ref inspectedItems, 5, ref changed).nl_ifFolded())
            {
                if (Cfg.colorSchemes.Count == 0)
                    Cfg.colorSchemes.Add(new ColorScheme() { paletteName = "New Color Scheme" });

                pegi.edit_List(ref Cfg.colorSchemes, ref Cfg.inspectedColorScheme).changes(ref changed);
            }

            return changed;
        }

        public bool ComponentDependent_PEGI(bool showToggles, PlaytimePainter painter)
        {
            var changed = false;

            var property = painter.GetMaterialTextureProperty;

            var forceOpenUTransparentLayer = false;

            var hasAlphaLayerTag =
                painter.Material.Has(property, ShaderTags.LayerTypes.Transparent);//GetTag(PainterDataAndConfig.ShaderTagLayerType + property, false).Equals("Transparent");

            if (!isATransparentLayer && hasAlphaLayerTag) {
                "Material Field {0} is a Transparent Layer ".F(property).writeHint();
                forceOpenUTransparentLayer = true;
            }

            if (showToggles || (isATransparentLayer && !hasAlphaLayerTag) || forceOpenUTransparentLayer)
            {
                "Transparent Layer".toggleIcon(ref isATransparentLayer).changes(ref changed);
                "Toggle this on if texture has transparent(invisible) areas which contains color you don't want to see"
                .fullWindowDocumentationWithLinkClick("https://www.quizcanners.com/single-post/2018/09/30/Why-do-I-get-black-outline-around-the-stroke", "More About it");

                pegi.nl();
            }
            
            if (showToggles)
            {


                "Use Masks".toggleIcon(ref GlobalBrush.useMask).nl(ref changed);

                if (isATransparentLayer)
                    preserveTransparency = true;
                else {
                    "Preserve Transparency".toggleIcon(ref preserveTransparency).changes(ref changed);

                    ("if every pixel of texture has alpha = 1 (Max) Unity will be save it as .png without transparency. To counter this " +
                     " I set first pixels to alpha 0.9. I know it is hacky, it you know a better way, let me know")
                        .fullWindowDocumentationClick();

                    pegi.nl();
                }
            }

            var forceOpenUv2 = false;
            var hasUv2Tag = painter.Material.Has(property, ShaderTags.SamplingModes.Uv2);

            if (!useTexCoord2 && hasUv2Tag) {

                if (!_useTexCoord2AutoAssigned)
                {
                    useTexCoord2 = true;
                    _useTexCoord2AutoAssigned = true;
                }
                else
                    "Material Field {0} is Sampled using Texture Coordinates 2 ".F(property).writeHint();
                forceOpenUv2 = true;
            }

            if (showToggles || (useTexCoord2 && !hasUv2Tag) || forceOpenUv2)
                changed |= "Use UV2".toggleIcon(ref useTexCoord2).nl();

            return changed;
        }
        
        public bool Undo_redo_PEGI()
        {
            var changed = false;

            if (cache == null) cache = new UndoCache();
            if (recordedStrokes == null) recordedStrokes = new List<string>();
            if (recordedStrokesForUndoRedo == null) recordedStrokesForUndoRedo = new List<string>(); // to sync strokes recording with Undo Redo

            if (cache.undo.GotData) {
                if (icon.Undo.Click("Press Z to undo (Scene View)",ref changed))
                    cache.undo.ApplyTo(this);
            }
            else
                icon.UndoDisabled.write("Nothing to Undo (set number of undo frames in config)");

            if (cache.redo.GotData) {
                if (icon.Redo.Click("X to Redo", ref changed ))
                    cache.redo.ApplyTo(this);
            }
            else
                icon.RedoDisabled.write("Nothing to Redo");

            pegi.nl();

#if UNITY_EDITOR
            if (recording) {
                ("Recording... " + recordedStrokes.Count + " vectors").nl();
                "Will Save As ".edit(70, ref saveName);

                if (icon.Close.Click("Stop, don't save"))
                    recording = false;
                if (icon.Done.Click("Finish & Save"))
                    SaveRecording();

                pegi.newLine();
            }
#endif
          
            return changed;
        }

        public bool PEGI_inList(IList list, int ind, ref int edited)
        {
            this.ToPegiString().write_obj(60, texture2D);
            if (this.Click_Enter_Attention())
                edited = ind;
            texture2D.ClickHighlight();

            return false;
        }

        public string NeedAttention()
        {
            if (_numberOfTexture2DBackups > 50)
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
                SetAndApply(!dontRedoMipMaps);

            pixelsDirty = false;
            _repaintTimer = _repaintDelay;
        }
    }

}