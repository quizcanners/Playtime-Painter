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
        
        public static StdEncoder Encode(this ISTD std, ISTD_SerializeNestedReferences keeper) {

            var prevKeeper = StdEncoder.keeper;
            StdEncoder.keeper = keeper;

            var ret = std.Encode();

            StdEncoder.keeper = prevKeeper;
            return ret;

        }

        public static StdEncoder Encode<T>(this T[] val) where T : ISTD {
            StdEncoder cody = new StdEncoder();

            if (val != null)  {

                cody.Add("len", val.Length);

                var types = typeof(T).TryGetDerrivedClasses();

                if (types != null && types.Count > 0) {

                    foreach (var v in val)
                        cody.Add(v, types);
                }
                else
                    foreach (var v in val) {
                    if (v != null)
                        cody.Add(StdDecoder.ListElementTag, v.Encode());
                    else
                        cody.Add_String(StdEncoder.nullTag, "");
                }
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

        public static StdEncoder Encode(this Dictionary<string, string> dic)
        {
            var sub = new StdEncoder();

            if (dic != null)
                foreach (var e in dic)
                    sub.Add_String(e.Key, e.Value);

            return sub;
        }

        public static StdEncoder Encode(this Dictionary<int, string> dic)
        {
            var sub = new StdEncoder();

            if (dic != null)
                foreach (var e in dic)
                    sub.Add_String(e.Key.ToString(), e.Value);

            return sub;
        }

        #region ValueTypes
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
            .Add_ifNotZero("i0", bw.boneIndex0)
            .Add("w0", bw.weight0)

            .Add_ifNotZero("i1", bw.boneIndex1)
            .Add("w1", bw.weight1)

            .Add_ifNotZero("i2", bw.boneIndex2)
            .Add("w2", bw.weight2)

            .Add_ifNotZero("i3", bw.boneIndex3)
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
        #endregion
    }

    public class StdEncoder
    {
        public const char splitter = '|';
        public const string nullTag = "null";
        public const string listElementTag = "e";
        public const string unrecognizedTag = "_urec";

        StringBuilder builder = new StringBuilder();

        public delegate StdEncoder EncodeDelegate();

        #region Unity_Objects

        public static ISTD_SerializeNestedReferences keeper;

        public StdEncoder Add_GUID(string tag, UnityEngine.Object obj)
        {
            var guid = obj.GetGUID();
            if (guid != null)
                Add_String(tag, guid);

            return this;
        }

        public StdEncoder Add_Reference(string tag, UnityEngine.Object obj) => Add(tag, obj, keeper);

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

        public StdEncoder Add_References<T>(string tag, List<T> objs) where T : UnityEngine.Object => Add_References<T>(tag, objs,keeper);

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

        public StdEncoder TryAdd<T>(string tag, T obj, ISTD_SerializeNestedReferences referencesKeeper)
        {
            var prevKeeper = keeper;
            keeper = referencesKeeper;

            TryAdd(tag, obj);

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

        public StdEncoder Add(string tag, ISTD other) {
            if (other != null) {
                var safe = other as ISTD_SafeEncoding;
                if (safe!= null) {
                    var ll = safe.GetLoopLock;

                    if (ll.Unlocked)
                        using (ll.Lock()) {
                            Add(tag, other.Encode());
                        }
                    else
                        Debug.LogError("Infinite encoding loop detected");
                }
                else 
                Add(tag, other.Encode());
            }
            return this;
        }
        
        public StdEncoder TryAdd<T>(string tag, T obj) {
            if (obj != null) {
                var objstd = obj as ISTD;
                if (objstd != null)
                    Add(tag, objstd);
            }
            return this;
        }

        #region Internal Add Unrecognized Data
        
        StdEncoder Add<T>(T val, List<Type> types, List_Data ld, int index) where T : ISTD {

            var el = ld.elementDatas.GetIfExists(index);

            if (val != null) {
                int typeIndex = types.IndexOf(val.GetType());
                if (typeIndex != -1) {
                    if (el != null)
                        el.SetRecognized();

                    Add(typeIndex.ToString(), val.Encode());
                } else {
                    el = ld.elementDatas[index];
                    el.unrecognized = true;
                    el.std_dta = val.Encode().ToString();
                    Add_String(unrecognizedTag, " ");
                }
            }
            else  {
                if (el != null && el.unrecognized)
                    Add_String(el.unrecognizedUnderTag, el.std_dta);
                else
                    Add_String(nullTag, "");
            }

            return this;
        }

        StdEncoder Add_Abstract<T>(T val, List_Data ld, int index) where T : IGotClassTag {
            var el = ld.elementDatas.GetIfExists(index);

            if (val != null) {

                Add(val.ClassTag, val);

                if (el != null)
                   el.SetRecognized();

            } else {
                if (el != null && el.unrecognized)
                    Add_String(el.unrecognizedUnderTag, el.std_dta);
                else
                    Add_String(nullTag, "");
            }

            return this;
        }

        public StdEncoder Add<T>(T v, List<Type> types) where T : ISTD {
            if (v != null) {
                int typeIndex = types.IndexOf(v.GetType());
                if (typeIndex != -1)
                    Add(typeIndex.ToString(), v.Encode());
                else
                    Add(unrecognizedTag, v.Encode());
            }
            else
                Add_String(nullTag, "");

            return this;
        }

        #endregion

        public override string ToString() {
            
            return builder.ToString();
        }

        public StdEncoder Add_IfNotNegative(string tag, int val) {
            if (val >= 0)
                Add_String(tag, val.ToString());
            return this;
        }
        
        public StdEncoder Add(string tag, int val) => Add_String(tag, val.ToString());
        
        public StdEncoder Add(string tag, uint val) => Add_String(tag, val.ToString());
        
        public StdEncoder Add(string tag, Transform tf) => Add(tag, tf.Encode(true));

        public StdEncoder Add(string tag, Transform tf, bool local) => Add(tag, tf.Encode(local));

        public StdEncoder Add(string tag, Rect tf) => Add(tag, tf.Encode(true));

        public StdEncoder Add(string tag, List<int> val)
        {

            StdEncoder cody = new StdEncoder();
            foreach (int i in val)
                cody.Add(StdDecoder.ListElementTag, i);

            Add(tag, cody);

            return this;
        }

        public StdEncoder Add(string tag, List<string> lst)
        {
            if (lst != null) {
                StdEncoder cody = new StdEncoder();
                foreach (var s in lst)
                    cody.Add_String(StdDecoder.ListElementTag, s);

                Add(tag, cody);
            }

            return this;
        }

        public StdEncoder Add(string tag, List<uint> val)
        {

            StdEncoder cody = new StdEncoder();
            foreach (uint i in val)
                cody.Add(StdDecoder.ListElementTag, i);
            Add(tag, cody);

            return this;
        }

        public StdEncoder Add(string tag, List<Color> val)  {

            StdEncoder cody = new StdEncoder();
            foreach (Color i in val)
                cody.Add(StdDecoder.ListElementTag, i);
            Add(tag, cody);

            return this;
        }

        public StdEncoder Add<T>(string tag, List<T> val, List_Data ld = null) where T : ISTD {
                StdEncoder cody = new StdEncoder();

            if (val != null) {
                var indTypes = typeof(T).TryGetDerrivedClasses();
                
                if (indTypes != null)  {

                    if (ld == null)
                    {
                        foreach (var v in val)
                            cody.Add(v, indTypes);
                    }
                    else for (int i = 0; i < val.Count; i++)
                        {
                            var v = val[i];
                            cody.Add(v, indTypes, ld, i);
                        }
                }
                else
                {
                    foreach (var v in val)
                        if (v != null)
                            cody.Add(StdDecoder.ListElementTag, v.Encode());
                        else
                            cody.Add_String(nullTag, "");
                }

            }

            Add(tag, cody);
            
            return this;
        }

        public StdEncoder Add_Abstract<T>(string tag, List<T> val, List_Data ld = null) where T : IGotClassTag {

            StdEncoder cody = new StdEncoder();

            if (val != null)  {

                if (ld == null)
                    foreach (var v in val)
                        cody.Add(v.ClassTag, v);
                
                else for (int i = 0; i < val.Count; i++)  {
                        var v = val[i];
                        cody.Add_Abstract(v, ld, i);
                }
            }

            Add(tag, cody);

            return this;
        }
        
        public StdEncoder Add(string tag, Dictionary<int, string> dic)
        {
            var sub = new StdEncoder();

            if (dic != null)
                foreach (var e in dic)
                    sub.Add_String(e.Key.ToString(), e.Value);

            Add(tag, sub);

            return this;
        }

        public StdEncoder Add(string tag, Dictionary<string, string> dic) {

            Add(tag, dic.Encode());

            return this;
        }
        
        public StdEncoder Add<T>(string tag, T[] val) where T : ISTD => Add(tag, val.Encode());

        #region ValueTypes
        public StdEncoder Add(string tag, Matrix4x4 m) => Add(tag, m.Encode());
        public StdEncoder Add(string tag, BoneWeight bw) => Add(tag, bw.Encode());
        public StdEncoder Add(string tag, Quaternion q) => Add(tag, q.Encode());
        public StdEncoder Add(string tag, Vector4 v4) => Add(tag, v4.Encode());
        public StdEncoder Add(string tag, Vector3 v3) => Add(tag, v3.Encode());
        public StdEncoder Add(string tag, Vector2 v2) => Add(tag, v2.Encode());
        public StdEncoder Add(string tag, Vector3 v3, int percision) => Add(tag, v3.Encode(percision));
        public StdEncoder Add(string tag, Vector2 v2, int percision) => Add(tag, v2.Encode(percision));
        public StdEncoder Add(string tag, Color col) => Add(tag, col.Encode());
        #endregion

        public StdEncoder Add_Abstract(string tag, IGotClassTag typeTag)
        {
            if (typeTag != null) {
                var sub = new StdEncoder().Add(typeTag.ClassTag, typeTag.Encode());
                Add(tag, sub);
            }

            return this;
        }
        
        #region NonDefault Encodes
        public StdEncoder Add_IfTrue(string tag, bool val)
        {
            if (val)
                Add_Bool(tag, val);
            return this;
        }

        public StdEncoder Add_IfNotDefault(string tag, ICanBeDefault_STD std) {
            if (std != null && !std.IsDefault)
                Add(tag, std);
            return this;
        }

        public StdEncoder Add_IfNotEmpty(string tag, string val) {
            if ((val != null) && (val.Length > 0)) 
                Add_String(tag, val);
            return this;
        }
        
        public StdEncoder Add_IfNotEmpty<T>(string tag, List<T> val, List_Data ld = null) where T : ISTD {

            if (val != null && val.Count > 0) 
                Add(tag, val, ld);
            
            return this;
        }

        public StdEncoder Add_IfNotEmpty(string tag, List<string> val) {
            if (val.Count > 0)
                Add(tag, val);
            return this;
        }

        public StdEncoder Add_IfNotEmpty(string tag, List<int> val)
        {
            if (val.Count > 0)
                Add(tag, val);
            return this;
        }

        public StdEncoder Add_IfNotEmpty(string tag, List<uint> val)
        {
            if (val.Count > 0)
                Add(tag, val);
            return this;
        }

        public StdEncoder Add_IfNotEmpty<T>(string tag, List<List<T>> val) where T : ISTD
        {

            if (val.Count > 0) {

                StdEncoder sub = new StdEncoder();

                foreach (var l in val)
                    sub.Add_IfNotEmpty(StdDecoder.ListElementTag, l);

                Add_String(tag, sub.ToString());
            }
            return this;
        }

        public StdEncoder Add_IfNotEmpty(string tag, Dictionary<int, string> dic) {
            if (dic != null && dic.Count > 0)
                Add(tag, dic);
            return this;
        }
        
        public StdEncoder Add_IfNotEmpty(string tag, Dictionary<string, string> dic){
            if (dic!= null && dic.Count > 0) 
                Add(tag, dic);
            
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
        #endregion
    }

}