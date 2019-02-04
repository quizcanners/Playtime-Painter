using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
#if UNITY_EDITOR  && PEGI
using UnityEditor;

namespace Playtime_Painter {
    
    [CustomEditor(typeof(PainterCamera))]
    public class RenderTexturePainterEditor : PEGI_Inspector<PainterCamera> { }

    [CustomEditor(typeof(VolumeTexture))]
    public class VolumeTextureEditor : PEGI_Inspector<VolumeTexture> { }
}
#endif
