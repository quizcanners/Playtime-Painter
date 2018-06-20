#if PEGI

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using System;
using System.Linq;

using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using SharedTools_Stuff;

#pragma warning disable IDE1006 // Naming Styles
namespace PlayerAndEditorGUI
{

#region interfaces & Attributes



    public interface IPEGI
    {
        bool PEGI();
    }

    public interface INeedAttention
    {
        string NeedAttention();
    }

    public interface IPEGI_ListInspect
    {
        bool PEGI_inList(IList list, int ind, ref int edited);
    }

    public interface IGotName
    {
        string NameForPEGI { get; set; }
    }

    public interface IGotDisplayName
    {
        string NameForPEGIdisplay(); 
    }
    
    public interface IGotIndex
    {
        int GetIndex();
        void SetIndex(int index);
    }
    
#endregion

    

    public static class pegi
    {

#region Other Stuff
        public delegate bool CallDelegate();

        public class windowPositionData
        {
            public windowFunction funk;
            public Rect windowRect;

            public void drawFunction(int windowID)
            {
                paintingPlayAreaGUI = true;

                elementIndex = 0;
                lineOpen = false;
                focusInd = 0;

                GUI.backgroundColor = Color.white;

                funk();

                mouseOverUI = windowRect.Contains(Input.mousePosition);

                newLine();
                GUI.DragWindow(new Rect(0, 0, 10000, 20));
                newLine();
                GUI.color = Color.white;
                ("Tip:" + GUI.tooltip).nl();

                paintingPlayAreaGUI = false;
            }

            public void Render(windowFunction doWindow, string c_windowName)
            {
                funk = doWindow;
                windowRect = GUILayout.Window(0, windowRect, drawFunction, c_windowName);
            }

            public void collapse()
            {
                windowRect.width = 10;
                windowRect.height = 10;
            }

            public windowPositionData()
            {
                windowRect = new Rect(20, 20, 120, 400);
            }
        }
        
        public delegate bool windowFunction();
        
        static int elementIndex;
        static int focusInd;

        public static bool isFoldedOut = false;
        public static bool mouseOverUI = false;

        static int selectedFold = -1;
        public static int tabIndex; // will be reset on every NewLine;
        public static bool paintingPlayAreaGUI { get; private set; }

        static GUIStyle head = null;
        static GUIStyle heading
        {
            get
            {
                if (head == null)
                    head = new GUIStyle() {
                        margin = new RectOffset(10, 10, 10, 10),
                        fontSize = 15,
                        richText = true,
                        wordWrap = true
                    };
                   
                return head;
            }
        }

        static bool lineOpen;
        
        public static void checkLine()
        {



#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {

                ef.checkLine();
            }
            else
#endif
        if (!lineOpen)
            {
                tabIndex = 0;
                GUILayout.BeginHorizontal();
                lineOpen = true;
            }



        }

        public static void end(this GameObject go)
        {
#if UNITY_EDITOR


           //if (paintingPlayAreaGUI == false)
                ef.end(go);

#endif

        }

        public static void DropFocus() => FocusControl("_");

        public static string needsAttention(this IList list)
        {


            for (int i = 0; i < list.Count; i++) {

                var el = list[i];
                if (el != null)
            {
                var need = el as INeedAttention;
                if (need != null)
                {
                    var what = need.NeedAttention();
                    if (what != null)
                    {
                        what = " " + what + " on "+i+":" + need.ToPEGIstring();
                        return what;
                    }
                }

            }
        }
            return null;
        }

        public static void FocusControl(string name)
        {
#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                ef.focusTextInControl(name);
            }
            else
#endif
            {
                GUI.FocusControl(name);

            }

        }

        public static string thisMethodName() => thisMethodName(1);
        
        public static string thisMethodName(int up) => (new StackFrame(up)).GetMethod().Name;
        
        public static void NameNext(string name) => GUI.SetNextControlName(name);
        
        public static int NameNextUnique(ref string name)
        {
            name += focusInd.ToString();
            GUI.SetNextControlName(name);
            focusInd++;

            return (focusInd - 1);
        }

        public static string nameFocused
        {
            get
            {
                return GUI.GetNameOfFocusedControl();
            }
        }

        public static void Space()
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                ef.Space();
            }
            else
#endif

            {
                checkLine();
                GUILayout.Space(10);
            }
        }

        public static void Line(Color col)
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

#endregion

#region New Line

        public static void newLine()
        {
#if UNITY_EDITOR
            if (!paintingPlayAreaGUI)
            {
                ef.newLine();
            }
            else
#endif
        if (lineOpen)
            {
                lineOpen = false;
                GUILayout.EndHorizontal();
            }
        }

        public static void nl() => newLine();


        public static bool nl(this bool value)
        {
            newLine();
            return value;
        }

        public static bool nl(this string value)
        {
            write(value);
            newLine();
            return false;
        }

        public static bool nl(this string value, string tip)
        {
            write(value, tip);
            newLine();
            return false;
        }

        public static bool nl(this string value, int width)
        {
            write(value, width);
            newLine();
            return false;
        }

        public static bool nl(this string value, string tip, int width)
        {
            write(value, tip, width);
            newLine();
            return false;
        }

        public static void nl(this icon icon, int size) => write(icon.getIcon(), size);


#endregion

#region WRITE

        public static void write<T>(T field) where T : UnityEngine.Object
        {


#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                ef.write(field);
            }
            else
#endif
            {
                checkLine();
                write(field == null ? "-no " + typeof(T).ToString() : field.ToPEGIstring());
            }

        }

        public static void write<T>(this string label, string tip, int width, T field) where T : UnityEngine.Object
        {
            write(label, tip, width);
            write(field);

        }

        public static void write<T>(this string label, int width, T field) where T : UnityEngine.Object
        {
            write(label, width);
            write(field);

        }

        public static void write<T>(this string label, T field) where T : UnityEngine.Object
        {
            write(label);
            write(field);

        }

        public static void write(Texture img, int width)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                ef.write(img, width);
            }
            else
#endif
            {
                checkLine();
                GUIContent c = new GUIContent() { image = img } ;

                GUILayout.Label(c, GUILayout.MaxWidth(width + 5), GUILayout.MaxHeight(width));
            }

        }

        public static void write(Texture img, string tip, int width)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                ef.write(img, tip, width);
            }
            else
#endif
            {
                checkLine();
                GUIContent c = new GUIContent() { image = img, tooltip = tip };
                GUILayout.Label(c, GUILayout.MaxWidth(width + 5), GUILayout.MaxHeight(width));
            }

        }

        public static void write(Texture img, string tip, int width, int height)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                ef.write(img, tip, width, height);
            }
            else
#endif
            {
                checkLine();
                GUIContent c = new GUIContent() { image = img, tooltip = tip };
                GUILayout.Label(c, GUILayout.MaxWidth(width + 5), GUILayout.MaxHeight(height));
            }

        }

        public static void write(this icon icon) => write(icon.getIcon(), defaultButtonSize);
        
        public static void write(this icon icon, int size) =>  write(icon.getIcon(), size);
        
        public static void write(this icon icon, string tip, int size) => write(icon.getIcon(), tip, size);
        
        public static void write(this icon icon, string tip, int width, int height) => write(icon.getIcon(), tip, width, height);
        
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
                GUILayout.Label(text,style);
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

        public static void write(this string text, int width) => text.write(text, width);
        
        public static void write(this string text, string tip)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
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
            if (paintingPlayAreaGUI == false)
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

        public static void writeWarning(this string text)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
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
            if (paintingPlayAreaGUI == false)
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

        public static void showNotification(this string text)
        {
#if UNITY_EDITOR

            if (!Application.isPlaying)
            {
                var lst = Resources.FindObjectsOfTypeAll<SceneView>();
                if (lst.Length > 0)
                    lst[0].ShowNotification(new GUIContent(text));

            }
            else

            {


                //   EditorWindow gameview =
                var ed = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
                if (ed != null)
                    ed.ShowNotification(new GUIContent(text));
                //var lst = Resources.FindObjectsOfTypeAll<>();
            }
#endif
        }

        public static void resetOneTimeHint(this string name) => PlayerPrefs.SetInt(name, 0);
        
        public static bool writeOneTimeHint(this string text, string name)
        {

            if (PlayerPrefs.GetInt(name) != 0) return false;

            newLine();

            text += " (press OK)";

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                ef.writeHint(text, MessageType.Info);
            }
            else
#endif
            {
                checkLine();
                GUILayout.Label(text);
            }

            if (Click("OK", 50).nl())
            {
                PlayerPrefs.SetInt(name, 1);

                return true;
            }


            return false;
        }

#endregion
        
