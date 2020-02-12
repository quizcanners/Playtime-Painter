﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using QuizCannersUtilities;
using Object = UnityEngine.Object;

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
                set { if (value) mouseOverUi = Time.frameCount; }
            }

            public delegate bool InspectionDelegate();

            public delegate bool WindowFunction();

            public class Window
            {
                private WindowFunction _function;
                private Rect _windowRect;
                private Vector2 _scrollPosition = Vector2.zero;
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
                            GUI.matrix = Matrix4x4.TRS(new Vector3(0, 0, 0), Quaternion.identity, new Vector3(upscale, upscale, 1));

                        if (!PopUpService.ShowingPopup())
                            _function();

                        nl();

                        UnIndent();

                        "{0}:{1}".F(Msg.ToolTip.GetText(), GUI.tooltip).nl();

                        if (UseWindow)
                        {
                            if (_windowRect.Contains(Input.mousePosition))
                                MouseOverUI = true;

                            GUI.DragWindow(new Rect(0, 0, 3000, 40 * upscale));
                        }
                        else
                            MouseOverUI = true;

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

                    ef.inspectedTarget = target;

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

            public static int Width => (int)Resolution.x;

            public static int Height => (int)Resolution.y;

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

        private enum PegiPaintingMode { EditorInspector, PlayAreaGui }

        private static PegiPaintingMode currentMode = PegiPaintingMode.EditorInspector;

        public static bool PaintingGameViewUI
        {
            get { return currentMode == PegiPaintingMode.PlayAreaGui; }
            private set { currentMode = value ? PegiPaintingMode.PlayAreaGui : PegiPaintingMode.EditorInspector; }
        }

        private static int _playtimeGuiWidth = 400;

        private static GUILayoutOption GuiMaxWidthOption => GUILayout.MaxWidth(_playtimeGuiWidth);

        private static GUILayoutOption GuiMaxWidthOptionFrom(string text) => GUILayout.MaxWidth(Mathf.Min(_playtimeGuiWidth, ApproximateLength(text)));

        private static GUILayoutOption GuiMaxWidthOptionFrom(GUIContent cnt) => GUILayout.MaxWidth(Mathf.Min(_playtimeGuiWidth, ApproximateLength(cnt.text)));

        private const int letterSizeInPixels = 9;

        public static int ApproximateLength(this string label)
        {
            if (label == null || label.Length == 0)
                return 1;

            int length = letterSizeInPixels * label.Length;

            if (PaintingGameViewUI && length > _playtimeGuiWidth)
                return _playtimeGuiWidth;

            int count = 0;
            for (int i = 0; i < label.Length; i++)
            {
                if (char.IsUpper(label[i])) count++;
            }

            length += (int)(count * letterSizeInPixels * 0.5f);

            return length;
        }

        private static int ApproximateLength(this string label, int otherElements) => Mathf.Min(label.IsNullOrEmpty() ? 1 : letterSizeInPixels * label.Length, Screen.width - otherElements);

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

        private static List<Color> _previousGuiColors = new List<Color>();

        private static icon GUIColor(this icon icn, Color col)
        {
            SetGUIColor(col);
            return icn;
        }

        private static void SetGUIColor(this Color col)
        {
            if (!_guiColorReplaced)
                _originalGuiColor = GUI.color;
            else
                _previousGuiColors.Add(GUI.color);

            GUI.color = col;

            _guiColorReplaced = true;

        }

        private static void RestoreGUIcolor()
        {
            if (_guiColorReplaced)
                GUI.color = _originalGuiColor;

            _previousGuiColors.Clear();

            _guiColorReplaced = false;
        }

        private static bool RestoreGUIColor(this bool val)
        {
            RestoreGUIcolor();
            return val;
        }

#endregion

#region BG Color

        private static bool _bgColorReplaced = false;

        private static Color _originalBgColor;

        private static readonly List<Color> _previousBgColors = new List<Color>();

        public static icon BgColor(this icon icn, Color col)
        {
            SetBgColor(col);
            return icn;
        }

        private static bool PreviousBgColor(this bool val)
        {
            PreviousBgColor();
            return val;
        }

        public static bool RestoreBGColor(this bool val)
        {
            RestoreBGcolor();
            return val;
        }

        public static void PreviousBgColor()
        {
            if (!_bgColorReplaced) return;

            if (_previousBgColors.Count > 0)
                SetBgColor(_previousBgColors.RemoveLast());
            else
                RestoreBGcolor();

        }

        public static void SetBgColor(Color col)
        {

            if (!_bgColorReplaced)
                _originalBgColor = GUI.backgroundColor;
            else
                _previousBgColors.Add(GUI.backgroundColor);

            GUI.backgroundColor = col;

            _bgColorReplaced = true;

        }

        public static void RestoreBGcolor()
        {
            if (_bgColorReplaced)
                GUI.backgroundColor = _originalBgColor;

            _previousBgColors.Clear();

            _bgColorReplaced = false;
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

        public static bool NeedsAttention(IList list, out string message, string listName = "list", bool canBeNull = false)
        {
            message = NeedAttentionMessage(list, listName, canBeNull);
            return message != null;
        }

        public static string NeedAttentionMessage(IList list, string listName = "list", bool canBeNull = false)
        {
            string msg = null;
            if (list == null)
                msg = canBeNull ? null : "{0} is Null".F(listName);
            else
            {
                for (var i = 0; i < list.Count; i++)
                {
                    var el = list[i];
                    if (!el.IsNullOrDestroyed_Obj())
                    {
                        var need = el as INeedAttention;

                        if (need == null) continue;

                        var what = need.NeedAttention();

                        if (what == null) continue;

                        msg = " {0} on {1}:{2}".F(what, i, need.GetNameForInspector());
                        LastNeedAttentionIndex = i;

                        return msg;
                    }
                    else if (!canBeNull)
                    {
                        msg = "{0} element in {1} is NULL".F(i, listName);
                        LastNeedAttentionIndex = i;

                        return msg;
                    }
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

        public static void line() => line(PaintingGameViewUI ? Color.white : Color.black);

        public static void line(Color col)
        {
            nl();

            var c = GUI.color;
            GUI.color = col;
            GUILayout.Box(GUIContent.none, PEGI_Styles.HorizontalLine.Current);
            GUI.color = c;
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

        private static bool fullWindowDocumentationClickOpen(string toolTip = "", int buttonSize = 20,
            icon clickIcon = icon.Question)
        {
            if (toolTip.IsNullOrEmpty())
                toolTip = icon.Question.GetDescription();

            return clickIcon.BgColor(Color.clear).Click(toolTip, buttonSize).PreviousBgColor();
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

        public static bool fullWindowDocumentationClickOpen(GameView.InspectionDelegate function, string toolTip = "", int buttonSize = 20)
        {
            if (toolTip.IsNullOrEmpty())
                toolTip = icon.Question.GetDescription();

            if (fullWindowDocumentationClickOpen(toolTip, buttonSize))
            {
                PopUpService.inspectDocumentationDelegate = function;
                PopUpService.InitiatePopUp();
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
            PopUpService.popUpText = text;
            PopUpService.InitiatePopUp();
        }

        public static bool fullWindowWarningDocumentationClickOpen(this string text, string toolTip = "What is this?",
            int buttonSize = 20) => text.fullWindowDocumentationClickOpen(toolTip, buttonSize, icon.Warning);

        public static bool fullWindowDocumentationClickOpen(this string text, string toolTip = "", int buttonSize = 20, icon clickIcon = icon.Question)
        {

            bool gotHeadline = false;

            if (toolTip.IsNullOrEmpty())
                toolTip = Msg.ToolTip.GetDescription();
            else gotHeadline = true;

            if (fullWindowDocumentationClickOpen(toolTip, buttonSize, clickIcon))
            {
                PopUpService.popUpText = text;
                PopUpService.popUpHeader = gotHeadline ? toolTip : "";
                PopUpService.InitiatePopUp();
                return true;
            }

            return false;
        }

        public static bool fullWindowDocumentationWithLinkClickOpen(this string text, string link, string linkName = null, string tip = "", int buttonSize = 20)
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

            public static GameView.InspectionDelegate inspectDocumentationDelegate;

            public static Action areYouSureFunk;

            private static readonly List<string> gotItTexts = new List<string>()
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
                "Now I can continue",


            };

            private static readonly List<string> gotItTextsWeird = new List<string>()
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

                switch (textsShown)
                {
                    case 0: understoodPopUpText = "OK"; break;
                    case 1: understoodPopUpText = "Got it!"; break;
                    case 666: understoodPopUpText = "By clicking I confirm to selling my kidney"; break;
                    default: understoodPopUpText = (textsShown < 20 ? gotItTexts : gotItTextsWeird).GetRandom(); break;
                }

                textsShown++;
            }

            static void ClosePopUp()
            {
                popUpText = null;
                relatedLink = null;
                relatedLinkName = null;
                inspectDocumentationDelegate = null;
                areYouSureFunk = null;
            }

#region Elements

            private static void ContactOptions()
            {
                pegi.nl();
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

                if (understoodPopUpText.Click(15).nl())
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
                else if (inspectDocumentationDelegate != null)
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
                else if (!popUpText.IsNullOrEmpty())
                {

                    WriteHeaderIfAny().nl();

                    popUpText.writeBig("Click the blue text below to close this toolTip. This is basically a toolTip for a toolTip. It is the world we are living in now.");

                    if (!relatedLink.IsNullOrEmpty() && relatedLinkName.Click(14))
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
                return;
            }
#endif

        }

        public static void Indent(int width = 1)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                ef.Indent(width);
                return;
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

#region WRITE

#region GUI Contents
        private static GUIContent imageAndTip = new GUIContent();

        private static GUIContent ImageAndTip(Texture tex, string toolTip)
        {
            imageAndTip.image = tex;
            imageAndTip.tooltip = toolTip;
            return imageAndTip;
        }

        private static GUIContent ImageAndTip(Texture tex)
        {
            imageAndTip.image = tex;
            imageAndTip.tooltip = tex ? tex.name : "Null Image";
            return imageAndTip;
        }

        private static GUIContent textAndTip = new GUIContent();

        private static GUIContent TextAndTip(string text)
        {
            textAndTip.text = text;
            textAndTip.tooltip = text;
            return textAndTip;
        }

        private static GUIContent TextAndTip(string text, string toolTip)
        {
            textAndTip.text = text;
            textAndTip.tooltip = toolTip;
            return textAndTip;
        }

        private static GUIContent tipOnlyContent = new GUIContent();

        private static GUIContent TipOnlyContent(string text)
        {
            tipOnlyContent.tooltip = text;
            return tipOnlyContent;
        }

#endregion
        
#region Unity Object

        public static void write<T>(T field) where T : Object
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                ef.write(field);
#endif
        }

        public static void write<T>(this string label, string tip, int width, T field) where T : Object
        {
            write(label, tip, width);
            write(field);

        }

        public static void write<T>(this string label, int width, T field) where T : Object
        {
            write(label, width);
            write(field);

        }

        public static void write<T>(this string label, T field) where T : Object
        {
            write(label);
            write(field);

        }

        public static void write(this Sprite sprite, int width = defaultButtonSize, bool alphaBlend = false)
        {
            if (!sprite)
            {
                icon.Empty.write(width);
            }
            else
            {

                checkLine();

                Rect c = sprite.textureRect;

                float max = Mathf.Max(c.width, c.height);

                float scale = defaultButtonSize / max;

                float spriteW = c.width * scale;
                float spriteH = c.height * scale;
                Rect rect = GUILayoutUtility.GetRect(spriteW, spriteH, GUILayout.ExpandWidth(false)); //GetRect(spriteW, spriteW, spriteH, spriteH);

                if (Event.current.type == EventType.Repaint)
                {
                    var tex = sprite.texture;
                    c.xMin /= tex.width;
                    c.xMax /= tex.width;
                    c.yMin /= tex.height;
                    c.yMax /= tex.height;
                    GUI.DrawTextureWithTexCoords(rect, tex, c, alphaBlend);
                }
            }
        }

        public static void write(this Sprite sprite, string toolTip, int width = defaultButtonSize)
        {
            if (sprite)
                sprite.texture.write(toolTip, width);
            else
                icon.Empty.write(toolTip, width);
        }

        public static void write(this Sprite sprite, string toolTip, int width, int height)
        {
            if (sprite)
                sprite.texture.write(toolTip, width, height);
            else
                icon.Empty.write(toolTip, width, height);
        }

        public static void write(this Texture img, int width = defaultButtonSize)
        {
            if (!img)
                return;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                ef.write(img, width);

            else
#endif
            {
                SetBgColor(Color.clear);

                img.Click(width);

                RestoreBGcolor();
            }
        }

        public static void write(this Texture img, string toolTip, int width = defaultButtonSize)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                ef.write(img, toolTip, width);

            else
#endif


            {

                SetBgColor(Color.clear);

                img.Click(toolTip, width, width);

                RestoreBGcolor();
            }

        }

        public static void write(this Texture img, string toolTip, int width, int height)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                ef.write(img, toolTip, width, height);
            else
#endif
            {

                SetBgColor(Color.clear);

                img.Click(toolTip, width, height);

                RestoreBGcolor();

            }

        }

#endregion

        public static void write(this icon icon, int size = defaultButtonSize) => write(icon.GetIcon(), size);

        public static void write(this icon icon, string toolTip, int size = defaultButtonSize) => write(icon.GetIcon(), toolTip, size);

        public static void write(this icon icon, string toolTip, int width, int height) => write(icon.GetIcon(), toolTip, width, height);

        public static void write(this string text)
        {
            var cnt = TextAndTip(text);

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                ef.write(cnt);
            else
#endif
            {
                checkLine();
                GUILayout.Label(cnt, GuiMaxWidthOption);
            }

        }

        public static void write(this string text, PEGI_Styles.PegiGuiStyle style)
        {
            var cnt = TextAndTip(text);

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                ef.write(cnt, style.Current);
            else
#endif
            {
                checkLine();
                GUILayout.Label(cnt, GuiMaxWidthOption);
            }
        }

        public static void write(this string text, string toolTip, PEGI_Styles.PegiGuiStyle style)
        {
            textAndTip.text = text;
            textAndTip.tooltip = toolTip;


#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                ef.write(textAndTip, style.Current);
            else
#endif
            {
                checkLine();
                GUILayout.Label(textAndTip, GuiMaxWidthOption);
            }
        }

        public static void write(this string text, int width, PEGI_Styles.PegiGuiStyle style)
        {
            textAndTip.text = text;
            textAndTip.tooltip = text;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                ef.write(textAndTip, width, style.Current);
                return;
            }
#endif

            checkLine();
            GUILayout.Label(textAndTip, GUILayout.MaxWidth(width));

        }

        public static void write(this string text, string toolTip, int width, PEGI_Styles.PegiGuiStyle style)
        {

            textAndTip.text = text;
            textAndTip.tooltip = toolTip;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                ef.write(textAndTip, width, style.Current);
                return;
            }
#endif

            checkLine();
            GUILayout.Label(textAndTip, GUILayout.MaxWidth(width));

        }

        public static void write(this string text, int width) => text.write(text, width);

        public static void write(this string text, string toolTip)
        {

            textAndTip.text = text;
            textAndTip.tooltip = toolTip;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                ef.write(textAndTip);
                return;
            }
#endif

            checkLine();
            GUILayout.Label(textAndTip, GuiMaxWidthOption);
        }

        public static void write(this string text, string toolTip, int width)
        {

            textAndTip.text = text;
            textAndTip.tooltip = toolTip;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                ef.write(textAndTip, width);
                return;
            }
