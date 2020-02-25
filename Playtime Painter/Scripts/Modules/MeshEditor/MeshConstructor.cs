using System.Collections.Generic;
using QuizCannersUtilities;
using UnityEngine;

namespace PlaytimePainter.MeshEditing {

    public class MeshConstructor {

        public List<int>[] triangles;
        public uint[] baseVertex;

        private Vector4[] _perVertexTriangleTexture;
        private Vector2[] _uvs;
        private Vector2[] _uvs1;

        private Vector3[] _position;
        private Vector3[] _normals;
        private Vector3[] _sharpNormals;

        private BoneWeight[] _boneWeights;
        //private Matrix4x4[] _bindPoses;

        public Vector4[] tangents;

        private Vector3[] _edgeNormal0;
        private Vector3[] _edgeNormal1;
        private Vector3[] _edgeNormal2;

        private Color[] _colors;
        private Vector4[] _edgeData;
        private Vector3[] _edgeWeightedOnly;
        private Vector4[] _shadowBake;
        private int[] _originalIndex;

        public MeshPackagingProfile profile;

        public EditableMesh edMesh;

        public Mesh mesh;

        public int vertexCount;

        public Color[] Colors {
            get {
                if (_colors == null) {
                    _colors = new Color[vertexCount];
                    foreach (var vp in edMesh.meshPoints)
                        foreach (var uvi in vp.vertices)
                            _colors[uvi] = uvi.color;
                }
                return _colors;
            }
        }

        public Vector4[] ShadowBake {
            get {
                if (_shadowBake != null) return _shadowBake;

                _shadowBake = new Vector4[vertexCount];
                foreach (var vp in edMesh.meshPoints)
                foreach (var uvi in vp.vertices)
                    _shadowBake[uvi] = vp.shadowBake;
                return _shadowBake;
            }
        }

        public Vector2[] Uv { get {
                if (_uvs != null) return _uvs;

                _uvs = new Vector2[vertexCount];
                foreach (var point in edMesh.meshPoints)
                foreach (var uvi in point.vertices)
                    _uvs[uvi.finalIndex] = uvi.GetUv(0);
                return _uvs;
            }
        }

        public Vector2[] Uv1 {
            get {
                if (_uvs1 != null) return _uvs1;

                _uvs1 = new Vector2[vertexCount];
                foreach (var vp in edMesh.meshPoints)
                foreach (var uvi in vp.vertices)
                    _uvs1[uvi] = uvi.GetUv(1);
                return _uvs1;
            }
        }

        public Vector4[] TriangleTextures {
            get {
                if (_perVertexTriangleTexture != null) return _perVertexTriangleTexture;

                _perVertexTriangleTexture = new Vector4[vertexCount];

                foreach (var tri in edMesh.triangles) 
                    for (var no = 0; no < 3; no++)
                        _perVertexTriangleTexture[tri.vertexes[no]] = tri.textureNo;
                
                return _perVertexTriangleTexture;
            }
        }

        public Vector4[] EdgeData {
            get {
                if (_edgeData != null) return _edgeData;
                _edgeData = new Vector4[vertexCount];

                foreach (var tri in edMesh.triangles) {
                    for (var no = 0; no < 3; no++) {
                        var up = tri.vertexes[no];
                        float edge = (up.triangles.Count == 1) ? 1 : 0;
                        _edgeData[up] = new Vector4(no == 0 ? 0 : edge, no == 1 ? 0 : edge, no == 2 ? 0 : edge, up.meshPoint.edgeStrength);
                    }
                }
                return _edgeData;
            }
        }

        public Vector3[] EdgeDataByWeight
        {
            get
            {

                if (_edgeWeightedOnly != null) return _edgeWeightedOnly;

                _edgeWeightedOnly = new Vector3[vertexCount];

                foreach (var tri in edMesh.triangles)
                    for (var no = 0; no < 3; no++)
                    {
                        var up = tri.vertexes[no];

                        // If other triangles of the point 

                        // A weight for line 1-2 is in position 3, for 2-3 in 1 and so on.
                        var ew = tri.edgeWeight;


                        var weight = new Vector3(
                            no == 0 ? 0 : ew[0]
                            , no == 1 ? 0 : ew[1]
                            , no == 2 ? 0 : ew[2]
                        );

                        if (weight.magnitude < 0.3f)
                            weight[(no + 1) % 3] = up.meshPoint.edgeStrength;

                        _edgeWeightedOnly[up] = weight;

                    }
                return _edgeWeightedOnly;
            }
        }

