using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.EditorTools;
#endif
using System;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace PlaytimePainter {

    public static class BrushExtensions {
        public static bool HasFlag(this BrushMask mask, int flag) => (mask & (BrushMask)(Mathf.Pow(2, flag))) != 0;

        public static bool HasFlag(this BrushMask mask, BrushMask flag) => (mask & flag) != 0;
    }

    public enum DecalRotationMethod { Constant, Random, FaceStrokeDirection }

    public enum SourceTextureColorUsage { Unchanged = 0, MultiplyByBrushColor = 1, ReplaceWithBrushColor = 2}

    [Serializable]
    public class BrushConfig : PainterSystemCfg, IPEGI {

        #region Modes & Types

        public bool IsCpu(PlaytimePainter painter) => painter ? painter.ImgMeta.TargetIsTexture2D() : targetIsTex2D;
        
        [SerializeField] private int _inGpuBrushType;
        [SerializeField] private int _inCpuBrushType;
        private int _brushType(bool cpu) => cpu ? _inCpuBrushType : _inGpuBrushType;
        public void SetBrushType(bool cpu, BrushType t) { if (cpu) _inCpuBrushType = t.index; else _inGpuBrushType = t.index; }
        public BrushType GetBrushType(PlaytimePainter painter) => GetBrushType(IsCpu(painter));
        public BrushType GetBrushType(bool cpu) => BrushType.AllTypes[_brushType(cpu)];

        [SerializeField] private int _inGpuBlitMode;
        [SerializeField] private int _inCpuBlitMode;

        public int BlitMode(bool cpu) => cpu ? _inCpuBlitMode : _inGpuBlitMode;
        public BlitMode GetBlitMode(bool cpu) => global::PlaytimePainter.BlitMode.AllModes[BlitMode(cpu)];
        public BlitMode GetBlitMode(PlaytimePainter painter) => global::PlaytimePainter.BlitMode.AllModes[BlitMode(IsCpu(painter))];
        
        public void SetBlitMode(bool cpu, BlitMode mode)
        {
            if (cpu) _inCpuBlitMode = mode.index;
            else _inGpuBlitMode = mode.index;
            
        }
        #endregion

        private void SetSupportedFor(bool cpu, bool rtDoubleBuffer) {
            if (!cpu) {
                if (rtDoubleBuffer) {
                    if (!GetBrushType(false).SupportedByRenderTexturePair) foreach (var t in BrushType.AllTypes) { if (t.SupportedByRenderTexturePair) { SetBrushType(false, t); break; } }
                    if (!GetBlitMode(false).SupportedByRenderTexturePair) foreach (var t in global::PlaytimePainter.BlitMode.AllModes) { if (t.SupportedByRenderTexturePair) { SetBlitMode(false, t); break; } }
                } else {
                    if (!GetBrushType(false).SupportedBySingleBuffer) foreach (var t in BrushType.AllTypes) { if (t.SupportedBySingleBuffer) { SetBrushType(false, t); break; } }
                    if (!GetBlitMode(false).SupportedBySingleBuffer) foreach (var t in global::PlaytimePainter.BlitMode.AllModes) { if (t.SupportedBySingleBuffer) { SetBlitMode(false, t); break; } }
                }
            } else
            {
                if (!GetBrushType(true).SupportedByTex2D) foreach (var t in BrushType.AllTypes) { if (t.SupportedByTex2D) { SetBrushType(true, t); break; } }
                if (!GetBlitMode(true).SupportedByTex2D) foreach (var t in global::PlaytimePainter.BlitMode.AllModes) { if (t.SupportedByTex2D) { SetBlitMode(true, t); break; } }
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
        
        public void MaskToggle(BrushMask flag) =>
            mask ^= flag;

        public void MaskSet(BrushMask flag, bool to)
        {
            if (to)
                mask |= flag;
            else
                mask &= ~flag;
        }

        public BrushMask mask;

        public bool PaintingAllChannels => BrushExtensions.HasFlag(mask, BrushMask.R) && BrushExtensions.HasFlag(mask, BrushMask.G) && BrushExtensions.HasFlag(mask, BrushMask.B) && BrushExtensions.HasFlag(mask, BrushMask.A);

        public bool PaintingRGB => BrushExtensions.HasFlag(mask, BrushMask.R) && BrushExtensions.HasFlag(mask, BrushMask.G) && BrushExtensions.HasFlag(mask, BrushMask.B) && (!BrushExtensions.HasFlag(mask, BrushMask.A));


        #endregion

        [NonSerialized] public bool previewDirty = false;
        
        #region Decal
        public int selectedDecal;
        public float decalAngle;
        public DecalRotationMethod decalRotationMethod;
        public bool decalContentious;
        public float decalAngleModifier;
        #endregion
        
        #region Brush Dynamics
     
        public bool showBrushDynamics;
        public ElementData brushDynamicsConfigs = new ElementData();

        public BrushDynamic brushDynamic = new BrushDynamic_None();
        #endregion

        #region Brush Parameters
        public float hardness = 256;
        public float blurAmount = 1;
        public float brush3DRadius = 16;
        public float brush2DRadius = 16;
        public bool useAlphaBuffer;
        

        public float alphaLimitForAlphaBuffer = 1;

        public bool worldSpaceBrushPixelJitter;

        public float Size(bool worldSpace) => (worldSpace ? brush3DRadius : brush2DRadius);
        public LinearColor colorLinear;

        public Color Color { get { return colorLinear.ToGamma(); } set { colorLinear.From(value); } }
  
        public virtual bool IsA3DBrush(PlaytimePainter painter)
        {
            var overrideOther = false;

            var isA3D = false;

            if (painter)
                foreach (var pl in PainterSystemManagerModuleBase.BrushPlugins)
                {
                    isA3D = pl.IsA3DBrush(painter, this, ref overrideOther);
                    if (overrideOther) break;
                }

            if (!overrideOther)
            {
                var cpu = IsCpu(painter);
                isA3D = GetBrushType(cpu).IsAWorldSpaceBrush;
            }

            return isA3D;
        }

        [SerializeField] public DynamicRangeFloat _dSpeed = new DynamicRangeFloat(0.1f, 4.5f, 3f );

        public float Speed
        {
            get { return _dSpeed.value * _dSpeed.value; }
            set { _dSpeed.value = Mathf.Sqrt(value); }
        }

        #endregion
        
        public BrushConfig() {
            colorLinear = new LinearColor(Color.green);
            mask = new BrushMask();
            mask |= BrushMask.R | BrushMask.G | BrushMask.B;
        }
        
        public PlaytimePainter Paint(StrokeVector stroke, PlaytimePainter painter) {

            var imgData = painter.ImgMeta;

            if (imgData == null) {
                painter.InitIfNotInitialized();
                imgData = painter.ImgMeta;
                if (imgData == null)
                    return painter;
            }

            var cpu = imgData.TargetIsTexture2D();
            var brushType = GetBrushType(cpu);
            var blitMode = GetBlitMode(cpu);

            blitMode.PrePaint(painter, this, stroke);

            if (cpu) {
                painter.RecordingMgmt();
                brushType.PaintToTexture2D(painter, this, stroke);
            } else {

                var materialData = painter.MatDta;

                if (!imgData.renderTexture  && !TexMGMT.materialsUsingRenderTexture.Contains(materialData)) {
                    TexMGMT.ChangeBufferTarget(imgData, materialData, painter.GetMaterialTextureProperty, painter);
                    painter.SetTextureOnMaterial(imgData);
                }

                var rendered = false;

                foreach (var pl in PainterSystemManagerModuleBase.BrushPlugins)
                    if (pl.IsEnabledFor(painter, imgData, this)) { 
                        pl.PaintRenderTexture(stroke, imgData, this, painter);
                        rendered = true;
                        break;
                    }

                if (!painter.terrain || brushType.SupportedForTerrainRt) {

                    painter.RecordingMgmt();

                    if (!rendered)
                        brushType.PaintRenderTexture(painter, this, stroke);
                }
            }

            return painter;
        }

        #region Inspector

        public bool showingSize = true;
        public static bool showAdvanced = false;
        public static BrushConfig _inspectedBrush;
        public static bool InspectedIsCpuBrush => PlaytimePainter.inspected ? InspectedImageMeta.TargetIsTexture2D() : _inspectedBrush.targetIsTex2D;
        #if PEGI
        public bool Mode_Type_PEGI()
        {
            var p = PlaytimePainter.inspected;
            var id = p ? p.ImgMeta : null;

            IPainterManagerModuleBrush module = null;

            foreach (var b in PainterSystemManagerModuleBase.BrushPlugins)
                if (b.IsEnabledFor(p, id, this))
                {
                    module = b;
                    break;
                }

            BrushType.AllTypes.ClampIndexToCount(ref _inCpuBrushType);
            BrushType.AllTypes.ClampIndexToCount(ref _inGpuBrushType);
            
            _inspectedBrush = this;
            var changed = false;
            var cpu = p ? id.TargetIsTexture2D() : targetIsTex2D;

            var blitMode = GetBlitMode(cpu);
            var brushType = GetBrushType(cpu);

            pegi.newLine();

            MsgPainter.BlitMode.Write("How final color will be calculated");

            if (pegi.select(ref blitMode, global::PlaytimePainter.BlitMode.AllModes).changes(ref changed)) 
                SetBlitMode(cpu, blitMode);

            if (docsEnabled && blitMode != null && pegi.DocumentationClick("About {0} mode".F(blitMode.NameForDisplayPEGI)))
                pegi.FullWindwDocumentationOpen(blitMode.ToolTip);

            if (showAdvanced)
                pegi.nl();

            pegi.toggle(ref showAdvanced, icon.FoldedOut, icon.Create, "Brush Options", 25);

            if (showAdvanced)
                "Advanced options: (if any)".write();

            pegi.nl();

            if (!cpu)  {
               
                MsgPainter.BrushType.Write();
                pegi.select_Index(ref _inGpuBrushType, BrushType.AllTypes).changes(ref changed);
                
                if (docsEnabled && brushType != null && pegi.DocumentationClick("About {0} brush type".F(brushType.NameForDisplayPEGI)))
                    pegi.FullWindwDocumentationOpen(brushType.ToolTip);

                if (!brushType.ShowInDropdown())
                {
                    pegi.nl();
                    "Selected brush type is not supported in context of this Painter".writeWarning();
                    
                }

                pegi.nl();

            }
            
            var overrideBlitModePegi = false;


             module?.BrushConfigPEGI(ref overrideBlitModePegi, this);

            //foreach (var b in PainterSystemManagerModuleBase.BrushPlugins)
              //  b.BrushConfigPEGI(ref overrideBlitModePegi, this).nl(ref changed);

            if (p)
                foreach (var pl in p.Modules)
                    if (pl.BrushConfigPEGI().nl(ref changed))
                        pl.SetToDirty_Obj();


            brushType.Inspect().nl(ref changed);

            if (blitMode.AllSetUp) {

                if (blitMode.UsingSourceTexture) {

                    "Texture Color".editEnum(120, ref srcColorUsage).nl(ref changed);

                    if (InspectAdvanced) {
                        "Clamp".toggleIcon(ref clampSourceTexture).nl(ref changed);
                        "Ignore Transparency".toggleIcon(ref ignoreSrcTextureTransparency).changes(ref changed);
                        "Ignore transparency of the source texture. Otherwise the tool will only paint parts of the texture which are not transparent".fullWindowDocumentationClickOpen().nl();
                    }
                }

              

                if (!cpu && brushType.SupportsAlphaBufferPainting && blitMode.SupportsAlphaBufferPainting && (useAlphaBuffer || InspectAdvanced)) {

                    "Alpha Buffer".toggleIcon(ref useAlphaBuffer, true).changes(ref changed);

                    if (useAlphaBuffer)
                    {
                        var txt = MsgPainter.Opacity.GetText();

                        txt.edit(
                            "This is the kind of alpha you see in standard painting software. But it is only available when using Alpha Buffer",
                            pegi.ApproximateLengthUnsafe(txt), ref alphaLimitForAlphaBuffer, 0.01f, 1f).changes(ref changed);

                        if (p && p.NotUsingPreview)
                            MsgPainter.PreviewRecommended.DocumentationWarning();

                    }

                    MsgPainter.AlphaBufferBlit.DocumentationClick().nl();

                    pegi.nl();
                }
            }

            if (!overrideBlitModePegi && blitMode.ShowInDropdown())
            {
                blitMode.Inspect(module).nl(ref changed);
                showingSize = true;
            }

            _inspectedBrush = null;

            return changed;
        }

        public bool Targets_PEGI()
        {
            var changed = false;

            if ((targetIsTex2D ? icon.CPU : icon.GPU).Click(
                targetIsTex2D ? "Render Texture Config" : "Texture2D Config", ref changed ,45))
            {
                targetIsTex2D = !targetIsTex2D;
                SetSupportedFor(targetIsTex2D, true);
            }

            var smooth = GetBrushType(targetIsTex2D) != BrushTypePixel.Inst;

            if (targetIsTex2D && 
                pegi.toggle(ref smooth, icon.Round.GetIcon(), icon.Square.GetIcon(), "Smooth/Pixels Brush", 45).changes(ref changed))
                SetBrushType(targetIsTex2D, smooth ? BrushTypeNormal.Inst : (BrushType)BrushTypePixel.Inst);
            

            return changed;
        }
        
        public virtual bool Inspect() {

            var p = PlaytimePainter.inspected;

            if (!p) {
                "No Painter Detected".nl();
                return false;
            }

            pegi.nl();

            if (p.skinnedMeshRenderer) {
                if ("Update Collider from Skinned Mesh".Click())
                    p.UpdateMeshCollider();

                if (docsEnabled && pegi.DocumentationClick("Why Update Collider from skinned mesh?"))
                    pegi.FullWindwDocumentationOpen(
                        ("To paint an object a collision detection is needed. Mesh Collider is not being animated. To paint it, update Mesh Collider with Update Collider button." +
                        " For ingame painting it is preferable to use simple colliders like Speheres to avoid per frame updates for collider mesh."
                        ));

                pegi.nl();
            }


            var id = p.ImgMeta;

            var changed = false;
            var cpuBlit = id.destination == TexTarget.Texture2D;
            
            p.PreviewShaderToggleInspect().changes(ref changed);

            if (!PainterCamera.GotBuffers && icon.Refresh.Click("Refresh Main Camera Buffers"))
                RenderTextureBuffersManager.RefreshPaintingBuffers();
            
            if ((PainterCamera.GotBuffers || id.renderTexture) && id.texture2D)
            {
                if ((cpuBlit ? icon.CPU : icon.GPU).Click(
                    cpuBlit ? "Switch to Render Texture" : "Switch to Texture2D", ref changed ,45))
                {
                    p.UpdateOrSetTexTarget(cpuBlit ? TexTarget.RenderTexture : TexTarget.Texture2D);
                    SetSupportedFor(cpuBlit, !id.renderTexture);
                }
            }
            
            if (cpuBlit) {
                var smooth = _brushType(cpuBlit) != BrushTypePixel.Inst.index;

                if (pegi.toggle(ref smooth, icon.Round, icon.Square, "Smooth/Pixels Brush", 45).changes(ref changed))
                    SetBrushType(cpuBlit, smooth ? BrushTypeNormal.Inst : (BrushType)BrushTypePixel.Inst);
            }

            pegi.nl();

            if (showBrushDynamics)
            {
                if ("Brush Dynamic".selectType( 90, ref brushDynamic, brushDynamicsConfigs, true).nl(ref changed))
                    brushDynamic?.Nested_Inspect().nl(ref changed);
            }
            else
                brushDynamic.AllTypes.Replace_IfDifferent(ref brushDynamic, typeof(BrushDynamic_None));

#if UNITY_EDITOR

#if !UNITY_2019_1_OR_NEWER
            if ( Tools.current != Tool.None ) {
                MsgPainter.LockToolToUseTransform.GetText().writeWarning();
                if (MsgPainter.HideTransformTool.GetText().Click().nl())
                    UnityUtils.HideUnityTool();
            }
#endif

#endif


            if (Mode_Type_PEGI().changes(ref changed) && GetBrushType(cpuBlit) == BrushTypeDecal.Inst)
                    MaskSet(BrushMask.A, true);

            if (p.terrain) {

                if (p.ImgMeta != null && p.IsTerrainHeightTexture && p.NotUsingPreview)
                    "Preview Shader is needed to see changes to terrain height.".writeWarning();

                pegi.nl();

                if (p.terrain && "Update Terrain".Click("Will Set Terrain texture as global shader values.").nl())
                    p.UpdateShaderGlobals();

            }

            return changed;
        }

        public bool ChannelSlider(BrushMask m, ref float channel)
        {
            m.GetIcon().write();
            return pegi.edit(ref channel, 0, 1).nl();
        }

        public bool ChannelSlider(BrushMask m, ref float chanel, Texture icon, bool slider) {

            var changed = false;

            if (!icon)
                icon = m.GetIcon();

            var letter = m.ToText();
            var maskVal = mask.HasFlag(m);

            if (InspectedPainter && InspectedPainter.meshEditing && MeshMGMT.MeshTool == VertexColorTool.inst) {

                var mat = InspectedPainter.Material;
                if (mat)
                {
                    var tag = mat.Get(m.ToString(), ShaderTags.VertexColorRole);

                    if (!tag.IsNullOrEmpty()) {

                        if (maskVal)
                            (tag + ":").nl();
                        else
                            letter = tag + " ";
                    }
                }
            }

            if (maskVal ? icon.Click(letter) : "{0} channel ignored".F(letter).toggleIcon(ref maskVal, true).changes(ref changed)) 
                MaskToggle(m);
            
            if (slider && mask.HasFlag(m))
                pegi.edit(ref chanel, 0, 1).nl(ref changed);
            
            return changed;
        }

        public bool ColorSliders() {

            if (InspectedPainter && !InspectedPainter.IsEditingThisMesh)
                return ColorSliders_PlaytimePainter();

            var changed = false;

            var col = Color;
            if (pegi.edit(ref col).nl(ref changed))
                Color = col;

            if (!Cfg.showColorSliders) return changed;

            ChannelSlider(BrushMask.R, ref colorLinear.r, null, true).nl(ref changed);
            ChannelSlider(BrushMask.G, ref colorLinear.g, null, true).nl(ref changed);
            ChannelSlider(BrushMask.B, ref colorLinear.b, null, true).nl(ref changed);
            ChannelSlider(BrushMask.A, ref colorLinear.a, null, true).nl(ref changed);

            return changed;
        }

        private bool ColorSliders_PlaytimePainter() {

           
            var painter = PlaytimePainter.inspected;
            var id = painter.ImgMeta;
            var cpu = id.TargetIsTexture2D();
            var blitMode = GetBlitMode(cpu);

            if (!blitMode.AllSetUp)
                return false;

            var changed = false;

           // if (Cfg.showColorSliders) {
           bool r = Cfg.showColorSliders || !mask.HasFlag(BrushMask.R);
           bool g = Cfg.showColorSliders || !mask.HasFlag(BrushMask.G);
           bool b = Cfg.showColorSliders || !mask.HasFlag(BrushMask.B);
           bool a = Cfg.showColorSliders || !mask.HasFlag(BrushMask.A);


            var slider = GetBlitMode(cpu).ShowColorSliders;

            if (painter && painter.IsTerrainHeightTexture)
            {
                ChannelSlider(BrushMask.A, ref colorLinear.a, null, true).changes(ref changed);
            }
            else if (painter && painter.IsTerrainControlTexture)
            {
                if (r) ChannelSlider(BrushMask.R, ref colorLinear.r, painter.terrain.GetSplashPrototypeTexture(0), slider)
                    .nl(ref changed);
                if (g) ChannelSlider(BrushMask.G, ref colorLinear.g, painter.terrain.GetSplashPrototypeTexture(1), slider)
                    .nl(ref changed);
                if (b) ChannelSlider(BrushMask.B, ref colorLinear.b, painter.terrain.GetSplashPrototypeTexture(2), slider)
                    .nl(ref changed);
                if (a) ChannelSlider(BrushMask.A, ref colorLinear.a, painter.terrain.GetSplashPrototypeTexture(3), slider)
                    .nl(ref changed);
            }
            else
            {
               
                if (id.TargetIsRenderTexture() && id.renderTexture)
                {
                    if (r) ChannelSlider(BrushMask.R, ref colorLinear.r).nl(ref changed);
                    if (g) ChannelSlider(BrushMask.G, ref colorLinear.g).nl(ref changed);
                    if (b) ChannelSlider(BrushMask.B, ref colorLinear.b).nl(ref changed);

                }
                else
                {

                    if (painter.IsEditingThisMesh || id==null || !id.isATransparentLayer || colorLinear.a > 0)  {

                        var slider_copy = blitMode.UsingSourceTexture ?
                            (srcColorUsage != SourceTextureColorUsage.Unchanged)
                            :slider;

                        if (r) ChannelSlider(BrushMask.R, ref colorLinear.r, null, slider_copy).nl(ref changed);
                        if (g) ChannelSlider(BrushMask.G, ref colorLinear.g, null, slider_copy).nl(ref changed);
                        if (b) ChannelSlider(BrushMask.B, ref colorLinear.b, null, slider_copy).nl(ref changed);
                    }
                    
                    var gotAlpha = painter.meshEditing || id == null || id.texture2D.TextureHasAlpha();

                    if (id == null ||  (!painter.IsEditingThisMesh &&  (gotAlpha || id.preserveTransparency) && !id.isATransparentLayer)) {
                        if (!gotAlpha)
                            icon.Warning.write("Texture as no alpha, clicking save will fix it");

                        if (a) ChannelSlider(BrushMask.A, ref colorLinear.a, null, slider).nl(ref changed);
                    }
                }
            }
          //  }

            if (!painter.IsEditingThisMesh && id!=null && id.isATransparentLayer) {

                var erase = colorLinear.a < 0.5f;

                "Erase".toggleIcon(ref erase).nl(ref changed);

                colorLinear.a = erase ? 0 : 1;

            }

            return changed;
        }
#endif
        #endregion
        
        #region Encode Decode
        public override CfgEncoder Encode() => new CfgEncoder()
                .Add("dyn", brushDynamic, BrushDynamic.all);
        
        public CfgEncoder EncodeStrokeFor(PlaytimePainter painter)
        {

            var id = painter.ImgMeta;

            var rt = id.TargetIsRenderTexture();

            var mode = GetBlitMode(!rt);
            var type = GetBrushType(!rt);

            var worldSpace = rt && IsA3DBrush(painter);

            var cody = new CfgEncoder()

            .Add(rt ? "typeGPU" : "typeCPU", _brushType(!rt));

            if (worldSpace)
                cody.Add("size3D", brush3DRadius);
            else
                cody.Add("size2D", brush2DRadius / ((float)id.width));


            cody.Add_Bool("useMask", useMask)
                .Add("modeCPU", _inCpuBlitMode)
                .Add("modeGPU", _inGpuBlitMode);



            if (useMask)
                cody.Add("mask", (int)mask);

            cody.Add("bc", colorLinear);

            if (mode.UsingSourceTexture)
                cody.Add_IfNotZero("source", selectedSourceTexture);

            if (rt)
            {

                if ((mode.GetType() == typeof(BlitModeBlur)))
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
                .Add("dSpeed", _dSpeed);
            //.Add("Speed", speed);

            return cody;
        }

        public override bool Decode(string tg, string data)
        {

            switch (tg)
            {
                case "typeGPU": _inGpuBrushType = data.ToInt(); break;
                case "typeCPU": _inCpuBrushType = data.ToInt(); break;
                case "size2D": brush2DRadius = data.ToFloat(); break;
                case "size3D": brush3DRadius = data.ToFloat(); break;

                case "useMask": useMask = data.ToBool(); break;

                case "mask": mask = (BrushMask)data.ToInt(); break;

                case "modeCPU": _inCpuBlitMode = data.ToInt(); break;
                case "modeGPU": _inGpuBlitMode = data.ToInt(); break;

                case "bc": colorLinear.Decode(data); break;

                case "source": selectedSourceTexture = data.ToInt(); break;

                case "blur": blurAmount = data.ToFloat(); break;

                case "decA": decalAngle = data.ToFloat(); break;
                case "decNo": selectedDecal = data.ToInt(); break;

                case "Smask": selectedSourceMask = data.ToInt(); break;
                case "maskTil": maskTiling = data.ToFloat(); break;
                case "maskFlip": flipMaskAlpha = data.ToBool(); break;

                case "hard": hardness = data.ToFloat(); break;
                case "Speed": _dSpeed.SetValue(data.ToFloat()); break;
                case "dSpeed": _dSpeed.Decode(data); break;
                case "dyn": data.Decode(out brushDynamic, BrushDynamic.all); break;

                case "maskOff": maskOffset = data.ToVector2(); break;
                default: return false;
            }
            return true;


        }
        #endregion

    }

    #region Dynamics

    public class BrushDynamicAttribute : AbstractWithTaggedTypes
    {
        public override TaggedTypesCfg TaggedTypes => BrushDynamic.all;
    }

    [BrushDynamic]
    public abstract class BrushDynamic : AbstractCfg, IPEGI, IGotClassTag {

        public virtual void OnPrepareRay(PlaytimePainter p, BrushConfig bc, ref Ray ray) { }

        #region Encode & Decode
        public abstract string ClassTag { get; }

        public static TaggedTypesCfg all = new TaggedTypesCfg(typeof(BrushDynamic));
        public TaggedTypesCfg AllTypes => all;
        
        public override bool Decode(string tg, string data) {
            switch (tg) {
                case "t": testValue = data.ToInt(); break;
                default: return false;
            }

            return true;
        }

        public override CfgEncoder Encode() => new CfgEncoder().Add("t", testValue);

        #endregion

        #region Inspector
        int testValue = -1;

#if PEGI
        public virtual bool Inspect() => false;  /*
        {
            bool changed = false;

            "Test Value".edit(60, ref testValue).nl(ref changed);

            return changed;
        }*/
#endif

        #endregion
    }

    [TaggedType(classTag, "None")]
    public class BrushDynamic_None : BrushDynamic {
        const string classTag = "none";

        public override string ClassTag => classTag;
    }

    [TaggedType(classTag, "Jitter")]
    public class BrushDynamic_Jitter : BrushDynamic {
        const string classTag = "gitter";
        public override string ClassTag => classTag;

        private float jitterStrength = 0.1f;

        public override void OnPrepareRay(PlaytimePainter p, BrushConfig bc, ref Ray rey)
        {
            // Quaternion * Vector3

            rey.direction = Vector3.Lerp( rey.direction, UnityEngine.Random.rotation * rey.direction, jitterStrength); //  Quaternion.Lerp(cameraConfiguration.rotation, , camShake);
        }

        #region Inspector
        #if PEGI
        public override bool Inspect() =>
       // {
         //   var changed = false;

            "Strength".edit(ref jitterStrength, 0.00001f, 0.25f);

          //  return changed;

      //  }
        #endif
        #endregion

        #region Encode & Decode
        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "j": jitterStrength = data.ToFloat(); break;
                default: return false;
            }

            return true;
        }

        public override CfgEncoder Encode() => new CfgEncoder().Add("j", jitterStrength);    
        #endregion

    }

    [TaggedType(classTag, "Size from Speed")]
    public class BrushDynamic_SpeedToSize : BrushDynamic {
        const string classTag = "sts";

        public override string ClassTag => classTag;
    }
    
    #endregion
}