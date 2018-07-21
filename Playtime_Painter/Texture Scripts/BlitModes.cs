using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace Playtime_Painter
{

    public abstract class BlitMode : PainterStuff, IeditorDropdown
    {

        private static List<BlitMode> _allModes;
        
        public int index;
        
        public static List<BlitMode> AllModes
        {
            get
            {
                if (_allModes == null)
                    InstantiateBrushes();
                return _allModes;
            }
        }

        public virtual bool showInDropdown() {

            bool CPU = BrushConfig.InspectedIsCPUbrush;

            if (PlaytimePainter.inspectedPainter == null)
                return (CPU ? SupportedByTex2D : SupportedByRenderTexturePair);

            ImageData id = InspectedImageData;

            if (id == null)
                return false;

            return ((id.destination == TexTarget.Texture2D) && (SupportedByTex2D)) ||
                (
                    (id.destination == TexTarget.RenderTexture) &&
                    ((SupportedByRenderTexturePair && (id.renderTexture == null))
                        || (SupportedBySingleBuffer && (id.renderTexture != null)))
                );
        }

        public BlitMode SetKeyword()
        {

            foreach (BlitMode bs in AllModes)
                if (bs != this)
                {
                    string name = bs.ShaderKeyword;
                    if (name != null)
                        BlitModeExtensions.KeywordSet(name, false);
                }

            if (ShaderKeyword != null)
                BlitModeExtensions.KeywordSet(ShaderKeyword, true);

            return this;

        }

        protected virtual string ShaderKeyword { get { return null; } }

        public virtual void SetGlobalShaderParameters() {}

        public BlitMode(int ind)
        {
            index = ind;//AllModes.Count;
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

        public virtual Blit_Functions.blitModeFunction BlitFunctionTex2D { get { return Blit_Functions.AlphaBlit; } }

        public virtual bool SupportedByTex2D { get { return true; } }
        public virtual bool SupportedByRenderTexturePair { get { return true; } }
        public virtual bool SupportedBySingleBuffer { get { return true; } }
        public virtual bool UsingSourceTexture { get { return false; } }
        public virtual bool ShowColorSliders { get { return true; } }
        public virtual Shader ShaderForDoubleBuffer { get { return TexMGMT.br_Multishade; } }
        public virtual Shader ShaderForSingleBuffer { get { return TexMGMT.br_Blit; } }
#if PEGI
        public virtual bool PEGI() {

            ImageData id = InspectedImageData;

            bool cpuBlit = id == null ? InspectedBrush.TargetIsTex2D : id.destination == TexTarget.Texture2D;
            BrushType brushType = InspectedBrush.Type(cpuBlit);
            bool usingDecals = (!cpuBlit) && brushType.IsUsingDecals;
            
            bool changed = false;

            pegi.newLine();

            if ((!cpuBlit) && (!usingDecals)) 
                changed |= "Hardness:".edit("Makes edges more rough.", 70, ref InspectedBrush.Hardness, 1f, 512f).nl();

            changed |=  (usingDecals ? "Tint alpha" : "Speed").edit(usingDecals ? 70 : 40, ref InspectedBrush.speed, 0.01f, 20).nl();
       
            pegi.write("Scale:", 40);

            if (InspectedBrush.IsA3Dbrush(InspectedPainter))  {
                Mesh m = PlaytimePainter.inspectedPainter.getMesh();
           
                float maxScale = (m != null ? m.bounds.max.magnitude : 1) * (PlaytimePainter.inspectedPainter == null ? 1 : PlaytimePainter.inspectedPainter.transform.lossyScale.magnitude);

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

            if ((InspectedBrush.BlitMode.UsingSourceTexture) && (id == null || id.TargetIsRenderTexture())) {
                if (TexMGMT.sourceTextures.Count > 0)
                {
                    pegi.write("Copy From:", 70);
                    changed |= pegi.selectOrAdd(ref InspectedBrush.selectedSourceTexture, ref TexMGMT.sourceTextures);
                }
                else
                    "Add Textures to Render Camera to copy from".nl();
            }

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
            protected override string ShaderKeyword { get { return "BRUSH_NORMAL"; } }
        public BlitModeAlphaBlit(int ind) : base(ind)
        {
            
        }
    }

        public class BlitModeAdd : BlitMode
        {
            static BlitModeAdd _inst;
            public static BlitModeAdd Inst { get { if (_inst == null) InstantiateBrushes(); return _inst; } }

            public override string ToString() { return "Add"; }
            protected override string ShaderKeyword { get { return "BRUSH_ADD"; } }

            public override Shader ShaderForSingleBuffer { get { return TexMGMT.br_Add; } }
            public override Blit_Functions.blitModeFunction BlitFunctionTex2D { get { return Blit_Functions.AddBlit; } }

            public BlitModeAdd(int ind) : base(ind)
            {
                _inst = this;
            }
        }

    public class BlitModeSubtract : BlitMode
    {
        public override string ToString() { return "Subtract"; }
        protected override string ShaderKeyword { get { return "BRUSH_SUBTRACT"; } }

        //public override Shader shaderForSingleBuffer { get { return meshMGMT.br_Add; } }
        public override bool SupportedBySingleBuffer { get { return false; } }

        public override Blit_Functions.blitModeFunction BlitFunctionTex2D { get { return Blit_Functions.SubtractBlit; } }

        public BlitModeSubtract(int ind) : base(ind){}

    }

        public class BlitModeCopy : BlitMode
        {
            public override string ToString() { return "Copy"; }
            protected override string ShaderKeyword { get { return "BRUSH_COPY"; } }
            public override bool ShowColorSliders { get { return false; } }

            public override bool SupportedByTex2D { get { return false; } }
            public override bool UsingSourceTexture { get { return true; } }
            public override Shader ShaderForSingleBuffer { get { return TexMGMT.br_Copy; } }

        public BlitModeCopy(int ind) : base(ind) { }
    }

        public class BlitModeMin : BlitMode
        {
            public override string ToString() { return "Min"; }
            public override bool SupportedByRenderTexturePair { get { return false; } }
            public override bool SupportedBySingleBuffer { get { return false; } }
            public override Blit_Functions.blitModeFunction BlitFunctionTex2D { get { return Blit_Functions.MinBlit; } }
        public BlitModeMin(int ind) : base(ind) { }
    }

        public class BlitModeMax : BlitMode
        {
            public override string ToString() { return "Max"; }
            public override bool SupportedByRenderTexturePair { get { return false; } }
            public override bool SupportedBySingleBuffer { get { return false; } }
            public override Blit_Functions.blitModeFunction BlitFunctionTex2D { get { return Blit_Functions.MaxBlit; } }
        public BlitModeMax(int ind) : base(ind) { }
    }

        public class BlitModeBlur : BlitMode
        {
            public override string ToString() { return "Blur"; }
            protected override string ShaderKeyword { get { return "BRUSH_BLUR"; } }
            public override bool ShowColorSliders { get { return false; } }
            public override bool SupportedBySingleBuffer { get { return false; } }
            public override bool SupportedByTex2D { get { return false; } }

            public override Shader ShaderForDoubleBuffer { get { return TexMGMT.br_BlurN_SmudgeBrush; } }
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

        protected override string ShaderKeyword { get { return "BRUSH_SAMPLE_DISPLACE"; } }

            public enum ColorSetMethod { MDownPosition = 0, MDownColor = 1, Manual = 2 }

            public myIntVec2 currentPixel = new myIntVec2();

            public ColorSetMethod method;

            public override bool SupportedByTex2D { get { return true; } }

            public override string ToString() { return "Pixel Reshape"; }

            public void FromUV(Vector2 uv)  {
                currentPixel.x = (int)Mathf.Floor(uv.x * Cfg.samplingMaskSize.x);
                currentPixel.y = (int)Mathf.Floor(uv.y * Cfg.samplingMaskSize.y);
            }
        
            public void FromColor(BrushConfig brush, Vector2 uv) {
                var c = brush.colorLinear.ToGamma();

                currentPixel.x = (int)Mathf.Floor((uv.x + (c.r - 0.5f) * 2) * Cfg.samplingMaskSize.x);
                currentPixel.y = (int)Mathf.Floor((uv.y + (c.g - 0.5f) * 2) * Cfg.samplingMaskSize.y);
            }
#if PEGI
        public override bool PEGI()
        {
            bool changed = base.PEGI();

            if (InspectedPainter == null)
                return changed;

            pegi.newLine();

            changed |= "Mask Size: ".edit(60, ref Cfg.samplingMaskSize).nl();

            Cfg.samplingMaskSize.Clamp(1, 512);

            changed |= "Color Set On".editEnum(ref method).nl();

            if (method == ColorSetMethod.Manual) {
                changed |= "CurrentPixel".edit(80, ref currentPixel).nl();

                currentPixel.Clamp(-Cfg.samplingMaskSize.max, Cfg.samplingMaskSize.max * 2);
            }

            var id = InspectedImageData;

            if (id != null)
            { 

            if ("Set Tiling Offset".Click()) {

                id.tiling = Vector2.one * 1.5f;
                id.offset = -Vector2.one * 0.25f;
                InspectedPainter.UpdateTylingToMaterial();
                changed = true;
            }

            if (InspectedPainter != null && "Generate Default".Click().nl())
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

                            for (int subx = 0; subx < dx; subx++) {
                                int ind = start + subx;

                                float offx = (center_uv_x - ((float)(startX + subx) / (float)id.width)) / 2f + 0.5f;

                                pix[ind].r = offx;
                                pix[ind].g = offy;
                            }
                        }

                    }

                id.SetAndApply(true);
                if (!id.TargetIsTexture2D())
                    id.Texture2D_To_RenderTexture();

            }
        }
            pegi.newLine();

                return changed;
            }
#endif
        public override void PrePaint(PlaytimePainter pntr, BrushConfig br, StrokeVector st) {

            var v4 = new Vector4(st.unRepeatedUV.x, st.unRepeatedUV.y, Mathf.Floor(st.unRepeatedUV.x), Mathf.Floor(st.unRepeatedUV.y));

            Shader.SetGlobalVector("_brushPointedUV_Untiled", v4);

            if (st.firstStroke)
            {
                
                    if (method == (ColorSetMethod.MDownColor)) {
                    if (pntr) {
                        pntr.SampleTexture(st.uvTo);
                        FromColor(br, st.unRepeatedUV);
                    }
                    }
                    else
                    if (method == (ColorSetMethod.MDownPosition))
                        FromUV(st.uvTo);
              
                Shader.SetGlobalVector(PainterConfig.BRUSH_SAMPLING_DISPLACEMENT, new Vector4(
                    ((float)currentPixel.x + 0.5f) / ((float)Cfg.samplingMaskSize.x),

                    ((float)currentPixel.y + 0.5f) / ((float)Cfg.samplingMaskSize.y),
                    
                    Cfg.samplingMaskSize.x, Cfg.samplingMaskSize.y));

            }
        }
        }

        public class BlitModeBloom : BlitMode
        {
            public override string ToString() { return "Bloom"; }
            protected override string ShaderKeyword { get { return "BRUSH_BLOOM"; } }

            public override bool ShowColorSliders { get { return false; } }
            public override bool SupportedBySingleBuffer { get { return false; } }
            public override bool SupportedByTex2D { get { return false; } }

        public BlitModeBloom(int ind) : base(ind) { }

        public override Shader ShaderForDoubleBuffer { get { return TexMGMT.br_BlurN_SmudgeBrush; } }
#if PEGI
            public override bool PEGI()
            {

                bool changed = base.PEGI().nl();
                changed |= "Bloom Radius".edit(70,ref InspectedBrush.blurAmount, 1f, 8f).nl();
                return changed;
            }
#endif
    }

}