using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Globalization;

namespace QuizCannersUtilities
{

    public static class EncodeExtensions {

        public static void AppendSplit(this StringBuilder builder, string value) => builder.Append(value).Append(StdEncoder.Splitter);
        
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

        public static StdEncoder Encode<T>(this T[] arr) where T : ISTD {
            var cody = new StdEncoder();

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
                    cody.Add(StdDecoder.ListElementTag, v.Encode());
                else
                    cody.Add_String(StdEncoder.NullTag, "");
            }

            return cody;
        }
        
        public static StdEncoder Encode(this Dictionary<string, string> dic)
        {
            var sub = new StdEncoder();

            if (dic == null) return sub;
            
            foreach (var e in dic)
                sub.Add_String(e.Key, e.Value);

            return sub;
        }

        public static StdEncoder Encode(this Dictionary<int, string> dic)
        {
            var sub = new StdEncoder();

            if (dic == null) return sub; 
            
            foreach (var e in dic)
                sub.Add_String(e.Key.ToString(), e.Value);

            return sub;
        }

        #region ValueTypes
        public static StdEncoder Encode(this Vector3 v3, int precision) => new StdEncoder()
            .Add_IfNotEpsilon("x", v3.x.RoundTo(precision))
            .Add_IfNotEpsilon("y", v3.y.RoundTo(precision))
            .Add_IfNotEpsilon("z", v3.z.RoundTo(precision));
            
        public static StdEncoder Encode(this Vector2 v2, int precision) => new StdEncoder()
            .Add_IfNotEpsilon("x", v2.x.RoundTo(precision))
            .Add_IfNotEpsilon("y", v2.y.RoundTo(precision));
        
        public static StdEncoder Encode(this Quaternion q) => new StdEncoder()
            .Add_IfNotEpsilon("x", q.x.RoundTo6Dec())
            .Add_IfNotEpsilon("y", q.y.RoundTo6Dec())
            .Add_IfNotEpsilon("z", q.z.RoundTo6Dec())
            .Add_IfNotEpsilon("w", q.w.RoundTo6Dec());
            
        public static StdEncoder Encode(this BoneWeight bw) => new StdEncoder()
            .Add_IfNotZero("i0", bw.boneIndex0)
            .Add_IfNotEpsilon("w0", bw.weight0)

            .Add_IfNotZero("i1", bw.boneIndex1)
            .Add_IfNotEpsilon("w1", bw.weight1)

            .Add_IfNotZero("i2", bw.boneIndex2)
            .Add_IfNotEpsilon("w2", bw.weight2)

            .Add_IfNotZero("i3", bw.boneIndex3)
            .Add_IfNotEpsilon("w3", bw.weight3);
            
        public static StdEncoder Encode (this Matrix4x4 m) => new StdEncoder()

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
        
        public static StdEncoder Encode(this Vector4 v4) => new StdEncoder()
            .Add_IfNotEpsilon("x", v4.x.RoundTo6Dec())
            .Add_IfNotEpsilon("y", v4.y.RoundTo6Dec())
            .Add_IfNotEpsilon("z", v4.z.RoundTo6Dec())
            .Add_IfNotEpsilon("w", v4.w.RoundTo6Dec());

        public static StdEncoder Encode(this Vector3 v3) => new StdEncoder()
            .Add_IfNotEpsilon("x", v3.x.RoundTo6Dec())
            .Add_IfNotEpsilon("y", v3.y.RoundTo6Dec())
            .Add_IfNotEpsilon("z", v3.z.RoundTo6Dec());

        public static StdEncoder Encode(this Vector2 v2) => new StdEncoder()
            .Add_IfNotEpsilon("x", v2.x.RoundTo6Dec())
            .Add_IfNotEpsilon("y", v2.y.RoundTo6Dec());
        
