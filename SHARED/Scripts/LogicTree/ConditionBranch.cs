using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using SharedTools_Stuff;
using PlayerAndEditorGUI;

namespace STD_Logic {
    public class ConditionBranch : AbstractKeepUnrecognized_STD, IGotName,
        IPEGI, IAmConditional, ICanBeDefault_STD, IPEGI_ListInspect, IGotCount {

        public enum ConditionBranchType { OR, AND }

        public List<ConditionLogic> conds = new List<ConditionLogic>();
        public List<ConditionBranch> branches = new List<ConditionBranch>();

        public ConditionBranchType type = ConditionBranchType.AND;
        public string description = "new branch";
        public TaggedTarget targ;

        Values TargetValues => targ.TryGetValues(Values.global);

        public int CountForInspector => CountRecursive();
        
        public string NameForPEGI
        {
            get
            {
                return description;
            }

            set
            {
                description = value;
            }
        }

        public int CountRecursive() {
            int count = conds.Count;

            foreach (var b in branches)
                count += b.CountRecursive();

            return count; 
        }

        #region Encode & Decode
        public override bool IsDefault => (conds.Count == 0 && branches.Count == 0);

        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add_IfNotEmpty("wb", branches)
            .Add_IfNotEmpty("v", conds)
            .Add_ifNotZero("t", (int)type)
            .Add_IfNotEmpty("d", description)
            .Add("tag", targ)
            .Add_IfNotNegative("insB", browsedBranch)
            .Add_IfNotNegative("ic", browsedCondition);

        public override bool Decode(string subtag, string data)
        {
            switch (subtag)
            {
                case "t": type = (ConditionBranchType)data.ToInt(); break;
                case "d": description = data; break;
                case "tag": data.DecodeInto(out targ); break;
                case "wb": data.Decode_List(out branches); break;
                case "v": data.Decode_List(out conds); break;
                case "insB": browsedBranch = data.ToInt(); break;
                case "ic": browsedCondition = data.ToInt(); break;
                default: return false;
            }
            return true;
        }
        #endregion

        public bool IsTrue => CheckConditions(TargetValues);

        public bool CheckConditions(Values vals) {
            vals = targ.TryGetValues(vals);

            switch (type)
            {
                case ConditionBranchType.AND:
                    foreach (var c in conds)
                        if (c.TestFor(vals) == false) return false;
                    foreach (var b in branches)
                        if (b.CheckConditions(vals) == false) return false;
                    return true;
                case ConditionBranchType.OR:
                    foreach (var c in conds)
                        if (c.TestFor(vals) == true)
                            return true;
                    foreach (var b in branches)
                        if (b.CheckConditions(vals) == true) return true;
                    return ((conds.Count == 0) && (branches.Count == 0));
            }
            return true;
        }

        public bool TryForceTo(bool toTrue) => TryForceTo(TargetValues, toTrue);
        
        public bool TryForceTo(Values vals, bool toTrue)  {

            vals = targ.TryGetValues(vals);

            if ((toTrue && type == ConditionBranchType.AND) || (!toTrue && type == ConditionBranchType.OR)) {
                bool anyApplied = false;
                foreach (var c in conds)
                    anyApplied |= c.TryForceConditionValue(vals, toTrue);
                foreach (var b in branches)
                    anyApplied |= b.TryForceTo(vals, toTrue);

                return toTrue || anyApplied;

            } else {

                foreach (var c in conds)
                    if (c.TryForceConditionValue(vals, toTrue))
                        return true;
                

                foreach (var b in branches)
                    if (b.TryForceTo(vals, toTrue))
                        return true;

                return toTrue; 

            }
            
        }

        #region Inspector
        int browsedBranch = -1;
        int browsedCondition = -1;

#if PEGI
        public override bool Inspect() {

            var before = ConditionLogic.inspectedTarget;

            ConditionLogic.inspectedTarget = TargetValues;


            bool changed = false;

            if (browsedBranch == -1) {
                if (pegi.Click(type.ToString(),
                    (type == ConditionBranchType.AND ? "All conditions and sub branches should be true" : "At least one condition OR sub branch should be true")))
                    type = (type == ConditionBranchType.AND ? ConditionBranchType.OR : ConditionBranchType.AND);

                 (CheckConditions(ConditionLogic.inspectedTarget) ? icon.Active : icon.InActive).nl();
              
                var newC = "Conditions".edit_List(ref conds, ref browsedCondition, ref changed);
                if (newC != null)
                    newC.TriggerIndexes = TriggerGroup.TryGetLastUsedTrigger();
            }

            pegi.Line(Color.black);

            changed |= "Sub Branches".edit_List(ref branches, ref browsedBranch);
            

            ConditionLogic.inspectedTarget = before;

            return changed;
        }

        public bool PEGI_inList(IList list, int ind, ref int edited)
        {
            if ((IsTrue ? icon.Active : icon.InActive).Click() && !TryForceTo(!IsTrue)) 
                Debug.Log("No Conditions to force to {0}".F(!IsTrue));

            "[{0}]".F(this.CountForInspector).write(30);

            var changed = this.inspect_Name();

            if (icon.Enter.Click())
                edited = ind;

            return changed;
        }
#endif
        #endregion

    }


    public interface IAmConditional {
        bool CheckConditions(Values vals);
    }

    public static class ConditionalsExtensions {
    
        public static bool Test_And_For(this List<IAmConditional> lst, Values vals) {

        if (lst == null)
            return true;

            foreach (var e in lst) 
            if (e != null && !e.CheckConditions(vals))
                return false;


            return true;
        }

        public static bool IsTrue(this IAmConditional cond) => cond.CheckConditions(Values.global); 

        public static bool TryTestCondition (this object obj) {
            var cnd = obj as IAmConditional;
            if (cnd == null) return true;
            return cnd.IsTrue();
        }
    }


}

