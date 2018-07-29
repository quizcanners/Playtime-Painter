using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StoryTriggerData {

    public class CharacterGUNscript : MonoBehaviour {

        public Actor hero;

        public Transform Gun;
        public Transform relbow;
        public Transform rshoulder;

        public Transform lelbow;
        public Transform lshoulder;

        public Transform chest;
        public Transform gunDownPosition;
        public Transform leftGunPosition;

        public float sholderToGunShooting = 2.5f;

        public float sholderToElbow = 1.5f;
        public float elbowToArm = 1.5f;
        public float shoulderWidth = 2f;

        public Vector3 pointerPosition;
        public float lookDistance = 1;

     //   Quaternion gunDirection;

        public void OnEnable() {
            if (hero == null)
                hero = GetComponent<Actor>();
        }

        public void DistantUpdate() {

            /*

            float armLength = sholderToElbow + elbowToArm;

            Vector3 mousePosv3 = pointerPosition - transform.position;
            if (mousePosv3.magnitude == 0) mousePosv3 = Vector3.one;

            bool pointingGun = hero.shooting || (!hero.runing);

            Vector3 gunPosDest = pointingGun ? rshoulder.position + mousePosv3.normalized * sholderToGunShooting * 0.5f * (1 + lookDistance) : gunDownPosition.position;

            float gunAnimSpeed = hero.shooting ? 1 : Time.deltaTime * 5;

            Vector3 gunPos = Vector3.Lerp(Gun.position, gunPosDest, gunAnimSpeed);
            Gun.position = gunPos;
            Vector3 lGunPos = leftGunPosition.position;

            Quaternion destRotaton = pointingGun ? Quaternion.LookRotation(mousePosv3) : gunDownPosition.rotation;

            Quaternion gunRotation = Quaternion.Lerp(Gun.rotation, destRotaton, gunAnimSpeed);

            Gun.rotation = gunRotation;


            Vector3 lGunToShoulder = leftGunPosition.transform.position - lshoulder.position;
            Vector3 rGunToShoulder = Gun.transform.position - rshoulder.position;


            float lbase = rGunToShoulder.magnitude;
            float rbase = lGunToShoulder.magnitude;

            // Debug.Log(lbase + " " + elbowToArm + sholderToElbow);
            float lelbowDist = MyMath.HeronHforBase(lbase, elbowToArm, sholderToElbow);
            float relbowDist = MyMath.HeronHforBase(rbase, elbowToArm, sholderToElbow);

            Vector3 lshoulderPosition = chest.position - chest.right * shoulderWidth;
            Vector3 rshoulderPosition = chest.position + chest.right * shoulderWidth;

            Vector3 lelbowBasePoint = lshoulderPosition + (lGunToShoulder * elbowToArm) / (armLength);
            Vector3 relbowBasePoint = rshoulderPosition + (rGunToShoulder * elbowToArm) / (armLength);

            //  Debug.Log(lkneeBasePoint + " " + lGunToShoulder + " " + chest + " " + lelbowDist + " ");

            Vector3 lshoulderVec = chest.up - chest.right;
            Vector3 rshoulderVec = chest.up + chest.right;

            lelbow.position = lelbowBasePoint + Vector3.Cross(lGunToShoulder, lshoulderVec).normalized * lelbowDist;
            relbow.position = relbowBasePoint - Vector3.Cross(rGunToShoulder, rshoulderVec).normalized * relbowDist;

            Vector3 lelbowUpVec = lelbow.position - lGunPos;
            Vector3 relbowUpVec = relbow.position - gunPos;
            lelbow.LookAt(Vector3.Cross(lelbowUpVec, lshoulderVec).normalized + lelbow.position, lelbowUpVec);
            relbow.LookAt(Vector3.Cross(relbowUpVec, rshoulderVec).normalized + relbow.position, relbowUpVec);

            Vector3 lelbowPos = lelbow.position;
            Vector3 relbowPos = relbow.position;
            Quaternion lelbowRot = lelbow.rotation;
            Quaternion relbowRot = relbow.rotation;


            Vector3 lshoulderUpVector = lshoulderPosition - lelbowPos;
            Vector3 rshoulderUpVector = rshoulderPosition - relbowPos;
            lshoulder.position = lshoulderPosition;
            rshoulder.position = rshoulderPosition;

            lshoulder.LookAt(Vector3.Cross(lshoulderUpVector, chest.right).normalized + lshoulder.position, lshoulderUpVector);
            rshoulder.LookAt(Vector3.Cross(rshoulderUpVector, chest.right).normalized + rshoulder.position, rshoulderUpVector);

            lelbow.position = lelbowPos;
            relbow.position = relbowPos;
            lelbow.rotation = lelbowRot;
            relbow.rotation = relbowRot;

            Gun.position = gunPos;
            Gun.rotation = gunRotation;
            */
        }


    }
}