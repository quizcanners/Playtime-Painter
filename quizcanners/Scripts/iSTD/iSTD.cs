using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using static QuizCannersUtilities.StdDecoder;
#if UNITY_EDITOR
using UnityEditor;
#endif

// This interface works for simple data and for complex classes
// Usually the base class for comples classes will have 

namespace QuizCannersUtilities {

    #region Interfaces

    public interface ISTD {
        StdEncoder Encode(); 
        void Decode(string data);
        bool Decode(string tg, string data);
    }

    public interface IKeepUnrecognizedSTD : ISTD
    {
        UnrecognizedTags_List UnrecognizedSTD { get; }
    }

    public interface ICanBeDefault_STD : ISTD {
        bool IsDefault { get; }
    }

    public interface ISTD_SerializeNestedReferences
    {
        int GetReferenceIndex(UnityEngine.Object obj);
        T GetReferenced<T>(int index) where T: UnityEngine.Object;
    }

    public interface ISTD_SafeEncoding: ISTD
    {
        LoopLock GetLoopLock { get;  }
    }

    public interface IKeepMySTD : ISTD
    {
        string Config_STD { get; set; }
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

    public class UnrecognizedTags_List : IPEGI
    {

        public class UnrecognizedElement : IPEGI, IGotName, IGotCount, IPEGI_ListInspect
        {
            public string tag;
            public string data;

            public List<UnrecognizedElement> elements = new List<UnrecognizedElement>();

            public UnrecognizedElement() { }

            public UnrecognizedElement(string tag, string data)
            {
                this.tag = tag;
                this.data = data;
            }

            public UnrecognizedElement(List<string> tags, string data)
            {
                Add(tags, data);
            }

            public void Add(List<string> tags, string data)
            {
                if (tags.Count > 1)
                {
                    tag = tags[0];
                    elements.Add(tags.GetRange(1, tags.Count - 1), data);
                }
                else
                {
                    tag = tags[0];
                    this.data = data;
                }
            }

            public string NameForPEGI { get { return tag; } set { tag = value; } }


            #region Inspector
#if PEGI
            public int CountForInspector => elements.Count == 0 ? 1 : elements.CountForInspector();
            
            int inspected = -1;
            public bool Inspect() => "{0} Sub Tags".F(tag).edit_List(ref elements, ref inspected);

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

        List<UnrecognizedElement> elements = new List<UnrecognizedElement>();

        public bool locked = false;

        public void Clear() => elements = new List<UnrecognizedElement>();

        public void Add(string tag, string data)
        {
            var existing = elements.GetByIGotName(tag);

            if (existing != null)
                existing.data = data;
            else
                elements.Add(tag, data);
        }

        public void Add(List<string> tags, string data) => elements.Add(tags, data);

        public StdEncoder Encode() => locked ? new StdEncoder() : elements.Encode().Lock(this);

        #if PEGI
    
        public int Count => elements.CountForInspector();
    
    
        private int _inspected = -1;
        public bool Inspect()
        {
            var changed = false;
    
            pegi.nl();
    
            "Unrecognized".edit_List(ref elements, ref _inspected).nl(ref changed);
    
            pegi.nl();
    
            return changed;
        }
        #endif

    }


    #endregion

    #region Abstract Implementations

    public class STD_SimpleReferenceHolder : ISTD_SerializeNestedReferences {

        [SerializeField] public readonly List<UnityEngine.Object> _nestedReferences = new List<UnityEngine.Object>();
        public virtual int GetReferenceIndex(UnityEngine.Object obj) => _nestedReferences.TryGetIndexOrAdd(obj);

        public virtual T GetReferenced<T>(int index) where T : UnityEngine.Object => _nestedReferences.TryGet(index) as T;

    }

    public class STD_ReferencesHolder : ScriptableObject, ISTD_SerializeNestedReferences, IPEGI, IKeepUnrecognizedSTD, ISTD_SafeEncoding
    {
        
        #region Encode & Decode

        private readonly UnrecognizedTags_List uTags = new UnrecognizedTags_List();
        public UnrecognizedTags_List UnrecognizedSTD => uTags;

        readonly LoopLock _encodingLoopLock = new LoopLock();

        public LoopLock GetLoopLock => _encodingLoopLock;

        protected readonly ListMetaData listMetaData = new ListMetaData("References");

