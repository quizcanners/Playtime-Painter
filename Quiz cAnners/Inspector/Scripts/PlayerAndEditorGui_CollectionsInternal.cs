using QuizCanners.Migration;
using QuizCanners.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Enm = System.Linq.Enumerable;

namespace QuizCanners.Inspect
{
    public static partial class pegi
    {
        internal class CollectionInspector : System.IDisposable
        {
            private const int SCROLL_ARROWS_WIDTH = 190;
            private const int SCROLL_ARROWS_HEIGHT = 20;

            public int Index { get; set; } = -1;
            public IList reordering;
            public string currentListLabel = "";
            public System.Array _editingArrayOrder;
            public readonly CountlessBool selectedEls = new CountlessBool();
            public object previouslyEntered;

            private readonly Dictionary<IEnumerable, int> Indexes = new Dictionary<IEnumerable, int>();
            private bool _searching;
            private List<int> filteredList;
            private int _sectionSizeOptimal;
            private int _count;
            private List<int> _copiedElements = new List<int>();
            private bool cutPaste;
            private readonly CountlessInt SectionOptimal = new CountlessInt();
            private static IList addingNewOptionsInspected;
            private string addingNewNameHolder = "Name";
            private bool exitOptionHandled;
            private static IList listCopyBuffer;
            private int _lastElementToShow;
            private int _sectionStartIndex;
            private SearchData searchData; // IN META
            private bool _scrollDownRequested;
            private bool allowDuplicants; // IN META

