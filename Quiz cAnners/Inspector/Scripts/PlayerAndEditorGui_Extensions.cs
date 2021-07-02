using System;
using System.Collections.Generic;
using QuizCanners.Utils;
using UnityEngine;

using Object = UnityEngine.Object;
using System.Collections;

#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0011 // Add braces
#pragma warning disable IDE0008 // Use explicit type
#pragma warning disable IDE1006 // Naming Styles

namespace QuizCanners.Inspect
{
    public static partial class pegi
    {

        #region Inspect Name

        public static bool Try_NameInspect(object obj, string label = "", string tip = "")
        {
            return obj.Try_NameInspect(out _, label, tip);
        }

        private static bool Try_NameInspect(this object obj, out bool couldInspect, string label = "", string tip = "")
        {

            var changed = false;

            bool gotLabel = !label.IsNullOrEmpty();

            couldInspect = true;
            var iname = obj as IGotName;
            if (iname != null)
                return iname.inspect_Name(label);

            Object uObj = obj as ScriptableObject;

            if (!uObj)
                uObj = QcUnity.TryGetGameObjectFromObj(obj);

            if (!uObj)
                uObj = obj as Object;

            if (uObj)
            {
                var n = uObj.name;
                if (gotLabel ? label.editDelayed(tip, 80, ref n) : editDelayed(ref n))
                {
                    uObj.name = n;
                    QcUnity.RenameAsset(uObj, n);
                    changed = true;
                }
            }
            else
                couldInspect = false;

            return changed;
        }

        public static bool inspect_Name(this IGotName obj) => obj.inspect_Name("");

        private static bool focusPassedToTheNext;
        public static bool inspect_Name(this IGotName obj, string label)
        {

            var n = obj.NameForInspector;

            bool gotLabel = !label.IsNullOrEmpty();

            var uObj = obj as Object;

            if (uObj)
            {
                if ((gotLabel && label.editDelayed(80, ref n)) || (!gotLabel && editDelayed(ref n)))
                {
                    obj.NameForInspector = n;

                    return true;
                }
            }
            else
            {
                string focusName = InspectedIndex.ToString() + obj.GetNameForInspector();

                if (focusPassedToTheNext)
                {
                    FocusedText = focusName;
                    focusPassedToTheNext = false;
                }

                if (FocusedName.Equals(focusName) && KeyCode.DownArrow.IsDown())
                    focusPassedToTheNext = true;

                NameNextForFocus(focusName);


                if ((gotLabel && label.edit(80, ref n)) || (!gotLabel && edit(ref n)))
                {
                    obj.NameForInspector = n;
                    return true;
                }
            }

            return false;
        }

        #endregion

