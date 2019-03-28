using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Globalization;

namespace QuizCannersUtilities
{

    public static class EncodeExtensions {

        public static void AppendSplit(this StringBuilder builder, string value) => builder.Append(value).Append(CfgEncoder.Splitter);
        
        public static CfgEncoder Encode (this Transform tf, bool local) {

            var cody = new CfgEncoder();

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

        public static CfgEncoder Encode(this Rect rect, bool local) => new CfgEncoder()
            .Add("pos",rect.position)
            .Add("size",rect.size);
            
        public static CfgEncoder Encode(this RectTransform tf, bool local)
        {
            return new CfgEncoder()
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
        
        public static CfgEncoder Encode(this ICfg cfg, ICfgSerializeNestedReferences keeper) {

            var prevKeeper = CfgEncoder.keeper;
            CfgEncoder.keeper = keeper;

            var ret = cfg.Encode();

            CfgEncoder.keeper = prevKeeper;
            return ret;

        }

        public static CfgEncoder Encode<T>(this T[] arr) where T : ICfg {
            var cody = new CfgEncoder();

            if (arr.IsNullOrEmpty()) return cody; 

            cody.Add("len", arr.Length);

            var types = typeof(T).TryGetDerivedClasses();

            if (types != null && types.Count > 0) {
                foreach (var v in arr)
                    cody.Add(v, types);
            }
            else
                foreach (var v in arr) {
                if (!v.IsNullOrDestroyed_Obj())
                    cody.Add(CfgDecoder.ListElementTag, v.Encode());
                else
                    cody.Add_String(CfgEncoder.NullTag, "");
            }

            return cody;
        }
        
        public static CfgEncoder Encode(this Dictionary<string, string> dic)
        {
            var sub = new CfgEncoder();

            if (dic == null) return sub;
            
            foreach (var e in dic)
                sub.Add_String(e.Key, e.Value);

            return sub;
        }

        public static CfgEncoder Encode(this Dictionary<int, string> dic)
        {
            var sub = new CfgEncoder();

            if (dic == null) return sub; 
            
            foreach (var e in dic)
                sub.Add_String(e.Key.ToString(), e.Value);

            return sub;
        }

        #region ValueTypes
        public static CfgEncoder Encode(this Vector3 v3, int precision) => new CfgEncoder()
            .Add_IfNotEpsilon("x", v3.x.RoundTo(precision))
            .Add_IfNotEpsilon("y", v3.y.RoundTo(precision))
            .Add_IfNotEpsilon("z", v3.z.RoundTo(precision));
            
        public static CfgEncoder Encode(this Vector2 v2, int precision) => new CfgEncoder()
            .Add_IfNotEpsilon("x", v2.x.RoundTo(precision))
            .Add_IfNotEpsilon("y", v2.y.RoundTo(precision));
        
        public static CfgEncoder Encode(this Quaternion q) => new CfgEncoder()
            .Add_IfNotEpsilon("x", q.x.RoundTo6Dec())
            .Add_IfNotEpsilon("y", q.y.RoundTo6Dec())
            .Add_IfNotEpsilon("z", q.z.RoundTo6Dec())
            .Add_IfNotEpsilon("w", q.w.RoundTo6Dec());
            
        public static CfgEncoder Encode(this BoneWeight bw) => new CfgEncoder()
            .Add_IfNotZero("i0", bw.boneIndex0)
            .Add_IfNotEpsilon("w0", bw.weight0)

            .Add_IfNotZero("i1", bw.boneIndex1)
            .Add_IfNotEpsilon("w1", bw.weight1)

            .Add_IfNotZero("i2", bw.boneIndex2)
            .Add_IfNotEpsilon("w2", bw.weight2)

            .Add_IfNotZero("i3", bw.boneIndex3)
            .Add_IfNotEpsilon("w3", bw.weight3);
            
        public static CfgEncoder Encode (this Matrix4x4 m) => new CfgEncoder()

                .Add_IfNotEpsilon("00", m.m00)
                .Add_IfNotEpsilon("01", m.m01)
                .Add_IfNotEpsilon("02", m.m02)
                .Add_IfNotEpsilon("03", m.m03)

                .Add_IfNotEpsilon("10", m.m10)
                .Add_IfNotEpsilon("11", m.m11)
                .Add_IfNotEpsilon("12", m.m12)
                .Add_IfNotEpsilon("13", m.m13)

                .Add_IfNotEpsilon("20", m.m20)
                .Add_IfNotEpsilon("21", m.m21)
                .Add_IfNotEpsilon("22", m.m22)
                .Add_IfNotEpsilon("23", m.m23)

                .Add_IfNotEpsilon("30", m.m30)
                .Add_IfNotEpsilon("31", m.m31)
                .Add_IfNotEpsilon("32", m.m32)
                .Add_IfNotEpsilon("33", m.m33);
        
        public static CfgEncoder Encode(this Vector4 v4) => new CfgEncoder()
            .Add_IfNotEpsilon("x", v4.x.RoundTo6Dec())
            .Add_IfNotEpsilon("y", v4.y.RoundTo6Dec())
            .Add_IfNotEpsilon("z", v4.z.RoundTo6Dec())
            .Add_IfNotEpsilon("w", v4.w.RoundTo6Dec());

        public static CfgEncoder Encode(this Vector3 v3) => new CfgEncoder()
            .Add_IfNotEpsilon("x", v3.x.RoundTo6Dec())
            .Add_IfNotEpsilon("y", v3.y.RoundTo6Dec())
            .Add_IfNotEpsilon("z", v3.z.RoundTo6Dec());

        public static CfgEncoder Encode(this Vector2 v2) => new CfgEncoder()
            .Add_IfNotEpsilon("x", v2.x.RoundTo6Dec())
            .Add_IfNotEpsilon("y", v2.y.RoundTo6Dec());
        
        public static CfgEncoder Encode(this Color col) => new CfgEncoder()
            .Add_IfNotEpsilon("r", col.r.RoundTo(3))
            .Add_IfNotEpsilon("g", col.g.RoundTo(3))
            .Add_IfNotEpsilon("b", col.b.RoundTo(3))
            .Add_IfNotEpsilon("a", col.a.RoundTo(3));
        #endregion
    }

    public class CfgEncoder
    {
        #region Constants
        public const char Splitter = '|';
        public const string NullTag = "null";
        public const string ListElementTag = "e";
        public const string UnrecognizedTag = "_urec";
        public const string ListTag = "_lst";
        public const string ListMetaTag = "_lstMeta";
        public const string IsTrueTag = "y";
        public const string IsFalseTag = "n";
        #endregion

        private readonly StringBuilder _builder = new StringBuilder();

        UnrecognizedTagsList _toUnlock;

        public CfgEncoder Lock(UnrecognizedTagsList tags) {
            _toUnlock = tags;
            tags.locked = true;
            return this;
        }
        
        public override string ToString() {
            if (_toUnlock != null)
                _toUnlock.locked = false;

            return _builder.ToString();
        }

        public delegate CfgEncoder EncodeDelegate();
        public CfgEncoder Add(string tag, EncodeDelegate cody) => cody == null ? this : Add(tag, cody());

        public CfgEncoder Add(string tag, CfgEncoder cody) => cody == null ? this : Add_String(tag, cody.ToString());
       
        public CfgEncoder Add_String(string tag, string data)
        {
            if (data == null)
                data = "";

            _builder.AppendSplit(tag);
            _builder.AppendSplit(data.Length.ToString());
            _builder.AppendSplit(data);
            return this;
        }

        public CfgEncoder Add_Bool(string tag, bool val) => Add_String(tag, val ? IsTrueTag : IsFalseTag);
        
        #region Unity_Objects

        public static ICfgSerializeNestedReferences keeper;

        public CfgEncoder Add_GUID(string tag, UnityEngine.Object obj) => Add_IfNotEmpty(tag, obj.GetGuid());

        public CfgEncoder Add_Reference(string tag, UnityEngine.Object obj) => Add_Reference(tag, obj, keeper);

        public CfgEncoder Add_Reference(string tag, UnityEngine.Object obj, ICfgSerializeNestedReferences referencesKeeper) => (referencesKeeper == null || !obj) ? this : Add_IfNotNegative(tag, referencesKeeper.GetReferenceIndex(obj));
            
        public CfgEncoder Add_References<T>(string tag, List<T> objs) where T : UnityEngine.Object => Add_References(tag, objs,keeper);

        public CfgEncoder Add_References<T>(string tag, List<T> lst, ICfgSerializeNestedReferences referencesKeeper) where T: UnityEngine.Object
        {
            if (referencesKeeper == null || lst == null) return this;
            
            var indxs = new List<int>();

            foreach (var o in lst)
                indxs.Add(referencesKeeper.GetReferenceIndex(o));
          
            return Add(tag, indxs);
            
        }

        public CfgEncoder Add(string tag, ICfg other, ICfgSerializeNestedReferences referencesKeeper)
        {
            var prevKeeper = keeper;
            keeper = referencesKeeper;

            Add(tag, other);   

            keeper = prevKeeper;
            
            return this;
        }

        public CfgEncoder TryAdd<T>(string tag, T obj, ICfgSerializeNestedReferences referencesKeeper)
        {
            var prevKeeper = keeper;
            keeper = referencesKeeper;

            TryAdd(tag, obj);

            keeper = prevKeeper;
            return this;
        }
        
        public CfgEncoder Add<T>(string tag, List<T> other, ICfgSerializeNestedReferences referencesKeeper) where T : ICfg, new()
        {
            var prevKeeper = keeper;
            keeper = referencesKeeper;

            Add(tag, other);

            keeper = prevKeeper;
            return this;
        }
        #endregion

        #region ValueTypes
        public CfgEncoder Add(string tag, float val) =>
        Add_String(tag, val.ToString(CultureInfo.InvariantCulture.NumberFormat));
        public CfgEncoder Add(string tag, float val, int precision) =>
            Add_String(tag, val.RoundTo(precision).ToString(CultureInfo.InvariantCulture.NumberFormat));
        public CfgEncoder Add(string tag, int val) => Add_String(tag, val.ToString());
        public CfgEncoder Add(string tag, uint val) => Add_String(tag, val.ToString());

        public CfgEncoder Add(string tag, Transform tf) => Add(tag, tf.Encode(true));
        public CfgEncoder Add(string tag, Transform tf, bool local) => Add(tag, tf.Encode(local));
        public CfgEncoder Add(string tag, Rect tf) => Add(tag, tf.Encode(true));
        public CfgEncoder Add(string tag, Matrix4x4 m) => Add(tag, m.Encode());
        public CfgEncoder Add(string tag, BoneWeight bw) => Add(tag, bw.Encode());
        public CfgEncoder Add(string tag, Quaternion q) => Add(tag, q.Encode());
        public CfgEncoder Add(string tag, Vector4 v4) => Add(tag, v4.Encode());
        public CfgEncoder Add(string tag, Vector3 v3) => Add(tag, v3.Encode());
        public CfgEncoder Add(string tag, Vector2 v2) => Add(tag, v2.Encode());
        public CfgEncoder Add(string tag, Vector3 v3, int precision) => Add(tag, v3.Encode(precision));
        public CfgEncoder Add(string tag, Vector2 v2, int precision) => Add(tag, v2.Encode(precision));
        public CfgEncoder Add(string tag, Color col) => Add(tag, col.Encode());
        #endregion

        #region Internal Add Unrecognized Data
        private CfgEncoder Add<T>(T val, IList<Type> types, ListMetaData ld, int index) where T : ICfg
        {

            var el = ld.elementDatas.GetIfExists(index);

            if (!val.IsNullOrDestroyed_Obj())
            {
                var typeIndex = types.IndexOf(val.GetType());
                if (typeIndex != -1)
                {
                    el?.SetRecognized();

                    Add(typeIndex.ToString(), val.Encode());
                }
                else
                {
                    el = ld.elementDatas[index];
                    el.unrecognized = true;
                    el.stdDta = val.Encode().ToString();
                    Add_String(UnrecognizedTag, " ");
                }
            }
            else
            {
                if (el != null && el.unrecognized)
                    Add_String(el.unrecognizedUnderTag, el.stdDta);
                else
                    Add_String(NullTag, "");
            }

            return this;
        }

        private CfgEncoder Add_Abstract<T>(T val, ListMetaData ld, int index) where T : IGotClassTag
        {
            var el = ld.elementDatas.GetIfExists(index);

            if (val == null)  return (el != null && el.unrecognized) 
                ? Add_String(el.unrecognizedUnderTag, el.stdDta)
                : Add_String(NullTag, "");
            
            
                el?.SetRecognized();
                return Add(val.ClassTag, val);
 
        }

        public CfgEncoder Add<T>(T v, List<Type> types) where T : ICfg
        {
            if (v.IsNullOrDestroyed_Obj())  return Add_String(NullTag, "");
            
            var typeIndex = types.IndexOf(v.GetType());
            return Add(typeIndex != -1 ? typeIndex.ToString() : UnrecognizedTag, v.Encode());
           
        }
        #endregion

        #region Abstracts

        public CfgEncoder Add<T>(string tag, List<T> val, TaggedTypesCfg tts) where T : IGotClassTag => Add_Abstract(tag, val);

        public CfgEncoder Add_Abstract<T>(string tag, List<T> lst) where T : IGotClassTag {

            if (lst.IsNullOrEmpty()) return this;
            
            var cody = new CfgEncoder();

            foreach (var v in lst)
                if (v!= null)
                    cody.Add(v.ClassTag, v);
                else
                    cody.Add_String(NullTag, "");
            

            return Add(tag, cody);
        }
        
        public CfgEncoder Add<T>(string tag, List<T> val, ListMetaData ld, TaggedTypesCfg tts) where T : IGotClassTag  => Add_Abstract(tag, val, ld);

        public CfgEncoder Add_Abstract<T>(string tag, List<T> val, ListMetaData ld) where T : IGotClassTag {

            var cody = new CfgEncoder();

            if (val != null) {

                if (ld == null)
                    foreach (var v in val)
                        cody.Add(v.ClassTag, v);

                else for (var i = 0; i < val.Count; i++) {
                        var v = val[i];
                        cody.Add_Abstract(v, ld, i);
                }
            }

            Add(tag, new CfgEncoder().Add(ListMetaTag, ld).Add(ListTag, cody));

            return this;
        }
        
        public CfgEncoder Add_Abstract(string tag, IGotClassTag typeTag) =>  typeTag == null ? this :
             Add(tag, new CfgEncoder().Add(typeTag.ClassTag, typeTag.Encode()));
        
        #endregion

        public CfgEncoder Add(string tag, ICfg other)
        {
            if (other.IsNullOrDestroyed_Obj()) return this;
            
            var safe = other as ICfgSafeEncoding;
            
            if (safe == null)  return Add(tag, other.Encode());
            
            var ll = safe.GetLoopLock;

            if (ll.Unlocked)
                using (ll.Lock()) {
                    Add(tag, other.Encode());
                }
            else
                Debug.LogError("Infinite encoding loop detected");
        
            return this;
        }
        
        public CfgEncoder Add(string tag, List<int> val) {

            var cody = new CfgEncoder();
            
            foreach (var i in val)
                cody.Add(CfgDecoder.ListElementTag, i);

            return Add(tag, cody);
        }

        public CfgEncoder Add(string tag, List<string> lst)
        {
            if (lst == null) return this;
            
            var cody = new CfgEncoder();
            
            foreach (var s in lst)
                cody.Add_String(CfgDecoder.ListElementTag, s);

            return Add(tag, cody);
            
        }

        public CfgEncoder Add(string tag, List<uint> val)
        {

            var cody = new CfgEncoder();
            foreach (var i in val)
                cody.Add(CfgDecoder.ListElementTag, i);
            
            return Add(tag, cody);
        }

        public CfgEncoder Add(string tag, List<Color> val)  {

            var cody = new CfgEncoder();
            
            foreach (var i in val)
                cody.Add(CfgDecoder.ListElementTag, i);
            
            return Add(tag, cody);

        }

        public CfgEncoder Add(string tag, List<Matrix4x4> val)  {

            var cody = new CfgEncoder();
            
            foreach (var i in val)
                cody.Add(CfgDecoder.ListElementTag, i);
            
            return Add(tag, cody);

        }

        public CfgEncoder Add(string tag, Matrix4x4[] arr) 
        {
          
            if (arr == null) return this;
            
            var cody = new CfgEncoder()
            .Add("len", arr.Length);

            foreach (var v in arr) 
                cody.Add(CfgDecoder.ListElementTag, v.Encode());
            
            return Add(tag, cody);

        }

        public CfgEncoder Add<T>(string tag, List<T> lst, ListMetaData ld) where T : ICfg, new() {

            var cody = new CfgEncoder();

            if (lst != null) {
                var indTypes = typeof(T).TryGetDerivedClasses();

                if (indTypes != null) {
                    for (var i = 0; i < lst.Count; i++) {
                            var v = lst[i];
                            cody.Add(v, indTypes, ld, i);
                    }
                }
                else  {
                    foreach (var v in lst)
                        if (v!= null)
                            cody.Add(CfgDecoder.ListElementTag, v);
                        else
                            cody.Add_String(NullTag, "");
                }
            }

            return Add(tag, new CfgEncoder().Add(ListMetaTag, ld).Add(ListTag, cody));

        }
        
        public CfgEncoder Add<T>(string tag, List<T> lst) where T : ICfg, new() {

            var cody = new CfgEncoder();

            if (lst == null) return this;
            
            var indTypes = typeof(T).TryGetDerivedClasses();
            
            if (indTypes != null)  {
                    foreach (var v in lst)
                        cody.Add(v, indTypes);
            }
            else  
                foreach (var v in lst)
                    if (v != null)
                        cody.Add(CfgDecoder.ListElementTag, v.Encode());
                    else
                        cody.Add_String(NullTag, "");
            
            
            return Add(tag, cody);
        }

        public CfgEncoder Add(string tag, Dictionary<int, string> dic)
        {
            var sub = new CfgEncoder();

            if (dic == null) return this;
                
            foreach (var e in dic)
                sub.Add_String(e.Key.ToString(), e.Value);

            return Add(tag, sub);

        }

        public CfgEncoder Add(string tag, Dictionary<string, string> dic) => Add(tag, dic.Encode());

        public CfgEncoder Add<T>(string tag, T[] val) where T : ICfg => Add(tag, val.Encode());

        #region NonDefault Encodes

        public CfgEncoder TryAdd<T>(string tag, T obj) {

            var objStd = obj.TryGet_fromObj<ICfg>(); 
            return (objStd != null) ? Add(tag, objStd) : this;
        }

        public CfgEncoder Add_IfNotNegative(string tag, int val) => (val >= 0) ? Add_String(tag, val.ToString()) : this;
        
        public CfgEncoder Add_IfTrue(string tag, bool val) => val ? Add_Bool(tag, true) : this;
  
        public CfgEncoder Add_IfFalse(string tag, bool val) => (!val) ? Add_Bool(tag, false) :  this;
        
        public CfgEncoder Add_IfNotDefault(string tag, ICanBeDefaultCfg cfg) => (!cfg.IsNullOrDestroyed_Obj() && !cfg.IsDefault) ? Add(tag, cfg): this;

        public CfgEncoder Add_IfNotDefault(string tag, ICfg cfg)
        {
            if (cfg.IsNullOrDestroyed_Obj()) return this;
            
            var def = cfg as ICanBeDefaultCfg;

            return (def == null || !def.IsDefault) ? Add(tag, cfg) : this;
        }

        public CfgEncoder Add_IfNotEmpty(string tag, string val) => val.IsNullOrEmpty() ? this : Add_String(tag, val);
            
        public CfgEncoder Add_IfNotEmpty<T>(string tag, List<T> lst) where T : ICfg, new() => lst.IsNullOrEmpty() ? this : Add(tag, lst);

        public CfgEncoder Add_IfNotEmpty(string tag, List<string> val) => val.IsNullOrEmpty() ? this : Add(tag, val);
        
        public CfgEncoder Add_IfNotEmpty(string tag, List<int> val) => val.IsNullOrEmpty() ? this : Add(tag, val);

        public CfgEncoder Add_IfNotEmpty(string tag, List<uint> val) => val.IsNullOrEmpty() ? this : Add(tag, val);

        public CfgEncoder Add_IfNotEmpty<T>(string tag, List<List<T>> lst) where T : ICfg, new()
        {

            if (lst.IsNullOrEmpty()) return this;

            var sub = new CfgEncoder();

            foreach (var l in lst)
                sub.Add_IfNotEmpty(CfgDecoder.ListElementTag, l);

            return Add_String(tag, sub.ToString());
            
        }
        
        public CfgEncoder Add_IfNotEmpty<T>(string tag, List<T> val, TaggedTypesCfg tts) where T : IGotClassTag  => val.IsNullOrEmpty() ? this : Add_Abstract(tag, val);

        public CfgEncoder Add_IfNotEmpty<T>(string tag, List<T> val, TaggedTypesCfg tts, ListMetaData ld) where T : IGotClassTag => val.IsNullOrEmpty() ? this : Add_Abstract(tag, val, ld);
 
        public CfgEncoder Add_IfNotEmpty(string tag, Dictionary<int, string> dic) => dic.IsNullOrEmpty() ? this : Add(tag, dic);
   
        public CfgEncoder Add_IfNotEmpty(string tag, Dictionary<string, string> dic) => dic.IsNullOrEmpty() ? this :  Add(tag, dic);
            
        public CfgEncoder Add_IfNotEpsilon(string tag, float val) => (Mathf.Abs(val) > float.Epsilon * 100) ? Add(tag, val.RoundTo6Dec()) : this;
       
        public CfgEncoder Add_IfNotOne(string tag, Vector4 v4) => v4.Equals(Vector4.one) ? this : Add(tag, v4.Encode());

        public CfgEncoder Add_IfNotOne(string tag, Vector3 v3) => v3.Equals(Vector3.one) ? this : Add(tag, v3.Encode());

        public CfgEncoder Add_IfNotOne(string tag, Vector2 v2) => v2.Equals(Vector2.one) ? this : Add(tag, v2.Encode());
        
        public CfgEncoder Add_IfNotZero(string tag, int val) => val == 0 ? this : Add_String(tag, val.ToString());
            
        public CfgEncoder Add_IfNotZero(string tag, float val, float precision) => Mathf.Abs(val) > precision ?  Add(tag, val): this;
            
        public CfgEncoder Add_IfNotZero(string tag, Vector4 v4)  => v4.magnitude> Mathf.Epsilon ? Add(tag, v4.Encode()) : this;
        
        public CfgEncoder Add_IfNotZero(string tag, Vector3 v3) => v3.magnitude> Mathf.Epsilon ? Add(tag, v3.Encode()) : this;

        public CfgEncoder Add_IfNotZero(string tag, Vector2 v2) => v2.magnitude > Mathf.Epsilon ? Add(tag, v2.Encode()) : this;

        public CfgEncoder Add_IfNotBlack(string tag, Color col) => col == Color.black ? this : Add(tag, col);

        #endregion
    }

}