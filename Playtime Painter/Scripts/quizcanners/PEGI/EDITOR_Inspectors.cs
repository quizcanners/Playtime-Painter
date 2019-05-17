using UnityEngine;
using QuizCannersUtilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PlayerAndEditorGUI {

#if UNITY_EDITOR

    // Use this two lines to override default Inspector with contents of Inspect() function (replacing SimplePEGInspectorsBrowser with YouClassName)
    [CustomEditor(typeof(SimplePEGInspectorsBrowser))]
    public class PEGI_SimpleInspectorsBrowserDrawer : PEGI_Inspector_Mono<SimplePEGInspectorsBrowser> { }

#endif

    public abstract class PEGI_Inspector_Material
        #if UNITY_EDITOR
        : ShaderGUI
        #endif
    {

        #if UNITY_EDITOR
        public static bool drawDefaultInspector;
        public MaterialEditor unityMaterialEditor;
        MaterialProperty[] _properties;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            unityMaterialEditor = materialEditor;
            _properties = properties;



            #if PEGI
              PEGI_Extensions.ResetInspectedChain();

            if (!drawDefaultInspector) {
                ef.Inspect_Material(this).RestoreBGColor();
                return;
            }

            ef.editorTypeForDefaultInspector = ef.EditorType.Material;

            pegi.toggleDefaultInspector();
#endif

            DrawDefaultInspector();

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
        protected abstract ef.EditorType EditorType { get;  }
        #endif

        public override void OnInspectorGUI() {
            #if PEGI
            
            PEGI_Extensions.ResetInspectedChain();

            if (!drawDefaultInspector) {
                Inspect(this).RestoreBGColor();
                return;
            }

            ef.editorTypeForDefaultInspector = EditorType;

            pegi.toggleDefaultInspector();
            #endif
       
            DrawDefaultInspector();
        }


    }

    public abstract class PEGI_Inspector_Mono<T> : PEGI_Inspector_Base where T : MonoBehaviour
    {
#if PEGI
        protected override ef.EditorType EditorType => ef.EditorType.Mono;

        protected override bool Inspect(Editor editor) => ef.Inspect<T>(editor);
#endif
    }

    public abstract class PEGI_Inspector_SO<T> : PEGI_Inspector_Base where T : ScriptableObject
    {
#if PEGI
        protected override ef.EditorType EditorType => ef.EditorType.ScriptableObject;

        protected override bool Inspect(Editor editor) => ef.Inspect_so<T>(editor);
#endif
    }

    [CustomEditor(typeof(PEGI_Styles))]
    public class PEGI_StylesDrawer : PEGI_Inspector_Mono<PEGI_Styles>
    {

#if   NO_PEGI
        [MenuItem("Tools/" + "PEGI" + "/Enable")]
        public static void EnablePegi() {
            UnityUtils.SetDefine("NO_PEGI", false);
        }
#else 


#if   PEGI
        [MenuItem("Tools/" + "PEGI" + "/Disable")]
        public static void DisablePegi()
        {
            UnityUtils.SetDefine("PEGI", false);
        }
#else

        [MenuItem("Tools/" + "PEGI" + "/Enable")]
        public static void EnablePegi() {
            UnityUtils.SetDefine("PEGI", true);
        }

        [MenuItem("Tools/" + "PEGI" + "/Disable")]
        public static void DisablePegi() {
            UnityUtils.SetDefine("NO_PEGI", true);
        }
#endif

#endif
    }


#endif
}

