using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace SharedTools_Stuff
{

    public static class DecodeExtensions {

        #region Non-Instancible

        public static void DecodeInto(this string data, StdDecoder.DecodeDelegate dec, IKeepUnrecognizedSTD unrec, string tag = "b") 
            => new StdDecoder(data).DecodeTagsFor(dec, unrec, tag);
        
        public static void DecodeInto(this string data, StdDecoder.DecodeDelegate dec) => new StdDecoder(data).DecodeTagsFor(dec);

        public static void DecodeInto(this string data, Transform tf)
        {

            var cody = new StdDecoder(data);
            bool local = false;

            foreach (var tag in cody)
            {
                var d = cody.GetData();
                switch (tag)
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

            var cody = new StdDecoder(data);

            foreach (var tag in cody)
            {
                var d = cody.GetData();
                switch (tag)
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
            var cody = new StdDecoder(data);
            var b = new BoneWeight();

            foreach (var tag in cody)
            {
                var d = cody.GetData();
                switch (tag)
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

        public static Matrix4x4 ToMatrix4x4(this string data)
        {
            var cody = new StdDecoder(data);
            var m = new Matrix4x4();

            foreach (var tag in cody)
            {
                var d = cody.GetData();
                switch (tag)
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

                    default: Debug.Log("Uncnown component: " + tag); break;
                }
            }
            return m;
        }

        public static Quaternion ToQuaternion(this string data)
        {

            StdDecoder cody = new StdDecoder(data);

            Quaternion q = new Quaternion();

            foreach (var tag in cody)
            {
                var d = cody.GetData();
                switch (tag)
                {
                    case "x": q.x = d.ToFloat(); break;
                    case "y": q.y = d.ToFloat(); break;
                    case "z": q.z = d.ToFloat(); break;
                    case "w": q.w = d.ToFloat(); break;
                    default: Debug.Log("Uncnown component: " + cody.GetType()); break;
                }
            }
            return q;
        }

        public static Vector4 ToVector4(this string data) {

            StdDecoder cody = new StdDecoder(data);

            Vector4 v4 = new Vector4();

            foreach (var tag in cody)
            {
                var d = cody.GetData();
                switch (tag) {
                    case "x": v4.x = d.ToFloat(); break;
                    case "y": v4.y = d.ToFloat(); break;
                    case "z": v4.z = d.ToFloat(); break;
                    case "w": v4.w = d.ToFloat(); break;
                    default: Debug.Log("Uncnown component: " + tag); break;
                }
            }
            return v4;
        }

        public static Vector3 ToVector3(this string data) {

            StdDecoder cody = new StdDecoder(data);

            Vector3 v3 = new Vector3();

            foreach (var tag in cody)
            {
                var d = cody.GetData();
                switch (tag)
                {
                    case "x": v3.x = d.ToFloat(); break;
                    case "y": v3.y = d.ToFloat(); break;
                    case "z": v3.z = d.ToFloat(); break;
                }
            }
            return v3;
        }

        public static Vector2 ToVector2(this string data) {

            StdDecoder cody = new StdDecoder(data);

            Vector2 v2 = new Vector3();

            foreach (var tag in cody)
            {
                var d = cody.GetData();
                switch (tag)
                {
                    case "x": v2.x = d.ToFloat(); break;
                    case "y": v2.y = d.ToFloat(); break;
                }
            }
            return v2;
        }

        public static Rect ToRect(this string data)
        {
            StdDecoder cody = new StdDecoder(data);

            Rect rect = new Rect();

            foreach (var tag in cody)
            {
                var d = cody.GetData();
                switch (tag)
                {
                    case "pos": rect.position = d.ToVector2(); break;
                    case "size": rect.size = d.ToVector2(); break;
                }
            }
            return rect;
        }

        public static bool ToBool(this string data) {
            return data == "y";
        }

        public static int ToInt(this string data) {

            int variable = 0;
            int.TryParse(data, out variable);

            return variable;
            //return Convert.ToInt32(data); //int.Parse(data);

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

        public static LinearColor ToLinearColor(this string data)
        {
            LinearColor lc = new LinearColor();
            lc.Decode(data);
            return lc;
        }

        public static Color ToColor(this string data)
        {
            var cody = new StdDecoder(data);
            Color c = new Color();
            foreach (var tag in cody)
            {
                var d = cody.GetData();
                switch (tag)
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

            StdDecoder cody = new StdDecoder(data);

            foreach (var tag in cody)
                l.Add(cody.GetData());

            return l;
        }

        public static List<int> Decode_List(this string data, out List<int> l) {

            l = new List<int>();

            StdDecoder cody = new StdDecoder(data);

            foreach (var tag in cody)
                l.Add(cody.GetData().ToInt());

            return l;
        }

        public static List<float> Decode_List(this string data, out List<float> l)
        {

            l = new List<float>();

            StdDecoder cody = new StdDecoder(data);

            foreach (var tag in cody)
                l.Add(cody.GetData().ToFloat());


            return l;
        }

        public static List<uint> Decode_List(this string data, out List<uint> l)
        {

            l = new List<uint>();

            StdDecoder cody = new StdDecoder(data);

            foreach (var tag in cody)
                l.Add(cody.GetData().ToUInt());


            return l;
        }

        public static List<Color> Decode_List(this string data, out List<Color> l)
        {

            l = new List<Color>();

            StdDecoder cody = new StdDecoder(data);

            foreach (var tag in cody)
                l.Add(cody.GetData().ToColor());


            return l;
        }
        #endregion

        #region InternalDecode

        static T DecodeData<T>(this StdDecoder cody, TaggedTypes_STD tps, List_Data ld) where T : IGotClassTag
            => Decode<T>(cody._currentTag, cody.GetData(), tps, ld, cody.currentTagIndex);

        static T DecodeData<T>(this StdDecoder cody, TaggedTypes_STD tps) where T : IGotClassTag
             => Decode<T>(cody._currentTag, cody.GetData(), tps);
        
        static T DecodeData<T>(this StdDecoder cody, List<Type> tps, List_Data ld) where T : ISTD, new()
            => Decode<T>(cody._currentTag, cody.GetData(), tps, ld, cody.currentTagIndex);


        static T DecodeData<T>(this StdDecoder cody, List<Type> tps) where T : ISTD, new()
            => Decode<T>(cody._currentTag, cody.GetData(), tps);

        static T Decode<T>(string tag, string data, TaggedTypes_STD tps, List_Data ld, int index) where T : IGotClassTag
        {

            T ret = default(T);

            if (tag != StdEncoder.nullTag)
            {
                var type = tps.TaggedTypes.TryGet(tag);
                if (type != null)
                    ret = data.DecodeInto_Type<T>(type);
                else ld.elementDatas[index].Unrecognized(tag, data);

            }

            return ret;
        }

        static T Decode<T>(string tag, string data, TaggedTypes_STD tps) where T : IGotClassTag
        {

            T ret = default(T);

            if (tag != StdEncoder.nullTag)
            {
                var type = tps.TaggedTypes.TryGet(tag);
                if (type != null)
                    ret = data.DecodeInto_Type<T>(type);
            }

            return ret;
        }

        static T Decode<T>(string tag, string data, List<Type> tps, List_Data ld, int index) where T : ISTD, new()
        {

            T ret = default(T);

            if (tag != StdEncoder.nullTag)
            {
                var type = tps.TryGet(tag.ToIntFromTextSafe(-1));
                if (type != null)
                    ret = data.DecodeInto_Type<T>(type);
                else
                    ld.elementDatas[index].Unrecognized(tag, data);
            }

            return ret;
        }

        static T Decode<T>(string tag, string data, List<Type> tps) where T : ISTD, new()
        {

            T ret = default(T);

            if (tag != StdEncoder.nullTag)
            {
                var type = tps.TryGet(tag.ToIntFromTextSafe(-1));
                if (type != null)
                    ret = data.DecodeInto_Type<T>(type);
                else if (tag == StdDecoder.ListElementTag)
                    ret = data.DecodeInto_Type<T>(tps[0]);
            }

            return ret;
        }
        #endregion

        #region STD List
        public static bool TryDecode_List<T>(this string data, List<T> val)
        {
            if (val != null) {
                var cody = new StdDecoder(data);

                while (cody.GotData) {
                    var ind = cody.GetTag().ToIntFromTextSafe(-1);
                    cody.GetData().TryDecodeInto(val.TryGet(ind));
                }
                return true;
            }
            return false;
        }

        public static List<List<T>> Decode_ListOfList<T>(this string data, out List<List<T>> l) where T : ISTD, new()
        {
            l = new List<List<T>>();

            var cody = new StdDecoder(data);

            while (cody.GotData) {
                cody.GetTag();
                List<T> el;
                cody.GetData().Decode_List(out el);
                l.Add(el);
            }

            return l;
        }
        
        public static List<T> Decode_List<T>(this string data, out List<T> l, ref List_Data ld) where T : ISTD, new() {

            if (ld == null)
                ld = new List_Data();
            l = new List<T>();

            List<Type> tps = typeof(T).TryGetDerrivedClasses();
       
            var overCody = new StdDecoder(data);
            foreach (var tag in overCody) {

                switch (tag) {

                    case StdEncoder.listMetaTag: ld.Decode(overCody.GetData()); break;
                        
                    case StdEncoder.listTag:
                        StdDecoder cody = new StdDecoder(overCody.GetData());
                        if (tps != null)
                            foreach (var t in cody)
                                l.Add(cody.DecodeData<T>(tps, ld)); //Decode<T>(t, cody.GetData(), tps, ld, cody.currentTagIndex));
                        else foreach (var t in cody)
                                l.Add(cody.GetData().DecodeInto<T>());
                        break;

                    default:
                        if (tps != null)
                            l.Add(overCody.DecodeData<T>(tps,ld)); //Decode<T>(tag, overCody.GetData(), tps));
                        else
                            l.Add(overCody.GetData().DecodeInto<T>());
                        break;
                }
            }

            return l;
        }
        
        public static List<T> Decode_List<T>(this string data, out List<T> l) where T : ISTD, new() {

            StdDecoder cody = new StdDecoder(data);

            l = new List<T>();

            List<Type> tps = typeof(T).TryGetDerrivedClasses();

            if (tps != null) 
                foreach (var tag in cody)
                    l.Add(cody.DecodeData<T>(tps)); 
            else foreach (var tag in cody)
                    l.Add(cody.GetData().DecodeInto<T>());

            return l;
        }
        #endregion

        #region Abstract 

        public static List<T> Decode_List<T>(this string data, out List<T> l, TaggedTypes_STD tps) where T : IGotClassTag {
            StdDecoder cody = new StdDecoder(data);

            l = new List<T>();

            foreach (var tag in cody)
                l.Add(cody.DecodeData<T>(tps));

            return l;
        }

        public static List<T> Decode_List<T>(this string data, out List<T> l, ref List_Data ld, TaggedTypes_STD tps) where T : IGotClassTag
        {
            l = new List<T>();
            if (ld == null)
                ld = new List_Data();

            var overCody = new StdDecoder(data);
            foreach (var tag in overCody) {
                switch (tag) {
                    case StdEncoder.listMetaTag: ld.Decode(overCody.GetData()); break;
                    case StdEncoder.listTag:
                        StdDecoder cody = new StdDecoder(overCody.GetData());
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
            var cody = new StdDecoder(data);

            dic = new Dictionary<int, string>();

            while (cody.GotData)
                dic.Add(cody.GetTag().ToInt(), cody.GetData());

        }

        public static void Decode_Dictionary(this string data, out Dictionary<string, string> dic)
        {
            var cody = new StdDecoder(data);

            dic = new Dictionary<string, string>();

            while (cody.GotData)
                dic.Add(cody.GetTag(), cody.GetData());

        }
        #endregion

        #region STD class
        public static ISTD DecodeTagsFor<T>(this string data, T val) where T : ISTD
        {
            if (val == null)
                return val;

            new StdDecoder(data).DecodeTagsFor(val);
            return val;
        }

        public static T DecodeInto<T>(this string data, out T val) where T : ISTD, new()
        {
            val = data.DecodeInto<T>();
            return val;
        }

        public static T DecodeInto<T>(this string data) where T : ISTD, new()
        {
            var obj = new T();
            obj.Decode(data);
            return obj;
        }

        public static bool TryDecodeInto<T>(this string data, T val) => (val as ISTD).Decode_ifNotNull(data);

        public static bool Decode_ifNotNull(this ISTD istd, string data)
        {
            if (istd != null)
            {
                istd.Decode(data);
                return true;
            }

            return false;
        }

        public static T TryDecodeInto<T>(this string data) where T : new()
        {
            var obj = new T();
            data.TryDecodeInto(obj);
            return obj;
        }

        public static T DecodeInto_Type<T>(this string data, Type childType) where T : ISTD
        {
            T val = (T)Activator.CreateInstance(childType);
            val.Decode(data);
            return val;
        }

        public static T TryDecodeInto_Type<T>(this string data, Type childType)
        {
            T val = (T)Activator.CreateInstance(childType);
            var std = val as ISTD;
            if (std != null)
                std.Decode(data); //.DecodeTagsFor(std);

            return val;
        }

        public static T TryDecodeInto<T>(this ISTD ovj, Type childType)
        {
            T val = (T)Activator.CreateInstance(childType);

            if (ovj != null)
            {
                var std = val as ISTD;

                if (std != null)
                    std.Decode(ovj.Encode().ToString()); //.DecodeTagsFor(std);
            }

            return val;
        }
        #endregion

        public static void DecodeInto<T>(this string data, out T val, TaggedTypes_STD typeList) where T : IGotClassTag {

            val = default(T);

            var cody = new StdDecoder(data);

            var type = typeList.TaggedTypes.TryGet(cody.GetTag());

            if (type != null)
                val = cody.GetData().DecodeInto_Type<T>(type);
        }

        public static T[] Decode_Array<T>(this string data, out T[] l) where T : ISTD, new() {

            StdDecoder cody = new StdDecoder(data);

            l = null;

            List<T> tmpList = new List<T>();

            int ind = 0;

            foreach (var tag in cody) {
                var d = cody.GetData();

                if (tag == "len")
                    l = new T[d.ToInt()];
                else {
                    bool isNull = tag == StdEncoder.nullTag;

                    var obj = isNull ? default(T) : d.DecodeInto<T>();

                    if (l != null) 
                         l[ind] = obj;
                     else 
                         tmpList.Add(obj);
                       
                    ind++;
                }
            }

            if (l == null)
                l = tmpList.ToArray();

            return l;
        }
        
        #region Into Unity Objects
        static ISTD_SerializeNestedReferences keeper;

        public static bool TryDecodeInto<T>(this string data, T val, ISTD_SerializeNestedReferences referencesKeeper) {
            var std = val as ISTD;
            if (std != null) {
                data.DecodeInto(std, referencesKeeper);
                return true;
            }
            return false;
        }

        public static T DecodeInto<T>(this string data, out T val, ISTD_SerializeNestedReferences referencesKeeper)
            where T : ISTD, new() {
            val = data.DecodeInto<T>(referencesKeeper);
            return val;
        }

        public static T DecodeInto<T>(this string data, ISTD_SerializeNestedReferences referencesKeeper) where T : ISTD, new()
        {
            var obj = new T();
            data.DecodeInto(obj, referencesKeeper);
            return obj;
        }
        
        public static T Decode_Reference<T>(this string data, ref T val) where T : UnityEngine.Object => data.Decode<T>(ref val, keeper);

        public static List<T> Decode_References<T>(this string data, out List<T> list) where T: UnityEngine.Object => data.Decode_References(out list, keeper);

        public static bool DecodeInto<T>(this string data, T val, ISTD_SerializeNestedReferences referencesKeeper) where T : ISTD
        {
            if (val != null)  {

                var prevKeeper = keeper;
                keeper = referencesKeeper;

                val.Decode(data); 

                keeper = prevKeeper;
                return true;
            }

            return false;
        }

        public static List<T> Decode_List<T>(this string data, out List<T> val, ISTD_SerializeNestedReferences referencesKeeper, ref List_Data ld) where T : ISTD, new()
        {
            var prevKeeper = keeper;
            keeper = referencesKeeper;

            data.Decode_List(out val, ref ld);

            keeper = prevKeeper;

            return val;
        }

        public static List<T> Decode_List<T>(this string data, out List<T> val, ISTD_SerializeNestedReferences referencesKeeper) where T : ISTD, new()
        {
            var prevKeeper = keeper;
            keeper = referencesKeeper;

            data.Decode_List(out val);

            keeper = prevKeeper;

            return val;
        }

        public static T Decode<T>(this string data, ref T val, ISTD_SerializeNestedReferences referencesKeeper) where T: UnityEngine.Object
        {
            var ind = data.ToInt();
            if (referencesKeeper != null) {
                T getting = referencesKeeper.GetISTDreferenced<T>(ind);
                if (getting != null)
                    val = getting; 
            }

            return val;
        }

        public static List<T> Decode_References<T>(this string data, out List<T> list, ISTD_SerializeNestedReferences referencesKeeper) where T : UnityEngine.Object
        {
            list = new List<T>();

            if (referencesKeeper != null) {

                List<int> indxs;
                data.Decode_List(out indxs);

                foreach (var i in indxs)
                    list.Add(referencesKeeper.GetISTDreferenced<T>(i));
            }
            return list;
        }

        public static void ToAssetByGUID<T>(this string data, ref T val) where T : UnityEngine.Object
        {
            var ass = UnityHelperFunctions.GUIDtoAsset<T>(data);
            if (ass)
                val = ass;
        }
        #endregion

    }
    

    public class StdDecoder   {

        public static char Splitter => StdEncoder.splitter;
        public static string NullTag => StdEncoder.nullTag;
        public static string UnrecognizedTag => StdEncoder.unrecognizedTag;
        public static string ListElementTag => StdEncoder.listElementTag;
        public static string ListTag => StdEncoder.listTag;
        public static string ListMetaTag => StdEncoder.listMetaTag;

        public delegate bool DecodeDelegate(string tag, string data);

        string data;
        int position;
        bool expectingGetData = false;

        public StdDecoder(string dataStream)
        {
            data = dataStream;
            if (data == null)
                data = "";
            position = 0;
        }

        public void DecodeTagsFor (DecodeDelegate dec) {
            foreach (var tag in this)
                dec(tag, GetData());
        }


        public static List<string> baseClassChain = new List<string>();
        public void DecodeTagsFor(DecodeDelegate dec, IKeepUnrecognizedSTD unrec, string tag) {

            baseClassChain.Add(tag);
            try {
                foreach (var t in this) {
                    var data = GetData();

                    if (!dec(t, data)) {
                        baseClassChain.Add(t);
                        unrec.UnrecognizedSTD.Add(baseClassChain, data);
                        baseClassChain.RemoveLast();
                    }
                }
            }
            finally {
                baseClassChain.RemoveLast();
            }
           
        }

        public T DecodeTagsFor<T>(T std) where T : ISTD {

            var unrec = (std as IKeepUnrecognizedSTD)?.UnrecognizedSTD;

            try {
                if (unrec == null)
                    foreach (var tag in this)
                        std.Decode(tag, GetData());
                else
                    foreach (var tag in this)
                    {
                        var d = GetData();
                        if (!std.Decode(tag, d))
                            unrec.Add(tag, d);
                    }
            } catch (Exception ex) {
                Debug.Log("Couldn't decode {0} for {1} : {2}".F(data, std, ex));
            }
            
            return std;
        }

        string ToNextSplitter()
        {
            int start = position;
            while (data[position] != StdEncoder.splitter)
                position++;
            position++;
            return data.Substring(start, position - start - 1);
        }

        public bool GotData => position < data.Length; 

        public string GetTag()
        {

            if (position >= data.Length)
                return null;

            if (expectingGetData)
            {

                string hold = ToNextSplitter();
                Debug.Log("Was expecting Get Data for " + hold);
                return hold;
            }
            expectingGetData = true;

            _currentTag = ToNextSplitter();

            currentTagIndex++;

            return _currentTag;
        }

        public string GetData()
        {

            if (!expectingGetData)
                Debug.Log("Was expecting Get Tag");
            expectingGetData = false;

            int length = Int32.Parse(ToNextSplitter());

            string result = data.Substring(position, length);
            position += length + 1; // skipping tagtag

            return result;
        }

        public string _currentTag;

        public int currentTagIndex = 0;

        public IEnumerator<string> GetEnumerator()
        {
            currentTagIndex = 0;
            while (NextTag())
                yield return _currentTag;
        }

        bool NextTag()
        {
            if (expectingGetData)
                GetData();
            return GetTag() != null;
        }

    }
}