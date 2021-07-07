using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QuizCanners.Utils;
using UnityEngine;
using Object = UnityEngine.Object;
using QuizCanners.Migration;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0011 // Add braces
#pragma warning disable IDE0008 // Use explicit type

namespace QuizCanners.Inspect
{
    public static partial class pegi
    {
        #region Collection MGMT Functions 

        public static int InspectedIndex => collectionInspector.Index;

        public static string CurrentListLabel<T>(CollectionMetaData meta = null) => collectionInspector.GetCurrentListLabel<T>(meta);

        internal static readonly CollectionInspector collectionInspector = new CollectionInspector();

   
        internal static bool InspectValueInDictionary<K, T>(KeyValuePair<K,T> pair, Dictionary<K, T> dic, int index, ref int inspected, CollectionMetaData listMeta = null)
        {
            var el = pair.Value;

            var changed = InspectValueInCollection(ref el, index, ref inspected, listMeta);

            if (changed && typeof(T).IsValueType)
            {
                dic[pair.Key] = el;
            }

            return changed;
        }

        internal static bool InspectValueInArray<T>(ref T[] array, int index, ref int inspected, CollectionMetaData listMeta = null)
        {
            T el = array[index];

            var changed = InspectValueInCollection(ref el, index, ref inspected, listMeta);

            if (changed)
                array[index] = el;

            return changed;
        }

        internal static bool InspectValueInList<T>(T el, List<T> list, int index, ref int inspected,
            CollectionMetaData listMeta = null)
        {

            var changed = InspectValueInCollection(ref el, index, ref inspected, listMeta);

            if (changed && typeof(T).IsValueType)
                list[index] = el;

            return changed;

        }

        public static bool InspectValueInCollection<T>(ref T el, int index, ref int inspected, CollectionMetaData listMeta = null)
        {

            var changed = ChangeTrackStart();

            var isPrevious = (listMeta != null && listMeta.previouslyInspectedElement == index);

            if (isPrevious)
                SetBgColor(PreviousInspectedColor);

            if (el.IsNullOrDestroyed_Obj())
            {
                var ed = listMeta?[index];
                if (ed == null)
                {
                    "{0}: NULL {1}".F(index, typeof(T).ToPegiStringType()).write(150);

                }
                else
                {
                    object obj = el;

                    if (ed.PEGI_inList<T>(ref obj))
                    {
                        el = (T)obj;
                        isPrevious = true;
                    }
                }
            }
            else
            {
                var pl = el as IPEGI_ListInspect;

                if (pl != null)
                {
                    try
                    {
                        pl.InspectInList(ref inspected, index);
                    }
                    catch (System.Exception ex)
                    {
                        write(ex);
                    }

                    if (changed && (typeof(T).IsValueType))
                        el = (T)pl;

                    if (changed || inspected == index)
                        isPrevious = true;

                }
                else
                {
                    var uo = el as Object;

                    var pg = el as IPEGI;

                    var need = el as INeedAttention;
                    var warningText = need?.NeedAttention();

                    if (warningText != null)
                        SetBgColor(AttentionColor);

                    var clickHighlightHandled = false;

                    bool isShown = false;

                    if (el is Object)
                    {
                        isShown = true;

                        if (edit(ref uo, typeof(T), 200))
                            el = (T)(object)uo;
                    }

                    var named = el as IGotName;
                    if (named != null)
                    {
                        var n = named.NameForInspector;
                        if (edit(ref n))
                        {
                            named.NameForInspector = n;
                            if (typeof(T).IsValueType)
                                el = (T)named;

                            isPrevious = true;
                        }

                        var sb = new System.Text.StringBuilder();

                        var iind = el as IGotIndex;
                        if (iind != null)
                            sb.Append(iind.IndexForInspector.ToString() + ": ");

                        var count = el as IGotCount;
                        if (count != null)
                            sb.Append("[x{0}] ".F(count.GetCount()));
                        
                        var label = sb.ToString();

                        if (label.Length > 0)
                            label.write(70);
                    }
                    else
                    {
                        if (!uo && pg == null && listMeta == null)
                        {
                            if (!isShown && el.GetNameForInspector().ClickLabel(Msg.InspectElement.GetText(), RemainingLength(otherElements: defaultButtonSize * 2 + 10)))
                            {
                                inspected = index;
                                isPrevious = true;
                            }
                        }
                        else
                        {

                            if (uo)
                            {
                                Texture tex = uo as Texture;

                                if (tex)
                                {
                                    if (uo.ClickHighlight(tex))
                                        isPrevious = true;

                                    clickHighlightHandled = true;
                                }
                            }
                            else if (el.GetNameForInspector().ClickLabel("Inspect", RemainingLength(defaultButtonSize * 2 + 10)))
                            {
                                inspected = index;
                                isPrevious = true;
                            }
                        }

                    }

                    if ((warningText == null &&
                            icon.Enter.ClickUnFocus(Msg.InspectElement)) ||
                        (warningText != null && icon.Warning.ClickUnFocus(warningText)))
                    {
                        inspected = index;
                        isPrevious = true;
                    }

                    if (!clickHighlightHandled && uo.ClickHighlight())
                        isPrevious = true;

                    if (listMeta != null && listMeta[CollectionInspectParams.showCopyPasteOptions])
                        CopyPaste.InspectOptionsFor(ref el);
                }
            }
            

            RestoreBGColor();

            if (listMeta != null)
            {
                if (listMeta.inspectedElement != -1)
                    listMeta.previouslyInspectedElement = listMeta.inspectedElement;
                else if (isPrevious)
                    listMeta.previouslyInspectedElement = index;

            }
            else if (isPrevious)
                collectionInspector.previouslyEntered = el;

            return changed;
        }

