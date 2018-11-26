using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using SharedTools_Stuff;
using PlayerAndEditorGUI;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Playtime_Painter
{

    #region Manager
    [TaggedType(tag)]
    [ExecuteInEditMode]
    public class MultiBufferProcessing : PainterManagerPluginBase, ISTD
    {
        const string tag = "MltBffr";
        public override string ClassTag => tag;

        public static MultiBufferProcessing inst;

        [SerializeField] string std_data;

        public List<RenderSection> sections = new List<RenderSection>();
        public List<TextureBuffer> buffers = new List<TextureBuffer>();

        bool pauseBuffers = false;

        private void ManualUpdate()
        {
            if (!pauseBuffers)
                sections.ForEach(s => s.Update());
        }

        void Update()
        {
            if (Application.isPlaying)
                ManualUpdate();
        }

        #region Inspector
#if PEGI
        public override string NameForPEGIdisplay => "Buffer Blitting";

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

                    var tex = b.GetTextureDisplay();

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

        public override bool ConfigTab_PEGI()
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

#endif
        #endregion

        public override void OnDisable()
        {
            std_data = Encode().ToString();

            base.OnDisable();
#if UNITY_EDITOR
            EditorApplication.update -= ManualUpdate;
#endif
        }

        public override void OnEnable()
        {
            inst = this;
            Decode(std_data); //.DecodeTagsFor(this);
#if UNITY_EDITOR
            EditorApplication.update -= ManualUpdate;
            if (!UnityHelperFunctions.ApplicationIsAboutToEnterPlayMode())
                EditorApplication.update += ManualUpdate;
#endif

#if PEGI
            PlugIn_PainterComponent = Component_PEGI;
#endif

        }

#if PEGI
        public bool Component_PEGI()
        {
            bool changed = false;

            if (buffers.Count > 0)
            {
                if ((pauseBuffers ? icon.Play : icon.Pause).Click("Stop/Start ALL"))
                    pauseBuffers = !pauseBuffers;

                int cur = -1;
                if ("Buffers".select(60, ref cur, buffers, (x) => x.CanBeAssignedToPainter).nl(ref changed))
                    InspectedPainter.SetTextureOnMaterial(buffers.TryGet(cur).GetTextureDisplay());
                
            }

            return changed;
        }
#endif
        public override StdEncoder Encode() =>this.EncodeUnrecognized()
            .Add("s", buffers);

        public override bool Decode(string tag, string data)
        {
            switch (tag)
            {
                case "s": data.Decode_List(out buffers); break;
                default: return false;
            }
            return true;
        }
        
    }

    #endregion

    #region TextureBuffers

    [DerrivedList(typeof(OnDemandRT)
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

        protected static MultiBufferProcessing Mgmt { get { return MultiBufferProcessing.inst; } }

        protected static PainterCamera TexMGMT { get { return PainterCamera.Inst; } }

        protected static PainterDataAndConfig Data { get { return PainterCamera.Data; } }

        public virtual string NameForPEGIdisplay => "Override This";

        public virtual Texture GetTextureNext() => null;

        public virtual Texture GetTextureDisplay() => GetTextureNext();

        public virtual RenderTexture GetTargetTextureNext()
        {
            var rt = RenderTexture();
            return rt;
        }

        protected virtual RenderTexture RenderTexture() => null;

        public virtual void AfterRender() { }

        public virtual bool CanBeTarget => false;

        public virtual bool CanBeAssignedToPainter => false;

        public virtual bool IsReady => true;

        public virtual bool BlitMethod(TextureBuffer sourceBuffer, Material material, Shader shader)
        {
            var trg = GetTargetTextureNext();
            var src = sourceBuffer?.GetTextureNext();

            if (trg)
            {
                if (src)
                    Shader.SetGlobalTexture(PainterDataAndConfig.SOURCE_TEXTURE, src);

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
            bool changed = NameForPEGIdisplay.toggle(ref showOnGUI);

            var asP = this as IPEGI;
            if (asP != null && icon.Enter.Click())
                edited = ind;

            return changed;
        }

#endif
        #endregion

        #region Encode & Decode
        public override StdEncoder Encode() =>this.EncodeUnrecognized().Add_IfTrue("show", showOnGUI);

        public override bool Decode(string tag, string data)
        {
            switch (tag)
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

        ImageData id;
        Texture Texture => id.CurrentTexture();

        #region Encode & Decode
        public override StdEncoder Encode() => new StdEncoder()
            .Add_GUID("t", Texture)
            .Add_IfTrue("show", showOnGUI);

        public override bool Decode(string tag, string data)
        {
            switch (tag)
            {
                case "t": Texture tmp = Texture; data.ToAssetByGUID(ref tmp); if (tmp != null) id = Texture.GetImgData(); break;
                case "show": showOnGUI = data.ToBool(); break;
                default: return false;
            }
            return true;
        }
        #endregion

        public override bool CanBeTarget => Texture && Texture.GetType() == typeof(RenderTexture);

        public override Texture GetTextureNext() => Texture;

        protected override RenderTexture RenderTexture() => Texture ? (RenderTexture)Texture : null;

        #region Inspector
#if PEGI
        public override string NameForPEGIdisplay => "Custom: " + Texture.ToPEGIstring();

        public override bool PEGI_inList(IList list, int ind, ref int edited) => "Source".select(50, ref id, Data.imgDatas);

        public override bool Inspect()
        {
            "Source".select(50, ref id, Data.imgDatas);
            Texture tmp = Texture;
            if ("Texture".edit(ref tmp).nl() && (tmp != null))
                id = tmp.GetImgData();

            return false;
        }

#endif
        #endregion
    }

    public class BigRTpair : TextureBuffer
    {

        public override string NameForPEGIdisplay => "BIG RT pair";

        public override void AfterRender()
        {
            TexMGMT.UpdateBufferTwo();
            TexMGMT.bigRTversion++;
        }

        public override int Version
        {
            get
            {
                return TexMGMT.bigRTversion;
            }

            set
            {
                TexMGMT.bigRTversion = value;
            }
        }

        public override Texture GetTextureNext() => RenderTexture();

        protected override RenderTexture RenderTexture()
        {
            Shader.SetGlobalTexture(PainterDataAndConfig.DESTINATION_BUFFER, TexMGMT.BigRT_pair[1]);
            return TexMGMT.BigRT_pair[0];
        }

        public override Texture GetTextureDisplay() => TexMGMT.BigRT_pair[0];

        public override bool CanBeTarget => true;


    }

    public class OnDemandRT : TextureBuffer, IPEGI
    {
        RenderTexture rt;
        public int width = 512;
        public string name;
        public bool linear;
        public RenderTextureReadWrite colorMode;

        public override string NameForPEGIdisplay => "RT " + name;

        public override bool CanBeTarget => true;

        public override Texture GetTextureNext() => RenderTexture();

        public override bool CanBeAssignedToPainter => true;

        protected override RenderTexture RenderTexture()
        {
            if (rt == null) rt = new RenderTexture(width, width, 0, RenderTextureFormat.ARGBFloat, colorMode);
            return rt;
        }

        public override StdEncoder Encode() =>this.EncodeUnrecognized()
            .Add("w", width)
            .Add("c", (int)colorMode)
            .Add_IfTrue("show", showOnGUI);

        public override bool Decode(string tag, string data)
        {
            switch (tag)
            {
                case "w": width = data.ToInt(); break;
                case "c": colorMode = (RenderTextureReadWrite)data.ToInt(); break;
                case "show": showOnGUI = data.ToBool(); break;
                default: return false;
            }
            return true;
        }

#if PEGI
        public override bool Inspect()
        {
            "name".edit(50, ref name).nl();
            "width".edit(ref width).nl();
            "mode".editEnum(ref colorMode).nl();

            return false;
        }
#endif

    }

    public class OnDemandRTPair : OnDemandRT
    {
        RenderTexture[] rts; // = new RenderTexture[2];

        public override string NameForPEGIdisplay => "RT PAIR " + name;

        public override bool CanBeTarget => true;

        public override Texture GetTextureDisplay() => rts[0];

        protected override RenderTexture RenderTexture()
        {
            if (rts == null)
            {
                rts = new RenderTexture[2];
                rts[0] = new RenderTexture(width, width, 0, RenderTextureFormat.ARGBFloat, colorMode);
                rts[1] = new RenderTexture(width, width, 0, RenderTextureFormat.ARGBFloat, colorMode);
            }

            Shader.SetGlobalTexture(PainterDataAndConfig.DESTINATION_BUFFER, rts[1]);
            return rts[0];
        }

        public override Texture GetTextureNext() => RenderTexture();

        public override void AfterRender()
        {
            var tmp = rts[0];
            rts[0] = rts[1];
            rts[1] = tmp;
        }

        public override bool CanBeAssignedToPainter => true;
        
    }

    public class WebCamTextureBuffer : TextureBuffer, IPEGI_ListInspect {

        public override StdEncoder Encode() => new StdEncoder().Add_IfTrue("show", showOnGUI);

        public override bool Decode(string tag, string data)
        {
            switch (tag)
            {
                case "show": showOnGUI = data.ToBool(); break;
                default: return false;
            }
            return true;
        }

        public override bool IsReady
        {
            get
            {
                var cam = TexMGMT ? Data.webCamTexture : null;

                return Mgmt!=null
                    && (cam || !cam.isPlaying || cam.didUpdateThisFrame);
            }
        }

        public override string NameForPEGIdisplay => "Web Cam Tex";

        public override Texture GetTextureDisplay() => Data.webCamTexture;

        public override Texture GetTextureNext() => Data.GetWebCamTexture();

        public override bool CanBeAssignedToPainter => true;

        public override void Stop() => Data.webCamTexture.Stop();
        
#if PEGI
        public override bool PEGI_inList(IList list, int ind, ref int edited)
        {

            "WebCam".write(60);

            var cam = TexMGMT ? Data.webCamTexture : null;

            if (cam != null && cam.isPlaying && icon.Pause.Click("Stop Camera"))
                Data.StopCamera();

            if ((cam == null || !cam.isPlaying) && WebCamTexture.devices.Length > 0 && icon.Play.Click("Start Camera"))
            {
                Data.webCamTexture = new WebCamTexture(WebCamTexture.devices[0].name, 512, 512, 30);
                Data.webCamTexture.Play();
            }
            return false;
        }
#endif

    }

    public class SectionTarget : TextureBuffer
    {
        int targetIndex;


        #region Encode & Decode
        public override StdEncoder Encode() => new StdEncoder().Add("t", targetIndex).Add_IfTrue("show", showOnGUI);

        public override bool Decode(string tag, string data)
        {
            switch (tag)
            {
                case "t": targetIndex = data.ToInt(); break;
                case "show": showOnGUI = data.ToBool(); break;
                default: return false;
            }
            return true;
        }
        #endregion

        TextureBuffer Target { get { var sct = Mgmt.sections.TryGet(targetIndex); if (sct != null) return sct.TargetRenderTexture; else return null; } }

        public override void AfterRender()
        {
            var t = Target;
            if (t != null)
                t.AfterRender();
        }

        public override Texture GetTextureNext()
        {
            var t = Target;
            if (t != null)
                return t.GetTextureNext();
            else
                return null;

        }

        public override Texture GetTextureDisplay() => Target?.GetTextureDisplay();
        

        public override bool CanBeTarget
        {
            get
            {
                var t = Target;
                return (t != null) ?
                    t.CanBeTarget : false;
            }
        }

        public override RenderTexture GetTargetTextureNext() => Target?.GetTargetTextureNext();

        #region Inspector
#if PEGI
        public override string NameForPEGIdisplay => "Other: " + Mgmt.sections.TryGet(targetIndex).ToPEGIstring();
        public override bool Inspect() => "Source".select(50, ref targetIndex, Mgmt.sections).nl();
#endif
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

        public override string NameForPEGIdisplay => (name == null || name.Length == 0 ? "Scaler" : name);

        public override bool CanBeTarget => true;

        public override bool CanBeAssignedToPainter => true;

        ~Downscaler()
        {
            buffer.DestroyWhatever();
        }

        void InitIfNull()
        {
            if (buffer == null)
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
                var tex = other.GetTextureNext();
                if (tex != null)
                {
                    InitIfNull();

                    buffer.CopyFrom(TexMGMT.Downscale_ToBuffer(tex, width, width, material, shader));
                    lastReadVersion = other.Version;

                    var px = buffer.GetPixels();

                    // px.ToLinear();

                    buffer.SetPixels(px);

                    buffer.Apply();

                    Version++;

                    return true;
                }
            }

            return false;
        }

        public override Texture GetTextureNext() => buffer;

#if PEGI
        public override bool Inspect()
        {

            var changed = base.Inspect();

            "Name".edit(ref name).nl();

            if ("Result Width".edit(ref width).nl() ||
            "Not Color".toggle(ref linear).nl())
            {
                width = Mathf.Clamp(width, 8, 512);
                width = Mathf.ClosestPowerOfTwo(width);

                buffer.DestroyWhatever();
                InitIfNull();
            }

            return changed;
        }

#endif

        public override StdEncoder Encode() =>this.EncodeUnrecognized()
            .Add_IfNotEmpty("n", name)
            .Add_IfNotEpsilon("w", width)
            .Add_IfTrue("l", linear)
            .Add_IfTrue("show", showOnGUI)
            .Add_GUID("s", shader);

        public override bool Decode(string tag, string data)
        {
            switch (tag)
            {
                case "n": name = data; break;
                case "w": width = data.ToInt(); break;
                case "l": linear = data.ToBool(); break;
                case "show": showOnGUI = data.ToBool(); break;
                case "s": data.ToAssetByGUID(ref shader); break;
                default: return false;
            }

            return true;
        }

    }

    #endregion

    #region Section

    [Serializable]
    public class RenderSection : PainterStuff , IPEGI, IGotDisplayName, IPEGI_ListInspect
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

        public string NameForPEGIdisplay => SourceBuffer.ToPEGIstring() + "-> " + TargetRenderTexture.ToPEGIstring();//(material ? material.name : "No Material");


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

        public bool Inspect()
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

            if (material == null)
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