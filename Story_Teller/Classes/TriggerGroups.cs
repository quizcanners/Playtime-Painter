using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif
using PlayerAndEditorGUI;


namespace StoryTriggerData {

    public class TriggerGroups : abstract_STD {

        static UnnullableSTD<TriggerGroups> _all;

        public UnnullableSTD<Trigger> triggers;

        public const string StoriesFolderName = "Stories";
        public const string triggersFileName = "triggers";

        public override string ToString()
        {
            return "runtime " + myInstantiatedIndex;
        }

        public string GetAssePath() {
            return StoriesFolderName + "/" + ToString();
        }

        public UnnullableLists<STD_Values> taggedBool = new UnnullableLists<STD_Values>();

        public UnnullableSTD<UnnullableLists<STD_Values>> taggedInts = new UnnullableSTD<UnnullableLists<STD_Values>>();

        public TriggerGroups this[int index] { get { return _all[index]; } }

        public static UnnullableSTD<TriggerGroups> all {
            get {
                if (_all == null) {

                    _all = new UnnullableSTD<TriggerGroups>();

                    List<Type> triggerGroups = CsharpFuncs.GetAllChildTypesOf<TriggerGroups>();

                    foreach (Type group in triggerGroups) {
                        
                        TriggerGroups s = (TriggerGroups)Activator.CreateInstance(group);

#if UNITY_EDITOR
                        s.LoadFromAssets( s.GetAssePath(), triggersFileName);

                        Type type = s.getIntegerEnums();

                        if (type != null) {
                            
                            string[] nms = Enum.GetNames(type);

                            int[] indexes = (int[])Enum.GetValues(type);

                            for (int i = 0; i < nms.Length; i++) {
                              
                                Trigger tr = s.triggers[indexes[i]];

                                if (tr.name != nms[i]) {
                                    tr._usage = TriggerUsage.number;
                                    tr.name = nms[i];
                                }
                            }
                        }

                        //Boolean enums
                        type = s.getBooleanEnums();

                        if (type != null) {

                            string[] nms = Enum.GetNames(type);

                            int[] indexes = (int[])Enum.GetValues(type);

                            for (int i = 0; i < nms.Length; i++) {
                                
                                Trigger tr = s.triggers[indexes[i]];

                                if (tr.name != nms[i]) {
                                    tr._usage = TriggerUsage.boolean;
                                    tr.name = nms[i];
                                }
                            }
                        }

#endif

                        all[s.GetHashCode()] = s;
                        browsed = s;
                    }

                }
                return _all;
            }

        }

        static int browsedGroup;

        public static TriggerGroups browsed {get {return all[browsedGroup];}
            set { browsedGroup = value.GetHashCode(); } }

        int myInstantiatedIndex; // If Story was instantiated

        public override int GetHashCode() {
            return myInstantiatedIndex;
        }

        public override string getDefaultTagName() {
            return StoriesFolderName;
        }

        public TriggerGroups() {
            myInstantiatedIndex = UnnullableSTD<TriggerGroups>.IndexOfCurrentlyCreatedUnnulable;
            triggers = new UnnullableSTD<Trigger>();
        }

        public override stdEncoder Encode() {
            var cody = new stdEncoder(); //EncodeData();

                cody.Add("t",triggers);

            return cody;
        }

        public override bool Decode(string tag, string data) {
            switch (tag) {
                case "t": triggers.Decode(data); break;
                default: return false;
            }
            return true;
        }


        public static void Save()  {
                foreach (TriggerGroups s in all.GetAllObjsNoOrder())
                s.SaveToAssets(s.GetAssePath(), triggersFileName);
        }

        public override bool PEGI() {
            Trigger.search_PEGI();

           return PEGI(null);
        }

        public bool PEGI(STD_Values so) {
            
            bool changed = false;

            List<int> indxs;
            List<Trigger> lt = triggers.GetAllObjs(out indxs);

            int showMax = 20;

            string groupName = this.ToString();

            for (int i = 0; i < lt.Count; i++) {
                Trigger t = lt[i];

                if (t.SearchCompare(groupName)){ //rigger.searchField.SearchCompare(t.name)) {//  .Length < 1) || Regex.IsMatch(t.name, Trigger.searchField, RegexOptions.IgnoreCase))) {
                    showMax--;

                    Trigger.searchMatchesFound++;

                    t.PEGI();

                    if (t._usage.hasMoreTriggerOptions()) {
                        if (Trigger.edited != t) {
                            if (icon.Edit.Click(20))
                                Trigger.edited = t;
                        } else if (icon.Close.Click(20))
                            Trigger.edited = null;
                    }
                       
                    changed |= t._usage.editTrigger_And_Value_PEGI(indxs[i], this, so);

                    pegi.newLine();

                    if (t._usage.hasMoreTriggerOptions()) {
                        pegi.Space();
                        pegi.newLine();
                    }

                }
                if (showMax < 0) break;
                }

            return changed;

            }

        public bool searchTriggers_PEGI() {

            bool changed = false;

            List<int> indxs;
            List<Trigger> lt = triggers.GetAllObjs(out indxs);

            int showMax = 20;

            for (int i = 0; i < lt.Count; i++) {
                Trigger t = lt[i];

                if (((Trigger.searchField.Length < 1) || Regex.IsMatch(t.name, Trigger.searchField, RegexOptions.IgnoreCase))) {
                    showMax--;

                    Trigger.searchMatchesFound++;

                    t.PEGI();

                    if (t._usage.hasMoreTriggerOptions()) {
                        if (Trigger.edited != t) {
                            if (icon.Edit.Click(20))
                                Trigger.edited = t;
                        } else if (icon.Close.Click(20))
                            Trigger.edited = null;
                    }

                    changed |= t._usage.editTrigger_And_Value_PEGI(indxs[i], this, null);

                    pegi.newLine();

                    if (t._usage.hasMoreTriggerOptions()) {
                        pegi.Space();
                        pegi.newLine();
                    }

                }
                if (showMax < 0) break;
            }

            return changed;

        }


        public bool AddTrigger_PEGI( Argument arg) {

            bool changed = false;

            Trigger selectedTrig = arg!= null ? arg.trig : null;

            if ((Trigger.searchMatchesFound==0) && (Trigger.searchField.Length > 3) && (!Trigger.searchField.isIncludedIn(selectedTrig.name)))  {

                if ((selectedTrig != null)
                    && pegi.Click("Rename " + selectedTrig.name)) {
                    selectedTrig.name = Trigger.searchField;
                    changed = true;
                }



                if (pegi.Click("CREATE [" + Trigger.searchField + "]")) {
                    
                    int ind = triggers.AddNew();
                    if (arg != null) {
                        arg.groupIndex = GetHashCode();
                        arg.triggerIndex = ind;
                    }
                    Trigger t = triggers[ind];
                    t.name = Trigger.searchField;
                    changed = true;
                }

                int slctd = browsed.GetHashCode();
                if (pegi.select(ref slctd, all))
                    browsed = all[slctd];

                pegi.newLine();
            }

            return changed;
        }

        public virtual Type getIntegerEnums() {
            return null;
        }

        public virtual Type getBooleanEnums() {
            return null;
        }
    }
}

