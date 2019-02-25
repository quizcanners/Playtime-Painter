
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
    using UnityEditor;
#endif
using System;
using System.Linq;

using System.Linq.Expressions;
using QuizCannersUtilities;
using Object = UnityEngine.Object;

// ReSharper disable InconsistentNaming

#pragma warning disable IDE1006
namespace PlayerAndEditorGUI {

    #region interfaces & Attributes

    public interface IPEGI
    {
    #if PEGI
        bool Inspect();
    #endif
    }

    public interface IPEGI_ListInspect
    {
    #if PEGI
        bool PEGI_inList(IList list, int ind, ref int edited);
    #endif
    }

    public interface IPEGI_Searchable
    {
    #if PEGI
        bool String_SearchMatch(string searchString);
    #endif
    }

    public interface INeedAttention
    {
    #if PEGI
        string NeedAttention();
    #endif
    }

    public interface IGotName
    {
    #if PEGI
        string NameForPEGI { get; set; }
    #endif
    }

    public interface IGotDisplayName
    {
    #if PEGI
        string NameForDisplayPEGI { get; }
    #endif
    }

    public interface IGotIndex
    {
    #if PEGI
        int IndexForPEGI { get; set; }
    #endif
    }

    public interface IGotCount
    {
    #if PEGI
        int CountForInspector { get; }
    #endif
    }

    public interface IEditorDropdown
    {
    #if PEGI
        bool ShowInDropdown();
    #endif
    }

    public interface IPegiReleaseGuiManager {
        #if PEGI
        void Inspect();
        void Write(string label);
        bool Click(string label);
        #endif
    }


    #endregion
    
    public static class pegi {

        public static string EnvironmentNl => Environment.NewLine;

        private static int mouseOverUi = -1;

        public static bool MouseOverPlaytimePainterUI {
            get { return mouseOverUi >= Time.frameCount - 1; }
            set { if (value) mouseOverUi = Time.frameCount; }
        }

        #if PEGI

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
            public Rect windowRect;

            private void DrawFunction(int windowID)
            {

                paintingPlayAreaGui = true;

                try
                {
                    globChanged = false;
                    _elementIndex = 0;
                    _lineOpen = false;
                    PEGI_Extensions.focusInd = 0;

                    if (!PopUpService.ShowingPopup())
                        function();

                    nl();

                    "Tip:{0}".F(GUI.tooltip).nl();

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

            public void Render(IPEGI p) => Render(p, p.Inspect, p.ToPegiString());

            public void Render(IPEGI target, WindowFunction doWindow, string c_windowName) {

                inspectedTerget = target;

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

        #region Other Stuff

        public static object inspectedTerget;

        private static int _elementIndex;

        public static bool isFoldedOutOrEntered;
        public static bool IsFoldedOut => isFoldedOutOrEntered;
        public static bool IsEntered => isFoldedOutOrEntered; 
        
        private static int selectedFold = -1;
        public static int tabIndex; // will be reset on every NewLine;
        
        private static bool _lineOpen;
        
        private static readonly Color AttentionColor = new Color(1f, 0.7f, 0.7f, 1);
        
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
                        
                        LastNeedAttentionMessage = " {0} on {1}:{2}".F(what, i, need.ToPegiString());
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
            name += PEGI_Extensions.focusInd.ToString();
            GUI.SetNextControlName(name);
            PEGI_Extensions.focusInd++;

            return (PEGI_Extensions.focusInd - 1);
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

            var horizontalLine = new GUIStyle();

        #if UNITY_EDITOR
            horizontalLine.normal.background = EditorGUIUtility.whiteTexture;
        #endif
            horizontalLine.margin = new RectOffset(0, 0, 4, 4);
            horizontalLine.fixedHeight = 1;

            var c = GUI.color;
            GUI.color = col;
            GUILayout.Box(GUIContent.none, horizontalLine);
            GUI.color = c;
        }

        #endregion

        #region Pop UP Services

        private static bool fullWindowDocumentationClick(string toolTip = "What is this?", int buttonSize = 20) =>
            icon.Question.BgColor(Color.clear).Click(toolTip, buttonSize).PreviousBgColor();
        
        public static bool fullWindowDocumentationClick(InspectionDelegate function, string toolTip = "What is this?", int buttonSize = 20)
        {
            if (fullWindowDocumentationClick(toolTip, buttonSize))
            {
                PopUpService.inspectDocumentationDelegate = function;
                PopUpService.InitiatePopUp();
                return true;
            }

            return false;
        }

        public static bool fullWindowDocumentationClick(this string text, string toolTip = "What is this?", int buttonSize = 20)
        {

            if (fullWindowDocumentationClick(toolTip, buttonSize))
            {
                PopUpService.popUpText = text;
                PopUpService.InitiatePopUp();
                return true;
            }

            return false;
        }

        public static class PopUpService
        {
            
            public const string DiscordServer = "https://discord.gg/rF7yXq3";

            public const string SupportEmail = "quizcanners@gmail.com";

            public static string popUpText = "";
            
            private static object popUpTarget;

            private static string understoodPopUpText = "Got it";

            public static InspectionDelegate inspectDocumentationDelegate;
            
            private static readonly List<string> gotItTexts = new List<string>()
            {
                "Got it!",
                "I understand",
                "Clear as day",
                "Roger that",
                "OK",
                "Without a shadow of a doubt",
                "Couldn't be more clear",
                "Totally got it",
                "Well said",
                "Perfect explanation",
                "Thanks",
                "Take me back",
                "Reading Done",
                "Thanks"
            };
            
            public static void InitiatePopUp()
            {
                popUpTarget = inspectedTerget;
                understoodPopUpText = gotItTexts.GetRandom();
            }

            private static void Confirm()
            {
                nl();

                if (understoodPopUpText.Click(15).nl()) {
                    popUpText = null;
                    inspectDocumentationDelegate = null;
                }

                "Didn't get the answer you need?".write();
                if (icon.Discord.Click())
                    Application.OpenURL(DiscordServer);
                if (icon.Email.Click())
                    UnityHelperFunctions.SendEmail(SupportEmail, "About this hint",
                        "The tooltip:{0}***{0} {1} {0}***{0} haven't answered some of the questions I had on my mind. Specifically: {0}".F(EnvironmentNl, popUpText));

            }

            public static bool ShowingPopup() {

                if (popUpTarget == null || popUpTarget != inspectedTerget)
                    return false;

                if (!popUpText.IsNullOrEmpty()) {
                    popUpText.writeBig();
                    Confirm();
                    return true;
                }

                if (inspectDocumentationDelegate != null)  {
                    inspectDocumentationDelegate();
                    Confirm();
                    return true;
                }

                return false;
            }

        }

        #endregion

        #region Changes 
        public static bool globChanged;

        private static bool change { get { globChanged = true; return true; } }

        private static bool Dirty(this bool val) { globChanged |= val; return val; }

        public static bool changes(this bool value, ref bool changed)
        {
            changed |= value;
            return value;
        }

        #endregion
        
        #region New Line

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
        
        private const int letterSizeInPixels = 7;

        static int ApproximateLength(this string label) => label.IsNullOrEmpty() ? 1 : letterSizeInPixels * label.Length;

        static int ApproximateLength(this string label, int otherElements) => Mathf.Min(label.IsNullOrEmpty() ? 1 : letterSizeInPixels * label.Length, Screen.width - otherElements);

        static int RemainingLength(int otherElements) => Screen.width - otherElements;


        #region Unity Object
        public static void write<T>(T field) where T : UnityEngine.Object {
        #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                ef.write(field);
        #endif
        }
        
        public static void write_obj<T>(T field, int width) where T : UnityEngine.Object
        {
        #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                ef.write(field, width);
        #endif
        }

        public static void write_obj<T>(this string label, string tip, int width, T field) where T : UnityEngine.Object
        {
            write(label, tip, width);
            write(field);

        }

        public static void write_obj<T>(this string label, int width, T field) where T : UnityEngine.Object
        {
            write(label, width);
            write(field);

        }

        public static void write_obj<T>(this string label, T field) where T : UnityEngine.Object
        {
            write(label);
            write(field);

        }

        public static void write(this Texture img, int width = defaultButtonSize)
        {

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

        public static void write(this Texture img, string tip, int width = defaultButtonSize)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                ef.write(img, tip, width);
            
            else
#endif

            
           {

                SetBgColor(Color.clear);

                img.Click(tip, width, width);

                RestoreBGcolor();
            }

        }

        public static void write(this Texture img, string tip, int width, int height)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                ef.write(img, tip, width, height);
            else
#endif
            {

                SetBgColor(Color.clear);

                img.Click(tip, width, height);

                RestoreBGcolor();

            }

        }

        #endregion

        public static void write(this icon icon, int size = defaultButtonSize) => write(icon.GetIcon(), size);

        public static void write(this icon icon, string tip, int size = defaultButtonSize) => write(icon.GetIcon(), tip, size);

        public static void write(this icon icon, string tip, int width, int height) => write(icon.GetIcon(), tip, width, height);

        public static void write(this string text)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                ef.write(text, text);
            else
#endif
            {
                checkLine();
                GUILayout.Label(text);
            }

        }

        public static void write(this string text, GUIStyle style)
        {
        #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                ef.write(text, text, style);
            else
        #endif
            {
                checkLine();
                GUILayout.Label(text);
            }
        }

        public static void write(this string text, string hint, GUIStyle style)
        {
        #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                ef.write(text, hint, style);
            else
        #endif
            {
                checkLine();
                var cont = new GUIContent() { text = text, tooltip = hint };
                GUILayout.Label(cont, style);
            }
        }

        public static void write(this string text, int width, GUIStyle style)
        {
        #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
            {
                ef.write(text, width, style);
                return;
            }
        #endif
            
            checkLine();
            GUILayout.Label(new GUIContent() { text = text }, style, GUILayout.MaxWidth(width));
            
        }

        public static void write(this string text, string hint, int width, GUIStyle style)
        {
        #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
            {
                ef.write(text, hint, width, style);
                return;
            }
        #endif
            
            checkLine();
            GUILayout.Label(new GUIContent() { text = text, tooltip = hint }, style, GUILayout.MaxWidth(width));
            
        }

        public static void write(this string text, int width) => text.write(text, width);

        public static void write(this string text, string tip)
        {

        #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
            {
                ef.write(text, tip);
                return;
            }
        #endif
            
            checkLine();
            GUILayout.Label(new GUIContent() { text = text, tooltip = tip });
    
        }

        public static void write(this string text, string tip, int width)
        {
            if (width <= 0)
                write(text, tip);

        #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
            {
                ef.write(text, tip, width);
                return;
            }
        #endif
            
            checkLine();
            GUILayout.Label(new GUIContent() { text = text, tooltip = tip }, GUILayout.MaxWidth(width));

        }

        public static void writeBig(this string text)
        {
            text.write(PEGI_Styles.OverflowText);
            pegi.nl();
        }
        public static bool write_ForCopy(string val) => edit(ref val);
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

        public static void writeHint(this string text)
        {
        
        #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
            {
                ef.writeHint(text, MessageType.Info);
                ef.newLine();
                return;
            }
        #endif
            
            checkLine();
            GUILayout.Label(text);
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

        #region Extended Select

        public static bool select(this string text, int width, ref int value, string[] array)
        {
            write(text, width);
            return select(ref value, array);
        }

        public static bool select<T>(this string text, int width, ref T value, List<T> array, bool showIndex = false, bool stripSlashes = false)
        {
            write(text, width);
            return select(ref value, array, showIndex, stripSlashes);
        }

        public static bool select(this string text, ref string val, List<string> lst)
        {
            write(text);
            return select(ref val, lst);
        }

        public static bool select<T>(this string text, ref T value, List<T> list, bool showIndex = false)
        {
            write(text);
            return select(ref value, list, showIndex);
        }

        public static bool select(this string text, int width, ref string val, List<string> lst)
        {
            write(text, width);
            return select(ref val, lst);
        }

        public static bool select<T>(this string text, ref int ind, List<T> lst, bool showIndex = false)
        {
            write(text);
            return select(ref ind, lst, showIndex);
        }

        public static bool select<T>(this string text, ref int ind, T[] lst)
        {
            write(text);
            return select(ref ind, lst);
        }

        public static bool select<T>(this string text, int width, ref int ind, T[] lst)
        {
            write(text, width);
            return select(ref ind, lst);
        }

        public static bool select<T>(this string text, string tip, int width, ref int ind, T[] lst, bool showIndex = false)
        {
            write(text, tip, width);
            return select(ref ind, lst, showIndex);
        }

        public static bool select<T>(this string text, ref T value, T[] lst, bool showIndex = false)
        {
            write(text);
            return select(ref value, lst, showIndex);
        }

        public static bool select<T>(this string text, string tip, ref int ind, List<T> lst)
        {
            write(text, tip);
            return select(ref ind, lst);
        }

        public static bool select<T>(this string text, int width, ref int ind, List<T> lst, bool showIndex = false)
        {
            write(text, width);
            return select(ref ind, lst, showIndex);
        }

        public static bool select<T>(this string text, string tip, int width, ref int ind, List<T> lst, bool showIndex = false)
        {
            write(text, tip, width);
            return select(ref ind, lst, showIndex);
        }

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
                filtered.Add(objs[i].ToPegiString());
            }

            if (tmpindex == -1)
                filtered.Add(">>{0}<<".F(no.ToPegiString()));

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

