using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using Playtime_Painter.Examples;


namespace Playtime_Painter.Examples {

public class PixelPerfectMaterialDrawer : PEGI_Inspector_Material {
    
    private static readonly ShaderProperty.FloatValue Softness = new ShaderProperty.FloatValue(RoundedGraphic.EDGE_SOFTNESS_FLOAT);

    private static readonly ShaderProperty.TextureValue Outline = new ShaderProperty.TextureValue("_OutlineGradient");

        #if PEGI
        public override bool Inspect(Material mat) {

            var changed = pegi.toggleDefaultInspector();

            mat.edit(Softness, "Softness", 0, 1).nl(ref changed);
       
            mat.edit(Outline).nl(ref changed);

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