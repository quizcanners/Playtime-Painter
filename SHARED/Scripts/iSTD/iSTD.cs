using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
#if UNITY_EDITOR
using UnityEditor;
#endif

// This interface works for simple data and for complex classes
// Usually the base class for comples classes will have 

namespace SharedTools_Stuff
{

    public interface ISTD {
        StdEncoder Encode(); 
        ISTD Decode(string data);
        bool Decode(string tag, string data);
    }

    #region Nested
    
    ///<summary>For runtime initialization.
    ///<para> Best used on Scriptable Objects. They don't loose referances. Prefabs needs to be updated and scenes saved to keep any referances</para>
    ///<seealso cref="StdEncoder"/>
    ///</summary>
    public interface ISTD_SerializeNestedReferences
    {
        int GetISTDreferenceIndex(UnityEngine.Object obj);
        T GetISTDreferenced<T>(int index) where T: UnityEngine.Object;
    }
    
    ///<summary>For runtime initialization.
    ///<para> Usage [DerrivedListAttribute(derrivedClass1, DerrivedClass2, DerrivedClass3 ...)] </para>
    ///<seealso cref="StdEncoder"/>
    ///</summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DerrivedListAttribute : Attribute
    {
        public readonly List<Type> derrivedTypes;
        public DerrivedListAttribute(params Type[] ntypes)
        {
            derrivedTypes = new List<Type>(ntypes);
        }
    }

    #endregion

    #region Unrecognized Tags Persistance

    public interface IKeepUnrecognizedSTD : ISTD {
       UnrecognizedTags_List UnrecognizedSTD { get; }
    }


    ///<summary>Often controller may be storing his data in itself.
    ///<para> Best to mark data [HideInInspector] </para>
    ///<seealso cref="StdEncoder"/>
    ///</summary>
    public interface IKeepMySTD : ISTD
    {
        string Config_STD { get; set; }
    }


    public class STD_ReferancesHolder : ScriptableObject, ISTD_SerializeNestedReferences, IPEGI, IKeepUnrecognizedSTD {

        UnrecognizedTags_List uTags = new UnrecognizedTags_List();
        public UnrecognizedTags_List UnrecognizedSTD => uTags;

        protected UnnullableSTD<ElementData> nestedReferenceDatas = new UnnullableSTD<ElementData>();
        
        [HideInInspector]
        [SerializeField] protected List<UnityEngine.Object> _nestedReferences = new List<UnityEngine.Object>();
        public virtual int GetISTDreferenceIndex(UnityEngine.Object obj) => _nestedReferences.TryGetIndexOrAdd(obj);
        
        public virtual T GetISTDreferenced<T>(int index) where T : UnityEngine.Object => _nestedReferences.TryGet(index) as T;

        public virtual StdEncoder Encode() => this.EncodeUnrecognized()
            .Add("refDta", nestedReferenceDatas);

        public virtual ISTD Decode(string data) => data.DecodeTagsFor(this);

        public virtual bool Decode(string tag, string data)
        {
            switch (tag)
            {
                case "refDta": data.DecodeInto(out nestedReferenceDatas); break;
                default: return false;
            }
            return true;
        }

#if !UNITY_EDITOR
        [NonSerialized]
#endif
        public ISTD_ExplorerData explorer = new ISTD_ExplorerData();

        public bool showDebug;

#if PEGI

        [SerializeField] int inspectedStuff = -1;
        [SerializeField] int inspectedReference = -1;
        public virtual bool PEGI()
        {

            bool changed = false;
            
            if (!showDebug && icon.Config.Click())
                showDebug = true;

            if (showDebug)
            {
                if (icon.Edit.Click("Back to element inspection"))
                    showDebug = false;

                this.clickHighlight();

                if (inspectedStuff == -1)
                    pegi.nl();

                if (("STD Saves: " + explorer.states.Count).fold_enter_exit(ref inspectedStuff, 0).nl_ifTrue())
                    explorer.PEGI(this);

                if (inspectedStuff == -1)
                    pegi.nl();

                if (("Object References: " + _nestedReferences.Count).fold_enter_exit(ref inspectedStuff, 1).nl_ifTrue())
                {
                    "References".edit_List_Obj(_nestedReferences, ref inspectedReference, nestedReferenceDatas);

                    if (inspectedReference == -1 && "Clear All Referances".Click("Will clear the list. Make sure everything" +
                        ", that usu this object to hold referances is currently decoded to avoid mixups"))
                        _nestedReferences.Clear();

                }

                if (inspectedStuff == -1)
                    pegi.nl();

                if (("Unrecognized Tags: " + uTags.Count).fold_enter_exit(ref inspectedStuff, 2).nl_ifTrue())
                    changed |= uTags.Nested_Inspect();

                if (inspectedStuff == -1)
                    pegi.nl();

            }
            return changed;
        }
        
#endif
    }