        public static bool selectOrAdd<T>(ref int selected, ref List<T> objcts) where T : UnityEngine.Object
        {
            var changed = select(ref selected, objcts);

            var tex = objcts.TryGet(selected);

            if (edit(ref tex).changes(ref changed)) {
                if (!tex)
                    selected = -1;
                else {
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

        public static bool select<T>(ref int ind, List<T> lst, int width) =>
        #if UNITY_EDITOR
            (!paintingPlayAreaGui) ?
                ef.select(ref ind, lst, width) :
        #endif            
                select(ref ind, lst);
        

        public static bool selectOrAdd<T>(this string label, int width, ref int selected, ref List<T> objs) where T : UnityEngine.Object  {
            label.write(width);
            return selectOrAdd(ref selected, ref objs);
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

        public static bool selectType<T>(ref T el, ElementData ed = null, bool keepTypeConfig = true) where T : class, IGotClassTag
        {
            object obj = el;

            if (selectType_Obj<T>(ref obj, ed, keepTypeConfig)) {
                el = obj as T;
                return change;
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
                return change;
            }

            return false;
        }

        public static bool selectTypeTag(this TaggedTypesStd types, ref string tag) => select(ref tag, types.Keys);

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
                if (from.Count>1)
                newLine();
                for (var i = 0; i < from.Count; i++) 
                    if (i != no && "{0}: {1}".F(i, from[i]).ClickUnFocus().nl()) {
                        no = i;
                        foldIn();
                        return change;
                    }
            }

            GUILayout.Space(10);

            return false;
            
        }

        public static bool select(ref int no, string[] from, int width = -1)
        {
        #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return  width > 0 ? 
                    ef.select(ref no, from, width) : 
                    ef.select(ref no, from);
        #endif

            if (from.IsNullOrEmpty())
            return false;

            foldout(from.TryGet(no,"..."));
        
            if (isFoldedOutOrEntered) {

                if (from.Length > 1)
                    newLine();

                for (var i = 0; i < from.Length; i++) 
                    if (i != no && "{0}: {1}".F(i, from[i]).ClickUnFocus().nl()) {
                        no = i;
                        foldIn();
                        return change;
                    }
                        
                
            }

            GUILayout.Space(10);

            return false;
            
        }

      /*  public static bool select(ref int no, string[] from, int width, bool showIndex = false)
        {
        #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.select(ref no, from, width);
        #endif

            if (from.IsNullOrEmpty())
                return false;

            foldout(from.TryGet(no,"..."));
            newLine();

            if (isFoldedOutOrEntered) 
                for (var i = 0; i < from.Length; i++)
                    if (i != no && (showIndex ? "{0}: {1}".F(i, from[i]) : from[i]).ClickUnFocus(width).nl())
                    {
                        no = i;
                        foldIn();
                        return change;
                    }

            GUILayout.Space(10);

            return false;
            
        }*/

        private static bool selectFinal(ref int val, ref int indexes, List<string> namesList)
        {
            var count = namesList.Count;

            if (count == 0)
                return edit(ref val);
            
            if (indexes == -1)
            {
                indexes = namesList.Count;
                namesList.Add("[{0}]".F(val.ToPegiString()));
              
            }

            var tmp = indexes;

            if (select(ref tmp, namesList.ToArray()) && (tmp < count))
            {
                indexes = tmp;
                return change;
            }

            return false;
            
        }

        private static bool selectFinal<T>(T val, ref int indexes, List<string> namesList)
        {
            var count = namesList.Count;

            if (indexes == -1 && !val.IsNullOrDestroyed_Obj())
            {
                indexes = namesList.Count;
                namesList.Add("[{0}]".F(val.ToPegiString()));
               
            }

            var tmp = indexes;

            if (select(ref tmp, namesList.ToArray()) && (tmp < count))
            {
                indexes = tmp;
                return change;
            }

            return false;
        }

        private static string _compileName<T>(bool showIndex, int index, T obj, bool stripSlashes = false)
        {
            var st = obj.ToPegiString();
            if (stripSlashes) 
                st = st.SimplifyDirectory();
            
            return (showIndex || st.Length == 0) ? "{0}: {1}".F(index, st) : st;
        }

        public static bool select<T>(ref T val, T[] lst, bool showIndex = false)
        {
            checkLine();

            var lnms = new List<string>();
            var indxs = new List<int>();

            var jindx = -1;

            for (var j = 0; j < lst.Length; j++)
            {
                var tmp = lst[j];
                if (!tmp.filterEditorDropdown().IsDefaultOrNull())
                {
                    if ((!val.IsDefaultOrNull()) && val.Equals(tmp))
                        jindx = lnms.Count;
                    lnms.Add(_compileName(showIndex, j, tmp)); //showIndex ? "{0}: {1}".F(j, tmp.ToPegiString()) : tmp.ToPegiString());
                    indxs.Add(j);
                }
            }

            if (selectFinal(val, ref jindx, lnms))
            {
                val = lst[indxs[jindx]];
                return change;
            }

            return false;

        }

        public static bool select<T>(ref int val, List<T> lst, Func<T, bool> lambda, bool showIndex = false)
        {

            checkLine();

            var lnms = new List<string>();
            var indxs = new List<int>();

            var jindx = -1;

            for (var j = 0; j < lst.Count; j++)
            {
                var tmp = lst[j];

                if ((!tmp.filterEditorDropdown().IsDefaultOrNull()) && lambda(tmp))
                {
                    if (val == j)
                        jindx = lnms.Count;
                    lnms.Add(_compileName(showIndex, j, tmp));//showIndex ? "{0}: {1}".F(j, tmp.ToPegiString()) : tmp.ToPegiString());
                    indxs.Add(j);
                }
            }


            if (selectFinal(val, ref jindx, lnms))
            {
                val = indxs[jindx];
                return change;
            }

            return false;
        }

        public static bool select_IGotIndex<T>(ref int val, List<T> lst, Func<T, bool> lambda, bool showIndex = false) where T : IGotIndex
        {

            checkLine();

            var lnms = new List<string>();
            var indxs = new List<int>();

            var jindx = -1;

            foreach (var tmp in lst) {
                
                if (tmp.filterEditorDropdown().IsDefaultOrNull() || !lambda(tmp)) continue;
                
                var ind = tmp.IndexForPEGI;

                if (val == ind)
                    jindx = lnms.Count;
                lnms.Add(_compileName(showIndex, ind, tmp));//showIndex ? "{0}: {1}".F(ind, tmp.ToPegiString()) : tmp.ToPegiString());
                indxs.Add(ind);
                
            }

            if (selectFinal(ref val, ref jindx, lnms))
            {
                val = indxs[jindx];
                return change;
            }

            return false;
        }

        public static bool select<T>(ref T val, List<T> lst, Func<T, bool> lambda, bool showIndex = false)
        {
            var changed = false;


            checkLine();

            var lnms = new List<string>();
            var indxs = new List<int>();

            var jindx = -1;

            for (var j = 0; j < lst.Count; j++)
            {
                var tmp = lst[j];
                if (!tmp.filterEditorDropdown().IsDefaultOrNull() && lambda(tmp))
                {
                    if ((jindx == -1) && tmp.Equals(val))
                        jindx = lnms.Count;

                    lnms.Add(_compileName(showIndex, j, tmp)); 
                    indxs.Add(j);
                }
            }

            if (selectFinal(val, ref jindx, lnms).changes(ref changed))
                val = lst[indxs[jindx]];
            

            return changed;

        }

        public static bool select<T>(ref T val, List<T> lst, bool showIndex = false, bool stripSlashes = false)
        {
            checkLine();

            var names = new List<string>();
            var indexes = new List<int>();

            var currentIndex = -1;

            for (var i = 0; i < lst.Count; i++)
            {
                var tmp = lst[i];
                if (tmp.filterEditorDropdown().IsDefaultOrNull()) continue;
                
                if (!val.IsDefaultOrNull() && tmp.Equals(val))
                    currentIndex = names.Count;
                
                names.Add(_compileName(showIndex, i, tmp, stripSlashes)); 
                indexes.Add(i);
                
            }

            if (selectFinal(val, ref currentIndex, names))
            {
                val = lst[indexes[currentIndex]];
                return change;
            }

            return false;

        }

        public static bool select<G,T>(ref T val, Dictionary<G, T> dic, bool showIndex = false) => select(ref val, new List<T>(dic.Values), showIndex);

        public static bool select(ref Type val, List<Type> lst, string textForCurrent, bool showIndex = false)
        {
            checkLine();

            var lnms = new List<string>();
            var indxs = new List<int>();

            var jindx = -1;

            for (var j = 0; j < lst.Count; j++)
            {
                var tmp = lst[j];
                if (tmp.IsDefaultOrNull()) continue;
                
                if ((!val.IsDefaultOrNull()) && tmp == val)
                    jindx = lnms.Count;
                lnms.Add(_compileName(showIndex, j, tmp)); //"{0}: {1}".F(j, tmp.ToPegiString()));
                indxs.Add(j);
                
            }

            if (jindx == -1 && val != null)
                lnms.Add(textForCurrent);

            if (select(ref jindx, lnms.ToArray()) && (jindx < indxs.Count))
            {
                val = lst[indxs[jindx]];
                return change;
            }

            return false;

        }

        public static bool select_SameClass<T, G>(ref T val, List<G> lst, bool showIndex = false) where T : class where G : class
        {
            var changed = false;
            var same = typeof(T) == typeof(G);

            checkLine();

            var lnms = new List<string>();
            var indxs = new List<int>();

            var jindx = -1;

            for (var j = 0; j < lst.Count; j++)
            {
                G tmp = lst[j];
                if (!tmp.filterEditorDropdown().IsDefaultOrNull() && (same || typeof(T).IsAssignableFrom(tmp.GetType())))
                {
                    if (tmp.Equals(val))
                        jindx = lnms.Count;
                    lnms.Add(_compileName(showIndex, j, tmp)); //"{0}: {1}".F(j, tmp.ToPegiString()));
                    indxs.Add(j);
                }
            }

            if (selectFinal(val, ref jindx, lnms).changes(ref changed))
                val = lst[indxs[jindx]] as T;
             
            return changed;

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
                return change;
            }

            return false;
        }

        public static bool select<T>(ref int ind, List<T> lst, bool showIndex = false)
        {

            checkLine();

            var lnms = new List<string>();
            var indxs = new List<int>();

            var jindx = -1;

            for (var j = 0; j < lst.Count; j++)
                if (!lst[j].filterEditorDropdown().IsNullOrDestroyed_Obj())
                {
                    if (ind == j)
                        jindx = indxs.Count;
                    lnms.Add(_compileName(showIndex, j, lst[j])); //lst[j].ToPegiString());
                    indxs.Add(j);
                }
            
            if (selectFinal(ind, ref jindx, lnms))
            {
                ind = indxs[jindx];
                return change;
            }

            return false;

        }
      
        public static bool select<T>(ref int no, CountlessStd<T> tree) where T : IStd, new()
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
                filtered.Add(objs[i].ToPegiString());
            }

            if (select(ref tmpindex, filtered.ToArray()))
            {
                no = inds[tmpindex];
                return change;
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
            var inds = new List<int>();
            var objs = tree.GetAllObjs(out unfinds);
            var lnms = new List<string>();
            var jindx = -1;
            var j = 0;
            for (var i = 0; i < objs.Count; i++)
            {

                var el = objs[i];

                if (!el.filterEditorDropdown().IsNullOrDestroyed_Obj() && lambda(el))
                {
                    inds.Add(unfinds[i]);
                    if (no == inds[j])
                        jindx = j;
                    lnms.Add(objs[i].ToPegiString());
                    j++;
                }
            }


            if (selectFinal(no, ref jindx, lnms))
            {
                no = inds[jindx];
                return change;
            }
            return false;
            
        }

        public static bool selectEnum<T>(ref int current) => selectEnum(ref current, typeof(T));
        
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
            return change;
        }

        public static bool select<T>(ref int ind, T[] arr, bool showIndex = false)
        {

            checkLine();

            var lnms = new List<string>();
            
            if (arr.ClampIndexToLength(ref ind)) {
                for (var i = 0; i < arr.Length; i++)
                    lnms.Add(_compileName(showIndex, i, arr[i])); 
            }

            return selectFinal(ind, ref ind, lnms);

        }

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

            if (select(ref ind, options)) {
                current = from.ElementAt(ind).Key;
                return change;
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
            
            if (select(ref ind, options, width)) {
                current = from.ElementAt(ind).Key;
                return change;
            }
            return false;
            
        }

