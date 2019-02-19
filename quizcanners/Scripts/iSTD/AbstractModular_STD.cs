using System;
using System.Collections;
using System.Collections.Generic;
using PlayerAndEditorGUI;
using UnityEngine;

namespace QuizCannersUtilities {

    public interface ICanChangeClass {
        void Copy_NonStdData_From_PreviousInstance(object previous);
    }

    public interface IGotClassTag : IStd {
        string ClassTag { get; }
        TaggedTypesStd AllTypes { get; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public abstract class AbstractWithTaggedTypes : Attribute
    {
        public abstract TaggedTypesStd TaggedTypes { get; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class TaggedType : Attribute
    {

        public string tag;

        public string displayName;

        public TaggedType(string nTag, string nDisplayName = null) {
            tag = nTag;
            displayName = nDisplayName ?? tag;
        }
    }

    public class TaggedTypesStd
    {
        private readonly Type _coreType;

        public TaggedTypesStd(Type type)
        {
            _coreType = type;
        }

        private List<string> _keys;

        private List<Type> _types;

        private List<string> _displayNames;

        private Dictionary<string, Type> _dictionary;

        private TaggedTypesStd RefreshNodeTypesList() {
            if (_keys != null) return this;
            
            _dictionary = new Dictionary<string, Type>();

            _keys = new List<string>();

            _types = new List<Type>();

            _displayNames = new List<string>();

            //   Debug.Log("Getting all types of {0}".F(coreType));

            var allTypes = _coreType.GetAllChildTypes();

            foreach (var t in allTypes) {
                var att = t.TryGetClassAttribute<TaggedType>();

                if (att == null) continue;
                
                if (_dictionary.ContainsKey(att.tag)) 
                    Debug.LogError("Class {0} and class {1} both share the same tag {2}".F(att.displayName, _dictionary[att.tag].ToString(), att.tag));
                else
                {
                    _dictionary.Add(att.tag, t);
                    _displayNames.Add(att.displayName);
                    _keys.Add(att.tag);
                    _types.Add(t);
                }
            }

            return this;
        }

        public Dictionary<string, Type> TaggedTypes =>
            RefreshNodeTypesList()._dictionary;

        public List<string> Keys =>
                RefreshNodeTypesList()._keys;

        public List<Type> Types =>
                RefreshNodeTypesList()._types;

        public IEnumerator<Type> GetEnumerator()
        {
            foreach (var t in Types)
                yield return t;
        }

        public List<string> DisplayNames =>
                RefreshNodeTypesList()._displayNames;

        public string Tag (Type type) {

                int ind = Types.IndexOf(type);
                if (ind >= 0)
                    return _keys[ind];
            
            return null;
        }

#if PEGI
        public bool Select(ref Type type) {

            var ind = type != null ? Types.IndexOf(type) : -1;
            if (pegi.select(ref ind, DisplayNames)) {

                type = _types[ind];

                return true;
            }
            return false;
        }
#endif

    }


    public static class AbstractTaggedStdExtensions {

        public static void TryChangeObjectType(this IList list, int index, Type type, ListMetaData ld = null) {

          

            var previous = list.TryGet(index);

            var el = previous;

            var iTag = el as IGotClassTag;

            var std = (el as IStd);

            var ed = ld.TryGetElement(index);
            
            if (ed != null && ld.keepTypeData && iTag != null)
                ed.ChangeType(ref el, type, iTag.GetTaggedTypes_Safe(), ld.keepTypeData);
            else  {
                el = std.TryDecodeInto<object>(type);
                StdExtensions.TryCopy_Std_AndOtherData(previous, el);
            }

            list[index] = el;

        }


        public static void Replace_IfDifferent<T>(this TaggedTypesStd std, ref T obj, Type newType) {
            if (obj.GetType() != newType)
                obj = (T)Activator.CreateInstance(newType);
        }
        
        public static T TryGetByTag <T>(this List<T> lst, string tag) where T: IGotClassTag {
            
            if (lst == null || tag == null || tag.Length <= 0) return default(T);
            
            foreach (var e in lst) {
                if (e != null && e.ClassTag.SameAs(tag))
                    return e;
            }
            
            return default(T);

        }
    }

}
