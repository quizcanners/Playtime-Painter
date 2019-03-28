#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace PlayerAndEditorGUI
{

    [ExecuteInEditMode]
    public class PEGI_Styles : MonoBehaviour, IPEGI
    {
        #region Button
        static GUIStyle _imageButton;
        public static GUIStyle ImageButton =>
            _imageButton ?? (_imageButton = new GUIStyle(GUI.skin.button)
            {
                overflow = new RectOffset(-3, -3, 0, 0),
                margin = new RectOffset(-3, -3, 1, 1),
            });

        static GUIStyle _clickableText;
        public static GUIStyle ClickableText =>
            _clickableText ?? (_clickableText = new GUIStyle(GUI.skin.label)
            {
                wordWrap = false,
                fontStyle = FontStyle.Bold,
                contentOffset = new Vector2(0, 4),
                normal = {textColor = new Color32(40, 40, 40, 255)},
            });

        static GUIStyle _scalableText;
        public static GUIStyle ScalableBlueText(int fontSize = 11)
        {
          
                if (_scalableText == null)
                    _scalableText = new GUIStyle(GUI.skin.label)
                    {
                        wordWrap = false,
                        fontStyle = FontStyle.Bold,
                        normal = {textColor = new Color32(40, 40, 255, 255)},
                        margin = new RectOffset(0, 0, 0, -15),
                        //contentOffset = new Vector2(0, 4),
                    };
                

                _scalableText.fontSize = fontSize;

                return _scalableText;
            
        }

        #endregion

        #region Toggle

        static GUIStyle _toggleButton;
        public static GUIStyle ToggleButton =>
            _toggleButton ?? (_toggleButton = new GUIStyle(GUI.skin.button)
            {
                overflow = new RectOffset(-3, -3, 0, 0),
                margin = new RectOffset(-13, -13, -10, -10),
                contentOffset = new Vector2(0, 6),
            });

        public static GUIStyle ToggleLabel(bool isOn) => isOn ? ToggleLabel_On : ToggleLabel_Off;
            
        static GUIStyle _toggleTextOff;
        static GUIStyle ToggleLabel_Off =>
            _toggleTextOff ?? (_toggleTextOff = new GUIStyle(GUI.skin.label)
            {
                contentOffset = new Vector2(0, 2),
                wordWrap = true,
                normal = {textColor = new Color32(40, 40, 40, 255)},
                //fontSize = 10
                //fontStyle = FontStyle.Italic
            });

        static GUIStyle _toggleTextOn;
        static GUIStyle ToggleLabel_On =>
            _toggleTextOn ?? (_toggleTextOn = new GUIStyle(GUI.skin.label)
            {
                contentOffset = new Vector2(0, 2),
                wordWrap = true,


                // fontStyle = FontStyle.Bold
            });

        #endregion

        #region List
        static GUIStyle _listLabel;
        public static GUIStyle ListLabel =>
            _listLabel ?? (_listLabel = new GUIStyle(GUI.skin.label)
            {
                margin = new RectOffset(1, 1, 6, 1),
                fontSize = 12,
                clipping = TextClipping.Clip,
                richText = true,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = {textColor = new Color32(43, 30, 11, 255)},
            });

        public static Color listReadabilityRed = new Color(1, 0.9f, 0.9f, 1);
        public static Color listReadabilityBlue = new Color(0.95f, 0.95f, 1f, 1);


        #endregion

        #region Fold / Enter / Exit
        static GUIStyle _enterLabel;
        public static GUIStyle EnterLabel =>
            _enterLabel ?? (_enterLabel = new GUIStyle
            {
                margin = new RectOffset(10, 10, 10, 10),
                fontSize = 12,
                richText = true,
                wordWrap = false,
                clipping = TextClipping.Clip,
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold,
                contentOffset = new Vector2(0, -6),
                normal = {textColor = new Color32(43, 30, 77, 255)},
            });

        static GUIStyle _exitLabel;
        public static GUIStyle ExitLabel =>
            _exitLabel ?? (_exitLabel = new GUIStyle
            {
                margin = new RectOffset(10, 10, 10, 10),
                fontSize = 13,
                richText = true,
                wordWrap = false,
                clipping = TextClipping.Clip,
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Italic,
                contentOffset = new Vector2(0, 2),
                normal = {textColor = new Color32(77, 77, 77, 255)},
            });

        static GUIStyle _foldedOutLabel;
        public static GUIStyle FoldedOutLabel =>
            _foldedOutLabel ?? (_foldedOutLabel = new GUIStyle
            {
                margin = new RectOffset(40, 10, 10, 10),
                fontSize = 12,
                richText = true,
                wordWrap = false,
                clipping = TextClipping.Clip,
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold,
                imagePosition = ImagePosition.ImageLeft,
                normal = {textColor = new Color32(43, 77, 33, 255)},
            });

        #endregion

        #region Text
        static GUIStyle _wrappingText;
        public static GUIStyle WrappingText =>
            _wrappingText ?? (_wrappingText = new GUIStyle(GUI.skin.label)
            {
                clipping = TextClipping.Clip,
                //wordWrap = true
            });

        static GUIStyle _overflowText;
        public static GUIStyle OverflowText =>
            _overflowText ?? (_overflowText = new GUIStyle(GUI.skin.label)
            {
                clipping = TextClipping.Overflow,
                wordWrap = true,
                fontSize = 12,
            });
        #endregion

        #region Line

        static GUIStyle _horizontalLine = new GUIStyle();

        public static GUIStyle HorizontalLine =>
            _horizontalLine ?? (_horizontalLine = new GUIStyle()
            {
#if UNITY_EDITOR
                normal = {background = EditorGUIUtility.whiteTexture},
#endif
                margin = new RectOffset(0, 0, 4, 4),
                fixedHeight = 1
            });
        
        #endregion

        // Testing 

        public GUIStyle testListLabel;
        public GUIStyle testImageButton;
        public GUISkin skin;

        #if PEGI
        public bool Inspect()
        {
            var changed = false;

            "Some text asdj iosjd oasjdoiasjd ioasjdisao diaduisa".write();
            if (icon.Refresh.Click().nl()) Refresh();

            "Some more text".nl();

            icon.Docs.GetIcon().edit_Property("Button icon" , () => testImageButton, this).nl();
            
            "List Label".edit_Property(() => testListLabel, this).nl(ref changed);

            "List Label Test".write(testListLabel); pegi.nl();

            return changed;
        }
        #endif

        private void Refresh()
        {
            testListLabel = ListLabel;
            testImageButton = ImageButton;
        }
        
        void OnEnable() => Refresh();

    }
}