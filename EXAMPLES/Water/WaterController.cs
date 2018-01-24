using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(WaterController))]
public class WaterEditor : Editor
{

    public override void OnInspectorGUI()
    {
        ef.start(serializedObject);
        WaterController trg = (WaterController)target;

        bool modified = false;

        ef.write("Thickness:", 70);
        modified |= ef.edit(ref trg.thickness, 5, 300);
        ef.newLine();
        ef.write("Noise:", 50);
        modified |= ef.edit(ref trg.noise, 0, 100);
        ef.newLine();
        ef.write("Upscale:", 50);
        modified |= ef.edit(ref trg.upscale, 1, 64);
        ef.newLine();
        ef.write("Wet Area Height:", 50);
        modified |= ef.edit(ref trg.wetAreaHeight, 0.1f, 10);
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
#endif



[ExecuteInEditMode]
public class WaterController : MonoBehaviour {

//    public Texture2D foamTexture;

    private void OnEnable() {

        setFoamDynamics();

    }

    public Vector4 foamParameters;
    float MyTime = 0;
    public float thickness;
    public float noise;
    public float upscale;
    public float wetAreaHeight;
    public float eyeBrightness = 1;
    public float colorBleeding = 0.01f;
    public bool modifyBrightness = false;
    public bool colorBleed = false;

    public void setFoamDynamics()  {
        Shader.SetGlobalVector("_foamDynamics", new Vector4(thickness, noise, upscale, (300-thickness)));
        Shader.SetGlobalVector("_lightControl", new Vector4(colorBleeding, 0, 0, eyeBrightness));

        UnityHelperFunctions.SetKeyword("MODIFY_BRIGHTNESS", modifyBrightness);
        UnityHelperFunctions.SetKeyword("COLOR_BLEED", colorBleed);
     
    }


    // Update is called once per frame
    void Update () {

        if ((Application.isPlaying) || (gameObject.isFocused())) {
                MyTime += Time.deltaTime;
        }

        foamParameters.x = MyTime; 
        foamParameters.y = MyTime * 0.6f;
        foamParameters.z = transform.position.y;
        foamParameters.w = wetAreaHeight;

        Shader.SetGlobalVector("_foamParams", foamParameters);
      

	}
}
