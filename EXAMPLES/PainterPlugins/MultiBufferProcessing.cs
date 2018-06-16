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
        
#if PEGI && UNITY_EDITOR
        using UnityEditor;
        [CustomEditor(typeof(MultiBufferProcessing))]
        public class MultiBufferProcessingEditor : Editor {
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

           // enum textureSource { Custom = 0, BigRenderTexture = 1, OnDemandRenderTexture = 2, OnDemandRenderTexturePair = 3, WebCamera, OtherSectionTarget } // Screen
           // enum textureTarget { Custom = 0, BigRenderTexture = 1, OnDemandRenderTexture = 2, OnDemandRenderTexturePair = 3 }
            enum BlitTrigger { Manual, PerFrame, AfterAnother }

           /* public Texture sourceTexture
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
            }*/

           /* public RenderTexture targetTexture
            {
                get {
                    switch (targetType) {
                        case textureTarget.Custom: return customSource;
                        case textureTarget.BigRenderTexture: return texMGMT.BigRT_pair[0];
                        default: return customSource;
                    }
                }
            }*/

            public Material material;
            public TextureBuffer targetRenderTexture;
            public TextureBuffer sourceTexture;
            public string globalParameterForTarget;


            #region Trigger
            BlitTrigger trigger;
            public bool enabled = true;
            int version = 0;
            int dependentVersion = 0;
            public RenderSection triggerSection;
            #endregion


            MultiBufferProcessing mgmt { get { return MultiBufferProcessing.inst; } }

            public string NameForPEGIdisplay() => globalParameterForTarget + " : " + sourceTexture.ToPEGIstring() + "-> " + (material ? material.name : "No Material");


            public bool Blit()
            {
                var trg = targetRenderTexture.GetRenderTexture();
                var src = sourceTexture.GetTexture();

                if (material && trg)
                {
                    bool isWebTexture = sourceTexture != null && sourceTexture.GetType() == typeof(WebCamTextureBuffer);

                    if (!isWebTexture || (mgmt && mgmt.webCamTexture && mgmt.webCamTexture.didUpdateThisFrame))
                    {
                        version++;
                        Graphics.Blit(src, trg, material);
                        targetRenderTexture.AfterRender();
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
                            if (triggerSection != null && triggerSection.version > dependentVersion)
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

              //  "Source Type".editEnum(80, ref sourceTexture).nl();

              /*  switch (sourceTexture) {
                    case textureSource.Custom:
                        if ("Source".select_SameClass_or_edit(50, ref source, mgmt.textures).nl()) {
                            if (source && mgmt.textures != null && !mgmt.textures.Contains(source))
                                mgmt.textures.Add(source);
                        }
                        break;

                    case textureSource.OtherSectionTarget:
                        "Source".select(50, ref sourceSection, mgmt.sections).nl();
                        break;
                }*/

                /*"Target Type".editEnum(80, ref targetRenderTexture).nl();

                switch (targetRenderTexture)
                {
                    case textureTarget.Custom:
                        "Target:".select_SameClass_or_edit(50, ref customSource, mgmt.textures).nl(); break;
                }*/

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

        #endregion

        #region Manager

        public class MultiBufferProcessing : PainterManagerPluginBase, iSTD
        {
            
            public static MultiBufferProcessing inst;

            [NonSerialized] public WebCamTexture webCamTexture;

            [SerializeField] string std_data;

            [NonSerialized] public List<RenderSection> sections = new List<RenderSection>();
            [NonSerialized] public List<TextureBuffer> buffers = new List<TextureBuffer>();
            [NonSerialized] public List<Texture> textures = new List<Texture>();
           
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
                    if (s != null && s.enabled && s.targetRenderTexture!= null)
                        DrawTexture(s.targetRenderTexture.GetTexture(), s.ToPEGIstring());
                    
            }

            private void Update() => sections.ForEach(s => s.Update());

            [SerializeField] int editedBuffer = -1;
            [SerializeField] int editedSection = -1;
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

                pegi.nl();

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
                StopCamera();
                base.OnDisable();
            }

            public override void OnEnable() { inst = this; std_data.DecodeInto(this); }
            
            void StopCamera()
            {
                if (webCamTexture != null)
                {
                    webCamTexture.Stop();
                    webCamTexture.DestroyWhatever();
                    webCamTexture = null;
                }
            }

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

        [DerrivedListAttribute(typeof(OnDemandRT), typeof(OnDemandRTPair), typeof(WebCamTextureBuffer), typeof(CustomTexture)) ]
        public class TextureBuffer : abstractKeepUnrecognized_STD, iGotDisplayName
        {
            protected static MultiBufferProcessing mgmt { get { return MultiBufferProcessing.inst; } }

            public virtual string NameForPEGIdisplay() => "Override This";
            
            public virtual Texture GetTexture() => null;
            
            public virtual RenderTexture GetRenderTexture() => null;

            public virtual void AfterRender() { }
           
            public bool CanBeTarget => true;
 
            public override stdEncoder Encode() => new stdEncoder();

            public override bool Decode(string tag, string data) => true; 
            
        }

        // enum textureSource { Custom = 0, BigRenderTexture = 1, OnDemandRenderTexture = 2, OnDemandRenderTexturePair = 3, WebCamera, OtherSectionTarget } // Screen

        // They will not be serialized as proper classes. Try to Encode To STD and keep type


        public class CustomTexture : TextureBuffer
        {
            Texture texture; // Only for the test
            int TextureIndex;

            public override string NameForPEGIdisplay() => "Custom: "+texture.ToPEGIstring();

            public override stdEncoder Encode() => new stdEncoder().Add_GUID("t", texture);

            public override bool Decode(string tag, string data)
            {
                switch (tag)
                {
                    case "t": data.ToAssetByGUID(ref texture); break;
                    default: return false;
                }
                return true;
            }
        }

        public class OnDemandRT : TextureBuffer
        {
            RenderTexture rt;
            public int width;
            public string name;

            public override string NameForPEGIdisplay() => "RT "+name;

            public override stdEncoder Encode() => new stdEncoder()
                .Add("w", width);
            
        }

        public class OnDemandRTPair : OnDemandRT
        {
            RenderTexture[] rts = new RenderTexture[2];

            public override string NameForPEGIdisplay() => "RT PAIR " + name;

            public override Texture GetTexture() {
                Shader.SetGlobalTexture(PainterConfig.DESTINATION_BUFFER, rts[1]);
                return rts[0];
            }

            public override void AfterRender()
            {
                var tmp = rts[0];
                rts[0] = rts[1];
                rts[1] = tmp;
            }



        }

        public class WebCamTextureBuffer: TextureBuffer {

            public override string NameForPEGIdisplay() => "Web Cam Tex";

        }

        #endregion

    }
}