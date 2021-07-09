using System;
using System.IO;
using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;
using QuizCanners.Migration;

namespace PlaytimePainter
{
    #pragma warning disable UNT0017 // SetPixels is Needed for floating point calculation

    public enum TexTarget { Texture2D, RenderTexture }

    [Serializable]
    public class TextureMeta : PainterClass, IPEGI, IPEGI_ListInspect, IGotName, INeedAttention //, ICanBeDefaultCfg
    {

        #region Values

        private static Texture2D _sampler;

        [SerializeField] public TexTarget Target;
        [SerializeField] public RenderTexture RenderTexture;
        [SerializeField] public Texture2D Texture2D;
        [SerializeField] public Texture OtherTexture;
        [SerializeField] public int Width = 128;
        [SerializeField] public int Height = 128;
        [SerializeField] public bool UseTexCoord2;
        [SerializeField] public bool IsATransparentLayer;
        [SerializeField] public bool PreserveTransparency = true;
        [SerializeField] public bool IsAVolumeTexture;

        [SerializeField] internal bool enableUndoRedo;
        [SerializeField] internal float repaintDelay = 0.016f;
        [SerializeField] internal bool updateTex2DafterStroke;
        [SerializeField] internal Color clearColor = Color.black;
        [SerializeField] internal bool backupManually;
        [SerializeField] internal string saveName = "No Name";

        [SerializeField] private int _numberOfTexture2DBackups = 10;
        [SerializeField] private int _numberOfRenderTextureBackups = 10;
        [SerializeField] private bool _useTexCoord2AutoAssigned;
        [SerializeField] private float _sdfMaxInside = 1f;
        [SerializeField] private float _sdfMaxOutside = 1f;
        [SerializeField] private float _sdfPostProcessDistance = 1f;


        [NonSerialized] internal bool disableContiniousLine;
        [NonSerialized] internal bool errorWhileReading;
        [NonSerialized] internal bool dontRedoMipMaps;

        [NonSerialized] private Color[] _pixels;
        [NonSerialized] private bool _alphaPreservePixelSet;
        [NonSerialized] private bool _pixelsDirty;

        public Vector2 Tiling { get; internal set; } = Vector2.one;
        public Vector2 Offset { get; internal set; } = Vector2.zero;

        public bool NeedsToBeSaved => QcUnity.IsSavedAsAsset(Texture2D) || QcUnity.IsSavedAsAsset(RenderTexture);

        private int WidthInternal 
        {
            get 
            {
                if (Texture2D)
                    return Texture2D.width;
                if (RenderTexture)
                    return RenderTexture.width;
                return Width;
            }
        }

        private int HaightInternal
        {
            get
            {
                if (Texture2D)
                    return Texture2D.height;
                if (RenderTexture)
                    return RenderTexture.height;
                return Height;
            }
        }

        private void CheckTextureChange() 
        {
            if (Width != WidthInternal || Height != HaightInternal) 
            {
                _pixels = null;
                Debug.LogWarning("Texture size changed. Updating.");
            }
        }

        public Color[] Pixels
        {
            get {
                CheckTextureChange();

                if (_pixels == null) PixelsFromTexture2D(Texture2D); return _pixels; }
            set { _pixels = value; }
        }
        
        public void Rename(string newName)
        {
            saveName = newName;
            if (Texture2D)
                Texture2D.name = newName;
            if (RenderTexture)
                RenderTexture.name = newName;
        }


        #endregion

        #region Modules

        private ImgMetaModules _modulesContainer;

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

        private const string SavedImagesFolder = "Saved Images";

        public string SaveInPlayer()
        {
            if (Texture2D == null) return "Save Failed";

            if (Target == TexTarget.RenderTexture)
                RenderTexture_To_Texture2D();

            var png = Texture2D.EncodeToPNG();

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
                if (!Texture2D)
                    Texture2D = new Texture2D(2, 2);

                if (Texture2D.LoadImage(fileData))
                    Init(Texture2D);

                else pegi.GameView.ShowNotification("Couldn't Load Image ");

            }
        }

