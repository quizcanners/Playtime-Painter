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

    [Serializable]
    public class Sentance : AbstractKeepUnrecognized_STD, IPEGI, IPEGI_ListInspect, IGotName {

        public static Languages curlang = Languages.en; // Don't rely on enums, use Dictionary to store languages. Key - language code, value - translation.

        public Dictionary<int, string> txts = new Dictionary<int, string>();

        public static bool singleView = true;

        public string NameForPEGI { get { return this[curlang]; } set { this[curlang] = value; } }

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
            .Add("txts", txts);

        public override bool Decode(string tag, string data){
            switch (tag) {
                case "txts": data.Decode_Dictionary(out txts); break;
                default: return false;
            }
            return true;
        }
        #endregion

        #region Inspector
        #if PEGI

        public static bool LanguageSelector_PEGI() {
            return pegi.editEnum(ref curlang);
        }

        public bool PEGI_inList(IList list, int ind, ref int edited) {
            var changed = this.inspect_Name();

            if (icon.Hint.Click(curlang.ToPEGIstring()))
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
                "Translations".edit_Dictionary(ref txts);

                LanguageSelector_PEGI();
                if (!Contains() && icon.Add.Click("Add {0}".F(curlang.ToPEGIstring())))
                    NameForPEGI = this[curlang];

                pegi.nl();

            }

            return false;
        }
        #endif
        #endregion
    }




}


