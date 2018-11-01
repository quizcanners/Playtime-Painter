using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using PlayerAndEditorGUI;
using SharedTools_Stuff;
using System.IO;
#if UNITY_2018_1_OR_NEWER
using Unity.Collections;
using Unity.Jobs;
#endif

namespace Playtime_Painter
{

    public enum TexTarget { Texture2D, RenderTexture }

    public class ImageData : PainterStuffKeepUnrecognized_STD, IPEGI, IPEGI_ListInspect, IGotName, INeedAttention
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
        public bool NeedsToBeSaved { get { return ((texture2D != null && texture2D.SavedAsAsset()) || (renderTexture != null && renderTexture.SavedAsAsset())); } }
        public bool showRecording = false;
        public bool enableUndoRedo;

        public int numberOfTexture2Dbackups = 10;
        public int numberOfRenderTextureBackups = 10;
        public bool backupManually;
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

        List<string> playtimeSavedTextures = new List<string>();

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

                playtimeSavedTextures.Add(fullPath);
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

        public override StdEncoder Encode()
        {
            var cody = this.EncodeUnrecognized()
            .Add("dst", (int)destination)
.Add_Reference("tex2D", texture2D)
.Add_Reference("other", other)
.Add("w", width)
.Add("h", height)
.Add_Bool("useUV2", useTexcoord2)
.Add_Bool("Lock", lockEditing)
.Add_Bool("b", backupManually)
.Add("tl", tiling)
.Add("off", offset)
.Add_String("sn", SaveName)
.Add("svs", playtimeSavedTextures)
.Add_Bool("rec", showRecording)
.Add_IfTrue("trnsp", isATransparentLayer)
.Add_Bool("bu", enableUndoRedo)
.Add_Bool("tc2Auto", useTexcoord2_AutoAssigned)
.Add("clear", clearColor)
.Add_IfNotEmpty("URL", URL);

            if (enableUndoRedo)
                cody.Add("2dUndo", numberOfTexture2Dbackups)
.Add("rtBackups", numberOfRenderTextureBackups);

            return cody;
        }

        public override bool Decode(string tag, string data)
        {
            switch (tag)
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
                case "svs": data.DecodeInto(out playtimeSavedTextures); break;
                case "trnsp": isATransparentLayer = data.ToBool(); break;
                case "rec": showRecording = data.ToBool(); break;
                case "bu": enableUndoRedo = data.ToBool(); break;
                case "tc2Auto": useTexcoord2_AutoAssigned = data.ToBool(); break;
                case "clear": clearColor = data.ToColor(); break;
                case "URL": URL = data; break;
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

            StuffSaver.SaveToPersistantPath(Cfg.vectorsFolderName, SaveName, allStrokes);

            Cfg.recordingNames.Add(SaveName);

            recording = false;

        }
        #endregion

