using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace STD_Logic
{
    
    public class Trigger : ValueIndex , IGotDisplayName , IPEGI_ListInspect, IGotName {
        
        public static int focusIndex = -2;
        public static string searchField = "";
        public static int searchMatchesFound;
        public static Trigger inspected;
        public string name = "";
        public Dictionary<int, string> enm;

        private int _usage = 0;

        public TriggerUsage Usage { get { return TriggerUsage.Get(_usage); }  set { _usage = value.index; } }

        public Trigger Using() { Group.LastUsedTrigger = this;  return this; }
        
        public override bool IsBoolean => Usage.IsBoolean;
        
        public bool SearchWithGroupName(string groupName) {

            if (searchField.Length == 0 || searchField.IsSubstringOf(name)) return true; // Regex.IsMatch(name, searchField, RegexOptions.IgnoreCase)) return true;

            return (searchField.Contains(" ")) && searchField.Split(' ').All(sub => sub.IsSubstringOf(name) || sub.IsSubstringOf(groupName));
        }

        #region Encode & Decode

        public override StdEncoder Encode() => new StdEncoder()
                .Add_String("n", name)
                .Add_IfNotZero("u", _usage)
                .Add_IfNotEmpty("e", enm);
          
        public override bool Decode(string tg, string data) {

            switch (tg) {
                case "n": name = data; break;
                case "u": _usage = data.ToInt(); break;
                case "e": data.Decode_Dictionary(out enm); break;
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

        public override string NameForDisplayPEGI => name;

        public override bool PEGI_inList(IList list, int ind, ref int edited) {

            var changed = false;

            if (inspected == this) {

                if (Usage.HasMoreTriggerOptions) {
                    if (icon.Close.Click(20))
                        inspected = null;
                }

                TriggerUsage.SelectUsage(ref _usage).changes(ref changed);

                Usage.Inspect(this).nl(ref changed);

                if (Usage.HasMoreTriggerOptions)
                {
                    pegi.space();
                    pegi.nl();
                }

            }
            else
            {
                this.inspect_Name(Group.ToPegiString(), "g:{0}t:{1}".F(groupIndex,triggerIndex));

                if (icon.Edit.ClickUnFocus())
                    inspected = this;
            }
            return changed;
        }


#endif

        #endregion

    }
}



