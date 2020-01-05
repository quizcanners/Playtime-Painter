using System.Collections.Generic;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using UnityEngine;

namespace PlaytimePainter
{
    [ExecuteInEditMode]
    public class VolumeRayTrace : VolumeTexture, IUseDepthProjector {

        public MaterialLightManager lights = new MaterialLightManager();

        #region Inspect

        public override bool Inspect()
        {
            var changed = base.Inspect();

            var dp = DepthProjectorCamera.Instance;

            if (inspectedElement == -1)
            {
                if (!dp)
                {
                    "Depth Projector is needed to update shadows".writeHint();

                    if ("Instantiate Depth Projector Camera".Click().nl())
                        PainterCamera.GetProjectorCamera();
                }
            }

            if ("Depth Camera ".enter(ref inspectedElement, 10).nl())
            {
                GetAllBakedDepths().write(250);

                var tex = GetAllBakedDepths();

                "Depth Mask".edit(ref tex).nl();

                pegi.nl();

                dp.Nested_Inspect();
            }

            if ("Lights ".enter(ref inspectedElement, 11).nl())
                lights.Nested_Inspect().nl(ref changed);

            return changed;
        }

        
#endregion

        public override void Update()
        {

            base.Update();
            
            lights.UpdateLightsGlobal();

        }

        public override void UpdateMaterials() {

            base.UpdateMaterials();
            
            lights.UpdateLightsGlobal();
        }

#region ProjectionUpdates
        public static RenderTexture allBakedDepthesBufferTexture;
        public static RenderTexture _allBakedDepthesTexture;
        static readonly ShaderProperty.TextureValue bakedDepthes = new ShaderProperty.TextureValue("_qcPp_RayProjectorDepthes");

        public RenderTexture GetAllBakedDepths() {

            if (!_allBakedDepthesTexture) {
                _allBakedDepthesTexture = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGBFloat)
                {
                    autoGenerateMips = false,
                    useMipMap = false
                };
                bakedDepthes.GlobalValue = _allBakedDepthesTexture;
            }

            return _allBakedDepthesTexture;
        }

        public RenderTexture GetBakedDepthsBuffer() {
            if (!allBakedDepthesBufferTexture)
            {
                allBakedDepthesBufferTexture = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGB32)
                {
                    autoGenerateMips = false,
                    useMipMap = false
                };
            }

            return allBakedDepthesBufferTexture;
        }

        private int lastUpdatedLight = 0;

        public bool ProjectorReady() 
            => lights.GetNextLight(ref lastUpdatedLight);
        
        private static List<CameraMatrixParameters> projectorCameraParams;

        public CameraMatrixParameters GetGlobalCameraMatrixParameters()
            => projectorCameraParams[lastUpdatedLight];

        public ProjectorCameraConfiguration GetProjectorCameraConfiguration()
        {
            var l = lights.GetLight(lastUpdatedLight);
            return l ? l.UpdateAndGetCameraConfiguration() : null;
        }

        public RenderTexture GetTargetTexture() => RenderTextureBuffersManager.GetReusableDepthTarget();

        public DepthProjectorCamera.Mode GetMode() => DepthProjectorCamera.Mode.Clear;

        public void AfterCameraRender(RenderTexture depthTexture) {

            var buff = GetBakedDepthsBuffer();

            PainterCamera.Inst.RenderDepth(depthTexture, buff, (ColorChanel) lastUpdatedLight);
            depthTexture.DiscardContents();

            lastUpdatedLight += 1;

            if (lastUpdatedLight >= lights.probes.Length)
            {
                PainterCamera.Inst.Render(buff, GetAllBakedDepths(), 0.5f);
                buff.DiscardContents();
                lastUpdatedLight = 0;
            }

        }

        protected override void OnBecomeActive()
        {
            lights.SetIndexesOnLightSources();
        }
        
#endregion
        
#region Initialization
        public override void OnEnable() {

            base.OnEnable();

            DepthProjectorCamera.TrySubscribeToDepthCamera(this);

            if (projectorCameraParams == null) {
                projectorCameraParams = new List<CameraMatrixParameters>();
                for (int i=0; i<3; i++)
                    projectorCameraParams.Add(new CameraMatrixParameters("rt{0}_".F(i)));
            }

            bakedDepthes.GlobalValue = _allBakedDepthesTexture;
        }

        public override void OnDisable()
        {
            base.OnDisable();


        }


#endregion
    }
}
