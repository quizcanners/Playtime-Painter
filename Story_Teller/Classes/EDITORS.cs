using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;


#if UNITY_EDITOR
using UnityEditor;

namespace StoryTriggerData {
    public class StoryEditorScripts {

        [CustomEditor(typeof(Page))]
        public class StoryPageDrawer : Editor {
            public override void OnInspectorGUI() {
                ef.start(serializedObject);
                ((Page)target).PEGI();
                pegi.newLine();
            }
        }

     /*   [CustomEditor(typeof(StoryObject))]
        public class StoryObjectDrawer : Editor {
            public override void OnInspectorGUI() {
                ef.start();
                ((StoryObject)target).PEGI();
                pegi.newLine();
            }
        }*/
      

        [CustomEditor(typeof(Book))]
        public class StoryLinkControllerDrawer : Editor {
            public override void OnInspectorGUI() {
                ef.start(serializedObject);
                ((Book)target).PEGI();
                pegi.newLine();
            }
        }

      

       

    }
}
#endif