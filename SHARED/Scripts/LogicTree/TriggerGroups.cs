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

    public class TriggerGroup : AbstractKeepUnrecognized_STD, IGotName, IGotIndex, IPEGI
    {

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

        static int browsedGroup = -1;
        public static TriggerGroup Browsed
        {
            get { return browsedGroup >= 0 ? all[browsedGroup] : null; }
            set { browsedGroup = value != null ? value.IndexForPEGI : -1; }
        }

        UnnullableSTD<Trigger> triggers = new UnnullableSTD<Trigger>();

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

        public bool showInInspectorBrowser = true;

        string name = "Unnamed_Triggers";
        int index;

        public int IndexForPEGI { get { return index; } set { index = value; } }

        public string NameForPEGI { get { return name; } set { name = value; } }

        public override string ToString() => name;

        [NonSerialized]
        public UnnullableLists<Values> taggedBool = new UnnullableLists<Values>();

        public UnnullableSTD<UnnullableLists<Values>> taggedInts = new UnnullableSTD<UnnullableLists<Values>>();

        public TriggerGroup()
        {

            index = UnnullableSTD<TriggerGroup>.IndexOfCurrentlyCreatedUnnulable;

            triggers = new UnnullableSTD<Trigger>();

#if UNITY_EDITOR

            Type type = GetIntegerEnums();

            if (type != null)
            {

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

            if (type != null)
            {

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

        #region Encode_Decode

        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add_String("n", name)
            .Add("ind", index)
            .Add("t", triggers)
            .Add("br", browsedGroup)
            .Add_IfTrue("show", showInInspectorBrowser);

        public override bool Decode(string tag, string data)
        {
            switch (tag)
            {
                case "n": name = data; break;
                case "ind": index = data.ToInt(); break;
                case "t":
                    data.DecodeInto(out triggers);
                    foreach (var t in triggers)
                    {
                        t.groupIndex = index;
                        t.triggerIndex = triggers.currentEnumerationIndex;
                    }
                    break;
                case "br": browsedGroup = data.ToInt(); break;
                case "show": showInInspectorBrowser = data.ToBool(); break;
                default: return false;
            }
            return true;
        }

        public override ISTD Decode(string data)
        {
#if PEGI
            listDirty = true;
#endif
            return base.Decode(data);
        }

        #endregion

        #region Inspector
        
#if PEGI

        bool listDirty;

        string lastFilteredString = "";

        List<Trigger> filteredList = new List<Trigger>();
        public List<Trigger> GetFilteredList(ref int showMax)
        {
            if (!listDirty && lastFilteredString.SameAs(Trigger.searchField))
                return filteredList;
            else
            {
                //Debug.Log("Refiltering from {0} to {1}, because {2}".F(lastFilteredString, Trigger.searchField, lastFilteredString.SameAs(Trigger.searchField)));

                filteredList.Clear();
                foreach (Trigger t in triggers)
                    if (t.SearchWithGroupName(name))
                    {
                        showMax--;

                        Trigger.searchMatchesFound++;

                        filteredList.Add(t);

                        if (showMax < 0)
                            break;
                    }

                lastFilteredString = Trigger.searchField;
                listDirty = false;
            }
            return filteredList;
        }

        public static TriggerGroup inspected;

        public bool ListInspecting()
        {
            bool changed = false;

            int showMax = 20;

            var lst = GetFilteredList(ref showMax);

            if (lst.Count > 0 || Trigger.searchField.Length == 0)
            {
                if (showInInspectorBrowser && icon.FoldedOut.Click())
                    showInInspectorBrowser = false;

                if (showInInspectorBrowser)
                    changed |= this.ToPEGIstring().write_List(lst);
                else
                    changed |= this.ToPEGIstring().foldout(ref showInInspectorBrowser);
            }

            return changed;
        }

        public override bool PEGI()
        {

            inspected = this;

            bool changed = false;

            changed |= base.PEGI();

            if (showDebug)
                return changed;

            changed |= "{0} Name".F(index).edit(60, ref name).nl();
            Trigger.Search_PEGI();

            changed |= ListInspecting().nl();

            inspected = null;

            return changed;

        }

        public bool SearchTriggers_PEGI()
        {

            bool changed = false;

            List<int> indxs;
            List<Trigger> lt = triggers.GetAllObjs(out indxs);

            int showMax = 20;

            for (int i = 0; i < lt.Count; i++)
            {
                Trigger t = lt[i];

                if (((Trigger.searchField.Length < 1) || Regex.IsMatch(t.name, Trigger.searchField, RegexOptions.IgnoreCase)))
                {
                    showMax--;

                    Trigger.searchMatchesFound++;

                    t.PEGI();

                    if (t._usage.HasMoreTriggerOptions())
                    {
                        if (Trigger.editedTrigger != t)
                        {
                            if (icon.Edit.Click(20))
                                Trigger.editedTrigger = t;
                        }
                        else if (icon.Close.Click(20))
                            Trigger.editedTrigger = null;
                    }

                    changed |= t._usage.Inspect(t).nl();

                    if (t._usage.HasMoreTriggerOptions())
                    {
                        pegi.Space();
                        pegi.newLine();
                    }

                }
                if (showMax < 0) break;
            }

            return changed;

        }

        public bool AddTrigger_PEGI(ValueIndex arg)
        {

            bool changed = false;

            Trigger selectedTrig = arg?.Trigger;

            if ((Trigger.searchMatchesFound == 0) && (Trigger.searchField.Length > 3))
            {

                if ((selectedTrig != null && !selectedTrig.name.SameAs(Trigger.searchField))
                    && "Rename {0}".F(selectedTrig.name).Click())
                {
                    selectedTrig.name = Trigger.searchField;
                    changed = true;
                }

                if (selectedTrig == null || !Trigger.searchField.IsIncludedIn(selectedTrig.name))
                {
                    if (icon.Add.Click("CREATE [" + Trigger.searchField + "]"))
                    {
                        int ind = triggers.AddNew();
                        Trigger t = this[ind];
                        t.name = Trigger.searchField;
                        t.groupIndex = IndexForPEGI;
                        t.triggerIndex = ind;

                        if (arg != null)
                        {
                            if (arg.IsBoolean())
                                t._usage = TriggerUsage.boolean;
                            else
                                t._usage = TriggerUsage.number;

                            arg.Trigger = t;
                        }

                        listDirty = true;

                        changed = true;
                    }
                }

                var trigList = all.GetAllObjsNoOrder();

                if (trigList.Count > 0)
                {
                    int slctd = Browsed == null ? -1 : Browsed.IndexForPEGI;
                    if (pegi.select(ref slctd, all))
                        Browsed = all[slctd];
                }
                else
                    "No Trigger Groups found".nl();

                pegi.nl();
            }

            return changed;
        }

#endif

        #endregion

        public virtual Type GetIntegerEnums()
        {
            return null;
        }

        public virtual Type GetBooleanEnums()
        {
            return null;
        }

    }
}

