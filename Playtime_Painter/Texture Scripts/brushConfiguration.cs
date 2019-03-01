using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace Playtime_Painter {

    public static class BrushExtensions {
        public static bool HasFlag(this BrushMask mask, int flag) => (mask & (BrushMask)(Mathf.Pow(2, flag))) != 0;

        public static bool HasFlag(this BrushMask mask, BrushMask flag) => (mask & flag) != 0;
    }

    public enum DecalRotationMethod { Set, Random, StrokeDirection }

    [Serializable]
    public class BrushConfig : PainterStuffStd, IPEGI {

        #region Encode Decode
        public override StdEncoder Encode() {

            StdEncoder cody = new StdEncoder()
                .Add_Abstract("dyn", brushDynamic);

            return cody;
        }

        public StdEncoder EncodeStrokeFor(PlaytimePainter painter)
        {

            var id = painter.ImgMeta;

            var rt = id.TargetIsRenderTexture();

            var mode = BlitMode;
            var type = Type(!rt);

            var worldSpace = rt && IsA3DBrush(painter);

            var cody = new StdEncoder()

            .Add(rt ? "typeGPU" : "typeCPU", _type(!rt));

            if (worldSpace)
                cody.Add("size3D", brush3DRadius);
            else
                cody.Add("size2D", brush2DRadius / ((float)id.width));


            cody.Add_Bool("useMask", useMask)
            .Add("mode", blitMode);

            if (useMask)
                cody.Add("mask", (int)mask);

            cody.Add("bc", colorLinear);

            if (mode.UsingSourceTexture)
                cody.Add("source", selectedSourceTexture);

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
            .Add("speed", speed);

            return cody;
        }

        public override bool Decode(string tg, string data)
        {

            switch (tg)
            {
                case "typeGPU": inGpuType = data.ToInt(); break;
                case "typeCPU": inCpuType = data.ToInt(); break;
                case "size2D": brush2DRadius = data.ToFloat(); break;
                case "size3D": brush3DRadius = data.ToFloat(); break;

                case "useMask": useMask = data.ToBool(); break;

                case "mask": mask = (BrushMask)data.ToInt(); break;

                case "mode": blitMode = data.ToInt(); break;

                case "bc": colorLinear.Decode(data); break;

                case "source": selectedSourceTexture = data.ToInt(); break;

                case "blur": blurAmount = data.ToFloat(); break;

                case "decA": decalAngle = data.ToFloat(); break;
                case "decNo": selectedDecal = data.ToInt(); break;

                case "Smask": selectedSourceMask = data.ToInt(); break;
                case "maskTil": maskTiling = data.ToFloat(); break;
                case "maskFlip": flipMaskAlpha = data.ToBool(); break;

                case "hard": hardness = data.ToFloat(); break;
                case "speed": speed = data.ToFloat(); break;
                case "dyn": data.DecodeInto(out brushDynamic, BrushDynamic.all); break;

                case "maskOff": maskOffset = data.ToVector2(); break;
                default: return false;
            }
            return true;


        }
        #endregion

        #region Brush Mask
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
        
        public int selectedSourceMask;

        public bool maskFromGreyscale;
        #endregion

        #region Modes & Types
        public int blitMode;
        private int _type(bool cpu) => cpu ? inCpuType : inGpuType;
        public void TypeSet(bool cpu, BrushType t) { if (cpu) inCpuType = t.index; else inGpuType = t.index; }
        public int inGpuType;
        public int inCpuType;

        public BrushType Type(PlaytimePainter painter) => Type(painter ? painter.ImgMeta.TargetIsTexture2D() : targetIsTex2D);

        public BrushType Type(bool cpu) => BrushType.AllTypes[_type(cpu)];

        public BlitMode BlitMode { get { return BlitMode.AllModes[blitMode]; } set { blitMode = value.index; } }

        #endregion

        private void SetSupportedFor(bool cpu, bool rtDoubleBuffer) {
            if (!cpu) {
                if (rtDoubleBuffer) {
                    if (!Type(cpu).SupportedByRenderTexturePair) foreach (var t in BrushType.AllTypes) { if (t.SupportedByRenderTexturePair) { TypeSet(cpu, t); break; } }
                    if (!BlitMode.SupportedByRenderTexturePair) foreach (var t in BlitMode.AllModes) { if (t.SupportedByRenderTexturePair) { BlitMode = t; break; } }
                } else {
                    if (!Type(cpu).SupportedBySingleBuffer) foreach (var t in BrushType.AllTypes) { if (t.SupportedBySingleBuffer) { TypeSet(cpu, t); break; } }
                    if (!BlitMode.SupportedBySingleBuffer) foreach (var t in BlitMode.AllModes) { if (t.SupportedBySingleBuffer) { BlitMode = t; break; } }
                }
            } else
            {
                if (!Type(cpu).SupportedByTex2D) foreach (var t in BrushType.AllTypes) { if (t.SupportedByTex2D) { TypeSet(cpu, t); break; } }
                if (!BlitMode.SupportedByTex2D) foreach (var t in BlitMode.AllModes) { if (t.SupportedByTex2D) { BlitMode = t; break; } }
            }
        }

        public float Size(bool worldSpace) => (worldSpace ? brush3DRadius : brush2DRadius);
        
        public int selectedSourceTexture;
        public bool useMask;
        public bool previewDirty = false;
        [NonSerialized]
        public Vector2 maskOffset;
        public bool randomMaskOffset;
        
        public int selectedDecal;
        public float maskTiling = 1;
        public float hardness = 256;
        public float blurAmount = 1;
        public float decalAngle;
        public DecalRotationMethod decalRotationMethod;
        public bool decalContentious;
        public float decalAngleModifier;
        public bool flipMaskAlpha;
        public bool targetIsTex2D;
        public bool showBrushDynamics;

        public ElementData brushDynamicsConfigs = new ElementData();

        public BrushDynamic brushDynamic = new BrushDynamic_None();

        public float brush3DRadius = 16;
        public float brush2DRadius = 16;
        
        public virtual bool IsA3DBrush(PlaytimePainter painter)
        {
            var overrideOther = false;

            var isA3D = false;

            if (painter)
                foreach (var pl in PainterManagerPluginBase.BrushPlugins)
                {
                    isA3D = pl.IsA3DBrush(painter, this, ref overrideOther);
                    if (overrideOther) break;
                }

            if (!overrideOther)
                isA3D = Type(painter).IsA3DBrush;

            return isA3D;
        }

        public float speed = 10;
        public bool mb1ToLinkPositions;
        public bool dontRedoMipMaps;

        public LinearColor colorLinear;

        public Color Color { get { return colorLinear.ToGamma(); } set { colorLinear.From(value); } }

        [NonSerialized]
        public Vector2 sampledUv;

        public BrushConfig() {
            colorLinear = new LinearColor(Color.green);
            mask = new BrushMask();
            mask |= BrushMask.R | BrushMask.G | BrushMask.B;
        }

        public bool PaintingAllChannels => BrushExtensions.HasFlag(mask, BrushMask.R) && BrushExtensions.HasFlag(mask, BrushMask.G) && BrushExtensions.HasFlag(mask, BrushMask.B) && BrushExtensions.HasFlag(mask, BrushMask.A);

        public bool PaintingRGB => BrushExtensions.HasFlag(mask, BrushMask.R) && BrushExtensions.HasFlag(mask, BrushMask.G) && BrushExtensions.HasFlag(mask, BrushMask.B) && (!BrushExtensions.HasFlag(mask, BrushMask.A));

        public PlaytimePainter Paint(StrokeVector stroke, PlaytimePainter painter) {

            var imgData = painter.ImgMeta;

            if (imgData == null) {
                painter.InitIfNotInitialized();
                imgData = painter.ImgMeta;
                if (imgData == null)
                    return painter;
            }

            var cpu = imgData.TargetIsTexture2D();
            var brushType = Type(cpu);

            BlitMode.PrePaint(painter, this, stroke);

            if (cpu) {
                painter.RecordingMgmt();
                brushType.PaintToTexture2D(painter, this, stroke);
            } else {

                var materialData = painter.MatDta;

                if (!imgData.renderTexture  && !TexMGMT.materialsUsingTendTex.Contains(materialData)) {
                    TexMGMT.ChangeBufferTarget(imgData, materialData, painter.GetMaterialTextureProperty, painter);
                    painter.SetTextureOnMaterial(imgData);
                }

                var rendered = false;

                foreach (var pl in PainterManagerPluginBase.BrushPlugins)
                    if (pl.PaintRenderTexture(stroke, imgData, this, painter)) {
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
        public static BrushConfig _inspectedBrush;
        public static bool InspectedIsCpuBrush => PlaytimePainter.inspected ? InspectedImageMeta.TargetIsTexture2D() : _inspectedBrush.targetIsTex2D;
        #if PEGI
        public bool Mode_Type_PEGI()
        {
            var p = PlaytimePainter.inspected;

            BrushType.AllTypes.ClampIndexToCount(ref inCpuType);
            BrushType.AllTypes.ClampIndexToCount(ref inGpuType);
            
            _inspectedBrush = this;
            var changed = false;

            pegi.newLine();

            Msg.BlitMode.Write("How final color will be calculated", 70);

            pegi.select(ref blitMode, BlitMode.AllModes).changes(ref changed);

            BlitMode?.ToolTip.fullWindowDocumentationClick("About this blit mode", 20).nl();

            pegi.space();
            pegi.newLine();
            
            var cpu = p ? p.ImgMeta.TargetIsTexture2D() : targetIsTex2D;
            
            if (!cpu) {
                Msg.BrushType.Write(80);
                pegi.select(ref inGpuType, BrushType.AllTypes).changes(ref changed);

                Type(p)?.ToolTip.fullWindowDocumentationClick("About this brush type", 20);

            }

            var overrideBlitModePegi = false;

            foreach (var b in PainterManagerPluginBase.BrushPlugins)
                b.BrushConfigPEGI(ref overrideBlitModePegi, this).nl(ref changed);
                          
            if (p && !p.plugins.IsNullOrEmpty())
                foreach (var pl in p.plugins)
                    if (pl.BrushConfigPEGI().nl(ref changed)) 
                        pl.SetToDirty_Obj();
                    

            Type(cpu).Inspect().nl(ref changed);

            if (!overrideBlitModePegi && BlitMode.ShowInDropdown())
                BlitMode.Inspect().nl(ref changed);

            _inspectedBrush = null;

            return changed;
        }

        public bool Targets_PEGI()
        {
            bool changed = false;

            if ((targetIsTex2D ? icon.CPU : icon.GPU).Click(
                targetIsTex2D ? "Render Texture Config" : "Texture2D Config", ref changed ,45))
            {
                targetIsTex2D = !targetIsTex2D;
                SetSupportedFor(targetIsTex2D, true);
            }

            bool smooth = Type(targetIsTex2D) != BrushTypePixel.Inst;

            if ((targetIsTex2D) && 
                pegi.toggle(ref smooth, icon.Round.GetIcon(), icon.Square.GetIcon(), "Smooth/Pixels Brush", 45).changes(ref changed))
                TypeSet(targetIsTex2D, smooth ? (BrushType)BrushTypeNormal.Inst : (BrushType)BrushTypePixel.Inst);
            

            return changed;
        }

        public virtual bool Inspect() {

            var p = PlaytimePainter.inspected;

            if (!p) {
                "No Painter Detected".nl();
                return false;
            }

            pegi.nl();

            if (p.skinnedMeshRenderer && "Update Collider from Skinned Mesh".Click().nl())
                p.UpdateColliderForSkinnedMesh();
            
            var id = p.ImgMeta;

            var changed = false;
            var cpuBlit = id.destination == TexTarget.Texture2D;
            
            changed |= p.PreviewShaderToggle_PEGI();

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
                var smooth = _type(cpuBlit) != BrushTypePixel.Inst.index;

                if (pegi.toggle(ref smooth, icon.Round, icon.Square, "Smooth/Pixels Brush", 45).changes(ref changed))
                    TypeSet(cpuBlit, smooth ? BrushTypeNormal.Inst : (BrushType)BrushTypePixel.Inst);
            }

            pegi.newLine();

            if (showBrushDynamics)
            {
                if ("Brush Dynamic".selectType( 90, ref brushDynamic, brushDynamicsConfigs, true).nl(ref changed))
                    brushDynamic?.Nested_Inspect().nl();
            }
            else
                brushDynamic.AllTypes.Replace_IfDifferent(ref brushDynamic, typeof(BrushDynamic_None));

#if UNITY_EDITOR
            if (Tools.current != Tool.None) {
                Msg.LockToolToUseTransform.Get().writeWarning();
                if (Msg.HideTransformTool.Get().Click().nl())
                    UnityHelperFunctions.HideUnityTool();
            }
#endif


            if (Mode_Type_PEGI().changes(ref changed) && Type(cpuBlit) == BrushTypeDecal.Inst)
                    MaskSet(BrushMask.A, true);

            if (p.terrain) {

                if (p.ImgMeta != null && p.IsTerrainHeightTexture && p.IsOriginalShader)
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
            var maskVal = BrushExtensions.HasFlag(mask, m);

            if (InspectedPainter && InspectedPainter.meshEditing && MeshMGMT.MeshTool == VertexColorTool.inst) {

                var mat = InspectedPainter.Material;
                if (mat) {
                    var tag = mat.GetTag(PainterDataAndConfig.VertexColorRole + m, false, null);
                    if (!tag.IsNullOrEmpty()) {

                        if (maskVal)
                            (tag + ":").nl();
                        else
                            letter = tag + " ";
                    }
                }
            }

            if (maskVal ? icon.Click(letter) : "{0} channel disabled".F(letter).toggleIcon(ref maskVal, true).changes(ref changed)) 
                MaskToggle(m);
            
            if (slider && BrushExtensions.HasFlag(mask, m))
                pegi.edit(ref chanel, 0, 1).nl(ref changed);



            return changed;
        }

        public bool ColorSliders() {

            if (InspectedPainter)
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

            var changed = false;
            var painter = PlaytimePainter.inspected;
            var id = painter.ImgMeta;

            if (Cfg.showColorSliders) {

             
                var slider = BlitMode.ShowColorSliders;

                if (painter && painter.IsTerrainHeightTexture)
                {
                    ChannelSlider(BrushMask.A, ref colorLinear.a, null, true).changes(ref changed);
                }
                else if (painter && painter.IsTerrainControlTexture)
                {
                    ChannelSlider(BrushMask.R, ref colorLinear.r, painter.terrain.GetSplashPrototypeTexture(0), slider)
                        .nl(ref changed);
                    ChannelSlider(BrushMask.G, ref colorLinear.g, painter.terrain.GetSplashPrototypeTexture(1), slider)
                        .nl(ref changed);
                    ChannelSlider(BrushMask.B, ref colorLinear.b, painter.terrain.GetSplashPrototypeTexture(2), slider)
                        .nl(ref changed);
                    ChannelSlider(BrushMask.A, ref colorLinear.a, painter.terrain.GetSplashPrototypeTexture(3), slider)
                        .nl(ref changed);
                }
                else
                {
                   
                    if (id.TargetIsRenderTexture() && id.renderTexture)
                    {
                        ChannelSlider(BrushMask.R, ref colorLinear.r).nl(ref changed);
                        ChannelSlider(BrushMask.G, ref colorLinear.g).nl(ref changed);
                        ChannelSlider(BrushMask.B, ref colorLinear.b).nl(ref changed);

                    }
                    else
                    {

                        if (!id.isATransparentLayer || colorLinear.a > 0)  {
                            ChannelSlider(BrushMask.R, ref colorLinear.r, null, slider).nl(ref changed);
                            ChannelSlider(BrushMask.G, ref colorLinear.g, null, slider).nl(ref changed);
                            ChannelSlider(BrushMask.B, ref colorLinear.b, null, slider).nl(ref changed);
                        }
                        
                        var gotAlpha = painter.meshEditing || id.texture2D.TextureHasAlpha();

                        if ((gotAlpha || id.preserveTransparency) && !id.isATransparentLayer) {
                            if (!gotAlpha)
                                icon.Warning.write("Texture as no alpha, clicking save will fix it");

                            ChannelSlider(BrushMask.A, ref colorLinear.a, null, slider).nl(ref changed);
                        }
                    }
                }
            }

            if (id.isATransparentLayer) {

                var erase = colorLinear.a < 0.5f;

                "Erase".toggleIcon(ref erase).nl(ref changed);

                colorLinear.a = erase ? 0 : 1;

            }

            return changed;
        }
        #endif
        #endregion
    }
    
    public class BrushDynamicAttribute : AbstractWithTaggedTypes  {
        public override TaggedTypesStd TaggedTypes => BrushDynamic.all;
    }

    [BrushDynamic]
    public abstract class BrushDynamic : AbstractStd, IPEGI, IGotClassTag {
        public abstract string ClassTag { get; }

        public static TaggedTypesStd all = new TaggedTypesStd(typeof(BrushDynamic));
        public TaggedTypesStd AllTypes => all;

        public override bool Decode(string tg, string data) {
            switch (tg) {
                case "t": testValue = data.ToInt(); break;
                default: return false;
            }

            return true;
        }

        public override StdEncoder Encode() => new StdEncoder().Add("t", testValue);

        #region Inspector
        int testValue = -1;

#if PEGI
        public bool Inspect()
        {
            bool changed = false;

            changed |= "Test Value".edit(60, ref testValue).nl();

            return changed;
        }
#endif
        #endregion
    }

    [TaggedType(classTag, "None")]
    public class BrushDynamic_None : BrushDynamic {
        const string classTag = "none";

        public override string ClassTag => classTag;
    }

    #region Size to Speed Dynamic
    [TaggedType(classTag, "Size from Speed")]
    public class SpeedToSize : BrushDynamic {
        const string classTag = "sts";

        public override string ClassTag => classTag;

//        public override bool Decode(string tag, string data) => true;

  //      public override StdEncoder Encode() => new StdEncoder();
    }
    #endregion

}