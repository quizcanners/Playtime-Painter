using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace Playtime_Painter
{

    public static class BrushExtensions
    {

        public static bool GetFlag(this BrushMask mask, int flag)
        {
            return (mask & (BrushMask)(Mathf.Pow(2,flag))) != 0;
        }

        public static bool GetFlag(this BrushMask mask, BrushMask flag)
        {
            return (mask & flag) != 0;
        }
    }

   

    public enum DecalRotationMethod
    {
        Set, Random, StrokeDirection
    }

    [Serializable]
    public class BrushConfig : PainterStuff_STD , IPEGI
    {

        public delegate bool BrushConfigPEGIplugin(ref bool overrideBlitModePEGI, BrushConfig br);
        public static BrushConfigPEGIplugin brushConfigPegies;

        #region Encode Decode
        public override StdEncoder Encode()
        {
            StdEncoder cody = new StdEncoder();

            // No use for this one yet

            Debug.Log("Brush is saved trough serializarion at the moment, but there is a function to provide stroke data");

            return cody;
        }

        public StdEncoder EncodeStrokeFor(PlaytimePainter painter)
        {

            var id = painter.ImgData;

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

        public override bool Decode(string tag, string data)
        {

            switch (tag)
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
                // case "smooth": Smooth= data.ToBool(); break;
                case "maskOff": maskOffset = data.ToVector2(); break;
                default: return false;
            }
            return true;


        }
        #endregion

        public void MaskToggle(BrushMask flag)
        {
            mask ^= flag;
        }

        public void MaskSet(BrushMask flag, bool to)
        {
            if (to)
                mask |= flag;
            else
                mask &= ~flag;
        }

        public BrushMask mask;

        public int _bliTMode;
        public int _type (bool CPU) { return CPU ? inCPUtype : inGPUtype;}
        public void TypeSet (bool CPU, BrushType t ) { if (CPU) inCPUtype = t.index; else  inGPUtype = t.index; }
        public int inGPUtype;
        public int inCPUtype;

        public void SetSupportedFor (bool CPU, bool RTpair) {
            if (!CPU) {
                if (RTpair) {
                    if (!Type(CPU).SupportedByRenderTexturePair) foreach (var t in BrushType.AllTypes) { if (t.SupportedByRenderTexturePair) { TypeSet(CPU,t); break; } }
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

        public float repaintDelay = 0.016f;
        public int selectedSourceTexture = 0;
        public int selectedSourceMask = 0;
        public bool useMask = false;
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

        public float Brush3D_Radius = 16;
        public float Brush2D_Radius = 16;

        public float Size(bool worldSpace) { return (worldSpace ? Brush3D_Radius : Brush2D_Radius); }

        public BrushType Type(PlaytimePainter pntr) {
            return pntr == null ? Type(TargetIsTex2D) : Type(pntr.ImgData.TargetIsTexture2D());
        }

        public BrushType Type(bool CPU) { 
            return BrushType.AllTypes[_type(CPU)]; }

        public BlitMode BlitMode { get { return BlitMode.AllModes[_bliTMode]; } set { _bliTMode = value.index; } }

        public virtual bool IsA3Dbrush(PlaytimePainter pntr)
        {
            bool overrideOther = false;

            bool isA3d = false;

            if (pntr != null)
                foreach (var pl in TexMGMT.Plugins)
                {
                    isA3d = pl.IsA3Dbrush(pntr, this, ref overrideOther);
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

        [NonSerialized]
        public Vector2 SampledUV;

        public BrushConfig() {
            colorLinear = new LinearColor(Color.green);
            mask = new BrushMask();
            mask |= BrushMask.R | BrushMask.G | BrushMask.B;
        }

        public bool PaintingAllChannels { get { return mask.GetFlag(BrushMask.R) && mask.GetFlag(BrushMask.G) && mask.GetFlag(BrushMask.B) && mask.GetFlag(BrushMask.A); } }

        public bool PaintingRGB { get { return mask.GetFlag(BrushMask.R) && mask.GetFlag(BrushMask.G) && mask.GetFlag(BrushMask.B) && (!mask.GetFlag(BrushMask.A)); } }
        
        public PlaytimePainter Paint(StrokeVector stroke, PlaytimePainter pntr)  {

            var id = pntr.ImgData;

            if (id == null) {
                pntr.InitIfNotInited();
                id = pntr.ImgData;
                if (id == null) return pntr;
            }
            
          
            var cpu = id.TargetIsTexture2D();
            var t = Type(cpu);

            BlitMode.PrePaint(pntr, this, stroke);

            if (cpu) {
                pntr.RecordingMGMT();
                t.PaintToTexture2D(pntr, this, stroke);
            } else {

                var md = pntr.MatDta;

                if (id.renderTexture == null && !TexMGMT.materialsUsingTendTex.Contains(md)) {
                    TexMGMT.ChangeBufferTarget(id, md, pntr.GetMaterialTexturePropertyName, pntr);
                    //materialsUsingTendTex.Add(md);
                    pntr.SetTextureOnMaterial(id);
                    //Debug.Log("Adding RT target");
                }

                bool rendered = false;

                foreach (var pl in TexMGMT.Plugins)
                    if (pl.PaintRenderTexture(stroke, id, this, pntr))
                    {
                        rendered = true;
                        break;
                    }
                        
                if ((pntr.terrain != null) && (!t.SupportedForTerrain_RT))
                    return pntr;
              
                    pntr.RecordingMGMT();

                if (!rendered)
                    t.PaintRenderTexture(pntr, this, stroke);
            }
            
            return pntr;
        }

        #region Inspector
        public static BrushConfig _inspectedBrush;
        public static bool InspectedIsCPUbrush { get{ return PlaytimePainter.inspectedPainter != null ? InspectedImageData.TargetIsTexture2D() : _inspectedBrush.TargetIsTex2D; } }
#if PEGI
        public bool Mode_Type_PEGI()
        {
            PlaytimePainter p = PlaytimePainter.inspectedPainter;

            inCPUtype = inCPUtype.ClampZeroTo(BrushType.AllTypes.Count);
            inGPUtype = inGPUtype.ClampZeroTo(BrushType.AllTypes.Count);

            bool CPU = p != null ? p.ImgData.TargetIsTexture2D() : TargetIsTex2D;

            _inspectedBrush = this;
            bool changed = false;
            
            pegi.newLine();

            Msg.BlitMode.Write("How final color will be calculated", 80);

            var bm = BlitMode;

            changed |= pegi.select(ref _bliTMode, BlitMode.AllModes);

            pegi.newLine();
            pegi.Space();
            pegi.newLine();

            if (!CPU) {
                Msg.BrushType.Write(80);
                changed |= pegi.select<BrushType>(ref inGPUtype, BrushType.AllTypes);
            }

            bool overrideBlitModePegi = false;

            if (brushConfigPegies != null)
            foreach (BrushConfigPEGIplugin pl in brushConfigPegies.GetInvocationList())
                changed |= pl(ref overrideBlitModePegi, this).nl();
               

            if (p != null)
            foreach (var pl in p.plugins)
                if (pl.BrushConfigPEGI().nl())
                {
                    pl.SetToDirty();
                    changed = true;
                }

            changed |= Type(CPU).PEGI().nl();

            if (!overrideBlitModePegi)
            changed |= BlitMode.PEGI();

            _inspectedBrush = null;

            return changed;
        }
        
        public bool Targets_PEGI()
        {
            bool changed = false;

            if ((TargetIsTex2D ? icon.CPU : icon.GPU).Click(
                TargetIsTex2D ? "Render Texture Config" : "Texture2D Config", 45))
            {
                TargetIsTex2D = !TargetIsTex2D;
                SetSupportedFor(TargetIsTex2D, true);
                changed = true;
            }

            bool smooth = Type(TargetIsTex2D) != BrushTypePixel.Inst;

            if ((TargetIsTex2D) && pegi.toggle(ref smooth, icon.Round.getIcon(), icon.Square.getIcon(), "Smooth/Pixels Brush", 45))
            {
                changed = true;
                TypeSet(TargetIsTex2D,  smooth ? (BrushType)BrushTypeNormal.Inst : (BrushType)BrushTypePixel.Inst);
            }

            return changed;
        }

        public virtual bool PEGI()  {

            PlaytimePainter p = PlaytimePainter.inspectedPainter;

            if (p == null) { "No Painter Detected".nl(); return false; }
            
            if ((p.skinnedMeshRendy != null) && (pegi.Click("Update Collider from Skinned Mesh")))
                p.UpdateColliderForSkinnedMesh();
            pegi.newLine();


            ImageData id = p.ImgData;

            bool changed = false;
            bool cpuBlit = id.destination == TexTarget.Texture2D;

            pegi.newLine();

            changed |= p.PreviewShaderToggle_PEGI();

            if ((PainterCamera.GotBuffers() || (id.renderTexture != null)) && (id.texture2D != null))
            {
                if ((cpuBlit ? icon.CPU : icon.GPU).Click(
                    cpuBlit ? "Switch to Render Texture" : "Switch to Texture2D", 45))
                {
                    p.UpdateOrSetTexTarget(cpuBlit ? TexTarget.RenderTexture : TexTarget.Texture2D);
                    SetSupportedFor(cpuBlit, id.renderTexture == null);
                    changed = true;
                }
            }


            if (cpuBlit)
            {
                bool smooth = _type(cpuBlit) != BrushTypePixel.Inst.index;

                if (pegi.toggle(ref smooth, icon.Round, icon.Square, "Smooth/Pixels Brush", 45))
                {
                    changed = true;
                    TypeSet(cpuBlit,  smooth ? (BrushType)BrushTypeNormal.Inst : (BrushType)BrushTypePixel.Inst);
                }
            }

            pegi.newLine();
#if UNITY_EDITOR
            if (Tools.current != Tool.None)
            {
                Msg.LockToolToUseTransform.Get().writeWarning();
                if (Msg.HideTransformTool.Get().Click().nl())
                    PlaytimePainter.HideUnityTool();
            }
#endif


            if (Mode_Type_PEGI())
            {
                if (Type(cpuBlit) == BrushTypeDecal.Inst)
                    MaskSet(BrushMask.A, true);

                changed = true;
            }

            if (p.terrain != null)  {

                if ((p.ImgData != null) && ((p.IsTerrainHeightTexture())) && (p.IsOriginalShader))
                    pegi.writeWarning(" You need to use Preview Shader to see changes");

                pegi.newLine();

                if ((p.terrain != null) && (pegi.Click("Update Terrain").nl()))
                    p.UpdateShaderGlobals();

            }

            return changed;
        }

        public bool ChannelSlider(BrushMask m, ref float channel)
        {
            pegi.write(m.getIcon(), 25);
            bool changed = pegi.edit(ref channel, 0, 1).nl();
            return changed;
        }

        public bool ChannelSlider(BrushMask m, ref float chanel, Texture icon, bool slider) {

            bool changed = false;

            if (icon == null)
                icon = m.getIcon();

            string letter = m.ToText();
            bool maskVal = mask.GetFlag(m);

            if (InspectedPainter != null && InspectedPainter.meshEditing && MeshMGMT.MeshTool == VertexColorTool.inst) {

                var mat = InspectedPainter.Material;
                if (mat != null) {
                    var tag = mat.GetTag(PainterDataAndConfig.vertexColorRole + m.ToString(), false, null);
                    if (tag != null && tag.Length > 0) {
                       
                        if (maskVal)
                            (tag+":").nl();
                        else
                            letter = tag+" ";
                    }

                    
                }

            }
            
            if (maskVal ? icon.Click(letter) : "{0} channel disabled".F(letter).toggleIcon(ref maskVal)) {
                MaskToggle(m);
                changed = true;
            }

            if ((slider) && (mask.GetFlag(m)))
                changed |= pegi.edit(ref chanel, 0, 1);

            pegi.newLine();

            return changed;
        }

        public bool ColorSliders_PEGI()
        {

            if (InspectedPainter != null)
                return ColorSliders();

            bool changed = false;

            Color col = colorLinear.ToGamma();
            if (pegi.edit(ref col).nl())
            {
                colorLinear.From(col);
                changed = true;
            }
            
            changed |= ChannelSlider(BrushMask.R, ref colorLinear.r, null, true);
            changed |= ChannelSlider(BrushMask.G, ref colorLinear.g, null, true);
            changed |= ChannelSlider(BrushMask.B, ref colorLinear.b, null, true);
            changed |= ChannelSlider(BrushMask.A, ref colorLinear.a, null, true);

            return changed;
        }

        bool ColorSliders( ) {
            bool changed = false;
            PlaytimePainter painter = PlaytimePainter.inspectedPainter;
            bool slider = BlitMode.ShowColorSliders;

            if ((painter != null) && (painter.IsTerrainHeightTexture())) {
                changed |= ChannelSlider(BrushMask.A, ref colorLinear.a, null, true);
            }
            else if ((painter != null) && painter.IsTerrainControlTexture())
            {
                // Debug.Log("Is control texture");
                changed |= ChannelSlider(BrushMask.R, ref colorLinear.r, painter.terrain.GetSplashPrototypeTexture(0), slider);
                changed |= ChannelSlider(BrushMask.G, ref colorLinear.g, painter.terrain.GetSplashPrototypeTexture(1), slider);
                changed |= ChannelSlider(BrushMask.B, ref colorLinear.b, painter.terrain.GetSplashPrototypeTexture(2), slider);
                changed |= ChannelSlider(BrushMask.A, ref colorLinear.a, painter.terrain.GetSplashPrototypeTexture(3), slider);
            }
            else
            {
                var id = painter.ImgData;
                if ((id.TargetIsRenderTexture()) && (id.renderTexture != null))
                {
                    changed |= ChannelSlider(BrushMask.R, ref colorLinear.r);
                    changed |= ChannelSlider(BrushMask.G, ref colorLinear.g);
                    changed |= ChannelSlider(BrushMask.B, ref colorLinear.b);

                }
                else
                {

                    changed |= ChannelSlider(BrushMask.R, ref colorLinear.r, null, slider);
                    changed |= ChannelSlider(BrushMask.G, ref colorLinear.g, null, slider);
                    changed |= ChannelSlider(BrushMask.B, ref colorLinear.b, null, slider);
                    changed |= ChannelSlider(BrushMask.A, ref colorLinear.a, null, slider);
                }
            }
            return changed;
        }
#endif
        #endregion
    }

}