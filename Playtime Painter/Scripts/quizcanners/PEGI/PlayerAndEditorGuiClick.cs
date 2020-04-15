using QuizCannersUtilities;
using UnityEngine;
using static PlayerAndEditorGUI.PEGI_Styles;
using System;
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
    public static partial class pegi
    {
        public const int defaultButtonSize = 26;

        public static bool ClickLink(this string label, string link, string tip = null)
        {

            if (tip == null)
                tip = "Go To: {0}".F(link);

            if (label.ClickText(tip, 12))
            {
                Application.OpenURL(link);
                return true;
            }

            return false;
        }

        public static class EditorView
        {

            public static void RefocusIfLocked(Object current, Object target)
            {
#if UNITY_EDITOR
                if (current != target && target && ActiveEditorTracker.sharedTracker.isLocked)
                {
                    ActiveEditorTracker.sharedTracker.isLocked = false;
                    QcUnity.FocusOn(target);
                    ActiveEditorTracker.sharedTracker.isLocked = true;
                }
#endif
            }

            public static void Lock_UnlockClick(GameObject obj)
            {
#if UNITY_EDITOR
                if (!PaintingGameViewUI)
                {

                    if (ActiveEditorTracker.sharedTracker.isLocked == false &&
                        icon.Unlock.ClickUnFocus("Lock Inspector Window"))
                    {
                        QcUnity.FocusOn(ef.serObj.targetObject);
                        ActiveEditorTracker.sharedTracker.isLocked = true;
                    }

                    if (ActiveEditorTracker.sharedTracker.isLocked && icon.Lock.ClickUnFocus("Unlock Inspector Window"))
                    {
                        ActiveEditorTracker.sharedTracker.isLocked = false;
                        QcUnity.FocusOn(obj);
                    }
                }
#endif
            }

        }

        public static class ConfirmationDialogue
        {
            private static string _tag;
            private static object _targetObject;
            private static string _details;

            public static string ConfirmationText
            {
                get
                {
                    return _details.IsNullOrEmpty() ? Msg.AreYouSure.GetText() : _details;
                }
            }

            public static void Request(string tag, object forObject = null, string details = "")
            {
                _tag = tag;
                _targetObject = forObject;
                _details = details;
            }

            public static void Close()
            {
                _tag = null;
                _targetObject = null;
            }

            public static bool IsRequestedFor(string tag) => tag.Equals(_tag);//(!_confirmTag.IsNullOrEmpty() && _confirmTag.Equals(tag));

            public static bool IsRequestedFor(string confirmationTag, object obj) =>
                confirmationTag.Equals(_tag) && ((_targetObject != null && _targetObject.Equals(obj)) ||
                                                        (obj == null && _targetObject == null));
        }
        
        private static bool ConfirmClick()
        {

            nl();

            if (BgColorReplaced)
                SetBgColor(_previousBgColors[0]);

            if (icon.Close.Click(Msg.No.GetText(), 30))
                ConfirmationDialogue.Close();

            ConfirmationDialogue.ConfirmationText.writeHint(false);

            if (icon.Done.Click(Msg.Yes.GetText(), 30))
            {
                ConfirmationDialogue.Close();
                return true;
            }

            if (BgColorReplaced)
                SetPreviousBgColor();

            nl();


            return false;
        }

        public static bool ClickConfirm(this string label, string confirmationTag, string tip = "")
        {
            if (ConfirmationDialogue.IsRequestedFor(confirmationTag))
                return ConfirmClick();

            if (label.ClickUnFocus(tip))
                ConfirmationDialogue.Request(confirmationTag, details: tip);

            return false;
        }

        public static bool ClickConfirm(this icon icon, string confirmationTag, string tip = "", int width = defaultButtonSize)
        {

            if (ConfirmationDialogue.IsRequestedFor(confirmationTag))
                return ConfirmClick();

            if (icon.ClickUnFocus(tip, width))
                ConfirmationDialogue.Request(confirmationTag, details: tip);

            return false;
        }

        public static bool ClickConfirm(this icon icon, string confirmationTag, object obj, string tip = "", int width = defaultButtonSize)
        {

            if (ConfirmationDialogue.IsRequestedFor(confirmationTag, obj))
                return ConfirmClick();

            if (icon.ClickUnFocus(tip, width))
                ConfirmationDialogue.Request(confirmationTag, obj, tip);

            return false;
        }

        public static bool ClickUnFocus(this Texture tex, int width = defaultButtonSize)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.Click(tex, width).UnFocusIfTrue();
#endif

            checkLine();
            return GUILayout.Button(tex, GUILayout.MaxWidth(width + 5), GUILayout.MaxHeight(width)).DirtyUnFocus();
        }

        public static bool ClickUnFocus(this Texture tex, string tip, int width = defaultButtonSize) =>
             Click(tex, tip, width).UnFocusIfTrue();

        public static bool ClickUnFocus(this Texture tex, string tip, int width, int height) =>
                Click(tex, tip, width, height).UnFocusIfTrue();

        public static bool ClickUnFocus(this string text)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.Click(text).UnFocusIfTrue();
