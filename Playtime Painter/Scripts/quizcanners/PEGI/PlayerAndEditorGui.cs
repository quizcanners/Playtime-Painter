using System;
using System.Collections;
using System.Collections.Generic;
using QuizCannersUtilities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0011 // Add braces
#pragma warning disable IDE0008 // Use explicit type
#pragma warning disable IDE0009 // Member access should be qualified.

namespace PlayerAndEditorGUI
{

    #region interfaces & Attributes

    public interface IPEGI { bool Inspect(); }

    public interface IPEGI_ListInspect { bool InspectInList(IList list, int ind, ref int edited); }

    public interface IPEGI_Searchable { bool String_SearchMatch(string searchString); }

    public interface INeedAttention { string NeedAttention(); }

    public interface IGotName { string NameForPEGI { get; set; } }

    public interface IGotDisplayName { string NameForDisplayPEGI(); }

    public interface IGotIndex { int IndexForPEGI { get; set; } }

    public interface IGotCount { int CountForInspector(); }

    public interface IEditorDropdown { bool ShowInDropdown(); }

    public interface IPegiReleaseGuiManager
    {
        void Inspect();
        void Write(string label);
        bool Click(string label);
    }


    #endregion

    public static partial class pegi
    {

        public static bool IsFoldedOut => ef.isFoldedOutOrEntered;

        public static string EnvironmentNl => Environment.NewLine;

        public static class GameView
        {

            private static Type gameViewType;

            public static void ShowNotification(string text)
            {
#if UNITY_EDITOR

                if (Application.isPlaying)
                {
                    if (gameViewType == null)
                        gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");

                    var ed = EditorWindow.GetWindow(gameViewType);
                    if (ed != null)
                        ed.ShowNotification(new GUIContent(text));
                }
                else
                {
                    var lst = Resources.FindObjectsOfTypeAll<SceneView>();

                    foreach (var w in lst)
                        w.ShowNotification(new GUIContent(text));

                }
#endif
            }

            private static int mouseOverUi = -1;

            public static bool MouseOverUI
            {
                get { return mouseOverUi >= Time.frameCount - 1; }
                set
                {
                    if (value) mouseOverUi = Time.frameCount;
                }
            }

            public delegate bool WindowFunction();

            public class Window
            {
                private WindowFunction _function;
                private Rect _windowRect;
                public float upscale = 2;

                protected bool UseWindow => upscale == 1;

                private void DrawFunctionWrapper(int windowID)
                {

                    PaintingGameViewUI = true;
                    ef.globChanged = false;
                    _elementIndex = 0;
                    _lineOpen = false;
                    focusInd = 0;

                    try
                    {
                        if (!UseWindow)
                        {

                            GUI.matrix = Matrix4x4.TRS(new Vector3(0, 0, 0), Quaternion.identity,
                                new Vector3(upscale, upscale, 1));
                            GUILayout.BeginArea(new Rect(40 / upscale, 20 / upscale, Screen.width / upscale,
                                Screen.height / upscale));
                        }

                        if (!PopUpService.ShowingPopup())
                            _function();

                        nl();

                        UnIndent();

                        (GUI.tooltip.IsNullOrEmpty() ? "" : "{0}:{1}".F(Msg.ToolTip.GetText(), GUI.tooltip)).nl(
                            PEGI_Styles.HintText);

                        if (UseWindow)
                        {
                            if (_windowRect.Contains(Input.mousePosition))
                                MouseOverUI = true;

                            GUI.DragWindow(new Rect(0, 0, 3000, 40 * upscale));
                        }
                        else
                        {
                            MouseOverUI = true;
                            GUILayout.EndArea();
                        }

                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(ex);
                    }

                    PaintingGameViewUI = false;
                }

                public void Render(IPEGI p) => Render(p, p.Inspect, p.GetNameForInspector());

                public void Render(IPEGI p, string windowName) => Render(p, p.Inspect, windowName);

                public void Render(IPEGI target, WindowFunction doWindow, string c_windowName)
                {

                    ef.ResetInspectionTarget(target);

                    _function = doWindow;

                    if (UseWindow)
                    {
                        _windowRect.x = Mathf.Clamp(_windowRect.x, 0, Screen.width - 10);
                        _windowRect.y = Mathf.Clamp(_windowRect.y, 0, Screen.height - 10);

                        _windowRect = GUILayout.Window(0, _windowRect, DrawFunctionWrapper, c_windowName,
                            GUILayout.MaxWidth(360 * upscale), GUILayout.ExpandWidth(true));
                    }
                    else
                    {
                        DrawFunctionWrapper(0);
                    }

                }

