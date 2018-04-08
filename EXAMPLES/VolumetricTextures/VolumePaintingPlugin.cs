using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using System;

namespace Playtime_Painter
{

#if UNITY_EDITOR
    using UnityEditor;
    [CustomEditor(typeof(VolumePaintingPlugin))]
    public class VolumePaintingPluginEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            ef.start(serializedObject);
            ((VolumePaintingPlugin)target).ConfigTab_PEGI();
            ef.end();
        }
    }
#endif

    public class VolumePaintingPlugin : PainterManagerPluginBase
    {
        public const string VOLUME_H_SLICES = "VOLUME_H_SLICES";
        public const string VOLUME_POSITION_N_SIZE = "VOLUME_POSITION_N_SIZE";
        public const string VolumeTextureTag = "_VOL";
        public const string VolumeSlicesCountTag = "_slices";

        public bool useGrid;

        public static VolumePaintingPlugin _inst;

        public override void OnEnable() {
            base.OnEnable();
            _inst = this;
        }

        public List<VolumeTexture> volumes = new List<VolumeTexture>();

        public override string ToString() {
            return "Volume Painting";
        }

        public bool exploreVolumeData = false;
        public override bool BrushConfigPEGI() {
            bool changed = false;

            PlaytimePainter p = painter;
            var id = p.curImgData;

            if (id!= null && id.texture2D != null) {
                int slices = id.texture2D.volumeSlices();
                if (slices > 1) {
                  
                    var vt = id.texture2D.GetVolumeTextureData();
                    if (vt != null) {
                        "Grid".toggle(50, ref useGrid).nl();

                        if ((vt.name + " " + id.texture2D.volumeSize(slices).ToString()).foldout(ref exploreVolumeData).nl())
                            changed |= vt.PEGI();

                    }
                    else "No _Volume Controller found ".nl(); // (" + id.texture2D.volumeSize(slices).ToString()).nl();
                }
            }

            return changed;
        }

        public override bool needsGrid(PlaytimePainter p)
        {
            var id = p.curImgData;

            return (useGrid && id != null && id.TargetIsTexture2D() && id.texture2D.volumeSlices() > 1 && id.texture2D.GetVolumeTextureData() != null);
        }

        public override bool PaintTexture2D(StrokeVector stroke, float brushAlpha, imgData image, BrushConfig bc) {

            int slices;

            slices = image.texture2D.volumeSlices();

            if (slices > 1) {
                var volume = image.texture2D.GetVolumeTextureData();

                if (volume == null)
                    return false;

                float volumeScale = volume.size;

                Vector3 pos = (stroke.posFrom - volume.transform.position) / volumeScale +0.5f*Vector3.one;

                int height = volume.height;
                int texWidth = image.width;

                Blit_Functions.brAlpha = brushAlpha;
                bc.PrepareCPUBlit();

                var pixels = image.pixels;

                int ihalf = (int)(Blit_Functions.half - 0.5f);
                bool smooth = bc.type(true) != BrushTypePixel.inst;
                if (smooth) ihalf += 1;

                Blit_Functions._alphaMode = Blit_Functions.SphereAlpha;

                int sliceWidth = texWidth / slices;

                int hw = sliceWidth / 2;

                int y = (int)pos.y; // (int)Mathf.Clamp(pos.y, 0, height - 1);
                int z = (int)(pos.z + hw); // (int)Mathf.Clamp(pos.z + hw, 0, hw - 1);
                int x = (int)(pos.x + hw); // (int)Mathf.Clamp(pos.x + hw, 0, hw - 1);
                

                for (Blit_Functions.y = -ihalf; Blit_Functions.y < ihalf + 1; Blit_Functions.y++) {

                    int h = y + Blit_Functions.y;

                    if (h >= height) return true;

                    if (h >= 0) {

                        int hy = h / slices;
                        int hx = h % slices;
                        int hTex_index = (hy * texWidth + hx) * sliceWidth;

                        for (Blit_Functions.z = -ihalf; Blit_Functions.z < ihalf + 1; Blit_Functions.z++) {

                            int trueZ = z + Blit_Functions.z;

                            if (trueZ >= 0 && trueZ < sliceWidth)  {

                                int yTex_index = hTex_index + trueZ * texWidth;

                                for (Blit_Functions.x = -ihalf; Blit_Functions.x < ihalf + 1; Blit_Functions.x++)
                                {
                                    int trueX = x + Blit_Functions.x;

                                    if (trueX >= 0 && trueX < sliceWidth) {
                                        int texIndex = yTex_index + trueX;

                                        if (Blit_Functions._alphaMode())
                                            Blit_Functions._blitMode(ref pixels[texIndex]);
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
        
        public override bool Component_PEGI() {
            bool changed = false;
            
            if (painter.curImgData == null && painter.materialTexturePropertyName.Contains(VolumeTextureTag)) {

                "Volume Texture Expected".nl();
                int tmp = -1;

                if ("Available:".select(60,ref tmp, volumes)) {
                    var vol = volumes[tmp];
                    if (vol != null) {
                        if (vol.tex != null)
                        {
                            vol.AssignTo(painter);
                          

                        }
                        else
                            "Volume Has No Texture".showNotification();
                    }
                }

            }


            return changed;
        }


        [SerializeField]
        int exploredVolume;
        public override bool ConfigTab_PEGI()
        {
            bool changes = false;

            if (volumes.Count == 0)
                "No volumes found".nl();

            for (int i=0; i<volumes.Count; i++) 
               changes |= volumes.PEGI(ref exploredVolume, false);
                
            return changes;
        }
        
    }

    public static class VolumeEditingExtensions {

        public static Vector3 volumeSize (this Texture2D tex, int slices) {      
            int w= tex.width/slices;
            return new Vector3(w, slices * slices, w);
        }

        public static int volumeSlices (this Texture2D tex) {
            if (tex == null) return 1;

            string n = tex.name;
            int tag = n.IndexOf(VolumePaintingPlugin.VolumeTextureTag);

            if (tag != -1){
                int countLen = n.IndexOf(VolumePaintingPlugin.VolumeSlicesCountTag);
                if (countLen > tag) {
                    tag += VolumePaintingPlugin.VolumeTextureTag.Length;
                    string slices = tex.name.Substring(tag, countLen-tag);
                    int cnt = slices.ToIntFromTextSafe(1);
                    return cnt;
                }
            }

            return 1;
        }

        public static void SetVolumeTexture(this Material material, string name, VolumeTexture vt, Vector3 pos) {
            material.SetVolumeTexture(name, vt.tex.currentTexture(), pos, vt.size, vt.h_slices);
        }

        public static void SetVolumeTexture(this List<Material> material, string name, VolumeTexture vt, Vector3 pos) {
            material.SetVolumeTexture(name, vt.tex.currentTexture(), pos, vt.size, vt.h_slices);
        }

        public static void SetVolumeTexture(this Material material, string name, Texture tex, Vector3 pos, float size, int slices) {
            float w = (tex.width - slices * 2) / slices;
            material.SetVector(VolumePaintingPlugin.VOLUME_POSITION_N_SIZE, new Vector4(pos.x, pos.y, pos.z, 1f / size));
            material.SetVector(VolumePaintingPlugin.VOLUME_H_SLICES, new Vector4(slices, w * 0.5f, 1f / ((float)w), 1f / ((float)slices)));
            material.SetTexture(name, tex);
        }

        public static void SetVolumeTexture(this List<Material> materials, string name, Texture tex, Vector3 pos, float size, int slices) {
            float w = (tex.width - slices * 2) / slices;

            var PnS = new Vector4(pos.x, pos.y, pos.z, 1f / size);
            var VhS = new Vector4(slices, w * 0.5f, 1f / ((float)w), 1f / ((float)slices));

            foreach (var m in materials) {
                m.SetVector(VolumePaintingPlugin.VOLUME_POSITION_N_SIZE, PnS );
                m.SetVector(VolumePaintingPlugin.VOLUME_H_SLICES, VhS);
                m.SetTexture(name, tex);
            }
        }

        public static VolumeTexture GetVolumeTextureData (this Texture tex) {
            if (VolumePaintingPlugin._inst == null)
                return null;

         
            var pl = VolumePaintingPlugin._inst;

            for (int i = 0; i < pl.volumes.Count; i++) {
                var vt = pl.volumes[i];
                if (vt == null) { pl.volumes.RemoveAt(i); i--; }
                    else if (vt.tex != null && vt.tex.Equals(tex)) return vt;
            }

            return null;
        }

      

    }


}