        #endregion
        
        #region LISTS

        #region List of Unity Objects

        public static bool edit_List_UObj<T>(this string label, List<T> list, ref int inspected) where T : Object
        {
            using (collectionInspector.Write_Search_ListLabel(label, ref inspected, list))
            {
                return edit_or_select_List_UObj(list, ref inspected);
            }
        }

        public static bool edit_List_UObj<T>(this string label, List<T> list) where T : Object
        {
            using (collectionInspector.Write_Search_ListLabel(label, list))
            {
                return list.edit_List_UObj();
            }
        }

        public static bool edit_List_UObj<T>(this List<T> list) where T : Object
        {
            var edited = -1;
            return edit_or_select_List_UObj(list, ref edited);
        }

        public static bool edit_List_UObj<T>(List<T> list, System.Func<T, T> lambda) where T : Object
        {
            var changed = ChangeTrackStart();

            if (collectionInspector.CollectionIsNull(list))
                return changed;

            collectionInspector.Edit_List_Order(list);

            if (list != collectionInspector.reordering)
            {
                collectionInspector.ListAddEmptyClick(list);

                foreach (var el in collectionInspector.InspectionIndexes(list))
                {
                    int i = collectionInspector.Index;
                    var ch = GUI.changed;

                    var tmpEl = lambda(el);
                    nl();
                    if (ch || !GUI.changed) 
                        continue;

                    changed.Changed = true;
                    list[i] = tmpEl;
                }
            }
            nl();
            return changed;
        }

        public static bool edit_or_select_List_UObj<T>(List<T> list, ref int inspected, CollectionMetaData listMeta = null) where T : Object
        {
            var changed = ChangeTrackStart();

            if (collectionInspector.CollectionIsNull(list))
                return false;

            var before = inspected;
            if (list.ClampIndexToCount(ref inspected, -1))
                changed.Changed |= (inspected != before);

            if (inspected == -1)
            {

                collectionInspector.Edit_List_Order(list, listMeta);

                if (list != collectionInspector.reordering)
                {
                    collectionInspector.ListAddEmptyClick(list, listMeta);

                    foreach (var el in collectionInspector.InspectionIndexes(list, listMeta))
                    {
                        var i = collectionInspector.Index;

                        if (!el)
                        {
                            var elTmp = el;

                            if (edit(ref elTmp))
                                list[i] = elTmp;
                        }
                        else
                            collectionInspector.InspectClassInList(list, i, ref inspected, listMeta);

                        nl();
                    }

                    CopyPaste.InspectOptions<T>(listMeta);

                }
                else
                    collectionInspector.List_DragAndDropOptions(list, listMeta);

            }
            else collectionInspector.ExitOrDrawPEGI(list, ref inspected);

            nl();
            return changed;

        }

        #endregion

        #region List

        public static bool edit_List<T>(this string label, List<T> list, ref int inspected)
        {
            using (collectionInspector.Write_Search_ListLabel(label, ref inspected, list))
            {
                return edit_List(list, ref inspected);
            }
        }

        public static bool edit_List<T>(List<T> list, ref int inspected) => edit_List(list, ref inspected, out _);
        
        public static bool edit_List<T>(this string label, List<T> list)
        {
            using (collectionInspector.Write_Search_ListLabel(label, list))
            {
                return edit_List(list);
            }
        }

        public static bool edit_List<T>(List<T> list)
        {
            var edited = -1;
            return edit_List(list, ref edited);
        }

        public static bool edit_List<T>(this string label, List<T> list, out T added)
        {
            using (collectionInspector.Write_Search_ListLabel(label, list))
            {
                return edit_List(list, out added);
            }
        }

        public static bool edit_List<T>(List<T> list, out T added)
        {
            var edited = -1;
            return edit_List(list, ref edited, out added);
        }

        public static bool edit_List<T>(this string label, List<T> list, ref int inspected, out T added)
        {
            using (collectionInspector.Write_Search_ListLabel(label, ref inspected, list))
            {
                return edit_List(list, ref inspected, out added);
            }
        }

