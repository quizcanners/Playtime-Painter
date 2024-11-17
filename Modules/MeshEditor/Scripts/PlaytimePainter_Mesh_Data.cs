using System;
using System.Collections.Generic;
using QuizCanners.Inspect;
using QuizCanners.Migration;
using QuizCanners.Utils;
using UnityEngine;

namespace PainterTool.MeshEditing
{
#pragma warning disable IDE0019 // Use pattern matching

    internal class MeshData : PainterClassCfg, IPEGI, ICfgCustom
    {
        public string meshName = "unnamed";

        public bool Dirty
        {
            get
            {
                return dirtyColor || dirtyVertexIndexes || dirtyPosition || dirtyUvs;
            }
            set
            {

                dirtyVertexIndexes = value;
                dirtyColor = value;
                dirtyPosition = value;
                dirtyUvs = value;
            }
        }
        public bool dirtyPosition;
        public bool dirtyColor;
        public bool dirtyVertexIndexes;
        public bool dirtyUvs;
        public int vertexCount;
        public int maxGroupIndex;
        public bool firstBuildRun;
        public bool gotBoneWeights;
        public int subMeshCount = 1;
        public int uv2DistributeRow;
        public int uv2DistributeCurrent;

        public List<uint> baseVertex = new();
        public Countless<Color> groupColors = new();
        public List<string> shapes;
        public readonly UnNullable<Countless<float>> blendWeights = new();
        public Matrix4x4[] bindPoses;
        public List<PainterMesh.MeshPoint> meshPoints = new();
        public void Remove(MeshPointIndex point) => meshPoints.RemoveAt(point.index);
        public PainterMesh.MeshPoint this[MeshPointIndex point] => meshPoints[point.index];
        public List<PainterMesh.Triangle> triangles = new();
        //public Mesh actualMesh;
        public float averageSize;

        public void Edit(PainterComponent painter)
        {
            if (!painter.SharedMesh) return;

            if (!painter.SavedEditableMesh.ToString().IsNullOrEmpty())
            {
                this.Decode(painter.SavedEditableMesh);
                if (triangles.Count == 0)
                    BreakMesh(painter.SharedMesh);
                else return;
            }
            else
            {
                BreakMesh(painter.SharedMesh);
            }

            var mat = painter.Material;

            if (!mat)
                return;

            var name = mat.Get(ShaderTags.MeshSolution, false, "Standard");

            var prf = Painter.Data.meshPackagingSolutions;

            for (var i = 0; i < prf.Count; i++)
                if (prf[i].name.SameAs(name)) painter.selectedMeshProfile = name;
        }

