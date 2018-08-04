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

        public static void AppendSplit(this StringBuilder builder, string value) => builder.Append(value).Append(StdEncoder.splitter);
        
        public static StdEncoder Encode (this Transform tf, bool local) {

            var cody = new StdEncoder();

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

        public static StdEncoder Encode(this Rect rect, bool local) => new StdEncoder()
            .Add("pos",rect.position)
            .Add("size",rect.size);
            
        public static StdEncoder Encode(this RectTransform tf, bool local)
        {
            return new StdEncoder()
            .Add("tfBase", tf.transform.Encode(local))
            .Add("aPos", tf.anchoredPosition)
            .Add("aPos3D", tf.anchoredPosition3D)
            .Add("aMax", tf.anchorMax)
            .Add("aMin", tf.anchorMin)
            .Add("ofMax", tf.offsetMax)
            .Add("ofMin", tf.offsetMin)
            .Add("pvt", tf.pivot)
            .Add("deSize", tf.sizeDelta);
        }
        
        public static StdEncoder Encode<T>(this List<T> val) where T : ISTD
        {
            StdEncoder cody = new StdEncoder();

           // int debugCount = 0;

            if (val != null)
            {

                var types = typeof(T).TryGetDerrivedClasses();

                if (types != null && types.Count > 0)
                {
                    foreach (var v in val)
                    {
                        if (v != null)
                        {
                            int typeIndex = types.IndexOf(v.GetType());
                            if (typeIndex != -1)
                                cody.Add(typeIndex.ToString(), v.Encode());
#if UNITY_EDITOR
                            else
                            {
                                cody.Add("e", v.Encode());
#if PEGI
                                Debug.Log("Type not listed: " + v.GetType() + " in " + typeof(T).ToPEGIstring());
#endif
                            }
#endif
                        }
                        else
                            cody.Add_String(StdEncoder.nullTag, "");
                    }
                }
                else 
                foreach (var v in val) {
                        //debugCount++;
                    if (v != null)
                        cody.Add("e", v.Encode());
                    else
                        cody.Add_String(StdEncoder.nullTag, "");
                }

              //  Debug.Log("Encoded "+ debugCount + " points");

            }
            return cody;
        }

        public static StdEncoder TryEncode<T>(this List<T> val)
        {
            StdEncoder cody = new StdEncoder();
            if (val != null)
            {
                for (int i = 0; i < val.Count; i++)
                {
                    var v = val[i];
                    if (v != null)
                    {
                        var std = v as ISTD;
                        if (std!= null)
                            cody.Add(i.ToString(), std.Encode());
                    }
                   
                }
            }
            return cody;
        }

        public static StdEncoder Encode(this Vector3 v3, int percision) => new StdEncoder()
            .Add_IfNotZero("x", v3.x.RoundTo(percision))
            .Add_IfNotZero("y", v3.y.RoundTo(percision))
            .Add_IfNotZero("z", v3.z.RoundTo(percision));
            
        public static StdEncoder Encode(this Vector2 v2, int percision) => new StdEncoder()
            .Add_IfNotZero("x", v2.x.RoundTo(percision))
            .Add_IfNotZero("y", v2.y.RoundTo(percision));
        
        public static StdEncoder Encode(this Quaternion q) => new StdEncoder()
            .Add_IfNotZero("x", q.x.RoundTo6Dec())
            .Add_IfNotZero("y", q.y.RoundTo6Dec())
            .Add_IfNotZero("z", q.z.RoundTo6Dec())
            .Add_IfNotZero("w", q.w.RoundTo6Dec());
            
        public static StdEncoder Encode(this BoneWeight bw) => new StdEncoder()
            .Add("i0", bw.boneIndex0)
            .Add("w0", bw.weight0)

            .Add("i1", bw.boneIndex1)
            .Add("w1", bw.weight1)

            .Add("i2", bw.boneIndex2)
            .Add("w2", bw.weight2)

            .Add("i3", bw.boneIndex3)
            .Add("w3", bw.weight3);
            
        public static StdEncoder Encode (this Matrix4x4 m)
        {
                StdEncoder sub = new StdEncoder();

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

            return sub;
        }

        public static StdEncoder Encode(this Vector4 v4) => new StdEncoder()
            .Add_IfNotZero("x", v4.x.RoundTo6Dec())
            .Add_IfNotZero("y", v4.y.RoundTo6Dec())
            .Add_IfNotZero("z", v4.z.RoundTo6Dec())
            .Add_IfNotZero("w", v4.w.RoundTo6Dec());

        public static StdEncoder Encode(this Vector3 v3) => new StdEncoder()
            .Add_IfNotZero("x", v3.x.RoundTo6Dec())
            .Add_IfNotZero("y", v3.y.RoundTo6Dec())
            .Add_IfNotZero("z", v3.z.RoundTo6Dec());

        public static StdEncoder Encode(this Vector2 v2) => new StdEncoder()
            .Add_IfNotZero("x", v2.x.RoundTo6Dec())
            .Add_IfNotZero("y", v2.y.RoundTo6Dec());
        
        public static StdEncoder Encode(this Color col) => new StdEncoder()
            .Add_IfNotZero("r", col.r.RoundTo6Dec())
            .Add_IfNotZero("g", col.g.RoundTo6Dec())
            .Add_IfNotZero("b", col.b.RoundTo6Dec())
            .Add_IfNotZero("a", col.a.RoundTo6Dec());
        
    }

    public class StdEncoder
    {
        public const char splitter = '|';
        public const string nullTag = "null";

        StringBuilder builder = new StringBuilder();

        public delegate StdEncoder EncodeDelegate();

        #region Unity_Objects

        static ISTD_SerializeNestedReferences keeper;

        public StdEncoder Add_GUID(string tag, UnityEngine.Object obj)
        {
            var guid = obj.GetGUID();
            if (guid != null)
                Add_String(tag, guid);

            return this;
        }

        public StdEncoder Add_Referance(string tag, UnityEngine.Object obj) => Add(tag, obj, keeper);

        public StdEncoder Add(string tag, UnityEngine.Object obj, ISTD_SerializeNestedReferences referencesKeeper)
        {
            if (referencesKeeper != null && obj)
            {
                int ind = referencesKeeper.GetISTDreferenceIndex(obj);
                if (ind != -1)
                    Add(tag, ind);
            }
            return this;
        }

        public StdEncoder Add_Referances<T>(string tag, List<T> objs) where T : UnityEngine.Object => Add_References<T>(tag, objs,keeper);

        public StdEncoder Add_References<T>(string tag, List<T> objs, ISTD_SerializeNestedReferences referencesKeeper) where T: UnityEngine.Object
        {
            if (referencesKeeper != null && objs!= null)
            {
                var indxs = new List<int>();

                foreach (var o in objs)
                    indxs.Add(referencesKeeper.GetISTDreferenceIndex(o));
              
                Add(tag, indxs);
            }
            return this;
        }

        public StdEncoder Add(string tag, ISTD other, ISTD_SerializeNestedReferences referencesKeeper)
        {
            var prevKeeper = keeper;
            keeper = referencesKeeper;

            Add(tag, other);   

            keeper = prevKeeper;
            return this;
        }

        public StdEncoder Add<T>(string tag, List<T> other, ISTD_SerializeNestedReferences referencesKeeper) where T : ISTD
        {
            var prevKeeper = keeper;
            keeper = referencesKeeper;

            Add(tag, other);

            keeper = prevKeeper;
            return this;
        }
        
        #endregion

        public StdEncoder Add_String(string tag, String data) {

            if (data == null)
                data = "";

            builder.AppendSplit(tag);
            builder.AppendSplit(data.Length.ToString());
            builder.AppendSplit(data);
            return this;
        }

        public StdEncoder Add(string tag, EncodeDelegate cody)
        {
            if (cody != null)
                Add(tag, cody());
            return this;
        }

        public StdEncoder Add(string tag, StdEncoder cody)
        {
            if (cody!= null)
            Add_String(tag, cody.ToString());
            return this;
        }
        
        public StdEncoder Add(string tag, float val) =>
            Add_String(tag, val.ToString(CultureInfo.InvariantCulture.NumberFormat));

        public StdEncoder Add(string tag, float val, int percision) => 
            Add_String(tag, val.RoundTo(percision).ToString(CultureInfo.InvariantCulture.NumberFormat));
        
        public StdEncoder Add_Bool(string tag, bool val) =>
            Add_String(tag, val ? "y" : "n");
        
        public StdEncoder Add_ifTrue(string tag, bool val) {
            if (val)
                Add_Bool(tag, val);
            return this;
        }

        public StdEncoder Add(string tag, ISTD other) {
            if (other != null)
                Add(tag, other.Encode());

            return this;
        }

        public StdEncoder TryAdd<T>(string tag, T obj)
        {
            if (obj != null)
            {
                var objstd = obj as ISTD;
                if (objstd != null)
                    Add(tag, objstd);
            }
            return this;
        }

        public override string ToString() {
            
            return builder.ToString();
        }

        public StdEncoder Add_ifNotNegative(string tag, int val) {
            if (val >= 0)
                Add_String(tag, val.ToString());
            return this;
        }
        
        public StdEncoder Add(string tag, int val) => Add_String(tag, val.ToString());
        
        public StdEncoder Add(string tag, uint val) => Add_String(tag, val.ToString());
        
        public StdEncoder Add(string tag, Transform  tf) => Add(tag, tf.Encode(true));
        
        public StdEncoder Add(string tag, Rect tf) => Add(tag, tf.Encode(true));

        public StdEncoder Add(string tag, List<int> val)
        {

            StdEncoder cody = new StdEncoder();
            foreach (int i in val)
                cody.Add("e", i);

            Add(tag, cody);

            return this;
        }

        public StdEncoder Add(string tag, List<string> lst)
        {
            if (lst != null)
            {
                StdEncoder cody = new StdEncoder();
                foreach (var s in lst)
                    cody.Add_String("e", s);

                Add(tag, cody);
            }

            return this;
        }

        public StdEncoder Add(string tag, List<uint> val)
        {

            StdEncoder cody = new StdEncoder();
            foreach (uint i in val)
                cody.Add("e", i);
            Add(tag, cody);

            return this;
        }

        public StdEncoder Add<T>(string tag, List<T> val) where T : ISTD => Add(tag, val.Encode());
        
        public StdEncoder Add(string tag, Matrix4x4 m) => Add(tag, m.Encode());
        public StdEncoder Add(string tag, BoneWeight bw) => Add(tag, bw.Encode());
        public StdEncoder Add(string tag, Quaternion q) => Add(tag, q.Encode());
        public StdEncoder Add(string tag, Vector4 v4) => Add(tag, v4.Encode());
        public StdEncoder Add(string tag, Vector3 v3) => Add(tag, v3.Encode());
        public StdEncoder Add(string tag, Vector2 v2) => Add(tag, v2.Encode());
        public StdEncoder Add(string tag, Vector3 v3, int percision) => Add(tag, v3.Encode(percision));
        public StdEncoder Add(string tag, Vector2 v2, int percision) => Add(tag, v2.Encode(percision));
        public StdEncoder Add(string tag, Color col) => Add(tag, col.Encode());


        // Optional encoding:

        public StdEncoder Add_IfNotEmpty(string tag, string val) {
            if ((val != null) && (val.Length > 0)) 
                Add_String(tag, val);
            return this;
        }
        
        public StdEncoder Add_ifNotEmpty<T>(string tag, List<T> val) where T : ISTD {

            if (val.Count > 0) 
                Add(tag, val);
            
            return this;
        }

        public StdEncoder Add_IfNotEmpty<T>(string tag, List<List<T>> val) where T : ISTD
        {

            if (val.Count > 0) {

                StdEncoder sub = new StdEncoder();

                foreach (var l in val)
                    sub.Add_ifNotEmpty("e",l);

                Add_String(tag, sub.ToString());
            }
            return this;
        }
        
        public StdEncoder Add_IfNotEmpty(string tag, Dictionary<int, string> dic) {
            if (dic.Count > 0) {

                var sub = new StdEncoder();

                foreach (var e in dic) 
                    sub.Add_String(e.Key.ToString(), e.Value);
                
                Add(tag, sub);
            }
            return this;
        }

        public StdEncoder Add_IfNotZero(string tag, float val) {

            if (Mathf.Abs(val) > float.Epsilon * 100) 
                Add(tag, val.RoundTo6Dec());
            
            return this;
        }

        public StdEncoder Add_ifNotZero(string tag, int val) {

            if (val != 0) 
                Add_String(tag, val.ToString());
            
            return this;
        }

        public StdEncoder Add_IfNotZero(string tag, float val, float percision) {

            if (Mathf.Abs(val) > percision) 
                Add(tag, val);
            

            return this;
        }
        
        public StdEncoder Add_IfNotZero(string tag, Vector3 v3) {

            if ((Math.Abs(v3.x) > Mathf.Epsilon) || (Math.Abs(v3.y) > Mathf.Epsilon) || (Math.Abs(v3.z) > Mathf.Epsilon)) 
                Add(tag, v3.Encode());
            
            return this;
        }

        public StdEncoder Add_IfNotOne(string tag, Vector3 v3) {
            if (!v3.Equals(Vector3.one)) 
                Add(tag, v3.Encode());

            return this;
        }
        
        public StdEncoder Add_IfNotZero(string tag, Vector2 v2) {

            if ((Math.Abs(v2.x) > Mathf.Epsilon) || (Math.Abs(v2.y) > Mathf.Epsilon)) 
                Add(tag, v2.Encode());
            

            return this;
        }

    }

}