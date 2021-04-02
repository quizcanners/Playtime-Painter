using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using QuizCanners.Utils;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QuizCanners.Inspect {

    public abstract class PEGI_Inspector_Material
        #if UNITY_EDITOR
        : ShaderGUI
        #endif
    {

        #if UNITY_EDITOR
        public static bool drawDefaultInspector;
        public MaterialEditor unityMaterialEditor;
        private MaterialProperty[] _properties;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            unityMaterialEditor = materialEditor;
            _properties = properties;

            if (!drawDefaultInspector) {
                ef.Inspect_Material(this).RestoreBGColor();
                return;
            }

            ef.editorTypeForDefaultInspector = ef.EditorType.Material;

            pegi.toggleDefaultInspector(materialEditor.target);

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
    
    public abstract class PEGI_UnityObjectInspector_Base  : Editor
    {
        public static Object drawDefaultInspector;
        
        protected abstract bool Inspect(Editor editor);
        internal abstract ef.EditorType EditorType { get;  }

        public override void OnInspectorGUI()
        {
            ef.inspectedUnityObject = target;
            ef.ResetInspectionTarget(target);

            if (target != drawDefaultInspector) {
                
                Inspect(this).RestoreBGColor();
              
                return;
            }

            ef.editorTypeForDefaultInspector = EditorType;

            pegi.toggleDefaultInspector(target);
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck())
            {
                target.SetToDirty();
            }
        }
        
    }

    public abstract class PEGI_Inspector_Mono<T> : PEGI_UnityObjectInspector_Base where T : MonoBehaviour
    {
        internal override ef.EditorType EditorType => ef.EditorType.Mono;

        protected override bool Inspect(Editor editor) => ef.Inspect<T>(editor);

    }

    public abstract class PEGI_Inspector_SO<T> : PEGI_UnityObjectInspector_Base where T : ScriptableObject
    {
        internal override ef.EditorType EditorType => ef.EditorType.ScriptableObject;

        protected override bool Inspect(Editor editor) => ef.Inspect_so<T>(editor);

    }

    

 // --------------------------------------------------------------------------------------------------------------------
 // <author>
 //   HiddenMonk
 //   http://answers.unity3d.com/users/496850/hiddenmonk.html
 //   
 //   Johannes Deml
 //   send@johannesdeml.com
 // </author>
 // --------------------------------------------------------------------------------------------------------------------
/// <summary>
/// Extension class for SerializedProperties
/// See also: http://answers.unity3d.com/questions/627090/convert-serializedproperty-to-custom-class.html
/// </summary>
///
/// 
    public static class SerializedPropertyExtensions {


        public static bool Inspect(this SerializedProperty prop, string name, Rect pos, GUIContent label)
        {
            var changed = false;

            var before = GUI.changed;
            EditorGUI.PropertyField(pos, prop.FindPropertyRelative(name), label);

            if (GUI.changed && !before) {
                prop.serializedObject.ApplyModifiedProperties();
                return true;
            }

            return changed;
        }


        /// <summary>
        /// Get the object the serialized property holds by using reflection
        /// </summary>
        /// <typeparam name="T">The object type that the property contains</typeparam>
        /// <param name="property"></param>
        /// <returns>Returns the object type T if it is the type the property actually contains</returns>
        public static T GetValue<T>(this SerializedProperty property) =>
            GetNestedObject<T>(property.propertyPath, property.GetRootComponent());
        

        /// <summary>
        /// Set the value of a field of the property with the type T
        /// </summary>
        /// <typeparam name="T">The type of the field that is set</typeparam>
        /// <param name="property">The serialized property that should be set</param>
        /// <param name="value">The new value for the specified property</param>
        /// <returns>Returns if the operation was successful or failed</returns>
        public static bool SetValue<T>(this SerializedProperty property, T value) {

            object obj = property.GetRootComponent();

            //Iterate to parent object of the value, necessary if it is a nested object
            string[] fieldStructure = property.propertyPath.Split('.');

            for (int i = 0; i < fieldStructure.Length - 1; i++)
                obj = GetFieldOrPropertyValue<object>(fieldStructure[i], obj);
            
            string fieldName = fieldStructure.TryGetLast();

            return SetFieldOrPropertyValue(fieldName, obj, value);

        }

        /// <summary>
        /// Get the component of a serialized property
        /// </summary>
        /// <param name="property">The property that is part of the component</param>
        /// <returns>The root component of the property</returns>
        private static Component GetRootComponent(this SerializedProperty property) =>
             (Component)property.serializedObject.targetObject;
        
        /// <summary>
        /// Iterates through objects to handle objects that are nested in the root object
        /// </summary>
        /// <typeparam name="T">The type of the nested object</typeparam>
        /// <param name="path">Path to the object through other properties e.g. PlayerInformation.Health</param>
        /// <param name="obj">The root object from which this path leads to the property</param>
        /// <param name="includeAllBases">Include base classes and interfaces as well</param>
        /// <returns>Returns the nested object casted to the type T</returns>
        private static T GetNestedObject<T>(string path, object obj, bool includeAllBases = false) {

            foreach (string part in path.Split('.'))
                obj = GetFieldOrPropertyValue<object>(part, obj, includeAllBases);
            
            return (T)obj;
        }

        private static T GetFieldOrPropertyValue<T>(string fieldName, object obj, bool includeAllBases = false, BindingFlags bindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
        {
            FieldInfo field = obj.GetType().GetField(fieldName, bindings);
            if (field != null) return (T)field.GetValue(obj);

            PropertyInfo property = obj.GetType().GetProperty(fieldName, bindings);
            if (property != null) return (T)property.GetValue(obj, null);

            if (includeAllBases) {

                foreach (Type type in GetBaseClassesAndInterfaces(obj.GetType())) {

                    field = type.GetField(fieldName, bindings);
                    if (field != null) return (T)field.GetValue(obj);

                    property = type.GetProperty(fieldName, bindings);
                    if (property != null) return (T)property.GetValue(obj, null);
                }
            }

            return default;
        }

        public static bool SetFieldOrPropertyValue(string fieldName, object obj, object value, bool includeAllBases = false, BindingFlags bindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
        {
            FieldInfo field = obj.GetType().GetField(fieldName, bindings);
            if (field != null)
            {
                field.SetValue(obj, value);
                return true;
            }

            PropertyInfo property = obj.GetType().GetProperty(fieldName, bindings);
            if (property != null)
            {
                property.SetValue(obj, value, null);
                return true;
            }

            if (includeAllBases)
            {
                foreach (Type type in GetBaseClassesAndInterfaces(obj.GetType()))
                {
                    field = type.GetField(fieldName, bindings);
                    if (field != null)
                    {
                        field.SetValue(obj, value);
                        return true;
                    }

                    property = type.GetProperty(fieldName, bindings);
                    if (property != null)
                    {
                        property.SetValue(obj, value, null);
                        return true;
                    }
                }
            }
            return false;
        }

        public static IEnumerable<Type> GetBaseClassesAndInterfaces(this Type type, bool includeSelf = false)
        {
            List<Type> allTypes = new List<Type>();

            if (includeSelf) allTypes.Add(type);
            
            allTypes.AddRange(

                (type.BaseType == typeof(object)) ? 
                    type.GetInterfaces() :
                     Enumerable
                    .Repeat(type.BaseType, 1)
                    .Concat(type.GetInterfaces())
                    .Concat(type.BaseType.GetBaseClassesAndInterfaces())
                    .Distinct()
                    
                    );
            

            return allTypes;
        }
    
    }

#endif
            }