        public static bool edit_List<T>(this CollectionMetaData listMeta, List<T> list)
        {
            var changed = ChangeTrackStart();
            using (collectionInspector.Write_Search_ListLabel(listMeta, list)) 
            {
                edit_List(list, ref listMeta.inspectedElement, out _, listMeta);
            }
            collectionInspector.End();
            return changed;
        }

        public static bool edit_List<T>(this CollectionMetaData listMeta, List<T> list, out T added)
        {
            using (collectionInspector.Write_Search_ListLabel(listMeta, list))
            {
                return edit_List(list, ref listMeta.inspectedElement, out added, listMeta);
            }
        }

        public static bool edit_List<T>(List<T> list, ref int inspected, out T added, CollectionMetaData listMeta = null)
        {
            var changes = ChangeTrackStart();

            added = default;

            if (list == null)
            {
                "List of {0} is null".F(typeof(T).ToPegiStringType()).write();

                    return changes;
            }

            if (inspected >= list.Count)
            {
                inspected = -1;
                changes.Changed = true;
            }

            if (inspected == -1)
            {

                collectionInspector.Edit_List_Order(list, listMeta);

                if (list != collectionInspector.reordering)
                {

                    collectionInspector.TryShowListAddNewOption(list, ref added, listMeta);

                    foreach (var el in collectionInspector.InspectionIndexes(list, listMeta))
                    {
                        int i = collectionInspector.Index;

                        if (el.IsNullOrDestroyed_Obj())
                        {
                            if (!collectionInspector.IsMonoType(list, i))
                            {
                                write(typeof(T).IsSubclassOf(typeof(Object))
                                    ? "use edit_List_UObj"
                                    : "is NUll");
                            }
                        }
                        else
                        {
                            InspectValueInList(el, list, i, ref inspected, listMeta);
                        }

                        nl();
                    }

                    collectionInspector.TryShowListCreateNewOptions(list, ref added, listMeta);

                    CopyPaste.InspectOptions<T>(listMeta);
                }
            }
            else collectionInspector.ExitOrDrawPEGI(list, ref inspected);

            nl();
            return changes;
        }

        #region Tagged Types

        public static bool edit_List<T>(this CollectionMetaData listMeta, List<T> list, TaggedTypesCfg types, out T added)
        {
            using (collectionInspector.Write_Search_ListLabel(listMeta, list))
            {
                return edit_List(list, ref listMeta.inspectedElement, types, out added, listMeta);
            }
        }

        public static bool edit_List<T>(this CollectionMetaData listMeta, List<T> list, TaggedTypesCfg types)
        {
            using (collectionInspector.Write_Search_ListLabel(listMeta, list))
            {
                return edit_List(list, ref listMeta.inspectedElement, types, out _, listMeta);
            }
        }

        public static bool edit_List<T>(this string label, List<T> list, ref int inspected, TaggedTypesCfg types, out T added)
        {
            using (collectionInspector.Write_Search_ListLabel(label, ref inspected, list))
            {
                return edit_List(list, ref inspected, types, out added);
            }
        }

        private static bool edit_List<T>(List<T> list, ref int inspected, TaggedTypesCfg types, out T added, CollectionMetaData listMeta = null)
        {
            var changes = ChangeTrackStart();

            added = default;

            if (list == null)
            {
                "List of {0} is null".F(typeof(T)).write();

                 return changes;
            }

            if (inspected >= list.Count)
            {
                inspected = -1;
                changes.Changed = true;
            }

            if (inspected == -1)
            {
                collectionInspector.Edit_List_Order(list, listMeta);

                if (list != collectionInspector.reordering)
                {
                    foreach (var el in collectionInspector.InspectionIndexes(list, listMeta))
                    {
                        int i = collectionInspector.Index;

                        if (el == null)
                        {

                            if (!collectionInspector.IsMonoType(list, i))
                            {
                                write(typeof(T).IsSubclassOf(typeof(Object))
                                    ? "use edit_List_UObj"
                                    : "is NUll");
                            }
                        }
                        else
                        {
                            InspectValueInList(el, list, i, ref inspected, listMeta);
                        }
                        nl();
                    }

                    collectionInspector.TryShowListCreateNewOptions(list, ref added, types, listMeta).nl();

                    CopyPaste.InspectOptions<T>(listMeta);
                }
            }
            else collectionInspector.ExitOrDrawPEGI(list, ref inspected);

            nl();
            return changes;
        }

        #endregion

        #endregion

        #region List by Lambda 

        #region SpecialLambdas

        private static IList listElementsRoles;

        private static Color lambda_Color(Color val)
        {
            edit(ref val);
            return val;
        }

        private static Color32 lambda_Color(Color32 val)
        {
            edit(ref val);
            return val;
        }

        private static int lambda_int(int val)
        {
            edit(ref val);
            return val;
        }

