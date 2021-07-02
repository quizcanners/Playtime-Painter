using System.Collections.Generic;
using QuizCanners.Utils;
using UnityEngine;

using CultureInfo = System.Globalization.CultureInfo;
using Object = UnityEngine.Object;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0011 // Add braces
#pragma warning disable IDE0008 // Use explicit type
#pragma warning disable IDE0075 // Simplify conditional expression

namespace QuizCanners.Inspect
{
    public static partial class pegi
    {
        #region Changes 

        public class ChangesToken 
        {
            private bool _wasAlreadyChanged;
            public bool Changed 
            {
                get => !_wasAlreadyChanged && ef.globChanged;
                set 
                {
                    if (value)
                    {
                        _wasAlreadyChanged = false;
                        ef.globChanged = true;
                    }

                    if (!value) 
                    {
                        _wasAlreadyChanged = true;
                    }
                }
            }

            public static implicit operator bool(ChangesToken me) => me.Changed;
            
            internal ChangesToken() 
            {
                _wasAlreadyChanged = ef.globChanged;
            }
        }

        public static ChangesToken ChangeTrackStart() => new ChangesToken();
        
        private static bool changes_Internal(this bool value, ref bool changed)
        {
            changed |= value;
            return value;
        }
        
        private static bool SetChangedTrue_Internal { get { ef.globChanged = true; return true; } }

        private static bool FeedChanges_Internal(this bool val) { ef.globChanged |= val; return val; }

        private static bool ignoreChanges(this bool changed)
        {
            if (changed)
                ef.globChanged = false;
            return changed;
        }

        private static bool wasChangedBefore;

        private static void _START()
        {
            checkLine();
            wasChangedBefore = GUI.changed;
        }

        private static bool _END() => (GUI.changed && !wasChangedBefore).FeedChanges_Internal();

        #endregion

        #region Toggle
        private const int DefaultToggleIconSize = 34;

        public static bool toggleInt(ref int val)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
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
                icon.draw(label);
            else if (icon.Click(label))
            {
                selected = current;
                return true;
            }

            return false;
        }

        public static bool toggle(ref bool val)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.toggle(ref val);
#endif

            _START();
            val = GUILayout.Toggle(val, "", GUILayout.MaxWidth(30));
            return _END();

        }

        public static bool toggle(ref bool val, string text, int width)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                ef.write(text, width);
                return ef.toggle(ref val);
            }
