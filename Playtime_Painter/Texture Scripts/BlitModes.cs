using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace Playtime_Painter {

    public abstract class BlitMode : PainterStuff, IEditorDropdown {

        private static List<BlitMode> _allModes;

        public int index;

        public virtual bool SupportsTransparentLayer => false;

        public static List<BlitMode> AllModes {
            get {
                if (_allModes == null)
                    InstantiateBrushes();
                return _allModes;
            }
        }
        
        public BlitMode SetKeyword(ImageData id) {

            foreach (BlitMode bs in AllModes)
                UnityHelperFunctions.SetShaderKeyword(bs.ShaderKeyword(id), false);

            UnityHelperFunctions.SetShaderKeyword(ShaderKeyword(id), true);

            return this;

        }

        protected virtual string ShaderKeyword(ImageData id) => null;

        public virtual List<string> ShaderKeywords => null;

        public virtual void SetGlobalShaderParameters()
        {
            Shader.DisableKeyword("PREVIEW_SAMPLING_DISPLACEMENT");
            UnityHelperFunctions.ToggleShaderKeywords(TexMGMTdata.previewAlphaChanel, "PREVIEW_ALPHA", "PREVIEW_RGB");
        }

        public BlitMode(int ind)
        {
            index = ind;
        }

        protected static void InstantiateBrushes()
        {
            _allModes = new List<BlitMode>
            {
                new BlitModeAlphaBlit(0),
                new BlitModeAdd(1),
                new BlitModeSubtract(2),
                new BlitModeCopy(3),
                new BlitModeMin(4),
                new BlitModeMax(5),
                new BlitModeBlur(6),
                new BlitModeBloom(7),
                new BlitModeSamplingOffset(8)
            };
            // The code below uses reflection to find all classes that are child classes of BlitMode.
            // The code above adds them manually to save some compilation time,
            // and if you add new BlitMode, just add _allModes.Add (new BlitModeMyStuff ());
            // Alternatively, in a far-far future, the code below may be reanabled if there will be like hundreds of fan-made brushes for my asset
            // Which would be cool, but I'm a realist so whatever happens its ok, I lived a good life and greatfull for every day.

            /*
			List<Type> allTypes = CsharpFuncs.GetAllChildTypesOf<BlitMode>();
			foreach (Type t in allTypes) {
				BlitMode tb = (BlitMode)Activator.CreateInstance(t);
				_allModes.Add(tb);
			}
            */
        }

        public virtual Blit_Functions.blitModeFunction BlitFunctionTex2D(ImageData id)
        {
            if (id.isATransparentLayer)
                return Blit_Functions.AlphaBlitTransparent;
            return Blit_Functions.AlphaBlitOpaque;
        }

        public virtual BlitJobBlitMode BlitJobFunction() => BlitJobBlitMode.Alpha;

        public virtual bool SupportedByTex2D { get { return true; } }
        public virtual bool SupportedByRenderTexturePair { get { return true; } }
        public virtual bool SupportedBySingleBuffer { get { return true; } }
        public virtual bool UsingSourceTexture { get { return false; } }
        public virtual bool ShowColorSliders { get { return true; } }
        public virtual Shader ShaderForDoubleBuffer => TexMGMTdata.br_Multishade;
        public virtual Shader ShaderForSingleBuffer => TexMGMTdata.br_Blit;
#if PEGI

        public virtual bool ShowInDropdown()
        {

            bool CPU = BrushConfig.InspectedIsCPUbrush;

            if (!PlaytimePainter.inspectedPainter)
                return (CPU ? SupportedByTex2D : SupportedByRenderTexturePair);

            ImageData id = InspectedImageData;

            if (id == null)
                return false;

            return ((id.destination == TexTarget.Texture2D) && (SupportedByTex2D)) ||
                ((id.destination == TexTarget.RenderTexture) &&
                    ((SupportedByRenderTexturePair && (!id.renderTexture))
                        || (SupportedBySingleBuffer && (id.renderTexture))));
        }


        public virtual bool PEGI()
        {

            ImageData id = InspectedImageData;

            bool cpuBlit = id == null ? InspectedBrush.TargetIsTex2D : id.destination == TexTarget.Texture2D;
            BrushType brushType = InspectedBrush.Type(cpuBlit);
            bool usingDecals = (!cpuBlit) && brushType.IsUsingDecals;

            bool changed = false;

            pegi.newLine();

            if (!cpuBlit)
                changed |= "Hardness:".edit("Makes edges more rough.", 70, ref InspectedBrush.Hardness, 1f, 512f).nl();

            changed |= (usingDecals ? "Tint alpha" : "Speed").edit(usingDecals ? 70 : 40, ref InspectedBrush.speed, 0.01f, 20).nl();

            pegi.write("Scale:", 40);

            if (InspectedBrush.IsA3Dbrush(InspectedPainter))
            {
                Mesh m = PlaytimePainter.inspectedPainter.GetMesh();

                float maxScale = (m ? m.bounds.max.magnitude : 1) * (!PlaytimePainter.inspectedPainter ? 1 : PlaytimePainter.inspectedPainter.transform.lossyScale.magnitude);

                changed |= pegi.edit(ref InspectedBrush.Brush3D_Radius, 0.001f * maxScale, maxScale * 0.5f);
            }
            else
            {
                if (!brushType.IsPixelPerfect)
                    changed |= pegi.edit(ref InspectedBrush.Brush2D_Radius, cpuBlit ? 1 : 0.1f, usingDecals ? 128 : (id != null ? id.width * 0.5f : 256));
                else
                {
                    int val = (int)InspectedBrush.Brush2D_Radius;
                    changed |= pegi.edit(ref val, (int)(cpuBlit ? 1 : 0.1f), (int)(usingDecals ? 128 : (id != null ? id.width * 0.5f : 256)));
                    InspectedBrush.Brush2D_Radius = val;
                }
            }

            pegi.newLine();

            if (InspectedBrush.BlitMode.UsingSourceTexture && (id == null || id.TargetIsRenderTexture()))
                 "Copy From:".selectOrAdd(70, ref InspectedBrush.selectedSourceTexture, ref TexMGMTdata.sourceTextures).nl(ref changed);

            pegi.newLine();

            return changed;
        }
#endif
        public virtual void PrePaint(PlaytimePainter pntr, BrushConfig br, StrokeVector st)
        {

            return;
        }
    }
    public class BlitModeAlphaBlit : BlitMode
    {
        public override string ToString() { return "Alpha Blit"; }

        protected override string ShaderKeyword(ImageData id) => "BRUSH_NORMAL";

        public BlitModeAlphaBlit(int ind) : base(ind) { }
        public override bool SupportsTransparentLayer => true;
    }

    public class BlitModeAdd : BlitMode
    {
        static BlitModeAdd _inst;
        public static BlitModeAdd Inst { get { if (_inst == null) InstantiateBrushes(); return _inst; } }

        public override string ToString() => "Add";
        protected override string ShaderKeyword(ImageData id) => "BRUSH_ADD";

        public override Shader ShaderForSingleBuffer => TexMGMTdata.br_Add;
        public override Blit_Functions.blitModeFunction BlitFunctionTex2D(ImageData id) => Blit_Functions.AddBlit;

        public BlitModeAdd(int ind) : base(ind)
        {
            _inst = this;
        }
    }

    public class BlitModeSubtract : BlitMode
    {
        public override string ToString() { return "Subtract"; }
        protected override string ShaderKeyword(ImageData id) => "BRUSH_SUBTRACT";

        //public override Shader shaderForSingleBuffer { get { return meshMGMT.br_Add; } }
        public override bool SupportedBySingleBuffer { get { return false; } }

        public override Blit_Functions.blitModeFunction BlitFunctionTex2D(ImageData id) => Blit_Functions.SubtractBlit;
        public override BlitJobBlitMode BlitJobFunction() => BlitJobBlitMode.Subtract;
        public BlitModeSubtract(int ind) : base(ind) { }

    }

    public class BlitModeCopy : BlitMode
    {
        public override string ToString() => "Copy";
        protected override string ShaderKeyword(ImageData id) => "BRUSH_COPY";
        public override bool ShowColorSliders => false;

        public override bool SupportedByTex2D => false;
        public override bool UsingSourceTexture => true;
        public override Shader ShaderForSingleBuffer => TexMGMTdata.br_Copy;

        public BlitModeCopy(int ind) : base(ind) { }
    }

    public class BlitModeMin : BlitMode
    {
        public override string ToString() { return "Min"; }
        public override bool SupportedByRenderTexturePair { get { return false; } }
        public override bool SupportedBySingleBuffer { get { return false; } }
        public override Blit_Functions.blitModeFunction BlitFunctionTex2D(ImageData id) => Blit_Functions.MinBlit;
        public override BlitJobBlitMode BlitJobFunction() => BlitJobBlitMode.Min;
        public BlitModeMin(int ind) : base(ind) { }
    }

    public class BlitModeMax : BlitMode
    {
        public override string ToString() { return "Max"; }
        public override bool SupportedByRenderTexturePair { get { return false; } }
        public override bool SupportedBySingleBuffer { get { return false; } }
        public override Blit_Functions.blitModeFunction BlitFunctionTex2D(ImageData id) => Blit_Functions.MaxBlit;
        public override BlitJobBlitMode BlitJobFunction() => BlitJobBlitMode.Max;
        public BlitModeMax(int ind) : base(ind) { }
    }

    public class BlitModeBlur : BlitMode
    {
        public override string ToString() => "Blur";
        protected override string ShaderKeyword(ImageData id) => "BRUSH_BLUR";
        public override bool ShowColorSliders => false;
        public override bool SupportedBySingleBuffer => false;
        public override bool SupportedByTex2D => false;

        public override Shader ShaderForDoubleBuffer => TexMGMTdata.br_BlurN_SmudgeBrush;
#if PEGI
        public override bool PEGI()
        {

            bool brushChanged_RT = base.PEGI();
            pegi.newLine();
            brushChanged_RT |= "Blur Amount".edit(70, ref InspectedBrush.blurAmount, 1f, 8f);
            pegi.newLine();
            return brushChanged_RT;
        }
#endif

        public BlitModeBlur(int ind) : base(ind) { }

    }

    public class BlitModeSamplingOffset : BlitMode
    {
        public BlitModeSamplingOffset(int ind) : base(ind) { }

        protected override string ShaderKeyword(ImageData id) => "BRUSH_SAMPLE_DISPLACE";

        public enum ColorSetMethod { MDownPosition = 0, MDownColor = 1, Manual = 2 }

        public MyIntVec2 currentPixel = new MyIntVec2();

        public ColorSetMethod method;

        public override void SetGlobalShaderParameters()
        {
            Shader.EnableKeyword("PREVIEW_SAMPLING_DISPLACEMENT");

            Shader.DisableKeyword("PREVIEW_ALPHA");
            Shader.DisableKeyword("PREVIEW_RGB");
        }

        public override bool SupportedByTex2D { get { return true; } }

        public override string ToString() { return "Pixel Reshape"; }

        public void FromUV(Vector2 uv)
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
#if PEGI
        public override bool PEGI()
        {
            bool changed = base.PEGI();

            if (!InspectedPainter)
                return changed;

            pegi.newLine();

            changed |= "Mask Size: ".edit(60, ref Cfg.samplingMaskSize).nl();

            Cfg.samplingMaskSize.Clamp(1, 512);

            changed |= "Color Set On".editEnum(ref method).nl();

            if (method == ColorSetMethod.Manual)
            {
                changed |= "CurrentPixel".edit(80, ref currentPixel).nl();

                currentPixel.Clamp(-Cfg.samplingMaskSize.Max, Cfg.samplingMaskSize.Max * 2);
            }

            var id = InspectedImageData;

            if (id != null)
            {

                if ("Set Tiling Offset".Click(ref changed)) {
                    id.tiling = Vector2.one * 1.5f;
                    id.offset = -Vector2.one * 0.25f;
                    InspectedPainter.UpdateTylingToMaterial();
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

        ShaderProperty.VectorValue pointedUV_Untiled_Property = new ShaderProperty.VectorValue("_brushPointedUV_Untiled");

        public override void PrePaint(PlaytimePainter pntr, BrushConfig br, StrokeVector st)
        {

            var v4 = new Vector4(st.unRepeatedUV.x, st.unRepeatedUV.y, Mathf.Floor(st.unRepeatedUV.x), Mathf.Floor(st.unRepeatedUV.y));

            pointedUV_Untiled_Property.GlobalValue = v4;

            if (st.firstStroke)
            {

                if (method == (ColorSetMethod.MDownColor))
                {
                    if (pntr)
                    {
                        pntr.SampleTexture(st.uvTo);
                        FromColor(br, st.unRepeatedUV);
                    }
                }
                else
                if (method == (ColorSetMethod.MDownPosition))
                    FromUV(st.uvTo);

                PainterDataAndConfig.BRUSH_SAMPLING_DISPLACEMENT.GlobalValue = new Vector4(
                    ((float)currentPixel.x + 0.5f) / ((float)Cfg.samplingMaskSize.x),

                    ((float)currentPixel.y + 0.5f) / ((float)Cfg.samplingMaskSize.y),

                    Cfg.samplingMaskSize.x, Cfg.samplingMaskSize.y);

            }
        }
    }

    public class BlitModeBloom : BlitMode
    {
        public override string ToString() => "Bloom";
        protected override string ShaderKeyword(ImageData id) => "BRUSH_BLOOM";

        public override bool ShowColorSliders => false;
        public override bool SupportedBySingleBuffer => false;
        public override bool SupportedByTex2D => false;

        public BlitModeBloom(int ind) : base(ind) { }

        public override Shader ShaderForDoubleBuffer => TexMGMTdata.br_BlurN_SmudgeBrush;
#if PEGI
        public override bool PEGI()
        {

            bool changed = base.PEGI().nl();
            changed |= "Bloom Radius".edit(70, ref InspectedBrush.blurAmount, 1f, 8f).nl();
            return changed;
        }
#endif
    }

}