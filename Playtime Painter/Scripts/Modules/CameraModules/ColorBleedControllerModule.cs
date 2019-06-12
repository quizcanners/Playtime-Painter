using System;
using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PlaytimePainter {

    [TaggedType(tag)]
    public class ColorBleedControllerModule : PainterSystemManagerModuleBase
    {

        const string tag = "Color Mgmt";

        public override string ClassTag => tag;

        [SerializeField] [HideInInspector] public List<WeatherConfig> weatherConfigurations = new List<WeatherConfig>();
        
        #region Encode & Decode

        public static CfgEncoder EncodeWeather()
        {
            var cody = new CfgEncoder()
                    .Add("sh", shadowStrength.targetValue)
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

        public static bool DecodeWeather(string tg, string data)
        {
            switch (tg)
            {
                case "sh": shadowStrength.targetValue = data.ToFloat(); break;
                case "sdst": shadowDistance.Decode(data); break;
                case "sc": skyColor.targetValue = data.ToColor(); break;
                case "fg": RenderSettings.fog = data.ToBool(); break;
                case "fogD": fogDistance.Decode(data); break;
                case "fogDen": fogDensity.Decode(data); break;
                case "fogCol": fogColor.targetValue = data.ToColor(); break;
                case "lr": mainLightRotation.Decode(data); break;
                case "lcol": mainLightColor.Decode(data); break;
                case "lint": mainLightIntensity.Decode(data); break;
                case "br": brightness.Decode(data); break;
                case "bl": colorBleed.Decode(data); break;
                default: return false;
            }

            return true;
        }
        
        public override CfgEncoder Encode()
        {
            var cody = this.EncodeUnrecognized();
          

            cody.Add_IfNotEmpty("cfgs", weatherConfigurations);

            return cody;
        }

        public override bool Decode(string tg, string data) {
            switch (tg) {
               
                case "cfgs": data.Decode_List(out weatherConfigurations); break;
                default: return false;
            }
            return true;
        }

        public override void Decode(string data)
        {
            base.Decode(data);

            UpdateShader();
        }

        #endregion

        #region Inspector
        public override string NameForDisplayPEGI => "Bleed & Brightness";


        #if !NO_PEGI

        public override string ToolTip =>
            "This is not a postprocess effect. Color Bleed and Brightness modifies Global Shader Parameter used by Custom shaders included with the asset.";            
        
        public override bool Inspect() {
            var changed = false;
            
            Inspect(ref weatherConfigurations).nl(ref changed);
            
            if (changed)  
                UnityUtils.RepaintViews();
             
            return changed;
        }

        #endif
        #endregion


        #region Lerping
        private readonly ShaderProperty.VectorValue _lightProperty = new ShaderProperty.VectorValue("pp_COLOR_BLEED");

        private void UpdateShader() => _lightProperty.GlobalValue = new Vector4(colorBleed.CurrentValue, 0, 0, brightness.CurrentValue);
        
        static readonly LinkedLerp.FloatValue brightness = new LinkedLerp.FloatValue("Brightness", 1, 1);
        static readonly LinkedLerp.FloatValue colorBleed = new LinkedLerp.FloatValue("Color Bleed", 0, 0.1f);

        static readonly LinkedLerp.ColorValue mainLightColor = new LinkedLerp.ColorValue("Light Color");
        static readonly LinkedLerp.FloatValue mainLightIntensity = new LinkedLerp.FloatValue("Main Light Intensity");
        static readonly LinkedLerp.QuaternionValue mainLightRotation = new LinkedLerp.QuaternionValue("Main light rotation");

        static readonly LinkedLerp.ColorValue fogColor = new LinkedLerp.ColorValue("Fog Color");
        static readonly LinkedLerp.ColorValue skyColor = new LinkedLerp.ColorValue("Sky Color");
        static readonly LinkedLerp.FloatValue shadowStrength = new LinkedLerp.FloatValue("Shadow Strength", 1);
        static readonly LinkedLerp.FloatValue shadowDistance = new LinkedLerp.FloatValue("Shadow Distance", 100, 500, 10, 1000);
        static readonly LinkedLerp.FloatValue fogDistance = new LinkedLerp.FloatValue("Fog Distance", 100, 500, 0.01f, 1000);
        static readonly LinkedLerp.FloatValue fogDensity = new LinkedLerp.FloatValue("Fog Density", 0.01f, 0.01f, 0.00001f, 0.1f);

        private LerpData ld = new LerpData();

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

            var mgmt = PainterCamera.Inst;
            if (mgmt && mgmt.mainDirectionalLight)
            {
                var l = mgmt.mainDirectionalLight;
                if (l)
                {
                    mainLightColor.TargetAndCurrentValue = l.color;
                    mainLightIntensity.TargetAndCurrentValue = l.intensity;
                    mainLightRotation.TargetAndCurrentValue = l.transform.rotation;
                }
            }


        }

        private List<LinkedLerp.BaseLerp> lerpsList;
        
        #endregion

        #region Inspector
        #if !NO_PEGI
        private int inspectedProperty = -1;

        public bool Inspect(ref List<WeatherConfig> configurations)
        {

            bool changed = false;

            bool notInspectingProperty = inspectedProperty == -1;

            "Bleed".edit(60, ref colorBleed.targetValue, 0f, 0.3f).nl(ref changed);

            "Brightness".edit(90, ref brightness.targetValue, 0.0001f, 8f).nl(ref changed);

            shadowDistance.enter_Inspect_AsList(ref inspectedProperty, 3).nl(ref changed);

            bool fog = RenderSettings.fog;

            if (notInspectingProperty && "Fog".toggleIcon(ref fog, true).changes(ref changed))
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
                    fogDistance.enter_Inspect_AsList(ref inspectedProperty, 4).nl(ref changed);
                else
                    fogDensity.enter_Inspect_AsList(ref inspectedProperty, 5).nl(ref changed);
            }

            if (notInspectingProperty)
                "Sky Color".edit(60, ref skyColor.targetValue).nl(ref changed);

            pegi.nl();


            var mgmt = PainterCamera.Inst;
            if (mgmt)
            {
                "Main Directional Light".edit(ref mgmt.mainDirectionalLight).nl(ref changed);

                var l = mgmt.mainDirectionalLight;

                if (l)
                {

                    pegi.nl();
                    mainLightRotation.Nested_Inspect().nl(ref changed);

                    "Light Intensity".edit(ref mainLightIntensity.targetValue).nl(ref changed);
                    "Light Color".edit(ref mainLightColor.targetValue).nl(ref changed);
                }

            }

            var newObj = "Configurations".edit_List(ref configurations, ref changed);

            pegi.nl();

            if (newObj != null)
            {
                ReadCurrentValues();
                newObj.data = Encode().ToString();
                newObj.ActiveConfiguration = newObj;
            }

            if (Application.isPlaying)
            {
                if (ld.MinPortion < 1)
                {
                    "Lerping {0}".F(ld.dominantParameter).write();
                    ("Each parameter has a transition speed. THis text shows which parameter sets speed for others (the slowest one). " +
                     "If Transition is too slow, increase this parameter's speed").fullWindowDocumentationClickOpen();
                    pegi.nl();
                }
            }

            if (changed)
            {
                Update();
#if UNITY_EDITOR
                if (Application.isPlaying == false)
                {
                    SceneView.RepaintAll();
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                }
#endif
            }

            if (changed)
                UpdateShader();

            return changed;
        }

        #endif
        #endregion
        
        public override void Update()
        {
            base.Update();
            
            if (WeatherConfig.activeConfig != null)
            {
                ld.Reset();

                Light l = PainterCamera.Inst ? PainterCamera.Inst.mainDirectionalLight : null;

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

                if (l)
                {
                    mainLightIntensity.Portion(ld);
                    mainLightColor.Portion(ld);
                    mainLightRotation.CurrentValue = l.transform.rotation;
                    mainLightRotation.Portion(ld);
                }

                lerpsList.Lerp(ld);


                if (l)
                {
                    mainLightIntensity.Lerp(ld);
                    mainLightColor.Lerp(ld);
                    mainLightRotation.Lerp(ld);
                }

                RenderSettings.fogColor = fogColor.CurrentValue;

                if (RenderSettings.fog)
                {
                    RenderSettings.fogEndDistance = fogDistance.CurrentValue;
                    RenderSettings.fogDensity = fogDensity.CurrentValue;
                }

                RenderSettings.ambientSkyColor = skyColor.CurrentValue;
                QualitySettings.shadowDistance = shadowDistance.CurrentValue;

                if (l)
                {
                    l.intensity = mainLightIntensity.CurrentValue;
                    l.color = mainLightColor.CurrentValue;
                    l.transform.rotation = mainLightRotation.CurrentValue;
                }

                UpdateShader();

            }
            
        }
        
        public class WeatherConfig : Configuration {
            public static Configuration activeConfig;

            public override Configuration ActiveConfiguration
            {
                get { return activeConfig; }
                set
                {
                    activeConfig = value;
                    new CfgDecoder(data).DecodeTagsFor(DecodeWeather);
                }
            }

            public override void ReadConfigurationToData() => data = EncodeWeather().ToString();
        }
    }
}