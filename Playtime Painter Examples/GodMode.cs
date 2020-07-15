using System;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using Unity.Collections;
using UnityEngine;

namespace PlaytimePainter.Examples
{

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration

    [ExecuteInEditMode]
    public class GodMode : MonoBehaviour, IPEGI, ICfg, ILinkedLerping
    {

        public enum Mode { FPS = 0, STATIC = 1, LERP = 2 }

        public float speed = 20;
        public float sensitivity = 5;
        public bool _disableRotation;
        public bool rotateWithoutRmb;
        public bool simulateFlying;

        [SerializeField] private QcUtils.DynamicRangeFloat targetHeight = new QcUtils.DynamicRangeFloat(0.1f, 10, 1);

        public Mode mode;


        #region Encode & Decode

        public CfgEncoder Encode()
        {
            var cody = new CfgEncoder()
                .Add("pos", transform.localPosition)
                .Add("h", _heightLerp.CurrentValue)
                .Add("sp", speed);

            if (_mainCam)
                cody.Add("rot", _mainCam.transform.localRotation);
            
            return cody;
        }

        public bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "pos": _positionLerp.TargetValue = data.ToVector3(); break;
                case "rot": _rotationLerp.TargetValue = data.ToQuaternion(); break;
                case "h": _heightLerp.TargetValue = data.ToFloat(); break;
                case "sp": speed = data.ToFloat(); break;
                default: return false;
            }