        private static string lambda_string_role(string val)
        {
            var role = listElementsRoles.TryGetObj(collectionInspector.Index);
            if (role != null)
                role.GetNameForInspector().edit(90, ref val);
            else edit(ref val);

            return val;
        }

        public static string lambda_string(string val)
        {
            edit(ref val);
            return val;
        }

        public static bool edit_List(this string label, List<int> list) =>
            label.edit_List(list, lambda_int);

        public static bool edit_List(this string label, List<Color> list) =>
            label.edit_List(list, lambda_Color);

        public static bool edit_List(this string label, List<Color32> list) =>
            label.edit_List(list, lambda_Color);

        public static bool edit_List(this string label, List<string> list) =>
            label.edit_List(list, lambda_string);
        #endregion

        public static bool edit_List<T>(this string label, List<T> list, System.Func<T, T> lambda) 
        {
            using (collectionInspector.Write_Search_ListLabel(label, list))
            {
                return edit_List(list, lambda, out _);
            }
        }

        public static bool edit_List<T>(this string label, List<T> list, System.Func<T, T> lambda, out T added) 
        {
            using (collectionInspector.Write_Search_ListLabel(label, list))
            {
                return edit_List(list, lambda, out added);
            }
        }

        public static bool edit_List<T>(List<T> list, System.Func<T, T> lambda, out T added)
        {
            var changed = ChangeTrackStart();

            added = default;

            if (collectionInspector.CollectionIsNull(list))
                return changed;

            collectionInspector.Edit_List_Order(list);

            if (list != collectionInspector.reordering)
            {
                collectionInspector.TryShowListAddNewOption(list, ref added);

                foreach (var el in collectionInspector.InspectionIndexes(list))
                {
                    int i = collectionInspector.Index;

                    var ch = GUI.changed;
                    var tmpEl = lambda(el);
                    if (!ch && GUI.changed)
                    {
                        list[i] = tmpEl;
                    }
                    nl();
                }
            }
            nl();
            return changed;
        }

        public static bool edit_List(this string name, List<string> list, System.Func<string, string> lambda)
        {
            using (collectionInspector.Write_Search_ListLabel(name, list))
            {
                return edit_List(list, lambda);
            }
        }

        public static bool edit_List(List<string> list, System.Func<string, string> lambda)
        {
            var changed = ChangeTrackStart();

            if (collectionInspector.CollectionIsNull(list))
                return false;

            collectionInspector.Edit_List_Order(list);

            if (list != collectionInspector.reordering)
            {
                if (icon.Add.ClickUnFocus())
                {
                    list.Add("");
                    collectionInspector.SkrollToBottom();
                }

                foreach (var el in collectionInspector.InspectionIndexes(list))
                {
                    int i = collectionInspector.Index;

                    var ch = GUI.changed;
                    var tmpEl = lambda(el);
                    nl();
                    if (ch || !GUI.changed) continue;

                    changed.Changed = true;
                    list[i] = tmpEl;
                }

            }

            nl();
            return changed;
        }

        #endregion

        #endregion

        #region Dictionary Generic
        internal interface iCollectionInspector<T>
        {
            void Set(T val);
        }

        internal class KeyValuePairInspector<T,G> : iCollectionInspector<KeyValuePair<T,G>>, IGotReadOnlyName, ISearchable, INeedAttention
        {
            private KeyValuePair<T, G> _pair;

            public void Set(KeyValuePair<T, G> pair)
            {
                _pair = pair;
            }
          
            public string GetNameForInspector()
            {
                return _pair.Value == null ? _pair.Key.GetNameForInspector() : _pair.Value.GetNameForInspector();
            }

            public bool IsContainsSearchWord(string searchString)
            {
                return Try_SearchMatch_Obj(_pair.Value, searchString) || Try_SearchMatch_Obj(_pair.Key, searchString);
            }

            public string NeedAttention()
            {

                string msg;// = null;

                if (NeedsAttention(_pair.Value, out msg))
                    "{0} at {1}".F(msg, _pair.Key.GetNameForInspector());

                return msg;
               
            }
        }

        private static void WriteNullDictionary_Internal<T>() => "NULL {0} Dictionary".write();
        

        private static int _tmpKeyInt;
        private static string _tmpKeyString = "";
        public static bool addDictionaryPairOptions<TValue>(Dictionary<int, TValue> dic) 
        {
            var changed = ChangeTrackStart();
            if (dic == null)
            {
                WriteNullDictionary_Internal<TValue>();
                return changed;
            }

            "Key".edit(60, ref _tmpKeyInt);

            if (dic.ContainsKey(_tmpKeyInt))
            {
                if (icon.Refresh.Click("Find Free index"))
                {
                    while (dic.ContainsKey(_tmpKeyInt))
                        _tmpKeyInt++;
                }
                "Key {0} already exists".F(_tmpKeyInt).writeWarning();
            }
            else
            {
                if (icon.Add.Click("Add new Value"))
                {
                    dic.Add(_tmpKeyInt, System.Activator.CreateInstance<TValue>());
                    while (dic.ContainsKey(_tmpKeyInt))
                        _tmpKeyInt++;
                }
            }

            nl();

            return changed;

        }