        public static bool select(CountlessInt cint, int ind, Dictionary<int, string> from)
        {

            var value = cint[ind];

            if (select(ref value, from))  {
                cint[ind] = value;
                return change;
            }
            return false;
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

        // ***************************** Select or edit

        public static bool select_or_edit_ColorPropertyName(this string name, int width, ref string property, Material material)
        {
            name.write(width);
            return select_or_edit_TexturePropertyName(ref property, material);
        }

        public static bool select_or_edit_ColorProperty(ref string property, Material material)
        {
            var lst = material.GetColorProperties();
            return lst.Count == 0 ?  edit(ref property) : select(ref property, lst);
        }

        public static bool select_or_edit_TexturePropertyName(this string name, int width, ref string property, Material material)
        {
            name.write(width);
            return select_or_edit_TexturePropertyName(ref property, material);
        }

        public static bool select_or_edit_TexturePropertyName(ref string property, Material material)
        {
            var lst = material.MyGetTexturePropertiesNames();
            return lst.Count == 0 ? edit(ref property) : select(ref property, lst);

        }

        public static bool select_or_edit_TextureProperty(ref ShaderProperty.TextureValue property, Material material)
        {
            var lst = material.MyGetTextureProperties();
            return select(ref property, lst);

        }
        
        public static bool select_or_edit<T>(string text, string hint, int width, ref T obj, List<T> list, bool showIndex = false) where T : UnityEngine.Object
        {
            if (list.IsNullOrEmpty())
            {
                if (text != null)
                    write(text, hint, width);

                return edit(ref obj);
            }
           
            var changed = false;
            if (obj && icon.Delete.ClickUnFocus().changes(ref changed))
                obj = null;
            
            if (text != null)
                write(text, hint, width);

            select(ref obj, list, showIndex).changes(ref changed);

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
             list.IsNullOrEmpty() ?  name.edit(ref val) : name.select(ref val, list, showIndex);
        
        public static bool select_or_edit(ref string val, List<string> list, bool showIndex = false)
        {
            var changed = false;

            var gotList = !list.IsNullOrEmpty();

            var gotValue = !val.IsNullOrEmpty();

            if (gotList && gotValue && icon.Delete.ClickUnFocus())
                val = "";

            if (!gotValue || !gotList)
                changed |= edit(ref val);

            if (gotList)
                changed |= select(ref val, list, showIndex);

            return changed;
        }

        public static bool select_or_edit(this string name, ref string val, List<string> list, bool showIndex = false)
        {
            var changed = false;

            var gotList = !list.IsNullOrEmpty();

            var gotValue = !val.IsNullOrEmpty();

            if (gotList && gotValue && icon.Delete.ClickUnFocus())
                val = "";

            if (!gotValue || !gotList)
                changed |= name.edit(ref val);

            if (gotList)
                changed |= name.select(ref val, list, showIndex);

            return changed;
        }

        public static bool select_or_edit<T>(this string name, int width, ref int val, List<T> list, bool showIndex = false) =>
            list.IsNullOrEmpty() ? name.edit(width, ref val) : name.select(width, ref val, list, showIndex);
        
        public static bool select_or_edit<T>(this string name, string hint, int width, ref int val, List<T> list, bool showIndex = false) =>
            list.IsNullOrEmpty()  ? name.edit(hint, width, ref val) : name.select(hint, width, ref val, list, showIndex);
        

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

            var lnms = new List<string>();
            var indxs = new List<int>();

            var jindx = -1;

            foreach (var el in lst)
                if (!el.filterEditorDropdown().IsNullOrDestroyed_Obj())
                {
                    var index = el.IndexForPEGI;

                    if (ind == index)
                        jindx = indxs.Count;
                    lnms.Add((showIndex ? index + ": " : "") + el.ToPegiString());
                    indxs.Add(index);

                }
            
            if (selectFinal(ind, ref jindx, lnms))
            {
                ind = indxs[jindx];
                return change;
            }

            return false;
        }


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

            var lnms = new List<string>();

            var jindx = -1;


            foreach (var el in lst)
                if (!el.filterEditorDropdown().IsNullOrDestroyed_Obj())
                {
                    var name = el.NameForPEGI;

                    if (name == null) continue;
                    
                    if (val != null && val.SameAs(name))
                        jindx = lnms.Count;
                    lnms.Add(name);
                    
                }
            

            if (selectFinal(val, ref jindx, lnms))
            {
                val = lnms[jindx];
                return change;
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
            return select_iGotIndex_SameClass<T, G>(ref ind, lst, out val);
        }

        public static bool select_iGotIndex_SameClass<T, G>(this string label, ref int ind, List<T> lst, out G val) where G : class, T where T : IGotIndex
        {
            write(label);
            return select_iGotIndex_SameClass<T, G>(ref ind, lst, out val);
        }

        public static bool select_iGotIndex_SameClass<T, G>(ref int ind, List<T> lst, out G val) where G : class, T where T : IGotIndex {
            val = default(G);

            if (lst == null)
                return false;
            
            var lnms = new List<string>();
            var indxs = new List<int>();
            var els = new List<G>();
            var jindx = -1;

            foreach (var el in lst)
            {
                var g = el as G;

                if (g.filterEditorDropdown().IsNullOrDestroyed_Obj()) continue;
                
                var index = g.IndexForPEGI;

                if (ind == index)
                {
                    jindx = indxs.Count;
                    val = g;
                }
                lnms.Add(el.ToPegiString());
                indxs.Add(index);
                els.Add(g);
                
            }

            if (lnms.Count == 0)
                return edit(ref ind);
            else
            if (selectFinal(ref ind, ref jindx, lnms))
            {
                ind = indxs[jindx];
                val = els[jindx];
                return change;
            }

            return false;
        }

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
        
        public static bool enter(this icon ico, ref bool state, string tip = null)
        {

            if (state)
            {
                if (icon.Exit.ClickUnFocus(tip))
                    state = false;
            }
            else 
            {
                if (ico.ClickUnFocus(tip))
                    state = true;
            }

            isFoldedOutOrEntered = state;

            return isFoldedOutOrEntered;
        }

        public static bool enter(this icon ico, string txt, ref bool state, bool showLabelIfTrue = false) {

            if (state)  {
                if (icon.Exit.ClickUnFocus("Exit {0}".F(txt)))
                    state = false;
            }
            else 
            {
                if (ico.ClickUnFocus("Enter {0}".F(txt)))
                    state = true;
            }

            if ((showLabelIfTrue || !state) &&
                txt.ClickLabel(txt, -1, state ? PEGI_Styles.ExitLabel : PEGI_Styles.EnterLabel))
                state = !state;

            isFoldedOutOrEntered = state;

            return isFoldedOutOrEntered;
        }

        public static bool enter(this icon ico, string txt, ref int enteredOne, int thisOne, bool showLabelIfTrue = false, GUIStyle enterLabelStyle = null)
        {
            var outside = enteredOne == -1;

            if (enteredOne == thisOne)
            {
                if (icon.Exit.ClickUnFocus("Exit {0}".F(txt)))
                    enteredOne = -1;

            }
            else if (outside)
            {
                if (ico.ClickUnFocus(txt))
                    enteredOne = thisOne;
            }

            if ((showLabelIfTrue || outside) &&
                txt.ClickLabel(txt, -1, outside ? (enterLabelStyle == null ? PEGI_Styles.EnterLabel : enterLabelStyle) : PEGI_Styles.ExitLabel)) 
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

            if (el != null && ico.Click(msg + el.ToPegiString())) {
                inspected = tmp;
                isFoldedOutOrEntered = true;
                return change;
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

        private static bool enter_HeaderPart<T>(this ListMetaData meta, ref List<T> list, ref int enteredOne, int thisOne, bool showLabelIfTrue = false) {

            if (listIsNull(ref list)) {
                if (enteredOne == thisOne)
                    enteredOne = -1;
                return false;
            }

            var entered = enteredOne == thisOne;

           // if (entered)
               // listMeta.searchData.ToggleSearch(list);

            var ret = meta.Icon.enter(meta.label.AddCount(list, entered), ref enteredOne, thisOne, showLabelIfTrue, list.Count == 0 ? PEGI_Styles.WrappingText : null);
            
            ret |= list.enter_SkipToOnlyElement<T>(ref meta.inspected, ref enteredOne, thisOne);
            
            return ret;
        }

        public static bool enter(this string txt, ref bool state) => icon.Enter.enter(txt, ref state);

        public static bool enter(this string txt, ref int enteredOne, int thisOne) => icon.Enter.enter(txt, ref enteredOne, thisOne);

        public static bool enter(this string txt, ref int enteredOne, int thisOne, IList forAddCount) =>
            icon.Enter.enter(txt.AddCount(forAddCount), ref enteredOne, thisOne, enterLabelStyle: forAddCount.IsNullOrEmpty() ? PEGI_Styles.WrappingText : PEGI_Styles.EnterLabel);

        public static bool enter(this string txt, ref int enteredOne, int thisOne, IGotCount forAddCount) =>
            icon.Enter.enter(txt.AddCount(forAddCount), ref enteredOne, thisOne, enterLabelStyle: forAddCount.IsNullOrDestroyed_Obj() ? PEGI_Styles.EnterLabel :
                (forAddCount.CountForInspector > 0 ? PEGI_Styles.EnterLabel : PEGI_Styles.WrappingText));

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

            var ret = (inspected == -1 ? icon.List : icon.Next).enter(txt.AddCount(list, entered), ref entered);
            ret |= list.enter_SkipToOnlyElement<T>(ref inspected, ref entered);
            return ret;
        }

        private static string TryAddCount(this string txt, object obj) {
            var c = obj as IGotCount;
            if (!c.IsNullOrDestroyed_Obj())
                txt += " [{0}]".F(c.CountForInspector);

            return txt;
        }

        public static string AddCount(this string txt, IGotCount obj)
        {
            var isNull = obj.IsNullOrDestroyed_Obj();

            var cnt = !isNull ? obj.CountForInspector : 0;
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

            if (!entered)
            {

                var el = lst[0];

                if (!el.IsNullOrDestroyed_Obj())
                {

                    var nm = el as IGotDisplayName;

                       if (nm!= null)
                            return "{0}: {1}".F(txt, nm.NameForDisplayPEGI);

                       var n = el as IGotName;

                       if (n != null)
                            return "{0}: {1}".F(txt, n.NameForPEGI);

                        
                      return "{0}: {1}".F(txt, el.ToPegiString());
                    
                }
                else return "{0} one Null Element".F(txt);
            }
            else return "{0} [1]".F(txt);
        }
        public static bool enter_Inspect(this icon ico, string txt, IPEGI var, ref int enteredOne, int thisOne, bool showLabelIfTrue = false)
        {
            if (ico.enter(txt.TryAddCount(var), ref enteredOne, thisOne, showLabelIfTrue).nl_ifNotEntered()) 
                var.Try_NameInspect();
            return isFoldedOutOrEntered && var.Nested_Inspect();
        }
        
        public static bool enter_Inspect(this IPEGI var, ref int enteredOne, int thisOne)
        {

            var lst = var as IPEGI_ListInspect;
            
            return lst!=null ? lst.enter_Inspect_AsList(ref enteredOne, thisOne) : 
                var.ToPegiString().enter_Inspect(var, ref enteredOne, thisOne);
        }

        public static bool enter_Inspect(this string txt, IPEGI var, ref int enteredOne, int thisOne) {

            if (txt.TryAddCount(var).enter(ref enteredOne, thisOne))
                var.Try_NameInspect();
            return isFoldedOutOrEntered && var.Nested_Inspect();
        }

        public static bool enter_Inspect(this string label, int width, IPEGI_ListInspect var, ref int enteredOne, int thisOne)
        {
            if (enteredOne == -1)
                label.TryAddCount(var).write(width);
            return var.enter_Inspect_AsList(ref enteredOne, thisOne);
        }

        public static bool enter_Inspect_AsList(this IPEGI_ListInspect var, ref int enteredOne, int thisOne)
        {
            var changed = false;

            var outside = enteredOne == -1;

            if (!var.IsNullOrDestroyed_Obj()) {

                if (outside)
                    var.PEGI_inList(null, thisOne, ref enteredOne).changes(ref changed);
                else if (enteredOne == thisOne) {
                    if (icon.Exit.ClickUnFocus("Exit L {0}".F(var)))
                        enteredOne = -1;
                    var.Try_Nested_Inspect().changes(ref changed);
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
                changed |= label.enter_Inspect(50, ilpgi, ref enteredOne, thisOne).nl_ifFolded();
            else {
                var ipg = obj as IPEGI;
                changed |= label.conditional_enter_inspect(ipg != null, ipg, ref enteredOne, thisOne).nl_ifFolded();
            }

        

            return changed;
        }

        public static bool conditional_enter(this icon ico, bool canEnter, ref int enteredOne, int thisOne) {

            if (!canEnter && enteredOne == thisOne)
                enteredOne = -1;

            if (canEnter)
                ico.enter(ref enteredOne, thisOne);
            else
                isFoldedOutOrEntered = false;

            return isFoldedOutOrEntered;
        }

        public static bool conditional_enter(this icon ico, string label, bool canEnter, ref int enteredOne, int thisOne)
        {

            if (!canEnter && enteredOne == thisOne)
                enteredOne = -1;

            if (canEnter)
                ico.enter(label, ref enteredOne, thisOne);
            else
                isFoldedOutOrEntered = false;

            return isFoldedOutOrEntered;
        }

        public static bool conditional_enter(this string label, bool canEnter, ref int enteredOne, int thisOne) {

            if (!canEnter && enteredOne == thisOne)
                enteredOne = -1;

            if (canEnter)
                label.enter(ref enteredOne, thisOne);
            else
                isFoldedOutOrEntered = false;

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
                changed |= label.toggleIcon(ref val);
            
            if (val)
                enter(ref enteredOne, thisOne);
            else
                isFoldedOutOrEntered = false;

            if (enteredOne == thisOne)
                changed |= label.toggleIcon(ref val);

            if (!val && enteredOne == thisOne)
                enteredOne = -1;

            return isFoldedOutOrEntered;
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

        public static bool enter_List<T>(this ListMetaData meta, ref List<T> list, ref int enteredOne, int thisOne)
        {
            var changed = false;

            if (meta.enter_HeaderPart(ref list, ref enteredOne, thisOne)) 
                meta.edit_List(ref list).nl(ref changed);

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
            
            if (enter_ListIcon(label, ref list ,ref inspectedElement, ref enteredOne, thisOne)) //if (label.AddCount(list).enter(ref enteredOne, thisOne))
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

        public static T enter_List<T>(this ListMetaData meta, ref List<T> list, ref int enteredOne, int thisOne, TaggedTypesStd types, ref bool changed) =>
            meta.enter_HeaderPart(ref list, ref enteredOne, thisOne) ? meta.edit_List(ref list, types, ref changed) : default(T);
        

      /*  public static bool enter_List<T>(this string label, ref List<T> list, ref int enteredOne, int thisOne, TaggedTypes_STD types) {

            bool changed = false;
            int insp = -1;

             if (enter_ListIcon(label, ref list,ref insp, ref enteredOne, thisOne)) 
                label.edit_List(ref list, types, ref changed);
            

            return changed;
        }*/
       
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
        public const int defaultButtonSize = 25;

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

        public static void Lock_UnlockWindowClick(GameObject go)
        {
        #if UNITY_EDITOR
            if (ActiveEditorTracker.sharedTracker.isLocked == false && icon.Unlock.ClickUnFocus("Lock Inspector Window"))
            {
                UnityHelperFunctions.FocusOn(ef.serObj.targetObject);
                ActiveEditorTracker.sharedTracker.isLocked = true;
            }
        
            if (ActiveEditorTracker.sharedTracker.isLocked && icon.Lock.ClickUnFocus("Unlock Inspector Window"))
            {
                ActiveEditorTracker.sharedTracker.isLocked = false;
                UnityHelperFunctions.FocusOn(go);
            }
        #endif
        }
        
        public static bool ClickLabel(this string label, string hint = "ClickAble Text", int width = -1, GUIStyle style = null)
        {
            SetBgColor(Color.clear);

            if (style == null)
                style = PEGI_Styles.ClickableText;

        #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return (width == -1 ? ef.Click(label, hint, style) : ef.Click(label, hint, width, style)).UnFocus().RestoreBGColor();
        #endif
            
            checkLine();
            var cont = new GUIContent() { text = label, tooltip = hint };
            return (width ==-1 ? GUILayout.Button(cont, style) : GUILayout.Button(cont, style, GUILayout.MaxWidth(width))).DirtyUnFocus().PreviousBgColor();
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
        #if UNITY_EDITOR
            !paintingPlayAreaGui ? ef.Click(tex, tip, width).UnFocus() :
        #endif
             Click(tex, tip, width).UnFocus();
        
        public static bool ClickUnFocus(this Texture tex, string tip, int width, int height) =>
        #if UNITY_EDITOR
              !paintingPlayAreaGui ? ef.Click(tex, tip, width, height).UnFocus() :
        #endif
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

        public static bool Click(this string label, int fontSize) => new GUIContent() { text = label }.Click(PEGI_Styles.ScalableBlueText(fontSize));

        public static bool Click(this string label, string hint, int fontSize) => new GUIContent() { text = label, tooltip = hint}.Click(PEGI_Styles.ScalableBlueText(fontSize));
        
        public static bool Click(this string label, GUIStyle style) => new GUIContent() {text = label}.Click(style); 
        
        public static bool Click(this GUIContent content, GUIStyle style)
        {

            #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.Click(content, style);
            #endif
            checkLine();
            return GUILayout.Button(content, style, GUILayout.MaxWidth(maxWidthForPlaytimeButtonText)).Dirty();
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
            #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.Click(text, tip);
            #endif
            checkLine();
            return GUILayout.Button(new GUIContent() { text = text, tooltip = tip }, GUILayout.MaxWidth(maxWidthForPlaytimeButtonText)).Dirty();
        }

      /*public static bool Click(this string text, string tip, int width)
        {
            #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.Click(text, tip, width);
            #endif
            checkLine();
            return GUILayout.Button(new GUIContent() { text = text, tooltip = tip }, GUILayout.MaxWidth(width)).Dirty();
        }*/

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
        
        #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.Click(img, tip, size);
        #endif
            
                checkLine();
                return GUILayout.Button(new GUIContent(img, tip), GUILayout.MaxWidth(size + 5), GUILayout.MaxHeight(size)).Dirty();

        }

        public static bool Click(this Texture img, string tip, int width, int height)
        {
            if (!img) img = icon.Empty.GetIcon();

        #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.Click(img, tip, width, height);
        #endif
                checkLine();
                return GUILayout.Button(new GUIContent(img, tip), GUILayout.MaxWidth(width), GUILayout.MaxHeight(height)).Dirty();
        }

        public static bool Click(this icon icon) => Click(icon.GetIcon(), icon.ToPegiString());

        public static bool Click(this icon icon, ref bool changed) => Click(icon.GetIcon(), icon.ToPegiString()).changes(ref changed);

        public static bool ClickUnFocus(this icon icon, ref bool changed) => ClickUnFocus(icon.GetIcon(), icon.ToPegiString()).changes(ref changed);
        
        public static bool ClickUnFocus(this icon icon, int size = defaultButtonSize) => ClickUnFocus(icon.GetIcon(), icon.ToPegiString(), size);

        public static bool ClickUnFocus(this icon icon, string tip, int size = defaultButtonSize)
        {
            if (tip == null)
                tip = icon.ToString();

            return ClickUnFocus(icon.GetIcon(), tip, size);
        }

        public static bool ClickUnFocus(this icon icon, string tip, int width, int height) => ClickUnFocus(icon.GetIcon(), tip, width, height);

        public static bool Click(this icon icon, int size) => Click(icon.GetIcon(), size);

        public static bool Click(this icon icon, string tip, int width, int height) => Click(icon.GetIcon(), tip, width, height);

        public static bool Click(this icon icon, string tip, ref bool changed, int size = defaultButtonSize) => Click(icon.GetIcon(), tip, size).changes(ref changed);

        public static bool Click(this icon icon, string tip, int size = defaultButtonSize) => Click(icon.GetIcon(), tip, size);
        
        public static bool Click(this Color col) => icon.Empty.GUIColor(col).BgColor(Color.clear).Click().RestoreGUIColor().RestoreBGColor();

        public static bool Click(this Color col, string tip, int size = defaultButtonSize) => icon.Empty.GUIColor(col).BgColor(Color.clear).Click(tip, size).RestoreGUIColor().RestoreBGColor();

        public static bool TryClickHighlight(this object obj, int width = defaultButtonSize)
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
            if (sp  && sp.Click(Msg.HighlightElement.Get(), width))
            {
                EditorGUIUtility.PingObject(sp);
                return change;
            }
        #endif
            return false;
        }

        public static bool ClickHighlight(this Texture tex, int width = defaultButtonSize)
        {
        #if UNITY_EDITOR
            if (tex && tex.Click(Msg.HighlightElement.Get(), width))
            {
                EditorGUIUtility.PingObject(tex);
                return change;
            }
        #endif

            return false;
        }
        
        public static bool ClickHighlight(this Object obj, int width = defaultButtonSize) =>
           obj.ClickHighlight(icon.Search.GetIcon(), width);

        public static bool ClickHighlight(this Object obj, Texture tex, int width = defaultButtonSize)
        {
#if UNITY_EDITOR
            if (obj && tex.Click(Msg.HighlightElement.Get()))
            {
                EditorGUIUtility.PingObject(obj);
                return change;
            }
#endif

            return false;
        }

        public static bool ClickHighlight(this Object obj, icon icon, int width = defaultButtonSize)
        {
        #if UNITY_EDITOR
            if (obj && icon.Click(Msg.HighlightElement.Get()))
            {
                EditorGUIUtility.PingObject(obj);
                return change;
            }
        #endif

            return false;
        }

        public static bool ClickHighlight(this Object obj, string hint, icon icon = icon.Enter, int width = defaultButtonSize)
        {
        #if UNITY_EDITOR
            if (obj && icon.Click(hint)) {
                EditorGUIUtility.PingObject(obj);
                return change;
            }
        #endif

            return false;
        }
        
        public static bool Click_Attention_Highlight<T>(this T obj, icon icon = icon.Enter, string hint = "", bool canBeNull = true) where T : UnityEngine.Object, INeedAttention
        {
            var ch = obj.Click_Enter_Attention(icon, hint, canBeNull);
            obj.ClickHighlight();
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
                hint = icon.ToString();

            return icon.ClickUnFocus(hint);
        }

        public static bool Click_Attention(this INeedAttention attention, Texture tex, string hint = "", bool canBeNull = true)
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
        private const int DefaultToggleIconSize = 35;

        public static bool toggleInt(ref int val)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.toggleInt(ref val);
#endif
            
            checkLine();
            var before = val > 0;
            
            if (!toggle(ref before)) return false;
            
            val = before ? 1 : 0;
            return change;
        }

        public static bool toggle(this icon icon, ref int selected, int current)
          => icon.toggle(icon.ToString(), ref selected, current);

        public static bool toggle(this icon icon, string label, ref int selected, int current)
        {
            if (selected == current)
                icon.write(label);
            else if (icon.Click(label))
                {
                selected = current;
                return change;
                }
            return false;
        }
        
        public static bool toggle(ref bool val)
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.toggle(ref val);
#endif
            
            checkLine();
            var before = val;
            val = GUILayout.Toggle(val, "", GUILayout.MaxWidth(30));
            return (before != val).Dirty();
            
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
            
            checkLine();
            var before = val;
            val = GUILayout.Toggle(val, text);
            return (before != val).Dirty();
            
        }

        public static bool toggle(ref bool val, string text, string tip)
        {

        #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.toggle(ref val, text, tip);
            
        #endif
                checkLine();
                var before = val;
                var cont = new GUIContent() { text = text, tooltip = tip };
                val = GUILayout.Toggle(val, cont);
                return (before != val).Dirty();
        }

        public static bool toggle(ref bool val, icon TrueIcon, icon FalseIcon, string tip, int width = defaultButtonSize, GUIStyle style = null) 
            => toggle(ref val, TrueIcon.GetIcon(), FalseIcon.GetIcon(), tip, width, style);

        public static bool toggle(ref bool val, icon TrueIcon, icon FalseIcon, GUIStyle style = null) => toggle(ref val, TrueIcon.GetIcon(), FalseIcon.GetIcon(), "", defaultButtonSize, style);

        public static bool toggleVisibilityIcon(this string label, string hint, ref bool val, bool dontHideTextWhenOn = false)
        {
            SetBgColor(Color.clear);

            var ret = toggle(ref val, icon.Show, icon.Hide, hint, DefaultToggleIconSize, PEGI_Styles.ToggleButton).PreviousBgColor();

            if (!val || dontHideTextWhenOn) label.write(hint, PEGI_Styles.ToggleLabel(val));

            return ret;
        }

        public static bool toggleVisibilityIcon(this string label, ref bool val, bool showTextWhenTrue = false)
        {
            var ret = toggle(ref val, icon.Show.BgColor(Color.clear), icon.Hide, label, DefaultToggleIconSize, PEGI_Styles.ToggleButton).PreviousBgColor();

            if (!val || showTextWhenTrue) label.write(PEGI_Styles.ToggleLabel(val));

            return ret;
        }

        public static bool toggleIcon(ref bool val, string hint = "Toggle On/Off") => toggle(ref val, icon.True.BgColor(Color.clear), icon.False, hint, DefaultToggleIconSize, PEGI_Styles.ToggleButton).PreviousBgColor();

        public static bool toggleIcon(ref int val, string hint = "Toggle On/Off") {
            var boo = val != 0;
            if (toggle(ref boo, icon.True.BgColor(Color.clear), icon.False, hint, DefaultToggleIconSize, PEGI_Styles.ToggleButton).PreviousBgColor())
            {
                val = boo ? 1 : 0;
                return change;
            }

            return false;
        }

        public static bool toggleIcon(this string label, string hint, ref bool val, bool hideTextWhenTrue = false)
        {
            SetBgColor(Color.clear);

            var ret = toggle(ref val, icon.True, icon.False, hint, DefaultToggleIconSize, PEGI_Styles.ToggleButton).PreviousBgColor();
            if ((!val || !hideTextWhenTrue) && 
                 label.ClickLabel(hint,-1, PEGI_Styles.ToggleLabel(val))) {
                ret = true;
                val = !val;
            }

            return ret;
        }

        public static bool toggleIcon(this string label, ref bool val, bool hideTextWhenTrue = false)
        {
            var ret = toggle(ref val, icon.True.BgColor(Color.clear), icon.False, label, DefaultToggleIconSize, PEGI_Styles.ToggleButton).PreviousBgColor();

            if ((!val || !hideTextWhenTrue) && label.ClickLabel(label, -1, PEGI_Styles.ToggleLabel(val)))
            {
                ret = true;
                val = !val;
            }

                return ret;
        }

        public static bool toggleIcon(this string labelIfFalse, ref bool val, string labelIfTrue)
            => (val ? labelIfTrue : labelIfFalse).toggleIcon(ref val);

        public static bool toggle(ref bool val, Texture2D TrueIcon, Texture2D FalseIcon, string tip, int width, GUIStyle style = null)
        {
            #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.toggle(ref val, TrueIcon, FalseIcon, tip, width, style);
            #endif
            
                checkLine();
                var before = val;

                if (val)  {
                    if (Click(TrueIcon, tip, width))
                        val = false;
                }
                else
                    if (Click(FalseIcon, tip, width))
                        val = true;

                return (before != val);
        }

        public static bool toggle(ref bool val, string text, string tip, int width)
        {

        #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
            {
                ef.write(text, tip, width);
                return ef.toggle(ref val);
            }
        
        #endif

            checkLine();
            var before = val;
            var cont = new GUIContent() { text = text, tooltip = tip };
            val = GUILayout.Toggle(val, cont);
            return (before != val).Dirty();

        }

        public static bool toggle(int ind, CountlessBool tb)
        {
        #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.toggle(ind, tb);
        #endif
            var has = tb[ind];
            if (toggle(ref has))
            {
                tb.Toggle(ind);
                return change;
            }
            return false;
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

        #region UnityObject

        public static bool edit<T>(ref T field) where T : UnityEngine.Object =>
            #if UNITY_EDITOR
             !paintingPlayAreaGui ? ef.edit(ref field) : 
            #endif
                 false;
        

        public static bool edit<T>(ref T field, int width) where T : UnityEngine.Object =>
        #if UNITY_EDITOR
                !paintingPlayAreaGui ?  ef.edit(ref field, width) :
        #endif
            false;
        

        public static bool edit<T>(ref T field, bool allowDrop) where T : UnityEngine.Object =>
        #if UNITY_EDITOR
            !paintingPlayAreaGui ? ef.edit(ref field, allowDrop) :
        #endif
                false;
        
        public static bool edit<T>(this string label, ref T field) where T : UnityEngine.Object
        {
    #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
            {
                write(label);
                return edit(ref field);
            }

    #endif

            return false;

        }

        public static bool edit<T>(this string label, ref T field, bool allowDrop) where T : UnityEngine.Object
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

        public static bool edit<T>(this string label, int width, ref T field) where T : UnityEngine.Object
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

        public static bool edit_enter_Inspect<T>(this string label, ref T obj, ref int entered, int current, List<T> selectFrom = null) where T : UnityEngine.Object
            => label.edit_enter_Inspect(90, ref obj, ref entered, current, selectFrom);
        
        public static bool edit_enter_Inspect<T>(this string label, int width ,ref T obj, ref int entered, int current, List<T> selectFrom = null) where T : UnityEngine.Object
        {
            var changed = false;

            if (entered == -1) {
                if (selectFrom == null) {
                    label.write(width);
                    if (!obj) changed |= edit(ref obj);
                        else 
                    if (icon.Delete.Click("Null this object"))
                        obj = null;
                }
                else
                    label.select_or_edit(width, ref obj, selectFrom).changes(ref changed);
            }

            var lst = obj as IPEGI_ListInspect;

            if (lst != null)
            {
                if (lst.enter_Inspect_AsList(ref entered, current))
                {
                    obj.Try_NameInspect(label);
                    changed |= obj.Try_Nested_Inspect();
                }
            }
            else if (icon.Enter.conditional_enter(obj.TryGet_fromObj<IPEGI>() != null, ref entered, current))
            {
                obj.Try_NameInspect(label);
                changed |= obj.Try_Nested_Inspect();
            }

            return changed;
        }

        #endregion

        #region Vectors
        
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
                return change;
            }

            return false;
        }

        public static bool edit(ref Vector4 val)
        {

    #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(ref val);
    #endif
        
        checkLine();
        var modified = false;
        modified |= "X".edit(ref val.x) | "Y".edit(ref val.y) | "Z".edit(ref val.z) | "W".edit(ref val.w);
        return modified;
            
        }

        public static bool edit(this string label, ref Vector4 val)
        {

        #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(label, ref val);
        #endif
            
            write(label);
            var changed = edit(ref val.x);
            edit(ref val.y).changes(ref changed);
            edit(ref val.z).changes(ref changed);
            edit(ref val.w).changes(ref changed);
            return changed;

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
            
            checkLine();
            return  edit(ref val.x) || edit(ref val.y);
        }

        public static bool edit_Range(this string label, int width, ref Vector2 vec2) {

            var x = vec2.x;
            var y = vec2.y;

            if (label.edit_Range(width, ref x, ref y)) {
                vec2.x = x;
                vec2.y = y;
                return change;
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

        #region Unity Types

        public static bool edit(this string name, ref AnimationCurve val) =>
    #if UNITY_EDITOR
            !paintingPlayAreaGui ? ef.edit(name, ref val) :
    #endif
            false;
        
        
        public static bool editTexture(this Material mat, string name) => mat.editTexture(name, name);

        public static bool editTexture(this Material mat, string name, string display)
        {
            write(display);
            var tex = mat.GetTexture(name);

            if (edit(ref tex))
            {
                mat.SetTexture(name, tex);
                return change; 
            }

            return false;
        }

        #endregion

        #region Custom Structs

        public static bool edit(ref MyIntVec2 val)
        {

    #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(ref val);
    #endif
            
            checkLine();
            return edit(ref val.x) || edit(ref val.y);
        }

        public static bool edit(ref MyIntVec2 val, int min, int max)
        {

    #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(ref val, min, max);
    #endif
            
            checkLine();
            return edit(ref val.x, min, max) || edit(ref val.y, min, max);

        }

        public static bool edit(ref MyIntVec2 val, int min, MyIntVec2 max)
        {

        #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(ref val, min, max);
        #endif
            
            checkLine();
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

        public static bool edit(ref LinearColor col)
        {
            var c = col.ToGamma();
            if (edit(ref c))
            {
                col.From(c);
                return change;
            }
            return false;
        }

        public static bool edit(this string label, ref LinearColor col)
        {
            write(label);
            return edit(ref col);
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

        #region UInt

        public static bool edit(ref uint val)
        {
    #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(ref val);
    #endif
                checkLine();
                var before = val.ToString();
                var newval = GUILayout.TextField(before);
                if (!before.SameAs(newval)) {

                    int newValue;
                    if (int.TryParse(newval, out newValue))
                        val = (uint)newValue;

                    return change;
                }
                return false;
            

        }

        public static bool edit(ref uint val, int width)
        {

    #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(ref val, width);
    #endif
            
                checkLine();
                var before = val.ToString();
                var strVal = GUILayout.TextField(before, GUILayout.MaxWidth(width));
                if (string.Compare(before, strVal) == 0) return false;
                int newValue;
                if (int.TryParse(strVal, out newValue))
                    val = (uint)newValue;

                return change;

        }

        public static bool edit(ref uint val, uint min, uint max)
        {

        #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(ref val, min, max);
        #endif
            
            checkLine();
            float before = val;
            val = (uint)GUILayout.HorizontalSlider(before, (float)min, (float)max);
            return (before != val).Dirty();
            
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

        public static bool edit(ref int val)
        {
    #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(ref val);
            
    #endif
            checkLine();
            var before = val.ToString();
            var newval = GUILayout.TextField(before);
            if (!before.SameAs( newval))
            {

                int newValue;
                if (int.TryParse(newval, out newValue))
                    val = newValue;

                return change;
            }
            return false;
        }
        
        public static bool edit(ref int val, int width)
        {

    #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(ref val, width);
    #endif
            
            checkLine();
            var before = val.ToString();
            var newval = GUILayout.TextField(before, GUILayout.MaxWidth(width));
            if (!before.SameAs(newval))
            {
                int newValue;
                if (int.TryParse(newval, out newValue))
                    val = newValue;

                return change;
            }
            return false;
            
        }
        
        public static bool edit(ref int val, int min, int max)
        {

    #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(ref val, min, max);
    #endif
            
            checkLine();
            float before = val;
            val = (int)GUILayout.HorizontalSlider(before, min, max);
            return (before != val).Dirty();
            
        }

        static int editedInteger;
        static int editedIntegerIndex;
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
            
            if (edit(ref tmp))
            {
                editedInteger = tmp;
                editedIntegerIndex = _elementIndex;
                globChanged = false;
            }

            _elementIndex++;

            return false;
            
        }

        public static bool editDelayed(this string label, ref int val, int width) {
            write(label, Msg.editDelayed_HitEnter.Get());
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
            
            checkLine();
            var before = val.ToString();
            var newval = GUILayout.TextField(before);

            if (before.SameAs(newval)) return false;
            
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
            
            checkLine();
            var before = val.ToString();
            var newval = GUILayout.TextField(before, GUILayout.MaxWidth(width));
            
            if (before.SameAs(newval)) return false;
            
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
            
            checkLine();
            var before = Mathf.Sqrt(val);
            var after = GUILayout.HorizontalSlider(before, min, max);
            if (before == after) return false;
            val = after * after;
            return change;


        }

        public static bool edit(ref float val, float min, float max)
        {

    #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(ref val, min, max);
            
    #endif
            
            checkLine();
            var before = val;

            val = GUILayout.HorizontalSlider(before, min, max);
            return (before != val);
            
        }

        static string editedFloat;
        static int editedFloatIndex;
        public static bool editDelayed(ref float val, int width)
        {

    #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.editDelayed(ref val, width);
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


            if (edit(ref tmp))
            {
                editedFloat = tmp;
                editedFloatIndex = _elementIndex;
            }

            _elementIndex++;

            return false;

        }
        
        public static bool edit(this string label, int width, ref float val)
        {
            write(label, width);
            return edit(ref val);
        }

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

        static void sliderText(this string label, float val, string tip, int width)
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

        #endregion

        #region double

        public static bool edit(ref double val)
        {

        #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(ref val);
        #endif
            
            checkLine();
            var before = val.ToString();
            var newval = GUILayout.TextField(before);
            if (before.SameAs(newval)) return false;
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
            
            checkLine();
            var before = val.ToString();
            var newval = GUILayout.TextField(before, GUILayout.MaxWidth(width));
            if (string.Compare(before, newval) != 0) {

                double newValue;
                if (double.TryParse(newval, out newValue))
                    val = newValue;

                return change;
            }
            return false;
            
        }
        
        #endregion

        #region Enum

        public static bool editEnum<T>(this string text, string tip, int width, ref T eval)
        {
            write(text, tip, width);
            return editEnum<T>(ref eval);
        }

        public static bool editEnum<T>(this string text, int width, ref T eval)
        {
            write(text, width);
            return editEnum<T>(ref eval);
        }

        public static bool editEnum<T>(this string text, ref T eval)
        {
            write(text);
            return editEnum<T>(ref eval);
        }
        
        public static bool editEnum<T>(ref T eval, int width = -1) {
            var val = Convert.ToInt32(eval);

            if (selectEnum(ref val, typeof(T), width)) {
                eval = (T)((object)val);
                return change;
            }

            return false;
        }
        
        public static int editEnum<T>(T val)
        {
            var ival = Convert.ToInt32(val);
            selectEnum(ref ival, typeof(T));
            return ival;
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
        static string editedText;
        static string editedHash = "";
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
            if (edit(ref tmp))
            {
                editedText = tmp;
                editedHash = val.GetHashCode().ToString();
            }

            return false;
            
        }

        public static bool editDelayed(this string label, ref string val)
        {
            write(label, Msg.editDelayed_HitEnter.Get());
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
            if (edit(ref tmp, width))
            {
                editedText = tmp;
                editedHash = val.GetHashCode().ToString();
            }

            return false;
            
        }

        public static bool editDelayed(this string label, ref string val, int width)
        {
            write(label, Msg.editDelayed_HitEnter.Get(), width);

            return editDelayed(ref val);

        }
        
        public static bool editDelayed(this string label, int width, ref string val)
        {
            write(label, width);
            return editDelayed(ref val);
        }

        const int maxStringSize = 1000;

        static bool LengthIsTooLong(ref string label)
        {
            if (label == null || label.Length < maxStringSize)
                return false;
           
            if (icon.Delete.ClickUnFocus())
                label = "";
            else
                write("String is too long {0}".F(label.Substring(0, 10)));
        
            return change;
        }

        public static bool edit(ref string val) {
            if (LengthIsTooLong(ref val)) return false;

    #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(ref val);
    #endif
            
            checkLine();
            var before = val;
            var newval = GUILayout.TextField(before);
            if (!before.SameAs(newval))
            {
                val = newval;
                return change;
            }
            return false;
            
        }

        public static bool edit(ref string val, int width)
        {

            if (LengthIsTooLong(ref val)) return false;

    #if UNITY_EDITOR
            if (!paintingPlayAreaGui)
                return ef.edit(ref val, width);
    #endif
            
            checkLine();
            var before = val;
            var newval = GUILayout.TextField(before, GUILayout.MaxWidth(width));
            if (!before.SameAs(newval))
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
            
            checkLine();
            var before = val;
            var newval = GUILayout.TextArea(before);
            if (!before.SameAs(newval).nl())
            {
                val = newval;
                return change;
            }
            return false;
            
        }

        public static bool editBig(this string name, ref string val) {
            write(name);
            return editBig(ref val);
        }
        
        public static bool edit(this string label, ref string val)
        {
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

        public static bool edit_Property<T>(this string label, Expression<Func<T>> memberExpression, UnityEngine.Object obj)
            => edit_Property(label, null, null, -1, memberExpression, obj);
        
        public static bool edit_Property<T>(this string label, string tip, Expression<Func<T>> memberExpression, UnityEngine.Object obj)
        => edit_Property(label, null, tip, -1, memberExpression, obj);
        
        public static bool edit_Property<T>(this Texture tex, string tip, Expression<Func<T>> memberExpression, UnityEngine.Object obj)
            => edit_Property(null, tex, tip, -1, memberExpression, obj);
        
        public static bool edit_Property<T>(this string label, Texture image, string tip, int width, Expression<Func<T>> memberExpression, UnityEngine.Object obj) {
            bool changes = false;

        #if UNITY_EDITOR
        
            if (paintingPlayAreaGui) return false;
        
            var sobj = (!obj ? ef.serObj : GetSerObj(obj)); //new SerializedObject(obj));
        
            if (sobj == null) return false;
        
            var member = ((MemberExpression)memberExpression.Body).Member;
            var name = member.Name;
    
            var cont = new GUIContent(label, image, tip);
    
            var tps = sobj.FindProperty(name);
            if (tps != null)
            {
                EditorGUI.BeginChangeCheck();
    
                if (width < 1)
                    EditorGUILayout.PropertyField(tps, cont, true);
                else
                    EditorGUILayout.PropertyField(tps, cont, true, GUILayout.MaxWidth(width));
    
                if (EditorGUI.EndChangeCheck())
                {
                    sobj.ApplyModifiedProperties();
                    changes = change;
                }
    
            }
            
        
            
        #endif
            return changes;
        }

        #if UNITY_EDITOR
        private static readonly Dictionary<UnityEngine.Object, SerializedObject> _serializedObjects = new Dictionary<UnityEngine.Object, SerializedObject>();

        static SerializedObject GetSerObj(UnityEngine.Object obj)
        {
            SerializedObject so;

            if (!_serializedObjects.TryGetValue(obj, out so))
            {
                so = new SerializedObject(obj);
                _serializedObjects.Add(obj, so);
            }

            return so;

        }
        #endif

        #endregion

        #endregion

        #region LISTS

        #region List MGMT Functions 

        private const int listLabelWidth = 105;

        private static readonly Dictionary<IList, int> ListInspectionIndexes = new Dictionary<IList, int>();

        private const int UpDownWidth = 120;
        private const int UpDownHeight = 30;
        private static int _sectionSizeOptimal;
        private static int _listSectionMax;
        private static int _listSectionStartIndex;
        private static readonly CountlessInt ListSectionOptimal = new CountlessInt();

        private static void SetOptimalSectionFor(int count)
        {
            const int listShowMax = 10;

            if (count < listShowMax) {
                _sectionSizeOptimal = listShowMax;
                return;
            }
            
            if (count > listShowMax * 3)
            {
                _sectionSizeOptimal = listShowMax;
                return;
            }

            _sectionSizeOptimal = ListSectionOptimal[count];

            if (_sectionSizeOptimal != 0) return;
            
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
        

        }

        private static IList addingNewOptionsInspected;
        static string addingNewNameHolder = "Name";

        private static void listInstantiateNewName<T>()  {
                Msg.New.Get().write(Msg.NameNewBeforeInstancing_1p.Get().F(typeof(T).ToPegiStringType()) ,30, PEGI_Styles.ExitLabel);
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
                    if (icon.Create.ClickUnFocus("Create new object").nl(ref changed))
                        added = lst.CreateAsset_SO("Assets/ScriptableObjects/", addingNewNameHolder);
                }
                else
                {
                    var selectingDerrived = lst == addingNewOptionsInspected;

                    icon.Create.foldout("Instantiate Class Options", ref selectingDerrived).nl();

                    if (selectingDerrived)
                        addingNewOptionsInspected = lst;
                    else if (addingNewOptionsInspected == lst)
                        addingNewOptionsInspected = null;

                    if (selectingDerrived)
                    {
                        if (indTypes != null)
                        foreach (var t in indTypes) {
                            write(t.ToPegiStringType());
                            if (icon.Create.ClickUnFocus().nl(ref changed))
                                added = lst.CreateAsset_SO("Assets/ScriptableObjects/", addingNewNameHolder, t);
                        }

                        if (tagTypes != null)
                        {
                            var k = tagTypes.Keys;

                            for (var i=0; i<k.Count; i++) {

                                write(tagTypes.DisplayNames[i]);
                                if (icon.Create.ClickUnFocus().nl(ref changed)) 
                                    added = lst.CreateAsset_SO("Assets/ScriptableObjects/", addingNewNameHolder, tagTypes.TaggedTypes.TryGet(k[i]));

                            }
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

            var hasName = typeof(T).IsSubclassOf(typeof(UnityEngine.Object)) || typeof(IGotName).IsAssignableFrom(typeof(T));

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
                            lst.AddWithUniqueNameAndIndex(added, addingNewNameHolder);
                        }
                    }

                if (tagTypes != null) {
                    var k = tagTypes.Keys;

                    for (var i = 0; i < k.Count; i++)
                    {

                        write(tagTypes.DisplayNames[i]);
                        if (icon.Create.ClickUnFocus().nl(ref changed)) {
                            added = (T)Activator.CreateInstance(tagTypes.TaggedTypes.TryGet((k[i])));
                            lst.AddWithUniqueNameAndIndex(added, addingNewNameHolder);
                        }

                    }
                }

                    }
            }
            else
                "Add".write("Input a name for a new element", 40);
            nl();

            return changed;
        }

        private static bool PEGI_InstantiateOptions<T>(this List<T> lst, ref T added, TaggedTypesStd types, ListMetaData ld) 
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
                                lst.AddWithUniqueNameAndIndex(added, addingNewNameHolder);
                            }
                        }
                }
            }
            else
                "Add".write("Input a name for a new element", 40);
            nl();

            return changed;
        }

