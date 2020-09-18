using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using PlayerAndEditorGUI;
using PlaytimePainter.MeshEditing;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuizCannersUtilities {

    #region Interfaces

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration

   /* public interface ICfg {
        CfgEncoder Encode(); 
        void Decode(string data);
        bool Decode(string key, string data);
    }*/

    public interface ICfgDecode
    {
        void Decode(string key, CfgData data);
    }

    public interface ICfg : ICfgDecode
    {
        CfgEncoder Encode();
        void Decode(CfgData data);
    }
    
    public interface ICanBeDefaultCfg: ICfg {
        bool IsDefault { get; }
    }

    #endregion

    #region Config 2
    public struct CfgData : IPEGI
    {
        private string _value;

        public override string ToString() => _value;

        public bool IsEmpty => _value.IsNullOrEmpty();

        public CfgData(string val)
        {
            _value = val;
        }
        
        public bool Inspect()
        {
            var changed = false;

            pegi.editBig(ref _value).nl(ref changed);

            return changed;
        }

        private int ToIntInternal(string text)
        {
            int variable;
            int.TryParse(text, out variable);
            return variable;
        }

        #region Decoding Base Values
        public BoneWeight ToBoneWeight()
        {
            var cody = new CfgDecoder(_value);
            var b = new BoneWeight();

            foreach (var t in cody)
            {
                var d = cody.GetData();
                switch (t)
                {
                    case "i0": b.boneIndex0 = d.ToInt(0); break;
                    case "w0": b.weight0 = d.ToFloat(); break;

                    case "i1": b.boneIndex1 = d.ToInt(0); break;
                    case "w1": b.weight1 = d.ToFloat(); break;

                    case "i2": b.boneIndex2 = d.ToInt(0); break;
                    case "w2": b.weight2 = d.ToFloat(); break;

                    case "i3": b.boneIndex3 = d.ToInt(0); break;
                    case "w3": b.weight3 = d.ToFloat(); break;
                }
            }
            return b;
        }

        public Matrix4x4 ToMatrix4X4()
        {
            var cody = new CfgDecoder(_value);
            var m = new Matrix4x4();

            foreach (var t in cody)
            {
                var d = cody.GetData();
                switch (t)
                {

                    case "00": m.m00 = d.ToFloat(); break;
                    case "01": m.m01 = d.ToFloat(); break;
                    case "02": m.m02 = d.ToFloat(); break;
                    case "03": m.m03 = d.ToFloat(); break;

                    case "10": m.m10 = d.ToFloat(); break;
                    case "11": m.m11 = d.ToFloat(); break;
                    case "12": m.m12 = d.ToFloat(); break;
                    case "13": m.m13 = d.ToFloat(); break;

                    case "20": m.m20 = d.ToFloat(); break;
                    case "21": m.m21 = d.ToFloat(); break;
                    case "22": m.m22 = d.ToFloat(); break;
                    case "23": m.m23 = d.ToFloat(); break;

                    case "30": m.m30 = d.ToFloat(); break;
                    case "31": m.m31 = d.ToFloat(); break;
                    case "32": m.m32 = d.ToFloat(); break;
                    case "33": m.m33 = d.ToFloat(); break;

                    default: Debug.Log("Unknown component: " + t); break;
                }
            }
            return m;
        }

        public Quaternion ToQuaternion()
        {

            var cody = new CfgDecoder(_value);

            var q = new Quaternion();

            foreach (var t in cody)
            {
                var d = cody.GetData();
                switch (t)
                {
                    case "x": q.x = d.ToFloat(); break;
                    case "y": q.y = d.ToFloat(); break;
                    case "z": q.z = d.ToFloat(); break;
                    case "w": q.w = d.ToFloat(); break;
                    default: Debug.Log("Unknown component: " + cody.GetType()); break;
                }
            }
            return q;
        }

        public Vector4 ToVector4()
        {

            var cody = new CfgDecoder(_value);

            var v4 = new Vector4();

            foreach (var t in cody)
            {
                var d = cody.GetData();
                switch (t)
                {
                    case "x": v4.x = d.ToFloat(); break;
                    case "y": v4.y = d.ToFloat(); break;
                    case "z": v4.z = d.ToFloat(); break;
                    case "w": v4.w = d.ToFloat(); break;
                    default: Debug.Log("Unknown component: " + t); break;
                }
            }
            return v4;
        }

        public Vector3 ToVector3()
        {

            var cody = new CfgDecoder(_value);

            var v3 = new Vector3();

            foreach (var t in cody)
            {
                var d = cody.GetData();
                switch (t)
                {
                    case "x": v3.x = d.ToFloat(); break;
                    case "y": v3.y = d.ToFloat(); break;
                    case "z": v3.z = d.ToFloat(); break;
                }
            }
            return v3;
        }

        public Vector2 ToVector2()
        {

            var cody = new CfgDecoder(_value);

            var v2 = new Vector3();

            foreach (var t in cody)
            {
                var d = cody.GetData();
                switch (t)
                {
                    case "x": v2.x = d.ToFloat(); break;
                    case "y": v2.y = d.ToFloat(); break;
                }
            }
            return v2;
        }

        public Rect ToRect()
        {
            var cody = new CfgDecoder(_value);

            var rect = new Rect();

            foreach (var t in cody)
            {
                var d = cody.GetData();
                switch (t)
                {
                    case "pos": rect.position = d.ToVector2(); break;
                    case "size": rect.size = d.ToVector2(); break;
                }
            }
            return rect;
        }
        
        public bool ToBool() => _value == CfgEncoder.IsTrueTag;

        public bool ToBool(string yesTag) => _value == yesTag;
        
        public void ToInt(ref int value)
        {
            int variable;
            if (int.TryParse(_value, out variable))
                value = variable;
        }

        public int ToInt(int defaultValue)
        {
            int variable;
            return int.TryParse(_value, out variable) ? variable : defaultValue;
        }

        public uint ToUInt()
        {
            uint value;
            uint.TryParse(_value, out value);
            return value;
        }

        public float ToFloat()
        {
            float val;
            float.TryParse(_value, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out val);
            return val;
        }


        public Color ToColor()
        {
            var cody = new CfgDecoder(_value);
            var c = new Color();
            foreach (var t in cody)
            {
                var d = cody.GetData();
                switch (t)
                {
                    case "r": c.r = d.ToFloat(); break;
                    case "g": c.g = d.ToFloat(); break;
                    case "b": c.b = d.ToFloat(); break;
                    case "a": c.a = d.ToFloat(); break;
                    default:
                        cody.GetData(); break;
                }
            }

            return c;
        }

        #endregion

        #region Arrays
        public T[] Decode_Array<T>(out T[] l) where T : ICfg, new()
        {
            var cody = new CfgDecoder(this);

            l = null;

            var tmpList = new List<T>();

            var ind = 0;

            foreach (var tag in cody)
            {
                var d = cody.GetData();

                if (tag == "len")
                    l = new T[d.ToInt(0)];
                else
                {
                    var isNull = tag == CfgEncoder.NullTag;

                    var obj = isNull ? default : d.Decode<T>();

                    if (l != null)
                        l[ind] = obj;
                    else
                        tmpList.Add(obj);

                    ind++;
                }
            }

            return l ?? tmpList.ToArray();
        }

        public Matrix4x4[] Decode_Array(out Matrix4x4[] l)
        {

            var cody = new CfgDecoder(this);

            l = null;

            var tmpList = new List<Matrix4x4>();

            var ind = 0;

            foreach (var tag in cody)
            {
                var d = cody.GetData();

                if (tag == "len")
                    l = new Matrix4x4[d.ToInt(0)];
                else
                {
                    var obj = d.ToMatrix4X4();

                    if (l != null)
                        l[ind] = obj;
                    else
                        tmpList.Add(obj);

                    ind++;
                }
            }

            return l ?? tmpList.ToArray();
        }
        #endregion

        #region InternalDecode

        private T DecodeData<T>(CfgDecoder cody, TaggedTypesCfg tps, ListMetaData ld) where T : IGotClassTag
            => Decode<T>(cody.CurrentTag, cody.GetData(), tps, ld, cody.currentTagIndex);

        private T DecodeData<T>(CfgDecoder cody, TaggedTypesCfg tps) where T : IGotClassTag
             => Decode<T>(cody.CurrentTag, cody.GetData(), tps);

        private T DecodeData<T>(CfgDecoder cody, List<Type> tps, ListMetaData ld) where T : ICfg
            => Decode<T>(cody.CurrentTag, cody.GetData(), tps, ld, cody.currentTagIndex);

        private T DecodeData<T>(CfgDecoder cody, List<Type> tps) where T : ICfg
            => Decode<T>(cody.CurrentTag, cody.GetData(), tps);

        private T Decode<T>(string tagAsTypeIndex, CfgData data, TaggedTypesCfg tps, ListMetaData ld, int index) where T : IGotClassTag
        {

            if (tagAsTypeIndex == CfgEncoder.NullTag) return default;

            var type = tps.TaggedTypes.TryGet(tagAsTypeIndex);

            if (type != null)
                return data.Decode<T>(type);

            return default;
        }

        private static T Decode<T>(string tagAsTypeIndex, CfgData data, TaggedTypesCfg tps) where T : IGotClassTag
        {

            if (tagAsTypeIndex == CfgEncoder.NullTag) return default;

            var type = tps.TaggedTypes.TryGet(tagAsTypeIndex);

            return (type == null) ? default : data.Decode<T>(type);
        }

        private static T Decode<T>(string tagAsTypeIndex, CfgData data, List<Type> tps, ListMetaData ld, int index) where T : ICfg
        {

            if (tagAsTypeIndex == CfgEncoder.NullTag) return default;

            var type = tps.TryGet(tagAsTypeIndex.ToIntFromTextSafe(-1));

            if (type != null)
                return data.Decode<T>(type);

            // ld.elementDatas[index].Unrecognized(tagAsTypeIndex, data);


            return default;
        }

        private static T Decode<T>(string tagAsTypeIndex, CfgData data, List<Type> tps) where T : ICfg
        {


            if (tagAsTypeIndex == CfgEncoder.NullTag) return default;

            var type = tps.TryGet(tagAsTypeIndex.ToIntFromTextSafe(-1));

            if (type != null)
                return data.Decode<T>(type);

            return tagAsTypeIndex == CfgDecoder.ListElementTag ? data.Decode<T>(tps[0]) : default;
        }
        #endregion

        public void ToList<T>(out List<T> list, ref ListMetaData ld) where T : ICfg, new()
        {
            list = new List<T>();

            if (ld == null)
                ld = new ListMetaData();

            var tps = typeof(T).TryGetDerivedClasses();

            var overCody = new CfgDecoder(this);
            foreach (var tag in overCody)
            {

                switch (tag)
                {

                    case CfgEncoder.ListMetaTag: ld.Decode(overCody.GetData()); break;

                    case CfgEncoder.ListTag:
                        var cody = new CfgDecoder(overCody.GetData());
                        if (tps != null)
                            foreach (var t in cody)
                                list.Add(DecodeData<T>(cody, tps, ld)); // Probably something off here (Decoding list of sub nodes)
                        else foreach (var t in cody)
                            list.Add(cody.GetData().Decode<T>());
                        break;

                    default: list.Add((tps != null) ? DecodeData<T>(overCody, tps, ld) : overCody.GetData().Decode<T>()); break;
                }
            }
        }
        
        public void ToList<T>(out List<T> list) where T : ICfg, new()
        {
            list = new List<T>();

            var cody = new CfgDecoder(this);

            var tps = typeof(T).TryGetDerivedClasses();

            if (tps != null)
                foreach (var tag in cody)
                    list.Add(DecodeData<T>(cody, tps));
            else foreach (var tag in cody)
                list.Add(cody.GetData().Decode<T>());
        }

        public void Decode_List<T>(out List<T> l, TaggedTypesCfg tps) where T : IGotClassTag
        {
            var cody = new CfgDecoder(_value);

            l = new List<T>();

            foreach (var tag in cody)
                l.Add(DecodeData<T>(cody, tps));
        }

        public void Decode_List<T>(out List<T> l, ref ListMetaData ld, TaggedTypesCfg tps) where T : IGotClassTag
        {
            l = new List<T>();
            if (ld == null)
                ld = new ListMetaData();

            var overCody = new CfgDecoder(_value);
            foreach (var tag in overCody)
            {
                switch (tag)
                {
                    case CfgEncoder.ListMetaTag: ld.Decode(overCody.GetData()); break;
                    case CfgEncoder.ListTag:
                        var cody = new CfgDecoder(overCody.GetData());
                        foreach (var t in cody) l.Add(DecodeData<T>(cody, tps, ld)); break;
                    default:
                        l.Add(DecodeData<T>(overCody, tps, ld));
                        break;
                }
            }
            
        }
        
        private T Decode<T>(Type childType) where T : ICfg
        {
            var val = (T)Activator.CreateInstance(childType);
            val.Decode(this);
            return val;
        }
        
        public void Decode<T>(out T val, TaggedTypesCfg typeList) where T : IGotClassTag
        {

            val = default;

            var cody = new CfgDecoder(_value);

            var type = typeList.TaggedTypes.TryGet(cody.GetNextTag());

            if (type != null)
                val = cody.GetData().Decode<T>(type);
        }
        
        public T Decode<T>(out T val) where T : ICfg, new()
        {
            val = Decode<T>();
            return val;
        }

        public T Decode<T>() where T : ICfg, new()
        {
            var obj = new T();
            obj.Decode(this);
            return obj;
        }

        public void Decode(Transform tf)
        {

            var cody = new CfgDecoder(_value);
            var local = false;

            foreach (var t in cody)
            {
                var d = cody.GetData();
                switch (t)
                {
                    case "loc": local = d.ToBool(); break;
                    case "pos": if (local) tf.localPosition = d.ToVector3(); else tf.position = d.ToVector3(); break;
                    case "size": tf.localScale = d.ToVector3(); break;
                    case "rot": if (local) tf.localRotation = d.ToQuaternion(); else tf.rotation = d.ToQuaternion(); break;
                }
            }
        }
        
        public void Decode(CfgDecoder.DecodeDelegate dec) => new CfgDecoder(this).DecodeTagsFor(dec);

        public List<string> ToList()
        {
            List<string> list;
            Decoder.Decode_List(_value, out list);
            return list;
        }
        
        public List<string> ToList(out List<string> l)
        {

            l = new List<string>();

            var cody = new CfgDecoder(_value);

            foreach (var tag in cody)
                l.Add(cody.GetData().ToString());

            return l;
        }

        public List<int> ToList(out List<int> l)
        {

            l = new List<int>();

            var cody = new CfgDecoder(_value);

            foreach (var tag in cody)
                l.Add(cody.GetData().ToInt(0));

            return l;
        }

        public List<float> ToList(out List<float> l)
        {

            l = new List<float>();

            var cody = new CfgDecoder(_value);

            foreach (var tag in cody)
                l.Add(cody.GetData().ToFloat());


            return l;
        }

        public List<uint> ToList(out List<uint> l)
        {

            l = new List<uint>();

            var cody = new CfgDecoder(_value);

            foreach (var tag in cody)
                l.Add(cody.GetData().ToUInt());


            return l;
        }

        public List<Color> ToList(out List<Color> l)
        {

            l = new List<Color>();

            var cody = new CfgDecoder(_value);

            foreach (var tag in cody)
                l.Add(cody.GetData().ToColor());


            return l;
        }

        public void Decode_Dictionary(out Dictionary<int, string> dic)
        {
            var cody = new CfgDecoder(_value);

            dic = new Dictionary<int, string>();

            while (cody.GotData)
                dic.Add(ToIntInternal(cody.GetNextTag()), cody.GetData().ToString());
        }

        public void Decode_Dictionary(out Dictionary<string, string> dic)
        {
            var cody = new CfgDecoder(_value);

            dic = new Dictionary<string, string>();

            while (cody.GotData)
                dic.Add(cody.GetNextTag(), cody.GetData().ToString());

        }

        public void Decode_Dictionary<T>(out Dictionary<string, T> dic) where T : ICfg, new()
        {
            var cody = new CfgDecoder(_value);

            dic = new Dictionary<string, T>();

            while (cody.GotData)
            {
                var val = new T();
                var tag = cody.GetNextTag();
                val.Decode(cody.GetData());
                dic.Add(tag, val);
            }

        }

     
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
    public abstract class Configuration : ICfg, IPEGI_ListInspect, IGotName
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

            if (!allowOverride && !data.IsNullOrEmpty() && icon.Delete.ClickUnFocus(ref changed))
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

        public CfgEncoder Encode() => new CfgEncoder()
            .Add_String("n", name)
            .Add_IfNotEmpty("d", data);

        public void Decode(string key, CfgData d) {
            switch (key) {
                case "n": name = d.ToString(); break;
                case "d": data = d.ToString(); break;
            }
        }

        public void Decode(CfgData data) => this.DecodeTagsFrom(data);
        
        #endregion

        public Configuration() {
            name = "New Config";
        }

        public Configuration(string name)
        {
            this.name = name;
        }

    }
    /*
    public class StdSimpleReferenceHolder : ICfgSerializeNestedReferences {
        
        public readonly List<Object> nestedReferences = new List<Object>();
        public int GetReferenceIndex(Object obj) => QcSharp.TryGetIndexOrAdd(nestedReferences, obj);

        public T GetReferenced<T>(int index) where T : Object => nestedReferences.TryGet(index) as T;

    }*/
    /*
    public class CfgReferencesHolder : ScriptableObject, ICfgSerializeNestedReferences, IPEGI, IKeepUnrecognizedCfg
    {
        
        #region Encode & Decode

        public UnrecognizedTagsList UnrecognizedStd { get; } = new UnrecognizedTagsList();

        private readonly ListMetaData _listMetaData = new ListMetaData("References");

        [SerializeField] protected List<Object> nestedReferences = new List<Object>();
        public virtual int GetReferenceIndex(Object obj) => QcSharp.TryGetIndexOrAdd(nestedReferences, obj);

        public virtual T GetReferenced<T>(int index) where T : Object => nestedReferences.TryGet(index) as T;


        public virtual CfgEncoder Encode() => this.EncodeUnrecognized()
            .Add("listDta", _listMetaData);

        public virtual void Decode(string data) => this.DecodeTagsFrom(data);

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
        
        [NonSerialized] public int inspectedItems = -1;
        private int _inspectedDebugItems = -1;
        [NonSerialized] private int inspectedReference = -1;
      
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
    */

   /* public abstract class AbstractCfg : ICanBeDefaultCfg {
        public abstract CfgEncoder Encode();
        public virtual void Decode(string data) => this.DecodeTagsFrom(data);
        public abstract bool Decode(string key, string data);

        public virtual bool IsDefault => false;
    }*/


   /* public abstract class AbstractKeepUnrecognizedCfg : AbstractCfg, IKeepUnrecognizedCfg {
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
    }*/

    public abstract class ComponentCfg : MonoBehaviour, ICanBeDefaultCfg, IPEGI, IPEGI_ListInspect, IGotName, INeedAttention {

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
                pegi.EditorView.Lock_UnlockClick(gameObject);

            if (!icon.Debug.enter(ref inspectedItems, 0).nl(ref changed))
                return changed; 
                


            "{0} Debug ".F(this.GetNameForInspector()).write(90);

            pegi.toggleDefaultInspector(this);

            if (icon.Refresh.Click("Reset Inspector"))
                ResetInspector();

            this.CopyPasteStdPegi().nl(ref changed);

            if (("Cfg Saves: " + explorer.CountForInspector()).enter(ref _inspectedDebugItems, 0).nl())
                explorer.Inspect(this);

            if (inspectedItems == -1)
                pegi.nl();

          /*  if (("Object References: " + nestedReferences.Count).enter(ref _inspectedDebugItems, 1).nl_ifNotEntered())
            {
                referencesMeta.edit_List_UObj(ref nestedReferences);
                if (!referencesMeta.Inspecting && "Clear All References".Click("Will clear the list. Make sure everything" +
                ", that usu this object to hold references is currently decoded to avoid mixups"))
                    nestedReferences.Clear();

            }*/

            if (inspectedItems == -1)
                pegi.nl();

            /*if (("Unrecognized Tags: " + UnrecognizedStd.Count).enter(ref _inspectedDebugItems, 2).nl_ifNotEntered())
                UnrecognizedStd.Nested_Inspect().changes(ref changed);*/

            if ("Inspect Inspector".enter(ref _inspectedDebugItems, 3).nl())
                QcUtils.InspectInspector();

            if (inspectedItems == -1)
                pegi.nl();
            
           
            return changed;
        }

        #endregion

        #region Encoding & Decoding

        public virtual bool IsDefault => false;

        protected ListMetaData referencesMeta = new ListMetaData("References");

        [HideInInspector]
       // [SerializeField] protected List<Object> nestedReferences = new List<Object>();
       // public int GetReferenceIndex(Object obj) => QcSharp.TryGetIndexOrAdd(nestedReferences, obj);
        
      //  public T GetReferenced<T>(int index) where T : Object => nestedReferences.TryGet(index) as T;

     //   public UnrecognizedTagsList UnrecognizedStd { get; } = new UnrecognizedTagsList();

        public virtual void Decode(string key, CfgData data)
        {
            switch (key) {
                case "db": data.ToInt(ref inspectedItems); break;
            }
        }

        public virtual CfgEncoder Encode() => new CfgEncoder().Add_IfNotNegative("db", inspectedItems) ;

        public virtual void Decode(CfgData data) {
           // UnrecognizedStd.Clear();
            this.DecodeTagsFrom(data);
        }

#endregion
    }

