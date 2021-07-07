using System;
using System.Collections.Generic;
using QuizCanners.Utils;
using UnityEngine;


// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0011 // Add braces
#pragma warning disable IDE0008 // Use explicit type

namespace QuizCanners.Inspect
{

    #region interfaces & Attributes

    public interface IPEGI { void Inspect(); }

    public interface IPEGI_ListInspect { void InspectInList(ref int edited, int ind); }

    public interface IGotReadOnlyName { string GetNameForInspector(); }

    public interface IGotName { string NameForInspector { get; set; } }

    public interface IGotIndex { int IndexForInspector { get; set; } }

    public interface IGotCount { int GetCount(); }

    public interface ISearchable { bool IsContainsSearchWord(string searchWord); }

    public interface INeedAttention { string NeedAttention(); }

    public interface IInspectorDropdown { bool ShowInInspectorDropdown(); }

    #endregion

    public static partial class pegi
    {
        private const int PLAYTIME_GUI_WIDTH = 400;

        private static int _elementIndex;
        private static int selectedFold = -1;
        private static bool _lineOpen;
        private static readonly Color AttentionColor = new Color(1f, 0.7f, 0.7f, 1);
        private static readonly Color PreviousInspectedColor = new Color(0.3f, 0.7f, 0.3f, 1);
        private static bool _guiColorReplaced;
        private static Color _originalGuiColor;
        private static readonly List<Color> _previousBgColors = new List<Color>();


        public static bool IsFoldedOut => ef.isFoldedOutOrEntered;
        public static string EnvironmentNl => Environment.NewLine;

     
        #region GUI Modes & Fitting

        public static bool PaintingGameViewUI
        {
            get { return currentMode == PegiPaintingMode.PlayAreaGui; }
            private set { currentMode = value ? PegiPaintingMode.PlayAreaGui : PegiPaintingMode.EditorInspector; }
        }

        private enum PegiPaintingMode
        {
            EditorInspector,
            PlayAreaGui
        }

        private static PegiPaintingMode currentMode = PegiPaintingMode.EditorInspector;

        private static int letterSizeInPixels => PaintingGameViewUI ? 10 : 9;
        private static GUILayoutOption GuiMaxWidthOption => GUILayout.MaxWidth(PLAYTIME_GUI_WIDTH);
        private static GUILayoutOption GuiMaxWidthOptionFrom(string text) =>
            GUILayout.MaxWidth(Mathf.Min(PLAYTIME_GUI_WIDTH, ApproximateLength(text)));
        private static GUILayoutOption GuiMaxWidthOptionFrom(string txt, GUIStyle style) =>
            GUILayout.MaxWidth(Mathf.Min(PLAYTIME_GUI_WIDTH, ApproximateLength(txt, style.fontSize)));
        private static GUILayoutOption GuiMaxWidthOptionFrom(GUIContent cnt, PEGI_Styles.PegiGuiStyle style) =>
            GUILayout.MaxWidth(Mathf.Min(PLAYTIME_GUI_WIDTH, ApproximateLength(cnt.text, style.Current.fontSize)));

        public static int ApproximateLength(this string label, int fontSize = -1)
        {
            if (label == null || label.Length == 0)
                return 1;

            if (fontSize == -1)
                fontSize = letterSizeInPixels;
           
            int length = fontSize * label.Length;

            if (PaintingGameViewUI && length > PLAYTIME_GUI_WIDTH)
                return PLAYTIME_GUI_WIDTH;

            int count = 0;
            for (int i = 0; i < label.Length; i++)
            {
                if (char.IsUpper(label[i])) count++;
            }

            length += (int)(count * fontSize * 0.5f);

            return length;
        }
        private static int RemainingLength(int otherElements) => PaintingGameViewUI ? PLAYTIME_GUI_WIDTH - otherElements : Screen.width - otherElements;

        #endregion

        #region Inspection Variables

        #region GUI Colors

        private static icon GUIColor(this icon icn, Color col)
        {
            SetGUIColor(col);
            return icn;
        }

