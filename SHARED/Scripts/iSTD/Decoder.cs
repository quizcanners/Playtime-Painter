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
            
        public static void DecodeInto(this string data, out BoneWeight b) {
            var cody = new stdDecoder(data);
             b = new BoneWeight();

            while (cody.gotData)
                switch (cody.getTag()) {
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

            var cody = new stdDecoder(data);
            bool local = false;

            foreach (var tag in cody)
            {
                var d = cody.getData();
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

            var cody = new stdDecoder(data);

            foreach (var tag in cody)
            {
                var d = cody.getData();
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
            var cody = new stdDecoder(data);
             m = new Matrix4x4();

            while (cody.gotData) 
                switch (cody.getTag()) {

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

            stdDecoder cody = new stdDecoder(data);

            Quaternion q = new Quaternion();

            while (cody.gotData)
            {
                switch (cody.getTag())
                {
                    case "x": q.x = cody.getData().ToFloat(); break;
                    case "y": q.y = cody.getData().ToFloat(); break;
                    case "z": q.z = cody.getData().ToFloat(); break;
                    case "w": q.w = cody.getData().ToFloat(); break;
                    default: Debug.Log("Uncnown component: " + cody.GetType()); break;
                }
            }
            return q;
        }
        
        public static Vector4 ToVector4(this string data) {

            stdDecoder cody = new stdDecoder(data);

            Vector4 v4 = new Vector4();

            while (cody.gotData) {
                switch (cody.getTag()) {
                    case "x": v4.x = cody.getData().ToFloat(); break;
                    case "y": v4.y = cody.getData().ToFloat(); break;
                    case "z": v4.z = cody.getData().ToFloat(); break;
                    case "w": v4.w = cody.getData().ToFloat(); break;
                    default: Debug.Log("Uncnown component: "+cody.GetType()); break;
                }
            }
            return v4;
        }
        
        public static Vector3 ToVector3(this string data) {

            stdDecoder cody = new stdDecoder(data);

            Vector3 v3 = new Vector3();

            while (cody.gotData) {
                switch (cody.getTag()) {
                    case "x": v3.x = cody.getData().ToFloat(); break;
                    case "y": v3.y = cody.getData().ToFloat(); break;
                    case "z": v3.z = cody.getData().ToFloat(); break;
                }
            }
            return v3;
        }

        public static Vector2 ToVector2(this string data) {

            stdDecoder cody = new stdDecoder(data);

            Vector2 v2 = new Vector3();

            while (cody.gotData) {
                var tag = cody.getTag();
                var dta = cody.getData();

                switch (tag) {
                    case "x": v2.x = dta.ToFloat(); break;
                    case "y": v2.y = dta.ToFloat(); break;
                }
            }
            return v2;
        }

        public static Rect ToRect(this string data)
        {
            stdDecoder cody = new stdDecoder(data);

            Rect rect = new Rect();

            while (cody.gotData)
            {
                var tag = cody.getTag();
                var dta = cody.getData();
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

            stdDecoder cody = new stdDecoder(data);

            while (cody.gotData) {
                cody.getTag();
                l.Add(cody.getData().ToInt());
            }

            return l;
        }

        public static List<float> DecodeInto(this string data, out List<float> l)
        {

            l = new List<float>();

            stdDecoder cody = new stdDecoder(data);

            while (cody.gotData)
            {
                cody.getTag();
                l.Add(cody.getData().ToFloat());
            }

            return l;
        }

        public static List<uint> DecodeInto(this string data, out List<uint> l)
        {

             l = new List<uint>();

            stdDecoder cody = new stdDecoder(data);

            while (cody.gotData)
            {
                cody.getTag();
                l.Add(cody.getData().ToUInt());
            }

            return l;
        }


        // STD
        public static T DecodeInto<T>(this string data, T val) where T : iSTD
        {
            if (val != null)
                new stdDecoder(data).DecodeTagsFor(val);
            return val;
        }

        public static T DecodeInto<T>(this string data) where T : iSTD, new() => new stdDecoder(data).DecodeTagsFor(new T());

        public static T TryDecodeInto<T>(this string data, T val)
        {
            if (val != null)
                data.DecodeInto(val as iSTD);

            return val;
        }

        public static T DecodeInto<T>(this string data, out T val) where T : iSTD, new()
        {
            val = data.DecodeInto<T>();
            return val;
        }

        public static T DecodeInto<T>(this string data, Type childType) where T : iSTD, new()
        {
            T val = (T)Activator.CreateInstance(childType);
            new stdDecoder(data).DecodeTagsFor(val);
            return val;
        }
        
        // ToListOfSTD
        public static void TryDecodeInto<T>(this string data, List<T> val) 
        {
            var cody = new stdDecoder(data);

            while (cody.gotData)  {
                var ind = cody.getTag().ToIntFromTextSafe(-1);
                cody.getData().TryDecodeInto(val.TryGet(ind));
            }
        }

        public static List<List<T>> DecodeInto<T>(this string data, out List<List<T>> l) where T : iSTD, new()
        {
            l = new List<List<T>>();

            var cody = new stdDecoder(data);

            while (cody.gotData) {
                cody.getTag();
                List<T> el;
                cody.getData().DecodeInto(out el);
                l.Add(el);
            }

            return l;
        }

        public static List<T> DecodeInto<T>(this string data, out List<T> l) where T : iSTD, new() {

            stdDecoder cody = new stdDecoder(data);

             l = new List<T>();

            List<Type> tps = typeof(T).TryGetDerrivedClasses(); 

            while (cody.gotData) {
                var tag = cody.getTag();
                var dta = cody.getData();

                var isNull = tag == stdEncoder.nullTag;
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
            var cody = new stdDecoder(data);

            dic = new Dictionary<int, string>();

            while (cody.gotData)
                dic.Add(cody.getTag().ToInt(), cody.getData());
            
        }
        
        public static linearColor ToLinearColor(this string data) {
            linearColor lc = new linearColor();
            lc.Decode(data);
            return lc;
        }

        public static Color ToColor(this string data) {
            var cody = new stdDecoder(data);
            Color c = new Color();
            while (cody.gotData) {
                switch (cody.getTag()) {
                    case "r": c.r = cody.getData().ToFloat(); break;
                    case "g": c.g = cody.getData().ToFloat(); break;
                    case "b": c.b = cody.getData().ToFloat(); break;
                    case "a": c.a = cody.getData().ToFloat(); break;
                    default:
                        cody.getData(); break;
                }
            }

            return c;
        }
        
    }


    public class stdDecoder //: IEnumerable<string>
    {

        string data;
        int position;
        bool expectingGetData = false;

        public stdDecoder(string dataStream) {
            data = dataStream;
            if (data == null)
                data = "";
            position = 0;
        }

        public T DecodeTagsFor<T>(T storyComponent) where T : iSTD{

            var unrec = storyComponent as iKeepUnrecognizedSTD;

            if (unrec == null)
                foreach (var tag in this)
                    storyComponent.Decode(tag, getData());
            else
                foreach (var tag in this) {
                    var d = getData();
                    if (!storyComponent.Decode(tag, d))
                        unrec.Unrecognized(tag, d);
                }

            return storyComponent;
        }

        string toNextSplitter() {
            int start = position;
            while (data[position] != stdEncoder.splitter)
                position++;
            position++;
            return data.Substring(start, position - start - 1);
        }

        public bool gotData { get { return position < data.Length; } }

        public string getTag() {

            if (position >= data.Length)
                return null;

            if (expectingGetData) {
                
                string hold = toNextSplitter();
                Debug.Log("Was expecting Get Data for "+hold);
                return hold;
            }
            expectingGetData = true;

            _currentTag = toNextSplitter();

            return _currentTag;
        }

        public string getData() {

            if (!expectingGetData)
                Debug.Log("Was expecting Get Tag");
            expectingGetData = false;
            
            int length = Int32.Parse(toNextSplitter());

            string result = data.Substring(position, length);
            position += length + 1; // skipping tagtag

            return result;
        }

        string _currentTag;
    // public IEnumerable<string> enumerator { get { while (NextTag()) { yield return _currentTag; } } }

    public IEnumerator<string> GetEnumerator() {
            while (NextTag()) 
                yield return _currentTag;
    }

    public bool NextTag() {
            if (expectingGetData)
                getData();
            return getTag() != null;
        }
    }
}