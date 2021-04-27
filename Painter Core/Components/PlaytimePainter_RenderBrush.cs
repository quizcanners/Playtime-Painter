using System;
using System.Collections.Generic;
using QuizCanners.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace PainterTool
{

    [ExecuteInEditMode]
    [AddComponentMenu("Playtime Painter/Painter Brush")]
    public class PlaytimePainter_RenderBrush : PainterSystemMono
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

        [NonSerialized] private readonly MaterialInstancer.ForMeshRenderer materialInstancer = new() { InstantiateInEditor = true };

        public override string InspectedCategory => nameof(PainterComponent);

        public Material GetMaterial() => materialInstancer.GetMaterialInstance(meshRenderer);
        
        public void AfterRender() 
        {
            if (!deformedBounds)
                return;

            deformedBounds = false;

            if (_replacedTargetsMaterial)
            {
                var lst = _changedSkinnedMeshRenderer.sharedMaterials;
                lst[_modifiedSubMesh] = _replacedTargetsMaterial;
                _changedSkinnedMeshRenderer.sharedMaterials = lst;

                _changedSkinnedMeshRenderer.localBounds = modifiedBound;
                _changedGameObject.layer = _replacedLayer;
                _replacedTargetsMaterial = null;
                meshRenderer.enabled = true;
            }
            else 
            {
                transform.parent = Painter.Camera.transform;
                _modifiedMesh.bounds = modifiedBound;

                meshRenderer.materials = _replacedBrushesMaterials;
            }
        }
        
        public void PrepareWorldSpace(Painter.Command.WorldSpaceBase command) 
        {
            if (command.SkinnedMeshRenderer) 
                UseSkinMeshAsBrush(command); 
            else
                UseMeshAsBrush(command);
        }

        private void UseSkinMeshAsBrush(Painter.Command.WorldSpaceBase command) 
        {
            GameObject go = command.GameObject;
            SkinnedMeshRenderer skinny = command.SkinnedMeshRenderer;
            int subMesh = command.SubMeshIndexFirst;

            _modifiedSubMesh = subMesh;

            meshRenderer.enabled = false;

            var camTransform = Painter.Camera.transform;

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


        public void UseMeshAsBrush(Painter.Command.WorldSpaceBase command) 
        {
            GameObject go = command.GameObject;
            Mesh mesh = command.Mesh;
            List<int> selectedSubMeshes = command.SelectedSubMeshes;

            UseMeshAsBrush(go, mesh, selectedSubMeshes);
        }

        public void UseMeshAsBrush(GameObject go, Mesh mesh, List<int> selectedSubMeshes)
        {
            if (!mesh)
            {
                Debug.LogError("No mesh in Use Mesh As Brush Command");
                return;
            }

            if (selectedSubMeshes.IsNullOrEmpty())
            {
                QcLog.ChillLogger.LogErrosExpOnly(()=> "Painter.Command.WorldSpace arrived with unassigned selectedSubmeshes array. Seeting 0", key: "emptSbM");
                selectedSubMeshes = new List<int>(1) {0};
            }

            var camTransform = Painter.Camera.transform;

            var target = go.transform;

            var tf = transform;
            
            tf.SetPositionAndRotation(target.position, target.rotation);
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

        private readonly ShaderProperty.TextureValue _mainTex = new("_MainTex");


        public PlaytimePainter_RenderBrush Set(Shader shade)
        {
            GetMaterial().shader = shade;

            if (!shade)
                Debug.LogError("Shader not found");


            return this;
        }

        public PlaytimePainter_RenderBrush Set(Texture tex)
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
            const float size = Singleton_PainterCamera.OrthographicSize * 2;
            var tf = transform;
            tf.localScale = new Vector3(size, size, 0);
            tf.SetLocalPositionAndRotation(Vector3.forward * 10, Quaternion.identity);
            meshFilter.mesh = Painter.BrushMeshGenerator.GetQuad();
        }

        public RenderTexture CopyBuffer(Texture tex, RenderTexture onto, Shader shade) => CopyBuffer(tex, onto, null, shade);

        public RenderTexture CopyBuffer(Texture tex, RenderTexture onto, Material mat) => CopyBuffer(tex, onto, mat, null);

        private RenderTexture CopyBuffer(Texture tex, RenderTexture onto, Material material, Shader shade)
        {
            if (!tex || !onto) return onto;
            
            const float size = Singleton_PainterCamera.OrthographicSize * 2;
            
            var aspectRatio = tex.width / (float)tex.height;

            var ar2 = onto.width / (float)onto.height;

            if (tex.GetType() == typeof(RenderTexture))
            {
                aspectRatio = ar2 / aspectRatio;
            }
            else 
            {
                aspectRatio = 1; 
            }

            Painter.Camera.TargetTexture = onto;

            PainterShaderVariables.SourceTextureSize = tex;
            PainterShaderVariables.BufferCopyAspectRatio.GlobalValue = 1f / aspectRatio;

            var tf = transform;
            tf.localScale = new Vector3(size * aspectRatio, size, 0);
            tf.SetLocalPositionAndRotation(Vector3.forward * 10, Quaternion.identity);
            meshFilter.mesh = Painter.BrushMeshGenerator.GetQuad();

            if (material)
            {
                var tmpMat = meshRenderer.material;
                meshRenderer.material = material;
                if (tex)
                    Set(tex);
                Painter.Camera.Render();
                meshRenderer.material = tmpMat;
            }
            else
            {

                if (!shade)
                    shade = Painter.Data.pixPerfectCopy.Shader;

                Set(shade);
                Set(tex);
                Painter.Camera.Render();
            }


            if (Math.Abs(aspectRatio - 1) > float.Epsilon)
                PainterShaderVariables.BufferCopyAspectRatio.GlobalValue = 1;

            return onto;
        }

        public void PrepareColorPaint(Color col)
        {
            const float size = Singleton_PainterCamera.OrthographicSize * 2;

            var tf = transform;
            
            tf.localScale = new Vector3(size, size, 0);
            tf.SetLocalPositionAndRotation(Vector3.forward * 10, Quaternion.identity);
            meshFilter.mesh = Painter.BrushMeshGenerator.GetQuad();
            PainterShaderVariables.BrushColorProperty.GlobalValue = col;
            Set(Painter.Data.bufferColorFill); //.Set(col);
        }
        
        protected override void OnAfterEnable()
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

            gameObject.layer = Painter.Data.playtimePainterLayer;

            PainterShaderVariables.BufferCopyAspectRatio.GlobalValue = 1f;
        }
    }
}