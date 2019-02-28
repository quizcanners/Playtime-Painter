using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
#if UNITY_EDITOR
using UnityEditor;
#endif
using PlayerAndEditorGUI;
using QuizCannersUtilities;


namespace Playtime_Painter
{

    public static class MeshAnatomyExtensions {

        public static Vector3 SmoothVector (this List<Triangle> td) {

            var v = Vector3.zero;

            foreach (var t in td)
                v += t.sharpNormal;

            return v.normalized;

        }

        public static bool Contains(this List<MeshPoint> lst, LineData ld) => lst.Contains(ld.pnts[0].meshPoint) && lst.Contains(ld.pnts[1].meshPoint);
      
        public static bool Contains(this List<MeshPoint> lst, Triangle ld)
        {
            foreach (var p in ld.vertexes)
                if (!lst.Contains(p.meshPoint))
                    return false;
            return true;
        }

    }

    public class Vertex : PainterStuffKeepUnrecognized_STD
    {
        protected PlaytimePainter Painter => MeshManager.Inst.target; 

        public int uvIndex;
        public int finalIndex;
        public Color _color;

        public bool tmpMark;
        public bool HasVertex;
        public List<Triangle> tris = new List<Triangle>();
        public Vertex MyLastCopy;
        public MeshPoint meshPoint;

        public bool SameAsLastFrame => this == EditedMesh.lastFramePointedUv; 

        public Vector3 Pos => meshPoint.localPos; 
        
        void AddToList(MeshPoint nPoint)
        {
            meshPoint = nPoint;
            nPoint.vertices.Add(this);
        }

        #region Encode & Decode
        public override StdEncoder Encode() {
            var cody = new StdEncoder();

            cody.Add("i", finalIndex);
            cody.Add_IfNotZero("uvi", uvIndex);
            cody.Add_IfNotBlack("col", _color);

            return cody;
        }

        public override bool Decode(string tg, string data) {
            switch (tg) {
                case "i": finalIndex = data.ToInt(); EditedMesh.uvsByFinalIndex[finalIndex] = this; break;
                case "uvi": uvIndex = data.ToInt(); break;
                case "col": _color = data.ToColor(); break;
                default: return false;
            }
            return true;
        }
        #endregion

        public Vertex GetConnectedUVinVert(MeshPoint other)
        {
            foreach (Triangle t in tris)
            {
                if (t.Includes(other))
                    return (t.GetByVert(other));
            }
            return null;
        }

        public List<Triangle> GetTrianglesFromLine(Vertex other) {
            List<Triangle> lst = new List<Triangle>();

                foreach (var t in tris)
                    if (t.Includes(other)) lst.Add(t);
            
            return lst;
        }

        public void RunDebug()
        {

        }

        public Vertex() {
            meshPoint = MeshPoint.currentlyDecoded;
        }

        public Vertex(Vertex other) {
            _color = other._color;
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
            _color = other._color;
            AddToList(newVertex);
            SetUvIndexBy(other.GetUV(0), other.GetUV(1));
        }

        public Vertex(MeshPoint newVertex, string data) {
            AddToList(newVertex);
            Decode(data);
        }

        public void AssignToNewVertex(MeshPoint vp) {
            Vector2[] myUV = meshPoint.sharedV2S[uvIndex];
            meshPoint.vertices.Remove(this);
            meshPoint = vp;
            meshPoint.vertices.Add(this);
            SetUvIndexBy(myUV);
        }

        public Vector2 EditedUV {
            get { return meshPoint.sharedV2S[uvIndex][MeshMGMT.EditedUV]; }
            set { SetUvIndexBy(value); }
        }

        public Vector2 SharedEditedUV {
            get { return meshPoint.sharedV2S[uvIndex][MeshMGMT.EditedUV]; }
            set { meshPoint.sharedV2S[uvIndex][MeshMGMT.EditedUV] = value; }
        }

        public Vector2 GetUV(int ind) => meshPoint.sharedV2S[uvIndex][ind];
        
        public bool SameUV(Vector2 uv, Vector2 uv1) => (uv - GetUV(0)).magnitude < 0.0000001f && (uv1 - GetUV(1)).magnitude < 0.0000001f;
        

        public void SetUvIndexBy(Vector2[] uvs) =>  uvIndex = meshPoint.GetIndexFor(uvs[0], uvs[1]);
        
