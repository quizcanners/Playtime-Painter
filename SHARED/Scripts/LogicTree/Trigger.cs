using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif
using PlayerAndEditorGUI;

using SharedTools_Stuff;

namespace STD_Logic
{
    
    public class Trigger : ValueIndex , IPEGI, IGotDisplayName , IPEGI_ListInspect
    {

        // public static string searched = "";
        public static string TriggerEdControlName;
        public static string EditedtextHold;
        public static int focusIndex = -2;
        public static string searchField = " ";
        public static bool filterBoolean = true;
        public static bool filterIntagers = true;
        public static bool showTriggers;
        public static int searchMatchesFound;

        public static Trigger editedTrigger;
        public string name = "";
        public bool isStatic;

        public Dictionary<int, string> enm;

        public string this[int index] {
            get {
                string hold = "_";
                enm.TryGetValue(index, out hold);
                return hold;
            }
        }
        
        int usage;

        public TriggerUsage _usage { get { return TriggerUsage.Get(usage); }  set { usage = value.index; } }

        public override bool IsBoolean() => _usage.UsingBool;

#if PEGI
        public static void Search_PEGI() {
            pegi.write("Search", 60);
            pegi.edit(ref searchField);//, GUILayout.Width(60));
            pegi.newLine();
        }
#endif
        public bool SearchWithGroupName(string groupName) {
            
            if ((searchField.Length == 0) || Regex.IsMatch(name, searchField, RegexOptions.IgnoreCase)) return true;

            if (searchField.Contains(" ")) {
               
                string[] sgmnts = searchField.Split(' ');
                for (int i = 0; i < sgmnts.Length; i++) {
                    string sub = sgmnts[i];
                    if ((!Regex.IsMatch(name, sub, RegexOptions.IgnoreCase))
                        && (!Regex.IsMatch(groupName, sub, RegexOptions.IgnoreCase))) return false;
                }
                    return true;
                }
                return false;
            
        }

        public override StdEncoder Encode() {
            StdEncoder cody = new StdEncoder()
            .Add_String("n", name)
            .Add("u", usage)
            .Add_IfNotEmpty("e", enm)
            .Add_Bool ("s", isStatic);
            return cody;
        }

        public override bool Decode(string tag, string data) {

            switch (tag) {
                case "n": name = data; break;
                case "u": usage = data.ToInt(); break;
                case "e": data.DecodeInto(out enm); break;
                case "s": isStatic = data.ToBool(); break;
                default: return false;
            }
            return true;

        }

        

        public Trigger() {
            if (enm == null)
                enm = new Dictionary<int, string>();
                isStatic = true;
        }

        public const string storyTag_Trg = "Trg";

#if PEGI
        public override bool PEGI() {
            bool changed = "static".toggle(50, ref isStatic);

            changed |= TriggerUsage.SelectUsage(ref usage);

            if (_usage.HasMoreTriggerOptions())
            {
                if (icon.Close.Click(20))
                    Trigger.editedTrigger = null;
            }

            changed |= _usage.Inspect(this).nl();

            if (_usage.HasMoreTriggerOptions())
            {
                pegi.Space();
                pegi.newLine();
            }

            return changed;
        }

        public override string NameForPEGIdisplay() => name;

        public bool PEGI_inList(IList list, int ind, ref int edited)
        {

            bool chnaged = false;

            name.write();

            if (icon.Edit.Click(20))
                Trigger.editedTrigger = this;

            return chnaged;
        }

      
#endif
    }



  

}



