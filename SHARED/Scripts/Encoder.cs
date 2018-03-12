using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Text;
using System.Globalization;

namespace StoryTriggerData {

    public static class EncodeExtensions {

        public static void AppendSplit(this StringBuilder builder, string value) {
            builder.Append(value);
            builder.Append(stdEncoder.splitter);
        }


        public static string Encode(this Vector3 v3, int percision) {

            stdEncoder cody = new stdEncoder();

            cody.AddIfNotZero("x", v3.x.RoundTo(percision));
            cody.AddIfNotZero("y", v3.y.RoundTo(percision));
            cody.AddIfNotZero("z", v3.z.RoundTo(percision));

            return cody.ToString();
        }

        public static string Encode(this Vector2 v2, int percision) {

            stdEncoder cody = new stdEncoder();

            cody.AddIfNotZero("x", v2.x.RoundTo(percision));
            cody.AddIfNotZero("y", v2.y.RoundTo(percision));

            return cody.ToString();
        }

        public static string Encode(this Vector4 v4) {

            stdEncoder cody = new stdEncoder();

            cody.AddIfNotZero("x", v4.x.RoundTo6Dec());
            cody.AddIfNotZero("y", v4.y.RoundTo6Dec());
            cody.AddIfNotZero("z", v4.z.RoundTo6Dec());
            cody.AddIfNotZero("w", v4.w.RoundTo6Dec());

            return cody.ToString();
        }

        public static string Encode(this Vector3 v3) {

            stdEncoder cody = new stdEncoder();

            cody.AddIfNotZero("x", v3.x.RoundTo6Dec());
            cody.AddIfNotZero("y", v3.y.RoundTo6Dec());
            cody.AddIfNotZero("z", v3.z.RoundTo6Dec());

            return cody.ToString();
        }

        public static string Encode(this Vector2 v2) {

            stdEncoder cody = new stdEncoder();

            cody.AddIfNotZero("x", v2.x.RoundTo6Dec());
            cody.AddIfNotZero("y", v2.y.RoundTo6Dec());

            return cody.ToString();
        }

        public static string Encode(this Color col) {

            stdEncoder cody = new stdEncoder();

            cody.AddIfNotZero("r", col.r.RoundTo6Dec());
            cody.AddIfNotZero("g", col.g.RoundTo6Dec());
            cody.AddIfNotZero("b", col.b.RoundTo6Dec());
            cody.AddIfNotZero("a", col.a.RoundTo6Dec());

            return cody.ToString();
        }



    }



    public class stdEncoder {
        //public const char tagtag = '<' ;
        public const char splitter = '|';
        bool dataExpected;
        bool unclosedData;
        int dataStartedAt;

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

        public void AddText(string tag, String data) {
            checkLine();

            builder.AppendSplit(tag);
            builder.AppendSplit(data.Length.ToString());
            builder.AppendSplit(data);
        }

