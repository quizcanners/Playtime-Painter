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

    public abstract class ValueIndex : ISTD, IPEGI, IGotDisplayName
    {

        public int groupIndex;
        public int triggerIndex;

        public ISTD Decode(string data) => data.DecodeTagsFor(this);

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

        public void SetBool(Values st, bool value) => st.bools[groupIndex][triggerIndex] = value;

        public Trigger Trigger { get { return Group[triggerIndex]; } set { groupIndex = value.groupIndex; triggerIndex = value.triggerIndex; } }

        public TriggerGroup Group => TriggerGroup.all[groupIndex];

        public abstract bool IsBoolean();
#if !NO_PEGI
        public static Trigger selectedTrig;
        public static ValueIndex selected;

        public virtual bool PEGI()
        {
            return false;
        }

        public static string focusName;

        public bool PEGI(int index, string prefix)
        {
            bool changed = false;

            if (Trigger.edited != Trigger)
            {
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
                changed |= t._usage.Inspect(t);
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

                if (Search_PEGI(Trigger.searchField, Values.current))
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

            Trigger current = Trigger;

            Trigger.searchMatchesFound = 0;

            if (KeyCode.Return.IsDown())
            {
                pegi.FocusControl("none");
                changed = true;
            }
            pegi.newLine();


            // List<TriggerGroup> lst = TriggerGroup.all.GetAllObjsNoOrder();

            int searchMax = 20;

            current.ToPEGIstring().write();

            if (icon.Done.Click().nl())
            {
                pegi.FocusControl("none");
                changed = true;
            }
            else
                foreach (var gb in TriggerGroup.all)
                {
                    var lst = gb.GetFilteredList(ref searchMax);
                    foreach (var t in lst)
                    {
                        if (t != current)
                        {
                            Trigger.searchMatchesFound++;
                            t.ToPEGIstring().write();
                            if (icon.Done.ClickUnfocus(20).nl())
                            {
                                Trigger = t;
                                changed = true;
                            }
                        }
                    }
                }
            return changed;
        }

        public virtual string NameForPEGIdisplay() => Trigger.ToPEGIstring();
#endif
    }

    public static class ValueSettersExtensions
    {
        public static bool Get(this UnnullableSTD<CountlessBool> uc, ValueIndex ind) => uc[ind.groupIndex][ind.triggerIndex];
        public static void Set(this UnnullableSTD<CountlessBool> uc, ValueIndex ind, bool value) => uc[ind.groupIndex][ind.triggerIndex] = value;

        public static int Get(this UnnullableSTD<CountlessInt> uc, ValueIndex ind) => uc[ind.groupIndex][ind.triggerIndex];
        public static void Set(this UnnullableSTD<CountlessInt> uc, ValueIndex ind, int value) => uc[ind.groupIndex][ind.triggerIndex] = value;


#if !NO_PEGI
        public static bool Toogle(this UnnullableSTD<CountlessBool> uc, ValueIndex ind)
        {
            var tmp = uc.Get(ind);//[ind.groupIndex][ind.triggerIndex];
            if (pegi.toggle(ref tmp))
            {
                uc.Set(ind, tmp);
                return true;
            }
            return false;
        }

        public static bool Edit(this UnnullableSTD<CountlessInt> uc, ValueIndex ind)
        {
            var tmp = uc.Get(ind);//[ind.groupIndex][ind.triggerIndex];
            if (pegi.edit(ref tmp))
            {
                uc.Set(ind, tmp);
                return true;
            }
            return false;
        }

        public static bool Select(this UnnullableSTD<CountlessInt> uc, Trigger t)
        {
            var tmp = uc.Get(t);
            if (pegi.select(ref tmp, t.enm))
            {
                uc.Set(t, tmp);
                return true;
            }
            return false;
        }

#endif

    }

}