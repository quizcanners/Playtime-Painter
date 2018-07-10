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
        public static GUIStyle ImageButton
        {
            get
            {
                if (_imageButton == null)
                {
                    _imageButton = new GUIStyle()
                    {
                        margin = new RectOffset(0, 0, 0, 0),
                        fontSize = 15,
                        richText = true,
                        wordWrap = true,
                        alignment = TextAnchor.MiddleCenter,
                        fontStyle = FontStyle.Bold,
                        imagePosition = ImagePosition.ImageOnly,

                    };
                   // _listLabel.normal.textColor = new Color32(43, 30, 11, 255); //2C1F0B);
                }
                return _imageButton;
            }
            set
            {
                _imageButton = value;
            }
        }

        static GUIStyle _listLabel;
        public static GUIStyle ListLabel { get {
                if (_listLabel == null)
                {
                    _listLabel = new GUIStyle()
                    {
                        margin = new RectOffset(10, 10, 10, 10),
                        fontSize = 15,
                        richText = true,
                        wordWrap = true,
                        alignment = TextAnchor.MiddleCenter,
                        fontStyle = FontStyle.Bold,
                        
                    };
                    _listLabel.normal.textColor = new Color32(43, 30, 11,255); //2C1F0B);
                }
                return _listLabel; }
            set {
                _listLabel = value;
            }
        }

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