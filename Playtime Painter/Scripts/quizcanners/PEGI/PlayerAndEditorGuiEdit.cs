using System.Collections.Generic;
using UnityEngine;

using System;
using System.Linq;

using System.Linq.Expressions;
using QuizCannersUtilities;
using UnityEngine.U2D;
using Object = UnityEngine.Object;
using static PlayerAndEditorGUI.PEGI_Styles;

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
        
        #region Changes 

        private static bool change { get { ef.globChanged = true; return true; } }

        private static bool Dirty(this bool val) { ef.globChanged |= val; return val; }

        public static bool changes(this bool value, ref bool changed)
        {
            changed |= value;
            return value;
        }

        private static bool ignoreChanges(this bool changed)
        {
            if (changed)
                ef.globChanged = false;
            return changed;
        }

        private static bool wasChangedBefore;

        private static void bc()
        {
            checkLine();
            wasChangedBefore = GUI.changed;
        }

        private static bool ec() => (GUI.changed && !wasChangedBefore).Dirty();

        #endregion
        
        #region SELECT

        static T filterEditorDropdown<T>(this T obj)
        {
            var edd = obj as IEditorDropdown;
            return (edd == null || edd.ShowInDropdown()) ? obj : default(T);
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

        private static bool selectFinal(ref int val, ref int index, List<string> namesList)
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

        private static bool selectFinal<T>(T val, ref int index, List<string> namesList)
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

            List<int> tmp = new List<int>();

            min = Mathf.NextPowerOfTwo(min);

            while (min <= max)
            {
                tmp.Add(min);
                min = Mathf.NextPowerOfTwo(min + 1);
            }

            return select(ref current, tmp);

        }

        public static bool select(this string label, string tip, ref int value, List<int> list)
        {
            label.write(tip);
            return select(ref value, list);
        }

        public static bool select(this string label, string tip, int width, ref int value, List<int> list)
        {
            label.write(tip, width);
            return select(ref value, list);
        }

        public static bool select(this string label, int width, ref int value, List<int> list)
        {
            label.write(width);
            return select(ref value, list);
        }

        public static bool select(this string label, ref int value, List<int> list)
        {
            label.write(ApproximateLength(label));
            return select(ref value, list);
        }

        public static bool select(ref int value, List<int> list) => select(ref value, list.ToArray());

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

        public static bool select(this string text, ref int ind, int[] arr)
        {
            write(text);
            return select(ref ind, arr);
        }

        public static bool select(this string text, int width, ref int ind, int[] arr)
        {
            write(text, width);
            return select(ref ind, arr);
        }

        public static bool select(this string text, string tip, int width, ref int ind, int[] arr, bool showIndex = false)
        {
            write(text, tip, width);
            return select(ref ind, arr, showIndex);
        }

        public static bool select(ref int val, int[] arr, bool showIndex = false, bool stripSlashes = false, bool dotsToSlashes = true)
        {

            checkLine();

            var lnms = new List<string>();

            int tmp = -1;

            if (arr != null)
                for (var i = 0; i < arr.Length; i++)
                {

                    var el = arr[i];
                    if (el == val)
                        tmp = i;
                    lnms.Add(CompileSelectionName(i, el, showIndex, stripSlashes, dotsToSlashes));
                }

            if (selectFinal(val, ref tmp, lnms))
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

            foldout(from.TryGet(no, "..."));

            if (ef.isFoldedOutOrEntered)
            {
                if (from.Count > 1)
                    nl();
                for (var i = 0; i < from.Count; i++)
                    if (i != no && "{0}: {1}".F(i, from[i]).ClickUnFocus().nl())
                    {
                        no = i;
                        foldIn();
                        return true;
                    }
            }

            GUILayout.Space(10);

            return false;

        }

        public static bool select<T>(ref int no, List<T> from) where T : IGotName
        {

            var lst = new List<string>();

            foreach (var e in from)
                lst.Add(e.GetNameForInspector());

            return select(ref no, lst);
        }

        public static bool select(this string text, ref int value, List<string> list)
        {
            write(text, ApproximateLength(text));
            return select(ref value, list);
        }

        public static bool select(this string text, int width, ref int value, List<string> list)
        {
            write(text, width);
            return select(ref value, list);
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

        public static bool select(ref int no, string[] from, int width = -1)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return width > 0 ?
                    ef.select(ref no, from, width) :
                    ef.select(ref no, from);
#endif

            if (from.IsNullOrEmpty())
                return false;

            foldout(from.TryGet(no, "..."));

            if (ef.isFoldedOutOrEntered)
            {

                if (from.Length > 1)
                    nl();

                for (var i = 0; i < from.Length; i++)
                    if (i != no && "{0}: {1}".F(i, from[i]).ClickUnFocus().nl())
                    {
                        no = i;
                        foldIn();
                        return true;
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

        public static bool select(this string text, ref string val, List<string> lst)
        {
            write(text);
            return select(ref val, lst);
        }

        public static bool select(this string text, int width, ref string val, List<string> lst)
        {
            write(text, width);
            return select(ref val, lst);
        }

        #endregion

        #region UnityObject

        public static bool select(this string label, int width, ref string spriteName, SpriteAtlas atlas)
        {
            label.write(width);
            return select(ref spriteName, atlas);
        }

        public static bool select(this string label, ref string spriteName, SpriteAtlas atlas)
        {
            label.write();
            return select(ref spriteName, atlas);
        }

        public static bool select(ref string spriteName, SpriteAtlas atlas)
        {

            if (!atlas)
            {
                "No Atlas".write();
                return false;
            }

            List<string> names = new List<string>();

            Sprite[] sprites = new Sprite[atlas.spriteCount];

            atlas.GetSprites(sprites);

            foreach (var sp in sprites)
            {
                var n = sp.name;
                int cut = n.LastIndexOf('(');
                if (cut > 0)
                    n = n.Substring(0, cut);

                names.Add(n);

            }

            return select(ref spriteName, names);

        }

        public static bool select(ref SortingLayer sortingLayer)
        {
            var indexes = new List<int>();
            var values = new List<string>();

            int selected = -1;

            foreach (var layer in SortingLayer.layers)
            {
                if (layer.Equals(sortingLayer))
                    selected = indexes.Count;

                indexes.Add(layer.id);
                values.Add("{0} [{1}]".F(layer.name, layer.value));
            }

            if (selectFinal(sortingLayer, ref selected, values))
            {
                sortingLayer = SortingLayer.layers[selected];
                return true;
            }

            return false;
        }

        private static readonly Dictionary<Type, List<Object>> objectsInScene = new Dictionary<Type, List<Object>>();

        static List<Object> FindObjects<T>() where T : Object
        {
            var objects = new List<Object>(Object.FindObjectsOfType<T>());

            objectsInScene[typeof(T)] = objects;

            return objects;
        }

        public static bool selectInScene<T>(this string label, ref T obj) where T : Object
        {

            List<Object> objects;

            if (!objectsInScene.TryGetValue(typeof(T), out objects))
                objects = FindObjects<T>();

            Object o = obj;

            var changed = false;

            if (label.select(ApproximateLength(label), ref o, objects).changes(ref changed))
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

            if (edit(ref tex, 50).changes(ref changed))
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

        public static bool select(ref int no, Texture[] tex)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.select(ref no, tex);

#endif

            if (tex.Length == 0) return false;

            checkLine();

            var tnames = new List<string>();
            var tnumbers = new List<int>();

            var curno = 0;
            for (var i = 0; i < tex.Length; i++)
                if (!tex[i].IsNullOrDestroyed_Obj())
                {
                    tnumbers.Add(i);
                    tnames.Add("{0}: {1}".F(i, tex[i].name));
                    if (no == i) curno = tnames.Count - 1;
                }

            var changed = false;

            if (select(ref curno, tnames.ToArray()).changes(ref changed) && curno >= 0 && curno < tnames.Count)
                no = tnumbers[curno];

            return changed;

        }

        #endregion

        #region Select Audio Clip

        public static bool select(this string text, ref AudioClip clip, List<AudioClip> lst, bool showIndex = false, bool stripSlashes = false, bool allowInsert = true) =>
            text.select(text, ApproximateLength(text), ref clip, lst, showIndex, stripSlashes, allowInsert);

        public static bool select(this string text, int width, ref AudioClip clip, List<AudioClip> lst, bool showIndex = false, bool stripSlashes = false, bool allowInsert = true) =>
            text.select(text, width, ref clip, lst, showIndex, stripSlashes, allowInsert);

        public static bool select(this string text, string tip, ref AudioClip clip, List<AudioClip> lst, bool showIndex = false, bool stripSlashes = false, bool allowInsert = true) =>
            text.select(tip, ApproximateLength(text), ref clip, lst, showIndex, stripSlashes, allowInsert);

        public static bool select(this string text, string tip, int width, ref AudioClip clip, List<AudioClip> lst, bool showIndex = false, bool stripSlashes = false, bool allowInsert = true)
        {
            text.write(tip, width);

            var ret = select(ref clip, lst, showIndex, stripSlashes, allowInsert);

            if (clip && icon.Play.Click(20))
                clip.Play();

            return ret;
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
            write(text);
            return select(ref value, list, showIndex, stripSlashes, allowInsert);
        }

        public static bool select<T>(this string text, ref T value, T[] lst, bool showIndex = false)
        {
            write(text);
            return select(ref value, lst, showIndex);
        }

        public static bool select<T>(ref T val, T[] lst, bool showIndex = false, bool stripSlashes = false, bool dotsToSlashes = true)
        {
            checkLine();

            var namesList = new List<string>();
            var indexList = new List<int>();

            var current = -1;

            for (var j = 0; j < lst.Length; j++)
            {
                var tmp = lst[j];
                if (tmp.filterEditorDropdown().IsDefaultOrNull()) continue;

                if (!val.IsDefaultOrNull() && val.Equals(tmp))
                    current = namesList.Count;

                namesList.Add(CompileSelectionName(j, tmp, showIndex, stripSlashes, dotsToSlashes));
                indexList.Add(j);
            }

            if (selectFinal(val, ref current, namesList))
            {
                val = lst[indexList[current]];
                return true;
            }

            return false;

        }

        public static bool select<T>(ref T val, List<T> lst, bool showIndex = false, bool stripSlashes = false, bool allowInsert = true)
        {
            var changed = false;

            checkLine();

            var names = new List<string>();
            var indexes = new List<int>();

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

                if (selectFinal(val, ref currentIndex, names))
                {
                    val = lst[indexes[currentIndex]];
                    changed = true;
                }
                else if (allowInsert && notInTheList && !currentIsNull && icon.Insert.Click("Insert into list").changes(ref changed))
                    lst.Add(val);
            }
            else
                val.GetNameForInspector().write();



            return changed;

        }

        public static bool select_SameClass<T, G>(ref T val, List<G> lst, bool showIndex = false, bool stripSlashes = false, bool allowInsert = true) where T : class where G : class
        {
            var changed = false;
            var same = typeof(T) == typeof(G);

            checkLine();

            var namesList = new List<string>();
            var indexList = new List<int>();

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

            if (selectFinal(val, ref current, namesList).changes(ref changed))
                val = lst[indexList[current]] as T;
            else if (allowInsert && notInTheList && !currentIsNull && icon.Insert.Click("Insert into list").changes(ref changed))
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

            var namesList = new List<string>();
            var indexes = new List<int>();

            var current = -1;

            for (var j = 0; j < lst.Count; j++)
                if (!lst[j].filterEditorDropdown().IsNullOrDestroyed_Obj())
                {
                    if (ind == j)
                        current = indexes.Count;
                    namesList.Add(CompileSelectionName(j, lst[j], showIndex, stripSlashes, dotsToSlashes));
                    indexes.Add(j);
                }

            if (selectFinal(ind, ref current, namesList))
            {
                ind = indexes[current];
                return true;
            }

            return false;

        }

        public static bool select_Index<T>(ref int ind, T[] arr, bool showIndex = false, bool stripSlashes = false, bool dotsToSlashes = true)
        {

            checkLine();

            var lnms = new List<string>();

            if (arr.ClampIndexToCount(ref ind))
            {
                for (var i = 0; i < arr.Length; i++)
                    lnms.Add(CompileSelectionName(i, arr[i], showIndex, stripSlashes, dotsToSlashes));
            }

            return selectFinal(ind, ref ind, lnms);

        }


        #endregion

        #region With Lambda
        public static bool select<T>(this string label, ref int val, List<T> list, Func<T, bool> lambda, bool showIndex = false)
        {
            write(label);
            return select(ref val, list, lambda, showIndex);
        }

        public static bool select<T>(this string label, int width, ref int val, List<T> list, Func<T, bool> lambda, bool showIndex = false)
        {
            write(label, width);
            return select(ref val, list, lambda, showIndex);
        }

        public static bool select<T>(this string label, string tip, int width, ref int val, List<T> list, Func<T, bool> lambda, bool showIndex = false)
        {
            write(label, tip, width);
            return select(ref val, list, lambda, showIndex);
        }

        public static bool select<T>(this string text, ref T val, List<T> list, Func<T, bool> lambda, bool showIndex = false)
        {
            write(text);
            return select(ref val, list, lambda, showIndex);
        }

        public static bool select<T>(this string text, int width, ref T val, List<T> list, Func<T, bool> lambda, bool showIndex = false)
        {
            write(text, width);
            return select(ref val, list, lambda, showIndex);
        }

        public static bool select<T>(this string text, string hint, int width, ref T val, List<T> list, Func<T, bool> lambda, bool showIndex = false)
        {
            write(text, hint, width);
            return select(ref val, list, lambda, showIndex);
        }

        public static bool select<T>(ref int val, List<T> lst, Func<T, bool> lambda, bool showIndex = false, bool stripSlashes = false, bool dotsToSlashes = true)
        {

            checkLine();

            var names = new List<string>();
            var indexes = new List<int>();

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


            if (selectFinal(val, ref current, names))
            {
                val = indexes[current];
                return true;
            }

            return false;
        }

        public static bool select<T>(ref T val, List<T> lst, Func<T, bool> lambda, bool showIndex = false, bool stripSlashes = false, bool dotsToSlashes = true)
        {
            var changed = false;

            checkLine();

            var namesList = new List<string>();
            var indexList = new List<int>();

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

            if (selectFinal(val, ref current, namesList).changes(ref changed))
                val = lst[indexList[current]];

            return changed;

        }

        #endregion

        #region Countless
        public static bool select<T>(this string label, int width, ref int no, Countless<T> tree)
        {
            label.write(width);
            return select(ref no, tree);
        }

        public static bool select<T>(this string label, ref int no, Countless<T> tree)
        {
            label.write();
            return select(ref no, tree);
        }

        public static bool select<T>(ref int no, Countless<T> tree)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.select(ref no, tree);
#endif


            List<int> inds;
            var objs = tree.GetAllObjs(out inds);
            var filtered = new List<string>();
            var tmpindex = -1;
            for (var i = 0; i < objs.Count; i++)
            {
                if (no == inds[i])
                    tmpindex = i;
                filtered.Add(objs[i].GetNameForInspector());
            }

            if (tmpindex == -1)
                filtered.Add(">>{0}<<".F(no.GetNameForInspector()));

            if (select(ref tmpindex, filtered.ToArray()) && tmpindex < inds.Count)
            {
                no = inds[tmpindex];
                return true;
            }
            return false;

        }

        public static bool select<T>(this string label, int width, ref int no, Countless<T> tree, Func<T, bool> lambda)
        {
            label.write(width);
            return select(ref no, tree, lambda);
        }

        public static bool select<T>(this string label, ref int no, Countless<T> tree, Func<T, bool> lambda)
        {
            label.write();
            return select(ref no, tree, lambda);
        }

        public static bool select<T>(ref int no, CountlessCfg<T> tree) where T : ICfg, new()
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.select(ref no, tree);
#endif

            List<int> inds;
            var objs = tree.GetAllObjs(out inds);
            var filtered = new List<string>();
            var tmpindex = -1;
            for (var i = 0; i < objs.Count; i++)
            {
                if (no == inds[i])
                    tmpindex = i;
                filtered.Add(objs[i].GetNameForInspector());
            }

            if (select(ref tmpindex, filtered.ToArray()))
            {
                no = inds[tmpindex];
                return true;
            }
            return false;

        }

        public static bool select<T>(ref int no, Countless<T> tree, Func<T, bool> lambda)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.select(ref no, tree);
