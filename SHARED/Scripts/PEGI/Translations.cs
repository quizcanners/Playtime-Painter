using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PlayerAndEditorGUI
{

    public enum Msg  {Texture2D, RenderTexture, BrushType, BlitMode, editDelayed_HitEnter, InspectElement, LockToolToUseTransform, HideTransformTool,
        HighlightElement, RemoveFromList, AddListElement, ReturnToList, MakeElementNull, NameNewBeforeInstancing_1p, New };

   

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
            Msg.AddListElement.Add("Add element to a list");
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
            Msg.AddListElement.Add("Створити новий елемент у списку");

            systemLanguage = (int)Application.systemLanguage;
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
        public static bool Click(this icon icon, Msg text) => icon.ClickUnfocus(text.Get(), pegi.defaultButtonSize);
        public static bool Click(this icon icon, Msg text, ref bool changed) => icon.ClickUnfocus(text.Get(), pegi.defaultButtonSize).changes(ref changed);
        public static bool ClickUnfocus(this icon icon, Msg text, int size = pegi.defaultButtonSize) => pegi.ClickUnfocus(icon.GetIcon(), text.Get(), size);
        public static bool ClickUnfocus(this icon icon, Msg text, int width, int height) => pegi.ClickUnfocus(icon.GetIcon(), text.Get(), width, height);
#endif

        public static string Get(this Msg s) {

            string tmp = s.GetIn(systemLanguage);

            if (tmp != null)
                return tmp;

            return s.GetIn((int)SystemLanguage.English);
        }

        public static string GetIn (this Msg s, int l) {
            if (texts == null)
                Init();

            if (texts.Count > l) {
                List<string> list = texts[l];

                if (list.Count > (int)s) {
                    string tmp = list[(int)s];
                    if (tmp != null)
                        return tmp;
                }
            }

            return null;
        }

        static int systemLanguage;

        static int currentLanguage = 0;

        static void WillBeTranslatingFrom (SystemLanguage l) {
            currentLanguage = (int)l;
            while (texts.Count <= currentLanguage)
                texts.Add(new List<string>());
        }

        static void Add (this Msg m, string text) {
            int line = (int)m;
            while (texts[currentLanguage].Count <= line)
                texts[currentLanguage].Add(null);

            texts[currentLanguage][line] = text;
        }
    }
}
