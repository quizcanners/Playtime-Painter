using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using Unity.Jobs;
using Unity.Collections;
using System;

namespace Playtime_Painter
{
    
    [ExecuteInEditMode]
    public class ShadowVolumeTexture : VolumeTexture {

        private enum RayCastStep { Nothing, Requested, RayCasting, FillingTheColor }

        public MaterialLightManager lights;

        protected override string PropertyName => "BakedShadow";

        public override bool VolumeJobIsRunning => _rayStep != RayCastStep.Nothing || base.VolumeJobIsRunning;

        [NonSerialized] private RayCastStep _rayStep = RayCastStep.Nothing;

        public bool[] lightSourceDirty = new bool[MaterialLightManager.maxLights];

        public override void Update() {
            base.Update();
            lights.UpdateLightOnMaterials(materials);
            UpdateRayCasts();
        }

        public override bool DrawGizmosOnPainter(PlaytimePainter painter) {

            for (var i = 0; i < MaterialLightManager.maxLights; i++)
                if (GlobalBrush.mask.HasFlag(i))
                {
                    var l = lights.GetLight(i);
                    
                    if (!l) continue;
                    
                    Gizmos.color = i == 0 ? Color.red : (i == 1 ? Color.green : Color.blue);

                    Gizmos.DrawLine(painter.stroke.posTo, l.transform.position);
                }
            return true;
        }

        private int _rayJobChannel;

        public override void OnEnable()
        {
            base.OnEnable();

            if (lights == null)
                lights = new MaterialLightManager();
        }

        public override void OnDisable()
        {
            CompleteAndDisposeAll();

            base.OnDisable();
        }

        #region Shadow Baking Jobs

        private struct JobToFillTheArray : IJob
        {

            public NativeArray<Color> volume;
            [ReadOnly] public NativeArray<RaycastHit> hitJobResults;
            [ReadOnly] public NativeArray<Vector3> hitJobDirection;
            [ReadOnly] public NativeArray<byte> gotHit;
            [ReadOnly] public int channelIndex;
            [ReadOnly] public float size;
            [ReadOnly] public int w;
            [ReadOnly] public int height;
            [ReadOnly] public Vector3 center;
            [ReadOnly] public Vector3 lPos;

