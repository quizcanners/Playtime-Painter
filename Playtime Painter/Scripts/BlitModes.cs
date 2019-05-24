using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace PlaytimePainter {

    #region Base
    public abstract class BlitMode : PainterSystem, IEditorDropdown, IGotDisplayName {

        #region All Modes 
        private static List<BlitMode> _allModes;

        public static List<BlitMode> AllModes
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
            _allModes = new List<BlitMode>
            {
                new BlitModeAlpha(0),
                new BlitModeAdd(1),
                new BlitModeSubtract(2),
                new BlitModeCopy(3),
                new BlitModeMin(4),
                new BlitModeMax(5),
                new BlitModeBlur(6),
                new BlitModeBloom(7),
                new BlitModeSamplingOffset(8),
                new BlitModeProjector(9),
                new BlitModeInkFiller(10)
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


        protected BlitMode(int ind)
        {
            index = ind;
        }


        #endregion

        public virtual bool SupportsTransparentLayer => false;

        public BlitMode SetKeyword(ImageMeta id) {

            foreach (var bs in AllModes)
                UnityUtils.SetShaderKeyword(bs.ShaderKeyword(id), false);

            UnityUtils.SetShaderKeyword(ShaderKeyword(id), true);

            return this;

        }

        protected virtual string ShaderKeyword(ImageMeta id) => null;

        public virtual List<string> ShaderKeywords => null;

        public virtual void SetGlobalShaderParameters()
        {
            Shader.DisableKeyword("PREVIEW_SAMPLING_DISPLACEMENT");
            UnityUtils.ToggleShaderKeywords(TexMGMTdata.previewAlphaChanel, "PREVIEW_ALPHA", "PREVIEW_RGB");
        }

        public virtual BlitFunctions.BlitModeFunction BlitFunctionTex2D(ImageMeta id)
        {
            if (id.isATransparentLayer)
                return BlitFunctions.AlphaBlitTransparent;
            return BlitFunctions.AlphaBlitOpaque;
        }

        //public virtual BlitJobBlitMode BlitJobFunction() => BlitJobBlitMode.Alpha;

        public virtual bool AllSetUp => true;
        public virtual bool SupportedByTex2D => true;
        public virtual bool SupportedByRenderTexturePair => true;
        public virtual bool SupportedBySingleBuffer => true;
        public virtual bool SupportsAlphaBufferPainting => true;
        public virtual bool UsingSourceTexture => false;
        public virtual bool ShowColorSliders => true;
        public virtual bool NeedsWorldSpacePosition => false; // WorldSpace effect needs to be rendered using terget's mesh to have world positions of the vertexes
        public virtual Shader ShaderForDoubleBuffer => TexMGMTdata.brushDoubleBuffer;
        public virtual Shader ShaderForSingleBuffer => TexMGMTdata.brushBlit;
        public virtual Shader ShaderForAlphaOutput => TexMGMTdata.additiveAlphaOutput;
        public virtual Shader ShaderForAlphaBufferBlit => TexMGMTdata.multishadeBufferBlit;

        #region Inspect

        protected virtual MsgPainter Translation => MsgPainter.Unnamed;

        public virtual string NameForDisplayPEGI => Translation.GetText();

        public virtual string ToolTip => Translation.GetDescription();

        #if PEGI
        public virtual bool ShowInDropdown()
        {

            var cpu = BrushConfig.InspectedIsCpuBrush;

            if (!PlaytimePainter.inspected)
                return (cpu ? SupportedByTex2D : SupportedByRenderTexturePair);

            var id = InspectedImageMeta;

            if (id == null)
                return false;

            var br = GlobalBrush;

           // if (!cpu && br.useAlphaBuffer && !SupportsAlphaBufferPainting && br.GetBrushType(false).SupportsAlphaBufferPainting)
             //   return false;

            return ((id.destination == TexTarget.Texture2D) && (SupportedByTex2D)) ||
                ((id.destination == TexTarget.RenderTexture) &&
                    ((SupportedByRenderTexturePair && (!id.renderTexture))
                        || (SupportedBySingleBuffer && (id.renderTexture))));
        }

        protected virtual bool Inspect()
        {

            return false;
        }

        public bool Inspect(IPainterManagerModuleBrush module)
        {
            
            var changed = false;

            Inspect();
            
            if (AllSetUp) {
                

                var id = InspectedImageMeta;
                var cpuBlit = id == null ? InspectedBrush.targetIsTex2D : id.destination == TexTarget.Texture2D;
                var brushType = InspectedBrush.GetBrushType(cpuBlit);
                var blitMode = InspectedBrush.GetBlitMode(cpuBlit);
                var usingDecals = (!cpuBlit) && brushType.IsUsingDecals;
                
                pegi.nl();

                if (!cpuBlit)
                    MsgPainter.Hardness.GetText().edit("Makes edges more rough.", 70, ref InspectedBrush.hardness, 1f, 22f).nl(ref changed);
                
                var txt = (usingDecals ? "Tint alpha" : MsgPainter.Speed.GetText());

                txt.write(txt.ApproximateLengthUnsafe());

                InspectedBrush._dSpeed.Inspect().nl(ref changed);
                
                MsgPainter.Scale.Write();

                if (InspectedBrush.IsA3DBrush(InspectedPainter))
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
                    MsgPainter.CopyFrom.GetText().selectOrAdd(70, ref InspectedBrush.selectedSourceTexture, ref TexMGMTdata.sourceTextures)
                        .nl(ref changed);
            }


            pegi.newLine();

            return changed;
        }
        #endif

        #endregion

        public virtual void PrePaint(PlaytimePainter painter, BrushConfig br, StrokeVector st) { }

    }

    #endregion

    #region Alpha Blit

    public class BlitModeAlpha : BlitMode
    {

        protected override string ShaderKeyword(ImageMeta id) => "BLIT_MODE_ALPHABLEND";

        protected override MsgPainter Translation => MsgPainter.BlitModeAlpha;

        public BlitModeAlpha(int ind) : base(ind) { }
        public override bool SupportsTransparentLayer => true;
    }

    #endregion

    #region Add Blit

    public class BlitModeAdd : BlitMode
    {
        static BlitModeAdd _inst;
        public static BlitModeAdd Inst { get { if (_inst == null) InstantiateBrushes(); return _inst; } }
        


        protected override string ShaderKeyword(ImageMeta id) => "BLIT_MODE_ADD";
        
        public override Shader ShaderForSingleBuffer => TexMGMTdata.brushAdd;
        public override BlitFunctions.BlitModeFunction BlitFunctionTex2D(ImageMeta id) => BlitFunctions.AddBlit;

        protected override MsgPainter Translation => MsgPainter.BlitModeAdd;


        public BlitModeAdd(int ind) : base(ind)
        {
            _inst = this;
        }
    }

    #endregion
    
    #region Subtract Blit

    public class BlitModeSubtract : BlitMode
    {

        protected override MsgPainter Translation => MsgPainter.BlitModeSubtract;
        
        protected override string ShaderKeyword(ImageMeta id) => "BLIT_MODE_SUBTRACT";
        
        public override bool SupportedBySingleBuffer => false;

        public override BlitFunctions.BlitModeFunction BlitFunctionTex2D(ImageMeta id) => BlitFunctions.SubtractBlit;
        //public override BlitJobBlitMode BlitJobFunction() => BlitJobBlitMode.Subtract;
        
        public BlitModeSubtract(int ind) : base(ind) { }

    }

    #endregion

    #region Copy Blit

    public class BlitModeCopy : BlitMode
    {
        protected override MsgPainter Translation => MsgPainter.BlitModeCopy;
        
        protected override string ShaderKeyword(ImageMeta id) => "BLIT_MODE_COPY";
        public override bool ShowColorSliders => false;

        public override bool SupportedByTex2D => false;
        public override bool UsingSourceTexture => true;
        public override Shader ShaderForSingleBuffer => TexMGMTdata.brushCopy;

        public BlitModeCopy(int ind) : base(ind) { }
    }

    #endregion

    #region Min Blit

    public class BlitModeMin : BlitMode
    {
        protected override MsgPainter Translation => MsgPainter.BlitModeMin;
        
        public override bool SupportedByRenderTexturePair => false;
        public override bool SupportedBySingleBuffer => false;
        public override BlitFunctions.BlitModeFunction BlitFunctionTex2D(ImageMeta id) => BlitFunctions.MinBlit;
        //public override BlitJobBlitMode BlitJobFunction() => BlitJobBlitMode.Min;

        public BlitModeMin(int ind) : base(ind) { }
    }

    #endregion

    #region Max Blit

    public class BlitModeMax : BlitMode
    {

        protected override MsgPainter Translation => MsgPainter.BlitModeMax;

        
        public override bool SupportedByRenderTexturePair => false;
        public override bool SupportedBySingleBuffer => false;
        public override BlitFunctions.BlitModeFunction BlitFunctionTex2D(ImageMeta id) => BlitFunctions.MaxBlit;
        //public override BlitJobBlitMode BlitJobFunction() => BlitJobBlitMode.Max;

        //"Paints highest value between brush color and current texture color for each channel.";
        public BlitModeMax(int ind) : base(ind) { }
    }

    #endregion

    #region Blur Blit

    public class BlitModeBlur : BlitMode
    {
        protected override string ShaderKeyword(ImageMeta id) => "BRUSH_BLUR";
        public override bool ShowColorSliders => false;
        public override bool SupportedBySingleBuffer => false;
        public override bool SupportedByTex2D => false;

        public override Shader ShaderForDoubleBuffer => TexMGMTdata.brushBlurAndSmudge;
        public override Shader ShaderForAlphaBufferBlit => TexMGMTdata.blurAndSmudgeBufferBlit;

        #region Inspector

        protected override MsgPainter Translation => MsgPainter.BlitModeBlur;

#if PEGI
        protected override bool Inspect()
        {
            var changed = base.Inspect().nl();

            var txt = MsgPainter.BlurAmount.GetText();

            txt.edit(txt.ApproximateLengthUnsafe(), ref InspectedBrush.blurAmount, 1f, 8f).nl(ref changed);
            return changed;
        }
        #endif

        #endregion

        public BlitModeBlur(int ind) : base(ind) { }

    }

    #endregion

    #region Sampling Offset Blit

    public class BlitModeSamplingOffset : BlitMode
    {
        public BlitModeSamplingOffset(int ind) : base(ind) { }

        protected override string ShaderKeyword(ImageMeta id) => "BLIT_MODE_SAMPLE_DISPLACE";

        public enum ColorSetMethod { MDownPosition = 0, MDownColor = 1, Manual = 2 }

        public MyIntVec2 currentPixel = new MyIntVec2();

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
            currentPixel.x = (int)Mathf.Floor(uv.x * Cfg.samplingMaskSize.x);
            currentPixel.y = (int)Mathf.Floor(uv.y * Cfg.samplingMaskSize.y);
        }

        public void FromColor(BrushConfig brush, Vector2 uv)
        {
            var c = brush.Color;

            currentPixel.x = (int)Mathf.Floor((uv.x + (c.r - 0.5f) * 2) * Cfg.samplingMaskSize.x);
            currentPixel.y = (int)Mathf.Floor((uv.y + (c.g - 0.5f) * 2) * Cfg.samplingMaskSize.y);
        }

        #region Inspector

        protected override MsgPainter Translation => MsgPainter.BlitModeOff;

#if PEGI
        protected override bool Inspect()
        {
            bool changed = base.Inspect();

            if (!InspectedPainter)
                return changed;

            pegi.newLine();

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

                if ("Set Tiling Offset".Click(ref changed)) {
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

                            float center_uv_x = ((float)currentPixel.x + 0.5f) / (float)Cfg.samplingMaskSize.x;
                            float center_uv_y = ((float)currentPixel.y + 0.5f) / (float)Cfg.samplingMaskSize.y;

                            int startX = currentPixel.x * dx;

                            for (int suby = 0; suby < dy; suby++)
                            {

                                int y = (currentPixel.y * dy + suby);
                                int start = y * id.width + startX;

                                float offy = (center_uv_y - ((float)y / (float)id.height)) / 2f + 0.5f;

                                for (int subx = 0; subx < dx; subx++)
                                {
                                    int ind = start + subx;

                                    float offx = (center_uv_x - ((float)(startX + subx) / (float)id.width)) / 2f + 0.5f;

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
            pegi.newLine();

            return changed;
        }
#endif
        #endregion

        private readonly ShaderProperty.VectorValue _pointedUvUnTiledProperty = new ShaderProperty.VectorValue("_brushPointedUV_Untiled");

        public override void PrePaint(PlaytimePainter painter, BrushConfig br, StrokeVector st)
        {

            var v4 = new Vector4(st.unRepeatedUv.x, st.unRepeatedUv.y, Mathf.Floor(st.unRepeatedUv.x), Mathf.Floor(st.unRepeatedUv.y));

            _pointedUvUnTiledProperty.GlobalValue = v4;

            if (!st.firstStroke) return;

            if (method == (ColorSetMethod.MDownColor))
            {
                if (painter)
                {
                    painter.SampleTexture(st.uvTo);
                    FromColor(br, st.unRepeatedUv);
                }
            }
            else
            if (method == (ColorSetMethod.MDownPosition))
                FromUv(st.uvTo);

            PainterDataAndConfig.BRUSH_SAMPLING_DISPLACEMENT.GlobalValue = new Vector4(
                (currentPixel.x + 0.5f) / Cfg.samplingMaskSize.x,

                (currentPixel.y + 0.5f) / Cfg.samplingMaskSize.y,

                Cfg.samplingMaskSize.x, Cfg.samplingMaskSize.y);
        }
    }

    #endregion

    #region Bloom Blit

    public class BlitModeBloom : BlitMode
    {
        protected override string ShaderKeyword(ImageMeta id) => "BRUSH_BLOOM";

        public override bool ShowColorSliders => false;
        public override bool SupportedBySingleBuffer => false;
        public override bool SupportedByTex2D => false;
        
        public override Shader ShaderForDoubleBuffer => TexMGMTdata.brushBlurAndSmudge;
        public override Shader ShaderForAlphaBufferBlit => TexMGMTdata.blurAndSmudgeBufferBlit;

        #region Inspector
        protected override MsgPainter Translation => MsgPainter.BlitModeBloom;

#if PEGI
        protected override bool Inspect() {

            var changed = base.Inspect().nl();
            "Bloom Radius".edit(70, ref InspectedBrush.blurAmount, 1f, 8f).nl(ref changed);
            return changed;
        }
        #endif
        #endregion

        public BlitModeBloom(int ind) : base(ind) { }
    }

    #endregion

    #region Projector Blit

    public class BlitModeProjector : BlitMode
    {

        public override bool SupportedByTex2D => false;

        public override bool SupportedBySingleBuffer => false;

        public override bool UsingSourceTexture => true;

        public override bool SupportsAlphaBufferPainting => true;
        
        public override bool AllSetUp => PainterCamera.depthProjectorCamera;

        protected override string ShaderKeyword(ImageMeta id) => "BLIT_MODE_PROJECTION";

        public override Shader ShaderForDoubleBuffer => TexMGMTdata.brushDoubleBufferProjector;

        public override Shader ShaderForAlphaBufferBlit => TexMGMTdata.projectorBrushBufferBlit;
        public override Shader ShaderForAlphaOutput => TexMGMTdata.additiveAlphaAndUVOutput;

        public override void SetGlobalShaderParameters()
        {
            base.SetGlobalShaderParameters();
            UnityUtils.SetShaderKeyword(PainterDataAndConfig.USE_DEPTH_FOR_PROJECTOR, TexMGMTdata.useDepthForProjector);
        }

        public override bool NeedsWorldSpacePosition => true;

        #region Inspector
        protected override MsgPainter Translation => MsgPainter.BlitModeProjector;


#if PEGI
        protected override bool Inspect()
        {
            var changed = false;

            if (!DepthProjectorCamera.Instance) {

                "Projector brush needs Projector Camera to be in the scene".writeWarning();
                pegi.nl();

                if ("Create Projector Camera".Click().nl()) {

                    UnityUtils.Instantiate<DepthProjectorCamera>();
                    "posProjCam".resetOneTimeHint();
                }
            }
            else
            {
              
                if (BrushConfig.showAdvanced)
                    "Paint only visible (by Projector)".toggleIcon(ref TexMGMTdata.useDepthForProjector).nl();

                "First step is usually to position the projector camera. You can Lock it to current camera view.".writeOneTimeHint("posProjCam");

                if (icon.Delete.Click("Delete Projector Camera"))
                    DepthProjectorCamera.Instance.gameObject.DestroyWhatever();
                else
                    DepthProjectorCamera.Instance.Nested_Inspect().nl(ref changed);
            }

            base.Inspect().nl(ref changed);


            return changed;
        }

#endif
        #endregion

        public BlitModeProjector(int ind) : base(ind)  { }
    }
    #endregion

    #region Filler Blit

    public class BlitModeInkFiller : BlitMode {

        public override bool SupportedByTex2D => false;

        public override bool SupportedBySingleBuffer => false;

        public override bool SupportsAlphaBufferPainting => false;

        public override Shader ShaderForDoubleBuffer => TexMGMTdata.inkColorSpread;

        #region Inspector

        protected override MsgPainter Translation => MsgPainter.BlitModeFiller;

#if PEGI
        protected override bool Inspect()
        {
            var changed = base.Inspect().nl();

            var txt = MsgPainter.SpreadSpeed.GetText();

            txt.edit(txt.ApproximateLengthUnsafe(), ref InspectedBrush.blurAmount, 1f, 8f).nl(ref changed);

            return changed;
        }
        #endif
        #endregion

        public BlitModeInkFiller(int ind) : base(ind) { }
    }

    #endregion

}