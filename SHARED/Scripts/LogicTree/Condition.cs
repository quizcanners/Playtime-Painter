using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace STD_Logic
{
    public enum ConditionType { Bool , Above, Below, Equals, RealTimePassedAbove, RealTimePassedBelow, VirtualTimePassedAbove, VirtualTimePassedBelow, NotEquals }


    public class ConditionLogic : ValueIndex , iSTD
#if PEGI
        , iPEGI
#endif
    {

        public static string tag = "cond";

        public ConditionType type;
        public int compareValue;

        public string getDefaultTagName() {
            return tag;
        }

        public virtual stdEncoder Encode() {
            var cody = new stdEncoder();

            cody.Add_ifNotZero("v", compareValue);
            cody.Add_ifNotZero("ty",(int)type);
            cody.Add("g", groupIndex);
            cody.Add("t", triggerIndex);

            return cody;
        }

        public virtual bool Decode(string subtag, string data) {
            switch (subtag) {
                case "v": compareValue = data.ToInt(); break;
                case "ty": type = (ConditionType)data.ToInt(); break;
                case "g": groupIndex = data.ToInt(); break;
                case "t": triggerIndex = data.ToInt(); break;
                default: return false;
            }
            return true;
        }

        public iSTD Decode(string data) {
            new stdDecoder(data).DecodeTagsFor(this);
            return this;
        }

        public void ForceConditionTrue(Values st) {

            switch (type) {
                case ConditionType.Bool:  SetBool(st,compareValue > 0); break;
                case ConditionType.Above: SetInt(st, compareValue + 1); break;
                case ConditionType.Below: SetInt(st, compareValue - 1); break;
                case ConditionType.Equals: SetInt(st, compareValue); break;
                case ConditionType.NotEquals: if (GetInt(st) == compareValue) st.ints[groupIndex].Add(triggerIndex, 1); break;
            }
        }

        public virtual bool TestFor(Values st) {

            int timeGap;

            switch (type) {
                case ConditionType.Bool: if (GetBool(st) == ((compareValue > 0) ? true : false)) return true; break;
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
            //Debug.Log ("No pass on: " + glob.triggers.triggers [cond.TriggerNo].name+ " with "+cond.Type);
            return false;
        }

        public int isItClaimable( int dir, Values st) {

            switch (type) {
                case ConditionType.Bool: if ((dir > 0) == (compareValue > 0)) return 1; break;
                case ConditionType.Above: if ((GetInt(st) < compareValue) && (dir > 0)) return (compareValue - GetInt(st) + 1); break;
                case ConditionType.Below: if ((GetInt(st) > compareValue) && (dir < 0)) return (GetInt(st) - compareValue + 1); break;
                case ConditionType.Equals: if ((GetInt(st) > compareValue) == (dir < 0)) return Mathf.Abs(GetInt(st) - compareValue); break;
            }
            return -2;
        }

        public ConditionLogic() {
            groupIndex = TriggerGroup.browsed.GetHashCode();
        }

        public override bool isBoolean(){
            return type == (int)ConditionType.Bool;
        }

        public static bool unfoldPegi;
      
        public override string ToString()
        {
            return (trig.name) + " " + type + " " + (isBoolean() ?
                                            (compareValue == 1 ? "True" : "false")
                                            : compareValue.ToString());
        }
        
    }
#if PEGI
    public static class ConditionLogicExtensions
    {

        public static bool edit(this string labes, ConditionBranch web , Values so)
        {
            pegi.write(labes);
            return web.PEGI(so);

        }

        public static bool edit(this string labes, ref List<ConditionLogic> list, Values so)
        {
            pegi.write(labes);
            return list.PEGI(so);

        }

            public static bool PEGI(this List<ConditionLogic> list, Values so)
        {
            bool changed = false;

            if (icon.Add.ClickUnfocus().nl())
            {
                changed = true;
                list.Add(new ConditionLogic());
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (icon.Delete.ClickUnfocus(25))
                {
                    list.RemoveAt(i);
                    changed = true;
                    i--;
                }
                else
                {
                    var el = list[i];

                    changed |= el.PEGI(i, so, "Cond");

                    el.trig._usage.conditionPEGI(el, so);

                    changed |= el.searchAndAdd_PEGI(i);

                }

                pegi.newLine();
            }
            
            pegi.newLine();

            return changed;
        }
        
    }
#endif


}