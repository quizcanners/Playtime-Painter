
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
    using UnityEditor;
#endif
using System;
using System.Linq;

using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using SharedTools_Stuff;


#pragma warning disable IDE1006
namespace PlayerAndEditorGUI {

    #region interfaces & Attributes

    public interface IPEGI
    {
#if PEGI
        bool Inspect();
#endif
    }

    public interface INeedAttention
    {
#if PEGI
        string NeedAttention();
#endif
    }

    public interface IPEGI_ListInspect
    {
#if PEGI
        bool PEGI_inList(IList list, int ind, ref int edited);
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
        string NameForPEGIdisplay { get; }
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


    #endregion
    
    public static class pegi {

        public static string EnvironmentNL => Environment.NewLine;

        static int mouseOverUI = -1;

        public static bool MouseOverUI {
            get { return mouseOverUI >= Time.frameCount - 1; }
            set { if (value) mouseOverUI = Time.frameCount; }
        }

#if PEGI

        #region Other Stuff
        public delegate bool CallDelegate();

        public class WindowPositionData_PEGI_GUI
        {
            public WindowFunction function;
            public Rect windowRect;

            public void DrawFunction(int windowID) {

                paintingPlayAreaGUI = true;

                try {

                    elementIndex = 0;
                    lineOpen = false;
                    PEGI_Extensions.focusInd = 0;

                    function();

                    nl();

                    "Tip:{0}".F(GUI.tooltip).nl();

                    MouseOverUI = windowRect.Contains(Input.mousePosition);

                    GUI.DragWindow(new Rect(0, 0, 10000, 20));
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError(ex);
                }

                paintingPlayAreaGUI = false;
            }

            public void Render(IPEGI p) => Render(p.Inspect, p.ToPEGIstring());

            public void Render(WindowFunction doWindow, string c_windowName)
            {
                windowRect.x = Mathf.Clamp( windowRect.x, 0, Screen.width - 10);
                windowRect.y = Mathf.Clamp( windowRect.y, 0, Screen.height - 10);
                
                function = doWindow;
                windowRect = GUILayout.Window(0, windowRect, DrawFunction, c_windowName);
            }

            public void Collapse() {
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

        static int elementIndex;

        public static bool isFoldedOut_or_Entered = false;
        public static bool IsFoldedOut => isFoldedOut_or_Entered;
        public static bool IsEntered => isFoldedOut_or_Entered; 
        
        static int selectedFold = -1;
        public static int tabIndex; // will be reset on every NewLine;
        public static bool paintingPlayAreaGUI { get; private set; }

        static bool lineOpen;
        
        static Color attentionColor = new Color(1f, 0.7f, 0.7f, 1);

        #region GUI Colors

        static bool GUIcolorReplaced = false;

        static Color originalGUIcolor;

        static List<Color> previousGUIcolors = new List<Color>();

        public static icon GUIColor(this icon icn, Color col)
        {
            SetGUIColor(col);
            return icn;
        }

        public static void SetGUIColor(this Color col)
        {
            if (!GUIcolorReplaced)
                originalGUIcolor = GUI.color;
            else
                previousGUIcolors.Add(GUI.color);

            GUI.color = col;

            GUIcolorReplaced = true;

        }

        public static void RestoreGUIcolor()
        {
            if (GUIcolorReplaced)
                GUI.color = originalGUIcolor;

            previousGUIcolors.Clear();

            GUIcolorReplaced = false;
        }

        public static bool RestoreGUIColor(this bool val)
        {
            RestoreGUIcolor();
            return val;
        }

        #endregion

        #region BG Color

        static bool BGcolorReplaced = false;

        static Color originalBGcolor;

        static List<Color> previousBGcolors = new List<Color>();

        public static icon BGColor(this icon icn, Color col)
        {
            SetBgColor(col);
            return icn;
        }

        public static string BGColor(this string txt, Color col)
        {
            SetBgColor(col);
            return txt;
        }

        public static bool PreviousBGcolor(this bool val)
        {
            PreviousBGcolor();
            return val;
        }

        public static bool RestoreBGColor(this bool val)
        {
            RestoreBGcolor();
            return val;
        }

        public static void PreviousBGcolor()
        {
            if (BGcolorReplaced)
            {
                if (previousBGcolors.Count > 0)
                    SetBgColor(previousBGcolors.RemoveLast());
                else
                    RestoreBGcolor();
            }
        }

        public static void SetBgColor(this Color col)
        {

            if (!BGcolorReplaced)
                originalBGcolor = GUI.backgroundColor;
            else
                previousBGcolors.Add(GUI.backgroundColor);

            GUI.backgroundColor = col;

            BGcolorReplaced = true;

        }

        public static void RestoreBGcolor()
        {
            if (BGcolorReplaced)
                GUI.backgroundColor = originalBGcolor;

            previousBGcolors.Clear();

            BGcolorReplaced = false;
        }

        #endregion

        public static void checkLine()
        {
            #if UNITY_EDITOR
                if (!paintingPlayAreaGUI)
                    ef.checkLine();
                else
            #endif
            if (!lineOpen) {
                tabIndex = 0;
                GUILayout.BeginHorizontal();
                lineOpen = true;
            }
        }

        public static void DropFocus() => FocusControl("_");

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
                for (int i = 0; i < list.Count; i++)
                {
                    var el = list[i];
                    if (!el.IsNullOrDestroyed())
                    {
                        var need = el as INeedAttention;
                        if (need != null)
                        {
                            var what = need.NeedAttention();
                            if (what != null)
                            {
                                LastNeedAttentionMessage = " {0} on {1}:{2}".F(what, i, need.ToPEGIstring());
                                LastNeedAttentionIndex = i;

                                return LastNeedAttentionMessage;
                            }
                        }
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
            if (!paintingPlayAreaGUI)
            {
                EditorGUI.FocusTextInControl(name);
            }
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
            if (!paintingPlayAreaGUI)
                ef.Space();
            else
            #endif

            {
                checkLine();
                GUILayout.Space(10);
            }
        }

        public static void line() => line(paintingPlayAreaGUI ? Color.white : Color.black);
  
        public static void line(Color col)
        {
            nl();

            GUIStyle horizontalLine;
            horizontalLine = new GUIStyle();

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
            if (!paintingPlayAreaGUI) {
                ef.newLine();
                return;
            }
            #endif

            if (lineOpen) {
                lineOpen = false;
                GUILayout.EndHorizontal();
            }
        }

        public static void nl_ifFolded() => isFoldedOut_or_Entered.nl_ifFalse();

        public static void nl_ifFoldedOut() => isFoldedOut_or_Entered.nl_ifTrue();
        
        public static void nl_ifNotEntered() => isFoldedOut_or_Entered.nl_ifFalse();

        public static void nl_ifEntered() => isFoldedOut_or_Entered.nl_ifTrue();

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

        static bool nl_ifTrue(this bool value)
        {
            if (value)
                newLine();
            return value;
        }

        static bool nl_ifFalse(this bool value)
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

        #region Unity Object
        public static void write<T>(T field) where T : UnityEngine.Object {
#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
                ef.write(field);
#endif
        }
        
        public static void write_obj<T>(T field, int width) where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
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
            if (!paintingPlayAreaGUI)
            {
                ef.write(img, width);
            }
            else
#endif
            {

                SetBgColor(Color.clear);

                img.Click(width);

                RestoreBGcolor();

                /* checkLine();
                 GUIContent c = new GUIContent() { image = img };

                 GUILayout.Button(c, GUILayout.MaxWidth(width + 5), GUILayout.MaxHeight(width));*/
            }

        }

        public static void write(this Texture img, string tip, int width = defaultButtonSize)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                ef.write(img, tip, width);
            }
            else
#endif

            
           {

                SetBgColor(Color.clear);

                img.Click(tip, width, width);

                RestoreBGcolor();

                /* checkLine();
                 GUIContent c = new GUIContent() { image = img, tooltip = tip };
                 GUILayout.Label(c, GUILayout.MaxWidth(width + 5), GUILayout.MaxHeight(width));*/
            }

        }

        public static void write(this Texture img, string tip, int width, int height)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                ef.write(img, tip, width, height);
            }
            else
#endif
            {

                SetBgColor(Color.clear);

                img.Click(tip, width, height);

                RestoreBGcolor();


                /*checkLine();
                GUIContent c = new GUIContent() { image = img, tooltip = tip };
                GUILayout.Label(c, GUILayout.MaxWidth(width + 5), GUILayout.MaxHeight(height));*/
            }

        }

        #endregion

        public static void write(this icon icon, int size = defaultButtonSize) => write(icon.GetIcon(), size);

        public static void write(this icon icon, string tip, int size = defaultButtonSize) => write(icon.GetIcon(), tip, size);

        public static void write(this icon icon, string tip, int width, int height) => write(icon.GetIcon(), tip, width, height);

        public static void write(this string text)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                ef.write(text, text);
            }
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
            if (!paintingPlayAreaGUI)
            {
                ef.write(text, text, style);
            }
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
            if (!paintingPlayAreaGUI)
            {
                ef.write(text, hint, style);
            }
            else
#endif
            {
                checkLine();
                GUIContent cont = new GUIContent() { text = text, tooltip = hint };
                GUILayout.Label(cont, style);
            }
        }

        public static void write(this string text, int width, GUIStyle style)
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                ef.write(text, width, style);
            }
            else
#endif
            {
                checkLine();
                GUIContent cont = new GUIContent() { text = text };
                GUILayout.Label(cont, style, GUILayout.MaxWidth(width));
            }
        }

        public static void write(this string text, string hint, int width, GUIStyle style)
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                ef.write(text, hint, width, style);
            }
            else
#endif
            {
                checkLine();
                GUIContent cont = new GUIContent() { text = text, tooltip = hint };
                GUILayout.Label(cont, style, GUILayout.MaxWidth(width));
            }
        }

        public static void write(this string text, int width) => text.write(text, width);

        public static void write(this string text, string tip)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                ef.write(text, tip);
            }
            else
#endif
            {
                checkLine();
                GUIContent cont = new GUIContent() { text = text, tooltip = tip };
                GUILayout.Label(cont);
            }

        }

        public static void write(this string text, string tip, int width)
        {
            if (width <= 0)
                write(text, tip);

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                ef.write(text, tip, width);
            }
            else
#endif
            {
                checkLine();
                GUIContent cont = new GUIContent() { text = text, tooltip = tip };
                GUILayout.Label(cont, GUILayout.MaxWidth(width));
            }

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
            if (!paintingPlayAreaGUI) {
                ef.writeHint(text, MessageType.Warning);
                ef.newLine();
            }
            else
#endif
            {
                checkLine();
                GUILayout.Label(text);
                newLine();
            }
        }

        public static void writeHint(this string text)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                ef.writeHint(text, MessageType.Info);
                ef.newLine();
            }
            else
#endif
            {
                checkLine();
                GUILayout.Label(text);

                newLine();
            }

        }

        public static void resetOneTimeHint(this string name) => PlayerPrefs.SetInt(name, 0);

        public static bool writeOneTimeHint(this string text, string name) {

            if (PlayerPrefs.GetInt(name) != 0) return false;

            nl();

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI) {
                ef.writeHint(text, MessageType.Info);
            }
            else
#endif
            {
                checkLine();
                GUILayout.Label(text);
            }

            if (icon.Done.ClickUnfocus("Got it").nl()) {
                PlayerPrefs.SetInt(name, 1);
                return true;
            }

            return false;
        }
        #endregion

        #endregion

        #region SELECT

        #region Extended Select

        public static bool select(this string text, int width, ref int value, string[] array)
        {
            write(text, width);
            return select(ref value, array);
        }

        public static bool select<T>(this string text, int width, ref T value, List<T> array, bool showIndex = false)
        {
            write(text, width);
            return select(ref value, array, showIndex);
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
            return select<T>(ref no, tree);
        }

        public static bool select<T>(this string label, ref int no, Countless<T> tree)
        {
            label.write();
            return select<T>(ref no, tree);
        }

        public static bool select<T>(ref int no, Countless<T> tree)
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
                return ef.select(ref no, tree);
#endif
            
            
                List<int> inds;
                List<T> objs = tree.GetAllObjs(out inds);
                List<string> filtered = new List<string>();
                int tmpindex = -1;
                for (int i = 0; i < objs.Count; i++)
                {
                    if (no == inds[i])
                        tmpindex = i;
                    filtered.Add(objs[i].ToPEGIstring());
                }

                if (tmpindex == -1)
                    filtered.Add(">>{0}<<".F(no.ToPEGIstring()));

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
            return select<T>(ref no, tree, lambda);
        }

        public static bool select<T>(this string label, ref int no, Countless<T> tree, Func<T, bool> lambda)
        {
            label.write();
            return select<T>(ref no, tree, lambda);
        }

        public static bool selectOrAdd(ref int selected, ref List<Texture> texes)
        {
            bool change = select(ref selected, texes);

            Texture tex = texes.TryGet(selected);

            if (edit(ref tex))
            {
                change = true;
                if (!tex)
                    selected = -1;
                else
                {
                    int ind = texes.IndexOf(tex);
                    if (ind >= 0)
                        selected = ind;
                    else
                    {
                        selected = texes.Count;
                        texes.Add(tex);
                    }
                }
            }

            return change;
        }

        public static bool select<T>(ref int ind, List<T> lst, int width) {
#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
                return ef.select(ref ind, lst, width);
#endif            
                return select(ref ind, lst);
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
            return selectType<T>(ref el, ed, keepTypeConfig);
        }

        public static bool selectType<T>(this string text, int width, ref T el, ElementData ed = null, bool keepTypeConfig = true) where T : class, IGotClassTag
        {
            text.write(width);
            return selectType<T>(ref el, ed, keepTypeConfig);
        }

        public static bool selectType<T>(this string text, ref T el, ElementData ed = null, bool keepTypeConfig = true) where T : class, IGotClassTag
        {
            text.write();
            return selectType<T>(ref el, ed, keepTypeConfig);
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

            if (typeof(T).TryGetTaggetClasses().Select(ref type).nl()) {
                var previous = obj;
                obj = (T)Activator.CreateInstance(type);
                STDExtensions.TryCopy_Std_AndOtherData(previous, obj);
                return true;
            }

            return false;
        }

        public static bool selectTypeTag(this TaggedTypes_STD types, ref string tag) => select(ref tag, types.Keys);

        public static bool select(ref int no, List<string> from)
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                if (from == null)
                    return "select from null ".edit(90, ref no);
                else
                    return ef.select(ref no, from.ToArray());
            }
            else
#endif

            {
                if (from.IsNullOrEmpty()) return false;

                foldout(from.TryGet(no, "...")); 

                if (isFoldedOut_or_Entered)
                {
                    if (from.Count>1)
                    newLine();
                    for (int i = 0; i < from.Count; i++)
                    {
                        if (i != no)
                            if (ClickUnfocus("{0}: {1}".F(i, from[i]), 100))
                            {
                                no = i;
                                foldIn();
                                return true;
                            }

                        newLine();
                    }
                }

                GUILayout.Space(10);

                return false;
            }
        }

        public static bool select(ref int no, string[] from, int width = -1)
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
                return  width > 0 ? 
                    ef.select(ref no, from, width) : 
                    ef.select(ref no, from);
#endif

                if (from.IsNullOrEmpty())
                return false;

                foldout(from.TryGet(no,"..."));
            
                if (isFoldedOut_or_Entered) {

                    if (from.Length > 1)
                        newLine();

                    for (int i = 0; i < from.Length; i++) {
                        if (i != no)
                            if ("{0}: {1}".F(i, from[i]).ClickUnfocus()) {
                                no = i;
                                foldIn();
                                return true;
                            }

                        newLine();
                    }
                }

                GUILayout.Space(10);

                return false;
            
        }

        public static bool select(ref int no, string[] from, int width, bool showIndex = false)
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
                return ef.select(ref no, from, width);
         
