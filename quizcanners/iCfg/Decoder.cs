using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuizCannersUtilities {


#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration

    public static class DecodeExtensions {

        #region Non-Instancible

        public static void Decode_Base(this string data, CfgDecoder.DecodeDelegate dec, IKeepUnrecognizedCfg unrecognizedKeeper, string tag = "b") 
            => new CfgDecoder(data).DecodeTagsFor(dec, unrecognizedKeeper, tag);
        
        public static void Decode_Delegate(this string data, CfgDecoder.DecodeDelegate dec) => new CfgDecoder(data).DecodeTagsFor(dec);

        public static void DecodeInto(this string data, Transform tf)
        {

            var cody = new CfgDecoder(data);
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

        public static void DecodeInto(this string data, RectTransform tf)
        {

            var cody = new CfgDecoder(data);

            foreach (var t in cody)
            {
                var d = cody.GetData();
                switch (t)
                {
                    case "tfBase": d.DecodeInto(tf.transform); break;
                    case "aPos": tf.anchoredPosition = d.ToVector2(); break;
                    case "aPos3D": tf.anchoredPosition3D = d.ToVector3(); break;
                    case "aMax": tf.anchorMax = d.ToVector2(); break;
                    case "aMin": tf.anchorMin = d.ToVector2(); break;
                    case "ofMax": tf.offsetMax = d.ToVector2(); break;
                    case "ofMin": tf.offsetMin = d.ToVector2(); break;
                    case "pvt": tf.pivot = d.ToVector2(); break;
                    case "deSize": tf.sizeDelta = d.ToVector2(); break;
                }
            }
        }
        #endregion

        #region To Value Type

        public static BoneWeight ToBoneWeight(this string data)
        {
            var cody = new CfgDecoder(data);
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

        public static Matrix4x4 ToMatrix4X4(this string data)
        {
            var cody = new CfgDecoder(data);
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

        public static Quaternion ToQuaternion(this string data)
        {

            var cody = new CfgDecoder(data);

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

        public static Vector4 ToVector4(this string data) {

            var cody = new CfgDecoder(data);

            var v4 = new Vector4();

            foreach (var t in cody)
            {
                var d = cody.GetData();
                switch (t) {
                    case "x": v4.x = d.ToFloat(); break;
                    case "y": v4.y = d.ToFloat(); break;
                    case "z": v4.z = d.ToFloat(); break;
                    case "w": v4.w = d.ToFloat(); break;
                    default: Debug.Log("Unknown component: " + t); break;
                }
            }
            return v4;
        }

        public static Vector3 ToVector3(this string data) {

            var cody = new CfgDecoder(data);

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

        public static Vector2 ToVector2(this string data) {

            var cody = new CfgDecoder(data);

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

        public static Rect ToRect(this string data)
        {
            var cody = new CfgDecoder(data);

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

        public static bool ToBool(this string data) => data == CfgEncoder.IsTrueTag;
        
        public static int ToInt(this string data) {

            int variable;
            int.TryParse(data, out variable);

            return variable;

        }

        public static uint ToUInt(this string data) {
            uint value;
            uint.TryParse(data, out value);
            return value;
        }

        public static float ToFloat(this string data) {
            float val;
            float.TryParse(data, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out val);
            return val;
        }

        public static Color ToColor(this string data)
        {
            var cody = new CfgDecoder(data);
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

        #region To List Of Value Type
        public static List<string> Decode_List(this string data, out List<string> l)
        {

            l = new List<string>();

            var cody = new CfgDecoder(data);

            foreach (var tag in cody)
                l.Add(cody.GetData());

            return l;
        }

        public static List<int> Decode_List(this string data, out List<int> l) {

            l = new List<int>();

            var cody = new CfgDecoder(data);

            foreach (var tag in cody)
                l.Add(cody.GetData().ToInt());

            return l;
        }

        public static List<float> Decode_List(this string data, out List<float> l)
        {

            l = new List<float>();

            var cody = new CfgDecoder(data);

            foreach (var tag in cody)
                l.Add(cody.GetData().ToFloat());


            return l;
        }

        public static List<uint> Decode_List(this string data, out List<uint> l)
        {

            l = new List<uint>();

            var cody = new CfgDecoder(data);

            foreach (var tag in cody)
                l.Add(cody.GetData().ToUInt());


            return l;
        }

        public static List<Color> Decode_List(this string data, out List<Color> l)
        {

            l = new List<Color>();

            var cody = new CfgDecoder(data);

            foreach (var tag in cody)
                l.Add(cody.GetData().ToColor());


            return l;
        }
        #endregion

        #region InternalDecode

        private static T DecodeData<T>(this CfgDecoder cody, TaggedTypesCfg tps, ListMetaData ld) where T : IGotClassTag
            => Decode<T>(cody.currentTag, cody.GetData(), tps, ld, cody.currentTagIndex);

        private static T DecodeData<T>(this CfgDecoder cody, TaggedTypesCfg tps) where T : IGotClassTag
             => Decode<T>(cody.currentTag, cody.GetData(), tps);
        
        private static T DecodeData<T>(this CfgDecoder cody, List<Type> tps, ListMetaData ld) where T : ICfg
            => Decode<T>(cody.currentTag, cody.GetData(), tps, ld, cody.currentTagIndex);


        private static T DecodeData<T>(this CfgDecoder cody, List<Type> tps) where T : ICfg
            => Decode<T>(cody.currentTag, cody.GetData(), tps);

        private static T Decode<T>(string tagAsTypeIndex, string data, TaggedTypesCfg tps, ListMetaData ld, int index) where T : IGotClassTag
        {

            if (tagAsTypeIndex == CfgEncoder.NullTag) return default;

            var type = tps.TaggedTypes.TryGet(tagAsTypeIndex);
            
            if (type != null)
                return data.DecodeInto_Type<T>(type);
            
            ld.elementDatas[index].Unrecognized(tagAsTypeIndex, data);

            return default;
        }

        private static T Decode<T>(string tagAsTypeIndex, string data, TaggedTypesCfg tps) where T : IGotClassTag
        {

            if (tagAsTypeIndex == CfgEncoder.NullTag) return default;
            
            var type = tps.TaggedTypes.TryGet(tagAsTypeIndex);
            
            return (type == null) ? default : data.DecodeInto_Type<T>(type);
        }

        private static T Decode<T>(string tagAsTypeIndex, string data, List<Type> tps, ListMetaData ld, int index) where T : ICfg
        {

            if (tagAsTypeIndex == CfgEncoder.NullTag) return default;
            
            var type = tps.TryGet(tagAsTypeIndex.ToIntFromTextSafe(-1));
            
            if (type != null)
                return data.DecodeInto_Type<T>(type);
         
            ld.elementDatas[index].Unrecognized(tagAsTypeIndex, data);
            

            return default;
        }

        private static T Decode<T>(string tagAsTypeIndex, string data, List<Type> tps) where T : ICfg
        {


            if (tagAsTypeIndex == CfgEncoder.NullTag) return default;
            
            var type = tps.TryGet(tagAsTypeIndex.ToIntFromTextSafe(-1));
            
            if (type != null)
                return data.DecodeInto_Type<T>(type);
            
            return tagAsTypeIndex == CfgDecoder.ListElementTag ? data.DecodeInto_Type<T>(tps[0]) : default;
        }
        #endregion

        #region Cfg List
        
        public static bool TryDecode_IntoList_Elements<T>(this string data, List<T> val)
        {

            if (val == null) return false;
            
            var cody = new CfgDecoder(data);

            var index = 0;

            foreach (var t in cody) {

                if (index >= val.Count)
                    return true;
  
                cody.GetData().TryDecodeInto(val[index]);
                index++;
            }
            
            return true;
            
        }

        public static List<T> TryDecode_IntoList_Elements<T>(this string data, List<T> l, ref ListMetaData ld) where T : ICfg, new() {

            if (ld == null)
                ld = new ListMetaData();

            var overCody = new CfgDecoder(data);
            var index = 0;

            foreach (var tag in overCody)  {

                switch (tag) {

                    case CfgEncoder.ListMetaTag: ld.Decode(overCody.GetData()); break;

                    case CfgEncoder.ListTag:
                        var cody = new CfgDecoder(overCody.GetData());

                        foreach (var t in cody) {

                            var d = cody.GetData();

                            if (index >= l.Count || !d.TryDecodeInto(l[index]))
                                ld.elementDatas[index].Unrecognized(tag, d);

                            index++;
                        }
                        break;

                    default:
                        var d1 = overCody.GetData();

                        if (index >= l.Count || !d1.TryDecodeInto(l[index]))
                            ld.elementDatas[index].Unrecognized(tag, d1);

                        index++;
                        break;
                }
            }

            return l;
        }
        
        public static List<List<T>> Decode_ListOfList<T>(this string data, out List<List<T>> l) where T : ICfg, new()
        {
            l = new List<List<T>>();

            var cody = new CfgDecoder(data);

            while (cody.GotData) {
                cody.GetNextTag();
                List<T> el;
                cody.GetData().Decode_List(out el);
                l.Add(el);
            }

            return l;
        }
        
        public static List<T> Decode_List<T>(this string data, out List<T> l, ref ListMetaData ld) where T : ICfg, new() {

            if (ld == null)
                ld = new ListMetaData();
            l = new List<T>();

            var tps = typeof(T).TryGetDerivedClasses();
       
            var overCody = new CfgDecoder(data);
            foreach (var tag in overCody) {

                switch (tag) {

                    case CfgEncoder.ListMetaTag: ld.Decode(overCody.GetData()); break;
                        
                    case CfgEncoder.ListTag:
                        var cody = new CfgDecoder(overCody.GetData());
                        if (tps != null)
                            foreach (var t in cody)
                                l.Add(cody.DecodeData<T>(tps, ld)); // Probably something off here (Decoding list of sub nodes)
                        else foreach (var t in cody)
                                l.Add(cody.GetData().DecodeInto<T>());
                        break;

                    default: l.Add( (tps != null) ? overCody.DecodeData<T>(tps,ld) : overCody.GetData().DecodeInto<T>()); break;
                }
            }

            return l;
        }

        public static List<T> Decode_List_Derived<T>(this string data, out List<T> l, ref ListMetaData ld) where T : ICfg
        {

            if (ld == null)
                ld = new ListMetaData();
            l = new List<T>();

            var tps = typeof(T).TryGetDerivedClasses();

            var overCody = new CfgDecoder(data);
            foreach (var tag in overCody)
            {

                switch (tag)
                {

                    case CfgEncoder.ListMetaTag: ld.Decode(overCody.GetData()); break;

                    case CfgEncoder.ListTag:
                        var cody = new CfgDecoder(overCody.GetData());
                        if (tps != null)
                            foreach (var t in cody)
                                l.Add(cody.DecodeData<T>(tps, ld));
                        break;

                    default: l.Add(overCody.DecodeData<T>(tps, ld)); break;
                }
            }

            return l;
        }
        
        public static List<T> Decode_List<T>(this string data, out List<T> l) where T : ICfg, new() {

            var cody = new CfgDecoder(data);

            l = new List<T>();

            var tps = typeof(T).TryGetDerivedClasses();

            if (tps != null) 
                foreach (var tag in cody)
                    l.Add(cody.DecodeData<T>(tps)); 
            else foreach (var tag in cody)
                    l.Add(cody.GetData().DecodeInto<T>());

            return l;
        }
        #endregion

        #region Arrays
        public static T[] Decode_Array<T>(this string data, out T[] l) where T : ICfg, new()
        {

            var cody = new CfgDecoder(data);

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

                    var obj = isNull ? default : d.DecodeInto<T>();

                    if (l != null)
                        l[ind] = obj;
                    else
                        tmpList.Add(obj);

                    ind++;
                }
            }

            return l ?? tmpList.ToArray();
        }

        public static Matrix4x4[] Decode_Array(this string data, out Matrix4x4[] l)
        {

            var cody = new CfgDecoder(data);

            l = null;

            var tmpList = new List<Matrix4x4>();

            var ind = 0;

            foreach (var tag in cody)
            {
                var d = cody.GetData();

                if (tag == "len")
                    l = new Matrix4x4[d.ToInt()];
                else {
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

        #region Abstract 

      /*  public static List<T> Decode_List_Abstract<T>(this string data, out List<T> l, TaggedTypesCfg taggedTypes) where T : IGotClassTag
        {

            l = new List<T>();
            
            #if UNITY_EDITOR

            var ct = taggedTypes.CoreType;
            var lt = typeof(T);
            if (ct!=lt &&   !lt.IsSubclassOf(ct)) {
                Debug.LogError("Type of {0} is not a subclass of {1}".F(lt.ToPegiStringType(), ct.ToPegiStringType()));
                return l;
            }
            #endif

            var cody = new CfgDecoder(data);
            
            foreach (var tag in cody)
            {
                var tmp = cody.DecodeData<T>(taggedTypes);
                if (tmp != null)
                    l.Add(tmp);
            }

            return l;
        }*/

        public static List<T> Decode_List<T>(this string data, out List<T> l, TaggedTypesCfg tps) where T : IGotClassTag {
            var cody = new CfgDecoder(data);

            l = new List<T>();

            foreach (var tag in cody)
                l.Add(cody.DecodeData<T>(tps));

            return l;
        }

        public static List<T> Decode_List<T>(this string data, out List<T> l, ref ListMetaData ld, TaggedTypesCfg tps) where T : IGotClassTag
        {
            l = new List<T>();
            if (ld == null)
                ld = new ListMetaData();

            var overCody = new CfgDecoder(data);
            foreach (var tag in overCody) {
                switch (tag) {
                    case CfgEncoder.ListMetaTag: ld.Decode(overCody.GetData()); break;
                    case CfgEncoder.ListTag:
                        var cody = new CfgDecoder(overCody.GetData());
                            foreach (var t in cody) l.Add(cody.DecodeData<T>(tps, ld));   break;
                    default: l.Add(overCody.DecodeData<T>(tps, ld));
                    break;
                }
            }
 
            return l;
        }

        #endregion

        #region Dictionary
        public static void Decode_Dictionary(this string data, out Dictionary<int, string> dic)
        {
            var cody = new CfgDecoder(data);

            dic = new Dictionary<int, string>();

            while (cody.GotData)
                dic.Add(cody.GetNextTag().ToInt(), cody.GetData());

        }

        public static void Decode_Dictionary(this string data, out Dictionary<string, string> dic)
        {
            var cody = new CfgDecoder(data);

            dic = new Dictionary<string, string>();

            while (cody.GotData)
                dic.Add(cody.GetNextTag(), cody.GetData());

        }
        
        public static void Decode_Dictionary<T>(this string data, out Dictionary<string, T> dic) where T: ICfg, new() 
        {
            var cody = new CfgDecoder(data);

            dic = new Dictionary<string, T>();

            while (cody.GotData)
            {
                var val = new T();
                var tag = cody.GetNextTag();
                val.Decode(cody.GetData());
                dic.Add(tag, val);
            }

        }
        #endregion

        #region CFG class

        public static ICfg DecodeTagsFrom<T>(this T obj, string data) where T : class, ICfg
            => (QcUnity.IsNullOrDestroyed_Obj(obj)) ? obj : new CfgDecoder(data).DecodeTagsFor(obj);
        
        public static T DecodeInto<T>(this string data, out T val) where T : ICfg, new()
        {
            val = data.DecodeInto<T>();
            return val;
        }

        public static T DecodeInto<T>(this string data) where T : ICfg, new()
        {
            var obj = new T();
            obj.Decode(data);
            return obj;
        }

        public static bool TryDecodeInto<T>(this string data, T val) =>
            QcUnity.TryGet_fromObj<ICfg>(val).Decode_ifNotNull(data);
        
        public static bool Decode_ifNotNull(this ICfg istd, string data)
        {
            if (istd == null) return false;
            
            istd.Decode(data);
            return true;
           
        }

        public static T TryDecodeInto<T>(this string data) where T : new()
        {
            var obj = new T();
            data.TryDecodeInto(obj);
            return obj;
        }

        public static T DecodeInto_Type<T>(this string data, Type childType) where T : ICfg
        {
            var val = (T)Activator.CreateInstance(childType);
            val.Decode(data);
            return val;
        }

        public static T TryDecodeInto_Type<T>(this string data, Type childType)
        {
            var val = (T)Activator.CreateInstance(childType);

            (val as ICfg).Decode_ifNotNull(data);

            return val;
        }

        public static T TryDecodeInto<T>(this ICfg ovj, Type childType)
        {
            var val = (T)Activator.CreateInstance(childType);

            if (ovj == null) return val;
            
            var std = val as ICfg;

            if (std == null) return val;
            
            std.Decode(ovj.Encode().ToString()); 
            
            return val;
        }
    
        public static void Decode<T>(this string data, out T val, TaggedTypesCfg typeList) where T : IGotClassTag
        {

            val = default;

            var cody = new CfgDecoder(data);

            var type = typeList.TaggedTypes.TryGet(cody.GetNextTag());

            if (type != null)
                val = cody.GetData().DecodeInto_Type<T>(type);
        }
        #endregion
        
        #region Into Unity Objects
        public static ICfgSerializeNestedReferences Keeper { get { return CfgEncoder.keeper;  } set { CfgEncoder.keeper = value; } }

        public static bool TryDecodeInto<T>(this string data, T val, ICfgSerializeNestedReferences referencesKeeper) {
            var std = QcUnity.TryGet_fromObj<ICfg>(val);
           
            if (std == null) return false;
            
            data.DecodeInto(std, referencesKeeper);
            
            return true;
                
        }

        public static T DecodeInto<T>(this string data, out T val, ICfgSerializeNestedReferences referencesKeeper)
            where T : ICfg, new() {
            val = data.DecodeInto<T>(referencesKeeper);
            return val;
        }

        public static T DecodeInto<T>(this string data, ICfgSerializeNestedReferences referencesKeeper) where T : ICfg, new()
        {
            var obj = new T();
            data.DecodeInto(obj, referencesKeeper);
            return obj;
        }
        
        public static T Decode_Reference<T>(this string data, ref T val) where T : Object => data.Decode(ref val, Keeper);

        public static List<T> Decode_References<T>(this string data, out List<T> list) where T: Object => data.Decode_References(out list, Keeper);

        public static bool DecodeInto<T>(this string data, T val, ICfgSerializeNestedReferences referencesKeeper) where T : ICfg
        {
            if (val == null) return false;  

            var prevKeeper = Keeper;
            Keeper = referencesKeeper;

            val.Decode(data); 

            Keeper = prevKeeper;
            return true;

        }

        public static List<T> Decode_List<T>(this string data, out List<T> val, ICfgSerializeNestedReferences referencesKeeper, ref ListMetaData ld) where T : ICfg, new()
        {
            var prevKeeper = Keeper;
            Keeper = referencesKeeper;

            data.Decode_List(out val, ref ld);

            Keeper = prevKeeper;

            return val;
        }

        public static List<T> Decode_List<T>(this string data, out List<T> val, ICfgSerializeNestedReferences referencesKeeper) where T : ICfg, new()
        {
            var prevKeeper = Keeper;
            Keeper = referencesKeeper;

            data.Decode_List(out val);

            Keeper = prevKeeper;

            return val;
        }

        public static T Decode<T>(this string data, ref T val, ICfgSerializeNestedReferences referencesKeeper) where T: Object
        {
           
            if (referencesKeeper == null) return val;
            
            var ind = data.ToInt();
            
            var getting = referencesKeeper.GetReferenced<T>(ind);
            
            if (getting != null)
                val = getting; 
            
            return val;
        }

        public static List<T> Decode_References<T>(this string data, out List<T> list, ICfgSerializeNestedReferences referencesKeeper) where T : Object
        {
            list = new List<T>();

            if (referencesKeeper == null) return list;

            List<int> indexes;
            
            data.Decode_List(out indexes);

            foreach (var i in indexes)
                list.Add(referencesKeeper.GetReferenced<T>(i));
            
            return list;
        }

        public static void TryReplaceAssetByGuid<T>(this string data, ref T val) where T : Object
        {
            var ass = QcUnity.GuidToAsset<T>(data);
            if (ass)
                val = ass;
        }
        #endregion

        public static int ToIntFromTextSafe(this string text, int defaultReturn)
        {
            int res;
            return int.TryParse(text, out res) ? res : defaultReturn;
        }
    }
    
    public class CfgDecoder   {

        public static string ListElementTag => CfgEncoder.ListElementTag;

        public delegate bool DecodeDelegate(string tag, string data);

        private readonly string _data;
        private int _position;
        private bool _expectingGetData;

        public CfgDecoder(string dataStream)
        {
            _data = dataStream ?? "";
            
            _position = 0;
        }

        public void DecodeTagsFor (DecodeDelegate decodeDelegate) {
            foreach (var tag in this)
                decodeDelegate(tag, GetData());
        }
        
        private static readonly List<string> BaseClassChain = new List<string>();
        public void DecodeTagsFor(DecodeDelegate decodeDelegate, IKeepUnrecognizedCfg unrecognizedKeeper, string tag) {

            BaseClassChain.Add(tag);
            try {
                foreach (var t in this) {
                    var data = GetData();

                    if (decodeDelegate(t, data)) continue;
                    
                    BaseClassChain.Add(t);
                    unrecognizedKeeper.UnrecognizedStd.Add(BaseClassChain, data);
                    BaseClassChain.RemoveLast();
                    
                }
            }
            finally {
                BaseClassChain.RemoveLast();
            }
           
        }

        private static bool _ignoreErrors;

        public void DecodeTagsIgnoreErrors<T>(T std) where T : class, ICfg
        {
            _ignoreErrors = true;
            try
            {
                DecodeTagsFor(std);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }

            _ignoreErrors = false;
        }

        public T DecodeTagsFor<T>(T std) where T : class, ICfg
        {

            var unrecognizedKeeper = (std as IKeepUnrecognizedCfg)?.UnrecognizedStd;

            if (_ignoreErrors)
            {
                if (unrecognizedKeeper == null)
                    foreach (var tag in this)
                    {
                        var d = GetData();
                        try
                        {
                            std.Decode(tag, d);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError(_data + Environment.NewLine + ex.ToString());
                        }
                    }
                else foreach (var tag in this)
                {
                    var d = GetData();
                    try
                    {
                        if (!std.Decode(tag, d))
                            unrecognizedKeeper.Add(tag, d);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(_data + Environment.NewLine + ex.ToString());
                    }
                }
            }
            else
            {

                if (unrecognizedKeeper == null)
                    foreach (var tag in this)
                        std.Decode(tag, GetData());
                else
                    foreach (var tag in this)
                    {
                        var d = GetData(); 
                        if (!std.Decode(tag, d))
                            unrecognizedKeeper.Add(tag, d);
                    }
            }

            return std;
        }
        
        public void DecodeTagsFor<T>(ref T std) where T : struct, ICfg
        {
         
            var unrecognizedKeeper = (std as IKeepUnrecognizedCfg)?.UnrecognizedStd;

            if (unrecognizedKeeper == null)
                foreach (var tag in this)
                    std.Decode(tag, GetData());
            else
                foreach (var tag in this) {
                    var d = GetData();
                    if (!std.Decode(tag, d))
                        unrecognizedKeeper.Add(tag, d);
                }

        }

        private string ToNextSplitter()
        {
            var start = _position;
            while (_data[_position] != CfgEncoder.Splitter)
                _position++;
            
            _position++;
            
            return _data.Substring(start, _position - start - 1);
        }

        public bool GotData => _position < _data.Length; 

        public string GetNextTag()
        {

            if (_position >= _data.Length)
                return null;

            if (_expectingGetData)
            {
              
                throw new ArgumentException("Was expecting Get Data");
            }
            
            _expectingGetData = true;

            var tag = ToNextSplitter();

            if (tag.Length == 0)
            {
                Debug.LogError("Tag was empty after [{1}] tag. Position: {2} Length: {3}  {0} {4}".F(Environment.NewLine, currentTag, _position, _data.Length, _data));

                throw new ArgumentException("Tag length was 0");
            }

            currentTag = tag;

            currentTagIndex++;

            return currentTag;
        }

        public string GetData()
        {

            if (!_expectingGetData)
            {
                throw new ArgumentException("Was expecting Get Tag");
            }

            _expectingGetData = false;

            var text = ToNextSplitter();

            try
            {
                int length = int.Parse(text);

                var result = _data.Substring(_position, length);

                _position += length + 1;

                return result;

            }
            catch (Exception ex)
            {
                Debug.LogError("Couldn't get next splitter section [{0}] of {1}".F(currentTag, _data));
                throw ex;
            }
            
        }

        public string currentTag { get; private set; }

        public int currentTagIndex;

        public IEnumerator<string> GetEnumerator()
        {
            currentTagIndex = 0;
            while (GotNextTag())
                yield return currentTag;
        }

        private bool GotNextTag()
        {
            if (_expectingGetData)
            {
                throw new ArgumentException("Was expecting Get Tag");
                //GetData();
            }

            return GetNextTag() != null;
        }
    }

}

#pragma warning restore IDE0034 // Simplify 'default' expression
#pragma warning restore IDE0019 // Use pattern matching
#pragma warning restore IDE0018 // Inline variable declaration