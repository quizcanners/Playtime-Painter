using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QuizCannersUtilities;
using PlayerAndEditorGUI;

namespace STD_Logic {

    public class ConditionBranch : AbstractKeepUnrecognizedCfg, IPEGI, 
        IAmConditional, ICanBeDefaultCfg, IPEGI_ListInspect, IGotCount, IPEGI_Searchable, IGotName, INeedAttention {
        private enum ConditionBranchType { Or, And }

        private List<ConditionLogic> _conditions = new List<ConditionLogic>();
        private List<ConditionBranch> _branches = new List<ConditionBranch>();

        private string _name = "";

        private ConditionBranchType _type = ConditionBranchType.And;

        public int CountForInspector => CountRecursive();

        private int CountRecursive() {
            var count = _conditions.Count;

            foreach (var b in _branches)
                count += b.CountRecursive();

            return count; 
        }

        #region Inspector


        private int _browsedBranch = -1;
        private int _browsedCondition = -1;

        #if PEGI
        
        public string NeedAttention() {

            if (_branches.NeedsAttention() || _conditions.NeedsAttention())
                return pegi.LastNeedAttentionMessage;

            return null;
        }
        
        public string NameForPEGI { get { return _name; } set { _name = value; } }

        private readonly LoopLock _searchLoopLock = new LoopLock();

        public bool String_SearchMatch(string searchString) {
            if (!_searchLoopLock.Unlocked) return false;
            
            using (_searchLoopLock.Lock())
            {
                foreach (var c in _conditions)
                    if (c.SearchMatch_Obj(searchString))
                        return true;

                foreach (var b in _branches)
                    if (b.SearchMatch_Obj(searchString))
                        return true;
            }

            return false;
        }
        
        public override bool Inspect()
        {
            if (!_name.IsNullOrEmpty())
                _name.nl(PEGI_Styles.ListLabel);

            var before = ConditionLogic.inspectedTarget;

            ConditionLogic.inspectedTarget = Values.global;
            
            var changed = false;

            if (_browsedBranch == -1)
            {
                if (_type.ToString().Click((_type == ConditionBranchType.And ? "All conditions and sub branches should be true" : "At least one condition OR sub branch should be true")))
                    _type = (_type == ConditionBranchType.And ? ConditionBranchType.Or : ConditionBranchType.And);

                (CheckConditions(ConditionLogic.inspectedTarget) ? icon.Active : icon.InActive).nl();

                var newC = "Conditions".edit_List(ref _conditions, ref _browsedCondition, ref changed);
                if (newC != null)
                    newC.TriggerIndexes = TriggerGroup.TryGetLastUsedTrigger();
            }

            pegi.line(Color.black);

            changed |= "Sub Branches".edit_List(ref _branches, ref _browsedBranch);


            ConditionLogic.inspectedTarget = before;

            return changed;
        }

        public bool PEGI_inList(IList list, int ind, ref int edited)
        {
          
            var changed = false;
            
            if ((IsTrue ? icon.Active : icon.InActive).Click(ref changed) && !TryForceTo(Values.global, !IsTrue))
                Debug.Log("No Conditions to force to {0}".F(!IsTrue));

            var cnt = CountForInspector;

            switch (cnt)
            {
                case 0:
                    "{0}: Unconditional".F(_name).write();
                    break;
                case 1:
                    if (_conditions.Count == 1)
                        "{0}: {1}".F(_name, _conditions[0].ToPegiString()).write();
                    else goto default;
                    break;
                default:
                    if (_branches.Count>0)
                        "{0}: {1} conditions; {2} branches".F(_name, cnt, _branches.Count).write();
                    else 
                        "{0}: {1} conditions".F(_name, cnt).write();
                    break;
            }

            
            
            if (this.Click_Enter_Attention(icon.Condition, "Explore Condition branch", false))
                edited = ind;
            
            return changed;
        }
        #endif
        #endregion
        
        #region Encode & Decode
        public override bool IsDefault => (_conditions.Count == 0 && _branches.Count == 0);

        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add_IfNotEmpty("wb",         _branches)
            .Add_IfNotEmpty("v",          _conditions)
            .Add("t",                     (int)_type)
            .Add_IfNotNegative("insB",    _browsedBranch)
            .Add_IfNotNegative("ic",      _browsedCondition);

        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "t":     _type = (ConditionBranchType)data.ToInt(); break;
                case "wb":    data.Decode_List(out _branches); break;
                case "v":     data.Decode_List(out _conditions); break;
                case "insB":  _browsedBranch = data.ToInt(); break;
                case "ic":    _browsedCondition = data.ToInt(); break;
                default:      return false;
            }
            return true;
        }
        #endregion

        public bool IsTrue => CheckConditions(Values.global);
        
        public bool CheckConditions(Values values) {

            switch (_type) {
                case ConditionBranchType.And:
                    foreach (var c in _conditions)
                        if (!c.TestFor(values)) return false;
                    foreach (var b in _branches)
                        if (!b.CheckConditions(values)) return false;
                    return true;
                case ConditionBranchType.Or:
                    foreach (var c in _conditions)
                        if (c.TestFor(values))
                            return true;
                    foreach (var b in _branches)
                        if (b.CheckConditions(values)) return true;
                    return (_conditions.Count == 0 && _branches.Count == 0);
            }
            return true;
        }

        public bool TryForceTo(Values values, bool toTrue)  {

            if ((toTrue && _type == ConditionBranchType.And) || (!toTrue && _type == ConditionBranchType.Or)) {
                var anyApplied = false;
                foreach (var c in _conditions)
                    anyApplied |= c.TryForceConditionValue(values, toTrue);
                foreach (var b in _branches)
                    anyApplied |= b.TryForceTo(values, toTrue);

                return toTrue || anyApplied;

            } else {

                foreach (var c in _conditions)
                    if (c.TryForceConditionValue(values, toTrue))
                        return true;
                
                foreach (var b in _branches)
                    if (b.TryForceTo(values, toTrue))
                        return true;

                return toTrue; 

            }
            
        }
        
        public ConditionBranch() { }

        public ConditionBranch(string usage)
        {
            _name = usage;
        }

    }
}