        private static void SetGUIColor(this Color col)
        {
            if (!_guiColorReplaced)
                _originalGuiColor = GUI.color;

            GUI.color = col;

            _guiColorReplaced = true;

        }

        private static void RestoreGUIcolor()
        {
            if (_guiColorReplaced)
                GUI.color = _originalGuiColor;

            _guiColorReplaced = false;
        }

        private static bool RestoreGUIColor(this bool val)
        {
            RestoreGUIcolor();
            return val;
        }

        #endregion

        #region BG Color

        private static bool BgColorReplaced => !_previousBgColors.IsNullOrEmpty();

        public static icon BgColor(this icon icn, Color col)
        {
            SetBgColor(col);
            return icn;
        }

        private static bool SetPreviousBgColor(this bool val)
        {
            SetPreviousBgColor();
            return val;
        }

        public static bool RestoreBGColor(this bool val)
        {
            RestoreBGColor();
            return val;
        }

        public static void SetPreviousBgColor()
        {
            if (BgColorReplaced)
            {
                GUI.backgroundColor = _previousBgColors.RemoveLast();
            }
        }

        public static void SetBgColor(Color col)
        {
            _previousBgColors.Add(GUI.backgroundColor);

            GUI.backgroundColor = col;

        }

        public static void RestoreBGColor()
        {
            if (BgColorReplaced)
                GUI.backgroundColor = _previousBgColors[0];

            _previousBgColors.Clear();
        }

#endregion

        private static void checkLine()
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                ef.checkLine();
            else
#endif
            if (!_lineOpen)
            {
               
                GUILayout.BeginHorizontal();
                _lineOpen = true;
            }
        }

        private static int LastNeedAttentionIndex;

        private static bool NeedsAttention(object el, out string msg)
        {
            msg = null;

            var need = el as INeedAttention;

            if (need == null) 
                return false;

            msg = need.NeedAttention();

            return msg != null;
        }

        public static bool NeedsAttention(System.Collections.IList list, out string message, string listName = "list", bool canBeNull = false)
        {
            message = NeedsAttention(list, listName, canBeNull);
            return message != null;
        }

        public static string NeedsAttention(System.Collections.IList list, string listName = "list", bool canBeNull = false)
        {
            string msg = null;
            if (list == null)
                msg = canBeNull ? null : "{0} is Null".F(listName);
            else
            {
                
                int i= 0;
                
                foreach (var el in list)
                {
                    if (!el.IsNullOrDestroyed_Obj())
                    {

                        if (NeedsAttention(el, out msg))
                        {
                            msg = " {0} on {1}:{2}".F(msg, i, el.GetNameForInspector());
                            LastNeedAttentionIndex = i;
                            return msg;
                        }
                    } else if (!canBeNull)
                    {
                        msg = "{0} element in {1} is NULL".F(i, listName);
                        LastNeedAttentionIndex = i;

                        return msg;
                    }
                    
                    i++;
                }
            }

            return msg;
        }

        public static string NeedsAttention<T,K>(IDictionary<T,K> dic, string listName = "dictionary", bool canBeNull = false)
        {
            string msg = null;
            if (dic == null)
                msg = canBeNull ? null : "{0} is Null".F(listName);
            else
            {

                int i = 0;

                foreach (var pair in dic)
                {
                    var value = pair.Value;

                    if (!value.IsNullOrDestroyed_Obj())
                    {

                        if (NeedsAttention(value, out msg))
                        {
                            msg = " {0} on {1}:{2}".F(msg, pair.Key, value.GetNameForInspector());
                            LastNeedAttentionIndex = i;
                            return msg;
                        }
                    }
                    else if (!canBeNull)
                    {
                        msg = "{0} element in {1} is NULL".F(pair.Key, listName);
                        LastNeedAttentionIndex = i;

                        return msg;
                    }

                    i++;
                }
            }

            return msg;
        }

        public static void space()
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                ef.Space();
            else
#endif

