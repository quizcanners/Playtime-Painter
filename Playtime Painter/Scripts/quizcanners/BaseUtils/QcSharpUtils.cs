using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using PlayerAndEditorGUI;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace QuizCannersUtilities {

    #pragma warning disable IDE0034 // Simplify 'default' expression
    #pragma warning disable IDE0019 // Use pattern matching
    #pragma warning disable IDE0018 // Inline variable declaration
    
    public static class QcSharp {

        public static string ThisMethodName() => ThisMethodName(1);

        public static string ThisMethodName(int up) => (new StackFrame(up)).GetMethod()?.Name;

        #region Timer

        public static string SecondsToReadableString(int seconds) => TicksToReadableString(seconds * TimeSpan.TicksPerSecond);

        public static string SecondsToReadableString(double seconds) => TicksToReadableString(seconds * TimeSpan.TicksPerSecond);
        
        public static string SecondsToReadableString(float seconds) => TicksToReadableString((long)seconds * TimeSpan.TicksPerSecond);

        public static string SecondsToReadableString(long seconds) => TicksToReadableString(seconds * TimeSpan.TicksPerSecond);

        public static string TicksToReadableString(double elapsed)
        {

            double absElapsed = Math.Abs(elapsed);

            if (absElapsed < TimeSpan.TicksPerMillisecond)
                return "{0} ticks  ({1} ms)".F(elapsed.ToString("0.00"), (elapsed / TimeSpan.TicksPerMillisecond).ToString("0.00"));
            if (absElapsed < TimeSpan.TicksPerSecond)
                return "{0} miliseconds  ({1} s)".F((elapsed / TimeSpan.TicksPerMillisecond).ToString("0.00"), (elapsed / TimeSpan.TicksPerSecond).ToString("0.00"));
            if (absElapsed < TimeSpan.TicksPerMinute)
                return "{0} seconds  ({1} min)".F((elapsed / TimeSpan.TicksPerSecond).ToString("0.00"), (elapsed / TimeSpan.TicksPerMinute).ToString("0.00"));
            if (absElapsed < TimeSpan.TicksPerHour)
                return "{0} minutes  ({1} hours)".F((elapsed / TimeSpan.TicksPerMinute).ToString("0.00"), (elapsed / TimeSpan.TicksPerHour).ToString("0.00"));
            return "{0} hours  ({1} days)".F((elapsed / TimeSpan.TicksPerHour).ToString("0.00"), (elapsed / TimeSpan.TicksPerDay).ToString("0.00"));

        }

        public static Timer timer = new Timer();

        public class Timer : IDisposable {

            private readonly Stopwatch StopWatch = new Stopwatch();

            private string _timerStartLabel;

            public Timer Start()
            {
                _timerStartLabel = null;

                StopWatch.Restart();

                return this;
            }

            public Timer Start(string label)
            {
                _timerStartLabel = label;
                StopWatch.Restart();


                return this;
            }

            public float GetMiliseconds() => StopWatch.ElapsedMilliseconds;

            public float GetSeconds() => StopWatch.ElapsedMilliseconds / 1000f;

            public override string ToString()
            {
                var text = (_timerStartLabel != null) ? (_timerStartLabel + "->") : "";

                text += TicksToReadableString(StopWatch.ElapsedTicks);

                _timerStartLabel = null;

                return text;
            }


            public string End() => End(null, false);

            public string End(string label, bool logIt = true) => End(label, logIt, false);

            public string End(string label, float threshold) => End(label, true, false, threshold);

            public string End(string label, bool logInEditor, bool logInPlayer) =>
                End(label, logInEditor, logInPlayer, 0);

            public string End(string label, bool logInEditor, bool logInPlayer, float logThreshold)
            {
                StopWatch.Stop();

                var text = label + (label.IsNullOrEmpty() ? "" : ": ") + ToString();
                
                if ((Math.Abs(logThreshold) < float.Epsilon || ((StopWatch.ElapsedTicks / TimeSpan.TicksPerSecond) > logThreshold)) &&
                    ((Application.isEditor && logInEditor) || (!Application.isEditor && logInPlayer)))
                    Debug.Log(text);

                StopWatch.Reset();

                return text;
            }

            public string End_Restart(string labelForEndedSection = null) =>
                End_Restart(labelForEndedSection, true);

            public string End_Restart(string labelForEndedSection, bool logIt) =>
                End_Restart(labelForEndedSection, logIt, logIt, 0);

            public string End_Restart(string labelForEndedSection, bool logIt, int logThreshold) =>
                End_Restart(labelForEndedSection, logIt, logIt, logThreshold);

            public string End_Restart(string labelForEndedSection, bool logInEditor, bool logInPlayer) =>
                End_Restart(labelForEndedSection, logInEditor, logInPlayer, 0);

            public string End_Restart(string labelForEndedSection, bool logInEditor, bool logInPlayer,
                int logThreshold) {
                StopWatch.Stop();
                var txt = End(labelForEndedSection, logInEditor, logInPlayer, logThreshold);
                StopWatch.Start();
                return txt;
            }
            
            private string _timingKey = "?";
            private Dictionary<string, string> _timingLogDictionary;

            private Dictionary<string, string> TimingDictionary
            {
                get
                {
                    if (_timingLogDictionary == null)
                        _timingLogDictionary = new Dictionary<string, string>();

                    return _timingLogDictionary;
                }
            }

            public Timer Start_Dictionary(string keyForNextMeasurment) {
                NextDictionaryMeasurment(keyForNextMeasurment);
                Start();

                return this;
            }

            public Timer Start_Dictionary(Dictionary<string, string> logDictionary,
                string keyForNextMeasurment)
            {
                _timingLogDictionary = logDictionary;
                NextDictionaryMeasurment(keyForNextMeasurment);
                Start();

                return this;
            }

            public void End_Restart_Dictionary(string keyForNextMeasurment) {
                TimingDictionary[_timingKey] = End_Restart(null, false);
                NextDictionaryMeasurment(keyForNextMeasurment);
            }

            private void NextDictionaryMeasurment(string key)
            {
                _timingKey = key;

                if (TimingDictionary.ContainsKey(_timingKey))
                {
                    _timingKey += TimingDictionary.Count.ToString();
                }

                TimingDictionary[_timingKey] = "...";
            }

            public Dictionary<string,string> End_Dictionary()
            {
                TimingDictionary[_timingKey] = End(null, false);
                var ret = _timingLogDictionary;
                _timingLogDictionary = null;
                return ret;
            }

         

            public void Dispose() {
                if (_timingLogDictionary!= null)
                    End_Dictionary();
                else
                    End();
            }

        }

        #endregion

        #region Html Tags

        public static string HtmlTag(string tag, string value) => "<{0}={1}>".F(tag, value);

        public static string HtmlTag(string tag) => "<{0}>".F(tag);
        
        public static string HtmlTagWrap(string tag, string content) => content.IsNullOrEmpty() ? "" : "<{0}>{1}</{0}>".F(tag, content);

        public static string HtmlTagWrap(string tag, string value, string content) => content.IsNullOrEmpty() ? "" : "<{0}={1}>{2}</{0}>".F(tag, value, content);

        public static string HtmlTagWrap(string content, Color color) => content.IsNullOrEmpty() ? "" : "<{0}={1}>{2}</{0}>".F("color", "#" + ColorUtility.ToHtmlStringRGBA(color), content);

        public static string HtmlTagAlpha(float alpha01) => HtmlTag("alpha", "#" + Mathf.FloorToInt(Mathf.Clamp01(alpha01) * 255).ToString("X2"));


        public static string HtmlTagWrapAlpha(string content, float alpha01) => HtmlTagAlpha(alpha01) + content + HtmlTagAlpha(1f); 

        
        public static StringBuilder AppendHtmlTag(this StringBuilder bld, string tag) => bld.Append(HtmlTag(tag));
        
        public static StringBuilder AppendHtmlTag(this StringBuilder bld, string tag, string value) => bld.Append(HtmlTag(tag, value));
        
        public static StringBuilder AppendHtmlText(this StringBuilder bld, string tag, string value, string content) => bld.Append(HtmlTagWrap(tag, value, content));

        public static StringBuilder AppendHtmlText(this StringBuilder bld, string tag, string content) => bld.Append(HtmlTagWrap(tag, content));

        public static StringBuilder AppendHtmlAlpha(this StringBuilder bld, string content, float alpha) => bld.Append(HtmlTagWrapAlpha(content, alpha));
        
        public static StringBuilder AppendHtmlBold(this StringBuilder bld, string content) => bld.Append(HtmlTagWrap("b", content));
        
        public static StringBuilder AppendHtmlItalics(this StringBuilder bld, string content) => bld.Append(HtmlTagWrap("i", content));

        public static StringBuilder AppendHtml(this StringBuilder bld, string content, Color col) => bld.Append(HtmlTagWrap(content, col)); //content.IsNullOrEmpty() ? bld : bld.AppendHtmlText("color", "#"+ColorUtility.ToHtmlStringRGBA(col), content);

        public static StringBuilder AppendHtmlLink(this StringBuilder bld, string content) => content.IsNullOrEmpty() ? bld : bld.AppendHtmlText("link", "dummy", content);

        public static StringBuilder AppendHtmlLink(this StringBuilder bld, string content, Color col) => content.IsNullOrEmpty() ? bld : 
            bld.AppendHtmlText("link", "dummy", HtmlTagWrap(content, col));


        #endregion

        public static T TryGetClassAttribute<T>(this Type type, bool inherit = false) where T : Attribute
        {
   
            if (!type.IsClass) return null;
            
            var attrs = type.GetCustomAttributes(typeof(T), inherit);
            return (attrs.Length > 0) ? (T) attrs[0] : null;

        }

        #region List Management

        public static void ForceSetCount<T>(this List<T> list, int count) where T : new()
        {
            if (count == list.Count) 
                return;

            while (list.Count < count)
                list.Add(new T());

            while (list.Count > count)
                list.RemoveLast();
        }

        public static List<T> TryAdd<T>(this List<T> list, object ass, bool onlyIfNew = true)
        {

            T toAdd;

            if (list.CanAdd(ref ass, out toAdd, onlyIfNew))
                list.Add(toAdd);

            return list;

        }

        public static List<string> TryAddIfNewAndNotAmpty(this List<string> lst, string text) {
            if (!text.IsNullOrEmpty() && (lst.IndexOf(text) == -1))
                lst.Add(text);

            return lst;
        }

        public static T TryTake<T>(this List<T> list, int index) {

            if (list.IsNullOrEmpty() || list.Count<= index)
                return default;

            var ret = list[index];

            list.RemoveAt(index);

            return ret;
        }

        public static string GetUniqueName<T>(this string s, List<T> list)
        {

            bool match = true;
            int index = 1;
            string mod = s;


            while (match)
            {
                match = false;

                foreach (var l in list)
                    if (l.ToString().SameAs(mod))
                    {
                        match = true;
                        break;
                    }

                if (match)
                {
                    mod = s + index;
                    index++;
                }
            }

            return mod;
        }
        
        public static T GetRandom<T>(this List<T> list) => list.Count == 0 ? default : list[Random.Range(0, list.Count)];
        
        public static void ForceSet<T,G>(this List<T> list, int index, G val) where G:T {
            if (list == null || index < 0) return;

            while (list.Count <= index)
                list.Add(default);

            list[index] = val;
        }
        
        public static bool AddIfNew<T>(this List<T> list, T val)
        {
            if (list.Contains(val))
                return false;

            list.Add(val);
            return true;
        }

        public static bool TryRemoveTill<T>(List<T> list, int maxCountLeft) {
            if (list == null || list.Count <= maxCountLeft) return false;

            list.RemoveRange(maxCountLeft, list.Count - maxCountLeft);
            return true;

        }

        public static T TryGetLast<T>(this IList<T> list)
        {

            if (list == null || list.Count == 0)
                return default;

            return list[list.Count - 1];

        }
        
        public static T TryGet<T>(this List<T> list, int index)
        {
            if (list == null || index < 0 || index >= list.Count)
                return default;
            return list[index];
        }

        public static object TryGetObj(IList list, int index)
        {
            if (list == null || index < 0 || index >= list.Count)
                return null;
            var el = list[index];
            return el;
        }

        public static T TryGet<T>(List<T> list, int index, T defaultValue)
        {
            if (list == null || index < 0 || index >= list.Count)
                return defaultValue;

            return list[index];
        }

        public static int TryGetIndexOrAdd<T>(List<T> list, T obj)
        {
            var ind = -1;
            if (list == null || obj == null) return ind;

            ind = list.IndexOf(obj);

            if (ind != -1) return ind;

            list.Add(obj);
            ind = list.Count - 1;
            return ind;
        }

        public static bool IsNew(this Type t) => t.IsValueType || (!t.IsUnityObject() && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null);

        public static void Move<T>(this List<T> list, int oldIndex, int newIndex) {
            if (oldIndex == newIndex) return;

            var item = list[oldIndex];
            list.RemoveAt(oldIndex);
            list.Insert(newIndex, item);
        }

        public static List<T> RemoveLast<T>(this List<T> list, int count)
        {

            var len = list.Count;

            count = Mathf.Min(count, len);

            var from = len - count;

            var range = list.GetRange(from, count);

            list.RemoveRange(from, count);

            return range;

        }

        public static T RemoveLast<T>(this List<T> list)
        {

            var index = list.Count - 1;

            var last = list[index];

            list.RemoveAt(index);

            return last;
        }

        public static void Swap<T>(this List<T> list, int indexOfFirst)
        {
            var tmp = list[indexOfFirst];
            list[indexOfFirst] = list[indexOfFirst + 1];
            list[indexOfFirst + 1] = tmp;
        }

        public static void Swap<T>(this IList<T> list, int indexA, int indexB)
        {
            var tmp = list[indexA];
            list[indexA] = list[indexB];
            list[indexB] = tmp;
        }

        public static bool IsNullOrEmpty<T>(this ICollection<T> list) => list == null || list.Count == 0;

        public static bool IsNullOrEmptyCollection(this ICollection list) => list == null || list.Count == 0;

        public static List<T> NullIfEmpty<T>(this List<T> list) => (list == null || list.Count == 0) ? null : list;

        public static string CountToString(this IList lst) => lst == null ? "NULL" : lst.Count.ToString();

        #endregion

        #region Array Management
        public static T TryGetLast<T>(this T[] array)
        {

            if (array.IsNullOrEmpty())
                return default;

            return array[array.Length - 1];

        }

        public static T TryGet<T>(this T[] array, int index)
        {

            if (array == null || array.Length <= index || index < 0)
                return default;

            return array[index];
        }

        public static T TryGet<T>(this T[] array, int index, T defaultValue)
        {

            if (array == null || array.Length <= index || index < 0)
                return defaultValue;

            return array[index];
        }

        public static T[] GetCopy<T>(this T[] args)
        {
            var temp = new T[args.Length];
            args.CopyTo(temp, 0);
            return temp;
        }
        
        public static void Swap<T>(ref T[] array, int a, int b)
        {
            if (array == null || a >= array.Length || b >= array.Length || a == b) return;

            var tmp = array[a];
            array[a] = array[b];
            array[b] = tmp;
        }
        
        public static T[] Resize<T>(this T[] args, int to)
        {
            var temp = new T[to];
            if (args != null)
                Array.Copy(args, 0, temp, 0, Mathf.Min(to, args.Length));
          
            return temp;
        }

        public static T[] ExpandBy<T>(this T[] args, int add)
        {
            T[] temp;
            if (args != null)
            {
                temp = new T[args.Length + add];
                args.CopyTo(temp, 0);
            }
            else temp = new T[add];
            return temp;
        }

        public static void Remove<T>(ref T[] args, int ind)
        {
            var temp = new T[args.Length - 1];
            Array.Copy(args, 0, temp, 0, ind);
            var count = args.Length - ind - 1;
            Array.Copy(args, ind + 1, temp, ind, count);
            args = temp;
        }

        public static void AddAndInit<T>(ref T[] args, int add) 
        {
            T[] temp;
            if (args != null)
            {
                temp = new T[args.Length + add];
                args.CopyTo(temp, 0);
            }
            else temp = new T[add];
            args = temp;
            for (var i = args.Length - add; i < args.Length; i++)
                args[i] = Activator.CreateInstance<T>();
        }

        public static T AddAndInit<T>(ref T[] args) where T : new()
        {
            T[] temp;
            if (args != null)
            {
                temp = new T[args.Length + 1];
                args.CopyTo(temp, 0);
            }
            else temp = new T[1];
            args = temp;
            var tmp = new T();
            args[temp.Length - 1] = tmp;
            return tmp;
        }

        public static void InsertAfterAndInit<T>(ref T[] args, int ind) where T : new()
        {
            if ((args != null) && (args.Length > 0))
            {
                var temp = new T[args.Length + 1];
                Array.Copy(args, 0, temp, 0, ind + 1);
                if (ind < args.Length - 1)
                {
                    var count = args.Length - ind - 1;
                    Array.Copy(args, ind + 1, temp, ind + 2, count);
                }
                args = temp;
                args[ind + 1] = new T();
            }
            else
            {

                args = new T[ind + 1];
                for (var i = 0; i < ind + 1; i++)
                    args[i] = new T();
            }


        }


        #endregion

        #region Dictionaries

        public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key) where TValue : new()
        {
            TValue val;

            if (dict.TryGetValue(key, out val)) return val;

            val = new TValue();
            dict.Add(key, val);

            return val;
        }

        public static T TryGet<T>(this Dictionary<string, T> dic, string tag)
        {
            T value;
            dic.TryGetValue(tag, out value);
            return value;
        }

        public static T TryGet<T,G>(this Dictionary<G, T> dic, G tag)
        {
            T value;
            dic.TryGetValue(tag, out value);
            return value;
        }

        public static bool TryChangeKey(this Dictionary<int, string> dic, int before, int now)
        {
            string value;
            if ((dic.TryGetValue(now, out value)) || !dic.TryGetValue(before, out value)) return false;

            dic.Remove(before);
            dic.Add(now, value);
            return true;
        }

        public static bool IsNullOrEmpty<T, TG>(this Dictionary<T, TG> dic) => dic == null || dic.Count == 0;
        #endregion

        #region String Editing

        public static IEnumerator TextAnimation(string fullText, Action<string> feed, float characterFadeInSpeed = 10)
        {
            var segments = fullText.Split(' ');

            int i = 0;
            
            float fadeInTimer = 1f;

            float fadeInSpeed = characterFadeInSpeed* 0.1f;

            var visibleBuilder = new StringBuilder(fullText.Length + 50);
            var resultBuilder = new StringBuilder(fullText.Length + 50);
            
            while (i< segments.Length)
            {

                fadeInTimer -= Time.deltaTime * fadeInSpeed;

                if (fadeInTimer < 0)
                {

                    string fadingSegment = segments[i];

                    if (fadingSegment.Length > 0)
                    {
                        var lastChar = fadingSegment[fadingSegment.Length - 1];

                        float waitFor = 0;

                        switch (lastChar)
                        {
                            case '.': waitFor = 0.4f; break;
                            case '!': waitFor = 0.6f; break;
                            case ',': waitFor = 0.2f; break;
                            case ':': waitFor = 0.15f; break;
                        }

                        while (waitFor > 0)
                        {
                            waitFor -= Time.deltaTime;
                            yield return null;
                        }
                    }


                    visibleBuilder.Append(segments[i]).Append(' ');
                    i++;
                    //newSegment = true;
                    fadeInTimer += 1f;

                    if (i < segments.Length)
                    {
                        
                        fadingSegment = segments[i];
                        
                        var dontFade = true;

                        while (dontFade && i < segments.Length)
                        {
                            dontFade = fadingSegment.Length == 0 || fadingSegment[0] == '<';

                            if (dontFade)
                            {
                                visibleBuilder.Append(fadingSegment).Append(' ');
                                i++;
                            }

                            if (i < segments.Length)
                            {
                                fadingSegment = segments[i];
                            }
                        }

                        fadeInSpeed = characterFadeInSpeed / (1f + fadingSegment.Length);
                    }

                }

                if (i >= segments.Length)
                {
                    feed.Invoke(fullText);
                    yield break; 
                }
                
                string visible = visibleBuilder.ToString();

                string fading =  segments[i];

                resultBuilder.Clear();
                
                resultBuilder.Append(visible)
                    .Append(HtmlTagAlpha(1f - fadeInTimer))
                    .Append(fading)
                    .Append(HtmlTagAlpha(0f))
                    .Append(fullText.Substring(visible.Length + fading.Length));

                feed.Invoke(resultBuilder.ToString()); //visible + HtmlTagAlpha(fadeInTimer) + fading + HtmlTagAlpha(1f) + fullText.Substring(visible.Length + fading.Length));
                
                yield return null;
            }

           

           

        }

        public static string FirstLine(this string str) => new StringReader(str).ReadLine();

        public static string ToPegiStringType(this Type type) => type.ToString().SimplifyTypeName();

        public static string SimplifyTypeName(this string name)
        {
            if (name == null)
                return "TYPE IS A NULL STRING";
            if (name.Length == 0)
                return "TYPE IS EMPTY STRING";

            var ind = Mathf.Max(name.LastIndexOf(".", StringComparison.Ordinal), name.LastIndexOf("+", StringComparison.Ordinal));
            return (ind == -1 || ind > name.Length - 5) ? name : name.Substring(ind + 1);
        }

        public static string SimplifyDirectory(this string name)
        {
            var ind = name.LastIndexOf("/", StringComparison.Ordinal);
            return (ind == -1 || ind > name.Length - 2) ? name : name.Substring(ind + 1);
        }

        public static string ToElipsisString(this string text, int maxLength) {

            if (text == null)
                return "null";

            int index = text.IndexOf(Environment.NewLine, StringComparison.Ordinal);

            if (index > 10)
                text = text.Substring(0, index);

            if (text.Length < (maxLength+3))
                return text;

            return text.Substring(0, maxLength) + "…";

        }

        public static bool SameAs(this string s, string other) => s?.Equals(other) ?? other==null;

        public static bool IsSubstringOf(this string text, string biggerText,
            RegexOptions opt = RegexOptions.IgnoreCase)
        {
            try
            {
                if (Regex.IsMatch(biggerText, text, opt))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                QcUnity.ChillLogger.LogErrorOnce(()=> "Is Substring of({0} -> {1}) Error {2}".F(text, biggerText, ex.ToString()), key: text);
            }
            
            return false;
        }

        public static string RemoveAssetsPart(this string s)
        {
            var ind = s.IndexOf("Assets", StringComparison.Ordinal);
            
            if (ind == 0 || ind == 1) return s.Substring(6 + ind);
            
            return ind > 1 ? s.Substring(0, ind) : s;
        }
        
        public static string RemoveFirst(this string name, int index) =>
            name.Substring(index, name.Length - index);
        
        public static int FindMostSimilarFrom(this string s, string[] t)
        {
            var mostSimilar = -1;
            var distance = 999;
            for (var i = 0; i < t.Length; i++)
            {
                var newDistance = s.LevenshteinDistance(t[i]);
                if (newDistance >= distance) continue;
                mostSimilar = i;
                distance = newDistance;
            }
            return mostSimilar;
        }

        private static int LevenshteinDistance(this string s, string t)
        {

            if (s == null || t == null)
            {
                Debug.Log("Compared string is null: " + (s == null) + " " + (t == null));
                return 999;
            }

            if (s.Equals(t))
                return 0;

            var n = s.Length;
            var m = t.Length;
            var d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
                return m;
            

            if (m == 0)
                return n;
            

            // Step 2
            for (var i = 0; i <= n; d[i, 0] = i++){ }

            for (var j = 0; j <= m; d[0, j] = j++) { }

            // Step 3
            for (var i = 1; i <= n; i++)  
                for (var j = 1; j <= m; j++) {
                    var cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            
            return d[n, m];
        }

        public static bool IsNullOrEmpty(this string s) => string.IsNullOrEmpty(s);

        #endregion

        public static bool IsDefaultOrNull<T>(T obj) => (obj == null) || EqualityComparer<T>.Default.Equals(obj, default);
        
        public static float RoundTo(this float val, int digits) => (float)Math.Round(val, digits);
        
        public static void SetMaximumLength<T>(List<T> list, int length)
        {
            while (list.Count > length)
                list.RemoveAt(0);
        }

        public static T MoveFirstToLast<T>(List<T> list)
        {
            var item = list[0];
            list.RemoveAt(0);
            list.Add(item);
            return item;
        }
        
        public static int CharToInt(this char c) => c - '0';

        #region Type MGMT
        public static List<Type> GetAllChildTypesOf<T>() => GetAllChildTypes(typeof(T));

        public static List<Type> GetAllChildTypes(this Type type)
        {
            var types = Assembly.GetAssembly(type).GetTypes();

            List<Type> list = new List<Type>();

            foreach (var t in types)
            {
                if (t.IsSubclassOf(type) && t.IsClass && !t.IsAbstract && (t != type))
                    list.Add(t);
            }

            return list;
        }

        public static bool ContainsInstanceOfType<T>(this List<T> collection, Type type)
        {

            foreach (var t in collection)
                if (t.GetType() == type) return true;

            return false;
        }
        
        public static List<List<Type>> GetAllChildTypesOf(List<Type> baseTypes)
        {
            var types = new List<List<Type>>();
            for (var i = 0; i < baseTypes.Count; i++)
                types[i] = new List<Type>();

            foreach (var type in Assembly.GetAssembly(baseTypes[0]).GetTypes())
            {
                if (!type.IsClass || type.IsAbstract) continue;
                
                for (var i = 0; i < baseTypes.Count; i++)
                    if (type.IsSubclassOf(baseTypes[i]) && (type != baseTypes[i]))
                    {
                        types[i].Add(type);
                        break;
                    }
            }
            return types;
        }
        #endregion

        public static bool SameAs<T>(this T ind, T other) where T : SafeIndexBase
        {
            if (ind == null && other == null)
                return true;

            if (ind == null || other == null)
                return false;

            return ind.index == other.index;
        }

    }

    public abstract class SafeIndexBase
    {
        public int index;

        public static bool operator ==(SafeIndexBase a, SafeIndexBase b)
        {
            var aIsNull = a is null;// ReferenceEquals(a, null);
            var bIsNull = b is null;//ReferenceEquals(b, null);

           return (aIsNull && bIsNull)
                || ((!aIsNull && !bIsNull)
                    && a.index == b.index && (a.GetType() == b.GetType()));
        }

        public static bool operator !=(SafeIndexBase a, SafeIndexBase b)
            => !(a == b);

        public bool Equals(SafeIndexBase other) => this == other;

        public override bool Equals(object other) => other.GetType() == GetType() && ((SafeIndexBase)other).index == index;

        public override int GetHashCode() => GetType().GetHashCode() + index;
    }

    public class LoopLock : IEnumerator
    {
        private volatile bool _lLock;

        private bool _loopErrorLogged;

        public SkipLock Lock()
        {
            if (_lLock)
                Debug.LogError("Should check it is Unlocked before calling a Lock");

            return new SkipLock(this);
        }

        public bool Unlocked => !_lLock;

        public object Current => _lLock;

        public void Run(Action action)
        {
            if (!Unlocked) return;
            
            using (Lock()) {
                action();
            }
        }

        public class SkipLock : IDisposable
        {
            public void Dispose()
            {
                creator._lLock = false;
            }

            private volatile LoopLock creator;

            public SkipLock(LoopLock make)
            {
                creator = make;
                make._lLock = true;
            }
        }

        public void LogErrorOnce(string msg = "Infinite Loop Detected")
        {
            if (_loopErrorLogged) return;
            
            Debug.LogError(msg);
            _loopErrorLogged = true;
        }

        public bool MoveNext() => _lLock;

        public void Reset() { }
    }

}