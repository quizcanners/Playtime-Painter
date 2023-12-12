using System.Collections.Generic;
using QuizCanners.Inspect;
using QuizCanners.Lerp;
using QuizCanners.Migration;
using QuizCanners.Utils;
using UnityEngine;

namespace PainterTool {

    public static class BlitModes {


        private static List<Base> _allModes;

        public static List<Base> All
        {
            get
            {
                if (_allModes == null)
                    InstantiateBrushes();
                return _allModes;
            }
        }


        private static void InstantiateBrushes()
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

        public abstract class Base : PainterClass, IInspectorDropdown {

            #region All Modes 



            public int index;


            protected Base(int ind)
            {
                index = ind;
            }


            #endregion

            internal Base SetKeyword(TextureMeta id)
            {

                foreach (var bs in All)
                    QcUnity.SetShaderKeyword(bs.ShaderKeyword(id), false);

                QcUnity.SetShaderKeyword(ShaderKeyword(id), true);

                return this;

            }

            internal virtual string ShaderKeyword(TextureMeta id) => null;

            public virtual List<string> ShaderKeywords => null;

            public virtual void SetGlobalShaderParameters()
            {
                Shader.DisableKeyword("PREVIEW_SAMPLING_DISPLACEMENT");
                QcUnity.ToggleShaderKeywords(Painter.Data.previewAlphaChanel, "PREVIEW_ALPHA", "PREVIEW_RGB");
            }

            internal virtual BlitFunctions.BlitModeFunction BlitFunctionTex2D(TextureMeta id)
            {
                if (id[TextureCfgFlags.TransparentLayer])
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

            public virtual Shader ShaderForDoubleBuffer => Painter.Data.brushDoubleBuffer.Shader;
            public virtual Shader ShaderForSingleBuffer => Painter.Data.brushBlit.Shader;
            public virtual Shader ShaderForAlphaOutput => Painter.Data.additiveAlphaOutput;
            public virtual Shader ShaderForAlphaBufferBlit => Painter.Data.multishadeBufferBlit;

            #region Inspect

            protected virtual MsgPainter Translation => MsgPainter.Unnamed;

            public override string ToString() => Translation.GetText();

            public virtual string ToolTip => Translation.GetDescription();
            
            public virtual bool ShowInInspectorDropdown()
            {
                var cpu = Brush.InspectedIsCpuBrush;

                if (!PainterComponent.inspected)
                    return (cpu ? SupportedByTex2D : SupportedByRenderTexturePair);

                var id = InspectedImageMeta;

                if (id == null)
                    return false;

                return ((id.Target == TexTarget.Texture2D) && (SupportedByTex2D)) ||
                       ((id.Target == TexTarget.RenderTexture) &&
                        ((SupportedByRenderTexturePair && (!id.RenderTexture))
                         || (SupportedBySingleBuffer && (id.RenderTexture))));
            }

            protected virtual void Inspect() { }
            
            public void InspectWithModule(TexTarget target)
            {
                Inspect();

                if (AllSetUp)
                {

                    var id = InspectedImageMeta;
                    var brushType = InspectedBrush.GetBrushType(target);
                    var blitMode = InspectedBrush.GetBlitMode(target);
                    var usingDecals = target == TexTarget.RenderTexture && brushType.IsUsingDecals;
                    bool cpuBlit = target == TexTarget.Texture2D;

                    pegi.Nl();

                    if (target == TexTarget.RenderTexture)
                        MsgPainter.Sharpness.GetText().PegiLabel("Makes edges more rough.", 70)
                            .Edit( ref InspectedBrush.hardness, 1f, 5f).Nl();

                    var txt = (usingDecals ? "Tint alpha" : MsgPainter.Flow.GetText());

                    txt.PegiLabel().ApproxWidth().Write();

                    pegi.Nested_Inspect_Value(ref InspectedBrush._dFlow);
            
                    pegi.Nl();

                    var is3D = InspectedBrush.Is3DBrush(id);

                    if (is3D)
                    {
                        var m = PainterComponent.inspected.GetMesh();

                        if (m)
                        {
                            MsgPainter.Scale.Write(tip: "Blit Mode Scale for Sphere Brush", 50);

                            var maxScale = (m ? m.bounds.max.magnitude : 1) * (!PainterComponent.inspected
                                               ? 1
                                               : PainterComponent.inspected.transform.lossyScale.magnitude);

                            pegi.Edit(ref InspectedBrush.brush3DRadius, 0.001f * maxScale, maxScale * 0.5f);
                        } else 
                        {
                            "Scale".PegiLabel().Edit(ref InspectedBrush.brush3DRadius);
                        }
                    }
                    else
                    {
                        MsgPainter.Scale.Write(tip: "Blit Mode Scale for 2D brush", 50);

                        if (!brushType.IsPixelPerfect)
                            pegi.Edit(ref InspectedBrush.brush2DRadius, cpuBlit ? 1 : 0.1f,
                                usingDecals ? 128 : id?.Width * 0.5f ?? 256);
                        else
                        {
                            var val = (int) InspectedBrush.brush2DRadius;
                            pegi.Edit(ref val, (int) (cpuBlit ? 1 : 0.1f),
                                (int) (usingDecals ? 128 : id?.Width * 0.5f ?? 256));
                            InspectedBrush.brush2DRadius = val;

                        }
                    }

                    pegi.Nl();

                    if (blitMode.UsingSourceTexture && (id == null || id.TargetIsRenderTexture()))
                        MsgPainter.CopyFrom.GetText().PegiLabel(70).SelectOrAdd(ref InspectedBrush.selectedSourceTexture,
                                ref Painter.Data.sourceTextures)
                            .Nl();
                }


                pegi.Nl();
            }

            #endregion

            public virtual void PrePaint(Painter.Command.Base command) //Brush br, Stroke st, PlaytimePainter painter = null)
            {
            }

        }
        
