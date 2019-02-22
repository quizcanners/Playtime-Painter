using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;

namespace QuizCannersUtilities {

    #region Interfaces

    public interface IStd {
        StdEncoder Encode(); 
        void Decode(string data);
        bool Decode(string tg, string data);
    }

    public interface IKeepUnrecognizedStd : IStd
    {
        UnrecognizedTagsList UnrecognizedStd { get; }
    }

    public interface ICanBeDefaultStd : IStd {
        bool IsDefault { get; }
    }

    public interface IStdSerializeNestedReferences
    {
        int GetReferenceIndex(UnityEngine.Object obj);
        T GetReferenced<T>(int index) where T: UnityEngine.Object;
    }

    public interface IStdSafeEncoding: IStd
    {
        LoopLock GetLoopLock { get;  }
    }

    public interface IKeepMyStd : IStd
    {
        string ConfigStd { get; set; }
    }


    #endregion

    #region EnumeratedTypeList

    [AttributeUsage(AttributeTargets.Class)]
    public class DerivedListAttribute : Attribute {
        public readonly List<Type> derivedTypes;
        public DerivedListAttribute(params Type[] types) {
            derivedTypes = new List<Type>(types);
        }
    }
    #endregion

    #region UNRECOGNIZED

    public class UnrecognizedTagsList : IPEGI
    {

        public class UnrecognizedElement : IPEGI, IGotName, IGotCount, IPEGI_ListInspect
        {
            public string tag;
            public string data;

            public List<UnrecognizedElement> elements = new List<UnrecognizedElement>();

            public UnrecognizedElement() { }

            public UnrecognizedElement(string nTag, string nData)
            {
                tag = nTag;
                data = nData;
            }

            public UnrecognizedElement(List<string> tags, string nData)
            {
                Add(tags, nData);
            }

            public void Add(List<string> tags, string nData)
            {
                if (tags.Count > 1)
                {
                    tag = tags[0];
                    elements.Add(tags.GetRange(1, tags.Count - 1), nData);
                }
                else
                {
                    tag = tags[0];
                    data = nData;
                }
            }

            public string NameForPEGI { get { return tag; } set { tag = value; } }


            #region Inspector
#if PEGI
            public int CountForInspector => elements.Count == 0 ? 1 : elements.CountForInspector();
            
            int _inspected = -1;
            public bool Inspect() => "{0} Sub Tags".F(tag).edit_List(ref elements, ref _inspected);

            public bool PEGI_inList(IList list, int ind, ref int edited)
            {
                var changed = false;

                changed |= pegi.edit(ref tag, 70);

                if (elements.Count == 0)
                    changed |= pegi.edit(ref data);
                else
                {
                    "+[{0}]".F(CountForInspector).write(50);
                    if (icon.Enter.Click())
                        edited = ind;
                }
                return changed;
            }
#endif
            #endregion
        }

        private List<UnrecognizedElement> _elements = new List<UnrecognizedElement>();

        public bool locked = false;

        public void Clear() => _elements = new List<UnrecognizedElement>();

        public void Add(string tag, string data)
        {
            var existing = _elements.GetByIGotName(tag);

            if (existing != null)
                existing.data = data;
            else
                _elements.Add(tag, data);
        }

        public void Add(List<string> tags, string data) => _elements.Add(tags, data);

        public StdEncoder Encode() => locked ? new StdEncoder() : _elements.Encode().Lock(this);

        #if PEGI
    
        public int Count => _elements.CountForInspector();
    
    
        private int _inspected = -1;
        public bool Inspect()
        {
            var changed = false;
    
            pegi.nl();
    
            "Unrecognized".edit_List(ref _elements, ref _inspected).nl(ref changed);
    
            pegi.nl();
    
            return changed;
        }
        #endif

    }


    #endregion

    #region Abstract Implementations

    public class StdSimpleReferenceHolder : IStdSerializeNestedReferences {
        
        public readonly List<UnityEngine.Object> nestedReferences = new List<UnityEngine.Object>();
        public int GetReferenceIndex(UnityEngine.Object obj) => nestedReferences.TryGetIndexOrAdd(obj);

        public T GetReferenced<T>(int index) where T : UnityEngine.Object => nestedReferences.TryGet(index) as T;

    }

