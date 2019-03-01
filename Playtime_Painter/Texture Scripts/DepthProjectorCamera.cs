using PlayerAndEditorGUI;
using QuizCannersUtilities;
using UnityEngine;

namespace Playtime_Painter
{
    [ExecuteInEditMode]
    public class DepthProjectorCamera : PainterStuffMono, IPEGI
    {
        private static DepthProjectorCamera _inst;

        public static DepthProjectorCamera Instance
        {
            get
            {
                if (_inst)
                    return _inst;

                _inst = FindObjectOfType<DepthProjectorCamera>();

                return _inst;

            }
        }

       // [SerializeField] private Camera _cameraToReplaceShader;

        [SerializeField] private Camera projectorCamera;
        [SerializeField] private RenderTexture _depthTarget;
        public int targetSize = 512;
        public float shadowBias = 0.005f;


        private bool foldOut;
        public bool Inspect()
        {
            var changed = false;

            if ("Projector Camera".foldout(ref foldOut).nl_ifFoldedOut())
            {
               // if ("Texture Size".select(ref targetSize, PainterCamera.Tex  ))
                    //UpdateDepthCamera()

            }

            this.ClickHighlight().nl();




            return changed;
        }
        
        void OnEnable()
        {
          

            if (!projectorCamera)
                projectorCamera = GetComponent<Camera>();

            if (!projectorCamera)
                projectorCamera = gameObject.AddComponent<Camera>();

         /*   if (_cameraToReplaceShader)
                _cameraToReplaceShader.SetReplacementShader(Shader.Find("Playtime Painter/ReplacementShaderTest"),
                    "RenderType");*/

      

         UpdateDepthCamera();

            _inst = this;

        }

        void Update()
        {
            if (projectorCamera)
                projectorCamera.Render();
        }
        
        #region Global Shader Parameters
        void UpdateDepthCamera()
        {
            if (!projectorCamera) return;

            if (projectorCamera)
            {
                projectorCamera.enabled = false;
                projectorCamera.depthTextureMode = DepthTextureMode.None;
                projectorCamera.depth = -1000;
                projectorCamera.clearFlags = CameraClearFlags.Depth;
            }
            
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

            projectorCamera.targetTexture = _depthTarget;
        }
        
        private readonly ShaderProperty.MatrixValue _spMatrix = new ShaderProperty.MatrixValue("pp_ProjectorMatrix");
        private readonly ShaderProperty.TextureValue _spDepth = new ShaderProperty.TextureValue("pp_DepthProjection");
        private readonly ShaderProperty.VectorValue _spPos = new ShaderProperty.VectorValue("pp_ProjectorPosition");
        private readonly ShaderProperty.VectorValue _spZBuffer = new ShaderProperty.VectorValue("pp_ProjectorClipPrecompute");
        private readonly ShaderProperty.VectorValue _camParams = new ShaderProperty.VectorValue("pp_ProjectorConfiguration");

        private void OnPostRender() {

            if (!projectorCamera) return;

            _spMatrix.GlobalValue = projectorCamera.projectionMatrix * projectorCamera.worldToCameraMatrix;
            _spDepth.GlobalValue = _depthTarget;
            _spPos.GlobalValue = transform.position.ToVector4(0);

            var far = projectorCamera.farClipPlane;
            var near = projectorCamera.nearClipPlane;

            _camParams.GlobalValue = new Vector4(
                projectorCamera.aspect,
                Mathf.Tan(projectorCamera.fieldOfView * Mathf.Deg2Rad * 0.5f),
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