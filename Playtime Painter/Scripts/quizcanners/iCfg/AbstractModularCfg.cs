using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PlayerAndEditorGUI;
using UnityEngine;

namespace QuizCannersUtilities {

    #region Interfaces
    #pragma warning disable IDE0034 // Simplify 'default' expression
    #pragma warning disable IDE0019 // Use pattern matching
    #pragma warning disable IDE0018 // Inline variable declaration

    public interface ICanChangeClass {
        void OnClassTypeChange(object previousInstance);
    }

    public interface IGotClassTag : ICfg {
        string ClassTag { get; }
        TaggedTypesCfg AllTypes { get; }
    }

    #endregion

    [AttributeUsage(AttributeTargets.Class)]
    public abstract class AbstractWithTaggedTypes : Attribute
    {
        public abstract TaggedTypesCfg TaggedTypes { get; }

        public AbstractWithTaggedTypes()
        {

        }

        public AbstractWithTaggedTypes(params Type[] type)
        {
            TaggedTypes.Types = type.ToList();
            
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class TaggedType : Attribute
    {

        public string tag;

        public string displayName;

        public bool allowMultiplePerList;

        public TaggedType(string tag, string displayName = null, bool allowMultiplePerList = true) {
            this.tag = tag;
            this.displayName = displayName ?? tag;
            this.allowMultiplePerList = allowMultiplePerList;
        }

      
    }

    public class TaggedTypesCfg
    {
        private readonly Type _coreType;

        public Type CoreType => _coreType;

        public TaggedTypesCfg(Type type)
        {
            _coreType = type;
        }

        private List<string> _keys;

        public CountlessBool _disallowMultiplePerList = new CountlessBool();

        public bool CanAdd(int typeIndex, IList toList) {

            RefreshNodeTypesList();

            if (!_disallowMultiplePerList[typeIndex])
                return true;

            var t = _types[typeIndex];

            foreach (var el in toList) {
                if (el!= null && (el.GetType().Equals(t)))
                    return false;
            }

            return true;
        }

        private List<Type> _types;

        private List<string> _displayNames;

        private Dictionary<string, Type> _dictionary;

        private TaggedTypesCfg RefreshNodeTypesList()
        {
            if (_keys != null) return this;

            _dictionary = new Dictionary<string, Type>();

            _keys = new List<string>();
            
            //var atr = _coreType.TryGetClassAttribute<AbstractWithTaggedTypes>();
            
            List<Type> allTypes;

            if (_types == null)
            {
                _types = new List<Type>();
                allTypes = _coreType.GetAllChildTypes();
            }
            else
            {
                allTypes = _types;
                _types = new List<Type>();
            }

            _displayNames = new List<string>();

            int cnt = 0;

            foreach (var t in allTypes) {

                var att = t.TryGetClassAttribute<TaggedType>();

                if (att == null)
                    continue;

                if (_dictionary.ContainsKey(att.tag))
                    Debug.LogError("Class {0} and class {1} both share the same tag {2}".F(att.displayName,
                        _dictionary[att.tag].ToString(), att.tag));
                else
                {
                   

                    _dictionary.Add(att.tag, t);
                    _displayNames.Add(att.displayName);
                    _keys.Add(att.tag);
                    _types.Add(t);
                    _disallowMultiplePerList[cnt] = !att.allowMultiplePerList;
                    cnt++;
                }
            }

            return this;
        }

        public Dictionary<string, Type> TaggedTypes =>
            RefreshNodeTypesList()._dictionary;

        public List<string> Keys =>
            RefreshNodeTypesList()._keys;

        public List<Type> Types {
            get { return RefreshNodeTypesList()._types; }
            set { _types = value; }
        }

        public IEnumerator<Type> GetEnumerator() {
            foreach (var t in Types)
                yield return t;
        }

        public List<string> DisplayNames => RefreshNodeTypesList()._displayNames;

        public string Tag (Type type) {

            int ind = Types.IndexOf(type);
            if (ind >= 0)
                return _keys[ind];
            
            return null;
        }

#if !NO_PEGI
        public bool Select(ref Type type)
        {

            var changed = false;

            var ind = type != null ? Types.IndexOf(type) : -1;
            if (pegi.select(ref ind, DisplayNames).changes(ref changed)) 
                type = _types[ind];
            
            return changed;
        }
#endif

       


    }


    #region Example
    /* Implementation Template
    public class MYCLASSAttribute : AbstractWithTaggedTypes {
        public override TaggedTypesCfg TaggedTypes => MYCLASSBase.all;
    }

    [MYCLASS]
    public abstract class MYCLASSBase : AbstractKeepUnrecognizedCfg, IGotClassTag {
        public static TaggedTypesCfg all = new TaggedTypesCfg(typeof(MYCLASSBase));
        public TaggedTypesCfg AllTypes => all;
        public abstract string ClassTag { get; }
    }

    [TaggedType(Tag)]
    public class MYCLASSimplementationA : MYCLASSBase
    {
        private const string Tag = "SomeClassA";
        public override string ClassTag => Tag;
    }
    */
    #endregion

    public static class TaggedTypes {

        public static void TryChangeObjectType(IList list, int index, Type type, ListMetaData ld = null)
        {

            var previous = list.TryGetObj(index);

            var el = previous;

            var iTag = el as IGotClassTag;

            var std = (el as ICfg);

            var ed = ld.TryGetElement(index);

            if (ed != null && ld.keepTypeData && iTag != null)
                ed.ChangeType(ref el, type, iTag.GetTaggedTypes_Safe(), ld.keepTypeData);
            else
            {
                el = std.TryDecodeInto<object>(type);
                StdExtensions.TryCopy_Std_AndOtherData(previous, el);
            }

            list[index] = el;

        }
        
        public static T TryGetByTag<T> (List<T> lst, string tag) where T : IGotClassTag
        {

            if (lst == null || tag == null || tag.Length <= 0) return default(T);

            foreach (var e in lst)
            {
                if (e != null && e.ClassTag.SameAs(tag))
                    return e;
            }

            return default(T);

        }

    }


    public class TaggedModulesList<T> : AbstractCfg where T : IGotClassTag {

        public static readonly TaggedTypesCfg all = new TaggedTypesCfg(typeof(T));

        protected List<T> modules = new List<T>();

        private bool initialized = false;

        public virtual List<T> Modules {
            get {

                if (initialized)
                    return modules;

                initialized = true;

                for (var i = modules.Count - 1; i >= 0; i--)
                    if (modules[i] == null)
                        modules.RemoveAt(i);

                if (modules.Count < all.Types.Count)
                    foreach (var t in all)
                        if (!modules.ContainsInstanceOfType(t))
                            modules.Add((T)Activator.CreateInstance(t));
                
                OnInitialize();

                return modules;
            }
        }

        protected virtual void OnInitialize() { }

        [NonSerialized] private T _lastFetchedModule;

        public G GetModule<G>() where G : T {

            G returnPlug = default(G);

            if (_lastFetchedModule != null && _lastFetchedModule.GetType() == typeof(G))
                returnPlug = (G)_lastFetchedModule;
            else
                returnPlug = Modules.GetInstanceOf<G>();

            _lastFetchedModule = returnPlug;

            return returnPlug;
        }

        public override CfgEncoder Encode()  
            => new CfgEncoder()
            .Add("pgns", Modules, all);
        
        public override bool Decode(string tg, string data) {
            switch (tg) {
                case "pgns": data.Decode_List_Abstract(out modules, all); break;
                default: return true;
            }

            return false;

        }
    }

}
