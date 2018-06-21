using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Text;
using System.Globalization;

namespace SharedTools_Stuff
{

    public static class DecodeExtensions {

        public static void ToAssetByGUID<T>(this string data, ref T val) where T : UnityEngine.Object
        {
            var ass = UnityHelperFunctions.GUIDtoAsset<T>(data);
            if (ass)
                val = ass;
        }

        public static void DecodeInto(this string data, StdDecoder.DecodeDelegate dec) => new StdDecoder(data).DecodeTagsFor(dec);
      
        public static void DecodeInto(this string data, out BoneWeight b) {
            var cody = new StdDecoder(data);
             b = new BoneWeight();

            while (cody.GotData)
                switch (cody.GetTag()) {
                    case "i0": b.boneIndex0 = data.ToInt(); break;
                    case "w0": b.weight0 = data.ToFloat(); break;

                    case "i1": b.boneIndex1 = data.ToInt(); break;
                    case "w1": b.weight1 = data.ToFloat(); break;

                    case "i2": b.boneIndex2 = data.ToInt(); break;
                    case "w2": b.weight2 = data.ToFloat(); break;

                    case "i3": b.boneIndex3 = data.ToInt(); break;
                    case "w3": b.weight3 = data.ToFloat(); break;
                }
          
        }
        
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
                    case "aPos": tf.anchoredPosition = data.ToVector2(); break;
                    case "aPos3D": tf.anchoredPosition3D = data.ToVector3(); break;
                    case "aMax": tf.anchorMax = data.ToVector2(); break;
                    case "aMin": tf.anchorMin = data.ToVector2(); break;
                    case "ofMax": tf.offsetMax = data.ToVector2(); break;
                    case "ofMin": tf.offsetMin = data.ToVector2(); break;
                    case "pvt": tf.pivot = data.ToVector2(); break;
                    case "deSize": tf.sizeDelta = data.ToVector2(); break;
                }
            }
        }

        public static void DecodeInto (this string data, out Matrix4x4 m) {
            var cody = new StdDecoder(data);
             m = new Matrix4x4();

            while (cody.GotData) 
                switch (cody.GetTag()) {

                    case "00": m.m00 = data.ToFloat(); break;
                    case "01": m.m01 = data.ToFloat(); break;
                    case "02": m.m02 = data.ToFloat(); break;
                    case "03": m.m03 = data.ToFloat(); break;

                    case "10": m.m10 = data.ToFloat(); break;
                    case "11": m.m11 = data.ToFloat(); break;
                    case "12": m.m12 = data.ToFloat(); break;
                    case "13": m.m13 = data.ToFloat(); break;

                    case "20": m.m20 = data.ToFloat(); break;
                    case "21": m.m21 = data.ToFloat(); break;
                    case "22": m.m22 = data.ToFloat(); break;
                    case "23": m.m23 = data.ToFloat(); break;

                    case "30": m.m30 = data.ToFloat(); break;
                    case "31": m.m31 = data.ToFloat(); break;
                    case "32": m.m32 = data.ToFloat(); break;
                    case "33": m.m33 = data.ToFloat(); break;

                    default: Debug.Log("Uncnown component: " + cody.GetType()); break;
                }

           // return m;
        }

        public static Quaternion ToQuaternion(this string data)
        {

            StdDecoder cody = new StdDecoder(data);

            Quaternion q = new Quaternion();

            while (cody.GotData)
            {
                switch (cody.GetTag())
                {
                    case "x": q.x = cody.GetData().ToFloat(); break;
                    case "y": q.y = cody.GetData().ToFloat(); break;
                    case "z": q.z = cody.GetData().ToFloat(); break;
                    case "w": q.w = cody.GetData().ToFloat(); break;
                    default: Debug.Log("Uncnown component: " + cody.GetType()); break;
                }
            }
            return q;
        }
        
        public static Vector4 ToVector4(this string data) {

            StdDecoder cody = new StdDecoder(data);

            Vector4 v4 = new Vector4();

            while (cody.GotData) {
                switch (cody.GetTag()) {
                    case "x": v4.x = cody.GetData().ToFloat(); break;
                    case "y": v4.y = cody.GetData().ToFloat(); break;
                    case "z": v4.z = cody.GetData().ToFloat(); break;
                    case "w": v4.w = cody.GetData().ToFloat(); break;
                    default: Debug.Log("Uncnown component: "+cody.GetType()); break;
                }
            }
            return v4;
        }
        
        public static Vector3 ToVector3(this string data) {

            StdDecoder cody = new StdDecoder(data);

            Vector3 v3 = new Vector3();

            while (cody.GotData) {
                switch (cody.GetTag()) {
                    case "x": v3.x = cody.GetData().ToFloat(); break;
                    case "y": v3.y = cody.GetData().ToFloat(); break;
                    case "z": v3.z = cody.GetData().ToFloat(); break;
                }
            }
            return v3;
        }

        public static Vector2 ToVector2(this string data) {

            StdDecoder cody = new StdDecoder(data);

            Vector2 v2 = new Vector3();

            while (cody.GotData) {
                var tag = cody.GetTag();
                var dta = cody.GetData();

                switch (tag) {
                    case "x": v2.x = dta.ToFloat(); break;
                    case "y": v2.y = dta.ToFloat(); break;
                }
            }
            return v2;
        }

        public static Rect ToRect(this string data)
        {
            StdDecoder cody = new StdDecoder(data);

            Rect rect = new Rect();

            while (cody.GotData)
            {
                var tag = cody.GetTag();
                var dta = cody.GetData();
                switch (tag)
                {
                    case "pos": rect.position = dta.ToVector2(); break;
                    case "size": rect.size = dta.ToVector2(); break;
                }
            }
            return rect;
        }


        // Integer
        public static bool ToBool(this string data) {
            return data == "y";
        }

        public static int ToInt(this string data) {
            return Convert.ToInt32(data); //int.Parse(data);
        }

        public static uint ToUInt(this string data)
        {
            return uint.Parse(data);
        }


        // Float
        public static float ToFloat(this string data) {
            return float.Parse(data, CultureInfo.InvariantCulture.NumberFormat);

        }
        

        // List (int)
        public static List<int> DecodeInto (this string data, out List<int> l ) {

            l = new List<int>();

            StdDecoder cody = new StdDecoder(data);

            while (cody.GotData) {
                cody.GetTag();
                l.Add(cody.GetData().ToInt());
            }

            return l;
        }

        public static List<float> DecodeInto(this string data, out List<float> l)
        {

            l = new List<float>();

            StdDecoder cody = new StdDecoder(data);

            while (cody.GotData)
            {
                cody.GetTag();
                l.Add(cody.GetData().ToFloat());
            }

            return l;
        }

        public static List<uint> DecodeInto(this string data, out List<uint> l)
        {

             l = new List<uint>();

            StdDecoder cody = new StdDecoder(data);

            while (cody.GotData)
            {
                cody.GetTag();
                l.Add(cody.GetData().ToUInt());
            }

            return l;
        }


        // STD
        public static T DecodeInto<T>(this string data, T val) where T : iSTD
        {
            if (val != null)
                new StdDecoder(data).DecodeTagsFor(val);
            return val;
        }

        public static T DecodeInto<T>(this string data) where T : iSTD, new() => new StdDecoder(data).DecodeTagsFor(new T());

        public static T TryDecodeInto<T>(this string data, T val)
        {
            if (val != null)
                data.DecodeInto(val as iSTD);
            return val;
        }

        public static T TryDecodeInto<T>(this string data) where T: new() => data.TryDecodeInto(new T());
        
        public static T DecodeInto<T>(this string data, out T val) where T : iSTD, new()
        {
            val = data.DecodeInto<T>();
            return val;
        }

        public static T DecodeInto<T>(this string data, Type childType) where T : iSTD, new()
        {
            T val = (T)Activator.CreateInstance(childType);
            new StdDecoder(data).DecodeTagsFor(val);
            return val;
        }

        public static T TryDecodeInto<T>(this string data, Type childType) where T : new()
        {
            T val = (T)Activator.CreateInstance(childType);
            var std = val as iSTD;
            if (std != null)
                data.DecodeInto(std);

            return val;
        }

        public static T TryDecodeInto<T>(this iSTD ovj, Type childType) //where T : new()
        {
            T val = (T)Activator.CreateInstance(childType);

            if (ovj != null)
            {
                var std = val as iSTD;

                if (std != null)
                    ovj.Encode().ToString().DecodeInto(std);
            }

            return val;
        }

        // ToListOfSTD
        public static void TryDecodeInto<T>(this string data, List<T> val) 
        {
            var cody = new StdDecoder(data);

            while (cody.GotData)  {
                var ind = cody.GetTag().ToIntFromTextSafe(-1);
                cody.GetData().TryDecodeInto(val.TryGet(ind));
            }
        }

        public static List<List<T>> DecodeInto<T>(this string data, out List<List<T>> l) where T : iSTD, new()
        {
            l = new List<List<T>>();

            var cody = new StdDecoder(data);

            while (cody.GotData) {
                cody.GetTag();
                List<T> el;
                cody.GetData().DecodeInto(out el);
                l.Add(el);
            }

            return l;
        }

        public static List<T> DecodeInto<T>(this string data, out List<T> l) where T : iSTD, new() {

            StdDecoder cody = new StdDecoder(data);

             l = new List<T>();

            List<Type> tps = typeof(T).TryGetDerrivedClasses(); 

            while (cody.GotData) {
                var tag = cody.GetTag();
                var dta = cody.GetData();

                var isNull = tag == StdEncoder.nullTag;
                if (isNull)
                    l.Add(default(T));
                else
                {
                    if (tps != null)
                    {
                        var type = tps.TryGet(tag.ToIntFromTextSafe(-1));
                        if (type != null)
                            l.Add(dta.DecodeInto<T>(type));
#if UNITY_EDITOR
                        else
                        {
                            l.Add(dta.DecodeInto<T>());
                            Debug.Log("Couldn't decode class no: " + tag + " for " + typeof(T).ToString());
                        }
#endif

                    }
                    else {
                        T tmp;
                        l.Add(dta.DecodeInto(out tmp));
                    }
                }
            }

            return l;
        }

        public static void DecodeInto(this string data, out Dictionary<int, string> dic) {
            var cody = new StdDecoder(data);

            dic = new Dictionary<int, string>();

            while (cody.GotData)
                dic.Add(cody.GetTag().ToInt(), cody.GetData());
            
        }
        
        public static linearColor ToLinearColor(this string data) {
            linearColor lc = new linearColor();
            lc.Decode(data);
            return lc;
        }

        public static Color ToColor(this string data) {
            var cody = new StdDecoder(data);
            Color c = new Color();
            while (cody.GotData) {
                switch (cody.GetTag()) {
                    case "r": c.r = cody.GetData().ToFloat(); break;
                    case "g": c.g = cody.GetData().ToFloat(); break;
                    case "b": c.b = cody.GetData().ToFloat(); break;
                    case "a": c.a = cody.GetData().ToFloat(); break;
                    default:
                        cody.GetData(); break;
                }
            }

            return c;
        }
        
    }


    public class StdDecoder //: IEnumerable<string>
    {

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

        public T DecodeTagsFor<T>(T storyComponent) where T : iSTD {

            var unrec = storyComponent as iKeepUnrecognizedSTD;

            if (unrec == null)
                foreach (var tag in this)
                    storyComponent.Decode(tag, GetData());
            else
                foreach (var tag in this)
                {
                    var d = GetData();
                    if (!storyComponent.Decode(tag, d))
                        unrec.Unrecognized(tag, d);
                }

            return storyComponent;
        }

        string ToNextSplitter()
        {
            int start = position;
            while (data[position] != StdEncoder.splitter)
                position++;
            position++;
            return data.Substring(start, position - start - 1);
        }

        public bool GotData { get { return position < data.Length; } }

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

        string _currentTag;

        public IEnumerator<string> GetEnumerator()
        {
            while (NextTag())
                yield return _currentTag;
        }

        public bool NextTag()
        {
            if (expectingGetData)
                GetData();
            return GetTag() != null;
        }
    }
}