#endif

            _START();
            val = GUILayout.Toggle(val, text, GuiMaxWidthOption);
            return _END();

        }

        public static bool toggle(ref bool val, string text, string tip)
        {
            var cnt = TextAndTip(text, tip);
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.toggle(ref val, cnt);

#endif
            _START();
            val = GUILayout.Toggle(val, cnt, GuiMaxWidthOption);
            return _END();
        }

        private static bool toggle(ref bool val, icon TrueIcon, icon FalseIcon, string tip, int width, PEGI_Styles.PegiGuiStyle style)
            => toggle(ref val, TrueIcon.GetIcon(), FalseIcon.GetIcon(), tip, width, style.Current);

        public static bool toggle(ref bool val, icon TrueIcon, icon FalseIcon, string tip, int width = defaultButtonSize)
            => toggle(ref val, TrueIcon.GetIcon(), FalseIcon.GetIcon(), tip, width);

        public static bool toggle(ref bool val, icon TrueIcon, icon FalseIcon, GUIStyle style = null) => toggle(ref val, TrueIcon.GetIcon(), FalseIcon.GetIcon(), "", defaultButtonSize, style);

        public static bool toggleVisibilityIcon(this string label, string hint, ref bool val, bool dontHideTextWhenOn = false)
        {
            SetBgColor(Color.clear);

            var changed = toggle(ref val, icon.Show, icon.Hide, hint, DefaultToggleIconSize, PEGI_Styles.ToggleButton).SetPreviousBgColor();

            if (!val || dontHideTextWhenOn) label.write(hint, PEGI_Styles.ToggleLabel(val));

            return changed;
        }

        public static bool toggleIcon(ref bool val, string hint = "Toggle On/Off") => toggle(ref val, icon.True.BgColor(Color.clear), icon.False, hint, DefaultToggleIconSize, PEGI_Styles.ToggleButton).SetPreviousBgColor();

        public static bool toggleIcon(ref int val, string hint = "Toggle On/Off")
        {
            var boo = val != 0;

            if (toggle(ref boo, icon.True.BgColor(Color.clear), icon.False, hint, DefaultToggleIconSize, PEGI_Styles.ToggleButton).SetPreviousBgColor())
            {
                val = boo ? 1 : 0;
                return true;
            }

            return false;
        }

        public static bool toggleIcon(this string label, string hint, ref bool val, bool hideTextWhenTrue = false)
        {
            SetBgColor(Color.clear);

            var ret = toggle(ref val, icon.True, icon.False, hint, DefaultToggleIconSize, PEGI_Styles.ToggleButton).SetPreviousBgColor();

            if ((!val || !hideTextWhenTrue) && label.ClickLabel(hint, -1, PEGI_Styles.ToggleLabel(val)).changes_Internal(ref ret))
                val = !val;

            return ret;
        }

        public static bool toggleIcon(this string label, ref bool val, bool hideTextWhenTrue = false)
        {
            var changed = toggle(ref val, icon.True.BgColor(Color.clear), icon.False, label, DefaultToggleIconSize, PEGI_Styles.ToggleButton).SetPreviousBgColor();

            if ((!val || !hideTextWhenTrue) && label.ClickLabel(label, -1, PEGI_Styles.ToggleLabel(val)).changes_Internal(ref changed))
                val = !val;

            return changed;
        }

        public static bool toggleIcon(this string labelIfFalse, ref bool val, string labelIfTrue)
            => (val ? labelIfTrue : labelIfFalse).toggleIcon(ref val);

        public static bool toggleIcon(this string label, bool isTrue, System.Action<bool> setValue, bool hideTextWhenTrue = false)
        {
            if (label.toggleIcon(ref isTrue, hideTextWhenTrue: hideTextWhenTrue))
            {
                setValue.Invoke(isTrue);
                return true;
            }
            return false;
        }

        public static bool toggleIconConfirm(this string label, ref bool val, string confirmationTag, string tip = null, bool hideTextWhenTrue = false)
        {
            var changed = toggleConfirm(ref val, icon.True.BgColor(Color.clear), icon.False, confirmationTag: confirmationTag, tip: tip.IsNullOrEmpty() ? label : tip, DefaultToggleIconSize).SetPreviousBgColor();

            if (!ConfirmationDialogue.IsRequestedFor(confirmationTag) && (!val || !hideTextWhenTrue))
            {
                if (label.ClickLabelConfirm(confirmationTag: confirmationTag, style: PEGI_Styles.ToggleLabel(val)).changes_Internal(ref changed))
                    val = !val;
            }

            return changed;
        }


        private static bool toggle(ref bool val, Texture2D TrueIcon, Texture2D FalseIcon, string tip, int width = defaultButtonSize, GUIStyle style = null)
        {

            if (val)
            {
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
        
        public static bool toggleConfirm(ref bool val, icon TrueIcon, icon FalseIcon, string confirmationTag, string tip, int width = defaultButtonSize)
        {
            if (val)
            {
                if (TrueIcon.ClickConfirm(confirmationTag: confirmationTag, toolTip: tip, width))
                {
                    val = false;
                    return true;
                }
            }
            else if (FalseIcon.ClickConfirm(confirmationTag: confirmationTag, toolTip: tip, width))
            {
                val = true;
                return true;
            }

            return false;
        }

        public static bool toggle(ref bool val, string text, string tip, int width)
        {
            var cnt = TextAndTip(text, tip);
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                ef.write(cnt, width);
                return ef.toggle(ref val);
            }

#endif

            _START();
            val = GUILayout.Toggle(val, cnt, GuiMaxWidthOption);
            return _END();

        }

        public static bool toggle(int ind, CountlessBool tb)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.toggle(ind, tb);
#endif
            var has = tb[ind];

            if (!toggle(ref has)) return false;

            tb.Toggle(ind);
            return true;
        }



        public static bool toggle(this Texture img, ref bool val)
        {
            draw(img, 25);
            return toggle(ref val);
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

        public static bool toggle_CompileDirective(string text, string keyword)
        {
            var changed = false;

#if UNITY_EDITOR
            var val = QcUnity.GetPlatformDirective(keyword);

            if (text.toggleIconConfirm(ref val, confirmationTag: keyword, tip: "Changing Compile directive will force scripts to recompile. {0} {1}? ".F(val ? "Disable" : "Enable" , keyword)))
                QcUnity.SetPlatformDirective(keyword, val);
#endif

            return changed;
        }

        public static bool toggleDefaultInspector(Object target)
        {
#if UNITY_EDITOR

            if (!PaintingGameViewUI)
                return ef.toggleDefaultInspector(target);
#endif

            return false;
        }

        #endregion

        #region Edit

        #region Audio Clip

        public static bool edit(this string label, int width, ref AudioClip field)
        {
            label.write(width);
            return edit(ref field);
        }

        public static bool edit(this string label, ref AudioClip field)
        {
            label.write(ApproximateLength(label));
            return edit(ref field);
        }

        public static bool edit(ref AudioClip clip, int width)
        {

            var ret =
#if UNITY_EDITOR
                !PaintingGameViewUI ? ef.edit(ref clip, width) :
#endif
                    false;

            clip.PlayButton();

            return ret;
        }

        public static bool edit(ref AudioClip clip)
        {

            var ret =
#if UNITY_EDITOR
                !PaintingGameViewUI ? ef.edit(ref clip) :
#endif
                    false;

            clip.PlayButton();

            return ret;
        }

        private static void PlayButton(this AudioClip clip)
        {
            if (clip && icon.Play.Click(20))
            {
                //var req = 
                    clip.Play();
                //if (offset > 0)
                    //req.FromTimeOffset(offset);
            }
        }

        #endregion

        #region UnityObject

        public static bool edit_scene(this string label, int width, ref string path)
        {
            if (PaintingGameViewUI)
            {
                label.write(width);
                path.write();
                return false;
            }
            else
            {
                label.write(width);
                return edit_Scene(ref path);
            }
        }

        public static bool edit_scene(this string label, ref string path) 
        {
            if (PaintingGameViewUI)
            {
                "{0}: {1}".F(label, path).write();
                return false;
            }
            else
            {
                label.write();
                return edit_Scene(ref path);
            }
        }

        public static bool edit_Scene(ref string path, int width) =>
#if UNITY_EDITOR
                !PaintingGameViewUI ? ef.edit_Scene(ref path, width) :
#endif
            false;

        public static bool edit_Scene(ref string path) =>
#if UNITY_EDITOR
                !PaintingGameViewUI ? ef.edit_Scene(ref path) :
#endif
            false;

        public static bool edit_ifNull<T>(this string label, ref T component, GameObject parent) where T : Component
        {
            if (component)
                return false;
            
            label.write();
            return edit_ifNull(ref component, parent);
            
        }

        public static bool edit_ifNull<T>(ref T component, GameObject parent) where T : Component
        {
            if (component)
                return false;

            var changed = ChangeTrackStart();

            typeof(T).ToString().SimplifyTypeName().write();
            if (icon.Refresh.Click("Get Component()"))
                component = parent.GetComponent<T>();
            if (icon.Add.Click("Add Component"))
                component = parent.AddComponent<T>();

            return changed;
        }

        public static bool edit<T>(ref T field, int width, bool allowSceneObjects = true) where T : Object =>
#if UNITY_EDITOR
                !PaintingGameViewUI ? ef.edit(ref field, width, allowSceneObjects) :
#endif
            false;


 

        public static bool edit<T>(this string label, ref T field, bool allowSceneObjects = true) where T : Object
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                write(label);
                return edit(ref field, allowSceneObjects);
            }
