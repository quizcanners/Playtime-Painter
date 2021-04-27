using System.Collections.Generic;
using QuizCanners.Migration;
using UnityEngine; 

namespace PainterTool.MeshEditing
{
    public partial class PainterMesh 
    {
        public class LineData : PainterClass
        {
            private readonly Triangle triangle;
            public readonly Vertex[] vertexes = new Vertex[2];
            public int trianglesCount_Cahced;

            public MeshPoint this[int index] => vertexes[index].meshPoint;

            public float LocalLength => (this[0].localPos - this[1].localPos).magnitude;

            public float WorldSpaceLength => (this[0].WorldPos - this[1].WorldPos).magnitude;

            public bool SameAsLastFrame => Equals(EditedMesh.lastFramePointedLine);

            public bool IsASeam() 
            {
                var t = GetOtherTriangle();

                if (t == null)
                    return false;

                return (!t.ContainsUv(vertexes[0]) && !t.ContainsUv(vertexes[1]));
                   

               /* foreach (var t in tris)
                {
                    if (t == triangle)
                        continue;

                    if (!t.Contains(vertexes[0]) || !t.Contains(vertexes[1]))
                        return true;
                }*/

            }

            public bool Includes(Vertex vertex) => ((vertex == vertexes[0]) || (vertex == vertexes[1]));

            public bool Includes(MeshPoint point) => ((point == vertexes[0].meshPoint) || (point == vertexes[1].meshPoint));

            public bool SameVertices(LineData other) => (((other.vertexes[0].meshPoint == vertexes[0].meshPoint) &&
                                                          (other.vertexes[1].meshPoint == vertexes[1].meshPoint)) ||
                                                         ((other.vertexes[0].meshPoint == vertexes[1].meshPoint) &&
                                                          (other.vertexes[1].meshPoint == vertexes[0].meshPoint)));

            public LineData(MeshPoint a, MeshPoint b)
            {
                triangle = a.GetTriangleFromLine(b);
                vertexes[0] = a.vertices[0];
                vertexes[1] = b.vertices[0];
                trianglesCount_Cahced = 0;
            }

            public LineData(Triangle tri, Vertex[] nPoints)
            {
                triangle = tri;
                vertexes[0] = nPoints[0];
                vertexes[1] = nPoints[1];
                trianglesCount_Cahced = 0;
            }

            public LineData(Triangle tri, Vertex a, Vertex b)
            {
                triangle = tri;
                vertexes[0] = a;
                vertexes[1] = b;

                trianglesCount_Cahced = 0;
            }

            public Triangle GetOtherTriangle()
            {
                foreach (Vertex vert in vertexes)
                foreach (Vertex uv in vert.meshPoint.vertices)
                foreach (Triangle tri in uv.triangles)
                    if (tri != triangle && tri.Includes(vertexes[0].meshPoint) && tri.Includes(vertexes[1].meshPoint))
                        return tri;


                return null;

            }

            public List<Triangle> TryGetBothTriangles()
            {
                var allTriangles = new List<Triangle>();

                foreach (Vertex uv0 in vertexes)

                foreach (Vertex uv in uv0.meshPoint.vertices)

                foreach (Triangle tri in uv.triangles)

                    if (!allTriangles.Contains(tri) && tri.Includes(vertexes[0].meshPoint) &&
                        tri.Includes(vertexes[1].meshPoint))
                        allTriangles.Add(tri);

                return allTriangles;
            }

            public Vector3 Vector => vertexes[1].LocalPos - vertexes[0].LocalPos;

            public Vector3 HalfVectorToB(LineData other)
            {
                var lineA = this;
                var lineB = other;

                if (other.vertexes[1] == vertexes[0])
                {
                    lineA = other;
                    lineB = this;
                }

                var a = lineA.vertexes[0].LocalPos - lineA.vertexes[1].LocalPos;
                var b = lineB.vertexes[1].LocalPos - lineB.vertexes[0].LocalPos;

                var grid = MeshPainting.Grid;

                var fromVector2 = grid.InPlaneVector(a);
                var toVector2 = grid.InPlaneVector(b);

                var mid = (fromVector2.normalized + toVector2.normalized).normalized;

                var cross = Vector3.Cross(fromVector2, toVector2);

                if (cross.z > 0)
                    mid = -mid;

                return grid.PlaneToWorldVector(mid).normalized;
            }

            public override bool Equals(object obj)
            {
                if (obj == null || obj.GetType() != typeof(LineData))
                    return false;

                var ld = (LineData) obj;

                return (ld.vertexes[0].meshPoint == vertexes[0].meshPoint && ld.vertexes[1].meshPoint == vertexes[1].meshPoint)
                       || (ld.vertexes[0].meshPoint == vertexes[1].meshPoint &&
                           ld.vertexes[1].meshPoint == vertexes[0].meshPoint);

            }

            public override int GetHashCode() => vertexes[0].finalIndex * 1000000000 + vertexes[1].finalIndex;

            public bool GiveUniqueVerticesToTriangles()
            {
                var changed = false;
                var triangles = TryGetBothTriangles();

                for (var i = 0; i < triangles.Count; i++)
                for (var j = i + 1; j < triangles.Count; j++)
                    changed |= triangles[i].GiveUniqueVerticesAgainst(triangles[j]);

                return changed;
            }

        }

        public class BlendFrame : PainterClassCfg
        {
            public Vector3 deltaPosition;
            public Vector3 deltaTangent;
            public Vector3 deltaNormal;

            #region Encode & Decode

            public override void DecodeTag(string key, CfgData data)
            {
                switch (key)
                {
                    case "p": deltaPosition = data.ToVector3(); break;
                    case "t": deltaTangent = data.ToVector3(); break;
                    case "n": deltaNormal = data.ToVector3(); break;
                }
            }

            public override CfgEncoder Encode()
            {
                var cody = new CfgEncoder()
                .Add_IfNotZero("p", deltaPosition)
                .Add_IfNotZero("t", deltaTangent)
                .Add_IfNotZero("n", deltaNormal);

                return cody;
            }

            #endregion

            public BlendFrame()
            {

            }

            public BlendFrame(Vector3 pos, Vector3 norm, Vector3 tang)
            {
                deltaPosition = pos;
                deltaNormal = norm;
                deltaTangent = tang;
            }

        }
    }

    public static class MeshAnatomyExtensions
    {
        public static Vector3 SmoothVector(this List<PainterMesh.Triangle> td)
        {

            var v = Vector3.zero;

            foreach (var t in td)
                v += t.sharpNormal;

            return v.normalized;

        }

        public static bool Contains(this List<PainterMesh.MeshPoint> lst, PainterMesh.LineData ld) => lst.Contains(ld.vertexes[0].meshPoint) && lst.Contains(ld.vertexes[1].meshPoint);

        public static bool Contains(this List<PainterMesh.MeshPoint> lst, PainterMesh.Triangle ld)
        {
            foreach (var vertex in ld.vertexes)
            {
                if (!lst.Contains(vertex.meshPoint))
                    return false;
            }

            return true;
        }
    }
}