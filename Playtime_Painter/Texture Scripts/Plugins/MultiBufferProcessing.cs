using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using UnityEngine;
using QuizCannersUtilities;
using PlayerAndEditorGUI;
using System.Linq;

namespace Playtime_Painter {

    #region Manager
    [TaggedType(tag)]
    public class MultiBufferProcessing : PainterManagerPluginBase, IPainterManagerPluginComponentPEGI, IPainterManagerPluginOnGUI
    {
        const string tag = "MltBffr";
        public override string ClassTag => tag;

        public static MultiBufferProcessing inst;

        public static List<RenderSection> sections = new List<RenderSection>();
        public static List<TextureBuffer> buffers = new List<TextureBuffer>();

        #region Encode & Decode
        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add("s", buffers)
            .Add("sc", sections)
            .Add_Bool("gui", _showOnGui);

        public override bool Decode(string tg, string data) {
            switch (tg) {
                case "s": data.Decode_List(out buffers); break;
                case "sc": data.Decode_List(out sections); break;
                case "gui": _showOnGui = data.ToBool(); break;
                default: return false;
            }
            return true;
        }
        #endregion

        private bool _pauseBuffers;

        private bool _showOnGui;

        public override void Update() {
            if (!_pauseBuffers)
                sections.ForEach(s => s.Update());
        }

        #region Inspector
        #if PEGI
        public override string NameForDisplayPEGI => "Buffer Blitting";

        int _guiTextureSize = 128;
        private int _texIndex;
        
        public void OnGUI()  {

            if (_pauseBuffers || !_showOnGui)
                return;

            _texIndex = 0;

            const int textGap = 30;
            var fullSize = textGap + _guiTextureSize;

            var inColumn = Screen.height / fullSize;

            foreach (var b in buffers)
                if (b != null && b.showOnGui)
                {

                    var sct = sections.Where(s => s.TargetRenderTexture != null && s.TargetRenderTexture == b);
                    var count = sct.Count();
                    var section = count > 0 ? sct.First() : null;

                    var tex = b.GetTextureDisplay;

                    var pos = new Rect((_texIndex / inColumn) * _guiTextureSize, (_texIndex % inColumn) * fullSize - textGap, _guiTextureSize, _guiTextureSize);
                    GUI.Label(pos, b.ToPEGIstring());
                    pos.y += textGap;

                    if (tex)
                    {
                        if (section != null && section.previewMaterial)
                            Graphics.DrawTexture(pos, tex, section.previewMaterial);
                        else
                            GUI.DrawTexture(pos, tex);
                    }

                    _texIndex++;

                    pos.width = 25;
                    pos.height = 25;

                    if (section != null && GUI.Button(pos, icon.Refresh.GetIcon()))
                        section.Blit();
                }

        }

        public override bool Inspect()
        {
            var changed = false;

            if ((_pauseBuffers ? icon.Play : icon.Pause).Click("Stop/Start ALL"))
            {
                _pauseBuffers = !_pauseBuffers;
                if (_pauseBuffers)
                    foreach (var b in buffers)
                        b.Stop();
            }


            if (_editedSection == -1 && _editedBuffer == -1)
            {
                "Show on GUI".toggleIcon(ref _showOnGui).nl(ref changed);
                "Size: ".edit(40, ref _guiTextureSize, 128, 512).nl(ref changed);
            }

            if (_editedBuffer == -1)
                "Sections".edit_List(ref sections, ref _editedSection).changes(ref changed);
            else
                _editedSection = -1;

            if (_editedSection == -1)
                "Buffers".edit_List(ref buffers, ref _editedBuffer).changes(ref changed);

            return changed;
        }

        private int _editedBuffer = -1;
        private int _editedSection = -1;

        public bool ComponentInspector()
        {
            var changed = false;

            if (buffers.Count <= 0) return changed;
            
            if ((_pauseBuffers ? icon.Play : icon.Pause).Click("Stop/Start ALL"))
                _pauseBuffers = !_pauseBuffers;

            var cur = -1;
            if ("Buffers".@select(60, ref cur, buffers, (x) => x.CanBeAssignedToPainter).nl(ref changed))
                InspectedPainter.SetTextureOnMaterial(buffers.TryGet(cur).GetTextureDisplay);

            return changed;
        }

        #endif
        #endregion

        public override void Enable()
        {
            inst = this;
        }

    }

    #endregion

    #region TextureBuffers