        #region Alpha

        public class Alpha : Base
        {

            internal override string ShaderKeyword(TextureMeta id) => "BLIT_MODE_ALPHABLEND";

            protected override MsgPainter Translation => MsgPainter.BlitModeAlpha;

            public Alpha(int ind) : base(ind)
            {
            }
            
        }

        #endregion

        #region Add

        public class Add : Base
        {
            private static Add _inst;

            public static Add Inst
            {
                get
                {
                    if (_inst == null) InstantiateBrushes();
                    return _inst;
                }
            }

            internal override string ShaderKeyword(TextureMeta id) => "BLIT_MODE_ADD";

            public override Shader ShaderForSingleBuffer => Painter.Data.brushAdd.Shader;
            internal override BlitFunctions.BlitModeFunction BlitFunctionTex2D(TextureMeta id) => BlitFunctions.AddBlit;

            protected override MsgPainter Translation => MsgPainter.BlitModeAdd;

            protected override void Inspect()
            {
                base.Inspect();

                if (GlobalBrush.Color.DistanceRgba(Color.clear, GlobalBrush.mask) < 0.1f)
                    "Color value is very low. Increase if flow of painting is slower then expected. ".PegiLabel().WriteWarning();
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

            internal override string ShaderKeyword(TextureMeta id) => "BLIT_MODE_SUBTRACT";

            public override bool SupportedBySingleBuffer => false;

            internal override BlitFunctions.BlitModeFunction BlitFunctionTex2D(TextureMeta id) =>
                BlitFunctions.SubtractBlit;

            public Subtract(int ind) : base(ind)
            {
            }

            protected override void Inspect()
            {
                base.Inspect();

                if (GlobalBrush.Color.DistanceRgba(Color.clear, GlobalBrush.mask) < 0.1f)
                    "Color value is very low. Subtraction effect will accumulate slowly. ".PegiLabel().WriteWarning();
            }

        }

        #endregion

        #region Copy

        public class Copy : Base
        {
            protected override MsgPainter Translation => MsgPainter.BlitModeCopy;

            internal override string ShaderKeyword(TextureMeta id) => "BLIT_MODE_COPY";
            public override bool ShowColorSliders => false;

            public override bool SupportedByTex2D => false;
            public override bool UsingSourceTexture => true;
            public override Shader ShaderForSingleBuffer => Painter.Data.brushCopy.Shader;

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
            internal override BlitFunctions.BlitModeFunction BlitFunctionTex2D(TextureMeta id) => BlitFunctions.MinBlit;

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

            internal override BlitFunctions.BlitModeFunction BlitFunctionTex2D(TextureMeta id) => BlitFunctions.MaxBlit;
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
            internal override string ShaderKeyword(TextureMeta id) => "BRUSH_BLUR";
            public override bool ShowColorSliders => false;
            public override bool SupportedBySingleBuffer => false;
            public override bool SupportedByTex2D => false;

