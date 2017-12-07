using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class skyController : MonoBehaviour {

	// Use this for initialization
	void Start () {
       
	}

    public Light directional;
    public MeshRenderer _rendy;

    void findComponents() {
        if (_rendy == null)
            _rendy = GetComponent<MeshRenderer>();

        if (directional == null) {
            Light[] ls = FindObjectsOfType<Light>();
            for (int i = 0; i < ls.Length; i++)
                if (ls[i].type == LightType.Directional) {
                    directional = ls[i];
                    i = ls.Length;
                        }
                }


    }

    private void OnEnable() {
        findComponents();
        _rendy.enabled = Application.isPlaying;
    
    }

    public float skyDynamics = 0.1f;

    void Update () {

        // Got some clipping when using _WorldSpaceLightPos0 
        if (directional != null) {

            Vector3 v3 = directional.transform.rotation * Vector3.back;
            Shader.SetGlobalVector("_SunDirection", new Vector4(v3.x,v3.y,v3.z));
            Shader.SetGlobalColor("_Directional", directional.color);
        }
        Camera c = Camera.main;
        if (c != null)
        {
            Vector3 pos = c.transform.position* skyDynamics;
            Shader.SetGlobalVector("_Off", new Vector4(pos.x, pos.z, 0f, 0f));
        }
    }

    void LateUpdate()
    {
        transform.rotation = Quaternion.identity;
    }

}
