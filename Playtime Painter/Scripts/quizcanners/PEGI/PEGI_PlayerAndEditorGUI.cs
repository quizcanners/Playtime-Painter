using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Linq;

using System.Linq.Expressions;
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


namespace PlayerAndEditorGUI
{

    #region interfaces & Attributes

    public interface IPEGI
    {
    #if !NO_PEGI
        bool Inspect();
    #endif
    }

    public interface IPEGI_ListInspect
    {
    #if !NO_PEGI
        bool InspectInList(IList list, int ind, ref int edited);
    #endif
    }

    public interface IPEGI_Searchable
    {
    #if !NO_PEGI
        bool String_SearchMatch(string searchString);
    #endif
    }

    public interface INeedAttention
    {
    #if !NO_PEGI
        string NeedAttention();
    #endif
    }

    public interface IGotName
    {
    #if !NO_PEGI
        string NameForPEGI { get; set; }
    #endif
    }

    public interface IGotDisplayName
    {
    #if !NO_PEGI
        string NameForDisplayPEGI();
    #endif
    }

    public interface IGotIndex
    {
    #if !NO_PEGI
        int IndexForPEGI { get; set; }
    #endif
    }

    public interface IGotCount
    {
    #if !NO_PEGI
        int CountForInspector();
#endif
    }

    public interface IEditorDropdown
    {
    #if !NO_PEGI
        bool ShowInDropdown();
    #endif
    }

    public interface IPegiReleaseGuiManager {
        #if !NO_PEGI
        void Inspect();
        void Write(string label);
        bool Click(string label);
        #endif
    }


    #endregion
    
    public static partial class pegi {

        public static string EnvironmentNl => Environment.NewLine;

        private static int mouseOverUi = -1;

        public static bool MouseOverPlaytimePainterUI {
            get { return mouseOverUi >= Time.frameCount - 1; }
            set { if (value) mouseOverUi = Time.frameCount; }
        }

#if !NO_PEGI

#region UI Modes

        private enum PegiPaintingMode { EditorInspector, PlayAreaGui, Release }

        private static PegiPaintingMode currentMode = PegiPaintingMode.EditorInspector;

        public static bool paintingReleaseGUI => currentMode == PegiPaintingMode.Release;

        public static bool paintingPlayAreaGui
        {
            get { return currentMode == PegiPaintingMode.PlayAreaGui; }
            private set { currentMode = value ? PegiPaintingMode.PlayAreaGui : PegiPaintingMode.EditorInspector; }
        }

#endregion

#region Release GUI

        private static IPegiReleaseGuiManager currentReleaseManager;

        public static void ReleaseInspect(IPegiReleaseGuiManager manager) {
            currentMode = PegiPaintingMode.Release;
            currentReleaseManager = manager;
        }

#endregion

#region Play Area GUI
        public delegate bool InspectionDelegate();

        public class WindowPositionData_PEGI_GUI
        {
            private WindowFunction function;
            private Rect windowRect;

            private void DrawFunction(int windowID)
            {

                paintingPlayAreaGui = true;

                try
                {
                    globChanged = false;
                    _elementIndex = 0;
                    _lineOpen = false;
                    focusInd = 0;

                    if (!PopUpService.ShowingPopup())
                        function();

                    nl();

                    UnIndent();

                    "{0}:{1}".F(Msg.ToolTip.GetText(),GUI.tooltip).nl();

                    if (windowRect.Contains(Input.mousePosition))
                        MouseOverPlaytimePainterUI = true;

                    GUI.DragWindow(new Rect(0, 0, 10000, 20));
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                }

                paintingPlayAreaGui = false;
            }
            
            public void Render(IPEGI p) => Render(p, p.Inspect, p.GetNameForInspector());

            public void Render(IPEGI p, string windowName) => Render(p, p.Inspect, windowName);
            
            public void Render(IPEGI target, WindowFunction doWindow, string c_windowName) {

                inspectedTarget = target;

                windowRect.x = Mathf.Clamp(windowRect.x, 0, Screen.width - 10);
                windowRect.y = Mathf.Clamp(windowRect.y, 0, Screen.height - 10);

                function = doWindow;
                windowRect = GUILayout.Window(0, windowRect, DrawFunction, c_windowName);
            }

            public void Collapse()
            {
                windowRect.width = 10;
                windowRect.height = 10;
                windowRect.x = 10;
                windowRect.y = 10;
            }

            public WindowPositionData_PEGI_GUI()
            {
                windowRect = new Rect(20, 20, 350, 400);
            }
        }

        public delegate bool WindowFunction();
#endregion

#region Inspection Progress

        public static object inspectedTarget;

        private static int _elementIndex;

        public static bool isFoldedOutOrEntered;
        public static bool IsFoldedOut => isFoldedOutOrEntered;
        public static bool IsEntered => isFoldedOutOrEntered; 
        
        private static int selectedFold = -1;
        public static int tabIndex; // will be reset on every NewLine;
        
        private static bool _lineOpen;
        
        private static readonly Color AttentionColor = new Color(1f, 0.7f, 0.7f, 1);

        private static readonly Color PreviousInspectedColor = new Color(0.3f,0.7f, 0.3f, 1);


#region GUI Colors

        private static bool _guiColorReplaced;

        private static Color _originalGuiColor;

        private static List<Color> _previousGuiColors = new List<Color>();

        public static icon GUIColor(this icon icn, Color col)
        {
            SetGUIColor(col);
            return icn;
        }

        public static void SetGUIColor(this Color col)
        {
            if (!_guiColorReplaced)
                _originalGuiColor = GUI.color;
            else
                _previousGuiColors.Add(GUI.color);

            GUI.color = col;

            _guiColorReplaced = true;

        }

        public static void RestoreGUIcolor()
        {
            if (_guiColorReplaced)
                GUI.color = _originalGuiColor;

            _previousGuiColors.Clear();

            _guiColorReplaced = false;
        }

        public static bool RestoreGUIColor(this bool val)
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

        public static string BgColor(this string txt, Color col)
        {
            SetBgColor(col);
            return txt;
        }

        public static bool PreviousBgColor(this bool val)
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

        public static void SetBgColor(this Color col)
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
                if (!paintingPlayAreaGui)
                    ef.checkLine();
                else
#endif
            if (!_lineOpen) {
                tabIndex = 0;
                GUILayout.BeginHorizontal();
                _lineOpen = true;
            }
        }
        
        public static bool UnFocus(this bool anyChanges)
        {
            if (anyChanges)
                FocusControl("_");
            return anyChanges;
        }

        private static bool DirtyUnFocus(this bool anyChanges) {
            if (anyChanges)
                FocusControl("_");
            return anyChanges.Dirty();
        }

        public static string LastNeedAttentionMessage;
        public static int LastNeedAttentionIndex;

        public static bool NeedsAttention(this IList list, string listName = "list", bool canBeNull = false) {
            LastNeedAttentionMessage = null;
            LastNeedAttentionMessage = list.NeedAttentionMessage(listName, canBeNull);
            return LastNeedAttentionMessage != null;
        }

        public static string NeedAttentionMessage(this IList list, string listName = "list", bool canBeNull = false) {
            LastNeedAttentionMessage = null;
            if (list == null)
                LastNeedAttentionMessage = canBeNull ? null : "{0} is Null".F(listName);
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
                        
                        LastNeedAttentionMessage = " {0} on {1}:{2}".F(what, i, need.GetNameForInspector());
                        LastNeedAttentionIndex = i;

                        return LastNeedAttentionMessage;
                    }
                    else if (!canBeNull) {
                        LastNeedAttentionMessage = "{0} element in {1} is NULL".F(i, listName);
                        LastNeedAttentionIndex = i;

                        return LastNeedAttentionMessage;
                    }
                }
            }

            return LastNeedAttentionMessage;
        }

        public static void FocusControl(string name)
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                EditorGUI.FocusTextInControl(name);
            else
#endif
                GUI.FocusControl(name);
        }
        
        public static void NameNext(string name) => GUI.SetNextControlName(name);

        public static int NameNextUnique(ref string name)
        {
            name += focusInd.ToString();
            GUI.SetNextControlName(name);
            focusInd++;

            return (focusInd - 1);
        }

        public static string nameFocused => GUI.GetNameOfFocusedControl(); 

        public static void space()
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                ef.Space();
            else
#endif

            {
                checkLine();
                GUILayout.Space(10);
            }
        }

        public static void line() => line(paintingPlayAreaGui ? Color.white : Color.black);
  
        public static void line(Color col)
        {
            nl();

            var c = GUI.color;
            GUI.color = col;
            GUILayout.Box(GUIContent.none, PEGI_Styles.HorizontalLine);
            GUI.color = c;
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

        public static bool fullWindowDocumentationClickOpen(InspectionDelegate function, string toolTip = "", int buttonSize = 20)
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

        public static bool DocumentationClick(string toolTip, int buttonSize = 20, icon clickIcon = icon.Question) {
            if (fullWindowDocumentationClickOpen(toolTip, buttonSize, clickIcon)) {
                PopUpService.popUpHeader = toolTip;
                return true;
            }

            return false;
        }

        public static bool DocumentationWarningClick(string toolTip, int buttonSize = 20 )
        => DocumentationClick(toolTip,  buttonSize = 20, icon.Warning);

        public static void FullWindwDocumentationOpen(string text) {
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

            if (fullWindowDocumentationClickOpen(toolTip, buttonSize, clickIcon)) {
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

            public static InspectionDelegate inspectDocumentationDelegate;

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

            public static void InitiatePopUp() {

                popUpTarget = inspectedTarget;

                switch (textsShown) {
                    case 0: understoodPopUpText = "OK";  break;
                    case 1: understoodPopUpText = "Got it!"; break;
                    case 666: understoodPopUpText = "By clicking I confirm to selling my kidney"; break;
                    default: understoodPopUpText = (textsShown < 20 ? gotItTexts : gotItTextsWeird).GetRandom();  break;
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

            public static bool ShowingPopup() {

                if (popUpTarget == null || popUpTarget != inspectedTarget)
                    return false;

                if (areYouSureFunk != null) {

                    if (icon.Close.Click(Msg.No.GetText(), 35))
                        ClosePopUp();

                    WriteHeaderIfAny();
                    
                    if (icon.Done.Click(Msg.Yes.GetText(), 35)) {
                        try {
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

                } else if (inspectDocumentationDelegate != null) {

                    if (icon.Back.Click(Msg.Exit))
                        ClosePopUp();
                    else {
                        WriteHeaderIfAny().nl();

                        inspectDocumentationDelegate();

                        ContactOptions();
                    }

                    return true;
                } else if (!popUpText.IsNullOrEmpty()) {

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

#region Changes 
        public static bool globChanged; // Some times user can change temporary fields, like delayed Edits

        private static bool change { get { globChanged = true; return true; } }

        private static bool Dirty(this bool val) { globChanged |= val; return val; }

        public static bool changes(this bool value, ref bool changed)
        {
            changed |= value;
            return value;
        }

        private static bool ignoreChanges(this bool changed)
        {
            if (changed)
                globChanged = false;
            return changed;
        }

        private static bool wasChangedBefore;

        private static void bc()
        {
            checkLine();
            wasChangedBefore = GUI.changed;
        }

        private static bool ec() => (GUI.changed && !wasChangedBefore).Dirty();

#endregion

#region New Line

        private static int IndentLevel
        {
            get
            {
                #if UNITY_EDITOR
                if (!paintingPlayAreaGui)
                   return EditorGUI.indentLevel;
                #endif

                return 0;
            }

            set
            {
                #if UNITY_EDITOR
                if (!paintingPlayAreaGui)
                    EditorGUI.indentLevel = Mathf.Max(0, value);
                #endif
            }
        } 

        public static void UnIndent(int width = 1)
        {
            #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
            {
                ef.UnIndent(width);
                return;
            }
            #endif
            
        }

        public static void Indent(int width = 1) {
            #if UNITY_EDITOR
            if (!paintingPlayAreaGui) {
                ef.Indent(width);
                return;
            }
            #endif
          
        }

        public static void newLine()
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGui) {
                ef.newLine();
                return;
            }
#endif

            if (_lineOpen) {
                _lineOpen = false;
                GUILayout.EndHorizontal();
            }
        }

        public static void nl_ifFolded() => isFoldedOutOrEntered.nl_ifFalse();

        public static void nl_ifFoldedOut() => isFoldedOutOrEntered.nl_ifTrue();
        
        public static void nl_ifNotEntered() => isFoldedOutOrEntered.nl_ifFalse();

        public static void nl_ifEntered() => isFoldedOutOrEntered.nl_ifTrue();

        public static bool nl_ifFolded(this bool value) {
            nl_ifFolded();
            return value;
        }

        public static bool nl_ifFoldedOut(this bool value) {
            nl_ifFoldedOut();
            return value;
        }

        public static bool nl_ifNotEntered(this bool value, ref bool changed) {
            changed |= value;
            nl_ifNotEntered();
            return value;
        }

        public static bool nl_ifNotEntered(this bool value) {
            nl_ifNotEntered();
            return value;
        }

        public static bool nl_ifEntered(this bool value) {
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
                newLine();
            return value;
        }

        private static bool nl_ifFalse(this bool value)
        {
            if (!value)
                newLine();
            return value;
        }
        
        public static void nl() => newLine();

        public static bool nl(this bool value)
        {
            newLine();
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
            newLine();
        }

        public static void nl(this string value, string tip)
        {
            write(value, tip);
            newLine();
        }

        public static void nl(this string value, int width)
        {
            write(value, width);
            newLine();
        }

        public static void nl(this string value, string tip, int width)
        {
            write(value, tip, width);
            newLine();
        }

        public static void nl(this string value, GUIStyle style) {
            write(value, style);
            nl();
        }

        public static void nl(this string value, string hint, GUIStyle style)
        {
            write(value, hint, style);
            nl();
        }

        public static void nl(this icon icon, int size = defaultButtonSize) {
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

        private static GUIContent TipOnlyContent(string text)  {
            tipOnlyContent.tooltip = text;
            return tipOnlyContent;
        }

#endregion

        public const int letterSizeInPixels = 8;

        public static int ApproximateLengthUnsafe(this string label) => letterSizeInPixels * label.Length;
        
        private static int ApproximateLength(this string label) => label.IsNullOrEmpty() ? 1 : letterSizeInPixels * label.Length;

        private static int ApproximateLength(this string label, int otherElements) => Mathf.Min(label.IsNullOrEmpty() ? 1 : letterSizeInPixels * label.Length, Screen.width - otherElements);

        private static int RemainingLength(int otherElements) => Screen.width - otherElements;
        
#region Unity Object
        public static void write<T>(T field) where T : UnityEngine.Object {
#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                ef.write(field);
#endif
        }
        
        public static void writeUobj<T>(T field, int width) where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                ef.write(field, width);
#endif
        }

        public static void writeUobj<T>(this string label, string tip, int width, T field) where T : UnityEngine.Object
        {
            write(label, tip, width);
            write(field);

        }

        public static void writeUobj<T>(this string label, int width, T field) where T : UnityEngine.Object
        {
            write(label, width);
            write(field);

        }

        public static void writeUobj<T>(this string label, T field) where T : UnityEngine.Object
        {
            write(label);
            write(field);

        }

        public static void write(this Texture img, int width = defaultButtonSize)
        {
            if (!img)
                return;

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
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
            if (!paintingPlayAreaGui)
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
            if (!paintingPlayAreaGui)
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
            if (!paintingPlayAreaGui)
                ef.write(cnt);
            else
#endif
            {
                checkLine();
                GUILayout.Label(cnt);
            }

        }

        public static void write(this string text, GUIStyle style)
        {
            var cnt = TextAndTip(text);

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                ef.write(cnt, style);
            else
#endif
            {
                checkLine();
                GUILayout.Label(cnt);
            }
        }

        public static void write(this string text, string toolTip, GUIStyle style)
        {
            textAndTip.text = text;
            textAndTip.tooltip = toolTip;


#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                ef.write(textAndTip, style);
            else
#endif
            {
                checkLine();
                GUILayout.Label(textAndTip, style);
            }
        }

        public static void write(this string text, int width, GUIStyle style)
        {
            textAndTip.text = text;
            textAndTip.tooltip = text;

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
            {
                ef.write(textAndTip, width, style);
                return;
            }
#endif
            
            checkLine();
            GUILayout.Label(textAndTip, style, GUILayout.MaxWidth(width));
            
        }

        public static void write(this string text, string toolTip, int width, GUIStyle style)
        {

            textAndTip.text = text;
            textAndTip.tooltip = toolTip;

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
            {
                ef.write(textAndTip, width, style);
                return;
            }
#endif
            
            checkLine();
            GUILayout.Label(textAndTip, style, GUILayout.MaxWidth(width));
            
        }

        public static void write(this string text, int width) => text.write(text, width);

        public static void write(this string text, string toolTip)
        {

            textAndTip.text = text;
            textAndTip.tooltip = toolTip;

#if UNITY_EDITOR
            if (!paintingPlayAreaGui) {
                ef.write(textAndTip);
                return;
            }
#endif
            
            checkLine();
            GUILayout.Label(textAndTip);
        }

        public static void write(this string text, string toolTip, int width)  {

            textAndTip.text = text;
            textAndTip.tooltip = toolTip;
            
#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
            {
                ef.write(textAndTip, width);
                return;
            }
#endif
            
            checkLine();
            GUILayout.Label(textAndTip, GUILayout.MaxWidth(width));

        }

        public static void writeBig(this string text)
        {
            text.write("",PEGI_Styles.OverflowText);
            nl();
        }

        public static void writeBig(this string text, string toolTip)
        {
            text.write(toolTip, PEGI_Styles.OverflowText);
            nl();
        }

        public static bool write_ForCopy(this string val) => edit(ref val);

        public static bool write_ForCopy(this string val, int width) => edit(ref val, width);
        
        public static bool write_ForCopy(this string label, int width, string val) => edit(label, width, ref val);

        public static bool write_ForCopy(this string label, string val) => edit(label, ref val);
        
        public static bool write_ForCopy_Big(string val) => editBig(ref val);

        public static bool write_ForCopy_Big(this string label, string val) => label.editBig(ref val);

#region Warning & Hints
        public static void writeWarning(this string text)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui) {
                ef.writeHint(text, MessageType.Warning);
                ef.newLine();
                return;
            }
#endif
            
            checkLine();
            GUILayout.Label(text);
            newLine();
            
        }

        public static void writeHint(this string text, bool startNewLineAfter = true)
        {
        
#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
            {
                ef.writeHint(text, MessageType.Info);
                if (startNewLineAfter)
                    ef.newLine();
                return;
            }
#endif
            
            checkLine();
            GUILayout.Label(text);
            if (startNewLineAfter)
                newLine();
            

        }

        public static void resetOneTimeHint(this string name) => PlayerPrefs.SetInt(name, 0);

        public static bool writeOneTimeHint(this string text, string name) {

            if (PlayerPrefs.GetInt(name) != 0) return false;

            nl();

#if UNITY_EDITOR
            if (!paintingPlayAreaGui) {
                ef.writeHint(text, MessageType.Info);
            }
            else
#endif
            {
                checkLine();
                GUILayout.Label(text);
            }

            if (!icon.Done.ClickUnFocus("Got it").nl()) return false;
            
            PlayerPrefs.SetInt(name, 1);
            return true;
        }

#endregion

#endregion

#region SELECT
        
        static T filterEditorDropdown<T>(this T obj)  {
            var edd = obj as IEditorDropdown;
            return (edd == null || edd.ShowInDropdown()) ? obj : default(T); 
        }

        private static string CompileSelectionName<T>(int index, T obj, bool showIndex,  bool stripSlashes = false, bool dotsToSlashes = true) {
            var st = obj.GetNameForInspector();
            
            if (stripSlashes)
                st = st.SimplifyDirectory();

            if (dotsToSlashes)
                st = st.Replace('.', '/');

            return (showIndex || st.Length == 0) ? "{0}: {1}".F(index, st) : st;
        }
        
        private static bool selectFinal(ref int val, ref int indexes, List<string> namesList)
        {
            var count = namesList.Count;

            if (count == 0)
                return edit(ref val);

            if (indexes == -1)
            {
                indexes = namesList.Count;
                namesList.Add("[{0}]".F(val.GetNameForInspector()));

            }

            var tmp = indexes;

            if (select(ref tmp, namesList.ToArray()) && (tmp < count))
            {
                indexes = tmp;
                return true;
            }

            return false;

        }

        private static bool selectFinal<T>(T val, ref int indexes, List<string> namesList)
        {
            var count = namesList.Count;

            if (indexes == -1 && !val.IsNullOrDestroyed_Obj())
            {
                indexes = namesList.Count;
                namesList.Add("[{0}]".F(val.GetNameForInspector()));

            }

            var tmp = indexes;

            if (select(ref tmp, namesList.ToArray()) && tmp < count)
            {
                indexes = tmp;
                return true;
            }

            return false;
        }
        
        #region Select From Int List

        public static bool selectPow2(this string label, ref int current, int min, int max) =>
            label.selectPow2(label, label.ApproximateLength(), ref current, min, max);
        
        public static bool selectPow2(this string label, string tip, int width, ref int current, int min, int max)
        {

            label.write(tip, width);

            return selectPow2(ref current, min, max);

        }

        public static bool selectPow2(ref int current, int min, int max) {

            List<int> tmp = new List<int>();

            min = Mathf.NextPowerOfTwo(min);

            while (min <= max) {
                tmp.Add(min);
                min = Mathf.NextPowerOfTwo(min+1);
            }
            
            return select(ref current, tmp);

        }

        public static bool select(this string label, string tip, ref int value, List<int> list)
        {
            label.write(tip);
            return select(ref value, list);
        }

        public static bool select(this string label, string tip, int width, ref int value, List<int> list)
        {
            label.write(tip, width);
            return select(ref value, list);
        }

        public static bool select(this string label, int width, ref int value, List<int> list)
        {
            label.write(width);
            return select(ref value, list);
        }

        public static bool select(this string label, ref int value, List<int> list)
        {
            label.write(label.ApproximateLength());
            return select(ref value, list);
        }

        public static bool select(ref int value, List<int> list) => select(ref value, list.ToArray());

        public static bool select(this string text, int width, ref int value, int min, int max)
        {
            write(text, width);
            return select(ref value, min, max);
        }

        public static bool select(ref int value, int min, int max)
        {
            var cnt = max - min + 1;

            var tmp = value - min;
            var array = new int[cnt];
            for (var i = min; i < cnt; i++)
                array[i] = min + i;

            if (select(ref tmp, array))
            {
                value = tmp + min;
                return true;
            }

            return false;
        }

        public static bool select(this string text, ref int ind, int[] arr)
        {
            write(text);
            return select(ref ind, arr);
        }

        public static bool select(this string text, int width, ref int ind, int[] arr)
        {
            write(text, width);
            return select(ref ind, arr);
        }

        public static bool select(this string text, string tip, int width, ref int ind, int[] arr, bool showIndex = false)
        {
            write(text, tip, width);
            return select(ref ind, arr, showIndex);
        }

        public static bool select(ref int val, int[] arr, bool showIndex = false, bool stripSlashes = false, bool dotsToSlashes = true) {

            checkLine();

            var lnms = new List<string>();

            int tmp = -1;

            if (arr != null)
                for (var i = 0; i < arr.Length; i++) {

                    var el = arr[i];
                    if (el == val)
                        tmp = i;
                    lnms.Add(CompileSelectionName(i, el, showIndex, stripSlashes, dotsToSlashes ));
                }
            
            if (selectFinal(val, ref tmp, lnms))  {
                val = arr[tmp];
                return true;
            }

            return false;

        }

#endregion

#region From Strings

        public static bool select(ref int no, List<string> from)
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return from.IsNullOrEmpty() ? "Selecting from null:".edit(90, ref no) : ef.select(ref no, from.ToArray());
#endif


            if (from.IsNullOrEmpty()) return false;

            foldout(from.TryGet(no, "..."));

            if (isFoldedOutOrEntered)
            {
                if (from.Count > 1)
                    newLine();
                for (var i = 0; i < from.Count; i++)
                    if (i != no && "{0}: {1}".F(i, from[i]).ClickUnFocus().nl())
                    {
                        no = i;
                        foldIn();
                        return true;
                    }
            }

            GUILayout.Space(10);

            return false;

        }

        public static bool select(this string text, ref int value, List<string> list)
        {
            write(text, text.ApproximateLength());
            return select(ref value, list);
        }

        public static bool select(this string text, int width, ref int value, List<string> list) {
            write(text, width);
            return select(ref value, list);
        }

        public static bool select(this string text, int width, ref int value, string[] array)
        {
            write(text, width);
            return select(ref value, array);
        }


        public static bool selectFlags(ref int no, string[] from, int width = -1) {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return width > 0 ? ef.selectFlags(ref no, from, width) : ef.selectFlags(ref no, from);
#endif

            "Flags Only in Editor for now".write();

            return false;
        }

        public static bool select(ref int no, string[] from, int width = -1)
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return width > 0 ?
                    ef.select(ref no, from, width) :
                    ef.select(ref no, from);
#endif

            if (from.IsNullOrEmpty())
                return false;

            foldout(from.TryGet(no, "..."));

            if (isFoldedOutOrEntered) {

                if (from.Length > 1)
                    newLine();

                for (var i = 0; i < from.Length; i++)
                    if (i != no && "{0}: {1}".F(i, from[i]).ClickUnFocus().nl())
                    {
                        no = i;
                        foldIn();
                        return true;
                    }


            }

            GUILayout.Space(10);

            return false;

        }
        
        public static bool select(ref string val, List<string> lst)
        {
            var ind = -1;

            for (var i = 0; i < lst.Count; i++)
                if (lst[i] != null && lst[i].SameAs(val))
                {
                    ind = i;
                    break;
                }

            if (select(ref ind, lst))
            {
                val = lst[ind];
                return true;
            }

            return false;
        }
        
        public static bool select(this string text, ref string val, List<string> lst)
        {
            write(text);
            return select(ref val, lst);
        }
        
        public static bool select(this string text, int width, ref string val, List<string> lst)
        {
            write(text, width);
            return select(ref val, lst);
        }

#endregion

#region UnityObject

        private static readonly Dictionary<Type, List<Object>> objectsInScene = new Dictionary<Type, List<Object>>();

        static List<Object> FindObjects<T>() where T : Object
        {
            var objects = new List<Object>(Object.FindObjectsOfType<T>());

            objectsInScene[typeof(T)] = objects;

            return objects;
        }
        
        public static bool selectInScene<T>(this string label, ref T obj) where T : Object {

            List<Object> objects;

            if (!objectsInScene.TryGetValue(typeof(T), out objects))
                objects = FindObjects<T>();

            Object o = obj;

            var changed = false;

            if (label.select(label.ApproximateLength(), ref o, objects).changes(ref changed))
                obj = o as T;

            if (icon.Refresh.Click("Refresh List"))
                FindObjects<T>();

            return changed;
        }

        public static bool selectOrAdd<T>(this string label, int width, ref int selected, ref List<T> objs) where T : Object
        {
            label.write(width);
            return selectOrAdd(ref selected, ref objs);
        }

        public static bool selectOrAdd<T>(ref int selected, ref List<T> objcts) where T : Object
        {
            var changed = select_Index(ref selected, objcts);

            var tex = objcts.TryGet(selected);

            if (edit(ref tex, 50).changes(ref changed))
            {
                if (!tex)
                    selected = -1;
                else
                {
                    var ind = objcts.IndexOf(tex);
                    if (ind >= 0)
                        selected = ind;
                    else
                    {
                        selected = objcts.Count;
                        objcts.Add(tex);
                    }
                }
            }

            return changed;
        }
        
        public static bool select(ref int no, Texture[] tex)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.select(ref no, tex);

#endif

            if (tex.Length == 0) return false;

            checkLine();

            var tnames = new List<string>();
            var tnumbers = new List<int>();

            var curno = 0;
            for (var i = 0; i < tex.Length; i++)
                if (!tex[i].IsNullOrDestroyed_Obj())
                {
                    tnumbers.Add(i);
                    tnames.Add("{0}: {1}".F(i, tex[i].name));
                    if (no == i) curno = tnames.Count - 1;
                }

            var changed = false;

            if (select(ref curno, tnames.ToArray()).changes(ref changed) && curno >= 0 && curno < tnames.Count)
                no = tnumbers[curno];

            return changed;

        }

#endregion

#region Select Audio Clip

        public static bool select(this string text, ref AudioClip clip, List<AudioClip> lst, bool showIndex = false, bool stripSlashes = false, bool allowInsert = true) =>
            text.select(text, text.ApproximateLength(), ref clip, lst, showIndex, stripSlashes, allowInsert);

        public static bool select(this string text, int width, ref AudioClip clip, List<AudioClip> lst, bool showIndex = false, bool stripSlashes = false, bool allowInsert = true) =>
            text.select(text, width, ref clip, lst, showIndex, stripSlashes, allowInsert);

        public static bool select(this string text, string tip, ref AudioClip clip, List<AudioClip> lst, bool showIndex = false, bool stripSlashes = false, bool allowInsert = true) =>
            text.select(tip, text.ApproximateLength(), ref clip, lst, showIndex, stripSlashes, allowInsert);

        public static bool select(this string text, string tip, int width, ref AudioClip clip, List<AudioClip> lst, bool showIndex = false, bool stripSlashes = false, bool allowInsert = true)
        {
            text.write(tip, width);

           var ret = select(ref clip, lst, showIndex, stripSlashes, allowInsert);

           if (clip && icon.Play.Click(20))
               clip.Play();

           return ret;
        }

#endregion

#region Select Generic

        public static bool select<T>(this string text, int width, ref T value, List<T> lst, bool showIndex = false, bool stripSlashes = false, bool allowInsert = true)
        {
            write(text, width);
            return select(ref value, lst, showIndex, stripSlashes, allowInsert);
        }

        public static bool select<T>(this string text, ref T value, List<T> list, bool showIndex = false, bool stripSlashes = false, bool allowInsert = true)
        {
            write(text);
            return select(ref value, list, showIndex, stripSlashes, allowInsert);
        }

        public static bool select<T>(this string text, ref T value, T[] lst, bool showIndex = false)
        {
            write(text);
            return select(ref value, lst, showIndex);
        }

        public static bool select<T>(ref T val, T[] lst, bool showIndex = false, bool stripSlashes = false, bool dotsToSlashes = true)
        {
            checkLine();

            var namesList = new List<string>();
            var indexList = new List<int>();

            var current = -1;

            for (var j = 0; j < lst.Length; j++)
            {
                var tmp = lst[j];
                if (tmp.filterEditorDropdown().IsDefaultOrNull()) continue;

                if (!val.IsDefaultOrNull() && val.Equals(tmp))
                    current = namesList.Count;

                namesList.Add(CompileSelectionName(j, tmp, showIndex, stripSlashes, dotsToSlashes)); 
                indexList.Add(j);
            }

            if (selectFinal(val, ref current, namesList))
            {
                val = lst[indexList[current]];
                return true;
            }

            return false;

        }

        public static bool select<T>(ref T val, List<T> lst, bool showIndex = false, bool stripSlashes = false, bool allowInsert = true )
        {
            var changed = false;

            checkLine();

            var names = new List<string>();
            var indexes = new List<int>();

            var currentIndex = -1;

            bool notInTheList = true;

            var currentIsNull = val.IsDefaultOrNull();

            if (lst != null) {
                for (var i = 0; i < lst.Count; i++) {

                    var tmp = lst[i];
                    if (tmp.filterEditorDropdown().IsDefaultOrNull()) continue;

                    if (!currentIsNull && tmp.Equals(val))
                    {
                        currentIndex = names.Count;
                        notInTheList = false;
                    }

                    names.Add(CompileSelectionName(i, tmp, showIndex, stripSlashes));
                    indexes.Add(i);
                }

                if (selectFinal(val, ref currentIndex, names))
                {
                    val = lst[indexes[currentIndex]];
                    changed = true;
                }
                else if (allowInsert && notInTheList && !currentIsNull && icon.Insert.Click("Insert into list").changes(ref changed))
                    lst.Add(val);
            }
            else
                val.GetNameForInspector().write(); 



            return changed;

        }

        public static bool select_SameClass<T, G>(ref T val, List<G> lst, bool showIndex = false, bool stripSlashes = false, bool allowInsert = true) where T : class where G : class
        {
            var changed = false;
            var same = typeof(T) == typeof(G);

            checkLine();

            var namesList = new List<string>();
            var indexList = new List<int>();

            var current = -1;

            var notInTheList = true;

            var currentIsNull = val.IsNullOrDestroyed_Obj();

            for (var j = 0; j < lst.Count; j++)
            {
                var tmp = lst[j];
                if (tmp.filterEditorDropdown().IsDefaultOrNull() ||
                    (!same && !typeof(T).IsAssignableFrom(tmp.GetType()))) continue;

                if (!currentIsNull && tmp.Equals(val))
                {
                    current = namesList.Count;
                    notInTheList = false;
                }

                namesList.Add(CompileSelectionName(j, tmp, showIndex));
                indexList.Add(j);
            }

            if (selectFinal(val, ref current, namesList).changes(ref changed))
                val = lst[indexList[current]] as T;
            else if (allowInsert && notInTheList && !currentIsNull && icon.Insert.Click("Insert into list").changes(ref changed))
                lst.TryAdd(val);

            return changed;

        }

#endregion

#region Select Index

        public static bool select_Index<T>(this string text, ref int ind, List<T> lst, bool showIndex = false)
        {
            write(text);
            return select_Index(ref ind, lst, showIndex);
        }

        public static bool select_Index<T>(this string text, ref int ind, T[] lst)
        {
            write(text);
            return select_Index(ref ind, lst);
        }

        public static bool select_Index<T>(this string text, int width, ref int ind, T[] lst)
        {
            write(text, width);
            return select_Index(ref ind, lst);
        }

        public static bool select_Index<T>(this string text, string tip, int width, ref int ind, T[] lst, bool showIndex = false)
        {
            write(text, tip, width);
            return select_Index(ref ind, lst, showIndex);
        }

        public static bool select_Index<T>(this string text, string tip, ref int ind, List<T> lst)
        {
            write(text, tip);
            return select_Index(ref ind, lst);
        }

        public static bool select_Index<T>(this string text, int width, ref int ind, List<T> lst, bool showIndex = false)
        {
            write(text, width);
            return select_Index(ref ind, lst, showIndex);
        }

        public static bool select_Index<T>(this string text, string tip, int width, ref int ind, List<T> lst, bool showIndex = false)
        {
            write(text, tip, width);
            return select_Index(ref ind, lst, showIndex);
        }
        
        public static bool select_Index<T>(ref int ind, List<T> lst, int width) =>
#if UNITY_EDITOR
            (!paintingPlayAreaGui) ?
                ef.select(ref ind, lst, width) :
#endif
                select_Index(ref ind, lst);
        
        public static bool select_Index<T>(ref int ind, List<T> lst, bool showIndex = false, bool stripSlashes = false, bool dotsToSlashes = true)
        {

            checkLine();

            var namesList = new List<string>();
            var indexes = new List<int>();

            var current = -1;

            for (var j = 0; j < lst.Count; j++)
                if (!lst[j].filterEditorDropdown().IsNullOrDestroyed_Obj())
                {
                    if (ind == j)
                        current = indexes.Count;
                    namesList.Add(CompileSelectionName(j, lst[j], showIndex, stripSlashes, dotsToSlashes)); 
                    indexes.Add(j);
                }

            if (selectFinal(ind, ref current, namesList))
            {
                ind = indexes[current];
                return true;
            }

            return false;

        }

        public static bool select_Index<T>(ref int ind, T[] arr, bool showIndex = false, bool stripSlashes = false, bool dotsToSlashes = true)
        {

            checkLine();

            var lnms = new List<string>();

            if (arr.ClampIndexToLength(ref ind))
            {
                for (var i = 0; i < arr.Length; i++)
                    lnms.Add(CompileSelectionName(i, arr[i], showIndex, stripSlashes, dotsToSlashes));
            }

            return selectFinal(ind, ref ind, lnms);

        }
        
#endregion

#region With Lambda
        public static bool select<T>(this string label, ref int val, List<T> list, Func<T, bool> lambda, bool showIndex = false)
        {
            write(label);
            return select(ref val, list, lambda, showIndex);
        }

        public static bool select<T>(this string label, int width, ref int val, List<T> list, Func<T, bool> lambda, bool showIndex = false)
        {
            write(label, width);
            return select(ref val, list, lambda, showIndex);
        }

        public static bool select<T>(this string label, string tip, int width, ref int val, List<T> list, Func<T, bool> lambda, bool showIndex = false)
        {
            write(label, tip, width);
            return select(ref val, list, lambda, showIndex);
        }

        public static bool select<T>(this string text, ref T val, List<T> list, Func<T, bool> lambda, bool showIndex = false)
        {
            write(text);
            return select(ref val, list, lambda, showIndex);
        }

        public static bool select<T>(this string text, int width, ref T val, List<T> list, Func<T, bool> lambda, bool showIndex = false)
        {
            write(text, width);
            return select(ref val, list, lambda, showIndex);
        }

        public static bool select<T>(this string text, string hint, int width, ref T val, List<T> list, Func<T, bool> lambda, bool showIndex = false)
        {
            write(text, hint, width);
            return select(ref val, list, lambda, showIndex);
        }
        
        public static bool select<T>(ref int val, List<T> lst, Func<T, bool> lambda, bool showIndex = false, bool stripSlashes = false, bool dotsToSlashes = true)
        {

            checkLine();

            var names = new List<string>();
            var indexes = new List<int>();

            var current = -1;

            for (var j = 0; j < lst.Count; j++)
            {
                var tmp = lst[j];

                if (tmp.filterEditorDropdown().IsDefaultOrNull() || !lambda(tmp)) continue;

                if (val == j)
                    current = names.Count;
                names.Add(CompileSelectionName(j, tmp, showIndex, stripSlashes, dotsToSlashes));
                indexes.Add(j);
            }


            if (selectFinal(val, ref current, names))
            {
                val = indexes[current];
                return true;
            }

            return false;
        }

        public static bool select_IGotIndex<T>(ref int val, List<T> lst, Func<T, bool> lambda, bool showIndex = false, bool stripSlashes = false, bool dotsToSlashes = true) where T : IGotIndex
        {

            checkLine();

            var names = new List<string>();
            var indexes = new List<int>();

            var current = -1;

            foreach (var tmp in lst)
            {

                if (tmp.filterEditorDropdown().IsDefaultOrNull() || !lambda(tmp)) continue;

                var ind = tmp.IndexForPEGI;

                if (val == ind)
                    current = names.Count;
                names.Add(CompileSelectionName(ind, tmp, showIndex, stripSlashes, dotsToSlashes));
                indexes.Add(ind);

            }

            if (selectFinal(ref val, ref current, names))
            {
                val = indexes[current];
                return true;
            }

            return false;
        }

        public static bool select<T>(ref T val, List<T> lst, Func<T, bool> lambda, bool showIndex = false, bool stripSlashes = false, bool dotsToSlashes = true)
        {
            var changed = false;
            
            checkLine();

            var namesList = new List<string>();
            var indexList = new List<int>();

            var current = -1;

            for (var j = 0; j < lst.Count; j++)
            {
                var tmp = lst[j];
                if (tmp.filterEditorDropdown().IsDefaultOrNull() || !lambda(tmp)) continue;

                if (current == -1 && tmp.Equals(val))
                    current = namesList.Count;

                namesList.Add(CompileSelectionName(j, tmp, showIndex, stripSlashes, dotsToSlashes));
                indexList.Add(j);
            }

            if (selectFinal(val, ref current, namesList).changes(ref changed))
                val = lst[indexList[current]];
            
            return changed;

        }

#endregion

#region Countless
        public static bool select<T>(this string label, int width, ref int no, Countless<T> tree)
        {
            label.write(width);
            return select(ref no, tree);
        }

        public static bool select<T>(this string label, ref int no, Countless<T> tree)
        {
            label.write();
            return select(ref no, tree);
        }

        public static bool select<T>(ref int no, Countless<T> tree)
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.select(ref no, tree);
#endif
            
            
            List<int> inds;
            var objs = tree.GetAllObjs(out inds);
            var filtered = new List<string>();
            var tmpindex = -1;
            for (var i = 0; i < objs.Count; i++)
            {
                if (no == inds[i])
                    tmpindex = i;
                filtered.Add(objs[i].GetNameForInspector());
            }

            if (tmpindex == -1)
                filtered.Add(">>{0}<<".F(no.GetNameForInspector()));

            if (select(ref tmpindex, filtered.ToArray()) && tmpindex < inds.Count)
            {
                no = inds[tmpindex];
                return true;
            }
            return false;
            
        }

        public static bool select<T>(this string label, int width, ref int no, Countless<T> tree, Func<T, bool> lambda)
        {
            label.write(width);
            return select(ref no, tree, lambda);
        }

        public static bool select<T>(this string label, ref int no, Countless<T> tree, Func<T, bool> lambda)
        {
            label.write();
            return select(ref no, tree, lambda);
        }

        public static bool select<T>(ref int no, CountlessCfg<T> tree) where T : ICfg, new()
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.select(ref no, tree);
#endif

            List<int> inds;
            var objs = tree.GetAllObjs(out inds);
            var filtered = new List<string>();
            var tmpindex = -1;
            for (var i = 0; i < objs.Count; i++)
            {
                if (no == inds[i])
                    tmpindex = i;
                filtered.Add(objs[i].GetNameForInspector());
            }

            if (select(ref tmpindex, filtered.ToArray()))
            {
                no = inds[tmpindex];
                return true;
            }
            return false;

        }

        public static bool select<T>(ref int no, Countless<T> tree, Func<T, bool> lambda)
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.select(ref no, tree);
#endif

            List<int> unfinds;
            var indexes = new List<int>();
            var objects = tree.GetAllObjs(out unfinds);
            var namesList = new List<string>();
            var current = -1;
            var j = 0;
            for (var i = 0; i < objects.Count; i++)
            {

                var el = objects[i];

                if (el.filterEditorDropdown().IsNullOrDestroyed_Obj() || !lambda(el)) continue;

                indexes.Add(unfinds[i]);

                if (no == indexes[j])
                    current = j;

                namesList.Add(objects[i].GetNameForInspector());
                j++;
            }


            if (selectFinal(no, ref current, namesList))
            {
                no = indexes[current];
                return true;
            }
            return false;

        }

        public static bool select(CountlessInt cint, int ind, Dictionary<int, string> from)
        {

            var value = cint[ind];

            if (select(ref value, from))
            {
                cint[ind] = value;
                return true;
            }
            return false;
        }

