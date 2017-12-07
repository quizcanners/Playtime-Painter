using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using PlayerAndEditorGUI;


[CustomEditor(typeof(GodMode))]
public class GodModeEditor: Editor {
    public override void OnInspectorGUI()  {
        ef.start(serializedObject);
        ((GodMode)target).PEGI();
        pegi.newLine();
    }
}




[CustomEditor(typeof(AtlasTextureCreator))]
public class AtlasEditorDrawer : Editor {
    public override void OnInspectorGUI() {
        ef.start(serializedObject);
        AtlasTextureCreator tmp = (AtlasTextureCreator)target;

        tmp.PEGI();

        ef.write("Assign Quad to Preview Atlas In Scene:");
        ef.newLine();
        tmp.preview = (MeshRenderer)EditorGUILayout.ObjectField(tmp.preview, typeof(MeshRenderer), true);
        ef.newLine();



    }


    
}