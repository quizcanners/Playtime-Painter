using System;
using UnityEngine;
using System.Collections.Generic;
using QuizCannersUtilities;

namespace PlayerAndEditorGUI {

    public enum Msg  {Texture2D, RenderTexture,  EditDelayed_HitEnter, InspectElement, 
        HighlightElement, RemoveFromCollection, AddNewCollectionElement, AddEmptyCollectionElement,
        ReturnToCollection, MakeElementNull, NameNewBeforeInstancing_1p, New,
        ToolTip, ClickYesToConfirm, Yes, No, Exit, AreYouSure, ClickToInspect,
        FinishMovingCollectionElements, MoveCollectionElements, TryDuplicateSelected, TryCopyReferences,
        Init, List, Collection, Array, Dictionary
    };
    
    public static partial class LazyTranslations {

        public static LazyTranslation Get(this Msg msg, int lang)
        {

            int index = (int)msg;

            if (coreTranslations.Initialized(index))
                return coreTranslations.GetWhenInited(index, lang);

            switch (msg)
            {
                case Msg.New:
                    msg.Translate("New").From(ukr, "Новий").From(trk, "Yeni");
                    break;

                case Msg.NameNewBeforeInstancing_1p:
                    msg.Translate("Name for the new {0} you'll instantiate");
                    break;
                case Msg.Texture2D:
                    msg.Translate("Texture")
                        .From(ukr, "Текстура");
                    break;
                case Msg.RenderTexture:
                    msg.Translate("Render Texture")
                        .From(ukr, "Рендер Текстура");
                    break;

                case Msg.EditDelayed_HitEnter:
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

                case Msg.RemoveFromCollection:
                    msg.Translate("Remove element from this collection")
                        .From(ukr, "Забрати цей елемент з коллекції");
                    break;

                case Msg.AddNewCollectionElement:
                    msg.Translate("Add New element to a list")
                        .From(ukr, "Створити новий елемент у списку");
                    break;

                case Msg.AddEmptyCollectionElement:
                    msg.Translate("Add NULL/default collection element")
                        .From(ukr, "Додати порожній елемент до коллекції"); ;
                    break;

                case Msg.ReturnToCollection:
                    msg.Translate("Return to collection")
                        .From(ukr, "Повернутись до коллекції"); ;
                    break;

                case Msg.MakeElementNull:
                    msg.Translate("Null this element.")
                        .From(ukr, "Забрати елемент зі списку");
                    break;
                case Msg.ToolTip:
                    msg.Translate("ToolTip", "What is this?").From(rus, "Подсказка").From(ukr, "Підказка");
                    break;
                case Msg.ClickYesToConfirm:
                    msg.Translate("Click yes to confirm operation")
                        .From(ukr, "Натисніть Так щоб підтвердити.");
                    break;
                case Msg.No:
                    msg.Translate("NO");
                    break;
                case Msg.Yes:
                    msg.Translate("YES");
                    break;

                case Msg.AreYouSure:
                    msg.Translate("Are you sure?");
                    break;

                case Msg.ClickToInspect:
                    msg.Translate("Click to Inspect");
                    break;

                case Msg.FinishMovingCollectionElements:
                    msg.Translate("Finish moving");
                    break;
                case Msg.MoveCollectionElements:
                    msg.Translate("Organize collection elements");
                    break;

                case Msg.TryCopyReferences:
                    msg.Translate("Try Copy References");
                    break;
                case Msg.TryDuplicateSelected:
                    msg.Translate("Try duplicate selected items");
                    break;

                case Msg.Init: msg.Translate(
                    "Init"); break;
                case Msg.List: msg.Translate(
                    "List"); break;
                case Msg.Collection: msg.Translate(
                    "Collection"); break;
                case Msg.Array: msg.Translate(
                        "Array");
                    break;
                case Msg.Dictionary: msg.Translate(
                        "Dictionary");
                    break;
                    
            }

            return coreTranslations.GetWhenInited(index, lang);
        }



        public static int _systemLanguage = -1;
        