#endif

            List<int> unfinds;
            var indexes = new List<int>();
            var objects = tree.GetAllObjs(out unfinds);
            var namesList = new List<string>();
            var current = -1;
            var j = 0;
            for (var i = 0; i < objects.Count; i++)
            {

                var el = objects[i];

                if (el.filterEditorDropdown().IsNullOrDestroyed_Obj() || !lambda(el)) continue;

                indexes.Add(unfinds[i]);

                if (no == indexes[j])
                    current = j;

                namesList.Add(objects[i].GetNameForInspector());
                j++;
            }


            if (selectFinal(no, ref current, namesList))
            {
                no = indexes[current];
                return true;
            }
            return false;

        }

        public static bool select(CountlessInt cint, int ind, Dictionary<int, string> from)
        {

            var value = cint[ind];

            if (select(ref value, from))
            {
                cint[ind] = value;
                return true;
            }
            return false;
        }

        #endregion

        #region Enum
        public static bool selectEnum<T>(ref int current) => selectEnum(ref current, typeof(T));

        public static bool selectEnum<T>(this string label, int width, ref int current, List<int> options)
        {
            label.write(width);
            return selectEnum<T>(ref current, options);
        }

        public static bool selectEnum<T>(ref int eval, List<int> options, int width = -1) =>
            selectEnum(ref eval, typeof(T), options, width);

        public static bool selectEnum<T>(ref T eval, List<int> options, int width = -1)
        {
            var val = Convert.ToInt32(eval);

            if (selectEnum(ref val, typeof(T), options, width))
            {
                eval = (T)((object)val);
                return true;
            }

            return false;
        }

        public static bool selectEnum(ref int current, Type type, int width = -1)
        {
            checkLine();
            var tmpVal = -1;

            var names = Enum.GetNames(type);
            var val = (int[])Enum.GetValues(type);

            for (var i = 0; i < val.Length; i++)
                if (val[i] == current)
                    tmpVal = i;

            if (!select(ref tmpVal, names, width)) return false;

            current = val[tmpVal];
            return true;
        }

        public static bool selectEnumFlags(ref int current, Type type, int width = -1)
        {

            checkLine();

            var names = Enum.GetNames(type);
            var values = (int[])Enum.GetValues(type);

            Countless<string> sortedNames = new Countless<string>();

            int currentPower = 0;

            int toPow = 1;

            for (var i = 0; i < values.Length; i++)
            {
                var val = values[i];
                while (val > toPow)
                {
                    currentPower++;
                    toPow = (int)Mathf.Pow(2, currentPower);
                }

                if (val == toPow)
                    sortedNames[currentPower] = names[i];
            }

            string[] snms = new string[currentPower + 1];

            for (int i = 0; i <= currentPower; i++)
                snms[i] = sortedNames[i];

            return selectFlags(ref current, snms, width);
        }

        public static bool selectEnum(ref int current, Type type, List<int> options, int width = -1)
        {
            checkLine();
            var tmpVal = -1;

            List<string> names = new List<string>();

            for (var i = 0; i < options.Count; i++)
            {
                var op = options[i];
                names.Add(Enum.GetName(type, op));
                if (options[i] == current)
                    tmpVal = i;
            }

            if (width == -1 ? select(ref tmpVal, names) : select_Index(ref tmpVal, names, width))
            {
                current = options[tmpVal];
                return true;
            }

            return false;
        }


        public static bool selectEnum<T>(this string text, string tip, int width, ref int eval)
        {
            write(text, tip, width);
            return selectEnum<T>(ref eval);
        }

        public static bool selectEnum<T>(this string text, int width, ref int eval)
        {
            write(text, width);
            return selectEnum<T>(ref eval);
        }

        public static bool selectEnum<T>(this string text, ref int eval)
        {
            write(text);
            return selectEnum<T>(ref eval);
        }
        #endregion

        #region Select Type

        public static bool select(ref Type val, List<Type> lst, string textForCurrent, bool showIndex = false, bool stripSlashes = false, bool dotsToSlashes = true)
        {
            checkLine();

            var names = new List<string>();
            var indexes = new List<int>();

            var current = -1;

            for (var j = 0; j < lst.Count; j++)
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

        public static bool selectType<T>(this string text, int width, ref T el, ElementData ed = null, bool keepTypeConfig = true) where T : class, IGotClassTag
        {
            text.write(width);
            return selectType(ref el, ed, keepTypeConfig);
        }

        public static bool selectType<T>(this string text, ref T el, ElementData ed = null, bool keepTypeConfig = true) where T : class, IGotClassTag
        {
            text.write();
            return selectType(ref el, ed, keepTypeConfig);
        }

        public static bool selectType<T>(ref T el, TaggedTypesCfg types, ElementData ed = null, bool keepTypeConfig = true) where T : class, IGotClassTag
        {

            object obj = el;

            if (selectType_Obj<T>(ref obj, types, ed, keepTypeConfig))
            {
                el = obj as T;
                return true;
            }
            return false;

        }

        public static bool selectType<T>(ref T el, ElementData ed = null, bool keepTypeConfig = true) where T : class, IGotClassTag
        {
            object obj = el;

            var cfg = TaggedTypesCfg.TryGetOrCreate(typeof(T));

            if (selectType_Obj<T>(ref obj, cfg, ed, keepTypeConfig))
            {
                el = obj as T;
                return true;
            }
            return false;
        }

        private static bool selectType_Obj<T>(ref object obj, TaggedTypesCfg cfg, ElementData ed = null, bool keepTypeConfig = true) where T : IGotClassTag
        {

            if (ed != null)
                return ed.SelectType<T>(ref obj, cfg, keepTypeConfig);

            var type = obj?.GetType();

            if (cfg.Select(ref type).nl())
            {
                var previous = obj;
                obj = (T)Activator.CreateInstance(type);
                StdExtensions.TryCopy_Std_AndOtherData(previous, obj);
                return true;
            }

            return false;
        }

        public static bool selectTypeTag(this TaggedTypesCfg types, ref string tag) => select(ref tag, types.Keys);
        #endregion

        #region Dictionary
        public static bool select<G, T>(ref T val, Dictionary<G, T> dic, bool showIndex = false, bool stripSlashes = false, bool allowInsert = true) => select(ref val, new List<T>(dic.Values), showIndex, stripSlashes, allowInsert);

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
                var e = from.ElementAt(i);
                options[i] = e.Value;
                if (current == e.Key)
                    ind = i;
            }

            if (select(ref ind, options))
            {
                current = from.ElementAt(ind).Key;
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
                var e = from.ElementAt(i);
                options[i] = e.Value;
                if (current == e.Key)
                    ind = i;
            }

            if (select(ref ind, options, width))
            {
                current = from.ElementAt(ind).Key;
                return true;
            }
            return false;

        }

        public static bool select<T>(this string text, int width, ref int key, Dictionary<int, T> from)
        {
            write(text, width);
            return select(ref key, from);
        }

        public static bool select<T>(this string text, ref int key, Dictionary<int, T> from)
        {
            write(text);
            return select(ref key, from);
        }

        public static bool select<T>(ref int key, Dictionary<int, T> from)
        {


            checkLine();

            var namesList = new List<string>();
            var indexes = new List<int>();

            int elementIndex = -1;

            T val = default(T);

            for (var i = 0; i < from.Count; i++)
            {

                var pair = from.ElementAt(i);


                if (key == pair.Key)
                    elementIndex = i;

                namesList.Add("{0}: {1}".F(pair.Key.ToString(), pair.Value.GetNameForInspector()));
                indexes.Add(i);

            }

            if (selectFinal(val, ref elementIndex, namesList))
            {
                key = from.ElementAt(elementIndex).Key;
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
            if (obj && icon.Delete.ClickUnFocus().changes(ref changed))
                obj = null;

            if (text != null)
                write(text, hint, width);

            select(ref obj, list, showIndex, stripSlahes, allowInsert).changes(ref changed);

            obj.ClickHighlight();

            return changed;
        }

        public static bool select_or_edit<T>(this string name, ref T obj, List<T> list, bool showIndex = false) where T : UnityEngine.Object
            => select_or_edit(name, null, 0, ref obj, list, showIndex);

        public static bool select_or_edit<T>(this string name, int width, ref T obj, List<T> list, bool showIndex = false) where T : UnityEngine.Object
        => select_or_edit(name, null, width, ref obj, list, showIndex);

        public static bool select_or_edit<T>(ref T obj, List<T> list, bool showIndex = false) where T : UnityEngine.Object
            => select_or_edit(null, null, 0, ref obj, list, showIndex);

        public static bool select_or_edit<T>(this string name, ref int val, List<T> list, bool showIndex = false) =>
             list.IsNullOrEmpty() ? name.edit(ref val) : name.select_Index(ref val, list, showIndex);

        public static bool select_or_edit(ref string val, List<string> list, bool showIndex = false, bool stripSlashes = true, bool allowInsert = true)
        {
            var changed = false;

            var gotList = !list.IsNullOrEmpty();

            var gotValue = !val.IsNullOrEmpty();

            if (gotList && gotValue && icon.Delete.ClickUnFocus(ref changed))
                val = "";

            if (!gotValue || !gotList)
                edit(ref val).changes(ref changed);

            if (gotList)
                select(ref val, list, showIndex, stripSlashes, allowInsert).changes(ref changed);

            return changed;
        }

        public static bool select_or_edit(this string name, ref string val, List<string> list, bool showIndex = false)
        {
            var changed = false;

            var gotList = !list.IsNullOrEmpty();

            var gotValue = !val.IsNullOrEmpty();

            if (gotList && gotValue && icon.Delete.ClickUnFocus(ref changed))
                val = "";

            if (!gotValue || !gotList)
                name.edit(ref val).changes(ref changed);

            if (gotList)
                name.select(ref val, list, showIndex).changes(ref changed);

            return changed;
        }

        public static bool select_or_edit<T>(this string name, int width, ref int val, List<T> list, bool showIndex = false) =>
            list.IsNullOrEmpty() ? name.edit(width, ref val) : name.select_Index(width, ref val, list, showIndex);

        public static bool select_or_edit<T>(this string name, string hint, int width, ref int val, List<T> list, bool showIndex = false) =>
            list.IsNullOrEmpty() ? name.edit(hint, width, ref val) : name.select_Index(hint, width, ref val, list, showIndex);

        public static bool select_SameClass_or_edit<T, G>(this string text, string hint, int width, ref T obj, List<G> list) where T : UnityEngine.Object where G : class
        {
            if (list.IsNullOrEmpty())
                return edit(ref obj);

            var changed = false;

            if (obj && icon.Delete.ClickUnFocus().changes(ref changed))
                obj = null;

            if (text != null)
                write(text, hint, width);

            select_SameClass(ref obj, list).changes(ref changed);

            return changed;

        }

        public static bool select_SameClass_or_edit<T, G>(ref T obj, List<G> list) where T : UnityEngine.Object where G : class =>
             select_SameClass_or_edit(null, null, 0, ref obj, list);

        public static bool select_SameClass_or_edit<T, G>(this string name, ref T obj, List<G> list) where T : UnityEngine.Object where G : class =>
             select_SameClass_or_edit(name, null, 0, ref obj, list);

        public static bool select_SameClass_or_edit<T, G>(this string name, int width, ref T obj, List<G> list) where T : UnityEngine.Object where G : class =>
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

            var names = new List<string>();
            var indexes = new List<int>();

            var current = -1;

            foreach (var el in lst)
                if (!el.filterEditorDropdown().IsNullOrDestroyed_Obj())
                {
                    var index = el.IndexForPEGI;

                    if (ind == index)
                        current = indexes.Count;
                    names.Add((showIndex ? index + ": " : "") + el.GetNameForInspector());
                    indexes.Add(index);

                }

            if (selectFinal(ind, ref current, names))
            {
                ind = indexes[current];
                return true;
            }

            return false;
        }

        public static bool select_iGotIndex<T>(ref int val, List<T> lst, Func<T, bool> lambda, bool showIndex = false, bool stripSlashes = false, bool dotsToSlashes = true) where T : IGotIndex
        {

            checkLine();

            var names = new List<string>();
            var indexes = new List<int>();

            var current = -1;

            foreach (var tmp in lst)
            {

                if (tmp.filterEditorDropdown().IsDefaultOrNull() || !lambda(tmp)) continue;

                var ind = tmp.IndexForPEGI;

                if (val == ind)
                    current = names.Count;
                names.Add(CompileSelectionName(ind, tmp, showIndex, stripSlashes, dotsToSlashes));
                indexes.Add(ind);

            }

            if (selectFinal(ref val, ref current, names))
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
            G val;
            return select_iGotIndex_SameClass(ref ind, lst, out val);
        }

        public static bool select_iGotIndex_SameClass<T, G>(this string label, ref int ind, List<T> lst, out G val) where G : class, T where T : IGotIndex
        {
            write(label);
            return select_iGotIndex_SameClass<T, G>(ref ind, lst, out val);
        }

        public static bool select_iGotIndex_SameClass<T, G>(ref int ind, List<T> lst, out G val) where G : class, T where T : IGotIndex
        {
            val = default(G);

            if (lst == null)
                return false;

            var names = new List<string>();
            var indexes = new List<int>();
            var els = new List<G>();
            var current = -1;

            foreach (var el in lst)
            {
                var g = el as G;

                if (g.filterEditorDropdown().IsNullOrDestroyed_Obj()) continue;

                var index = g.IndexForPEGI;

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

            if (selectFinal(ref ind, ref current, names))
            {
                ind = indexes[current];
                val = els[current];
                return true;
            }

            return false;
        }

        #endregion

        #region Select IGotName

        public static bool select_iGotDisplayName<T>(this string label, int width, ref string name, List<T> lst) where T : IGotDisplayName
        {
            write(label, width);
            return select_iGotDisplayName(ref name, lst);
        }

        public static bool select_iGotDisplayName<T>(this string label, string tip, ref string name, List<T> lst) where T : IGotDisplayName
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

            var namesList = new List<string>();

            var current = -1;

            foreach (var el in lst)
                if (!el.filterEditorDropdown().IsNullOrDestroyed_Obj())
                {
                    var name = el.NameForPEGI;

                    if (name == null) continue;

                    if (val != null && val.SameAs(name))
                        current = namesList.Count;
                    namesList.Add(name);

                }

            if (selectFinal(val, ref current, namesList))
            {
                val = namesList[current];
                return true;
            }

            return false;
        }

        public static bool select_iGotDisplayName<T>(ref string val, List<T> lst) where T : IGotDisplayName
        {

            if (lst == null)
                return false;

            var namesList = new List<string>();

            var current = -1;

            foreach (var el in lst)
                if (!el.filterEditorDropdown().IsNullOrDestroyed_Obj())
                {
                    var name = el.NameForDisplayPEGI();

                    if (name == null) continue;

                    if (val != null && val.SameAs(name))
                        current = namesList.Count;
                    namesList.Add(name);

                }

            if (selectFinal(val, ref current, namesList))
            {
                val = namesList[current];
                return true;
            }

            return false;
        }


        #endregion

        #endregion

        #region Click
        public const int defaultButtonSize = 26;

        public static bool ClickLink(this string label, string link, string tip = null)
        {

            if (tip == null)
                tip = "Go To: {0}".F(link);

            if (label.ClickText(tip, 12))
            {
                Application.OpenURL(link);
                return true;
            }

            return false;
        }

        public static bool ClickDuplicate(ref Material mat, string newName = null, string folder = "Materials") => ClickDuplicate(ref mat, folder, ".mat", newName);

        private static bool ClickDuplicate<T>(ref T obj, string folder, string extension, string newName = null) where T : Object
        {

            if (!obj) return false;

            var changed = false;

#if UNITY_EDITOR
            var path = AssetDatabase.GetAssetPath(obj);
            if (icon.Copy.ClickConfirm("dpl" + obj + "|" + path, "{0} Duplicate at {1}".F(obj, path)).changes(ref changed))
            {
                if (path.IsNullOrEmpty())
                {
                    obj = Object.Instantiate(obj);
                    if (!newName.IsNullOrEmpty())
                        obj.name = newName;

                    QcFile.Saving.Asset(obj, folder, extension, true);
                }
                else
                {
                    var newPath =
                        AssetDatabase.GenerateUniqueAssetPath(newName.IsNullOrEmpty()
                            ? path
                            : path.Replace(obj.name, newName));

                    AssetDatabase.CopyAsset(path, newPath);
                    obj = AssetDatabase.LoadAssetAtPath<T>(newPath);
                }
            }
#else
             if (icon.Copy.Click("Create Instance of {0}".F(obj)))
                obj = GameObject.Instantiate(obj);

#endif


            return changed;
        }

        public static void Lock_UnlockWindowClick(Object obj)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {

                if (ActiveEditorTracker.sharedTracker.isLocked == false &&
                    icon.Unlock.ClickUnFocus("Lock Inspector Window"))
                {
                    QcUnity.FocusOn(ef.serObj.targetObject);
                    ActiveEditorTracker.sharedTracker.isLocked = true;
                }

                if (ActiveEditorTracker.sharedTracker.isLocked && icon.Lock.ClickUnFocus("Unlock Inspector Window"))
                {
                    ActiveEditorTracker.sharedTracker.isLocked = false;
                    QcUnity.FocusOn(obj);
                }
            }
