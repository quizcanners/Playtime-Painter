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


    [Serializable]
    public class Trigger : Abstract_STD
#if PEGI
            , IPEGI, IGotDisplayName 
#endif
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

        public static Trigger edited;

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

        public TriggerUsage _usage { get { return TriggerUsage.get(usage); }  set { usage = value.index; } }


        #if PEGI
        public static void search_PEGI() {
            pegi.write("Search", 60);
            pegi.edit(ref searchField);//, GUILayout.Width(60));
            pegi.newLine();
        }
#endif
        public bool SearchCompare(string groupName) {
            
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
        public virtual bool PEGI() {
            return TriggerUsage.selectUsage(ref usage);
        }

        public string NameForPEGIdisplay() => name;
#endif
    }
}



