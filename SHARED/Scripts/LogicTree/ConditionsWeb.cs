using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using SharedTools_Stuff;

namespace LogicTree
{

    [Serializable]
    public class ConditionsWeb : abstract_STD {
        public List<Condition> vars;
        public List<ConditionsBranch> branches;
        
        public override bool PEGI() {
            //return false;
             return this.PEGI(null);
        }


        public override string getDefaultTagName() {
            return "condWeb";
        }


        public override stdEncoder Encode() {
            var cody = new stdEncoder();

            cody.AddIfNotEmpty("wb",branches);
            cody.AddIfNotEmpty("v",vars);

            return cody;
        }

        public override bool Decode(string subtag, string data) {
            switch (subtag) {
                case "wb": data.DecodeInto(out branches); break;
                case "v": data.DecodeInto(out vars); break;
                default: return false;
            }
            return true;
        }

        //#if UNITY_EDITOR
        public Condition addVar(ConditionsBranch br) {
            //Condition tmp = new Condition();
            br.vars.Add(vars.Count);
            Condition tmp = vars.Add();
            return tmp;
        }

        public List<Condition> getAllFromBranch(ConditionsBranch wb) {
            List<Condition> tmp = new List<Condition>();//[wb.vars.Count];

            for (int i = 0; i < wb.vars.Count; i++)
                tmp.Add(vars[wb.vars[i]]);

            return tmp;
        }

        public List<Condition> getAllFromBranch(int no) {
            ConditionsBranch wb = branches[no];
            List<Condition> tmp = new List<Condition>();//[wb.vars.Count];

            for (int i = 0; i < wb.vars.Count; i++)
                tmp.Add(vars[wb.vars[i]]);

            return tmp;
        }

        public ConditionsBranch addBranch(ConditionsBranch br) {
            ConditionsBranch tmp = new ConditionsBranch();
            br.branches.Add(branches.Count);
            branches.Add(tmp);
            return tmp;
        }

        public void DeleteVar(int no) {
            vars[no] = null; //default(T);
            ReindexVars();
        }

        public void DeleteBranch(int no) {
            DeleteCascade(no);

            //  Debug.Log("Deleting "+ no);

            int[] brinds = new int[branches.Count];
            int newInd = 0;
            for (int i = 0; i < branches.Count; i++)
                if (branches[i] != null) { brinds[i] = newInd; newInd++; } else {
                    //     Debug.Log("brind "+i+" is null");
                    brinds[i] = -1;
                }

            for (int i = 0; i < branches.Count; i++) {
                ConditionsBranch subb = branches[i];
                if (subb != null) {
                    for (int j = 0; j < subb.branches.Count; j++)
                        if (brinds[subb.branches[j]] == -1) {
                            //  Debug.Log("Deleting sub " + j);
                            subb.branches.RemoveAt(j); j--;
                        } else {
                            // Debug.Log("Assigning "+ subb.branches[j] + " to "+ brinds[subb.branches[j]]);
                            subb.branches[j] = brinds[subb.branches[j]];

                        }
                } else {
                    // Debug.Log("Deleting branch "+i);
                    branches.RemoveAt(i); i--;
                }
            }

            //   Debug.Log("Left "+branches.Count+" branches ");

            ReindexVars();
        }

        void ReindexVars() {
            int[] varinds = new int[vars.Count];
            int newInd = 0;
            for (int i = 0; i < vars.Count; i++) {
                if (vars[i] != null) { varinds[i] = newInd; newInd++; } else {
                    varinds[i] = -1;
                    //  Debug.Log("Deleting variable "+i+" out of "+vars.Count);
                }
            }

            for (int i = 0; i < branches.Count; i++) {
                ConditionsBranch subb = branches[i];
                for (int j = 0; j < subb.vars.Count; j++) {
                    if (varinds[subb.vars[j]] == -1) {
                        //Debug.Log("Removing at " + subb.vars[j]);
                        subb.vars.RemoveAt(j);
                        j--;
                    } else
                        subb.vars[j] = varinds[subb.vars[j]];
                }
            }

            for (int j = vars.Count - 1; j >= 0; j--)
                if (varinds[j] == -1) {
                    vars.RemoveAt(j);
                }

            //   Debug.Log("Finished got "+vars.Count+" left");

        }

        void DeleteCascade(int no) {
            ConditionsBranch wb = branches[no];

            if (wb.branches.Count > 0)
                for (int i = 0; i < wb.branches.Count; i++)
                    DeleteBranch(wb.branches[i]);

            for (int i = 0; i < wb.vars.Count; i++)
                vars[wb.vars[i]] = null;//default(T);

            for (int i = 0; i < wb.branches.Count; i++)
                branches[wb.branches[i]] = null;

            branches[no] = null;
        }
        //#endif

        public ConditionsWeb(string data) {
            
            vars = new List<Condition>();
            branches = new List<ConditionsBranch>();
            branches.Add(new ConditionsBranch());
            if (data != null)
                Decode(data);
        }
    }

    public enum ConditionBranchType { OR, AND }

    [Serializable]
    public class ConditionsBranch : abstract_STD {

        public const string tag = "br";

        public override string getDefaultTagName() {
            return tag;
        }

        public List<int> branches;
        public List<int> vars;
        public ConditionBranchType type;
        public string description = "new branch";
        public TaggedTarget targ;
        
        public override stdEncoder Encode() {
            var cody = new stdEncoder();

            cody.AddIfNotZero("t",(int)type );
            cody.AddIfNotEmpty("b",branches);
            cody.AddIfNotEmpty("v", vars);
            cody.AddText("d", description);
            cody.Add(targ);
            return cody;
        }

        public override bool Decode(string subtag, string data) {
            switch (subtag) {
                case "t": type = (ConditionBranchType)data.ToInt(); break;
                case "b": data.DecodeInto(out branches); break;
                case "v": data.DecodeInto(out vars); break;
                case "d": description = data; break;
                case TaggedTarget.stdTag_TagTar: data.DecodeInto(out targ); break;
                default: return false;
            }
            return true;
        }
        
        void Clear() {
            branches = new List<int>();
            vars = new List<int>();
        }

        public ConditionsBranch() {
            Clear();
        }
    }
}