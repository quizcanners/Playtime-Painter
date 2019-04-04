using System;
using UnityEngine;
using System.Collections.Generic;
using QuizCannersUtilities;

namespace PlayerAndEditorGUI {

    public enum Msg  {Texture2D, RenderTexture,  editDelayed_HitEnter, InspectElement, 
        HighlightElement, RemoveFromList, AddNewListElement, AddEmptyListElement, ReturnToList, MakeElementNull, NameNewBeforeInstancing_1p, New };
    
    public static partial class LazyTranslations {

        public static int _systemLanguage = -1;
        
        private const int eng = (int)SystemLanguage.English;
        private const int ukr = (int)SystemLanguage.Ukrainian;
        private const int trk = (int)SystemLanguage.Turkish;
        private const int rus = (int)SystemLanguage.Russian;


        private const int chn = (int)SystemLanguage.Chinese;
        private const int gmn = (int)SystemLanguage.German; 
        private const int spn = (int)SystemLanguage.Spanish;
        private const int jap = (int)SystemLanguage.Japanese;
        private const int frc = (int)SystemLanguage.French;
        private const int kor = (int)SystemLanguage.Korean;
        private const int ptg = (int)SystemLanguage.Portuguese;

        public class LazyTranslation
        {
            public string text;
            public string details;

            public LazyTranslation(string mText)
            {
                text = mText;
            }

            public LazyTranslation(string mText, string mDetails)
            {
                text = mText;
                details = mDetails;
            }

            public override string ToString() => text;
        }

        #region Inspector
        #if PEGI
        public static bool LanguageSelection() {
            if (_systemLanguage == -1)
                Init();

            "Language".editEnum<SystemLanguage>(60, ref _systemLanguage).nl();
            
            return false;
        } 

        public static bool Documentation(this LazyTranslation trnsl) {
            if (pegi.DocumentationClick(trnsl.text))
            {
                pegi.FullWindwDocumentationOpen(trnsl.details);
                return true;
            }

            return false;
        }

        public static bool WarningDocumentation(this LazyTranslation trnsl)
        {
            if (pegi.DocumentationWarningClick(trnsl.text))
            {
                pegi.FullWindwDocumentationOpen(trnsl.details);
                return true;
            }

            return false;
        }

#endif
        #endregion

        #region Translation Class
        class TranslationsEnum
        {
            public static UnNullable<Countless<LazyTranslation>> pntrTexts = new UnNullable<Countless<LazyTranslation>>();
            public static CountlessBool textInitialized = new CountlessBool();

            public bool Initialized(int index) => textInitialized[index];

            public Countless<LazyTranslation> this[int ind] => pntrTexts[ind];

            public LazyTranslation GetWhenInited(int ind, int lang)
            {

                textInitialized[ind] = true;

                var val = pntrTexts[ind][lang];

                if (val != null)
                    return val;

                val = pntrTexts[ind][eng];

                return val;
            }
        }

        static Countless<LazyTranslation> From(this Countless<LazyTranslation> sntnc, int lang, string text)
        {
            sntnc[lang] = new LazyTranslation(text);
            return sntnc;
        }

        static Countless<LazyTranslation> From(this Countless<LazyTranslation> sntnc, int lang, string text, string details)
        {
            sntnc[lang] = new LazyTranslation(text, details);
            return sntnc;
        }
        #endregion

        #region Implementation

        static void Init() => _systemLanguage = (int)Application.systemLanguage;

        public static string Get(this Msg s)
        {

            if (_systemLanguage == -1)
                Init();

            var tmp = s.GetIn(_systemLanguage);

            return tmp ?? s.GetIn((int)SystemLanguage.English);
        }

        public static string GetIn(this Msg s, int l)
        {
            var lt = s.Get(l);

            if (lt == null)  {
                coreTranslations[(int) s][l] = new LazyTranslation(s.ToString());

                lt = s.Get(l);
            }

            return lt.text;
        }

        static TranslationsEnum coreTranslations = new TranslationsEnum();