    public class UnrecognizedTags_List :IPEGI
    {
        protected List<string> tags = new List<string>();
        protected List<string> datas = new List<string>();

        public int Count { get { return tags.Count; } }

        public void Clear() { tags = new List<string>(); datas = new List<string>(); }

        public StdEncoder GetAll()
        {
            var cody = new StdEncoder();
            for (int i = 0; i < tags.Count; i++)
                cody.Add_String(tags[i], datas[i]);
            return cody;
        }

        public void Add(string tag, string data)
        {
            if (tags.Contains(tag))
            {
                int ind = tags.IndexOf(tag);
                tags[ind] = tag;
                datas[ind] = data;
            }
            else
            {
                tags.Add(tag);
                datas.Add(data);
            }
            
        }

        public UnrecognizedTags_List()
        {
            Clear();
        }

#if PEGI
        int inspected = -1;
        bool foldout = false;
        public bool PEGI( )
        {
            bool changed = false;

            pegi.nl();

            var cnt = tags.Count;

            if (cnt > 0 && ("Unrecognized ["+cnt+"]").foldout(ref foldout))
                {

                    if (inspected < 0)
                    {
                        for (int i = 0; i < tags.Count; i++)
                        {
                            if (icon.Delete.Click())
                            {
                                changed = true;
                                tags.RemoveAt(i);
                                datas.RemoveAt(i);
                                i--;
                            }
                            else
                            {
                                pegi.write(tags[i]);
                                if (icon.Edit.Click().nl())
                                    inspected = i;
                            }
                        }
                    }
                    else
                    {
                        if (inspected >= tags.Count || icon.Back.Click())
                            inspected = -1;
                        else
                        {
                            int i = inspected;
                            var t = tags[i];
                            if ("Tag".edit(40, ref t).nl())
                                tags[i] = t;
                            var d = datas[i];
                            if ("Data".edit(50, ref d).nl())
                                datas[i] = d;
                        }
                    }
                }

            pegi.nl();

            return changed;
        }
#endif

    }
    
    public abstract class Abstract_STD : ISTD
    {

        public abstract StdEncoder Encode();
        public virtual ISTD Decode(string data) => data.DecodeTagsFor(this);
        public abstract bool Decode(string tag, string data);
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
        
#if PEGI
        public bool showDebug;

        public virtual bool PEGI() {
            bool changed = false;
           
            if (!showDebug && icon.Config.Click())
                showDebug = true;

            if (showDebug)
            {
                if (icon.Exit.Click("Back to element inspection").nl())
                    showDebug = false;

                explorer.PEGI(this);

                changed |= uTags.Nested_Inspect();
            }

            return changed;
        }
#endif
    }

    public abstract class ComponentSTD : MonoBehaviour, IKeepUnrecognizedSTD, ISTD_SerializeNestedReferences, IPEGI, IPEGI_ListInspect, IGotName, INeedAttention {

        protected UnnullableSTD<ElementData> nestedReferenceDatas = new UnnullableSTD<ElementData>();
        
        [HideInInspector]
        [SerializeField] protected List<UnityEngine.Object> _nestedReferences = new List<UnityEngine.Object>();
        public int GetISTDreferenceIndex(UnityEngine.Object obj)
        {
            int before = _nestedReferences.Count;
            int index = _nestedReferences.TryGetIndexOrAdd(obj);
            #if PEGI
            if (before != _nestedReferences.Count) nestedReferencesChanged = true;
            #endif
            return index;
        }
        public T GetISTDreferenced<T>(int index) where T : UnityEngine.Object => _nestedReferences.TryGet(index) as T;
        
