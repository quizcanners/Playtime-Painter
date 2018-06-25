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

    [CustomEditor(typeof(PathBox))]
    public class PathBoxDrawer : Editor
    {
        public override void OnInspectorGUI() => ((PathBox)target).Inspect(serializedObject);
    }

    [CustomEditor(typeof(Page))]
    public class StoryPageDrawer : Editor
    {
        public override void OnInspectorGUI() => ((Page)target).Inspect(serializedObject);
    }
    
    [CustomEditor(typeof(Book))]
    public class StoryLinkControllerDrawer : Editor
    {
        public override void OnInspectorGUI() => ((Book)target).Inspect_so(serializedObject);
    }
    
    [CustomEditor(typeof(Terra))]
    public class TerraDrawer : Editor
    {
        public override void OnInspectorGUI() => ((Terra)target).Inspect(serializedObject);
    }

    [CustomEditor(typeof(CubeWorldSpace))]
    public class CubeWorldSpaceDrawer : Editor
    {
        public override void OnInspectorGUI() => ((CubeWorldSpace)target).Inspect(serializedObject);
    }

    [CustomEditor(typeof(Actor))]
    public class ActorDrawer : Editor
    {
        public override void OnInspectorGUI() => ((Actor)target).Inspect(serializedObject);
    }

#endif

}
