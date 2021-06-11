using QuizCanners.Inspect;
using UnityEngine;
using QuizCanners.CfgDecode;

namespace PlaytimePainter.Modules
{

    [CreateAssetMenu(fileName = FILE_NAME, menuName = "Playtime Painter/" + FILE_NAME)]
    public class PlaytimePainter_SceneLighting_Configurations : ConfigurationsSO_Generic<WeatherConfig>, IPEGI
    {
        public const string FILE_NAME = "Scene Lighting Config";


    }

    public class WeatherConfig : Configuration
    {
        public static Configuration activeConfig;

        public override Configuration ActiveConfiguration
        {
            get { return activeConfig; }
            set
            {
                activeConfig = value;
                PlaytimePainter_SceneLightingManager.inspected.Decode(value.data);
            }
        }

        public override CfgEncoder EncodeData() => PlaytimePainter_SceneLightingManager.inspected.Encode();

    }
}