#endregion

#region Enum
        public static bool selectEnum<T>(ref int current) => selectEnum(ref current, typeof(T));

        public static bool selectEnum<T>(this string label, int width, ref int current, List<int> options)
        {
            label.write(width);
            return selectEnum<T>(ref current, options);
        }

        public static bool selectEnum<T>(ref int eval, List<int> options, int width = -1) =>
            selectEnum(ref eval, typeof(T), options, width);

        public static bool selectEnum<T>(ref T eval, List<int> options, int width = -1)
        {
            var val = Convert.ToInt32(eval);

            if (selectEnum(ref val, typeof(T), options, width))
            {
                eval = (T)((object)val);
                return true;
            }

            return false;
        }

        public static bool selectEnum(ref int current, Type type, int width = -1)
        {
            checkLine();
            var tmpVal = -1;

            var names = Enum.GetNames(type);
            var val = (int[])Enum.GetValues(type);

            for (var i = 0; i < val.Length; i++)
                if (val[i] == current)
                    tmpVal = i;

            if (!select(ref tmpVal, names, width)) return false;

            current = val[tmpVal];
            return true;
        }

        public static bool selectEnumFlags(ref int current, Type type, int width = -1) {

            checkLine();

            var names = Enum.GetNames(type);
            var values = (int[])Enum.GetValues(type);

            Countless<string> sortedNames = new Countless<string>();

            int currentPower = 0;

            int toPow = 1;

            for (var i = 0; i < values.Length; i++) {
                var val = values[i];
                while (val > toPow) {
                    currentPower++;
                    toPow = (int)Mathf.Pow(2, currentPower);
                }

                if (val == toPow)
                    sortedNames[currentPower] = names[i];
            }

            string[] snms = new string[currentPower+1];

            for (int i = 0; i <= currentPower; i++)
                snms[i] = sortedNames[i];

            return selectFlags(ref current, snms, width);
        }

        public static bool selectEnum(ref int current, Type type, List<int> options, int width = -1)
        {
            checkLine();
            var tmpVal = -1;

            List<string> names = new List<string>();

            for (var i = 0; i < options.Count; i++)
            {
                var op = options[i];
                names.Add(Enum.GetName(type, op));
                if (options[i] == current)
                    tmpVal = i;
            }

            if (width == -1 ? select(ref tmpVal, names) : select_Index(ref tmpVal, names, width))
            {
                current = options[tmpVal];
                return true;
            }

            return false;
        }


        public static bool selectEnum<T>(this string text, string tip, int width, ref int eval) {
            write(text, tip, width);
            return selectEnum<T>(ref eval);
        }

        public static bool selectEnum<T>(this string text, int width, ref int eval) {
            write(text, width);
            return selectEnum<T>(ref eval);
        }

        public static bool selectEnum<T>(this string text, ref int eval) {
            write(text);
            return selectEnum<T>(ref eval);
        }
#endregion

#region Select Type

        public static bool select(ref Type val, List<Type> lst, string textForCurrent, bool showIndex = false, bool stripSlashes = false, bool dotsToSlashes = true)
        {
            checkLine();

            var names = new List<string>();
            var indexes = new List<int>();

            var current = -1;

            for (var j = 0; j < lst.Count; j++)
            {
                var tmp = lst[j];
                if (tmp.IsDefaultOrNull()) continue;

                if ((!val.IsDefaultOrNull()) && tmp == val)
                    current = names.Count;
                names.Add(CompileSelectionName(j, tmp, showIndex, stripSlashes, dotsToSlashes));
                indexes.Add(j);

            }

            if (current == -1 && val != null)
                names.Add(textForCurrent);

            if (select(ref current, names.ToArray()) && (current < indexes.Count))
            {
                val = lst[indexes[current]];
                return true;
            }

            return false;

        }
        
        public static bool selectType<T>(this string text, string hint, int width, ref T el, ElementData ed = null, bool keepTypeConfig = true) where T : class, IGotClassTag
        {
            text.write(hint, width);
            return selectType(ref el, ed, keepTypeConfig);
        }

        public static bool selectType<T>(this string text, int width, ref T el, ElementData ed = null, bool keepTypeConfig = true) where T : class, IGotClassTag
        {
            text.write(width);
            return selectType(ref el, ed, keepTypeConfig);
        }

        public static bool selectType<T>(this string text, ref T el, ElementData ed = null, bool keepTypeConfig = true) where T : class, IGotClassTag
        {
            text.write();
            return selectType(ref el, ed, keepTypeConfig);
        }

        public static bool selectType<T>(ref T el, TaggedTypesCfg types, ElementData ed = null, bool keepTypeConfig = true) where T : class, IGotClassTag {

            object obj = el;

            if (selectType_Obj<T>(ref obj, types, ed, keepTypeConfig))
            {
                el = obj as T;
                return true;
            }
            return false;

        }

        public static bool selectType<T>(ref T el, ElementData ed = null, bool keepTypeConfig = true) where T : class, IGotClassTag
        {
            object obj = el;

            if (selectType_Obj<T>(ref obj, ed, keepTypeConfig)) {
                el = obj as T;
                return true;
            }
            return false;
        }

        public static bool selectType_Obj<T>(ref object obj, ElementData ed = null, bool keepTypeConfig = true) where T : IGotClassTag {

            if (ed != null)
                return ed.SelectType<T>(ref obj, keepTypeConfig);
            
            var type = obj?.GetType();

            if (typeof(T).TryGetTaggedClasses().Select(ref type).nl()) {
                var previous = obj;
                obj = (T)Activator.CreateInstance(type);
                StdExtensions.TryCopy_Std_AndOtherData(previous, obj);
                return true;
            }

            return false;
        }

        public static bool selectType_Obj<T>(ref object obj, TaggedTypesCfg types, ElementData ed = null, bool keepTypeConfig = true) where T : IGotClassTag
        {

            if (ed != null)
                return ed.SelectType<T>(ref obj, types, keepTypeConfig);

            var type = obj?.GetType();

            if (types.Select(ref type).nl())
            {
                var previous = obj;
                obj = (T)Activator.CreateInstance(type);
                StdExtensions.TryCopy_Std_AndOtherData(previous, obj);
                return true;
            }

            return false;
        }

        public static bool selectTypeTag(this TaggedTypesCfg types, ref string tag) => select(ref tag, types.Keys);
#endregion

#region Dictionary
        public static bool select<G, T>(ref T val, Dictionary<G, T> dic, bool showIndex = false, bool stripSlashes = false, bool allowInsert = true) => select(ref val, new List<T>(dic.Values), showIndex, stripSlashes, allowInsert);

        public static bool select(ref int current, Dictionary<int, string> from)
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.select(ref current, from);

#endif

            var options = new string[from.Count];

            int ind = current;

            for (int i = 0; i < from.Count; i++)
            {
                var e = from.ElementAt(i);
                options[i] = e.Value;
                if (current == e.Key)
                    ind = i;
            }

            if (select(ref ind, options))
            {
                current = from.ElementAt(ind).Key;
                return true;
            }
            return false;

        }

        public static bool select(ref int current, Dictionary<int, string> from, int width)
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.select(ref current, from, width);
#endif

            var options = new string[from.Count];

            var ind = current;

            for (var i = 0; i < from.Count; i++)
            {
                var e = from.ElementAt(i);
                options[i] = e.Value;
                if (current == e.Key)
                    ind = i;
            }

            if (select(ref ind, options, width))
            {
                current = from.ElementAt(ind).Key;
                return true;
            }
            return false;

        }

#endregion
        
#region Select Or Edit
        public static bool select_or_edit_ColorPropertyName(this string name, int width, ref string property, Material material)
        {
            name.write(width);
            return select_or_edit_TexturePropertyName(ref property, material);
        }

        public static bool select_or_edit_ColorProperty(ref string property, Material material) {
            var lst = material.GetColorProperties();
            return lst.Count == 0 ?  edit(ref property) : select(ref property, lst);
        }

        public static bool select_or_edit_TexturePropertyName(this string name, int width, ref string property, Material material)
        {
            name.write(width);
            return select_or_edit_TexturePropertyName(ref property, material);
        }

        public static bool select_or_edit_TexturePropertyName(ref string property, Material material) {
            var lst = material.MyGetTexturePropertiesNames();
            return lst.Count == 0 ? edit(ref property) : select(ref property, lst);
        }

        public static bool select_or_edit_TextureProperty(ref ShaderProperty.TextureValue property, Material material) {
            var lst = material.MyGetTextureProperties();
            return select(ref property, lst, allowInsert:false);

        }
        
        public static bool select_or_edit<T>(string text, string hint, int width, ref T obj, List<T> list, bool showIndex = false, bool stripSlahes = false, bool allowInsert = true) where T : Object
        {
            if (list.IsNullOrEmpty()) {
                if (text != null)
                    write(text, hint, width);

                return edit(ref obj);
            }
           
            var changed = false;
            if (obj && icon.Delete.ClickUnFocus().changes(ref changed))
                obj = null;
            
            if (text != null)
                write(text, hint, width);

            select(ref obj, list, showIndex, stripSlahes, allowInsert).changes(ref changed);

            obj.ClickHighlight();

            return changed;
        }

        public static bool select_or_edit<T>(this string name, ref T obj, List<T> list, bool showIndex = false) where T : UnityEngine.Object 
            =>  select_or_edit(name, null, 0, ref obj, list, showIndex);
        
        public static bool select_or_edit<T>(this string name, int width, ref T obj, List<T> list, bool showIndex = false) where T : UnityEngine.Object
        => select_or_edit(name, null, width, ref obj, list, showIndex);

        public static bool select_or_edit<T>(ref T obj, List<T> list, bool showIndex = false) where T : UnityEngine.Object
            => select_or_edit(null, null, 0, ref obj, list, showIndex);

        public static bool select_or_edit<T>(this string name, ref int val, List<T> list, bool showIndex = false) =>
             list.IsNullOrEmpty() ?  name.edit(ref val) : name.select_Index(ref val, list, showIndex);
        
        public static bool select_or_edit(ref string val, List<string> list, bool showIndex = false, bool stripSlashes = true, bool allowInsert = true)
        {
            var changed = false;

            var gotList = !list.IsNullOrEmpty();

            var gotValue = !val.IsNullOrEmpty();

            if (gotList && gotValue && icon.Delete.ClickUnFocus(ref changed))
                val = "";

            if (!gotValue || !gotList)
                edit(ref val).changes(ref changed);

            if (gotList)
                select(ref val, list, showIndex, stripSlashes, allowInsert).changes(ref changed);

            return changed;
        }

        public static bool select_or_edit(this string name, ref string val, List<string> list, bool showIndex = false)
        {
            var changed = false;

            var gotList = !list.IsNullOrEmpty();

            var gotValue = !val.IsNullOrEmpty();

            if (gotList && gotValue && icon.Delete.ClickUnFocus(ref changed))
                val = "";

            if (!gotValue || !gotList)
                name.edit(ref val).changes(ref changed);

            if (gotList)
                name.select(ref val, list, showIndex).changes(ref changed);

            return changed;
        }

        public static bool select_or_edit<T>(this string name, int width, ref int val, List<T> list, bool showIndex = false) =>
            list.IsNullOrEmpty() ? name.edit(width, ref val) : name.select_Index(width, ref val, list, showIndex);
        
        public static bool select_or_edit<T>(this string name, string hint, int width, ref int val, List<T> list, bool showIndex = false) =>
            list.IsNullOrEmpty()  ? name.edit(hint, width, ref val) : name.select_Index(hint, width, ref val, list, showIndex);
        
        public static bool select_SameClass_or_edit<T, G>(this string text, string hint, int width, ref T obj, List<G> list) where T : UnityEngine.Object where G : class
        {
            if (list.IsNullOrEmpty())
                return edit(ref obj);
         
            var changed = false;
            
            if (obj && icon.Delete.ClickUnFocus().changes(ref changed))
                obj = null;
            
            if (text != null)
                write(text, hint, width);

            select_SameClass(ref obj, list).changes(ref changed);
            
            return changed;
            
        }

        public static bool select_SameClass_or_edit<T, G>(ref T obj, List<G> list) where T : UnityEngine.Object where G : class =>
             select_SameClass_or_edit(null, null, 0, ref obj, list);

        public static bool select_SameClass_or_edit<T, G>(this string name, ref T obj, List<G> list) where T : UnityEngine.Object where G : class =>
             select_SameClass_or_edit(name, null, 0, ref obj, list);

        public static bool select_SameClass_or_edit<T, G>(this string name, int width, ref T obj, List<G> list) where T : UnityEngine.Object where G : class =>
             select_SameClass_or_edit(name, null, width, ref obj, list);

#endregion