#endif
        }

        public static void UnlockInspectorWindowIfLocked(GameObject go)
        {
#if UNITY_EDITOR
            if (ActiveEditorTracker.sharedTracker.isLocked && (Selection.objects.IsNullOrEmpty() || Selection.objects.Contains(go)))
            {
                ActiveEditorTracker.sharedTracker.isLocked = false;
                QcUnity.FocusOn(go);
            }
#endif
        }

        private static string _confirmTag;
        private static object _objectToConfirm;
        private static string _confirmationDetails;

        private static void RequestConfirmation(string tag, object forObject = null, string details = "")
        {
            _confirmTag = tag;
            _objectToConfirm = forObject;
            _confirmationDetails = details;
        }

        private static void CloseConfirmation()
        {
            _confirmTag = null;
            _objectToConfirm = null;
        }

        public static bool IsConfirmingRequestedFor(string tag) => (!_confirmTag.IsNullOrEmpty() && _confirmTag.Equals(tag));

        public static bool IsConfirmingRequestedFor(string confirmationTag, object obj) =>
            confirmationTag.Equals(_confirmTag) && ((_objectToConfirm != null && _objectToConfirm.Equals(obj)) ||
                                                    (obj == null && _objectToConfirm == null));

        private static bool ConfirmClick()
        {

            nl();

            if (icon.Close.Click(Msg.No.GetText(), 30))
                CloseConfirmation();

            (_confirmationDetails.IsNullOrEmpty() ? Msg.AreYouSure.GetText() : _confirmationDetails).writeHint(false);

            if (icon.Done.Click(Msg.Yes.GetText(), 30))
            {
                CloseConfirmation();
                return true;
            }

            nl();


            return false;
        }

        public static bool ClickConfirm(this Texture tex, string confirmationTag)
        {

            if (confirmationTag.Equals(_confirmTag))
                return ConfirmClick();

            if (tex.ClickUnFocus())
                RequestConfirmation(confirmationTag);

            return false;
        }

        public static bool ClickConfirm(this string label, string confirmationTag, string tip = "")
        {

            if (confirmationTag.Equals(_confirmTag))
                return ConfirmClick();

            if (label.ClickUnFocus(tip))
                RequestConfirmation(confirmationTag, details: tip);

            return false;
        }

        public static bool ClickConfirm(this icon icon, string confirmationTag, string tip = "", int width = defaultButtonSize)
        {

            if (confirmationTag.Equals(_confirmTag))
                return ConfirmClick();

            if (icon.ClickUnFocus(tip, width))
                RequestConfirmation(confirmationTag, details: tip);

            return false;
        }

        public static bool ClickConfirm(this icon icon, string confirmationTag, object obj, string tip = "", int width = defaultButtonSize)
        {

            if (IsConfirmingRequestedFor(confirmationTag, obj))
                return ConfirmClick();

            if (icon.ClickUnFocus(tip, width))
                RequestConfirmation(confirmationTag, obj, tip);

            return false;
        }

        public static bool ClickUnFocus(this Texture tex, int width = defaultButtonSize)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.Click(tex, width).UnFocusIfTrue();
