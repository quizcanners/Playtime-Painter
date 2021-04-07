using System;
using UnityEngine;

namespace PlaytimePainter
{

    public class BrushMeshGenerator  {

        public Mesh GetQuad()
        {
            if (_quad) return _quad;
            
            _quad = new Mesh();
            
            var qVertices = new Vector3[4];
            var quv = new Vector2[4];
            var qTris = new int[6];

            quv[0] = new Vector2(0, 1);
            quv[1] = new Vector2(1, 1);
            quv[2] = new Vector2(0, 0);
            quv[3] = new Vector2(1, 0);

            qVertices[0] = new Vector3(-0.5f, 0.5f);
            qVertices[1] = new Vector3(0.5f, 0.5f);
            qVertices[2] = new Vector3(-0.5f, -0.5f);
            qVertices[3] = new Vector3(0.5f, -0.5f);

            qTris[0] = 0; qTris[1] = 1; qTris[2] = 2;
            qTris[3] = 2; qTris[4] = 1; qTris[5] = 3;

            _quad.vertices = qVertices;
            _quad.uv = quv;
            _quad.triangles = qTris;
            _quad.name = "Quad";
            return _quad;
        }

        #region Lazy Brush

        public enum SectionType { Head, Tail, Center }

        private const int SegmentsCount = 4;

        private QuadSegment[] _segments;

        private int[] _segmentTris;

        private Mesh _segmentMesh;

        private void InitSegmentsIfNull()
        {
            _segments = new QuadSegment[SegmentsCount];
            for (var i = 0; i < SegmentsCount; i++)
                _segments[i] = new QuadSegment();


            _segmentTris = new int[(SegmentsCount - 1) * 4 * 3];

            for (var segment = 0; segment < SegmentsCount - 1; segment++)
            {
                var vno = segment * 3;
                var ind = segment * 3 * 4;
                _segmentTris[ind] = vno; _segmentTris[ind + 1] = vno + 1; _segmentTris[ind + 2] = vno + 2;
                ind += 3;
                _segmentTris[ind] = vno; _segmentTris[ind + 1] = vno + 2; _segmentTris[ind + 2] = vno + 3;  // 
                ind += 3;
                _segmentTris[ind] = vno + 2; _segmentTris[ind + 1] = vno + 1; _segmentTris[ind + 2] = vno + 4;  // 
                ind += 3;
                _segmentTris[ind] = vno + 3; _segmentTris[ind + 1] = vno + 2; _segmentTris[ind + 2] = vno + 4;  // 
            }

        }

        public class QuadSegment
        {
            public readonly Vector3[] vertices;
            private readonly Vector2[] _uv;

            private static readonly Vector3[] VertArr = new Vector3[SegmentsCount * 3 - 1];
            private static readonly Vector2[] UvArr = new Vector2[SegmentsCount * 3 - 1];

            public static Vector3[] VertexArrayFrom(QuadSegment[] arr)
            {
                var len = arr.Length;

                for (var i = 0; i < len; i++)
                {
                    VertArr[i * 3] = arr[i].vertices[0];
                    VertArr[i * 3 + 1] = arr[i].vertices[1];
                    if (i < (len - 1))
                        VertArr[i * 3 + 2] = arr[i].vertices[2];
                }
                return VertArr;
            }

            public static Vector2[] UvArrayFrom(QuadSegment[] arr)
            {
                var len = arr.Length;
                for (var i = 0; i < len; i++)
                {
                    UvArr[i * 3] = arr[i]._uv[0];
                    UvArr[i * 3 + 1] = arr[i]._uv[1];
                    if (i < (len - 1))
                        UvArr[i * 3 + 2] = arr[i]._uv[2];
                }
                return UvArr;
            }

            public void SetSection(SectionType type)
            {
                float y = 0;
                switch (type)
                {
                    case SectionType.Head: y = 0; break;
                    case SectionType.Tail: y = 1; break;
                    case SectionType.Center: y = 0.5f; break;
                }
                _uv[0].y = y;
                _uv[1].y = y;
            }

            public void SetCenter(QuadSegment next)
            {

                vertices[2] = (vertices[0] + vertices[1] + next.vertices[0] + next.vertices[1]) / 4;
                _uv[2] = (_uv[0] + _uv[1] + next._uv[0] + next._uv[1]) / 4;

            }

            public QuadSegment()
            {
                vertices = new Vector3[3];
                vertices[0] = Vector3.zero;
                vertices[1] = Vector3.zero;
                vertices[2] = Vector3.zero;
                _uv = new Vector2[3];
                _uv[0] = new Vector2();
                _uv[1] = Vector2.one;
                _uv[2] = Vector2.one;
                SetSection(SectionType.Center);
            }

        }

        private Vector2 _prevA;
        private Vector2 _prevB;