#endif

            "{0} [{1}]".F(label, field ? field.name : "NULL").write(toolTip: field.GetNameForInspector_Uobj());

            return false;

        }

        public static bool edit<T>(this string label, int width, ref T field, bool allowSceneObjects = true) where T : Object
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                write(label, width);
                return edit(ref field, allowSceneObjects);
            }

#endif
            "{0} [{1}]".F(label, field ? field.name : "NULL").write(toolTip: field.GetNameForInspector_Uobj());
            return false;

        }

        public static bool edit<T>(this string label, string toolTip, int width, ref T field, bool allowSceneObjects = true) where T : Object
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                write(label, toolTip, width);
                return edit(ref field, allowSceneObjects);
            }

#endif
            "{0} [{1}]".F(label, field ? field.name : "NULL").write(toolTip: toolTip);
            return false;
        }

        public static bool edit<T>(ref T field, bool allowSceneObjects = true) where T : Object =>
#if UNITY_EDITOR
            !PaintingGameViewUI ? ef.edit(ref field, allowSceneObjects) :
#endif
                false;
        
        public static bool edit(ref Object field, System.Type type, bool allowSceneObjects = true) =>
#if UNITY_EDITOR
            !PaintingGameViewUI ? ef.edit(ref field, type, allowSceneObjects) :
#endif
                false;

        public static bool edit(ref Object field, System.Type type, int width, bool allowSceneObjects = true) =>
#if UNITY_EDITOR
                     !PaintingGameViewUI ? ef.edit(ref field, type, width, allowSceneObjects) :
#endif
                false;

        public static bool edit_enter_Inspect<T>(this string label, ref T obj, ref int entered, int current, List<T> selectFrom = null, bool showLabelIfEntered = true) where T : Object
            => label.edit_enter_Inspect(-1, ref obj, ref entered, current, selectFrom,  showLabelIfEntered: showLabelIfEntered);

        public static bool edit_enter_Inspect<T>(this string label, int width, ref T obj, ref int entered, int thisOne, List<T> selectFrom = null, bool showLabelIfEntered = true) where T : Object
        {
            if (!EnterOptionsDrawn_Internal(ref entered, thisOne))
                return false;

            var changed = ChangeTrackStart();

            if (!obj)
            {
                if (label.IsNullOrEmpty())
                    label = typeof(T).ToPegiStringType();

                if (!selectFrom.IsNullOrEmpty()) 
                    label.select_or_edit(width, ref obj, selectFrom);
                else
                    label.edit(width: ApproximateLength(label), ref obj);
            }
            else
            {
                if (label.IsNullOrEmpty())
                    label = obj.GetNameForInspector();

                label = label.tryAddCount(obj);

                var lst = obj as IPEGI_ListInspect;

                if (lst != null)
                    lst.enter_Inspect_AsList(ref entered, thisOne, label);
                else
                {
                    var pgi = QcUnity.TryGetInterfaceFrom<IPEGI>(obj);

                    if (label.isConditionally_Entered(pgi != null, ref entered, thisOne, showLabelIfEntered: showLabelIfEntered).nl_ifEntered())
                        pgi.Nested_Inspect();
                    else
                        obj.ClickHighlight();
                }

                if ((entered == -1) && icon.Clear.ClickConfirm(confirmationTag: "Del " + label + thisOne, Msg.MakeElementNull.GetText()))
                    obj = null;
            }

            return changed;
        }