#endif

            checkLine();
            return GUILayout.Button(tex, GUILayout.MaxWidth(width + 5), GUILayout.MaxHeight(width)).DirtyUnFocus();
        }

        public static bool ClickUnFocus(this Texture tex, string tip, int width = defaultButtonSize) =>
             Click(tex, tip, width).UnFocusIfTrue();

        public static bool ClickUnFocus(this Texture tex, string tip, int width, int height) =>
                Click(tex, tip, width, height).UnFocusIfTrue();

        public static bool ClickUnFocus(this string text)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.Click(text).UnFocusIfTrue();
#endif
            checkLine();
            return GUILayout.Button(text, GuiMaxWidthOptionFrom(text)).DirtyUnFocus();
        }

        public static bool ClickUnFocus(this string text, string tip)
        {

            var cntnt = TextAndTip(text, tip);

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.Click(cntnt).UnFocusIfTrue();
#endif
            checkLine();
            return GUILayout.Button(cntnt, GuiMaxWidthOptionFrom(text)).DirtyUnFocus();
        }
        
        public static bool ClickText(this string label, int fontSize)
        {
            textAndTip.text = label;
            textAndTip.tooltip = label;
            return textAndTip.ClickText(ScalableBlueText(fontSize));
        }

        public static bool ClickText(this string label, string hint, int fontSize) => TextAndTip(label, hint).ClickText(ScalableBlueText(fontSize));

        public static bool ClickText(this string label, PegiGuiStyle style) => TextAndTip(label).ClickText(style);

        private static bool ClickText(this GUIContent content, PegiGuiStyle style)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.Click(content, style.Current);
#endif
            checkLine();
            return GUILayout.Button(content, style.Current , GuiMaxWidthOptionFrom(content, style: style)).Dirty();
        }

        public static bool ClickLabel(this string label, string hint = "ClickAble Text", int width = -1, PegiGuiStyle style = null)
        {
            SetBgColor(Color.clear);

            GUIStyle st = style == null ? ClickableText.Current : style.Current;

            textAndTip.text = label;
            textAndTip.tooltip = hint;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return (width == -1 ? ef.Click(textAndTip, st) : ef.Click(textAndTip, width, st)).UnFocusIfTrue()
                    .RestoreBGColor();
#endif

            checkLine();

            return (width == -1 ? GUILayout.Button(textAndTip, st, GuiMaxWidthOptionFrom(label)) : GUILayout.Button(textAndTip, st, GUILayout.MaxWidth(width))).DirtyUnFocus().PreviousBgColor();
        }
        
        private static bool ClickImage(this GUIContent content, int width, GUIStyle style) =>
            content.ClickImage(width, width, style);

        private static bool ClickImage(this GUIContent content, int width, int height, GUIStyle style)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.ClickImage(content, width, style);
#endif
            checkLine();

            return GUILayout.Button(content, GUILayout.MaxWidth(width + 5), GUILayout.MaxHeight(height)).Dirty();
        }

        public static bool Click(this string text, ref bool changed) => text.Click().changes(ref changed);

        public static bool Click(this string text)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.Click(text);
#endif
            checkLine();
            return GUILayout.Button(text, GuiMaxWidthOptionFrom(text)).Dirty();
        }

        public static bool Click(this string text, string tip, ref bool changed) => text.Click(tip).changes(ref changed);

        public static bool Click(this string text, string tip)
        {
            var cnt = TextAndTip(text, tip);
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.Click(cnt);
#endif
            checkLine();
            return GUILayout.Button(cnt, GuiMaxWidthOptionFrom(text)).Dirty();
        }

        private static Texture GetTexture_orEmpty(this Sprite sp) => sp ? sp.texture : icon.Empty.GetIcon();

        public static bool Click(this Sprite img, int size = defaultButtonSize)
            => img.GetTexture_orEmpty().Click(size);

        public static bool Click(this Sprite img, string tip, int size = defaultButtonSize)
            => img.GetTexture_orEmpty().Click(tip, size);

        public static bool Click(this Sprite img, string tip, int width, int height)
            => img.GetTexture_orEmpty().Click(tip, width, height);

        public static bool Click(this Texture img, int size = defaultButtonSize)
        {

            if (!img) img = icon.Empty.GetIcon();

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.Click(img, size);
#endif

            checkLine();
            return GUILayout.Button(img, GUILayout.MaxWidth(size + 5), GUILayout.MaxHeight(size)).Dirty();

        }

        public static bool Click(this Texture img, string tip, int size = defaultButtonSize)
        {

            if (!img) img = icon.Empty.GetIcon();

            var cnt = ImageAndTip(img, tip);

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.ClickImage(cnt, size);
#endif

            checkLine();
            return GUILayout.Button(cnt, GUILayout.MaxWidth(size + 5), GUILayout.MaxHeight(size)).Dirty();
        }

        public static bool Click(this Texture img, string tip, int width, int height)
        {
            if (!img) img = icon.Empty.GetIcon();

            var cnt = ImageAndTip(img, tip);

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.ClickImage(cnt, width, height);
#endif
            checkLine();
            return GUILayout.Button(cnt, GUILayout.MaxWidth(width), GUILayout.MaxHeight(height)).Dirty();
        }

        public static bool Click(this icon icon) => Click(icon.GetIcon(), icon.GetText());

        public static bool Click(this icon icon, ref bool changed) => Click(icon.GetIcon(), icon.GetText()).changes(ref changed);

        public static bool ClickUnFocus(this icon icon) => ClickUnFocus(icon.GetIcon(), icon.GetText());

        public static bool ClickUnFocus(this icon icon, ref bool changed) => ClickUnFocus(icon.GetIcon(), icon.GetText()).changes(ref changed);

        public static bool ClickUnFocus(this icon icon, int size = defaultButtonSize) => ClickUnFocus(icon.GetIcon(), icon.GetText(), size);

        public static bool ClickUnFocus(this icon icon, string tip, int size = defaultButtonSize)
        {
            if (tip == null)
                tip = icon.GetText();

            return ClickUnFocus(icon.GetIcon(), tip, size);
        }

        public static bool ClickUnFocus(this icon icon, string tip, int width, int height) => ClickUnFocus(icon.GetIcon(), tip, width, height);

        public static bool Click(this icon icon, int size) => Click(icon.GetIcon(), size);

        public static bool Click(this icon icon, string tip, int width, int height) => Click(icon.GetIcon(), tip, width, height);

        public static bool Click(this icon icon, string tip, ref bool changed, int size = defaultButtonSize) => Click(icon.GetIcon(), tip, size).changes(ref changed);

        public static bool Click(this icon icon, string tip, int size = defaultButtonSize) => Click(icon.GetIcon(), tip, size);

        public static bool Click(this Color col) => icon.Empty.GUIColor(col).BgColor(Color.clear).Click().RestoreGUIColor().RestoreBGColor();

        public static bool Click(this Color col, string tip, int size = defaultButtonSize) => icon.Empty.GUIColor(col).BgColor(Color.clear).Click(tip, size).RestoreGUIColor().RestoreBGColor();

        public static bool TryClickHighlight(object obj, int width = defaultButtonSize)
        {
#if UNITY_EDITOR
            var uo = obj as UnityEngine.Object;
            if (uo)
                uo.ClickHighlight(width);
#endif

            return false;
        }

        public static bool ClickHighlight(this Sprite sp, int width = defaultButtonSize)
        {
#if UNITY_EDITOR
            if (sp && sp.Click(Msg.HighlightElement.GetText(), width))
            {
                EditorGUIUtility.PingObject(sp);
                return true;
            }
#endif
            return false;
        }

        public static bool ClickHighlight(this Texture tex, int width = defaultButtonSize)
        {
#if UNITY_EDITOR
            if (tex && tex.Click(Msg.HighlightElement.GetText(), width))
            {
                EditorGUIUtility.PingObject(tex);
                return true;
            }
#endif

            return false;
        }

        public static bool ClickHighlight(this Object obj, int width = defaultButtonSize) =>
           obj.ClickHighlight(icon.Search.GetIcon(), width);

        public static bool ClickHighlight(this Object obj, Texture tex, int width = defaultButtonSize)
        {
#if UNITY_EDITOR
            if (obj && tex.Click(Msg.HighlightElement.GetText()))
            {
                EditorGUIUtility.PingObject(obj);
                return true;
            }
#endif

            return false;
        }

        public static bool ClickHighlight(this Object obj, icon icon, int width = defaultButtonSize)
        {
#if UNITY_EDITOR
            if (obj && icon.Click(Msg.HighlightElement.GetText()))
            {
                EditorGUIUtility.PingObject(obj);
                return true;
            }
#endif

            return false;
        }

        public static bool ClickHighlight(this Object obj, string hint, icon icon = icon.Search, int width = defaultButtonSize)
        {
#if UNITY_EDITOR
            if (obj && icon.Click(hint))
            {
                EditorGUIUtility.PingObject(obj);
                return true;
            }
#endif

            return false;
        }

        public static bool Click_Enter_Attention_Highlight<T>(this T obj, ref bool changed, icon icon = icon.Enter, string hint = "", bool canBeNull = true) where T : UnityEngine.Object, INeedAttention
        {
            var ch = obj.Click_Enter_Attention(icon, hint, canBeNull).changes(ref changed);
            obj.ClickHighlight().changes(ref changed);
            return ch;
        }

        public static bool Click_Enter_Attention(this INeedAttention attention, icon icon = icon.Enter, string hint = "", bool canBeNull = true)
        {
            if (attention.IsNullOrDestroyed_Obj())
            {
                if (!canBeNull)
                    return icon.Warning.ClickUnFocus("Object is null; {0}".F(hint));
            }
            else
            {

                var msg = attention.NeedAttention();
                if (msg != null)
                    return icon.Warning.ClickUnFocus(msg);
            }

            if (hint.IsNullOrEmpty())
                hint = icon.GetText();

            return icon.ClickUnFocus(hint);
        }

        public static bool Click_Enter_Attention(this INeedAttention attention, Texture tex, string hint = "", bool canBeNull = true)
        {
            if (attention.IsNullOrDestroyed_Obj())
            {
                if (!canBeNull)
                    return icon.Warning.ClickUnFocus("Object is null; {0}".F(hint));
            }
            else
            {

                var msg = attention.NeedAttention();
                if (msg != null)
                    return icon.Warning.ClickUnFocus(msg);
            }

            if (hint.IsNullOrEmpty())
                hint = tex ? tex.ToString() : "Null Texture";

            return tex ? tex.ClickUnFocus(hint) : icon.Enter.ClickUnFocus(hint);
        }

        #endregion

        #region Toggle
        private const int DefaultToggleIconSize = 34;

        public static bool toggleInt(ref int val)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.toggleInt(ref val);
