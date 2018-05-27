using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Playtime_Painter{

[ExecuteInEditMode]
public class brushMeshGenerator : MonoBehaviour {
    public static brushMeshGenerator inst() {
        if (_inst == null) _inst = GameObject.FindObjectOfType<brushMeshGenerator>();

        return _inst;
    }
    static brushMeshGenerator _inst;
    public MeshFilter debug;



    // ################################ quad

    public Mesh GetQuad()
    {
        if (quad == null)
        {
            quad = new Mesh();
            Vector3[] qverts = new Vector3[4];
            Vector2[] quv = new Vector2[4];
            int[] qtris = new int[6];

            quv[0] = new Vector2(0, 1);
            quv[1] = new Vector2(1, 1);
            quv[2] = new Vector2(0, 0);
            quv[3] = new Vector2(1, 0);

            qverts[0] = new Vector3(-0.5f, 0.5f);
            qverts[1] = new Vector3(0.5f, 0.5f);
            qverts[2] = new Vector3(-0.5f, -0.5f);
            qverts[3] = new Vector3(0.5f, -0.5f);

            qtris[0] = 0; qtris[1] = 1; qtris[2] = 2;
            qtris[3] = 2; qtris[4] = 1; qtris[5] = 3;

            quad.vertices = qverts;
            quad.uv = quv;
            quad.triangles = qtris;
                quad.name = "Quad";
        }
        return quad;
    }



    // ################################## Lazy Brush:


    public enum sectionType { head, tail, center}

    const int segmentsCount = 4;

    quadSegment[] segments;

    int[] segmentTris;

    [NonSerialized] Mesh segmMesh = null;

    void InitSegmentsIfNull() {
       // if ((segments != null) && (segments.Length == segmentsCount) && (segments[0]!= null)) return;

            segments = new quadSegment[segmentsCount];
            for (int i = 0; i < segmentsCount; i++)
                segments[i] = new quadSegment();


            segmentTris = new int[(segmentsCount - 1) * 4 *3]; 

            for (int segm = 0; segm < segmentsCount - 1; segm++) {
                int vno = segm * 3;
                int ind = segm * 3 * 4; 
                segmentTris[ind] = vno; segmentTris[ind + 1] = vno + 1; segmentTris[ind + 2] = vno + 2;
                ind += 3;
                segmentTris[ind] = vno; segmentTris[ind + 1] = vno + 2; segmentTris[ind + 2] = vno + 3;  // 
                ind += 3;
                segmentTris[ind] = vno+2; segmentTris[ind + 1] = vno + 1; segmentTris[ind + 2] = vno + 4;  // 
                ind += 3;
                segmentTris[ind] = vno+3; segmentTris[ind + 1] = vno + 2; segmentTris[ind + 2] = vno + 4;  // 
        }
    
    }

    public class quadSegment
    {
        public Vector3[] verts;
        public Vector2[] uv;

        static Vector3[] vertArr = new Vector3[segmentsCount*3-1];
        static Vector2[] uvArr = new Vector2[segmentsCount*3-1];

        public static Vector3[] VertexArrayFrom(quadSegment[] arr) {
            int len = arr.Length;

            for (int i=0; i<len; i++) {
                vertArr[i * 3] = arr[i].verts[0];
                vertArr[i * 3+1] = arr[i].verts[1];
                if (i<(len-1))
                    vertArr[i * 3 + 2] = arr[i].verts[2];
            }
            return vertArr;
        }

        public static Vector2[]UVArrayFrom(quadSegment[] arr) {
            int len = arr.Length;
            for (int i = 0; i < len; i++) {
                uvArr[i * 3] = arr[i].uv[0];
                uvArr[i * 3 + 1] = arr[i].uv[1];
                if (i < (len - 1))
                    uvArr[i * 3 + 2] = arr[i].uv[2];
            }
            return uvArr;
        }

        public void SetSection(sectionType type)
        {
            float y = 0;
            switch (type)
            {
                case sectionType.head: y = 0; break;
                case sectionType.tail: y = 1; break;
                case sectionType.center: y = 0.5f; break;
            }
            uv[0].y = y;
            uv[1].y = y;
        }

        public void setCenter (quadSegment next) {

            verts[2] = (verts[0] + verts[1] + next.verts[0] + next.verts[1]) / 4;
            uv[2] = (uv[0] + uv[1] + next.uv[0] + next.uv[1]) / 4;

        }

        public quadSegment() {
            verts = new Vector3[3];
            verts[0] = Vector3.zero;
            verts[1] = Vector3.zero;
            verts[2] = Vector3.zero;
            uv = new Vector2[3];
            uv[0] = new Vector2();
            uv[1] = Vector2.one;
            uv[2] = Vector2.one;
            SetSection(sectionType.center);
        }

    }

    Vector2 prevA;
    Vector2 prevB;

    public Mesh GetStreak(Vector3 from, Vector3 to, float width, bool head, bool tail) {

        InitSegmentsIfNull();

        quadSegment hsegm = segments[segmentsCount - 1];
        quadSegment tsegm = segments[0];

 
        Vector3 vector = to - from;
        Vector3 fromA;
        Vector3 fromB;
        Vector3 toA;
        Vector3 toB;

        Vector3 side = Vector3.Cross(vector, Vector3.forward).normalized * width / 2;

        if (!tail) {
            fromA = prevA;
            fromB = prevB;
        }
        else
        {
         
            fromA = from + side;
            fromB = from - side;
        }


        toA = to + side;
        toB = to - side;

        Vector3 dirA = toA - fromA;
        Vector3 dirB = toB - fromB;

        Vector3 offA = dirA.normalized * width * 0.5f;
        Vector3 offB = dirB.normalized * width * 0.5f;

       
     
          
        if (head) {
            hsegm.verts[0] = toA + offA;
            hsegm.verts[1] = toB + offB; 
        }


        if (tail) {
            tsegm.verts[0] = fromA - offA;
            tsegm.verts[1] = fromB - offB;
        }



        int till = segmentsCount - (head ? 1 : 0);

        int midSegms = segmentsCount - 1-  ((tail ? 1 : 0) + (head ? 1 : 0));

        Vector3 stepA = (toA - fromA) / midSegms;
        Vector3 stepB = (toB - fromB) / midSegms;

        // Vector3 step = (to - from) / midSegms;



        for (int i=(tail ? 1 : 0); i<till; i++) {
          
            quadSegment q = segments[i];
            q.verts[0] = fromA;//from+ side;
            q.verts[1] = fromB;//from- side;

          //  from += step;
            fromA += stepA;
            fromB += stepB;
        }


        hsegm.SetSection(head ? sectionType.head : sectionType.center);
        tsegm.SetSection(tail ? sectionType.tail : sectionType.center);


        for (int i = 0; i < segmentsCount - 1; i++)
            segments[i].setCenter(segments[i + 1]);

        bool initing = (segmMesh == null);
        if (initing)
            segmMesh = new Mesh();

        segmMesh.vertices = quadSegment.VertexArrayFrom(segments);
        segmMesh.uv = quadSegment.UVArrayFrom(segments);

        if (initing)
        segmMesh.triangles = segmentTris;

        prevA = toA;
        prevB = toB;

        if (debug != null) debug.mesh = segmMesh;

            segmMesh.name = "Segmant Mesh";

        return segmMesh;



    }

 

    // ################################  rounded Line:
    public Vector3[] verts = new Vector3[8];
    public Vector2[] uv = new Vector2[8];
    public int[] tris = new int[18];
    [NonSerialized] Mesh mesh;
    [NonSerialized] public Mesh quad;

    public Mesh GetLongMesh(float length, float mwidth) {

        if (mesh == null) GenerateLongMesh();

        length = Mathf.Max(0.0001f, length);
        mwidth = Mathf.Max(0.0001f, mwidth);

        float hwidth = mwidth * 0.5f;
        float hlength = length * 0.5f;
        float ends = hlength + hwidth;


        verts[0] = new Vector3(-hwidth, ends);
        verts[1] = new Vector3(hwidth, ends);
        verts[2] = new Vector3(-hwidth, hlength);
        verts[3] = new Vector3(hwidth, hlength);
        verts[4] = new Vector3(-hwidth, -hlength);
        verts[5] = new Vector3(hwidth, -hlength);
        verts[6] = new Vector3(-hwidth, -ends);
        verts[7] = new Vector3(hwidth, -ends);

        mesh.vertices = verts;


        return mesh;
    }

    void GenerateLongMesh() {
        if (mesh == null)  {
            mesh = new Mesh();

            GetLongMesh(size,  width);

            uv[0] = new Vector2(0, 1);
            uv[1] = new Vector2(1, 1);
            uv[2] = new Vector2(0, 0.5f);
            uv[3] = new Vector2(1, 0.5f);
            uv[4] = new Vector2(0, 0.5f);
            uv[5] = new Vector2(1, 0.5f);
            uv[6] = new Vector2(0, 0);
            uv[7] = new Vector2(1, 0);

            int t = 0;
            tris[t] = 0; tris[t + 1] = 1; tris[t + 2] = 2;
            t = 1*3;
            tris[t] = 2; tris[t + 1] = 1; tris[t + 2] = 3;
            t = 2 * 3;
            tris[t] = 2; tris[t + 1] = 3; tris[t + 2] = 4;
            t = 3 * 3;
            tris[t] = 4; tris[t + 1] = 3; tris[t + 2] = 5;
            t = 4 * 3;
            tris[t] = 4; tris[t + 1] = 5; tris[t + 2] = 6;
            t = 5 * 3;
            tris[t] = 6; tris[t + 1] = 5; tris[t + 2] = 7;

            mesh.vertices = verts;
            mesh.uv = uv;
            mesh.triangles = tris;
        }
        if (debug != null) debug.mesh = mesh;


            mesh.name = "Long Mesh";
        }

    public bool rebuild = false;
    public float width = 1;
    public float size = 1;
    public Vector3 pos1;
    public Vector3 pos2;
    public void Update() {
        if (rebuild) {
          //  Mesh m = GetStreak(pos1, pos2, 1, false, false, true);
           // pos1 = pos2;
            //GetLongMesh(size, width);

           rebuild = false;

            //   if (debug != null) debug.mesh = m; 
        }
    }

    private void Awake()  {
        _inst = this;
    }


}
}