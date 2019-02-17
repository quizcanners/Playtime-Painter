using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using QuizCannersUtilities;
using PlayerAndEditorGUI;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Playtime_Painter {

    #region Manager
    [TaggedType(tag)]
    public class MultiBufferProcessing : PainterManagerPluginBase, ISTD, IPainterManagerPlugin_ComponentPEGI
    {
        const string tag = "MltBffr";
        public override string ClassTag => tag;

        public static MultiBufferProcessing inst;

        public List<RenderSection> sections = new List<RenderSection>();
        public List<TextureBuffer> buffers = new List<TextureBuffer>();

        #region Encode & Decode
        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add("s", buffers)
            .Add("sc", sections);

        public override bool Decode(string tg, string data) {
            switch (tg) {
                case "s": data.Decode_List(out buffers); break;
                case "sc": data.Decode_List(out sections); break;
                default: return false;
            }
            return true;
        }
        #endregion

        bool pauseBuffers = false;

        public override void Update() {
            if (!pauseBuffers)
                sections.ForEach(s => s.Update());
        }

        #region Inspector
        #if PEGI
        public override string NameForDisplayPEGI => "Buffer Blitting";

        [SerializeField] int GUITextureSize = 128;
        int texIndex = 0;
        
        void OnGUI()
        {

            if (pauseBuffers)
                return;

            texIndex = 0;

            const int textGap = 30;
            int fullSize = textGap + GUITextureSize;

            int inColumn = Screen.height / fullSize;

            foreach (var b in buffers)
                if (b != null && b.showOnGUI)
                {

                    var sct = sections.Where(s => s.TargetRenderTexture != null && s.TargetRenderTexture == b);
                    RenderSection section = sct.Count() > 0 ? sct.First() : null;

                    var tex = b.GetTextureDisplay;

                    Rect pos = new Rect((texIndex / inColumn) * GUITextureSize, (texIndex % inColumn) * fullSize - textGap, GUITextureSize, GUITextureSize);
                    GUI.Label(pos, b.ToPEGIstring());
                    pos.y += textGap;

                    if (tex)
                    {
                        if (section != null && section.previewMaterial)
                            Graphics.DrawTexture(pos, tex, section.previewMaterial);
                        else
                            GUI.DrawTexture(pos, tex);
                    }

                    texIndex++;

                    pos.width = 25;
                    pos.height = 25;

                    if (section != null && GUI.Button(pos, icon.Refresh.GetIcon()))
                        section.Blit();
                }

        }

        public override bool Inspect()
        {
            bool changed = false;

            if ((pauseBuffers ? icon.Play : icon.Pause).Click("Stop/Start ALL"))
            {
                pauseBuffers = !pauseBuffers;
                if (pauseBuffers)
                    foreach (var b in buffers)
                        b.Stop();
            }


            if (editedSection == -1 && editedBuffer == -1)
                "Size: ".edit(40, ref GUITextureSize, 128, 512).nl();

            if (editedBuffer == -1)
                "Sections".edit_List(ref sections, ref editedSection);
            else
                editedSection = -1;

            if (editedSection == -1)
                "Buffers".edit_List(ref buffers, ref editedBuffer);

            return changed;
        }

        [SerializeField] int editedBuffer = -1;
        [SerializeField] int editedSection = -1;

        public bool ComponentInspector()
        {
            bool changed = false;

            if (buffers.Count > 0)
            {
                if ((pauseBuffers ? icon.Play : icon.Pause).Click("Stop/Start ALL"))
                    pauseBuffers = !pauseBuffers;

                int cur = -1;
                if ("Buffers".select(60, ref cur, buffers, (x) => x.CanBeAssignedToPainter).nl(ref changed))
                    InspectedPainter.SetTextureOnMaterial(buffers.TryGet(cur).GetTextureDisplay);

            }

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
        , typeof(OnDemandRTPair)
        , typeof(WebCamTextureBuffer)
        , typeof(CustomImageData)
        , typeof(SectionTarget)
        , typeof(BigRTpair)
        , typeof(Downscaler)
        )]
    public class TextureBuffer : AbstractKeepUnrecognized_STD, IPEGI_ListInspect, IGotDisplayName {

        int _version = 0;

        public virtual int Version { get { return _version; } set { _version = value; } }

        public bool showOnGUI = false;

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

            if (trg)
            {
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


            return false;
        }

        public virtual void Stop() { }

        #region Inspector
        #if PEGI
        public override bool Inspect()
        {
            bool changed = false;

            "Show On GUI".toggle(ref showOnGUI).nl();

            return changed;
        }

        public virtual bool PEGI_inList(IList list, int ind, ref int edited)
        {
            bool changed = NameForDisplayPEGI.toggle(ref showOnGUI);

            var asP = this as IPEGI;
            if (asP != null && icon.Enter.Click())
                edited = ind;

            return changed;
        }

        #endif
        #endregion

        #region Encode & Decode
        public override StdEncoder Encode() =>this.EncodeUnrecognized().Add_IfTrue("show", showOnGUI);

        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "show": showOnGUI = data.ToBool(); break;
                default: return false;
            }
            return true;
        }
        #endregion
    }

    public class CustomImageData : TextureBuffer  , IPEGI_ListInspect, IPEGI
    {

        ImageMeta id;
        Texture Texture => id.CurrentTexture();

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
            Texture tmp = Texture;
            if ("Texture".edit(ref tmp).nl() && tmp)
                id = tmp.GetImgData();

            return false;
        }