#endif

            
                if (from.IsNullOrEmpty())
                    return false;

                foldout(from.TryGet(no,"..."));
                newLine();

                if (isFoldedOut_or_Entered)
                {
                    for (int i = 0; i < from.Length; i++)
                    {
                        if (i != no)
                            if ((showIndex ? "{0}: {1}".F(i, from[i]) : from[i]).ClickUnfocus(width))
                            {
                                no = i;
                                foldIn();
                                return true;
                            }

                        newLine();
                    }
                }

                GUILayout.Space(10);

                return false;
            
        }

        static bool select_Final(ref int val, ref int jindx, List<string> lnms)
        {
            int count = lnms.Count;

            if (count == 0)
                return edit(ref val);
            else
            {
                if (jindx == -1)
                {
                    lnms.Add("[{0}]".F(val.ToPEGIstring()));
                    jindx = lnms.Count - 1;
                }

                int tmp = jindx;

                if (select(ref tmp, lnms.ToArray()) && (tmp < count))
                {
                    jindx = tmp;
                    return true;
                }

                return false;
            }
        }

        static bool select_Final<T>(T val, ref int jindx, List<string> lnms)
        {
            int count = lnms.Count;

            if (jindx == -1 && !val.IsNullOrDestroyed())
            {
                lnms.Add("[{0}]".F(val.ToPEGIstring()));
                jindx = lnms.Count - 1;
            }

            int tmp = jindx;

            if (select(ref tmp, lnms.ToArray()) && (tmp < count))
            {
                jindx = tmp;
                return true;
            }

            return false;
        }

        static string _compileName<T>(bool showIndex, int index, T obj)
        {
            var st = obj.ToPEGIstring();
            return (showIndex || st.Length == 0) ? "{0}: {1}".F(index, st) : st;
        }

        public static bool select<T>(ref T val, T[] lst, bool showIndex = false)
        {
            checkLine();

            List<string> lnms = new List<string>();
            List<int> indxs = new List<int>();

            int jindx = -1;

            for (int j = 0; j < lst.Length; j++)
            {
                T tmp = lst[j];
                if (!tmp.IsDefaultOrNull())
                {
                    if ((!val.IsDefaultOrNull()) && val.Equals(tmp))
                        jindx = lnms.Count;
                    lnms.Add(_compileName(showIndex, j, tmp)); //showIndex ? "{0}: {1}".F(j, tmp.ToPEGIstring()) : tmp.ToPEGIstring());
                    indxs.Add(j);
                }
            }

            if (select_Final(val, ref jindx, lnms))
            {
                val = lst[indxs[jindx]];
                return true;
            }

            return false;

        }

        public static bool select<T>(ref int val, List<T> lst, Func<T, bool> lambda, bool showIndex = false)
        {

            checkLine();

            List<string> lnms = new List<string>();
            List<int> indxs = new List<int>();

            int jindx = -1;

            for (int j = 0; j < lst.Count; j++)
            {
                T tmp = lst[j];

                if ((!tmp.IsDefaultOrNull()) && lambda(tmp))
                {
                    if (val == j)
                        jindx = lnms.Count;
                    lnms.Add(_compileName(showIndex, j, tmp));//showIndex ? "{0}: {1}".F(j, tmp.ToPEGIstring()) : tmp.ToPEGIstring());
                    indxs.Add(j);
                }
            }


            if (select_Final(val, ref jindx, lnms))
            {
                val = indxs[jindx];
                return true;
            }

            return false;
        }

        public static bool select_IGotIndex<T>(ref int val, List<T> lst, Func<T, bool> lambda, bool showIndex = false) where T : IGotIndex
        {

            checkLine();

            List<string> lnms = new List<string>();
            List<int> indxs = new List<int>();

            int jindx = -1;

            for (int j = 0; j < lst.Count; j++)
            {
                T tmp = lst[j];

                if ((!tmp.IsDefaultOrNull()) && lambda(tmp))
                {
                    int ind = tmp.IndexForPEGI;

                    if (val == ind)
                        jindx = lnms.Count;
                    lnms.Add(_compileName(showIndex, ind, tmp));//showIndex ? "{0}: {1}".F(ind, tmp.ToPEGIstring()) : tmp.ToPEGIstring());
                    indxs.Add(ind);
                }
            }

            if (select_Final(ref val, ref jindx, lnms))
            {
                val = indxs[jindx];
                return true;
            }

            return false;
        }

        public static bool select<T>(ref T val, List<T> lst, Func<T, bool> lambda, bool showIndex = false)
        {
            bool changed = false;


            checkLine();

            var lnms = new List<string>();
            var indxs = new List<int>();

            int jindx = -1;

            for (int j = 0; j < lst.Count; j++)
            {
                var tmp = lst[j];
                if (!tmp.IsDefaultOrNull() && lambda(tmp))
                {
                    if ((jindx == -1) && tmp.Equals(val))
                        jindx = lnms.Count;

                    lnms.Add(_compileName(showIndex, j, tmp)); 
                    indxs.Add(j);
                }
            }

            if (select_Final(val, ref jindx, lnms).changes(ref changed))
                val = lst[indxs[jindx]];
            

            return changed;

        }

        public static bool select<T>(ref T val, List<T> lst, bool showIndex = false)
        {
            checkLine();

            List<string> lnms = new List<string>();
            List<int> indxs = new List<int>();

            int jindx = -1;

            for (int j = 0; j < lst.Count; j++)
            {
                T tmp = lst[j];
                if (!tmp.IsDefaultOrNull())
                {
                    if ((!val.IsDefaultOrNull()) && tmp.Equals(val))
                        jindx = lnms.Count;
                    lnms.Add(_compileName(showIndex, j, tmp)); 
                    indxs.Add(j);
                }
            }

            if (select_Final(val, ref jindx, lnms))
            {
                val = lst[indxs[jindx]];
                return true;
            }

            return false;

        }

        public static bool select<G,T>(ref T val, Dictionary<G, T> dic, bool showIndex = false) => select(ref val, new List<T>(dic.Values), showIndex);

        public static bool select(ref Type val, List<Type> lst, string textForCurrent, bool showIndex = false)
        {
            checkLine();

            List<string> lnms = new List<string>();
            List<int> indxs = new List<int>();

            int jindx = -1;

            for (int j = 0; j < lst.Count; j++)
            {
                Type tmp = lst[j];
                if (!tmp.IsDefaultOrNull())
                {
                    if ((!val.IsDefaultOrNull()) && tmp.Equals(val))
                        jindx = lnms.Count;
                    lnms.Add(_compileName(showIndex, j, tmp)); //"{0}: {1}".F(j, tmp.ToPEGIstring()));
                    indxs.Add(j);
                }
            }

            if (jindx == -1 && val != null)
                lnms.Add(textForCurrent);

            if (select(ref jindx, lnms.ToArray()) && (jindx < indxs.Count))
            {
                val = lst[indxs[jindx]];
                return true;
            }

            return false;

        }

        public static bool select_SameClass<T, G>(ref T val, List<G> lst, bool showIndex = false) where T : class where G : class
        {
            bool changed = false;
            bool same = typeof(T) == typeof(G);

            checkLine();

            List<string> lnms = new List<string>();
            List<int> indxs = new List<int>();

            int jindx = -1;

            for (int j = 0; j < lst.Count; j++)
            {
                G tmp = lst[j];
                if (!tmp.IsDefaultOrNull() && (same || typeof(T).IsAssignableFrom(tmp.GetType())))
                {
                    if (tmp.Equals(val))
                        jindx = lnms.Count;
                    lnms.Add(_compileName(showIndex, j, tmp)); //"{0}: {1}".F(j, tmp.ToPEGIstring()));
                    indxs.Add(j);
                }
            }

            if (select_Final(val, ref jindx, lnms).changes(ref changed))
                val = lst[indxs[jindx]] as T;
             
            return changed;

        }

        public static bool select(ref string val, List<string> lst)
        {
            var ind = -1;

            for (int i = 0; i < lst.Count; i++)
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

        public static bool select<T>(ref int ind, List<T> lst, bool showIndex = false)
        {

            checkLine();

            List<string> lnms = new List<string>();
            List<int> indxs = new List<int>();

            int jindx = -1;

            for (int j = 0; j < lst.Count; j++)
                if (!lst[j].IsNullOrDestroyed())
                {
                    if (ind == j)
                        jindx = indxs.Count;
                    lnms.Add(_compileName(showIndex, j, lst[j])); //lst[j].ToPEGIstring());
                    indxs.Add(j);
                }
            
            if (select_Final(ind, ref jindx, lnms))
            {
                ind = indxs[jindx];
                return true;
            }

            return false;

        }
      
        public static bool select<T>(ref int no, CountlessSTD<T> tree) where T : ISTD, new()
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                return ef.select(ref no, tree);
            }
            else
#endif
            {
                List<int> inds;
                List<T> objs = tree.GetAllObjs(out inds);
                List<string> filtered = new List<string>();
                int tmpindex = -1;
                for (int i = 0; i < objs.Count; i++)
                {
                    if (no == inds[i])
                        tmpindex = i;
                    filtered.Add(objs[i].ToPEGIstring());
                }

                if (select(ref tmpindex, filtered.ToArray()))
                {
                    no = inds[tmpindex];
                    return true;
                }
                return false;
            }
        }

        public static bool select<T>(ref int no, Countless<T> tree, Func<T, bool> lambda)
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                return ef.select(ref no, tree);
            }
            else
#endif
            {
                List<int> unfinds;
                List<int> inds = new List<int>();
                List<T> objs = tree.GetAllObjs(out unfinds);
                List<string> lnms = new List<string>();
                int jindx = -1;
                int j = 0;
                for (int i = 0; i < objs.Count; i++)
                {

                    var el = objs[i];

                    if (!el.IsNullOrDestroyed() && lambda(el))
                    {
                        inds.Add(unfinds[i]);
                        if (no == inds[j])
                            jindx = j;
                        lnms.Add(objs[i].ToPEGIstring());
                        j++;
                    }
                }


                if (select_Final(no, ref jindx, lnms))
                {
                    no = inds[jindx];
                    return true;
                }
                return false;
            }
        }

        public static bool selectEnum<T>(ref int current) => selectEnum(ref current, typeof(T));
        
        public static bool selectEnum(ref int current, Type type, int width = -1)
        {
                checkLine();
                int tmpVal = -1;

                string[] names = Enum.GetNames(type);
                int[] val = (int[])Enum.GetValues(type);

                for (int i = 0; i < val.Length; i++)
                    if (val[i] == current)
                        tmpVal = i;

                if (select(ref tmpVal, names, width)) {
                    current = val[tmpVal];
                    return true;
                }

                return false;
        }

        public static bool select<T>(ref int ind, T[] arr, bool showIndex = false)
        {

            checkLine();

            var lnms = new List<string>();
            
            if (arr.ClampIndexToLength(ref ind)) {
                for (int i = 0; i < arr.Length; i++)
                    lnms.Add(_compileName(showIndex, i, arr[i])); 
            }

            return select_Final(ind, ref ind, lnms);

        }

        public static bool select(ref int current, Dictionary<int, string> from)
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
                return ef.select(ref current, from);
          
#endif
            
                string[] options = new string[from.Count];

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
                    return true;
                }
                return false;
            
        }

        public static bool select(ref int current, Dictionary<int, string> from, int width)
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
                return ef.select(ref current, from, width);
#endif

                string[] options = new string[from.Count];

                int ind = current;

                for (int i = 0; i < from.Count; i++)
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

        public static bool select(CountlessInt cint, int ind, Dictionary<int, string> from)
        {

            int value = cint[ind];

            if (select(ref value, from))  {
                cint[ind] = value;
                return true;
            }
            return false;
        }

        public static bool select(ref int no, Texture[] tex)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
                return ef.select(ref no, tex);
           
#endif

                if (tex.Length == 0) return false;

                checkLine();

                List<string> tnames = new List<string>();
                List<int> tnumbers = new List<int>();

                int curno = 0;
                for (int i = 0; i < tex.Length; i++)
                    if (!tex[i].IsNullOrDestroyed())
                    {
                        tnumbers.Add(i);
                        tnames.Add("{0}: {1}".F(i, tex[i].name));
                        if (no == i) curno = tnames.Count - 1;
                    }

                bool changed = select(ref curno, tnames.ToArray());

                if (changed)
                {
                    if ((curno >= 0) && (curno < tnames.Count))
                        no = tnumbers[curno];
                }

                return changed;
            
        }

        // ***************************** Select or edit

        public static bool select_or_edit_ColorProperty(this string name, int width, ref string property, Material material)
        {
            name.write(width);
            return select_or_edit_TextureProperty(ref property, material);
        }

        public static bool select_or_edit_ColorProperty(ref string property, Material material)
        {
            var lst = material.GetColorProperties();

            if (lst.Count == 0)
                return edit(ref property);
            else
                return select(ref property, lst);

        }

        public static bool select_or_edit_TextureProperty(this string name, int width, ref string property, Material material)
        {
            name.write(width);
            return select_or_edit_TextureProperty(ref property, material);
        }

        public static bool select_or_edit_TextureProperty(ref string property, Material material)
        {
            var lst = material.MyGetTextureProperties();

            if (lst.Count == 0)
                return edit(ref property);
            else
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
            else
            {
                bool changed = false;
                if (obj && icon.Delete.ClickUnfocus().changes(ref changed))
                    obj = null;
                

                if (text != null)
                    write(text, hint, width);

                changed |= select(ref obj, list, showIndex);

                obj.ClickHighlight();

                return changed;
            }
        }

        public static bool select_or_edit<T>(this string name, ref T obj, List<T> list, bool showIndex = false) where T : UnityEngine.Object
        {
            return select_or_edit(name, null, 0, ref obj, list, showIndex);
        }

        public static bool select_or_edit<T>(this string name, int width, ref T obj, List<T> list, bool showIndex = false) where T : UnityEngine.Object
        => select_or_edit(name, null, width, ref obj, list, showIndex);

        public static bool select_or_edit<T>(ref T obj, List<T> list, bool showIndex = false) where T : UnityEngine.Object
            => select_or_edit(null, null, 0, ref obj, list, showIndex);

        public static bool select_or_edit<T>(this string name, ref int val, List<T> list, bool showIndex = false)
        {
            if (list.IsNullOrEmpty())
                return name.edit(ref val);
            else
                return name.select(ref val, list, showIndex);
        }

        public static bool select_or_edit(ref string val, List<string> list, bool showIndex = false)
        {
            bool changed = false;

            bool gotList = !list.IsNullOrEmpty();

            bool gotValue = !val.IsNullOrEmpty();

            if (gotList && gotValue && icon.Delete.ClickUnfocus())
                val = "";

            if (!gotValue || !gotList)
                changed |= edit(ref val);

            if (gotList)
                changed |= select(ref val, list, showIndex);

            return changed;
        }

        public static bool select_or_edit(this string name, ref string val, List<string> list, bool showIndex = false)
        {
            bool changed = false;

            bool gotList = !list.IsNullOrEmpty();

            bool gotValue = !val.IsNullOrEmpty();

            if (gotList && gotValue && icon.Delete.ClickUnfocus())
                val = "";

            if (!gotValue || !gotList)
                changed |= name.edit(ref val);

            if (gotList)
                changed |= name.select(ref val, list, showIndex);

            return changed;
        }

        public static bool select_or_edit<T>(this string name, int width, ref int val, List<T> list, bool showIndex = false)
        {
            if (list.IsNullOrEmpty())
                return name.edit(width, ref val);
            else
                return name.select(width, ref val, list, showIndex);
        }

        public static bool select_or_edit<T>(this string name, string hint, int width, ref int val, List<T> list, bool showIndex = false)
        {
            if (list.IsNullOrEmpty())
                return name.edit(hint, width, ref val);
            else
                return name.select(hint, width, ref val, list, showIndex);
        }


        public static bool select_SameClass_or_edit<T, G>(this string text, string hint, int width, ref T obj, List<G> list) where T : UnityEngine.Object where G : class
        {
            if (list.IsNullOrEmpty())
                return edit(ref obj);
            else
            {
                bool changed = false;
                if (obj && icon.Delete.ClickUnfocus().changes(ref changed))
                    obj = null;
                
                if (text != null)
                    write(text, hint, width);

                changed |= select_SameClass(ref obj, list);
                return changed;
            }
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

            List<string> lnms = new List<string>();
            List<int> indxs = new List<int>();

            int jindx = -1;

            foreach (var el in lst)
                if (!el.IsNullOrDestroyed())
                {
                    int index = el.IndexForPEGI;

                    if (ind == index)
                        jindx = indxs.Count;
                    lnms.Add((showIndex ? index + ": " : "") + el.ToPEGIstring());
                    indxs.Add(index);

                }
            
            if (select_Final(ind, ref jindx, lnms))
            {
                ind = indxs[jindx];
                return true;
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

            List<string> lnms = new List<string>();

            int jindx = -1;


            for (int i = 0; i < lst.Count; i++)
            {
                var el = lst[i];
                if (!el.IsNullOrDestroyed())
                {
                    var name = el.NameForPEGI;

                    if (name != null)
                    {
                        if (val != null && val.SameAs(name))
                            jindx = lnms.Count;
                        lnms.Add(name);
                    }
                }
            }

            if (select_Final(val, ref jindx, lnms))
            {
                val = lnms[jindx];
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
            
            List<string> lnms = new List<string>();
            List<int> indxs = new List<int>();
            List<G> els = new List<G>();
            int jindx = -1;

            foreach (var el in lst)
            {
                var g = el as G;

                if (!g.IsNullOrDestroyed())
                {
                    int index = g.IndexForPEGI;

                    if (ind == index)
                    {
                        jindx = indxs.Count;
                        val = g;
                    }
                    lnms.Add(el.ToPEGIstring());
                    indxs.Add(index);
                    els.Add(g);
                }
            }

            if (lnms.Count == 0)
                return edit(ref ind);
            else
            if (select_Final(ref ind, ref jindx, lnms))
            {
                ind = indxs[jindx];
                val = els[jindx];
                return true;
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

            return isFoldedOut_or_Entered;

        }

        public static bool foldout(this string txt, ref int selected, int current, ref bool changed)
        {
            var before = selected;

            txt.foldout(ref selected, current);

            changed |= before != selected;

            return isFoldedOut_or_Entered;

        }

        public static bool foldout(this string txt, ref bool state)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                return ef.foldout(txt, ref state);
            }
            else
#endif

            {

                checkLine();

                if (ClickUnfocus((state ? "..⏵ " : "..⏷ ") + txt))
                    state = !state;


                isFoldedOut_or_Entered = state;

                return isFoldedOut_or_Entered;
            }
        }

        public static bool foldout(this string txt, ref int selected, int current)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                return ef.foldout(txt, ref selected, current);
            }
            else
