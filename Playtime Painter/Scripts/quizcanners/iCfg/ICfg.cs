using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using Object = UnityEngine.Object;

namespace QuizCannersUtilities {

    #region Interfaces

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration

    public interface ICfg {
        CfgEncoder Encode(); 
        void Decode(string data);
        bool Decode(string tg, string data);
    }

    public interface IKeepUnrecognizedCfg : ICfg
    {
        UnrecognizedTagsList UnrecognizedStd { get; }
    }

    public interface ICanBeDefaultCfg : ICfg {
        bool IsDefault { get; }
    }

    public interface ICfgSerializeNestedReferences
    {
        int GetReferenceIndex(UnityEngine.Object obj);
        T GetReferenced<T>(int index) where T: UnityEngine.Object;
    }

    public interface ICfgSafeEncoding: ICfg
    {
        LoopLock GetLoopLock { get;  }
    }

    public interface IKeepMyCfg : ICfg
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
            public int CountForInspector() => elements.Count == 0 ? 1 : elements.CountForInspector();
            
            int _inspected = -1;
            public bool Inspect() => "{0} Sub Tags".F(tag).edit_List(ref elements, ref _inspected);

            public bool InspectInList(IList list, int ind, ref int edited)
            {
                var changed = false;

                changed |= pegi.edit(ref tag, 70);

                if (elements.Count == 0)
                    changed |= pegi.edit(ref data);
                else
                {
                    "+[{0}]".F(CountForInspector()).write(50);
                    if (icon.Enter.Click())
                        edited = ind;
                }
                return changed;
            }

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

