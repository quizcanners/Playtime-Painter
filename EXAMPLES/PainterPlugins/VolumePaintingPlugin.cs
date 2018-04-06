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

            PlaytimePainter p = PlaytimePainter.inspectedPainter;
            var id = p.curImgData;

            if (id!= null && id.texture2D != null) {
                if (id.texture2D.name.Contains(VolumeTextureTag)) {
                    ("Is Volume Texture ("+VolumeTextureTag+")").nl();
                    var vt = id.texture2D.GetVolumeTextureData();
                    if (vt == null)
                        "No Volume Texture Data Found".nl();
                    else if (vt.name.foldout(ref exploreVolumeData)) 
                        changed |= vt.PEGI();
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

            for (int i=0; i<volumes.Count; i++) {
                volumes.PEGI(ref exploredVolume, false);
                
            }
            
            return changes;
        }


    }

    public static class VolumeEditingExtensions {
        public static void SetVolumeTexture(this Material material, string name, Texture tex, Vector3 pos, float size, int slices)
        {
            float w = (tex.width - slices * 2) / slices;

            material.SetVector(VolumePaintingPlugin.VOLUME_POSITION_N_SIZE, new Vector4(pos.x, pos.y, pos.z, 1f / size));
            material.SetVector(VolumePaintingPlugin.VOLUME_H_SLICES, new Vector4(slices, w * 0.5f, 1f / ((float)w), 1f / ((float)slices)));
            material.SetTexture(name, tex);
        }

        public static VolumeTexture GetVolumeTextureData (this Texture2D tex) {
            if (VolumePaintingPlugin._inst == null)
                return null;

            foreach (var vt in VolumePaintingPlugin._inst.volumes)
                if (vt.tex != null && vt.tex == tex) return vt;

            return null;
        }

        public static void SetVolumeTexture(this Material material, string name, VolumeTexture vt, Vector3 pos) {
           material.SetVolumeTexture(name, vt.tex, pos, vt.size, vt.h_slices);
        }

    }


}