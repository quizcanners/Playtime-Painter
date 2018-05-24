using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Playtime_Painter;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace StoryTriggerData {


#if PEGI && UNITY_EDITOR
        using UnityEditor;

    [CustomEditor(typeof(Actor))]
    public class ActorDrawer : Editor {
            public override void OnInspectorGUI() {
                ef.start(serializedObject);
                ((Actor)target).PEGI();
            ef.end();
        }
        }
#endif


    [TagName(tagName)]
    [ExecuteInEditMode]
    public class Actor : STD_Poolable {

        public static Actor controlled;

        public const string tagName = "actor";

        public bool controlledByPlayer;
        public PathBox managedBy;
        public float unmanagedTime;

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
        public float acceleration = 10;
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
            torsoDirection = transform.localRotation;
            assDirection = transform.localRotation;
            lastPosition = transform.localPosition;

            if ((allRenderers == null) || (allRenderers.Length == 0))
                allRenderers = GetComponentsInChildren<MeshRenderer>(true);

            RecordInitialTransformIfNull();

            controlledByPlayer = false;

            dead = false;

            RigidSet(false);

            if (charFeetsScript != null)
                charFeetsScript.feettracking.Init(this.transform);

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
                rigiMovement[i].Record(allRigidbodies[i].transform, transform.parent);
        }

        public override bool Decode(string tag, string data) {

            switch (tag) {
                case "pos": transform.localPosition = data.ToVector3(); break;
                case "p": controlledByPlayer = true; controlled = this; break;
                case "n": gameObject.name = data; break;
                case "speed": acceleration = data.ToFloat(); break;
                default: return false;
            }
            return true;
        }

        public override stdEncoder Encode() {

            stdEncoder cody = new stdEncoder();

            cody.Add("pos", transform.localPosition);

            cody.Add("speed", acceleration);

            if (controlledByPlayer) {
                cody.AddText("p", "");
            }

            cody.AddText("n", gameObject.name);
            
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
        public Vector3 toMove = Vector3.zero;

        float shootDelay = 0.2f;
        float shootingTime = 0;

        void Update() {

            //Debug.Log("Updates");

            if (controlledByPlayer) {

                looking = mousePos;

                Transform tf = Camera.main == null ? this.transform : Camera.main.transform;

                Vector3 move = Vector3.zero;
                if (Input.GetKey(KeyCode.W))
                    move += tf.forward;

                if (Input.GetKey(KeyCode.A))
                    move -= tf.right;

                if (Input.GetKey(KeyCode.S))
                    move -= tf.forward;

                if (Input.GetKey(KeyCode.D))
                    move += tf.right;

                if (Input.GetKey(KeyCode.LeftShift))
                    move -= tf.up;

                if (Input.GetKey(KeyCode.Space))
                    move += tf.up;

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

             //   move -= tf.up * 0.1f;



                velocity += transform.parent.InverseTransformVector(move) * Time.deltaTime * acceleration;

                float damper = Mathf.Max(0, (1 - Time.deltaTime * 2));

                if (move.x == 0) velocity.x *= damper;
                if (move.z == 0) velocity.z *= damper;
            }

            MovementUpdate();

        }

        void findNearestPathBox() {

            float MaxDistance = 999999;


            for (int i = 0; i < PathBox.StoryPoolController.pool.initializedCount; i++) {
                var path = (PathBox)PathBox.StoryPoolController.pool.getScript(i);
                if (path.gameObject.activeSelf) {
                    float dist = path.nearestDistance(transform.position);
                    if (dist < MaxDistance) {
                        MaxDistance = dist;
                        nearest = path;
                    }
                }
                    
            }
        }

        PathBox nearest;

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

                toMove = velocity *Mathf.Clamp01( Time.deltaTime);

                // transform.localPosition += toMove;
                //   toMove = Vector3.zero;


                if (managedBy != null)
                    managedBy.TryManageVelocity(this);
                
                for ( int i=0; i<PathBox.StoryPoolController.pool.initializedCount; i++) {
                    var path = (PathBox)PathBox.StoryPoolController.pool.getScript(i);
                    if ((path.gameObject.activeSelf) && (path != managedBy)) 
                        path.TryManageVelocity(this);
                }

                if (unmanagedTime > 2) {
                    if (nearest != null)
                        transform.position = Vector3.Lerp(transform.position, nearest.transform.position, Time.deltaTime);
                    else {
                        findNearestPathBox();
                        if (nearest == null) unmanagedTime = 0;
                    }
                }
                else nearest = null;

                unmanagedTime += Time.deltaTime;

                velocity -= toMove;

                velocity *= 0.9f;
                //velocity = Vector3.zero;

                //



                Vector3 delta = transform.localPosition - lastPosition;

                Quaternion needToFace = (pointingGun) ? Quaternion.LookRotation(new Vector3(looking.x, 0, looking.y)) :

                     (magnitude > walkingSpeed - 1) ? Quaternion.LookRotation(transform.parent.TransformPoint(velocity)) : torsoDirection;


                float angle = Quaternion.Angle(needToFace, torsoDirection);

                if ((velocity.magnitude > 1) || (pointingGun && (angle > 45 * (1.2 - (pointingGun ? LookDistance : 0)))) || (gotFinalizingSteps))
                    torsoDirection = Quaternion.Lerp(torsoDirection, needToFace, Mathf.Clamp01(Time.deltaTime * (1 + Mathf.Clamp(angle - 45, 0, 10))));

                if ((charFeetsScript != null) && ((Quaternion.Angle(torsoDirection, assDirection) > 70) || (magnitude > walkingSpeed - 1)))
                    charFeetsScript.feettracking.finalizingSteps = 2;

                if (gotFinalizingSteps)
                    assDirection = Quaternion.Lerp(assDirection, torsoDirection, Time.deltaTime * 5);

                transform.localRotation = torsoDirection;
                if (charFeetsScript != null)
                    charFeetsScript.feettracking.ass.transform.localRotation = assDirection;

                speed = (speed * 4 + (delta.magnitude) / Time.deltaTime) / 5;

                lastPosition = transform.localPosition;

                if (charGunScript != null)
                    charGunScript.DistantUpdate();
                if (charFeetsScript != null)
                    charFeetsScript.DistantUpdate();

                UpdateMovementTracking();

            }
        }