        public static bool addDictionaryPairOptions<TValue>(Dictionary<string, TValue> dic, string newElementName = "") 
        {
            var changed = ChangeTrackStart();
            if (dic == null)
            {
                WriteNullDictionary_Internal<TValue>();
                return changed;
            }

            "Key".edit(60, ref _tmpKeyString);

            if (_tmpKeyString.Length > 0 && icon.Refresh.Click())
                _tmpKeyString = newElementName.Length > 0 ? newElementName : "New Element";

            if (dic.ContainsKey(_tmpKeyString))
            {
                nl();
                "Key {0} already exists".F(_tmpKeyString).writeWarning();
            }
            else
            {
                if (icon.Add.Click("Add new Value"))
                {
                    var value = System.Activator.CreateInstance<TValue>();
                    dic.Add(_tmpKeyString, value);
                    var name = value as IGotName;
                    if (name != null)
                        name.NameForInspector = _tmpKeyString;

                    _tmpKeyString = newElementName;
                }

            }

            nl();

            return changed;
        }

        public static bool edit_Dictionary<TKey, TValue>(Dictionary<TKey, TValue> dic, bool showKey = true)
        {
            int inspected = -1;
            using (collectionInspector.Write_Search_DictionaryLabel(dic.ToString(), ref inspected, dic))
            {
                return edit_Dictionary_Internal(dic, ref inspected, showKey: showKey);
            }
        }

        public static bool edit_Dictionary<TKey, TValue>(Dictionary<TKey, TValue> dic, ref int inspected, bool showKey = true)
        {
            using (collectionInspector.Write_Search_DictionaryLabel(dic.ToString(), ref inspected, dic))
            {
                return edit_Dictionary_Internal(dic, ref inspected, showKey: showKey);
            }
        }

        public static bool edit_Dictionary<TKey, TValue>(this string label, Dictionary<TKey, TValue> dic, bool showKey = true)
        {
            int inspected = -1;
            using (collectionInspector.Write_Search_DictionaryLabel(label, ref inspected, dic))
            {
                return edit_Dictionary_Internal(dic, ref inspected, showKey: showKey);
            }
        }

        public static bool edit_Dictionary<TKey, TValue>(this string label, Dictionary<TKey, TValue> dic, ref int inspected, bool showKey = true)
        {
            using (collectionInspector.Write_Search_DictionaryLabel(label, ref inspected, dic))
            {
                return edit_Dictionary_Internal(dic, ref inspected, showKey: showKey);
            }
        }

        public static bool edit_Dictionary<TKey, TValue>(this string label, Dictionary<TKey, TValue> dic, System.Func<TValue, TValue> lambda, bool showKey = false)
        {
            int inspected = -1;
            using (collectionInspector.Write_Search_DictionaryLabel(label, ref inspected, dic))
            {
                return edit_Dictionary_Internal(dic, lambda, showKey: showKey);
            }
        }

        public static bool edit_Dictionary<G, T>(this CollectionMetaData listMeta, Dictionary<G, T> dic)
        {
            using (collectionInspector.Write_Search_DictionaryLabel(listMeta, dic))
            {
                return edit_Dictionary_Internal(dic, ref listMeta.inspectedElement, showKey: listMeta[CollectionInspectParams.showDictionaryKey], listMeta: listMeta);
            }
        }

        public static bool edit_Dictionary<TKey, TValue>(this CollectionMetaData listMeta, Dictionary<TKey, TValue> dic, System.Func<TValue, TValue> lambda)
        {
            using (collectionInspector.Write_Search_DictionaryLabel(listMeta, dic))
            {
                return edit_Dictionary_Internal(dic, lambda, listMeta: listMeta);
            }
        }

        public static bool edit_Dictionary(this CollectionMetaData listMeta, Dictionary<string, string> dic)
        {
            using (collectionInspector.Write_Search_DictionaryLabel(listMeta, dic))
            {
                return edit_Dictionary_Internal(dic, lambda_string, listMeta: listMeta);
            }
        }
        
        public static bool edit_Dictionary(this string label, Dictionary<string, string> dic)
        {
            int inspected = -1;
            using (collectionInspector.Write_Search_DictionaryLabel(label, ref inspected, dic))
            {
                return edit_Dictionary_Internal(dic, lambda_string);
            }
        }

        public static bool edit_Dictionary(this string label, Dictionary<int, string> dic, List<string> roles)
        {
            int inspected = -1;
            using (collectionInspector.Write_Search_DictionaryLabel(label, ref inspected, dic))
            {
                listElementsRoles = roles;
                var changes = edit_Dictionary_Internal(dic, lambda_string_role, false);
                listElementsRoles = null;
                return changes;
            }
        }

