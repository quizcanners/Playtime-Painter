using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using QuizCanners.Inspect;
using UnityEngine;
using Object = UnityEngine.Object;
using QuizCanners.Utils;
#if UNITY_EDITOR

#endif

namespace QuizCanners.CfgDecode
{
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration

   

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