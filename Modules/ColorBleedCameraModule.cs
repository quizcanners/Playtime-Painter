﻿using System;
using System.Collections.Generic;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace PlaytimePainter.CameraModules {

    [TaggedType(tag)]
    public class ColorBleedCameraModule : CameraModuleBase, IPEGI, ICfgCustom
    {

        public const string tag = "Color Mgmt";

        public override string ClassTag => tag;

        [SerializeField] [HideInInspector] public List<WeatherConfig> weatherConfigurations = new List<WeatherConfig>();
        
        #region Encode & Decode

        public static CfgEncoder EncodeWeather()
        {
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

        public static void DecodeWeather(string tg, CfgData data)
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
        
        public override CfgEncoder Encode()
        {
            var cody = base.Encode(); //this.EncodeUnrecognized();
          

            cody.Add_IfNotEmpty("cfgs", weatherConfigurations);

            return cody;
        }

        public override void Decode(string key, CfgData data) {
            switch (key) {
               
                case "cfgs": data.ToList(out weatherConfigurations); break;

            }
        }

        public void Decode(CfgData data)
        {
            this.DecodeTagsFrom(data);

            UpdateShader();
        }

        #endregion

        #region Inspector
        public override string NameForDisplayPEGI()=> "Bleed & Brightness";

        public override string ToolTip =>
            "This is not a postprocess effect. Color Bleed and Brightness modifies Global Shader Parameter used by Custom shaders included with the asset.";            
        
        public void Inspect() {

            if (Inspect(ref weatherConfigurations).nl())  
                QcUnity.RepaintViews();

        }
        
        #endregion


        #region Lerping
        private readonly ShaderProperty.VectorValue _lightProperty = new ShaderProperty.VectorValue("pp_COLOR_BLEED");

        private void UpdateShader() => _lightProperty.GlobalValue = new Vector4(colorBleed.CurrentValue, 0, 0, brightness.CurrentValue);
        
        static readonly LinkedLerp.FloatValue brightness = new LinkedLerp.FloatValue( 1, 1, "Brightness");
        static readonly LinkedLerp.FloatValue colorBleed = new LinkedLerp.FloatValue( 0, 0.1f, "Color Bleed");

        static readonly LinkedLerp.ColorValue mainLightColor = new LinkedLerp.ColorValue("Light Color");
        static readonly LinkedLerp.FloatValue mainLightIntensity = new LinkedLerp.FloatValue(name: "Main Light Intensity");
        static readonly LinkedLerp.QuaternionValue mainLightRotation = new LinkedLerp.QuaternionValue("Main light rotation");

        static readonly LinkedLerp.ColorValue fogColor = new LinkedLerp.ColorValue("Fog Color");
        static readonly LinkedLerp.ColorValue skyColor = new LinkedLerp.ColorValue("Sky Color");
        static readonly LinkedLerp.FloatValue shadowStrength = new LinkedLerp.FloatValue( 1, name: "Shadow Strength");
        static readonly LinkedLerp.FloatValue shadowDistance = new LinkedLerp.FloatValue(100, 500, 10, 1000, "Shadow Distance");
        static readonly LinkedLerp.FloatValue fogDistance = new LinkedLerp.FloatValue(100, 500, 0.01f, 1000, "Fog Distance");
        static readonly LinkedLerp.FloatValue fogDensity = new LinkedLerp.FloatValue(0.01f, 0.01f, 0.00001f, 0.1f, "Fog Density");

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
        private int inspectedProperty = -1;

        public bool Inspect(ref List<WeatherConfig> configurations)
        {

            bool changed = false;

            bool notInspectingProperty = inspectedProperty == -1;

            "Bleed".edit(60, ref colorBleed.targetValue, 0f, 0.3f).nl(ref changed);

            "Brightness".edit(90, ref brightness.targetValue, 0f, 8f).nl(ref changed);

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
                    pegi.FullWindow.DocumentationClickOpen(()=> "Each parameter has a transition speed. THis text shows which parameter sets speed for others (the slowest one). " +
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
                    SceneView.RepaintAll();
                    InternalEditorUtility.RepaintAllViews();
                }
#endif
            }

            if (changed)
                UpdateShader();

            return changed;
        }
        
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

                ld.LerpAndReset();
                
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

            public override CfgEncoder EncodeData() => EncodeWeather();
            
        }
    }
}