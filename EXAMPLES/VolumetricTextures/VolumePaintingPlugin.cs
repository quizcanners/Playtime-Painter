using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using System;
using SharedTools_Stuff;


namespace Playtime_Painter {

#if PEGI && UNITY_EDITOR
    using UnityEditor;
    [CustomEditor(typeof(VolumePaintingPlugin))]
    public class VolumePaintingPluginEditor : Editor {
        public override void OnInspectorGUI()
        {
            ef.start(serializedObject);
            ((VolumePaintingPlugin)target).ConfigTab_PEGI();
            ef.end();
        }
    }
#endif

    [Serializable]
    [ExecuteInEditMode]
    public class VolumePaintingPlugin : PainterManagerPluginBase {
        public const string VOLUME_H_SLICES = "VOLUME_H_SLICES";
        public const string VOLUME_POSITION_N_SIZE = "VOLUME_POSITION_N_SIZE";
        public const string VolumeTextureTag = "_VOL";
        public const string VolumeSlicesCountTag = "_slices";

        public bool useGrid;

        public Shader preview;
        public Shader brush;

        public static VolumePaintingPlugin _inst;

        public override void OnEnable() {
            base.OnEnable();
            _inst = this;
            
            if (preview == null)
                preview = Shader.Find("Editor/br_PreviewVolume");

            if (brush == null)
                brush = Shader.Find("Editor/br_Volume");
#if PEGI
            PlugIn_PainterComponent(Component_PEGI);
            
            PlugIn_BrushConfigPEGI(BrushConfigPEGI);
#endif
            PlugIn_PainterGizmos(DrawGizmosOnPainter);

            PlugIn_NeedsGrid(needsGrid);

            PlugIn_CPUblitMethod(PaintTexture2D);


        }

        public override string ToString() {
            return "Volume Painting";
        }

        public override Shader GetPreviewShader(PlaytimePainter p) {
            if (p.GetVolumeTexture() != null)
                return preview;
            return null;
        }

        public override Shader GetBrushShaderDoubleBuffer(PlaytimePainter p)  {
            if (p.GetVolumeTexture() != null)
                return brush;
            return null;
        }

        public bool needsGrid(PlaytimePainter p){ 
            return (useGrid && p.GetVolumeTexture() != null);
        }

        public override bool isA3Dbrush(PlaytimePainter pntr, BrushConfig bc, ref bool overrideOther) {
           if (pntr.GetVolumeTexture() != null) {
                overrideOther = true;
                return true;
            }
            return false;
        }

