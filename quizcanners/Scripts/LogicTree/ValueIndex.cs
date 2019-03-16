using UnityEngine;
using System.Collections;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace STD_Logic
{

    public abstract class ValueIndex : ICfg, IGotDisplayName {

        public int groupIndex;
        public int triggerIndex;

        public ValueIndex TriggerIndexes { set { if (value != null) {
               groupIndex = value.groupIndex;
               triggerIndex = value.triggerIndex;
        }}}

    
        #region Encode & Decode
        public void Decode(string data) => data.DecodeTagsFor(this);

        public abstract StdEncoder Encode();
        public abstract bool Decode(string tg, string data);

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

        #endregion

        protected int GetInt(Values st) => st.ints[groupIndex][triggerIndex];

        public void SetInt(Values st, int value) => st.ints[groupIndex][triggerIndex] = value;

        protected bool GetBool(Values st) => st.booleans[groupIndex][triggerIndex];

        public void SetBool(Values st, bool value) => st.booleans[groupIndex][triggerIndex] = value;

        public Trigger Trigger {
            get { return Group[triggerIndex]; }
            set { groupIndex = value.groupIndex; triggerIndex = value.triggerIndex; } }

        public TriggerGroup Group => TriggerGroup.all[groupIndex];
        
        public abstract bool IsBoolean { get; }

        protected virtual bool SearchTriggerSameType => false;
        
        #region Inspector
        #if PEGI

        public static Trigger selectedTrig;
        public static ValueIndex selected;

        public virtual bool Inspect()
        {
            return false;
        }

        public static string focusName;

        public static ValueIndex edited;

        public virtual bool PEGI_inList_Sub(IList list, int ind, ref int inspecte) => false;
        
        public virtual bool PEGI_inList(IList list, int ind, ref int inspected)
        {

            var changed = false;

            if (this != edited) {
                changed = PEGI_inList_Sub(list, ind, ref inspected);

                if (icon.Edit.ClickUnFocus())
                    edited = this;

                changed |= SearchAndAdd_Triggers_PEGI(ind);
            }
            else
            {
                if (icon.FoldedOut.Click())
                    edited = null;

                Trigger.inspected = Trigger;

                Trigger.Inspect_AsInList();

                if (Trigger.inspected != Trigger)
                    edited = null;
            }


            return changed;
        }
        
        public bool FocusedField_PEGI(int index, string prefix) {

            bool changed = false;

                focusName = "{0}{1}_{2}".F(prefix,index,groupIndex);

                pegi.NameNext(focusName);

                string tmpname = Trigger.name;

                if (Trigger.focusIndex == index)
                    changed |= pegi.edit(ref Trigger.searchField);
                else
                    changed |= pegi.edit(ref tmpname);

            return changed;
        }

        public bool SearchAndAdd_Triggers_PEGI(int index) {
            bool changed = false;

            Trigger t = Trigger;

            if (this == edited)
                changed |= t.Inspect();

            if ((pegi.nameFocused == (focusName)) && (this != edited)) {
                selected = this;

                if (Trigger.focusIndex != index) {
                    Trigger.focusIndex = index;
                    Trigger.searchField = Trigger.name;
                }

                if (Search_Triggers_PEGI(Trigger.searchField, Values.global))
                    Trigger.searchField = Trigger.name;

                selectedTrig = Trigger;

            }
            else if (index == Trigger.focusIndex) Trigger.focusIndex = -2;

            if (this == selected)
                changed |= TriggerGroup.AddTrigger_PEGI(this);

            return changed;
        }

        public bool Search_Triggers_PEGI(string search, Values so) {

            bool changed = false;

            Trigger current = Trigger;

            Trigger.searchMatchesFound = 0;

            if (KeyCode.Return.IsDown().nl(ref changed))
                pegi.FocusControl("none");

            int searchMax = 20;

            current.ToPegiString().write();

            if (icon.Done.Click().nl(ref changed))
                pegi.FocusControl("none");
            else foreach (var gb in TriggerGroup.all) {
                    var lst = gb.GetFilteredList(ref searchMax,
                        !SearchTriggerSameType || IsBoolean ,
                        !SearchTriggerSameType || !IsBoolean );
                    foreach (var t in lst)
                        if (t != current) {
                            Trigger.searchMatchesFound++;

                            if (icon.Done.ClickUnFocus(20).changes(ref changed)) 
                                Trigger = t;
                            
                            t.ToPegiString().nl();
                        }

            }
            return changed;
        }

        public virtual string NameForDisplayPEGI => Trigger.ToPegiString();
#endif
        #endregion
    }

    public static class ValueSettersExtensions
    {

        public static ValueIndex SetLastUsedTrigger(this ValueIndex index) {
            if (index != null)
                index.TriggerIndexes = TriggerGroup.TryGetLastUsedTrigger();
            return index;
        }

        public static bool Get(this UnNullableCfg<CountlessBool> uc, ValueIndex ind) => uc[ind.groupIndex][ind.triggerIndex];
        public static void Set(this UnNullableCfg<CountlessBool> uc, ValueIndex ind, bool value) => uc[ind.groupIndex][ind.triggerIndex] = value;

        public static int Get(this UnNullableCfg<CountlessInt> uc, ValueIndex ind) => uc[ind.groupIndex][ind.triggerIndex];
        public static void Set(this UnNullableCfg<CountlessInt> uc, ValueIndex ind, int value) => uc[ind.groupIndex][ind.triggerIndex] = value;


#if PEGI
        public static bool Toggle(this UnNullableCfg<CountlessBool> uc, ValueIndex ind)
        {
            var tmp = uc.Get(ind);//[ind.groupIndex][ind.triggerIndex];
            if (pegi.toggleIcon(ref tmp))
            {
                uc.Set(ind, tmp);
                return true;
            }
            return false;
        }

        public static bool Edit(this UnNullableCfg<CountlessInt> uc, ValueIndex ind)
        {
            var tmp = uc.Get(ind);//[ind.groupIndex][ind.triggerIndex];
            if (pegi.edit(ref tmp))
            {
                uc.Set(ind, tmp);
                return true;
            }
            return false;
        }

        public static bool Select(this UnNullableCfg<CountlessInt> uc, Trigger t)
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