#region SELECT

        public static bool select(this string text, int width, ref int value, string[] array)
        {
            write(text, width);
            return select(ref value, array);
        }

        public static bool select<T>(this string text, int width, ref T value, List<T> array)
        {
            write(text, width);
            return select(ref value, array);
        }

        public static bool select(this string text, ref string val, List<string> lst)
        {
            write(text);
            return select(ref val, lst);
        }

        public static bool select<T>(this string text, ref T value, List<T> list)
        {
            write(text);
            return select(ref value, list);
        }

        public static bool select(this string text, int width, ref string val, List<string> lst)
        {
            write(text, width);
            return select(ref val, lst);
        }

        public static bool select<T>(this string text, ref int ind, List<T> lst)
        {
            write(text);
            return select(ref ind, lst);
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

        public static bool select<T>(this string text, string tip, int width, ref int ind, T[] lst)
        {
            write(text, tip, width);
            return select(ref ind, lst);
        }

        public static bool select<T>(this string text, ref T value, T[] lst)
        {
            write(text);
            return select(ref value, lst);
        }

        public static bool select<T>(this string text, string tip, ref int ind, List<T> lst)
        {
            write(text, tip);
            return select(ref ind, lst);
        }

        public static bool select<T>(this string text, int width, ref int ind, List<T> lst)
        {
            write(text, width);
            return select(ref ind, lst);
        }

        public static bool select<T>(this string text, string tip, int width, ref int ind, List<T> lst)
        {
            write(text, tip, width);
            return select(ref ind, lst);
        }

        public static bool select(ref int no, List<string> from)
        {
#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.select(ref no, from.ToArray());
            }
            else
#endif

            {
                if ((from == null) || (from.Count == 0)) return false;

                foldout(from[Mathf.Min(from.Count - 1, no)]);
                newLine();

                if (isFoldedOut)
                {
                    for (int i = 0; i < from.Count; i++)
                    {
                        if (i != no) //write("Selected: "+from[i]);
                                     // else
                            if (Click(i + ": " + from[i], 100))
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

        public static bool select(ref int no, string[] from)
        {
#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.select(ref no, from);
            }
            else
#endif

            {
                if ((from == null) || (from.Length == 0)) return false;

                foldout((no > -1) ? from[Mathf.Min(from.Length - 1, no)] : ". . .");
                newLine();

                if (isFoldedOut)
                {
                    for (int i = 0; i < from.Length; i++)
                    {
                        if (i != no) //write("Selected: "+from[i]);
                                     // else
                            if (Click(i + ": " + from[i], 100))
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

        public static bool select(ref int no, string[] from, int width)
        {
#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.select(ref no, from, width);
            }
            else
#endif

            {
                if ((from == null) || (from.Length == 0)) return false;

                foldout(from[Mathf.Min(from.Length - 1, no)]);
                newLine();

                if (isFoldedOut)
                {
                    for (int i = 0; i < from.Length; i++)
                    {
                        if (i != no) 
                            if (Click(i + ": " + from[i], width))
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

        public static bool select<T>(ref T val, T[] lst)
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
                        lnms.Add(j + ": " + tmp.ToPEGIstring());
                        indxs.Add(j);
                    }
                }

                if (jindx == -1 && val != null)
                    lnms.Add(">>" + val.ToPEGIstring() + "<<");


                if (select(ref jindx, lnms.ToArray()) && (jindx < indxs.Count))
                {
                    val = lst[indxs[jindx]];
                    return true;
                }

                return false;
            
        }

        public static bool select<T>(ref int ind, T[] lst)
        {

                checkLine();

                var lnms = new string[lst.Length];

                int before = ind;
                ind = ind.ClampZeroTo(lst.Length);


                for (int i = 0; i < lst.Length; i++)
                {
                    var e = lst[i];
                    lnms[i] = (i + ": " + (e == null ? "Nothing" : e.ToPEGIstring()));
                }

                if (select(ref ind, lnms))
                    return true;

                return before != ind;
            
        }

        public static bool select<T>(ref int val, List<T> lst, Func<T, bool> lambda)
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
                    lnms.Add(j + ": " + tmp.ToPEGIstring());
                    indxs.Add(j);
                }
            }

            if (jindx == -1 && val >= 0)
                lnms.Add(">>" + val.ToPEGIstring() + "<<");
            
            if (select(ref jindx, lnms.ToArray()) && (jindx < indxs.Count))
            {
                val = indxs[jindx];
                return true;
            }

            return false;
        }

        public static bool select<T>(ref T val, List<T> lst, Func<T, bool> lambda)
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

                    lnms.Add(j.ToString() + ":" + tmp.ToPEGIstring());
                    indxs.Add(j);
                }
            }

            if (jindx == -1 && val != null)
                lnms.Add(">>" + val.ToPEGIstring() + "<<");

            if (select(ref jindx, lnms.ToArray()) && (jindx < indxs.Count))
            {
                val = lst[indxs[jindx]];
                changed = true;
            }

            return changed;
            
        }

        public static bool select<T>(this string label, ref int val, List<T> list, Func<T, bool> lambda)
        {
            write(label);
            return select(ref val, list, lambda);
        }

        public static bool select<T>(this string label, int width, ref int val, List<T> list, Func<T, bool> lambda)
        {
            write(label, width);
            return select(ref val, list, lambda);
        }

        public static bool select<T>(this string label, string tip, int width, ref int val, List<T> list, Func<T, bool> lambda)
        {
            write(label, tip, width);
            return select(ref val, list, lambda);
        }

        public static bool select<T>(this string text, ref T val, List<T> list, Func<T, bool> lambda) {
            write(text);
            return select(ref val, list, lambda);
        }

        public static bool select<T>(this string text, int width, ref T val, List<T> list, Func<T, bool> lambda)
        {
            write(text, width);
            return select(ref val, list, lambda);
        }

        public static bool select<T>(this string text, string hint, int width, ref T val, List<T> list, Func<T, bool> lambda)
        {
            write(text, hint, width);
            return select(ref val, list, lambda);
        }

        public static bool select<T>(ref T val, List<T> lst)
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
                        lnms.Add(j + ": " + tmp.ToPEGIstring());
                        indxs.Add(j);
                    }
                }

                if (jindx == -1 && val != null)
                    lnms.Add(">>" + val.ToPEGIstring() + "<<");

                if (select(ref jindx, lnms.ToArray()) && (jindx < indxs.Count))
                {
                    val = lst[indxs[jindx]];
                    return true;
                }

                return false;
            
        }

        public static bool select_SameClass<T,G>(ref T val, List<G> lst) where T : class where G : class
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
                        lnms.Add(j.ToString()+":"+tmp.ToPEGIstring());
                        indxs.Add(j);
                    }
                }

            if (jindx == -1 && val != null)
                lnms.Add(">>" + val.ToPEGIstring() + "<<");

            if (select(ref jindx, lnms.ToArray()) && (jindx < indxs.Count))
                {
                    val = lst[indxs[jindx]] as T;
                    changed = true;
                }

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

        public static bool select<T>(ref int ind, List<T> lst)
        {


                checkLine();

                List<string> lnms = new List<string>();
                List<int> indxs = new List<int>();

                int jindx = -1;

                for (int j = 0; j < lst.Count; j++)
                {
                    if (lst[j] != null)
                    {
                        if (ind == j)
                            jindx = indxs.Count;
                        lnms.Add(lst[j].ToPEGIstring());
                        indxs.Add(j);

                    }
                }

            if (jindx == -1)
                lnms.Add(">>" + ind.ToString() + "<<");

            if (select(ref jindx, lnms.ToArray()) && (jindx < indxs.Count))
                {
                    ind = indxs[jindx];
                    return true;
                }

                return false;
            
        }

        public static bool select<T>(ref int ind, List<T> lst, int width)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.select(ref ind, lst, width);
            }
            else
#endif

            {
                return select(ref ind, lst);
              
            }
        }

        public static bool select<T>(ref int i, T[] ar, bool clampValue) where T : IeditorDropdown
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.select<T>(ref i, ar, clampValue);
            }
            else
#endif

            {
                checkLine();

                bool changed = false;

                List<string> lnms = new List<string>();
                List<int> ints = new List<int>();

                int ind = -1;

                if (clampValue)
                {
                    i = i.ClampZeroTo(ar.Length);
                    if (ar[i].showInDropdown() == false)
                        for (int v = 0; v < ar.Length; v++)
                        {
                            T val = ar[v];
                            if (val.showInDropdown())
                            {
                                i = v;
                                changed = true;
                                break;
                            }
                        }
                }

                for (int j = 0; j < ar.Length; j++)
                {
                    T val = ar[j];
                    if (val.showInDropdown())
                    {
                        if (i == j) ind = ints.Count;
                        lnms.Add(val.ToPEGIstring());
                        ints.Add(j);
                    }
                }

                //int newNo = ind; EditorGUILayout.Popup(ind, lnms.ToArray());

                if (select(ref ind, lnms.ToArray())) // (newNo != ind)
                {
                    i = ints[ind];
                    changed = true;
                }
                return changed;
            }
        }

        public static bool select<T>(ref int no, CountlessSTD<T> tree) where T : iSTD, new()
        {
#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
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

        public static bool select<T>(this string label, int width, ref int no, Countless<T> tree)
        {
            label.write(width);
            return select<T>(ref no, tree);
        }

        public static bool select<T>(this string label, ref int no, Countless<T> tree) {
            label.write();
            return select<T>(ref no, tree);
        }

        public static bool select<T>(ref int no, Countless<T> tree)
        {
#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
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


                if (tmpindex == -1)
                    filtered.Add(">>" + no.ToPEGIstring() + "<<");


                    if (select(ref tmpindex, filtered.ToArray()) && tmpindex< inds.Count)
                {
                    no = inds[tmpindex];
                    return true;
                }
                return false;
            }
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

        public static bool select<T>(ref int no, Countless<T> tree, Func<T, bool> lambda)
        {
#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.select(ref no, tree);
            }
            else
#endif
            {
                List<int> unfinds;
                List<int> inds = new List<int>();
                List<T> objs = tree.GetAllObjs(out unfinds);
                List<string> filtered = new List<string>();
                int tmpindex = -1;
                int j=0;
                for (int i = 0; i < objs.Count; i++)
                {

                    var el = objs[i];
                    
                    if (el != null && lambda(el))
                    {
                        inds.Add(unfinds[i]);
                        if (no == inds[j])
                            tmpindex = j;
                        filtered.Add(objs[i].ToPEGIstring());
                        j++;
                    }
                }



                if (tmpindex == -1)
                    filtered.Add(">>" + no.ToPEGIstring() + "<<");


                if (select(ref tmpindex, filtered.ToArray()) && tmpindex < inds.Count)

                  //  if (select(ref tmpindex, filtered.ToArray()))
                {
                    no = inds[tmpindex];
                    return true;
                }
                return false;
            }
        }

        public static bool selectEnum(ref int current, Type type)
        {
#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.select(ref current, type);
            }
            else
