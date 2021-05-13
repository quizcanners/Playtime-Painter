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

        public static bool isFoldout(this string txt, ref bool state)
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

        public static bool isFoldout(this string txt, ref int selected, int current)
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

        public static bool isFoldout(this icon ico, string text, ref bool state) => ico.GetIcon().isFoldout(text, ref state);

        public static bool isFoldout(this string txt)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.foldout(txt);
#endif

            isFoldout(txt, ref selectedFold, _elementIndex);

            _elementIndex++;

            return ef.isFoldedOutOrEntered;
        }

        internal static bool isFoldout(this Texture2D tex, string text, ref bool state)
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

        internal static void FoldInNow() => selectedFold = -1;
        #endregion

        #region Enter & Exit
        
        public static bool isEntered(ref int enteredOne, int current)
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

        public static bool isEntered<T>(this icon ico, ref int enteredOne, T currentEnum) where T : struct
            => ico.isEntered(ref enteredOne, Convert.ToInt32(currentEnum), currentEnum.ToString());

        public static bool isEntered(this icon ico, ref int enteredOne, int thisOne, string tip = null)
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

        public static bool isEntered(this icon ico, ref bool state, string tip = null)
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

        public static bool isEntered(this icon ico, string txt, ref bool state, bool showLabelIfTrue = true)
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

        public static bool isEntered(this icon ico, string txt, ref int enteredOne, int thisOne, bool showLabelIfTrue = true, PEGI_Styles.PegiGuiStyle enterLabelStyle = null)
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

        private static bool isEntered_DirectlyToElement<T>(this List<T> list, ref int inspected)
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

        private static bool isEntered_DirectlyToElement<T>(this List<T> list, ref int inspected, ref int enteredOne, int thisOne)
        {

            if (enteredOne == -1 && list.isEntered_DirectlyToElement(ref inspected))
                enteredOne = thisOne;

            return enteredOne == thisOne;
        }

        private static void isEntered_DirectlyToElement<T>(this List<T> list, ref int inspected, ref bool entered)
        {
            if (!entered && list.isEntered_DirectlyToElement(ref inspected))
                entered = true;
        }

        private static bool isEntered_HeaderPart<T>(this ListMetaData meta, List<T> list, ref bool entered, bool showLabelIfTrue = false)
        {
            int tmpEntered = entered ? 1 : -1;
            var ret = meta.isEntered_HeaderPart(list, ref tmpEntered, 1, showLabelIfTrue);
            entered = tmpEntered == 1;
            return ret;
        }

        private static bool isEntered_HeaderPart<T>(this ListMetaData meta, List<T> list, ref int enteredOne, int thisOne, bool showLabelIfTrue = false)
        {
            
            if (collectionInspector.listIsNull(list))
            {
                if (enteredOne == thisOne)
                    enteredOne = -1;
                return false;
            }

            var entered = enteredOne == thisOne;

            var ret =icon.Enter.isEntered(meta.label.addCount(list, entered), ref enteredOne, thisOne, showLabelIfTrue, list.Count == 0 ? PEGI_Styles.ClippingText : null);

            if (!entered && ret)
                meta.inspected = -1;

            ret |= list.isEntered_DirectlyToElement(ref meta.inspected, ref enteredOne, thisOne);

            return ret;
        }

        public static bool isEntered(this string txt, ref bool state, bool showLabelIfTrue = true) => icon.Enter.isEntered(txt, ref state, showLabelIfTrue);

        public static bool isEntered(this string txt, ref int enteredOne, int thisOne, bool showLabelIfTrue = true, PEGI_Styles.PegiGuiStyle enterLabelStyle = null)
            => icon.Enter.isEntered(txt, ref enteredOne, thisOne, showLabelIfTrue, enterLabelStyle);
        
        private static bool isEntered_ListIcon<T>(this string txt, List<T> list, ref bool isEntered)
        {
            if (collectionInspector.listIsNull(list))
            {
                if (isEntered)
                    isEntered = false;
                return false;
            }

            return icon.List.isEntered(txt.addCount(list, isEntered), ref isEntered, showLabelIfTrue: false);
        }
        
        private static bool isEntered_ListIcon<T>(this string txt, List<T> list, ref int inspected, ref int enteredOne, int thisOne)
        {
            if (collectionInspector.listIsNull(list))
            {
                if (enteredOne == thisOne)
                    enteredOne = -1;
                return false;
            }

            var before = enteredOne == thisOne;

            if (icon.List.isEntered(txt.addCount(list, before), ref enteredOne, thisOne, false,
                list.Count == 0 ? PEGI_Styles.ClippingText : null) && (!before) && (enteredOne == thisOne))
                inspected = -1;

            list.isEntered_DirectlyToElement(ref inspected, ref enteredOne, thisOne);

            return enteredOne == thisOne;
        }

        private static bool isEnter_ListIcon<T>(this string txt, List<T> list, ref int inspected, ref bool entered)
        {
            if (collectionInspector.listIsNull(list))
            {
                if (entered)
                    entered = false;
                return false;
            }

            bool before = entered;

            if (icon.List.isEntered(txt.addCount(list, entered), ref entered, false) && (before != entered))
                inspected = -1;

            list.isEntered_DirectlyToElement(ref inspected, ref entered);

            return entered;
        }

        private static string tryAddCount(this string txt, object obj)
        {
            var c = obj as IGotCount;
            if (!c.IsNullOrDestroyed_Obj())
                txt += " [{0}]".F(c.CountForInspector());

            return txt;
        }

        public static string addCount<T>(this string txt, ICollection<T> lst, bool entered = false)
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
            if (val == null) 
            {
                label += " (NULL)";
            }

            if (label.isEntered(ref inspected, current))
            {              
                return val.Nested_Inspect();
            }

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
                    var.InspectInList(thisOne, ref enteredOne);
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

        public static bool isConditionally_Entered(bool canEnter, ref int enteredOne, int thisOne, string exitLabel = "")
        {

            if (!canEnter && enteredOne == thisOne)
                enteredOne = -1;

            if (canEnter)
            {
                icon.Enter.isEntered(ref enteredOne, thisOne);
                if (enteredOne == thisOne && !exitLabel.IsNullOrEmpty() &&
                    exitLabel.ClickLabel(icon.Exit.GetDescription(), style: PEGI_Styles.ExitLabel))
                    enteredOne = -1;
            }
            else
                ef.isFoldedOutOrEntered = false;

            return ef.isFoldedOutOrEntered;
        }

        public static bool isConditionally_Entered(this string label, bool canEnter, ref int enteredOne, int thisOne, bool showLabelIfTrue = true, PEGI_Styles.PegiGuiStyle enterLabelStyle = null)
        {

            if (!canEnter && enteredOne == thisOne)
            {
                if (icon.Back.Click() || "All Done here".ClickText(14))
                    enteredOne = -1;
            }
            else
            {
                if (canEnter)
                    label.isEntered(ref enteredOne, thisOne, showLabelIfTrue, enterLabelStyle);
                else
                    ef.isFoldedOutOrEntered = false;
            }

            return ef.isFoldedOutOrEntered;
        }

        public static bool isConditionally_Entered(this string label, bool canEnter, ref bool entered, bool showLabelIfTrue = true)
        {

            if (!canEnter && entered)
            {
                if (icon.Back.Click() || "All Done here".ClickText(14))
                    entered = false;
            }
            else
            {

                if (canEnter)
                    label.isEntered(ref entered, showLabelIfTrue);
                else
                    ef.isFoldedOutOrEntered = false;
            }

            return ef.isFoldedOutOrEntered;
        }

        public static bool isToggle_Entered(this string label, ref bool val, ref int enteredOne, int thisOne, bool showLabelWhenEntered = false)
        {

            if (enteredOne == -1)
                label.toggleIcon(ref val);

            if (val)
                isEntered(ref enteredOne, thisOne);
            else
                ef.isFoldedOutOrEntered = false;

            if (enteredOne == thisOne)
                label.toggleIcon(ref val);

            if (!val && enteredOne == thisOne)
                enteredOne = -1;

            return ef.isFoldedOutOrEntered;
        }

        public static bool enter_List<T>(this ListMetaData meta, List<T> list, ref int enteredOne, int thisOne)
        {
            if (meta.isEntered_HeaderPart(list, ref enteredOne, thisOne))
                return meta.edit_List(list).nl();

            return false;
        }

        public static bool enter_List_UObj<T>(this string label, List<T> list, ref int enteredOne, int thisOne, List<T> selectFrom = null) where T : Object
        {
            var insp = -1;
            if (isEntered_ListIcon(label, list, ref insp, ref enteredOne, thisOne)) 
                return label.edit_List_UObj(list, selectFrom).nl();

            return false;
        }

        public static bool enter_List<T>(this string label, List<T> list, ref int inspectedElement, ref int enteredOne, int thisOne) =>
            enter_List(label, list, ref inspectedElement, ref enteredOne, thisOne, out _);

        public static bool enter_List<T>(this string label, List<T> list, ref int inspectedElement, ref int enteredOne, int thisOne, out T added)
        {
            added = default(T);

            var changes = ChangeTrackStart();

            if (enteredOne == -1 && list != null && list.Count == 0 && typeof(Object).IsAssignableFrom(typeof(T)) == false)
            {
                var showedOption = collectionInspector.TryShowListAddNewOption(label, list, ref added);

                if (!showedOption)
                    showedOption = collectionInspector.TryShowListCreateNewOptions(list, ref added, null);

                if (showedOption)
                {
                    return changes;
                }
            }

            if (isEntered_ListIcon(label, list, ref inspectedElement, ref enteredOne, thisOne)) //if (label.AddCount(list).enter(ref enteredOne, thisOne))
                label.edit_List(list, ref inspectedElement, out added);

            return changes;
        }

        public static bool enter_List<T>(this string label, List<T> list, ref int inspectedElement, ref bool entered)
        {

            var changed = pegi.ChangeTrackStart();

            if (isEnter_ListIcon(label, list, ref inspectedElement, ref entered))// if (label.AddCount(list).enter(ref entered))
                label.edit_List(list, ref inspectedElement).nl();

            return changed;
        }

        public static bool enter_List<T>(this string label, List<T> list, ref bool entered)
        {
            if (isEntered_ListIcon(label, list, ref entered))
                return label.edit_List(list).nl();

            return false;
        }


        #region Tagged Types

        public static bool enter_List<T>(this ListMetaData meta, List<T> list, ref int enteredOne, int thisOne, TaggedTypesCfg types, out T added)
        {

            if (meta.isEntered_HeaderPart(list, ref enteredOne, thisOne))
                return meta.edit_List(list, types, out added);

            added = default;
            return false;
        }

        public static bool enter_List<T>(this ListMetaData meta, List<T> list, ref bool entered, TaggedTypesCfg types, out T added)
        {
            if (meta.isEntered_HeaderPart(list, ref entered))
                return meta.edit_List(list, types, out added);

            added = default;
            return false;
        }

        #endregion

        public static bool conditional_enter_List<T>(this string label, bool canEnter, List<T> list, ref int inspectedElement, ref int enteredOne, int thisOne)
        {

            var changed = ChangeTrackStart();

            if (!canEnter && enteredOne == thisOne)
                enteredOne = -1;

            if (canEnter)
                label.enter_List(list, ref inspectedElement, ref enteredOne, thisOne);
            else
                ef.isFoldedOutOrEntered = false;

            return changed;

        }

        public static bool conditional_enter_List<T>(this ListMetaData meta, bool canEnter, List<T> list, ref int enteredOne, int thisOne)
        {

            var changed = false;

            if (!canEnter && enteredOne == thisOne)
                enteredOne = -1;

            if (canEnter)
                meta.enter_List(list, ref enteredOne, thisOne).changes_Internal(ref changed);
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