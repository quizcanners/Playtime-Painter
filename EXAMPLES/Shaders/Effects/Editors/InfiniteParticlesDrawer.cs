using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

public class InfiniteParticlesDrawerGUI : PEGI_Inspector_Material {

#if PEGI
    public override bool Inspect(Material mat) {

        var changed = pegi.toggleDefaultInspector();

        mat.toggle("SCREENSPACE").nl(ref changed);
        mat.toggle("DYNAMIC_SPEED").changes(ref changed);

        (mat.GetKeyword("DYNAMIC_SPEED") ? "Speed does nothing" : "Custom time does nothing").writeHint();
        
        return changed;
    }
#endif
}

