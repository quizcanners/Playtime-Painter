/* ATTACH THIS SCRIPT TO ANY OBJECT TO START EXPLORING THE INSPECTOR
 *
 * Player & Editor GUI, further referenced as PEGI is a wrapper of Unity's native EditorGuiLayout & GUILayout systems.
 * GitHub: https://github.com/quizcanners/Tools/tree/master/Playtime%20Painter/Scripts/quizcanners  (GNU General Public License)
 * I used and developed it since around 2016 and the goal is to simply write inspectors as effortlessly as possible.
 */

using System;
using UnityEngine;

namespace QuizCanners.Inspect.Examples
{

    public class PlayerAndEditorGui_DOCUMENTATION_COMPONENT : MonoBehaviour, IPEGI
    {
        
        [SerializeField] private bool showInspectorInTheGameView;

        [NonSerialized] private InspectExample_Nested someOtherScript;

        #region To render inspecto in game view

        private static readonly pegi.GameView.Window OnGUIWindow = new pegi.GameView.Window();
        public void OnGUI()
        {
            if (showInspectorInTheGameView)
            {
                OnGUIWindow.Render(this);
            }
        }
        #endregion

        #region Actual Inspector 

        private int _selectedMenuOption = -1; //To split and group controls

        public void Inspect()
        {
            /*
             * You should attach this script to the game object and see the example
             * 
             * 
             */


            if (_selectedMenuOption == -1)
            {
                pegi.nl();

                if (!gameObject.name.Equals("PEGI Examples") && "Game Object name".Click())
                    gameObject.name = "PEGI Examples";

                "PEGI MAIN MENU".nl(style: PEGI_Styles.ListLabel);
            }

            if ("write, nl".isEntered(enteredOne: ref _selectedMenuOption, thisOne: 0).nl())
            {
                pegi.write(text: "pegi.nl(); means New Line");
                pegi.nl();

                pegi.write("pegi.write(\"You will use New Line often\");");
                pegi.write("or all will be in one line and not fit. So pegi.nl();", toolTip: "And this is a tooltip");
                pegi.nl();

                pegi.nl("There are shorter ways to write the above: pegi.nl(text)");

                "\"Extension function\".write();".write();
                pegi.nl();

                "\"and this is the shortest version\".nl()".nl();

                "I will use it further on...".nl(tip: "You can have tooltips and width. Tooltip comes first", width: 120);

                string.Format("This is Sibling number {0} in the hierarchy", transform.GetSiblingIndex() + 1 ).nl();

            }

            if ("click, toggle".isEntered(ref _selectedMenuOption, 1).nl())
            {
                if (pegi.Click("pegi.Click(\" Button name \")"))
                {
                    Debug.Log("Clicking");
                }

                "Is a button. Returns true when clicked.".write();
                pegi.nl();

                if (showInspectorInTheGameView)
                {
                    if ("Hide inspector from game view".Click(toolTip: "Tooltip").nl())
                    {
                        showInspectorInTheGameView = false;
                    }
                }
                else
                {
                    if ("Show inspector in the game view".Click().nl())
                    {
                        showInspectorInTheGameView = true;
                    }
                }

                "Inspector visible in the game view".toggle(ref showInspectorInTheGameView);
                pegi.nl();

                if (showInspectorInTheGameView)
                    "Inspector size".edit(width: 70, ref OnGUIWindow.upscale, min: 1, max: 3).nl();

            }

            if ("Nested_Inspect(), edit".isEntered(ref _selectedMenuOption, 2).nl())
            {
                if (!someOtherScript)
                {
                    "Nested component not found".writeWarning();
                    pegi.nl();

                    if ("Search for Component".Click().nl())
                    {
                        someOtherScript = GetComponent<InspectExample_Nested>();
                        if (!someOtherScript)
                            Debug.Log("One is not attached. Please click Create");
                    }

                    if ("Attach component".Click().nl())
                    {
                        someOtherScript = gameObject.AddComponent<InspectExample_Nested>();
                    }
                }
                else
                {
                    someOtherScript.Nested_Inspect().nl(); // Always use NestedInspect(); when referencing Inspect(); function of another UnityEngine.Object,
                                                           // otherwise changes may not be serialized.
                }
            }
        }

        #endregion

    }

    #region To Override Unity's default inspector for this component

    [PEGI_Inspector_Override(typeof(PlayerAndEditorGui_DOCUMENTATION_COMPONENT))] internal class InspectEXAMPLE_DOCDrawer : PEGI_Inspector_Override { }

    #endregion
}