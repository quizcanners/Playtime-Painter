using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
#if UNITY_EDITOR
using UnityEditor;
#endif
//using TextureEditor;
using StoryTriggerData;
using PlayerAndEditorGUI;

namespace Playtime_Painter
{

    public static class MeshAnatomyExtensions {

        public static Vector3 SmoothVector (this List<trisDta> td) {

            var v = Vector3.zero;

            foreach (var t in td)
                v += t.sharpNormal;

            return v.normalized;

        }

    }

    public class UVpoint : PainterStuffKeepUnrecognized_STD
    {

        public override string getDefaultTagName() {
            return stdTag_uv;
        }
        public const string stdTag_uv = "uv";
        
        protected PlaytimePainter target { get { return MeshManager.inst.target; } }

        public int uvIndex;
        public int finalIndex;
        public Color _color;

        public bool tmpMark;
        public bool HasVertex;
        public List<trisDta> tris = new List<trisDta>();
        public UVpoint MyLastCopy;
        public vertexpointDta vert;

        public bool sameAsLastFrame { get { return this == editedMesh.LastFramePointedUV; } }

        public Vector3 pos { get { return vert.localPos; } }
        
        void Init(vertexpointDta nvert)
        {
            vert = nvert;
            nvert.uvpoints.Add(this);
        }

        void Init (UVpoint other) {
            _color = other._color;
          
        }
        
        public override stdEncoder Encode() {
            var cody = new stdEncoder();

            cody.Add("i", finalIndex);
            cody.Add("uvi", uvIndex);
            cody.Add("col", _color);


            return cody;
        }

        public override bool Decode(string tag, string data) {
            switch (tag) {
                case "i": finalIndex = data.ToInt(); editedMesh.uvsByFinalIndex[finalIndex] = this; break;
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

        public UVpoint GetConnectedUVinVert(vertexpointDta other)
        {
            foreach (trisDta t in tris)
            {
                if (t.includes(other))
                    return (t.GetByVert(other));
            }
            return null;
        }

        public List<trisDta> getTrianglesFromLine(UVpoint other) {
            List<trisDta> lst = new List<trisDta>();

                foreach (var t in tris)
                    if (t.includes(other)) lst.Add(t);
            
            return lst;
        }

        public UVpoint() {
            Init(vertexpointDta.currentlyDecoded);
        }

        public UVpoint(UVpoint other)
        {
            Init(other);
            Init(other.vert);
            uvIndex = other.uvIndex;
            
        }

        public UVpoint(vertexpointDta nvert)
        {
            Init(nvert);

            if (vert.shared_v2s.Count == 0)
                SetUVindexBy(Vector2.one * 0.5f, Vector2.one * 0.5f);
            else
                uvIndex = 0;
        }

        public UVpoint(vertexpointDta nvert, Vector2 uv_0)
        {
            Init(nvert);
          
            SetUVindexBy(uv_0, Vector2.zero);
        }

        public UVpoint(vertexpointDta nvert, Vector2 uv_0, Vector2 uv_1)
        {
            Init(nvert);
            SetUVindexBy(uv_0, uv_1);
        }

        public UVpoint(vertexpointDta nvert, UVpoint other)
        {
            Init(other);
            Init(nvert);
            SetUVindexBy(other.GetUV(0), other.GetUV(1));
        }

        public UVpoint(vertexpointDta nvert, string data) {
            Init(nvert);
            Decode(data);
        }

        public void AssignToNewVertex(vertexpointDta vp) {
            Vector2[] myUV = vert.shared_v2s[uvIndex];
            vert.uvpoints.Remove(this);
            vert = vp;
            vert.uvpoints.Add(this);
            SetUVindexBy(myUV);
        }

        public Vector2 editedUV {
            get { return vert.shared_v2s[uvIndex][meshMGMT.editedUV]; }
            set { SetUVindexBy(value); }
        }

        public Vector2 sharedEditedUV {
            get { return vert.shared_v2s[uvIndex][meshMGMT.editedUV]; }
            set { vert.shared_v2s[uvIndex][meshMGMT.editedUV] = value; }
        }

        public Vector2 GetUV(int ind) {
            return vert.shared_v2s[uvIndex][ind];
        }

        public bool SameUV(Vector2 uv, Vector2 uv1)
        {
            return (((uv - GetUV(0)).magnitude < 0.0000001f) && ((uv1 - GetUV(1)).magnitude < 0.0000001f));
        }

        public void SetUVindexBy(Vector2[] uvs)
        {
            uvIndex = vert.getIndexFor(uvs[0], uvs[1]);
        }

        public void SetUVindexBy(Vector2 uv_0, Vector2 uv_1)
        {
            uvIndex = vert.getIndexFor(uv_0, uv_1);
        }

        public void SetUVindexBy(Vector2 uv_edited) {
            var uv_0 = meshMGMT.editedUV == 0 ? uv_edited : GetUV(0);
            var uv_1 = meshMGMT.editedUV == 1 ? uv_edited : GetUV(1);

            uvIndex = vert.getIndexFor(uv_0, uv_1);
        }

        public bool ConnectedTo(vertexpointDta other)
        {
            foreach (trisDta t in tris)
                if (t.includes(other))
                    return true;

            return false;
        }

        public bool ConnectedTo(UVpoint other)
        {
            foreach (trisDta t in tris)
                if (t.includes(other))
                    return true;

            return false;
        }

        public void setColor_OppositeTo(ColorChanel chan)
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
                foreach (UVpoint u in vert.uvpoints) if (u != this)
                    {
                        ch = GetZeroChanelIfOne(ref count);
                        if (count == 2) return ch;
                    }
            }


            return ColorChanel.A;
        }

