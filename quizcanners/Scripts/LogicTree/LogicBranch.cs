using PlayerAndEditorGUI;
using QuizCannersUtilities;
using System.Collections.Generic;

namespace STD_Logic
{

    public class LogicBranch<T> : AbstractKeepUnrecognized_STD  , IGotName , IPEGI, IAmConditional, ICanBeDefault_STD, IPEGI_Searchable  where T: ISTD, new() {

        public string name = "no name";

        public List<LogicBranch<T>> subBranches = new List<LogicBranch<T>>();

        public ConditionBranch conditions = new ConditionBranch();

        public List<T> elements = new List<T>();

        public override bool IsDefault => subBranches.Count ==0 && conditions.IsDefault && elements.Count == 0;

        public List<T> CollectAll(ref List<T> lst) {

            if (lst == null)
                lst = new List<T>();

            lst.AddRange(elements);

            foreach (var b in subBranches)
                b.CollectAll(ref lst);

            return lst;
        }

        public bool CheckConditions(Values vals) => conditions.CheckConditions(Values.global);

        #region Encode & Decode
        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add_String("name", name)
            .Add("cond", conditions)
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
                case "cond": conditions.Decode(data); break;
                case "sub": data.Decode_List(out subBranches); break;
                case "el": data.Decode_List(out elements); break;
                case "ie": inspectedElement = data.ToInt(); break;
                case "is": inspectedStuff = data.ToInt(); break;
                case "br": inspectedBranch = data.ToInt(); break;
                default: return false;
            }
            return true;
        }
        #endregion

        #region Inspector

        LoopLock searchLoopLock = new LoopLock();

        public bool String_SearchMatch(string searchString)
        {
            if (searchLoopLock.Unlocked)
                using(searchLoopLock.Lock()){

                    if (conditions.SearchMatch_Obj(searchString))
                        return true;

                    foreach (var e in elements)
                        if (e.SearchMatch_Obj(searchString))
                            return true;

                    foreach (var sb in subBranches)
                        if (sb.SearchMatch_Obj(searchString))
                            return true;
                }

            return false;

        }

        public virtual string NameForElements => typeof(T).ToPEGIstring_Type();

        public string NameForPEGI
        {
            get { return name; }
            set { name = value; }
        }

        public override void ResetInspector() {
            inspectedElement = -1;
            inspectedBranch = -1;
            base.ResetInspector();
        }

        int inspectedElement = -1;
        int inspectedBranch = -1;

        #if PEGI
        static LogicBranch<T> parent;

        public override bool Inspect() {
            bool changed = false;
         
            pegi.nl();

            if (parent != null || conditions.CountForInspector>0)
                conditions.enter_Inspect_AsList(ref inspectedStuff, 1).nl(ref changed);
            
            parent = this;

            changed |= NameForElements.enter_List(ref elements, ref inspectedElement, ref inspectedStuff, 2).nl();
            
            changed |= "Sub Branches".enter_List(ref subBranches, ref inspectedBranch, ref inspectedStuff, 3).nl();

            parent = null;
            return changed;
        }

        #endif
        #endregion

    }
}