#endregion

#region Vectors & Rects

        public static bool edit(this string label, ref Quaternion qt)
        {
            var eul = qt.eulerAngles;

            if (edit(label, ref eul))
            {
                qt.eulerAngles = eul;
                return true;
            }

            return false;
        }
        
        public static bool edit(this string label, int width, ref Quaternion qt)
        {
            write(label, width);
            return edit(ref qt);
        }

        public static bool edit(ref Quaternion qt)
        {
            var eul = qt.eulerAngles;

            if (edit(ref eul))
            {
                qt.eulerAngles = eul;
                return true;
            }

            return false;
        }
        
        public static bool edit(ref Vector4 val)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
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

        public static bool edit(this string label, ref Rect val)
        {
            var v4 = val.ToVector4(true);

            if (label.edit(ref v4))
            {
                val = v4.ToRect(true);
                return true;
            }

            return false;
        }

        public static bool edit(ref RectOffset val, int min, int max)
        {
            int top = val.top;
            int bottom = val.bottom;
            int left = val.left;
            int right = val.right;

            if (
                "Left".edit(70, ref left, min, max).nl() ||
                "Right".edit(70, ref right, min, max).nl() ||
                "Top".edit(70, ref top, min, max).nl() ||
                "Bottom".edit(70, ref bottom, min, max).nl())
            {
                val = new RectOffset(left: left, right: right, top: top, bottom: bottom);

                return true;
            }

            return false;
        }

        public static bool edit(ref RectOffset val)
        {
            int top = val.top;
            int bottom = val.bottom;
            int left = val.left;
            int right = val.right;

            if (
                "Left".edit(70, ref left).nl() ||
                "Right".edit(70, ref right).nl() ||
                "Top".edit(70, ref top).nl() ||
                "Bottom".edit(70, ref bottom).nl())
            {
                val = new RectOffset(left: left, right: right, top: top, bottom: bottom);

                return true;
            }

            return false;
        }
        
        public static bool edit(this string label, ref Vector4 val)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
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
           "X".edit(15, ref val.x) || "Y".edit(15, ref val.y) || "Z".edit(15, ref val.z);

        public static bool edit(this string label, int width, ref Vector3 val)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(label, ref val);
#endif

            write(label);
            nl();
            return edit(ref val);
        }

        public static bool edit(this string label, ref Vector3 val)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(label, ref val);
#endif

            write(label);
            nl();
            return edit(ref val);
        }

        public static bool edit(ref Vector2 val)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val);

#endif

            return edit(ref val.x) || edit(ref val.y);
        }

        public static bool edit01(this string label, ref Vector2 val)
        {
            label.nl(label.ApproximateLength());
            return edit01(ref val);
        }

        public static bool edit01(ref Vector2 val) =>
            "X".edit01(10, ref val.x).nl() ||
            "Y".edit01(10, ref val.y).nl();

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
            if (!PaintingGameViewUI)
                return ef.edit(label, ref val);
