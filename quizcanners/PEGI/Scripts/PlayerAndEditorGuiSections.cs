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

        private static bool EnterOptionsDrawn_Internal (ref int entered, int thisOne) => entered == -1 || entered == thisOne;


        public static bool isEntered(ref int entered, int thisOne)
        {
            if (!EnterOptionsDrawn_Internal(ref entered, thisOne))
                return false;

            if (entered == thisOne)
            {
                if (icon.Exit.ClickUnFocus())
                    entered = -1;
            }
            else if (entered == -1 && icon.Enter.ClickUnFocus())
                entered = thisOne;

            ef.isFoldedOutOrEntered = (entered == thisOne);

            return ef.isFoldedOutOrEntered;
        }

        public static bool isEntered<T>(this icon ico, ref int enteredOne, T currentEnum) where T : struct
            => ico.isEntered(ref enteredOne, Convert.ToInt32(currentEnum), currentEnum.ToString());

        public static bool isEntered(this icon ico, ref int entered, int thisOne, string tip = null)
        {
            if (!EnterOptionsDrawn_Internal(ref entered, thisOne))
                return false;

            var outside = entered == -1;

            if (entered == thisOne)
            {
                if (icon.Exit.ClickUnFocus(tip))
                    entered = -1;

            }
            else if (outside)
            {
                if (ico.ClickUnFocus(tip))
                    entered = thisOne;
            }

            ef.isFoldedOutOrEntered = (entered == thisOne);

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

        public static bool isEntered(this icon ico, string txt, ref int entered, int thisOne, bool showLabelIfTrue = true, PEGI_Styles.PegiGuiStyle enterLabelStyle = null)
        {
            if (!EnterOptionsDrawn_Internal(ref entered, thisOne))
                return false;

            var outside = entered == -1;

            var IsCurrent = entered == thisOne;

            if (IsCurrent)
            {
                if (icon.Exit.ClickUnFocus("{0} {1}".F(icon.Exit.GetText(), txt)))
                    entered = -1;
            }
            else if (outside && ico.ClickUnFocus(txt))
                entered = thisOne;


            if (((showLabelIfTrue && IsCurrent) || outside) &&
                txt.ClickLabel(txt, -1, outside ? enterLabelStyle ?? PEGI_Styles.EnterLabel : PEGI_Styles.ExitLabel))
                entered = outside ? thisOne : -1;


            ef.isFoldedOutOrEntered = (entered == thisOne);

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

        private static bool isEntered_HeaderPart<T>(this ListMetaData meta, List<T> list, ref int entered, int thisOne, bool showLabelIfTrue = false)
        {
            if (!EnterOptionsDrawn_Internal(ref entered, thisOne))
                return false;

            if (collectionInspector.listIsNull(list))
            {
                if (entered == thisOne)
                    entered = -1;
                return false;
            }

            var isEntered = entered == thisOne;

            var ret =icon.Enter.isEntered(meta.label.addCount(list, isEntered), ref entered, thisOne, showLabelIfTrue, list.Count == 0 ? PEGI_Styles.ClippingText : null);

            if (!isEntered && ret)
                meta.inspected = -1;

            ret |= list.isEntered_DirectlyToElement(ref meta.inspected, ref entered, thisOne);

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
        
        private static bool isEntered_ListIcon<T>(this string txt, List<T> list, ref int inspected, ref int entered, int thisOne)
        {
            if (!EnterOptionsDrawn_Internal(ref entered, thisOne))
                return false;

            if (collectionInspector.listIsNull(list))
            {
                if (entered == thisOne)
                    entered = -1;
                return false;
            }

            var before = entered == thisOne;

            if (icon.List.isEntered(txt.addCount(list, before), ref entered, thisOne, false,
                list.Count == 0 ? PEGI_Styles.ClippingText : null) && (!before) && (entered == thisOne))
                inspected = -1;

            list.isEntered_DirectlyToElement(ref inspected, ref entered, thisOne);

            return entered == thisOne;
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


        public static bool enter_Inspect(this string label, IPEGI val, ref int entered, int thisOne)
        {
            if (!EnterOptionsDrawn_Internal(ref entered, thisOne))
                return false;

            if (val == null) 
            {
                label += " (NULL)";
            }

            if (label.isEntered(ref entered, thisOne))
            {              
                return val.Nested_Inspect();
            }

            return false;
        }

        public static bool enter_Inspect(this IPEGI var, ref int entered, int thisOne)
        {
            if (!EnterOptionsDrawn_Internal(ref entered, thisOne))
                return false;

            var lst = var as IPEGI_ListInspect;

            return lst != null ? lst.enter_Inspect_AsList(ref entered, thisOne) :
                var.GetNameForInspector().enter_Inspect(var, ref entered, thisOne);
        }

        public static bool try_enter_Inspect(this string label, object target, ref int entered, int thisOne) 
        {
            if (!EnterOptionsDrawn_Internal(ref entered, thisOne))
                return false;

            var lst = target as IPEGI_ListInspect;

            if (lst != null)
            {
                return lst.enter_Inspect_AsList(ref entered, thisOne);
            }

            var IPEGI = target as IPEGI;

            if (IPEGI == null) 
            {
                if (entered == thisOne && icon.Back.Click())
                    entered = -1;

                "{0} : {1}".F(label, (target == null) ? "NULL" : "No IPEGI").write();

                return false;
            }

            return target.GetNameForInspector().enter_Inspect(IPEGI, ref entered, thisOne);
        }

        public static bool enter_Inspect_AsList(this IPEGI_ListInspect var, ref int entered, int thisOne, string exitLabel = null)
        {
            if (!EnterOptionsDrawn_Internal(ref entered, thisOne))
                return false;

            var changed = ChangeTrackStart();

            var outside = entered == -1;

            if (!var.IsNullOrDestroyed_Obj())
            {

                if (outside) 
                    var.InspectInList(thisOne, ref entered);
                else if (entered == thisOne)
                {

                    if (exitLabel.IsNullOrEmpty())
                        exitLabel = var.GetNameForInspector();

                    if (icon.Exit.ClickUnFocus("{0} L {1}".F(icon.Exit.GetText(), var))
                        || exitLabel.ClickLabel(icon.Exit.GetDescription(), style: PEGI_Styles.ExitLabel))
                        entered = -1;
                    Try_Nested_Inspect(var);
                }
            }
            else if (entered == thisOne)
                entered = -1;


            ef.isFoldedOutOrEntered = entered == thisOne;

            return changed;
        }

        public static bool isConditionally_Entered(bool canEnter, ref int entered, int thisOne, string exitLabel = "")
        {
            if (!EnterOptionsDrawn_Internal(ref entered, thisOne))
                return false;

            if (!canEnter && entered == thisOne)
                entered = -1;

            if (canEnter)
            {
                icon.Enter.isEntered(ref entered, thisOne);
                if (entered == thisOne && !exitLabel.IsNullOrEmpty() &&
                    exitLabel.ClickLabel(icon.Exit.GetDescription(), style: PEGI_Styles.ExitLabel))
                    entered = -1;
            }
            else
                ef.isFoldedOutOrEntered = false;

            return ef.isFoldedOutOrEntered;
        }

        public static bool isConditionally_Entered(this string label, bool canEnter, ref int entered, int thisOne, bool showLabelIfTrue = true, PEGI_Styles.PegiGuiStyle enterLabelStyle = null)
        {
            if (!EnterOptionsDrawn_Internal(ref entered, thisOne))
                return false;

            if (!canEnter && entered == thisOne)
            {
                if (icon.Back.Click() || "All Done here".ClickText(14))
                    entered = -1;
            }
            else
            {
                if (canEnter)
                    label.isEntered(ref entered, thisOne, showLabelIfTrue, enterLabelStyle);
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

        public static bool isToggle_Entered(this string label, ref bool val, ref int entered, int thisOne, bool showLabelWhenEntered = false)
        {
            if (!EnterOptionsDrawn_Internal(ref entered, thisOne))
                return false;

            if (entered == -1)
                label.toggleIcon(ref val);

            if (val)
                isEntered(ref entered, thisOne);
            else
                ef.isFoldedOutOrEntered = false;

            if (entered == thisOne)
                label.toggleIcon(ref val);

            if (!val && entered == thisOne)
                entered = -1;

            return ef.isFoldedOutOrEntered;
        }

        public static bool enter_List<T>(this ListMetaData meta, List<T> list, ref int entered, int thisOne)
        {
            if (!EnterOptionsDrawn_Internal(ref entered, thisOne))
                return false;

            if (meta.isEntered_HeaderPart(list, ref entered, thisOne))
                return meta.edit_List(list).nl();

            return false;
        }

        public static bool enter_List_UObj<T>(this string label, List<T> list, ref int entered, int thisOne, List<T> selectFrom = null) where T : Object
        {
            if (!EnterOptionsDrawn_Internal(ref entered, thisOne))
                return false;

            var insp = -1;
            if (isEntered_ListIcon(label, list, ref insp, ref entered, thisOne)) 
                return label.edit_List_UObj(list, selectFrom).nl();

            return false;
        }

        public static bool enter_List<T>(this string label, List<T> list, ref int inspectedElement, ref int enteredOne, int thisOne) =>
            enter_List(label, list, ref inspectedElement, ref enteredOne, thisOne, out _);

        public static bool enter_List<T>(this string label, List<T> list, ref int inspectedElement, ref int entered, int thisOne, out T added)
        {
            added = default(T);

            if (!EnterOptionsDrawn_Internal(ref entered, thisOne))
                return false;

            var changes = ChangeTrackStart();

            if (entered == -1 && list != null && list.Count == 0 && typeof(Object).IsAssignableFrom(typeof(T)) == false)
            {
                var showedOption = collectionInspector.TryShowListAddNewOption(label, list, ref added);

                if (!showedOption)
                    showedOption = collectionInspector.TryShowListCreateNewOptions(list, ref added, null);

                if (showedOption)
                {
                    return changes;
                }
            }

            if (isEntered_ListIcon(label, list, ref inspectedElement, ref entered, thisOne)) //if (label.AddCount(list).enter(ref enteredOne, thisOne))
                label.edit_List(list, ref inspectedElement, out added);

            return changes;
        }

        public static bool enter_List<T>(this string label, List<T> list, ref int inspectedElement, ref bool entered)
        {

            var changed = ChangeTrackStart();

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

        public static bool enter_List<T>(this ListMetaData meta, List<T> list, ref int entered, int thisOne, TaggedTypesCfg types, out T added)
        {
            added = default;

            if (!EnterOptionsDrawn_Internal(ref entered, thisOne))
                return false;

            if (meta.isEntered_HeaderPart(list, ref entered, thisOne))
                return meta.edit_List(list, types, out added);

          
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

        public static bool conditional_enter_List<T>(this string label, bool canEnter, List<T> list, ref int inspectedElement, ref int entered, int thisOne)
        {
            if (!EnterOptionsDrawn_Internal(ref entered, thisOne))
                return false;

            var changed = ChangeTrackStart();

            if (!canEnter && entered == thisOne)
                entered = -1;

            if (canEnter)
                label.enter_List(list, ref inspectedElement, ref entered, thisOne);
            else
                ef.isFoldedOutOrEntered = false;

            return changed;

        }

        public static bool conditional_enter_List<T>(this ListMetaData meta, bool canEnter, List<T> list, ref int entered, int thisOne)
        {
            if (!EnterOptionsDrawn_Internal(ref entered, thisOne))
                return false;

            var changed = ChangeTrackStart(); ;

            if (!canEnter && entered == thisOne)
                entered = -1;

            if (canEnter)
                meta.enter_List(list, ref entered, thisOne);
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