        public const int eng = (int)SystemLanguage.English;
        public const int ukr = (int)SystemLanguage.Ukrainian;
        public const int trk = (int)SystemLanguage.Turkish;
        public const int rus = (int)SystemLanguage.Russian;
        public const int chn = (int)SystemLanguage.Chinese;
        public const int gmn = (int)SystemLanguage.German;
        public const int spn = (int)SystemLanguage.Spanish;
        public const int jap = (int)SystemLanguage.Japanese;
        public const int frc = (int)SystemLanguage.French;
        public const int kor = (int)SystemLanguage.Korean;
        public const int ptg = (int)SystemLanguage.Portuguese;

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

        private static readonly List<int> supportedLanguages = new List<int>() {eng, ukr, trk};

        public static bool LanguageSelection() {
            if (_systemLanguage == -1)
                InitSystemLanguage();

            "Language".selectEnum<SystemLanguage>(60, ref _systemLanguage, supportedLanguages).nl();
            
            return false;
        } 

        public static bool DocumentationClick(this LazyTranslation trnsl) {
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
        
        #endregion

        #region Translation Class
        public class TranslationsEnum
        {
            public UnNullable<Countless<LazyTranslation>> pntrTexts = new UnNullable<Countless<LazyTranslation>>();
            public CountlessBool textInitialized = new CountlessBool();

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

        public static Countless<LazyTranslation> From(this Countless<LazyTranslation> sntnc, int lang, string text)
        {
            sntnc[lang] = new LazyTranslation(text);
            return sntnc;
        }

        public static Countless<LazyTranslation> From(this Countless<LazyTranslation> sntnc, int lang, string text, string details)
        {
            sntnc[lang] = new LazyTranslation(text, details);
            return sntnc;
        }
        #endregion
        
        #region Implementation of Extensions
        
        public static void InitSystemLanguage() => _systemLanguage = (int)Application.systemLanguage;

        static TranslationsEnum coreTranslations = new TranslationsEnum();

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

        private static LazyTranslation Get(this Msg msg)
        {
            if (_systemLanguage == -1)
                InitSystemLanguage();

            return msg.Get(_systemLanguage);
        }

        public static string GetText(this Msg s)
        {
            var lt = s.Get();
            return lt != null ? lt.ToString() : s.ToString();
        }

        public static string GetDescription(this Msg msg)
        {
            var lt = msg.Get();
            return lt != null ? lt.details : msg.ToString();
        }

        public static string GetIn(this Msg s, int l)
        {
            var lt = s.Get(l);

            if (lt == null)
            {
                coreTranslations[(int)s][l] = new LazyTranslation(s.ToString());

                lt = s.Get(l);
            }

            return lt.text;
        }
        
        static LazyTranslation GetLt(this Msg msg)
        {
            if (_systemLanguage == -1)
                InitSystemLanguage();

            return msg.Get(_systemLanguage);
        }
        
        public static string F(this Msg msg, Msg other) =>
            msg.GetText() + " " + other.GetText();
        public static bool DocumentationClick(this Msg msg) => msg.GetLt().DocumentationClick();
        public static void Nl(this Msg m) { m.GetText().nl(); }
        public static void Nl(this Msg m, int width) { m.GetText().nl(width); }
        public static void Nl(this Msg m, string tip, int width) { m.GetText().nl(tip, width); }
        public static void Write(this Msg m) { m.GetText().write(); }
        public static void Write(this Msg m, int width) { m.GetText().write(width); }
        public static void Write(this Msg m, string tip, int width) { m.GetText().write(tip, width); }
        public static bool Click(this icon icon, Msg text) => icon.ClickUnFocus(text.GetText());
        public static bool Click(this icon icon, Msg text, ref bool changed) => icon.ClickUnFocus(text.GetText()).changes(ref changed);
        public static bool ClickUnFocus(this icon icon, Msg text, int size = pegi.defaultButtonSize) => pegi.ClickUnFocus(icon.GetIcon(), text.GetText(), size);
        public static bool ClickUnFocus(this icon icon, Msg text, int width, int height) => pegi.ClickUnFocus(icon.GetIcon(), text.GetText(), width, height);

        #endregion

    }
}