#endif

            {
                checkLine();
                int tmpVal = current;

                string[] names = Enum.GetNames(type);
                int[] val = (int[])Enum.GetValues(type);

                for (int i = 0; i < val.Length; i++)
                    if (val[i] == current)
                        tmpVal = i;

                if (select(ref tmpVal, names))
                {
                    current = val[tmpVal];
                    return true;
                }

                return false;
            }
        }

        public static bool select<T>(ref int current)
        {
#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.select(ref current, typeof(T));
            }
            else
#endif

            {
                checkLine();
                int tmpVal = current;
                Type t = typeof(T);
                string[] names = Enum.GetNames(t);
                int[] val = (int[])Enum.GetValues(t);

                for (int i = 0; i < val.Length; i++)
                    if (val[i] == current)
                        tmpVal = i;

                if (select(ref tmpVal, names))
                {
                    current = val[tmpVal];
                    return true;
                }

                return false;
            }
        }

        public static bool select(ref int current, Dictionary<int, string> from)
        {
#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.select(ref current, from);
            }
            else
#endif

            {
                checkLine();

                checkLine();
                string[] options = new string[from.Count];

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
        }

        public static bool select(ref int current, Dictionary<int, string> from, int width)
        {
#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.select(ref current, from, width);
            }
            else
#endif
        
            {
                checkLine();
                
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
        }

        public static bool select(CountlessInt cint, int ind, Dictionary<int, string> from)
        {

            int value = cint[ind];

            if (select(ref value, from))
            {

                cint[ind] = value;

                return true;
            }
            return false;
        }

        public static bool select(ref int no, Texture[] tex)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.select(ref no, tex);
            }
            else
#endif
            {

                if (tex.Length == 0) return false;

                checkLine();

                List<string> tnames = new List<string>();
                List<int> tnumbers = new List<int>();

                int curno = 0;
                for (int i = 0; i < tex.Length; i++)
                    if (tex[i] != null)
                    {
                        tnumbers.Add(i);
                        tnames.Add(i + ": " + tex[i].name);
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
        }

        public static bool selectOrAdd(ref int no, ref Texture[] tex)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.selectOrAdd(ref no, ref tex);
            }
            else
#endif
            {


                return select(ref no, tex);
            }
        }


        // ***************************** Select or edit
        
        public static bool select_or_edit<T>(string text, string hint, int width, ref T obj, List<T> list) where T : UnityEngine.Object
        {
            if (list == null || list.Count == 0)
                return edit(ref obj);
            else
            {
                bool changed = false;
                if (icon.Delete.Click())
                {
                    changed = true;
                    obj = null;
                }

                if (text != null)
                        write(text, hint, width);
                  
                

                 changed |=  select(ref obj, list);
                return changed;
            }
        }
        
        public static bool select_or_edit<T>(this string name, ref T obj, List<T> list) where T : UnityEngine.Object
        {
            return select_or_edit(name, null, 0, ref obj, list);
        }

        public static bool select_or_edit<T>(this string name, int width, ref T obj, List<T> list) where T : UnityEngine.Object
        {
            // write(name, width);
            return select_or_edit(name, null, width, ref obj, list);
        }

        public static bool select_or_edit<T>(ref T obj, List<T> list) where T : UnityEngine.Object
        {
            return select_or_edit(null, null, 0, ref obj, list);
        }

        public static bool select_SameClass_or_edit<T, G>(this string text, string hint, int width, ref T obj, List<G> list) where T : UnityEngine.Object where G : class
        {
            if (list == null || list.Count == 0)
                return edit(ref obj);
            else
            {
                bool changed = false;
                if (icon.Delete.Click())
                {
                    changed = true;
                    obj = null;
                }


                if (text != null)
                    write(text, hint, width);

                changed |= select_SameClass(ref obj, list);
                return changed;
            }
        }
        
        public static bool select_SameClass_or_edit<T, G>(ref T obj, List<G> list) where T : UnityEngine.Object where G : class
        {
            return select_SameClass_or_edit(null, null, 0, ref obj, list);
        }

        public static bool select_SameClass_or_edit<T, G>(this string name, ref T obj, List<G> list) where T : UnityEngine.Object where G : class
        {
            return select_SameClass_or_edit(name, null, 0, ref obj, list);
        }
      
        public static bool select_SameClass_or_edit<T, G>(this string name, int width, ref T obj, List<G> list) where T : UnityEngine.Object where G : class
        {
            return select_SameClass_or_edit(name, null, width, ref obj, list);
        }
        
        public static bool select_iGotIndex<T>(this string label, string tip, ref int ind, List<T> lst) where T : IGotIndex
        {
            write(label, tip);

            return select_iGotIndex(ref ind, lst);
        }

        public static bool select_iGotIndex<T>(this string label, string tip, int width, ref int ind, List<T> lst) where T : IGotIndex
        {
            write(label, tip, width);

            return select_iGotIndex(ref ind, lst);
        }

        public static bool select_iGotIndex<T>(this string label, int width, ref int ind, List<T> lst) where T : IGotIndex
        {
            write(label, width);

            return select_iGotIndex(ref ind, lst);
        }

        public static bool select_iGotIndex<T>(this string label, ref int ind, List<T> lst) where T : IGotIndex
        {
            write(label);
            return select_iGotIndex(ref ind, lst);
        }

        public static bool select_iGotIndex<T>(ref int ind, List<T> lst) where T : IGotIndex
        {

            List<string> lnms = new List<string>();
            List<int> indxs = new List<int>();

            int jindx = -1;

            foreach (var el in lst)
            {
                if (el != null)
                {
                    int index = el.GetIndex();

                    if (ind == index)
                        jindx = indxs.Count;
                    lnms.Add(el.ToPEGIstring());
                    indxs.Add(index);

                }
            }

            if (jindx == -1)
                lnms.Add(">>" + ind.ToString() + "<<");

            if (select(ref jindx, lnms.ToArray()) && (jindx < indxs.Count))
            {
                    ind = indxs[jindx];
                    return true;
            }

            return false;
        }

#endregion

#region Foldout    
        public static bool foldout(this string txt, ref bool state)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.foldout(txt, ref state);
            }
            else
#endif

            {

                checkLine();

                if (Click((state ? "..⏵ " : "..⏷ ") + txt))
                    state = !state;


                isFoldedOut = state;

                return isFoldedOut;
            }
        }

        public static bool foldout(this string txt, ref int selected, int current)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.foldout(txt, ref selected, current);
            }
            else
#endif
            {

                checkLine();

                isFoldedOut = (selected == current);

                if (Click((isFoldedOut ? "..⏵ " : "..⏷ ") + txt))
                {
                    if (isFoldedOut)
                        selected = -1;
                    else
                        selected = current;
                }

                isFoldedOut = selected == current;

                return isFoldedOut;
            }
        }

        public static bool foldout(this Texture2D tex, string text, ref int selected, int current)  {

            if (selected == current)
            {
                if (icon.FoldedOut.Click(text, 30))
                    selected = -1;
            }
            else
            {
                if (tex.Click(text, 25))
                    selected = current;
            }
            return selected == current;
        }

        public static bool foldout(this Texture2D tex, string text, ref bool state)
        {

            if (state)
            {
                if (icon.FoldedOut.Click(text, 30))
                    state = false;
            }
            else
            {
                if (tex.Click(text, defaultButtonSize))
                    state = true;
            }
            return state;
        }

        public static bool foldout(this icon ico, string text, ref bool state) => ico.getIcon().foldout(text, ref state);
        
        public static bool foldout(this icon ico, string text, ref int selected, int current)=> ico.getIcon().foldout(text, ref selected, current);
        
        public static bool fold_enter_exit(ref int selected, int current) {

            if (selected == current) {
                if (icon.Exit.Click())
                    selected = -1;
            }
            else if (selected == -1 && icon.Enter.Click())
                selected = current;

            return (selected == current);
        }
        
        public static bool fold_enter_exit(this icon ico, string txt, ref int selected, int current)
        {

            if (selected == current)
            {
                if (icon.Exit.ClickUnfocus(txt))
                    selected = -1;
            }
            else if (selected == -1)
            {
                if (ico.ClickUnfocus(txt))
                    selected = current;
                write(txt);
            }

            return (selected == current);
        }

        public static bool fold_enter_exit(this string txt, ref int selected, int current) => icon.Enter.fold_enter_exit(txt, ref selected, current);
        
        public static bool foldout(this string txt)
        {


#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.foldout(txt);
            }
            else
#endif

            {

                foldout(txt, ref selectedFold, elementIndex);

                elementIndex++;

                return isFoldedOut;
            }

        }

        public static void foldIn() => selectedFold = -1;
        

#endregion

