using QuizCanners.Lerp;
using System.Collections.Generic;
using UnityEngine;
using QuizCanners.Migration;
using QuizCanners.Inspect;
using QuizCanners.Utils;
#if UNITY_EDITOR
using UnityEditorInternal;
#endif


namespace PlaytimePainter.Modules
{
    public class PlaytimePainter_SceneLightingManager : MonoBehaviour, ICfgCustom, IPEGI, ILinkedLerping
    {
        [SerializeField] private PlaytimePainter_SceneLighting_Configurations _configs;
        [SerializeField] private Light _mainDirectionalLight;

        private readonly LinkedLerp.FloatValue brightness = new LinkedLerp.FloatValue(1, 1, "Brightness");
        private readonly LinkedLerp.FloatValue colorBleed = new LinkedLerp.FloatValue(0, 0.1f, "Color Bleed");

        private readonly LinkedLerp.ColorValue mainLightColor = new LinkedLerp.ColorValue("Light Color");
        private readonly LinkedLerp.FloatValue mainLightIntensity = new LinkedLerp.FloatValue(name: "Main Light Intensity");
        private readonly LinkedLerp.QuaternionValue mainLightRotation = new LinkedLerp.QuaternionValue("Main light rotation");

        private readonly LinkedLerp.ColorValue fogColor = new LinkedLerp.ColorValue("Fog Color");
        private readonly LinkedLerp.ColorValue skyColor = new LinkedLerp.ColorValue("Sky Color");
        private readonly LinkedLerp.FloatValue shadowStrength = new LinkedLerp.FloatValue(1, name: "Shadow Strength");
        private readonly LinkedLerp.FloatValue shadowDistance = new LinkedLerp.FloatValue(100, 500, 10, 1000, "Shadow Distance");
        private readonly LinkedLerp.FloatValue fogDistance = new LinkedLerp.FloatValue(100, 500, 0.01f, 1000, "Fog Distance");
        private readonly LinkedLerp.FloatValue fogDensity = new LinkedLerp.FloatValue(0.01f, 0.01f, 0.00001f, 0.1f, "Fog Density");
        private readonly ShaderProperty.VectorValue _lightProperty = new ShaderProperty.VectorValue("pp_COLOR_BLEED");
        private void UpdateShader() => _lightProperty.GlobalValue = new Vector4(colorBleed.CurrentValue, 0, 0, brightness.CurrentValue);


       


        #region Encode & Decode

        public void ReadCurrentValues()
        {
            fogColor.TargetAndCurrentValue = RenderSettings.fogColor;

            if (RenderSettings.fog)
            {
                fogDistance.TargetAndCurrentValue = RenderSettings.fogEndDistance;
                fogDensity.TargetAndCurrentValue = RenderSettings.fogDensity;
            }

            skyColor.TargetAndCurrentValue = RenderSettings.ambientSkyColor;
            shadowDistance.TargetAndCurrentValue = QualitySettings.shadowDistance;

            if (_mainDirectionalLight)
            {
                mainLightColor.TargetAndCurrentValue = _mainDirectionalLight.color;
                mainLightIntensity.TargetAndCurrentValue = _mainDirectionalLight.intensity;
                mainLightRotation.TargetAndCurrentValue = _mainDirectionalLight.transform.rotation;
            }
        }


        public CfgEncoder Encode()
        {

            if (!enabled)
                ReadCurrentValues();

            var cody = new CfgEncoder()
                    .Add("sh", shadowStrength.TargetValue)
                    .Add("sdst", shadowDistance)
                    .Add("sc", skyColor.targetValue)
                    .Add_Bool("fg", RenderSettings.fog)
                    .Add("lcol", mainLightColor)
                    .Add("lint", mainLightIntensity)
                ;

            cody.Add("lr", mainLightRotation);

            cody.Add("br", brightness);
            cody.Add("bl", colorBleed);

            if (RenderSettings.fog)
                cody.Add("fogCol", fogColor.targetValue)
                    .Add("fogD", fogDistance)
                    .Add("fogDen", fogDensity);

            return cody;
        }

        public void Decode(string tg, CfgData data)
        {
            switch (tg)
            {
                case "sh": shadowStrength.TargetValue = data.ToFloat(); break;
                case "sdst": shadowDistance.DecodeFull(data); break;
                case "sc": skyColor.targetValue = data.ToColor(); break;
                case "fg": RenderSettings.fog = data.ToBool(); break;
                case "fogD": fogDistance.DecodeFull(data); break;
                case "fogDen": fogDensity.DecodeFull(data); break;
                case "fogCol": fogColor.targetValue = data.ToColor(); break;
                case "lr": mainLightRotation.DecodeFull(data); break;
                case "lcol": mainLightColor.DecodeFull(data); break;
                case "lint": mainLightIntensity.DecodeFull(data); break;
                case "br": brightness.DecodeFull(data); break;
                case "bl": colorBleed.DecodeFull(data); break;
            }
        }

        public void Decode(CfgData data)
        {
            this.DecodeFull(data);
            UpdateShader();
        }

        #endregion

        #region Inspector