            public override Shader ShaderForDoubleBuffer => Painter.Data.brushBlurAndSmudge.Shader;
            public override Shader ShaderForAlphaBufferBlit => Painter.Data.blurAndSmudgeBufferBlit;

            #region Inspector

            protected override MsgPainter Translation => MsgPainter.BlitModeBlur;
            
            protected override void Inspect()
            {
                base.Inspect(); pegi.Nl();
                var txt = MsgPainter.BlurAmount.GetText();
                txt.PegiLabel().ApproxWidth().Edit( ref InspectedBrush.blurAmount, 1f, 8f).Nl();
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

            internal override string ShaderKeyword(TextureMeta id) => "BLIT_MODE_SAMPLE_DISPLACE";

            public enum ColorSetMethod
            {
                MDownPosition = 0,
                MDownColor = 1,
                Manual = 2
            }

            public Vector2Int currentPixel;

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
                currentPixel.x = (int) Mathf.Floor(uv.x * Painter.Data.samplingMaskSize.x);
                currentPixel.y = (int) Mathf.Floor(uv.y * Painter.Data.samplingMaskSize.y);
            }

            public void FromColor(Brush brush, Vector2 uv)
            {
                var c = brush.Color;

                currentPixel.x = (int) Mathf.Floor((uv.x + (c.r - 0.5f) * 2) * Painter.Data.samplingMaskSize.x);
                currentPixel.y = (int) Mathf.Floor((uv.y + (c.g - 0.5f) * 2) * Painter.Data.samplingMaskSize.y);
            }

            #region Inspector

            protected override MsgPainter Translation => MsgPainter.BlitModeOff;
            
