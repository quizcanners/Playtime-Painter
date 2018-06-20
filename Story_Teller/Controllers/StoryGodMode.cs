using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
namespace StoryTriggerData
{

#if PEGI && UNITY_EDITOR
    using UnityEditor;

    [CustomEditor(typeof(StoryGodMode))]
    public class StoryGodModeDrawer : Editor
    {
        public override void OnInspectorGUI() => ((StoryGodMode)target).Inspect(serializedObject);
        
    }
#endif

    public class StoryGodMode : GodMode
    {



        public override void DistantUpdate()
        {

            if (Actor.controlled == null)
            {
                Vector3 add = Vector3.zero;

                if (Input.GetKey(KeyCode.W)) add += transform.forward;
                if (Input.GetKey(KeyCode.A)) add -= transform.right;
                if (Input.GetKey(KeyCode.S)) add -= transform.forward;
                if (Input.GetKey(KeyCode.D)) add += transform.right;
                add.y = 0;
                if (Input.GetKey(KeyCode.LeftShift)) add += Vector3.down;
                if (Input.GetKey(KeyCode.Space)) add += Vector3.up;


                transform.position += add * speed * Time.deltaTime;
            }


            if ((Application.isPlaying) && (!disableRotation))
            {

                if (Input.GetMouseButton(1))
                {
                    float rotationX = transform.localEulerAngles.y;
                    rotationY = transform.localEulerAngles.x;



                    rotationX += Input.GetAxis("Mouse X") * sensitivity;
                    rotationY -= Input.GetAxis("Mouse Y") * sensitivity;

                    if (rotationY < 120)
                        rotationY = Mathf.Min(rotationY, 85);
                    else
                        rotationY = Mathf.Max(rotationY, 270);


                    transform.localEulerAngles = new Vector3(rotationY, rotationX, 0);

                }


                SpinAround();

                if ((OrbitDistance == 0) && (transform.position != Vector3.zero))
                {
                    SpaceValues.playerPosition.Add(transform.position);
                    transform.position = Vector3.zero;
                    Book.instBook.AfterPlayerSpacePosUpdate();
                }
            }
        }

        public override void Update()
        {



        }

        public void OnEnable()
        {
            inst = this;
        }

    }
}