#endif
        #endregion
        
        #region Encode & Decode
        public override StdEncoder Encode() => new StdEncoder()
            .Add_GUID("t", Texture)
            .Add_IfTrue("show", showOnGUI);

        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "t": Texture tmp = Texture; data.ToAssetByGuid(ref tmp); if (tmp) id = Texture.GetImgData(); break;
                case "show": showOnGUI = data.ToBool(); break;
                default: return false;
            }
            return true;
        }
        #endregion

    }

    public class BigRTpair : TextureBuffer
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
        RenderTexture rt;
        public int width = 512;
        public string name;
        public bool linear;
        public RenderTextureReadWrite colorMode;

        public override string NameForDisplayPEGI => "RT " + name;

        public override bool CanBeTarget => true;

        public override Texture TextureNext
        {
            get
            {
                rt = GetRenderTexture;
                return rt;
            }
        }
        public override bool CanBeAssignedToPainter => true;

        protected override RenderTexture GetRenderTexture {
            get {
                if (!rt) rt = new RenderTexture(width, width, 0, RenderTextureFormat.ARGBFloat, colorMode);
                return rt;
            }
        }

        #region Encode & Decode
        public override StdEncoder Encode() =>this.EncodeUnrecognized()
            .Add("w", width)
            .Add("c", (int)colorMode)
            .Add_IfTrue("show", showOnGUI);

        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "w": width = data.ToInt(); break;
                case "c": colorMode = (RenderTextureReadWrite)data.ToInt(); break;
                case "show": showOnGUI = data.ToBool(); break;
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

    public class OnDemandRTPair : OnDemandRT
    {
        RenderTexture[] rts; // = new RenderTexture[2];

        public override string NameForDisplayPEGI => "RT PAIR " + name;

        public override bool CanBeTarget => true;

        public override Texture GetTextureDisplay => rts[0];

        protected override RenderTexture GetRenderTexture
        {
            get
            {
                if (rts == null)
                {
                    rts = new RenderTexture[2];
                    rts[0] = new RenderTexture(width, width, 0, RenderTextureFormat.ARGBFloat, colorMode);
                    rts[1] = new RenderTexture(width, width, 0, RenderTextureFormat.ARGBFloat, colorMode);
                }

                PainterDataAndConfig.DESTINATION_BUFFER.GlobalValue = rts[1];
                return rts[0];
            }
        }

        public override Texture TextureNext => GetRenderTexture;

        public override void AfterRender()
        {
            var tmp = rts[0];
            rts[0] = rts[1];
            rts[1] = tmp;
        }

        public override bool CanBeAssignedToPainter => true;
        
    }

    public class WebCamTextureBuffer : TextureBuffer, IPEGI_ListInspect {
        
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
        public override StdEncoder Encode() => new StdEncoder().Add_IfTrue("show", showOnGUI);

        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "show": showOnGUI = data.ToBool(); break;
                default: return false;
            }
            return true;
        }
        #endregion
    }

    public class SectionTarget : TextureBuffer
    {
        int targetIndex;
        
        TextureBuffer Target => Mgmt.sections.TryGet(targetIndex)?.TargetRenderTexture; 

        public override void AfterRender() => Target?.AfterRender();
        
        public override Texture TextureNext => Target?.TextureNext;
        
        public override Texture GetTextureDisplay => Target?.GetTextureDisplay;
        
        public override bool CanBeTarget
        {
            get
            {
                var t = Target;
                return (t != null) ?
                    t.CanBeTarget : false;
            }
        }

        public override RenderTexture GetTargetTextureNext => Target?.GetTargetTextureNext;

        #region Inspector
#if PEGI
        public override string NameForDisplayPEGI => "Other: " + Mgmt.sections.TryGet(targetIndex).ToPEGIstring();
        public override bool Inspect() => "Source".select(50, ref targetIndex, Mgmt.sections).nl();
#endif
        #endregion
        
        #region Encode & Decode
        public override StdEncoder Encode() => new StdEncoder().Add("t", targetIndex).Add_IfTrue("show", showOnGUI);

        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "t": targetIndex = data.ToInt(); break;
                case "show": showOnGUI = data.ToBool(); break;
                default: return false;
            }
            return true;
        }
        #endregion
    }

    public class Downscaler : TextureBuffer, IPEGI
    {
        string name;
        int width = 64;
        int lastReadVersion = -1;
        bool linear;
        Shader shader;
        Texture2D buffer;

        public override string NameForDisplayPEGI => (name.IsNullOrEmpty() ? "Scaler" : name);

        public override bool CanBeTarget => true;

        public override bool CanBeAssignedToPainter => true;

        ~Downscaler()
        {
            buffer.DestroyWhatever();
        }

        void InitIfNull()
        {
            if (!buffer)
            {
                buffer = new Texture2D(width, width, TextureFormat.ARGB32, false, linear)
#if UNITY_EDITOR
                { alphaIsTransparency = true }
#endif
                ;
            }
        }

        public override bool BlitMethod(TextureBuffer sourceBuffer, Material material, Shader shader)
        {
            var other = sourceBuffer;

            if (other != null && lastReadVersion != other.Version)
            {
                var tex = other.TextureNext;
                if (tex) {

                    InitIfNull();

                    buffer.CopyFrom(TexMGMT.Downscale_ToBuffer(tex, width, width, material, shader));
                    lastReadVersion = other.Version;

                    var px = buffer.GetPixels();

                    buffer.SetPixels(px);

                    buffer.Apply();

                    Version++;

                    return true;
                }
            }

            return false;
        }

        public override Texture TextureNext => buffer;

        #region Inspector
        #if PEGI
        public override bool Inspect() {

            var changed = base.Inspect();

            "Name".edit(ref name).nl();

            if ("Result Width".edit(ref width).nl() || "Not Color".toggle(ref linear).nl()) {
                width = Mathf.Clamp(width, 8, 512);
                width = Mathf.ClosestPowerOfTwo(width);

                buffer.DestroyWhatever();
                InitIfNull();
            }

            return changed;
        }
        #endif
        #endregion

        #region Encode & Decode
        public override StdEncoder Encode() =>this.EncodeUnrecognized()
            .Add_IfNotEmpty("n", name)
            .Add_IfNotEpsilon("w", width)
            .Add_IfTrue("l", linear)
            .Add_IfTrue("show", showOnGUI)
            .Add_GUID("s", shader);

        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "n": name = data; break;
                case "w": width = data.ToInt(); break;
                case "l": linear = data.ToBool(); break;
                case "show": showOnGUI = data.ToBool(); break;
                case "s": data.ToAssetByGuid(ref shader); break;
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
        enum BlitTrigger { Manual, PerFrame, WhenOtherSectionUpdated, WhenSourceReady, Delay, DelayAndUpdated }

        [SerializeField] Material material = null;
        [SerializeField] Shader shader = null;
        [SerializeField] public Material previewMaterial;

        [SerializeField] int targetBufferIndex = -1;
        [SerializeField] int sourceBufferIndex = -1;
        [SerializeField] float delayTime = 0.01f;
        public TextureBuffer TargetRenderTexture => Mgmt.buffers.TryGet(targetBufferIndex);
        public TextureBuffer SourceBuffer => Mgmt.buffers.TryGet(sourceBufferIndex);
        MultiBufferProcessing Mgmt { get { return MultiBufferProcessing.inst; } }

        int Version { get { return TargetRenderTexture != null ? TargetRenderTexture.Version : 0; } }

        [SerializeField] BlitTrigger trigger = BlitTrigger.Manual;
        [SerializeField] bool enabled = true;
        [SerializeField] RenderSection triggerSection = null;

        #region Encode & Decode

        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add_Reference("m", material)
            .Add("trg", targetBufferIndex)
            .Add_Bool("e", enabled);

        public override bool Decode(string tg, string data)
        {
            switch (tg) {
                case "m": data.Decode_Reference(ref material); break;
                case "trg": targetBufferIndex = data.ToInt(); break;
                case "e": enabled = data.ToBool(); break;
                default: return false;
            }
            return true;
        }

        #endregion

        int dependentVersion = 0;
        float timer = 0;
        
        public bool Blit() => TargetRenderTexture != null ? TargetRenderTexture.BlitMethod(SourceBuffer, material, shader) : false;

        public void Update()
        {

            if (enabled)
            {
                switch (trigger)
                {
                    case BlitTrigger.PerFrame:
                        Blit(); break;
                    case BlitTrigger.WhenOtherSectionUpdated:
                        if (triggerSection != null && triggerSection.Version > dependentVersion)
                        {
                            dependentVersion = triggerSection.Version;
                            Blit();
                        }
                        break;

                    case BlitTrigger.DelayAndUpdated:
                        timer -= Time.deltaTime;
                        if (timer < 0 && SourceBuffer != null && SourceBuffer.Version > dependentVersion)
                        {
                            dependentVersion = SourceBuffer.Version;
                            timer = delayTime;
                            Blit();
                        }
                        break;

                    case BlitTrigger.WhenSourceReady:
                        if (SourceBuffer != null && SourceBuffer.IsReady)
                        {
                            dependentVersion = SourceBuffer.Version;
                            Blit();
                        }
                        break;

                    case BlitTrigger.Delay:
                        timer -= Time.deltaTime;
                        if (timer < 0)
                        {
                            timer = delayTime;
                            Blit();
                        }
                        break;


                }
            }
        }

        #region Inspector
#if PEGI

        public string NameForDisplayPEGI => SourceBuffer.ToPEGIstring() + "-> " + TargetRenderTexture.ToPEGIstring();//(material ? material.name : "No Material");


        public bool PEGI_inList(IList list, int ind, ref int edited)
        {
            this.ToPEGIstring().write();

            if (trigger != BlitTrigger.Manual)
                pegi.toggle(ref enabled);

            if (TargetRenderTexture != null && trigger == BlitTrigger.Manual && icon.Refresh.Click())
                Blit();

            if (icon.StateMachine.Click())
                edited = ind;

            return false;
        }

        public override bool Inspect()
        {
            bool changed = false;

            "Update Type".editEnum(70, ref trigger);

            if ((trigger != BlitTrigger.PerFrame || !enabled) && "Blit".Click())
                Blit();

            if (trigger != BlitTrigger.Manual)
                pegi.toggle(ref enabled, icon.Pause, icon.Play);

            pegi.nl();

            "From ".select(ref sourceBufferIndex, Mgmt.buffers).nl();

            "To ".select(ref targetBufferIndex, Mgmt.buffers, e => e.CanBeTarget).nl();

            if (material || !shader)
                "Blit Material".edit(100, ref material).nl();

            if (!material)
                "Shader".edit(60, ref shader).nl();

            if (TargetRenderTexture != null)
                "Preview Material".edit(100, ref previewMaterial);

            pegi.nl();

            switch (trigger)
            {
                case BlitTrigger.WhenOtherSectionUpdated:
                    "After: ".select(50, ref triggerSection, Mgmt.sections);
                    break;
                case BlitTrigger.DelayAndUpdated:
                case BlitTrigger.Delay:
                    "Delay:".edit(ref delayTime).nl(); break;
            }

            pegi.nl();

            return changed;
        }

      
#endif
        #endregion
    }

    #endregion
    
}