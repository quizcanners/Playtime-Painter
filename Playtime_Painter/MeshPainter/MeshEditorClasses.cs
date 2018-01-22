using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using StoryTriggerData;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
#if UNITY_EDITOR
using UnityEditor;
#endif



namespace Painter {

    //#if UNITY_EDITOR

    [Serializable]
    public class EditableMesh : abstract_STD {


        protected painterConfig cfg { get { return painterConfig.inst; } }

        public bool Dirty;

        public bool gotColors;
        public bool gotBoneWeights;
        public bool gotBindPos;

        public List<string> shapes;
        public Countless<Countless<float>> blendWeights = new Countless<Countless<float>>();

        public string meshName = "unnamed";
        public List<vertexpointDta> vertices = new List<vertexpointDta>();
        public List<trisDta> triangles = new List<trisDta>();

        public CountlessBool hasFrame = new CountlessBool();

        public override stdEncoder Encode() {
            var cody = new stdEncoder();
            cody.AddText("n", meshName);
            cody.AddIfNotEmpty(vertices);
            cody.AddIfNotEmpty(triangles);

            return cody;
        }

        public override void Decode(string tag, string data) {
            switch (tag) {
                case vertexpointDta.stdTag_vrt: vertices = data.ToListOf_STD<vertexpointDta>(); break;
                case trisDta.stdTag_tri: triangles = data.ToListOf_STD<trisDta>(); break;
                case "n": meshName = data; break;
            }
        }

        public override string getDefaultTagName() { return stdTag_mesh; }

        public const string stdTag_mesh = "mesh";

        [NonSerialized]
        public Mesh mesh;

        public void RemoveEmptyDots()
        {
            foreach (vertexpointDta v in vertices)
            {
                foreach (UVpoint uv in v.uv)
                {
                    uv.HasVertex = false;
                }
            }
            foreach (trisDta t in triangles)
            {
                foreach (UVpoint uv in t.uvpnts) { uv.HasVertex = true; }
            }

            foreach (vertexpointDta v in vertices)
            {
                for (int i = 0; i < v.uv.Count; i++)
                    if (v.uv[i].HasVertex == false)
                    {
                        v.uv.RemoveAt(i);
                        i--;
                    }
            }

            for (int i = 0; i < vertices.Count; i++)
            {
                if (vertices[i].uv.Count == 0)
                {
                    vertices.RemoveAt(i);
                    i--;
                }
            }
        }