    [DerivedList(typeof(OnDemandRT)
        , typeof(OnDemandRtPair)
        , typeof(WebCamTextureBuffer)
        , typeof(CustomImageData)
        , typeof(SectionTarget)
        , typeof(BigRtPair)
        , typeof(DownScalar)
        )]
    public class TextureBuffer : AbstractKeepUnrecognizedStd, IPEGI_ListInspect, IGotDisplayName {
        
        private int _version = 0;

        public virtual int Version { get { return _version; } set { _version = value; } }

        public bool showOnGui;

        protected static MultiBufferProcessing Mgmt => MultiBufferProcessing.inst; 

        protected static PainterCamera TexMGMT => PainterCamera.Inst; 

        protected static PainterDataAndConfig Data => PainterCamera.Data; 

        public virtual string NameForDisplayPEGI => "Override This";

        public virtual Texture TextureNext { get; }

        public virtual Texture GetTextureDisplay => TextureNext;

        public virtual RenderTexture GetTargetTextureNext => GetRenderTexture;
        
        protected virtual RenderTexture GetRenderTexture => null;

        public virtual void AfterRender() { }

        public virtual bool CanBeTarget => false;

        public virtual bool CanBeAssignedToPainter => false;

        public virtual bool IsReady => true;

        public virtual bool BlitMethod(TextureBuffer sourceBuffer, Material material, Shader shader)
        {
            var trg = GetTargetTextureNext;
            var src = sourceBuffer?.TextureNext;

            if (!trg) return false;
            
            if (src)
                PainterDataAndConfig.SOURCE_TEXTURE.GlobalValue = src;

            if (material)
                Graphics.Blit(src, trg, material);
            else
                PainterCamera.Inst.Render(src, trg, shader);
            AfterRender();

            Version++;

            return true;


        }

        public virtual void Stop() { }

        #region Inspector
        #if PEGI
        public override bool Inspect()
        {
            var changed = false;

            "Show On GUI".toggle(ref showOnGui).nl(ref changed);

            return changed;
        }

        public virtual bool PEGI_inList(IList list, int ind, ref int edited)
        {
            var changed = NameForDisplayPEGI.toggle(ref showOnGui);

            var asP = this as IPEGI;
            if (asP != null && icon.Enter.Click())
                edited = ind;

            return changed;
        }

        #endif
        #endregion

