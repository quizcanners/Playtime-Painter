﻿using System;
using System.Collections;
using System.Collections.Generic;
using QuizCanners.Inspect;
using QuizCanners.Utils;

namespace QuizCanners.Migration {

    #region Interfaces
    #pragma warning disable IDE0019 // Use pattern matching
    #pragma warning disable IDE0018 // Inline variable declaration

    public interface ICanChangeClass {
        void OnClassTypeChange(object previousInstance);
    }

    public interface IGotClassTag {
        string ClassTag { get; }
    }

    #endregion
    
    [AttributeUsage(AttributeTargets.Class)]
    public class TaggedType : UnityEngine.Scripting.PreserveAttribute
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
        public static Dictionary<Type, TaggedTypesCfg> _configs = new Dictionary<Type, TaggedTypesCfg>();

        public static TaggedTypesCfg TryGetOrCreate(Type type)
        {
            if (!typeof(IGotClassTag).IsAssignableFrom(type))
            {
                return null;
            }

            TaggedTypesCfg cfg;

            if (_configs.TryGetValue(type, out cfg))
                return cfg;

            cfg = new TaggedTypesCfg(type);

            _configs[type] = cfg;

            return cfg;
        }

        public Type CoreType { get; private set; } 

        public TaggedTypesCfg(Type type)
        {
            CoreType = type;
            _configs[type] = this;
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
            
            List<Type> allTypes;

            if (_types == null)
            {
                _types = new List<Type>();
                allTypes = CoreType.GetAllChildTypes();
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
                    UnityEngine.Debug.LogError("Class {0} and class {1} both share the same tag {2}".F(att.displayName,
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

        public string GetTag (Type type) {

            int ind = Types.IndexOf(type);
            if (ind >= 0)
                return _keys[ind];
            
            return null;
        }
        
        public bool Inspect_Select(ref Type type)
        {
            var changed = pegi.ChangeTrackStart();

            var ind = type != null ? Types.IndexOf(type) : -1;
            if (pegi.select(ref ind, DisplayNames)) 
                type = _types[ind];
            
            return changed;
        }

    }
    
    public static class TaggedTypes {

        public static void TryChangeObjectType(IList list, int index, Type type, CollectionMetaData ld = null)
        {
            object previous = null;

            if (list != null && index >= 0 && index < list.Count)
                previous = list[index];
            
            var el = previous;

            var iTag = el as IGotClassTag;

            var std = (el as ICfgCustom);

            var ed = ld.TryGetElement(index);

            if (ed != null && iTag != null)
                ed.ChangeType(ref el, type);
            else
            {
                el = std.TryDecodeInto<object>(type);
                ICfgExtensions.TryCopy_Std_AndOtherData(previous, el);
            }

            list[index] = el;

        }
        
        public static T TryGetByTag<T> (List<T> lst, string tag) where T : IGotClassTag
        {
            if (lst == null || tag == null || tag.Length <= 0) return default;

            foreach (var e in lst)
            {
                if (e != null && e.ClassTag.SameAs(tag))
                    return e;
            }

            return default;
        }
    }
    
    public class TaggedModulesList<T> : ICfg, IPEGI, IEnumerable<T> where T : class, IGotClassTag, ICfg {
        
        protected List<T> modules = new List<T>();
        
        protected virtual List<T> Modules {
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
        
        public IEnumerator<T> GetEnumerator() => Modules.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Modules.GetEnumerator();

        private bool initialized;

        public G GetModule<G>() where G : class, T {

            G returnPlug = null;

            var targetType = typeof(G);
            
            foreach (var i in Modules)
                if (i.GetType() == targetType)
                {
                    returnPlug = (G)i;
                    break;
                }

            return returnPlug;
        }
        
        #region Encode & Decode
        public static readonly TaggedTypesCfg all = new TaggedTypesCfg(typeof(T));
        
        public CfgEncoder Encode()  
            => new CfgEncoder()
            .Add("pgns", Modules, all);
        
        public void Decode(string key, CfgData data) {
            switch (key) {
                case "pgns": data.ToList(out modules, all);
                    OnInitialize();
                    break;
            }
        }

        #endregion

        public virtual void OnInitialize() { }

        #region Inspector
        
        private readonly CollectionMetaData modulesMeta = new CollectionMetaData("Modules", allowDeleting: false, showAddButton:false, allowReordering: false, showEditListButton:false);

        public void Inspect()
        {
            modulesMeta.edit_List(modules).nl();

        }

        #endregion
    }

}
