using System;
using System.Collections.Generic;
using QuizCannersUtilities;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0011 // Add braces
#pragma warning disable IDE0008 // Use explicit type
#pragma warning disable IDE0009 // Member access should be qualified.

namespace PlayerAndEditorGUI
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

        private static object SetToDirty_Obj(this object obj)
        {

#if UNITY_EDITOR
            (obj as Object).SetToDirty();
#endif

            return obj;
        }

        public static int focusInd;

        private static Dictionary<IPEGI, int> inspectionChain = new Dictionary<IPEGI, int>();

        public static void ResetInspectedChain() => inspectionChain.Clear();

        public static bool Nested_Inspect<T>(this T pgi, ref bool changed) where T : class, IPEGI =>
            pgi.Nested_Inspect().changes(ref changed);

        public static bool Nested_Inspect<T>(this T pgi, Object objToSetDirty) where T : class, IPEGI
        {

            if (pgi.Nested_Inspect())
            {
                objToSetDirty.SetToDirty();
                return true;
            }

            return false;
        }

        public static bool Nested_Inspect<T>(this T pgi) where T : class, IPEGI
        {

            if (pgi.IsNullOrDestroyed_Obj())
                return false;

            var isFOOE = ef.isFoldedOutOrEntered;

            var changed = false;

            int recurses;

            if (!inspectionChain.TryGetValue(pgi, out recurses) || recurses < 4)
            {

                inspectionChain[pgi] = recurses + 1;

                var indent = IndentLevel;

                pgi.Inspect().RestoreBGColor().changes(ref changed);

                IndentLevel = indent;

                var count = inspectionChain[pgi];
                if (count < 2)
                    inspectionChain.Remove(pgi);
                else
                    inspectionChain[pgi] = count - 1;
            }
            else
                "3rd recursion".writeWarning();

            if (changed || ef.globChanged)
            {
#if UNITY_EDITOR
                ef.ClearFromPooledSerializedObjects(pgi as Object);
#endif
                pgi.SetToDirty_Obj();
            }

            ef.isFoldedOutOrEntered = isFOOE;

            return changed || ef.globChanged;

        }

        public static bool Inspect_AsInList<T>(this T obj, List<T> list, int current, ref int inspected) where T : IPEGI_ListInspect
        {

            var il = IndentLevel;

            var changes = obj.InspectInList(list, current, ref inspected);

            IndentLevel = il;

            if (ef.globChanged || changes)
            {
#if UNITY_EDITOR
                ef.ClearFromPooledSerializedObjects(obj as Object);
#endif
                obj.SetToDirty_Obj();
            }

            return changes;
        }

        public static bool Inspect_AsInList(this IPEGI_ListInspect obj)
        {
            var tmp = -1;

            var il = IndentLevel;

            var changes = obj.InspectInList(null, 0, ref tmp);
            IndentLevel = il;


            if (ef.globChanged || changes)
            {
#if UNITY_EDITOR
                ef.ClearFromPooledSerializedObjects(obj as Object);
#endif
                obj.SetToDirty_Obj();
            }

            return changes;
        }

#if UNITY_EDITOR
        private static readonly Dictionary<Type, Editor> defaultEditors = new Dictionary<Type, Editor>();
#endif

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

        public static bool Try_Nested_Inspect(GameObject go, Component cmp = null)
        {
            var changed = false;

            IPEGI pgi = null;

            if (cmp)
                pgi = cmp as IPEGI;

            if (pgi == null)
                pgi = go.GetComponent<IPEGI>();

            if (pgi != null)
                pgi.Nested_Inspect().RestoreBGColor().changes(ref changed);
            else
            {
                var mbs = go.GetComponents<Component>();

                foreach (var m in mbs)
                    TryDefaultInspect(m).changes(ref changed);
            }

            nl();
            UnIndent();

            if (changed)
                go.SetToDirty();

            return changed;
        }

        public static bool Try_Nested_Inspect(Component cmp) => cmp && Try_Nested_Inspect(cmp.gameObject, cmp);

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

        public static bool Try_enter_Inspect(object obj, ref int enteredOne, int thisOne)
        {

            var changed = false;

            var l = obj as IPEGI_ListInspect;

            if (l != null)
                return l.enter_Inspect_AsList(ref enteredOne, thisOne);

            var p = obj as IPEGI;

            if (p != null)
            {
                var name = obj as IGotName;

                if (name != null)
                {

                    name.inspect_Name().changes(ref changed);

                    if (icon.Enter.Click())
                        enteredOne = thisOne;

                    return changed;
                }

                return p.GetNameForInspector()
                    .enter_Inspect(p, ref enteredOne, thisOne);
            }

            if (enteredOne == thisOne)
                enteredOne = -1;

            return changed;
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

        public static int TryCountForInspector<T>(this List<T> list)
        {

            if (list.IsNullOrEmpty())
                return 0;

            var count = list.Count;

            foreach (var e in list)
            {
                var cnt = e as IGotCount;
                if (!cnt.IsNullOrDestroyed_Obj())
                    count += cnt.CountForInspector();
            }

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

        public static T GetByIGotIndex<T, G>(this List<T> lst, int index) where T : IGotIndex where G : T
        {
            if (lst != null)
                foreach (var el in lst)
                    if (!el.IsNullOrDestroyed_Obj() && el.IndexForPEGI == index && el.GetType() == typeof(G))
                        return el;

            return default;
        }

        public static T GetByIGotName<T>(this List<T> lst, string name) where T : IGotName
        {

            if (lst != null)
                foreach (var el in lst)
                    if (!el.IsNullOrDestroyed_Obj() && el.NameForPEGI.SameAs(name))
                        return el;


            return default;
        }

        public static T GetByIGotName<T>(this List<T> lst, T other) where T : IGotName
        {
            if (lst != null && !other.IsNullOrDestroyed_Obj())
                foreach (var el in lst)
                    if (!el.IsNullOrDestroyed_Obj() && el.NameForPEGI.SameAs(other.NameForPEGI))
                        return el;

            return default;
        }

        public static G GetByIGotName<T, G>(this List<T> lst, string name) where T : IGotName where G : class, T
        {

            if (lst != null)
                foreach (var el in lst)
                    if (!el.IsNullOrDestroyed_Obj() && el.NameForPEGI.SameAs(name) && el.GetType() == typeof(G))
                        return el as G;

            return default;
        }


    }
}