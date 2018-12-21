using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
#if UNITY_EDITOR  && PEGI
using UnityEditor;

namespace Playtime_Painter {
    
    [CustomEditor(typeof(PainterCamera))]
    public class RenderTexturePainterEditor : Editor {
        public override void OnInspectorGUI() => ((PainterCamera)target).Inspect(serializedObject);
    }

    [CustomEditor(typeof(VolumeTexture))]
    public class VolumeTextureEditor : Editor {
        public override void OnInspectorGUI() => ((VolumeTexture)target).Inspect(serializedObject);
    }
}
#endif