        [SerializeField] protected List<UnityEngine.Object> _nestedReferences = new List<UnityEngine.Object>();
        public virtual int GetReferenceIndex(UnityEngine.Object obj) => _nestedReferences.TryGetIndexOrAdd(obj);

        public virtual T GetReferenced<T>(int index) where T : UnityEngine.Object => _nestedReferences.TryGet(index) as T;


        public virtual StdEncoder Encode() => this.EncodeUnrecognized()
            .Add("listDta", listMetaData);

        public virtual void Decode(string data) => data.DecodeTagsFor(this);

        public virtual bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "listDta": listMetaData.Decode(data); break;
                default: return false;
            }
            return true;
        }
        #endregion

        public ISTD_ExplorerData explorer = new ISTD_ExplorerData();

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

            if (("Object References: " + _nestedReferences.Count).enter(ref _inspectedDebugStuff, 1).nl_ifNotEntered())
            {
                listMetaData.edit_List_UObj(ref _nestedReferences);

                if (inspectedReference == -1 && "Clear All References".Click("Will clear the list. Make sure everything" +
                    ", that usu this object to hold references is currently decoded to avoid mixups"))
                    _nestedReferences.Clear();

            }

            if (inspectedStuff == -1)
                pegi.nl();

            if (("Unrecognized Tags: " + uTags.Count).enter(ref _inspectedDebugStuff, 2).nl_ifNotEntered())
                changed |= uTags.Nested_Inspect();

            if (inspectedStuff == -1)
                pegi.nl();

            
            return changed;
        }

        #endif
        #endregion
    }


    public abstract class Abstract_STD : ISTD_SafeEncoding, ICanBeDefault_STD {
        public abstract StdEncoder Encode();
        public virtual void Decode(string data) => data.DecodeTagsFor(this);
        public abstract bool Decode(string tg, string data);

        public LoopLock GetLoopLock => _loopLockStd;

        private readonly LoopLock _loopLockStd = new LoopLock();

        public virtual bool IsDefault => false;
    }

    public abstract class AbstractKeepUnrecognized_STD : Abstract_STD, IKeepUnrecognizedSTD {

        private readonly UnrecognizedTags_List _uTags = new UnrecognizedTags_List();
        public UnrecognizedTags_List UnrecognizedSTD => _uTags;

        #if !UNITY_EDITOR
        [NonSerialized]
        #endif
        private readonly ISTD_ExplorerData _explorer = new ISTD_ExplorerData();
        
        public override StdEncoder Encode() => this.EncodeUnrecognized();

        public override bool Decode(string tg, string data) => false;
        #region Inspector

        public virtual void ResetInspector() {
            inspectedStuff = -1;
        }
        [HideInInspector]
        public int inspectedStuff = -1;

        #if PEGI
        public virtual bool Inspect() {
            var changed = false;

            if (icon.Debug.enter(ref inspectedStuff, 0)) {
                if (icon.Refresh.Click("Reset Inspector"))
                    ResetInspector();
                this.CopyPasteStdPegi().nl(ref changed);

                _explorer.Inspect(this);
                changed |= _uTags.Nested_Inspect();
            }

            return changed;
        }
        #endif
        #endregion
    }

    public abstract class ComponentSTD : MonoBehaviour, ISTD_SafeEncoding, IKeepUnrecognizedSTD, ICanBeDefault_STD, ISTD_SerializeNestedReferences, IPEGI, IPEGI_ListInspect, IGotName, INeedAttention {

#if !UNITY_EDITOR
        [NonSerialized]
#endif
        public ISTD_ExplorerData explorer = new ISTD_ExplorerData();

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
        [HideInInspector]
        int _inspectedDebugStuff = -1;
        public virtual bool Inspect() {

            var changed = false;

            if (inspectedStuff == -1)
                pegi.Lock_UnlockWindowClick(gameObject);

            if (!icon.Debug.enter(ref inspectedStuff, 0).changes(ref changed)) return changed; 
                
            if (icon.Refresh.Click("Reset Inspector"))
                ResetInspector();

            this.CopyPasteStdPegi().nl(ref changed);

            pegi.toggleDefaultInspector().nl();
            
            "{0} Debug ".F(this.ToPEGIstring()).nl();

            if (("STD Saves: " + explorer.states.Count).enter(ref _inspectedDebugStuff, 0).nl_ifNotEntered())
                explorer.Inspect(this);

            if (inspectedStuff == -1)
                pegi.nl();

            if (("Object References: " + _nestedReferences.Count).enter(ref _inspectedDebugStuff, 1).nl_ifNotEntered())
            {
                references_Meta.edit_List_UObj(ref _nestedReferences);
                if (!references_Meta.Inspecting && "Clear All References".Click("Will clear the list. Make sure everything" +
                ", that usu this object to hold references is currently decoded to avoid mixups"))
                    _nestedReferences.Clear();

            }

            if (inspectedStuff == -1)
                pegi.nl();

            if (("Unrecognized Tags: " + _uTags.Count).enter(ref _inspectedDebugStuff, 2).nl_ifNotEntered())
                changed |= _uTags.Nested_Inspect();

            if (inspectedStuff == -1)
                pegi.nl();
            
           
            return changed;
        }
        #endif
        #endregion

        #region Encoding & Decoding
        private readonly LoopLock _loopLock = new LoopLock();
        public LoopLock GetLoopLock => _loopLock;

        public virtual bool IsDefault => false;

        protected readonly ListMetaData references_Meta = new ListMetaData("References");

        [HideInInspector]
        [SerializeField] protected List<UnityEngine.Object> _nestedReferences = new List<UnityEngine.Object>();
        public int GetReferenceIndex(UnityEngine.Object obj) => _nestedReferences.TryGetIndexOrAdd(obj);
        
        public T GetReferenced<T>(int index) where T : UnityEngine.Object => _nestedReferences.TryGet(index) as T;

        private readonly UnrecognizedTags_List _uTags = new UnrecognizedTags_List();
        public UnrecognizedTags_List UnrecognizedSTD => _uTags;
        
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
            _uTags.Clear();
            data.DecodeTagsFor(this);
        }

