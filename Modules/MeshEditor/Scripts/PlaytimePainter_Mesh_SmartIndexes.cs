using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PainterTool.MeshEditing
{

    public struct VertexIndexInTriangle 
    {
        public int index;

        public static VertexIndexInTriangle From(int index) 
        {
            var tmp = new VertexIndexInTriangle();
            tmp.index = index;
            return tmp;
        }
    }

    public struct MeshPointIndex
    {
        public int index;

        public static MeshPointIndex From(int index)
        {
            var tmp = new MeshPointIndex();
            tmp.index = index;
            return tmp;
        }
    }

    public struct VertexUVsDataIndex
    {
        public int index;

        public static VertexUVsDataIndex From(int index)
        {
            var tmp = new VertexUVsDataIndex();
            tmp.index = index;
            return tmp;
        }
    }

    public struct UvSetIndex
    {
        public int index;

        public static UvSetIndex From(int index)
        {
            var tmp = new UvSetIndex();
            tmp.index = index;
            return tmp;
        }
    }

    public struct LineInTriangleIndex
    {
        public int index;

        public static LineInTriangleIndex From(int index)
        {
            var tmp = new LineInTriangleIndex();
            tmp.index = index;
            return tmp;
        }
    }
}
