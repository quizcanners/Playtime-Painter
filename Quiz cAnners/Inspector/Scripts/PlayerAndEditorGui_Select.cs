using QuizCanners.Migration;
using QuizCanners.Utils;
using System.Collections.Generic;
using UnityEngine;


namespace QuizCanners.Inspect
{
    public static partial class pegi
    {
#pragma warning disable IDE1006 // Naming Styles


        #region SELECT

        private static T filterEditorDropdown<T>(this T obj)
        {
            var edd = obj as IInspectorDropdown;
            return (edd == null || edd.ShowInInspectorDropdown()) ? obj : default;
        }

        private static string CompileSelectionName<T>(int index, T obj, bool showIndex, bool stripSlashes = false, bool dotsToSlashes = true)
        {
            var st = obj.GetNameForInspector();

            if (stripSlashes)
                st = st.SimplifyDirectory();

            if (dotsToSlashes)
                st = st.Replace('.', '/');

            return (showIndex || st.Length == 0) ? "{0}: {1}".F(index, st) : st;
        }

        private static bool selectFinal_Internal(ref int val, ref int index, List<string> namesList)
        {
            var count = namesList.Count;

            if (count == 0)
                return edit(ref val);

            if (index == -1)
            {
                index = namesList.Count;
                namesList.Add("[{0}]".F(val.GetNameForInspector()));

            }

            var tmp = index;

            if (select(ref tmp, namesList.ToArray()) && (tmp < count))
            {
                index = tmp;
                return true;
            }

            return false;

        }

        private static bool selectFinal_Internal<T>(T val, ref int index, List<string> namesList)
        {
            var count = namesList.Count;

            if (index == -1 && !val.IsNullOrDestroyed_Obj())
            {
                index = namesList.Count;
                namesList.Add("[{0}]".F(val.GetNameForInspector()));

            }

            var tmp = index;

            if (select(ref tmp, namesList.ToArray()) && tmp < count)
            {
                index = tmp;
                return true;
            }

            return false;
        }

        #region Select From Int List

        public static bool selectPow2(this string label, ref int current, int min, int max) =>
            label.selectPow2(label, ApproximateLength(label), ref current, min, max);

        public static bool selectPow2(this string label, string tip, int width, ref int current, int min, int max)
        {

            label.write(tip, width);

            return selectPow2(ref current, min, max);

        }

        public static bool selectPow2(ref int current, int min, int max)
        {

            List<int> tmp = new List<int>(4);

            min = Mathf.NextPowerOfTwo(min);

            while (min <= max)
            {
                tmp.Add(min);
                min = Mathf.NextPowerOfTwo(min + 1);
            }

            return select(ref current, tmp);

        }

        internal static bool select(ref int value, List<int> list) => select(ref value, list.ToArray());

        public static bool select(this string text, int width, ref int value, int min, int max)
        {
            write(text, width);
            return select(ref value, min, max);
        }

        public static bool select(ref int value, int min, int max)
        {
            var cnt = max - min + 1;

            var tmp = value - min;
            var array = new int[cnt];
            for (var i = min; i < cnt; i++)
                array[i] = min + i;

            if (select(ref tmp, array))
            {
                value = tmp + min;
                return true;
            }

            return false;
        }

        public static bool select(ref int val, int[] arr, bool showIndex = false, bool stripSlashes = false, bool dotsToSlashes = true)
        {

            checkLine();

            var listNames = new List<string>(arr.Length + 1);

            int tmp = -1;

            for (var i = 0; i < arr.Length; i++)
            {

                var el = arr[i];
                if (el == val)
                    tmp = i;
                listNames.Add(CompileSelectionName(i, el, showIndex, stripSlashes, dotsToSlashes));
            }

            if (selectFinal_Internal(val, ref tmp, listNames))
            {
                val = arr[tmp];
                return true;
            }

            return false;

        }

        #endregion

        #region From Strings

        public static bool select(ref int no, List<string> from)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return from.IsNullOrEmpty() ? "Selecting from null:".edit(90, ref no) : ef.select(ref no, from.ToArray());
#endif


            if (from.IsNullOrEmpty()) return false;

            isFoldout(QcSharp.TryGet(from, no, "..."));

            if (ef.isFoldedOutOrEntered)
            {
                if (from.Count > 1)
                    nl();
                for (var i = 0; i < from.Count; i++)
                    if (i != no && "{0}: {1}".F(i, from[i]).ClickUnFocus().nl())
                    {
                        no = i;
                        FoldInNow();
                        return true;
                    }
            }

            GUILayout.Space(10);

            return false;

        }

        public static bool select(this string text, int width, ref int value, string[] array)
        {
            write(text, width);
            return select(ref value, array);
        }

