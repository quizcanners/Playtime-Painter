using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace Playtime_Painter {

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

        public override bool DrawGizmosOnPainter(PlaytimePainter pntr)
        {

            for (int i = 0; i < 3; i++)
                if (GlobalBrush.mask.GetFlag(i)) {
                var l = lights.GetLight(i);
                if (l!= null) {
                    Gizmos.color =  i == 0 ? Color.red : (i == 1 ? Color.green : Color.blue);

                    Gizmos.DrawLine(pntr.stroke.posTo, l.transform.position);

                }
            }
            return true;  
        }



        public void RecalculateVolumeFast()
        {
            int w = Width;
            int h = Height;
            Vector3 center = transform.position;

            float hw = Width * 0.5f;

            var col = new Color(1,1,1,1);
            for (int i = 0; i < volume.Length; i++)
                volume[i] = col; 


                Vector3 pos = Vector3.zero;

            for (int l = 0; l < 3; l++)
            {
                var light = lights.GetLight(l);

                if (light != null)
                    for (int side = 0; side < 3; side++) {

                        int addY = side == 0 ? h - 1 : 1;
                        int addX = side == 1 ? w - 1 : 1;
                        int addZ = side == 2 ? w - 1 : 1;

                        for (int y = 0; y < h; y += addY) {

                            pos.y = center.y + y * size;

                            for (int x = 0; x < w; x += addX) {

                                pos.x = center.x + ((float)(x - hw)) * size;
                                
                                for (int z = 0; z < w; z += addZ) {

                                    pos.z = center.z + ((float)(z - hw)) * size;

                                    RaycastHit hit;

                                    bool isHit = light.transform.position.RaycastHit(pos, out hit);

                                    if (isHit) {

                                        var vector = pos - light.transform.position; 

                                        var hitDist = hit.distance;
                                        
                                        float steps = Mathf.FloorToInt(vector.magnitude);

                                        vector /= steps;

                                        float step = vector.magnitude;

                                        float dist = 0;

                                        Vector3 tracePos = pos;

                                        for (int i = 0; i < steps; i++)
                                        {
                                            tracePos += vector;

                                            int HH = Mathf.FloorToInt((tracePos.y - center.y) / size);

                                            if (HH >= 0 && HH < h)
                                            {
                                                int YY = Mathf.FloorToInt((tracePos.z - center.z) / size + hw);

                                                if (YY >= 0 && YY < w)
                                                {
                                                    int XX = Mathf.FloorToInt((tracePos.x - center.x) / size + hw);

                                                    if (XX >= 0 && XX < w) {

                                                        int index = (HH * Width + YY) * Width + XX;
                                                        volume[index][l] = dist < hitDist ? 1 : 0;

                                                    }
                                                }
                                            }
                                            
                                            dist += step;

                                        }
                                    }
                                }
                            }
                        }
                    }
            }
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
                } else {
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
        }

        Vector3[] rayOffsets;

        public override void RecalculateVolume() {
            
            int volumeLength = Width * Width * Height;

            if (volume == null || volume.Length != volumeLength)
                volume = new Color[volumeLength];
            
            RecalculateVolumeFast();
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


            if (ImageData != null && ImageData.texture2D != null) {
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

                if (recalc) {
                    changed = true;
                    RecalculateVolume();
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