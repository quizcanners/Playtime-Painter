using System.Collections;
using System.Collections.Generic;
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
    public class BrushConfig : PainterStuff_STD, IPEGI {

        public delegate bool BrushConfigPEGIplugin(ref bool overrideBlitModePEGI, BrushConfig br);
        public static BrushConfigPEGIplugin brushConfigPegies;

        #region Encode Decode
        public override StdEncoder Encode() {

            StdEncoder cody = new StdEncoder()
                .Add_Abstract("dyn", brushDynamic);

            return cody;
        }

        public StdEncoder EncodeStrokeFor(PlaytimePainter painter)
        {

            var id = painter.ImgMeta;

            bool rt = id.TargetIsRenderTexture();

            BlitMode mode = BlitMode;
            BrushType type = Type(!rt);

            bool worldSpace = rt && IsA3Dbrush(painter);

            StdEncoder cody = new StdEncoder()

            .Add(rt ? "typeGPU" : "typeCPU", _type(!rt));

            if (worldSpace)
                cody.Add("size3D", Brush3D_Radius);
            else
                cody.Add("size2D", Brush2D_Radius / ((float)id.width));


            cody.Add_Bool("useMask", useMask)
            .Add("mode", _bliTMode);

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

            cody.Add("hard", Hardness)
            .Add("speed", speed);

            return cody;
        }

        public override bool Decode(string tg, string data)
        {

            switch (tg)
            {
                case "typeGPU": inGPUtype = data.ToInt(); break;
                case "typeCPU": inCPUtype = data.ToInt(); break;
                case "size2D": Brush2D_Radius = data.ToFloat(); break;
                case "size3D": Brush3D_Radius = data.ToFloat(); break;

                case "useMask": useMask = data.ToBool(); break;

                case "mask": mask = (BrushMask)data.ToInt(); break;

                case "mode": _bliTMode = data.ToInt(); break;

                case "bc": colorLinear.Decode(data); break;

                case "source": selectedSourceTexture = data.ToInt(); break;

                case "blur": blurAmount = data.ToFloat(); break;

                case "decA": decalAngle = data.ToFloat(); break;
                case "decNo": selectedDecal = data.ToInt(); break;

                case "Smask": selectedSourceMask = data.ToInt(); break;
                case "maskTil": maskTiling = data.ToFloat(); break;
                case "maskFlip": flipMaskAlpha = data.ToBool(); break;

                case "hard": Hardness = data.ToFloat(); break;
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
        #endregion

        #region Modes & Types
        public int _bliTMode;
        public int _type(bool CPU) => CPU ? inCPUtype : inGPUtype;
        public void TypeSet(bool cpu, BrushType t) { if (cpu) inCPUtype = t.index; else inGPUtype = t.index; }
        public int inGPUtype;
        public int inCPUtype;

        public BrushType Type(PlaytimePainter pntr) => Type(!pntr ? TargetIsTex2D : pntr.ImgMeta.TargetIsTexture2D());

        public BrushType Type(bool CPU) => BrushType.AllTypes[_type(CPU)];

        public BlitMode BlitMode { get { return BlitMode.AllModes[_bliTMode]; } set { _bliTMode = value.index; } }

        #endregion

        public void SetSupportedFor(bool CPU, bool RTpair) {
            if (!CPU) {
                if (RTpair) {
                    if (!Type(CPU).SupportedByRenderTexturePair) foreach (var t in BrushType.AllTypes) { if (t.SupportedByRenderTexturePair) { TypeSet(CPU, t); break; } }
                    if (!BlitMode.SupportedByRenderTexturePair) foreach (var t in BlitMode.AllModes) { if (t.SupportedByRenderTexturePair) { BlitMode = t; break; } }
                } else
                {
                    if (!Type(CPU).SupportedBySingleBuffer) foreach (var t in BrushType.AllTypes) { if (t.SupportedBySingleBuffer) { TypeSet(CPU, t); break; } }
                    if (!BlitMode.SupportedBySingleBuffer) foreach (var t in BlitMode.AllModes) { if (t.SupportedBySingleBuffer) { BlitMode = t; break; } }
                }
            } else
            {
                if (!Type(CPU).SupportedByTex2D) foreach (var t in BrushType.AllTypes) { if (t.SupportedByTex2D) { TypeSet(CPU, t); break; } }
                if (!BlitMode.SupportedByTex2D) foreach (var t in BlitMode.AllModes) { if (t.SupportedByTex2D) { BlitMode = t; break; } }
            }
        }

        public float Size(bool worldSpace) => (worldSpace ? Brush3D_Radius : Brush2D_Radius);
        
        public int selectedSourceTexture = 0;
        public int selectedSourceMask = 0;
        public bool useMask = false;
        public bool previewDirty = false;
        [NonSerialized]
        public Vector2 maskOffset;
        public bool randomMaskOffset;

        public int selectedDecal = 0;
        public float maskTiling = 1;
        public float Hardness = 256;
        public float blurAmount = 1;
        public float decalAngle = 0;
        public DecalRotationMethod decalRotationMethod;
        public bool decalContinious = false;
        public float decalAngleModifier;
        public bool flipMaskAlpha = false;
        public bool TargetIsTex2D = false;
        public bool showBrushDynamics = false;

        public ElementData brushDunamicConfigs = new ElementData();

        public BrushDynamic brushDynamic = new BrushDynamic_None();

        public float Brush3D_Radius = 16;
        public float Brush2D_Radius = 16;

        public virtual bool IsA3Dbrush(PlaytimePainter pntr)
        {
            bool overrideOther = false;

            bool isA3d = false;

            if (pntr)
                foreach (var pl in PainterManagerPluginBase.brushPlugins)
                {
                    isA3d = pl.IsA3DBrush(pntr, this, ref overrideOther);
                    if (overrideOther) break;
                }

            if (!overrideOther)
                isA3d = Type(pntr).IsA3DBrush;

            return isA3d;
        }

        public float speed = 10;
        public bool MB1ToLinkPositions;
        public bool DontRedoMipmaps;

        public LinearColor colorLinear;

        public Color Color { get { return colorLinear.ToGamma(); } set { colorLinear.From(value); } }

        [NonSerialized]
        public Vector2 SampledUV;

        public BrushConfig() {
            colorLinear = new LinearColor(Color.green);
            mask = new BrushMask();
            mask |= BrushMask.R | BrushMask.G | BrushMask.B;
        }

        public bool PaintingAllChannels => BrushExtensions.HasFlag(mask, BrushMask.R) && BrushExtensions.HasFlag(mask, BrushMask.G) && BrushExtensions.HasFlag(mask, BrushMask.B) && BrushExtensions.HasFlag(mask, BrushMask.A);

        public bool PaintingRGB => BrushExtensions.HasFlag(mask, BrushMask.R) && BrushExtensions.HasFlag(mask, BrushMask.G) && BrushExtensions.HasFlag(mask, BrushMask.B) && (!BrushExtensions.HasFlag(mask, BrushMask.A));

        public PlaytimePainter Paint(StrokeVector stroke, PlaytimePainter pntr) {

            var imgData = pntr.ImgMeta;

            if (imgData == null) {
                pntr.InitIfNotInitialized();
                imgData = pntr.ImgMeta;
                if (imgData == null)
                    return pntr;
            }

            var cpu = imgData.TargetIsTexture2D();
            var brushType = Type(cpu);

            BlitMode.PrePaint(pntr, this, stroke);

            if (cpu) {
                pntr.RecordingMgmt();
                brushType.PaintToTexture2D(pntr, this, stroke);
            } else {

                var materialData = pntr.MatDta;

                if (!imgData.renderTexture  && !TexMGMT.materialsUsingTendTex.Contains(materialData)) {
                    TexMGMT.ChangeBufferTarget(imgData, materialData, pntr.GetMaterialTextureProperty, pntr);
                    pntr.SetTextureOnMaterial(imgData);
                }

                bool rendered = false;

                foreach (var pl in PainterManagerPluginBase.brushPlugins)
                    if (pl.PaintRenderTexture(stroke, imgData, this, pntr)) {
                        rendered = true;
                        break;
                    }

                if (!pntr.terrain || brushType.SupportedForTerrainRt) {

                    pntr.RecordingMgmt();

                    if (!rendered)
                        brushType.PaintRenderTexture(pntr, this, stroke);
                }
            }

            return pntr;
        }

        #region Inspector
        public static BrushConfig _inspectedBrush;
        public static bool InspectedIsCPUbrush => PlaytimePainter.inspected ? InspectedImageMeta.TargetIsTexture2D() : _inspectedBrush.TargetIsTex2D;
        #if PEGI
        public bool Mode_Type_PEGI()
        {
            PlaytimePainter p = PlaytimePainter.inspected;

            BrushType.AllTypes.ClampIndexToCount(ref inCPUtype);
            BrushType.AllTypes.ClampIndexToCount(ref inGPUtype);
            
            bool CPU = p ? p.ImgMeta.TargetIsTexture2D() : TargetIsTex2D;

            _inspectedBrush = this;
            bool changed = false;

            pegi.newLine();

            Msg.BlitMode.Write("How final color will be calculated", 80);

            var bm = BlitMode;

            changed |= pegi.select(ref _bliTMode, BlitMode.AllModes);

            pegi.newLine();
            pegi.space();
            pegi.newLine();

            if (!CPU) {
                Msg.BrushType.Write(80);
                changed |= pegi.select(ref inGPUtype, BrushType.AllTypes);
            }

            bool overrideBlitModePegi = false;

            if (brushConfigPegies != null)
                foreach (BrushConfigPEGIplugin pl in brushConfigPegies.GetInvocationList())
                    changed |= pl(ref overrideBlitModePegi, this).nl();
            
            if (p && !p.plugins.IsNullOrEmpty())
                foreach (var pl in p.plugins)
                    if (pl.BrushConfigPEGI().nl(ref changed)) 
                        pl.SetToDirty_Obj();
                    

            changed |= Type(CPU).Inspect().nl();

            if (!overrideBlitModePegi)
                changed |= BlitMode.PEGI();

            _inspectedBrush = null;

            return changed;
        }

        public bool Targets_PEGI()
        {
            bool changed = false;

            if ((TargetIsTex2D ? icon.CPU : icon.GPU).Click(
                TargetIsTex2D ? "Render Texture Config" : "Texture2D Config", ref changed ,45))
            {
                TargetIsTex2D = !TargetIsTex2D;
                SetSupportedFor(TargetIsTex2D, true);
            }

            bool smooth = Type(TargetIsTex2D) != BrushTypePixel.Inst;

            if ((TargetIsTex2D) && 
                pegi.toggle(ref smooth, icon.Round.GetIcon(), icon.Square.GetIcon(), "Smooth/Pixels Brush", 45).changes(ref changed))
                TypeSet(TargetIsTex2D, smooth ? (BrushType)BrushTypeNormal.Inst : (BrushType)BrushTypePixel.Inst);
            

            return changed;
        }

        public virtual bool Inspect() {

            PlaytimePainter p = PlaytimePainter.inspected;

            if (!p) {
                "No Painter Detected".nl();
                return false;
            }

            pegi.nl();

            if (p.skinnedMeshRenderer && "Update Collider from Skinned Mesh".Click().nl())
                p.UpdateColliderForSkinnedMesh();
            
            ImageMeta id = p.ImgMeta;

            bool changed = false;
            bool cpuBlit = id.destination == TexTarget.Texture2D;
            
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
                bool smooth = _type(cpuBlit) != BrushTypePixel.Inst.index;

                if (pegi.toggle(ref smooth, icon.Round, icon.Square, "Smooth/Pixels Brush", 45).changes(ref changed))
                    TypeSet(cpuBlit, smooth ? BrushTypeNormal.Inst : (BrushType)BrushTypePixel.Inst);
            }

            pegi.newLine();

            if (showBrushDynamics)
            {
                if ("Brush Dynamic".selectType( 90, ref brushDynamic, brushDunamicConfigs, true).nl(ref changed))

                if (brushDynamic != null)
                    brushDynamic.Nested_Inspect().nl();
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
                    pegi.writeWarning("Preview Shader is needed to see changes to terrain height.");

                pegi.nl();

                if (p.terrain && "Update Terrain".Click("Will Set Terrain texture as global shader values.").nl())
                    p.UpdateShaderGlobals();

            }

            return changed;
        }

        public bool ChannelSlider(BrushMask m, ref float channel)
        {
            pegi.write(m.GetIcon(), 25);
            return pegi.edit(ref channel, 0, 1).nl();
        }

        public bool ChannelSlider(BrushMask m, ref float chanel, Texture icon, bool slider) {

            bool changed = false;

            if (!icon)
                icon = m.GetIcon();

            string letter = m.ToText();
            bool maskVal = BrushExtensions.HasFlag(mask, m);

            if (InspectedPainter && InspectedPainter.meshEditing && MeshMGMT.MeshTool == VertexColorTool.inst) {

                var mat = InspectedPainter.Material;
                if (mat) {
                    var tag = mat.GetTag(PainterDataAndConfig.VertexColorRole + m.ToString(), false, null);
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

            bool changed = false;

            Color col = Color;
            if (pegi.edit(ref col).nl(ref changed))
                Color = col;
             
            if (Cfg.showColorSliders) {
                ChannelSlider(BrushMask.R, ref colorLinear.r, null, true).nl(ref changed);
                ChannelSlider(BrushMask.G, ref colorLinear.g, null, true).nl(ref changed);
                ChannelSlider(BrushMask.B, ref colorLinear.b, null, true).nl(ref changed);
                ChannelSlider(BrushMask.A, ref colorLinear.a, null, true).nl(ref changed);
            }

            return changed;
        }

        bool ColorSliders_PlaytimePainter() {

            if (!Cfg.showColorSliders)
                return false;
            
            bool changed = false;
            PlaytimePainter painter = PlaytimePainter.inspected;
            bool slider = BlitMode.ShowColorSliders;

            if (painter && painter.IsTerrainHeightTexture) {
                changed |= ChannelSlider(BrushMask.A, ref colorLinear.a, null, true);
            }
            else if (painter && painter.IsTerrainControlTexture)
            {
                ChannelSlider(BrushMask.R, ref colorLinear.r, painter.terrain.GetSplashPrototypeTexture(0), slider).nl(ref changed);
                ChannelSlider(BrushMask.G, ref colorLinear.g, painter.terrain.GetSplashPrototypeTexture(1), slider).nl(ref changed);
                ChannelSlider(BrushMask.B, ref colorLinear.b, painter.terrain.GetSplashPrototypeTexture(2), slider).nl(ref changed);
                ChannelSlider(BrushMask.A, ref colorLinear.a, painter.terrain.GetSplashPrototypeTexture(3), slider).nl(ref changed);
            }
            else
            {
                var id = painter.ImgMeta;
                if (id.TargetIsRenderTexture() && id.renderTexture)
                {
                    ChannelSlider(BrushMask.R, ref colorLinear.r).nl(ref changed);
                    ChannelSlider(BrushMask.G, ref colorLinear.g).nl(ref changed);
                    ChannelSlider(BrushMask.B, ref colorLinear.b).nl(ref changed);

                }
                else
                {

                    ChannelSlider(BrushMask.R, ref colorLinear.r, null, slider).nl(ref changed);
                    ChannelSlider(BrushMask.G, ref colorLinear.g, null, slider).nl(ref changed);
                    ChannelSlider(BrushMask.B, ref colorLinear.b, null, slider).nl(ref changed);

                    bool gotAlpha = painter.meshEditing || id.texture2D.TextureHasAlpha();

                    if (gotAlpha || id.preserveTransparency) {

                        if (!gotAlpha)
                            icon.Warning.write("Texture as no alpha, clicking save will fix it");
                        ChannelSlider(BrushMask.A, ref colorLinear.a, null, slider).nl(ref changed);
                    }
                }
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