#endregion

#region Extensions
    public static class StdExtensions {

        private const string StdStart = "<-<-<";
        private const string StdEnd = ">->->";


        public static void Decode(this ICfg cfg, string rawData) => cfg.Decode(new CfgData(rawData));

        public static void EmailData(this ICfg cfg, string subject, string note)
        {
            if (cfg == null) return;

            QcUnity.SendEmail ( "somebody@gmail.com", subject, 
                "{0} {1} Copy this entire email and paste it in the corresponding field on your side to paste it (don't change data before pasting it). {2} {3}{4}{5}".F(note, pegi.EnvironmentNl, pegi.EnvironmentNl,
                StdStart,  cfg.Encode().ToString(), StdEnd ) ) ;
        }

        public static void DecodeFromExternal(this ICfg cfg, string rawData) => cfg?.Decode(ClearFromExternal(rawData));
        
        private static CfgData ClearFromExternal(string data) {

            if (!data.Contains(StdStart)) return new CfgData(data);
            
            var start = data.IndexOf(StdStart, StringComparison.Ordinal) + StdStart.Length;
            var end = data.LastIndexOf(StdEnd, StringComparison.Ordinal);

            data = data.Substring(start, end - start);
                
            return new CfgData(data);

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

        public static bool SendReceivePegi(this ICfg cfg, string name, string folderName, out CfgData cfgData) {
  
            if (icon.Email.Click("Send {0} to somebody via email.".F(folderName)))
                cfg.EmailData(name, "Use this {0}".F(name));

            var data = "";
            if (pegi.edit(ref data).UnFocusIfTrue()) {
                cfgData = ClearFromExternal(data);
                return true;
            }

            if (icon.Folder.Click("Save {0} to the file".F(name))) {
                cfg.SaveToAssets(folderName, name);
                QcUnity.RefreshAssetDatabase();
            }

            if (DropStringObject(out data))
            {
                cfgData = new CfgData(data);
                return true;
            }

            cfgData = new CfgData();

            pegi.nl();

            return false;
        }

       // private static readonly StdSimpleReferenceHolder TmpHolder = new StdSimpleReferenceHolder();

        public static void TryCopy_Std_AndOtherData(object from, object into)
        {
            if (into == null || into == from) return;
            
            var intoStd = into as ICfg;
            
            if (intoStd != null)
            {
                var fromStd = from as ICfg;

                if (fromStd != null)
                {
                   // var prev = CfgEncoder.keeper;
                  //  CfgEncoder.keeper = TmpHolder;
                    intoStd.Decode(new CfgData(fromStd.Encode().ToString()));
                  //  CfgEncoder.keeper = prev;

                    //TmpHolder.nestedReferences.Clear();
                }


            }

            var ch = into as ICanChangeClass;
            if (ch != null && !QcUnity.IsNullOrDestroyed_Obj(from))
                ch.OnClassTypeChange(from);

            
        }

/*
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
        }*/

        public static List<Type> TryGetDerivedClasses (this Type t) => t.TryGetClassAttribute<DerivedListAttribute>()?.derivedTypes.NullIfEmpty();
            
        public static string copyBufferValue;
        public static string copyBufferTag;

        public static bool DropStringObject(out string txt) {

            txt = null;

            Object myType = null;
            if (pegi.edit(ref myType)) {
                txt = QcFile.Load.TryLoadAsTextAsset(myType, useBytes: true);
                pegi.GameView.ShowNotification("Loaded " + myType.name);

                return true;
            }
            return false;
        }

        public static bool LoadCfgOnDrop<T>(this T obj) where T: ICfg
        {
            string txt;
            if (DropStringObject(out txt)) {
                obj.Decode(new CfgData(txt));
                return true;
            }

            return false;
        }

        /*
        public static void UpdateCfgPrefab (this ICfg s, GameObject go) {
            var iK = s as IKeepMyCfg;

            if (iK != null)
                iK.SaveCfgData();

            QcUnity.UpdatePrefab(go);
        }

        
        public static void SaveCfgData(this IKeepMyCfg s, bool setDirty = true) {
            if (s != null)
            {
                s.ConfigStd = s.Encode().ToString();

                var scObj = s as ScriptableObject;
                if (scObj)
                    scObj.SetToDirty();
            }
        }

        public static bool LoadCfgData(this IKeepMyCfg s)
        {
            if (s == null)
                return false;

            s.Decode(s.ConfigStd);

            return true;
        }*/

        public static ICfg SaveToAssets(this ICfg s, string path, string filename)
        {
            QcFile.Save.ToAssets(path, filename, s.Encode().ToString(), asBytes: true);
            return s;
        }

       /* public static ICfg SaveToAssets(this ICfg s, string path, string filename)
        {
            QcFile.Save.ToAssets(path, filename, s.Encode().ToString(), asBytes: true);
            return s;
        }*/

        public static ICfg SaveToPersistentPath(this ICfg s, string path, string filename)
        {
            QcFile.Save.ToPersistentPath(path, filename, s.Encode().ToString(), asBytes: true);
            return s;
        }

        public static bool LoadFromPersistentPath(this ICfg s, string path, string filename)
        {
            var data = QcFile.Load.FromPersistentPath(path, filename, asBytes: true);
            if (data != null)
            {
                s.Decode(new CfgData(data));
                return true;
            }
            return false;
        }

        public static ICfg SaveToResources(this ICfg s, string resFolderPath, string insideResPath, string filename)
        {
            QcFile.Save.ToResources(resFolderPath, insideResPath, filename, s.Encode().ToString(), asBytes: true);
            return s;
        }
        
        public static bool TryLoadFromResources<T>(this T s, string subFolder, string file) where T : ICfg
        {
            var load = QcFile.Load.FromResources(subFolder, file, asBytes: true);

            if (load == null)
                return false;

            try
            {
                s.Decode(new CfgData(load));
            }
            catch (Exception ex)
            {
                Debug.LogError("Couldn't Decode: {0}".F(load) + ex.ToString());
                return false;
            }

            return true;
        }

        public static T LoadFromResources<T>(this T s, string subFolder, string file)where T:ICfg, new() {
			if (s == null)
				s = new T ();
			s.Decode(new CfgData(QcFile.Load.FromResources(subFolder, file, asBytes: true)));
			return s;
		}

       // public static CfgEncoder EncodeUnrecognized(this IKeepUnrecognizedCfg ur) => ur.UnrecognizedStd.Encode();
        
       // public static bool Decode(this ICfg cfg, string data, ICfgSerializeNestedReferences keeper) => data.DecodeInto(cfg, keeper);
    }
#endregion
}