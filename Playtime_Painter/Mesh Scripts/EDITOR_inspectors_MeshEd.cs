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
    
    [CustomEditor(typeof(PainterManager))]
    public class RenderTexturePainterEditor : Editor
    {
        public override void OnInspectorGUI() => ((PainterManager)target).Inspect(serializedObject);
    }
}
#endif