#region Select IGotIndex
        public static bool select_iGotIndex<T>(this string label, string tip, ref int ind, List<T> lst, bool showIndex = false) where T : IGotIndex
        {
            write(label, tip);
            return select_iGotIndex(ref ind, lst, showIndex);
        }

        public static bool select_iGotIndex<T>(this string label, string tip, int width, ref int ind, List<T> lst, bool showIndex = false) where T : IGotIndex
        {
            write(label, tip, width);
            return select_iGotIndex(ref ind, lst, showIndex);
        }

        public static bool select_iGotIndex<T>(this string label, int width, ref int ind, List<T> lst, bool showIndex = false) where T : IGotIndex
        {
            write(label, width);
            return select_iGotIndex(ref ind, lst, showIndex);
        }

        public static bool select_iGotIndex<T>(this string label, ref int ind, List<T> lst, bool showIndex = false) where T : IGotIndex
        {
            write(label);
            return select_iGotIndex(ref ind, lst, showIndex);
        }

        public static bool select_iGotIndex<T>(ref int ind, List<T> lst, bool showIndex = false) where T : IGotIndex
        {

            if (lst.IsNullOrEmpty())
            {
                return edit(ref ind);
            }

            var names = new List<string>();
            var indexes = new List<int>();

            var current = -1;

            foreach (var el in lst)
                if (!el.filterEditorDropdown().IsNullOrDestroyed_Obj())
                {
                    var index = el.IndexForPEGI;

                    if (ind == index)
                        current = indexes.Count;
                    names.Add((showIndex ? index + ": " : "") + el.GetNameForInspector());
                    indexes.Add(index);

                }
            
            if (selectFinal(ind, ref current, names))
            {
                ind = indexes[current];
                return true;
            }

            return false;
        }

        public static bool select_iGotIndex_SameClass<T, G>(this string label, string tip, int width, ref int ind, List<T> lst) where G : class, T where T : IGotIndex
        {
            write(label, tip, width);
            return select_iGotIndex_SameClass<T, G>(ref ind, lst);
        }

        public static bool select_iGotIndex_SameClass<T, G>(this string label, int width, ref int ind, List<T> lst) where G : class, T where T : IGotIndex
        {
            write(label, width);
            return select_iGotIndex_SameClass<T, G>(ref ind, lst);
        }

        public static bool select_iGotIndex_SameClass<T, G>(this string label, ref int ind, List<T> lst) where G : class, T where T : IGotIndex
        {
            write(label);
            return select_iGotIndex_SameClass<T, G>(ref ind, lst);
        }

        public static bool select_iGotIndex_SameClass<T, G>(ref int ind, List<T> lst) where G : class, T where T : IGotIndex
        {
            G val;
            return select_iGotIndex_SameClass(ref ind, lst, out val);
        }

        public static bool select_iGotIndex_SameClass<T, G>(this string label, ref int ind, List<T> lst, out G val) where G : class, T where T : IGotIndex
        {
            write(label);
            return select_iGotIndex_SameClass<T, G>(ref ind, lst, out val);
        }

        public static bool select_iGotIndex_SameClass<T, G>(ref int ind, List<T> lst, out G val) where G : class, T where T : IGotIndex
        {
            val = default(G);

            if (lst == null)
                return false;

            var names = new List<string>();
            var indexes = new List<int>();
            var els = new List<G>();
            var current = -1;

            foreach (var el in lst)
            {
                var g = el as G;

                if (g.filterEditorDropdown().IsNullOrDestroyed_Obj()) continue;

                var index = g.IndexForPEGI;

                if (ind == index)
                {
                    current = indexes.Count;
                    val = g;
                }
                names.Add(el.GetNameForInspector());
                indexes.Add(index);
                els.Add(g);

            }

            if (names.Count == 0)
                return edit(ref ind);

            if (selectFinal(ref ind, ref current, names))
            {
                ind = indexes[current];
                val = els[current];
                return true;
            }

            return false;
        }

#endregion

#region Select IGotName

        public static bool select_iGotName<T>(this string label, string tip, ref string name, List<T> lst) where T : IGotName
        {
            write(label, tip);
            return select_iGotName(ref name, lst);
        }

        public static bool select_iGotName<T>(this string label, string tip, int width, ref string name, List<T> lst) where T : IGotName
        {
            write(label, tip, width);
            return select_iGotName(ref name, lst);
        }

        public static bool select_iGotName<T>(this string label, int width, ref string name, List<T> lst) where T : IGotName {
            write(label, width);
            return select_iGotName(ref name, lst);
        }

        public static bool select_iGotName<T>(this string label, ref string name, List<T> lst) where T : IGotName
        {
            if (lst == null) 
                return false;

            write(label);
            return select_iGotName(ref name, lst);
        }

        public static bool select_iGotName<T>(ref string val, List<T> lst) where T : IGotName   {

            if (lst == null)
                return false;

            var namesList = new List<string>();

            var current = -1;


            foreach (var el in lst)
                if (!el.filterEditorDropdown().IsNullOrDestroyed_Obj())
                {
                    var name = el.NameForPEGI;

                    if (name == null) continue;
                    
                    if (val != null && val.SameAs(name))
                        current = namesList.Count;
                    namesList.Add(name);
                    
                }
            

            if (selectFinal(val, ref current, namesList))
            {
                val = namesList[current];
                return true;
            }

            return false;
        }
#endregion

#endregion

#region Foldout    
        public static bool foldout(this string txt, ref bool state, ref bool changed)
        {
            var before = state;

            txt.foldout(ref state);

            changed |= before != state;

            return isFoldedOutOrEntered;

        }

        public static bool foldout(this string txt, ref int selected, int current, ref bool changed)
        {
            var before = selected;

            txt.foldout(ref selected, current);

            changed |= before != selected;

            return isFoldedOutOrEntered;

        }

        public static bool foldout(this string txt, ref bool state)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.foldout(txt, ref state);
#endif

            checkLine();

            if (ClickUnFocus((state ? "..⏵ " : "..⏷ ") + txt))
                state = !state;


            isFoldedOutOrEntered = state;

            return isFoldedOutOrEntered;
            
        }

        public static bool foldout(this string txt, ref int selected, int current)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.foldout(txt, ref selected, current);
#endif
            
            checkLine();

            isFoldedOutOrEntered = (selected == current);

            if (ClickUnFocus((isFoldedOutOrEntered ? "..⏵ " : "..⏷ ") + txt))
            {
                if (isFoldedOutOrEntered)
                    selected = -1;
                else
                    selected = current;
            }

            isFoldedOutOrEntered = selected == current;

            return isFoldedOutOrEntered;
            
        }

        public static bool foldout(this Texture2D tex, string text, ref int selected, int current, ref bool changed)
        {
            var before = selected;

            tex.foldout(text, ref selected, current);

            changed |= before != selected;

            return isFoldedOutOrEntered;

        }

        public static bool foldout(this Texture2D tex, string text, ref bool state, ref bool changed)
        {
            var before = state;

            tex.foldout(text, ref state);

            changed |= before != state;

            return isFoldedOutOrEntered;

        }

        public static bool foldout(this Texture2D tex, string text, ref int selected, int current)
        {

            if (selected == current)
            {
                if (icon.FoldedOut.ClickUnFocus(text, 30))
                    selected = -1;
            }
            else
            {
                if (tex.ClickUnFocus(text, 25))
                    selected = current;
            }
            return selected == current;
        }

        public static bool foldout(this Texture2D tex, ref bool state)
        {

            if (state)
            {
                if (icon.FoldedOut.ClickUnFocus("Fold In", 30))
                    state = false;
            }
            else
            {
                if (tex.Click("Fold Out"))
                    state = true;
            }
            return state;
        }

        public static bool foldout(this Texture2D tex, string text, ref bool state)
        {

            if (state)
            {
                if (icon.FoldedOut.ClickUnFocus(text, 30))
                    state = false;
            }
            else
            {
                if (tex.Click(text))
                    state = true;
            }
            return state;
        }

        public static bool foldout(this icon ico, ref bool state) => ico.GetIcon().foldout(ref state);

        public static bool foldout(this icon ico, string text, ref bool state) => ico.GetIcon().foldout(text, ref state);

        public static bool foldout(this icon ico, string text, ref int selected, int current) => ico.GetIcon().foldout(text, ref selected, current);

        public static bool foldout(this string txt)
        {


#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.foldout(txt);
            
#endif

            foldout(txt, ref selectedFold, _elementIndex);

            _elementIndex++;

            return isFoldedOutOrEntered;
            

        }

        public static void foldIn() => selectedFold = -1;
#endregion

#region Tabs
        public static int tab(ref int selected, params icon[] icons) {
            nl();

            if (selected != -1) {
                if (icon.Close.ClickUnFocus())
                    selected = -1;
            } else
                icon.Next.write();
            

            for (var i=0; i<icons.Length; i++) {
                if (selected == i)
                    icons[i].write();
                else
                {
                    if (icons[i].ClickUnFocus())
                        selected = i;
                }
            }

            nl();
            return selected;
        }
#endregion

#region Enter & Exit
        public static bool enter(ref int enteredOne, int current, string tip = null)
        {
            

            if (enteredOne == current)
            {
                if (icon.Exit.ClickUnFocus())
                    enteredOne = -1;
            }
            else if (enteredOne == -1 && icon.Enter.ClickUnFocus())
                enteredOne = current;

            isFoldedOutOrEntered = (enteredOne == current);

            return isFoldedOutOrEntered;
        }

        public static bool enter(this icon ico, ref int enteredOne, int thisOne, string tip = null)
        {
            var outside = enteredOne == -1;
            
            if (enteredOne == thisOne) {
                if (icon.Exit.ClickUnFocus(tip))
                    enteredOne = -1;

            }
            else if (outside)  {
                if (ico.ClickUnFocus(tip))
                    enteredOne = thisOne;
            }

            isFoldedOutOrEntered = (enteredOne == thisOne);

            return isFoldedOutOrEntered;
        }
        
        public static bool enter(this icon ico, ref bool state, string tip = null) {

            if (state) {
                if (icon.Exit.ClickUnFocus(tip))
                    state = false;
            } else if (ico.ClickUnFocus(tip))
                    state = true;
            
            isFoldedOutOrEntered = state;

            return isFoldedOutOrEntered;
        }

        public static bool enter(this icon ico, string txt, ref bool state, bool showLabelIfTrue = true) {

            if (state)  {
                if (icon.Exit.ClickUnFocus("{0} {1}".F(icon.Exit.GetText(), txt)))
                    state = false;
            }
            else if (ico.ClickUnFocus("{0} {1}".F(icon.Enter.GetText(), txt)))
                    state = true;
            

            if ((showLabelIfTrue || !state) &&
                txt.ClickLabel(txt, -1, state ? PEGI_Styles.ExitLabel : PEGI_Styles.EnterLabel))
                state = !state;

            isFoldedOutOrEntered = state;

            return isFoldedOutOrEntered;
        }

        public static bool enter(this icon ico, string txt, ref int enteredOne, int thisOne, bool showLabelIfTrue = true, GUIStyle enterLabelStyle = null)
        {
            var outside = enteredOne == -1;

            var current = enteredOne == thisOne;

            if (current) {
                if (icon.Exit.ClickUnFocus("{0} {1}".F(icon.Exit.GetText(), txt)))
                    enteredOne = -1;
            }
            else if (outside && ico.ClickUnFocus(txt))
                    enteredOne = thisOne;
            

            if (((showLabelIfTrue && current) || outside) &&
                txt.ClickLabel(txt, -1, outside ? enterLabelStyle ?? PEGI_Styles.EnterLabel : PEGI_Styles.ExitLabel)) 
                enteredOne = outside ? thisOne : -1;
            

            isFoldedOutOrEntered = (enteredOne == thisOne);

            return isFoldedOutOrEntered;
        }
        
        private static bool enter_SkipToOnlyElement<T>(this List<T> list, ref int inspected)
        {

            if (inspected != -1 || list.Count == 0) return false;

            int tmp;
            icon ico;
            string msg;

            if (list.NeedsAttention()) {
                tmp = LastNeedAttentionIndex;
                ico = icon.Warning;
                msg = LastNeedAttentionMessage;
            }
            else {
                tmp = 0;
                ico = icon.Next;
                msg = "->";
            }

            var el = list.TryGet(tmp) as IPEGI;

            if (el != null && ico.Click(msg + el.GetNameForInspector())) {
                inspected = tmp;
                isFoldedOutOrEntered = true;
                return true;
            }
            return false;
        }

        private static bool enter_SkipToOnlyElement<T>(this List<T> list, ref int inspected, ref int enteredOne, int thisOne) {
            
            if (enteredOne == -1 && list.enter_SkipToOnlyElement(ref inspected)) 
                        enteredOne = thisOne;

            return enteredOne == thisOne;
        }

        private static bool enter_SkipToOnlyElement<T>(this List<T> list, ref int inspected, ref bool entered) {

            if (!entered && list.enter_SkipToOnlyElement(ref inspected)) 
                entered = true;

            return entered;
        }

        private static bool enter_HeaderPart<T>(this ListMetaData meta, ref List<T> list, ref bool entered, bool showLabelIfTrue = false)
        {
            int tmpEntered = entered ? 1 : -1;
            var ret = meta.enter_HeaderPart(ref list, ref tmpEntered, 1, showLabelIfTrue);
            entered = tmpEntered == 1;
            return ret;
        }

        private static bool enter_HeaderPart<T>(this ListMetaData meta, ref List<T> list, ref int enteredOne, int thisOne, bool showLabelIfTrue = false) {

            if (listIsNull(ref list)) {
                if (enteredOne == thisOne)
                    enteredOne = -1;
                return false;
            }

            var entered = enteredOne == thisOne;

            var ret = meta.Icon.enter(meta.label.AddCount(list, entered), ref enteredOne, thisOne, showLabelIfTrue, list.Count == 0 ? PEGI_Styles.WrappingText : null);
            
            ret |= list.enter_SkipToOnlyElement<T>(ref meta.inspected, ref enteredOne, thisOne);
            
            return ret;
        }

        public static bool enter(this string txt, ref bool state, bool showLabelIfTrue = true) => icon.Enter.enter(txt, ref state, showLabelIfTrue);

        public static bool enter(this string txt, ref int enteredOne, int thisOne, bool showLabelIfTrue = true, GUIStyle enterLabelStyle = null) => icon.Enter.enter(txt, ref enteredOne, thisOne, showLabelIfTrue, enterLabelStyle);

        public static bool enter(this string txt, ref int enteredOne, int thisOne, IList forAddCount) =>
            icon.Enter.enter(txt.AddCount(forAddCount), ref enteredOne, thisOne, enterLabelStyle: forAddCount.IsNullOrEmpty() ? PEGI_Styles.WrappingText : PEGI_Styles.EnterLabel);

        public static bool enter(this string txt, ref int enteredOne, int thisOne, IGotCount forAddCount) =>
            icon.Enter.enter(txt.AddCount(forAddCount), ref enteredOne, thisOne, enterLabelStyle: forAddCount.IsNullOrDestroyed_Obj() ? PEGI_Styles.EnterLabel :
                (forAddCount.CountForInspector() > 0 ? PEGI_Styles.EnterLabel : PEGI_Styles.WrappingText));

        private static bool enter_ListIcon<T>(this string txt, ref List<T> list, ref int enteredOne, int thisOne)
        {
            if (listIsNull(ref list)) {
                if (enteredOne == thisOne)
                    enteredOne = -1;
                return false;
            }

            return icon.List.enter(txt.AddCount(list, enteredOne == thisOne), ref enteredOne, thisOne, false, list.Count == 0 ? PEGI_Styles.WrappingText : null); 
        }

        private static bool enter_ListIcon<T>(this string txt, ref List<T> list,  ref int inspected, ref int enteredOne, int thisOne)
        {
            if (listIsNull(ref list)) {
                if (enteredOne == thisOne)
                    enteredOne = -1;
                return false;
            }

            var entered = enteredOne == thisOne;

            var ret = (inspected == -1 ? icon.List : icon.Next).enter(txt.AddCount(list, entered), ref enteredOne, thisOne, false, list.Count == 0 ? PEGI_Styles.WrappingText : null);
            ret |= list.enter_SkipToOnlyElement<T>(ref inspected, ref enteredOne, thisOne);
            return ret;
        }

        private static bool enter_ListIcon<T>(this string txt, ref List<T> list, ref int inspected, ref bool entered)
        {
            if (listIsNull(ref list))
            {
                if (entered)
                    entered = false;
                return false;
            }

            var ret = (inspected == -1 ? icon.List : icon.Next).enter(txt.AddCount(list, entered), ref entered, false);
            ret |= list.enter_SkipToOnlyElement<T>(ref inspected, ref entered);
            return ret;
        }

        private static string TryAddCount(this string txt, object obj) {
            var c = obj as IGotCount;
            if (!c.IsNullOrDestroyed_Obj())
                txt += " [{0}]".F(c.CountForInspector());

            return txt;
        }

        public static string AddCount(this string txt, IGotCount obj) {
            var isNull = obj.IsNullOrDestroyed_Obj();

            var cnt = !isNull ? obj.CountForInspector() : 0;
            return "{0} {1}".F(txt, !isNull ?
            (cnt > 0 ?
            (cnt == 1 ? "|" : "[{0}]".F(cnt))
            : "") : "null");
        }

        public static string AddCount(this string txt, IList lst, bool entered = false)
        {
            if (lst == null)
                return "{0} is NULL".F(txt);

            if (lst.Count > 1)
                return "{0} [{1}]".F(txt, lst.Count);

            if (lst.Count == 0)
                return "NO {0}".F(txt);

            if (!entered)  {

                var el = lst[0];

                if (!el.IsNullOrDestroyed_Obj())
                {

                    var nm = el as IGotDisplayName;

                    if (nm!= null)
                        return "{0}: {1}".F(txt, nm.NameForDisplayPEGI());

                    var n = el as IGotName;

                    if (n != null)
                        return "{0}: {1}".F(txt, n.NameForPEGI);
                    
                    return "{0}: {1}".F(txt, el.GetNameForInspector());
                    
                }
                else return "{0} one Null Element".F(txt);
            }
            else return "{0} [1]".F(txt);
        }

        public static bool enter_Inspect(this icon ico, string txt, IPEGI var, ref int enteredOne, int thisOne, bool showLabelIfTrue = true)
        {
            var changed = false;
            
            var il = IndentLevel;

            ico.enter(txt.TryAddCount(var), ref enteredOne, thisOne, showLabelIfTrue).nl_ifNotEntered();//) 

            IndentLevel = il;

            return (isFoldedOutOrEntered && var.Nested_Inspect()) || changed;
        }
        
        public static bool enter_Inspect(this IPEGI var, ref int enteredOne, int thisOne)
        {

            var lst = var as IPEGI_ListInspect;
            
            return lst!=null ? lst.enter_Inspect_AsList(ref enteredOne, thisOne) : 
                var.GetNameForInspector().enter_Inspect(var, ref enteredOne, thisOne);
        }

        public static bool enter_Inspect(this IPEGI var, ref bool entered)
        {

            var lst = var as IPEGI_ListInspect;

            return lst != null ? lst.enter_Inspect_AsList(ref entered) :
                var.GetNameForInspector().enter_Inspect(var, ref entered);
        }

        public static bool enter_Inspect(this string txt, IPEGI var, ref bool entered, bool showLabelIfTrue = true)
        {
            var changed = false;

            //if (
                    txt.TryAddCount(var).enter(ref entered, showLabelIfTrue);//)
               // var.Try_NameInspect().changes(ref changed);

            return (isFoldedOutOrEntered && var.Nested_Inspect()) || changed;
        }

        public static bool enter_Inspect(this string txt, IPEGI var, ref int enteredOne, int thisOne, bool showLabelIfTrue = true, GUIStyle enterLabelStyle = null)
        {
            var changed = false;
            
            txt.TryAddCount(var).enter(ref enteredOne, thisOne, showLabelIfTrue, enterLabelStyle);//)
           
            return (isFoldedOutOrEntered && var.Nested_Inspect()) || changed;
        }

        public static bool enter_Inspect(this string label, IPEGI_ListInspect var, ref int enteredOne, int thisOne)
        {
            if (enteredOne == -1 && label.TryAddCount(var).ClickLabel(style: PEGI_Styles.EnterLabel))
                enteredOne = thisOne;

            return var.enter_Inspect_AsList(ref enteredOne, thisOne, label);
        }

        public static bool enter_Inspect_AsList(this IPEGI_ListInspect var, ref bool entered)
        {
            var tmpEnt = entered ? 1 : -1;
            var ret = var.enter_Inspect_AsList(ref tmpEnt, 1);
            entered = tmpEnt == 1;
            return ret;
        }

        public static bool enter_Inspect_AsList(this IPEGI_ListInspect var, ref int enteredOne, int thisOne, string exitLabel = null)
        {
            var changed = false;

            var outside = enteredOne == -1;

            if (!var.IsNullOrDestroyed_Obj()) {

                if (outside)
                    var.InspectInList(null, thisOne, ref enteredOne).changes(ref changed);
                else if (enteredOne == thisOne)
                {

                    if (exitLabel.IsNullOrEmpty())
                        exitLabel = var.GetNameForInspector();

                    if (icon.Exit.ClickUnFocus("{0} L {1}".F(icon.Exit.GetText(), var))
                        || exitLabel.ClickLabel(icon.Exit.GetDescription(), style: PEGI_Styles.ExitLabel))
                        enteredOne = -1;
                    Try_Nested_Inspect(var).changes(ref changed);
                }
            }
            else if (enteredOne == thisOne)
                    enteredOne = -1;
            

            isFoldedOutOrEntered = enteredOne == thisOne;

            return changed;
        }

        public static bool TryEnter_Inspect(this string label, object obj, ref int enteredOne, int thisOne) {
            var changed = false;

            var ilpgi = obj as IPEGI_ListInspect;

            if (ilpgi != null)
                label.enter_Inspect(ilpgi, ref enteredOne, thisOne).nl_ifFolded(ref changed);
            else {
                var ipg = obj as IPEGI;
                label.conditional_enter_inspect(ipg != null, ipg, ref enteredOne, thisOne).nl_ifFolded(ref changed);
            }
            
            return changed;
        }

        public static bool conditional_enter(this icon ico, bool canEnter, ref int enteredOne, int thisOne, string exitLabel = "") {

            if (!canEnter && enteredOne == thisOne)
                enteredOne = -1;

            if (canEnter)
            {
                ico.enter(ref enteredOne, thisOne);
                if (enteredOne == thisOne && !exitLabel.IsNullOrEmpty() &&
                    exitLabel.ClickLabel(icon.Exit.GetDescription(), style: PEGI_Styles.ExitLabel))
                    enteredOne = -1;
            }
            else
                isFoldedOutOrEntered = false;

            return isFoldedOutOrEntered;
        }

        public static bool conditional_enter(this icon ico, string label, bool canEnter, ref int enteredOne, int thisOne, bool showLabelIfTrue = true, GUIStyle enterLabelStyle = null)
        {

            if (!canEnter && enteredOne == thisOne)
                enteredOne = -1;

            if (canEnter)
                ico.enter(label, ref enteredOne, thisOne, showLabelIfTrue, enterLabelStyle);
            else
                isFoldedOutOrEntered = false;

            return isFoldedOutOrEntered;
        }

        public static bool conditional_enter(this string label, bool canEnter, ref int enteredOne, int thisOne, bool showLabelIfTrue = true, GUIStyle enterLabelStyle = null) {

            if (!canEnter && enteredOne == thisOne)
            {
                if (icon.Back.Click() || "All Done here".Click(14))
                    enteredOne = -1;
            }
            else
            {

                if (canEnter)
                    label.enter(ref enteredOne, thisOne, showLabelIfTrue, enterLabelStyle);
                else
                    isFoldedOutOrEntered = false;
            }

            return isFoldedOutOrEntered;
        }

        public static bool conditional_enter(this string label, bool canEnter, ref bool entered, bool showLabelIfTrue = true)
        {

            if (!canEnter && entered)
            {
                if (icon.Back.Click() || "All Done here".Click(14))
                    entered = false;
            } else {

                if (canEnter)
                    label.enter(ref entered, showLabelIfTrue);
                else
                    isFoldedOutOrEntered = false;
            }

            return isFoldedOutOrEntered;
        }
        
        public static bool conditional_enter_inspect(this IPEGI_ListInspect obj, bool canEnter, ref int enteredOne, int thisOne) {
            if (!canEnter && enteredOne == thisOne)
                enteredOne = -1;

            if (canEnter)
                return obj.enter_Inspect_AsList(ref enteredOne, thisOne); 
            else
                isFoldedOutOrEntered = false;

            return false;
        }

        public static bool conditional_enter_inspect(this string label, bool canEnter, IPEGI obj, ref int enteredOne, int thisOne) {
            if (label.TryAddCount(obj).conditional_enter(canEnter, ref enteredOne, thisOne))
                return obj.Nested_Inspect();

            isFoldedOutOrEntered = enteredOne == thisOne;

            return false;
        }

        public static bool toggle_enter(this string label, ref bool val, ref int enteredOne, int thisOne, ref bool changed, bool showLabelWhenEntered = false)
        {

            if (enteredOne == -1)
                label.toggleIcon(ref val).changes(ref changed);
            
            if (val)
                enter(ref enteredOne, thisOne);
            else
                isFoldedOutOrEntered = false;

            if (enteredOne == thisOne)
                label.toggleIcon(ref val).changes(ref changed);

            if (!val && enteredOne == thisOne)
                enteredOne = -1;

            return isFoldedOutOrEntered;
        }

        public static bool enter_List<T>(this ListMetaData meta, ref List<T> list, ref int enteredOne, int thisOne)
        {
            var changed = false;

            if (meta.enter_HeaderPart(ref list, ref enteredOne, thisOne))
                meta.edit_List(ref list).nl(ref changed);

            return changed;
        }
        
        public static bool enter_List_UObj<T>(this string label, ref List<T> list, ref int inspectedElement, ref int enteredOne, int thisOne, List<T> selectFrom = null) where T : UnityEngine.Object
        {

            var changed = false;
            
            if (enter_ListIcon( label, ref list ,ref inspectedElement, ref enteredOne, thisOne))
                label.edit_List_UObj(ref list, ref inspectedElement, selectFrom).nl(ref changed);

            return changed;
        }

        public static bool enter_List_UObj<T>(this ListMetaData meta, ref List<T> list, ref int enteredOne, int thisOne, List<T> selectFrom = null) where T : UnityEngine.Object {

            var changed = false;
            
            if (meta.enter_HeaderPart(ref list, ref enteredOne, thisOne).changes(ref changed))  
                meta.edit_List_UObj(ref list, selectFrom).nl(ref changed);

            return changed;
        }

        public static bool enter_List_UObj<T>(this string label, ref List<T> list, ref int enteredOne, int thisOne, List<T> selectFrom = null) where T : UnityEngine.Object
        {

            var changed = false;
            
            var insp = -1;
            if (enter_ListIcon(label, ref list ,ref insp, ref enteredOne, thisOne)) // if (label.AddCount(list).enter(ref enteredOne, thisOne))
                label.edit_List_UObj(ref list, selectFrom).nl(ref changed);   

            return changed;
        }

        public static bool enter_List_SO<T>(this ListMetaData meta, ref List<T> list, ref int enteredOne, int thisOne) where T : ScriptableObject {

            var changed = false;

            if (meta.enter_HeaderPart(ref list, ref enteredOne, thisOne).changes(ref changed))
                meta.edit_List_SO(ref list).nl(ref changed);

            return changed;
        }

        public static bool enter_List<T>(this string label, ref List<T> list,  ref int enteredOne, int thisOne)
        {

            var changed = false;

            if (enter_ListIcon(label, ref list,  ref enteredOne, thisOne)) 
                    label.edit_List(ref list,  ref changed);

            return changed;
        }
        
        public static bool enter_List<T>(this string label, ref List<T> list, ref int inspectedElement, ref int enteredOne, int thisOne) 
        {
            var changed = false;
            label.enter_List(ref list, ref inspectedElement, ref enteredOne, thisOne, ref changed);
            return changed;
        }

        public static bool enter_List<T>(this string label, ref List<T> list, ref int enteredOne, int thisOne, Func<T, T> lambda) where T : new()
        {
            var changed = false;

            if (enter_ListIcon(label, ref list, ref enteredOne, thisOne))
                label.edit_List(ref list, lambda).nl(ref changed);

            return changed;
        }

        public static bool enter_List<T>(this ListMetaData meta, ref List<T> list, ref int enteredOne, int thisOne, Func<T, T> lambda) where T : new()
        {
            var changed = false;

            if (meta.enter_HeaderPart(ref list, ref enteredOne, thisOne))
                meta.label.edit_List(ref list, lambda).nl(ref changed);

            return changed;
        }

        public static T enter_List<T>(this string label, ref List<T> list, ref int inspectedElement, ref int enteredOne, int thisOne, ref bool changed) 
        {
            var tmp = default(T);
            
            if (enter_ListIcon(label, ref list, ref inspectedElement, ref enteredOne, thisOne)) //if (label.AddCount(list).enter(ref enteredOne, thisOne))
                tmp = label.edit_List(ref list, ref inspectedElement, ref changed);

            return tmp;
        }
        
        public static bool enter_List<T>(this string label, ref List<T> list, ref int inspectedElement, ref bool entered) 
        {

            var changed = false;
            
            if (enter_ListIcon(label, ref list,ref inspectedElement, ref entered))// if (label.AddCount(list).enter(ref entered))
                label.edit_List(ref list, ref inspectedElement).nl(ref changed);

            return changed;
        }
        
#region Tagged Types

        public static T enter_List<T>(this ListMetaData meta, ref List<T> list, ref int enteredOne, int thisOne, TaggedTypesCfg types, ref bool changed) =>
            meta.enter_HeaderPart(ref list, ref enteredOne, thisOne) ? meta.edit_List(ref list, types, ref changed) : default(T);

        public static T enter_List<T>(this ListMetaData meta, ref List<T> list, ref bool entered, TaggedTypesCfg types, ref bool changed) =>
            meta.enter_HeaderPart(ref list, ref entered) ? meta.edit_List(ref list, types, ref changed) : default(T);
        
