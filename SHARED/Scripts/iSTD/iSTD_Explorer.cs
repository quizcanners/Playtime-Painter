using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using System;
//using System.Windows;

namespace SharedTools_Stuff
{

    public class ElementData : abstract_STD
    {
        public string name;
        public string std_dta;
        public string guid;

        public override bool Decode(string tag, string data)
        {
            switch (tag)
            {
                case "n": name = data; break;
                case "std": std_dta = data; break;
                case "guid": guid = data; break;
                default: return false;
            }
            return true;
        }

        public override stdEncoder Encode()
        {
            var cody = new stdEncoder();
            cody.Add_String("n", name);
            cody.Add_String("std", std_dta);
            cody.Add_String("guid", guid);
            return cody;
        }
    }

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

            this.inspect_Name().nl();

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

        public static bool PEGI_Static(iSTD target)
        {
            bool changed = false;
            pegi.write("Load File:", 90);
            target.LoadOnDrop().nl();
            
            if (icon.Copy.Click("Copy Component Data"))
                STDExtensions.copyBufferValue = target.Encode().ToString();

            if (STDExtensions.copyBufferValue != null && icon.Paste.Click("Paste Component Data"))
                target.Decode(STDExtensions.copyBufferValue);

            var comp = target as ComponentSTD;
            if (comp != null)
            {
                if ("Clear Component".Click())
                    comp.Reboot();
            }

            pegi.nl();

            var iki = target as abstractKeepUnrecognized_STD;
            if (iki != null)
                iki.PEGI_Unrecognized().nl();
            return changed;
        }

        public bool PEGI(iSTD target)
        {
            bool changed = false;
            inspectedSTD = target;
            
            if (target != null && inspectedState == -1)
            {

                "Save Folder:".edit(80, ref fileFolderHolder);

                var uobj = target as UnityEngine.Object;

                if (uobj && icon.Done.Click("Use the same directory as current object")) 
                    fileFolderHolder = uobj.GetAssetFolder();
                
                pegi.nl();
                "File Name:".edit("No file extension", 80, ref fileNameHolder);

                if (fileNameHolder.Length > 0 && icon.Save.Click("Save To Assets"))
                    target.SaveToAssets(fileFolderHolder, fileNameHolder).RefreshAssetDatabase();

                pegi.nl();
                
                PEGI_Static(target);
            }

            var aded = "____ Saved States:".edit_List(states, ref inspectedState, true, ref changed);

            if (aded != null && target != null)
            {
                aded.data = target.Encode().ToString();
                aded.NameForPEGI = target.ToPEGIstring();
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
        public static bool PEGI_Static (iSTD target)
        {
            return iSTD_ExplorerData.PEGI_Static(target);
        }

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