#endif

            write(label);
            return edit(ref val.x) || edit(ref val.y);

        }

        public static bool edit(this string label, string toolTip, int width, ref Vector2 v2)
        {
            write(label, toolTip, width);
            return edit(ref v2);
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
            if (!PaintingGameViewUI)
                return ef.edit(ref col);

#endif
            var changed = false;

            SetBgColor(col);

            if ("Color".isFoldout())
            {
                pegi.nl();

                changed = icon.Red.edit_ColorChannel(ref col, 0).nl() ||
                       icon.Green.edit_ColorChannel(ref col, 1).nl() ||
                       icon.Blue.edit_ColorChannel(ref col, 2).nl() ||
                       icon.Alpha.edit_ColorChannel(ref col, 3).nl();
            }

            SetPreviousBgColor();

            return changed;


        }

        public static bool edit(ref Color col, int width)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
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

                if (ico.edit(ref chan, 0, 1).changes_Internal(ref changed))
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

                if (label.edit(ref chan, 0, 1).changes_Internal(ref changed))
                    col[channel] = chan;

            }

            return changed;
        }

        public static bool edit(this string label, ref Color col)
        {
            if (PaintingGameViewUI)
            {
                if (label.isFoldout())
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
            if (PaintingGameViewUI)
            {
                if (label.isFoldout())
                    return edit(ref col);

            }
            else
            {
                write(label, width);
                return edit(ref col);
            }

            return false;
        }

        public static bool edit(this string label, string toolTip, int width, ref Color col)
        {
            if (PaintingGameViewUI)
                return false;

            write(label, toolTip, width);
            return edit(ref col);
        }

#endregion

#region Material

        public static bool editTexture(this Material mat, string name) => mat.editTexture(name, name);

        public static bool editTexture(this Material mat, string name, string display)
        {

            display.write(display.ApproximateLength());
            var tex = mat.GetTexture(name);

            if (edit(ref tex))
            {
                mat.SetTexture(name, tex);
                return true;
            }

            return false;
        }

        public static bool toggle(this Material mat, string keyword)
        {
            var val = System.Array.IndexOf(mat.shaderKeywords, keyword) != -1;

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
                name = property.GetNameForInspector();

            if (name.edit(name.Length * letterSizeInPixels, ref val))
            {
                mat.Set(property, val);
                return true;
            }

            return false;
        }

        public static bool edit(this Material mat, ShaderProperty.FloatValue property, string name, float min, float max)
        {
            var val = mat.Get(property);

            if (name.IsNullOrEmpty())
                name = property.GetNameForInspector();

            if (name.edit(name.Length * letterSizeInPixels, ref val, min, max))
            {
                mat.Set(property, val);
                return true;
            }

            return false;
        }

        public static bool edit(this Material mat, ShaderProperty.ColorFloat4Value property, string name = null)
        {
            var val = mat.Get(property);

            if (name.IsNullOrEmpty())
                name = property.GetNameForInspector();

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
                name = property.GetNameForInspector();

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
                name = property.GetNameForInspector();

            if (name.edit(name.Length * letterSizeInPixels, ref val))
            {
                mat.Set(property, val);
                return true;
            }

            return false;
        }

#endregion

#region UInt

        public static bool edit(ref uint val)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val);
#endif
            _START();
            var newval = GUILayout.TextField(val.ToString(), GuiMaxWidthOption);
            if (!_END()) return false;

            int newValue;
            if (int.TryParse(newval, out newValue))
                val = (uint)newValue;

            return true;


        }

        public static bool edit(ref uint val, int width)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val, width);
#endif

            _START();
            var strVal = GUILayout.TextField(val.ToString(), GUILayout.MaxWidth(width));
            if (!_END()) return false;

            int newValue;
            if (int.TryParse(strVal, out newValue))
                val = (uint)newValue;

            return true;

        }

        public static bool edit(ref uint val, uint min, uint max)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val, min, max);
#endif

            _START();
            val = (uint)GUILayout.HorizontalSlider(val, min, max, GuiMaxWidthOption);
            return _END();

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

        public static bool edit(this string label, string toolTip, int width, ref uint val)
        {
            write(label, toolTip, width);
            return edit(ref val);
        }

        public static bool edit(this string label, string toolTip, int width, ref uint val, uint min, uint max)
        {
            label.sliderText(val, toolTip, width);
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
            if (!PaintingGameViewUI)
                return ef.editTag(ref tag);
#endif

            return false;
        }
        
        public static bool editLayerMask(this string label, ref int val)
        {
            label.write(label.ApproximateLength());
            return editLayerMask(ref val);
        }

        public static bool editLayerMask(ref int val)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.editLayerMask(ref val);
#endif

            return false;
        }

        public static bool edit(ref int val)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val);
#endif

            _START();
            var intText = GUILayout.TextField(val.ToString(), GuiMaxWidthOption);
            if (!_END()) return false;

            int newValue;

            if (int.TryParse(intText, out newValue))
                val = newValue;

            return true;
        }

        public static bool edit(ref int val, int width)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val, width);
#endif

            _START();

            var newValText = GUILayout.TextField(val.ToString(), GUILayout.MaxWidth(width));

            if (!_END()) return false;

            int newValue;
            if (int.TryParse(newValText, out newValue))
                val = newValue;

            return SetChangedTrue_Internal;

        }

        public static bool edit(ref int val, int min, int max)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val, min, max);
