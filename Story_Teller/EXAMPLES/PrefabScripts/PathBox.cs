using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using TextureEditor;
using PlayerAndEditorGUI;

namespace StoryTriggerData {


#if UNITY_EDITOR
    using UnityEditor;

    [CustomEditor(typeof(PathBox))]
    public class PathBoxDrawer : Editor {
        public override void OnInspectorGUI() {
            ef.start(serializedObject);
            ((PathBox)target).PEGI();
            pegi.newLine();
        }
    }
# endif
  
    [TagName(PathBox.tagName)]
    [ExecuteInEditMode]
    public class PathBox : STD_Object {

        VariablesWeb conditions;

        public const string tagName = "path";

        public override void Decode(string tag, string data) {
            switch (tag) {
                case "size": transform.localScale = data.ToVector3(); break;
                case "pos": transform.localPosition = data.ToVector3(); break;
                case "rot": transform.localRotation = Quaternion.Euler(data.ToVector3()); break;
                case "c": conditions.Reboot(data); break;
            }
        }

        public override void Reboot() {
            transform.localPosition = Vector3.zero;
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;
            conditions = new VariablesWeb(null);
        }

        public override stdEncoder Encode() {
            var cody = new stdEncoder();
            cody.Add("size", transform.localScale);
            cody.AddIfNotZero("pos", transform.localPosition);
            cody.Add("rot", transform.localRotation.eulerAngles);
            cody.Add("c", conditions);
            return cody;
        }

        public static STD_Pool StoryPoolController;
        public override void SetStaticPoolController(STD_Pool inst) {
            StoryPoolController = inst;
        }

        public override string getDefaultTagName() {
            return tagName;
        }

        public override void PEGI() {
            "Path pegi".nl();
          
            pegi.ClickToEditScript();

            conditions.PEGI();

            base.PEGI();
        }

        void OnDrawGizmosSelected() {
            transform.DrawTransformedCubeGizmo(Color.green);
        }

		public override OnDeactivate(){
			base.OnDeactivate();
			foreach (var a in managedActors)
				a.managedBy = null;
				managedActors.Clear();
		}

		public List<Actor> managedActors = new List<Actor>();

		void Manage(Actor a){
			a.managedBy = this;
			managedActors.Add(a);
		}

		float Inside (Vector3 pos ){
			 // Actor velocity will be in worldspace.

			return Mathf.Max(Mathf.Max( Mathf.Abs(pos.x), Mathf.Abs(pos.y)), Mathf.Abs(pos.z));
		}

		
void ManageSoftMovement (ref float localPos, ref float localVelocity, float localScale){

bool sameDirection = ((localPos>0) == (localVelocity>0));
					
					if (sameDirection){
					
						float fromWallDistance = (1-Mathf.Abs(localPos))*localScale;
						const float wallThickness = 0.3f;
						float existingInsideWall = Mathf.Max(0, wallThickness-fromWallDistance); 
						if (existingInsideWall<wallThickness){
							float stretchedInsideWall = 1/(wallThickness - existingInsideWall);
							stretchedInsideWall+= Mathf.Abs(localVelocity);
							float newInsideWall = wallThickness - 1/(stretchedInsideWall);
							float usedPortion = newInsideWall - existingInsideWall;
							localVelocity *= usedPortion/ Mathf.Abs(localVelocity);
						}
						
						
					} else {
						localPos+=localVelocity;
						localVelocity = 0;
					}

}

		public bool TryManageVelocity (Actor actor){
			if (actor.managedBy != this){
				if ((actor.transform.position-transform.position).magnitude < transform.lossyScale.magnitude*1.1f){
					
					Vector3 localPos = transform.InverseTransformPoint(actor.transform.position);
					Vector3 localVelocity = transform.InverseTransformVector(actor.velocity);
					
					if (Inside(localPos+localVelocity)){
						actor.transform.position+=actor.velocity;
						actor.velocity = Vector3.zero;
									  	// If actor was unmanaged for some time, 
									  	//lerp him back to managing path or nearest path if managing path was removed.
						if (actor.unmanagedTime > 0) 	// Can only be added bu filed management or null management
							Manage(actor);
					} else {
						
						
						
					}
				}
			} else {
			
				Vector3 localPos = transform.InverseTransformPoint(actor.transform.position);
			
				if (Inside(localPos)){
					Vector3 localVelocity = transform.InverseTransformVector(actor.velocity);
					//Vector3 localDirection = transform.InverseTransformDirection(actor.velocity);
					
					//float posX = localPos.x;
					//float velX = localVelocity.x;
					ManageSoftMovement (ref localPos.x, ref localVelocity.x, localScale.x);
					
					// localPos = new Vector3(posX, posY, posZ);
					// localVelocity = new Vector3....
					
					actor.velocity = transform.TransformVector(localVelocity);
					actor.unmanagedTime = 0;
					
				} else {
					if (actor.unmanagedTime>2) 
						actor.transform.position = Vector3.Lerp(actor.transform.position, transform.position, Time.deltaTime);
					actor.unmanagedTime += Time.deltaTime; // Also do this when managed by is null;
				}
			}
		}
    }
}
