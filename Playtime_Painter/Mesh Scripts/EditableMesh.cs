using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
#if UNITY_EDITOR
using UnityEditor;
#endif



namespace Playtime_Painter {

    public class EditableMesh : PainterStuff_STD, IPEGI {

        public bool Dirty {
            get {
                return  dirty_Color || dirty_Normals || dirty_Position;
            } set {
                dirty_Color = value;
                dirty_Normals = value;
                dirty_Position = value;
            }
        }
        public bool dirty_Vertices;
        public bool dirty_Position;
        public bool dirty_Color;
        public bool dirty_Normals;
        public int vertexCount;

        public bool gotBoneWeights;
        public bool gotBindPos;
        public int submeshCount;
        public int UV2distributeRow;
        public int UV2distributeCurrent;
        public List<uint> baseVertex = new List<uint>();

        public List<string> shapes;
        public Countless<Countless<float>> blendWeights = new Countless<Countless<float>>();

        public string meshName = "unnamed";
        public List<MeshPoint> meshPoints = new List<MeshPoint>();
        public List<Triangle> triangles = new List<Triangle>();
        
        public CountlessBool hasFrame = new CountlessBool();

        public void RunDebug()
        {
            foreach (var t in triangles)
                t.RunDebug();

            foreach (var m in meshPoints)
                m.RunDebug();

            Dirty = true;
        }

        #region Encode & Decode
        public override StdEncoder Encode() {
            var cody = new StdEncoder();
            cody.Add_String("n", meshName);
            cody.Add_IfNotEmpty("vrt",meshPoints);
            cody.Add_IfNotEmpty("tri",triangles);
            cody.Add("sub", submeshCount);
            cody.Add_IfTrue("wei", gotBoneWeights);
            cody.Add_IfTrue("bp", gotBindPos);
            cody.Add("bv", baseVertex);
            if (UV2distributeRow > 0) {
                cody.Add("UV2dR", UV2distributeRow);
                cody.Add("UV2cur", UV2distributeCurrent);
            }
            if (selectedUV != null)
                cody.Add("sctdUV", meshPoints.IndexOf(selectedUV.meshPoint));
            if (selectedTris != null)
                cody.Add("sctdTris", triangles.IndexOf(selectedTris));
            return cody;
        }

        public override bool Decode(string tag, string data) {
            switch (tag) {
                case "vrt":  data.Decode_List(out meshPoints); break;
                case "tri": data.Decode_List(out triangles); break;
                case "n": meshName = data; break;
                case "sub":  submeshCount = data.ToInt(); break;
                case "wei": gotBoneWeights = data.ToBool(); break;
                case "bp": gotBindPos = data.ToBool(); break;
                case "bv": data.Decode_List(out baseVertex); break;
                case "UV2dR": UV2distributeRow = data.ToInt(); break;
                case "UV2cur": UV2distributeCurrent = data.ToInt(); break;
                case "sctdUV": selectedUV = meshPoints[data.ToInt()].uvpoints[0]; break;
                case "sctdTris": selectedTris = triangles[data.ToInt()]; break;
                default: return false;
            }
            return true;
        }
        #endregion

        public Mesh actualMesh;

        public float avarageSize;

        public void RemoveEmptyDots()
        {
            foreach (var v in meshPoints)
                foreach (var uv in v.uvpoints)
                    uv.HasVertex = false;
                
            foreach (var t in triangles)
                foreach (var uv in t.vertexes)
                    uv.HasVertex = true; 
            
            foreach (var v in meshPoints)
                for (int i = 0; i < v.uvpoints.Count; i++)
                    if (!v.uvpoints[i].HasVertex)  {
                        v.uvpoints.RemoveAt(i);
                        i--;
                    }
            
            for (int i = 0; i < meshPoints.Count; i++)
                if (meshPoints[i].uvpoints.Count == 0) {
                    meshPoints.RemoveAt(i);
                    i--;
                }
            
        }

        public MeshPoint GetClosestToPos(Vector3 pos)
        {
            MeshPoint closest = null;
            float dist = 0;
            foreach (MeshPoint v in meshPoints)
            {
                //if (v.pos.SameAs(pos))
                float newDist = Vector3.Distance(v.localPos, pos);
                if ((closest == null) || (dist > newDist))
                {
                    dist = newDist;
                    closest = v;
                }
                //	return v; 
            }
            return closest;
        }

