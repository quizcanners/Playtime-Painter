using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using SharedTools_Stuff;
#if PEGI
using PlayerAndEditorGUI;
#endif

namespace Playtime_Painter
{

#if PEGI && UNITY_EDITOR
    using UnityEditor;
    [CustomEditor(typeof(MultiBufferProcessing))]
    public class MultiBufferProcessingEditor : Editor  {
        public override void OnInspectorGUI() => ((MultiBufferProcessing)target).inspect(serializedObject);
    }
#endif


    [Serializable]
    public class RenderSection : PainterStuff
#if PEGI
        , iPEGI, iGotDisplayName, iPEGI_ListInspect 
#endif
    {

        enum textureSource { Custom = 0, BigRenderTexture =1 , WebCamera, OtherSectionTarget } // Screen
        enum textureTarget { Custom = 0, BigRenderTexture =1 }
        enum BlitTrigger { Manual, PerFrame, AfterAnother }

        public Texture sourceTexture
        {
            get
            {
                switch (sourceType)
                {
                    case textureSource.Custom: return customSource;
                    case textureSource.BigRenderTexture: return texMGMT.BigRT_pair[0];
                    case textureSource.WebCamera: return mgmt.webCamTexture;
                    case textureSource.OtherSectionTarget: return sourceSection != null ? sourceSection.sourceTexture : null;
                    default: return customSource;
                }
            }
        }

        public RenderTexture targetTexture
        {
            get {
                switch (targetType) {
                    case textureTarget.Custom: return customSource;
                    case textureTarget.BigRenderTexture: return texMGMT.BigRT_pair[0];
                    default: return customSource;
                }
            }
        }
        
        public Material material;
        textureTarget targetType;
        textureSource sourceType;
        BlitTrigger trigger;

        public bool enabled = true;
        int version = 0;
        int dependentVersion = 0;
        public Texture source;
        public RenderSection triggerSection;
        public RenderSection sourceSection;
        public RenderTexture customSource;
        public string globalParameterForTarget;

        MultiBufferProcessing mgmt { get { return MultiBufferProcessing.inst; } }

        public string NameForPEGIdisplay() => globalParameterForTarget + " : " + sourceType.ToPEGIstring() + "-> " + (material ? material.name : "No Material");
        

        public bool Blit()
        {
            var trg = targetTexture;
            var src = sourceTexture;

            if (material || targetTexture)
            {
                if (sourceType == textureSource.WebCamera && mgmt.webCamTexture && mgmt.webCamTexture.didUpdateThisFrame) {
                    version++;
                    Graphics.Blit(src, trg, material);
                    
                } 
            }
            else
                return false;

            return true;
        }

        public void Update() {

            if (enabled)
            {
                switch (trigger)
                {
                    case BlitTrigger.PerFrame: Blit(); break;
                    case BlitTrigger.AfterAnother: 
                        if (triggerSection!= null && triggerSection.version> dependentVersion)
                        {
                            dependentVersion = triggerSection.version;
                            Blit();
                        } break;
                }
            }

        }


#if PEGI
        public bool PEGI_inList(IList list, int ind, ref int edited)
        {
           this.ToPEGIstring().write();

            if (trigger != BlitTrigger.Manual)
                pegi.toggle(ref enabled);

            if (trigger == BlitTrigger.Manual && icon.Refresh.Click())
                Blit();

            if (icon.StateMachine.Click())
                edited = ind;

            return false;
        }

        public bool PEGI()
        {
            bool changed = false;
            
                "Material".edit(60, ref material).nl();

                "Source Type".editEnum(80, ref sourceType).nl();

                switch (sourceType) {
                    case textureSource.Custom:
                        if ("Source".select_SameClass_or_edit(50, ref source, mgmt.textures).nl()) {
                            if (source && mgmt.textures != null && !mgmt.textures.Contains(source))
                                mgmt.textures.Add(source);
                        }
                        break;

                    case textureSource.OtherSectionTarget:
                        "Source".select(50, ref sourceSection, mgmt.sections).nl();
                        break;
                        
                }
                
                "Target Type".editEnum(80, ref targetType).nl();

            switch (targetType)
            {
                case textureTarget.Custom:
                    "Target:".select_SameClass_or_edit(50, ref customSource, mgmt.textures).nl(); break;
            }

            "Update Type".editEnum(70, ref trigger);

            if (trigger != BlitTrigger.PerFrame && "Blit".Click())
                Blit();

            pegi.nl();

            if (trigger != BlitTrigger.Manual)
                "Enabled".toggle(60, ref enabled).nl();
            
            switch (trigger)
            {
                case BlitTrigger.AfterAnother:
                    "After: ".select(50, ref triggerSection, mgmt.sections);
                    break;
            }
            
            pegi.nl();

            return changed;
        }
#endif

    }


    public class MultiBufferProcessing : PainterManagerPluginBase
    {
        public static MultiBufferProcessing inst;

        [NonSerialized] public WebCamTexture webCamTexture;

        public List<RenderSection> sections = new List<RenderSection>();
        public List<Texture> textures = new List<Texture>();

        #region PEGI
#if PEGI

        public override string NameForPEGIdisplay() => "Buffer Blitting";
        

        [SerializeField] int OnScreenSize = 128;
        int texIndex = 0;
        void DrawTexture(Texture tex, string text)
        {
            if (tex != null)
            {
                Rect pos = new Rect(0f, texIndex * (OnScreenSize + 30) - 30, OnScreenSize, OnScreenSize);
                GUI.Label(pos, text);
                pos.y += 30;
                GUI.DrawTexture(pos, tex);
                texIndex++;
            }
        }

        void OnGUI()
        {
            texIndex = 0;
            DrawTexture(webCamTexture, "Input");

            foreach (var s in sections)
                if (s != null && s.enabled)
                    DrawTexture(s.customSource, s.ToPEGIstring());
        }

        private void Update() => sections.ForEach(s => s.Update());
        
        [SerializeField] int editedBuffer = -1;
        public override bool ConfigTab_PEGI()
        {
            bool changed = false;

            "Size: ".edit(40, ref OnScreenSize, 128, 512).nl();

            if (webCamTexture != null && icon.Pause.Click("Stop Camera"))
                    StopCamera();
            

            if (webCamTexture == null && WebCamTexture.devices.Length > 0 && icon.Play.Click("Start Camera"))
            {
                webCamTexture = new WebCamTexture(WebCamTexture.devices[0].name, 512, 512, 30);

                if (textures.Count > 0 && textures[0] == null)
                    textures[0] = webCamTexture;
                else textures.Add(webCamTexture);

                webCamTexture.Play();
            }

            "Sections".edit_List(sections, ref editedBuffer, true);

            return changed;

        }
#endif
#endregion

        public override void OnDisable()
        {
            StopCamera();
            base.OnDisable();
        }

        public override void OnEnable() => inst = this;
        

        void StopCamera()
        {
            if (webCamTexture != null)
            {
                webCamTexture.Stop();
                webCamTexture.DestroyWhatever();
                webCamTexture = null;
            }
        }

    }
}