            return true;
        }

        public void Decode(string data)
        {
            IsLerpInitialized();
            new CfgDecoder(data).DecodeTagsFor(this);
            mode = Mode.LERP;
        }
        #endregion
        
      /*  #region Camera Smoothing
        
        [NonSerialized] private Vector3 cameraSmoothedVelocity;
        [NonSerialized] private Vector3 mainCameraVelocity;
        [NonSerialized] private Vector3 cameraSmoothingOffset;

        private void UpdateCameraSmoothing()
        {
            var offset = cameraSmoothedVelocity - mainCameraVelocity - cameraSmoothingOffset * 16;

            var magn = offset.magnitude;

            cameraSmoothedVelocity = cameraSmoothedVelocity.LerpBySpeed_DirectionFirst(mainCameraVelocity, magn * 0.8f);

            cameraSmoothingOffset = cameraSmoothingOffset.LerpBySpeed_DirectionFirst(offset, magn);
        }

        #endregion*/

        #region Advanced Camera

        private float CameraWindowNearClip()
        {
            float val = (targetHeight.Value) / Mathf.Tan(Mathf.Deg2Rad * _mainCam.fieldOfView * 0.5f);

            return val;
        }

        public float CameraWindowHeight
        {

            get { return targetHeight.Value; }
            set
            {
                targetHeight.Value = Mathf.Max(0.1f, value);
                AdjustCamera();
            }

        }

        private float CameraClipDistance
        {
            get
            {
                return _mainCam.farClipPlane - _mainCam.nearClipPlane;

            }
            set { _mainCam.farClipPlane = _mainCam.nearClipPlane + value; }
        }

        void AdjustCamera()
        {

           // if (advancedCamera)
          //  {
                float clipRange = CameraClipDistance;

                float clip = CameraWindowNearClip();
                _mainCam.transform.position = transform.position - _mainCam.transform.forward * clip;
                _mainCam.nearClipPlane = clip;
                _mainCam.farClipPlane = clip + CameraClipDistance;


           /* }
            else
            {
                _mainCam.transform.localPosition = Vector3.zero;
                _mainCam.nearClipPlane = 0.3f;
            }*/

           // _mainCam.transform.position += cameraSmoothingOffset;

        }

        #endregion
        
        void OnEnable()
        {
            /*cameraSmoothedVelocity = Vector3.zero;
            mainCameraVelocity = Vector3.zero;
            cameraSmoothingOffset = Vector3.zero;*/

            if (mode == Mode.LERP)
                mode = Mode.FPS;
        }

        #region Linked Lerp

        LinkedLerp.TransformLocalPosition _positionLerp;// = new LinkedLerp.TransformLocalPosition("Position");
        LinkedLerp.TransformLocalRotation _rotationLerp;// = new LinkedLerp.TransformLocalRotation("Rotation");
        LinkedLerp.FloatValue _heightLerp = new LinkedLerp.FloatValue(name: "Height");
        
        private bool IsLerpInitialized()
        {
            if (_positionLerp != null)
                return true;

            if (_positionLerp == null && _mainCam)
            {
                _positionLerp = new LinkedLerp.TransformLocalPosition(transform, 1000);
                _rotationLerp = new LinkedLerp.TransformLocalRotation(_mainCam.transform, 180);
                return true;
            }

            return false;
        }

        private void OnFpsUpdate()
        {

            var operatorTf = transform;
            var camTf = _mainCam.transform;

            var add = Vector3.zero;

            if (Input.GetKey(KeyCode.W)) add += camTf.forward;
            if (Input.GetKey(KeyCode.A)) add -= camTf.right;
            if (Input.GetKey(KeyCode.S)) add -= camTf.forward;
            if (Input.GetKey(KeyCode.D)) add += camTf.right;

            if (!simulateFlying)
                add.y = 0;

            if (Input.GetKey(KeyCode.Q)) add += Vector3.down;
            if (Input.GetKey(KeyCode.E)) add += Vector3.up;

            add.Normalize();

            var mainCameraVelocity = add * speed * (Input.GetKey(KeyCode.LeftShift) ? 3f : 1f);

            operatorTf.localPosition += mainCameraVelocity * Time.deltaTime;

            operatorTf.localRotation = operatorTf.localRotation.LerpBySpeed(Quaternion.identity, 160);

            if (!Application.isPlaying || _disableRotation) return;

            if (rotateWithoutRmb || Input.GetMouseButton(1))
            {
                var eul = camTf.localEulerAngles;

                var rotationX = eul.y;
                float _rotationY = eul.x;

                rotationX += Input.GetAxis("Mouse X") * sensitivity;
                _rotationY -= Input.GetAxis("Mouse Y") * sensitivity;

                _rotationY = _rotationY < 120 ? Mathf.Min(_rotationY, 85) : Mathf.Max(_rotationY, 270);

                camTf.localEulerAngles = new Vector3(_rotationY, rotationX, 0);
            }

            SpinAround();
        }
        
        public void Portion(LerpData ld)
        {
            if (IsLerpInitialized() && mode!= Mode.FPS)
            {
                _positionLerp.Portion(ld);
                _rotationLerp.Portion(ld);
                _heightLerp.Portion(ld);
            } 
        }

        public void Lerp(LerpData ld, bool canSkipLerp)
        {
            if (mode != Mode.FPS)
            {
                _positionLerp.Lerp(ld, canSkipLerp);
                _rotationLerp.Lerp(ld, canSkipLerp);
                _heightLerp.Lerp(ld, canSkipLerp);
            }
        }

        private LerpData lerpData = new LerpData();

        private bool lerpYourself = false;

        public virtual void Update()
        {

            if (!_mainCam)
                return;
            
            switch (mode)
            {
                case Mode.FPS:
                    OnFpsUpdate();
                    break;
                case Mode.LERP:
                    if (lerpYourself)
                    {
                        lerpData.Reset();

                        Portion(lerpData);

                        Lerp(lerpData, false);

                        if (lerpData.MinPortion == 1)
                        {
                            mode = Mode.FPS;
                            lerpYourself = false;
                        }
                    }

                    break;
            }

            //UpdateCameraSmoothing();
            AdjustCamera();

        }
        #endregion

        public Vector2 camOrbit;
        public Vector3 spinCenter;
        private float _orbitDistance;
        public bool orbitingFocused;
        public float spinStartTime;

        [SerializeField] private Camera _mainCam;

        public Camera MainCam
        {
            get
            {
                if (!_mainCam)
                    _mainCam = Camera.main;
                return _mainCam;
            }
        }

        private void SpinAround()
        {

            var camTr = _mainCam.transform;

            if (Input.GetMouseButtonDown(2))
            {

                var ray = MainCam.ScreenPointToRay(Input.mousePosition);

                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                    spinCenter = hit.point;
                else return;

                var before = camTr.localRotation;
                camTr.LookAt(spinCenter);
                var rot = camTr.localRotation.eulerAngles;
                camOrbit.x = rot.y;
                camOrbit.y = rot.x;
                _orbitDistance = (spinCenter - transform.position).magnitude;

                camTr.rotation = before;
                orbitingFocused = false;
                spinStartTime = Time.time;
            }

            if (Input.GetMouseButtonUp(2))
                _orbitDistance = 0;

            if ((!(_orbitDistance > 0)) || (!Input.GetMouseButton(2)))
                return;
            
            camOrbit.x += Input.GetAxis("Mouse X") * 5;
            camOrbit.y -= Input.GetAxis("Mouse Y") * 5;

            if (camOrbit.y <= -360)
                camOrbit.y += 360;
            if (camOrbit.y >= 360)
                camOrbit.y -= 360;

            var rot2 = Quaternion.Euler(camOrbit.y, camOrbit.x, 0);
            var campos = rot2 *
                             (new Vector3(0.0f, 0.0f, -_orbitDistance)) +
                             spinCenter;

            transform.position = campos;
            if (!orbitingFocused)
            {
                camTr.localRotation = camTr.localRotation.LerpBySpeed(rot2, 200);
                if (Quaternion.Angle(camTr.localRotation, rot2) < 1)
                    orbitingFocused = true;
            }
            else camTr.localRotation = rot2;
            
        }

        #region Inspector
        public bool Inspect()
        {

            var changed = false;

            pegi.toggleDefaultInspector(this);

            switch (mode)
            {
                case Mode.STATIC:

                    "Not Lerping himself".writeWarning();
                    pegi.nl();
                    if ("Lepr Yourself".Click().nl())
                        lerpYourself = true;

                    if ("Enable first-person controls".Click().nl())
                        mode = Mode.FPS;
                break;
                case Mode.LERP:
                    "IS LERPING".nl();
                    break;
            }

            if (MainCam)
                "Main Camera".edit(ref _mainCam).nl(ref changed);
            
            if (!_mainCam)
            {
                "Main Camera".selectInScene(ref _mainCam).nl();
                "Camera is missing, spin around will not work".writeWarning();
            }

            pegi.nl();

            if (MainCam)
            {

                if (!_mainCam.transform.IsChildOf(transform) || (_mainCam.transform == transform))
                {

                    "Make main camera a child object of this script".writeWarning();

                    if (transform.childCount == 0)
                    {

                        if ("Add Empty Child".Click().nl())
                        {

                            var go = new GameObject("Advanced Camera");
                            var tf = go.transform;
                            tf.SetParent(transform, false);
                        }

                    }
                    else
                        "Delete Main Camera and create one on a child".writeHint();

                }
                else
                {

                    if (mode != Mode.LERP)
                    {
                        bool cameraDirty = false;

                        float fov = _mainCam.fieldOfView;

                        "FOV".edit(ref fov, 5, 170).nl(ref cameraDirty);
                        _mainCam.fieldOfView = fov;

                        float clipDistance = CameraClipDistance;

                        if ("Clip Range".editDelayed(ref clipDistance).nl())

                            CameraClipDistance = Mathf.Clamp(clipDistance, 0.03f, 100000);

                        "Height:".write(60);
                        targetHeight.Inspect().nl(ref cameraDirty);

                        "Clip Distance: {0}".F(CameraWindowNearClip()).nl();

                        if (cameraDirty)
                            AdjustCamera();
                    }
                }
            }


            pegi.nl();


            switch (mode)
            {
                case Mode.FPS:

                    pegi.FullWindowService.DocumentationClickOpen(() =>
                        "WASD - move {0} Q, E - Dwn, Up {0} Shift - faster {0} {1} {0} MMB - Orbit Collider".F(
                            pegi.EnvironmentNl,
                            _disableRotation ? "" : (rotateWithoutRmb ? "RMB - rotation" : "Mouse to rotate")
                        ));

                    "Speed:".edit("Speed of movement", ref speed).nl();

                    "Sensitivity:".edit("How fast camera will rotate", ref sensitivity).nl(ref changed);

                    "Flying".toggleIcon("Looking up/down will make camera move up/down.", ref simulateFlying).nl(ref changed);

                    "Disable Rotation".toggleIcon(ref _disableRotation).nl(ref changed);

                    if (!_disableRotation)
                        "Rotate without RMB".toggleIcon(ref rotateWithoutRmb).nl(ref changed);

                    break;
            }

            pegi.nl();
            

          /*  "Smoothing offset: {0}".F(cameraSmoothingOffset).nl();

            "Smoothing velocity: {0}".F(cameraSmoothedVelocity).nl();*/

            return false;
        }
        
        #endregion
    }
}
