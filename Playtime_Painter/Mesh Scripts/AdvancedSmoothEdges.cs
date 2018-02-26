using UnityEngine;
using System.Collections;

namespace Painter
{

    public class AdvancedSmoothEdges : VertexDataType  {

        public static AdvancedSmoothEdges inst;
        const int dataSize = 12;

        Vector4[] vertices;

        public override void GenerateIfNull() {
            //if (vertices == null) 
              //  vertices = MeshSolutions.curMeshDta.verts;

        }

        public override float[] getValue(int no) {
            
            for (int i = 0; i < MeshSolutions.vcnt; i++)
                MeshSolutions.chanelMedium[i] = vertices[i][no];

            return MeshSolutions.chanelMedium;
        }

        public override string ToString() {
            return "TRUE Smooth Edges";
        }

        public AdvancedSmoothEdges(int index) : base(dataSize, index) {
            inst = this;
        }

        public override void Clear() {
            vertices = null;
        }

    }
}