        private void BreakMesh(Mesh mesh)
        {
            if (!mesh)
                return;

            meshName = mesh.name;

            using (QcDebug.TimeProfiler.Start("Breaking mesh").SetLogTreshold(1)) { 

                var vCnt = mesh.vertices.Length;

                meshPoints = new List<PainterMesh.MeshPoint>();
                var vertices = mesh.vertices;

                var cols = mesh.colors;
                var bW = mesh.boneWeights;
                var uv1 = mesh.uv;
                var uv2 = mesh.uv2;
                bindPoses = mesh.bindposes;

                var gotUv1 = (uv1 != null) && (uv1.Length == vCnt);
                var gotUv2 = (uv2 != null) && (uv2.Length == vCnt);
                var gotColors = (cols != null) && (cols.Length == vCnt);
                gotBoneWeights = (bW != null) && (bW.Length == vCnt);

                for (var i = 0; i < vCnt; i++)
                {
                    var v = new PainterMesh.MeshPoint(vertices[i]);
                    meshPoints.Add(v);
                    new PainterMesh.Vertex(meshPoints[i], gotUv1 ? uv1[i] : Vector2.zero, gotUv2 ? uv2[i] : Vector2.zero);
                }

                if (gotColors)
                    for (var i = 0; i < vCnt; i++)
                    {
                        var p = meshPoints[i];
                        p.vertices[0].color = cols[i];
                    }

                //   "Got Colors".TimerEnd_Restart();

                if (gotBoneWeights)
                    for (var i = 0; i < vCnt; i++)
                    {
                        var p = meshPoints[i];
                        p.vertices[0].boneWeight = bW[i];
                    }


            //   "Gote Bone Weights".TimerEnd_Restart();

                shapes = new List<string>();

                for (var s = 0; s < mesh.blendShapeCount; s++)
                {

                    for (var v = 0; v < vCnt; v++)
                        meshPoints[v].shapes.Add(new List<PainterMesh.BlendFrame>());

                    shapes.Add(mesh.GetBlendShapeName(s));

                    for (var f = 0; f < mesh.GetBlendShapeFrameCount(s); f++)
                    {

                        blendWeights[s][f] = mesh.GetBlendShapeFrameWeight(s, f);

                        var normals = new Vector3[vCnt];
                        var pos = new Vector3[vCnt];
                        var tng = new Vector3[vCnt];
                        mesh.GetBlendShapeFrameVertices(s, f, pos, normals, tng);

                        for (var v = 0; v < vCnt; v++)
                            meshPoints[v].shapes[^1].Add(new PainterMesh.BlendFrame(pos[v], normals[v], tng[v]));

                    }
                }

                triangles = new List<PainterMesh.Triangle>();
                var points = new PainterMesh.Vertex[3];

                subMeshCount = Mathf.Max(1, mesh.subMeshCount);
                baseVertex = new List<uint>();

                //   "Blend Shapes Done".TimerEnd_Restart();

                for (var s = 0; s < subMeshCount; s++)
                {
                    baseVertex.Add(mesh.GetBaseVertex(s));

                    var indices = mesh.GetTriangles(s);

                    var tCnt = indices.Length / 3;

                    for (var i = 0; i < tCnt; i++)
                    {
                        for (var e = 0; e < 3; e++)
                            points[e] = meshPoints[indices[i * 3 + e]].vertices[0];

                        var t = new PainterMesh.Triangle(points)
                        {
                            subMeshIndex = s
                        };
                        triangles.Add(t);
                    }
                }

                //   "Triangles done".TimerEnd_Restart();

                if (vCnt > 50)
                {

                    //    Debug.Log("Using caching to merge vertex points.");
                    // var mSize = mesh.bounds;

                    float coef = 10000f / mesh.bounds.size.magnitude;

                    UnNullableLists<PainterMesh.MeshPoint> distanceGroups = new();

                    for (var i = 0; i < vCnt; i++)
                    {
                        var p = meshPoints[i];
                        distanceGroups[Mathf.FloorToInt(p.localPos.magnitude * coef)].Add(p);
                    }

                    var grps = distanceGroups.GetAllObjsNoOrder();

                    //  Debug.Log("Got {0} groups".F(grps.Count));

                    if (vCnt > 10000)
                    {
                        Debug.LogWarning("Too many vertexes. Skipping grouping.TO DO: merge in a coroutine");

                    }
                    else
                    {

                        foreach (var groupList in grps)
                        {
                            var cnt = groupList.Count;

                            for (var aInd = 0; aInd < cnt; aInd++)
                            {
                                var aPoint = groupList[aInd];

                                for (var bInd = aInd + 1; bInd < cnt; bInd++)
                                {
                                    var bPoint = groupList[bInd];

                                    if (bPoint.localPos.Equals(aPoint.localPos))
                                    {
                                        aPoint.StripPointData_StageForDeleteFrom(bPoint);
                                        groupList.RemoveAt(bInd);
                                        bInd--;
                                        cnt--;
                                    }
                                }
                            }
                        }

                    }
                    distanceGroups.Clear();
                    DeleteStagedMeshPoints();
                }
                else
                {
                    for (var i = 0; i < vCnt; i++)
                    {
                        var main = meshPoints[i];
                        for (var j = i + 1; j < vCnt; j++)
                        {
                            // if (!((meshPoints[j].localPos - main.localPos).magnitude < float.Epsilon)) continue;

                            if (meshPoints[j].localPos.Equals(main.localPos))
                            {
                                Merge(MeshPointIndex.From(i), MeshPointIndex.From(j));
                                j--;
                                vCnt = meshPoints.Count;
                            }
                        }
                    }
                }
            }

            //mesh = new Mesh();
            Dirty = true;
        }

        public void DeleteStagedMeshPoints()
        {
            var vCnt = meshPoints.Count;

            for (var i = vCnt - 1; i >= 0; i--)
            {
                var p = meshPoints[i];
                if (p.stagedForDeletion)
                    meshPoints.RemoveAt(i);
            }
        }

        public MeshData()
        {

        }

        public MeshData(PainterComponent painter)
        {
            Edit(painter);
        }

        #region Editing

        public void Merge(MeshPointIndex indA, MeshPointIndex indB) => Merge(this[indA], indB);

        public void Merge(PainterMesh.MeshPoint pointA, MeshPointIndex indB)
        {

            var pointB = this[indB];

            if (pointA == pointB)
                return;

            pointA.StripPointData_StageForDeleteFrom(pointB);

            /*foreach (var buv in pointB.vertices)
            {
                var uvs = new Vector2[] { buv.GetUv(0), buv.GetUv(1) };
                pointA.vertices.Add(buv);
                buv.meshPoint = pointA;
                buv.SetUvIndexBy(uvs);
            }*/

            Remove(indB);
        }

