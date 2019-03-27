using System;
using System.Collections.Generic;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using UnityEngine;

namespace Playtime_Painter
{
    [ExecuteInEditMode]
    public class DepthProjectorCamera : PainterSystemMono {





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
        [SerializeField] public bool pauseUpdates;
        
        public int targetSize = 512;

        #region Inspector
        private bool _foldOut;

        #if PEGI

        public override bool Inspect()
        {
            var changed = false;

            pegi.toggle(ref pauseUpdates, icon.Pause, icon.Play,
                pauseUpdates ? "Resume Updates" : "Pause Updates").changes(ref changed);

            if ("Projector Camera ".enter(ref _foldOut).nl_ifFoldedOut()) {

                "Target Size".edit(ref targetSize).changes(ref changed);
                if (icon.Refresh.Click("Recreate Depth Texture").nl(ref changed)) {
                    _depthTarget.DestroyWhatever();
                    _depthTarget = null;
                    UpdateDepthCamera();
                }

                if (_projectorCamera) {
                    var fov = _projectorCamera.fieldOfView;
                    
                    if ("FOV".edit(30, ref fov, 0.1f, 180f).nl(ref changed))
                        _projectorCamera.fieldOfView = fov;

                    "Project from Camera".toggleIcon("Will always project from Play or Editor Camera" ,ref _projectFromMainCamera).nl(ref changed);

                    if (_projectFromMainCamera) {
                        "Match Main Camera's config".toggleIcon(ref _matchMainCamera).nl(ref changed);
                        "Follow the mouse".toggleIcon(ref _centerOnMousePosition).nl(ref changed);
                    }
                }
            } else this.ClickHighlight().nl();

            return changed;
        }

        #endif
        #endregion

        private void OnEnable() {

            if (!_projectorCamera) {
                _projectorCamera = GetComponent<Camera>();

                if (!_projectorCamera)
                    _projectorCamera = gameObject.AddComponent<Camera>();
            }

            UpdateDepthCamera();

            PainterCamera.depthProjectorCamera = this;

        }