        public CfgEncoder Encode() => locked ? new CfgEncoder() : _elements.Encode().Lock(this);

   
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
    
    }


    #endregion

    #region Abstract Implementations

    public abstract class ConfigurationsListGeneric<T> : ConfigurationsListBase where T : Configuration
    {

        public List<T> configurations = new List<T>();

        #region Inspector

        public override bool Inspect() => "Configurations".edit_List(ref configurations);

        #endregion

    }


    public abstract class ConfigurationsListBase : ScriptableObject, IPEGI
    {

        public virtual bool Inspect() => false;

        public static bool Inspect<T>(ref T configs) where T : ConfigurationsListBase
        {
            var changed = false;

            if (configs)
            {
                if (icon.UnLinked.Click("Disconnect config"))
                    configs = null;
                else
                    configs.Nested_Inspect().nl(ref changed);
            }
            else
            {
                "Configs".edit(90, ref configs);

                if (icon.Create.Click("Create new Config"))
                    configs = QcUnity.CreateScriptableObjectAsset<T>("ScriptableObjects/Configs", "Config");

                pegi.nl();
            }

            return changed;
        }

    }

    [Serializable]
    public abstract class Configuration : AbstractCfg, IPEGI_ListInspect, IGotName
    {
        public string name;
        public string data;
        
        public abstract Configuration ActiveConfiguration { get; set; }

        public void SetAsCurrent() {
            ActiveConfiguration = this;
        }

        public abstract CfgEncoder EncodeData();

        #region Inspect

        public string NameForPEGI
        {
            get { return name; }
            set { name = value; }
        }
        
        public virtual bool InspectInList(IList list, int ind, ref int edited) {

            var changed = false;
            var active = ActiveConfiguration;

            bool allowOverride = active == null || active == this;

            bool isActive = this == active;

            if (isActive)
                pegi.SetBgColor(Color.green);

            if (!allowOverride && !data.IsNullOrEmpty() && icon.Delete.ClickUnFocus())
                data = null;

            pegi.edit(ref name);

            if (isActive) {
                if (icon.Red.ClickUnFocus())
                    ActiveConfiguration = null;
            }
            else
            {

                if (!data.IsNullOrEmpty())  {
                    if (icon.Play.ClickUnFocus())
                        ActiveConfiguration = this;
                    
                }
                else if (icon.SaveAsNew.ClickUnFocus())
                    data = EncodeData().ToString();
            }



            if (allowOverride)
            {
                if (icon.Save.ClickUnFocus())
                    data = EncodeData().ToString();
            }


            pegi.RestoreBGcolor();

            return changed;
        }
       
        #endregion

        #region Encode & Decode

        public override CfgEncoder Encode() => new CfgEncoder()
            .Add_String("n", name)
            .Add_IfNotEmpty("d", data);

        public override bool Decode(string tg, string d) {
            switch (tg) {
                case "n": name = d; break;
                case "d": data = d; break;
                default: return false;   
            }

            return true;
        }



        #endregion

        public Configuration() {
            name = "New Config";
        }

        public Configuration(string name)
        {
            this.name = name;
        }

    }

    public class StdSimpleReferenceHolder : ICfgSerializeNestedReferences {
        
        public readonly List<Object> nestedReferences = new List<Object>();
        public int GetReferenceIndex(Object obj) => nestedReferences.TryGetIndexOrAdd(obj);

        public T GetReferenced<T>(int index) where T : Object => nestedReferences.TryGet(index) as T;

    }

    public class CfgReferencesHolder : ScriptableObject, ICfgSerializeNestedReferences, IPEGI, IKeepUnrecognizedCfg, ICfgSafeEncoding
    {
        
        #region Encode & Decode

        public UnrecognizedTagsList UnrecognizedStd { get; } = new UnrecognizedTagsList();

        public LoopLock GetLoopLock { get; } = new LoopLock();

        private readonly ListMetaData _listMetaData = new ListMetaData("References");

        [SerializeField] protected List<Object> nestedReferences = new List<Object>();
        public virtual int GetReferenceIndex(Object obj) => nestedReferences.TryGetIndexOrAdd(obj);

        public virtual T GetReferenced<T>(int index) where T : Object => nestedReferences.TryGet(index) as T;


        public virtual CfgEncoder Encode() => this.EncodeUnrecognized()
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

        public ICfgObjectExplorer explorer = new ICfgObjectExplorer();

        #region Inspector
 
        [ContextMenu("Reset Inspector")] // Because ContextMenu doesn't accepts overrides
        private void Reset() => ResetInspector();

        public virtual void ResetInspector()
        {
            _inspectedDebugItems = -1;
            inspectedReference = -1;
            inspectedItems = -1;
        }

        [HideInInspector]
        [SerializeField] public int inspectedItems = -1;
        private int _inspectedDebugItems = -1;
        [SerializeField] private int inspectedReference = -1;
      

        public virtual bool Inspect()
        {

            var changed = false;

            if (!icon.Debug.enter(ref inspectedItems, 0)) return false;
            
            if (icon.Refresh.Click("Reset Inspector"))
                ResetInspector();

            this.ClickHighlight();
            
            pegi.nl();
            
            if ("Configs: ".AddCount(explorer).enter(ref _inspectedDebugItems, 0).nl())
                explorer.Inspect(this);

            if (inspectedItems == -1)
                pegi.nl();

            if (("Object References: " + nestedReferences.Count).enter(ref _inspectedDebugItems, 1).nl())
            {
                _listMetaData.edit_List_UObj(ref nestedReferences);

                if (inspectedReference == -1 && "Clear All References".Click("Will clear the list. Make sure everything" +
                    ", that usu this object to hold references is currently decoded to avoid mixups"))
                    nestedReferences.Clear();

            }

            if (inspectedItems == -1)
                pegi.nl();

            if (("Unrecognized Tags: " + UnrecognizedStd.Count).enter(ref _inspectedDebugItems, 2).nl_ifNotEntered())
                UnrecognizedStd.Nested_Inspect(ref changed);

            if (inspectedItems == -1)
                pegi.nl();

            
            return changed;
        }
        
        #endregion
    }


    public abstract class AbstractCfg : ICfgSafeEncoding, ICanBeDefaultCfg {
        public abstract CfgEncoder Encode();
        public virtual void Decode(string data) => data.DecodeTagsFor(this);
        public abstract bool Decode(string tg, string data);

        public LoopLock GetLoopLock { get; } = new LoopLock();

        public virtual bool IsDefault => false;
    }

    public abstract class AbstractKeepUnrecognizedCfg : AbstractCfg, IKeepUnrecognizedCfg {
        public UnrecognizedTagsList UnrecognizedStd { get; } = new UnrecognizedTagsList();

#if !UNITY_EDITOR
        [NonSerialized]
        #endif
        private readonly ICfgObjectExplorer _explorer = new ICfgObjectExplorer();
        
        public override CfgEncoder Encode() => this.EncodeUnrecognized();

        public override bool Decode(string tg, string data) => false;
        #region Inspector

        public virtual void ResetInspector() {
            _inspectedItems = -1;
        }

        public int _inspectedItems = -1;

        public bool InspectingVisuals() => _inspectedItems == 21;

        public virtual bool Inspect() {
            var changed = false;

            if (icon.Debug.enter(ref _inspectedItems, 0)) {
                if (icon.Refresh.Click("Reset Inspector"))
                    ResetInspector();
                this.CopyPasteStdPegi().nl(ref changed);

                _explorer.Inspect(this);
                changed |= UnrecognizedStd.Nested_Inspect();
            }

            return changed;
        }
      
        #endregion
    }

    public abstract class ComponentCfg : MonoBehaviour, ICfgSafeEncoding, IKeepUnrecognizedCfg, ICanBeDefaultCfg, ICfgSerializeNestedReferences, IPEGI, IPEGI_ListInspect, IGotName, INeedAttention {

#if !UNITY_EDITOR
        [NonSerialized]
#endif
        public ICfgObjectExplorer explorer = new ICfgObjectExplorer();

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

        [HideInInspector]
        [SerializeField] public int inspectedItems = -1;
        
        [ContextMenu("Reset Inspector")]
        private void Reset() => ResetInspector();

        protected virtual void ResetInspector()
        {
            _inspectedDebugItems = -1;
            inspectedItems = -1;
        }

        public virtual string NeedAttention() => null;
        
        public virtual bool InspectInList(IList list, int ind, ref int edited)
        {
            var changed = false;
            var n = gameObject.name;
            if ((pegi.editDelayed(ref n) && n.Length > 0).changes(ref changed))
                gameObject.name = n;
            
            if (this.Click_Enter_Attention_Highlight(ref changed))
                edited = ind;
            

            return changed;
        }
        
        private int _inspectedDebugItems = -1;
        public virtual bool Inspect() {

            var changed = false;

            if (inspectedItems == -1)
                pegi.Lock_UnlockWindowClick(gameObject);

            if (!icon.Debug.enter(ref inspectedItems, 0).changes(ref changed)) return changed; 
                
            if (icon.Refresh.Click("Reset Inspector"))
                ResetInspector();

            this.CopyPasteStdPegi().nl(ref changed);

            pegi.toggleDefaultInspector(this).nl();
            
            "{0} Debug ".F(this.GetNameForInspector()).nl();

            if (("Cfg Saves: " + explorer.CountForInspector()).enter(ref _inspectedDebugItems, 0).nl())
                explorer.Inspect(this);

            if (inspectedItems == -1)
                pegi.nl();

            if (("Object References: " + nestedReferences.Count).enter(ref _inspectedDebugItems, 1).nl_ifNotEntered())
            {
                referencesMeta.edit_List_UObj(ref nestedReferences);
                if (!referencesMeta.Inspecting && "Clear All References".Click("Will clear the list. Make sure everything" +
                ", that usu this object to hold references is currently decoded to avoid mixups"))
                    nestedReferences.Clear();

            }

            if (inspectedItems == -1)
                pegi.nl();

            if (("Unrecognized Tags: " + UnrecognizedStd.Count).enter(ref _inspectedDebugItems, 2).nl_ifNotEntered())
                UnrecognizedStd.Nested_Inspect().changes(ref changed);

            if ("Inspect Inspector".enter(ref _inspectedDebugItems, 3).nl())
                QcUtils.InspectInspector();

            if (inspectedItems == -1)
                pegi.nl();
            
           
            return changed;
        }

        #endregion

        #region Encoding & Decoding

        public LoopLock GetLoopLock { get; } = new LoopLock();

        public virtual bool IsDefault => false;

        protected ListMetaData referencesMeta = new ListMetaData("References");

        [HideInInspector]
        [SerializeField] protected List<UnityEngine.Object> nestedReferences = new List<UnityEngine.Object>();
        public int GetReferenceIndex(UnityEngine.Object obj) => nestedReferences.TryGetIndexOrAdd(obj);
        
        public T GetReferenced<T>(int index) where T : UnityEngine.Object => nestedReferences.TryGet(index) as T;

        public UnrecognizedTagsList UnrecognizedStd { get; } = new UnrecognizedTagsList();

        public virtual bool Decode(string tg, string data)
        {

            switch (tg) {
          
                case "db": inspectedItems = data.ToInt(); break;
                default: return false;
            }
            
            return true;

        }

        public virtual CfgEncoder Encode() => this.EncodeUnrecognized().Add_IfNotNegative("db", inspectedItems) ;

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

        public static void EmailData(this ICfg cfg, string subject, string note)
        {
            if (cfg == null) return;

            QcUnity.SendEmail ( "somebody@gmail.com", subject, 
                "{0} {1} Copy this entire email and paste it in the corresponding field on your side to paste it (don't change data before pasting it). {2} {3}{4}{5}".F(note, pegi.EnvironmentNl, pegi.EnvironmentNl,
                StdStart,  cfg.Encode().ToString(), StdEnd ) ) ;
        }

        public static void DecodeFromExternal(this ICfg cfg, string data) => cfg?.Decode(ClearFromExternal(data));
        
        private static string ClearFromExternal(string data) {

            if (!data.Contains(StdStart)) return data;
            
            var start = data.IndexOf(StdStart, StringComparison.Ordinal) + StdStart.Length;
            var end = data.LastIndexOf(StdEnd, StringComparison.Ordinal);

            data = data.Substring(start, end - start);
                
            return data;

        }
        
        private static ICfg _toCopy;

        public static bool CopyPasteStdPegi(this ICfg cfg) {
            
            if (cfg == null) return false;
            
            var changed = false;
            
            if (_toCopy == null && icon.Copy.Click("Copy {0}".F(cfg.GetNameForInspector())).changes(ref changed))
                _toCopy = cfg;

            if (_toCopy == null) return changed;
            
            if (icon.Close.Click("Empty copy buffer"))
                _toCopy = null;
            else if (!Equals(cfg, _toCopy) && icon.Paste.Click("Copy {0} into {1}".F(_toCopy, cfg)))
                TryCopy_Std_AndOtherData(_toCopy, cfg);
                  
            return changed;
        }

        public static bool SendReceivePegi(this ICfg cfg, string name, string folderName, out string data) {
  
            if (icon.Email.Click("Send {0} to somebody via email.".F(folderName)))
                cfg.EmailData(name, "Use this {0}".F(name));

            data = "";
            if (pegi.edit(ref data).UnFocus()) {
                data = ClearFromExternal(data);
                return true;
            }

            if (icon.Folder.Click("Save {0} to the file".F(name))) {
                cfg.SaveToAssets(folderName, name);
                QcUnity.RefreshAssetDatabase();
            }

            if (DropStringObject(out data))
                return true;


            pegi.nl();

            return false;
        }

        private static readonly StdSimpleReferenceHolder TmpHolder = new StdSimpleReferenceHolder();

        public static void TryCopy_Std_AndOtherData(object from, object into)
        {
            if (into == null || into == from) return;
            
            var intoStd = into as ICfg;
            
            if (intoStd != null)
            {
                var fromStd = from as ICfg;

                if (fromStd != null)
                {
                    var prev = CfgEncoder.keeper;
                    CfgEncoder.keeper = TmpHolder;
                    intoStd.Decode(fromStd.Encode().ToString());
                    CfgEncoder.keeper = prev;

                    TmpHolder.nestedReferences.Clear();
                }


            }

            var ch = into as ICanChangeClass;
            if (ch != null && !QcUnity.IsNullOrDestroyed_Obj(from))
                ch.OnClassTypeChange(from);

            
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

        public static CfgEncoder Encode(this IEnumerable<UnrecognizedTagsList.UnrecognizedElement> lst) {
            var cody = new CfgEncoder();
            foreach (var e in lst) {
                if (e.elements.Count == 0)
                    cody.Add_String(e.tag, e.data);
                else
                    cody.Add(e.tag, e.elements.Encode());
            }

            return cody;
        }

        //public static TaggedTypesCfg GetTaggedTypes_Safe<T>(this T obj) where T : IGotClassTag => obj != null ? obj.AllTypes : typeof(T).TryGetTaggedClasses();
        
       /* public static TaggedTypesCfg TryGetTaggedClasses(this Type type)
        {

            if (!typeof(IGotClassTag).IsAssignableFrom(type)) return null;

            var attrs = type.GetCustomAttributes(typeof(AbstractWithTaggedTypes), true);

            if (!attrs.IsNullOrEmpty()) 
                return (attrs[0] as AbstractWithTaggedTypes).TaggedTypes;
            
            if (Debug.isDebugBuild)
                Debug.Log("{0} does not have Abstract_WithTaggedTypes Attribute");
            
            return null;
        }*/

        public static List<Type> TryGetDerivedClasses (this Type t) => t.TryGetClassAttribute<DerivedListAttribute>()?.derivedTypes.NullIfEmpty();
            

        public static string copyBufferValue;
        public static string copyBufferTag;

        public static bool DropStringObject(out string txt) {

            txt = null;

            UnityEngine.Object myType = null;
            if (pegi.edit(ref myType)) {
                txt = QcFile.LoadUtils.TryLoadAsTextAsset(myType);
                ("Loaded " + myType.name).showNotificationIn3D_Views();

                return true;
            }
            return false;
        }

        public static bool LoadCfgOnDrop<T>(this T obj) where T: ICfg
        {
            string txt;
            if (DropStringObject(out txt)) {
                obj.Decode(txt);
                return true;
            }

            return false;
        }

        public static void UpdateCfgPrefab (this ICfg s, GameObject go) {
            var iK = s as IKeepMyCfg;

            if (iK != null)
                iK.SaveCfgData();

            QcUnity.UpdatePrefab(go);
        }

        public static void SaveCfgData(this IKeepMyCfg s) {
            if (s != null)
            {
                s.ConfigStd = s.Encode().ToString();
                (s as Object).SetToDirty();
            }
        }

        public static bool LoadCfgData(this IKeepMyCfg s)
        {
            if (s == null)
                return false;

            s.Decode(s.ConfigStd);

            return true;
        }

        public static T LoadFromAssets<T>(this T s, string fullPath, string name) where T:ICfg, new() {
			if (s == null)
				s = new T ();
            s.Decode(QcFile.LoadUtils.LoadFromAssets(fullPath, name));
			return s;
        }

        public static ICfg SaveToAssets(this ICfg s, string path, string filename)
        {
            QcFile.SaveUtils.SaveToAssets(path, filename, s.Encode().ToString());
            return s;
        }

        public static ICfg SaveToPersistentPath(this ICfg s, string path, string filename)
        {
            QcFile.SaveUtils.SaveToPersistentPath(path, filename, s.Encode().ToString());
            return s;
        }

        public static bool LoadFromPersistentPath(this ICfg s, string path, string filename)
        {
            var data = QcFile.LoadUtils.LoadFromPersistentPath(path, filename);
            if (data != null)
            {
                s.Decode(data);
                return true;
            }
            return false;
        }

        public static ICfg SaveToResources(this ICfg s, string resFolderPath, string insideResPath, string filename)
        {
            QcFile.SaveUtils.SaveToResources(resFolderPath, insideResPath, filename, s.Encode().ToString());
            return s;
        }

        public static T CloneCfg<T>(this T obj, ICfgSerializeNestedReferences nested = null) where T : ICfg {

            if (QcUnity.IsNullOrDestroyed_Obj(obj)) return default(T);
            
            var ret = (T)Activator.CreateInstance(obj.GetType());

            if (nested != null)
                obj.Encode(nested).ToString().DecodeInto(ret, nested);
            else
                ret.Decode(obj.Encode().ToString());

            return ret;
        }

        public static bool TryLoadFromResources<T>(this T s, string subFolder, string file) where T : ICfg
        {
            var load = QcFile.LoadUtils.LoadStringFromResource(subFolder, file);

            if (load == null)
                return false;

            s.Decode(load);

            return true;
        }

        public static T LoadFromResources<T>(this T s, string subFolder, string file)where T:ICfg, new() {
			if (s == null)
				s = new T ();
			s.Decode(QcFile.LoadUtils.LoadStringFromResource(subFolder, file));
			return s;
		}

        public static CfgEncoder EncodeUnrecognized(this IKeepUnrecognizedCfg ur) => ur.UnrecognizedStd.Encode();
        
        public static bool Decode(this ICfg cfg, string data, ICfgSerializeNestedReferences keeper) => data.DecodeInto(cfg, keeper);
    }
#endregion
}