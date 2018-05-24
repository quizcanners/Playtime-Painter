using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace STD_Logic {


    public class Values: abstractKeepUnrecognized_STD    {

        public UnnullableSTD<CountlessBool> bools = new UnnullableSTD<CountlessBool>();
        public UnnullableSTD<CountlessInt> ints = new UnnullableSTD<CountlessInt>();
        public UnnullableSTD<CountlessInt> enumTags = new UnnullableSTD<CountlessInt>();
        public UnnullableSTD<CountlessBool> boolTags = new UnnullableSTD<CountlessBool>();
        public CountlessBool groupsToShowInBrowser = new CountlessBool();

        public override stdEncoder Encode() {
            var cody = new stdEncoder();
            cody.Add("ints", ints);
            cody.Add("bools", bools);
            cody.Add("tags", boolTags);
            cody.Add("enumTags", enumTags);
            cody.Add("Test Unrecognized values", 1);
            return cody;
        }

        public override bool Decode(string tag, string data) {
            switch (tag) {
                case "ints": data.DecodeInto(out ints); break;
                case "bools": data.DecodeInto(out bools); break;
                case "tags": data.DecodeInto(out boolTags); break;
                case "enumTags": data.DecodeInto(out enumTags); break;
                default: return false;
            }
            return true;
        }

        public void SetTagBool(int groupIndex, int tagIndex, bool value) {

            boolTags[groupIndex][tagIndex] = value;

            TriggerGroup s = TriggerGroup.all[groupIndex];

            if (s.taggedBool[tagIndex].Contains(this))
            {
                if (value)
                    return;
                else
                    s.taggedBool[tagIndex].Remove(this);

            }
            else if (value)
                s.taggedBool[tagIndex].Add(this);
        }

        public void SetTagEnum(int groupIndex, int tagIndex, int value) {

            enumTags[groupIndex][tagIndex] = value;

            TriggerGroup s = TriggerGroup.all[groupIndex];

            if (s.taggedInts[tagIndex][value].Contains(this)) {
                if (value != 0)
                    return;
                else
                    s.taggedInts[tagIndex][value].Remove(this);

            }
            else if (value != 0)
                s.taggedInts[tagIndex][value].Add(this);
        }
        
        public void removeAllTags() {
            List<int> groupInds;
            List<CountlessBool> lsts = boolTags.GetAllObjs(out groupInds);
            //Stories.all.GetAllObjs(out inds);

            for (int i = 0; i < groupInds.Count; i++)
            {
                CountlessBool vb = lsts[i];
                List<int> tag = vb.GetItAll();

                foreach (int t in tag)
                    SetTagBool(groupInds[i], t, false);

            }

            boolTags = new UnnullableSTD<CountlessBool>();
        }
#if PEGI
        public override bool PEGI() {
            
            bool changed = false;
            
                if ("quest++".Click().nl())
                    LogicMGMT.AddLogicVersion();
                
                pegi.newLine();

                "Click Enter to apply renaming.".writeOneTimeHint("EntApplyTrig");

                Trigger.search_PEGI();

                Trigger.searchMatchesFound = 0;

                foreach (TriggerGroup td in TriggerGroup.all) 
                    td.PEGI(this);
  
                TriggerGroup.browsed.AddTrigger_PEGI(null);
                
                groupsToShowInBrowser[TriggerGroup.browsed.GetHashCode()] = true;
                
            pegi.nl();

            changed |= base.PEGI();

            pegi.newLine();
            
            return changed;
        }
#endif
    }
}