        public void SetUvIndexBy(Vector2 uv0, Vector2 uv1) => uvIndex = meshPoint.GetIndexFor(uv0, uv1);
        
        public void SetUvIndexBy(Vector2 uvEdited) {
            var uv0 = MeshMGMT.EditedUV == 0 ? uvEdited : GetUV(0);
            var uv1 = MeshMGMT.EditedUV == 1 ? uvEdited : GetUV(1);

            uvIndex = meshPoint.GetIndexFor(uv0, uv1);
        }

        public bool ConnectedTo(MeshPoint other)
        {
            foreach (var t in tris)
                if (t.Includes(other))
                    return true;

            return false;
        }

        public bool ConnectedTo(Vertex other)
        {
            foreach (var t in tris)
                if (t.Includes(other))
                    return true;

            return false;
        }

        public void SetColor_OppositeTo(ColorChanel chan)
        {
            for (var i = 0; i < 3; i++)
            {
                var c = (ColorChanel)i;
                c.SetChanel(ref _color, c == chan ? 0 : 1);
            }
        }

        public void FlipChanel(ColorChanel chan)
        {
            var val = _color.GetChanel(chan);
            val = (val > 0.9f) ? 0 : 1;
            chan.SetChanel(ref _color, val);
        }

        private ColorChanel GetZeroChanelIfOne(ref int count)
        {
            count = 0;
            var ch = ColorChanel.A;
            for (var i = 0; i < 3; i++)
                if (_color.GetChanel((ColorChanel)i) > 0.9f)
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

    public class BlendFrame : PainterStuffStd
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
        public override StdEncoder Encode() {
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

    public class MeshPoint : PainterStuffKeepUnrecognized_STD
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
        public Countless<Vector3> anim;
        public BoneWeight boneWeight;
        public List<List<BlendFrame>> shapes = new List<List<BlendFrame>>(); // not currently working
        public float edgeStrength;
        public int vertexGroup;

        public void RunDebug()
        {

            for (var i=0; i<vertices.Count; i++) {
                var up = vertices[i];

                if (up.tris.Count != 0) continue;

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
                var emc = MeshManager.Inst.target;
                
                if (!emc.AnimatedVertices())  
                    return emc.transform.TransformPoint(localPos);
                
                var animNo = emc.GetVertexAnimationNumber();
                return emc.transform.TransformPoint(localPos + anim[animNo]);
                
            } 
            set {
              localPos =  MeshMGMT.target.transform.InverseTransformPoint(value);
            }
        }

        public Vector3 GetWorldNormal() => MeshMGMT.target.transform.TransformDirection(GetNormal());
        
        private Vector3 GetNormal() {
            normal = Vector3.zero;
            
            foreach (var u in vertices)
                foreach (var t in u.tris) 
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

        public void AnimateTo(Vector3 dest) {
            dest -= localPos;
            int no = 0;
            if (dest.magnitude > 0)
                anim[no] = dest;
            else anim[no] = Vector3.zero;

            if (dest.magnitude > 0) EditedMesh.hasFrame[no] = true;
        }
        
        public MeshPoint() {
            Reboot(Vector3.zero);
        }

        public MeshPoint(Vector3 npos) {
            Reboot(npos);
        }

        public void PixPerfect() {
            var trg = MeshManager.Inst.target;

            if ((trg!= null) && (trg.ImgMeta!= null)){
                var id = trg.ImgMeta;
                var width = id.width*2;
                var height = id.height*2;

                foreach (var v2a in sharedV2S)
                    for(int i=0; i<2; i++) {

                        var x = v2a[i].x;
                        var y = v2a[i].y;
                        x = Mathf.Round(x * width)/width;
                        y = Mathf.Round(y * height) / height;
                        v2a[i] = new Vector2(x, y);
                }


            }

        }

        void Reboot(Vector3 npos) {
            localPos = npos;
            anim = new Countless<Vector3>();
            vertices = new List<Vertex>();

            smoothNormal = Cfg.newVerticesSmooth;
        }

        public void ClearColor(BrushMask bm) {
            foreach (Vertex uvi in vertices)
                bm.Transfer(ref uvi._color, Color.black);
        }

        void SetChanel(ColorChanel chan, MeshPoint other, float val)
        {
            foreach (Vertex u in vertices)
                if (u.ConnectedTo(other))
                    chan.SetChanel(ref u._color, val);
        }

        public bool FlipChanelOnLine(ColorChanel chan, MeshPoint other)
        {
            float val = 1;

            if (Cfg.makeVerticesUniqueOnEdgeColoring)
               EditedMesh.GiveLineUniqueVerticesRefreshTriangleListing(new LineData(this, other));

            foreach (Vertex u in vertices)
                if (u.ConnectedTo(other))
                    val *= u._color.GetChanel(chan) * u.GetConnectedUVinVert(other)._color.GetChanel(chan);

            val = (val > 0.9f) ? 0 : 1;

            SetChanel(chan, other, val);
            other.SetChanel(chan, this, val);



            EditedMesh.Dirty = true;


            return (val == 1);
        }

        public void SetColorOnLine(Color col, BrushMask bm, MeshPoint other)
        {
            foreach (Vertex u in vertices)
                if (u.ConnectedTo(other))
                    bm.Transfer(ref u._color, col);   //val *= u._color.GetChanel01(chan) * u.GetConnectedUVinVert(other)._color.GetChanel01(chan);

        }

        public void RemoveBorderFromLine(MeshPoint other)
        {
            foreach (Vertex u in vertices)
                if (u.ConnectedTo(other))
                    for (int i = 0; i < 4; i++)
                    {
                        Vertex ouv = u.GetConnectedUVinVert(other);
                        ColorChanel ch = (ColorChanel)i;

                        float val = u._color.GetChanel(ch) * ouv._color.GetChanel(ch);

                        if (val > 0.9f)
                        {
                             ch.SetChanel(ref u._color, 0);
                            ch.SetChanel(ref ouv._color, 0);
                        }
                    }

        }

        public float DistanceTo (MeshPoint other) {
            return (localPos - other.localPos).magnitude;
        }

        public void MergeWith (MeshPoint other) {

            for (int i = 0; i < other.vertices.Count; i++) {
                Vertex buv = other.vertices[i];
                Vector2[] uvs = new Vector2[] { buv.GetUV(0), buv.GetUV(1) };
                vertices.Add(buv);
                buv.meshPoint = this;
                buv.SetUvIndexBy(uvs);
            }

            EditedMesh.meshPoints.Remove(other);

        }

        public void MergeWithNearest() {

            List<MeshPoint> vrts = EditedMesh.meshPoints;

            MeshPoint nearest = null;
            float maxDist = float.MaxValue;

            foreach (MeshPoint v in vrts) {
                float dist = v.DistanceTo(this);
                if ((dist < maxDist) && (v != this))
                {
                    maxDist = dist;
                    nearest = v;
                }
            }

            if (nearest != null)
                MergeWith(nearest);

        }

        public List<Triangle> Triangles()
        {
            List<Triangle> Alltris = new List<Triangle>();

            foreach (Vertex uvi in vertices)
                foreach (Triangle tri in uvi.tris)
                    //if (!Alltris.Contains(tri))
                        Alltris.Add(tri);
            
            return Alltris;
        }

        public bool AllPointsUnique() {
            return (Triangles().Count <= vertices.Count);
        }

        public List<LineData> GetAllLines_USES_Tris_Listing()
        {
            List<LineData> Alllines = new List<LineData>();


            foreach (Vertex uvi in vertices)
            {
                foreach (Triangle tri in uvi.tris)
                {
                    LineData[] lines;
                    lines = tri.GetLinesFor(uvi);

                    for (int i = 0; i < 2; i++)
                    {
                        bool same = false;
                        for (int j = 0; j < Alllines.Count; j++)
                        {
                            if (Alllines[j].SameVerticles(lines[i]))
                            {
                                Alllines[j].trianglesCount++;
                                same = true;
                            }
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
            for (int i = 0; i < vertices.Count; i++)
            {
                for (int g = 0; g < vertices[i].tris.Count; g++)
                    if (vertices[i].tris[g].Includes(other)) return vertices[i].tris[g];
            }
            return null;
        }

        public List<Triangle> GetTrianglesFromLine(MeshPoint other) {
            List<Triangle> lst = new List<Triangle>();
            for (int i = 0; i < vertices.Count; i++) {
                foreach (var t in vertices[i].tris) 
                    if (t.Includes(other)) lst.Add(t);
            }
            return lst;
        }

        public bool SetAllUVsShared()
        {
            if (vertices.Count == 1)
                return false;

            while (vertices.Count > 1)
                if (!EditedMesh.MoveTriangle(vertices[1], vertices[0])) {
                    break;
                }

            return true;
        }
    }
    
    public class Triangle : PainterStuffKeepUnrecognized_STD
    {
        public MeshPoint this[int index] {
            get { return vertexes[index].meshPoint; }
        }
        
        public Vertex[] vertexes = new Vertex[3];
        public bool[] DominantCourner = new bool[3];
        public float[] edgeWeight = new float[3];
        public Vector4 textureNo = new Vector4();
        public int submeshIndex;
        public Vector3 sharpNormal;

        public void RunDebug()
        {

        }

        public Vector3 GetNormal() {

            sharpNormal = MyMath.GetNormalOfTheTriangle(
                    vertexes[0].Pos,
                    vertexes[1].Pos,
                    vertexes[2].Pos);

            return sharpNormal;
        }

        public Vector3 GetNormalByArea(float accuracyFix = 1) {

            Vector3 p0 = vertexes[0].Pos * accuracyFix;
            Vector3 p1 = vertexes[1].Pos * accuracyFix;
            Vector3 p2 = vertexes[2].Pos * accuracyFix;
            
            sharpNormal = MyMath.GetNormalOfTheTriangle(
                    p0 ,
                    p1,
                    p2);

            return sharpNormal * Vector3.Cross(p0 - p1, p0 - p2).magnitude;
        }

        public bool SameAsLastFrame => this == EditedMesh.lastFramePointedTriangle;

        public float Area => Vector3.Cross(vertexes[0].Pos - vertexes[1].Pos, vertexes[0].Pos - vertexes[2].Pos).magnitude * 0.5f;

        #region Encode & Decode
        public override StdEncoder Encode() {
            var cody = new StdEncoder()

            .Add_IfTrue("f0", DominantCourner[0])
            .Add_IfTrue("f1", DominantCourner[1])
            .Add_IfTrue("f2", DominantCourner[2])

            .Add_IfNotEpsilon("ew0", edgeWeight[0])
            .Add_IfNotEpsilon("ew1", edgeWeight[1])
            .Add_IfNotEpsilon("ew2", edgeWeight[2]);

            for (int i = 0; i < 3; i++)
                cody.Add(i.ToString(),vertexes[i].finalIndex);

            cody.Add_IfNotZero("tex", textureNo)

            .Add_IfNotZero("sub", submeshIndex);
           

            return cody;
        }

        public override bool Decode(string tg, string data) {

            switch (tg) {
                case "tex": textureNo = data.ToVector4(); break;
                case "f0": DominantCourner[0] = true; break;
                case "f1": DominantCourner[1] = true; break;
                case "f2": DominantCourner[2] = true; break;
                case "ew0": edgeWeight[0] = data.ToFloat(); break;
                case "ew1": edgeWeight[1] = data.ToFloat(); break;
                case "ew2": edgeWeight[2] = data.ToFloat(); break;
                case "sub": submeshIndex = data.ToInt(); break;
                case "0": vertexes[0] = EditedMesh.uvsByFinalIndex[data.ToInt()]; break;
                case "1": vertexes[1] = EditedMesh.uvsByFinalIndex[data.ToInt()]; break;
                case "2": vertexes[2] = EditedMesh.uvsByFinalIndex[data.ToInt()]; break;
                default: return false;
            }
            return true;

        }
        #endregion

        public Triangle CopySettingsFrom (Triangle td) {
            for (int i = 0; i < 3; i++)
                DominantCourner[i] = td.DominantCourner[i];
            textureNo = td.textureNo;
            submeshIndex = td.submeshIndex;

            return this;
        }

        public bool wasProcessed;

        public bool IsVertexIn(MeshPoint vrt)
        {
            foreach (Vertex v in vertexes)
                if (v.meshPoint == vrt) return true;
            
            return false;
        }

        public bool SetAllVerticesShared()
        {
            bool changed = false;

            for (int i = 0; i < 3; i++) 
                changed |= this[i].SetAllUVsShared();

            return changed;
        }

        public bool SetSmoothVertices (bool to) {
            bool changed = false;
            for (int i = 0; i < 3; i++)
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
                if (DominantCourner[i] != to)
                {
                    changed = true;
                    DominantCourner[i] = to;
                }
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

        public bool IsSameUV(Vertex[] other)
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
            for (int i = 0; i < 3; i++)
                vertexes[i] = nvrts[i];
        }

        public bool Includes(Vertex vrt)
        {
            for (int i = 0; i < 3; i++)
                if (vrt == vertexes[i]) return true;
            return false;
        }

        public bool Includes(MeshPoint vrt)
        {
            for (int i = 0; i < 3; i++)
                if (vrt == vertexes[i].meshPoint) return true;
            return false;
        }

        public bool Includes(MeshPoint a, MeshPoint b)
        {
            int cnt = 0;
            for (int i = 0; i < 3; i++)
                if ((a == vertexes[i].meshPoint) || (b == vertexes[i].meshPoint)) cnt++;
            
            return cnt > 1;
        }

        public bool Includes(LineData ld) => (Includes(ld.pnts[0].meshPoint) && (Includes(ld.pnts[1].meshPoint)));
        

        public bool PointOnTriangle()
        {
            Vector3 va = vertexes[0].meshPoint.distanceToPointedV3;//point.DistanceV3To(uvpnts[0].pos);
            Vector3 vb = vertexes[1].meshPoint.distanceToPointedV3;//point.DistanceV3To(uvpnts[1].pos);
            Vector3 vc = vertexes[2].meshPoint.distanceToPointedV3;//point.DistanceV3To(uvpnts[2].pos);

            float sum = Vector3.Angle(va, vb) + Vector3.Angle(va, vc) + Vector3.Angle(vb, vc);
            return (Mathf.Abs(sum - 360) < 0.01f);
        }

        public int NumberOf(Vertex pnt)
        {
            for (int i = 0; i < 3; i++)
                if (pnt == vertexes[i]) return i;

            return -1;
        }

        public Vertex GetClosestTo(Vector3 fpos)
        {

            Vertex nearest = vertexes[0];
            for (int i = 1; i < 3; i++)
                if ((fpos - vertexes[i].Pos).magnitude < (fpos - nearest.Pos).magnitude) nearest = vertexes[i];

            return nearest;

        }

        public Vertex GetByVert(MeshPoint vrt)
        {
            for (int i = 0; i < 3; i++)
                if (vertexes[i].meshPoint == vrt) return vertexes[i];

            Debug.Log("Error using Get By Vert");
            return null;
        }

        public Vector2 LocalPosToEditedUV(Vector3 localPos)
        {
            Vector3 w = DistanceToWeight(localPos);
            var ind = MeshMGMT.EditedUV;
            return vertexes[0].GetUV(ind) * w.x + vertexes[1].GetUV(ind) * w.y + vertexes[2].GetUV(ind) * w.z;
        }

        public Vector3 DistanceToWeight(Vector3 localPos)
        {

            Vector3 p1 = vertexes[0].Pos;
            Vector3 p2 = vertexes[1].Pos;
            Vector3 p3 = vertexes[2].Pos;

            Vector3 f1 = p1 - localPos;
            Vector3 f2 = p2 - localPos;
            Vector3 f3 = p3 - localPos;

            float a = Vector3.Cross(p2 - p1, p3 - p1).magnitude; // main triangle area a
            Vector3 p = new Vector3( 
                Vector3.Cross(f2, f3).magnitude / a,
                Vector3.Cross(f3, f1).magnitude / a,
                Vector3.Cross(f1, f2).magnitude / a // p3's triangle area / a
            );
            return p; 


    
        }

        public void AssignWeightedData (Vertex to, Vector3 weight) {
         
            to._color = vertexes[0]._color * weight.x + vertexes[1]._color * weight.y + vertexes[2]._color * weight.z;
            to.meshPoint.shadowBake = vertexes[0].meshPoint.shadowBake * weight.x + vertexes[1].meshPoint.shadowBake * weight.y + vertexes[2].meshPoint.shadowBake * weight.z;
            Vertex nearest = (Mathf.Max(weight.x, weight.y) > weight.z)  ? (weight.x > weight.y ? vertexes[0] : vertexes[1]) : vertexes[2];
            to.meshPoint.boneWeight = nearest.meshPoint.boneWeight; 
        }

        public void Replace(Vertex point, Vertex with)
        {
            for (int i = 0; i < 3; i++)
                if (vertexes[i] == point)
                {
                    vertexes[i] = with;
                    return;
                }

        }

        public void Replace(int i, Vertex with)
        {
            vertexes[i].tris.Remove(this);
            vertexes[i] = with;
            with.tris.Add(this);

        }

        public int GetIndexOfNoOneIn(LineData l) {
            for (int i = 0; i < 3; i++)
                if ((vertexes[i].meshPoint != l.pnts[0].meshPoint) && (vertexes[i].meshPoint != l.pnts[1].meshPoint))
                    return i;

            return 0;
        }
        public Vertex GetNotOneOf(MeshPoint a, MeshPoint b)
        {
            for (int i = 0; i < 3; i++)
                if ((vertexes[i].meshPoint != a) && (vertexes[i].meshPoint != b))
                    return vertexes[i];

            return vertexes[0];
        }
        public Vertex GetNotOneIn(LineData l)
        {
            for (int i = 0; i < 3; i++)
                if ((vertexes[i].meshPoint != l.pnts[0].meshPoint) && (vertexes[i].meshPoint != l.pnts[1].meshPoint))
                    return vertexes[i];

            return vertexes[0];
        }
        public Vertex NotOneOf(Vertex[] others)
        {
            for (int i = 0; i < 3; i++)
            {
                bool same;
                same = false;
                foreach (Vertex uvi in others)
                    if (uvi.meshPoint == vertexes[i].meshPoint) same = true;

                if (!same) return vertexes[i];
            }
            return null;
        }
        
        public bool GiveUniqueVerticesAgainst(Triangle td)
        {
            bool changed = false;
            for (int i = 0; i < 3; i++)
            {
                Vertex u = vertexes[i];

                if (td.Includes(u))
                {
                    vertexes[i] = new Vertex(u.meshPoint);
                    changed = true;
                }
            }

            return changed;
        }

        public void MergeAround(Triangle other, MeshPoint vrt)
        {
            for (int i = 0; i < 3; i++)
            {
                if (!Includes(other.vertexes[i].meshPoint))
                {
                    Replace(GetByVert(vrt), other.vertexes[i]);
                    return;
                }
            }
 
        }

        public void MakeTriangleVertUnique(Vertex pnt)
        {
            if (pnt.tris.Count == 1) return;

            Vertex nuv = new Vertex(pnt.meshPoint, pnt);
            
            Replace(pnt, nuv);

            EditedMesh.Dirty = true;
        }
        
        public Triangle NewForCopiedVerticles()
        {
            Vertex[] nvpnts = new Vertex[3];

            for (int i = 0; i < 3; i++)
            {
                if (vertexes[i].MyLastCopy == null) { Debug.Log("Error: UV has not been copied!"); return null; }

                nvpnts[i] = vertexes[i].MyLastCopy;
            }

            return new Triangle(nvpnts);

        }

        public Triangle(Vertex[] nvrts)
        {
            for (int i = 0; i < 3; i++)
            {
                vertexes[i] = nvrts[i];
                vertexes[i].tris.Add(this);
            }
        }

        public Triangle() {
        }

        public LineData[] GetLinesFor(Vertex pnt)
        {
            LineData[] ld = new LineData[2];
            int no = NumberOf(pnt);
            ld[0] = new LineData(this, new Vertex[] { vertexes[no], vertexes[(no + 1) % 3] });
            ld[1] = new LineData(this, new Vertex[] { vertexes[(no + 2) % 3], vertexes[no] });

            return ld;
        }

        public List<Triangle> GetNeighboringTriangles() {
            var lst = new List<Triangle>();

            foreach (var u in vertexes)
                foreach (var t in u.tris)
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

            List<MeshPoint> l = new List<MeshPoint>(); //= new LineData();
            
            foreach (var u in vertexes)
                foreach (var u2 in other.vertexes)
                    if (u.meshPoint == u2.meshPoint) {
                        l.Add(u.meshPoint);
                        break;
                }

            if (l.Count == 2) return new LineData(l[0], l[1]);
            return null;
        }

        public LineData ShortestLine() {
            float shortest = float.PositiveInfinity;
            int shortestIndex = 0;
            for (int i = 0; i < 3; i++) {
                float len = (this[i].localPos - this[(i + 1) % 3].localPos).magnitude;
                if (len < shortest) {
                    shortest = len;
                    shortestIndex = i;
                }
            }

            return new LineData(this[shortestIndex], this[(shortestIndex + 1) % 3]);

        }

    }
    
    public class LineData : PainterStuff
    {
        public Triangle triangle;
        public Vertex[] pnts = new Vertex[2];
        public int trianglesCount;

        public MeshPoint this[int index] => pnts[index].meshPoint; 
        
        public float LocalLength => (this[0].localPos - this[1].localPos).magnitude; 

        public float WorldSpaceLength => (this[0].WorldPos - this[1].WorldPos).magnitude; 

        public bool SameAsLastFrame => Equals(EditedMesh.lastFramePointedLine); 

        public bool Includes(Vertex uv) => ((uv == pnts[0]) || (uv == pnts[1]));
        
        public bool Includes(MeshPoint vp) => ((vp == pnts[0].meshPoint) || (vp == pnts[1].meshPoint));
        
        public bool SameVerticles(LineData other) => (((other.pnts[0].meshPoint == pnts[0].meshPoint) && (other.pnts[1].meshPoint == pnts[1].meshPoint)) ||
                ((other.pnts[0].meshPoint == pnts[1].meshPoint) && (other.pnts[1].meshPoint == pnts[0].meshPoint)));
        
        public LineData(MeshPoint a, MeshPoint b)
        {
            triangle = a.GetTriangleFromLine(b);
            pnts[0] = a.vertices[0];
            pnts[1] = b.vertices[0];
            trianglesCount = 0;
        }

        public LineData(Triangle tri, Vertex[] npoints)
        {
            triangle = tri;
            pnts[0] = npoints[0];
            pnts[1] = npoints[1];
            trianglesCount = 0;
        }

        public LineData(Triangle tri, Vertex a, Vertex b)
        {
            triangle = tri;
            pnts[0] = a;
            pnts[1] = b;

            trianglesCount = 0;
        }

        public Triangle GetOtherTriangle() {
            foreach (Vertex uv0 in pnts)
                foreach (Vertex uv in uv0.meshPoint.vertices)
                    foreach (Triangle tri in uv.tris)
                        if (tri != triangle && tri.Includes(pnts[0].meshPoint) && tri.Includes(pnts[1].meshPoint))
                            return tri;


            return null;
            
        }

        public List<Triangle> GetAllTriangles_USES_Tris_Listing()
        {
            List<Triangle> allTris = new List<Triangle>();

            foreach (Vertex uv0 in pnts)
            {
                foreach (Vertex uv in uv0.meshPoint.vertices)
                {
                    foreach (Triangle tri in uv.tris)
                    {

                        if ((allTris.Contains(tri) == false) && tri.Includes(pnts[0].meshPoint) && (tri.Includes(pnts[1].meshPoint)))
                            allTris.Add(tri);

                    }
                }
            }
      
            return allTris;
        }

        public Vector3 Vector => pnts[1].Pos - pnts[0].Pos;
        
        public Vector3 HalfVectorToB(LineData other)
        {
            LineData LineA = this;
            LineData LineB = other;

            if (other.pnts[1] == pnts[0])
            {
                LineA = other;
                LineB = this;
            }

            Vector3 a = LineA.pnts[0].Pos - LineA.pnts[1].Pos;
            Vector3 b = LineB.pnts[1].Pos - LineB.pnts[0].Pos;

      
            Vector2 fromVector2 = GridNavigator.Inst().InPlaneVector(a);
            Vector2 toVector2 = GridNavigator.Inst().InPlaneVector(b);

       
            Vector2 mid = (fromVector2.normalized + toVector2.normalized).normalized;

            Vector3 cross = Vector3.Cross(fromVector2, toVector2);

            if (cross.z > 0)
                mid = -mid;




            return GridNavigator.Inst().PlaneToWorldVector(mid).normalized;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(LineData))
                return false;

            LineData ld = (LineData)obj;

            return (ld.pnts[0].meshPoint == pnts[0].meshPoint && ld.pnts[1].meshPoint == pnts[1].meshPoint) 
                || (ld.pnts[0].meshPoint == pnts[1].meshPoint && ld.pnts[1].meshPoint == pnts[0].meshPoint);

        }

        public bool AllVerticesShared()
        {
            bool changed = false;
            for (int i = 0; i < 2; i++) 
                changed |= this[i].SetAllUVsShared();

            return changed;
        }

        public bool GiveUniqueVerticesToTriangles() {
            bool changed = false;
            var tris = GetAllTriangles_USES_Tris_Listing();

            for (int i = 0; i < tris.Count; i++)
                for (int j = i + 1; j < tris.Count; j++)
                   changed |= tris[i].GiveUniqueVerticesAgainst(tris[j]);

            return changed;
        }

        public override int GetHashCode() => pnts[0].finalIndex;
        

    }

}