        public Mesh GetStreak(Vector3 from, Vector3 to, float streakWidth, bool head, bool tail)
        {

            InitSegmentsIfNull();

            var headSegment = _segments[SegmentsCount - 1];
            var tailSegment = _segments[0];


            var vector = to - from;
            Vector3 fromA;
            Vector3 fromB;

            var side = Vector3.Cross(vector, Vector3.forward).normalized * streakWidth / 2;

            if (!tail)
            {
                fromA = _prevA;
                fromB = _prevB;
            }
            else
            {

                fromA = from + side;
                fromB = from - side;
            }


            var toA = to + side;
            var toB = to - side;

            var dirA = toA - fromA;
            var dirB = toB - fromB;

            var offA = dirA.normalized * streakWidth * 0.5f;
            var offB = dirB.normalized * streakWidth * 0.5f;

            if (head)
            {
                headSegment.vertices[0] = toA + offA;
                headSegment.vertices[1] = toB + offB;
            }

            if (tail)
            {
                tailSegment.vertices[0] = fromA - offA;
                tailSegment.vertices[1] = fromB - offB;
            }

            var till = SegmentsCount - (head ? 1 : 0);

            var midSegment = SegmentsCount - 1 - ((tail ? 1 : 0) + (head ? 1 : 0));

            var stepA = (toA - fromA) / midSegment;
            var stepB = (toB - fromB) / midSegment;

            for (var i = (tail ? 1 : 0); i < till; i++)
            {

                var q = _segments[i];
                q.vertices[0] = fromA;//from+ side;
                q.vertices[1] = fromB;//from- side;

                //  from += step;
                fromA += stepA;
                fromB += stepB;
            }


            headSegment.SetSection(head ? SectionType.Head : SectionType.Center);
            tailSegment.SetSection(tail ? SectionType.Tail : SectionType.Center);


            for (var i = 0; i < SegmentsCount - 1; i++)
                _segments[i].SetCenter(_segments[i + 1]);

            var initializing = (!_segmentMesh);
            if (initializing)
                _segmentMesh = new Mesh();

            _segmentMesh.vertices = QuadSegment.VertexArrayFrom(_segments);
            _segmentMesh.uv = QuadSegment.UvArrayFrom(_segments);

            if (initializing)
                _segmentMesh.triangles = _segmentTris;

            _prevA = toA;
            _prevB = toB;

            _segmentMesh.name = "Segment Mesh";

            return _segmentMesh;



        }
        #endregion

        #region Rounded Line

        private readonly Vector3[] _vertices = new Vector3[8];
        private readonly Vector2[] _uv = new Vector2[8];
        private readonly int[] _tris = new int[18];
        [NonSerialized] private Mesh _mesh;
        [NonSerialized] private Mesh _quad;

        public Mesh GetLongMesh(float length, float mWidth)
        {

            if (!_mesh) GenerateLongMesh();

            length = Mathf.Max(0.0001f, length);
            mWidth = Mathf.Max(0.0001f, mWidth);

            var hWidth = mWidth * 0.5f;
            var hLength = length * 0.5f;
            var ends = hLength + hWidth;


            _vertices[0] = new Vector3(-hWidth, ends);
            _vertices[1] = new Vector3(hWidth, ends);
            _vertices[2] = new Vector3(-hWidth, hLength);
            _vertices[3] = new Vector3(hWidth, hLength);
            _vertices[4] = new Vector3(-hWidth, -hLength);
            _vertices[5] = new Vector3(hWidth, -hLength);
            _vertices[6] = new Vector3(-hWidth, -ends);
            _vertices[7] = new Vector3(hWidth, -ends);

            _mesh.vertices = _vertices;


            return _mesh;
        }

        private void GenerateLongMesh()
        {
            if (!_mesh)
            {
                _mesh = new Mesh();

                GetLongMesh(Size, Width);

                _uv[0] = new Vector2(0, 1);
                _uv[1] = new Vector2(1, 1);
                _uv[2] = new Vector2(0, 0.5f);
                _uv[3] = new Vector2(1, 0.5f);
                _uv[4] = new Vector2(0, 0.5f);
                _uv[5] = new Vector2(1, 0.5f);
                _uv[6] = new Vector2(0, 0);
                _uv[7] = new Vector2(1, 0);


                int t = 0;
                _tris[t] = 0; _tris[    1] = 1; _tris[    2] = 2;
                t = 1 * 3;
                _tris[t] = 2; _tris[t + 1] = 1; _tris[t + 2] = 3;
                t = 2 * 3;
                _tris[t] = 2; _tris[t + 1] = 3; _tris[t + 2] = 4;
                t = 3 * 3;
                _tris[t] = 4; _tris[t + 1] = 3; _tris[t + 2] = 5;
                t = 4 * 3;
                _tris[t] = 4; _tris[t + 1] = 5; _tris[t + 2] = 6;
                t = 5 * 3;
                _tris[t] = 6; _tris[t + 1] = 5; _tris[t + 2] = 7;

                _mesh.vertices = _vertices;
                _mesh.uv = _uv;
                _mesh.triangles = _tris;
            }

            _mesh.name = "Long Mesh";
        }

        #endregion
        
        private const float Width = 1;
        private const float Size = 1;
        public Vector3 pos1;
        public Vector3 pos2;
    }
}