using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace STD_Logic
{

    // Trigger usage is only used for PEGI. Logic engine will not need this to process triggers

    public abstract class TriggerUsage  {
        
        protected static List<TriggerUsage> usgs = new List<TriggerUsage>();

        public static TriggerUsage get(int ind) {
            return usgs[ind];
        }

        public static List<string> names = new List<string>();
#if PEGI
        public static bool select_PEGI(ref int ind) {
            return pegi.select(ref ind, usgs, 45);
        }

        public bool editTrigger_And_Value_PEGI(ValueIndex arg, Values so) {
            return editTrigger_And_Value_PEGI(arg.triggerIndex, arg.group, so);
        }

        public virtual void conditionPEGI(ConditionLogic c, Values so) {
   
        }

        public virtual bool resultsPEGI(Result r, Values so) {
            return false;
        }

        public virtual void testOnceConditionPEGI(TestOnceCondition c, Values so) {
            conditionPEGI(c, so);

        }

        public virtual bool selectActionPEGI(ref ResultType r, Dictionary<int, string> resultUsages) {
            bool changed = false;
            int t = (int)r;

            if (!resultUsages.ContainsKey(t)) {
                if (("Is " + r.ToString() + ". FIX ").Click())
                    r = (ResultType)(resultUsages.First().Key);
                changed = true;
            }
            else
            {
                if (pegi.select(ref t, resultUsages, 40)) {
                    changed = true;
                    r = (ResultType)t;

                }
            }
            return changed;
        }


        public virtual bool selectActionPEGI(ref ConditionType c, Dictionary<int, string> conditionUsages)
        {
            bool changed = false;
            int t = (int)c;

            if (!conditionUsages.ContainsKey(t))
            {
                if (("Is " + c.ToString() + ". FIX ").Click())
                    c = (ConditionType)(conditionUsages.First().Key);
                changed = true;
            }
            else
            {
                if (pegi.select(ref t, conditionUsages, 40))
                {
                    changed = true;
                    c = (ConditionType)t;

                }
            }
            return changed;
        }


              public virtual bool editTrigger_And_Value_PEGI(int ind, TriggerGroup group, Values so) {
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

#endif

        public virtual bool hasMoreTriggerOptions() {
            return false;
        }

  
        public virtual bool usingBool() {
            return false;
        }

        public virtual bool usingEnum() {
            return false;
        }

        public static Usage_Boolean boolean = new Usage_Boolean(0);
        public static Usage_Number number = new Usage_Number(1);
        public static Usage_StringEnum enumer = new Usage_StringEnum(2);
        // public static Usage_BoolTag boolTag = new Usage_BoolTag(3);
        // public static Usage_IntTag intTag = new Usage_IntTag(4);
        // public static Usage_GameTimeStemp timestamp = new Usage_GameTimeStemp(5);
        // public static Usage_RealTimestemp realTime = new Usage_RealTimestemp(6);
        // public static Usage_Pointer pointer = new Usage_Pointer(7);

        public int index;

        public TriggerUsage(int ind) {
            index = ind;

            while (names.Count <= ind) names.Add("");
            while(usgs.Count <= ind) usgs.Add(null);

            names[ind] = ToString();
            usgs[ind]= this;
        }
    }

    public class Usage_Boolean : TriggerUsage {

        public override string ToString() { return string.Format("YesNo"); }
#if PEGI
        public override void conditionPEGI(ConditionLogic c, Values so) {
            if (!c.isBoolean()){
                if (("Wrong Type: " + c.type.ToString() + " FIX").Click())
                    c.type = ConditionType.Bool;
            }
             else 
            pegi.toggleInt(ref c.compareValue);
        }

        public override void testOnceConditionPEGI(TestOnceCondition c, Values so) {
            conditionPEGI(c, so);

            if (!c.isBoolean()) {
                if (("Wrong Type:" + c.resultType.ToString() + ". Change To Bool").Click())
                    c.resultType = ResultType.SetBool;
                
            }

            pegi.toggleInt(ref c.updateValue);

        }

        public override bool resultsPEGI(Result r, Values so) {

            if (!r.isBoolean())
            {
                if (("Wrong Type:" + r.type.ToString() + ". Change To Bool").Click())
                {
                    r.type = ResultType.SetBool;
                    return true;
                }
                return false;
            }

            return pegi.toggleInt(ref r.updateValue);
        }

        public override bool editTrigger_And_Value_PEGI(int ind, TriggerGroup group, Values so)
        {
            bool changed = base.editTrigger_And_Value_PEGI(ind, group, so);
            if (so != null)
                changed |= pegi.toggle(ind, so.bools[group.GetHashCode()]);

            return changed;
        }
#endif
        public Usage_Boolean(int index) : base(index) { }
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
            {(int)ResultType.Set, ResultType.Set.GetText()},
            {(int)ResultType.Add, ResultType.Add.GetText()},
            {(int)ResultType.Subtract, ResultType.Subtract.GetText()},
        };
#if PEGI
        public override void conditionPEGI(ConditionLogic c, Values so) {

            selectActionPEGI(ref c.type, conditionUsages);
            
            pegi.edit(ref c.compareValue, 40);
        }

        public override void testOnceConditionPEGI(TestOnceCondition c, Values so)
        {
            conditionPEGI(c, so);

            pegi.newLine();
            "If true:".write(60);
            selectActionPEGI(ref c.resultType, resultUsages);

            pegi.edit(ref c.updateValue, 40);
        }

        public override bool resultsPEGI(Result r, Values so) {
            bool changed = false;

            changed |= selectActionPEGI(ref r.type, resultUsages);

            changed |= pegi.edit(ref r.updateValue, 40);
            return changed;
        }

        public override bool editTrigger_And_Value_PEGI(int ind, TriggerGroup group, Values so) {
            bool changed = base.editTrigger_And_Value_PEGI(ind, group, so);

            if (so != null) 
                changed |= pegi.edit(ind, so.ints[group.GetHashCode()]);

            return changed;
        }
#endif

        public Usage_Number(int index) : base(index) { }
    }


    public class Usage_StringEnum : TriggerUsage
    {

        public override string ToString() { return string.Format("Enums"); }
#if PEGI
        public override void conditionPEGI(ConditionLogic c, Values so) {
       
            selectActionPEGI(ref c.type, Usage_Number.conditionUsages);

            pegi.select(ref c.compareValue, c.trig.enm);
        }

        public override void testOnceConditionPEGI(TestOnceCondition c, Values so) {
            conditionPEGI(c, so);

            pegi.newLine();
            "If true:".write(60);
            selectActionPEGI(ref c.resultType , Usage_Number.resultUsages);

            pegi.select(ref c.updateValue, c.trig.enm);
        }

        public override bool resultsPEGI(Result r, Values so)
        {
            bool changed = false;

            changed |= selectActionPEGI(ref r.type, Usage_Number.resultUsages);



            pegi.select(ref r.updateValue, r.trig.enm);
            return changed;
        }

        public override bool editTrigger_And_Value_PEGI(int ind, TriggerGroup group, Values so)
        {
            bool changed = base.editTrigger_And_Value_PEGI(ind, group, so);
            Trigger t = group.triggers[ind];

            if (so != null)
                changed |= pegi.select(so.ints[group.GetHashCode()], ind, t.enm);

            pegi.newLine();

            if (Trigger.edited != t) return changed;

            pegi.write("__ Enums__");

            changed |= t.enm.edit_PEGI();

            return changed;
        }

#endif

        public override bool hasMoreTriggerOptions()
        {
            return true;
        }

        public Usage_StringEnum(int index) : base(index) { }
    }

    /*
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

        public override void conditionPEGI(ConditionLogic c, Values so) {

            int ty = (int)c.type;
            if (pegi.select(ref ty, conditionUsages))
                c.type = (ConditionType)ty;

            
            pegi.edit(ref c.compareValue);
        }

        public static readonly Dictionary<int, string> resultUsages = new Dictionary<int, string> {
            {(int)ResultType.SetTimeGame, ResultType.SetTimeGame.GetText()},
            {(int)ResultType.Add, ResultType.Add.GetText()},
            {(int)ResultType.Subtract, ResultType.Subtract.GetText()},
            {(int)ResultType.Set, ResultType.Set.GetText()},
        };

        public override bool resultsPEGI(Result r, Values so) {
            bool changed = false;


            changed |= selectActionPEGI(r, resultUsages);

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

        public override void conditionPEGI(ConditionLogic c, Values so) {
       
            int ty = (int)c.type;
            if (pegi.select(ref ty, conditionUsages))
                c.type = (ConditionType)ty;

            pegi.edit(ref c.compareValue);
        }

        public static readonly Dictionary<int, string> resultUsages = new Dictionary<int, string> {
            {(int)ResultType.SetTimeReal, ResultType.SetTimeReal.GetText()},
            {(int)ResultType.Add, ResultType.Add.GetText()},
            {(int)ResultType.Subtract, ResultType.Subtract.GetText()},
        };

        public override bool resultsPEGI(Result r, Values so) {
            bool changed = false;

            changed |= selectActionPEGI(r, resultUsages);

          
            if (r.type != ResultType.SetTimeReal)
                changed |= pegi.edit(ref r.updateValue);
            return changed;
        }

        public Usage_RealTimestemp(int index) : base(index) { }
    }

    public class Usage_IntTag : TriggerUsage {

        public override string ToString() { return string.Format("TagGroup"); }

        public override bool editTrigger_And_Value_PEGI(int ind, TriggerGroup group, Values so) {
            var changed = base.editTrigger_And_Value_PEGI(ind, group, so);

            Trigger t = group.triggers[ind];

            int value = so.enumTags[group.GetIndex()][ind];
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

        public override void conditionPEGI(ConditionLogic c, Values so) {
            int ty = (int)c.type;
            if (pegi.select(ref ty, Usage_Number.conditionUsages))
                c.type = (ConditionType)ty;
            pegi.select(ref c.compareValue, c.trig.enm);
            //c.trig.enm.//select_or_Edit_PEGI(ref c.compareValue);
        }

        public override bool resultsPEGI(Result r, Values so) {
            bool changed = false;

            changed |= selectActionPEGI(r, Usage_Number.resultUsages);


         
            
            pegi.select(ref r.updateValue, r.trig.enm);
            return changed;
        }

        public override bool hasMoreTriggerOptions() {
            return true;
        }

        public Usage_IntTag(int index) : base(index) { }
    }

    public class Usage_BoolTag : TriggerUsage {
     
        public override string ToString() { return string.Format("Tag"); }

        public override void conditionPEGI(ConditionLogic c, Values so) {
            pegi.toggleInt(ref c.compareValue);
        }

        public override bool resultsPEGI(Result r, Values so) {
            return pegi.toggleInt(ref r.updateValue);
        }

        public override bool editTrigger_And_Value_PEGI(int ind, TriggerGroup group, Values so) {
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

    */


}