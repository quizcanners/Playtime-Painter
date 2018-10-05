using PlayerAndEditorGUI;
using SharedTools_Stuff;
using System.Collections.Generic;

namespace STD_Logic
{

    public class LogicBranch<T> : AbstractKeepUnrecognized_STD  , IGotName , IPEGI  where T: ISTD, new() {

        public string name = "no name";

        public List<LogicBranch<T>> subBranches = new List<LogicBranch<T>>();

        public ConditionBranch conds = new ConditionBranch();

        public List<T> elements = new List<T>();

        public List<T> CollectAll(ref List<T> lst) {

            lst.AddRange(elements);

            foreach (var b in subBranches)
                b.CollectAll(ref lst);

            return lst;
        }

        #region Encode & Decode
        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add_String("name", name)
            .Add("cond", conds)
            .Add_IfNotEmpty("sub", subBranches)
            .Add_IfNotEmpty("el", elements)
            .Add_IfNotNegative("ie", inspectedElement)
            .Add_IfNotNegative("is", inspectedStuff)
            .Add_IfNotNegative("br", inspectedBranch);
        
        public override bool Decode(string subtag, string data)
        {
            switch (subtag)
            {
                case "name": name = data; break;
                case "cond": conds.Decode(data); break;
                case "sub": data.DecodeInto(out subBranches); break;
                case "el": data.DecodeInto(out elements); break;
                case "ie": inspectedElement = data.ToInt(); break;
                case "is": inspectedStuff = data.ToInt(); break;
                case "br": inspectedBranch = data.ToInt(); break;
                default: return false;
            }
            return true;
        }
        #endregion

        #region Inspector

        public virtual string NameForElements => "Unnamed";

        public string NameForPEGI
        {
            get { return name; }
            set { name = value; }
        }

        int inspectedStuff = -1;
        int inspectedElement = -1;
        int inspectedBranch = -1;

        #if PEGI
        public override bool PEGI() {
            bool changed = false;

            changed |= NameForElements.fold_enter_exit_List(elements, ref inspectedElement, ref inspectedStuff, 0).nl();

            "Conditions".fold_enter_exit(ref inspectedStuff, 1).nl();
            changed |= conds.PEGI();

            changed |= "Sub Branches".fold_enter_exit_List(subBranches, ref inspectedBranch, ref inspectedStuff, 2).nl();

            return changed;
        }
        #endif
        #endregion

    }



}