        public static int InspectedIndex { get; private set; } = -1;

        private static IEnumerable<int> InspectionIndexes<T>(this List<T> list, ListMetaData ld = null) {
            
            var sd = ld == null? searchData : ld.searchData;
            
            #region Inspect Start

            bool searching;
            string[] searchby;
            sd.SearchString(list, out searching, out searchby);
  
            _listSectionStartIndex = 0;

            _listSectionMax = list.Count;

            SetOptimalSectionFor(_listSectionMax);

            if (_listSectionMax >= _sectionSizeOptimal * 2) {

                if (ld != null)
                    _listSectionStartIndex = ld.listSectionStartIndex;
                else if (!ListInspectionIndexes.TryGetValue(list, out _listSectionStartIndex))
                    ListInspectionIndexes.Add(list, 0);

                if (_listSectionMax > _sectionSizeOptimal)
                {
                    var changed = false;

                    while (_listSectionStartIndex > 0 && _listSectionStartIndex >= _listSectionMax) {
                        changed = true;
                        _listSectionStartIndex = Mathf.Max(0, _listSectionStartIndex - _sectionSizeOptimal);
                    }

                    nl();
                    if (_listSectionStartIndex > 0)
                    {
                        if (icon.Up.ClickUnFocus("To previous elements of the list. ", UpDownWidth, UpDownHeight).changes(ref changed))
                        {
                            _listSectionStartIndex = Mathf.Max(0, _listSectionStartIndex - _sectionSizeOptimal+1);
                            if (_listSectionStartIndex == 1)
                                _listSectionStartIndex = 0;
                        }
                    }
                    else
                        icon.UpLast.write("Is the first section of the list.", UpDownWidth, UpDownHeight);
                    nl();

                    if (changed)
                    {
                        if (ld != null)
                            ld.listSectionStartIndex = _listSectionStartIndex;
                        else 
                            ListInspectionIndexes[list] = _listSectionStartIndex;
                    }
                }
                else line(Color.gray);


                _listSectionMax = Mathf.Min(_listSectionMax, _listSectionStartIndex + _sectionSizeOptimal);
            }
            else if (list.Count > 0)
                line(Color.gray);

            nl();

            #endregion

            var cnt = list.Count;

            if (!searching)
            {

                for (InspectedIndex = _listSectionStartIndex; InspectedIndex < _listSectionMax; InspectedIndex++)
                {
                    switch (InspectedIndex % 4)
                    {
                        case 1: PEGI_Styles.listReadabilityBlue.SetBgColor(); break;
                        case 3: PEGI_Styles.listReadabilityRed.SetBgColor(); break;
                    }
                    yield return InspectedIndex;

                    RestoreBGcolor();

                }
            } else {

                var sectionIndex = 0;
                
                var fcnt = sd.filteredListElements.Count;

                var filtered = sd.filteredListElements;

                while (sd.uncheckedElement <= cnt && sectionIndex < _listSectionMax) {

                    InspectedIndex = -1;
                    
                    if (fcnt > _listSectionStartIndex + sectionIndex)
                        InspectedIndex = filtered[_listSectionStartIndex + sectionIndex];
                    else {
                        while (sd.uncheckedElement < cnt && InspectedIndex == -1) {
                            if (list[sd.uncheckedElement].SearchMatch_Obj_Internal(searchby)) {
                                InspectedIndex = sd.uncheckedElement;
                                sd.filteredListElements.Add(InspectedIndex);
                            }

                            sd.uncheckedElement++;
                        }
                    }
                    
                    if (InspectedIndex != -1)
                    {
            
                        switch (sectionIndex % 4)
                        {
                            case 1: PEGI_Styles.listReadabilityBlue.SetBgColor(); break;
                            case 3: PEGI_Styles.listReadabilityRed.SetBgColor(); break;
                        }
                        
                        yield return InspectedIndex;

                        RestoreBGcolor();

                        sectionIndex++;
                    }
                    else break;

                }

            }


            if (_listSectionStartIndex > 0 ||  cnt > _listSectionMax)
            {

                nl();
                if (cnt > _listSectionMax)
                {
                    if (icon.Down.ClickUnFocus("To next elements of the list. ", UpDownWidth, UpDownHeight)) {
                        _listSectionStartIndex += _sectionSizeOptimal-1;
                        ListInspectionIndexes[list] = _listSectionStartIndex;
                    }
                }
                else if (_listSectionStartIndex > 0)
                    icon.DownLast.write("Is the last section of the list. ", UpDownWidth, UpDownHeight);

            }
            else if (list.Count > 0)
                line(Color.gray);
            
        }

