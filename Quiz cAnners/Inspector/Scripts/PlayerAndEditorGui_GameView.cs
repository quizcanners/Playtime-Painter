using QuizCanners.Utils;
using UnityEngine;

namespace QuizCanners.Inspect
{
    public static partial class pegi
    {
        public static class GameView
        {

            private static System.Type _gameViewType;
            private static int _mouseOverUi = -1;

            public static void ShowNotification(string text)
            {
#if UNITY_EDITOR

                if (Application.isPlaying)
                {
                    if (_gameViewType == null)
                        _gameViewType = typeof(EditorView).Assembly.GetType("UnityEditor.GameView");

                    if (_gameViewType == null)
                    {
                        //Debug.LogError(" text [Couldn't find GameView class to show in gameView Window]");

                        /*var result = new List<Type>();
                        System.Reflection.Assembly[] AS = System.AppDomain.CurrentDomain.GetAssemblies();
                        Type editorWindow = typeof(EditorWindow);
                        foreach (var A in AS)
                        {
                            System.Type[] types = A.GetTypes();
                            foreach (var T in types)
                            {
                                if (T.IsSubclassOf(editorWindow))
                                    Debug.Log(T.ToString()); //result.Add(T);
                            }
                        }*/
                    }
                    else
                    {

                        var ed = UnityEditor.EditorWindow.GetWindow(_gameViewType);
                        if (ed != null)
                            ed.ShowNotification(new GUIContent(text));
                    }
                }
                else
                {
                    var lst = Resources.FindObjectsOfTypeAll<UnityEditor.SceneView>();

                    foreach (var w in lst)
                        w.ShowNotification(new GUIContent(text));

                }
#endif
            }

            public static bool MouseOverUI
            {
                get { return _mouseOverUi >= Time.frameCount - 1; }
                set
                {
                    if (value) _mouseOverUi = Time.frameCount;
                }
            }

            public delegate void WindowFunction();

            public class Window
            {
                public float Upscale;

                private WindowFunction _function;
                private Rect _windowRect;
                private Vector2 _scrollPosition;

                protected bool UseWindow => Mathf.Approximately(Upscale, 1);
                private void DrawFunctionWrapper(int windowID)
                {
                    PaintingGameViewUI = true;
                    ef.globChanged = false;
                    _elementIndex = 0;
                    _lineOpen = false;

                    try
                    {
                        if (!UseWindow)
                        {
                            GUI.matrix = Matrix4x4.TRS(new Vector3(0, 0, 0), Quaternion.identity,
                                new Vector3(Upscale, Upscale, 1));
                            GUILayout.BeginArea(new Rect(40 / Upscale, 20 / Upscale, Screen.width / Upscale,
                                Screen.height / Upscale));
                        }

                        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition
                            , GUILayout.Width(Screen.width * 0.9f / Upscale)
                            , GUILayout.Height(Screen.height * 0.9f / Upscale));

                        if (!FullWindow.ShowingPopup())
                            _function();

                        nl();

                        UnIndent();

                        (GUI.tooltip.IsNullOrEmpty() ? "" : "{0}:{1}".F(Msg.ToolTip.GetText(), GUI.tooltip)).nl(
                            PEGI_Styles.HintText);

                        GUILayout.EndScrollView();

                        if (UseWindow)
                        {
                            if (_windowRect.Contains(Input.mousePosition))
                                MouseOverUI = true;

                            GUI.DragWindow(new Rect(0, 0, 3000, 40 * Upscale));
                        }
                        else
                        {
                            MouseOverUI = true;
                            GUILayout.EndArea();
                        }

                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogException(ex);
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
                            GUILayout.MaxWidth(360 * Upscale), GUILayout.ExpandWidth(true));
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
                    this.Upscale = upscale;
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
                    return UnityEditor.Handles.GetMainGameViewSize();
#else
                    return new Vector2(Screen.width, Screen.height);
#endif
                }
            }
        }


    }
}
