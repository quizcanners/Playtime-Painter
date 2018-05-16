using System.Collections.Generic;
using System;
using UnityEngine;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace LogicTree
{

    public enum ResultType { SetBool, Set, Add, Subtract, SetTimeReal, SetTimeGame, SetTagBool, SetTagInt }


    public static class ResultExtensionFunctions {
        public static string ToStringSafe(this List<TargetedResult> o, bool showDetail) {
            bool AnyFinals = ((o != null) && (o.Count > 0));
            return (AnyFinals ? "[" + o.Count + "]: " +
                 (showDetail ? "..." : o[0].ToString()) : " NONE");

        }

        public static void apply(this List<TargetedResult> results, Values to) {

            if (results.Count > 0) {
                for (int i = 0; i < results.Count; i++)
                    results[i].apply(to);

                LogicMGMT.AddQuestVersion();
            }
        }

        public static void PEGI(this List<TargetedResult> res, Values tell) {

            if (icon.Add.Click(25))
                res.Add();
            
            pegi.newLine();

            int DeleteNo = -1;

            for (int i = 0; i < res.Count; i++) 
                res[i].PEGI(i, ref DeleteNo,  tell);

          

            if (DeleteNo != -1)  
                res.RemoveAt(DeleteNo);
            pegi.newLine();

           
        }


        public static TargetedResult Add(this List<TargetedResult> lst) {
            TargetedResult r = new TargetedResult();

            if (lst.Count > 0) {
                TargetedResult prev = lst.last();

                r.groupIndex = prev.groupIndex;
                r.triggerIndex = prev.triggerIndex;

                // Making sure new trigger will not be a duplicate (a small quality of life improvement)

                List<int> indxs;
                r.group.triggers.GetAllObjs(out indxs);

                foreach (TargetedResult res in lst)
                    if (res.groupIndex == r.groupIndex)
                        indxs.Remove(res.triggerIndex);

                if (indxs.Count > 0)
                    r.triggerIndex = indxs[0];

            }

            lst.Add(r);

            return r;
        }

    }
    
    public class TargetedResult : ValueIndex, iSTD {

        public TaggedTarget targ;

        public bool Decode(string subtag, string data) {
            switch (subtag) {
                case "ty": _type = data.ToInt(); break;
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
            return   t == null ? "???" : t.name + _type + " " + updateValue;
        }

        public TargetedResult() {
            groupIndex = TriggerGroups.browsed.GetHashCode();
        }

        public iSTD Decode(string data) {
            new stdDecoder(data).DecodeTagsFor(this);
            return this;
        }

        public static string CompileResultText(TargetedResult res) {
            return res.trig.name + res._type + " " + res.updateValue;
        }

        public static bool showFinal;
        public static bool showOnExit;
        public static bool showOnEnter;
        public static int exploredResult = -1;

        public int _type;
        public int updateValue;

        public ResultType type { get { return (ResultType)_type; } set { _type = (int)value; } }

        public stdEncoder Encode()
        {
            stdEncoder cody = new stdEncoder();

            cody.AddIfNotZero("ty", _type);
            cody.AddIfNotZero("val", updateValue);
            cody.Add("g", groupIndex);
            cody.Add("t", triggerIndex);
            return cody;
        }

        public void apply(Values so)
        {

            switch ((ResultType)_type)
            {
                case ResultType.SetBool: SetBool(so, (updateValue > 0)); break;
                case ResultType.Set: SetInt(so, updateValue); break;
                case ResultType.Add: so.ints[groupIndex].Add(triggerIndex, updateValue); break;
                case ResultType.Subtract: so.ints[groupIndex].Add(triggerIndex, -updateValue); break;
                case ResultType.SetTimeReal: SetInt(so, LogicMGMT.RealTimeNow()); break;
                case ResultType.SetTimeGame: SetInt(so, (int)Time.time); break;
                case ResultType.SetTagBool: so.SetTagBool(groupIndex, triggerIndex, updateValue > 0); break;
                case ResultType.SetTagInt: so.SetTagEnum(groupIndex, triggerIndex, updateValue); break;
            }
        }

        public override bool isBoolean()
        {
            return ((type == ResultType.SetBool) || (type == ResultType.SetTagBool));
        }

        public bool PEGI(int index, ref int DeleteNo, Values so) {
           
            bool changed = false;

            changed |= base.PEGI(index, ref DeleteNo, so, "Res");

            changed |= trig._usage.resultsPEGI(this, so);

            changed |= base.searchAndAdd_PEGI(index);

            return changed;
        }
    }

}