            public void Dispose() => End();
            public void End() => currentListLabel = "";
            public IEnumerable<T> InspectionIndexes<T>(ICollection<T> collectionReference, CollectionMetaData listMeta = null, iCollectionInspector<T> listElementInspector = null)
            {

                searchData = listMeta == null ? defaultSearchData : listMeta.searchData;

                #region Inspect Start

                var changed = false;

                if (_scrollDownRequested)
                    searchData.CloseSearch();

                searchData.SearchString(collectionReference, out _searching, out string[] searchby);

                _sectionStartIndex = 0;

                if (_searching)
                    _sectionStartIndex = searchData.InspectionIndexStart;
                else if (listMeta != null)
                    _sectionStartIndex = listMeta.listSectionStartIndex;
                else if (!Indexes.TryGetValue(collectionReference, out _sectionStartIndex))
                {
                    if (Indexes.Count > 100)
                    {
                        Debug.LogError("Inspector Indexes > 100. Clearing");
                        Indexes.Clear();
                    }
                    Indexes.Add(collectionReference, 0);
                }

                _count = collectionReference.Count;

                _lastElementToShow = _count;

                _sectionSizeOptimal = listMeta == null ? 10 : (listMeta.useOptimalShowRange ? GetOptimalSectionFor(_count) : listMeta.itemsToShow);

                if (_scrollDownRequested)
                {
                    changed = true;
                    SkrollToBottomInternal();
                }

                if (_count >= _sectionSizeOptimal * 2 || _sectionStartIndex > 0)
                {

                    if (_count > _sectionSizeOptimal)
                    {

                        while ((_sectionStartIndex > 0 && _sectionStartIndex >= _count).changes_Internal(ref changed))
                            _sectionStartIndex = Mathf.Max(0, _sectionStartIndex - _sectionSizeOptimal);

                        nl();
                        if (_sectionStartIndex > 0)
                        {

                            if (_sectionStartIndex > _sectionSizeOptimal && icon.UpLast.ClickUnFocus("To First element").changes_Internal(ref changed))
                                _sectionStartIndex = 0;

                            if (icon.Up.ClickUnFocus("To previous elements of the list. ", SCROLL_ARROWS_WIDTH, SCROLL_ARROWS_HEIGHT).changes_Internal(ref changed))
                            {
                                _sectionStartIndex = Mathf.Max(0, _sectionStartIndex - _sectionSizeOptimal + 1);
                                if (_sectionStartIndex == 1)
                                    _sectionStartIndex = 0;
                            }

                            ".. {0}; ".F(_sectionStartIndex - 1).write();

                        }
                        else
                            icon.UpLast.write("Is the first section of the list.", SCROLL_ARROWS_WIDTH, SCROLL_ARROWS_HEIGHT);

                        nl();

                    }
                    else line(Color.gray);

                }
                else if (_count > 0)
                    line(Color.gray);

                nl();

                #endregion

                PEGI_Styles.InList = true;

                if (!_searching)
                {
                    _lastElementToShow = Mathf.Min(_count, _sectionStartIndex + _sectionSizeOptimal);

                    Index = _sectionStartIndex;

                    var list = collectionReference as IList<T>;

                    if (list != null)
                    {
                        for (; Index < collectionReference.Count; Index++)
                        {

                            var lel = list[Index];

                            SetListElementReadabilityBackground(Index);

                            yield return lel;

                            RestoreBGColor();

                            if (Index >= _lastElementToShow)
                                break;
                        }
                    }
                    else
                    {
                        foreach (var el in Enm.Skip(collectionReference, _sectionStartIndex))
                        {
                            SetListElementReadabilityBackground(Index);

                            yield return el;

                            RestoreBGColor();

                            if (Index >= _lastElementToShow)
                                break;

                            Index++;
                        }
                    }

                    if ((_sectionStartIndex > 0) || (_count > _lastElementToShow))
                    {

                        nl();
                        if (_count > _lastElementToShow)
                        {

                            if (icon.Down.ClickUnFocus("To next elements of the list. ", SCROLL_ARROWS_WIDTH, SCROLL_ARROWS_HEIGHT).changes_Internal(ref changed))
                                _sectionStartIndex += _sectionSizeOptimal - 1;

                            if (icon.DownLast.ClickUnFocus("To Last element").changes_Internal(ref changed))
                                SkrollToBottomInternal();

                            "+ {0}".F(_count - _lastElementToShow).write();

                        }
                        else if (_sectionStartIndex > 0)
                            icon.DownLast.write("Is the last section of the list. ", SCROLL_ARROWS_WIDTH, SCROLL_ARROWS_HEIGHT);

                    }
                    else if (_count > 0)
                        line(Color.gray);

                }
                else
                {

                    var sectionIndex = _sectionStartIndex;

                    filteredList = searchData.GetFilteredList(_count);

                    _lastElementToShow = Mathf.Min(_count, _sectionStartIndex + _sectionSizeOptimal);

                    while (sectionIndex < _lastElementToShow)
                    {

                        Index = -1;

                        if (filteredList.Count > sectionIndex)
                            Index = filteredList[sectionIndex];
                        else
                            Index = GetNextFiltered(collectionReference, searchby, listElementInspector);


                        if (Index != -1)
                        {

                            SetListElementReadabilityBackground(sectionIndex);

                            yield return collectionReference.GetElementAt(Index);

                            RestoreBGColor();

                            sectionIndex++;
                        }
                        else break;
                    }


                    bool gotUnchecked = (searchData.UncheckedElement < _count - 1);

                    bool gotToShow = (filteredList.Count > _lastElementToShow) || gotUnchecked;

                    if (_sectionStartIndex > 0 || gotToShow)
                    {

                        nl();
                        if (gotToShow)
                        {

                            if (icon.Down.ClickUnFocus("To next elements of the list. ", SCROLL_ARROWS_WIDTH, SCROLL_ARROWS_HEIGHT).changes_Internal(ref changed))
                                _sectionStartIndex += _sectionSizeOptimal - 1;

                            if (icon.DownLast.ClickUnFocus("To Last element").changes_Internal(ref changed))
                            {
                                if (_searching)
                                    while (GetNextFiltered(collectionReference, searchby, listElementInspector) != -1) { }


                                SkrollToBottomInternal();
                            }

                            if (!gotUnchecked)
                                "+ {0}".F(filteredList.Count - _lastElementToShow).write();

                        }
                        else if (_sectionStartIndex > 0)
                            icon.DownLast.write("Is the last section of the list. ", SCROLL_ARROWS_WIDTH, SCROLL_ARROWS_HEIGHT);

                    }
                    else if (_count > 0)
                        line(Color.gray);

                }

                #region Finilize

                PEGI_Styles.InList = false;


                if (changed)
                    SaveSectionIndex(collectionReference, listMeta);

                #endregion
            }
            public void ListInstantiateNewName<T>()
            {
                Msg.New.GetText().write(Msg.NameNewBeforeInstancing_1p.GetText().F(typeof(T).ToPegiStringType()), 30, PEGI_Styles.ExitLabel);
                edit(ref addingNewNameHolder);
            }
            public bool TryShowListCreateNewOptions<T>(List<T> lst, ref T added, CollectionMetaData ld)
            {
                if (ld != null && !ld[CollectionInspectParams.showAddButton])
                    return false;

                if (reordering != null && reordering == lst)
                    return false;

                var type = typeof(T);

                var intTypes = ICfgExtensions.TryGetDerivedClasses(type);

                var tagTypes = TaggedTypesCfg.TryGetOrCreate(type);

                if (intTypes == null && tagTypes == null)
                    return false;

                var hasName = typeof(T).IsSubclassOf(typeof(Object)) || typeof(IGotName).IsAssignableFrom(typeof(T));

                if (hasName)
                    ListInstantiateNewName<T>();
                else
                    (intTypes == null ? "Create new {0}".F(typeof(T).ToPegiStringType()) : "Create Derrived from {0}".F(typeof(T).ToPegiStringType())).write();

                if (!hasName || addingNewNameHolder.Length > 1)
                {

                    var selectingDerrived = lst == addingNewOptionsInspected;

                    icon.Add.isFoldout("Instantiate Class Options", ref selectingDerrived).nl();

                    if (selectingDerrived)
                        addingNewOptionsInspected = lst;
                    else if (addingNewOptionsInspected == lst)
                        addingNewOptionsInspected = null;

                    if (selectingDerrived)
                    {
                        if (intTypes != null)
                            foreach (var t in intTypes)
                            {
                                write(t.ToPegiStringType());
                                if (icon.Create.ClickUnFocus().nl())
                                {
                                    added = (T)System.Activator.CreateInstance(t);
                                    QcSharp.AddWithUniqueNameAndIndex(lst, added, addingNewNameHolder);
                                    SkrollToBottom();
                                }
                            }

                        if (tagTypes != null)
                        {
                            var k = tagTypes.Keys;

                            int availableOptions = 0;

                            for (var i = 0; i < k.Count; i++)
                            {
                                if (tagTypes.CanAdd(i, lst))
                                {
                                    availableOptions++;

                                    write(tagTypes.DisplayNames[i]);
                                    if (icon.Create.ClickUnFocus().nl())
                                    {
                                        added = (T)System.Activator.CreateInstance(tagTypes.TaggedTypes.TryGet(k[i]));
                                        QcSharp.AddWithUniqueNameAndIndex(lst, added, addingNewNameHolder);
                                        SkrollToBottom();
                                    }
                                }

                            }

                            if (availableOptions == 0)
                                (k.Count == 0 ? "There no types derrived from {0}".F(typeof(T).ToString()) :
                                    "Existing types are restricted to one instance per list").writeHint();

                        }

                    }
                }
                else
                    icon.Add.GetText().write("Input a name for a new element", 40);
                nl();

                return true;
            }
            public bool TryShowListCreateNewOptions<T>(List<T> lst, ref T added, TaggedTypesCfg types, CollectionMetaData ld)
            {
                if (ld != null && !ld[CollectionInspectParams.showAddButton])
                    return false;

                if (reordering != null && reordering == lst)
                    return false;

                var changed = false;

                var hasName = typeof(T).IsSubclassOf(typeof(Object)) || typeof(IGotName).IsAssignableFrom(typeof(T));

                if (hasName)
                    ListInstantiateNewName<T>();
                else
                    "Create new {0}".F(typeof(T).ToPegiStringType()).write();

                if (!hasName || addingNewNameHolder.Length > 1)
                {

                    var selectingDerrived = lst == addingNewOptionsInspected;

                    icon.Add.isFoldout("Instantiate Class Options", ref selectingDerrived).nl();

                    if (selectingDerrived)
                        addingNewOptionsInspected = lst;
                    else if (addingNewOptionsInspected == lst)
                        addingNewOptionsInspected = null;

                    if (selectingDerrived)
                    {

                        var k = types.Keys;
                        for (var i = 0; i < k.Count; i++)
                        {

                            write(types.DisplayNames[i]);
                            if (icon.Create.ClickUnFocus().nl(ref changed))
                            {
                                added = (T)System.Activator.CreateInstance(types.TaggedTypes.TryGet(k[i]));
                                QcSharp.AddWithUniqueNameAndIndex(lst, added, addingNewNameHolder);
                                SkrollToBottom();
                            }
                        }
                    }
                }
                else
                    icon.Add.GetText().write("Input a name for a new element", 40);
                nl();

                return changed;
            }
            public void SkrollToBottom()
            {
                _scrollDownRequested = true;
            }

