using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TextureEditor;
using PlayerAndEditorGUI;

namespace StoryTriggerData {


#if UNITY_EDITOR
        using UnityEditor;

    [CustomEditor(typeof(Actor))]
    public class ActorDrawer : Editor {
            public override void OnInspectorGUI() {
                ef.start(serializedObject);
                ((Actor)target).PEGI();
                pegi.newLine();
            }
        }
#endif


    [TagName(tagName)]
    [ExecuteInEditMode]
    public class Actor : STD_Object {

        public const string tagName = "actor";

        public bool controlledByPlayer;
        public PathBox managedBy;
        public float unmanagedTime;
        public Vector3 insideBoxLocalPosition;

        public Rigidbody[] allRigidbodies;
        public Collider[] allColliders;
        MeshRenderer[] allRenderers;
        MovementTracking[] rigiMovement = new MovementTracking[0];

        public Collider[] pushingColliders;

        public Animator animator;
        public CharacterFeetScript charFeetsScript;
        public CharacterGUNscript charGunScript;
        Vector3 lastPosition;
        public Vector3 velocity = Vector3.zero;
        public float speed = 0;
        public float walkingSpeed = 20;
        public float runningSpeed = 60;
        public float acceleration = 300;
        float LookDistance = 0.5f;
        public Vector2 looking;
        Quaternion torsoDirection;// = new Quaternion();
        Quaternion assDirection;// = new Quaternion();
        static Vector3[] renderersLocalPos;
        static Quaternion[] renderersLocalRot;

        public bool runing;
        public bool shooting;
        public bool dead;
        public float drawToBGdelay;

        public override void Reboot() {
            SetMovementTracking();
            torsoDirection = transform.rotation;
            assDirection = transform.rotation;
            lastPosition = transform.position;

            if ((allRenderers == null) || (allRenderers.Length == 0))
                allRenderers = GetComponentsInChildren<MeshRenderer>(true);

            RecordInitialTransformIfNull();

            controlledByPlayer = false;

            dead = false;

            RigidSet(false);

        }

        public void RigidSet(bool isRigid) {
            dead = isRigid;
            if (isRigid) 
                drawToBGdelay = 2;
            
            if (!isRigid) {
                ResetInitialTransform();

            }

            for (int i = 0; i < pushingColliders.Length; i++) {
                pushingColliders[i].enabled = (!isRigid);
            }

            for (int i = 0; i < allRigidbodies.Length; i++) {
                Rigidbody r = allRigidbodies[i];
                Collider c = allColliders[i];
                if (c != null)
                    allColliders[i].enabled = isRigid;

                r.isKinematic = !isRigid;

                if (isRigid)
                    r.velocity = rigiMovement[i].velocity;//Vector3.zero;
            }
        }

        void SetMovementTracking() {
            int cnt = allRigidbodies.Length;
            rigiMovement = new MovementTracking[cnt];
            allColliders = new Collider[cnt];
            for (int i = 0; i < cnt; i++) {
                allColliders[i] = allRigidbodies[i].gameObject.GetComponent<Collider>();
                rigiMovement[i] = new MovementTracking();
            }
        }

        void UpdateMovementTracking() {
            int cnt = allRigidbodies.Length;
            for (int i = 0; i > cnt; i++)
                rigiMovement[i].Record(allRigidbodies[i].transform);
        }

        public override void Decode(string tag, string data) {

            switch (tag) {
                case "p": controlledByPlayer = true; break;
                case "n": gameObject.name = data; break;
                default: Unrecognized(tag, data); break;
            }

        }

        public override stdEncoder Encode() {

            stdEncoder cody = new stdEncoder();

            if (controlledByPlayer) {
                cody.AddText("p", "");
            }

            cody.AddText("n", gameObject.name);
            
            SaveUnrecognized(cody);
            
            return cody;
        }

        public void RecordInitialTransformIfNull() {
            int cnt = allRigidbodies.Length;
            if ((renderersLocalPos == null) || (renderersLocalPos.Length != cnt)) {
                renderersLocalPos = new Vector3[cnt];
                renderersLocalRot = new Quaternion[cnt];
                for (int i = 0; i < cnt; i++) {
                    renderersLocalPos[i] = allRigidbodies[i].transform.localPosition;
                    renderersLocalRot[i] = allRigidbodies[i].transform.localRotation;
                }
            }
        }

        public void ResetInitialTransform() {
            int cnt = allRigidbodies.Length;
            for (int i = 0; i < cnt; i++) {
                allRigidbodies[i].transform.localPosition = renderersLocalPos[i];
                allRigidbodies[i].transform.localRotation = renderersLocalRot[i];
            }
        }

