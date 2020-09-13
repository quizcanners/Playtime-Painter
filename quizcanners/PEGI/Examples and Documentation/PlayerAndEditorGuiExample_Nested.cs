using System;
using PlayerAndEditorGUI;
using UnityEngine;

namespace PlayerAndEditorGUI.Examples
{
    public class PlayerAndEditorGuiExample_Nested : MonoBehaviour, IPEGI
    {

        [NonSerialized] private Light lightSource;

        [SerializeField] private Color SunColor;
        [SerializeField] private Color MoonColor;
        private bool isDay = true;


        private int _nestedExamples;

        public bool Inspect()
        {
            var changed = false;

            if ("Light".enter(ref _nestedExamples, 0).nl())
            {
                "Sun".write(toolTip: "Color of sunlight", width: 50);

                changed |= pegi.edit(ref SunColor);                 // In some cases we don't care if something have changed, but often it is useful.

                if (!isDay)
                {
                    if ("Set Day".Click())
                    {
                        isDay = true;
                        changed = true;
                    }
                }

                pegi.nl();

                // Shorter version of the above:
                "Moon".edit(toolTip: "Color of the Moon", width: 50, ref MoonColor).changes(ref changed);

                if (isDay && "Set Night".Click(ref changed))
                {
                    isDay = false;
                }

                pegi.nl();

                "Light Source".edit(ref lightSource).nl(ref changed);

                if (lightSource)
                {
                    var light = lightSource.color;
                    if ("Current:".edit(ref light).nl())
                    {
                        lightSource.color = light;

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




            return changed;

        }
    }
}