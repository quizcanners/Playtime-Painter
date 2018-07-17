using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using Playtime_Painter;
using PlayerAndEditorGUI;
using SharedTools_Stuff;
using STD_Logic;


namespace StoryTriggerData {


    [StoryTagName(PathBox.tagName)]
    [ExecuteInEditMode]
    public class PathBox : STD_Poolable, IPEGI
    {

        ConditionBranch conditions;

        public const string tagName = "path";

        public override bool Decode(string tag, string data) {
            switch (tag) {
                case "n": gameObject.name = data; break;
                case "size": transform.localScale = data.ToVector3(); break;
                case "pos": transform.localPosition = data.ToVector3(); break;
                case "rot": transform.localRotation = Quaternion.Euler(data.ToVector3()); break;
                case "c": conditions.Decode(data); break;
                default: return false;
            }
            return true;
        }

        public override void Reboot() {
            transform.localPosition = Vector3.zero;
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
            conditions = new ConditionBranch();
        }

        public override StdEncoder Encode() =>this.EncodeUnrecognized()
            .Add_String("n", name)
            .Add("size", transform.localScale)
            .Add_IfNotZero("pos", transform.localPosition)
            .Add("rot", transform.localRotation.eulerAngles)
            .Add("c", conditions);

        public static STD_Pool StoryPoolController;
        public override void SetStaticPoolController(STD_Pool inst) {
            StoryPoolController = inst;
        }

        public override string GetObjectTag() {
            return tagName;
        }
#if PEGI
        public override bool PEGI() {
            "Path pegi".nl();

            if (managedActors.Count > 0)
            {
                "Managed Actors:".nl();
                foreach (var actor in managedActors)
                    actor.name.nl();
            }


            pegi.ClickToEditScript();

            conditions.PEGI();

            base.PEGI();
            return false;
        }

#endif

        void OnDrawGizmos() {
            transform.DrawTransformedCubeGizmo(managedActors.Count>0 ?  Color.green : Color.white);

          /*  for (int i=0; i< StoryPoolController.pool.Max; i++) {
                var path = StoryPoolController.pool.getScript(i);
                if ((path != null) && (path != this) && (path.gameObject.activeSelf))
                    path.transform.DrawTransformedCubeGizmo(Color.white);
            }*/
                

        }
        
		public void OnDeactivate(){
			
			foreach (var a in managedActors)
				a.managedBy = null;
				managedActors.Clear();
		}

		public List<Actor> managedActors = new List<Actor>();

		void Manage(Actor a){
           // Debug.Log("Starting to manage: "+name);
            if (a.managedBy != null) a.managedBy.managedActors.Remove(a);
			a.managedBy = this;
			managedActors.Add(a);
		}

		float Max (Vector3 pos ){
			 // Actor velocity will be in worldspace.

			return Mathf.Max(Mathf.Max( Mathf.Abs(pos.x), Mathf.Abs(pos.y)), Mathf.Abs(pos.z));
		}

        void ManageSoftMovement (ref float localPos, ref float localVelocity, float localScale){

            if (localVelocity == 0) return;

         

                    bool sameDirection = ((localPos>0) == (localVelocity>0));
					
					if (sameDirection) {

                     

						float fromWallDistance = (1-Mathf.Abs(localPos))*localScale;
						const float wallThickness_A = 0.2999f;
						

                if (wallThickness_A < fromWallDistance) {
                    float portion = Mathf.Min(fromWallDistance - wallThickness_A, Mathf.Abs(localVelocity)) / Mathf.Abs(localVelocity);
                    localPos += portion * localVelocity;
                    localVelocity *= (1 - portion);

                    fromWallDistance = (1 - Mathf.Abs(localPos)) * localScale;

                    if (localVelocity.IsNaN())
                        Debug.Log("Pre Wall created to Nan");

                }

                const float wallThickness_B = 0.3f;

                if (wallThickness_B >= fromWallDistance) {

                    float existingInsideWall = wallThickness_B - fromWallDistance;

                    float risk = wallThickness_B - existingInsideWall;
                    
                    float stretchedInsideWall = 1 /  risk;
                    stretchedInsideWall += Mathf.Abs(localVelocity);
                    float newInsideWall = wallThickness_B - 1 /  stretchedInsideWall;
                    float usedAmount = newInsideWall - existingInsideWall;
                    float signedAmount = localVelocity > 0 ? usedAmount : -usedAmount;

                    localPos += signedAmount;
                    localVelocity -= signedAmount;

                    if (localVelocity.IsNaN())
                        Debug.Log("In Wall turned to Nan");

                }

						
						
					} else {

                
						localPos+=localVelocity;
						localVelocity = 0;
					}

        }

