using System;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using static QuizCannersUtilities.QcUtils;

namespace PlaytimePainter.Examples {

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration

    [ExecuteInEditMode]
    public class GodMode : MonoBehaviour, IPEGI {

        public float speed = 20;
        public float sensitivity = 5;
        public bool _disableRotation;
        public bool rotateWithoutRmb;
        public bool simulateFlying = false;


        #region Camera Smoothing

        // private float trackingInitiating = 0;
        [NonSerialized] private Vector3 cameraSmoothedVelocity;
        [NonSerialized] private Vector3 mainCameraVelocity;
        [NonSerialized] private Vector3 cameraSmoothingOffset;

        private void UpdateCameraSmoothing()
        {

            var offset = cameraSmoothedVelocity - mainCameraVelocity - cameraSmoothingOffset * 16;

            var magn = offset.magnitude;

          //  offset -= cameraSmoothingOffset * (3 + Mathf.Clamp01(magn/speed)*Vector3.Dot(offset.normalized, cameraSmoothingOffset.normalized));
          
            
            cameraSmoothedVelocity = cameraSmoothedVelocity.LerpBySpeed_DirectionFirst(mainCameraVelocity, magn * 0.8f);

            cameraSmoothingOffset = cameraSmoothingOffset.LerpBySpeed_DirectionFirst(offset, magn);




        }

        #endregion


        #region Advanced Camera
        public bool advancedCamera;

        [SerializeField] private DynamicRangeFloat targetHeight = new DynamicRangeFloat(0.1f, 10, 1);

        private float CameraWindowNearClip()
        {
            float val = (targetHeight.Value) / Mathf.Tan(Mathf.Deg2Rad*_mainCam.fieldOfView * 0.5f);
            
            return val;
        }