        public static bool selectFlags(ref int no, string[] from, int width = -1)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return width > 0 ? ef.selectFlags(ref no, from, width) : ef.selectFlags(ref no, from);
#endif

            "Flags Only in Editor for now".write();

            return false;
        }

        private static string tmpSelectSearch;

        private static int SEARCH_SELECTIONTHOLD => PaintingGameViewUI ? 8 : 16;

        public static bool select(ref int no, string[] from, int width = -1)
        {
            var needSearch = from.Length > SEARCH_SELECTIONTHOLD;

#if UNITY_EDITOR
            if (!PaintingGameViewUI && !needSearch)
                return width > 0 ?
                    ef.select(ref no, from, width) :
                    ef.select(ref no, from);
#endif

            if (from.IsNullOrEmpty())
                return false;

            string hint = ef.IsNextFoldedOut ? "{0} ... " : "{0} ... (foldout to select)";

            isFoldout(from.TryGet(no, hint.F(no)));

            if (ef.isFoldedOutOrEntered)
            {
                if (from.Length > 1)
                    nl();

                if (needSearch)
                    "Search".edit(70, ref tmpSelectSearch).nl();

                if (needSearch && !tmpSelectSearch.IsNullOrEmpty())
                {
                    for (var i = 0; i < from.Length; i++)
                        if (i != no && tmpSelectSearch.IsSubstringOf(from[i]) && "{0}: {1}".F(i, from[i]).ClickUnFocus().nl())
                        {
                            no = i;
                            FoldInNow();
                            return true;
                        }
                }
                else
                {

                    for (var i = 0; i < from.Length; i++)
                        if (i != no && "{0}: {1}".F(i, from[i]).ClickUnFocus().nl())
                        {
                            no = i;
                            FoldInNow();
                            return true;
                        }
                }

            }

            GUILayout.Space(10);

            return false;

        }

        public static bool select(ref string val, List<string> lst)
        {
            var ind = -1;

            for (var i = 0; i < lst.Count; i++)
                if (lst[i] != null && lst[i].SameAs(val))
                {
                    ind = i;
                    break;
                }

            if (select(ref ind, lst))
            {
                val = lst[ind];
                return true;
            }

            return false;
        }

        #endregion

        #region UnityObject

        public static bool select(ref SortingLayer sortingLayer)
        {
            var indexes = new List<int>(SortingLayer.layers.Length + 1);
            var values = new List<string>(SortingLayer.layers.Length + 1);

            int selected = -1;

            foreach (var layer in SortingLayer.layers)
            {
                if (layer.Equals(sortingLayer))
                    selected = indexes.Count;

                indexes.Add(layer.id);
                values.Add("{0} [{1}]".F(layer.name, layer.value));
            }

            if (selectFinal_Internal(sortingLayer, ref selected, values))
            {
                sortingLayer = SortingLayer.layers[selected];
                return true;
            }

            return false;
        }

        private static readonly Dictionary<System.Type, List<Object>> objectsInScene = new Dictionary<System.Type, List<Object>>();

        private static List<Object> FindObjects<T>() where T : Object
        {
            var objects = new List<Object>(Object.FindObjectsOfType<T>());

            objectsInScene[typeof(T)] = objects;

            return objects;
        }

        public static bool selectInScene<T>(this string label, ref T obj) where T : Object
        {
            if (!objectsInScene.TryGetValue(typeof(T), out List<Object> objects))
                objects = FindObjects<T>();

            Object o = obj;

            var changed = false;

            if (label.select(ApproximateLength(label), ref o, objects).changes_Internal(ref changed))
                obj = o as T;

            if (icon.Refresh.Click("Refresh List"))
                FindObjects<T>();

            return changed;
        }

        public static bool selectOrAdd<T>(this string label, int width, ref int selected, ref List<T> objs) where T : Object
        {
            label.write(width);
            return selectOrAdd(ref selected, ref objs);
        }

        public static bool selectOrAdd<T>(ref int selected, ref List<T> objcts) where T : Object
        {
            var changed = select_Index(ref selected, objcts);

            var tex = objcts.TryGet(selected);

            if (edit(ref tex, 100).changes_Internal(ref changed))
            {
                if (!tex)
                    selected = -1;
                else
                {
                    var ind = objcts.IndexOf(tex);
                    if (ind >= 0)
                        selected = ind;
                    else
                    {
                        selected = objcts.Count;
                        objcts.Add(tex);
                    }
                }
            }

            return changed;
        }

        #endregion

        #region Select Generic

        public static bool select<T>(this string text, int width, ref T value, List<T> lst, bool showIndex = false, bool stripSlashes = false, bool allowInsert = true)
        {
            write(text, width);
            return select(ref value, lst, showIndex, stripSlashes, allowInsert);
        }

        public static bool select<T>(this string text, ref T value, List<T> list, bool showIndex = false, bool stripSlashes = false, bool allowInsert = true)
        {
            if (list != null && list.Count > SEARCH_SELECTIONTHOLD)
                write(text, 120);
            else
                write(text);

            return select(ref value, list, showIndex, stripSlashes, allowInsert);
        }

        internal static bool select<T>(ref T val, T[] array, bool showIndex = false, bool stripSlashes = false, bool dotsToSlashes = true)
        {
            checkLine();

            var namesList = new List<string>(array.Length + 1);
            var indexList = new List<int>(array.Length + 1);

            var current = -1;

            for (var j = 0; j < array.Length; j++)
            {
                var tmp = array[j];
                if (tmp.filterEditorDropdown().IsDefaultOrNull()) continue;

                if (!val.IsDefaultOrNull() && val.Equals(tmp))
                    current = namesList.Count;

                namesList.Add(CompileSelectionName(j, tmp, showIndex, stripSlashes, dotsToSlashes));
                indexList.Add(j);
            }

            if (selectFinal_Internal(val, ref current, namesList))
            {
                val = array[indexList[current]];
                return true;
            }

            return false;

        }

        public static bool select<T>(ref T val, List<T> lst, bool showIndex = false, bool stripSlashes = false, bool allowInsert = true)
        {
            var changed = false;

            checkLine();

            var names = new List<string>(lst.Count + 1);
            var indexes = new List<int>(lst.Count + 1);

            var currentIndex = -1;

            bool notInTheList = true;

            var currentIsNull = val.IsDefaultOrNull();

            if (lst != null)
            {
                for (var i = 0; i < lst.Count; i++)
                {

                    var tmp = lst[i];
                    if (tmp.filterEditorDropdown().IsDefaultOrNull()) continue;

                    if (!currentIsNull && tmp.Equals(val))
                    {
                        currentIndex = names.Count;
                        notInTheList = false;
                    }

                    names.Add(CompileSelectionName(i, tmp, showIndex, stripSlashes));
                    indexes.Add(i);
                }

                if (selectFinal_Internal(val, ref currentIndex, names))
                {
                    val = lst[indexes[currentIndex]];
                    changed = true;
                }
                else if (allowInsert && notInTheList && !currentIsNull && icon.Insert.Click("Insert into list").changes_Internal(ref changed))
                    lst.Add(val);
            }
            else
                val.GetNameForInspector().write();

            return changed;

        }

        public static bool select_SameClass<T, G>(ref T val, List<G> lst, bool showIndex = false, bool allowInsert = true) where T : class where G : class
        {
            var changed = false;
            var same = typeof(T) == typeof(G);

            checkLine();

            var namesList = new List<string>(lst.Count + 1);
            var indexList = new List<int>(lst.Count + 1);

            var current = -1;

            var notInTheList = true;

            var currentIsNull = val.IsNullOrDestroyed_Obj();

            for (var j = 0; j < lst.Count; j++)
            {
                var tmp = lst[j];
                if (tmp.filterEditorDropdown().IsDefaultOrNull() ||
                    (!same && !typeof(T).IsAssignableFrom(tmp.GetType()))) continue;

                if (!currentIsNull && tmp.Equals(val))
                {
                    current = namesList.Count;
                    notInTheList = false;
                }

                namesList.Add(CompileSelectionName(j, tmp, showIndex));
                indexList.Add(j);
            }

            if (selectFinal_Internal(val, ref current, namesList).changes_Internal(ref changed))
                val = lst[indexList[current]] as T;
            else if (allowInsert && notInTheList && !currentIsNull && icon.Insert.Click("Insert into list").changes_Internal(ref changed))
                lst.TryAdd(val);

            return changed;

        }

        #endregion

        #region Select Index
        public static bool select_Index<T>(this string text, ref int ind, List<T> lst, bool showIndex = false)
        {
            write(text);
            return select_Index(ref ind, lst, showIndex);
        }

        public static bool select_Index<T>(this string text, ref int ind, T[] lst)
        {
            write(text);
            return select_Index(ref ind, lst);
        }

        public static bool select_Index<T>(this string text, int width, ref int ind, T[] lst)
        {
            write(text, width);
            return select_Index(ref ind, lst);
        }

        public static bool select_Index<T>(this string text, string tip, int width, ref int ind, T[] lst, bool showIndex = false)
        {
            write(text, tip, width);
            return select_Index(ref ind, lst, showIndex);
        }

        public static bool select_Index<T>(this string text, string tip, ref int ind, List<T> lst)
        {
            write(text, tip);
            return select_Index(ref ind, lst);
        }

        public static bool select_Index<T>(this string text, int width, ref int ind, List<T> lst, bool showIndex = false)
        {
            write(text, width);
            return select_Index(ref ind, lst, showIndex);
        }

        public static bool select_Index<T>(this string text, string tip, int width, ref int ind, List<T> lst, bool showIndex = false)
        {
            write(text, tip, width);
            return select_Index(ref ind, lst, showIndex);
        }

        public static bool select_Index<T>(ref int ind, List<T> lst, int width) =>