#endif

            _START();
            val = (int)GUILayout.HorizontalSlider(val, min, max, GuiMaxWidthOption);
            return _END();

        }

        private static int editedInteger;
        private static int editedIntegerIndex = -1;
        public static bool editDelayed(ref int val, int width = -1)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
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

                return SetChangedTrue_Internal;
            }

            if (edit(ref tmp).ignoreChanges())
            {
                editedInteger = tmp;
                editedIntegerIndex = _elementIndex;
            }

            _elementIndex++;

            return false;

        }

        public static bool editDelayed(this string label, ref int val, int width)
        {
            write(label, Msg.EditDelayed_HitEnter.GetText());
            return editDelayed(ref val, width);
        }

        public static bool editDelayed(this string label, ref int val)
        {
            write(label, Msg.EditDelayed_HitEnter.GetText());
            return editDelayed(ref val);
        }

        public static bool editDelayed(this string label, int width, ref int val)
        {
            label.write(Msg.EditDelayed_HitEnter.GetText(), width: width);
            return editDelayed(ref val);
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

        public static bool edit(this string label, string toolTip, int width, ref int val)
        {
            write(label, toolTip, width);
            return edit(ref val);
        }

        public static bool edit(this string label, string toolTip, int width, ref int val, int min, int max)
        {
            label.sliderText(val, toolTip, width);
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, int width, ref int val)
        {
            write(label, width);
            return edit(ref val);
        }

        public static bool edit(this string label, int width, ref int val, int valueWidth)
        {
            write(label, width);
            return edit(ref val, valueWidth);
        }

        public static bool edit_Range(this string label, ref int from, ref int to) => label.edit_Range(ApproximateLength(label), ref from, ref to);

        public static bool edit_Range(this string label, int width, ref int from, ref int to)
        {
            write(label, width);
            var changed = false;
            if (editDelayed(ref from).changes_Internal(ref changed))
                to = Mathf.Max(from, to);

            write("-", 10);

            if (editDelayed(ref to).changes_Internal(ref changed))
                from = Mathf.Min(from, to);

            return changed;
        }

#endregion

#region Long

        public static bool edit(ref long val)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val);
#endif

            _START();
            var intText = GUILayout.TextField(val.ToString(), GuiMaxWidthOption);
            if (!_END()) return false;

            long newValue;

            if (long.TryParse(intText, out newValue))
                val = newValue;

            return true;
        }

        public static bool edit(ref long val, int width)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val, width);
#endif

            _START();

            var newValText = GUILayout.TextField(val.ToString(), GUILayout.MaxWidth(width));

            if (!_END()) return false;

            long newValue;
            if (long.TryParse(newValText, out newValue))
                val = newValue;

            return SetChangedTrue_Internal;

        }

        public static bool edit(this string label, ref long val)
        {
            write(label);
            return edit(ref val);
        }

        public static bool edit(this string label, int width, ref long val)
        {
            write(label, width);
            return edit(ref val);
        }
        
#endregion

#region Float

        public static bool edit(ref float val)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val);
#endif

            _START();
            var newval = GUILayout.TextField(val.ToString(CultureInfo.InvariantCulture), GuiMaxWidthOption);

            if (!_END()) return false;

            float newValue;
            if (float.TryParse(newval, out newValue))
                val = newValue;

            return SetChangedTrue_Internal;
        }

        public static bool edit(ref float val, int width)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val, width);
#endif

            _START();

            var newval = GUILayout.TextField(val.ToString(CultureInfo.InvariantCulture), GUILayout.MaxWidth(width));

            if (!_END()) return false;

            float newValue;
            if (float.TryParse(newval, out newValue))
                val = newValue;

            return SetChangedTrue_Internal;

        }

        public static bool edit(this string label, ref float val)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(label, ref val);
#endif
            write(label);
            return edit(ref val);
        }

        public static bool editPOW(ref float val, float min, float max)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.editPOW(ref val, min, max);
#endif

            _START();
            var after = GUILayout.HorizontalSlider(Mathf.Sqrt(val), min, max, GuiMaxWidthOption);
            if (!_END()) return false;
            val = after * after;
            return SetChangedTrue_Internal;
        }

        public static bool edit(ref float val, float min, float max)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val, min, max);
#endif

            _START();
            val = GUILayout.HorizontalSlider(val, min, max, GuiMaxWidthOption);
            return _END();

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
            if (!PaintingGameViewUI)
                return ef.editDelayed(ref val, width);
#endif

            return editDelayed(ref val);
        }

        public static bool editDelayed(ref float val)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.editDelayed(ref val);
