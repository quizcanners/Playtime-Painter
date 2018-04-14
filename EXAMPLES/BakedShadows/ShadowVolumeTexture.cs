using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;


namespace Playtime_Painter {

#if UNITY_EDITOR

    using UnityEditor;

    [CustomEditor(typeof(ShadowVolumeTexture))]
    public class ShadowVolumeTextureEditor : Editor {

        public override void OnInspectorGUI() {
            ef.start(serializedObject);
            ((ShadowVolumeTexture)target).PEGI();
            ef.end();
        }
    }
#endif
    
    [System.Serializable]
    [ExecuteInEditMode]
    public class ShadowVolumeTexture : VolumeTexture {

        public MaterialLightManager lights;
        public override string MaterialPropertyName{ get{  return "_BakedShadow" + VolumePaintingPlugin.VolumeTextureTag;  } }
        
        public override void Update() {
            base.Update();
            lights.UpdateLightOnMaterials(materials);
        }

        public override void OnEnable() {
            base.OnEnable();

            if (lights == null) 
                lights = new MaterialLightManager();

         //   all.Add(this);
            
        }

        public override void OnDisable() {
            base.OnDisable();

           // if (all.Contains(this))
             //   all.Remove(this);
        }

        public override void AssignTo(PlaytimePainter p) {
            base.AssignTo(p);
            lights.UpdateLightOnMaterials(materials);
            UpdateVolumePositionOnMaterials();
        }

        void UpdateMaterials() {
            UpdateVolumePositionOnMaterials();
            lights.UpdateLightOnMaterials(materials);
        }

        public override bool PEGI() {
            
            bool changed = base.PEGI();

            changed |= lights.PEGI();
            
            
            if (tex != null && tex.texture2D != null && "Recalculate ".Click().nl()) {
                changed = true;
                RecalculateVolume(transform.position);
                VolumeToTexture();
            }

            if (changed) 
                UpdateMaterials();
            

            return changed;
        }
    }
}