    public class StdReferencesHolder : ScriptableObject, IStdSerializeNestedReferences, IPEGI, IKeepUnrecognizedStd, IStdSafeEncoding
    {
        
        #region Encode & Decode

        public UnrecognizedTagsList UnrecognizedStd { get; } = new UnrecognizedTagsList();

        public LoopLock GetLoopLock { get; } = new LoopLock();

        private readonly ListMetaData _listMetaData = new ListMetaData("References");

        [SerializeField] protected List<UnityEngine.Object> nestedReferences = new List<UnityEngine.Object>();
        public virtual int GetReferenceIndex(UnityEngine.Object obj) => nestedReferences.TryGetIndexOrAdd(obj);

        public virtual T GetReferenced<T>(int index) where T : UnityEngine.Object => nestedReferences.TryGet(index) as T;


        public virtual StdEncoder Encode() => this.EncodeUnrecognized()
            .Add("listDta", _listMetaData);

        public virtual void Decode(string data) => data.DecodeTagsFor(this);

        public virtual bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "listDta": _listMetaData.Decode(data); break;
                default: return false;
            }
            return true;
        }
        #endregion

        public StdExplorerData explorer = new StdExplorerData();

        #region Inspector
        #if PEGI

        [ContextMenu("Reset Inspector")] // Because ContextMenu doesn't accepts overrides
        private void Reset() => ResetInspector();

        public virtual void ResetInspector()
        {
            _inspectedDebugStuff = -1;
            inspectedReference = -1;
            inspectedStuff = -1;
        }

        [HideInInspector]
        [SerializeField] public int inspectedStuff = -1;
        private int _inspectedDebugStuff = -1;
        [SerializeField] private int inspectedReference = -1;
        public virtual bool Inspect()
        {

            var changed = false;

            if (!icon.Debug.enter(ref inspectedStuff, 0)) return false;
            
            if (icon.Refresh.Click("Reset Inspector"))
                ResetInspector();

            this.ClickHighlight();

            if (inspectedStuff == -1)
                pegi.nl();

            if ("STD Saves: ".AddCount(explorer).enter(ref _inspectedDebugStuff, 0).nl_ifNotEntered())
                explorer.Inspect(this);

            if (inspectedStuff == -1)
                pegi.nl();

            if (("Object References: " + nestedReferences.Count).enter(ref _inspectedDebugStuff, 1).nl_ifNotEntered())
            {
                _listMetaData.edit_List_UObj(ref nestedReferences);

                if (inspectedReference == -1 && "Clear All References".Click("Will clear the list. Make sure everything" +
                    ", that usu this object to hold references is currently decoded to avoid mixups"))
                    nestedReferences.Clear();

            }

            if (inspectedStuff == -1)
                pegi.nl();

            if (("Unrecognized Tags: " + UnrecognizedStd.Count).enter(ref _inspectedDebugStuff, 2).nl_ifNotEntered())
                changed |= UnrecognizedStd.Nested_Inspect();

            if (inspectedStuff == -1)
                pegi.nl();

            
            return changed;
        }

        #endif
        #endregion
    }


    public abstract class AbstractStd : IStdSafeEncoding, ICanBeDefaultStd {
        public abstract StdEncoder Encode();
        public virtual void Decode(string data) => data.DecodeTagsFor(this);
        public abstract bool Decode(string tg, string data);

        public LoopLock GetLoopLock { get; } = new LoopLock();

        public virtual bool IsDefault => false;
    }

    public abstract class AbstractKeepUnrecognizedStd : AbstractStd, IKeepUnrecognizedStd {
        public UnrecognizedTagsList UnrecognizedStd { get; } = new UnrecognizedTagsList();

#if !UNITY_EDITOR
        [NonSerialized]
        #endif
        private readonly StdExplorerData _explorer = new StdExplorerData();
        
        public override StdEncoder Encode() => this.EncodeUnrecognized();

        public override bool Decode(string tg, string data) => false;
        #region Inspector

        public virtual void ResetInspector() {
            inspectedStuff = -1;
        }

        public int inspectedStuff = -1;

        #if PEGI
        public virtual bool Inspect() {
            var changed = false;

            if (icon.Debug.enter(ref inspectedStuff, 0)) {
                if (icon.Refresh.Click("Reset Inspector"))
                    ResetInspector();
                this.CopyPasteStdPegi().nl(ref changed);

                _explorer.Inspect(this);
                changed |= UnrecognizedStd.Nested_Inspect();
            }

            return changed;
        }
        #endif
        #endregion
    }

    public abstract class ComponentStd : MonoBehaviour, IStdSafeEncoding, IKeepUnrecognizedStd, ICanBeDefaultStd, IStdSerializeNestedReferences, IPEGI, IPEGI_ListInspect, IGotName, INeedAttention {

#if !UNITY_EDITOR
        [NonSerialized]
#endif
        public StdExplorerData explorer = new StdExplorerData();

        #region Inspector

        public virtual string NameForPEGI
        {
            get
            {
                return gameObject.name;
            }

            set
            {
                gameObject.RenameAsset(value);
            }
        }

        #if PEGI
        [ContextMenu("Reset Inspector")]
        private void Reset() => ResetInspector();

        protected virtual void ResetInspector()
        {
            _inspectedDebugStuff = -1;
            inspectedStuff = -1;
        }

        public virtual string NeedAttention() => null;
        
        public virtual bool PEGI_inList(IList list, int ind, ref int edited)
        {
            var changed = false;
            var n = gameObject.name;
            if ((pegi.editDelayed(ref n) && n.Length > 0).changes(ref changed))
                gameObject.name = n;
            
            if (this.Click_Attention_Highlight())
                edited = ind;
            

            return changed;
        }
        [HideInInspector]
        [SerializeField] public int inspectedStuff = -1;

        private int _inspectedDebugStuff = -1;
        public virtual bool Inspect() {

            var changed = false;

            if (inspectedStuff == -1)
                pegi.Lock_UnlockWindowClick(gameObject);

            if (!icon.Debug.enter(ref inspectedStuff, 0).changes(ref changed)) return changed; 
                
            if (icon.Refresh.Click("Reset Inspector"))
                ResetInspector();

            this.CopyPasteStdPegi().nl(ref changed);

            pegi.toggleDefaultInspector().nl();
            
            "{0} Debug ".F(this.ToPegiString()).nl();

            if (("STD Saves: " + explorer.states.Count).enter(ref _inspectedDebugStuff, 0).nl_ifNotEntered())
                explorer.Inspect(this);

            if (inspectedStuff == -1)
                pegi.nl();

            if (("Object References: " + nestedReferences.Count).enter(ref _inspectedDebugStuff, 1).nl_ifNotEntered())
            {
                referencesMeta.edit_List_UObj(ref nestedReferences);
                if (!referencesMeta.Inspecting && "Clear All References".Click("Will clear the list. Make sure everything" +
                ", that usu this object to hold references is currently decoded to avoid mixups"))
                    nestedReferences.Clear();

            }

            if (inspectedStuff == -1)
                pegi.nl();

            if (("Unrecognized Tags: " + UnrecognizedStd.Count).enter(ref _inspectedDebugStuff, 2).nl_ifNotEntered())
                changed |= UnrecognizedStd.Nested_Inspect();

            if (inspectedStuff == -1)
                pegi.nl();
            
           
            return changed;
        }
        #endif
        #endregion

        #region Encoding & Decoding

        public LoopLock GetLoopLock { get; } = new LoopLock();

        public virtual bool IsDefault => false;

        protected readonly ListMetaData referencesMeta = new ListMetaData("References");

        [HideInInspector]
        [SerializeField] protected List<UnityEngine.Object> nestedReferences = new List<UnityEngine.Object>();
        public int GetReferenceIndex(UnityEngine.Object obj) => nestedReferences.TryGetIndexOrAdd(obj);
        
        public T GetReferenced<T>(int index) where T : UnityEngine.Object => nestedReferences.TryGet(index) as T;

        public UnrecognizedTagsList UnrecognizedStd { get; } = new UnrecognizedTagsList();

        public virtual bool Decode(string tg, string data)
        {
            switch (tg) {
            #if PEGI
                case "db": inspectedStuff = data.ToInt(); break;
            #endif
                default: return false;
            }
            return true;
        }

        public virtual StdEncoder Encode() => this.EncodeUnrecognized()
#if PEGI
            .Add_IfNotNegative("db", inspectedStuff)
#endif
            ;

        public virtual void Decode(string data) {
            UnrecognizedStd.Clear();
            data.DecodeTagsFor(this);
        }

#endregion
    }

