using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using System;
using System.Windows;

namespace SharedTools_Stuff
{

    [Serializable]
    public class savedISTD: iPEGI, iGotName  {
        public string _name;
        public string Name { get { return _name; }  set { _name = value; } }
        public string comment;
        public string data;

      iSTD std { get { return iSTD_Explorer.inspected.inspectedSTD; } }

        public bool PEGI() {
            bool changed = false;

            if (std != null) {
                if (icon.Load.ClickUnfocus())
                    std.Decode(data);
                this.PEGI_Name();
                if (icon.save.ClickUnfocus().nl())
                    data = std.Encode().ToString();
            }

            "Comment:".editBig(ref comment).nl();
            "Data:".editBig(ref data).nl();
            if (icon.Copy.Click())
                STDExtensions.copyBufferValue = data;
   
            return changed;
        }
    }

    public class iSTD_Explorer : MonoBehaviour, iPEGI  {

        public static iSTD_Explorer inspected;
        public List<savedISTD> states;
        public int inspectedState = -1;
        public iSTD inspectedSTD;
        public string fileFolderHolder = "STDEncodes";
        public string fileNameHolder = "file Name";
        [SerializeField] bool inspectThis;


        public iSTD_Explorer() {
            if (states == null)
                states = new List<savedISTD>();
        }



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

            if ("STD Explorer".foldout(ref inspectThis).nl()) {
                
                if (inspectedSTD != null) {

                    if ("Save To Assets".Click())
                        inspectedSTD.SaveToAssets(fileFolderHolder, fileNameHolder).RefreshAssetDatabase();

                    pegi.write("Load:", 40);
                    inspectedSTD.LoadOnDrop().nl();

                    if (STDExtensions.copyBufferValue != null && icon.Paste.Click().nl()) {
                        inspectedSTD.Decode(STDExtensions.copyBufferValue);
                        STDExtensions.copyBufferValue = null;
                    }

                    "Folder:".edit(60, ref fileFolderHolder).nl();
                    "Name:".edit("No file extension", 60, ref fileNameHolder).nl();
                    
                    var iki = inspectedSTD as abstractKeepUnrecognized_STD;
                    if (iki != null)
                        iki.PEGI_Unrecognized().nl();

                    var comp = inspectedSTD as ComponentSTD;
                    if (comp != null) {
                        if ("Clear Component".Click().nl())
                            comp.Reboot();
                    }
                    
                }


                var aded = "____ Saved States:".edit(states, ref inspectedState, true, ref changed);

                if (aded != null && inspectedSTD != null) {
                    aded.data = inspectedSTD.Encode().ToString();
                    aded.Name = inspectedSTD.ToString(); 
                    aded.comment = DateTime.Now.ToString();
                    inspectedState = states.Count - 1;
                }

             

            }

            //inspectedSTD = null;

            inspected = null;
            return changed;
        }
    }
}