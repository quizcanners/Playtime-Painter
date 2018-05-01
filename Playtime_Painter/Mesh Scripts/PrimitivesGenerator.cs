using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;

namespace Playtime_Painter {

    namespace Mesh_Primitives
    {

        public class EditableMeshPreProcess {

            public virtual bool PEGI() {
                "No configurations".nl();
                return false;
            }

        }

        public class Generate_Button : EditableMeshPreProcess
        {

            float widthPercentage = 0.1f;
            vertexpointDta[] grid;

            public override bool PEGI() {
                bool changed = false;




                return changed;
            }

            void GenerateRoundedButton() {


            }

            public override string ToString()
            {
                return "Smoothed button";
            }


        }
    }
}