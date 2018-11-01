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
    
    public class Trigger : ValueIndex , IGotDisplayName , IPEGI_ListInspect, IGotName {
        
        public static string TriggerEdControlName;
        public static string EditedtextHold;
        public static int focusIndex = -2;
        public static string searchField = "";
        public static bool filterBoolean = true;
        public static bool filterIntagers = true;
        public static bool showTriggers;
        public static int searchMatchesFound;
        public static Trigger inspected;
        

        public string name = "";
        public Dictionary<int, string> enm;
        
        public string this[int index] {
            get {
                string hold = "_";
                enm.TryGetValue(index, out hold);
                return hold;
            }
        }
        
        int usage = 0;

        public TriggerUsage _usage { get { return TriggerUsage.Get(usage); }  set { usage = value.index; } }

        public override bool IsBoolean() => _usage.UsingBool;
        
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

        #region Encode & Decode

        public override StdEncoder Encode() {
            var cody = new StdEncoder()
            .Add_String("n", name)
            .Add_ifNotZero("u", usage)
            .Add_IfNotEmpty("e", enm);
          
            return cody;
        }

        public override bool Decode(string tag, string data) {

            switch (tag) {
                case "n": name = data; break;
                case "u": usage = data.ToInt(); break;
                case "e": data.DecodeInto(out enm); break;
              //  case "s": isStatic = data.ToBool(); break;
                default: return false;
            }
            return true;
        }

        #endregion

        public Trigger() {
            if (enm == null)
                enm = new Dictionary<int, string>();
               // isStatic = true;
        }

        #region Inspector

        public string NameForPEGI { get { return name; } set { name = value; } }

#if PEGI

        public static bool Search_PEGI() => "Search".edit(60, ref searchField);

        public override string NameForPEGIdisplay => name;

        public override bool PEGI_inList(IList list, int ind, ref int edited) {

            bool changed = false;

            if (inspected == this) {

                if (_usage.HasMoreTriggerOptions) {
                    if (icon.Close.Click(20))
                        inspected = null;
                }

                changed |= TriggerUsage.SelectUsage(ref usage);

                changed |= _usage.Inspect(this).nl();

                if (_usage.HasMoreTriggerOptions)
                {
                    pegi.Space();
                    pegi.newLine();
                }

            }
            else
            {
                this.inspect_Name("g:{0}t:{1}".F(groupIndex,triggerIndex));

                if (icon.Edit.ClickUnfocus())
                    inspected = this;
            }
            return changed;
        }


#endif

        #endregion

    }
}



