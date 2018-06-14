using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace Playtime_Painter {

#if PEGI && UNITY_EDITOR 

    using UnityEditor;

    [CustomEditor(typeof(ShadowVolumeTexture))]
    public class ShadowVolumeTextureEditor : Editor {

        public override void OnInspectorGUI() => ((ShadowVolumeTexture)target).inspect(serializedObject);
        
    }
#endif

    [System.Serializable]
    [ExecuteInEditMode]
    public class ShadowVolumeTexture : VolumeTexture {

        public MaterialLightManager lights;
        public override string MaterialPropertyName{ get{  return "_BakedShadow" + VolumePaintingPlugin.VolumeTextureTag;  } }
        public bool recalculatePrecise;
        
        public override void Update() {
            base.Update();
            lights.UpdateLightOnMaterials(materials);
        }

        public override void OnEnable() {
            base.OnEnable();

            if (lights == null) 
                lights = new MaterialLightManager();
  
        }

        public override void OnDisable() {
            base.OnDisable();

        }

        public override bool DrawGizmosOnPainter(PlaytimePainter pntr)
        {

            for (int i = 0; i < 3; i++)
                if (globalBrush.mask.GetFlag(i)) {
                var l = lights.GetLight(i);
                if (l!= null) {
                    Gizmos.color =  i == 0 ? Color.red : (i == 1 ? Color.green : Color.blue);

                    Gizmos.DrawLine(pntr.stroke.posTo, l.transform.position);

                }
            }
            return true;  
        }

        public override Color GetColorFor(Vector3 pos) {

            Color col = Color.black;

            float defaultAmbient = 0.8f;

            if (!recalculatePrecise)
                for (int i = 0; i < 3; i++)
                {
                    var l = lights.GetLight(i);
                    if (l != null)
                        col[i] = l.transform.position.RaycastGotHit(pos, size*2) ? defaultAmbient : 0;
                }
            else
            {
                float portion = defaultAmbient / 8f;
                for (int i = 0; i < 3; i++) {
                    var l = lights.GetLight(i);
                    if (l != null) {
                        if (l.transform.position.RaycastGotHit(pos))
                            col[i] = defaultAmbient;
                        else
                        for (int o = 0; o < 8; o++)
                            col[i] += l.transform.position.RaycastGotHit(pos + rayOffsets[o], size*2) ? portion : 0;
                    }
                }
            }

            col.a = 0.5f;

            return col;
            //return base.GetColorFor(pos);
        }

        Vector3[] rayOffsets;

        public override void RecalculateVolume(Vector3 center)
        {
            if (recalculatePrecise) {
                rayOffsets = new Vector3[8];
                float off = size * 0.45f;

                int ind = 0;

                for (int y = -1; y < 2; y += 2)
                    for (int x = -1; x < 2; x += 2)
                        for (int z = -1; z < 2; z += 2)
                        {
                            rayOffsets[ind] = (new Vector3(x, z, y)) * off;
                            ind++;
                        }
            }

            base.RecalculateVolume(center);
        }

        public override void UpdateMaterials()
        {
            base.UpdateMaterials();
            lights.UpdateLightOnMaterials(materials);
        }
        #if PEGI
        public override bool PEGI() {
            
            bool changed = base.PEGI();

            changed |= lights.Nested_Inspect();


            if (imageData != null && imageData.texture2D != null) {
                bool recalc = false;

                if ("Recalculate ".Click().nl()) {
                    recalc = true;
                    recalculatePrecise = false;
                }

                if ("Recalculate Precise".Click().nl())
                {
                    recalc = true;
                    recalculatePrecise = true;
                }

                if (recalc)
                {
                    changed = true;
                    RecalculateVolume(transform.position);
                    VolumeToTexture();
                }
                
            }

            if (changed)
                UpdateMaterials();



            return changed;
        }
#endif
    }
}