#endif

            checkLine();
            GUILayout.Label(textAndTip, GUILayout.MaxWidth(width));

        }

        public static void writeBig(this string text, int width, string contents, string tooltip = "")
        {
            text.nl(width);
            contents.writeBig(tooltip: tooltip);
            nl();
        }

        public static void writeBig(this string text, string tooltip ="")
        {
            text.write(tooltip, PEGI_Styles.OverflowText);
            nl();
        }

        public static void SetClipboard (string value, string hint = "", bool sendNotificationIn3Dview = true)
        {
            GUIUtility.systemCopyBuffer = value; 

            if (sendNotificationIn3Dview)
               GameView.ShowNotification("{0} Copied to clipboard".F(hint));
        }

        public static bool write_ForCopy(this string text, bool showCopyButton = false)
        {

            var ret = false;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                ef.write_ForCopy(text);
            else
#endif
            {
                ret = edit(ref text);
            }
            
            if (showCopyButton && icon.Copy.Click("Copy text to clipboard"))
                SetClipboard(text);

            return ret;
        }

        public static bool write_ForCopy(this string text, int width, bool showCopyButton = false)
        {
            var ret = false;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                ef.write_ForCopy(text);
            else
#endif
            {
                ret = edit(ref text);
            }

            if (showCopyButton && icon.Copy.Click("Copy text to clipboard"))
                SetClipboard(text);

            return ret;

        }

        public static bool write_ForCopy(this string label, int width, string val, bool showCopyButton = false)
        {
            var ret = edit(label, width, ref val);

            if (showCopyButton && icon.Copy.Click("Copy {0} to clipboard".F(label)))
                SetClipboard(val, label);

            return ret;

        }

        public static bool write_ForCopy(this string label, string val, bool showCopyButton = false)
        {
            var ret = label.edit(ref val);

            if (showCopyButton && icon.Copy.Click("Copy {0} to clipboard".F(label)))
                SetClipboard(val, label);

            return ret;

        }

        public static bool write_ForCopy_Big(string val, bool showCopyButton = false)
        {

            if (showCopyButton && "Copy text to clipboard".Click().nl())
                SetClipboard(val);

            if (PaintingGameViewUI && !val.IsNullOrEmpty() && val.ContainsAtLeast('\n', 5)) // Due to MGUI BUG
                ".....   Big Text Has Many Lines: {0}".F(val.FirstLine()).write();
            else
                return editBig(ref val);

            return false;
        }

        public static bool write_ForCopy_Big(this string label, string val, bool showCopyButton = false)
        {

            if (showCopyButton && icon.Copy.Click("Copy text to clipboard"))
                SetClipboard(val, label);

            label.nl();

            if (PaintingGameViewUI && !val.IsNullOrEmpty() && val.ContainsAtLeast('\n', 5)) // Due to MGUI BUG
                ".....   Big Text Has Many Lines: {0}".F(val.FirstLine()).write();
            else
                return editBig(ref val);

            return false;
        }

#region Warning & Hints
        public static void writeWarning(this string text)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                ef.writeHint(text, MessageType.Warning);
                ef.newLine();
                return;
            }
#endif

            checkLine();
            GUILayout.Label(text, GuiMaxWidthOption);
            nl();

        }

        public static void writeHint(this string text, bool startNewLineAfter = true)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                ef.writeHint(text, MessageType.Info);
                if (startNewLineAfter)
                    ef.newLine();
                return;
            }
#endif

            checkLine();
            GUILayout.Label(text, GuiMaxWidthOption);
            if (startNewLineAfter)
                nl();


        }

        public static void resetOneTimeHint(this string name) => PlayerPrefs.SetInt(name, 0);

        public static bool writeOneTimeHint(this string text, string name)
        {

            if (PlayerPrefs.GetInt(name) != 0) return false;

            nl();

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                ef.writeHint(text, MessageType.Info);
            }
            else
#endif
            {
                checkLine();
                GUILayout.Label(text, GuiMaxWidthOption);
            }

            if (!icon.Done.ClickUnFocus("Got it").nl()) return false;

            PlayerPrefs.SetInt(name, 1);
            return true;
        }

#endregion

#endregion

    }
}