        UnrecognizedTags_List uTags = new UnrecognizedTags_List();
        public UnrecognizedTags_List UnrecognizedSTD => uTags;
        
        public virtual void Reboot() => uTags.Clear();
        
        public abstract bool Decode(string tag, string data);
        public abstract StdEncoder Encode();

#if !UNITY_EDITOR
        [NonSerialized]
#endif
        public ISTD_ExplorerData explorer = new ISTD_ExplorerData();

        public bool showDebug;

        public virtual string NameForPEGI
        {
            get
            {
                return gameObject.name;
            }

            set
            {
                gameObject.name = value;
            }
        }

#if PEGI
 
          bool nestedReferencesChanged;

        public virtual string NeedAttention()
        {
            if (nestedReferencesChanged && gameObject.IsPrefab())
                return "Nested References changed";

            return null;
        }

        public virtual bool PEGI_inList(IList list, int ind, ref int edited)
        {
            bool changed = false;
            var n = gameObject.name;
            if (pegi.editDelayed(ref n) && n.Length > 0)
            {
                gameObject.name = n;
                changed = true;
            }

            if (icon.Enter.Click())
                edited = ind;

            this.clickHighlight();

            return changed;
        }

        [SerializeField] int inspectedStuff = -1;
        [SerializeField] int inspectedReference = -1;
        public virtual bool PEGI()
        {

            bool changed = false;

            var attention = NeedAttention();

            if (!showDebug && (attention!= null ? icon.Warning.Click(attention) : icon.Config.Click()))
                showDebug = true;

            if (showDebug)
            {

                if (nestedReferencesChanged && gameObject.IsPrefab() && icon.Save.Click())
                    this.UpdatePrefab(gameObject);

                if (icon.Edit.Click("Back to element inspection"))
                    showDebug = false;

                (this.ToPEGIstring() + " Debug").write();

                if (inspectedStuff == -1)
                    pegi.nl();

                if (("STD Saves: " + explorer.states.Count).fold_enter_exit(ref inspectedStuff, 0).nl_ifTrue())
                    explorer.PEGI(this);

                if (inspectedStuff == -1)
                    pegi.nl();

                if (("Object References: " + _nestedReferences.Count).fold_enter_exit(ref inspectedStuff, 1).nl_ifTrue())
                {
                    "References".edit_List_Obj(_nestedReferences, ref inspectedReference, nestedReferenceDatas);
                    if (inspectedReference == -1 && "Clear All Referances".Click("Will clear the list. Make sure everything" +
                    ", that usu this object to hold referances is currently decoded to avoid mixups"))
                        _nestedReferences.Clear();

                }

                if (inspectedStuff == -1)
                    pegi.nl();

                if (("Unrecognized Tags: " + uTags.Count).fold_enter_exit(ref inspectedStuff, 2).nl_ifTrue())
                    changed |= uTags.Nested_Inspect();

                if (inspectedStuff == -1)
                    pegi.nl();

            }
            return changed;
        }
#endif

        public virtual ISTD Decode(string data)
        {
            Reboot();
            data.DecodeTagsFor(this);
            return this;
        }
        
    }

#endregion

    public static class STDExtensions {

        public static List<Type> TryGetDerrivedClasses (this Type t)
        {
            List<Type> tps = null;
            var att = t.ClassAttribute<DerrivedListAttribute>();
            if (att != null)
                tps = att.derrivedTypes;

            return tps;
        }

        public static string copyBufferValue;
        public static string copyBufferTag;

        public static bool LoadOnDrop<T>(this T obj) where T: ISTD
        {

#if PEGI
            UnityEngine.Object myType = null;
            if (pegi.edit(ref myType)) {
                obj.Decode(StuffLoader.LoadTextAsset(myType));

                ("Loaded " + myType.name).showNotification();

                return true;
            }
#endif
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
                var ret = Activator.CreateInstance<T>();

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

        public static StdEncoder EncodeUnrecognized(this IKeepUnrecognizedSTD ur) => ur.UnrecognizedSTD.GetAll();
  
    }
}