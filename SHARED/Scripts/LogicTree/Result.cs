using System.Collections.Generic;
using System;
using UnityEngine;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace STD_Logic  {

    public enum ResultType { SetBool, Set, Add, Subtract, SetTimeReal, SetTimeGame, SetTagBool, SetTagInt }
    
    public static class ResultExtensionFunctions {

        public static void apply(this ResultType type, int updateValue, ValueIndex dest, Values so)
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

        public static void apply(this List<Result> results, Values to) {

            if (results.Count > 0)
            {
                foreach (var r in results)
                    r.apply(to);

                LogicMGMT.AddLogicVersion();
            }

        }
#if PEGI
        public static bool edit(this string label, ref List<Result> res, Values vals)
        {
            pegi.write(label);
            return res.PEGI(vals);
        }

        public static bool PEGI(this List<Result> res, Values vals) {
            bool changed = false;
            if (icon.Add.Click(25))
                res.Add();
            
            pegi.newLine();

            int DeleteNo = -1;

            for (int i = 0; i < res.Count; i++)
            {
                var r = res[i];

                if (icon.Delete.Click())
                    DeleteNo = i;

                changed |= r.PEGI(i, vals, "Res");

                changed |= r.trig._usage.resultsPEGI(r, vals);

                changed |= r.searchAndAdd_PEGI(i);

            }
            
            if (DeleteNo != -1)  
                res.RemoveAt(DeleteNo);
            pegi.newLine();

            return changed;
        }

#endif

        public static Result Add(this List<Result> lst) {
            Result r = new Result();

            if (lst.Count > 0) {
                Result prev = lst.last();

                r.groupIndex = prev.groupIndex;
                r.triggerIndex = prev.triggerIndex;

                // Making sure new trigger will not be a duplicate (a small quality of life improvement)

                List<int> indxs;
                r.group.triggers.GetAllObjs(out indxs);

                foreach (Result res in lst)
                    if (res.groupIndex == r.groupIndex)
                        indxs.Remove(res.triggerIndex);

                if (indxs.Count > 0)
                    r.triggerIndex = indxs[0];

            }

            lst.Add(r);

            return r;
        }

    }
    
    public class Result : ValueIndex, iSTD {

        public TaggedTarget targ;

        public bool Decode(string subtag, string data) {
            switch (subtag) {
                case "ty": type = (ResultType)data.ToInt(); break;
                case "val": updateValue = data.ToInt(); break;
                case "g": groupIndex = data.ToInt(); break;
                case "t": triggerIndex = data.ToInt(); break;
                case TaggedTarget.stdTag_TagTar: data.DecodeInto(out targ); break;
                default: return false;
            }
            return true;
        }
        
        public const string storyTag_res = "res";

        public string getDefaultTagName() {
            return storyTag_res;
        }

        public override string ToString()
        {
			Trigger t = trig;
            return   t == null ? "???" : t.name + type + " " + updateValue;
        }

        public Result() {
            groupIndex = TriggerGroup.browsed.GetHashCode();
        }

        public iSTD Decode(string data) {
            new stdDecoder(data).DecodeTagsFor(this);
            return this;
        }

        public static string CompileResultText(Result res) {
            return res.trig.name + res.type + " " + res.updateValue;
        }
        
        public static int exploredResult = -1;

        public ResultType type;
        public int updateValue;

        public stdEncoder Encode()
        {
            stdEncoder cody = new stdEncoder();

            cody.Add_ifNotZero("ty", (int)type);
            cody.Add_ifNotZero("val", updateValue);
            cody.Add("g", groupIndex);
            cody.Add("t", triggerIndex);
            return cody;
        }

        public bool apply(Values so) {

            type.apply(updateValue, this, so);

          /*  switch (type)  {
                case ResultType.SetBool: SetBool(so, (updateValue > 0)); break;
                case ResultType.Set: SetInt(so, updateValue); break;
                case ResultType.Add: so.ints[groupIndex].Add(triggerIndex, updateValue); break;
                case ResultType.Subtract: so.ints[groupIndex].Add(triggerIndex, -updateValue); break;
                case ResultType.SetTimeReal: SetInt(so, LogicMGMT.RealTimeNow()); break;
                case ResultType.SetTimeGame: SetInt(so, (int)Time.time); break;
                case ResultType.SetTagBool: so.SetTagBool(groupIndex, triggerIndex, updateValue > 0); break;
                case ResultType.SetTagInt: so.SetTagEnum(groupIndex, triggerIndex, updateValue); break;
            }*/
            return true;
        }

       

        public override bool isBoolean()
        {
            return ((type == ResultType.SetBool) || (type == ResultType.SetTagBool));
        }

       

    }

}