        public int AssignIndexes() {
            uvsByFinalIndex.Clear();
            int index = 0;
            for (int i = 0; i < meshPoints.Count; i++) {
                for (int u = 0; u < meshPoints[i].uvpoints.Count; u++) {
                    meshPoints[i].uvpoints[u].finalIndex = index;
                    uvsByFinalIndex[index] = meshPoints[i].uvpoints[u];
                    index++;
                }
            }
            vertexCount = index;
            return index;
        }

		public void TileAndOffsetUVs(Vector2 offs, Vector2 tile, int uvSet){
			foreach (var v in meshPoints)
				for (int i = 0; i < v.shared_v2s.Count; i++) 
					v.shared_v2s[i][uvSet] = Vector2.Scale (v.shared_v2s [i][uvSet], tile) + offs;
		}

        public void AddTextureAnimDisplacement()
        {
            MeshManager m = MeshManager.Inst;
            if (m.target != null)
            {
                PlaytimePainter em = m.target;
                if (!em) return;
                int y = em.GetAnimationUVy();
                foreach (MeshPoint v in meshPoints)
                    v.localPos += (v.anim[y]);
            }
        }

        Vector3 previousPointed = new Vector3();
        float recalculateDelay;
		int nearestVertCount = 50;
        public float distanceLimit = 1;
		static readonly float nearTarget = 64;

        List<MeshPoint> sortVertsClose = new List<MeshPoint>();
        List<MeshPoint> sortVertsFar = new List<MeshPoint>();
        public CountlessSTD<Vertex> uvsByFinalIndex = new CountlessSTD<Vertex>();
        
        public Vertex selectedUV;
        
        public LineData selectedLine;
    
        public Triangle selectedTris;
    
        public Vertex pointedUV;

        public LineData pointedLine;

        public Triangle pointedTris;

        public Vertex LastFramePointedUV;

        public LineData LastFramePointedLine;

        public Triangle LastFramePointedTris;

        public void ClearLastPointed()
        {
            LastFramePointedUV = null;
            LastFramePointedLine = null;
            LastFramePointedTris = null;
        }

        public void SetLastPointed (Vertex uv)
        {
            ClearLastPointed();
            LastFramePointedUV = uv;
        }

        public void SetLastPointed(LineData l)
        {
            ClearLastPointed();
            LastFramePointedLine = l;
        }

        public void SetLastPointed(Triangle t)
        {
            ClearLastPointed();
            LastFramePointedTris = t;
        }
        
        public Vertex[] TrisSet = new Vertex[3];

        public int trisVerts;

        public void SortAround(Vector3 center, bool forceRecalculate)  {
         
            distanceLimit = Mathf.Max(1, distanceLimit * (1f - Time.deltaTime));
			if (distanceLimit == 0)
				distanceLimit = 0.1f;

            recalculateDelay -= Time.deltaTime;

			bool recalculated = false;
			int near = 0;
			if (((center - previousPointed).magnitude > distanceLimit * 0.05f) || (recalculateDelay < 0) || (forceRecalculate)) {
				
				for (int i = 0; i < meshPoints.Count; i++) {
					meshPoints [i].distanceToPointedV3 = (center - meshPoints [i].localPos);
					meshPoints [i].distanceToPointed = meshPoints [i].distanceToPointedV3.magnitude;
					if (meshPoints [i].distanceToPointed < distanceLimit)
						near++;
				}
				recalculated = true;
			}

			if (meshPoints.Count > nearTarget*1.5f) {

				if (recalculated){ 
				distanceLimit += Mathf.Max(-distanceLimit*0.5f, ((float)(nearTarget  - near)/nearTarget)*distanceLimit);
				
                previousPointed = center;
                recalculateDelay = 1;

                    sortVertsClose.Clear();
                    sortVertsFar.Clear();

                    foreach (MeshPoint v in meshPoints)
                        if (v.distanceToPointed > distanceLimit)
                            sortVertsFar.Add(v);
                        else
                            sortVertsClose.Add(v);

					nearestVertCount = sortVertsClose.Count;

                    meshPoints.Clear();
                    meshPoints.AddRange(sortVertsClose);
                    meshPoints.AddRange(sortVertsFar);
                    
					distanceLimit += Mathf.Max(-distanceLimit*0.5f,  ((float)(nearTarget - sortVertsClose.Count))*distanceLimit/nearTarget);

				}
                
                for (int j = 0; j < 25; j++) {
					bool changed = false;
					for (int i = 0; i < nearestVertCount-1; i++) {
                        MeshPoint a = meshPoints[i];
                        MeshPoint b = meshPoints[i + 1];

                        if (a.distanceToPointed > b.distanceToPointed) {
                            meshPoints[i] = b;
                            meshPoints[i + 1] = a;
							changed = true;
                        }
                    }
					if (!changed)
						j = 999;

                }
            }
            else
            {
                meshPoints.Sort(delegate (MeshPoint a, MeshPoint b)
                {
                    return b.distanceToPointed < a.distanceToPointed ? 1 : ((b.distanceToPointed - 0.0001f) < a.distanceToPointed ? 0 : -1);

                }
                );

                return;
            }

        }

