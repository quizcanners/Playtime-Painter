using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using SharedTools_Stuff;
using Unity.Jobs;
using Unity.Collections;
using System;

namespace Playtime_Painter {

    [Serializable]
    [ExecuteInEditMode]
    public class ShadowVolumeTexture : VolumeTexture {

        enum RaycastsStep { Nothing, Requested, Raycasting, FillingTheColor }

        public MaterialLightManager lights;
        public override string MaterialPropertyName{ get{  return "_BakedShadow" + VolumePaintingPlugin.VolumeTextureTag;  } }

        public override bool volumeJobIsRunning => rayStep!= RaycastsStep.Nothing || base.volumeJobIsRunning;

        [NonSerialized] RaycastsStep rayStep = RaycastsStep.Nothing;
        int rayJobChannel = 0;

        public bool[] lightSourceDirty = new bool[3];

        public override void Update() {
            base.Update();
            lights.UpdateLightOnMaterials(materials);
            UpdateRaycasts();
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

        public struct JobToFillTheArray : IJob {

            public NativeArray<Color> volume;
             [ReadOnly]  public NativeArray<RaycastHit> hitJobResults;
             [ReadOnly] public NativeArray<Vector3> hitJobDirection;
             [ReadOnly]  public NativeArray<byte> gotHit;
             [ReadOnly]  public int channelIndex;
             [ReadOnly]  public float size;
             [ReadOnly]  public int w;
             [ReadOnly]  public int height;
             [ReadOnly]  public Vector3 center;
             [ReadOnly]  public Vector3 lpos;

            public void Execute() {

                for (int i=0; i< volume.Length; i++) {
                    var col = volume[i];
                    col[channelIndex] = 0;
                    volume[channelIndex] = col;
                }

                float deSize = 1f/size;
                int hw = (int)(w * 0.5f);

                Vector3 lightLocalPos = lpos - center;

                for (int ind = 0; ind < hitJobResults.Length; ind++) {

                    if (gotHit[ind] > 0) {

                        var vector = hitJobDirection[ind];

                        float magnitude = vector.magnitude;

                        var hitDist = (hitJobResults[ind].point - lpos).magnitude;

                        if (magnitude > hitDist) {
                            
                            float steps = Mathf.FloorToInt(magnitude * 4);

                            int skipSteps = (int)(steps * (hitDist / magnitude));

                            var marchVector = vector / steps;

                            float marchStep = marchVector.magnitude;

                            float marchDist = skipSteps * marchStep;

                            Vector3 marchPos = lightLocalPos + skipSteps * marchVector;

                            for (int i = skipSteps; i < steps; i++) {

                                if (marchDist > hitDist)  {

                                    int HH = Mathf.FloorToInt((marchPos.y) * deSize);
                                    if (HH >= 0 && HH < height) {

                                        int YY = Mathf.FloorToInt((marchPos.z) * deSize + hw);
                                        if (YY >= 0 && YY < w) {

                                            int XX = Mathf.FloorToInt((marchPos.x) * deSize + hw);
                                            if (XX >= 0 && XX < w) {

                                                float diff = marchDist - hitDist;
                                                int index = (HH * w + YY) * w + XX;
                                                var col = volume[index];
                                                col[channelIndex] = Mathf.Min(diff * size, 1);
                                                volume[index] = col;
                                            }
                                        }
                                    }
                                }

                                marchDist += marchStep;
                                marchPos += marchVector;
                            }
                        }
                    }
                }
            }
        }

        NativeArray<RaycastHit> hitJobResults;
        NativeArray<RaycastCommand> hitJobCommands;
        NativeArray<Vector3> hitJobDirections;
        NativeArray<byte> gotHit;
        JobHandle jobHandle;

        public void UpdateRaycasts() {

            if (!volumeJobIsRunning) {
                for (int i = 0; i < 3; i++) {
                    if (lightSourceDirty[i]) {
                        rayJobChannel = i;
                        rayStep = RaycastsStep.Requested;
                        break;
                    }
                }
            }

            if (rayStep == RaycastsStep.Requested)  {
                
                if ( lights == null || lights.GetLight(rayJobChannel) == null)  {
                    rayStep = RaycastsStep.Nothing;
                    Debug.Log("No light {0}".F(rayJobChannel));
                    return;
                }

                rayStep = RaycastsStep.Raycasting;

                CheckVolume();

                List<Vector3> dirs; //= new List<Vector3>();

                List<RaycastCommand> futureHits = RecalculateVolumePrepareJobs(rayJobChannel, out dirs); 

                hitJobResults = new NativeArray<RaycastHit>(futureHits.Count, Allocator.TempJob);
                hitJobCommands = new NativeArray<RaycastCommand>(futureHits.ToArray(), Allocator.TempJob);
                hitJobDirections = new NativeArray<Vector3>(dirs.ToArray(), Allocator.TempJob);
                jobHandle = RaycastCommand.ScheduleBatch(hitJobCommands, hitJobResults, 250);

                JobHandle.ScheduleBatchedJobs();

            }

            if (rayStep == RaycastsStep.Raycasting && jobHandle.IsCompleted) {

                rayStep = RaycastsStep.FillingTheColor;

                jobHandle.Complete();
                
                gotHit = new NativeArray<byte>(new byte[hitJobCommands.Length], Allocator.TempJob);

                hitJobCommands.Dispose();

                for (int i = 0; i < hitJobResults.Length; i++)
                    gotHit[i] = (byte)((hitJobResults[i].collider != null) ? 1 : 0);
                
                  var fillArray = new JobToFillTheArray() {

                      hitJobResults = hitJobResults,
                      hitJobDirection = hitJobDirections, 
                      channelIndex =  rayJobChannel,
                      volume = unsortedVolume,
                      size =  size,
                      w =  Width,
                      height  = Height ,
                      center =  transform.position ,
                      gotHit = gotHit,
                      lpos = lights.GetLight(rayJobChannel).transform.position ,
                  };

                jobHandle = fillArray.Schedule();

                JobHandle.ScheduleBatchedJobs();
            }

            if (rayStep == RaycastsStep.FillingTheColor && jobHandle.IsCompleted) {
                
                jobHandle.Complete();
                
                lightSourceDirty[rayJobChannel] = false;
                
                gotHit.Dispose();
                hitJobResults.Dispose();
                hitJobDirections.Dispose();
                VolumeToTexture();

                rayStep = RaycastsStep.Nothing;
            }

        }
        
        public List<RaycastCommand> RecalculateVolumePrepareJobs(int channel, out List<Vector3> directions)  {

            directions = new List<Vector3>();

            List<RaycastCommand> futureHits = new List<RaycastCommand>();

            int w = Width;
            int h = Height;
            Vector3 center = transform.position;

            float hw = Width * 0.5f;

            Vector3 pos = Vector3.zero;
            
                var light = lights.GetLight(channel);

                if (light != null)
                    for (int side = 0; side < 3; side++)
                    {

                        Vector3 lpos = light.transform.position;

                        int addY = side == 0 ? h - 1 : 1;
                        int addX = side == 1 ? w - 1 : 1;
                        int addZ = side == 2 ? w - 1 : 1;

                        for (int y = 0; y < h; y += addY)
                        {

                            pos.y = center.y + y * size;

                            for (int x = 0; x < w; x += addX)
                            {

                                pos.x = center.x + ((float)(x - hw)) * size;

                                for (int z = 0; z < w; z += addZ)
                                {

                                    pos.z = center.z + ((float)(z - hw)) * size;

                                    var vector = pos - lpos;
                                    directions.Add(vector);
                                    futureHits.Add(new RaycastCommand(lpos, vector, vector.magnitude*1.5f));
                                }
                            }
                        }
                    }
            

            return futureHits;
        }
        
        public override void RecalculateVolume() {
            if (rayStep == RaycastsStep.Nothing)
                rayStep = RaycastsStep.Requested;
        }

        public override void UpdateMaterials()
        {
            base.UpdateMaterials();
            lights.UpdateLightOnMaterials(materials);
        }

#if PEGI
        public override bool PEGI()  {

            bool changed = base.PEGI();

            changed |= lights.Nested_Inspect();
            
            if (ImageData != null && ImageData.texture2D != null) {
                
                if (!volumeJobIsRunning) {

                    "Channel: ".edit(ref rayJobChannel, 0, 2).nl();

                    if ("Recalculate ".Click()) {
                        changed = true;
                        VolumeFromTexture();
                        lightSourceDirty[rayJobChannel] = true;
                        rayStep = RaycastsStep.Requested;
                    }

                    if ("All".Click().nl()) {
                        changed = true;
                        VolumeFromTexture();
                        for (int i = 0; i < 3; i++)
                            lightSourceDirty[i] = true;
                    }
                }
                else
                    "Recalculating channel {0} : {1}".F(rayJobChannel, rayStep.ToString()).nl();
                
            } else  {
                if (ImageData == null)
                    "Image Data is Null".nl();
                else
                    "Texture 2D is null".nl();
            }

            if (changed)
                UpdateMaterials();

            return changed;
            
        }
#endif
    }
}