        public void Merge(PainterMesh.MeshPoint pointA, PainterMesh.MeshPoint pointB)
        {

            if (pointA == pointB)
                return;

            pointA.StripPointData_StageForDeleteFrom(pointB);

            /*foreach (var buv in pointB.vertices)
            {
                var uvs = new Vector2[] { buv.GetUv(0), buv.GetUv(1) };
                pointA.vertices.Add(buv);
                buv.meshPoint = pointA;
                buv.SetUvIndexBy(uvs);
            }*/

            meshPoints.Remove(pointB);
        }

        public bool SetAllUVsShared(PainterMesh.MeshPoint pnt)
        {
            if (pnt.vertices.Count == 1)
                return false;

            while (pnt.vertices.Count > 1)
                if (!MoveTriangle(pnt.vertices[1], pnt.vertices[0])) break;

            Dirty = true;
            return true;
        }

        public bool SetAllVerticesShared(PainterMesh.Triangle tri)
        {
            var changed = false;

            for (var i = 0; i < 3; i++)
                changed |= SetAllUVsShared(tri[i]);

            if (changed)
                Dirty = true;

            return changed;
        }

        public bool AllVerticesShared(PainterMesh.LineData ld)
        {
            var changed = false;
            for (var i = 0; i < 2; i++)
                changed |= SetAllUVsShared(ld[i]);

            return changed;
        }

        public void DeleteUv(PainterMesh.Vertex uv)
        {
            var vrt = uv.meshPoint;

            NullPointedSelected();

            for (var i = 0; i < triangles.Count; i++)
            {
                if (!triangles[i].Includes(uv))
                    continue;

                triangles.RemoveAt(i);
                i--;
            }

            if (IsInTriangleSet(uv))
                triVertices = 0;

            vrt.vertices.Remove(uv);

            if (vrt.vertices.Count == 0)
                meshPoints.Remove(vrt);

            Dirty = true;
        }

        public void DeleteLine(PainterMesh.LineData ld)
        {
            NullPointedSelected();

            RemoveLine(ld);
        }

        #endregion

        #region Encode & Decode
        public override CfgEncoder Encode()
        {
            var cody = new CfgEncoder()
            .Add_String("n", meshName)
            .Add_IfNotZero("grM", maxGroupIndex)
            .Add_IfNotEmpty("vrt", meshPoints)
            .Add_IfNotEmpty("tri", triangles)
            .Add("sub", subMeshCount)
            .Add_IfTrue("wei", gotBoneWeights)
            .Add("bv", baseVertex)
            .Add("gcls", groupColors.Encode())
            .Add("biP", bindPoses);

            if (uv2DistributeRow > 0)
            {
                cody.Add("UV2dR", uv2DistributeRow);
                cody.Add("UV2cur", uv2DistributeCurrent);
            }
            if (selectedUv != null)
                cody.Add("sctdUV", meshPoints.IndexOf(selectedUv.meshPoint));
            if (selectedTriangle != null)
                cody.Add("sctdTris", triangles.IndexOf(selectedTriangle));

            if (!MeshToolBase.AllTools.IsNullOrEmpty())
                foreach (var t in MeshToolBase.allToolsWithPerMeshData)
                {
                    var mtb = t as MeshToolBase;
                    if (mtb != null)
                        cody.Add(mtb.StdTag, t.EncodePerMeshData());
                }

            return cody;
        }

        public static MeshData decodedEditableMesh;

        public void DecodeInternal(CfgData data)
        {
            decodedEditableMesh = this;

            this.DecodeTagsFrom(data);

            decodedEditableMesh = null;
        }

        public override void DecodeTag(string key, CfgData data)
        {
            switch (key)
            {
                case "vrt": data.ToList(out meshPoints); break;
                case "tri": data.ToList(out triangles); break;
                case "n": meshName = data.ToString(); break;
                case "grM": data.ToInt(ref maxGroupIndex); break;
                case "sub": data.ToInt(ref subMeshCount); break;
                case "wei": gotBoneWeights = data.ToBool(); break;

                case "gcls": data.ToCountless(out groupColors); break;

                case "bv": data.ToList(out baseVertex); break;
                case "biP": data.ToArray(out bindPoses); break;
                case "UV2dR": data.ToInt(ref uv2DistributeRow); break;
                case "UV2cur": data.ToInt(ref uv2DistributeCurrent); break;
                case "sctdUV":
                    selectedUv = meshPoints[data.ToInt()].vertices[0]; break;
                case "sctdTris":
                    selectedTriangle = triangles[data.ToInt()]; break;
                default:
                    if (MeshToolBase.AllTools.IsNullOrEmpty()) break;

                    foreach (var t in MeshToolBase.allToolsWithPerMeshData)
                    {
                        var mt = t as MeshToolBase;
                        if (mt == null || !mt.StdTag.Equals(key)) continue;
                        mt.Decode(data);
                       break;
                    }
                    break;

                    
            }
        }
        #endregion

        #region Point & Select

