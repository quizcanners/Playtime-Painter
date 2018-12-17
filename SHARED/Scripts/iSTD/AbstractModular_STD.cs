using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerAndEditorGUI;
using UnityEngine;

namespace SharedTools_Stuff {

    public interface ICanChangeClass {
        void Copy_NonSTDdata_From_PreviousInstance(object previous);
    }

    public interface IGotClassTag : ISTD {
        string ClassTag { get; }
        TaggedTypes_STD AllTypes { get; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public abstract class Abstract_WithTaggedTypes : Attribute
    {
        public abstract TaggedTypes_STD TaggedTypes { get; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class TaggedType : Attribute
    {

        public string tag;

        public string displayName;

        public TaggedType(string ntag, string ndisplayName = null) {
            tag = ntag;
            displayName = ndisplayName ?? tag;
        }
    }

    public class TaggedTypes_STD
    {
        readonly Type coreType;

        public TaggedTypes_STD(Type type)
        {
            coreType = type;
        }

        List<string> keys = null;

        List<Type> types = null;

        List<string> displayNames = null;

        Dictionary<string, Type> dictionary = null;

        TaggedTypes_STD RefreshNodeTypesList() {

            if (keys == null)
            {

                dictionary = new Dictionary<string, Type>();

                keys = new List<string>();

                types = new List<Type>();

                displayNames = new List<string>();

             //   Debug.Log("Getting all types of {0}".F(coreType));

                List<Type> allTypes = CsharpFuncs.GetAllChildTypes(coreType);

                foreach (var t in allTypes) {
                    var att = t.TryGetClassAttribute<TaggedType>();

                    if (att != null) {
                        if (dictionary.ContainsKey(att.tag)) 
                            Debug.LogError("Class {0} and class {1} both share the same tag {2}".F(att.displayName, dictionary[att.tag].ToString(), att.tag));
                        else
                        {
                            dictionary.Add(att.tag, t);
                            displayNames.Add(att.displayName);
                            keys.Add(att.tag);
                            types.Add(t);
                        }
                    }
                }
            }

            return this;
        }

        public Dictionary<string, Type> TaggedTypes =>
            RefreshNodeTypesList().dictionary;

        public List<string> Keys =>
                RefreshNodeTypesList().keys;

        public List<Type> Types =>
                RefreshNodeTypesList().types;

        public IEnumerator<Type> GetEnumerator()
        {
            foreach (var t in Types)
                yield return t;
        }

        public List<string> DisplayNames =>
                RefreshNodeTypesList().displayNames;

        public string Tag (Type type) {

                int ind = Types.IndexOf(type);
                if (ind >= 0)
                    return keys[ind];
            
            return null;
        }

#if PEGI
        public bool Select(ref Type type) {

            int ind = type != null ? Types.IndexOf(type) : -1;
            if (pegi.select(ref ind, DisplayNames)) {

                type = types[ind];

                return true;
            }
            return false;
        }
#endif

    }


    public static class AbstractTaggedSTDExtensions {

        public static void TryChangeObjectType(this IList list, int index, Type type, List_Data ld = null) {

            var ed = ld.TryGetElement(index);

            var previous = list.TryGet(index);

            var el = previous;

            var iTag = el as IGotClassTag;

            var std = (el as ISTD);

            if (ld._keepTypeData && iTag != null && ed != null)
                ed.ChangeType(ref el, type, iTag.GetTaggedTypes_Safe(), ld._keepTypeData);
            else  {
                el = std.TryDecodeInto<object>(type);
                STDExtensions.TryCopy_Std_AndOtherData(previous, el);
            }

            list[index] = el;

        }


        public static void Replace_IfDifferent<T>(this TaggedTypes_STD std, ref T obj, Type newType) {
            if (obj.GetType() != newType)
                obj = (T)Activator.CreateInstance(newType);
        }
        
        public static T TryGetByTag <T>(this List<T> lst, string tag) where T: IGotClassTag {

            if (lst != null && tag != null && tag.Length > 0) {
                foreach (var e in lst) {
                    if (e != null && e.ClassTag.SameAs(tag))
                        return e;
                }
            }
            return default(T);

        }
    }

}
