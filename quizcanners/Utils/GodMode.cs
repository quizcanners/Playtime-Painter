using UnityEngine;
using QuizCanners.Inspect;
using QuizCanners.CfgDecode;
using QuizCanners.Lerp;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QuizCanners.Utils
{

#pragma warning disable IDE0018 // Inline variable declaration

    [ExecuteInEditMode]
    public class GodMode : MonoBehaviour, IPEGI, ICfgCustom, ILinkedLerping
    {
        public enum Mode { FPS = 0, STATIC = 1, LERP = 2 }

        public float speed = 20;
        public float offsetClip = 0;
        public float sensitivity = 5;
        public bool _disableRotation;
        public bool rotateWithoutRmb;
        public bool simulateFlying;
        public bool _onlyInEditor;

        [SerializeField] private QcUtils.DynamicRangeFloat targetHeight = new QcUtils.DynamicRangeFloat(0.001f, 10, 0.2f);

        public Mode mode;

        #region Advanced Camera

        private float CameraWindowNearClip()
        {
            float val = ((targetHeight.Value) / Mathf.Tan(Mathf.Deg2Rad * _mainCam.fieldOfView * 0.5f));

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
            get => _mainCam.farClipPlane - CameraWindowNearClip();
            set => _mainCam.farClipPlane = CameraWindowNearClip() + value;
        }

   

        private void AdjustCamera()
        {
            var camTf = _mainCam.transform;

            if (!camTf.parent || camTf.parent != transform)
                return;

            float clip = CameraWindowNearClip();
            camTf.position = transform.position - camTf.forward * clip;
            _mainCam.nearClipPlane = clip * Mathf.Clamp(1 - offsetClip, 0.01f, 0.99f);
            _mainCam.farClipPlane = clip + CameraClipDistance;
        }

        #endregion

        private void OnEnable()
        {
            if (mode == Mode.LERP)
                mode = Mode.FPS;
        }


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

        public void Decode(string tg, CfgData data)
        {
            switch (tg)
            {
                case "pos": _positionLerp.TargetValue = data.ToVector3(); break;
                case "rot": _rotationLerp.TargetValue = data.ToQuaternion(); break;
                case "h": _heightLerp.TargetValue = data.ToFloat(); break;
                case "sp": speed = data.ToFloat(); break;
            }
        }

        public void Decode(CfgData data)
        {
            IsLerpInitialized();
            new CfgDecoder(data).DecodeTagsFor(this);
            mode = Mode.LERP;
        }
        #endregion

        #region Linked Lerp

        private LinkedLerp.TransformLocalPosition _positionLerp;// = new LinkedLerp.TransformLocalPosition("Position");
        private LinkedLerp.TransformLocalRotation _rotationLerp;// = new LinkedLerp.TransformLocalRotation("Rotation");
        private LinkedLerp.FloatValue _heightLerp = new LinkedLerp.FloatValue(name: "Height");

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
            if (IsLerpInitialized() && mode != Mode.FPS)
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

        private bool lerpYourself;

        public virtual void Update()
        {

            if (!_mainCam || (_onlyInEditor && Application.isEditor == false))
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

                        Lerp(lerpData, canSkipLerp: false);

                        if (lerpData.Done)
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

        #region Inspector
        public void Inspect()
        {

            pegi.toggleDefaultInspector(this);



            switch (mode)
            {
                case Mode.FPS:
                    pegi.FullWindow.DocumentationClickOpen(() =>
                       "WASD - move {0} Q, E - Dwn, Up {0} Shift - faster {0} {1} {0} MMB - Orbit Collider".F(
                           pegi.EnvironmentNl,
                           _disableRotation ? "" : (rotateWithoutRmb ? "RMB - rotation" : "Mouse to rotate")
                       ));
                    break;


                case Mode.STATIC:

                    "Not Lerping himself".writeWarning();

                    if ("Lepr Yourself".Click().nl())
                        lerpYourself = true;

                    if ("Enable first-person controls".Click().nl())
                        mode = Mode.FPS;
                    break;
                case Mode.LERP:
                    "IS LERPING".write();
                    break;
            }

            pegi.nl();

            if (MainCam)
                "Main Camera".edit(90, ref _mainCam).nl();
            
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
                        bool changes = pegi.ChangeTrackStart();

                        float fov = _mainCam.fieldOfView;

                        if ("FOV".edit(60, ref fov, 5, 170).nl())
                            _mainCam.fieldOfView = fov;

                        float clipDistance = CameraClipDistance;

                        "Height:".write(60);
                        targetHeight.Inspect();
                        pegi.nl();

                        if ("Clip Range".editDelayed(90, ref clipDistance).nl())

                            CameraClipDistance = Mathf.Clamp(clipDistance, 0.03f, 100000);

                        "Clip Distance (Debug): {0}".F(CameraWindowNearClip()).nl();

                        "Offset Clip".edit(90, ref offsetClip, 0.01f, 0.99f).nl();



                        if (changes)
                            AdjustCamera();
                    }
                }
            }


            pegi.nl();


            switch (mode)
            {
                case Mode.FPS:

                    "Speed:".edit("Speed of movement", 50, ref speed).nl();

                    "Sensitivity:".edit("How fast camera will rotate", 50, ref sensitivity).nl();

                    "Flying".toggleIcon("Looking up/down will make camera move up/down.", ref simulateFlying).nl();

                    "Disable Rotation".toggleIcon(ref _disableRotation).nl();

                    if (!_disableRotation)
                        "Rotate without RMB".toggleIcon(ref rotateWithoutRmb).nl();

                    break;
            }

            pegi.nl();

            "Editor Only".toggleIcon(ref _onlyInEditor).nl();
        }

        #endregion
    }

#if UNITY_EDITOR
[CustomEditor(typeof(GodMode))] internal class GodModeDrawer : PEGI_Inspector { }
#endif
}
