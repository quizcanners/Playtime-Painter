using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingStuff : MonoBehaviour
{

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.W))
            transform.position += Vector3.up * Time.deltaTime;
       
    }

}
