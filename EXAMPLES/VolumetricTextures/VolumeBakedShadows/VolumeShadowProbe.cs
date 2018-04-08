using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;


namespace Playtime_Painter {

#if UNITY_EDITOR

    using UnityEditor;

    [CustomEditor(typeof(VolumeShadowProbe))]
    public class VolumeShadowProbeEditor : Editor {

        public override void OnInspectorGUI() {
            ef.start(serializedObject);
            ((VolumeShadowProbe)target).PEGI();
            ef.end();
        }
    }
#endif
    
    [ExecuteInEditMode]
    public class VolumeShadowProbe : VolumeTexture {
        
        MaterialLightManager mats;
        public const string BakedShadowVolumeTextureProperty = "_BakedShadow_VOL";


        public void OnDrawGizmosSelected() {
            DrawGizmo(transform.position, Color.green);
        }

        public void Update() {
            mats.UpdateMaterials();
        }

        public void OnEnable() {
            if (mats == null)
                mats = new MaterialLightManager();
        }

        public override void AssignTo(PlaytimePainter p) {
            if (!mats.materials.Contains(p.material))
                mats.materials.Add(p.material);
            mats.UpdateMaterials();
            p.material.SetVolumeTexture(BakedShadowVolumeTextureProperty, this, transform.position);
        }

        public override bool PEGI() {
            
            bool changed = base.PEGI();
            
            changed |= mats.PEGI();
            
            if (tex != null && tex.texture2D != null && "Recalculate".Click().nl()) {
                changed = true;
                RecalculateVolume(transform.position);
                VolumeToTexture();

                Vector3 pos = transform.position;

                mats.materials.SetVolumeTexture(BakedShadowVolumeTextureProperty, this, transform.position);
            }

            return changed;
        }
    }
}