            private int GetOptimalSectionFor(int count)
            {
                const int listShowMax = 10;

                if (count < listShowMax)
                    return listShowMax;

                if (count > listShowMax * 3)
                    return listShowMax;

                _sectionSizeOptimal = SectionOptimal[count];

                if (_sectionSizeOptimal != 0)
                    return _sectionSizeOptimal;

                var minDiff = 999;

                for (var i = listShowMax - 2; i < listShowMax + 2; i++)
                {
                    var difference = i - (count % i);

                    if (difference >= minDiff) continue;
                    _sectionSizeOptimal = i;
                    minDiff = difference;
                    if (difference == 0)
                        break;
                }

                SectionOptimal[count] = _sectionSizeOptimal;

                return _sectionSizeOptimal;

            }
            private int GetNextFiltered<T>(ICollection<T> collectionReference, string[] searchby, iCollectionInspector<T> inspector = null)
            {

                foreach (var reff in Enm.Skip(collectionReference, searchData.UncheckedElement))
                {

                    if (searchData.UncheckedElement >= _count)
                        return -1;

                    int index = searchData.UncheckedElement;

                    searchData.UncheckedElement++;

                    object target;

                    if (inspector != null)
                    {
                        inspector.Set(reff);
                        target = inspector;
                    }
                    else
                        target = reff;

                    var na = target as INeedAttention;

                    var msg = na?.NeedAttention();

                    if (!searchData.FilterByNeedAttention || !msg.IsNullOrEmpty())
                    {
                        if (searchby.IsNullOrEmpty() || target.SearchMatch_Obj_Internal(searchby))
                        {
                            filteredList.Add(index);
                            return index;
                        }
                    }

                }

                return -1;
            }
            private void SaveSectionIndex<T>(ICollection<T> list, CollectionMetaData listMeta)
            {
                if (_searching)
                    searchData.InspectionIndexStart = _sectionStartIndex;
                else if (listMeta != null)
                    listMeta.listSectionStartIndex = _sectionStartIndex;
                else
                {
                    if (Indexes.Count > 100)
                    {
                        Debug.LogError("Collection Inspector Indexes > 100. Clearing...");
                        Indexes.Clear();
                    }
                    Indexes[list] = _sectionStartIndex;
                }
            }

            private void SkrollToBottomInternal()
            {

                if (!_searching)
                    _sectionStartIndex = _count - _sectionSizeOptimal;
                else
                    _sectionStartIndex = filteredList.Count - _sectionSizeOptimal;

                _sectionStartIndex = Mathf.Max(0, _sectionStartIndex);

                _scrollDownRequested = false;
            }
            private void SetListElementReadabilityBackground(int index)
            {
                switch (index % 4)
                {
                    case 1: SetBgColor(PEGI_Styles.listReadabilityBlue); break;
                    case 3: SetBgColor(PEGI_Styles.listReadabilityRed); break;
                }
            }
            internal string GetCurrentListLabel<T>(CollectionMetaData ld = null) =>
                ld != null
                    ? ld.label :
                        (currentListLabel.IsNullOrEmpty() ? typeof(T).ToPegiStringType() : currentListLabel);


