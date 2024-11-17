using System;
using System.IO;
using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;
using QuizCanners.Migration;

namespace PainterTool
{
    public enum TexTarget { Texture2D, RenderTexture }

    [Flags]
    public enum TextureCfgFlags 
    {
        None = 0,
        Texcoord2 = 1,
        TransparentLayer = 2,
        PreserveTransparency = 4,
        EnableUndoRedo = 8,
    }

    [Flags]
    internal enum TextureStateFlags
    {
        DisableContiniousLine = 1,
    }

    public class TextureMeta : PainterClass, IPEGI, IPEGI_ListInspect, IGotName, INeedAttention //, ICanBeDefaultCfg
    {
        #region Values

        private static Texture2D _sampler;

        [SerializeField] private TextureCfgFlags _configurationFlags;
        [NonSerialized] private TextureStateFlags textureStateFlags;

        [SerializeField] internal TexTarget Target;
        [SerializeField] internal RenderTexture RenderTexture;
        [SerializeField] public Texture2D Texture2D;
        [SerializeField] internal Texture OtherTexture;
        [SerializeField] public int Width = 128;
        [SerializeField] public int Height = 128;

        [SerializeField] public bool IsAVolumeTexture;
        [SerializeField] internal bool updateTex2DafterStroke;
        [SerializeField] internal bool backupManually;
        [SerializeField] private bool _useTexCoord2AutoAssigned;
        [SerializeField] private bool _isPng;

        [NonSerialized] internal bool errorWhileReading;
        [NonSerialized] internal bool dontRedoMipMaps;
        [NonSerialized] private bool _alphaPreservePixelSet;
        [NonSerialized] private bool _pixelsDirty;

        [SerializeField] internal float repaintDelay = 0.016f;
        [SerializeField] internal Color clearColor = Color.black;
        [SerializeField] public string saveName = "No Name";

        [SerializeField] private int _numberOfTexture2DBackups = 10;
        [SerializeField] private int _numberOfRenderTextureBackups = 10;
        [SerializeField] private float _sdfMaxInside = 1f;
        [SerializeField] private float _sdfMaxOutside = 1f;
        [SerializeField] private float _sdfPostProcessDistance = 1f;

        [NonSerialized] private Color[] _pixels;

        public bool this[TextureCfgFlags val]
        {
            get => _configurationFlags.HasFlag(val);
            set
            {
                if (value)
                    _configurationFlags |= val;
                else
                    _configurationFlags &= ~val;
            }
        }

        internal bool this[TextureStateFlags val]
        {
            get => textureStateFlags.HasFlag(val);
            set
            {
                if (value)
                    textureStateFlags |= val;
                else
                    textureStateFlags &= ~val;
            }
        }

        public bool IsReadable
        {
            get => !Texture2D || Texture2D.isReadable;

            set
            {
                if (value && Texture2D && Texture2D.isReadable == false)
                {
#if UNITY_EDITOR
                    Texture2D.Reimport_IfNotReadale_Editor();
#endif
                }
                else
                {
                    Debug.LogError("Is Readable = false not implemented");
                }
            }
        }

        public bool IsPNG  => _isPng;
        
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
        

        #endregion

        #region Modules

        private ImgMetaModules _modulesContainer;

        public ImgMetaModules Modules
        {
            get
            {
                _modulesContainer ??= new ImgMetaModules(this);

                return _modulesContainer;
            }
        }

        public T GetModule<T>() where T : ImageMetaModuleBase => Modules.GetModule<T>();

        public class ImgMetaModules : TaggedModulesList<ImageMetaModuleBase>
        {

            public override void OnAfterInitialize()
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

            Painter.Data.playtimeSavedTextures.Add(fullPath);

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
        public PaintingUndoRedo.UndoCache cache = new();

