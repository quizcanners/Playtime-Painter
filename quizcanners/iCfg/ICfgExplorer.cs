using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using PlayerAndEditorGUI;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QuizCannersUtilities
{
    #region List Data
    
    #pragma warning disable IDE0034 // Simplify 'default' expression
    #pragma warning disable IDE0019 // Use pattern matching
    #pragma warning disable IDE0018 // Inline variable declaration

    public class ListMetaData : ICfg, IPEGI {
        
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
      
        public List<int> GetSelectedElements() {
            var sel = new List<int>();
            foreach (var e in elementDatas)
                if (e.selected) sel.Add(elementDatas.currentEnumerationIndex);
            return sel;
        }

        public bool GetIsSelected(int ind) {
            var el = elementDatas.GetIfExists(ind);
            return el != null && el.selected;
        }

        public void SetIsSelected(int ind, bool value)
        {
            var el = value ? elementDatas[ind] : elementDatas.GetIfExists(ind);
            if (el != null)
                el.selected = value;
        }
        public ElementData this[int i] {
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

        public void Inspect() {

            pegi.nl();
            if (!_enterElementDatas) {
                "Show 'Explore Encoding' Button".toggleIcon(ref ElementData.enableEnterInspectEncoding).nl();
                "List Label".edit(70, ref label).nl();
                "Keep Type Data".toggleIcon("Will keep unrecognized data when you switch between class types.", ref keepTypeData).nl();
                "Allow Delete".toggleIcon(ref allowDelete).nl();
                "Allow Reorder".toggleIcon(ref allowReorder).nl();
            }

            if ("Elements".enter(ref _enterElementDatas).nl())
                elementDatas.Inspect();
        }

        public bool Inspect<T>(List<T> list) where T : Object {
            var changed = false;
#if UNITY_EDITOR

            "{0} Folder".F(label).edit(90, ref folderToSearch);
            if (icon.Search.Click("Populate {0} with objects from folder".F(label), ref changed)) {
                if (folderToSearch.Length > 0)
                {
                    var scrObjs = AssetDatabase.FindAssets("t:Object", new[] { folderToSearch });
                    foreach (var o in scrObjs)
                    {
                        var ass = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(o));
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

        #region Encode & Decode

        public void Decode(string key, CfgData data)
        {
            switch (key)
            {
                case "adl": allowDuplicants = data.ToBool(); break;
                case "insp": inspected = data.ToInt(0); break;
                case "pi": previousInspected = data.ToInt(0); break;
                case "fld": folderToSearch = data.ToString(); break;
                case "ktd": keepTypeData = data.ToBool(); break;
                case "del": allowDelete = data.ToBool(); break;
                case "reord": allowReorder = data.ToBool(); break;
                case "st": data.ToInt(ref listSectionStartIndex); break;
                case "s": searchData.DecodeFull(data); break;
            }
        }

        public CfgEncoder Encode()
        {
            var cody = new CfgEncoder()
                .Add_IfNotNegative("insp", inspected)
                .Add_IfNotNegative("pi", previousInspected)
                .Add_IfNotZero("st", listSectionStartIndex)
                .Add_IfTrue("adl", allowDuplicants)
                .Add_IfNotDefault("s", searchData)
                ;
            
            if (!folderToSearch.SameAs(DefaultFolderToSearch))
                cody.Add_String("fld", folderToSearch);
            
            return cody;
        }
        


        #endregion

        public ListMetaData() {
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
            icon enterIcon = icon.Enter) {

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

    public class ElementData : IPEGI, IGotName {


        public string name;
        public bool selected;

        public static bool enableEnterInspectEncoding;

        private Dictionary<string, CfgData> _perTypeConfig = new Dictionary<string, CfgData>();
        
        public void ChangeType(ref object obj, Type newType, TaggedTypesCfg taggedTypes, bool keepTypeConfig = false)
        {
            var previous = obj;

            var tObj = obj as IGotClassTag;

            if (keepTypeConfig && tObj != null)
             _perTypeConfig[tObj.ClassTag] = tObj.Encode().CfgData;

            obj = Activator.CreateInstance(newType);

            var std = obj as ICfg;

            if (std != null)
            {
                CfgData data;
                if (_perTypeConfig.TryGetValue(taggedTypes.Tag(newType), out data))
                    std.DecodeFull(data);
            }

            CfgExtensions.TryCopy_Std_AndOtherData(previous, obj);

        }

        public void Save<T>(T el)
        {
            name = el.GetNameForInspector();
        }
        
        #region Inspector

        public string NameForPEGI { get => name;
            set => name = value;
        }

        public void Inspect()
        {
            if (_perTypeConfig.Count > 0)
                "Per type config".edit_Dictionary(ref _perTypeConfig, pegi.lambda_cfg).nl();
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

        /*  public bool SelectType<T>(ref object obj, bool keepTypeConfig = false) {
              var changed = false;

              var all = typeof(T).TryGetTaggedClasses();

              if (all == null) {
                  "No Types Holder".writeWarning();
                  return false;
              }

             // var previous = obj;

              var type = obj?.GetType();

              if (all.Select(ref type).nl(ref changed)) {
                  ChangeType(ref obj, type, all, keepTypeConfig);
              }

              return changed;
          }*/

     /*   private bool PEGI_inList<T>(ref object obj, int ind, ref int edited, TaggedTypesCfg cfg)
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

                if (unrecognized)
                    unrecognizedUnderTag.write("Type Tag {0} was unrecognized during decoding".F(unrecognizedUnderTag), 40);

                if (!name.IsNullOrEmpty())
                    name.write();

                SelectType<T>(ref obj, cfg);

            }

            return changed;
        }
        */
        public bool PEGI_inList<T>(ref object obj) {

            var changed = false;

            if (typeof(T).IsUnityObject()) {

                var uo = obj as Object;
                if (PEGI_inList_Obj(ref uo).changes(ref changed))
                    obj = uo;

            } else {
                if (!name.IsNullOrEmpty())
                    name.write(150);

                if (obj is IGotClassTag)
                    SelectType(ref obj, TaggedTypesCfg.TryGetOrCreate(typeof(T)));
            }

            return changed;
        }

        public bool PEGI_inList_Obj<T>(ref T field) where T : Object {

            var changed = name.edit(100, ref field);

            return changed;
        }

        #endregion


    }

    public static class StdListDataExtensions {

        public static T TryGet<T>(this List<T> list, ListMetaData meta) => list.TryGet(meta.inspected);

        public static ElementData TryGetElement(this ListMetaData ld, int ind)
        {
            ElementData ed = new ElementData();
            ld?.elementDatas.TryGet(ind, out ed);
            return ed;
        }

    }

    #endregion

    #region Saved Cfg

    [Serializable]
    public class ICfgObjectExplorer : IGotCount
    {
        private List<CfgState> states = new List<CfgState>();
        private string fileFolderHolder = "STDEncodes";
        private static ICfg inspectedCfg;

        #region Inspector

        [NonSerialized] private int inspectedState = -1;

        public int CountForInspector() => states.Count;
        
        public static bool PEGI_Static(ICfgCustom target)
        {
            inspectedCfg = target;

            var changed = false;
            
            "Load File:".write(90);
            target.LoadCfgOnDrop().nl(ref changed);

            if (icon.Copy.Click("Copy Component Data").nl())
                CfgExtensions.copyBufferValue = target.Encode().ToString();

            pegi.nl();

            return changed;
        }

        public static ICfgObjectExplorer inspected;

        public bool Inspect(ICfg target)
        {
            var changed = false;
            inspectedCfg = target;
            inspected = this;

            var added = "Saved CFGs:".edit_List(ref states, ref inspectedState, ref changed);

            if (added != null && target != null)
            {
                added.dataExplorer.data = target.Encode().CfgData;
                added.NameForPEGI = target.GetNameForInspector();
                added.comment = DateTime.Now.ToString(CultureInfo.InvariantCulture);
            }

            if (inspectedState == -1)
            {
                Object myType = null;
                
                if ("From File:".edit(65, ref myType))
                {
                    added = new CfgState();

                    string path = QcFile.Explorer.TryGetFullPathToAsset(myType);

                    Debug.Log(path);

                    added.dataExplorer.data = new CfgData(QcFile.Load.TryLoadAsTextAsset(myType));

                    added.NameForPEGI = myType.name;
                    added.comment = DateTime.Now.ToString(CultureInfo.InvariantCulture);
                    states.Add(added);
                }
                /*
                var selfStd = target as IKeepMyCfg;

                if (selfStd != null)
                {
                    if (icon.Save.Click("Save itself (IKeepMySTD)"))
                        selfStd.SaveCfgData();
                    var slfData = selfStd.ConfigStd;
                    if (!string.IsNullOrEmpty(slfData)) {

                        if (icon.Load.Click("Use IKeepMySTD data to create new CFG")) {
                            var ss = new CfgState();
                            states.Add(ss);
                            ss.dataExplorer.data = slfData;
                            ss.NameForPEGI = "from Keep my STD";
                            ss.comment = DateTime.Now.ToString(CultureInfo.InvariantCulture);
                        }

                      if (icon.Refresh.Click("Load from itself (IKeepMySTD)"))
                        target.Decode(slfData);
                    }
                }
                */
                pegi.nl();
            }

            inspectedCfg = null;

            return changed;
        }

        #endregion

        [Serializable]
        private class ICfgProperty : ICfgCustom, IPEGI, IGotName, IPEGI_ListInspect, IGotCount
        {

            public string tag;
            public CfgData data;
            public bool dirty;

            public void UpdateData()
            {
                if (_tags != null)
                    foreach (var t in _tags)
                        t.UpdateData();

                dirty = false;
                if (_tags != null)
                    data = Encode().CfgData;
            }

            public int inspectedTag = -1;
            [NonSerialized] private List<ICfgProperty> _tags;

            public ICfgProperty() { tag = ""; data = new CfgData(); }

            public ICfgProperty(string nTag, CfgData nData)
            {
                tag = nTag;
                data = nData;
            }

            #region Inspector

            public int CountForInspector() => _tags.IsNullOrEmpty() ? data.ToString().Length : _tags.CountForInspector();

            public string NameForPEGI
            {
                get { return tag; }
                set { tag = value; }
            }

            public void Inspect()
            {
                if (_tags == null && data.ToString().Contains("|"))
                    Decode(data);

                if (_tags != null)
                    tag.edit_List(ref _tags, ref inspectedTag).changes(ref dirty);

                if (inspectedTag == -1)
                {
                    var changes = pegi.ChangeTrackStart();
                    //"data".edit(40, ref data).changes(ref dirty);
                    data.Inspect();

                    dirty |= changes;
                   /* UnityEngine.Object myType = null;

                    if (pegi.edit(ref myType))
                    {
                        dirty = true;
                        data = QcFile.LoadUtils.TryLoadAsTextAsset(myType);
                    }*/

                    if (dirty)
                    {
                        if (icon.Refresh.Click("Update data string from tags"))
                            UpdateData();

                        if (icon.Load.Click("Load from data String").nl())
                        {
                            _tags = null;
                            Decode(data);//.DecodeTagsFor(this);
                            dirty = false;
                        }
                    }

                    pegi.nl();
                }


                pegi.nl();
            }

            public void InspectInList(IList list, int ind, ref int edited)
            {

                bool changed = false;

                CountForInspector().ToString().write(50);

                if (data.IsEmpty == false && data.ToString().Contains("|"))
                {
                    pegi.edit(ref tag).changes(ref changed);

                    if (icon.Enter.Click("Explore data"))
                        edited = ind;
                }
                else
                {
                    pegi.edit(ref tag).changes(ref dirty);
                    data.Inspect(); //.changes(ref dirty);
                    //pegi.edit(ref data).changes(ref dirty);
                }

                if (icon.Copy.Click("Copy " + tag + " data to buffer."))
                {
                    CfgExtensions.copyBufferValue = data.ToString();
                    CfgExtensions.copyBufferTag = tag;
                }

                if (CfgExtensions.copyBufferValue != null && icon.Paste.Click("Paste " + CfgExtensions.copyBufferTag + " Data").nl())
                {
                    dirty = true;
                    data = new CfgData(CfgExtensions.copyBufferValue);
                }

            }

            #endregion

            #region Encode & Decode

            public void Decode(CfgData data)=>
                new CfgDecoder(data).DecodeTagsIgnoreErrors(this);
            

            public CfgEncoder Encode()
            {
                var cody = new CfgEncoder();

                if (_tags == null) return cody;

                foreach (var t in _tags)
                    cody.Add_String(t.tag, t.data.ToString());

                return cody;

            }

            public void Decode(string key, CfgData dta)
            {
                if (_tags == null)
                    _tags = new List<ICfgProperty>();

                _tags.Add(new ICfgProperty(key, dta));
            }
            #endregion

        }

        [Serializable]
        private class CfgState : IPEGI, IGotName, IPEGI_ListInspect, IGotCount
        {
            private static ICfg Cfg => inspectedCfg;

            public string comment;
            public ICfgProperty dataExplorer = new ICfgProperty("", new CfgData());

            #region Inspector
            public string NameForPEGI { get { return dataExplorer.tag; } set { dataExplorer.tag = value; } }

            public static ICfgObjectExplorer Mgmt => inspected;

            public int CountForInspector() => dataExplorer.CountForInspector();

            public void Inspect()
            {
                bool changed = false;


                if (dataExplorer.inspectedTag == -1)
                {
                    this.inspect_Name();
                    if (dataExplorer.tag.Length > 0 && icon.Save.Click("Save To Assets", ref changed))
                    {
                        QcFile.Save.ToAssets(Mgmt.fileFolderHolder, filename: dataExplorer.tag, data: dataExplorer.data.ToString(), asBytes: true);
                        QcUnity.RefreshAssetDatabase();
                    }

                    pegi.nl();

                    if (Cfg != null)
                    {
                        if (dataExplorer.tag.Length == 0)
                            dataExplorer.tag = Cfg.GetNameForInspector() + " config";

                        "Save To:".edit(50, ref Mgmt.fileFolderHolder).changes(ref changed);

                        var uObj = Cfg as Object;

                        if (uObj && icon.Done.Click("Use the same directory as current object.", ref changed))
                            Mgmt.fileFolderHolder = QcUnity.GetAssetFolder(uObj);

                        uObj.ClickHighlight().nl(ref changed);
                    }

                    if ("Description".foldout().nl())
                    {
                        pegi.editBig(ref comment).nl(ref changed);
                    }
                }

                dataExplorer.Nested_Inspect().changes(ref changed);
            }

            public void InspectInList(IList list, int ind, ref int edited)
            {
                var changed = false;

                if (dataExplorer.data.ToString().IsNullOrEmpty() == false && icon.Copy.Click())
                    pegi.SetCopyPasteBuffer(dataExplorer.data.ToString());
                
                CountForInspector().ToString().edit(60, ref dataExplorer.tag).changes(ref changed);

                if (Cfg != null)
                {
                    if (icon.Load.ClickConfirm("sfgLoad", "Decode Data into " + Cfg.GetNameForInspector()).changes(ref changed))
                    {
                        dataExplorer.UpdateData();
                        Cfg.DecodeFull(dataExplorer.data);
                    }
                    if (icon.Save.ClickConfirm("cfgSave", "Save data from " + Cfg.GetNameForInspector()).changes(ref changed))
                        dataExplorer = new ICfgProperty(dataExplorer.tag, Cfg.Encode().CfgData);
                }

                if (icon.Enter.Click(comment))
                    edited = ind;
            }

            #endregion
        }


    }
    #endregion
}