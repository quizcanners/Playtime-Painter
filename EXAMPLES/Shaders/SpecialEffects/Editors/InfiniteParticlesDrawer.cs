using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

public class InfiniteParticlesDrawerGUI : PEGI_Inspector_Material {

    public override bool Inspect(Material mat) {

        var changed = false;

        mat.toggle("SCREENSPACE").nl(ref changed);
        mat.toggle("DYNAMIC_SPEED").changes(ref changed);

        (mat.GetKeyword("DYNAMIC_SPEED") ? "Speed does nothing" : "Custom time does nothing").writeHint();
        
        pegi.DrawDefaultInspector();

        return changed;
    }
}

