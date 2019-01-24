using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using Playtime_Painter.Examples;

namespace Playtime_Painter
{

#if PEGI && UNITY_EDITOR
    using UnityEditor;


    [CustomEditor(typeof(GodMode))]
    public class GodModeDrawer : PEGI_Editor<GodMode> {}
    
    [CustomEditor(typeof(PixelPerfectUVupdate))]
    public class PixelPerfectUVupdateEditor : PEGI_Editor<PixelPerfectUVupdate> { }

    [CustomEditor(typeof(PixelArtMeshGenerator))]
    public class PixelArtMeshGeneratorEditor : PEGI_Editor<PixelArtMeshGenerator> { }

    [CustomEditor(typeof(LightCaster))]
    public class BakedShadowsLightProbeEditor : PEGI_Editor<LightCaster> {}

    [CustomEditor(typeof(MergingTerrainController))]
    public class MergingTerrainEditor : PEGI_Editor<MergingTerrainController> { }

    [CustomEditor(typeof(PainterBall))]
    public class PainterBallEditor : PEGI_Editor<PainterBall> { }

    [CustomEditor(typeof(PaintingReciever))]
    public class PaintingRecieverEditor : PEGI_Editor<PaintingReciever>  { }

    [CustomEditor(typeof(PaintWithoutComponent))]
    public class PaintWithoutComponentEditor : PEGI_Editor<PaintWithoutComponent>  { }

    [CustomEditor(typeof(RaycastOnCollisionPainter))]
    public class PainterCasterEditor : PEGI_Editor<RaycastOnCollisionPainter> { }

    [CustomEditor(typeof(ShadowVolumeTexture))]
    public class ShadowVolumeTextureEditor : PEGI_Editor<ShadowVolumeTexture> { }

    [CustomEditor(typeof(SkinnedMeshCaster))]
    public class SkinnedMeshCasterEditor : PEGI_Editor<SkinnedMeshCaster> { }
    
    [CustomEditor(typeof(WaterController))]
    public class WaterEditor : PEGI_Editor<WaterController> { }

#endif
}