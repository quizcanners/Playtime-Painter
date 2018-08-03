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
using SharedTools_Stuff;


namespace Playtime_Painter
{

    public static class MeshAnatomyExtensions {

        public static Vector3 SmoothVector (this List<Triangle> td) {

            var v = Vector3.zero;

            foreach (var t in td)
                v += t.sharpNormal;

            return v.normalized;

        }

        public static bool Contains(this List<MeshPoint> lst, LineData ld)
        {
            return lst.Contains(ld.pnts[0].meshPoint) && lst.Contains(ld.pnts[1].meshPoint);
        }

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
        protected PlaytimePainter Painter { get { return MeshManager.Inst.target; } }

        public int uvIndex;
        public int finalIndex;
        public Color _color;

        public bool tmpMark;
        public bool HasVertex;
        public List<Triangle> tris = new List<Triangle>();
        public Vertex MyLastCopy;
        public MeshPoint meshPoint;

        public bool SameAsLastFrame { get { return this == EditedMesh.LastFramePointedUV; } }

        public Vector3 Pos { get { return meshPoint.localPos; } }
        
        void AddToList(MeshPoint nPoint)
        {
            meshPoint = nPoint;
            nPoint.uvpoints.Add(this);
        }

        void Init (Vertex other) {
            _color = other._color;
        }
        
        public override StdEncoder Encode() {
            var cody = new StdEncoder();

            cody.Add("i", finalIndex);
            cody.Add("uvi", uvIndex);
            cody.Add("col", _color);


            return cody;
        }

        public override bool Decode(string tag, string data) {
            switch (tag) {
                case "i": finalIndex = data.ToInt(); EditedMesh.uvsByFinalIndex[finalIndex] = this; break;
                case "uvi": uvIndex = data.ToInt(); break;
                case "col": _color = data.ToColor(); break;
                default: return false;
            }
            return true;
        }

       /* public UVpoint DeepCopyTo(vertexpointDta nvert)
        {
            UVpoint tmp = new UVpoint(nvert, GetUV(0), GetUV(1));
            tmp.finalIndex = finalIndex;
            tmp.uvIndex = uvIndex;
            tmp._color = _color;

            MyLastCopy = tmp;
            return tmp;
        }*/

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

        public Vertex(Vertex other)
        {
            Init(other);
            AddToList(other.meshPoint);
            uvIndex = other.uvIndex;
            
        }

        public Vertex(MeshPoint nvert)
        {
            AddToList(nvert);

            if (meshPoint.shared_v2s.Count == 0)
                SetUVindexBy(Vector2.one * 0.5f, Vector2.one * 0.5f);
            else
                uvIndex = 0;
        }

        public Vertex(MeshPoint nvert, Vector2 uv_0)
        {
            AddToList(nvert);
          
            SetUVindexBy(uv_0, Vector2.zero);
        }

        public Vertex(MeshPoint nvert, Vector2 uv_0, Vector2 uv_1)
        {
            AddToList(nvert);
            SetUVindexBy(uv_0, uv_1);
        }

        public Vertex(MeshPoint nvert, Vertex other)
        {
            Init(other);
            AddToList(nvert);
            SetUVindexBy(other.GetUV(0), other.GetUV(1));
        }

        public Vertex(MeshPoint nvert, string data) {
            AddToList(nvert);
            Decode(data);
        }

        public void AssignToNewVertex(MeshPoint vp) {
            Vector2[] myUV = meshPoint.shared_v2s[uvIndex];
            meshPoint.uvpoints.Remove(this);
            meshPoint = vp;
            meshPoint.uvpoints.Add(this);
            SetUVindexBy(myUV);
        }

        public Vector2 EditedUV {
            get { return meshPoint.shared_v2s[uvIndex][MeshMGMT.editedUV]; }
            set { SetUVindexBy(value); }
        }

        public Vector2 SharedEditedUV {
            get { return meshPoint.shared_v2s[uvIndex][MeshMGMT.editedUV]; }
            set { meshPoint.shared_v2s[uvIndex][MeshMGMT.editedUV] = value; }
        }

        public Vector2 GetUV(int ind) {
            return meshPoint.shared_v2s[uvIndex][ind];
        }

        public bool SameUV(Vector2 uv, Vector2 uv1)
        {
            return (((uv - GetUV(0)).magnitude < 0.0000001f) && ((uv1 - GetUV(1)).magnitude < 0.0000001f));
        }

