using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace LogicTree
{

    public abstract class ValueIndex {

        public int groupIndex;
        public int triggerIndex;

        public int GetInt(Values st) {
            return st.ints[groupIndex][triggerIndex];
        }

        public void SetInt(Values st, int value) {
            st.ints[groupIndex][triggerIndex] = value;
        }

        public bool GetBool(Values st)
        {
            if (groupIndex < 0) Debug.Log("group is " + groupIndex);
            if (triggerIndex < 0) Debug.Log("trigger index is " + triggerIndex);
            return st.bools[groupIndex][triggerIndex];
        }

        public void SetBool(Values st, bool value)
        {
            st.bools[groupIndex][triggerIndex] = value;
        }

        public Trigger trig
        {
            get
            {
                return TriggerGroups.all[groupIndex].triggers[triggerIndex];
            }
        }

        public TriggerGroups group
        {
            get
            {
                return TriggerGroups.all[groupIndex];
            }
        }

        public abstract bool isBoolean();

        public static Trigger selectedTrig;
        public static ValueIndex selected;

        public virtual bool PEGI()
        {
            return false;
        }

        public static string focusName;
        public static Values editedSo;

        public bool PEGI(int index, ref int DeleteNo, Values so, string prefix)
        {
            bool changed = false;
            editedSo = so;

            if (icon.Delete.Click(20))
            {
                DeleteNo = index;
                changed = true;
            }

            if (Trigger.edited != trig)
            {
                if (icon.Edit.Click(20))
                    Trigger.edited = trig;

                focusName = prefix + index;

                pegi.NameNext(focusName);

                string tmpname = trig.name;

                if (Trigger.focusIndex == index)
                    changed |= pegi.edit(ref Trigger.searchField);
                else
                    changed |= pegi.edit(ref tmpname);

            }
            else if (icon.Close.Click(20))
                Trigger.edited = null;

            return changed;
        }

        public bool searchAndAdd_PEGI(int index)
        {
            bool changed = false;

            pegi.newLine();
            pegi.Space();
            pegi.newLine();

            Trigger t = trig;

            if (t == Trigger.edited)
            {
                t.PEGI();
                pegi.newLine();
                changed |= t._usage.editTrigger_And_Value_PEGI(this, null);
            }

            if ((pegi.nameFocused == (focusName)) && (t != Trigger.edited))
            {
                selected = this;

                if (Trigger.focusIndex != index)
                {
                    Trigger.focusIndex = index;
                    Trigger.searchField = trig.name;
                }

                pegi.newLine();

                if (search_PEGI(Trigger.searchField, editedSo))
                    Trigger.searchField = trig.name;

                selectedTrig = trig;

            }
            else if (index == Trigger.focusIndex) Trigger.focusIndex = -2;


            pegi.newLine();

            if (selected == this)
                changed |= group.AddTrigger_PEGI(this);

            return changed;
        }

        public bool search_PEGI(string search, Values so)
        {

            bool changed = false;

            Trigger current = trig;

            Trigger.searchMatchesFound = 0;

            if (KeyCode.Return.isDown())
            {
                pegi.FocusControl("none");
                changed = true;
            }
            pegi.newLine();
            pegi.write(current.name);

            if (pegi.Click("<>", 20))
            {
                pegi.FocusControl("none");
                changed = true;
            }
            pegi.newLine();

            List<TriggerGroups> lst = TriggerGroups.all.GetAllObjsNoOrder();

            for (int i = 0; i < lst.Count; i++)
            {
                TriggerGroups gb = lst[i];
                string gname = gb.ToString();
                List<int> indxs;
                List<Trigger> trl = gb.triggers.GetAllObjs(out indxs);
                for (int j = 0; j < trl.Count; j++)
                {
                    Trigger t = trl[j];
                    int indx = indxs[j];
                    if (((groupIndex != gb.GetHashCode()) || (triggerIndex != indx)) && t.SearchCompare(gname))
                    {//search.SearchCompare(t.name)) {
                        Trigger.searchMatchesFound++;
                        pegi.write(t.name + "_" + indx);
                        if (icon.Done.Click(20)) { changed = true; triggerIndex = indx; groupIndex = gb.GetHashCode(); pegi.DropFocus(); pegi.newLine(); return true; }
                        pegi.newLine();
                    }
                }
            }
            return changed;
        }


    }
}