        public static implicit operator int(UVpoint d)   {
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
        public override stdEncoder Encode() {
            var cody = new stdEncoder(); 

            cody.AddIfNotZero("p", deltaPosition);
            cody.AddIfNotZero("t", deltaTangent);
            cody.AddIfNotZero("n", deltaNormal);

            return cody;
        }

        public override string getDefaultTagName()
        {
            return tagName_bs;
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

        public const string tagName_bs = "bs";    
  
    } 

    public class vertexpointDta : PainterStuffKeepUnrecognized_STD
    {

        // TEMPORATY DATA / NEEDS MANUAL UPDATE:
        public Vector3 normal;
        public int index;
        public bool NormalIsSet;
        public float distanceToPointed;
        public Vector3 distanceToPointedV3;
        public static vertexpointDta currentlyDecoded;

        // Data to save:
        public List<Vector2[]> shared_v2s = new List<Vector2[]>();
        public List<UVpoint> uvpoints;
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

        public bool sameAsLastFrame { get { return this == editedMesh.LastFramePointedUV.vert; } }

        public bool SetSmoothNormal(bool to)
        {
            if (to == SmoothNormal)
                return false;
            SmoothNormal = to;
            return true;
        }

        public Vector3 worldPos { get {
                PlaytimePainter emc = MeshManager.inst.target;
                if (emc.AnimatedVertices())   {
                    int animNo = emc.GetVertexAnimationNumber();
                    return emc.transform.TransformPoint(localPos + anim[animNo]);
                }

                return emc.transform.TransformPoint(localPos);
            } 

            set {
              localPos =  meshMGMT.target.transform.InverseTransformPoint(value);
            }

        }

        public Vector3 GetWorldNormal() {
            return meshMGMT.target.transform.TransformDirection(GetNormal());
        }

        public Vector3 GetNormal() {
            normal = Vector3.zero;
            foreach (var u in uvpoints)
                foreach (var t in u.tris) 
                normal += t.GetNormal();
            
            normal.Normalize();

            return normal;
        }

        public override stdEncoder Encode() {
            var cody = new stdEncoder();

            foreach (var lst in shared_v2s) {
                cody.Add("u0", lst[0]);
                cody.Add("u1", lst[1]);
            }

            cody.AddIfNotEmpty("uvs", uvpoints);

            cody.Add("pos", localPos);

            cody.Add("smth", SmoothNormal);

            cody.Add("shad", shadowBake);

            cody.Add("bw", boneWeight);

            cody.Add("biP", bindPoses);

            cody.Add("edge", edgeStrength);

            if (shapes != null)
                cody.AddIfNotEmpty(BlendFrame.tagName_bs, shapes);

            cody.AddIfNotZero("gr", vertexGroup);
          
            return cody;
        }
       
        public override bool Decode(string tag, string data) {
            switch (tag) {
                case "u0":  shared_v2s.Add(new Vector2[2]); 
                            shared_v2s.last()[0] = data.ToVector2(); break;
                case "u1":  shared_v2s.last()[1] = data.ToVector2(); break;
                case "uvs": currentlyDecoded = this;
                            uvpoints = data.ToListOf_STD<UVpoint>(); break;
                case "pos": localPos = data.ToVector3(); break;
                case "smth": SmoothNormal = data.ToBool(); break;
                case "shad": shadowBake = data.ToVector4(); break;
                case "bw": boneWeight = data.ToBoneWeight(); break;
                case "biP": bindPoses = data.ToMatrix4x4(); break;
                case "edge":  edgeStrength = data.ToFloat(); break;
                case BlendFrame.tagName_bs: shapes = data.ToListOfList_STD<BlendFrame>(); break;
                case "gr": vertexGroup = data.ToInt(); break;
                default: return false;
            }
            return true;
        }

        public override string getDefaultTagName() { return stdTag_vrt;}

        public const string stdTag_vrt = "vrt";

        public int getIndexFor(Vector2 uv_0, Vector2 uv_1) {
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

            if (dest.magnitude > 0) editedMesh.hasFrame[no] = true;
        }
        
        public vertexpointDta() {
            Reboot(Vector3.zero);
        }

        public vertexpointDta(Vector3 npos) {
            Reboot(npos);
        }

        public void PixPerfect() {
            var trg = MeshManager.inst.target;

            if ((trg!= null) && (trg.imgData!= null)){
                var id = trg.imgData;
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
            uvpoints = new List<UVpoint>();

            SmoothNormal = cfg.newVerticesSmooth;
        }

        public void clearColor(BrushMask bm) {
            foreach (UVpoint uvi in uvpoints)
                bm.Transfer(ref uvi._color, Color.black);
        }

        void SetChanel(ColorChanel chan, vertexpointDta other, float val)
        {
            foreach (UVpoint u in uvpoints)
                if (u.ConnectedTo(other))
                    chan.SetChanel(ref u._color, val);
        }

        public bool FlipChanelOnLine(ColorChanel chan, vertexpointDta other)
        {
            float val = 1;

            if (cfg.MakeVericesUniqueOnEdgeColoring)
               editedMesh.GiveLineUniqueVerticles_RefreshTrisListing(new LineData(this, other));

            foreach (UVpoint u in uvpoints)
                if (u.ConnectedTo(other))
                    val *= u._color.GetChanel(chan) * u.GetConnectedUVinVert(other)._color.GetChanel(chan);

            val = (val > 0.9f) ? 0 : 1;

            SetChanel(chan, other, val);
            other.SetChanel(chan, this, val);



            editedMesh.dirty = true;


            return (val == 1);
        }

        public void SetColorOnLine(Color col, BrushMask bm, vertexpointDta other)
        {
            foreach (UVpoint u in uvpoints)
                if (u.ConnectedTo(other))
                    bm.Transfer(ref u._color, col);   //val *= u._color.GetChanel01(chan) * u.GetConnectedUVinVert(other)._color.GetChanel01(chan);

        }

        public void RemoveBorderFromLine(vertexpointDta other)
        {
            foreach (UVpoint u in uvpoints)
                if (u.ConnectedTo(other))
                    for (int i = 0; i < 4; i++)
                    {
                        UVpoint ouv = u.GetConnectedUVinVert(other);
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
        public float DistanceTo (vertexpointDta other) {
            return (localPos - other.localPos).magnitude;
        }

        public void MergeWith (vertexpointDta other) {

            for (int i = 0; i < other.uvpoints.Count; i++) {
                UVpoint buv = other.uvpoints[i];
                Vector2[] uvs = new Vector2[] { buv.GetUV(0), buv.GetUV(1) };
                uvpoints.Add(buv);
                buv.vert = this;
                buv.SetUVindexBy(uvs);
            }

            editedMesh.vertices.Remove(other);

        }

        public void MergeWithNearest() {

            List<vertexpointDta> vrts = editedMesh.vertices;

            vertexpointDta nearest = null;
            float maxDist = float.MaxValue;

            foreach (vertexpointDta v in vrts) {
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

        public List<trisDta> triangles()
        {
            List<trisDta> Alltris = new List<trisDta>();

            foreach (UVpoint uvi in uvpoints)
                foreach (trisDta tri in uvi.tris)
                    //if (!Alltris.Contains(tri))
                        Alltris.Add(tri);
            
            return Alltris;
        }

        public bool AllPointsUnique() {
            return (triangles().Count <= uvpoints.Count);
        }

        public List<LineData> GetAllLines_USES_Tris_Listing()
        {
            List<LineData> Alllines = new List<LineData>();


            foreach (UVpoint uvi in uvpoints)
            {
                foreach (trisDta tri in uvi.tris)
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

        public trisDta getTriangleFromLine(vertexpointDta other)
        {
            for (int i = 0; i < uvpoints.Count; i++)
            {
                for (int g = 0; g < uvpoints[i].tris.Count; g++)
                    if (uvpoints[i].tris[g].includes(other)) return uvpoints[i].tris[g];
            }
            return null;
        }

        public List<trisDta> getTrianglesFromLine(vertexpointDta other) {
            List<trisDta> lst = new List<trisDta>();
            for (int i = 0; i < uvpoints.Count; i++) {
                foreach (var t in uvpoints[i].tris) 
                    if (t.includes(other)) lst.Add(t);
            }
            return lst;
        }

        public bool SetAllUVsShared()
        {
            if (uvpoints.Count == 1)
                return false;

            while (uvpoints.Count > 1)
                if (!editedMesh.MoveTris(uvpoints[1], uvpoints[0])) {
                    break;
                }

            return true;
        }
    }

    [Serializable]
    public class trisDta : PainterStuffKeepUnrecognized_STD
    {
        public vertexpointDta this[int index] {
            get { return uvpnts[index].vert; }
        }


        public UVpoint[] uvpnts = new UVpoint[3];
        public bool[] DominantCourner = new bool[3];
        public float[] edgeWeight = new float[3];
        public Vector4 textureNo = new Vector4();
        public int submeshIndex;
        public Vector3 sharpNormal;

        public Vector3 GetNormal() {

            sharpNormal = MyMath.GetNormalOfTheTriangle(
                    uvpnts[0].pos,
                    uvpnts[1].pos,
                    uvpnts[2].pos);

            return sharpNormal;
        }

        public bool sameAsLastFrame { get { return this == editedMesh.LastFramePointedTris; } }

        public float area { get
            {
                return Vector3.Cross(uvpnts[0].pos - uvpnts[1].pos, uvpnts[0].pos - uvpnts[2].pos).magnitude * 0.5f;
               // V.magnitude * 0.5f;



            } }

        public override stdEncoder Encode() {
            var cody = new stdEncoder();

            cody.AddIfTrue("f0", DominantCourner[0]);
            cody.AddIfTrue("f1", DominantCourner[1]);
            cody.AddIfTrue("f2", DominantCourner[2]);

            cody.Add("ew0", edgeWeight[0]);
            cody.Add("ew1", edgeWeight[1]);
            cody.Add("ew2", edgeWeight[2]);

            for (int i = 0; i < 3; i++)
                cody.Add(i.ToString(),uvpnts[i].finalIndex);

            cody.Add("tex", textureNo);

            cody.AddIfNotZero("sub", submeshIndex);
           

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
                case "0": uvpnts[0] = editedMesh.uvsByFinalIndex[data.ToInt()]; break;
                case "1": uvpnts[1] = editedMesh.uvsByFinalIndex[data.ToInt()]; break;
                case "2": uvpnts[2] = editedMesh.uvsByFinalIndex[data.ToInt()]; break;
                default: return false;
            }
            return true;

        }

        public override string getDefaultTagName() {
            return stdTag_tri;
        }

        public const string stdTag_tri = "tri";

        public trisDta CopySettingsFrom (trisDta td) {
            for (int i = 0; i < 3; i++)
                DominantCourner[i] = td.DominantCourner[i];
            textureNo = td.textureNo;
            submeshIndex = td.submeshIndex;

            return this;
        }

        public bool wasProcessed;

        public bool isVertexIn(vertexpointDta vrt)
        {
            foreach (UVpoint v in uvpnts)
            {
                if (v.vert == vrt) return true;
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
            UVpoint hold = uvpnts[0];

            uvpnts[0] = uvpnts[2];
            uvpnts[2] = hold;
        }

        public bool IsSamePoints(UVpoint[] other)
        {
            foreach (UVpoint v in other)
            {
                bool same = false;
                foreach (UVpoint v1 in uvpnts)
                {
                    if (v.vert == v1.vert) same = true;
                }
                if (!same) return false;
            }
            return true;
        }

        public bool IsSameUV(UVpoint[] other)
        {
            foreach (UVpoint v in other)
            {
                bool same = false;
                foreach (UVpoint v1 in uvpnts)
                {
                    if (v == v1) same = true;
                }
                if (!same) return false;
            }
            return true;
        }

        public bool IsSameAs(vertexpointDta[] other)
        {
            foreach (vertexpointDta v in other)
            {
                bool same = false;
                foreach (UVpoint v1 in uvpnts)
                {
                    if (v == v1.vert) same = true;
                }
                if (!same) return false;
            }
            return true;
        }

        public bool isNeighbourOf(trisDta td) {
            if (td == this) return false;

            int same = 0;

            foreach (var u in td.uvpnts)
                for (int i = 0; i < 3; i++)
                    if (uvpnts[i].vert == u.vert) { same++; break; }

            return same == 2;
        }

        public void Change(UVpoint[] nvrts)
        {
            for (int i = 0; i < 3; i++)
           // {
               // nvrts[i].editedUV = uvpnts[i].editedUV;
                uvpnts[i] = nvrts[i];
            //}
        }

        public bool includes(UVpoint vrt)
        {
            for (int i = 0; i < 3; i++)
                if (vrt == uvpnts[i]) return true;
            return false;
        }

        public bool includes(vertexpointDta vrt)
        {
            for (int i = 0; i < 3; i++)
                if (vrt == uvpnts[i].vert) return true;
            return false;
        }

        public bool includes(vertexpointDta a, vertexpointDta b)
        {
            int cnt = 0;
            for (int i = 0; i < 3; i++)
            {
                if ((a == uvpnts[i].vert) || (b == uvpnts[i].vert)) cnt++;
            }
            return cnt > 1;
        }

        public bool includes(LineData ld)
        {
            return (includes(ld.pnts[0].vert) && (includes(ld.pnts[1].vert)));
        }

        public bool PointOnTriangle()
        {
            Vector3 va = uvpnts[0].vert.distanceToPointedV3;//point.DistanceV3To(uvpnts[0].pos);
            Vector3 vb = uvpnts[1].vert.distanceToPointedV3;//point.DistanceV3To(uvpnts[1].pos);
            Vector3 vc = uvpnts[2].vert.distanceToPointedV3;//point.DistanceV3To(uvpnts[2].pos);

            float sum = Vector3.Angle(va, vb) + Vector3.Angle(va, vc) + Vector3.Angle(vb, vc);
            return (Mathf.Abs(sum - 360) < 0.01f);
        }

        public int NumberOf(UVpoint pnt)
        {
            for (int i = 0; i < 3; i++)
                if (pnt == uvpnts[i]) return i;

            return -1;
        }

        public UVpoint GetClosestTo(Vector3 fpos)
        {

            UVpoint nearest = uvpnts[0];
            for (int i = 1; i < 3; i++)
                if ((fpos - uvpnts[i].pos).magnitude < (fpos - nearest.pos).magnitude) nearest = uvpnts[i];

            return nearest;

        }

        public UVpoint GetByVert(vertexpointDta vrt)
        {
            for (int i = 0; i < 3; i++)
                if (uvpnts[i].vert == vrt) return uvpnts[i];

            Debug.Log("Error using Get By Vert");
            return null;//uvpnts[0];
        }

        public Vector2 LocalPosToEditedUV(Vector3 localPos)
        {
            Vector3 w = DistanceToWeight(localPos);
            var ind = meshMGMT.editedUV;
            return uvpnts[0].GetUV(ind) * w.x + uvpnts[1].GetUV(ind) * w.y + uvpnts[2].GetUV(ind) * w.z;
        }

        public Vector3 DistanceToWeight(Vector3 localPos)
        {

            Vector3 p1 = uvpnts[0].pos;
            Vector3 p2 = uvpnts[1].pos;
            Vector3 p3 = uvpnts[2].pos;

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

        public void AssignWeightedData (UVpoint to, Vector3 weight) {
         
            to._color = uvpnts[0]._color * weight.x + uvpnts[1]._color * weight.y + uvpnts[2]._color * weight.z;
            to.vert.shadowBake = uvpnts[0].vert.shadowBake * weight.x + uvpnts[1].vert.shadowBake * weight.y + uvpnts[2].vert.shadowBake * weight.z;
            UVpoint nearest = (Mathf.Max(weight.x, weight.y) > weight.z)  ? (weight.x > weight.y ? uvpnts[0] : uvpnts[1]) : uvpnts[2];
            to.vert.boneWeight = nearest.vert.boneWeight; //boneWeight. * weight.x + uvpnts[1]._color * weight.y + uvpnts[2]._color * weight.z;
            //to.vert.submeshIndex = nearest.vert.submeshIndex;
        }

        public void Replace(UVpoint point, UVpoint with)
        {
            for (int i = 0; i < 3; i++)
                if (uvpnts[i] == point)
                {
                    uvpnts[i] = with;
                    return;
                }

        }

        public void Replace(int i, UVpoint with)
        {
            uvpnts[i].tris.Remove(this);
            uvpnts[i] = with;
            with.tris.Add(this);

        }

        public int NotOnLineIndex(LineData l) {
            for (int i = 0; i < 3; i++)
                if ((uvpnts[i].vert != l.pnts[0].vert) && (uvpnts[i].vert != l.pnts[1].vert))
                    return i;

            return 0;
        }
        public UVpoint NotOnLine(vertexpointDta a, vertexpointDta b)
        {
            for (int i = 0; i < 3; i++)
                if ((uvpnts[i].vert != a) && (uvpnts[i].vert != b))
                    return uvpnts[i];

            return uvpnts[0];
        }
        public UVpoint NotOnLine(LineData l)
        {
            for (int i = 0; i < 3; i++)
                if ((uvpnts[i].vert != l.pnts[0].vert) && (uvpnts[i].vert != l.pnts[1].vert))
                    return uvpnts[i];

            return uvpnts[0];
        }
        public UVpoint NotOneOf(UVpoint[] others)
        {
            for (int i = 0; i < 3; i++)
            {
                bool same;
                same = false;
                foreach (UVpoint uvi in others)
                    if (uvi.vert == uvpnts[i].vert) same = true;

                if (!same) return uvpnts[i];
            }
            return null;
        }
        
        public bool GiveUniqueVerticesAgainst(trisDta td)
        {
            bool changed = false;
            for (int i = 0; i < 3; i++)
            {
                UVpoint u = uvpnts[i];

                if (td.includes(u))
                {
                    uvpnts[i] = new UVpoint(u.vert);
                    changed = true;
                }
            }

            return changed;
        }

        public void MergeAround(trisDta other, vertexpointDta vrt)
        {
            // if (!includes(vrt)) Debug.Log("Error Using Merge Around");

            //Debug.Log("Using Merge Around");
            for (int i = 0; i < 3; i++)
            {
                if (!includes(other.uvpnts[i].vert))
                {

                    Replace(GetByVert(vrt), other.uvpnts[i]);
                    return;
                }
            }
            // Debug.Log("Done Merge Around");



        }

        public void MakeTriangleVertUnique(UVpoint pnt)
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

            UVpoint nuv = new UVpoint(pnt.vert, pnt);


            Replace(pnt, nuv);

            editedMesh.dirty = true;


        }
        
        public trisDta NewForCopiedVerticles()
        {
            UVpoint[] nvpnts = new UVpoint[3];

            for (int i = 0; i < 3; i++)
            {
                if (uvpnts[i].MyLastCopy == null) { Debug.Log("Error: UV has not been copied!"); return null; }

                nvpnts[i] = uvpnts[i].MyLastCopy;
            }

            return new trisDta(nvpnts);

        }

        public trisDta(UVpoint[] nvrts)
        {
            for (int i = 0; i < 3; i++)
            {
                uvpnts[i] = nvrts[i];
                uvpnts[i].tris.Add(this);
            }
        }

        public trisDta() {
        }

        public LineData[] GetLinesFor(UVpoint pnt)
        {

            LineData[] ld = new LineData[2];
            int no = NumberOf(pnt);
            ld[0] = new LineData(this, new UVpoint[] { uvpnts[no], uvpnts[(no + 1) % 3] });
            ld[1] = new LineData(this, new UVpoint[] { uvpnts[(no + 2) % 3], uvpnts[no] });

            return ld;


        }

        public List<trisDta> GetNeighboringTriangles() {
            var lst = new List<trisDta>();

            foreach (var u in uvpnts)
                foreach (var t in u.tris)
                    if (!lst.Contains(t) && t.isNeighbourOf(this))
                        lst.Add(t);

            return lst;
        }

        public List<trisDta> GetNeighboringTrianglesUnprocessed(){
            var lst = new List<trisDta>();

            foreach (var u in uvpnts)
                foreach (var t in u.vert.triangles())
                    if (!t.wasProcessed && !lst.Contains(t) && t.isNeighbourOf(this))
                        lst.Add(t);
            
            return lst;
        }

        public LineData LineWith(trisDta other) {

            List<vertexpointDta> l = new List<vertexpointDta>(); //= new LineData();
            

            foreach (var u in uvpnts)
                foreach (var u2 in other.uvpnts)
                    if (u.vert == u2.vert) {
                        l.Add(u.vert);
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
        public trisDta triangle;
        public UVpoint[] pnts = new UVpoint[2];
        public int trianglesCount;

        public vertexpointDta this[int index]
        {
            get { return pnts[index].vert; }
        }

        public float localLength { get { return (this[0].localPos - this[1].localPos).magnitude; } }

        public float worldSpaceLength { get { return (this[0].worldPos - this[1].worldPos).magnitude; } }

        public bool sameAsLastFrame { get { return this.Equals(editedMesh.LastFramePointedLine); } }

        public bool includes(UVpoint uv)
        {
            return ((uv == pnts[0]) || (uv == pnts[1]));
        }

        public bool includes(vertexpointDta vp)
        {
            return ((vp == pnts[0].vert) || (vp == pnts[1].vert));
        }

        public bool SameVerticles(LineData other)
        {
            return (((other.pnts[0].vert == pnts[0].vert) && (other.pnts[1].vert == pnts[1].vert)) ||
                ((other.pnts[0].vert == pnts[1].vert) && (other.pnts[1].vert == pnts[0].vert)));
        }

        public LineData(vertexpointDta a, vertexpointDta b)
        {

            triangle = a.getTriangleFromLine(b);
            pnts[0] = a.uvpoints[0];
            pnts[1] = b.uvpoints[0];
            trianglesCount = 0;
        }

        public LineData(trisDta tri, UVpoint[] npoints)
        {
            triangle = tri;
            pnts[0] = npoints[0];
            pnts[1] = npoints[1];
            trianglesCount = 0;
        }

        public LineData(trisDta tri, UVpoint a, UVpoint b)
        {
            triangle = tri;
            pnts[0] = a;
            pnts[1] = b;

            trianglesCount = 0;
        }

        public trisDta getOtherTriangle() {
            foreach (UVpoint uv0 in pnts)
                foreach (UVpoint uv in uv0.vert.uvpoints)
                    foreach (trisDta tri in uv.tris)
                        if (tri != triangle && tri.includes(pnts[0].vert) && tri.includes(pnts[1].vert))
                            return tri;


            return null;
            
        }

        public List<trisDta> getAllTriangles_USES_Tris_Listing()
        {
            List<trisDta> allTris = new List<trisDta>();

            foreach (UVpoint uv0 in pnts)
            {
                foreach (UVpoint uv in uv0.vert.uvpoints)
                {
                    foreach (trisDta tri in uv.tris)
                    {

                        if ((allTris.Contains(tri) == false) && tri.includes(pnts[0].vert) && (tri.includes(pnts[1].vert)))
                            allTris.Add(tri);

                    }
                }
            }
            //Debug.Log("Found "+allTris.Count+ " tris for line");

            return allTris;
        }

        public Vector3 Vector()
        {
            return pnts[1].pos - pnts[0].pos;
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

            Vector3 a = LineA.pnts[0].pos - LineA.pnts[1].pos;
            Vector3 b = LineB.pnts[1].pos - LineB.pnts[0].pos;

            //Debug.Log("Vectors A "+ a + " and B "+ b);

            Vector2 fromVector2 = GridNavigator.inst().InPlaneVector(a);
            Vector2 toVector2 = GridNavigator.inst().InPlaneVector(b);

            // Debug.Log("Vectors2 A " + fromVector2 + " and B " + toVector2);

            Vector2 mid = (fromVector2.normalized + toVector2.normalized).normalized;

            Vector3 cross = Vector3.Cross(fromVector2, toVector2);

            if (cross.z > 0)
                mid = -mid;




            return GridNavigator.inst().PlaneToWorldVector(mid).normalized;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(LineData))
                return false;

            LineData ld = (LineData)obj;

            return (ld.pnts[0].vert == pnts[0].vert && ld.pnts[1].vert == pnts[1].vert) 
                || (ld.pnts[0].vert == pnts[1].vert && ld.pnts[1].vert == pnts[0].vert);

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
            var tris = getAllTriangles_USES_Tris_Listing();

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