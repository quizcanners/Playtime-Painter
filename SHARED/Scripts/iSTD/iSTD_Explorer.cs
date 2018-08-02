using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using System;
//using System.Windows;

namespace SharedTools_Stuff
{


    public class ISTD_Explorer : MonoBehaviour, IPEGI
    {
        public ISTD ConnectSTD;
        public ISTD_ExplorerData data = new ISTD_ExplorerData();

#if PEGI
        /* public static bool PEGI_Static(iSTD target)
         {
             return iSTD_ExplorerData.PEGI_Static(target);
         }*/

        public bool PEGI()
        {

            UnityEngine.Object obj = ConnectSTD == null ? null : ConnectSTD as UnityEngine.Object;
            if ("Target Obj: ".edit(60, ref obj) && obj != null)
                    ConnectSTD = obj as ISTD;
            

            MonoBehaviour mono = ConnectSTD == null ? null : ConnectSTD as MonoBehaviour;
            if ("Target Obj: ".edit(60, ref mono).nl() && mono != null)
                    ConnectSTD = mono as ISTD;
            

            return data.PEGI(ConnectSTD);

        }
#endif

    }

    public class ElementData : Abstract_STD
    {
        public string name;
        public string componentType;
        public string std_dta;
        public string guid;

#if PEGI
        public void Save<T>(T el)
        {
            name = el.ToPEGIstring();

            var cmp = el as Component;
            if ( cmp != null)
                componentType = cmp.GetType().ToPEGIstring();
            
            var std = el as ISTD;
            if (std != null)
                std_dta = std.Encode().ToString();

            guid = (el as UnityEngine.Object).GetGUID(guid);
        }

        public bool Inspect<T>(ref T field) where T : UnityEngine.Object
        {
            
            bool changed = name.edit(100, ref field);

#if UNITY_EDITOR
            if (guid != null && icon.Search.Click("Find Object " +componentType +" by guid").nl())
                {
                    var obj = UnityHelperFunctions.GUIDtoAsset<T>(guid);

                    if (obj)
                    {
                        field = obj;

                        if (componentType != null && componentType.Length > 0)
                        {
                            var go = obj as GameObject;
                            if (go)
                            {
                                var getScripts = go.GetComponent(componentType) as T;
                                if (getScripts)
                                    field = getScripts;
                            }
                        }

                        changed = true;
                    }
                    else
                        (typeof(T).ToString() + " Not found ").showNotification();
                }
#endif
 

            return changed;
        }
#endif

        public override bool Decode(string tag, string data)
        {
            switch (tag)
            {
                case "n": name = data; break;
                case "std": std_dta = data; break;
                case "guid": guid = data; break;
                case "t": componentType = data; break;
                default: return false;
            }
            return true;
        }

        public override StdEncoder Encode() => new StdEncoder()
            .Add_String("n", name)
            .Add_String("std", std_dta)
            .Add_String("guid", guid)
            .Add_String ("t", componentType);

    }

    [Serializable]
    public class Exploring_STD : Abstract_STD , IPEGI, IGotName, IPEGI_ListInspect
    {
        ISTD Std { get { return ISTD_ExplorerData.inspectedSTD; } }

        public string tag;
        public string data;
        public bool dirty = false;

        public void UpdateData()
        {
            if (tags != null)
            foreach (var t in tags)
                t.UpdateData();

            dirty = false;
            if (tags!= null)
            data = this.Encode().ToString();
        }

        public int inspectedTag = -1;
        [NonSerialized]
        public List<Exploring_STD> tags;

        public Exploring_STD() { tag = ""; data = "";  }

        public Exploring_STD(string ntag, string ndata)
        {
            tag = ntag;
            data = ndata;
        }

#if PEGI
 
        public string NameForPEGI
        {
            get
            {
                return tag;
            }

            set
            {
                tag = value;
            }
        }

        public bool PEGI()
        {
            if (tags == null && data.Contains("|"))
                Decode(data);//.DecodeTagsFor(this);

            if (tags != null)
                dirty |= tag.edit_List(tags, ref inspectedTag, true);

            if (inspectedTag == -1)
            {
                dirty |= "data".edit(40, ref data);

                UnityEngine.Object myType = null;

                if (pegi.edit(ref myType))
                {
                    dirty = true;
                    data = ResourceLoader.LoadStory(myType);
                }

                if (dirty)
                {
                    if (icon.Refresh.Click("Update data string from tags"))
                        UpdateData();

                    if (icon.Load.Click("Load from data String").nl())
                    {
                        tags = null;
                        Decode(data);//.DecodeTagsFor(this);
                        dirty = false;
                    }
                }
            }


            pegi.nl();

            return dirty;
        }
        
        public bool PEGI_inList(IList list, int ind, ref int edited)
        {

            bool changed = false;

            if (data != null && data.Contains("|"))
            {
                changed |= pegi.edit(ref tag);//  tag.write(60);

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
                STDExtensions.copyBufferValue = data;
                STDExtensions.copyBufferTag = tag;
            }

            if (STDExtensions.copyBufferValue != null && icon.Paste.Click("Paste "+ STDExtensions.copyBufferTag + " Data").nl()) {
                dirty = true;
                data = STDExtensions.copyBufferValue;
            }
            
            return dirty | changed;
        }

#endif

