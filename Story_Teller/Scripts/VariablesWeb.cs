using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace StoryTriggerData {

    [Serializable]
    public class VariablesWeb : abstract_STD {
        public List<Condition> vars;
        public List<WebBranch> branches;


        public override void PEGI() {
            this.PEGI(null);
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

        public override void Decode(string subtag, string data) {
            switch (subtag) {
                case "wb": branches = data.ToListOf_STD<WebBranch>(); break;
                case "v": vars = data.ToListOf_STD<Condition>(); break;
            }
        }

        //#if UNITY_EDITOR
        public Condition addVar(WebBranch br) {
            //Condition tmp = new Condition();
            br.vars.Add(vars.Count);
            Condition tmp = vars.Add();
            return tmp;
        }

        public List<Condition> getAllFromBranch(WebBranch wb) {
            List<Condition> tmp = new List<Condition>();//[wb.vars.Count];

            for (int i = 0; i < wb.vars.Count; i++)
                tmp.Add(vars[wb.vars[i]]);

            return tmp;
        }

        public List<Condition> getAllFromBranch(int no) {
            WebBranch wb = branches[no];
            List<Condition> tmp = new List<Condition>();//[wb.vars.Count];

            for (int i = 0; i < wb.vars.Count; i++)
                tmp.Add(vars[wb.vars[i]]);

            return tmp;
        }

        public WebBranch addBranch(WebBranch br) {
            WebBranch tmp = new WebBranch();
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
                WebBranch subb = branches[i];
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
                WebBranch subb = branches[i];
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
            WebBranch wb = branches[no];

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

        public VariablesWeb(string data) {
            
            vars = new List<Condition>();
            branches = new List<WebBranch>();
            branches.Add(new WebBranch());
            if (data != null)
                Reboot(data);
        }
    }

    public enum ConditionBranchType { OR, AND }

    [Serializable]
    public class WebBranch : abstract_STD {

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
            cody.AddIfNotNull(targ);
            return cody;
        }

        public override void Decode(string subtag, string data) {
            switch (subtag) {
                case "t": type = (ConditionBranchType)data.ToInt(); break;
                case "b": branches = data.ToListOfInt_STD(); break;
                case "v": vars = data.ToListOfInt_STD(); break;
                case "d": description = data; break;
                case TaggedTarget.stdTag_TagTar: targ = new TaggedTarget(data); break;
            }
        }


        void Clear() {
            branches = new List<int>();
            vars = new List<int>();
        }

        public WebBranch(string data) {
            Clear();
            Reboot(data);
        }

        public WebBranch() {
            Clear();
        }
    }
}