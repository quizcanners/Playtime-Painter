﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Painter {


    [Serializable]
    public class MeshConstructor {

        public int[] tris;

        Vector4[] perVertexTrisTexture;
        Vector2[] uvs;
        Vector2[] uvs2;

        public Vector3[] verts;
        public Vector3[] normals;
        public Vector3[] sharpNormals;

        BoneWeight[] boneWeights;
        Matrix4x4[] bindPoses;
        BlendFrame[] blendShapes;

        public float[] weight;

        public Vector4[] FirstNormal;
        public Vector4[] SecondNormal;
        public Vector4[] ThirdNormal;
        public Vector4[] Tangents;


        Vector3[] edgeNormal0;
        Vector3[] edgeNormal1;
        Vector3[] edgeNormal2;


        Color[] colors;
        Vector4[] edgeData;
        Vector4[] shadowBake;
        Countless<vertexAnimationFrame> anims; // outer tree - animation no, inner - vertices
        public int[] originalIndex;

        public MeshSolutionProfile profile;

        public EditableMesh edMesh;

        public Mesh mesh;

        int vertsCount;

        void GeneratePreConstructionData() {
            if (mesh != null)
                mesh.Clear();

            if (edMesh.triangles.Count == 0)
                return;

            vertsCount = edMesh.AssignIndexes();
            tris = new int[edMesh.triangles.Count * 3];
            verts = new Vector3[vertsCount];
            normals = new Vector3[vertsCount];

            sharpNormals = new Vector3[vertsCount];
            originalIndex = new int[vertsCount];
            weight = new float[vertsCount];
            FirstNormal = new Vector4[vertsCount];
            SecondNormal = new Vector4[vertsCount];
            ThirdNormal = new Vector4[vertsCount];
            edMesh.NumberVerticlesInTangentsW();

          

            bool[] NormalForced = new bool[vertsCount];

            for (int i = 0; i < edMesh.vertices.Count; i++) {
                vertexpointDta vp = edMesh.vertices[i];
                vp.NormalIsSet = false;
                vp.normal = Vector3.zero;

                for (int u = 0; u < vp.uv.Count; u++) {
                    UVpoint uvi = vp.uv[u];
                    int index = uvi.finalIndex;
                    verts[index] = vp.pos;  
                    normals[index] = Vector3.zero;
                    sharpNormals[index] = Vector3.zero;
                }
            }

            trisDta tri;
            //Vector3 trisNorm;
            int nom;
            int[] inds = new int[3];

            for (int i = 0; i < edMesh.triangles.Count; i++) {
                tri = edMesh.triangles[i];

                nom = i * 3;

                for (int j = 0; j < 3; j++) {
                    inds[j] = tri.uvpnts[j].finalIndex;
                    tris[nom + j] = inds[j];
                }


                // ********* Calculating Normals

                tri.sharpNormal = MyMath.GetNormalOfTheTriangle(
                    verts[inds[0]],
                    verts[inds[1]],
                    verts[inds[2]]);


                for (int no = 0; no < 3; no++) {

                    vertexpointDta vertPnt = tri.uvpnts[no].vert;
                    int mDIndex = inds[no];

                    sharpNormals[mDIndex] = tri.sharpNormal;
                    originalIndex[mDIndex] = vertPnt.index;

                    if (tri.ForceSmoothedNorm[no]) {

                        normals[mDIndex] = tri.sharpNormal;
                        NormalForced[mDIndex] = true;

                        if (vertPnt.NormalIsSet)
                            vertPnt.normal += tri.sharpNormal;
                        else
                            vertPnt.normal = tri.sharpNormal;

                        vertPnt.NormalIsSet = true;

                    } else {
                        if (!NormalForced[mDIndex])
                            normals[mDIndex] = tri.sharpNormal;

                        if (!vertPnt.NormalIsSet)
                            vertPnt.normal += tri.sharpNormal;

                    }
                }
            }

            for (int i = 0; i < vertsCount; i++) {
                normals[i].Normalize();
                sharpNormals[i].Normalize();
            }

            for (int i = 0; i < edMesh.triangles.Count; i++) {
                tri = edMesh.triangles[i];
                nom = i * 3;
                for (int j = 0; j < 3; j++) {
                    inds[j] = tri.uvpnts[j].finalIndex;
                }

                // get normal of the line

                for (int no = 0; no < 3; no++) {
                    //int mDIndex = inds[no];
                    // FirstNormal[mDIndex].FromV3(trisNorm);

                    // UNFINISHED, assign weight and normals

                }
            }

            for (int i = 0; i < edMesh.vertices.Count; i++) {

                vertexpointDta vp = edMesh.vertices[i];
                if (vp.SmoothNormal) {
                    vp.normal = vp.normal.normalized;
                    foreach (UVpoint uv in vp.uv)
                        normals[uv.finalIndex] = vp.normal;

                }
            }

          

        }

        public Color[] _colors {
            get {
                if (colors == null) {
                    colors = new Color[vertsCount];
                    foreach (var vp in edMesh.vertices)
                        foreach (var uvi in vp.uv)
                            colors[uvi.finalIndex] = uvi._color;
                }
                return colors;
            }
        }

        public Vector4[] _shadowBake {
            get {
                if (shadowBake == null) {
                    shadowBake = new Vector4[vertsCount];
                    foreach (var vp in edMesh.vertices)
                        foreach (var uvi in vp.uv)
                            shadowBake[uvi.finalIndex] = vp.shadowBake;
                }
                return shadowBake;
            }
        }

        public Vector2[] _uv { get {
                if (uvs == null){
                uvs = new Vector2[vertsCount];
                    foreach (var vp in edMesh.vertices)
                        foreach (var uvi in vp.uv)
                            uvs[uvi.finalIndex] = uvi.getUV(0);
                }
                return uvs;
            }
        }

        public Vector2[] _uv2 {
            get {
                if (uvs2 == null) {
                    uvs2 = new Vector2[vertsCount];
                    foreach (var vp in edMesh.vertices)
                        foreach (var uvi in vp.uv)
                            uvs2[uvi.finalIndex] = uvi.getUV(0);
                }
                return uvs2;
            }
        }

        public Vector4[] _trisTextures {
            get{
                if (perVertexTrisTexture == null) {
                
                    perVertexTrisTexture = new Vector4[vertsCount];

                    foreach (var tri in edMesh.triangles){
                        for (int no = 0; no < 3; no++) 
                            perVertexTrisTexture[tri.uvpnts[no].finalIndex] = tri.textureNo;
                        
                    }
                }

                return perVertexTrisTexture;
            }
        }

        public Vector4[] _edgeData {
            get {
                if (edgeData == null) {
                    edgeData = new Vector4[vertsCount];
                    
                    foreach (var tri in edMesh.triangles) {
                        for (int no = 0; no < 3; no++) {
                            UVpoint up = tri.uvpnts[no];
                            float edge = (up.tris.Count == 1 //up.vert.SmoothNormal
                                ) ? 1 : 0;
                            edgeData[up.finalIndex] = new Vector4(no == 0 ? 0 : edge, no == 1 ? 0 : edge, no == 2 ? 0 : edge,  up.vert.edgeStrength);
                        }
                    }
                }
                return edgeData;
            }
        }

        public Vector3[] _edgeNormal_0_OrSharp {
            get  {
                if (edgeNormal0 == null) {
                    edgeNormal0 = new Vector3[vertsCount];

                    foreach (var tri in edMesh.triangles)
                    {
                        UVpoint up = tri.uvpnts[0];
                        edgeNormal0[up.finalIndex] = sharpNormals[up.finalIndex];

                        UVpoint up1 = tri.uvpnts[1];
                        UVpoint up2 = tri.uvpnts[2];

                        var tris = up1.vert.getTrianglesFromLine(up2.vert);

                        var nrm =  tris.SmoothVector();

                        edgeNormal0[up1.finalIndex] = nrm;
                        edgeNormal0[up2.finalIndex] = nrm;
                    }
                }
                return edgeNormal0;
            }
        }

        public Vector3[] _edgeNormal_1_OrSharp {
            get {
                if (edgeNormal1 == null)
                {
                    edgeNormal1 = new Vector3[vertsCount];

                    foreach (var tri in edMesh.triangles)
                    {
                        UVpoint up = tri.uvpnts[1];
                        edgeNormal1[up.finalIndex] = sharpNormals[up.finalIndex];

                        UVpoint up0 = tri.uvpnts[0];
                        UVpoint up2 = tri.uvpnts[2];

                        var tris = up0.vert.getTrianglesFromLine(up2.vert);

                        var nrm = tris.SmoothVector();

                        edgeNormal1[up0.finalIndex] = nrm;
                        edgeNormal1[up2.finalIndex] = nrm;
                    }
                }
                return edgeNormal1;
            }
        }

        public Vector3[] _edgeNormal_2_OrSharp {
            get
            {
                if (edgeNormal2 == null) {
                    edgeNormal2 = new Vector3[vertsCount];

                    foreach (var tri in edMesh.triangles) {
                        UVpoint up = tri.uvpnts[2];
                        edgeNormal2[up.finalIndex] = sharpNormals[up.finalIndex];

                        UVpoint up0 = tri.uvpnts[0];
                        UVpoint up1 = tri.uvpnts[1];

                        var tris = up0.vert.getTrianglesFromLine(up1.vert);

                        var nrm = tris.SmoothVector();

                        edgeNormal2[up0.finalIndex] = nrm;
                        edgeNormal2[up1.finalIndex] = nrm;
                    }
                }
                return edgeNormal2;
            }
        }

        public Vector4[] _tangents
        {
            get
            {

                if (Tangents == null)
                {

                    Tangents = new Vector4[vertsCount];
                    Vector3[] tan1 = new Vector3[vertsCount];
                    Vector3[] tan2 = new Vector3[vertsCount];

                    int tri = 0;

                    foreach (var t in edMesh.triangles) {

                        var i1 = t.uvpnts[0].finalIndex;
                        var i2 = t.uvpnts[1].finalIndex;
                        var i3 = t.uvpnts[2].finalIndex;

                        Vector3 v1 = t.uvpnts[0].vert.pos;
                        Vector3 v2 = t.uvpnts[1].vert.pos;
                        Vector3 v3 = t.uvpnts[2].vert.pos;

                        Vector2 w1 = t.uvpnts[0].getUV(0);// texcoords[i1];
                        Vector2 w2 = t.uvpnts[1].getUV(0);
                        Vector2 w3 = t.uvpnts[2].getUV(0);

                        float x1 = v2.x - v1.x;
                        float x2 = v3.x - v1.x;
                        float y1 = v2.y - v1.y;
                        float y2 = v3.y - v1.y;
                        float z1 = v2.z - v1.z;
                        float z2 = v3.z - v1.z;

                        float s1 = w2.x - w1.x;
                        float s2 = w3.x - w1.x;
                        float t1 = w2.y - w1.y;
                        float t2 = w3.y - w1.y;

                        float r = 1.0f / (s1 * t2 - s2 * t1);
                        Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
                        Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

                        tan1[i1] += sdir;
                        tan1[i2] += sdir;
                        tan1[i3] += sdir;

                        tan2[i1] += tdir;
                        tan2[i2] += tdir;
                        tan2[i3] += tdir;

                        tri += 3;

                    }



                    for (int i = 0; i < (vertsCount); i++) {

                        Vector3 n = normals[i];
                        Vector3 t = tan1[i];

                        // Gram-Schmidt orthogonalize
                        Vector3.OrthoNormalize(ref n, ref t);

                        Tangents[i].x = t.x;
                        Tangents[i].y = t.y;
                        Tangents[i].z = t.z;

                        // Calculate handedness
                        Tangents[i].w = (Vector3.Dot(Vector3.Cross(n, t), tan2[i]) < 0.0f) ? -1.0f : 1.0f;

                    }

                   
                }

                return Tangents;
            }
        }


    public Countless<vertexAnimationFrame> _anim {  get { if (anims == null) {

                    List<int> frameInds = edMesh.hasFrame.GetItAll();

                    anims = new Countless<vertexAnimationFrame>();

                    foreach (int i in frameInds)
                        anims[i] = new vertexAnimationFrame();

                    foreach( var vp in edMesh.vertices){
                    List<Vector3> framesOfVertex = vp.anim.GetAllObjsNoOrder();
                        for (int j = 0; j < frameInds.Count; j++) 
                            anims[frameInds[j]].verts[vp.index] = framesOfVertex[j]; // This is likely to be super wrong
                    }
                }

                return anims;
            }
        }

        public MeshConstructor(EditableMesh edmesh, MeshSolutionProfile solution, Mesh fmesh)
        {
            profile = solution;
            edMesh = edmesh;
            mesh = fmesh;
            if (mesh == null)
                mesh = new Mesh();

            GeneratePreConstructionData();
            profile.StartPacking(this);

           // vertsCount = edMesh.vertices.Count;

            if (edMesh.gotBindPos) {
                bindPoses = new Matrix4x4[vertsCount];
                for (int i = 0; i < edMesh.vertices.Count; i++)
                    bindPoses[i] = edMesh.vertices[i].bindPoses;
                mesh.bindposes = bindPoses;
            }

            if (edMesh.gotBoneWeights) {
                boneWeights = new BoneWeight[vertsCount];
                for (int i = 0; i < edMesh.vertices.Count; i++)
                    boneWeights[i] = edMesh.vertices[i].boneWeight;
               // Debug.Log("verts "+vertsCount+"  actual " + mesh.vertices.Length);
                mesh.boneWeights = boneWeights;
            }

            int vCnt = mesh.vertices.Length;

            if (edMesh.shapes!= null)
            for (int s=0; s<edMesh.shapes.Count; s++){
                var name = edMesh.shapes[s];
                int frames = edMesh.vertices[0].shapes[s].Count;
                
                for (int f=0; f<frames; f++) {
                    
                    var pos = new Vector3[vCnt];
                    var nrm = new Vector3[vCnt];
                    var tng = new Vector3[vCnt];

                    for (int v=0; v<vCnt; v++) {
                        BlendFrame bf = edMesh.uvsByFinalIndex[v].vert.shapes[s][f];

                        pos[v] = bf.deltaPosition;
                        nrm[v] = bf.deltaNormal;
                        tng[v] = bf.deltaTangent;

                    }
                    mesh.AddBlendShapeFrame(name, edMesh.blendWeights[s][f],pos,nrm,tng);
                }
            }

            mesh.name = edmesh.meshName;
            // TODO: Add a function that will return blend shapes to where they should be

        }

        public void CopyMeshTo(ref Mesh other) {
            if ((verts == null) || (tris == null) || (verts.Length < 3) || (tris.Length < 3) || (mesh == null)) return;
            if (other == null) other = new Mesh();
            other.Clear();
            other.vertices = mesh.vertices;
            other.uv = mesh.uv;
            other.uv2 = mesh.uv2;
            other.uv3 = mesh.uv3;
            other.uv4 = mesh.uv4;
            other.triangles = mesh.triangles;
            other.tangents = mesh.tangents;
            other.colors = mesh.colors;
            other.normals = mesh.normals;
        }

     
        public void AssignMeshAsCollider(MeshCollider c)  {
            c.sharedMesh = null;
            c.sharedMesh = mesh;
        }

        public void AssignMesh(GameObject go) {
            AssignMesh(go.GetComponent<MeshFilter>(), go.GetComponent<MeshCollider>());

        }

        public void AssignMesh(MeshFilter m, MeshCollider c) {
            if ((tris == null) || (tris.Length < 3)) return;
            if (m!= null)
            m.sharedMesh = mesh;
            if (c != null) {
                c.sharedMesh = null;
                c.sharedMesh = m.sharedMesh;
            }
        }

    }

    public enum MegavoxelRole { Solid, Damaged, Decorative }

    [Serializable]
    public class vertexAnimationFrame : CanCopy<vertexAnimationFrame> {
        public Countless<Vector3> verts;
        [NonSerialized]
       // public vertAnimNo animTexLines;

        public static ArrayManager<vertexAnimationFrame> array = new ArrayManager<vertexAnimationFrame>();
        public ArrayManager<vertexAnimationFrame> getArrMan() {
            return array;
        }

        public vertexAnimationFrame DeepCopy() {
            vertexAnimationFrame tmp = new vertexAnimationFrame();
            return tmp;
        }

        public vertexAnimationFrame() {
            verts = new Countless<Vector3>();
        }
    }


}