#endif
            {

                checkLine();

                isFoldedOut_or_Entered = (selected == current);

                if (ClickUnfocus((isFoldedOut_or_Entered ? "..⏵ " : "..⏷ ") + txt))
                {
                    if (isFoldedOut_or_Entered)
                        selected = -1;
                    else
                        selected = current;
                }

                isFoldedOut_or_Entered = selected == current;

                return isFoldedOut_or_Entered;
            }
        }

        public static bool foldout(this Texture2D tex, string text, ref int selected, int current, ref bool changed)
        {
            var before = selected;

            tex.foldout(text, ref selected, current);

            changed |= before != selected;

            return isFoldedOut_or_Entered;

        }

        public static bool foldout(this Texture2D tex, string text, ref bool state, ref bool changed)
        {
            var before = state;

            tex.foldout(text, ref state);

            changed |= before != state;

            return isFoldedOut_or_Entered;

        }

        public static bool foldout(this Texture2D tex, string text, ref int selected, int current)
        {

            if (selected == current)
            {
                if (icon.FoldedOut.ClickUnfocus(text, 30))
                    selected = -1;
            }
            else
            {
                if (tex.ClickUnfocus(text, 25))
                    selected = current;
            }
            return selected == current;
        }

        public static bool foldout(this Texture2D tex, ref bool state)
        {

            if (state)
            {
                if (icon.FoldedOut.ClickUnfocus("Fold In", 30))
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
                if (icon.FoldedOut.ClickUnfocus(text, 30))
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
            if (!paintingPlayAreaGUI)
            {
                return ef.foldout(txt);
            }
            else
#endif

            {

                foldout(txt, ref selectedFold, elementIndex);

                elementIndex++;

                return isFoldedOut_or_Entered;
            }

        }

        public static void foldIn() => selectedFold = -1;
        #endregion

        #region Tabs
        public static int tab(ref int selected, params icon[] icons) {
            nl();

            if (selected != -1) {
                if (icon.Close.ClickUnfocus())
                    selected = -1;
            } else
                icon.Next.write();
            

            for (int i=0; i<icons.Length; i++) {
                if (selected == i)
                    icons[i].write();
                else
                {
                    if (icons[i].ClickUnfocus())
                        selected = i;
                }
            }

            nl();
            return selected;
        }
        #endregion

        #region Enter & Exit
        public static bool enter(ref int enteredOne, int current)
        {

            if (enteredOne == current)
            {
                if (icon.Exit.ClickUnfocus())
                    enteredOne = -1;
            }
            else if (enteredOne == -1 && icon.Enter.ClickUnfocus())
                enteredOne = current;

            isFoldedOut_or_Entered = (enteredOne == current);

            return isFoldedOut_or_Entered;
        }

        public static bool enter(this icon ico, ref int enteredOne, int thisOne)
        {
            bool outside = enteredOne == -1;

            if (enteredOne == thisOne) {
                if (icon.Exit.ClickUnfocus())
                    enteredOne = -1;

            }
            else if (outside)  {
                if (ico.ClickUnfocus())
                    enteredOne = thisOne;
            }

            isFoldedOut_or_Entered = (enteredOne == thisOne);

            return isFoldedOut_or_Entered;
        }
        
        public static bool enter(this icon ico, ref bool state)
        {

            if (state)
            {
                if (icon.Exit.ClickUnfocus())
                    state = false;
            }
            else if (!state)
            {
                if (ico.ClickUnfocus())
                    state = true;
            }

            isFoldedOut_or_Entered = state;

            return isFoldedOut_or_Entered;
        }

        public static bool enter(this icon ico, string txt, ref bool state, bool showLabelIfTrue = false) {

            if (state)  {
                if (icon.Exit.ClickUnfocus("Exit {0}".F(txt)))
                    state = false;
            }
            else if (!state)
            {
                if (ico.ClickUnfocus("Enter {0}".F(txt)))
                    state = true;
            }

            if ((showLabelIfTrue || !state) &&
                txt.ClickLabel(txt, state ? PEGI_Styles.ExitLabel : PEGI_Styles.EnterLabel))
                state = !state;

            isFoldedOut_or_Entered = state;

            return isFoldedOut_or_Entered;
        }

        public static bool enter(this icon ico, string txt, ref int enteredOne, int thisOne, bool showLabelIfTrue = false, GUIStyle enterLabelStyle = null)
        {
            bool outside = enteredOne == -1;

            if (enteredOne == thisOne)
            {
                if (icon.Exit.ClickUnfocus("Exit {0}".F(txt)))
                    enteredOne = -1;

            }
            else if (outside)
            {
                if (ico.ClickUnfocus(txt))
                    enteredOne = thisOne;
            }

            if ((showLabelIfTrue || outside) &&
                txt.ClickLabel(txt, outside ? (enterLabelStyle == null ? PEGI_Styles.EnterLabel : enterLabelStyle) : PEGI_Styles.ExitLabel)) 
                enteredOne = outside ? thisOne : -1;
            

            isFoldedOut_or_Entered = (enteredOne == thisOne);

            return isFoldedOut_or_Entered;
        }

        static bool enter_SkipToOnlyElement<T>(this List<T> list, ref int inspected)
        {
            int tmp;
            icon ico;
            string msg;

            if (list.NeedsAttention()) {
                tmp = LastNeedAttentionIndex;
                ico = icon.Warning;
                msg = LastNeedAttentionMessage;
            }
            else {
                tmp = Mathf.Max(0, inspected);
                ico = icon.Next;
                msg = "Inspect element {0}".F(tmp);
            }

            var el = list.TryGet(tmp) as IPEGI;

            if (el != null && ico.Click(msg)) {
                inspected = tmp;
                isFoldedOut_or_Entered = true;
                return true;
            }
            return false;
        }

        static bool enter_SkipToOnlyElement<T>(this List<T> list, ref int inspected, ref int enteredOne, int thisOne) {
            
            if (enteredOne == -1 && list.enter_SkipToOnlyElement(ref inspected)) 
                        enteredOne = thisOne;

            return enteredOne == thisOne;
        }

        static bool enter_SkipToOnlyElement<T>(this List<T> list, ref int inspected, ref bool entered) {

            if (!entered && list.enter_SkipToOnlyElement(ref inspected)) 
                entered = true;

            return entered;
        }

        static bool enter_HeaderPart<T>(this List_Data meta, ref List<T> list, ref int enteredOne, int thisOne, bool showLabelIfTrue = false) {

            if (listIsNull(ref list)) {
                if (enteredOne == thisOne)
                    enteredOne = -1;
                return false;
            }

            var ret = meta.Icon.enter(meta.label.AddCount(list, enteredOne == thisOne), ref enteredOne, thisOne, showLabelIfTrue, list.Count == 0 ? PEGI_Styles.WrappingText : null);
            
            ret |= list.enter_SkipToOnlyElement<T>(ref meta.inspected, ref enteredOne, thisOne);
            
            return ret;
        }

        public static bool enter(this string txt, ref bool state) => icon.Enter.enter(txt, ref state);

        public static bool enter(this string txt, ref int enteredOne, int thisOne) => icon.Enter.enter(txt, ref enteredOne, thisOne);

        public static bool enter(this string txt, ref int enteredOne, int thisOne, IList forAddCount) =>
            icon.Enter.enter(txt.AddCount(forAddCount), ref enteredOne, thisOne, enterLabelStyle: forAddCount.IsNullOrEmpty() ? PEGI_Styles.WrappingText : PEGI_Styles.EnterLabel);

        public static bool enter(this string txt, ref int enteredOne, int thisOne, IGotCount forAddCount) =>
            icon.Enter.enter(txt.AddCount(forAddCount), ref enteredOne, thisOne, enterLabelStyle: forAddCount.IsNullOrDestroyed() ? PEGI_Styles.EnterLabel :
                (forAddCount.CountForInspector > 0 ? PEGI_Styles.EnterLabel : PEGI_Styles.WrappingText));
                
        static bool enter_ListIcon<T>(this string txt, ref List<T> list, ref int enteredOne, int thisOne)
        {
            if (listIsNull(ref list)) {
                if (enteredOne == thisOne)
                    enteredOne = -1;
                return false;
            }

            var ret = icon.List.enter(txt.AddCount(list, enteredOne == thisOne), ref enteredOne, thisOne, false, list.Count == 0 ? PEGI_Styles.WrappingText : null);
            return ret;
        }

        static bool enter_ListIcon<T>(this string txt, ref List<T> list,  ref int inspected, ref int enteredOne, int thisOne)
        {
            if (listIsNull(ref list)) {
                if (enteredOne == thisOne)
                    enteredOne = -1;
                return false;
            }

            var ret = (inspected == -1 ? icon.List : icon.Next).enter(txt.AddCount(list, enteredOne == thisOne), ref enteredOne, thisOne, false, list.Count == 0 ? PEGI_Styles.WrappingText : null);
            ret |= list.enter_SkipToOnlyElement<T>(ref inspected, ref enteredOne, thisOne);
            return ret;
        }

        static bool enter_ListIcon<T>(this string txt, ref List<T> list, ref int inspected, ref bool entered)
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

        static string TryAddCount(this string txt, object obj) {
            var c = obj as IGotCount;
            if (!c.IsNullOrDestroyed())
                txt += " [{0}]".F(c.CountForInspector);

            return txt;
        }

        public static string AddCount(this string txt, IGotCount obj)
        {
            bool isNull = obj.IsNullOrDestroyed();

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

                if (!el.IsNullOrDestroyed())
                {

                    var nm = el as IGotDisplayName;

                    if (nm != null)
                        return "{0}: {1}".F(txt, nm.NameForPEGIdisplay);

                    var n = el as IGotName;

                    if (n != null)
                        return "{0}: {1}".F(txt, n.NameForPEGI);

                    return "{0}: {1}".F(txt, el.ToPEGIstring());

                }
                else return "{0} one Null Element".F(txt);
            }
            else return "{0} [1]".F(txt);
        }
        public static bool enter_Inspect(this icon ico, string txt, IPEGI var, ref int enteredOne, int thisOne, bool showLabelIfTrue = false)
        {
            if (ico.enter(txt.TryAddCount(var), ref enteredOne, thisOne, showLabelIfTrue).nl_ifNotEntered()) 
                var.Try_NameInspect();
            if (isFoldedOut_or_Entered)
                return var.Nested_Inspect();
            

            return false;
        }
        
        public static bool enter_Inspect(this IPEGI var, ref int enteredOne, int thisOne) {

            var lst = var as IPEGI_ListInspect;
            if (lst != null)
                return lst.enter_Inspect_AsList(ref enteredOne, thisOne);
            else
                return var.ToPEGIstring().enter_Inspect(var, ref enteredOne, thisOne);
        }

        public static bool enter_Inspect(this string txt, IPEGI var, ref int enteredOne, int thisOne) {

            if (txt.TryAddCount(var).enter(ref enteredOne, thisOne))
                var.Try_NameInspect();
            if (isFoldedOut_or_Entered)
                return var.Nested_Inspect();
 
            return false;
        }

        public static bool enter_Inspect(this string label, int width, IPEGI_ListInspect var, ref int enteredOne, int thisOne)
        {
            if (enteredOne == -1)
                label.TryAddCount(var).write(width);
            return var.enter_Inspect_AsList(ref enteredOne, thisOne);
        }

        public static bool enter_Inspect_AsList(this IPEGI_ListInspect var, ref int enteredOne, int thisOne)
        {
            bool changed = false;

            bool outside = enteredOne == -1;

            if (!var.IsNullOrDestroyed()) {

                if (outside)
                    changed |= var.PEGI_inList(null, thisOne, ref enteredOne);
                else if (enteredOne == thisOne) {
                    if (icon.Exit.ClickUnfocus("Exit {0}".F(var)))
                        enteredOne = -1;
                    changed |= var.Try_Nested_Inspect();
                }
            }
            else if (enteredOne == thisOne)
                    enteredOne = -1;
            

            isFoldedOut_or_Entered = enteredOne == thisOne;

            return changed;
        }

        public static bool TryEnter_Inspect(this string label, object obj, ref int enteredOne, int thisOne) {
            bool changed = false;

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
                isFoldedOut_or_Entered = false;

            return isFoldedOut_or_Entered;
        }

        public static bool conditional_enter(this icon ico, string label, bool canEnter, ref int enteredOne, int thisOne)
        {

            if (!canEnter && enteredOne == thisOne)
                enteredOne = -1;

            if (canEnter)
                ico.enter(label, ref enteredOne, thisOne);
            else
                isFoldedOut_or_Entered = false;

            return isFoldedOut_or_Entered;
        }

        public static bool conditional_enter(this string label, bool canEnter, ref int enteredOne, int thisOne) {

            if (!canEnter && enteredOne == thisOne)
                enteredOne = -1;

            if (canEnter)
                label.enter(ref enteredOne, thisOne);
            else
                isFoldedOut_or_Entered = false;

            return isFoldedOut_or_Entered;
        }

        public static bool conditional_enter_inspect(this IPEGI_ListInspect obj, bool canEnter, ref int enteredOne, int thisOne) {
            if (!canEnter && enteredOne == thisOne)
                enteredOne = -1;

            if (canEnter)
                return obj.enter_Inspect_AsList(ref enteredOne, thisOne); 
            else
                isFoldedOut_or_Entered = false;

            return false;
        }

        public static bool conditional_enter_inspect(this string label, bool canEnter, IPEGI obj, ref int enteredOne, int thisOne) {
            if (label.TryAddCount(obj).conditional_enter(canEnter, ref enteredOne, thisOne))
                return obj.Nested_Inspect();

            isFoldedOut_or_Entered = enteredOne == thisOne;

            return false;
        }

        public static bool toggle_enter(this string label, ref bool val, ref int enteredOne, int thisOne, ref bool changed, bool showLabelWhenEntered = false)
        {

            if (enteredOne == -1)
                changed |= label.toggleIcon(ref val);
            
            if (val)
                enter(ref enteredOne, thisOne);
            else
                isFoldedOut_or_Entered = false;

            if (enteredOne == thisOne)
                changed |= label.toggleIcon(ref val);

            if (!val && enteredOne == thisOne)
                enteredOne = -1;

            return isFoldedOut_or_Entered;
        }
        
        public static bool enter_List_UObj<T>(this string label, ref List<T> list, ref int inspectedElement, ref int enteredOne, int thisOne, List<T> selectFrom = null) where T : UnityEngine.Object
        {

            bool changed = false;
            
            if (enter_ListIcon( label, ref list ,ref inspectedElement, ref enteredOne, thisOne))
                label.edit_List_UObj(ref list, ref inspectedElement, selectFrom).nl();

            return changed;
        }

        public static bool enter_List_UObj<T>(this List_Data meta, ref List<T> list, ref int enteredOne, int thisOne, List<T> selectFrom = null) where T : UnityEngine.Object {

            bool changed = false;
            
            if (meta.enter_HeaderPart(ref list, ref enteredOne, thisOne))  
                meta.edit_List_UObj(ref list, selectFrom);

            return changed;
        }

        public static bool enter_List<T>(this List_Data meta, ref List<T> list, ref int enteredOne, int thisOne)
        {
            bool changed = false;

            if (meta.enter_HeaderPart(ref list, ref enteredOne, thisOne)) 
                changed |= meta.edit_List(ref list);

            return changed;
        }

        public static bool enter_List_UObj<T>(this string label, ref List<T> list, ref int enteredOne, int thisOne, List<T> selectFrom = null) where T : UnityEngine.Object
        {

            bool changed = false;
            
            int insp = -1;
            if (enter_ListIcon(label, ref list ,ref insp, ref enteredOne, thisOne)) // if (label.AddCount(list).enter(ref enteredOne, thisOne))
                label.edit_List_UObj(ref list, selectFrom);   

            return changed;
        }
        
        public static bool enter_List<T>(this string label, ref List<T> list, ref int inspectedElement, ref int enteredOne, int thisOne) 
        {
            bool changed = false;
            label.enter_List(ref list, ref inspectedElement, ref enteredOne, thisOne, ref changed);
            return changed;
        }

        public static bool enter_List<T>(this string label, ref List<T> list, ref int enteredOne, int thisOne, Func<T, T> lambda) where T : new()
        {
            bool changed = false;

            if (enter_ListIcon(label, ref list, ref enteredOne, thisOne))
                label.edit_List(ref list, lambda);

            return changed;
        }

        public static bool enter_List<T>(this List_Data meta, ref List<T> list, ref int enteredOne, int thisOne, Func<T, T> lambda) where T : new()
        {
            bool changed = false;

            if (meta.enter_HeaderPart(ref list, ref enteredOne, thisOne))
                changed |= meta.label.edit_List(ref list, lambda);

            return changed;
        }

        public static T enter_List<T>(this string label, ref List<T> list, ref int inspectedElement, ref int enteredOne, int thisOne, ref bool changed) 
        {
            T tmp = default(T);
            
            if (enter_ListIcon(label, ref list ,ref inspectedElement, ref enteredOne, thisOne)) //if (label.AddCount(list).enter(ref enteredOne, thisOne))
                tmp = label.edit_List(ref list, ref inspectedElement, ref changed);

            return tmp;
        }
        
        public static bool enter_List<T>(this string label, ref List<T> list, ref int inspectedElement, ref bool entered) 
        {

            bool changed = false;
            
            if (enter_ListIcon(label, ref list,ref inspectedElement, ref entered))// if (label.AddCount(list).enter(ref entered))
                label.edit_List(ref list, ref inspectedElement).nl();

            return changed;
        }


        #region Tagged Types

        public static T enter_List<T>(this List_Data meta, ref List<T> list, ref int enteredOne, int thisOne, TaggedTypes_STD types, ref bool changed) {
            if (meta.enter_HeaderPart(ref list, ref enteredOne, thisOne))
               return meta.edit_List(ref list, types, ref changed);
            return default(T);
        }

        public static bool enter_List<T>(this string label, ref List<T> list, ref int enteredOne, int thisOne, TaggedTypes_STD types) {

            bool changed = false;
            int insp = -1;

             if (enter_ListIcon(label, ref list,ref insp, ref enteredOne, thisOne)) 
                label.edit_List(ref list, types, ref changed);
            

            return changed;
        }
       
        #endregion


        public static bool conditional_enter_List<T>(this string label, bool canEnter, ref List<T> list, ref int inspectedElement, ref int enteredOne, int thisOne) 
        {

            bool changed = false;

            if (!canEnter && enteredOne == thisOne)
                enteredOne = -1;

            if (canEnter)
                changed |= label.enter_List(ref list, ref inspectedElement, ref enteredOne, thisOne);
            else
                isFoldedOut_or_Entered = false;

            return changed;

        }

        public static bool conditional_enter_List<T>(this List_Data meta, bool canEnter, ref List<T> list, ref int enteredOne, int thisOne) {

            bool changed = false;

            if (!canEnter && enteredOne == thisOne)
                enteredOne = -1;

            if (canEnter)
                changed |= meta.enter_List(ref list, ref enteredOne, thisOne);
            else
                isFoldedOut_or_Entered = false;

            return changed;
        }
        
        #endregion

        #region Click
        public const int defaultButtonSize = 25;
        
        public static void Lock_UnlockWindowClick(GameObject go)
        {
#if UNITY_EDITOR
            if (ActiveEditorTracker.sharedTracker.isLocked == false && icon.Unlock.ClickUnfocus("Lock Inspector Window"))
            {
                UnityHelperFunctions.FocusOn(ef.serObj.targetObject);
                ActiveEditorTracker.sharedTracker.isLocked = true;
            }

            if (ActiveEditorTracker.sharedTracker.isLocked && icon.Lock.ClickUnfocus("Unlock Inspector Window"))
            {
                ActiveEditorTracker.sharedTracker.isLocked = false;
                UnityHelperFunctions.FocusOn(go);
            }
#endif
        }
        
        static bool ClickLabel(this string label, string hint, GUIStyle style = null) {
            SetBgColor(Color.clear);

            var changed = false;

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
                changed = (style == null) ? ef.Click(label, hint) : ef.Click(label, hint, style);
            else
#endif
            {
                checkLine();
                GUIContent cont = new GUIContent() { text = label, tooltip = hint };
                changed = style == null ? GUILayout.Button(cont) : GUILayout.Button(cont, style);
            }
            PreviousBGcolor();

            return changed;
        }

        public static bool ClickUnfocus(this string text, int width)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                if (ef.Click(text, width))
                {
                    DropFocus();
                    return true;
                }
                return false;
            }
            else