        public vertexpointDta GetClosestToPos(Vector3 pos)
        {
            vertexpointDta closest = null;
            float dist = 0;
            foreach (vertexpointDta v in vertices)
            {
                //if (v.pos.SameAs(pos))
                float newDist = Vector3.Distance(v.pos, pos);
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
            for (int i = 0; i < vertices.Count; i++) {
                for (int u = 0; u < vertices[i].uv.Count; u++) {
                    vertices[i].uv[u].finalIndex = index;
                    uvsByFinalIndex[index] = vertices[i].uv[u];
                    index++;
                }
            }
            return index;
        }

        public void AddTextureAnimDisplacement()
        {
            MeshManager m = MeshManager.inst();
            if (m._target != null)
            {
                PlaytimePainter em = m._target;
                if (em == null) return;
                int y = em.GetAnimationUVy();
                foreach (vertexpointDta v in vertices)
                    v.pos += (v.anim[y]);
            }
        }

        Vector3 previousPointed = new Vector3();
        float recalculateDelay;
		int nearestVertCount = 50;
        public float distanceLimit = 1;
		static float nearTarget = 64;

        List<vertexpointDta> sortVertsClose = new List<vertexpointDta>();
        List<vertexpointDta> sortVertsFar = new List<vertexpointDta>();
        public CountlessSTD<UVpoint> uvsByFinalIndex = new CountlessSTD<UVpoint>();


        public void SortAround(Vector3 center, bool forceRecalculate)  {

         
            distanceLimit = Mathf.Max(1, distanceLimit * (1f - Time.deltaTime));
			if (distanceLimit == 0)
				distanceLimit = 0.1f;

            recalculateDelay -= Time.deltaTime;

		

			bool recalculated = false;
			int near = 0;
			if (((center - previousPointed).magnitude > distanceLimit * 0.05f) || (recalculateDelay < 0) || (forceRecalculate)) {
				
				for (int i = 0; i < vertices.Count; i++) {
					vertices [i].distanceToPointedV3 = (center - vertices [i].pos);
					vertices [i].distanceToPointed = vertices [i].distanceToPointedV3.magnitude;
					if (vertices [i].distanceToPointed < distanceLimit)
						near++;
				}
				recalculated = true;
			}

			if (vertices.Count > nearTarget*1.5f) {

				if (recalculated){ 
				distanceLimit += Mathf.Max(-distanceLimit*0.5f, ((float)(nearTarget  - near)/nearTarget)*distanceLimit);
				
                previousPointed = center;
                recalculateDelay = 1;

                /*int outOfDistanceLImitCount = 0;

				for (int i = 0; i < nearestVertCount; i++) {
                    vertexpointDta a = vertices[i];
                    vertexpointDta b = vertices[i + 1];
                    if (b.distanceToPointed > distanceLimit)
                        outOfDistanceLImitCount++;

                    if (a.distanceToPointed > b.distanceToPointed) {
                        vertices[i] = b;
                        vertices[i + 1] = a;
                    }
                }*/
				//(outOfDistanceLImitCount > 0) || 

				//if (Mathf.Abs(nearestVertCount-near)>2) {

                    sortVertsClose.Clear();
                    sortVertsFar.Clear();

                    foreach (vertexpointDta v in vertices)
                        if (v.distanceToPointed > distanceLimit)
                            sortVertsFar.Add(v);
                        else
                            sortVertsClose.Add(v);

					nearestVertCount = sortVertsClose.Count;

                    vertices.Clear();
                    vertices.AddRange(sortVertsClose);
                    vertices.AddRange(sortVertsFar);


					distanceLimit += Mathf.Max(-distanceLimit*0.5f,  ((float)(nearTarget - sortVertsClose.Count))*distanceLimit/nearTarget);

                  //  Debug.Log ("range "+distanceLimit + " close " + sortVertsClose.Count + " far "+sortVertsFar.Count);
                //}
				}

				//int min = 0;
				//int max = nearestVertCount;

                for (int j = 0; j < 25; j++) {
					bool changed = false;
					for (int i = 0; i < nearestVertCount-1; i++) {
                        vertexpointDta a = vertices[i];
                        vertexpointDta b = vertices[i + 1];

                        if (a.distanceToPointed > b.distanceToPointed) {
                            vertices[i] = b;
                            vertices[i + 1] = a;
							changed = true;
                        }
                    }
					if (!changed)
						j = 999;

                }
            }
            else
            {
                vertices.Sort(delegate (vertexpointDta a, vertexpointDta b)
                {

                    return b.distanceToPointed < a.distanceToPointed ? 1 : ((b.distanceToPointed - 0.0001f) < a.distanceToPointed ? 0 : -1);

                    //float diff = a.distanceToPointed - b.distanceToPointed;
                    //if (diff == 0) return 0;

                    //return (int)(diff / Mathf.Abs(diff));
                }
                );
                return;
            }
            //Debug.Log ("Min: " + Min + "Max: " + Max + " avg Dist: " + avgDistance);






            /* vertices.Sort(delegate(vertexpointDta a, vertexpointDta b)  {
                 float diff = a.pos.DistanceTo(center) - b.pos.DistanceTo(center);
                 if (diff == 0) return 0;

                 return (int)(diff / Mathf.Abs(diff));
             }
           );*/
        }

        /*
        public void ReadFromSaveData(MeshConstructionData mDta) {

          //  Debug.Log("Reading Saved Mesh Data");

            vertices.Clear();
            triangles.Clear();

            if (mDta.verts == null)
                return;

            int cnt = mDta.verts.Length;

            bool gotUV2 = (mDta.uvs2 != null) && (mDta.uvs2.Length == cnt);

      

          //  DoShadowBake = mDta.DoShadowBake && (mDta.shadowBake != null) && (mDta.shadowBake.Length == cnt);

            if ((mDta.isSmoothNormal == null) || (mDta.isSmoothNormal.Length == 0))
                mDta.isSmoothNormal = new bool[mDta.verts.Length];

            if ((mDta.trisSmoothNormForced == null) || (mDta.trisSmoothNormForced.Length == 0))
                mDta.trisSmoothNormForced = new bool[mDta.tris.Length];


            Vector3 tmp = new Vector3();
            List<UVpoint> tmpUV = new List<UVpoint>();
            myVec3[] vrts = mDta.verts;
            for (int i = 0; i < vrts.Length; i++)
            {
                tmp = vrts[i].ToV3();

                int ind = mDta.originalIndex[i];//smoothNorms[i].w;

                vertexpointDta holder = vertices.Count > ind ? vertices[ind] : null; //GetByPos(tmp);
                if (holder == null)
                {
                    holder = new vertexpointDta(tmp);
                    vertices.Add(holder);
                    holder.index = ind;
                    holder.shadowBake = mDta.shadowBake[i].ToV4();
                }

                holder.SmoothNormal = mDta.isSmoothNormal[i];
                UVpoint nuv = new UVpoint(holder, mDta.uvs[i].ToV2(), gotUV2 ? mDta.uvs2[i].ToV2() : Vector2.zero);
                tmpUV.Add(nuv);
                if (mDta.colors != null)
                    nuv._color.CopyFrom(mDta.colors[i]);

                nuv.index = i;
            }


            hasFrame = new CountlessBool();
            int[] frameInds;
            vertexAnimationFrame[] anms = mDta.anims.GetAllUnsorted(out frameInds);
            for (int i = 0; i < frameInds.Length; i++)
            {
                int frameInd = frameInds[i];
                hasFrame[frameInd] = true;
                //  Debug.Log("Reading frame "+ frameInd);

                int[] vertexIndexes;
                myVec3[] mv3s = anms[i].verts.GetAllUnsorted(out vertexIndexes);

                for (int j = 0; j < vertexIndexes.Length; j++)
                {
                    // Debug.Log("Setting frame "+frameInd+" for vert "+ vertexIndexes[j]);
                    vertices[vertexIndexes[j]].anim[frameInd] = mv3s[j];
                }

            }

            UVpoint[] tri = new UVpoint[3];
            for (int i = 0; i < mDta.tris.Length; i += 3)
            {

                tri[0] = tmpUV[mDta.tris[i]];
                tri[1] = tmpUV[mDta.tris[i + 1]];
                tri[2] = tmpUV[mDta.tris[i + 2]];

                trisDta ntri;
                ntri = new trisDta(tri);
                triangles.Add(ntri);

                ntri.ForceSmoothedNorm[0] = mDta.trisSmoothNormForced[i];
                ntri.ForceSmoothedNorm[1] = mDta.trisSmoothNormForced[i + 1];
                ntri.ForceSmoothedNorm[2] = mDta.trisSmoothNormForced[i + 2];
            }

            Vector4[] tt = mDta.trisTexture;

            if (tt != null)
                for (int i = 0; i < tt.Length; i++)
                    triangles[i].textureNo = tt[i];

            MeshManager.inst().NullPoinedSelected();

            Dirty = true;

        }
        */

        public void NumberVerticlesInTangentsW()
        {
            for (int i = 0; i < vertices.Count; i++)
                vertices[i].index = i; //< 128 ? i : 0;
                                       // Debug.Log("Dirty");
                                       // Dirty = true;

        }

        public void MirrorVerticlesAgainsThePlane(Vector3 ptdPos)
        {

            Vector3 Mirror = GridNavigator.inst().getGridPerpendicularVector();//new Vector3(0,0,0);

            int Count = vertices.Count;
            for (int i = 0; i < Count; i++)
            {
                vertexpointDta vp;
                vertexpointDta newvp;
                Vector3 diff;

                vp = vertices[i];
                diff = 2 * (Vector3.Scale(ptdPos - vp.pos, Mirror));

                newvp = vp.DeepCopy();
                newvp.pos += diff;


                vertices.Add(newvp);

            }


            Count = triangles.Count;

            for (int i = 0; i < Count; i++)
                triangles.Add(triangles[i].NewForCopiedVerticles());

            for (int i = Count; i < triangles.Count; i++)
                triangles[i].InvertNormal();
            Debug.Log("Dirty");
            Dirty = true;

        }

        public void BreakMesh(Mesh Nmesh) {

            meshName = Nmesh.name;

            mesh = Nmesh;

            int vCnt = mesh.vertices.Length;
              Debug.Log("Breaking Mesh "+ vCnt + " verts");

            vertices = new List<vertexpointDta>();
            Vector3[] vrts = mesh.vertices;
            bool gotUV1 = (mesh.uv != null) && (mesh.uv.Length == vCnt);
            bool gotUV2 = (mesh.uv2 != null) && (mesh.uv2.Length == vCnt);
            gotColors = (mesh.colors != null) && (mesh.colors.Length == vCnt);
            gotBoneWeights = (mesh.boneWeights != null) && (mesh.boneWeights.Length == vCnt);
            gotBindPos = (mesh.bindposes != null) && (mesh.bindposes.Length == vCnt);

            

            for (int i = 0; i < vCnt; i++) {
                var v = new vertexpointDta(vrts[i]);
                vertices.Add(v);
                UVpoint uv = new UVpoint(vertices[i], gotUV1 ? mesh.uv[i] : Vector2.zero, gotUV2 ? mesh.uv2[i] : Vector2.zero);
                if (gotColors)
                    uv._color = mesh.colors[i];
                if (gotBoneWeights)
                    v.boneWeight = mesh.boneWeights[i];
                if (gotBindPos)
                    v.bindPoses = mesh.bindposes[i];
            }

            if (mesh.subMeshCount > 1) 
                for (int s=0; s<mesh.subMeshCount; s++) 
                    for (uint i= mesh.GetIndexStart(s); i<mesh.GetIndexCount(s); i++) 
                        vertices[(int)i].submeshIndex = s;
 
            shapes = new List<string>();

            for (int s=0; s<mesh.blendShapeCount; s++) {

                for (int v = 0; v < vCnt; v++)
                    vertices[v].shapes.Add(new List<BlendFrame>());

                shapes.Add(mesh.GetBlendShapeName(s));

            

                for (int f=0; f<mesh.GetBlendShapeFrameCount(s); f++) {

                    blendWeights[s][f] = mesh.GetBlendShapeFrameWeight(s, f);

                    Vector3[] nrms = new Vector3[vCnt];
                    Vector3[] pos = new Vector3[vCnt];
                    Vector3[] tng = new Vector3[vCnt];
                    mesh.GetBlendShapeFrameVertices(s, f, pos, nrms, tng);

                for (int v = 0; v < vCnt; v++) 
                    vertices[v].shapes.last().Add(new BlendFrame(pos[v], nrms[v], tng[v]));
                
                }
            }

            int tCnt = mesh.triangles.Length / 3;
            triangles = new List<trisDta>();
            UVpoint[] pnts = new UVpoint[3];
            int[] tris = mesh.triangles;
            for (int i = 0; i < tCnt; i++)
            {
                int no;
                no = i * 3;
                pnts[0] = vertices[tris[no]].uv[0]; no++;
                pnts[1] = vertices[tris[no]].uv[0]; no++;
                pnts[2] = vertices[tris[no]].uv[0];

                triangles.Add(new trisDta(pnts));
            }

            // Debug.Log("Merging");

            for (int i = 0; i < vCnt; i++)
            {
                vertexpointDta main = vertices[i];
                for (int j = i + 1; j < vCnt; j++)
                {
                    if ((vertices[j].pos - main.pos).magnitude < float.Epsilon)
                    {
                        MergeVertices(i, j);
                        j--;
                        vCnt = vertices.Count;
                    }

                }
            }

            mesh = new Mesh();
           // Debug.Log("Breaking mesh");
        }

        public void MergeVertices(vertexpointDta one, vertexpointDta two) {
            one.MergeWith(two);
        }

        public void MergeVertices(int a, int b) {
            vertices[a].MergeWith(vertices[b]);

        }

        public void MoveTris(UVpoint from, UVpoint to)
        {
            if (from == to) return;

            int cnt = triangles.Count;

            for (int i = 0; i < cnt; i++)
            {
                trisDta td;
                td = triangles[i];
                if (td.includes(from))
                {
                    if (td.includes(to)) return;
                    for (int j = 0; j < 3; j++)
                        if (td.uvpnts[j] == from)
                            td.uvpnts[j] = to;

                }
            }

            vertexpointDta vp = from.vert;
            vp.uv.Remove(from);
            if (vp.uv.Count == 0)
                vertices.Remove(vp);

        }

        public void SMOOTHALLVERTS()
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                vertexpointDta vp;
                vp = vertices[i];
                while (vp.uv.Count > 1)
                    MoveTris(vp.uv[1], vp.uv[0]);

            }
        }

