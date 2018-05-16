using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace LogicTree
{

    // Trigger usage is only used for PEGI. Logic engine will not need this to process triggers

    public abstract class TriggerUsage  {
        
        protected static List<TriggerUsage> usgs = new List<TriggerUsage>();

        public static TriggerUsage get(int ind) {
            return usgs[ind];
        }

        public static List<string> names = new List<string>();

        public static bool select_PEGI(ref int ind) {
            return pegi.select(ref ind, usgs, 45);
        }

        public bool editTrigger_And_Value_PEGI(ValueIndex arg, Values so) {
            return editTrigger_And_Value_PEGI(arg.triggerIndex, arg.group, so);
        }

        public virtual void conditionPEGI(Condition c, Values so) {
   
        }

        public virtual bool resultsPEGI(TargetedResult r, Values so) {
            return false;
        }

        public virtual bool hasMoreTriggerOptions() {
            return false;
        }

        public virtual bool editTrigger_And_Value_PEGI(int ind, TriggerGroups group, Values so) {
            bool changed = false;
            Trigger t = group.triggers[ind];
            string before = t.name;
            if (pegi.editDelayed(ref before, 150 - (hasMoreTriggerOptions() ? 30 : 0))) {
                Trigger.searchField = before;
                t.name = before;
                pegi.FocusControl("none");
                changed = true;
            }
            return changed;
        }

        public virtual bool usingBool() {
            return false;
        }

        public virtual bool usingEnum() {
            return false;
        }

        public static Usage_Boolean boolean = new Usage_Boolean(0);
        public static Usage_Number number = new Usage_Number(1);
        public static Usage_BoolTag boolTag = new Usage_BoolTag(2);
        public static Usage_IntTag intTag = new Usage_IntTag(3);
        public static Usage_GameTimeStemp timestamp = new Usage_GameTimeStemp(4);
        public static Usage_RealTimestemp realTime = new Usage_RealTimestemp(5);
        public static StringEnum enumer = new StringEnum(6);
        public static Usage_Pointer pointer = new Usage_Pointer(7);
      

        public int index;

        public TriggerUsage(int ind) {
            index = ind;

            while (names.Count <= ind) names.Add("");
            while(usgs.Count <= ind) usgs.Add(null);

            names[ind] = ToString();
            usgs[ind]= this;
        }
    }

    public class Usage_Number : TriggerUsage {
        
        public override string ToString() { return string.Format("Number"); }

        public static readonly Dictionary<int,string> conditionUsages = new Dictionary<int, string> { 
            { ((int)ConditionType.Equals), "==" },
            { ((int)ConditionType.Above), ">" },
            { ((int)ConditionType.Below), "<" },
            { ((int)ConditionType.NotEquals), "!=" },
        };

        public static readonly Dictionary<int, string> resultUsages = new Dictionary<int, string> {
            {(int)ResultType.Set, "="},
            {(int)ResultType.Add, "+"},
            {(int)ResultType.Subtract, "-"},
        };

        public override void conditionPEGI(Condition c, Values so) {
            pegi.select(ref c._type, conditionUsages, 40);
            pegi.edit(ref c.compareValue, 40);
        }

        public override bool resultsPEGI(TargetedResult r, Values so) {
            bool changed = false;
            changed |= pegi.select(ref r._type, resultUsages, 40);
            changed |= pegi.edit(ref r.updateValue, 40);
            return changed;
        }

        public override bool editTrigger_And_Value_PEGI(int ind, TriggerGroups group, Values so) {
            bool changed = base.editTrigger_And_Value_PEGI(ind, group, so);

            if (so != null) 
                changed |= pegi.edit(ind, so.ints[group.GetHashCode()]);

            return changed;
        }

        public Usage_Number(int index) : base(index) { }
    }

    public class Usage_Pointer : TriggerUsage {
      
        public override string ToString() { return string.Format("Pointer"); }

        public Usage_Pointer(int index) : base(index) { }
    }

    public class Usage_GameTimeStemp : TriggerUsage {
       
        public override string ToString() { return string.Format("Game Time"); }

        public static readonly Dictionary<int, string> conditionUsages = new Dictionary<int, string> {
            { ((int)ConditionType.VirtualTimePassedAbove), "Game_Time passed > " },
            { ((int)ConditionType.VirtualTimePassedBelow), "Game_Time passed < " },
        };

        public override void conditionPEGI(Condition c, Values so) {
            pegi.select(ref c._type, conditionUsages);
            pegi.edit(ref c.compareValue);
        }

        public static readonly Dictionary<int, string> resultUsages = new Dictionary<int, string> {
            {(int)ResultType.SetTimeGame, "Set To Game_Now_Time"},
            {(int)ResultType.Add, "+"},
            {(int)ResultType.Subtract, "-"},
            {(int)ResultType.Set, "="},
        };

        public override bool resultsPEGI(TargetedResult r, Values so) {
            bool changed = false;

            changed |= pegi.select(ref r._type, resultUsages);
            if (r.type!= ResultType.SetTimeGame)
                changed |= pegi.edit(ref r.updateValue);

            return changed;
        }


        public Usage_GameTimeStemp(int index) : base(index) { }
    }

    public class Usage_RealTimestemp : TriggerUsage {

        public override string ToString() { return string.Format("Real Time"); }

        public static readonly Dictionary<int, string> conditionUsages = new Dictionary<int, string> {
            { ((int)ConditionType.RealTimePassedAbove), "Real_Time passed > " },
            { ((int)ConditionType.RealTimePassedBelow), "Real_Time passed < " },
        };

        public override void conditionPEGI(Condition c, Values so) {
            pegi.select(ref c._type, conditionUsages);
            pegi.edit(ref c.compareValue);
        }

        public static readonly Dictionary<int, string> resultUsages = new Dictionary<int, string> {
            {(int)ResultType.SetTimeReal, "Set To Real_Now_Time"},
            {(int)ResultType.Add, "+"},
            {(int)ResultType.Subtract, "-"},
        };

        public override bool resultsPEGI(TargetedResult r, Values so) {
            bool changed = false;
            changed |= pegi.select(ref r._type, resultUsages);
            if (r.type != ResultType.SetTimeReal)
                changed |= pegi.edit(ref r.updateValue);
            return changed;
        }

        public Usage_RealTimestemp(int index) : base(index) { }
    }

    public class StringEnum : TriggerUsage {
      
        public override string ToString() { return string.Format("Enums"); }

        public override void conditionPEGI(Condition c, Values so) {
            pegi.select(ref c._type, Usage_Number.conditionUsages);
            pegi.select(ref c.compareValue, c.trig.enm);
        }

        public override bool resultsPEGI(TargetedResult r, Values so) {
            bool changed = false;
            changed |= pegi.select(ref r._type, Usage_Number.resultUsages);
            pegi.select(ref r.updateValue, r.trig.enm);
            return changed;
        }

        public override bool editTrigger_And_Value_PEGI(int ind, TriggerGroups group, Values so) {
            bool changed = base.editTrigger_And_Value_PEGI(ind, group, so);
            Trigger t = group.triggers[ind];

            if (so!= null)
                changed |= pegi.select(so.ints[group.GetHashCode()], ind, t.enm);

            pegi.newLine();

            if (Trigger.edited != t) return changed;

            pegi.write("__ Enums__");

            changed |= t.enm.edit_PEGI();

            return changed;
        }

        public override bool hasMoreTriggerOptions() {
            return true;
        }

        public StringEnum(int index) : base(index) { }
    }

    public class Usage_IntTag : TriggerUsage {

        public override string ToString() { return string.Format("TagGroup"); }

        public override bool editTrigger_And_Value_PEGI(int ind, TriggerGroups group, Values so) {
            var changed = base.editTrigger_And_Value_PEGI(ind, group, so);

            Trigger t = group.triggers[ind];

            int value = so.enumTags[group.GetHashCode()][ind];
            if ((so != null) && (pegi.select(ref value, t.enm))) {
                so.SetTagEnum(group.GetHashCode(), ind, value);
                changed = true;
            }


            pegi.newLine();

            if (Trigger.edited != t) return changed;

            pegi.write("__ Tags __");

            const string NoZerosForTrigs = "No04t";
            pegi.newLine();
            pegi.writeOneTimeHint("Can't use 0 as tag index. ", NoZerosForTrigs);
            pegi.newLine();

            if (t.enm.edit_PEGI()) {
                string dummy;
                if (t.enm.TryGetValue(0, out dummy)) {
                    t.enm.Remove(0);
                    pegi.resetOneTimeHint(NoZerosForTrigs);
                }
                changed = true;
            }

            return changed;
        }

        public override void conditionPEGI(Condition c, Values so) {
            pegi.select(ref c._type, Usage_Number.conditionUsages);
            pegi.select(ref c.compareValue, c.trig.enm);
            //c.trig.enm.//select_or_Edit_PEGI(ref c.compareValue);
        }

        public override bool resultsPEGI(TargetedResult r, Values so) {
            bool changed = false;
            changed |= pegi.select(ref r._type, Usage_Number.resultUsages);
            pegi.select(ref r.updateValue, r.trig.enm);
            return changed;
        }

        public override bool hasMoreTriggerOptions() {
            return true;
        }

        public Usage_IntTag(int index) : base(index) { }
    }

    public class Usage_Boolean : TriggerUsage {

        public override string ToString() { return string.Format("YesNo"); }

        public override void conditionPEGI(Condition c, Values so) {
            pegi.toggleInt(ref c.compareValue);
        }

        public override bool resultsPEGI(TargetedResult r, Values so) {
            
            return pegi.toggleInt(ref r.updateValue);
        }

        public override bool editTrigger_And_Value_PEGI(int ind, TriggerGroups group, Values so) {
            bool changed = base.editTrigger_And_Value_PEGI(ind, group, so);
            if (so != null)
                changed |= pegi.toggle(ind ,so.bools[group.GetHashCode()]);

            return changed;
        }

        public Usage_Boolean(int index) : base(index) { }
    }

    public class Usage_BoolTag : TriggerUsage {
     
        public override string ToString() { return string.Format("Tag"); }

        public override void conditionPEGI(Condition c, Values so) {
            pegi.toggleInt(ref c.compareValue);
        }

        public override bool resultsPEGI(TargetedResult r, Values so) {
            return pegi.toggleInt(ref r.updateValue);
        }

        public override bool editTrigger_And_Value_PEGI(int ind, TriggerGroups group, Values so) {
            bool changed = base.editTrigger_And_Value_PEGI(ind, group, so);

            if (so != null) {
                int gr = group.GetHashCode();
                bool val = so.boolTags[gr][ind];
                if (pegi.toggle(ref val)) {
                    changed = true; 
                    so.SetTagBool(gr, ind, val);
                }
            }

            return changed;
        }

        public Usage_BoolTag(int index) : base(index) { }
    }




}