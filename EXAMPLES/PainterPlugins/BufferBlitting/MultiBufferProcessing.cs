using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using SharedTools_Stuff;
#if PEGI
using PlayerAndEditorGUI;
#endif

#region Namespaces And Editor

namespace Playtime_Painter
{

    namespace MultiBufferProcessing
    {
        using System.Linq;

#if PEGI && UNITY_EDITOR
        using UnityEditor;
        [CustomEditor(typeof(MultiBufferProcessing))]
        public class MultiBufferProcessingEditor : Editor
        {
            public override void OnInspectorGUI() => ((MultiBufferProcessing)target).inspect(serializedObject);
        }
#endif

        #endregion

        #region Section

     

        [Serializable]
        public class RenderSection : PainterStuff
#if PEGI
        , iPEGI, iGotDisplayName, iPEGI_ListInspect
#endif
        {
            enum BlitTrigger { Manual, PerFrame, WhenOtherSectionUpdated, WhenSourceReady, Delay, DelayAndUpdated }

            [SerializeField] Material material;
            [SerializeField] Shader shader;
            [SerializeField] public Material previewMaterial;
            
            [SerializeField] int targetBufferIndex = -1;
            [SerializeField] int sourceBufferIndex = -1;
            [SerializeField] float delayTime;
            public TextureBuffer targetRenderTexture => mgmt.buffers.TryGet(targetBufferIndex);
            public TextureBuffer sourceBuffer => mgmt.buffers.TryGet(sourceBufferIndex);
            MultiBufferProcessing mgmt { get { return MultiBufferProcessing.inst; } }
            
            int version { get { return targetRenderTexture != null ? targetRenderTexture.version : 0; } }
            
            [SerializeField] BlitTrigger trigger;
            [SerializeField] bool enabled = true;
            [SerializeField] RenderSection triggerSection;

            int dependentVersion = 0;
            float timer = 0;
           
            public string NameForPEGIdisplay() => sourceBuffer.ToPEGIstring() + "-> " + targetRenderTexture.ToPEGIstring();//(material ? material.name : "No Material");

            public bool Blit() => targetRenderTexture != null ? targetRenderTexture.BlitMethod(sourceBuffer, material, shader) : false;

            public void Update()
            {

                if (enabled)
                {
                    switch (trigger)
                    {
                        case BlitTrigger.PerFrame:
                            Blit(); break;
                        case BlitTrigger.WhenOtherSectionUpdated:
                            if (triggerSection != null && triggerSection.version > dependentVersion)
                            {
                                dependentVersion = triggerSection.version;
                                Blit();
                            }
                            break;

                        case BlitTrigger.DelayAndUpdated:
                            timer -= Time.deltaTime;
                            if (timer < 0 && sourceBuffer != null && sourceBuffer.version > dependentVersion)
                                {
                                    dependentVersion = sourceBuffer.version;
                                timer = delayTime;
                                Blit();
                                }
                                break;

                        case BlitTrigger.WhenSourceReady:
                            if (sourceBuffer != null && sourceBuffer.isReady)
                            {
                                dependentVersion = sourceBuffer.version;
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
            
#if PEGI
            public bool PEGI_inList(IList list, int ind, ref int edited)
            {
                this.ToPEGIstring().write();

                if (trigger != BlitTrigger.Manual)
                    pegi.toggle(ref enabled);

                if (targetRenderTexture != null && trigger == BlitTrigger.Manual && icon.Refresh.Click())
                    Blit();

                if (icon.StateMachine.Click())
                    edited = ind;

                return false;
            }

            public bool PEGI()
            {
                bool changed = false;

                "Update Type".editEnum(70, ref trigger);

                if ((trigger != BlitTrigger.PerFrame || !enabled) && "Blit".Click())
                    Blit();

                if (trigger != BlitTrigger.Manual)
                    pegi.toggle(ref enabled, icon.Pause, icon.Play);

                pegi.nl();
         
                "From ".select(ref sourceBufferIndex, mgmt.buffers).nl();

                "To ".select(ref targetBufferIndex, mgmt.buffers, e => e.CanBeTarget ).nl();
                
                if (material || !shader)
                    "Blit Material".edit(100, ref material).nl();

                if (material == null)
                    "Shader".edit(60, ref shader).nl();

                if (targetRenderTexture != null)
                    "Preview Material".edit(100, ref previewMaterial);

                pegi.nl();

                switch (trigger)
                {
                    case BlitTrigger.WhenOtherSectionUpdated:
                        "After: ".select(50, ref triggerSection, mgmt.sections);
                        break;
                    case BlitTrigger.DelayAndUpdated:
                    case BlitTrigger.Delay:
                        "Delay:".edit(ref delayTime).nl(); break;
                }

                pegi.nl();

                return changed;
            }
#endif

        }

        #endregion

        #region Manager

        [ExecuteInEditMode]
        public class MultiBufferProcessing : PainterManagerPluginBase, iSTD
        {

            public static MultiBufferProcessing inst;

            [SerializeField] string std_data;

            public List<RenderSection> sections = new List<RenderSection>();
            public List<TextureBuffer> buffers = new List<TextureBuffer>();
            
#if PEGI
            public override string NameForPEGIdisplay() => "Buffer Blitting";

            [SerializeField] int GUITextureSize = 128;
            int texIndex = 0;

            bool pauseBuffers = false;

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
                        
                        var sct = sections.Where(s => s.targetRenderTexture != null && s.targetRenderTexture == b);
                        RenderSection section = sct.Count() > 0 ? sct.First() : null;
                        
                        var tex = b.GetTextureDisplay();

                        Rect pos = new Rect((texIndex / inColumn) * GUITextureSize, (texIndex % inColumn) * fullSize - textGap, GUITextureSize, GUITextureSize);
                        GUI.Label(pos, b.ToPEGIstring());
                        pos.y += textGap;
                        
                        if (tex) {
                            if (section != null && section.previewMaterial)
                                Graphics.DrawTexture(pos, tex, section.previewMaterial);
                            else
                                GUI.DrawTexture(pos, tex);
                        }

                        texIndex++;

                        pos.width = 25;
                        pos.height = 25;
                        
                        if (section != null && GUI.Button(pos, icon.Refresh.getIcon()))
                            section.Blit();
                    }

            }

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

            [SerializeField] int editedBuffer = -1;
            [SerializeField] int editedSection = -1;
            public override bool ConfigTab_PEGI()
            {
                bool changed = false;

                if ((pauseBuffers ? icon.Play : icon.Pause).Click("Stop/Start ALL"))
                    pauseBuffers = !pauseBuffers;
                
                if (editedSection == -1 && editedBuffer == -1)
                    "Size: ".edit(40, ref GUITextureSize, 128, 512).nl();
                
                if (editedBuffer == -1)
                    "Sections".edit_List(sections, ref editedSection, true);
                else
                    editedSection = -1;

                if (editedSection == -1)
                    "Buffers".edit_List(buffers, ref editedBuffer, true);

                return changed;
            }
#endif

            public override void OnDisable()
            {
                std_data = Encode().ToString();
               
                base.OnDisable();
#if UNITY_EDITOR
                EditorApplication.update -= ManualUpdate;
#endif
            }

            public override void OnEnable() {
                inst = this;
                std_data.DecodeInto(this);
#if UNITY_EDITOR
                EditorApplication.update -= ManualUpdate;
                if (!this.ApplicationIsAboutToEnterPlayMode())
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
                
                if (buffers.Count>0)
                {
                    if ((pauseBuffers ? icon.Play : icon.Pause).Click("Stop/Start ALL"))
                        pauseBuffers = !pauseBuffers;

                    int cur = -1;
                    if ("Buffers".select(60, ref cur, buffers, (x) => x.CanBeAssignedToPainter ).nl()) {
                        changed = true;
                        inspectedPainter.SetTextureOnMaterial(buffers.TryGet(cur).GetTextureDisplay());
                    }
                }

                return changed;
            }
#endif
            public override stdEncoder Encode() => EncodeUnrecognized()
                .Add("s", buffers);

            public override bool Decode(string tag, string data)
            {
                switch (tag)
                {
                    case "s": data.DecodeInto(out buffers); break;
                    default: return false;
                }
                return true;
            }
            

        }

        #endregion

        #region TextureBuffers

        [DerrivedListAttribute(typeof(OnDemandRT), typeof(OnDemandRTPair), typeof(WebCamTextureBuffer), typeof(CustomImageData)
            , typeof(SectionTarget)
            , typeof(BigRTpair)
            , typeof(Downscaler)
            )]
        public class TextureBuffer : abstractKeepUnrecognized_STD, iGotDisplayName
#if PEGI
            , iPEGI_ListInspect
#endif
        {
            
            int _version = 0;

            public virtual int version { get { return _version; } set { _version = value; } }

            public bool showOnGUI  = false;

            protected static MultiBufferProcessing mgmt { get { return MultiBufferProcessing.inst; } }

            protected static PainterManager texMGMT { get { return PainterManager.inst; } }

            public virtual string NameForPEGIdisplay() => "Override This";

            public virtual Texture GetTextureNext() => null;

            public virtual Texture GetTextureDisplay() => GetTextureNext();

            public virtual RenderTexture GetTargetTextureNext()
            {
                var rt = renderTexture();
                return rt;
            }

            protected virtual RenderTexture renderTexture() => null;

            public virtual void AfterRender() { }

            public virtual bool CanBeTarget => false;

            public virtual bool CanBeAssignedToPainter => false;

            public virtual bool isReady => true;

            public virtual bool BlitMethod(TextureBuffer sourceBuffer, Material material, Shader shader)
            {
                    var trg = GetTargetTextureNext();
                    var src = sourceBuffer != null ? sourceBuffer.GetTextureNext() : null;

                    if (trg)
                    {
                        if (src)
                            Shader.SetGlobalTexture(PainterConfig.SOURCE_TEXTURE, src);

                        if (material)
                            Graphics.Blit(src, trg, material);
                        else
                            PainterManager.inst.Render(src, trg, shader);
                        AfterRender();

                        version++;

                        return true;
                    }
                

                return false;
            }

#if PEGI
            public override bool PEGI()
            {
                bool changed = false;

                "Show On GUI".toggle(ref showOnGUI).nl();

                return changed;
            }

            public virtual bool PEGI_inList(IList list, int ind, ref int edited)
            {
                bool changed = NameForPEGIdisplay().toggle(ref showOnGUI);

                var asP = this as iPEGI;
                if (asP != null && icon.Enter.Click())
                    edited = ind;

                return changed;
            }

#endif

            public override stdEncoder Encode() => EncodeUnrecognized().Add_ifTrue("show", showOnGUI);

            public override bool Decode(string tag, string data)
            {
                switch (tag)
                {
                    case "show": showOnGUI = data.ToBool(); break;
                    default: return false;
                }
                return true;
            }
        }

        public class CustomImageData : TextureBuffer
#if PEGI
           , iPEGI_ListInspect, iPEGI
#endif
        {

            ImageData id;
            Texture texture => id.currentTexture(); 

            public override string NameForPEGIdisplay() => "Custom: " + texture.ToPEGIstring();

            public override stdEncoder Encode() => new stdEncoder()
                .Add_GUID("t", texture)
                .Add_ifTrue("show", showOnGUI);

            public override bool Decode(string tag, string data)
            {
                switch (tag)
                {
                    case "t": Texture tmp = texture; data.ToAssetByGUID(ref tmp); if (tmp != null) id = texture.getImgData(); break;
                    case "show": showOnGUI = data.ToBool(); break;
                    default: return false;
                }
                return true;
            }

            public override bool CanBeTarget => texture && texture.GetType() == typeof(RenderTexture);

            public override Texture GetTextureNext() => texture;

            protected override RenderTexture renderTexture() => texture ? (RenderTexture)texture : null;

#if PEGI

            public override bool PEGI_inList(IList list, int ind, ref int edited) => "Source".select(50, ref id, texMGMT.imgDatas);

            public override bool PEGI()
            {
                "Source".select(50, ref id, texMGMT.imgDatas);
                Texture tmp = texture;
                if ("Texture".edit(ref tmp).nl() && (tmp != null))
                        id = tmp.getImgData();
                
                return false;
            }

#endif
        }

        public class BigRTpair : TextureBuffer
        {

            public override string NameForPEGIdisplay() => "BIG RT pair";

            public override void AfterRender()
            {
                texMGMT.UpdateBufferTwo();
                texMGMT.bigRTversion++;
            }

            public override int version
            {
                get
                {
                    return texMGMT.bigRTversion;
                }

                set
                {
                    texMGMT.bigRTversion = value;
                }
            }

            public override Texture GetTextureNext() => renderTexture();

            protected override RenderTexture renderTexture()
            {
                Shader.SetGlobalTexture(PainterConfig.DESTINATION_BUFFER, texMGMT.BigRT_pair[1]);
                return texMGMT.BigRT_pair[0];
            }

            public override Texture GetTextureDisplay() => texMGMT.BigRT_pair[0];

            public override bool CanBeTarget => true;


        }

        public class OnDemandRT : TextureBuffer
#if PEGI
           , iPEGI
#endif
        {
            RenderTexture rt;
            public int width = 512;
            public string name;
            public bool linear;
            public RenderTextureReadWrite colorMode;

            public override string NameForPEGIdisplay() => "RT " + name;

            public override bool CanBeTarget => true;

            public override Texture GetTextureNext() => renderTexture();

            public override bool CanBeAssignedToPainter => true;

            protected override RenderTexture renderTexture()
            {
                if (rt == null) rt = new RenderTexture(width, width, 0, RenderTextureFormat.ARGBFloat, colorMode);
                return rt;
            }

            public override stdEncoder Encode() => EncodeUnrecognized()
                .Add("w", width)
                .Add("c", (int)colorMode)
                .Add_ifTrue("show", showOnGUI);

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
            public override bool PEGI()
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

            public override string NameForPEGIdisplay() => "RT PAIR " + name;

            public override bool CanBeTarget => true;

            public override Texture GetTextureDisplay() => rts[0];

            protected override RenderTexture renderTexture()
            {
                if (rts == null)
                {
                    rts = new RenderTexture[2];
                    rts[0] = new RenderTexture(width, width, 0, RenderTextureFormat.ARGBFloat, colorMode);
                    rts[1] = new RenderTexture(width, width, 0, RenderTextureFormat.ARGBFloat, colorMode);
                }

                Shader.SetGlobalTexture(PainterConfig.DESTINATION_BUFFER, rts[1]);
                return rts[0];
            }

            public override Texture GetTextureNext() => renderTexture();

            public override void AfterRender()
            {
                var tmp = rts[0];
                rts[0] = rts[1];
                rts[1] = tmp;
            }

            public override bool CanBeAssignedToPainter => true;


        }

        public class WebCamTextureBuffer : TextureBuffer
#if PEGI
           , iPEGI_ListInspect
#endif
        {

            public override stdEncoder Encode() => new stdEncoder().Add_ifTrue("show", showOnGUI);

            public override bool Decode(string tag, string data)
            {
                switch (tag)
                {
                    case "show": showOnGUI = data.ToBool(); break;
                    default: return false;
                }
                return true;
            }

            public override bool isReady
            {
                get
                {
                    var cam = texMGMT ? texMGMT.webCamTexture : null;

                    return mgmt
                        && (cam == null || cam.isPlaying == false || cam.didUpdateThisFrame);
                }
            }

            public override string NameForPEGIdisplay() => "Web Cam Tex";

            public override Texture GetTextureDisplay() =>  texMGMT.webCamTexture;
            
            public override Texture GetTextureNext() => texMGMT.GetWebCamTexture();

            public override bool CanBeAssignedToPainter => true;

#if PEGI
            public override bool PEGI_inList(IList list, int ind, ref int edited)
            {

                "WebCam".write(60);

                var cam = texMGMT ? texMGMT.webCamTexture : null;

                if (cam != null && cam.isPlaying && icon.Pause.Click("Stop Camera"))
                    texMGMT.StopCamera();

                if ((cam == null || !cam.isPlaying) && WebCamTexture.devices.Length > 0 && icon.Play.Click("Start Camera"))
                {
                    texMGMT.webCamTexture = new WebCamTexture(WebCamTexture.devices[0].name, 512, 512, 30);
                    texMGMT.webCamTexture.Play();
                }
                return false;
            }
#endif
        }
        
        public class SectionTarget : TextureBuffer
        {
            int targetIndex;

            public override string NameForPEGIdisplay() => "Other: " + mgmt.sections.TryGet(targetIndex).ToPEGIstring();
            
            public override stdEncoder Encode() => new stdEncoder().Add("t", targetIndex).Add_ifTrue("show", showOnGUI);

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

            TextureBuffer target { get { var sct = mgmt.sections.TryGet(targetIndex); if (sct != null) return sct.targetRenderTexture; else return null; } }

            public override void AfterRender()
            {
                var t = target;
                if (t != null)
                    t.AfterRender();
            }

            public override Texture GetTextureNext()
            {
                var t = target;
                return (t != null) ?
                    t.GetTextureNext() : null;

            }

            public override Texture GetTextureDisplay()
            {
                var sct = target;
                return sct != null ? sct.GetTextureDisplay() : null;
            }

            public override bool CanBeTarget
            {
                get
                {
                    var t = target;
                    return (t != null) ?
                        t.CanBeTarget : false;
                }
            }

            public override RenderTexture GetTargetTextureNext()
            {
                var t = target;
                return (t != null) ?
                    t.GetTargetTextureNext() : null;
            }

#if PEGI
            public override bool PEGI()
            {
                "Source".select(50, ref targetIndex, mgmt.sections).nl();

                return false;
            }
#endif

        }
        
        public class Downscaler : TextureBuffer
#if PEGI
            ,iPEGI
#endif
        {
            string name;
            int width = 64;
            int lastReadVersion = -1;
            bool linear;
            Shader shader;
            Texture2D buffer;

            public override string NameForPEGIdisplay() => (name == null || name.Length == 0 ? "Scaler" : name);

            public override bool CanBeTarget => true;

            ~Downscaler() {
                buffer.DestroyWhatever();
            }

            void InitIfNull()
            {
                if (buffer == null)
                {
                    buffer = new Texture2D(width, width, TextureFormat.ARGB32, false, linear);
                    buffer.alphaIsTransparency = true;
                }
            }

            public override bool BlitMethod(TextureBuffer sourceBuffer, Material material, Shader shader)
            {
                var other = sourceBuffer;

                if (other != null && lastReadVersion != other.version)
                {
                    var tex = other.GetTextureNext();
                    if (tex != null)
                    {
                        InitIfNull();

                        buffer.CopyFrom(texMGMT.Downscale_ToBuffer(tex, width, width, material, shader));
                        lastReadVersion = other.version;

                        var px = buffer.GetPixels();

                       // px.ToLinear();

                        buffer.SetPixels(px);

                        buffer.Apply();

                        version++;

                        return true;
                    }
                }

                return false;
            }

            public override Texture GetTextureNext() => buffer;

#if PEGI
            public override bool PEGI()
            {

                var changed = base.PEGI();

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

            public override stdEncoder Encode() => EncodeUnrecognized()
                .Add_IfNotEmpty("n", name)
                .Add_IfNotZero("w", width)
                .Add_ifTrue("l", linear)
                .Add_ifTrue("show", showOnGUI)
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


      
    }
}