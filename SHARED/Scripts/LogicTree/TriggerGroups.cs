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
    
    public class TriggerGroup : AbstractKeepUnrecognized_STD
     #if PEGI
        , IGotName, IGotIndex, IPEGI
    #endif
    {

        public static UnnullableSTD<TriggerGroup> all = new UnnullableSTD<TriggerGroup>();

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
            set { browsedGroup = value.GetIndex(); }
        }

        public UnnullableSTD<Trigger> triggers = new UnnullableSTD<Trigger>();

        public bool showInInspectorBrowser = true;

        public string name = "Unnamed_Triggers";
        int index;
        public int GetIndex() => index;
        public void SetIndex(int val) =>  index = val;
        
        public string NameForPEGI { get { return name; } set { name = value; } }

        public override int GetHashCode() => index;
        
        static int browsedGroup;

        public override string ToString() => name;
        
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
            cody.Add_ifTrue("show", showInInspectorBrowser);
            return cody;
        }

        public override bool Decode(string tag, string data) {
            switch (tag) {
                case "n": name = data; break;
                case "t":  data.DecodeInto(out triggers); break; 
                case "ind": index = data.ToInt(); break;
                case "show": showInInspectorBrowser = data.ToBool(); break;
                default: return false;
            }
            return true;
        }
        
        public static void SaveAll(string path)  {
            foreach (TriggerGroup s in all)
                s.Save(path);
        }

        public void Save(string path)
        {
            this.SaveToAssets(path, name);
        }

        public static void LoadAll(string path) {
            foreach (TriggerGroup s in all)
                s.Load(path);
        }

        public void Load(string path)
        {
              this.LoadFromAssets(path, name);

        }

         #if PEGI
       
        public override bool PEGI() {
            
            bool changed = false;

            if (Values.inspected == null)
            {
                changed |= base.PEGI();
                if (showDebug)
                    return changed;

                changed |= (index + " Name").edit(60, ref name).nl();
                Trigger.search_PEGI();
            }

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

                    changed |= t._usage.inspect(triggers.currentEnumerationIndex, this).nl();
                    
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

                    changed |= t._usage.inspect(indxs[i], this);

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

            Trigger selectedTrig = arg!= null ? arg.Trigger : null;

            if ((Trigger.searchMatchesFound==0) && (Trigger.searchField.Length > 3) )
              //  
            {

                if ((selectedTrig != null && !selectedTrig.name.SameAs(Trigger.searchField))
                    && pegi.Click("Rename " + selectedTrig.name)) {
                    selectedTrig.name = Trigger.searchField;
                    changed = true;
                }

                if (selectedTrig == null || !Trigger.searchField.IsIncludedIn(selectedTrig.name)) {
                   if (icon.Add.Click("CREATE [" + Trigger.searchField + "]")) {

                        int ind = triggers.AddNew();
                        if (arg != null) {
                            arg.groupIndex = index;
                            arg.triggerIndex = ind;
                        }
                        Trigger t = triggers[ind];
                        t.name = Trigger.searchField;
                        changed = true;
                    }

                    int slctd = Browsed.GetIndex();
                    if (pegi.select(ref slctd, all))
                        Browsed = all[slctd];

                }
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

