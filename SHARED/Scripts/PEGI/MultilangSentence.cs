using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using System.Text;
using PlayerAndEditorGUI;
using SharedTools_Stuff;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PlayerAndEditorGUI
{

    public enum Languages { en = 1, uk = 2, tr = 3, ru = 4 }

    public static class SentenceEditorExtensions
    {

        public static string ToStringSafe(this List<Sentance> s, bool detail)
        {
            return "[" + s.Count + "]: " + (detail ? "..." : s[0].ToString());
        }
#if PEGI
        public static void PEGI(this List<Sentance> options)
        {
            pegi.newLine();
            for (int i = 0; i < options.Count; i++)
            {// Sentance s in options) {
                Sentance s = options[i];
                s.PEGI();
                if (pegi.Click("X", 35))
                    options.RemoveAt(i);
                pegi.newLine();
            }

            if (pegi.Click("Add Text"))
                options.Add(new Sentance(null));
        }
#endif
    }

    [Serializable]
    public class Sentance : Abstract_STD , IPEGI

    {

        public static bool showTexts;

        public static Languages curlang; // Don't rely on enums, use Dictionary to store languages. Key - language code, value - translation.

        public List<string> txt;

        public override StdEncoder Encode()
        {
            StdEncoder enc = new StdEncoder();
            for (int i = 0; i < txt.Count; i++)
                if (txt[i].Length > 0)
                    enc.Add_String(i.ToString(), txt[i]);
            return enc;
        }

        public override bool Decode(string subtag, string data)
        {

            int l = subtag.ToInt();
            SetTranslation((Languages)l, data);
            return true;
        }
        #if PEGI
        public virtual bool PEGI()
        {
            string tmp = ToString();
            if (pegi.editBig(ref tmp))
            {
                SetTranslation(tmp);
                return true;
            }
            return false;
        }
#endif
        public override string ToString()
        {
            int no = (int)curlang;
            while (txt.Count <= no) txt.Add("");
            return txt[no];
        }

        public Sentance()
        {
            txt = new List<string>();
        }

        public Sentance(string data)
        {
            txt = new List<string>();

            if (data != null)
                Decode(data);
        }

        public Sentance(string str, Languages len)
        {
            txt = new List<string>();
            SetTranslation(len, str);
        }

        public void SetTranslation(Languages len, string text)
        {
            int no = (int)len;
            while (txt.Count <= no) txt.Add("");
            txt[no] = text;
        }

        public void SetTranslation(string text)
        {
            int no = (int)curlang;
            while (txt.Count <= no) txt.Add("");
            txt[no] = text;
        }

        public string GetTranslation(Languages len)
        {
            int no = (int)len;
            while (txt.Count <= no) txt.Add("");
            return txt[no];
        }



    }




}


