using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using System;
using System.Globalization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QuizCannersUtilities
{
    #region List Data

    public class ListMetaData : AbstractCfg, IPEGI {
        
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
        public readonly bool allowCreate;
        private readonly icon icon;
        public icon Icon => inspected == -1 ? icon : icon.Next;
        public UnNullableCfg<ElementData> elementDatas = new UnNullableCfg<ElementData>();
      

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
        public ElementData this[int i] => elementDatas.TryGet(i);

        #region Inspector
        #if PEGI

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

        public bool Inspect() {

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

            return false;
        }

        public bool Inspect<T>(List<T> list) where T : UnityEngine.Object {
            var changed = false;
#if UNITY_EDITOR

            "{0} Folder".F(label).edit(90, ref folderToSearch);
            if (icon.Search.Click("Populate {0} with objects from folder".F(label), ref changed)) {
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
        #endif
        #endregion

        #region Encode & Decode

        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "ed": data.DecodeInto(out elementDatas); break;
                case "insp": inspected = data.ToInt(); break;
                case "pi": previousInspected = data.ToInt(); break;
                case "fld": folderToSearch = data; break;
                case "ktd": keepTypeData = data.ToBool(); break;
                case "del": allowDelete = data.ToBool(); break;
                case "reord": allowReorder = data.ToBool(); break;
                case "st": listSectionStartIndex = data.ToInt(); break;
#if PEGI
                case "s": searchData.Decode(data); break;
#endif
                default: return false;
            }
            return true;
        }

        public override CfgEncoder Encode()
        {
            var cody = new CfgEncoder()
                .Add_IfNotNegative("insp", inspected)
                .Add_IfNotNegative("pi", previousInspected)
                .Add_IfNotZero("st", listSectionStartIndex)
                #if PEGI
                .Add_IfNotDefault("s", searchData)
                #endif
                ;
            
            if (!folderToSearch.SameAs(DefaultFolderToSearch))
                cody.Add_String("fld", folderToSearch);

            cody.Add_IfNotDefault("ed", elementDatas);
 
            return cody;
        }

#endregion

        public ListMetaData() {
            allowDelete = true;
            allowReorder = true;
            allowCreate = true;
            keepTypeData = false;
        }

        public ListMetaData(string nameMe, bool allowDeleting = true, bool allowReordering = true, bool keepTypeData = false, bool allowCreating = true, icon enterIcon = icon.Enter)
        {
            allowCreate = allowCreating;
            allowDelete = allowDeleting;
            allowReorder = allowReordering;
            label = nameMe;
            this.keepTypeData = keepTypeData;
            icon = enterIcon;
        }
    }

    public class ElementData : AbstractCfg, IPEGI, IGotName, IPEGI_Searchable {


        public string name;
        public string componentType;
        public string stdDta;
        private string _guid;
        public bool unrecognized;
        public string unrecognizedUnderTag;
        public bool selected;

        public override bool IsDefault => (unrecognized || !_guid.IsNullOrEmpty() || _perTypeConfig.Count>0 );

        public static bool enableEnterInspectEncoding;

        private Dictionary<string, string> _perTypeConfig = new Dictionary<string, string>();

        public ElementData SetRecognized() {
            if (!unrecognized) return this;
            
            unrecognized = false;
            unrecognizedUnderTag = null;
            stdDta = null;
            return this;
        }

        public void Unrecognized(string tag, string data) {
            unrecognized = true;
            unrecognizedUnderTag = tag;
            stdDta = data;
        }
        
        public void ChangeType(ref object obj, Type newType, TaggedTypesCfg taggedTypes, bool keepTypeConfig = false)
        {
            var previous = obj;

            var tObj = obj as IGotClassTag;

            if (keepTypeConfig && tObj != null)
                _perTypeConfig[tObj.ClassTag] = tObj.Encode().ToString();

            obj = Activator.CreateInstance(newType);

            var std = obj as ICfg;

            if (std != null)
            {
                string data;
                if (_perTypeConfig.TryGetValue(taggedTypes.Tag(newType), out data))
                    std.Decode(data);
            }

            StdExtensions.TryCopy_Std_AndOtherData(previous, obj);

        }

        public void Save<T>(T el)
        {
            name = el.ToPegiString();

            var cmp = el as Component;
            if (cmp != null)
                componentType = cmp.GetType().ToPegiStringType();

            var std = el as ICfg;
            if (std != null)
                stdDta = std.Encode().ToString();

            _guid = (el as UnityEngine.Object).GetGuid(_guid);
        }
        
        public bool TryGetByGuid<T>(ref T field) where T : UnityEngine.Object {

            var obj = UnityUtils.GuidToAsset<T>(_guid);

            field = null;

            if (!obj) return false;
            
            field = obj;

            if (componentType.IsNullOrEmpty()) return true;
            
            var go = obj as GameObject;

            if (!go) return true;
            
            var getScripts = go.GetComponent(componentType) as T;
            
            if (getScripts)
                field = getScripts;

            return true;

        }

        #region Inspector
        #if PEGI

        public bool String_SearchMatch(string searchString)
        {
            //TODO: Finish this
            return false;
        }

        public string NameForPEGI { get { return name; } set { name = value; } }

        public bool Inspect()
        {
        
            if (unrecognized)
                "Was unrecognized under tag {0}".F(unrecognizedUnderTag).writeWarning();

            if (_perTypeConfig.Count > 0)
                "Per type config".edit_Dictionary_Values(ref _perTypeConfig, pegi.lambda_string).nl();

            return false;
        }

        public bool SelectType<T>(ref object obj, TaggedTypesCfg all, bool keepTypeConfig = false)
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


        public bool SelectType<T>(ref object obj, bool keepTypeConfig = false) {
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


                /* if (keepTypeConfig && tobj != null)
                     perTypeConfig[tobj.ClassTag] = tobj.Encode().ToString();

                 string data;

                 if (!perTypeConfig.TryGetValue(all.Tag(type), out data) && tobj != null)
                         data = tobj.Encode().ToString();

                 obj = data.TryDecodeInto_Type<T>(type);*/


            }

            return changed;
        }

        public bool PEGI_inList<T>(ref object obj, int ind, ref int edited) {

            var changed = false;

            if (typeof(T).IsUnityObject()) {

                var uo = obj as UnityEngine.Object;
                if (PEGI_inList_Obj(ref uo).changes(ref changed))
                    obj = uo;

            } else {

                if (unrecognized)
                    unrecognizedUnderTag.write("Type Tag {0} was unrecognized during decoding".F(unrecognizedUnderTag), 40);

                if (!name.IsNullOrEmpty())
                    name.write();

                if (typeof(T) is IGotClassTag)
                    SelectType<T>(ref obj);

            }

            return changed;
        }

        public bool PEGI_inList_Obj<T>(ref T field) where T : UnityEngine.Object {

            if (unrecognized)
                unrecognizedUnderTag.write("Type Tag {0} was unrecognized during decoding".F(unrecognizedUnderTag), 40);

            var changed = name.edit(100, ref field);

#if UNITY_EDITOR
            if (_guid != null && icon.Search.Click("Find Object " + componentType + " by guid").nl()) {

                if (!TryGetByGuid(ref field))
                    (typeof(T).ToString() + " Not found ").showNotificationIn3D_Views();
                else changed = true;
            }
#endif

            return changed;
        }
#endif
#endregion

#region Encode & Decode
        public override bool Decode(string tg, string data) {
            switch (tg) {
                case "n": name = data; break;
                case "cfg": stdDta = data; break;
                case "guid": _guid = data; break;
                case "t": componentType = data; break;
                case "ur": unrecognized = data.ToBool(); break;
                case "tag": unrecognizedUnderTag = data; break;
                case "perType": data.Decode_Dictionary(out _perTypeConfig); break;
                case "sel": selected = data.ToBool(); break;
                default: return false;
            }
            return true;
        }

        public override CfgEncoder Encode() {
            var cody = new CfgEncoder()
                .Add_IfNotEmpty("n", name)
                .Add_IfNotEmpty("cfg", stdDta);

            if (!_guid.IsNullOrEmpty()) {
                cody.Add_IfNotEmpty("guid", _guid)
                    .Add_IfNotEmpty("t", componentType);
            }

            cody.Add_IfNotEmpty("perType", _perTypeConfig)
                .Add_IfTrue("sel", selected);

            if (unrecognized) {
                cody.Add_Bool("ur", unrecognized)
                .Add_String("tag", unrecognizedUnderTag);
            }
            return cody;
        }

#endregion


    }

    public static class StdListDataExtensions {

        public static ElementData TryGetElement(this ListMetaData ld, int ind) => ld?.elementDatas.TryGet(ind);

    }

