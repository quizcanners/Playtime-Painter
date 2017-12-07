using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using PlayerAndEditorGUI;
/* Story Tell Tool Requirements:
 * Many Subprojects can use the same tool and merge nicely. This means every implementation needs to have database instance - a unique static class. All the triggers will be dependable on it.
 * For each object you can select which Trigger groups are relevant to it.
 * 
 * 
 */

namespace StoryTriggerData {


    //   [AddComponentMenu("Logic/StoryTelll")]
    //   [ExecuteInEditMode]
    //PlaytimeToolComponent,

    public class STD_Values : iSTD {

        public const string storyTag = "story";

        public InteractionBranch iGroup;
        public List<string> interactionReferences;

        // This are used to modify triggers when interacting with the object
        public List<Result> OnEnterResults = new List<Result>();
        public List<Result> OnExitResults = new List<Result>();

        public UnnullableSTD<CountlessBool> bools;
        public UnnullableSTD<CountlessInt> ints;

        public UnnullableSTD<CountlessBool> boolTags;
        public UnnullableSTD<CountlessInt> enumTags;

        public CountlessBool groupsToShowInBrowser = new CountlessBool();

        public QOoptionType type;

        public string getDefaultTagName() {
            return storyTag;
        }

        public stdEncoder Encode() {
            var cody = new stdEncoder();

            cody.Add("i", iGroup);
            cody.AddIfNotEmpty("ent", OnEnterResults);
            cody.AddIfNotEmpty("ext", OnExitResults);
            cody.Add("inst", ints);
            cody.Add("bools", bools);
            cody.Add("tags", boolTags);
            cody.Add("enumTags", enumTags);
            cody.Add("qoType", (int)type);

            return cody;
        }

        public void Decode(string tag, string data) {
            switch (tag) {
                case "i": iGroup = new InteractionBranch(data);

                    List<Interaction> lst = new List<Interaction>();
                    iGroup.getAllInteractions(ref lst);

                    foreach (Interaction si in lst)
                        if (si.reference.Length > 0)
                            interactionReferences.Add(si.reference);
                            
                    break;//.Add(new StoryEvent(data)); break;
                case "ent": OnEnterResults = data.ToListOf_STD<Result>(); break;
                case "ext": OnExitResults = data.ToListOf_STD<Result>(); break;
                case "ints": ints.Reboot(data); break;
                case "bools": bools.Reboot(data); break;
                case "tags": boolTags.Reboot(data); break;
                case "enumTags": enumTags.Reboot(data); break;
                case "qoType": type = (QOoptionType)data.ToInt(); break; 
            }
        }

        public void Reboot(string data) {
            Reboot();
            new stdDecoder(data).DecodeTagsFor(this);
        }

        public void Reboot() {
            myQuestVersion = 0;
            iGroup = new InteractionBranch();
            iGroup.name = "ROOT";
            ints = new UnnullableSTD<CountlessInt>();
            bools = new UnnullableSTD<CountlessBool>();
            boolTags = new UnnullableSTD<CountlessBool>();
            enumTags = new UnnullableSTD<CountlessInt>();
            interactionReferences = new List<string>();
        }

        public static int questVersion = 0;
        public int myQuestVersion = -1;

        public static void AddQuestVersion() {
            questVersion++;
            //Debug.Log("Story triggers changed");
        }

        public void SetTagBool(int groupIndex, int tagIndex, bool value) {
            
            boolTags[groupIndex][tagIndex] = value;

            TriggerGroups s = TriggerGroups.all[groupIndex];

            if (s.taggedBool[tagIndex].Contains(this)){
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

            } else if (value!= 0)
                s.taggedInts[tagIndex][value].Add(this);
        }

        public void UpdateLogic()
        {
            myQuestVersion = questVersion;
        }

        public void Update()
        {
            if (myQuestVersion != questVersion)
                UpdateLogic();
        }

        public STD_Values() {
            Reboot();
        }

        public void removeAllTags() {
            List<int> groupInds;
            List<CountlessBool> lsts = boolTags.GetAllObjs(out groupInds);
            //Stories.all.GetAllObjs(out inds);

            for (int i = 0; i < groupInds.Count; i++) {
                CountlessBool vb = lsts[i];
                List<int> tag = vb.GetItAll();

                foreach (int t in tag)
                    SetTagBool(groupInds[i], t, false);

            }

            boolTags = new UnnullableSTD<CountlessBool>();
        }

        public bool browsing_interactions = false;

        public void PEGI() {

            if (!browsing_interactions) {
                
                if (this != Dialogue.browsedObj) {

                    pegi.newLine();

                    pegi.foldout("All Triggers", ref Trigger.showTriggers);
                  

                    if (Trigger.showTriggers) {

                        if (pegi.Click("quest++"))
                            questVersion++;

                        pegi.ClickToEditScript();

                        pegi.newLine();

                        Result.showOnExit = false;
                        Result.showOnEnter = false;

                        //groupFilter_PEGI();

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

                    pegi.write("Interactions: ", iGroup.getShortDescription());

                    if (icon.Edit.Click("Edit Interaction Tree", 20))
                        browsing_interactions = true;

                    pegi.newLine();

                    if (pegi.foldout("__ON_ENTER" + OnEnterResults.ToStringSafe(Result.showOnEnter), ref Result.showOnEnter)) {
                        Result.showOnExit = false;
                        Trigger.showTriggers = false;

                        OnEnterResults.PEGI(this);

                    }
                    pegi.newLine();
                    if (pegi.foldout("__ON_EXIT" + OnExitResults.ToStringSafe(Result.showOnExit), ref Result.showOnExit)) {
                        Result.showOnEnter = false;
                        Trigger.showTriggers = false;

                        OnExitResults.PEGI(this);

                    }
                    pegi.newLine();
                }



            } else {

                if (pegi.Click("<"+STD_Object.browsed.gameObject.name, 40))
                    browsing_interactions = false;
                else {
                    pegi.newLine();
                    iGroup.PEGI(this);
                }
            }

        }


        public void groupFilter_PEGI() {

            List<TriggerGroups> lst = TriggerGroups.all.GetAllObjsNoOrder();

            for (int i = 0; i < lst.Count; i++) {
                TriggerGroups td = lst[i];
                pegi.write(td + "_" + td.GetHashCode(), 230);
                pegi.toggle(td.GetHashCode(), groupsToShowInBrowser);
                pegi.newLine();
            }


        }

    }
}