#if UNITY_EDITOR
            (!PaintingGameViewUI) ?
                ef.select(ref ind, lst, width) :
#endif
                select_Index(ref ind, lst);

        public static bool select_Index<T>(ref int ind, List<T> lst, bool showIndex = false, bool stripSlashes = false, bool dotsToSlashes = true)
        {

            checkLine();

            var namesList = new List<string>(lst.Count + 1);
            var indexes = new List<int>(lst.Count + 1);

            var current = -1;

            for (var j = 0; j < lst.Count; j++)
                if (!lst[j].filterEditorDropdown().IsNullOrDestroyed_Obj())
                {
                    if (ind == j)
                        current = indexes.Count;
                    namesList.Add(CompileSelectionName(j, lst[j], showIndex, stripSlashes, dotsToSlashes));
                    indexes.Add(j);
                }

            if (selectFinal_Internal(ind, ref current, namesList))
            {
                ind = indexes[current];
                return true;
            }

            return false;

        }

        public static bool select_Index<T>(ref int ind, T[] arr, bool showIndex = false, bool stripSlashes = false, bool dotsToSlashes = true)
        {

            checkLine();

            var lnms = new List<string>(arr.Length + 1);

            if (arr.ClampIndexToCount(ref ind))
            {
                for (var i = 0; i < arr.Length; i++)
                    lnms.Add(CompileSelectionName(i, arr[i], showIndex, stripSlashes, dotsToSlashes));
            }

            return selectFinal_Internal(ind, ref ind, lnms);

        }

        #endregion

        #region With Lambda
        public static bool select<T>(this string label, ref int val, List<T> list, System.Func<T, bool> lambda, bool showIndex = false)
        {
            write(label);
            return select(ref val, list, lambda, showIndex);
        }

        public static bool select<T>(this string label, int width, ref int val, List<T> list, System.Func<T, bool> lambda, bool showIndex = false)
        {
            write(label, width);
            return select(ref val, list, lambda, showIndex);
        }

        public static bool select<T>(this string label, string tip, int width, ref int val, List<T> list, System.Func<T, bool> lambda, bool showIndex = false)
        {
            write(label, tip, width);
            return select(ref val, list, lambda, showIndex);
        }

        public static bool select<T>(this string text, ref T val, List<T> list, System.Func<T, bool> lambda, bool showIndex = false)
        {
            write(text);
            return select(ref val, list, lambda, showIndex);
        }

        public static bool select<T>(this string text, int width, ref T val, List<T> list, System.Func<T, bool> lambda, bool showIndex = false)
        {
            write(text, width);
            return select(ref val, list, lambda, showIndex);
        }

        public static bool select<T>(this string text, string hint, int width, ref T val, List<T> list, System.Func<T, bool> lambda, bool showIndex = false)
        {
            write(text, hint, width);
            return select(ref val, list, lambda, showIndex);
        }

        public static bool select<T>(ref int val, List<T> lst, System.Func<T, bool> lambda, bool showIndex = false, bool stripSlashes = false, bool dotsToSlashes = true)
        {

            checkLine();

            var names = new List<string>(lst.Count + 1);
            var indexes = new List<int>(lst.Count + 1);

            var current = -1;

            for (var j = 0; j < lst.Count; j++)
            {
                var tmp = lst[j];

                if (tmp.filterEditorDropdown().IsDefaultOrNull() || !lambda(tmp)) continue;

                if (val == j)
                    current = names.Count;
                names.Add(CompileSelectionName(j, tmp, showIndex, stripSlashes, dotsToSlashes));
                indexes.Add(j);
            }


            if (selectFinal_Internal(val, ref current, names))
            {
                val = indexes[current];
                return true;
            }

            return false;
        }

        public static bool select<T>(ref T val, List<T> lst, System.Func<T, bool> lambda, bool showIndex = false, bool stripSlashes = false, bool dotsToSlashes = true)
        {
            var changed = false;

            checkLine();

            var namesList = new List<string>(lst.Count + 1);
            var indexList = new List<int>(lst.Count + 1);

            var current = -1;

            for (var j = 0; j < lst.Count; j++)
            {
                var tmp = lst[j];
                if (tmp.filterEditorDropdown().IsDefaultOrNull() || !lambda(tmp)) continue;

                if (current == -1 && tmp.Equals(val))
                    current = namesList.Count;

                namesList.Add(CompileSelectionName(j, tmp, showIndex, stripSlashes, dotsToSlashes));
                indexList.Add(j);
            }

            if (selectFinal_Internal(val, ref current, namesList).changes_Internal(ref changed))
                val = lst[indexList[current]];

            return changed;

        }

        #endregion

        #region Select Type

        public static bool select(ref System.Type val, List<System.Type> lst, string textForCurrent, bool showIndex = false, bool stripSlashes = false, bool dotsToSlashes = true)
        {
            checkLine();

            var count = lst.Count;
            var names = new List<string>(count + 1);
            var indexes = new List<int>(count + 1);

            var current = -1;

            for (var j = 0; j < count; j++)
            {
                var tmp = lst[j];
                if (tmp.IsDefaultOrNull()) continue;

                if ((!val.IsDefaultOrNull()) && tmp == val)
                    current = names.Count;
                names.Add(CompileSelectionName(j, tmp, showIndex, stripSlashes, dotsToSlashes));
                indexes.Add(j);

            }

            if (current == -1 && val != null)
                names.Add(textForCurrent);

            if (select(ref current, names.ToArray()) && (current < indexes.Count))
            {
                val = lst[indexes[current]];
                return true;
            }

            return false;

        }

        public static bool selectType<T>(this string text, int width, ref T el, ElementData ed = null) where T : class, IGotClassTag
        {
            text.write(width);
            return selectType(ref el, ed);
        }

        public static bool selectType<T>(this string text, ref T el, ElementData ed = null) where T : class, IGotClassTag
        {
            text.write();
            return selectType(ref el, ed);
        }

        public static bool selectType<T>(ref T el, TaggedTypesCfg types, ElementData ed = null) where T : class, IGotClassTag
        {

            object obj = el;

            if (selectType_Obj<T>(ref obj, types, ed))
            {
                el = obj as T;
                return true;
            }
            return false;

        }

        public static bool selectType<T>(ref T el, ElementData ed = null) where T : class, IGotClassTag
        {
            object obj = el;

            var cfg = TaggedTypesCfg.TryGetOrCreate(typeof(T));

            if (selectType_Obj<T>(ref obj, cfg, ed))
            {
                el = obj as T;
                return true;
            }
            return false;
        }

        private static bool selectType_Obj<T>(ref object obj, TaggedTypesCfg cfg, ElementData ed = null) where T : IGotClassTag
        {

            if (ed != null)
                return ed.SelectType(ref obj, cfg);

            var type = obj?.GetType();

            if (cfg.Inspect_Select(ref type).nl())
            {
                var previous = obj;
                obj = (T)System.Activator.CreateInstance(type);
                ICfgExtensions.TryCopy_Std_AndOtherData(previous, obj);
                return true;
            }

            return false;
        }

        public static bool selectTypeTag(this TaggedTypesCfg types, ref string tag) => select(ref tag, types.Keys);
        #endregion

        #region Dictionary
        public static bool select<TKey, TValue>(ref TValue val, Dictionary<TKey, TValue> dic, bool showIndex = false, bool stripSlashes = false, bool allowInsert = true)
            => select(ref val, new List<TValue>(dic.Values), showIndex, stripSlashes, allowInsert);

        public static bool select(ref int current, Dictionary<int, string> from)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.select(ref current, from);

