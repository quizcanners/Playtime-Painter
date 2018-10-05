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

        public static TriggerUsage Get(int ind) {
            return usgs[ind];
        }

        public static List<string> names = new List<string>();
#if PEGI
        public static bool SelectUsage(ref int ind) => pegi.select(ref ind, usgs, 45);
        
        public bool Inspect(ValueIndex arg) => Inspect(arg.Trigger);
        
        public virtual void Inspect(ConditionLogic c) { }

        public abstract bool Inspect(Result r);// => false;
        
        public virtual bool Select(ref ResultType r, Dictionary<int, string> resultUsages) {
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
        
        public virtual bool Select(ref ConditionType c, Dictionary<int, string> conditionUsages)
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
        
        public virtual bool Inspect(Trigger t) {
            bool changed = false;
            string before = t.name;
            if (pegi.editDelayed(ref before, 150 - (HasMoreTriggerOptions() ? 30 : 0))) {
                Trigger.searchField = before;
                t.name = before;
                pegi.FocusControl("none");
                changed = true;
            }
            return changed;
        }

#endif

        public virtual bool HasMoreTriggerOptions() {
            return false;
        }
        
        public virtual bool UsingBool => false;
        

        public virtual bool UsingEnum() {
            return false;
        }

        public static Usage_Boolean boolean = new Usage_Boolean(0);
        public static Usage_Number number = new Usage_Number(1);
        public static Usage_StringEnum enumer = new Usage_StringEnum(2);
        public static Usage_BoolTag boolTag = new Usage_BoolTag(3);
        public static Usage_IntTag intTag = new Usage_IntTag(4);
        public static Usage_GameTimeStemp timestamp = new Usage_GameTimeStemp(5);
        public static Usage_RealTimestemp realTime = new Usage_RealTimestemp(6);
     //   public static Usage_Pointer pointer = new Usage_Pointer(7);

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
        public override void Inspect(ConditionLogic c) {
            if (!c.IsBoolean())
                ("Wrong Type: " + c.IsBoolean()).write();
             else 
                pegi.toggle(ref ((ConditionLogicBool)c).compareValue);
        }
        
        public override bool Inspect(Result r) {

            if (!r.IsBoolean())
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

        public override bool Inspect(Trigger t)
        {
            var vals = Values.global;
            
            bool changed = base.Inspect(t);
            changed |= vals.bools.Toogle(t); 

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
        public override void Inspect(ConditionLogic c) {

            var num = c as ConditionLogicInt;

            if (num == null)
                "Condition is not a number".write();
            else
            {
                Select(ref num.type, conditionUsages);

                pegi.edit(ref num.compareValue, 40);
            }
        }

        public override bool Inspect(Result r) {
            bool changed = false;

            changed |= Select(ref r.type, resultUsages);

            changed |= pegi.edit(ref r.updateValue, 40);
            return changed;
        }

        public override bool Inspect(Trigger t) {
            bool changed = base.Inspect(t);
            changed |= Values.global.ints.Edit(t);

            return changed;
        }
#endif

        public Usage_Number(int index) : base(index) { }
    }


    public class Usage_StringEnum : TriggerUsage
    {

        public override string ToString() { return string.Format("Enums"); }
#if PEGI

        public override void Inspect(ConditionLogic c) {


            var num = c as ConditionLogicInt;

            if (num != null)
            {
                Select(ref num.type, Usage_Number.conditionUsages);

                pegi.select(ref num.compareValue, num.Trigger.enm);
            }
            else
                "Incorrect type".write();
        }

        public override bool Inspect(Result r) {
            bool changed = false;

            changed |= Select(ref r.type, Usage_Number.resultUsages);
            
            pegi.select(ref r.updateValue, r.Trigger.enm);
            return changed;
        }

        public override bool Inspect(Trigger t) {
            
            bool changed = base.Inspect(t);
            
           changed |= Values.global.ints.Select(t).nl(); 

            if (Trigger.editedTrigger != t) return changed;

            "__ Enums__".write(60);
            changed |= t.enm.edit_PEGI();

            return changed;
        }

#endif

        public override bool HasMoreTriggerOptions()
        {
            return true;
        }

        public Usage_StringEnum(int index) : base(index) { }
    }

    
   /* public class Usage_Pointer : TriggerUsage {
      
        public override string ToString() { return string.Format("Pointer"); }

        public Usage_Pointer(int index) : base(index) { }
    }*/

    public class Usage_GameTimeStemp : TriggerUsage {
       
        public override string ToString() { return string.Format("Game Time"); }

        public static readonly Dictionary<int, string> conditionUsages = new Dictionary<int, string> {
            { ((int)ConditionType.VirtualTimePassedAbove), "Game_Time passed > " },
            { ((int)ConditionType.VirtualTimePassedBelow), "Game_Time passed < " },
        };

#if PEGI
        public override void Inspect(ConditionLogic c) {

            var num = c as ConditionLogicInt;

            if (num == null)
                "Condition is not a number".write();
            else
            {
                Select(ref num.type, conditionUsages);

                pegi.edit(ref num.compareValue, 40);
            }
        }

         public override bool Inspect(Result r) {
            bool changed = false;
            
            changed |= Select(ref r.type , resultUsages);

            if (r.type!= ResultType.SetTimeGame)
                changed |= pegi.edit(ref r.updateValue);

            return changed;
        }

#endif

        public static readonly Dictionary<int, string> resultUsages = new Dictionary<int, string> {
            {(int)ResultType.SetTimeGame, ResultType.SetTimeGame.GetText()},
            {(int)ResultType.Add, ResultType.Add.GetText()},
            {(int)ResultType.Subtract, ResultType.Subtract.GetText()},
            {(int)ResultType.Set, ResultType.Set.GetText()},
        };

       


        public Usage_GameTimeStemp(int index) : base(index) { }
    }

    public class Usage_RealTimestemp : TriggerUsage {

        public override string ToString() { return string.Format("Real Time"); }

        public static readonly Dictionary<int, string> conditionUsages = new Dictionary<int, string> {
            { ((int)ConditionType.RealTimePassedAbove), "Real_Time passed > " },
            { ((int)ConditionType.RealTimePassedBelow), "Real_Time passed < " },
        };

#if PEGI

        public override void Inspect(ConditionLogic c) {

            var num = c as ConditionLogicInt;

            if (num == null)
                "Condition is not a number".write();
            else
            {
                Select(ref num.type, conditionUsages);

                pegi.edit(ref num.compareValue, 40);
            }
        }

          public override bool Inspect(Result r) {
            bool changed = false;

            changed |= Select(ref r.type, resultUsages);

          
            if (r.type != ResultType.SetTimeReal)
                changed |= pegi.edit(ref r.updateValue);
            return changed;
        }

#endif
        public static readonly Dictionary<int, string> resultUsages = new Dictionary<int, string> {
            {(int)ResultType.SetTimeReal, ResultType.SetTimeReal.GetText()},
            {(int)ResultType.Add, ResultType.Add.GetText()},
            {(int)ResultType.Subtract, ResultType.Subtract.GetText()},
        };

      

        public Usage_RealTimestemp(int index) : base(index) { }
    }

    public class Usage_IntTag : TriggerUsage {

        public override string ToString() { return string.Format("TagGroup"); }

        #if PEGI

        public override bool Inspect(Trigger t) {
            var changed = base.Inspect(t);

            var vals = Values.global;

            int value = vals.GetTagEnum(t);//[group.GetIndex()][ind];
            if (pegi.select(ref value, t.enm)) {
                vals.SetTagEnum(t, value);
                changed = true;
            }


            pegi.newLine();

            if (Trigger.editedTrigger != t) return changed;

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

        public override void Inspect(ConditionLogic c) {

            var num = c as ConditionLogicInt;

            if (num == null)
                "Condition is not a number".write();
            else
            {
                Select(ref num.type, Usage_Number.conditionUsages);

                pegi.select(ref num.compareValue, num.Trigger.enm);
            }

         
        }

        public override bool Inspect(Result r) {
            bool changed = false;

            changed |= Select(ref r.type, Usage_Number.resultUsages);

            
            pegi.select(ref r.updateValue, r.Trigger.enm);
            return changed;
        }

#endif

        public override bool HasMoreTriggerOptions() => true;
        

        public Usage_IntTag(int index) : base(index) { }
    }

    public class Usage_BoolTag : TriggerUsage {
     
        public override string ToString() { return string.Format("Tag"); }


        #if PEGI

        public override void Inspect(ConditionLogic c) {

            var num = c as ConditionLogicBool;

            if (num == null)
                "Condition is not a bool".write();
            else
                pegi.toggle(ref num.compareValue);
        }

        public override bool Inspect(Result r) => pegi.toggleInt(ref r.updateValue);
        
        public override bool Inspect(Trigger t) {
            bool changed = base.Inspect(t);
            
                bool val = Values.global.GetTagBool(t);
                if (pegi.toggle(ref val)) {
                    changed = true; 
                    Values.global.SetTagBool(t, val);
                }
            

            return changed;
        }

        #endif

        public Usage_BoolTag(int index) : base(index) { }
    }

    


}