#endif

            {
                checkLine();
                if (GUILayout.Button(text, GUILayout.MaxWidth(width)))
                {
                    DropFocus();
                    return true;
                }
                return false;
            }

        }

        public static bool ClickUnfocus(this Texture tex, int width = defaultButtonSize)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                if (ef.Click(tex, width))
                {
                    DropFocus();
                    return true;
                }
                return false;
            }
            else
#endif

            {
                checkLine();
                if (GUILayout.Button(tex, GUILayout.MaxWidth(width + 5), GUILayout.MaxHeight(width)))
                {
                    DropFocus();
                    return true;
                }
                return false;
            }

        }

        public static bool ClickUnfocus(this Texture tex, string tip, int width = defaultButtonSize)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                if (ef.Click(tex, tip, width))
                {
                    DropFocus();
                    return true;
                }
                return false;
            }
            else
#endif

            {
                return Click(tex, tip, width);
            }

        }

        public static bool ClickUnfocus(this Texture tex, string tip, int width, int height)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                if (ef.Click(tex, tip, width, height))
                {
                    DropFocus();
                    return true;
                }
                return false;
            }
            else
#endif

            {
                return Click(tex, tip, width, height);
            }

        }

        public static bool ClickUnfocus(this string text)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                if (ef.Click(text))
                {
                    DropFocus();
                    return true;
                }
                return false;
            }
            else
#endif

            {
                checkLine();
                if (GUILayout.Button(text))
                {
                    DropFocus();
                    return true;
                }
                return false;
            }

        }

        public static bool Click(this string text, int width)
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
                return ef.Click(text, width);
#endif
                checkLine();
                return GUILayout.Button(text, GUILayout.MaxWidth(width));
        }

        public static bool Click(this string text, ref bool changed) => text.Click().changes(ref changed);

        public static bool Click(this string text)
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
                return ef.Click(text);
#endif
                checkLine();
                return GUILayout.Button(text);
        }

        public static bool Click(this string text, string tip, ref bool changed) => text.Click(tip).changes(ref changed);
        
        public static bool Click(this string text, string tip)
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
                return ef.Click(text, tip);
#endif
                checkLine();
                GUIContent cont = new GUIContent() { text = text, tooltip = tip };
                return GUILayout.Button(cont);
        }

        public static bool Click(this string text, string tip, int width = defaultButtonSize)
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
                return ef.Click(text, tip, width);
#endif
                checkLine();
                GUIContent cont = new GUIContent() { text = text, tooltip = tip };
                return GUILayout.Button(cont, GUILayout.MaxWidth(width));
        }

        static Texture GetTexture_orEmpty(this Sprite sp) => sp ? sp.texture : icon.Empty.GetIcon();

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
            if (!paintingPlayAreaGUI)
                return ef.Click(img, size);
#endif
            
            checkLine();
            return GUILayout.Button(img, GUILayout.MaxWidth(size + 5), GUILayout.MaxHeight(size));
            

        }

        public static bool Click(this Texture img, string tip, int size = defaultButtonSize)  {

            if (!img) img = icon.Empty.GetIcon();

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
                return ef.Click(img, tip, size);
#endif
            
                checkLine();
                return GUILayout.Button(new GUIContent(img, tip), GUILayout.MaxWidth(size + 5), GUILayout.MaxHeight(size));
            

        }

        public static bool Click(this Texture img, string tip, int width, int height)
        {
            if (!img) img = icon.Empty.GetIcon();

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
                return ef.Click(img, tip, width, height);
#endif
                checkLine();
                return GUILayout.Button(new GUIContent(img, tip), GUILayout.MaxWidth(width), GUILayout.MaxHeight(height));
        }

        public static bool Click(this icon icon) => Click(icon.GetIcon(), icon.ToPEGIstring(), defaultButtonSize);

        public static bool Click(this icon icon, ref bool changed) => Click(icon.GetIcon(), icon.ToPEGIstring(), defaultButtonSize).changes(ref changed);

        public static bool ClickUnfocus(this icon icon, ref bool changed) => ClickUnfocus(icon.GetIcon(), icon.ToPEGIstring(), defaultButtonSize).changes(ref changed);
        
        public static bool ClickUnfocus(this icon icon, int size = defaultButtonSize) => ClickUnfocus(icon.GetIcon(), icon.ToPEGIstring(), size);

        public static bool ClickUnfocus(this icon icon, string text, int size = defaultButtonSize) => ClickUnfocus(icon.GetIcon(), text, size);

        public static bool ClickUnfocus(this icon icon, string text, int width, int height) => ClickUnfocus(icon.GetIcon(), text, width, height);

        public static bool Click(this icon icon, int size) => Click(icon.GetIcon(), size);

        public static bool Click(this icon icon, string tip, int width, int height) => Click(icon.GetIcon(), tip, width, height);

        public static bool Click(this icon icon, string tip, ref bool changed, int size = defaultButtonSize) => Click(icon.GetIcon(), tip, size).changes(ref changed);

        public static bool Click(this icon icon, string tip, int size = defaultButtonSize) => Click(icon.GetIcon(), tip, size);
        
        public static bool Click(this Color col) => icon.Empty.GUIColor(col).BGColor(Color.clear).Click().RestoreGUIColor().RestoreBGColor();

        public static bool Click(this Color col, string tip, int size = defaultButtonSize) => icon.Empty.GUIColor(col).BGColor(Color.clear).Click(tip, size).RestoreGUIColor().RestoreBGColor();

        public static bool ClickToEditScript()
        {
#if UNITY_EDITOR
            if (icon.Script.Click("Click to edit current position in a script", 20))
            {
                var frame = new StackFrame(1, true);

                string fileName = frame.GetFileName();

                fileName = fileName.RemoveFirst(fileName.IndexOf("Assets", StringComparison.InvariantCulture));

                UnityEditorInternal.InternalEditorUtility
                                   .OpenFileAtLineExternal(fileName,
                                    frame.GetFileLineNumber()
                                   );
                return true;
            }
#endif

            return false;
        }

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
                return true;
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
                return true;
            }
#endif

            return false;
        }
        
        public static bool ClickHighlight(this UnityEngine.Object obj, int width = defaultButtonSize) =>
           obj.ClickHighlight(icon.Search.GetIcon(), width);

        public static bool ClickHighlight(this UnityEngine.Object obj, Texture tex, int width = defaultButtonSize)
        {
#if UNITY_EDITOR
            if (obj && tex.Click(Msg.HighlightElement.Get()))
            {
                EditorGUIUtility.PingObject(obj);
                return true;
            }
#endif

            return false;
        }

        public static bool ClickHighlight(this UnityEngine.Object obj, icon icon, int width = defaultButtonSize)
        {
#if UNITY_EDITOR
            if (obj && icon.Click(Msg.HighlightElement.Get()))
            {
                EditorGUIUtility.PingObject(obj);
                return true;
            }
#endif

            return false;
        }

        public static bool ClickHighlight(this UnityEngine.Object obj, string hint, icon icon = icon.Enter, int width = defaultButtonSize)
        {
#if UNITY_EDITOR
            if (obj && icon.Click(hint)) {
                EditorGUIUtility.PingObject(obj);
                return true;
            }
#endif

            return false;
        }


        public static bool Click_Attention_Highlight<T>(this T obj, icon icon = icon.Enter, string hint = "", bool canBeNull = true) where T : UnityEngine.Object, INeedAttention
        {
            var ch = obj.Click_Attention(icon, hint, canBeNull);
            obj.ClickHighlight();
            return ch;
        }

        public static bool Click_Attention(this INeedAttention attention, icon icon = icon.Enter, string hint = "", bool canBeNull = true)
        {
            if (attention.IsNullOrDestroyed())   {
                if (!canBeNull)
                    return icon.Warning.ClickUnfocus("Object is null; {0}".F(hint));
            }
            else
            {

                var msg = attention.NeedAttention();
                if (msg != null)
                    return icon.Warning.ClickUnfocus(msg);
            }

            if (hint.IsNullOrEmpty())
                hint = icon.ToString();

            return icon.ClickUnfocus(hint);
        }

        public static bool Click_Attention(this INeedAttention attention, Texture tex, string hint = "", bool canBeNull = true)
        {
            if (attention.IsNullOrDestroyed())
            {
                if (!canBeNull)
                    return icon.Warning.ClickUnfocus("Object is null; {0}".F(hint));
            }
            else
            {

                var msg = attention.NeedAttention();
                if (msg != null)
                    return icon.Warning.ClickUnfocus(msg);
            }

            if (hint.IsNullOrEmpty())
                hint = tex ? tex.ToString() : "Null Texture";

            return tex ? tex.ClickUnfocus(hint) : icon.Enter.ClickUnfocus(hint);
        }

        #endregion

        #region Toggle
        const int defaultToggleIconSize = 35;

        public static bool toggleInt(ref int val)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                return ef.toggleInt(ref val);
            }
            else
#endif
            {
                checkLine();
                bool before = val > 0;
                if (toggle(ref before))
                {
                    val = before ? 1 : 0;
                    return true;
                }
                return false;
            }
        }

        public static bool toggle(this icon icon, ref int selected, int current)
          => icon.toggle(icon.ToString(), ref selected, current);

        public static bool toggle(this icon icon, string label, ref int selected, int current)
        {
            if (selected == current)
                icon.write(label);
            else
                if (icon.Click(label))
            {
                selected = current;
                return true;
            }
            return false;
        }
        
        public static bool toggle(ref bool val)
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
                return ef.toggle(ref val);
#endif
            
                checkLine();
                bool before = val;
                val = GUILayout.Toggle(val, "", GUILayout.MaxWidth(30));
                return (before != val);
            
        }

        public static bool toggle(ref bool val, string text, int width)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                ef.write(text, width);
                return ef.toggle(ref val);
            }
            else
#endif
            {
                checkLine();
                bool before = val;
                val = GUILayout.Toggle(val, text);
                return (before != val);
            }
        }

        public static bool toggle(ref bool val, string text, string tip)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
                return ef.toggle(ref val, text, tip);
            
#endif
                checkLine();
                bool before = val;
                GUIContent cont = new GUIContent() { text = text, tooltip = tip };
                val = GUILayout.Toggle(val, cont);
                return (before != val);
        }

        public static bool toggle(ref bool val, icon TrueIcon, icon FalseIcon, string tip, int width = defaultButtonSize, GUIStyle style = null) => toggle(ref val, TrueIcon.GetIcon(), FalseIcon.GetIcon(), tip, width, style);

        public static bool toggle(ref bool val, icon TrueIcon, icon FalseIcon, GUIStyle style = null) => toggle(ref val, TrueIcon.GetIcon(), FalseIcon.GetIcon(), "", defaultButtonSize, style);

        public static bool toggleVisibilityIcon(this string label, string hint, ref bool val, bool dontHideTextWhenOn = false)
        {
            SetBgColor(Color.clear);

            var ret = toggle(ref val, icon.Show, icon.Hide, hint, defaultToggleIconSize, PEGI_Styles.ToggleButton).PreviousBGcolor();

            if (!val || dontHideTextWhenOn) label.write(hint, PEGI_Styles.ToggleLabel(val));

            return ret;
        }

        public static bool toggleVisibilityIcon(this string label, ref bool val, bool showTextWhenTrue = false)
        {
            var ret = toggle(ref val, icon.Show.BGColor(Color.clear), icon.Hide, label, defaultToggleIconSize, PEGI_Styles.ToggleButton).PreviousBGcolor();

            if (!val || showTextWhenTrue) label.write(PEGI_Styles.ToggleLabel(val));

            return ret;
        }

        public static bool toggleIcon(ref bool val, string hint = "Toggle On/Off") => toggle(ref val, icon.True.BGColor(Color.clear), icon.False, hint, defaultToggleIconSize, PEGI_Styles.ToggleButton).PreviousBGcolor();

        public static bool toggleIcon(ref int val, string hint = "Toggle On/Off") {
            var boo = val != 0;
            if (toggle(ref boo, icon.True.BGColor(Color.clear), icon.False, hint, defaultToggleIconSize, PEGI_Styles.ToggleButton).PreviousBGcolor())
            {
                val = boo ? 1 : 0;
                return true;
            }

            return false;
        }

        public static bool toggleIcon(this string label, string hint, ref bool val, bool hideTextWhenTrue = false)
        {
            SetBgColor(Color.clear);

            var ret = toggle(ref val, icon.True, icon.False, hint, defaultToggleIconSize, PEGI_Styles.ToggleButton).PreviousBGcolor();
            if ((!val || !hideTextWhenTrue) && 
                 label.ClickLabel(hint, PEGI_Styles.ToggleLabel(val))) {
                ret = true;
                val = !val;
            }

            return ret;
        }

        public static bool toggleIcon(this string label, ref bool val, bool hideTextWhenTrue = false)
        {
            var ret = toggle(ref val, icon.True.BGColor(Color.clear), icon.False, label, defaultToggleIconSize, PEGI_Styles.ToggleButton).PreviousBGcolor();

            if ((!val || !hideTextWhenTrue) && label.ClickLabel(label, PEGI_Styles.ToggleLabel(val)))
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
            if (!paintingPlayAreaGUI)
                return ef.toggle(ref val, TrueIcon, FalseIcon, tip, width, style);
#endif
            
                checkLine();
                bool before = val;

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
            if (!paintingPlayAreaGUI)
            {
                ef.write(text, tip, width);
                return ef.toggle(ref val);
            }

#endif

            checkLine();
            bool before = val;
            GUIContent cont = new GUIContent() { text = text, tooltip = tip };
            val = GUILayout.Toggle(val, cont);
            return (before != val);

        }

        public static bool toggle(int ind, CountlessBool tb)
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
                return ef.toggle(ind, tb);
#endif
            bool has = tb[ind];
            if (toggle(ref has))
            {
                tb.Toggle(ind);
                return true;
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

        #endregion

        #region Edit

        #region UnityObject

        public static bool edit<T>(ref T field) where T : UnityEngine.Object
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                return ef.edit(ref field);
            }
            else
#endif

                return false;
        }

        public static bool edit<T>(ref T field, bool allowDrop) where T : UnityEngine.Object
        {


#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                return ef.edit(ref field, allowDrop);
            }
            else
#endif

                return false;
        }
        public static bool edit<T>(this string label, ref T field) where T : UnityEngine.Object
        {

#if UNITY_EDITOR

            if (!paintingPlayAreaGUI)
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
            if (!paintingPlayAreaGUI)
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
            if (!paintingPlayAreaGUI)
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
            if (!paintingPlayAreaGUI)
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
            if (!paintingPlayAreaGUI)
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
            if (!paintingPlayAreaGUI)
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
                return true;
            }

            return false;
        }

        public static bool edit(ref Vector4 val)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                return ef.edit(ref val);
            }
            else
#endif
            {
                checkLine();
                bool modified = false;
                modified |= "X".edit(ref val.x) | "Y".edit(ref val.y) | "Z".edit(ref val.z) | "W".edit(ref val.w);
                return modified;
            }
        }

        public static bool edit(this string label, ref Vector4 val)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                return ef.edit(label, ref val);
            }
            else
#endif
            {

                write(label);
                bool modified = false;
                modified |= edit(ref val.x);
                modified |= edit(ref val.y);
                modified |= edit(ref val.z);
                modified |= edit(ref val.w);
                return modified;
            }


        }

        public static bool edit(ref Vector3 val)
        {
            bool changed = "X".edit(15, ref val.x);
            changed |= "Y".edit(15, ref val.y);
            changed |= "Z".edit(15, ref val.z);

           return changed;
        }

        public static bool edit(this string label, ref Vector3 val)
        {
            write(label);
            nl();
            return edit(ref val);
        }

        public static bool edit(ref Vector2 val)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                return ef.edit(ref val);
            }
            else
#endif
            {
                checkLine();
                bool modified = false;
                modified |= edit(ref val.x);
                modified |= edit(ref val.y);
                return modified;
            }


        }

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

        public static bool edit(ref Vector2 val, float min, float max)
        {
            bool modified = false;

            modified |= "X".edit(10, ref val.x, min, max);
            modified |= "Y".edit(10, ref val.y, min, max);

            return modified;
        }

        public static bool edit(this string label, ref Vector2 val)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                return ef.edit(label, ref val);
            }
            else
