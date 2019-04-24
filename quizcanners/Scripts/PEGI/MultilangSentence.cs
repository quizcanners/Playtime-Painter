using System.Collections;
using System.Collections.Generic;
using System;
using QuizCannersUtilities;
using UnityEngine;

namespace PlayerAndEditorGUI {

    public enum Languages { note = 0, en = 1, uk = 2, tr = 3, ru = 4 }


    public class SentenceAttribute : AbstractWithTaggedTypes
    {
        public override TaggedTypesCfg TaggedTypes => Sentence.all;
    }

    [Sentence]
    public abstract class Sentence: AbstractKeepUnrecognizedCfg, IGotClassTag, IGotName, IPEGI {

        #region Tagged Types MGMT
        public abstract string ClassTag { get; }
        
        public static TaggedTypesCfg all = new TaggedTypesCfg(typeof(Sentence));
        public TaggedTypesCfg AllTypes => all;
        #endregion

        public override string ToString() => NameForPEGI;
        
        public abstract string NameForPEGI { get; set; }

        public virtual string GetNext() => NameForPEGI; // Will update all the options inside;

        public virtual bool TimeToGetNext() => false; // For timed sentences

        public virtual string GetNextIfTime() => NameForPEGI;

    }

    [TaggedType(classTag, "String")]
    public class StringSentence : Sentence, IPEGI {

        const string classTag = "s";

        protected string text;

        public override string ClassTag => classTag;

        public override string NameForPEGI
        {
            get { return text; }
            set { text = value; }
        }
        
        #region Encode & Decode
        public override CfgEncoder Encode() => this.EncodeUnrecognized()
            .Add_String("t", text);

        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "t": text = data; break;
                default: return false;
            }
            return true;
        }
        #endregion

        #region Inspector

        #if PEGI
        public override bool Inspect()
        {
            var changed = pegi.edit(ref text).nl();

            return changed;
        }
        #endif
        #endregion

        public StringSentence()
        {

        }

        public StringSentence(string newText)
        {
            text = newText;
        }

    }

    [TaggedType(classTag, "Timed")]
    public class TimedSentence : Sentence, IPEGI, IPEGI_ListInspect {

        const string classTag = "tm";

        public override string ClassTag => classTag;

        protected Sentence text = new StringSentence();

        private float timer;

        private float timeToShow = 5f;
        
        public override string NameForPEGI
        {
            get { return text.NameForPEGI; }
            set { text.NameForPEGI = value; }
        }

        public override bool TimeToGetNext()
        {
            timer += Time.deltaTime;

            return timer > timeToShow;
        }

        public override string GetNextIfTime() => text.GetNextIfTime();

        public override string GetNext()
        {
            timer = 0;

            return text.GetNext();
        }

        #region Encode & Decode
        public override CfgEncoder Encode() => this.EncodeUnrecognized()
            .Add("sn", text, all)
            .Add("delay", timeToShow);

        public override bool Decode(string tg, string data) {
            switch (tg) {
                case "t": text.Decode(tg, data); break;
                case "sn": data.Decode(out text, all); break;
                case "delay": timeToShow = data.ToFloat(); break;
                default: return false;
            }
            return true;
        }


        #endregion

        #region Inspector
        #if PEGI

        public bool PEGI_inList(IList list, int ind, ref int edited)
        {
            var changed = false;

            "Time".write(35);
            pegi.edit(ref timeToShow, 35).changes(ref changed);
            
            text.Try_enter_Inspect(ref edited, ind).changes(ref changed);
            
            return changed;
        }

        public override bool Inspect()
        {
            var changed = pegi.selectType(ref text, all).nl();

            text.Nested_Inspect().nl(ref changed);

            return changed;
        }

#endif
        #endregion

    }

    [TaggedType(classTag, "Multi Language")]
    public class MultilanguageSentence : Sentence,  IPEGI, IPEGI_ListInspect, INeedAttention {

        const string classTag = "ml";

        public override string ClassTag => classTag;

        public override string NameForPEGI { get { return this[currentLanguage]; } set { this[currentLanguage] = value; } }
        
        #region Languages MGMT
        public static Languages currentLanguage = Languages.en;

        private static List<string> _languageCodes;

        public static List<string> LanguageCodes
        {
            get
            {
                if (_languageCodes != null) return _languageCodes;

                _languageCodes = new List<string>();
                var names = Enum.GetNames(typeof(Languages));
                var values = (int[])Enum.GetValues(typeof(Languages));
                for (var i = 0; i < values.Length; i++)
                    _languageCodes.ForceSet(values[i], names[i]);

                return _languageCodes;
            }
        }
        
        public string this[Languages lang]
        {
            get
            {
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
        
        #endregion
        
        // Change this to also use Sentence base
        public Dictionary<int, string> texts = new Dictionary<int, string>();

        bool needsReview;

        public static bool singleView = true;

        #region Encode & Decode
        public override CfgEncoder Encode() => this.EncodeUnrecognized()
            .Add("txts", texts)
            .Add_IfTrue("na", needsReview);

        public override bool Decode(string tg, string data){
            switch (tg) {
                case "t": NameForPEGI = data; break;
                case "txts": data.Decode_Dictionary(out texts); break;
                case "na": needsReview = data.ToBool(); break;
                default: return false;
            }
            return true;
        }
        #endregion

        #region Inspector
        #if PEGI

        public static bool LanguageSelector_PEGI() => pegi.editEnum(ref currentLanguage, 30);
        
        public string NeedAttention() {
            if (needsReview)
                return "Marked for review";
            return null;
        }
        
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

    [TaggedType(classTag, "Random Sentence")]
    public class RandomSentence : ListOfSentences, IPEGI {

        const string classTag = "rnd";

        public override string ClassTag => classTag;

        public override string GetNext() {
            index = UnityEngine.Random.Range(0, options.Count);
            return Valid ? Current.GetNext() : "null";
        }

        #region Inspector
        #if PEGI

        public override bool Inspect()
        {
            var changed = pegi.edit_List(ref options).nl();

            return changed;
        }

        #endif
        #endregion

    }
    
    [TaggedType(classTag, "List")]
    public class ListOfSentences : Sentence, IPEGI
    {

        const string classTag = "lst";

        public override string ClassTag => classTag;

        protected List<Sentence> options = new List<Sentence>();

        protected Sentence Current => options[index];

        protected int index = 0;

        protected bool Valid => options.Count > index;

        public override string NameForPEGI
        {
            get
            {
                return (Valid) ? Current.NameForPEGI : "NULL";
            }
            set { if (Valid) Current.NameForPEGI = value; }
        }

        public override string GetNext() {
            index = Mathf.Clamp(index+1, 0, options.Count);
            return Valid ? Current.GetNext() : "null";
        }

        public override bool TimeToGetNext() => Valid ? Current.TimeToGetNext() : false;

        public override string GetNextIfTime() {

            if (Valid && Current.TimeToGetNext())
                GetNext();

            return NameForPEGI;
        }


        #region Encode & Decode
        public override CfgEncoder Encode() => this.EncodeUnrecognized()
            .Add("txs", options, all)
            .Add("ins", inspectedSentence);

        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "txs": data.Decode_List(out options, all); break;
                case "t": options.Add(new StringSentence(data)); break;
                case "ins": inspectedSentence = data.ToInt(); break;
                default: return false;
            }
            return true;
        }
        #endregion

        #region Inspector
        private int inspectedSentence = -1;
        
        #if PEGI
        
        public override bool Inspect()
        {
            var changed = "Sentences".edit_List(ref options, ref inspectedSentence).nl();

            return changed;
        }

        #endif
        #endregion

    }


}


