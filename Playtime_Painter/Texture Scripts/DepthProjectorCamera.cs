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

                if (!triedToFindDepthCamera)
                {
                    PainterCamera.depthProjectorCamera = FindObjectOfType<DepthProjectorCamera>();
                    triedToFindDepthCamera = false;
                }

                return PainterCamera.depthProjectorCamera;

            }
        }

        public static bool triedToFindDepthCamera;

        [SerializeField] private Camera _projectorCamera;
        [SerializeField] private RenderTexture _depthTarget;
        [SerializeField] private static RenderTexture _depthTargetForUsers;

        public static RenderTexture GetReusableDepthTarget()
        {
            if (_depthTargetForUsers)
                return _depthTargetForUsers;

            _depthTargetForUsers = GetDepthRenderTexture(1024);

            Debug.Log("Creating new depth texture");

            return _depthTargetForUsers;
        }

        private static RenderTexture GetDepthRenderTexture(int sz) => new RenderTexture(sz, sz, 32, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            autoGenerateMips = false,
            useMipMap = false
        };

        [SerializeField] private bool _projectFromMainCamera;
        [SerializeField] private bool _centerOnMousePosition;
        [SerializeField] public bool pauseUpdates;
 
        public int targetSize = 512;

        #region Inspector
        private bool _foldOut;
        private int _inspectedUser = -1;

        #if PEGI

        public override bool Inspect()
        {
            var changed = false;

            pegi.toggle(ref pauseUpdates, icon.Play, icon.Pause,
                pauseUpdates ? "Resume Updates" : "Pause Updates").changes(ref changed);

            if (!_foldOut)
                pegi.toggle(ref _projectFromMainCamera, icon.Link, icon.UnLinked, "Link Projector Camera to {0} camera".F(Application.isPlaying ? "Main Camera" : "Editor Camera")).changes(ref changed);
            
            if ("Projector ".enter(ref _foldOut).nl_ifFoldedOut()) {

                "Target Size".edit(ref targetSize).changes(ref changed);
                if (icon.Refresh.Click("Recreate Depth Texture").nl(ref changed)) {
                    _depthTarget.DestroyWhatever();
                    _depthTarget = null;
                    UpdateDepthCamera();
                }

                if (_projectorCamera) {
                   
                    "Project from Camera".toggleIcon("Will always project from Play or Editor Camera" ,ref _projectFromMainCamera).nl(ref changed);

                    if (_projectFromMainCamera) 
                        "Follow the mouse".toggleIcon(ref _centerOnMousePosition).nl(ref changed);
                    
                    var fov = _projectorCamera.fieldOfView;

                    if ("FOV".edit(30, ref fov, 0.1f, 180f).nl(ref changed)) {

                        _projectorCamera.fieldOfView = fov;
                    }

                }

                if (!PlaytimePainter.inspected)
                    "Requested updates".edit_List(ref depthUsers, ref _inspectedUser).nl();

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

            _spDepth.GlobalValue = _depthTarget;

        }

        void OnDisable()
        {
            triedToFindDepthCamera = false;
        }

        private void UpdateDepthCamera()
        {

            if (!_projectorCamera) return;

            _projectorCamera.enabled = false;
            _projectorCamera.depthTextureMode = DepthTextureMode.None;
            _projectorCamera.depth = -1000;
            _projectorCamera.clearFlags = CameraClearFlags.Depth;

            var l = Cfg ? Cfg.playtimePainterLayer : 30;

            _projectorCamera.cullingMask &= ~(1 << l);

            if (_depthTarget)
            {
                if (_depthTarget.width == targetSize)
                    return;
                else
                    _depthTarget.DestroyWhateverUnityObject();
            }

            var sz = Mathf.Max(targetSize, 16);

            _depthTarget = GetDepthRenderTexture(sz);

            _spDepth.GlobalValue = _depthTarget;

            _projectorCamera.targetTexture = _depthTarget;
        }
        
        #region Depth Requests 

        private int lastUpdatedUser = 0;

        private IUseDepthProjector userToGetUpdate;

        ProjectorCameraConfiguration painterProjectorCameraConfiguration = new ProjectorCameraConfiguration();
        
        [NonSerialized] private static List<IUseDepthProjector> depthUsers = new List<IUseDepthProjector>();
        
        public static void SubscribeToDepthCamera(IUseDepthProjector pj) {
            if (!depthUsers.Contains(pj))
                depthUsers.Add(pj);

        }

        #endregion

        public void ManagedUpdate()
        {

            if (_projectorCamera)
            {

               

                CallRender();
            }
        }

        void UpdatePainterCameraPosition()
        {
            if (_projectFromMainCamera)
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
                                transform.LookAt(transform.position +
                                                 cam.ScreenPointToRay(Input.mousePosition).direction);
                            else
                                transform.localRotation = Quaternion.identity;

                            transform.parent = null;
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

        private void CallRender() {
           
            if (!pauseUpdates)  {

                if (lastUpdatedUser >= depthUsers.Count) {
                    lastUpdatedUser = 0;
                    userToGetUpdate = null;

                }  else {

                    userToGetUpdate = depthUsers[lastUpdatedUser];
                    lastUpdatedUser++;

                   
                }
                
                bool gotUser = false;

                if (userToGetUpdate != null)
                {
                    var cfg = userToGetUpdate.GetProjectorCameraConfiguration();
                    var trg = userToGetUpdate.GetTargetTexture();
                   

                    if (trg && cfg != null)
                    {
                        painterProjectorCameraConfiguration.From(_projectorCamera);
                        cfg.To(_projectorCamera);
                        _projectorCamera.targetTexture = trg;

                        var prm = userToGetUpdate.GetProjectionParameter();
                        if (prm != null)
                            prm.Set(_projectorCamera);

                        gotUser = true;
                    }
                }
                

                if (!gotUser)
                {
                    
                    UpdatePainterCameraPosition();

                    _projectorCamera.targetTexture = _depthTarget;
                   
                    painterProjection.Set(_projectorCamera);
                    userToGetUpdate = null;
                }

                if (gotUser || TexMgmtData.useDepthForProjector)
                    _projectorCamera.Render();

            }
        }

        void OnPostRender() {
            if (userToGetUpdate !=null)  {

                try
                {
                   
                    userToGetUpdate.AfterDepthCameraRender(_projectorCamera.targetTexture);

                }
                catch (Exception ex) {
                    Debug.LogError(ex);
                }

                userToGetUpdate = null;
                painterProjectorCameraConfiguration.To(_projectorCamera);
            }
            else
            {
               
            }
        }

        private readonly ProjectorCameraParameters painterProjection = new ProjectorCameraParameters("pp_");
        private readonly ShaderProperty.TextureValue _spDepth = new ShaderProperty.TextureValue("pp_DepthProjection");

    }

    public interface IUseDepthProjector {
        bool ProjectorReady();
        ProjectorCameraParameters GetProjectionParameter();
        ProjectorCameraConfiguration GetProjectorCameraConfiguration();
        void AfterDepthCameraRender(RenderTexture depthTexture);
        RenderTexture GetTargetTexture();
    }

    [Serializable]
    public class ProjectorCameraConfiguration : AbstractCfg, IPEGI
    {
        public float fieldOfView = 90;
        public Vector3 position;
        public Quaternion rotation;
        public float nearPlane = 0.1f;
        public float farPlane = 100;
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

        public void CopyTransform(Transform tf)
        {
            position = tf.position;
            rotation = tf.rotation;
        }

        public void DrawFrustrum(Matrix4x4 gizmoMatrix)
        {
            //Gizmos.matrix = gizmoMatrix;

            Gizmos.DrawFrustum(Vector3.zero, fieldOfView, farPlane, nearPlane, 1);
             
        }

        #region Inspector

        [SerializeField] private Camera camera;
        public bool Inspect() {
            var changed = false;

            "Local".toggleIcon("Use local Position and rotation of the camera." ,ref localTransform).nl();
            "Position: {0}".F(position).nl();
            "Rotation: {0}".F(rotation).nl();

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

    public class ProjectorCameraParameters {

        private readonly ShaderProperty.MatrixValue _spMatrix;
        private readonly ShaderProperty.VectorValue _spPos;
        private readonly ShaderProperty.VectorValue _spZBuffer;
        private readonly ShaderProperty.VectorValue _camParams;

        public void Set(Camera cam) {

            if (!cam)
                return;

            var tf = cam.transform;
            var far = cam.farClipPlane;
            var near = cam.nearClipPlane;

            _spMatrix.GlobalValue = cam.projectionMatrix * cam.worldToCameraMatrix;

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
            _spPos = new ShaderProperty.VectorValue(prefix + "ProjectorPosition");
            _spZBuffer = new ShaderProperty.VectorValue(prefix + "ProjectorClipPrecompute");
            _camParams = new ShaderProperty.VectorValue(prefix + "ProjectorConfiguration");
        }
    }

}