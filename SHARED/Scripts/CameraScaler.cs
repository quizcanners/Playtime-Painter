using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
public class CameraScaler : MonoBehaviour {

    public static CameraScaler inst;
    public Camera cam;
    public float orthogonalWidth = 7;
    public float orthogonalHeight = 7;
    public float projectionDistance = 100;
    public float areaToCapture = 10;
    private void Awake() {
        inst = this;
    }


    // Use this for initialization
    void Update () {
        if (cam == null)
            cam = GetComponent<Camera>();

        if (cam.orthographic) {

            float proportion = Screen.width / (float)Screen.height;
            float target = orthogonalWidth / orthogonalHeight;

            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, Mathf.Max(orthogonalHeight, orthogonalWidth * (target / proportion)), Time.deltaTime);
        } else {
            float area =  cam.targetTexture == null ?   (float)Mathf.Max(Screen.width, Screen.height) / (float)Screen.height : 1;

          //  var radAngle = cam.fieldOfView * Mathf.Deg2Rad;
          //  var radHFOV = 2 * Math.Atan(Mathf.Tan(radAngle / 2) * cam.aspect);
          //  var hFOV = Mathf.Rad2Deg * radHFOV;


            cam.fieldOfView = Vector2.Angle(new Vector2(0, projectionDistance), new Vector2(areaToCapture/area, projectionDistance));

        }

    }
	

}
