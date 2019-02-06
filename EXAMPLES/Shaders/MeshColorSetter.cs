using System;
using UnityEngine;
using QuizCannersUtilities;

[ExecuteInEditMode]
public class MeshColorSetter : MonoBehaviour
{

    public MeshFilter filter;
    [NonSerialized] Mesh meshCopy;
    [SerializeField] Mesh originalMesh;

    private void OnEnable()
    {
        if (!filter)
            filter = GetComponent<MeshFilter>();
    }

    private void OnDisable()
    {
        if (filter && originalMesh)
        {
            filter.sharedMesh = originalMesh;
            this.SetToDirty();
        }
        meshCopy.DestroyWhatever_UObj();
    }

    float previousAlpha = -1;
    Color previousColor = Color.gray;
    [Range(0, 1)]
    public float colorAlpha = 0;

    public bool changeColor = false;
    public Color color = Color.white;



    // Update is called once per frame
    void LateUpdate()
    {

        if (filter && (colorAlpha != previousAlpha || (changeColor && color!= previousColor)))
        {
            if (!meshCopy)
            {
                if (!filter.sharedMesh)
                    filter.sharedMesh = originalMesh;
                else
                    originalMesh = filter.sharedMesh;

                if (originalMesh)
                {
                    meshCopy = Instantiate(originalMesh);
                    filter.mesh = meshCopy;
                }
            }

            if (meshCopy)
            {


                var verts = meshCopy.vertexCount;

                var cols = meshCopy.colors;

                if (cols.IsNullOrEmpty())  {
                    cols = new Color[verts];

                    for (int i = 0; i < verts; i++)
                        cols[i] = Color.white;


                }

                if (changeColor) {
                    color.a = colorAlpha;

                    for (int i = 0; i < verts; i++)
                        cols[i] = color;

                    previousColor = color;

                }
                 else for (int i = 0; i < verts; i++)
                    cols[i].a = colorAlpha;

                meshCopy.colors = cols;

                previousAlpha = colorAlpha;
            }
        }
    }
}
