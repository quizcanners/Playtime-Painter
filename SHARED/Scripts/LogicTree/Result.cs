using System.Collections.Generic;
using System;
using UnityEngine;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace STD_Logic  {

    public enum ResultType { SetBool, Set, Add, Subtract, SetTimeReal, SetTimeGame, SetTagBool, SetTagInt }
    
    public static class ResultExtensionFunctions {

        public static void Apply(this ResultType type, int updateValue, ValueIndex dest, Values so)
        {
            switch (type)
            {
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

            switch (type)
            {
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

        public static string ToStringSafe(this List<Result> o, bool showDetail) {
            bool AnyFinals = ((o != null) && (o.Count > 0));
            return (AnyFinals ? "[" + o.Count + "]: " +
                 (showDetail ? "..." : o[0].ToString()) : " NONE");
        }

        public static void Apply(this List<Result> results, Values to) {

            if (results.Count > 0)
            {
                foreach (var r in results)
                    r.Apply(to);

                LogicMGMT.AddLogicVersion();
            }

        }

#if !NO_PEGI
        public static bool Inspect(this string label, ref List<Result> res, Values vals)
        {
            pegi.write(label);
            return res.Inspect(vals);
        }

        public static bool Inspect(this List<Result> res, Values vals) {
            bool changed = false;

            Values.current = vals;

            if (icon.Add.Click(25))
                res.Add();
            
            pegi.newLine();

            int DeleteNo = -1;

            for (int i = 0; i < res.Count; i++)
            {
                var r = res[i];

                if (icon.Delete.Click())
                    DeleteNo = i;

                changed |= r.FocusedField_PEGI(i, "Res");

                changed |= r.Trigger._usage.Inspect(r);

                changed |= r.SearchAndAdd_PEGI(i);

            }
            
            if (DeleteNo != -1)  
                res.RemoveAt(DeleteNo);
            pegi.newLine();

            Values.current = null;
            return changed;
        }
#endif

        public static Result Add(this List<Result> lst) {
            Result r = new Result();

            if (lst.Count > 0) {
                Result prev = lst.Last();
                
                r.groupIndex = prev.groupIndex;
                r.triggerIndex = prev.triggerIndex;



                // Making sure new trigger will not be a duplicate (a small quality of life improvement)
                /*
                List<int> indxs;
                r.Group.triggers.GetAllObjs(out indxs);

                foreach (Result res in lst)
                    if (res.groupIndex == r.groupIndex)
                        indxs.Remove(res.triggerIndex);

                if (indxs.Count > 0)
                    r.triggerIndex = indxs[0];
                    */

            }

            lst.Add(r);

            return r;
        }

    }
    
    public class Result : ValueIndex, IPEGI, IGotDisplayName
    {

       // public TaggedTarget targ;
        public ResultType type;
        public int updateValue;
        
        public override bool Decode(string subtag, string data) {
            switch (subtag) {
                case "ty": type = (ResultType)data.ToInt(); break;
                case "val": updateValue = data.ToInt(); break;
                case "ind": data.DecodeInto(DecodeIndex); break;
                default: return false;
            }
            return true;
        }
        
        public override StdEncoder Encode()
        {
            StdEncoder cody = new StdEncoder();
            cody.Add_ifNotZero("ty", (int)type);
            cody.Add_ifNotZero("val", updateValue);
            cody.Add("ind", EncodeIndex);
            return cody;
        }
        
#if !NO_PEGI
        public override string NameForPEGIdisplay() => base.NameForPEGIdisplay() + type + " " + updateValue;
#endif
        public static string CompileResultText(Result res) => res.Trigger.name + res.type + " " + res.updateValue;
        
        public static int exploredResult = -1;
        
        public void Apply(Values to) => type.Apply(updateValue, this, to);
           
        public override bool IsBoolean() => ((type == ResultType.SetBool) || (type == ResultType.SetTagBool));
        
        public Result()
        {
            if (TriggerGroup.Browsed != null)
                groupIndex = TriggerGroup.Browsed.IndexForPEGI;
        }
        
    }

}