using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QuizCannersUtilities;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;

namespace PlayerAndEditorGUI {

    public abstract class PEGI_Editor_Base : Editor {
        public static bool drawDefaultInspector;

        protected abstract bool Inspect(Editor editor);

        public override void OnInspectorGUI()
        {
#if PEGI
            if (!drawDefaultInspector)
            {
                Inspect(this).RestoreBGColor();
                return;
            }

            pegi.toggleDefaultInspector();
#endif

            DrawDefaultInspector();
        }

    }

     public abstract class PEGI_Editor<T> : PEGI_Editor_Base where T : MonoBehaviour {
        protected override bool Inspect(Editor editor) => ef.Inspect<T>(editor);
     }

    public abstract class PEGI_Editor_SO<T> : PEGI_Editor_Base where T : ScriptableObject {
        protected override bool Inspect(Editor editor) => ef.Inspect_so<T>(editor);
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

