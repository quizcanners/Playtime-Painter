using System.Collections.Generic;
using System;
using UnityEngine;
using PlayerAndEditorGUI;
using SharedTools_Stuff;
using System.Collections;

namespace STD_Logic  {

    public enum ResultType { SetBool, Set, Add, Subtract, SetTimeReal, SetTimeGame, SetTagBool, SetTagInt }
    
    public static class ResultExtensionFunctions {

        public static void Apply(this ResultType type, int updateValue, ValueIndex dest, Values so) {
            switch (type)  {
                case ResultType.SetBool: dest.SetBool(so, (updateValue > 0)); break;
                case ResultType.Set: dest.SetInt(so, updateValue); break;
                case ResultType.Add: so.ints[dest.groupIndex].Add(dest.triggerIndex, updateValue); break;
                case ResultType.Subtract: so.ints[dest.groupIndex].Add(dest.triggerIndex, -updateValue); break;
                case ResultType.SetTimeReal: dest.SetInt(so, LogicMGMT.RealTimeNow()); break;
                case ResultType.SetTimeGame: dest.SetInt(so, (int)Time.time); break;
                case ResultType.SetTagBool: so.SetTagBool(dest.groupIndex, dest.triggerIndex, updateValue > 0); break;
                case ResultType.SetTagInt: so.SetTagEnum(dest.groupIndex, dest.triggerIndex, updateValue); break;
            }
        }

        public static string GetText(this ResultType type) {
            switch (type) {
                case ResultType.SetBool: return "Set";
                case ResultType.Set: return "=";
                case ResultType.Add: return "+";
                case ResultType.Subtract: return "-";
                case ResultType.SetTimeReal: return "Set To Real_Time_Now";
                case ResultType.SetTimeGame: return "Set To Game_Now_Time";
                case ResultType.SetTagBool: return "Set Bool Tag";
                case ResultType.SetTagInt: return "Set Int Tag";
                default: return type.ToString();
            }

        }
        
        public static void Apply(this List<Result> results) => results.Apply(Values.global);

        public static void Apply(this List<Result> results, Values to) {

            if (results.Count > 0)
            {
                foreach (var r in results)
                    r.Apply(to);

                LogicMGMT.AddLogicVersion();
            }

        }
    }
    
    public class Result : ValueIndex, IGotDisplayName, IPEGI_ListInspect {
        
        public ResultType type;
        public int updateValue;

        #region Encode & Decode
        public override bool Decode(string subtag, string data) {
            switch (subtag) {
                case "ty": type = (ResultType)data.ToInt(); break;
                case "val": updateValue = data.ToInt(); break;
                case "ind": data.Decode_Delegate(DecodeIndex); break;
                default: return false;
            }
            return true;
        }
        
        public override StdEncoder Encode()
        {
            StdEncoder cody = new StdEncoder();
            cody.Add_IfNotZero("ty", (int)type);
            cody.Add_IfNotZero("val", updateValue);
            cody.Add("ind", EncodeIndex);
            return cody;
        }
        #endregion

        public static string CompileResultText(Result res) => res.Trigger.name + res.type.GetText() + " " + res.updateValue;
        
        public void Apply(Values to) => type.Apply(updateValue, this, to);
           
        public override bool IsBoolean => ((type == ResultType.SetBool) || (type == ResultType.SetTagBool));
        
        public Result()  {
            if (TriggerGroup.Browsed != null)
                groupIndex = TriggerGroup.Browsed.IndexForPEGI;
        }
        
        #region Inspector
        #if PEGI
        public override string NameForPEGIdisplay => base.NameForPEGIdisplay + type + " " + updateValue;

        public override bool PEGI_inList_Sub(IList list, int ind, ref int inspecte) {

            var changed = FocusedField_PEGI(ind, "Res");
            changed |= Trigger._usage.Inspect(this);

            return changed;
        }
    
        #endif
        #endregion

    }

}