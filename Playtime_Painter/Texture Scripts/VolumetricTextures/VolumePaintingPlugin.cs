using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using SharedTools_Stuff;
#if UNITY_2018_1_OR_NEWER
using Unity.Jobs;
using Unity.Collections;
#endif
using System;

namespace Playtime_Painter
{

    [TaggedType(tag)]
    public class VolumePaintingPlugin : PainterManagerPluginBase, IPEGI, IGotDisplayName  {

        const string tag = "VolumePntng";
        public override string ClassTag => tag;

        public const string VOLUME_H_SLICES = "VOLUME_H_SLICES";
        public const string VOLUME_POSITION_N_SIZE = "VOLUME_POSITION_N_SIZE";
        public const string VolumeTextureTag = "_VOL";
        public const string VolumeSlicesCountTag = "_slices";

        public bool useGrid;

        public Shader preview;
        public Shader brush;

        #region Encode & Decode

        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add_Bool("ug", useGrid);

        public override bool Decode(string tag, string data)
        {
            switch (tag)
            {
                case "ug": useGrid = data.ToBool(); break;
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

            if (preview == null)
                preview = Shader.Find("Editor/br_PreviewVolume");

            if (brush == null)
                brush = Shader.Find("Editor/br_Volume");
#if PEGI
            PlugIn_PainterComponent = Component_PEGI;

            PlugIn_BrushConfigPEGI(BrushConfigPEGI);
#endif
            PlugIn_PainterGizmos(DrawGizmosOnPainter);

            PlugIn_NeedsGrid(NeedsGrid);

            PlugIn_CPUblitMethod(PaintTexture2D);

        }
        
        public override Shader GetPreviewShader(PlaytimePainter p)
        {
            if (p.GetVolumeTexture() != null)
                return preview;
            return null;
        }

        public override Shader GetBrushShaderDoubleBuffer(PlaytimePainter p)
        {
            if (p.GetVolumeTexture() != null)
                return brush;
            return null;
        }

        public bool NeedsGrid(PlaytimePainter p)
        {
            return (useGrid && p.GetVolumeTexture() != null);
        }

        public override bool IsA3Dbrush(PlaytimePainter pntr, BrushConfig bc, ref bool overrideOther)
        {
            if (pntr.GetVolumeTexture() != null)
            {
                overrideOther = true;
                return true;
            }
            return false;
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

        public bool PaintTexture2D(StrokeVector stroke, float brushAlpha, ImageData image, BrushConfig bc, PlaytimePainter pntr)
        {

            var volume = image.texture2D.GetVolumeTextureData();

            if (volume != null)
            {

                if (volume.VolumeJobIsRunning)
                    return false;

                float volumeScale = volume.size;

                Vector3 pos = (stroke.posFrom - volume.transform.position) / volumeScale + 0.5f * Vector3.one;

                int height = volume.Height;
                int texWidth = image.width;

                Blit_Functions.brAlpha = brushAlpha;
                bc.PrepareCPUBlit(image);
                Blit_Functions.half = bc.Size(true) / volumeScale;

                var pixels = image.Pixels;

                int ihalf = (int)(Blit_Functions.half - 0.5f);
                bool smooth = bc.Type(true) != BrushTypePixel.Inst;
                if (smooth) ihalf += 1;

                Blit_Functions._alphaMode = Blit_Functions.SphereAlpha;

                int sliceWidth = texWidth / volume.h_slices;

                int hw = sliceWidth / 2;

                int y = (int)pos.y;
                int z = (int)(pos.z + hw);
                int x = (int)(pos.x + hw);

                for (Blit_Functions.y = -ihalf; Blit_Functions.y < ihalf + 1; Blit_Functions.y++)
                {

                    int h = y + Blit_Functions.y;

                    if (h >= height) return true;

                    if (h >= 0)
                    {

                        int hy = h / volume.h_slices;
                        int hx = h % volume.h_slices;
                        int hTex_index = (hy * texWidth + hx) * sliceWidth;

                        for (Blit_Functions.z = -ihalf; Blit_Functions.z < ihalf + 1; Blit_Functions.z++)
                        {

                            int trueZ = z + Blit_Functions.z;

                            if (trueZ >= 0 && trueZ < sliceWidth)
                            {

                                int yTex_index = hTex_index + trueZ * texWidth;

                                for (Blit_Functions.x = -ihalf; Blit_Functions.x < ihalf + 1; Blit_Functions.x++)
                                {
                                    if (Blit_Functions._alphaMode())
                                    {
                                        int trueX = x + Blit_Functions.x;

                                        if (trueX >= 0 && trueX < sliceWidth)
                                        {

                                            int texIndex = yTex_index + trueX;
                                            Blit_Functions._blitMode(ref pixels[texIndex]);

                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return true;
            }
            return false;
        }

        public override bool PaintRenderTexture(StrokeVector stroke, ImageData image, BrushConfig bc, PlaytimePainter pntr)
        {
            var vt = pntr.GetVolumeTexture();
            if (vt != null)
            {

                BrushTypeSphere.Inst.BeforeStroke(pntr, bc, stroke);

                Shader.SetGlobalVector(VOLUME_POSITION_N_SIZE + "_BRUSH", vt.PosNsize4Shader);
                Shader.SetGlobalVector(VOLUME_H_SLICES + "_BRUSH", vt.Slices4Shader);
                if (stroke.mouseDwn)
                    stroke.posFrom = stroke.posTo;

                image.useTexcoord2 = false;
                //  stroke.useTexcoord2 = false;

                TexMGMT.Shader_UpdateStrokeSegment(bc, bc.speed * 0.05f, image, stroke, pntr);
                stroke.SetWorldPosInShader();

                TexMGMT.brushRendy.FullScreenQuad();

                TexMGMT.Render();

                BrushTypeSphere.Inst.AfterStroke(pntr, bc, stroke);

                return true;
            }
            return false;
        }

        public bool DrawGizmosOnPainter(PlaytimePainter pntr)
        {
            var volume = pntr.ImgData.GetVolumeTextureData();

            if (volume != null && !pntr.LockTextureEditing)
                return volume.DrawGizmosOnPainter(pntr);

            return false;
        }

        #region Inspector
        #if PEGI
        public override string NameForPEGIdisplay => "Volume Painting";

        public bool Component_PEGI()
        {
            bool changed = false;

            if (InspectedPainter && InspectedPainter.ImgData == null)
            {
                var matProp = InspectedPainter.GetMaterialTexturePropertyName;
                if (matProp != null && matProp.Contains(VolumeTextureTag))
                {

                    "Volume Texture Expected".nl();
                    int tmp = -1;

                    if ("Available:".select(60, ref tmp, VolumeTexture.all))
                    {
                        var vol = VolumeTexture.all[tmp];
                        if (vol != null)
                        {
                            if (String.Compare(vol.MaterialPropertyName, matProp) == 0)
                            {
                                if (vol.ImageData != null)
                                    vol.AddIfNew(InspectedPainter);
                                else
                                    "Volume Has No Texture".showNotificationIn3D_Views();
                            }
                            else { ("Volume is for " + vol.MaterialPropertyName + " not " + matProp).showNotificationIn3D_Views(); }
                        }
                    }
                }
            }
            return changed;
        }

        bool exploreVolumeData = false;
        public bool BrushConfigPEGI(ref bool overrideBlitMode, BrushConfig br)
        {
            bool changed = false;

            PlaytimePainter p = InspectedPainter;

            var volTex = p.GetVolumeTexture();

            if (volTex != null)
            {

                overrideBlitMode = true;

                var id = p.ImgData;

                "Grid".toggle(50, ref useGrid).nl();

                if ((volTex.name + " " + id.texture2D.VolumeSize(volTex.h_slices).ToString()).foldout(ref exploreVolumeData).nl())
                    changed |= volTex.Nested_Inspect();

                if (volTex.NeedsToManageMaterials)
                {
                    var pmat = InspectedPainter.Material;
                    if (pmat != null)
                    {
                        if (!volTex.materials.Contains(pmat))
                        {
                            if ("Add This Material".Click().nl())
                                volTex.AddIfNew(p);
                        }
                    }
                }

                bool cpuBlit = id.TargetIsTexture2D();

                pegi.nl();

                if (!cpuBlit)
                    changed |= "Hardness:".edit("Makes edges more rough.", 70, ref br.Hardness, 1f, 512f).nl();

                changed |= "Speed".edit(40, ref br.speed, 0.01f, 20).nl();

                float maxScale = volTex.size * volTex.Width * 0.25f;

                changed |= "Scale:".edit(40, ref br.Brush3D_Radius, 0.001f * maxScale, maxScale * 0.5f).nl();

                if ((br.BlitMode.UsingSourceTexture) && (id == null || id.TargetIsRenderTexture()))
                {
                    if (TexMGMTdata.sourceTextures.Count > 0)
                    {
                        pegi.write("Copy From:", 70);
                        changed |= pegi.selectOrAdd(ref br.selectedSourceTexture, ref TexMGMTdata.sourceTextures);
                    }
                    else
                        "Add Textures to Render Camera to copy from".nl();
                }

            }
            if (changed) this.SetToDirty();

            return changed;
        }

        int exploredVolume;
        public override bool Inspect()
        {
            bool changes = false;

            changes |= "Volumes".edit_List(ref VolumeTexture.all, ref exploredVolume);

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
            var id = p.ImgData;
            if (id != null && id.texture2D != null)
                return id.GetVolumeTextureData();
            return null;
        }

        public static Vector3 VolumeSize(this Texture2D tex, int slices)
        {
            int w = tex.width / slices;
            return new Vector3(w, slices * slices, w);
        }

        public static void SetVolumeTexture(this Material material, string name, VolumeTexture vt)
        {
            material.SetVector(VolumePaintingPlugin.VOLUME_POSITION_N_SIZE, vt.PosNsize4Shader);
            material.SetVector(VolumePaintingPlugin.VOLUME_H_SLICES, vt.Slices4Shader);
            material.SetTexture(name, vt.ImageData.CurrentTexture());
        }

        public static void SetVolumeTexture(this List<Material> materials, string name, VolumeTexture vt)
        {
            if (vt == null || vt.ImageData == null) return;

            var PnS = vt.PosNsize4Shader;
            var VhS = vt.Slices4Shader;

            foreach (var m in materials) if (m != null)
                {
                    m.SetVector(VolumePaintingPlugin.VOLUME_POSITION_N_SIZE, PnS);
                    m.SetVector(VolumePaintingPlugin.VOLUME_H_SLICES, VhS);
                    m.SetTexture(name, vt.ImageData.CurrentTexture());
                }
        }

        public static VolumeTexture GetVolumeTextureData(this Texture tex)
        {
            return GetVolumeTextureData(tex.GetImgData());
            /* if (VolumePaintingPlugin._inst == null)
                return null;
 
            for (int i = 0; i < VolumeTexture.all.Count; i++) {
                var vt = VolumeTexture.all[i];
                if (vt == null) { VolumeTexture.all.RemoveAt(i); i--; }
                    else if (vt.tex != null && vt.tex.Equals(tex)) return vt;
            }

            return null;*/
        }

        static VolumeTexture lastFetchedVT;
        public static VolumeTexture GetVolumeTextureData(this ImageData id)
        {
            if (VolumePaintingPlugin._inst == null || id == null)
                return null;

            if (lastFetchedVT != null && lastFetchedVT.ImageData != null && lastFetchedVT.ImageData == id)
                return lastFetchedVT;

            for (int i = 0; i < VolumeTexture.all.Count; i++)
            {
                var vt = VolumeTexture.all[i];
                if (vt == null) { VolumeTexture.all.RemoveAt(i); i--; }

                else if (vt.ImageData != null && id == vt.ImageData)
                {
                    lastFetchedVT = vt;
                    return vt;
                }
            }

            return null;
        }

    }
}