using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using QuizCanners.Utils;
using UnityEngine;

namespace QuizCanners.Inspect {

    // ReSharper disable InconsistentNaming
    #pragma warning disable IDE1006 // Naming Styles

    public enum icon
    {
        Alpha, Active, Add, Animation, Audio,
        Back, Book,
        Close, Condition, Config, Copy, Cut, Create, Clear, CPU, GPU,
        Discord, Delete, Done, Docs, Download, Down, DownLast, Debug, DeSelectAll, Dice,
        Edit, Enter, Exit, Email, Empty,
        False, FoldedOut, Folder,
        NewMaterial, NewTexture, Next,
        On,
        Off,
        Lock, Unlock, List, Link, UnLinked,
        Round, Record, Replace, Refresh,
        Search, Script, Square, Save, SaveAsNew, StateMachine, State, Show, SelectAll, Share, Size, ScreenGrab,
        Question,
        Painter,
        PreviewShader,
        OriginalShader,
        Undo,
        Redo,
        UndoDisabled,
        RedoDisabled,
        Play,
        True,
        Load,
        Pause, Ping,
        Mesh,
        Move,
        Red,
        Green,
        Blue,
        InActive,
        Insert,
        Hint,
        Home,
        Hide,
        Paste,
        Up, UpLast, User,
        Warning,
        Wait
    }
    #pragma warning restore IDE1006 // Naming Styles

    public static class Icons_MGMT {

        const string FOLDER_NAME = "Inspector Icons";

        private static readonly Countless<Texture2D> _managementIcons = new Countless<Texture2D>();

        public static Texture2D GetIcon(this icon icon)
        {

            var ind = (int) icon;

            var ico = _managementIcons[ind];

            if (ico)
                return ico;

            switch (icon) {
                case icon.Red: return ColorIcon(0) as Texture2D;
                case icon.Green: return ColorIcon(1) as Texture2D;
                case icon.Blue: return ColorIcon(2) as Texture2D;
                case icon.Alpha: return ColorIcon(3) as Texture2D;
                default:
                    var tmp = Resources.Load(Path.Combine(FOLDER_NAME, Enum.GetName(typeof(icon), ind))) as Texture2D;

                    _managementIcons[ind] = tmp ? tmp : Texture2D.whiteTexture;

                    return tmp;
            }
        }

        private static List<Texture2D> _painterIcons;

        private static Texture ColorIcon(int ind)
        {
            if (_painterIcons == null) _painterIcons = new List<Texture2D>();

            while (_painterIcons.Count <= ind) _painterIcons.Add(null);

            if (_painterIcons[ind] != null) return (_painterIcons[ind]);
            switch (ind)
            {
                case 0:
                    _painterIcons[ind] = Resources.Load(Path.Combine(FOLDER_NAME, "Red")) as Texture2D;
                    break;
                case 1:
                    _painterIcons[ind] = Resources.Load(Path.Combine(FOLDER_NAME, "Green")) as Texture2D;
                    break;
                case 2:
                    _painterIcons[ind] = Resources.Load(Path.Combine(FOLDER_NAME, "Blue")) as Texture2D;
                    break;
                case 3:
                    _painterIcons[ind] = Resources.Load(Path.Combine(FOLDER_NAME, "Alpha")) as Texture2D;
                    break;
            }

            return (_painterIcons[ind]);
        }

        public static Texture GetIcon(this ColorChanel icon) => ColorIcon((int) icon);

        public static Texture GetIcon(this ColorMask icon) => icon.ToColorChannel().GetIcon();
        
 
    }
    
    public static partial class LazyLocalization {

        static TranslationsEnum iconTranslations = new TranslationsEnum();

        public static LazyTranslation Get(this icon msg, int lang = 0) {

            int index = (int)msg;

            if (iconTranslations.Initialized(index))
                return iconTranslations.GetWhenInited(index, lang);

            switch (msg) {

                case icon.Add:
                    msg.Translate("Add");

                break;
                case icon.Enter:
                    msg.Translate("Enter", "Click to enter");
                    break;
                case icon.Exit:
                    msg.Translate("Exit", "Click to exit");
                    break;

                case icon.Empty:
                    msg.Translate("Empty");
                    break;

                case icon.SelectAll:
                    msg.Translate("Select All");
                    break;

                case icon.DeSelectAll:
                    msg.Translate("Deselect All");
                    break;
                case icon.Search:
                    msg.Translate("Serch");
                    break;
                case icon.Show:
                    msg.Translate("Show");
                    break;
                case icon.Hide:
                    msg.Translate("Hide");
                    break;
                case icon.Question:
                    msg.Translate("Question", "What is this?");
                    break;
            }

            return iconTranslations.GetWhenInited(index, lang);
        }
        
        public static string GetText(this icon msg)
        {
            var lt = msg.Get();
            return lt != null ? lt.ToString() : msg.ToString();
        }

        public static string GetDescription(this icon msg)
        {
            var lt = msg.Get();
            return lt != null ? lt.details : msg.ToString();
        }

        static Countless<LazyTranslation> Translate(this icon smg, string english)
        {
            var org = iconTranslations[(int)smg];
            org[eng] = new LazyTranslation(english);
            return org;
        }

        static Countless<LazyTranslation> Translate(this icon smg, string english, string englishDetails)
        {
            var org = iconTranslations[(int)smg];
            org[eng] = new LazyTranslation(english, englishDetails);
            return org;
        }
        
        public static string F(this icon msg, Msg other) =>  "{0} {1}".F(msg.GetText(), other.GetText());

        public static string F(this Msg msg, icon other) => "{0} {1}".F(msg.GetText(), other.GetText());
     
    }


}