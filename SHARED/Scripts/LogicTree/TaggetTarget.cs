using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using PlayerAndEditorGUI;

using SharedTools_Stuff;

namespace STD_Logic
{
    
    public class TaggedTarget: ValueIndex, iSTD {  // if there are zero
      
        public int targValue; // if zero - we are talking about bool target

        public stdEncoder Encode() {

            stdEncoder cody = new stdEncoder();
            cody.Add("g", groupIndex);
            cody.Add("t",triggerIndex);
            cody.Add_ifNotZero("v", targValue);

            return cody;
        }

        public bool Decode(string tag, string data) {
            switch (tag) {
                case "g" : groupIndex = data.ToInt(); break;
                case "t": triggerIndex = data.ToInt(); break;
                case "v": targValue = data.ToInt(); break;
                default: return false;
            }
            return true;
        }
        
        public override bool isBoolean() {
            return targValue == 0;
        }
        
        public string tagName { get { return trig.name + (isBoolean() ?  "" : trig.enm[triggerIndex]);}}

        public List<Values> getObjectsByTag() {
            if (targValue > 0)
                return TriggerGroup.all[groupIndex].taggedInts[triggerIndex][targValue];
            else 
                return TriggerGroup.all[groupIndex].taggedBool[triggerIndex];
        }
#if PEGI
        public override bool PEGI() {
            bool changed = false;
           
            if (Trigger.edited != trig) {
                if (icon.Edit.Click(20))
                    Trigger.edited = trig;

                string focusName = "Tt";
                int index = pegi.NameNextUnique(ref focusName);

                string tmpname = trig.name;

                if (Trigger.focusIndex == index)
                    changed |= pegi.edit(ref Trigger.searchField);
                else
                    changed |= pegi.edit(ref tmpname);

            } else if (icon.Close.Click(20))
                Trigger.edited = null;

            return false;
        }

#endif

        public const string stdTag_TagTar = "tagTar";
        public string getDefaultTagName() {
            return stdTag_TagTar;
        }

        public iSTD Decode(string data) => data.DecodeInto(this);

    }
    
}
