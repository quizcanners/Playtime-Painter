using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(WaterController))]
public class WaterEditor : Editor
{

    public override void OnInspectorGUI() {
        ef.start(serializedObject);
        WaterController trg = (WaterController)target;

        bool modified = false;

        ef.write("Thickness:", 70);
        modified |= ef.edit(ref trg._thickness, 5, 300);
        ef.newLine();
        ef.write("Noise:",50);
        modified |= ef.edit(ref trg._noise, 0, 100);
        ef.newLine();
        ef.write("Upscale:",50);
        modified |= ef.edit(ref trg._upscale, 1, 64);
        ef.newLine();
        ef.write("Wet Area Height:", 50);
        modified |= ef.edit(ref trg._wetAreaHeight, 0.1f, 10);
        ef.newLine();
        ef.write("Bleed Colors", 80);
        modified |= ef.toggle(ref trg.colorBleed);
        ef.newLine();
    
        if (trg.colorBleed)
            modified |= ef.edit(ref trg.colorBleeding, 0.0001f, 0.1f);
        ef.newLine();
        ef.write("Brightness", 80);
        modified |= ef.toggle(ref trg.modifyBrightness);
        ef.newLine();
        if (trg.modifyBrightness)
            modified |= ef.edit(ref trg.eyeBrightness, 0.0001f, 8f);
        ef.newLine();

        if (modified) { trg.setFoamDynamics(); SceneView.RepaintAll(); UnityEditorInternal.InternalEditorUtility.RepaintAllViews(); }
        ef.newLine();

    }
}