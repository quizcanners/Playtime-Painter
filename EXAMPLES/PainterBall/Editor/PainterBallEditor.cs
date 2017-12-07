using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace TextureEditor {

    [CustomEditor(typeof(PainterBall))]
    public class PainterBallEditor : Editor  {



        public override void OnInspectorGUI() {
            ef.start(serializedObject);

            ((PainterBall)target).PEGI();

            ef.newLine();
        
        }
    }
}