        public static StdEncoder Encode(this Color col) => new StdEncoder()
            .Add_IfNotEpsilon("r", col.r.RoundTo(3))
            .Add_IfNotEpsilon("g", col.g.RoundTo(3))
            .Add_IfNotEpsilon("b", col.b.RoundTo(3))
            .Add_IfNotEpsilon("a", col.a.RoundTo(3));
        #endregion
    }

    public class StdEncoder
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

        UnrecognizedTags_List _toUnlock;

        public StdEncoder Lock(UnrecognizedTags_List tags) {
            _toUnlock = tags;
            tags.locked = true;
            return this;
        }
        
        public override string ToString() {
            if (_toUnlock != null)
                _toUnlock.locked = false;

            return _builder.ToString();
        }

        public delegate StdEncoder EncodeDelegate();
        public StdEncoder Add(string tag, EncodeDelegate cody) => cody == null ? this : Add(tag, cody());

        public StdEncoder Add(string tag, StdEncoder cody) => cody == null ? this : Add_String(tag, cody.ToString());
       
        public StdEncoder Add_String(string tag, string data)
        {
            if (data == null)
                data = "";

            _builder.AppendSplit(tag);
            _builder.AppendSplit(data.Length.ToString());
            _builder.AppendSplit(data);
            return this;
        }

        public StdEncoder Add_Bool(string tag, bool val) => Add_String(tag, val ? IsTrueTag : IsFalseTag);
        
        #region Unity_Objects

        public static ISTD_SerializeNestedReferences keeper;

        public StdEncoder Add_GUID(string tag, UnityEngine.Object obj) => Add_IfNotEmpty(tag, obj.GetGuid());

        public StdEncoder Add_Reference(string tag, UnityEngine.Object obj) => Add_Reference(tag, obj, keeper);

        public StdEncoder Add_Reference(string tag, UnityEngine.Object obj, ISTD_SerializeNestedReferences referencesKeeper) => (referencesKeeper == null || !obj) ? this : Add_IfNotNegative(tag, referencesKeeper.GetReferenceIndex(obj));
            
        public StdEncoder Add_References<T>(string tag, List<T> objs) where T : UnityEngine.Object => Add_References<T>(tag, objs,keeper);

        public StdEncoder Add_References<T>(string tag, List<T> lst, ISTD_SerializeNestedReferences referencesKeeper) where T: UnityEngine.Object
        {
            if (referencesKeeper == null || lst == null) return this;
            
            var indxs = new List<int>();

            foreach (var o in lst)
                indxs.Add(referencesKeeper.GetReferenceIndex(o));
          
            return Add(tag, indxs);
            
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
        
        public StdEncoder Add<T>(string tag, List<T> other, ISTD_SerializeNestedReferences referencesKeeper) where T : ISTD, new()
        {
            var prevKeeper = keeper;
            keeper = referencesKeeper;

            Add(tag, other);

            keeper = prevKeeper;
            return this;
        }
        #endregion

        #region ValueTypes
        public StdEncoder Add(string tag, float val) =>
        Add_String(tag, val.ToString(CultureInfo.InvariantCulture.NumberFormat));
        public StdEncoder Add(string tag, float val, int precision) =>
            Add_String(tag, val.RoundTo(precision).ToString(CultureInfo.InvariantCulture.NumberFormat));
        public StdEncoder Add(string tag, int val) => Add_String(tag, val.ToString());
        public StdEncoder Add(string tag, uint val) => Add_String(tag, val.ToString());

        public StdEncoder Add(string tag, Transform tf) => Add(tag, tf.Encode(true));
        public StdEncoder Add(string tag, Transform tf, bool local) => Add(tag, tf.Encode(local));
        public StdEncoder Add(string tag, Rect tf) => Add(tag, tf.Encode(true));
        public StdEncoder Add(string tag, Matrix4x4 m) => Add(tag, m.Encode());
        public StdEncoder Add(string tag, BoneWeight bw) => Add(tag, bw.Encode());
        public StdEncoder Add(string tag, Quaternion q) => Add(tag, q.Encode());
        public StdEncoder Add(string tag, Vector4 v4) => Add(tag, v4.Encode());
        public StdEncoder Add(string tag, Vector3 v3) => Add(tag, v3.Encode());
        public StdEncoder Add(string tag, Vector2 v2) => Add(tag, v2.Encode());
        public StdEncoder Add(string tag, Vector3 v3, int precision) => Add(tag, v3.Encode(precision));
        public StdEncoder Add(string tag, Vector2 v2, int precision) => Add(tag, v2.Encode(precision));
        public StdEncoder Add(string tag, Color col) => Add(tag, col.Encode());
        #endregion

        #region Internal Add Unrecognized Data
        private StdEncoder Add<T>(T val, IList<Type> types, ListMetaData ld, int index) where T : ISTD
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
                    el.std_dta = val.Encode().ToString();
                    Add_String(UnrecognizedTag, " ");
                }
            }
            else
            {
                if (el != null && el.unrecognized)
                    Add_String(el.unrecognizedUnderTag, el.std_dta);
                else
                    Add_String(NullTag, "");
            }

            return this;
        }

        private StdEncoder Add_Abstract<T>(T val, ListMetaData ld, int index) where T : IGotClassTag
        {
            var el = ld.elementDatas.GetIfExists(index);

            if (val == null)  return (el != null && el.unrecognized) 
                ? Add_String(el.unrecognizedUnderTag, el.std_dta)
                : Add_String(NullTag, "");
            
            
                el?.SetRecognized();
                return Add(val.ClassTag, val);
 
        }

        public StdEncoder Add<T>(T v, List<Type> types) where T : ISTD
        {
            if (v.IsNullOrDestroyed_Obj())  return Add_String(NullTag, "");
            
            var typeIndex = types.IndexOf(v.GetType());
            return Add(typeIndex != -1 ? typeIndex.ToString() : UnrecognizedTag, v.Encode());
           
        }
        #endregion

        #region Abstracts

        public StdEncoder Add<T>(string tag, List<T> val, TaggedTypes_STD tts) where T : IGotClassTag => Add_Abstract(tag, val);

        public StdEncoder Add_Abstract<T>(string tag, List<T> lst) where T : IGotClassTag {

            if (lst.IsNullOrEmpty()) return this;
            
            var cody = new StdEncoder();

            foreach (var v in lst)
                if (v!= null)
                    cody.Add(v.ClassTag, v);
                else
                    cody.Add_String(NullTag, "");
            

            return Add(tag, cody);
        }
        
        public StdEncoder Add<T>(string tag, List<T> val, ListMetaData ld, TaggedTypes_STD tts) where T : IGotClassTag  => Add_Abstract(tag, val, ld);

        public StdEncoder Add_Abstract<T>(string tag, List<T> val, ListMetaData ld) where T : IGotClassTag {

            var cody = new StdEncoder();

            if (val != null) {

                if (ld == null)
                    foreach (var v in val)
                        cody.Add(v.ClassTag, v);

                else for (var i = 0; i < val.Count; i++) {
                        var v = val[i];
                        cody.Add_Abstract(v, ld, i);
                }
            }

            Add(tag, new StdEncoder().Add(ListMetaTag, ld).Add(ListTag, cody));

            return this;
        }
        
        public StdEncoder Add_Abstract(string tag, IGotClassTag typeTag) =>  typeTag == null ? this :
             Add(tag, new StdEncoder().Add(typeTag.ClassTag, typeTag.Encode()));
        
        #endregion

        public StdEncoder Add(string tag, ISTD other)
        {
            if (other.IsNullOrDestroyed_Obj()) return this;
            
            var safe = other as ISTD_SafeEncoding;
            
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
        
        public StdEncoder Add(string tag, List<int> val) {

            var cody = new StdEncoder();
            
            foreach (var i in val)
                cody.Add(StdDecoder.ListElementTag, i);

            return Add(tag, cody);
        }

        public StdEncoder Add(string tag, List<string> lst)
        {
            if (lst == null) return this;
            
            var cody = new StdEncoder();
            
            foreach (var s in lst)
                cody.Add_String(StdDecoder.ListElementTag, s);

            return Add(tag, cody);
            
        }

        public StdEncoder Add(string tag, List<uint> val)
        {

            var cody = new StdEncoder();
            foreach (var i in val)
                cody.Add(StdDecoder.ListElementTag, i);
            
            return Add(tag, cody);
        }

        public StdEncoder Add(string tag, List<Color> val)  {

            var cody = new StdEncoder();
            
            foreach (var i in val)
                cody.Add(StdDecoder.ListElementTag, i);
            
            return Add(tag, cody);

        }

        public StdEncoder Add(string tag, List<Matrix4x4> val)  {

            var cody = new StdEncoder();
            
            foreach (var i in val)
                cody.Add(StdDecoder.ListElementTag, i);
            
            return Add(tag, cody);

        }

        public StdEncoder Add(string tag, Matrix4x4[] arr) 
        {
          
            if (arr == null) return this;
            
            var cody = new StdEncoder()
            .Add("len", arr.Length);

            foreach (var v in arr) 
                cody.Add(StdDecoder.ListElementTag, v.Encode());
            
            return Add(tag, cody);

        }

        public StdEncoder Add<T>(string tag, List<T> lst, ListMetaData ld) where T : ISTD, new() {

            var cody = new StdEncoder();

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
                            cody.Add(StdDecoder.ListElementTag, v);
                        else
                            cody.Add_String(NullTag, "");
                }
            }

            return Add(tag, new StdEncoder().Add(ListMetaTag, ld).Add(ListTag, cody));

        }
        
        public StdEncoder Add<T>(string tag, List<T> lst) where T : ISTD, new() {

            var cody = new StdEncoder();

            if (lst == null) return this;
            
            var indTypes = typeof(T).TryGetDerivedClasses();
            
            if (indTypes != null)  {
                    foreach (var v in lst)
                        cody.Add(v, indTypes);
            }
            else  
                foreach (var v in lst)
                    if (v != null)
                        cody.Add(StdDecoder.ListElementTag, v.Encode());
                    else
                        cody.Add_String(NullTag, "");
            
            
            return Add(tag, cody);
        }

        public StdEncoder Add(string tag, Dictionary<int, string> dic)
        {
            var sub = new StdEncoder();

            if (dic == null) return this;
                
            foreach (var e in dic)
                sub.Add_String(e.Key.ToString(), e.Value);

            return Add(tag, sub);

        }

        public StdEncoder Add(string tag, Dictionary<string, string> dic) => Add(tag, dic.Encode());

        public StdEncoder Add<T>(string tag, T[] val) where T : ISTD => Add(tag, val.Encode());

        #region NonDefault Encodes

        public StdEncoder TryAdd<T>(string tag, T obj) {

            var objstd = obj.TryGet_fromObj<ISTD>(); 
            return (objstd != null) ? Add(tag, objstd) : this;
        }

        public StdEncoder Add_IfNotNegative(string tag, int val) => (val >= 0) ? Add_String(tag, val.ToString()) : this;
        
        public StdEncoder Add_IfTrue(string tag, bool val) => val ? Add_Bool(tag, true) : this;
  
        public StdEncoder Add_IfFalse(string tag, bool val) => (!val) ? Add_Bool(tag, false) :  this;
        
        public StdEncoder Add_IfNotDefault(string tag, ICanBeDefault_STD std) => (!std.IsNullOrDestroyed_Obj() && !std.IsDefault) ? Add(tag, std): this;

        public StdEncoder Add_IfNotDefault(string tag, ISTD std)
        {
            if (std.IsNullOrDestroyed_Obj()) return this;
            
            var def = std as ICanBeDefault_STD;

            return (def == null || !def.IsDefault) ? Add(tag, std) : this;
        }

        public StdEncoder Add_IfNotEmpty(string tag, string val) => val.IsNullOrEmpty() ? this : Add_String(tag, val);
            
        public StdEncoder Add_IfNotEmpty<T>(string tag, List<T> lst) where T : ISTD, new() => lst.IsNullOrEmpty() ? this : Add(tag, lst);

        public StdEncoder Add_IfNotEmpty(string tag, List<string> val) => val.IsNullOrEmpty() ? this : Add(tag, val);
        
        public StdEncoder Add_IfNotEmpty(string tag, List<int> val) => val.IsNullOrEmpty() ? this : Add(tag, val);

        public StdEncoder Add_IfNotEmpty(string tag, List<uint> val) => val.IsNullOrEmpty() ? this : Add(tag, val);

        public StdEncoder Add_IfNotEmpty<T>(string tag, List<List<T>> lst) where T : ISTD, new()
        {

            if (lst.IsNullOrEmpty()) return this;

            var sub = new StdEncoder();

            foreach (var l in lst)
                sub.Add_IfNotEmpty(StdDecoder.ListElementTag, l);

            return Add_String(tag, sub.ToString());
            
        }
        
        public StdEncoder Add_IfNotEmpty<T>(string tag, List<T> val, TaggedTypes_STD tts) where T : IGotClassTag  => val.IsNullOrEmpty() ? this : Add_Abstract(tag, val);

        public StdEncoder Add_IfNotEmpty<T>(string tag, List<T> val, TaggedTypes_STD tts, ListMetaData ld) where T : IGotClassTag => val.IsNullOrEmpty() ? this : Add_Abstract(tag, val, ld);
 
        public StdEncoder Add_IfNotEmpty(string tag, Dictionary<int, string> dic) => dic.IsNullOrEmpty() ? this : Add(tag, dic);
   
        public StdEncoder Add_IfNotEmpty(string tag, Dictionary<string, string> dic) => dic.IsNullOrEmpty() ? this :  Add(tag, dic);
            
        public StdEncoder Add_IfNotEpsilon(string tag, float val) => (Mathf.Abs(val) > float.Epsilon * 100) ? Add(tag, val.RoundTo6Dec()) : this;
       
        public StdEncoder Add_IfNotOne(string tag, Vector4 v4) => v4.Equals(Vector4.one) ? this : Add(tag, v4.Encode());

        public StdEncoder Add_IfNotOne(string tag, Vector3 v3) => v3.Equals(Vector3.one) ? this : Add(tag, v3.Encode());

        public StdEncoder Add_IfNotOne(string tag, Vector2 v2) => v2.Equals(Vector2.one) ? this : Add(tag, v2.Encode());
        
        public StdEncoder Add_IfNotZero(string tag, int val) => val == 0 ? this : Add_String(tag, val.ToString());
            
        public StdEncoder Add_IfNotZero(string tag, float val, float precision) => Mathf.Abs(val) > precision ?  Add(tag, val): this;
            
        public StdEncoder Add_IfNotZero(string tag, Vector4 v4)  => v4.magnitude> Mathf.Epsilon ? Add(tag, v4.Encode()) : this;
        
        public StdEncoder Add_IfNotZero(string tag, Vector3 v3) => v3.magnitude> Mathf.Epsilon ? Add(tag, v3.Encode()) : this;

        public StdEncoder Add_IfNotZero(string tag, Vector2 v2) => v2.magnitude > Mathf.Epsilon ? Add(tag, v2.Encode()) : this;

        public StdEncoder Add_IfNotBlack(string tag, Color col) => col == Color.black ? this : Add(tag, col);

        #endregion
    }

}