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

    public abstract class ValueIndex : iSTD
        #if PEGI
        , IPEGI, IGotDisplayName 
#endif
    {

        public int groupIndex;
        public int triggerIndex;

        public iSTD Decode(string data) => data.DecodeInto(this);

        public abstract StdEncoder Encode();
        public abstract bool Decode(string tag, string data);

        protected StdEncoder EncodeIndex() => new StdEncoder()
            .Add("gi", groupIndex)
            .Add("ti", triggerIndex);
        
        protected bool DecodeIndex(string tag, string data)
        {
            switch (tag)
            {
                case "gi": groupIndex = data.ToInt(); break;
                case "ti": triggerIndex = data.ToInt(); break;
                default: return false;
            }
            return true;
        }
        
        public int GetInt(Values st) => st.ints[groupIndex][triggerIndex];
        
        public void SetInt(Values st, int value) => st.ints[groupIndex][triggerIndex] = value;
        
        public bool GetBool(Values st) => st.bools[groupIndex][triggerIndex];
        
        public void SetBool(Values st, bool value) =>  st.bools[groupIndex][triggerIndex] = value;
        
        public Trigger Trigger => Group.triggers[triggerIndex];

        public TriggerGroup Group => TriggerGroup.all[groupIndex];

        public abstract bool IsBoolean();
#if PEGI
        public static Trigger selectedTrig;
        public static ValueIndex selected;

        public virtual bool PEGI()
        {
            return false;
        }

        public static string focusName;

        public bool PEGI(int index,  string prefix)
        {
            bool changed = false;

            if (Trigger.edited != Trigger) {
                if (icon.Edit.Click(20))
                    Trigger.edited = Trigger;

                focusName = prefix + index;

                pegi.NameNext(focusName);

                string tmpname = Trigger.name;

                if (Trigger.focusIndex == index)
                    changed |= pegi.edit(ref Trigger.searchField);
                else
                    changed |= pegi.edit(ref tmpname);

            }
            else if (icon.Close.Click(20))
                Trigger.edited = null;

            return changed;
        }

        public bool SearchAndAdd_PEGI(int index)
        {
            bool changed = false;

            pegi.newLine();
            pegi.Space();
            pegi.newLine();

            Trigger t = Trigger;

            if (t == Trigger.edited)
            {
                t.PEGI();
                pegi.newLine();
                changed |= t._usage.inspect(this, null);
            }

            if ((pegi.nameFocused == (focusName)) && (t != Trigger.edited))
            {
                selected = this;

                if (Trigger.focusIndex != index)
                {
                    Trigger.focusIndex = index;
                    Trigger.searchField = Trigger.name;
                }

                pegi.newLine();

                if (Search_PEGI(Trigger.searchField, Values.inspected))
                    Trigger.searchField = Trigger.name;

                selectedTrig = Trigger;

            }
            else if (index == Trigger.focusIndex) Trigger.focusIndex = -2;


            pegi.newLine();

            if (selected == this)
                changed |= Group.AddTrigger_PEGI(this);

            return changed;
        }

        public bool Search_PEGI(string search, Values so)
        {

            bool changed = false;

            bool showedFirst = false;

            Trigger current = Trigger;

            Trigger.searchMatchesFound = 0;

            if (KeyCode.Return.IsDown())
            {
                pegi.FocusControl("none");
                changed = true;
            }
            pegi.newLine();
          

            List<TriggerGroup> lst = TriggerGroup.all.GetAllObjsNoOrder();

            for (int i = 0; i < lst.Count; i++)
            {
                TriggerGroup gb = lst[i];
                string gname = gb.ToString();
                List<int> indxs;
                List<Trigger> trl = gb.triggers.GetAllObjs(out indxs);
                for (int j = 0; j < trl.Count; j++)
                {
                    Trigger t = trl[j];
                    int indx = indxs[j];
                    if (((groupIndex != gb.GetIndex()) || (triggerIndex != indx)) && t.SearchCompare(gname))
                    {
                        if (!showedFirst)
                        {
                            pegi.write(current.name);

                            if (icon.Done.Click())//pegi.Click("<>", 20))
                            {
                                pegi.FocusControl("none");
                                changed = true;
                            }
                            pegi.newLine();
                            showedFirst = true;
                        }



                        Trigger.searchMatchesFound++;
                        pegi.write(t.name + "_" + indx);
                        if (icon.Done.Click(20)) { changed = true; triggerIndex = indx; groupIndex = gb.GetIndex(); pegi.DropFocus(); pegi.newLine(); return true; }
                        pegi.newLine();
                    }
                }
            }
            return changed;
        }

        public virtual string NameForPEGIdisplay() => Trigger.ToPEGIstring();
#endif
    }
}