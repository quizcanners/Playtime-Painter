using System.Collections.Generic;
using QuizCannersUtilities;
using UnityEngine;

namespace Playtime_Painter
{
    [ExecuteInEditMode]
    public class VolumeRayTrace : VolumeTexture, IUseDepthProjector {

        public MaterialLightManager lights = new MaterialLightManager();

        #region ProjectionUpdates

        public static RenderTexture allBakedDepthesTexture;
        static readonly ShaderProperty.TextureValue bakedDepthes = new ShaderProperty.TextureValue("_ProjectorDepthes");

        public RenderTexture GetAllBakedDepths() {

            if (!allBakedDepthesTexture) {
                allBakedDepthesTexture = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGB32);
                bakedDepthes.GlobalValue = allBakedDepthesTexture;
            }

            return allBakedDepthesTexture;
        }

        private int lastUpdatedLight = 0;

        public bool ProjectorReady() 
            => lights.GetNextLight(ref lastUpdatedLight);
        
        private static List<ProjectorCameraParameters> projectorCameraParams;

        public ProjectorCameraParameters GetProjectorCameraParameter()
            => projectorCameraParams[lastUpdatedLight];

        public ProjectorCameraConfiguration GetProjectorCameraConfiguration()
        {

            var l = lights.GetLight(lastUpdatedLight);
              return l ? l.cameraConfiguration : null;
        }

        public RenderTexture GetTargetTexture() => DepthProjectorCamera.GetReusableDepthTarget();

        public void AfterDepthCameraRender(RenderTexture depthTexture)
        {
            PainterCamera.Inst.RenderDepth(depthTexture, GetAllBakedDepths(), (ColorChanel) lastUpdatedLight);
            depthTexture.DiscardContents();
        }

        protected override string PropertyNameRoot => "BakedRays";

        #endregion
        
        #region Initialization
        public override void OnEnable() {

            base.OnEnable();

            DepthProjectorCamera.SubscribeToDepthCamera(this);

            if (projectorCameraParams == null) {
                projectorCameraParams = new List<ProjectorCameraParameters>();
                for (int i=0; i<3; i++)
                    projectorCameraParams.Add(new ProjectorCameraParameters("rt{0}_".F(i)));
            }

            bakedDepthes.GlobalValue = allBakedDepthesTexture;
        }

    
        #endregion
    }
}