#endif
            checkLine();
            return GUILayout.Button(text, GuiMaxWidthOptionFrom(text)).DirtyUnFocus();
        }

        public static bool ClickUnFocus(this string text, string tip)
        {

            var cntnt = TextAndTip(text, tip);

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.Click(cntnt).UnFocusIfTrue();
#endif
            checkLine();
            return GUILayout.Button(cntnt, GuiMaxWidthOptionFrom(text)).DirtyUnFocus();
        }

        public static bool ClickText(this string label, int fontSize)
        {
            textAndTip.text = label;
            textAndTip.tooltip = label;
            return textAndTip.ClickText(ScalableBlueText(fontSize));
        }

        public static bool ClickText(this string label, string hint, int fontSize) => TextAndTip(label, hint).ClickText(ScalableBlueText(fontSize));

        public static bool ClickText(this string label, PegiGuiStyle style) => TextAndTip(label).ClickText(style);

        private static bool ClickText(this GUIContent content, PegiGuiStyle style)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.Click(content, style.Current);
#endif
            checkLine();
            return GUILayout.Button(content, style.Current, GuiMaxWidthOptionFrom(content, style: style)).Dirty();
        }

        public static bool ClickLabelConfirm(this string label, string confirmationTag, string hint = "ClickAble Text", int width = -1, PegiGuiStyle style = null)
        {
            if (ConfirmationDialogue.IsRequestedFor(confirmationTag))
                return ConfirmClick();

            if (label.ClickLabel(hint: hint, width: width, style: style))
                ConfirmationDialogue.Request(confirmationTag, details: hint);

            return false;
        }
        
        public static bool ClickLabel(this string label, string hint = "ClickAble Text", int width = -1, PegiGuiStyle style = null)
        {
            SetBgColor(Color.clear);

            GUIStyle st = style == null ? ClickableText.Current : style.Current;

            textAndTip.text = label;
            textAndTip.tooltip = hint;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return (width == -1 ? ef.Click(textAndTip, st) : ef.Click(textAndTip, width, st)).UnFocusIfTrue()
                    .RestoreBGColor();
#endif

            checkLine();

            return (width == -1 ? GUILayout.Button(textAndTip, st, GuiMaxWidthOptionFrom(label, st)) : GUILayout.Button(textAndTip, st, GUILayout.MaxWidth(width))).DirtyUnFocus().SetPreviousBgColor();
        }

        private static bool ClickImage(this GUIContent content, int width, GUIStyle style) =>
            content.ClickImage(width, width, style);

        private static bool ClickImage(this GUIContent content, int width, int height, GUIStyle style)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.ClickImage(content, width, style);
#endif
            checkLine();

            return GUILayout.Button(content, GUILayout.MaxWidth(width + 5), GUILayout.MaxHeight(height)).Dirty();
        }

        #region Action

        public static bool Click<T>(this Action<T> action, T value)
        {
            string name = "{0}({1})".F(action.Method.Name, value.GetNameForInspector().SimplifyTypeName());

            if (name.Click())
            {
                action.Invoke(value);
                return true;
            }

            return false;
        }

        public static bool Click(Action action)
        {
            string name = "{0}()".F(action.Method.Name);

            if (name.Click())
            {
                action.Invoke();
                return true;
            }

            return false;
        }

        #endregion

        public static bool Click(this string text, ref bool changed) => text.Click().changes(ref changed);

        public static bool Click(this string text)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.Click(text);
