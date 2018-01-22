using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


[ExecuteInEditMode]
public class CharacterFeetScript : MonoBehaviour {


    [Serializable]
    public class feetTracking {

        public enum feetside { left = 0, right = 1}

        public Transform rfoot;
        public Transform lfoot;

        public Transform rknee;
        public Transform lknee;

        public Transform rthize;
        public Transform lthize;

        public Transform ass;


        public float feetToKnee = 1;
        public float kneeToAss = 1;
        public float assToFloor = 1.5f;
        public float assSize = 0.5f;

        public class foot {
            public Vector3 position = new Vector3();
            public Vector3 kneePosition = new Vector3();
        }



        feetside turn = feetside.left;
        foot[] feet = null;
        Vector3 assPreviousPosition;
        Vector3 assVelocity;
        float stepSpeed = 0.1f;
        float stepDistanceLeft = 0;
        public int finalizingSteps = 2;
        Vector3 lastDisposition;
        Vector3 dest;
      
        public float stepSize;
        public float stepDistanceHalf;
        public Vector3 otherLegInTheAir;

        public void DistantUpdate(Transform holder) {
           
            float legLength = feetToKnee + kneeToAss;


            //    Debug.Log("ll " + legLength + " a t f " + assToFloor + " ss "+stepSize);
            Transform parent = holder.parent;

            assVelocity = (assVelocity  + (parent.InverseTransformPoint(ass.position) - assPreviousPosition) / Time.deltaTime) / 2;


            assToFloor = legLength * Mathf.Clamp(1- assVelocity.magnitude/ legLength*0.05f, 0.5f,0.95f);

            stepSize = Mathf.Sqrt(legLength * legLength - assToFloor * assToFloor);


            bool switching = false;

            if (((stepDistanceLeft <= 0) || ((assVelocity.magnitude / stepSize) > stepSpeed)) && (finalizingSteps>0)) {
                turn = (turn == feetside.left ? feetside.right : feetside.left);
                switching = true;
                finalizingSteps--;
            }

            foot cur = feet[(int)turn];
          //  foot other = feet[1 - (int)turn];
          //  Transform otherFoot = turn == feetside.left ? lfoot : rfoot;
          //  otherLegInTheAir = otherFoot.localPosition;

            dest = Mathf.Min(stepSize, assVelocity.magnitude * 2) * (assVelocity.magnitude > 0 ? assVelocity.normalized : Vector3.zero)

                   + parent.InverseTransformPoint((holder.right.normalized * (turn == feetside.right ? 1 : -1) * assSize)
                   + (ass.position) - ass.up * assToFloor);



            float feetPathLeft = Vector3.Distance(cur.position, dest);

            float minSpeed = feetPathLeft*assVelocity.magnitude / stepSize;

            if (switching) {
                stepDistanceLeft = feetPathLeft * 1.5f;
                stepDistanceHalf = feetPathLeft;
                stepSpeed = minSpeed;
             //   Debug.Log("Walk "+stepDistanceLeft+" with "+stepSpeed);
            }
          

            stepSpeed = Mathf.Max(Mathf.Max(stepSpeed, minSpeed),10); //  magic number

            if (stepDistanceLeft > 0) {

                float speedByTime = stepSpeed * Time.deltaTime;

                cur.position = MyMath.Lerp(cur.position, dest, speedByTime);
                
                stepDistanceLeft -= speedByTime;

                if (Vector3.Distance(cur.position, dest) < 1) stepDistanceLeft = -1;
            }

            if ((stepDistanceHalf<stepDistanceLeft) || (turn == feetside.left))
                lfoot.position = parent.TransformPoint(feet[(int)feetside.left].position);
            if ((stepDistanceHalf < stepDistanceLeft) || (turn == feetside.right))
                rfoot.position = parent.TransformPoint(feet[(int)feetside.right].position);


            // Stopping inversion:

            Vector3 deButt = ass.right * assSize;

            Vector3 AssPos = ass.position;

            Vector3 lhalfButtPosition = AssPos - deButt;
            Vector3 rhalfButtPosition = AssPos + deButt;

            Vector3 lfootToButt = lfoot.position - lhalfButtPosition;
            Vector3 rfootToButt = rfoot.position - rhalfButtPosition;

            if (rfootToButt.magnitude > legLength * 0.9f) {
                rfootToButt = rfootToButt.normalized * legLength * 0.9f;
                rfoot.position = rhalfButtPosition + rfootToButt;   
            }

            if (lfootToButt.magnitude > legLength * 0.9f)
            {
                lfootToButt = lfootToButt.normalized * legLength * 0.9f;
                lfoot.position = parent.TransformPoint( lhalfButtPosition + lfootToButt);
            }

            float lbase = lfootToButt.magnitude;
            float rbase = rfootToButt.magnitude;

            float lkneeDist = MyMath.HeronHforBase(lbase, kneeToAss, feetToKnee);
            float rkneeDist = MyMath.HeronHforBase(rbase, kneeToAss, feetToKnee);

            

            Vector3 lkneeBasePoint = lhalfButtPosition + (lfootToButt * feetToKnee) / (legLength);
            Vector3 rkneeBasePoint = rhalfButtPosition + (rfootToButt * feetToKnee) / (legLength);


            lknee.position = Vector3.Cross(lfootToButt, ass.right).normalized * lkneeDist+ lkneeBasePoint;
            rknee.position = Vector3.Cross(rfootToButt, ass.right).normalized * rkneeDist+ rkneeBasePoint;

            Vector3 lkneeUpVec = lknee.position - lfoot.position;
            Vector3 rkneeUpVec = rknee.position - rfoot.position;
            lknee.LookAt(Vector3.Cross(lkneeUpVec, ass.right).normalized + lknee.position, lkneeUpVec);
            rknee.LookAt(Vector3.Cross(rkneeUpVec, ass.right).normalized + rknee.position, rkneeUpVec);

            feet[(int)feetside.left].kneePosition = lknee.position;
            feet[(int)feetside.right].kneePosition =  rknee.position;

            Vector3 lthizeUpVector = lhalfButtPosition - lknee.position;
            Vector3 rthizeUpVector = rhalfButtPosition - rknee.position;
            lthize.position = lhalfButtPosition;
            rthize.position = rhalfButtPosition;

            lthize.LookAt(Vector3.Cross(lthizeUpVector, ass.right).normalized + lthize.position, lthizeUpVector);
            rthize.LookAt(Vector3.Cross(rthizeUpVector, ass.right).normalized + rthize.position, rthizeUpVector);
         


            lknee.position = feet[(int)feetside.left].kneePosition;
            rknee.position = feet[(int)feetside.right].kneePosition;

            // Back to transforming
            lfoot.position = parent.InverseTransformPoint(feet[(int)feetside.left].position);
            rfoot.position = parent.InverseTransformPoint(feet[(int)feetside.right].position);

            Vector3 itAss = parent.InverseTransformPoint(ass.position);

            assPreviousPosition = itAss;

            if ((lastDisposition - itAss).magnitude > stepSize * 0.3f) {
                lastDisposition = itAss;
                finalizingSteps = 2;
            }

        }



        public void Init(Transform holder) {
            Transform parent = holder.parent;
            assVelocity = Vector3.zero;
            lastDisposition = parent.InverseTransformPoint(ass.position);
            assPreviousPosition = lastDisposition;
            if ((feet == null) || (feet.Length == 0)) {
                feet = new foot[2];
                feet[0] = new foot();
                feet[1] = new foot();
            }
            feet[(int)feetside.left].position = parent.InverseTransformPoint(lfoot.position);
            feet[(int)feetside.right].position = parent.InverseTransformPoint(rfoot.position);
        }

    }

  

    public feetTracking feettracking;


	
	// Update is called once per frame
	public void DistantUpdate () {

     //   if (allRigidbodies.Length != rigiMovement.Length)
          //  SetMovementTracking();

        if (Application.isPlaying) {
         
            feettracking.DistantUpdate(transform);
        }
    }
}
