using QuizCannersUtilities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PlayerAndEditorGUI
{

    public static class PEGI_Styles
    {
        public static bool InList;
        private static bool InGameView => pegi.PaintingGameViewUI
#if UNITY_EDITOR
                                          || EditorGUIUtility.isProSkin
#endif
        ;

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
                        if (InList)
                            return playtimeInList ?? (playtimeInList = generator());
                        return playtime ?? (playtime = generator());
                    }

                    if (InList)
                        return editorGuiInList ?? (editorGuiInList = generator());
                    return editorGui ?? (editorGui = generator());
                }
            }

            public PegiGuiStyle(CreateGUI generator)
            {
                this.generator = generator;
            }

            #region Inspector

            private int _inspectedProperty = -1;

            bool IPEGI.Inspect()
            {
                var cur = Current;

                var al = cur.alignment;

                if ("Allignment".editEnum(90, ref al).nl())
                    cur.alignment = al;

                var fs = cur.fontSize;
                if ("Font Size".edit(90, ref fs).nl())
                    cur.fontSize = fs;
                
                if ("Padding".foldout(ref _inspectedProperty, 0).nl())
                {
                    RectOffset pad = cur.padding;

                    if (pegi.edit(ref pad, -15, 15).nl())
                        cur.padding = pad;
               
                }

                if ("Margins".foldout(ref _inspectedProperty, 1).nl())
                {
                    RectOffset mar = cur.margin;

                    if (pegi.edit(ref mar, -15, 15).nl())
                        cur.margin = mar;

                }

                return false;
            }
            #endregion
        }
        
        #region Button
        
        public static PegiGuiStyle ImageButton = new PegiGuiStyle(()=> new GUIStyle(GUI.skin.button)
                {
                    overflow = new RectOffset(-3, -3, 0, 0),
                    margin = new RectOffset(1, -3, 1, 1)
                });
        
        public static PegiGuiStyle ClickableText = new PegiGuiStyle(()=> new GUIStyle(GUI.skin.label) {
                    wordWrap = false,
                    fontStyle = FontStyle.Bold,
                    contentOffset = new Vector2(0, 4),
                    alignment = TextAnchor.MiddleLeft,
                    normal = {textColor = InGameView ? new Color32(220,220,255,255) : new Color32(40, 40, 40, 255)}
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
            contentOffset = new Vector2(0, 6)
        });

         static PegiGuiStyle ToggleLabel_Off = new PegiGuiStyle(() => new GUIStyle(GUI.skin.label)
         {
             contentOffset = new Vector2(0, 2),
             wordWrap = true,
             normal = { textColor = InGameView ? new Color32(255, 255, 255, 255) : new Color32(40, 40, 40, 255) }
         });

         static PegiGuiStyle ToggleLabel_On = new PegiGuiStyle(() => new GUIStyle(GUI.skin.label)
         {
             contentOffset = new Vector2(0, 2),
             wordWrap = true
         });

        public static PegiGuiStyle ToggleLabel(bool isOn) => isOn ? ToggleLabel_On : ToggleLabel_Off;

        #endregion

        #region List


        public static PegiGuiStyle ListLabel = new PegiGuiStyle(() => new GUIStyle(GUI.skin.label)
        {
            margin = new RectOffset(9, 1, 6, 1),
            fontSize = 12,
            clipping = TextClipping.Clip,
            richText = true,
            wordWrap = true,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            normal =
            {
                textColor = InGameView ? new Color32(255, 255, 255, 255) : new Color32(43, 30, 11, 255)
            }
        });

        #endregion

        #region Fold / Enter / Exit

        public static PegiGuiStyle EnterLabel = new PegiGuiStyle(() => new GUIStyle
        {
            padding = InGameView ? new RectOffset(0, 0, 4, 7) : new RectOffset(10, 10, 10, 0),
            margin = InGameView ? new RectOffset(9, 0, 3, 3) : new RectOffset(9, 0, 0, 0),
            fontSize = InGameView ? 14 : 12,
            richText = true,
            wordWrap = false,
            clipping = TextClipping.Clip,
            alignment = TextAnchor.MiddleLeft,
            fontStyle = FontStyle.Bold,
            contentOffset = InGameView ? new Vector2(0,0) : new Vector2(0, -6),
            normal = { textColor = InGameView ? new Color32(255, 255, 220, 255) : new Color32(43, 30, 77, 255) }
        });

        public static PegiGuiStyle ExitLabel = new PegiGuiStyle(() => new GUIStyle
        {
            padding = InGameView ? new RectOffset(0, 0, 4, 7) : new RectOffset(10, 10, 10, 0),
            margin = InGameView ? new RectOffset(9, 0, 3, 3) : new RectOffset(9, 0, 0, 0),
            fontSize = 13,
            richText = true,
            wordWrap = false,
            clipping = TextClipping.Clip,
            alignment = TextAnchor.MiddleLeft,
            fontStyle = FontStyle.Italic,
            contentOffset = InGameView ? new Vector2(0, 0) : new Vector2(0, -6),
            normal = { textColor = InGameView ? new Color32(160, 160, 160, 255) : new Color32(77, 77, 77, 255) }
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
            normal = { textColor = InGameView ? new Color32(200, 220, 220, 255) : new Color32(43, 77, 33, 255) }
        });

        #endregion

        #region Text

        public static PegiGuiStyle ClippingText = new PegiGuiStyle(() => InList ? 
            new GUIStyle(GUI.skin.label){clipping = TextClipping.Clip}.ToGrayBg() :
            new GUIStyle(GUI.skin.label){clipping = TextClipping.Clip});


        public static PegiGuiStyle OverflowText = new PegiGuiStyle(() => new GUIStyle(GUI.skin.label)
        {
            clipping = TextClipping.Overflow,
            wordWrap = true,
            fontSize = 12
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

        public static PegiGuiStyle HorizontalLine = new PegiGuiStyle(() => new GUIStyle
        {
#if UNITY_EDITOR
            normal = { background = EditorGUIUtility.whiteTexture },
#endif
            margin = new RectOffset(0, 0, 4, 4),
            fixedHeight = 1
        });
        
        #endregion

        // Todo: Only give texture with BG for Lists
        private static GUIStyle ToGrayBg(this GUIStyle style)
        {
#if UNITY_2020_1_OR_NEWER
            if (InList && !InGameView)
                style.normal.background = Texture2D.linearGrayTexture;
#endif
            return style;
        }


        private static GUIStyle testListLabel;
        private static GUIStyle testImageButton;
        private static GUISkin skin;

        private static int _inspectedFont = -1;
        private static int _iteratiedFont;

        private static bool InspectInteranl(this string StyleName, PegiGuiStyle style)
        {
            if (StyleName.enter(ref _inspectedFont, _iteratiedFont).nl())
            {
                "Example text in {0} style ".F(StyleName).nl(style);
                style.Nested_Inspect().nl();
            }

            _iteratiedFont++;

            return false;
        }

        public static bool Inspect()
        {
            _iteratiedFont = 0;

            "Clipping Text".InspectInteranl(ClippingText);

            "Overfloaw text".InspectInteranl(OverflowText);
            
            "Text Button".InspectInteranl(ClickableText);

            "Enter Label".InspectInteranl(EnterLabel);

            "Exit Label".InspectInteranl(ExitLabel);

            "Hint Text".InspectInteranl(HintText);

            "Warning Text".InspectInteranl(WarningText);

            return false;
        }
    }
}