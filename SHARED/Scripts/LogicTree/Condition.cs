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
    public class ConditionLogic : ValueIndex, ISTD , IPEGI, IPEGI_ListInspect  {

        #region Encode & Decode
        public override StdEncoder Encode() => new StdEncoder().Add("ind", EncodeIndex());

        public override bool Decode(string subtag, string data) => true;
        #endregion

        public virtual bool TryForceConditionValue(Values values, bool toTrue) => false;

        public virtual bool TestFor(Values values) => false;

        public virtual int IsItClaimable( int dir, Values values) => -2;
        
        public override bool IsBoolean => false;

        #region Inspector
        #if PEGI

        public override bool SearchTriggerSameType => false;

        public static Values inspectedTarget = null;

        public override bool PEGI_inList_Sub(IList list, int ind, ref int inspecte)
        {
            var changed = FocusedField_PEGI(ind, "Cond");

            Trigger._usage.Inspect(this);

            return changed;
        }

        #endif
        #endregion

        public ConditionLogic()
        {
            if (TriggerGroup.Browsed != null)
                groupIndex = TriggerGroup.Browsed.IndexForPEGI;
        }

    }

    public class ConditionLogicBool : ConditionLogic {

        public bool compareValue;

        #region Encode & Decode
        public override StdEncoder Encode() => new StdEncoder()
            .Add_IfTrue("b", compareValue)
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
        #endregion

        public override bool TryForceConditionValue(Values values, bool toTrue)
        {
            SetBool(values, toTrue ? compareValue : !compareValue);
            LogicMGMT.currentLogicVersion++;
            return true;
        }

        public override bool SearchTriggerSameType => true;

        public override bool TestFor(Values values) => GetBool(values) == compareValue;

        public override int IsItClaimable(int dir, Values st) => (dir > 0) == (compareValue) ?  1 : -2;
        
        public override bool IsBoolean => true;

    }

    public class ConditionLogicInt : ConditionLogic {
        public ConditionType type;
        public int compareValue;

        #region Encode & Decode
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
        #endregion

        public override bool SearchTriggerSameType => true;

        public override bool TryForceConditionValue(Values value,bool toTrue) {

            if (TestFor(value) == toTrue)
                return true;
            
            if (toTrue) {
                switch (type) { 
                    case ConditionType.Above: SetInt(value, compareValue + 1); break;
                    case ConditionType.Below: SetInt(value, compareValue - 1); break;
                    case ConditionType.Equals: SetInt(value, compareValue); break;
                    case ConditionType.NotEquals: if (GetInt(value) == compareValue) value.ints[groupIndex].Add(triggerIndex, 1); break;
                    case ConditionType.RealTimePassedAbove: SetInt(value, (int)LogicMGMT.RealTimeNow() - compareValue - 1); break;
                    case ConditionType.RealTimePassedBelow: SetInt(value, (int)LogicMGMT.RealTimeNow()); break;
                    case ConditionType.VirtualTimePassedAbove: SetInt(value, (int)Time.time - compareValue - 1); break;
                    case ConditionType.VirtualTimePassedBelow: SetInt(value, (int)Time.time); break;
                }
            } else {
                switch (type) {
                    case ConditionType.Above: SetInt(value, compareValue - 1); break;
                    case ConditionType.Below: SetInt(value, compareValue + 1); break;
                    case ConditionType.Equals: SetInt(value, compareValue + 1); break;
                    case ConditionType.NotEquals: SetInt(value, compareValue); break;
                    case ConditionType.RealTimePassedAbove: SetInt(value, (int)LogicMGMT.RealTimeNow() ); break;
                    case ConditionType.RealTimePassedBelow: SetInt(value, (int)LogicMGMT.RealTimeNow() - compareValue - 1); break;
                    case ConditionType.VirtualTimePassedAbove: SetInt(value, (int)Time.time ); break;
                    case ConditionType.VirtualTimePassedBelow: SetInt(value, (int)Time.time - compareValue - 1); break;
                }
            }

            LogicMGMT.currentLogicVersion++;

            return true;
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

        public override int IsItClaimable(int dir, Values st) {
            switch (type) {
                case ConditionType.Above: if ((GetInt(st) < compareValue) && (dir > 0)) return (compareValue - GetInt(st) + 1); break;
                case ConditionType.Below: if ((GetInt(st) > compareValue) && (dir < 0)) return (GetInt(st) - compareValue + 1); break;
                case ConditionType.Equals: if ((GetInt(st) > compareValue) == (dir < 0)) return Mathf.Abs(GetInt(st) - compareValue); break;
            }

            return -2;
        }
    }
    
    public class TestOnceCondition : ConditionLogicBool
    {

        #region Inspector
        #if PEGI
        public override bool PEGI_inList(IList list, int ind, ref int edited)
        {
            bool changed = FocusedField_PEGI(ind, "Cond");

            Trigger._usage.Inspect(this);

            changed |= SearchAndAdd_PEGI(0);

            return base.Inspect() || changed;
        }
        #endif
        #endregion

        public override bool TestFor(Values st)
        {
            bool value = base.TestFor(st);

            if (value)
                ResultType.SetBool.Apply(compareValue ? 0 : 1, this, st);

            return value;
        }

    }



}