using System.Collections.Generic;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using UnityEngine;

namespace Playtime_Painter
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
                        UnityUtils.Instantiate<DepthProjectorCamera>();
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

            if (IsCurrentGlobalVolume)
                lights.UpdateLightsGlobal();

        }

        public override void UpdateMaterials() {

            base.UpdateMaterials();

            if (IsCurrentGlobalVolume)
                lights.UpdateLightsGlobal();

        }

        #region ProjectionUpdates

        public static RenderTexture allBakedDepthesTexture;
        static readonly ShaderProperty.TextureValue bakedDepthes = new ShaderProperty.TextureValue("_pp_RayProjectorDepthes");

        public RenderTexture GetAllBakedDepths() {

            if (!allBakedDepthesTexture) {
                allBakedDepthesTexture = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGB32);
                bakedDepthes.GlobalValue = allBakedDepthesTexture;
            }

            return allBakedDepthesTexture;
        }

        private int lastUpdatedLight = 0;

        public bool ProjectorReady() 
            => IsCurrentGlobalVolume && lights.GetNextLight(ref lastUpdatedLight);
        
        private static List<ProjectorCameraParameters> projectorCameraParams;

        public ProjectorCameraParameters GetProjectionParameter()
            => projectorCameraParams[lastUpdatedLight];

        public ProjectorCameraConfiguration GetProjectorCameraConfiguration()
        {

            var l = lights.GetLight(lastUpdatedLight);
              return l ? l.UpdateAndGetCameraConfiguration() : null;
        }

        public RenderTexture GetTargetTexture() => DepthProjectorCamera.GetReusableDepthTarget();

        public void AfterDepthCameraRender(RenderTexture depthTexture)
        {

           // Debug.Log("Updating Baked depths");
            //PainterCamera.Inst.Render(depthTexture, GetAllBakedDepths());

             PainterCamera.Inst.RenderDepth(depthTexture, GetAllBakedDepths(), (ColorChanel) lastUpdatedLight);
            depthTexture.DiscardContents();

            //Debug.Log("Updated light {0}".F(lastUpdatedLight));

            lastUpdatedLight = (lastUpdatedLight + 1) % lights.probes.Length;

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
