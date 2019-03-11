using UnityEngine;
using System.Collections.Generic;

namespace PlayerAndEditorGUI
{

    public enum Msg  {Texture2D, RenderTexture, BrushType, BlitMode, editDelayed_HitEnter, InspectElement, LockToolToUseTransform, HideTransformTool,
        HighlightElement, RemoveFromList, AddNewListElement, AddEmptyListElement, ReturnToList, MakeElementNull, NameNewBeforeInstancing_1p, New };

   

    public static class LazyTranslationsExtension {
    
        static void Init() {

            texts = new List<List<string>>();

            WillBeTranslatingFrom(SystemLanguage.English);
            Msg.New.Add("New");
            Msg.NameNewBeforeInstancing_1p.Add("Name for the new {0} you'll instantiate");
            Msg.Texture2D.Add("Texture");
            Msg.RenderTexture.Add("Render Texture");
            Msg.BrushType.Add("Brush Type");
            Msg.BlitMode.Add("Blit Mode");
            Msg.editDelayed_HitEnter.Add("Press Enter to Complete Edit");
            Msg.InspectElement.Add("Inspect element");
            Msg.HighlightElement.Add("Highlight this element in the project");
            Msg.RemoveFromList.Add("Remove this list element");
            Msg.AddNewListElement.Add("Add New element to a list");
            Msg.AddEmptyListElement.Add("Add NULL/default list element");
            Msg.ReturnToList.Add("Return to list");
            Msg.MakeElementNull.Add("Null this element.");
            Msg.LockToolToUseTransform.Add("Lock texture to use transform tools. Or click 'Hide transform tool'");
            Msg.HideTransformTool.Add("Hide transform tool");

            WillBeTranslatingFrom(SystemLanguage.Ukrainian);
            Msg.New.Add("Новий");
            Msg.Texture2D.Add("Текстура");
            Msg.RenderTexture.Add("Рендер Текстура");
            Msg.BrushType.Add("Тип");
            Msg.BlitMode.Add("Метод");
            Msg.LockToolToUseTransform.Add("Постав блок на текстурі щоб рухати обєкт, або натисни на 'Приховати трансформації' щоб не мішали.");
            Msg.HideTransformTool.Add("Приховати трансформації");
            Msg.HighlightElement.Add("Показати цей елемент в проекті");
            Msg.RemoveFromList.Add("Забрати цей елемент зі списку");
            Msg.AddNewListElement.Add("Створити новий елемент у списку");

            _systemLanguage = (int)Application.systemLanguage;
        }


    // No need to modify anything below

        public static List<List<string>> texts;

#if PEGI

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
#endif

        public static string Get(this Msg s) {

            var tmp = s.GetIn(_systemLanguage);

            return tmp ?? s.GetIn((int)SystemLanguage.English);
        }

        public static string GetIn (this Msg s, int l) {
            if (texts == null)
                Init();

            if (texts.Count <= l) return null;
            var list = texts[l];

            if (list.Count <= (int) s) return null;
            var tmp = list[(int)s];
            return tmp;
        }

        private static int _systemLanguage;

        private static int _currentLanguage;

        private static void WillBeTranslatingFrom (SystemLanguage l) {
            _currentLanguage = (int)l;
            while (texts.Count <= _currentLanguage)
                texts.Add(new List<string>());
        }

        private static void Add (this Msg m, string text) {
            var line = (int)m;
            while (texts[_currentLanguage].Count <= line)
                texts[_currentLanguage].Add(null);

            texts[_currentLanguage][line] = text;
        }
    }
}