#endif

            var before = val > 0;
            if (!toggle(ref before)) return false;
            val = before ? 1 : 0;
            return true;
        }

        public static bool toggle(this icon icon, ref int selected, int current)
          => icon.toggle(icon.GetText(), ref selected, current);

        public static bool toggle(this icon icon, string label, ref int selected, int current)
        {
            if (selected == current)
                icon.write(label);
            else if (icon.Click(label))
            {
                selected = current;
                return true;
            }

            return false;
        }

        public static bool toggle(ref bool val)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.toggle(ref val);
#endif

            bc();
            val = GUILayout.Toggle(val, "", GUILayout.MaxWidth(30));
            return ec();

        }

        public static bool toggle(ref bool val, string text, int width)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                ef.write(text, width);
                return ef.toggle(ref val);
            }
#endif

            bc();
            val = GUILayout.Toggle(val, text, GuiMaxWidthOption);
            return ec();

        }

        public static bool toggle(ref bool val, string text, string tip)
        {
            var cnt = TextAndTip(text, tip);
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.toggle(ref val, cnt);

#endif
            bc();
            val = GUILayout.Toggle(val, cnt, GuiMaxWidthOption);
            return ec();
        }

        private static bool toggle(ref bool val, icon TrueIcon, icon FalseIcon, string tip, int width, PegiGuiStyle style)
            => toggle(ref val, TrueIcon.GetIcon(), FalseIcon.GetIcon(), tip, width, style.Current);

        public static bool toggle(ref bool val, icon TrueIcon, icon FalseIcon, string tip, int width = defaultButtonSize)
            => toggle(ref val, TrueIcon.GetIcon(), FalseIcon.GetIcon(), tip, width);

        public static bool toggle(ref bool val, icon TrueIcon, icon FalseIcon, GUIStyle style = null) => toggle(ref val, TrueIcon.GetIcon(), FalseIcon.GetIcon(), "", defaultButtonSize, style);

        public static bool toggleVisibilityIcon(ref bool val, string hint, int width = DefaultToggleIconSize)
        {
            SetBgColor(Color.clear);

            var changed = toggle(ref val, icon.Show, icon.Hide, hint, width, PEGI_Styles.ToggleButton).PreviousBgColor();

            return changed;
        }


        public static bool toggleVisibilityIcon(this string label, string hint, ref bool val, bool dontHideTextWhenOn = false)
        {
            SetBgColor(Color.clear);

            var changed = toggle(ref val, icon.Show, icon.Hide, hint, DefaultToggleIconSize, PEGI_Styles.ToggleButton).PreviousBgColor();

            if (!val || dontHideTextWhenOn) label.write(hint, PEGI_Styles.ToggleLabel(val));

            return changed;
        }

        public static bool toggleVisibilityIcon(this string label, ref bool val, bool showTextWhenTrue = false)
        {
            var changed = toggle(ref val, icon.Show.BgColor(Color.clear), icon.Hide, label, DefaultToggleIconSize, ToggleButton).PreviousBgColor();

            if (!val || showTextWhenTrue) label.write(ToggleLabel(val));

            return changed;
        }

        public static bool toggleIcon(ref bool val, string hint = "Toggle On/Off") => toggle(ref val, icon.True.BgColor(Color.clear), icon.False, hint, DefaultToggleIconSize, ToggleButton).PreviousBgColor();

        public static bool toggleIcon(ref int val, string hint = "Toggle On/Off")
        {
            var boo = val != 0;

            if (toggle(ref boo, icon.True.BgColor(Color.clear), icon.False, hint, DefaultToggleIconSize, PEGI_Styles.ToggleButton).PreviousBgColor())
            {
                val = boo ? 1 : 0;
                return true;
            }

            return false;
        }

        public static bool toggleIcon(this string label, string hint, ref bool val, bool hideTextWhenTrue = false)
        {
            SetBgColor(Color.clear);

            var ret = toggle(ref val, icon.True, icon.False, hint, DefaultToggleIconSize, PEGI_Styles.ToggleButton).PreviousBgColor();

            if ((!val || !hideTextWhenTrue) && label.ClickLabel(hint, -1, PEGI_Styles.ToggleLabel(val)).changes(ref ret))
                val = !val;

            return ret;
        }

        public static bool toggleIcon(this string label, ref bool val, bool hideTextWhenTrue = false)
        {
            var changed = toggle(ref val, icon.True.BgColor(Color.clear), icon.False, label, DefaultToggleIconSize, ToggleButton).PreviousBgColor();

            if ((!val || !hideTextWhenTrue) && label.ClickLabel(label, -1, ToggleLabel(val)).changes(ref changed))
                val = !val;

            return changed;
        }

        public static bool toggleIcon(this string labelIfFalse, ref bool val, string labelIfTrue)
            => (val ? labelIfTrue : labelIfFalse).toggleIcon(ref val);

        public static bool toggle(ref bool val, Texture2D TrueIcon, Texture2D FalseIcon, string tip, int width = defaultButtonSize, GUIStyle style = null)
        {

            if (val)
            {
                if (ClickImage(ImageAndTip(TrueIcon, tip), width, style))
                {
                    val = false;
                    return true;
                }

            }
            else if (ClickImage(ImageAndTip(FalseIcon, tip), width, style))
            {
                val = true;
                return true;
            }

            return false;
        }

        public static bool toggle(ref bool val, string text, string tip, int width)
        {
            var cnt = TextAndTip(text, tip);
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                ef.write(cnt, width);
                return ef.toggle(ref val);
            }

#endif

            bc();
            val = GUILayout.Toggle(val, cnt, GuiMaxWidthOption);
            return ec();

        }

        public static bool toggle(int ind, CountlessBool tb)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.toggle(ind, tb);
#endif
            var has = tb[ind];

            if (!toggle(ref has)) return false;

            tb.Toggle(ind);
            return true;
        }

        public static bool toggle(this icon img, ref bool val)
        {
            write(img.GetIcon(), 25);
            return toggle(ref val);
        }

        public static bool toggle(this Texture img, ref bool val)
        {
            write(img, 25);
            return toggle(ref val);
        }

        public static bool toggleInt(this string text, ref int val)
        {
            write(text);
            return toggleInt(ref val);
        }

        public static bool toggleInt(this string text, string hint, ref int val)
        {
            write(text, hint);
            return toggleInt(ref val);
        }

        public static bool toggle(this string text, ref bool val)
        {
            write(text);
            return toggle(ref val);
        }

        public static bool toggle(this string text, int width, ref bool val)
        {
            write(text, width);
            return toggle(ref val);
        }

        public static bool toggle(this string text, string tip, ref bool val)
        {
            write(text, tip);
            return toggle(ref val);
        }

        public static bool toggle(this string text, string tip, int width, ref bool val)
        {
            write(text, tip, width);
            return toggle(ref val);
        }

        public static bool toggle_CompileDirective(string text, string keyword)
        {
            var changed = false;

#if UNITY_EDITOR
            var val = QcUnity.GetPlatformDirective(keyword);

            if (text.toggleIcon(keyword, ref val))
                QcUnity.SetPlatformDirective(keyword, val);
#endif

            return changed;
        }

        public static bool toggleDefaultInspector(Object target)
        {
#if UNITY_EDITOR

            if (!PaintingGameViewUI)
                return ef.toggleDefaultInspector(target);
#endif

            return false;
        }

        #endregion

        #region Edit

        #region Audio Clip

        public static bool edit(this string label, int width, ref AudioClip field, float offset = 0)
        {
            label.write(width);
            return edit(ref field, offset);
        }

        public static bool edit(this string label, ref AudioClip field, float offset = 0)
        {
            label.write(ApproximateLength(label));
            return edit(ref field, offset);
        }

        public static bool edit(ref AudioClip clip, int width, float offset = 0)
        {

            var ret =
#if UNITY_EDITOR
                !PaintingGameViewUI ? ef.edit(ref clip, width) :
#endif
                    false;

            clip.PlayButton(offset);

            return ret;
        }

        public static bool edit(ref AudioClip clip, float offset = 0)
        {

            var ret =
#if UNITY_EDITOR
                !PaintingGameViewUI ? ef.edit(ref clip) :
#endif
                    false;

            clip.PlayButton(offset);

            return ret;
        }

        private static void PlayButton(this AudioClip clip, float offset = 0)
        {
            if (clip && icon.Play.Click(20))
            {
                var req = clip.Play();
                if (offset > 0)
                    req.FromTimeOffset(offset);
            }
        }

        #endregion

        #region UnityObject

        public static bool edit_ifNull<T>(this GameObject parent, ref T component) where T : Component
        {
            if (component)
                return false;

            var changed = false;

            typeof(T).ToString().SimplifyTypeName().write();
            if (icon.Refresh.Click("Get Component()").changes(ref changed))
                component = parent.GetComponent<T>();
            if (icon.Add.Click("Add Component").changes(ref changed))
                component = parent.AddComponent<T>();

            return changed;
        }

        public static bool edit(ref SortingLayer sortingLayer) => select(ref sortingLayer);

        public static bool edit(this string label, ref SortingLayer sortingLayer)
        {
            label.write();
            return select(ref sortingLayer, SortingLayer.layers);
        }

        public static bool edit(this string label, int width, ref SortingLayer sortingLayer)
        {
            label.write(width);
            return select(ref sortingLayer);
        }

        public static bool edit<T>(ref T field, int width) where T : Object =>
#if UNITY_EDITOR
                !PaintingGameViewUI ? ef.edit(ref field, width) :
#endif
            false;

        public static bool edit<T>(ref T field, bool allowDrop) where T : Object =>
#if UNITY_EDITOR
            !PaintingGameViewUI ? ef.edit(ref field, allowDrop) :
#endif
                false;

        public static bool edit<T>(this string label, ref T field) where T : Object
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                label.write(ApproximateLength(label));
                return edit(ref field);
            }
#endif

            return false;

        }

        public static bool edit<T>(this string label, ref T field, bool allowDrop) where T : Object
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                write(label);
                return edit(ref field, allowDrop);
            }
#endif

            return false;

        }

        public static bool edit<T>(this string label, int width, ref T field) where T : Object
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                write(label, width);
                return edit(ref field);
            }
#endif

            return false;

        }

        public static bool edit<T>(this string label, int width, ref T field, bool allowDrop) where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                write(label, width);
                return edit(ref field, allowDrop);
            }

#endif

            return false;

        }

        public static bool edit<T>(this string label, string tip, int width, ref T field) where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                write(label, tip, width);
                return edit(ref field);
            }

#endif

            return false;

        }

        public static bool edit<T>(this string label, string tip, int width, ref T field, bool allowDrop) where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                write(label, tip, width);
                return edit(ref field, allowDrop);
            }

#endif

            return false;

        }

        public static bool edit<T>(ref T field) where T : Object =>