#endif


            checkLine();

            var tmp = (editedFloatIndex == _elementIndex) ? editedFloat : val.ToString(CultureInfo.InvariantCulture);

            if (KeyCode.Return.IsDown() && (_elementIndex == editedFloatIndex))
            {
                edit(ref tmp);

                float newValue;
                if (float.TryParse(editedFloat, out newValue))
                    val = newValue;
                _elementIndex++;

                editedFloatIndex = -1;

                return SetChangedTrue_Internal;
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
        private static int editedFloatIndex = -1;

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
            if (editDelayed(ref from).changes_Internal(ref changed))
                to = Mathf.Max(from, to);

            write("-", 10);

            if (editDelayed(ref to).changes_Internal(ref changed))
                from = Mathf.Min(from, to);
            
            return changed;
        }

        private static void sliderText(this string label, float val, string tip, int width)
        {
            if (PaintingGameViewUI)
                "{0} [{1}]".F(label, val.ToString("F3")).write(width);
            else
                write(label, tip, width);
        }

        public static bool edit(this string label, ref float val, float min, float max)
        {
            label.sliderText(val, label, label.Length * letterSizeInPixels);
            return edit(ref val, min, max);
        }

        private static bool edit(this icon ico, ref float val, float min, float max)
        {
            ico.draw();
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, int width, ref float val, float min, float max)
        {
            label.sliderText(val, label, width);
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, string toolTip, int width, ref float val, float min, float max)
        {
            label.sliderText(val, toolTip, width);
            return edit(ref val, min, max);
        }

        public static bool edit(this string label, string toolTip, int width, ref float val)
        {
            write(label, toolTip, width);
            return edit(ref val);
        }

        public static bool edit(this string label, string toolTip, ref float val)
        {
            write(label, toolTip);
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
            if (!PaintingGameViewUI)
                return ef.editDelayed(ref val, width);
#endif

            return editDelayed(ref val);
        }

        public static bool editDelayed(ref double val)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.editDelayed(ref val);
#endif


            checkLine();

            var tmp = (editedDoubleIndex == _elementIndex) ? editedDouble : val.ToString(CultureInfo.InvariantCulture);

            if (KeyCode.Return.IsDown() && (_elementIndex == editedDoubleIndex))
            {
                edit(ref tmp);

                double newValue;
                if (double.TryParse(editedDouble, out newValue))
                    val = newValue;
                _elementIndex++;

                editedDoubleIndex = -1;

                return SetChangedTrue_Internal;
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
        private static int editedDoubleIndex = -1;
        
        public static bool edit(ref double val)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val);
#endif
            _START();
            var newval = GUILayout.TextField(val.ToString(CultureInfo.InvariantCulture), GuiMaxWidthOption);
            if (!_END()) return false;
            double newValue;
            if (!double.TryParse(newval, out newValue)) return false;
            val = newValue;
            return SetChangedTrue_Internal;
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

        public static bool edit(this string label, string toolTip, int width, ref double val)
        {
            label.write(toolTip, width);
            return edit(ref val);
        }

        public static bool edit(ref double val, int width)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val, width);
#endif

            _START();
            var newval = GUILayout.TextField(val.ToString(CultureInfo.InvariantCulture), GUILayout.MaxWidth(width));
            if (!_END()) return false;

            double newValue;
            if (double.TryParse(newval, out newValue))
                val = newValue;

            return SetChangedTrue_Internal;

        }

#endregion

#region Enum

        public static bool editEnum<T>(this string text, int width, ref T eval)
        {
            write(text, width);
            return editEnum(ref eval);
        }

        public static bool editEnum<T>(this string text, ref T value)
        {
            write(text, width: ApproximateLength(text));
            return editEnum(ref value);
        }

        public static bool editEnum<T>(this string label, int width, ref int current, List<int> options)
        {
            label.write(width);
            return editEnum_Internal<T>(ref current, options);
        }

        public static bool editEnum<T>(ref T eval, int width = -1)
        {
            var val = System.Convert.ToInt32(eval);

            if (editEnum_Internal(ref val, typeof(T), width))
            {
                eval = (T)((object)val);
                return true;
            }

            return false;
        }

#pragma warning disable IDE0051 // Remove unused private members
        private static bool editEnum<T>(ref T eval, List<int> options, int width = -1)