        public float CameraWindowHeight {

            get { return targetHeight.Value; }
            set { targetHeight.Value = Mathf.Max(0.1f,  value);
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

        void AdjustCamera() {

            if (advancedCamera)
            {
                float clipRange = CameraClipDistance;

                float clip = CameraWindowNearClip();
                _mainCam.transform.position = transform.position - _mainCam.transform.forward * clip;
                _mainCam.nearClipPlane = clip;
                _mainCam.farClipPlane = clip + CameraClipDistance;


            } else {
                _mainCam.transform.localPosition = Vector3.zero;
                _mainCam.nearClipPlane = 0.3f;
            }

            _mainCam.transform.position += cameraSmoothingOffset;

        }


        #endregion


        private float _rotationY;

        void OnEnable()
        {
            cameraSmoothedVelocity = Vector3.zero;
            mainCameraVelocity = Vector3.zero;
            cameraSmoothingOffset  = Vector3.zero;
        }

        public virtual void Update() {

            if (!_mainCam)
                return;
            
            var add = Vector3.zero;

            var opratorTf = transform;
            var camTf = _mainCam.transform;

            if (Input.GetKey(KeyCode.W)) add += camTf.forward;
            if (Input.GetKey(KeyCode.A)) add -= camTf.right;
            if (Input.GetKey(KeyCode.S)) add -= camTf.forward;
            if (Input.GetKey(KeyCode.D)) add += camTf.right;

            if (!simulateFlying)
                add.y = 0;
            
            if (Input.GetKey(KeyCode.Q)) add += Vector3.down;
            if (Input.GetKey(KeyCode.E)) add += Vector3.up;

            add.Normalize();

            mainCameraVelocity = add * speed * (Input.GetKey(KeyCode.LeftShift) ? 3f : 1f);

            opratorTf.position += mainCameraVelocity * Time.deltaTime ;

            if (!Application.isPlaying || _disableRotation) return;
            
            if (rotateWithoutRmb || Input.GetMouseButton(1)) {

                var eul = camTf.localEulerAngles;
                
                var rotationX = eul.y;
                _rotationY = eul.x;

                rotationX += Input.GetAxis("Mouse X") * sensitivity;
                _rotationY -= Input.GetAxis("Mouse Y") * sensitivity;

                _rotationY = _rotationY < 120 ? Mathf.Min(_rotationY, 85) : Mathf.Max(_rotationY, 270);

                camTf.localEulerAngles = new Vector3(_rotationY, rotationX, 0);

            }

            SpinAround();

            UpdateCameraSmoothing();

            AdjustCamera();

        }
        
        public Vector2 camOrbit;
        public Vector3 spinCenter;
        private float _orbitDistance;
        public bool orbitingFocused;
        public float spinStartTime;

        [SerializeField] private Camera _mainCam;
        
        private Camera MainCam {
            get {
                if (!_mainCam)
                    _mainCam = Camera.main;
                return _mainCam;
            }
        }
        
        private void SpinAround() {
            
            var camTr = _mainCam.transform;

            if (Input.GetMouseButtonDown(2)) {
                
                var ray = MainCam.ScreenPointToRay(Input.mousePosition);
                
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                    spinCenter = hit.point;
                else return;
                
                var before = camTr.rotation;
                camTr.LookAt(spinCenter);
                var rot = camTr.rotation.eulerAngles;
                camOrbit.x = rot.y;
                camOrbit.y = rot.x;
                _orbitDistance = (spinCenter - transform.position).magnitude;

                camTr.rotation = before;
                orbitingFocused = false;
                spinStartTime = Time.time;
            }

            if (Input.GetMouseButtonUp(2))
                _orbitDistance = 0;

            if ((!(_orbitDistance > 0)) || (!Input.GetMouseButton(2))) return;
            {
                camOrbit.x += Input.GetAxis("Mouse X") * 5;
                camOrbit.y -= Input.GetAxis("Mouse Y") * 5;

                if (camOrbit.y <= -360)
                    camOrbit.y += 360;
                if (camOrbit.y >= 360)
                    camOrbit.y -= 360;

                var rot = Quaternion.Euler(camOrbit.y, camOrbit.x, 0);
                var campos = rot *
                                 (new Vector3(0.0f, 0.0f, -_orbitDistance)) +
                                 spinCenter;

                transform.position = campos;
                if (!orbitingFocused)
                {
                    camTr.rotation = camTr.rotation.LerpBySpeed(rot, 200);
                    if (Quaternion.Angle(camTr.rotation, rot) < 1)
                        orbitingFocused = true;
                }
                else camTr.rotation = rot;
            }
        }

        #region Inspector
        public bool Inspect() {

            var changed = false;

            "WASD - move {0} Q, E - Dwn, Up {0} Shift - faster {0} {1} {0} MMB - Orbit Collider".F(pegi.EnvironmentNl,
                _disableRotation ? "" : (rotateWithoutRmb ? "RMB - rotation" : "Mouse to rotate")).fullWindowDocumentationClickOpen();
            
            pegi.nl();

            if (!_mainCam)  {
                "Main Camera".selectInScene(ref _mainCam).nl();
                "Camera is missing, spin around will not work".writeWarning();
            }
          
            "Speed:".edit("Speed of movement", ref speed).nl();

            "Sensitivity:".edit("How fast camera will rotate", ref sensitivity).nl(ref changed);

            "Flying".toggleIcon("Looking up/down will make camera move up/down.",ref simulateFlying).nl(ref changed);

            "Disable Rotation".toggleIcon( ref _disableRotation).nl(ref changed);

            if (!_disableRotation)
                "Rotate without RMB".toggleIcon(ref rotateWithoutRmb).nl(ref changed);
            
            "Smoothing offset: {0}".F(cameraSmoothingOffset).nl();

            "Smoothing velocity: {0}".F(cameraSmoothedVelocity).nl();

            if (MainCam) {

                "Main Camera".edit(ref _mainCam).nl(ref changed);

                if ("Advanced Camera".toggleIcon(ref advancedCamera).nl())
                    AdjustCamera();
                
                if (advancedCamera) {

                    if (!_mainCam.transform.IsChildOf(transform) || (_mainCam.transform == transform)) {

                        "Make main camera a child object of this script".writeWarning();

                        if (transform.childCount == 0) {

                            if ("Add Empty Child".Click().nl()) {

                                var go = new GameObject("Advanced Camera");
                                var tf = go.transform;
                                tf.SetParent(transform, false);
                            }

                        }
                        else
                            "Delete Main Camera and create one on a child".writeHint();

                    } else {

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

            return false;
        }
        
        #endregion
    }
}
