using System;
using QuizCanners.Inspect;
using PainterTool.CameraModules;
using PainterTool.MeshEditing;
using QuizCanners.Migration;
using QuizCanners.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PainterTool {
    
    [Serializable]
    public class Brush : PainterClassCfg, IPEGI 
    {
        
        // COPY BLIT
        public int selectedSourceTexture;
        public bool clampSourceTexture;
        public bool ignoreSrcTextureTransparency;
        public SourceTextureColorUsage srcColorUsage = SourceTextureColorUsage.Unchanged;

        private bool fallbackTargetIsTex2D;
        public TexTarget FallbackTarget
        {
            get => fallbackTargetIsTex2D ? TexTarget.Texture2D : TexTarget.RenderTexture;
            set => fallbackTargetIsTex2D = value == TexTarget.Texture2D;
        }

        [SerializeField] private int _inGpuBrushType;
        [SerializeField] private int _inCpuBrushType;
        [SerializeField] private int _inGpuBlitMode;
        [SerializeField] private int _inCpuBlitMode;

        [NonSerialized] public bool previewDirty = false;

        public void Paint(Painter.Command.Base command)
        {
            if (command == null) 
            {
                Debug.LogError("Command is nul");
                return;
            }

            var imgData = command.TextureData;

            if (imgData == null)
            {
                Debug.LogError("Img Data is null");
                return;
            }


            TexTarget target = command.TextureData.Target;
            BrushTypes.Base brushType = GetBrushType(target);
            BlitModes.Base blitMode = GetBlitMode(target);
            bool isWorldSpace = command.Is3DBrush;

            if (brushType.IsAWorldSpaceBrush && !isWorldSpace)
            {
                brushType = BrushTypes.Normal.Inst;
            }

            Painter.Command.ForPainterComponent painterCommand = command as Painter.Command.ForPainterComponent;

            PainterComponent painter = painterCommand?.painter;

            blitMode.PrePaint(command);

            if (target == TexTarget.Texture2D)
            {
                if (painter)
                    foreach (var module in imgData.Modules)
                        module.OnPainting(painter);

                brushType.PaintPixelsInRam(command);
            }
            else
            {
                if (painter)
                {
                    var materialData = painter.MatDta;

                    if (!imgData.RenderTexture && !Painter.Camera.materialsUsingRenderTexture.Contains(materialData))
                    {
                        Painter.Camera.ChangeBufferTarget(imgData, materialData, painter.GetMaterialTextureProperty(), painter);
                        painter.SetTextureOnMaterial(imgData);
                    }

                    var rendered = false;

                    foreach (var pl in CameraModuleBase.BrushPlugins)
                        if (pl.IsEnabledFor(painter, imgData, this))
                        {
                            pl.PaintRenderTextureUvSpace(command);
                            rendered = true;
                            break;
                        }

                    foreach (var module in imgData.Modules)
                        module.OnPainting(painter);

                    if (!rendered)
                    {
                        if (isWorldSpace)
                            brushType.PaintRenderTextureInWorldSpace(command as Painter.Command.WorldSpaceBase);
                        else
                            brushType.PaintRenderTextureUvSpace(command);
                    }
                    
                }
                else
                {
                    if (isWorldSpace)
                        brushType.PaintRenderTextureInWorldSpace(command as Painter.Command.WorldSpaceBase);
                    else
                        brushType.PaintRenderTextureUvSpace(command);
                }
            }
        }

        #region Modes & Types
        internal TexTarget GetTarget(TextureMeta textureMeta) => textureMeta!= null ? textureMeta.Target : FallbackTarget;

        private int BrushTypeIndex(TexTarget target)
        {
            return target == TexTarget.Texture2D ? _inCpuBrushType : _inGpuBrushType;
        }
            public void SetBrushType(TexTarget target, BrushTypes.Base t) { if (target == TexTarget.Texture2D) _inCpuBrushType = t.index; else _inGpuBrushType = t.index; }

        internal BrushTypes.Base GetBrushType(TextureMeta textureMeta) => GetBrushType(GetTarget(textureMeta));
        public BrushTypes.Base GetBrushType(TexTarget target)
        {
            return BrushTypes.All[BrushTypeIndex(target)];
        }
        public int BlitMode(TexTarget target) => target == TexTarget.Texture2D ? _inCpuBlitMode : _inGpuBlitMode;
        public BlitModes.Base GetBlitMode(TexTarget target) => BlitModes.All[BlitMode(target)];

        public void SetBlitMode(TexTarget target, BlitModes.Base mode)
        {
            if (target == TexTarget.Texture2D) _inCpuBlitMode = mode.index;
            else _inGpuBlitMode = mode.index;
            
        }
        #endregion

        private void SetSupportedFor(TexTarget target, bool rtDoubleBuffer) 
        {
            var mode = GetBlitMode(target);
            var type = GetBrushType(target);

            if (target == TexTarget.RenderTexture)
            {
                if (rtDoubleBuffer) {
                    if (!type.SupportedByRenderTexturePair) foreach (var t in BrushTypes.All) { if (t.SupportedByRenderTexturePair) { SetBrushType(target, t); break; } }
                    if (!mode.SupportedByRenderTexturePair) foreach (var t in BlitModes.All) { if (t.SupportedByRenderTexturePair) { SetBlitMode(target, t); break; } }
                } else {
                    if (!type.SupportedBySingleBuffer) foreach (var t in BrushTypes.All) { if (t.SupportedBySingleBuffer) { SetBrushType(target, t); break; } }
                    if (!mode.SupportedBySingleBuffer) foreach (var t in BlitModes.All) { if (t.SupportedBySingleBuffer) { SetBlitMode(target, t); break; } }
                }
            } else
            {
                if (!type.SupportedByTex2D) foreach (var t in BrushTypes.All) { if (t.SupportedByTex2D) { SetBrushType(target, t); break; } }
                if (!mode.SupportedByTex2D) foreach (var t in BlitModes.All) { if (t.SupportedByTex2D) { SetBlitMode(target, t); break; } }
            }
        }
        
        #region Masking

        public bool useMask;
        public int selectedSourceMask;
        public bool maskFromGreyscale;
        public Vector2 maskOffset;
        public bool randomMaskOffset;
        public bool flipMaskAlpha;
        public float maskTiling = 1;
        
        public void MaskSet(ColorMask flag, bool to)
        {
            if (to)
                mask |= flag;
            else
                mask &= ~flag;
        }

        public ColorMask mask;

        public bool PaintingAllChannels => mask.HasFlag(ColorMask.R) && mask.HasFlag( ColorMask.G) 
                                        && mask.HasFlag( ColorMask.B) && mask.HasFlag( ColorMask.A);

        public bool PaintingRGB => mask.HasFlag(ColorMask.R) && mask.HasFlag( ColorMask.G)
                                && mask.HasFlag(ColorMask.B) && (!mask.HasFlag( ColorMask.A));
        
        #endregion

        #region Decal
        public int selectedDecal;
        public float decalAngle;
        public BrushTypes.Decal.RotationMethod rotationMethod;
        public bool decalContentious;
        public float decalAngleModifier;
        #endregion
        
        #region Brush Dynamics
     
        public bool showBrushDynamics;

        public BrushDynamic.Base brushDynamic = new BrushDynamic.None();
        #endregion

        #region Brush Parameters
        public float hardness = 2;
        public float blurAmount = 1;
        public float brush3DRadius = 0.2f;
        public float brush2DRadius = 16;
        public bool useAlphaBuffer;

        public float alphaLimitForAlphaBuffer = 0.5f;

        public bool worldSpaceBrushPixelJitter;

        public float Size(bool worldSpace) => Mathf.Max(0.01f, worldSpace ? brush3DRadius : brush2DRadius);
        public Color Color;

        public bool Is3DBrush(TextureMeta texture = null)
        {
            return GetBrushType(texture).IsAWorldSpaceBrush;
        }

        [SerializeField] public QcMath.DynamicRangeFloat _dFlow = new(0.1f, 4.5f, 3f );

        public float Flow
        {
            get { return _dFlow.Value * _dFlow.Value; }
            set { _dFlow.Value = Mathf.Sqrt(value); }
        }

        #endregion
        
        public Brush() {
            Color = Color.green;
            mask = new ColorMask();
            mask |= ColorMask.R | ColorMask.G | ColorMask.B;
        }
        
      
        #region Inspector

        public bool showingSize = true;
        public static bool showAdvanced;
        public static Brush _inspectedBrush;
        public static bool InspectedIsCpuBrush => PainterComponent.inspected ? InspectedImageMeta.TargetIsTexture2D() : _inspectedBrush.fallbackTargetIsTex2D;
     
        public void Mode_Type_PEGI()
        {
            var changes = pegi.ChangeTrackStart();

            var p = PainterComponent.inspected;
            TextureMeta id = p ? p.TexMeta : null;

            IPainterManagerModuleBrush cameraModule = null;

            foreach (var b in CameraModuleBase.BrushPlugins)
                if (b.IsEnabledFor(p, id, this)) {
                    cameraModule = b;
                    break;
                }

            BrushTypes.All.ClampIndexToCount(ref _inCpuBrushType);
            BrushTypes.All.ClampIndexToCount(ref _inGpuBrushType);
            
            _inspectedBrush = this;

            TexTarget target = GetTarget(id);

            BlitModes.Base blitMode = GetBlitMode(target);
            BrushTypes.Base brushType = GetBrushType(target);

            pegi.Nl();

            MsgPainter.BlitMode.Write("How final color will be calculated", width: 60);

            if (pegi.Select(ref blitMode, BlitModes.All)) 
                SetBlitMode(target, blitMode);

            if (DocsEnabled && blitMode != null)
                pegi.FullWindow.DocumentationClickOpen(blitMode.ToolTip, toolTip: "About {0} mode".F(blitMode.ToString()));

            if (showAdvanced)
                pegi.Nl();

            pegi.Toggle(ref showAdvanced, Icon.FoldedOut, Icon.Create, "Brush Options", 25);

            if (showAdvanced)
                "Advanced options: (if any)".PL().Write();
            

            pegi.Nl();

            if (target == TexTarget.RenderTexture)  {
               
                MsgPainter.BrushType.Write(70);
                pegi.Select_Index(ref _inGpuBrushType, BrushTypes.All);

                if (DocsEnabled && brushType != null)
                    pegi.FullWindow.DocumentationClickOpen(brushType.ToolTip, toolTip: "About {0} brush type".F(brushType.ToString()));

                if (!brushType.ShowInInspectorDropdown())
                {
                    pegi.Nl();
                    "Selected brush type is not supported in context of this Painter".PL().WriteWarning();
                    
                }

                pegi.Nl();

            }
            
             cameraModule?.BrushConfigPEGI(this);

              if (p)
              {
                foreach (var pl in p.Modules)
                {
                    pl.BrushConfigPEGI();
                    pegi.Nl();
                }


                if (id != null)
                    foreach (var mod in id.Modules)
                    {
                        pegi.Nested_Inspect(()=> mod.BrushConfigPEGI(p), null);
                       
                        pegi.Nl();
                    }
              }

          
            brushType.Nested_Inspect().Nl();

          

            if (blitMode.AllSetUp) {

                if (blitMode.UsingSourceTexture) {

                    "Texture Color".ConstL().Edit_Enum(ref srcColorUsage).Nl();

                    if (InspectAdvanced) {
                        "Clamp".PL().ToggleIcon(ref clampSourceTexture).Nl();
                        "Multiply by Alpha".PL().ToggleIcon(ref ignoreSrcTextureTransparency);
                        pegi.FullWindow.DocumentationClickOpen("Ignore transparency of the source texture. To only paint parts of the texture which are visible").Nl();
                    }
                }
                
                if (target == TexTarget.RenderTexture && brushType.SupportsAlphaBufferPainting && blitMode.SupportsAlphaBufferPainting && (useAlphaBuffer || InspectAdvanced)) {

                    "Alpha Buffer".PL().ToggleIcon(ref useAlphaBuffer, true);

                    if (useAlphaBuffer)
                    {
                        var txt = MsgPainter.Opacity.GetText();

                        txt.PL("This is the kind of alpha you see in standard painting software. But it is only available when using Alpha Buffer"
                            ).ApproxWidth().Edit( ref alphaLimitForAlphaBuffer, 0.01f, 1f);

                        if (p && p.NotUsingPreview)
                            MsgPainter.PreviewRecommended.DocumentationWarning();

                    }

                    MsgPainter.AlphaBufferBlit.DocumentationClick().Nl();

                    pegi.Nl();
                }
            }

          
            if (blitMode.ShowInInspectorDropdown())
            {
                blitMode.InspectWithModule(target); pegi.Nl();
                showingSize = true;
            } else 
            {
                pegi.Nl();
                "Blit Mode {0} is not Supported for {1} painting".F(blitMode.ToString(), target).PL().WriteWarning().Nl();
            }
          
            _inspectedBrush = null;

        }

        public void Targets_PEGI()
        {
            var ico = Painter.Data.UiIcons;
            if (pegi.Click(fallbackTargetIsTex2D ? ico.CPU : ico.GPU, fallbackTargetIsTex2D ? "Render Texture Config" : "Texture2D Config", 45))
            {
                fallbackTargetIsTex2D = !fallbackTargetIsTex2D;
                SetSupportedFor(fallbackTargetIsTex2D ? TexTarget.Texture2D : TexTarget.RenderTexture, true);
            }

            var smooth = GetBrushType(fallbackTargetIsTex2D ? TexTarget.Texture2D : TexTarget.RenderTexture) != BrushTypes.Pixel.Inst;


            if (fallbackTargetIsTex2D)
            {
                var current = smooth ? ico.Round : ico.Square;
                if (pegi.Click(current))
                {
                    smooth = !smooth;
                    //pegi.Toggle(ref smooth, Icon.Round, Icon.Square, "Smooth/Pixels Brush", 45))
                    SetBrushType(fallbackTargetIsTex2D ? TexTarget.Texture2D : TexTarget.RenderTexture, smooth ? BrushTypes.Normal.Inst : BrushTypes.Pixel.Inst);
                }
            }
        }
        
        public virtual void Inspect() {

            var p = PainterComponent.inspected;

            if (!p) 
            {
                Targets_PEGI(); pegi.Nl();
                Mode_Type_PEGI(); pegi.Nl();
                ColorSliders(); pegi.Nl();
                return;
            }
            
            var id = p.TexMeta;

            var target = id.Target;
            bool cpuBlit = target == TexTarget.Texture2D;

            var icos = Painter.Data.UiIcons;

            if (id.Texture2D)
            {
                var ico = cpuBlit ? icos.CPU : icos.GPU;
                if (pegi.Click(ico, cpuBlit ? "Switch to Render Texture" : "Switch to Texture2D" ,45))
                {
                    cpuBlit = !cpuBlit;

                    if (!cpuBlit && !Singleton_PainterCamera.GotBuffers)
                        RenderTextureBuffersManager.RefreshPaintingBuffers();

                    var trg = cpuBlit ? TexTarget.Texture2D : TexTarget.RenderTexture;

                    p.UpdateOrSetTexTarget(trg);
                    SetSupportedFor(trg, !id.RenderTexture);
                }
            }
            
            var smooth = BrushTypeIndex(target) != BrushTypes.Pixel.Inst.index;
            
            if (cpuBlit) 
            {
                var sIco = smooth ? icos.Round : icos.Square;
                if (pegi.Click(sIco, "Smooth/Pixels Brush", 45))
                {
                    smooth = !smooth;
                    SetBrushType(target, smooth ? BrushTypes.Normal.Inst : BrushTypes.Pixel.Inst);
                } //pegi.Toggle(ref smooth, Icon.Round, Icon.Square, "Smooth/Pixels Brush", 45))
                   
            }

            pegi.Nl();

            if (showBrushDynamics) {
                if ("Brush Dynamic".ConstL().SelectType( ref brushDynamic).Nl())
                    brushDynamic?.Nested_Inspect().Nl();
            }
            else if (brushDynamic.GetType() != typeof(BrushDynamic.None))
                    brushDynamic = (BrushDynamic.None)Activator.CreateInstance(typeof(BrushDynamic.None));
            
            if (pegi.Nested_Inspect(Mode_Type_PEGI) && GetBrushType(target) == BrushTypes.Decal.Inst)
                    MaskSet(ColorMask.A, true);
        }

        public void ChannelSlider(ColorChanel chan, ref Color col)
        {
            pegi.Draw(chan.GetIcon());
            float val = chan.GetValueFrom(col);
            if (pegi.Edit(ref val, 0, 1).Nl())
            {
                chan.SetValueOn(ref col, val);
                return;
            }

            return;
        }

        public void ChannelSlider(ColorMask inspectedMask, ref Color col, Texture icon = null, bool slider = true) 
        {
            var channel = inspectedMask.ToColorChannel();

            if (icon)
            {
                pegi.Draw(icon, alphaBlend: false);
            }

            icon = channel.GetIcon();

            var label = inspectedMask.ToText();
            var channelEnabled = mask.HasFlag(inspectedMask);

            if (InspectedPainter && InspectedPainter.meshEditing && MeshEditorManager.MeshTool == VertexColorTool.inst) {

                var mat = InspectedPainter.Material;
                if (mat)
                {
                    var tag = mat.Get(inspectedMask.ToString(), ShaderTags.VertexColorRole);

                    if (!tag.IsNullOrEmpty()) {

                        if (channelEnabled)
                            (tag + ":").PL().Nl();
                        else
                            label = tag + " ";
                    }
                }
            }

            

            if (channelEnabled ? pegi.Click(icon, label) : "{0} channel ignored".F(label).PL().ToggleIcon(ref channelEnabled, true))
                 mask ^= inspectedMask;

            if (slider && channelEnabled) {

                float val = channel.GetValueFrom(col);
                if (pegi.Edit(ref val, 0, 1).Nl())
                    channel.SetValueOn(ref col, val);
            }
        }

        public void ColorSliders() {

            if (InspectedPainter && !InspectedPainter.IsEditingThisMesh)
            {
                ColorSliders_PlaytimePainter();
                return;
            }

            pegi.Edit(ref Color).Nl();
            
            if (Painter.Data && !Painter.Data.showColorSliders)
                return;

            ChannelSlider(ColorMask.R, ref Color);  pegi.Nl();
            ChannelSlider(ColorMask.G, ref Color); pegi.Nl();
            ChannelSlider(ColorMask.B, ref Color); pegi.Nl();
            ChannelSlider(ColorMask.A, ref Color); pegi.Nl();
        }

        /*
        private static Texture GetSplashPrototypeTexture(Terrain terrain, int ind)
        {
            var l = terrain.terrainData.terrainLayers;

            if (l.Length > ind)
            {
                var sp = l[ind];
                return sp != null ? l[ind].diffuseTexture : null;
            }

            return null;
        }*/


        private bool ColorSliders_PlaytimePainter() {

           
            var painter = PainterComponent.inspected;
            var id = painter.TexMeta;
            var blitMode = GetBlitMode(id.Target);

            if (!blitMode.AllSetUp)
                return false;

            var changed = pegi.ChangeTrackStart();

           // if (Cfg.showColorSliders) {
           bool r = Painter.Data.showColorSliders || !mask.HasFlag(ColorMask.R);
           bool g = Painter.Data.showColorSliders || !mask.HasFlag(ColorMask.G);
           bool b = Painter.Data.showColorSliders || !mask.HasFlag(ColorMask.B);
           bool a = Painter.Data.showColorSliders || !mask.HasFlag(ColorMask.A);


            var slider = GetBlitMode(id.Target).ShowColorSliders;

            if (id.TargetIsRenderTexture() && id.RenderTexture)
            {
                if (r) ChannelSlider(ColorChanel.R, ref Color); pegi.Nl();
                if (g) ChannelSlider(ColorChanel.G, ref Color); pegi.Nl();
                if (b) ChannelSlider(ColorChanel.B, ref Color); pegi.Nl();

            }
            else
            {

                if (painter.IsEditingThisMesh || id==null || !id[TextureCfgFlags.TransparentLayer] || Color.a > 0)  {

                    var slider_copy = blitMode.UsingSourceTexture ?
                        (srcColorUsage != SourceTextureColorUsage.Unchanged)
                        :slider;

                    if (r) ChannelSlider(ColorMask.R, ref Color, slider: slider_copy); pegi.Nl();
                    if (g) ChannelSlider(ColorMask.G, ref Color, slider: slider_copy); pegi.Nl();
                    if (b) ChannelSlider(ColorMask.B, ref Color, slider: slider_copy); pegi.Nl();
                }
                    
                var gotAlpha = painter.meshEditing || id == null || id.Texture2D.TextureHasAlpha();

                if (id == null ||  (!painter.IsEditingThisMesh &&  (gotAlpha || id[TextureCfgFlags.PreserveTransparency]) && (!id[TextureCfgFlags.TransparentLayer] || !mask.HasFlag(ColorMask.A)))) {
                    if (!gotAlpha)
                        Icon.Warning.Draw("Texture as no alpha, clicking save will fix it");

                    if (a) ChannelSlider(ColorMask.A, ref Color, slider: slider); pegi.Nl();
                }
            }
            

            if (!painter.IsEditingThisMesh && id!=null && id[TextureCfgFlags.TransparentLayer]) {

                var erase = Color.a < 0.5f;

                "Erase".PL().ToggleIcon(ref erase).Nl();

                Color.a = erase ? 0 : 1;

            }

            return changed;
        }

        #endregion
        
        #region Encode Decode
        public override CfgEncoder Encode() => new CfgEncoder()
                .Add("dyn", brushDynamic, BrushDynamic.Base.all);
        
        public CfgEncoder EncodeStrokeFor(PainterComponent painter)
        {

            var id = painter.TexMeta;

            var rt = id.TargetIsRenderTexture();
            var target = id.Target;

            var mode = GetBlitMode(target);
            var type = GetBrushType(target);

            var worldSpace = rt && painter.Is3DBrush();

            var cody = new CfgEncoder()

            .Add(rt ? "typeGPU" : "typeCPU", BrushTypeIndex(target));

            if (worldSpace)
                cody.Add("size3D", brush3DRadius);
            else
                cody.Add("size2D", brush2DRadius / id.Width);


            cody.Add_Bool("useMask", useMask)
                .Add("modeCPU", _inCpuBlitMode)
                .Add("modeGPU", _inGpuBlitMode);



            if (useMask)
                cody.Add("mask", (int)mask);

            cody.Add("bc", Color);

            if (mode.UsingSourceTexture)
                cody.Add_IfNotZero("source", selectedSourceTexture);

            if (rt)
            {

                if ((mode.GetType() == typeof(BlitModes.Blur)))
                    cody.Add("blur", blurAmount);

                if (type.IsUsingDecals)
                {
                    cody.Add("decA", decalAngle)
                    .Add("decNo", selectedDecal);
                }

                if (useMask)
                {
                    cody.Add("Smask", selectedSourceMask)
                    .Add("maskTil", maskTiling)
                    .Add_Bool("maskFlip", flipMaskAlpha)
                    .Add("maskOff", maskOffset);
                }
            }

            cody.Add("hard", hardness)
                .Add("dSpeed", _dFlow);
            //.Add("Speed", speed);

            return cody;
        }

        public override void DecodeTag(string key, CfgData data)
        {

            switch (key)
            {
                case "typeGPU": data.ToInt(ref _inGpuBrushType); break;
                case "typeCPU": data.ToInt(ref _inCpuBrushType); break;
                case "size2D": brush2DRadius = data.ToFloat(); break;
                case "size3D": brush3DRadius = data.ToFloat(); break;

                case "useMask": useMask = data.ToBool(); break;

                case "mask": mask = (ColorMask)data.ToInt(); break;

                case "modeCPU":  data.ToInt(ref _inCpuBlitMode); break;
                case "modeGPU":  data.ToInt(ref _inGpuBlitMode); break;

                case "bc": Color = data.ToColor(); break;

                case "source":  data.ToInt(ref selectedSourceTexture); break;

                case "blur": blurAmount = data.ToFloat(); break;

                case "decA": decalAngle = data.ToFloat(); break;
                case "decNo":  data.ToInt(ref selectedDecal); break;

                case "Smask":  data.ToInt(ref selectedSourceMask); break;
                case "maskTil": maskTiling = data.ToFloat(); break;
                case "maskFlip": flipMaskAlpha = data.ToBool(); break;

                case "hard": hardness = data.ToFloat(); break;
                case "Speed": _dFlow.Value = data.ToFloat(); break;
                case "dSpeed": data.DecodeOverride(ref _dFlow); break;
                case "dyn": data.Decode(out brushDynamic, BrushDynamic.Base.all); break;

                case "maskOff": maskOffset = data.ToVector2(); break;
            }


        }
        #endregion

        public enum SourceTextureColorUsage { Unchanged = 0, MultiplyByBrushColor = 1, ReplaceWithBrushColor = 2 }

    }

    #region Dynamics

    public static class BrushDynamic {
        
        public abstract class Base : IPEGI, IGotClassTag, ICfg
        {

            public virtual void OnPrepareRay(PainterComponent p, Brush bc, ref Ray ray)
            {
            }

            #region Encode & Decode

            public abstract string ClassTag { get; }

             public static TaggedTypes.DerrivedList all = TaggedTypes<Base>.DerrivedList;
            public TaggedTypes.DerrivedList AllTypes => TaggedTypes<Base>.DerrivedList;//all;




            public virtual void DecodeTag(string key, CfgData data)
            {
                switch (key)
                {
                    case "t": data.ToInt(ref testValue); break;
                }
                
            }

            public virtual CfgEncoder Encode() => new CfgEncoder().Add("t", testValue);

            #endregion

            #region Inspector

            private int testValue = -1;

            public virtual void Inspect() { } /*
        {
            bool changed = false;

            "Test Value".PegiLabel().edit(60, ref testValue).nl();

            return changed;
        }*/
  


            #endregion
        }

        [TaggedTypes.Tag(CLASS_KEY, "None")]
        public class None : Base
        {
            private const string CLASS_KEY = "none";

            public override string ClassTag => CLASS_KEY;
        }

        [TaggedTypes.Tag(CLASS_KEY, "Jitter")]
        public class Jitter : Base
        {
            private const string CLASS_KEY = "gitter";
            public override string ClassTag => CLASS_KEY;

            private float jitterStrength = 0.1f;

            public override void OnPrepareRay(PainterComponent p, Brush bc, ref Ray rey)
            {
                // Quaternion * Vector3

                rey.direction = Vector3.Lerp(rey.direction, Random.rotation * rey.direction,
                    jitterStrength); //  Quaternion.Lerp(cameraConfiguration.rotation, , camShake);
            }

            #region Inspector
            
           public override void Inspect() =>
                // {
                //   var changed = false;

                "Strength".PL().Edit(ref jitterStrength, 0.00001f, 0.25f);

            //  return changed;

            //  }

            #endregion

            #region Encode & Decode

            public override void DecodeTag(string key, CfgData data)
            {
                switch (key)
                {
                    case "j": jitterStrength = data.ToFloat(); break;
                }
            }

            public override CfgEncoder Encode() => new CfgEncoder().Add("j", jitterStrength);

            #endregion

        }

        [TaggedTypes.Tag(CLASS_KEY, "Size from Speed")]
        public class SpeedToSize : Base
        {
            private const string CLASS_KEY = "sts";

            public override string ClassTag => CLASS_KEY;
        }
    }

    #endregion
}