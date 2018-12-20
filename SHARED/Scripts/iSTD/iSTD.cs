using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using SharedTools_Stuff;
using static SharedTools_Stuff.StdDecoder;
#if UNITY_EDITOR
using UnityEditor;
#endif

// This interface works for simple data and for complex classes
// Usually the base class for comples classes will have 

namespace SharedTools_Stuff {

    #region Interfaces

    public interface ISTD {
        StdEncoder Encode(); 
        void Decode(string data);
        bool Decode(string tag, string data);
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
        int GetISTDreferenceIndex(UnityEngine.Object obj);
        T GetISTDreferenced<T>(int index) where T: UnityEngine.Object;
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
    ///<summary>For runtime initialization.
    ///<para> Usage [DerrivedListAttribute(derrivedClass1, DerrivedClass2, DerrivedClass3 ...)] </para>
    ///<seealso cref="StdEncoder"/>
    ///</summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DerrivedListAttribute : Attribute {
        public readonly List<Type> derrivedTypes;
        public DerrivedListAttribute(params Type[] ntypes) {
            derrivedTypes = new List<Type>(ntypes);
        }
    }
    #endregion

    #region UNRECOGNIZED


    public class UnrecognizedTags_List : IPEGI
    {

        public class UnrecognizedElement : IPEGI, IGotName, IGotCount, IPEGI_ListInspect
        {
            public string _tag;
            public string _data;

            public List<UnrecognizedElement> elements = new List<UnrecognizedElement>();

            public UnrecognizedElement() { }

            public UnrecognizedElement(string tag, string data)
            {
                _tag = tag;
                _data = data;
            }

            public UnrecognizedElement(List<string> tags, string data)
            {
                Add(tags, data);
            }

            public void Add(List<string> tags, string data)
            {
                if (tags.Count > 1)
                {
                    _tag = tags[0];
                    elements.Add(tags.GetRange(1, tags.Count - 1), data);
                }
                else
                {
                    _tag = tags[0];
                    _data = data;
                }
            }

            public string NameForPEGI { get { return _tag; } set { _tag = value; } }


            #region Inspector
#if PEGI
            public int CountForInspector => elements.Count == 0 ? 1 : elements.CountForInspector();
            
            int inspected = -1;
            public bool Inspect() => "{0} Subtags".F(_tag).edit_List(ref elements, ref inspected);

            public bool PEGI_inList(IList list, int ind, ref int edited)
            {
                bool changed = false;

                changed |= pegi.edit(ref _tag, 70);

                if (elements.Count == 0)
                    changed |= pegi.edit(ref _data);
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
            var exst = elements.GetByIGotName(tag);

            if (exst != null)
                exst._data = data;
            else
                elements.Add(tag, data);
        }

        public void Add(List<string> tags, string data) => elements.Add(tags, data);

        public StdEncoder Encode() => locked ? new StdEncoder() : elements.Encode().Lock(this);

#if PEGI

        public int Count => elements.CountForInspector();


        int inspected = -1;
        public bool Inspect()
        {
            bool changed = false;

            pegi.nl();

            "Unrecognized".edit_List(ref elements, ref inspected).nl(ref changed);

            pegi.nl();

            return changed;
        }
#endif

    }


    #endregion

    #region Abstract Implementations

    public class STD_SimpleReferenceHolder : ISTD_SerializeNestedReferences {

        [SerializeField] public List<UnityEngine.Object> _nestedReferences = new List<UnityEngine.Object>();
        public virtual int GetISTDreferenceIndex(UnityEngine.Object obj) => _nestedReferences.TryGetIndexOrAdd(obj);

        public virtual T GetISTDreferenced<T>(int index) where T : UnityEngine.Object => _nestedReferences.TryGet(index) as T;

    }

    public class STD_ReferencesHolder : ScriptableObject, ISTD_SerializeNestedReferences, IPEGI, IKeepUnrecognizedSTD, ISTD_SafeEncoding
    {
        
        #region Encode & Decode

        UnrecognizedTags_List uTags = new UnrecognizedTags_List();
        public UnrecognizedTags_List UnrecognizedSTD => uTags;

        readonly LoopLock encodeingLoopLock = new LoopLock();

        public LoopLock GetLoopLock => encodeingLoopLock;

        protected List_Data listData = new List_Data("References");

        [SerializeField] protected List<UnityEngine.Object> _nestedReferences = new List<UnityEngine.Object>();
        public virtual int GetISTDreferenceIndex(UnityEngine.Object obj) => _nestedReferences.TryGetIndexOrAdd(obj);

