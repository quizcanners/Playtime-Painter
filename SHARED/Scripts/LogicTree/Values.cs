using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace LogicTree { 

    public class Values: abstractKeepUnrecognized_STD    {

        public UnnullableSTD<CountlessBool> bools;
        public UnnullableSTD<CountlessInt> ints;
        public UnnullableSTD<CountlessInt> enumTags;
        public UnnullableSTD<CountlessBool> boolTags;
        public CountlessBool groupsToShowInBrowser = new CountlessBool();

        public override string getDefaultTagName(){
            return "STD_ValuesBase";
        }

        public override stdEncoder Encode() {
            var cody = new stdEncoder();
            cody.Add("inst", ints);
            cody.Add("bools", bools);
            cody.Add("tags", boolTags);
            cody.Add("enumTags", enumTags);
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

            TriggerGroups s = TriggerGroups.all[groupIndex];

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

            TriggerGroups s = TriggerGroups.all[groupIndex];

            if (s.taggedInts[tagIndex][value].Contains(this)) {
                if (value != 0)
                    return;
                else
                    s.taggedInts[tagIndex][value].Remove(this);

            }
            else if (value != 0)
                s.taggedInts[tagIndex][value].Add(this);
        }
        
        public override iSTD Decode(string data) {
            Reboot();
            new stdDecoder(data).DecodeTagsFor(this);
            return this;
        }

        public virtual void Reboot() {
            ints = new UnnullableSTD<CountlessInt>();
            bools = new UnnullableSTD<CountlessBool>();
            boolTags = new UnnullableSTD<CountlessBool>();
            enumTags = new UnnullableSTD<CountlessInt>();
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

        public override bool PEGI() {
            bool changed = false;

            pegi.foldout("All Triggers", ref Trigger.showTriggers);

            if (Trigger.showTriggers) {

                if (pegi.Click("quest++"))
                    LogicMGMT.AddQuestVersion();

                pegi.ClickToEditScript();

                pegi.newLine();

                TargetedResult.showOnExit = false;
                TargetedResult.showOnEnter = false;

                pegi.newLine();

                Trigger.search_PEGI();

                List<TriggerGroups> lst = TriggerGroups.all.GetAllObjsNoOrder();

                Trigger.searchMatchesFound = 0;

                for (int i = 0; i < lst.Count; i++) {
                    TriggerGroups td = lst[i];
                    td.PEGI(this);
                }

                TriggerGroups.browsed.AddTrigger_PEGI(null);
                
                groupsToShowInBrowser[TriggerGroups.browsed.GetHashCode()] = true;

            }

            pegi.newLine();

            return changed;
        }

    }
}