        public static LazyTranslation Get(this Msg msg, int lang)
        {

            int index = (int)msg;

            if (coreTranslations.Initialized(index))
                return coreTranslations.GetWhenInited(index, lang);

            switch (msg)
            {
                case Msg.New: msg.Translate("New").From(ukr, "Новий").From(trk, "Yeni");
                    break;

                case Msg.NameNewBeforeInstancing_1p: msg.Translate("Name for the new {0} you'll instantiate");
                    break;
                case Msg.Texture2D: msg.Translate("Texture")
                        .From(ukr, "Текстура");
                    break;
                case Msg.RenderTexture: msg.Translate("Render Texture")
                        .From(ukr, "Рендер Текстура");
                    break;
       
                case Msg.editDelayed_HitEnter:
                    msg.Translate("Press Enter to Complete Edit")
                        .From(ukr, "Натисніть Ентер щоб завершити введення");
                    break;

                case Msg.InspectElement:
                    msg.Translate("Inspect element")
                        .From(ukr, "Оглянути елемент"); ;
                    break;

                case Msg.HighlightElement:
                    msg.Translate("Highlight this element in the project")
                        .From(ukr, "Показати цей елемент в проекті");
                    break;

                case Msg.RemoveFromList:
                    msg.Translate("Remove this list element")
                        .From(ukr, "Забрати цей елемент зі списку");
                    break;

                case Msg.AddNewListElement:
                    msg.Translate("Add New element to a list")
                        .From(ukr, "Створити новий елемент у списку");
                    break;

                case Msg.AddEmptyListElement:
                    msg.Translate("Add NULL/default list element")
                        .From(ukr, "Додати порожній елемент до списку"); ;
                    break;

                case Msg.ReturnToList:
                    msg.Translate("Return to list")
                        .From(ukr, "Повернутись до списку"); ;
                    break;

                case Msg.MakeElementNull:
                    msg.Translate("Null this element.")
                        .From(ukr, "Забрати елемент зі списку");
                    break;
            }

            return coreTranslations.GetWhenInited(index, lang);
        }

        public static string GetText(this Msg msg)
        {
            var lt = msg.GetLt();
            return lt != null ? lt.ToString() : msg.ToString();
        }

        public static bool Documentation(this Msg msg) => msg.GetLt().Documentation();

        static LazyTranslation GetLt(this Msg msg)
        {
            if (_systemLanguage == -1)
                Init();

            return msg.Get(_systemLanguage);
        }

        static Countless<LazyTranslation> Translate(this Msg smg, string english)
        {
            var org = coreTranslations[(int)smg];
            org[eng] = new LazyTranslation(english);
            return org;
        }

        static Countless<LazyTranslation> Translate(this Msg smg, string english, string englishDetails)
        {
            var org = coreTranslations[(int)smg];
            org[eng] = new LazyTranslation(english, englishDetails);
            return org;
        }
        
        public static void Nl(this Msg m) { m.Get().nl(); }
        public static void Nl(this Msg m, int width) { m.Get().nl(width); }
        public static void Nl(this Msg m, string tip, int width) { m.Get().nl(tip, width); }
        public static void Write(this Msg m) { m.Get().write(); }
        public static void Write(this Msg m, int width) { m.Get().write(width); }
        public static void Write(this Msg m, string tip, int width) { m.Get().write(tip, width); }
        public static bool Click(this icon icon, Msg text) => icon.ClickUnFocus(text.Get());
        public static bool Click(this icon icon, Msg text, ref bool changed) => icon.ClickUnFocus(text.Get()).changes(ref changed);
        public static bool ClickUnFocus(this icon icon, Msg text, int size = pegi.defaultButtonSize) => pegi.ClickUnFocus(icon.GetIcon(), text.Get(), size);
        public static bool ClickUnFocus(this icon icon, Msg text, int width, int height) => pegi.ClickUnFocus(icon.GetIcon(), text.Get(), width, height);

        #endregion

    }
}