        public virtual T GetISTDreferenced<T>(int index) where T : UnityEngine.Object => _nestedReferences.TryGet(index) as T;


        public virtual StdEncoder Encode() => this.EncodeUnrecognized()
            .Add("listDta", listData);

        public virtual void Decode(string data) => data.DecodeTagsFor(this);

        public virtual bool Decode(string tag, string data)
        {
            switch (tag)
            {
                case "listDta": listData.Decode(data); break;
                default: return false;
            }
            return true;
        }
        #endregion

        public ISTD_ExplorerData explorer = new ISTD_ExplorerData();

        #region Inspector
        #if PEGI

        [ContextMenu("Reset Inspector")] // Because ContextMenu doesn't accepts overrides
        void Reset() => ResetInspector();

        public virtual void ResetInspector()
        {
            inspectedDebugStuff = -1;
            inspectedReference = -1;
            inspectedStuff = -1;
        }

        [HideInInspector]
        [SerializeField] public int inspectedStuff = -1;
        int inspectedDebugStuff = -1;
        [SerializeField] int inspectedReference = -1;
        public virtual bool Inspect()
        {

            bool changed = false;

            if (icon.Config.enter(ref inspectedStuff, 0))
            {

                if (icon.Refresh.Click("Reset Inspector"))
                    ResetInspector();

                this.clickHighlight();

                if (inspectedStuff == -1)
                    pegi.nl();

                if ("STD Saves: ".AddCount(explorer).enter(ref inspectedDebugStuff, 0).nl_ifNotEntered())
                    explorer.Inspect(this);

                if (inspectedStuff == -1)
                    pegi.nl();

                if (("Object References: " + _nestedReferences.Count).enter(ref inspectedDebugStuff, 1).nl_ifNotEntered())
                {
                    listData.edit_List_UObj(ref _nestedReferences);

                    if (inspectedReference == -1 && "Clear All References".Click("Will clear the list. Make sure everything" +
                        ", that usu this object to hold references is currently decoded to avoid mixups"))
                        _nestedReferences.Clear();

                }

                if (inspectedStuff == -1)
                    pegi.nl();

                if (("Unrecognized Tags: " + uTags.Count).enter(ref inspectedDebugStuff, 2).nl_ifNotEntered())
                    changed |= uTags.Nested_Inspect();

                if (inspectedStuff == -1)
                    pegi.nl();

            }
            return changed;
        }

        #endif
        #endregion
    }


    public abstract class Abstract_STD : ISTD_SafeEncoding, ICanBeDefault_STD {
        public abstract StdEncoder Encode();
        public virtual void Decode(string data) => data.DecodeTagsFor(this);
        public abstract bool Decode(string tag, string data);

        public LoopLock GetLoopLock => loopLock_std;

        public LoopLock loopLock_std = new LoopLock();

        public virtual bool IsDefault => false;
    }

    public abstract class AbstractKeepUnrecognized_STD : Abstract_STD, IKeepUnrecognizedSTD {

        UnrecognizedTags_List uTags = new UnrecognizedTags_List();
        public UnrecognizedTags_List UnrecognizedSTD => uTags;

#if !UNITY_EDITOR
        [NonSerialized]
#endif
        public ISTD_ExplorerData explorer = new ISTD_ExplorerData();
        
        public override StdEncoder Encode() => this.EncodeUnrecognized();

        public override bool Decode(string tag, string data) => false;
        #region Inspector

        public virtual void ResetInspector() {
            inspectedStuff = -1;
        }
        [HideInInspector]
        public int inspectedStuff = -1;

