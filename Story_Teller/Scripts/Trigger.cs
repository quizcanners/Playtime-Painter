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


namespace StoryTriggerData {


    [Serializable]
    public class Trigger : abstract_STD {

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

       

        public static void search_PEGI() {
            pegi.write("Search", 60);
            pegi.edit(ref searchField);//, GUILayout.Width(60));
            pegi.newLine();
        }

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

        public override stdEncoder Encode() {
            stdEncoder cody = new stdEncoder();
            cody.AddText("n", name);
            cody.Add("u", usage);
            cody.AddIfNotEmpty("e", enm);
            return cody;
        }

        public override void Decode(string tag, string data) {

            switch (tag) {
                case "n": name = data; break;
                case "u": usage = data.ToInt(); break;
                case "e": enm = data.ToDictionaryIntString_STD(); break;
            }

        }


        public Trigger() {
            if (enm == null)
                enm = new Dictionary<int, string>();
        }

        public const string storyTag_Trg = "Trg";

        public override string getDefaultTagName() {
            return storyTag_Trg;
        }

        public override void PEGI() {
            TriggerUsage.select_PEGI(ref usage);
        }

    }
}



