using PlayerAndEditorGUI;
using QuizCannersUtilities;
using UnityEngine;

namespace Playtime_Painter
{
    [ExecuteInEditMode]
    public class DepthProjectorCamera : PainterSystemMono
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
        [SerializeField] private bool _projectFromMainCamera;
        [SerializeField] private bool _centerOnMousePosition;
        [SerializeField] private bool _matchMainCamera;
        [SerializeField] private bool _pauseUpdates;


        public int targetSize = 512;
        
        private bool _foldOut;
        public override bool Inspect()
        {
            var changed = false;

            if ("Projector Camera ".enter(ref _foldOut).nl_ifFoldedOut()) {

                "Target Size".edit(ref targetSize).changes(ref changed);
                if (icon.Refresh.Click("Recreate Depth Texture").nl(ref changed))
                {
                    _depthTarget.DestroyWhatever();
                    _depthTarget = null;
                    UpdateDepthCamera();
                }

                if (_projectorCamera) {
                    var fov = _projectorCamera.fieldOfView;

                    pegi.toggle(ref _pauseUpdates, icon.Pause, icon.Play,
                        _pauseUpdates ? "Resume Updates" : "Pause Updates").changes(ref changed);

                    if ("FOV".edit(30, ref fov, 0.1f, 180f).nl(ref changed))
                        _projectorCamera.fieldOfView = fov;

                    "Project from Camera".toggleIcon("Will always project from Play or Editor Camera" ,ref _projectFromMainCamera).nl(ref changed);

                    if (_projectFromMainCamera)
                    {
                        "Match Main Camera's config".toggleIcon(ref _matchMainCamera).nl(ref changed);
                        "Follow the mouse".toggleIcon(ref _centerOnMousePosition).nl(ref changed);
                    }

                }

                // if ("Texture Size".select(ref targetSize, PainterCamera.Tex  ))
                //UpdateDepthCamera()

            } else this.ClickHighlight().nl();




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

        private void LateUpdate()
        {
            if (_projectorCamera && _projectFromMainCamera)
            {

                if (Application.isPlaying)
                {
                    if (PainterCamera.Inst)
                    {
                        var cam = PainterCamera.Inst.MainCamera;

                        if (cam)
                        {
                            transform.parent = cam.transform;
                            transform.localScale = Vector3.one;
                            transform.localPosition = Vector3.zero;

                            if (_centerOnMousePosition)
                                transform.LookAt(transform.position + cam.ScreenPointToRay(Input.mousePosition).direction);
                            else
                                transform.localRotation = Quaternion.identity;
                        }
                    }
                }
                else
                {
                    transform.parent = null;
                    var ray = _centerOnMousePosition
                        ? EditorInputManager.mouseRaySceneView
                        : EditorInputManager.centerRaySceneView;

                    transform.position = ray.origin;
                    transform.LookAt(ray.origin + ray.direction);

                }

            }

        }

        private void Update()
        {
            if (_projectorCamera) {

           
                if (!_pauseUpdates)
                    _projectorCamera.Render();
            }
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