        internal static bool edit_Dictionary_Internal<TKey, TValue>(Dictionary<TKey, TValue> dic, System.Func<TValue, TValue> lambda, bool showKey = true, CollectionMetaData listMeta = null)
        {

            if (dic == null)
            {
                WriteNullDictionary_Internal<TValue>();
                return false;
            }

            nl();

            if (listMeta != null)
                showKey = listMeta[CollectionInspectParams.showDictionaryKey];

            var changed = false;

            if (listMeta != null && listMeta.IsInspectingElement)
            {

                if (icon.Exit.Click("Exit " + listMeta.label))
                    listMeta.IsInspectingElement = false;

                if (listMeta.IsInspectingElement && (dic.Count > listMeta.inspectedElement))
                {
                    var el = dic.GetElementAt(listMeta.inspectedElement);

                    var val = el.Value;

                    var ch = GUI.changed;

                    Try_Nested_Inspect(val);

                    if ((!ch && GUI.changed).changes_Internal(ref changed))
                        dic[el.Key] = val;
                }
            }
            else
            {
                foreach (var item in collectionInspector.InspectionIndexes(dic, listMeta, new KeyValuePairInspector<TKey, TValue>()))
                {
                    var itemKey = item.Key;
                    
                    if ((listMeta != null && listMeta[CollectionInspectParams.allowDeleting]) && icon.Delete.ClickUnFocus(25).changes_Internal(ref changed))
                        dic.Remove(itemKey);
                    else
                    {
                        if (showKey)
                            itemKey.GetNameForInspector().write_ForCopy(50);

                        var el = item.Value;
                        var ch = GUI.changed;
                        el = lambda(el);

                        if ((!ch && GUI.changed).changes_Internal(ref changed))
                        {
                            dic[itemKey] = el;
                            break;
                        }

                        if (listMeta != null && icon.Enter.Click("Enter " + el))
                            listMeta.inspectedElement = collectionInspector.Index;
                    }
                    nl();
                }
            }
            return changed;
        }

        internal static bool edit_Dictionary_Internal<TKey, TValue>(Dictionary<TKey, TValue> dic, ref int inspected, bool showKey, CollectionMetaData listMeta = null)
        {
            bool changed = false;

            nl();

            if (dic == null)
            {
                WriteNullDictionary_Internal<TValue>();
                return false;
            }

            int before = inspected;
            inspected = Mathf.Clamp(inspected, -1, dic.Count - 1);
            changed |= (inspected != before);
            
            if (inspected == -1)
            {

                string keyToReplace = null;
                string keyToReplaceWith = null;

                if (listMeta != null)
                    showKey = listMeta[CollectionInspectParams.showDictionaryKey];

                foreach (var item in collectionInspector.InspectionIndexes(dic, listMeta, new KeyValuePairInspector<TKey, TValue>()))
                {
                    var itemKey = item.Key;
                    
                    if ((listMeta != null && listMeta[CollectionInspectParams.allowDeleting]) 
                        && icon.Delete.ClickConfirm(confirmationTag: "DelDicEl"+collectionInspector.Index).changes_Internal(ref changed))
                    {
                        dic.Remove(itemKey);
                        return true;
                    }
                    else
                    {
                        if (showKey)
                        {
                            bool keyHandled = false;

                            var strKey = itemKey as string;
                            if (strKey!= null)
                            {
                                var name = item.Value as IGotName;
                                if (name != null)
                                {
                                    keyHandled = true;

                                    var theName = name.NameForInspector;

                                    if (!theName.Equals(strKey))
                                    {
                                        var strDic = dic as Dictionary<string, TValue>;

                                        if (strDic.ContainsKey(theName))
                                            "Name exists as Key".write(90);
                                        else
                                        {
                                            if ("Key<-".ClickUnFocus("Override Key with Name"))
                                            {
                                                keyToReplace = strKey;
                                                keyToReplaceWith = theName;
                                            }

                                            if ("->Name".ClickUnFocus("Override Name with Key"))
                                                name.NameForInspector = strKey;
                                        }
                                    }
                                }
                            } 
                            
                            if (!keyHandled)
                                itemKey.GetNameForInspector().write_ForCopy(50);
                        }

                        InspectValueInDictionary(item, dic, collectionInspector.Index, ref inspected, listMeta).changes_Internal(ref changed);
                    }
                    nl();
                }

                if (keyToReplace != null)
                {
                    var strDic = dic as Dictionary<string, TValue>;
                    var tmpVal = strDic[keyToReplace];
                    strDic.Remove(keyToReplace);
                    strDic.Add(keyToReplaceWith, tmpVal);
                }

                if ((listMeta != null && listMeta[CollectionInspectParams.showAddButton]) && typeof(TKey).Equals(typeof(string)))
                {
                    var stringDick = dic as Dictionary<string, TValue>;
                    addDictionaryPairOptions(stringDick, newElementName: "New " + (listMeta == null ? CurrentListLabel<TValue>() : listMeta.label));
                }

            }
            else
                collectionInspector.ExitOrDrawPEGI(dic, ref inspected).changes_Internal(ref changed);

            nl();
            return changed;
        }
        