#pragma warning restore IDE0051 // Remove unused private members
        {
            var val = System.Convert.ToInt32(eval);

            if (editEnum_Internal(ref val, typeof(T), options, width))
            {
                eval = (T)((object)val);
                return true;
            }

            return false;
        }

        public static bool editEnum<T>(ref int current, int width = -1) => editEnum_Internal(ref current, typeof(T), width: width);



        private static bool editEnum_Internal<T>(ref int eval, List<int> options, int width = -1)
            => editEnum_Internal(ref eval, typeof(T), options, width);

        private static bool editEnum_Internal(ref int current, System.Type type, int width = -1)
        {
            checkLine();
            var tmpVal = -1;

            var names = System.Enum.GetNames(type);
            var val = (int[])System.Enum.GetValues(type);

            for (var i = 0; i < val.Length; i++)
            {
                names[i] = "{0}:".F(val[i]) + names[i];
                if (val[i] == current)
                    tmpVal = i;
            }

            if (!select(ref tmpVal, names, width)) return false;

            current = val[tmpVal];
            return true;
        }
        
     
        private static bool editEnum_Internal(ref int current, System.Type type, List<int> options, int width = -1)
        {
            checkLine();
            var tmpVal = -1;

            List<string> names = new List<string>(options.Count + 1);

            for (var i = 0; i < options.Count; i++)
            {
                var op = options[i];
                names.Add("{0}:".F(op)+ System.Enum.GetName(type, op));
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
        
#endregion

#region Enum Flags

        public static bool editEnumFlags<T>(this string text, ref T eval)
        {
            write(text);
            return editEnumFlags(ref eval);
        }
        
        public static bool editEnumFlags<T>(this string text, int width, ref T eval)
        {
            write(text, width);
            return editEnumFlags(ref eval);
        }

        public static bool editEnumFlags<T>(ref T eval, int width = -1)
        {
            var val = System.Convert.ToInt32(eval);

            if (editEnumFlags(ref val, typeof(T), width))
            {
                eval = (T)((object)val);
                return true;
            }

            return false;
        }

        private static bool editEnumFlags(ref int current, System.Type type, int width = -1)
        {

            checkLine();

            var names = System.Enum.GetNames(type);
            var values = (int[])System.Enum.GetValues(type);

            Countless<string> sortedNames = new Countless<string>();

            int currentPower = 0;

            int toPow = 1;

            for (var i = 0; i < values.Length; i++)
            {
                var val = values[i];
                while (val > toPow)
                {
                    currentPower++;
                    toPow = (int)Mathf.Pow(2, currentPower);
                }

                if (val == toPow)
                    sortedNames[currentPower] = names[i];
            }

            string[] snms = new string[currentPower + 1];

            for (int i = 0; i <= currentPower; i++)
                snms[i] = sortedNames[i];

            return selectFlags(ref current, snms, width);
        }
#endregion

#region String

        private static string editedText;
        private static string editedHash = "";
        public static bool editDelayed(ref string val)
        {
            if (LengthIsTooLong(ref val)) return false;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.editDelayed(ref val);
#endif

            checkLine();

            if ((KeyCode.Return.IsDown() && (val.GetHashCode().ToString() == editedHash)))
            {
                GUILayout.TextField(val, GuiMaxWidthOption);
                val = editedText;

                return SetChangedTrue_Internal;
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
            if (!PaintingGameViewUI)
                return ef.editDelayed(ref val, width);
#endif

            checkLine();

            if ((KeyCode.Return.IsDown() && (val.GetHashCode().ToString() == editedHash)))
            {
                GUILayout.TextField(val, GuiMaxWidthOption);
                val = editedText;
                return SetChangedTrue_Internal;
            }

            var tmp = val;
            if (edit(ref tmp, width).ignoreChanges())
            {
                editedText = tmp;
                editedHash = val.GetHashCode().ToString();
            }

            return false;

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
            
            if ("String is too long: {0} COPY".F(label.Substring(0, 10)).Click())
                SetCopyPasteBuffer(label);
            
            return true;
        }

        public static bool edit(ref string val)
        {

            if (LengthIsTooLong(ref val)) return false;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val);
#endif

            _START();
            val = GUILayout.TextField(val, GUILayout.MaxWidth(250));
            return _END();
        }

        public static bool edit(ref string val, int width)
        {

            if (LengthIsTooLong(ref val)) return false;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val, width);
#endif

            _START();
            var newval = GUILayout.TextField(val, GUILayout.MaxWidth(width));
            if (_END())
            {
                val = newval;
                return SetChangedTrue_Internal;
            }
            return false;

        }

        public static bool edit(this string label, ref string val)
        {

            if (LengthIsTooLong(ref val)) return false;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
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

        public static bool edit(this string label, string toolTip, int width, ref string val)
        {
            write(label, toolTip, width);
            return edit(ref val);
        }

        public static bool editBig(this string label, ref string val, int height = 100)
        {
            write(label);
            return editBig(ref val, height: height);
        }


        public static bool editBig(ref string val, int height = 100)
        {

            nl();

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.editBig(ref val, height).nl();
#endif

            _START();
            val = GUILayout.TextArea(val, GUILayout.MaxHeight(height), GuiMaxWidthOption);
            return _END();

        }


#endregion

#region Property

        public static bool edit_Property<T>(this string label, System.Linq.Expressions.Expression<System.Func<T>> memberExpression, Object obj, int fieldWidth = -1, bool includeChildren = true)
        {
            label.nl();
            return edit_Property(memberExpression, fieldWidth, obj, includeChildren);
        }

        private static bool edit_Property<T>(System.Linq.Expressions.Expression<System.Func<T>> memberExpression, int width, Object obj, bool includeChildren)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit_Property(width, memberExpression, obj, includeChildren);

#endif
            return false;
        }


#endregion

#region Custom classes

        public static bool edit(ref MyIntVec2 val)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.edit(ref val);
#endif

            return edit(ref val.x) || edit(ref val.y);
        }

        public static bool edit(this string label, int width, ref MyIntVec2 val)
        {
            write(label, width);
            nl();
            return edit(ref val);
        }

#endregion
        
#endregion
        
    }
}