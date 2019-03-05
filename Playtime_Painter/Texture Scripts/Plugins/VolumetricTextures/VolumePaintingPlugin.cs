
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using Unity.Jobs;
using Unity.Collections;
using System;

namespace Playtime_Painter
{

    [TaggedType(tag)]
    public class VolumePaintingPlugin : PainterSystemManagerPluginBase, IGotDisplayName,
        IPainterManagerPluginComponentPEGI, IPainterManagerPluginBrush, IPainterManagerPluginGizmis
    {

        const string tag = "VolumePntng";
        public override string ClassTag => tag;

        public static ShaderProperty.VectorValue VOLUME_H_SLICES = new ShaderProperty.VectorValue("VOLUME_H_SLICES");
        public static ShaderProperty.VectorValue VOLUME_POSITION_N_SIZE = new ShaderProperty.VectorValue("VOLUME_POSITION_N_SIZE");

        public static ShaderProperty.VectorValue VOLUME_H_SLICES_Global = new ShaderProperty.VectorValue( PainterDataAndConfig.GlobalPropertyPrefix + "VOLUME_H_SLICES");
        public static ShaderProperty.VectorValue VOLUME_POSITION_N_SIZE_Global = new ShaderProperty.VectorValue(PainterDataAndConfig.GlobalPropertyPrefix + "VOLUME_POSITION_N_SIZE");


        public static ShaderProperty.VectorValue VOLUME_H_SLICES_BRUSH = new ShaderProperty.VectorValue("VOLUME_H_SLICES_BRUSH");
        public static ShaderProperty.VectorValue VOLUME_POSITION_N_SIZE_BRUSH = new ShaderProperty.VectorValue("VOLUME_POSITION_N_SIZE_BRUSH");
        public const string VolumeTextureTag = "_VOL";
        public const string VolumeSlicesCountTag = "_slices";

        private bool _useGrid;

        private static Shader _preview;
        private static Shader _brush;

        #region Encode & Decode

        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add_Bool("ug", _useGrid);

        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "ug": _useGrid = data.ToBool(); break;
                default: return false;
            }
            return true;
        }

        #endregion

        public static VolumePaintingPlugin _inst;

        public override void Enable()
        {
            base.Enable();
            _inst = this;

            if (_preview == null)
                _preview = Shader.Find("Playtime Painter/Editor/Preview/Volume");

            if (_brush == null)
                _brush = Shader.Find("Playtime Painter/Editor/Brush/Volume");          
        }
        
        public Shader GetPreviewShader(PlaytimePainter p) => p.GetVolumeTexture() != null ? _preview : null;
        
        public Shader GetBrushShaderDoubleBuffer(PlaytimePainter p) => p.GetVolumeTexture() != null ? _brush : null;

        public Shader GetBrushShaderSingleBuffer(PlaytimePainter p) => null;

        public bool NeedsGrid(PlaytimePainter p) => _useGrid && p.GetVolumeTexture() != null;
        
        public bool IsA3DBrush(PlaytimePainter painter, BrushConfig bc, ref bool overrideOther)
        {
            if (!painter.GetVolumeTexture()) return false;
            overrideOther = true;
            return true;
        }

        public bool PaintPixelsInRam(StrokeVector stroke, float brushAlpha, ImageMeta image, BrushConfig bc, PlaytimePainter painter)
        {

            var volume = image.texture2D.GetVolumeTextureData();

            if (!volume) return false;
            
            if (volume.VolumeJobIsRunning)
                return false;

            var volumeScale = volume.size;

            var pos = (stroke.posFrom - volume.transform.position) / volumeScale + 0.5f * Vector3.one;

            var height = volume.Height;
            var texWidth = image.width;

            BlitFunctions.brAlpha = brushAlpha;
            bc.PrepareCpuBlit(image);
            BlitFunctions.half = bc.Size(true) / volumeScale;

            var pixels = image.Pixels;

            var iHalf = (int)(BlitFunctions.half - 0.5f);
            var smooth = bc.GetBrushType(true) != BrushTypePixel.Inst;
            if (smooth) iHalf += 1;

            BlitFunctions.alphaMode = BlitFunctions.SphereAlpha;

            var sliceWidth = texWidth / volume.hSlices;

            var hw = sliceWidth / 2;

            var y = (int)pos.y;
            var z = (int)(pos.z + hw);
            var x = (int)(pos.x + hw);

            for (BlitFunctions.y = -iHalf; BlitFunctions.y < iHalf + 1; BlitFunctions.y++)
            {

                var h = y + BlitFunctions.y;

                if (h >= height) return true;

                if (h < 0) continue;
                var hy = h / volume.hSlices;
                var hx = h % volume.hSlices;
                var hTexIndex = (hy * texWidth + hx) * sliceWidth;

                for (BlitFunctions.z = -iHalf; BlitFunctions.z < iHalf + 1; BlitFunctions.z++)
                {

                    var trueZ = z + BlitFunctions.z;

                    if (trueZ < 0 || trueZ >= sliceWidth) continue;
                    var yTexIndex = hTexIndex + trueZ * texWidth;

                    for (BlitFunctions.x = -iHalf; BlitFunctions.x < iHalf + 1; BlitFunctions.x++)
                    {
                        if (!BlitFunctions.alphaMode()) continue;
                        var trueX = x + BlitFunctions.x;

                        if (trueX < 0 || trueX >= sliceWidth) continue;
                        var texIndex = yTexIndex + trueX;
                        BlitFunctions.blitMode(ref pixels[texIndex]);
                    }
                }
            }

            return true;
        }

        public bool PaintRenderTexture(StrokeVector stroke, ImageMeta image, BrushConfig bc, PlaytimePainter painter)
        {
            var vt = painter.GetVolumeTexture();
            if (vt == null) return false;
            BrushTypeSphere.Inst.BeforeStroke(painter, bc, stroke);

            VOLUME_POSITION_N_SIZE_BRUSH.GlobalValue = vt.PosSize4Shader;
            VOLUME_H_SLICES_BRUSH.GlobalValue = vt.Slices4Shader;
            if (stroke.mouseDwn)
                stroke.posFrom = stroke.posTo;

            image.useTexcoord2 = false;
            TexMGMT.Shader_UpdateStrokeSegment(bc, bc.speed * 0.05f, image, stroke, painter);
            stroke.SetWorldPosInShader();

            TexMGMT.brushRenderer.FullScreenQuad();

            TexMGMT.Render();

            BrushTypeSphere.Inst.AfterStroke(painter, bc, stroke);

            return true;
        }

        #region Inspector
        #if PEGI
        public override string NameForDisplayPEGI => "Volume Painting";

        public bool ComponentInspector()
        {
            var id = InspectedPainter.ImgMeta;
            
            if (id != null) return false;
            
            var inspectedProperty = InspectedPainter.GetMaterialTextureProperty;
            
            if (inspectedProperty == null || !inspectedProperty.NameForDisplayPEGI.Contains(VolumeTextureTag)) return false;

            if (inspectedProperty.IsGlobalVolume()) {
                "Global Volume Expected".nl();

                "Create a game object with one of the Volume scripts and set it as Global Parameter"
                    .fullWindowDocumentationClick();

            }
            else
            {
                "Volume Texture Expected".nl();

                var tmp = -1;

                if (!"Available:".select(60, ref tmp, VolumeTexture.all))
                {
                    var vol = VolumeTexture.all.TryGet(tmp);

                    if (!vol) return false;

                    if (vol.ImageMeta != null)
                        vol.AddIfNew(InspectedPainter);
                    else
                        "Volume Has No Texture".showNotificationIn3D_Views();

                    return true;
                }
            }

            return false;
        }

        private bool _exploreVolumeData;

        public bool BrushConfigPEGI(ref bool overrideBlitMode, BrushConfig br)
        {
            var changed = false;

            var p = InspectedPainter;

            var volTex = p.GetVolumeTexture();

            if (volTex != null)
            {

                overrideBlitMode = true;

                var id = p.ImgMeta;

                "Grid".toggle(50, ref _useGrid).nl();

                if ((volTex.name + " " + id.texture2D.VolumeSize(volTex.hSlices)).foldout(ref _exploreVolumeData).nl())
                    changed |= volTex.Nested_Inspect();

                if (volTex.NeedsToManageMaterials)
                {
                    var painterMaterial = InspectedPainter.Material;
                    if (painterMaterial != null)
                    {
                        if (!volTex.materials.Contains(painterMaterial))
                        {
                            if ("Add This Material".Click().nl())
                                volTex.AddIfNew(p);
                        }
                    }
                }

                var cpuBlit = id.TargetIsTexture2D();

                pegi.nl();

                if (!cpuBlit)
                   "Hardness:".edit("Makes edges more rough.", 70, ref br.hardness, 1f, 512f).nl(ref changed);

                "Speed".edit(40, ref br.speed, 0.01f, 20).nl(ref changed);

                var maxScale = volTex.size * volTex.Width * 0.25f;

                "Scale:".edit(40, ref br.brush3DRadius, 0.001f * maxScale, maxScale * 0.5f).nl(ref changed);

                if (br.GetBlitMode(cpuBlit).UsingSourceTexture && id.TargetIsRenderTexture())
                {
                    if (TexMGMTdata.sourceTextures.Count > 0)
                    {
                        "Copy From:".write(70);
                        changed |= pegi.selectOrAdd(ref br.selectedSourceTexture, ref TexMGMTdata.sourceTextures);
                    }
                    else
                        "Add Textures to Render Camera to copy from".nl();
                }

            }
            if (changed) this.SetToDirty_Obj();

            return changed;
        }

        private int _exploredVolume;
        public override bool Inspect()
        {
            var changes = false;

            changes |= "Volumes".edit_List(ref VolumeTexture.all, ref _exploredVolume);

            return changes;
        }
        #endif
        #endregion

        public bool PlugIn_PainterGizmos(PlaytimePainter painter)
        {
            var volume = painter.ImgMeta.GetVolumeTextureData();

            if (volume && !painter.LockTextureEditing)
                return volume.DrawGizmosOnPainter(painter);

            return false;
        }
    }


    [TaggedType(Tag)]
    public class VolumeTextureManagement : PainterComponentPluginBase
    {
        private const string Tag = "VolTexM";
        public override string ClassTag => Tag;

        private static VolumeTexture GlobalVolume => VolumeTexture.currentlyActiveGlobalVolume;

        public override void GetNonMaterialTextureNames(PlaytimePainter painter, ref List<ShaderProperty.TextureValue> dest)
        {
            var mat = painter.Material;

            if (mat && mat.GetTag("Volume", false, "").Equals("Global"))
            {
                var vol = GlobalVolume;
                if (vol)
                    dest.Add(vol.MaterialPropertyNameGlobal);
            }
        }

        public override bool SetTextureOnMaterial(ShaderProperty.TextureValue field, ImageMeta id, PlaytimePainter painter)
        {
            if (!field.IsGlobalVolume()) return false;

            GlobalVolume.texture = id.CurrentTexture() as Texture2D;

            GlobalVolume.UpdateMaterials();
            

            return true;
        }

        public override bool GetTexture(ShaderProperty.TextureValue field, ref Texture tex, PlaytimePainter painter)
        {
            if (!field.IsGlobalVolume()) return false;

            tex = GlobalVolume.texture;

            return true;
        }

        public override bool UpdateTilingFromMaterial(ShaderProperty.TextureValue fieldName, PlaytimePainter painter)
        {
            if (!fieldName.IsGlobalVolume()) return false;
            var id = painter.ImgMeta;
            if (id == null) return true;
            id.tiling = Vector2.one;
            id.offset = Vector2.zero;
            return true;
        }
    }

    public static class VolumeEditingExtensions
    {

        public static bool IsGlobalVolume(this ShaderProperty.TextureValue field) {

            var vol = VolumeTexture.currentlyActiveGlobalVolume;
            return vol && field.Equals(vol.MaterialPropertyNameGlobal);
        }

        public static VolumeTexture GetVolumeTexture(this PlaytimePainter p)
        {
            if (p == null)
                return null;

            var id = p.ImgMeta;

            if (id != null && id.texture2D)
                return id.GetVolumeTextureData();

            return null;
        }

        public static Vector3 VolumeSize(this Texture2D tex, int slices)
        {
            var w = tex.width / slices;
            return new Vector3(w, slices * slices, w);
        }

        public static void SetVolumeTexture(this Material material, string name, VolumeTexture vt)
        {
            VolumePaintingPlugin.VOLUME_POSITION_N_SIZE.SetOn(material, vt.PosSize4Shader);
            VolumePaintingPlugin.VOLUME_H_SLICES.SetOn(material, vt.Slices4Shader);
            material.SetTexture(name, vt.ImageMeta.CurrentTexture());
        }

        public static void SetVolumeTexture(this IEnumerable<Material> materials, ShaderProperty.TextureValue name, VolumeTexture vt)
        {
            if (vt == null || vt.ImageMeta == null) return;

            var pnS = vt.PosSize4Shader;
            var vhS = vt.Slices4Shader;

            foreach (var m in materials) if (m != null)
                {
                    m.Set(VolumePaintingPlugin.VOLUME_POSITION_N_SIZE, pnS);
                    m.Set(VolumePaintingPlugin.VOLUME_H_SLICES, vhS);
                    m.Set(name, vt.ImageMeta.CurrentTexture());
                }
        }

        public static VolumeTexture GetVolumeTextureData(this Texture tex) => GetVolumeTextureData(tex.GetImgData());


        private static VolumeTexture _lastFetchedVt;
        public static VolumeTexture GetVolumeTextureData(this ImageMeta id)
        {
            if (VolumePaintingPlugin._inst == null || id == null)
                return null;

            if (_lastFetchedVt != null && _lastFetchedVt.ImageMeta != null && _lastFetchedVt.ImageMeta == id)
                return _lastFetchedVt;

            for (var i = 0; i < VolumeTexture.all.Count; i++)
            {
                var vt = VolumeTexture.all[i];
                if (vt == null) { VolumeTexture.all.RemoveAt(i); i--; }

                else if (vt.ImageMeta != null && id == vt.ImageMeta)
                {
                    _lastFetchedVt = vt;
                    return vt;
                }
            }

            return null;
        }

    }
}