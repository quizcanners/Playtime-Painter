using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QuizCannersUtilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PlayerAndEditorGUI {
    
    public abstract class PEGI_Inspector_Material
        #if UNITY_EDITOR
        : ShaderGUI
        #endif
    {

        #if UNITY_EDITOR
        public MaterialEditor unityMaterialEditor;
        MaterialProperty[] _properties;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            unityMaterialEditor = materialEditor;
            _properties = properties;
            #if PEGI
            ef.Inspect_Material(this);
            #endif
        }
        #endif

        public void DrawDefaultInspector()
        #if UNITY_EDITOR
            => base.OnGUI(unityMaterialEditor, _properties);
        #else
            {}
        #endif




#if PEGI
        public abstract bool Inspect(Material mat);
#endif

    }

#if UNITY_EDITOR

    public abstract class PEGI_Inspector_Base  : Editor
    {
    public static bool drawDefaultInspector;

        #if PEGI
        protected abstract bool Inspect(Editor editor);
        #endif

        public override void OnInspectorGUI() {
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

     public abstract class PEGI_Inspector<T> : PEGI_Inspector_Base where T : MonoBehaviour {
#if PEGI
        protected override bool Inspect(Editor editor) => ef.Inspect<T>(editor);
#endif
     }

    public abstract class PEGI_Inspector_SO<T> : PEGI_Inspector_Base where T : ScriptableObject {
#if PEGI
        protected override bool Inspect(Editor editor) => ef.Inspect_so<T>(editor);
#endif
    }


    [CustomEditor(typeof(PEGI_Styles))]
    public class PEGI_StylesDrawer : PEGI_Inspector<PEGI_Styles>
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
    public class PEGI_SimpleInspectorsBrowserDrawer : PEGI_Inspector<PEGI_SimpleInspectorsBrowser> { }


#endif
}

