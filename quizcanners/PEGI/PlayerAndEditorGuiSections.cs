using System;
using System.Collections.Generic;
using System.Linq;
using QuizCanners.CfgDecode;
using QuizCanners.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0011 // Add braces
#pragma warning disable IDE0008 // Use explicit type
#pragma warning disable IDE0009 // Member access should be qualified.

namespace QuizCanners.Inspect
{
    public static partial class pegi
    {

        #region Foldout    

        public static bool foldout(this string txt, ref bool state)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.foldout(txt, ref state);
#endif

            checkLine();

            if (ClickUnFocus((state ? "[Hide] {0}..." : ">{0} [Show]").F(txt)))
                state = !state;


            ef.isFoldedOutOrEntered = state;

            return ef.isFoldedOutOrEntered;

        }

        public static bool foldout(this string txt, ref int selected, int current)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.foldout(txt, ref selected, current);
#endif

            checkLine();

            ef.isFoldedOutOrEntered = (selected == current);

            if (ClickUnFocus((ef.isFoldedOutOrEntered ? "[Hide] {0}..." : ">{0} [Show]").F(txt)))
            {
                if (ef.isFoldedOutOrEntered)
                    selected = -1;
                else
                    selected = current;
            }

            ef.isFoldedOutOrEntered = selected == current;

            return ef.isFoldedOutOrEntered;

        }

        public static bool foldout(this icon ico, string text, ref bool state) => ico.GetIcon().foldout(text, ref state);

        public static bool foldout(this string txt)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.foldout(txt);