        private Vector3 _previousPointed;
        private float _recalculateDelay;
        private int _nearestVertexCount = 50;
        private float _distanceLimit = 1;
        private const float NearTarget = 64;

        public readonly List<PainterMesh.MeshPoint> _draggedVertices = new();
        private readonly List<PainterMesh.MeshPoint> _sortVerticesClose = new();
        private readonly List<PainterMesh.MeshPoint> _sortVerticesFar = new();
        public CountlessCfg<PainterMesh.Vertex> uvsByFinalIndex = new();

        public PainterMesh.Vertex selectedUv;

        public PainterMesh.LineData selectedLine;

        public PainterMesh.Triangle selectedTriangle;

        public PainterMesh.Vertex pointedUv;

        public PainterMesh.LineData pointedLine;

        public PainterMesh.Triangle pointedTriangle;

        public PainterMesh.Vertex lastFramePointedUv;

        public PainterMesh.LineData lastFramePointedLine;

        public PainterMesh.Triangle lastFramePointedTriangle;

        public PainterMesh.MeshPoint PointedVertex => pointedUv?.meshPoint;

        public PainterMesh.MeshPoint GetClosestToPos(Vector3 pos)
        {
            PainterMesh.MeshPoint closest = null;
            float dist = 0;
            foreach (var v in meshPoints)
            {
                var newDist = Vector3.Distance(v.localPos, pos);

                if (closest != null && !(dist > newDist)) continue;

                dist = newDist;

                closest = v;

            }
            return closest;
        }

        public void ClearLastPointed()
        {
            lastFramePointedUv = null;
            lastFramePointedLine = null;
            lastFramePointedTriangle = null;
        }

        public void SetLastPointed(PainterMesh.Vertex uv)
        {
            ClearLastPointed();
            lastFramePointedUv = uv;
        }

        public void SetLastPointed(PainterMesh.LineData l)
        {
            ClearLastPointed();
            lastFramePointedLine = l;
        }

        public void SetLastPointed(PainterMesh.Triangle t)
        {
            ClearLastPointed();
            lastFramePointedTriangle = t;
        }

        public PainterMesh.Vertex[] triangleSet = new PainterMesh.Vertex[3];

        public int triVertices;

        public void SortAround(Vector3 center, bool forceRecalculate)
        {

            _distanceLimit = Mathf.Max(0.1f, _distanceLimit * (1f - Time.deltaTime));

            _recalculateDelay -= Time.deltaTime;

            var recalculated = false;
            var near = 0;
            if (((center - _previousPointed).magnitude > _distanceLimit * 0.05f) || (_recalculateDelay < 0) || (forceRecalculate))
            {
                foreach (var t in meshPoints)
                {
                    t.distanceToPointedV3 = (center - t.localPos);
                    t.distanceToPointed = t.distanceToPointedV3.magnitude;
                    if (t.distanceToPointed < _distanceLimit)
                        near++;
                }

                recalculated = true;
            }

            if (meshPoints.Count > NearTarget * 1.5f)
            {

                if (recalculated)
                {

                    _distanceLimit += Mathf.Max(-_distanceLimit * 0.5f, ((NearTarget - near) / NearTarget) * _distanceLimit);

                    _previousPointed = center;
                    _recalculateDelay = 1;

                    _sortVerticesClose.Clear();
                    _sortVerticesFar.Clear();

                    foreach (var v in meshPoints)
                        if (v.distanceToPointed > _distanceLimit)
                            _sortVerticesFar.Add(v);
                        else
                            _sortVerticesClose.Add(v);

                    _nearestVertexCount = _sortVerticesClose.Count;

                    meshPoints.Clear();
                    meshPoints.AddRange(_sortVerticesClose);
                    meshPoints.AddRange(_sortVerticesFar);

                    _distanceLimit += Mathf.Max(-_distanceLimit * 0.5f, (NearTarget - _sortVerticesClose.Count) * _distanceLimit / NearTarget);

                }

                for (var j = 0; j < 25; j++)
                {
                    var changed = false;
                    for (var i = 0; i < _nearestVertexCount - 1; i++)
                    {
                        var a = meshPoints[i];
                        var b = meshPoints[i + 1];

                        if (!(a.distanceToPointed > b.distanceToPointed)) continue;

                        meshPoints[i] = b;
                        meshPoints[i + 1] = a;
                        changed = true;
                    }

                    if (!changed)
                        break;

                }
            }
            else
            {
                meshPoints.Sort((a, b) =>
                    b.distanceToPointed < a.distanceToPointed
                        ? 1
                        : ((b.distanceToPointed - 0.0001f) < a.distanceToPointed ? 0 : -1)
                );

            }

        }