        public Vector3[] Normals { get { if (_normals == null) GenerateNormals(); return _normals; } }

        public Vector3[] SharpNormals { get { if (_sharpNormals == null) GenerateNormals(); return _sharpNormals; } }

        public Vector3[] EdgeNormal0OrSharp {
            get {
                if (_edgeNormal0 != null) return _edgeNormal0;

                _edgeNormal0 = new Vector3[vertexCount];

                var sn = SharpNormals;

                foreach (var tri in edMesh.triangles)
                {
                    var up = tri.vertexes[0];
                    _edgeNormal0[up] = sn[up];

                    var up1 = tri.vertexes[1];
                    var up2 = tri.vertexes[2];

                    var tris = up1.meshPoint.GetTrianglesFromLine(up2.meshPoint);

                    var nrm = tris.SmoothVector();

                    _edgeNormal0[up1] = nrm;
                    _edgeNormal0[up2] = nrm;
                }
                return _edgeNormal0;
            }
        }

        public Vector3[] EdgeNormal1OrSharp {
            get {
                if (_edgeNormal1 != null) return _edgeNormal1;
                _edgeNormal1 = new Vector3[vertexCount];

                var sn = SharpNormals;

                foreach (var tri in edMesh.triangles)
                {
                    var up = tri.vertexes[1];
                    _edgeNormal1[up] = sn[up];

                    var up0 = tri.vertexes[0];
                    var up2 = tri.vertexes[2];

                    var tris = up0.meshPoint.GetTrianglesFromLine(up2.meshPoint);

                    var nrm = tris.SmoothVector();

                    _edgeNormal1[up0] = nrm;
                    _edgeNormal1[up2] = nrm;
                }
                return _edgeNormal1;
            }
        }

        public Vector3[] EdgeNormal2OrSharp {
            get
            {
                if (_edgeNormal2 != null) return _edgeNormal2;

                _edgeNormal2 = new Vector3[vertexCount];

                var sn = SharpNormals;

                foreach (var tri in edMesh.triangles) {
                    var up = tri.vertexes[2];
                    _edgeNormal2[up] = sn[up];

                    var up0 = tri.vertexes[0];
                    var up1 = tri.vertexes[1];

                    var tris = up0.meshPoint.GetTrianglesFromLine(up1.meshPoint);

                    var nrm = tris.SmoothVector();

                    _edgeNormal2[up0] = nrm;
                    _edgeNormal2[up1] = nrm;
                }
                return _edgeNormal2;
            }
        }

        public Vector4[] Tangents
        {
            get
            {
                if (tangents != null) return tangents;
                tangents = new Vector4[vertexCount];
                var tan1 = new Vector3[vertexCount];
                var tan2 = new Vector3[vertexCount];

                var tri = 0;

                foreach (var t in edMesh.triangles)
                {

                    var i1 = t.vertexes[0];
                    var i2 = t.vertexes[1];
                    var i3 = t.vertexes[2];

                    var v1 = t.vertexes[0].LocalPos;
                    var v2 = t.vertexes[1].LocalPos;
                    var v3 = t.vertexes[2].LocalPos;

                    var w1 = t.vertexes[0].GetUv(0);// texcoords[i1];
                    var w2 = t.vertexes[1].GetUv(0);
                    var w3 = t.vertexes[2].GetUv(0);

                    var x1 = v2.x - v1.x;
                    var x2 = v3.x - v1.x;
                    var y1 = v2.y - v1.y;
                    var y2 = v3.y - v1.y;
                    var z1 = v2.z - v1.z;
                    var z2 = v3.z - v1.z;

                    var s1 = w2.x - w1.x;
                    var s2 = w3.x - w1.x;
                    var t1 = w2.y - w1.y;
                    var t2 = w3.y - w1.y;

                    var r = 1.0f / (s1 * t2 - s2 * t1);
                    var sDir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
                    var tDir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

                    tan1[i1] += sDir;
                    tan1[i2] += sDir;
                    tan1[i3] += sDir;

                    tan2[i1] += tDir;
                    tan2[i2] += tDir;
                    tan2[i3] += tDir;

                    tri += 3;

                }

                for (var i = 0; i < (vertexCount); i++)
                {

                    var n = _normals[i];
                    var t = tan1[i];

                    // Gram-Schmidt orthogonalize
                    Vector3.OrthoNormalize(ref n, ref t);

                    tangents[i].x = t.x;
                    tangents[i].y = t.y;
                    tangents[i].z = t.z;

                    // Calculate handedness
                    tangents[i].w = (Vector3.Dot(Vector3.Cross(n, t), tan2[i]) < 0.0f) ? -1.0f : 1.0f;

                }

                return tangents;
            }
        }

