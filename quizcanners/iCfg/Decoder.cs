using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuizCannersUtilities {


#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration

    public static class Decoder {

        #region Non-Instancible

        //public static void Decode_Base(this string data, CfgDecoder.DecodeDelegate dec)//, IKeepUnrecognizedCfg unrecognizedKeeper
                                                                                       //  , string tag = "b") 
          //  => new CfgDecoder(data).DecodeTagsFor(dec); //unrecognizedKeeper,
             //   tag);
        
             //public static void DecodeInto(this string data, CfgDecoder.DecodeDelegate dec) => new CfgDecoder(data).DecodeTagsFor(dec);
        
        public static void DecodeInto(this string data, RectTransform tf)
        {

            var cody = new CfgDecoder(data);

            foreach (var t in cody)
            {
                var d = cody.GetData();
                switch (t)
                {
                    case "tfBase": d.Decode(tf.transform); break;
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

        #region To List Of Value Type
        public static List<string> Decode_List(string data, out List<string> l)
        {

            l = new List<string>();

            var cody = new CfgDecoder(data);

            foreach (var tag in cody)
                l.Add(cody.GetData().ToString());

            return l;
        }

        public static List<int> Decode_List(this string data, out List<int> l) {

            l = new List<int>();

            var cody = new CfgDecoder(data);

            foreach (var tag in cody)
                l.Add(cody.GetData().ToInt(0));

            return l;
        }

        public static List<float> Decode_List(this string data, out List<float> l)
        {

            l = new List<float>();

            var cody = new CfgDecoder(data);

            foreach (var tag in cody)
                l.Add(cody.GetData().ToFloat());


            return l;
        }

        public static List<uint> Decode_List(this string data, out List<uint> l)
        {

            l = new List<uint>();

            var cody = new CfgDecoder(data);

            foreach (var tag in cody)
                l.Add(cody.GetData().ToUInt());


            return l;
        }

        public static List<Color> Decode_List(this string data, out List<Color> l)
        {

            l = new List<Color>();

            var cody = new CfgDecoder(data);

            foreach (var tag in cody)
                l.Add(cody.GetData().ToColor());


            return l;
        }
        #endregion
        
        #region Cfg List
        /*
        public static bool TryDecode_IntoList_Elements<T>(this string data, List<T> val)
        {

            if (val == null) return false;
            
            var cody = new CfgDecoder(data);

            var index = 0;

            foreach (var t in cody) {

                if (index >= val.Count)
                    return true;
  
                cody.GetData().TryDecodeInto(val[index]);
                index++;
            }
            
            return true;
            
        }

        public static List<T> TryDecode_IntoList_Elements<T>(this string data, List<T> l, ref ListMetaData ld) where T : ICfg, new() {

            if (ld == null)
                ld = new ListMetaData();

            var overCody = new CfgDecoder(data);
            var index = 0;

            foreach (var tag in overCody)  {

                switch (tag) {

                    case CfgEncoder.ListMetaTag: ld.Decode(overCody.GetData()); break;

                    case CfgEncoder.ListTag:
                        var cody = new CfgDecoder(overCody.GetData());

                        foreach (var t in cody) {

                            var d = cody.GetData();

                            if (index >= l.Count || !d.TryDecodeInto(l[index]))
                                ld.elementDatas[index].Unrecognized(tag, d);

                            index++;
                        }
                        break;

                    default:
                        var d1 = overCody.GetData();

                        if (index >= l.Count || !d1.TryDecodeInto(l[index]))
                            ld.elementDatas[index].Unrecognized(tag, d1);

                        index++;
                        break;
                }
            }

            return l;
        }
        */
        public static List<List<T>> Decode_ListOfList<T>(this CfgData data, out List<List<T>> l) where T : ICfg, new()
        {
            l = new List<List<T>>();

            var cody = new CfgDecoder(data);

            while (cody.GotData) {
                cody.GetNextTag();
                List<T> el;
                cody.GetData().ToList(out el);
                l.Add(el);
            }

            return l;
        }



      /*  public static List<T> Decode_List<T>(this string data, out List<T> l) where T : ICfg, new() {

            var cody = new CfgDecoder(data);

            l = new List<T>();

            var tps = typeof(T).TryGetDerivedClasses();

            if (tps != null) 
                foreach (var tag in cody)
                    l.Add(DecodeData<T>(cody, (TaggedTypesCfg) tps)); 
            else foreach (var tag in cody)
                    l.Add(cody.GetData().DecodeInto<T>());

            return l;
        }*/
        #endregion

        #region CFG class

        public static ICfg DecodeTagsFrom<T>(this T obj, CfgData data) where T : class, ICfg
            => (QcUnity.IsNullOrDestroyed_Obj(obj)) ? obj : new CfgDecoder(data).DecodeTagsFor(obj);
        
        public static T TryDecodeInto<T>(this ICfg ovj, Type childType)
        {
            var val = (T)Activator.CreateInstance(childType);

            if (ovj == null) return val;
            
            var std = val as ICfg;

            if (std == null) return val;
            
            std.Decode(new CfgData(ovj.Encode().ToString())); 
            
            return val;
        }
    
        #endregion
        
        public static int ToIntFromTextSafe(this string text, int defaultReturn)
        {
            int res;
            return int.TryParse(text, out res) ? res : defaultReturn;
        }
    }
    
    public class CfgDecoder   {

        public static string ListElementTag => CfgEncoder.ListElementTag;

        public delegate void DecodeDelegate(string tag, CfgData data);
        
        private readonly string _data;
        private int _position;
        private bool _expectingGetData;

        public CfgDecoder(string dataStream)
        {
            _data = dataStream ?? "";
            _position = 0;
        }

        public CfgDecoder(CfgData data)
        {
            _data = data.ToString() ?? "";
            _position = 0;
        }

        public void DecodeTagsFor(DecodeDelegate decodeDelegate)
        {
            foreach (var tag in this)
                decodeDelegate(tag, GetData());
        }

        private static bool _ignoreErrors;

        public void DecodeTagsIgnoreErrors<T>(T std) where T : class, ICfg
        {
            _ignoreErrors = true;
            try
            {
                DecodeTagsFor(std);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }

            _ignoreErrors = false;
        }

        public T DecodeTagsFor<T>(T std) where T : class, ICfg
        {

           // var unrecognizedKeeper = (std as IKeepUnrecognizedCfg)?.UnrecognizedStd;

            if (_ignoreErrors)
            {
               // if (unrecognizedKeeper == null)
                    foreach (var tag in this)
                    {
                        var d = GetData();
                        try
                        {
                            std.Decode(tag, d);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError(_data + Environment.NewLine + ex.ToString());
                        }
                    }
             /*   else foreach (var tag in this)
                {
                    var d = GetData();
                    try
                    {
                        if (!std.Decode(tag, d))
                            unrecognizedKeeper.Add(tag, d);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(_data + Environment.NewLine + ex.ToString());
                    }
                }*/
            }
            else
            {

               // if (unrecognizedKeeper == null)
                    foreach (var tag in this)
                        std.Decode(tag, GetData());
              /*  else
                    foreach (var tag in this)
                    {
                        var d = GetData(); 
                        if (!std.Decode(tag, d))
                            unrecognizedKeeper.Add(tag, d);
                    }*/
            }

            return std;
        }
        
        public void DecodeTagsFor<T>(ref T std) where T : struct, ICfg
        {
            foreach (var tag in this)
                std.Decode(tag, GetData());
        }

        private string ToNextSplitter()
        {
            var start = _position;
            while (_data[_position] != CfgEncoder.Splitter)
                _position++;
            
            _position++;
            
            return _data.Substring(start, _position - start - 1);
        }

        public bool GotData => _position < _data.Length; 

        public string GetNextTag()
        {
            if (_position >= _data.Length)
                return null;

            if (_expectingGetData)
            {
                throw new ArgumentException("Was expecting Get Data");
            }
            
            _expectingGetData = true;

            var tag = ToNextSplitter();

            if (tag.Length == 0)
            {
                Debug.LogError("Tag was empty after [{1}] tag. Position: {2} Length: {3}  {0} {4}".F(Environment.NewLine, CurrentTag, _position, _data.Length, _data));

                throw new ArgumentException("Tag length was 0");
            }

            CurrentTag = tag;

            currentTagIndex++;

            return CurrentTag;
        }
        /*
        public string GetData()
        {

            if (!_expectingGetData)
            {
                throw new ArgumentException("Was expecting Get Tag");
            }

            _expectingGetData = false;

            var text = ToNextSplitter();

            try
            {
                int length = int.Parse(text);

                var result = _data.Substring(_position, length);

                _position += length + 1;

                return result;

            }
            catch (Exception ex)
            {
                Debug.LogError("Couldn't get next splitter section [{0}] of {1}".F(CurrentTag, _data));
                throw ex;
            }
            
        }
        */
        public CfgData GetData()
        {

            if (!_expectingGetData)
            {
                throw new ArgumentException("Was expecting Get Tag");
            }

            _expectingGetData = false;

            var text = ToNextSplitter();

            try
            {
                int length = int.Parse(text);

                var result = _data.Substring(_position, length);

                _position += length + 1;

                return  new CfgData(result);

            }
            catch (Exception ex)
            {
                Debug.LogError("Couldn't get next splitter section [{0}] of {1}".F(CurrentTag, _data));
                throw ex;
            }

        }


        public string CurrentTag { get; private set; }

        public int currentTagIndex;

        public IEnumerator<string> GetEnumerator()
        {
            currentTagIndex = 0;
            while (GotNextTag())
                yield return CurrentTag;
        }

        private bool GotNextTag()
        {
            if (_expectingGetData)
            {
                throw new ArgumentException("Was expecting Get Tag");
                //GetData();
            }

            return GetNextTag() != null;
        }
    }

}

#pragma warning restore IDE0034 // Simplify 'default' expression
#pragma warning restore IDE0019 // Use pattern matching
#pragma warning restore IDE0018 // Inline variable declaration