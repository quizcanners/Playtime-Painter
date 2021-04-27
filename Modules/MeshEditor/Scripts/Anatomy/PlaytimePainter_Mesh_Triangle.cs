using QuizCanners.Migration;
using QuizCanners.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PainterTool.MeshEditing
{
    public partial class PainterMesh
    {
        public class Triangle : PainterClassCfg
        {
            public Vertex[] vertexes = new Vertex[3];
            public bool[] isPointDominant = new bool[3];
            public float[] edgeWeight = new float[3];
            public Vector4 textureNo;
            public int subMeshIndex;
            public bool subMeshIndexRemapped;
            public Vector3 sharpNormal;

            public MeshPoint this[int index] => vertexes[index].meshPoint;

            public LineData this[LineInTriangleIndex line] => new(this, new[] { vertexes[line.index], vertexes[(line.index + 1) % 3] });

            public Vertex this[VertexIndexInTriangle vertex]
            {
                get => vertexes[vertex.index];
                set => vertexes[vertex.index] = value;
            }

            public void SetEdgeWeight(LineData line, float value) => edgeWeight[GetIndexOfOppositeTo(line).index] = value;

            public void RunDebug()
            {

            }

            public Vector3 GetSharpNormalWorldSpace()
            {
                sharpNormal = QcMath.GetNormalOfTheTriangle(
                    vertexes[0].meshPoint.WorldPos,
                    vertexes[1].meshPoint.WorldPos,
                    vertexes[2].meshPoint.WorldPos);

                return sharpNormal;
            }

            public Vector3 GetSharpNormalLocalSpace()
            {
                sharpNormal = QcMath.GetNormalOfTheTriangle(
                    vertexes[0].LocalPos,
                    vertexes[1].LocalPos,
                    vertexes[2].LocalPos);

                return sharpNormal;
            }

            public Vector3 GetNormalByArea()
            {
                float accuracyFix = 1000f;

                var p0 = vertexes[0].LocalPos * accuracyFix;
                var p1 = vertexes[1].LocalPos * accuracyFix;
                var p2 = vertexes[2].LocalPos * accuracyFix;

                sharpNormal = QcMath.GetNormalOfTheTriangle(
                    p0,
                    p1,
                    p2);

                return sharpNormal * Vector3.Cross(p0 - p1, p0 - p2).magnitude;
            }

            public bool SameAsLastFrame => this == EditedMesh.lastFramePointedTriangle;

            public float Area =>
                Vector3.Cross(vertexes[0].LocalPos - vertexes[1].LocalPos, vertexes[0].LocalPos - vertexes[2].LocalPos)
                    .magnitude * 0.5f;

            #region Encode & Decode

            public override CfgEncoder Encode()
            {
                var cody = new CfgEncoder()

                    .Add_IfTrue("f0", isPointDominant[0])
                    .Add_IfTrue("f1", isPointDominant[1])
                    .Add_IfTrue("f2", isPointDominant[2])

                    .Add_IfNotEpsilon("ew0", edgeWeight[0])
                    .Add_IfNotEpsilon("ew1", edgeWeight[1])
                    .Add_IfNotEpsilon("ew2", edgeWeight[2]);

                for (var i = 0; i < 3; i++)
                    cody.Add(i.ToString(), vertexes[i].finalIndex);

                    cody.Add_IfNotZero("tex", textureNo)
                    .Add_IfNotZero("sub", subMeshIndex);


                return cody;
            }

            public override void DecodeTag(string key, CfgData data)
            {
                switch (key)
                {
                    case "tex": textureNo = data.ToVector4(); break;
                    case "f0": isPointDominant[0] = true; break;
                    case "f1": isPointDominant[1] = true; break;
                    case "f2": isPointDominant[2] = true; break;
                    case "ew0": edgeWeight[0] = data.ToFloat(); break;
                    case "ew1": edgeWeight[1] = data.ToFloat(); break;
                    case "ew2": edgeWeight[2] = data.ToFloat(); break;
                    case "sub": data.ToInt(ref subMeshIndex); break;
                    case "0": vertexes[0] = MeshData.decodedEditableMesh.uvsByFinalIndex[data.ToInt()]; break;
                    case "1": vertexes[1] = MeshData.decodedEditableMesh.uvsByFinalIndex[data.ToInt()]; break;
                    case "2": vertexes[2] = MeshData.decodedEditableMesh.uvsByFinalIndex[data.ToInt()]; break;
                }
            }

            #endregion

            public Triangle CopySettingsFrom(Triangle td)
            {
                for (var i = 0; i < 3; i++)
                    isPointDominant[i] = td.isPointDominant[i];
                textureNo = td.textureNo;
                subMeshIndex = td.subMeshIndex;

                return this;
            }

            public bool wasProcessed;

            public bool ContainsUv(Vertex vrt)
            {
                foreach (var vertex in vertexes)
                {
                    if (vertex.SameUv(vrt, uvSetId: 0))
                        return true;
                }

                return false;
            }

            public bool Contains(Vertex vrt)
            {
                foreach (var vertex in vertexes)
                {
                    if (vertex == vrt)
                        return true;
                }

                return false;
            }

            public bool Contains(MeshPoint vrt)
            {
                foreach (var vertex in vertexes)
                {
                    if (vertex.meshPoint == vrt)
                        return true;
                }

                return false;
            }

            public bool SetSmoothVertices(bool to)
            {
                var changed = false;
                for (var i = 0; i < 3; i++)
                    if (this[i].smoothNormal != to)
                    {
                        changed = true;
                        this[i].smoothNormal = to;
                    }

                return changed;
            }

            public bool SetSharpCorners(bool to)
            {
                var changed = false;
                for (var i = 0; i < 3; i++)
                    if (isPointDominant[i] != to)
                    {
                        changed = true;
                        isPointDominant[i] = to;
                    }

                return changed;
            }

            public bool SetColor(Color col)
            {
                var changed = false;

                foreach (var p in vertexes)
                    changed |= p.SetColor(col);

                return changed;
            }

            public void InvertNormal()
            {
                (vertexes[2], vertexes[0]) = (vertexes[0], vertexes[2]);
            }

            public bool IsSamePoints(Vertex[] other)
            {
                foreach (var v in other)
                {
                    var same = false;
                    foreach (var v1 in vertexes)
                        if (v.meshPoint == v1.meshPoint)
                            same = true;

                    if (!same) return false;
                }

                return true;
            }

            public bool IsSameUv(Vertex[] other)
            {
                foreach (var v in other)
                {
                    var same = false;
                    foreach (var v1 in vertexes)
                        if (v == v1)
                            same = true;

                    if (!same) return false;
                }

                return true;
            }

            public bool IsSameAs(MeshPoint[] other)
            {
                foreach (var v in other)
                {
                    var same = false;
                    foreach (var v1 in vertexes)
                        if (v == v1.meshPoint)
                            same = true;

                    if (!same) return false;
                }

                return true;
            }

            public bool IsNeighbourOf(Triangle td)
            {
                if (td == this) return false;

                var same = 0;

                foreach (var u in td.vertexes)
                    for (var i = 0; i < 3; i++)
                        if (vertexes[i].meshPoint == u.meshPoint)
                        {
                            same++;
                            break;
                        }

                return same == 2;
            }

            public void Set(Vertex[] nvrts)
            {
                for (var i = 0; i < 3; i++)
                    vertexes[i] = nvrts[i];
            }

            #region Includes

            public bool Includes(Vertex vrt)
            {
                for (var i = 0; i < 3; i++)
                    if (vrt == vertexes[i])
                        return true;
                return false;
            }

            public bool Includes(MeshPoint vrt)
            {
                for (var i = 0; i < 3; i++)
                    if (vrt == vertexes[i].meshPoint)
                        return true;
                return false;
            }

            public bool Includes(MeshPoint a, MeshPoint b)
            {
                var cnt = 0;
                for (var i = 0; i < 3; i++)
                    if ((a == vertexes[i].meshPoint) || (b == vertexes[i].meshPoint))
                        cnt++;

                return cnt > 1;
            }

            public bool Includes(LineData ld) =>
                (Includes(ld.vertexes[0].meshPoint) && (Includes(ld.vertexes[1].meshPoint)));

            #endregion

            public bool PointOnTriangle()
            {
                var va = vertexes[0].meshPoint.distanceToPointedV3; //point.DistanceV3To(uvpnts[0].pos);
                var vb = vertexes[1].meshPoint.distanceToPointedV3; //point.DistanceV3To(uvpnts[1].pos);
                var vc = vertexes[2].meshPoint.distanceToPointedV3; //point.DistanceV3To(uvpnts[2].pos);

                var sum = Vector3.Angle(va, vb) + Vector3.Angle(va, vc) + Vector3.Angle(vb, vc);
                return (Mathf.Abs(sum - 360) < 0.01f);
            }

            public int NumberOf(Vertex pnt)
            {
                for (var i = 0; i < 3; i++)
                    if (pnt == vertexes[i])
                        return i;

                return -1;
            }

            public Vertex GetClosestTo(Vector3 fPos)
            {

                var nearest = vertexes[0];
                for (int i = 1; i < 3; i++)
                    if ((fPos - vertexes[i].LocalPos).magnitude < (fPos - nearest.LocalPos).magnitude)
                        nearest = vertexes[i];

                return nearest;

            }

            public Vertex GetByVertex(MeshPoint vrt)
            {
                for (var i = 0; i < 3; i++)
                    if (vertexes[i].meshPoint == vrt)
                        return vertexes[i];

                Debug.Log("Error using Get By Vertex");
                return null;
            }

            public Vector2 LocalPosToEditedUv(Vector3 localPos)
            {
                var w = DistanceToWeight(localPos);
                var ind = Painter.MeshManager.EditedUV;
                return vertexes[0].GetUvSet(ind) * w.x + vertexes[1].GetUvSet(ind) * w.y + vertexes[2].GetUvSet(ind) * w.z;
            }

            public Vector3 DistanceToWeight(Vector3 localPos)
            {

                var p1 = vertexes[0].LocalPos;
                var p2 = vertexes[1].LocalPos;
                var p3 = vertexes[2].LocalPos;

                var f1 = p1 - localPos;
                var f2 = p2 - localPos;
                var f3 = p3 - localPos;

                var a = Vector3.Cross(p2 - p1, p3 - p1).magnitude; // main triangle area a
                var p = new Vector3(
                    Vector3.Cross(f2, f3).magnitude / a,
                    Vector3.Cross(f3, f1).magnitude / a,
                    Vector3.Cross(f1, f2).magnitude / a // p3's triangle area / a
                );
                return p;
            }

            public void AssignWeightedData(Vertex to, Vector3 weight)
            {

                to.color = vertexes[0].color * weight.x + vertexes[1].color * weight.y + vertexes[2].color * weight.z;
                to.meshPoint.shadowBake = vertexes[0].meshPoint.shadowBake * weight.x +
                                          vertexes[1].meshPoint.shadowBake * weight.y +
                                          vertexes[2].meshPoint.shadowBake * weight.z;
                var nearest = (Mathf.Max(weight.x, weight.y) > weight.z)
                    ? (weight.x > weight.y ? vertexes[0] : vertexes[1])
                    : vertexes[2];

                var newV20 = vertexes[0].GetUvSet(0) * weight.x + vertexes[1].GetUvSet(0) * weight.y +
                             vertexes[2].GetUvSet(0) * weight.z;
                var newV21 = vertexes[0].GetUvSet(1) * weight.x + vertexes[1].GetUvSet(1) * weight.y +
                             vertexes[2].GetUvSet(1) * weight.z;

                to.SetUvIndexBy(newV20, newV21);

                to.boneWeight = nearest.boneWeight;
            }

            public void Replace(Vertex point, Vertex with)
            {
                for (var i = 0; i < 3; i++)
                    if (vertexes[i] == point)
                    {
                        vertexes[i] = with;
                        return;
                    }

            }

            public void Replace(VertexIndexInTriangle i, Vertex with)
            {
                vertexes[i.index].triangles.Remove(this);
                vertexes[i.index] = with;
                with.triangles.Add(this);

            }

            public VertexIndexInTriangle GetIndexOfOppositeTo(LineData l)
            {
                for (var i = 0; i < 3; i++)
                    if ((vertexes[i].meshPoint != l.vertexes[0].meshPoint) &&
                        (vertexes[i].meshPoint != l.vertexes[1].meshPoint))
                        return VertexIndexInTriangle.From(i);

                return VertexIndexInTriangle.From(0);
            }

            public Vertex GetNotOneOf(MeshPoint a, MeshPoint b)
            {
                for (var i = 0; i < 3; i++)
                    if ((vertexes[i].meshPoint != a) && (vertexes[i].meshPoint != b))
                        return vertexes[i];

                return vertexes[0];
            }

            public Vertex GetNotOneIn(LineData l)
            {
                for (var i = 0; i < 3; i++)
                    if ((vertexes[i].meshPoint != l.vertexes[0].meshPoint) &&
                        (vertexes[i].meshPoint != l.vertexes[1].meshPoint))
                        return vertexes[i];

                return vertexes[0];
            }

            public Vertex NotOneOf(Vertex[] others)
            {
                for (var i = 0; i < 3; i++)
                {
                    bool same;
                    same = false;
                    foreach (var uvi in others)
                        if (uvi.meshPoint == vertexes[i].meshPoint)
                            same = true;

                    if (!same) return vertexes[i];
                }

                return null;
            }

            public bool GiveUniqueVerticesAgainst(Triangle td)
            {
                var changed = false;
                for (var i = 0; i < 3; i++)
                {
                    var u = vertexes[i];

                    if (!td.Includes(u)) continue;

                    vertexes[i] = new Vertex(u.meshPoint);
                    changed = true;
                }

                return changed;
            }

            public void MergeAround(Triangle other, MeshPoint vrt)
            {
                for (var i = 0; i < 3; i++)
                {
                    if (Includes(other.vertexes[i].meshPoint)) continue;
                    Replace(GetByVertex(vrt), other.vertexes[i]);
                    return;
                }

            }

            public void MakeTriangleVertexUnique(Vertex pnt)
            {
                if (pnt.triangles.Count == 1) return;

                var nuv = new Vertex(pnt.meshPoint, pnt);

                Replace(pnt, nuv);

                EditedMesh.Dirty = true;
            }

            public Triangle NewForCopiedVertices()
            {
                var nPoints = new Vertex[3];

                for (var i = 0; i < 3; i++)
                {
                    if (vertexes[i].myLastCopy == null)
                    {
                        Debug.Log("Error: UV has not been copied!");
                        return null;
                    }

                    nPoints[i] = vertexes[i].myLastCopy;
                }

                return new Triangle(nPoints);

            }

            public Triangle(IReadOnlyList<Vertex> nPoints)
            {
                for (var i = 0; i < 3; i++)
                {
                    vertexes[i] = nPoints[i];
                    vertexes[i].triangles.Add(this);
                }
            }

            public Triangle()
            {
            }

            public List<LineData> Lines()
            {
                var lines = new List<LineData>(3);

                for (int i = 0; i < 3; i++)
                    lines.Add(this[LineInTriangleIndex.From(i)]); //new LineData(this, new[] { vertexes[i], vertexes[(i+1) % 3] }));


                return lines;
            }

            public LineData[] GetLinesFor(Vertex pnt)
            {
                var ld = new LineData[2];
                var no = NumberOf(pnt);
                ld[0] = new LineData(this, new[] { vertexes[no], vertexes[(no + 1) % 3] });
                ld[1] = new LineData(this, new[] { vertexes[(no + 2) % 3], vertexes[no] });

                return ld;
            }

            public List<Triangle> GetNeighboringTriangles()
            {
                var lst = new List<Triangle>();

                foreach (var u in vertexes)
                    foreach (var t in u.triangles)
                        if (!lst.Contains(t) && t.IsNeighbourOf(this))
                            lst.Add(t);

                return lst;
            }

            public List<Triangle> GetNeighboringTrianglesUnprocessed()
            {
                var lst = new List<Triangle>();

                foreach (var u in vertexes)
                    foreach (var t in u.meshPoint.FindAllTriangles())
                        if (!t.wasProcessed && !lst.Contains(t) && t.IsNeighbourOf(this))
                            lst.Add(t);

                return lst;
            }

            public LineData LineWith(Triangle other)
            {

                var l = new List<MeshPoint>(); //= new LineData();

                foreach (var u in vertexes)
                {
                    foreach (var vertex in other.vertexes)
                    {
                        if (u.meshPoint == vertex.meshPoint)
                            l.Add(u.meshPoint);
                    }

                    /* if (other.vertexes.Any(u2 => u.meshPoint == u2.meshPoint))
                     {
                         l.Add(u.meshPoint);
                     }*/
                }

                return l.Count == 2 ? new LineData(l[0], l[1]) : null;
            }

            public LineData ShortestLine()
            {
                var shortest = float.PositiveInfinity;
                VertexIndexInTriangle shortestVertex = VertexIndexInTriangle.From(0);
                for (var i = 0; i < 3; i++)
                {
                    var len = (this[i].localPos - this[(i + 1) % 3].localPos).magnitude;
                    if (!(len < shortest)) continue;
                    shortest = len;
                    shortestVertex.index = i;
                }

                return new LineData(this[shortestVertex].meshPoint, this[(shortestVertex.index + 1) % 3]);

            }

            public bool MoveNext()
            {
                throw new System.NotImplementedException();
            }

            public void Reset()
            {
                throw new System.NotImplementedException();
            }

            public void Dispose()
            {
                throw new System.NotImplementedException();
            }
        }
    }
}