#endif

            foldout(txt, ref selectedFold, _elementIndex);

            _elementIndex++;

            return ef.isFoldedOutOrEntered;
        }

        internal static bool foldout(this Texture2D tex, string text, ref bool state)
        {

            if (state)
            {
                if (icon.FoldedOut.ClickUnFocus(text, 30))
                    state = false;
            }
            else
            {
                if (tex.Click(text))
                    state = true;
            }
            return state;
        }

        internal static void foldIn() => selectedFold = -1;
        #endregion

        #region Enter & Exit

        public static bool enter<T>(ref int enteredOne, T currentEnum, string tip) where T: struct =>
             enter(ref enteredOne, Convert.ToInt32(currentEnum), tip);

        public static bool enter(ref int enteredOne, int current)
        {
            if (enteredOne == current)
            {
                if (icon.Exit.ClickUnFocus())
                    enteredOne = -1;
            }
            else if (enteredOne == -1 && icon.Enter.ClickUnFocus())
                enteredOne = current;

            ef.isFoldedOutOrEntered = (enteredOne == current);

            return ef.isFoldedOutOrEntered;
        }

        public static bool enter<T>(this icon ico, ref int enteredOne, T currentEnum) where T : struct
            => ico.enter(ref enteredOne, Convert.ToInt32(currentEnum), currentEnum.ToString());

        public static bool enter(this icon ico, ref int enteredOne, int thisOne, string tip = null)
        {
            var outside = enteredOne == -1;

            if (enteredOne == thisOne)
            {
                if (icon.Exit.ClickUnFocus(tip))
                    enteredOne = -1;

            }
            else if (outside)
            {
                if (ico.ClickUnFocus(tip))
                    enteredOne = thisOne;
            }

            ef.isFoldedOutOrEntered = (enteredOne == thisOne);

            return ef.isFoldedOutOrEntered;
        }

        public static bool enter(this icon ico, ref bool state, string tip = null)
        {

            if (state)
            {
                if (icon.Exit.ClickUnFocus(tip))
                    state = false;
            }
            else if (ico.ClickUnFocus(tip))
                state = true;

            ef.isFoldedOutOrEntered = state;

            return ef.isFoldedOutOrEntered;
        }

        public static bool enter(this icon ico, string txt, ref bool state, bool showLabelIfTrue = true)
        {

            if (state)
            {
                if (icon.Exit.ClickUnFocus("{0} {1}".F(icon.Exit.GetText(), txt)))
                    state = false;
            }
            else if (ico.ClickUnFocus("{0} {1}".F(icon.Enter.GetText(), txt)))
                state = true;


            if ((showLabelIfTrue || !state) &&
                txt.ClickLabel(txt, -1, state ? PEGI_Styles.ExitLabel : PEGI_Styles.EnterLabel))
                state = !state;

            ef.isFoldedOutOrEntered = state;

            return ef.isFoldedOutOrEntered;
        }

        public static bool enter(this icon ico, string txt, ref int enteredOne, int thisOne, bool showLabelIfTrue = true, PEGI_Styles.PegiGuiStyle enterLabelStyle = null)
        {
            var outside = enteredOne == -1;

            var current = enteredOne == thisOne;

            if (current)
            {
                if (icon.Exit.ClickUnFocus("{0} {1}".F(icon.Exit.GetText(), txt)))
                    enteredOne = -1;
            }
            else if (outside && ico.ClickUnFocus(txt))
                enteredOne = thisOne;


            if (((showLabelIfTrue && current) || outside) &&
                txt.ClickLabel(txt, -1, outside ? enterLabelStyle ?? PEGI_Styles.EnterLabel : PEGI_Styles.ExitLabel))
                enteredOne = outside ? thisOne : -1;


            ef.isFoldedOutOrEntered = (enteredOne == thisOne);

            return ef.isFoldedOutOrEntered;
        }

        private static bool enter_DirectlyToElement<T>(this List<T> list, ref int inspected)
        {

            if ((inspected == -1 && list.Count > 1) || list.Count == 0) return false;

            int suggestedIndex = Mathf.Max(inspected, 0);

            if (suggestedIndex >= list.Count)
                suggestedIndex = 0;

            icon ico;
            string msg;

            if (NeedsAttention(list, out msg))
            {
                if (inspected == -1)
                    suggestedIndex = LastNeedAttentionIndex;

                ico = icon.Warning;
            }
            else
            {
                ico = icon.Next;
                msg = "->";
            }

            var el = list.TryGet(suggestedIndex);// as IPEGI;

            if (ico.Click(msg + el.GetNameForInspector()))
            {
                inspected = suggestedIndex;
                ef.isFoldedOutOrEntered = true;
                return true;
            }
            return false;
        }

        private static bool enter_DirectlyToElement<T>(this List<T> list, ref int inspected, ref int enteredOne, int thisOne)
        {

            if (enteredOne == -1 && list.enter_DirectlyToElement(ref inspected))
                enteredOne = thisOne;

            return enteredOne == thisOne;
        }

        private static bool enter_DirectlyToElement<T>(this List<T> list, ref int inspected, ref bool entered)
        {

            if (!entered && list.enter_DirectlyToElement(ref inspected))
                entered = true;

            return entered;
        }

        private static bool enter_HeaderPart<T>(this ListMetaData meta, ref List<T> list, ref bool entered, bool showLabelIfTrue = false)
        {
            int tmpEntered = entered ? 1 : -1;
            var ret = meta.enter_HeaderPart(ref list, ref tmpEntered, 1, showLabelIfTrue);
            entered = tmpEntered == 1;
            return ret;
        }

        private static bool enter_HeaderPart<T>(this ListMetaData meta, ref List<T> list, ref int enteredOne, int thisOne, bool showLabelIfTrue = false)
        {

            if (collectionInspector.listIsNull(ref list))
            {
                if (enteredOne == thisOne)
                    enteredOne = -1;
                return false;
            }

            var entered = enteredOne == thisOne;

            var ret = meta.icon.enter(meta.label.AddCount(list, entered), ref enteredOne, thisOne, showLabelIfTrue, list.Count == 0 ? PEGI_Styles.ClippingText : null);

            if (!entered && ret)
                meta.inspected = -1;

            ret |= list.enter_DirectlyToElement(ref meta.inspected, ref enteredOne, thisOne);

            return ret;
        }

        public static bool enter(this string txt, ref bool state, bool showLabelIfTrue = true) => icon.Enter.enter(txt, ref state, showLabelIfTrue);

        public static bool enter(this string txt, ref int enteredOne, int thisOne, bool showLabelIfTrue = true, PEGI_Styles.PegiGuiStyle enterLabelStyle = null)
            => icon.Enter.enter(txt, ref enteredOne, thisOne, showLabelIfTrue, enterLabelStyle);

        private static bool enter_ListIcon<T>(this string txt, ref List<T> list, ref int enteredOne, int thisOne)
        {
            if (collectionInspector.listIsNull(ref list))
            {
                if (enteredOne == thisOne)
                    enteredOne = -1;
                return false;
            }

            return icon.List.enter(txt.AddCount(list, enteredOne == thisOne), ref enteredOne, thisOne, false, list.Count == 0 ? PEGI_Styles.ClippingText : null);
        }

        private static bool enter_ListIcon<T>(this string txt, ref List<T> list, ref bool isEntered)
        {
            if (collectionInspector.listIsNull(ref list))
            {
                if (isEntered)
                    isEntered = false;
                return false;
            }

            return icon.List.enter(txt.AddCount(list, isEntered), ref isEntered, showLabelIfTrue: false);
        }
        
        private static bool enter_ListIcon<T>(this string txt, ref List<T> list, ref int inspected, ref int enteredOne, int thisOne)
        {
            if (collectionInspector.listIsNull(ref list))
            {
                if (enteredOne == thisOne)
                    enteredOne = -1;
                return false;
            }

            var before = enteredOne == thisOne;

            if (icon.List.enter(txt.AddCount(list, before), ref enteredOne, thisOne, false,
                list.Count == 0 ? PEGI_Styles.ClippingText : null) && (!before) && (enteredOne == thisOne))
                inspected = -1;

            list.enter_DirectlyToElement(ref inspected, ref enteredOne, thisOne);

            return enteredOne == thisOne;
        }

        private static bool enter_ListIcon<T>(this string txt, ref List<T> list, ref int inspected, ref bool entered)
        {
            if (collectionInspector.listIsNull(ref list))
            {
                if (entered)
                    entered = false;
                return false;
            }

            bool before = entered;

            if (icon.List.enter(txt.AddCount(list, entered), ref entered, false) && (before != entered))
                inspected = -1;

            list.enter_DirectlyToElement(ref inspected, ref entered);

            return entered;
        }

        private static string TryAddCount(this string txt, object obj)
        {
            var c = obj as IGotCount;
            if (!c.IsNullOrDestroyed_Obj())
                txt += " [{0}]".F(c.CountForInspector());

            return txt;
        }

        public static string AddCount<T>(this string txt, ICollection<T> lst, bool entered = false)
        {
            if (lst == null)
                return "{0} is NULL".F(txt);

            if (lst.Count > 1)
                return "{0} [{1}]".F(txt, lst.Count);

            if (lst.Count == 0)
                return "NO {0}".F(txt);

            if (!entered)
            {

                var el = lst.ElementAt(0);

                if (!el.IsNullOrDestroyed_Obj())
                {

                    var nm = el as IGotDisplayName;

                    if (nm != null)
                        return "{0}: {1}".F(txt, nm.NameForDisplayPEGI());

                    var n = el as IGotName;

                    if (n != null)
                        return "{0}: {1}".F(txt, n.NameForPEGI);

                    return "{0}: {1}".F(txt, el.GetNameForInspector());

                }

                return "{0} one Null Element".F(txt);
            }

            return "{0} [1]".F(txt);
        }

        public static bool enter_Inspect(this string label, IPEGI val, ref int inspected, int current)
        {
            if (label.enter(ref inspected, current))
                return val.Nested_Inspect();

            return false;
        }

        public static bool enter_Inspect(this IPEGI var, ref int enteredOne, int thisOne)
        {

            var lst = var as IPEGI_ListInspect;

            return lst != null ? lst.enter_Inspect_AsList(ref enteredOne, thisOne) :
                var.GetNameForInspector().enter_Inspect(var, ref enteredOne, thisOne);
        }

        public static bool enter_Inspect_AsList(this IPEGI_ListInspect var, ref int enteredOne, int thisOne, string exitLabel = null)
        {
            var changed = ChangeTrackStart();

            var outside = enteredOne == -1;

            if (!var.IsNullOrDestroyed_Obj())
            {

                if (outside) 
                    var.InspectInList(null, thisOne, ref enteredOne);
                else if (enteredOne == thisOne)
                {

                    if (exitLabel.IsNullOrEmpty())
                        exitLabel = var.GetNameForInspector();

                    if (icon.Exit.ClickUnFocus("{0} L {1}".F(icon.Exit.GetText(), var))
                        || exitLabel.ClickLabel(icon.Exit.GetDescription(), style: PEGI_Styles.ExitLabel))
                        enteredOne = -1;
                    Try_Nested_Inspect(var);
                }
            }
            else if (enteredOne == thisOne)
                enteredOne = -1;


            ef.isFoldedOutOrEntered = enteredOne == thisOne;

            return changed;
        }

        public static bool conditional_enter(bool canEnter, ref int enteredOne, int thisOne, string exitLabel = "")
        {

            if (!canEnter && enteredOne == thisOne)
                enteredOne = -1;

            if (canEnter)
            {
                icon.Enter.enter(ref enteredOne, thisOne);
                if (enteredOne == thisOne && !exitLabel.IsNullOrEmpty() &&
                    exitLabel.ClickLabel(icon.Exit.GetDescription(), style: PEGI_Styles.ExitLabel))
                    enteredOne = -1;
            }
            else
                ef.isFoldedOutOrEntered = false;

            return ef.isFoldedOutOrEntered;
        }

        public static bool conditional_enter(this string label, bool canEnter, ref int enteredOne, int thisOne, bool showLabelIfTrue = true, PEGI_Styles.PegiGuiStyle enterLabelStyle = null)
        {

            if (!canEnter && enteredOne == thisOne)
            {
                if (icon.Back.Click() || "All Done here".ClickText(14))
                    enteredOne = -1;
            }
            else
            {
                if (canEnter)
                    label.enter(ref enteredOne, thisOne, showLabelIfTrue, enterLabelStyle);
                else
                    ef.isFoldedOutOrEntered = false;
            }

            return ef.isFoldedOutOrEntered;
        }

        public static bool conditional_enter(this string label, bool canEnter, ref bool entered, bool showLabelIfTrue = true)
        {

            if (!canEnter && entered)
            {
                if (icon.Back.Click() || "All Done here".ClickText(14))
                    entered = false;
            }
            else
            {

                if (canEnter)
                    label.enter(ref entered, showLabelIfTrue);
                else
                    ef.isFoldedOutOrEntered = false;
            }

            return ef.isFoldedOutOrEntered;
        }

        public static bool toggle_enter(this string label, ref bool val, ref int enteredOne, int thisOne, ref bool changed, bool showLabelWhenEntered = false)
        {

            if (enteredOne == -1)
                label.toggleIcon(ref val).changes(ref changed);

            if (val)
                enter(ref enteredOne, thisOne);
            else
                ef.isFoldedOutOrEntered = false;

            if (enteredOne == thisOne)
                label.toggleIcon(ref val).changes(ref changed);

            if (!val && enteredOne == thisOne)
                enteredOne = -1;

            return ef.isFoldedOutOrEntered;
        }

        public static bool enter_List<T>(this ListMetaData meta, ref List<T> list, ref int enteredOne, int thisOne)
        {
            var changed = false;

            if (meta.enter_HeaderPart(ref list, ref enteredOne, thisOne))
                meta.edit_List(ref list).nl(ref changed);

            return changed;
        }

        public static bool enter_List_UObj<T>(this string label, ref List<T> list, ref int enteredOne, int thisOne, List<T> selectFrom = null) where T : Object
        {

            var changed = false;

            var insp = -1;
            if (enter_ListIcon(label, ref list, ref insp, ref enteredOne, thisOne)) // if (label.AddCount(list).enter(ref enteredOne, thisOne))
                label.edit_List_UObj(ref list, selectFrom).nl(ref changed);

            return changed;
        }

        public static bool enter_List<T>(this string label, ref List<T> list, ref int inspectedElement, ref int enteredOne, int thisOne)
        {
            var changed = false;
            label.enter_List(ref list, ref inspectedElement, ref enteredOne, thisOne, ref changed);
            return changed;
        }

        public static T enter_List<T>(this string label, ref List<T> list, ref int inspectedElement, ref int enteredOne, int thisOne, ref bool changed)
        {
            var added = default(T);

            if (enteredOne == -1 && list != null && list.Count == 0 && typeof(Object).IsAssignableFrom(typeof(T)) == false)
            {
                var showedOption = collectionInspector.TryShowListAddNewOption(label, list, ref added, ref changed, null);

                if (!showedOption)
                    showedOption = collectionInspector.TryShowListCreateNewOptions(list, ref added, null, ref changed);

                if (showedOption)
                {
                    return added;
                }
            }

            if (enter_ListIcon(label, ref list, ref inspectedElement, ref enteredOne, thisOne)) //if (label.AddCount(list).enter(ref enteredOne, thisOne))
                added = label.edit_List(ref list, ref inspectedElement, ref changed);

            return added;
        }

        public static bool enter_List<T>(this string label, ref List<T> list, ref int inspectedElement, ref bool entered)
        {

            var changed = false;

            if (enter_ListIcon(label, ref list, ref inspectedElement, ref entered))// if (label.AddCount(list).enter(ref entered))
                label.edit_List(ref list, ref inspectedElement).nl(ref changed);

            return changed;
        }

        #region Tagged Types

        public static T enter_List<T>(this ListMetaData meta, ref List<T> list, ref int enteredOne, int thisOne, TaggedTypesCfg types, ref bool changed) =>
            meta.enter_HeaderPart(ref list, ref enteredOne, thisOne) ? meta.edit_List(ref list, types, ref changed) : default;

        public static T enter_List<T>(this ListMetaData meta, ref List<T> list, ref bool entered, TaggedTypesCfg types, ref bool changed) =>
            meta.enter_HeaderPart(ref list, ref entered) ? meta.edit_List(ref list, types, ref changed) : default;

        #endregion

        public static bool conditional_enter_List<T>(this string label, bool canEnter, ref List<T> list, ref int inspectedElement, ref int enteredOne, int thisOne)
        {

            var changed = false;

            if (!canEnter && enteredOne == thisOne)
                enteredOne = -1;

            if (canEnter)
                label.enter_List(ref list, ref inspectedElement, ref enteredOne, thisOne).changes(ref changed);
            else
                ef.isFoldedOutOrEntered = false;

            return changed;

        }

        public static bool conditional_enter_List<T>(this ListMetaData meta, bool canEnter, ref List<T> list, ref int enteredOne, int thisOne)
        {

            var changed = false;

            if (!canEnter && enteredOne == thisOne)
                enteredOne = -1;

            if (canEnter)
                meta.enter_List(ref list, ref enteredOne, thisOne).changes(ref changed);
            else
                ef.isFoldedOutOrEntered = false;

            return changed;
        }

        #endregion

        #region Line

        public static void line() => line(PaintingGameViewUI ? Color.white : Color.black);

        public static void line(Color col)
        {
            nl();

            var c = GUI.color;
            GUI.color = col;
            if (PaintingGameViewUI)
                GUILayout.Box(GUIContent.none, PEGI_Styles.HorizontalLine.Current, GuiMaxWidthOption);
            else
                GUILayout.Box(GUIContent.none, PEGI_Styles.HorizontalLine.Current);

            GUI.color = c;
        }

        #endregion
    }
}