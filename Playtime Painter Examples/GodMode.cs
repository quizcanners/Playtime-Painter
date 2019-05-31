using System;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace PlaytimePainter.Examples {
    
    [ExecuteInEditMode]
    public class GodMode : MonoBehaviour, IPEGI {

        public float speed = 20;
        public float sensitivity = 5;
        private bool _disableRotation;
        public bool rotateWithoutRmb;
        private bool simulateFlying;

        private bool Rotate() {

            #if !UNITY_IOS && !UNITY_ANDROID
            return (rotateWithoutRmb || Input.GetMouseButton(1));
            #else
            return true;
            #endif
        }

        private float _rotationY;

        public virtual void Update()
        {

            var add = Vector3.zero;

            var tf = transform;
            
            if (Input.GetKey(KeyCode.W)) add += tf.forward;
            if (Input.GetKey(KeyCode.A)) add -= tf.right;
            if (Input.GetKey(KeyCode.S)) add -= tf.forward;
            if (Input.GetKey(KeyCode.D)) add += tf.right;

            if (!simulateFlying)
                add.y = 0;

            if (Input.GetKey(KeyCode.Q)) add += Vector3.down;
            if (Input.GetKey(KeyCode.E)) add += Vector3.up;


            tf.position += add * speed * Time.deltaTime * (Input.GetKey(KeyCode.LeftShift) ? 3f: 1f);

            if (!Application.isPlaying || _disableRotation) return;
            
            if (rotateWithoutRmb || Input.GetMouseButton(1))
            {
                var eul = tf.localEulerAngles;
                
                var rotationX = eul.y;
                _rotationY = eul.x;

                rotationX += Input.GetAxis("Mouse X") * sensitivity;
                _rotationY -= Input.GetAxis("Mouse Y") * sensitivity;

                _rotationY = _rotationY < 120 ? Mathf.Min(_rotationY, 85) : Mathf.Max(_rotationY, 270);

                tf.localEulerAngles = new Vector3(_rotationY, rotationX, 0);

            }

            SpinAround();
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
        
        private void SpinAround()
        {

            var camTr = gameObject.TryGetCameraTransform(MainCam);
            
            if (Input.GetMouseButtonDown(2) && _mainCam)
            {
                var ray = MainCam.ScreenPointToRay(Input.mousePosition);
                
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                    spinCenter = hit.point;
                else return;
                
                var before = camTr.rotation;
                camTr.transform.LookAt(spinCenter);
                var rot = camTr.rotation.eulerAngles;
                camOrbit.x = rot.y;
                camOrbit.y = rot.x;
                _orbitDistance = (spinCenter - camTr.position).magnitude;

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

                camTr.position = campos;
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
        #if !NO_PEGI
        public bool Inspect()
        {

            var changed = false;

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

            "WASD - move {0} Q, E - Dwn, Up {0} Shift - faster {0} {1} {0} MMB - Orbit Collider".F(pegi.EnvironmentNl,
                _disableRotation ? "" : (rotateWithoutRmb ? "RMB - rotation" : "Mouse to rotate"));

            if (MainCam)
                "Main Camera".edit(ref _mainCam).nl(ref changed);

            return false;
        }
#endif
#endregion
    }
}
