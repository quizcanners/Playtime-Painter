using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PlayerAndEditorGUI
{

    // Add new lines of text to enum:

    public enum Msg  {Texture2D, RenderTexture, BrushType, BlitMode, editDelayed_HitEnter, InspectElement, LockToolToUseTransform, HideTransformTool,
        //LIST
        HighlightElement, RemoveFromList, AddListElement, ReturnToListView, MakeElementNull};

    // Will be used as:   msg.Texture2D.Get();     - will get translation in current language.

    public static class LazyTranslationsExtensions {
    
    // Add their translations here:

        static void Init() {

            texts = new List<List<string>>();

            WillBeTranslatingFrom(SystemLanguage.English);
            Msg.Texture2D.Add("Texture");
            Msg.RenderTexture.Add("Render Texture");
            Msg.BrushType.Add("Brush Type");
            Msg.BlitMode.Add("Blit Mode");
            Msg.editDelayed_HitEnter.Add("Press Enter to Complete Edit");
            Msg.InspectElement.Add("Inspect PEGI of this element");
            Msg.HighlightElement.Add("Highlight this element in the project");
            Msg.RemoveFromList.Add("Remove this list element");
            Msg.AddListElement.Add("Add element to a list");
            Msg.ReturnToListView.Add("Return to list view");
            Msg.MakeElementNull.Add("Null this element.");
            Msg.LockToolToUseTransform.Add("Lock texture to use transform tools. Or click 'Hide transform tool'");
            Msg.HideTransformTool.Add("Hide transform tool");

            WillBeTranslatingFrom(SystemLanguage.Ukrainian);
            Msg.Texture2D.Add("Текстура");
            Msg.RenderTexture.Add("Рендер Текстура");
            Msg.BrushType.Add("Тип");
            Msg.BlitMode.Add("Метод");
            Msg.LockToolToUseTransform.Add("Постав блок на текстурі щоб рухати обєкт, або натисни на 'Приховати трансформації' щоб не мішали.");
            Msg.HideTransformTool.Add("Приховати трансформації");
            
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