        #endregion

        #region Undo & Redo
        public PaintingUndoRedo.UndoCache cache = new PaintingUndoRedo.UndoCache();

        public void OnStrokeMouseDown_CheckBackup()
        {
            if (backupManually) return;

            if (enableUndoRedo)
            {
                if (Target == TexTarget.RenderTexture)
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

            if (newHeight >= 8 && newHeight <= 4096 && newWight >= 8 && newWight <= 4096 && (newWight != Width || newHeight != Height) && Texture2D)
            {

                var tmp = RenderTexture;
                RenderTexture = null;

                Texture2D_To_RenderTexture();

                Texture2D.Resize(newWight, newHeight);

                Width = newWight;

                Height = newHeight;

                Texture2D.CopyFrom(PlaytimePainter_RenderTextureBuffersManager.GetDownscaledBigRt(Width, Height));

                PixelsFromTexture2D(Texture2D);

                SetAndApply();

                RenderTexture = tmp;
                
            }

        }

        public bool Contains(Texture tex)
        {
            return tex && ((Texture2D && tex == Texture2D) || (RenderTexture && RenderTexture == tex) || (OtherTexture && tex == OtherTexture));
        }

        public RenderTexture AddRenderTexture() => AddRenderTexture(Width, Height, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, FilterMode.Bilinear, null);

        public RenderTexture AddRenderTexture(int nwidth, int nheight, RenderTextureFormat format, RenderTextureReadWrite dataType, FilterMode filterMode, string global)
        {

            if (Target == TexTarget.RenderTexture)
                RenderTexture_To_Texture2D();


            Width = nwidth;
            Height = nheight;

            RenderTexture = new RenderTexture(Width, Height, 0, format, dataType)
            {
                filterMode = filterMode,

                name = saveName
            };

            if (!global.IsNullOrEmpty())
                Shader.SetGlobalTexture(global, RenderTexture);

            if (Target == TexTarget.RenderTexture)
                Texture2D_To_RenderTexture();

            return RenderTexture;
        }

        public void Texture2D_To_RenderTexture() => Texture2DToRenderTexture(Texture2D);

        public void Texture2DToRenderTexture(Texture2D tex) => PainterCamera.Inst.Render(tex, this.CurrentRenderTexture(), Cfg.pixPerfectCopy.Shader);

        public void RenderTexture_To_Texture2D() => RenderTexture_To_Texture2D(Texture2D);

        private void RenderTexture_To_Texture2D(Texture2D tex)
        {
            if (!Texture2D)
                return;

            var rt = RenderTexture;

            TexMGMT.TryApplyBufferChangesTo(this);

            if (!rt && TexMGMT.imgMetaUsingRendTex == this)
                rt = PlaytimePainter_RenderTextureBuffersManager.GetDownscaledBigRt(Width, Height);

            //Graphics.CopyTexture();

            if (!rt)
                return;

            tex.CopyFrom(rt);

            PixelsFromTexture2D(tex);

            var converted = false;

            if (PainterCamera.Inst.IsLinearColorSpace)
            {
                if (!tex.IsColorTexture())
                {
                    converted = true;
                    PixelsToLinear();
                }
            }

            if (converted)
                SetAndApply();
            else
                Texture2D.Apply(true);
        }

        internal void ChangeDestination(TexTarget changeTo, MaterialMeta mat, ShaderProperty.TextureValue parameter, PlaytimePainter painter)
        {

            if (changeTo != Target)
            {

                if (changeTo == TexTarget.RenderTexture)
                {
                    if (!RenderTexture)
                        PainterCamera.Inst.ChangeBufferTarget(this, mat, parameter, painter);
                    Texture2DToRenderTexture(Texture2D);
                }
                else
                {
                    if (!Texture2D)
                        return;

                    if (!RenderTexture)
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
                Target = changeTo;
                painter.SetTextureOnMaterial(this);

            }
            else Debug.Log("Destination already Set");

        }

        public void SetPixelsInRam()
        {
            try
            {
                Texture2D.SetPixels(_pixels);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);

                _pixels = null;
                errorWhileReading = true;
            }
        }

        public void ApplyToTexture2D(bool mipMaps = true) => Texture2D.Apply(mipMaps, false);

        public void SetAndApply(bool mipMaps = true)
        {
            if (_pixels == null) return;
            SetPixelsInRam();
            ApplyToTexture2D(mipMaps);
        }

        public void SetApplyUpdateRenderTexture(bool mipMaps = true)
        {
            SetAndApply(mipMaps);
            if (Target == TexTarget.RenderTexture)
                Texture2D_To_RenderTexture();
        }

        public void AfterStroke(Stroke st)
        {
            if (this.TargetIsTexture2D())
                _pixelsDirty = true;
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

        internal void UnsetAlphaSavePixel()
        {
            if (_alphaPreservePixelSet)
            {
                _pixels[0].a = 1;
                SetAndApply();
            }
        }

        public void SetAlphaSavePixel()
        {
            if (!PreserveTransparency || !(Math.Abs(Pixels[0].a - 1) < float.Epsilon)) return;

            _pixels[0].a = 0.9f;
            _alphaPreservePixelSet = true;
            SetPixel_InRAM(0, 0);

        }

        public void SetPixel_InRAM(int x, int y) => Texture2D.SetPixel(x, y, _pixels[PixelNo(x, y)]);

        public void PixelsToGamma()
        {
            var p = Pixels;
            for (int i = 0; i < p.Length; i++)
                _pixels[i] = _pixels[i].gamma;
        }

        private void PixelsToLinear()
        {
            var p = Pixels;
            for (int i = 0; i < p.Length; i++)
                _pixels[i] = _pixels[i].linear;
        }

        private void UVto01(ref Vector2 uv)
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
                pix.r = r ? col.r : pix.r;
                pix.g = g ? col.g : pix.g;
                pix.b = b ? col.b : pix.b;
                pix.a = a ? col.a : pix.a;
                p[i] = pix;
            }

            return this;
        }

