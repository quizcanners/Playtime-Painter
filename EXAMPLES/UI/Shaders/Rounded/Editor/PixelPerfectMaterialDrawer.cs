using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using Playtime_Painter.Examples;


namespace Playtime_Painter.Examples {

public class PixelPerfectMaterialDrawer : PEGI_Inspector_Material {

        static readonly ShaderProperty.FloatValue softness = new ShaderProperty.FloatValue(RoundedGraphic.EDGE_SOFTNESS_FLOAT);

        static readonly ShaderProperty.TextureValue outline = new ShaderProperty.TextureValue("_OutlineGradient");

        #if PEGI
        public override bool Inspect(Material mat) {

            var changed = pegi.toggleDefaultInspector();

            mat.edit(softness, "Softness|", 0, 1).nl(ref changed);
            mat.edit(softness, "Softnessssssssssssss|", 0, 1).nl(ref changed);
            mat.edit(softness, "Softnessswqwewwwwwwwwwwwwwwwwwwwwwww|", 0, 1).nl(ref changed);

            mat.edit(outline).nl(ref changed);

            if (mat.IsKeywordEnabled(RoundedGraphic.UNLINKED_VERTICES))
                "UNLINKED VERTICES".nl();

            var go = UnityHelperFunctions.GetFocusedGameObject();

            if (go) {

                var rndd = go.GetComponent<RoundedGraphic>();

                if (!rndd)
                    "No RoundedGrahic.cs detected, shader needs custom data.".writeWarning();
                else if (!rndd.enabled)
                    "Controller is disabled".writeWarning();

            }

            return changed;
        }
        #endif

    }
}