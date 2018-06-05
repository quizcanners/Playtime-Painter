using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Text;
using System.Globalization;
using PlayerAndEditorGUI;

namespace SharedTools_Stuff
{

    public static class EncodeExtensions {

        public static void AppendSplit(this StringBuilder builder, string value) {
            builder.Append(value);
            builder.Append(stdEncoder.splitter);
        }

        public static stdEncoder Encode (this Transform tf, bool local) {

            var cody = new stdEncoder();

            cody.Add_Bool("loc", local);

            if (local) {
                cody.Add("pos", tf.localPosition);
                cody.Add("size", tf.localScale);
                cody.Add("rot", tf.localRotation);
            } else {
                cody.Add("pos", tf.position);
                cody.Add("size", tf.localScale);
                cody.Add("rot", tf.rotation);
            }

            return cody;
        }

        public static stdEncoder Encode(this Rect rect, bool local)
        {
            var cody = new stdEncoder();

            cody.Add("pos",rect.position);
            cody.Add("size",rect.size);

            return cody;
        }

            public static stdEncoder Encode(this RectTransform tf, bool local)
        {

            var cody = new stdEncoder();

            cody.Add("tfBase", tf.transform.Encode(local));

            cody.Add("aPos", tf.anchoredPosition);
            cody.Add("aPos3D", tf.anchoredPosition3D);
            cody.Add("aMax", tf.anchorMax);
            cody.Add("aMin", tf.anchorMin);
            cody.Add("ofMax", tf.offsetMax);
            cody.Add("ofMin", tf.offsetMin);
            cody.Add("pvt", tf.pivot);
            cody.Add("deSize", tf.sizeDelta);

            return cody;
        }

        public static stdEncoder Encode<T>(this List<T> val) where T : iSTD {

            stdEncoder cody = new stdEncoder();

            if (val != null)
            foreach (var v in val)
                if (v != null)
                cody.Add("e", v.Encode());

            return cody;
        }

        public static stdEncoder TryEncode<T>(this List<T> val)
        {
            stdEncoder cody = new stdEncoder();
            if (val != null)
            {
                for (int i = 0; i < val.Count; i++)
                {
                    var v = val[i];
                    if (v != null)
                    {
                        var std = v as iSTD;
                        if (std!= null)
                            cody.Add(i.ToString(), std.Encode());
                    }
                   
                }
            }
            return cody;
        }

        public static stdEncoder Encode(this Vector3 v3, int percision) {

            stdEncoder cody = new stdEncoder();

            cody.Add_IfNotZero("x", v3.x.RoundTo(percision));
            cody.Add_IfNotZero("y", v3.y.RoundTo(percision));
            cody.Add_IfNotZero("z", v3.z.RoundTo(percision));

            return cody;
        }

        public static stdEncoder Encode(this Vector2 v2, int percision) {

            stdEncoder cody = new stdEncoder();

            cody.Add_IfNotZero("x", v2.x.RoundTo(percision));
            cody.Add_IfNotZero("y", v2.y.RoundTo(percision));

            return cody;
        }

        public static stdEncoder Encode(this Quaternion q)
        {
            stdEncoder cody = new stdEncoder();

            cody.Add_IfNotZero("x", q.x.RoundTo6Dec());
            cody.Add_IfNotZero("y", q.y.RoundTo6Dec());
            cody.Add_IfNotZero("z", q.z.RoundTo6Dec());
            cody.Add_IfNotZero("w", q.w.RoundTo6Dec());

            return cody;
        }

        public static stdEncoder Encode(this Vector4 v4) {

            stdEncoder cody = new stdEncoder();

            cody.Add_IfNotZero("x", v4.x.RoundTo6Dec());
            cody.Add_IfNotZero("y", v4.y.RoundTo6Dec());
            cody.Add_IfNotZero("z", v4.z.RoundTo6Dec());
            cody.Add_IfNotZero("w", v4.w.RoundTo6Dec());

            return cody;
        }

        public static stdEncoder Encode(this Vector3 v3) {

            stdEncoder cody = new stdEncoder();

            cody.Add_IfNotZero("x", v3.x.RoundTo6Dec());
            cody.Add_IfNotZero("y", v3.y.RoundTo6Dec());
            cody.Add_IfNotZero("z", v3.z.RoundTo6Dec());

            return cody;
        }

        public static stdEncoder Encode(this Vector2 v2) {

            stdEncoder cody = new stdEncoder();

            cody.Add_IfNotZero("x", v2.x.RoundTo6Dec());
            cody.Add_IfNotZero("y", v2.y.RoundTo6Dec());

            return cody;
        }

