using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using StoryTriggerData;

namespace SharedTools_Stuff
{
    
#if UNITY_EDITOR
    using UnityEditor;

    [CustomEditor(typeof(SpeedAnimationController))]
    public class SpeedAnimationControllerDrawer : Editor {
        public override void OnInspectorGUI() {
            ((SpeedAnimationController)target).inspect(serializedObject); 
        }
    }

    [CustomEditor(typeof(iSTD_Explorer))]
    public class iSTD_ExplorerDrawer : Editor
    {
        public override void OnInspectorGUI() {
            ((iSTD_Explorer)target).inspect(serializedObject);
        }
    }


#endif

}