        public void ManagedUpdate()
        {
            if (_projectorCamera && _projectFromMainCamera) {

                if (Application.isPlaying) {
                    if (PainterCamera.Inst) {
                        var cam = PainterCamera.Inst.MainCamera;

                        if (cam) {
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

        private void LateUpdate()
        {
            if (Application.isPlaying)
                ManagedUpdate();
        }


        #region Other Updates 

        private int lastUpdatedUser = 0;

        private IUseDepthProjector userToGetUpdate;

        ProjectorCameraConfiguration painterProjectorCameraConfiguration = new ProjectorCameraConfiguration();
        
        [NonSerialized] private static readonly List<IUseDepthProjector> depthUsers = new List<IUseDepthProjector>();
        
        public static void SubscribeToDepthCamera(IUseDepthProjector pj) {
            if (!depthUsers.Contains(pj))
                depthUsers.Add(pj);

        }

        #endregion

        private void Update()
        {
            if (_projectorCamera) {
                if (!pauseUpdates)
                {
                    

                    if (lastUpdatedUser >= depthUsers.Count) {
                        lastUpdatedUser = 0;
                        userToGetUpdate = null;
                    }
                    else
                    {
                        userToGetUpdate = depthUsers[lastUpdatedUser];
                        lastUpdatedUser++;
                    }

                    if (userToGetUpdate != null)
                    {
                        painterProjectorCameraConfiguration.From(_projectorCamera);
                        userToGetUpdate.GetProjectorCameraConfiguration().To(_projectorCamera);
                    }

                    _projectorCamera.Render();
                    painterProjection.Set(_projectorCamera, _depthTarget);



                    if (userToGetUpdate != null)
                        painterProjectorCameraConfiguration.To(_projectorCamera);
                    

                }
            }
        }

        void OnPostRender()
        {
            if (userToGetUpdate !=null)
            {
                //userToGetUpdate.AfterDepthCameraRender();
                userToGetUpdate = null;

            }
        }

        #region Global Shader Parameters

        private void UpdateDepthCamera() {

            if (!_projectorCamera) return;
            
            _projectorCamera.enabled = false;
            _projectorCamera.depthTextureMode = DepthTextureMode.None;
            _projectorCamera.depth = -1000;
            _projectorCamera.clearFlags = CameraClearFlags.Depth;

            var l = Cfg ? Cfg.playtimePainterLayer : 30;

            _projectorCamera.cullingMask &= ~(1 << l);

            if (_depthTarget) {
                if (_depthTarget.width == targetSize)
                    return;
                else 
                    _depthTarget.DestroyWhateverUnityObject();
            }

            var sz = Mathf.Max(targetSize, 16);

            _depthTarget = new RenderTexture(sz, sz, 32, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear) {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                autoGenerateMips = false,
                useMipMap = false
            };

            _projectorCamera.targetTexture = _depthTarget;
        }
        
        private readonly ProjectorCameraParameters painterProjection = new ProjectorCameraParameters("pp_");

        //private void OnPostRender() {
            
            

           /* _spMatrix.GlobalValue = _projectorCamera.projectionMatrix * _projectorCamera.worldToCameraMatrix;
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
            zBuff.z = 1 / zBuff.x; 

            _spZBuffer.GlobalValue = zBuff;*/
      //  }

        /* private readonly ShaderProperty.MatrixValue _spMatrix = new     ShaderProperty.MatrixValue("pp_ProjectorMatrix");
           private readonly ShaderProperty.TextureValue _spDepth = new     ShaderProperty.TextureValue("pp_DepthProjection");
           private readonly ShaderProperty.VectorValue _spPos = new        ShaderProperty.VectorValue("pp_ProjectorPosition");
           private readonly ShaderProperty.VectorValue _spZBuffer = new    ShaderProperty.VectorValue("pp_ProjectorClipPrecompute");
           private readonly ShaderProperty.VectorValue _camParams = new    ShaderProperty.VectorValue("pp_ProjectorConfiguration");*/

        #endregion

    }

    public interface IUseDepthProjector
    {
        bool ProjectorReady();
        ProjectorCameraParameters GetProjectorCameraParameter();
        ProjectorCameraConfiguration GetProjectorCameraConfiguration();
        void AfterDepthCameraRender(Texture depthTexture);
    }

    [Serializable]
    public class ProjectorCameraConfiguration : AbstractCfg, IPEGI
    {
        public float fieldOfView;
        public Vector3 position;
        public Quaternion rotation;
        public float nearPlane;
        public float farPlane;
        private bool localTransform;

        public void From(Camera cam) {

            if (!cam) return;

            var tf = cam.transform;

            fieldOfView = cam.fieldOfView;
            position = localTransform ? tf.localPosition : tf.position;
            rotation = localTransform ? tf.localRotation : tf.rotation;
            nearPlane = cam.nearClipPlane;
            farPlane = cam.farClipPlane;
        }

        public void To(Camera cam) {

            if (!cam) return;

            var tf = cam.transform;

            cam.fieldOfView = fieldOfView;
            if (localTransform)
                tf.localPosition = position;
            else
                tf.position = position;

            if (localTransform)
                tf.localRotation = rotation;
            else
                tf.rotation = rotation;

            cam.nearClipPlane = nearPlane;
            cam.farClipPlane = farPlane;
        }

        #region Inspector

        [SerializeField] private Camera camera;
        public bool Inspect() {
            var changed = false;

            "Local".toggleIcon("Use local Position and rotation of the camera." ,ref localTransform).nl();

            "Tmp Camera".edit(ref camera).changes(ref changed);

            if (camera) {
                if (icon.Load.Click("Load configuration into camera"))
                    To(camera);
                if (icon.Save.Click("Save configuration from camera"))
                    From(camera);
            }

            pegi.nl();

            return changed;
        }

        #endregion

        #region Encode & Decode

        public override bool Decode(string tg, string data) {
            switch (tg) {
                case "fov": fieldOfView = data.ToFloat(); break;
                case "p": position = data.ToVector3(); break;
                case "r": rotation = data.ToQuaternion();  break;
                case "n": nearPlane = data.ToFloat(); break;
                case "f": farPlane = data.ToFloat(); break;
                case "l": localTransform = true; break;
                default: return false;
            }

            return true;
        }

        public override CfgEncoder Encode() => new CfgEncoder()
            .Add("fov", fieldOfView)
            .Add("p", position)
            .Add("r", rotation)
            .Add("n", nearPlane)
            .Add("f", farPlane)
            .Add_IfTrue("l", localTransform);
        
        #endregion

    }

    public class ProjectorCameraParameters
    {

        private readonly ShaderProperty.MatrixValue _spMatrix;
        private readonly ShaderProperty.TextureValue _spDepth;
        private readonly ShaderProperty.VectorValue _spPos;
        private readonly ShaderProperty.VectorValue _spZBuffer;
        private readonly ShaderProperty.VectorValue _camParams;

        public void Set(Camera cam, Texture tex)
        {

            if (!cam)
                return;

            var tf = cam.transform;
            var far = cam.farClipPlane;
            var near = cam.nearClipPlane;

            _spMatrix.GlobalValue = cam.projectionMatrix * cam.worldToCameraMatrix;
            _spDepth.GlobalValue = tex;
            _spPos.GlobalValue = tf.position.ToVector4(0);

            _camParams.GlobalValue = new Vector4(
                cam.aspect,
                Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * 0.5f),
                near,
                1f / far);

            var zBuff = new Vector4(1f - far / near, far / near, 0, 0);
            zBuff.z = 1 / zBuff.x;

            _spZBuffer.GlobalValue = zBuff;
        }

        public ProjectorCameraParameters(string prefix)
        {
            _spMatrix = new ShaderProperty.MatrixValue(prefix + "ProjectorMatrix");
            _spDepth = new ShaderProperty.TextureValue(prefix + "DepthProjection");
            _spPos = new ShaderProperty.VectorValue(prefix + "ProjectorPosition");
            _spZBuffer = new ShaderProperty.VectorValue(prefix + "ProjectorClipPrecompute");
            _camParams = new ShaderProperty.VectorValue(prefix + "ProjectorConfiguration");
        }
    }

}