                public void Collapse()
                {
                    _windowRect.width = 250;
                    _windowRect.height = 350;
                    _windowRect.x = 20;
                    _windowRect.y = 50;
                }

                public Window(float upscale = 1)
                {
                    this.upscale = upscale;
                    _windowRect = new Rect(20, 50, 350 * upscale, 400 * upscale);
                }
            }

            public static float AspectRatio
            {
                get
                {
                    var res = Resolution;
                    return res.x / res.y;
                }
            }

            public static int Width => (int) Resolution.x;

            public static int Height => (int) Resolution.y;

            public static Vector2 Resolution
            {
                get
                {
#if UNITY_EDITOR
                    return Handles.GetMainGameViewSize();
#else
                    return new Vector2(Screen.width, Screen.height);
#endif
                }
            }
        }

        #region GUI Modes & Fitting

        private enum PegiPaintingMode
        {
            EditorInspector,
            PlayAreaGui
        }

        private static PegiPaintingMode currentMode = PegiPaintingMode.EditorInspector;

        public static bool PaintingGameViewUI
        {
            get { return currentMode == PegiPaintingMode.PlayAreaGui; }
            private set { currentMode = value ? PegiPaintingMode.PlayAreaGui : PegiPaintingMode.EditorInspector; }
        }

        private static int _playtimeGuiWidth = 400;

        private static GUILayoutOption GuiMaxWidthOption => GUILayout.MaxWidth(_playtimeGuiWidth);

        private static GUILayoutOption GuiMaxWidthOptionFrom(string text) =>
            GUILayout.MaxWidth(Mathf.Min(_playtimeGuiWidth, ApproximateLength(text)));

        private static GUILayoutOption GuiMaxWidthOptionFrom(string txt, GUIStyle style) =>
            GUILayout.MaxWidth(Mathf.Min(_playtimeGuiWidth, ApproximateLength(txt, style.fontSize)));

        private static GUILayoutOption GuiMaxWidthOptionFrom(GUIContent cnt, PEGI_Styles.PegiGuiStyle style) =>
            GUILayout.MaxWidth(Mathf.Min(_playtimeGuiWidth, ApproximateLength(cnt.text, style.Current.fontSize)));

        private static int letterSizeInPixels => PaintingGameViewUI ? 10 : 9;

    public static int ApproximateLength(this string label, int fontSize = -1)
        {
            if (label == null || label.Length == 0)
                return 1;

            if (fontSize == -1)
                fontSize = letterSizeInPixels;
           
            int length = fontSize * label.Length;

            if (PaintingGameViewUI && length > _playtimeGuiWidth)
                return _playtimeGuiWidth;

            int count = 0;
            for (int i = 0; i < label.Length; i++)
            {
                if (char.IsUpper(label[i])) count++;
            }

            length += (int)(count * fontSize * 0.5f);

            return length;
        }

      //  private static int RemainingLength(this string label, int otherElements) => Mathf.Min(label.IsNullOrEmpty() ? 1 : letterSizeInPixels * label.Length, Screen.width - otherElements);

        private static int RemainingLength(int otherElements) => PaintingGameViewUI ? _playtimeGuiWidth - otherElements : Screen.width - otherElements;

        #endregion

        #region Inspection Variables

        private static int _elementIndex;
        private static int selectedFold = -1;
       
        private static bool _lineOpen;

        private static readonly Color AttentionColor = new Color(1f, 0.7f, 0.7f, 1);

        private static readonly Color PreviousInspectedColor = new Color(0.3f, 0.7f, 0.3f, 1);


#region GUI Colors

        private static bool _guiColorReplaced;

        private static Color _originalGuiColor;

        //private static List<Color> _previousGuiColors = new List<Color>();

        private static icon GUIColor(this icon icn, Color col)
        {
            SetGUIColor(col);
            return icn;
        }

        private static void SetGUIColor(this Color col)
        {
            if (!_guiColorReplaced)
                _originalGuiColor = GUI.color;
            //else
              //  _previousGuiColors.Add(GUI.color);

            GUI.color = col;

            _guiColorReplaced = true;

        }

        private static void RestoreGUIcolor()
        {
            if (_guiColorReplaced)
                GUI.color = _originalGuiColor;

            //_previousGuiColors.Clear();

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

        private static readonly List<Color> _previousBgColors = new List<Color>();

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
            RestoreBGcolor();
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

        public static void RestoreBGcolor()
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

            if (need == null) return false;

            msg = need.NeedAttention();

            return msg != null;
        }