        public static stdEncoder Encode(this Color col) {

            stdEncoder cody = new stdEncoder();

            cody.Add_IfNotZero("r", col.r.RoundTo6Dec());
            cody.Add_IfNotZero("g", col.g.RoundTo6Dec());
            cody.Add_IfNotZero("b", col.b.RoundTo6Dec());
            cody.Add_IfNotZero("a", col.a.RoundTo6Dec());

            return cody;
        }
        
    }
    
    public class stdEncoder {
        //public const char tagtag = '<' ;
        public const char splitter = '|';
        bool dataExpected;
        bool unclosedData;
        //int dataStartedAt;

        StringBuilder builder = new StringBuilder();
        StringBuilder databuilder = new StringBuilder();

        void checkLine() {
            if (unclosedData) {
                string data = databuilder.ToString();
                builder.AppendSplit(data.Length.ToString());
                builder.AppendSplit(data);
                unclosedData = false;
            }

            if (dataExpected) Debug.Log("Data was expected");

        }

        public void AddTag(string tagName) {
            checkLine();
            builder.AppendSplit(tagName);
            dataExpected = true;
        }

        public void AddData(string data) {

            if ((data == null) || (data.Length == 0)) {
                Debug.Log("Got empty data!");
            } else
                databuilder.Append(data);

            unclosedData = true;
            dataExpected = false;
        }

        public void Add_String(string tag, String data) {
            checkLine();

            if (data == null)
                data = "";

            builder.AppendSplit(tag);
            builder.AppendSplit(data.Length.ToString());
            builder.AppendSplit(data);
        }

        public void Add(string tag, stdEncoder cody) {
            if (cody == null) return;

            checkLine();

            builder.AppendSplit(tag);
            string data = cody.ToString();
            builder.AppendSplit(data.Length.ToString());
            builder.AppendSplit(data);
        }

        public void Add(string tag, float val) {
            checkLine();

            builder.AppendSplit(tag);
            string data = val.ToString(CultureInfo.InvariantCulture.NumberFormat);
            builder.AppendSplit(data.Length.ToString());
            builder.AppendSplit(data);
        }

        public void Add(string tag, float val, int percision) {
            checkLine();

            builder.AppendSplit(tag);
            string data = val.RoundTo(percision).ToString(CultureInfo.InvariantCulture.NumberFormat);
            builder.AppendSplit(data.Length.ToString());
            builder.AppendSplit(data);
        }

        public void Add_Bool(string tag, bool val) {
            checkLine();

            builder.AppendSplit(tag);
            string data = val ? "y" : "n";//val.ToString(CultureInfo.InvariantCulture.NumberFormat);
            builder.AppendSplit(data.Length.ToString());
            builder.AppendSplit(data);
        }

        public void Add_ifTrue(string tag, bool val) {
            if (val)
                Add_Bool(tag, val);
        }

        public void Add(string tag, iSTD other) {
            if (other != null) {
                var cody = other.Encode();
                Add(tag, cody);
            }
        }

        public bool Add_ifSTD<T>(string tag, T obj)
        {
            if (obj != null)
            {
                var objstd = obj as iSTD;
                if (objstd != null)
                {
                    Add(tag, objstd);
                    return true;
                }
            }
            return false;
        }

        public override string ToString() {
            checkLine();
            return builder.ToString();
        }

        // Add Data wrappers:

        public void AddData(int data) {
            AddData(data.ToString());
        }

        public void AddData(float data) {
            AddData(data.ToString());
        }

        public void Add_ifNotNegative(string tag, int val) {
            if (val >= 0)
                Add_String(tag, val.ToString());
        }
        
        public void Add(string tag, int val) {
            Add_String(tag, val.ToString());
        }

        public void Add(string tag, uint val) {
            Add_String(tag, val.ToString());
        }
        
        public void Add(string tag, Transform  tf)
        {
            Add(tag, tf.Encode(true));
        }

        public void Add(string tag, Rect tf)
        {
            Add(tag, tf.Encode(true));
        }
        // Optional encoding:

        public bool Add_IfNotEmpty(string tag, string val) {

            if ((val != null) && (val.Length > 0)) {
                Add_String(tag, val);
                return true;
            }

            return false;
        }

        public bool Add_IfNotEmpty(string tag, List<int> val) {

            if (val.Count > 0) {
                
                stdEncoder cody = new stdEncoder();
                foreach (int i in val)
                    cody.Add("e", i);

                Add(tag, cody);
                return true;
            }

            return false;
        }

        public bool Add_IfNotEmpty(string tag, List<uint> val){
            if (val.Count > 0) {
                stdEncoder cody = new stdEncoder();
                foreach (uint i in val)
                    cody.Add("e", i);
                Add(tag, cody);
                return true;
            }
            return false;
        }