#endif

            var options = new string[from.Count];

            int ind = current;

            for (int i = 0; i < from.Count; i++)
            {
                var e = from.GetElementAt(i);
                options[i] = e.Value;
                if (current == e.Key)
                    ind = i;
            }

            if (select(ref ind, options))
            {
                current = from.GetElementAt(ind).Key;
                return true;
            }
            return false;

        }

        public static bool select(ref int current, Dictionary<int, string> from, int width)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.select(ref current, from, width);
#endif

            var options = new string[from.Count];

            var ind = current;

            for (var i = 0; i < from.Count; i++)
            {
                var e = from.GetElementAt(i);
                options[i] = e.Value;
                if (current == e.Key)
                    ind = i;
            }

            if (select(ref ind, options, width))
            {
                current = from.GetElementAt(ind).Key;
                return true;
            }
            return false;

        }

        public static bool select<TKey, TValue>(this string text, int width, ref TKey key, Dictionary<TKey, TValue> from)
        {
            write(text, width);
            return select(ref key, from);
        }

        public static bool select<TKey, TValue>(this string text, ref TKey key, Dictionary<TKey, TValue> from)
        {
            write(text);
            return select(ref key, from);
        }

        public static bool select<TKey, TValue>(ref TKey key, Dictionary<TKey, TValue> from)
        {
            checkLine();

            if (from == null)
            {
                "Dictionary of {0} for {1} is null ".F(typeof(TValue).ToPegiStringType(), typeof(TKey).ToPegiStringType()).write();
                return false;
            }

            var namesList = new List<string>(from.Count + 1);

            int elementIndex = -1;

            TValue val = default;

            if (key == null)
            {
                for (var i = 0; i < from.Count; i++)
                {
                    var pair = from.GetElementAt(i);
                    namesList.Add("{0}: {1}".F(pair.Key.ToString(), pair.Value.GetNameForInspector()));
                }
            }
            else
            {
                for (var i = 0; i < from.Count; i++)
                {
                    var pair = from.GetElementAt(i);

                    if (key.Equals(pair.Key))
                        elementIndex = i;

                    namesList.Add("{0}: {1}".F(pair.Key.ToString(), pair.Value.GetNameForInspector()));
                }
            }

            if (selectFinal_Internal(val, ref elementIndex, namesList))
            {
                key = from.GetElementAt(elementIndex).Key;
                return true;
            }

            return false;

        }

        #endregion

        #region Select Or Edit
        public static bool select_or_edit_ColorPropertyName(this string name, int width, ref string property, Material material)
        {
            name.write(width);
            return select_or_edit_TexturePropertyName(ref property, material);
        }

        public static bool select_or_edit_ColorProperty(ref string property, Material material)
        {
            var lst = material.GetColorProperties();
            return lst.Count == 0 ? edit(ref property) : select(ref property, lst);
        }

        public static bool select_or_edit_TexturePropertyName(this string name, int width, ref string property, Material material)
        {
            name.write(width);
            return select_or_edit_TexturePropertyName(ref property, material);
        }

        public static bool select_or_edit_TexturePropertyName(ref string property, Material material)
        {
            var lst = material.MyGetTexturePropertiesNames();
            return lst.Count == 0 ? edit(ref property) : select(ref property, lst);
        }

        public static bool select_or_edit_TextureProperty(ref ShaderProperty.TextureValue property, Material material)
        {
            var lst = material.MyGetTextureProperties_Editor();
            return select(ref property, lst, allowInsert: false);

        }

        public static bool select_or_edit<T>(string text, string hint, int width, ref T obj, List<T> list, bool showIndex = false, bool stripSlahes = false, bool allowInsert = true) where T : Object
        {
            if (list.IsNullOrEmpty())
            {
                if (text != null)
                    write(text, hint, width);

                return edit(ref obj);
            }

            var changed = false;
            if (obj && icon.Delete.ClickUnFocus().changes_Internal(ref changed))
                obj = null;

            if (text != null)
                write(text, hint, width);

            select(ref obj, list, showIndex, stripSlahes, allowInsert).changes_Internal(ref changed);

            obj.ClickHighlight();

            return changed;
        }

        public static bool select_or_edit<T>(this string name, ref T obj, List<T> list, bool showIndex = false) where T : Object
            => select_or_edit(name, null, 0, ref obj, list, showIndex);

        public static bool select_or_edit<T>(this string name, int width, ref T obj, List<T> list, bool showIndex = false) where T : Object
        => select_or_edit(name, null, width, ref obj, list, showIndex);

        public static bool select_or_edit<T>(ref T obj, List<T> list, bool showIndex = false) where T : Object
            => select_or_edit(null, null, 0, ref obj, list, showIndex);

        public static bool select_or_edit<T>(this string name, ref int val, List<T> list, bool showIndex = false) =>
             list.IsNullOrEmpty() ? name.edit(ref val) : name.select_Index(ref val, list, showIndex);

        public static bool select_or_edit(ref string val, List<string> list, bool showIndex = false, bool stripSlashes = true, bool allowInsert = true)
        {
            var changed = ChangeTrackStart();

            var gotList = !list.IsNullOrEmpty();

            var gotValue = !val.IsNullOrEmpty();

            if (gotList && gotValue && icon.Delete.ClickUnFocus())
                val = "";

            if (!gotValue || !gotList)
                edit(ref val);

            if (gotList)
                select(ref val, list, showIndex, stripSlashes, allowInsert);

            return changed;
        }

        public static bool select_or_edit(this string name, ref string val, List<string> list, bool showIndex = false)
        {
            var changed = ChangeTrackStart();

            var gotList = !list.IsNullOrEmpty();

            var gotValue = !val.IsNullOrEmpty();

            if (gotList && gotValue && icon.Delete.ClickUnFocus())
                val = "";

            if (!gotValue || !gotList)
                name.edit(ref val);

            if (gotList)
                name.select(ref val, list, showIndex);

            return changed;
        }

        public static bool select_or_edit<T>(this string name, int width, ref int val, List<T> list, bool showIndex = false) =>
            list.IsNullOrEmpty() ? name.edit(width, ref val) : name.select_Index(width, ref val, list, showIndex);

        public static bool select_or_edit<T>(this string name, string hint, int width, ref int val, List<T> list, bool showIndex = false) =>
            list.IsNullOrEmpty() ? name.edit(hint, width, ref val) : name.select_Index(hint, width, ref val, list, showIndex);

        public static bool select_SameClass_or_edit<T, G>(this string text, string hint, int width, ref T obj, List<G> list) where T : Object where G : class
        {
            if (list.IsNullOrEmpty())
                return edit(ref obj);

            var changed = false;

            if (obj && icon.Delete.ClickUnFocus().changes_Internal(ref changed))
                obj = null;

            if (text != null)
                write(text, hint, width);

            select_SameClass(ref obj, list).changes_Internal(ref changed);

            return changed;

        }

        public static bool select_SameClass_or_edit<T, G>(ref T obj, List<G> list) where T : Object where G : class =>
             select_SameClass_or_edit(null, null, 0, ref obj, list);

        public static bool select_SameClass_or_edit<T, G>(this string name, ref T obj, List<G> list) where T : Object where G : class =>
             select_SameClass_or_edit(name, null, 0, ref obj, list);

        public static bool select_SameClass_or_edit<T, G>(this string name, int width, ref T obj, List<G> list) where T : Object where G : class =>
             select_SameClass_or_edit(name, null, width, ref obj, list);

        #endregion

        #region Select IGotIndex
        public static bool select_iGotIndex<T>(this string label, string tip, ref int ind, List<T> lst, bool showIndex = false) where T : IGotIndex
        {
            write(label, tip);
            return select_iGotIndex(ref ind, lst, showIndex);
        }

        public static bool select_iGotIndex<T>(this string label, string tip, int width, ref int ind, List<T> lst, bool showIndex = false) where T : IGotIndex
        {
            write(label, tip, width);
            return select_iGotIndex(ref ind, lst, showIndex);
        }

        public static bool select_iGotIndex<T>(this string label, int width, ref int ind, List<T> lst, bool showIndex = false) where T : IGotIndex
        {
            write(label, width);
            return select_iGotIndex(ref ind, lst, showIndex);
        }

        public static bool select_iGotIndex<T>(this string label, ref int ind, List<T> lst, bool showIndex = false) where T : IGotIndex
        {
            write(label);
            return select_iGotIndex(ref ind, lst, showIndex);
        }

        public static bool select_iGotIndex<T>(ref int ind, List<T> lst, bool showIndex = false) where T : IGotIndex
        {

            if (lst.IsNullOrEmpty())
            {
                return edit(ref ind);
            }

            var names = new List<string>(lst.Count + 1);
            var indexes = new List<int>(lst.Count + 1);

            var current = -1;

            foreach (var el in lst)
                if (!el.filterEditorDropdown().IsNullOrDestroyed_Obj())
                {
                    var index = el.IndexForInspector;

                    if (ind == index)
                        current = indexes.Count;
                    names.Add((showIndex ? index + ": " : "") + el.GetNameForInspector());
                    indexes.Add(index);

                }

            if (selectFinal_Internal(ind, ref current, names))
            {
                ind = indexes[current];
                return true;
            }

            return false;
        }

        public static bool select_iGotIndex<T>(ref int val, List<T> lst, System.Func<T, bool> lambda, bool showIndex = false, bool stripSlashes = false, bool dotsToSlashes = true) where T : IGotIndex
        {

            checkLine();

            var names = new List<string>(lst.Count + 1);
            var indexes = new List<int>(lst.Count + 1);

            var current = -1;

            foreach (var tmp in lst)
            {

                if (tmp.filterEditorDropdown().IsDefaultOrNull() || !lambda(tmp)) continue;

                var ind = tmp.IndexForInspector;

                if (val == ind)
                    current = names.Count;
                names.Add(CompileSelectionName(ind, tmp, showIndex, stripSlashes, dotsToSlashes));
                indexes.Add(ind);

            }

            if (selectFinal_Internal(ref val, ref current, names))
            {
                val = indexes[current];
                return true;
            }

            return false;
        }

        public static bool select_iGotIndex_SameClass<T, G>(this string label, string tip, int width, ref int ind, List<T> lst) where G : class, T where T : IGotIndex
        {
            write(label, tip, width);
            return select_iGotIndex_SameClass<T, G>(ref ind, lst);
        }

        public static bool select_iGotIndex_SameClass<T, G>(this string label, int width, ref int ind, List<T> lst) where G : class, T where T : IGotIndex
        {
            write(label, width);
            return select_iGotIndex_SameClass<T, G>(ref ind, lst);
        }

        public static bool select_iGotIndex_SameClass<T, G>(this string label, ref int ind, List<T> lst) where G : class, T where T : IGotIndex
        {
            write(label);
            return select_iGotIndex_SameClass<T, G>(ref ind, lst);
        }

        public static bool select_iGotIndex_SameClass<T, G>(ref int ind, List<T> lst) where G : class, T where T : IGotIndex
        {
            return select_iGotIndex_SameClass(ref ind, lst, out G _);
        }

        public static bool select_iGotIndex_SameClass<T, G>(this string label, ref int ind, List<T> lst, out G val) where G : class, T where T : IGotIndex
        {
            write(label);
            return select_iGotIndex_SameClass(ref ind, lst, out val);
        }

        public static bool select_iGotIndex_SameClass<T, G>(ref int ind, List<T> lst, out G val) where G : class, T where T : IGotIndex
        {
            val = default;

            if (lst == null)
                return false;

            var count = lst.Count;

            var names = new List<string>(count + 1);
            var indexes = new List<int>(count + 1);
            var els = new List<G>(count + 1);
            var current = -1;

            foreach (var el in lst)
            {
                var g = el as G;

                if (g.filterEditorDropdown().IsNullOrDestroyed_Obj()) continue;

                var index = g.IndexForInspector;

                if (ind == index)
                {
                    current = indexes.Count;
                    val = g;
                }
                names.Add(el.GetNameForInspector());
                indexes.Add(index);
                els.Add(g);

            }

            if (names.Count == 0)
                return edit(ref ind);

            if (selectFinal_Internal(ref ind, ref current, names))
            {
                ind = indexes[current];
                val = els[current];
                return true;
            }

            return false;
        }

        #endregion

        #region Select IGotName

        public static bool select_iGotDisplayName<T>(this string label, int width, ref string name, List<T> lst) where T : IGotReadOnlyName
        {
            write(label, width);
            return select_iGotDisplayName(ref name, lst);
        }

        public static bool select_iGotDisplayName<T>(this string label, string tip, ref string name, List<T> lst) where T : IGotReadOnlyName
        {
            write(label, tip);
            return select_iGotDisplayName(ref name, lst);
        }

        public static bool select_iGotName<T>(this string label, string tip, ref string name, List<T> lst) where T : IGotName
        {
            write(label, tip);
            return select_iGotName(ref name, lst);
        }

        public static bool select_iGotName<T>(this string label, string tip, int width, ref string name, List<T> lst) where T : IGotName
        {
            write(label, tip, width);
            return select_iGotName(ref name, lst);
        }

        public static bool select_iGotName<T>(this string label, int width, ref string name, List<T> lst) where T : IGotName
        {
            write(label, width);
            return select_iGotName(ref name, lst);
        }

        public static bool select_iGotName<T>(this string label, ref string name, List<T> lst) where T : IGotName
        {
            if (lst == null)
                return false;

            write(label);
            return select_iGotName(ref name, lst);
        }

        public static bool select_iGotName<T>(ref string val, List<T> lst) where T : IGotName
        {

            if (lst == null)
                return false;

            var namesList = new List<string>(lst.Count + 1);

            var current = -1;

            foreach (var el in lst)
                if (!el.filterEditorDropdown().IsNullOrDestroyed_Obj())
                {
                    var name = el.NameForInspector;

                    if (name == null) continue;

                    if (val != null && val.SameAs(name))
                        current = namesList.Count;
                    namesList.Add(name);

                }

            if (selectFinal_Internal(val, ref current, namesList))
            {
                val = namesList[current];
                return true;
            }

            return false;
        }

        public static bool select_iGotDisplayName<T>(ref string val, List<T> lst) where T : IGotReadOnlyName
        {

            if (lst == null)
                return false;

            var namesList = new List<string>(lst.Count + 1);

            var current = -1;

            foreach (var el in lst)
                if (!el.filterEditorDropdown().IsNullOrDestroyed_Obj())
                {
                    var name = el.GetNameForInspector();

                    if (name == null) continue;

                    if (val != null && val.SameAs(name))
                        current = namesList.Count;
                    namesList.Add(name);

                }

            if (selectFinal_Internal(val, ref current, namesList))
            {
                val = namesList[current];
                return true;
            }

            return false;
        }


        #endregion

        #endregion


    }
}