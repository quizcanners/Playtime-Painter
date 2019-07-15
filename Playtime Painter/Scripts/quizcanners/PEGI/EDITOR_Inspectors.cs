using UnityEngine;
using QuizCannersUtilities;
using System;
using System.Linq;
using System.Reflection;

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
            
              pegi.ResetInspectedChain();

            if (!drawDefaultInspector) {
                ef.Inspect_Material(this).RestoreBGColor();
                return;
            }

            ef.editorTypeForDefaultInspector = ef.EditorType.Material;

            pegi.toggleDefaultInspector();

            DrawDefaultInspector();

        }

        #endif

        public void DrawDefaultInspector()
        #if UNITY_EDITOR
            => base.OnGUI(unityMaterialEditor, _properties);
        #else
            {}
        #endif
        
        public abstract bool Inspect(Material mat);

    }



#if UNITY_EDITOR
    
    public abstract class PEGI_Inspector_Base  : Editor
    {
        public static bool drawDefaultInspector;
        
        protected abstract bool Inspect(Editor editor);
        protected abstract ef.EditorType EditorType { get;  }
  
        public override void OnInspectorGUI() {

            pegi.ResetInspectedChain();

            if (!drawDefaultInspector) {
                Inspect(this).RestoreBGColor();
                return;
            }

            ef.editorTypeForDefaultInspector = EditorType;

            pegi.toggleDefaultInspector();
          
       
            DrawDefaultInspector();
        }


    }

    public abstract class PEGI_Inspector_Mono<T> : PEGI_Inspector_Base where T : MonoBehaviour
    {
        protected override ef.EditorType EditorType => ef.EditorType.Mono;

        protected override bool Inspect(Editor editor) => ef.Inspect<T>(editor);

    }

    public abstract class PEGI_Inspector_SO<T> : PEGI_Inspector_Base where T : ScriptableObject
    {
        protected override ef.EditorType EditorType => ef.EditorType.ScriptableObject;

        protected override bool Inspect(Editor editor) => ef.Inspect_so<T>(editor);

    }

    [CustomEditor(typeof(PEGI_Styles))]
    public class PEGI_StylesDrawer : PEGI_Inspector_Mono<PEGI_Styles> { }
    
#endif
}

