using System;
using System.Collections.Generic;
using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;
using QuizCanners.Migration;

namespace PainterTool
{

#pragma warning disable IDE0019 // Use pattern matching
    [ExecuteInEditMode]
    [AddComponentMenu("Playtime Painter/Depth Projector Camera")]
    public class Singleton_DepthProjectorCamera : PainterSystemMono {

        public enum Mode { Clear, ReplacementShader }
        
        public Camera _projectorCamera;

        public Vector2 _fromMouseOffset;

        public bool _projectFromMainCamera;
        public bool _centerOnMousePosition;
        public bool pauseAutoUpdates;

        #region Inspector

        public bool _foldOut;
        
        private int _inspectedUser = -1;

       public override void Inspect()
        {
            if (Icon.Delete.Click("Delete Projector Camera"))
                gameObject.DestroyWhatever();
            
            pegi.Toggle(ref pauseAutoUpdates, Icon.Play, Icon.Pause,
                pauseAutoUpdates ? "Resume Updates" : "Pause Updates");

            if (pegi.Nested_Inspect(RenderTextureBuffersManager.InspectDepthTarget).Nl())
                UpdateDepthCamera();

            if (_projectorCamera)
            {

                "Project from Camera".PegiLabel("Will always project from Play or Editor Camera").ToggleIcon(ref _projectFromMainCamera).Nl();

                if (_projectFromMainCamera)
                    "Follow the mouse".PegiLabel().ToggleIcon(ref _centerOnMousePosition).Nl();

                var fov = _projectorCamera.fieldOfView;

                if ("FOV".PegiLabel(30).Edit(ref fov, 0.1f, 180f).Nl())
                {

                    _projectorCamera.fieldOfView = fov;
                }
            }

            "Requested updates".PegiLabel().Edit_List(depthUsers, ref _inspectedUser).Nl();

        }

        public void Inspect_PainterShortcut()
        {
            
            pegi.Toggle(ref _projectFromMainCamera, Icon.Link, Icon.UnLinked, "Link Projector Camera to {0} camera".F(Application.isPlaying ? "Main Camera" : "Editor Camera"));

            if (_projectFromMainCamera)
            {
                "Follow the mouse".PegiLabel().ToggleIcon(ref _centerOnMousePosition);

                if (_centerOnMousePosition)
                {
                    pegi.Nl();
                    "Off X".PegiLabel(60).Edit(ref _fromMouseOffset.x, -1, 1).Nl();
                    "Off Y".PegiLabel(60).Edit(ref _fromMouseOffset.y, -1, 1).Nl();
                }

            }

            pegi.ClickHighlight(this).Nl();
        }
        
        #endregion

        protected override void OnAfterEnable() 
        {
            if (!_projectorCamera) {
                _projectorCamera = GetComponent<Camera>();

                if (!_projectorCamera)
                    _projectorCamera = gameObject.AddComponent<Camera>();
            }

            RenderTextureBuffersManager.UpdateDepthTarget();
            UpdateDepthCamera();

            _painterDepthTexture.GlobalValue = RenderTextureBuffersManager.depthTarget;

        }

        #region Render Queue  

        private int lastUpdatedUser;

        private IUseDepthProjector userToGetUpdate;

        [NonSerialized] private static readonly List<IUseDepthProjector> depthUsers = new();

        public static bool TrySubscribeToDepthCamera(IUseDepthProjector pj) 
        {
            if (!depthUsers.Contains(pj)) {
                depthUsers.Add(pj);
                return true;
            }

            return false;
        }

        #endregion

        public void RenderRightNow(IUseDepthProjector proj) 
        {
            userToGetUpdate = proj;
            RequestRender(updatePainterIfNoUser: false);   
        }

        private void LateUpdate()
        {
            if (Application.isPlaying)
                ManagedUpdate();
        }


        private readonly Gate.UnityTimeSinceStartup userUpdateGate = new(Gate.InitialValue.StartArmed);
        private readonly Gate.UnityTimeSinceStartup updateGate = new(Gate.InitialValue.StartArmed);

        public void ManagedUpdate() {

            if (userToGetUpdate != null && userUpdateGate.TryUpdateIfTimePassed(1f))
            {
                logger.Log( "Could not return to user {0}".F(userToGetUpdate));
                
                userToGetUpdate = null;
            }

            if (_projectorCamera && userToGetUpdate == null && updateGate.TryUpdateIfTimePassed(0.25f)) 
            {
                if (!pauseAutoUpdates)
                    TryGetNextUser();

                if (userToGetUpdate != null)
                    RequestRender(updatePainterIfNoUser: true);
            }

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
                    lastUpdatedUser++;

                    if ( QcUnity.IsNullOrDestroyed_Obj(userToGetUpdate))
                    {
                        gotNull = true;
                        userToGetUpdate = null;
                    }
                    else
                    if (userToGetUpdate.ProjectorReady())
                        break;
                    else userToGetUpdate = null;

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
                        prm?.SetGlobalFrom(_projectorCamera);

                        gotUser = true;

                        var mode = userToGetUpdate.GetMode();

                        switch (mode)
                        {
                            case Mode.ReplacementShader:

                                var repl = userToGetUpdate as IUseReplacementCamera;

                                if (repl != null)
                                {
                                    _projectorCamera.SetReplacementShader(repl.ProjectorShaderToReplaceWith(), repl.ProjectorTagToReplace());
                                    _projectorCamera.clearFlags = CameraClearFlags.Color;
                                    _projectorCamera.backgroundColor = repl.CameraReplacementClearColor();
                                }

                                break;
                        }
                    }
                }

