﻿using System;
using QuizCanners.Inspect;
using PlaytimePainter.CameraModules;
using PlaytimePainter.MeshEditing;
using QuizCanners.Migration;
using QuizCanners.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PlaytimePainter {
    
    [Serializable]
    public class Brush : PainterClassCfg, IPEGI {

        public enum SourceTextureColorUsage { Unchanged = 0, MultiplyByBrushColor = 1, ReplaceWithBrushColor = 2 }
        
        #region Modes & Types

        internal bool IsCpu(TextureMeta textureMeta) => textureMeta!= null ? textureMeta.TargetIsTexture2D() : targetIsTex2D;
        
        [SerializeField] private int _inGpuBrushType;
        [SerializeField] private int _inCpuBrushType;
        private int BrushType(bool cpu) => cpu ? _inCpuBrushType : _inGpuBrushType;
        public void SetBrushType(bool cpu, BrushTypes.Base t) { if (cpu) _inCpuBrushType = t.index; else _inGpuBrushType = t.index; }
        internal BrushTypes.Base GetBrushType(TextureMeta textureMeta) => GetBrushType(IsCpu(textureMeta));
        public BrushTypes.Base GetBrushType(bool cpu) => BrushTypes.Base.AllTypes[BrushType(cpu)];

        [SerializeField] private int _inGpuBlitMode;
        [SerializeField] private int _inCpuBlitMode;

        public int BlitMode(bool cpu) => cpu ? _inCpuBlitMode : _inGpuBlitMode;
        public BlitModes.Base GetBlitMode(bool cpu) => BlitModes.Base.AllModes[BlitMode(cpu)];
        internal BlitModes.Base GetBlitMode(TextureMeta textureMeta) => BlitModes.Base.AllModes[BlitMode(IsCpu(textureMeta))];
        
        public void SetBlitMode(bool cpu, BlitModes.Base mode)
        {
            if (cpu) _inCpuBlitMode = mode.index;
            else _inGpuBlitMode = mode.index;
            
        }
        #endregion

        private void SetSupportedFor(bool cpu, bool rtDoubleBuffer) {
            if (!cpu) {
                if (rtDoubleBuffer) {
                    if (!GetBrushType(false).SupportedByRenderTexturePair) foreach (var t in BrushTypes.Base.AllTypes) { if (t.SupportedByRenderTexturePair) { SetBrushType(false, t); break; } }
                    if (!GetBlitMode(false).SupportedByRenderTexturePair) foreach (var t in BlitModes.Base.AllModes) { if (t.SupportedByRenderTexturePair) { SetBlitMode(false, t); break; } }
                } else {
                    if (!GetBrushType(false).SupportedBySingleBuffer) foreach (var t in BrushTypes.Base.AllTypes) { if (t.SupportedBySingleBuffer) { SetBrushType(false, t); break; } }
                    if (!GetBlitMode(false).SupportedBySingleBuffer) foreach (var t in BlitModes.Base.AllModes) { if (t.SupportedBySingleBuffer) { SetBlitMode(false, t); break; } }
                }
            } else
            {
                if (!GetBrushType(true).SupportedByTex2D) foreach (var t in BrushTypes.Base.AllTypes) { if (t.SupportedByTex2D) { SetBrushType(true, t); break; } }
                if (!GetBlitMode(true).SupportedByTex2D) foreach (var t in BlitModes.Base.AllModes) { if (t.SupportedByTex2D) { SetBlitMode(true, t); break; } }
            }
        }
        
        public bool targetIsTex2D;
        
        #region Copy texture
        public int selectedSourceTexture;

        public bool clampSourceTexture;

        public bool ignoreSrcTextureTransparency;

        public SourceTextureColorUsage srcColorUsage = SourceTextureColorUsage.Unchanged;

        #endregion

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

        [NonSerialized] public bool previewDirty = false;
        
        #region Decal
        public int selectedDecal;
        public float decalAngle;
        public BrushTypes.Decal.RotationMethod rotationMethod;
        public bool decalContentious;
        public float decalAngleModifier;
        #endregion
        
        #region Brush Dynamics
     
        public bool showBrushDynamics;
        public ElementData brushDynamicsConfigs = new ElementData();

        public BrushDynamic.Base brushDynamic = new BrushDynamic.None();
        #endregion

        #region Brush Parameters
        public float hardness = 256;
        public float blurAmount = 1;
        public float brush3DRadius = 16;
        public float brush2DRadius = 16;
        public bool useAlphaBuffer;

        public float alphaLimitForAlphaBuffer = 0.5f;

        public bool worldSpaceBrushPixelJitter;

        public float Size(bool worldSpace) => Mathf.Max(0.01f, worldSpace ? brush3DRadius : brush2DRadius);
        public Color Color;

        public bool Is3DBrush(TextureMeta texture = null)
        {
            var cpu = IsCpu(texture);
            return GetBrushType(cpu).IsAWorldSpaceBrush;
        }

        [SerializeField] public QcUtils.DynamicRangeFloat _dFlow = new QcUtils.DynamicRangeFloat(0.1f, 4.5f, 3f );

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
        
        public void Paint(PaintCommand.UV command)
        {
            var imgData = command.TextureData;

            var cpu = command.TextureData.TargetIsTexture2D();
            var brushType = GetBrushType(cpu);
            var blitMode = GetBlitMode(cpu);
            var isWorldSpace = command.Is3DBrush;

            var painterCommand = command as PaintCommand.ForPainterComponent;

            PlaytimePainter painter = painterCommand?.painter;

            blitMode.PrePaint(command);

            if (cpu)
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

                    if (!imgData.RenderTexture && !TexMGMT.materialsUsingRenderTexture.Contains(materialData))
                    {
                        TexMGMT.ChangeBufferTarget(imgData, materialData, painter.GetMaterialTextureProperty(), painter);
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

                    if (!painter.terrain || brushType.SupportedForTerrainRt)
                    {

                        foreach (var module in imgData.Modules)
                            module.OnPainting(painter);

                        if (!rendered)
                        {
                            if (isWorldSpace)
                                brushType.PaintRenderTextureInWorldSpace(command as PaintCommand.WorldSpaceBase);
                            else 
                                brushType.PaintRenderTextureUvSpace(command);
                        }
                    }
                }
                else
                {
                    if (isWorldSpace)
                        brushType.PaintRenderTextureInWorldSpace(command as PaintCommand.WorldSpaceBase);
                    else
                        brushType.PaintRenderTextureUvSpace(command);
                }
            }
        }


        #region Inspector

        public bool showingSize = true;
        public static bool showAdvanced;
        public static Brush _inspectedBrush;
        public static bool InspectedIsCpuBrush => PlaytimePainter.inspected ? InspectedImageMeta.TargetIsTexture2D() : _inspectedBrush.targetIsTex2D;
     
        public bool Mode_Type_PEGI()
        {
            var changes = pegi.ChangeTrackStart();


            var p = PlaytimePainter.inspected;
            var id = p ? p.TexMeta : null;

            IPainterManagerModuleBrush cameraModule = null;

            foreach (var b in CameraModuleBase.BrushPlugins)
                if (b.IsEnabledFor(p, id, this)) {
                    cameraModule = b;
                    break;
                }

            BrushTypes.Base.AllTypes.ClampIndexToCount(ref _inCpuBrushType);
            BrushTypes.Base.AllTypes.ClampIndexToCount(ref _inGpuBrushType);
            
            _inspectedBrush = this;

            var cpu = p ? id.TargetIsTexture2D() : targetIsTex2D;

            var blitMode = GetBlitMode(cpu);
            var brushType = GetBrushType(cpu);

            pegi.nl();

            MsgPainter.BlitMode.Write("How final color will be calculated");

            if (pegi.select(ref blitMode, BlitModes.Base.AllModes)) 
                SetBlitMode(cpu, blitMode);

            if (DocsEnabled && blitMode != null)
                pegi.FullWindow.DocumentationClickOpen(blitMode.ToolTip, toolTip: "About {0} mode".F(blitMode.GetNameForInspector()));

            if (showAdvanced)
                pegi.nl();

            pegi.toggle(ref showAdvanced, icon.FoldedOut, icon.Create, "Brush Options", 25);

            if (showAdvanced)
                "Advanced options: (if any)".write();
            

            pegi.nl();

            if (!cpu)  {
               
                MsgPainter.BrushType.Write();
                pegi.select_Index(ref _inGpuBrushType, BrushTypes.Base.AllTypes);

                if (DocsEnabled && brushType != null)
                    pegi.FullWindow.DocumentationClickOpen(brushType.ToolTip, toolTip: "About {0} brush type".F(brushType.GetNameForInspector()));

                if (!brushType.ShowInInspectorDropdown())
                {
                    pegi.nl();
                    "Selected brush type is not supported in context of this Painter".writeWarning();
                    
                }

                pegi.nl();

            }
            
             cameraModule?.BrushConfigPEGI(this);

              if (p)
              {
                  foreach (var pl in p.Modules)
                      pl.BrushConfigPEGI().nl();
                        

                  if (id != null)
                      foreach (var mod in id.Modules)
                          mod.BrushConfigPEGI(p).nl();
              }

            brushType.Nested_Inspect().nl();

            if (blitMode.AllSetUp) {

                if (blitMode.UsingSourceTexture) {

                    "Texture Color".editEnum(120, ref srcColorUsage).nl();

                    if (InspectAdvanced) {
                        "Clamp".toggleIcon(ref clampSourceTexture).nl();
                        "Multiply by Alpha".toggleIcon(ref ignoreSrcTextureTransparency);
                        pegi.FullWindow.DocumentationClickOpen("Ignore transparency of the source texture. To only paint parts of the texture which are visible").nl();
                    }
                }
                
                if (!cpu && brushType.SupportsAlphaBufferPainting && blitMode.SupportsAlphaBufferPainting && (useAlphaBuffer || InspectAdvanced)) {

                    "Alpha Buffer".toggleIcon(ref useAlphaBuffer, true);

                    if (useAlphaBuffer)
                    {
                        var txt = MsgPainter.Opacity.GetText();

                        txt.edit(
                            "This is the kind of alpha you see in standard painting software. But it is only available when using Alpha Buffer",
                            txt.ApproximateLength(), ref alphaLimitForAlphaBuffer, 0.01f, 1f);

                        if (p && p.NotUsingPreview)
                            MsgPainter.PreviewRecommended.DocumentationWarning();

                    }

                    MsgPainter.AlphaBufferBlit.DocumentationClick().nl();

                    pegi.nl();
                }
            }


            if (blitMode.ShowInInspectorDropdown())
            {
                blitMode.InspectWithModule().nl();
                showingSize = true;
            }
            
            _inspectedBrush = null;

            return changes;
        }

        public bool Targets_PEGI()
        {
            var changed = pegi.ChangeTrackStart();

            if ((targetIsTex2D ? icon.CPU : icon.GPU).Click(
                targetIsTex2D ? "Render Texture Config" : "Texture2D Config", 45))
            {
                targetIsTex2D = !targetIsTex2D;
                SetSupportedFor(targetIsTex2D, true);
            }

            var smooth = GetBrushType(targetIsTex2D) != BrushTypes.Pixel.Inst;

            if (targetIsTex2D && 
                pegi.toggle(ref smooth, icon.Round, icon.Square, "Smooth/Pixels Brush", 45))
                SetBrushType(targetIsTex2D, smooth ? BrushTypes.Normal.Inst : BrushTypes.Pixel.Inst.AsBase);
            

            return changed;
        }
        
        public virtual void Inspect() {

            var p = PlaytimePainter.inspected;

            if (!p) {
                "No Painter Detected".nl();
                return;
            }
            
            var id = p.TexMeta;

            var cpuBlit = id.Target == TexTarget.Texture2D;
            
            if (id.Texture2D)
            {
                if ((cpuBlit ? icon.CPU : icon.GPU).Click( cpuBlit ? "Switch to Render Texture" : "Switch to Texture2D" ,45))
                {
                    cpuBlit = !cpuBlit;

                    if (!cpuBlit && !PainterCamera.GotBuffers)
                        PlaytimePainter_RenderTextureBuffersManager.RefreshPaintingBuffers();

                    p.UpdateOrSetTexTarget(cpuBlit ? TexTarget.Texture2D : TexTarget.RenderTexture);
                    SetSupportedFor(cpuBlit, !id.RenderTexture);
                }
            }
            
            var smooth = BrushType(cpuBlit) != BrushTypes.Pixel.Inst.index;
            
            if (cpuBlit) 
            {
                if (pegi.toggle(ref smooth, icon.Round, icon.Square, "Smooth/Pixels Brush", 45))
                    SetBrushType(cpu: true, smooth ? BrushTypes.Normal.Inst : BrushTypes.Pixel.Inst.AsBase);
            }

            pegi.nl();

            if (showBrushDynamics) {
                if ("Brush Dynamic".selectType( 90, ref brushDynamic, brushDynamicsConfigs).nl())
                    brushDynamic?.Nested_Inspect().nl();
            }
            else if (brushDynamic.GetType() != typeof(BrushDynamic.None))
                    brushDynamic = (BrushDynamic.None)Activator.CreateInstance(typeof(BrushDynamic.None));
            
            if (Mode_Type_PEGI() && GetBrushType(cpuBlit) == BrushTypes.Decal.Inst)
                    MaskSet(ColorMask.A, true);

            if (p.terrain) {

                if (p.TexMeta != null && p.IsTerrainHeightTexture && p.NotUsingPreview)
                    "Preview Shader is needed to see changes to terrain height.".writeWarning();

                pegi.nl();

                if (p.terrain && "Update Terrain".Click("Will Set Terrain texture as global shader values.").nl())
                {
                    if (!p.IsUsingPreview)
                    {
                        p.UnityTerrain_To_HeightTexture();
                    }
                    p.UpdateModules();
                }
            }
        }

        public bool ChannelSlider(ColorChanel chan, ref Color col)
        {
            chan.GetIcon().draw();
            float val = chan.GetValueFrom(col);
            if (pegi.edit(ref val, 0, 1).nl())
            {
                chan.SetValueOn(ref col, val);
                return true;
            }

            return false;
        }

        public bool ChannelSlider(ColorMask inspectedMask, ref Color col, Texture icon = null, bool slider = true) {

            var changed = pegi.ChangeTrackStart();

            var channel = inspectedMask.ToColorChannel();

            if (icon)
            {
                icon.draw(alphaBlend: false);
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
                            (tag + ":").nl();
                        else
                            label = tag + " ";
                    }
                }
            }

            

            if (channelEnabled ? icon.Click(label) : "{0} channel ignored".F(label).toggleIcon(ref channelEnabled, true))
                 mask ^= inspectedMask;

            if (slider && channelEnabled) {

                float val = channel.GetValueFrom(col);
                if (pegi.edit(ref val, 0, 1).nl())
                    channel.SetValueOn(ref col, val);
            }

            return changed;
        }

        public bool ColorSliders() {

            if (InspectedPainter && !InspectedPainter.IsEditingThisMesh)
                return ColorSliders_PlaytimePainter();

            var changed = false;
            
            pegi.edit(ref Color).nl();
            
            if (!Cfg.showColorSliders)
                return changed;

            ChannelSlider(ColorMask.R, ref Color).nl();
            ChannelSlider(ColorMask.G, ref Color).nl();
            ChannelSlider(ColorMask.B, ref Color).nl();
            ChannelSlider(ColorMask.A, ref Color).nl();

            return changed;
        }

        private static Texture GetSplashPrototypeTexture(Terrain terrain, int ind)
        {
            var l = terrain.terrainData.terrainLayers;

            if (l.Length > ind)
            {
                var sp = l[ind];
                return sp != null ? l[ind].diffuseTexture : null;
            }

            return null;
        }


        private bool ColorSliders_PlaytimePainter() {

           
            var painter = PlaytimePainter.inspected;
            var id = painter.TexMeta;
            var cpu = id.TargetIsTexture2D();
            var blitMode = GetBlitMode(cpu);

            if (!blitMode.AllSetUp)
                return false;

            var changed = pegi.ChangeTrackStart();

           // if (Cfg.showColorSliders) {
           bool r = Cfg.showColorSliders || !mask.HasFlag(ColorMask.R);
           bool g = Cfg.showColorSliders || !mask.HasFlag(ColorMask.G);
           bool b = Cfg.showColorSliders || !mask.HasFlag(ColorMask.B);
           bool a = Cfg.showColorSliders || !mask.HasFlag(ColorMask.A);


            var slider = GetBlitMode(cpu).ShowColorSliders;

            if (painter && painter.IsTerrainHeightTexture)
            {
                ChannelSlider(ColorMask.A, ref Color);
            }
            else if (painter && painter.IsTerrainControlTexture)
            {
                if (r) ChannelSlider(ColorMask.R, ref Color, GetSplashPrototypeTexture(painter.terrain, 0), slider)
                    .nl();
                if (g) ChannelSlider(ColorMask.G, ref Color, GetSplashPrototypeTexture(painter.terrain,1), slider)
                    .nl();
                if (b) ChannelSlider(ColorMask.B, ref Color, GetSplashPrototypeTexture(painter.terrain, 2), slider)
                    .nl();
                if (a) ChannelSlider(ColorMask.A, ref Color, GetSplashPrototypeTexture(painter.terrain, 3), slider)
                    .nl();
            }
            else
            {
               
                if (id.TargetIsRenderTexture() && id.RenderTexture)
                {
                    if (r) ChannelSlider(ColorChanel.R, ref Color).nl();
                    if (g) ChannelSlider(ColorChanel.G, ref Color).nl();
                    if (b) ChannelSlider(ColorChanel.B, ref Color).nl();

                }
                else
                {

                    if (painter.IsEditingThisMesh || id==null || !id.IsATransparentLayer || Color.a > 0)  {

                        var slider_copy = blitMode.UsingSourceTexture ?
                            (srcColorUsage != SourceTextureColorUsage.Unchanged)
                            :slider;

                        if (r) ChannelSlider(ColorMask.R, ref Color, slider: slider_copy).nl();
                        if (g) ChannelSlider(ColorMask.G, ref Color, slider: slider_copy).nl();
                        if (b) ChannelSlider(ColorMask.B, ref Color, slider: slider_copy).nl();
                    }
                    
                    var gotAlpha = painter.meshEditing || id == null || id.Texture2D.TextureHasAlpha();

                    if (id == null ||  (!painter.IsEditingThisMesh &&  (gotAlpha || id.PreserveTransparency) && (!id.IsATransparentLayer || !mask.HasFlag(ColorMask.A)))) {
                        if (!gotAlpha)
                            icon.Warning.draw("Texture as no alpha, clicking save will fix it");

                        if (a) ChannelSlider(ColorMask.A, ref Color, slider: slider).nl();
                    }
                }
            }

            if (!painter.IsEditingThisMesh && id!=null && id.IsATransparentLayer) {

                var erase = Color.a < 0.5f;

                "Erase".toggleIcon(ref erase).nl();

                Color.a = erase ? 0 : 1;

            }

            return changed;
        }

        #endregion
        
        #region Encode Decode
        public override CfgEncoder Encode() => new CfgEncoder()
                .Add("dyn", brushDynamic, BrushDynamic.Base.all);
        
        public CfgEncoder EncodeStrokeFor(PlaytimePainter painter)
        {

            var id = painter.TexMeta;

            var rt = id.TargetIsRenderTexture();

            var mode = GetBlitMode(!rt);
            var type = GetBrushType(!rt);

            var worldSpace = rt && painter.Is3DBrush();

            var cody = new CfgEncoder()

            .Add(rt ? "typeGPU" : "typeCPU", BrushType(!rt));

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

        public override void Decode(string key, CfgData data)
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
                case "dSpeed": _dFlow.Decode(data); break;
                case "dyn": data.Decode(out brushDynamic, BrushDynamic.Base.all); break;

                case "maskOff": maskOffset = data.ToVector2(); break;
            }


        }
        #endregion

    }

    #region Dynamics

    public static class BrushDynamic {
        
        public abstract class Base : IPEGI, IGotClassTag, ICfg
        {

            public virtual void OnPrepareRay(PlaytimePainter p, Brush bc, ref Ray ray)
            {
            }

            #region Encode & Decode

            public abstract string ClassTag { get; }

            public static TaggedTypesCfg all = new TaggedTypesCfg(typeof(Base));
            public TaggedTypesCfg AllTypes => all;
            


            public virtual void Decode(string key, CfgData data)
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

            "Test Value".edit(60, ref testValue).nl();

            return changed;
        }*/
  


            #endregion
        }

        [TaggedType(CLASS_KEY, "None")]
        public class None : Base
        {
            private const string CLASS_KEY = "none";

            public override string ClassTag => CLASS_KEY;
        }

        [TaggedType(CLASS_KEY, "Jitter")]
        public class Jitter : Base
        {
            private const string CLASS_KEY = "gitter";
            public override string ClassTag => CLASS_KEY;

            private float jitterStrength = 0.1f;

            public override void OnPrepareRay(PlaytimePainter p, Brush bc, ref Ray rey)
            {
                // Quaternion * Vector3

                rey.direction = Vector3.Lerp(rey.direction, Random.rotation * rey.direction,
                    jitterStrength); //  Quaternion.Lerp(cameraConfiguration.rotation, , camShake);
            }

            #region Inspector
            
           public override void Inspect() =>
                // {
                //   var changed = false;

                "Strength".edit(ref jitterStrength, 0.00001f, 0.25f);

            //  return changed;

            //  }

            #endregion

            #region Encode & Decode

            public override void Decode(string key, CfgData data)
            {
                switch (key)
                {
                    case "j": jitterStrength = data.ToFloat(); break;
                }
            }

            public override CfgEncoder Encode() => new CfgEncoder().Add("j", jitterStrength);

            #endregion

        }

        [TaggedType(CLASS_KEY, "Size from Speed")]
        public class SpeedToSize : Base
        {
            private const string CLASS_KEY = "sts";

            public override string ClassTag => CLASS_KEY;
        }
    }

    #endregion
}