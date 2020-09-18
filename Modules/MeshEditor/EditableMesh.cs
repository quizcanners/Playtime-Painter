using System;
using System.Collections.Generic;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using UnityEngine;

namespace PlaytimePainter.MeshEditing
{
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration


    public class EditableMesh : PainterClassCfg, IPEGI
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

        public List<uint> baseVertex = new List<uint>();

        public Countless<Color> groupColors = new Countless<Color>();

        public List<string> shapes;

        public readonly UnNullable<Countless<float>> blendWeights = new UnNullable<Countless<float>>();

        public Matrix4x4[] bindPoses;

        public List<PainterMesh.MeshPoint> meshPoints = new List<PainterMesh.MeshPoint>();

        public List<PainterMesh.Triangle> triangles = new List<PainterMesh.Triangle>();

        public Mesh actualMesh;

        public float averageSize;

        public void Edit(PlaytimePainter painter)
        {
            if (!painter.SharedMesh) return;

            if (!painter.SavedEditableMesh.IsNullOrEmpty())
            {

                Decode(painter.SavedEditableMesh);
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

            var prf = PainterCamera.Data.meshPackagingSolutions;

            for (var i = 0; i < prf.Count; i++)
                if (prf[i].name.SameAs(name)) painter.selectedMeshProfile = name;

        }

        private void BreakMesh(Mesh mesh)
        {

            if (!mesh)
                return;

            meshName = mesh.name;

            QcSharp.timer.Start("Breaking mesh");

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
                var uv = new PainterMesh.Vertex(meshPoints[i], gotUv1 ? uv1[i] : Vector2.zero, gotUv2 ? uv2[i] : Vector2.zero);
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
                        meshPoints[v].shapes.TryGetLast().Add(new PainterMesh.BlendFrame(pos[v], normals[v], tng[v]));

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

                var mSize = mesh.bounds;

                float coef = 10000f / mesh.bounds.size.magnitude;

                UnNullableLists<PainterMesh.MeshPoint> distanceGroups = new UnNullableLists<PainterMesh.MeshPoint>();

                for (var i = 0; i < vCnt; i++)
                {
                    var p = meshPoints[i];
                    distanceGroups[Mathf.FloorToInt(p.localPos.magnitude * coef)].Add(p);
                }

                var grps = distanceGroups.GetAllObjsNoOrder();

                //  Debug.Log("Got {0} groups".F(grps.Count));

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
                            Merge(i, j);
                            j--;
                            vCnt = meshPoints.Count;
                        }
                    }
                }
            }

            QcSharp.timer.End("Breaking mesh done", 1);

            mesh = new Mesh();

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

        public EditableMesh()
        {

        }

        public EditableMesh(PlaytimePainter painter)
        {
            Edit(painter);
        }

        #region Editing

        public void Merge(int indA, int indB) => Merge(meshPoints[indA], indB);

        public void Merge(PainterMesh.MeshPoint pointA, int indB)
        {

            var pointB = meshPoints[indB];

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

            meshPoints.RemoveAt(indB);
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
                    cody.Add((t as MeshToolBase).StdTag, t.EncodePerMeshData());


            return cody;
        }

        public static EditableMesh decodedEditableMesh;

        public override void Decode(string data)
        {
            decodedEditableMesh = this;

            base.Decode(data);

            decodedEditableMesh = null;
        }

        public override bool Decode(string key, string data)
        {
            switch (key)
            {
                case "vrt": data.Decode_List(out meshPoints); break;
                case "tri": data.Decode_List(out triangles); break;
                case "n": meshName = data; break;
                case "grM": maxGroupIndex = data.ToInt(); break;
                case "sub": subMeshCount = data.ToInt(); break;
                case "wei": gotBoneWeights = data.ToBool(); break;

                case "gcls": data.DecodeInto(out groupColors); break;

                case "bv": data.Decode_List(out baseVertex); break;
                case "biP": data.Decode_Array(out bindPoses); break;
                case "UV2dR": uv2DistributeRow = data.ToInt(); break;
                case "UV2cur": uv2DistributeCurrent = data.ToInt(); break;
                case "sctdUV": selectedUv = meshPoints[data.ToInt()].vertices[0]; break;
                case "sctdTris": selectedTriangle = triangles[data.ToInt()]; break;
                default:
                    if (MeshToolBase.AllTools.IsNullOrEmpty()) return false;

                    foreach (var t in MeshToolBase.allToolsWithPerMeshData)
                    {
                        var mt = t as MeshToolBase;
                        if (mt == null || !mt.StdTag.Equals(key)) continue;
                        mt.Decode(data);
                        return true;
                    }

                    return false;
            }
            return true;
        }
        #endregion

        #region Point & Select

        private Vector3 _previousPointed;
        private float _recalculateDelay;
        private int _nearestVertexCount = 50;
        private float _distanceLimit = 1;
        private const float NearTarget = 64;

        public readonly List<PainterMesh.MeshPoint> _draggedVertices = new List<PainterMesh.MeshPoint>();
        private readonly List<PainterMesh.MeshPoint> _sortVerticesClose = new List<PainterMesh.MeshPoint>();
        private readonly List<PainterMesh.MeshPoint> _sortVerticesFar = new List<PainterMesh.MeshPoint>();
        public CountlessCfg<PainterMesh.Vertex> uvsByFinalIndex = new CountlessCfg<PainterMesh.Vertex>();

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

            if (!EditorInputManager.Control)
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

        public void MergeWith(PlaytimePainter other) => MergeWith(new EditableMesh(other), other);

        public void MergeWith(EditableMesh edm, PlaytimePainter other)
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

            var trs = ld.GetAllTriangles();

            if (trs.Count != 2) return;

            ld.points[0].meshPoint.smoothNormal = true;
            ld.points[1].meshPoint.smoothNormal = true;

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
                foreach (var t in v.sharedV2S)
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
            var bm = Cfg.Brush.mask;//glob.getBrush().brushMask;
            foreach (var point in meshPoints)
                foreach (var vertex in point.vertices)
                    bm.SetValuesOn(ref vertex.color, c);

            Dirty = true;
        }

        public void SetShadowAll(Color col)
        {
            var bm = Cfg.Brush.mask;

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

            float weightA = dstB / sum;
            float weightB = dstA / sum;

            pos = (a.localPos * dstB + b.localPos * dstA) / sum;

            var newVrt = new PainterMesh.MeshPoint(a, pos);

            meshPoints.Add(newVrt);

            var pointTris = a.Triangles();

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
                PainterMesh.Vertex newUv = new PainterMesh.Vertex(newVrt);


                tr.AssignWeightedData(newUv, tr.DistanceToWeight(pos));


                var trb = new PainterMesh.Triangle(tr.vertexes).CopySettingsFrom(tr);
                triangles.Add(trb);
                tr.Replace(auv, newUv);

                if (Cfg.newVerticesUnique)
                {
                    var split = new PainterMesh.Vertex(splitUv);
                    trb.Replace(splitUv, split);
                    var newB = new PainterMesh.Vertex(newUv);
                    trb.Replace(buv, newB);
                }
                else trb.Replace(buv, newUv);
            }

            Dirty = true;

            if (Cfg.pixelPerfectMeshEditing)
                newVrt.PixPerfect();

            return newVrt;
        }

        public void RemoveLine(PainterMesh.LineData ld)
        {
            var a = ld.points[0];
            var b = ld.points[1];

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

        public PainterMesh.MeshPoint InsertIntoTriangle(PainterMesh.Triangle a, Vector3 pos)
        {
            // Debug.Log("Inserting into triangle");
            var newVrt = new PainterMesh.MeshPoint(a.vertexes[0].meshPoint, pos);

            var w = a.DistanceToWeight(pos);

            var newV20 = a.vertexes[0].GetUv(0) * w.x + a.vertexes[1].GetUv(0) * w.y + a.vertexes[2].GetUv(0) * w.z;
            var newV21 = a.vertexes[0].GetUv(1) * w.x + a.vertexes[1].GetUv(1) * w.y + a.vertexes[2].GetUv(1) * w.z;

            var newUv = new PainterMesh.Vertex(newVrt, newV20, newV21);

            a.AssignWeightedData(newUv, w);

            meshPoints.Add(newVrt);

            var b = new PainterMesh.Triangle(a.vertexes).CopySettingsFrom(a);
            var c = new PainterMesh.Triangle(a.vertexes).CopySettingsFrom(a);

            a.Replace(0, newUv);//uvpnts[0] = newUV;
            b.Replace(1, newUv);// uvpnts[1] = newUV;
            c.Replace(2, newUv);// uvpnts[2] = newUV;

            triangles.Add(b);
            triangles.Add(c);


            if (Cfg.pixelPerfectMeshEditing)
                newVrt.PixPerfect();

            Dirty = true;
            return newVrt;
        }

        public PainterMesh.MeshPoint InsertIntoTriangleUniqueVertices(PainterMesh.Triangle a, Vector3 localPos)
        {

            var newVrt = new PainterMesh.MeshPoint(a.vertexes[0].meshPoint, localPos);
            meshPoints.Add(newVrt);

            var newUv = new PainterMesh.Vertex[3]; // (newVrt);

            var w = a.DistanceToWeight(localPos);

            //var newV20 = a.vertexes[0].GetUv(0) * w.x + a.vertexes[1].GetUv(0) * w.y + a.vertexes[2].GetUv(0) * w.z;
            //var newV21 = a.vertexes[0].GetUv(1) * w.x + a.vertexes[1].GetUv(1) * w.y + a.vertexes[2].GetUv(1) * w.z;
            //Color col = a.uvpnts[0]._color * w.x + a.uvpnts[1]._color * w.y + a.uvpnts[2]._color * w.z;
            for (var i = 0; i < 3; i++)
            {
                newUv[i] = new PainterMesh.Vertex(newVrt);//, newV20, newV21);
                a.AssignWeightedData(newUv[i], w);
            }

            var b = new PainterMesh.Triangle(a.vertexes).CopySettingsFrom(a);
            var c = new PainterMesh.Triangle(a.vertexes).CopySettingsFrom(a);

            a.vertexes[0] = newUv[0];
            b.vertexes[1] = newUv[1];
            c.vertexes[2] = newUv[2];

            triangles.Add(b);
            triangles.Add(c);

            a.MakeTriangleVertexUnique(a.vertexes[1]);
            b.MakeTriangleVertexUnique(b.vertexes[2]);
            c.MakeTriangleVertexUnique(c.vertexes[0]);


            if (Cfg.pixelPerfectMeshEditing)
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

        public bool ColorSubMesh(int index, Color col)
        {

            var changed = false;

            foreach (var t in triangles)
                if (t.subMeshIndex == index)
                    changed |= t.SetColor(col);

            Dirty |= changed;

            return changed;
        }

        #endregion

        #region Inspector

        public bool Inspect()
        {
            var changed = false;

            if ("Run Debug".Click().nl(ref changed))
                RunDebug();

            "{0} points; Avg size {1}; {2} sub Meshes; {3} triangles".F(vertexCount, averageSize, subMeshCount, triangles.Count).nl();

            "Bone Weights".write(); (gotBoneWeights ? icon.Done : icon.Close).write(gotBoneWeights ? "Got Bone Weights" : " No Bone Weights");

            if (gotBoneWeights && icon.Delete.Click("Don't Save Bone Weights", ref changed))
                gotBoneWeights = false;

            pegi.nl();

            var gotBindPos = !bindPoses.IsNullOrEmpty();

            "Bind Positions".write(); (gotBindPos ? icon.Done : icon.Close).write(gotBindPos ? "Got Bind Positions {0}".F(bindPoses.Length) : " No Bind Positions");

            if (gotBindPos && icon.Delete.Click("Remove Bind Positions", ref changed))
                bindPoses = null;

            pegi.nl();

            if (!shapes.IsNullOrEmpty())
                "Shapes".edit_List(ref shapes).nl(ref changed);

            return changed;
        }
        public static EditableMesh inspected;

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