        public void FillWithColor(Color color)
        {
            if (Target == TexTarget.Texture2D)
            {
                SetPixels(color);
                SetAndApply();
            }
            else
            {
                PainterCamera.Inst.Render(color, this.CurrentRenderTexture());
            }
        }

        public Color SampleAt(Vector2 uv) => (Target == TexTarget.Texture2D) ? PixelSafe_Slow(UvToPixelNumber(uv)) : SampleRenderTexture(uv);

        private Color SampleRenderTexture(Vector2 uv)
        {

            var curRt = RenderTexture.active;

          
            int size = PlaytimePainter_RenderTextureBuffersManager.renderBuffersSize / 4;
            RenderTexture.active = RenderTexture ? RenderTexture : PlaytimePainter_RenderTextureBuffersManager.GetDownscaledBigRt(size, size);

            if (!_sampler) _sampler = new Texture2D(8, 8);

            UVto01(ref uv);

            if (!RenderTexture)
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
                        Width = tex.width;
                        Height = tex.height;
                        errorWhileReading = false;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    errorWhileReading = true;
                }
            }
        }

        private Color PixelSafe_Slow(MyIntVec2 v) => Pixels[PixelNo(v.x, v.y)];

        public Color PixelUnSafe(int x, int y) => _pixels[y * Width + x];

        public Color SetPixelUnSafe(int x, int y, Color col) => _pixels[y * Width + x] = col;

        public int PixelNo(MyIntVec2 v) => PixelNo(v.x, v.y);

        public int PixelNo(int x, int y)
        {

            x = ((x % Width) + Width) % Width;

            y = ((y % Height) + Height) % Height;

            return y * Width + x;
        }

        public MyIntVec2 UvToPixelNumber(Vector2 uv) => new MyIntVec2(Mathf.FloorToInt(uv.x * Width), Mathf.FloorToInt(uv.y * Height));

        public MyIntVec2 UvToPixelNumber(Vector2 uv, out Vector2 pixelOffset)
        {
            uv *= new Vector2(Width, Height);
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
                    int x = XSection * (Width - 1);
                    for (int y = 0; y < Height; y++)
                    {
                        var pix = PixelUnSafe(x, y);
                        mask.SetValuesOn(ref pix, col);
                        SetPixelUnSafe(x, y, pix);
                    }
                }

                for (int YSection = 0; YSection <= 1; YSection++)
                {
                    int y = YSection * (Height - 1);
                    for (int x = 0; x < Width; x++)
                    {
                        var pix = PixelUnSafe(x, y);
                        mask.SetValuesOn(ref pix, col);
                        SetPixelUnSafe(x, y, pix);
                    }
                }

            }


        }

        public void AddEdgePixels(Color col)
        {
            var tmpPixels = Pixels;
            int oldWidth = Width;
           // int oldHeight = height;

            Resize(Width +2, Height+2);

            for(int x=0; x<Width; x++)
                for (int y = 0; y < Height; y++)
                {
                        if (x == 0 || y == 0 || x == Width - 1 || y == Height - 1)
                            SetPixelUnSafe(x, y, col);
                        else
                            SetPixelUnSafe(x, y, tmpPixels[(y - 1) * oldWidth + x - 1]);
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

            dx = ((dx % Width) + Width) % Width;
            dy = ((dy % Height) + Height) % Height;

            for (int y = 0; y < Height; y++)
            {
                var srcOff = y * Width;
                var dstOff = ((y + dy) % Height) * Width;

                for (int x = 0; x < Width; x++)
                {

                    var srcInd = srcOff + x;

                    var dstInd = dstOff + ((x + dx) % Width);

                    _pixels[srcInd] = pixelsCopy[dstInd];


                }
            }
        }

        #endregion

        #region Init

        public TextureMeta Init(int renderTextureSize)
        {
            Width = renderTextureSize;
            Height = renderTextureSize;
            AddRenderTexture();
            Cfg.imgMetas.Insert(0, this);
            Target = TexTarget.RenderTexture;
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
                OtherTexture = tex;

            if (Cfg == null)
                return this;

            if (!Cfg.imgMetas.Contains(this))
                Cfg.imgMetas.Insert(0, this);
            return this;
        }

        public void FromRenderTextureToNewTexture2D()
        {
            Texture2D = new Texture2D(Width, Height);
            RenderTexture_To_Texture2D();
        }

        public void From(Texture2D texture, bool userClickedRetry = false)
        {

            Texture2D = texture;
            saveName = texture.name;

            if (userClickedRetry || !errorWhileReading)
            {

#if UNITY_EDITOR
                if (texture)
                {

                    var imp = texture.GetTextureImporter();
                    if (imp != null)
                    {

                        IsATransparentLayer = imp.alphaIsTransparency;

                        texture.Reimport_IfNotReadale();
                    }
                }
#endif

                PixelsFromTexture2D(Texture2D, userClickedRetry);
            }
        }

        private void UseRenderTexture(RenderTexture rt)
        {
            RenderTexture = rt;
            Width = rt.width;
            Height = rt.height;
            Target = TexTarget.RenderTexture;

#if UNITY_EDITOR
            string path = UnityEditor.AssetDatabase.GetAssetPath(rt);
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
            Target = TexTarget.Texture2D;
#if UNITY_EDITOR
            string path = UnityEditor.AssetDatabase.GetAssetPath(tex);
            if (!path.IsNullOrEmpty())
                saveName = tex.name;
            else
#endif
                if (saveName.IsNullOrEmpty())
                saveName = "New img";
        }

        #endregion

        #region Inspector
        public string NameForInspector
        {
            get { return saveName; }

            set { saveName = value; }
        }

        private int _inspectedProcess = -1;
        public int inspectedItems = -1;

        private void ReturnToRenderTexture()
        {
            var p = PlaytimePainter.inspected;
            p.UpdateOrSetTexTarget(TexTarget.RenderTexture);
        }

        private bool WasRenderTexture()
        {
            if (Target == TexTarget.RenderTexture)
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

        private string LoadTexturePegi(string path)
        {
            if ("Load {0}".F(path.Substring(path.LastIndexOf("/", StringComparison.Ordinal))).Click())
                LoadInPlayer(path);
            return path;
        }

        private TimedCoroutine _processEnumerator;

        public TimedCoroutine ProcessEnumerator
        {
            get
            {
                if (_processEnumerator != null && _processEnumerator.Exited)
                    _processEnumerator = null;

                return _processEnumerator;
            }
        }



        public void Inspect() {

            if (ProcessEnumerator != null) 
            {
                "Running Coroutine".nl();
                _processEnumerator.Inspect_AsInList();
                return;
            }

            if ("CPU blit options".isConditionally_Entered(this.TargetIsTexture2D(), ref inspectedItems, 0).nl())
            {
                "Disable Continious Lines".toggleIcon("If you see unwanted lines appearing on the texture as you paint, enable this.", ref disableContiniousLine).nl();

                "CPU blit repaint delay".edit("Delay for video memory update when painting to Texture2D", 140, ref repaintDelay, 0.01f, 0.5f).nl();

                "Don't update mipMaps".toggleIcon("May improve performance, but your changes may not disaplay if you are far from texture.",
                    ref dontRedoMipMaps);
            }

            if ("GPU blit options".isEntered(ref inspectedItems, 1).nl())
            {
                "Update Texture2D after every stroke".toggleIcon(ref updateTex2DafterStroke).nl();
            }

            #region Processors

            var newWidth = Cfg.SelectedWidthForNewTexture(); //PainterDataAndConfig.SizeIndexToSize(PainterCamera.Data.selectedWidthIndex);
            var newHeight = Cfg.SelectedHeightForNewTexture();

            if ("Texture Processors".isEntered(ref inspectedItems, 6).nl())
            {

                if (errorWhileReading)
                    "There was en error reading texture pixels, can't process it".writeWarning();
                else
                {
                    if ("Resize ({0}*{1}) => ({2}*{3})".F(Width, Height, newWidth, newHeight).isEntered(ref _inspectedProcess, 0).nl_ifFoldedOut())
                    {
                        "New Width ".select(60, ref PainterCamera.Data.selectedWidthIndex, PainterDataAndConfig.NewTextureSizeOptions).nl();

                        "New Height ".select(60, ref PainterCamera.Data.selectedHeightIndex, PainterDataAndConfig.NewTextureSizeOptions).nl();

                        if (newWidth != Width || newHeight != Height)
                        {

                            bool rescale;

                            if (newWidth <= Width && newHeight <= Height)
                                rescale = "Downscale".Click();
                            else if (newWidth >= Width && newHeight >= Height)
                                rescale = "Upscale".Click();
                            else
                                rescale = "Rescale".Click();

                            if (rescale)
                            {
                                Resize(newWidth, newHeight);
                                var pp = PlaytimePainter.inspected;
                                if (pp)
                                {
                                    if (pp.IsUsingPreview)
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

                        if ((newWidth != Width || newHeight != Height) && icon.Size.Click("Resize").nl())
                            Resize(newWidth, newHeight);

                        pegi.nl();
                    }

                    if ("Clear ".isEntered(ref _inspectedProcess, 1, false))
                    {

                        "Clear Color".edit(80, ref clearColor).nl();

                        if ("Clear Texture".Click().nl())
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

                    if ("Color to Alpha".isEntered(ref _inspectedProcess, 2).nl())
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

                    if ("Signed Distance Filelds generator".isEntered(ref _inspectedProcess, 4).nl())
                    {

                        if (Texture2D.IsColorTexture())
                        {
                            "Texture is a color texture, best to switch to non-color for SDF. Save any changes first, as the texture will reimport.".writeWarning();

#if UNITY_EDITOR
                            var ai = Texture2D.GetTextureImporter();

                            if (ai != null && "Convert to non-Color".ClickConfirm("SDFnc", "This will undo any unsaved changes. Proceed?") && ai.WasWrongIsColor(false))
                                ai.SaveAndReimport();

#endif
                        }

                        "Will convert black and white color to black and white signed field".nl();

                        "SDF Max Inside".edit(ref _sdfMaxInside).nl();
                        "SDF Max Outside".edit(ref _sdfMaxOutside).nl();
                        "SDF Post Process".edit(ref _sdfPostProcessDistance).nl();

                        bool fromGs = "From Greyscale".Click();
                        bool fromAlpha = "From Transparency".Click();

                        if (fromGs || fromAlpha) {

                            bool wasRt = WasRenderTexture();

                            var p = PlaytimePainter.inspected;
                            if (p)
                                p.UpdateOrSetTexTarget(TexTarget.Texture2D);

                            _processEnumerator = QcAsync.DefaultCoroutineManager.Add(
                                DistanceFieldProcessor.Generate(this, _sdfMaxInside, _sdfMaxOutside,
                                    _sdfPostProcessDistance, fromAlpha: fromAlpha), () => {

                                    SetAndApply();
                                    if (wasRt)
                                        ReturnToRenderTexture();
                                });
                        }

                        pegi.nl();

                    }

                    if ("Curves".isEntered(ref _inspectedProcess, 5).nl())
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

                    if ("Save Textures In Game ".isEntered(ref _inspectedProcess, 7).nl())
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
                            "Playtime Saved Textures".edit_List(Cfg.playtimeSavedTextures, LoadTexturePegi);
                    }

                    if ("Fade edges".isEntered(ref _inspectedProcess, 8).nl())
                    {

                        ("This will cahange pixels on the edges of the texture. Useful when wrap mode " +
                         "is set to clamp.").writeHint();

                        if (Texture2D)
                        {

#if UNITY_EDITOR
                            var ti = Texture2D.GetTextureImporter();
                            if (ti)
                            {
                                if (ti.wrapMode != TextureWrapMode.Clamp && "Change wrap mode from {0} to Clamp"
                                        .F(ti.wrapMode).Click().nl())
                                {
                                    ti.wrapMode = TextureWrapMode.Clamp;
                                    ti.SaveAndReimport();
                                }
                            }
#endif

                            //AddEdgePixels(Color col)
                            if ("Set edges to transparent".Click().nl())
                            {
                                SetEdges(Color.clear, ColorMask.A);
                                SetAndApply();
                            }

                            if ("Set edges to Clear Black".Click().nl())
                            {
                                SetEdges(Color.clear);
                                SetAndApply();
                            }

                            "Background Color".edit(ref clearColor);

                            if ("Apply to edges".Click())
                            {
                                SetEdges(clearColor);
                                SetAndApply();
                            }

                            pegi.nl();

                            if ("Add Edges".ClickConfirm(confirmationTag: "addEdge",
                                toolTip: "This will resize the texture"))
                            {
                                AddEdgePixels(clearColor);
                                SetAndApply();
                            }

                        }
                    }

                    if ("Add Background".isEntered(ref _inspectedProcess, 9).nl())
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

                    if ("Offset".isEntered(ref _inspectedProcess, 10).nl())
                    {

                        "X:".edit(ref _offsetByX);

                        if ((_offsetByX != Width / 2) && "{0}/{1}".F(Width / 2, Width).Click())
                            _offsetByX = Width / 2;

                        pegi.nl();

                        "Y:".edit(ref _offsetByY);

                        if ((_offsetByY != Height / 2) && "{0}/{1}".F(Height / 2, Height).Click())
                            _offsetByY = Height / 2;

                        pegi.nl();

                        if (((_offsetByX % Width != 0) || (_offsetByY % Height != 0)) && "Apply Offset".Click())
                        {
                            OffsetPixels();
                            SetAndApply();
                        }

                    }

                }
            }

            #endregion

            if ("Enable Undo for '{0}'".F(NameForInspector).isToggle_Entered(ref enableUndoRedo, ref inspectedItems, 2).nl())
            {

                "UNDOs: Tex2D".edit(80, ref _numberOfTexture2DBackups);
                "RendTex".edit(60, ref _numberOfRenderTextureBackups).nl();

                "Backup manually".toggleIcon(ref backupManually).nl();

                if (_numberOfTexture2DBackups > 50 || _numberOfRenderTextureBackups > 50)
                    "Too big of a number will eat up lot of memory".writeWarning();

                "Creating more backups will eat more memory".writeOneTimeHint("backupIsMem");
                "This are not connected to Unity's Undo/Redo because when you run out of backups you will by accident start undoing other operations.".writeOneTimeHint("noNativeUndo");
                "Use Z/X to undo/redo".writeOneTimeHint("ZxUndoRedo");

            }

            if (inspectedItems == -1)
            {
                if (IsAVolumeTexture)
                    "Is A volume texture".toggleIcon(ref IsAVolumeTexture).nl();
            }
        }

        public bool ComponentDependent_PEGI(bool showToggles, PlaytimePainter painter)
        {
            var changed = pegi.ChangeTrackStart();

            var property = painter.GetMaterialTextureProperty();

            var forceOpenUTransparentLayer = false;

            var material = painter.Material;
            
            var hasAlphaLayerTag = material && (property!=null) &&
                material.Has(property,
                    ShaderTags.LayerTypes
                        .Transparent); //GetTag(PainterDataAndConfig.ShaderTagLayerType + property, false).Equals("Transparent");

            if (!IsATransparentLayer && hasAlphaLayerTag)
            {
                "Material Field {0} is a Transparent Layer ".F(property).writeHint();
                forceOpenUTransparentLayer = true;
            }

            if (showToggles || (IsATransparentLayer && !hasAlphaLayerTag) || forceOpenUTransparentLayer)
            {
                MsgPainter.TransparentLayer.GetText().toggleIcon(ref IsATransparentLayer);

                pegi.FullWindow.DocumentationWithLinkClickOpen(
                MsgPainter.TransparentLayer.GetDescription(),
                        "https://www.quizcanners.com/single-post/2018/09/30/Why-do-I-get-black-outline-around-the-stroke",
                        "More About it");

                if (IsATransparentLayer)
                    PreserveTransparency = true;

                pegi.nl();
            }
            

            if (showToggles)
            {

                if (IsATransparentLayer)
                    PreserveTransparency = true;
                else
                {

                    MsgPainter.PreserveTransparency.GetText().toggleIcon(ref PreserveTransparency);

                    MsgPainter.PreserveTransparency.DocumentationClick();

                    pegi.nl();
                }
            }

            var forceOpenUv2 = false;
            var hasUv2Tag = painter.Material.Has(property, ShaderTags.SamplingModes.Uv2);

            if (!UseTexCoord2 && hasUv2Tag)
            {

                if (!_useTexCoord2AutoAssigned)
                {
                    UseTexCoord2 = true;
                    _useTexCoord2AutoAssigned = true;
                }
                else
                    "Material Field {0} is Sampled using Texture Coordinates 2 ".F(property).writeHint();
                forceOpenUv2 = true;
            }

            if (showToggles || (UseTexCoord2 && !hasUv2Tag) || forceOpenUv2)
                "Use UV2".toggleIcon(ref UseTexCoord2).nl();

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
                icon.UndoDisabled.draw("Nothing to Undo (set number of undo frames in config)");

            if (cache.redo.GotData)
            {
                if (icon.Redo.Click("X to Redo", ref changed))
                    cache.redo.ApplyTo(this);
            }
            else
                icon.RedoDisabled.draw("Nothing to Redo");

            pegi.nl();

            return changed;
        }

        public void InspectInList(ref int edited, int ind)
        {
            pegi.write(Texture2D);
            if (this.Click_Enter_Attention())
                edited = ind;
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
            if (_pixelsDirty)
            {

                var noTimeYet = (QcUnity.TimeSinceStartup() - _repaintTime < repaintDelay);
                if (noTimeYet && !painter.stroke.MouseUpEvent)
                    return;
                
                if (Texture2D)
                    SetAndApply(!dontRedoMipMaps);

                _pixelsDirty = false;
                _repaintTime = (float)QcUnity.TimeSinceStartup();
            }

            foreach (var m in Modules)
                m.ManagedUpdate();
        }

    }

}