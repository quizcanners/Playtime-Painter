using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class PixelArtMeshGenerator : MonoBehaviour {

    public static int width = 8;
    static float halfPix = 0;
    static vert[] verts;// = new List<vert>();
    static List<int> tris = new List<int>();

    public float thickness = 0.2f;
    public Vector2 tiling = Vector2.one;
    public Vector2 offset = Vector2.zero;
    public string meshName = "unnamed";
    public MeshFilter mFilter;

    public int meshesInBulk = 1;

    enum picV { lup = 0, rup = 1, rdwn = 2, ldwn = 3 };

    class vert {
        public Vector3 pos;// = new Vector3();
        public Vector4 uv;// = new Vector2();

        public vert(int x, int y, picV p, float borderPercent, PixelArtMeshGenerator gen) {
            uv = new Vector4(halfPix + (float)x / (float)width, halfPix + (float)y / (float)width                                                     // normal coordinate

                , halfPix + (float)x / (float)width, halfPix + (float)y / (float)width); // with center coordinate

            pos = new Vector3(uv.x - 0.5f, uv.y - 0.5f, 0);

            float off = halfPix * (1 - borderPercent);

            Vector3 offf = Vector3.zero;

            switch (p) {
                case picV.ldwn: offf += new Vector3(-off, off, 0); break;
                case picV.lup: offf += new Vector3(-off, -off, 0); break;
                case picV.rdwn: offf += new Vector3(off, off, 0); break;
                case picV.rup: offf += new Vector3(off, -off, 0); break;
            }

            pos += offf;

            uv.x += offf.x;
            uv.y += offf.y;

            uv.x = gen.offset.x + uv.x * gen.tiling.x;
            uv.y = gen.offset.y + uv.y * gen.tiling.y;
            uv.z = gen.offset.x + uv.z * gen.tiling.x;
            uv.w = gen.offset.y + uv.w * gen.tiling.y;
        }
    }

    static int getIndOf(int x, int y, picV p) {
        return (y * width + x) * 4 + (int)p;
    }

    void JoinDiagonal(int x, int y) {
        tris.Add(getIndOf(x, y, picV.rdwn));
        tris.Add(getIndOf(x + 1, y, picV.ldwn));
        tris.Add(getIndOf(x, y + 1, picV.rup));

        tris.Add(getIndOf(x, y + 1, picV.rup));
        tris.Add(getIndOf(x + 1, y, picV.ldwn));
        tris.Add(getIndOf(x + 1, y + 1, picV.lup));
    }

    void JoinDown(int x, int y) {
        tris.Add(getIndOf(x, y, picV.ldwn));
        tris.Add(getIndOf(x, y, picV.rdwn));
        tris.Add(getIndOf(x, y + 1, picV.lup));

        tris.Add(getIndOf(x, y, picV.rdwn));
        tris.Add(getIndOf(x, y + 1, picV.rup));
        tris.Add(getIndOf(x, y + 1, picV.lup));

    }


    void JoinRight(int x, int y) {
        tris.Add(getIndOf(x, y, picV.rup));
        tris.Add(getIndOf(x + 1, y, picV.lup));
        tris.Add(getIndOf(x, y, picV.rdwn));

        tris.Add(getIndOf(x + 1, y, picV.lup));
        tris.Add(getIndOf(x + 1, y, picV.ldwn));
        tris.Add(getIndOf(x, y, picV.rdwn));
    }

    void FillPixel(int x, int y) {
        tris.Add(getIndOf(x, y, picV.lup));
        tris.Add(getIndOf(x, y, picV.rup));
        tris.Add(getIndOf(x, y, picV.ldwn));

        tris.Add(getIndOf(x, y, picV.rup));
        tris.Add(getIndOf(x, y, picV.rdwn));
        tris.Add(getIndOf(x, y, picV.ldwn));
    }


    public Mesh GenerateMesh() {
        width = Mathf.Max(2, width);

        halfPix = 0.5f / width;

        Mesh m = new Mesh();

        int pixls = width * width;

        verts = new vert[pixls * 4];
        tris.Clear();

        Vector3[] fverts = new Vector3[verts.Length];
        List<Vector4> uvs = new List<Vector4>();//[verts.Length];

        for (int i = 0; i < verts.Length; i++)
            uvs.Add(new Vector4());

        for (int x = 0; x < width; x++)
            for (int y = 0; y < width; y++) {

                for (int p = 0; p < 4; p++) {
                    int ind = getIndOf(x, y, (picV)p);
                    verts[ind] = new vert(x, y, (picV)p, thickness, this);
                    fverts[ind] = verts[ind].pos;
                    uvs[ind] = verts[ind].uv;
                }

                FillPixel(x, y);
                if (thickness > 0) {
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

        //m.UploadMeshData(false);

        return m;
    }

    public void GenerateAndSave() {
        mFilter.mesh = GenerateMesh();

#if UNITY_EDITOR

        string meshFilePath = "Assets/" + meshName + ".asset";
        Mesh meshToSave = mFilter.sharedMesh;
        mFilter.name = meshName;
        AssetDatabase.CreateAsset(meshToSave, meshFilePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
#endif
    }

    public void GenerateBulk() {

        float step = 1f / (float)meshesInBulk;
        tiling = Vector2.one * step;

        for (int x = 0; x < meshesInBulk; x++) {
            for (int y = 0; y < meshesInBulk; y++) {
                offset = new Vector2(x * step, y * step);

                Mesh holder = GenerateMesh();

                GameObject meshHolder = Instantiate(mFilter.gameObject);
                meshHolder.transform.parent = this.transform;
                meshHolder.transform.localPosition = new Vector3(x, y, 0);
                meshHolder.GetComponent<MeshFilter>().mesh = holder;


#if UNITY_EDITOR
                string submeshName = meshName + "_" + x + "_" + y;

                string meshFilePath = "Assets/" + submeshName + ".asset";
                AssetDatabase.CreateAsset(holder, meshFilePath);
                AssetDatabase.SaveAssets();
#endif
            }

        }
#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif
    }

}


#if UNITY_EDITOR


[CustomEditor(typeof(PixelArtMeshGenerator))]
public class PixArtMeshGenDrawer : Editor
{
   


        public override void OnInspectorGUI() {
        ef.start(serializedObject);
            PixelArtMeshGenerator tmp = (PixelArtMeshGenerator)target;

            ef.write("width");
            ef.edit(ref PixelArtMeshGenerator.width);
            ef.newLine();

        ef.write("thickness");
        ef.edit(ref tmp.thickness);
        ef.newLine();
        ef.write("offset");
        ef.edit(ref tmp.offset);
        ef.newLine();
        ef.write("tiling:");
        ef.edit(ref tmp.tiling);
        ef.newLine();
        ef.write("name:");
        ef.edit(ref tmp.meshName);
        if (ef.Click("Generate"))
            tmp.GenerateAndSave();
        ef.newLine();
        ef.write("subgrids count:");
        ef.edit(ref tmp.meshesInBulk);
        if (ef.Click("Generate Bulk"))
            tmp.GenerateBulk();
        ef.newLine();

        tmp.mFilter = (MeshFilter)EditorGUILayout.ObjectField(tmp.mFilter, typeof(MeshFilter), true);


        ef.newLine();
        }
    
}


#endif