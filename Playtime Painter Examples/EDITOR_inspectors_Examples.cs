using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using PlaytimePainter.Examples;

namespace PlaytimePainter
{

#if !NO_PEGI && UNITY_EDITOR
    using UnityEditor;

    [CustomEditor(typeof(NoiseTextureMGMT))]
    public class NoiseTextureMGMTDrawer : PEGI_Inspector_Mono<NoiseTextureMGMT> { }

    [CustomEditor(typeof(GodMode))]
    public class GodModeDrawer : PEGI_Inspector_Mono<GodMode> {}
    
    [CustomEditor(typeof(PixelArtMeshGenerator))]
    public class PixelArtMeshGeneratorEditor : PEGI_Inspector_Mono<PixelArtMeshGenerator> { }

    [CustomEditor(typeof(LightCaster))]
    public class BakedShadowsLightProbeEditor : PEGI_Inspector_Mono<LightCaster> {}

    [CustomEditor(typeof(MergingTerrainController))]
    public class MergingTerrainEditor : PEGI_Inspector_Mono<MergingTerrainController> { }

    [CustomEditor(typeof(PainterBall))]
    public class PainterBallEditor : PEGI_Inspector_Mono<PainterBall> { }

    [CustomEditor(typeof(PaintingReceiver))]
    public class PaintingReceiverEditor : PEGI_Inspector_Mono<PaintingReceiver>  { }

    [CustomEditor(typeof(PaintWithoutComponent))]
    public class PaintWithoutComponentEditor : PEGI_Inspector_Mono<PaintWithoutComponent>  { }

    [CustomEditor(typeof(RaycastOnCollisionPainter))]
    public class PainterCasterEditor : PEGI_Inspector_Mono<RaycastOnCollisionPainter> { }

    [CustomEditor(typeof(VolumeRayTrace))]
    public class VolumeRayTraceEditor : PEGI_Inspector_Mono<VolumeRayTrace> { }

    [CustomEditor(typeof(SkinnedMeshCaster))]
    public class SkinnedMeshCasterEditor : PEGI_Inspector_Mono<SkinnedMeshCaster> { }
    
    [CustomEditor(typeof(WaterController))]
    public class WaterEditor : PEGI_Inspector_Mono<WaterController> { }

    [CustomEditor(typeof(ColorPickerHUV))]
    public class ColorPickerHUVEditor : PEGI_Inspector_Mono<ColorPickerHUV> { }

#endif
}