using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Text;
//using StoryLink;
using System.Globalization;

namespace StoryTriggerData {

    public static class DecodeExtensions {

        public static BoneWeight ToBoneWeight(this string data) {
            var cody = new stdDecoder(data);
            var b = new BoneWeight();

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
            return b;
        }

        public static Matrix4x4 ToMatrix4x4 (this string data) {
            var cody = new stdDecoder(data);
            var m = new Matrix4x4();

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

            return m;
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
                switch (cody.getTag()) {
                    case "x": v2.x = cody.getData().ToFloat(); break;
                    case "y": v2.y = cody.getData().ToFloat(); break;
                }
            }
            return v2;
        }


        // Integer



        public static bool ToBool(this string data) {
            return data == "y";
        }

        public static int ToInt(this string data) {
            return int.Parse(data);
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

        public static List<int> ToListOfInt(this string data) {



            List<int> l = new List<int>();

            stdDecoder cody = new stdDecoder(data);

            while (cody.gotData) {
                cody.getTag();
                l.Add(cody.getData().ToInt());
            }

            return l;
        }

        public static List<uint> ToListOfUInt(this string data)
        {



            List<uint> l = new List<uint>();

            stdDecoder cody = new stdDecoder(data);

            while (cody.gotData)
            {
                cody.getTag();
                l.Add(cody.getData().ToUInt());
            }

            return l;
        }
        // ToSlistOfStorySaveable

        public static List<List<T>> ToListOfList_STD<T>(this string data) where T : iSTD, new()
        {
            List<List<T>> l = new List<List<T>>();

            var cody = new stdDecoder(data);

            while (cody.gotData) {
                cody.getTag();
                l.Add(cody.getData().ToListOf_STD<T>());
            }

            return l;
        }

        public static List<T> ToListOf_STD<T>(this string data) where T : iSTD, new() {

            stdDecoder cody = new stdDecoder(data);

            List<T> l = new List<T>();

            while (cody.gotData) {
                cody.getTag();
                T tmp = new T();
                tmp.Reboot(cody.getData());
                l.Add(tmp);
            }

            return l;
        }

        public static Dictionary<int, string> ToDictionaryIntString_STD(this string data) {
            var cody = new stdDecoder(data);

            Dictionary<int, string> dic = new Dictionary<int, string>();

            while (cody.gotData)
                dic.Add(cody.getTag().ToInt(), cody.getData());

            return dic;
        }


        public static linearColor ToLinearColor(this string data) {
            linearColor lc = new linearColor();
            lc.Reboot(data);
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


    public class stdDecoder {

        string data;
        int position;
        bool expectingGetData = false;

        public stdDecoder(string dataStream) {
            data = dataStream;
            if (data == null)
                data = "";
            position = 0;
        }

        public void DecodeTagsFor(iSTD storyComponent) {
            while (gotData) {

                string tag = getTag();
                //Debug.Log("tag: " + tag);


                if (tag == null)
                    return;

                storyComponent.Decode(tag, getData());

            }
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

            return toNextSplitter();

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

    }
}