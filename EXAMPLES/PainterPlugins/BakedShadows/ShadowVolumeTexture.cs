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

        [NonSerialized] RaycastsStep rayStep = RaycastsStep.Nothing;
        int rayJobChannel = 0;


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

        public NativeArray<Color> colorJob;

        public struct JobToFillTheArray : IJob
        {

            public NativeArray<Color> volume;
            [ReadOnly]  public NativeArray<RaycastHit> hitJobResults;
            [ReadOnly]  public NativeArray<RaycastCommand> hitJobCommands;
            [DeallocateOnJobCompletion]  [ReadOnly]  public NativeArray<byte> gotHit;
            [DeallocateOnJobCompletion] [ReadOnly]  public NativeArray<int> channelIndex;
            [DeallocateOnJobCompletion] [ReadOnly]  public NativeArray<float> Size;
            [DeallocateOnJobCompletion] [ReadOnly]  public NativeArray<int> WidthNA;
            [DeallocateOnJobCompletion] [ReadOnly]  public NativeArray<int> Height;
            [DeallocateOnJobCompletion] [ReadOnly]  public NativeArray<Vector3> Center;
            [DeallocateOnJobCompletion] [ReadOnly]  public NativeArray<Vector3> LightPosition;

            public void Execute() {

                for (int ind = 0; ind < hitJobResults.Length; ind++)
                {

                    var vector = hitJobCommands[ind].direction;

                    var hit = hitJobResults[ind];

                    if (gotHit[ind] > 0 && vector.magnitude > 0)
                    {

                        float size = Size[0];
                        int w = WidthNA[0];
                        int hw = (int)(w * 0.5f);
                        int h = Height[0];
                        int l = channelIndex[0];
                        Vector3 center = Center[0];

                        var hitDist = hit.distance;

                        float steps = Mathf.FloorToInt(vector.magnitude * 4);

                        int start = (int)(steps * (hitDist / vector.magnitude));

                        vector /= steps;

                        float step = vector.magnitude;

                        float dist = 0;

                        Vector3 tracePos = LightPosition[0];

                        for (int i = start; i < steps; i++)
                        {

                            int HH = Mathf.FloorToInt((tracePos.y - center.y) / size);

                            if (HH >= 0 && HH < h)
                            {

                                int YY = Mathf.FloorToInt((tracePos.z - center.z) / size + hw);

                                if (YY >= 0 && YY < w)
                                {
                                    int XX = Mathf.FloorToInt((tracePos.x - center.x) / size + hw);

                                    if (XX >= 0 && XX < w)
                                    {
                                        int index = (HH * w + YY) * w + XX;
                                        float diff = dist - hitDist;
                                        var col = volume[index];
                                        col[l] = diff < 0 ? 0 : Mathf.Min(1, diff * size);
                                        volume[index] = col;
                                    }
                                }
                            }

                            dist += step;

                            tracePos += vector;
                        }
                    }
                }


        
           /*     gotHit.Dispose();
                channelIndex.Dispose();
                Size.Dispose();
                WidthNA.Dispose();
                Height.Dispose();
                Center.Dispose();
                LightPosition.Dispose();*/


        }
    }


        NativeArray<RaycastHit> hitJobResults;
        NativeArray<RaycastCommand> hitJobCommands;
        NativeArray<Color> result;
        NativeArray<byte> gotHit;
        JobHandle jobHandle;

        public void UpdateRaycasts()
        {
          
            if (rayStep == RaycastsStep.Requested)  {
                
                if ( lights == null || lights.GetLight(rayJobChannel) == null)  {
                    rayStep = RaycastsStep.Nothing;
                    Debug.Log("No light {0}".F(rayJobChannel));
                    return;
                }

                rayStep = RaycastsStep.Raycasting;

                int volumeLength = Width * Width * Height;

                if (volume == null || volume.Length != volumeLength)
                    volume = new Color[volumeLength];

                List<RaycastCommand> futureHits = RecalculateVolumePrepareJobs(rayJobChannel); // new List<RaycastCommand>();
                
                hitJobResults = new NativeArray<RaycastHit>(futureHits.Count, Allocator.TempJob);
                hitJobCommands = new NativeArray<RaycastCommand>(futureHits.ToArray(), Allocator.TempJob);
            
                jobHandle = RaycastCommand.ScheduleBatch(hitJobCommands, hitJobResults, 250);

                JobHandle.ScheduleBatchedJobs();

            }

            if (rayStep == RaycastsStep.Raycasting && jobHandle.IsCompleted) {

                rayStep = RaycastsStep.FillingTheColor;

                jobHandle.Complete();
                
                result = new NativeArray<Color>(volume, Allocator.Persistent);

                gotHit = new NativeArray<byte>(new byte[hitJobCommands.Length], Allocator.TempJob);

                for (int i = 0; i < hitJobResults.Length; i++)
                    gotHit[i] = (byte)((hitJobResults[i].collider != null) ? 1 : 0);
                
                  var fillArray = new JobToFillTheArray() {

                      hitJobResults = hitJobResults,
                      hitJobCommands = hitJobCommands,
                      channelIndex = new NativeArray<int>(new int[] { rayJobChannel }, Allocator.TempJob),
                      volume = result,
                      Size = new NativeArray<float>(new float[] { size }, Allocator.TempJob),
                      WidthNA = new NativeArray<int>(new int[] { Width }, Allocator.TempJob),
                      Height = new NativeArray<int>(new int[] { Height }, Allocator.TempJob),
                      Center = new NativeArray<Vector3>(new Vector3[] { transform.position }, Allocator.TempJob),
                      gotHit = gotHit,
                      LightPosition = new NativeArray<Vector3>(new Vector3[] { lights.GetLight(rayJobChannel).transform.position }, Allocator.TempJob),
                  };

                jobHandle = fillArray.Schedule();

                JobHandle.ScheduleBatchedJobs();

            }

            if (rayStep == RaycastsStep.FillingTheColor && jobHandle.IsCompleted) {
                
                try
                {
                    jobHandle.Complete();
                    volume = result.ToArray();
                }
                catch (Exception ex)  {
                    Debug.LogError(ex);

                  
                }

                rayStep = RaycastsStep.Nothing;

                result.Dispose();
                hitJobResults.Dispose();
                hitJobCommands.Dispose();
                VolumeToTexture();
            }

        }


        public List<RaycastCommand> RecalculateVolumePrepareJobs(int channel)  {

            List<RaycastCommand> futureHits = new List<RaycastCommand>();

            int w = Width;
            int h = Height;
            Vector3 center = transform.position;

            float hw = Width * 0.5f;

            var col = new Color(0, 0, 0, 1);
            for (int i = 0; i < volume.Length; i++)
                volume[i] = col;

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

                                    futureHits.Add(new RaycastCommand(lpos, vector));
                                }
                            }
                        }
                    }
            

            return futureHits;
        }


        /*
        public void RecalculateVolumeFast()
        {
            int w = Width;
            int h = Height;
            Vector3 center = transform.position;

            float hw = Width * 0.5f;

            var col = new Color(0,0,0,1);
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

                                    var vector = pos - light.transform.position;

                                    if (vector.magnitude > 0 && light.transform.position.RaycastHit(pos, out hit)) {

                                        var hitDist = hit.distance;
                                        
                                        float steps = Mathf.FloorToInt(vector.magnitude * 4);

                                        int start = (int)(steps * (hitDist / vector.magnitude));

                                        vector /= steps;

                                        float step = vector.magnitude;

                                        float dist = 0;

                                        Vector3 tracePos = light.transform.position;

                                        for (int i = start; i < steps; i++) {
                                          
                                            int HH = Mathf.FloorToInt((tracePos.y - center.y) / size);

                                            if (HH >= 0 && HH < h)
                                            {
                                                int YY = Mathf.FloorToInt((tracePos.z - center.z) / size + hw);

                                                if (YY >= 0 && YY < w)
                                                {
                                                    int XX = Mathf.FloorToInt((tracePos.x - center.x) / size + hw);

                                                    if (XX >= 0 && XX < w) {

                                                        int index = (HH * Width + YY) * Width + XX;
                                                        float diff = dist - hitDist;
                                                        volume[index][l] = diff < 0 ? 0 : Mathf.Min(1, diff*size);

                                                    }
                                                }
                                            }
                                            
                                            dist += step;

                                            tracePos += vector;
                                        }
                                    }
                                }
                            }
                        }
                    }
            }
        }
        */

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
        public override bool PEGI()
        {

            bool changed = base.PEGI();

            changed |= lights.Nested_Inspect();

           

            if (ImageData != null && ImageData.texture2D != null) {


                if (rayStep == RaycastsStep.Nothing)
                {

                    "Channel: ".edit(ref rayJobChannel, 0, 3).nl();

                    if ("Recalculate ".Click().nl())
                    {
                        changed = true;
                        rayStep = RaycastsStep.Requested;
                    }
                }
                else
                {

                    "Recalculating channel {0} : {1}".F(rayJobChannel, rayStep.ToString()).nl();
                }

            } else
            {
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