        public void NumberVerticles()
        {
            for (int i = 0; i < meshPoints.Count; i++)
                meshPoints[i].index = i; //< 128 ? i : 0;
                                       // Debug.Log("Dirty");
                                       // Dirty = true;

        }

        public void Edit(PlaytimePainter pntr) {

            if (pntr.meshFilter && pntr.meshFilter.sharedMesh){

                //Temporary
                submeshCount = 1;
                if (pntr.SavedEditableMesh != null)
                {
                    Decode(pntr.SavedEditableMesh);
                    if (triangles.Count == 0)
                        BreakMesh(pntr.meshFilter.sharedMesh);

                }
                else
                {
                    BreakMesh(pntr.meshFilter.sharedMesh);
                    pntr.selectedMeshProfile = pntr.Material.GetMeshProfileByTag();
                }

                // Temporary
                while (baseVertex.Count < submeshCount)
                    baseVertex.Add(0);
            }
        }

        public void BreakMesh(Mesh Nmesh) {

            if (!Nmesh)
                return;

            meshName = Nmesh.name;

            actualMesh = Nmesh;
            
            int vCnt = actualMesh.vertices.Length;
    
            meshPoints = new List<MeshPoint>();
            Vector3[] vrts = actualMesh.vertices;
            bool gotUV1 = (actualMesh.uv != null) && (actualMesh.uv.Length == vCnt);
            bool gotUV2 = (actualMesh.uv2 != null) && (actualMesh.uv2.Length == vCnt);
            bool gotColors = (actualMesh.colors != null) && (actualMesh.colors.Length == vCnt);
            gotBoneWeights = (actualMesh.boneWeights != null) && (actualMesh.boneWeights.Length == vCnt);
            gotBindPos = (actualMesh.bindposes != null) && (actualMesh.bindposes.Length == vCnt);

            for (int i = 0; i < vCnt; i++) {
                var v = new MeshPoint(vrts[i])
                {
                    SmoothNormal = false
                };
                meshPoints.Add(v);
                Vertex uv = new Vertex(meshPoints[i], gotUV1 ? actualMesh.uv[i] : Vector2.zero, gotUV2 ? actualMesh.uv2[i] : Vector2.zero);
                if (gotColors)
                    uv._color = actualMesh.colors[i];
                if (gotBoneWeights)
                    v.boneWeight = actualMesh.boneWeights[i];
                if (gotBindPos)
                    v.bindPoses = actualMesh.bindposes[i];
            }
            
            shapes = new List<string>();

            for (int s=0; s<actualMesh.blendShapeCount; s++) {

                for (int v = 0; v < vCnt; v++)
                    meshPoints[v].shapes.Add(new List<BlendFrame>());

                shapes.Add(actualMesh.GetBlendShapeName(s));

                for (int f=0; f<actualMesh.GetBlendShapeFrameCount(s); f++) {

                    blendWeights[s][f] = actualMesh.GetBlendShapeFrameWeight(s, f);

                    Vector3[] nrms = new Vector3[vCnt];
                    Vector3[] pos = new Vector3[vCnt];
                    Vector3[] tng = new Vector3[vCnt];
                    actualMesh.GetBlendShapeFrameVertices(s, f, pos, nrms, tng);

                for (int v = 0; v < vCnt; v++) 
                    meshPoints[v].shapes.Last().Add(new BlendFrame(pos[v], nrms[v], tng[v]));
                
                }
            }
            
            triangles = new List<Triangle>();
            Vertex[] pnts = new Vertex[3];

            submeshCount = actualMesh.subMeshCount;
            baseVertex = new List<uint>();

            for (int s = 0; s < submeshCount; s++)  {

                baseVertex.Add(
#if UNITY_2018_1_OR_NEWER
                    actualMesh.GetBaseVertex(s)
#else
                    0
#endif

                    );
               
                int[] indices = actualMesh.GetTriangles(s);

                int tCnt = indices.Length / 3;

                for (int i = 0; i < tCnt; i++) {

                    for (int e = 0; e < 3; e++)
                        pnts[e] = meshPoints[indices[i * 3 + e]].uvpoints[0];

                    var t = new Triangle(pnts) {
                        submeshIndex = s
                    };
                    triangles.Add(t);
                }
            }


            // Debug.Log("Merging");

            for (int i = 0; i < vCnt; i++)
            {
                MeshPoint main = meshPoints[i];
                for (int j = i + 1; j < vCnt; j++)
                {
                    if ((meshPoints[j].localPos - main.localPos).magnitude < float.Epsilon)
                    {
                       
                        meshPoints[i].MergeWith(meshPoints[j]);
                        j--;
                        vCnt = meshPoints.Count;
                    }

                }
            }

            actualMesh = new Mesh();

            Dirty = true;
        }

