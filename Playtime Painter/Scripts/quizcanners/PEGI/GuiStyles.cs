
using System.Linq.Expressions;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace PlayerAndEditorGUI
{

    public static class PEGI_Styles
    {
        public static bool inspectingList;

        private static bool InGameView => pegi.PaintingGameViewUI;

        public static Color listReadabilityRed = new Color(1, 0.85f, 0.85f, 1);
        public static Color listReadabilityBlue = new Color(0.9f, 0.9f, 1f, 1);

        public delegate GUIStyle CreateGUI();

        public class PegiGuiStyle : IPEGI
        {
            private GUIStyle editorGui;
            private GUIStyle playtime;
            private GUIStyle editorGuiInList;
            private GUIStyle playtimeInList;
            private CreateGUI generator;

            public GUIStyle Current
            {
                get
                {
                    if (InGameView)
                    {
                        if (inspectingList)
                            return playtimeInList ?? (playtimeInList = generator());
                        else
                            return playtime ?? (playtime = generator());
                    }
                    else
                    {
                        if (inspectingList)
                            return editorGuiInList ?? (editorGuiInList = generator());
                        else
                            return editorGui ?? (editorGui = generator());
                    }
                }
            }

            public PegiGuiStyle(CreateGUI generator)
            {
                this.generator = generator;
            }

            public bool Inspect()
            {
                var cur = Current;

                var al = cur.alignment;

                if ("Allignment".editEnum(90, ref al).nl())
                    cur.alignment = al;

                var fs = cur.fontSize;
                if ("Font Size".edit(90, ref fs).nl())
                    cur.fontSize = fs;

                return false;
            }
        }
        
        #region Button
        
        public static PegiGuiStyle ImageButton = new PegiGuiStyle(()=> new GUIStyle(GUI.skin.button)
                {
                    overflow = new RectOffset(-3, -3, 0, 0),
                    margin = new RectOffset(-3, -3, 1, 1),
                });
        
        public static PegiGuiStyle ClickableText = new PegiGuiStyle(()=> new GUIStyle(GUI.skin.label) {
                    wordWrap = false,
                    fontStyle = FontStyle.Bold,
                    contentOffset = new Vector2(0, 4),
                    alignment = TextAnchor.MiddleLeft,
                    normal = {textColor = InGameView ? new Color32(220,220,255,255) : new Color32(40, 40, 40, 255)},
                });

        public static PegiGuiStyle ScalableBlueText(int fontSize)
        {
            _scalableBlueText.Current.fontSize = fontSize;
            return _scalableBlueText;
        }

        private static PegiGuiStyle _scalableBlueText = new PegiGuiStyle(() => new GUIStyle(GUI.skin.label) {
                    wordWrap = false,
                    fontStyle = FontStyle.Bold,
                    normal = {textColor = InGameView ? new Color32(120, 120, 255, 255) : new Color32(40, 40, 255, 255)},
                    margin = new RectOffset(0, 0, 0, -15),
                    fontSize = 14
                });

        #endregion

        #region Toggle

        public static PegiGuiStyle ToggleButton = new PegiGuiStyle(() => new GUIStyle(GUI.skin.button)
        {
            overflow = new RectOffset(-3, -3, 0, 0),
            margin = new RectOffset(-13, -13, -10, -10),
            contentOffset = new Vector2(0, 6),
        });

         static PegiGuiStyle ToggleLabel_Off = new PegiGuiStyle(() => new GUIStyle(GUI.skin.label)
         {
             contentOffset = new Vector2(0, 2),
             wordWrap = true,
             normal = { textColor = InGameView ? new Color32(255, 255, 255, 255) : new Color32(40, 40, 40, 255) },
         });

         static PegiGuiStyle ToggleLabel_On = new PegiGuiStyle(() => new GUIStyle(GUI.skin.label)
         {
             contentOffset = new Vector2(0, 2),
             wordWrap = true,
         });

        public static PegiGuiStyle ToggleLabel(bool isOn) => isOn ? ToggleLabel_On : ToggleLabel_Off;

        #endregion

        #region List


        public static PegiGuiStyle ListLabel = new PegiGuiStyle(() => new GUIStyle(GUI.skin.label)
        {
            margin = new RectOffset(1, 1, 6, 1),
            fontSize = 12,
            clipping = TextClipping.Clip,
            richText = true,
            wordWrap = true,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            normal =
            {
                textColor = InGameView ? new Color32(255, 255, 255, 255) : new Color32(43, 30, 11, 255)
            },
        });

        #endregion

        #region Fold / Enter / Exit

        public static PegiGuiStyle EnterLabel = new PegiGuiStyle(() => new GUIStyle
        {
            margin = new RectOffset(10, 10, 10, 10),
            fontSize = 12,
            richText = true,
            wordWrap = false,
            clipping = TextClipping.Clip,
            alignment = TextAnchor.MiddleLeft,
            fontStyle = FontStyle.Bold,
            contentOffset = new Vector2(0, -6),
            normal = { textColor = InGameView ? new Color32(255, 255, 220, 255) : new Color32(43, 30, 77, 255) },
        });

        public static PegiGuiStyle ExitLabel = new PegiGuiStyle(() => new GUIStyle
        {
            margin = new RectOffset(10, 10, 10, 10),
            fontSize = 13,
            richText = true,
            wordWrap = false,
            clipping = TextClipping.Clip,
            alignment = TextAnchor.MiddleLeft,
            fontStyle = FontStyle.Italic,
            contentOffset = new Vector2(0, -6),
            normal = { textColor = InGameView ? new Color32(255, 220, 220, 255) : new Color32(77, 77, 77, 255) },
        });

        public static PegiGuiStyle FoldedOutLabel = new PegiGuiStyle(() => new GUIStyle
        {
            margin = new RectOffset(40, 10, 10, 10),
            fontSize = 12,
            richText = true,
            wordWrap = false,
            clipping = TextClipping.Clip,
            alignment = TextAnchor.MiddleLeft,
            fontStyle = FontStyle.Bold,
            imagePosition = ImagePosition.ImageLeft,
            normal = { textColor = InGameView ? new Color32(200, 220, 220, 255) : new Color32(43, 77, 33, 255) },
        });

        #endregion

        #region Text

        public static PegiGuiStyle WrappingText = new PegiGuiStyle(() => inspectingList ? 
            new GUIStyle(GUI.skin.label){clipping = TextClipping.Clip,}.ToWhiteBg() :
            new GUIStyle(GUI.skin.label){clipping = TextClipping.Clip,});


        public static PegiGuiStyle OverflowText = new PegiGuiStyle(() => new GUIStyle(GUI.skin.label)
        {
            clipping = TextClipping.Overflow,
            wordWrap = true,
            fontSize = 12,
        });

        public static PegiGuiStyle HintText = new PegiGuiStyle(() => new GUIStyle(GUI.skin.label)
        {
            clipping = TextClipping.Overflow,
            wordWrap = true,
            fontSize = 10,
            fontStyle = FontStyle.Italic,
            normal =
            {
                textColor = InGameView ? new Color32(192, 192, 100, 255) : new Color32(64, 64, 11, 255)
            }
        });

        public static PegiGuiStyle WarningText = new PegiGuiStyle(() => new GUIStyle(GUI.skin.label)
        {
            clipping = TextClipping.Overflow,
            wordWrap = true,
            fontSize = 13,
            fontStyle = FontStyle.BoldAndItalic,
            normal =
            {
                textColor = InGameView ? new Color32(255, 20, 20, 255) : new Color32(255, 64, 64, 255)
            }
        });

        #endregion

        #region Line

        public static PegiGuiStyle HorizontalLine = new PegiGuiStyle(() => new GUIStyle()
        {
#if UNITY_EDITOR
            normal = { background = EditorGUIUtility.whiteTexture },
#endif
            margin = new RectOffset(0, 0, 4, 4),
            fixedHeight = 1
        });
        
        #endregion

        // Todo: Only give texture with BG for Lists
        private static GUIStyle ToWhiteBg(this GUIStyle style)
        {

#if UNITY_2020_1_OR_NEWER
            if (inspectingList)
                style.normal.background = Texture2D.linearGrayTexture;
#endif
            return style;
        }


        private static GUIStyle testListLabel;
        private static GUIStyle testImageButton;
        private static GUISkin skin;

        public static bool Inspect()
        {
            var changed = false;

            "Warning Text".nl(WarningText);
            WarningText.Nested_Inspect().nl();

            "Wrapping Text".nl(WrappingText);
            WrappingText.Nested_Inspect().nl();

            pegi.line();
            pegi.nl();

            "ClickableText".nl(ClickableText);
            ClickableText.Nested_Inspect().nl();


            "Hint: Theese changes will not be saved, for tunning only. They are hardcoded.".writeHint();
            HintText.Nested_Inspect().nl();

            return changed;
        }

        private static void Refresh()
        {
            testListLabel = ListLabel.Current;
            testImageButton = ImageButton.Current;
        }

    }
}