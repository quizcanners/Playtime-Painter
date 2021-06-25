using QuizCanners.CfgDecode;
using QuizCanners.Utils;
using System;
using System.Collections.Generic;

namespace QuizCanners.Inspect
{
    #region Collection Inspect Data


    [Flags]
    public enum CollectionInspectParams
    {
        None = 0,
        allowDeleting = 1,
        allowReordering = 2,
        showAddButton = 4,
        showEditListButton = 8,
        showSearchButton = 16,
        showDictionaryKey = 32,
        allowDuplicates = 64,
        showCopyPasteOptions = 128,
    }
    
    public class CollectionMetaData : IPEGI
    {
        public string label = "list";
        public int inspectedElement = -1;
        public int previouslyInspectedElement = -1;
        public int listSectionStartIndex;
        private CollectionInspectParams _config;
        public bool useOptimalShowRange = true;
        public int itemsToShow = 10;
        public UnNullable<ElementData> elementDatas = new UnNullable<ElementData>();
        
        public bool this[CollectionInspectParams param]
        {
            get => (_config & param) == param;
            set
            {
                if (value)
                {
                    _config |= param;
                }
                else
                {
                    _config &= ~param;
                }
            }
        }
        
        public bool InspectingElement 
        { 
            get => inspectedElement != -1;
            set { if (value == false) inspectedElement = -1; } 
        }
        
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
                elementDatas.TryGet(i, out ElementData dta);
                return dta;
            }
        }

        #region Inspector

        internal readonly pegi.SearchData searchData = new pegi.SearchData();

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
                "Config".editEnumFlags(ref _config).nl();
            }

            if ("Elements".isEntered(ref _enterElementDatas).nl())
                elementDatas.Inspect();
        }

       /* public bool Inspect<T>(List<T> list) where T : UnityEngine.Object
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
        }*/

        #endregion

    
        public CollectionMetaData()
        {
            this[CollectionInspectParams.showAddButton] = true;
            this[CollectionInspectParams.allowDeleting] = true;
            this[CollectionInspectParams.allowReordering] = true;
        }

        public CollectionMetaData(string nameMe,params CollectionInspectParams[] configs)
        {
            label = nameMe;
            foreach (var config in configs)
                this[config] = true;
        }
        
        public CollectionMetaData(string nameMe, bool allowDeleting = true,
            bool allowReordering = true,
            bool showAddButton = true,
            bool showEditListButton = true,
            bool showSearchButton = true,
            bool showDictionaryKey = true,
            bool showCopyPasteOptions = false)
        {

            label = nameMe;
            
            this[CollectionInspectParams.showAddButton] = showAddButton;
            this[CollectionInspectParams.allowDeleting] = allowDeleting;
            this[CollectionInspectParams.allowReordering] = allowReordering;
            this[CollectionInspectParams.showEditListButton] = showEditListButton;
            this[CollectionInspectParams.showSearchButton] = showSearchButton;
            this[CollectionInspectParams.showDictionaryKey] = showDictionaryKey;
            this[CollectionInspectParams.showCopyPasteOptions] = showCopyPasteOptions;
        }
    }

    public class ElementData : IGotName
    {
        public string name;
        public bool selected;

        public static bool enableEnterInspectEncoding;

        public void ChangeType(ref object obj, Type newType)
        {
            var previous = obj;

            CfgData prev = new CfgData();

            if (obj is ICfg tObj)
                prev = tObj.Encode().CfgData;

            obj = Activator.CreateInstance(newType);

            if (obj is ICfg std)
            {
                std.DecodeFull(prev);
            }

            ICfgExtensions.TryCopy_Std_AndOtherData(previous, obj);
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

     

        public bool SelectType(ref object obj, TaggedTypesCfg all)
        {
            var changed = pegi.ChangeTrackStart();

            if (all == null)
            {
                "No Types Holder".writeWarning();
                return false;
            }

            var type = obj?.GetType();

            if (all.Inspect_Select(ref type).nl())
                ChangeType(ref obj, type);

            return changed;
        }

        public bool PEGI_inList<T>(ref object obj)
        {

            var changed = pegi.ChangeTrackStart();

            if (typeof(T).IsUnityObject())
            {
                var uo = obj as UnityEngine.Object;
                if (PEGI_inList_Obj(ref uo))
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
        public static T TryGet<T>(this List<T> list, CollectionMetaData meta) => list.TryGet(meta.inspectedElement);

        public static ElementData TryGetElement(this CollectionMetaData ld, int ind)
        {
            ElementData ed = new ElementData();
            ld?.elementDatas.TryGet(ind, out ed);
            return ed;
        }
    }
    #endregion
}
