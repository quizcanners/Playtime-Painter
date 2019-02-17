
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
    public class VolumePaintingPlugin : PainterManagerPluginBase, IPEGI, IGotDisplayName,
        IPainterManagerPlugin_ComponentPEGI, IPainterManagerPlugin_Brush
    {

        const string tag = "VolumePntng";
        public override string ClassTag => tag;

        public static ShaderProperty.VectorValue VOLUME_H_SLICES = new ShaderProperty.VectorValue("VOLUME_H_SLICES");
        public static ShaderProperty.VectorValue VOLUME_POSITION_N_SIZE = new ShaderProperty.VectorValue("VOLUME_POSITION_N_SIZE");
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
            if (painter.GetVolumeTexture() == null) return false;
            overrideOther = true;
            return true;
        }

        /*
        public bool PaintTexture2D_NEW(StrokeVector stroke, float brushAlpha, ImageData image, 
            BrushConfig bc, PlaytimePainter pntr) {

            var volume = image.texture2D.GetVolumeTextureData();

            if (image.CanUsePixelsForJob()) {

                var blitJob = new BlitJobs();

                blitJob.PrepareVolumeBlit(bc, image, brushAlpha, stroke, volume);

                image.jobHandle = blitJob.Schedule();

                JobHandle.ScheduleBatchedJobs();

                return true;
            }
            
            return false;
        }*/

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

            Blit_Functions.brAlpha = brushAlpha;
            bc.PrepareCPUBlit(image);
            Blit_Functions.half = bc.Size(true) / volumeScale;

            var pixels = image.Pixels;

            var ihalf = (int)(Blit_Functions.half - 0.5f);
            var smooth = bc.Type(true) != BrushTypePixel.Inst;
            if (smooth) ihalf += 1;

            Blit_Functions._alphaMode = Blit_Functions.SphereAlpha;

            var sliceWidth = texWidth / volume.h_slices;

            var hw = sliceWidth / 2;

            var y = (int)pos.y;
            var z = (int)(pos.z + hw);
            var x = (int)(pos.x + hw);

            for (Blit_Functions.y = -ihalf; Blit_Functions.y < ihalf + 1; Blit_Functions.y++)
            {

                var h = y + Blit_Functions.y;

                if (h >= height) return true;

                if (h < 0) continue;
                var hy = h / volume.h_slices;
                var hx = h % volume.h_slices;
                var hTexIndex = (hy * texWidth + hx) * sliceWidth;

                for (Blit_Functions.z = -ihalf; Blit_Functions.z < ihalf + 1; Blit_Functions.z++)
                {

                    var trueZ = z + Blit_Functions.z;

                    if (trueZ < 0 || trueZ >= sliceWidth) continue;
                    var yTexIndex = hTexIndex + trueZ * texWidth;

                    for (Blit_Functions.x = -ihalf; Blit_Functions.x < ihalf + 1; Blit_Functions.x++)
                    {
                        if (!Blit_Functions._alphaMode()) continue;
                        var trueX = x + Blit_Functions.x;

                        if (trueX < 0 || trueX >= sliceWidth) continue;
                        var texIndex = yTexIndex + trueX;
                        Blit_Functions._blitMode(ref pixels[texIndex]);
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

            VOLUME_POSITION_N_SIZE_BRUSH.GlobalValue = vt.PosNsize4Shader;
            VOLUME_H_SLICES_BRUSH.GlobalValue = vt.Slices4Shader;
            if (stroke.mouseDwn)
                stroke.posFrom = stroke.posTo;

            image.useTexcoord2 = false;
            //  stroke.useTexcoord2 = false;

            TexMGMT.Shader_UpdateStrokeSegment(bc, bc.speed * 0.05f, image, stroke, painter);
            stroke.SetWorldPosInShader();

            TexMGMT.brushRenderer.FullScreenQuad();

            TexMGMT.Render();

            BrushTypeSphere.Inst.AfterStroke(painter, bc, stroke);

            return true;
        }

        public bool DrawGizmosOnPainter(PlaytimePainter painter)
        {
            var volume = painter.ImgMeta.GetVolumeTextureData();

            if (volume != null && !painter.LockTextureEditing)
                return volume.DrawGizmosOnPainter(painter);

            return false;
        }

        #region Inspector
        #if PEGI
        public override string NameForDisplayPEGI => "Volume Painting";

        public bool ComponentInspector()
        {
            var id = InspectedPainter.ImgMeta;
            
            if (id == null) return false;
            
            var inspectedProperty = InspectedPainter.GetMaterialTextureProperty.NameForDisplayPEGI;
            
            if (!inspectedProperty.IsNullOrEmpty() && inspectedProperty.Contains(VolumeTextureTag)) return false;
            
            "Volume Texture Expected".nl();

            var tmp = -1;

            if (!"Available:".select(60, ref tmp, VolumeTexture.all))
            {
                var vol = VolumeTexture.all.TryGet(tmp);

                if (vol) {

                    if (vol.MaterialPropertyName.SameAs(inspectedProperty)) {
                        if (vol.ImageMeta != null)
                            vol.AddIfNew(InspectedPainter);
                        else
                            "Volume Has No Texture".showNotificationIn3D_Views();
                    }
                    else { ("Volume is for " + vol.MaterialPropertyName + " not " + inspectedProperty).showNotificationIn3D_Views(); }
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

                if ((volTex.name + " " + id.texture2D.VolumeSize(volTex.h_slices).ToString()).foldout(ref _exploreVolumeData).nl())
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
                   "Hardness:".edit("Makes edges more rough.", 70, ref br.Hardness, 1f, 512f).nl(ref changed);

                "Speed".edit(40, ref br.speed, 0.01f, 20).nl(ref changed);

                var maxScale = volTex.size * volTex.Width * 0.25f;

                "Scale:".edit(40, ref br.Brush3D_Radius, 0.001f * maxScale, maxScale * 0.5f).nl(ref changed);

                if ((br.BlitMode.UsingSourceTexture) && (id.TargetIsRenderTexture()))
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
    }

    public static class VolumeEditingExtensions
    {

        public static VolumeTexture GetVolumeTexture(this PlaytimePainter p)
        {
            if (p == null)
                return null;
            var id = p.ImgMeta;
            if (id != null && id.texture2D != null)
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
            VolumePaintingPlugin.VOLUME_POSITION_N_SIZE.SetOn(material, vt.PosNsize4Shader);
            VolumePaintingPlugin.VOLUME_H_SLICES.SetOn(material, vt.Slices4Shader);
            material.SetTexture(name, vt.ImageMeta.CurrentTexture());
        }

        public static void SetVolumeTexture(this List<Material> materials, string name, VolumeTexture vt)
        {
            if (vt == null || vt.ImageMeta == null) return;

            var pnS = vt.PosNsize4Shader;
            var vhS = vt.Slices4Shader;

            foreach (var m in materials) if (m != null)
                {
                    m.Set(VolumePaintingPlugin.VOLUME_POSITION_N_SIZE, pnS);
                    m.Set(VolumePaintingPlugin.VOLUME_H_SLICES, vhS);
                    m.SetTexture(name, vt.ImageMeta.CurrentTexture());
                }
        }

        public static VolumeTexture GetVolumeTextureData(this Texture tex) => GetVolumeTextureData(tex.GetImgData());

        
        static VolumeTexture _lastFetchedVt;
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