#endregion

    #region Saved Cfg
    [Serializable]
    public class ExploringCfg : AbstractCfg, IPEGI, IGotName, IPEGI_ListInspect, IGotCount
    {

        public string tag;
        public string data;
        public bool dirty = false;

        public void UpdateData()
        {
            if (_tags != null)
                foreach (var t in _tags)
                    t.UpdateData();

            dirty = false;
            if (_tags != null)
                data = Encode().ToString();
        }

        public int inspectedTag = -1;
        [NonSerialized] private List<ExploringCfg> _tags;

        public ExploringCfg() { tag = ""; data = ""; }

        public ExploringCfg(string nTag, string nData) {
            tag = nTag;
            data = nData;
        }

#region Inspector
#if PEGI

        public int CountForInspector => _tags.IsNullOrEmpty() ? data.Length : _tags.CountForInspector();
        
        public string NameForPEGI  {
            get  {  return tag; }
            set {  tag = value; }
        }
        
        public bool Inspect()
        {
            if (_tags == null && data.Contains("|"))
                Decode(data);

            if (_tags != null)
                tag.edit_List(ref _tags, ref inspectedTag).changes(ref dirty);

            if (inspectedTag == -1)
            {
                "data".edit(40, ref data).nl(ref dirty);

                UnityEngine.Object myType = null;

                if (pegi.edit(ref myType))
                {
                    dirty = true;
                    data = FileLoadUtils.LoadTextAsset(myType);
                }

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
            }


            pegi.nl();

            return dirty;
        }

        public bool InspectInList(IList list, int ind, ref int edited)
        {

            bool changed = false;

            CountForInspector.ToString().write(50);

            if (data != null && data.Contains("|"))
            {
                changed |= pegi.edit(ref tag);

                if (icon.Enter.Click("Explore data"))
                    edited = ind;
            }
            else
            {
                dirty |= pegi.edit(ref tag);
                dirty |= pegi.edit(ref data);
            }

            if (icon.Copy.Click("Copy " + tag + " data to buffer."))
            {
                StdExtensions.copyBufferValue = data;
                StdExtensions.copyBufferTag = tag;
            }

            if (StdExtensions.copyBufferValue != null && icon.Paste.Click("Paste " + StdExtensions.copyBufferTag + " Data").nl())
            {
                dirty = true;
                data = StdExtensions.copyBufferValue;
            }

            return dirty | changed;
        }

#endif
#endregion

#region Encode & Decode
        public override CfgEncoder Encode()
        {
            var cody = new CfgEncoder();

            if (_tags == null) return cody;
            
            foreach (var t in _tags)
                cody.Add_String(t.tag, t.data);

            return cody;

        }

        public override bool Decode(string tg, string data)
        {
            if (_tags == null)
                _tags = new List<ExploringCfg>();
            _tags.Add(new ExploringCfg(tg, data));
            return true;
        }
#endregion

    }

    [Serializable]
    public class SavedIstd : IPEGI, IGotName, IPEGI_ListInspect, IGotCount {
        private static ICfg Cfg => StdExplorerData.inspectedCfg;

        public string comment;
        public ExploringCfg dataExplorer = new ExploringCfg("", "");

#region Inspector
        public string NameForPEGI { get { return dataExplorer.tag; } set { dataExplorer.tag = value; } }

#if PEGI
        public static StdExplorerData Mgmt => StdExplorerData.inspected;

        public int CountForInspector => dataExplorer.CountForInspector;

        public bool Inspect()
        {
            bool changed = false;


            if (dataExplorer.inspectedTag == -1)
            {
                this.inspect_Name();
                if (Cfg != null && dataExplorer.tag.Length > 0 && icon.Save.Click("Save To Assets", ref changed))
                {
                    FileSaveUtils.SaveBytesToAssetsByRelativePath(Mgmt.fileFolderHolder, dataExplorer.tag, dataExplorer.data);
                    UnityUtils.RefreshAssetDatabase();
                }

                pegi.nl();

                if (Cfg != null)
                {
                    if (dataExplorer.tag.Length == 0)
                        dataExplorer.tag = Cfg.ToPegiString() + " config";

                    "Save To:".edit(50, ref Mgmt.fileFolderHolder).changes(ref changed);

                    var uObj = Cfg as UnityEngine.Object;

                    if (uObj && icon.Done.Click("Use the same directory as current object.", ref changed))
                        Mgmt.fileFolderHolder = uObj.GetAssetFolder();

                    uObj.ClickHighlight().nl(ref changed);
                }


                "Comment:".editBig(ref comment).nl(ref changed);
            }

            dataExplorer.Nested_Inspect().changes(ref changed);

            return changed;
        }

        public bool InspectInList(IList list, int ind, ref int edited)
        {
            var changed = false;

            CountForInspector.ToString().edit(60, ref dataExplorer.tag).changes(ref changed);

            if (Cfg != null)
            {
                if (icon.Load.ClickUnFocus("Decode Data into " + Cfg.ToPegiString()).changes(ref changed)) {
                    dataExplorer.UpdateData();
                    Cfg.Decode(dataExplorer.data);
                }
                if (icon.Save.ClickUnFocus("Save data from " + Cfg.ToPegiString()).changes(ref changed))
                    dataExplorer = new ExploringCfg(dataExplorer.tag, Cfg.Encode().ToString());
            }

            if (icon.Enter.Click(comment))
                edited = ind;

            return changed;
        }
#endif
#endregion
    }

    [Serializable]
    public class StdExplorerData : IGotCount
    {
        public List<SavedIstd> states = new List<SavedIstd>();
        [NonSerialized] private int inspectedState = -1;
        public string fileFolderHolder = "STDEncodes";
        public static ICfg inspectedCfg;

#region Inspector
#if PEGI
        public int CountForInspector => states.Count;
        
        public static bool PEGI_Static(ICfg target)
        {
            inspectedCfg = target;

            var changed = false;
            
            "Load File:".write(90);
            target.LoadOnDrop().nl(ref changed);

            if (icon.Copy.Click("Copy Component Data").nl())
                StdExtensions.copyBufferValue = target.Encode().ToString();

            pegi.nl();

            return changed;
        }

        public static StdExplorerData inspected;

        public bool Inspect(ICfg target)
        {
            var changed = false;
            inspectedCfg = target;
            inspected = this;

            var added = "Saved CFGs:".edit_List(ref states, ref inspectedState, ref changed);

            if (added != null && target != null)
            {
                added.dataExplorer.data = target.Encode().ToString();
                added.NameForPEGI = target.ToPegiString();
                added.comment = DateTime.Now.ToString(CultureInfo.InvariantCulture);
            }

            if (inspectedState == -1)
            {
                UnityEngine.Object myType = null;
                if ("From File:".edit(65, ref myType))
                {
                    added = new SavedIstd();
                    added.dataExplorer.data = FileLoadUtils.LoadTextAsset(myType);
                    added.NameForPEGI = myType.name;
                    added.comment = DateTime.Now.ToString(CultureInfo.InvariantCulture);
                    states.Add(added);
                }

                var selfStd = target as IKeepMyCfg;

                if (selfStd != null)
                {
                    if (icon.Save.Click("Save itself (IKeepMySTD)"))
                        selfStd.SaveStdData();
                    var slfData = selfStd.ConfigStd;
                    if (!string.IsNullOrEmpty(slfData)) {

                        if (icon.Load.Click("Use IKeepMySTD data to create new CFG")) {
                            var ss = new SavedIstd();
                            states.Add(ss);
                            ss.dataExplorer.data = slfData;
                            ss.NameForPEGI = "from Keep my STD";
                            ss.comment = DateTime.Now.ToString(CultureInfo.InvariantCulture);
                        }

                      if (icon.Refresh.Click("Load from itself (IKeepMySTD)"))
                        target.Decode(slfData);
                    }
                }
                pegi.nl();
            }

            inspectedCfg = null;

            return changed;
        }
#endif
#endregion
    }
#endregion
}