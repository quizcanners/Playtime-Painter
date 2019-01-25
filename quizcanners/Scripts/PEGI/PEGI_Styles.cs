using PlayerAndEditorGUI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerAndEditorGUI
{

    [ExecuteInEditMode]
    public class PEGI_Styles : MonoBehaviour, IPEGI
    {
        #region Button
        static GUIStyle _imageButton;
        public static GUIStyle ImageButton    {
            get {
                if (_imageButton == null)
                {
                    _imageButton = new GUIStyle(GUI.skin.button)
                    {

                        overflow = new RectOffset(-3, -3, 0, 0),
                        margin = new RectOffset(-3, -3, 1, 1),

                    };

                  //  _imageButton.normal.background = iconGackground.Frame.GetSprite();
                }
                
                return _imageButton;
            }
    
        }

        static GUIStyle _clickableText;
        public static GUIStyle ClickableText
        {
            get
            {
                if (_clickableText == null)
                {
                    _clickableText = new GUIStyle(GUI.skin.label)
                    {
                        wordWrap = false,
                        fontStyle = FontStyle.Bold,
                        contentOffset = new Vector2(0, 4),
                    };
                    _clickableText.normal.textColor = new Color32(40, 40, 255, 255);
                }

                return _clickableText;
            }
        }

        #endregion

        #region Toggle

        static GUIStyle _toggleButton;
        public static GUIStyle ToggleButton
        {
            get
            {
                if (_toggleButton == null)
                {
                    _toggleButton = new GUIStyle(GUI.skin.button)
                    {

                        overflow = new RectOffset(-3, -3, 0, 0),
                        margin = new RectOffset(-13, -13, -10, -10),
                        contentOffset = new Vector2(0, 6),
                    };

                    
                    //  _imageButton.normal.background = iconGackground.Frame.GetSprite();
                }

                return _toggleButton;
            }

        }

        public static GUIStyle ToggleLabel(bool isOn) => isOn ? ToggleLabel_On : ToggleLabel_Off;
            
        static GUIStyle _toggleTextOff;
        static GUIStyle ToggleLabel_Off
        {
            get
            {
                if (_toggleTextOff == null)
                {
                    _toggleTextOff = new GUIStyle(GUI.skin.label)
                    {
                        contentOffset = new Vector2(0, 2),
                        wordWrap = true,
                        //fontSize = 10
                        //fontStyle = FontStyle.Italic
                    };

                    _toggleTextOff.normal.textColor = new Color32(40, 40, 40, 255); 
                }
                return _toggleTextOff;
            }
        }

        static GUIStyle _toggleTextOn;
        static GUIStyle ToggleLabel_On
        {
            get
            {
                if (_toggleTextOn == null)
                    _toggleTextOn = new GUIStyle(GUI.skin.label)
                    {
                        contentOffset = new Vector2(0, 2),
                        wordWrap = true,
                       
                        
                        // fontStyle = FontStyle.Bold
                    };
             

                return _toggleTextOn;
            }
        }
        #endregion

        #region List
        static GUIStyle _listLabel;
        public static GUIStyle ListLabel {
            get {
                if (_listLabel == null)
                {
                    _listLabel = new GUIStyle(GUI.skin.label)
                    {
                        margin = new RectOffset(1, 1, 6, 1),
                        fontSize = 12,
                        clipping = TextClipping.Clip,
                        richText = true,
                        wordWrap = false,
                        alignment = TextAnchor.MiddleCenter,
                        fontStyle = FontStyle.Bold, 

                    };
                    _listLabel.normal.textColor = new Color32(43, 30, 11,255); //2C1F0B);
                }
                return _listLabel;
            }
        
        }

        public static Color listReadabilityRed = new Color(1, 0.9f, 0.9f, 1);
        public static Color listReadabilityBlue = new Color(0.95f, 0.95f, 1f, 1);


        #endregion

        #region Fold / Enter / Exit
        static GUIStyle _enterLabel;
        public static GUIStyle EnterLabel
        {
            get
            {
                if (_enterLabel == null)
                {
                    _enterLabel = new GUIStyle()
                    {
                        margin = new RectOffset(10, 10, 10, 10),
                        fontSize = 12,
                        richText = true,
                        wordWrap = false,
                        clipping = TextClipping.Clip,
                        alignment = TextAnchor.MiddleLeft,
                        fontStyle = FontStyle.Bold,
                        contentOffset = new Vector2(0, -6),

                    };
                    _enterLabel.normal.textColor = new Color32(43, 30, 77, 255); //2C1F0B);
                }
                return _enterLabel;
            }
         
        }

        static GUIStyle _exitLabel;
        public static GUIStyle ExitLabel
        {
            get
            {
                if (_exitLabel == null)
                {
                    _exitLabel = new GUIStyle()
                    {
                        margin = new RectOffset(10, 10, 10, 10),
                        fontSize = 13,
                        richText = true,
                        wordWrap = false,
                        clipping = TextClipping.Clip,
                        alignment = TextAnchor.MiddleLeft,
                        fontStyle = FontStyle.Italic,
                        contentOffset = new Vector2(0, 2),

                    };
                    _exitLabel.normal.textColor = new Color32(77, 77, 77, 255); //2C1F0B);
                }
                return _exitLabel;
            }

        }

        static GUIStyle _foldedOutLabel;
        public static GUIStyle FoldedOutLabel
        {
            get
            {
                if (_foldedOutLabel == null)
                {
                    _foldedOutLabel = new GUIStyle()
                    {
                        margin = new RectOffset(40, 10, 10, 10),
                        fontSize = 12,
                        richText = true,
                        wordWrap = false,
                        clipping = TextClipping.Clip,
                        alignment = TextAnchor.MiddleLeft,
                        fontStyle = FontStyle.Bold,
                        imagePosition = ImagePosition.ImageLeft,
                        
                        
                    };
                    _foldedOutLabel.normal.textColor = new Color32(43, 77, 33, 255); //2C1F0B);
                }
                return _foldedOutLabel;
            }
         
        }
        #endregion
        
        static GUIStyle _wrappingText;
        public static GUIStyle WrappingText
        {
            get
            {
                if (_wrappingText == null)
                {
                    _wrappingText = new GUIStyle(GUI.skin.label)
                    {
                        clipping = TextClipping.Clip,
                        //wordWrap = true
                    };


                }

                return _wrappingText;
            }
        }

        // Testing stuff

        public GUIStyle testListLabel;
        public GUIStyle testImageButton;
        public GUISkin skin;

        public List<GameObject> testList = new List<GameObject>();

        #if PEGI
        public bool Inspect()
        {
            bool changed = false;

            "Some text asdj iosjd oasjdoiasjd ioasjdisao diaduisa".write();
            if (icon.Refresh.Click().nl()) Refresh();

            "Some more text".nl();

            icon.Docs.GetIcon().edit_Property("Button icon" , () => testImageButton, this).nl();

           

            "List Label".edit_Property(() => testListLabel, this).nl();

            "List Label Test".write(testListLabel); pegi.nl();

            return changed;
        }
        #endif

        void Refresh()
        {
            testListLabel = ListLabel;
            testImageButton = ImageButton;
        }
        
        void OnEnable() => Refresh();

    }
}