        public static bool Nested_Inspect(Func<bool> function, Object target)
        {
            var changed = ChangeTrackStart();

            var il = IndentLevel;

            try
            {
                if (function())
                {
                    if (target)
                        target.SetToDirty();
                    else
                        function.Target.SetToDirty_Obj();
                }
            } catch (Exception ex) 
            {
                write(ex);
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

            bool inDic = inspectionChain.TryGetValue(pgi, out recurses);

            if (!inDic || recurses < 4)
            {
                inspectionChain[pgi] = recurses + 1;

                var indent = IndentLevel;

                pgi.Inspect();
                RestoreBGColor();

                IndentLevel = indent;

                int count;
                if (inspectionChain.TryGetValue(pgi, out count))
                {
                    if (count < 2)
                        inspectionChain.Remove(pgi);
                    else
                        inspectionChain[pgi] = count - 1;
                }
            }
            else
                "3rd recursion".writeWarning();

            bool isChanged = ef.globChanged && !wasChanged;

            ef.isFoldedOutOrEntered = isFOOE;

            return isChanged;

        }

        public static bool Nested_Inspect<T>(this T pgi, bool fromNewLine = true) where T : class, IPEGI
        {
            if (fromNewLine) 
                nl();

            if (pgi.IsNullOrDestroyed_Obj())
            {
                "NULL".F(typeof(T).ToPegiStringType()).write();
                return false;
            }

            var isFOOE = ef.isFoldedOutOrEntered;

            int recurses;

            bool wasChanged = ef.globChanged;

            if (!inspectionChain.TryGetValue(pgi, out recurses) || recurses < 4)
            {

                inspectionChain[pgi] = recurses + 1;

                var indent = IndentLevel;

                pgi.Inspect();

                RestoreBGColor();
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

        public static bool Inspect_AsInListNested<T>(this T obj, ref int inspected, int current) where T : IPEGI_ListInspect
        {
            if (!EnterOptionsDrawn_Internal(ref inspected, current))
                return false;

            var change = ChangeTrackStart();

            var il = IndentLevel;

            if (inspected == current)
            {
                if (icon.Back.Click() || obj.GetNameForInspector().ClickLabel().nl())
                    inspected = -1;
                else 
                    Try_Nested_Inspect(obj);
            }
            else
            {
                obj.InspectInList(ref inspected, current);
            }

            nl();

            IndentLevel = il;

            if (change)
            {
#if UNITY_EDITOR
                ef.ClearFromPooledSerializedObjects(obj as Object);
#endif
                obj.SetToDirty_Obj();
            }

            return change;
        }

        public static bool Inspect_AsInList<T>(this T obj) where T: class, IPEGI_ListInspect
        {
            var tmp = -1;

            var il = IndentLevel;

            bool wasChanged = ef.globChanged;

            obj.InspectInList(ref tmp, 0);
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
            UnityEditor.EditorApplication.Beep();
#endif
        }

        public static bool Nested_Inspect(ref object obj)
        {
            var pgi = obj as IPEGI;
            var ch = pgi?.Nested_Inspect() ?? TryDefaultInspect(ref obj);

            nl();

            UnIndent();

            return ch;
        }

        public static bool Inspect_AsInList_Value<T>(ref T obj) where T: struct, IPEGI_ListInspect
        {
            int entered = -1;

            var pgi = obj as IPEGI_ListInspect;
            var ch = false;

            if (pgi != null)
            {
                ch = pgi.Inspect_AsInListNested(ref entered, entered);
                if (ch)
                    obj = (T)pgi;
            }

            nl();

            UnIndent();

            return ch;
        }

        public static bool Try_Inspect_AsInList(ref object obj)
        {
            int entered = -1;

            return Try_Inspect_AsInList(ref obj, ref entered, 0);
        }

        public static bool Try_Inspect_AsInList(ref object obj, ref int entered, int current)
        {
            var pgi = obj as IPEGI_ListInspect;
            var ch = false;

            if (pgi != null)
            {
                ch = pgi.Inspect_AsInListNested(ref entered, current);
                if (ch)
                    obj = pgi;
            }

            nl();

            UnIndent();

            return ch;
        }
        
        public static bool TryDefaultInspect(Object uObj)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI && uObj)
            {
                UnityEditor.Editor ed;
                var t = uObj.GetType();
                if (!defaultEditors.TryGetValue(t, out ed))
                {
                    ed = UnityEditor.Editor.CreateEditor(uObj);
                    defaultEditors.Add(t, ed);
                }

                if (ed == null)
                    return false;

                nl();
                UnityEditor.EditorGUI.BeginChangeCheck();
                ed.DrawDefaultInspector();
                var changed = UnityEditor.EditorGUI.EndChangeCheck();
                if (changed)
                    ef.globChanged = true;

                return changed;

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
                    UnityEditor.Editor ed;
                    var t = uObj.GetType();
                    if (!defaultEditors.TryGetValue(t, out ed))
                    {
                        ed = UnityEditor.Editor.CreateEditor(uObj);
                        defaultEditors.Add(t, ed);
                    }

                    if (ed == null)
                        return false;

                    nl();
                    UnityEditor.EditorGUI.BeginChangeCheck();
                    ed.DrawDefaultInspector();
                    var changed = UnityEditor.EditorGUI.EndChangeCheck();
                    if (changed)
                        ef.globChanged = true;

                    return changed;
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
        
        public static int CountForInspector<T>(this List<T> lst) where T : IGotCount
        {
            var count = 0;

            foreach (var e in lst)
                if (!e.IsNullOrDestroyed_Obj())
                    count += e.GetCount();

            return count;
        }

        private static bool IsNullOrDestroyed_Obj(this object obj)
        {
            var uobj = obj as Object;

            if (uobj!= null)
                return !uobj;

            return obj == null;
        }

        private static bool IsDefaultOrNull<T>(this T obj) => (obj == null) || EqualityComparer<T>.Default.Equals(obj, default);

        public static T GetByIGotIndex<T>(this List<T> lst, int index) where T : IGotIndex
        {
            if (lst != null)
                foreach (var el in lst)
                    if (!el.IsNullOrDestroyed_Obj() && el.IndexForInspector == index)
                        return el;

            return default;
        }

        public static void AddOrReplaceByIGotIndex<T>(this List<T> list, T newElement) where T: IGotIndex
        {
            var newIndex = newElement.IndexForInspector;

            for (int i = 0; i < list.Count; i++)
            {
                var el = list[i];
                if (el != null && el.IndexForInspector == newIndex)
                {
                    list.RemoveAt(i);
                    list.Insert(i, newElement);
                    return;
                }
            }

            list.Add(newElement);
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
                    if (!el.IsNullOrDestroyed_Obj() && el.NameForInspector.SameAs(name))
                        return el;


            return default;
        }

        public static int GetFreeIndex<T>(this List<T> list) where T : IGotIndex
        {
            CountlessBool bools = new CountlessBool();

            foreach (var el in list)
            {
                bools[el.IndexForInspector] = true;
            }

            int index = 0;
            while (bools[index])
                index++;
            
            return index;
        }

        internal static V TryGetByElementIndex<T, V>(this Dictionary<T, V> list, int index, V defaultValue = default)
        {
            if (list == null || index < 0 || index >= list.Count)
                return defaultValue;

            return list.GetElementAt(index).Value;
        }

        internal static object SetToDirty_Obj(this object obj)
        {

#if UNITY_EDITOR
            (obj as Object).SetToDirty();
#endif

            return obj;
        }

        private static readonly Dictionary<IPEGI, int> inspectionChain = new Dictionary<IPEGI, int>();

        internal static void ResetInspectedChain() => inspectionChain.Clear();

#if UNITY_EDITOR
        private static readonly Dictionary<Type, UnityEditor.Editor> defaultEditors = new Dictionary<Type, UnityEditor.Editor>();
#endif

        private static object TryGetObj(this IList list, int index)
        {
            if (list == null || index < 0 || index >= list.Count)
                return null;
            var el = list[index];
            return el;
        }

        private static bool ToPegiStringInterfacePart(this object obj, out string name)
        {
            name = null;

            var dn = obj as IGotReadOnlyName;
            if (dn != null)
            {
                name = dn.GetNameForInspector();
                if (!name.IsNullOrEmpty())
                {
                    name = name.FirstLine();
                    return true;
                }

            }

            var sn = obj as IGotName;

            if (sn != null)
            {
                name = sn.NameForInspector;
                if (!name.IsNullOrEmpty())
                {
                    name = name.FirstLine();
                    return true;
                }
            }

            return false;
        }



    }
}