        public void AddToTrisSet(PainterMesh.Vertex nuv)
        {

            triangleSet[triVertices] = nuv;
            triVertices++;

            if (triVertices == 3)
                foreach (var t in triangles)
                    if (t.IsSamePoints(triangleSet))
                    {
                        t.Set(triangleSet);
                        Dirty = true;
                        triVertices = 0;
                        return;
                    }


            if (triVertices < 3) return;

            var td = new PainterMesh.Triangle(triangleSet);

            triangles.Add(td);

            if (!PlaytimePainter_EditorInputManager.Control)
            {
                MakeTriangleVertUnique(td, triangleSet[0]);
                MakeTriangleVertUnique(td, triangleSet[1]);
                MakeTriangleVertUnique(td, triangleSet[2]);
            }

            triVertices = 0;
            Dirty = true;
        }

        public void SwapLine(PainterMesh.MeshPoint a, PainterMesh.MeshPoint b)
        {
            NullPointedSelected();

            var trs = new PainterMesh.Triangle[2];
            var cnt = 0;
            foreach (var tmp in triangles)
            {
                if (!tmp.Includes(a, b)) continue;

                if (cnt == 2) return;
                trs[cnt] = tmp;
                cnt++;
            }
            if (cnt != 2) return;

            var nol0 = trs[0].GetNotOneOf(a, b);
            var nol1 = trs[1].GetNotOneOf(a, b);

            trs[0].Replace(trs[0].GetByVertex(a), nol1);
            trs[1].Replace(trs[1].GetByVertex(b), nol0);

        }

        public void MakeTriangleVertUnique(PainterMesh.Triangle tris, PainterMesh.Vertex pnt)
        {

            if (pnt.triangles.Count == 1) return;

            var nuv = new PainterMesh.Vertex(pnt.meshPoint, pnt);

            tris.Replace(pnt, nuv);

            Dirty = true;

        }

        public bool IsInTriangleSet(PainterMesh.MeshPoint vertex)
        {
            for (var i = 0; i < triVertices; i++)
                if (triangleSet[i].meshPoint == vertex) return true;
            return false;
        }

        public bool IsInTriangleSet(PainterMesh.Vertex uv)
        {
            for (var i = 0; i < triVertices; i++)
                if (triangleSet[i] == uv) return true;
            return false;
        }

        public void NullPointedSelected()
        {
            pointedUv = null;
            pointedLine = null;
            pointedTriangle = null;
            selectedUv = null;
            selectedLine = null;
            selectedTriangle = null;

            triVertices = 0;
        }

        #endregion

        #region Points MGMT

        public void MergeWith(PainterComponent other) => MergeWith(new MeshData(other), other);

        public void MergeWith(MeshData edm, PainterComponent other)
        {

            if (uv2DistributeRow > 1)
            {
                var tile = Vector2.one / uv2DistributeRow;
                var y = uv2DistributeCurrent / uv2DistributeRow;
                var x = uv2DistributeCurrent - y * uv2DistributeRow;
                var offset = tile;
                offset.Scale(new Vector2(x, y));
                edm.TileAndOffsetUVs(offset, tile, 1);
                uv2DistributeCurrent++;
            }

            triangles.AddRange(edm.triangles);

            var tf = other.transform;

            int groupOffset = maxGroupIndex + 1;

            foreach (var point in edm.meshPoints)
            {

                foreach (var vertex in point.vertices)
                {
                    vertex.groupIndex += groupOffset;
                    maxGroupIndex = Mathf.Max(maxGroupIndex, vertex.groupIndex);
                }

                point.WorldPos = tf.TransformPoint(point.localPos);
                meshPoints.Add(point);
            }

        }

        public bool MoveTriangle(PainterMesh.Vertex from, PainterMesh.Vertex to)
        {
            if (from == to) return false;

            foreach (var td in triangles)
                if (td.Includes(from))
                {
                    if (td.Includes(to)) return false;
                    td.Replace(from, to);
                }

            var vp = from.meshPoint;
            vp.vertices.Remove(from);
            if (vp.vertices.Count == 0)
                meshPoints.Remove(vp);

            return true;
        }

        public void GiveLineUniqueVerticesRefreshTriangleListing(PainterMesh.LineData ld)
        {

            var trs = ld.TryGetBothTriangles();

            if (trs.Count != 2) return;

            ld.vertexes[0].meshPoint.smoothNormal = true;
            ld.vertexes[1].meshPoint.smoothNormal = true;

            trs[0].GiveUniqueVerticesAgainst(trs[1]);
            RefreshVertexTriangleList();
        }

        public bool GiveTriangleUniqueVertices(PainterMesh.Triangle triangle)
        {
            var change = false;
            // Mistake here somewhere

            var changed = new PainterMesh.Vertex[3];
            for (var i = 0; i < 3; i++)
            {

                var uvi = triangle.vertexes[i];

                if (uvi.triangles.Count > 1)
                {
                    changed[i] = new PainterMesh.Vertex(uvi);
                    change = true;
                }
                else
                    changed[i] = uvi;

            }
            if (change)
                triangle.Set(changed);

            Dirty |= change;

            return change;
        }

