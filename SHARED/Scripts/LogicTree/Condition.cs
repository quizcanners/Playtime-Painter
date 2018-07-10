using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace STD_Logic
{
    public enum ConditionType {Above, Below, Equals, RealTimePassedAbove, RealTimePassedBelow, VirtualTimePassedAbove, VirtualTimePassedBelow, NotEquals }

    [DerrivedList(typeof(ConditionLogicBool), typeof(ConditionLogicInt), typeof(TestOnceCondition))]
    public class ConditionLogic : ValueIndex, ISTD
#if PEGI
        , IPEGI, IPEGI_ListInspect 
#endif
    {

        public override StdEncoder Encode() => new StdEncoder().Add("ind", EncodeIndex());

        public override bool Decode(string subtag, string data) => true;
        
        public virtual void ForceConditionTrue(Values st) { }

        public virtual bool TestFor(Values st) => false;

        public virtual int IsItClaimable( int dir, Values st) => -2;
        
        public override bool IsBoolean() => false;

#if PEGI
        
        public virtual bool PEGI_inList(IList list, int ind, ref int edited) {

            bool changed = PEGI(ind, "Cond");

            Trigger._usage.inspect(this);

            changed |= SearchAndAdd_PEGI(ind);

            return changed;
        }

#endif

        public static bool unfoldPegi;

        public ConditionLogic()
        {
            groupIndex = TriggerGroup.Browsed.IndexForPEGI;
        }

    }

    public class ConditionLogicBool : ConditionLogic
    {
        public bool compareValue;

        public override StdEncoder Encode() => new StdEncoder()
            .Add_ifTrue("b", compareValue)
            .Add("ind", EncodeIndex());

        public override bool Decode(string subtag, string data)
        {
            switch (subtag)
            {
                case "b": compareValue = data.ToBool(); break;
                case "ind": data.DecodeInto(DecodeIndex); break;
                default: return false;
            }
            return true;
        }
        
        public override void ForceConditionTrue(Values st) =>  SetBool(st, compareValue);
        
        public override bool TestFor(Values st) => GetBool(st) == compareValue;

        public override int IsItClaimable(int dir, Values st) => (dir > 0) == (compareValue) ?  1 : -2;
        
        public override bool IsBoolean() => true;
    }

    public class ConditionLogicInt : ConditionLogic
    {
        public ConditionType type;
        public int compareValue;

        public override StdEncoder Encode() => new StdEncoder()
            .Add_ifNotZero("v", compareValue)
            .Add_ifNotZero("ty", (int)type)
            .Add("ind", EncodeIndex);

        public override bool Decode(string subtag, string data)
        {
            switch (subtag)
            {
                case "v": compareValue = data.ToInt(); break;
                case "ty": type = (ConditionType)data.ToInt(); break;
                case "ind": data.DecodeInto(DecodeIndex); break;
                default: return false;
            }
            return true;
        }
        
        public override void ForceConditionTrue(Values st)
        {

            switch (type)
            {
                case ConditionType.Above: SetInt(st, compareValue + 1); break;
                case ConditionType.Below: SetInt(st, compareValue - 1); break;
                case ConditionType.Equals: SetInt(st, compareValue); break;
                case ConditionType.NotEquals: if (GetInt(st) == compareValue) st.ints[groupIndex].Add(triggerIndex, 1); break;
            }
        }

        public override bool TestFor(Values st)
        {
            int timeGap;

            switch (type)
            {
                case ConditionType.Above: if (GetInt(st) > compareValue) return true; break;
                case ConditionType.Below: if (GetInt(st) < compareValue) return true; break;
                case ConditionType.Equals: if (GetInt(st) == compareValue) return true; break;
                case ConditionType.NotEquals: if (GetInt(st) != compareValue) return true; break;
                case ConditionType.VirtualTimePassedAbove:
                    timeGap = (int)Time.time - GetInt(st);
                    if (timeGap > compareValue) return true; LogicMGMT.inst.AddTimeListener(compareValue - timeGap); break;
                case ConditionType.VirtualTimePassedBelow:
                    timeGap = (int)Time.time - GetInt(st);
                    if (timeGap < compareValue) { LogicMGMT.inst.AddTimeListener(compareValue - timeGap); return true; }
                    break;
                case ConditionType.RealTimePassedAbove:
                    timeGap = (LogicMGMT.RealTimeNow() - GetInt(st));
                    if (timeGap > compareValue) return true; LogicMGMT.inst.AddTimeListener(compareValue - timeGap); break;
                case ConditionType.RealTimePassedBelow:
                    timeGap = (LogicMGMT.RealTimeNow() - GetInt(st));
                    if (timeGap < compareValue) { LogicMGMT.inst.AddTimeListener(compareValue - timeGap); return true; }
                    break;

            }
            return false;
        }

        public override int IsItClaimable(int dir, Values st)
        {
            switch (type)
            {
                case ConditionType.Above: if ((GetInt(st) < compareValue) && (dir > 0)) return (compareValue - GetInt(st) + 1); break;
                case ConditionType.Below: if ((GetInt(st) > compareValue) && (dir < 0)) return (GetInt(st) - compareValue + 1); break;
                case ConditionType.Equals: if ((GetInt(st) > compareValue) == (dir < 0)) return Mathf.Abs(GetInt(st) - compareValue); break;
            }
            return -2;
        }
    }


    public class TestOnceCondition : ConditionLogicBool
    {
        
#if PEGI
        public override bool PEGI_inList(IList list, int ind, ref int edited)
        {
            bool changed = PEGI(0, "Cond");

            Trigger._usage.inspect(this);

            changed |= SearchAndAdd_PEGI(0);

            return base.PEGI() || changed;
        }
#endif

        public override bool TestFor(Values st)
        {
            bool value = base.TestFor(st);

            if (value)
                ResultType.SetBool.Apply(compareValue ? 0 : 1, this, st);

            return value;
        }

    }



}