#region BUTTONS
        const int defaultButtonSize = 25;

        public static int selectedTab;
        public static void ClickTab(ref bool open, string text)
        {
            if (open) write("|" + text + "|", 60);
            else if (Click(text, 40)) selectedTab = tabIndex;
            tabIndex++;
        }

        public static bool ClickUnfocus(this string text, int width)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
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

        public static bool ClickUnfocus(this Texture tex, int width)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
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
        
        public static bool ClickUnfocus(this Texture tex, string tip, int width)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
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
            if (paintingPlayAreaGUI == false)
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
            if (paintingPlayAreaGUI == false)
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
            if (paintingPlayAreaGUI == false)
            {
                return ef.Click(text, width);
            }
            else
#endif

            {
                checkLine();
                return GUILayout.Button(text, GUILayout.MaxWidth(width));
            }

        }

        public static bool Click(this string text)
        {


#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.Click(text);
            }
            else
#endif

            {
                checkLine();
                return GUILayout.Button(text);
            }

        }

        public static bool Click(this string text, string tip)
        {


#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.Click(text, tip);
            }
            else
#endif

            {
                checkLine();
                GUIContent cont = new GUIContent() { text = text, tooltip = tip };
                return GUILayout.Button(cont);
            }

        }

        public static bool Click(this string text, string tip, int width)
        {


#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.Click(text, tip, width);
            }
            else
#endif

            {
                checkLine();
                GUIContent cont = new GUIContent() { text = text, tooltip = tip };
                return GUILayout.Button(cont, GUILayout.MaxWidth(width));
            }

        }

        public static bool Click(this Texture img) =>  img.Click(defaultButtonSize);
            
        public static bool Click(this Texture img, string tip) => img.Click(tip, defaultButtonSize);
        
        public static bool Click(this Texture img, int size)
        {
            
#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.Click(img, size);
            }
            else
#endif

            {
                checkLine();
                return GUILayout.Button(img, GUILayout.MaxWidth(size+5), GUILayout.MaxHeight(size));
            }

        }

        public static bool Click(this Texture img, string tip, int size)
        {

            
#if UNITY_EDITOR
                        if (paintingPlayAreaGUI == false)
                        {
                            return ef.Click(img, tip, size);
                        }
                        else
#endif
                        {
                            checkLine();
                            return GUILayout.Button(new GUIContent(img, tip), GUILayout.MaxWidth(size + 5), GUILayout.MaxHeight(size));
                        }

        }

        public static bool Click(this Texture img, string tip, int width, int height)
        {


#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.Click(img, tip, width, height);
            }
            else
#endif
            {
                checkLine();
                return GUILayout.Button(new GUIContent(img, tip), GUILayout.MaxWidth(width), GUILayout.MaxHeight(height));
            }

        }
        
        public static bool Click(this icon icon) => Click(icon.getIcon(), defaultButtonSize);
        
        public static bool ClickUnfocus(this icon icon) =>  ClickUnfocus(icon.getIcon(), defaultButtonSize);
        
        public static bool ClickUnfocus(this icon icon, msg text) => ClickUnfocus(icon.getIcon(), text.Get(), defaultButtonSize);
        
        public static bool ClickUnfocus(this icon icon, string text) => ClickUnfocus(icon.getIcon(), text, defaultButtonSize);
        
        public static bool ClickUnfocus(this icon icon, msg text, int width) => ClickUnfocus(icon.getIcon(), text.Get(), width);
        
        public static bool ClickUnfocus(this icon icon, msg text, int width, int height) => ClickUnfocus(icon.getIcon(), text.Get(), width, height);
        
        public static bool ClickUnfocus(this icon icon, string text, int width, int height) => ClickUnfocus(icon.getIcon(), text, width, height);
        
        public static bool ClickUnfocus(this icon icon, string text, int width) => ClickUnfocus(icon.getIcon(), text, width);
        
        public static bool ClickUnfocus(this icon icon,  int width) => ClickUnfocus(icon.getIcon(), width);
        
        public static bool Click(this icon icon, int size) => Click(icon.getIcon(), size);
        
        public static bool Click(this icon icon, string tip, int width, int height) => Click(icon.getIcon(), tip, width, height);
        
        public static bool Click(this icon icon, string tip, int size)  => Click(icon.getIcon(), tip, size);
        
        public static bool Click(this icon icon, string tip) => Click(icon.getIcon(), tip, defaultButtonSize);
        
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

#endregion
       
#region Toggle

        public static bool toggleInt(ref int val)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
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

        public static bool toggle(ref bool val)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.toggle(ref val);
            }
            else
#endif
            {
                checkLine();
                bool before = val;
                val = GUILayout.Toggle(val, "", GUILayout.MaxWidth(30));
                return (before != val);
            }
        }

        public static bool toggle(ref bool val, string text, int width)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
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
            if (paintingPlayAreaGUI == false)
            {
                return ef.toggle(ref val, text, tip);
            }
            else
#endif
            {
                checkLine();
                bool before = val;
                GUIContent cont = new GUIContent() { text = text, tooltip = tip };
                val = GUILayout.Toggle(val, cont);
                return (before != val);
            }
        }

        public static bool toggle(ref bool val, icon TrueIcon, icon FalseIcon, string tip, int width) => toggle(ref val, TrueIcon.getIcon(), FalseIcon.getIcon(), tip, width);

        public static bool toggle(ref bool val, icon TrueIcon, icon FalseIcon) => toggle(ref val, TrueIcon.getIcon(), FalseIcon.getIcon(), "", defaultButtonSize);
        
        public static bool toggle(ref bool val, Texture2D TrueIcon, Texture2D FalseIcon, string tip, int width)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.toggle(ref val, TrueIcon, FalseIcon, tip, width);
            }
            else
#endif
            {
                checkLine();
                bool before = val;

                if (val)
                {
                    if (Click(TrueIcon, tip, width))
                        val = false;
                }
                else
                {
                    if (Click(FalseIcon, tip, width))
                        val = true;
                }



                return (before != val);
            }


        }

        public static bool toggle(ref bool val, string text, string tip, int width)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                ef.write(text, tip, width);
                return ef.toggle(ref val);
            }
            else
#endif
            {
                checkLine();
                bool before = val;
                GUIContent cont = new GUIContent() { text = text, tooltip = tip };
                val = GUILayout.Toggle(val, cont);
                return (before != val);
            }
        }

        public static bool toggle(int ind, CountlessBool tb)
        {
#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.toggle(ind, tb);
            }
            else
#endif
            {
                bool has = tb[ind];
                if (toggle(ref has))
                {
                    tb.Toggle(ind);
                    return true;
                }
                return false;
            }
        }

        public static bool toggle(this icon img, ref bool val)
        {
            write(img.getIcon(), 25);
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
        
#region edit

        public static bool editKey(ref Dictionary<int, string> dic, int key)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
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
            if (paintingPlayAreaGUI == false)
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

        public static bool edit(this string name, ref AnimationCurve val)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.edit(name, ref val);
            }
            else
#endif
                return false;
        }

        public static bool edit(ref int current, Type type)
        {
            return selectEnum(ref current, type);
        }

        public static bool edit<T>(ref T field) where T : UnityEngine.Object
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.edit(ref field);
            }
            else
#endif
            {
                checkLine();
                write(field == null ? "-no " + typeof(T).ToString() : field.ToPEGIstring());
                return false;
            }

        }

        public static bool edit<T>(ref T field, bool allowDrop) where T : UnityEngine.Object
        {


#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.edit(ref field, allowDrop);
            }
            else
#endif
            {
                checkLine();
                write(field == null ? "-no " + typeof(T).ToString() : field.ToPEGIstring());
                return false;
            }

        }

        public static bool edit(GameObject go)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {

                string name = go.name;
                if (ef.edit(ref name))
                {
                    go.name = name;
                    return true;
                }
                return false;
            }
            else
#endif
            {

                string name = go.name;
                if (edit(ref name))
                {
                    go.name = name;
                    return true;
                }
                return false;
            }

        }

        public static bool edit(ref Vector4 val)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
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
            if (paintingPlayAreaGUI == false)
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

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.edit(ref val);
            }
            else
#endif
            {
                checkLine();
                bool modified = false;
                modified |= "X".edit(ref val.x) | "Y".edit(ref val.y) | "Z".edit(ref val.z);
                return modified;
            }
        }

        public static bool edit(ref Vector2 val)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
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

        public static bool edit(this string label, ref Vector2 val)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
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

        public static bool edit(ref myIntVec2 val)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
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

        public static bool edit(ref myIntVec2 val, int min, int max)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
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

        public static bool edit(ref myIntVec2 val, int min, myIntVec2 max)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
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

        public static bool edit(ref linearColor col)
        {
            Color c = col.ToGamma();
            if (edit(ref c))
            {
                col.From(c);
                return true;
            }
            return false;
        }

        public static bool edit(ref Color col)
        {
#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.edit(ref col);
            }
            else
#endif
            {
                checkLine();
                // Color editing not implemented yet
                return false;
            }
        }

        public static bool edit(ref float val, float min, float max)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
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

        public static bool edit(ref int val)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
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
            if (paintingPlayAreaGUI == false)
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

        public static bool edit(ref float val)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
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

        public static bool edit(this string label, ref float val)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.edit(label, ref val);
            }
            else
#endif
            {
                write(label);
                return edit(ref val);
            }
        }

        public static bool edit(ref int val, int min, int max)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.edit(ref val, (int)min, (int)max);
            }
            else
#endif
            {
                checkLine();
                float before = val;
                //if (edit(ref val))
                //val = Mathf.Clamp(val, min, max);
                val = (int)GUILayout.HorizontalSlider(before, min, max);
                return (before != val);
            }
        }

        public static bool editPOW(ref float val, float min, float max)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
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

        public static bool edit(this Sentance val)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.edit(val);
            }
            else
