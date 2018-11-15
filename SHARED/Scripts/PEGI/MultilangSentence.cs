using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using System.Text;
using PlayerAndEditorGUI;
using SharedTools_Stuff;
using STD_Logic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PlayerAndEditorGUI {

    public enum Languages { note = 0, en = 1, uk = 2, tr = 3, ru = 4 }

    [Serializable]
    [DerrivedList(typeof(Sentance), typeof(ConditionalSentance))]
    public class Sentance : AbstractKeepUnrecognized_STD, IPEGI, IPEGI_ListInspect, IGotName, INeedAttention {

        public static Languages curlang = Languages.en; // Don't rely on enums, use Dictionary to store languages. Key - language code, value - translation.

        List<string> lanCodes;

        public List<string> LanguageCodes { get {
                if (lanCodes == null) {
                    lanCodes = new List<string>();
                    string[] names = Enum.GetNames(typeof(Languages));
                    var values = (int[])Enum.GetValues(typeof(Languages));
                    for (int i = 0; i < values.Length; i++)
                        lanCodes.ForceSet(values[i], names[i]);
                }

                return lanCodes;
            }
        }

        public Dictionary<int, string> txts = new Dictionary<int, string>();

        bool needsReview = false;

        public static bool singleView = true;

        public string NameForPEGI { get { return this[curlang]; } set { this[curlang] = value; } }

        public override string ToString() => NameForPEGI;

        public string this[Languages lang] {
            get {
                string text;
                int ind = (int)lang;

                if (txts.TryGetValue(ind, out text))
                    return text;
                else
                {
                    if (lang == Languages.en)
                    {
                        text = "English Text";
                        txts[ind] = text;
                    }
                    else
                        text = this[Languages.en];
                }

                return text;
            }
            set { txts[(int)lang] = value; }
        }

        public bool Contains(Languages lang) => txts.ContainsKey((int)lang);

        public bool Contains() => Contains(curlang);

        #region Encode & Decode
        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add("txts", txts)
            .Add_IfTrue("na", needsReview);

        public override bool Decode(string tag, string data){
            switch (tag) {
                case "txts": data.Decode_Dictionary(out txts); break;
                case "na": needsReview = data.ToBool(); break;
                default: return false;
            }
            return true;
        }
        #endregion

        #region Inspector
        #if PEGI

        public string NeedAttention() {
            if (needsReview)
                return "Marked for review";
            return null;
        }

        public static bool LanguageSelector_PEGI() => pegi.editEnum(ref curlang, 30);
        
        public virtual bool PEGI_inList(IList list, int ind, ref int edited) {
            var changed = this.inspect_Name();

            if (this.Attention_Or_Click(icon.Hint, curlang.ToPEGIstring()))
                edited = ind;
            return changed;
        }

        public override bool Inspect() {
            string tmp = NameForPEGI;

            "Show All".toggleIcon(ref singleView);
            if (singleView)  {
                LanguageSelector_PEGI();
                if (pegi.editBig(ref tmp)) {
                    NameForPEGI = tmp;
                    return true;
                }
            } else {

                "Translations".edit_Dictionary_Values(ref txts, LanguageCodes);

                LanguageSelector_PEGI();
                if (!Contains() && icon.Add.Click("Add {0}".F(curlang.ToPEGIstring())))
                    NameForPEGI = this[curlang];

                pegi.nl();
            }

            "Mark for review".toggleIcon(ref needsReview, "NEEDS REVIEW");
          

            return false;
        }

      
#endif
        #endregion
    }

    public class ConditionalSentance : Sentance, IAmConditional {

        ConditionBranch condition = new ConditionBranch();

        public bool CheckConditions(Values vals) => condition.CheckConditions(vals);

        #region Inspector
#if PEGI
        public override bool PEGI_inList(IList list, int ind, ref int edited) {
            var changed = this.inspect_Name();
            if (this.Attention_Or_Click(condition.IsTrue() ? icon.Active : icon.InActive, curlang.ToPEGIstring()))
                edited = ind;
            return changed;
        }

        public override bool Inspect() {
            var changes = condition.Nested_Inspect().nl();
            changes |= base.Inspect();
            return changes;
        }
#endif
#endregion

        #region Encode & Decode
        public override StdEncoder Encode() => new StdEncoder()
                .Add("b", base.Encode)
                .Add_IfNotDefault("cnd", condition);
         
        public override bool Decode(string tag, string data)
        {
            switch (tag)
            {
                case "b": data.DecodeInto(base.Decode); break;
                case "cnd": condition.Decode(data); break;
                default: return false;
            }
            return true;
        }
        #endregion

    }


    public static class MultilanguageSentanceExtensions
    {

        public static Sentance GetNextText (this List<Sentance> list, ref int startIndex) {

            while (list.Count > startIndex) {
                var txt = list[startIndex];

                if (!txt.TryTestCondition())
                    startIndex++;
                else
                    return txt;
            }
            return null;
        }

    }

}


