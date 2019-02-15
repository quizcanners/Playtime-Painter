using System;
using System.Collections.Generic;
using UnityEngine;

namespace Playtime_Painter
{

    [ExecuteInEditMode]
    public class RenderBrush : PainterStuffMono
    {

        public MeshRenderer meshRenderer;
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

        public void RestoreBounds()
        {

            if (replacedTargetsMaterial)
            {

                var lst = changedSkinnedMeshRendy.sharedMaterials;
                lst[modifiedSubmesh] = replacedTargetsMaterial;
                changedSkinnedMeshRendy.sharedMaterials = lst;

                changedSkinnedMeshRendy.localBounds = modifiedBound;
                changedGameObject.layer = replacedLayer;
                replacedTargetsMaterial = null;
                meshRenderer.enabled = true;

            }
            else
            {
                transform.parent = PainterCamera.Inst.transform;
                modifiedMesh.bounds = modifiedBound;

                meshRenderer.materials = replacedBrushesMaterials;
            }

            deformedBounds = false;

        }
        
        public void UseMeshAsBrush(PlaytimePainter painter)
        {

            var go = painter.gameObject;
            var camTransform = PainterCamera.Inst.transform;

            var skinny = painter.skinnedMeshRenderer;

            if (skinny)
                UseSkinMeshAsBrush(go, skinny, painter.selectedSubMesh);
            else
                UseMeshAsBrush(go, painter.GetMesh(), new List<int> { painter.selectedSubMesh });
        }

        public void UseSkinMeshAsBrush(GameObject go, SkinnedMeshRenderer skinny, int subMesh)
        {
            modifiedSubmesh = subMesh;

            meshRenderer.enabled = false;

            var camTransform = PainterCamera.Inst.transform;

            changedSkinnedMeshRendy = skinny;
            changedGameObject = go;

            modifiedBound = skinny.localBounds;
            skinny.localBounds = new Bounds(go.transform.InverseTransformPoint(camTransform.position + camTransform.forward * 100), Vector3.one * 15000f);

            replacedLayer = go.layer;
            go.layer = gameObject.layer;

            replacedTargetsMaterial = skinny.sharedMaterials[modifiedSubmesh];
            var lst = skinny.sharedMaterials;
            lst[modifiedSubmesh] = meshRenderer.sharedMaterial;
            skinny.sharedMaterials = lst;

            deformedBounds = true;
        }

        public void UseMeshAsBrush(GameObject go, Mesh mesh, List<int> selectedSubmeshes)
        {

            var camTransform = TexMGMT.transform;

            var target = go.transform;

            var tf = transform;
            
            tf.position = target.position;
            tf.rotation = target.rotation;
            tf.localScale = target.localScale;

            modifiedMesh = mesh;
            meshFilter.sharedMesh = mesh;

            modifiedBound = modifiedMesh.bounds;
            modifiedMesh.bounds = new Bounds(transform.InverseTransformPoint(camTransform.position + camTransform.forward * 100), Vector3.one * 500f);
            tf.parent = target.parent;

            replacedBrushesMaterials = meshRenderer.sharedMaterials;

            var max = 0;

            foreach (var e in selectedSubmeshes)
                max = Mathf.Max(e, max);

            if (max > 0)
            {
                var mats = new Material[max + 1];
                foreach (var e in selectedSubmeshes)
                    mats[e] = meshRenderer.sharedMaterial;
                meshRenderer.materials = mats;
            }

            deformedBounds = true;
        }

        public RenderBrush Set(Shader shade)
        {
            meshRenderer.sharedMaterial.shader = shade;
            return this;
        }

        public void Set(Texture tex) =>  meshRenderer.sharedMaterial.SetTexture("_MainTex", tex);
        

        private void Set(Color col) => meshRenderer.sharedMaterial.SetColor("_Color", col);
   

        public void FullScreenQuad()
        {
            const float size = PainterCamera.OrthographicSize * 2;
            var tf = transform;
            tf.localScale = new Vector3(size, size, 0);
            tf.localPosition = Vector3.forward * 10;
            tf.localRotation = Quaternion.identity;
            meshFilter.mesh = PainterCamera.BrushMeshGenerator.GetQuad();
        }

        public void CopyBuffer(Texture tex, RenderTexture onto, Shader shade) => CopyBuffer(tex, onto, null, shade);

        public void CopyBuffer(Texture tex, RenderTexture onto, Material mat) => CopyBuffer(tex, onto, mat, null);

        private void CopyBuffer(Texture tex, RenderTexture onto, Material material, Shader shade)
        {
            if (!tex || !onto) return;
            
            const float size = PainterCamera.OrthographicSize * 2;
            
            var aspectRatio = tex.width / (float)tex.height;

            var ar2 = onto.width / (float)onto.height;


#if UNITY_2018_1_OR_NEWER
            if (tex.GetType() == typeof(RenderTexture))
                aspectRatio = ar2 / aspectRatio;
            else
                aspectRatio = 1;
#else
                aspectRatio = ar2 / aspectRatio;
#endif

            TexMGMT.theCamera.targetTexture = onto;

            var tf = transform;
            
            tf.localScale = new Vector3(size * aspectRatio, size, 0);
            tf.localPosition = Vector3.forward * 10;
            tf.localRotation = Quaternion.identity;
            meshFilter.mesh = PainterCamera.BrushMeshGenerator.GetQuad();

            PainterDataAndConfig.BufferCopyAspectRatio.GlobalValue = 1f / aspectRatio;

            if (material)
            {
                var tmpMat = meshRenderer.material;
                meshRenderer.material = material;
                if (tex)
                    Set(tex);
                TexMGMT.Render();
                meshRenderer.material = tmpMat;
            }
            else
            {

                if (shade == null)
                    shade = TexMGMTdata.pixPerfectCopy;
                Set(shade);
                Set(tex);
                TexMGMT.Render();
            }


            if (Math.Abs(aspectRatio - 1) > float.Epsilon)
                PainterDataAndConfig.BufferCopyAspectRatio.GlobalValue = 1;

        }

        public void PrepareColorPaint(Color col)
        {
            const float size = PainterCamera.OrthographicSize * 2;

            var tf = transform;
            
            tf.localScale = new Vector3(size, size, 0);
            tf.localPosition = Vector3.forward * 10;
            tf.localRotation = Quaternion.identity;
            meshFilter.mesh = PainterCamera.BrushMeshGenerator.GetQuad();
            Set(TexMGMTdata.brushColorFill).Set(col);
        }
        
        private void OnEnable()
        {
            if (!meshRenderer)
                meshRenderer = GetComponent<MeshRenderer>();

            if (!meshFilter)
                meshFilter = GetComponent<MeshFilter>();

            PainterDataAndConfig.BufferCopyAspectRatio.GlobalValue = 1f;
        }

        private void Awake()
        {
            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();

            if (meshFilter == null)
                meshFilter = GetComponent<MeshFilter>();

        }


    }
}