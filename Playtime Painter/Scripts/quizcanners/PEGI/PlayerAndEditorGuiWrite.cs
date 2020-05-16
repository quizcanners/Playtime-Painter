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
    public static partial class pegi
    {
        #region GUI Contents
        private static GUIContent imageAndTip = new GUIContent();

        private static GUIContent ImageAndTip(Texture tex, string toolTip)
        {
            imageAndTip.image = tex;
            imageAndTip.tooltip = toolTip;
            return imageAndTip;
        }

      /*  private static GUIContent ImageAndTip(Texture tex)
        {
            imageAndTip.image = tex;
            imageAndTip.tooltip = tex ? tex.name : "Null Image";
            return imageAndTip;
        }*/

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

       // private static GUIContent tipOnlyContent = new GUIContent();

      /*  private static GUIContent TipOnlyContent(string text)
        {
            tipOnlyContent.tooltip = text;
            return tipOnlyContent;
        }*/

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

        public static void write(this Sprite sprite, int width = defaultButtonSize, bool alphaBlend = false) =>
            write(sprite, Color.white, width: width, alphaBlend: alphaBlend);
        
        public static void write(this Sprite sprite, Color color, int width = defaultButtonSize, bool alphaBlend = false)
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
                Rect rect = GUILayoutUtility.GetRect(spriteW, spriteH,
                    GUILayout.ExpandWidth(false));

                if (Event.current.type == EventType.Repaint)
                {
                    if (sprite.packed)
                    {
                        var tex = sprite.texture;
                        c.xMin /= tex.width;
                        c.xMax /= tex.width;
                        c.yMin /= tex.height;
                        c.yMax /= tex.height;
                        GUI.DrawTextureWithTexCoords(rect, tex, c, alphaBlend);
                    }

                    else
                    {
                        GUI.DrawTexture(rect, sprite.texture, ScaleMode.ScaleToFit, alphaBlend, 1, color,
                            Vector4.zero, Vector4.zero);
                    }
                }
            }

        }

        /*  public static void write(this Sprite sprite, string toolTip, int width = defaultButtonSize)
        {
            if (sprite)
                sprite.texture.write(toolTip, width);
            else
                icon.Empty.write(toolTip, width);
        }

        public static void write(this Sprite sprite, string toolTip, int width, int height, bool alphaBlend = true)
        {
            if (sprite)
                sprite.texture.write(toolTip, width, height, alphaBlend: alphaBlend);
            else
                icon.Empty.write(toolTip, width, height);
        }*/

        public static void write(this Texture img, int width = defaultButtonSize, bool alphaBlend = true)
        {
            if (!img)
                return;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                ef.write(img, width, alphaBlend: alphaBlend);

            else
#endif
            {
                SetBgColor(Color.clear);

                img.Click(width);

                RestoreBGcolor();
            }
        }

        public static void write(this Texture img, string toolTip, int width = defaultButtonSize, bool alphaBlend = true)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                ef.write(img, toolTip, width, alphaBlend: alphaBlend);
            else
#endif
            {

                SetBgColor(Color.clear);

                img.Click(toolTip, width, width);

                RestoreBGcolor();
            }

        }

        public static void write(this Texture img, string toolTip, int width, int height, bool alphaBlend = true)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                ef.write(img, toolTip, width, height, alphaBlend: alphaBlend);
            else
#endif
            {

                SetBgColor(Color.clear);

                img.Click(toolTip, width, height);

                RestoreBGcolor();

            }

        }

        #endregion

        #region Icon

        public static void write(this icon icon, int size = defaultButtonSize) => write(icon.GetIcon(), size);

        public static void write(this icon icon, string toolTip, int size = defaultButtonSize) => write(icon.GetIcon(), toolTip, size);

        public static void write(this icon icon, string toolTip, int width, int height) => write(icon.GetIcon(), toolTip, width, height);

        #endregion

        #region String

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
                GUILayout.Label(cnt, style.Current, GuiMaxWidthOption);
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
                GUILayout.Label(textAndTip, style.Current, GuiMaxWidthOption);
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
            GUILayout.Label(textAndTip, style.Current, GUILayout.MaxWidth(width));

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
            GUILayout.Label(textAndTip, style.Current, GUILayout.MaxWidth(width));

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

        public static void writeBig(this string text, string tooltip = "")
        {
            text.write(tooltip, PEGI_Styles.OverflowText);
            nl();
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
                SetCopyPasteBuffer(text);

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
                SetCopyPasteBuffer(text);

            return ret;

        }

        public static bool write_ForCopy(this string label, int width, string value, bool showCopyButton = false)
        {
            var ret = edit(label, width, ref value);

            if (showCopyButton && icon.Copy.Click("Copy {0} to clipboard".F(label)))
                SetCopyPasteBuffer(value, label);

            return ret;

        }

        public static bool write_ForCopy(this string label, string value, bool showCopyButton = false)
        {
            var ret = label.edit(ref value);

            if (showCopyButton && icon.Copy.Click("Copy {0} to clipboard".F(label)))
                SetCopyPasteBuffer(value, label);

            return ret;

        }

        public static bool write_ForCopy_Big(string value, bool showCopyButton = false)
        {

            if (showCopyButton && "Copy text to clipboard".Click().nl())
                SetCopyPasteBuffer(value);

            if (PaintingGameViewUI && !value.IsNullOrEmpty() && value.ContainsAtLeast('\n', 5)) // Due to MGUI BUG
                ".....   Big Text Has Many Lines: {0}".F(value.FirstLine()).write();
            else
                return editBig(ref value);

            return false;
        }

        public static bool write_ForCopy_Big(this string label, string value, bool showCopyButton = false)
        {

            if (showCopyButton && icon.Copy.Click("Copy text to clipboard"))
                SetCopyPasteBuffer(value, label);

            label.nl();

            if (PaintingGameViewUI && !value.IsNullOrEmpty() && value.ContainsAtLeast('\n', 5)) // Due to MGUI BUG
                ".....   Big Text Has Many Lines: {0}".F(value.FirstLine()).write();
            else
                return editBig(ref value);

            return false;
        }

        public static void SetCopyPasteBuffer(string value, string hint = "", bool sendNotificationIn3Dview = true)
        {
            GUIUtility.systemCopyBuffer = value;

            if (sendNotificationIn3Dview)
                GameView.ShowNotification("{0} Copied to clipboard".F(hint));
        }
        
        #endregion

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
            GUILayout.Label(text, PEGI_Styles.WarningText.Current, GuiMaxWidthOption);
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
            GUILayout.Label(text, PEGI_Styles.HintText.Current, GuiMaxWidthOption);
            if (startNewLineAfter)
                nl();


        }

        public static void resetOneTimeHint(string key) => PlayerPrefs.SetInt(key, 0);

        public static void hideOneTimeHint(string key) => PlayerPrefs.SetInt(key, 1);

        public static bool writeOneTimeHint(this string text, string key)
        {

            if (PlayerPrefs.GetInt(key) != 0) return false;

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
                GUILayout.Label(text, PEGI_Styles.HintText.Current, GuiMaxWidthOption);
            }

            if (icon.Done.ClickUnFocus("Got it").nl()) 
                PlayerPrefs.SetInt(key, 1);

            return true;
        }

        #endregion
        
    }
}