        public override StdEncoder Encode()
        {
            var cody = new StdEncoder();

            if (tags != null)
                foreach (var t in tags)
                    cody.Add_String(t.tag, t.data);
            

            return cody;

        }

        public override bool Decode(string tag, string data)
        {
            if (tags == null)
                tags = new List<Exploring_STD>();
            tags.Add(new Exploring_STD(tag, data));
            return true;
        }

     
    }

    [Serializable]
    public class SavedISTD : IPEGI, IGotName, IPEGI_ListInspect
    {
        
        ISTD Std => ISTD_ExplorerData.inspectedSTD;

        public string NameForPEGI { get { return dataExplorer.tag; } set { dataExplorer.tag = value; } }
        public string comment;
        public Exploring_STD dataExplorer = new Exploring_STD("", "");


#if PEGI
        public static ISTD_ExplorerData Mgmt => ISTD_ExplorerData.inspected;

        public bool PEGI()
        {
            bool changed = false;


            if (dataExplorer.inspectedTag == -1)  {
                this.inspect_Name();
                if (Std != null && dataExplorer.tag.Length > 0 && icon.Save.Click("Save To Assets"))
                {
                    ResourceSaver.Save_ToAssets_ByRelativePath(Mgmt.fileFolderHolder, dataExplorer.tag, dataExplorer.data);
                    Std.RefreshAssetDatabase();
                }
   
                pegi.nl();
                
                if (Std != null)
                {
                    if (dataExplorer.tag.Length == 0)
                        dataExplorer.tag = Std.ToPEGIstring() + " config";

                    "Save To:".edit(50, ref Mgmt.fileFolderHolder);

                    var uobj = Std as UnityEngine.Object;

                    if (uobj && icon.Done.Click("Use the same directory as current object."))
                        Mgmt.fileFolderHolder = uobj.GetAssetFolder();

                    uobj.clickHighlight();

                    pegi.nl();
                }


                "Comment:".editBig(ref comment).nl();
            }


          
            
            dataExplorer.Nested_Inspect();
            
            return changed;
        }

        public bool PEGI_inList(IList list, int ind, ref int edited)
        {
            bool changed = false;

            pegi.edit(ref dataExplorer.tag, 100);

            if (Std != null)
            {
                if (icon.Load.ClickUnfocus("Decode Data into " + Std.ToPEGIstring()))
                    Std.Decode(dataExplorer.data);
                if (icon.Save.ClickUnfocus("Save data from " + Std.ToPEGIstring()))
                    dataExplorer = new Exploring_STD(dataExplorer.tag, Std.Encode().ToString());
            }

            if (icon.Enter.Click(comment))
                edited = ind;

            return changed;
        }

#endif
    }

    [Serializable]
    public class ISTD_ExplorerData
    {
        public List<SavedISTD> states = new List<SavedISTD>();
        public int inspectedState = -1;
        public string fileFolderHolder = "STDEncodes";
        public static ISTD inspectedSTD;
        public bool SaveToFileOptions;
        
#if PEGI

        public static bool PEGI_Static(ISTD target)
        {
            inspectedSTD = target;

            bool changed = false;
            pegi.write("Load File:", 90);
            target.LoadOnDrop().nl();

            if (icon.Copy.Click("Copy Component Data"))
                STDExtensions.copyBufferValue = target.Encode().ToString();
            
            var comp = target as ComponentSTD;
            if (comp != null && "Clear Component".Click())
                    comp.Reboot();
            

            pegi.nl();

            return changed;
        }
        
        public static ISTD_ExplorerData inspected;
        public bool PEGI(ISTD target)
        {
            bool changed = false;
            inspectedSTD = target;
            inspected = this;

            var aded = "Saved CFGs:".edit_List(states, ref inspectedState, true, ref changed);

            if (aded != null && target != null)
            {
                aded.dataExplorer.data = target.Encode().ToString();
                aded.NameForPEGI = target.ToPEGIstring();
                aded.comment = DateTime.Now.ToString();
            }

            if (inspectedState == -1)
            {
                UnityEngine.Object myType = null;
                if ("From File:".edit(65, ref myType)) {
                    aded = new SavedISTD();
                    aded.dataExplorer.data = ResourceLoader.LoadStory(myType);
                    aded.NameForPEGI = myType.name;
                    aded.comment = DateTime.Now.ToString();
                    states.Add(aded);
                }

                var selfSTD = target as IKeepMySTD;

                if (selfSTD != null)
                {
                    if (icon.Save.Click("Save On itself"))
                        selfSTD.UpdateSTDdata(); //Config_STD = selfSTD.Encode().ToString();
                    var slfData = selfSTD.Config_STD;
                    if (slfData != null && slfData.Length > 0 && icon.Load.Click("Load from itself"))
                        target.Decode(slfData); //.DecodeTagsFor(target);
                 }
                pegi.nl();
            }

            inspectedSTD = null;

            return changed;
        }
#endif
    }

}