        public void GiveLineUniqueVerticles_REFRESHTRISLISTING(LineData ld)
        {

            List<trisDta> trs = ld.getAllTriangles_USES_Tris_Listing();

            //  Debug.Log("Got "+trs.Count+" triangles");

            if (trs.Count != 2) return;

            ld.pnts[0].vert.SmoothNormal = true;
            ld.pnts[1].vert.SmoothNormal = true;

            trs[0].GiveUniqueVerticesAgainst(trs[1]);
            RefresVerticleTrisList();
        }

        public void GiveTriangleUniqueVerticles(trisDta tris)
        {
            UVpoint[] changed = new UVpoint[3];
            for (int i = 0; i < 3; i++)
            {
                int count = 0;
                UVpoint uvi;
                uvi = tris.uvpnts[i];
                for (int t = 0; t < triangles.Count; t++)
                    if (triangles[t].includes(uvi)) count++;

                if (count > 1)
                    changed[i] = new UVpoint(uvi.vert);
                else
                    changed[i] = uvi;


            }
            tris.Change(changed);
        }

        public void CopyFrom(EditableMesh from)
        {
            vertices.Clear();
            triangles.Clear();
            from.AssignIndexes();

            foreach (vertexpointDta v in from.vertices)
            {
                vertices.Add(v.DeepCopy());
            }
            AssignIndexes();
            UVpoint[] tmp = new UVpoint[3];
            foreach (trisDta t in from.triangles) {
                for (int i = 0; i < 3; i++) {
                    tmp[i] = uvsByFinalIndex[t.uvpnts[i].finalIndex];
                }
                triangles.Add(new trisDta(tmp));
            }

        }