        private static string currentListLabel = "";
        public static string GetCurrentListLabel<T>(ListMetaData ld = null) => ld != null ? ld.label :
                    (currentListLabel.IsNullOrEmpty() ? typeof(T).ToPegiStringType()  : currentListLabel);

        private static bool listLabel_Used(this bool val) {
            currentListLabel = "";

            return val;
        }
        public static T listLabel_Used<T>(this T val)
        {
            currentListLabel = "";

            return val;
        }

        public static void write_Search_ListLabel(this string label, IList lst = null)
        {
            int notInsp = -1;
            label.write_Search_ListLabel(ref notInsp, lst);
        }

        public static void write_Search_ListLabel(this string label, ref int inspected, IList lst) {

            currentListLabel = label;

            if (inspected == -1)
                searchData.ToggleSearch(lst);

            if (lst != null && inspected >= 0 && lst.Count > inspected) 
                label = "{0}->{1}".F(label, lst[inspected].ToPegiString());
            else label = label.AddCount(lst, true);

            if (label.ClickLabel(label, -1, PEGI_Styles.ListLabel) && inspected != -1)
                inspected = -1;
        }

        public static void write_Search_ListLabel(this ListMetaData ld, IList lst) {

            var editedName = false;

            currentListLabel = ld.label;

            if (!ld.Inspecting)
                ld.searchData.ToggleSearch(lst);

            if (lst != null && ld.inspected >= 0 && lst.Count > ld.inspected) {

                var el = lst[ld.inspected];

                el.Try_NameInspect(out editedName, ld.label);
                
                currentListLabel = editedName ? ld.label+":" : "{0}->{1}".F(ld.label, lst[ld.inspected].ToPegiString());
                
            } else currentListLabel = ld.label.AddCount(lst, true);

            if (!editedName && currentListLabel.ClickLabel(ld.label, RemainingLength(70), PEGI_Styles.ListLabel) && ld.inspected != -1)
                ld.inspected = -1;
        }