        public float nearestDistance(Vector3 to) {
            Vector3 localPos = transform.InverseTransformPoint(to);
            return Mathf.Max(Mathf.Max(Mathf.Max(
                Mathf.Abs(localPos.x) - transform.localScale.x,
                 Mathf.Abs(localPos.y) - transform.localScale.y),
                  Mathf.Abs(localPos.z) - transform.localScale.z
                 ), 0);


        }
        
        Vector3 localVelocity = Vector3.zero;
        Vector3 localPos = Vector3.zero;
        public void TryManageVelocity (Actor actor){

            bool inited = false;
           

            if (actor.managedBy != this)  {
                if ((actor.transform.position-transform.position).magnitude < transform.lossyScale.magnitude*1.1f) {

                    localVelocity = transform.InverseTransformVector(actor.transform.parent.TransformVector(actor.toMove));
                    localPos = transform.InverseTransformPoint(actor.transform.position);

                    if (Max(localPos+localVelocity)<1) {

                        //Debug.Log("Fully ");

                        localPos += localVelocity;
                        localVelocity = Vector3.zero;
                        inited = true;

                        if (localVelocity.IsNaN())
                            Debug.Log("After fully " + localPos + " local velocity " + localVelocity);

                        if (actor.unmanagedTime > 0.1f) 	// Can only be added bu filed management or null management
							Manage(actor);
					} else {

                        Debug.Log("Partially ");

                        if (Max(localPos + Vector3.Scale(localVelocity, Vector3.right)) < 1) {
                            localPos.x += localVelocity.x;
                            localVelocity.x = 0;
                            inited = true;
                        }
						
						if (Max(localPos+Vector3.Scale(localVelocity, Vector3.up)) < 1) { 
							localPos.y += localVelocity.y;
                            localVelocity.y = 0;
                            inited = true;
                        }

                    if (Max(localPos + Vector3.Scale(localVelocity, Vector3.forward)) < 1) {
                        localPos.z += localVelocity.z;
                        localVelocity.z = 0;
                            inited = true;
                    }
                    

                    }

                    actor.toMove = actor.transform.parent.InverseTransformVector(transform.TransformVector(localVelocity));
                    actor.transform.position = transform.TransformPoint(localPos);
                }
                
            } else {

            

                 localPos = transform.InverseTransformPoint(actor.transform.position);

                if (Max(localPos)<1) {

                    

                    localVelocity = transform.InverseTransformVector(actor.transform.parent.TransformVector(actor.toMove));
     
                    ManageSoftMovement (ref localPos.x, ref localVelocity.x, transform.localScale.x);
					ManageSoftMovement (ref localPos.z, ref localVelocity.z, transform.localScale.z);
					ManageSoftMovement (ref localPos.y, ref localVelocity.y, transform.localScale.y);

                 

                    actor.unmanagedTime = 0;
                    inited = true;
                   
                }
			}


            if (inited) {
                if (localVelocity.IsNaN()) Debug.Log("Local vel is nan");
                else
                actor.toMove = actor.transform.parent.InverseTransformVector(transform.TransformVector(localVelocity));
                if (localPos.IsNaN()) Debug.Log("Local pos is Nan");
                else
                actor.transform.position = transform.TransformPoint(localPos);
            }

		}
    }
}
