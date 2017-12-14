using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using PlayerAndEditorGUI;



namespace StoryTriggerData
{

    public enum ConditionType { Bool , Above, Below, Equals, RealTimePassedAbove, RealTimePassedBelow, GameTimePassedAbove, GameTimePassedBelow, NotEquals }

    public static class ConditionExtensionFunctions
    {

        public static bool TestConditions(this VariablesWeb tree, STD_Values ip) {
            bool tmp = tree.ConditionsCascade(0, ip);
            return tmp;
        }

        public static bool ConditionsCascade(this VariablesWeb tree, int branch, STD_Values ip) {
            WebBranch vb = tree.branches[branch];
            switch (vb.type) {
                case ConditionBranchType.AND:
                    for (int i = 0; i < vb.vars.Count; i++)
                        if (tree.vars[vb.vars[i]].TestFor(ip) == false) return false;
                    for (int i = 0; i < vb.branches.Count; i++)
                        if (ConditionsCascade(tree, vb.branches[i], ip) == false) return false;
                    return true;
                case ConditionBranchType.OR:
                    for (int i = 0; i < vb.vars.Count; i++)
                        if (tree.vars[vb.vars[i]].TestFor(ip) == true) return true;
                    for (int i = 0; i < vb.branches.Count; i++)
                        if (ConditionsCascade(tree, vb.branches[i], ip) == true) return true;
                    return ((vb.vars.Count == 0) && (vb.branches.Count == 0));
            }
            return true;
        }

        public static void MakeConditionsTrue(this VariablesWeb tree, STD_Values ip) {
            tree.MakeCondTrueCascade(0, ip);
        }

        static void MakeCondTrueCascade(this VariablesWeb tree, int branch, STD_Values ip) {
            WebBranch vb = tree.branches[branch];

            switch ((ConditionBranchType)vb.type) {
                case ConditionBranchType.AND:
                    for (int i = 0; i < vb.vars.Count; i++)
                        tree.vars[vb.vars[i]].ForceConditionTrue(ip);
                    for (int i = 0; i < vb.branches.Count; i++)
                        tree.MakeCondTrueCascade(vb.branches[i], ip);
                    break;
                case ConditionBranchType.OR:
                    if (vb.vars.Count > 0) {
                        tree.vars[vb.vars[0]].ForceConditionTrue(ip);
                        return;
                    }
                    if (vb.branches.Count > 0) {
                        tree.MakeCondTrueCascade( vb.branches[0], ip);
                        return;
                    }
                    break;
            }

        }


        public static string ToStringSafe(this VariablesWeb c, STD_Values tell, bool showDetails)
        {

            bool AnyConditions = (c.vars.Count > 0);

           return c.TestConditions(tell) + " " + (showDetails ? "..." :
                     ((AnyConditions) ? "[" + c.vars.Count + "]: " + c.vars[0].ToString() : "UNCONDITIONAL"));
        }


        public static VariablesWeb lastTree = null;

        public static List<int> browsingBranch = new List<int>();

        public static bool PEGI(this VariablesWeb tree, STD_Values so) {

            bool changed = false;

                if (tree != lastTree) 
                    browsingBranch.Clear();
                lastTree = tree;

                bool root = browsingBranch.Count == 0;

            int brNo = root ? 0 : browsingBranch.last();//[browsingBranch.Count - 1];
                WebBranch wb = tree.branches[brNo];

            if ((!root) && icon.Close.Click("Back",20)) browsingBranch.RemoveLast(1);

                pegi.newLine();

            if (pegi.Click("Logic: " + wb.type + (wb.type == ConditionBranchType.AND ? " (ALL should be true)" : " (At least one should be true)"), 
                           (wb.type == ConditionBranchType.AND ? "All conditions and sub branches should be true" : 
                            "At least one condition or sub branch should be true")))
                    wb.type = (wb.type == ConditionBranchType.AND ? ConditionBranchType.OR : ConditionBranchType.AND);

            pegi.newLine();

            pegi.write("Conditions: ");
            if (icon.Add.Click(25))
                tree.addVar(wb);

            pegi.newLine();

            List<Condition> conds = tree.getAllFromBranch(wb);
            int del = conds.PEGI(so);
            if (del != -1) tree.DeleteVar(wb.vars[del]);

            pegi.newLine();

            pegi.write("Sub Branches: ");

            if (icon.Add.Click(25))
                tree.addBranch(wb);

                pegi.newLine();

                for (int i = 0; i < wb.branches.Count; i++) {
                int index = wb.branches[i];

                if (icon.Delete.Click(20))
                {
                    tree.DeleteBranch(index);
                    changed = true;
                }
                else
                {
                    changed |= pegi.edit(ref tree.branches[index].description);
                    if (icon.Edit.Click(20))
                        browsingBranch.Add(index);
                }

                    pegi.newLine();
                }

              

              
             
                pegi.newLine();
            return changed;
            }

        public static Condition Add(this List<Condition> lst) {
            Condition r = new Condition();

            if (lst.Count > 0) {
                Condition prev = lst.last();

                r.groupIndex = prev.groupIndex;
                r.triggerIndex = prev.triggerIndex;

                // Making sure new trigger will not be a duplicate (a small quality of life improvement)

                List<int> indxs;
                r.group.triggers.GetAllObjs(out indxs);

                foreach (Condition res in lst)
                    if (res.groupIndex == r.groupIndex)
                        indxs.Remove(res.triggerIndex);

                if (indxs.Count > 0)
                    r.triggerIndex = indxs[0];

            }

            lst.Add(r);

            return r;
        }

