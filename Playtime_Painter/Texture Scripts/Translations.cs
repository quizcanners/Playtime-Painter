using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PlayerAndEditorGUI;

namespace Playtime_Painter
{

    // Add new lines of text to enum:

    public enum msg  {Texture2D, RenderTexture, BrushType, BlitMode};

    // Will be used as:   msg.Texture2D.Get();     - will get translation in current language.

    public static class LazyTranslationsExtensions {
    
    // Add their translations here:

        static void init() {

            texts = new List<List<string>>();

            WillBeTranslatingFrom(SystemLanguage.English);
            msg.Texture2D.Add("Texture");
            msg.RenderTexture.Add("Render Texture");
            msg.BrushType.Add("Brush Type");
            msg.BlitMode.Add("Blit Mode");


            WillBeTranslatingFrom(SystemLanguage.Ukrainian);
            msg.Texture2D.Add("Текстура");
            msg.RenderTexture.Add("Рендер Текстура");
            msg.BrushType.Add("Тип");
            msg.BlitMode.Add("Метод");


            systemLanguage = (int)Application.systemLanguage;
        }


    // No need to modify anything below

        public static List<List<string>> texts;


        public static void nl(this msg m) { m.Get().nl(); }
        public static void nl(this msg m, int width) { m.Get().nl(width); }
        public static void nl(this msg m, string tip, int width) { m.Get().nl(tip, width); }
        public static void write(this msg m) { m.Get().write(); }
        public static void write(this msg m, int width) { m.Get().write(width); }
        public static void write(this msg m, string tip, int width) { m.Get().write(tip, width); }


        public static string Get(this msg s) {

            string tmp = s.getIn(systemLanguage);

            if (tmp != null)
                return tmp;

            return s.getIn((int)SystemLanguage.English);
        }

        public static string getIn (this msg s, int l) {
            if (texts == null)
                init();

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

        static void Add (this msg m, string text) {
            int line = (int)m;
            while (texts[currentLanguage].Count <= line)
                texts[currentLanguage].Add(null);

            texts[currentLanguage][line] = text;
        }
    }
}
