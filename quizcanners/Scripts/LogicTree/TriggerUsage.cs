using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace STD_Logic
{

    // Trigger usage is only used for PEGI. Logic engine will not need this to process triggers

    public abstract class TriggerUsage : IGotDisplayName  {
        private static readonly List<TriggerUsage> Usages = new List<TriggerUsage>();

        public static TriggerUsage Get(int ind) {
            return Usages[ind];
        }

        private static readonly List<string> Names = new List<string>();

        #region Inspector
        #if PEGI
        public static bool SelectUsage(ref int ind) => pegi.select(ref ind, Usages, 45);
        
        public bool Inspect(ValueIndex arg) => Inspect(arg.Trigger);
        
        public virtual void Inspect(ConditionLogic c) { }

        public abstract bool Inspect(Result r);

        public virtual bool Select(ref ResultType r, Dictionary<int, string> resultUsages) {
            bool changed = false;
            int t = (int)r;

            if (!resultUsages.ContainsKey(t)) {
                if (icon.Warning.Click("Is " + r.ToString() + ". Click to FIX ", ref changed))
                    r = (ResultType)(resultUsages.First().Key);
            }
            else
            {
                if (pegi.select(ref t, resultUsages, 40).changes(ref changed)) 
                    r = (ResultType)t;
            }
            return changed;
        }
        
        public virtual bool Select(ref ConditionType c, Dictionary<int, string> conditionUsages)
        {
            bool changed = false;
            int t = (int)c;

            if (!conditionUsages.ContainsKey(t))
            {
                if (icon.Warning.Click("Is {0}. FIC".F(c), ref changed))
                    c = (ConditionType)(conditionUsages.First().Key);
 
            }
            else if (pegi.select(ref t, conditionUsages, 40).changes(ref changed))
                    c = (ConditionType)t;
            
            return changed;
        }
        
        public virtual bool Inspect(Trigger t) {
            bool changed = false;
            string before = t.name;
            if (pegi.editDelayed(ref before, 150 - (HasMoreTriggerOptions ? 30 : 0)).changes(ref changed)) {
                Trigger.searchField = before;
                t.name = before;
                pegi.FocusControl("none");
            }
            return changed;
        }

        #endif
        #endregion

        public virtual bool HasMoreTriggerOptions => false;
        
        public virtual bool IsBoolean => false;

        public abstract string NameForDisplayPEGI { get;  }

        public virtual bool UsingEnum() {
            return false;
        }

        public static readonly Usage_Boolean Boolean = new Usage_Boolean(0);
        public static readonly Usage_Number Number = new Usage_Number(1);
        public static readonly Usage_StringEnum Enumeration = new Usage_StringEnum(2);
        public static readonly Usage_GameTimeStemp Timestamp = new Usage_GameTimeStemp(3);
        public static readonly Usage_RealTimestemp RealTime = new Usage_RealTimestemp(4);
        //  public static Usage_BoolTag boolTag = new Usage_BoolTag(5);
        //  public static Usage_IntTag intTag = new Usage_IntTag(6);
     //   public static Usage_Pointer pointer = new Usage_Pointer(7);

        public readonly int index;

        protected TriggerUsage(int ind) {
            index = ind;

            while (Names.Count <= ind) Names.Add("");
            while(Usages.Count <= ind) Usages.Add(null);

            Names[ind] = ToString();
            Usages[ind]= this;
        }
    }

    public class Usage_Boolean : TriggerUsage {

        public override string NameForDisplayPEGI => "YesNo";

        #region Inspector
#if PEGI
        public override void Inspect(ConditionLogic c) {
            if (!c.IsBoolean) {
                icon.Warning.write("Wrong Type: " + c.IsBoolean);

                
            }
            else
                pegi.toggleIcon(ref ((ConditionLogicBool)c).compareValue, "Condition Value");
        }
        
        public override bool Inspect(Result r) {

            if (!r.IsBoolean)
            {
                if (icon.Warning.Click("Wrong Type:" + r.type.ToString() + ". Change To Bool"))
                {
                    r.type = ResultType.SetBool;
                    return true;
                }
                return false;
            }

            return pegi.toggleIcon(ref r.updateValue);
        }

        public override bool Inspect(Trigger t)
        {
            var vals = Values.global;
            
            bool changed = base.Inspect(t);
            changed |= vals.booleans.Toogle(t); 

            return changed;
        }
#endif
        #endregion

        public override bool IsBoolean => true;

        public Usage_Boolean(int index) : base(index) { }
    }

    public class Usage_Number : TriggerUsage {

        public override string NameForDisplayPEGI => "Number";

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

        #region Inspector
#if PEGI
        public override void Inspect(ConditionLogic c) {

            var num = c as ConditionLogicInt;

            if (num == null)
                icon.Warning.write("Condition is not a number");
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
        #endregion

        public Usage_Number(int index) : base(index) { }
    }
    
    public class Usage_StringEnum : TriggerUsage
    {

        public override string NameForDisplayPEGI => "Enums";

        #region Inspector
#if PEGI
        public override void Inspect(ConditionLogic c) {


            var num = c as ConditionLogicInt;

            if (num != null)
            {
                Select(ref num.type, Usage_Number.conditionUsages);

                pegi.select(ref num.compareValue, num.Trigger.enm);
            }
            else
                icon.Warning.write("Incorrect type");
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

            if (Trigger.inspected != t) return changed;


            "__ Enums__".edit_Dictionary(ref t.enm).changes(ref changed);

            return changed;
        }
#endif
        #endregion

        public override bool HasMoreTriggerOptions => true;
        
        public Usage_StringEnum(int index) : base(index) { }
    }

    public class Usage_GameTimeStemp : TriggerUsage {

        public override string NameForDisplayPEGI => "Game Time";

        public static readonly Dictionary<int, string> conditionUsages = new Dictionary<int, string> {
            { ((int)ConditionType.VirtualTimePassedAbove), "Game_Time passed > " },
            { ((int)ConditionType.VirtualTimePassedBelow), "Game_Time passed < " },
        };

        #region Inspector
        #if PEGI
        public override void Inspect(ConditionLogic c) {

            var num = c as ConditionLogicInt;

            if (num == null)
                icon.Warning.write("Condition is not a number", 90); //.write();
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
        #endregion
        
        public static readonly Dictionary<int, string> resultUsages = new Dictionary<int, string> {
            {(int)ResultType.SetTimeGame, ResultType.SetTimeGame.GetText()},
            {(int)ResultType.Add, ResultType.Add.GetText()},
            {(int)ResultType.Subtract, ResultType.Subtract.GetText()},
            {(int)ResultType.Set, ResultType.Set.GetText()},
        };

        public Usage_GameTimeStemp(int index) : base(index) { }
    }

    public class Usage_RealTimestemp : TriggerUsage {

        public override string NameForDisplayPEGI => "Real Time";

        public static readonly Dictionary<int, string> conditionUsages = new Dictionary<int, string> {
            { ((int)ConditionType.RealTimePassedAbove), "Real_Time passed > " },
            { ((int)ConditionType.RealTimePassedBelow), "Real_Time passed < " },
        };

        #region Inspector
#if PEGI
        public override void Inspect(ConditionLogic c) {

            var num = c as ConditionLogicInt;

            if (num == null)
                icon.Warning.write("Condition is not a number", 90);
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
        #endregion

        public static readonly Dictionary<int, string> resultUsages = new Dictionary<int, string> {
            {(int)ResultType.SetTimeReal, ResultType.SetTimeReal.GetText()},
            {(int)ResultType.Add, ResultType.Add.GetText()},
            {(int)ResultType.Subtract, ResultType.Subtract.GetText()},
        };

      

        public Usage_RealTimestemp(int index) : base(index) { }
    }

    /*
    public class Usage_IntTag : TriggerUsage {

        public override string NameForDisplayPEGI => "TagGroup";

        #region Inspector
        #if PEGI
        public override bool Inspect(Trigger t) {
            var changed = base.Inspect(t);

            int value = Values.global.GetTagEnum(t);
            if (pegi.select(ref value, t.enm).nl(ref changed)) 
                Values.global.SetTagEnum(t, value);
             
            if (Trigger.inspected != t) return changed;

           // "__ Tags __".nl();

            const string NoZerosForTrigs = "No04t";
            "Can't use 0 as tag index. ".writeOneTimeHint(NoZerosForTrigs);

            if ("___Tags___".edit_Dictionary(ref t.enm).changes(ref changed)) {
                string dummy;
                if (t.enm.TryGetValue(0, out dummy)) {
                    t.enm.Remove(0);
                    pegi.resetOneTimeHint(NoZerosForTrigs);
                }
            }

            return changed;
        }

        public override void Inspect(ConditionLogic c) {

            var num = c as ConditionLogicInt;

            if (num == null)
                icon.Warning.write("Condition is not a number", 60); 
            else {
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
        #endregion

        public override bool HasMoreTriggerOptions => true;
        
        public Usage_IntTag(int index) : base(index) { }
    }

    public class Usage_BoolTag : TriggerUsage {

        public override string NameForDisplayPEGI => "Tag";

        #region Inspector
        #if PEGI
        public override void Inspect(ConditionLogic c) {

            var num = c as ConditionLogicBool;

            if (num == null)
                icon.Warning.write("Condition is not a bool",60);
            else
                pegi.toggleIcon(ref num.compareValue);
        }

        public override bool Inspect(Result r) => pegi.toggleIcon(ref r.updateValue);
        
        public override bool Inspect(Trigger t) {
            bool changed = base.Inspect(t);
            
                bool val = Values.global.GetTagBool(t);
                if (pegi.toggleIcon(ref val).changes(ref changed)) 
                    Values.global.SetTagBool(t, val);

            return changed;
        }
#endif
        #endregion
        
        public override bool IsBoolean => true;

        public Usage_BoolTag(int index) : base(index) { }
    }

    */


}