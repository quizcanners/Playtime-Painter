using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PlaytimePainter {

    [ExecuteInEditMode]
    public class PixelArtMeshGenerator : MonoBehaviour, IPEGI {

        static int width = 8;
        static float halfPix = 0;
        static Vert[] verts;
        static List<int> tris = new List<int>();

        public int testWidth = 8;
        public float thickness = 0;
        
        public MeshFilter meshFilter;
        public MeshCollider meshCollider;

        enum PicV { lup = 0, rup = 1, rdwn = 2, ldwn = 3 };

        class Vert
        {
            public Vector3 pos;
            public Vector4 uv;

            public Vert(int x, int y, PicV p, float borderPercent)
            {
                uv = new Vector4(halfPix + (float)x / (float)width, halfPix + (float)y / (float)width                                                     // normal coordinate

                    , halfPix + (float)x / (float)width, halfPix + (float)y / (float)width); // with center coordinate

                pos = new Vector3(uv.x - 0.5f, uv.y - 0.5f, 0);

                float off = halfPix * (1 - borderPercent);

                Vector3 offf = Vector3.zero;

                switch (p)
                {
                    case PicV.ldwn: offf += new Vector3(-off, off, 0); break;
                    case PicV.lup: offf += new Vector3(-off, -off, 0); break;
                    case PicV.rdwn: offf += new Vector3(off, off, 0); break;
                    case PicV.rup: offf += new Vector3(off, -off, 0); break;
                }

                pos += offf;

                uv.x += offf.x;
                uv.y += offf.y;

            }
        }

        static int GetIndOf(int x, int y, PicV p) => (y * width + x) * 4 + (int)p;
        

        void JoinDiagonal(int x, int y)
        {
            tris.Add(GetIndOf(x, y, PicV.rdwn));
            tris.Add(GetIndOf(x + 1, y, PicV.ldwn));
            tris.Add(GetIndOf(x, y + 1, PicV.rup));

            tris.Add(GetIndOf(x, y + 1, PicV.rup));
            tris.Add(GetIndOf(x + 1, y, PicV.ldwn));
            tris.Add(GetIndOf(x + 1, y + 1, PicV.lup));
        }

        void JoinDown(int x, int y)
        {
            tris.Add(GetIndOf(x, y, PicV.ldwn));
            tris.Add(GetIndOf(x, y, PicV.rdwn));
            tris.Add(GetIndOf(x, y + 1, PicV.lup));

            tris.Add(GetIndOf(x, y, PicV.rdwn));
            tris.Add(GetIndOf(x, y + 1, PicV.rup));
            tris.Add(GetIndOf(x, y + 1, PicV.lup));

        }

        void JoinRight(int x, int y)
        {
            tris.Add(GetIndOf(x, y, PicV.rup));
            tris.Add(GetIndOf(x + 1, y, PicV.lup));
            tris.Add(GetIndOf(x, y, PicV.rdwn));

            tris.Add(GetIndOf(x + 1, y, PicV.lup));
            tris.Add(GetIndOf(x + 1, y, PicV.ldwn));
            tris.Add(GetIndOf(x, y, PicV.rdwn));
        }

        void FillPixel(int x, int y)
        {
            tris.Add(GetIndOf(x, y, PicV.lup));
            tris.Add(GetIndOf(x, y, PicV.rup));
            tris.Add(GetIndOf(x, y, PicV.ldwn));

            tris.Add(GetIndOf(x, y, PicV.rup));
            tris.Add(GetIndOf(x, y, PicV.rdwn));
            tris.Add(GetIndOf(x, y, PicV.ldwn));
        }

        public Mesh GenerateMesh(int w)
        {
            width = w;
            halfPix = 0.5f / width;

            Mesh m = new Mesh();

            int pixls = width * width;

            verts = new Vert[pixls * 4];
            tris.Clear();

            Vector3[] fverts = new Vector3[verts.Length];
            List<Vector4> uvs = new List<Vector4>();//[verts.Length];

            for (int i = 0; i < verts.Length; i++)
                uvs.Add(new Vector4());

            for (int x = 0; x < width; x++)
                for (int y = 0; y < width; y++)
                {
                    for (int p = 0; p < 4; p++)
                    {
                        int ind = GetIndOf(x, y, (PicV)p);
                        verts[ind] = new Vert(x, y, (PicV)p, thickness);
                        fverts[ind] = verts[ind].pos;
                        uvs[ind] = verts[ind].uv;
                    }

                    FillPixel(x, y);
                    if (thickness > 0)
                    {
                        thickness = Mathf.Min(thickness, 0.9f);
                        if (x < width - 1) JoinRight(x, y);
                        if (y < width - 1) JoinDown(x, y);
                        if ((x < width - 1) && (y < width - 1))
                            JoinDiagonal(x, y);
                    }
                }

            m.vertices = fverts;
            m.SetUVs(0, uvs);
            m.triangles = tris.ToArray();

            return m;
        }

        void OnEnable() {
            if (!meshFilter)
                meshFilter = GetComponent<MeshFilter>();

            if (!meshCollider)
                meshCollider = GetComponent<MeshCollider>();

            if (meshCollider && meshFilter)
                meshCollider.sharedMesh = meshFilter.sharedMesh;
        }

        #if UNITY_EDITOR
        void Save() {

           
            string meshFilePath = "Assets/" + meshFilter.transform.name + ".asset";
            Mesh meshToSave = meshFilter.sharedMesh;
            AssetDatabase.CreateAsset(meshToSave, meshFilePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
           
        }
        #endif

        #region Inspector
        #if PEGI
        public bool Inspect()
        {
            bool changed = false;
            "Mesh Filter".edit(ref meshFilter).nl(ref changed);
            "Mesh Collider".edit(ref meshCollider).nl(ref changed);
            "Width: ".edit(ref testWidth).nl(ref changed);
            "Thickness ".edit(ref thickness).nl(ref changed);
            if ("Generate".Click()) {
               
                meshFilter.mesh = GenerateMesh(testWidth * 2);

                if (meshCollider)
                    meshCollider.sharedMesh = meshFilter.sharedMesh;
            }

            #if UNITY_EDITOR
            if (meshFilter && meshFilter.sharedMesh && "Save".Click())
                Save();
#endif

            "For Pix Art shader set width equal to texture size, and thickness - 0".writeHint();

            return changed;
        }
        #endif
        #endregion

    }
}