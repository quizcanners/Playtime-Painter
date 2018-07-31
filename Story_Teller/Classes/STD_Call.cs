using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using SharedTools_Stuff;
using STD_Logic;

namespace StoryTriggerData {

    public static class STD_CallExtensions {
#if PEGI
        public static bool PEGI(this List<STD_Call> lst) {
            bool changed = false;

            if (STD_Call.edited == null) {
                pegi.newLine();

                for (int i = 0; i < lst.Count; i++) {
                    if (icon.Delete.Click(20)) {
                        lst.RemoveAt(i);
                        i--;
                    } else {
                        pegi.write(lst[i].GetDescription());
                        if (icon.Edit.Click(20))
                            STD_Call.edited = lst[i];
                    }
                    pegi.newLine();
                }

                pegi.newLine();

                if (pegi.Click("Add Call"))
                    lst.Add(new STD_Call());
                
            } else {
                if (icon.Close.Click(20))
                    STD_Call.edited = null;
                else
                    STD_Call.edited.PEGI();
            }

            return changed;
        }
#endif
    }

    public class STD_Call : Abstract_STD , IPEGI

    {

        public string tag = "null";
        public string _data = "null";

        public TaggedTarget targ;

        public string GetDescription() {
            return tag + (targ == null ? "_this" : targ.Trigger.name); 
        }

        public override bool Decode(string subtag, string data) {
            tag = subtag;
            _data = data;
            return true;
        }

        public override StdEncoder Encode() {
            StdEncoder cody = new StdEncoder();

            cody.Add_String(tag, _data);

            return cody;
        }

        public const string storyTag_resT = "resT";
#if PEGI
        public static string returnTag = "";
        public static string returnData = "";
        public static string objectSearch="";
        public static STD_Poolable browsedForCalls;
        public static STD_Call edited;

        public virtual bool PEGI() {
            bool changed = false;
            pegi.newLine();
            pegi.write(GetDescription());
            pegi.newLine();

            if ((browsedForCalls != null) && (!browsedForCalls.gameObject.activeSelf))
                browsedForCalls = null;

                if (browsedForCalls != null) {
                    if (icon.Close.Click(20))
                        browsedForCalls = null;
                    else
                    if (browsedForCalls.Call_PEGI()) {
                        tag = returnTag;
                        _data = returnData;
                        changed = true;
                    }
                    
                } else {

                    pegi.write("Search:", 70);
                    changed |= pegi.edit(ref objectSearch);
                    pegi.newLine();

                    int maxToShow = 20;

                    foreach (STD_Poolable obj in STD_Pool.AllEnabledObjects()) {
                   
                        if (objectSearch.SearchCompare(obj.gameObject.name)) {

                            pegi.write(obj.gameObject.name);

                            if (icon.Record.Click(20))
                                browsedForCalls = obj;
                        
                            pegi.newLine();

                            maxToShow--;
                        }

                        if (maxToShow < 0)
                            break;
                    }
                }
            return changed;
            }
#endif
    }

}
