using SharedTools_Stuff;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshAnaliser {

    public static int GetSubmeshNumber(this Mesh m, int triangleIndex)
    {
        
        if (m) {

            if (m.subMeshCount == 1)
                return 0;

            if (!m.isReadable)
            {
                Debug.Log("Mesh {0} is not readable. Enable for submesh material editing.".F(m.name));
                return 0;
            }


            int[] hittedTriangle = new int[] {
                m.triangles[triangleIndex * 3],
                m.triangles[triangleIndex * 3 + 1],
                m.triangles[triangleIndex * 3 + 2] };


            for (int i = 0; i < m.subMeshCount; i++)
            {

                if (i == m.subMeshCount - 1)
                    return i;

                int[] subMeshTris = m.GetTriangles(i);
                for (int j = 0; j < subMeshTris.Length; j += 3)
                    if (subMeshTris[j] == hittedTriangle[0] &&
                        subMeshTris[j + 1] == hittedTriangle[1] &&
                        subMeshTris[j + 2] == hittedTriangle[2])
                        return i;


            }
        }

        return 0;
    }

    static Mesh GetMesh(GameObject go)  {
        var mf = go?.GetComponent<MeshFilter>();

        if (mf) {
            var m = mf.sharedMesh;
            if (!m)
                m = mf.mesh;
            return m;
        }
        return null;
    }
}