using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Playtime_Painter{

[ExecuteInEditMode]
public class RenderBrush : PainterStuffMono {

		//public static PainterManager rtp {get {return PainterManager.inst;}}

    public MeshRenderer meshRendy;
    public MeshFilter meshFilter;
    [NonSerialized] public Mesh modifiedMesh;
    public Bounds modifiedBound;
   
        SkinnedMeshRenderer changedSkinnedMeshRendy;
        GameObject changedGameObject;
    Material replacedTargetsMaterial;
        Material[] replacedBrushesMaterials;
        int modifiedSubmesh;
    int replacedLayer;
        public bool deformedBounds;

    public void RestoreBounds() {
           // return;
           if (replacedTargetsMaterial!= null) {

                var lst = changedSkinnedMeshRendy.sharedMaterials;
                lst[modifiedSubmesh] = replacedTargetsMaterial;
                changedSkinnedMeshRendy.sharedMaterials = lst;

                changedSkinnedMeshRendy.localBounds = modifiedBound;
                changedGameObject.layer = replacedLayer;
                replacedTargetsMaterial = null;
                meshRendy.enabled = true;

            } else
            {
                transform.parent = PainterManager.inst.transform;
                modifiedMesh.bounds = modifiedBound;

                meshRendy.materials = replacedBrushesMaterials;
            }

            deformedBounds = false;

        }



        public void UseMeshAsBrush (PlaytimePainter painter) {

            GameObject go = painter.gameObject;
            Transform camTransform = PainterManager.inst.transform;

            var skinny = painter.skinnedMeshRendy;

            if (skinny != null) 
                UseSkinMeshAsBrush(go, skinny, painter.selectedSubmesh);
            else 
                UseMeshAsBrush(go, painter.getMesh(), new List<int> { painter.selectedSubmesh });
        }

        public void UseSkinMeshAsBrush(GameObject go, SkinnedMeshRenderer skinny, int submesh)
        {
            modifiedSubmesh = submesh;

            meshRendy.enabled = false;

            Transform camTransform = PainterManager.inst.transform;

            changedSkinnedMeshRendy = skinny;
            changedGameObject = go;

            modifiedBound = skinny.localBounds;
            skinny.localBounds = new Bounds(go.transform.InverseTransformPoint(camTransform.position + camTransform.forward * 100), Vector3.one * 15000f);

            replacedLayer = go.layer;
            go.layer = gameObject.layer;

            replacedTargetsMaterial = skinny.sharedMaterials[modifiedSubmesh];
            var lst = skinny.sharedMaterials;
            lst[modifiedSubmesh] = meshRendy.sharedMaterial;
            skinny.sharedMaterials = lst;

            deformedBounds = true;
        }

        public void UseMeshAsBrush (GameObject go, Mesh mesh, List<int> selectedSubmeshes) {

            Transform camTransform = PainterManager.inst.transform;

            Transform target = go.transform;

            transform.position = target.position;
            transform.rotation = target.rotation;
            transform.localScale = target.localScale;

            modifiedMesh = mesh;
            meshFilter.sharedMesh = mesh;

            modifiedBound = modifiedMesh.bounds;
            modifiedMesh.bounds = new Bounds(transform.InverseTransformPoint(camTransform.position + camTransform.forward * 100), Vector3.one * 500f);
            transform.parent = target.parent;

            replacedBrushesMaterials = meshRendy.sharedMaterials;

            int max = 0;

            foreach (var e in selectedSubmeshes)
                max = Mathf.Max(e, max);

            if (max > 0)
            {
                var mats = new Material[max + 1];
                foreach (var e in selectedSubmeshes)
                    mats[e] = meshRendy.sharedMaterial;
                meshRendy.materials = mats;
            }

            deformedBounds = true;
        }

    public RenderBrush  Set(Shader shade) {
        meshRendy.sharedMaterial.shader = shade;
            return this;
    }

        public RenderBrush Set(Texture tex) {
            meshRendy.sharedMaterial.SetTexture("_MainTex", tex);
            return this;
        }

        public RenderBrush Set(Color col)
        {
            meshRendy.sharedMaterial.SetColor("_Color", col);
            return this;
        }

        public void FullScreenQuad() {
            float size = PainterManager.orthoSize * 2;
            transform.localScale = new Vector3(size , size, 0);
            transform.localPosition = Vector3.forward * 10;
            transform.localRotation = Quaternion.identity;
            meshFilter.mesh = brushMeshGenerator.inst().GetQuad();
        }

        public RenderBrush CopyBuffer(Texture tex, RenderTexture onto, Shader shade) => CopyBuffer(tex, onto, null, shade);

        public RenderBrush CopyBuffer(Texture tex, RenderTexture onto, Material mat) => CopyBuffer(tex, onto, mat, null);

        public RenderBrush CopyBuffer(Texture tex, RenderTexture onto, Material material, Shader shade)
        {

            if (tex != null && onto != null)
            {
                
                float size = PainterManager.orthoSize * 2;
                float aspectRatio = (float)tex.width / (float)tex.height;

                float ar2 = onto.width / onto.height;

                aspectRatio = ar2 / aspectRatio;
                texMGMT.rtcam.targetTexture = onto;

                transform.localScale = new Vector3(size * aspectRatio, size, 0);
                transform.localPosition = Vector3.forward * 10;
                transform.localRotation = Quaternion.identity;
                meshFilter.mesh = brushMeshGenerator.inst().GetQuad();

                Shader.SetGlobalFloat(PainterConfig.bufferCopyAspectRation, 1f/aspectRatio);
               
                if (material)
                {
                    var tmpMat = meshRendy.material;
                    meshRendy.material = material;
                    if (tex)
                        Set(tex);
                    texMGMT.Render();
                    meshRendy.material = tmpMat;
                }
                else
                {
                  
                    if (shade == null)
                        shade = texMGMT.pixPerfectCopy;
                    Set(shade);
                    Set(tex);
                    texMGMT.Render();
                }
                
               
                if (aspectRatio != 1)
                    Shader.SetGlobalFloat(PainterConfig.bufferCopyAspectRation, 1);

            }

            return this;
        }

        public void PrepareColorPaint(Color col)
        {
            float size = PainterManager.orthoSize * 2;
            transform.localScale = new Vector3(size , size, 0);
            transform.localPosition = Vector3.forward * 10;
            transform.localRotation = Quaternion.identity;
            meshFilter.mesh = brushMeshGenerator.inst().GetQuad();
            Set(texMGMT.br_ColorFill).Set(col);
        }


        private void OnEnable()
        {
            Shader.SetGlobalFloat(PainterConfig.bufferCopyAspectRation, 1f);
        }

        // Use this for initialization
        void Awake () {
        if (meshRendy == null)
            meshRendy = GetComponent<MeshRenderer>();

        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();

	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
}