#endif
            {

                write(label);
                bool modified = false;
                modified |= edit(ref val.x);
                modified |= edit(ref val.y);
                return modified;
            }


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

        public static bool edit(ref Color col)
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
                return ef.edit(ref col);

#endif
            nl();
            bool changed = icon.Red.edit_ColorChannel(ref col, 0).nl();
            changed |= icon.Green.edit_ColorChannel(ref col, 1).nl();
            changed |= icon.Blue.edit_ColorChannel(ref col, 2).nl();
            changed |= icon.Alpha.edit_ColorChannel(ref col, 3).nl();

            return changed;
        }

        public static bool edit_ColorChannel(this icon ico, ref Color col, int channel)
        {
            bool changed = false;

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
            bool changed = false;

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
            if (paintingPlayAreaGUI)
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
            if (paintingPlayAreaGUI)
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
            if (paintingPlayAreaGUI)
                return false;

            write(label, tip, width);
            return edit(ref col);
        }

        #endregion

        #region Unity Types

        public static bool edit(this string name, ref AnimationCurve val)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                return ef.edit(name, ref val);
            }
            else
#endif
                return false;
        }
        
        public static bool editTexture(this Material mat, string name) => mat.editTexture(name, name);

        public static bool editTexture(this Material mat, string name, string display)
        {
            write(display);
            Texture tex = mat.GetTexture(name);

            if (edit(ref tex))
            {
                mat.SetTexture(name, tex);
                return true;
            }

            return false;
        }

        #endregion

        #region Custom Structs
        public static bool edit(ref MyIntVec2 val)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                return ef.edit(ref val);
            }
            else
#endif
            {
                checkLine();
                bool modified = false;
                modified |= edit(ref val.x);
                modified |= edit(ref val.y);
                return modified;
            }


        }

        public static bool edit(ref MyIntVec2 val, int min, int max)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                return ef.edit(ref val, min, max);
            }
            else
#endif
            {
                checkLine();
                bool modified = false;
                modified |= edit(ref val.x, min, max);
                modified |= edit(ref val.y, min, max);
                return modified;
            }


        }

        public static bool edit(ref MyIntVec2 val, int min, MyIntVec2 max)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                return ef.edit(ref val, min, max);
            }
            else
#endif
            {
                checkLine();
                bool modified = false;
                modified |= edit(ref val.x, min, max.x);
                modified |= edit(ref val.y, min, max.y);
                return modified;
            }


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
            Color c = col.ToGamma();
            if (edit(ref c))
            {
                col.From(c);
                return true;
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
        /*
        public static bool edit(int ind, CountlessInt val)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                int before = val[ind];
                if (ef.edit(ref before, 45))
                {
                    val[ind] = before;
                    return true;
                }
                return false;
            }
            else
#endif
            {
                int before = val[ind];
                if (edit(ref before, 45))
                {
                    val[ind] = before;
                    return true;
                }
                return false;
            }
        }
        */
        #endregion

        #region UInt

        public static bool edit(ref uint val)
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
                return ef.edit(ref val);
#endif
                checkLine();
                string before = val.ToString();
                string newval = GUILayout.TextField(before);
                if (string.Compare(before, newval) != 0) {

                    int newValue;
                    bool parsed = int.TryParse(newval, out newValue);
                    if (parsed)
                        val = (uint)newValue;

                    return true;
                }
                return false;
            

        }

        public static bool edit(ref uint val, int width)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
                return ef.edit(ref val, width);
#endif
            
                checkLine();
                string before = val.ToString();
                string newval = GUILayout.TextField(before, GUILayout.MaxWidth(width));
                if (string.Compare(before, newval) != 0)
                {

                    int newValue;
                    bool parsed = int.TryParse(newval, out newValue);
                    if (parsed)
                        val = (uint)newValue;

                    return true;
                }
                return false;
            
        }

        public static bool edit(ref uint val, uint min, uint max)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
                return ef.edit(ref val, min, max);
            
#endif
            
                checkLine();
                float before = val;
                val = (uint)GUILayout.HorizontalSlider(before, (float)min, (float)max);
                return (before != val);
            
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
            if (!paintingPlayAreaGUI)
            {
                return ef.edit(ref val);
            }
            else
#endif
            {
                checkLine();
                string before = val.ToString();
                string newval = GUILayout.TextField(before);
                if (String.Compare(before, newval) != 0)
                {

                    int newValue;
                    bool parsed = int.TryParse(newval, out newValue);
                    if (parsed)
                        val = newValue;

                    return true;
                }
                return false;
            }

        }
        
        public static bool edit(ref int val, int width)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                return ef.edit(ref val, width);
            }
            else
#endif
            {
                checkLine();
                string before = val.ToString();
                string newval = GUILayout.TextField(before, GUILayout.MaxWidth(width));
                if (String.Compare(before, newval) != 0)
                {

                    int newValue;
                    bool parsed = int.TryParse(newval, out newValue);
                    if (parsed)
                        val = newValue;

                    return true;
                }
                return false;
            }
        }
        
        public static bool edit(ref int val, int min, int max)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                return ef.edit(ref val, (int)min, (int)max);
            }
            else
#endif
            {
                checkLine();
                float before = val;
                val = (int)GUILayout.HorizontalSlider(before, min, max);
                return (before != val);
            }
        }

        static int editedInteger;
        static int editedIntegerIndex;
        public static bool editDelayed(ref int val, int width)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                return ef.editDelayed(ref val, width);
            }
            else
#endif
            {

                checkLine();

                int tmp = (editedIntegerIndex == elementIndex) ? editedInteger : val;

                if (KeyCode.Return.IsDown() && (elementIndex == editedIntegerIndex))
                {
                    edit(ref tmp);
                    val = editedInteger;
                    editedIntegerIndex = -1;

                    elementIndex++;

                    return true;
                }


                if (edit(ref tmp))
                {
                    editedInteger = tmp;
                    editedIntegerIndex = elementIndex;
                }

                elementIndex++;

                return false;
            }
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
            if (!paintingPlayAreaGUI)
            {
                return ef.edit(ref val);
            }
            else
#endif
            {
                checkLine();
                string before = val.ToString();
                string newval = GUILayout.TextField(before);
                if (String.Compare(before, newval) != 0)
                {

                    float newValue;
                    bool parsed = float.TryParse(newval, out newValue);
                    if (parsed)
                        val = newValue;

                    return true;
                }
                return false;
            }
        }

        public static bool edit(ref float val, int width)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                return ef.edit(ref val, width);
            }
            else
#endif
            {
                checkLine();
                string before = val.ToString();
                string newval = GUILayout.TextField(before, GUILayout.MaxWidth(width));
                if (String.Compare(before, newval) != 0)
                {

                    float newValue;
                    bool parsed = float.TryParse(newval, out newValue);
                    if (parsed)
                        val = newValue;

                    return true;
                }
                return false;
            }
        }

        public static bool edit(this string label, ref float val) {
#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
                return ef.edit(label, ref val);
#endif
                write(label);
                return edit(ref val);
        }

        public static bool editPOW(ref float val, float min, float max)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                return ef.editPOW(ref val, min, max);
            }
            else
#endif
            {
                checkLine();
                float before = Mathf.Sqrt(val);
                float after = GUILayout.HorizontalSlider(before, min, max);
                if (before != after)
                {
                    val = after * after;
                    return true;
                }
                return false;

            }
        }

        public static bool edit(ref float val, float min, float max)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                return ef.edit(ref val, min, max);
            }
            else
#endif
            {
                checkLine();
                float before = val;

                val = GUILayout.HorizontalSlider(before, min, max);
                return (before != val);
            }
        }

        static string editedFloat;
        static int editedFloatIndex;
        public static bool editDelayed(ref float val, int width)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
                return ef.editDelayed(ref val, width);
#endif


            checkLine();

            string tmp = (editedFloatIndex == elementIndex) ? editedFloat : val.ToString();

            if (KeyCode.Return.IsDown() && (elementIndex == editedFloatIndex))
            {
                edit(ref tmp);

                float newValue;
                if (float.TryParse(editedFloat, out newValue))
                    val = newValue;
                elementIndex++;

                editedFloatIndex = -1;

                return true;
            }


            if (edit(ref tmp))
            {
                editedFloat = tmp;
                editedFloatIndex = elementIndex;
            }

            elementIndex++;

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
            bool changed = false;
            if (edit(ref from).changes(ref changed))
                to = Mathf.Max(from, to);


            write("-", 10);

            if (edit(ref to).changes(ref changed))
                from = Mathf.Min(from, to);


            return changed;
        }

        static void sliderText(this string label, float val, string tip, int width)
        {
            if (paintingPlayAreaGUI)
                "{0} [{1}]".F(label, val.ToString("F3")).write(width);
            else
                write(label, tip, width);
        }

        public static bool edit(this string label, ref float val, float min, float max)
        {
            label.sliderText(val, label, 90);
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
            if (!paintingPlayAreaGUI)
                return ef.edit(ref val);
            else
#endif
            {
                checkLine();
                string before = val.ToString();
                string newval = GUILayout.TextField(before);
                if (String.Compare(before, newval) != 0)
                {
                    double newValue;
                    if (double.TryParse(newval, out newValue))
                    {
                        val = newValue;
                        return true;
                    }
                }
                return false;
            }

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
            if (!paintingPlayAreaGUI)
            {
                return ef.edit(ref val, width);
            }
            else
#endif
            {
                checkLine();
                string before = val.ToString();
                string newval = GUILayout.TextField(before, GUILayout.MaxWidth(width));
                if (String.Compare(before, newval) != 0)
                {

                    double newValue;
                    bool parsed = double.TryParse(newval, out newValue);
                    if (parsed)
                        val = newValue;

                    return true;
                }
                return false;
            }
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
            int val = Convert.ToInt32(eval);

            if (selectEnum(ref val, typeof(T), width)) {
                eval = (T)((object)val);
                return true;
            }

            return false;
        }
        
        public static int editEnum<T>(T val)
        {
            int ival = Convert.ToInt32(val);
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
            if (!paintingPlayAreaGUI)
            {
                return ef.editDelayed(ref val);
            }
            else
#endif
            {

                checkLine();

                if ((KeyCode.Return.IsDown() && (val.GetHashCode().ToString() == editedHash)))
                {
                    GUILayout.TextField(val);


                    val = editedText;

                    return true;
                }

                string tmp = val;
                if (edit(ref tmp))
                {
                    editedText = tmp;
                    editedHash = val.GetHashCode().ToString();
                }

                return false;
            }
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
            if (!paintingPlayAreaGUI)
            {
                return ef.editDelayed(ref val, width);
            }
            else
#endif
            {

                checkLine();

                if ((KeyCode.Return.IsDown() && (val.GetHashCode().ToString() == editedHash)))
                {
                    GUILayout.TextField(val);


                    val = editedText;

                    return true;
                }

                string tmp = val;
                if (edit(ref tmp, width))
                {
                    editedText = tmp;
                    editedHash = val.GetHashCode().ToString();
                }

                return false;
            }
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
            else
            {
                if (icon.Delete.ClickUnfocus())
                    label = "";
                else
                    write("String is too long {0}".F(label.Substring(0, 10)));
            }
            return true;
        }

        public static bool edit(ref string val) {
            if (LengthIsTooLong(ref val)) return false;

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
                return ef.edit(ref val);
#endif
            
                checkLine();
                string before = val;
                string newval = GUILayout.TextField(before);
                if (String.Compare(before, newval) != 0)
                {
                    val = newval;
                    return true;
                }
                return false;
            
        }

        public static bool edit(ref string val, int width)
        {

            if (LengthIsTooLong(ref val)) return false;

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
                return ef.edit(ref val, width);
#endif
            
                checkLine();
                string before = val;
                string newval = GUILayout.TextField(before, GUILayout.MaxWidth(width));
                if (String.Compare(before, newval) != 0)
                {
                    val = newval;
                    return true;
                }
                return false;
            
        }

        public static bool editBig(ref string val)  {

            nl();

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
                return ef.editBig(ref val).nl();
#endif
            
                checkLine();
                string before = val;
                string newval = GUILayout.TextArea(before);
                if ((String.Compare(before, newval) != 0).nl())
                {
                    val = newval;
                    return true;
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

            if (!paintingPlayAreaGUI) {

                SerializedObject sobj = (!obj ? ef.serObj : GetSerObj(obj)); //new SerializedObject(obj));

                if (sobj != null)  {

                    MemberInfo member = ((MemberExpression)memberExpression.Body).Member;
                    string name = member.Name;

                    var cont = new GUIContent(label, image, tip);

                    SerializedProperty tps = sobj.FindProperty(name);
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
                            changes = true;
                        }

                    }
                }

            }