        public void OnStrokeMouseDown_CheckBackup()
        {
            if (backupManually) return;

            if (this[TextureCfgFlags.EnableUndoRedo])
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

                Texture2D.Reinitialize(newWight, newHeight);

                Width = newWight;

                Height = newHeight;

                Texture2D.CopyFrom(RenderTextureBuffersManager.GetDownscaledBigRt(Width, Height));

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

        public void Texture2DToRenderTexture(Texture2D tex) => Painter.Camera.Render(tex, this.CurrentRenderTexture(), Painter.Data.pixPerfectCopy.Shader);

        public void RenderTexture_To_Texture2D() => RenderTexture_To_Texture2D(Texture2D);

        private void RenderTexture_To_Texture2D(Texture2D tex)
        {
            if (!Texture2D)
                return;

            var rt = RenderTexture;

            Painter.Camera.TryApplyBufferChangesTo(this);

            if (!rt && Painter.Camera.imgMetaUsingRendTex == this)
                rt = RenderTextureBuffersManager.GetDownscaledBigRt(Width, Height);

            //Graphics.CopyTexture();

            if (!rt)
                return;

            tex.CopyFrom(rt);

            PixelsFromTexture2D(tex);

            var converted = false;

            if (Painter.IsLinearColorSpace)
            {
                if (!tex.IsColorTexture())
                {
                    converted = true;
                    ConvertPixelsToLinear();
                }
            }

            if (converted)
                SetAndApply();
            else
                Texture2D.Apply(true);
        }

        internal void ChangeDestination(TexTarget changeTo, MaterialMeta mat, ShaderProperty.TextureValue parameter, PainterComponent painter)
        {

            if (changeTo != Target)
            {

                if (changeTo == TexTarget.RenderTexture)
                {
                    if (!RenderTexture)
                        Painter.Camera.ChangeBufferTarget(this, mat, parameter, painter);
                    Texture2DToRenderTexture(Texture2D);
                }
                else
                {
                    if (!Texture2D)
                        return;

                    if (!RenderTexture)
                    {
                        Painter.Camera.EmptyBufferTarget();
                        Painter.Camera.DiscardAlphaBuffer();
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
#pragma warning disable UNT0017 // SetPixels invocation is slow
                Texture2D.SetPixels(_pixels);
#pragma warning restore UNT0017 // SetPixels invocation is slow
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

            st.TrySetPreviousValues();
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
            if (!this[TextureCfgFlags.PreserveTransparency] || !(Math.Abs(Pixels[0].a - 1) < float.Epsilon)) return;

            _pixels[0].a = 0.9f;
            _alphaPreservePixelSet = true;
            SetPixel_InRAM(0, 0);

        }

        public void SetPixel_InRAM(int x, int y) => Texture2D.SetPixel(x, y, _pixels[PixelNo(x, y)]);

        public void ConvertPixelsToGamma()
        {
            var p = Pixels;
            for (int i = 0; i < p.Length; i++)
                _pixels[i] = _pixels[i].gamma;
        }

        private void ConvertPixelsToLinear()
        {
            var p = Pixels;
            for (int i = 0; i < p.Length; i++)
                _pixels[i] = _pixels[i].linear;
        }

        private void ConvertUVto01(ref Vector2 uv)
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
                Painter.Camera.Prepare(color, this.CurrentRenderTexture()).Render();
            }
        }

        public Color SampleAt(Vector2 uv) => (Target == TexTarget.Texture2D) ? PixelSafe_Slow(UvToPixelNumber(uv)) : SampleRenderTexture(uv);

        private Color SampleRenderTexture(Vector2 uv)
        {

            var curRt = RenderTexture.active;

          
            int size = RenderTextureBuffersManager.renderBuffersSize / 4;
            RenderTexture.active = RenderTexture ? RenderTexture : RenderTextureBuffersManager.GetDownscaledBigRt(size, size);

            if (!_sampler) _sampler = new Texture2D(8, 8);

            ConvertUVto01(ref uv);

            if (!RenderTexture)
                uv.y = 1 - uv.y; // For some reason sampling is mirrored around Y axiz for BigRenderTexture (?)

            uv *= RenderTexture.active.width;

            _sampler.ReadPixels(new Rect(uv.x, uv.y, 1, 1), 0, 0);

            RenderTexture.active = curRt;

            var pix = _sampler.GetPixel(0, 0);

            if (Painter.IsLinearColorSpace)
                pix = pix.linear;

            return pix;
        }

        public void PixelsFromTexture2D(Texture2D tex, bool forceRetry = false)
        {
            if (forceRetry || !errorWhileReading)
            {
                try
                {
                    if (tex)
                    {
                        if (IsReadable)
                        {
                            Pixels = tex.GetPixels();
                        }
                        else
                            Pixels = null;

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

        private Color PixelSafe_Slow(Vector2Int v) => Pixels[PixelNo(v.x, v.y)];

        public Color PixelUnSafe(int x, int y) => _pixels[y * Width + x];

        public Color SetPixelUnSafe(int x, int y, Color col) => _pixels[y * Width + x] = col;

        public int PixelNo(Vector2Int v) => PixelNo(v.x, v.y);

        public int PixelNo(int x, int y)
        {

            x = ((x % Width) + Width) % Width;

            y = ((y % Height) + Height) % Height;

            return y * Width + x;
        }

        public Vector2Int UvToPixelNumber(Vector2 uv) => new(Mathf.FloorToInt(uv.x * Width), Mathf.FloorToInt(uv.y * Height));

        public Vector2Int UvToPixelNumber(Vector2 uv, out Vector2 pixelOffset)
        {
            uv *= new Vector2(Width, Height);
            var result = new Vector2Int(Mathf.RoundToInt(uv.x), Mathf.RoundToInt(uv.y));

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
            Painter.Data.imgMetas.Insert(0, this);
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

            if (Painter.Data == null)
                return this;

            if (!Painter.Data.imgMetas.Contains(this))
                Painter.Data.imgMetas.Insert(0, this);
            return this;
        }

        public void From(Texture2D texture, bool userClickedRetry = false)
        {

            Texture2D = texture;
            saveName = texture.name;

            if (userClickedRetry || !errorWhileReading)
            {


                if (Texture2D)
                {
                    _isPng = Texture2D.TextureHasAlpha();

#if UNITY_EDITOR

                    var assetPath = UnityEditor.AssetDatabase.GetAssetPath(Texture2D);
                    var extension = assetPath[(assetPath.LastIndexOf(".", StringComparison.Ordinal) + 1)..];

                    _isPng = extension == "png";

                    var imp = Texture2D.GetTextureImporter_Editor();
                    if (imp != null)
                    {
                        this[TextureCfgFlags.TransparentLayer] = imp.alphaIsTransparency;
                    }
#endif
                }

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

        private void ReturnToRenderTexture()
        {
            var p = PainterComponent.inspected;
            p.UpdateOrSetTexTarget(TexTarget.RenderTexture);
        }

        private bool WasRenderTexture()
        {
            if (Target == TexTarget.RenderTexture)
            {
                var p = PainterComponent.inspected;
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
            if ("Load {0}".F(path[path.LastIndexOf("/", StringComparison.Ordinal)..]).PegiLabel().Click())
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

        public pegi.EnterExitContext processContext = new();
        public pegi.EnterExitContext context = new();

        void IPEGI.Inspect() {

            using (context.StartContext())
            {
                if (ProcessEnumerator != null)
                {
                    "Running Coroutine".PegiLabel().Nl();
                    _processEnumerator.InspectInList_Nested();
                    return;
                }

                if ("CPU blit options".PegiLabel().IsConditionally_Entered(this.TargetIsTexture2D()).Nl())
                {
                    var tmp = this[TextureStateFlags.DisableContiniousLine];
                    "Disable Continious Lines".PegiLabel("If you see unwanted lines appearing on the texture as you paint, enable this.").ToggleIcon(ref tmp).Nl().OnChanged(()=> this[TextureStateFlags.DisableContiniousLine] = tmp);

                    "CPU blit repaint delay".PegiLabel("Delay for video memory update when painting to Texture2D", 140).Edit(ref repaintDelay, 0.01f, 0.5f).Nl();

                    "Don't update mipMaps".PegiLabel("May improve performance, but your changes may not disaplay if you are far from texture.").ToggleIcon(ref dontRedoMipMaps);
                }

                if ("GPU blit options".PegiLabel().IsEntered().Nl())
                {
                    "Update Texture2D after every stroke".PegiLabel().ToggleIcon(ref updateTex2DafterStroke).Nl();
                }

                var newWidth = Painter.Data.SelectedWidthForNewTexture(); //PainterDataAndConfig.SizeIndexToSize(PainterCamera.Data.selectedWidthIndex);
                var newHeight = Painter.Data.SelectedHeightForNewTexture();

                if ("Texture Processors".PegiLabel().IsEntered().Nl())
                {
                    if (errorWhileReading)
                        "There was en error reading texture pixels, can't process it".PegiLabel().WriteWarning();
                    else
                    {
                        using (processContext.StartContext())
                        {

                            if ("Resize ({0}*{1}) => ({2}*{3})".F(Width, Height, newWidth, newHeight).PegiLabel().IsEntered().Nl_ifEntered())
                            {
                                "New Width".ConstLabel().Select(ref Painter.Data.selectedWidthIndex, SO_PainterDataAndConfig.NewTextureSizeOptions).Nl();

                                "New Height".ConstLabel().Select(ref Painter.Data.selectedHeightIndex, SO_PainterDataAndConfig.NewTextureSizeOptions).Nl();

                                if (newWidth != Width || newHeight != Height)
                                {
                                    bool rescale;

                                    if (newWidth <= Width && newHeight <= Height)
                                        rescale = "Downscale".PegiLabel().Click();
                                    else if (newWidth >= Width && newHeight >= Height)
                                        rescale = "Upscale".PegiLabel().Click();
                                    else
                                        rescale = "Rescale".PegiLabel().Click();

                                    if (rescale)
                                    {
                                        Resize(newWidth, newHeight);
                                        var pp = PainterComponent.inspected;
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
                                pegi.Nl();
                            }

                            if (processContext.IsAnyEntered == false)
                            {

                                if ((newWidth != Width || newHeight != Height) && Icon.Size.Click("Resize").Nl())
                                    Resize(newWidth, newHeight);

                                pegi.Nl();
                            }

                            if ("Clear ".PegiLabel().IsEntered(false))
                            {

                                "Clear Color".ConstLabel().Edit(ref clearColor).Nl();

                                if ("Clear Texture".PegiLabel().Click().Nl())
                                {
                                    FillWithColor(clearColor);
                                    //SetPixels(clearColor);
                                    //SetApplyUpdateRenderTexture();
                                }
                            }

                            if (processContext.IsAnyEntered == false && Icon.Refresh.Click("Apply color {0}".F(clearColor)).Nl())
                            {
                                FillWithColor(clearColor);
                                //SetPixels(clearColor);
                                //SetApplyUpdateRenderTexture();
                            }

                            if ("Color to Alpha".PegiLabel().IsEntered().Nl())
                            {

                                "Background Color".ConstLabel().Edit(ref clearColor).Nl();
                                if (Pixels != null)
                                {

                                    if ("Color to Alpha".PegiLabel("Will Convert Background Color with transparency").Click().Nl())
                                    {
                                        bool wasRt = WasRenderTexture();

                                        for (int i = 0; i < _pixels.Length; i++)
                                            _pixels[i] = BlitFunctions.ColorToAlpha(_pixels[i], clearColor);

                                        SetAndApply();

                                        if (wasRt)
                                            ReturnToRenderTexture();
                                    }

                                    if ("Color from Alpha".PegiLabel("Will subtract background color from transparency").Click().Nl())
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

                            if ("Signed Distance Filelds generator".PegiLabel().IsEntered().Nl())
                            {

                                if (Texture2D.IsColorTexture())
                                {
                                    "Texture is a color texture, best to switch to non-color for SDF. Save any changes first, as the texture will reimport.".PegiLabel().WriteWarning();

#if UNITY_EDITOR
                                    var ai = Texture2D.GetTextureImporter_Editor();

                                    if (ai != null && "Convert to non-Color".PegiLabel("This will undo any unsaved changes. Proceed?").ClickConfirm("SDFnc") && ai.WasWrongIsColor_Editor(false))
                                        ai.SaveAndReimport();

#endif
                                }

                                "Will convert black and white color to black and white signed field".PegiLabel().Nl();

                                "SDF Max Inside".PegiLabel().Edit(ref _sdfMaxInside).Nl();
                                "SDF Max Outside".PegiLabel().Edit(ref _sdfMaxOutside).Nl();
                                "SDF Post Process".PegiLabel().Edit(ref _sdfPostProcessDistance).Nl();

                                bool fromGs = "From Greyscale".PegiLabel().Click();
                                bool fromAlpha = "From Transparency".PegiLabel().Click();

                                if (fromGs || fromAlpha)
                                {

                                    bool wasRt = WasRenderTexture();

                                    var p = PainterComponent.inspected;
                                    if (p)
                                        p.UpdateOrSetTexTarget(TexTarget.Texture2D);

                                    _processEnumerator = QcAsync.DefaultCoroutineManager.Add(
                                        DistanceFieldProcessor.Generate(this, _sdfMaxInside, _sdfMaxOutside,
                                            _sdfPostProcessDistance, fromAlpha: fromAlpha), () =>
                                            {

                                                SetAndApply();
                                                if (wasRt)
                                                    ReturnToRenderTexture();
                                            });
                                }

                                pegi.Nl();

                            }

                            if ("Curves".PegiLabel().IsEntered().Nl())
                            {
                                var crv = Painter.Camera.InspectAnimationCurve("Channel");

                                if (Pixels != null)
                                {

                                    if ("Remap Alpha".PegiLabel().Click())
                                    {
                                        for (int i = 0; i < _pixels.Length; i++)
                                        {
                                            var col = _pixels[i];
                                            col.a = crv.Evaluate(col.a);
                                            _pixels[i] = col;
                                        }
                                        SetApplyUpdateRenderTexture();
                                    }

                                    if ("Remap Color".PegiLabel().Click())
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

                            if ("Save Textures In Game ".PegiLabel().IsEntered().Nl())
                            {

                                "This is intended to test playtime saving. The functions to do so are quite simple. You can find them inside ImageData.cs class.".PegiLabel()
                                    .Write_Hint();

                                pegi.Nl();

                                "Save Name".ConstLabel().Edit(ref saveName);

                                if (Icon.Folder.Click("Open Folder with textures").Nl())
                                    QcFile.Explorer.OpenPersistentFolder(SavedImagesFolder);

                                if ("Save Playtime".PegiLabel("Will save to {0}/{1}".F(Application.persistentDataPath, saveName)).Click().Nl())
                                    SaveInPlayer();

                                if (Painter.Data && Painter.Data.playtimeSavedTextures.Count > 0)
                                    "Playtime Saved Textures".PegiLabel().Edit_List(Painter.Data.playtimeSavedTextures, LoadTexturePegi);
                            }

                            if ("Fade edges".PegiLabel().IsEntered().Nl())
                            {

                                ("This will cahange pixels on the edges of the texture. Useful when wrap mode " +
                                 "is set to clamp.").PegiLabel().Write_Hint();

                                if (Texture2D)
                                {

#if UNITY_EDITOR
                                    var ti = Texture2D.GetTextureImporter_Editor();
                                    if (ti)
                                    {
                                        if (ti.wrapMode != TextureWrapMode.Clamp && "Change wrap mode from {0} to Clamp"
                                                .F(ti.wrapMode).PegiLabel().Click().Nl())
                                        {
                                            ti.wrapMode = TextureWrapMode.Clamp;
                                            ti.SaveAndReimport();
                                        }
                                    }
#endif

                                    //AddEdgePixels(Color col)
                                    if ("Set edges to transparent".PegiLabel().Click().Nl())
                                    {
                                        SetEdges(Color.clear, ColorMask.A);
                                        SetAndApply();
                                    }

                                    if ("Set edges to Clear Black".PegiLabel().Click().Nl())
                                    {
                                        SetEdges(Color.clear);
                                        SetAndApply();
                                    }

                                    "Background Color".PegiLabel().Edit(ref clearColor);

                                    if ("Apply to edges".PegiLabel().Click())
                                    {
                                        SetEdges(clearColor);
                                        SetAndApply();
                                    }

                                    pegi.Nl();

                                    if ("Add Edges".PegiLabel(toolTip: "This will resize the texture").ClickConfirm(confirmationTag: "addEdge"))
                                    {
                                        AddEdgePixels(clearColor);
                                        SetAndApply();
                                    }

                                }
                            }

                            if ("Add Background".PegiLabel().IsEntered().Nl())
                            {

                                "Background Color".ConstLabel().Edit(ref clearColor).Nl();

                                if ("Add Background".PegiLabel("Will Add Beckground color and make everything non transparent").Click().Nl())
                                {

                                    bool wasRt = WasRenderTexture();

                                    for (int i = 0; i < _pixels.Length; i++)
                                        _pixels[i] = BlitFunctions.AddBackground(_pixels[i], clearColor);

                                    SetAndApply();

                                    if (wasRt)
                                        ReturnToRenderTexture();
                                }

                            }

                            if ("Offset".PegiLabel().IsEntered().Nl())
                            {

                                "X:".PegiLabel().Edit(ref _offsetByX);

                                if ((_offsetByX != Width / 2) && "{0}/{1}".F(Width / 2, Width).PegiLabel().Click())
                                    _offsetByX = Width / 2;

                                pegi.Nl();

                                "Y:".PegiLabel().Edit(ref _offsetByY);

                                if ((_offsetByY != Height / 2) && "{0}/{1}".F(Height / 2, Height).PegiLabel().Click())
                                    _offsetByY = Height / 2;

                                pegi.Nl();

                                if (((_offsetByX % Width != 0) || (_offsetByY % Height != 0)) && "Apply Offset".PegiLabel().Click())
                                {
                                    OffsetPixels();
                                    SetAndApply();
                                }

                            }
                        }
                    }
                }

                var lbl = "Enable Undo for '{0}'".F(NameForInspector).PegiLabel();

                if (context.IsAnyEntered == false)
                {
                    var undoRedo = this[TextureCfgFlags.EnableUndoRedo];
                    lbl.ToggleIcon(ref undoRedo, hideTextWhenTrue: true).Nl().OnChanged(()=> this[TextureCfgFlags.EnableUndoRedo] = undoRedo);
                }


                if (lbl.IsConditionally_Entered(canEnter: this[TextureCfgFlags.EnableUndoRedo]).Nl())
                {
                    "UNDOs: Tex2D".ConstLabel().Edit(ref _numberOfTexture2DBackups);
                    "RendTex".ConstLabel().Edit(ref _numberOfRenderTextureBackups).Nl();

                    "Backup manually".PegiLabel().ToggleIcon(ref backupManually).Nl();

                    if (_numberOfTexture2DBackups > 50 || _numberOfRenderTextureBackups > 50)
                        "Too big of a number will eat up lot of memory".PegiLabel().WriteWarning();

                    "Creating more backups will eat more memory".PegiLabel().WriteOneTimeHint("backupIsMem");
                    "This are not connected to Unity's Undo/Redo because when you run out of backups you will by accident start undoing other operations.".PegiLabel().WriteOneTimeHint("noNativeUndo");
                    "Use Z/X to undo/redo".PegiLabel().WriteOneTimeHint("ZxUndoRedo");
                }

                pegi.Nl();

                if (!context.IsAnyEntered && IsAVolumeTexture)
                        "Is A volume texture".PegiLabel().ToggleIcon(ref IsAVolumeTexture).Nl();
            }
        }

        public bool ComponentDependent_PEGI(bool showToggles, PainterComponent painter)
        {
            showToggles &= !context.IsAnyEntered;

            var changed = pegi.ChangeTrackStart();

            var property = painter.GetMaterialTextureProperty();

            var forceOpenUTransparentLayer = false;

            var material = painter.Material;
            
            var hasAlphaLayerTag = material && (property!=null) &&
                material.Has(property,
                    ShaderTags.LayerTypes
                        .Transparent); //GetTag(PainterDataAndConfig.ShaderTagLayerType + property, false).Equals("Transparent");

            if (!this[TextureCfgFlags.TransparentLayer] && hasAlphaLayerTag)
            {
                "Material Field {0} is a Transparent Layer ".F(property).PegiLabel().Write_Hint();
                forceOpenUTransparentLayer = true;
            }

            if (showToggles || (this[TextureCfgFlags.TransparentLayer] && !hasAlphaLayerTag) || forceOpenUTransparentLayer)
            {
                var isTp = this[TextureCfgFlags.TransparentLayer];
                MsgPainter.TransparentLayer.GetText().PegiLabel().ToggleIcon(ref isTp).OnChanged(()=> this[TextureCfgFlags.TransparentLayer] = isTp);

                pegi.FullWindow.DocumentationWithLinkClickOpen(
                MsgPainter.TransparentLayer.GetDescription(),
                        "https://www.quizcanners.com/single-post/2018/09/30/Why-do-I-get-black-outline-around-the-stroke",
                        "More About it");

                if (this[TextureCfgFlags.TransparentLayer])
                    this[TextureCfgFlags.PreserveTransparency] = true;

                pegi.Nl();
            }
            

            if (showToggles)
            {

                if (this[TextureCfgFlags.TransparentLayer])
                    this[TextureCfgFlags.PreserveTransparency] = true;
                else
                {
                    var pts = this[TextureCfgFlags.PreserveTransparency];
                    MsgPainter.PreserveTransparency.GetText().PegiLabel().ToggleIcon(ref pts).OnChanged(()=> this[TextureCfgFlags.PreserveTransparency] = pts);

                    MsgPainter.PreserveTransparency.DocumentationClick();

                    pegi.Nl();
                }
            }

            var forceOpenUv2 = false;
            var hasUv2Tag = painter.Material.Has(property, ShaderTags.SamplingModes.Uv2);

            if (!this[TextureCfgFlags.Texcoord2] && hasUv2Tag)
            {
                if (!_useTexCoord2AutoAssigned)
                {
                    this[TextureCfgFlags.Texcoord2] = true;
                    _useTexCoord2AutoAssigned = true;
                }
                else
                    "Material Field {0} is Sampled using Texture Coordinates 2 ".F(property).PegiLabel().Write_Hint();
                forceOpenUv2 = true;
            }

            if (showToggles || (this[TextureCfgFlags.Texcoord2] && !hasUv2Tag) || forceOpenUv2)
            {
                var uv2 = this[TextureCfgFlags.Texcoord2];
                "Use UV2".PegiLabel().ToggleIcon(ref uv2).Nl().OnChanged(()=> this[TextureCfgFlags.Texcoord2] = uv2);
            }

            return changed;
        }

        public void Undo_redo_PEGI()
        {
            if (this[TextureCfgFlags.EnableUndoRedo] == false)
                return;

            cache ??= new PaintingUndoRedo.UndoCache();

            if (cache.undo.GotData)
            {
                if (Icon.Undo.Click("Press Z to undo (Scene View)"))
                    cache.undo.ApplyTo(this);
            }
            else
                Icon.UndoDisabled.Draw("Nothing to Undo (set number of undo frames in config)");

            if (cache.redo.GotData)
            {
                if (Icon.Redo.Click("X to Redo"))
                    cache.redo.ApplyTo(this);
            }
            else
                Icon.RedoDisabled.Draw("Nothing to Redo");

            pegi.Nl();
        }

        public void InspectInList(ref int edited, int ind)
        {
            pegi.Write(Texture2D);
            if (this.Click_Enter_Attention())
                edited = ind;
        }

        public void InspectConvestionOptions(PainterComponent painter)
        {

            if (IsPNG && IsReadable)
                return;

            pegi.Nl();

            using (pegi.Indent())
            {

                pegi.Line(Color.red);

                if (!IsPNG && !IsReadable)
                {
                    "Texture isn't readable and isn't in PNG format. Can't edit. ".PegiLabel().WriteWarning();
                    pegi.Nl();
                    if (Application.isEditor)
                    {
                        "Convert Options:".PegiLabel().Nl();// pegi.Styles.ListLabel);

                        " ".PegiLabel(10).Write();

                        if ("Copy to PNG".PegiLabel().Click())
                            ConvertToPngIfNeeded(painter);

                        if ("Readable without Alpha".PegiLabel().Click())
                            IsReadable = true;
                    }

                }
                else if (!IsPNG)
                {

                    "Texture isn't in PNG format, transparency edits may not persist".PegiLabel().WriteWarning();
                    pegi.Nl();
                    if (Application.isEditor)
                    {
                        if ("Create .png copy".PegiLabel().Click())
                            ConvertToPngIfNeeded(painter);
                    }

                }
                else if (!IsReadable)
                {

                    "Texture wasn't marked as readable, can't edit".PegiLabel().WriteWarning();
                    pegi.Nl();

                    if (Application.isEditor)
                    {
                        if ("Make Texture Readable".PegiLabel().Click())
                            IsReadable = true;
                    }
                }

                pegi.Nl();
                pegi.Line(Color.red);


            }
        }

        public string NeedAttention()
        {
            if (_numberOfTexture2DBackups > 50)
                return "Too many backups";
            return null;
        }

        #endregion

        public void ConvertToPngIfNeeded(PainterComponent painter) 
        {
#if UNITY_EDITOR

            var t2D = Texture2D as Texture2D;

            if (t2D)
            {
                var imp = t2D.GetTextureImporter_Editor();
                if (imp)
                {
                    var assetPath = UnityEditor.AssetDatabase.GetAssetPath(Texture2D);
                    var extension = assetPath[(assetPath.LastIndexOf(".", StringComparison.Ordinal) + 1)..];

                    _isPng = extension == "png";

                    if (!_isPng)
                    {
                        pegi.GameView.ShowNotification("Converting {0} to .png".F(assetPath));
                        Texture2D = QcUnity.CreatePngSameDirectory(t2D, t2D.name);
                        PixelsFromTexture2D(Texture2D, false);

                        _isPng = true;

                        if (painter)
                            painter.SetTextureOnMaterial(this);
                    }

                }
            }
#endif
        }

        private float _repaintTime;

        public void ManagedUpdate(PainterComponent painter)
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