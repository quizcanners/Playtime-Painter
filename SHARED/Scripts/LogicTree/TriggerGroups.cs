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

using SharedTools_Stuff;

namespace STD_Logic
{
    
   

    public class TriggerGroup : abstractKeepUnrecognized_STD
     #if PEGI
        , IGotName, IGotIndex, IPEGI
    #endif
    {

        public static UnnullableSTD<TriggerGroup> all = new UnnullableSTD<TriggerGroup>();

        public const string StoriesFolderName = "Stories";

        public static void FindAllTriggerGroups() {
            all = new UnnullableSTD<TriggerGroup>();

            List<Type> triggerGroups = CsharpFuncs.GetAllChildTypesOf<TriggerGroup>();

            foreach (Type group in triggerGroups) {
                TriggerGroup s = (TriggerGroup)Activator.CreateInstance(group);
                Browsed = s;
            }
        }

        public static TriggerGroup Browsed
        {
            get { return all[browsedGroup]; }
            set { browsedGroup = value.GetHashCode(); }
        }

        public UnnullableSTD<Trigger> triggers = new UnnullableSTD<Trigger>();

        int index;
        public int GetIndex()
        {
            return index;
        }

        public void SetIndex(int val)
        {
            index = val;
        }

        public string name = "Unnamed_Triggers";

        public string NameForPEGI { get { return name; } set { name = value; } }

        public override int GetHashCode()
        {
            return index;
        }

        static int browsedGroup;

        public override string ToString() {
            return name;
        }

        public string GetAssetPath() {
            return StoriesFolderName + "/" + ToString();
        }

        [NonSerialized]
        public UnnullableLists<Values> taggedBool = new UnnullableLists<Values>();

        public UnnullableSTD<UnnullableLists<Values>> taggedInts = new UnnullableSTD<UnnullableLists<Values>>();

        public TriggerGroup() {

            index = UnnullableSTD<TriggerGroup>.IndexOfCurrentlyCreatedUnnulable;

            triggers = new UnnullableSTD<Trigger>();
            
#if UNITY_EDITOR

            Type type = GetIntegerEnums();

            if (type != null) {

                string[] nms = Enum.GetNames(type);

                int[] indexes = (int[])Enum.GetValues(type);

                for (int i = 0; i < nms.Length; i++)
                {

                    Trigger tr = triggers[indexes[i]];

                    if (tr.name != nms[i])
                    {
                        tr._usage = TriggerUsage.number;
                        tr.name = nms[i];
                    }
                }
            }

            //Boolean enums
            type = GetBooleanEnums();

            if (type != null) {

                string[] nms = Enum.GetNames(type);

                int[] indexes = (int[])Enum.GetValues(type);

                for (int i = 0; i < nms.Length; i++)
                {

                    Trigger tr = triggers[indexes[i]];

                    if (tr.name != nms[i])
                    {
                        tr._usage = TriggerUsage.boolean;
                        tr.name = nms[i];
                    }
                }
            }

#endif
        }

        public override StdEncoder Encode() {
            var cody = new StdEncoder(); 
            cody.Add_String("n", name);
            cody.Add("t",triggers);
            cody.Add("ind", index);
            return cody;
        }

        public override bool Decode(string tag, string data) {
            switch (tag) {
                case "n": name = data; break;
                case "t": triggers.Decode(data); break;
                case "ind": index = data.ToInt(); break;
                default: return false;
            }
            return true;
        }
        
        public static void SaveAll()  {
            foreach (TriggerGroup s in all)
                s.Save();
        }

        public void Save()
        {
            this.SaveToAssets(GetAssetPath(), name);
        }

        public static void LoadAll() {
            foreach (TriggerGroup s in all)
                s.Load();
        }

        public void Load()
        {
              this.LoadFromAssets(GetAssetPath(), name);

        }
         #if PEGI
        public override bool PEGI() {
            bool changed = false;
            changed |= (index+" Name").edit(60, ref name).nl();
            
            Trigger.search_PEGI();

            changed |= PEGI(null);

            return changed;
        }
        
        public bool PEGI(Values so) {
            
            bool changed = false;

            int showMax = 20;
            
            foreach (Trigger t in triggers)
            {

                if (t.SearchCompare(name))
                {
                    showMax--;

                    Trigger.searchMatchesFound++;

                    t.PEGI();

                    if (t._usage.hasMoreTriggerOptions())
                    {
                        if (Trigger.edited != t)
                        {
                            if (icon.Edit.Click(20))
                                Trigger.edited = t;
                        }
                        else if (icon.Close.Click(20))
                            Trigger.edited = null;
                    }

                    changed |= t._usage.editTrigger_And_Value_PEGI(triggers.currentEnumerationIndex, this, so).nl();
                    
                    if (t._usage.hasMoreTriggerOptions()) {
                        pegi.Space();
                        pegi.newLine();
                    }

                }
                if (showMax < 0) break;
            }

            pegi.nl();

            return changed;

        }

        public bool SearchTriggers_PEGI() {

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
        
        public bool AddTrigger_PEGI( ValueIndex arg) {

            bool changed = false;

            Trigger selectedTrig = arg!= null ? arg.trig : null;

            if ((Trigger.searchMatchesFound==0) && (Trigger.searchField.Length > 3) 
                && (selectedTrig == null || !Trigger.searchField.IsIncludedIn(selectedTrig.name)))  {

                if ((selectedTrig != null)
                    && pegi.Click("Rename " + selectedTrig.name)) {
                    selectedTrig.name = Trigger.searchField;
                    changed = true;
                }
                
                if (icon.Add.Click("CREATE [" + Trigger.searchField + "]")) {
                    
                    int ind = triggers.AddNew();
                    if (arg != null) {
                        arg.groupIndex = GetHashCode();
                        arg.triggerIndex = ind;
                    }
                    Trigger t = triggers[ind];
                    t.name = Trigger.searchField;
                    changed = true;
                }

                int slctd = Browsed.GetHashCode();
                if (pegi.select(ref slctd, all))
                    Browsed = all[slctd];

                pegi.newLine();
            }

            return changed;
        }

#endif

        public virtual Type GetIntegerEnums() {
            return null;
        }

        public virtual Type GetBooleanEnums() {
            return null;
        }

    }
}

