using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using QuizCannersUtilities;


namespace Playtime_Painter
{

    public class Vertex : PainterSystemKeepUnrecognizedStd
    {
        protected PlaytimePainter Painter => MeshManager.target; 

        public int uvIndex;
        public int finalIndex;
        public Color color;
        
        public bool hasVertex;
        public List<Triangle> triangles = new List<Triangle>();
        public Vertex myLastCopy;
        public MeshPoint meshPoint;


        public bool SetColor(Color col)
        {
            if (col.Equals(color)) return false;
            color = col;
            return true;
        }

        public bool SameAsLastFrame => this == EditedMesh.lastFramePointedUv; 

        public Vector3 LocalPos => meshPoint.localPos;

        private void AddToList(MeshPoint nPoint)
        {
            meshPoint = nPoint;
            nPoint.vertices.Add(this);
        }

        #region Encode & Decode
        public override StdEncoder Encode() {
            var cody = new StdEncoder();

            cody.Add("i", finalIndex);
            cody.Add_IfNotZero("uvi", uvIndex);
            cody.Add_IfNotBlack("col", color);

            return cody;
        }

        public override bool Decode(string tg, string data) {
            switch (tg) {
                case "i": finalIndex = data.ToInt(); EditableMesh.decodedEditableMesh.uvsByFinalIndex[finalIndex] = this; break;
                case "uvi": uvIndex = data.ToInt(); break;
                case "col": color = data.ToColor(); break;
                default: return false;
            }
            return true;
        }
        #endregion

        public Vertex GetConnectedUVinVertex(MeshPoint other)
        {
            foreach (var t in triangles)
                if (t.Includes(other))
                    return (t.GetByVertex(other));
            
            return null;
        }

        public List<Triangle> GetTrianglesFromLine(Vertex other) {
            var lst = new List<Triangle>();

                foreach (var t in triangles)
                    if (t.Includes(other)) lst.Add(t);
            
            return lst;
        }

        public void RunDebug()
        {

        }

        #region Constructors
        public Vertex() {
            meshPoint = MeshPoint.currentlyDecoded;
        }

        public Vertex(Vertex other) {
            color = other.color;
            AddToList(other.meshPoint);
            uvIndex = other.uvIndex;
        }

        public Vertex(MeshPoint newVertex)
        {
            AddToList(newVertex);

            if (meshPoint.sharedV2S.Count == 0)
                SetUvIndexBy(Vector2.one * 0.5f, Vector2.one * 0.5f);
            else
                uvIndex = 0;
        }

        public Vertex(MeshPoint newVertex, Vector2 uv0)
        {
            AddToList(newVertex);
          
            SetUvIndexBy(uv0, Vector2.zero);
        }

        public Vertex(MeshPoint newVertex, Vector2 uv0, Vector2 uv1)
        {
            AddToList(newVertex);
            SetUvIndexBy(uv0, uv1);
        }

        public Vertex(MeshPoint newVertex, Vertex other)
        {
            color = other.color;
            AddToList(newVertex);
            SetUvIndexBy(other.GetUv(0), other.GetUv(1));
        }

        public Vertex(MeshPoint newVertex, string data) {
            AddToList(newVertex);
            Decode(data);
        }
        #endregion

        public void AssignToNewVertex(MeshPoint vp) {
            var myUv = meshPoint.sharedV2S[uvIndex];
            meshPoint.vertices.Remove(this);
            meshPoint = vp;
            meshPoint.vertices.Add(this);
            SetUvIndexBy(myUv);
        }

        #region UV MGMT
        public Vector2 EditedUv {
            get { return meshPoint.sharedV2S[uvIndex][MeshMGMT.EditedUV]; }
            set { SetUvIndexBy(value); }
        }

        public Vector2 SharedEditedUv {
            get { return meshPoint.sharedV2S[uvIndex][MeshMGMT.EditedUV]; }
            set { meshPoint.sharedV2S[uvIndex][MeshMGMT.EditedUV] = value; }
        }