        public Vector3 mousePos;
        public Vector3 pointerPosition;

        float shootDelay = 0.2f;
        float shootingTime = 0;

        void Update() {

            //Debug.Log("Updates");

            if (controlledByPlayer) {

                looking = mousePos;

                Vector3 move = Vector3.zero;
                if (Input.GetKey(KeyCode.W))
                    move += Vector3.forward;

                if (Input.GetKey(KeyCode.A))
                    move += Vector3.left;

                if (Input.GetKey(KeyCode.S))
                    move += Vector3.back;

                if (Input.GetKey(KeyCode.D))
                    move += Vector3.right;

                runing = (Input.GetMouseButton(1) == false);
                shooting = Input.GetMouseButton(0);

                if (shooting) {
                    shootingTime += Time.deltaTime;

                    if (shootingTime > 0) {
                        shootingTime -= shootDelay;
                        Vector3 target = pointerPosition - transform.position;
                        target.y -= 1;
                        // Code to instantiate bollets
                    }
                }

                velocity += move * Time.deltaTime * acceleration;

                Debug.Log("Moving");

                float damper = Mathf.Max(0, (1 - Time.deltaTime * 2));

                if (move.x == 0) velocity.x *= damper;
                if (move.z == 0) velocity.z *= damper;
            }

            MovementUpdate();

        }

        public virtual void MovementUpdate() {
            if (dead) {
                drawToBGdelay -= Time.deltaTime;
                if (drawToBGdelay < 0)
                    base.Deactivate();

            } else {

                bool pointingGun = shooting || (!runing);
                float speedLimit = runing ? runningSpeed : walkingSpeed;
                float magnitude = velocity.magnitude;
                if (magnitude > speedLimit)
                    velocity *= (speedLimit / magnitude);

                bool gotFinalizingSteps = (charFeetsScript == null ? true : charFeetsScript.feettracking.finalizingSteps > 0);

                transform.position += velocity * Time.deltaTime;

                Vector3 delta = transform.position - lastPosition;

                Quaternion needToFace = (pointingGun) ? Quaternion.LookRotation(new Vector3(looking.x, 0, looking.y)) :

                     (magnitude > walkingSpeed - 1) ? Quaternion.LookRotation(velocity) : torsoDirection;


                float angle = Quaternion.Angle(needToFace, torsoDirection);

                if ((velocity.magnitude > 1) || (pointingGun && (angle > 45 * (1.2 - (pointingGun ? LookDistance : 0)))) || (gotFinalizingSteps))
                    torsoDirection = Quaternion.Lerp(torsoDirection, needToFace, Mathf.Clamp01(Time.deltaTime * (1 + Mathf.Clamp(angle - 45, 0, 10))));

                if ((charFeetsScript != null) && ((Quaternion.Angle(torsoDirection, assDirection) > 70) || (magnitude > walkingSpeed - 1)))
                    charFeetsScript.feettracking.finalizingSteps = 2;

                if (gotFinalizingSteps)
                    assDirection = Quaternion.Lerp(assDirection, torsoDirection, Time.deltaTime * 5);

                transform.rotation = torsoDirection;
                if (charFeetsScript != null)
                    charFeetsScript.feettracking.ass.transform.rotation = assDirection;

                speed = (speed * 4 + (delta.magnitude) / Time.deltaTime) / 5;

                lastPosition = transform.position;

                if (charGunScript != null)
                    charGunScript.DistantUpdate();
                if (charFeetsScript != null)
                    charFeetsScript.DistantUpdate();

                UpdateMovementTracking();

            }
        }





        public override bool PEGI() {
            bool changed =  base.PEGI();

            if ((stdValues == null) || (!stdValues.browsing_interactions)) {
                "Controlled by player: ".toggle(ref controlledByPlayer).nl();

                pegi.ClickToEditScript();

            }

            return changed;
        }




        public override string getDefaultTagName() {
            return tagName;
        }

        public static STD_Pool pool;
        public override void SetStaticPoolController(STD_Pool inst) {
            pool = inst;
        }

        public class MovementTracking {
            Vector3 prePos = new Vector3();
            public Vector3 velocity = new Vector3();

            public void Record(Transform tr) {
                Vector3 delta = (tr.position - prePos) / Time.deltaTime;
                velocity = (velocity * 4 + delta) / 5;
                prePos = tr.position;
            }

            public void Set(Transform tr) {
                prePos = tr.position;
                velocity = Vector3.zero;
            }

        }

    }
}