#endif
            {
                checkLine();
                string before = val.ToString();
                if (edit(ref before))
                {
                    val.setTranslation(before);
                    return true;
                }
                return false;
            }
        }

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

        public static bool editEnum<T>(ref T eval)
        {

            int val = Convert.ToInt32(eval);

            if (selectEnum(ref val, typeof(T)))
            {

                eval = (T)((object)val);
                return true;
            }

            return false;
        }

        public static bool editTexture(this Material mat, string name)
        {
            return mat.editTexture(name, name);
        }

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

        static string editedText;
        static string editedHash = "";
        public static bool editDelayed(ref string val)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
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

        public static bool editDelayed(ref string val, int width)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
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
            write(label, msg.editDelayed_HitEnter.Get(), width);

            return editDelayed(ref val);


        }

        public static bool editDelayed(this string label, ref int val, int width)
        {
            write(label, msg.editDelayed_HitEnter.Get());
            return editDelayed(ref val, width);
        }

        public static bool editDelayed(this string label, int width, ref string val)
        {
            write(label, width);
            return editDelayed(ref val);
        }

        static int editedInteger;
        static int editedIntegerIndex;
        public static bool editDelayed(ref int val, int width)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.editDelayed(ref val, width);
            }
            else
#endif
            {

                checkLine();

                int tmp = val;

                if (KeyCode.Return.IsDown() && (elementIndex == editedIntegerIndex))
                {
                    edit(ref tmp);
                    val = editedInteger;
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

        public static bool edit(ref string val)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.edit(ref val);
            }
            else
#endif
            {
                checkLine();
                string before = val;
                string newval = GUILayout.TextField(before);
                if (String.Compare(before, newval) != 0)
                {
                    //val = newval;
                    return true;
                }
                return false;
            }
        }

        public static bool edit(ref string val, int width)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.edit(ref val, width);
            }
            else
#endif
            {
                checkLine();
                string before = val;
                string newval = GUILayout.TextField(before, GUILayout.MaxWidth(width));
                if (String.Compare(before, newval) != 0)
                {
                    //val = newval;
                    return true;
                }
                return false;
            }
        }

        public static bool editBig(ref string val)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
            {
                return ef.editBig(ref val);
            }
            else