#if PEGI
        public override bool PEGI() {
            bool changed =  base.PEGI();

            ("speed " + velocity).nl();

            if (nearest != null) ("Lerping to " + nearest.name).nl();

            if (managedBy == null)
                ("Unmanaged " + unmanagedTime).nl();
            else ("Managed by " + managedBy.name).nl();

            "Acceleration ".edit(ref acceleration).nl();

            if ((stdValues == null) || (!stdValues.browsing_interactions)) {
                if ("Controlled by player: ".toggle(ref controlledByPlayer).nl()) {
                    if (controlledByPlayer) {
                        if (controlled != null) controlled.controlledByPlayer = false;
                        controlled = this;
                    } else {
                        if (controlled == this)
                            controlled = null;
                    }
                }

                pegi.ClickToEditScript();

            }

            return changed;
        }
#endif

        private void OnDisable()
        {
            if (controlled == this)
                controlled = null;
        }

        public override string getDefaultTagName() {
            return tagName;
        }

        public static STD_Pool pool;
        public override void SetStaticPoolController(STD_Pool inst) {
            pool = inst;
        }

       /* public override void OnDestroy()
        {
            base.OnDestroy();

            this.transform.Clear();
        }*/

        public class MovementTracking {
            Vector3 prePos = new Vector3();
            public Vector3 velocity = new Vector3();

            public void Record(Transform tr, Transform parent) {
                Vector3 inv = parent.InverseTransformPoint(tr.position);
                Vector3 delta = (inv - prePos) / Time.deltaTime;
                velocity = (velocity * 4 + delta) / 5;
                prePos = inv;
            }

            public void Set(Transform tr, Transform parent) {
                prePos = parent.InverseTransformPoint(tr.position);
                velocity = Vector3.zero;
            }

        }

    }
}
