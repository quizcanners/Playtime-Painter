using QuizCanners.Migration;
using QuizCanners.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace PainterTool.MeshEditing
{

    public partial class PainterMesh
    {
        public class MeshPoint : PainterClassCfg//PainterClassKeepUnrecognizedCfg
        {
            // TEMPORATY DATA / NEEDS MANUAL UPDATE:
            public Vector3 normal;
            public int index;
            public bool normalIsSet;
            public float distanceToPointed;
            public Vector3 distanceToPointedV3;
            public static MeshPoint currentlyDecoded;
            public bool stagedForDeletion;

            // Data to save:
            public List<Vector2[]> sharedUVs = new();
            public List<Vertex> vertices;
            public Vector3 localPos;
            public bool smoothNormal;
            public Vector4 shadowBake;
            public List<List<BlendFrame>> shapes = new(); // not currently working
            public Vector4 edgeStrength;

         

     
            public void StripPointData_StageForDeleteFrom(MeshPoint pointB)
            {
                foreach (var buv in pointB.vertices)
                {
                    var uvs = new[] { buv.GetUvSet(0), buv.GetUvSet(1) };
                    vertices.Add(buv);
                    buv.meshPoint = this;
                    buv.SetUvIndexBy(uvs);
                }

                pointB.stagedForDeletion = true;
            }

            public void UVto01Space()
            {
                int ind = Painter.MeshManager.EditedUV;

                for (int i = 0; i < sharedUVs.Count; i++)
                    sharedUVs[i][ind] = sharedUVs[i][ind].To01Space();
            }

            public void RunDebug()
            {
                for (var i = 0; i < vertices.Count; i++)
                {
                    var up = vertices[i];

                    if (up.triangles.Count != 0)
                        continue;

                    vertices.RemoveAt(i);
                    i--;
                }

                foreach (var v in vertices)
                    v.RunDebug();

                CleanEmptyIndexes();
            }

            public bool SameAsLastFrame => this == EditedMesh.lastFramePointedUv.meshPoint;

            public bool TryChangeSmoothNormal(bool isSmooth)
            {
                if (isSmooth == smoothNormal)
                    return false;

                smoothNormal = isSmooth;

                return true;
            }

            public Vector3 WorldPos
            {
                get
                
                    //  if (!emc.AnimatedVertices())  
                    => MeshPainting.targetTransform.TransformPoint(localPos);

                    // var animNo = emc.GetVertexAnimationNumber();
                    //  return emc.transform.TransformPoint(localPos + anim[animNo]);

                
                set => localPos = MeshPainting.targetTransform.InverseTransformPoint(value); 
            }

            public Vector3 GetWorldNormal() => MeshPainting.targetTransform.TransformDirection(GetNormal());

            private Vector3 GetNormal()
            {
                normal = Vector3.zero;

                foreach (var u in vertices)
                    foreach (var t in u.triangles)
                        normal += t.GetSharpNormalLocalSpace();

                normal.Normalize();

                return normal;
            }

            #region Encode & Decode

            public override CfgEncoder Encode()
            {
                var cody = new CfgEncoder();

                foreach (var lst in sharedUVs)
                {
                    cody.Add("u0", lst[0]);
                    cody.Add("u1", lst[1]);
                }

                cody.Add_IfNotEmpty("uvs", vertices)

                    .Add("pos", localPos)

                    .Add_IfTrue("smth", smoothNormal)

                    .Add_IfNotZero("shad", shadowBake)

                   // .Add_IfNotEpsilon("edge", edgeStrength)
                   .Add("edgev4", edgeStrength)
                    .Add_IfNotEmpty("bs", shapes);

                return cody;
            }

            public override void DecodeTag(string key, CfgData data)
            {
                switch (key)
                {
                    case "u0":
                        sharedUVs.Add(new Vector2[2]);
                        sharedUVs[^1][0] = data.ToVector2();
                        break;
                    case "u1":
                        sharedUVs[^1][1] = data.ToVector2();
                        break;
                    case "uvs":
                        currentlyDecoded = this;
                        data.ToList(out vertices);
                        break;
                    case "pos":
                        localPos = data.ToVector3();
                        break;
                    case "smth":
                        smoothNormal = data.ToBool();
                        break;
                    case "shad":
                        shadowBake = data.ToVector4();
                        break;
                    case "edge": edgeStrength.x = data.ToFloat(); break;
                    case "edgev4": edgeStrength = data.ToVector4(); break;
                    case "bs": data.Decode_ListOfList(out shapes);break;
                }
            }

            #endregion

            public Vertex GetVertexForUv(Vector2 uv_0, Vector2 uv_1) 
            {
                foreach (var v in vertices) 
                {
                    if (v.GetUvSet(0) == uv_0 && v.GetUvSet(1) == uv_1)
                        return v;
                }

                return new Vertex(this, uv_0, uv_1);
            }

            public VertexUVsDataIndex GetUvsDataIndexFor(Vector2 uv_0, Vector2 uv_1)
            {
                var cnt = sharedUVs.Count;

                for (var i = 0; i < cnt; i++)
                {
                    var v2 = sharedUVs[i];
                    if (v2[0] == uv_0 && v2[1] == uv_1)
                        return VertexUVsDataIndex.From(i);
                }

                var tmp = new Vector2[2];
                tmp[0] = uv_0;
                tmp[1] = uv_1;

                sharedUVs.Add(tmp);

                return VertexUVsDataIndex.From(cnt);
            }

            public void CleanEmptyIndexes()
            {
                int cnt = sharedUVs.Count;

                bool[] used = new bool[cnt];
                int[] newIndexes = new int[cnt];

                foreach (var u in vertices)
                    used[u.uvIndex] = true;

                int currentInd = 0;

                for (int i = 0; i < cnt; i++)
                    if (used[i])
                    {
                        newIndexes[i] = currentInd;
                        currentInd++;
                    }

                if (currentInd < cnt)
                {

                    for (int i = cnt - 1; i >= 0; i--)
                        if (!used[i])
                            sharedUVs.RemoveAt(i);

                    foreach (var u in vertices) u.uvIndex = newIndexes[u.uvIndex];
                }
            }

            public MeshPoint()
            {
                Reboot(Vector3.zero);
            }

            public MeshPoint(Vector3 npos, bool editing = false)
            {
                Reboot(npos);

                if (editing)
                    smoothNormal = Painter.Data.newVerticesSmooth;
            }

            private void CopyFrom(MeshPoint other)
            {
                smoothNormal = other.smoothNormal;
                edgeStrength = other.edgeStrength;
            }

            public MeshPoint(MeshPoint other)
            {
                Reboot(other.localPos);
                CopyFrom(other);
            }

            public MeshPoint(MeshPoint other, Vector3 pos)
            {
                Reboot(pos);
                CopyFrom(other);
            }

            public void PixPerfect()
            {
                var trg = MeshPainting.target;

                if (trg && (trg.TexMeta != null))
                {
                    var id = trg.TexMeta;
                    var width = id.Width * 2;
                    var height = id.Height * 2;

                    foreach (var v2a in sharedUVs)
                        for (var i = 0; i < 2; i++)
                        {
                            var x = v2a[i].x;
                            var y = v2a[i].y;
                            x = Mathf.Round(x * width) / width;
                            y = Mathf.Round(y * height) / height;
                            v2a[i] = new Vector2(x, y);
                        }
                }
            }

            private void Reboot(Vector3 nPos)
            {
                localPos = nPos;
                vertices = new List<Vertex>();
            }

       /*     public void ClearColor(ColorMask bm)
            {
                foreach (var v in vertices)
                    bm.SetValuesOn(ref v.color, Color.black);
            }*/

            private void SetChanel(ColorChanel chan, MeshPoint other, float val)
            {
                foreach (var u in vertices)
                    if (u.ConnectedTo(other))
                        chan.SetValueOn(ref u.color, val);
            }

            public void SetColor(ColorMask bm, Color color) 
            {
                foreach (var v in vertices)
                    bm.SetValuesOn(ref v.color, color);
            }

            public bool FlipChanelOnLine(ColorChanel chan, MeshPoint other)
            {
                float val = 1;

                if (Painter.Data.makeVerticesUniqueOnEdgeColoring)
                    EditedMesh.GiveLineUniqueVerticesRefreshTriangleListing(new LineData(this, other));

                foreach (var u in vertices)
                    if (u.ConnectedTo(other))
                        val *= chan.GetValueFrom(u.color) * chan.GetValueFrom(u.GetConnectedUVinVertex(other).color);

                val = (val > 0.9f) ? 0 : 1;

                SetChanel(chan, other, val);
                other.SetChanel(chan, this, val);

                EditedMesh.Dirty = true;

                return Mathf.Approximately(val, 1);
            }

            public void SetColorOnLine(Color col, ColorMask bm, MeshPoint other)
            {
                foreach (var u in vertices)
                    if (u.ConnectedTo(other))
                        bm.SetValuesOn(ref u.color,
                            col); //val *= u._color.GetChanel01(chan) * u.GetConnectedUVinVertex(other)._color.GetChanel01(chan);

            }

            public void RemoveBorderFromLine(MeshPoint other)
            {
                foreach (var u in vertices)
                    if (u.ConnectedTo(other))
                        for (var i = 0; i < 4; i++)
                        {
                            var ouv = u.GetConnectedUVinVertex(other);
                            var ch = (ColorChanel)i;

                            var val = ch.GetValueFrom(u.color) * ch.GetValueFrom(ouv.color);

                            if (!(val > 0.9f)) continue;

                            ch.SetValueOn(ref u.color, 0);
                            ch.SetValueOn(ref ouv.color, 0);
                        }
            }

            public float DistanceTo(MeshPoint other) => (localPos - other.localPos).magnitude;

            internal void MergeWithNearest(MeshData edMesh)
            {
                var allVertices = edMesh.meshPoints;

                MeshPoint nearest = null;
                var maxDist = float.MaxValue;

                foreach (var v in allVertices)
                {
                    var dist = v.DistanceTo(this);
                    if (!(dist < maxDist) || v == this) continue;
                    maxDist = dist;
                    nearest = v;
                }

                if (nearest != null)
                    edMesh.Merge(this, nearest);
            }

            public List<Triangle> FindAllTriangles()
            {
                var allTriangles = new List<Triangle>();

                foreach (var uvi in vertices)
                    foreach (var tri in uvi.triangles)
                        allTriangles.Add(tri);

                return allTriangles;
            }

            public float ExtrusionLevelOneMinusOne() 
            {
                float extrusion = 0;

                List<Triangle> tris = FindAllTriangles();
                Vector3 norm = GetNormal();

                int points = 0;
                
                foreach (var t in tris) 
                {
                    foreach (var v in t.vertexes) 
                    {
                        var p = v.meshPoint;                        
                        if (p != this) 
                        {
                            points++;
                            Vector3 vector = p.localPos - localPos;
                            if (vector.magnitude > 0) 
                            {
                                extrusion -= Vector3.Dot(vector.normalized, norm);
                            }
                        }
                    }
                }

                if (points > 0)
                    extrusion /= points;

                return extrusion;
            }

            public bool AllPointsUnique()
            {
                return FindAllTriangles().Count <= vertices.Count;
            }

            public List<LineData> GetAllLines_USES_Tris_Listing()
            {
                var Alllines = new List<LineData>();


                foreach (var uvi in vertices)
                {
                    foreach (var tri in uvi.triangles)
                    {
                        LineData[] lines;
                        lines = tri.GetLinesFor(uvi);

                        for (var i = 0; i < 2; i++)
                        {
                            var same = false;
                            foreach (var t in Alllines)
                            {
                                if (!t.SameVertices(lines[i])) continue;
                                t.trianglesCount_Cahced++;
                                same = true;
                            }

                            if (!same)
                                Alllines.Add(lines[i]);

                        }

                    }
                }


                return Alllines;
            }

            public Triangle GetTriangleFromLine(MeshPoint other)
            {
                foreach (var t in vertices)
                {
                    foreach (var t1 in t.triangles)
                        if (t1.Includes(other))
                            return t1;
                }

                return null;
            }

            public List<Triangle> GetTrianglesFromLine(MeshPoint other)
            {
                var lst = new List<Triangle>();
                foreach (var t1 in vertices)
                    foreach (var t in t1.triangles)
                        if (t.Includes(other))
                            lst.Add(t);

                return lst;
            }


        }

    }
}