        public Vector2 GetUv(int ind) => meshPoint.sharedV2S[uvIndex][ind];
        
        public bool SameUv(Vector2 uv, Vector2 uv1) => (uv - GetUv(0)).magnitude < 0.0000001f && (uv1 - GetUv(1)).magnitude < 0.0000001f;
        
        public void SetUvIndexBy(Vector2[] uvs) =>  uvIndex = meshPoint.GetIndexFor(uvs[0], uvs[1]);
        
        public void SetUvIndexBy(Vector2 uv0, Vector2 uv1) => uvIndex = meshPoint.GetIndexFor(uv0, uv1);
        
        public bool SetUvIndexBy(Vector2 uvEdited) {
            var uv0 = MeshMGMT.EditedUV == 0 ? uvEdited : GetUv(0);
            var uv1 = MeshMGMT.EditedUV == 1 ? uvEdited : GetUv(1);

            var index = meshPoint.GetIndexFor(uv0, uv1);

            if (index == uvIndex) return false;
            uvIndex = index;
            return true;

        }
        #endregion

        public bool ConnectedTo(MeshPoint other)
        {
            foreach (var t in triangles)
                if (t.Includes(other))
                    return true;

            return false;
        }

        public bool ConnectedTo(Vertex other)
        {
            foreach (var t in triangles)
                if (t.Includes(other))
                    return true;

            return false;
        }

        public void SetColor_OppositeTo(ColorChanel chan)
        {
            for (var i = 0; i < 3; i++)
            {
                var c = (ColorChanel)i;
                c.SetChanel(ref color, c == chan ? 0 : 1);
            }
        }

        public void FlipChanel(ColorChanel chan)
        {
            var val = color.GetChanel(chan);
            val = (val > 0.9f) ? 0 : 1;
            chan.SetChanel(ref color, val);
        }

        private ColorChanel GetZeroChanelIfOne(ref int count)
        {
            count = 0;
            var ch = ColorChanel.A;
            for (var i = 0; i < 3; i++)
                if (color.GetChanel((ColorChanel)i) > 0.9f)
                    count++;
                else ch = (ColorChanel)i;

            return ch;
        }

        public ColorChanel GetZeroChanel_AifNotOne()
        {
            var count = 0;

            var ch = GetZeroChanelIfOne(ref count);

            if (count == 2)
                return ch;
            
            foreach (var u in meshPoint.vertices)
                if (u != this){
                    ch = GetZeroChanelIfOne(ref count);
                    if (count == 2) return ch;
                }
            
            return ColorChanel.A;
        }

        public static implicit operator int(Vertex d) => d.finalIndex;
        
    }

    public class MeshPoint : PainterSystemKeepUnrecognizedStd
    {

        // TEMPORATY DATA / NEEDS MANUAL UPDATE:
        public Vector3 normal;
        public int index;
        public bool normalIsSet;
        public float distanceToPointed;
        public Vector3 distanceToPointedV3;
        public static MeshPoint currentlyDecoded;

        // Data to save:
        public List<Vector2[]> sharedV2S = new List<Vector2[]>();
        public List<Vertex> vertices;
        public Vector3 localPos;
        public bool smoothNormal;
        public Vector4 shadowBake;
       // public Countless<Vector3> anim;
        public BoneWeight boneWeight;
        public List<List<BlendFrame>> shapes = new List<List<BlendFrame>>(); // not currently working
        public float edgeStrength;
        public int vertexGroup;

        public void RunDebug()
        {

            for (var i=0; i<vertices.Count; i++) {
                var up = vertices[i];

                if (up.triangles.Count != 0) continue;

                vertices.RemoveAt(i);

                i--;
            }

            foreach (var v in vertices)
                v.RunDebug();


            CleanEmptyIndexes();
        }

        public bool SameAsLastFrame => this == EditedMesh.lastFramePointedUv.meshPoint; 

