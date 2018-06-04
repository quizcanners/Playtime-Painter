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
    public class BrushConfig : PainterStuff_STD 
    {

        public delegate bool BrushConfigPEGIplugin(ref bool overrideBlitModePEGI, BrushConfig br);
        public static BrushConfigPEGIplugin brushConfigPegies;

        public override stdEncoder Encode()
        {
            stdEncoder cody = new stdEncoder();

            // No use for this one yet

            Debug.Log("Brush is saved trough serializarion at the moment, but there is a function to provide stroke data");

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

                case linearColor.toryTag: colorLinear.Decode(data); break;

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

        public const string storyTag = "brush";
        public override string GetDefaultTagName() { return storyTag; }

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
        public void typeSet (bool CPU, BrushType t ) { if (CPU) inCPUtype = t.index; else  inGPUtype = t.index; }
        public int inGPUtype;
        public int inCPUtype;

        public void setSupportedFor (bool CPU, bool RTpair) {
            if (!CPU) {
                if (RTpair) {
                    if (!type(CPU).supportedByRenderTexturePair) foreach (var t in BrushType.allTypes) { if (t.supportedByRenderTexturePair) { typeSet(CPU,t); break; } }
                    if (!blitMode.supportedByRenderTexturePair) foreach (var t in BlitMode.allModes) { if (t.supportedByRenderTexturePair) { blitMode = t; break; } }
                } else
                {
                    if (!type(CPU).supportedBySingleBuffer) foreach (var t in BrushType.allTypes) { if (t.supportedBySingleBuffer) { typeSet(CPU, t); break; } }
                    if (!blitMode.supportedBySingleBuffer) foreach (var t in BlitMode.allModes) { if (t.supportedBySingleBuffer) { blitMode = t; break; } }
                }
            } else
            {
                if (!type(CPU).supportedByTex2D) foreach (var t in BrushType.allTypes) { if (t.supportedByTex2D) { typeSet(CPU, t); break; } }
                if (!blitMode.supportedByTex2D) foreach (var t in BlitMode.allModes) { if (t.supportedByTex2D) { blitMode = t; break; } }
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

        public BrushType type(PlaytimePainter pntr) {
            return pntr == null ? type(TargetIsTex2D) : type(pntr.imgData.TargetIsTexture2D());
        }

        public BrushType type(bool CPU) { 
            return BrushType.allTypes[_type(CPU)]; }

        public BlitMode blitMode { get { return BlitMode.allModes[_bliTMode]; } set { _bliTMode = value.index; } }

        public virtual bool IsA3Dbrush(PlaytimePainter pntr)
        {
            bool overrideOther = false;

            bool isA3d = false;

            if (pntr != null)
                foreach (var pl in texMGMT.plugins)
                {
                    isA3d = pl.isA3Dbrush(pntr, this, ref overrideOther);
                    if (overrideOther) break;
                }

            if (!overrideOther)
                isA3d = type(pntr).isA3DBrush;

            return isA3d;
        }
        
        public float speed = 10;
        public bool MB1ToLinkPositions;
        public bool DontRedoMipmaps;

        public linearColor colorLinear;

        [NonSerialized]
        public Vector2 SampledUV;

        public BrushConfig() {
            colorLinear = new linearColor(Color.green);
            mask = new BrushMask();
            mask |= BrushMask.R | BrushMask.G | BrushMask.B;
        }

        public bool paintingAllChannels { get { return mask.GetFlag(BrushMask.R) && mask.GetFlag(BrushMask.G) && mask.GetFlag(BrushMask.B) && mask.GetFlag(BrushMask.A); } }

        public bool paintingRGB { get { return mask.GetFlag(BrushMask.R) && mask.GetFlag(BrushMask.G) && mask.GetFlag(BrushMask.B) && (!mask.GetFlag(BrushMask.A)); } }
        
        public PlaytimePainter Paint(StrokeVector stroke, PlaytimePainter pntr)  {

            var id = pntr.imgData;

            if (id == null) {
                pntr.InitIfNotInited();
                id = pntr.imgData;
                if (id == null) return pntr;
            }
            
          
            var cpu = id.TargetIsTexture2D();
            var t = type(cpu);

            blitMode.PrePaint(pntr, this, stroke);

            if (cpu) {
                pntr.RecordingMGMT();
                t.PaintToTexture2D(pntr, this, stroke);
            } else {

                var md = pntr.matDta;

                if (id.renderTexture == null && !texMGMT.materialsUsingTendTex.Contains(md)) {
                    texMGMT.changeBufferTarget(id, md, pntr.MaterialTexturePropertyName, pntr);
                    //materialsUsingTendTex.Add(md);
                    pntr.SetTextureOnMaterial(id);
                    //Debug.Log("Adding RT target");
                }

                bool rendered = false;

                foreach (var pl in texMGMT.plugins)
                    if (pl.PaintRenderTexture(stroke, id, this, pntr))
                    {
                        rendered = true;
                        break;
                    }
                        
                if ((pntr.terrain != null) && (!t.supportedForTerrain_RT))
                    return pntr;
              
                    pntr.RecordingMGMT();

                if (!rendered)
                    t.PaintRenderTexture(pntr, this, stroke);
            }
            
            return pntr;
        }
        
        public static BrushConfig _inspectedBrush;
        public static bool inspectedIsCPUbrush { get{ return PlaytimePainter.inspectedPainter != null ? inspectedImageData.TargetIsTexture2D() : _inspectedBrush.TargetIsTex2D; } }
#if PEGI
        public bool Mode_Type_PEGI()
        {
            PlaytimePainter p = PlaytimePainter.inspectedPainter;

            inCPUtype = inCPUtype.ClampZeroTo(BrushType.allTypes.Count);
            inGPUtype = inGPUtype.ClampZeroTo(BrushType.allTypes.Count);

            bool CPU = p != null ? p.imgData.TargetIsTexture2D() : TargetIsTex2D;

            _inspectedBrush = this;
            bool changed = false;
            
            pegi.newLine();

            msg.BlitMode.write("How final color will be calculated", 80);

            var bm = blitMode;

            changed |= pegi.select(ref _bliTMode, BlitMode.allModes.ToArray(), true);

            pegi.newLine();
            pegi.Space();
            pegi.newLine();

            if (!CPU) {
                msg.BrushType.write(80);
                changed |= pegi.select<BrushType>(ref inGPUtype, BrushType.allTypes);
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

            changed |= type(CPU).PEGI().nl();

            if (!overrideBlitModePegi)
            changed |= blitMode.PEGI();

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
                setSupportedFor(TargetIsTex2D, true);
                changed = true;
            }

            bool smooth = type(TargetIsTex2D) != BrushTypePixel.inst;

            if ((TargetIsTex2D) && pegi.toggle(ref smooth, icon.Round.getIcon(), icon.Square.getIcon(), "Smooth/Pixels Brush", 45))
            {
                changed = true;
                typeSet(TargetIsTex2D,  smooth ? (BrushType)BrushTypeNormal.inst : (BrushType)BrushTypePixel.inst);
            }

            return changed;
        }

        public override bool PEGI()  {

            PlaytimePainter p = PlaytimePainter.inspectedPainter;

            if (p == null) { "No Painter Detected".nl(); return false; }

          

            if ((p.skinnedMeshRendy != null) && (pegi.Click("Update Collider from Skinned Mesh")))
                p.UpdateColliderForSkinnedMesh();
            pegi.newLine();


            ImageData id = p.imgData;

            bool changed = false;
            bool cpuBlit = id.destination == texTarget.Texture2D;

            pegi.newLine();

            changed |= p.PreviewShaderToggle_PEGI();

            if ((PainterManager.GotBuffers() || (id.renderTexture != null)) && (id.texture2D != null))
            {
                if ((cpuBlit ? icon.CPU : icon.GPU).Click(
                    cpuBlit ? "Switch to Render Texture" : "Switch to Texture2D", 45))
                {
                    p.UpdateOrSetTexTarget(cpuBlit ? texTarget.RenderTexture : texTarget.Texture2D);
                    setSupportedFor(cpuBlit, id.renderTexture == null);
                    changed = true;
                }
            }


            if (cpuBlit)
            {
                bool smooth = _type(cpuBlit) != BrushTypePixel.inst.index;

                if (pegi.toggle(ref smooth, icon.Round, icon.Square, "Smooth/Pixels Brush", 45))
                {
                    changed = true;
                    typeSet(cpuBlit,  smooth ? (BrushType)BrushTypeNormal.inst : (BrushType)BrushTypePixel.inst);
                }
            }

            pegi.newLine();
#if UNITY_EDITOR
            if (Tools.current != Tool.None)
            {
                "Lock to use Transform tools".writeWarning();
                if ("Hide Transform tool".Click().nl())
                    Tools.current = Tool.None;
            }
#endif

            if //(
                (!p.isOriginalShader)// && (cfg.moreOptions))
                changed |= pegi.toggle(ref cfg.previewAlphaChanel, "Preview Enabled Chanels", 130);




            if (Mode_Type_PEGI())
            {
                if (type(cpuBlit) == BrushTypeDecal.inst)
                    MaskSet(BrushMask.A, true);

                changed = true;
            }

            if (p.terrain != null)  {

                if ((p.imgData != null) && ((p.isTerrainHeightTexture())) && (p.isOriginalShader))
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
            if (icon == null)
                icon = m.getIcon();

            string letter = m.ToString();
            bool maskVal = mask.GetFlag(m);

            if (inspectedPainter != null && inspectedPainter.meshEditing && meshMGMT.meshTool == VertexColorTool.inst) {

                var mat = inspectedPainter.material;
                if (mat != null) {
                    var tag = mat.GetTag(PainterConfig.vertexColorRole + letter, false, null);
                    if (tag != null && tag.Length > 0)
                    {
                       
                        if (maskVal)
                            (tag+":").nl();
                        else
                            letter = tag+" ";
                    }

                    
                }

            }

            bool changed = false;
            
            if (maskVal ? pegi.Click(icon, 25) : pegi.Click(letter + " disabled")) {
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

            if (inspectedPainter != null)
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
            bool slider = blitMode.showColorSliders;

            if ((painter != null) && (painter.isTerrainHeightTexture())) {
                changed |= ChannelSlider(BrushMask.A, ref colorLinear.a, null, true);
            }
            else if ((painter != null) && painter.isTerrainControlTexture())
            {
                // Debug.Log("Is control texture");
                changed |= ChannelSlider(BrushMask.R, ref colorLinear.r, painter.terrain.getSplashPrototypeTexture(0), slider);
                changed |= ChannelSlider(BrushMask.G, ref colorLinear.g, painter.terrain.getSplashPrototypeTexture(1), slider);
                changed |= ChannelSlider(BrushMask.B, ref colorLinear.b, painter.terrain.getSplashPrototypeTexture(2), slider);
                changed |= ChannelSlider(BrushMask.A, ref colorLinear.a, painter.terrain.getSplashPrototypeTexture(3), slider);
            }
            else
            {
                var id = painter.imgData;
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

    }

}