        private static bool ExitOrDrawPEGI<T>(T[] array, ref int index, ListMetaData ld = null)
        {
            var changed = false;

            if (index >= 0) {
                if (array == null || index >= array.Length || icon.List.ClickUnFocus("Return to {0} array".F(GetCurrentListLabel<T>(ld))).nl())
                    index = -1;
                else
                    changed |= array[index].Try_Nested_Inspect();
            }

            return changed;
        }

        private static bool ExitOrDrawPEGI<T>(this List<T> list, ref int index, ListMetaData ld = null)
        {
            var changed = false;

            if (icon.List.ClickUnFocus("{0}[{1}] of {2}".F(Msg.ReturnToList.Get(), list.Count, GetCurrentListLabel<T>(ld))).nl())
                index = -1;
            else
                changed |= list[index].Try_Nested_Inspect();

            return changed;
        }

        private static IList editing_List_Order;

        private static bool listIsNull<T>(ref List<T> list) {
            if (list == null) {
                if ("Instantiate list".ClickUnFocus().nl())
                    list = new List<T>();
                else
                    return change;
                
            }

            return false;
        }

        private static bool list_DropOption<T>(this List<T> list) where T : UnityEngine.Object
        {
            var changed = false;
        #if UNITY_EDITOR
    
            if (ActiveEditorTracker.sharedTracker.isLocked == false && "Lock Inspector Window".ClickUnFocus())
                ActiveEditorTracker.sharedTracker.isLocked = true;

            if (ActiveEditorTracker.sharedTracker.isLocked && icon.Lock.ClickUnFocus("Unlock Inspector Window")) {
                ActiveEditorTracker.sharedTracker.isLocked = false;

                var mb = ef.serObj.targetObject as MonoBehaviour;

                UnityHelperFunctions.FocusOn(mb ? mb.gameObject : ef.serObj.targetObject);

            }

            foreach (var ret in ef.DropAreaGUI<T>())  {
                list.Add(ret);
                changed = true;
            }
        #endif
            return changed;
        }

        static Array _editingArrayOrder;

        public static CountlessBool selectedEls = new CountlessBool();

        private static List<int> _copiedElements = new List<int>();

        private static bool move;

        private static void TryMoveCopiedElement<T>(this List<T> list)
        {
            
            foreach (var e in _copiedElements)
                list.TryAdd(listCopyBuffer.TryGet(e));

            for (var i = _copiedElements.Count - 1; i >= 0; i--)
                listCopyBuffer.RemoveAt(_copiedElements[i]);

            listCopyBuffer = null;
        }

        private static bool edit_Array_Order<T>(ref T[] array, ListMetaData listMeta = null) {

            var changed = false;

            if (array != _editingArrayOrder) {
                if (icon.Edit.ClickUnFocus("Modify list elements", 28))
                    _editingArrayOrder = array;
            }

            else if (icon.Done.ClickUnFocus("Finish moving", 28).nl(ref changed))
                _editingArrayOrder = null;


            if (array != _editingArrayOrder) return changed;

            var derivedClasses = typeof(T).TryGetDerivedClasses();

            for (var i = 0; i< array.Length; i++) {

                if (listMeta == null || listMeta.allowReorder)
                {

                    if (i > 0)
                    {
                        if (icon.Up.ClickUnFocus("Move up").changes(ref changed))
                            CsharpUtils.Swap(ref array, i, i - 1);
                            
                    }
                    else
                        icon.UpLast.write("Last");

                    if (i < array.Length - 1)
                    {
                        if (icon.Down.ClickUnFocus("Move down").changes(ref changed))
                            CsharpUtils.Swap(ref array, i, i + 1);
                            
                    }
                    else icon.DownLast.write();
                }

                var el = array[i];

                var isNull = el.IsNullOrDestroyed_Obj();

                if (listMeta == null || listMeta.allowDelete) {
                    if (!isNull && typeof(T).IsUnityObject())
                    {
                        if (icon.Delete.ClickUnfocus(Msg.MakeElementNull).changes(ref changed))
                            array[i] = default(T);
                    }
                    else
                    {
                        if (icon.Close.ClickUnFocus("Remove From Array").changes(ref changed)) {
                            CsharpUtils.Remove(ref array, i);
                            i--;
                        }
                    }
                }

                if (!isNull && derivedClasses != null) {
                    var ty = el.GetType();
                    if (@select(ref ty, derivedClasses, el.ToPegiString()))
                        array[i] = (el as IStd).TryDecodeInto<T>(ty);
                }

                if (!isNull)
                    write(el.ToPegiString());
                else
                    "Empty {0}".F(typeof(T).ToPegiStringType()).write();

                nl();
            }

            return changed;
        }

        private static bool edit_List_Order<T>(this List<T> list, ListMetaData listMeta = null) {

            var changed = false;
            

            var sd = listMeta == null ? searchData : listMeta.searchData;

            if (list != editing_List_Order)
            {
                if (sd.filteredList != list && icon.Edit.ClickUnFocus("Change Order", 28))  //"Edit".ClickLabel("Change Order", 35))//
                    editing_List_Order = list;
            } else if (icon.Done.ClickUnFocus("Finish moving", 28).changes(ref changed))
                editing_List_Order = null;


            if (list != editing_List_Order) return changed;

#if UNITY_EDITOR
            if (!paintingPlayAreaGui)
            {
                nl();
                changed |= ef.reorder_List(list, listMeta);
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
                            if (icon.Delete.ClickUnfocus(Msg.MakeElementNull))
                                list[i] = default(T);
                        }
                        else
                        {
                            if (icon.Close.ClickUnfocus(Msg.RemoveFromList).changes(ref changed))
                            {
                                list.RemoveAt(InspectedIndex);
                                InspectedIndex--;
                                _listSectionMax--;
                            }
                        }
                    }


                    if (!isNull && derivedClasses != null)
                    {
                        var ty = el.GetType();
                        if (@select(ref ty, derivedClasses, el.ToPegiString()))
                            list[i] = (el as IStd).TryDecodeInto<T>(ty);
                    }

                    if (!isNull)
                        write(el.ToPegiString());
                    else
                        "Empty {0}".F(typeof(T).ToPegiStringType()).write();

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

            if (selectedCount > 0 && icon.DeSelectAll.Click("Deselect All"))
                SetSelected(listMeta, list, false);

            if (selectedCount == 0 && icon.SelectAll.Click("Select All"))
                SetSelected(listMeta, list, true);


            #endregion

            #region Copy, Cut, Paste, Move 
             