        public bool SetSmoothNormal(bool to)
        {
            if (to == smoothNormal)
                return false;

            smoothNormal = to;

            return true;
        }

        public Vector3 WorldPos { get {

                
              //  if (!emc.AnimatedVertices())  
              return MeshManager.targetTransform.TransformPoint(localPos);
                
               // var animNo = emc.GetVertexAnimationNumber();
              //  return emc.transform.TransformPoint(localPos + anim[animNo]);
                
            } 
            set {
              localPos = MeshManager.targetTransform.InverseTransformPoint(value);
            }
        }

        public Vector3 GetWorldNormal() => MeshManager.targetTransform.TransformDirection(GetNormal());
        
        private Vector3 GetNormal() {
            normal = Vector3.zero;
            
            foreach (var u in vertices)
                foreach (var t in u.triangles) 
                normal += t.GetNormal();
            
            normal.Normalize();

            return normal;
        }

        #region Encode & Decode
        public override StdEncoder Encode() {
            var cody = new StdEncoder();

            foreach (var lst in sharedV2S) {
                cody.Add("u0", lst[0]);
                cody.Add("u1", lst[1]);
            }
            
            cody.Add_IfNotEmpty("uvs", vertices)

            .Add("pos", localPos)

            .Add_IfTrue("smth", smoothNormal)

            .Add_IfNotZero("shad", shadowBake)

            .Add("bw", boneWeight)

            .Add_IfNotEpsilon("edge", edgeStrength)
            .Add_IfNotEmpty("bs", shapes)
            .Add_IfNotZero("gr", vertexGroup);
          
            return cody;
        }
       
        public override bool Decode(string tg, string data) {
            switch (tg) {
                case "u0":  sharedV2S.Add(new Vector2[2]); 
                            sharedV2S.Last()[0] = data.ToVector2(); break;
                case "u1":  sharedV2S.Last()[1] = data.ToVector2(); break;
                case "uvs": currentlyDecoded = this;  data.Decode_List(out vertices); break;
                case "pos": localPos = data.ToVector3(); break;
                case "smth": smoothNormal = data.ToBool(); break;
                case "shad": shadowBake = data.ToVector4(); break;
                case "bw": boneWeight = data.ToBoneWeight(); break;
                case "edge":  edgeStrength = data.ToFloat(); break;
                case "bs": data.Decode_ListOfList(out shapes); break;
                case "gr": vertexGroup = data.ToInt(); break;
                default: return false;
            }
            return true;
        }
        #endregion

        public int GetIndexFor(Vector2 uv_0, Vector2 uv_1) {
            var cnt = sharedV2S.Count;
            
            for (var i = 0; i < cnt; i++) {
                var v2 = sharedV2S[i];
                if (v2[0] == uv_0 && v2[1] == uv_1)
                    return i;
            }

            var tmp = new Vector2[2];
            tmp[0] = uv_0;
            tmp[1] = uv_1;

            sharedV2S.Add(tmp);


            return cnt;
        }

        public void CleanEmptyIndexes() {

            int cnt = sharedV2S.Count;

            bool[] used = new bool[cnt];
            int[] newIndexes = new int[cnt];

            foreach (var u in vertices)
                used[u.uvIndex] = true;
            
            int currentInd = 0;

            for (int i = 0; i < cnt; i++)
                if (used[i]) {
                    newIndexes[i] = currentInd;
                    currentInd++;
                }

            if (currentInd < cnt) {

                for (int i = cnt - 1; i >= 0; i--)
                    if (!used[i]) sharedV2S.RemoveAt(i);

                foreach (var u in vertices) u.uvIndex = newIndexes[u.uvIndex];
            }
        }
        
        public MeshPoint() {
            Reboot(Vector3.zero);
        }

        public MeshPoint(Vector3 npos) {
            Reboot(npos);
        }