            internal CollectionInspector Write_Search_DictionaryLabel<K, V>(CollectionMetaData listMEta, Dictionary<K, V> dic) =>
                Write_Search_DictionaryLabel<K, V>(listMEta.label, ref listMEta.inspectedElement, dic);
            internal CollectionInspector Write_Search_DictionaryLabel<K, V>(string label, ref int inspected, Dictionary<K, V> dic)
            {
                currentListLabel = label;

                bool inspecting = inspected != -1;

                if (!inspecting)
                    defaultSearchData.ToggleSearch(dic, label);
                else
                {
                    exitOptionHandled = true;
                    if (icon.List.ClickUnFocus("{0} [1]".F(Msg.ReturnToCollection.GetText(), dic.Count)))
                        inspected = -1;
                }

                if (dic != null && inspected >= 0 && dic.Count > inspected)
                {
                    var el = dic.GetElementAt(inspected);
                    label = "{0}->{1}:{2}".F(label, el.Key.GetNameForInspector(), el.Value.GetNameForInspector());
                }
                else label = (dic == null || dic.Count < 6) ? label : label.addCount(dic, true);

                if (label.ClickLabel(label, RemainingLength(defaultButtonSize * 2 + 10), PEGI_Styles.ListLabel) && inspected != -1)
                    inspected = -1;


                return this;
            }
            internal CollectionInspector Write_Search_ListLabel<T>(string label, ICollection<T> lst = null)
            {
                var notInsp = -1;
                return collectionInspector.Write_Search_ListLabel(label, ref notInsp, lst);
            }
            internal CollectionInspector Write_Search_ListLabel<T>(string label, ref int inspected, ICollection<T> lst)
            {
                currentListLabel = label;

                bool inspecting = inspected != -1;

                if (!inspecting)
                    defaultSearchData.ToggleSearch(lst, label);
                else
                {
                    exitOptionHandled = true;
                    if (icon.List.ClickUnFocus("{0} [1]".F(Msg.ReturnToCollection.GetText(), lst.Count)))
                        inspected = -1;
                }

                if (lst != null && inspected >= 0 && lst.Count > inspected)
                    label = "{0}->{1}".F(label, lst.GetElementAt(inspected).GetNameForInspector());
                else label = (lst == null || lst.Count < 6) ? label : label.addCount(lst, true);

                if (label.ClickLabel(label, RemainingLength(defaultButtonSize * 2 + 10), PEGI_Styles.ListLabel) && inspected != -1)
                    inspected = -1;

                return this;
            }
            internal CollectionInspector Write_Search_ListLabel<T>(CollectionMetaData ld, ICollection<T> lst)
            {

                currentListLabel = ld.label;

                if (!ld.IsInspectingElement && ld[CollectionInspectParams.showSearchButton])
                    ld.searchData.ToggleSearch(lst, ld.label);

                if (lst != null && ld.inspectedElement >= 0 && lst.Count > ld.inspectedElement)
                {
                    var el = lst.GetElementAt(ld.inspectedElement);
                    string nameToShow = el.GetNameForInspector();
                    currentListLabel = "{0}->{1}".F(ld.label, nameToShow);
                }
                else currentListLabel = (lst == null || lst.Count < 6) ? ld.label : ld.label.addCount(lst, true);


                if (ld.IsInspectingElement && lst != null)
                {
                    exitOptionHandled = true;
                    if (icon.List.ClickUnFocus("{0} {1} [2]".F(Msg.ReturnToCollection.GetText(), currentListLabel, lst.Count)))
                        ld.IsInspectingElement = false;
                }

                if (currentListLabel.ClickLabel(ld.label, RemainingLength(defaultButtonSize * 2 + 10), PEGI_Styles.ListLabel) && ld.inspectedElement != -1)
                    ld.inspectedElement = -1;

                return this;
            }
            internal bool ExitOrDrawPEGI<T>(T[] array, ref int index, CollectionMetaData ld = null)
            {
                var changed = false;

                if (index >= 0)
                {
                    if (!exitOptionHandled && (array == null || index >= array.Length || icon.List.ClickUnFocus("Return to {0} array".F(GetCurrentListLabel<T>(ld))).nl()))
                        index = -1;
                    else
                    {
                        nl();

                        object obj = array[index];
                        if (Nested_Inspect(ref obj).changes_Internal(ref changed))
                            array[index] = (T)obj;
                    }
                }

                exitOptionHandled = false;

                return changed;
            }
            internal bool ExitOrDrawPEGI<K, T>(Dictionary<K, T> dic, ref int index, CollectionMetaData ld = null)
            {
                var changed = false;

                if (!exitOptionHandled && icon.List.ClickUnFocus("{0}[{1}] of {2}".F(Msg.ReturnToCollection.GetText(), dic.Count, GetCurrentListLabel<T>(ld))).nl())
                    index = -1;
                else
                {
                    nl();

                    var item = dic.GetElementAt(index);
                    var key = item.Key;

                    object obj = dic[key];
                    if (Nested_Inspect(ref obj).changes_Internal(ref changed))
                        dic[key] = (T)obj;
                }

                exitOptionHandled = false;

                return changed;
            }
            internal bool ExitOrDrawPEGI<T>(List<T> list, ref int index, CollectionMetaData ld = null)
            {
                var changed = false;

                if (!exitOptionHandled && icon.List.ClickUnFocus("{0}[{1}] of {2}".F(Msg.ReturnToCollection.GetText(), list.Count, GetCurrentListLabel<T>(ld))).nl())
                    index = -1;
                else
                {
                    nl();

                    object obj = list[index];
                    if (Nested_Inspect(ref obj).changes_Internal(ref changed))
                        list[index] = (T)obj;
                }

                exitOptionHandled = false;

                return changed;
            }
            internal bool CollectionIsNull<T, V>(Dictionary<T, V> list)
            {
                if (list == null)
                {
                    "Dictionary of {0} is null".F(typeof(T).ToPegiStringType()).write();

                    /* if ("Initialize list".ClickUnFocus().nl())
                         list = new List<T>();
                     else*/
                    return true;
                }

                return false;
            }
            internal bool CollectionIsNull<T>(List<T> list)
            {
                if (list == null)
                {
                    "List of {0} is null".F(typeof(T).ToPegiStringType()).write();

                    /* if ("Initialize list".ClickUnFocus().nl())
                         list = new List<T>();
                     else*/
                    return true;
                }

                return false;
            }
            internal bool List_DragAndDropOptions<T>(List<T> list, CollectionMetaData meta = null) where T : Object
            {
                var changed = false;
#if UNITY_EDITOR

                var tracker = UnityEditor.ActiveEditorTracker.sharedTracker;

                if (tracker.isLocked == false && icon.Unlock.ClickUnFocus("Lock Inspector Window"))
                    tracker.isLocked = true;

                if (tracker.isLocked && icon.Lock.ClickUnFocus("Unlock Inspector Window"))
                {
                    tracker.isLocked = false;

                    var mb = ef.serObj.targetObject as MonoBehaviour;

                    QcUnity.FocusOn(mb ? mb.gameObject : ef.serObj.targetObject);

                }

                var dpl = meta?[CollectionInspectParams.allowDuplicates] ?? allowDuplicants;

                foreach (var ret in ef.DropAreaGUI<T>())
                {
                    if (dpl || !list.Contains(ret))
                    {
                        list.Add(ret);
                        changed = true;
                    }
                }



#endif
                return changed;
            }
            private void SetSelected<T>(CollectionMetaData meta, List<T> list, bool val)
            {
                if (meta == null)
                {
                    for (var i = 0; i < list.Count; i++)
                        selectedEls[i] = val;
                }
                else for (var i = 0; i < list.Count; i++)
                        meta.SetIsSelected(i, val);
            }
            private void TryMoveCopiedElement<T>(List<T> list, bool isAllowDuplicants)
            {
                bool errorShown = false;

                for (var i = _copiedElements.Count - 1; i >= 0; i--)
                {

                    var srcInd = _copiedElements[i];
                    var e = listCopyBuffer.TryGetObj(srcInd);

                    if (QcSharp.CanAdd(list, ref e, out T conv, !isAllowDuplicants))
                    {
                        list.Add(conv);
                        listCopyBuffer.RemoveAt(srcInd);
                    }
                    else if (!errorShown)
                    {
                        errorShown = true;
                        Debug.LogError("Couldn't add some of the elements");
                    }
                }

                if (!errorShown)
                    listCopyBuffer = null;
            }
            internal bool Edit_Array_Order<T>(ref T[] array, CollectionMetaData listMeta = null)
            {

                var changed = false;

                if (array != _editingArrayOrder)
                {
                    if ((listMeta == null || listMeta[CollectionInspectParams.showEditListButton]) && icon.Edit.ClickUnFocus(Msg.MoveCollectionElements, 28))
                        _editingArrayOrder = array;
                }

                else if (icon.Done.ClickUnFocus(Msg.FinishMovingCollectionElements.GetText(), 28).nl(ref changed))
                    _editingArrayOrder = null;

                if (array != _editingArrayOrder) return changed;

                var derivedClasses = ICfgExtensions.TryGetDerivedClasses(typeof(T));

                for (var i = 0; i < array.Length; i++)
                {

                    if (listMeta == null || listMeta[CollectionInspectParams.allowReordering])
                    {

                        if (i > 0)
                        {
                            if (icon.Up.ClickUnFocus("Move up").changes_Internal(ref changed))
                                QcSharp.Swap(ref array, i, i - 1);
                        }
                        else
                            icon.UpLast.draw("Last");

                        if (i < array.Length - 1)
                        {
                            if (icon.Down.ClickUnFocus("Move down").changes_Internal(ref changed))
                                QcSharp.Swap(ref array, i, i + 1);
                        }
                        else icon.DownLast.draw();
                    }

                    var el = array[i];

                    var isNull = el.IsNullOrDestroyed_Obj();

                    if (listMeta == null || listMeta[CollectionInspectParams.allowDeleting])
                    {
                        if (!isNull && typeof(T).IsUnityObject())
                        {
                            if (icon.Delete.ClickUnFocus(Msg.MakeElementNull).changes_Internal(ref changed))
                                array[i] = default;
                        }
                        else
                        {
                            if (icon.Close.ClickUnFocus(Msg.RemoveFromCollection).changes_Internal(ref changed))
                            {
                                QcSharp.Remove(ref array, i);
                                i--;
                            }
                        }
                    }

                    if (!isNull && derivedClasses != null)
                    {
                        var ty = el.GetType();
                        if (select(ref ty, derivedClasses, el.GetNameForInspector()))
                            array[i] = (el as ICfgCustom).TryDecodeInto<T>(ty);
                    }

                    if (!isNull)
                        write(el.GetNameForInspector());
                    else
                        "{0} {1}".F(icon.Empty.GetText(), typeof(T).ToPegiStringType()).write();

                    nl();
                }

                return changed;
            }
            internal bool Edit_List_Order<T>(List<T> list, CollectionMetaData listMeta = null)
            {

                var changed = false;

                var sd = listMeta == null ? defaultSearchData : listMeta.searchData;

                if (list != collectionInspector.reordering)
                {
                    if (!ReferenceEquals(sd.FilteredList, list) && (listMeta == null || listMeta[CollectionInspectParams.showEditListButton]) &&
                        icon.Edit.ClickUnFocus(Msg.MoveCollectionElements, 28))
                        reordering = list;
                }
                else if (icon.Done.ClickUnFocus(Msg.FinishMovingCollectionElements, 28).changes_Internal(ref changed))
                    reordering = null;

                if (list != collectionInspector.reordering) return changed;

#if UNITY_EDITOR
                if (!PaintingGameViewUI)
                {
                    nl();
                    ef.reorder_List(list, listMeta).changes_Internal(ref changed);
                }
                else
#endif

                #region Playtime UI reordering

                {
                    var derivedClasses = ICfgExtensions.TryGetDerivedClasses(typeof(T));

                    foreach (var el in collectionInspector.InspectionIndexes(list, listMeta))
                    {
                        int i = collectionInspector.Index;

                        if (listMeta == null || listMeta[CollectionInspectParams.allowReordering])
                        {

                            if (i > 0)
                            {
                                if (icon.Up.ClickUnFocus("Move up").changes_Internal(ref changed))
                                    list.Swap(i - 1);

                            }
                            else
                                icon.UpLast.draw("Last");

                            if (i < list.Count - 1)
                            {
                                if (icon.Down.ClickUnFocus("Move down").changes_Internal(ref changed))
                                    list.Swap(i);
                            }
                            else icon.DownLast.draw();
                        }

                        var isNull = el.IsNullOrDestroyed_Obj();

                        if (listMeta == null || listMeta[CollectionInspectParams.allowDeleting])
                        {

                            if (!isNull && typeof(T).IsUnityObject())
                            {
                                if (icon.Delete.ClickUnFocus(Msg.MakeElementNull))
                                    list[i] = default;
                            }
                            else
                            {
                                if (icon.Close.ClickUnFocus(Msg.RemoveFromCollection).changes_Internal(ref changed))
                                {
                                    list.RemoveAt(Index);
                                    Index--;
                                    _lastElementToShow--;
                                }
                            }
                        }


                        if (!isNull && derivedClasses != null)
                        {
                            var ty = el.GetType();
                            if (select(ref ty, derivedClasses, el.GetNameForInspector()))
                                list[i] = (el as ICfgCustom).TryDecodeInto<T>(ty);
                        }

                        if (!isNull)
                            write(el.GetNameForInspector());
                        else
                            "{0} {1}".F(icon.Empty.GetText(), typeof(T).ToPegiStringType()).write();

                        nl();
                    }

                }

                #endregion

                #region Select

                var selectedCount = 0;

                if (listMeta == null)
                {
                    for (var i = 0; i < list.Count; i++)
                        if (selectedEls[i])
                            selectedCount++;
                }
                else
                    for (var i = 0; i < list.Count; i++)
                        if (listMeta.GetIsSelected(i))
                            selectedCount++;

                if (selectedCount > 0 && icon.DeSelectAll.Click(icon.DeSelectAll.GetText()))
                    SetSelected(listMeta, list, false);

                if (selectedCount == 0 && icon.SelectAll.Click(icon.SelectAll.GetText()))
                    SetSelected(listMeta, list, true);


                #endregion

                #region Copy, Cut, Paste, Move 


                var duplicants = listMeta != null ? listMeta[CollectionInspectParams.allowDuplicates] : allowDuplicants;

                if (list.Count > 1 && typeof(IGotIndex).IsAssignableFrom(typeof(T)))
                {

                    bool down = false;

                    if (icon.Down.Click("Sort Ascending").changes_Internal(ref down) || icon.Up.Click("Sort Descending"))
                    {
                        changed = true;

                        list.Sort((emp1, emp2) =>
                        {

                            var igc1 = emp1 as IGotIndex;
                            var igc2 = emp2 as IGotIndex;

                            if (igc1 == null || igc2 == null)
                                return 0;

                            return (down ? 1 : -1) * (igc1.IndexForInspector - igc2.IndexForInspector);

                        });
                    }
                }

                if (listCopyBuffer != null)
                {

                    if (icon.Close.ClickUnFocus("Clean buffer"))
                        listCopyBuffer = null;

                    bool same = listCopyBuffer == list;

                    if (same && !cutPaste)
                        "DUPLICATE:".write("Selected elements are from this list", 60);

                    if (typeof(T).IsUnityObject())
                    {

                        if (!cutPaste && icon.Paste.ClickUnFocus(same
                                ? Msg.TryDuplicateSelected.GetText()
                                : "{0} Of {1} to here".F(Msg.TryDuplicateSelected.GetText(),
                                    listCopyBuffer.GetNameForInspector())))
                        {
                            foreach (var e in _copiedElements)
                                list.TryAdd(listCopyBuffer.TryGetObj(e), !duplicants);
                        }

                        if (!same && cutPaste && icon.Move.ClickUnFocus("Try Move References Of {0}".F(listCopyBuffer)))
                            collectionInspector.TryMoveCopiedElement(list, duplicants);

                    }
                    else
                    {

                        if (!cutPaste && icon.Paste.ClickUnFocus(same
                                ? "Try to duplicate selected references"
                                : "Try Add Deep Copy {0}".F(listCopyBuffer.GetNameForInspector())))
                        {

                            foreach (var e in _copiedElements)
                            {

                                var el = listCopyBuffer.TryGetObj(e);

                                if (el != null)
                                {

                                    var istd = el as ICfgCustom;

                                    if (istd != null)
                                    {
                                        var ret = (T)System.Activator.CreateInstance(el.GetType());

                                        (ret as ICfgCustom).Decode(istd.Encode().ToString());

                                        list.TryAdd(ret);
                                    }

                                    //list.TryAdd(istd.CloneCfg());
                                    else
                                        list.TryAdd(JsonUtility.FromJson<T>(JsonUtility.ToJson(el)));
                                }
                            }
                        }

                        if (!same && cutPaste && icon.Move.ClickUnFocus("Try Move {0}".F(listCopyBuffer)))
                            collectionInspector.TryMoveCopiedElement(list, duplicants);
                    }

                }
                else if (selectedCount > 0)
                {
                    var copyOrMove = false;

                    if (icon.Copy.ClickUnFocus("Copy selected elements"))
                    {
                        cutPaste = false;
                        copyOrMove = true;
                    }

                    if (icon.Cut.ClickUnFocus("Cut selected elements"))
                    {
                        cutPaste = true;
                        copyOrMove = true;
                    }

                    if (copyOrMove)
                    {
                        listCopyBuffer = list;
                        _copiedElements = listMeta != null ? listMeta.GetSelectedElements() : selectedEls.GetItAll();
                    }
                }

                #endregion

                #region Clean & Delete

                if (list != listCopyBuffer)
                {

                    if ((listMeta == null || listMeta[CollectionInspectParams.allowDeleting]) && list.Count > 0)
                    {
                        var nullOrDestroyedCount = 0;

                        for (var i = 0; i < list.Count; i++)
                            if (list[i].IsNullOrDestroyed_Obj())
                                nullOrDestroyedCount++;

                        if (nullOrDestroyedCount > 0 && icon.Refresh.ClickUnFocus("Remove all null elements"))
                        {
                            for (var i = list.Count - 1; i >= 0; i--)
                                if (list[i].IsNullOrDestroyed_Obj())
                                    list.RemoveAt(i);

                            SetSelected(listMeta, list, false);
                        }
                    }

                    if ((listMeta == null || listMeta[CollectionInspectParams.allowDeleting]) && list.Count > 0)
                    {
                        if (selectedCount > 0 &&
                            icon.Delete.ClickConfirm("delLstPegi", list, "Delete {0} Selected".F(selectedCount)))
                        {
                            if (listMeta == null)
                            {
                                for (var i = list.Count - 1; i >= 0; i--)
                                    if (selectedEls[i])
                                        list.RemoveAt(i);
                            }
                            else
                                for (var i = list.Count - 1; i >= 0; i--)
                                    if (listMeta.GetIsSelected(i))
                                        list.RemoveAt(i);

                            SetSelected(listMeta, list, false);

                        }
                    }
                }

                #endregion

                if (listMeta != null && icon.Config.isEntered(ref listMeta.inspectListMeta))
                    listMeta.Nested_Inspect();
                else if (typeof(Object).IsAssignableFrom(typeof(T)) || !listCopyBuffer.IsNullOrEmpty())
                {
                    "Allow Duplicants".toggle("Will add elements to the list even if they are already there", 120, ref duplicants)
                        .changes_Internal(ref changed);

                    if (listMeta != null)
                        listMeta[CollectionInspectParams.allowDuplicates] = duplicants;
                    else allowDuplicants = duplicants;
                }

                return changed;
            }
            internal bool InspectClassInList<T>(List<T> list, int index, ref int inspected, CollectionMetaData listMeta = null) where T : class
            {
                var el = list[index];
                var changed = false;

                var pl = el as IPEGI_ListInspect;
                var isPrevious = (listMeta != null && listMeta.previouslyInspectedElement == index)
                                 || (listMeta == null && collectionInspector.previouslyEntered != null && el == collectionInspector.previouslyEntered);

                if (isPrevious)
                    SetBgColor(PreviousInspectedColor);

                if (pl != null)
                {
                    var chBefore = GUI.changed;

                    pl.InspectInList(ref inspected, index);

                    if (!chBefore && GUI.changed)
                        pl.SetToDirty_Obj();

                    if (changed || inspected == index)
                        isPrevious = true;

                }
                else
                {

                    if (el.IsNullOrDestroyed_Obj())
                    {
                        var ed = listMeta?[index];
                        if (ed == null)
                            "{0}: NULL {1}".F(index, typeof(T).ToPegiStringType()).write();
                        else
                        {
                            var elObj = (object)el;
                            if (ed.PEGI_inList<T>(ref elObj))
                            {
                                isPrevious = true;
                                list[index] = elObj as T;
                            }

                        }
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

                        var iind = el as IGotIndex;

                        iind?.IndexForInspector.ToString().write(20);

                        var named = el as IGotName;
                        if (named != null)
                        {
                            var so = uo as ScriptableObject;
                            var n = named.NameForInspector;

                            if (so)
                            {
                                if (editDelayed(ref n).changes_Internal(ref changed))
                                {
                                    QcUnity.RenameAsset(so, n);
                                    named.NameForInspector = n;
                                    isPrevious = true;
                                }
                            }
                            else if (edit(ref n).changes_Internal(ref changed))
                            {
                                named.NameForInspector = n;
                                isPrevious = true;
                            }
                        }
                        else
                        {
                            if (!uo && pg == null && listMeta == null)
                            {
                                if (el.GetNameForInspector().ClickLabel(Msg.InspectElement.GetText(), RemainingLength(defaultButtonSize * 2 + 10)))
                                {
                                    inspected = index;
                                    isPrevious = true;
                                }
                            }
                            else
                            {
                                if (uo)
                                {
                                    if (edit(ref uo))
                                        list[index] = uo as T;

                                    Texture tex = uo as Texture;

                                    if (tex)
                                    {
                                        if (uo.ClickHighlight(tex))
                                            isPrevious = true;

                                        clickHighlightHandled = true;
                                    }
                                    else if (Try_NameInspect(uo).changes_Internal(ref changed))
                                        isPrevious = true;
                                }
                                else if (el.GetNameForInspector().ClickLabel("Inspect", RemainingLength(defaultButtonSize * 2 + 50)).changes_Internal(ref changed))
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
            internal bool IsMonoType<T>(IList<T> list, int i)
            {
                if (!(typeof(MonoBehaviour)).IsAssignableFrom(typeof(T))) return false;

                GameObject mb = null;
                if (edit(ref mb))
                {
#pragma warning disable UNT0014 // Invalid type for call to GetComponent
                    list[i] = mb.GetComponent<T>();
#pragma warning restore UNT0014 // Invalid type for call to GetComponent
                    if (list[i] == null) GameView.ShowNotification(typeof(T) + " Component not found");
                }
                return true;

            }
            internal bool TryShowListAddNewOption<T>(string text, List<T> list, ref T added, CollectionMetaData ld = null)
            {
                if (ld != null && !ld[CollectionInspectParams.showAddButton])
                    return false;

                var type = typeof(T);

                if (!type.IsNew())
                {
                    collectionInspector.ListAddEmptyClick(list, ld);
                    return true;
                }

                if (type.TryGetClassAttribute<DerivedListAttribute>() != null || typeof(IGotClassTag).IsAssignableFrom(type))
                    return false;

                string name = null;

                var sd = ld == null ? defaultSearchData : ld.searchData;

                if (ReferenceEquals(sd.FilteredList, list))
                    name = sd.SearchedText;

                if ("+ NEW {0}".F(text).ClickUnFocus(Msg.AddNewCollectionElement.GetText() + (name.IsNullOrEmpty() ? "" : " Named {0}".F(name))))
                {
                    if (typeof(T).IsSubclassOf(typeof(Object)))
                        list.Add(default);
                    else
                        added = name.IsNullOrEmpty() ? QcSharp.AddWithUniqueNameAndIndex(list) : QcSharp.AddWithUniqueNameAndIndex(list, name);

                    SkrollToBottom();
                }

                return true;
            }
            internal bool TryShowListAddNewOption<T>(List<T> list, ref T added, CollectionMetaData ld = null)
            {

                if (ld != null && !ld[CollectionInspectParams.showAddButton])
                    return false;

                var type = typeof(T);

                if (!type.IsNew())
                {
                    collectionInspector.ListAddEmptyClick(list, ld);
                    return true;
                }

                if (type.TryGetClassAttribute<DerivedListAttribute>() != null || typeof(IGotClassTag).IsAssignableFrom(type))
                    return false;

                string name = null;

                var sd = ld == null ? defaultSearchData : ld.searchData;

                if (ReferenceEquals(sd.FilteredList, list))
                    name = sd.SearchedText;

                if (icon.Add.ClickUnFocus(Msg.AddNewCollectionElement.GetText() + (name.IsNullOrEmpty() ? "" : " Named {0}".F(name))))
                {
                    if (typeof(T).IsSubclassOf(typeof(Object)))
                        list.Add(default);
                    else
                        added = name.IsNullOrEmpty() ? QcSharp.AddWithUniqueNameAndIndex(list) : QcSharp.AddWithUniqueNameAndIndex(list, name);

                    SkrollToBottom();
                }

                return true;
            }
            internal bool ListAddEmptyClick<T>(IList<T> list, CollectionMetaData ld = null)
            {

                if (ld != null && !ld[CollectionInspectParams.showAddButton])
                    return false;

                var type = typeof(T);

                if (!type.IsUnityObject() && (type.TryGetClassAttribute<DerivedListAttribute>() != null || typeof(IGotClassTag).IsAssignableFrom(type)))
                    return false;

                if (icon.Add.ClickUnFocus(Msg.AddNewCollectionElement.GetText()))
                {
                    list.Add(default);
                    collectionInspector.SkrollToBottom();
                    return true;
                }
                return false;
            }
        }


    }
}
