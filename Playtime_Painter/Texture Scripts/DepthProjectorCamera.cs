using PlayerAndEditorGUI;
using QuizCannersUtilities;
using UnityEngine;

namespace Playtime_Painter
{
    [ExecuteInEditMode]
    public class DepthProjectorCamera : PainterStuffMono
    {
      
        public static DepthProjectorCamera Instance
        {
            get
            {
                if (PainterCamera.depthProjectorCamera)
                    return PainterCamera.depthProjectorCamera;

                PainterCamera.depthProjectorCamera = FindObjectOfType<DepthProjectorCamera>();

                return PainterCamera.depthProjectorCamera;

            }
        }

        [SerializeField] private Camera _projectorCamera;
        [SerializeField] private RenderTexture _depthTarget;
        public int targetSize = 512;
        public float shadowBias = 0.005f;


        private bool _foldOut;
        public override bool Inspect()
        {
            const bool changed = false;

            if ("Projector Camera".foldout(ref _foldOut).nl_ifFoldedOut())
            {
               // if ("Texture Size".select(ref targetSize, PainterCamera.Tex  ))
                    //UpdateDepthCamera()

            }

            this.ClickHighlight().nl();




            return changed;
        }

        private void OnEnable() {

            if (!_projectorCamera) {
                _projectorCamera = GetComponent<Camera>();

                if (!_projectorCamera)
                    _projectorCamera = gameObject.AddComponent<Camera>();
            }

            UpdateDepthCamera();

            PainterCamera.depthProjectorCamera = this;

        }

        private void Update()
        {
            if (_projectorCamera)
                _projectorCamera.Render();
        }
        
        #region Global Shader Parameters

        private void UpdateDepthCamera()
        {
            if (!_projectorCamera) return;
            
            _projectorCamera.enabled = false;
            _projectorCamera.depthTextureMode = DepthTextureMode.None;
            _projectorCamera.depth = -1000;
            _projectorCamera.clearFlags = CameraClearFlags.Depth;

            var l = Cfg ? Cfg.playtimePainterLayer : 30;

            _projectorCamera.cullingMask &= ~(1 << l);
            
            if (_depthTarget && _depthTarget.width == targetSize) return;

            if (_depthTarget)
                _depthTarget.DestroyWhatever_UObj();

            var sz = Mathf.Max(targetSize, 16);

            _depthTarget = new RenderTexture(sz, sz, 32, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear) {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                autoGenerateMips = false,
                useMipMap = false
            };

            _projectorCamera.targetTexture = _depthTarget;
        }
        
        private readonly ShaderProperty.MatrixValue _spMatrix = new ShaderProperty.MatrixValue("pp_ProjectorMatrix");
        private readonly ShaderProperty.TextureValue _spDepth = new ShaderProperty.TextureValue("pp_DepthProjection");
        private readonly ShaderProperty.VectorValue _spPos = new ShaderProperty.VectorValue("pp_ProjectorPosition");
        private readonly ShaderProperty.VectorValue _spZBuffer = new ShaderProperty.VectorValue("pp_ProjectorClipPrecompute");
        private readonly ShaderProperty.VectorValue _camParams = new ShaderProperty.VectorValue("pp_ProjectorConfiguration");

        private void OnPostRender() {

            if (!_projectorCamera) return;

            _spMatrix.GlobalValue = _projectorCamera.projectionMatrix * _projectorCamera.worldToCameraMatrix;
            _spDepth.GlobalValue = _depthTarget;
            _spPos.GlobalValue = transform.position.ToVector4(0);

            var far = _projectorCamera.farClipPlane;
            var near = _projectorCamera.nearClipPlane;

            _camParams.GlobalValue = new Vector4(
                _projectorCamera.aspect,
                Mathf.Tan(_projectorCamera.fieldOfView * Mathf.Deg2Rad * 0.5f),
                near,
                1f/far);
            
            var zBuff = new Vector4(1f - far / near, far / near, 0, 0);
            zBuff.z = 1 / zBuff.x; //zBuff.x / far;
                //zBuff.w = zBuff.y / far;

            _spZBuffer.GlobalValue = zBuff;
            
        }

        #endregion

    }
}