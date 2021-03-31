using QuizCanners.CfgDecode;
using QuizCanners.Utils;
using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QuizCanners.Inspect
{
    #region List Data

    public class ListMetaData : IPEGI
    {

        private const string DefaultFolderToSearch = "Assets/";

        public string label = "list";
        private string folderToSearch = DefaultFolderToSearch;
        public int inspected = -1;
        public int previousInspected = -1;
        public int listSectionStartIndex;
        public bool Inspecting { get { return inspected != -1; } set { if (value == false) inspected = -1; } }
        public bool keepTypeData;
        public bool allowDelete;
        public bool allowReorder;
        public bool allowDuplicants;
        public bool showEditListButton;
        public bool showSearchButton;
        public bool showDictionaryKey;
        public bool useOptimalShowRange = true;
        public int itemsToShow = 10;
        public readonly bool showAddButton;
        public readonly icon icon;
        public UnNullable<ElementData> elementDatas = new UnNullable<ElementData>();

        public List<int> GetSelectedElements()
        {
            var sel = new List<int>();
            foreach (var e in elementDatas)
                if (e.selected) sel.Add(elementDatas.currentEnumerationIndex);
            return sel;
        }

        public bool GetIsSelected(int ind)
        {
            var el = elementDatas.GetIfExists(ind);
            return el != null && el.selected;
        }

        public void SetIsSelected(int ind, bool value)
        {
            var el = value ? elementDatas[ind] : elementDatas.GetIfExists(ind);
            if (el != null)
                el.selected = value;
        }
        public ElementData this[int i]
        {
            get
            {
                ElementData dta;
                elementDatas.TryGet(i, out dta);
                return dta;
            }
        }

        #region Inspector

        public readonly pegi.SearchData searchData = new pegi.SearchData();

        public void SaveElementDataFrom<T>(List<T> list)
        {
            for (var i = 0; i < list.Count; i++)
                SaveElementDataFrom(list, i);
        }

        private void SaveElementDataFrom<T>(List<T> list, int i)
        {
            var el = list[i];
            if (el != null)
                elementDatas[i].Save(el);
        }

        private bool _enterElementDatas;
        public bool inspectListMeta = false;

        public void Inspect()
        {

            pegi.nl();
            if (!_enterElementDatas)
            {
                "Show 'Explore Encoding' Button".toggleIcon(ref ElementData.enableEnterInspectEncoding).nl();
                "List Label".edit(70, ref label).nl();
                "Keep Type Data".toggleIcon("Will keep unrecognized data when you switch between class types.", ref keepTypeData).nl();
                "Allow Delete".toggleIcon(ref allowDelete).nl();
                "Allow Reorder".toggleIcon(ref allowReorder).nl();
            }

            if ("Elements".enter(ref _enterElementDatas).nl())
                elementDatas.Inspect();
        }

        public bool Inspect<T>(List<T> list) where T : UnityEngine.Object
        {
            var changed = false;
#if UNITY_EDITOR

            "{0} Folder".F(label).edit(90, ref folderToSearch);
            if (icon.Search.Click("Populate {0} with objects from folder".F(label), ref changed))
            {
                if (folderToSearch.Length > 0)
                {
                    var scrObjs = AssetDatabase.FindAssets("t:Object", new[] { folderToSearch });
                    foreach (var o in scrObjs)
                    {
                        var ass = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GUIDToAssetPath(o));
                        if (!ass) continue;
                        list.TryAddUObjIfNew(ass);
                    }
                }
            }

            if (list.Count > 0 && list[0] != null && icon.Refresh.Click("Use location of the first element in the list", ref changed))
                folderToSearch = AssetDatabase.GetAssetPath(list[0]);

#endif
            return changed;
        }

        #endregion

    
        public ListMetaData()
        {
            allowDelete = true;
            allowReorder = true;
            showAddButton = true;
            keepTypeData = false;
        }

        public ListMetaData(string nameMe, bool allowDeleting = true,
            bool allowReordering = true,
            bool keepTypeData = false,
            bool showAddButton = true,
            bool showEditListButton = true,
            bool showSearchButton = true,
            bool showDictionaryKey = true,
            icon enterIcon = icon.Enter)
        {

            this.showAddButton = showAddButton;
            allowDelete = allowDeleting;
            allowReorder = allowReordering;
            label = nameMe;
            this.keepTypeData = keepTypeData;
            this.showEditListButton = showEditListButton;
            this.showSearchButton = showSearchButton;
            this.showDictionaryKey = showDictionaryKey;
            icon = enterIcon;
        }
    }

    public class ElementData : IGotName
    {


        public string name;
        public bool selected;

        public static bool enableEnterInspectEncoding;


        public void ChangeType(ref object obj, Type newType, TaggedTypesCfg taggedTypes, bool keepTypeConfig = false)
        {
            var previous = obj;

            var tObj = obj as IGotClassTag;

            CfgData prev = new CfgData();

            if (tObj != null)
                prev = tObj.Encode().CfgData;

            obj = Activator.CreateInstance(newType);

            var std = obj as ICfg;

            if (std != null)
            {
                 std.DecodeFull(prev);
            }

            CfgExtensions.TryCopy_Std_AndOtherData(previous, obj);

        }

        public void Save<T>(T el)
        {
            name = el.GetNameForInspector();
        }

        #region Inspector

        public string NameForPEGI
        {
            get => name;
            set => name = value;
        }

     

        public bool SelectType(ref object obj, TaggedTypesCfg all, bool keepTypeConfig = false)
        {
            var changed = false;

            if (all == null)
            {
                "No Types Holder".writeWarning();
                return false;
            }

            var type = obj?.GetType();

            if (all.Select(ref type).nl(ref changed))
                ChangeType(ref obj, type, all, keepTypeConfig);

            return changed;
        }

        public bool PEGI_inList<T>(ref object obj)
        {

            var changed = false;

            if (typeof(T).IsUnityObject())
            {

                var uo = obj as UnityEngine.Object;
                if (PEGI_inList_Obj(ref uo).changes(ref changed))
                    obj = uo;

            }
            else
            {
                if (!name.IsNullOrEmpty())
                    name.write(150);

                //if (obj is IGotClassTag)
                   // SelectType(ref obj, TaggedTypesCfg.TryGetOrCreate(typeof(T)));
            }

            return changed;
        }

        public bool PEGI_inList_Obj<T>(ref T field) where T : UnityEngine.Object
        {

            var changed = name.edit(100, ref field);

            return changed;
        }

        #endregion


    }

    public static class StdListDataExtensions
    {

        public static T TryGet<T>(this List<T> list, ListMetaData meta) => list.TryGet(meta.inspected);

        public static ElementData TryGetElement(this ListMetaData ld, int ind)
        {
            ElementData ed = new ElementData();
            ld?.elementDatas.TryGet(ind, out ed);
            return ed;
        }

    }

    #endregion



}
