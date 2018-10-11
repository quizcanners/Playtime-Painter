using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerAndEditorGUI;
using UnityEngine;

namespace SharedTools_Stuff {

    public interface IGotClassTag : ISTD {
        string ClassTag { get; }
        TaggedTypes_STD AllTypes { get; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public abstract class TaggedTypeHolder : Attribute
    {
        public abstract TaggedTypes_STD TaggedTypes { get; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class TaggedType : Attribute
    {

        public string tag;

        public string displayName;

        public TaggedType(string ntag, string ndisplayName = null)
        {
            tag = ntag;
            displayName = ndisplayName ?? tag;
        }
    }

    public class TaggedTypes_STD
    {
        Type coreType;

        public TaggedTypes_STD(Type type)
        {
            coreType = type;
        }

        List<string> keys = null;

        List<string> displayNames = null;

        Dictionary<string, Type> _types = null;

        void RefreshNodeTypesList()
        {
            _types = new Dictionary<string, Type>();

            keys = new List<string>();

            displayNames = new List<string>();

            Debug.Log("Getting all types of {0}".F(coreType));

            List<Type> allTypes = CsharpFuncs.GetAllChildTypes(coreType);

            foreach (var t in allTypes) {
                var att = t.TryGetClassAttribute<TaggedType>();

                if (att != null && att.tag != null)
                {
                    displayNames.Add(att.displayName);
                    keys.Add(att.tag);
                    _types.Add(att.tag, t);
                }
            }
        }

        public Dictionary<string, Type> Types
        {
            get
            {

                if (_types == null)
                    RefreshNodeTypesList();

                return _types;
            }
        }

        public List<string> Keys
        {
            get
            {

                if (keys == null)
                    RefreshNodeTypesList();

                return keys;
            }
        }

        public List<string> DisplayNames
        {
            get
            {

                if (displayNames == null)
                    RefreshNodeTypesList();

                return displayNames;
            }
        }

    }

    public static class TaggedClassesExtensions {

        public static StdEncoder Add_Abstract(this StdEncoder cody, string tag, IGotClassTag typeTag) {
            if (typeTag != null) {
                var sub = new StdEncoder().Add(typeTag.ClassTag, typeTag.Encode());
                cody.Add(tag, sub);
            }
            
            return cody;
        }

        public static void DecodeInto<T>(this string data, out T val, TaggedTypes_STD typeList) where T: IGotClassTag {

            val = default(T);

            var cody = new StdDecoder(data);

            var type = typeList.Types.TryGet(cody.GetTag());

            if (type != null)
                val = cody.GetData().DecodeInto_Type<T>(type);
        }

    }

}