        public void SetUVindexBy(Vector2[] uvs)
        {
            uvIndex = meshPoint.GetIndexFor(uvs[0], uvs[1]);
        }

        public void SetUVindexBy(Vector2 uv_0, Vector2 uv_1)
        {
            uvIndex = meshPoint.GetIndexFor(uv_0, uv_1);
        }

        public void SetUVindexBy(Vector2 uv_edited) {
            var uv_0 = MeshMGMT.editedUV == 0 ? uv_edited : GetUV(0);
            var uv_1 = MeshMGMT.editedUV == 1 ? uv_edited : GetUV(1);

            uvIndex = meshPoint.GetIndexFor(uv_0, uv_1);
        }

        public bool ConnectedTo(MeshPoint other)
        {
            foreach (Triangle t in tris)
                if (t.Includes(other))
                    return true;

            return false;
        }

        public bool ConnectedTo(Vertex other)
        {
            foreach (Triangle t in tris)
                if (t.Includes(other))
                    return true;

            return false;
        }

        public void SetColor_OppositeTo(ColorChanel chan)
        {
            for (int i = 0; i < 3; i++)
            {
                ColorChanel c = (ColorChanel)i;
                c.SetChanel(ref _color, c == chan ? 0 : 1);
            }
        }

        public void FlipChanel(ColorChanel chan)
        {
            float val = _color.GetChanel(chan);
            val = (val > 0.9f) ? 0 : 1;
            chan.SetChanel(ref _color, val);
        }

        ColorChanel GetZeroChanelIfOne(ref int count)
        {
            count = 0;
            ColorChanel ch = ColorChanel.A;
            for (int i = 0; i < 3; i++)
                if (_color.GetChanel((ColorChanel)i) > 0.9f)
                    count++;
                else ch = (ColorChanel)i;

            return ch;
        }

        public ColorChanel GetZeroChanel_AifNotOne()
        {
            int count = 0;

            ColorChanel ch = GetZeroChanelIfOne(ref count);

            if (count == 2)
                return ch;
            else
            {
                foreach (Vertex u in meshPoint.uvpoints) if (u != this)
                    {
                        ch = GetZeroChanelIfOne(ref count);
                        if (count == 2) return ch;
                    }
            }


            return ColorChanel.A;
        }

        public static implicit operator int(Vertex d)   {
            return d.finalIndex;
        }
    }

    public class BlendFrame : PainterStuff_STD
    {
        public Vector3 deltaPosition;
        public Vector3 deltaTangent;
        public Vector3 deltaNormal;

        public override bool Decode(string tag, string data)
        {
            switch (tag)
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



        public BlendFrame()
        {

        }

        public BlendFrame(Vector3 pos, Vector3 norm, Vector3 tang)
        {
            deltaPosition = pos;
            deltaNormal = norm;
            deltaTangent = tang;
        }

       // public const string tagName_bs = "bs";    
  
    } 

    public class MeshPoint : PainterStuffKeepUnrecognized_STD
    {

        // TEMPORATY DATA / NEEDS MANUAL UPDATE:
        public Vector3 normal;
        public int index;
        public bool NormalIsSet;
        public float distanceToPointed;
        public Vector3 distanceToPointedV3;
        public static MeshPoint currentlyDecoded;

        // Data to save:
        public List<Vector2[]> shared_v2s = new List<Vector2[]>();
        public List<Vertex> uvpoints;
        public Vector3 localPos;
        public bool SmoothNormal;
        public Vector4 shadowBake;
        public Countless<Vector3> anim;
        public BoneWeight boneWeight;
        public Matrix4x4 bindPoses;
        public List<List<BlendFrame>> shapes; // not currently working
        //public int submeshIndex;
        public float edgeStrength;
        public int vertexGroup = 0;

        public void RunDebug()
        {

            for (int i=0; i<uvpoints.Count; i++) {
                var up = uvpoints[i];
                if (up.tris.Count == 0) {
                    uvpoints.RemoveAt(i);
                    i--;
                }
            }

            foreach (var v in uvpoints)
                v.RunDebug();


            CleanEmptyIndexes();
        }

        public bool SameAsLastFrame { get { return this == EditedMesh.LastFramePointedUV.meshPoint; } }

        public bool SetSmoothNormal(bool to)
        {
            if (to == SmoothNormal)
                return false;
            SmoothNormal = to;
            return true;
        }