        public void PixPerfect() {
            var trg = MeshManager.target;

            if (trg && (trg.ImgMeta!= null)){
                var id = trg.ImgMeta;
                var width = id.width*2;
                var height = id.height*2;

                foreach (var v2a in sharedV2S)
                    for(var i=0; i<2; i++) {

                        var x = v2a[i].x;
                        var y = v2a[i].y;
                        x = Mathf.Round(x * width)/width;
                        y = Mathf.Round(y * height) / height;
                        v2a[i] = new Vector2(x, y);
                }


            }

        }

        private void Reboot(Vector3 nPos) {
            localPos = nPos;
            vertices = new List<Vertex>();

            smoothNormal = Cfg.newVerticesSmooth;
        }

        public void ClearColor(BrushMask bm) {
            foreach (var uvi in vertices)
                bm.Transfer(ref uvi.color, Color.black);
        }

        private void SetChanel(ColorChanel chan, MeshPoint other, float val)
        {
            foreach (var u in vertices)
                if (u.ConnectedTo(other))
                    chan.SetChanel(ref u.color, val);
        }

        public bool FlipChanelOnLine(ColorChanel chan, MeshPoint other)
        {
            float val = 1;

            if (Cfg.makeVerticesUniqueOnEdgeColoring)
               EditedMesh.GiveLineUniqueVerticesRefreshTriangleListing(new LineData(this, other));

            foreach (var u in vertices)
                if (u.ConnectedTo(other))
                    val *= u.color.GetChanel(chan) * u.GetConnectedUVinVertex(other).color.GetChanel(chan);

            val = (val > 0.9f) ? 0 : 1;

            SetChanel(chan, other, val);
            other.SetChanel(chan, this, val);
            
            EditedMesh.Dirty = true;
            
            return (val == 1);
        }

        public void SetColorOnLine(Color col, BrushMask bm, MeshPoint other)
        {
            foreach (var u in vertices)
                if (u.ConnectedTo(other))
                    bm.Transfer(ref u.color, col);   //val *= u._color.GetChanel01(chan) * u.GetConnectedUVinVertex(other)._color.GetChanel01(chan);

        }

        public void RemoveBorderFromLine(MeshPoint other)
        {
            foreach (var u in vertices)
                if (u.ConnectedTo(other))
                    for (var i = 0; i < 4; i++)
                    {
                        var ouv = u.GetConnectedUVinVertex(other);
                        var ch = (ColorChanel)i;

                        var val = u.color.GetChanel(ch) * ouv.color.GetChanel(ch);

                        if (!(val > 0.9f)) continue;

                        ch.SetChanel(ref u.color, 0);
                        ch.SetChanel(ref ouv.color, 0);
                    }

        }

        public float DistanceTo (MeshPoint other) => (localPos - other.localPos).magnitude;
        
        
        public void MergeWithNearest(EditableMesh edMesh) {

            var allVertices = edMesh.meshPoints;

            MeshPoint nearest = null;
            var maxDist = float.MaxValue;

            foreach (var v in allVertices) {
                var dist = v.DistanceTo(this);
                if (!(dist < maxDist) || v == this) continue;
                maxDist = dist;
                nearest = v;
            }

            if (nearest != null)
                edMesh.Merge(this,nearest);

        }

        public List<Triangle> Triangles()
        {
            var allTriangles = new List<Triangle>();

            foreach (var uvi in vertices)
                foreach (var tri in uvi.triangles)
                        allTriangles.Add(tri);
            
            return allTriangles;
        }

        public bool AllPointsUnique() {
            return (Triangles().Count <= vertices.Count);
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
                            t.trianglesCount++;
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
                    if (t1.Includes(other)) return t1;
            }

            return null;
        }

