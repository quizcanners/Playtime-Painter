using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using PlayerAndEditorGUI;
using Painter;

public class BevelMaterialInspector : MaterialEditor
{
    public override void OnInspectorGUI()
    {
      

        if (!isVisible)
            return;

  
        Material mat = target as Material;

        ef.start(serializedObject);

        string[] keyWords = mat.shaderKeywords;

        var atlased = mat.shaderKeywords.Contains(PainterConfig.UV_ATLASED);


        var changed = false;

        changed |= mat.editTexture("_MainTex", atlased ? "MainTex Atlas" : "MainTex");

       // DefaultShaderProperty(targetMat.shade , 0);

        if (changed)
        EditorUtility.SetDirty(mat);

        ef.end();
    }
}
