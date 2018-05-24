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
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SharedTools_Stuff
{

    public static class CsharpFuncs
    {
        static Stopwatch stopWatch = new Stopwatch();

        public static void Log(this string text)
        {

#if UNITY_EDITOR
            UnityEngine.Debug.Log(text);
#endif
        }


        static void AssignUniqueNameIn<T>(this T el, List<T> list)
        {
#if PEGI
            var named = el as iGotName;
            if (named == null) return;

            string tmpName = named.NameForPEGI;
            bool duplicate = true;
            int counter = 0;

            while (duplicate)
            {
                duplicate = false;

                foreach (var e in list)
                {
                    var other = e as iGotName;
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


        public static T AddWithUniqueName<T>(this List<T> list) where T : new()
        {

            T e = new T();
            list.Add(e);
            e.AssignUniqueNameIn(list);
            return e;
        }

        public static T AddWithUniqueName<T>(this List<T> list, string name) where T : new()
        {

            T e = new T();
            list.Add(e);
            #if PEGI
            var named = e as iGotName;
            if (named != null)
                named.NameForPEGI = name;
#endif
            e.AssignUniqueNameIn(list);
            return e;
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


        public static bool isDefaultOrNull<T>(this T obj)
        {
            return EqualityComparer<T>.Default.Equals(obj, default(T));
        }

        public static float RoundTo(this float val, int percision)
        {
            return (float)Math.Round(val, percision);
        }

        public static float RoundTo6Dec(this float val)
        {
            return Mathf.Round(val * 1000000f) * 0.000001f;// 10000f;
        }

        public static void timerFrom()
        {
            stopWatch.Start();
        }

        public static void timerTo(string Label)
        {
            UnityEngine.Debug.Log(Label + ": " + stopWatch.ElapsedTicks);
            stopWatch.Reset();
        }

        public static void timerTo()
        {
            timerTo("Ticks");

        }

        public static void Move<T>(this List<T> list, int oldIndex, int newIndex)
        {
            T item = list[oldIndex];
            list.RemoveAt(oldIndex);
            list.Insert(newIndex, item);
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

        public static T last<T>(this List<T> list)
        {
            return list[list.Count - 1];
        }

        public static string RemoveFirst(this string name, int index)
        {
            return name.Substring(index, name.Length - index);
        }



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
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }

        public static void RemoveEmpty<T>(this List<T> list)
        {

            for (int i = 0; i < list.Count; i++)
                if (list[i].isGenericNull())
                {
                    list.RemoveAt(i);
                    i--;
                }
        }

        public static bool isGenericNull<T>(this T obj)
        {
            return EqualityComparer<T>.Default.Equals(obj, default(T));
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

        public static int ToIntFromText(this string text)
        {
            return Convert.ToInt32(text);
        }

        public static int ToIntFromTextSafe(this string text, int defaultReturn)
        {
            int res;
            if (Int32.TryParse(text, out res))
                return res;
            else
                return defaultReturn;
        }

        public static int charToInt(this char c)
        {

            return (int)(c - '0');
        }

        public static bool isIncludedIn(this string sub, string big)
        {
            return Regex.IsMatch(big, sub, RegexOptions.IgnoreCase);
        }




        public static string GetMemberName<T>(Expression<Func<T>> memberExpression)
        {
            MemberExpression expressionBody = (MemberExpression)memberExpression.Body;
            return expressionBody.Member.Name;
        }


        public static TValue GetAttributeValue<TAttribute, TValue>(
            this Type type,
            Func<TAttribute, TValue> valueSelector)
            where TAttribute : Attribute
        {
            var att = type.GetCustomAttributes(
                typeof(TAttribute), true
            ) as TAttribute;
            if (att != null)
            {
                return valueSelector(att);
            }
            return default(TValue);
        }


        /*
        public static List<Type> GetTypesWithAttribute<TAttribute> (bool inherit) where TAttribute: System.Attribute{

            List<Type> types = new List<Type>();

            foreach (Type type in AppDomain.CurrentDomain.GetAssemblies()) {
                if (type.IsDefined(typeof(TAttribute),inherit))
                    types.Add(type);
            }

            return types;
        }*/

        /*public static List<Type> GetAllChildTypes<T>(this T daddy){
            return GetAllChildTypesOf<T> ();
        }*/

        public static List<Type> GetAllChildTypesOf<T>()
        {
            List<Type> types = new List<Type>();
            foreach (Type type in Assembly.GetAssembly(typeof(T)).GetTypes())
            {
                if (type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(T)) && (type != typeof(T)))
                {
                    types.Add(type);
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


        /*  public static IEnumerable<T> GetEnumerableOfType<T>(params object[] constructorArgs)
          {
              List<T> objects = new List<T>();
              foreach (Type type in Assembly.GetAssembly(typeof(T)).GetTypes())
              {
                  if (type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(T)))
                      objects.Add((T)Activator.CreateInstance(type, constructorArgs));
              }
              objects.Sort();
              return objects;
          }*/

        /*  public static bool ContainsInstanceType(this IEnumerable collection, Type type) {

             foreach (var t in collection)
             {
                 if (t == type) { return true; }//.Any(i => i.GetType() == type);
                 Debug.Log("Comparing "+type.ToString() + " with "+t.ToString() + " Result:  not same");
             }
             return false;
         }*/
    }
}