        public Vector3 WorldPos { get {
                PlaytimePainter emc = MeshManager.Inst.target;
                if (emc.AnimatedVertices())   {
                    int animNo = emc.GetVertexAnimationNumber();
                    return emc.transform.TransformPoint(localPos + anim[animNo]);
                }

                return emc.transform.TransformPoint(localPos);
            } 

            set {
              localPos =  MeshMGMT.target.transform.InverseTransformPoint(value);
            }

        }

        public Vector3 GetWorldNormal() {
            return MeshMGMT.target.transform.TransformDirection(GetNormal());
        }

        public Vector3 GetNormal() {
            normal = Vector3.zero;
            foreach (var u in uvpoints)
                foreach (var t in u.tris) 
                normal += t.GetNormal();
            
            normal.Normalize();

            return normal;
        }

        public override StdEncoder Encode() {
            var cody = new StdEncoder();

            foreach (var lst in shared_v2s) {
                cody.Add("u0", lst[0]);
                cody.Add("u1", lst[1]);
            }

            
            cody.Add_ifNotEmpty("uvs", uvpoints);

            cody.Add("pos", localPos);

            cody.Add_Bool("smth", SmoothNormal);

            cody.Add("shad", shadowBake);

            cody.Add("bw", boneWeight);

            cody.Add("biP", bindPoses);

            cody.Add("edge", edgeStrength);

            if (shapes != null)
                cody.Add_IfNotEmpty("bs", shapes);

            cody.Add_ifNotZero("gr", vertexGroup);
          
            return cody;
        }
       
        public override bool Decode(string tag, string data) {
            switch (tag) {
                case "u0":  shared_v2s.Add(new Vector2[2]); 
                            shared_v2s.Last()[0] = data.ToVector2(); break;
                case "u1":  shared_v2s.Last()[1] = data.ToVector2(); break;
                case "uvs": currentlyDecoded = this;
                    data.DecodeInto(out uvpoints);
                    break;
                case "pos": localPos = data.ToVector3(); break;
                case "smth": SmoothNormal = data.ToBool(); break;
                case "shad": shadowBake = data.ToVector4(); break;
                case "bw": data.DecodeInto(out boneWeight); //ToBoneWeight();
                    break;
                case "biP":  data.DecodeInto(out bindPoses);  break;
                case "edge":  edgeStrength = data.ToFloat(); break;
                case "bs": data.DecodeInto(out shapes); break;
                case "gr": vertexGroup = data.ToInt(); break;
                default: return false;
            }
            return true;
        }

     //   public override string GetDefaultTagName() { return stdTag_vrt;}

      //  public const string stdTag_vrt = "vrt";

        public int GetIndexFor(Vector2 uv_0, Vector2 uv_1) {
            int cnt = shared_v2s.Count;
            for (int i = 0; i < cnt; i++) {
                Vector2[] v2 = shared_v2s[i];
                if ((v2[0] == uv_0) && (v2[1] == uv_1))
                    return i;
            }

            Vector2[] tmp = new Vector2[2];
            tmp[0] = uv_0;
            tmp[1] = uv_1;

            shared_v2s.Add(tmp);

            //if (uvpoints.Count < cnt)
              //  CleanEmptyIndexes();

            return cnt;
        }