#if UNITY_EDITOR
            !PaintingGameViewUI ? ef.edit(ref field) :
#endif
                false;

        public static bool edit_enter_Inspect<T>(ref T obj, ref int entered, int current, List<T> selectFrom = null) where T : Object
            => edit_enter_Inspect(null, -1, ref obj, ref entered, current, selectFrom);

        public static bool edit_enter_Inspect<T>(this string label, ref T obj, ref int entered, int current, List<T> selectFrom = null) where T : Object
            => label.edit_enter_Inspect(-1, ref obj, ref entered, current, selectFrom);

        public static bool edit_enter_Inspect<T>(this string label, int width, ref T obj, ref int entered, int current,
            List<T> selectFrom = null) where T : Object
        {
            var changed = false;

            var lst = obj as IPEGI_ListInspect;

            if (lst != null)
                lst.enter_Inspect_AsList(ref entered, current, label).changes(ref changed);
            else
            {
                var pgi = QcUnity.TryGet_fromObj<IPEGI>(obj);

                if (icon.Enter.conditional_enter(pgi != null, ref entered, current, label))
                    pgi.Nested_Inspect().changes(ref changed);
            }

            if (entered == -1)
            {

                if (selectFrom == null)
                {

                    string lab = label;

                    if (lab.IsNullOrEmpty())
                    {

                        if (obj)
                            lab = obj.GetNameForInspector();
                        else
                            lab = typeof(T).ToPegiStringType();
                    }

                    lab = lab.TryAddCount(obj);

                    if (width > 0)
                    {

                        if (lab.ClickLabel(Msg.ClickToInspect.GetText(), width, PEGI_Styles.EnterLabel))
                            entered = current;
                    }
                    else
                    if (lab.ClickLabel(Msg.ClickToInspect.GetText(), style: PEGI_Styles.EnterLabel))
                        entered = current;

                    if (!obj)
                        edit(ref obj).changes(ref changed);
                }
                else
                    label.select_or_edit(width, ref obj, selectFrom).changes(ref changed);

                obj.ClickHighlight();

                if (obj && icon.Delete.Click(Msg.MakeElementNull.GetText()))
                    obj = null;
            }



            return changed;
        }

        #endregion

        #region Vectors

        public static bool edit(this string label, ref Quaternion qt)
        {
            var eul = qt.eulerAngles;

            if (edit(label, ref eul))
            {
                qt.eulerAngles = eul;
                return true;
            }

            return false;
        }


        public static bool edit(this string label, int width, ref Quaternion qt)
        {
            write(label, width);
            return edit(ref qt);
        }

        public static bool edit(ref Quaternion qt)
        {
            var eul = qt.eulerAngles;

            if (edit(ref eul))
            {
                qt.eulerAngles = eul;
                return true;
            }

            return false;
        }


        public static bool edit(ref Vector4 val)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val);
#endif

            return "X".edit(ref val.x) | "Y".edit(ref val.y) | "Z".edit(ref val.z) | "W".edit(ref val.w);

        }

        public static bool edit01(this string label, int width, ref Rect val)
        {
            label.nl(width);
            return edit01(ref val);
        }

        public static bool edit01(ref float val) => edit(ref val, 0, 1);

        public static bool edit01(this string label, ref float val) => label.edit(label.ApproximateLength(), ref val, 0, 1);

        public static bool edit01(this string label, int width, ref float val) => label.edit(width, ref val, 0, 1);

        public static bool edit01(ref Rect val)
        {
            var center = val.center;
            var size = val.size;

            if (
                "X".edit01(30, ref center.x).nl() ||
                "Y".edit01(30, ref center.y).nl() ||
                "W".edit01(30, ref size.x).nl() ||
                "H".edit01(30, ref size.y).nl())
            {
                var half = size * 0.5f;
                val.min = center - half;
                val.max = center + half;
                return true;
            }

            return false;
        }

        public static bool edit(this string label, ref Rect val)
        {
            var v4 = val.ToVector4(true);

            if (label.edit(ref v4))
            {
                val = v4.ToRect(true);
                return true;
            }

            return false;
        }

        public static bool edit(this string label, ref Vector4 val)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(label, ref val);
#endif

            write(label);
            return
                edit(ref val.x) |
                edit(ref val.y) |
                edit(ref val.z) |
                edit(ref val.w);

        }

        public static bool edit(ref Vector3 val) =>
           "X".edit(15, ref val.x) || "Y".edit(15, ref val.y) || "Z".edit(15, ref val.z);

        public static bool edit(ref Vector3 val, float min, float max) =>
            "X".edit(10, ref val.x, min, max) ||
            "Y".edit(10, ref val.y, min, max) ||
            "Z".edit(10, ref val.z, min, max);


        public static bool edit(this string label, ref Vector3 val)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(label, ref val);
#endif

            write(label);
            nl();
            return edit(ref val);
        }

        public static bool edit(ref Vector2 val)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val);

#endif

            return edit(ref val.x) || edit(ref val.y);
        }

        public static bool edit01(this string label, int width, ref Vector2 val)
        {
            label.nl(width);
            return edit01(ref val);
        }

        public static bool edit01(this string label, ref Vector2 val)
        {
            label.nl(label.ApproximateLength());
            return edit01(ref val);
        }

        public static bool edit01(ref Vector2 val) =>
            "X".edit01(10, ref val.x).nl() ||
            "Y".edit01(10, ref val.y).nl();

        public static bool edit_Range(this string label, int width, ref Vector2 vec2)
        {

            var x = vec2.x;
            var y = vec2.y;

            if (label.edit_Range(width, ref x, ref y))
            {
                vec2.x = x;
                vec2.y = y;
                return true;
            }

            return false;
        }

        public static bool edit(this string label, ref Vector2 val, float min, float max)
        {
            "{0} [X: {1} Y: {2}]".F(label, val.x.RoundTo(2), val.y.RoundTo(2)).nl();
            return edit(ref val, min, max);
        }

        public static bool edit(ref Vector2 val, float min, float max) =>
            "X".edit(10, ref val.x, min, max) ||
            "Y".edit(10, ref val.y, min, max);

        public static bool edit(this string label, ref Vector2 val)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(label, ref val);
#endif

            write(label);
            return edit(ref val.x) || edit(ref val.y);

        }

        public static bool edit(this string label, string tip, int width, ref Vector2 v2)
        {
            write(label, tip, width);
            return edit(ref v2);
        }

        public static bool edit(this string label, int width, ref Vector3 v3)
        {
            write(label, width);
            return edit(ref v3);
        }

        public static bool edit(this string label, string tip, int width, ref Vector3 v3)
        {
            write(label, tip, width);
            return edit(ref v3);
        }
        #endregion

        #region Color

        public static bool edit(ref Color32 col)
        {
            Color tcol = col;
            if (edit(ref tcol))
            {
                col = tcol;
                return true;
            }
            return false;
        }

        public static bool edit(ref Color col)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref col);

#endif
            nl();
            return icon.Red.edit_ColorChannel(ref col, 0).nl() ||
                   icon.Green.edit_ColorChannel(ref col, 1).nl() ||
                   icon.Blue.edit_ColorChannel(ref col, 2).nl() ||
                   icon.Alpha.edit_ColorChannel(ref col, 3).nl();

        }

        public static bool edit(ref Color col, int width)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref col, width);

#endif
            return false;
        }

        public static bool edit_ColorChannel(this icon ico, ref Color col, int channel)
        {
            var changed = false;

            if (channel < 0 || channel > 3)
                "Color has no channel {0} ".F(channel).writeWarning();
            else
            {
                var chan = col[channel];

                if (ico.edit(ref chan, 0, 1).changes(ref changed))
                    col[channel] = chan;

            }

            return changed;
        }

        public static bool edit_ColorChannel(this string label, ref Color col, int channel)
        {
            var changed = false;

            if (channel < 0 || channel > 3)
                "{0} color does not have {1}'th channel".F(label, channel).writeWarning();
            else
            {
                var chan = col[channel];

                if (label.edit(ref chan, 0, 1).changes(ref changed))
                    col[channel] = chan;

            }

            return changed;
        }

        public static bool edit(this string label, ref Color col)
        {
            if (PaintingGameViewUI)
            {
                if (label.foldout())
                    return edit(ref col);
            }
            else
            {
                write(label);
                return edit(ref col);
            }

            return false;
        }

        public static bool edit(this string label, int width, ref Color col)
        {
            if (PaintingGameViewUI)
            {
                if (label.foldout())
                    return edit(ref col);

            }
            else
            {
                write(label, width);
                return edit(ref col);
            }

            return false;
        }

        public static bool edit(this string label, string tip, int width, ref Color col)
        {
            if (PaintingGameViewUI)
                return false;

            write(label, tip, width);
            return edit(ref col);
        }

        #endregion

        #region Material

        public static bool editTexture(this Material mat, string name) => mat.editTexture(name, name);

        public static bool editTexture(this Material mat, string name, string display)
        {

            display.write(display.ApproximateLength());
            var tex = mat.GetTexture(name);

            if (edit(ref tex))
            {
                mat.SetTexture(name, tex);
                return true;
            }

            return false;
        }

        public static bool toggle(this Material mat, string keyword)
        {
            var val = Array.IndexOf(mat.shaderKeywords, keyword) != -1;

            if (!keyword.toggleIcon(ref val)) return false;

            if (val)
                mat.EnableKeyword(keyword);
            else
                mat.DisableKeyword(keyword);

            return true;
        }

        public static bool edit(this Material mat, ShaderProperty.FloatValue property, string name = null)
        {
            var val = mat.Get(property);

            if (name.IsNullOrEmpty())
                name = property.NameForDisplayPEGI();

            if (name.edit(name.Length * letterSizeInPixels, ref val))
            {
                mat.Set(property, val);
                return true;
            }

            return false;
        }

        public static bool edit(this Material mat, ShaderProperty.FloatValue property, string name, float min, float max)
        {
            var val = mat.Get(property);

            if (name.IsNullOrEmpty())
                name = property.NameForDisplayPEGI();

            if (name.edit(name.Length * letterSizeInPixels, ref val, min, max))
            {
                mat.Set(property, val);
                return true;
            }

            return false;
        }

        public static bool edit(this Material mat, ShaderProperty.ColorFloat4Value property, string name = null)
        {
            var val = mat.Get(property);

            if (name.IsNullOrEmpty())
                name = property.NameForDisplayPEGI();

            if (name.edit(name.Length * letterSizeInPixels, ref val))
            {
                mat.Set(property, val);
                return true;
            }

            return false;
        }

        public static bool edit(this Material mat, ShaderProperty.VectorValue property, string name = null)
        {
            var val = mat.Get(property);

            if (name.IsNullOrEmpty())
                name = property.NameForDisplayPEGI();

            if (name.edit(ref val))
            {
                mat.Set(property, val);
                return true;
            }

            return false;
        }

        public static bool edit(this Material mat, ShaderProperty.TextureValue property, string name = null)
        {
            var val = mat.Get(property);

            if (name.IsNullOrEmpty())
                name = property.NameForDisplayPEGI();

            if (name.edit(name.Length * letterSizeInPixels, ref val))
            {
                mat.Set(property, val);
                return true;
            }

            return false;
        }

        #endregion

        #region Animation Curve
        public static bool edit(this string name, ref AnimationCurve val) =>
#if UNITY_EDITOR
            !PaintingGameViewUI ? ef.edit(name, ref val) :
#endif
                false;

        #endregion

        #region UInt

        public static bool edit(ref uint val)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val);