#endregion

        public static bool conditional_enter_List<T>(this string label, bool canEnter, ref List<T> list, ref int inspectedElement, ref int enteredOne, int thisOne) 
        {

            var changed = false;

            if (!canEnter && enteredOne == thisOne)
                enteredOne = -1;

            if (canEnter)
                label.enter_List(ref list, ref inspectedElement, ref enteredOne, thisOne).changes(ref changed);
            else
                isFoldedOutOrEntered = false;

            return changed;

        }

        public static bool conditional_enter_List<T>(this ListMetaData meta, bool canEnter, ref List<T> list, ref int enteredOne, int thisOne) {

            var changed = false;

            if (!canEnter && enteredOne == thisOne)
                enteredOne = -1;

            if (canEnter)
                meta.enter_List(ref list, ref enteredOne, thisOne).changes(ref changed);
            else
                isFoldedOutOrEntered = false;

            return changed;
        }
        
#endregion

#region Click
        public const int defaultButtonSize = 26;

        private const int maxWidthForPlaytimeButtonText = 100;

        public static bool ClickLink(this string label, string link, string tip = null) {

            if (tip == null)
                tip = "Go To: {0}".F(link);

            if (label.Click(tip, 12))
            {
                Application.OpenURL(link);
                return true;
            }

            return false;
        }

        public static bool ClickDuplicate(ref Material mat, string newName = null, string folder = "Materials") => ClickDuplicate(ref mat, folder, ".mat", newName);

        public static bool ClickDuplicate<T>(ref T obj, string folder, string extension, string newName = null) where T: UnityEngine.Object
        {
           
            if (!obj) return false; 

            var changed = false;
            
#if UNITY_EDITOR
            var path = AssetDatabase.GetAssetPath(obj);
            if (icon.Copy.Click("{0} Duplicate at {1}".F(obj, path)).changes(ref changed)) {
                if (path.IsNullOrEmpty())
                {
                    obj = Object.Instantiate(obj);
                    if (!newName.IsNullOrEmpty())
                        obj.name = newName;

                    obj.SaveAsset(folder, extension, true);
                }
                else
                {
                    var newPath =
                        AssetDatabase.GenerateUniqueAssetPath(newName.IsNullOrEmpty()
                            ? path
                            : path.Replace(obj.name, newName)); 

                    AssetDatabase.CopyAsset(path, newPath);
                    obj = AssetDatabase.LoadAssetAtPath<T>(newPath);
                }
            }
#else
             if (icon.Copy.Click("Create Instance of {0}".F(obj)))
                obj = GameObject.Instantiate(obj);

#endif
            

            return changed;
        }

        public static void Lock_UnlockWindowClick(UnityEngine.Object obj)
        {
#if UNITY_EDITOR
            if (ActiveEditorTracker.sharedTracker.isLocked == false && icon.Unlock.ClickUnFocus("Lock Inspector Window"))
            {
                QcUnity.FocusOn(ef.serObj.targetObject);
                ActiveEditorTracker.sharedTracker.isLocked = true;
            }
        
            if (ActiveEditorTracker.sharedTracker.isLocked && icon.Lock.ClickUnFocus("Unlock Inspector Window"))
            {
                ActiveEditorTracker.sharedTracker.isLocked = false;
                QcUnity.FocusOn(obj);
            }
#endif
        }

        public static void UnlockInspectorWindowIfLocked(GameObject go)
        {
#if UNITY_EDITOR
            if (ActiveEditorTracker.sharedTracker.isLocked && (Selection.objects.IsNullOrEmpty() || Selection.objects.Contains(go)))
            {
                ActiveEditorTracker.sharedTracker.isLocked = false;
                QcUnity.FocusOn(go);
            }
#endif
        }
        
        private static string _confirmTag;
        private static object _objectToConfirm;
        private static string _confirmationDetails;

        private static void RequestConfirmation(string tag, object forObject = null, string details = "")
        {
            _confirmTag = tag;
            _objectToConfirm = forObject;
            _confirmationDetails = details;
        }

        private static void CloseConfirmation()
        {
            _confirmTag = null;
            _objectToConfirm = null;
        }

        public static bool IsConfirmingRequestedFor(string tag) => (!_confirmTag.IsNullOrEmpty() && _confirmTag.Equals(tag));
        
        public static bool IsConfirmingRequestedFor(string confirmationTag, object obj) =>
            confirmationTag.Equals(_confirmTag) && ((_objectToConfirm != null && _objectToConfirm.Equals(obj)) ||
                                                    (obj == null && _objectToConfirm == null));

        private static bool ConfirmClick() {
            
            nl();

            if (icon.Close.Click(Msg.No.GetText(), 30))
                CloseConfirmation();

            (_confirmationDetails.IsNullOrEmpty() ? Msg.AreYouSure.GetText() : _confirmationDetails).writeHint(false);

            if (icon.Done.Click(Msg.Yes.GetText(), 30))
            {
                CloseConfirmation();
                return true;
            }

            nl();
            

            return false;
        }

        public static bool ClickConfirm(this Texture tex, string confirmationTag)
        {

            if (confirmationTag.Equals(_confirmTag))
                return ConfirmClick();

            if (tex.ClickUnFocus())
                RequestConfirmation(confirmationTag);

            return false;
        }
        
        public static bool ClickConfirm(this string label, string confirmationTag, string tip = "") {

            if (confirmationTag.Equals(_confirmTag))
                return ConfirmClick();

            if (label.ClickUnFocus(tip))
                RequestConfirmation(confirmationTag, details: tip);

            return false;
        }

        public static bool ClickConfirm(this icon icon, string confirmationTag, string tip = "", int width = defaultButtonSize) {

            if (confirmationTag.Equals(_confirmTag))
                return ConfirmClick();

            if (icon.ClickUnFocus(tip, width))
                RequestConfirmation(confirmationTag, details: tip);

            return false;
        }

        public static bool ClickConfirm(this icon icon, string confirmationTag, object obj, string tip = "", int width = defaultButtonSize) {

            if (IsConfirmingRequestedFor(confirmationTag, obj))
                return ConfirmClick();

            if (icon.ClickUnFocus(tip, width))
                RequestConfirmation(confirmationTag, obj, tip);

            return false;
        }

        public static bool ClickLabel(this string label, string hint = "ClickAble Text", int width = -1, GUIStyle style = null)
        {
            SetBgColor(Color.clear);

            if (style == null)
                style = PEGI_Styles.ClickableText;

            textAndTip.text = label;
            textAndTip.tooltip = hint;

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return (width == -1 ? ef.Click(textAndTip, style) : ef.Click(textAndTip, width, style)).UnFocus().RestoreBGColor();
#endif
            
            checkLine();

            return (width ==-1 ? GUILayout.Button(textAndTip, style) : GUILayout.Button(textAndTip, style, GUILayout.MaxWidth(width))).DirtyUnFocus().PreviousBgColor();
        }

        public static bool ClickUnFocus(this Texture tex, int width = defaultButtonSize)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.Click(tex, width).UnFocus();
#endif

            checkLine();
            return GUILayout.Button(tex, GUILayout.MaxWidth(width + 5), GUILayout.MaxHeight(width)).DirtyUnFocus();
        }

        public static bool ClickUnFocus(this Texture tex, string tip, int width = defaultButtonSize) =>
             Click(tex, tip, width).UnFocus();

        public static bool ClickUnFocus(this Texture tex, string tip, int width, int height) =>
                Click(tex, tip, width, height).UnFocus();
        
        public static bool ClickUnFocus(this string text)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.Click(text).UnFocus();
#endif
            checkLine();
            return GUILayout.Button(text, GUILayout.MaxWidth(maxWidthForPlaytimeButtonText)).DirtyUnFocus();
        }

        public static bool ClickUnFocus(this string text, string tip)
        {

            var cntnt = TextAndTip(text, tip);

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.Click(cntnt).UnFocus();
#endif
            checkLine();
            return GUILayout.Button(cntnt, GUILayout.MaxWidth(maxWidthForPlaytimeButtonText)).DirtyUnFocus();
        }


        public static bool Click(this string label, int fontSize)
        {
            textAndTip.text = label;
            textAndTip.tooltip = label;
            return textAndTip.ClickText(PEGI_Styles.ScalableBlueText(fontSize));
        }

        public static bool Click(this string label, string hint, int fontSize) => TextAndTip(label, hint).ClickText(PEGI_Styles.ScalableBlueText(fontSize));
        
        public static bool Click(this string label, GUIStyle style) => TextAndTip(label).ClickText(style); 
        
        private static bool ClickText(this GUIContent content, GUIStyle style)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.Click(content, style);
#endif
            checkLine();
            return GUILayout.Button(content, style, GUILayout.MaxWidth(maxWidthForPlaytimeButtonText)).Dirty();
        }

        private static bool ClickImage(this GUIContent content, int width, GUIStyle style) =>
            content.ClickImage(width, width, style);

        private static bool ClickImage(this GUIContent content, int width, int height, GUIStyle style)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.ClickImage(content, width, style);
#endif
            checkLine();

           // if (style == null)
             //   style = PEGI_Styles.ImageButton;
            

            return (style != null ? 
                    GUILayout.Button(content, style, GUILayout.MaxWidth(width+5),  GUILayout.MaxHeight(height)) :
                    GUILayout.Button(content,  GUILayout.MaxWidth(width + 5), GUILayout.MaxHeight(height)))
                .Dirty();
        }

        public static bool Click(this string text, ref bool changed) => text.Click().changes(ref changed);

        public static bool Click(this string text)
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.Click(text);
#endif
            checkLine();
            return GUILayout.Button(text, GUILayout.MaxWidth(maxWidthForPlaytimeButtonText)).Dirty();
        }

        public static bool Click(this string text, string tip, ref bool changed) => text.Click(tip).changes(ref changed);
        
        public static bool Click(this string text, string tip)
        {
            var cnt = TextAndTip(text, tip);
#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.Click(cnt);
#endif
            checkLine();
            return GUILayout.Button(cnt, GUILayout.MaxWidth(maxWidthForPlaytimeButtonText)).Dirty();
        }

        private static Texture GetTexture_orEmpty(this Sprite sp) => sp ? sp.texture : icon.Empty.GetIcon();

        public static bool Click(this Sprite img, int size = defaultButtonSize) 
            => img.GetTexture_orEmpty().Click(size);
       
        public static bool Click(this Sprite img, string tip, int size = defaultButtonSize)
            => img.GetTexture_orEmpty().Click(tip, size);

        public static bool Click(this Sprite img, string tip, int width, int height)
            => img.GetTexture_orEmpty().Click(tip, width, height);
        
        public static bool Click(this Texture img, int size = defaultButtonSize )
        {

            if (!img) img = icon.Empty.GetIcon();

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.Click(img, size);
#endif
            
            checkLine();
            return GUILayout.Button(img, GUILayout.MaxWidth(size + 5), GUILayout.MaxHeight(size)).Dirty();

        }

        public static bool Click(this Texture img, string tip, int size = defaultButtonSize)  {

            if (!img) img = icon.Empty.GetIcon();

            var cnt = ImageAndTip(img, tip);

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.ClickImage(cnt, size);
#endif
            
                checkLine();
                return GUILayout.Button(cnt, GUILayout.MaxWidth(size + 5), GUILayout.MaxHeight(size)).Dirty();
        }
        
        public static bool Click(this Texture img, string tip, int width, int height)
        {
            if (!img) img = icon.Empty.GetIcon();

            var cnt = ImageAndTip(img, tip);

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.ClickImage(cnt, width, height);
#endif
                checkLine();
                return GUILayout.Button(cnt, GUILayout.MaxWidth(width), GUILayout.MaxHeight(height)).Dirty();
        }

        public static bool Click(this icon icon) => Click(icon.GetIcon(), icon.GetText());

        public static bool Click(this icon icon, ref bool changed) => Click(icon.GetIcon(), icon.GetText()).changes(ref changed);

        public static bool ClickUnFocus(this icon icon) => ClickUnFocus(icon.GetIcon(), icon.GetText());
        
        public static bool ClickUnFocus(this icon icon, ref bool changed) => ClickUnFocus(icon.GetIcon(), icon.GetText()).changes(ref changed);
        
        public static bool ClickUnFocus(this icon icon, int size = defaultButtonSize) => ClickUnFocus(icon.GetIcon(), icon.GetText(), size);

        public static bool ClickUnFocus(this icon icon, string tip, int size = defaultButtonSize)
        {
            if (tip == null)
                tip = icon.GetText();

            return ClickUnFocus(icon.GetIcon(), tip, size);
        }

        public static bool ClickUnFocus(this icon icon, string tip, int width, int height) => ClickUnFocus(icon.GetIcon(), tip, width, height);

        public static bool Click(this icon icon, int size) => Click(icon.GetIcon(), size);

        public static bool Click(this icon icon, string tip, int width, int height) => Click(icon.GetIcon(), tip, width, height);

        public static bool Click(this icon icon, string tip, ref bool changed, int size = defaultButtonSize) => Click(icon.GetIcon(), tip, size).changes(ref changed);

        public static bool Click(this icon icon, string tip, int size = defaultButtonSize) => Click(icon.GetIcon(), tip, size);
        
        public static bool Click(this Color col) => icon.Empty.GUIColor(col).BgColor(Color.clear).Click().RestoreGUIColor().RestoreBGColor();

        public static bool Click(this Color col, string tip, int size = defaultButtonSize) => icon.Empty.GUIColor(col).BgColor(Color.clear).Click(tip, size).RestoreGUIColor().RestoreBGColor();

        public static bool TryClickHighlight(object obj, int width = defaultButtonSize)
        {
#if UNITY_EDITOR
            var uo = obj as UnityEngine.Object;
            if (uo)
                uo.ClickHighlight(width);
#endif

            return false;
        }

        public static bool ClickHighlight(this Sprite sp, int width = defaultButtonSize)
        {
#if UNITY_EDITOR
            if (sp  && sp.Click(Msg.HighlightElement.GetText(), width))
            {
                EditorGUIUtility.PingObject(sp);
                return true;
            }
#endif
            return false;
        }

        public static bool ClickHighlight(this Texture tex, int width = defaultButtonSize)
        {
#if UNITY_EDITOR
            if (tex && tex.Click(Msg.HighlightElement.GetText(), width))
            {
                EditorGUIUtility.PingObject(tex);
                return true;
            }
#endif

            return false;
        }
        
        public static bool ClickHighlight(this Object obj, int width = defaultButtonSize) =>
           obj.ClickHighlight(icon.Search.GetIcon(), width);

        public static bool ClickHighlight(this Object obj, Texture tex, int width = defaultButtonSize)
        {
#if UNITY_EDITOR
            if (obj && tex.Click(Msg.HighlightElement.GetText()))
            {
                EditorGUIUtility.PingObject(obj);
                return true;
            }
#endif

            return false;
        }

        public static bool ClickHighlight(this Object obj, icon icon, int width = defaultButtonSize)
        {
#if UNITY_EDITOR
            if (obj && icon.Click(Msg.HighlightElement.GetText()))
            {
                EditorGUIUtility.PingObject(obj);
                return true;
            }
#endif

            return false;
        }

        public static bool ClickHighlight(this Object obj, string hint, icon icon = icon.Search, int width = defaultButtonSize)
        {
#if UNITY_EDITOR
            if (obj && icon.Click(hint)) {
                EditorGUIUtility.PingObject(obj);
                return true;
            }
#endif

            return false;
        }
        
        public static bool Click_Enter_Attention_Highlight<T>(this T obj, ref bool changed, icon icon = icon.Enter, string hint = "", bool canBeNull = true) where T : UnityEngine.Object, INeedAttention
        {
            var ch = obj.Click_Enter_Attention(icon, hint, canBeNull).changes(ref changed);
            obj.ClickHighlight().changes(ref changed);
            return ch;
        }

        public static bool Click_Enter_Attention(this INeedAttention attention, icon icon = icon.Enter, string hint = "", bool canBeNull = true)
        {
            if (attention.IsNullOrDestroyed_Obj())   {
                if (!canBeNull)
                    return icon.Warning.ClickUnFocus("Object is null; {0}".F(hint));
            }
            else
            {

                var msg = attention.NeedAttention();
                if (msg != null)
                    return icon.Warning.ClickUnFocus(msg);
            }

            if (hint.IsNullOrEmpty())
                hint = icon.GetText();

            return icon.ClickUnFocus(hint);
        }

        public static bool Click_Enter_Attention(this INeedAttention attention, Texture tex, string hint = "", bool canBeNull = true)
        {
            if (attention.IsNullOrDestroyed_Obj())
            {
                if (!canBeNull)
                    return icon.Warning.ClickUnFocus("Object is null; {0}".F(hint));
            }
            else
            {

                var msg = attention.NeedAttention();
                if (msg != null)
                    return icon.Warning.ClickUnFocus(msg);
            }

            if (hint.IsNullOrEmpty())
                hint = tex ? tex.ToString() : "Null Texture";

            return tex ? tex.ClickUnFocus(hint) : icon.Enter.ClickUnFocus(hint);
        }

#endregion

#region Toggle
        private const int DefaultToggleIconSize = 34;

        public static bool toggleInt(ref int val)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.toggleInt(ref val);
#endif
            
            var before = val > 0;
            if (!toggle(ref before)) return false;
            val = before ? 1 : 0;
            return true;
        }

        public static bool toggle(this icon icon, ref int selected, int current)
          => icon.toggle(icon.GetText(), ref selected, current);

        public static bool toggle(this icon icon, string label, ref int selected, int current)
        {
            if (selected == current)
                icon.write(label);
            else if (icon.Click(label)) {
                selected = current;
                return true;
                }

            return false;
        }
        
        public static bool toggle(ref bool val)
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.toggle(ref val);
#endif

            bc();
            val = GUILayout.Toggle(val, "", GUILayout.MaxWidth(30));
            return ec();
            
        }

        public static bool toggle(ref bool val, string text, int width)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
            {
                ef.write(text, width);
                return ef.toggle(ref val);
            }
#endif

            bc();
            val = GUILayout.Toggle(val, text);
            return ec();

        }

        public static bool toggle(ref bool val, string text, string tip)
        {
            var cnt = TextAndTip(text, tip);
#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.toggle(ref val, cnt);
            
#endif
            bc();
            val = GUILayout.Toggle(val, cnt);
            return ec();
        }

        public static bool toggle(ref bool val, icon TrueIcon, icon FalseIcon, string tip, int width = defaultButtonSize, GUIStyle style = null) 
            => toggle(ref val, TrueIcon.GetIcon(), FalseIcon.GetIcon(), tip, width, style);

        public static bool toggle(ref bool val, icon TrueIcon, icon FalseIcon, GUIStyle style = null) => toggle(ref val, TrueIcon.GetIcon(), FalseIcon.GetIcon(), "", defaultButtonSize, style);

        public static bool toggleVisibilityIcon(ref bool val, string hint, int width = DefaultToggleIconSize)
        {
            SetBgColor(Color.clear);

            var changed = toggle(ref val, icon.Show, icon.Hide, hint, width, PEGI_Styles.ToggleButton).PreviousBgColor();

            return changed;
        }


        public static bool toggleVisibilityIcon(this string label, string hint, ref bool val, bool dontHideTextWhenOn = false)
        {
            SetBgColor(Color.clear);

            var changed = toggle(ref val, icon.Show, icon.Hide, hint, DefaultToggleIconSize, PEGI_Styles.ToggleButton).PreviousBgColor();

            if (!val || dontHideTextWhenOn) label.write(hint, PEGI_Styles.ToggleLabel(val));

            return changed;
        }

        public static bool toggleVisibilityIcon(this string label, ref bool val, bool showTextWhenTrue = false)
        {
            var changed = toggle(ref val, icon.Show.BgColor(Color.clear), icon.Hide, label, DefaultToggleIconSize, PEGI_Styles.ToggleButton).PreviousBgColor();

            if (!val || showTextWhenTrue) label.write(PEGI_Styles.ToggleLabel(val));

            return changed;
        }

        public static bool toggleIcon(ref bool val, string hint = "Toggle On/Off") => toggle(ref val, icon.True.BgColor(Color.clear), icon.False, hint, DefaultToggleIconSize, PEGI_Styles.ToggleButton).PreviousBgColor();

        public static bool toggleIcon(ref int val, string hint = "Toggle On/Off") {
            var boo = val != 0;

            if (toggle(ref boo, icon.True.BgColor(Color.clear), icon.False, hint, DefaultToggleIconSize, PEGI_Styles.ToggleButton).PreviousBgColor())
            {
                val = boo ? 1 : 0;
                return true;
            }

            return false;
        }

        public static bool toggleIcon(this string label, string hint, ref bool val, bool hideTextWhenTrue = false)
        {
            SetBgColor(Color.clear);

            var ret = toggle(ref val, icon.True, icon.False, hint, DefaultToggleIconSize, PEGI_Styles.ToggleButton).PreviousBgColor();

            if ((!val || !hideTextWhenTrue) && label.ClickLabel(hint,-1, PEGI_Styles.ToggleLabel(val)).changes(ref ret)) 
                val = !val;
            
            return ret;
        }

        public static bool toggleIcon(this string label, ref bool val, bool hideTextWhenTrue = false)
        {
            var changed = toggle(ref val, icon.True.BgColor(Color.clear), icon.False, label, DefaultToggleIconSize, PEGI_Styles.ToggleButton).PreviousBgColor();

            if ((!val || !hideTextWhenTrue) && label.ClickLabel(label, -1, PEGI_Styles.ToggleLabel(val)).changes(ref changed))
                val = !val;
            
            return changed;
        }

        public static bool toggleIcon(this string labelIfFalse, ref bool val, string labelIfTrue)
            => (val ? labelIfTrue : labelIfFalse).toggleIcon(ref val);

        public static bool toggle(ref bool val, Texture2D TrueIcon, Texture2D FalseIcon, string tip, int width = defaultButtonSize, GUIStyle style = null) {

            if (val)  {
                if (ClickImage(ImageAndTip(TrueIcon, tip), width, style))
                {
                    val = false;
                    return true;
                }

            }
            else if (ClickImage(ImageAndTip(FalseIcon, tip), width, style))
            {
                val = true;
                return true;
            }

            return false;
        }

        public static bool toggle(ref bool val, string text, string tip, int width)
        {
            var cnt = TextAndTip( text,  tip);
#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
            {
                ef.write(cnt, width);
                return ef.toggle(ref val);
            }
        
#endif

            bc();
            val = GUILayout.Toggle(val, cnt);
            return ec();

        }

        public static bool toggle(int ind, CountlessBool tb)
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.toggle(ind, tb);
#endif
            var has = tb[ind];

            if (!toggle(ref has)) return false;

            tb.Toggle(ind);
            return true;
        }

        public static bool toggle(this icon img, ref bool val)
        {
            write(img.GetIcon(), 25);
            return toggle(ref val);
        }

        public static bool toggle(this Texture img, ref bool val)
        {
            write(img, 25);
            return toggle(ref val);
        }

        public static bool toggleInt(this string text, ref int val)
        {
            write(text);
            return toggleInt(ref val);
        }

        public static bool toggleInt(this string text, string hint, ref int val)
        {
            write(text, hint);
            return toggleInt(ref val);
        }

        public static bool toggle(this string text, ref bool val)
        {
            write(text);
            return toggle(ref val);
        }

        public static bool toggle(this string text, int width, ref bool val)
        {
            write(text, width);
            return toggle(ref val);
        }

        public static bool toggle(this string text, string tip, ref bool val)
        {
            write(text, tip);
            return toggle(ref val);
        }

        public static bool toggle(this string text, string tip, int width, ref bool val)
        {
            write(text, tip, width);
            return toggle(ref val);
        }
        
        public static bool toggleDefaultInspector() =>
#if UNITY_EDITOR
                 ef.toggleDefaultInspector();
#else
                false;
#endif


#endregion

#region Edit

#region Audio Clip

        public static bool edit(this string label, int width, ref AudioClip field, float offset = 0)
        {
            label.write(width);
            return edit(ref field, offset);
        }

        public static bool edit(this string label, ref AudioClip field, float offset = 0)
        {
            label.write(label.ApproximateLength());
            return edit(ref field, offset);
        }

        public static bool edit(ref AudioClip clip, int width, float offset = 0)
        {

            var ret =
#if UNITY_EDITOR
                !paintingPlayAreaGui ? ef.edit(ref clip, width) :
#endif
                    false;

            clip.PlayButton(offset);

            return ret;
        }

        public static bool edit(ref AudioClip clip, float offset = 0)
        {

            var ret =
#if UNITY_EDITOR
                !paintingPlayAreaGui ? ef.edit(ref clip) :
#endif
                    false;

            clip.PlayButton(offset);

            return ret;
        }

        private static void PlayButton(this AudioClip clip, float offset = 0)
        {
            if (clip && icon.Play.Click(20))
            {
                var req = clip.Play();
                if (offset > 0)
                    req.FromTimeOffset(offset);
            }
        }

#endregion
        
#region UnityObject

        public static bool edit<T>(ref T field, int width) where T : Object =>
#if UNITY_EDITOR
                !paintingPlayAreaGui ?  ef.edit(ref field, width) :
#endif
            false;
        
        public static bool edit<T>(ref T field, bool allowDrop) where T : Object =>
#if UNITY_EDITOR
            !paintingPlayAreaGui ? ef.edit(ref field, allowDrop) :
#endif
                false;
        
        public static bool edit<T>(this string label, ref T field) where T : Object
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGui)  {
                label.write(label.ApproximateLength());
                return edit(ref field);
            }
#endif

            return false;

        }

        public static bool edit<T>(this string label, ref T field, bool allowDrop) where T : Object
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
            {
                write(label);
                return edit(ref field, allowDrop);
            }
#endif

            return false;

        }

        public static bool edit<T>(this string label, int width, ref T field) where T : Object
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
            {
                write(label, width);
                return edit(ref field);
            }
#endif

            return false;

        }

        public static bool edit<T>(this string label, int width, ref T field, bool allowDrop) where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
            {
                write(label, width);
                return edit(ref field, allowDrop);
            }

#endif

            return false;

        }

        public static bool edit<T>(this string label, string tip, int width, ref T field) where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
            {
                write(label, tip, width);
                return edit(ref field);
            }

#endif

            return false;

        }

        public static bool edit<T>(this string label, string tip, int width, ref T field, bool allowDrop) where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
            {
                write(label, tip, width);
                return edit(ref field, allowDrop);
            }

#endif

            return false;

        }
        
        public static bool edit<T>(ref T field) where T : Object =>
#if UNITY_EDITOR
            !paintingPlayAreaGui ? ef.edit(ref field) :
#endif
                false;

        public static bool edit_enter_Inspect<T>(ref T obj, ref int entered, int current, List<T> selectFrom = null) where T : Object
            => edit_enter_Inspect(null, -1, ref obj, ref entered, current, selectFrom);

        public static bool edit_enter_Inspect<T>(this string label, ref T obj, ref int entered, int current, List<T> selectFrom = null) where T : Object
            => label.edit_enter_Inspect(-1, ref obj, ref entered, current, selectFrom);

        public static bool edit_enter_Inspect<T>(this string label, int width, ref T obj, ref int entered, int current,
            List<T> selectFrom = null) where T : Object
        {
            var changed = false;
            
            var lst = obj as IPEGI_ListInspect;

            if (lst != null) 
                lst.enter_Inspect_AsList(ref entered, current, label).changes(ref changed);
            else {
                var pgi = QcUnity.TryGet_fromObj<IPEGI>(obj);
                
                if (icon.Enter.conditional_enter(pgi != null, ref entered, current, label))
                    pgi.Nested_Inspect().changes(ref changed);
            }
            
            if (entered == -1) {

                if (selectFrom == null) {

                    string lab = label;

                    if (lab.IsNullOrEmpty()) {

                        if (obj)
                            lab = obj.GetNameForInspector();
                        else
                            lab = typeof(T).ToPegiStringType();
                    }

                    lab = lab.TryAddCount(obj);

                    if (width > 0) {

                        if (lab.ClickLabel(Msg.ClickToInspect.GetText(), width, PEGI_Styles.EnterLabel))
                            entered = current;
                    }
                    else
                    if (lab.ClickLabel(Msg.ClickToInspect.GetText(), style: PEGI_Styles.EnterLabel))
                        entered = current;

                    if (!obj)
                        edit(ref obj).changes(ref changed);
                }
                else
                    label.select_or_edit(width, ref obj, selectFrom).changes(ref changed);

                obj.ClickHighlight();

                if (obj && icon.Delete.Click(Msg.MakeElementNull.GetText()))
                    obj = null;
            }
            
           
            
            return changed;
        }
        