        public void RefreshVertexTriangleList()
        {
            foreach (var vp in meshPoints)
                foreach (var uv in vp.vertices)
                    uv.triangles.Clear();

            foreach (var tr in triangles)
                for (var i = 0; i < 3; i++)
                    tr.vertexes[i].triangles.Add(tr);
        }

        public PainterMesh.Vertex GetUvPointAFromLine(PainterMesh.MeshPoint a, PainterMesh.MeshPoint b)
        {
            foreach (var t in triangles)
                if (t.Includes(a) && t.Includes(b))
                    return t.GetByVertex(a);

            return null;
        }

        public void TagTrianglesUnprocessed()
        {
            foreach (var t in triangles)
                t.wasProcessed = false;
        }

        #endregion

        #region Set All

        public void RunDebug()
        {
            foreach (var t in triangles)
                t.RunDebug();

            foreach (var m in meshPoints)
                m.RunDebug();

            Dirty = true;
        }

        public void TileAndOffsetUVs(Vector2 offs, Vector2 tile, int uvSet)
        {
            foreach (var v in meshPoints)
                foreach (var t in v.sharedUVs)
                    t[uvSet] = Vector2.Scale(t[uvSet], tile) + offs;
        }

        public void RemoveEmptyDots()
        {
            foreach (var v in meshPoints)
                foreach (var uv in v.vertices)
                    uv.hasVertex = false;

            foreach (var t in triangles)
                foreach (var uv in t.vertexes)
                    uv.hasVertex = true;

            foreach (var v in meshPoints)
                for (var i = 0; i < v.vertices.Count; i++)
                    if (!v.vertices[i].hasVertex)
                    {
                        v.vertices.RemoveAt(i);
                        i--;
                    }

            for (var i = 0; i < meshPoints.Count; i++)
                if (meshPoints[i].vertices.Count == 0)
                {
                    meshPoints.RemoveAt(i);
                    i--;
                }

        }

        public void AllVerticesShared()
        {
            foreach (var vp in meshPoints)
                while (vp.vertices.Count > 1)
                    if (!MoveTriangle(vp.vertices[1], vp.vertices[0])) { break; }

        }

        public void AllVerticesSharedIfSameUV()
        {

            foreach (var vp in meshPoints)
                for (var i = 0; i < vp.vertices.Count; i++)
                    for (var j = i + 1; j < vp.vertices.Count; j++)
                    {
                        var a = vp.vertices[i];
                        var b = vp.vertices[j];
                        if (a.uvIndex == b.uvIndex && MoveTriangle(b, a))
                            j--;
                    }
        }

        public int AssignIndexes()
        {
            uvsByFinalIndex.Clear();
            var index = 0;

            foreach (var p in meshPoints)
            {
                foreach (var vertex in p.vertices)
                {
                    vertex.finalIndex = index;
                    uvsByFinalIndex[index] = vertex;
                    index++;
                }
            }
            vertexCount = index;
            return index;
        }

        public void PaintAll(Color c)
        {
            var bm = Painter.Data.Brush.mask;//glob.getBrush().brushMask;
            foreach (var point in meshPoints)
                foreach (var vertex in point.vertices)
                    bm.SetValuesOn(ref vertex.color, c);

            Dirty = true;
        }

        public void SetShadowAll(Color col)
        {
            var bm = Painter.Data.Brush.mask;

            foreach (var v in meshPoints)
                bm.SetValuesOn(ref v.shadowBake, col);

            Dirty = true;
        }

        public int SubMeshIndex
        {
            set
            {
                foreach (var t in triangles)
                    t.subMeshIndex = value;

                Dirty = true;
            }
        }

        public void AfterRemappingTriangleSubMeshIndexes()
        {
            foreach (var t in triangles)
                t.subMeshIndexRemapped = false;
        }

        public bool ChangeSubMeshIndex(int current, int target)
        {

            if (current == target)
                return false;

            var changed = false;

            foreach (var t in triangles)
                if (!t.subMeshIndexRemapped && t.subMeshIndex == current)
                {
                    t.subMeshIndex = target;
                    t.subMeshIndexRemapped = true;
                    changed = true;
                }

            if (changed)
                Dirty = true;

            return changed;
        }

        #endregion

        #region Sculpting Shape

        public void Rescale(float multiplySize, Vector3 center)
        {
            foreach (var vp in meshPoints)
            {
                var diff = vp.localPos - center;
                vp.localPos = center + diff * multiplySize;
            }

        }

