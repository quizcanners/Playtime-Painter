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

  

        // Use this for initialization
        void Start() {

        }


        void OnDrawGizmosSelected() {
            transform.DrawTransformedCubeGizmo(Color.green);
        }

        // Update is called once per frame
        void Update() {

           



         
        }
    }
}