        #region Encode & Decode
        public override StdEncoder Encode() =>this.EncodeUnrecognized().Add_IfTrue("show", showOnGui);

        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "show": showOnGui = data.ToBool(); break;
                default: return false;
            }
            return true;
        }
        #endregion
    }

    public class CustomImageData : TextureBuffer, IPEGI
    {

        ImageMeta id;
        private Texture Texture => id.CurrentTexture();

        public override bool CanBeTarget => Texture && Texture.GetType() == typeof(RenderTexture);

        public override Texture TextureNext => Texture;

        protected override RenderTexture GetRenderTexture => Texture ? (RenderTexture)Texture : null;

        #region Inspector
#if PEGI
        public override string NameForDisplayPEGI => "Custom: " + Texture.ToPEGIstring();

        public override bool PEGI_inList(IList list, int ind, ref int edited) => "Source".select(50, ref id, Data.imgMetas);

        public override bool Inspect()
        {
            "Source".select(50, ref id, Data.imgMetas);
            var tmp = Texture;
            if ("Texture".edit(ref tmp).nl() && tmp)
                id = tmp.GetImgData();

            return false;
        }

#endif
        #endregion
        
        #region Encode & Decode
        public override StdEncoder Encode() => new StdEncoder()
            .Add_GUID("t", Texture)
            .Add_IfTrue("show", showOnGui);

        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "t": var tmp = Texture; data.TryReplaceAssetByGuid(ref tmp); if (tmp) id = Texture.GetImgData(); break;
                case "show": showOnGui = data.ToBool(); break;
                default: return false;
            }
            return true;
        }
        #endregion

    }

    public class BigRtPair : TextureBuffer
    {

        public override string NameForDisplayPEGI => "BIG RT pair";

        public override void AfterRender()
        {
            TexMGMT.UpdateBufferTwo();
            TexMGMT.bigRtVersion++;
        }

        public override int Version
        {
            get
            {
                return TexMGMT.bigRtVersion;
            }

            set
            {
                TexMGMT.bigRtVersion = value;
            }
        }

        public override Texture TextureNext => GetRenderTexture;

        protected override RenderTexture GetRenderTexture
        {
            get
            {
                PainterDataAndConfig.DESTINATION_BUFFER.GlobalValue = TexMGMT.bigRtPair[1];
                return TexMGMT.bigRtPair[0];
            }
        }

        public override Texture GetTextureDisplay => TexMGMT.bigRtPair[0];

        public override bool CanBeTarget => true;


    }

    public class OnDemandRT : TextureBuffer, IPEGI
    {
        private RenderTexture _rt;
        protected int width = 512;
        protected string name;
        public bool linear;
        protected RenderTextureReadWrite colorMode;

        public override string NameForDisplayPEGI => "RT " + name;

        public override bool CanBeTarget => true;

        public override Texture TextureNext
        {
            get
            {
                _rt = GetRenderTexture;
                return _rt;
            }
        }
        public override bool CanBeAssignedToPainter => true;

        protected override RenderTexture GetRenderTexture {
            get {
                if (!_rt) _rt = new RenderTexture(width, width, 0, RenderTextureFormat.ARGBFloat, colorMode);
                return _rt;
            }
        }

        #region Encode & Decode
        public override StdEncoder Encode() =>this.EncodeUnrecognized()
            .Add("w", width)
            .Add("c", (int)colorMode)
            .Add_IfTrue("show", showOnGui);

        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "w": width = data.ToInt(); break;
                case "c": colorMode = (RenderTextureReadWrite)data.ToInt(); break;
                case "show": showOnGui = data.ToBool(); break;
                default: return false;
            }
            return true;
        }
        #endregion

        #region Inspector
        #if PEGI
        public override bool Inspect()
        {
            "name".edit(50, ref name).nl();
            "width".edit(ref width).nl();
            "mode".editEnum(ref colorMode).nl();

            return false;
        }
        #endif
        #endregion
    }

    public class OnDemandRtPair : OnDemandRT
    {
        private RenderTexture[] _rts; // = new RenderTexture[2];

        public override string NameForDisplayPEGI => "RT PAIR " + name;

        public override bool CanBeTarget => true;

        public override Texture GetTextureDisplay => _rts[0];

        protected override RenderTexture GetRenderTexture
        {
            get
            {
                if (_rts == null)
                {
                    _rts = new RenderTexture[2];
                    _rts[0] = new RenderTexture(width, width, 0, RenderTextureFormat.ARGBFloat, colorMode);
                    _rts[1] = new RenderTexture(width, width, 0, RenderTextureFormat.ARGBFloat, colorMode);
                }

                PainterDataAndConfig.DESTINATION_BUFFER.GlobalValue = _rts[1];
                return _rts[0];
            }
        }

        public override Texture TextureNext => GetRenderTexture;

        public override void AfterRender()
        {
            var tmp = _rts[0];
            _rts[0] = _rts[1];
            _rts[1] = tmp;
        }

        public override bool CanBeAssignedToPainter => true;
        
    }

    public class WebCamTextureBuffer : TextureBuffer {
        
        public override bool IsReady
        {
            get
            {
                var cam = TexMGMT ? Data.webCamTexture : null;

                return Mgmt != null
                    && (cam || !cam.isPlaying || cam.didUpdateThisFrame);
            }
        }

        public override Texture GetTextureDisplay => Data.webCamTexture;

        public override Texture TextureNext => Data.GetWebCamTexture();

        public override bool CanBeAssignedToPainter => true;

        public override void Stop() => Data.webCamTexture.Stop();

        #region Inspector

        public override string NameForDisplayPEGI => "Web Cam Tex";

        #if PEGI
        public override bool PEGI_inList(IList list, int ind, ref int edited)
        {

            "WebCam".write(60);

            var cam = TexMGMT ? Data.webCamTexture : null;

            if (cam && cam.isPlaying && icon.Pause.Click("Stop Camera"))
                Data.StopCamera();

            if ((!cam || !cam.isPlaying) && WebCamTexture.devices.Length > 0 && icon.Play.Click("Start Camera"))
            {
                Data.webCamTexture = new WebCamTexture(WebCamTexture.devices[0].name, 512, 512, 30);
                Data.webCamTexture.Play();
            }
            return false;
        }
#endif
        #endregion

        #region Encode & Decode
        public override StdEncoder Encode() => new StdEncoder().Add_IfTrue("show", showOnGui);

        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "show": showOnGui = data.ToBool(); break;
                default: return false;
            }
            return true;
        }
        #endregion
    }

    public class SectionTarget : TextureBuffer
    {
        int targetIndex;

        private TextureBuffer Target => MultiBufferProcessing.sections.TryGet(targetIndex)?.TargetRenderTexture; 

        public override void AfterRender() => Target?.AfterRender();
        
        public override Texture TextureNext => Target?.TextureNext;
        
        public override Texture GetTextureDisplay => Target?.GetTextureDisplay;
        
        public override bool CanBeTarget
        {
            get
            {
                var t = Target;
                return t?.CanBeTarget ?? false;
            }
        }

        public override RenderTexture GetTargetTextureNext => Target?.GetTargetTextureNext;

        #region Inspector
#if PEGI
        public override string NameForDisplayPEGI => "Other: " + MultiBufferProcessing.sections.TryGet(targetIndex).ToPEGIstring();
        public override bool Inspect() => "Source".select(50, ref targetIndex, MultiBufferProcessing.sections).nl();
#endif
        #endregion
        
        #region Encode & Decode
        public override StdEncoder Encode() => new StdEncoder().Add("t", targetIndex).Add_IfTrue("show", showOnGui);

        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "t": targetIndex = data.ToInt(); break;
                case "show": showOnGui = data.ToBool(); break;
                default: return false;
            }
            return true;
        }
        #endregion
    }

    public class DownScalar : TextureBuffer, IPEGI
    {
        private string _name;
        private int _width = 64;
        private int _lastReadVersion = -1;
        private bool _linear;
        private Shader _shader;
        private Texture2D _buffer;

        public override string NameForDisplayPEGI => (_name.IsNullOrEmpty() ? "Scalar" : _name);

        public override bool CanBeTarget => true;

        public override bool CanBeAssignedToPainter => true;

        ~DownScalar()
        {
            _buffer.DestroyWhatever();
        }

        private void InitIfNull()
        {
            if (!_buffer)
            {
                _buffer = new Texture2D(_width, _width, TextureFormat.ARGB32, false, _linear)
#if UNITY_EDITOR
                { alphaIsTransparency = true }
#endif
                ;
            }
        }

        public override bool BlitMethod(TextureBuffer sourceBuffer, Material material, Shader shader)
        {
            var other = sourceBuffer;

            if (other == null || _lastReadVersion == other.Version) return false;
            
            var tex = other.TextureNext;

            if (!tex) return false;
            
            InitIfNull();

            _buffer.CopyFrom(TexMGMT.Downscale_ToBuffer(tex, _width, _width, material, shader));
            _lastReadVersion = other.Version;

            var px = _buffer.GetPixels();

            _buffer.SetPixels(px);

            _buffer.Apply();

            Version++;

            return true;

        }

        public override Texture TextureNext => _buffer;

        #region Inspector
        #if PEGI
        public override bool Inspect() {

            var changed = base.Inspect();

            "Name".edit(ref _name).nl();

            if ("Result Width".edit(ref _width).nl() || "Not Color".toggle(ref _linear).nl()) {
                _width = Mathf.Clamp(_width, 8, 512);
                _width = Mathf.ClosestPowerOfTwo(_width);

                _buffer.DestroyWhatever();
                InitIfNull();
            }

            return changed;
        }
        #endif
        #endregion

        #region Encode & Decode
        public override StdEncoder Encode() =>this.EncodeUnrecognized()
            .Add_IfNotEmpty("n", _name)
            .Add_IfNotEpsilon("w", _width)
            .Add_IfTrue("l", _linear)
            .Add_IfTrue("show", showOnGui)
            .Add_GUID("s", _shader);

        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "n": _name = data; break;
                case "w": _width = data.ToInt(); break;
                case "l": _linear = data.ToBool(); break;
                case "show": showOnGui = data.ToBool(); break;
                case "s": data.TryReplaceAssetByGuid(ref _shader); break;
                default: return false;
            }

            return true;
        }
        #endregion
    }

    #endregion

    #region Section
    
    public class RenderSection : PainterStuffKeepUnrecognized_STD , IPEGI, IGotDisplayName, IPEGI_ListInspect
    {
        private enum BlitTrigger { Manual, PerFrame, WhenOtherSectionUpdated, WhenSourceReady, Delay, DelayAndUpdated }

        private Material _material;
        private Shader _shader;
        public Material previewMaterial;

        private int _targetBufferIndex = -1;
        private int _sourceBufferIndex = -1;
        private float _delayTime = 0.01f;
        public TextureBuffer TargetRenderTexture => MultiBufferProcessing.buffers.TryGet(_targetBufferIndex);
        private TextureBuffer SourceBuffer => MultiBufferProcessing.buffers.TryGet(_sourceBufferIndex);

        private int Version => TargetRenderTexture?.Version ?? 0;

        private BlitTrigger _trigger = BlitTrigger.Manual;
        private bool _enabled = true;
        private RenderSection _triggerSection;

        #region Encode & Decode

        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add_Reference("m", _material)
            .Add("trg", _targetBufferIndex)
            .Add_Bool("e", _enabled);

        public override bool Decode(string tg, string data)
        {
            switch (tg) {
                case "m": data.Decode_Reference(ref _material); break;
                case "trg": _targetBufferIndex = data.ToInt(); break;
                case "e": _enabled = data.ToBool(); break;
                default: return false;
            }
            return true;
        }

        #endregion

        private int _dependentVersion;
        private float _timer;
        
        public bool Blit() => TargetRenderTexture?.BlitMethod(SourceBuffer, _material, _shader) ?? false;

        public void Update()
        {
            if (!_enabled) return;
            
            switch (_trigger)
            {
                case BlitTrigger.PerFrame:
                    Blit(); break;
                case BlitTrigger.WhenOtherSectionUpdated:
                    if (_triggerSection != null && _triggerSection.Version > _dependentVersion)
                    {
                        _dependentVersion = _triggerSection.Version;
                        Blit();
                    }
                    break;

                case BlitTrigger.DelayAndUpdated:
                    _timer -= Time.deltaTime;
                    if (_timer < 0 && SourceBuffer != null && SourceBuffer.Version > _dependentVersion)
                    {
                        _dependentVersion = SourceBuffer.Version;
                        _timer = _delayTime;
                        Blit();
                    }
                    break;

                case BlitTrigger.WhenSourceReady:
                    if (SourceBuffer != null && SourceBuffer.IsReady)
                    {
                        _dependentVersion = SourceBuffer.Version;
                        Blit();
                    }
                    break;

                case BlitTrigger.Delay:
                    _timer -= Time.deltaTime;
                    if (_timer < 0)
                    {
                        _timer = _delayTime;
                        Blit();
                    }
                    break;


            }
        }

        #region Inspector
#if PEGI

        public string NameForDisplayPEGI => SourceBuffer.ToPEGIstring() + "-> " + TargetRenderTexture.ToPEGIstring();//(material ? material.name : "No Material");


        public bool PEGI_inList(IList list, int ind, ref int edited)
        {
            this.ToPEGIstring().write();

            if (_trigger != BlitTrigger.Manual)
                pegi.toggle(ref _enabled);

            if (TargetRenderTexture != null && _trigger == BlitTrigger.Manual && icon.Refresh.Click())
                Blit();

            if (icon.StateMachine.Click())
                edited = ind;

            return false;
        }

        public override bool Inspect()
        {
            var changed = false;

            "Update Type".editEnum(70, ref _trigger).changes(ref changed);

            if ((_trigger != BlitTrigger.PerFrame || !_enabled) && "Blit".Click(ref changed))
                Blit();

            if (_trigger != BlitTrigger.Manual)
                pegi.toggle(ref _enabled, icon.Pause, icon.Play).changes(ref changed);

            pegi.nl();

            "From ".select(ref _sourceBufferIndex, MultiBufferProcessing.buffers).nl(ref changed);

            "To ".select(ref _targetBufferIndex, MultiBufferProcessing.buffers, e => e.CanBeTarget).nl(ref changed);

            if (_material || !_shader)
                "Blit Material".edit(100, ref _material).nl(ref changed);

            if (!_material)
                "Shader".edit(60, ref _shader).nl(ref changed);

            if (TargetRenderTexture != null)
                "Preview Material".edit(100, ref previewMaterial).changes(ref changed);

            pegi.nl();

            switch (_trigger)
            {
                case BlitTrigger.WhenOtherSectionUpdated:
                    "After: ".select(50, ref _triggerSection, MultiBufferProcessing.sections);
                    break;
                case BlitTrigger.DelayAndUpdated:
                case BlitTrigger.Delay:
                    "Delay:".edit(ref _delayTime).nl(); break;
            }

            pegi.nl();

            return changed;
        }

      
#endif
        #endregion
    }

    #endregion
    
}