#endif
            bc();
            var newval = GUILayout.TextField(val.ToString(), GuiMaxWidthOption);
            if (!ec()) return false;

            int newValue;
            if (int.TryParse(newval, out newValue))
                val = (uint)newValue;

            return true;


        }

        public static bool edit(ref uint val, int width)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val, width);
#endif

            bc();
            var strVal = GUILayout.TextField(val.ToString(), GUILayout.MaxWidth(width));
            if (!ec()) return false;

            int newValue;
            if (int.TryParse(strVal, out newValue))
                val = (uint)newValue;

            return true;

        }

        public static bool edit(ref uint val, uint min, uint max)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val, min, max);
#endif

            bc();
            val = (uint)GUILayout.HorizontalSlider(val, min, max, GuiMaxWidthOption);
            return ec();

        }

        public static bool edit(this string label, ref uint val)
        {
            write(label);
            return edit(ref val);
        }

        public static bool edit(this string label, ref uint val, uint min, uint max)
        {
            label.sliderText(val, label, 90);
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, int width, ref uint val, uint min, uint max)
        {
            label.sliderText(val, label, width);
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, string tip, int width, ref uint val)
        {
            write(label, tip, width);
            return edit(ref val);
        }

        public static bool edit(this string label, string tip, int width, ref uint val, uint min, uint max)
        {
            label.sliderText(val, tip, width);
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, int width, ref uint val)
        {
            write(label, width);
            return edit(ref val);
        }

        #endregion

        #region Int

        public static bool editLayerMask(this string label, string tip, int width, ref string tag)
        {
            label.write(tip, width);
            return editTag(ref tag);
        }

        public static bool editLayerMask(this string label, int width, ref string tag)
        {
            label.write(width);
            return editTag(ref tag);
        }

        public static bool editLayerMask(this string label, ref string tag)
        {
            label.write(label.ApproximateLength());
            return editTag(ref tag);
        }

        public static bool editTag(ref string tag)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.editTag(ref tag);
#endif

            return false;
        }



        public static bool editLayerMask(this string label, ref int val)
        {
            label.write(label.ApproximateLength());
            return editLayerMask(ref val);
        }

        public static bool editLayerMask(ref int val)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.editLayerMask(ref val);
#endif

            return false;
        }

        public static bool edit(ref int val)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val);
#endif

            bc();
            var intText = GUILayout.TextField(val.ToString(), GuiMaxWidthOption);
            if (!ec()) return false;

            int newValue;

            if (int.TryParse(intText, out newValue))
                val = newValue;

            return true;
        }

        public static bool edit(ref int val, int width)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val, width);
#endif

            bc();

            var newValText = GUILayout.TextField(val.ToString(), GUILayout.MaxWidth(width));

            if (!ec()) return false;

            int newValue;
            if (int.TryParse(newValText, out newValue))
                val = newValue;

            return change;

        }

        public static bool edit(ref int val, int min, int max)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val, min, max);
#endif

            bc();
            val = (int)GUILayout.HorizontalSlider(val, min, max, GuiMaxWidthOption);
            return ec();

        }

        private static int editedInteger;
        private static int editedIntegerIndex;
        public static bool editDelayed(ref int val, int width = -1)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.editDelayed(ref val, width);

#endif

            checkLine();

            var tmp = (editedIntegerIndex == _elementIndex) ? editedInteger : val;

            if (KeyCode.Return.IsDown() && (_elementIndex == editedIntegerIndex))
            {
                edit(ref tmp);
                val = editedInteger;
                editedIntegerIndex = -1;

                _elementIndex++;

                return change;
            }

            if (edit(ref tmp).ignoreChanges())
            {
                editedInteger = tmp;
                editedIntegerIndex = _elementIndex;
            }

            _elementIndex++;

            return false;

        }

        public static bool editDelayed(this string label, ref int val, int width)
        {
            write(label, Msg.EditDelayed_HitEnter.GetText());
            return editDelayed(ref val, width);
        }

        public static bool editDelayed(this string label, ref int val)
        {
            write(label, Msg.EditDelayed_HitEnter.GetText());
            return editDelayed(ref val);
        }

        public static bool edit(this string label, ref int val)
        {
            write(label);
            return edit(ref val);
        }

        public static bool edit(this string label, ref int val, int min, int max)
        {
            label.sliderText(val, label, 90);
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, int width, ref int val, int min, int max)
        {
            label.sliderText(val, label, width);
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, string tip, int width, ref int val)
        {
            write(label, tip, width);
            return edit(ref val);
        }

        public static bool edit(this string label, string tip, int width, ref int val, int min, int max)
        {
            label.sliderText(val, tip, width);
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, int width, ref int val)
        {
            write(label, width);
            return edit(ref val);
        }

        #endregion

        #region Long

        public static bool edit(ref long val)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val);
#endif

            bc();
            var intText = GUILayout.TextField(val.ToString(), GuiMaxWidthOption);
            if (!ec()) return false;

            long newValue;

            if (long.TryParse(intText, out newValue))
                val = newValue;

            return true;
        }

        public static bool edit(ref long val, int width)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val, width);
#endif

            bc();

            var newValText = GUILayout.TextField(val.ToString(), GUILayout.MaxWidth(width));

            if (!ec()) return false;

            long newValue;
            if (long.TryParse(newValText, out newValue))
                val = newValue;

            return change;

        }

        public static bool edit(this string label, ref long val)
        {
            write(label);
            return edit(ref val);
        }

        public static bool edit(this string label, int width, ref long val)
        {
            write(label, width);
            return edit(ref val);
        }


        #endregion

        #region Float

        public static bool edit(ref float val)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val);
#endif

            bc();
            var newval = GUILayout.TextField(val.ToString(), GuiMaxWidthOption);

            if (!ec()) return false;

            float newValue;
            if (float.TryParse(newval, out newValue))
                val = newValue;

            return change;
        }

        public static bool edit(ref float val, int width)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val, width);
#endif

            bc();

            var newval = GUILayout.TextField(val.ToString(), GUILayout.MaxWidth(width));

            if (!ec()) return false;

            float newValue;
            if (float.TryParse(newval, out newValue))
                val = newValue;

            return change;

        }

        public static bool edit(this string label, ref float val)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(label, ref val);
#endif
            write(label);
            return edit(ref val);
        }

        public static bool editPOW(ref float val, float min, float max)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.editPOW(ref val, min, max);
#endif

            bc();
            var after = GUILayout.HorizontalSlider(Mathf.Sqrt(val), min, max, GuiMaxWidthOption);
            if (!ec()) return false;
            val = after * after;
            return change;


        }

        public static bool edit(ref float val, float min, float max)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val, min, max);

#endif

            bc();
            val = GUILayout.HorizontalSlider(val, min, max, GuiMaxWidthOption);
            return ec();

        }

        public static bool editDelayed(this string label, string tip, int width, ref float val)
        {
            write(label, tip, width);
            return editDelayed(ref val);
        }

        public static bool editDelayed(this string label, int width, ref float val)
        {
            write(label, width);
            return editDelayed(ref val);
        }

        public static bool editDelayed(this string label, ref float val)
        {
            write(label);
            return editDelayed(ref val);
        }

        public static bool editDelayed(ref float val, int width)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.editDelayed(ref val, width);
#endif

            return editDelayed(ref val);
        }

        public static bool editDelayed(ref float val)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.editDelayed(ref val);
#endif


            checkLine();

            var tmp = (editedFloatIndex == _elementIndex) ? editedFloat : val.ToString();

            if (KeyCode.Return.IsDown() && (_elementIndex == editedFloatIndex))
            {
                edit(ref tmp);

                float newValue;
                if (float.TryParse(editedFloat, out newValue))
                    val = newValue;
                _elementIndex++;

                editedFloatIndex = -1;

                return change;
            }

            if (edit(ref tmp).ignoreChanges())
            {
                editedFloat = tmp;
                editedFloatIndex = _elementIndex;
            }

            _elementIndex++;

            return false;
        }

        private static string editedFloat;
        private static int editedFloatIndex;

        public static bool edit(this string label, int width, ref float val)
        {
            write(label, width);
            return edit(ref val);
        }

        public static bool edit_Range(this string label, ref float from, ref float to) => label.edit_Range(label.ApproximateLength(), ref from, ref to);

        public static bool edit_Range(this string label, int width, ref float from, ref float to)
        {
            write(label, width);
            var changed = false;
            if (edit(ref from).changes(ref changed))
                to = Mathf.Max(from, to);

            write("-", 10);

            if (edit(ref to).changes(ref changed))
                from = Mathf.Min(from, to);


            return changed;
        }

        private static void sliderText(this string label, float val, string tip, int width)
        {
            if (PaintingGameViewUI)
                "{0} [{1}]".F(label, val.ToString("F3")).write(width);
            else
                write(label, tip, width);
        }

        public static bool edit(this string label, ref float val, float min, float max)
        {
            label.sliderText(val, label, label.Length * letterSizeInPixels);
            return edit(ref val, min, max);
        }

        public static bool edit(this icon ico, ref float val, float min, float max)
        {
            ico.write();
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, int width, ref float val, float min, float max)
        {
            label.sliderText(val, label, width);
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, string tip, int width, ref float val, float min, float max)
        {
            label.sliderText(val, tip, width);
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, string tip, int width, ref float val)
        {
            write(label, tip, width);
            return edit(ref val);
        }

        public static bool edit(this string label, string tip, ref float val)
        {
            write(label, tip);
            return edit(ref val);
        }

        #endregion

        #region Double

        public static bool editDelayed(this string label, string tip, int width, ref double val)
        {
            write(label, tip, width);
            return editDelayed(ref val);
        }

        public static bool editDelayed(this string label, int width, ref double val)
        {
            write(label, width);
            return editDelayed(ref val);
        }

        public static bool editDelayed(this string label, ref double val)
        {
            write(label);
            return editDelayed(ref val);
        }

        public static bool editDelayed(ref double val, int width)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.editDelayed(ref val, width);
#endif

            return editDelayed(ref val);
        }

        public static bool editDelayed(ref double val)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.editDelayed(ref val);
#endif


            checkLine();

            var tmp = (editedDoubleIndex == _elementIndex) ? editedDouble : val.ToString();

            if (KeyCode.Return.IsDown() && (_elementIndex == editedDoubleIndex))
            {
                edit(ref tmp);

                double newValue;
                if (double.TryParse(editedFloat, out newValue))
                    val = newValue;
                _elementIndex++;

                editedDoubleIndex = -1;

                return change;
            }

            if (edit(ref tmp).ignoreChanges())
            {
                editedDouble = tmp;
                editedDoubleIndex = _elementIndex;
            }

            _elementIndex++;

            return false;
        }

        private static string editedDouble;
        private static int editedDoubleIndex;


        public static bool edit(ref double val)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val);
#endif
            bc();
            var newval = GUILayout.TextField(val.ToString(), GuiMaxWidthOption);
            if (!ec()) return false;
            double newValue;
            if (!double.TryParse(newval, out newValue)) return false;
            val = newValue;
            return change;
        }

        public static bool edit(this string label, ref double val)
        {
            label.write();
            return edit(ref val);
        }

        public static bool edit(this string label, int width, ref double val)
        {
            label.write(width);
            return edit(ref val);
        }

        public static bool edit(this string label, string tip, int width, ref double val)
        {
            label.write(tip, width);
            return edit(ref val);
        }

        public static bool edit(ref double val, int width)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val, width);