#endif
            {
                checkLine();
                string before = val;
                string newval = GUILayout.TextArea(before);
                if (String.Compare(before, newval) != 0)
                {
                    val = newval;
                    return true;
                }
                return false;
            }
        }

        public static bool editBig(this string name, ref string val)
        {

            write(name);
            nl();
            return editBig(ref val);

        }

        public static bool edit<T>(ref int current)
        {
            return selectEnum(ref current, typeof(T));
        }

        public static bool edit(this string label, ref linearColor col)
        {
            write(label);
            return edit(ref col);
        }

        public static bool edit<T>(this string label, ref T field) where T : UnityEngine.Object
        {
            write(label);
            return edit(ref field);
        }

        public static bool edit<T>(this string label, ref T field, bool allowDrop) where T : UnityEngine.Object
        {
            write(label);
            return edit(ref field, allowDrop);
        }

        public static bool edit<T>(this string label, int width, ref T field) where T : UnityEngine.Object
        {
            write(label, width);
            return edit(ref field);
        }

        public static bool edit<T>(this string label, int width, ref T field, bool allowDrop) where T : UnityEngine.Object
        {
            write(label, width);
            return edit(ref field, allowDrop);
        }

        public static bool edit<T>(this string label, string tip, int width, ref T field) where T : UnityEngine.Object
        {
            write(label, tip, width);
            return edit(ref field);
        }

        public static bool edit<T>(this string label, string tip, int width, ref T field, bool allowDrop) where T : UnityEngine.Object
        {
            write(label, tip, width);
            return edit(ref field, allowDrop);
        }

        public static bool edit(this string label, ref myIntVec2 val)
        {
            write(label);
            nl();
            return edit(ref val);
        }

        public static bool edit(this string label, int width, ref myIntVec2 val)
        {
            write(label, width);
            nl();
            return edit(ref val);
        }

        public static bool edit(this string label, int width, ref myIntVec2 val, int min, int max)
        {
            write(label, width);
            nl();
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, int width, ref myIntVec2 val, int min, myIntVec2 max)
        {
            write(label, width);
            nl();
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, ref Vector3 val)
        {
            write(label);
            nl();
            return edit(ref val);
        }

        public static bool edit(int ind, CountlessInt val)
        {

#if UNITY_EDITOR
            if (paintingPlayAreaGUI == false)
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

        public static bool edit(this string label, ref int val)
        {
            write(label);
            return edit(ref val);
        }

        public static bool edit(this string label, int width, ref float val)
        {
            write(label, width);
            return edit(ref val);
        }

        public static bool edit(this string label, int width, ref float from, ref float to)
        {
            write(label, width);
            bool changed = false;
            if (edit(ref from))
            {
                changed = true;
                to = Mathf.Max(from, to);
            }

            write("-", 10);

            if (edit(ref to))
            {
                from = Mathf.Min(from, to);
                changed = true;
            }

            return changed;
        }

        public static bool edit(this string label, ref float val, float min, float max)
        {
            write(label);
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, ref int val, int min, int max)
        {
            write(label);
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, int width, ref int val, int min, int max)
        {
            write(label, width);
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, string tip, int width, ref int val)
        {
            write(label, tip, width);
            return edit(ref val);
        }

        public static bool edit(this string label, string tip, int width, ref int val, int min, int max)
        {
            write(label, tip, width);
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, int width, ref float val, float min, float max)
        {
            write(label, width);
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, string tip, int width, ref float val, float min, float max)
        {
            write(label, tip, width);
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, int width, ref int val)
        {
            write(label, width);
            return edit(ref val);
        }

        public static bool edit(this string label, string tip, int width, ref float val)
        {
            write(label, tip, width);
            return edit(ref val);
        }

        public static bool edit(this string label, ref string val)
        {
            write(label);
            return edit(ref val);
        }

        public static bool edit(this string label, ref Color col)
        {
            write(label);
            return edit(ref col);
        }

        public static bool edit(this string label, int width, ref Color col)
        {
            write(label, width);
            return edit(ref col);
        }

        public static bool edit(this string label, string tip, int width, ref Color col)
        {
            write(label, tip, width);
            return edit(ref col);
        }

        public static bool edit(this string label, int width, ref Vector2 v2)
        {
            write(label, width);
            return edit(ref v2);
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

        public static int editEnum<T>(T val)
        {
            int ival = Convert.ToInt32(val);
            selectEnum(ref ival, typeof(T));
            return ival;
        }

        public static bool edit<T>(this string label, Expression<Func<T>> memberExpression, UnityEngine.Object obj)
        {
            return edit<T>(label, null, null, -1, memberExpression, obj);
        }

        public static bool edit<T>(this string label, string tip, Expression<Func<T>> memberExpression, UnityEngine.Object obj)
        {
            return edit<T>(label, null, tip, -1, memberExpression, obj);
        }

        public static bool edit<T>(this Texture tex, string tip, Expression<Func<T>> memberExpression, UnityEngine.Object obj)
        {
            return edit<T>(null, tex, tip, -1, memberExpression, obj);
        }

        public static bool edit<T>(this string label, Texture image, string tip, int width, Expression<Func<T>> memberExpression, UnityEngine.Object obj)
        {
            bool changes = false;
#if UNITY_EDITOR

            if (paintingPlayAreaGUI == false)
            {

                SerializedObject sobj = (obj == null ? ef.serObj : GetSerObj(obj)); //new SerializedObject(obj));

                if (sobj != null)
                {

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

#region LISTS

#region List MGMT Functions 

        const int listLabelWidth = 105;
        
        static Dictionary<IList, int> listInspectionIndexes = new Dictionary<IList, int>();
      
        const int UpDownWidth = 120;
        const int UpDownHeight = 30;
        static int SectionSizeOptimal = 0;
        static int ListSectionMax = 0;
        static int ListSectionStartIndex = 0;
        static CountlessInt ListSectionOptimal = new CountlessInt();

        static void SetOptimalSectionFor (int Count)
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
                
                for (int i=listShowMax-2; i< listShowMax+2; i++)
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
        static string addingNewNameHolder = "New Name";
        public static bool PEGI_InstantiateOptions_SO<T>(this List<T> lst, ref T added) where T : ScriptableObject
        {
            if (editingOrder != null && editingOrder == lst)
                return false;

            bool changed = false;

            var types = typeof(T).TryGetDerrivedClasses();

            pegi.edit(ref addingNewNameHolder);

            if (addingNewNameHolder.Length > 1)
            {
                if (types == null)
                {
                    if (icon.Create.Click("Create new object").nl())
                    {
                        added = lst.CreateAsset_SO<T>("Assets/ScriptableObjects/", addingNewNameHolder);
                        changed = true;
                    }
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
                        foreach (var t in types)
                        {
                            var n = t.ToString();
                            pegi.write(n.Substring(Mathf.Max(0, n.LastIndexOf("."))));
                            if (icon.Create.Click().nl())
                            {
                                added = lst.CreateAsset_SO("Assets/ScriptableObjects/", addingNewNameHolder, t);
                                changed = true;
                            }
                        }
                }
            }
            pegi.nl();

            return changed;

        }

        public static bool PEGI_InstantiateOptions<T>(this List<T> lst, ref T added) where T : new()
        {
            if (editingOrder != null && editingOrder == lst)
                return false;
            
            bool changed = false;

            var types = typeof(T).TryGetDerrivedClasses();
            
            pegi.edit(ref addingNewNameHolder);

            if (addingNewNameHolder.Length > 1)
            {
                if (types == null)
                {
                    if (icon.Create.Click("Instantiate a new object").nl())
                    {
                        added = new T();
                        lst.Add(added);
                        changed = true;
                    }
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
                        foreach (var t in types)
                        {
                            var n = t.ToString();
                            pegi.write(n.Substring(Mathf.Max(0, n.LastIndexOf("."))));
                            if (icon.Create.Click().nl())
                            {
                                added = (T)Activator.CreateInstance(t);
                                lst.Add(added);
                                changed = true;
                            }
                        }
                    }
                }
            }
            pegi.nl();

            if (changed && added != null)
            {
                var nm = added as IGotName;
                if (nm != null)
                    nm.NameForPEGI = addingNewNameHolder;
            }

            return changed;
        }

        public static bool PEGI_InstantiateOptions<T>(this List<T> lst) where T : new()
        {
            T tmp = default(T);
            return lst.PEGI_InstantiateOptions(ref tmp);

        }
        
        static void InspectionStart<T> (this List<T> list)  {

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

                    while (ListSectionStartIndex > 0 && ListSectionStartIndex >= ListSectionMax) { changed = true; ListSectionStartIndex = Mathf.Max(0, ListSectionStartIndex - SectionSizeOptimal); }

                    nl();
                    if (ListSectionStartIndex > 0)
                    {
                        if (icon.Up.ClickUnfocus("To previous elements of the list. ", UpDownWidth, UpDownHeight))
                        {
                            ListSectionStartIndex = Mathf.Max(0, ListSectionStartIndex - SectionSizeOptimal);
                            changed = true;
                        }
                    }
                    else
                        icon.UpLast.write("Is the first section of the list." ,UpDownWidth, UpDownHeight);
                    nl();

                    if (changed)
                        listInspectionIndexes[list] = ListSectionStartIndex;
                }
                else  Line(Color.gray);


                ListSectionMax = Mathf.Min(ListSectionMax, ListSectionStartIndex + SectionSizeOptimal);
            }
            else if (list.Count > 0)
                Line(Color.gray);

            nl();

        }
        
        static bool InspectionEnd<T>(this List<T> list) 
        {

            var cnt = list.Count;

            if (cnt < SectionSizeOptimal * 2)
                return false;

            if (cnt > SectionSizeOptimal)
            {
            
                nl();

                if (cnt > ListSectionStartIndex + SectionSizeOptimal) {
                    if (icon.Down.ClickUnfocus("To next elements of the list. ", UpDownWidth, UpDownHeight)) {
                        ListSectionStartIndex += SectionSizeOptimal;
                        listInspectionIndexes[list] = ListSectionStartIndex;
                    } 
                }
                else
                    icon.DownLast.write("Is the last section of the list. ", UpDownWidth, UpDownHeight);
               
            } else if (list.Count > 0)
                Line(Color.gray);

            return false;
        }

        static void write_ListLabel(this string label, IList lst) =>
        
           /* if (lst == editingOrder)
                write(label, listLabelWidth);
            else*/
                write(label, heading);
        
        static bool ExitOrDrawPEGI<T>(this List<T> list, ref int index)
        {
            bool changed = false;
           

            if (icon.List.ClickUnfocus(msg.ReturnToListView.Get()).nl())
            {
                changed = true;
                index = -1;
            }
            else
                changed |= list[index].Try_Nested_Inspect();
                        
            return changed;
        }

        static IList editingOrder;
        
        static bool list_DropOption<T> (this List<T> list) where T: UnityEngine.Object
        {
            bool changed = false;
#if UNITY_EDITOR
           
            foreach (var ret in ef.DropAreaGUI<T>()) {
                list.Add(ret);
                changed = true;
            }
#endif
            return changed;
        }
        
        static bool edit_List_Order<T>(this List<T> list, bool allowDelete) { //, UnnullableSTD<ElementData> datas ) {
            bool changed = false;
            
            const int bttnWidth = 25;

            if (list != editingOrder) {
                if (icon.Edit.Click("Change Order", 28))
                    editingOrder = list;
            } else {
                if (icon.Done.Click("Finish moving", 28))
                    editingOrder = null;
            }

            if (list == editingOrder)
            {
                list.InspectionStart();
                for (int i = ListSectionStartIndex; i < ListSectionMax; i++)
                {
                    if (i > 0)
                    {
                        if (icon.Up.Click("Move up", bttnWidth))
                        {
                            changed = true;
                            list.Swap(i - 1);
                        }
                    }
                    else
                        icon.UpLast.write("Last", bttnWidth);

                    if (i < list.Count - 1) {
                        if (icon.Down.Click("Move down",bttnWidth))
                        {
                            changed = true;
                            list.Swap(i);
                        }
                    }
                    else icon.DownLast.write(bttnWidth);

                    var el = list[i];

                    if (allowDelete)
                    {
                        if (el != null && typeof(T).IsUnityObject())
                        {
                            if (icon.Delete.ClickUnfocus(msg.MakeElementNull, bttnWidth))
                                list[i] = default(T);
                        }
                        else
                        {
                            if (icon.Close.ClickUnfocus(msg.RemoveFromList, bttnWidth))
                            {
                                list.RemoveAt(i);
                                changed = true;
                                i--;
                                ListSectionMax--;
                            }
                        }
                    }

                    if (el != null)
                        write(el.ToPEGIstring());
                    else
                        ("Empty " + typeof(T).ToPEGIstring()).write();
                    
                    nl();
                }
                list.InspectionEnd().nl();

            }
            return changed;
        }

        public static bool Name_ClickInspect_PEGI<T>(this object el, List<T> list, int index, ref int edited, UnnullableSTD<ElementData> datas)
        {

            bool changed = false;

            var pl = el as IPEGI_ListInspect;
            
            var uo = el as UnityEngine.Object;

            var need = el as INeedAttention;
            string warningText = need?.NeedAttention();

            if (pl == null)
            {
                var pg = el as IPEGI;

                var named = el as IGotName;
                if (named != null)
                {
                    var so = uo != null ? el as ScriptableObject : null;
                    var n = named.NameForPEGI;
                    if (so)
                    {
                        if (editDelayed(ref n, 120))
                        {
                            so.RenameAsset(n);
                            named.NameForPEGI = n;
                        }
                    }
                    else
                        if (edit(ref n, 120))
                        named.NameForPEGI = n;


                }
                else
                    write(el.ToPEGIstring(), 120 + defaultButtonSize*((uo == null ? 1 : 0)  + (pg == null ? 1 : 0) + (datas == null ? 1: 0)));

                
                if (pg != null)
                {
                    if ((warningText == null && icon.Enter.ClickUnfocus(msg.InspectElement)) || (warningText != null && icon.Warning.ClickUnfocus(warningText)))
                        edited = index;
                    warningText = null;
                }
            }
            else
            {
                changed |= pl.PEGI_inList(list, index, ref edited);
                if (changed || PEGI_Extensions.EfChanges)
                    pl.SetToDirty();
            }

            if (warningText != null)
                icon.Warning.write(warningText, 25);

            if (datas != null)
            {
                var std = el as iSTD;
                if ((datas.GetIfExists(index) != null ? icon.Save : icon.SaveAsNew).Click("Save guid, name " + (std != null ? "configuration." : "."), 25, 25))
                    datas.SaveElementDataFrom(list, index);

                var dta = ExtensionsForGenericCountless.GetIfExists(datas, index);
                if (std != null && dta != null && dta.std_dta != null && icon.Load.Click("Load STD", 25, 25))
                    dta.std_dta.DecodeInto(std);

            }

            uo.clickHighlight();

            return changed;
        }

        public static bool clickHighlight(this UnityEngine.Object obj)
        {
#if UNITY_EDITOR
            if (obj != null && icon.Search.Click(msg.HighlightElement.Get()))
            {
                EditorGUIUtility.PingObject(obj);
                return true;
            }
#endif

            return false;
        }

        public static bool TryClickHighlight(this object obj)
        {
#if UNITY_EDITOR
            var uo = obj as UnityEngine.Object;
            if (uo != null)
                uo.clickHighlight();
#endif

            return false;
        }

        static bool isMonoType<T>(List<T> list, int i)
        {
            if ((typeof(MonoBehaviour)).IsAssignableFrom(typeof(T)))
            {
                GameObject mb = null;
                if (edit(ref mb))
                {
                    list[i] = mb.GetComponent<T>();
                    if (list[i] == null) (typeof(T).ToString() + " Component not found").showNotification();

                }
                return true;

            }
            return false;
        }

        static bool ListAddClick<T>(this List<T> list, ref T added) where T : new()
        {

            if (!typeof(T).IsUnityObject() && typeof(T).ClassAttribute<DerrivedListAttribute>() != null)
                return false;

            if (icon.Add.ClickUnfocus(msg.AddListElement.Get()))
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

        static bool ListAddClick<T>(this List<T> list)
        {

            if (typeof(T).IsUnityObject() && typeof(T).ClassAttribute<DerrivedListAttribute>() != null)
                return false;

            if (icon.Add.ClickUnfocus(msg.AddListElement.Get()))
            {
                list.Add(default(T));
                return true;
            }
            return false;
        }


#endregion

        //Lists ...... of Monobehaviour
        public static bool edit_List_MB<T>(this string label, List<T> list, ref int edited, bool allowDelete, ref T added, UnnullableSTD<ElementData> datas) where T : MonoBehaviour
        {
            label.write_ListLabel(list);
            return list.edit_List_MB(ref edited, allowDelete, ref added, datas);
        }

        public static bool edit_List_MB<T>(this List<T> list, ref int edited, bool allowDelete, ref T added, UnnullableSTD<ElementData> datas) where T : MonoBehaviour
        {
            bool changed = false;

            added = default(T);

            int before = edited;
            edited = Mathf.Clamp(edited, -1, list.Count - 1);
            changed |= (edited != before);

            if (edited == -1)
            {
                changed |= list.ListAddClick<T>();

                changed |= list.edit_List_Order(allowDelete);

                if (list != editingOrder)
                {
                    list.InspectionStart();
                    for (int i = ListSectionStartIndex; i < ListSectionMax; i++)
                    {
                        
                        var el = list[i];
                        if (el == null)
                        {
                            T obj = null;

                            if (ExtensionsForGenericCountless.GetIfExists(datas, i).edit(ref obj))
                            {
                                if (obj)
                                {
                                    list[i] = obj.GetComponent<T>();
                                    if (list[i] == null) (typeof(T).ToString() + " Component not found").showNotification();
                                }
                            }
                        }
                        else
                        {
                            changed |= el.Name_ClickInspect_PEGI(list, i, ref edited, datas);
                            //el.clickHighlight();
                        }
                        newLine();
                    }
                    list.InspectionEnd().nl();
                }
                else
                    list.list_DropOption();
                
            }
            else changed |= list.ExitOrDrawPEGI(ref edited);
      
            newLine();

            return changed;
        }

        // ...... of Scriptable Object

        public static T edit_List_SO<T>(this string label, List<T> list, ref int edited, ref bool changed) where T : ScriptableObject
        {
            label.write_ListLabel(list);

            return list.edit_List_SO(ref edited, ref changed, null);
        }

        public static bool edit_List_SO<T>(this List<T> list, ref int edited) where T : ScriptableObject
        {
            bool changed = false;

            list.edit_List_SO<T>(ref edited, ref changed, null);

            return changed;
        }

        public static bool edit_List_SO<T>(this string label, List<T> list, ref int edited) where T : ScriptableObject
        {
            label.write_ListLabel(list);

            bool changed = false;

            list.edit_List_SO<T>(ref edited, ref changed, null);

            return changed;
        }

        public static bool edit_List_SO<T>(this string label, List<T> list, ref int edited, UnnullableSTD<ElementData> datas) where T : ScriptableObject
        {
            label.write_ListLabel(list);

            bool changed = false;

            list.edit_List_SO<T>(ref edited, ref changed, datas);

            return changed;
        }
        
        public static T edit_List_SO<T>(this List<T> list, ref int edited, ref bool changed, UnnullableSTD<ElementData> datas) where T : ScriptableObject
        {
            T added = default(T);

            int before = edited;
            edited = Mathf.Clamp(edited, -1, list.Count - 1);
            changed |= (edited != before);

            if (edited == -1)
            {

                changed |= list.ListAddClick<T>();

                changed |= list.edit_List_Order(true);

                if (list != editingOrder)
                {
                    list.InspectionStart();
                    for (int i = ListSectionStartIndex; i < ListSectionMax; i++)
                    {
                       
                            var el = list[i];
                            if (el == null)
                            {
                                if (ExtensionsForGenericCountless.GetIfExists(datas, i).edit(ref el))
                                    list[i] = el;

                            }
                            else
                            {

                                changed |= el.Name_ClickInspect_PEGI<T>(list, i, ref edited, datas);
                            
#if UNITY_EDITOR
                                var path = AssetDatabase.GetAssetPath(el);

                                if (path != null && path.Length > 0)
                                {
                                  //  el.clickHighlight();
                                   /* if (icon.Search.Click())
                                        EditorGUIUtility.PingObject(list[i]);*/// Selection.activeObject = list[i];

                                    if (icon.Copy.Click())
                                    {

                                        added = el.DuplicateScriptableObject();

                                        list.Insert(i + 1, added);

                                        var indx = added as IGotIndex;

                                        if (indx != null)
                                        {
                                            int max = 0;

                                            foreach (var eee in list)
                                                if (eee != null)
                                                {
                                                    var eeind = eee as IGotIndex;
                                                    if (eeind != null)
                                                        max = Math.Max(max, eeind.GetIndex() + 1);
                                                }

                                            indx.SetIndex(max);
                                        }

                                    }
                                }
#endif
                            }

                            
                        newLine();
                    }
                    list.InspectionEnd();

                    if (typeof(T).TryGetDerrivedClasses() != null)
                        list.PEGI_InstantiateOptions_SO(ref added);

                    nl();

                }
                else list.list_DropOption();
            }
            else changed |= list.ExitOrDrawPEGI(ref edited);

            newLine();
            return added;
        }

        // ...... of Object

        public static bool edit_List_Obj<T>(this List<T> list, ref int edited) where T : UnityEngine.Object
            => list.edit_or_select_List_Obj(null, ref edited, true, null);
        
        public static bool edit_List_Obj<T>(this List<T> list, bool allowDelete) where T : UnityEngine.Object
        {
            int edited = -1;
            return list.edit_or_select_List_Obj(null, ref edited, allowDelete, null);
        }
        
        public static bool edit_List_Obj<T>(this string label, List<T> list, bool allowDelete) where T : UnityEngine.Object
        {
            label.write_ListLabel(list);
            return (list.edit_List_Obj(allowDelete));
        }
        
        public static bool edit_List_Obj<T>(this string label, List<T> list, ref int edited) where T : UnityEngine.Object
        {
            label.write_ListLabel(list);
            return list.edit_List_Obj(ref edited);
        }

        public static bool edit_List_Obj<T>(this string label, List<T> list, ref int edited, UnnullableSTD<ElementData> datas) where T : UnityEngine.Object
        {
            label.write_ListLabel(list);
            return list.edit_or_select_List_Obj(null, ref edited, true, datas);
        }

        public static bool edit_or_select_List_Obj<T>(this List<T> list, List<T> from, ref int edited, bool allowDelete, UnnullableSTD<ElementData> datas) where T : UnityEngine.Object
        {

            bool changed = false;

            int before = edited;
            edited = Mathf.Clamp(edited, -1, list.Count - 1);
            changed |= (edited != before);

            if (edited == -1)
            {
                changed |= list.ListAddClick<T>();

                changed |= list.edit_List_Order(allowDelete);

                if (list != editingOrder)
                {
                    list.InspectionStart();
                    for (int i = ListSectionStartIndex; i < ListSectionMax; i++)
                    {
                       
                            var el = list[i];
                            if (el == null)
                            {

                                if (from != null && from.Count > 0)
                                {
                                    if (select(ref el, from))
                                        list[i] = el;
                                }
                                
                               // if (!isMonoType<T>(list, i))
                               // {
                                   // UnityEngine.Object so = null;
                                    if (datas.GetIfExists(i).edit(ref el))
                                        list[i] = el;
                               // }
                            }
                            else
                            {

                                changed |= list[i].Name_ClickInspect_PEGI(list, i, ref edited, datas);

                              //  list[i].clickHighlight();
                            }
                            
                        newLine();
                    }
                    list.InspectionEnd().nl();
                }
                else list.list_DropOption();
            }
            else changed |= list.ExitOrDrawPEGI(ref edited);
            newLine();
            return changed;

        }


        // ...... of New()

        public static bool edit_List<T>(this string label, List<T> list, ref int edited, bool allowDelete) where T : new()
        {
            label.write_ListLabel(list);
            return list.edit_List(ref edited, allowDelete);
        }

        public static bool edit_List<T>(this List<T> list, ref int edited, bool allowDelete) where T : new() {
            bool changes = false;
                list.edit_List(ref edited, allowDelete, ref changes);
            return changes;
        }

        public static bool edit_List<T>(this List<T> list, bool allowDelete) where T : new()
        {
            int edited = -1;
            bool changes = false;
            list.edit_List(ref edited, allowDelete, ref changes);
            return changes;
        }

        public static T edit_List<T>(this string label, List<T> list, ref int edited, bool allowDelete, ref bool changed) where T : new()
        {
            label.write_ListLabel(list);
            return list.edit_List(ref edited, allowDelete, ref changed);
        }

        public static T edit_List<T>(this List<T> list, ref int edited, bool allowDelete, ref bool changed) where T : new() {
            T added = default(T);

            int before = edited;
            edited = Mathf.Clamp(edited, -1, list.Count - 1);
            changed |= (edited != before);

            if (edited == -1)
            {
               
                changed |= list.edit_List_Order(allowDelete);

                if (list != editingOrder)
                {
                    changed |= list.ListAddClick<T>(ref added);
                    list.InspectionStart();
                    for (int i = ListSectionStartIndex; i < ListSectionMax; i++)
                    {

                        var el = list[i];
                        if (el == null)
                        {
                            if (!isMonoType<T>(list, i))
                            {
                                if (typeof(T).IsSubclassOf(typeof(UnityEngine.Object)))
                                    write("use edit_List_Obj");
                                else
                                    write("is NUll");
                            }
                        }
                        else
                            changed |= list[i].Name_ClickInspect_PEGI(list, i, ref edited, null);
                        
                        newLine();
                    }
                    list.InspectionEnd();

                   if (typeof(T).TryGetDerrivedClasses() != null)
                        list.PEGI_InstantiateOptions(ref added);

                    nl();
                }
            }
            else changed |= list.ExitOrDrawPEGI(ref edited);


            newLine();
            return added;
        }

        // ...... of not New
        public static bool write_List<T>(this List<T> list, ref int edited) 
        {
            bool changed = false;

            int before = edited;
            edited = Mathf.Clamp(edited, -1, list.Count - 1);
            changed |= (edited != before);

            if (edited == -1)
            {
                for (int i = 0; i < list.Count; i++)
                {

                    var el = list[i];
                    if (el == null)
                        write("NULL");
                    else
                    {

                        changed |= list[i].Name_ClickInspect_PEGI(list, i, ref edited, null);

                      //  (list[i] as UnityEngine.Object).clickHighlight();
                    }

                    newLine();
                }

            }
            else
                changed |= list.ExitOrDrawPEGI(ref edited);


            newLine();
            return changed;
        }

        public static bool edit<G, T>(this Dictionary<G, T> dic, ref int edited, bool allowDelete)
        {
            bool changed = false;

            int before = edited;
            edited = Mathf.Clamp(edited, -1, dic.Count - 1);
            changed |= (edited != before);

            if (edited == -1)
            {
                for (int i = 0; i < dic.Count; i++)
                {
                    var item = dic.ElementAt(i);
                    var itemKey = item.Key;
                    if (allowDelete && icon.Delete.Click(25))
                    {
                        dic.Remove(itemKey);
                        changed = true;
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
                            {
                                named.NameForPEGI = n;
                            }
                        }
                        else
                            write(el.ToPEGIstring(), 120);

                        if ((el is IPEGI) && icon.Enter.ClickUnfocus(msg.InspectElement, 25))
                            edited = i;

                    }

                    newLine();
                }

            }
            else
            {
                if (icon.Back.Click(25).nl())
                {
                    changed = true;
                    edited = -1;
                }
                else
                    changed |= dic.ElementAt(edited).Value.Try_Nested_Inspect();

            }

            newLine();
            return changed;
        }

        // ....... of Countless
        
#endregion
        
#region Transform
        static bool _editLocalSpace = false;
        public static bool PEGI_CopyPaste(this Transform tf, ref bool editLocalSpace)
        {
            bool changed = false;

            changed |= "Local".toggle(40, ref editLocalSpace);

            if (icon.Copy.Click("Copy Transform"))
            {
                STDExtensions.copyBufferValue  = tf.Encode(editLocalSpace).ToString();
                changed = true;
            }

            if (STDExtensions.copyBufferValue != null && icon.Paste.Click("Paste Transform"))
            {
                STDExtensions.copyBufferValue.DecodeInto(tf);
                STDExtensions.copyBufferValue = null;
                changed = true;
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
        

        public static bool inspect_Name(this IGotName obj) {
            var n = obj.NameForPEGI;
            if ("Name:".edit(40, ref n))
            {
                obj.NameForPEGI = n;
                return true;
            }
            return false;
        }

        public static bool select_or_Edit_PEGI(this Dictionary<int, string> dic, ref int selected)
        {
            bool changed = false;

            if (editedDic != dic)
            {
                changed |= select(ref selected, dic);
                if (icon.Add.Click(20))
                {
                    editedDic = dic;
                    changed = true;
                    SetNewKeyToMax(dic);
                }
            }
            else
            {
                if (icon.Close.Click(20)) { editedDic = null; changed = true; }
                else
                    changed |= dic.newElement_PEGI();

            }

            return changed;
        }
        
        public static Dictionary<int, string> editedDic;

        public static string newEnumName = "UNNAMED";
        public static int newEnumKey = 1;
        public static bool edit_PEGI(this Dictionary<int, string> dic)
        {

            bool changed = false;

            newLine();

            for (int i = 0; i < dic.Count; i++)
            {

                var e = dic.ElementAt(i);
                if (icon.Delete.Click(20))
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

            changed |= dic.newElement_PEGI();

            return changed;
        }

        public static bool newElement_PEGI(this Dictionary<int, string> dic)
        {
            bool changed = false;
            newLine();
            write("______New [Key, Value]");
            newLine();
            changed |= edit(ref newEnumKey); changed |= edit(ref newEnumName);
            string dummy;
            bool isNewIndex = !dic.TryGetValue(newEnumKey, out dummy);
            bool isNewValue = !dic.ContainsValue(newEnumName);

            if ((isNewIndex) && (isNewValue) && (icon.Add.Click("Add Element", 25)))
            {
                dic.Add(newEnumKey, newEnumName);
                changed = true;
                SetNewKeyToMax(dic);
                newEnumName = "UNNAMED";
            }

            if (!isNewIndex)
                write("Index Takken by " + dummy);
            else if (!isNewValue)
                write("Value already assigned ");

            newLine();

            return changed;
        }
        public static void SetNewKeyToMax(Dictionary<int, string> dic)
        {
            newEnumKey = 1;
            string dummy;
            while (dic.TryGetValue(newEnumKey, out dummy)) newEnumKey++;
        }

#endregion

        /*
        class TypeSwitch
        {
            Dictionary<Type, Action<object>> matches = new Dictionary<Type, Action<object>>();
            public TypeSwitch Case<T>(Action<T> action) { matches.Add(typeof(T), (x) => action((T)x)); return this; }
            public void Switch(object x, string name) { matches[x.GetType()](x); }
        }*/

        /*
                static List<MonoBehaviour> editorSubscribedMb = new List<MonoBehaviour>();
#if UNITY_EDITOR

                public static bool SubscribeToEditorUpdate_PEGI<T>(this T mb, EditorApplication.CallbackFunction myMethodName) where T : MonoBehaviour
                {

                    if (!editorSubscribedMb.Contains(mb))
                    {
                        if (icon.Play.Click())
                        {
                            EditorApplication.update += myMethodName;
                            editorSubscribedMb.Add(mb);
                            return true;
                        }

                    }
                    else
                    {
                        if ("Stop Updates".Click())
                        {
                            EditorApplication.update -= myMethodName;
                            editorSubscribedMb.Remove(mb);
                            return true;
                        }

                    }




                    return false;
                }
#else
                public static bool SubscribeToEditorUpdate_PEGI<T>(this T mb, Action myMethodName) where T : MonoBehaviour
                {
                    return false;
                }
#endif

                public static bool isSubscribedToEditorUpdates(this MonoBehaviour mb)
                {
                    return editorSubscribedMb.Contains(mb);
                }

            }*/
    }
    

#region Extensions
    public static class PEGI_Extensions
    {

        public static string ToPEGIstring(this object obj)
        {

            if (obj == null) return "NULL";

            if (typeof(UnityEngine.Object).IsAssignableFrom(obj.GetType()))
            {
                var uobj = obj as UnityEngine.Object;
                if (uobj == null)
                    return "NULL Object";
            }

#if PEGI
            var dn = obj as IGotDisplayName;
            if (dn != null)
                return dn.NameForPEGIdisplay();

            var sn = obj as IGotName;
            if (sn != null)
                return sn.NameForPEGI;
#endif

            return obj.ToString();
        }

        public static string ToPEGIstring(this Type type)
        {
            var name = type.ToString();
            int ind = name.LastIndexOf(".");
            return ind == -1 ? name : name.Substring(ind+1);
        }

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

        public static bool Inspect<T>(this T o, object so) where T : MonoBehaviour, IPEGI
        {
#if UNITY_EDITOR
            return ef.Inspect(o, (SerializedObject)so);
#else
             "PEGI is compiled without UNITY_EDITOR directive".nl();
                return false;
#endif
        }

        public static bool Inspect_so<T>(this T o, object so) where T : ScriptableObject, IPEGI
        {
#if UNITY_EDITOR
            return ef.Inspect_so(o, (SerializedObject)so);
#else
             "PEGI is compiled without UNITY_EDITOR directive".nl();
                return false;
#endif
        }

        public static bool Nested_Inspect(this IPEGI pgi)
        {
            if (pgi == null)
                return false;

            if (pgi != null)
            {
                var changes = pgi.PEGI();

                if (changes || EfChanges)
                    pgi.SetToDirty();

                return changes;
            }
            return false;
        }

        public static bool Try_Nested_Inspect(this object other)
        {
            if (other == null)
                return false;

            var pgi = other as IPEGI;

            return pgi != null ? pgi.Nested_Inspect() : false;
        }
#endregion

#region Element Data

        public static void SaveElementDataFrom<T>(this UnnullableSTD<ElementData> datas, List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
                datas.SaveElementDataFrom(list, i);
        }

        public static void SaveElementDataFrom<T>(this UnnullableSTD<ElementData> datas, List<T> list, int i)
        {
            var el = list[i];
            if (el != null)
            {
                var dta = datas[i];

                dta.name = el.ToPEGIstring();

                var std = el as iSTD;
                if (std != null)
                    dta.std_dta = std.Encode().ToString();
                
                dta.guid = (el as UnityEngine.Object).GetGUID(dta.guid);


            }
        }

        public static bool edit<T>(this ElementData dta, ref T field) where T : UnityEngine.Object
        {

            bool changed = false;

            if (field == null && dta != null)
            {

                changed |= dta.name.edit(100, ref field);
#if UNITY_EDITOR
                if (dta.guid != null && icon.Search.Click("Find Object by guid").nl()) {
                    var obj = UnityHelperFunctions.GUIDtoAsset<T>(dta.guid);

                    if (obj)
                    {
                        field = obj;
                        changed = true;
                    }
                    else
                        (typeof(T).ToString() + " Not found ").showNotification();
                }
#endif
            }
            else
                changed |= pegi.edit(ref field);

            return changed;
        }

       
    }
#endregion


}
#pragma warning restore IDE1006 // Naming Styles
#endif


