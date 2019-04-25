using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Playtime_Painter {

    [TaggedType(tag)]
    public class ColorBleedControllerPlugin : PainterSystemManagerPluginBase  {

        const string tag = "Color Mgmt";
        public override string ClassTag => tag;

        private float _eyeBrightness = 1f;
        private float _colorBleeding;
        private bool _modifyBrightness;
        private bool _colorBleed;

        [SerializeField] [HideInInspector] public List<WeatherConfig> weatherConfigurations = new List<WeatherConfig>();
        
        private static WeatherManagement weatherManager = new WeatherManagement();

        private readonly ShaderProperty.VectorValue _lightProperty = new ShaderProperty.VectorValue("_lightControl");

        private void UpdateShader() => _lightProperty.GlobalValue = new Vector4(_colorBleeding, 0, 0, _eyeBrightness);
        
        #region Encode & Decode

        public override CfgEncoder Encode()
        {
            var cody = this.EncodeUnrecognized();
            if (_modifyBrightness)
                cody.Add("br", _eyeBrightness);
            if (_colorBleed)
                cody.Add("bl", _colorBleeding);

            cody.Add_IfNotEmpty("cfgs", weatherConfigurations);

            return cody;
        }

        public override bool Decode(string tg, string data) {
            switch (tg) {
                case "br": _modifyBrightness = true; _eyeBrightness = data.ToFloat(); break;
                case "bl": _colorBleed = true; _colorBleeding = data.ToFloat(); break;
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


        #if PEGI

        public override string ToolTip =>
            "This is not a postprocess effect. Color Bleed and Brightness modifies Global Shader Parameter used by Custom shaders included with the asset.";            
        
        public override bool Inspect() {
            var changed = false;

            "Color Bleed".toggleIcon(ref _colorBleed, true).changes(ref changed);

            if (_colorBleed)
                "Bleed".edit(60, ref _colorBleeding, 0.0001f, 0.3f).nl(ref changed);
            else
                _colorBleeding = 0;
            
            pegi.nl();

            "Brightness".toggleIcon(ref _modifyBrightness, true).changes(ref changed);

            if (_modifyBrightness)
                "Brightness".edit(90, ref _eyeBrightness, 0.0001f, 8f).changes(ref changed);
            else
                _eyeBrightness = 1;

            pegi.nl();
            


            weatherManager.Inspect(ref weatherConfigurations).nl(ref changed);
            
            if (changed)  {
                UnityUtils.RepaintViews();
                UpdateShader();
                this.SetToDirty_Obj();
            }


            return changed;
        }

        #endif
        #endregion
        
        public override void Update()
        {
            base.Update();

            weatherManager.Update();

        }

        #region Weather Management
        
        public class WeatherConfig : Configuration
        {

            public static Configuration activeWeatherConfig;

            public override Configuration ActiveConfiguration
            {
                get { return activeWeatherConfig; }
                set
                {
                    activeWeatherConfig = value;
                    weatherManager.Decode(data);
                }
            }

            public override void ReadConfigurationToData() => data = weatherManager.Encode().ToString();
        }

        public class WeatherManagement : ICfg
        {

            #region Lerping

           // private bool affectLightRotation;

            LinkedLerp.ColorValue mainLightColor = new LinkedLerp.ColorValue("Light Color");
            LinkedLerp.FloatValue mainLightIntensity = new LinkedLerp.FloatValue("Main Light Intensity");
            LinkedLerp.QuaternionValue mainLightRotation = new LinkedLerp.QuaternionValue("Main light rotation");

            LinkedLerp.ColorValue fogColor = new LinkedLerp.ColorValue("Fog Color");
            LinkedLerp.ColorValue skyColor = new LinkedLerp.ColorValue("Sky Color");
            LinkedLerp.FloatValue shadowStrength = new LinkedLerp.FloatValue("Shadow Strength", 1);
            LinkedLerp.FloatValue shadowDistance = new LinkedLerp.FloatValue("Shadow Distance", 100, 500, 10, 1000);
            LinkedLerp.FloatValue fogDistance = new LinkedLerp.FloatValue("Fog Distance", 100, 500, 0.01f, 1000);
            LinkedLerp.FloatValue fogDensity = new LinkedLerp.FloatValue("Fog Density", 0.01f, 0.01f, 0.00001f, 0.1f);

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

            public void Update()
            {
                if (WeatherConfig.activeWeatherConfig != null)
                {
                    ld.Reset();

                    Light l = PainterCamera.Inst ? PainterCamera.Inst.mainDirectionalLight : null;

                    // Find slowest property
                    shadowStrength.Portion(ld);
                    shadowDistance.Portion(ld);
                    fogColor.Portion(ld);
                    skyColor.Portion(ld);
                    fogDensity.Portion(ld);
                    fogDistance.Portion(ld);
                    if (l)
                    {
                        mainLightIntensity.Portion(ld);
                        mainLightColor.Portion(ld);
                      
                        mainLightRotation.CurrentValue = l.transform.rotation;
                        mainLightRotation.Portion(ld);

                        
                    }

                    // Lerp all the properties
                    shadowStrength.Lerp(ld);
                    shadowDistance.Lerp(ld);
                    fogColor.Lerp(ld);
                    skyColor.Lerp(ld);
                    fogDensity.Lerp(ld);
                    fogDistance.Lerp(ld);

                    if (l)
                    {
                        mainLightIntensity.Lerp(ld);
                        mainLightColor.Lerp(ld);
                       //if (affectLightRotation)
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

                    if (l) {
                        l.intensity = mainLightIntensity.CurrentValue;
                        l.color = mainLightColor.CurrentValue;

                        //if (affectLightRotation)
                            l.transform.rotation = mainLightRotation.CurrentValue;
                    }

                }

            }
            #endregion

            #region Inspector
#if PEGI
            private int inspectedProperty = -1;

            public bool Inspect(ref List<WeatherConfig> configurations)
            {

                bool changed = false;

                bool notInspectingProperty = inspectedProperty == -1;

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
                if (mgmt) {
                    "Main Directional Light".edit(ref mgmt.mainDirectionalLight).nl(ref changed);

                    var l = mgmt.mainDirectionalLight;

                    if (l)
                    {
                     // "Rotation".toggleIcon(ref affectLightRotation).changes(ref changed);

                      //  if (icon.Save.Click("Use current value"))
                         //   mainLightRotation.targetValue = l.transform.rotation;

                      //  if (affectLightRotation)
                      //  {
                            pegi.nl();
                            mainLightRotation.Nested_Inspect().nl(ref changed); // targetValue).nl(ref changed);

                      //  }

                        pegi.nl();

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
                    if (ld.linkedPortion < 1)
                    {
                        "Lerping {0}".F(ld.dominantParameter).write();
                        ("Each parameter has a transition speed. THis text shows which parameter sets speed for others (the slowest one). " +
                         "If Transition is too slow, increase this parameter's speed").fullWindowDocumentationClick();
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

                return changed;
            }

#endif
            #endregion

            #region Encode & Decode

            // Encode and Decode class lets you store configuration of this class in a string 

            public CfgEncoder Encode()
            {
                var cody = new CfgEncoder()
                    .Add("sh", shadowStrength.targetValue)
                    .Add("sdst", shadowDistance)
                    .Add("sc", skyColor.targetValue)
                    .Add_Bool("fg", RenderSettings.fog)
                    .Add("lcol", mainLightColor)
                    .Add("lint", mainLightIntensity)
                    //.Add_Bool("rot", affectLightRotation)
                    ;

               // if (affectLightRotation)
                    cody.Add("lr", mainLightRotation);

                if (RenderSettings.fog)
                    cody.Add("fogCol", fogColor.targetValue)
                        .Add("fogD", fogDistance)
                        .Add("fogDen", fogDensity);

                return cody;
            }

            public void Decode(string data) => new CfgDecoder(data).DecodeTagsFor(this);

            public bool Decode(string tg, string data)
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
                   // case "rot": affectLightRotation = data.ToBool(); break;
                    default: return false;
                }

                return true;
            }

            #endregion

        }
        
        #endregion

    }






}