        public void Rescale(float By, Vector3 center)
        {
            foreach (vertexpointDta vp in vertices)
            {
                Vector3 diff = vp.pos - center;
                vp.pos = center + diff * By;
            }

        }

        public UVpoint GetUVpointAFromLine(vertexpointDta a, vertexpointDta b)
        {
            for (int i = 0; i < triangles.Count; i++)
                if (triangles[i].includes(a) && triangles[i].includes(b))
                    return triangles[i].GetByVert(a);

            // Debug.Log("Error getting from line");
            return null;
        }

        public void RefresVerticleTrisList()
        {
            foreach (vertexpointDta vp in vertices)
                foreach (UVpoint uv in vp.uv)
                    uv.tris.Clear();

            foreach (trisDta tr in triangles)
                for (int i = 0; i < 3; i++)
                    tr.uvpnts[i].tris.Add(tr);


        }

        public void tagTrianglesUnprocessed()
        {
            foreach (trisDta t in triangles)
                t.wasProcessed = false;
        }

        public void PaintAll(linearColor col)
        {
            BrushMask bm = cfg.brushConfig.mask;//glob.getBrush().brushMask;
            Color c = col.ToColor();
            foreach (vertexpointDta v in vertices)
                foreach (UVpoint uv in v.uv)
                   bm.Transfer(ref uv._color, c);
            //Debug.Log("Dirty");
            Dirty = true;
        }