        #endregion

        #region Arrays

        public static bool edit_Array<T>(this string label, ref T[] array)
        {
            int inspected = -1;
            using (collectionInspector.Write_Search_ListLabel(label, array))
            {
                return edit_Array(ref array, ref inspected);
            }
        }

        public static bool edit_Array<T>(this string label, ref T[] array, ref int inspected)
        {
            using (collectionInspector.Write_Search_ListLabel(label, ref inspected, array))
            {
                return edit_Array(ref array, ref inspected);
            }
        }

        public static bool edit_Array<T>(ref T[] array, ref int inspected)
        {
            bool changes = false;
            edit_Array(ref array, ref inspected, ref changes);
            return changes;
        }

        public static T edit_Array<T>(ref T[] array, ref int inspected, ref bool changed, CollectionMetaData metaDatas = null)
        {
            nl();

            var added = default(T);

            if (array == null)
            {
                if (Msg.Init.F(Msg.Array).ClickUnFocus().nl())
                    array = new T[0];
            }
            else
            {

                collectionInspector.ExitOrDrawPEGI(array, ref inspected).changes_Internal(ref changed);

                if (inspected != -1) return added;

                if (!typeof(T).IsNew())
                {
                    if (icon.Add.ClickUnFocus(Msg.AddEmptyCollectionElement))
                    {
                        array = array.ExpandBy(1);
                        collectionInspector.SkrollToBottom();
                    }
                }
                else if (icon.Create.ClickUnFocus(Msg.AddNewCollectionElement))
                    QcSharp.AddAndInit(ref array, 1);

                collectionInspector.Edit_Array_Order(ref array, metaDatas).nl(ref changed);

                if (array == collectionInspector._editingArrayOrder) return added;

                for (var i = 0; i < array.Length; i++)
                {
                    //var el = array[i];
                    //if (
                    InspectValueInArray(ref array, i, ref inspected, metaDatas).nl(ref changed);// &&
                       // typeof(T).IsValueType)
                        //array[i] = el;
                }
            }

            collectionInspector.End();

            return added;
        }

        #endregion
        
        #region Searching

        public static bool SearchMatch_ObjectList(this IEnumerable list, string searchText) => list.Cast<object>().Any(e => Try_SearchMatch_Obj(e, searchText));

        public static bool Try_SearchMatch_Obj(object obj, string searchText) => SearchMatch_Obj_Internal(obj, new[] { searchText });

        private static bool SearchMatch_Obj_Internal(this object obj, string[] text, int[] indexes = null)
        {

            if (obj.IsNullOrDestroyed_Obj())
                return false;

            var go = QcUnity.TryGetGameObjectFromObj(obj);

            var matched = new bool[text.Length];

            if (go)
            {

                if (go.GetComponent<ISearchable>().SearchMatch_Internal(text, ref matched))
                    return true;

                if (go.GetComponent<IGotName>().SearchMatch_Internal(text, ref matched))
                    return true;

                if (go.GetComponent<IGotReadOnlyName>().SearchMatch_Internal(text, ref matched))
                    return true;

                if (go.name.SearchMatch_Internal(text, ref matched))
                    return true;

                if (go.GetComponent<INeedAttention>().SearchMatch_Internal(text, ref matched))
                    return true;

                if (!indexes.IsNullOrEmpty() && go.GetComponent<IGotIndex>().SearchMatch_Internal(indexes))
                    return true;

            }
            else
            {
                if ((QcUnity.TryGetInterfaceFrom<ISearchable>(obj)).SearchMatch_Internal(text, ref matched))
                    return true;

                if (QcUnity.TryGetInterfaceFrom<IGotName>(obj).SearchMatch_Internal(text, ref matched))
                    return true;

                if (QcUnity.TryGetInterfaceFrom<IGotReadOnlyName>(obj).SearchMatch_Internal(text, ref matched))
                    return true;

                if (QcUnity.TryGetInterfaceFrom<INeedAttention>(obj).SearchMatch_Internal(text, ref matched))
                    return true;

                if (obj.ToString().SearchMatch_Internal(text, ref matched))
                    return true;

                if (!indexes.IsNullOrEmpty() && go.GetComponent<IGotIndex>().SearchMatch_Internal(indexes))
                    return true;
            }

            return false;
        }

        private static bool SearchMatch_Internal(this ISearchable searchable, string[] text, ref bool[] matched)
        {
            if (searchable == null) return false;

            var fullMatch = true;

            for (var i = 0; i < text.Length; i++)
                if (!matched[i])
                {
                    if (!searchable.IsContainsSearchWord(text[i]))
                        fullMatch = false;
                    else
                        matched[i] = true;
                }

            return fullMatch;

        }