        public bool PaintTexture2D(StrokeVector stroke, float brushAlpha, ImageData image, BrushConfig bc, PlaytimePainter pntr) {

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
                Blit_Functions.half = bc.Size(true)/volumeScale;

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

        public override bool PaintRenderTexture(StrokeVector stroke, ImageData image, BrushConfig bc, PlaytimePainter pntr) {
            var vt = pntr.GetVolumeTexture();
            if (vt != null) {

                BrushTypeSphere.inst.BeforeStroke(pntr, bc, stroke);

                Shader.SetGlobalVector(VOLUME_POSITION_N_SIZE+"_BRUSH", vt.posNsize4Shader);
                Shader.SetGlobalVector(VOLUME_H_SLICES + "_BRUSH", vt.slices4Shader);
                if (stroke.mouseDwn)
                    stroke.posFrom = stroke.posTo;

                stroke.useTexcoord2 = false;

                texMGMT.ShaderPrepareStroke(bc, bc.speed * 0.05f, image, stroke, pntr);
                stroke.SetWorldPosInShader();

                texMGMT.brushRendy.FullScreenQuad();

                texMGMT.Render();

                BrushTypeSphere.inst.AfterStroke(pntr, bc, stroke);

                return true;
            }
            return false;
        }

#if PEGI
        public bool Component_PEGI() {
            bool changed = false;

            if (inspectedPainter && inspectedPainter.imgData == null)  {
                var matProp = inspectedPainter.MaterialTexturePropertyName;
                if (matProp != null && matProp.Contains(VolumeTextureTag))  {

                    "Volume Texture Expected".nl();
                    int tmp = -1;

                    if ("Available:".select(60, ref tmp, VolumeTexture.all))  {
                        var vol = VolumeTexture.all[tmp];
                        if (vol != null) {
                            if (String.Compare(vol.MaterialPropertyName, matProp) == 0) {
                                if (vol.imageData != null)
                                    vol.AddIfNew(inspectedPainter);
                                else
                                    "Volume Has No Texture".showNotification();
                            } else { ("Volume is for " + vol.MaterialPropertyName + " not " + matProp).showNotification();}
                        }
                    }
                }
            }
            return changed;
        }
#endif


        public bool DrawGizmosOnPainter(PlaytimePainter pntr) {
            var volume = pntr.imgData.GetVolumeTextureData();

            if (volume!= null && !pntr.LockTextureEditing) 
                 return volume.DrawGizmosOnPainter(pntr);

            return false;
        }
#if PEGI
        [SerializeField]
        bool exploreVolumeData = false;
        public bool BrushConfigPEGI(ref bool overrideBlitMode, BrushConfig br)
        {
            bool changed = false;

            PlaytimePainter p = inspectedPainter;
         
            var volTex = p.GetVolumeTexture();
            
            if (volTex != null) {

                overrideBlitMode = true;

                var id = p.imgData;

                "Grid".toggle(50, ref useGrid).nl();

                if ((volTex.name + " " + id.texture2D.volumeSize(volTex.h_slices).ToString()).foldout(ref exploreVolumeData).nl())
                    changed |= volTex.PEGI();

                if (volTex.needsToManageMaterials) {
                    var pmat = inspectedPainter.material;
                    if (pmat != null) {
                        if (!volTex.materials.Contains(pmat)) {
                            if ("Add This Material".Click().nl())
                                volTex.AddIfNew(p); 
                        }
                    }
                }

                bool cpuBlit = id.TargetIsTexture2D();

                pegi.newLine();

                if (!cpuBlit)
                    changed |= "Hardness:".edit("Makes edges more rough.", 70, ref br.Hardness, 1f, 512f).nl();

                changed |= "Speed".edit(40, ref br.speed, 0.01f, 20).nl();

                float maxScale = volTex.size * volTex.width *0.25f;

                changed |= "Scale:".edit(40,ref br.Brush3D_Radius, 0.001f * maxScale, maxScale * 0.5f).nl();
            
                if ((br.blitMode.usingSourceTexture) && (id == null || id.TargetIsRenderTexture()))
                {
                    if (texMGMT.sourceTextures.Length > 0)
                    {
                        br.selectedSourceTexture = Mathf.Min(br.selectedSourceTexture, texMGMT.sourceTextures.Length - 1);
                        pegi.write("Copy From:", 70);
                        changed |= pegi.selectOrAdd(ref br.selectedSourceTexture, ref texMGMT.sourceTextures);
                    }
                else
                    "Add Textures to Render Camera to copy from".nl();
            }
                
            }
            if (changed) this.SetToDirty();

            return changed;
        }

        [SerializeField]
        int exploredVolume;
        public override bool ConfigTab_PEGI()
        {
            bool changes = false;

            if (VolumeTexture.all.Count == 0)
                "No volumes found".nl();

            for (int i=0; i<VolumeTexture.all.Count; i++) 
               changes |= VolumeTexture.all.edit_List(ref exploredVolume, false);
                
            return changes;
        }
#endif
    }

    public static class VolumeEditingExtensions {

        public static VolumeTexture GetVolumeTexture(this PlaytimePainter p) {
            if (p == null)
                return null;
            var id = p.imgData;
            if (id != null && id.texture2D!=null && id.texture2D.volumeSlices() > 1)
                return id.GetVolumeTextureData();
            return null;
        }

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

        public static void SetVolumeTexture(this Material material, string name, VolumeTexture vt) {
            material.SetVector(VolumePaintingPlugin.VOLUME_POSITION_N_SIZE, vt.posNsize4Shader);
            material.SetVector(VolumePaintingPlugin.VOLUME_H_SLICES, vt.slices4Shader);
            material.SetTexture(name, vt.imageData.currentTexture());
        }
        
        public static void SetVolumeTexture(this List<Material> materials, string name, VolumeTexture vt)
        {
            if (vt == null || vt.imageData == null) return;
         
            var PnS = vt.posNsize4Shader;
            var VhS = vt.slices4Shader;

            foreach (var m in materials) if (m != null) {
                m.SetVector(VolumePaintingPlugin.VOLUME_POSITION_N_SIZE, PnS );
                m.SetVector(VolumePaintingPlugin.VOLUME_H_SLICES, VhS);
                m.SetTexture(name, vt.imageData.currentTexture());
            }
        }

        public static VolumeTexture GetVolumeTextureData (this Texture tex) {
            return GetVolumeTextureData(tex.getImgData());
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

            if (lastFetchedVT != null && lastFetchedVT.imageData != null && lastFetchedVT.imageData == id)
                return lastFetchedVT;

            for (int i = 0; i < VolumeTexture.all.Count; i++)
            {
                var vt = VolumeTexture.all[i];
                if (vt == null) { VolumeTexture.all.RemoveAt(i); i--; }
                else if (vt.imageData != null && id == vt.imageData) {
                    lastFetchedVT = vt;
                    return vt;
                }
            }

            return null;
        }

    }
}