            protected override void Inspect()
            {
                base.Inspect();

                if (!InspectedPainter)
                    return;

                pegi.Nl();

                "Mask Size: ".PegiLabel(60).Edit(ref Painter.Data.samplingMaskSize).Nl();

                Painter.Data.samplingMaskSize.Clamp(1, 512);

                "Color Set On".PegiLabel().Edit_Enum(ref method).Nl();

                if (method == ColorSetMethod.Manual)
                {
                    "CurrentPixel".PegiLabel(80).Edit(ref currentPixel).Nl();

                    var ssize = Painter.Data.samplingMaskSize;

                    var max = System.Math.Max(ssize.x, ssize.y); 

                    currentPixel.Clamp(-max, max * 2);
                }

                var id = InspectedImageMeta;

                if (id != null)
                {

                    if ("Set Tiling Offset".PegiLabel().Click())
                    {
                        id.Tiling = Vector2.one * 1.5f;
                        id.Offset = -Vector2.one * 0.25f;
                        InspectedPainter.UpdateTilingToMaterial();
                    }

                    if (InspectedPainter != null && "Generate Default".PegiLabel().Click().Nl())
                    {
                        var pix = id.Pixels;

                        var samplingMaskSize = Painter.Data.samplingMaskSize;

                        int dx = id.Width / samplingMaskSize.x;
                        int dy = id.Height / samplingMaskSize.y;

                        for (currentPixel.x = 0; currentPixel.x < samplingMaskSize.x; currentPixel.x++)
                        for (currentPixel.y = 0; currentPixel.y < samplingMaskSize.y; currentPixel.y++)
                        {

                            float center_uv_x = (currentPixel.x + 0.5f) / samplingMaskSize.x;
                            float center_uv_y = (currentPixel.y + 0.5f) / samplingMaskSize.y;

                            int startX = currentPixel.x * dx;

                            for (int suby = 0; suby < dy; suby++)
                            {

                                int y = (currentPixel.y * dy + suby);
                                int start = y * id.Width + startX;

                                float offy = (center_uv_y - (y / (float) id.Height)) / 2f + 0.5f;

                                for (int subx = 0; subx < dx; subx++)
                                {
                                    int ind = start + subx;

                                    float offx = (center_uv_x - ((startX + subx) / (float) id.Width)) / 2f +
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

                pegi.Nl();
            }

            #endregion

            private readonly ShaderProperty.VectorValue _pointedUvUnTiledProperty = new("_qcPp_brushUvPosTo_Untiled");

            private static readonly ShaderProperty.VectorValue BRUSH_SAMPLING_DISPLACEMENT = new("_qcPp_brushSamplingDisplacement");

            public override void PrePaint(Painter.Command.Base Command) 
            {
                
                Stroke st = Command.Stroke;

                var v4 = new Vector4(st.unRepeatedUv.x, st.unRepeatedUv.y, Mathf.Floor(st.unRepeatedUv.x),
                    Mathf.Floor(st.unRepeatedUv.y));

                _pointedUvUnTiledProperty.GlobalValue = v4;

                if (!st.firstStroke) return;

                if (method == (ColorSetMethod.MDownColor))
                {
                    var painter = Command.TryGetPainter();

                    if (painter)
                    {
                        painter.SampleTexture(st.uvTo);
                    }
                    else
                        Command.Brush.Color = Command.TextureData.SampleAt(st.uvTo);

                    FromColor(Command.Brush, st.unRepeatedUv);
                }
                else if (method == (ColorSetMethod.MDownPosition))
                    FromUv(st.uvTo);

                var samplingMaskSize = Painter.Data.samplingMaskSize;

                BRUSH_SAMPLING_DISPLACEMENT.GlobalValue = new Vector4(
                    (currentPixel.x + 0.5f) / samplingMaskSize.x,
                    (currentPixel.y + 0.5f) / samplingMaskSize.y,
                    samplingMaskSize.x, samplingMaskSize.y);
            }
        }

        #endregion

        #region Bloom

        public class Bloom : Base
        {
            internal override string ShaderKeyword(TextureMeta id) => "BRUSH_BLOOM";

            public override bool ShowColorSliders => false;
            public override bool SupportedBySingleBuffer => false;
            public override bool SupportedByTex2D => false;

            public override Shader ShaderForDoubleBuffer => Painter.Data.brushBlurAndSmudge.Shader;
            public override Shader ShaderForAlphaBufferBlit => Painter.Data.blurAndSmudgeBufferBlit;

            #region Inspector

            protected override MsgPainter Translation => MsgPainter.BlitModeBloom;
            
            protected override void Inspect()
            {
                base.Inspect();
                pegi.Nl();
                "Bloom Radius".PegiLabel(70).Edit(ref InspectedBrush.blurAmount, 1f, 8f).Nl();
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

            public override bool AllSetUp => Singleton.Get<Singleton_DepthProjectorCamera>();

            internal override string ShaderKeyword(TextureMeta id) => "BLIT_MODE_PROJECTION";

            public override Shader ShaderForDoubleBuffer => Painter.Data.brushDoubleBufferProjector.Shader;

            public override Shader ShaderForAlphaBufferBlit => Painter.Data.projectorBrushBufferBlit;
            public override Shader ShaderForAlphaOutput => Painter.Data.additiveAlphaAndUVOutput;

            public override void SetGlobalShaderParameters()
            {
                base.SetGlobalShaderParameters();
                QcUnity.SetShaderKeyword(PainterShaderVariables.USE_DEPTH_FOR_PROJECTOR,
                    Painter.Data.useDepthForProjector);
            }

            public override bool NeedsWorldSpacePosition => true;

            #region Inspector

            protected override MsgPainter Translation => MsgPainter.BlitModeProjector;
            
            protected override void Inspect()
            {
                var depthCamera = Singleton.Get<Singleton_DepthProjectorCamera>(); //.Instance;

                if (!depthCamera)
                {
                    "Projector brush needs Projector Camera to be in the scene".PegiLabel().WriteWarning();
                    pegi.Nl();

                    if ("Create Projector Camera".PegiLabel().Click().Nl())
                    {
                        QcUnity.Instantiate<Singleton_DepthProjectorCamera>();
                    }
                }
                else
                {

                    "Projector:".PegiLabel().Nl();

#if UNITY_EDITOR
                    if (Application.isPlaying && UnityEditor.EditorApplication.isPaused && depthCamera._projectFromMainCamera)
                    {
                        "In Play mode Projector brush is copying position from main camera".PegiLabel().Write_Hint();
                    }
#endif

                        if (Brush.showAdvanced)
                        "Paint only visible (by Projector)".PegiLabel().ToggleIcon(ref Painter.Data.useDepthForProjector).Nl();

                    var painter = PainterComponent.inspected;

                    if (painter && painter.NotUsingPreview)
                        "Preview helps see the Projection".PegiLabel().WriteWarning();
                    else if (!depthCamera._projectFromMainCamera)
                         "You can Lock Projector to current camera view to allign the projection.".PegiLabel().Write_Hint();
                    
                    if (Icon.Delete.Click("Delete Projector Camera"))
                        depthCamera.gameObject.DestroyWhatever();
                    else
                       pegi.Nested_Inspect(depthCamera.Inspect_PainterShortcut, depthCamera).Nl();

                    pegi.Line(Color.black);
                    pegi.Nl();

                }

                base.Inspect();
                pegi.Nl();
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

            public override Shader ShaderForDoubleBuffer => Painter.Data.inkColorSpread.Shader;

#region Inspector

            protected override MsgPainter Translation => MsgPainter.BlitModeFiller;
            
            protected override void Inspect()
            {
                base.Inspect(); 
                pegi.Nl();

                var txt = MsgPainter.SpreadSpeed.GetText();

                txt.PegiLabel().ApproxWidth().Edit( ref InspectedBrush.blurAmount, 1f, 8f).Nl();
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

            internal override string ShaderKeyword(TextureMeta id) => null;

            public override List<string> ShaderKeywords => null;

            public override bool AllSetUp => true;
            public override bool SupportedByTex2D => false;
            public override bool SupportsAlphaBufferPainting => false;
            
            public override bool SupportedByRenderTexturePair => CustomCfg ? CustomCfg.doubleBuffer : true;
            public override bool SupportedBySingleBuffer => CustomCfg ? !CustomCfg.doubleBuffer : true;

            public override Shader ShaderForDoubleBuffer => CustomCfg ? CustomCfg.shader : null;
            public override Shader ShaderForSingleBuffer => CustomCfg ? CustomCfg.shader : null;
            public override Shader ShaderForAlphaOutput => CustomCfg ? CustomCfg.shader : null;
            public override Shader ShaderForAlphaBufferBlit => CustomCfg ? CustomCfg.shader : null;

            public override bool ShowInInspectorDropdown() => true;

            public override bool UsingSourceTexture => CustomCfg ? CustomCfg.selectSourceTexture : false;

            public override bool ShowColorSliders => CustomCfg ? CustomCfg.showColorSliders : false;
            
            public override bool NeedsWorldSpacePosition => CustomCfg ? CustomCfg.usingWorldSpacePosition : false;

            private PlaytimePainter_BlitModeCustom CustomCfg => Painter.Data.customBlitModes.TryGet(Painter.Data.selectedCustomBlitMode);   
            
            protected override MsgPainter Translation => MsgPainter.BlitModeCustom;

            private bool _showConfig;

            private bool IsBlitReady => CustomCfg && CustomCfg.AllSetUp;

            protected override void Inspect()
            {
                base.Inspect(); pegi.Nl();

                var allCstm = Painter.Data.customBlitModes;

                if ("Config".PegiLabel(60).Select_Index(ref Painter.Data.selectedCustomBlitMode, allCstm))
                    Painter.Data.SetToDirty();

                var cfg = CustomCfg;

                if (pegi.Edit(ref cfg, 60).Nl() && cfg)
                {
                    if (allCstm.Contains(cfg))
                        Painter.Data.selectedCustomBlitMode = allCstm.IndexOf(cfg);
                    else
                    {
                        Painter.Data.customBlitModes.Add(cfg);
                        Painter.Data.selectedCustomBlitMode = Painter.Data.customBlitModes.Count - 1;
                    }
                }



                if (CustomCfg)
                {
                    if (CustomCfg.name.PegiLabel().IsFoldout(ref _showConfig).Nl())
                    {
                        CustomCfg.Nested_Inspect();
                    }
                }
                else
                {
                    ("Create a BlitModeCustom Scriptable Object and put your custom blit shader into it. " +
                     " Right mouse button in the Project view and select Create->Playtime Painter-> Blit Mode Custom ").PegiLabel().Write_Hint();
                    pegi.Nl();
                }

                if (!IsBlitReady) return;
                if (!PainterComponent.inspected) return;
                
                var img = PainterComponent.inspected.TexMeta;
                if (img == null) return;
                
                var rt = img.CurrentRenderTexture();

                if (!rt) return;
                
                if ("Grahics BLIT".PegiLabel().Click().Nl())
                    RenderTextureBuffersManager.Blit(
                        @from: CustomCfg.sourceTexture ? CustomCfg.sourceTexture : rt,
                        to: rt,
                        shader: CustomCfg.shader);

                if ("Painter Camera Render".PegiLabel().Click().Nl())
                    Painter.Camera.Render(
                        @from: CustomCfg.sourceTexture ? CustomCfg.sourceTexture : rt,
                        to: rt,
                        shader: CustomCfg.shader);

                return;
            }

            public Custom(int ind) : base(ind)
            {
            }
        }

#endregion
    }
}