        public bool Add_ifNotEmpty<T>(string tag, List<T> val) where T : iSTD {

            if (val.Count > 0) {
                Add(tag, val);
                return true;
            }

            return false;
        }

        public bool Add_IfNotEmpty<T>(string tag, List<List<T>> val) where T : iSTD
        {

            if (val.Count > 0) {

                stdEncoder sub = new stdEncoder();

                foreach (var l in val)
                    sub.Add_ifNotEmpty("e",l);

                Add_String(tag, sub.ToString());
                return true;
            }

            return false;
        }
        
        public bool Add_IfNotEmpty(string tag, Dictionary<int, string> dic) {
            if (dic.Count > 0) {

                var sub = new stdEncoder();

                foreach (var e in dic) 
                    sub.Add_String(e.Key.ToString(), e.Value);
                
                Add(tag, sub);
                return true;
            }
            return false;
        }

        public bool Add_IfNotZero(string tag, float val) {

            if (Mathf.Abs(val) > float.Epsilon * 100) {
                Add(tag, val.RoundTo6Dec());
                return true;
            }

            return false;
        }

        public bool Add_ifNotZero(string tag, int val) {

            if (val != 0) {
                Add_String(tag, val.ToString());
                return true;
            }

            return false;
        }

        public bool Add_IfNotZero(string tag, float val, float percision) {

            if (Mathf.Abs(val) > percision) {
                Add(tag, val);
                return true;
            }

            return false;
        }

        public void Add<T>(string tag, List<T> val) where T : iSTD {
            stdEncoder sub = new stdEncoder();

            foreach (var e in val)
                if (e != null)
                    sub.Add("e", e);

            Add_String(tag, sub.ToString());
        }

        public void Add (string tag, Matrix4x4 m) {

            stdEncoder sub = new stdEncoder();

            sub.Add_IfNotZero("00", m.m00);
            sub.Add_IfNotZero("01", m.m01);
            sub.Add_IfNotZero("02", m.m02);
            sub.Add_IfNotZero("03", m.m03);

            sub.Add_IfNotZero("10", m.m10);
            sub.Add_IfNotZero("11", m.m11);
            sub.Add_IfNotZero("12", m.m12);
            sub.Add_IfNotZero("13", m.m13);

            sub.Add_IfNotZero("20", m.m20);
            sub.Add_IfNotZero("21", m.m21);
            sub.Add_IfNotZero("22", m.m22);
            sub.Add_IfNotZero("23", m.m23);

            sub.Add_IfNotZero("30", m.m30);
            sub.Add_IfNotZero("31", m.m31);
            sub.Add_IfNotZero("32", m.m32);
            sub.Add_IfNotZero("33", m.m33);

            string data = sub.ToString();

            Add_String(tag, data);

        }

        public void Add( string tag, BoneWeight bw) {

            
                var cody = new stdEncoder();

                cody.Add("i0",bw.boneIndex0);
                cody.Add("w0", bw.weight0);

                cody.Add("i1", bw.boneIndex1);
                cody.Add("w1", bw.weight1);

                cody.Add("i2", bw.boneIndex2);
                cody.Add("w2", bw.weight2);

                cody.Add("i3", bw.boneIndex3);
                cody.Add("w3", bw.weight3);

                
        }

        public void Add(string tag, Quaternion q) { Add(tag, q.Encode()); }
        public void Add(string tag, Vector4 v4) { Add(tag, v4.Encode()); }
        public void Add(string tag, Vector3 v3) { Add(tag, v3.Encode()); }
        public void Add(string tag, Vector2 v2) { Add(tag, v2.Encode()); }
        public void Add(string tag, Vector3 v3, int percision) { Add(tag, v3.Encode(percision)); }
        public void Add(string tag, Vector2 v2, int percision) { Add(tag, v2.Encode(percision)); }

        public void Add(string tag, Color col) {
            Add(tag, col.Encode());
        }

        public bool Add_IfNotZero(string tag, Vector3 v3) {

            if ((Math.Abs(v3.x) > Mathf.Epsilon) || (Math.Abs(v3.y) > Mathf.Epsilon) || (Math.Abs(v3.z) > Mathf.Epsilon)) {
                Add(tag, v3.Encode());
                return true;
            }

            return false;
        }

        public bool Add_IfNotOne(string tag, Vector3 v3)
        {

            if (!v3.Equals(Vector3.one)) {
                Add(tag, v3.Encode());
                return true;
            }

            return false;
        }
        
        public bool Add_IfNotZero(string tag, Vector2 v2) {

            if ((Math.Abs(v2.x) > Mathf.Epsilon) || (Math.Abs(v2.y) > Mathf.Epsilon)) {
                Add(tag, v2.Encode());
                return true;
            }

            return false;
        }

    }

}