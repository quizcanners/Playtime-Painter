using QuizCanners.Migration;
using QuizCanners.Utils;
using System.Collections.Generic;

namespace QuizCanners.Inspect
{
    #region Collection Inspect Data

    [System.Flags]
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
        internal string label = "list";
        internal int inspectedElement = -1;
        internal int previouslyInspectedElement = -1;
        internal int listSectionStartIndex;
        internal bool useOptimalShowRange = true;
        internal int itemsToShow = 10;
        internal UnNullable<ElementData> elementDatas = new UnNullable<ElementData>();
        internal bool inspectListMeta = false;

        private CollectionInspectParams _config;

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
        public ElementData this[int i]
        {
            get
            {
                elementDatas.TryGet(i, out ElementData dta);
                return dta;
            }
        }

        public bool IsInspectingElement 
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
      

        #region Inspector

        internal readonly pegi.SearchData searchData = new pegi.SearchData();

        private bool _enterElementDatas;
     
        public void Inspect()
        {

            pegi.nl();
            if (!_enterElementDatas)
            {
               
                "List Label".edit(70, ref label).nl();
                "Config".editEnumFlags(ref _config).nl();
            }

            if ("Elements".isEntered(ref _enterElementDatas).nl())
                elementDatas.Inspect();
        }
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
        
        public CollectionMetaData(string labelName, bool allowDeleting = true,
            bool allowReordering = true,
            bool showAddButton = true,
            bool showEditListButton = true,
            bool showSearchButton = true,
            bool showDictionaryKey = true,
            bool showCopyPasteOptions = false)
        {

            label = labelName;
            
            this[CollectionInspectParams.showAddButton] = showAddButton;
            this[CollectionInspectParams.allowDeleting] = allowDeleting;
            this[CollectionInspectParams.allowReordering] = allowReordering;
            this[CollectionInspectParams.showEditListButton] = showEditListButton;
            this[CollectionInspectParams.showSearchButton] = showSearchButton;
            this[CollectionInspectParams.showDictionaryKey] = showDictionaryKey;
            this[CollectionInspectParams.showCopyPasteOptions] = showCopyPasteOptions;
        }
    }

    public class ElementData 
    {
        public bool selected;
       
        public void ChangeType(ref object obj, System.Type newType)
        {
            var previous = obj;

            CfgData prev = new CfgData();

            if (obj is ICfg tObj)
                prev = tObj.Encode().CfgData;

            obj = System.Activator.CreateInstance(newType);

            if (obj is ICfg std)
            {
                std.DecodeFull(prev);
            }

            ICfgExtensions.TryCopy_Std_AndOtherData(previous, obj);
        }

        #region Inspector
   
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

        internal bool PEGI_inList<T>(ref object obj)
        {
            var changed = pegi.ChangeTrackStart();

            if (typeof(T).IsUnityObject())
            {
                var uo = obj as UnityEngine.Object;
                if (pegi.edit(ref uo))
                    obj = uo;
            }

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
