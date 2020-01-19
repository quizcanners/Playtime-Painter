using System;
using System.Collections.Generic;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace PlaytimePainter
{
    
    [ExecuteInEditMode]
    public class DepthProjectorCamera : PainterSystemMono {

        public enum Mode { Clear, ReplacementShader }
        
        public static DepthProjectorCamera Instance {
            get  {
                if (PainterCamera.depthProjectorCamera)
                    return PainterCamera.depthProjectorCamera;

                if (!triedToFindDepthCamera)
                {
                    PainterCamera.depthProjectorCamera = FindObjectOfType<DepthProjectorCamera>();
                    triedToFindDepthCamera = true;
                }

                return PainterCamera.depthProjectorCamera;

            }
        }

        public static bool triedToFindDepthCamera;

        [SerializeField] public Camera _projectorCamera;

        [SerializeField] public Vector2 _fromMouseOffset;

        public bool _projectFromMainCamera = false;
        public bool _centerOnMousePosition = false;
        public bool pauseAutoUpdates = false;

        #region Inspector

        public bool _foldOut;
        
        private int _inspectedUser = -1;

        public override bool Inspect()
        {
            var changed = false;

            pegi.toggleDefaultInspector(this);
            
            if (icon.Delete.Click("Delete Projector Camera"))
                gameObject.DestroyWhatever();
            
            pegi.toggle(ref pauseAutoUpdates, icon.Play, icon.Pause,
                pauseAutoUpdates ? "Resume Updates" : "Pause Updates").changes(ref changed);

            if (RenderTextureBuffersManager.InspectDepthTarget().nl(ref changed))
                UpdateDepthCamera();

            if (_projectorCamera)
            {

                "Project from Camera".toggleIcon("Will always project from Play or Editor Camera", ref _projectFromMainCamera).nl(ref changed);

                if (_projectFromMainCamera)
                    "Follow the mouse".toggleIcon(ref _centerOnMousePosition).nl(ref changed);

                var fov = _projectorCamera.fieldOfView;

                if ("FOV".edit(30, ref fov, 0.1f, 180f).nl(ref changed))
                {

                    _projectorCamera.fieldOfView = fov;
                }
            }

            "Requested updates".edit_List(ref depthUsers, ref _inspectedUser).nl();

            return changed;
        }

        public bool Inspect_PainterShortcut()
        {
            var changed = false;
            
            pegi.toggle(ref _projectFromMainCamera, icon.Link, icon.UnLinked, "Link Projector Camera to {0} camera".F(Application.isPlaying ? "Main Camera" : "Editor Camera")).changes(ref changed);

            if (_projectFromMainCamera)
            {
                "Follow the mouse".toggleIcon(ref _centerOnMousePosition).changes(ref changed);

                if (_centerOnMousePosition)
                {
                    pegi.nl();
                    "Off X".edit(60, ref _fromMouseOffset.x, -1, 1).nl();
                    "Off Y".edit(60, ref _fromMouseOffset.y, -1, 1).nl();
                }

            }

            this.ClickHighlight().nl();
            
            return changed;
        }
        
        #endregion

        private void OnEnable() {

            if (!_projectorCamera) {
                _projectorCamera = GetComponent<Camera>();

                if (!_projectorCamera)
                    _projectorCamera = gameObject.AddComponent<Camera>();
            }

            RenderTextureBuffersManager.UpdateDepthTarget();
            UpdateDepthCamera();

            PainterCamera.depthProjectorCamera = this;

            _painterDepthTexture.GlobalValue = RenderTextureBuffersManager.depthTarget;

        }

        void OnDisable()
        {
            triedToFindDepthCamera = false;
        }
        
        #region Render Queue  

        private int lastUpdatedUser = 0;

        private IUseDepthProjector userToGetUpdate;

        [NonSerialized] private static List<IUseDepthProjector> depthUsers = new List<IUseDepthProjector>();
        readonly CountlessBool disabledUser = new CountlessBool();

        public static bool TrySubscribeToDepthCamera(IUseDepthProjector pj) {
            if (!depthUsers.Contains(pj)) {
                depthUsers.Add(pj);
                return true;
            }

            return false;
        }

        private Mode currentMode = Mode.Clear;
        
        #endregion

        public void RenderRightNow(IUseDepthProjector proj) {

            userToGetUpdate = proj;
            RequestRender(false);
            
        }

        private void LateUpdate()
        {
            if (Application.isPlaying)
                ManagedUpdate();
        }

        private float lastUserUpdateReturned = 0;

        private float sinceLastPainterCall = 0;

        public void ManagedUpdate() {

            if (userToGetUpdate != null && (Time.time - lastUserUpdateReturned > 1))
            {
                logger.Log_Interval(60, "Could not return to user {0}".F(userToGetUpdate));
                
                userToGetUpdate = null;
            }

            if ((sinceLastPainterCall>0.25f) && _projectorCamera && userToGetUpdate == null) {
                if (!pauseAutoUpdates)
                    TryGetNextUser();

                RequestRender(true);
            }

            sinceLastPainterCall += Time.deltaTime;

        }
        
        private void TryGetNextUser()
        {
            if (lastUpdatedUser >= depthUsers.Count)
            {
                lastUpdatedUser = 0;
                userToGetUpdate = null;

            }
            else
            {

                bool gotNull = false;

                while (lastUpdatedUser < depthUsers.Count)
                {
                    userToGetUpdate = depthUsers[lastUpdatedUser];

                    if ( QcUnity.IsNullOrDestroyed_Obj(userToGetUpdate))
                    {
                        gotNull = true;
                        userToGetUpdate = null;
                    }
                    else
                    if (userToGetUpdate.ProjectorReady())
                        break;
                    else userToGetUpdate = null;

                    lastUpdatedUser++;
                }

                lastUpdatedUser++;

                if (gotNull)
                    for (int i = depthUsers.Count - 1; i >= 0; i--)
                    {
                        if ( QcUnity.IsNullOrDestroyed_Obj(depthUsers[i]))
                            depthUsers.RemoveAt(i);
                    }
            }

        }

        private void RequestRender(bool updatePainterIfNoUser) {

            bool gotUser = false;

            _projectorCamera.ResetReplacementShader();
            _projectorCamera.clearFlags = CameraClearFlags.Depth;
            
            try
            {

                if (userToGetUpdate != null)
                {
                    var cfg = userToGetUpdate.GetProjectorCameraConfiguration();
                    var trg = userToGetUpdate.GetTargetTexture();

                    if (trg && cfg != null)
                    {
                        if (!pauseAutoUpdates)
                            painterProjectorCameraConfiguration.From(_projectorCamera);

                        cfg.To(_projectorCamera);
                        _projectorCamera.targetTexture = trg;

                        var prm = userToGetUpdate.GetGlobalCameraMatrixParameters();
                        if (prm != null)
                            prm.SetGlobalFrom(_projectorCamera);

                        gotUser = true;

                        var mode = userToGetUpdate.GetMode();

                        switch (mode)
                        {
                            case Mode.ReplacementShader:

                                var repl = userToGetUpdate as IUseReplacementCamera;

                                if (repl != null)
                                {
                                    _projectorCamera.SetReplacementShader(repl.ProjectorShaderToReplaceWith(),
                                        repl.ProjectorTagToReplace());
                                    _projectorCamera.clearFlags = CameraClearFlags.Color;
                                    _projectorCamera.backgroundColor = repl.CameraReplacementClearColor();
                                  // _projectorCamera.farClipPlane = 3000;

                                }

                                break;
                        }

                        currentMode = mode;
                    }
                }

                if (!gotUser)
                    userToGetUpdate = null;
            }
            catch (Exception ex)
            {
                logger.Log_Interval(10, ex.ToString());
                userToGetUpdate = null;
            }

            if (userToGetUpdate == null) { 

                UpdateCameraPositionForPainter();

                _projectorCamera.targetTexture = RenderTextureBuffersManager.depthTarget;
               
                _painterDepthCameraMatrix.SetGlobalFrom(_projectorCamera);
            }

            if (userToGetUpdate != null ||
                (TexMgmtData.useDepthForProjector && updatePainterIfNoUser && !pauseAutoUpdates))
                _projectorCamera.Render();
        }

        QcUtils.ChillLogger logger = new QcUtils.ChillLogger();

        void OnPostRender()
        {
           // Debug.Log(transform.position);
            ReturnResults();
        }

        void ReturnResults() {

            if (userToGetUpdate != null)
            {

                //Debug.Log("Returning user "+userToGetUpdate.ToString());

                try
                {
                    userToGetUpdate.AfterCameraRender(_projectorCamera.targetTexture);
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                }

                lastUserUpdateReturned = Time.time;

                //if (userToGetUpdate as VolumeRayTrace != null)
                //Debug.Log("Returning result");

                userToGetUpdate = null;

                if (!pauseAutoUpdates)
                    painterProjectorCameraConfiguration.To(_projectorCamera);
            }
        }

        private void UpdateDepthCamera()
        {

            if (!_projectorCamera)
                return;

            _projectorCamera.enabled = false;
            _projectorCamera.depthTextureMode = DepthTextureMode.None;
            _projectorCamera.depth = -1000;
            _projectorCamera.clearFlags = CameraClearFlags.Depth;
            _projectorCamera.allowMSAA = false;
            _projectorCamera.allowHDR = false;

            var l = Cfg ? Cfg.playtimePainterLayer : 30;

            _projectorCamera.cullingMask &= ~(1 << l);

            _painterDepthTexture.GlobalValue = RenderTextureBuffersManager.depthTarget;

            _projectorCamera.targetTexture = RenderTextureBuffersManager.depthTarget;
        }

        //private Vector2 FromMousePositionOffset => Vector2.Scale(_fromMouseOffset, new Vector2(Screen.width, Screen.height));

        void UpdateCameraPositionForPainter()
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
                                                 cam.ScreenPointToRay(Input.mousePosition 
                                                                      + Vector2.Scale(_fromMouseOffset, new Vector2(Screen.width, Screen.height)).ToVector3(0)
                                                                      ).direction);
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
                    transform.LookAt(ray.origin + ray.direction 
                                                + transform.TransformDirection(_fromMouseOffset.ToVector3(1))
                                                );

                }
            }
        }

        ProjectorCameraConfiguration painterProjectorCameraConfiguration = new ProjectorCameraConfiguration();
        private readonly CameraMatrixParameters _painterDepthCameraMatrix = new CameraMatrixParameters("pp_");
        private readonly ShaderProperty.TextureValue _painterDepthTexture = new ShaderProperty.TextureValue("pp_DepthProjection");

    }


    public interface IUseDepthProjector {
        bool ProjectorReady();
        CameraMatrixParameters GetGlobalCameraMatrixParameters();
        ProjectorCameraConfiguration GetProjectorCameraConfiguration();
        void AfterCameraRender(RenderTexture depthTexture);
        RenderTexture GetTargetTexture();
        DepthProjectorCamera.Mode GetMode();
    }

    public interface IUseReplacementCamera
    {
        string ProjectorTagToReplace();
        Shader ProjectorShaderToReplaceWith();
        Color CameraReplacementClearColor();
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

        public void From(StrokeVector vec, bool lookInNormalDirection = true) {
            position = vec.posTo;
            localTransform = false;

            if (lookInNormalDirection)
            {
                position += vec.collisionNormal;
                rotation = Quaternion.LookRotation(vec.collisionNormal, Vector3.up);
            }
        }

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

            cam.nearClipPlane = Mathf.Max(nearPlane, 0.0001f);
            cam.farClipPlane = Mathf.Max(farPlane, 0.0002f);
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
        private Camera inspectedCamera;
        public bool Inspect() {
            var changed = false;

            "Local".toggleIcon("Use local Position and rotation of the camera." ,ref localTransform).nl();
            "Position: {0}".F(position).nl();
            "Rotation: {0}".F(rotation).nl();

            "FOV".edit(40, ref fieldOfView, 60, 180).nl(ref changed);

            "Range".edit_Range(ref nearPlane, ref farPlane).nl(ref changed);
            
            "Tmp Camera".edit(ref inspectedCamera).changes(ref changed);

            if (inspectedCamera) {
                if (icon.Load.Click("Load configuration into camera", ref changed))
                    To(inspectedCamera);
                if (icon.Save.Click("Save configuration from camera", ref changed))
                    From(inspectedCamera);
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

    public class CameraMatrixParameters {

        private readonly ShaderProperty.MatrixValue _spMatrix;
        private readonly ShaderProperty.VectorValue _spPos;
        private readonly ShaderProperty.VectorValue _spZBuffer;
        private readonly ShaderProperty.VectorValue _camParams;

        public void SetGlobalFrom(Camera cam) {

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
            zBuff.w = far - near;

            _spZBuffer.GlobalValue = zBuff;
        }

        public CameraMatrixParameters(string prefix)
        {
            _spMatrix = new ShaderProperty.MatrixValue(prefix + "ProjectorMatrix");
            _spPos = new ShaderProperty.VectorValue(prefix + "ProjectorPosition");
            _spZBuffer = new ShaderProperty.VectorValue(prefix + "ProjectorClipPrecompute");
            _camParams = new ShaderProperty.VectorValue(prefix + "ProjectorConfiguration");
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(DepthProjectorCamera))]
    public class DepthProjectorCameraDrawer : PEGI_Inspector_Mono<DepthProjectorCamera> { }
#endif

}