        public Vector3[] Position {
            get  {
                if (_position != null) return _position;

                _position = new Vector3[vertexCount];

                float totalSize = 0;

                foreach (var vp in edMesh.meshPoints) {
                    totalSize += vp.localPos.magnitude;
                    var lp = vp.localPos;
                    foreach (var uvi in vp.vertices)
                        _position[uvi] = lp;
                }

                edMesh.averageSize = totalSize / edMesh.meshPoints.Count;

                return _position;
            }
        }

        public int[] VertexIndex
        {
            get
            {
                if (_originalIndex != null) return _originalIndex;

                _originalIndex = new int[vertexCount];

                foreach (var vp in edMesh.meshPoints)
                foreach (var uvi in vp.vertices)
                    _originalIndex[uvi] = vp.index;
                return _originalIndex;
            }
        }

        public MeshConstructor(PlaytimePainter painter)
        {
            profile = painter.MeshProfile;
            edMesh = new EditableMesh(painter);
            mesh = painter.SharedMesh;
            if (!mesh)
                mesh = new Mesh();
        }

        public MeshConstructor(EditableMesh edMesh, MeshPackagingProfile solution, Mesh freshMesh)
        {
            profile = solution;
            this.edMesh = edMesh;
            mesh = freshMesh;
            if (!mesh)
                mesh = new Mesh();
        }

        private void GenerateNormals()
        {

            _normals = new Vector3[vertexCount];
            _sharpNormals = new Vector3[vertexCount];
            var NormalForced = new bool[vertexCount];

            foreach (var vp in edMesh.meshPoints)
            {
                vp.normalIsSet = false;
                vp.normal = Vector3.zero;
            }

            for (var i = 0; i < vertexCount; i++)
            {
                _normals[i] = Vector3.zero;
                _sharpNormals[i] = Vector3.zero;
            }

            var scaleNormalizer = 1f / (edMesh.averageSize + 0.001f);

            foreach (var tri in edMesh.triangles)
            {

                // ********* Calculating Normals

                tri.sharpNormal = tri.GetNormalByArea(scaleNormalizer);

                for (var no = 0; no < 3; no++)
                {

                    var vertPnt = tri.vertexes[no].meshPoint;
                    int mDIndex = tri.vertexes[no];

                    _sharpNormals[mDIndex] = tri.sharpNormal;

                    if (tri.dominantCorner[no])
                    {

                        _normals[mDIndex] = tri.sharpNormal;
                        NormalForced[mDIndex] = true;

                        if (vertPnt.normalIsSet)
                            vertPnt.normal += tri.sharpNormal;
                        else
                            vertPnt.normal = tri.sharpNormal;

                        vertPnt.normalIsSet = true;

                    }
                    else
                    {
                        if (!NormalForced[mDIndex])
                            _normals[mDIndex] = tri.sharpNormal;

                        if (!vertPnt.normalIsSet)
                            vertPnt.normal += tri.sharpNormal;

                    }
                }
            }

            for (int i = 0; i < vertexCount; i++)
            {
                _normals[i].Normalize();
                _sharpNormals[i].Normalize();
            }


            foreach (var vp in edMesh.meshPoints)
                if (vp.smoothNormal)
                {
                    vp.normal = vp.normal.normalized;
                    foreach (var uv in vp.vertices)
                        _normals[uv] = vp.normal;

                }
        }

