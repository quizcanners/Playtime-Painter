using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using PlaytimePainter.Examples;

namespace PlaytimePainter
{

#if PEGI && UNITY_EDITOR
    using UnityEditor;

    [CustomEditor(typeof(NoiseTextureMGMT))]
    public class NoiseTextureMGMTDrawer : PEGI_Inspector<NoiseTextureMGMT> { }

    [CustomEditor(typeof(GodMode))]
    public class GodModeDrawer : PEGI_Inspector<GodMode> {}
    
    [CustomEditor(typeof(PixelArtMeshGenerator))]
    public class PixelArtMeshGeneratorEditor : PEGI_Inspector<PixelArtMeshGenerator> { }

    [CustomEditor(typeof(LightCaster))]
    public class BakedShadowsLightProbeEditor : PEGI_Inspector<LightCaster> {}

    [CustomEditor(typeof(MergingTerrainController))]
    public class MergingTerrainEditor : PEGI_Inspector<MergingTerrainController> { }

    [CustomEditor(typeof(PainterBall))]
    public class PainterBallEditor : PEGI_Inspector<PainterBall> { }

    [CustomEditor(typeof(PaintingReceiver))]
    public class PaintingReceiverEditor : PEGI_Inspector<PaintingReceiver>  { }

    [CustomEditor(typeof(PaintWithoutComponent))]
    public class PaintWithoutComponentEditor : PEGI_Inspector<PaintWithoutComponent>  { }

    [CustomEditor(typeof(RaycastOnCollisionPainter))]
    public class PainterCasterEditor : PEGI_Inspector<RaycastOnCollisionPainter> { }

    [CustomEditor(typeof(VolumeRayTrace))]
    public class VolumeRayTraceEditor : PEGI_Inspector<VolumeRayTrace> { }

    [CustomEditor(typeof(SkinnedMeshCaster))]
    public class SkinnedMeshCasterEditor : PEGI_Inspector<SkinnedMeshCaster> { }
    
    [CustomEditor(typeof(WaterController))]
    public class WaterEditor : PEGI_Inspector<WaterController> { }

    [CustomEditor(typeof(ColorPickerHUV))]
    public class ColorPickerHUVEditor : PEGI_Inspector<ColorPickerHUV> { }

#endif
}