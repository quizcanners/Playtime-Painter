using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using System;
//using System.Windows;

namespace SharedTools_Stuff
{

    [Serializable]
    public class savedISTD
#if PEGI
        : iPEGI, iGotName
#endif
    {
        public string _name;
        public string NameForPEGI { get { return _name; } set { _name = value; } }
        public string comment;
        public string data;

        iSTD std { get { return iSTD_ExplorerData.inspectedSTD; } }
#if PEGI
        public bool PEGI()
        {
            bool changed = false;

            this.PEGI_Name().nl();

            if (std != null)
            {
                if (icon.Load.ClickUnfocus())
                    std.Decode(data);
                if (icon.Save.ClickUnfocus())
                    data = std.Encode().ToString();
                if (icon.Copy.Click().nl())
                    STDExtensions.copyBufferValue = data;
            }

            "Comment:".editBig(ref comment).nl();
            "Data:".editBig(ref data).nl();

            return changed;
        }

#endif
    }

    [Serializable]
    public class iSTD_ExplorerData
    {
        public List<savedISTD> states = new List<savedISTD>();
        public int inspectedState = -1;
        public string fileFolderHolder = "STDEncodes";
        public string fileNameHolder = "file Name";
        public static iSTD inspectedSTD;


#if PEGI
        public bool PEGI(iSTD target)
        {
            bool changed = false;
            inspectedSTD = target;
            
            if (inspectedSTD != null && inspectedState == -1)
            {
                
                "Save Folder:".edit(80, ref fileFolderHolder).nl();
                "File Name:".edit("No file extension", 80, ref fileNameHolder);

                if (fileNameHolder.Length > 0 && icon.Save.Click("Save To Assets"))
                    inspectedSTD.SaveToAssets(fileFolderHolder, fileNameHolder).RefreshAssetDatabase();

                pegi.nl();

                pegi.write("Load File:", 90);
                inspectedSTD.LoadOnDrop().nl();

          

                if (icon.Copy.Click("Copy Component Data"))
                    STDExtensions.copyBufferValue = inspectedSTD.Encode().ToString();

                if (STDExtensions.copyBufferValue != null && icon.Paste.Click("Paste Component Data"))
                    inspectedSTD.Decode(STDExtensions.copyBufferValue);
                
                var comp = inspectedSTD as ComponentSTD;
                if (comp != null)
                {
                    if ("Clear Component".Click())
                        comp.Reboot();
                }

                pegi.nl();

                var iki = inspectedSTD as abstractKeepUnrecognized_STD;
                if (iki != null)
                    iki.PEGI_Unrecognized().nl();

              

            }

            var aded = "____ Saved States:".edit_List(states, ref inspectedState, true, ref changed);

            if (aded != null && inspectedSTD != null)
            {
                aded.data = inspectedSTD.Encode().ToString();
                aded.NameForPEGI = inspectedSTD.ToPEGIstring();
                aded.comment = DateTime.Now.ToString();
                inspectedState = states.Count - 1;
            }


            inspectedSTD = null;

            return changed;
        }
#endif
    }

    public class iSTD_Explorer : MonoBehaviour
#if PEGI
        , iPEGI
#endif
    {
        public iSTD ConnectSTD;
        public iSTD_ExplorerData data = new iSTD_ExplorerData();

#if PEGI
        public bool PEGI()
        {

            UnityEngine.Object obj = ConnectSTD == null ? null : ConnectSTD as UnityEngine.Object;
            if ("Target Obj: ".edit(60, ref obj))
            {
                if (obj != null)
                    ConnectSTD = obj as iSTD;
            }


            MonoBehaviour mono = ConnectSTD == null ? null : ConnectSTD as MonoBehaviour;
            if ("Target Obj: ".edit(60, ref mono).nl())
            {
                if (mono != null)
                    ConnectSTD = mono as iSTD;
            }
            
            return data.PEGI(ConnectSTD);

        }
#endif



    }
}