        public void CleanEmptyIndexes() {

            int cnt = shared_v2s.Count;

            bool[] used = new bool[cnt];
            int[] newIndexes = new int[cnt];

            foreach (var u in uvpoints)
                used[u.uvIndex] = true;
            
            int currentInd = 0;

            for (int i = 0; i < cnt; i++)
                if (used[i]) {
                    newIndexes[i] = currentInd;
                    currentInd++;
                }

            if (currentInd < cnt) {

                for (int i = cnt - 1; i >= 0; i--)
                    if (!used[i]) shared_v2s.RemoveAt(i);

                foreach (var u in uvpoints) u.uvIndex = newIndexes[u.uvIndex];
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

            if ((trg!= null) && (trg.ImgData!= null)){
                var id = trg.ImgData;
                var width = id.width*2;
                var height = id.height*2;

                foreach (var v2a in shared_v2s)
                    for(int i=0; i<2; i++) {

                        var x = v2a[i].x;
                        var y = v2a[i].y;
                        x = Mathf.Round(x * width)/width;
                        y = Mathf.Round(y * height) / height;
                        v2a[i] = new Vector2(x, y);
                      //  Debug.Log("UV is "+v2a[i]);
                }


            }

        }

        void Reboot(Vector3 npos) {
            localPos = npos;
            anim = new Countless<Vector3>();
            uvpoints = new List<Vertex>();

            SmoothNormal = Cfg.newVerticesSmooth;
        }

        public void ClearColor(BrushMask bm) {
            foreach (Vertex uvi in uvpoints)
                bm.Transfer(ref uvi._color, Color.black);
        }

        void SetChanel(ColorChanel chan, MeshPoint other, float val)
        {
            foreach (Vertex u in uvpoints)
                if (u.ConnectedTo(other))
                    chan.SetChanel(ref u._color, val);
        }

        public bool FlipChanelOnLine(ColorChanel chan, MeshPoint other)
        {
            float val = 1;

            if (Cfg.MakeVericesUniqueOnEdgeColoring)
               EditedMesh.GiveLineUniqueVerticles_RefreshTrisListing(new LineData(this, other));

            foreach (Vertex u in uvpoints)
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
            foreach (Vertex u in uvpoints)
                if (u.ConnectedTo(other))
                    bm.Transfer(ref u._color, col);   //val *= u._color.GetChanel01(chan) * u.GetConnectedUVinVert(other)._color.GetChanel01(chan);

        }

        public void RemoveBorderFromLine(MeshPoint other)
        {
            foreach (Vertex u in uvpoints)
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
        /*
        public vertexpointDta DeepCopy()
        {
            vertexpointDta nyu = new vertexpointDta(localPos);
            nyu.SmoothNormal = SmoothNormal;
            nyu.distanceToPointed = distanceToPointed;
            nyu.distanceToPointedV3 = distanceToPointedV3;
            nyu.index = index;
            nyu.shadowBake = shadowBake;

            foreach (UVpoint u in uvpoints)
            {
                u.DeepCopyTo(nyu);
            }

            foreach (Vector2[] v2a in shared_v2s)
            {
                Vector2[] tmp = new Vector2[2];
                tmp[0] = v2a[0];
                tmp[1] = v2a[1];
                nyu.shared_v2s.Add(tmp);
            }


            return nyu;
        }
        */
        public float DistanceTo (MeshPoint other) {
            return (localPos - other.localPos).magnitude;
        }

        public void MergeWith (MeshPoint other) {

            for (int i = 0; i < other.uvpoints.Count; i++) {
                Vertex buv = other.uvpoints[i];
                Vector2[] uvs = new Vector2[] { buv.GetUV(0), buv.GetUV(1) };
                uvpoints.Add(buv);
                buv.meshPoint = this;
                buv.SetUVindexBy(uvs);
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

            foreach (Vertex uvi in uvpoints)
                foreach (Triangle tri in uvi.tris)
                    //if (!Alltris.Contains(tri))
                        Alltris.Add(tri);
            
            return Alltris;
        }

        public bool AllPointsUnique() {
            return (Triangles().Count <= uvpoints.Count);
        }

        public List<LineData> GetAllLines_USES_Tris_Listing()
        {
            List<LineData> Alllines = new List<LineData>();


            foreach (Vertex uvi in uvpoints)
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

            // Debug.Log("Found "+Alllines.Count + " Unique Lines ");

            return Alllines;
        }

        public Triangle GetTriangleFromLine(MeshPoint other)
        {
            for (int i = 0; i < uvpoints.Count; i++)
            {
                for (int g = 0; g < uvpoints[i].tris.Count; g++)
                    if (uvpoints[i].tris[g].Includes(other)) return uvpoints[i].tris[g];
            }
            return null;
        }

        public List<Triangle> GetTrianglesFromLine(MeshPoint other) {
            List<Triangle> lst = new List<Triangle>();
            for (int i = 0; i < uvpoints.Count; i++) {
                foreach (var t in uvpoints[i].tris) 
                    if (t.Includes(other)) lst.Add(t);
            }
            return lst;
        }

        public bool SetAllUVsShared()
        {
            if (uvpoints.Count == 1)
                return false;

            while (uvpoints.Count > 1)
                if (!EditedMesh.MoveTris(uvpoints[1], uvpoints[0])) {
                    break;
                }

            return true;
        }
    }

    [Serializable]
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

        public bool SameAsLastFrame { get { return this == EditedMesh.LastFramePointedTris; } }

        public float Area => Vector3.Cross(vertexes[0].Pos - vertexes[1].Pos, vertexes[0].Pos - vertexes[2].Pos).magnitude * 0.5f;
        
        public override StdEncoder Encode() {
            var cody = new StdEncoder()

            .Add_ifTrue("f0", DominantCourner[0])
            .Add_ifTrue("f1", DominantCourner[1])
            .Add_ifTrue("f2", DominantCourner[2])

            .Add("ew0", edgeWeight[0])
            .Add("ew1", edgeWeight[1])
            .Add("ew2", edgeWeight[2]);

            for (int i = 0; i < 3; i++)
                cody.Add(i.ToString(),vertexes[i].finalIndex);

            cody.Add("tex", textureNo)

            .Add_ifNotZero("sub", submeshIndex);
           

            return cody;
        }

        public override bool Decode(string tag, string data) {

            switch (tag) {
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

       /* public override string GetDefaultTagName() {
            return stdTag_tri;
        }*/

       // public const string stdTag_tri = "tri";

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
            {
                if (v.meshPoint == vrt) return true;
            }
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
                if (this[i].SmoothNormal != to)
                {
                    changed = true;
                    this[i].SmoothNormal = to;
                }
            return changed;
        }

        public bool SetSharpCorners(bool to) {
            bool changed = false;
            for (int i = 0; i < 3; i++)
                if (DominantCourner[i] != to)
                {
                    changed = true;
                    DominantCourner[i] = to;
                }
            return changed;
        }

        public void InvertNormal()
        {
            Vertex hold = vertexes[0];

            vertexes[0] = vertexes[2];
            vertexes[2] = hold;
        }

        public bool IsSamePoints(Vertex[] other)
        {
            foreach (Vertex v in other)
            {
                bool same = false;
                foreach (Vertex v1 in vertexes)
                {
                    if (v.meshPoint == v1.meshPoint) same = true;
                }
                if (!same) return false;
            }
            return true;
        }

        public bool IsSameUV(Vertex[] other)
        {
            foreach (Vertex v in other)
            {
                bool same = false;
                foreach (Vertex v1 in vertexes)
                {
                    if (v == v1) same = true;
                }
                if (!same) return false;
            }
            return true;
        }

        public bool IsSameAs(MeshPoint[] other)
        {
            foreach (MeshPoint v in other)
            {
                bool same = false;
                foreach (Vertex v1 in vertexes)
                {
                    if (v == v1.meshPoint) same = true;
                }
                if (!same) return false;
            }
            return true;
        }

        public bool IsNeighbourOf(Triangle td) {
            if (td == this) return false;

            int same = 0;

            foreach (var u in td.vertexes)
                for (int i = 0; i < 3; i++)
                    if (vertexes[i].meshPoint == u.meshPoint) { same++; break; }

            return same == 2;
        }

        public void Change(Vertex[] nvrts)
        {
            for (int i = 0; i < 3; i++)
           // {
               // nvrts[i].editedUV = uvpnts[i].editedUV;
                vertexes[i] = nvrts[i];
            //}
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
            {
                if ((a == vertexes[i].meshPoint) || (b == vertexes[i].meshPoint)) cnt++;
            }
            return cnt > 1;
        }

        public bool Includes(LineData ld)
        {
            return (Includes(ld.pnts[0].meshPoint) && (Includes(ld.pnts[1].meshPoint)));
        }

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
            return null;//uvpnts[0];
        }

        public Vector2 LocalPosToEditedUV(Vector3 localPos)
        {
            Vector3 w = DistanceToWeight(localPos);
            var ind = MeshMGMT.editedUV;
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


            /* Vector3 dst = new Vector3(point.DistanceTo(uvpnts[0].pos),
              point.DistanceTo(uvpnts[1].pos),
              point.DistanceTo(uvpnts[2].pos)).normalized;

             return (uvpnts[0].v2 * (1 - dst.x) + uvpnts[1].v2 * (1 - dst.y) + uvpnts[2].v2 * (1 - dst.z)) / 2;*/

        }

        public void AssignWeightedData (Vertex to, Vector3 weight) {
         
            to._color = vertexes[0]._color * weight.x + vertexes[1]._color * weight.y + vertexes[2]._color * weight.z;
            to.meshPoint.shadowBake = vertexes[0].meshPoint.shadowBake * weight.x + vertexes[1].meshPoint.shadowBake * weight.y + vertexes[2].meshPoint.shadowBake * weight.z;
            Vertex nearest = (Mathf.Max(weight.x, weight.y) > weight.z)  ? (weight.x > weight.y ? vertexes[0] : vertexes[1]) : vertexes[2];
            to.meshPoint.boneWeight = nearest.meshPoint.boneWeight; //boneWeight. * weight.x + uvpnts[1]._color * weight.y + uvpnts[2]._color * weight.z;
            //to.vert.submeshIndex = nearest.vert.submeshIndex;
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

        public int NotOnLineIndex(LineData l) {
            for (int i = 0; i < 3; i++)
                if ((vertexes[i].meshPoint != l.pnts[0].meshPoint) && (vertexes[i].meshPoint != l.pnts[1].meshPoint))
                    return i;

            return 0;
        }
        public Vertex NotOnLine(MeshPoint a, MeshPoint b)
        {
            for (int i = 0; i < 3; i++)
                if ((vertexes[i].meshPoint != a) && (vertexes[i].meshPoint != b))
                    return vertexes[i];

            return vertexes[0];
        }
        public Vertex NotOnLine(LineData l)
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
            // if (!includes(vrt)) Debug.Log("Error Using Merge Around");

            //Debug.Log("Using Merge Around");
            for (int i = 0; i < 3; i++)
            {
                if (!Includes(other.vertexes[i].meshPoint))
                {

                    Replace(GetByVert(vrt), other.vertexes[i]);
                    return;
                }
            }
            // Debug.Log("Done Merge Around");



        }

        public void MakeTriangleVertUnique(Vertex pnt)
        {
            // bool duplicant = false;

            /* for (int i = 0; i < _Mesh.triangles.Count; i++) {
                 trisDta other = _Mesh.triangles[i];
                 if ((other.includes(pnt)) && (other != tris))   {
                     duplicant = true;
                     break;
                 }
             }*/
            //if (!duplicant) return;

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

    [Serializable]
    public class LineData : PainterStuff
    {
        public Triangle triangle;
        public Vertex[] pnts = new Vertex[2];
        public int trianglesCount;

        public MeshPoint this[int index]
        {
            get { return pnts[index].meshPoint; }
        }

        public float LocalLength { get { return (this[0].localPos - this[1].localPos).magnitude; } }

        public float WorldSpaceLength { get { return (this[0].WorldPos - this[1].WorldPos).magnitude; } }

        public bool SameAsLastFrame { get { return this.Equals(EditedMesh.LastFramePointedLine); } }

        public bool Includes(Vertex uv) => ((uv == pnts[0]) || (uv == pnts[1]));
        
        public bool Includes(MeshPoint vp) => ((vp == pnts[0].meshPoint) || (vp == pnts[1].meshPoint));
        
        public bool SameVerticles(LineData other)
        {
            return (((other.pnts[0].meshPoint == pnts[0].meshPoint) && (other.pnts[1].meshPoint == pnts[1].meshPoint)) ||
                ((other.pnts[0].meshPoint == pnts[1].meshPoint) && (other.pnts[1].meshPoint == pnts[0].meshPoint)));
        }

        public LineData(MeshPoint a, MeshPoint b)
        {

            triangle = a.GetTriangleFromLine(b);
            pnts[0] = a.uvpoints[0];
            pnts[1] = b.uvpoints[0];
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
                foreach (Vertex uv in uv0.meshPoint.uvpoints)
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
                foreach (Vertex uv in uv0.meshPoint.uvpoints)
                {
                    foreach (Triangle tri in uv.tris)
                    {

                        if ((allTris.Contains(tri) == false) && tri.Includes(pnts[0].meshPoint) && (tri.Includes(pnts[1].meshPoint)))
                            allTris.Add(tri);

                    }
                }
            }
            //Debug.Log("Found "+allTris.Count+ " tris for line");

            return allTris;
        }

        public Vector3 Vector()
        {
            return pnts[1].Pos - pnts[0].Pos;
        }

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

            //Debug.Log("Vectors A "+ a + " and B "+ b);

            Vector2 fromVector2 = GridNavigator.Inst().InPlaneVector(a);
            Vector2 toVector2 = GridNavigator.Inst().InPlaneVector(b);

            // Debug.Log("Vectors2 A " + fromVector2 + " and B " + toVector2);

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

        public override int GetHashCode() {
            return pnts[0].finalIndex;
        }

    }

}