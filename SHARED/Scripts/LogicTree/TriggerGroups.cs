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

    public class TriggerGroup : AbstractKeepUnrecognized_STD, IGotName, IGotIndex, IPEGI, IPEGI_ListInspect {

        public static UnnullableSTD<TriggerGroup> all = new UnnullableSTD<TriggerGroup>();
        
        public static void FindAllTriggerGroups()
        {
            all = new UnnullableSTD<TriggerGroup>();

            List<Type> triggerGroups = CsharpFuncs.GetAllChildTypesOf<TriggerGroup>();

            foreach (Type group in triggerGroups)
            {
                TriggerGroup s = (TriggerGroup)Activator.CreateInstance(group);
                Browsed = s;
            }
        }
        
        UnnullableSTD<Trigger> triggers = new UnnullableSTD<Trigger>();

        public UnnullableLists<Values> taggedBool = new UnnullableLists<Values>();

        public UnnullableSTD<UnnulSTDLists<Values>> taggedInts = new UnnullableSTD<UnnulSTDLists<Values>>();
        
        string name = "Unnamed_Triggers";
        int index;

        #region Getters & Setters 

        public int Count => triggers.GetAllObjsNoOrder().Count;

        public Trigger this[int index]
        {
            get
            {
                if (index >= 0)
                {
                    var ready = triggers.GetIfExists(index);
                    if (ready != null)
                        return ready;

                    ready = triggers[index];
                    ready.groupIndex = IndexForPEGI;
                    ready.triggerIndex = index;
#if PEGI
                    listDirty = true;
#endif
                    return ready;
                }
                else return null;
            }
        }

        public virtual Type GetIntegerEnums() => null;

        public virtual Type GetBooleanEnums() => null;

        public int IndexForPEGI { get { return index; } set { index = value; } }

        public string NameForPEGI { get { return name; } set { name = value; } }
        
        public void Add(string name, ValueIndex arg = null)
        {
            int ind = triggers.AddNew();
            Trigger t = this[ind];
            t.name = name;
            t.groupIndex = IndexForPEGI;
            t.triggerIndex = ind;

            if (arg != null)
            {
                if (arg.IsBoolean)
                    t._usage = TriggerUsage.boolean;
                else
                    t._usage = TriggerUsage.number;

                arg.Trigger = t;
            }

            lastUsedTrigger = ind;

#if PEGI
            listDirty = true;
#endif
        }

        #endregion

        #region Encode_Decode

        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add_String("n", name)
            .Add("ind", index)
            .Add_IfNotDefault("t", triggers)
            .Add("br", browsedGroup)
            .Add_IfTrue("show", showInInspectorBrowser)
            .Add("last", lastUsedTrigger);

        public override bool Decode(string tag, string data) {
            switch (tag) {
                case "n": name = data; break;
                case "ind": index = data.ToInt(); break;
                case "t":
                    data.DecodeInto(out triggers);
                    foreach (var t in triggers){
                        t.groupIndex = index;
                        t.triggerIndex = triggers.currentEnumerationIndex;
                    }
                    break;
                case "br": browsedGroup = data.ToInt(); break;
                case "show": showInInspectorBrowser = data.ToBool(); break;
                case "last": lastUsedTrigger = data.ToInt(); break;
                default: return false;
            }
            return true;
        }

        public override void Decode(string data)
        {
#if PEGI
            listDirty = true;
#endif
            base.Decode(data);
        }

        #endregion

        #region Inspector
      
        static int browsedGroup = -1;
        public bool showInInspectorBrowser = true;
        int lastUsedTrigger = 0;

        public Trigger LastUsedTrigger
        {
            get { return triggers.GetIfExists(lastUsedTrigger); }
            set { if (value != null) lastUsedTrigger = value.triggerIndex; }
        }

        public static Trigger TryGetLastUsedTrigger()
        {
            if (Browsed != null)
                return Browsed.LastUsedTrigger;
            return null;
        }

        public static TriggerGroup Browsed
        {
            get { return browsedGroup >= 0 ? all[browsedGroup] : null; }
            set { browsedGroup = value != null ? value.IndexForPEGI : -1; }
        }

        #if PEGI
        bool listDirty;

        string lastFilteredString = "";

        List<Trigger> filteredList = new List<Trigger>();

        public List<Trigger> GetFilteredList(ref int showMax, bool showBools = true, bool showInts = true)
        {
            if (!showInInspectorBrowser)
            {
                filteredList.Clear();
                return filteredList;
            }

            if (!listDirty && lastFilteredString.SameAs(Trigger.searchField))
                return filteredList;
            else  {

                filteredList.Clear();
                foreach (Trigger t in triggers)
                    if ((t.IsBoolean ? showBools : showInts) && t.SearchWithGroupName(name)) {
                        showMax--;

                        Trigger.searchMatchesFound++;

                        filteredList.Add(t);

                        if (showMax < 0)
                            break;
                    }

                lastFilteredString = Trigger.searchField;
#if PEGI
                listDirty = false;
#endif
            }
            return filteredList;
        }

        public static TriggerGroup inspected;

        public bool ListInspecting() {
            bool changed = false;

            int showMax = 20;

            var lst = GetFilteredList(ref showMax);

            if (lst.Count > 0 || Trigger.searchField.Length == 0) {
                if (this.ToPEGIstring().toggleIcon(ref showInInspectorBrowser))
                    changed |= this.ToPEGIstring().write_List(lst);
            }

            return changed;
        }

        public bool PEGI_inList(IList list, int ind, ref int edited) {
            var changed = this.inspect_Name();

            if (icon.Enter.ClickUnfocus())
                edited = ind;

            if (icon.Email.Click("Send this Trigger Group to somebody via email."))
                this.EmailData("Trigger Group {0} [index: {1}]".F(name, index), "Use this Trigger Group in your Node Books");

            return changed;
        }
        
        public override bool Inspect()  {

            inspected = this;

            bool changed = false;

            if (inspectedStuff == -1) {


                changed |= "{0} : ".F(index).edit(50, ref name).nl();


                "Share:".write("Paste message full with numbers and lost of ' | ' symbols into the first line or drop file into second" ,50);
                
                string data;
                if (this.Send_Recieve_PEGI("Trigger Group {0} [{1}]".F(name, index), "Trigger Groups", out data)) {
                    TriggerGroup tmp = new TriggerGroup();
                    tmp.Decode(data);
                    if (tmp.index == index) {

                        Decode(data);
                        Debug.Log("Decoded Trigger Group {0}".F(name));
                    }
                    else
                        Debug.LogError("Pasted trigger group had different index, replacing");
                }

           
                pegi.Line();

                "New Variable".edit(80, ref Trigger.searchField);
                AddTriggerToGroup_PEGI();
                
                changed |= triggers.Nested_Inspect(); 
            }
            inspected = null;

            return changed;

        }

        public bool AddTriggerToGroup_PEGI(ValueIndex arg = null)
        {
            bool changed = false;

            if ((Trigger.searchMatchesFound == 0) && (Trigger.searchField.Length > 3)) {

                Trigger selectedTrig = arg?.Trigger;

                if (selectedTrig == null || !Trigger.searchField.IsIncludedIn(selectedTrig.name)) {
                    if (icon.Add.Click("CREATE [" + Trigger.searchField + "]").changes(ref changed)) {
                        Add(Trigger.searchField, arg);
                        pegi.DropFocus();
                    }
                }
            }

            return changed; 
        }

        public static bool AddTrigger_PEGI(ValueIndex arg = null) {

            bool changed = false;

            Trigger selectedTrig = arg?.Trigger;

            if (Trigger.searchMatchesFound == 0 && selectedTrig != null && !selectedTrig.name.SameAs(Trigger.searchField)) {

                bool goodLength = Trigger.searchField.Length > 3;

                pegi.nl();

                if (goodLength && icon.Replace.ClickUnfocus(
                    "Rename {0} if group {1} to {2}".F(selectedTrig.name, selectedTrig.Group.ToPEGIstring(), Trigger.searchField)
                    ).changes(ref changed)) selectedTrig.Using().name = Trigger.searchField;
                
                bool differentGroup = selectedTrig.Group != Browsed && Browsed != null;

                if (goodLength && differentGroup)
                    icon.Warning.write("Trigger {0} is of group {1} not {2}".F(selectedTrig.ToPEGIstring(), selectedTrig.Group.ToPEGIstring(), Browsed.ToPEGIstring()));

                var groupLost = all.GetAllObjsNoOrder();
                if (groupLost.Count > 0) {
                    int slctd = Browsed == null ? -1 : Browsed.IndexForPEGI;

                    if (pegi.select(ref slctd, all)) 
                        Browsed = all[slctd];
                }
                else
                    "No Trigger Groups found".nl();

                if (goodLength && Browsed != null)
                    Browsed.AddTriggerToGroup_PEGI(arg);
            }

            pegi.nl();

            return changed;
        }
#endif

        #endregion
        
        public TriggerGroup() {

            index = UnnullableSTD<TriggerGroup>.IndexOfCurrentlyCreatedUnnulable;
            
            #if UNITY_EDITOR
            // Will use provided enums to create hardcoded trigger list.
            Type type = GetIntegerEnums();

            if (type != null)  {

                string[] nms = Enum.GetNames(type);

                int[] indexes = (int[])Enum.GetValues(type);

                for (int i = 0; i < nms.Length; i++) {

                    Trigger tr = triggers[indexes[i]];

                    if (tr.name != nms[i])  {
                        tr._usage = TriggerUsage.number;
                        tr.name = nms[i];
                    }
                }
            }

            //Boolean enums
            type = GetBooleanEnums();

            if (type != null)   {
                string[] nms = Enum.GetNames(type);

                int[] indexes = (int[])Enum.GetValues(type);

                for (int i = 0; i < nms.Length; i++) {
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

    }
}

