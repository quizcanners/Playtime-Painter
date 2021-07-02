using System;
using System.Collections.Generic;
using System.Globalization;
using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuizCanners.Migration
{

    #region Interfaces

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration

    public interface ICfgDecode
    {
        void Decode(string key, CfgData data);
    }
    
    public interface ICfg : ICfgDecode
    {
        CfgEncoder Encode();
    }

    public interface ICfgCustom : ICfg
    {
        void Decode(CfgData data);
    }
    
    public interface ICanBeDefaultCfg: ICfg {
        bool IsDefault { get; }
    }

    #endregion

    #region Config

    [Serializable]
    public struct CfgData : IPEGI
    {
        [HideInInspector] [SerializeField] private string _value;

        public override string ToString() => _value;

        public bool IsEmpty => _value.IsNullOrEmpty();

        public void Clear() => _value = null;

        public CfgData(string val)
        {
            _value = val;
        }
        
        public void Inspect()
        {
            pegi.CopyPaste.InspectOptionsFor(ref this);

            if (_value != null)
                "{0} characters".write();
        }

        private int ToIntInternal(string text)
        {
            int variable;
            int.TryParse(text, out variable);
            return variable;
        }

        private int ToIntFromTextSafe(string text, int defaultReturn)
        {
            int res;
            return int.TryParse(text, out res) ? res : defaultReturn;
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
                    case "i0": b.boneIndex0 = d.ToInt(); break;
                    case "w0": b.weight0 = d.ToFloat(); break;

                    case "i1": b.boneIndex1 = d.ToInt(); break;
                    case "w1": b.weight1 = d.ToFloat(); break;

                    case "i2": b.boneIndex2 = d.ToInt(); break;
                    case "w2": b.weight2 = d.ToFloat(); break;

                    case "i3": b.boneIndex3 = d.ToInt(); break;
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

        public int ToInt(int defaultValue = 0)
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

        public T ToEnum<T>(T defaultValue = default(T), bool ignoreCase = true) where T : struct
        {
            T tmp;
            if (Enum.TryParse(_value, ignoreCase: ignoreCase, out tmp))
            {
                return tmp;
            } else
            {
                tmp = defaultValue;
            }

            return tmp;
        }

        #region Arrays
        /*
        public T[] Decode_Array<T>(out T[] l) where T : class, ICfgCustom, new()
        {
            var cody = new CfgDecoder(this);

            l = null;

            var tmpList = new List<T>();

            var ind = 0;

            foreach (var tag in cody)
            {
                var d = cody.GetData();

                if (tag == "len")
                    l = new T[d.ToInt()];
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
        */
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
                    l = new Matrix4x4[d.ToInt()];
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

        #region Tagged Types Internal
        private T Decode<T>(string tagAsTypeIndex, TaggedTypesCfg tps) where T : ICfg
        {
            if (tagAsTypeIndex == CfgEncoder.NullTag) return default;

            var type = tps.TaggedTypes.TryGet(tagAsTypeIndex);

            if (type != null)
                return Decode<T>(type);

            return default;
        }

        private T Decode<T>(string tagAsTypeIndex, List<Type> tps) where T : ICfg
        {
            if (tagAsTypeIndex == CfgEncoder.NullTag) return default;

            var type = tps.TryGet(ToIntFromTextSafe(tagAsTypeIndex , - 1));

            if (type != null)
                return Decode<T>(type);

            return tagAsTypeIndex == CfgDecoder.ListElementTag ? Decode<T>(tps[0]) : default;
        }
        #endregion

        #region Decodey To Type

        public void Decode<T>(out T val, TaggedTypesCfg typeList) where T : IGotClassTag, ICfg
        {
            val = default;

            var cody = new CfgDecoder(_value);

            var type = typeList.TaggedTypes.TryGet(cody.GetNextTag());

            if (type != null)
                val = cody.GetData().Decode<T>(type);
        }

        public T Decode<T>() where T : ICfg, new()
        {
            var val = new T();
            DecodeFull(ref val);
            return val;
        }

        public void Decode<T>(out T val) where T : ICfg, new()
        {
            val = new T();
            DecodeFull(ref val);
        }

        public void DecodeFull<T>(ref T obj) where T : ICfg
        {
            var cstm = obj as ICfgCustom;

            if (cstm != null)
                cstm.Decode(this);
            else
                new CfgDecoder(this).DecodeTagsFor(ref obj);
        }

        private T Decode<T>(Type childType) where T : ICfg
        {
            var val = (T)Activator.CreateInstance(childType);
             DecodeFull(ref val);
            return val;
        }

        #endregion
        
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

        #region List

        private const string ListTag = "_lst";
        private const string ListMetaTag = "_lstMeta";

        private void ToListInternal<T>(List<T> list, CfgDecoder overCody, TaggedTypesCfg tps) where T : ICfg
        {
            var dta = overCody.GetData();
            var tag = overCody.CurrentTag;

            if (tag == ListMetaTag)
                return;

            if (tag == ListTag)
            {
                var cody = new CfgDecoder(dta);

                foreach (var _ in cody)
                    ToListInternal(list, cody, tps);
            }
            else
                list.Add(dta.Decode<T>(tag, tps));
        }

        private void ToListInternal<T>(List<T> list, CfgDecoder overCody, List<Type> tps) where T : ICfg
        {
            var dta = overCody.GetData();
            var tag = overCody.CurrentTag;

            if (tag == ListMetaTag)
                return;

            if (tag == ListTag)
            {
                var cody = new CfgDecoder(dta);

                foreach (var _ in cody)
                    ToListInternal(list, cody, tps);
            } else 
                list.Add(dta.Decode<T>(tag, tps));
        }

        private void ToListInternal<T>(List<T> list, CfgDecoder overCody) where T : ICfg, new()
        {
            var dta = overCody.GetData();
            var tag = overCody.CurrentTag;

            if (tag == ListMetaTag)
                return;

            if (tag == ListTag)
            {
                var cody = new CfgDecoder(dta);

                foreach (var _ in cody)
                    ToListInternal(list, cody);
            }
            else
                list.Add(dta.Decode<T>());
        }

        public List<List<T>> Decode_ListOfList<T>(out List<List<T>> l) where T : ICfg, new()
        {
            l = new List<List<T>>();

            var cody = new CfgDecoder(this);

            while (cody.GotData)
            {
                cody.GetNextTag();
                List<T> el;
                cody.GetData().ToList(out el);
                l.Add(el);
            }

            return l;
        }

        public void ToList_Derrived<T>(out List<T> list) where T : ICfg
        {
            list = new List<T>();

            var cody = new CfgDecoder(this);

            var tps = ICfgExtensions.TryGetDerivedClasses(typeof(T));

            if (tps != null)
                foreach (string _ in cody)
                    ToListInternal(list, cody, tps);
            else
                Debug.LogError("{0} doesn't have Derrived classes".F(typeof(T).ToPegiStringType()));

        }

        public void ToList<T>(out List<T> list) where T : ICfg, new()
        {
            list = new List<T>();

            var cody = new CfgDecoder(this);

            foreach (var _ in cody)
                ToListInternal(list, cody);
        }

        public void ToList<T>(out List<T> l, TaggedTypesCfg tps) where T : ICfg
        {
            var cody = new CfgDecoder(_value);

            l = new List<T>();

            foreach (var _ in cody)
               ToListInternal(l, cody, tps); //l.Add(cody.GetData().Decode<T>(tag, tps)); 
        }
        
        public List<string> ToList()
        {
            List<string> list = new List<string>();

            var cody = new CfgDecoder(this);

            foreach (var _ in cody)
                list.Add(cody.GetData().ToString());
            
            return list;
        }
        
        public List<string> ToList(out List<string> l)
        {

            l = new List<string>();

            var cody = new CfgDecoder(_value);

            foreach (var _ in cody)
                l.Add(cody.GetData().ToString());

            return l;
        }

        public List<int> ToList(out List<int> l)
        {

            l = new List<int>();

            var cody = new CfgDecoder(_value);

            foreach (var _ in cody)
                l.Add(cody.GetData().ToInt());

            return l;
        }

        public List<float> ToList(out List<float> l)
        {

            l = new List<float>();

            var cody = new CfgDecoder(_value);

            foreach (var _ in cody)
                l.Add(cody.GetData().ToFloat());


            return l;
        }

        public List<uint> ToList(out List<uint> l)
        {

            l = new List<uint>();

            var cody = new CfgDecoder(_value);

            foreach (var _ in cody)
                l.Add(cody.GetData().ToUInt());


            return l;
        }

        public List<Color> ToList(out List<Color> l)
        {

            l = new List<Color>();

            var cody = new CfgDecoder(_value);

            foreach (var _ in cody)
                l.Add(cody.GetData().ToColor());


            return l;
        }

        #endregion

        #region Dictionary

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

        public void Decode_Dictionary(out Dictionary<string, CfgData> dic)
        {
            var cody = new CfgDecoder(_value);

            dic = new Dictionary<string, CfgData>();

            while (cody.GotData)
                dic.Add(cody.GetNextTag(), cody.GetData());
        }
        
        public void Decode_Dictionary<T>(out Dictionary<string, T> dic) where T : class, ICfg, new()
        {
            var cody = new CfgDecoder(_value);

            dic = new Dictionary<string, T>();

            while (cody.GotData)
            {
                var val = new T();
                var tag = cody.GetNextTag();
                val.DecodeFull(cody.GetData());
                dic.Add(tag, val);
            }

        }
        
        #endregion
    }

#endregion

    #region Extensions
    public static class ICfgExtensions {

        private const string StdStart = "<-<-<";
        private const string StdEnd = ">->->";
        
        public static void Decode(this ICfg cfg, string rawData) => cfg.DecodeFull(new CfgData(rawData));

        public static void EmailData(this ICfg cfg, string subject, string note)
        {
            if (cfg == null) return;

            QcUnity.SendEmail ( "somebody@gmail.com", subject, 
                "{0} {1} Copy this entire email and paste it in the corresponding field on your side to paste it (don't change data before pasting it). {2} {3}{4}{5}".F(note, pegi.EnvironmentNl, pegi.EnvironmentNl,
                StdStart,  cfg.Encode().ToString(), StdEnd ) ) ;
        }

        public static void DecodeFromExternal(this ICfg cfg, string rawData) => cfg?.DecodeFull(ClearFromExternal(rawData));
        
        private static CfgData ClearFromExternal(string data) {

            if (!data.Contains(StdStart)) return new CfgData(data);
            
            var start = data.IndexOf(StdStart, StringComparison.Ordinal) + StdStart.Length;
            var end = data.LastIndexOf(StdEnd, StringComparison.Ordinal);

            data = data.Substring(start, end - start);
                
            return new CfgData(data);

        }
        
        private static ICfg _toCopy;
        
        public static bool InspectCfgCopyPaste(this ICfg cfg) {
            
            if (cfg == null) 
                return false;
            
            var changed = pegi.ChangeTrackStart();
            
            if (_toCopy == null && icon.Copy.Click("Copy {0}".F(cfg.GetNameForInspector())))
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

        public static void TryCopy_Std_AndOtherData(object from, object into)
        {
            if (into == null || into == from) return;
            
            var intoStd = into as ICfgCustom;
            
            if (intoStd != null)
            {
                var fromStd = from as ICfgCustom;

                if (fromStd != null)
                {
                    intoStd.Decode(new CfgData(fromStd.Encode().ToString()));
                }
            }

            var ch = into as ICanChangeClass;
            if (ch != null && !QcUnity.IsNullOrDestroyed_Obj(from))
                ch.OnClassTypeChange(from);
        }

        public static List<Type> TryGetDerivedClasses(Type t)
        {
            var tps = t.TryGetClassAttribute<DerivedListAttribute>()?.derivedTypes;
            if (tps == null || tps.Count == 0)
                return null;
            return tps;
        }
        public static string copyBufferValue;
        public static string copyBufferTag;

        public static bool DropStringObject(out string txt) {

            txt = null;

            Object myType = null;
            if (pegi.edit(ref myType)) {
                txt = QcFile.Load.TryLoadAsTextAsset(myType, asBytes: true);
                pegi.GameView.ShowNotification("Loaded " + myType.name);

                return true;
            }
            return false;
        }

        public static bool LoadCfgOnDrop<T>(this T obj) where T: ICfg
        {
            string txt;
            if (DropStringObject(out txt)) {
               new CfgData(txt).DecodeFull(ref obj);
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

        public static ICfg SaveToPersistentPath(this ICfg s, string path, string filename)
        {
            QcFile.Save.ToPersistentPath.String(path, filename, s.Encode().ToString(), asBytes: true);
            return s;
        }

        public static bool LoadFromPersistentPath(this ICfg s, string path, string filename)
        {
            var data = QcFile.Load.FromPersistentPath.String(path, filename, asBytes: true);
            if (data != null)
            {
                s.DecodeFull(new CfgData(data));
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
                new CfgData(load).DecodeFull(ref s);
            }
            catch (Exception ex)
            {
                Debug.LogError("Couldn't Decode: {0}".F(load));
                Debug.LogException(ex);
                return false;
            }

            return true;
        }

        public static T LoadFromResources<T>(this T s, string subFolder, string file)where T:ICfg, new() {
			if (s == null)
				s = new T ();
			new CfgData(QcFile.Load.FromResources(subFolder, file, asBytes: true)).DecodeFull(ref s);
			return s;
		}

       // public static CfgEncoder EncodeUnrecognized(this IKeepUnrecognizedCfg ur) => ur.UnrecognizedStd.Encode();
        
       // public static bool Decode(this ICfg cfg, string data, ICfgSerializeNestedReferences keeper) => data.DecodeInto(cfg, keeper);
    }
#endregion
}