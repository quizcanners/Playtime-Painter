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


        static int browsedGroup = -1;
        public static TriggerGroup Browsed
        {
            get { return browsedGroup >= 0 ? all[browsedGroup] : null; }
            set { browsedGroup = value != null ? value.IndexForPEGI : -1;  }
        }

        UnnullableSTD<Trigger> triggers = new UnnullableSTD<Trigger>();

        public Trigger this[int index]
        {
            get {
                if (index >= 0)
                {
                    var ready = triggers.GetIfExists(index);
                    if (ready != null)
                        return ready;

                    ready = triggers[index];
                    ready.groupIndex = IndexForPEGI;
                    ready.triggerIndex = index;

                    return ready;
                }
                else return null;
            }
        }

        public bool showInInspectorBrowser = true;

    //    int inspectedTrigger = -1;

        string name = "Unnamed_Triggers";
        int index;


        public int IndexForPEGI { get { return index; } set { index = value; } }

        
        public string NameForPEGI { get { return name; } set { name = value; } }
        
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

        public override StdEncoder Encode() =>this.EncodeUnrecognized()
            .Add_String("n", name)
            .Add("ind", index)
            .Add("t", triggers)
            .Add_ifTrue("show", showInInspectorBrowser);

        public override bool Decode(string tag, string data) {
            switch (tag) {
                case "n": name = data; break;
                case "ind": index = data.ToInt(); break;
                case "t":  data.DecodeInto(out triggers);
                    foreach (var t in triggers) {
                        t.groupIndex = index;
                        t.triggerIndex = triggers.currentEnumerationIndex;
                    }

                    break; 
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

        string lastFilteredString = "";
        List<Trigger> filteredList = new List<Trigger>();
        public List<Trigger> GetFilteredList(ref int showMax)
        {
            if (lastFilteredString.SameAs(Trigger.searchField))
                return filteredList;
            else
            {
                filteredList.Clear();
                foreach (Trigger t in triggers)
                    if (t.SearchWithGroupName(name)) {
                        showMax--;

                        Trigger.searchMatchesFound++;

                        filteredList.Add(t);

                        if (showMax < 0)
                            break;
                    }
            }
            return filteredList;
        }

        public static TriggerGroup inspected;

        public override bool PEGI() {

            inspected = this;

            bool changed = false;

            if (Values.current == null)
            {
                changed |= base.PEGI();
                if (showDebug)
                    return changed;

                changed |= (index + " Name").edit(60, ref name).nl();
                Trigger.search_PEGI();
            }
            
            if (Trigger.edited != null)
            {
                if (icon.Close.Click())
                    Trigger.edited = null;

                Trigger.edited.PEGI();
            }
            else
            {
                int showMax = 20;

                var lst = GetFilteredList(ref showMax);

                "Triggers".write_List(lst);

              /*  foreach (var t in lst)
                {

                    t.PEGI();

                  

                }*/
            }

            pegi.nl();

            inspected = null;

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

                    changed |= t._usage.inspect(t);

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

            Trigger selectedTrig = arg?.Trigger;

            if ((Trigger.searchMatchesFound==0) && (Trigger.searchField.Length > 3) )
            {

                if ((selectedTrig != null && !selectedTrig.name.SameAs(Trigger.searchField))
                    && pegi.Click("Rename " + selectedTrig.name)) {
                    selectedTrig.name = Trigger.searchField;
                    changed = true;
                }

                if (selectedTrig == null || !Trigger.searchField.IsIncludedIn(selectedTrig.name)) {
                   if (icon.Add.Click("CREATE [" + Trigger.searchField + "]")) {
                        int ind = triggers.AddNew();
                        Trigger t = this[ind];
                        t.name = Trigger.searchField;
                        t.groupIndex = IndexForPEGI;
                        t.triggerIndex = ind;

                        if (arg != null) arg.Trigger = t;
                        
                        changed = true;
                    }

                    int slctd = Browsed.IndexForPEGI ;
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