#endif
            return changes;
        }

        #if UNITY_EDITOR
        static Dictionary<UnityEngine.Object, SerializedObject> SerDic = new Dictionary<UnityEngine.Object, SerializedObject>();

        static SerializedObject GetSerObj(UnityEngine.Object obj)
        {
            SerializedObject so;

            if (!SerDic.TryGetValue(obj, out so))
            {
                so = new SerializedObject(obj);
                SerDic.Add(obj, so);
            }

            return so;

        }
        #endif
        #endregion

        #endregion

        #region LISTS

        #region List MGMT Functions 

        const int listLabelWidth = 105;

        static Dictionary<IList, int> listInspectionIndexes = new Dictionary<IList, int>();

        const int UpDownWidth = 120;
        const int UpDownHeight = 30;
        static int SectionSizeOptimal = 0;
        static int ListSectionMax = 0;
        static int ListSectionStartIndex = 0;
        static readonly CountlessInt ListSectionOptimal = new CountlessInt();

        static void SetOptimalSectionFor(int Count)
        {
            const int listShowMax = 10;

            if (Count < listShowMax) {
                SectionSizeOptimal = listShowMax;
                return;
            }
            
            if (Count > listShowMax * 3)
            {
                SectionSizeOptimal = listShowMax;
                return;
            }

            SectionSizeOptimal = ListSectionOptimal[Count];

            if (SectionSizeOptimal == 0)
            {
                int bestdifference = 999;

                for (int i = listShowMax - 2; i < listShowMax + 2; i++)
                {
                    int difference = i - (Count % i);

                    if (difference < bestdifference)
                    {
                        SectionSizeOptimal = i;
                        bestdifference = difference;
                        if (difference == 0)
                            break;
                    }

                }


                ListSectionOptimal[Count] = SectionSizeOptimal;
            }

        }

        static IList addingNewOptionsInspected = null;
        static string addingNewNameHolder = "Name";

        static void listInstantiateNewName<T>()  {
              Msg.New.Get().write(Msg.NameNewBeforeInstancing_1p.Get().F(typeof(T).ToPEGIstring_Type()) ,30, PEGI_Styles.ExitLabel);
            edit(ref addingNewNameHolder);
        }

        static bool PEGI_InstantiateOptions_SO<T>(this List<T> lst, ref T added, List_Data ld) where T : ScriptableObject
        {
            if (ld != null && !ld.allowCreate)
                return false;

            if (editing_List_Order != null && editing_List_Order == lst)
                return false;

            var indTypes = typeof(T).TryGetDerrivedClasses();

            var tagTypes = typeof(T).TryGetTaggetClasses();

            if (indTypes == null && tagTypes == null && typeof(T).IsAbstract)
                return false;

            bool changed = false;

           // "New {0} ".F(typeof(T).ToPEGIstring_Type()).edit(80, ref addingNewNameHolder);
            listInstantiateNewName<T>();

            if (addingNewNameHolder.Length > 1) {
                if (indTypes == null  && tagTypes == null)  {
                    if (icon.Create.ClickUnfocus("Create new object").nl(ref changed))
                        added = lst.CreateAsset_SO("Assets/ScriptableObjects/", addingNewNameHolder);
                }
                else
                {
                    bool selectingDerrived = lst == addingNewOptionsInspected;

                    icon.Create.foldout("Instantiate Class Options", ref selectingDerrived).nl();

                    if (selectingDerrived)
                        addingNewOptionsInspected = lst;
                    else if (addingNewOptionsInspected == lst)
                        addingNewOptionsInspected = null;

                    if (selectingDerrived)
                    {
                        if (indTypes != null)
                        foreach (var t in indTypes) {
                            write(t.ToPEGIstring_Type());
                            if (icon.Create.ClickUnfocus().nl(ref changed))
                                added = lst.CreateAsset_SO("Assets/ScriptableObjects/", addingNewNameHolder, t);
                        }

                        if (tagTypes != null)
                        {
                            var k = tagTypes.Keys;

                            for (int i=0; i<k.Count; i++) {

                                write(tagTypes.DisplayNames[i]);
                                if (icon.Create.ClickUnfocus().nl(ref changed)) 
                                    added = lst.CreateAsset_SO("Assets/ScriptableObjects/", addingNewNameHolder, tagTypes.TaggedTypes.TryGet(k[i]));

                            }
                        }
                    }
                }
            }
            nl();

            return changed;

        }
        
        static bool PEGI_InstantiateOptions<T>(this List<T> lst, ref T added, List_Data ld)
        {
            if (ld != null && !ld.allowCreate)
                return false;

            if (editing_List_Order != null && editing_List_Order == lst)
                return false;

            var intTypes = typeof(T).TryGetDerrivedClasses();
            
            var tagTypes = typeof(T).TryGetTaggetClasses();
            
            if (intTypes == null && tagTypes == null)
                return false;

            bool changed = false;

            bool hasName = typeof(T).IsSubclassOf(typeof(UnityEngine.Object)) || typeof(IGotName).IsAssignableFrom(typeof(T));

            if (hasName)
                listInstantiateNewName<T>();
            else
                (intTypes == null ? "Create new {0}".F(typeof(T).ToPEGIstring_Type()) : "Create Derrived from {0}".F(typeof(T).ToPEGIstring_Type())).write();

            if (!hasName || addingNewNameHolder.Length > 1) {

                    bool selectingDerrived = lst == addingNewOptionsInspected;

                    icon.Add.foldout("Instantiate Class Options", ref selectingDerrived).nl();

                    if (selectingDerrived)
                        addingNewOptionsInspected = lst;
                    else if (addingNewOptionsInspected == lst)
                        addingNewOptionsInspected = null;

                    if (selectingDerrived)
                    {
                        if (intTypes != null)
                        foreach (var t in intTypes)  {
                            write(t.ToPEGIstring_Type());
                            if (icon.Create.ClickUnfocus().nl(ref changed))  {
                                added = (T)Activator.CreateInstance(t);
                                lst.AddWithUniqueNameAndIndex(added, addingNewNameHolder);
                            }
                        }

                    if (tagTypes != null) {
                        var k = tagTypes.Keys;

                        for (int i = 0; i < k.Count; i++)
                        {

                            write(tagTypes.DisplayNames[i]);
                            if (icon.Create.ClickUnfocus().nl(ref changed)) {
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

        static bool PEGI_InstantiateOptions<T>(this List<T> lst, ref T added, TaggedTypes_STD types, List_Data ld) 
        {
            if (ld != null && !ld.allowCreate)
                return false;

            if (editing_List_Order != null && editing_List_Order == lst)
                return false;

            bool changed = false;

            bool hasName = typeof(T).IsSubclassOf(typeof(UnityEngine.Object)) || typeof(IGotName).IsAssignableFrom(typeof(T));

            if (hasName)
                listInstantiateNewName<T>();
            else
                "Create new {0}".F(typeof(T).ToPEGIstring_Type()).write();

            if (!hasName || addingNewNameHolder.Length > 1) {

                bool selectingDerrived = lst == addingNewOptionsInspected;

                icon.Add.foldout("Instantiate Class Options", ref selectingDerrived).nl();

                if (selectingDerrived)
                    addingNewOptionsInspected = lst;
                else if (addingNewOptionsInspected == lst)
                    addingNewOptionsInspected = null;

                if (selectingDerrived)
                {

                    var k = types.Keys;
                        for (int i=0; i<k.Count; i++) {

                            write(types.DisplayNames[i]);
                            if (icon.Create.ClickUnfocus().nl(ref changed)) {
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

        static int inspectionIndex = -1;

        public static int InspectedIndex => inspectionIndex;

        static IEnumerable<int> InspectionIndexes<T>(this List<T> list)
        {

            #region Inspect Start

            ListSectionStartIndex = 0;

            ListSectionMax = list.Count;

            SetOptimalSectionFor(ListSectionMax);

            if (ListSectionMax >= SectionSizeOptimal * 2)
            {
                if (!listInspectionIndexes.TryGetValue(list, out ListSectionStartIndex))
                    listInspectionIndexes.Add(list, 0);

                if (ListSectionMax > SectionSizeOptimal)
                {
                    bool changed = false;

                    while (ListSectionStartIndex > 0 && ListSectionStartIndex >= ListSectionMax) {
                        changed = true;
                        ListSectionStartIndex = Mathf.Max(0, ListSectionStartIndex - SectionSizeOptimal);
                    }

                    nl();
                    if (ListSectionStartIndex > 0)
                    {
                        if (icon.Up.ClickUnfocus("To previous elements of the list. ", UpDownWidth, UpDownHeight).changes(ref changed))
                        {
                            ListSectionStartIndex = Mathf.Max(0, ListSectionStartIndex - SectionSizeOptimal+1);
                            if (ListSectionStartIndex == 1)
                                ListSectionStartIndex = 0;
                        }
                    }
                    else
                        icon.UpLast.write("Is the first section of the list.", UpDownWidth, UpDownHeight);
                    nl();

                    if (changed)
                        listInspectionIndexes[list] = ListSectionStartIndex;
                }
                else line(Color.gray);


                ListSectionMax = Mathf.Min(ListSectionMax, ListSectionStartIndex + SectionSizeOptimal);
            }
            else if (list.Count > 0)
                line(Color.gray);

            nl();

            #endregion

            for (inspectionIndex = ListSectionStartIndex; inspectionIndex < ListSectionMax; inspectionIndex++)
            {
                switch (inspectionIndex % 4)
                {
                    case 1: PEGI_Styles.listReadabilityBlue.SetBgColor(); break;
                    case 3: PEGI_Styles.listReadabilityRed.SetBgColor(); break;
                }
                yield return inspectionIndex;

                RestoreBGcolor();

            }

          //  if (list.Count > 0)
            //    Line(Color.gray);

            var cnt = list.Count;
            
                if (ListSectionStartIndex > 0 ||  cnt > ListSectionMax)
                {

                    nl();
                    if (cnt > ListSectionMax)
                    {
                        if (icon.Down.ClickUnfocus("To next elements of the list. ", UpDownWidth, UpDownHeight)) {
                            ListSectionStartIndex += SectionSizeOptimal-1;
                            listInspectionIndexes[list] = ListSectionStartIndex;
                        }
                    }
                    else if (ListSectionStartIndex > 0)
                        icon.DownLast.write("Is the last section of the list. ", UpDownWidth, UpDownHeight);

                }
                else if (list.Count > 0)
                    line(Color.gray);
            
        }

        static string currentListLabel = "";
        public static string GetCurrentListLabel<T>(List_Data ld = null) => ld != null ? ld.label :
                    (currentListLabel.IsNullOrEmpty() ? typeof(T).ToPEGIstring_Type()  : currentListLabel);

        static bool listLabel_Used(this bool val) {
            currentListLabel = "";

            return val;
        }
        public static T listLabel_Used<T>(this T val)
        {
            currentListLabel = "";

            return val;
        }

        static void write_ListLabel(this List_Data datas, IList lst) =>
            write_ListLabel(datas.label, ref datas.inspected, lst);

        public static void write_ListLabel(this string label, IList lst = null)
        {
            int notInsp = -1;
            label.write_ListLabel(ref notInsp, lst);
        }

        public static void write_ListLabel(this string label, ref int inspected, IList lst = null) {

            bool editedName = false;

            currentListLabel = label;

            if (lst != null && inspected >= 0 && lst.Count > inspected) {

                var el = lst[inspected];

                el.Try_NameInspect(out editedName, label);

                if (!editedName)
                    label = "{0} {1}".F(label, lst[inspected].ToPEGIstring());
            }

            if (!editedName && label.AddCount(lst, true).ClickLabel(label, PEGI_Styles.ListLabel) && inspected != -1)
                inspected = -1;
        }
        
        static bool ExitOrDrawPEGI<T>(T[] array, ref int index, List_Data ld = null)
        {
            bool changed = false;

            if (index >= 0) {
                if (array == null || index >= array.Length || icon.List.ClickUnfocus("Return to {0} array".F(GetCurrentListLabel<T>(ld))).nl())
                    index = -1;
                else
                    changed |= array[index].Try_Nested_Inspect();
            }

            return changed;
        }

        static bool ExitOrDrawPEGI<T>(this List<T> list, ref int index, List_Data ld = null)
        {
            bool changed = false;

            if (icon.List.ClickUnfocus("{0}[{1}] of {2}".F(Msg.ReturnToList.Get(), list.Count, GetCurrentListLabel<T>(ld))).nl())
                index = -1;
            else
                changed |= list[index].Try_Nested_Inspect();

            return changed;
        }

        static IList editing_List_Order;

        static bool listIsNull<T>(ref List<T> list) {
            if (list == null) {
                if ("Instantiate list".ClickUnfocus().nl())
                    list = new List<T>();
                else
                    return true;
                
            }

            return false;
        }

        static bool list_DropOption<T>(this List<T> list) where T : UnityEngine.Object
        {
            bool changed = false;
#if UNITY_EDITOR

            if (ActiveEditorTracker.sharedTracker.isLocked == false && "Lock Inspector Window".ClickUnfocus())
                ActiveEditorTracker.sharedTracker.isLocked = true;

            if (ActiveEditorTracker.sharedTracker.isLocked && icon.Lock.ClickUnfocus("Unlock Inspector Window")) {
                ActiveEditorTracker.sharedTracker.isLocked = false;

                var mb = ef.serObj.targetObject as MonoBehaviour;
                if (mb)
                    UnityHelperFunctions.FocusOn(mb.gameObject);
                else
                    UnityHelperFunctions.FocusOn(ef.serObj.targetObject);
            }

            foreach (var ret in ef.DropAreaGUI<T>())  {
                list.Add(ret);
                changed = true;
            }
#endif
            return changed;
        }

        static Array editing_Array_Order;

        public static CountlessBool selectedEls = new CountlessBool();

        static List<int> copiedElements = new List<int>();

        static bool move = false;

        static void TryMoveCopiedElement<T>(this List<T> list)
        {
            
            foreach (var e in copiedElements)
                list.TryAdd(listCopyBuffer.TryGet(e));

            for (int i = copiedElements.Count - 1; i >= 0; i--)
                listCopyBuffer.RemoveAt(copiedElements[i]);

            listCopyBuffer = null;
        }

        static bool edit_Array_Order<T>(ref T[] array, List_Data datas = null) {

            bool changed = false;

            const int bttnWidth = 25;

            if (array != editing_Array_Order) {
                if (icon.Edit.ClickUnfocus("Modify list elements", 28))
                    editing_Array_Order = array;
            }

            else if (icon.Done.ClickUnfocus("Finish moving", 28).nl(ref changed))
                editing_Array_Order = null;
            

            if (array == editing_Array_Order) {

                    var derr = typeof(T).TryGetDerrivedClasses();

                    for (int i = 0; i< array.Length; i++) {

                    if (datas == null || datas.allowReorder)
                    {

                        if (i > 0)
                        {
                            if (icon.Up.ClickUnfocus("Move up", bttnWidth).changes(ref changed))
                                CsharpFuncs.Swap(ref array, i, i - 1);
                            
                        }
                        else
                            icon.UpLast.write("Last", bttnWidth);

                        if (i < array.Length - 1)
                        {
                            if (icon.Down.ClickUnfocus("Move down", bttnWidth).changes(ref changed))
                                CsharpFuncs.Swap(ref array, i, i + 1);
                            
                        }
                        else icon.DownLast.write(bttnWidth);
                    }

                        var el = array[i];

                    var isNull = el.IsNullOrDestroyed();

                    if (datas == null || datas.allowDelete) {
                        if (!isNull && typeof(T).IsUnityObject())
                        {
                            if (icon.Delete.ClickUnfocus(Msg.MakeElementNull, bttnWidth).changes(ref changed))
                                array[i] = default(T);
                        }
                        else
                        {
                            if (icon.Close.ClickUnfocus("Remove From Array", bttnWidth).changes(ref changed)) {
                                CsharpFuncs.Remove(ref array, i);
                                i--;
                            }
                        }
                    }

                        if (!isNull && derr != null) {
                            var ty = el.GetType();
                            if (select(ref ty, derr, el.ToPEGIstring()))
                            array[i] = (el as ISTD).TryDecodeInto<T>(ty);
                        }

                        if (!isNull)
                            write(el.ToPEGIstring());
                        else
                            "Empty {0}".F(typeof(T).ToPEGIstring_Type()).write();

                        nl();
                    }
            }

            return changed;
        }
        
        static bool edit_List_Order<T>(this List<T> list, List_Data meta = null) {

            bool changed = false;

            const int bttnWidth = 25;

            if (list != editing_List_Order) {
                if (icon.Edit.ClickUnfocus("Change Order", 28))
                    editing_List_Order = list;
            }

            else if (icon.Done.ClickUnfocus("Finish moving", 28).changes(ref changed))
                editing_List_Order = null;
            

            if (list == editing_List_Order) {
#if UNITY_EDITOR
                if (!paintingPlayAreaGUI)
                {
                    nl();
                    changed |= ef.reorder_List(list, meta);
                }
                else
#endif
                #region Playtime UI reordering
                {
                    var derr = typeof(T).TryGetDerrivedClasses();

                    foreach (var i in list.InspectionIndexes()) {

                        if (meta == null || meta.allowReorder)
                        {

                            if (i > 0)
                            {
                                if (icon.Up.ClickUnfocus("Move up", bttnWidth).changes(ref changed))
                                    list.Swap(i - 1);
                                
                            }
                            else
                                icon.UpLast.write("Last", bttnWidth);

                            if (i < list.Count - 1) {
                                if (icon.Down.ClickUnfocus("Move down", bttnWidth).changes(ref changed))
                                    list.Swap(i);
                            }
                            else icon.DownLast.write(bttnWidth);
                        }

                        var el = list[i];

                        var isNull = el.IsNullOrDestroyed();

                        if (meta == null || meta.allowDelete)
                        {

                            if (!isNull && typeof(T).IsUnityObject())
                            {
                                if (icon.Delete.ClickUnfocus(Msg.MakeElementNull, bttnWidth))
                                    list[i] = default(T);
                            }
                            else
                            {
                                if (icon.Close.ClickUnfocus(Msg.RemoveFromList, bttnWidth).changes(ref changed))
                                {
                                    list.RemoveAt(inspectionIndex);
                                    inspectionIndex--;
                                    ListSectionMax--;
                                }
                            }
                        }


                        if (!isNull && derr != null)
                        {
                            var ty = el.GetType();
                            if (select(ref ty, derr, el.ToPEGIstring()))
                                list[i] = (el as ISTD).TryDecodeInto<T>(ty);
                        }

                        if (!isNull)
                            write(el.ToPEGIstring());
                        else
                            "Empty {0}".F(typeof(T).ToPEGIstring_Type()).write();

                        nl();
                    }

                }

                #endregion

                #region Select
                int selectedCount = 0;

                if (meta == null) {
                    for (int i = 0; i < list.Count; i++)
                        if (selectedEls[i]) selectedCount++;
                }
                else for (int i = 0; i < list.Count; i++)
                        if (meta.GetIsSelected(i)) selectedCount++;

                if (selectedCount > 0 && icon.DeSelectAll.Click("Deselect All"))
                    SetSelected(meta, list, false);

                if (selectedCount == 0 && icon.SelectAll.Click("Select All"))
                    SetSelected(meta, list, true);


                #endregion

                #region Copy, Cut, Paste, Move 
             
                if (listCopyBuffer != null) {

                    if (icon.Close.ClickUnfocus("Clean buffer"))
                        listCopyBuffer = null;

                    if (listCopyBuffer != list)
                    {

                        if (typeof(T).IsUnityObject())
                        {

                            if (!move && icon.Paste.ClickUnfocus("Try Past References Of {0} to here".F(listCopyBuffer.ToPEGIstring())))
                            {
                                foreach (var e in copiedElements)
                                    list.TryAdd(listCopyBuffer.TryGet(e));
                            }

                            if (move && icon.Move.ClickUnfocus("Try Move References Of {0}".F(listCopyBuffer)))
                                list.TryMoveCopiedElement();

                        }
                        else
                        {

                            if (!move && icon.Paste.ClickUnfocus("Try Add Deep Copy {0}".F(listCopyBuffer.ToPEGIstring())))
                            {

                                foreach (var e in copiedElements)
                                {

                                    var istd = listCopyBuffer.TryGet(e) as ISTD;

                                    if (istd != null)
                                        list.TryAdd(istd.Clone_ISTD());
                                }
                            }

                            if (move && icon.Move.ClickUnfocus("Try Move {0}".F(listCopyBuffer)))
                                list.TryMoveCopiedElement();
                        }
                    }
                }
                else if (selectedCount > 0)
                {
                    bool copyOrMove = false;

                    if (icon.Copy.ClickUnfocus("Copy List Elements"))
                    {
                        move = false;
                        copyOrMove = true;
                    }

                    if (icon.Cut.ClickUnfocus("Cut List Elements"))
                    {
                        move = true;
                        copyOrMove = true;
                    }

                    if (copyOrMove)
                    {
                        listCopyBuffer = list;
                        if (meta != null)
                            copiedElements = meta.GetSelectedElements();
                        else
                            copiedElements = selectedEls.GetItAll();
                    }
                }


                #endregion

                #region Clean & Delete

                if (list != listCopyBuffer)
                {

                    if ((meta == null || meta.allowDelete) && list.Count > 0)
                    {
                        int nullOrDestroyedCount = 0;

                        for (int i = 0; i < list.Count; i++)
                            if (list[i].IsNullOrDestroyed())
                                nullOrDestroyedCount++;

                        if (nullOrDestroyedCount > 0 && icon.Refresh.ClickUnfocus("Clean null elements"))
                        {
                            for (int i = list.Count - 1; i >= 0; i--)
                                if (list[i].IsNullOrDestroyed())
                                    list.RemoveAt(i);

                            SetSelected(meta, list, false);
                        }
                    }

                    if ((meta == null || meta.allowDelete) && list.Count > 0)
                    {
                        if (selectedCount > 0 && icon.Delete.Click("Delete {0} Selected".F(selectedCount)))
                        {
                            if (meta == null)
                            {
                                for (int i = list.Count - 1; i >= 0; i--)
                                    if (selectedEls[i]) list.RemoveAt(i);
                            }
                            else for (int i = list.Count - 1; i >= 0; i--)
                                    if (meta.GetIsSelected(i))
                                        list.RemoveAt(i);

                            SetSelected(meta, list, false);

                        }
                    }
                }
                #endregion

                if ((meta != null) && icon.Config.enter(ref meta.inspectListMeta))
                   meta.Nested_Inspect();

            }

            return changed;
        }

        static void SetSelected<T>(List_Data meta, List<T> list, bool val)
        {
            if (meta == null)
            {
                for (int i = 0; i < list.Count; i++)
                    selectedEls[i] = val;
            }
            else for (int i = 0; i < list.Count; i++)
                    meta.SetIsSelected(i, val);
        }

        static bool edit_List_Order_Obj<T>(this List<T> list, List_Data datas) where T : UnityEngine.Object {
            var changed = list.edit_List_Order(datas);

            if (list == editing_List_Order && datas != null) {

                if (icon.Search.ClickUnfocus("Find objects by GUID"))
                    for (int i = 0; i < list.Count; i++)

                        if (list[i] == null)
                        {
                            var dta = datas.elementDatas.TryGet(i);
                            if (dta != null)
                            {
                                T tmp = null;
                                if (dta.TryGetByGUID(ref tmp))
                                    list[i] = tmp;
                            }
                        }
            }

            return changed;
        }

        static IList listCopyBuffer = null;

        public static bool Name_ClickInspect_PEGI<T>(this object el, List<T> list, int index, ref int edited, List_Data datas = null) {
            bool changed = false;

            var pl = el.TryGet_fromObj<IPEGI_ListInspect>();

            if (pl != null)
            {
                if (pl.PEGI_inList(list, index, ref edited).changes(ref changed) || PEGI_Extensions.EfChanges)
                    pl.SetToDirty();
            } else {

                if (el.IsNullOrDestroyed()) {
                    ElementData ed = datas?[index];
                    if (ed == null)
                        "{0}: NULL {1}".F(index, typeof(T).ToPEGIstring_Type()).write();
                    else 
                        ed.PEGI_inList<T>(ref el, index, ref edited);
                }
                else {
                    var uo = el as UnityEngine.Object;

                    IPEGI pg = el.TryGet_fromObj<IPEGI>();
                    if (pg != null)
                        el = pg;

                    var need = el as INeedAttention;
                    string warningText = need?.NeedAttention();

                    if (warningText != null)
                        attentionColor.SetBgColor();

                    bool clickHighlightHandeled = false;

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
                        if (uo == null && pg == null && datas == null)
                            el.ToPEGIstring().write();
                        else
                        {
                            Texture tex = null;

                            if (uo)
                            {
                                tex = uo as Texture;
                                if (tex)
                                {
                                    uo.ClickHighlight(tex);
                                    clickHighlightHandeled = true;
                                }
                            }
                            write(el.ToPEGIstring());
                        }
                    }

                    if (pg != null)
                    {
                        if ((warningText == null && (datas == null ? icon.Enter : datas.icon).ClickUnfocus(Msg.InspectElement)) || (warningText != null && icon.Warning.ClickUnfocus(warningText)))
                            edited = index;
                        warningText = null;
                    }

                    if (!clickHighlightHandeled)
                        uo.ClickHighlight();
                }
            }  
 
            RestoreBGcolor();

            return changed;
        }
        
        static bool isMonoType<T>(List<T> list, int i)
        {
            if ((typeof(MonoBehaviour)).IsAssignableFrom(typeof(T)))
            {
                GameObject mb = null;
                if (edit(ref mb))
                {
                    list[i] = mb.GetComponent<T>();
                    if (list[i] == null) (typeof(T).ToString() + " Component not found").showNotificationIn3D_Views();

                }
                return true;

            }
            return false;
        }

        static bool ListAddNewClick<T>(this List<T> list, ref T added, List_Data ld = null) {

            if ((ld != null && !ld.allowCreate) || !typeof(T).IsNew())
                return false;

            if ((typeof(T).TryGetClassAttribute<DerrivedListAttribute>() != null || typeof(T).TryGetTaggetClasses() != null))
                return false;

            if (icon.Add.ClickUnfocus(Msg.AddNewListElement.Get()))
            {
                if (typeof(T).IsSubclassOf(typeof(UnityEngine.Object))) // //typeof(MonoBehaviour)) || typeof(T).IsSubclassOf(typeof(ScriptableObject)))
                {
                    list.Add(default(T));
                }
                else
                    added = list.AddWithUniqueNameAndIndex();

                return true;
            }

            return false;
        }

        static bool ListAddEmptyClick<T>(this List<T> list, List_Data ld = null)
        {

            if (ld != null && !ld.allowCreate)
                return false;

            if (!typeof(T).IsUnityObject() && (typeof(T).TryGetClassAttribute<DerrivedListAttribute>() != null || typeof(T).TryGetTaggetClasses() != null))
                return false;

            if (icon.Add.ClickUnfocus(Msg.AddNewListElement.Get()))
            {
                list.Add(default(T));
                return true;
            }
            return false;
        }

        #endregion

        #region MonoBehaviour
        public static bool edit_List_MB<T>(this string label, ref List<T> list, ref int inspected, ref T added) where T : MonoBehaviour
        {
            label.write_ListLabel( ref inspected, list);
            bool changed = false;
            edit_List_MB(ref list, ref inspected, ref changed).listLabel_Used();
            return changed;
        }

        public static bool edit_List_MB<T>(this List_Data datas, ref List<T> list) where T : MonoBehaviour {
            datas.write_ListLabel(list);
            bool changed = false;
            edit_List_MB(ref list, ref datas.inspected, ref changed, datas).listLabel_Used();
            return changed;
        }

        public static T edit_List_MB<T>(ref List<T> list, ref int inspected, ref bool changed, List_Data datas = null) where T : MonoBehaviour
        {
    
            T added = default(T);

            if (listIsNull(ref list))
                return added;

            int before = inspected;

            list.ClampIndexToCount(ref inspected, -1);

            changed |= (inspected != before);

            if (inspected == -1)
            {
                changed |= list.ListAddEmptyClick(datas);

                if (datas != null && icon.Save.ClickUnfocus())
                    datas.SaveElementDataFrom(list);

                changed |= list.edit_List_Order_Obj(datas);

                if (list != editing_List_Order)
                {
                    // list.InspectionStart();
                    foreach (var i in list.InspectionIndexes()) // (int i = ListSectionStartIndex; i < ListSectionMax; i++)
                    {

                        var el = list[i];
                        if (!el)
                        {
                            T obj = null;

                            if (datas.TryInspect(ref obj, i))
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
                            changed |= el.Name_ClickInspect_PEGI(list, i, ref inspected, datas);
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
            label.write_ListLabel(ref inspected, list);

            return edit_List_SO(ref list, ref inspected, ref changed).listLabel_Used();
        }
        
        public static bool edit_List_SO<T>(ref List<T> list, ref int inspected) where T : ScriptableObject
        {
            bool changed = false;

            edit_List_SO<T>(ref list, ref inspected, ref changed);

            return changed;
        }

        public static bool edit_List_SO<T>(this string label, ref List<T> list, ref int inspected) where T : ScriptableObject
        {
            label.write_ListLabel(ref inspected, list);

            bool changed = false;

            edit_List_SO<T>(ref list, ref inspected, ref changed).listLabel_Used();

            return changed;
        }

        public static bool edit_List_SO<T>(this string label, ref List<T> list) where T : ScriptableObject
        {
            label.write_ListLabel(list);

            bool changed = false;

            int edited = -1;

            edit_List_SO<T>(ref list, ref edited, ref changed).listLabel_Used();

            return changed;
        }

        public static bool edit_List_SO<T>(this List_Data datas, ref List<T> list) where T : ScriptableObject
        {
            write_ListLabel(datas, list);

            bool changed = false;

            edit_List_SO(ref list, ref datas.inspected, ref changed, datas).listLabel_Used();

            return changed;
        }

        public static T edit_List_SO<T>(ref List<T> list, ref int inspected, ref bool changed, List_Data datas = null) where T : ScriptableObject
        {
            if (listIsNull(ref list))
                return null;
            

            T added = default(T);

            int before = inspected;
            if (list.ClampIndexToCount(ref inspected, -1))
            changed |= (inspected != before);

            if (inspected == -1)
            {

                changed |= list.edit_List_Order_Obj(datas);

                changed |= list.ListAddEmptyClick(datas);

                if (datas != null && icon.Save.ClickUnfocus())
                    datas.SaveElementDataFrom(list);

                if (list != editing_List_Order) {
                    foreach (var i in list.InspectionIndexes()) {
                        var el = list[i];
                        if (!el)
                        {
                            if (datas.TryInspect(ref el, i).changes(ref changed))
                                list[i] = el;
                            
                        }
                        else
                        {

                            changed |= el.Name_ClickInspect_PEGI<T>(list, i, ref inspected, datas);

#if UNITY_EDITOR
                            var path = AssetDatabase.GetAssetPath(el);

                            if (!path.IsNullOrEmpty())
                            {
                                if (icon.Copy.ClickUnfocus().changes(ref changed))
                                {
                                    added = el.DuplicateScriptableObject();

                                    list.Insert(i + 1, added);

                                    var indx = added as IGotIndex;

                                    if (indx != null)
                                    {
                                        int max = 0;

                                        foreach (var eee in list)
                                            if (eee) {
                                                var eeind = eee as IGotIndex;
                                                if (eeind != null)
                                                    max = Math.Max(max, eeind.IndexForPEGI + 1);
                                            }

                                        indx.IndexForPEGI = max;
                                    }

                                }
                            }
#endif
                        }

                        newLine();
                    }

                    if (typeof(T).TryGetDerrivedClasses() != null)
                        changed |= list.PEGI_InstantiateOptions_SO(ref added, datas);

                    nl();

                }
                else list.list_DropOption();
            }
            else changed |= list.ExitOrDrawPEGI(ref inspected);

            newLine();
            return added;
        }
        #endregion

        #region Obj

        public static bool edit_List_UObj<T>(this string label, ref List<T> list, ref int inspected, List<T> selectFrom = null) where T : UnityEngine.Object
        {
            label.write_ListLabel(ref inspected, list);
            return edit_or_select_List_UObj(ref list, selectFrom, ref inspected);
        }

        public static bool edit_List_UObj<T>(ref List<T> list, ref int inspected, List<T> selectFrom = null) where T : UnityEngine.Object
            => edit_or_select_List_UObj(ref list, selectFrom, ref inspected);

        public static bool edit_List_UObj<T>(this string label, ref List<T> list, List<T> selectFrom = null) where T : UnityEngine.Object {
            label.write_ListLabel(list);
            return list.edit_List_UObj(selectFrom).listLabel_Used();
        }

        public static bool edit_List_UObj<T>(this List<T> list, List<T> selectFrom = null) where T : UnityEngine.Object{
                int edited = -1;
                return edit_or_select_List_UObj(ref list, selectFrom, ref edited, null);
        }
        
        public static bool edit_List_UObj<T>(this List_Data datas, ref List<T> list, List<T> selectFrom = null) where T : UnityEngine.Object
        {
            datas.label.write_ListLabel(ref datas.inspected, list);
            return edit_or_select_List_UObj(ref list, selectFrom, ref datas.inspected, datas).listLabel_Used();
        }

        public static bool edit_or_select_List_UObj<T,G>(this string label, ref List<T> list, List<G> from, ref int inspected, List_Data datas = null) where T : G where G : UnityEngine.Object
        {
            label.write_ListLabel(ref inspected, list);
            return edit_or_select_List_UObj(ref list, from, ref inspected, datas).listLabel_Used();
        }

        public static bool edit_or_select_List_UObj<T,G>(ref List<T> list, List<G> from, ref int inspected, List_Data datas = null) where T : G where G : UnityEngine.Object
        {
            if (listIsNull(ref list))
                return false;
            
            bool changed = false;

            int before = inspected;
            if (list.ClampIndexToCount(ref inspected, -1))
            changed |= (inspected != before);

            if (inspected == -1) {

                if (datas != null && icon.Save.ClickUnfocus())
                    datas.SaveElementDataFrom(list);

                changed |= list.edit_List_Order(datas);

                if (list != editing_List_Order)
                {
                    changed |= list.ListAddEmptyClick(datas);

                    foreach (var i in list.InspectionIndexes())     {
                        var el = list[i];
                        if (!el)
                        {
                            if (!from.IsNullOrEmpty() && select_SameClass(ref el, from))
                                list[i] = el;

                            if (datas.TryInspect(ref el, i))
                                list[i] = el;
                        }
                        else
                            changed |= list[i].Name_ClickInspect_PEGI(list, i, ref inspected, datas);

                        newLine();
                    }
                }
                else
                    list.list_DropOption();

            }
            else changed |= list.ExitOrDrawPEGI(ref inspected);
            newLine();
            return changed;

        }
        #endregion

        #region OfNew
        public static T edit<T>(this List_Data ld, ref List<T> list, ref bool changed)
        {
            ld.label.write_ListLabel(ref ld.inspected, list);
            return edit_List(ref list, ref ld.inspected, ref changed, ld).listLabel_Used();
        }
        
        public static bool edit_List<T>(this string label, ref List<T> list, ref int inspected) 
        {
            label.write_ListLabel(ref inspected, list);
            return edit_List(ref list, ref inspected).listLabel_Used();
        }

        public static bool edit_List<T>(ref List<T> list, ref int inspected) 
        {
            bool changes = false;
            edit_List(ref list, ref inspected, ref changes);
            return changes;
        }

        public static bool edit_List<T>(this string label, ref List<T> list) 
        {
            label.write_ListLabel(list);
            return edit_List(ref list).listLabel_Used();
        }

        public static bool edit_List<T>(ref List<T> list) 
        {
            int edited = -1;
            bool changes = false;
            edit_List(ref list, ref edited, ref changes);
            return changes;
        }

        public static T edit_List<T>(this string label, ref List<T> list, ref int inspected, ref bool changed)
        {
            label.write_ListLabel(ref inspected, list);
            return edit_List(ref list, ref inspected, ref changed).listLabel_Used();
        }

        public static bool edit_List<T>(this List_Data datas, ref List<T> list)  {
           write_ListLabel(datas, list);
            bool changed = false;
            edit_List(ref list, ref datas.inspected, ref changed, datas).listLabel_Used();
            return changed;
        }

        public static T edit_List<T>(this List_Data datas, ref List<T> list, ref bool changed) {

            write_ListLabel(datas, list);
            return edit_List(ref list, ref datas.inspected, ref changed, datas).listLabel_Used();

        }

        public static T edit_List<T>(ref List<T> list, ref int inspected, ref bool changed, List_Data datas = null)  {

            T added = default(T);

            if (list == null) {
                if ("Init list".ClickUnfocus())
                    list = new List<T>();
                else 
                    return added;
            }

            int before = inspected;
            if (inspected >= list.Count)
                inspected = -1;

            changed |= (inspected != before);

            if (inspected == -1)  {

                changed |= list.edit_List_Order(datas);

                if (list != editing_List_Order) {
                    
                        list.ListAddNewClick(ref added, datas).changes(ref changed);

                    foreach (var i in list.InspectionIndexes())   {

                        var el = list[i];
                        if (el.IsNullOrDestroyed()) {
                            if (!isMonoType(list, i))
                            {
                                if (typeof(T).IsSubclassOf(typeof(UnityEngine.Object)))
                                    write("use edit_List_UObj");
                                else
                                    write("is NUll");
                            }
                        }
                        else
                            changed |= list[i].Name_ClickInspect_PEGI(list, i, ref inspected, datas);

                        newLine();
                    }

                    changed |= list.PEGI_InstantiateOptions(ref added, datas);

                    nl();
                }
            }
            else changed |= list.ExitOrDrawPEGI(ref inspected);

            newLine();
            return added;
        }

        #region Tagged Types

        public static T edit_List<T>(this List_Data datas, ref List<T> list, TaggedTypes_STD types, ref bool changed)
        {
            write_ListLabel(datas, list);
            return edit_List(ref list, ref datas.inspected, types, ref changed, datas).listLabel_Used();
        }

        public static bool edit_List<T>(this List_Data datas, ref List<T> list, TaggedTypes_STD types) {
            bool changed = false;
            write_ListLabel(datas, list);
            edit_List(ref list, ref datas.inspected, types, ref changed, datas).listLabel_Used();
            return changed;
        }


        public static T edit_List<T>(this string label, ref List<T> list, TaggedTypes_STD types, ref bool changed, List_Data ld = null)
        {
            label.write_ListLabel(ref ld.inspected, list);
            return edit_List(ref list, ref ld.inspected, types, ref changed, ld).listLabel_Used();
        }

        public static T edit_List<T>(this string label, ref List<T> list, ref int inspected, TaggedTypes_STD types, ref bool changed) {
            label.write_ListLabel(ref inspected, list);
            return edit_List(ref list, ref inspected, types, ref changed).listLabel_Used();
        }
        
        public static T edit_List<T>(ref List<T> list, ref int inspected, TaggedTypes_STD types, ref bool changed, List_Data datas = null) {

            T added = default(T);

            if (list == null)
            {
                if ("Init list".ClickUnfocus())
                    list = new List<T>();
                else
                    return added;
            }

            int before = inspected;
            if (inspected >= list.Count)
                inspected = -1;

            changed |= (inspected != before);

            if (inspected == -1) {

                changed |= list.edit_List_Order(datas);

                if (list != editing_List_Order) {
 
                    foreach (var i in list.InspectionIndexes())  {

                        var el = list[i];
                        if (el == null) {

                            if (!isMonoType<T>(list, i)) {
                                if (typeof(T).IsSubclassOf(typeof(UnityEngine.Object)))
                                    write("use edit_List_UObj");
                                else
                                    write("is NUll");
                            }
                        }
                        else
                            changed |= list[i].Name_ClickInspect_PEGI(list, i, ref inspected, datas);

                        newLine();
                    }

                    changed |= list.PEGI_InstantiateOptions(ref added, types, datas);

                    nl();
                }
            }
            else changed |= list.ExitOrDrawPEGI(ref inspected);

            newLine();
            return added;
        }

        #endregion

        #endregion

        #region Lambda

        #region SpecialLambdas

        static IList listElementsRoles = null;

        static int lambda_int(int val)
        {
            edit(ref val);
            return val;
        }

        static string lambda_string_role(string val)
        {

            var role = listElementsRoles.TryGet(InspectedIndex);
            if (role != null)
                role.ToPEGIstring().edit(90, ref val);
            else edit(ref val);

            return val;
        }

        public static string lambda_string(string val)
        {
            edit(ref val);
            return val;
        }

        static T lambda_Obj_role<T>(T val) where T : UnityEngine.Object
        {

            var role = listElementsRoles.TryGet(InspectedIndex);
            if (!role.IsNullOrDestroyed())
                role.ToPEGIstring().edit(90, ref val);
            else edit(ref val);

            return val;
        }

        public static bool edit_List(this string label, ref List<int> list) =>
            label.edit_List(ref list, lambda_int);

        public static bool edit_List(this string label, ref List<string> list) =>
         label.edit_List(ref list, lambda_string);

        public static bool edit_List_WithRoles(this string label, ref List<string> list, IList roles)
        {
            listElementsRoles = roles;
            return label.edit_List(ref list, lambda_string_role);
        }

        public static bool edit_List_WithRoles<T>(this string label, ref List<T> list, IList roles) where T : UnityEngine.Object
        {
            label.write_ListLabel(list);
            listElementsRoles = roles;
            var ret = edit_List_UObj(ref list, lambda_Obj_role);
            listElementsRoles = null;
            return ret;
        }

        #endregion



        public static T edit_List<T>(this string label, ref List<T> list, ref bool changed, Func<T, T> lambda) where T : new()
        {
            label.write_ListLabel(list);
            return edit_List<T>(ref list, ref changed, lambda).listLabel_Used();
        }

        public static T edit_List<T>(ref List<T> list, ref bool changed, Func<T, T> lambda) where T : new()
        {

            T added = default(T);

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
                    bool isNull = el.IsNullOrDestroyed();
                    if (((!isNull && !el.Equals(before)) || (isNull && !before.IsNullOrDestroyed())).changes(ref changed))
                        list[i] = el;
                    
                    nl();
                }

                nl();
            }

            newLine();
            return added;
        }

        public static bool edit_List<T>(this string label, ref List<T> list, Func<T, T> lambda) where T : new()
        {
            label.write_ListLabel(list);
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
            T added = default(T);
   
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
                    bool isNull = el.IsNullOrDestroyed();
                    if (((!isNull && !el.Equals(before)) || (isNull && !before.IsNullOrDestroyed())).changes(ref changed))
                        list[i] = el;
                    
                    nl();
                }

                nl();
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
              
                    if (((el && !el.Equals(before)) || (!el && before)).changes(ref changed))
                        list[i] = el;
                    
                    nl();
                }

                nl();
            }

            newLine();
            return changed;
        }

        public static bool edit_List(this string name, ref List<string> list, Func<string, string> lambda)
        {
            name.write_ListLabel(list);
            return edit_List(ref list, lambda).listLabel_Used();
        }

        public static bool edit_List(ref List<string> list, Func<string, string> lambda)
        {
            bool changed = false;
            if (listIsNull(ref list))
                return changed;

            changed |= list.edit_List_Order();

            if (list != editing_List_Order) {
                if (icon.Add.ClickUnfocus().changes(ref changed))
                    list.Add("");
                  
                foreach (var i in list.InspectionIndexes()) {
                    var el = list[i];
                    var before = el;

                    el = lambda(el);

                    if ((!before.SameAs(el)).changes(ref changed))
                        list[i] = el;

                    nl();
                }

                nl();
            }

            newLine();
            return changed;
        }

        #endregion
        
        #region NotNew

        public static bool write_List<T>(this string label, List<T> list, Func<T, bool> lambda)
        {
            label.write_ListLabel(list);
            return list.write_List(lambda).listLabel_Used();

        }

        public static bool write_List<T>(this List<T> list, Func<T, bool> lambda)
        {
            bool changed = false;

            if (list == null)
            {
                "Empty List".nl();
                return changed;
            }

            //list.InspectionStart();
            foreach (var i in list.InspectionIndexes())
            { // (int i = ListSectionStartIndex; i < ListSectionMax; i++) {
                changed |= lambda(list[i]);
                nl();
            }
            //list.InspectionEnd();

            nl();

            return changed;
        }

        public static bool write_List<T>(this string label, List<T> list)
        {
            int edited = -1;
            label.write_ListLabel(list);
            return list.write_List<T>(ref edited).listLabel_Used();
        }

        public static bool write_List<T>(this string label, List<T> list, ref int inspected)
        {
            nl();
            label.write_ListLabel(ref inspected, list);

            return list.write_List<T>(ref inspected).listLabel_Used();
        }

        public static bool write_List<T>(this List<T> list, ref int edited)
        {
            bool changed = false;

            int before = edited;

            list.ClampIndexToCount(ref edited, -1); 

            changed |= (edited != before);

            if (edited == -1)
            {
                nl();

                for (int i = 0; i < list.Count; i++) {

                    var el = list[i];
                    if (el == null)
                        write("NULL");
                    else
                        changed |= list[i].Name_ClickInspect_PEGI(list, i, ref edited);
                    
                    newLine();
                }

            }
            else
                changed |= list.ExitOrDrawPEGI(ref edited);


            newLine();
            return changed;
        }
        #endregion

        #endregion

        #region Dictionaries
        
        public static bool editKey(ref Dictionary<int, string> dic, int key)
        {

#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
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
            if (!paintingPlayAreaGUI)
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
                if ("Instantiate list".ClickUnfocus().nl())
                    dic = new Dictionary<G, T>();
                else
                    return true;
            }
            return false;
        }

        public static bool edit_Dictionary_Values<G, T>(this string label, ref Dictionary<G, T> dic, ref int inspected)
        {
            label.write_ListLabel(ref inspected);
            return edit_Dictionary_Values(ref dic, ref inspected);
        }

        public static bool edit_Dictionary_Values<G, T>(ref Dictionary<G, T> dic, ref int inspected, List_Data ld = null)
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
                    if ((ld == null || ld.allowDelete) && icon.Delete.ClickUnfocus(25).changes(ref changed))
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
                            write(el.ToPEGIstring(), 120);

                        if ((el is IPEGI) && icon.Enter.ClickUnfocus(Msg.InspectElement, 25))
                            inspected = i;
                    }
                    newLine();
                }
            }
            else
            {
                if (icon.Back.ClickUnfocus(25).nl().changes(ref changed))
                    inspected = -1;
                else
                    changed |= dic.ElementAt(inspected).Value.Try_Nested_Inspect();
            }

            newLine();
            return changed;
        }

        public static bool edit_Dictionary_Values(this string label, ref Dictionary<int, string> dic, List<string> roles)
        {
            write_ListLabel(label, dic.ToList());
            return edit_Dictionary_Values(ref dic, roles);
        }

        public static bool edit_Dictionary_Values(ref Dictionary<int, string> dic, List<string> roles) {
            listElementsRoles = roles;
            var ret = edit_Dictionary_Values(ref dic, lambda_string_role, false);
            listElementsRoles = null;
            return ret;
        }

        public static bool edit_Dictionary_Values<G, T>(this string label, ref Dictionary<G, T> dic, Func<T, T> lambda, bool showKey = true, List_Data ld = null)
        {
            write_ListLabel(label);
            return edit_Dictionary_Values(ref dic, lambda, false, ld);
        }

        public static bool edit_Dictionary_Values<G, T>(ref Dictionary<G, T> dic, Func<T, T> lambda, bool showKey = true ,List_Data ld = null)
        {
            bool changed = false;

            if (dicIsNull(ref dic))
                return changed;

            nl();
            for (int i = 0; i < dic.Count; i++) {
                var item = dic.ElementAt(i);
                var itemKey = item.Key;

                inspectionIndex = Convert.ToInt32(itemKey);

                if ((ld == null || ld.allowDelete) && icon.Delete.ClickUnfocus(25).changes(ref changed)) 
                    dic.Remove(itemKey);
                else {
                    if (showKey)
                        itemKey.ToPEGIstring().write(50);

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
            write_ListLabel(label);
            return edit_Dictionary(ref dic);
        }

        public static bool edit_Dictionary(ref Dictionary<int, string> dic) {

            bool changed = false;

            if (dicIsNull(ref dic))
                return changed;

            for (int i = 0; i < dic.Count; i++) {

                var e = dic.ElementAt(i);
                inspectionIndex = e.Key;

                if (icon.Delete.ClickUnfocus(20))
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

            if ((isNewIndex) && (isNewValue) && (icon.Add.ClickUnfocus("Add Element", 25).changes(ref changed)))
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

        public static bool edit_Array<T>(this string label, ref T[] array, List_Data datas = null) 
        {
            int inspected = -1;
            label.write_ListLabel(array);
            return edit_Array(ref array, ref inspected, datas).listLabel_Used();
        }

        public static bool edit_Array<T>(this string label, ref T[] array, ref int inspected, List_Data datas = null)   {
            label.write_ListLabel(ref inspected, array);
            return edit_Array(ref array, ref inspected, datas).listLabel_Used();
        }

        public static bool edit_Array<T>(ref T[] array, ref int inspected, List_Data datas = null) 
        {
            bool changes = false;
            edit_Array(ref array, ref inspected, ref changes, datas);
            return changes;
        }

        public static T edit_Array<T>(ref T[] array, ref int inspected, ref bool changed, List_Data datas = null)  {


            T added = default(T);

            if (array == null)  {
                if ("init array".ClickUnfocus().nl())
                    array = new T[0];
            } else {

                changed |= ExitOrDrawPEGI(array, ref inspected);
                 
                if (inspected == -1) {

                    if (!typeof(T).IsNew()) {
                        if (icon.Add.ClickUnfocus("Add empty element"))
                        CsharpFuncs.Expand(ref array, 1);
                    } else if (icon.Create.ClickUnfocus("Add New Instance"))
                        CsharpFuncs.AddAndInit(ref array, 1);
                    


                    changed |= edit_Array_Order(ref array, datas).nl();

                    if (array != editing_Array_Order)
                    for (int i = 0; i < array.Length; i++) 
                        changed |= array[i].Name_ClickInspect_PEGI<T>(null, i, ref inspected, datas).nl();
                }
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

            if (icon.Copy.ClickUnfocus("Copy Transform"))
                STDExtensions.copyBufferValue = tf.Encode(editLocalSpace).ToString();
 
            if (STDExtensions.copyBufferValue != null && icon.Paste.ClickUnfocus("Paste Transform").changes(ref changed)) {
                STDExtensions.copyBufferValue.DecodeInto(tf);
                STDExtensions.copyBufferValue = null;
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

            var uobj = obj.TryGetGameObject(); 

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

        public static bool inspect_Name(this IGotName obj) => obj.inspect_Name("", obj.ToPEGIstring());

        public static bool inspect_Name(this IGotName obj, string label) => obj.inspect_Name(label, label);

        public static bool inspect_Name(this IGotName obj, string label, string hint)
        {
            var n = obj.NameForPEGI;

            bool gotLabel = !label.IsNullOrEmpty();

            if ((gotLabel && label.editDelayed(80, ref n) || (!gotLabel && editDelayed(ref n))))
            {
                obj.NameForPEGI = n;


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

        static bool ToPEGIstringInterfacePart(this object obj, out string name)
        {
            name = null;
            #if PEGI
            var dn = obj as IGotDisplayName;
            if (dn != null) {
                name = dn.NameForPEGIdisplay;
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
        
        static string RemovePreDots(this string name)
        {
            int ind = Mathf.Max(name.LastIndexOf("."), name.LastIndexOf("+"));
            return (ind == -1 || ind>name.Length-5) ? name : name.Substring(ind + 1);
        }

        public static string ToPEGIstring_Type(this Type type) => type.ToString().RemovePreDots();
           
        public static string ToPEGIstring_UObj<T>(this T obj) where T: UnityEngine.Object {
            if (obj == null)
                return "NULL UObj {0}".F(typeof(T).ToPEGIstring_Type());

            if (!obj)
                return "Destroyed UObj {0}".F(typeof(T).ToPEGIstring_Type());

            string tmp;
            if (obj.ToPEGIstringInterfacePart(out tmp))
                return tmp;
            
            return obj.name;
        }

        public static string ToPEGIstring<T>(this T obj) {

            if (obj == null)
                return "NULL {0}".F(typeof(T).ToPEGIstring_Type());

            if (obj.GetType().IsUnityObject())
                return (obj as UnityEngine.Object).ToPEGIstring_UObj();

            string tmp;
            if (obj.ToPEGIstringInterfacePart(out tmp))
                return tmp;

            return obj.ToString().RemovePreDots();
        }

        public static string ToPEGIstring(this object obj) {

            if (obj is string)
                return (string)obj;

            if (obj == null) return "NULL";

            if (obj.GetType().IsUnityObject())
                return (obj as UnityEngine.Object).ToPEGIstring_UObj();

            string tmp;
            if (obj.ToPEGIstringInterfacePart(out tmp))
                return tmp;

            return obj.ToString().RemovePreDots();
        }

        static void cantInspect()
        {



#if PEGI
#if !UNITY_EDITOR
             "PEGI is compiled without UNITY_EDITOR directive".nl();
#endif
#else
#if UNITY_EDITOR
             if (GUILayout.Button("Enable PEGI inspector")){
               "Recompilation in progress ".showNotificationIn3D_Views();
            

            PEGI_StylesDrawer.EnablePegi();
            }
#endif
#endif
        }

        public static bool Inspect<T>(this T o, object so) where T : MonoBehaviour, IPEGI
        {
#if PEGI && UNITY_EDITOR
            return ef.Inspect(o, (SerializedObject)so).RestoreBGColor();
#else
            cantInspect();
            return false;
#endif
        }

        public static bool Inspect_so<T>(this T o, object so) where T : ScriptableObject, IPEGI
        {
#if PEGI && UNITY_EDITOR
            return ef.Inspect_so(o, (SerializedObject)so).RestoreBGColor();
#else
            cantInspect();
            return false;
#endif
        }
        
#if PEGI

        public static int focusInd;

        public static bool EfChanges
        {
            get
            {
#if UNITY_EDITOR
                return ef.changes;
#else
            return false;
#endif
            }
            set
            {
#if UNITY_EDITOR
                ef.changes = value;
#endif
            }
        }

        public static bool Nested_Inspect(this IPEGI pgi)
        {
            if (pgi.IsNullOrDestroyed())
                return false;

                var isFOOE = pegi.isFoldedOut_or_Entered;

                var changes = pgi.Inspect().RestoreBGColor();

                if (changes || EfChanges)
                    pgi.SetToDirty();

                pegi.isFoldedOut_or_Entered = isFOOE;

                return changes;

        }

        public static bool Inspect_AsInList(this IPEGI_ListInspect obj)
        {
            int tmp = -1;
            var changes = obj.PEGI_inList(null, 0, ref tmp);

            if (changes)
                obj.SetToDirty();

            return changes;
        }
        
        public static bool Try_Nested_Inspect(this GameObject go)
        {
            bool changed = false;

            var pgi = go.TryGet<IPEGI>();

            if (pgi != null)
                changed |= pgi.Nested_Inspect().RestoreBGColor();

            return changed;
        }

        public static bool Try_Nested_Inspect(this Component cmp ) => cmp ? cmp.gameObject.Try_Nested_Inspect() : false;

        public static bool Try_Nested_Inspect(this object obj) {
            var pgi = obj.TryGet_fromObj<IPEGI>();
            return pgi != null ? pgi.Nested_Inspect() : false;
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

        public static bool TryInspect<T>(this List_Data ld, ref T obj, int ind) where T : UnityEngine.Object
        {
            var el = ld.TryGetElement(ind); 

            if (el != null)
                return el.PEGI_inList_Obj(ref obj);
            else
                return pegi.edit(ref obj);
        }

        public static int CountForInspector<T>(this List<T> lst) where T : IGotCount  {
            int count = 0;
            foreach (var e in lst)
                if (!e.IsNullOrDestroyed())
                    count += e.CountForInspector;

            return count;
        }

        public static int TryCountForInspector<T>(this List<T> list)
        {

            if (list.IsNullOrEmpty())
                return 0;

            int count = list.Count;

            foreach (var e in list)
            {
                var cnt = e as IGotCount;
                if (!cnt.IsNullOrDestroyed())
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
                    if (!el.IsNullOrDestroyed() && el.IndexForPEGI == index)
                        return el;
#endif
            return default(T);
        }

        public static T GetByIGotIndex<T, G>(this List<T> lst, int index) where T : IGotIndex where G : T
        {
#if PEGI
            if (lst != null)
                foreach (var el in lst)
                    if (!el.IsNullOrDestroyed() && el.IndexForPEGI == index && el.GetType() == typeof(G))
                        return el;
#endif
            return default(T);
        }

        public static T GetByIGotName<T>(this List<T> lst, string name) where T : IGotName
        {

#if PEGI
            if (lst != null)
                foreach (var el in lst)
                    if (!el.IsNullOrDestroyed() && el.NameForPEGI.SameAs(name))
                        return el;
#endif

            return default(T);
        }

        public static T GetByIGotName<T>(this List<T> lst, T other) where T : IGotName
        {

#if PEGI
            if (lst != null && !other.IsNullOrDestroyed())
                foreach (var el in lst)
                    if (!el.IsNullOrDestroyed() && el.NameForPEGI.SameAs(other.NameForPEGI))
                        return el;
#endif

            return default(T);
        }
        
        public static G GetByIGotName<T, G>(this List<T> lst, string name) where T : IGotName where G : class, T
        {
#if PEGI
            if (lst != null)
                foreach (var el in lst)
                    if (!el.IsNullOrDestroyed() && el.NameForPEGI.SameAs(name) && el.GetType() == typeof(G))
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