        public void Add(string tag, stdEncoder cody) {
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

        public void Add(string tag, bool val) {
            checkLine();

            builder.AppendSplit(tag);
            string data = val ? "y" : "n";//val.ToString(CultureInfo.InvariantCulture.NumberFormat);
            builder.AppendSplit(data.Length.ToString());
            builder.AppendSplit(data);
        }

        public void AddIfTrue(string tag, bool val) {
            if (val)
                Add(tag, val);
        }

        //CultureInfo.InvariantCulture.NumberFormat



        public void AddIfNotNull(iSTD other) {
            if (other == null)
                return;
            checkLine();
            AddText(other.getDefaultTagName(), other.Encode().ToString());
        }

        public void Add(string tag, iSTD other) {
            checkLine();
            AddText(tag, other.Encode().ToString());
        }

        public void AddIfNotNull(string tag, iSTD other) {
            if (other != null)
                Add(tag, other);
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

        public void Add(string tag, int val) {
            AddText(tag, val.ToString());
        }

        public void Add(string tag, uint val) {
            AddText(tag, val.ToString());
        }

        // Optional encoding:

        public bool AddIfNotEmpty(string tag, string val) {

            if ((val != null) && (val.Length > 0)) {
                AddText(tag, val);
                return true;
            }

            return false;
        }

        public bool AddIfNotEmpty(string tag, List<int> val) {

            if (val.Count > 0) {
                
                stdEncoder cody = new stdEncoder();
                foreach (int i in val)
                    cody.Add("e", i);

                Add(tag, cody);
                return true;
            }

            return false;
        }

        public bool AddIfNotEmpty(string tag, List<uint> val){
            if (val.Count > 0) {
                stdEncoder cody = new stdEncoder();
                foreach (uint i in val)
                    cody.Add("e", i);
                Add(tag, cody);
                return true;
            }
            return false;
        }

        public bool AddIfNotEmpty<T>( List<T> val) where T : iSTD {
            if (val.Count == 0)
                return false;
            return AddIfNotEmpty(val[0].getDefaultTagName(), val);
        }

        public bool AddIfNotEmpty<T>(string tag, List<T> val) where T : iSTD {

            if (val.Count > 0) {
                stdEncoder sub = new stdEncoder();

                for (int i = 0; i < val.Count; i++)// T s in val)
                    if (val[i] != null)
                        sub.Add("e", val[i].Encode());

                AddText(tag, sub.ToString());
                return true;
            }

            return false;
        }

        public bool AddIfNotEmpty<T>(string tag, List<List<T>> val) where T : iSTD
        {

            if (val.Count > 0) {

                stdEncoder sub = new stdEncoder();

                foreach (var l in val)
                    sub.AddIfNotEmpty("e",l);

                AddText(tag, sub.ToString());
                return true;
            }

            return false;
        }


        public bool AddIfNotEmpty(string tag, Dictionary<int, string> dic) {
            if (dic.Count > 0) {

                var sub = new stdEncoder();

                foreach (var e in dic) 
                    sub.AddText(e.Key.ToString(), e.Value);
                
                Add(tag, sub);
                return true;
            }
            return false;
        }

        public bool AddIfNotZero(string tag, float val) {

            if (Mathf.Abs(val) > float.Epsilon * 100) {
                Add(tag, val.RoundTo6Dec());
                return true;
            }

            return false;
        }

        public bool AddIfNotZero(string tag, int val) {

            if (val != 0) {
                AddText(tag, val.ToString());
                return true;
            }

            return false;
        }

        public bool AddIfNotZero(string tag, float val, float percision) {

            if (Mathf.Abs(val) > percision) {
                Add(tag, val);
                return true;
            }

            return false;
        }


        public void Add (string tag, Matrix4x4 m) {

            stdEncoder sub = new stdEncoder();

            sub.AddIfNotZero("00", m.m00);
            sub.AddIfNotZero("01", m.m01);
            sub.AddIfNotZero("02", m.m02);
            sub.AddIfNotZero("03", m.m03);

            sub.AddIfNotZero("10", m.m10);
            sub.AddIfNotZero("11", m.m11);
            sub.AddIfNotZero("12", m.m12);
            sub.AddIfNotZero("13", m.m13);

            sub.AddIfNotZero("20", m.m20);
            sub.AddIfNotZero("21", m.m21);
            sub.AddIfNotZero("22", m.m22);
            sub.AddIfNotZero("23", m.m23);

            sub.AddIfNotZero("30", m.m30);
            sub.AddIfNotZero("31", m.m31);
            sub.AddIfNotZero("32", m.m32);
            sub.AddIfNotZero("33", m.m33);

            string data = sub.ToString();

            AddText(tag, data);

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


        public void Add(string tag, Vector4 v4) { AddText(tag, v4.Encode()); }
        public void Add(string tag, Vector3 v3) { AddText(tag, v3.Encode()); }
        public void Add(string tag, Vector2 v2) { AddText(tag, v2.Encode()); }
        public void Add(string tag, Vector3 v3, int percision) { AddText(tag, v3.Encode(percision)); }
        public void Add(string tag, Vector2 v2, int percision) { AddText(tag, v2.Encode(percision)); }

        public void Add(string tag, Color col) {
            AddText(tag, col.Encode());
        }

        public bool AddIfNotZero(string tag, Vector3 v3) {

            if ((Math.Abs(v3.x) > Mathf.Epsilon) || (Math.Abs(v3.y) > Mathf.Epsilon) || (Math.Abs(v3.z) > Mathf.Epsilon)) {
                AddText(tag, v3.Encode());
                return true;
            }

            return false;
        }

        public bool AddIfNotOne(string tag, Vector3 v3)
        {

            if (!v3.Equals(Vector3.one)) {
                AddText(tag, v3.Encode());
                return true;
            }

            return false;
        }
        
        public bool AddIfNotZero(string tag, Vector2 v2) {

            if ((Math.Abs(v2.x) > Mathf.Epsilon) || (Math.Abs(v2.y) > Mathf.Epsilon)) {
                AddText(tag, v2.Encode());
                return true;
            }

            return false;
        }

    }

}