#endregion

#region Vectors

        public static bool edit(this string label, ref Quaternion qt)
        {
            write(label, label.ApproximateLength());
            return edit(ref qt);
        }


        public static bool edit(this string label, int width, ref Quaternion qt) 
        {
            write(label, width);
            return edit(ref qt);
        }

        public static bool edit(ref Quaternion qt)
        {
            var eul = qt.eulerAngles;

            if (edit(ref eul)) {
                qt.eulerAngles = eul;
                return true;
            }

            return false;
        }

        public static bool edit(ref Vector4 val)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(ref val);
#endif
        
            return "X".edit(ref val.x) | "Y".edit(ref val.y) | "Z".edit(ref val.z) | "W".edit(ref val.w);

        }

        public static bool edit01(this string label, int width, ref Rect val)
        {
            label.nl(width);
            return edit01(ref val);
        }

        public static bool edit01(ref float val) => edit(ref val, 0, 1);

        public static bool edit01(this string label, ref float val) => label.edit(label.ApproximateLength(), ref val, 0, 1);
        
        public static bool edit01(this string label, int width, ref float val) => label.edit(width, ref val, 0, 1);

        public static bool edit01(ref Rect val)
        {
            var center = val.center;
            var size = val.size;
            
            if (
                "X".edit01(30, ref center.x).nl() ||
                "Y".edit01(30, ref center.y).nl() ||
                "W".edit01(30, ref size.x).nl() ||
                "H".edit01(30, ref size.y).nl())
            {
                var half = size * 0.5f;
                val.min = center - half;
                val.max = center + half;
                return true;
            }

            return false;
        }

        public static bool edit(this string label, ref Rect val) {
            var v4 = val.ToVector4();

            if (label.edit(ref v4)) {
                val = v4.ToRect();
                return true;
            }

            return false;
        }

        public static bool edit(this string label, ref Vector4 val)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(label, ref val);
#endif
            
            write(label);
            return 
                edit(ref val.x) |
                edit(ref val.y) |
                edit(ref val.z) |
                edit(ref val.w);

        }

        public static bool edit(ref Vector3 val) =>
            "X".edit(15, ref val.x) ||  "Y".edit(15, ref val.y) || "Z".edit(15, ref val.z);

        public static bool edit(this string label, ref Vector3 val)
        {
            write(label);
            nl();
            return edit(ref val);
        }

        public static bool edit(ref Vector2 val)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(ref val);
            
#endif
            
            return  edit(ref val.x) || edit(ref val.y);
        }

        public static bool edit01(this string label, int width, ref Vector2 val)
        {
            label.nl(width);
            return edit01(ref val);
        }

        public static bool edit01(this string label, ref Vector2 val)
        {
            label.nl(label.ApproximateLength());
            return edit01(ref val);
        }

        public static bool edit01(ref Vector2 val) =>
            "X".edit01(10, ref val.x).nl() ||
            "Y".edit01(10, ref val.y).nl();
        
        public static bool edit_Range(this string label, int width, ref Vector2 vec2) {

            var x = vec2.x;
            var y = vec2.y;

            if (label.edit_Range(width, ref x, ref y)) {
                vec2.x = x;
                vec2.y = y;
                return true;
            }

            return false;
        }
        
        public static bool edit(this string label, ref Vector2 val, float min, float max)
        {
            "{0} [X: {1} Y: {2}]".F(label, val.x.RoundTo(2), val.y.RoundTo(2)).nl();
            return edit(ref val, min, max);
        }

        public static bool edit(ref Vector2 val, float min, float max) =>
            "X".edit(10, ref val.x, min, max) ||
            "Y".edit(10, ref val.y, min, max);

        public static bool edit(this string label, ref Vector2 val)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(label, ref val);
#endif
        
            write(label);
            return edit(ref val.x) || edit(ref val.y);
  
        }

        public static bool edit(this string label, string tip, int width, ref Vector2 v2)
        {
            write(label, tip, width);
            return edit(ref v2);
        }

        public static bool edit(this string label, int width, ref Vector3 v3)
        {
            write(label, width);
            return edit(ref v3);
        }

        public static bool edit(this string label, string tip, int width, ref Vector3 v3)
        {
            write(label, tip, width);
            return edit(ref v3);
        }
#endregion

#region Color

        public static bool edit(ref Color32 col)
        {
            Color tcol = col;
            if (edit(ref tcol))
            {
                col = tcol;
                return true;
            }
            return false;
        }
        
        public static bool edit(ref Color col)
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(ref col);
        
#endif
            nl();
            return icon.Red.edit_ColorChannel(ref col, 0).nl() ||
                   icon.Green.edit_ColorChannel(ref col, 1).nl() ||
                   icon.Blue.edit_ColorChannel(ref col, 2).nl() ||
                   icon.Alpha.edit_ColorChannel(ref col, 3).nl();

        }

        public static bool edit(ref Color col, int width)
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(ref col, width);

#endif
            return false;
        }

        public static bool edit_ColorChannel(this icon ico, ref Color col, int channel)
        {
            var changed = false;

            if (channel < 0 || channel > 3)
                "Color has no channel {0} ".F(channel).writeWarning();
            else
            {
                var chan = col[channel];

                if (ico.edit(ref chan, 0, 1).changes(ref changed))
                    col[channel] = chan;

            }

            return changed;
        }

        public static bool edit_ColorChannel(this string label, ref Color col, int channel)
        {
            var changed = false;

            if (channel < 0 || channel > 3)
                "{0} color does not have {1}'th channel".F(label, channel).writeWarning();
            else
            {
                var chan = col[channel];

                if (label.edit(ref chan, 0, 1).changes(ref changed))
                    col[channel] = chan;

            }

            return changed;
        }

        public static bool edit(this string label, ref Color col)
        {
            if (paintingPlayAreaGui)
            {
                if (label.foldout())
                    return edit(ref col);
            }
            else
            {
                write(label);
                return edit(ref col);
            }

            return false;
        }

        public static bool edit(this string label, int width, ref Color col)
        {
            if (paintingPlayAreaGui)
            {
                if (label.foldout())
                    return edit(ref col);

            }
            else
            {
                write(label, width);
                return edit(ref col);
            }

            return false;
        }

        public static bool edit(this string label, string tip, int width, ref Color col)
        {
            if (paintingPlayAreaGui)
                return false;

            write(label, tip, width);
            return edit(ref col);
        }

#endregion

#region Material

        public static bool editTexture(this Material mat, string name) => mat.editTexture(name, name);

        public static bool editTexture(this Material mat, string name, string display) {

            display.write(display.ApproximateLength());
            var tex = mat.GetTexture(name);

            if (edit(ref tex))
            {
                mat.SetTexture(name, tex);
                return true; 
            }

            return false;
        }

#endregion

#region Animation Curve
        public static bool edit(this string name, ref AnimationCurve val) =>
#if UNITY_EDITOR
            !paintingPlayAreaGui ? ef.edit(name, ref val) :
#endif
                false;

#endregion

#region UInt

        public static bool edit(ref uint val)
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(ref val);
#endif
            bc();
            var newval = GUILayout.TextField(val.ToString());
            if (!ec()) return false;

            int newValue;
            if (int.TryParse(newval, out newValue))
                val = (uint)newValue;

            return true;


        }

        public static bool edit(ref uint val, int width)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(ref val, width);
#endif

            bc();
            var strVal = GUILayout.TextField(val.ToString(), GUILayout.MaxWidth(width));
            if (!ec()) return false;

            int newValue;
            if (int.TryParse(strVal, out newValue))
                val = (uint)newValue;

            return true;

        }

        public static bool edit(ref uint val, uint min, uint max)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(ref val, min, max);
#endif
            
            bc();
            val = (uint)GUILayout.HorizontalSlider(val, min, max);
            return ec();
            
        }

        public static bool edit(this string label, ref uint val)
        {
            write(label);
            return edit(ref val);
        }

        public static bool edit(this string label, ref uint val, uint min, uint max)
        {
            label.sliderText(val, label, 90);
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, int width, ref uint val, uint min, uint max)
        {
            label.sliderText(val, label, width);
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, string tip, int width, ref uint val)
        {
            write(label, tip, width);
            return edit(ref val);
        }

        public static bool edit(this string label, string tip, int width, ref uint val, uint min, uint max)
        {
            label.sliderText(val, tip, width);
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, int width, ref uint val)
        {
            write(label, width);
            return edit(ref val);
        }

#endregion

#region Int

        public static bool editLayerMask(this string label, string tip, int width, ref string tag)
        {
            label.write(tip, width);
            return editTag(ref tag);
        }

        public static bool editLayerMask(this string label, int width, ref string tag)
        {
            label.write(width);
            return editTag(ref tag);
        }

        public static bool editLayerMask(this string label, ref string tag)
        {
            label.write(label.ApproximateLength());
            return editTag(ref tag);
        }

        public static bool editTag(ref string tag)
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.editTag(ref tag);
#endif

            return false;
        }



        public static bool editLayerMask(this string label, ref int val)
        {
            label.write(label.ApproximateLength());
            return editLayerMask(ref val);
        }

        public static bool editLayerMask(ref int val) {
#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.editLayerMask(ref val);
#endif

            return false;
        }

        public static bool edit(ref int val)
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(ref val);
#endif

            bc();
            var intText = GUILayout.TextField(val.ToString());
            if (!ec()) return false;

            int newValue;

            if (int.TryParse(intText, out newValue))
                val = newValue;

            return true;
        }
        
        public static bool edit(ref int val, int width)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(ref val, width);
#endif

            bc();

            var newValText = GUILayout.TextField(val.ToString(), GUILayout.MaxWidth(width));

            if (!ec()) return false;

            int newValue;
            if (int.TryParse(newValText, out newValue))
                val = newValue;

            return change;

        }
        
        public static bool edit(ref int val, int min, int max)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(ref val, min, max);
#endif

            bc();
            val = (int)GUILayout.HorizontalSlider(val, min, max);
            return ec();
            
        }

        private static int editedInteger;
        private static int editedIntegerIndex;
        public static bool editDelayed(ref int val, int width)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.editDelayed(ref val, width);
            
#endif
            
            checkLine();

            var tmp = (editedIntegerIndex == _elementIndex) ? editedInteger : val;

            if (KeyCode.Return.IsDown() && (_elementIndex == editedIntegerIndex))
            {
                edit(ref tmp);
                val = editedInteger;
                editedIntegerIndex = -1;

                _elementIndex++;

                return change;
            }
            
            if (edit(ref tmp).ignoreChanges())
            {
                editedInteger = tmp;
                editedIntegerIndex = _elementIndex;
            }

            _elementIndex++;

            return false;
            
        }

        public static bool editDelayed(this string label, ref int val, int width) {
            write(label, Msg.EditDelayed_HitEnter.GetText());
            return editDelayed(ref val, width);
        }

        public static bool edit(this string label, ref int val)
        {
            write(label);
            return edit(ref val);
        }

        public static bool edit(this string label, ref int val, int min, int max)
        {
            label.sliderText(val, label, 90);
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, int width, ref int val, int min, int max)
        {
            label.sliderText(val, label, width);
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, string tip, int width, ref int val)
        {
            write(label, tip, width);
            return edit(ref val);
        }

        public static bool edit(this string label, string tip, int width, ref int val, int min, int max)
        {
            label.sliderText(val, tip, width);
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, int width, ref int val)
        {
            write(label, width);
            return edit(ref val);
        }

#endregion

#region Float
        
        public static bool edit(ref float val)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(ref val);
#endif
            
            bc();
            var newval = GUILayout.TextField(val.ToString());

            if (!ec()) return false;
            
            float newValue;
            if (float.TryParse(newval, out newValue))
                val = newValue;

            return change;
        }
        
        public static bool edit(ref float val, int width)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(ref val, width);
#endif

            bc();

            var newval = GUILayout.TextField(val.ToString(), GUILayout.MaxWidth(width));
            
            if (!ec()) return false;
            
            float newValue;
            if (float.TryParse(newval, out newValue))
                val = newValue;

            return change;

        }

        public static bool edit(this string label, ref float val) {
#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(label, ref val);
#endif
                write(label);
                return edit(ref val);
        }

        public static bool editPOW(ref float val, float min, float max)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.editPOW(ref val, min, max);
#endif

            bc();
            var after = GUILayout.HorizontalSlider(Mathf.Sqrt(val), min, max);
            if (!ec()) return false;
            val = after * after;
            return change;


        }

        public static bool edit(ref float val, float min, float max)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(ref val, min, max);
            
#endif

            bc();
            val = GUILayout.HorizontalSlider(val, min, max);
            return ec();
            
        }

        public static bool editDelayed(this string label, string tip, int width, ref float val)
        {
            write(label, tip, width);
            return editDelayed(ref val);
        }

        public static bool editDelayed(this string label, int width, ref float val)
        {
            write(label, width);
            return editDelayed(ref val);
        }

        public static bool editDelayed(this string label, ref float val)
        {
            write(label);
            return editDelayed(ref val);
        }
        
        public static bool editDelayed(ref float val, int width)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.editDelayed(ref val, width);
#endif

            return editDelayed(ref val);
        }

        public static bool editDelayed(ref float val)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.editDelayed(ref val);
#endif


            checkLine();

            var tmp = (editedFloatIndex == _elementIndex) ? editedFloat : val.ToString();

            if (KeyCode.Return.IsDown() && (_elementIndex == editedFloatIndex))
            {
                edit(ref tmp);

                float newValue;
                if (float.TryParse(editedFloat, out newValue))
                    val = newValue;
                _elementIndex++;

                editedFloatIndex = -1;

                return change;
            }

            if (edit(ref tmp).ignoreChanges())
            {
                editedFloat = tmp;
                editedFloatIndex = _elementIndex;
            }

            _elementIndex++;

            return false;
        }

        private static string editedFloat;
        private static int editedFloatIndex;

        public static bool edit(this string label, int width, ref float val)
        {
            write(label, width);
            return edit(ref val);
        }

        public static bool edit_Range(this string label, ref float from, ref float to) => label.edit_Range(label.ApproximateLength(), ref from, ref to);
        
        public static bool edit_Range(this string label, int width, ref float from, ref float to)
        {
            write(label, width);
            var changed = false;
            if (edit(ref from).changes(ref changed))
                to = Mathf.Max(from, to);
            
            write("-", 10);

            if (edit(ref to).changes(ref changed))
                from = Mathf.Min(from, to);


            return changed;
        }

        private static void sliderText(this string label, float val, string tip, int width)
        {
            if (paintingPlayAreaGui)
                "{0} [{1}]".F(label, val.ToString("F3")).write(width);
            else
                write(label, tip, width);
        }

        public static bool edit(this string label, ref float val, float min, float max)
        {
            label.sliderText(val, label, label.Length*letterSizeInPixels);
            return edit(ref val, min, max);
        }

        public static bool edit(this icon ico, ref float val, float min, float max)
        {
            ico.write();
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, int width, ref float val, float min, float max)
        {
            label.sliderText(val, label, width);
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, string tip, int width, ref float val, float min, float max)
        {
            label.sliderText(val, tip, width);
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, string tip, int width, ref float val)
        {
            write(label, tip, width);
            return edit(ref val);
        }

        public static bool edit(this string label, string tip, ref float val) {
            write(label, tip);
            return edit(ref val);
        }

#endregion

#region Double

        public static bool editDelayed(this string label, string tip, int width, ref double val)
        {
            write(label, tip, width);
            return editDelayed(ref val);
        }

        public static bool editDelayed(this string label, int width, ref double val)
        {
            write(label, width);
            return editDelayed(ref val);
        }

        public static bool editDelayed(this string label, ref double val)
        {
            write(label);
            return editDelayed(ref val);
        }

        public static bool editDelayed(ref double val, int width)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.editDelayed(ref val, width);
#endif

            return editDelayed(ref val);
        }

        public static bool editDelayed(ref double val)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.editDelayed(ref val);
#endif


            checkLine();

            var tmp = (editedDoubleIndex == _elementIndex) ? editedDouble : val.ToString();

            if (KeyCode.Return.IsDown() && (_elementIndex == editedDoubleIndex))
            {
                edit(ref tmp);

                double newValue;
                if (double.TryParse(editedFloat, out newValue))
                    val = newValue;
                _elementIndex++;

                editedDoubleIndex = -1;

                return change;
            }

            if (edit(ref tmp).ignoreChanges())
            {
                editedDouble = tmp;
                editedDoubleIndex = _elementIndex;
            }

            _elementIndex++;

            return false;
        }

        private static string editedDouble;
        private static int editedDoubleIndex;


        public static bool edit(ref double val)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(ref val);
#endif
            bc();
            var newval = GUILayout.TextField(val.ToString());
            if (!ec()) return false;
            double newValue;
            if (!double.TryParse(newval, out newValue)) return false;
            val = newValue;
            return change;
        }

        public static bool edit(this string label, ref double val)
        {
            label.write();
            return edit(ref val);
        }

        public static bool edit(this string label, int width, ref double val)
        {
            label.write(width);
            return edit(ref val);
        }

        public static bool edit(this string label, string tip, int width, ref double val)
        {
            label.write(tip, width);
            return edit(ref val);
        }

        public static bool edit(ref double val, int width)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(ref val, width);
#endif

            bc();
            var newval = GUILayout.TextField(val.ToString(), GUILayout.MaxWidth(width));
            if (!ec()) return false;

            double newValue;
            if (double.TryParse(newval, out newValue))
                val = newValue;

            return change;

        }

#endregion

#region Enum

        public static bool editEnum<T>(this string text, string tip, int width, ref T eval)
        {
            write(text, tip, width);
            return editEnum(ref eval);
        }

        public static bool editEnum<T>(this string text, int width, ref T eval)
        {
            write(text, width);
            return editEnum(ref eval);
        }

        public static bool editEnum<T>(this string text, ref T eval)
        {
            write(text);
            return editEnum(ref eval);
        }
        
        public static bool editEnum<T>(ref T eval, int width = -1) {
            var val = Convert.ToInt32(eval);

            if (selectEnum(ref val, typeof(T), width)) {
                eval = (T)((object)val);
                return true;
            }

            return false;
        }

        public static bool editEnumFlags<T>(this string text, string tip, int width, ref T eval)
        {
            write(text, tip, width);
            return editEnumFlags(ref eval);
        }

        public static bool editEnumFlags<T>(this string text, int width, ref T eval)
        {
            write(text, width);
            return editEnumFlags(ref eval);
        }

        public static bool editEnumFlags<T>(this string text, ref T eval)
        {
            write(text);
            return editEnumFlags(ref eval);
        }

        public static bool editEnumFlags<T>(ref T eval, int width = -1)
        {
            var val = Convert.ToInt32(eval);

            if (selectEnumFlags(ref val, typeof(T), width)) {
                eval = (T)((object)val);
                return true;
            }

            return false;
        }

        public static bool editEnum<T>(ref int current, Type type, int width = -1)
                => selectEnum(ref current, typeof(T), width);

        public static bool editEnum<T>(this string text, string tip, int width, ref int eval)
        {
            write(text, tip, width);
            return editEnum<T>(ref eval, typeof(T), -1);
        }

        public static bool editEnum<T>(this string text, int width, ref int eval)
        {
            write(text, width);
            return editEnum<T>(ref eval, typeof(T), -1);
        }

        public static bool editEnum<T>(this string text, ref int eval)
        {
            write(text);
            return editEnum<T>(ref eval, typeof(T), -1);
        }
        
        public static bool editEnum<T>(ref int eval) => editEnum<T>(ref eval, typeof(T));
        
#endregion

#region String

        private static string editedText;
        private static string editedHash = "";
        public static bool editDelayed(ref string val)
        {
            if (LengthIsTooLong(ref val)) return false;

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.editDelayed(ref val);
#endif
            
            checkLine();

            if ((KeyCode.Return.IsDown() && (val.GetHashCode().ToString() == editedHash)))
            {
                GUILayout.TextField(val);
                val = editedText;

                return change;
            }

            var tmp = val;
            if (edit(ref tmp).ignoreChanges())
            {
                editedText = tmp;
                editedHash = val.GetHashCode().ToString();
            }

            return false;
            
        }

        public static bool editDelayed(this string label, ref string val)
        {
            write(label, Msg.EditDelayed_HitEnter.GetText());
            return editDelayed(ref val);
        }

        public static bool editDelayed(ref string val, int width)
        {

            if (LengthIsTooLong(ref val)) return false;

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.editDelayed(ref val, width);
#endif
            
            checkLine();

            if ((KeyCode.Return.IsDown() && (val.GetHashCode().ToString() == editedHash)))
            {
                GUILayout.TextField(val);
                val = editedText;
                return change;
            }

            var tmp = val;
            if (edit(ref tmp, width).ignoreChanges())
            {
                editedText = tmp;
                editedHash = val.GetHashCode().ToString();
            }

            return false;
            
        }

        public static bool editDelayed(this string label, ref string val, int width)
        {
            write(label, Msg.EditDelayed_HitEnter.GetText(), width);

            return editDelayed(ref val);

        }
        
        public static bool editDelayed(this string label, int width, ref string val)
        {
            write(label, width);
            return editDelayed(ref val);
        }

        public static bool editDelayed(this string label, string hint, int width, ref string val)
        {
            write(label, hint, width);
            return editDelayed(ref val);
        }

        private const int maxStringSize = 1000;

        private static bool LengthIsTooLong(ref string label)
        {
            if (label == null || label.Length < maxStringSize)
                return false;

            if (icon.Delete.ClickUnFocus())
            {
                label = "";
                return false;
            }
            else
                write("String is too long {0}".F(label.Substring(0, 10)));
        
            return true;
        }

        public static bool edit(ref string val) {
            if (LengthIsTooLong(ref val)) return false;

            #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(ref val);
            #endif

            bc();
            val = GUILayout.TextField(val);
            return ec(); 
        }

        public static bool edit(ref string val, int width)
        {

            if (LengthIsTooLong(ref val)) return false;

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(ref val, width);
#endif

            bc();
            var newval = GUILayout.TextField(val, GUILayout.MaxWidth(width));
            if (ec())
            {
                val = newval;
                return change;
            }
            return false;
            
        }

        public static bool editBig(ref string val)  {

            nl();

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.editBig(ref val).nl();
#endif

            bc();
            val = GUILayout.TextArea(val);
            return ec();

        }

        public static bool editBig(this string name, ref string val) {
            write(name);
            return editBig(ref val);
        }
        
        public static bool edit(this string label, ref string val) {

            if (LengthIsTooLong(ref val)) return false;

            #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(label, ref val);
            #endif
            
             write(label);
             return edit(ref val);
        }

        public static bool edit(this string label, int width, ref string val)
        {
            write(label, width);
            return edit(ref val);
        }

        public static bool edit(this string label, string tip, int width, ref string val)
        {
            write(label, tip, width);
            return edit(ref val);
        }

#endregion

#region Property

        public static bool edit_Property<T>(Expression<Func<T>> memberExpression, Object obj, int fieldWidth = -1, bool includeChildren = true)
            => edit_Property(memberExpression, fieldWidth, obj, includeChildren);
        
        public static bool edit_Property<T>(this string label, Expression<Func<T>> memberExpression, Object obj, int fieldWidth = -1, bool includeChildren = true) {
            label.nl();
            return edit_Property(memberExpression, fieldWidth, obj, includeChildren);
        }
        
        public static bool edit_Property<T>(this string label, string tip, Expression<Func<T>> memberExpression, Object obj, int fieldWidth = -1, bool includeChildren = true) {
            label.nl(tip);
            return edit_Property(memberExpression, fieldWidth, obj, includeChildren);
        }

        public static bool edit_Property<T>(this string label, int width, Expression<Func<T>> memberExpression, Object obj, int fieldWidth = -1, bool includeChildren = true) {
            label.nl(width);
            return edit_Property(memberExpression, fieldWidth, obj, includeChildren);
        }

        public static bool edit_Property<T>(this string label, string tip, int width, Expression<Func<T>> memberExpression, Object obj, int fieldWidth = -1, bool includeChildren = true) {
            label.nl(tip, width);
            return edit_Property(memberExpression, fieldWidth, obj, includeChildren);
        }

        public static bool edit_Property<T>(this Texture tex, string tip, Expression<Func<T>> memberExpression, Object obj, int fieldWidth = -1, bool includeChildren = true) {
            tex.write(tip);
            nl();
            return edit_Property(memberExpression, fieldWidth, obj, includeChildren);
        }

        private static bool edit_Property<T>(Expression<Func<T>> memberExpression, int width, Object obj, bool includeChildren) {
#if UNITY_EDITOR
            if (!paintingPlayAreaGui) 
                return ef.edit_Property(width, memberExpression, obj, includeChildren);
            
#endif
            return false;
        }


#endregion

#region MyIntVector2

        public static bool edit(ref MyIntVec2 val)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(ref val);
#endif

            return edit(ref val.x) || edit(ref val.y);
        }

        public static bool edit(ref MyIntVec2 val, int min, int max)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(ref val, min, max);
#endif

            return edit(ref val.x, min, max) || edit(ref val.y, min, max);

        }

        public static bool edit(ref MyIntVec2 val, int min, MyIntVec2 max)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(ref val, min, max);
#endif

            return edit(ref val.x, min, max.x) || edit(ref val.y, min, max.y);
        }

        public static bool edit(this string label, ref MyIntVec2 val)
        {
            write(label);
            nl();
            return edit(ref val);
        }

        public static bool edit(this string label, int width, ref MyIntVec2 val)
        {
            write(label, width);
            nl();
            return edit(ref val);
        }

        public static bool edit(this string label, int width, ref MyIntVec2 val, int min, int max)
        {
            write(label, width);
            nl();
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, int width, ref MyIntVec2 val, int min, MyIntVec2 max)
        {
            write(label, width);
            nl();
            return edit(ref val, min, max);
        }

#endregion

#endregion