        public void Displace(Vector3 by)
        {
            foreach (vertexpointDta vp in vertices)
                vp.pos += by;
        }

        public void insertIntoLine(vertexpointDta a, vertexpointDta b, Vector3 pos)
        {
            float dsta = Vector3.Distance(pos, a.pos);
            float dstb = Vector3.Distance(pos, b.pos);
            float sum = dsta + dstb;
            pos = (a.pos * dstb + b.pos * dsta) / sum;

            vertexpointDta newVrt = new vertexpointDta(pos);

         //   if ((a == null) || (b == null)) Debug.Log("Is null");



            vertices.Add(newVrt);

            List<trisDta> tris = a.triangles();

            for (int i = 0; i < tris.Count; i++) {
                trisDta tr = tris[i];

                if (tr.includes(b)) {

                    UVpoint auv;
                    UVpoint buv;
                    UVpoint spliUV;
                    Vector2 uv;
                    UVpoint newUV;

                    auv = tr.GetByVert(a);
                    buv = tr.GetByVert(b);
                    spliUV = tr.NotOnLine(a, b);


                    if ((auv == null) || (buv == null))
                    {
                        Debug.Log("Didn't found a uv");
                        continue;
                    }

                    Vector3 w = new Vector3();

                    float dst = dsta + dstb;
                    w[tr.NumberOf(auv)] = dstb / dst;
                    w[tr.NumberOf(buv)] = dsta / dst;

                    uv = (auv.uv * dstb + buv.uv * dsta) / (dsta + dstb);

                  

                    newUV = null;

                    if ((!MeshManager.cfg.newVerticesUnique) || (newVrt.uv == null) || (newVrt.uv.Count == 0))
                        newUV = new UVpoint(newVrt);
                    else
                    {
                        for (int j = 0; j < newVrt.uv.Count; j++)
                            if ((newVrt.uv[j].uv - uv).magnitude < 0.0001f)
                                newUV = newVrt.uv[j];
                    }

                    if (newUV == null)
                        newUV = new UVpoint(newVrt, uv);

                    newUV.uv = uv;
                    tr.AssignWeightedData(newUV, w);


                    trisDta trb;
                    trb = new trisDta(tr.uvpnts).CopySettingsFrom(tr);
                    triangles.Add(trb);
                    tr.Replace(auv, newUV);
                   

                 


                    if (!MeshManager.cfg.newVerticesUnique) {
                        var split = new UVpoint(spliUV);
                        trb.Replace(spliUV, split);
                        var newB = new UVpoint(newUV);
                        trb.Replace(buv, newB);
                    }
                    else trb.Replace(buv, newUV);


                }


            }

            Dirty = true;// _Mesh.GenerateMesh(_targetPiece);

        }