        private void GenerateTriangles() {

            if (mesh)
                mesh.Clear();

            mesh.colors = null;

            mesh.uv = null;

            if (edMesh.triangles.IsNullOrEmpty())
                return;

            edMesh.RefreshVertexTriangleList();

            vertexCount = edMesh.AssignIndexes();

            if (edMesh.subMeshCount > 1) {

                var maxSubMesh = 0;

                foreach (var t in edMesh.triangles)
                    maxSubMesh = Mathf.Max(maxSubMesh, t.subMeshIndex);

                edMesh.subMeshCount = maxSubMesh + 1;
            }

            triangles = new List<int>[edMesh.subMeshCount];
            for (var i = 0; i < edMesh.subMeshCount; i++)
                triangles[i] = new List<int>();

            foreach (var tri in edMesh.triangles) {
                var trs = triangles[tri.subMeshIndex];

                trs.Add(tri.vertexes[0]);
                trs.Add(tri.vertexes[1]);
                trs.Add(tri.vertexes[2]);
            }

            baseVertex = edMesh.baseVertex.ToArray();

        }

        public Mesh UpdateMesh<T>() where T: VertexDataSource
        {

            if (!edMesh.firstBuildRun)
                Construct();
            else
            {
                vertexCount = edMesh.vertexCount;

                profile.UpdatePackage<T>(this);
            }

            return mesh;
        }

        public Mesh Construct(PlaytimePainter p)
        {
            var m = Construct();

            if (m)
            {
                p.SharedMesh = m;
                p.UpdateMeshCollider(m);
            }

            return m;
        }

        public Mesh Construct() {

            if (profile == null)
            {
                Debug.LogError("NoMesh Packaging profile to generate mesh");
                return mesh;
            }

            GenerateTriangles();

            var valid = profile.Repack(this);

            if (!valid)
                return mesh;

            mesh.bindposes = edMesh.bindPoses;
            
            if (edMesh.gotBoneWeights) {
                _boneWeights = new BoneWeight[vertexCount];

               // for (var i = 0; i < edMesh.meshPoints.Count; i++)
                //    _boneWeights[i] = edMesh.meshPoints[i].boneWeight;

                foreach (var vp in edMesh.meshPoints) 
                    foreach (var uvi in vp.vertices)
                        _boneWeights[uvi] = uvi.boneWeight;
                
                mesh.boneWeights = _boneWeights;
            }

            var vCnt = mesh.vertices.Length;

            if (!edMesh.shapes.IsNullOrEmpty())
                for (var s = 0; s < edMesh.shapes.Count; s++)
                {
                    var name = edMesh.shapes[s];
                    var frames = edMesh.meshPoints[0].shapes[s].Count;

                    for (var f = 0; f < frames; f++)
                    {

                        var pos = new Vector3[vCnt];
                        var nrm = new Vector3[vCnt];
                        var tng = new Vector3[vCnt];

                        for (var v = 0; v < vCnt; v++)
                        {
                            var bf = edMesh.uvsByFinalIndex[v].meshPoint.shapes[s][f];

                            pos[v] = bf.deltaPosition;
                            nrm[v] = bf.deltaNormal;
                            tng[v] = bf.deltaTangent;

                        }
                        mesh.AddBlendShapeFrame(name, edMesh.blendWeights[s][f], pos, nrm, tng);
                    }
                }

            edMesh.firstBuildRun = true;

            mesh.name = edMesh.meshName;

            return mesh;
        }

        public bool Valid
        {
            get
            {
                if (triangles != null && edMesh.vertexCount >= 3 && mesh)
                {
                    int cnt = 0;

                    foreach (var t in triangles)
                    {
                        cnt += t.Count;
                    }

                    return cnt >= 3;

                }

                return false;
            }
        }

        public void AssignMesh(MeshFilter m, MeshCollider c) {
            if (triangles.IsNullOrEmpty())
                return;

            if (m)
                m.sharedMesh = mesh;

            if (c) {
                c.sharedMesh = null;
                c.sharedMesh = m.sharedMesh;
            }
        }
    }
}