            if (listCopyBuffer != null) {

                if (icon.Close.ClickUnFocus("Clean buffer"))
                    listCopyBuffer = null;

                if (listCopyBuffer != list)
                {

                    if (typeof(T).IsUnityObject())
                    {

                        if (!move && icon.Paste.ClickUnFocus("Try Past References Of {0} to here".F(listCopyBuffer.ToPegiString())))
                        {
                            foreach (var e in _copiedElements)
                                list.TryAdd(listCopyBuffer.TryGet(e));
                        }

                        if (move && icon.Move.ClickUnFocus("Try Move References Of {0}".F(listCopyBuffer)))
                            list.TryMoveCopiedElement();

                    }
                    else
                    {

                        if (!move && icon.Paste.ClickUnFocus("Try Add Deep Copy {0}".F(listCopyBuffer.ToPegiString())))
                        {

                            foreach (var e in _copiedElements)
                            {

                                var istd = listCopyBuffer.TryGet(e) as IStd;

                                if (istd != null)
                                    list.TryAdd(istd.CloneStd());
                            }
                        }

                        if (move && icon.Move.ClickUnFocus("Try Move {0}".F(listCopyBuffer)))
                            list.TryMoveCopiedElement();
                    }
                }
            }
            else if (selectedCount > 0)
            {
                var copyOrMove = false;

                if (icon.Copy.ClickUnFocus("Copy List Elements"))
                {
                    move = false;
                    copyOrMove = true;
                }

                if (icon.Cut.ClickUnFocus("Cut List Elements"))
                {
                    move = true;
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

                    if (nullOrDestroyedCount > 0 && icon.Refresh.ClickUnFocus("Clean null elements"))
                    {
                        for (var i = list.Count - 1; i >= 0; i--)
                            if (list[i].IsNullOrDestroyed_Obj())
                                list.RemoveAt(i);

                        SetSelected(listMeta, list, false);
                    }
                }

                if ((listMeta == null || listMeta.allowDelete) && list.Count > 0)
                {
                    if (selectedCount > 0 && icon.Delete.Click("Delete {0} Selected".F(selectedCount)))
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

        private static bool edit_List_Order_Obj<T>(this List<T> list, ListMetaData listMeta = null) where T : UnityEngine.Object {
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

        static IList listCopyBuffer;

        public static bool Name_ClickInspect_PEGI<T>(this object el, List<T> list, int index, ref int edited, ListMetaData listMeta = null) {
            var changed = false;

            var pl = el.TryGet_fromObj<IPEGI_ListInspect>();

            if (pl != null)
            {
                if (pl.PEGI_inList(list, index, ref edited).changes(ref changed) || globChanged)
                    pl.SetToDirty_Obj();
            } else {

                if (el.IsNullOrDestroyed_Obj()) {
                    var ed = listMeta?[index];
                    if (ed == null)
                        "{0}: NULL {1}".F(index, typeof(T).ToPegiStringType()).write();
                    else 
                        ed.PEGI_inList<T>(ref el, index, ref edited);
                }
                else {
                    var uo = el as Object;

                    var pg = el.TryGet_fromObj<IPEGI>();
                    if (pg != null)
                        el = pg;

                    var need = el as INeedAttention;
                    var warningText = need?.NeedAttention();

                    if (warningText != null)
                        AttentionColor.SetBgColor();

                    var clickHighlightHandled = false;

                    var iind = el as IGotIndex;

                    if (iind != null)
                        iind.IndexForPEGI.ToString().write(20);

                    var named = el as IGotName;
                    if (named != null)
                    {
                        var so = uo as ScriptableObject;
                        var n = named.NameForPEGI;
                        if (so)
                        {
                            if (editDelayed(ref n))
                            {
                                so.RenameAsset(n);
                                named.NameForPEGI = n;
                            }
                        }
                        else
                            if (edit(ref n))
                            named.NameForPEGI = n;
                    }
                    else
                    {
                        if (!uo && pg == null && listMeta == null)
                        {
                            if (el.ToPegiString().ClickLabel("Click to Inspect"))
                                edited = index;
                        }
                        else
                        {
                            Texture tex;

                            if (uo)
                            {
                                tex = uo as Texture;
                                if (tex)
                                {
                                    uo.ClickHighlight(tex);
                                    clickHighlightHandled = true;
                                }
                            }

                            if (el.ToPegiString().ClickLabel())
                                edited = index;
                        }
                    }
                    
                    if ((warningText == null && (listMeta == null ? icon.Enter : listMeta.Icon).ClickUnfocus(Msg.InspectElement)) || (warningText != null && icon.Warning.ClickUnFocus(warningText)))
                        edited = index;
                        
                    if (!clickHighlightHandled)
                        uo.ClickHighlight();
                }
            }  
 
            RestoreBGcolor();

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

            if (icon.Add.ClickUnFocus(Msg.AddNewListElement.Get()))
            {
                if (typeof(T).IsSubclassOf(typeof(UnityEngine.Object))) 
                    list.Add(default(T));
                else
                    added = list.AddWithUniqueNameAndIndex();

                return change;
            }

            return false;
        }

        private static bool ListAddEmptyClick<T>(this List<T> list, ListMetaData ld = null)
        {

            if (ld != null && !ld.allowCreate)
                return false;

            if (!typeof(T).IsUnityObject() && (typeof(T).TryGetClassAttribute<DerivedListAttribute>() != null || typeof(T).TryGetTaggedClasses() != null))
                return false;

            if (icon.Add.ClickUnFocus(Msg.AddNewListElement.Get()))
            {
                list.Add(default(T));
                return change;
            }
            return false;
        }

        #endregion

        #region MonoBehaviour

        public static bool edit_List_MB<T>(this string label, ref List<T> list, ref int inspected, ref T added) where T : MonoBehaviour
        {
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
                changed |= list.ListAddEmptyClick(listMeta);

                if (listMeta != null && icon.Save.ClickUnFocus())
                    listMeta.SaveElementDataFrom(list);

                changed |= list.edit_List_Order_Obj(listMeta);

                if (list != editing_List_Order)
                {
                    // list.InspectionStart();
                    foreach (var i in list.InspectionIndexes(listMeta)) // (int i = ListSectionStartIndex; i < ListSectionMax; i++)
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
                        {
                            changed |= el.Name_ClickInspect_PEGI(list, i, ref inspected, listMeta);
                            //el.clickHighlight();
                        }
                        newLine();
                    }
                    //list.InspectionEnd().nl();
                }
                else
                    list.list_DropOption();

            }
            else changed |= list.ExitOrDrawPEGI(ref inspected);

            newLine();

            return added;
        }
        
        #endregion

        #region SO

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

                changed |= list.edit_List_Order_Obj(listMeta);

                changed |= list.ListAddEmptyClick(listMeta);

                if (listMeta != null && icon.Save.ClickUnFocus())
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
                            changed |= el.Name_ClickInspect_PEGI<T>(list, i, ref inspected, listMeta).nl(ref changed);

                    }

                    if (typeof(T).TryGetDerivedClasses() != null)
                        list.PEGI_InstantiateOptions_SO(ref added, listMeta).nl(ref changed);

