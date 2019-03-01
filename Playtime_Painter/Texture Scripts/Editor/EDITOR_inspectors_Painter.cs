﻿using PlayerAndEditorGUI;

#if UNITY_EDITOR  && PEGI
using UnityEditor;

namespace Playtime_Painter {
    
    [CustomEditor(typeof(PainterCamera))]
    public class RenderTexturePainterEditor : PEGI_Inspector<PainterCamera> { }

    [CustomEditor(typeof(VolumeTexture))]
    public class VolumeTextureEditor : PEGI_Inspector<VolumeTexture> { }

    [CustomEditor(typeof(DepthProjectorCamera))]
    public class DepthProjectorCameraEditor : PEGI_Inspector<DepthProjectorCamera> { }
}
#endif
