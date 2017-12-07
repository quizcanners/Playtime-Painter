using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;



[ExecuteInEditMode]
public class GodMode : MonoBehaviour {

    public static GodMode inst;
    public float speed = 100;
    public float sensitivity = 5;

    public static string PrefSpeed = "GodSpeed";
    public static string PrefSens = "GodSensitivity";

    private void OnEnable() {
        speed= PlayerPrefs.GetFloat(PrefSpeed);
        sensitivity= PlayerPrefs.GetFloat(PrefSens);
        if (speed == 0) speed = 100;
        if (sensitivity == 0) sensitivity = 5;
        inst = this;
    }

    private void Start()  {
        inst = this;        
    }

    // Update is called once per frame
    float rotationY;


    void Update () {

        Vector3 add = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) add += transform.forward;
        if (Input.GetKey(KeyCode.A)) add -= transform.right;
        if (Input.GetKey(KeyCode.S)) add -= transform.forward;
        if (Input.GetKey(KeyCode.D)) add += transform.right;
        add.y = 0;
        if (Input.GetKey(KeyCode.LeftShift)) add += Vector3.down;
        if (Input.GetKey(KeyCode.Space)) add += Vector3.up;


        transform.position += add*speed * Time.deltaTime;


        if (Input.GetMouseButton(1) )   {
            float rotationX =transform.localEulerAngles.y;
            rotationY = transform.localEulerAngles.x;



            rotationX += Input.GetAxis("Mouse X") * sensitivity;
            rotationY -= Input.GetAxis("Mouse Y") * sensitivity;

            if (rotationY < 120 )
                rotationY = Mathf.Min(rotationY, 85);
            else
                rotationY = Mathf.Max(rotationY, 270);


            transform.localEulerAngles = new Vector3(rotationY, rotationX, 0);

        }

        if (Application.isPlaying)
            SpinAround();

    }



    public Vector2 camOrbit = new Vector2();
    public Vector3 SpinCenter;
    public float OrbitDistance = 0;
    public bool OrbitingFocused;
    public float SpinStartTime = 0;

    public void SpinAround() {

        Transform camTr = gameObject.tryGetCameraTransform();
        if (Input.GetMouseButtonDown(2))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                SpinCenter = hit.point;
            }
            else return;


            Quaternion before = camTr.transform.rotation;//cam.transform.rotation;
            camTr.transform.LookAt(SpinCenter);
            Vector3 rot = camTr.transform.rotation.eulerAngles;
            camOrbit.x = rot.y;
            camOrbit.y = rot.x;
            OrbitDistance = (SpinCenter - camTr.transform.position).magnitude;
          
            camTr.transform.rotation = before;
            OrbitingFocused = false;
            SpinStartTime = Time.time;
        }

        if (Input.GetMouseButtonUp(2))
            OrbitDistance = 0;

        if ((OrbitDistance > 0) && (Input.GetMouseButton(2)))
        {

            camOrbit.x += Input.GetAxis("Mouse X") * 5;
            camOrbit.y -= Input.GetAxis("Mouse Y") * 5;

            if (camOrbit.y <= -360)
                camOrbit.y += 360;
            if (camOrbit.y >= 360)
                camOrbit.y -= 360;

            Quaternion rot = Quaternion.Euler(camOrbit.y, camOrbit.x, 0);
            Vector3 campos = rot *
                (new Vector3(0.0f, 0.0f, -OrbitDistance)) +
                SpinCenter;

            camTr.position = campos;
         //   if ((Time.time - SpinStartTime) > 0.5f)
            
                if (!OrbitingFocused)
                {
                    camTr.rotation = MyMath.Lerp(camTr.rotation, rot, 200*Time.deltaTime);
                    if (Quaternion.Angle(camTr.rotation, rot) < 1)
                        OrbitingFocused = true;
                }
                else camTr.rotation = rot;
           // }

        }
    }

    public void PEGI() {

        pegi.write("speed:");
        if (pegi.edit(ref speed))
            PlayerPrefs.SetFloat(GodMode.PrefSpeed, speed);
        pegi.newLine();

        pegi.write("sensitivity:");
        if (pegi.edit(ref sensitivity))
            PlayerPrefs.SetFloat(GodMode.PrefSens, sensitivity);
        pegi.newLine();

        pegi.write("WASD - move"); pegi.newLine();
        pegi.write("Shift, Space - Dwn, Up"); pegi.newLine();
        pegi.write("RMB - look around"); pegi.newLine();
        pegi.write("MMB - Orbit");


    }



}