                    nl();

                }
                else list.list_DropOption();
            }
            else list.ExitOrDrawPEGI(ref inspected).changes(ref changed);

            newLine();
            return added;
        }
        
        #endregion

        #region Obj

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

                if (listMeta != null && icon.Save.ClickUnFocus())
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
                            list[i].Name_ClickInspect_PEGI(list, i, ref inspected, listMeta).changes(ref changed);

                        newLine();
                    }
                }
                else
                    list.list_DropOption();

            }
            else list.ExitOrDrawPEGI(ref inspected).changes(ref changed);

            newLine();
            return changed;

        }
        
        #endregion

        #region OfNew

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
                if ("Init list".ClickUnFocus())
                    list = new List<T>();
                else 
                    return added;
            }

            var before = inspected;

            if (inspected >= list.Count)
                inspected = -1;

            changed |= (inspected != before);

            if (inspected == -1)  {

                changed |= list.edit_List_Order(listMeta);

                if (list != editing_List_Order) {
                    
                        list.ListAddNewClick(ref added, listMeta).changes(ref changed);

                    foreach (var i in list.InspectionIndexes(listMeta))   {

                        var el = list[i];
                        if (el.IsNullOrDestroyed_Obj()) {
                            if (!isMonoType(list, i))
                            {
                                write(typeof(T).IsSubclassOf(typeof(UnityEngine.Object))
                                    ? "use edit_List_UObj"
                                    : "is NUll");
                            }
                        }
                        else
                            list[i].Name_ClickInspect_PEGI(list, i, ref inspected, listMeta).changes(ref changed);

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

    public static T edit_List<T>(this ListMetaData listMeta, ref List<T> list, TaggedTypesStd types, ref bool changed)
    {
        write_Search_ListLabel(listMeta, list);
        return edit_List(ref list, ref listMeta.inspected, types, ref changed, listMeta).listLabel_Used();
    }

    public static bool edit_List<T>(this ListMetaData listMeta, ref List<T> list, TaggedTypesStd types) {
        bool changed = false;
        write_Search_ListLabel(listMeta, list);
        edit_List(ref list, ref listMeta.inspected, types, ref changed, listMeta).listLabel_Used();
        return changed;
    }


    /* public static T edit_List<T>(this string label, ref List<T> list, TaggedTypes_STD types, ref bool changed, List_Data ld = null)
    {
        if (ld != null)
            ld.write_Search_ListLabel(list);
        else 
            label.write_Search_ListLabel(ref ld.inspected, list);
        return edit_List(ref list, ref ld.inspected, types, ref changed, ld).listLabel_Used();
    }*/

    public static T edit_List<T>(this string label, ref List<T> list, ref int inspected, TaggedTypesStd types, ref bool changed) {
        label.write_Search_ListLabel(ref inspected, list);
        return edit_List(ref list, ref inspected, types, ref changed).listLabel_Used();
    }
        
    public static T edit_List<T>(ref List<T> list, ref int inspected, TaggedTypesStd types, ref bool changed, ListMetaData listMeta = null) {

        var added = default(T);

        if (list == null)
        {
            if ("Init list".ClickUnFocus())
                list = new List<T>();
            else
                return added;
        }

        var before = inspected;
        if (inspected >= list.Count)
            inspected = -1;

        changed |= (inspected != before);

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
                        list[i].Name_ClickInspect_PEGI(list, i, ref inspected, listMeta).changes(ref changed);

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

        #region Lambda

        #region SpecialLambdas

        static IList listElementsRoles;

        static Color lambda_Color(Color val)
        {
            edit(ref val);
            return val;
        }

        static Color32 lambda_Color(Color32 val)
        {
            edit(ref val);
            return val;
        }
        
        static int lambda_int(int val)
        {
            edit(ref val);
            return val;
        }

        static string lambda_string_role(string val)
        {

            var role = listElementsRoles.TryGet(InspectedIndex);
            if (role != null)
                role.ToPegiString().edit(90, ref val);
            else edit(ref val);

            return val;
        }

        public static string lambda_string(string val)
        {
            edit(ref val);
            return val;
        }

        private static T lambda_Obj_role<T>(T val) where T : UnityEngine.Object
        {

            var role = listElementsRoles.TryGet(InspectedIndex);
            if (!role.IsNullOrDestroyed_Obj())
                role.ToPegiString().edit(90, ref val);
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

        public static T edit_List<T>(ref List<T> list, ref bool changed, Func<T, T> lambda) where T : new()
        {

            var added = default(T);

            if (listIsNull(ref list))
                return added;

            changed |= list.edit_List_Order();

            if (list != editing_List_Order)
            {
                changed |= list.ListAddNewClick(ref added);

                foreach (var i in list.InspectionIndexes())
                {
                    var el = list[i];
                    var before = el;
                    el = lambda(el);
                    var isNull = el.IsNullOrDestroyed_Obj();
                    if (((!isNull && !el.Equals(before)) || (isNull && !before.IsNullOrDestroyed_Obj())).nl(ref changed))
                        list[i] = el;
                    
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

        public static bool edit_List<T>(ref List<T> list, Func<T, T> lambda) where T : new()
        {
            bool changed = false;
            edit_List(ref list, lambda, ref changed);
            return changed;

        }

        public static T edit_List<T>(ref List<T> list, Func<T, T> lambda, ref bool changed) where T : new()
        {
            var added = default(T);
   
            if (listIsNull(ref list))
                return added;

            changed |= list.edit_List_Order();

            if (list != editing_List_Order)
            {

                changed |= list.ListAddNewClick(ref added);

                foreach (var i in list.InspectionIndexes())
                {
                    var el = list[i];
                    var before = el;
                    el = lambda(el);
                    var isNull = el.IsNullOrDestroyed_Obj();
                    if (((!isNull && !el.Equals(before)) || (isNull && !before.IsNullOrDestroyed_Obj())).nl(ref changed))
                        list[i] = el;
                    
                }
                
            }

            newLine();
            return added;
        }

        public static bool edit_List_UObj<T>(ref List<T> list, Func<T, T> lambda) where T : UnityEngine.Object
        {

            bool changed = false;

            if (listIsNull(ref list))
                return changed;

            changed |= list.edit_List_Order();

            if (list != editing_List_Order)
            {

                changed |= list.ListAddEmptyClick();

                foreach (var i in list.InspectionIndexes())
                {
                    var el = list[i];
                    var before = el;
                    el = lambda(el);
              
                    if (((el && !el.Equals(before)) || (!el && before)).nl(ref changed))
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
                    var before = el;

                    el = lambda(el);

                    if ((!before.SameAs(el)).nl(ref changed))
                        list[i] = el;
                }
                
            }

            newLine();
            return changed;
        }

        #endregion
        
        #region NotNew

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
                        list[i].Name_ClickInspect_PEGI(list, i, ref edited).changes(ref changed);
                    
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
            {
                return ef.edit(ref dic, atKey);
            }
            else
    #endif
            {
                string before = dic[atKey];
                if (editDelayed(ref before, 40))
                {
                    dic[atKey] = before;
                    return false;
                }

                return false;
            }
        }
        
        static bool dicIsNull<G,T>(ref Dictionary<G,T> dic)
        {
            if (dic == null) {
                if ("Instantiate list".ClickUnFocus().nl())
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
                            write(el.ToPegiString(), 120);

                        if ((el is IPEGI) && icon.Enter.ClickUnfocus(Msg.InspectElement, 25))
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
                    changed |= dic.ElementAt(inspected).Value.Try_Nested_Inspect();
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
            bool changed = false;

            if (dicIsNull(ref dic))
                return changed;

            nl();
            for (int i = 0; i < dic.Count; i++) {
                var item = dic.ElementAt(i);
                var itemKey = item.Key;

                InspectedIndex = Convert.ToInt32(itemKey);

                if ((ld == null || ld.allowDelete) && icon.Delete.ClickUnFocus(25).changes(ref changed)) 
                    dic.Remove(itemKey);
                else {
                    if (showKey)
                        itemKey.ToPegiString().write(50);

                    var el = item.Value;
                    var before = el;
                    el = lambda(el);

                    if ((!before.Equals(el)).changes(ref changed))
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

        static string newEnumName = "UNNAMED";
        static int newEnumKey = 1;
        static bool newElement(this Dictionary<int, string> dic)
        {
            bool changed = false;
            newLine();
            "______New [Key, Value]".nl();
            changed |= edit(ref newEnumKey, 50);
            changed |= edit(ref newEnumName);
            string dummy;
            bool isNewIndex = !dic.TryGetValue(newEnumKey, out dummy);
            bool isNewValue = !dic.ContainsValue(newEnumName);

            if ((isNewIndex) && (isNewValue) && (icon.Add.ClickUnFocus("Add Element", 25).changes(ref changed)))
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
                if ("init array".ClickUnFocus().nl())
                    array = new T[0];
            } else {

                ExitOrDrawPEGI(array, ref inspected).changes(ref changed);

                if (inspected != -1) return added;

                if (!typeof(T).IsNew()) {
                    if (icon.Add.ClickUnFocus("Add empty element"))
                        array = array.ExpandBy(1);
                } else if (icon.Create.ClickUnFocus("Add New Instance"))
                    CsharpUtils.AddAndInit(ref array, 1);
                    
                edit_Array_Order(ref array, metaDatas).nl(ref changed);

                if (array == _editingArrayOrder) return added;

                for (var i = 0; i < array.Length; i++) 
                    array[i].Name_ClickInspect_PEGI<T>(null, i, ref inspected, metaDatas).nl(ref changed);
            }

            return added;
        }

        #endregion

        #region Transform

        static bool _editLocalSpace = false;
        public static bool PEGI_CopyPaste(this Transform tf, ref bool editLocalSpace)
        {
            bool changed = false;

            changed |= "Local".toggle(40, ref editLocalSpace);

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
            bool changed = false;

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
        static bool Try_NameInspect(this object obj, string label = "") {
            bool could;
            return obj.Try_NameInspect(out could, label);
        }

        static bool Try_NameInspect(this object obj, out bool couldInspect, string label = "") {

            bool gotLabel = !label.IsNullOrEmpty();

            couldInspect = true;
            var iname = obj as IGotName;
            if (iname != null)
                return iname.inspect_Name(label);

            var uobj = obj.TryGetGameObject_Obj(); 

            if (uobj)
            {
                var n = uobj.name;
                if (gotLabel ? label.editDelayed(80, ref n) : editDelayed(ref n)) {
                    uobj.name = n;
                    uobj.RenameAsset(n);
                }
            }
            else
                couldInspect = false;



            return false;
        }

        public static bool inspect_Name(this IGotName obj) => obj.inspect_Name("", obj.ToPegiString());

        public static bool inspect_Name(this IGotName obj, string label) => obj.inspect_Name(label, label);

        public static bool inspect_Name(this IGotName obj, string label, string hint)
        {
            var n = obj.NameForPEGI;

            bool gotLabel = !label.IsNullOrEmpty();


            if (obj as UnityEngine.Object)
            {

                if ((gotLabel && label.editDelayed(80, ref n) || (!gotLabel && editDelayed(ref n))))
                {
                    obj.NameForPEGI = n;
                    return change;
                }
            } else


            if ((gotLabel && label.edit(80, ref n) || (!gotLabel && edit(ref n))))
            {
                obj.NameForPEGI = n;
                return change;
            }

            return false;
        }
        #endregion

        #region Searching

        public static bool SearchMatch (this IList list, string searchText) => list.Cast<object>().Any(e => e.SearchMatch_Obj(searchText));
        
        public static bool SearchMatch_Obj (this object obj, string searchText) => SearchMatch_Obj_Internal(obj, new string[] { searchText });

        private static bool SearchMatch_Obj_Internal(this object obj, string[] text, int[] indexes = null) {

            if (obj.IsNullOrDestroyed_Obj())
                return false;

            var go = obj.TryGetGameObject_Obj();

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

                if (!indexes.IsNullOrEmpty() && go.TryGet<IGotIndex>().SearchMatch_Internal(indexes))
                    return true;

            } else {

                if ((obj.TryGet_fromObj<IPEGI_Searchable>()).SearchMatch_Internal(text, ref matched))
                    return true;

                if (obj.TryGet_fromObj<IGotName>().SearchMatch_Internal(text, ref matched))
                    return true;

                if (obj.TryGet_fromObj<IGotDisplayName>().SearchMatch_Internal(text, ref matched))
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

        private static bool SearchMatch_Internal(this IGotName gotName, string[] text, ref bool[] matched)
        {
            if (gotName == null) return false;

            var fullMatch = true;

            for (var i = 0; i < text.Length; i++)
                if (!matched[i])  {
                    if (!text[i].IsSubstringOf(gotName.NameForPEGI))
                        fullMatch = false;
                    else
                        matched[i] = true;
                }
            return fullMatch;

        }

        private static bool SearchMatch_Internal(this IGotDisplayName gotDisplayName, string[] text, ref bool[] matched)
        {
            if (gotDisplayName == null) return false;

            var fullMatch = true;

            for (var i = 0; i < text.Length; i++)
                if (!matched[i]) {

                    if (!text[i].IsSubstringOf(gotDisplayName.NameForDisplayPEGI))
                        fullMatch = false;
                    else
                        matched[i] = true;
                }
            return fullMatch;

        }

        private static bool SearchMatch_Internal(this string label, string[] text, ref bool[] matched)
        {
         
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
        
        public class SearchData: AbstractStd, ICanBeDefaultStd {
            public IList filteredList;
            public string searchedText;
            public int uncheckedElement = 0;
            private string[] searchBys;
            public List<int> filteredListElements = new List<int>();

            public void ToggleSearch(IList ld)
            {

                if (ld == null)
                    return;

                var active = ld == filteredList;

                var changed = false;

                if (active && icon.FoldedOut.Click("Hide Search {0}".F(ld), 20).changes(ref changed))
                    active = false;

                if (!active && ld!=editing_List_Order && icon.Search.Click("Search {0}".F(ld), 20).changes(ref changed)) 
                    active = true;
                
                if (!changed) return;

                filteredList = active ? ld : null;

            }
            
            public void SearchString(IList list, out bool searching, out string[] searchBy)
            {
                searching = false;
               
                if (list == filteredList) {

                    nl();
                    if (edit(ref searchedText) || icon.Refresh.Click("Search again", 20).nl()) {
                        filteredListElements.Clear();
                        searchBys = searchedText.Split(splitCharacters, StringSplitOptions.RemoveEmptyEntries);
                        uncheckedElement = 0;
                    }

                    searching = !searchBys.IsNullOrEmpty();
                }

                searchBy = searchBys;
            }

            public override StdEncoder Encode() => new StdEncoder().Add_String("s", searchedText);

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

        #region Shaders
        
        public static bool toggle(this Material mat, string keyword) {
            bool val = Array.IndexOf(mat.shaderKeywords, keyword) != -1;

            if (!keyword.toggleIcon(ref val)) return false;
            
            if (val)
                mat.EnableKeyword(keyword);
            else
                mat.DisableKeyword(keyword);

            return true;
        }

        public static bool edit(this Material mat, ShaderProperty.FloatValue property, string name = null)
        {
            var val = mat.Get(property);

            if (name.IsNullOrEmpty())
                name = property.NameForDisplayPEGI;

            if (name.edit(name.Length * letterSizeInPixels, ref val)) {
                mat.Set(property, val);
                return true;
            }

            return false;
        }

        public static bool edit(this Material mat, ShaderProperty.FloatValue property, string name, float min, float max)
        {
            var val = mat.Get(property);

            if (name.IsNullOrEmpty())
                name = property.NameForDisplayPEGI;

            if (name.edit(name.Length * letterSizeInPixels, ref val, min, max)) {
                mat.Set(property, val);
                return true;
            }

            return false;
        }


        public static bool edit(this Material mat, ShaderProperty.ColorValue property, string name = null)
        {
            var val = mat.Get(property);
            
            if (name.IsNullOrEmpty())
                name = property.NameForDisplayPEGI;

            if (name.edit(name.Length * letterSizeInPixels, ref val))
            {
                mat.Set(property, val);
                return true;
            }

            return false;
        }

        public static bool edit(this Material mat, ShaderProperty.VectorValue property, string name = null)
        {
            var val = mat.Get(property);
            
            if (name.IsNullOrEmpty())
                name = property.NameForDisplayPEGI;

            if (name.edit(ref val))
            {
                mat.Set(property, val);
                return true;
            }

            return false;
        }

        public static bool edit(this Material mat, ShaderProperty.TextureValue property, string name = null)
        {
            var val = mat.Get(property);
            
            if (name.IsNullOrEmpty())
                name = property.NameForDisplayPEGI;

            if (name.edit(name.Length * letterSizeInPixels, ref val))
            {
                mat.Set(property, val);
                return true;
            }

            return false;
        }

        #endregion
  
        #endif

    }

    #region Extensions
    public static class PEGI_Extensions
    {

        static bool ToPegiStringInterfacePart(this object obj, out string name)
        {
            name = null;
            #if PEGI
            var dn = obj as IGotDisplayName;
            if (dn != null) {
                name = dn.NameForDisplayPEGI;
                if (!name.IsNullOrEmpty())
                    return true;
            }


            var sn = obj as IGotName;
            if (sn != null) {
                name = sn.NameForPEGI;
                if (!name.IsNullOrEmpty())
                return true;
            }
            #endif
            return false;
        }
        
        public static string ToPegiStringType(this Type type) => type.ToString().SimplifyTypeName();
           
        public static string ToPegiStringUObj<T>(this T obj) where T: UnityEngine.Object {
            if (obj == null)
                return "NULL UObj {0}".F(typeof(T).ToPegiStringType());

            if (!obj)
                return "Destroyed UObj {0}".F(typeof(T).ToPegiStringType());

            string tmp;
            return (obj.ToPegiStringInterfacePart(out tmp)) ? tmp : obj.name;
        }

        public static string ToPegiString<T>(this T obj) {

            if (obj == null)
                return "NULL {0}".F(typeof(T).ToPegiStringType());

            if (obj.GetType().IsUnityObject())
                return (obj as UnityEngine.Object).ToPegiStringUObj();

            string tmp;
            return (obj.ToPegiStringInterfacePart(out tmp)) ? tmp : obj.ToString().SimplifyTypeName();
        }

        public static string ToPegiString(this object obj) {

            if (obj is string)
                return (string)obj;

            if (obj == null) return "NULL";

            if (obj.GetType().IsUnityObject())
                return (obj as Object).ToPegiStringUObj();

            string tmp;

            return (obj.ToPegiStringInterfacePart(out tmp)) ? tmp : obj.ToString().SimplifyTypeName();
        }

#if PEGI

        public static int focusInd;
        
        public static bool Nested_Inspect(this IPEGI pgi)
        {
            if (pgi.IsNullOrDestroyed_Obj())
                return false;

                var isFOOE = pegi.isFoldedOutOrEntered;

                var changes = pgi.Inspect().RestoreBGColor();

                if (changes || pegi.globChanged)
                    pgi.SetToDirty_Obj();

                pegi.isFoldedOutOrEntered = isFOOE;

                return changes;

        }

        public static bool Inspect_AsInList(this IPEGI_ListInspect obj)
        {
            var tmp = -1;
            var changes = obj.PEGI_inList(null, 0, ref tmp);

            if (pegi.globChanged || changes)
                obj.SetToDirty_Obj();

            return changes;
        }

#if UNITY_EDITOR
        private static readonly Dictionary<Type, Editor> defaultEditors = new Dictionary<Type, Editor>();
#endif

        private static bool TryDefaultInspect(this object obj) {

#if UNITY_EDITOR
            if (pegi.paintingPlayAreaGui) return false;

            var uObj = obj as UnityEngine.Object;

            if (!uObj) return false;

            Editor ed;
            var t = uObj.GetType();
            if (!defaultEditors.TryGetValue(t, out ed))
            {
                ed = Editor.CreateEditor(uObj);
                defaultEditors.Add(t, ed);
            }

            if (ed == null) return false;

            pegi.nl();
            EditorGUI.BeginChangeCheck();
            ed.DrawDefaultInspector();
            return EditorGUI.EndChangeCheck();
#else
            return false;
#endif
        }

        public static bool Try_Nested_Inspect(this GameObject go)
        {
            var changed = false;

            var pgi = go.TryGet<IPEGI>();

            if (pgi != null)
                pgi.Nested_Inspect().RestoreBGColor().changes(ref changed);
            else {
                var mbs = go.GetComponents<Component>();

                foreach (var m in mbs)
                    m.TryDefaultInspect().changes(ref changed);
            }

            if (changed)
                go.SetToDirty();

            return changed;
        }

        public static bool Try_Nested_Inspect(this Component cmp ) => cmp && cmp.gameObject.Try_Nested_Inspect();

        public static bool Try_Nested_Inspect(this object obj) {
            var pgi = obj.TryGet_fromObj<IPEGI>();
            return pgi?.Nested_Inspect() ?? obj.TryDefaultInspect();
        }



        public static bool Try_enter_Inspect(this object obj, ref int enteredOne, int thisOne) {

            var l = obj.TryGet_fromObj<IPEGI_ListInspect>();

            if (l != null)
                return l.enter_Inspect_AsList(ref enteredOne, thisOne);

            var p = obj.TryGet_fromObj<IPEGI>();

            if (p != null)
                return p.enter_Inspect(ref enteredOne, thisOne);

            if (enteredOne == thisOne)
                enteredOne = -1;

            return false;
        }

        public static bool TryInspect<T>(this ListMetaData ld, ref T obj, int ind) where T : UnityEngine.Object
        {
            var el = ld.TryGetElement(ind);

            return el?.PEGI_inList_Obj(ref obj) ?? pegi.edit(ref obj);
        }

        public static int CountForInspector<T>(this List<T> lst) where T : IGotCount  {
            var count = 0;

            foreach (var e in lst)
                if (!e.IsNullOrDestroyed_Obj())
                    count += e.CountForInspector;

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
                    count += cnt.CountForInspector;
            }

            return count;
        }

#endif

        public static T GetByIGotIndex<T>(this List<T> lst, int index) where T : IGotIndex
        {
#if PEGI
            if (lst != null)
                foreach (var el in lst)
                    if (!el.IsNullOrDestroyed_Obj() && el.IndexForPEGI == index)
                        return el;
#endif
            return default(T);
        }

        public static T GetByIGotIndex<T, G>(this List<T> lst, int index) where T : IGotIndex where G : T
        {
#if PEGI
            if (lst != null)
                foreach (var el in lst)
                    if (!el.IsNullOrDestroyed_Obj() && el.IndexForPEGI == index && el.GetType() == typeof(G))
                        return el;
#endif
            return default(T);
        }

        public static T GetByIGotName<T>(this List<T> lst, string name) where T : IGotName
        {

#if PEGI
            if (lst != null)
                foreach (var el in lst)
                    if (!el.IsNullOrDestroyed_Obj() && el.NameForPEGI.SameAs(name))
                        return el;
#endif

            return default(T);
        }

        public static T GetByIGotName<T>(this List<T> lst, T other) where T : IGotName
        {

#if PEGI
            if (lst != null && !other.IsNullOrDestroyed_Obj())
                foreach (var el in lst)
                    if (!el.IsNullOrDestroyed_Obj() && el.NameForPEGI.SameAs(other.NameForPEGI))
                        return el;
#endif

            return default(T);
        }
        
        public static G GetByIGotName<T, G>(this List<T> lst, string name) where T : IGotName where G : class, T
        {
#if PEGI
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
    }
    #endregion

}
#pragma warning restore IDE1006
