using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;

#if UNITY_EDITOR  && PEGI
using UnityEditor;

namespace Playtime_Painter
{
    [CustomEditor(typeof(PixelArtMeshGenerator))]
    public class PixelArtMeshGeneratorEditor : Editor
    {
        public override void OnInspectorGUI() => ((PixelArtMeshGenerator)target).Inspect(serializedObject);
    }
    
    [CustomEditor(typeof(PainterCamera))]
    public class RenderTexturePainterEditor : Editor
    {
        public override void OnInspectorGUI() => ((PainterCamera)target).Inspect(serializedObject);
    }

   /* [CustomEditor(typeof(PainterManagerDataHolder ))]
    public class PainterManagerDataHolderEditor : Editor
    {
        public override void OnInspectorGUI() => ((PainterManagerDataHolder)target).Inspect_so(serializedObject);
    }*/

}
#endif
