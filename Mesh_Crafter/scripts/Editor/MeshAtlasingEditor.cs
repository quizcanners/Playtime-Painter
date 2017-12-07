using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using MeshEditingTools;

[CustomEditor(typeof(MeshAtlasing))]
public class MeshAtlasingEditor : Editor
{

    public override void OnInspectorGUI()  {
        ef.start(serializedObject);
        MeshAtlasing tmp = (MeshAtlasing)target;
        this.edit(CsharpFuncs.GetMemberName(() => tmp.a_configs)); 
        ef.newLine();
        ef.write("Material:");
        ef.edit(ref tmp.terget);

        tmp.EditorPEGI();

        ef.newLine();

    }
}