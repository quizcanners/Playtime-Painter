using System.Collections;
using System.Collections.Generic;
using System;
using QuizCannersUtilities;
using STD_Logic;

namespace PlayerAndEditorGUI {

    public enum Languages { note = 0, en = 1, uk = 2, tr = 3, ru = 4 }
    
    [DerivedList(typeof(Sentence), typeof(ConditionalSentence))]
    public class Sentence : AbstractKeepUnrecognizedCfg, IPEGI, IPEGI_ListInspect, IGotName, INeedAttention {

        public static Languages currentLanguage = Languages.en; // Don't rely on enums, use Dictionary to store languages. Key - language code, value - translation.

        List<string> _languageCodes;

        public List<string> LanguageCodes { get {
                if (_languageCodes != null) return _languageCodes;

                _languageCodes = new List<string>();
                var names = Enum.GetNames(typeof(Languages));
                var values = (int[])Enum.GetValues(typeof(Languages));
                for (var i = 0; i < values.Length; i++)
                    _languageCodes.ForceSet(values[i], names[i]);

                return _languageCodes;
            }
        }

        public Dictionary<int, string> texts = new Dictionary<int, string>();

        bool needsReview;

        public static bool singleView = true;

        public string NameForPEGI { get { return this[currentLanguage]; } set { this[currentLanguage] = value; } }

        public override string ToString() => NameForPEGI;

        public string this[Languages lang] {
            get {
                string text;
                var ind = (int)lang;

                if (texts.TryGetValue(ind, out text))
                    return text;
                else
                {
                    if (lang == Languages.en)
                    {
                        text = "English Text";
                        texts[ind] = text;
                    }
                    else
                        text = this[Languages.en];
                }

                return text;
            }
            set { texts[(int)lang] = value; }
        }

        public bool Contains(Languages lang) => texts.ContainsKey((int)lang);

        public bool Contains() => Contains(currentLanguage);

        #region Encode & Decode
        public override CfgEncoder Encode() => this.EncodeUnrecognized()
            .Add("txts", texts)
            .Add_IfTrue("na", needsReview);

        public override bool Decode(string tg, string data){
            switch (tg) {
                case "txts": data.Decode_Dictionary(out texts); break;
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

        public static bool LanguageSelector_PEGI() => pegi.editEnum(ref currentLanguage, 30);
        
        public virtual bool PEGI_inList(IList list, int ind, ref int edited) {
            var changed = this.inspect_Name();

            if (this.Click_Enter_Attention(icon.Hint, currentLanguage.ToPegiString()))
                edited = ind;
            return changed;
        }

        public override bool Inspect() {
            string tmp = NameForPEGI;

            "Show only one language".toggleIcon(ref singleView);
            if (singleView)  {
                LanguageSelector_PEGI();
                if (pegi.editBig(ref tmp)) {
                    NameForPEGI = tmp;
                    return true;
                }
            } else {

                "Translations".edit_Dictionary_Values(ref texts, LanguageCodes);

                LanguageSelector_PEGI();
                if (!Contains() && icon.Add.Click("Add {0}".F(currentLanguage.ToPegiString())))
                    NameForPEGI = this[currentLanguage];

                pegi.nl();
            }

            "Mark for review".toggleIcon(ref needsReview, "NEEDS REVIEW");
          

            return false;
        }

      
#endif
        #endregion
    }

    public class ConditionalSentence : Sentence, IAmConditional {

        readonly ConditionBranch _condition = new ConditionBranch();

        public bool CheckConditions(Values values) => _condition.CheckConditions(values);

        #region Inspector
#if PEGI
        public override bool PEGI_inList(IList list, int ind, ref int edited) {
            var changed = this.inspect_Name();
            if (this.Click_Enter_Attention(_condition.IsTrue() ? icon.Active : icon.InActive, currentLanguage.ToPegiString()))
                edited = ind;
            return changed;
        }

        public override bool Inspect() {
            var changes = _condition.Nested_Inspect().nl();
            changes |= base.Inspect();
            return changes;
        }
#endif
#endregion

        #region Encode & Decode
        public override CfgEncoder Encode() => new CfgEncoder()
                .Add("b", base.Encode)
                .Add_IfNotDefault("cnd", _condition);
         
        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "b": data.Decode_Base(base.Decode, this); break;
                case "cnd": _condition.Decode(data); break;
                default: return false;
            }
            return true;
        }
        #endregion

    }


    public static class MultiLanguageSentenceExtensions
    {

        public static Sentence GetNextText (this List<Sentence> list, ref int startIndex) {

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