#endif
            checkLine();
            return GUILayout.Button(text, GuiMaxWidthOptionFrom(text)).Dirty();
        }

        public static bool Click(this string text, string tip, ref bool changed) => text.Click(tip).changes(ref changed);

        public static bool Click(this string text, string tip)
        {
            var cnt = TextAndTip(text, tip);
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.Click(cnt);
#endif
            checkLine();
            return GUILayout.Button(cnt, GuiMaxWidthOptionFrom(text)).Dirty();
        }

        private static Texture GetTexture_orEmpty(this Sprite sp) => sp ? sp.texture : icon.Empty.GetIcon();

        public static bool Click(this Sprite img, int size = defaultButtonSize)
            => img.GetTexture_orEmpty().Click(size);

        public static bool Click(this Sprite img, string tip, int size = defaultButtonSize)
            => img.GetTexture_orEmpty().Click(tip, size);

        public static bool Click(this Sprite img, string tip, int width, int height)
            => img.GetTexture_orEmpty().Click(tip, width, height);

        public static bool Click(this Texture img, int size = defaultButtonSize)
        {

            if (!img) img = icon.Empty.GetIcon();

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return ef.Click(img, size);
#endif

            checkLine();
            return GUILayout.Button(img, GUILayout.MaxWidth(size + 5), GUILayout.MaxHeight(size)).Dirty();

        }

        public static bool Click(this Texture img, string tip, int size = defaultButtonSize)
        {

            if (!img)
                img = icon.Empty.GetIcon();
            
            var cnt = ImageAndTip(img, tip);

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
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
            if (!PaintingGameViewUI)
                return ef.ClickImage(cnt, width, height);
#endif
            checkLine();
            return GUILayout.Button(cnt, GUILayout.MaxWidth(width), GUILayout.MaxHeight(height)).Dirty();
        }

        public static bool Click(this icon icon)
        {
            var tex = icon.GetIcon();

            if (!tex || tex == Texture2D.whiteTexture)
                return icon.GetText().Click();
            
            return Click(tex, icon.GetText());
        }

        public static bool Click(this icon icon, ref bool changed)
        {
            var tex = icon.GetIcon();

            if (!tex || tex == Texture2D.whiteTexture)
                return icon.GetText().Click().changes(ref changed);

            return Click(tex, icon.GetText()).changes(ref changed);
        }

        public static bool ClickUnFocus(this icon icon)
        {
            var tex = icon.GetIcon();

            if (!tex || tex == Texture2D.whiteTexture)
                return icon.GetText().ClickUnFocus();

            return ClickUnFocus(tex, icon.GetText());
        }

        public static bool ClickUnFocus(this icon icon, ref bool changed)
        {
            var tex = icon.GetIcon();

            if (!tex || tex == Texture2D.whiteTexture)
                return icon.GetText().ClickUnFocus().changes(ref changed);

            return ClickUnFocus(tex, icon.GetText()).changes(ref changed);
        }

        public static bool ClickUnFocus(this icon icon, int size)
        {
            var tex = icon.GetIcon();

            if (!tex || tex == Texture2D.whiteTexture)
                return icon.GetText().ClickUnFocus();

            return ClickUnFocus(tex, icon.GetText(), size);
        }

        public static bool ClickUnFocus(this icon icon, string tip, int size = defaultButtonSize)
        {
            if (tip == null)
                tip = icon.GetText();

            var tex = icon.GetIcon();

            if (!tex || tex == Texture2D.whiteTexture)
                return icon.GetText().ClickUnFocus(tip);

            return ClickUnFocus(tex, tip, size);
        }

        public static bool ClickUnFocus(this icon icon, string tip, int width, int height)
        {
            var tex = icon.GetIcon();

            if (!tex || tex == Texture2D.whiteTexture)
                return icon.GetText().ClickUnFocus(tip);

            return ClickUnFocus(tex, tip, width, height);
        }

        public static bool Click(this icon icon, int size)
        {
            var tex = icon.GetIcon();

            if (!tex || tex == Texture2D.whiteTexture)
                return icon.GetText().ClickUnFocus();

            return Click(tex, size);
        }

        public static bool Click(this icon icon, string tip, int width, int height)
        {
            var tex = icon.GetIcon();

            if (!tex || tex == Texture2D.whiteTexture)
                return icon.GetText().ClickUnFocus(tip);

            return Click(tex, tip, width, height);
        }

        public static bool Click(this icon icon, string tip, ref bool changed, int size = defaultButtonSize)
        {
            var tex = icon.GetIcon();

            if (!tex || tex == Texture2D.whiteTexture)
                return icon.GetText().ClickUnFocus(tip).changes(ref changed);

            return Click(tex, tip, size).changes(ref changed);
        }

        public static bool Click(this icon icon, string tip, int size = defaultButtonSize)
        {
            var tex = icon.GetIcon();

            if (!tex || tex == Texture2D.whiteTexture)
                return icon.GetText().ClickUnFocus(tip);

            return Click(tex, tip, size);
        }

        public static bool Click(this Color col) => icon.Empty.GUIColor(col).BgColor(Color.clear).Click().RestoreGUIColor().RestoreBGColor();

        public static bool Click(this Color col, string tip, int size = defaultButtonSize) => icon.Empty.GUIColor(col).BgColor(Color.clear).Click(tip, size).RestoreGUIColor().RestoreBGColor();

        public static bool ClickHighlight(this Sprite sp, int width = defaultButtonSize)
        {
#if UNITY_EDITOR
            if (sp && sp.Click(Msg.HighlightElement.GetText(), width))
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
           obj.ClickHighlight(icon.Ping.GetIcon(), width);

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

        public static bool ClickHighlight(this Object obj, string hint, int width = defaultButtonSize)
        {
#if UNITY_EDITOR
            if (obj && icon.Ping.Click(hint))
            {
                EditorGUIUtility.PingObject(obj);
                return true;
            }
#endif

            return false;
        }

        public static bool Click_Enter_Attention_Highlight<T>(this T obj, ref bool changed, icon icon = icon.Enter, string hint = "", bool canBeNull = true) where T : Object, INeedAttention
        {
            var ch = obj.Click_Enter_Attention(icon, hint, canBeNull).changes(ref changed);
            obj.ClickHighlight().changes(ref changed);
            return ch;
        }

        public static bool Click_Enter_Attention(this INeedAttention attention, icon icon = icon.Enter, string hint = "", bool canBeNull = true)
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

    }
}