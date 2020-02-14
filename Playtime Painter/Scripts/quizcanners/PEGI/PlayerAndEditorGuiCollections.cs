using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Linq;

using System.Linq.Expressions;
using QuizCannersUtilities;
using UnityEngine.U2D;
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
        #region Collection MGMT Functions 

        public static int InspectedIndex
        {
            get { return collectionInspector.Index; }
            private set { collectionInspector.Index = value; }
        }

        private static T listLabel_Used<T>(this T val)
        {
            collectionInspector.listLabel_Used();
            return val;
        }
        
        public static string GetCurrentListLabel<T>(ListMetaData meta = null) => collectionInspector.GetCurrentListLabel<T>(meta);

        public static void UnselectAll() => collectionInspector.selectedEls.Clear();

        public static bool Getselected(int index) => collectionInspector.selectedEls[index];

        public static void SetSelected(int index, bool value) => collectionInspector.selectedEls[index] = value;

        private static CollectionInspector collectionInspector = new CollectionInspector();

        private class CollectionInspector
        {

            private const int UpDownWidth = 190;
            private const int UpDownHeight = 20;
            private readonly Dictionary<IEnumerable, int> Indexes = new Dictionary<IEnumerable, int>();

            public int Index { get; set; } = -1;

            private bool _searching;

            private List<int> filteredList;

            private int _sectionSizeOptimal;

            private int _count;

            public IList reordering;

            private bool allowDuplicants;

            private int _lastElementToShow;

            private int _sectionStartIndex;

            private readonly CountlessInt SectionOptimal = new CountlessInt();
            private int GetOptimalSectionFor(int count)
            {
                int _sectionSizeOptimal;

                const int listShowMax = 10;

                if (count < listShowMax)
                    return listShowMax;


                if (count > listShowMax * 3)
                    return listShowMax;

                _sectionSizeOptimal = SectionOptimal[count];

                if (_sectionSizeOptimal != 0)
                    return _sectionSizeOptimal;

                var bestdifference = 999;

                for (var i = listShowMax - 2; i < listShowMax + 2; i++)
                {
                    var difference = i - (count % i);

                    if (difference < bestdifference)
                    {
                        _sectionSizeOptimal = i;
                        bestdifference = difference;
                        if (difference == 0)
                            break;
                    }

                }

                SectionOptimal[count] = _sectionSizeOptimal;

                return _sectionSizeOptimal;

            }

            private static IList addingNewOptionsInspected;
            private string addingNewNameHolder = "Name";

            public IEnumerable<int> InspectionIndexes<T>(ICollection<T> list, ListMetaData listMeta = null)
            {

                searchData = listMeta == null ? defaultSearchData : listMeta.searchData;

                #region Inspect Start

                var changed = false;

                if (_scrollDownRequested)
                    searchData.CloseSearch();

                string[] searchby;
                searchData.SearchString(list, out _searching, out searchby);

                _sectionStartIndex = 0;

                if (_searching)
                    _sectionStartIndex = searchData.inspectionIndexStart;
                else if (listMeta != null)
                    _sectionStartIndex = listMeta.listSectionStartIndex;
                else if (!Indexes.TryGetValue(list, out _sectionStartIndex))
                    Indexes.Add(list, 0);

                _count = list.Count;

                _lastElementToShow = _count;

                _sectionSizeOptimal = GetOptimalSectionFor(_count);

                if (_scrollDownRequested)
                {
                    changed = true;
                    SkrollToBottomInternal();
                }

                if (_count >= _sectionSizeOptimal * 2 || _sectionStartIndex > 0)
                {

                    if (_count > _sectionSizeOptimal)
                    {

                        while ((_sectionStartIndex > 0 && _sectionStartIndex >= _count).changes(ref changed))
                            _sectionStartIndex = Mathf.Max(0, _sectionStartIndex - _sectionSizeOptimal);

                        nl();
                        if (_sectionStartIndex > 0)
                        {

                            if (_sectionStartIndex > _sectionSizeOptimal && icon.UpLast.ClickUnFocus("To First element").changes(ref changed))
                                _sectionStartIndex = 0;

                            if (icon.Up.ClickUnFocus("To previous elements of the list. ", UpDownWidth, UpDownHeight).changes(ref changed))
                            {
                                _sectionStartIndex = Mathf.Max(0, _sectionStartIndex - _sectionSizeOptimal + 1);
                                if (_sectionStartIndex == 1)
                                    _sectionStartIndex = 0;
                            }

                            ".. {0}; ".F(_sectionStartIndex - 1).write();

                        }
                        else
                            icon.UpLast.write("Is the first section of the list.", UpDownWidth, UpDownHeight);

                        nl();

                    }
                    else line(Color.gray);

                }
                else if (list.Count > 0)
                    line(Color.gray);

                nl();

                #endregion

                PEGI_Styles.InList = true;

                if (!_searching)
                {
                    _lastElementToShow = Mathf.Min(_count, _sectionStartIndex + _sectionSizeOptimal);

                    for (Index = _sectionStartIndex; Index < _lastElementToShow; Index++)
                    {

                        SetListElementReadabilityBackground(Index);

                        yield return Index;

                        RestoreBGcolor();
                    }

                    if ((_sectionStartIndex > 0) || (_count > _lastElementToShow))
                    {

                        nl();
                        if (_count > _lastElementToShow)
                        {

                            if (icon.Down.ClickUnFocus("To next elements of the list. ", UpDownWidth, UpDownHeight).changes(ref changed))
                                _sectionStartIndex += _sectionSizeOptimal - 1;

                            if (icon.DownLast.ClickUnFocus("To Last element").changes(ref changed))
                                SkrollToBottomInternal();

                            "+ {0}".F(_count - _lastElementToShow).write();

                        }
                        else if (_sectionStartIndex > 0)
                            icon.DownLast.write("Is the last section of the list. ", UpDownWidth, UpDownHeight);

                    }
                    else if (_count > 0)
                        line(Color.gray);

                }
                else
                {

                    var sectionIndex = _sectionStartIndex;

                    filteredList = searchData.filteredListElements;

                    _lastElementToShow = Mathf.Min(list.Count, _sectionStartIndex + _sectionSizeOptimal);

                    while (sectionIndex < _lastElementToShow)
                    {

                        Index = -1;

                        if (filteredList.Count > sectionIndex)
                            Index = filteredList[sectionIndex];
                        else
                            Index = GetNextFiltered(list, searchby);


                        if (Index != -1)
                        {

                            SetListElementReadabilityBackground(sectionIndex);

                            yield return Index;

                            RestoreBGcolor();

                            sectionIndex++;
                        }
                        else break;
                    }


                    bool gotUnchecked = (searchData.uncheckedElement < _count - 1);

                    bool gotToShow = (filteredList.Count > _lastElementToShow) || gotUnchecked;

                    if (_sectionStartIndex > 0 || gotToShow)
                    {

                        nl();
                        if (gotToShow)
                        {

                            if (icon.Down.ClickUnFocus("To next elements of the list. ", UpDownWidth, UpDownHeight).changes(ref changed))
                                _sectionStartIndex += _sectionSizeOptimal - 1;

                            if (icon.DownLast.ClickUnFocus("To Last element").changes(ref changed))
                            {
                                if (_searching)
                                    while (GetNextFiltered(list, searchby) != -1) { }


                                SkrollToBottomInternal();
                            }

                            if (!gotUnchecked)
                                "+ {0}".F(filteredList.Count - _lastElementToShow).write();

                        }
                        else if (_sectionStartIndex > 0)
                            icon.DownLast.write("Is the last section of the list. ", UpDownWidth, UpDownHeight);

                    }
                    else if (_count > 0)
                        line(Color.gray);

                }

                #region Finilize

                PEGI_Styles.InList = false;


                if (changed)
                    SaveSectionIndex(list, listMeta);

                #endregion
            }
            
            public void listInstantiateNewName<T>()
            {
                Msg.New.GetText().write(Msg.NameNewBeforeInstancing_1p.GetText().F(typeof(T).ToPegiStringType()), 30, PEGI_Styles.ExitLabel);
                edit(ref addingNewNameHolder);
            }

            public bool PEGI_InstantiateOptions_SO<T>(List<T> lst, ref T added, ListMetaData ld) where T : ScriptableObject
            {
                if (ld != null && !ld.showAddButton)
                    return false;

                if (reordering != null && reordering == lst)
                    return false;

                var type = typeof(T);

                var indTypes = type.TryGetDerivedClasses();

                var tagTypes = TaggedTypesCfg.TryGetOrCreate(type);

                if (indTypes == null && tagTypes == null && typeof(T).IsAbstract)
                    return false;

                var changed = false;

                listInstantiateNewName<T>();

                if (addingNewNameHolder.Length > 1)
                {
                    if (indTypes == null && tagTypes == null)
                    {
                        if (icon.Create.ClickUnFocus(Msg.AddNewCollectionElement).nl(ref changed))
                        {
                            added = QcUnity.CreateAndAddScriptableObjectAsset(lst, "Assets/ScriptableObjects/",
                                addingNewNameHolder);
                            SkrollToBottom();
                        }
                    }
                    else
                    {
                        var selectingDerrived = lst == addingNewOptionsInspected;

                        icon.Create.foldout("Instantiate Class Options", ref selectingDerrived).nl();

                        if (selectingDerrived)
                            addingNewOptionsInspected = lst;
                        else if (addingNewOptionsInspected == lst)
                            addingNewOptionsInspected = null;

                        if (selectingDerrived)
                        {
                            if (indTypes != null)
                                foreach (var t in indTypes)
                                {
                                    write(t.ToPegiStringType());
                                    if (icon.Create.ClickUnFocus().nl(ref changed))
                                    {
                                        added = QcUnity.CreateScriptableObjectAsset(lst, "Assets/ScriptableObjects/",
                                            addingNewNameHolder, t);
                                        SkrollToBottom();
                                    }
                                }

                            if (tagTypes != null)
                            {
                                var k = tagTypes.Keys;

                                int optionsPresented = 0;

                                for (var i = 0; i < k.Count; i++)
                                {

                                    if (tagTypes.CanAdd(i, lst))
                                    {
                                        optionsPresented++;
                                        write(tagTypes.DisplayNames[i]);
                                        if (icon.Create.ClickUnFocus().nl(ref changed))
                                        {
                                            added = QcUnity.CreateScriptableObjectAsset(lst, "Assets/ScriptableObjects/",
                                                addingNewNameHolder, tagTypes.TaggedTypes.TryGet(k[i]));

                                            SkrollToBottom();
                                        }
                                    }

                                }

                                if (optionsPresented == 0)
                                    (k.Count == 0 ? "There no types derrived from {0}".F(typeof(T).ToString()) :
                                            "Existing types are restricted to one instance per list").writeHint();

                            }
                        }
                    }
                }
                nl();

                return changed;

            }

            public bool PEGI_InstantiateOptions<T>(List<T> lst, ref T added, ListMetaData ld)
            {
                if (ld != null && !ld.showAddButton)
                    return false;

                if (reordering != null && reordering == lst)
                    return false;

                var type = typeof(T);

                var intTypes = type.TryGetDerivedClasses();

                var tagTypes = TaggedTypesCfg.TryGetOrCreate(type);

                if (intTypes == null && tagTypes == null)
                    return false;

                var changed = false;

                var hasName = typeof(T).IsSubclassOf(typeof(Object)) || typeof(IGotName).IsAssignableFrom(typeof(T));

                if (hasName)
                    listInstantiateNewName<T>();
                else
                    (intTypes == null ? "Create new {0}".F(typeof(T).ToPegiStringType()) : "Create Derrived from {0}".F(typeof(T).ToPegiStringType())).write();

                if (!hasName || addingNewNameHolder.Length > 1)
                {

                    var selectingDerrived = lst == addingNewOptionsInspected;

                    icon.Add.foldout("Instantiate Class Options", ref selectingDerrived).nl();

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
                                if (icon.Create.ClickUnFocus().nl(ref changed))
                                {
                                    added = (T)Activator.CreateInstance(t);
                                    QcUtils.AddWithUniqueNameAndIndex(lst, added, addingNewNameHolder);
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
                                    if (icon.Create.ClickUnFocus().nl(ref changed))
                                    {
                                        added = (T)Activator.CreateInstance(tagTypes.TaggedTypes.TryGet(k[i]));
                                        QcUtils.AddWithUniqueNameAndIndex(lst, added, addingNewNameHolder);
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

                return changed;
            }

            public bool PEGI_InstantiateOptions<T>(List<T> lst, ref T added, TaggedTypesCfg types, ListMetaData ld)
            {
                if (ld != null && !ld.showAddButton)
                    return false;

                if (reordering != null && reordering == lst)
                    return false;

                var changed = false;

                var hasName = typeof(T).IsSubclassOf(typeof(UnityEngine.Object)) || typeof(IGotName).IsAssignableFrom(typeof(T));

                if (hasName)
                    listInstantiateNewName<T>();
                else
                    "Create new {0}".F(typeof(T).ToPegiStringType()).write();

                if (!hasName || addingNewNameHolder.Length > 1)
                {

                    var selectingDerrived = lst == addingNewOptionsInspected;

                    icon.Add.foldout("Instantiate Class Options", ref selectingDerrived).nl();

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
                                added = (T)Activator.CreateInstance(types.TaggedTypes.TryGet(k[i]));
                                QcUtils.AddWithUniqueNameAndIndex(lst, added, addingNewNameHolder);
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

            private int GetNextFiltered<T>(ICollection<T> list, string[] searchby)
            {

                while (searchData.uncheckedElement < _count)
                {

                    int index = searchData.uncheckedElement;

                    searchData.uncheckedElement++;

                    var el = list.ElementAt(index);

                    var na = el as INeedAttention;

                    var msg = na?.NeedAttention();

                    if (!searchData.filterByNeedAttention || !msg.IsNullOrEmpty())
                    {
                        if (searchby.IsNullOrEmpty() || el.SearchMatch_Obj_Internal(searchby))
                        {
                            filteredList.Add(index);
                            return index;
                        }
                    }

                }

                return -1;
            }

            private SearchData searchData;

            private void SaveSectionIndex<T>(ICollection<T> list, ListMetaData listMeta)
            {
                if (_searching)
                    searchData.inspectionIndexStart = _sectionStartIndex;
                else if (listMeta != null)
                    listMeta.listSectionStartIndex = _sectionStartIndex;
                else
                    Indexes[list] = _sectionStartIndex;
            }

            private bool _scrollDownRequested = false;

            public void SkrollToBottom()
            {
                _scrollDownRequested = true;
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

            private string currentListLabel = "";
            public string GetCurrentListLabel<T>(ListMetaData ld = null) => ld != null ? ld.label :
                        (currentListLabel.IsNullOrEmpty() ? typeof(T).ToPegiStringType() : currentListLabel);

            public void listLabel_Used()
            {
                currentListLabel = "";
            }

            public void write_Search_ListLabel<T>(string label, ICollection<T> lst = null)
            {
                var notInsp = -1;
                collectionInspector.write_Search_ListLabel(label, ref notInsp, lst);
            }

            public void write_Search_ListLabel<T>(string label, ref int inspected, ICollection<T> lst)
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
                    label = "{0}->{1}".F(label, lst.ElementAt(inspected).GetNameForInspector());
                else label = (lst == null || lst.Count < 6) ? label : label.AddCount(lst, true);

                if (label.ClickLabel(label, RemainingLength(defaultButtonSize * 2 + 10), PEGI_Styles.ListLabel) && inspected != -1)
                    inspected = -1;
            }

            public void write_Search_ListLabel<T>(ListMetaData ld, ICollection<T> lst)
            {

                currentListLabel = ld.label;

                if (!ld.Inspecting && ld.showSearchButton)
                    ld.searchData.ToggleSearch(lst, ld.label);

                if (lst != null && ld.inspected >= 0 && lst.Count > ld.inspected)
                {

                    var el = lst.ElementAt(ld.inspected);

                    currentListLabel = "{0}->{1}".F(ld.label, el.GetNameForInspector());

                }
                else currentListLabel = (lst == null || lst.Count < 6) ? ld.label : ld.label.AddCount(lst, true);


                if (ld.Inspecting)
                {
                    exitOptionHandled = true;
                    if (icon.List.ClickUnFocus("{0} {1} [2]".F(Msg.ReturnToCollection.GetText(), currentListLabel, lst.Count)))
                        ld.Inspecting = false;
                }

                if (currentListLabel.ClickLabel(ld.label, RemainingLength(defaultButtonSize * 2 + 10), PEGI_Styles.ListLabel) && ld.inspected != -1)
                    ld.inspected = -1;
            }

            private bool exitOptionHandled;

            public bool ExitOrDrawPEGI<T>(T[] array, ref int index, ListMetaData ld = null)
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
                        if (Nested_Inspect(ref obj).changes(ref changed))
                            array[index] = (T)obj;
                    }
                }

                exitOptionHandled = false;

                return changed;
            }

            public bool ExitOrDrawPEGI<K, T>(Dictionary<K, T> dic, ref int index, ListMetaData ld = null)
            {
                var changed = false;

                if (!exitOptionHandled && icon.List.ClickUnFocus("{0}[{1}] of {2}".F(Msg.ReturnToCollection.GetText(), dic.Count, GetCurrentListLabel<T>(ld))).nl())
                    index = -1;
                else
                {
                    nl();

                    var item = dic.ElementAt(index);
                    var key = item.Key;

                    object obj = dic[key];
                    if (Nested_Inspect(ref obj).changes(ref changed))
                        dic[key] = (T)obj;
                }

                exitOptionHandled = false;

                return changed;
            }

            public bool ExitOrDrawPEGI<T>(List<T> list, ref int index, ListMetaData ld = null)
            {
                var changed = false;

                if (!exitOptionHandled && icon.List.ClickUnFocus("{0}[{1}] of {2}".F(Msg.ReturnToCollection.GetText(), list.Count, GetCurrentListLabel<T>(ld))).nl())
                    index = -1;
                else
                {
                    nl();

                    object obj = list[index];
                    if (Nested_Inspect(ref obj).changes(ref changed))
                        list[index] = (T)obj;
                }

                exitOptionHandled = false;

                return changed;
            }

            public bool listIsNull<T>(ref List<T> list)
            {
                if (list == null)
                {
                    if ("Initialize list".ClickUnFocus().nl())
                        list = new List<T>();
                    else
                        return true;

                }

                return false;
            }

            public bool list_DropOption<T>(List<T> list, ListMetaData meta = null) where T : UnityEngine.Object
            {
                var changed = false;
#if UNITY_EDITOR

                if (ActiveEditorTracker.sharedTracker.isLocked == false && icon.Unlock.ClickUnFocus("Lock Inspector Window"))
                    ActiveEditorTracker.sharedTracker.isLocked = true;

                if (ActiveEditorTracker.sharedTracker.isLocked && icon.Lock.ClickUnFocus("Unlock Inspector Window"))
                {
                    ActiveEditorTracker.sharedTracker.isLocked = false;

                    var mb = ef.serObj.targetObject as MonoBehaviour;

                    QcUnity.FocusOn(mb ? mb.gameObject : ef.serObj.targetObject);

                }

                var dpl = meta != null ? meta.allowDuplicants : allowDuplicants;

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

            public Array _editingArrayOrder;

            public CountlessBool selectedEls = new CountlessBool();

            private List<int> _copiedElements = new List<int>();

            private bool cutPaste;

            private void SetSelected<T>(ListMetaData meta, List<T> list, bool val)
            {
                if (meta == null)
                {
                    for (var i = 0; i < list.Count; i++)
                        selectedEls[i] = val;
                }
                else for (var i = 0; i < list.Count; i++)
                        meta.SetIsSelected(i, val);
            }

            private void TryMoveCopiedElement<T>(List<T> list, bool allowDuplicants)
            {

                //    foreach (var e in _copiedElements)
                //   list.TryAdd(listCopyBuffer.TryGetObj(e));

                bool errorShown = false;

                for (var i = _copiedElements.Count - 1; i >= 0; i--)
                {

                    var srcInd = _copiedElements[i];
                    var e = listCopyBuffer.TryGetObj(srcInd);

                    T conv;
                    if (list.CanAdd(ref e, out conv, !allowDuplicants))
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

            public bool edit_Array_Order<T>(ref T[] array, ListMetaData listMeta = null)
            {

                var changed = false;

                if (array != _editingArrayOrder)
                {
                    if ((listMeta == null || listMeta.showEditListButton) && icon.Edit.ClickUnFocus(Msg.MoveCollectionElements, 28))
                        _editingArrayOrder = array;
                }

                else if (icon.Done.ClickUnFocus(Msg.FinishMovingCollectionElements.GetText(), 28).nl(ref changed))
                    _editingArrayOrder = null;

                if (array != _editingArrayOrder) return changed;

                var derivedClasses = typeof(T).TryGetDerivedClasses();

                for (var i = 0; i < array.Length; i++)
                {

                    if (listMeta == null || listMeta.allowReorder)
                    {

                        if (i > 0)
                        {
                            if (icon.Up.ClickUnFocus("Move up").changes(ref changed))
                                QcSharp.Swap(ref array, i, i - 1);
                        }
                        else
                            icon.UpLast.write("Last");

                        if (i < array.Length - 1)
                        {
                            if (icon.Down.ClickUnFocus("Move down").changes(ref changed))
                                QcSharp.Swap(ref array, i, i + 1);
                        }
                        else icon.DownLast.write();
                    }

                    var el = array[i];

                    var isNull = el.IsNullOrDestroyed_Obj();

                    if (listMeta == null || listMeta.allowDelete)
                    {
                        if (!isNull && typeof(T).IsUnityObject())
                        {
                            if (icon.Delete.ClickUnFocus(Msg.MakeElementNull).changes(ref changed))
                                array[i] = default(T);
                        }
                        else
                        {
                            if (icon.Close.ClickUnFocus(Msg.RemoveFromCollection).changes(ref changed))
                            {
                                QcSharp.Remove(ref array, i);
                                i--;
                            }
                        }
                    }

                    if (!isNull && derivedClasses != null)
                    {
                        var ty = el.GetType();
                        if (@select(ref ty, derivedClasses, el.GetNameForInspector()))
                            array[i] = (el as ICfg).TryDecodeInto<T>(ty);
                    }

                    if (!isNull)
                        write(el.GetNameForInspector());
                    else
                        "{0} {1}".F(icon.Empty.GetText(), typeof(T).ToPegiStringType()).write();

                    nl();
                }

                return changed;
            }

            public bool edit_List_Order<T>(List<T> list, ListMetaData listMeta = null)
            {

                var changed = false;

                var sd = listMeta == null ? defaultSearchData : listMeta.searchData;

                if (list != collectionInspector.reordering)
                {
                    if (sd.filteredList != list && (listMeta == null || listMeta.showEditListButton) &&
                        icon.Edit.ClickUnFocus(Msg.MoveCollectionElements, 28))
                        reordering = list;
                }
                else if (icon.Done.ClickUnFocus(Msg.FinishMovingCollectionElements, 28).changes(ref changed))
                    reordering = null;

                if (list != collectionInspector.reordering) return changed;

#if UNITY_EDITOR
                if (!PaintingGameViewUI)
                {
                    nl();
                    ef.reorder_List(list, listMeta).changes(ref changed);
                }
                else
#endif

                #region Playtime UI reordering

                {
                    var derivedClasses = typeof(T).TryGetDerivedClasses();

                    foreach (var i in collectionInspector.InspectionIndexes(list, listMeta))
                    {

                        if (listMeta == null || listMeta.allowReorder)
                        {

                            if (i > 0)
                            {
                                if (icon.Up.ClickUnFocus("Move up").changes(ref changed))
                                    list.Swap(i - 1);

                            }
                            else
                                icon.UpLast.write("Last");

                            if (i < list.Count - 1)
                            {
                                if (icon.Down.ClickUnFocus("Move down").changes(ref changed))
                                    list.Swap(i);
                            }
                            else icon.DownLast.write();
                        }

                        var el = list[i];

                        var isNull = el.IsNullOrDestroyed_Obj();

                        if (listMeta == null || listMeta.allowDelete)
                        {

                            if (!isNull && typeof(T).IsUnityObject())
                            {
                                if (icon.Delete.ClickUnFocus(Msg.MakeElementNull))
                                    list[i] = default(T);
                            }
                            else
                            {
                                if (icon.Close.ClickUnFocus(Msg.RemoveFromCollection).changes(ref changed))
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
                            if (@select(ref ty, derivedClasses, el.GetNameForInspector()))
                                list[i] = (el as ICfg).TryDecodeInto<T>(ty);
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


                var duplicants = listMeta != null ? listMeta.allowDuplicants : allowDuplicants;

                if (list.Count > 1 && typeof(IGotIndex).IsAssignableFrom(typeof(T)))
                {

                    bool down = false;

                    if (icon.Down.Click("Sort Ascending").changes(ref down) || icon.Up.Click("Sort Descending"))
                    {
                        changed = true;

                        list.Sort((emp1, emp2) =>
                        {

                            var igc1 = emp1 as IGotIndex;
                            var igc2 = emp2 as IGotIndex;

                            if (igc1 == null || igc2 == null)
                                return 0;

                            return (down ? 1 : -1) * (igc1.IndexForPEGI - igc2.IndexForPEGI);

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
                    ;
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

                                    var istd = el as ICfg;

                                    if (istd != null)
                                        list.TryAdd(istd.CloneCfg());
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

                    if ((listMeta == null || listMeta.allowDelete) && list.Count > 0)
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

                    if ((listMeta == null || listMeta.allowDelete) && list.Count > 0)
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

                if (listMeta != null && icon.Config.enter(ref listMeta.inspectListMeta))
                    listMeta.Nested_Inspect();
                else if (typeof(Object).IsAssignableFrom(typeof(T)) || !listCopyBuffer.IsNullOrEmptyCollection())
                {
                    "Allow Duplicants".toggle("Will add elements to the list even if they are already there", 120, ref duplicants)
                        .changes(ref changed);

                    if (listMeta != null)
                        listMeta.allowDuplicants = duplicants;
                    else allowDuplicants = duplicants;
                }

                return changed;
            }

            public bool edit_List_Order_Obj<T>(List<T> list, ListMetaData listMeta = null) where T : Object
            {

                var changed = collectionInspector.edit_List_Order(list, listMeta);

                if (list != collectionInspector.reordering || listMeta == null) return changed;

                if (!icon.Search.ClickUnFocus("Find objects by GUID")) return changed;

                for (var i = 0; i < list.Count; i++)
                    if (list[i] == null)
                    {
                        var dta = listMeta.elementDatas.TryGet(i);
                        if (dta == null) continue;

                        T tmp = null;
                        if (dta.TryGetByGuid(ref tmp))
                            list[i] = tmp;

                    }

                return changed;
            }

            private IList listCopyBuffer;

            public object previouslyEntered;

            public bool InspectClassInList<T>(List<T> list, int index, ref int inspected, ListMetaData listMeta = null) where T : class
            {
                var el = list[index];
                var changed = false;

                var pl = el as IPEGI_ListInspect;
                var isPrevious = (listMeta != null && listMeta.previousInspected == index)
                                 || (listMeta == null && collectionInspector.previouslyEntered != null && el == collectionInspector.previouslyEntered);

                if (isPrevious)
                    SetBgColor(PreviousInspectedColor);

                if (pl != null)
                {
                    var chBefore = GUI.changed;
                    if (pl.InspectInList(list, index, ref inspected).changes(ref changed) || (!chBefore && GUI.changed))
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
                            if (ed.PEGI_inList<T>(ref elObj, index, ref inspected))
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

                        iind?.IndexForPEGI.ToString().write(20);

                        var named = el as IGotName;
                        if (named != null)
                        {
                            var so = uo as ScriptableObject;
                            var n = named.NameForPEGI;

                            if (so)
                            {
                                if (editDelayed(ref n).changes(ref changed))
                                {
                                    so.RenameAsset(n);
                                    named.NameForPEGI = n;
                                    isPrevious = true;
                                }
                            }
                            else if (edit(ref n).changes(ref changed))
                            {
                                named.NameForPEGI = n;
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
                                    Texture tex = uo as Texture;

                                    if (tex)
                                    {
                                        if (uo.ClickHighlight(tex))
                                            isPrevious = true;

                                        clickHighlightHandled = true;
                                    }
                                    else if (Try_NameInspect(uo).changes(ref changed))
                                        isPrevious = true;


                                }
                                else if (el.GetNameForInspector().ClickLabel("Inspect", RemainingLength(defaultButtonSize * 2 + 10)).changes(ref changed))
                                {
                                    inspected = index;
                                    isPrevious = true;
                                }
                            }
                        }

                        if ((warningText == null &&
                             (listMeta == null ? icon.Enter : listMeta.icon).ClickUnFocus(Msg.InspectElement)) ||
                            (warningText != null && icon.Warning.ClickUnFocus(warningText)))
                        {
                            inspected = index;
                            isPrevious = true;
                        }

                        if (!clickHighlightHandled && uo.ClickHighlight())
                            isPrevious = true;
                    }
                }

                RestoreBGcolor();

                if (listMeta != null)
                {
                    if (listMeta.inspected != -1)
                        listMeta.previousInspected = listMeta.inspected;
                    else if (isPrevious)
                        listMeta.previousInspected = index;

                }
                else if (isPrevious)
                    collectionInspector.previouslyEntered = el;

                return changed;
            }

            public bool isMonoType<T>(IList<T> list, int i)
            {
                if (!(typeof(MonoBehaviour)).IsAssignableFrom(typeof(T))) return false;

                GameObject mb = null;
                if (edit(ref mb))
                {
                    list[i] = mb.GetComponent<T>();
                    if (list[i] == null) GameView.ShowNotification(typeof(T).ToString() + " Component not found");
                }
                return true;

            }

            public bool ListAddNewClick<T>(List<T> list, ref T added, ListMetaData ld = null)
            {

                if (ld != null && !ld.showAddButton)
                    return false;

                var type = typeof(T);

                if (!type.IsNew())
                    return collectionInspector.ListAddEmptyClick(list, ld);

                if (type.TryGetClassAttribute<DerivedListAttribute>() != null || type is IGotClassTag)
                    return false;

                string name = null;

                var sd = ld == null ? defaultSearchData : ld.searchData;

                if (sd.filteredList == list)
                    name = sd.searchedText;

                if (icon.Add.ClickUnFocus(Msg.AddNewCollectionElement.GetText() + (name.IsNullOrEmpty() ? "" : " Named {0}".F(name))))
                {
                    if (typeof(T).IsSubclassOf(typeof(Object)))
                        list.Add(default(T));
                    else
                        added = name.IsNullOrEmpty() ? QcUtils.AddWithUniqueNameAndIndex(list) : QcUtils.AddWithUniqueNameAndIndex(list, name);

                    SkrollToBottom();

                    return true;
                }

                return false;
            }

            public bool ListAddEmptyClick<T>(IList<T> list, ListMetaData ld = null)
            {

                if (ld != null && !ld.showAddButton)
                    return false;

                var type = typeof(T);

                if (!type.IsUnityObject() && (type.TryGetClassAttribute<DerivedListAttribute>() != null || type is IGotClassTag))
                    return false;

                if (icon.Add.ClickUnFocus(Msg.AddNewCollectionElement.GetText()))
                {
                    list.Add(default(T));
                    collectionInspector.SkrollToBottom();
                    return true;
                }
                return false;
            }

        }

        public static bool InspectValueInDictionary<K, T>(ref T el, Dictionary<K, T> dic, int index, ref int inspected, ListMetaData listMeta = null)
        {

            var changed = InspectValueInCollection(ref el, null, index, ref inspected, listMeta);

            if (changed && typeof(T).IsValueType)
            {
                var pair = dic.ElementAt(index);

                dic[pair.Key] = el;
            }

            return changed;
        }

        public static bool InspectValueInArray<T>(ref T el, T[] array, int index, ref int inspected, ListMetaData listMeta = null)
        {

            var changed = InspectValueInCollection(ref el, array, index, ref inspected, listMeta);

            if (changed && typeof(T).IsValueType)
                array[index] = el;

            return changed;
        }

        public static bool InspectValueInList<T>(ref T el, List<T> list, int index, ref int inspected,
            ListMetaData listMeta = null)
        {

            var changed = InspectValueInCollection(ref el, list, index, ref inspected, listMeta);

            if (changed && typeof(T).IsValueType)
                list[index] = el;

            return changed;

        }

        public static bool InspectValueInCollection<T>(ref T el, IList collection, int index, ref int inspected, ListMetaData listMeta = null)
        {

            var changed = false;

            var pl = el as IPEGI_ListInspect;

            var isPrevious = (listMeta != null && listMeta.previousInspected == index);

            if (isPrevious)
                SetBgColor(PreviousInspectedColor);

            if (pl != null)
            {
                var chBefore = GUI.changed;
                if ((pl.InspectInList(collection, index, ref inspected).changes(ref changed) || (!chBefore && GUI.changed)) && (typeof(T).IsValueType))
                    el = (T)pl;

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
                        object obj = (object)el;

                        if (ed.PEGI_inList<T>(ref obj, index, ref inspected))
                        {
                            el = (T)obj;
                            isPrevious = true;
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

                    iind?.IndexForPEGI.ToString().write(20);

                    var named = el as IGotName;
                    if (named != null)
                    {
                        var so = uo as ScriptableObject;
                        var n = named.NameForPEGI;

                        if (so)
                        {
                            if (editDelayed(ref n).changes(ref changed))
                            {
                                so.RenameAsset(n);
                                named.NameForPEGI = n;
                                isPrevious = true;
                            }
                        }
                        else if (edit(ref n).changes(ref changed))
                        {
                            named.NameForPEGI = n;
                            if (typeof(T).IsValueType)
                                el = (T)named;

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
                                Texture tex = uo as Texture;

                                if (tex)
                                {
                                    if (uo.ClickHighlight(tex))
                                        isPrevious = true;

                                    clickHighlightHandled = true;
                                }
                                else if (Try_NameInspect(uo).changes(ref changed))
                                    isPrevious = true;


                            }
                            else if (el.GetNameForInspector().ClickLabel("Inspect", RemainingLength(defaultButtonSize * 2 + 10)).changes(ref changed))
                            {
                                inspected = index;
                                isPrevious = true;
                            }
                        }
                    }

                    if ((warningText == null &&
                         (listMeta == null ? icon.Enter : listMeta.icon).ClickUnFocus(Msg.InspectElement)) ||
                        (warningText != null && icon.Warning.ClickUnFocus(warningText)))
                    {
                        inspected = index;
                        isPrevious = true;
                    }

                    if (!clickHighlightHandled && uo.ClickHighlight())
                        isPrevious = true;
                }
            }

            RestoreBGcolor();

            if (listMeta != null)
            {
                if (listMeta.inspected != -1)
                    listMeta.previousInspected = listMeta.inspected;
                else if (isPrevious)
                    listMeta.previousInspected = index;

            }
            else if (isPrevious)
                collectionInspector.previouslyEntered = el;

            return changed;
        }



        #endregion
        
        #region LISTS

        #region List of MonoBehaviour


        public static bool edit_List_MB<T>(this string label, ref List<T> list, ref int inspected) where T : MonoBehaviour
        {
            collectionInspector.write_Search_ListLabel(label, ref inspected, list);
            var changed = false;
            edit_List_MB(ref list, ref inspected, ref changed).listLabel_Used();
            return changed;
        }

        public static bool edit_List_MB<T>(this ListMetaData metaDatas, ref List<T> list) where T : MonoBehaviour
        {
            collectionInspector.write_Search_ListLabel(metaDatas, list);
            bool changed = false;
            edit_List_MB(ref list, ref metaDatas.inspected, ref changed, metaDatas).listLabel_Used();
            return changed;
        }

        public static T edit_List_MB<T>(ref List<T> list, ref int inspected, ref bool changed, ListMetaData listMeta = null) where T : MonoBehaviour
        {

            if (collectionInspector.listIsNull(ref list))
                return null;

            var added = default(T);

            var before = inspected;

            list.ClampIndexToCount(ref inspected, -1);

            changed |= (inspected != before);

            if (inspected == -1)
            {
                collectionInspector.ListAddEmptyClick(list, listMeta).changes(ref changed);

                if (listMeta != null && icon.Save.ClickUnFocus("Save GUID & Names data to ListMeta"))
                    listMeta.SaveElementDataFrom(list);

                collectionInspector.edit_List_Order_Obj(list, listMeta).changes(ref changed);

                if (list != collectionInspector.reordering)
                {

                    foreach (var i in collectionInspector.InspectionIndexes(list, listMeta))
                    {

                        var el = list[i];
                        if (!el)
                        {
                            T obj = null;

                            if (listMeta.TryInspect(ref obj, i))
                            {
                                if (obj)
                                {
                                    list[i] = obj.GetComponent<T>();
                                    if (!list[i]) GameView.ShowNotification(typeof(T).ToString() + " Component not found");
                                }
                            }
                        }
                        else
                            collectionInspector.InspectClassInList(list, i, ref inspected, listMeta).changes(ref changed);

                        nl();
                    }
                }
                else
                    collectionInspector.list_DropOption(list, listMeta);

            }
            else collectionInspector.ExitOrDrawPEGI(list, ref inspected).changes(ref changed);

            nl();

            return added;
        }

        #endregion

        #region List of ScriptableObjects

        public static T edit_List_SO<T>(this string label, ref List<T> list, ref int inspected, ref bool changed) where T : ScriptableObject
        {
            collectionInspector.write_Search_ListLabel(label, ref inspected, list);

            return edit_List_SO(ref list, ref inspected, ref changed).listLabel_Used();
        }

        public static bool edit_List_SO<T>(ref List<T> list, ref int inspected) where T : ScriptableObject
        {
            var changed = false;

            edit_List_SO<T>(ref list, ref inspected, ref changed);

            return changed;
        }

        public static bool edit_List_SO<T>(this string label, ref List<T> list, ref int inspected) where T : ScriptableObject
        {
            collectionInspector.write_Search_ListLabel(label, ref inspected, list);

            var changed = false;

            edit_List_SO<T>(ref list, ref inspected, ref changed).listLabel_Used();

            return changed;
        }

        public static bool edit_List_SO<T>(this string label, ref List<T> list) where T : ScriptableObject
        {
            collectionInspector.write_Search_ListLabel(label, list);

            var changed = false;

            var edited = -1;

            edit_List_SO<T>(ref list, ref edited, ref changed).listLabel_Used();

            return changed;
        }

        public static bool edit_List_SO<T>(this ListMetaData listMeta, ref List<T> list) where T : ScriptableObject
        {
            collectionInspector.write_Search_ListLabel(listMeta, list);

            var changed = false;

            edit_List_SO(ref list, ref listMeta.inspected, ref changed, listMeta).listLabel_Used();

            return changed;
        }

        public static T edit_List_SO<T>(ref List<T> list, ref int inspected, ref bool changed, ListMetaData listMeta = null) where T : ScriptableObject
        {
            if (collectionInspector.listIsNull(ref list))
                return null;

            var added = default(T);

            var before = inspected;
            if (list.ClampIndexToCount(ref inspected, -1))
                changed |= (inspected != before);

            if (inspected == -1)
            {

                collectionInspector.edit_List_Order_Obj(list, listMeta).changes(ref changed);

                collectionInspector.ListAddEmptyClick(list, listMeta).changes(ref changed);

                if (listMeta != null && icon.Save.ClickUnFocus("Save GUID & Names to ListMeta"))
                    listMeta.SaveElementDataFrom(list);

                if (list != collectionInspector.reordering)
                {
                    foreach (var i in collectionInspector.InspectionIndexes(list, listMeta))
                    {
                        var el = list[i];
                        if (!el)
                        {
                            if (listMeta.TryInspect(ref el, i).nl(ref changed))
                                list[i] = el;

                        }
                        else
                            collectionInspector.InspectClassInList(list, i, ref inspected, listMeta).nl(ref changed);

                    }

                    if (typeof(T).TryGetDerivedClasses() != null)
                        collectionInspector.PEGI_InstantiateOptions_SO(list, ref added, listMeta).nl(ref changed);

                    nl();

                }
                else collectionInspector.list_DropOption(list, listMeta);
            }
            else collectionInspector.ExitOrDrawPEGI(list, ref inspected).changes(ref changed);

            nl();
            return added;
        }

        #endregion

        #region List of Unity Objects

        public static bool edit_List_UObj<T>(this string label, ref List<T> list, ref int inspected, List<T> selectFrom = null) where T : UnityEngine.Object
        {
            collectionInspector.write_Search_ListLabel(label, ref inspected, list);
            return edit_or_select_List_UObj(ref list, selectFrom, ref inspected);
        }

        public static bool edit_List_UObj<T>(ref List<T> list, ref int inspected, List<T> selectFrom = null) where T : Object
            => edit_or_select_List_UObj(ref list, selectFrom, ref inspected);

        public static bool edit_List_UObj<T>(this string label, ref List<T> list, List<T> selectFrom = null) where T : Object
        {
            collectionInspector.write_Search_ListLabel(label, list);
            return list.edit_List_UObj(selectFrom).listLabel_Used();
        }

        public static bool edit_List_UObj<T>(this List<T> list, List<T> selectFrom = null) where T : Object
        {
            var edited = -1;
            return edit_or_select_List_UObj(ref list, selectFrom, ref edited);
        }

        public static bool edit_List_UObj<T>(this ListMetaData listMeta, ref List<T> list, List<T> selectFrom = null) where T : Object
        {
            collectionInspector.write_Search_ListLabel(listMeta, list);
            return edit_or_select_List_UObj(ref list, selectFrom, ref listMeta.inspected, listMeta).listLabel_Used();
        }

        public static bool edit_List_UObj<T>(this string label, ref List<T> list, Func<T, T> lambda) where T : Object
        {
            collectionInspector.write_Search_ListLabel(label, list);
            return edit_List_UObj(ref list, lambda);
        }

        public static bool edit_List_UObj<T>(ref List<T> list, Func<T, T> lambda) where T : Object
        {

            var changed = false;

            if (collectionInspector.listIsNull(ref list))
                return changed;

            collectionInspector.edit_List_Order(list).changes(ref changed);
            //collectionInspector.
            if (list != collectionInspector.reordering)
            {

                collectionInspector.ListAddEmptyClick(list).changes(ref changed);

                foreach (var i in collectionInspector.InspectionIndexes(list))
                {
                    var el = list[i];
                    var ch = GUI.changed;
                    el = lambda(el);
                    nl();
                    if (ch || !GUI.changed) continue;

                    changed = true;
                    list[i] = el;
                }

            }

            nl();
            return changed;
        }

        public static bool edit_or_select_List_UObj<T, G>(ref List<T> list, List<G> from, ref int inspected, ListMetaData listMeta = null) where T : G where G : Object
        {
            if (collectionInspector.listIsNull(ref list))
                return false;

            var changed = false;

            var before = inspected;
            if (list.ClampIndexToCount(ref inspected, -1))
                changed |= (inspected != before);

            if (inspected == -1)
            {

                if (listMeta != null && icon.Save.ClickUnFocus("Save GUID & Names to List MEta"))
                    listMeta.SaveElementDataFrom(list);

                collectionInspector.edit_List_Order(list, listMeta).changes(ref changed);

                if (list != collectionInspector.reordering)
                {
                    collectionInspector.ListAddEmptyClick(list, listMeta).changes(ref changed);

                    foreach (var i in collectionInspector.InspectionIndexes(list, listMeta))
                    {
                        var el = list[i];
                        if (!el)
                        {
                            if (!from.IsNullOrEmpty() && select_SameClass(ref el, from))
                                list[i] = el;

                            if (listMeta.TryInspect(ref el, i).changes(ref changed))
                                list[i] = el;
                        }
                        else
                            collectionInspector.InspectClassInList(list, i, ref inspected, listMeta).changes(ref changed);

                        nl();
                    }
                }
                else
                    collectionInspector.list_DropOption(list, listMeta);

            }
            else collectionInspector.ExitOrDrawPEGI(list, ref inspected).changes(ref changed);

            nl();
            return changed;

        }

        #endregion

        #region List of New()

        public static T edit<T>(this ListMetaData ld, ref List<T> list, ref bool changed)
        {
            collectionInspector.write_Search_ListLabel(ld, list);
            return edit_List(ref list, ref ld.inspected, ref changed, ld).listLabel_Used();
        }

        public static bool edit_List<T>(this string label, ref List<T> list, ref int inspected)
        {
            collectionInspector.write_Search_ListLabel(label, ref inspected, list);
            return edit_List(ref list, ref inspected).listLabel_Used();
        }

        public static bool edit_List<T>(ref List<T> list, ref int inspected)
        {
            var changes = false;
            edit_List(ref list, ref inspected, ref changes);
            return changes;
        }

        public static bool edit_List<T>(this string label, ref List<T> list)
        {
            collectionInspector.write_Search_ListLabel(label, list);
            return edit_List(ref list).listLabel_Used();
        }

        public static bool edit_List<T>(ref List<T> list)
        {
            var edited = -1;
            var changes = false;
            edit_List(ref list, ref edited, ref changes);
            return changes;
        }

        public static T edit_List<T>(this string label, ref List<T> list, ref bool changed)
        {
            collectionInspector.write_Search_ListLabel(label, list);
            return edit_List(ref list, ref changed).listLabel_Used();
        }

        public static T edit_List<T>(ref List<T> list, ref bool changed)
        {
            var edited = -1;
            return edit_List(ref list, ref edited, ref changed);
        }

        public static T edit_List<T>(this string label, ref List<T> list, ref int inspected, ref bool changed)
        {
            collectionInspector.write_Search_ListLabel(label, ref inspected, list);
            return edit_List(ref list, ref inspected, ref changed).listLabel_Used();
        }

        public static bool edit_List<T>(this ListMetaData listMeta, ref List<T> list)
        {

            collectionInspector.write_Search_ListLabel(listMeta, list);
            var changed = false;
            edit_List(ref list, ref listMeta.inspected, ref changed, listMeta);
            collectionInspector.listLabel_Used();
            return changed;
        }

        public static T edit_List<T>(this ListMetaData listMeta, ref List<T> list, ref bool changed)
        {

            collectionInspector.write_Search_ListLabel(listMeta, list);
            var ret = edit_List(ref list, ref listMeta.inspected, ref changed, listMeta);
            collectionInspector.listLabel_Used();
            return ret;
        }

        public static T edit_List<T>(ref List<T> list, ref int inspected, ref bool changed, ListMetaData listMeta = null)
        {

            var added = default(T);

            if (list == null)
            {
                if (Msg.Init.F(Msg.List).ClickUnFocus().nl())
                    list = new List<T>();
                else
                    return added;
            }

            if (inspected >= list.Count)
            {
                inspected = -1;
                changed = true;
            }

            if (inspected == -1)
            {

                collectionInspector.edit_List_Order(list, listMeta).changes(ref changed);

                if (list != collectionInspector.reordering)
                {

                    collectionInspector.ListAddNewClick(list, ref added, listMeta).changes(ref changed);

                    foreach (var i in collectionInspector.InspectionIndexes(list, listMeta))
                    {

                        var el = list[i];
                        if (el.IsNullOrDestroyed_Obj())
                        {
                            if (!collectionInspector.isMonoType(list, i))
                            {
                                write(typeof(T).IsSubclassOf(typeof(Object))
                                    ? "use edit_List_UObj"
                                    : "is NUll");
                            }
                        }
                        else
                            InspectValueInList(ref el, list, i, ref inspected, listMeta).changes(ref changed);


                        nl();
                    }

                    collectionInspector.PEGI_InstantiateOptions(list, ref added, listMeta).nl(ref changed);
                }
            }
            else collectionInspector.ExitOrDrawPEGI(list, ref inspected).changes(ref changed);

            nl();
            return added;
        }

        #region Tagged Types

        public static T edit_List<T>(this ListMetaData listMeta, ref List<T> list, TaggedTypesCfg types, ref bool changed)
        {
            collectionInspector.write_Search_ListLabel(listMeta, list);
            var ret = edit_List(ref list, ref listMeta.inspected, types, ref changed, listMeta);
            collectionInspector.listLabel_Used();
            return ret;
        }

        public static bool edit_List<T>(this ListMetaData listMeta, ref List<T> list, TaggedTypesCfg types)
        {
            bool changed = false;
            collectionInspector.write_Search_ListLabel(listMeta, list);

            edit_List(ref list, ref listMeta.inspected, types, ref changed, listMeta).listLabel_Used();
            return changed;
        }

        public static T edit_List<T>(this string label, ref List<T> list, ref int inspected, TaggedTypesCfg types, ref bool changed)
        {
            collectionInspector.write_Search_ListLabel(label, ref inspected, list);
            return edit_List(ref list, ref inspected, types, ref changed).listLabel_Used();
        }

        private static T edit_List<T>(ref List<T> list, ref int inspected, TaggedTypesCfg types, ref bool changed, ListMetaData listMeta = null)
        {

            var added = default(T);

            if (list == null)
            {
                if (Msg.Init.F(Msg.List).ClickUnFocus().nl())
                    list = new List<T>();
                else
                    return added;
            }

            if (inspected >= list.Count)
            {
                inspected = -1;
                changed = true;
            }

            if (inspected == -1)
            {

                changed |= collectionInspector.edit_List_Order(list, listMeta);

                if (list != collectionInspector.reordering)
                {

                    foreach (var i in collectionInspector.InspectionIndexes(list, listMeta))
                    {

                        var el = list[i];
                        if (el == null)
                        {

                            if (!collectionInspector.isMonoType(list, i))
                            {
                                write(typeof(T).IsSubclassOf(typeof(Object))
                                    ? "use edit_List_UObj"
                                    : "is NUll");
                            }
                        }
                        else
                            InspectValueInList(ref el, list, i, ref inspected, listMeta).changes(ref changed);

                        nl();
                    }

                    collectionInspector.PEGI_InstantiateOptions(list, ref added, types, listMeta).nl(ref changed);
                }
            }
            else collectionInspector.ExitOrDrawPEGI(list, ref inspected).changes(ref changed);

            nl();
            return added;
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

        private static T lambda_Obj_role<T>(T val) where T : UnityEngine.Object
        {

            var role = listElementsRoles.TryGetObj(collectionInspector.Index);
            if (!role.IsNullOrDestroyed_Obj())
                role.GetNameForInspector().edit(90, ref val);
            else edit(ref val);

            return val;
        }

        public static bool edit_List(this string label, ref List<int> list) =>
            label.edit_List(ref list, lambda_int);

        public static bool edit_List(this string label, ref List<Color> list) =>
            label.edit_List(ref list, lambda_Color);

        public static bool edit_List(this string label, ref List<Color32> list) =>
            label.edit_List(ref list, lambda_Color);

        public static bool edit_List(this string label, ref List<string> list) =>
            label.edit_List(ref list, lambda_string);

        public static bool edit_List_WithRoles(this string label, ref List<string> list, IList roles)
        {
            listElementsRoles = roles;
            return label.edit_List(ref list, lambda_string_role);
        }

        public static bool edit_List_WithRoles<T>(this string label, ref List<T> list, IList roles) where T : UnityEngine.Object
        {
            collectionInspector.write_Search_ListLabel(label, list);
            listElementsRoles = roles;
            var ret = edit_List_UObj(ref list, lambda_Obj_role);
            listElementsRoles = null;
            return ret;
        }

        #endregion

        public static T edit_List<T>(this string label, ref List<T> list, ref bool changed, Func<T, T> lambda) where T : new()
        {
            collectionInspector.write_Search_ListLabel(label, list);
            return edit_List(ref list, ref changed, lambda).listLabel_Used();
        }

        public static T edit_List<T>(ref List<T> list, ref bool changed, Func<T, T> lambda) where T : new()
        {

            var added = default(T);

            if (collectionInspector.listIsNull(ref list))
                return added;

            collectionInspector.edit_List_Order(list).changes(ref changed);

            if (list != collectionInspector.reordering)
            {

                collectionInspector.ListAddNewClick(list, ref added).changes(ref changed);

                foreach (var i in collectionInspector.InspectionIndexes(list))
                {
                    var el = list[i];

                    var ch = GUI.changed;
                    el = lambda(el);
                    if (!ch && GUI.changed)
                    {
                        list[i] = el;
                        changed = true;
                    }

                    nl();
                }

            }

            nl();
            return added;
        }

        public static bool edit_List<T>(this string label, ref List<T> list, Func<T, T> lambda) where T : new()
        {
            collectionInspector.write_Search_ListLabel(label, list);
            return edit_List(ref list, lambda).listLabel_Used();
        }

        public static T edit_List<T>(this string label, ref List<T> list, Func<T, T> lambda, ref bool changed) where T : new()
        {
            collectionInspector.write_Search_ListLabel(label, list);
            return edit_List(ref list, lambda, ref changed).listLabel_Used();
        }

        public static bool edit_List<T>(ref List<T> list, Func<T, T> lambda) where T : new()
        {
            var changed = false;
            edit_List(ref list, lambda, ref changed);
            return changed;

        }

        public static T edit_List<T>(ref List<T> list, Func<T, T> lambda, ref bool changed) where T : new()
        {
            var added = default(T);

            if (collectionInspector.listIsNull(ref list))
                return added;

            collectionInspector.edit_List_Order(list).changes(ref changed);

            if (list != collectionInspector.reordering)
            {

                collectionInspector.ListAddNewClick(list, ref added).changes(ref changed);

                foreach (var i in collectionInspector.InspectionIndexes(list))
                {
                    var el = list[i];
                    var ch = GUI.changed;
                    el = lambda(el);
                    nl();
                    if (ch || !GUI.changed) continue;
                    list[i] = el;
                    changed = true;

                }
            }

            nl();
            return added;
        }

        public static bool edit_List(this string name, ref List<string> list, Func<string, string> lambda)
        {
            collectionInspector.write_Search_ListLabel(name, list);
            return edit_List(ref list, lambda).listLabel_Used();
        }

        public static bool edit_List(ref List<string> list, Func<string, string> lambda)
        {

            if (collectionInspector.listIsNull(ref list))
                return false;

            var changed = collectionInspector.edit_List_Order(list);

            if (list != collectionInspector.reordering)
            {
                if (icon.Add.ClickUnFocus().changes(ref changed))
                {
                    list.Add("");
                    collectionInspector.SkrollToBottom();
                }

                foreach (var i in collectionInspector.InspectionIndexes(list))
                {
                    var el = list[i];

                    var ch = GUI.changed;
                    el = lambda(el);
                    nl();
                    if (ch || !GUI.changed) continue;

                    changed = true;
                    list[i] = el;
                }

            }

            nl();
            return changed;
        }

        #endregion

        #region List of Not New()

        public static bool write_List<T>(this string label, List<T> list, Func<T, bool> lambda)
        {
            collectionInspector.write_Search_ListLabel(label, list);
            return list.write_List(lambda).listLabel_Used();

        }

        public static bool write_List<T>(this List<T> list, Func<T, bool> lambda)
        {

            if (list == null)
            {
                "Empty List".nl();
                return false;
            }

            var changed = false;

            foreach (var i in collectionInspector.InspectionIndexes(list))
                lambda(list[i]).nl(ref changed);

            nl();

            return changed;
        }

        public static bool write_List<T>(this string label, List<T> list)
        {
            var edited = -1;
            collectionInspector.write_Search_ListLabel(label, list);
            return list.write_List<T>(ref edited).listLabel_Used();
        }

        public static bool write_List<T>(this string label, List<T> list, ref int inspected)
        {
            nl();
            collectionInspector.write_Search_ListLabel(label, ref inspected, list);

            return list.write_List<T>(ref inspected).listLabel_Used();
        }

        public static bool write_List<T>(this List<T> list, ref int edited)
        {
            var changed = false;

            var before = edited;

            list.ClampIndexToCount(ref edited, -1);

            changed |= (edited != before);

            if (edited == -1)
            {
                nl();

                for (var i = 0; i < list.Count; i++)
                {

                    var el = list[i];
                    if (el == null)
                        write("NULL");
                    else
                        InspectValueInList(ref el, list, i, ref edited).changes(ref changed);

                    nl();
                }
            }
            else
                collectionInspector.ExitOrDrawPEGI(list, ref edited).changes(ref changed);


            nl();
            return changed;
        }

        #endregion

        #endregion

        #region Dictionary Generic

        public static bool edit_Dictionary_Values<G, T>(this string label, Dictionary<G, T> dic, bool showKey = false)
        {
            int inspected = -1;
            collectionInspector.write_Search_ListLabel(label, dic);
            return edit_Dictionary_Values_Internal(dic, ref inspected, showKey: showKey);
        }

        public static bool edit_Dictionary_Values<G, T>(this string label, Dictionary<G, T> dic, ref int inspected, bool showKey = false)
        {
            collectionInspector.write_Search_ListLabel(label, ref inspected, dic);
            return edit_Dictionary_Values_Internal(dic, ref inspected, showKey: showKey);
        }

        public static bool edit_Dictionary_Values<G, T>(this string label, Dictionary<G, T> dic, Func<T, T> lambda, bool showKey = true)
        {
            collectionInspector.write_Search_ListLabel(label, dic);
            return edit_Dictionary_Values_Internal(dic, lambda, showKey: showKey);
        }

        public static bool edit_Dictionary_Values<G, T>(this ListMetaData listMeta, Dictionary<G, T> dic, bool showKey = true)
        {
            collectionInspector.write_Search_ListLabel(listMeta, dic);
            return edit_Dictionary_Values_Internal(dic, ref listMeta.inspected, showKey: showKey, listMeta);
        }

        public static bool edit_Dictionary_Values<G, T>(this ListMetaData listMeta, Dictionary<G, T> dic, Func<T, T> lambda)
        {
            collectionInspector.write_Search_ListLabel(listMeta, dic);
            return edit_Dictionary_Values_Internal(dic, lambda, listMeta: listMeta);
        }

        private static bool edit_Dictionary_Values_Internal<G, T>(Dictionary<G, T> dic, Func<T, T> lambda, bool showKey = true, ListMetaData listMeta = null)
        {

            if (dic == null)
            {
                "Dictionary is null".writeHint();
                return false;
            }

            nl();

            if (listMeta != null)
                showKey = listMeta.showDictionaryKey;

            var changed = false;

            if (listMeta != null && listMeta.Inspecting)
            {

                if (icon.Exit.Click("Exit " + listMeta.label))
                    listMeta.Inspecting = false;

                if (listMeta.Inspecting && (dic.Count > listMeta.inspected))
                {

                    var el = dic.ElementAt(listMeta.inspected);

                    var val = el.Value;

                    var ch = GUI.changed;

                    Try_Nested_Inspect(val);

                    if ((!ch && GUI.changed).changes(ref changed))
                        dic[el.Key] = val;
                }
            }
            else
            {

                foreach (var i in collectionInspector.InspectionIndexes(dic, listMeta))
                {

                    var item = dic.ElementAt(i);
                    var itemKey = item.Key;

                    collectionInspector.Index = i;

                    if ((listMeta == null || listMeta.allowDelete) && icon.Delete.ClickUnFocus(25).changes(ref changed))
                        dic.Remove(itemKey);
                    else
                    {
                        if (showKey)
                            itemKey.GetNameForInspector().write_ForCopy(50);

                        var el = item.Value;
                        var ch = GUI.changed;
                        el = lambda(el);

                        if ((!ch && GUI.changed).changes(ref changed))
                            dic[itemKey] = el;

                        if (listMeta != null && icon.Enter.Click("Enter " + el.ToString()))
                            listMeta.inspected = i;
                    }

                    nl();

                }

            }

            return changed;
        }

        private static bool edit_Dictionary_Values_Internal<G, T>(Dictionary<G, T> dic, ref int inspected, bool showKey, ListMetaData listMeta = null)
        {
            bool changed = false;

            nl();

            int before = inspected;
            inspected = Mathf.Clamp(inspected, -1, dic.Count - 1);
            changed |= (inspected != before);
            
            if (inspected == -1)
            {

                if (listMeta != null)
                    showKey = listMeta.showDictionaryKey;

                foreach (var i in collectionInspector.InspectionIndexes(dic, listMeta))
                {

                    var item = dic.ElementAt(i);
                    var itemKey = item.Key;
                    collectionInspector.Index = i;
                    
                    if ((listMeta == null || listMeta.allowDelete) && icon.Delete.ClickUnFocus(25).changes(ref changed))
                    {
                        dic.Remove(itemKey);
                    }
                    else
                    {
                        if (showKey)
                            itemKey.GetNameForInspector().write_ForCopy(50);

                        var el = item.Value;
                        InspectValueInDictionary(ref el, dic, i, ref inspected, listMeta).changes(ref changed);
                    }
                    nl();
                }
            }
            else
                collectionInspector.ExitOrDrawPEGI(dic, ref inspected).changes(ref changed);

            nl();
            return changed;
        }

        private static bool dicIsNull<G, T>(ref Dictionary<G, T> dic)
        {
            if (dic != null)
                return false;

            if (Msg.Init.F(Msg.Dictionary).ClickUnFocus().nl())
            {
                dic = new Dictionary<G, T>();
                return false;
            }
            else
                return true;

        }

        #endregion

        #region Dictionary <Key,String>

        public static bool edit_Dictionary_Values(this string label, Dictionary<int, string> dic, List<string> roles)
        {
            collectionInspector.write_Search_ListLabel(label, dic);
            listElementsRoles = roles;
            var ret = edit_Dictionary_Values_Internal(dic, lambda_string_role, false);
            listElementsRoles = null;
            return ret;
        }

        public static bool edit_Dictionary_Values(this string label, Dictionary<string, string> dic)
        {
            collectionInspector.write_Search_ListLabel(label, dic);
            return edit_Dictionary_Values_Internal(dic, lambda_string);
        }
        
        public static bool edit_Dictionary_Values(this ListMetaData listMeta, Dictionary<string, string> dic)
        {
            collectionInspector.write_Search_ListLabel(listMeta, dic);
            return edit_Dictionary_Values_Internal(dic, lambda_string, listMeta: listMeta);
        }
        
        public static bool edit_Dictionary(this string label, ref Dictionary<int, string> dic)
        {
            collectionInspector.write_Search_ListLabel(label, dic);
            return edit_Dictionary(ref dic);
        }
        
        private static bool edit_Dictionary(ref Dictionary<int, string> dic)
        {

            bool changed = false;

            if (dicIsNull(ref dic))
                return changed;

            foreach (var i in collectionInspector.InspectionIndexes(dic))
            {

                var e = dic.ElementAt(i);
                collectionInspector.Index = e.Key;

                if (icon.Delete.ClickUnFocus(20))
                    dic.Remove(e.Key).changes(ref changed);
                else
                {
                    if (!editKey(ref dic, e.Key).changes(ref changed))
                        edit(ref dic, e.Key).changes(ref changed);
                }
                nl();
            }
            nl();

            dic.newElement().changes(ref changed);

            return changed;
        }

        private static bool edit(ref Dictionary<int, string> dic, int atKey)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref dic, atKey);
#endif

            var val = dic[atKey];
            if (editDelayed(ref val, 40))
            {
                dic[atKey] = val;
                return false;
            }

            return false;

        }

        private static bool editKey(ref Dictionary<int, string> dic, int key)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.editKey(ref dic, key);

#endif

            checkLine();
            int pre = key;
            if (editDelayed(ref key, 40))
                return dic.TryChangeKey(pre, key);

            return false;

        }

        private static string newEnumName = "UNNAMED";
        private static int newEnumKey = 1;

        private static bool newElement(this Dictionary<int, string> dic)
        {
            bool changed = false;
            nl();

            "______New [Key, Value]".nl();
            edit(ref newEnumKey, 50).changes(ref changed);
            edit(ref newEnumName).changes(ref changed);

            string dummy;
            var isNewIndex = !dic.TryGetValue(newEnumKey, out dummy);
            var isNewValue = !dic.ContainsValue(newEnumName);

            if (isNewIndex && isNewValue && icon.Add.ClickUnFocus(Msg.AddNewCollectionElement, 25).changes(ref changed))
            {
                dic.Add(newEnumKey, newEnumName);
                newEnumKey = 1;
                string ddm;
                while (dic.TryGetValue(newEnumKey, out ddm))
                    newEnumKey++;
                newEnumName = "UNNAMED";
            }

            if (!isNewIndex)
                "Index Takken by {0}".F(dummy).write();
            else if (!isNewValue)
                "Value already assigned ".write();

            nl();

            return changed;
        }

        #endregion

        #region Arrays

        public static bool edit_Array<T>(this string label, ref T[] array)
        {
            int inspected = -1;
            collectionInspector.write_Search_ListLabel(label, array);
            return edit_Array(ref array, ref inspected).listLabel_Used();
        }

        public static bool edit_Array<T>(this string label, ref T[] array, ref int inspected)
        {
            collectionInspector.write_Search_ListLabel(label, ref inspected, array);
            return edit_Array(ref array, ref inspected).listLabel_Used();
        }

        public static bool edit_Array<T>(ref T[] array, ref int inspected)
        {
            bool changes = false;
            edit_Array(ref array, ref inspected, ref changes);
            return changes;
        }

        public static T edit_Array<T>(ref T[] array, ref int inspected, ref bool changed, ListMetaData metaDatas = null)
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

                collectionInspector.ExitOrDrawPEGI(array, ref inspected).changes(ref changed);

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

                collectionInspector.edit_Array_Order(ref array, metaDatas).nl(ref changed);

                if (array == collectionInspector._editingArrayOrder) return added;

                for (var i = 0; i < array.Length; i++)
                {
                    var el = array[i];
                    if (InspectValueInArray(ref el, array, i, ref inspected, metaDatas).nl(ref changed) &&
                        typeof(T).IsValueType)
                        array[i] = el;
                }
            }

            return added;
        }

        #endregion
        
        #region Searching

        public static bool SearchMatch_ObjectList(this IEnumerable list, string searchText) => list.Cast<object>().Any(e => Try_SearchMatch_Obj(e, searchText));

        public static bool Try_SearchMatch_Obj(object obj, string searchText) => SearchMatch_Obj_Internal(obj, new string[] { searchText });

        private static bool SearchMatch_Obj_Internal(this object obj, string[] text, int[] indexes = null)
        {

            if (obj.IsNullOrDestroyed_Obj())
                return false;

            var go = QcUnity.TryGetGameObjectFromObj(obj);

            var matched = new bool[text.Length];

            if (go)
            {

                if (go.TryGet<IPEGI_Searchable>().SearchMatch_Internal(text, ref matched))
                    return true;

                if (go.TryGet<IGotName>().SearchMatch_Internal(text, ref matched))
                    return true;

                if (go.TryGet<IGotDisplayName>().SearchMatch_Internal(text, ref matched))
                    return true;

                if (go.name.SearchMatch_Internal(text, ref matched))
                    return true;

                if (go.TryGet<INeedAttention>().SearchMatch_Internal(text, ref matched))
                    return true;

                if (!indexes.IsNullOrEmpty() && go.TryGet<IGotIndex>().SearchMatch_Internal(indexes))
                    return true;

            }
            else
            {

                if ((QcUnity.TryGet_fromObj<IPEGI_Searchable>(obj)).SearchMatch_Internal(text, ref matched))
                    return true;

                if (QcUnity.TryGet_fromObj<IGotName>(obj).SearchMatch_Internal(text, ref matched))
                    return true;

                if (QcUnity.TryGet_fromObj<IGotDisplayName>(obj).SearchMatch_Internal(text, ref matched))
                    return true;

                if (QcUnity.TryGet_fromObj<INeedAttention>(obj).SearchMatch_Internal(text, ref matched))
                    return true;

                if (obj.ToString().SearchMatch_Internal(text, ref matched))
                    return true;

                if (!indexes.IsNullOrEmpty() && go.TryGet<IGotIndex>().SearchMatch_Internal(indexes))
                    return true;
            }

            return false;
        }

        private static bool SearchMatch_Internal(this IPEGI_Searchable searchable, string[] text, ref bool[] matched)
        {
            if (searchable == null) return false;

            var fullMatch = true;

            for (var i = 0; i < text.Length; i++)
                if (!matched[i])
                {
                    if (!searchable.String_SearchMatch(text[i]))
                        fullMatch = false;
                    else
                        matched[i] = true;
                }

            return fullMatch;

        }

        private static bool SearchMatch_Internal(this INeedAttention needAttention, string[] text, ref bool[] matched)
            => needAttention?.NeedAttention().SearchMatch_Internal(text, ref matched) ?? false;

        private static bool SearchMatch_Internal(this IGotName gotName, string[] text, ref bool[] matched)
            => gotName?.NameForPEGI.SearchMatch_Internal(text, ref matched) ?? false;

        private static bool SearchMatch_Internal(this IGotDisplayName gotDisplayName, string[] text, ref bool[] matched) =>
             gotDisplayName?.NameForDisplayPEGI().SearchMatch_Internal(text, ref matched) ?? false;

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

        private static bool SearchMatch_Internal(this IGotIndex gotIndex, int[] indexes) => gotIndex != null && indexes.Any(t => gotIndex.IndexForPEGI == t);

        private static SearchData defaultSearchData = new SearchData();

        private static readonly char[] splitCharacters = { ' ', '.' };

        public class SearchData : AbstractCfg, ICanBeDefaultCfg
        {
            public IEnumerable filteredList;
            public string searchedText;
            public int uncheckedElement = 0;
            public int inspectionIndexStart = 0;
            public bool filterByNeedAttention = false;
            private string[] searchBys;
            public List<int> filteredListElements = new List<int>();

            private const string searchFieldFocusName = "_pegiSearchField";

            private int focusOnSearchBarIn;

            public static bool unityFocusNameWillWork = false; // Focus name bug on first focus

            public void CloseSearch()
            {
                filteredList = null;
                pegi.UnFocusIfTrue(false);
            }

            public void ToggleSearch(IEnumerable ld, string label = "")
            {

                if (ld == null)
                    return;

                var active = ld == filteredList;

                var changed = false;

                if (active && icon.FoldedOut.ClickUnFocus("{0} {1} {2}".F(icon.Hide.GetText(), icon.Search.GetText(), ld), 27).changes(ref changed) || KeyCode.UpArrow.IsDown())
                    active = false;

                if (!active && ld != collectionInspector.reordering &&
                    (icon.Search
                        .Click("{0} {1}".F(icon.Search.GetText(), label.IsNullOrEmpty() ? ld.ToString() : label), 27) || KeyCode.DownArrow.IsDown())
                        .changes(ref changed))
                {
                    active = true;
                    focusOnSearchBarIn = 2;
                    FocusedName = searchFieldFocusName;
                }

                if (active)
                {
                    icon.Warning.write("Filter by warnings");
                    if (toggle(ref filterByNeedAttention))
                        Refresh();
                }

                if (!changed) return;

                filteredList = active ? ld : null;

            }

            public bool Searching(IList list) =>
                list == filteredList && (filterByNeedAttention || !searchBys.IsNullOrEmpty());
            
            public void SearchString(IEnumerable list, out bool searching, out string[] searchBy)
            {
                searching = false;

                if (list == filteredList)
                {

                    nl();

                    icon.Search.write();

                    NameNext(searchFieldFocusName);

                    if (edit(ref searchedText) || icon.Refresh.Click("Search again", 20).nl())
                    {
                        unityFocusNameWillWork = true;
                        Refresh();
                        searchBys = searchedText.Split(splitCharacters, StringSplitOptions.RemoveEmptyEntries);
                    }

                    if (focusOnSearchBarIn > 0)
                    {
                        focusOnSearchBarIn--;
                        if (focusOnSearchBarIn == 0)
                        {
                            FocusedName = searchFieldFocusName;
                            RepaintEditor();
                        }
                    }

                    searching = filterByNeedAttention || !searchBys.IsNullOrEmpty();
                }

                searchBy = searchBys;
            }

            public void Refresh()
            {
                filteredListElements.Clear();
                uncheckedElement = 0;
                inspectionIndexStart = 0;
            }

            public override CfgEncoder Encode() => new CfgEncoder().Add_String("s", searchedText);

            public override bool Decode(string tg, string data)
            {
                switch (tg)
                {
                    case "s":
                        searchedText = data;
                        break;
                    default: return false;
                }
                return true;
            }

            public override bool IsDefault => searchedText.IsNullOrEmpty();

        }

        #endregion
        
    }
}