#endregion

#region Extensions
    public static class StdExtensions {

        private const string StdStart = "<-<-<";
        private const string StdEnd = ">->->";

        public static void EmailData(this IStd std, string subject, string note)
        {
            if (std == null) return;

            UnityHelperFunctions.SendEmail ( "somebody@gmail.com", subject, 
                "{0} {1} Copy this entire email (or only stuff below) and paste it in the corresponding field on your side to paste it (don't change data before pasting it). {2} {3}{4}{5}".F(note, pegi.EnvironmentNl, pegi.EnvironmentNl,
                StdStart,  std.Encode().ToString(), StdEnd ) ) ;
        }

        public static void DecodeFromExternal(this IStd std, string data) => std?.Decode(ClearFromExternal(data));
        
        private static string ClearFromExternal(string data) {

            if (!data.Contains(StdStart)) return data;
            
            var start = data.IndexOf(StdStart, StringComparison.Ordinal) + StdStart.Length;
            var end = data.IndexOf(StdEnd, StringComparison.Ordinal);

            data = data.Substring(start, end - start);
                
            return data;

        }

#if PEGI
        private static IStd _toCopy;

        public static bool CopyPasteStdPegi(this IStd std) {
            
            if (std == null) return false;
            
            var changed = false;
            
            if (_toCopy == null && icon.Copy.Click("Copy {0}".F(std.ToPegiString())).changes(ref changed))
                _toCopy = std;

            if (_toCopy == null) return changed;
            
            if (icon.Close.Click("Empty copy buffer"))
                _toCopy = null;
            else if (!Equals(std, _toCopy) && icon.Paste.Click("Copy {0} into {1}".F(_toCopy, std)))
                TryCopy_Std_AndOtherData(_toCopy, std);
                  
            return changed;
        }

        public static bool SendReceivePegi(this IStd std, string name, string folderName, out string data) {
  
            if (icon.Email.Click("Send {0} to somebody via email.".F(folderName)))
                std.EmailData(name, "Use this {0}".F(name));

            data = "";
            if (pegi.edit(ref data).Unfocus()) {
                data = ClearFromExternal(data);
                return true;
            }

            if (icon.Folder.Click("Save {0} to the file".F(name))) {
                std.SaveToAssets(folderName, name);
                UnityHelperFunctions.RefreshAssetDatabase();
            }

            if (LoadOnDrop(out data))
                return true;


            pegi.nl();

            return false;
        }
#endif

        private static readonly StdSimpleReferenceHolder TmpHolder = new StdSimpleReferenceHolder();

        public static void TryCopy_Std_AndOtherData(object from, object into)
        {
            if (into == null || into == from) return;
            
            var intoStd = into as IStd;
            
            if (intoStd != null)
            {
                var fromStd = from as IStd;

                if (fromStd != null)
                {
                    var prev = StdEncoder.keeper;
                    StdEncoder.keeper = TmpHolder;
                    intoStd.Decode(fromStd.Encode().ToString());
                    StdEncoder.keeper = prev;

                    TmpHolder.nestedReferences.Clear();
                }


            }

            var ch = into as ICanChangeClass;
            if (ch != null && !from.IsNullOrDestroyed_Obj())
                ch.Copy_NonStdData_From_PreviousInstance(from);

            
        }


        public static void Add (this List<UnrecognizedTagsList.UnrecognizedElement> lst, List<string> tags, string data) {

            var existing = lst.GetByIGotName(tags[0]);
            if (existing != null)
                existing.Add(tags, data);
            else
                lst.Add(new UnrecognizedTagsList.UnrecognizedElement(tags, data));
        }

        public static void Add(this List<UnrecognizedTagsList.UnrecognizedElement> lst, string tag, string data)
            =>  lst.Add(new UnrecognizedTagsList.UnrecognizedElement(tag, data));

        public static StdEncoder Encode(this IEnumerable<UnrecognizedTagsList.UnrecognizedElement> lst) {
            var cody = new StdEncoder();
            foreach (var e in lst) {
                if (e.elements.Count == 0)
                    cody.Add_String(e.tag, e.data);
                else
                    cody.Add(e.tag, e.elements.Encode());
            }

            return cody;
        }

        public static TaggedTypesStd GetTaggedTypes_Safe<T>(this T obj) where T : IGotClassTag => obj != null ? obj.AllTypes : typeof(T).TryGetTaggedClasses();
        
        public static TaggedTypesStd TryGetTaggedClasses(this Type type)
        {

            if (!typeof(IGotClassTag).IsAssignableFrom(type)) return null;

            var attrs = type.GetCustomAttributes(typeof(AbstractWithTaggedTypes), true);

            if (!attrs.IsNullOrEmpty()) 
                return (attrs[0] as AbstractWithTaggedTypes).TaggedTypes;
            
            if (Debug.isDebugBuild)
                Debug.Log("{0} does not have Abstract_WithTaggedTypes Attribute");
            
            return null;
        }

        public static List<Type> TryGetDerivedClasses (this Type t) => t.TryGetClassAttribute<DerivedListAttribute>()?.derivedTypes.NullIfEmpty();
            

        public static string copyBufferValue;
        public static string copyBufferTag;

        public static bool LoadOnDrop(out string txt) {

            txt = null;
#if PEGI
            UnityEngine.Object myType = null;
            if (pegi.edit(ref myType)) {
                txt = StuffLoader.LoadTextAsset(myType);
                ("Loaded " + myType.name).showNotificationIn3D_Views();

                return true;
            }
#endif
            return false;
        }

        public static bool LoadOnDrop<T>(this T obj) where T: IStd
        {
            string txt;
            if (LoadOnDrop(out txt)) {
                obj.Decode(txt);
                return true;
            }

            return false;
        }

        public static void UpdatePrefab (this IStd s, GameObject go) {
            var iK = s as IKeepMyStd;

            if (iK != null)
                iK.SaveStdData();

            go.UpdatePrefab();
        }

        public static void SaveStdData(this IKeepMyStd s) {
            if (s != null)
                s.ConfigStd = s.Encode().ToString();
            
        }

        public static bool LoadStdData(this IKeepMyStd s)
        {
            if (s == null)
                return false;

            s.Decode(s.ConfigStd);

            return true;
        }

        public static T LoadFromAssets<T>(this T s, string fullPath, string name) where T:IStd, new() {
			if (s == null)
				s = new T ();
            s.Decode(StuffLoader.LoadStoryFromAssets(fullPath, name));
			return s;
        }

        public static IStd SaveToAssets(this IStd s, string path, string filename)
        {
            StuffSaver.Save_ToAssets_ByRelativePath(path, filename, s.Encode().ToString());
            return s;
        }

        public static IStd SaveToPersistentPath(this IStd s, string path, string filename)
        {
            StuffSaver.SaveToPersistentPath(path, filename, s.Encode().ToString());
            return s;
        }

        public static bool LoadFromPersistentPath(this IStd s, string path, string filename)
        {
            var data = StuffLoader.LoadFromPersistentPath(path, filename);
            if (data != null)
            {
                s.Decode(data);
                return true;
            }
            return false;
        }

        public static IStd SaveToResources(this IStd s, string resFolderPath, string insideResPath, string filename)
        {
            StuffSaver.SaveToResources(resFolderPath, insideResPath, filename, s.Encode().ToString());
            return s;
        }

        public static T CloneStd<T>(this T obj, IStdSerializeNestedReferences nested = null) where T : IStd
        {

            if (obj.IsNullOrDestroyed_Obj()) return default(T);
            
            var ret = (T)Activator.CreateInstance(obj.GetType());

            if (nested != null)
                obj.Encode(nested).ToString().DecodeInto(ret, nested);
            else
                ret.Decode(obj.Encode().ToString());

            return ret;
            

        }
        
		public static T LoadFromResources<T>(this T s, string subFolder, string file)where T:IStd, new() {
			if (s == null)
				s = new T ();
			s.Decode(StuffLoader.LoadStoryFromResource(subFolder, file));
			return s;
		}

        public static StdEncoder EncodeUnrecognized(this IKeepUnrecognizedStd ur) {
            return ur.UnrecognizedStd.Encode();
        }

        public static bool Decode(this IStd std, string data, IStdSerializeNestedReferences keeper) => data.DecodeInto(std, keeper);
    }
#endregion
}