        public void RemoveLine(LineData ld)
        {
            UVpoint a = ld.pnts[0];
            UVpoint b = ld.pnts[1];

            for (int i = 0; i < triangles.Count; i++)
                if (triangles[i].includes(a.vert, b.vert))
                {
                    triangles.Remove(triangles[i]);
                    i--;
                }
        }

        public void insertIntoTriangle(trisDta a, Vector3 pos)
        {
            // Debug.Log("Inserting into triangle");
            vertexpointDta newVrt = new vertexpointDta(pos);

            Vector3 w = a.DistanceToWeight(pos);

            Vector2 newV2_0 = a.uvpnts[0].getUV(0) * w.x + a.uvpnts[1].getUV(0) * w.y + a.uvpnts[2].getUV(0) * w.z;
            Vector2 newV2_1 = a.uvpnts[0].getUV(1) * w.x + a.uvpnts[1].getUV(1) * w.y + a.uvpnts[2].getUV(1) * w.z;

            UVpoint newUV = new UVpoint(newVrt, newV2_0, newV2_1);

            a.AssignWeightedData(newUV, w);

            vertices.Add(newVrt);

            trisDta b = new trisDta(a.uvpnts).CopySettingsFrom(a);
            trisDta c = new trisDta(a.uvpnts).CopySettingsFrom(a);

            a.Replace(0, newUV);//uvpnts[0] = newUV;
            b.Replace(1, newUV);// uvpnts[1] = newUV;
            c.Replace(2, newUV);// uvpnts[2] = newUV;

            triangles.Add(b);
            triangles.Add(c);

            Dirty = true;// _Mesh.GenerateMesh(_targetPiece);

        }