        public PainterMesh.MeshPoint InsertIntoLine(PainterMesh.MeshPoint a, PainterMesh.MeshPoint b, Vector3 pos)
        {
            var dstA = Vector3.Distance(pos, a.localPos);
            var dstB = Vector3.Distance(pos, b.localPos);


            var sum = dstA + dstB;

           // float weightA = dstB / sum;
           // float weightB = dstA / sum;

            pos = (a.localPos * dstB + b.localPos * dstA) / sum;

            var newVrt = new PainterMesh.MeshPoint(a, pos);

            meshPoints.Add(newVrt);

            var pointTris = a.FindAllTriangles();

            for (int i = 0; i < pointTris.Count; i++)
            {

                var tr = pointTris[i];

                if (!tr.Includes(b)) continue;

                var auv = tr.GetByVertex(a);
                var buv = tr.GetByVertex(b);
                var splitUv = tr.GetNotOneOf(a, b);


                if (auv == null || buv == null)
                {
                    Debug.LogError("Didn't found a uv");
                    continue;
                }

                //  var uv = (auv.GetUv(0) * weightA + buv.GetUv(0) * weightA);
                // var uv1 = (auv.GetUv(1) * weightA + buv.GetUv(1) * weightA);

                // Vertex newUv = null;

                //               if (Cfg.newVerticesUnique || newVrt.vertices.IsNullOrEmpty())
                //                 newUv = new Vertex(newVrt);
                /*else
                {
                    foreach (var t in newVrt.vertices)
                        if (t.SameUv(uv, uv1)) 
                            newUv = t;
                }*/

                //if (newUv == null)
                PainterMesh.Vertex newUv = new(newVrt);


                tr.AssignWeightedData(newUv, tr.DistanceToWeight(pos));


                var trb = new PainterMesh.Triangle(tr.vertexes).CopySettingsFrom(tr);
                triangles.Add(trb);
                tr.Replace(auv, newUv);

                if (Painter.Data.newVerticesUnique)
                {
                    var split = new PainterMesh.Vertex(splitUv);
                    trb.Replace(splitUv, split);
                    var newB = new PainterMesh.Vertex(newUv);
                    trb.Replace(buv, newB);
                }
                else trb.Replace(buv, newUv);
            }

            Dirty = true;

            if (Painter.Data.pixelPerfectMeshEditing)
                newVrt.PixPerfect();

            return newVrt;
        }

        public void RemoveLine(PainterMesh.LineData ld)
        {
            var a = ld.vertexes[0];
            var b = ld.vertexes[1];

            for (var i = 0; i < triangles.Count; i++)
                if (triangles[i].Includes(a.meshPoint, b.meshPoint))
                {
                    triangles.Remove(triangles[i]);
                    i--;
                }
        }

        public void Displace(Vector3 by)
        {
            foreach (var vp in meshPoints)
                vp.localPos += by;
        }

        public PainterMesh.MeshPoint InsertIntoTriangle(PainterMesh.Triangle triangleA, Vector3 pos)
        {
            var newVrt = new PainterMesh.MeshPoint(triangleA.vertexes[0].meshPoint, pos);

            var weights = triangleA.DistanceToWeight(pos);

            var UV0 = UvSetIndex.From(0);
            var UV1 = UvSetIndex.From(1);

            var VERT0 = VertexIndexInTriangle.From(0);
            var VERT1 = VertexIndexInTriangle.From(1);
            var VERT2 = VertexIndexInTriangle.From(2);

            var newV20 = triangleA[VERT0][UV0] * weights.x + triangleA[VERT1][UV0] * weights.y + triangleA[VERT2][UV0] * weights.z;
            var newV21 = triangleA[VERT0][UV1] * weights.x + triangleA[VERT1][UV1] * weights.y + triangleA[VERT2][UV1] * weights.z;

            var newUv = new PainterMesh.Vertex(newVrt, newV20, newV21);

            triangleA.AssignWeightedData(newUv, weights);

            meshPoints.Add(newVrt);

            var triangleB = new PainterMesh.Triangle(triangleA.vertexes).CopySettingsFrom(triangleA);
            var triandleC = new PainterMesh.Triangle(triangleA.vertexes).CopySettingsFrom(triangleA);

            triangleA.Replace(VERT0, newUv);//uvpnts[0] = newUV;
            triangleB.Replace(VERT1, newUv);// uvpnts[1] = newUV;
            triandleC.Replace(VERT2, newUv);// uvpnts[2] = newUV;

            triangles.Add(triangleB);
            triangles.Add(triandleC);


            if (Painter.Data.pixelPerfectMeshEditing)
                newVrt.PixPerfect();

            Dirty = true;
            return newVrt;
        }

