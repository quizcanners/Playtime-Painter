using System;
using System.Collections.Generic;
using QuizCanners.Utils;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0011 // Add braces
#pragma warning disable IDE0008 // Use explicit type

namespace QuizCanners.Inspect
{
    public static partial class pegi
    {
        public static bool Nested_Inspect(Func<bool> function, Object target = null)
        {
            var changed = false;

            var il = IndentLevel;

            if (function().changes(ref changed))
            {
                if (target)
                    target.SetToDirty();
                else
                    function.Target.SetToDirty_Obj();
            }

            IndentLevel = il;

            return changed;
        }

        public static bool Nested_Inspect<T>(ref T pgi) where T : struct, IPEGI
        {
            if (pgi.IsNullOrDestroyed_Obj())
                return false;

            var isFOOE = ef.isFoldedOutOrEntered;

            int recurses;

            bool wasChanged = ef.globChanged;

            if (!inspectionChain.TryGetValue(pgi, out recurses) || recurses < 4)
            {
                inspectionChain[pgi] = recurses + 1;

                var indent = IndentLevel;

                pgi.Inspect();
                RestoreBGcolor();

                IndentLevel = indent;

                var count = inspectionChain[pgi];
                if (count < 2)
                    inspectionChain.Remove(pgi);
                else
                    inspectionChain[pgi] = count - 1;
            }
            else
                "3rd recursion".writeWarning();

            bool isChanged = ef.globChanged && !wasChanged;

            ef.isFoldedOutOrEntered = isFOOE;

            return isChanged;

        }

        public static bool Nested_Inspect<T>(this T pgi) where T : class, IPEGI
        {

            if (pgi.IsNullOrDestroyed_Obj())
                return false;

            var isFOOE = ef.isFoldedOutOrEntered;

            int recurses;

            bool wasChanged = ef.globChanged;

            if (!inspectionChain.TryGetValue(pgi, out recurses) || recurses < 4)
            {

                inspectionChain[pgi] = recurses + 1;

                var indent = IndentLevel;

                pgi.Inspect();
                RestoreBGcolor();
                //RestoreBGColor();
                //.changes(ref changed);

                IndentLevel = indent;

                var count = inspectionChain[pgi];
                if (count < 2)
                    inspectionChain.Remove(pgi);
                else
                    inspectionChain[pgi] = count - 1;
            }
            else
                "3rd recursion".writeWarning();

            bool isChanged = ef.globChanged && !wasChanged;

            if (isChanged)
            {
#if UNITY_EDITOR
                ef.ClearFromPooledSerializedObjects(pgi as Object);
#endif
                pgi.SetToDirty_Obj();
            }

            ef.isFoldedOutOrEntered = isFOOE;

            return isChanged;

        }

        public static bool Inspect_AsInListNested<T>(this T obj, List<T> list, int current, ref int inspected) where T : IPEGI_ListInspect
        {

            var il = IndentLevel;

            bool wasChanged = ef.globChanged;

            obj.InspectInList(list, current, ref inspected);

            bool isChanged = ef.globChanged && !wasChanged;

            IndentLevel = il;

            if (isChanged)
            {
#if UNITY_EDITOR
                ef.ClearFromPooledSerializedObjects(obj as Object);
#endif
                obj.SetToDirty_Obj();
            }

            return isChanged;
        }

        public static bool Inspect_AsInList(this IPEGI_ListInspect obj)
        {
            var tmp = -1;

            var il = IndentLevel;

            bool wasChanged = ef.globChanged;

            obj.InspectInList(null, 0, ref tmp);
            IndentLevel = il;

            bool isChanged = ef.globChanged && !wasChanged;

            if (isChanged)
            {
#if UNITY_EDITOR
                ef.ClearFromPooledSerializedObjects(obj as Object);
#endif
                obj.SetToDirty_Obj();
            }

            return isChanged;
        }

        public static void BeepSound()
        {
#if UNITY_EDITOR
            EditorApplication.Beep();
#endif
        }

        internal static object SetToDirty_Obj(this object obj)
        {

#if UNITY_EDITOR
            (obj as Object).SetToDirty();
#endif

            return obj;
        }

        public static int focusInd;

        private static Dictionary<IPEGI, int> inspectionChain = new Dictionary<IPEGI, int>();

        internal static void ResetInspectedChain() => inspectionChain.Clear();



#if UNITY_EDITOR
        private static readonly Dictionary<Type, Editor> defaultEditors = new Dictionary<Type, Editor>();
#endif

        private static object TryGetObj(this IList list, int index)
        {
            if (list == null || index < 0 || index >= list.Count)
                return null;
            var el = list[index];
            return el;
        }

        private static bool TryDefaultInspect(Object uObj)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI && uObj)
            {


                Editor ed;
                var t = uObj.GetType();
                if (!defaultEditors.TryGetValue(t, out ed))
                {
                    ed = Editor.CreateEditor(uObj);
                    defaultEditors.Add(t, ed);
                }

                if (ed == null)
                    return false;

                nl();
                EditorGUI.BeginChangeCheck();
                ed.DrawDefaultInspector();
                return EditorGUI.EndChangeCheck();

            }
#endif