        public List<Triangle> GetTrianglesFromLine(MeshPoint other) {
            var lst = new List<Triangle>();
            foreach (var t1 in vertices)
                foreach (var t in t1.triangles) 
                    if (t.Includes(other)) lst.Add(t);
            
            return lst;
        }

       
    }
    
    public class Triangle : PainterSystemKeepUnrecognizedStd
    {
        public MeshPoint this[int index] => vertexes[index].meshPoint;

        public Vertex[] vertexes = new Vertex[3];
        public bool[] dominantCorner = new bool[3];
        public float[] edgeWeight = new float[3];
        public Vector4 textureNo;
        public int subMeshIndex;
        public bool subMeshIndexRemapped;
        public Vector3 sharpNormal;


        public void RunDebug()
        {

        }

        public Vector3 GetNormal() {

            sharpNormal = QcMath.GetNormalOfTheTriangle(
                    vertexes[0].LocalPos,
                    vertexes[1].LocalPos,
                    vertexes[2].LocalPos);

            return sharpNormal;
        }

        public Vector3 GetNormalByArea(float accuracyFix = 1) {

            var p0 = vertexes[0].LocalPos * accuracyFix;
            var p1 = vertexes[1].LocalPos * accuracyFix;
            var p2 = vertexes[2].LocalPos * accuracyFix;
            
            sharpNormal = QcMath.GetNormalOfTheTriangle(
                    p0 ,
                    p1,
                    p2);

            return sharpNormal * Vector3.Cross(p0 - p1, p0 - p2).magnitude;
        }

        public bool SameAsLastFrame => this == EditedMesh.lastFramePointedTriangle;

        public float Area => Vector3.Cross(vertexes[0].LocalPos - vertexes[1].LocalPos, vertexes[0].LocalPos - vertexes[2].LocalPos).magnitude * 0.5f;

        #region Encode & Decode
        public override StdEncoder Encode() {
            var cody = new StdEncoder()

            .Add_IfTrue("f0", dominantCorner[0])
            .Add_IfTrue("f1", dominantCorner[1])
            .Add_IfTrue("f2", dominantCorner[2])

            .Add_IfNotEpsilon("ew0", edgeWeight[0])
            .Add_IfNotEpsilon("ew1", edgeWeight[1])
            .Add_IfNotEpsilon("ew2", edgeWeight[2]);

            for (var i = 0; i < 3; i++)
                cody.Add(i.ToString(),vertexes[i].finalIndex);

            cody.Add_IfNotZero("tex", textureNo)

            .Add_IfNotZero("sub", subMeshIndex);
           

            return cody;
        }

        public override bool Decode(string tg, string data) {

            switch (tg) {
                case "tex": textureNo = data.ToVector4(); break;
                case "f0": dominantCorner[0] = true; break;
                case "f1": dominantCorner[1] = true; break;
                case "f2": dominantCorner[2] = true; break;
                case "ew0": edgeWeight[0] = data.ToFloat(); break;
                case "ew1": edgeWeight[1] = data.ToFloat(); break;
                case "ew2": edgeWeight[2] = data.ToFloat(); break;
                case "sub": subMeshIndex = data.ToInt(); break;
                case "0": vertexes[0] = EditableMesh.decodedEditableMesh.uvsByFinalIndex[data.ToInt()]; break;
                case "1": vertexes[1] = EditableMesh.decodedEditableMesh.uvsByFinalIndex[data.ToInt()]; break;
                case "2": vertexes[2] = EditableMesh.decodedEditableMesh.uvsByFinalIndex[data.ToInt()]; break;
                default: return false;
            }
            return true;

        }
        #endregion

        public Triangle CopySettingsFrom (Triangle td) {
            for (var i = 0; i < 3; i++)
                dominantCorner[i] = td.dominantCorner[i];
            textureNo = td.textureNo;
            subMeshIndex = td.subMeshIndex;

            return this;
        }

        public bool wasProcessed;

        public bool IsVertexIn(MeshPoint vrt) => vertexes.Any(v => v.meshPoint == vrt);
        
       

        public bool SetSmoothVertices (bool to) {
            var changed = false;
            for (var i = 0; i < 3; i++)
                if (this[i].smoothNormal != to)
                {
                    changed = true;
                    this[i].smoothNormal = to;
                }
            return changed;
        }

        public bool SetSharpCorners(bool to) {
            var changed = false;
            for (var i = 0; i < 3; i++)
                if (dominantCorner[i] != to)
                {
                    changed = true;
                    dominantCorner[i] = to;
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
            var hold = vertexes[0];

            vertexes[0] = vertexes[2];
            vertexes[2] = hold;
        }

        public bool IsSamePoints(Vertex[] other)
        {
            foreach (var v in other)
            {
                var same = false;
                foreach (var v1 in vertexes)
                    if (v.meshPoint == v1.meshPoint) same = true;
                
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
                    if (v == v1) same = true;
                
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
                    if (v == v1.meshPoint) same = true;
                
                if (!same) return false;
            }
            return true;
        }

        public bool IsNeighbourOf(Triangle td) {
            if (td == this) return false;

            var same = 0;

            foreach (var u in td.vertexes)
                for (var i = 0; i < 3; i++)
                    if (vertexes[i].meshPoint == u.meshPoint) { same++; break; }

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
                if (vrt == vertexes[i]) return true;
            return false;
        }

        public bool Includes(MeshPoint vrt)
        {
            for (var i = 0; i < 3; i++)
                if (vrt == vertexes[i].meshPoint) return true;
            return false;
        }

        public bool Includes(MeshPoint a, MeshPoint b)
        {
            var cnt = 0;
            for (var i = 0; i < 3; i++)
                if ((a == vertexes[i].meshPoint) || (b == vertexes[i].meshPoint)) cnt++;
            
            return cnt > 1;
        }

        public bool Includes(LineData ld) => (Includes(ld.points[0].meshPoint) && (Includes(ld.points[1].meshPoint)));
        #endregion

        public bool PointOnTriangle()
        {
            var va = vertexes[0].meshPoint.distanceToPointedV3;//point.DistanceV3To(uvpnts[0].pos);
            var vb = vertexes[1].meshPoint.distanceToPointedV3;//point.DistanceV3To(uvpnts[1].pos);
            var vc = vertexes[2].meshPoint.distanceToPointedV3;//point.DistanceV3To(uvpnts[2].pos);

            var sum = Vector3.Angle(va, vb) + Vector3.Angle(va, vc) + Vector3.Angle(vb, vc);
            return (Mathf.Abs(sum - 360) < 0.01f);
        }

        public int NumberOf(Vertex pnt)
        {
            for (var i = 0; i < 3; i++)
                if (pnt == vertexes[i]) return i;

            return -1;
        }

        public Vertex GetClosestTo(Vector3 fPos)
        {

            var nearest = vertexes[0];
            for (int i = 1; i < 3; i++)
                if ((fPos - vertexes[i].LocalPos).magnitude < (fPos - nearest.LocalPos).magnitude) nearest = vertexes[i];

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
            var ind = MeshMGMT.EditedUV;
            return vertexes[0].GetUv(ind) * w.x + vertexes[1].GetUv(ind) * w.y + vertexes[2].GetUv(ind) * w.z;
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

        public void AssignWeightedData (Vertex to, Vector3 weight) {
         
            to.color = vertexes[0].color * weight.x + vertexes[1].color * weight.y + vertexes[2].color * weight.z;
            to.meshPoint.shadowBake = vertexes[0].meshPoint.shadowBake * weight.x + vertexes[1].meshPoint.shadowBake * weight.y + vertexes[2].meshPoint.shadowBake * weight.z;
            var nearest = (Mathf.Max(weight.x, weight.y) > weight.z)  ? (weight.x > weight.y ? vertexes[0] : vertexes[1]) : vertexes[2];
            to.meshPoint.boneWeight = nearest.meshPoint.boneWeight; 
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

        public void Replace(int i, Vertex with)
        {
            vertexes[i].triangles.Remove(this);
            vertexes[i] = with;
            with.triangles.Add(this);

        }

        public int GetIndexOfNoOneIn(LineData l) {
            for (var i = 0; i < 3; i++)
                if ((vertexes[i].meshPoint != l.points[0].meshPoint) && (vertexes[i].meshPoint != l.points[1].meshPoint))
                    return i;

            return 0;
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
                if ((vertexes[i].meshPoint != l.points[0].meshPoint) && (vertexes[i].meshPoint != l.points[1].meshPoint))
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
                    if (uvi.meshPoint == vertexes[i].meshPoint) same = true;

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
                if (vertexes[i].myLastCopy == null) { Debug.Log("Error: UV has not been copied!"); return null; }

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

        public Triangle() {
        }

        public LineData[] GetLinesFor(Vertex pnt)
        {
            var ld = new LineData[2];
            var no = NumberOf(pnt);
            ld[0] = new LineData(this, new Vertex[] { vertexes[no], vertexes[(no + 1) % 3] });
            ld[1] = new LineData(this, new Vertex[] { vertexes[(no + 2) % 3], vertexes[no] });

            return ld;
        }

        public List<Triangle> GetNeighboringTriangles() {
            var lst = new List<Triangle>();

            foreach (var u in vertexes)
                foreach (var t in u.triangles)
                    if (!lst.Contains(t) && t.IsNeighbourOf(this))
                        lst.Add(t);

            return lst;
        }

        public List<Triangle> GetNeighboringTrianglesUnprocessed(){
            var lst = new List<Triangle>();

            foreach (var u in vertexes)
                foreach (var t in u.meshPoint.Triangles())
                    if (!t.wasProcessed && !lst.Contains(t) && t.IsNeighbourOf(this))
                        lst.Add(t);
            
            return lst;
        }

        public LineData LineWith(Triangle other) {

            var l = new List<MeshPoint>(); //= new LineData();
            
            foreach (var u in vertexes)
                if (other.vertexes.Any(u2 => u.meshPoint == u2.meshPoint))
                {
                    l.Add(u.meshPoint);
                }

            return l.Count == 2 ? new LineData(l[0], l[1]) : null;
        }

        public LineData ShortestLine() {
            var shortest = float.PositiveInfinity;
            var shortestIndex = 0;
            for (var i = 0; i < 3; i++) {
                var len = (this[i].localPos - this[(i + 1) % 3].localPos).magnitude;
                if (!(len < shortest)) continue;
                shortest = len;
                shortestIndex = i;
            }

            return new LineData(this[shortestIndex], this[(shortestIndex + 1) % 3]);

        }

    }
    
    public class LineData : PainterSystem
    {
        public Triangle triangle;
        public Vertex[] points = new Vertex[2];
        public int trianglesCount;

        public MeshPoint this[int index] => points[index].meshPoint; 
        
        public float LocalLength => (this[0].localPos - this[1].localPos).magnitude; 

        public float WorldSpaceLength => (this[0].WorldPos - this[1].WorldPos).magnitude; 

        public bool SameAsLastFrame => Equals(EditedMesh.lastFramePointedLine); 

        public bool Includes(Vertex uv) => ((uv == points[0]) || (uv == points[1]));
        
        public bool Includes(MeshPoint vp) => ((vp == points[0].meshPoint) || (vp == points[1].meshPoint));
        
        public bool SameVertices(LineData other) => (((other.points[0].meshPoint == points[0].meshPoint) && (other.points[1].meshPoint == points[1].meshPoint)) ||
                ((other.points[0].meshPoint == points[1].meshPoint) && (other.points[1].meshPoint == points[0].meshPoint)));
        
        public LineData(MeshPoint a, MeshPoint b)
        {
            triangle = a.GetTriangleFromLine(b);
            points[0] = a.vertices[0];
            points[1] = b.vertices[0];
            trianglesCount = 0;
        }

        public LineData(Triangle tri, Vertex[] nPoints)
        {
            triangle = tri;
            points[0] = nPoints[0];
            points[1] = nPoints[1];
            trianglesCount = 0;
        }

        public LineData(Triangle tri, Vertex a, Vertex b)
        {
            triangle = tri;
            points[0] = a;
            points[1] = b;

            trianglesCount = 0;
        }

        public Triangle GetOtherTriangle() {
            foreach (var uv0 in points)
                foreach (var uv in uv0.meshPoint.vertices)
                    foreach (var tri in uv.triangles)
                        if (tri != triangle && tri.Includes(points[0].meshPoint) && tri.Includes(points[1].meshPoint))
                            return tri;


            return null;
            
        }

        public List<Triangle> GetAllTriangles()
        {
            var allTriangles = new List<Triangle>();

            foreach (var uv0 in points)
            
                foreach (var uv in uv0.meshPoint.vertices)
                
                    foreach (var tri in uv.triangles)
                    

                        if (!allTriangles.Contains(tri) && tri.Includes(points[0].meshPoint) && tri.Includes(points[1].meshPoint))
                            allTriangles.Add(tri);

                    
               
            
      
            return allTriangles;
        }

        public Vector3 Vector => points[1].LocalPos - points[0].LocalPos;
        
        public Vector3 HalfVectorToB(LineData other)
        {
            var lineA = this;
            var lineB = other;

            if (other.points[1] == points[0])
            {
                lineA = other;
                lineB = this;
            }

            var a = lineA.points[0].LocalPos - lineA.points[1].LocalPos;
            var b = lineB.points[1].LocalPos - lineB.points[0].LocalPos;
            
            var fromVector2 = GridNavigator.Inst().InPlaneVector(a);
            var toVector2 = GridNavigator.Inst().InPlaneVector(b);
            
            var mid = (fromVector2.normalized + toVector2.normalized).normalized;

            var cross = Vector3.Cross(fromVector2, toVector2);

            if (cross.z > 0)
                mid = -mid;
            
            return GridNavigator.Inst().PlaneToWorldVector(mid).normalized;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(LineData))
                return false;

            var ld = (LineData)obj;

            return (ld.points[0].meshPoint == points[0].meshPoint && ld.points[1].meshPoint == points[1].meshPoint) 
                || (ld.points[0].meshPoint == points[1].meshPoint && ld.points[1].meshPoint == points[0].meshPoint);

        }

        public override int GetHashCode() => points[0].finalIndex * 1000000000 + points[1].finalIndex;
        
    

        public bool GiveUniqueVerticesToTriangles() {
            var changed = false;
            var triangles = GetAllTriangles();

            for (var i = 0; i < triangles.Count; i++)
                for (var j = i + 1; j < triangles.Count; j++)
                   changed |= triangles[i].GiveUniqueVerticesAgainst(triangles[j]);

            return changed;
        }

    }
    
    public class BlendFrame : PainterSystemStd
    {
        public Vector3 deltaPosition;
        public Vector3 deltaTangent;
        public Vector3 deltaNormal;

        #region Encode & Decode
        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "p": deltaPosition = data.ToVector3(); break;
                case "t": deltaTangent = data.ToVector3(); break;
                case "n": deltaNormal = data.ToVector3(); break;
                default: return false;
            }
            return true;
        }
        public override StdEncoder Encode()
        {
            var cody = new StdEncoder();

            cody.Add_IfNotZero("p", deltaPosition);
            cody.Add_IfNotZero("t", deltaTangent);
            cody.Add_IfNotZero("n", deltaNormal);

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

    public static class MeshAnatomyExtensions
    {

        public static Vector3 SmoothVector(this List<Triangle> td)
        {

            var v = Vector3.zero;

            foreach (var t in td)
                v += t.sharpNormal;

            return v.normalized;

        }

        public static bool Contains(this List<MeshPoint> lst, LineData ld) => lst.Contains(ld.points[0].meshPoint) && lst.Contains(ld.points[1].meshPoint);

        public static bool Contains(this List<MeshPoint> lst, Triangle ld) => ld.vertexes.All(p => lst.Contains(p.meshPoint));
        
    }
}