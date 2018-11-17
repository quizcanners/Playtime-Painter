using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;

#if UNITY_EDITOR
using UnityEditor;

namespace SharedTools_Stuff {

    [CustomEditor(typeof(PEGI_Styles))]
    public class PEGI_StylesDrawer : Editor
    {
        public override void OnInspectorGUI() => ((PEGI_Styles)target).Inspect(serializedObject);

#if PEGI
        [MenuItem("Tools/" + "PEGI" + "/Disable")]
        public static void DisablePegi()
        {
            UnityHelperFunctions.SetDefine("PEGI", false);
            UnityHelperFunctions.SetDefine("NO_PEGI", true);
        }
#else
        [MenuItem("Tools/" + "PEGI" + "/Enable")]
        public static void EnablePegi()
        {
            UnityHelperFunctions.SetDefine("PEGI", true);
            UnityHelperFunctions.SetDefine("NO_PEGI", false);
        }
#endif

    }



    [CustomEditor(typeof(PEGI_SimpleInspectorsBrowser))]
    public class PEGI_SimpleInspectorsBrowserDrawer : Editor
    {
        public override void OnInspectorGUI() => ((PEGI_SimpleInspectorsBrowser)target).Inspect(serializedObject);
    }

/*
    [CustomEditor(typeof(ISTD_Explorer))]
    public class ISTD_ExplorerDrawer : Editor
    {
        public override void OnInspectorGUI() => ((ISTD_Explorer)target).Inspect(serializedObject);
    }*/

    [CustomEditor(typeof(GodMode))]
    public class GodModeDrawer : Editor
    {
        public override void OnInspectorGUI() => ((GodMode)target).Inspect(serializedObject);
    }

   

}
#endif