        #region Texture MGMT
        public void Resize(int size)
        {

            if (size >= 8 && size <= 4096 && (size != width || size != height) && texture2D)
            {

                var tmp = renderTexture;
                renderTexture = null;

                Texture2D_To_RenderTexture();

                texture2D.Resize(size, size);

                width = height = size;

                texture2D.CopyFrom(PainterCamera.Inst.GetDownscaledBigRT(width, height));

                PixelsFromTexture2D(texture2D);

                SetAndApply(true);


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

            if ((global != null) && (global.Length > 0))
                Shader.SetGlobalTexture(global, renderTexture);

            if (destination == TexTarget.RenderTexture)
                Texture2D_To_RenderTexture();

            return renderTexture;
        }

        public void Texture2D_To_RenderTexture() => TextureToRenderTexture(texture2D);

        public void TextureToRenderTexture(Texture2D tex) => PainterCamera.Inst.Render(tex, this.CurrentRenderTexture(), TexMGMTdata.pixPerfectCopy);

        public void RenderTexture_To_Texture2D() => RenderTexture_To_Texture2D(texture2D);

        public void RenderTexture_To_Texture2D(Texture2D tex)
        {
            if (texture2D == null)
                return;

            RenderTexture rt = renderTexture;

            if (!rt && TexMGMT.imgDataUsingRendTex == this)
                rt = PainterCamera.Inst.GetDownscaledBigRT(width, height);

            if (rt == null)
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
                SetAndApply(true);
            else
                texture2D.Apply(true);
            // 

        }

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

        public void Colorize(Color col)
        {
            for (int i = 0; i < Pixels.Length; i++)
                _pixels[i] = col;

        }

        public Color SampleAT(Vector2 uv)
        {
            return (destination == TexTarget.Texture2D) ?
            Pixel(UvToPixelNumber(uv)) :
                SampleRenderTexture(uv);
        }

        public Color SampleRenderTexture(Vector2 uv)
        {

            RenderTexture curRT = RenderTexture.active;

            // Debug.Log("Sampling Render Texture");

            PainterCamera rtp = PainterCamera.Inst;
            int size = PainterCamera.renderTextureSize / 4;
            RenderTexture.active = renderTexture ?? rtp.GetDownscaledBigRT(size, size);

            if (sampler == null) sampler = new Texture2D(8, 8);

            UVto01(ref uv);

            if (renderTexture == null)
                uv.y = 1 - uv.y; // For some reason sampling is mirrored around Y axiz for BigRenderTexture (?)

            uv *= RenderTexture.active.width;

            sampler.ReadPixels(new Rect(uv.x, uv.y, 1, 1), 0, 0);

            RenderTexture.active = curRT;

            var pix = sampler.GetPixel(0, 0);

            if (PainterCamera.Inst.isLinearColorSpace)
                pix = pix.linear;

            return pix;
        }

        public void PixelsFromTexture2D(Texture2D tex)
        {
            if (tex == null)
                return;

            Pixels = tex.GetPixels();
            width = tex.width;
            height = tex.height;
        }

        public void ChangeDestination(TexTarget changeTo, MaterialData mat, string parameter, PlaytimePainter painter)
        {

            if (changeTo != destination)
            {
                //   Debug.Log("Changing destination");

                if (changeTo == TexTarget.RenderTexture)
                {
                    if (renderTexture == null)
                        PainterCamera.Inst.ChangeBufferTarget(this, mat, parameter, painter);
                    TextureToRenderTexture(texture2D);
                }
                else
                {
                    if (texture2D == null)
                        return;

                    if (renderTexture == null)
                        PainterCamera.Inst.EmptyBufferTarget();
                    else
                        if (painter.inited) // To avoid Clear to black when exiting playmode
                        RenderTexture_To_Texture2D();

                }
                destination = changeTo;
                painter.SetTextureOnMaterial(this);

            }
            else Debug.Log("Destination already Set");



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

        public MyIntVec2 UvToPixelNumber(Vector2 uv)
        {
            return new MyIntVec2(uv.x * width, uv.y * height);
        }

        public void SetAndApply(bool mipmaps)
        {
            if (_pixels == null)
                return;
            texture2D.SetPixels(_pixels);
            texture2D.Apply(mipmaps);
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
                SetAndApply(true);
            }
        }
#endif
        #endregion

        #region Init

        public ImageData Init(int renderTextureSize)
        {
            width = renderTextureSize;
            height = renderTextureSize;
            AddRenderTexture();
            TexMGMTdata.imgDatas.Insert(0, this);
            destination = TexTarget.RenderTexture;
            return this;
        }

        public ImageData Init(Texture tex)
        {

            if (tex.GetType() == typeof(Texture2D))
                UseTex2D((Texture2D)tex);
            else
                 if (tex.GetType() == typeof(RenderTexture))
                UseRenderTexture((RenderTexture)tex);
            else
                other = tex;

            if (!TexMGMTdata.imgDatas.Contains(this))
                TexMGMTdata.imgDatas.Insert(0, this);
            return this;
        }
        
        public void FromRenderTextureToNewTexture2D()
        {
            texture2D = new Texture2D(width, height);
            RenderTexture_To_Texture2D();
        }

        public void From(Texture2D texture)
        {

            texture2D = texture;
            SaveName = texture.name;

#if UNITY_EDITOR
            if (texture != null)
            {
                var imp = texture.GetTextureImporter();
                if (imp != null)
                {

                    isATransparentLayer = imp.alphaIsTransparency;

                    /*  var name =  AssetDatabase.GetAssetPath(texture);
                      var extension = name.Substring(name.LastIndexOf(".") + 1);

                      if (extension != "png") {
                          ("Converting " + name + " to .png").showNotificationIn3D_Views();
                          texture = texture.CreatePngSameDirectory(texture.name);
                      }*/

                    texture.Reimport_IfNotReadale();
                }
            }
#endif

            PixelsFromTexture2D(texture2D);
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
               if (SaveName == null)
                SaveName = "New img";
        }

        void UseTex2D(Texture2D tex)
        {

            From(tex);
            destination = TexTarget.Texture2D;
#if UNITY_EDITOR
            string path = AssetDatabase.GetAssetPath(tex);
            if (!string.IsNullOrEmpty(path))
            {
                SaveName = tex.name;
                // saved = true;
            }
            else
#endif
                if (SaveName == null)
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
               // changed |= "Use Unity Jobs".toggle("Unity 2018 or newer", ref Cfg.useJobsForCPUpainting).nl();

                changed |= "CPU blit repaint delay".edit("Delay for video memory update when painting to Texture2D", 140, ref GlobalBrush.repaintDelay, 0.01f, 0.5f).nl();

                changed |= "Don't update mipmaps:".toggleIcon("May increase performance, but your changes may not disaplay if you are far from texture.",
                    ref GlobalBrush.DontRedoMipmaps, true).nl();
            }

            if ("Save Textures In Game".enter(ref inspectedStuff, 1).nl())
            {

                "Save Name".edit(70, ref SaveName);
                
                if (icon.Folder.Click("Open Folder with textures").nl())
                    StuffExplorer.OpenPersistantFolder(savedImagesFolder);

                if ("Save Playtime".Click(string.Format("Will save to {0}/{1}", Application.persistentDataPath, SaveName)).nl())
                    SaveInPlayer();

                if (playtimeSavedTextures.Count > 0)
                    "Playtime Saved Textures".write_List(playtimeSavedTextures, LoadTexturePEGI);
            }

            #region Processors
            if ("Texture Processors".enter(ref inspectedStuff, 6).nl()) {

                if ("Resize Texture ({0} * {1})".F(width, height).enter(ref inspectedProcess, 0).nl_ifFalse())  {
                    "New size ".select(60, ref PainterCamera.Data.selectedSize, PainterDataAndConfig.NewTextureSizeOptions);
                    int size = PainterDataAndConfig.SelectedSizeForNewTexture(PainterCamera.Data.selectedSize);

                    if (size != width || size != height)
                    {
                        bool rescale = false;

                        if (size <= width && size <= height)
                            rescale = "Downscale".Click();
                        else if (size >= width && size >= height)
                            rescale = "Upscale".Click();
                        else
                            rescale = "Rescale".Click();

                        if (rescale)
                            Resize(size);
                    }

                    pegi.nl();
                 
                }

                if ("Colorize ".enter(ref inspectedProcess, 1).nl_ifFalse()) {

                    "Clear Color".edit(80, ref clearColor).nl();
                    if ("Clear Texture".Click().nl())
                    {
                        Colorize(clearColor);
                        SetAndApply(true);
                    }
                }

                    if ("Render Buffer Debug".enter(ref inspectedProcess, 3).nl())
                {
                    pegi.write(TexMGMT.BigRT_pair[0], 200);
                    pegi.nl();
                    pegi.write(TexMGMT.BigRT_pair[1], 200);

                    pegi.nl();
                }
                }

            #endregion

            if ("Undo Redo".toggle_enter(ref enableUndoRedo, ref inspectedStuff, 2, ref changed).nl())
            {

                changed |=
              "UNDOs: Tex2D".edit(80, ref numberOfTexture2Dbackups) ||
              "RendTex".edit(60, ref numberOfRenderTextureBackups).nl();

                "Backup manually".toggleIcon(ref backupManually, true).nl();

                if (numberOfTexture2Dbackups > 50 || numberOfRenderTextureBackups > 50)
                    "Too big of a number will eat up lot of memory".writeWarning();

                pegi.writeOneTimeHint("Creating more backups will eat more memory", "backupIsMem");
                pegi.writeOneTimeHint("This are not connected to Unity's " +
                "Undo/Redo because when you run out of backups you will by accident start undoing other stuff.", "noNativeUndo");
                pegi.writeOneTimeHint("Use Z/X to undo/redo", "ZXundoRedo");

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
            bool changed = false;

            var property = painter.GetMaterialTexturePropertyName;

            bool forceOpenUTransparentLayer = false;

            bool hasTPtag = painter.Material.HasTag(PainterDataAndConfig.TransparentLayerExpected + property);

            if (!isATransparentLayer && hasTPtag)  {
                "Material Field {0} is a Transparent Layer ".F(property).writeHint();
                forceOpenUTransparentLayer = true;
            }

            if (showToggles || (isATransparentLayer && !hasTPtag) || forceOpenUTransparentLayer)
                changed |= "Transparent Layer".toggleIcon(ref isATransparentLayer, true).nl();

            bool forceOpenUV2 = false;
            bool hasUV2tag = painter.Material.HasTag(PainterDataAndConfig.TextureSampledWithUV2 + property);

            if (!useTexcoord2 && hasUV2tag)
            {
                if (!useTexcoord2_AutoAssigned)
                {
                    useTexcoord2 = true;
                    useTexcoord2_AutoAssigned = true;
                }
                else
                    "Material Field {0} is Sampled using Texture Coordinates 2 ".F(property).writeHint();
                forceOpenUV2 = true;
            }

            if (showToggles || (useTexcoord2 && !hasUV2tag) || forceOpenUV2)
                changed |= "Use Texcoord 2".toggleIcon(ref useTexcoord2, true).nl();

            if (showToggles || (!painter.IsOriginalShader && Cfg.previewAlphaChanel))
                changed |= "Preview Shows Only Enabled Chanels".toggleIcon(ref Cfg.previewAlphaChanel, true).nl();

           
            return changed;
        }

        public bool Undo_redo_PEGI()
        {
            bool changed = false;

            if (cache == null) cache = new UndoCache();
            if (recordedStrokes == null) recordedStrokes = new List<string>();
            if (recordedStrokes_forUndoRedo == null) recordedStrokes_forUndoRedo = new List<string>(); // to sync strokes recording with Undo Redo

            if (cache.undo.gotData())
            {
                if (icon.Undo.Click("Press Z to undo (Scene View)", 25))
                {
                    cache.undo.ApplyTo(this);
                    changed = true;
                }
            }
            else
                icon.UndoDisabled.write("Nothing to Undo (set number of undo frames in config)", 25);

            if (cache.redo.gotData())
            {
                if (icon.Redo.Click("X to Redo", 25))
                {
                    changed = true;
                    cache.redo.ApplyTo(this);
                }
            }
            else
                icon.RedoDisabled.write("Nothing to Redo", 25);


            pegi.newLine();

#if UNITY_EDITOR
            if (recording)
            {

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
            if (icon.Enter.Click())
                edited = ind;
            texture2D.clickHighlight();

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
    }

}