            {
                checkLine();
                GUILayout.Space(10);
            }
        }
        
        #endregion

        #region Focus MGMT

        private static void RepaintEditor()
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                ef.RepaintEditor();
#endif
        }

        public static void UnFocus()
        {
            #if UNITY_EDITOR
            if (!PaintingGameViewUI)
                UnityEditor.EditorGUI.FocusTextInControl("_");
            else
            #endif
                GUI.FocusControl("_");
        }

        public static bool UnFocusIfTrue(this bool anyChanges)
        {
            if (anyChanges)
                UnFocus();
            return anyChanges;
        }

        private static bool DirtyUnFocus(this bool anyChanges)
        {
            if (anyChanges)
                UnFocus();
            return anyChanges.FeedChanges_Internal();
        }

        public static void NameNextForFocus(string name) => GUI.SetNextControlName(name);

        public static string FocusedName
        {
            get { return GUI.GetNameOfFocusedControl(); }
            set { GUI.FocusControl(value); }
        }

        public static string FocusedText 
        {
            set 
            {
#if UNITY_EDITOR
                UnityEditor.EditorGUI.FocusTextInControl(value);
#endif
            }
        }

        #endregion

#region Pop UP Services
        
        public static class FullWindow
        {
            public const string DISCORD_SERVER = "https://discord.gg/rF7yXq3";
            public const string SUPPORT_EMAIL = "quizcanners@gmail.com";

            internal static string popUpHeader = "";
            internal static string popUpText = "";
            internal static string relatedLink = "";
            internal static string relatedLinkName = "";
            internal static Func<bool> inspectDocumentationDelegate;
            internal static Action areYouSureFunk;


            private static object _popUpTarget;
            private static string _understoodPopUpText = "Got it";
            private static readonly List<string> _gotItTexts = new List<string>
            {
                "I understand",
                "Clear as day",
                "Roger that",
                "Without a shadow of a doubt",
                "Couldn't be more clear",
                "Totally got it",
                "Well said",
                "Perfect explanation",
                "Thanks",
                "Take me back",
                "Reading Done",
                "Thanks",
                "Affirmative",
                "Comprehended",
                "Grasped",
                "I have learned something",
                "Acknowledged",
                "I see",
                "I get it",
                "I take it as read",
                "Point taken",
                "I infer",
                "Clear message",
                "This was useful",
                "A comprehensive explanation",
                "I have my answer",
                "How do I close this?",
                "Now I want to know something else",
                "Can I close this Pop Up now?",
                "I would like to see previous screen please",
                "This is what I wanted to know",
                "Now I can continue"



            };
            private static readonly List<string> _gotItTextsWeird = new List<string>
            {
                "Nice, this is easier then opening a documentation",
                "So convenient, thanks!",
                "Cool, no need to browse Documentation!",
                "Wish getting answers were always this easy",
                "It is nice to have tooltips like this",
                "I wonder how many texts are here",
                "Did someone had nothing to do to write this texts?",
                "This texts are random every time, aren't they?",
                "Why not make this just OK button"
            };
            private static int _textsShown;

            internal static void InitiatePopUp()
            {
                _popUpTarget = ef.inspectedTarget;
                
                switch (_textsShown)
                {
                    case 0: _understoodPopUpText = "OK"; break;
                    case 1: _understoodPopUpText = "Got it!"; break;
                    case 666: _understoodPopUpText = "By clicking I confirm to selling my kidney"; break;
                    default: _understoodPopUpText = (_textsShown < 20 ? _gotItTexts : _gotItTextsWeird).GetRandom(); break;
                }

                _textsShown++;
            }

            internal static void ClosePopUp()
            {
                popUpText = null;
                relatedLink = null;
                relatedLinkName = null;
                inspectDocumentationDelegate = null;
                areYouSureFunk = null;
            }

            #region Documentation Click Open 

    
            public static void AreYouSureOpen(Action action, string header = "",  string text = "")
            {
                if (header.IsNullOrEmpty())
                    header = Msg.AreYouSure.GetText();

                if (text.IsNullOrEmpty())
                    text = Msg.ClickYesToConfirm.GetText();

                areYouSureFunk = action;
                popUpText = text;
                popUpHeader = header;
                InitiatePopUp();
            }
            public static bool DocumentationWarningClickOpen(string text, string toolTip, int buttonSize = 20)
            {
                if (DocumentationClickInternal(toolTip, buttonSize: buttonSize, icon.Warning)) 
                {
                    popUpText = text;
                    InitiatePopUp();
                    return true;
                }
                return false;
            }
            public static bool WarningDocumentationClickOpen(Func<string> text, string toolTip = "What is this?",
                int buttonSize = 20) => DocumentationClickOpen(text, toolTip, buttonSize, icon.Warning);
            public static bool WarningDocumentationClickOpen(string text, string toolTip = "What is this?",
                int buttonSize = 20) => DocumentationClickOpen(text, toolTip, buttonSize, icon.Warning);
            public static bool DocumentationClickOpen(Func<bool> inspectFunction, string toolTip = "", int buttonSize = 20, icon clickIcon = icon.Question)
            {
                if (toolTip.IsNullOrEmpty())
                    toolTip = clickIcon.GetDescription();

                if (DocumentationClickInternal(toolTip, buttonSize))
                {
                    inspectDocumentationDelegate = inspectFunction;
                    InitiatePopUp();
                    return true;
                }

                return false;
            }
            public static bool DocumentationClickOpen(Func<string> text, string toolTip = "", int buttonSize = 20, icon clickIcon = icon.Question)
            {

                bool gotHeadline = false;

                if (toolTip.IsNullOrEmpty())
                    toolTip = Msg.ToolTip.GetDescription();
                else gotHeadline = true;

                if (DocumentationClickInternal(toolTip, buttonSize, clickIcon))
                {
                    popUpText = text();
                    popUpHeader = gotHeadline ? toolTip : "";
                    InitiatePopUp();
                    return true;
                }

                return false;
            }
            public static bool DocumentationClickOpen(string text, string toolTip = "", int buttonSize = 20, icon clickIcon = icon.Question)
            {

                bool gotHeadline = false;

                if (toolTip.IsNullOrEmpty())
                    toolTip = Msg.ToolTip.GetDescription();
                else gotHeadline = true;

                if (DocumentationClickInternal(toolTip, buttonSize, clickIcon))
                {
                    popUpText = text;
                    popUpHeader = gotHeadline ? toolTip : "";
                    InitiatePopUp();
                    return true;
                }

                return false;
            }
            public static bool DocumentationWithLinkClickOpen(string text, string link, string linkName = null, string tip = "", int buttonSize = 20)
            {
                if (tip.IsNullOrEmpty())
                    tip = icon.Question.GetDescription();

                if (DocumentationClickInternal(tip, buttonSize))
                {
                    popUpText = text;
                    InitiatePopUp();
                    relatedLink = link;
                    relatedLinkName = linkName.IsNullOrEmpty() ? link : linkName;
                    return true;
                }

                return false;
            }
            private static bool DocumentationClickInternal(string toolTip = "", int buttonSize = 20, icon clickIcon = icon.Question)
            {
                if (toolTip.IsNullOrEmpty())
                    toolTip = icon.Question.GetDescription();

                return clickIcon.BgColor(Color.clear).Click(toolTip, buttonSize).SetPreviousBgColor();
            }

            #endregion

            #region Elements
            public static bool ShowingPopup()
            {

                if (_popUpTarget == null || _popUpTarget != ef.inspectedTarget)
                    return false;

                if (areYouSureFunk != null)
                {

                    if (icon.Close.Click(Msg.No.GetText(), 35))
                        ClosePopUp();

                    WriteHeaderIfAny();

                    if (icon.Done.Click(Msg.Yes.GetText(), 35))
                    {
                        try
                        {
                            areYouSureFunk();
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                        }
                        ClosePopUp();
                    }


                    nl();

                    popUpText.writeBig();

                    return true;

                }

                if (inspectDocumentationDelegate != null)
                {

                    if (icon.Back.Click(Msg.Exit))
                        ClosePopUp();
                    else
                    {
                        WriteHeaderIfAny().nl();

                        inspectDocumentationDelegate();

                        ContactOptions();
                    }

                    return true;
                }

                if (!popUpText.IsNullOrEmpty())
                {

                    WriteHeaderIfAny().nl();

                    popUpText.writeBig("Click the blue text below to close this toolTip. This is basically a toolTip for a toolTip. It is the world we are living in now.");

                    if (!relatedLink.IsNullOrEmpty() && relatedLinkName.ClickText(14))
                        Application.OpenURL(relatedLink);

                    ConfirmLabel();
                    return true;
                }



                return false;
            }
            private static void ContactOptions()
            {
                nl();
                "Didn't get the answer you need?".write();
                if (icon.Discord.Click())
                    Application.OpenURL(DISCORD_SERVER);
                if (icon.Email.Click())
                    QcUnity.SendEmail(SUPPORT_EMAIL, "About this hint",
                        "The toolTip:{0}***{0} {1} {0}***{0} haven't answered some of the questions I had on my mind. Specifically: {0}".F(EnvironmentNl, popUpText));

            }
            private static void ConfirmLabel()
            {
                nl();

                if (_understoodPopUpText.ClickText(15).nl())
                    ClosePopUp();

                ContactOptions();
            }

            private static bool WriteHeaderIfAny()
            {
                if (!popUpHeader.IsNullOrEmpty())
                {
                    popUpHeader.write(PEGI_Styles.ListLabel);
                    return true;
                }

                return false;
            }

            #endregion
        }

