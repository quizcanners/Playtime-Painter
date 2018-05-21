using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using System;
using System.Windows;

namespace SharedTools_Stuff
{

    [Serializable]
    public class savedISTD
#if !NO_PEGI
        :iPEGI, iGotName 
#endif
    {
        public string _name;
        public string Name { get { return _name; }  set { _name = value; } }
        public string comment;
        public string data;

      iSTD std { get { return iSTD_Explorer.inspected.inspectedSTD; } }
        #if !NO_PEGI
        public bool PEGI() {
            bool changed = false;

            this.PEGI_Name().nl();

            if (std != null) {
                if (icon.Load.ClickUnfocus())
                    std.Decode(data);
                if (icon.save.ClickUnfocus())
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

    public class iSTD_Explorer : MonoBehaviour
#if !NO_PEGI
        , iPEGI
#endif
    {

        public static iSTD_Explorer inspected;
        public List<savedISTD> states;
        public int inspectedState = -1;
        public iSTD inspectedSTD;
        public string fileFolderHolder = "STDEncodes";
        public string fileNameHolder = "file Name";


        public iSTD_Explorer() {
            if (states == null)
                states = new List<savedISTD>();
        }


        #if !NO_PEGI
        public bool PEGI() {
            inspected = this;
            bool changed = false;

            if (inspectedSTD == null)
            {
                MonoBehaviour mono = null;
                if ("Target: ".edit(ref mono).nl()) {
                    if (mono != null)
                        inspectedSTD = mono as iSTD;
                }
            }
                
                if (inspectedSTD != null && inspectedState == -1) {

      
                    "Save Folder:".edit(80, ref fileFolderHolder).nl();
                    "File Name:".edit("No file extension", 80, ref fileNameHolder);

                if (fileNameHolder.Length>0 && icon.save.Click("Save To Assets"))
                    inspectedSTD.SaveToAssets(fileFolderHolder, fileNameHolder).RefreshAssetDatabase();

                pegi.nl();

                pegi.write("Load File:", 90);
                inspectedSTD.LoadOnDrop().nl();

                var iki = inspectedSTD as abstractKeepUnrecognized_STD;
                    if (iki != null)
                        iki.PEGI_Unrecognized().nl();
                    
                    if (icon.Copy.Click("Copy Component Data"))
                            STDExtensions.copyBufferValue = inspectedSTD.Encode().ToString();

                    if (STDExtensions.copyBufferValue != null && icon.Paste.Click("Paste Component Data"))
                        inspectedSTD.Decode(STDExtensions.copyBufferValue);

                    var comp = inspectedSTD as ComponentSTD;
                    if (comp != null) {
                        if ("Clear Component".Click())
                            comp.Reboot();
                    }

                pegi.nl();

                }
                
                

                var aded = "____ Saved States:".edit(states, ref inspectedState, true, ref changed);

           

                if (aded != null && inspectedSTD != null) {
                    aded.data = inspectedSTD.Encode().ToString();
                    aded.Name = inspectedSTD.ToString(); 
                    aded.comment = DateTime.Now.ToString();
                    inspectedState = states.Count - 1;
                }
            
            
            inspected = null;
            return changed;
        }
#endif
    }
}