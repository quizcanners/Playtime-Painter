using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class WaterController : MonoBehaviour {

//    public Texture2D foamTexture;

    private void OnEnable() {

        setFoamDynamics();

    }

    public Vector4 foamParameters;
    float MyTime = 0;
    public float _thickness = 80;
    public float _noise;
    public float _upscale = 1;
    public float _wetAreaHeight = 2;
    public float eyeBrightness = 1;
    public float colorBleeding = 0.01f;
    public bool modifyBrightness = false;
    public bool colorBleed = false;

    public void setFoamDynamics()  {
        Shader.SetGlobalVector("_foamDynamics", new Vector4(_thickness, _noise, _upscale, (300-_thickness)));
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
        foamParameters.w = _wetAreaHeight;

        Shader.SetGlobalVector("_foamParams", foamParameters);
      

	}
}