#region LISTS

        #region List MGMT Functions 

        private const int listLabelWidth = 105;

        private static readonly Dictionary<IList, int> ListInspectionIndexes = new Dictionary<IList, int>();

        private const int UpDownWidth = 120;
        private const int UpDownHeight = 30;
       // private static int _sectionSizeOptimal;
        //private static int _listSectionMax;
        private static int _lastElementToShow;
        private static int _listSectionStartIndex;
        private static readonly CountlessInt ListSectionOptimal = new CountlessInt();
        private static int GetOptimalSectionFor(int count)
        {
            int _sectionSizeOptimal;

            const int listShowMax = 10;

            if (count < listShowMax) 
                return listShowMax;
            
            
            if (count > listShowMax * 3)
                return listShowMax;
            
            _sectionSizeOptimal = ListSectionOptimal[count];

            if (_sectionSizeOptimal != 0)
                return _sectionSizeOptimal;
            
            var bestdifference = 999;

            for (var i = listShowMax - 2; i < listShowMax + 2; i++)
            {
                var difference = i - (count % i);

                if (difference < bestdifference)
                {
                    _sectionSizeOptimal = i;
                    bestdifference = difference;
                    if (difference == 0)
                        break;
                }

            }


            ListSectionOptimal[count] = _sectionSizeOptimal;

            return _sectionSizeOptimal;

        }

        private static IList addingNewOptionsInspected;
        static string addingNewNameHolder = "Name";

        private static void listInstantiateNewName<T>()  {
                Msg.New.GetText().write(Msg.NameNewBeforeInstancing_1p.GetText().F(typeof(T).ToPegiStringType()) ,30, PEGI_Styles.ExitLabel);
            edit(ref addingNewNameHolder);
        }

        private static bool PEGI_InstantiateOptions_SO<T>(this List<T> lst, ref T added, ListMetaData ld) where T : ScriptableObject
        {
            if (ld != null && !ld.allowCreate)
                return false;

            if (editing_List_Order != null && editing_List_Order == lst)
                return false;

            var indTypes = typeof(T).TryGetDerivedClasses();

            var tagTypes = typeof(T).TryGetTaggedClasses();

            if (indTypes == null && tagTypes == null && typeof(T).IsAbstract)
                return false;

            var changed = false;

            // "New {0} ".F(typeof(T).ToPegiStringType()).edit(80, ref addingNewNameHolder);
            listInstantiateNewName<T>();

            if (addingNewNameHolder.Length > 1) {
                if (indTypes == null  && tagTypes == null)  {
                    if (icon.Create.ClickUnFocus(Msg.AddNewCollectionElement).nl(ref changed))
                        added = lst.CreateAndAddScriptableObjectAsset("Assets/ScriptableObjects/", addingNewNameHolder);
                }
                else
                {
                    var selectingDerrived = lst == addingNewOptionsInspected;

                    icon.Create.foldout("Instantiate Class Options", ref selectingDerrived).nl();

                    if (selectingDerrived)
                        addingNewOptionsInspected = lst;
                    else if (addingNewOptionsInspected == lst)
                        addingNewOptionsInspected = null;

                    if (selectingDerrived) {
                        if (indTypes != null)
                        foreach (var t in indTypes) {
                            write(t.ToPegiStringType());
                            if (icon.Create.ClickUnFocus().nl(ref changed))
                                added = lst.CreateScriptableObjectAsset("Assets/ScriptableObjects/", addingNewNameHolder, t);
                        }

                        if (tagTypes != null)
                        {
                            var k = tagTypes.Keys;

                            int optionsPresented = 0;

                            for (var i=0; i<k.Count; i++) {

                                if (tagTypes.CanAdd(i, lst))
                                {
                                    optionsPresented++;
                                    write(tagTypes.DisplayNames[i]);
                                    if (icon.Create.ClickUnFocus().nl(ref changed))
                                        added = lst.CreateScriptableObjectAsset("Assets/ScriptableObjects/",
                                            addingNewNameHolder, tagTypes.TaggedTypes.TryGet(k[i]));
                                }

                            }

                            if (optionsPresented == 0)
                                (k.Count == 0 ? "There no types derrived from {0}".F(typeof(T).ToString()) :
                                        "Existing types are restricted to one instance per list").writeHint();

                        }
                    }
                }
            }
            nl();

            return changed;

        }
        
        private static bool PEGI_InstantiateOptions<T>(this List<T> lst, ref T added, ListMetaData ld)
        {
            if (ld != null && !ld.allowCreate)
                return false;

            if (editing_List_Order != null && editing_List_Order == lst)
                return false;

            var intTypes = typeof(T).TryGetDerivedClasses();
            
            var tagTypes = typeof(T).TryGetTaggedClasses();
            
            if (intTypes == null && tagTypes == null)
                return false;

            var changed = false;

            var hasName = typeof(T).IsSubclassOf(typeof(Object)) || typeof(IGotName).IsAssignableFrom(typeof(T));

            if (hasName)
                listInstantiateNewName<T>();
            else
                (intTypes == null ? "Create new {0}".F(typeof(T).ToPegiStringType()) : "Create Derrived from {0}".F(typeof(T).ToPegiStringType())).write();

            if (!hasName || addingNewNameHolder.Length > 1) {

                var selectingDerrived = lst == addingNewOptionsInspected;

                icon.Add.foldout("Instantiate Class Options", ref selectingDerrived).nl();

                if (selectingDerrived)
                    addingNewOptionsInspected = lst;
                else if (addingNewOptionsInspected == lst)
                    addingNewOptionsInspected = null;

                if (selectingDerrived)
                {
                    if (intTypes != null)
                    foreach (var t in intTypes)  {
                        write(t.ToPegiStringType());
                        if (icon.Create.ClickUnFocus().nl(ref changed))  {
                            added = (T)Activator.CreateInstance(t);
                             QcUtils.AddWithUniqueNameAndIndex(lst, added, addingNewNameHolder);
                        }
                    }

                    if (tagTypes != null) {
                        var k = tagTypes.Keys;

                        int availableOptions = 0;

                        for (var i = 0; i < k.Count; i++)
                        {
                            if (tagTypes.CanAdd(i, lst))
                            {
                                availableOptions++;

                                write(tagTypes.DisplayNames[i]);
                                if (icon.Create.ClickUnFocus().nl(ref changed))
                                {
                                    added = (T) Activator.CreateInstance(tagTypes.TaggedTypes.TryGet(k[i]));
                                    QcUtils.AddWithUniqueNameAndIndex(lst, added, addingNewNameHolder);
                                }
                            }

                        }

                        if (availableOptions == 0)
                            (k.Count == 0 ? "There no types derrived from {0}".F(typeof(T).ToString()) :
                                "Existing types are restricted to one instance per list").writeHint();

                    }

                }
            }
            else
                icon.Add.GetText().write("Input a name for a new element", 40);
            nl();

            return changed;
        }

        private static bool PEGI_InstantiateOptions<T>(this List<T> lst, ref T added, TaggedTypesCfg types, ListMetaData ld) 
        {
            if (ld != null && !ld.allowCreate)
                return false;

            if (editing_List_Order != null && editing_List_Order == lst)
                return false;

            var changed = false;

            var hasName = typeof(T).IsSubclassOf(typeof(UnityEngine.Object)) || typeof(IGotName).IsAssignableFrom(typeof(T));

            if (hasName)
                listInstantiateNewName<T>();
            else
                "Create new {0}".F(typeof(T).ToPegiStringType()).write();

            if (!hasName || addingNewNameHolder.Length > 1) {

                var selectingDerrived = lst == addingNewOptionsInspected;

                icon.Add.foldout("Instantiate Class Options", ref selectingDerrived).nl();

                if (selectingDerrived)
                    addingNewOptionsInspected = lst;
                else if (addingNewOptionsInspected == lst)
                    addingNewOptionsInspected = null;

                if (selectingDerrived)
                {

                    var k = types.Keys;
                        for (var i=0; i<k.Count; i++) {

                            write(types.DisplayNames[i]);
                            if (icon.Create.ClickUnFocus().nl(ref changed)) {
                                added = (T)Activator.CreateInstance(types.TaggedTypes.TryGet(k[i]));
                                QcUtils.AddWithUniqueNameAndIndex(lst, added, addingNewNameHolder);
                            }
                        }
                }
            }
            else
                icon.Add.GetText().write("Input a name for a new element", 40);
            nl();

            return changed;
        }

        public static int InspectedIndex { get; private set; } = -1;

        private static IEnumerable<int> InspectionIndexes<T>(this List<T> list, ListMetaData listMeta = null) {
            
            var sd = listMeta == null? searchData : listMeta.searchData;

            #region Inspect Start

            var changed = false;
            bool searching;
            string[] searchby;
            sd.SearchString(list, out searching, out searchby);

            int _listSectionStartIndex = 0;

            if (searching)
                _listSectionStartIndex = sd.inspectionIndexStart;
            else if (listMeta != null)
                _listSectionStartIndex = listMeta.listSectionStartIndex;
            else if (!ListInspectionIndexes.TryGetValue(list, out _listSectionStartIndex))
                ListInspectionIndexes.Add(list, 0);
            

            var listCount = list.Count;

            _lastElementToShow = listCount;

            var _sectionSizeOptimal = GetOptimalSectionFor(listCount);

            if (listCount >= _sectionSizeOptimal * 2 || _listSectionStartIndex > 0) {
                
                if (listCount > _sectionSizeOptimal) {
                 
                    while ((_listSectionStartIndex > 0 && _listSectionStartIndex >= listCount).changes(ref changed)) 
                        _listSectionStartIndex = Mathf.Max(0, _listSectionStartIndex - _sectionSizeOptimal);
                    
                    nl();
                    if (_listSectionStartIndex > 0) {
                        
                        if (_listSectionStartIndex > _sectionSizeOptimal && icon.UpLast.ClickUnFocus("To First element").changes(ref changed))
                            _listSectionStartIndex = 0;

                        if (icon.Up.ClickUnFocus("To previous elements of the list. ", UpDownWidth, UpDownHeight).changes(ref changed)) {
                            _listSectionStartIndex = Mathf.Max(0, _listSectionStartIndex - _sectionSizeOptimal+1);
                            if (_listSectionStartIndex == 1)
                                _listSectionStartIndex = 0;
                        }

                        ".. {0}; ".F(_listSectionStartIndex-1).write();

                    }
                    else
                        icon.UpLast.write("Is the first section of the list.", UpDownWidth, UpDownHeight);

                    
                    nl();

                  
                }
                else line(Color.gray);

              

            }
            else if (list.Count > 0)
                line(Color.gray);

            nl();

            #endregion


            var filteredCount = listCount;

            if (!searching)
            {
                _lastElementToShow = Mathf.Min(listCount, _listSectionStartIndex + _sectionSizeOptimal);

                for (InspectedIndex = _listSectionStartIndex; InspectedIndex < _lastElementToShow; InspectedIndex++)
                {
                   
                    SetListElementReadabilityBackground(InspectedIndex);

                    yield return InspectedIndex;

                    RestoreBGcolor();
                }


                if ((_listSectionStartIndex > 0) || (filteredCount > _lastElementToShow)) {

                    nl();
                    if (listCount > _lastElementToShow) {

                        if (icon.Down.ClickUnFocus("To next elements of the list. ", UpDownWidth, UpDownHeight).changes(ref changed))
                            _listSectionStartIndex += _sectionSizeOptimal - 1;
                        
                        if (icon.DownLast.ClickUnFocus("To Last element").changes(ref changed))
                            _listSectionStartIndex = listCount - _sectionSizeOptimal;
                        
                        "+ {0}".F(listCount - _lastElementToShow).write();

                    }
                    else if (_listSectionStartIndex > 0)
                        icon.DownLast.write("Is the last section of the list. ", UpDownWidth, UpDownHeight);

                }
                else if (listCount > 0)
                    line(Color.gray);


            } else {

                var sectionIndex = _listSectionStartIndex;

                var flst = sd.filteredListElements;

                _lastElementToShow = Mathf.Min(list.Count, _listSectionStartIndex + _sectionSizeOptimal);

                while ((sd.uncheckedElement <= listCount) && (sectionIndex < _lastElementToShow)) {

                    InspectedIndex = -1;
                    
                    if (flst.Count > sectionIndex)
                        InspectedIndex = flst[sectionIndex];
                    else {
                        while (sd.uncheckedElement < listCount && InspectedIndex == -1) {

                            var el = list[sd.uncheckedElement];

                            var na = el as INeedAttention;

                            var msg = na?.NeedAttention();

                            if (!sd.filterByNeedAttention || !msg.IsNullOrEmpty()) {
                                if (searchby.IsNullOrEmpty() || el.SearchMatch_Obj_Internal(searchby)) {
                                    InspectedIndex = sd.uncheckedElement;
                                    flst.Add(InspectedIndex);
                                }
                            }

                            sd.uncheckedElement++;
                        }
                    }
                    
                    if (InspectedIndex != -1) {

                        SetListElementReadabilityBackground(sectionIndex);
                        
                        yield return InspectedIndex;

                        RestoreBGcolor();

                        sectionIndex++;
                    }
                    else break;
                }


                bool gotUnchecked = (sd.uncheckedElement < listCount - 1);

                bool gotToShow = (flst.Count > _lastElementToShow) || gotUnchecked;

                if (_listSectionStartIndex > 0 || gotToShow) {

                    nl();
                    if (gotToShow) {

                        if (icon.Down.ClickUnFocus("To next elements of the list. ", UpDownWidth, UpDownHeight).changes(ref changed))
                            _listSectionStartIndex += _sectionSizeOptimal - 1;

                        if (icon.DownLast.ClickUnFocus("To Last element").changes(ref changed))
                            _listSectionStartIndex = Mathf.Max(0, flst.Count - _sectionSizeOptimal);

                        if (!gotUnchecked)
                            "+ {0}".F(flst.Count - _lastElementToShow).write();

                    }
                    else if (_listSectionStartIndex > 0)
                        icon.DownLast.write("Is the last section of the list. ", UpDownWidth, UpDownHeight);

                }
                else if (listCount > 0)
                    line(Color.gray);

            }




            #region Finilize
            if (changed)  {
                if (searching)
                    sd.inspectionIndexStart = _listSectionStartIndex;
                else if (listMeta != null)
                    listMeta.listSectionStartIndex = _listSectionStartIndex;
                else
                    ListInspectionIndexes[list] = _listSectionStartIndex;
            }
            #endregion
        }

        private static void SetListElementReadabilityBackground(int index)
        {
            switch (index % 4)
            {
                case 1: PEGI_Styles.listReadabilityBlue.SetBgColor(); break;
                case 3: PEGI_Styles.listReadabilityRed.SetBgColor(); break;
            }
        }

        private static string currentListLabel = "";
        public static string GetCurrentListLabel<T>(ListMetaData ld = null) => ld != null ? ld.label :
                    (currentListLabel.IsNullOrEmpty() ? typeof(T).ToPegiStringType()  : currentListLabel);

        private static bool listLabel_Used(this bool val) {
            currentListLabel = "";

            return val;
        }

        private static T listLabel_Used<T>(this T val) {
            currentListLabel = "";
            return val;
        }

        private static void write_Search_ListLabel(this string label, IList lst = null)
        {
            var notInsp = -1;
            label.write_Search_ListLabel(ref notInsp, lst);
        }

        private static void write_Search_ListLabel(this string label, ref int inspected, IList lst) {

            currentListLabel = label;

            bool inspecting = inspected != -1;

            if (!inspecting)
                searchData.ToggleSearch(lst, label);

            if (lst != null && inspected >= 0 && lst.Count > inspected) 
                label = "{0}->{1}".F(label, lst[inspected].GetNameForInspector());
            else label = (lst == null || lst.Count < 6) ? label : label.AddCount(lst, true);

            if (label.ClickLabel(label, -1, PEGI_Styles.ListLabel) && inspected != -1)
                inspected = -1;
        }

        private static void write_Search_ListLabel(this ListMetaData ld, IList lst) {

            currentListLabel = ld.label;

            if (!ld.Inspecting)
                ld.searchData.ToggleSearch(lst, ld.label);
            
            if (lst != null && ld.inspected >= 0 && lst.Count > ld.inspected) {

                var el = lst[ld.inspected];

                currentListLabel = "{0}->{1}".F(ld.label, lst[ld.inspected].GetNameForInspector());
                
            } else currentListLabel = (lst == null || lst.Count < 6) ? ld.label : ld.label.AddCount(lst, true);

            if (currentListLabel.ClickLabel(ld.label, RemainingLength(70), PEGI_Styles.ListLabel) && ld.inspected != -1)
                ld.inspected = -1;
        }

        private static bool ExitOrDrawPEGI<T>(T[] array, ref int index, ListMetaData ld = null)
        {
            var changed = false;

            if (index >= 0) {
                if (array == null || index >= array.Length || icon.List.ClickUnFocus("Return to {0} array".F(GetCurrentListLabel<T>(ld))).nl())
                    index = -1;
                else
                {
                    object obj = array[index];
                    if (Nested_Inspect(ref obj).changes(ref changed))
                        array[index] = (T)obj;
                }
            }

            return changed;
        }

        private static bool ExitOrDrawPEGI<T>(this List<T> list, ref int index, ListMetaData ld = null)
        {
            var changed = false;

            if (icon.List.ClickUnFocus("{0}[{1}] of {2}".F(Msg.ReturnToCollection.GetText(), list.Count, GetCurrentListLabel<T>(ld))).nl())
                index = -1;
            else
            {
                object obj = list[index];
                if (Nested_Inspect(ref obj).changes(ref changed))
                    list[index] = (T)obj;
            }

            return changed;
        }

        private static IList editing_List_Order;

        private static bool listIsNull<T>(ref List<T> list) {
            if (list == null) {
                if ("Initialize list".ClickUnFocus().nl())
                    list = new List<T>();
                else
                    return true;
                
            }

            return false;
        }

        private static bool allowDuplicants;
        private static bool list_DropOption<T>(this List<T> list, ListMetaData meta = null) where T : UnityEngine.Object
        {
            var changed = false;
#if UNITY_EDITOR
    
            if (ActiveEditorTracker.sharedTracker.isLocked == false && icon.Unlock.ClickUnFocus("Lock Inspector Window"))
                ActiveEditorTracker.sharedTracker.isLocked = true;

            if (ActiveEditorTracker.sharedTracker.isLocked && icon.Lock.ClickUnFocus("Unlock Inspector Window")) {
                ActiveEditorTracker.sharedTracker.isLocked = false;

                var mb = ef.serObj.targetObject as MonoBehaviour;

                QcUnity.FocusOn(mb ? mb.gameObject : ef.serObj.targetObject);

            }

            var dpl = meta!= null ? meta.allowDuplicants : allowDuplicants;
            
            foreach (var ret in ef.DropAreaGUI<T>())  {
                if (dpl || !list.Contains(ret)) {
                    list.Add(ret);
                    changed = true;
                }
            }

            "Duplicants".toggle("Will add elements to the list even if they are already there", 80, ref dpl).nl(ref changed);
            
            if (meta != null)
                meta.allowDuplicants = dpl;
            else allowDuplicants = dpl;
            
#endif
            return changed;
        }

        static Array _editingArrayOrder;

        public static CountlessBool selectedEls = new CountlessBool();

        private static List<int> _copiedElements = new List<int>();

        private static bool cutPaste;

        private static void SetSelected<T>(ListMetaData meta, List<T> list, bool val)
        {
            if (meta == null)
            {
                for (var i = 0; i < list.Count; i++)
                    selectedEls[i] = val;
            }
            else for (var i = 0; i < list.Count; i++)
                meta.SetIsSelected(i, val);
        }
        
        private static void TryMoveCopiedElement<T>(this List<T> list)
        {
            
            foreach (var e in _copiedElements)
                list.TryAdd(listCopyBuffer.TryGetObj(e));

            for (var i = _copiedElements.Count - 1; i >= 0; i--)
                listCopyBuffer.RemoveAt(_copiedElements[i]);

            listCopyBuffer = null;
        }

        private static bool edit_Array_Order<T>(ref T[] array, ListMetaData listMeta = null) {

            var changed = false;
            
            if (array != _editingArrayOrder) {
                if (icon.Edit.ClickUnFocus(Msg.MoveCollectionElements, 28))
                    _editingArrayOrder = array;
            }

            else if (icon.Done.ClickUnFocus(Msg.FinishMovingCollectionElements.GetText(), 28).nl(ref changed))
                _editingArrayOrder = null;
            
            if (array != _editingArrayOrder) return changed;

            var derivedClasses = typeof(T).TryGetDerivedClasses();

            for (var i = 0; i< array.Length; i++) {

                if (listMeta == null || listMeta.allowReorder) {

                    if (i > 0) {
                        if (icon.Up.ClickUnFocus("Move up").changes(ref changed))
                            QcSharp.Swap(ref array, i, i - 1);
                    }
                    else
                        icon.UpLast.write("Last");

                    if (i < array.Length - 1) {
                        if (icon.Down.ClickUnFocus("Move down").changes(ref changed))
                            QcSharp.Swap(ref array, i, i + 1);
                    }
                    else icon.DownLast.write();
                }

                var el = array[i];

                var isNull = el.IsNullOrDestroyed_Obj();

                if (listMeta == null || listMeta.allowDelete) {
                    if (!isNull && typeof(T).IsUnityObject()) {
                        if (icon.Delete.ClickUnFocus(Msg.MakeElementNull).changes(ref changed))
                            array[i] = default(T);
                    }  else {
                        if (icon.Close.ClickUnFocus(Msg.RemoveFromCollection).changes(ref changed)) {
                            QcSharp.Remove(ref array, i);
                            i--;
                        }
                    }
                }

                if (!isNull && derivedClasses != null) {
                    var ty = el.GetType();
                    if (@select(ref ty, derivedClasses, el.GetNameForInspector()))
                        array[i] = (el as ICfg).TryDecodeInto<T>(ty);
                }

                if (!isNull)
                    write(el.GetNameForInspector());
                else
                    "{0} {1}".F(icon.Empty.GetText() ,typeof(T).ToPegiStringType()).write();

                nl();
            }

            return changed;
        }

        private static bool edit_List_Order<T>(this List<T> list, ListMetaData listMeta = null) {

            var changed = false;

            var sd = listMeta == null ? searchData : listMeta.searchData;

            if (list != editing_List_Order)
            {
                if (sd.filteredList != list && icon.Edit.ClickUnFocus(Msg.MoveCollectionElements, 28))  
                    editing_List_Order = list;
            } else if (icon.Done.ClickUnFocus(Msg.FinishMovingCollectionElements, 28).changes(ref changed))
                editing_List_Order = null;
            
            if (list != editing_List_Order) return changed;

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
            {
                nl();
                ef.reorder_List(list, listMeta).changes(ref changed);
            }
            else
#endif
#region Playtime UI reordering
            {
                var derivedClasses = typeof(T).TryGetDerivedClasses();

                foreach (var i in list.InspectionIndexes(listMeta)) {

                    if (listMeta == null || listMeta.allowReorder)
                    {

                        if (i > 0)
                        {
                            if (icon.Up.ClickUnFocus("Move up").changes(ref changed))
                                list.Swap(i - 1);
                                
                        }
                        else
                            icon.UpLast.write("Last");

                        if (i < list.Count - 1) {
                            if (icon.Down.ClickUnFocus("Move down").changes(ref changed))
                                list.Swap(i);
                        }
                        else icon.DownLast.write();
                    }

                    var el = list[i];

                    var isNull = el.IsNullOrDestroyed_Obj();

                    if (listMeta == null || listMeta.allowDelete)
                    {

                        if (!isNull && typeof(T).IsUnityObject())
                        {
                            if (icon.Delete.ClickUnFocus(Msg.MakeElementNull))
                                list[i] = default(T);
                        }
                        else
                        {
                            if (icon.Close.ClickUnFocus(Msg.RemoveFromCollection).changes(ref changed))
                            {
                                list.RemoveAt(InspectedIndex);
                                InspectedIndex--;
                                _lastElementToShow--;
                            }
                        }
                    }


                    if (!isNull && derivedClasses != null)
                    {
                        var ty = el.GetType();
                        if (@select(ref ty, derivedClasses, el.GetNameForInspector()))
                            list[i] = (el as ICfg).TryDecodeInto<T>(ty);
                    }

                    if (!isNull)
                        write(el.GetNameForInspector());
                    else
                        "{0} {1}".F(icon.Empty.GetText(), typeof(T).ToPegiStringType()).write();

                    nl();
                }

            }

#endregion

#region Select
            var selectedCount = 0;

            if (listMeta == null) {
                for (var i = 0; i < list.Count; i++)
                    if (selectedEls[i]) selectedCount++;
            }
            else for (var i = 0; i < list.Count; i++)
                if (listMeta.GetIsSelected(i)) selectedCount++;

            if (selectedCount > 0 && icon.DeSelectAll.Click(icon.DeSelectAll.GetText()))
                SetSelected(listMeta, list, false);

            if (selectedCount == 0 && icon.SelectAll.Click(icon.SelectAll.GetText()))
                SetSelected(listMeta, list, true);


#endregion

#region Copy, Cut, Paste, Move 

            if (list.Count > 1 && typeof(IGotIndex).IsAssignableFrom(typeof(T)))
            {

                bool down = false;

                if (icon.Down.Click("Sort Ascending").changes(ref down) || icon.Up.Click("Sort Descending"))
                {
                    changed = true;

                    list.Sort((emp1, emp2) => {

                        var igc1 = emp1 as IGotIndex;
                        var igc2 = emp2 as IGotIndex;

                        if (igc1 == null || igc2 == null)
                            return 0;

                        return (down ? 1 : -1) * (igc1.IndexForPEGI - igc2.IndexForPEGI);

                    });
                } 
            }

            if (listCopyBuffer != null) {

                if (icon.Close.ClickUnFocus("Clean buffer"))
                    listCopyBuffer = null;

                bool same = listCopyBuffer == list;

                if (same && !cutPaste)
                   "DUPLICATE:".write("Selected elements are from this list", 60);
;
                if (typeof(T).IsUnityObject()) {

                    if (!cutPaste && icon.Paste.ClickUnFocus(same ? Msg.TryDuplicateSelected.GetText() : "{0} Of {1} to here".F(Msg.TryDuplicateSelected.GetText(), listCopyBuffer.GetNameForInspector())))
                    {
                        foreach (var e in _copiedElements)
                            list.TryAdd(listCopyBuffer.TryGetObj(e));
                    }

                    if (!same && cutPaste && icon.Move.ClickUnFocus("Try Move References Of {0}".F(listCopyBuffer)))
                        list.TryMoveCopiedElement();

                }
                else
                {

                    if (!cutPaste && icon.Paste.ClickUnFocus(same ? "Try to duplicate selected references" : "Try Add Deep Copy {0}".F(listCopyBuffer.GetNameForInspector())))
                    {

                        foreach (var e in _copiedElements)
                        {

                            var el = listCopyBuffer.TryGetObj(e);

                            if (el != null) {

                                var istd = el as ICfg;

                                if (istd != null)
                                    list.TryAdd(istd.CloneStd());
                                else
                                    list.TryAdd(JsonUtility.FromJson<T>(JsonUtility.ToJson(el))); 
                            }
                        }
                    }

                    if (!same && cutPaste && icon.Move.ClickUnFocus("Try Move {0}".F(listCopyBuffer)))
                        list.TryMoveCopiedElement();
                }
                
            }
            else if (selectedCount > 0)
            {
                var copyOrMove = false;

                if (icon.Copy.ClickUnFocus("Copy selected elements"))
                {
                    cutPaste = false;
                    copyOrMove = true;
                }

                if (icon.Cut.ClickUnFocus("Cut selected elements"))
                {
                    cutPaste = true;
                    copyOrMove = true;
                }

                if (copyOrMove)
                {
                    listCopyBuffer = list;
                    _copiedElements = listMeta != null ? listMeta.GetSelectedElements() :  selectedEls.GetItAll();
                }
            }


#endregion

#region Clean & Delete
            
            if (list != listCopyBuffer)
            {

                if ((listMeta == null || listMeta.allowDelete) && list.Count > 0)
                {
                    var nullOrDestroyedCount = 0;

                    for (var i = 0; i < list.Count; i++)
                        if (list[i].IsNullOrDestroyed_Obj())
                            nullOrDestroyedCount++;

                    if (nullOrDestroyedCount > 0 && icon.Refresh.ClickUnFocus("Remove all null elements"))
                    {
                        for (var i = list.Count - 1; i >= 0; i--)
                            if (list[i].IsNullOrDestroyed_Obj())
                                list.RemoveAt(i);

                        SetSelected(listMeta, list, false);
                    }
                }

                if ((listMeta == null || listMeta.allowDelete) && list.Count > 0)
                {
                    if (selectedCount > 0 && icon.Delete.ClickConfirm("delLstPegi", list,"Delete {0} Selected".F(selectedCount)))
                    {
                        if (listMeta == null)
                        {
                            for (var i = list.Count - 1; i >= 0; i--)
                                if (selectedEls[i]) list.RemoveAt(i);
                        }
                        else for (var i = list.Count - 1; i >= 0; i--)
                            if (listMeta.GetIsSelected(i))
                                list.RemoveAt(i);

                        SetSelected(listMeta, list, false);

                    }
                }
            }
#endregion

            if (listMeta != null && icon.Config.enter(ref listMeta.inspectListMeta))
                listMeta.Nested_Inspect();

            return changed;
        }

        private static bool edit_List_Order_Obj<T>(this List<T> list, ListMetaData listMeta = null) where T : Object {

            var changed = list.edit_List_Order(listMeta);

            if (list != editing_List_Order || listMeta == null) return changed;

            if (!icon.Search.ClickUnFocus("Find objects by GUID")) return changed;
            
            for (var i = 0; i < list.Count; i++)
                if (list[i] == null) {
                    var dta = listMeta.elementDatas.TryGet(i);
                    if (dta == null) continue;
                    
                    T tmp = null;
                    if (dta.TryGetByGuid(ref tmp))
                        list[i] = tmp;
                    
                }
            
            return changed;
        }

        private static IList listCopyBuffer;
        
        private static object previouslyEntered;

        public static bool InspectValueInList<T>(T el, List<T> list, int index, ref int inspected, ListMetaData listMeta = null) {

            var changed = false;

            var pl = el as IPEGI_ListInspect;

            var isPrevious = (listMeta != null && listMeta.previousInspected == index);

            if (isPrevious)
                PreviousInspectedColor.SetBgColor();

            if (pl != null)
            {
                var chBefore = GUI.changed;
                if ((pl.InspectInList(list, index, ref inspected).changes(ref changed) || (!chBefore && GUI.changed)) && (typeof(T).IsValueType)) 
                    list[index] = (T)pl;
                
                if (changed || inspected == index)
                    isPrevious = true;

            }
            else
            {

                if (el.IsNullOrDestroyed_Obj())
                {
                    var ed = listMeta?[index];
                    if (ed == null)
                        "{0}: NULL {1}".F(index, typeof(T).ToPegiStringType()).write();
                    else
                    {
                        object obj = (object) el;

                        if (ed.PEGI_inList<T>(ref obj, index, ref inspected))
                        {
                            list[index] = (T) obj;
                            isPrevious = true;
                        }
                    }
                }
                else
                {

                    var uo = el as Object;

                    var pg = el as IPEGI; 

                    var need = el as INeedAttention;
                    var warningText = need?.NeedAttention();

                    if (warningText != null)
                        AttentionColor.SetBgColor();

                    var clickHighlightHandled = false;

                    var iind = el as IGotIndex;

                    iind?.IndexForPEGI.ToString().write(20);

                    var named = el as IGotName;
                    if (named != null) {
                        var so = uo as ScriptableObject;
                        var n = named.NameForPEGI;

                        if (so) {
                            if (editDelayed(ref n).changes(ref changed)) {
                                so.RenameAsset(n);
                                named.NameForPEGI = n;
                                isPrevious = true;
                            }
                        }
                        else if (edit(ref n).changes(ref changed))
                        {
                            named.NameForPEGI = n;
                            if (typeof(T).IsValueType)
                                list[index] = (T) named;

                            isPrevious = true;
                        }
                    }
                    else
                    {
                        if (!uo && pg == null && listMeta == null)
                        {
                            if (el.GetNameForInspector().ClickLabel(Msg.InspectElement.GetText()))
                            {
                                inspected = index;
                                isPrevious = true;
                            }
                        }
                        else
                        {

                            if (uo)
                            {
                                Texture tex = uo as Texture;

                                if (tex)
                                {
                                    if (uo.ClickHighlight(tex))
                                        isPrevious = true;

                                    clickHighlightHandled = true;
                                }
                                else if (Try_NameInspect(uo).changes(ref changed))
                                    isPrevious = true;


                            }
                            else if (el.GetNameForInspector().ClickLabel().changes(ref changed))
                            {
                                inspected = index;
                                isPrevious = true;
                            }
                        }
                    }

                    if ((warningText == null &&
                         (listMeta == null ? icon.Enter : listMeta.Icon).ClickUnFocus(Msg.InspectElement)) ||
                        (warningText != null && icon.Warning.ClickUnFocus(warningText)))
                    {
                        inspected = index;
                        isPrevious = true;
                    }

                    if (!clickHighlightHandled && uo.ClickHighlight())
                        isPrevious = true;
                }
            }

            RestoreBGcolor();

            if (listMeta != null)
            {
                if (listMeta.inspected != -1)
                    listMeta.previousInspected = listMeta.inspected;
                else if (isPrevious)
                    listMeta.previousInspected = index;

            }
            else if (isPrevious)
                previouslyEntered = el;
            
            return changed;
        }
        
        private static bool InspectClassInList<T>(this object el, List<T> list, int index, ref int inspected, ListMetaData listMeta = null) where T : class {
            var changed = false;

            var pl = el as IPEGI_ListInspect;

            var isPrevious = (listMeta != null && listMeta.previousInspected == index) 
                             || (listMeta == null && previouslyEntered!= null && el == previouslyEntered);

            if (isPrevious)
                PreviousInspectedColor.SetBgColor();
            
            if (pl != null)
            {
                var chBefore = GUI.changed;
                if (pl.InspectInList(list, index, ref inspected).changes(ref changed) || (!chBefore && GUI.changed))
                    pl.SetToDirty_Obj();

                if (changed || inspected == index)
                    isPrevious = true;

            } else {

                if (el.IsNullOrDestroyed_Obj()) {
                    var ed = listMeta?[index];
                    if (ed == null)
                        "{0}: NULL {1}".F(index, typeof(T).ToPegiStringType()).write();
                    else if (ed.PEGI_inList<T>(ref el, index, ref inspected))
                        isPrevious = true;
                }
                else {

                    var uo = el as Object;

                    var pg = el as IPEGI; //el.TryGet_fromObj<IPEGI>();

                    if (pg != null)
                        el = pg;

                    var need = el as INeedAttention;
                    var warningText = need?.NeedAttention();

                    if (warningText != null)
                        AttentionColor.SetBgColor();

                    var clickHighlightHandled = false;

                    var iind = el as IGotIndex;

                    iind?.IndexForPEGI.ToString().write(20);
                    
                    var named = el as IGotName;
                    if (named != null)
                    {
                        var so = uo as ScriptableObject;
                        var n = named.NameForPEGI;

                        if (so)
                        {
                            if (editDelayed(ref n).changes(ref changed))
                            {
                                so.RenameAsset(n);
                                named.NameForPEGI = n;
                                isPrevious = true;
                            }
                        }
                        else if (edit(ref n).changes(ref changed))
                        {
                            named.NameForPEGI = n;
                            isPrevious = true;
                        }
                    }
                    else
                    {
                        if (!uo && pg == null && listMeta == null)
                        {
                            if (el.GetNameForInspector().ClickLabel(Msg.InspectElement.GetText()))
                            {
                                inspected = index;
                                isPrevious = true;
                            }
                        } else  {
                          
                            if (uo) {
                                Texture tex = uo as Texture;

                                if (tex) {
                                    if (uo.ClickHighlight(tex))
                                        isPrevious = true;

                                    clickHighlightHandled = true;
                                }
                                else if (pegi.Try_NameInspect(uo).changes(ref changed))
                                        isPrevious = true;
                                

                            } else if (el.GetNameForInspector().ClickLabel().changes(ref changed))
                            {
                                inspected = index;
                                isPrevious = true;
                            }
                        }
                    }

                    if ((warningText == null &&
                         (listMeta == null ? icon.Enter : listMeta.Icon).ClickUnFocus(Msg.InspectElement)) ||
                        (warningText != null && icon.Warning.ClickUnFocus(warningText)))
                    {
                        inspected = index;
                        isPrevious = true;
                    }

                    if (!clickHighlightHandled && uo.ClickHighlight())
                        isPrevious = true;
                }
            }  
 
            RestoreBGcolor();

            if (listMeta != null) {
                if (listMeta.inspected != -1)
                    listMeta.previousInspected = listMeta.inspected;
                else if (isPrevious)
                    listMeta.previousInspected = index;

            } else if (isPrevious)
                previouslyEntered = el;
            


            return changed;
        }

        private static bool isMonoType<T>(IList<T> list, int i)
        {
            if (!(typeof(MonoBehaviour)).IsAssignableFrom(typeof(T))) return false;
            
            GameObject mb = null;
            if (edit(ref mb)) {
                list[i] = mb.GetComponent<T>();
                if (list[i] == null) (typeof(T).ToString() + " Component not found").showNotificationIn3D_Views();
            }
            return true;

        }

        private static bool ListAddNewClick<T>(this List<T> list, ref T added, ListMetaData ld = null) {

            if (ld != null && !ld.allowCreate)
                return false;
                    
            if (!typeof(T).IsNew())
                return list.ListAddEmptyClick(ld);

            if ((typeof(T).TryGetClassAttribute<DerivedListAttribute>() != null || typeof(T).TryGetTaggedClasses() != null))
                return false;


            string name = null;

            var sd = ld == null ? searchData : ld.searchData;

            if (sd.filteredList == list)
                name = sd.searchedText;

            if (icon.Add.ClickUnFocus(Msg.AddNewCollectionElement.GetText() + (name.IsNullOrEmpty() ? "" : " Named {0}".F(name)))) {
                if (typeof(T).IsSubclassOf(typeof(Object))) 
                    list.Add(default(T));
                else {
                    added = name.IsNullOrEmpty() ? QcUtils.AddWithUniqueNameAndIndex(list) : QcUtils.AddWithUniqueNameAndIndex(list, name);
                }

                return true;
            }

            return false;
        }

        private static bool ListAddEmptyClick<T>(this List<T> list, ListMetaData ld = null)
        {

            if (ld != null && !ld.allowCreate)
                return false;

            if (!typeof(T).IsUnityObject() && (typeof(T).TryGetClassAttribute<DerivedListAttribute>() != null || typeof(T).TryGetTaggedClasses() != null))
                return false;

            if (icon.Add.ClickUnFocus(Msg.AddNewCollectionElement.GetText()))
            {
                list.Add(default(T));
                return true;
            }
            return false;
        }

        #endregion

        #region List of MonoBehaviour


        public static bool edit_List_MB<T>(this string label, ref List<T> list, ref int inspected) where T : MonoBehaviour {
            label.write_Search_ListLabel( ref inspected, list);
            var changed = false;
            edit_List_MB(ref list, ref inspected, ref changed).listLabel_Used();
            return changed;
        }

        public static bool edit_List_MB<T>(this ListMetaData metaDatas, ref List<T> list) where T : MonoBehaviour {
            metaDatas.write_Search_ListLabel(list);
            bool changed = false;
            edit_List_MB(ref list, ref metaDatas.inspected, ref changed, metaDatas).listLabel_Used();
            return changed;
        }

        public static T edit_List_MB<T>(ref List<T> list, ref int inspected, ref bool changed, ListMetaData listMeta = null) where T : MonoBehaviour
        {

            if (listIsNull(ref list))
                return null;

            var added = default(T);
            
            var before = inspected;

            list.ClampIndexToCount(ref inspected, -1);

            changed |= (inspected != before);

            if (inspected == -1)
            {
                list.ListAddEmptyClick(listMeta).changes(ref changed);

                if (listMeta != null && icon.Save.ClickUnFocus("Save GUID & Names data to ListMeta"))
                    listMeta.SaveElementDataFrom(list);

                list.edit_List_Order_Obj(listMeta).changes(ref changed);

                if (list != editing_List_Order) {

                    foreach (var i in list.InspectionIndexes(listMeta))
                    {

                        var el = list[i];
                        if (!el)
                        {
                            T obj = null;

                            if (listMeta.TryInspect(ref obj, i))
                            {
                                if (obj)
                                {
                                    list[i] = obj.GetComponent<T>();
                                    if (!list[i]) (typeof(T).ToString() + " Component not found").showNotificationIn3D_Views();
                                }
                            }
                        }
                        else
                            el.InspectClassInList(list, i, ref inspected, listMeta).changes(ref changed);
                        
                        newLine();
                    }
                }
                else
                    list.list_DropOption(listMeta);

            }
            else list.ExitOrDrawPEGI(ref inspected).changes(ref changed);

            newLine();

            return added;
        }
        
