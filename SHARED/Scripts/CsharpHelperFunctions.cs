using UnityEngine;
using System.Collections;
using UnityEngine.UI;

using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using System.Linq.Expressions;


using PlayerAndEditorGUI;
using System.Linq;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SharedTools_Stuff
{

    public static class CsharpFuncs {

        #region Timer
        static Stopwatch stopWatch = new Stopwatch();


        public static int timerLastSection = 0;

        static string timerStartLabel = null;

        public static void TimerStart()
        {
            timerStartLabel = null;
            stopWatch.Start();
        }

        public static void TimerStart(this string Label)
        {
            timerStartLabel = Label;
            stopWatch.Start();
        }

        public static string TimerEnd(this string Label) => Label.TimerEnd(true);

        public static string TimerEnd(this string Label, bool logIt) => TimerEnd(Label, logIt, logIt);

        public static string TimerEnd(this string Label, bool logIt, int treshold) => TimerEnd(Label, logIt, logIt, treshold);

        public static string TimerEnd(this string Label, bool logInEditor, bool logInPlayer) => TimerEnd(Label, logInEditor, logInPlayer, 0);

        public static string TimerEnd(this string Label, bool logInEditor, bool logInPlayer, int logTrashold)
        {
            stopWatch.Stop();

            long ticks = stopWatch.ElapsedTicks;

            string timeText = "";

            timerLastSection = (int)ticks;

            if (ticks < 10000)
                timeText = ticks.ToString() + " ticks";
            else timeText = (ticks / 10000).ToString() + " ms " + (ticks % 10000) + " ticks";

            string text = "";
            if (timerStartLabel != null)
                text += timerStartLabel + "->";
            text += Label + ": " + timeText;

            timerStartLabel = null;

            if ((ticks > logTrashold) && (Application.isEditor && logInEditor) || (!Application.isEditor && logInPlayer))
                UnityEngine.Debug.Log(text);

            stopWatch.Reset();

            return text;
        }

        public static string TimerEnd_Restart(this string labelForEndedSection) => labelForEndedSection.TimerEnd_Restart(true);

        public static string TimerEnd_Restart(this string labelForEndedSection, bool logIt) => labelForEndedSection.TimerEnd_Restart(logIt, logIt, 0);

        public static string TimerEnd_Restart(this string labelForEndedSection, bool logIt, int logTreshold) => labelForEndedSection.TimerEnd_Restart(logIt, logIt, logTreshold);

        public static string TimerEnd_Restart(this string labelForEndedSection, bool logInEditor, bool logInPlayer) => labelForEndedSection.TimerEnd_Restart(logInEditor, logInPlayer, 0);

        public static string TimerEnd_Restart(this string labelForEndedSection, bool logInEditor, bool logInPlayer, int logTreshold)
        {
            stopWatch.Stop();
            var txt = TimerEnd(labelForEndedSection, logInEditor, logInPlayer, logTreshold);
            stopWatch.Start();
            return txt;
        }

        #endregion

        #region TextOperations
        public static string F(this string format, string obj1) => string.Format(format, obj1);
        
        public static string F(this string format, object obj1) => string.Format(format, obj1.ToPEGIstring());

        public static string F(this string format, string obj1, string obj2) => string.Format(format, obj1, obj2);
        
        public static string F(this string format, object obj1, object obj2) => string.Format(format, obj1.ToPEGIstring(), obj2.ToPEGIstring());

        public static string F(this string format, string obj1, string obj2, string obj3) => string.Format(format, obj1, obj2, obj3);
        
        public static string F(this string format, object obj1, object obj2, object obj3) => string.Format(format, obj1.ToPEGIstring(), obj2.ToPEGIstring(), obj3.ToPEGIstring());

        public static string F(this string format, params object[] objs) {
            try {
                return string.Format(format, objs);
            } catch(Exception ex) {
                return "Wrong Format" + format + " "+ex.ToString();
            }
        }
        public static string ToSuccessString(this bool value) => value ? "Success" : "Failed";

        #endregion

        public static T TryGetClassAttribute<T>(this Type type) where T : Attribute
        {
            T attr = null;

            if (type.IsClass)
            {
                var attrs = type.GetCustomAttributes(typeof(T), true);
                if (attrs.Length > 0)
                    attr = (T)attrs[0];
            }

            return attr;
        }

        static void AssignUniqueNameIn<T>(this T el, List<T> list)
        {
#if PEGI
            var named = el as IGotName;
            if (named == null) return;

            string tmpName = named.NameForPEGI;
            bool duplicate = true;
            int counter = 0;

            while (duplicate)
            {
                duplicate = false;

                foreach (var e in list)
                {
                    var other = e as IGotName;
                    if ((other != null) && (!e.Equals(el)) && (String.Compare(tmpName, other.NameForPEGI) == 0))
                    {
                        duplicate = true;
                        counter++;
                        tmpName = named.NameForPEGI + counter.ToString();
                        break;
                    }
                }
            }

            named.NameForPEGI = tmpName;
#endif
        }

        #region Casts
        public static bool TryCast<T>(this object obj, out T result)
        {
            if (obj != null)
            {
                if (obj is T)
                {
                    result = (T)obj;
                    return true;
                }
            }

            result = default(T);
            return false;
        }

        public static T TryCast<T>(this object obj)
        {
            if (obj is T)
                return (T)obj;
                
            return default(T);
        }
        #endregion

        #region List Management

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
                    mod = s + index.ToString();
                    index++;
                }
            }

            return mod;
        }

        public static int TotalCount(this List<int>[] lists) {
            int total = 0;

            foreach (var e in lists)
                total += e.Count;

            return total;
        }
        
        public static T GetRandom<T>(this List<T>list) {
            if (list.Count == 0)
                return default(T);

            return list[UnityEngine.Random.Range(0, list.Count)];
        }

        public static void ForceSet<T>(this List<T> list, int index, T val) {
            if (list != null && index>=0) {
                while (list.Count <= index)
                    list.Add(default(T));

                list[index] = val;
            }

        }

        public static bool TryAdd<T>(this List<T> list, object ass) => list.TryAdd(ass, true);
        
        public static bool CanAdd<T>(this List<T> list, ref object obj, out T conv, bool onlyIfNew = true)  {
            conv = default(T);

            if (obj == null || list == null)
                return false;
            
            if (typeof(T).IsSubclassOf(typeof(MonoBehaviour))) {
                var go = obj as GameObject;
                if (go && !go.isNullOrDestroyed()) {

                    conv = go.GetComponent<T>();

                    if (conv == null || (onlyIfNew && list.Contains(conv)))
                        return false;
                    else
                        obj = conv;
                }
                else
                    return false;
            }

            if (obj is T) {

                conv = (T)obj;

                Type objType = obj.GetType();

                var dl = typeof(T).TryGetDerrivedClasses();
                if (dl != null && !dl.Contains(objType))
                    return false;

                if (dl == null) {
                    var tc = typeof(T).TryGetTaggetClasses();

                    if (tc != null && !tc.Types.Contains(objType))
                        return false;
                }

                return true;
            }

            return false;

        }

        public static bool TryAdd<T>(this List<T> list, object ass, bool onlyIfNew = true)   {

            T toAdd;

            if (!list.CanAdd(ref ass, out toAdd, onlyIfNew))
                return false;
            else 
                list.Add(toAdd);

            return true;
      
        }
        
        public static T TryGetLast<T>(this IList<T> list)
        {

            if (list == null || list.Count == 0)
                return default(T);

            return list[list.Count - 1];

        }

        public static T TryGet<T>(this List<T> list, List_Data meta) => list.TryGet(meta.inspected);
        
        public static T TryGet<T>(this List<T> list, int index)
        {
            if (list == null || index < 0 || index >= list.Count)
                return default(T);
            return list[index];
        }

        public static object TryGet(this IList list, int index)
        {
            if (list == null || index < 0 || index >= list.Count)
                return null;
            var el = list[index];
            return el;
        }

        public static T TryGet<T>(this List<T> list, int index, T defaultValue)
        {
            if (list == null || index < 0 || index >= list.Count)
                return defaultValue;

            return list[index];
        }
        
        public static int TryGetIndex<T>(this List<T> list, T obj)
        {
            int ind = -1;
            if (list != null && obj != null)
                ind = list.IndexOf(obj);
            
            return ind;
        }

        public static int TryGetIndexOrAdd<T>(this List<T> list, T obj) {
            int ind = -1;
            if (list != null && obj != null){
                ind = list.IndexOf(obj);
                if (ind == -1){
                    list.Add(obj);
                    ind = list.Count - 1;
                }
            }
            return ind;
        }

        public static void AssignUniqueIndex<T>(this List<T> list, T el)
        {

#if PEGI
            var ind = el as IGotIndex;
            if (ind != null)
            {
                int MaxIndex = ind.IndexForPEGI;
                foreach (var o in list)
                    if (!el.Equals(o))
                {
                    var oind = o as IGotIndex;
                    if (oind != null)
                        MaxIndex = Mathf.Max(MaxIndex, oind.IndexForPEGI  + 1);
                }
                ind.IndexForPEGI = MaxIndex;
            }
#endif
        }

        public static T AddWithUniqueNameAndIndex<T>(this List<T> list) where T : new() => list.AddWithUniqueNameAndIndex("New "
#if PEGI
            + typeof(T).ToPEGIstring_Type()
#endif
            );
        
        public static T AddWithUniqueNameAndIndex<T>(this List<T> list, string name) where T : new() => list.AddWithUniqueNameAndIndex(new T(), name);
        
        public static T AddWithUniqueNameAndIndex<T>(this List<T> list, T e, string name) 
        {
            list.AssignUniqueIndex(e);
            list.Add(e);
#if PEGI
            var named = e as IGotName;
            if (named != null)
                named.NameForPEGI = name;
#endif
            e.AssignUniqueNameIn(list);
            return e;
        }
        
        public static void Move<T>(this List<T> list, int oldIndex, int newIndex) {
            if (oldIndex != newIndex) {
                T item = list[oldIndex];
                list.RemoveAt(oldIndex);
                list.Insert(newIndex, item);
            }
        }

        public static void SetFirst<T>(this List<T> list, T value) {
            for (int i = 0; i < list.Count; i++) 
                if (list[i].Equals(value)) {
                    list.Move(i, 0);
                    return;
                }

            if (list.Count > 0)
                list.Insert(0, value);
            else list.Add(value);
        }

        public static List<T> RemoveLast<T>(this List<T> list, int count)
        {

            int len = list.Count;

            count = Mathf.Min(count, len);

            int from = len - count;

            var range = list.GetRange(from, count);

            list.RemoveRange(from, count);

            return range;

        }

        public static T RemoveLast<T>(this List<T> list)
        {

            int index = list.Count - 1;

            var last = list[index];

            list.RemoveAt(index);

            return last;
        }

        public static T Last<T>(this List<T> list) => list.Count > 0 ? list[list.Count - 1] : default(T);

        public static void Swap<T>(this List<T> list, int indexOfFirst)
        {
            T tmp = list[indexOfFirst];
            list[indexOfFirst] = list[indexOfFirst + 1];
            list[indexOfFirst + 1] = tmp;
        }

        public static void Swap<T>(IList<T> list, int indexA, int indexB)
        {
            T tmp = list[indexA];
            list[indexA] = list[indexB];
            list[indexB] = tmp;
        }

        public static bool IsNullOrEmpty(this IList list) => list == null || list.Count == 0;

        #endregion

        #region Array Management
        public static T TryGetLast<T>(this T[] array)
        {

            if (array == null || array.Length == 0)
                return default(T);

            return array[array.Length - 1];

        }

        public static T TryGet<T>(this T[] array, int index)
        {

            if (array == null || array.Length <= index || index < 0)
                return default(T);

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
            T[] temp = new T[args.Length];
            args.CopyTo(temp, 0);
            return temp;
        }
        
        public static void Swap<T>(ref T[] array, int a, int b)
        {
            if (array != null && a < array.Length && b < array.Length && a != b)
            {
                var tmp = array[a];
                array[a] = array[b];
                array[b] = tmp;
            }
        }
        
        public static void Resize<T>(ref T[] args, int To)
        {
            T[] temp;
            temp = new T[To];
            if (args != null)
                Array.Copy(args, 0, temp, 0, Mathf.Min(To, args.Length));
            else
                args = temp;
        }

        public static void Expand<T>(ref T[] args, int add)
        {
            T[] temp;
            if (args != null)
            {
                temp = new T[args.Length + add];
                args.CopyTo(temp, 0);
            }
            else temp = new T[add];
            args = temp;
        }

        public static void Remove<T>(ref T[] args, int ind)
        {
            T[] temp = new T[args.Length - 1];
            Array.Copy(args, 0, temp, 0, ind);
            int count = args.Length - ind - 1;
            Array.Copy(args, ind + 1, temp, ind, count);
            args = temp;
        }

        public static void AddAndInit<T>(ref T[] args, int add) where T : new()
        {
            T[] temp;
            if (args != null)
            {
                temp = new T[args.Length + add];
                args.CopyTo(temp, 0);
            }
            else temp = new T[add];
            args = temp;
            for (int i = args.Length - add; i < args.Length; i++)
                args[i] = new T();
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
            T tmp = new T();
            args[temp.Length - 1] = tmp;
            return tmp;
        }

        public static void InsertAfterAndInit<T>(ref T[] args, int ind) where T : new()
        {
            if ((args != null) && (args.Length > 0))
            {
                T[] temp = new T[args.Length + 1];
                Array.Copy(args, 0, temp, 0, ind + 1);
                if (ind < args.Length - 1)
                {
                    int count = args.Length - ind - 1;
                    Array.Copy(args, ind + 1, temp, ind + 2, count);
                }
                args = temp;
                args[ind + 1] = new T();
            }
            else
            {

                args = new T[ind + 1];
                for (int i = 0; i < ind + 1; i++)
                    args[i] = new T();
            }


        }


        #endregion

        #region Dictionaries

        public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key) where TValue : new()
        {
            TValue val;

            if (!dict.TryGetValue(key, out val))
            {
                val = new TValue();
                dict.Add(key, val);
            }

            return val;
        }

        public static T TryGet<T>(this Dictionary<string, T> dic, string tag)
        {
            T value;
            dic.TryGetValue(tag, out value);
            return value;
        }

        public static bool TryChangeKey(this Dictionary<int, string> dic, int before, int now)
        {
            string value;
            if ((!dic.TryGetValue(now, out value)) && dic.TryGetValue(before, out value))
            {
                dic.Remove(before);
                dic.Add(now, value);
                return true;
            }
            return false;
        }

        public static bool IsNullOrEmpty<T, G>(this Dictionary<T, G> dic) => dic == null || dic.Count == 0;
        #endregion

        #region String Editing
        public static string ToStringShort(this Vector3 v)
        {
            StringBuilder sb = new StringBuilder();

            if (v.x != 0) sb.Append("x:" + ((int)v.x));
            if (v.y != 0) sb.Append(" y:" + ((int)v.y));
            if (v.z != 0) sb.Append(" z:" + ((int)v.z));

            return sb.ToString();
        }

        public static bool SameAs(this string s, string other) =>
            ((s.IsNullOrEmpty() && other.IsNullOrEmpty())
            || (String.Compare(s, other) == 0));

        public static bool SearchCompare(this string search, string name)
        {
            if ((search.Length == 0) || Regex.IsMatch(name, search, RegexOptions.IgnoreCase)) return true;

            if (search.Contains(" "))
            {
                string[] sgmnts = search.Split(' ');
                for (int i = 0; i < sgmnts.Length; i++)
                    if (!Regex.IsMatch(name, sgmnts[i], RegexOptions.IgnoreCase)) return false;

                return true;
            }
            return false;
        }

        public static string RemoveAssetsPart(this string s)
        {
            var ind = s.IndexOf("Assets");
            if (ind == 0 || ind == 1) return s.Substring(6 + ind);
            if (ind > 1) return s.Substring(0, ind);
            return s;
        }

        public static string AddPreSlashIfNotEmpty(this string s)
        {
            return (s.Length == 0 || (s[0] == '/')) ? s : "/" + s;
        }

        public static string AddPostSlashIfNotEmpty(this string s)
        {
            return (s.Length == 0 || (s[s.Length - 1] == '/')) ? s : s + "/";
        }

        public static string AddPreSlashIfNone(this string s)
        {
            return (s.Length == 0 || (s[0] != '/')) ? "/" + s : s;
        }

        public static string AddPostSlashIfNone(this string s)
        {
            return (s.Length == 0 || (s[s.Length - 1] != '/')) ? s + "/" : s;
        }
        
        public static string RemoveFirst(this string name, int index) =>
            name.Substring(index, name.Length - index);
        
        public static bool IsIncludedIn(this string sub, string big) 
            => Regex.IsMatch(big, sub, RegexOptions.IgnoreCase);
        
        public static int FindMostSimilarFrom(this string s, string[] t)
        {
            int mostSimilar = -1;
            int distance = 999;
            for (int i = 0; i < t.Length; i++)
            {
                int newdist = s.LevenshteinDistance(t[i]);
                if (newdist < distance)
                {
                    mostSimilar = i;
                    distance = newdist;
                }
            }
            return mostSimilar;
        }

        public static int LevenshteinDistance(this string s, string t)
        {

            if ((s == null) || (t == null))
            {
                UnityEngine.Debug.Log("Compared string is null: " + (s == null) + " " + (t == null));
                return 999;
            }

            if (s.CompareTo(t) == 0)
                return 0;

            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
                return m;
            

            if (m == 0)
                return n;
            

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++){ }

            for (int j = 0; j <= m; d[0, j] = j++) { }

            // Step 3
            for (int i = 1; i <= n; i++)  
                for (int j = 1; j <= m; j++) {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            
            return d[n, m];
        }

        public static bool IsNullOrEmpty(this string s) => s == null || s.Length == 0;

        #endregion
        
        public static bool IsDefaultOrNull<T>(this T obj) => (obj == null) || EqualityComparer<T>.Default.Equals(obj, default(T));
        
        public static float RoundTo(this float val, int percision)
        {
            return (float)Math.Round(val, percision);
        }

        public static float RoundTo6Dec(this float val)
        {
            return Mathf.Round(val * 1000000f) * 0.000001f;// 10000f;
        }

        public static void RemoveEmpty<T>(this List<T> list)
        {

            for (int i = 0; i < list.Count; i++)
                if (list[i].IsDefaultOrNull())
                {
                    list.RemoveAt(i);
                    i--;
                }
        }
        
        public static void SetMaximumLength<T>(this List<T> list, int Length)
        {
            while (list.Count > Length)
                list.RemoveAt(0);
        }

        public static T MoveFirstToLast<T>(this List<T> list)
        {
            T item = list[0];
            list.RemoveAt(0);
            list.Add(item);
            return item;
        }

        public static string ReplaceLastOccurrence(this string Source, string Find, string Replace)
        {
            int place = Source.LastIndexOf(Find);

            if (place == -1)
                return Source;

            string result = Source.Remove(place, Find.Length).Insert(place, Replace);
            return result;
        }

        public static int ToIntFromTextSafe(this string text, int defaultReturn)
        {
            int res;
            if (Int32.TryParse(text, out res))
                return res;
            else
                return defaultReturn;
        }

        public static int CharToInt(this char c)
        {

            return (int)(c - '0');
        }

        #region Type MGMT
        public static string GetMemberName<T>(Expression<Func<T>> memberExpression)
        {
            MemberExpression expressionBody = (MemberExpression)memberExpression.Body;
            return expressionBody.Member.Name;
        }

        public static List<Type> GetAllChildTypesOf<T>() =>
            GetAllChildTypes(typeof(T));

        public static List<Type> GetAllChildTypes(this Type type)
        {
            List<Type> types = new List<Type>();
            foreach (Type t in Assembly.GetAssembly(type).GetTypes())
            {
                if (t.IsSubclassOf(type) && t.IsClass && !t.IsAbstract && (t != type))
                {
                    types.Add(t);
                }
            }
            return types;
        }

        public static List<List<Type>> GetAllChildTypesOf(List<Type> baseTypes)
        {
            List<List<Type>> types = new List<List<Type>>();
            for (int i = 0; i < baseTypes.Count; i++)
                types[i] = new List<Type>();

            foreach (Type type in Assembly.GetAssembly(baseTypes[0]).GetTypes())
            {
                if (type.IsClass && !type.IsAbstract)
                {
                    for (int i = 0; i < baseTypes.Count; i++)
                        if (type.IsSubclassOf(baseTypes[i]) && (type != baseTypes[i]))
                        {
                            types[i].Add(type);
                            break;
                        }
                }
            }
            return types;
        }
        #endregion

    }
    
    public class CallsTracker : IPEGI
    {

#if PEGI

        List<Step> steps = new List<Step>();
        Step previous = null;

        #region Inspector

        static CallsTracker inspected;
        int inspectedStep = -1;
        public bool Inspect()
        {
            var changed = false;

            if (icon.Delete.Click("Delete All", ref changed))
                steps.Clear();

            inspected = this;
            "Steps".write_List(steps, ref inspectedStep);

            return changed;
        }

        #endregion

        class Step : IPEGI_ListInspect
        {
            public string tag;
            public int count = 0;
            public List<int> followedBy = new List<int>();

            public int FollowedBy
            {
                get
                {
                    if (followedBy.Count > 0)
                        return followedBy[0];
                    else
                        return -1;
                }
                set
                {
                    followedBy.SetFirst(value);
                }
            }

            public Step(string tagg)
            {
                tag = tagg;
            }

            public void Track()
            {
                count++;
            }

            public bool PEGI_inList(IList list, int ind, ref int edited)
            {
                "{0}: [{1}] => {2}".F(tag, count,
                     (followedBy.Count > 0) ?
                    CallsTracker.inspected.steps[followedBy[0]].tag + (followedBy.Count > 1 ? followedBy.Count.ToString() : "") : "").write();

                if (icon.Refresh.Click())
                    count = 0;

                return false;
            }
        }
#endif


        public void Track(string tag)
        {
#if PEGI

            Step exp = null;

            if (previous != null)
            {
                for (int i = 0; i < previous.followedBy.Count; i++)
                {
                    var e = previous.followedBy[i];
                    var tmp = steps.TryGet(e);
                    if (exp != null && exp.tag.SameAs(tag))
                    {
                        exp = tmp;
                        previous.FollowedBy = i;
                        break;
                    }
                }
            }

            if (exp == null)
            {
                if (previous != null)
                    previous.FollowedBy = steps.Count;

                exp = new Step(tag);
                steps.Add(exp);
            }

            exp.Track();

            previous = exp;
#endif
        }


    }

    public class LoopLock
    {
        volatile bool llock;

        bool loopErrorLogged = false;

        public SkipLock Lock()
        {
            if (llock)
                UnityEngine.Debug.LogError("Should check it is Unlocked before calling a Lock");

            return new SkipLock(this);
        }
        public bool Unlocked => !llock;

        public class SkipLock : IDisposable
        {
            public void Dispose()
            {
                creator.llock = false;
            }

            public volatile LoopLock creator;

            public SkipLock(LoopLock make)
            {
                creator = make;
                make.llock = true;
            }
        }

        public void LogErrorOnce(string msg = "Infinite Loop Detected")
        {
            if (!loopErrorLogged)
            {
                UnityEngine.Debug.LogError(msg);
                loopErrorLogged = true;
            }
        }

    }

}