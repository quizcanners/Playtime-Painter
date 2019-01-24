using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QuizCannersUtilities;

#if UNITY_EDITOR
using UnityEditor;

namespace PlayerAndEditorGUI {

    public abstract class PEGI_Editor<T> : Editor where T : MonoBehaviour
    {
        public override void OnInspectorGUI()
        {
            #if PEGI
            ef.Inspect<T>(this).RestoreBGColor();
            #else
             DrawDefaultInspector();
            #endif
        }
    }

    public abstract class PEGI_Editor_SO<T> : Editor where T : ScriptableObject
    {
        public override void OnInspectorGUI()
        {
            #if PEGI
            ef.Inspect_so<T>(this).RestoreBGColor();
            #else
            DrawDefaultInspector();
            #endif
        }
    }

    [CustomEditor(typeof(PEGI_Styles))]
    public class PEGI_StylesDrawer : PEGI_Editor<PEGI_Styles>
    {

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
    public class PEGI_SimpleInspectorsBrowserDrawer : PEGI_Editor<PEGI_SimpleInspectorsBrowser> { }

}
#endif