            return false;

        }

        private static bool TryDefaultInspect(ref object obj)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {

                var uObj = obj as Object;

                if (uObj)
                {


                    Editor ed;
                    var t = uObj.GetType();
                    if (!defaultEditors.TryGetValue(t, out ed))
                    {
                        ed = Editor.CreateEditor(uObj);
                        defaultEditors.Add(t, ed);
                    }

                    if (ed == null)
                        return false;

                    nl();
                    EditorGUI.BeginChangeCheck();
                    ed.DrawDefaultInspector();
                    return EditorGUI.EndChangeCheck();
                }
            }
#endif



            if (obj != null && obj is string)
            {
                var txt = obj as string;
                if (editBig(ref txt, 40))
                {
                    obj = txt;
                    return true;
                }
            }


            return false;

        }

        public static bool Try_Nested_Inspect(object obj)
        {
            var pgi = obj as IPEGI;
            var ch = pgi?.Nested_Inspect() ?? TryDefaultInspect(ref obj);

            nl();

            UnIndent();

            return ch;
        }

        public static bool Nested_Inspect(ref object obj)
        {
            var pgi = obj as IPEGI;
            var ch = pgi?.Nested_Inspect() ?? TryDefaultInspect(ref obj);

            nl();

            UnIndent();

            return ch;
        }

        public static bool TryInspect<T>(this ListMetaData ld, ref T obj, int ind) where T : Object
        {
            var el = ld.TryGetElement(ind);

            return el?.PEGI_inList_Obj(ref obj) ?? edit(ref obj);
        }

        public static int CountForInspector<T>(this List<T> lst) where T : IGotCount
        {
            var count = 0;

            foreach (var e in lst)
                if (!e.IsNullOrDestroyed_Obj())
                    count += e.CountForInspector();

            return count;
        }

        private static bool IsNullOrDestroyed_Obj(this object obj)
        {
            if (obj as Object)
                return false;

            return obj == null;
        }

        private static bool IsDefaultOrNull<T>(this T obj) => (obj == null) || EqualityComparer<T>.Default.Equals(obj, default);

        public static T GetByIGotIndex<T>(this List<T> lst, int index) where T : IGotIndex
        {
            if (lst != null)
                foreach (var el in lst)
                    if (!el.IsNullOrDestroyed_Obj() && el.IndexForPEGI == index)
                        return el;

            return default;
        }

        public static void AddOrReplaceByIGotIndex<T>(this List<T> list, T newElement) where T: IGotIndex
        {
            var newIndex = newElement.IndexForPEGI;

            for (int i = 0; i < list.Count; i++)
            {
                var el = list[i];
                if (el != null && el.IndexForPEGI == newIndex)
                {
                    list.RemoveAt(i);
                    list.Insert(i, newElement);
                    return;
                }
            }

            list.Add(newElement);
        }

        static bool ToPegiStringInterfacePart(this object obj, out string name)
        {
            name = null;

            var dn = obj as IGotDisplayName;
            if (dn != null)
            {
                name = dn.NameForDisplayPEGI();
                if (!name.IsNullOrEmpty())
                {
                    name = name.FirstLine();
                    return true;
                }

            }

            var sn = obj as IGotName;

            if (sn != null)
            {
                name = sn.NameForPEGI;
                if (!name.IsNullOrEmpty())
                {
                    name = name.FirstLine();
                    return true;
                }
            }

            return false;
        }

        public static string GetNameForInspector_Uobj<T>(this T obj) where T : Object
        {
            if (obj == null)
                return "NULL UObj {0}".F(typeof(T).ToPegiStringType());

            if (!obj)
                return "Destroyed UObj {0}".F(typeof(T).ToPegiStringType());

            string tmp;
            if (obj.ToPegiStringInterfacePart(out tmp)) return tmp;

            var cmp = obj as Component;
            return cmp ? "{0} on {1}".F(cmp.GetType().ToPegiStringType(), cmp.gameObject.name) : obj.name;
        }

        public static string GetNameForInspector<T>(this T obj)
        {
            if (obj.IsNullOrDestroyed_Obj())
                return "NULL {0}".F(typeof(T).ToPegiStringType());

            var type = obj.GetType();

            if (type.IsClass)
            {

                if (obj is string)
                {
                    var str = obj as string;
                    if (str == null)
                        return "NULL String";
                    return str;
                }

                if (obj.GetType().IsUnityObject())
                    return (obj as Object).GetNameForInspector_Uobj();

                string tmp;
                return (obj.ToPegiStringInterfacePart(out tmp)) ? tmp : obj.ToString().SimplifyTypeName();

            }

            if (!type.IsPrimitive)
            {
                string tmp;
                return (obj.ToPegiStringInterfacePart(out tmp)) ? tmp : obj.ToString();
            }

            return obj.ToString();
        }

        public static T GetByIGotName<T>(this List<T> lst, string name) where T : IGotName
        {

            if (lst != null)
                foreach (var el in lst)
                    if (!el.IsNullOrDestroyed_Obj() && el.NameForPEGI.SameAs(name))
                        return el;


            return default;
        }

        public static int GetFreeIndex<T>(this List<T> list) where T : IGotIndex
        {
            CountlessBool bools = new CountlessBool();

            foreach (var el in list)
            {
                bools[el.IndexForPEGI] = true;
            }

            int index = 0;
            while (bools[index] == true)
            {
                index++;
            }

            return index;
        }

    }
}