        public void MergeWith (PlaytimePainter other) {

            EditableMesh edm = new EditableMesh();
            edm.Edit(other);

            if (UV2distributeRow > 1) {
                Vector2 tile = Vector2.one / UV2distributeRow;
                int y = UV2distributeCurrent / UV2distributeRow;
                int x = UV2distributeCurrent - y * UV2distributeRow;
                Vector2 offset = tile;
                offset.Scale(new Vector2(x, y));
                edm.TileAndOffsetUVs(offset, tile, 1);
                UV2distributeCurrent++;
            }


            var tf = MeshManager.Inst.target.transform;

            triangles.AddRange(edm.triangles);

            foreach (var v in edm.meshPoints) {
                v.WorldPos  =  other.transform.TransformPoint(v.localPos);
                meshPoints.Add(v);
            }

        }

        public bool MoveTris(Vertex from, Vertex to)
        {
            if (from == to) return false;

            foreach (var td in triangles)
                if (td.Includes(from)) {
                    if (td.Includes(to)) return false;
                    td.Replace(from, to);
                }

            MeshPoint vp = from.meshPoint;
            vp.uvpoints.Remove(from);
            if (vp.uvpoints.Count == 0)
                meshPoints.Remove(vp);

            return true;
        }

        public void AllVerticesShared() {
            foreach (var vp in meshPoints)
                while (vp.uvpoints.Count > 1)
                    if (!MoveTris(vp.uvpoints[1], vp.uvpoints[0])) { break; }
            
        }

        public void GiveLineUniqueVerticles_RefreshTrisListing(LineData ld)
        {

            List<Triangle> trs = ld.GetAllTriangles_USES_Tris_Listing();

            if (trs.Count != 2) return;

            ld.pnts[0].meshPoint.SmoothNormal = true;
            ld.pnts[1].meshPoint.SmoothNormal = true;

            trs[0].GiveUniqueVerticesAgainst(trs[1]);
            RefresVerticleTrisList();
        }

        public bool GiveTriangleUniqueVerticles(Triangle tris)
        {
            bool change = false;
            // Mistake here somewhere

            Vertex[] changed = new Vertex[3];
            for (int i = 0; i < 3; i++)
            {
                //int count = 0;
                Vertex uvi = tris.vertexes[i];
        
              //  for (int t = 0; t < triangles.Count; t++)
               //     if (triangles[t].includes(uvi)) count++;

                if (uvi.tris.Count > 1) //count > 1)
                {
                    changed[i] = new Vertex(uvi);
                    change = true;
                }
                else
                    changed[i] = uvi;


            }
            if (change)
                tris.Set(changed);

            return change;
        }

        public void Rescale(float By, Vector3 center)
        {
            foreach (MeshPoint vp in meshPoints)
            {
                Vector3 diff = vp.localPos - center;
                vp.localPos = center + diff * By;
            }

        }

        public Vertex GetUVpointAFromLine(MeshPoint a, MeshPoint b)
        {
            for (int i = 0; i < triangles.Count; i++)
                if (triangles[i].Includes(a) && triangles[i].Includes(b))
                    return triangles[i].GetByVert(a);

            // Debug.Log("Error getting from line");
            return null;
        }

        public void RefresVerticleTrisList()
        {
            foreach (MeshPoint vp in meshPoints)
                foreach (Vertex uv in vp.uvpoints)
                    uv.tris.Clear();

            foreach (Triangle tr in triangles)
                for (int i = 0; i < 3; i++)
                    tr.vertexes[i].tris.Add(tr);


        }

        public void TagTrianglesUnprocessed()
        {
            foreach (Triangle t in triangles)
                t.wasProcessed = false;
        }