        static int PEGI(this List<Condition> conds, STD_Values so) {

            bool changed = false;

            int DeleteNo = -1;

            for (int i = 0; i < conds.Count; i++) {
                Condition cond = conds[i];

                changed |= cond.PEGI(i, ref DeleteNo, so, "Cond");

                cond.trig._usage.conditionPEGI(cond, so);

                changed |= cond.searchAndAdd_PEGI(i);

            }

                return DeleteNo;

            }

        public static void ConditionsFoldout(ref VariablesWeb cond, ref bool Show, string descr) {
                bool AnyConditions = ((cond != null) && (cond.vars.Count > 0));
                pegi.foldout(descr + (Show ? "..." :
                  ((AnyConditions) ? "[" + cond.vars.Count + "]: " + cond.vars[0].ToString() : "UNCONDITIONAL")), ref Show);

            }

        public static string CompileBranchText(VariablesWeb tree, int branch, STD_Values ip) {
                WebBranch cb = tree.branches[branch];
                int br = cb.branches.Count;
                int Conds = cb.vars.Count;

                return cb.type + ":" + tree.ConditionsCascade( branch, ip).ToString() + (br > 0 ? br + " br," : " ") + (Conds > 0 ? (tree.vars[cb.vars[0]].ToString()) +
                    (Conds > 1 ? "+" + (Conds - 1) : "") : "");

            }
    }


    [Serializable]
    public class Condition : Argument , iSTD {

        public static string tag = "cond";

        public int _type;
        public int compareValue;

        public ConditionType type { get { return (ConditionType)_type; } set { _type = (int)value; } }

        public string getDefaultTagName() {
            return tag;
        }

        public stdEncoder Encode() {
            var cody = new stdEncoder();

            cody.AddIfNotZero("v", compareValue);
            cody.AddIfNotZero("ty",_type);
            cody.Add("g", groupIndex);
            cody.Add("t", triggerIndex);

            return cody;
        }

        public void Decode(string subtag, string data) {
            switch (subtag) {
                case "v": compareValue = data.ToInt(); break;
                case "ty": _type = data.ToInt(); break;
                case "g": groupIndex = data.ToInt(); break;
                case "t": triggerIndex = data.ToInt(); break;
            }
        }

        public void Reboot(string data) {
            new stdDecoder(data).DecodeTagsFor(this);
        }

        public void ForceConditionTrue(STD_Values st) {

            switch (type) {
                case ConditionType.Bool:  SetBool(st,compareValue > 0); break;
                case ConditionType.Above: SetInt(st, compareValue + 1); break;
                case ConditionType.Below: SetInt(st, compareValue - 1); break;
                case ConditionType.Equals: SetInt(st, compareValue); break;
                case ConditionType.NotEquals: if (GetInt(st) == compareValue) st.ints[groupIndex].Add(triggerIndex, 1); break;
            }
        }

        public bool TestFor(STD_Values st) {

            int timeGap;

            switch (type) {
                case ConditionType.Bool: if (GetBool(st) == ((compareValue > 0) ? true : false)) return true; break;
                case ConditionType.Above: if (GetInt(st) > compareValue) return true; break;
                case ConditionType.Below: if (GetInt(st) < compareValue) return true; break;
                case ConditionType.Equals: if (GetInt(st) == compareValue) return true; break;
                case ConditionType.NotEquals: if (GetInt(st) != compareValue) return true; break;
                case ConditionType.GameTimePassedAbove:
                    timeGap = (int)Time.time - GetInt(st);
                    if (timeGap > compareValue) return true; Book.inst.AddTimeListener(compareValue - timeGap); break;
                case ConditionType.GameTimePassedBelow:
                    timeGap = (int)Time.time - GetInt(st);
                    if (timeGap < compareValue) { Book.inst.AddTimeListener(compareValue - timeGap); return true; }
                    break;
                case ConditionType.RealTimePassedAbove:
                    timeGap = (Book.GetRealTime() - GetInt(st));
                    if (timeGap > compareValue) return true; Book.inst.AddTimeListener(compareValue - timeGap); break;
                case ConditionType.RealTimePassedBelow:
                    timeGap = (Book.GetRealTime() - GetInt(st));
                    if (timeGap < compareValue) { Book.inst.AddTimeListener(compareValue - timeGap); return true; }
                    break;

          


            }
            //Debug.Log ("No pass on: " + glob.triggers.triggers [cond.TriggerNo].name+ " with "+cond.Type);
            return false;
        }

        public int isItClaimable( int dir, STD_Values st) {

            switch ((ConditionType)_type) {
                case ConditionType.Bool: if ((dir > 0) == (compareValue > 0)) return 1; break;
                case ConditionType.Above: if ((GetInt(st) < compareValue) && (dir > 0)) return (compareValue - GetInt(st) + 1); break;
                case ConditionType.Below: if ((GetInt(st) > compareValue) && (dir < 0)) return (GetInt(st) - compareValue + 1); break;
                case ConditionType.Equals: if ((GetInt(st) > compareValue) == (dir < 0)) return Mathf.Abs(GetInt(st) - compareValue); break;
            }
            return -2;
        }


        public Condition() {
            groupIndex = TriggerGroups.browsed.GetHashCode();
        }

        public override bool isBoolean(){
            return _type == (int)ConditionType.Bool;
        }


        public static bool unfoldPegi;
      

        public override string ToString()
        {
            return (trig.name) + " " + _type + " " + (isBoolean() ?
                                            (compareValue == 1 ? "True" : "false")
                                            : compareValue.ToString());
        }


     

    

    }


}