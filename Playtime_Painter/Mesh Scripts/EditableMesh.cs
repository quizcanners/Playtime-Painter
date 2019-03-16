using UnityEngine;
using System.Collections.Generic;
using System;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace Playtime_Painter {

    public class EditableMesh : PainterSystemCfg, IPEGI {

        public string meshName = "unnamed";

        public bool Dirty {
            get {
                return  dirtyColor || dirtyVertexIndexes || dirtyPosition;// || dirtyUvs;
            } set {
                dirtyVertexIndexes = value;
                dirtyColor = value;
                dirtyPosition = value;
                //dirtyUvs = value;
            }
        }
        public bool dirtyPosition;
        public bool dirtyColor;
        public bool dirtyVertexIndexes;
       // public bool dirtyUvs;
        public int vertexCount;

        public bool firstBuildRun;
        public bool gotBoneWeights;
        public int subMeshCount;
        public int uv2DistributeRow;
        public int uv2DistributeCurrent;

        public List<uint> baseVertex = new List<uint>();

        public List<string> shapes;

        public readonly UnNullable<Countless<float>> blendWeights = new UnNullable<Countless<float>>();

        public Matrix4x4[] bindPoses;

        public List<MeshPoint> meshPoints = new List<MeshPoint>();

        public List<Triangle> triangles = new List<Triangle>();
        
        public Mesh actualMesh;

        public float averageSize;

        public void Edit(PlaytimePainter painter)
        {
            if (!painter.SharedMesh) return;

            if (painter.SavedEditableMesh != null)
            {
                Decode(painter.SavedEditableMesh);
                if (triangles.Count == 0)
                    BreakMesh(painter.SharedMesh);

            }
            else
            {
                BreakMesh(painter.SharedMesh);
                painter.selectedMeshProfile = painter.Material.GetMeshProfileByTag();
            }

        }

        private void BreakMesh(Mesh nMesh)
        {

            if (!nMesh)
                return;

            meshName = nMesh.name;

            actualMesh = nMesh;

            var vCnt = actualMesh.vertices.Length;

            meshPoints = new List<MeshPoint>();
            var vertices = actualMesh.vertices;
            var gotUv1 = (actualMesh.uv != null) && (actualMesh.uv.Length == vCnt);
            var gotUv2 = (actualMesh.uv2 != null) && (actualMesh.uv2.Length == vCnt);
            var gotColors = (actualMesh.colors != null) && (actualMesh.colors.Length == vCnt);
            gotBoneWeights = (actualMesh.boneWeights != null) && (actualMesh.boneWeights.Length == vCnt);
            bindPoses = actualMesh.bindposes;

            for (var i = 0; i < vCnt; i++)
            {
                var v = new MeshPoint(vertices[i])
                {
                    smoothNormal = false
                };
                meshPoints.Add(v);
                var uv = new Vertex(meshPoints[i], gotUv1 ? actualMesh.uv[i] : Vector2.zero, gotUv2 ? actualMesh.uv2[i] : Vector2.zero);
                if (gotColors)
                    uv.color = actualMesh.colors[i];
                if (gotBoneWeights)
                    v.boneWeight = actualMesh.boneWeights[i];
            }

            shapes = new List<string>();

            for (var s = 0; s < actualMesh.blendShapeCount; s++)
            {

                for (var v = 0; v < vCnt; v++)
                    meshPoints[v].shapes.Add(new List<BlendFrame>());

                shapes.Add(actualMesh.GetBlendShapeName(s));

                for (var f = 0; f < actualMesh.GetBlendShapeFrameCount(s); f++)
                {

                    blendWeights[s][f] = actualMesh.GetBlendShapeFrameWeight(s, f);

                    var normals = new Vector3[vCnt];
                    var pos = new Vector3[vCnt];
                    var tng = new Vector3[vCnt];
                    actualMesh.GetBlendShapeFrameVertices(s, f, pos, normals, tng);

                    for (var v = 0; v < vCnt; v++)
                        meshPoints[v].shapes.Last().Add(new BlendFrame(pos[v], normals[v], tng[v]));

                }
            }

            triangles = new List<Triangle>();
            var points = new Vertex[3];

            subMeshCount = actualMesh.subMeshCount;
            baseVertex = new List<uint>();

            for (var s = 0; s < subMeshCount; s++)
            {

                baseVertex.Add( actualMesh.GetBaseVertex(s)  );

                var indices = actualMesh.GetTriangles(s);

                var tCnt = indices.Length / 3;

                for (var i = 0; i < tCnt; i++)
                {

                    for (var e = 0; e < 3; e++)
                        points[e] = meshPoints[indices[i * 3 + e]].vertices[0];

                    var t = new Triangle(points)
                    {
                        subMeshIndex = s
                    };
                    triangles.Add(t);
                }
            }

            for (var i = 0; i < vCnt; i++) {

                var main = meshPoints[i];
                for (var j = i + 1; j < vCnt; j++)
                {
                    if (!((meshPoints[j].localPos - main.localPos).magnitude < float.Epsilon)) continue;
                    
                    Merge(i, j);
                    j--;
                    vCnt = meshPoints.Count;

                }
            }

            actualMesh = new Mesh();

            Dirty = true;
        }

        public EditableMesh()
        {

        }

        public EditableMesh(PlaytimePainter painter)
        {
            Edit(painter);
        }

        #region Editing
        public void Merge(MeshPoint pointA, MeshPoint pointB) => Merge(pointA, meshPoints.IndexOf(pointB));
        
        public void Merge(int indA, int indB) => Merge(meshPoints[indA], indB);
        
        public void Merge(MeshPoint pointA, int indB) {
          
            var pointB = meshPoints[indB];

            if (pointA == pointB)
                return;

            foreach (var buv in pointB.vertices)
            {
                var uvs = new Vector2[] { buv.GetUv(0), buv.GetUv(1) };
                pointA.vertices.Add(buv);
                buv.meshPoint = pointA;
                buv.SetUvIndexBy(uvs);
            }

            meshPoints.RemoveAt(indB);
        }

        public bool SetAllUVsShared(MeshPoint pnt)
        {
            if (pnt.vertices.Count == 1)
                return false;

            while (pnt.vertices.Count > 1)
                if (!MoveTriangle(pnt.vertices[1], pnt.vertices[0])) break;
                
            Dirty = true;
            return true;
        }

        public bool SetAllVerticesShared(Triangle tri)
        {
            var changed = false;

            for (var i = 0; i < 3; i++)
                changed |= SetAllUVsShared(tri[i]);

            if (changed)
                Dirty = true;

            return changed;
        }

        public bool AllVerticesShared(LineData ld)
        {
            var changed = false;
            for (var i = 0; i < 2; i++)
                changed |= SetAllUVsShared(ld[i]);

            return changed;
        }
        
        public void DeleteUv(Vertex uv)
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

        public void DeleteLine(LineData ld)
        {
            NullPointedSelected();

            RemoveLine(ld);
        }

        #endregion

        #region Encode & Decode
        public override StdEncoder Encode() {
            var cody = new StdEncoder()
            .Add_String("n", meshName)
            .Add_IfNotEmpty("vrt",meshPoints)
            .Add_IfNotEmpty("tri",triangles)
            .Add("sub", subMeshCount)
            .Add_IfTrue("wei", gotBoneWeights)
            .Add("bv", baseVertex)
            .Add("biP", bindPoses);

            if (uv2DistributeRow > 0) {
                cody.Add("UV2dR", uv2DistributeRow);
                cody.Add("UV2cur", uv2DistributeCurrent);
            }
            if (selectedUv != null)
                cody.Add("sctdUV", meshPoints.IndexOf(selectedUv.meshPoint));
            if (selectedTriangle != null)
                cody.Add("sctdTris", triangles.IndexOf(selectedTriangle));

            if (!MeshToolBase.AllTools.IsNullOrEmpty())
                foreach (var t in MeshToolBase.allToolsWithPerMeshData)
                    cody.Add((t as MeshToolBase).stdTag, t.EncodePerMeshData());


            return cody;
        }

        public static EditableMesh decodedEditableMesh;

        public override void Decode(string data)
        {
            decodedEditableMesh = this;

            base.Decode(data);

            decodedEditableMesh = null;
        }

        public override bool Decode(string tg, string data) {
            switch (tg) {
                case "vrt":  data.Decode_List(out meshPoints); break;
                case "tri": data.Decode_List(out triangles); break;
                case "n": meshName = data; break;
                case "sub":  subMeshCount = data.ToInt(); break;
                case "wei": gotBoneWeights = data.ToBool(); break;

                case "bv": data.Decode_List(out baseVertex); break;
                case "biP": data.Decode_Array(out bindPoses);  break;
                case "UV2dR": uv2DistributeRow = data.ToInt(); break;
                case "UV2cur": uv2DistributeCurrent = data.ToInt(); break;
                case "sctdUV": selectedUv = meshPoints[data.ToInt()].vertices[0]; break;
                case "sctdTris": selectedTriangle = triangles[data.ToInt()]; break;
                default:
                    if (MeshToolBase.AllTools.IsNullOrEmpty()) return false;

                    foreach (var t in MeshToolBase.allToolsWithPerMeshData){
                        var mt = t as MeshToolBase;
                        if (mt == null || !mt.stdTag.Equals(tg)) continue;
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

        private readonly List<MeshPoint> _sortVerticesClose = new List<MeshPoint>();
        private readonly List<MeshPoint> _sortVerticesFar = new List<MeshPoint>();
        public CountlessCfg<Vertex> uvsByFinalIndex = new CountlessCfg<Vertex>();
        
        public Vertex selectedUv;

        public LineData selectedLine;

        public Triangle selectedTriangle;

        public Vertex pointedUv;

        public LineData pointedLine;

        public Triangle pointedTriangle;

        public Vertex lastFramePointedUv;

        public LineData lastFramePointedLine;

        public Triangle lastFramePointedTriangle;

        public MeshPoint PointedVertex => pointedUv?.meshPoint;

        public MeshPoint GetClosestToPos(Vector3 pos)
        {
            MeshPoint closest = null;
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

        public void SetLastPointed (Vertex uv)
        {
            ClearLastPointed();
            lastFramePointedUv = uv;
        }

        public void SetLastPointed(LineData l)
        {
            ClearLastPointed();
            lastFramePointedLine = l;
        }

        public void SetLastPointed(Triangle t)
        {
            ClearLastPointed();
            lastFramePointedTriangle = t;
        }
        
        public Vertex[] triangleSet = new Vertex[3];

        public int triVertices;

        public void SortAround(Vector3 center, bool forceRecalculate)  {
         
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

			if (meshPoints.Count > NearTarget*1.5f) {

				if (recalculated){ 
				_distanceLimit += Mathf.Max(-_distanceLimit*0.5f, ((NearTarget  - near)/NearTarget)*_distanceLimit);
				
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
                    
					_distanceLimit += Mathf.Max(-_distanceLimit*0.5f,  (NearTarget - _sortVerticesClose.Count)*_distanceLimit/NearTarget);

				}
                
                for (var j = 0; j < 25; j++) {
					var changed = false;
					for (var i = 0; i < _nearestVertexCount-1; i++) {
                        var a = meshPoints[i];
                        var b = meshPoints[i + 1];

                        if (!(a.distanceToPointed > b.distanceToPointed)) continue;
                        
                        meshPoints[i] = b;
                        meshPoints[i + 1] = a;
                        changed = true;
                    }
					if (!changed)
						j = 999;

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

        public void AddToTrisSet(Vertex nuv)
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

            var td = new Triangle(triangleSet);

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

        public void SwapLine(MeshPoint a, MeshPoint b)
        {
            NullPointedSelected();

            var trs = new Triangle[2];
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


        public void MakeTriangleVertUnique(Triangle tris, Vertex pnt)
        {

            if (pnt.triangles.Count == 1) return;

            var nuv = new Vertex(pnt.meshPoint, pnt);

            tris.Replace(pnt, nuv);

            Dirty = true;

        }

        public bool IsInTriangleSet(MeshPoint vertex)
        {
            for (var i = 0; i < triVertices; i++)
                if (triangleSet[i].meshPoint == vertex) return true;
            return false;
        }

        public bool IsInTriangleSet(Vertex uv)
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

            foreach (var v in edm.meshPoints)
            {
                v.WorldPos = tf.TransformPoint(v.localPos);
                meshPoints.Add(v);
            }

        }

        public bool MoveTriangle(Vertex from, Vertex to)
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
        
        public void GiveLineUniqueVerticesRefreshTriangleListing(LineData ld)
        {

            var trs = ld.GetAllTriangles();

            if (trs.Count != 2) return;

            ld.points[0].meshPoint.smoothNormal = true;
            ld.points[1].meshPoint.smoothNormal = true;

            trs[0].GiveUniqueVerticesAgainst(trs[1]);
            RefreshVertexTriangleList();
        }

        public bool GiveTriangleUniqueVertices(Triangle triangle)
        {
            var change = false;
            // Mistake here somewhere

            var changed = new Vertex[3];
            for (var i = 0; i < 3; i++)
            {

                var uvi = triangle.vertexes[i];
        
                if (uvi.triangles.Count > 1) //count > 1)
                {
                    changed[i] = new Vertex(uvi);
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

        public Vertex GetUvPointAFromLine(MeshPoint a, MeshPoint b)
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
        
        public void AllVerticesShared() {
            foreach (var vp in meshPoints)
                while (vp.vertices.Count > 1)
                    if (!MoveTriangle(vp.vertices[1], vp.vertices[0])) { break; }

        }

        public void AllVerticesSharedIfSameUV() {

            foreach (var vp in meshPoints)
                for (var i=0; i<vp.vertices.Count; i++)
                    for (var j = i + 1; j < vp.vertices.Count; j++) {
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
        
        public void PaintAll(LinearColor col)
        {
            var bm = Cfg.brushConfig.mask;//glob.getBrush().brushMask;
            var c = col.ToGamma();
            foreach (var point in meshPoints)
                foreach (var vertex in point.vertices)
                   bm.Transfer(ref vertex.color, c);

            Dirty = true;
        }

        public void SetShadowAll(LinearColor col)
        {
            var bm = Cfg.brushConfig.mask;//glob.getBrush().brushMask;
            var c = col.ToGamma();

            foreach (var v in meshPoints)
                bm.Transfer(ref v.shadowBake, c); 
               
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

        public bool ChangeSubMeshIndex (int current, int target) {

            if (current == target)
                return false;

            var changed = false;

            foreach (var t in triangles)
                if (!t.subMeshIndexRemapped && t.subMeshIndex == current)  {
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
        
        public MeshPoint InsertIntoLine(MeshPoint a, MeshPoint b, Vector3 pos)
        {
            var dstA = Vector3.Distance(pos, a.localPos);
            var dstB = Vector3.Distance(pos, b.localPos);
            var sum = dstA + dstB;
            pos = (a.localPos * dstB + b.localPos * dstA) / sum;

            var newVrt = new MeshPoint(pos);

            meshPoints.Add(newVrt);

            var triangles = a.Triangles();

            foreach (var tr in triangles)
            {
                if (!tr.Includes(b)) continue;

                var auv = tr.GetByVertex(a);
                var buv = tr.GetByVertex(b);
                var splitUv = tr.GetNotOneOf(a, b);


                if (auv == null || buv == null)
                {
                    Debug.Log("Didn't found a uv");
                    continue;
                }

                var w = new Vector3();

                var dst = dstA + dstB;
                w[tr.NumberOf(auv)] = dstB / dst;
                w[tr.NumberOf(buv)] = dstA / dst;

                var uv = (auv.GetUv(0) * dstB + buv.GetUv(0) * dstA) / (dstA + dstB);
                var uv1 = (auv.GetUv(1) * dstB + buv.GetUv(1) * dstA) / (dstA + dstB);


                Vertex newUv = null;

                if (Cfg.newVerticesUnique || newVrt.vertices.IsNullOrEmpty())
                    newUv = new Vertex(newVrt, uv, uv1);
                else
                {
                    foreach (var t in newVrt.vertices)
                        if (t.SameUv(uv, uv1)) 
                            newUv = t;
                }

                if (newUv == null)
                    newUv = new Vertex(newVrt, uv, uv1);
                else
                    newUv.SetUvIndexBy(uv, uv1);
                tr.AssignWeightedData(newUv, w);


                var trb = new Triangle(tr.vertexes).CopySettingsFrom(tr);
                triangles.Add(trb);
                tr.Replace(auv, newUv);
                
                if (Cfg.newVerticesUnique)
                {
                    var split = new Vertex(splitUv);
                    trb.Replace(splitUv, split);
                    var newB = new Vertex(newUv);
                    trb.Replace(buv, newB);
                }
                else trb.Replace(buv, newUv);
            }

            Dirty = true;

            if (Cfg.pixelPerfectMeshEditing)
                newVrt.PixPerfect();

            return newVrt;
        }

        public void RemoveLine(LineData ld)
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
        
        public MeshPoint InsertIntoTriangle(Triangle a, Vector3 pos)
        {
            // Debug.Log("Inserting into triangle");
            var newVrt = new MeshPoint(pos);

            var w = a.DistanceToWeight(pos);

            var newV20 = a.vertexes[0].GetUv(0) * w.x + a.vertexes[1].GetUv(0) * w.y + a.vertexes[2].GetUv(0) * w.z;
            var newV21 = a.vertexes[0].GetUv(1) * w.x + a.vertexes[1].GetUv(1) * w.y + a.vertexes[2].GetUv(1) * w.z;

            var newUv = new Vertex(newVrt, newV20, newV21);

            a.AssignWeightedData(newUv, w);

            meshPoints.Add(newVrt);

            var b = new Triangle(a.vertexes).CopySettingsFrom(a);
            var c = new Triangle(a.vertexes).CopySettingsFrom(a);

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

        public MeshPoint InsertIntoTriangleUniqueVertices(Triangle a, Vector3 localPos)
        {

            var newVrt = new MeshPoint(localPos);
            meshPoints.Add(newVrt);

            var newUv = new Vertex[3]; // (newVrt);

            var w = a.DistanceToWeight(localPos);

            var newV20 = a.vertexes[0].GetUv(0) * w.x + a.vertexes[1].GetUv(0) * w.y + a.vertexes[2].GetUv(0) * w.z;
            var newV21 = a.vertexes[0].GetUv(1) * w.x + a.vertexes[1].GetUv(1) * w.y + a.vertexes[2].GetUv(1) * w.z;
            //Color col = a.uvpnts[0]._color * w.x + a.uvpnts[1]._color * w.y + a.uvpnts[2]._color * w.z;
            for (var i = 0; i < 3; i++)
            {
                newUv[i] = new Vertex(newVrt, newV20, newV21);
                a.AssignWeightedData(newUv[i], w);
            }

            var b = new Triangle(a.vertexes).CopySettingsFrom(a);
            var c = new Triangle(a.vertexes).CopySettingsFrom(a);

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
                PaintAll(new LinearColor(value));
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
        #if PEGI
        public bool Inspect()
        {
            var changed = false;

            if ("Run Debug".Click().nl(ref changed))
                RunDebug();

            "{0} points; Avg size {1}; {2} sub Meshes; {3} triangles".F(vertexCount, averageSize, subMeshCount, triangles.Count).nl();

            "Bone Weights".write(); (gotBoneWeights ? icon.Done : icon.Close).write(gotBoneWeights ? "Got Bone Weights" : " No Bone Weights" );

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
        #endif
        #endregion
    }

    [Serializable]
    public class MarkerWithText
    {
        public GameObject go;
        public TextMesh textm;

        public void Init() {
            if (!go) return;

            if (!textm)
                textm = go.GetComponentInChildren<TextMesh>();

            go.hideFlags = HideFlags.DontSave;

            go.SetActive(false);
        }
    }
}