#if UNITY_EDITOR  && PEGI
using UnityEditor;

namespace PlaytimePainter {
    
    [CustomEditor(typeof(PainterCamera))]
    public class RenderTexturePainterEditor : PEGI_Inspector_Mono<PainterCamera> { }

    [CustomEditor(typeof(VolumeTexture))]
    public class VolumeTextureEditor : PEGI_Inspector_Mono<VolumeTexture> { }

    [CustomEditor(typeof(DepthProjectorCamera))]
    public class DepthProjectorCameraEditor : PEGI_Inspector_Mono<DepthProjectorCamera> { }
}
#endif