        public static bool NeedsAttention(ICollection list, out string message, string listName = "list", bool canBeNull = false)
        {
            message = NeedsAttention(list, listName, canBeNull);
            return message != null;
        }

        public static string NeedsAttention(ICollection list, string listName = "list", bool canBeNull = false)
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
                    }

                    if (!canBeNull)
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
                EditorGUI.FocusTextInControl("_");
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
            return anyChanges.Dirty();
        }

        public static void NameNext(string name) => GUI.SetNextControlName(name);

        public static string FocusedName
        {
            get { return GUI.GetNameOfFocusedControl(); }
            set { GUI.FocusControl(value); }
        }

        #endregion

#region Pop UP Services
        
        public static class PopUpService
        {
            
            public const string DiscordServer = "https://discord.gg/rF7yXq3";

            public const string SupportEmail = "quizcanners@gmail.com";

            public static string popUpHeader = "";

            public static string popUpText = "";

            public static string relatedLink = "";

            public static string relatedLinkName = "";

            private static object popUpTarget;

            private static string understoodPopUpText = "Got it";

            public static Func<bool> inspectDocumentationDelegate;

            public static Action areYouSureFunk;

            private static readonly List<string> gotItTexts = new List<string>
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

            private static readonly List<string> gotItTextsWeird = new List<string>
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

            private static int textsShown;

            public static void InitiatePopUp()
            {
                popUpTarget = ef.inspectedTarget;

                /*textsShown switch
                {
                 0 => understoodPopUpText = "OK",
                 1 => understoodPopUpText = "Got it!",
                 666 => understoodPopUpText = "By clicking I confirm to selling my kidney",
                 _ => understoodPopUpText = (textsShown < 20 ? gotItTexts : gotItTextsWeird).GetRandom()
                };*/
                
                switch (textsShown)
                {
                    case 0: understoodPopUpText = "OK"; break;
                    case 1: understoodPopUpText = "Got it!"; break;
                    case 666: understoodPopUpText = "By clicking I confirm to selling my kidney"; break;
                    default: understoodPopUpText = (textsShown < 20 ? gotItTexts : gotItTextsWeird).GetRandom(); break;
                }

                textsShown++;
            }

            public static void ClosePopUp()
            {
                popUpText = null;
                relatedLink = null;
                relatedLinkName = null;
                inspectDocumentationDelegate = null;
                areYouSureFunk = null;
            }

            #region Calls 

            private static bool fullWindowDocumentationClickOpen(string toolTip = "", int buttonSize = 20,
          icon clickIcon = icon.Question)
            {
                if (toolTip.IsNullOrEmpty())
                    toolTip = icon.Question.GetDescription();

                return clickIcon.BgColor(Color.clear).Click(toolTip, buttonSize).SetPreviousBgColor();
            }

            public static void fullWindowAreYouSureOpen(Action action, string header = "",
                string text = "")
            {

                if (header.IsNullOrEmpty())
                    header = Msg.AreYouSure.GetText();

                if (text.IsNullOrEmpty())
                    text = Msg.ClickYesToConfirm.GetText();

                PopUpService.areYouSureFunk = action;
                PopUpService.popUpText = text;
                PopUpService.popUpHeader = header;
                PopUpService.InitiatePopUp();
            }

            public static bool fullWindowDocumentationClickOpen(Func<bool> function, string toolTip = "", int buttonSize = 20)
            {
                if (toolTip.IsNullOrEmpty())
                    toolTip = icon.Question.GetDescription();

                if (fullWindowDocumentationClickOpen(toolTip, buttonSize))
                {
                    inspectDocumentationDelegate = function;
                    InitiatePopUp();
                    return true;
                }

                return false;
            }

            public static bool DocumentationClick(string toolTip, int buttonSize = 20, icon clickIcon = icon.Question)
            {
                if (fullWindowDocumentationClickOpen(toolTip, buttonSize, clickIcon))
                {
                    PopUpService.popUpHeader = toolTip;
                    return true;
                }

                return false;
            }

            public static bool DocumentationWarningClick(string toolTip, int buttonSize = 20)
            => DocumentationClick(toolTip, buttonSize = 20, icon.Warning);

            public static void FullWindwDocumentationOpen(string text)
            {
                popUpText = text;
                InitiatePopUp();
            }

            public static void FullWindwDocumentationOpen(Func<string> text)
            {
                popUpText = text();
                InitiatePopUp();
            }

            public static bool fullWindowWarningDocumentationClickOpen(Func<string> text, string toolTip = "What is this?",
                int buttonSize = 20) => fullWindowDocumentationClickOpen(text, toolTip, buttonSize, icon.Warning);

            public static bool fullWindowWarningDocumentationClickOpen(string text, string toolTip = "What is this?",
                int buttonSize = 20) => fullWindowDocumentationClickOpen(text, toolTip, buttonSize, icon.Warning);


            public static bool fullWindowDocumentationClickOpen(Func<string> text, string toolTip = "", int buttonSize = 20, icon clickIcon = icon.Question)
            {

                bool gotHeadline = false;

                if (toolTip.IsNullOrEmpty())
                    toolTip = Msg.ToolTip.GetDescription();
                else gotHeadline = true;

                if (fullWindowDocumentationClickOpen(toolTip, buttonSize, clickIcon))
                {
                    popUpText = text();
                    popUpHeader = gotHeadline ? toolTip : "";
                    InitiatePopUp();
                    return true;
                }

                return false;
            }

            public static bool fullWindowDocumentationClickOpen(string text, string toolTip = "", int buttonSize = 20, icon clickIcon = icon.Question)
            {

                bool gotHeadline = false;

                if (toolTip.IsNullOrEmpty())
                    toolTip = Msg.ToolTip.GetDescription();
                else gotHeadline = true;

                if (fullWindowDocumentationClickOpen(toolTip, buttonSize, clickIcon))
                {
                    popUpText = text;
                    popUpHeader = gotHeadline ? toolTip : "";
                    InitiatePopUp();
                    return true;
                }

                return false;
            }


            public static bool fullWindowDocumentationWithLinkClickOpen(string text, string link, string linkName = null, string tip = "", int buttonSize = 20)
            {
                if (tip.IsNullOrEmpty())
                    tip = icon.Question.GetDescription();

                if (fullWindowDocumentationClickOpen(tip, buttonSize))
                {
                    PopUpService.popUpText = text;
                    PopUpService.InitiatePopUp();
                    PopUpService.relatedLink = link;
                    PopUpService.relatedLinkName = linkName.IsNullOrEmpty() ? link : linkName;
                    return true;
                }

                return false;
            }
            
            #endregion
            
            #region Elements

            private static void ContactOptions()
            {
                nl();
                "Didn't get the answer you need?".write();
                if (icon.Discord.Click())
                    Application.OpenURL(DiscordServer);
                if (icon.Email.Click())
                    QcUnity.SendEmail(SupportEmail, "About this hint",
                        "The toolTip:{0}***{0} {1} {0}***{0} haven't answered some of the questions I had on my mind. Specifically: {0}".F(EnvironmentNl, popUpText));

            }

            private static void ConfirmLabel()
            {
                nl();

                if (understoodPopUpText.ClickText(15).nl())
                    ClosePopUp();

                ContactOptions();
            }

            static bool WriteHeaderIfAny()
            {
                if (!popUpHeader.IsNullOrEmpty())
                {
                    popUpHeader.write(PEGI_Styles.ListLabel);
                    return true;
                }

                return false;
            }

            public static bool ShowingPopup()
            {

                if (popUpTarget == null || popUpTarget != ef.inspectedTarget)
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
                            Debug.LogError(ex.ToString());
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
                    return EditorGUI.indentLevel;
#endif

                return 0;
            }

            set
            {
#if UNITY_EDITOR
                if (!PaintingGameViewUI)
                    EditorGUI.indentLevel = Mathf.Max(0, value);
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

        public static bool nl_ifNotEntered(this bool value, ref bool changed)
        {
            changed |= value;
            nl_ifNotEntered();
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

        public static bool nl_ifFolded(this bool value, ref bool changes)
        {
            nl_ifFolded();
            changes |= value;
            return value;
        }

        public static bool nl_ifFoldedOut(this bool value, ref bool changes)
        {
            nl_ifFoldedOut();
            changes |= value;
            return value;
        }

        public static bool nl_ifEntered(this bool value, ref bool changes)
        {
            nl_ifEntered();
            changes |= value;
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

        public static bool nl(this bool value, ref bool changed)
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

        public static void nl(this string value, string hint, PEGI_Styles.PegiGuiStyle style)
        {
            write(value, hint, style);
            nl();
        }

        public static void nl(this icon icon, int size = defaultButtonSize)
        {
            icon.write(size);
            nl();
        }

        public static void nl(this icon icon, string hint, int size = defaultButtonSize)
        {
            icon.write(hint, size);
            nl();
        }


#endregion

    }
}
