using System;
using System.Collections;
using System.IO;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PlaytimePainter
{
    public enum TexTarget { Texture2D, RenderTexture }

    public class TextureMeta : PainterSystemKeepUnrecognizedCfg, IPEGI_ListInspect, IGotName, INeedAttention, ICanBeDefaultCfg
    {

        #region Values

        private static Texture2D _sampler;

        public TexTarget target;
        public RenderTexture renderTexture;
        public Texture2D texture2D;
        public Texture other;
        public int width = 128;
        public int height = 128;
        public bool useTexCoord2;
        private bool _useTexCoord2AutoAssigned;
        public bool lockEditing;
        public bool isATransparentLayer;

        public bool NeedsToBeSaved => QcUnity.SavedAsAsset(texture2D) || QcUnity.SavedAsAsset(renderTexture);

        public bool enableUndoRedo;
        public bool pixelsDirty;
        public bool preserveTransparency = true;
        private bool _alphaPreservePixelSet;
        public bool errorWhileReading;
        public bool dontRedoMipMaps;
        public bool disableContiniousLine;

        private float sdfMaxInside = 1f;
        private float sdfMaxOutside = 1f;
        private float sdfPostProcessDistance = 1f;

        public bool isAVolumeTexture;

        private float _repaintDelay = 0.016f;
        public bool updateTex2DafterStroke;
        private int _numberOfTexture2DBackups = 10;
        private int _numberOfRenderTextureBackups = 10;
        public bool backupManually;
        public Vector2 tiling = Vector2.one;
        public Vector2 offset = Vector2.zero;
        public string saveName = "No Name";
        public string url = "";
        private Color[] _pixels;
        public Color clearColor = Color.black;

        public Color[] Pixels
        {
            get { if (_pixels == null) PixelsFromTexture2D(texture2D); return _pixels; }
            set { _pixels = value; }
        }
        
        public void Rename(string newName)
        {
            saveName = newName;
            if (texture2D)
                texture2D.name = newName;
            if (renderTexture)
                renderTexture.name = newName;
        }


        #endregion

        #region Modules

        ImgMetaModules _modulesContainer;

        public ImgMetaModules Modules
        {
            get
            {
                if (_modulesContainer == null)
                    _modulesContainer = new ImgMetaModules(this);

                return _modulesContainer;
            }
        }

        public T GetModule<T>() where T : ImageMetaModuleBase => Modules.GetModule<T>();


        public class ImgMetaModules : TaggedModulesList<ImageMetaModuleBase>
        {

            public override void OnInitialize()
            {
                foreach (var p in modules)
                    p.parentMeta = meta;
            }

            public TextureMeta meta;

            public ImgMetaModules(TextureMeta meta)
            {
                this.meta = meta;
            }
        }


        #endregion

        #region SAVE IN PLAYER

        const string SavedImagesFolder = "Saved Images";

        public string SaveInPlayer()
        {
            if (texture2D == null) return "Save Failed";

            if (target == TexTarget.RenderTexture)
                RenderTexture_To_Texture2D();

            var png = texture2D.EncodeToPNG();

            var path = Path.Combine(Application.persistentDataPath, SavedImagesFolder);

            Directory.CreateDirectory(path);

            var fullPath = Path.Combine(path, "{0}.png".F(saveName));

            File.WriteAllBytes(fullPath, png);

            var msg = $"Saved {saveName} to {fullPath}";

            Cfg.playtimeSavedTextures.Add(fullPath);

            pegi.GameView.ShowNotification(msg);

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

                else pegi.GameView.ShowNotification("Couldn't Load Image ");

            }
        }

        #endregion

        #region Encoding

        public bool IsDefault => !NeedsToBeSaved;

        public override CfgEncoder Encode()
        {
            var cody = this.EncodeUnrecognized()
            .Add("mods", Modules)
            .Add_IfNotZero("dst", (int)target)
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
            .Add_IfTrue("trnsp", isATransparentLayer)
            .Add_IfTrue("vol", isAVolumeTexture)
            .Add_IfTrue("updT2D", updateTex2DafterStroke)
            .Add_IfTrue("bu", enableUndoRedo)
            .Add_IfTrue("tc2Auto", _useTexCoord2AutoAssigned)
            .Add_IfTrue("dumm", dontRedoMipMaps)
            .Add_IfTrue("dCnt", disableContiniousLine)

            .Add_IfNotBlack("clear", clearColor)
            .Add_IfNotEmpty("URL", url)
            .Add_IfNotNegative("is", inspectedItems)
            .Add_IfNotNegative("ip", _inspectedProcess)
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

            if (texture2D)
            {
                width = texture2D.width;
                height = texture2D.height;
            }

        }

        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "mods": Modules.Decode(data); break;
                case "dst": target = (TexTarget)data.ToInt(); break;
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
                case "vol": isAVolumeTexture = data.ToBool(); break;
                case "updT2D": updateTex2DafterStroke = data.ToBool(); break;

                case "bu": enableUndoRedo = data.ToBool(); break;
                case "dumm": dontRedoMipMaps = data.ToBool(); break;
                case "dCnt": disableContiniousLine = data.ToBool(); break;
                case "tc2Auto": _useTexCoord2AutoAssigned = data.ToBool(); break;
                case "clear": clearColor = data.ToColor(); break;
                case "URL": url = data; break;
                case "alpha": preserveTransparency = data.ToBool(); break;
                case "is": inspectedItems = data.ToInt(); break;
                case "ip": _inspectedProcess = data.ToInt(); break;
                case "sdfMI": sdfMaxInside = data.ToFloat(); break;
                case "sdfMO": sdfMaxOutside = data.ToFloat(); break;
                case "sdfMD": sdfPostProcessDistance = data.ToFloat(); break;
                default: return false;
            }
            return true;
        }

        #endregion

        #region Undo & Redo
        public PaintingUndoRedo.UndoCache cache = new PaintingUndoRedo.UndoCache();

        public void OnStrokeMouseDown_CheckBackup()
        {
            if (backupManually) return;

            if (enableUndoRedo)
            {
                if (target == TexTarget.RenderTexture)
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

                texture2D.CopyFrom(RenderTextureBuffersManager.GetDownscaledBigRt(width, height));

                PixelsFromTexture2D(texture2D);

                SetAndApply();

                renderTexture = tmp;
                
            }

        }

        public bool Contains(Texture tex)
        {
            return tex && ((texture2D && tex == texture2D) || (renderTexture && renderTexture == tex) || (other && tex == other));
        }

        public RenderTexture AddRenderTexture() => AddRenderTexture(width, height, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, FilterMode.Bilinear, null);

        public RenderTexture AddRenderTexture(int nwidth, int nheight, RenderTextureFormat format, RenderTextureReadWrite dataType, FilterMode filterMode, string global)
        {

            if (target == TexTarget.RenderTexture)
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

            if (target == TexTarget.RenderTexture)
                Texture2D_To_RenderTexture();

            return renderTexture;
        }

        public void Texture2D_To_RenderTexture() => TextureToRenderTexture(texture2D);

        public void TextureToRenderTexture(Texture2D tex) => PainterCamera.Inst.Render(tex, this.CurrentRenderTexture(), Cfg.pixPerfectCopy);

        public void RenderTexture_To_Texture2D() => RenderTexture_To_Texture2D(texture2D);

        public void RenderTexture_To_Texture2D(Texture2D tex)
        {
            if (!texture2D)
                return;

            var rt = renderTexture;

            TexMGMT.TryApplyBufferChangesTo(this);

            if (!rt && TexMGMT.imgMetaUsingRendTex == this)
                rt = RenderTextureBuffersManager.GetDownscaledBigRt(width, height);

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


            if (PainterCamera.Inst.IsLinearColorSpace)
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

            if (changeTo != target)
            {

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
                    {
                        PainterCamera.Inst.EmptyBufferTarget();
                        PainterCamera.Inst.DiscardAlphaBuffer();
                    }
                    else if (painter.initialized && !painter.isBeingDisabled)
                    {
                        // To avoid Clear to black when exiting playmode
                        RenderTexture_To_Texture2D();
                        Debug.Log("Is being switched to Tex2D");
                    }

                }
                target = changeTo;
                painter.SetTextureOnMaterial(this);

            }
            else Debug.Log("Destination already Set");

        }

        public void SetPixelsInRam()
        {
            try
            {
                texture2D.SetPixels(_pixels);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);

                _pixels = null;
                errorWhileReading = true;
            }
        }

        public void ApplyToTexture2D(bool mipMaps = true) => texture2D.Apply(mipMaps, false);

        public void SetAndApply(bool mipMaps = true)
        {
            if (_pixels == null) return;
            SetPixelsInRam();
            ApplyToTexture2D(mipMaps);
        }

        public void SetApplyUpdateRenderTexture(bool mipMaps = true)
        {
            SetAndApply(mipMaps);
            if (target == TexTarget.RenderTexture)
                Texture2D_To_RenderTexture();
        }

        public void AfterStroke(Stroke st)
        {
            if (this.TargetIsTexture2D())
                pixelsDirty = true;
            else if (updateTex2DafterStroke && st.MouseUpEvent)
            {
                RenderTexture_To_Texture2D();
            }

            st.SetPreviousValues();
            st.firstStroke = false;
            st.MouseDownEvent = false;
        }

        #endregion

        #region Pixels MGMT

        public void UnsetAlphaSavePixel()
        {
            if (_alphaPreservePixelSet)
            {
                _pixels[0].a = 1;
                SetAndApply();
            }
        }

        public void SetAlphaSavePixel()
        {
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

        void PixelsToLinear()
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

        public void SetPixels(Color col)
        {
            var p = Pixels;
            for (var i = 0; i < p.Length; i++)
                p[i] = col;
        }

        public TextureMeta SetPixels(Color col, ColorMask mask)
        {
            var p = Pixels;

            bool r = mask.HasFlag(ColorMask.R);
            bool g = mask.HasFlag(ColorMask.G);
            bool b = mask.HasFlag(ColorMask.B);
            bool a = mask.HasFlag(ColorMask.A);

            for (var i = 0; i < p.Length; i++)
            {
                var pix = p[i];
                pix.r = r ? col.a : pix.r;
                pix.g = g ? col.g : pix.g;
                pix.b = b ? col.b : pix.b;
                pix.a = a ? col.a : pix.a;
                p[i] = pix;
            }

            return this;
        }

        public void FillWithColor(Color color)
        {
            if (target == TexTarget.Texture2D)
            {
                SetPixels(color);
                SetAndApply(true);
            }
            else
            {
                PainterCamera.Inst.Render(color, this.CurrentRenderTexture());
            }


        }

        public Color SampleAt(Vector2 uv) => (target == TexTarget.Texture2D) ? PixelSafe_Slow(UvToPixelNumber(uv)) : SampleRenderTexture(uv);

        private Color SampleRenderTexture(Vector2 uv)
        {

            var curRt = RenderTexture.active;

            var rtp = PainterCamera.Inst;
            int size = RenderTextureBuffersManager.renderBuffersSize / 4;
            RenderTexture.active = renderTexture ? renderTexture : RenderTextureBuffersManager.GetDownscaledBigRt(size, size);

            if (!_sampler) _sampler = new Texture2D(8, 8);

            UVto01(ref uv);

            if (!renderTexture)
                uv.y = 1 - uv.y; // For some reason sampling is mirrored around Y axiz for BigRenderTexture (?)

            uv *= RenderTexture.active.width;

            _sampler.ReadPixels(new Rect(uv.x, uv.y, 1, 1), 0, 0);

            RenderTexture.active = curRt;

            var pix = _sampler.GetPixel(0, 0);

            if (PainterCamera.Inst.IsLinearColorSpace)
                pix = pix.linear;

            return pix;
        }

        public void PixelsFromTexture2D(Texture2D tex, bool userClickedRetry = false)
        {

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

        private Color PixelSafe_Slow(MyIntVec2 v) => Pixels[PixelNo(v.x, v.y)];

        public Color PixelUnSafe(int x, int y) => _pixels[y * width + x];

        public Color SetPixelUnSafe(int x, int y, Color col) => _pixels[y * width + x] = col;

        public int PixelNo(MyIntVec2 v) => PixelNo(v.x, v.y);

        public int PixelNo(int x, int y)
        {

            x = ((x % width) + width) % width;

            y = ((y % height) + height) % height;

            return y * width + x;
        }

        public MyIntVec2 UvToPixelNumber(Vector2 uv) => new MyIntVec2(Mathf.FloorToInt(uv.x * width), Mathf.FloorToInt(uv.y * height));

        public MyIntVec2 UvToPixelNumber(Vector2 uv, out Vector2 pixelOffset)
        {
            uv *= new Vector2(width, height);
            var result = new MyIntVec2(Mathf.Round(uv.x), Mathf.Round(uv.y));

            pixelOffset = new Vector2(uv.x - result.x - 0.5f, uv.y - result.y - 0.5f);

            return result;
        }

        public void SetEdges(Color col) => SetEdges(col, ColorMask.All);

        public void SetEdges(Color col, ColorMask mask)
        {

            if (!Pixels.IsNullOrEmpty())
            {

                for (int XSection = 0; XSection < 2; XSection++)
                {
                    int x = XSection * (width - 1);
                    for (int y = 0; y < height; y++)
                    {
                        var pix = PixelUnSafe(x, y);
                        mask.SetValuesOn(ref pix, col);
                        SetPixelUnSafe(x, y, pix);
                    }
                }

                for (int YSection = 0; YSection <= 1; YSection++)
                {
                    int y = YSection * (height - 1);
                    for (int x = 0; x < height; x++)
                    {
                        var pix = PixelUnSafe(x, y);
                        mask.SetValuesOn(ref pix, col);
                        SetPixelUnSafe(x, y, pix);
                    }
                }

            }


        }

        private int _offsetByX;
        private int _offsetByY;

        private void OffsetPixels() => OffsetPixels(_offsetByX, _offsetByY);

        public void OffsetPixels(int dx, int dy)
        {

            if (Pixels == null)
            {
                Debug.LogError("Pixels are null");
                return;
            }

            var pixelsCopy = _pixels.GetCopy();

            dx = ((dx % width) + width) % width;
            dy = ((dy % height) + height) % height;

            for (int y = 0; y < height; y++)
            {
                var srcOff = y * width;
                var dstOff = ((y + dy) % height) * width;

                for (int x = 0; x < width; x++)
                {

                    var srcInd = srcOff + x;

                    var dstInd = dstOff + ((x + dx) % width);

                    _pixels[srcInd] = pixelsCopy[dstInd];


                }
            }
        }

        #endregion

        #region Init

        public TextureMeta Init(int renderTextureSize)
        {
            width = renderTextureSize;
            height = renderTextureSize;
            AddRenderTexture();
            Cfg.imgMetas.Insert(0, this);
            target = TexTarget.RenderTexture;
            return this;
        }

        public TextureMeta Init(Texture tex)
        {

            var t2D = tex as Texture2D;

            if (t2D)
                UseTex2D(t2D);
            else
                 if (tex.GetType() == typeof(RenderTexture))
                UseRenderTexture((RenderTexture)tex);
            else
                other = tex;

            if (Cfg == null)
                return this;

            if (!Cfg.imgMetas.Contains(this))
                Cfg.imgMetas.Insert(0, this);
            return this;
        }

        public void FromRenderTextureToNewTexture2D()
        {
            texture2D = new Texture2D(width, height);
            RenderTexture_To_Texture2D();
        }

        public void From(Texture2D texture, bool userClickedRetry = false)
        {

            texture2D = texture;
            saveName = texture.name;

            if (userClickedRetry || !errorWhileReading)
            {

#if UNITY_EDITOR
                if (texture)
                {

                    var imp = texture.GetTextureImporter();
                    if (imp != null)
                    {

                        isATransparentLayer = imp.alphaIsTransparency;

                        texture.Reimport_IfNotReadale();
                    }
                }
#endif

                PixelsFromTexture2D(texture2D, userClickedRetry);
            }
        }

        private void UseRenderTexture(RenderTexture rt)
        {
            renderTexture = rt;
            width = rt.width;
            height = rt.height;
            target = TexTarget.RenderTexture;

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

        private void UseTex2D(Texture2D tex)
        {

            From(tex);
            target = TexTarget.Texture2D;
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

        void ReturnToRenderTexture()
        {
            var p = PlaytimePainter.inspected;
            p.UpdateOrSetTexTarget(TexTarget.RenderTexture);
        }

        bool WasRenderTexture()
        {
            if (target == TexTarget.RenderTexture)
            {
                var p = PlaytimePainter.inspected;
                if (p)
                {
                    p.UpdateOrSetTexTarget(TexTarget.Texture2D);
                    return true;
                }
            }

            return false;
        }

        private bool LoadTexturePegi(string path)
        {
            const bool changed = false;

            if ("Load {0}".F(path.Substring(path.LastIndexOf("/", StringComparison.Ordinal))).Click())
                LoadInPlayer(path);

            return changed;
        }

        private QcAsync.TimedEnumeration _processEnumerator;

        public QcAsync.TimedEnumeration ProcessEnumerator
        {
            get
            {
                if (_processEnumerator != null && _processEnumerator.Exited)
                    _processEnumerator = null;

                return _processEnumerator;
            }
        }

        public override bool Inspect() {

            if (ProcessEnumerator != null) {
                
                "Running Coroutine".nl();
                _processEnumerator.Inspect_AsInList();
                return false;
                
            }

            var changed = false;

            if ("CPU blit options".conditional_enter(this.TargetIsTexture2D(), ref inspectedItems, 0).nl())
            {
                "Disable Continious Lines".toggleIcon("If you see unwanted lines appearing on the texture as you paint, enable this.", ref disableContiniousLine).nl(ref changed);

                "CPU blit repaint delay".edit("Delay for video memory update when painting to Texture2D", 140, ref _repaintDelay, 0.01f, 0.5f).nl(ref changed);

                "Don't update mipMaps".toggleIcon("May increase performance, but your changes may not disaplay if you are far from texture.",
                    ref dontRedoMipMaps).changes(ref changed);

          

            }

            if ("GPU blit options".enter(ref inspectedItems, 1).nl())
            {
                "Update Texture2D after every stroke".toggleIcon(ref updateTex2DafterStroke).nl();
            }

            #region Processors

            var newWidth = Cfg.SelectedWidthForNewTexture(); //PainterDataAndConfig.SizeIndexToSize(PainterCamera.Data.selectedWidthIndex);
            var newHeight = Cfg.SelectedHeightForNewTexture();

            if ("Texture Processors".enter(ref inspectedItems, 6).nl())
            {

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

                            if (rescale.changes(ref changed))
                            {
                                Resize(newWidth, newHeight);
                                var pp = PlaytimePainter.inspected;
                                if (pp)
                                {
                                    var preview = !pp.NotUsingPreview;
                                    if (preview)
                                    {
                                        // Doing this to fix bug with _TexelSize not updating when changing texture size.
                                        pp.SetOriginalShaderOnThis();
                                        pp.SetPreviewShader(); 
                                    }
                                }
                            }

                        }
                        pegi.nl();
                    }

                    if (_inspectedProcess == -1)
                    {

                        if ((newWidth != width || newHeight != height) && icon.Size.Click("Resize").nl(ref changed))
                            Resize(newWidth, newHeight);

                        pegi.nl();
                    }

                    if ("Clear ".enter(ref _inspectedProcess, 1, false))
                    {

                        "Clear Color".edit(80, ref clearColor).nl();

                        if ("Clear Texture".Click().nl(ref changed))
                        {
                            FillWithColor(clearColor);
                            //SetPixels(clearColor);
                            //SetApplyUpdateRenderTexture();
                        }
                    }

                    if (_inspectedProcess == -1 && icon.Refresh.Click("Apply color {0}".F(clearColor)).nl())
                    {
                        FillWithColor(clearColor);
                        //SetPixels(clearColor);
                        //SetApplyUpdateRenderTexture();
                    }

                    if ("Color to Alpha".enter(ref _inspectedProcess, 2).nl())
                    {

                        "Background Color".edit(80, ref clearColor).nl();
                        if (Pixels != null)
                        {

                            if ("Color to Alpha".Click("Will Convert Background Color with transparency").nl())
                            {
                                bool wasRt = WasRenderTexture();

                                for (int i = 0; i < _pixels.Length; i++)
                                    _pixels[i] = BlitFunctions.ColorToAlpha(_pixels[i], clearColor);

                                SetAndApply();

                                if (wasRt)
                                    ReturnToRenderTexture();
                            }

                            if ("Color from Alpha".Click("Will subtract background color from transparency").nl())
                            {

                                bool wasRt = WasRenderTexture();

                                for (int i = 0; i < _pixels.Length; i++)
                                {
                                    var col = _pixels[i];

                                    col.a = BlitFunctions.ColorToAlpha(_pixels[i], clearColor).a;

                                    _pixels[i] = col;
                                }

                                SetAndApply();

                                if (wasRt)
                                    ReturnToRenderTexture();
                            }

                        }
                    }

                    if ("Signed Distance Filelds generator".enter(ref _inspectedProcess, 4).nl())
                    {

                        if (texture2D.IsColorTexture())
                        {
                            "Texture is a color texture, best to switch to non-color for SDF. Save any changes first, as the texture will reimport.".writeWarning();

#if UNITY_EDITOR
                            var ai = texture2D.GetTextureImporter();

                            if (ai != null && "Convert to non-Color".ClickConfirm("SDFnc", "This will undo any unsaved changes. Proceed?") && ai.WasWrongIsColor(false))
                                ai.SaveAndReimport();

#endif
                        }

                        "Will convert black and white color to black and white signed field".nl();

                        "SDF Max Inside".edit(ref sdfMaxInside).nl();
                        "SDF Max Outside".edit(ref sdfMaxOutside).nl();
                        "SDF Post Process".edit(ref sdfPostProcessDistance).nl();

                        if ("Generate Assync".Click("Will take a bit longer but you'll be able to use Unity")) {

                            bool wasRt = WasRenderTexture();

                            var p = PlaytimePainter.inspected;
                            if (p)
                                p.UpdateOrSetTexTarget(TexTarget.Texture2D);

                            _processEnumerator = QcAsync.StartManagedCoroutine(
                                DistanceFieldProcessor.Generate(this, sdfMaxInside, sdfMaxOutside,
                                    sdfPostProcessDistance), () => {

                                    SetAndApply();
                                    if (wasRt)
                                        ReturnToRenderTexture();
                                });
                        }

                        pegi.nl();

                    }

                    if ("Curves".enter(ref _inspectedProcess, 5).nl())
                    {
                        var crv = TexMGMT.InspectAnimationCurve("Channel");

                        if (Pixels != null)
                        {

                            if ("Remap Alpha".Click())
                            {
                                for (int i = 0; i < _pixels.Length; i++)
                                {
                                    var col = _pixels[i];
                                    col.a = crv.Evaluate(col.a);
                                    _pixels[i] = col;
                                }
                                SetApplyUpdateRenderTexture();
                            }

                            if ("Remap Color".Click())
                            {
                                for (int i = 0; i < _pixels.Length; i++)
                                {
                                    var col = _pixels[i];
                                    col.r = crv.Evaluate(col.r);
                                    col.g = crv.Evaluate(col.g);
                                    col.b = crv.Evaluate(col.b);
                                    _pixels[i] = col;
                                }
                                SetApplyUpdateRenderTexture();
                            }

                        }
                    }

                    if ("Save Textures In Game ".enter(ref _inspectedProcess, 7).nl())
                    {

                        "This is intended to test playtime saving. The functions to do so are quite simple. You can find them inside ImageData.cs class."
                            .writeHint();

                        pegi.nl();

                        "Save Name".edit(70, ref saveName);

                        if (icon.Folder.Click("Open Folder with textures").nl())
                            QcFile.Explorer.OpenPersistentFolder(SavedImagesFolder);

                        if ("Save Playtime".Click("Will save to {0}/{1}".F(Application.persistentDataPath, saveName)).nl())
                            SaveInPlayer();

                        if (Cfg && Cfg.playtimeSavedTextures.Count > 0)
                            "Playtime Saved Textures".write_List(Cfg.playtimeSavedTextures, LoadTexturePegi);
                    }

                    if ("Fade edges".enter(ref _inspectedProcess, 8).nl())
                    {

                        ("This will cahange pixels on the edges of the texture. Useful when wrap mode " +
                         "is set to clamp.").writeHint();

                        if (texture2D)
                        {

#if UNITY_EDITOR
                            var ti = texture2D.GetTextureImporter();
                            if (ti)
                            {
                                if (ti.wrapMode != TextureWrapMode.Clamp && "Change wrap mode from {0} to Clamp"
                                        .F(ti.wrapMode).Click().nl(ref changed))
                                {
                                    ti.wrapMode = TextureWrapMode.Clamp;
                                    ti.SaveAndReimport();
                                }
                            }
#endif

                            if ("Set edges to transparent".Click().nl(ref changed))
                            {
                                SetEdges(Color.clear, ColorMask.A);
                                SetAndApply();
                            }

                            if ("Set edges to Clear Black".Click().nl(ref changed))
                            {
                                SetEdges(Color.clear);
                                SetAndApply();
                            }

                        }
                    }

                    if ("Add Background".enter(ref _inspectedProcess, 9).nl())
                    {

                        "Background Color".edit(80, ref clearColor).nl();

                        if ("Add Background".Click("Will Add Beckground color and make everything non transparent").nl())
                        {

                            bool wasRt = WasRenderTexture();

                            for (int i = 0; i < _pixels.Length; i++)
                                _pixels[i] = BlitFunctions.AddBackground(_pixels[i], clearColor);

                            SetAndApply();

                            if (wasRt)
                                ReturnToRenderTexture();
                        }

                    }

                    if ("Offset".enter(ref _inspectedProcess, 10).nl())
                    {

                        "X:".edit(ref _offsetByX);

                        if ((_offsetByX != width / 2) && "{0}/{1}".F(width / 2, width).Click())
                            _offsetByX = width / 2;

                        pegi.nl();

                        "Y:".edit(ref _offsetByY);

                        if ((_offsetByY != height / 2) && "{0}/{1}".F(height / 2, height).Click())
                            _offsetByY = height / 2;

                        pegi.nl();

                        if (((_offsetByX % width != 0) || (_offsetByY % height != 0)) && "Apply Offset".Click())
                        {
                            OffsetPixels();
                            SetAndApply();
                        }

                    }

                }
            }

            #endregion

            if ("Enable Undo for '{0}'".F(NameForPEGI).toggle_enter(ref enableUndoRedo, ref inspectedItems, 2, ref changed).nl())
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

            if (inspectedItems == -1)
            {
                if (isAVolumeTexture)
                    "Is A volume texture".toggleIcon(ref isAVolumeTexture).nl(ref changed);
            }

            return changed;
        }

        public bool ComponentDependent_PEGI(bool showToggles, PlaytimePainter painter)
        {
            var changed = false;

            var property = painter.GetMaterialTextureProperty;

            var forceOpenUTransparentLayer = false;

            var material = painter.Material;
            
            var hasAlphaLayerTag = material && (property!=null) &&
                material.Has(property,
                    ShaderTags.LayerTypes
                        .Transparent); //GetTag(PainterDataAndConfig.ShaderTagLayerType + property, false).Equals("Transparent");

            if (!isATransparentLayer && hasAlphaLayerTag)
            {
                "Material Field {0} is a Transparent Layer ".F(property).writeHint();
                forceOpenUTransparentLayer = true;
            }

            if (showToggles || (isATransparentLayer && !hasAlphaLayerTag) || forceOpenUTransparentLayer)
            {
                MsgPainter.TransparentLayer.GetText().toggleIcon(ref isATransparentLayer).changes(ref changed);

                pegi.FullWindowService.fullWindowDocumentationWithLinkClickOpen(
                MsgPainter.TransparentLayer.GetDescription(),
                        "https://www.quizcanners.com/single-post/2018/09/30/Why-do-I-get-black-outline-around-the-stroke",
                        "More About it");

                if (isATransparentLayer)
                    preserveTransparency = true;


                pegi.nl();
            }
            

            if (showToggles)
            {

                if (isATransparentLayer)
                    preserveTransparency = true;
                else
                {

                    MsgPainter.PreserveTransparency.GetText().toggleIcon(ref preserveTransparency).changes(ref changed);

                    MsgPainter.PreserveTransparency.DocumentationClick();

                    pegi.nl();
                }
            }

            var forceOpenUv2 = false;
            var hasUv2Tag = painter.Material.Has(property, ShaderTags.SamplingModes.Uv2);

            if (!useTexCoord2 && hasUv2Tag)
            {

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
                "Use UV2".toggleIcon(ref useTexCoord2).nl(ref changed);

            return changed;
        }

        public bool Undo_redo_PEGI()
        {
            var changed = false;

            if (cache == null) cache = new PaintingUndoRedo.UndoCache();

            if (cache.undo.GotData)
            {
                if (icon.Undo.Click("Press Z to undo (Scene View)", ref changed))
                    cache.undo.ApplyTo(this);
            }
            else
                icon.UndoDisabled.write("Nothing to Undo (set number of undo frames in config)");

            if (cache.redo.GotData)
            {
                if (icon.Redo.Click("X to Redo", ref changed))
                    cache.redo.ApplyTo(this);
            }
            else
                icon.RedoDisabled.write("Nothing to Redo");

            pegi.nl();

            return changed;
        }

        public bool InspectInList(IList list, int ind, ref int edited)
        {
            this.GetNameForInspector().write(150, texture2D);
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

        #endregion

        private float _repaintTime;

        public void ManagedUpdate(PlaytimePainter painter)
        {
            if (pixelsDirty)
            {

                var noTimeYet = (QcUnity.TimeSinceStartup() - _repaintTime < _repaintDelay);
                if (noTimeYet && !painter.stroke.MouseUpEvent)
                    return;
                
                if (texture2D)
                    SetAndApply(!dontRedoMipMaps);

                pixelsDirty = false;
                _repaintTime = (float)QcUnity.TimeSinceStartup();
            }

            foreach (var m in Modules)
                m.ManagedUpdate();
        }

    }

}