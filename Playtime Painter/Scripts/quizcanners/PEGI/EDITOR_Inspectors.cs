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



            #if !NO_PEGI
              pegi.ResetInspectedChain();

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




#if !NO_PEGI
        public abstract bool Inspect(Material mat);
#endif

    }



#if UNITY_EDITOR

    //[CustomPropertyDrawer(typeof(Ingredient))]
    // Work in progress...
    public class PEGI_PropertyDrawer<T> : PropertyDrawer where T : class {

        private T GetActualObjectForSerializedProperty(FieldInfo fieldInfo, SerializedProperty property) 
        {
            var obj = fieldInfo.GetValue(property.serializedObject.targetObject);
            if (obj == null) { return null; }

            T actualObject = null;
            if (obj.GetType().IsArray)
            {
                var index = Convert.ToInt32(new string(property.propertyPath.Where(c => char.IsDigit(c)).ToArray()));
                actualObject = ((T[])obj)[index];
            }
            else
            {
                actualObject = obj as T;
            }
            return actualObject;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            T obj = GetActualObjectForSerializedProperty(fieldInfo, property);

           // ef.Inspect_Prop(obj, property);
            
            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }
    }

    public abstract class PEGI_Inspector_Base  : Editor
    {
        public static bool drawDefaultInspector;

        #if !NO_PEGI
        protected abstract bool Inspect(Editor editor);
        protected abstract ef.EditorType EditorType { get;  }
        #endif

        public override void OnInspectorGUI() {
            #if !NO_PEGI
            
            pegi.ResetInspectedChain();

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
#if !NO_PEGI
        protected override ef.EditorType EditorType => ef.EditorType.Mono;

        protected override bool Inspect(Editor editor) => ef.Inspect<T>(editor);
#endif
    }

    public abstract class PEGI_Inspector_SO<T> : PEGI_Inspector_Base where T : ScriptableObject
    {
#if !NO_PEGI
        protected override ef.EditorType EditorType => ef.EditorType.ScriptableObject;

        protected override bool Inspect(Editor editor) => ef.Inspect_so<T>(editor);
#endif
    }

    [CustomEditor(typeof(PEGI_Styles))]
    public class PEGI_StylesDrawer : PEGI_Inspector_Mono<PEGI_Styles> {
        
        #if   NO_PEGI
            [MenuItem("Tools/" + "PEGI" + "/Enable")]
            public static void EnablePegi() {
                UnityUtils.SetDefine("NO_PEGI", false);
            }
        #else 
        
            #if   PEGI
                [MenuItem("Tools/" + "PEGI" + "/Disable")]
                public static void DisablePegi() => UnityUtils.SetDefine("PEGI", false);
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

