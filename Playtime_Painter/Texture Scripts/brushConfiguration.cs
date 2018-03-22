using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using StoryTriggerData;
using PlayerAndEditorGUI;


namespace Playtime_Painter
{

    public static class BrushExtensions
    {
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
    public class BrushConfig : abstract_STD
    {

        public override stdEncoder Encode()
        {
            stdEncoder cody = new stdEncoder();

            // No use for this one yet

            Debug.Log("Brush is saved trough serializarion at the moment, but there is a function to provide stroke data");

            return cody;
        }

        public override void Decode(string tag, string data)
        {

            switch (tag)
            {
                case "typeRT": _type = data.ToInt(); break;

                case "size2D": Brush2D_Radius = data.ToFloat(); break;
                case "size3D": Brush3D_Radius = data.ToFloat(); break;

                case "useMask": useMask = data.ToBool(); break;

                case "mask": mask = (BrushMask)data.ToInt(); break;

                case "mode": _bliTMode = data.ToInt(); break;

                case linearColor.toryTag: colorLinear.Reboot(data); break;

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
            }


        }

        public const string storyTag = "brush";
        public override string getDefaultTagName() { return storyTag; }

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
        public int _type;

        public void setSupportedFor (bool CPU, bool RTpair) {
            if (!CPU) {
                if (RTpair) {
                    if (!type.supportedByRenderTexturePair) foreach (var t in BrushType.allTypes) { if (t.supportedByRenderTexturePair) { type = t; break; } }
                    if (!blitMode.supportedByRenderTexturePair) foreach (var t in BlitMode.allModes) { if (t.supportedByRenderTexturePair) { blitMode = t; break; } }
                } else
                {
                    if (!type.supportedBySingleBuffer) foreach (var t in BrushType.allTypes) { if (t.supportedBySingleBuffer) { type = t; break; } }
                    if (!blitMode.supportedBySingleBuffer) foreach (var t in BlitMode.allModes) { if (t.supportedBySingleBuffer) { blitMode = t; break; } }
                }
            } else
            {
                if (!type.supportedByTex2D) foreach (var t in BrushType.allTypes) { if (t.supportedByTex2D) { type = t; break; } }
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

        public BrushType type { get {
                _type = Mathf.Clamp(_type, 0, BrushType.allTypes.Count-1);
                return BrushType.allTypes[_type]; } set { _type = value.index; } }
        public BlitMode blitMode { get { BlitMode.pegibrush = this;


                return BlitMode.allModes[_bliTMode]; } set { _bliTMode = value.index; } }

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

        static PainterConfig cfg { get { return PainterConfig.inst; } }
        static PainterManager rtp { get { return PainterManager.inst; } }

        public bool Mode_PEGI(bool cpuBlit) {

            bool changed = false;
            pegi.newLine();

            msg.BlitMode.write("How final color will be calculated", 80);

            var bm = blitMode;

            changed |= pegi.select(ref _bliTMode, BlitMode.allModes.ToArray(), true);

            pegi.newLine();
            pegi.Space();
            pegi.newLine();

            return changed;
        }

        public bool Mode_Type_PEGI(bool cpuBlit)
        {
            bool changed = Mode_PEGI(cpuBlit);

            if (!cpuBlit)
            {
                msg.BrushType.write(80);
              
                changed |= pegi.select<BrushType>(ref _type, BrushType.allTypes);
               
                changed |= type.PEGI(this);
            }

            pegi.newLine();

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

            bool smooth = _type != BrushTypePixel.inst.index;

            if ((TargetIsTex2D) && pegi.toggle(ref smooth, icon.Round.getIcon(), icon.Square.getIcon(), "Smooth/Pixels Brush", 45))
            {
                changed = true;
                _type = smooth ? BrushTypeNormal.inst.index : BrushTypePixel.inst.index;
            }

            return changed;
        }

        public bool PEGI(PlaytimePainter painter)  {


            if ((painter.skinnedMeshRendy != null) && (pegi.Click("Update Collider from Skinned Mesh")))
                painter.UpdateColliderForSkinnedMesh();
            pegi.newLine();


            imgData id = painter.curImgData;

            bool changed = false;
            bool cpuBlit = id.destination == texTarget.Texture2D;

            pegi.newLine();

            changed |= painter.PreviewShaderToggle_PEGI();

            if ((PainterManager.GotBuffers() || (id.renderTexture != null)) && (id.texture2D != null))
            {
                if ((cpuBlit ? icon.CPU : icon.GPU).Click(
                    cpuBlit ? "Switch to Render Texture" : "Switch to Texture2D", 45))
                {
                    painter.UpdateOrSetTexTarget(cpuBlit ? texTarget.RenderTexture : texTarget.Texture2D);
                    setSupportedFor(cpuBlit, id.renderTexture == null);
                    changed = true;
                }
            }


            if (cpuBlit)
            {
                bool smooth = _type != BrushTypePixel.inst.index;

                if (pegi.toggle(ref smooth, icon.Round, icon.Square, "Smooth/Pixels Brush", 45))
                {
                    changed = true;
                    _type = smooth ? BrushTypeNormal.inst.index : BrushTypePixel.inst.index;
                }
            }

            pegi.newLine();
            if ((painter.originalShader != null) && (cfg.moreOptions))
                changed |= pegi.toggle(ref cfg.previewAlphaChanel, "Preview Enabled Chanels", 130);



            BrushType.painter = painter;

            if (Mode_Type_PEGI(cpuBlit))
            {
                if (type == BrushTypeDecal.inst)
                    MaskSet(BrushMask.A, true);

                changed = true;
            }

            BrushType.painter = null;


           BlitMode blitMode = BlitMode.getCurrentBlitModeForPainter(painter);

            if (painter.terrain != null)
            {

                if ((painter.curImgData != null) && ((painter.isTerrainHeightTexture())) && (painter.originalShader == null))
                    pegi.writeWarning(" You need to use Preview Shader to see changes");

                pegi.newLine();

                if ((painter.terrain != null) && (pegi.Click("Update Terrain").nl()))
                    painter.UpdateShaderGlobalsForTerrain();

            }

            changed |= blitMode.PEGI(this, painter);

            BlitMode mode = blitMode;
            imgData image = painter.curImgData;


            if ((mode.usingSourceTexture) && (image.TargetIsRenderTexture()))
            {
                if (rtp.sourceTextures.Length > 0)
                {
                    selectedSourceTexture = Mathf.Min(selectedSourceTexture, rtp.sourceTextures.Length - 1);
                    pegi.write("Copy From:", 70);
                    pegi.selectOrAdd(ref selectedSourceTexture, ref rtp.sourceTextures);
                }
                else
                    pegi.write("Add Textures to Render Camera to copy from");
            }

            pegi.newLine();

            return changed;
        }

        public bool ColorSliders_PEGI()
        {
            bool changed = false;

            changed |= ColorSlider(BrushMask.R, ref colorLinear.r, null, true);
            changed |= ColorSlider(BrushMask.G, ref colorLinear.g, null, true);
            changed |= ColorSlider(BrushMask.B, ref colorLinear.b, null, true);
            changed |= ColorSlider(BrushMask.A, ref colorLinear.a, null, true);

            return changed;
        }

        public bool ColorSlider(BrushMask m, ref float chanel)
        {
            pegi.write(m.getIcon(), 25);
            bool changed = pegi.edit(ref chanel, 0, 1);

            pegi.newLine();

            return changed;
        }

        public bool ColorSlider(BrushMask m, ref float chanel, Texture icon, bool slider)
        {
            if (icon == null)
                icon = m.getIcon();



            bool changed = false;
            bool maskVal = mask.GetFlag(m);
            if (maskVal ? pegi.Click(icon, 25) : pegi.Click(m.ToString() + " disabled"))
            {
                MaskToggle(m);
                changed = true;
            }

            if ((slider) && (mask.GetFlag(m)))
                changed |= pegi.edit(ref chanel, 0, 1);

            pegi.newLine();

            return changed;
        }

    }

}