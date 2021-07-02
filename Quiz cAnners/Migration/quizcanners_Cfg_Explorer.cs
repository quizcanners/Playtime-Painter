using System;
using System.Collections.Generic;
using QuizCanners.Inspect;
using UnityEngine;
using Object = UnityEngine.Object;
using QuizCanners.Utils;

namespace QuizCanners.Migration
{

#pragma warning disable IDE0018 // Inline variable declaration

   

    #region Saved Cfg

    [Serializable]
    public class ICfgObjectExplorer : IGotCount
    {
        private readonly List<CfgState> states = new List<CfgState>();
        private string fileFolderHolder = "STDEncodes";
        private static ICfg inspectedCfg;

        #region Inspector

        [NonSerialized] private int inspectedState = -1;

        public int GetCount() => states.Count;
        
        public static bool PEGI_Static(ICfgCustom target)
        {
            inspectedCfg = target;

            var changed = false;
            
            "Load File:".write(90);
            target.LoadCfgOnDrop().nl();

            if (icon.Copy.Click("Copy Component Data").nl())
                ICfgExtensions.copyBufferValue = target.Encode().ToString();

            pegi.nl();

            return changed;
        }

        public static ICfgObjectExplorer inspected;

        public bool Inspect(ICfg target)
        {
            var changed = pegi.ChangeTrackStart();
            inspectedCfg = target;
            inspected = this;

            CfgState added; 

            "Saved CFGs:".edit_List(states, ref inspectedState, out added);

            if (added != null && target != null)
            {
                added.dataExplorer.data = target.Encode().CfgData;
                added.NameForInspector = target.GetNameForInspector();
                added.comment = DateTime.Now.ToString(System.Globalization.CultureInfo.InvariantCulture);
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

                    added.NameForInspector = myType.name;
                    added.comment = DateTime.Now.ToString(System.Globalization.CultureInfo.InvariantCulture);
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

            public int GetCount() => _tags.IsNullOrEmpty() ? data.ToString().Length : _tags.CountForInspector();

            public string NameForInspector
            {
                get { return tag; }
                set { tag = value; }
            }

            public void Inspect()
            {
                if (_tags == null && data.ToString().Contains("|"))
                    Decode(data);

                var changes = pegi.ChangeTrackStart();

                if (_tags != null)
                    tag.edit_List(_tags, ref inspectedTag);

                dirty |= changes;

                if (inspectedTag == -1)
                {
                   
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

            public void InspectInList(ref int edited, int ind)
            {

                GetCount().ToString().write(50);

                if (data.IsEmpty == false && data.ToString().Contains("|"))
                {
                    pegi.edit(ref tag);

                    if (icon.Enter.Click("Explore data"))
                        edited = ind;
                }
                else
                {
                    if (pegi.edit(ref tag))
                        dirty = true;

                    data.Inspect(); //.changes(ref dirty);
                    //pegi.edit(ref data).changes(ref dirty);
                }

                if (icon.Copy.Click("Copy " + tag + " data to buffer."))
                {
                    ICfgExtensions.copyBufferValue = data.ToString();
                    ICfgExtensions.copyBufferTag = tag;
                }

                if (ICfgExtensions.copyBufferValue != null && icon.Paste.Click("Paste " + ICfgExtensions.copyBufferTag + " Data").nl())
                {
                    dirty = true;
                    data = new CfgData(ICfgExtensions.copyBufferValue);
                }

            }

            #endregion

            #region Encode & Decode

            public void Decode(CfgData dataToDecode)=>
                new CfgDecoder(dataToDecode).DecodeTagsIgnoreErrors(this);
            

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
            public string NameForInspector { get { return dataExplorer.tag; } set { dataExplorer.tag = value; } }

            public static ICfgObjectExplorer Mgmt => inspected;

            public int GetCount() => dataExplorer.GetCount();

            public void Inspect()
            {

                if (dataExplorer.inspectedTag == -1)
                {
                    this.inspect_Name();
                    if (dataExplorer.tag.Length > 0 && icon.Save.Click("Save To Assets"))
                    {
                        QcFile.Save.ToAssets(Mgmt.fileFolderHolder, filename: dataExplorer.tag, data: dataExplorer.data.ToString(), asBytes: true);
                        QcUnity.RefreshAssetDatabase();
                    }

                    pegi.nl();

                    if (Cfg != null)
                    {
                        if (dataExplorer.tag.Length == 0)
                            dataExplorer.tag = Cfg.GetNameForInspector() + " config";

                        "Save To:".edit(50, ref Mgmt.fileFolderHolder);

                        var uObj = Cfg as Object;

                        if (uObj && icon.Done.Click("Use the same directory as current object."))
                            Mgmt.fileFolderHolder = QcUnity.GetAssetFolder(uObj);

                        uObj.ClickHighlight().nl();
                    }

                    if ("Description".isFoldout().nl())
                    {
                        pegi.editBig(ref comment).nl();
                    }
                }

                dataExplorer.Nested_Inspect();
            }

            public void InspectInList(ref int edited, int ind)
            {

                if (dataExplorer.data.ToString().IsNullOrEmpty() == false && icon.Copy.Click())
                    pegi.SetCopyPasteBuffer(dataExplorer.data.ToString());
                
                GetCount().ToString().edit(60, ref dataExplorer.tag);

                if (Cfg != null)
                {
                    if (icon.Load.ClickConfirm("sfgLoad", "Decode Data into " + Cfg.GetNameForInspector()))
                    {
                        dataExplorer.UpdateData();
                        Cfg.DecodeFull(dataExplorer.data);
                    }
                    if (icon.Save.ClickConfirm("cfgSave", "Save data from " + Cfg.GetNameForInspector()))
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