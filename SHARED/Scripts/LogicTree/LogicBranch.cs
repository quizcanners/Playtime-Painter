using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SharedTools_Stuff;
using PlayerAndEditorGUI;
using System;

namespace STD_Logic {

    public class LogicBranch<T> : AbstractKeepUnrecognized_STD 
        #if PEGI
        , IGotName , IPEGI
#endif
        where T: iSTD, new()
    {

        public List<LogicBranch<T>> subBranches = new List<LogicBranch<T>>();

        public ConditionBranch conds = new ConditionBranch();

        public List<T> elements = new List<T>();
        
        public string name = "no name";

        public string NameForPEGI { get{ return name;  }
            set { name = value; }
        }

        public virtual List<T> CollectPassedFor (Values val) {
            var collected = new List<T>();

            Collect(val, ref collected);
        
            return collected;
        }

        public virtual void Collect (Values val, ref List<T> collected) {
            if (conds.TestFor(val)) {
                collected.AddRange(elements);

                foreach (var b in subBranches)
                    Collect(val, ref collected);
            }
        }

        public override StdEncoder Encode() => EncodeUnrecognized()
            .Add_String("name", name)
            .Add("cond", conds)
            .Add_ifNotEmpty("sub", subBranches)
            .Add("el", elements)
            .Add_ifNotNegative("brE", browsedElement)
            .Add_ifNotNegative("brB", browsedBranch)
            .Add_ifTrue("conds", showConditions);

        public override bool Decode(string subtag, string data) {
            switch (subtag)
            {
                case "name": name = data; break;
                case "cond": data.DecodeInto(conds); break;
                case "sub": data.DecodeInto(out subBranches); break; //new List<InteractionGroup>(data); break;
                case "el": data.DecodeInto(out elements); break;
                case "brE": browsedElement = data.ToInt(); break;
                case "brB": browsedBranch = data.ToInt(); break;
                case "conds": showConditions = data.ToBool(); break;
                default:  return false;
            }
            return true;
        }

        public void getAllInteractions(ref List<T> lst)
        {
            lst.AddRange(elements);
            foreach (LogicBranch<T> ig in subBranches)
                ig.getAllInteractions(ref lst);
        }

        bool showConditions = false;
        int browsedElement = -1;
        int browsedBranch = -1;

        #if PEGI
       
        static string path;
        static bool isCalledFromAnotherBranch = false;
        public override bool PEGI() {
            bool changed = false;
            
            browsedBranch = Mathf.Min(browsedBranch, subBranches.Count-1);

            if (!isCalledFromAnotherBranch)
                path = "Brances:";
           else
                path += "->" + name;

            if (browsedBranch == -1)
            {
                browsedElement = Mathf.Min(browsedElement, elements.Count - 1);
             

                if (browsedElement == -1)  {
       
                    Values vals = LogicMGMT.inst != null ? LogicMGMT.inst.InspectedValues() : null;
                    
                    bool isTrue = vals != null ? conds.TestFor(vals) : false;

                    if ( icon.Condition.foldout(
                        ("Conditions" +( vals!= null ? "["+ (isTrue ? "True" : "False") +"]"  : " "))
                        //"text"
                        , ref showConditions).nl())
                    
                        changed |= conds.PEGI(vals); 
                    
                    else
                    {
                        changed |= "Elements:".edit_List(elements, ref browsedElement, true);

                        changed |= "Sub Branches:".edit_List(subBranches, ref browsedBranch, true);
                    }

                } else  {
                    if (icon.Exit.Click())
                        browsedElement = -1;
                    else {
                        
                        changed |= elements[browsedElement].Try_Nested_Inspect();
                    }
                }

            }
            else
            {
                isCalledFromAnotherBranch = true;
                var sub = subBranches[browsedBranch];
                if (sub.browsedBranch == -1 && icon.Exit.ClickUnfocus())
                    browsedBranch = -1;
                else
                    changed |= sub.PEGI();
                isCalledFromAnotherBranch = false;
            }
            

            return changed;
        }

#endif
    }

    /*public interface iCleanMyself {
        void StartFadeAway();
        bool CancelFade(); // Returns true if it was possible to revert fading
    }*/

}