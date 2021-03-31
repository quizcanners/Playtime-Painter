using System;
using System.Collections.Generic;
using QuizCanners.Inspect;
using PlaytimePainter.CameraModules;
using QuizCanners.Lerp;
using QuizCanners.Utils;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace PlaytimePainter {

    public static class BlitModes {

        public abstract class Base : PainterClass, IEditorDropdown, IGotDisplayName {

            #region All Modes 

            private static List<Base> _allModes;

            public static List<Base> AllModes
            {
                get
                {
                    if (_allModes == null)
                        InstantiateBrushes();
                    return _allModes;
                }
            }


            protected static void InstantiateBrushes()
            {
                _allModes = new List<Base>
                {
                    new Alpha(0),
                    new Add(1),
                    new Subtract(2),
                    new Copy(3),
                    new Min(4),
                    new Max(5),
                    new Blur(6),
                    new Bloom(7),
                    new SamplingOffset(8),
                    new Projector(9),
                    new Filler(10),
                    new Custom(11),
                };
                // The code below uses reflection to find all classes that are child classes of BlitMode.
                // The code above adds them manually to save some compilation time,
                // and if you add new BlitMode, just add _allModes.Add (new BlitModeSomething ());
                // Alternatively, in a far-far future, the code below may be reanabled if there will be like hundreds of fan-made brushes for my asset
                // Which would be cool, but I'm a realist so whatever happens its ok, I lived a good life and greatfull for every day.

                /*
                List<GetBrushType> allTypes = CsharpFuncs.GetAllChildTypesOf<BlitMode>();
                foreach (GetBrushType t in allTypes) {
                    BlitMode tb = (BlitMode)Activator.CreateInstance(t);
                    _allModes.Add(tb);
                }
                */
            }


            public int index;


            protected Base(int ind)
            {
                index = ind;
            }


            #endregion

            public Base SetKeyword(TextureMeta id)
            {

                foreach (var bs in AllModes)
                    QcUnity.SetShaderKeyword(bs.ShaderKeyword(id), false);

                QcUnity.SetShaderKeyword(ShaderKeyword(id), true);

                return this;

            }

            protected virtual string ShaderKeyword(TextureMeta id) => null;

            public virtual List<string> ShaderKeywords => null;

            public virtual void SetGlobalShaderParameters()
            {
                Shader.DisableKeyword("PREVIEW_SAMPLING_DISPLACEMENT");
                QcUnity.ToggleShaderKeywords(Cfg.previewAlphaChanel, "PREVIEW_ALPHA", "PREVIEW_RGB");
            }

            public virtual BlitFunctions.BlitModeFunction BlitFunctionTex2D(TextureMeta id)
            {
                if (id.isATransparentLayer)
                    return BlitFunctions.AlphaBlitTransparent;
                return BlitFunctions.AlphaBlitOpaque;
            }

            public virtual bool AllSetUp => true;
            public virtual bool SupportedByTex2D => true;
            public virtual bool SupportedByRenderTexturePair => true;
            public virtual bool SupportedBySingleBuffer => true;
            public virtual bool SupportsAlphaBufferPainting => true;
            public virtual bool UsingSourceTexture => false;
            public virtual bool ShowColorSliders => true;

            public virtual bool NeedsWorldSpacePosition =>
                false; // WorldSpace effect needs to be rendered using terget's mesh to have world positions of the vertexes

            public virtual Shader ShaderForDoubleBuffer => Cfg.brushDoubleBuffer;
            public virtual Shader ShaderForSingleBuffer => Cfg.brushBlit;
            public virtual Shader ShaderForAlphaOutput => Cfg.additiveAlphaOutput;
            public virtual Shader ShaderForAlphaBufferBlit => Cfg.multishadeBufferBlit;

            #region Inspect

            protected virtual MsgPainter Translation => MsgPainter.Unnamed;

            public virtual string NameForDisplayPEGI() => Translation.GetText();

            public virtual string ToolTip => Translation.GetDescription();
            
            public virtual bool ShowInDropdown()
            {

                var cpu = Brush.InspectedIsCpuBrush;

                if (!PlaytimePainter.inspected)
                    return (cpu ? SupportedByTex2D : SupportedByRenderTexturePair);

                var id = InspectedImageMeta;

                if (id == null)
                    return false;

                var br = GlobalBrush;

                return ((id.target == TexTarget.Texture2D) && (SupportedByTex2D)) ||
                       ((id.target == TexTarget.RenderTexture) &&
                        ((SupportedByRenderTexturePair && (!id.renderTexture))
                         || (SupportedBySingleBuffer && (id.renderTexture))));
            }

            protected virtual bool Inspect() => false;
            
            public bool InspectWithModule()
            {

                var changed = false;

                Inspect();

                if (AllSetUp)
                {

                    var id = InspectedImageMeta;
                    var cpuBlit = id == null ? InspectedBrush.targetIsTex2D : id.target == TexTarget.Texture2D;
                    var brushType = InspectedBrush.GetBrushType(cpuBlit);
                    var blitMode = InspectedBrush.GetBlitMode(cpuBlit);
                    var usingDecals = (!cpuBlit) && brushType.IsUsingDecals;

                    pegi.nl();

                    if (!cpuBlit)
                        MsgPainter.Hardness.GetText()
                            .edit("Makes edges more rough.", 70, ref InspectedBrush.hardness, 1f, 5f).nl(ref changed);

                    var txt = (usingDecals ? "Tint alpha" : MsgPainter.Flow.GetText());

                    txt.write(txt.ApproximateLength());

                    InspectedBrush._dFlow.Inspect();

                    pegi.nl();

                    MsgPainter.Scale.Write();

                    if (InspectedBrush.Is3DBrush(id))
                    {
                        var m = PlaytimePainter.inspected.GetMesh();

                        var maxScale = (m ? m.bounds.max.magnitude : 1) * (!PlaytimePainter.inspected
                                           ? 1
                                           : PlaytimePainter.inspected.transform.lossyScale.magnitude);

                        pegi.edit(ref InspectedBrush.brush3DRadius, 0.001f * maxScale, maxScale * 0.5f)
                            .changes(ref changed);
                    }
                    else
                    {
                        if (!brushType.IsPixelPerfect)
                            pegi.edit(ref InspectedBrush.brush2DRadius, cpuBlit ? 1 : 0.1f,
                                usingDecals ? 128 : id?.width * 0.5f ?? 256).changes(ref changed);
                        else
                        {
                            var val = (int) InspectedBrush.brush2DRadius;
                            pegi.edit(ref val, (int) (cpuBlit ? 1 : 0.1f),
                                (int) (usingDecals ? 128 : id?.width * 0.5f ?? 256)).changes(ref changed);
                            InspectedBrush.brush2DRadius = val;

                        }
                    }

                    pegi.nl();

                    if (blitMode.UsingSourceTexture && (id == null || id.TargetIsRenderTexture()))
                        MsgPainter.CopyFrom.GetText().selectOrAdd(70, ref InspectedBrush.selectedSourceTexture,
                                ref Cfg.sourceTextures)
                            .nl(ref changed);
                }


                pegi.nl();

                return changed;
            }

            #endregion

            public virtual void PrePaint(PaintCommand.UV paintCommand) //Brush br, Stroke st, PlaytimePainter painter = null)
            {
            }

        }
        
        #region Alpha

        public class Alpha : Base
        {

            protected override string ShaderKeyword(TextureMeta id) => "BLIT_MODE_ALPHABLEND";

            protected override MsgPainter Translation => MsgPainter.BlitModeAlpha;

            public Alpha(int ind) : base(ind)
            {
            }
            
        }

        #endregion

        #region Add

        public class Add : Base
        {
            static Add _inst;

            public static Add Inst
            {
                get
                {
                    if (_inst == null) InstantiateBrushes();
                    return _inst;
                }
            }
            
            protected override string ShaderKeyword(TextureMeta id) => "BLIT_MODE_ADD";

            public override Shader ShaderForSingleBuffer => Cfg.brushAdd;
            public override BlitFunctions.BlitModeFunction BlitFunctionTex2D(TextureMeta id) => BlitFunctions.AddBlit;

            protected override MsgPainter Translation => MsgPainter.BlitModeAdd;

            protected override bool Inspect()
            {
                var changed = base.Inspect();

                if (GlobalBrush.Color.DistanceRgba(Color.clear, GlobalBrush.mask) < 0.1f)
                    "Color value is very low. Increase if flow of painting is slower then expected. ".writeWarning();

                return changed;
            }


            public Add(int ind) : base(ind)
            {
                _inst = this;
            }
        }

        #endregion

        #region Subtract

        public class Subtract : Base
        {

            protected override MsgPainter Translation => MsgPainter.BlitModeSubtract;

            protected override string ShaderKeyword(TextureMeta id) => "BLIT_MODE_SUBTRACT";

            public override bool SupportedBySingleBuffer => false;

            public override BlitFunctions.BlitModeFunction BlitFunctionTex2D(TextureMeta id) =>
                BlitFunctions.SubtractBlit;

            public Subtract(int ind) : base(ind)
            {
            }

            protected override bool Inspect()
            {
                var changed = base.Inspect();

                if (GlobalBrush.Color.DistanceRgba(Color.clear, GlobalBrush.mask) < 0.1f)
                    "Color value is very low. Subtraction effect will accumulate slowly. ".writeWarning();

                return changed;
            }

        }

        #endregion

        #region Copy

        public class Copy : Base
        {
            protected override MsgPainter Translation => MsgPainter.BlitModeCopy;

            protected override string ShaderKeyword(TextureMeta id) => "BLIT_MODE_COPY";
            public override bool ShowColorSliders => false;

            public override bool SupportedByTex2D => false;
            public override bool UsingSourceTexture => true;
            public override Shader ShaderForSingleBuffer => Cfg.brushCopy;

            public Copy(int ind) : base(ind)
            {
            }
        }

        #endregion

        #region Min

        public class Min : Base
        {
            protected override MsgPainter Translation => MsgPainter.BlitModeMin;

            public override bool SupportedByRenderTexturePair => false;
            public override bool SupportedBySingleBuffer => false;
            public override BlitFunctions.BlitModeFunction BlitFunctionTex2D(TextureMeta id) => BlitFunctions.MinBlit;

            public Min(int ind) : base(ind)
            {
            }
        }

        #endregion

        #region Max

        public class Max : Base
        {

            protected override MsgPainter Translation => MsgPainter.BlitModeMax;


            public override bool SupportedByRenderTexturePair => false;
            public override bool SupportedBySingleBuffer => false;

            public override BlitFunctions.BlitModeFunction BlitFunctionTex2D(TextureMeta id) => BlitFunctions.MaxBlit;
            //public override BlitJobBlitMode BlitJobFunction() => BlitJobBlitMode.Max;

            //"Paints highest value between brush color and current texture color for each channel.";
            public Max(int ind) : base(ind)
            {
            }
        }

        #endregion

        #region Blur

        public class Blur : Base
        {
            protected override string ShaderKeyword(TextureMeta id) => "BRUSH_BLUR";
            public override bool ShowColorSliders => false;
            public override bool SupportedBySingleBuffer => false;
            public override bool SupportedByTex2D => false;

            public override Shader ShaderForDoubleBuffer => Cfg.brushBlurAndSmudge;
            public override Shader ShaderForAlphaBufferBlit => Cfg.blurAndSmudgeBufferBlit;

            #region Inspector

            protected override MsgPainter Translation => MsgPainter.BlitModeBlur;
            
            protected override bool Inspect()
            {
                var changed = base.Inspect().nl();

                var txt = MsgPainter.BlurAmount.GetText();

                txt.edit(txt.ApproximateLength(), ref InspectedBrush.blurAmount, 1f, 8f).nl(ref changed);
                return changed;
            }

            #endregion

            public Blur(int ind) : base(ind)
            {
            }

        }

        #endregion

        #region Sampling Offset

        public class SamplingOffset : Base
        {
            public SamplingOffset(int ind) : base(ind)
            {
            }

            protected override string ShaderKeyword(TextureMeta id) => "BLIT_MODE_SAMPLE_DISPLACE";

            public enum ColorSetMethod
            {
                MDownPosition = 0,
                MDownColor = 1,
                Manual = 2
            }

            public MyIntVec2 currentPixel;

            public ColorSetMethod method;

            public override void SetGlobalShaderParameters()
            {
                Shader.EnableKeyword("PREVIEW_SAMPLING_DISPLACEMENT");

                Shader.DisableKeyword("PREVIEW_ALPHA");
                Shader.DisableKeyword("PREVIEW_RGB");
            }

            public override bool SupportedByTex2D => true;

            public void FromUv(Vector2 uv)
            {
                currentPixel.x = (int) Mathf.Floor(uv.x * Cfg.samplingMaskSize.x);
                currentPixel.y = (int) Mathf.Floor(uv.y * Cfg.samplingMaskSize.y);
            }

            public void FromColor(Brush brush, Vector2 uv)
            {
                var c = brush.Color;

                currentPixel.x = (int) Mathf.Floor((uv.x + (c.r - 0.5f) * 2) * Cfg.samplingMaskSize.x);
                currentPixel.y = (int) Mathf.Floor((uv.y + (c.g - 0.5f) * 2) * Cfg.samplingMaskSize.y);
            }

            #region Inspector

            protected override MsgPainter Translation => MsgPainter.BlitModeOff;
            
            protected override bool Inspect()
            {
                bool changed = base.Inspect();

                if (!InspectedPainter)
                    return changed;

                pegi.nl();

                "Mask Size: ".edit(60, ref Cfg.samplingMaskSize).nl(ref changed);

                Cfg.samplingMaskSize.Clamp(1, 512);

                "Color Set On".editEnum(ref method).nl(ref changed);

                if (method == ColorSetMethod.Manual)
                {
                    "CurrentPixel".edit(80, ref currentPixel).nl(ref changed);

                    currentPixel.Clamp(-Cfg.samplingMaskSize.Max, Cfg.samplingMaskSize.Max * 2);
                }

                var id = InspectedImageMeta;

                if (id != null)
                {

                    if ("Set Tiling Offset".Click(ref changed))
                    {
                        id.tiling = Vector2.one * 1.5f;
                        id.offset = -Vector2.one * 0.25f;
                        InspectedPainter.UpdateTilingToMaterial();
                    }

                    if (InspectedPainter != null && "Generate Default".Click().nl(ref changed))
                    {
                        var pix = id.Pixels;

                        int dx = id.width / Cfg.samplingMaskSize.x;
                        int dy = id.height / Cfg.samplingMaskSize.y;

                        for (currentPixel.x = 0; currentPixel.x < Cfg.samplingMaskSize.x; currentPixel.x++)
                        for (currentPixel.y = 0; currentPixel.y < Cfg.samplingMaskSize.y; currentPixel.y++)
                        {

                            float center_uv_x = (currentPixel.x + 0.5f) / Cfg.samplingMaskSize.x;
                            float center_uv_y = (currentPixel.y + 0.5f) / Cfg.samplingMaskSize.y;

                            int startX = currentPixel.x * dx;

                            for (int suby = 0; suby < dy; suby++)
                            {

                                int y = (currentPixel.y * dy + suby);
                                int start = y * id.width + startX;

                                float offy = (center_uv_y - (y / (float) id.height)) / 2f + 0.5f;

                                for (int subx = 0; subx < dx; subx++)
                                {
                                    int ind = start + subx;

                                    float offx = (center_uv_x - ((startX + subx) / (float) id.width)) / 2f +
                                                 0.5f;

                                    pix[ind].r = offx;
                                    pix[ind].g = offy;
                                }
                            }

                        }

                        id.SetAndApply();
                        if (!id.TargetIsTexture2D())
                            id.Texture2D_To_RenderTexture();

                    }
                }

                pegi.nl();

                return changed;
            }

            #endregion

            private readonly ShaderProperty.VectorValue _pointedUvUnTiledProperty =
                new ShaderProperty.VectorValue("_qcPp_brushUvPosTo_Untiled");

            private static readonly ShaderProperty.VectorValue BRUSH_SAMPLING_DISPLACEMENT = new ShaderProperty.VectorValue("_qcPp_brushSamplingDisplacement");

            public override void PrePaint(PaintCommand.UV paintCommand) //Brush br, Stroke st, PlaytimePainter painter = null)
            {
                
                Stroke st = paintCommand.Stroke;

                var v4 = new Vector4(st.unRepeatedUv.x, st.unRepeatedUv.y, Mathf.Floor(st.unRepeatedUv.x),
                    Mathf.Floor(st.unRepeatedUv.y));

                _pointedUvUnTiledProperty.GlobalValue = v4;

                if (!st.firstStroke) return;

                if (method == (ColorSetMethod.MDownColor))
                {
                    var painter = paintCommand.TryGetPainter();

                    if (painter)
                    {
                        painter.SampleTexture(st.uvTo);
                    }
                    else
                        paintCommand.Brush.Color = paintCommand.TextureData.SampleAt(st.uvTo);

                    FromColor(paintCommand.Brush, st.unRepeatedUv);
                }
                else if (method == (ColorSetMethod.MDownPosition))
                    FromUv(st.uvTo);

                BRUSH_SAMPLING_DISPLACEMENT.GlobalValue = new Vector4(
                    (currentPixel.x + 0.5f) / Cfg.samplingMaskSize.x,

                    (currentPixel.y + 0.5f) / Cfg.samplingMaskSize.y,

                    Cfg.samplingMaskSize.x, Cfg.samplingMaskSize.y);
            }
        }

        #endregion

        #region Bloom

        public class Bloom : Base
        {
            protected override string ShaderKeyword(TextureMeta id) => "BRUSH_BLOOM";

            public override bool ShowColorSliders => false;
            public override bool SupportedBySingleBuffer => false;
            public override bool SupportedByTex2D => false;

            public override Shader ShaderForDoubleBuffer => Cfg.brushBlurAndSmudge;
            public override Shader ShaderForAlphaBufferBlit => Cfg.blurAndSmudgeBufferBlit;

            #region Inspector

            protected override MsgPainter Translation => MsgPainter.BlitModeBloom;
            
            protected override bool Inspect()
            {

                var changed = base.Inspect().nl();
                "Bloom Radius".edit(70, ref InspectedBrush.blurAmount, 1f, 8f).nl(ref changed);
                return changed;
            }

            #endregion

            public Bloom(int ind) : base(ind)
            {
            }
        }

        #endregion

        #region Projector

        public class Projector : Base
        {

            public override bool SupportedByTex2D => false;

            public override bool SupportedBySingleBuffer => false;

            public override bool UsingSourceTexture => true;

            public override bool SupportsAlphaBufferPainting => true;

            public override bool AllSetUp => PainterCamera.depthProjectorCamera;

            protected override string ShaderKeyword(TextureMeta id) => "BLIT_MODE_PROJECTION";

            public override Shader ShaderForDoubleBuffer => Cfg.brushDoubleBufferProjector;

            public override Shader ShaderForAlphaBufferBlit => Cfg.projectorBrushBufferBlit;
            public override Shader ShaderForAlphaOutput => Cfg.additiveAlphaAndUVOutput;

            public override void SetGlobalShaderParameters()
            {
                base.SetGlobalShaderParameters();
                QcUnity.SetShaderKeyword(PainterShaderVariables.USE_DEPTH_FOR_PROJECTOR,
                    Cfg.useDepthForProjector);
            }

            public override bool NeedsWorldSpacePosition => true;

            #region Inspector

            protected override MsgPainter Translation => MsgPainter.BlitModeProjector;
            
            protected override bool Inspect()
            {
                var changed = false;

                var depthCamera = DepthProjectorCamera.Instance;

                if (!depthCamera)
                {

                    "Projector brush needs Projector Camera to be in the scene".writeWarning();
                    pegi.nl();

                    if ("Create Projector Camera".Click().nl())
                    {
                        QcUnity.Instantiate<DepthProjectorCamera>();
                    }
                }
                else
                {

                    "Projector:".nl();

#if UNITY_EDITOR
                    if (Application.isPlaying && EditorApplication.isPaused && depthCamera._projectFromMainCamera)
                    {
                        "In Play mode Projector brush is copying position from main camera".writeHint();
                    }
#endif

                        if (Brush.showAdvanced)
                        "Paint only visible (by Projector)".toggleIcon(ref Cfg.useDepthForProjector).nl();

                    var painter = PlaytimePainter.inspected;

                    bool mentionLink = !depthCamera._projectFromMainCamera;
                    bool mentionPrview = painter && painter.NotUsingPreview;
                   
                    if (mentionLink || mentionPrview)
                        "{0} {1}".F(mentionLink ? "You can Lock Projector to current camera view to allign the projection." + Environment.NewLine : "", mentionPrview ? "Preview helps see the Projection" : "" ).writeHint();

                    if (icon.Delete.Click("Delete Projector Camera"))
                        depthCamera.gameObject.DestroyWhatever();
                    else
                       pegi.Nested_Inspect(depthCamera.Inspect_PainterShortcut).nl(ref changed);

                    pegi.line(Color.black);
                    pegi.nl();

                }

                base.Inspect().nl(ref changed);


                return changed;
            }

#endregion

            public Projector(int ind) : base(ind)
            {
            }
        }

#endregion

#region Filler

        public class Filler : Base
        {

            public override bool SupportedByTex2D => false;

            public override bool SupportedBySingleBuffer => false;

            public override bool SupportsAlphaBufferPainting => false;

            public override Shader ShaderForDoubleBuffer => Cfg.inkColorSpread;

#region Inspector

            protected override MsgPainter Translation => MsgPainter.BlitModeFiller;
            
            protected override bool Inspect()
            {
                var changed = base.Inspect().nl();

                var txt = MsgPainter.SpreadSpeed.GetText();

                txt.edit(txt.ApproximateLength(), ref InspectedBrush.blurAmount, 1f, 8f).nl(ref changed);

                return changed;
            }

#endregion

            public Filler(int ind) : base(ind)
            {
            }
        }

#endregion

#region Custom

        public class Custom : Base
        {
           
            protected override string ShaderKeyword(TextureMeta id) => null;

            public override List<string> ShaderKeywords => null;

            public override void SetGlobalShaderParameters()
            {
                base.SetGlobalShaderParameters();
            }

            public override bool AllSetUp => true;
            public override bool SupportedByTex2D => false;
            public override bool SupportsAlphaBufferPainting => false;
            
            public override bool SupportedByRenderTexturePair => _customCfg ? _customCfg.doubleBuffer : true;
            public override bool SupportedBySingleBuffer => _customCfg ? !_customCfg.doubleBuffer : true;

            public override Shader ShaderForDoubleBuffer => _customCfg ? _customCfg.shader : null;
            public override Shader ShaderForSingleBuffer => _customCfg ? _customCfg.shader : null;
            public override Shader ShaderForAlphaOutput => _customCfg ? _customCfg.shader : null;
            public override Shader ShaderForAlphaBufferBlit => _customCfg ? _customCfg.shader : null;

            public override bool ShowInDropdown() => true;

            public override bool UsingSourceTexture => _customCfg ? _customCfg.selectSourceTexture : false;

            public override bool ShowColorSliders => _customCfg ? _customCfg.showColorSliders : false;
            
            public override bool NeedsWorldSpacePosition => _customCfg ? _customCfg.usingWorldSpacePosition : false;

            private BlitModeCustom _customCfg
            {
                get
                {
                    return Cfg.customBlitModes.TryGet(Cfg.selectedCustomBlitMode);   
                }
            }
            
            protected override MsgPainter Translation => MsgPainter.BlitModeCustom;

            private bool _showConfig;

            private bool IsBlitReady => _customCfg && _customCfg.AllSetUp;

            protected override bool Inspect()
            {
                var changed = base.Inspect().nl();

                var allCstm = Cfg.customBlitModes;

                if ("Config".select_Index(60, ref Cfg.selectedCustomBlitMode, allCstm))
                    Cfg.SetToDirty();

                var cfg = _customCfg;

                if (pegi.edit(ref cfg, 60).nl(ref changed) && cfg)
                {
                    if (allCstm.Contains(cfg))
                        Cfg.selectedCustomBlitMode = allCstm.IndexOf(cfg);
                    else
                    {
                        Cfg.customBlitModes.Add(cfg);
                        Cfg.selectedCustomBlitMode = Cfg.customBlitModes.Count - 1;
                    }
                }



                if (_customCfg)
                {
                    if (_customCfg.name.foldout(ref _showConfig).nl(ref changed))
                    {
                        _customCfg.Nested_Inspect(ref changed);
                    }
                }
                else
                {
                    ("Create a BlitModeCustom Scriptable Object and put your custom blit shader into it. " +
                     " Right mouse button in the Project view and select Create->Playtime Painter-> Blit Mode Custom ").writeHint();
                    pegi.nl();
                }

                if (IsBlitReady)
                {
                    if (PlaytimePainter.inspected)
                    {
                        var img = PlaytimePainter.inspected.TexMeta;
                        if (img != null)
                        {
                            var rt = img.CurrentRenderTexture();

                            if (rt)
                            {
                                if ("Grahics BLIT".Click().nl())
                                RenderTextureBuffersManager.Blit(
                                    from: _customCfg.sourceTexture ? _customCfg.sourceTexture : rt,
                                    to: rt,
                                    shader: _customCfg.shader);

                                if ("Painter Camera Render".Click().nl())
                                    PainterCamera.Inst.Render(
                                        from: _customCfg.sourceTexture ? _customCfg.sourceTexture : rt,
                                        to: rt,
                                        shader: _customCfg.shader);

                            }
                        }
                    }
                }

                return false;
            }

            public Custom(int ind) : base(ind)
            {
            }
        }

#endregion
    }
}