        public PainterMesh.MeshPoint InsertIntoTriangleUniqueVertices(PainterMesh.Triangle a, Vector3 localPos)
        {

            var newVrt = new PainterMesh.MeshPoint(a.vertexes[0].meshPoint, localPos);
            meshPoints.Add(newVrt);

            var newVertexes = new PainterMesh.Vertex[3]; // (newVrt);

            var weights = a.DistanceToWeight(localPos);

            //var newV20 = a.vertexes[0].GetUv(0) * w.x + a.vertexes[1].GetUv(0) * w.y + a.vertexes[2].GetUv(0) * w.z;
            //var newV21 = a.vertexes[0].GetUv(1) * w.x + a.vertexes[1].GetUv(1) * w.y + a.vertexes[2].GetUv(1) * w.z;
            //Color col = a.uvpnts[0]._color * w.x + a.uvpnts[1]._color * w.y + a.uvpnts[2]._color * w.z;
            for (var i = 0; i < 3; i++)
            {
                newVertexes[i] = new PainterMesh.Vertex(newVrt);//, newV20, newV21);
                a.AssignWeightedData(newVertexes[i], weights);
            }

            var b = new PainterMesh.Triangle(a.vertexes).CopySettingsFrom(a);
            var c = new PainterMesh.Triangle(a.vertexes).CopySettingsFrom(a);

            var VERT0 = VertexIndexInTriangle.From(0);
            var VERT1 = VertexIndexInTriangle.From(1);
            var VERT2 = VertexIndexInTriangle.From(2);

            a[VERT0] = newVertexes[0];
            b[VERT1] = newVertexes[1];
            c[VERT2] = newVertexes[2];

            triangles.Add(b);
            triangles.Add(c);

            a.MakeTriangleVertexUnique(a[VERT1]);
            b.MakeTriangleVertexUnique(b[VERT2]);
            c.MakeTriangleVertexUnique(c[VERT0]);


            if (Painter.Data.pixelPerfectMeshEditing)
                newVrt.PixPerfect();

            Dirty = true;
            return newVrt;

        }

        #endregion

        #region Colors
        /*
        public void AutoSetVerticesColorsForEdgeDetection() {

            var asCol = new bool[3];
            var asUv = new bool[3];

            foreach (var td in triangles)
            {
                asCol[0] = asCol[1] = asCol[2] = false;
                asUv[0] = asUv[1] = asUv[2] = false;

                for (var i = 0; i < 3; i++) {
                    var c = td.vertexes[i].GetZeroChanel_AifNotOne();
                    if (c == ColorChanel.A || asCol[(int) c]) continue;
                    asCol[(int)c] = true;
                    asUv[i] = true;
                    td.vertexes[i].SetColor_OppositeTo(c);
                }

                for (var i = 0; i < 3; i++)
                {
                    if (asUv[i]) continue;

                    for (var j = 0; j < 3; j++) {

                        if (asCol[j]) continue;

                        asCol[j] = true;
                        asUv[i] = true;
                        td.vertexes[i].SetColor_OppositeTo((ColorChanel)j);
                        break;

                    }
                }
            }
            Dirty = true;
        }
        */

        public Color Color
        {
            set
            {
                PaintAll(value);
            }
        }

        public bool ColorSubMesh(int submeshIndex, Color col)
        {

            var changed = false;

            foreach (var t in triangles)
                if (t.subMeshIndex == submeshIndex)
                    changed |= t.SetColor(col);

            Dirty |= changed;

            return changed;
        }

        #endregion

        #region Inspector

        void IPEGI.Inspect()
        {
            if ("Run Debug".PegiLabel().Click().Nl())
                RunDebug();

            "{0} points; Avg size {1}; {2} sub Meshes; {3} triangles".F(vertexCount, averageSize, subMeshCount, triangles.Count).PegiLabel().Nl();

            "Bone Weights".PegiLabel().Write(); (gotBoneWeights ? Icon.Done : Icon.Close).Draw(gotBoneWeights ? "Got Bone Weights" : " No Bone Weights");

            if (gotBoneWeights && Icon.Delete.Click("Don't Save Bone Weights"))
                gotBoneWeights = false;

            pegi.Nl();

            var gotBindPos = !bindPoses.IsNullOrEmpty();

            "Bind Positions".PegiLabel().Write(); (gotBindPos ? Icon.Done : Icon.Close).Draw(gotBindPos ? "Got Bind Positions {0}".F(bindPoses.Length) : " No Bind Positions");

            if (gotBindPos && Icon.Delete.Click("Remove Bind Positions"))
                bindPoses = null;

            pegi.Nl();

            if (!shapes.IsNullOrEmpty())
                "Shapes".PegiLabel().Edit_List(shapes).Nl();
        }
        public static MeshData inspected;

        #endregion
    }

    [Serializable]
    public class MarkerWithText
    {
        public GameObject go;
        public TextMesh textm;

        public void Init()
        {
            if (!go) return;

            if (!textm)
                textm = go.GetComponentInChildren<TextMesh>();

            go.hideFlags = HideFlags.DontSave;

            go.SetActive(false);
        }
    }
}