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
        public string Name { get; set; }
        public string comment;
        public string data;

      iSTD std { get { return iSTD_Explorer.inspected.inspectedSTD; } }

        public bool PEGI() {
            bool changed = false;

            if (icon.Load.ClickUnfocus())
                std.Decode(data);
            this.PEGI_Name();
            if (icon.save.ClickUnfocus().nl())
                data = std.Encode().ToString();

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
        [SerializeField] bool inspectThis;


        public iSTD_Explorer() {
            if (states == null)
                states = new List<savedISTD>();
        }

        public bool PEGI() {
            inspected = this;
            bool changed = false;

            if ("STD Explorer".foldout(ref inspectThis).nl()) {

                if (inspectedSTD == null)
                    "Not attached to anythin".nl();
                else
                {

                    "Saved States:".nl();

                    var aded = states.PEGI(ref inspectedState, true, ref changed);

                    if (STDExtensions.copyBufferValue != null && icon.Paste.Click().nl()) {
                        inspectedSTD.Decode(STDExtensions.copyBufferValue);
                        STDExtensions.copyBufferValue = null;
                    }

                    if (aded!= null) {
                        aded.data = inspectedSTD.Encode().ToString();
                        inspectedState = states.Count - 1;
                    }

                    var iki = inspectedSTD as abstractKeepUnrecognized_STD;
                    if (iki != null)
                        iki.PEGI().nl();
                    

                }
            }

            //inspectedSTD = null;

            inspected = null;
            return changed;
        }
    }
}