                if (!gotUser)
                    userToGetUpdate = null;
            }
            catch (Exception ex)
            {
                logger.Log(ex, 10);
                userToGetUpdate = null;
            }

            if (userToGetUpdate == null) 
            { 
                UpdateCameraPositionForPainter();
                _projectorCamera.targetTexture = RenderTextureBuffersManager.depthTarget;
                _painterDepthCameraMatrix.SetGlobalFrom(_projectorCamera);
            }

            if (userToGetUpdate != null ||
                (Painter.Data.useDepthForProjector && updatePainterIfNoUser && !pauseAutoUpdates))
                _projectorCamera.Render();
        }

        private readonly QcLog.ChillLogger logger = new();

        private void OnPostRender()
        {
            ReturnResults();
        }

        private void ReturnResults() {

            if (userToGetUpdate != null)
            {
                try
                {
                    if (!QcUnity.IsNullOrDestroyed_Obj(userToGetUpdate))
                    {
                        userToGetUpdate.AfterCameraRender(_projectorCamera.targetTexture);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

                userUpdateGate.Update();

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

            var l = Painter.Data ? Painter.Data.playtimePainterLayer : 30;

            _projectorCamera.cullingMask &= ~(1 << l);

            _painterDepthTexture.GlobalValue = RenderTextureBuffersManager.depthTarget;

            _projectorCamera.targetTexture = RenderTextureBuffersManager.depthTarget;
        }

        private void UpdateCameraPositionForPainter()
        {
            if (_projectFromMainCamera)
            {
                if (Application.isPlaying)
                {
                    if (Painter.Camera)
                    {
                        var cam = Painter.Camera.MainCamera;

                        if (cam)
                        {
                            var tf = transform;
                            tf.parent = cam.transform;
                            tf.localScale = Vector3.one;
                            tf.localPosition = Vector3.zero;

                            if (_centerOnMousePosition)
                                transform.LookAt(transform.position +
                                                 cam.ScreenPointToRay(Input.mousePosition 
                                                                      + Vector2.Scale(_fromMouseOffset, new Vector2(Screen.width, Screen.height)).ToVector3()
                                                                      ).direction);
                            else
                                transform.localRotation = Quaternion.identity;

                            transform.parent = null;
                        }
                    }
                }
                else
                {
                    var rf = transform;
                    rf.parent = null;
                    var ray = _centerOnMousePosition
                        ? PlaytimePainter_EditorInputManager.mouseRaySceneView 
                        : PlaytimePainter_EditorInputManager.centerRaySceneView;

                    rf.position = ray.origin;
                    transform.LookAt(ray.origin + ray.direction 
                                                + (_centerOnMousePosition ? transform.TransformDirection(_fromMouseOffset.ToVector3(1)) : Vector3.zero)
                                                );

                }
            }
        }

        private readonly ProjectorCameraConfiguration painterProjectorCameraConfiguration = new();
        private readonly CameraMatrixParameters _painterDepthCameraMatrix = new("pp_");
        private readonly ShaderProperty.TextureValue _painterDepthTexture = new("pp_DepthProjection");

    }


    public interface IUseDepthProjector {
        bool ProjectorReady();
        CameraMatrixParameters GetGlobalCameraMatrixParameters();
        ProjectorCameraConfiguration GetProjectorCameraConfiguration();
        void AfterCameraRender(RenderTexture depthTexture);
        RenderTexture GetTargetTexture();
        Singleton_DepthProjectorCamera.Mode GetMode();
    }

    public interface IUseReplacementCamera
    {
        string ProjectorTagToReplace();
        Shader ProjectorShaderToReplaceWith();
        Color CameraReplacementClearColor();
    }

    [Serializable]
    public class ProjectorCameraConfiguration : ICfg, IPEGI
    {
        public float fieldOfView = 90;
        public Vector3 position;
        public Quaternion rotation;
        public float nearPlane = 0.1f;
        public float farPlane = 100;
        private bool localTransform;

        public void From(Stroke vec, bool lookInNormalDirection = true) {
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

       /* public void DrawFrustrum(Matrix4x4 gizmoMatrix)
        {
            //Gizmos.matrix = gizmoMatrix;

            Gizmos.DrawFrustum(Vector3.zero, fieldOfView, farPlane, nearPlane, 1);
             
        }*/

        #region Inspector
        private Camera inspectedCamera;
        void IPEGI.Inspect() {

            "Local".PegiLabel("Use local Position and rotation of the camera.").ToggleIcon(ref localTransform).Nl();
            "Position: {0}".F(position).PegiLabel().Nl();
            "Rotation: {0}".F(rotation).PegiLabel().Nl();

            "FOV".PegiLabel(40).Edit(ref fieldOfView, 60, 180).Nl();

            "Range".PegiLabel().Edit_Range(ref nearPlane, ref farPlane).Nl();
            
            "Tmp Camera".PegiLabel().Edit(ref inspectedCamera);

            if (inspectedCamera) {
                if (Icon.Load.Click("Load configuration into camera"))
                    To(inspectedCamera);
                if (Icon.Save.Click("Save configuration from camera"))
                    From(inspectedCamera);
            }

            pegi.Nl();
        }
        
        #endregion

        #region Encode & Decode

        public void DecodeTag(string key, CfgData data) {
            switch (key) {
                case "fov": fieldOfView = data.ToFloat(); break;
                case "p": position = data.ToVector3(); break;
                case "r": rotation = data.ToQuaternion();  break;
                case "n": nearPlane = data.ToFloat(); break;
                case "f": farPlane = data.ToFloat(); break;
                case "l": localTransform = true; break;
            }
        }

        public CfgEncoder Encode() => new CfgEncoder()
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

            _spPos.GlobalValue = tf.position.ToVector4();

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

    [PEGI_Inspector_Override(typeof(Singleton_DepthProjectorCamera))] internal class DepthProjectorCameraDrawer : PEGI_Inspector_Override { }


}