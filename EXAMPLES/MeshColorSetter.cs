using System;
using UnityEngine;
using QuizCannersUtilities;

namespace Playtime_Painter.Examples
{

    [ExecuteInEditMode]
    public class MeshColorSetter : MonoBehaviour
    {

        public MeshFilter filter;
        [NonSerialized] private Mesh _meshCopy;
        [SerializeField] private Mesh originalMesh;

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
            _meshCopy.DestroyWhatever_UObj();
        }

        private float _previousAlpha = -1;
        private Color _previousColor = Color.gray;
        [Range(0, 1)]
        public float colorAlpha;

        public bool changeColor;
        public Color color = Color.white;



        // Update is called once per frame
        private void LateUpdate()
        {
            if (!filter || (colorAlpha == _previousAlpha && (!changeColor || color == _previousColor))) return;
            
            if (!_meshCopy)
            {
                if (!filter.sharedMesh)
                    filter.sharedMesh = originalMesh;
                else
                    originalMesh = filter.sharedMesh;

                if (originalMesh)
                {
                    _meshCopy = Instantiate(originalMesh);
                    filter.mesh = _meshCopy;
                }
            }

            if (!_meshCopy) return;
            
            var verticesCount = _meshCopy.vertexCount;

            var cols = _meshCopy.colors;

            if (cols.IsNullOrEmpty())
            {
                cols = new Color[verticesCount];

                for (var i = 0; i < verticesCount; i++)
                    cols[i] = Color.white;


            }

            if (changeColor)
            {
                color.a = colorAlpha;

                for (var i = 0; i < verticesCount; i++)
                    cols[i] = color;

                _previousColor = color;

            }
            else for (var i = 0; i < verticesCount; i++)
                cols[i].a = colorAlpha;

            _meshCopy.colors = cols;

            _previousAlpha = colorAlpha;
        }
    }
}