        private static bool SearchMatch_Internal(this INeedAttention needAttention, string[] text, ref bool[] matched)
            => needAttention?.NeedAttention().SearchMatch_Internal(text, ref matched) ?? false;

        private static bool SearchMatch_Internal(this IGotName gotName, string[] text, ref bool[] matched)
            => gotName?.NameForInspector.SearchMatch_Internal(text, ref matched) ?? false;

        private static bool SearchMatch_Internal(this IGotReadOnlyName gotDisplayName, string[] text, ref bool[] matched) =>
             gotDisplayName?.GetNameForInspector().SearchMatch_Internal(text, ref matched) ?? false;

        private static bool SearchMatch_Internal(this string label, string[] text, ref bool[] matched)
        {

            if (label.IsNullOrEmpty())
                return false;

            var fullMatch = true;

            for (var i = 0; i < text.Length; i++)
                if (!matched[i])
                {
                    if (!text[i].IsSubstringOf(label))
                        fullMatch = false;
                    else
                        matched[i] = true;
                }

            return fullMatch;

        }

        private static bool SearchMatch_Internal(this IGotIndex gotIndex, int[] indexes) => gotIndex != null && indexes.Any(t => gotIndex.IndexForInspector == t);

        private static readonly SearchData defaultSearchData = new SearchData();

        private static readonly char[] splitCharacters = { ' ', '.' };

        internal class SearchData 
        {
            private const string SEARCH_FIELD_FOCUS_NAME = "_pegiSearchField";


            public static bool UnityFocusNameWillWork; // Focus name bug on first focus
            public IEnumerable FilteredList;
            public string SearchedText;
            public int UncheckedElement;
            public int InspectionIndexStart;
            public bool FilterByNeedAttention;

            private string[] _searchBys;
            private readonly List<int> _filteredListElements = new List<int>();
            private int _fileredForCount = -1;
            private int _focusOnSearchBarIn;
       

            public List<int> GetFilteredList(int count)
            {
                if (_fileredForCount != count)
                {
                    OnCountChange(count);
                }

                return _filteredListElements;
            }

            public void CloseSearch()
            {
                FilteredList = null;
                false.UnFocusIfTrue();
            }

            public void ToggleSearch(IEnumerable collection, string label = "")
            {

                if (collection == null)
                    return;

                var active = ReferenceEquals(collection, FilteredList);

                var changed = false;

                if (active && icon.FoldedOut.ClickUnFocus("{0} {1} {2}".F(icon.Hide.GetText(), icon.Search.GetText(), collection), 27).changes_Internal(ref changed) || KeyCode.UpArrow.IsDown())
                    active = false;

                if (!active && !ReferenceEquals(collection, collectionInspector.reordering) &&
                    (icon.Search
                        .Click("{0} {1}".F(icon.Search.GetText(), label.IsNullOrEmpty() ? collection.ToString() : label), 27)) // || KeyCode.DownArrow.IsDown())
                        .changes_Internal(ref changed))
                {
                    active = true;
                    _focusOnSearchBarIn = 2;
                    FocusedName = SEARCH_FIELD_FOCUS_NAME;
                }

                if (active)
                {
                    icon.Warning.draw(toolTip: "Filter by warnings");
                    if (toggleIcon(ref FilterByNeedAttention))
                        Refresh();
                }

                if (!changed) return;

                FilteredList = active ? collection : null;

            }

            public void SearchString(IEnumerable list, out bool searching, out string[] searchBy)
            {
                searching = false;

                if (ReferenceEquals(list, FilteredList))
                {

                    nl();

                    icon.Search.draw();

                    NameNextForFocus(SEARCH_FIELD_FOCUS_NAME);

                    if (edit(ref SearchedText) || icon.Refresh.Click("Search again", 20).nl())
                    {
                        UnityFocusNameWillWork = true;
                        Refresh();
                        _searchBys = SearchedText.Split(splitCharacters, System.StringSplitOptions.RemoveEmptyEntries);
                    }

                    if (_focusOnSearchBarIn > 0)
                    {
                        _focusOnSearchBarIn--;
                        if (_focusOnSearchBarIn == 0)
                        {
                            FocusedName = SEARCH_FIELD_FOCUS_NAME;
                            RepaintEditor();
                        }
                    }

                    searching = FilterByNeedAttention || !_searchBys.IsNullOrEmpty();
                }

                searchBy = _searchBys;
            }

            private void OnCountChange(int newCount = -1)
            {
                _fileredForCount = newCount;
                _filteredListElements.Clear();
                UncheckedElement = 0;
                InspectionIndexStart = Mathf.Max(0, Mathf.Min(InspectionIndexStart, newCount - 1));
            }
        
            public void Refresh()=> OnCountChange();

        }

        #endregion
        
    }
}
