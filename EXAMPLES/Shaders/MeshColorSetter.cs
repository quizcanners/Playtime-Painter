using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QuizCannersUtilities;

[ExecuteInEditMode]
public class MeshColorSetter : MonoBehaviour {

    public MeshFilter filter;
    [NonSerialized] public Mesh meshCopy;

    private void OnEnable()
    {
        if (!filter)
            filter = GetComponent<MeshFilter>();
    }

    float previousAlpha = -1;
    [Range(0,1)]
    public float colorAlpha = 0;

	// Update is called once per frame
	void LateUpdate () {

        if (Application.isPlaying)
        {

            if (colorAlpha != previousAlpha && filter)
            {
                if (!meshCopy)
                {
                    meshCopy = Instantiate(filter.mesh);
                    filter.mesh = meshCopy;
                }
                var verts = meshCopy.vertexCount;

                var cols = meshCopy.colors;

                if (cols.IsNullOrEmpty())
                {
                    cols = new Color[verts];

                    for (int i = 0; i < verts; i++)
                        cols[i] = Color.white;

                 
                }

                for (int i = 0; i < verts; i++)
                    cols[i].a = colorAlpha;

                meshCopy.colors = cols;

                previousAlpha = colorAlpha;
            }
        }
	}
}