#endregion

#region New Line

        private static int IndentLevel
        {
            get
            {
#if UNITY_EDITOR
                if (!PaintingGameViewUI)
                    return UnityEditor.EditorGUI.indentLevel;
#endif

                return 0;
            }

            set
            {
#if UNITY_EDITOR
                if (!PaintingGameViewUI)
                    UnityEditor.EditorGUI.indentLevel = Mathf.Max(0, value);
#endif
            }
        }

        public static void UnIndent(int width = 1)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                ef.UnIndent(width);
            }
#endif

        }

        public static void Indent(int width = 1)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                ef.Indent(width);
            }
#endif

        }

        public static void nl()
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                ef.newLine();
                return;
            }
#endif

            if (_lineOpen)
            {
                _lineOpen = false;
                GUILayout.EndHorizontal();
            }
        }
        
        public static void nl_ifFolded() => ef.isFoldedOutOrEntered.nl_ifFalse();

        public static void nl_ifFoldedOut() => ef.isFoldedOutOrEntered.nl_ifTrue();

        public static void nl_ifNotEntered() => ef.isFoldedOutOrEntered.nl_ifFalse();

        public static void nl_ifEntered() => ef.isFoldedOutOrEntered.nl_ifTrue();

        public static bool nl_ifFolded(this bool value)
        {
            nl_ifFolded();
            return value;
        }

        public static bool nl_ifFoldedOut(this bool value)
        {
            nl_ifFoldedOut();
            return value;
        }

        public static bool nl_ifNotEntered(this bool value)
        {
            nl_ifNotEntered();
            return value;
        }

        public static bool nl_ifEntered(this bool value)
        {
            nl_ifEntered();
            return value;
        }

        private static bool nl_ifTrue(this bool value)
        {
            if (value)
                nl();
            return value;
        }

        private static bool nl_ifFalse(this bool value)
        {
            if (!value)
                nl();
            return value;
        }

        public static bool nl(this bool value)
        {
            nl();
            return value;
        }

        private static bool nl(this bool value, ref bool changed)
        {
            changed |= value;

            return value.nl();
        }

        public static void nl(this string value)
        {
            write(value);
            nl();
        }

        public static void nl(this string value, string tip)
        {
            write(value, tip);
            nl();
        }

        public static void nl(this string value, int width)
        {
            write(value, width);
            nl();
        }

        public static void nl(this string value, string tip, int width)
        {
            write(value, tip, width);
            nl();
        }

        public static void nl(this string value, PEGI_Styles.PegiGuiStyle style)
        {
            write(value, style);
            nl();
        }

        public static void nl(this icon icon, int size = defaultButtonSize)
        {
            icon.draw(size);
            nl();
        }

        public static void nl(this icon icon, string hint, int size = defaultButtonSize)
        {
            icon.draw(hint, size);
            nl();
        }


#endregion

    }
}