#endregion

#region List of ScriptableObjects

        public static T edit_List_SO<T>(this string label, ref List<T> list, ref int inspected, ref bool changed) where T : ScriptableObject
        {
            label.write_Search_ListLabel(ref inspected, list);

            return edit_List_SO(ref list, ref inspected, ref changed).listLabel_Used();
        }
        
        public static bool edit_List_SO<T>(ref List<T> list, ref int inspected) where T : ScriptableObject
        {
            var changed = false;

            edit_List_SO<T>(ref list, ref inspected, ref changed);

            return changed;
        }

        public static bool edit_List_SO<T>(this string label, ref List<T> list, ref int inspected) where T : ScriptableObject
        {
            label.write_Search_ListLabel(ref inspected, list);

            var changed = false;

            edit_List_SO<T>(ref list, ref inspected, ref changed).listLabel_Used();

            return changed;
        }

        public static bool edit_List_SO<T>(this string label, ref List<T> list) where T : ScriptableObject
        {
            label.write_Search_ListLabel(list);

            var changed = false;

            var edited = -1;

            edit_List_SO<T>(ref list, ref edited, ref changed).listLabel_Used();

            return changed;
        }

        public static bool edit_List_SO<T>(this ListMetaData listMeta, ref List<T> list) where T : ScriptableObject
        {
            write_Search_ListLabel(listMeta, list);

            var changed = false;

            edit_List_SO(ref list, ref listMeta.inspected, ref changed, listMeta).listLabel_Used();

            return changed;
        }

        public static T edit_List_SO<T>(ref List<T> list, ref int inspected, ref bool changed, ListMetaData listMeta = null) where T : ScriptableObject
        {
            if (listIsNull(ref list))
                return null;
            
            var added = default(T);

            var before = inspected;
            if (list.ClampIndexToCount(ref inspected, -1))
            changed |= (inspected != before);

            if (inspected == -1)
            {

                list.edit_List_Order_Obj(listMeta).changes(ref changed);

                list.ListAddEmptyClick(listMeta).changes(ref changed);

                if (listMeta != null && icon.Save.ClickUnFocus("Save GUID & Names to ListMeta"))
                    listMeta.SaveElementDataFrom(list);

                if (list != editing_List_Order) {
                    foreach (var i in list.InspectionIndexes(listMeta)) {
                        var el = list[i];
                        if (!el)
                        {
                            if (listMeta.TryInspect(ref el, i).nl(ref changed))
                                list[i] = el;
                            
                        }
                        else
                            el.InspectClassInList(list, i, ref inspected, listMeta).nl(ref changed);

                    }

                    if (typeof(T).TryGetDerivedClasses() != null)
                        list.PEGI_InstantiateOptions_SO(ref added, listMeta).nl(ref changed);

                    nl();

                }
                else list.list_DropOption(listMeta);
            }
            else list.ExitOrDrawPEGI(ref inspected).changes(ref changed);

            newLine();
            return added;
        }
        
#endregion

#region List of Unity Objects

        public static bool edit_List_UObj<T>(this string label, ref List<T> list, ref int inspected, List<T> selectFrom = null) where T : UnityEngine.Object
        {
            label.write_Search_ListLabel(ref inspected, list);
            return edit_or_select_List_UObj(ref list, selectFrom, ref inspected);
        }

        public static bool edit_List_UObj<T>(ref List<T> list, ref int inspected, List<T> selectFrom = null) where T : UnityEngine.Object
            => edit_or_select_List_UObj(ref list, selectFrom, ref inspected);

        public static bool edit_List_UObj<T>(this string label, ref List<T> list, List<T> selectFrom = null) where T : UnityEngine.Object {
            label.write_Search_ListLabel(list);
            return list.edit_List_UObj(selectFrom).listLabel_Used();
        }

        public static bool edit_List_UObj<T>(this List<T> list, List<T> selectFrom = null) where T : UnityEngine.Object{
                var edited = -1;
                return edit_or_select_List_UObj(ref list, selectFrom, ref edited);
        }
        
        public static bool edit_List_UObj<T>(this ListMetaData listMeta, ref List<T> list, List<T> selectFrom = null) where T : UnityEngine.Object
        {
            listMeta.write_Search_ListLabel(list);
            return edit_or_select_List_UObj(ref list, selectFrom, ref listMeta.inspected, listMeta).listLabel_Used();
        }

        public static bool edit_or_select_List_UObj<T,G>(ref List<T> list, List<G> from, ref int inspected, ListMetaData listMeta = null) where T : G where G : UnityEngine.Object
        {
            if (listIsNull(ref list))
                return false;
            
            var changed = false;

            var before = inspected;
            if (list.ClampIndexToCount(ref inspected, -1))
            changed |= (inspected != before);

            if (inspected == -1) {

                if (listMeta != null && icon.Save.ClickUnFocus("Save GUID & Names to List MEta"))
                    listMeta.SaveElementDataFrom(list);

                list.edit_List_Order(listMeta).changes(ref changed);

                if (list != editing_List_Order)
                {
                    list.ListAddEmptyClick(listMeta).changes(ref changed);

                    foreach (var i in list.InspectionIndexes(listMeta))     {
                        var el = list[i];
                        if (!el)
                        {
                            if (!from.IsNullOrEmpty() && select_SameClass(ref el, from))
                                list[i] = el;

                            if (listMeta.TryInspect(ref el, i).changes(ref changed))
                                list[i] = el;
                        }
                        else
                            list[i].InspectClassInList(list, i, ref inspected, listMeta).changes(ref changed);

                        newLine();
                    }
                }
                else
                    list.list_DropOption(listMeta);

            }
            else list.ExitOrDrawPEGI(ref inspected).changes(ref changed);

            newLine();
            return changed;

        }

#endregion

#region List of New()

        public static T edit<T>(this ListMetaData ld, ref List<T> list, ref bool changed)
        {
            ld.write_Search_ListLabel(list);
            return edit_List(ref list, ref ld.inspected, ref changed, ld).listLabel_Used();
        }
        
        public static bool edit_List<T>(this string label, ref List<T> list, ref int inspected) 
        {
            label.write_Search_ListLabel(ref inspected, list);
            return edit_List(ref list, ref inspected).listLabel_Used();
        }

        public static bool edit_List<T>(ref List<T> list, ref int inspected) 
        {
            var changes = false;
            edit_List(ref list, ref inspected, ref changes);
            return changes;
        }

        public static bool edit_List<T>(this string label, ref List<T> list) 
        {
            label.write_Search_ListLabel(list);
            return edit_List(ref list).listLabel_Used();
        }

        public static bool edit_List<T>(ref List<T> list) 
        {
            var edited = -1;
            var changes = false;
            edit_List(ref list, ref edited, ref changes);
            return changes;
        }

        public static T edit_List<T>(this string label, ref List<T> list, ref bool changed)
        {
            label.write_Search_ListLabel(list);
            return edit_List(ref list, ref changed).listLabel_Used();
        }

        public static T edit_List<T>(ref List<T> list, ref bool changed)
        {
            var edited = -1;
            return edit_List(ref list, ref edited, ref changed);
        }

        public static T edit_List<T>(this string label, ref List<T> list, ref int inspected, ref bool changed)
        {
            label.write_Search_ListLabel(ref inspected, list);
            return edit_List(ref list, ref inspected, ref changed).listLabel_Used();
        }

        public static bool edit_List<T>(this ListMetaData listMeta, ref List<T> list)  {

            write_Search_ListLabel(listMeta, list);
            var changed = false;
            edit_List(ref list, ref listMeta.inspected, ref changed, listMeta).listLabel_Used();
            return changed;
        }

        public static T edit_List<T>(this ListMetaData listMeta, ref List<T> list, ref bool changed) {

            write_Search_ListLabel(listMeta, list);
            return edit_List(ref list, ref listMeta.inspected, ref changed, listMeta).listLabel_Used();

        }

        public static T edit_List<T>(ref List<T> list, ref int inspected, ref bool changed, ListMetaData listMeta = null)  {

            var added = default(T);

            if (list == null) {
                if (Msg.Init.F(Msg.List).ClickUnFocus())
                    list = new List<T>();
                else 
                    return added;
            }

            if (inspected >= list.Count)
            {
                inspected = -1;
                changed = true;
            }
            
            if (inspected == -1)  {

                list.edit_List_Order(listMeta).changes(ref changed);

                if (list != editing_List_Order) {
                    
                    list.ListAddNewClick(ref added, listMeta).changes(ref changed);

                    foreach (var i in list.InspectionIndexes(listMeta))   {

                        var el = list[i];
                        if (el.IsNullOrDestroyed_Obj()) {
                            if (!isMonoType(list, i)) {
                                write(typeof(T).IsSubclassOf(typeof(Object))
                                    ? "use edit_List_UObj"
                                    : "is NUll");
                            }
                        }
                        else
                            InspectValueInList(list[i], list, i, ref inspected, listMeta).changes(ref changed);
                        
                        
                        newLine();
                    }

                    list.PEGI_InstantiateOptions(ref added, listMeta).nl(ref changed);
                }
            }
            else list.ExitOrDrawPEGI(ref inspected).changes(ref changed);

            newLine();
            return added;
        }

#region Tagged Types

    public static T edit_List<T>(this ListMetaData listMeta, ref List<T> list, TaggedTypesCfg types, ref bool changed)
    {
        write_Search_ListLabel(listMeta, list);
        return edit_List(ref list, ref listMeta.inspected, types, ref changed, listMeta).listLabel_Used();
    }

    public static bool edit_List<T>(this ListMetaData listMeta, ref List<T> list, TaggedTypesCfg types) {
        bool changed = false;
        write_Search_ListLabel(listMeta, list);

        edit_List(ref list, ref listMeta.inspected, types, ref changed, listMeta).listLabel_Used();
        return changed;
    }

    public static T edit_List<T>(this string label, ref List<T> list, ref int inspected, TaggedTypesCfg types, ref bool changed) {
        label.write_Search_ListLabel(ref inspected, list);
        return edit_List(ref list, ref inspected, types, ref changed).listLabel_Used();
    }
        
    public static T edit_List<T>(ref List<T> list, ref int inspected, TaggedTypesCfg types, ref bool changed, ListMetaData listMeta = null) {

        var added = default(T);

        if (list == null)
        {
            if (Msg.Init.F(Msg.List).ClickUnFocus())
                list = new List<T>();
            else
                return added;
        }
        
        if (inspected >= list.Count)
        {
            inspected = -1;
            changed = true;
        }
        
        if (inspected == -1) {

            changed |= list.edit_List_Order(listMeta);

            if (list != editing_List_Order) {
 
                foreach (var i in list.InspectionIndexes(listMeta))  {

                    var el = list[i];
                    if (el == null) {

                        if (!isMonoType(list, i)) {
                            write(typeof(T).IsSubclassOf(typeof(Object))
                                ? "use edit_List_UObj"
                                : "is NUll");
                        }
                    }
                    else
                        InspectValueInList(list[i], list, i, ref inspected, listMeta).changes(ref changed);

                    newLine();
                }

                list.PEGI_InstantiateOptions(ref added, types, listMeta).nl(ref changed);
            }
        }
        else list.ExitOrDrawPEGI(ref inspected).changes(ref changed);

        newLine();
        return added;
    }

#endregion

#endregion

#region List by Lambda 

#region SpecialLambdas

        private static IList listElementsRoles;

        private static Color lambda_Color(Color val)
        {
            edit(ref val);
            return val;
        }

        private static Color32 lambda_Color(Color32 val)
        {
            edit(ref val);
            return val;
        }

        private static int lambda_int(int val)
        {
            edit(ref val);
            return val;
        }

        private static string lambda_string_role(string val)
        {
            var role = listElementsRoles.TryGetObj(InspectedIndex);
            if (role != null)
                role.GetNameForInspector().edit(90, ref val);
            else edit(ref val);

            return val;
        }

        public static string lambda_string(string val)
        {
            edit(ref val);
            return val;
        }

        private static T lambda_Obj_role<T>(T val) where T : UnityEngine.Object {

            var role = listElementsRoles.TryGetObj(InspectedIndex);
            if (!role.IsNullOrDestroyed_Obj())
                role.GetNameForInspector().edit(90, ref val);
            else edit(ref val);

            return val;
        }

        public static bool edit_List(this string label, ref List<int> list) =>
            label.edit_List(ref list, lambda_int);

        public static bool edit_List(this string label, ref List<Color> list) =>
            label.edit_List(ref list, lambda_Color);

        public static bool edit_List(this string label, ref List<Color32> list) =>
            label.edit_List(ref list, lambda_Color);

        public static bool edit_List(this string label, ref List<string> list) =>
            label.edit_List(ref list, lambda_string);

        public static bool edit_List_WithRoles(this string label, ref List<string> list, IList roles)
        {
            listElementsRoles = roles;
            return label.edit_List(ref list, lambda_string_role);
        }

        public static bool edit_List_WithRoles<T>(this string label, ref List<T> list, IList roles) where T : UnityEngine.Object
        {
            label.write_Search_ListLabel(list);
            listElementsRoles = roles;
            var ret = edit_List_UObj(ref list, lambda_Obj_role);
            listElementsRoles = null;
            return ret;
        }

#endregion
        
        public static T edit_List<T>(this string label, ref List<T> list, ref bool changed, Func<T, T> lambda) where T : new()
        {
            label.write_Search_ListLabel(list);
            return edit_List(ref list, ref changed, lambda).listLabel_Used();
        }

        public static T edit_List<T>(ref List<T> list, ref bool changed, Func<T, T> lambda) where T : new() {

            var added = default(T);

            if (listIsNull(ref list))
                return added;

            list.edit_List_Order().changes(ref changed);

            if (list != editing_List_Order) {

                list.ListAddNewClick(ref added).changes(ref changed);

                foreach (var i in list.InspectionIndexes()) {
                    var el = list[i];

                    var ch = GUI.changed;
                    el = lambda(el);
                    if (!ch && GUI.changed)
                    {
                        list[i] = el;
                        changed = true;
                    }

                    nl();
                }
                
            }

            newLine();
            return added;
        }

        public static bool edit_List<T>(this string label, ref List<T> list, Func<T, T> lambda) where T : new()
        {
            label.write_Search_ListLabel(list);
            return edit_List(ref list, lambda).listLabel_Used();
        }

        public static T edit_List<T>(this string label, ref List<T> list, Func<T, T> lambda, ref bool changed) where T : new()
        {
            label.write_Search_ListLabel(list);
            return edit_List(ref list, lambda, ref changed).listLabel_Used();
        }

        public static bool edit_List<T>(ref List<T> list, Func<T, T> lambda) where T : new()
        {
            var changed = false;
            edit_List(ref list, lambda, ref changed);
            return changed;

        }

        public static T edit_List<T>(ref List<T> list, Func<T, T> lambda, ref bool changed) where T : new()
        {
            var added = default(T);
   
            if (listIsNull(ref list))
                return added;

            list.edit_List_Order().changes(ref changed);

            if (list != editing_List_Order)
            {

                list.ListAddNewClick(ref added).changes(ref changed);

                foreach (var i in list.InspectionIndexes())
                {
                    var el = list[i];
                    var ch = GUI.changed;
                    el = lambda(el);
                    nl();
                    if (ch || !GUI.changed) continue;
                    list[i] = el;
                    changed = true;
                    
                }
            }

            newLine();
            return added;
        }

        public static bool edit_List_UObj<T>(ref List<T> list, Func<T, T> lambda) where T : UnityEngine.Object
        {

            var changed = false;

            if (listIsNull(ref list))
                return changed;

            list.edit_List_Order().changes(ref changed);

            if (list != editing_List_Order) {

                list.ListAddEmptyClick().changes(ref changed);

                foreach (var i in list.InspectionIndexes()) {
                    var el = list[i];
                    var ch = GUI.changed;
                    el = lambda(el);
                    nl();
                    if (ch || !GUI.changed) continue;

                    changed = true;
                    list[i] = el;
                }
                
            }

            newLine();
            return changed;
        }

        public static bool edit_List(this string name, ref List<string> list, Func<string, string> lambda)
        {
            name.write_Search_ListLabel(list);
            return edit_List(ref list, lambda).listLabel_Used();
        }

        public static bool edit_List(ref List<string> list, Func<string, string> lambda)
        {
           
            if (listIsNull(ref list))
                return false;

            var changed = list.edit_List_Order();

            if (list != editing_List_Order) {
                if (icon.Add.ClickUnFocus().changes(ref changed))
                    list.Add("");
                  
                foreach (var i in list.InspectionIndexes()) {
                    var el = list[i];

                    var ch = GUI.changed;
                    el = lambda(el);
                    nl();
                    if (ch || !GUI.changed) continue;

                    changed = true;
                    list[i] = el;
                }
                
            }

            newLine();
            return changed;
        }

#endregion

#region List of Not New()

        public static bool write_List<T>(this string label, List<T> list, Func<T, bool> lambda)
        {
            label.write_Search_ListLabel(list);
            return list.write_List(lambda).listLabel_Used();

        }

        public static bool write_List<T>(this List<T> list, Func<T, bool> lambda) {

            if (list == null)
            {
                "Empty List".nl();
                return false;
            }

            var changed = false;
            
            foreach (var i in list.InspectionIndexes())
                lambda(list[i]).nl(ref changed);
            
            nl();

            return changed;
        }

        public static bool write_List<T>(this string label, List<T> list)
        {
            var edited = -1;
            label.write_Search_ListLabel(list);
            return list.write_List<T>(ref edited).listLabel_Used();
        }

        public static bool write_List<T>(this string label, List<T> list, ref int inspected)
        {
            nl();
            label.write_Search_ListLabel(ref inspected, list);

            return list.write_List<T>(ref inspected).listLabel_Used();
        }

        public static bool write_List<T>(this List<T> list, ref int edited)
        {
            var changed = false;

            var before = edited;

            list.ClampIndexToCount(ref edited, -1); 

            changed |= (edited != before);

            if (edited == -1)
            {
                nl();

                for (var i = 0; i < list.Count; i++) {

                    var el = list[i];
                    if (el == null)
                        write("NULL");
                    else
                        InspectValueInList(list[i], list, i, ref edited).changes(ref changed);
                    
                    nl();
                }
            }
            else
                list.ExitOrDrawPEGI(ref edited).changes(ref changed);


            newLine();
            return changed;
        }
        
#endregion

#endregion

