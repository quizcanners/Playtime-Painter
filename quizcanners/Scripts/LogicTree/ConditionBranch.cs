using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using QuizCannersUtilities;
using PlayerAndEditorGUI;

namespace STD_Logic {

    public class ConditionBranch : AbstractKeepUnrecognized_STD, IPEGI, 
        IAmConditional, ICanBeDefault_STD, IPEGI_ListInspect, IGotCount, IPEGI_Searchable, IGotName {

        public enum ConditionBranchType { OR, AND }

        public List<ConditionLogic> conditions = new List<ConditionLogic>();
        public List<ConditionBranch> branches = new List<ConditionBranch>();

        string name = "";

        public ConditionBranchType type = ConditionBranchType.AND;
        public TaggedTarget targ;

        Values TargetValues => targ.TryGetValues(Values.global);

        public int CountForInspector => CountRecursive();
        
        public int CountRecursive() {
            int count = conditions.Count;

            foreach (var b in branches)
                count += b.CountRecursive();

            return count; 
        }
        
        #region Inspector
        int browsedBranch = -1;
        int browsedCondition = -1;

        #if PEGI
        public string NameForPEGI { get => name; set => name = value; }

        LoopLock searchLoopLock = new LoopLock();

        public bool String_SearchMatch(string searchString) {

            if (searchLoopLock.Unlocked)
                using (searchLoopLock.Lock())
                {
                    foreach (var c in conditions)
                        if (c.SearchMatch_Obj(searchString))
                            return true;

                    foreach (var b in branches)
                        if (b.SearchMatch_Obj(searchString))
                            return true;
                }

            return false;
        }
        
        public override bool Inspect()
        {
            if (!name.IsNullOrEmpty())
                name.nl(PEGI_Styles.EnterLabel);

            var before = ConditionLogic.inspectedTarget;

            ConditionLogic.inspectedTarget = TargetValues;
            
            bool changed = false;

            if (browsedBranch == -1)
            {
                if (pegi.Click(type.ToString(),
                    (type == ConditionBranchType.AND ? "All conditions and sub branches should be true" : "At least one condition OR sub branch should be true")))
                    type = (type == ConditionBranchType.AND ? ConditionBranchType.OR : ConditionBranchType.AND);

                (CheckConditions(ConditionLogic.inspectedTarget) ? icon.Active : icon.InActive).nl();

                var newC = "Conditions".edit_List(ref conditions, ref browsedCondition, ref changed);
                if (newC != null)
                    newC.TriggerIndexes = TriggerGroup.TryGetLastUsedTrigger();
            }

            pegi.line(Color.black);

            changed |= "Sub Branches".edit_List(ref branches, ref browsedBranch);


            ConditionLogic.inspectedTarget = before;

            return changed;
        }

        public bool PEGI_inList(IList list, int ind, ref int edited)
        {
            var changed = false;

            if ((IsTrue ? icon.Active : icon.InActive).Click() && !TryForceTo(!IsTrue))
                Debug.Log("No Conditions to force to {0}".F(!IsTrue));

            var cnt = CountForInspector;

            if (cnt == 0)
                "{0} Unconditional".F(name).write();
            else if (cnt == 1)
            {

                if (conditions.Count == 1)
                    "{0} If {1}".F(name, conditions[0].ToPEGIstring()).write();
                else "{0} Got subbranch".F(name).write();

            }
            else
                "{0} {1} conditions".F(name, cnt).write();

            if (icon.Enter.Click())
                edited = ind;

            return changed;
        }
        #endif
        #endregion
        
        #region Encode & Decode
        public override bool IsDefault => (conditions.Count == 0 && branches.Count == 0);

        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add_IfNotEmpty("wb", branches)
            .Add_IfNotEmpty("v", conditions)
            .Add_IfNotZero("t", (int)type)
            .Add("tag", targ)
            .Add_IfNotNegative("insB", browsedBranch)
            .Add_IfNotNegative("ic", browsedCondition);

        public override bool Decode(string subtag, string data)
        {
            switch (subtag)
            {
                case "t": type = (ConditionBranchType)data.ToInt(); break;
                case "tag": data.DecodeInto(out targ); break;
                case "wb": data.Decode_List(out branches); break;
                case "v": data.Decode_List(out conditions); break;
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
                    foreach (var c in conditions)
                        if (c.TestFor(vals) == false) return false;
                    foreach (var b in branches)
                        if (b.CheckConditions(vals) == false) return false;
                    return true;
                case ConditionBranchType.OR:
                    foreach (var c in conditions)
                        if (c.TestFor(vals) == true)
                            return true;
                    foreach (var b in branches)
                        if (b.CheckConditions(vals) == true) return true;
                    return ((conditions.Count == 0) && (branches.Count == 0));
            }
            return true;
        }

        public bool TryForceTo(bool toTrue) => TryForceTo(TargetValues, toTrue);
        
        public bool TryForceTo(Values vals, bool toTrue)  {

            vals = targ.TryGetValues(vals);

            if ((toTrue && type == ConditionBranchType.AND) || (!toTrue && type == ConditionBranchType.OR)) {
                bool anyApplied = false;
                foreach (var c in conditions)
                    anyApplied |= c.TryForceConditionValue(vals, toTrue);
                foreach (var b in branches)
                    anyApplied |= b.TryForceTo(vals, toTrue);

                return toTrue || anyApplied;

            } else {

                foreach (var c in conditions)
                    if (c.TryForceConditionValue(vals, toTrue))
                        return true;
                
                foreach (var b in branches)
                    if (b.TryForceTo(vals, toTrue))
                        return true;

                return toTrue; 

            }
            
        }
        
        public ConditionBranch() { }

        public ConditionBranch(string usage)
        {
            name = usage;
        }
    }
}

