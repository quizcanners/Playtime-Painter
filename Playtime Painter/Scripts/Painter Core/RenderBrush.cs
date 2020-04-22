using System;
using System.Collections.Generic;
using QuizCannersUtilities;
using UnityEngine;
using UnityEngine.Rendering;

namespace PlaytimePainter
{

    [ExecuteInEditMode]
    public class RenderBrush : PainterSystemMono
    {

        [SerializeField] private MeshRenderer meshRenderer;
        public MeshFilter meshFilter;
        public Bounds modifiedBound;


        [NonSerialized] private Mesh _modifiedMesh;
        private SkinnedMeshRenderer _changedSkinnedMeshRenderer;
        private GameObject _changedGameObject;
        
        private Material _replacedTargetsMaterial;
        private Material[] _replacedBrushesMaterials;
        private int _modifiedSubMesh;
        private int _replacedLayer;
        public bool deformedBounds;

        [NonSerialized] QcUtils.MeshMaterialPlaytimeInstancer materialInstancer = new QcUtils.MeshMaterialPlaytimeInstancer(true);

        public Material GetMaterial() => materialInstancer.GetMaterialInstance(meshRenderer);
        
        public void AfterRender() {

            if (!deformedBounds)
                return;

            if (_replacedTargetsMaterial) {

                var lst = _changedSkinnedMeshRenderer.sharedMaterials;
                lst[_modifiedSubMesh] = _replacedTargetsMaterial;
                _changedSkinnedMeshRenderer.sharedMaterials = lst;

                _changedSkinnedMeshRenderer.localBounds = modifiedBound;
                _changedGameObject.layer = _replacedLayer;
                _replacedTargetsMaterial = null;
                meshRenderer.enabled = true;

            }
            else {
                transform.parent = PainterCamera.Inst.transform;
                _modifiedMesh.bounds = modifiedBound;

                meshRenderer.materials = _replacedBrushesMaterials;
            }

            deformedBounds = false;

        }
        
        public void Prepare(PaintCommand.WorldSpace command) 
        {
            if (command.SkinnedMeshRenderer) 
                UseSkinMeshAsBrush(command); 
            else
                UseMeshAsBrush(command);
        }

        private void UseSkinMeshAsBrush(PaintCommand.WorldSpace command) 
        {
            GameObject go = command.GameObject;
            SkinnedMeshRenderer skinny = command.SkinnedMeshRenderer;
            int subMesh = command.SubMeshIndexFirst;

            _modifiedSubMesh = subMesh;

            meshRenderer.enabled = false;

            var camTransform = PainterCamera.Inst.transform;

            _changedSkinnedMeshRenderer = skinny;
            _changedGameObject = go;

            modifiedBound = skinny.localBounds;
            skinny.localBounds = new Bounds(go.transform.InverseTransformPoint(camTransform.position + camTransform.forward * 100), Vector3.one * 15000f);

            _replacedLayer = go.layer;
            go.layer = gameObject.layer;

  
            var lst = skinny.sharedMaterials;
            
            _replacedTargetsMaterial = lst[_modifiedSubMesh];
          
            lst[_modifiedSubMesh] = meshRenderer.sharedMaterial;
            skinny.sharedMaterials = lst;

            deformedBounds = true;
        }

        private void UseMeshAsBrush(PaintCommand.WorldSpace command)
        {

            GameObject go = command.GameObject;
            Mesh mesh = command.Mesh;
            List<int> selectedSubMeshes = command.SelectedSubmeshes;

            if (selectedSubMeshes.IsNullOrEmpty())
            {
                QcUtils.ChillLogger.LogErrorOnce("emptSbM", ()=> "PaintCommand.WorldSpace arrived with unassigned selectedSubmeshes array. Seeting 0");
                selectedSubMeshes = new List<int>(1) {0};
            }

            var camTransform = TexMGMT.transform;

            var target = go.transform;

            var tf = transform;
            
            tf.position = target.position;
            tf.rotation = target.rotation;
            tf.localScale = target.localScale;

            _modifiedMesh = mesh;
            meshFilter.sharedMesh = mesh;

            modifiedBound = _modifiedMesh.bounds;
            _modifiedMesh.bounds = new Bounds(transform.InverseTransformPoint(camTransform.position + camTransform.forward * 100), Vector3.one * 500f);
            tf.parent = target.parent;

            _replacedBrushesMaterials = meshRenderer.sharedMaterials;

            var max = 0;

            foreach (var e in selectedSubMeshes)
                max = Mathf.Max(e, max);

            if (max > 0)
            {
                var mats = new Material[max + 1];
                foreach (var e in selectedSubMeshes)
                    mats[e] = meshRenderer.sharedMaterial;
                meshRenderer.materials = mats;
            }

            deformedBounds = true;
        }

