using PlayerAndEditorGUI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerAndEditorGUI
{

    [ExecuteInEditMode]
    public class PEGI_Styles : MonoBehaviour, IPEGI
    {

        static GUIStyle _imageButton;
        public static GUIStyle ImageButton    {
            get {
                if (_imageButton == null)
                    _imageButton = new GUIStyle(GUI.skin.button) {

                        overflow = new RectOffset(-3,-3,0, 0),
                        margin = new RectOffset(-3,-3,1,1),
                    };
                
                return _imageButton;
            }
    
        }

        static GUIStyle _toggleText;
        public static GUIStyle ToggleLabel
        {
            get
            {
                if (_toggleText == null)
                    _toggleText = new GUIStyle(GUI.skin.label)  {
                        contentOffset = new Vector2(0, 4)
                       // fontStyle = FontStyle.Bold
                    };

                
                return _toggleText;
            }
        }

        static GUIStyle _listLabel;
        public static GUIStyle ListLabel { get {
                if (_listLabel == null)
                {
                    _listLabel = new GUIStyle()
                    {
                        margin = new RectOffset(1, 1, 1, 1),
                        fontSize = 13,
                        richText = true,
                        wordWrap = false,
                        alignment = TextAnchor.MiddleLeft,
                        fontStyle = FontStyle.Bold,
                        
                    };
                    _listLabel.normal.textColor = new Color32(43, 30, 11,255); //2C1F0B);
                }
                return _listLabel; }
        
        }

        static GUIStyle _enterExitLabel;
        public static GUIStyle EnterExitLabel
        {
            get
            {
                if (_enterExitLabel == null)
                {
                    _enterExitLabel = new GUIStyle()
                    {
                        margin = new RectOffset(10, 10, 10, 10),
                        fontSize = 12,
                        richText = true,
                        wordWrap = false,
                        alignment = TextAnchor.MiddleLeft,
                        fontStyle = FontStyle.Bold,
                        contentOffset = new Vector2(0, 4),

                    };
                    _enterExitLabel.normal.textColor = new Color32(43, 30, 77, 255); //2C1F0B);
                }
                return _enterExitLabel;
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
                        alignment = TextAnchor.MiddleLeft,
                        fontStyle = FontStyle.Bold,
                        imagePosition = ImagePosition.ImageLeft,
                        
                        
                    };
                    _foldedOutLabel.normal.textColor = new Color32(43, 77, 33, 255); //2C1F0B);
                }
                return _foldedOutLabel;
            }
         
        }
        
        public static Color listReadabilityRed = new Color(1, 0.9f,0.9f,1);
        public static Color listReadabilityBlue = new Color(0.95f, 0.95f, 1f, 1);

        // Testing stuff

        public GUIStyle TestListLabel;
        public GUIStyle TestImageButton;
        public GUISkin skin;

        public List<GameObject> testList = new List<GameObject>();

#if PEGI

        public bool PEGI()
        {
            bool changed = false;

            "Some text asdj iosjd oasjdoiasjd ioasjdisao diaduisa".write();
            if (icon.Refresh.Click().nl()) Refresh();

            "Some more text".nl();

            icon.Docs.getIcon().edit("Button icon" , () => TestImageButton, this).nl();

           

            "List Label".edit(() => TestListLabel, this).nl();

            "List Label Test".write(TestListLabel); pegi.nl();

            return changed;
        }

#endif

        void Refresh()
        {
            TestListLabel = ListLabel;
            TestImageButton = ImageButton;
        }

        // Use this for initialization
        void OnEnable() => Refresh();
        

        // Update is called once per frame
        void Update()
        {

        }
    }
}