        public void PaintAll(LinearColor col)
        {
            BrushMask bm = Cfg.brushConfig.mask;//glob.getBrush().brushMask;
            Color c = col.ToGamma();
            foreach (MeshPoint v in meshPoints)
                foreach (Vertex uv in v.uvpoints)
                   bm.Transfer(ref uv._color, c);
            //Debug.Log("Dirty");
            Dirty = true;
        }

        public void SetShadowAll(LinearColor col)
        {
            BrushMask bm = Cfg.brushConfig.mask;//glob.getBrush().brushMask;
            Color c = col.ToGamma();

            foreach (MeshPoint v in meshPoints)
                bm.Transfer(ref v.shadowBake, c); 
               
            Dirty = true;
        }

        public void Displace(Vector3 by)
        {
            foreach (MeshPoint vp in meshPoints)
                vp.localPos += by;
        }

        public MeshPoint InsertIntoLine(MeshPoint a, MeshPoint b, Vector3 pos) {
            float dsta = Vector3.Distance(pos, a.localPos);
            float dstb = Vector3.Distance(pos, b.localPos);
            float sum = dsta + dstb;
            pos = (a.localPos * dstb + b.localPos * dsta) / sum;

            MeshPoint newVrt = new MeshPoint(pos);

            meshPoints.Add(newVrt);

            List<Triangle> tris = a.Triangles();

            for (int i = 0; i < tris.Count; i++) {
                Triangle tr = tris[i];

                if (tr.Includes(b)) {

                    Vertex auv;
                    Vertex buv;
                    Vertex spliUV;
                    Vector2 uv;
                    Vector2 uv1;
                    Vertex newUV;

                    auv = tr.GetByVert(a);
                    buv = tr.GetByVert(b);
                    spliUV = tr.GetNotOneOf(a, b);


                    if ((auv == null) || (buv == null))  {
                        Debug.Log("Didn't found a uv");
                        continue;
                    }

                    Vector3 w = new Vector3();

                    float dst = dsta + dstb;
                    w[tr.NumberOf(auv)] = dstb / dst;
                    w[tr.NumberOf(buv)] = dsta / dst;

                    uv = (auv.GetUV(0) * dstb + buv.GetUV(0) * dsta) / (dsta + dstb);
                    uv1 = (auv.GetUV(1) * dstb + buv.GetUV(1) * dsta) / (dsta + dstb);


                    newUV = null;

                    if ((Cfg.newVerticesUnique) || (newVrt.uvpoints == null) || (newVrt.uvpoints.Count == 0))
                        newUV = new Vertex(newVrt,uv, uv1);
                    else
                    {
                        for (int j = 0; j < newVrt.uvpoints.Count; j++)
                            if (newVrt.uvpoints[j].SameUV(uv,uv1)) //.uv - uv).magnitude < 0.0001f) dfsdf
                                newUV = newVrt.uvpoints[j];
                    }

                    if (newUV == null)
                        newUV = new Vertex(newVrt, uv, uv1);
                    else
                    newUV.SetUVindexBy(uv, uv1);
                    tr.AssignWeightedData(newUV, w);


                    Triangle trb;
                    trb = new Triangle(tr.vertexes).CopySettingsFrom(tr);
                    triangles.Add(trb);
                    tr.Replace(auv, newUV);
                  

                    if (Cfg.newVerticesUnique) {
                        var split = new Vertex(spliUV);
                        trb.Replace(spliUV, split);
                        var newB = new Vertex(newUV);
                        trb.Replace(buv, newB);
                    }
                    else trb.Replace(buv, newUV);


                }


            }

            Dirty = true;

            if (Cfg.pixelPerfectMeshEditing)
            newVrt.PixPerfect();

            return newVrt;
        }

        public void RemoveLine(LineData ld)
        {
            Vertex a = ld.pnts[0];
            Vertex b = ld.pnts[1];

            for (int i = 0; i < triangles.Count; i++)
                if (triangles[i].Includes(a.meshPoint, b.meshPoint))
                {
                    triangles.Remove(triangles[i]);
                    i--;
                }
        }

