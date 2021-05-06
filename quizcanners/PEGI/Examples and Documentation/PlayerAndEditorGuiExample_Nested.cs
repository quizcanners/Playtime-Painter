using System;
using UnityEngine;

namespace QuizCanners.Inspect.Examples
{
    public class InspectExample_Nested : MonoBehaviour, IPEGI
    {

        [NonSerialized] private Light lightSource;

        [SerializeField] private Color SunColor;
        [SerializeField] private Color MoonColor;
        private bool isDay = true;


        private int _nestedExamples;

        public void Inspect()
        {
            var changed = pegi.ChangeTrackStart();

            if ("Light".IsEntered(ref _nestedExamples, 0).nl())
            {
                "Sun".write(toolTip: "Color of sunlight", width: 50);

                pegi.edit(ref SunColor);                 // In some cases we don't care if something have changed, but often it is useful.

                if (!isDay && "Set Day".Click())
                    isDay = true;
                    
                

                pegi.nl();

                // Shorter version of the above:
                "Moon".edit(toolTip: "Color of the Moon", width: 50, ref MoonColor);

                if (isDay && "Set Night".Click())
                    isDay = false;
                

                pegi.nl();

                "Light Source".edit(ref lightSource).nl();

                if (lightSource)
                {
                    var myLight = lightSource.color;
                    if ("Current:".edit(ref myLight).nl())
                    {
                        lightSource.color = myLight;

                    }
                }

                if (changed && lightSource)
                {
                    lightSource.color = isDay ? SunColor : MoonColor;
                }


                "FOG".nl(PEGI_Styles.ListLabel);

                var fogColor = RenderSettings.fogColor;

                if ("Fog".edit(ref fogColor).nl())
                {
                    RenderSettings.fogColor = fogColor;
                }

            }    

        }
    }
}