            public void Execute()
            {

                for (var i = 0; i < volume.Length; i++)
                {
                    var col = volume[i];
                    col[channelIndex] = 0;
                    volume[i] = col;
                }

                var deSize = 1f / size;
                var hw = (int)(w * 0.5f);

                var lightLocalPos = lPos - center;

                for (var ind = 0; ind < hitJobResults.Length; ind++)
                {
                    if (gotHit[ind] <= 0) continue;
                    
                    var vector = hitJobDirection[ind];

                    var magnitude = vector.magnitude;

                    var hitDist = (hitJobResults[ind].point - lPos).magnitude;

                    if (!(magnitude > hitDist)) continue;
                    
                    float steps = Mathf.FloorToInt(magnitude * 4);

                    var skipSteps = (int)(steps * (hitDist / magnitude));

                    var marchVector = vector / steps;

                    var marchStep = marchVector.magnitude;

                    var marchDist = skipSteps * marchStep;

                    var marchPos = lightLocalPos + skipSteps * marchVector;

                    for (var i = skipSteps; i < steps; i++)
                    {
                        if (marchDist > hitDist)
                        {

                            var hh = Mathf.FloorToInt((marchPos.y) * deSize);
                            if (hh >= 0 && hh < height)
                            {

                                var yy = Mathf.FloorToInt((marchPos.z) * deSize + hw);
                                if (yy >= 0 && yy < w)
                                {

                                    var xx = Mathf.FloorToInt((marchPos.x) * deSize + hw);
                                    if (xx >= 0 && xx < w)
                                    {

                                        var diff = marchDist - hitDist;
                                        var index = (hh * w + yy) * w + xx;
                                        var col = volume[index];
                                        col[channelIndex] = Mathf.Clamp01(diff * deSize - 1);
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

        private NativeArray<RaycastHit>     _hitJobResults;
        private NativeArray<RaycastCommand> _hitJobCommands;
        private NativeArray<Vector3>        _hitJobDirections;
        private NativeArray<byte>           _gotHit;
        private JobHandle                   _jobHandle;

        private void CompleteAndDisposeAll()
        {

            _jobHandle.Complete();

            if (_gotHit.IsCreated)
                _gotHit.Dispose();
            if (_hitJobResults.IsCreated)
                _hitJobResults.Dispose();
            if (_hitJobDirections.IsCreated)
                _hitJobDirections.Dispose();

            _rayStep = RayCastStep.Nothing;

            for (var i = 0; i < 3; i++)
                lightSourceDirty[i] = false;

        }

        private void UpdateRayCasts()
        {

            if (!VolumeJobIsRunning)
            {
                for (var i = 0; i < 3; i++)
                {
                    if (!lightSourceDirty[i]) continue;
                    
                    _rayJobChannel = i;
                    _rayStep = RayCastStep.Requested;
                    break;
                }
            }

            if (_rayStep == RayCastStep.Requested)
            {

                if (lights == null || !lights.GetLight(_rayJobChannel))
                {
                    _rayStep = RayCastStep.Nothing;
                    lightSourceDirty[_rayJobChannel] = false;
                    return;
                }

                _rayStep = RayCastStep.RayCasting;

                CheckVolume();

                var futureHits = RecalculateVolumePrepareJobs(_rayJobChannel, out var dirs);

                _hitJobResults = new NativeArray<RaycastHit>(futureHits.Count, Allocator.Persistent);
                _hitJobCommands = new NativeArray<RaycastCommand>(futureHits.ToArray(), Allocator.Persistent);
                _hitJobDirections = new NativeArray<Vector3>(dirs.ToArray(), Allocator.Persistent);
                _jobHandle = RaycastCommand.ScheduleBatch(_hitJobCommands, _hitJobResults, 250);

                JobHandle.ScheduleBatchedJobs();
            }

            if (_rayStep == RayCastStep.RayCasting && _jobHandle.IsCompleted)
            {

                _rayStep = RayCastStep.FillingTheColor;

                _jobHandle.Complete();

                _gotHit = new NativeArray<byte>(new byte[_hitJobCommands.Length], Allocator.Persistent);

                _hitJobCommands.Dispose();

                for (var i = 0; i < _hitJobResults.Length; i++)
                    _gotHit[i] = (byte)((_hitJobResults[i].collider) ? 1 : 0);

                var fillArray = new JobToFillTheArray()
                {

                    hitJobResults = _hitJobResults,
                    hitJobDirection = _hitJobDirections,
                    channelIndex = _rayJobChannel,
                    volume = unsortedVolume,
                    size = size,
                    w = Width,
                    height = Height,
                    center = transform.position,
                    gotHit = _gotHit,
                    lPos = lights.GetLight(_rayJobChannel).transform.position
                };

                _jobHandle = fillArray.Schedule();

                JobHandle.ScheduleBatchedJobs();
            }

            if (_rayStep == RayCastStep.FillingTheColor && _jobHandle.IsCompleted)
            {

                _jobHandle.Complete();

                lightSourceDirty[_rayJobChannel] = false;

                _gotHit.Dispose();

                _hitJobResults.Dispose();
                _hitJobDirections.Dispose();
                VolumeToTexture();

                _rayStep = RayCastStep.Nothing;
            }
        }

        private List<RaycastCommand> RecalculateVolumePrepareJobs(int channel, out List<Vector3> directions)
        {

            var futureHits = new List<RaycastCommand>();
            
            directions = new List<Vector3>();
            
            var lLight = lights.GetLight(channel);

            if (!lLight) return futureHits;
            
        

            var w = Width;
            var h = Height;
            var center = transform.position;

            var hw = Width * 0.5f;

            var pos = Vector3.zero;

            for (var side = 0; side < 3; side++)
            {

                var lPos = lLight.transform.position;

                var addY = side == 0 ? h - 1 : 1;
                var addX = side == 1 ? w - 1 : 1;
                var addZ = side == 2 ? w - 1 : 1;

                for (var y = 0; y < h; y += addY)
                {

                    pos.y = center.y + y * size;

                    for (var x = 0; x < w; x += addX)
                    {

                        pos.x = center.x + (x - hw) * size;

                        for (var z = 0; z < w; z += addZ)
                        {

                            pos.z = center.z + (z - hw) * size;

                            var vector = pos - lPos;
                            directions.Add(vector);
                            futureHits.Add(new RaycastCommand(lPos, vector, vector.magnitude * 1.5f));

                        }
                    }
                }
            }


            return futureHits;
        }

        public override void RecalculateVolume()
        {
            if (_rayStep == RayCastStep.Nothing)
                _rayStep = RayCastStep.Requested;
        }
        #endregion

        public override void UpdateMaterials() {

            base.UpdateMaterials();

            lights.UpdateLightOnMaterials(materials);

            if (IsCurrentGlobalVolume)
                lights.UpdateLightsGlobal();
        }

        #region Inspector
        #if PEGI
        protected override bool VolumeDocumentation() {
            base.VolumeDocumentation();

            ("For now only one Shadow volume is active at a time. It will set it's texture, position and lights as global shader parameters " +
             "R and G channels of the volume will hold shadow information about two light sources. B channel will hold sun/moon shadow information. " +
             "A channel will hold validity of the rayCast (Will be lover if value is approximated. ANd higher if it is more reliable.)" +
             "This is arbitrary for now to simplify management of shadow volumes. But this limitations are easy to overcome if needed.")
                .writeBig();

            return false;
        }

        public override bool Inspect()  {
            
            var changed = base.Inspect();

            if (inspectedMaterial != -1)
                return changed;

       
            lights.Nested_Inspect().nl(ref changed);
            
            if (changed && MaterialLightManager.probeChanged != -1)
                lightSourceDirty[MaterialLightManager.probeChanged] = true;
            
            if (ImageMeta != null && ImageMeta.texture2D) {

                if (!VolumeJobIsRunning) {

                    "Channel: ".edit(ref _rayJobChannel, 0, 2).changes(ref changed);

                    if (icon.Refresh.Click("Recalculate ", ref changed).nl())
                    {
                        VolumeFromTexture();
                        lightSourceDirty[_rayJobChannel] = true;
                        _rayStep = RayCastStep.Requested;
                    }

                    if ("All".Click().nl(ref changed))
                    {
                        VolumeFromTexture();
                        for (var i = 0; i < MaterialLightManager.maxLights; i++)
                            lightSourceDirty[i] = true;
                    }
                }
                else
                {
                    "Recalculating channel {0} : {1}".F(_rayJobChannel, _rayStep.ToString()).write();

                    if (icon.Close.Click("Stop Recalculations").nl())
                        CompleteAndDisposeAll();
                }

            } else  {
                if (ImageMeta == null)
                    "Image Data is Null".nl();
                else
                    "Texture 2D is null".nl();
            }
            if (changed)
                UpdateMaterials();

            return changed;
            
        }

        #endif
        #endregion
    }

}