        public MeshPoint InsertIntoTriangle(Triangle a, Vector3 pos)
        {
            // Debug.Log("Inserting into triangle");
            MeshPoint newVrt = new MeshPoint(pos);

            Vector3 w = a.DistanceToWeight(pos);

            Vector2 newV2_0 = a.vertexes[0].GetUV(0) * w.x + a.vertexes[1].GetUV(0) * w.y + a.vertexes[2].GetUV(0) * w.z;
            Vector2 newV2_1 = a.vertexes[0].GetUV(1) * w.x + a.vertexes[1].GetUV(1) * w.y + a.vertexes[2].GetUV(1) * w.z;

            Vertex newUV = new Vertex(newVrt, newV2_0, newV2_1);

            a.AssignWeightedData(newUV, w);

            meshPoints.Add(newVrt);

            Triangle b = new Triangle(a.vertexes).CopySettingsFrom(a);
            Triangle c = new Triangle(a.vertexes).CopySettingsFrom(a);

            a.Replace(0, newUV);//uvpnts[0] = newUV;
            b.Replace(1, newUV);// uvpnts[1] = newUV;
            c.Replace(2, newUV);// uvpnts[2] = newUV;

            triangles.Add(b);
            triangles.Add(c);


            if (Cfg.pixelPerfectMeshEditing)
                newVrt.PixPerfect();

            Dirty = true;
            return newVrt;
        }

        public MeshPoint InsertIntoTriangleUniqueVerticles(Triangle a, Vector3 localPos)  {

            MeshPoint newVrt = new MeshPoint(localPos);
            meshPoints.Add(newVrt);

            Vertex[] newUV = new Vertex[3]; // (newVrt);

            Vector3 w = a.DistanceToWeight(localPos);

            Vector2 newV2_0 = a.vertexes[0].GetUV(0) *w.x  + a.vertexes[1].GetUV(0) * w.y + a.vertexes[2].GetUV(0) * w.z;
            Vector2 newV2_1 = a.vertexes[0].GetUV(1) * w.x + a.vertexes[1].GetUV(1) * w.y + a.vertexes[2].GetUV(1) * w.z;
            //Color col = a.uvpnts[0]._color * w.x + a.uvpnts[1]._color * w.y + a.uvpnts[2]._color * w.z;
            for (int i = 0; i < 3; i++)
            {
                newUV[i] = new Vertex(newVrt, newV2_0, newV2_1);
                a.AssignWeightedData(newUV[i], w);
            }

            Triangle b = new Triangle(a.vertexes).CopySettingsFrom(a);
            Triangle c = new Triangle(a.vertexes).CopySettingsFrom(a);

            a.vertexes[0] = newUV[0];
            b.vertexes[1] = newUV[1];
            c.vertexes[2] = newUV[2];

            triangles.Add(b);
            triangles.Add(c);

            a.MakeTriangleVertUnique(a.vertexes[1]);
            b.MakeTriangleVertUnique(b.vertexes[2]);
            c.MakeTriangleVertUnique(c.vertexes[0]);


            if (Cfg.pixelPerfectMeshEditing)
                newVrt.PixPerfect();

            Dirty = true;
            return newVrt;

        }

        public void AutoSetVertColors()
        {
            //  foreach (vertexpointDta v in vertices)
            //    v.setColor(Color.black);

            bool[] asCol = new bool[3];
            bool[] asUV = new bool[3];

            foreach (Triangle td in triangles)
            {

                asCol[0] = asCol[1] = asCol[2] = false;
                asUV[0] = asUV[1] = asUV[2] = false;

                for (int i = 0; i < 3; i++)
                {
                    ColorChanel c = td.vertexes[i].GetZeroChanel_AifNotOne();
                    if ((c != ColorChanel.A) && (!asCol[(int)c]))
                    {
                        asCol[(int)c] = true;
                        asUV[i] = true;
                        td.vertexes[i].SetColor_OppositeTo(c);
                      //  Debug.Log("Setting requested " + c);
                    }
                }

                for (int i = 0; i < 3; i++)
                {
                    if (!asUV[i])
                        for (int j = 0; j < 3; j++)
                            if (!asCol[j])
                            {
                                asCol[(int)j] = true;
                                asUV[i] = true;
                                td.vertexes[i].SetColor_OppositeTo((ColorChanel)j);
                               // Debug.Log("Setting leftover " + (ColorChanel)j);
                                break;
                            }
                }
            }
           // Debug.Log("Dirty");
            Dirty = true;
        }

        #region Inspector
        #if PEGI
        public bool Inspect()
        {
            bool changed = false;

            if ("Run Debug".Click().nl())
                RunDebug();

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
            if (go) {
                if (!textm)
                    textm = go.GetComponentInChildren<TextMesh>();
                go.hideFlags = HideFlags.DontSave;
                go.SetActive(false);
            }
        }
    }
}