        private int inspectedProperty = -1;
        public static PlaytimePainter_SceneLightingManager inspected;

        public void Inspect()
        {
            
            inspected = this;

            if (Application.isPlaying)
            {
                if (enabled && "Pause".Click())
                    enabled = false;

                if (!enabled && "Control Light".Click())
                    enabled = true;
            }

            pegi.nl();

            var changed = pegi.ChangeTrackStart();

            bool notInspectingProperty = inspectedProperty == -1;

            "Bleed".edit(60, ref colorBleed.targetValue, 0f, 0.3f).nl();

            "Brightness".edit(90, ref brightness.targetValue, 0f, 8f).nl();

            shadowDistance.enter_Inspect_AsList(ref inspectedProperty, 3).nl();

            bool fog = RenderSettings.fog;

            if (notInspectingProperty && "Fog".toggleIcon(ref fog, true))
                RenderSettings.fog = fog;

            if (fog)
            {
                var fogMode = RenderSettings.fogMode;

                if (notInspectingProperty)
                {
                    "Fog Color".edit(60, ref fogColor.targetValue).nl();

                    if ("Fog Mode".editEnum(60, ref fogMode).nl())
                        RenderSettings.fogMode = fogMode;
                }

                if (fogMode == FogMode.Linear)
                    fogDistance.enter_Inspect_AsList(ref inspectedProperty, 4).nl();
                else
                    fogDensity.enter_Inspect_AsList(ref inspectedProperty, 5).nl();
            }

            if (notInspectingProperty)
                "Sky Color".edit(60, ref skyColor.targetValue).nl();

            pegi.nl();

            "Main Directional Light".edit(ref _mainDirectionalLight).nl();

            if (_mainDirectionalLight)
            {
                pegi.nl();
                mainLightRotation.Nested_Inspect().nl();

                "Light Intensity".edit(ref mainLightIntensity.targetValue).nl();
                "Light Color".edit(ref mainLightColor.targetValue).nl();
            }

            pegi.nl();

            if (Application.isPlaying)
            {
                if (ld.MinPortion < 1)
                {
                    "Lerping {0}".F(ld.dominantParameter).write();
                    pegi.FullWindow.DocumentationClickOpen(() => "Each parameter has a transition speed. THis text shows which parameter sets speed for others (the slowest one). " +
                                                               "If Transition is too slow, increase this parameter's speed");
                    pegi.nl();
                }
            }

            if (changed)
            {
                Update();
#if UNITY_EDITOR
                if (Application.isPlaying == false)
                {
                    UnityEditor.SceneView.RepaintAll();
                    InternalEditorUtility.RepaintAllViews();
                }
#endif
            }

            if (!_configs)
                pegi.edit(ref _configs);
            else
                _configs.Nested_Inspect();

            if (changed)
                QcUnity.RepaintViews();

            if (changed)
                UpdateShader();

            inspected = null;
        }

        #endregion

        #region Lerping


        private readonly LerpData ld = new LerpData();


        private List<LinkedLerp.BaseLerp> lerpsList;


        public void Update()
        {
            ld.Update(this, canSkipLerp: false);
            UpdateShader();
        }

        public void Portion(LerpData ld)
        {

            if (lerpsList == null)
            {
                lerpsList = new List<LinkedLerp.BaseLerp>
                    {
                        shadowStrength,
                        shadowDistance,
                        fogColor,
                        skyColor,
                        fogDensity,
                        fogDistance,
                        colorBleed,
                        brightness
                    };
            }

            lerpsList.Portion(ld);

            if (_mainDirectionalLight)
            {
                mainLightIntensity.Portion(ld);
                mainLightColor.Portion(ld);
                mainLightRotation.CurrentValue = _mainDirectionalLight.transform.rotation;
                mainLightRotation.Portion(ld);
            }
        }

        public void Lerp(LerpData ld, bool canSkipLerp)
        {
            lerpsList.Lerp(ld);

            if (_mainDirectionalLight)
            {
                mainLightIntensity.Lerp(ld, canSkipLerp);
                mainLightColor.Lerp(ld, canSkipLerp);
                mainLightRotation.Lerp(ld, canSkipLerp);
            }

            RenderSettings.fogColor = fogColor.CurrentValue;

            if (RenderSettings.fog)
            {
                RenderSettings.fogEndDistance = fogDistance.CurrentValue;
                RenderSettings.fogDensity = fogDensity.CurrentValue;
            }

            RenderSettings.ambientSkyColor = skyColor.CurrentValue;
            QualitySettings.shadowDistance = shadowDistance.CurrentValue;

            if (_mainDirectionalLight)
            {
                _mainDirectionalLight.intensity = mainLightIntensity.CurrentValue;
                _mainDirectionalLight.color = mainLightColor.CurrentValue;
                _mainDirectionalLight.transform.rotation = mainLightRotation.CurrentValue;
            }



        }

        #endregion

    }



    [PEGI_Inspector_Override(typeof(PlaytimePainter_SceneLightingManager))]
internal class PlaytimePainter_SceneLightingManagerInspectorOverride : PEGI_Inspector_Override { }



}