        public RenderBrush Set(Shader shade)
        {
            GetMaterial().shader = shade;
            return this;
        }

        private readonly ShaderProperty.TextureValue _mainTex = new ShaderProperty.TextureValue("_MainTex");

        private readonly ShaderProperty.ColorFloat4Value _colorVal = new ShaderProperty.ColorFloat4Value("_Color");

        public RenderBrush Set(Texture tex)
        {
            GetMaterial().Set(_mainTex, tex);
            return this;
        }

        // Not supported in Universal RP
        /*public RenderBrush Set(Color col) {
            GetMaterial().Set(_colorVal, col);
            return this;
        }*/

        public void FullScreenQuad()
        {
            const float size = PainterCamera.OrthographicSize * 2;
            var tf = transform;
            tf.localScale = new Vector3(size, size, 0);
            tf.localPosition = Vector3.forward * 10;
            tf.localRotation = Quaternion.identity;
            meshFilter.mesh = PainterCamera.BrushMeshGenerator.GetQuad();
        }

        public RenderTexture CopyBuffer(Texture tex, RenderTexture onto, Shader shade) => CopyBuffer(tex, onto, null, shade);

        public RenderTexture CopyBuffer(Texture tex, RenderTexture onto, Material mat) => CopyBuffer(tex, onto, mat, null);

        private RenderTexture CopyBuffer(Texture tex, RenderTexture onto, Material material, Shader shade)
        {
            if (!tex || !onto) return onto;
            
            const float size = PainterCamera.OrthographicSize * 2;
            
            var aspectRatio = tex.width / (float)tex.height;

            var ar2 = onto.width / (float)onto.height;
            
            if (tex.GetType() == typeof(RenderTexture))
                aspectRatio = ar2 / aspectRatio;
            else
                aspectRatio = 1;
            
            TexMGMT.TargetTexture = onto;

            var tf = transform;
            
            tf.localScale = new Vector3(size * aspectRatio, size, 0);
            tf.localPosition = Vector3.forward * 10;
            tf.localRotation = Quaternion.identity;
            meshFilter.mesh = PainterCamera.BrushMeshGenerator.GetQuad();

            PainterShaderVariables.SourceTextureSize = tex;
            PainterShaderVariables.BufferCopyAspectRatio.GlobalValue = 1f / aspectRatio;

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

                if (!shade)
                    shade = TexMgmtData.pixPerfectCopy;

                Set(shade);
                Set(tex);
                TexMGMT.Render();
            }


            if (Math.Abs(aspectRatio - 1) > float.Epsilon)
                PainterShaderVariables.BufferCopyAspectRatio.GlobalValue = 1;

            return onto;
        }

        public void PrepareColorPaint(Color col)
        {
            const float size = PainterCamera.OrthographicSize * 2;

            var tf = transform;
            
            tf.localScale = new Vector3(size, size, 0);
            tf.localPosition = Vector3.forward * 10;
            tf.localRotation = Quaternion.identity;
            meshFilter.mesh = PainterCamera.BrushMeshGenerator.GetQuad();
            PainterShaderVariables.BrushColorProperty.GlobalValue = col;
            Set(TexMgmtData.bufferColorFill); //.Set(col);
        }
        
        private void OnEnable()
        {
            if (!meshRenderer)
                meshRenderer = GetComponent<MeshRenderer>();

            if (!meshFilter)
                meshFilter = GetComponent<MeshFilter>();

            if (meshRenderer)
            {
                meshRenderer.receiveShadows = false;
                meshRenderer.shadowCastingMode = ShadowCastingMode.Off;

            }

            gameObject.layer = Cfg.playtimePainterLayer;

            PainterShaderVariables.BufferCopyAspectRatio.GlobalValue = 1f;
        }
    }
}