#region Dictionaries
        
        public static bool editKey(ref Dictionary<int, string> dic, int key)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
            {
                return ef.editKey(ref dic, key);
            }
            else
#endif
            {
                checkLine();
                int pre = key;
                if (editDelayed(ref key, 40))
                    return dic.TryChangeKey(pre, key);

                return false;
            }
        }

        public static bool edit(ref Dictionary<int, string> dic, int atKey)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(ref dic, atKey);
#endif
            
            var val = dic[atKey];
            if (editDelayed(ref val, 40)) {
                dic[atKey] = val;
                return false;
            }

            return false;
            
        }

        private static bool dicIsNull<G,T>(ref Dictionary<G,T> dic)
        {
            if (dic == null) {
                if (Msg.Init.F(Msg.Dictionary).ClickUnFocus().nl())
                    dic = new Dictionary<G, T>();
                else
                    return true;
            }
            return false;
        }

        public static bool edit_Dictionary_Values<G, T>(this string label, ref Dictionary<G, T> dic, ref int inspected)
        {
            label.write_Search_ListLabel(ref inspected, null);
            return edit_Dictionary_Values(ref dic, ref inspected);
        }

        public static bool edit_Dictionary_Values<G, T>(ref Dictionary<G, T> dic, ref int inspected, ListMetaData ld = null)
        {
            bool changed = false;

            int before = inspected;
            inspected = Mathf.Clamp(inspected, -1, dic.Count - 1);
            changed |= (inspected != before);

            if (inspected == -1)
            {
                for (int i = 0; i < dic.Count; i++)
                {
                    var item = dic.ElementAt(i);
                    var itemKey = item.Key;
                    if ((ld == null || ld.allowDelete) && icon.Delete.ClickUnFocus(25).changes(ref changed))
                    {
                        dic.Remove(itemKey);
                        i--;
                    }
                    else
                    {

                        var el = item.Value;

                        var named = el as IGotName;
                        if (named != null)
                        {
                            var n = named.NameForPEGI;
                            if (edit(ref n, 120))
                                named.NameForPEGI = n;
                        }
                        else
                            write(el.GetNameForInspector(), 120);

                        if ((el is IPEGI) && icon.Enter.ClickUnFocus(Msg.InspectElement, 25))
                            inspected = i;
                    }
                    newLine();
                }
            }
            else
            {
                if (icon.Back.ClickUnFocus(25).nl().changes(ref changed))
                    inspected = -1;
                else
                {
                    var el = dic.ElementAt(inspected);

                    object obj = el.Value;

                    if (Nested_Inspect(ref obj).changes(ref changed))
                        dic[el.Key] = (T)obj;
                }
            }

            newLine();
            return changed;
        }

        public static bool edit_Dictionary_Values(this string label, ref Dictionary<int, string> dic, List<string> roles)
        {
            write_Search_ListLabel(label, dic.ToList());
            return edit_Dictionary_Values(ref dic, roles);
        }

        public static bool edit_Dictionary_Values(ref Dictionary<int, string> dic, List<string> roles) {
            listElementsRoles = roles;
            var ret = edit_Dictionary_Values(ref dic, lambda_string_role, false);
            listElementsRoles = null;
            return ret;
        }

        public static bool edit_Dictionary_Values<G, T>(this string label, ref Dictionary<G, T> dic, Func<T, T> lambda, bool showKey = true, ListMetaData ld = null)
        {
            write_Search_ListLabel(label);
            return edit_Dictionary_Values(ref dic, lambda, false, ld);
        }

        public static bool edit_Dictionary_Values<G, T>(ref Dictionary<G, T> dic, Func<T, T> lambda, bool showKey = true ,ListMetaData ld = null)
        {
           

            if (dicIsNull(ref dic))
                return false;

            nl();

            var changed = false;

            for (var i = 0; i < dic.Count; i++) {
                var item = dic.ElementAt(i);
                var itemKey = item.Key;

                InspectedIndex = Convert.ToInt32(itemKey);

                if ((ld == null || ld.allowDelete) && icon.Delete.ClickUnFocus(25).changes(ref changed)) 
                    dic.Remove(itemKey);
                else {
                    if (showKey)
                        itemKey.GetNameForInspector().write(50);

                    var el = item.Value;
                    var ch = GUI.changed;
                    el = lambda(el);
                    
                    if (!ch && GUI.changed)
                        dic[itemKey] = el;
                    
                }
                nl();
            }

            return changed;
        }

        public static bool edit_Dictionary(this string label, ref Dictionary<int, string> dic)
        {
            write_Search_ListLabel(label);
            return edit_Dictionary(ref dic);
        }

        public static bool edit_Dictionary(ref Dictionary<int, string> dic) {

            bool changed = false;

            if (dicIsNull(ref dic))
                return changed;

            for (int i = 0; i < dic.Count; i++) {

                var e = dic.ElementAt(i);
                InspectedIndex = e.Key;

                if (icon.Delete.ClickUnFocus(20))
                    changed |= dic.Remove(e.Key);
                else
                {
                    changed |= editKey(ref dic, e.Key);
                    if (!changed)
                        changed |= edit(ref dic, e.Key);
                }
                newLine();
            }
            newLine();

            changed |= dic.newElement();

            return changed;
        }

        private static string newEnumName = "UNNAMED";
        private static int newEnumKey = 1;

        private static bool newElement(this Dictionary<int, string> dic)
        {
            bool changed = false;
            newLine();
            "______New [Key, Value]".nl();
            changed |= edit(ref newEnumKey, 50);
            changed |= edit(ref newEnumName);
            string dummy;
            bool isNewIndex = !dic.TryGetValue(newEnumKey, out dummy);
            bool isNewValue = !dic.ContainsValue(newEnumName);

            if ((isNewIndex) && (isNewValue) && (icon.Add.ClickUnFocus(Msg.AddNewCollectionElement, 25).changes(ref changed)))
            {
                dic.Add(newEnumKey, newEnumName);
                newEnumKey = 1;
                string ddm;
                while (dic.TryGetValue(newEnumKey, out ddm))
                    newEnumKey++;
                newEnumName = "UNNAMED";
            }

            if (!isNewIndex)
                "Index Takken by {0}".F(dummy).write();
            else if (!isNewValue)
                write("Value already assigned ");

            newLine();

            return changed;
        }
 
#endregion

#region Arrays

        public static bool edit_Array<T>(this string label, ref T[] array) 
        {
            int inspected = -1;
            label.write_Search_ListLabel(array);
            return edit_Array(ref array, ref inspected).listLabel_Used();
        }

        public static bool edit_Array<T>(this string label, ref T[] array, ref int inspected)   {
            label.write_Search_ListLabel(ref inspected, array);
            return edit_Array(ref array, ref inspected).listLabel_Used();
        }

        public static bool edit_Array<T>(ref T[] array, ref int inspected) 
        {
            bool changes = false;
            edit_Array(ref array, ref inspected, ref changes);
            return changes;
        }

        public static T edit_Array<T>(ref T[] array, ref int inspected, ref bool changed, ListMetaData metaDatas = null)  {


            var added = default(T);

            if (array == null)  {
                if (Msg.Init.F(Msg.Array).ClickUnFocus().nl())
                    array = new T[0];
            } else {

                ExitOrDrawPEGI(array, ref inspected).changes(ref changed);

                if (inspected != -1) return added;

                if (!typeof(T).IsNew()) {
                    if (icon.Add.ClickUnFocus(Msg.AddEmptyCollectionElement))
                        array = array.ExpandBy(1);
                } else if (icon.Create.ClickUnFocus(Msg.AddNewCollectionElement))
                    QcSharp.AddAndInit(ref array, 1);
                    
                edit_Array_Order(ref array, metaDatas).nl(ref changed);

                if (array == _editingArrayOrder) return added;

                for (var i = 0; i < array.Length; i++) 
                    InspectValueInList(array[i], null, i, ref inspected, metaDatas).nl(ref changed);
            }

            return added;
        }

#endregion

#region Transform

        private static bool _editLocalSpace = false;
        public static bool PEGI_CopyPaste(this Transform tf, ref bool editLocalSpace)
        {
            bool changed = false;

            "Local".toggle(40, ref editLocalSpace).changes(ref changed);

            if (icon.Copy.ClickUnFocus("Copy Transform"))
                StdExtensions.copyBufferValue = tf.Encode(editLocalSpace).ToString();
 
            if (StdExtensions.copyBufferValue != null && icon.Paste.ClickUnFocus("Paste Transform").changes(ref changed)) {
                StdExtensions.copyBufferValue.DecodeInto(tf);
                StdExtensions.copyBufferValue = null;
            }

            nl();

            return changed;
        }
        public static bool PEGI_CopyPaste(this Transform tf)
        {

            return tf.PEGI_CopyPaste(ref _editLocalSpace);
        }

        public static bool inspect(this Transform tf, bool editLocalSpace)
        {
            var changed = false;

            if (editLocalSpace)
            {
                var v3 = tf.localPosition;
                if ("Pos".edit(ref v3).nl())
                    tf.localPosition = v3;

                var rot = tf.localRotation.eulerAngles;
                if ("Rot".edit(ref rot).nl())
                    tf.localRotation = Quaternion.Euler(rot);

                v3 = tf.localScale;
                if ("Size".edit(ref v3).nl())
                    tf.localScale = v3;
            }
            else
            {
                var v3 = tf.position;
                if ("Pos".edit(ref v3).nl())
                    tf.position = v3;

                var rot = tf.rotation.eulerAngles;
                if ("Rot".edit(ref rot).nl())
                    tf.rotation = Quaternion.Euler(rot);

            }
            return changed;
        }
        public static bool inspect(this Transform tf) => tf.inspect(_editLocalSpace);

#endregion

#region Inspect Name

        public static bool Try_NameInspect(object obj, string label = "", string tip = "") {
            bool could;
            return obj.Try_NameInspect(out could, label, tip);
        }

        private static bool Try_NameInspect(this object obj, out bool couldInspect, string label = "", string tip = "") {

            var changed = false;

            bool gotLabel = !label.IsNullOrEmpty();

            couldInspect = true;
            var iname = obj as IGotName;
            if (iname != null)
                return iname.inspect_Name(label);

            Object uObj = obj as ScriptableObject;
                
            if (!uObj)
                uObj = QcUnity.TryGetGameObjectFromObj(obj); 
            
            if (uObj) {
                var n = uObj.name;
                if (gotLabel ? label.editDelayed(tip, 80, ref n) : editDelayed(ref n)) {
                    uObj.name = n;
                    uObj.RenameAsset(n);
                    changed = true;
                }
            }
            else
                  couldInspect = false;
            
            return changed;
        }
        
        public static bool inspect_Name(this IGotName obj) => obj.inspect_Name("", obj.GetNameForInspector());

        public static bool inspect_Name(this IGotName obj, string label) => obj.inspect_Name(label, label);

        public static bool inspect_Name(this IGotName obj, string label, string hint)
        {
       
            var n = obj.NameForPEGI;

            bool gotLabel = !label.IsNullOrEmpty();

            var uObj = obj as Object;

            if (uObj)
            {

                if ((gotLabel && label.editDelayed(80, ref n)) || (!gotLabel && editDelayed(ref n)))
                {
                    obj.NameForPEGI = n;

                    return true;
                }
            } else
            if ((gotLabel && label.edit(80, ref n) || (!gotLabel && edit(ref n))))
            {
                obj.NameForPEGI = n;
                return true;
            }

            return false;
        }
        
#endregion

#region Searching

        public static bool SearchMatch (this IList list, string searchText) => list.Cast<object>().Any(e => Try_SearchMatch_Obj(e, searchText));
        
        public static bool Try_SearchMatch_Obj (object obj, string searchText) => SearchMatch_Obj_Internal(obj, new string[] { searchText });

        private static bool SearchMatch_Obj_Internal(this object obj, string[] text, int[] indexes = null) {

            if (obj.IsNullOrDestroyed_Obj())
                return false;

            var go = QcUnity.TryGetGameObjectFromObj(obj);

            var matched = new bool[text.Length];

            if (go) {

                if (go.TryGet<IPEGI_Searchable>().SearchMatch_Internal(text, ref matched))
                    return true;

                if (go.TryGet<IGotName>().SearchMatch_Internal(text, ref matched))
                    return true;

                if (go.TryGet<IGotDisplayName>().SearchMatch_Internal(text, ref matched))
                    return true;

                if (go.name.SearchMatch_Internal(text, ref matched))
                    return true;

                if (go.TryGet<INeedAttention>().SearchMatch_Internal(text, ref matched))
                    return true;

                if (!indexes.IsNullOrEmpty() && go.TryGet<IGotIndex>().SearchMatch_Internal(indexes))
                    return true;

            } else {

                if ((QcUnity.TryGet_fromObj<IPEGI_Searchable>(obj)).SearchMatch_Internal(text, ref matched))
                    return true;

                if (QcUnity.TryGet_fromObj<IGotName>(obj).SearchMatch_Internal(text, ref matched))
                    return true;

                if (QcUnity.TryGet_fromObj<IGotDisplayName>(obj).SearchMatch_Internal(text, ref matched))
                    return true;

                if (QcUnity.TryGet_fromObj<INeedAttention>(obj).SearchMatch_Internal(text, ref matched))
                    return true;

                if (obj.ToString().SearchMatch_Internal(text, ref matched))
                    return true;

                if (!indexes.IsNullOrEmpty() && go.TryGet<IGotIndex>().SearchMatch_Internal(indexes))
                    return true;
            }

            return false;
        }

        private static bool SearchMatch_Internal(this IPEGI_Searchable searchable, string[] text, ref bool[] matched) {
            if (searchable == null) return false;

            var fullMatch = true;

            for (var i = 0; i < text.Length; i++)
                if (!matched[i]) {
                    if (!searchable.String_SearchMatch(text[i]))
                        fullMatch = false;
                    else
                        matched[i] = true;
                }

            return fullMatch;

        }

        private static bool SearchMatch_Internal(this INeedAttention needAttention, string[] text, ref bool[] matched)
            => needAttention?.NeedAttention().SearchMatch_Internal(text, ref matched) ?? false;
        
        private static bool SearchMatch_Internal(this IGotName gotName, string[] text, ref bool[] matched)
            =>  gotName?.NameForPEGI.SearchMatch_Internal(text, ref matched) ?? false;
       
        private static bool SearchMatch_Internal(this IGotDisplayName gotDisplayName, string[] text, ref bool[] matched) =>
             gotDisplayName?.NameForDisplayPEGI().SearchMatch_Internal(text, ref matched) ?? false;
            
        private static bool SearchMatch_Internal(this string label, string[] text, ref bool[] matched)
        {

            if (label.IsNullOrEmpty())
                return false;

            var fullMatch = true;
            
            for (var i = 0; i < text.Length; i++)
                if (!matched[i]) {
                    if (!text[i].IsSubstringOf(label))
                        fullMatch = false;
                    else
                        matched[i] = true;
                }

            return fullMatch;
            
        }

        private static bool SearchMatch_Internal(this IGotIndex gotIndex, int[] indexes) => gotIndex != null && indexes.Any(t => gotIndex.IndexForPEGI == t);
        
        private static SearchData searchData = new SearchData();

        private static readonly char[] splitCharacters = { ' ', '.' };
        
        public class SearchData: AbstractCfg, ICanBeDefaultCfg {
            public IList filteredList;
            public string searchedText;
            public int uncheckedElement = 0;
            public int inspectionIndexStart = 0;
            public bool filterByNeedAttention = false;
            private string[] searchBys;
            public List<int> filteredListElements = new List<int>();

            public void ToggleSearch(IList ld, string label = "")
            {

                if (ld == null)
                    return;

                var active = ld == filteredList;

                var changed = false;

                if (active && icon.FoldedOut.Click("{0} {1} {2}".F(icon.Hide.GetText(), icon.Search.GetText() ,ld), 20).changes(ref changed))
                    active = false;

                if (!active && ld!=editing_List_Order && icon.Search.Click("{0} {1}".F(icon.Search.GetText(), label.IsNullOrEmpty() ? ld.ToString() : label), 20).changes(ref changed)) 
                    active = true;


                if (active) {
                    icon.Warning.write("Filter by warnings");
                    if (toggle(ref filterByNeedAttention))
                        Refresh();
                }

                if (!changed) return;

                filteredList = active ? ld : null;

            }

            public bool Searching(IList list) =>
                list == filteredList && (filterByNeedAttention || !searchBys.IsNullOrEmpty());

            public void SearchString(IList list, out bool searching, out string[] searchBy)
            {
                searching = false;
               
                if (list == filteredList) {

                    nl();
                    if (edit(ref searchedText) || icon.Refresh.Click("Search again", 20).nl())
                    {
                        Refresh();
                        searchBys = searchedText.Split(splitCharacters, StringSplitOptions.RemoveEmptyEntries);
                        
                    }

                    searching = filterByNeedAttention || !searchBys.IsNullOrEmpty();
                }

                searchBy = searchBys;
            }

            public void Refresh()
            {
                filteredListElements.Clear();
                uncheckedElement = 0;
                inspectionIndexStart = 0;
            }

            public override CfgEncoder Encode() => new CfgEncoder().Add_String("s", searchedText);

            public override bool Decode(string tg, string data)
            {
                switch (tg)
                {
                    case "s": searchedText = data; 
    break;
                    default: return false;
                }
                return true;
            }

            public override bool IsDefault => searchedText.IsNullOrEmpty();

        }

        #endregion


        #region Inspect Extensions
        public static bool Nested_Inspect(InspectionDelegate function)
        {
            var changed = false;

            var il = IndentLevel;

            if (function().changes(ref changed))  
                function.Target.SetToDirty_Obj();
            
            IndentLevel = il;
            
            return changed;
        }
        
        private static object SetToDirty_Obj(this object obj)
        {

            #if UNITY_EDITOR
                QcUnity.SetToDirty(obj as UnityEngine.Object);
            #endif

            return obj;
        }

        public static int focusInd;

        private static Dictionary<IPEGI, int> inspectionChain = new Dictionary<IPEGI, int>();

        public static void ResetInspectedChain() => inspectionChain.Clear();

        public static bool Nested_Inspect(this IPEGI pgi, ref bool changed) =>
            pgi.Nested_Inspect().changes(ref changed);

        public static bool Nested_Inspect(this IPEGI pgi)
        {

            if (pgi.IsNullOrDestroyed_Obj())
                return false;

            var isFOOE = isFoldedOutOrEntered;

            var changed = false;

            int recurses;

            if (!inspectionChain.TryGetValue(pgi, out recurses) || recurses < 2) {
                
                inspectionChain[pgi] = recurses + 1;

                var indent = IndentLevel;

                pgi.Inspect().RestoreBGColor().changes(ref changed);

                IndentLevel = indent;

                var count = inspectionChain[pgi];
                if (count == 1)
                    inspectionChain.Remove(pgi);
                else
                    inspectionChain[pgi] = count - 1;
            }
            else
                "3rd recursion".writeWarning();

            if (changed || globChanged)
            {
#if UNITY_EDITOR
                ef.ClearFromPooledSerializedObjects(pgi as Object);
#endif
                pgi.SetToDirty_Obj();
            }

            isFoldedOutOrEntered = isFOOE;

            return changed;

        }

        public static bool Inspect_AsInList<T>(this T obj, List<T> list, int current, ref int inspected) where T : IPEGI_ListInspect
        {

            var il = IndentLevel;
            
            var changes = obj.InspectInList(list, current, ref inspected);
            
            IndentLevel = il;

            if (globChanged || changes)
            {
#if UNITY_EDITOR
                ef.ClearFromPooledSerializedObjects(obj as Object);
#endif
                obj.SetToDirty_Obj();
            }

            return changes;
        }

        public static bool Inspect_AsInList(this IPEGI_ListInspect obj)
        {
            var tmp = -1;

            var il = IndentLevel;

            var changes = obj.InspectInList(null, 0, ref tmp);
            IndentLevel = il;


            if (globChanged || changes)
            {
#if UNITY_EDITOR
                ef.ClearFromPooledSerializedObjects(obj as Object);
#endif
                obj.SetToDirty_Obj();
            }

            return changes;
        }

        #if UNITY_EDITOR
        private static readonly Dictionary<Type, Editor> defaultEditors = new Dictionary<Type, Editor>();
        #endif

        private static bool TryDefaultInspect(Object uObj) {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui && uObj)   {


                Editor ed;
                var t = uObj.GetType();
                if (!defaultEditors.TryGetValue(t, out ed))
                {
                    ed = Editor.CreateEditor(uObj);
                    defaultEditors.Add(t, ed);
                }

                if (ed == null)
                    return false;

                nl();
                EditorGUI.BeginChangeCheck();
                ed.DrawDefaultInspector();
                return EditorGUI.EndChangeCheck();
                
            }
#endif


            return false;

        }


        private static bool TryDefaultInspect(ref object obj)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui) {
                
                var uObj = obj as Object;

                if (uObj) {


                    Editor ed;
                    var t = uObj.GetType();
                    if (!defaultEditors.TryGetValue(t, out ed)) {
                        ed = Editor.CreateEditor(uObj);
                        defaultEditors.Add(t, ed);
                    }

                    if (ed == null)
                        return false;

                    nl();
                    EditorGUI.BeginChangeCheck();
                    ed.DrawDefaultInspector();
                    return EditorGUI.EndChangeCheck();
                }
            }
#endif



            if (obj!= null && obj is string) {
                var txt = obj as string;
                if (editBig(ref txt))
                {
                    obj = txt;
                    return true;
                }
            }


            return false;

        }

        public static bool Try_Nested_Inspect(this GameObject go, Component cmp = null)
        {
            var changed = false;

            IPEGI pgi = null;
            
            if (cmp)
                pgi = cmp as IPEGI;
            
            if (pgi == null)
                pgi = go.TryGet<IPEGI>();

            if (pgi != null)
                pgi.Nested_Inspect().RestoreBGColor().changes(ref changed);
            else
            {
                var mbs = go.GetComponents<Component>();

                foreach (var m in mbs)
                    TryDefaultInspect(m).changes(ref changed);
            }

            nl();
            UnIndent();

            if (changed)
                go.SetToDirty();

            return changed;
        }

        public static bool Try_Nested_Inspect(this Component cmp) => cmp && cmp.gameObject.Try_Nested_Inspect(cmp);

        public static bool Try_Nested_Inspect(object obj)
        {
            var pgi = obj as IPEGI;
            var ch = pgi?.Nested_Inspect() ?? TryDefaultInspect(ref obj);

            nl();

            UnIndent();

            return ch;
        }

        public static bool Nested_Inspect(ref object obj)
        {
            var pgi = obj as IPEGI;
            var ch = pgi?.Nested_Inspect() ?? TryDefaultInspect(ref obj);

            nl();

            UnIndent();

            return ch;
        }

        public static bool Try_enter_Inspect(object obj, ref int enteredOne, int thisOne)
        {

            var changed = false;

            var l = obj as IPEGI_ListInspect;

            if (l != null)
                return l.enter_Inspect_AsList(ref enteredOne, thisOne);

            var p = obj as IPEGI;

            if (p != null)  {
                var name = obj as IGotName;

                if (name != null) {

                    name.inspect_Name().changes(ref changed);

                    if (icon.Enter.Click())
                        enteredOne = thisOne;

                    return changed;
                }
                
                return p.GetNameForInspector()
                    .enter_Inspect(p, ref enteredOne, thisOne); 
            }

            if (enteredOne == thisOne)
                enteredOne = -1;

            return changed;
        }

        public static bool TryInspect<T>(this ListMetaData ld, ref T obj, int ind) where T : UnityEngine.Object
        {
            var el = ld.TryGetElement(ind);

            return el?.PEGI_inList_Obj(ref obj) ?? pegi.edit(ref obj);
        }

        public static int CountForInspector<T>(this List<T> lst) where T : IGotCount
        {
            var count = 0;

            foreach (var e in lst)
                if (!e.IsNullOrDestroyed_Obj())
                    count += e.CountForInspector();

            return count;
        }

        public static int TryCountForInspector<T>(this List<T> list)
        {

            if (list.IsNullOrEmpty())
                return 0;

            var count = list.Count;

            foreach (var e in list)
            {
                var cnt = e as IGotCount;
                if (!cnt.IsNullOrDestroyed_Obj())
                    count += cnt.CountForInspector();
            }

            return count;
        }

        private static bool IsNullOrDestroyed_Obj(this object obj)
        {
            if (obj as UnityEngine.Object)
                return false;

            return obj == null;
        }

        private static bool IsDefaultOrNull<T>(this T obj) => (obj == null) || EqualityComparer<T>.Default.Equals(obj, default(T));

        #endregion

#endif

        #region Inspect Utils
        public static T GetByIGotIndex<T>(this List<T> lst, int index) where T : IGotIndex
        {
#if !NO_PEGI
            if (lst != null)
                foreach (var el in lst)
                    if (!el.IsNullOrDestroyed_Obj() && el.IndexForPEGI == index)
                        return el;
#endif
            return default(T);
        }

        static bool ToPegiStringInterfacePart(this object obj, out string name)
        {
            name = null;
#if !NO_PEGI
            var dn = obj as IGotDisplayName;
            if (dn != null)
            {
                name = dn.NameForDisplayPEGI();
                if (!name.IsNullOrEmpty())
                    return true;
            }


            var sn = obj as IGotName;
            if (sn != null)
            {
                name = sn.NameForPEGI;
                if (!name.IsNullOrEmpty())
                    return true;
            }
#endif
            return false;
        }

        public static string ToPegiStringUObj<T>(this T obj) where T : Object
        {
            if (obj == null)
                return "NULL UObj {0}".F(typeof(T).ToPegiStringType());

            if (!obj)
                return "Destroyed UObj {0}".F(typeof(T).ToPegiStringType());

            string tmp;
            if (obj.ToPegiStringInterfacePart(out tmp)) return tmp;

            var cmp = obj as Component;
            return cmp ? "{0} on {1}".F(cmp.GetType().ToPegiStringType(), cmp.gameObject.name) : obj.name;
        }

        public static string GetNameForInspector<T>(this T obj)
        {

            if (obj == null)
                return "NULL {0}".F(typeof(T).ToPegiStringType());

            if (obj.GetType().IsUnityObject())
                return (obj as UnityEngine.Object).ToPegiStringUObj();

            string tmp;
            return (obj.ToPegiStringInterfacePart(out tmp)) ? tmp : obj.ToString().SimplifyTypeName();
        }

        public static string GetNameForInspector(this object obj)
        {

            if (obj is string)
                return (string)obj;

            if (obj == null) return "NULL";

            if (obj.GetType().IsUnityObject())
                return (obj as Object).ToPegiStringUObj();

            string tmp;

            return (obj.ToPegiStringInterfacePart(out tmp)) ? tmp : obj.ToString().SimplifyTypeName();
        }

        public static T GetByIGotIndex<T, G>(this List<T> lst, int index) where T : IGotIndex where G : T
        {
#if !NO_PEGI
            if (lst != null)
                foreach (var el in lst)
                    if (!el.IsNullOrDestroyed_Obj() && el.IndexForPEGI == index && el.GetType() == typeof(G))
                        return el;
#endif
            return default(T);
        }

        public static T GetByIGotName<T>(this List<T> lst, string name) where T : IGotName
        {

#if !NO_PEGI
            if (lst != null)
                foreach (var el in lst)
                    if (!el.IsNullOrDestroyed_Obj() && el.NameForPEGI.SameAs(name))
                        return el;
#endif

            return default(T);
        }

        public static T GetByIGotName<T>(this List<T> lst, T other) where T : IGotName
        {

#if !NO_PEGI
            if (lst != null && !other.IsNullOrDestroyed_Obj())
                foreach (var el in lst)
                    if (!el.IsNullOrDestroyed_Obj() && el.NameForPEGI.SameAs(other.NameForPEGI))
                        return el;
#endif

            return default(T);
        }

        public static G GetByIGotName<T, G>(this List<T> lst, string name) where T : IGotName where G : class, T
        {
#if !NO_PEGI
            if (lst != null)
                foreach (var el in lst)
                    if (!el.IsNullOrDestroyed_Obj() && el.NameForPEGI.SameAs(name) && el.GetType() == typeof(G))
                        return el as G;
#endif

            return default(G);
        }

        static Type gameViewType;
        public static void showNotificationIn3D_Views(this string text)
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

        #endregion

    }



#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore IDE0034 // Simplify 'default' expression
#pragma warning restore IDE0019 // Use pattern matching
#pragma warning restore IDE0018 // Inline variable declaration

}
