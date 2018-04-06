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
    
    public class VolumeShadowProbe : BakedShadowMaterialController {
        
        public VolumeTexture vt;

        public void OnDrawGizmosSelected() {
            vt.DrawGizmo(transform.position, Color.green);
        }

        public override bool PEGI() {
            
            bool changed = base.PEGI();
            
            changed |= vt.PEGI();
            
            if (vt.tex != null && "Recalculate".Click().nl()) {
                changed = true;
                vt.RecalculateVolume(transform.position);
                vt.VolumeToTexture();

                Vector3 pos = transform.position;

                material.SetVolumeTexture("_BakedShadow_VOL", vt, transform.position);
            }

            return changed;
        }
    }
}