#endif

            bc();
            var newval = GUILayout.TextField(val.ToString(), GUILayout.MaxWidth(width));
            if (!ec()) return false;

            double newValue;
            if (double.TryParse(newval, out newValue))
                val = newValue;

            return change;

        }

        #endregion

        #region Enum

        public static bool editEnum<T>(this string text, string tip, int width, ref T eval)
        {
            write(text, tip, width);
            return editEnum(ref eval);
        }

        public static bool editEnum<T>(this string text, int width, ref T eval)
        {
            write(text, width);
            return editEnum(ref eval);
        }

        public static bool editEnum<T>(this string text, ref T eval)
        {
            write(text);
            return editEnum(ref eval);
        }

        public static bool editEnum<T>(ref T eval, int width = -1)
        {
            var val = Convert.ToInt32(eval);

            if (selectEnum(ref val, typeof(T), width))
            {
                eval = (T)((object)val);
                return true;
            }

            return false;
        }

        public static bool editEnumFlags<T>(this string text, string tip, int width, ref T eval)
        {
            write(text, tip, width);
            return editEnumFlags(ref eval);
        }

        public static bool editEnumFlags<T>(this string text, int width, ref T eval)
        {
            write(text, width);
            return editEnumFlags(ref eval);
        }

        public static bool editEnumFlags<T>(this string text, ref T eval)
        {
            write(text);
            return editEnumFlags(ref eval);
        }

        public static bool editEnumFlags<T>(ref T eval, int width = -1)
        {
            var val = Convert.ToInt32(eval);

            if (selectEnumFlags(ref val, typeof(T), width))
            {
                eval = (T)((object)val);
                return true;
            }

            return false;
        }

        public static bool editEnum(ref int current, Type type, int width = -1)
                => selectEnum(ref current, type, width);

        public static bool editEnum<T>(this string text, string tip, int width, ref int eval)
        {
            write(text, tip, width);
            return editEnum(ref eval, typeof(T));
        }

        public static bool editEnum<T>(this string text, int width, ref int eval)
        {
            write(text, width);
            return editEnum(ref eval, typeof(T));
        }

        public static bool editEnum<T>(this string text, ref int eval)
        {
            write(text);
            return editEnum(ref eval, typeof(T));
        }

        public static bool editEnum<T>(ref int eval) => editEnum(ref eval, typeof(T));

        #endregion

        #region String

        private static string editedText;
        private static string editedHash = "";
        public static bool editDelayed(ref string val)
        {
            if (LengthIsTooLong(ref val)) return false;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.editDelayed(ref val);
#endif

            checkLine();

            if ((KeyCode.Return.IsDown() && (val.GetHashCode().ToString() == editedHash)))
            {
                GUILayout.TextField(val, GuiMaxWidthOption);
                val = editedText;

                return change;
            }

            var tmp = val;
            if (edit(ref tmp).ignoreChanges())
            {
                editedText = tmp;
                editedHash = val.GetHashCode().ToString();
            }

            return false;

        }

        public static bool editDelayed(this string label, ref string val)
        {
            write(label, Msg.EditDelayed_HitEnter.GetText());
            return editDelayed(ref val);
        }

        public static bool editDelayed(ref string val, int width)
        {

            if (LengthIsTooLong(ref val)) return false;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.editDelayed(ref val, width);
#endif

            checkLine();

            if ((KeyCode.Return.IsDown() && (val.GetHashCode().ToString() == editedHash)))
            {
                GUILayout.TextField(val, GuiMaxWidthOption);
                val = editedText;
                return change;
            }

            var tmp = val;
            if (edit(ref tmp, width).ignoreChanges())
            {
                editedText = tmp;
                editedHash = val.GetHashCode().ToString();
            }

            return false;

        }

        public static bool editDelayed(this string label, ref string val, int width)
        {
            write(label, Msg.EditDelayed_HitEnter.GetText(), width);

            return editDelayed(ref val);

        }

        public static bool editDelayed(this string label, int width, ref string val)
        {
            write(label, width);
            return editDelayed(ref val);
        }

        public static bool editDelayed(this string label, string hint, int width, ref string val)
        {
            write(label, hint, width);
            return editDelayed(ref val);
        }

        private const int maxStringSize = 1000;

        private static bool LengthIsTooLong(ref string label)
        {
            if (label == null || label.Length < maxStringSize)
                return false;

            if (icon.Delete.ClickUnFocus())
            {
                label = "";
                return false;
            }
            else
                write("String is too long {0}".F(label.Substring(0, 10)));

            return true;
        }

        public static bool edit(ref string val)
        {

            if (LengthIsTooLong(ref val)) return false;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val);
#endif

            bc();
            val = GUILayout.TextField(val, GUILayout.MaxWidth(250));
            return ec();
        }

        public static bool edit(ref string val, int width)
        {

            if (LengthIsTooLong(ref val)) return false;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val, width);
#endif

            bc();
            var newval = GUILayout.TextField(val, GUILayout.MaxWidth(width));
            if (ec())
            {
                val = newval;
                return change;
            }
            return false;

        }

        public static bool editBig(ref string val, int height = 100)
        {

            nl();

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.editBig(ref val, height).nl();
#endif

            bc();
            val = GUILayout.TextArea(val, GUILayout.MaxHeight(height), GuiMaxWidthOption);
            return ec();

        }

        public static bool editBig(this string name, ref string val, int height = 100)
        {
            write(name + ":");
            return editBig(ref val, height);
        }

        public static bool edit(this string label, ref string val)
        {

            if (LengthIsTooLong(ref val)) return false;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(label, ref val);
#endif

            write(label);
            return edit(ref val);
        }

        public static bool edit(this string label, int width, ref string val)
        {
            write(label, width);
            return edit(ref val);
        }

        public static bool edit(this string label, string tip, int width, ref string val)
        {
            write(label, tip, width);
            return edit(ref val);
        }

        #endregion

        #region Property

        public static bool edit_Property<T>(Expression<Func<T>> memberExpression, Object obj, int fieldWidth = -1, bool includeChildren = true)
            => edit_Property(memberExpression, fieldWidth, obj, includeChildren);

        public static bool edit_Property<T>(this string label, Expression<Func<T>> memberExpression, Object obj, int fieldWidth = -1, bool includeChildren = true)
        {
            label.nl();
            return edit_Property(memberExpression, fieldWidth, obj, includeChildren);
        }

        public static bool edit_Property<T>(this string label, string tip, Expression<Func<T>> memberExpression, Object obj, int fieldWidth = -1, bool includeChildren = true)
        {
            label.nl(tip);
            return edit_Property(memberExpression, fieldWidth, obj, includeChildren);
        }

        public static bool edit_Property<T>(this string label, int width, Expression<Func<T>> memberExpression, Object obj, int fieldWidth = -1, bool includeChildren = true)
        {
            label.nl(width);
            return edit_Property(memberExpression, fieldWidth, obj, includeChildren);
        }

        public static bool edit_Property<T>(this string label, string tip, int width, Expression<Func<T>> memberExpression, Object obj, int fieldWidth = -1, bool includeChildren = true)
        {
            label.nl(tip, width);
            return edit_Property(memberExpression, fieldWidth, obj, includeChildren);
        }

        public static bool edit_Property<T>(this Texture tex, string tip, Expression<Func<T>> memberExpression, Object obj, int fieldWidth = -1, bool includeChildren = true)
        {
            tex.write(tip);
            nl();
            return edit_Property(memberExpression, fieldWidth, obj, includeChildren);
        }

        private static bool edit_Property<T>(Expression<Func<T>> memberExpression, int width, Object obj, bool includeChildren)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit_Property(width, memberExpression, obj, includeChildren);

#endif
            return false;
        }


        #endregion

        #region Custom classes

        public static bool edit(ref MyIntVec2 val)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val);
#endif

            return edit(ref val.x) || edit(ref val.y);
        }

        public static bool edit(ref MyIntVec2 val, int min, int max)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val, min, max);
#endif

            return edit(ref val.x, min, max) || edit(ref val.y, min, max);

        }

        public static bool edit(ref MyIntVec2 val, int min, MyIntVec2 max)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val, min, max);
#endif

            return edit(ref val.x, min, max.x) || edit(ref val.y, min, max.y);
        }

        public static bool edit(this string label, ref MyIntVec2 val)
        {
            write(label);
            nl();
            return edit(ref val);
        }

        public static bool edit(this string label, int width, ref MyIntVec2 val)
        {
            write(label, width);
            nl();
            return edit(ref val);
        }

        public static bool edit(this string label, int width, ref MyIntVec2 val, int min, int max)
        {
            write(label, width);
            nl();
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, int width, ref MyIntVec2 val, int min, MyIntVec2 max)
        {
            write(label, width);
            nl();
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, BoolDefine boolDefine)
        {
            label.write();
            return boolDefine.Inspect();
        }

        public static bool edit(this string label, int width, BoolDefine boolDefine)
        {
            label.write(width);
            return boolDefine.Inspect();
        }

        #endregion



        #endregion

        #region Transform

        private static bool _editLocalSpace = false;
        public static bool PEGI_CopyPaste(this Transform tf, ref bool editLocalSpace)
        {
            bool changed = false;

            "Local".toggle(40, ref editLocalSpace).changes(ref changed);

            if (icon.Copy.ClickUnFocus("Copy Transform"))
                StdExtensions.copyBufferValue = tf.Encode(editLocalSpace).ToString();

            if (StdExtensions.copyBufferValue != null && icon.Paste.ClickUnFocus("Paste Transform").changes(ref changed))
            {
                StdExtensions.copyBufferValue.DecodeInto(tf);
                StdExtensions.copyBufferValue = null;
            }

            nl();

            return changed;
        }
        public static bool PEGI_CopyPaste(this Transform tf)
        {

            return tf.PEGI_CopyPaste(ref _editLocalSpace);
        }

        public static bool inspect(this Transform tf, bool editLocalSpace)
        {
            var changed = false;

            if (editLocalSpace)
            {
                var v3 = tf.localPosition;
                if ("Pos".edit(ref v3).nl())
                    tf.localPosition = v3;

                var rot = tf.localRotation.eulerAngles;
                if ("Rot".edit(ref rot).nl())
                    tf.localRotation = Quaternion.Euler(rot);

                v3 = tf.localScale;
                if ("Size".edit(ref v3).nl())
                    tf.localScale = v3;
            }
            else
            {
                var v3 = tf.position;
                if ("Pos".edit(ref v3).nl())
                    tf.position = v3;

                var rot = tf.rotation.eulerAngles;
                if ("Rot".edit(ref rot).nl())
                    tf.rotation = Quaternion.Euler(rot);

            }
            return changed;
        }
        public static bool inspect(this Transform tf) => tf.inspect(_editLocalSpace);

        #endregion

        #region Inspect Name

        public static bool Try_NameInspect(object obj, string label = "", string tip = "")
        {
            bool could;
            return obj.Try_NameInspect(out could, label, tip);
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

            if (uObj)
            {
                var n = uObj.name;
                if (gotLabel ? label.editDelayed(tip, 80, ref n) : editDelayed(ref n))
                {
                    uObj.name = n;
                    uObj.RenameAsset(n);
                    changed = true;
                }
            }
            else
                couldInspect = false;

            return changed;
        }

        public static bool inspect_Name(this IGotName obj) => obj.inspect_Name("", obj.GetNameForInspector());

        public static bool inspect_Name(this IGotName obj, string label) => obj.inspect_Name(label, label);

        public static bool inspect_Name(this IGotName obj, string label, string hint)
        {

            var n = obj.NameForPEGI;

            bool gotLabel = !label.IsNullOrEmpty();

            var uObj = obj as Object;

            if (uObj)
            {

                if ((gotLabel && label.editDelayed(80, ref n)) || (!gotLabel && editDelayed(ref n)))
                {
                    obj.NameForPEGI = n;

                    return true;
                }
            }
            else
            if ((gotLabel && label.edit(80, ref n) || (!gotLabel && edit(ref n))))
            {
                obj.NameForPEGI = n;
                return true;
            }

            return false;
        }

        #endregion

    }
}