        public void insertIntoTriangleUniqueVerticles(trisDta a, Vector3 pos)  {

            vertexpointDta newVrt = new vertexpointDta(pos);
            vertices.Add(newVrt);

            UVpoint[] newUV = new UVpoint[3]; // (newVrt);

            Vector3 w = a.DistanceToWeight(pos);

            Vector2 newV2_0 = a.uvpnts[0].getUV(0) *w.x  + a.uvpnts[1].getUV(0) * w.y + a.uvpnts[2].getUV(0) * w.z;
            Vector2 newV2_1 = a.uvpnts[0].getUV(1) * w.x + a.uvpnts[1].getUV(1) * w.y + a.uvpnts[2].getUV(1) * w.z;
            //Color col = a.uvpnts[0]._color * w.x + a.uvpnts[1]._color * w.y + a.uvpnts[2]._color * w.z;
            for (int i = 0; i < 3; i++)
            {
                newUV[i] = new UVpoint(newVrt, newV2_0, newV2_1);
                a.AssignWeightedData(newUV[i], w);
            }

            trisDta b = new trisDta(a.uvpnts).CopySettingsFrom(a);
            trisDta c = new trisDta(a.uvpnts).CopySettingsFrom(a);

            a.uvpnts[0] = newUV[0];
            b.uvpnts[1] = newUV[1];
            c.uvpnts[2] = newUV[2];

            triangles.Add(b);
            triangles.Add(c);

            a.MakeTriangleVertUnique(a.uvpnts[1]);
            b.MakeTriangleVertUnique(b.uvpnts[2]);
            c.MakeTriangleVertUnique(c.uvpnts[0]);


            Dirty = true;//_Mesh.GenerateMesh(_targetPiece);

        }

        public void AutoSetVertColors()
        {
            //  foreach (vertexpointDta v in vertices)
            //    v.setColor(Color.black);

            bool[] asCol = new bool[3];
            bool[] asUV = new bool[3];

            foreach (trisDta td in triangles)
            {

                asCol[0] = asCol[1] = asCol[2] = false;
                asUV[0] = asUV[1] = asUV[2] = false;

                for (int i = 0; i < 3; i++)
                {
                    ColorChanel c = td.uvpnts[i].GetZeroChanel_AifNotOne();
                    if ((c != ColorChanel.A) && (!asCol[(int)c]))
                    {
                        asCol[(int)c] = true;
                        asUV[i] = true;
                        td.uvpnts[i].setColor_OppositeTo(c);
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
                                td.uvpnts[i].setColor_OppositeTo((ColorChanel)j);
                               // Debug.Log("Setting leftover " + (ColorChanel)j);
                                break;
                            }
                }
            }
           // Debug.Log("Dirty");
            Dirty = true;
        }
    }

    [Serializable]
    public class MarkerWithText
    {
        public GameObject go;
        public TextMesh textm;

        public void init()
        {
            if (textm == null)
                textm = go.GetComponentInChildren<TextMesh>();
            go.hideFlags = HideFlags.DontSave;
        }
    }




    public static class quickMeshFunctionsExtensions {
        public static quickMeshFunctionForG current;

     
        public static bool selected(this quickMeshFunctionForG funk) {
            return current == funk;
        }
    }

    public enum quickMeshFunctionForG { Nothing, MakeOutline, Path, TrisColorForBorderDetection, Line_Center_Vertex_Add, DeleteTrianglesFully, RemoveBordersFromLine }
    // Ctrl + delete: merge this verticle into main verticle
    // Ctrl + move : disconnect this verticle from others
    public enum gtoolPathConfig { ToPlanePerpendicular, AsPrevious, Rotate }
    public class G_tool
    {
        public string[] toolsHints = new string[]
        {
        "", "Press go on edge to grow outline", "Select a line ang press g to add a road segment", "Auto assign tris color", "Add vertex to the center of the line", "Delete triangle and all vertices", "I need to check what this does"
        };

        public Vector2 uvChangeSpeed;
        public bool updated = false;
        public float width;
        public gtoolPathConfig mode;
        public Vector3 PrevDirection;
    }

    //#endif
}