#endregion
    }

#endregion

#region Extensions
    public static class STDExtensions {

        private const string stdStart = "<-<-<";
        private const string stdEnd = ">->->";

        public static void EmailData(this ISTD std, string subject, string note)
        {
            if (std == null) return;

            UnityHelperFunctions.SendEmail ( "somebody@gmail.com", subject, 
                "{0} {1} Copy this entire email (or only stuff below) and paste it in the corresponding field on your side to paste it (don't change data before pasting it). {2} {3}{4}{5}".F(note, pegi.EnvironmentNl, pegi.EnvironmentNl,
                stdStart,  std.Encode().ToString(), stdEnd ) ) ;
        }

        public static void DecodeFromExternal(this ISTD std, string data) => std?.Decode(ClearFromExternal(data));
        
        private static string ClearFromExternal(string data) {

            if (!data.Contains(stdStart)) return data;
            
            var start = data.IndexOf(stdStart) + stdStart.Length;
            var end = data.IndexOf(stdEnd);

            data = data.Substring(start, end - start);
                
            return data;

        }

#if PEGI
        static ISTD toCopy;

        public static bool CopyPasteStdPegi(this ISTD std) {
            
            if (std == null) return false;
            
            var changed = false;
            
            if (toCopy == null && icon.Copy.Click("Copy {0}".F(std.ToPEGIstring())).changes(ref changed))
                toCopy = std;

            if (toCopy == null) return changed;
            
            if (icon.Close.Click("Empty copy buffer"))
                toCopy = null;
            else if (std != toCopy && icon.Paste.Click("Copy {0} into {1}".F(toCopy, std)))
                TryCopy_Std_AndOtherData(toCopy, std);
                  
            return changed;
        }

        public static bool SendRecievePegi(this ISTD std, string name, string folderName, out string data) {
  
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

        static readonly STD_SimpleReferenceHolder tmpHolder = new STD_SimpleReferenceHolder();

        public static void TryCopy_Std_AndOtherData(object from, object into)
        {
            if (into == null || into == from) return;
            
            var intoStd = into as ISTD;
            
            if (intoStd != null)
            {
                var fromStd = from as ISTD;

                if (fromStd != null)
                {
                    var prev = StdEncoder.keeper;
                    StdEncoder.keeper = tmpHolder;
                    intoStd.Decode(fromStd.Encode().ToString());
                    StdEncoder.keeper = prev;

                    tmpHolder._nestedReferences.Clear();
                }


            }

            var ch = into as ICanChangeClass;
            if (ch != null && !from.IsNullOrDestroyed_Obj())
                ch.Copy_NonStdData_From_PreviousInstance(from);

            
        }


        public static void Add (this List<UnrecognizedTags_List.UnrecognizedElement> lst, List<string> tags, string data) {

            var existing = lst.GetByIGotName(tags[0]);
            if (existing != null)
                existing.Add(tags, data);
            else
                lst.Add(new UnrecognizedTags_List.UnrecognizedElement(tags, data));
        }

        public static void Add(this List<UnrecognizedTags_List.UnrecognizedElement> lst, string tag, string data)
            =>  lst.Add(new UnrecognizedTags_List.UnrecognizedElement(tag, data));

        public static StdEncoder Encode(this IEnumerable<UnrecognizedTags_List.UnrecognizedElement> lst) {
            var cody = new StdEncoder();
            foreach (var e in lst) {
                if (e.elements.Count == 0)
                    cody.Add_String(e.tag, e.data);
                else
                    cody.Add(e.tag, e.elements.Encode());
            }

            return cody;
        }

        public static TaggedTypes_STD GetTaggedTypes_Safe<T>(this T obj) where T : IGotClassTag => obj != null ? obj.AllTypes : typeof(T).TryGetTaggedClasses();
        
        public static TaggedTypes_STD TryGetTaggedClasses(this Type type)
        {

            if (!typeof(IGotClassTag).IsAssignableFrom(type)) return null;

            var attrs = type.GetCustomAttributes(typeof(Abstract_WithTaggedTypes), true);

            if (!attrs.IsNullOrEmpty()) 
                return (attrs[0] as Abstract_WithTaggedTypes).TaggedTypes;
            
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

        public static bool LoadOnDrop<T>(this T obj) where T: ISTD
        {
            string txt;
            if (LoadOnDrop(out txt)) {
                obj.Decode(txt);
                return true;
            }

            return false;
        }

        public static void UpdatePrefab (this ISTD s, GameObject go) {
            var iK = s as IKeepMySTD;

            if (iK != null)
                iK.SaveStdData();

            go.UpdatePrefab();
        }

        public static void SaveStdData(this IKeepMySTD s) {
            if (s != null)
                s.Config_STD = s.Encode().ToString();
        }

        public static void LoadStdData(this IKeepMySTD s) => s?.Decode(s.Config_STD);
        

        public static T LoadFromAssets<T>(this T s, string fullPath, string name) where T:ISTD, new() {
			if (s == null)
				s = new T ();
            s.Decode(StuffLoader.LoadStoryFromAssets(fullPath, name));
			return s;
        }

        public static ISTD SaveToAssets(this ISTD s, string path, string filename)
        {
            StuffSaver.Save_ToAssets_ByRelativePath(path, filename, s.Encode().ToString());
            return s;
        }

        public static ISTD SaveToPersistentPath(this ISTD s, string path, string filename)
        {
            StuffSaver.SaveToPersistentPath(path, filename, s.Encode().ToString());
            return s;
        }

        public static bool LoadFromPersistentPath(this ISTD s, string path, string filename)
        {
            var data = StuffLoader.LoadFromPersistentPath(path, filename);
            if (data != null)
            {
                s.Decode(data);
                return true;
            }
            return false;
        }

        public static ISTD SaveToResources(this ISTD s, string ResFolderPath, string InsideResPath, string filename)
        {
            StuffSaver.SaveToResources(ResFolderPath, InsideResPath, filename, s.Encode().ToString());
            return s;
        }

        public static T Clone_ISTD<T>(this T obj, ISTD_SerializeNestedReferences nested = null) where T : ISTD
        {

            if (obj.IsNullOrDestroyed_Obj()) return default(T);
            
            var ret = (T)Activator.CreateInstance(obj.GetType());

            if (nested != null)
                obj.Encode(nested).ToString().DecodeInto(ret, nested);
            else
                ret.Decode(obj.Encode().ToString());

            return ret;
            

        }
        
		public static T LoadFromResources<T>(this T s, string subFolder, string file)where T:ISTD, new() {
			if (s == null)
				s = new T ();
			s.Decode(StuffLoader.LoadStoryFromResource(subFolder, file));
			return s;
		}

        public static StdEncoder EncodeUnrecognized(this IKeepUnrecognizedSTD ur) {
            return ur.UnrecognizedSTD.Encode();
        }

        public static bool Decode(this ISTD std, string data, ISTD_SerializeNestedReferences keeper) => data.DecodeInto(std, keeper);
    }
#endregion
}