        #if PEGI
        public virtual bool Inspect() {
            bool changed = false;

            if (icon.Debug.enter(ref inspectedStuff, 0)) {
                if (icon.Refresh.Click("Reset Inspector"))
                    ResetInspector();
                this.CopyPasteSTD_PEGI().nl(ref changed);

                explorer.Inspect(this);
                changed |= uTags.Nested_Inspect();
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
        void Reset() => ResetInspector();

        public virtual void ResetInspector()
        {
            inspectedDebugStuff = -1;
            inspectedStuff = -1;
        }

        public virtual string NeedAttention() => null;
        
        public virtual bool PEGI_inList(IList list, int ind, ref int edited)
        {
            bool changed = false;
            var n = gameObject.name;
            if ((pegi.editDelayed(ref n) && n.Length > 0).changes(ref changed))
                gameObject.name = n;
            
            if (this.Attention_Or_Click())
                edited = ind;

            this.clickHighlight();

            return changed;
        }
        [HideInInspector]
        [SerializeField] public int inspectedStuff = -1;
        [HideInInspector]
        int inspectedDebugStuff = -1;
        public virtual bool Inspect() {

            bool changed = false;

            if (inspectedStuff == -1)
                pegi.Lock_UnlockWindow(gameObject);

           if (icon.Debug.enter(ref inspectedStuff, 0)) {

                if (icon.Refresh.Click("Reset Inspector"))
                    ResetInspector();

                this.CopyPasteSTD_PEGI().nl(ref changed);

                "{0} Debug ".F(this.ToPEGIstring()).nl();

                if (("STD Saves: " + explorer.states.Count).enter(ref inspectedDebugStuff, 0).nl_ifNotEntered())
                    explorer.Inspect(this);

                if (inspectedStuff == -1)
                    pegi.nl();

                if (("Object References: " + _nestedReferences.Count).enter(ref inspectedDebugStuff, 1).nl_ifNotEntered())
                {
                    references_Meta.edit_List_UObj(ref _nestedReferences);
                    if (!references_Meta.Inspecting && "Clear All References".Click("Will clear the list. Make sure everything" +
                    ", that usu this object to hold references is currently decoded to avoid mixups"))
                        _nestedReferences.Clear();

                }

                if (inspectedStuff == -1)
                    pegi.nl();

                if (("Unrecognized Tags: " + uTags.Count).enter(ref inspectedDebugStuff, 2).nl_ifNotEntered())
                    changed |= uTags.Nested_Inspect();

                if (inspectedStuff == -1)
                    pegi.nl();

            }
            return changed;
        }
        #endif
        #endregion

        #region Encoding & Decoding
        readonly LoopLock loopLock = new LoopLock();
        public LoopLock GetLoopLock => loopLock;

        public virtual bool IsDefault => false;

        protected List_Data references_Meta = new List_Data("References");

        [HideInInspector]
        [SerializeField] protected List<UnityEngine.Object> _nestedReferences = new List<UnityEngine.Object>();
        public int GetISTDreferenceIndex(UnityEngine.Object obj)
        {
            int before = _nestedReferences.Count;
            int index = _nestedReferences.TryGetIndexOrAdd(obj);
            return index;
        }
        public T GetISTDreferenced<T>(int index) where T : UnityEngine.Object => _nestedReferences.TryGet(index) as T;

        UnrecognizedTags_List uTags = new UnrecognizedTags_List();
        public UnrecognizedTags_List UnrecognizedSTD => uTags;
        
        public abstract bool Decode(string tag, string data);

        public abstract StdEncoder Encode();

        public virtual void Decode(string data) {
            uTags.Clear();
            data.DecodeTagsFor(this);
        }

        #endregion
    }

    #endregion

    #region Extensions
    public static class STDExtensions {

        const string stdStart = "<-<-<";
        const string stdEnd = ">->->";

        public static void EmailData(this ISTD std, string subject, string note) {
            if (std != null) {

                var data = std.Encode().ToString();

                UnityHelperFunctions.SendEmail ( "somebody@gmail.com", subject, 
                    "{0} {1} Copy this entire email (or only stuff below) and paste it in the corresponding field on your side to paste it (don't change data before pasting it). {2} {3}{4}{5}".F(note, pegi.EnvironmentNL, pegi.EnvironmentNL,
                    stdStart, data, stdEnd ) ) ;

            }
        }

        public static void DecodeFromExternal(this ISTD std, string data) {
            if (std != null)
                std.Decode(ClearFromExternal(data));

        }

        public static string ClearFromExternal(string data)  {
           
                if (data.Contains(stdStart)) {
                    var start = data.IndexOf(stdStart) + stdStart.Length;
                    var end = data.IndexOf(stdEnd);

                    data = data.Substring(start, end - start);
                }

            return data;

        }

#if PEGI
        static ISTD toCopy;

        public static bool CopyPasteSTD_PEGI(this ISTD std) {
            var changed = false;
            if (std != null) {
                if (toCopy == null && icon.Copy.Click("Copy {0}".F(std.ToPEGIstring())))
                    toCopy = std;
                if (toCopy != null) {
                    if (icon.Close.Click("Empty copy buffer"))
                        toCopy = null;
                    else if (std != toCopy && icon.Paste.Click("Copy {0} into {1}".F(toCopy, std)))
                        TryCopy_Std_AndOtherData(toCopy, std);

                }
            
                        
                        }
            return changed;
        }

        public static bool Send_Recieve_PEGI(this ISTD std, string name, string folderName, out string data) {
  
            if (icon.Email.Click("Send {0} to somebody via email.".F(folderName)))
                std.EmailData(name, "Use this {0}".F(name));

            data = "";
            if (pegi.edit(ref data)) {
                data = ClearFromExternal(data);
                pegi.DropFocus();
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
            if (into != null && into != from)
            {

                var intoSTD = into as ISTD;
                if (intoSTD != null)
                {
                    var fromSTD = from as ISTD;

                    if (fromSTD != null)
                    {
                        var prev = StdEncoder.keeper;
                        StdEncoder.keeper = tmpHolder;
                        intoSTD.Decode(fromSTD.Encode().ToString());
                        StdEncoder.keeper = prev;

                        tmpHolder._nestedReferences.Clear();
                    }


                }

                var ch = into as ICanChangeClass;
                if (ch != null && !from.IsNullOrDestroyed())
                    ch.Copy_NonSTDdata_From_PreviousInstance(from);

            }
        }


        public static void Add (this List<UnrecognizedTags_List.UnrecognizedElement> lst, List<string> tags, string data) {



            var exst = lst.GetByIGotName(tags[0]);
            if (exst != null)
                exst.Add(tags, data);
            else
                lst.Add(new UnrecognizedTags_List.UnrecognizedElement(tags, data));
        }

        public static void Add(this List<UnrecognizedTags_List.UnrecognizedElement> lst, string tag, string data)
            =>  lst.Add(new UnrecognizedTags_List.UnrecognizedElement(tag, data));

        public static StdEncoder Encode(this List<UnrecognizedTags_List.UnrecognizedElement> lst) {
            var cody = new StdEncoder();
            foreach (var e in lst) {
                if (e.elements.Count == 0)
                    cody.Add_String(e._tag, e._data);
                else
                    cody.Add(e._tag, e.elements.Encode());
            }

            return cody;
        }

        public static TaggedTypes_STD GetTaggedTypes_Safe<T>(this T obj) where T : IGotClassTag {
            if (obj != null)
                return obj.AllTypes;
            else
                return typeof(T).TryGetTaggetClasses();
        } 

        public static TaggedTypes_STD TryGetTaggetClasses(this Type type) {

            if (typeof(IGotClassTag).IsAssignableFrom(type)) {

                var attrs = type.GetCustomAttributes(typeof(Abstract_WithTaggedTypes), true);
                if (attrs.Length > 0)
                    return (attrs[0] as Abstract_WithTaggedTypes).TaggedTypes;
                else
                    if (Debug.isDebugBuild)
                    Debug.Log("{0} does not have Abstract_WithTaggedTypes Attribute");

            }

            return null;
        }

        public static List<Type> TryGetDerrivedClasses (this Type t)
        {
            List<Type> tps = null;
            var att = t.TryGetClassAttribute<DerrivedListAttribute>();
            if (att != null)
            {
                tps = att.derrivedTypes;
                if (tps != null && tps.Count == 0)
                    tps = null;
            }


            return tps;
        }

        public static string copyBufferValue;
        public static string copyBufferTag;

        public static bool LoadOnDrop(out string txt) {

            txt = null;
#if PEGI
            UnityEngine.Object myType = null;
            if (pegi.edit(ref myType)) {
                txt = StuffLoader.LoadTextAsset(myType);
               // Debug.Log("Decoded {0}".F(txt));
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
                iK.Save_STDdata();

            go.UpdatePrefab();
        }

        public static void Save_STDdata(this IKeepMySTD s) {
            if (s != null)
                s.Config_STD = s.Encode().ToString();
        }

        public static void Load_STDdata(this IKeepMySTD s) {
            if (s != null)
                s.Decode(s.Config_STD);
        }

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

        public static ISTD SaveToPersistantPath(this ISTD s, string path, string filename)
        {
            StuffSaver.SaveToPersistantPath(path, filename, s.Encode().ToString());
            return s;
        }

        public static ISTD LoadFromPersistantPath(this ISTD s, string path, string filename)
        {
            s.Decode(StuffLoader.LoadFromPersistantPath(path, filename));
            return s;
        }

        public static ISTD SaveToResources(this ISTD s, string ResFolderPath, string InsideResPath, string filename)
        {
            StuffSaver.SaveToResources(ResFolderPath, InsideResPath, filename, s.Encode().ToString());
            return s;
        }

        public static T Clone_ISTD<T>(this T obj, ISTD_SerializeNestedReferences nested = null) where T : ISTD {

            if (obj != null) {
                T ret = (T)Activator.CreateInstance(obj.GetType());

                if (nested != null)
                    obj.Encode(nested).ToString().DecodeInto(ret, nested);
                else
                    ret.Decode(obj.Encode().ToString());

                return ret;
            }

            return default(T);
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