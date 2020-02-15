
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using PlaytimePainter.CameraModules;
using PlaytimePainter.ComponentModules;

namespace PlaytimePainter { 

    using VectorValue = ShaderProperty.VectorValue;
    using ShaderKeyword = ShaderProperty.ShaderKeyword;

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration

    namespace CameraModules
    {
        
        [TaggedType(tag)]
        public class VolumePaintingCameraModule : CameraModuleBase, IGotDisplayName,
            IPainterManagerModuleComponentPEGI, IPainterManagerModuleBrush, IPainterManagerModuleGizmis,
            IUseDepthProjector, IUseReplacementCamera
        {

            const string tag = "VolumePntng";
            public override string ClassTag => tag;

            public static VectorValue VOLUME_H_SLICES = new VectorValue("VOLUME_H_SLICES");
            public static VectorValue VOLUME_POSITION_N_SIZE = new VectorValue("VOLUME_POSITION_N_SIZE");

            public static VectorValue VOLUME_H_SLICES_BRUSH = new VectorValue("VOLUME_H_SLICES_BRUSH");
            public static VectorValue VOLUME_POSITION_N_SIZE_BRUSH = new VectorValue("VOLUME_POSITION_N_SIZE_BRUSH");

            public static VectorValue VOLUME_BRUSH_DIRECTION = new VectorValue("VOLUME_BRUSH_DYRECTION");

            public static ShaderKeyword UseSmoothing = new ShaderKeyword("_SMOOTHING");

            private float smoothing = 0;

            private static Shader _preview;
            private static Shader _brush;
            private static Shader _brushShaderFroRayTrace;

            #region Encode & Decode

            public override CfgEncoder Encode()
            {
                var cody = this.EncodeUnrecognized()
                    .Add_IfTrue("rtr", _enableRayTracing)
                    .Add("mFiv", minFov)
                    .Add("mFov", maxFov)
                    .Add_IfNotEpsilon("smth", smoothing)
                    .Add("cam", rayTraceCameraConfiguration);

                return cody;
            }

            public override bool Decode(string tg, string data)
            {
                switch (tg)
                {
                    case "rtr":
                        _enableRayTracing = true;
                        break;
                    case "mFiv":
                        minFov = data.ToFloat();
                        break;
                    case "mFov":
                        maxFov = data.ToFloat();
                        break;
                    case "smth":
                        smoothing = data.ToFloat();
                        break;
                    case "cam":
                        rayTraceCameraConfiguration.Decode(data);
                        break;
                    default: return false;
                }

                return true;
            }

            #endregion

            public static VolumePaintingCameraModule _inst;

            public override void Enable()
            {
                base.Enable();
                _inst = this;

                if (!_preview)
                    _preview = Shader.Find("Playtime Painter/Editor/Preview/Volume");

                if (!_brush)
                    _brush = Shader.Find("Playtime Painter/Editor/Brush/Volume");

                if (!_brushShaderFroRayTrace)
                    _brushShaderFroRayTrace = Shader.Find("Playtime Painter/Editor/Brush/Volume_RayTrace");
            }

            public Shader GetPreviewShader(PlaytimePainter p) => p.TexMeta.isAVolumeTexture ? _preview : null;

            public Shader GetBrushShaderDoubleBuffer(PlaytimePainter p) =>
                p.TexMeta.isAVolumeTexture ? (_enableRayTracing ? _brushShaderFroRayTrace : _brush) : null;

            public Shader GetBrushShaderSingleBuffer(PlaytimePainter p) => null;

            public bool IsA3DBrush(PlaytimePainter painter, Brush bc, ref bool overrideOther)
            {
                if (!painter.TexMeta.isAVolumeTexture) return false;
                overrideOther = true;
                return true;
            }

            public void PaintPixelsInRam(PaintCommand.UV command)
            {

                Stroke stroke = command.stroke;
                float brushAlpha = command.strokeAlphaPortion;
                TextureMeta image = command.textureData;
                Brush bc = command.brush;

                var volume = image.texture2D.GetVolumeTextureData();

                if (!volume) return;

                bc.brush3DRadius = Mathf.Min(BrushScaleMaxForCpu(volume), bc.brush3DRadius);

                var volumeScale = volume.size;

                var pos = (stroke.posFrom - volume.transform.position) / volumeScale + 0.5f * Vector3.one;

                var height = volume.Height;
                var texWidth = image.width;

                BlitFunctions.brAlpha = brushAlpha;
                bc.PrepareCpuBlit(image);
                BlitFunctions.half = bc.Size(true) / volumeScale;

                var pixels = image.Pixels;

                var iHalf = (int) (BlitFunctions.half - 0.5f);
                var smooth = bc.GetBrushType(true) != BrushTypes.Pixel.Inst;
                if (smooth) iHalf += 1;

                BlitFunctions.alphaMode = BlitFunctions.SphereAlpha;

                var sliceWidth = texWidth / volume.hSlices;

                var hw = sliceWidth / 2;

                var y = (int) pos.y;
                var z = (int) (pos.z + hw);
                var x = (int) (pos.x + hw);

                for (BlitFunctions.y = -iHalf; BlitFunctions.y < iHalf + 1; BlitFunctions.y++)
                {

                    var h = y + BlitFunctions.y;

                    if (h >= height)
                        return;

                    if (h < 0) continue;
                    var hy = h / volume.hSlices;
                    var hx = h % volume.hSlices;
                    var hTexIndex = (hy * texWidth + hx) * sliceWidth;

                    for (BlitFunctions.z = -iHalf; BlitFunctions.z < iHalf + 1; BlitFunctions.z++)
                    {

                        var trueZ = z + BlitFunctions.z;

                        if (trueZ < 0 || trueZ >= sliceWidth) continue;
                        var yTexIndex = hTexIndex + trueZ * texWidth;

                        for (BlitFunctions.x = -iHalf; BlitFunctions.x < iHalf + 1; BlitFunctions.x++)
                        {
                            if (!BlitFunctions.alphaMode()) continue;
                            var trueX = x + BlitFunctions.x;

                            if (trueX < 0 || trueX >= sliceWidth) continue;
                            var texIndex = yTexIndex + trueX;
                            BlitFunctions.blitMode(ref pixels[texIndex]);
                        }
                    }
                }

            }

            public bool IsEnabledFor(PlaytimePainter painter, TextureMeta img, Brush cfg) =>
                img.GetVolumeTextureData();

            public void PaintRenderTextureUvSpace(PaintCommand.UV command) 
            {
                Stroke stroke = command.stroke;
                TextureMeta image = command.textureData;
                Brush bc = command.brush;

                var vt = image.GetVolumeTextureData();

                if (!vt)
                {
                    Debug.LogError("Painted volume was not found");
                    return;
                }

                if (_enableRayTracing)
                {

                    rayTraceCameraConfiguration.From(stroke);

                    bc.useAlphaBuffer = false;

                    delayedPaintingConfiguration = new PaintCommand.UV(stroke, image, bc);

                    PainterCamera.GetOrCreateProjectorCamera().RenderRightNow(this);
                }
                else
                    PaintRenderTextureUvSpace(command); 

            }

            public bool PaintRenderTextureInternal(PaintCommand.UV cfg)
            {
                var stroke = cfg.stroke;
                var image = cfg.textureData;
                var painter = cfg.painter;
                var bc = cfg.brush;

                var vt = cfg.textureData.GetVolumeTextureData(); //painter.GetVolumeTexture();
                stroke.posFrom = stroke.posTo;

                BrushTypes.Sphere.Inst.BeforeStroke(cfg); //bc, stroke, painter);

                VOLUME_POSITION_N_SIZE_BRUSH.GlobalValue = vt.PosSize4Shader;
                VOLUME_H_SLICES_BRUSH.GlobalValue = vt.Slices4Shader;
                VOLUME_BRUSH_DIRECTION.GlobalValue = stroke.collisionNormal.ToVector4(smoothing);

                UseSmoothing.Enabled = smoothing > 0;

                image.useTexCoord2 = false;
                //bool alphaBuffer;
                cfg.strokeAlphaPortion = bc.Speed * 0.05f;
                TexMGMT.SHADER_STROKE_SEGMENT_UPDATE(cfg); //bc, bc.Speed * 0.05f, image, stroke, out alphaBuffer, painter);

                stroke.SetWorldPosInShader();

                RenderTextureBuffersManager.Blit(null, image.CurrentRenderTexture(),
                    TexMGMT.brushRenderer.GetMaterial().shader);

                BrushTypes.Sphere.Inst.AfterStroke_Painter(cfg); //painter, bc, stroke, alphaBuffer, image);

                return true;
            }

            #region Ray Tracing

            private bool _enableRayTracing;

            private float minFov = 60;

            private float maxFov = 170;

            //private float arbitraryBrightnessIncrease = 1.5f;

            private PaintCommand.UV delayedPaintingConfiguration;

            private static ProjectorCameraConfiguration
                rayTraceCameraConfiguration = new ProjectorCameraConfiguration();

            public bool ProjectorReady() => delayedPaintingConfiguration != null;

            public CameraMatrixParameters GetGlobalCameraMatrixParameters() => null;

            public ProjectorCameraConfiguration GetProjectorCameraConfiguration()
            {
                rayTraceCameraConfiguration.fieldOfView = minFov + Random.Range(0, maxFov - minFov);
                return rayTraceCameraConfiguration;
            }

            public void AfterCameraRender(RenderTexture texture)
            {

                var size = RenderTextureBuffersManager.tinyTextureSize;

                int pixelsCount = size * size;

                var tiny = RenderTextureBuffersManager.GetDownscaleOf(texture,
                    RenderTextureBuffersManager.tinyTextureSize, true);

                var pix = RenderTextureBuffersManager.GetMinSizeTexture().CopyFrom(tiny).GetPixels();

                Color avg = Color.black;

                foreach (var p in pix)
                    avg += p;

                var pcam = PainterCamera.GetOrCreateProjectorCamera();

                GlobalBrush.Color = avg / (float) pixelsCount;

                PainterShaderVariables.BrushColorProperty.GlobalValue = GlobalBrush.Color;

                PaintRenderTextureUvSpace(delayedPaintingConfiguration);

                delayedPaintingConfiguration = null;
            }

            public RenderTexture GetTargetTexture() => RenderTextureBuffersManager.GetRenderTextureWithDepth();

            public DepthProjectorCamera.Mode GetMode() => DepthProjectorCamera.Mode.ReplacementShader;

            public string ProjectorTagToReplace() => "RenderType";

            public Shader ProjectorShaderToReplaceWith() => Cfg.rayTraceOutput;

            public Color CameraReplacementClearColor() => new Color(0, 0, 1, 1);

            #endregion

            #region Inspector

            float BrushScaleMaxForCpu(VolumeTexture volTex) => volTex.size * volTex.Width * 0.025f;

            public override string NameForDisplayPEGI() => "Volume Painting";

            public bool ComponentInspector()
            {
                var vt = InspectedPainter.GetModule<VolumeTextureComponentModule>().volumeTexture;

                if (!vt)
                    return false;

                var id = vt.ImageMeta;

                if (id == null)
                {
                    "Volume has no texture".writeWarning();
                    return false;
                }

                return true;
            }

            private bool _exploreVolumeData;

            private bool _exploreRayTaceCamera;

            public bool BrushConfigPEGI(ref bool overrideBlitMode, Brush br)
            {

                var changed = false;

                var p = InspectedPainter;

                var volTex = p.TexMeta.GetVolumeTextureData();

                if (volTex)
                {

                    var tex = volTex.texture;

                    if (tex)
                    {

                        "Volume is a {0} texture".F(tex.IsColorTexture() ? "Color" : "Non-Color Data").write();

#if UNITY_EDITOR
                        if (tex.IsColorTexture())
                        {
                            pegi.nl();
                            var imp = tex.GetTextureImporter();

                            if ((imp != null) && "FIX texture".Click() && (imp.WasWrongIsColor(false)))
                                imp.SaveAndReimport();
                        }
#endif


                        pegi.nl();
                    }
                    else
                        "Volume has no texture".writeWarning();

                    overrideBlitMode = true;

                    var id = p.TexMeta;

                    var cpuBlit = id.TargetIsTexture2D().nl();

                    br.showingSize = !_enableRayTracing || cpuBlit;

                    if (!cpuBlit)
                    {

                        if (Brush.showAdvanced || _enableRayTracing)
                        {
                            "Ray-Tracing".toggleIcon(ref _enableRayTracing, true).changes(ref changed);

                            if (br.useAlphaBuffer)
                                icon.Warning.write(
                                    "Ray Tracing doesn't use Alpha buffer. Alpha buffer will be automatically disabled");

                        }

                        if ("Ray Trace Camera".conditional_enter(
                            _enableRayTracing && PainterCamera.depthProjectorCamera,
                            ref _exploreRayTaceCamera).nl_ifFoldedOut())
                        {

                            "Min".edit(40, ref minFov, 60, maxFov - 1).nl(ref changed);

                            "Max".edit(40, ref maxFov, minFov + 1, 170).nl(ref changed);

                            rayTraceCameraConfiguration.Nested_Inspect().nl(ref changed);

                        }

                        if (smoothing > 0 || Brush.showAdvanced)
                        {
                            pegi.nl();
                            "Smoothing".edit(70, ref smoothing, 0, 1).changes(ref changed);
                            "Best used in the end".fullWindowDocumentationClickOpen();

                            pegi.nl();
                        }

                        if (!_exploreRayTaceCamera && _enableRayTracing)
                        {
                            var dp = PainterCamera.depthProjectorCamera;

                            if (!dp)
                            {
                                if ("Create Projector Camera".Click().nl())
                                    PainterCamera.GetOrCreateProjectorCamera();
                            }
                            else if (dp.pauseAutoUpdates)
                            {
                                pegi.nl();
                                "Light Projectors paused".toggleIcon(ref dp.pauseAutoUpdates).nl(ref changed);
                            }

                            pegi.nl();

                        }
                    }

                    if (!cpuBlit)
                    {
                        pegi.nl();

                        if (!br.GetBrushType(false).IsAWorldSpaceBrush)
                        {
                            "Only World space brush can edit volumes".writeHint();
                            pegi.nl();
                            if ("Change to Sphere brush".Click())
                                br.SetBrushType(false, BrushTypes.Sphere.Inst);
                        }
                    }

                    pegi.nl();


                    if (!_exploreRayTaceCamera && PainterCamera.Data.showVolumeDetailsInPainter &&
                        (volTex.name + " " + VolumeEditingExtensions.VolumeSize(id.texture2D, volTex.hSlices))
                        .foldout(ref _exploreVolumeData).nl())
                    {

                        volTex.Nested_Inspect().changes(ref changed);

                        if (volTex.NeedsToManageMaterials)
                        {
                            var painterMaterial = InspectedPainter.Material;
                            if (painterMaterial)
                            {
                                if (!volTex.materials.Contains(painterMaterial))
                                    if ("Add This Material".Click().nl())
                                        volTex.AddIfNew(p);
                            }
                        }
                    }

                    if (!cpuBlit)
                        MsgPainter.Hardness.GetText()
                            .edit(MsgPainter.Hardness.GetDescription(), 70, ref br.hardness, 1f, 5f).nl(ref changed);

                    var tmpSpeed = br._dSpeed.Value;
                    if (MsgPainter.Speed.GetText().edit(40, ref tmpSpeed, 0.01f, 4.5f).nl(ref changed))
                        br._dSpeed.Value = tmpSpeed;

                    if (br.showingSize)
                    {

                        var maxScale = volTex.size * volTex.Width * 4;

                        "Scale:".edit(40, ref br.brush3DRadius, 0.001f * maxScale, maxScale * 0.5f)
                            .changes(ref changed);

                        if (cpuBlit && !_brushShaderFroRayTrace && br.brush3DRadius > BrushScaleMaxForCpu(volTex))
                            icon.Warning.write(
                                "Size will be reduced when panting due to low performance of the CPU brush for volumes");

                    }

                    pegi.nl();
                }

                return changed;
            }

            private int _exploredVolume;

            public override bool Inspect()
            {
                var changes = false;

                "Volumes".edit_List(ref VolumeTexture.all, ref _exploredVolume).changes(ref changes);

                return changes;
            }

            public bool PlugIn_PainterGizmos(PlaytimePainter painter)
            {
                var volume = painter.TexMeta.GetVolumeTextureData();

                if (volume && !painter.LockTextureEditing)
                    return volume.DrawGizmosOnPainter(painter);

                return false;
            }


            #endregion

        }
    }

    namespace ComponentModules
    {
        [TaggedType(Tag)]
        public class VolumeTextureComponentModule : ComponentModuleBase
        {
            private const string Tag = "VolTexM";
            public override string ClassTag => Tag;

            public VolumeTexture volumeTexture;

            private bool expectingAVolume;

            public override void OnComponentDirty()
            {

            }

            public override void GetNonMaterialTextureNames(ref List<ShaderProperty.TextureValue> dest)
            {
                if (expectingAVolume && !volumeTexture)
                {
                    volumeTexture = painter.gameObject.GetComponent<VolumeTexture>();
                    expectingAVolume = false;
                }

                if (volumeTexture)
                    dest.Add(volumeTexture.TextureInShaderProperty);
            }

            public override bool GetTexture(ShaderProperty.TextureValue field, ref Texture tex)
            {
                if (volumeTexture && field.Equals(volumeTexture.TextureInShaderProperty))
                {
                    tex = volumeTexture.texture;
                    return true;
                }

                return false;
            }

            public override bool UpdateTilingFromMaterial(ShaderProperty.TextureValue fieldName)
            {
                if (!volumeTexture)
                    return false;

                var id = painter.TexMeta;
                if (id == null || !id.isAVolumeTexture)
                    return false;
                id.tiling = Vector2.one;
                id.offset = Vector2.zero;
                return true;
            }

            public override bool Inspect()
            {
                "Volume Texture:".edit(ref volumeTexture).nl();

                return false;
            }

            #region Encode & Decode

            public override CfgEncoder Encode() => new CfgEncoder()
                .Add("b", base.Encode)
                .Add_IfTrue("gotVol", volumeTexture);

            public override bool Decode(string tg, string data)
            {
                switch (tg)
                {
                    case "b": base.Decode(data); break;
                    case "gotVol": expectingAVolume = data.ToBool(); break;
                    default: return false;
                }

                return true;
            }
            #endregion

        }
    }

    public static class VolumeEditingExtensions
    {
        
        public static Vector3 VolumeSize(Texture2D tex, int slices)
        {
            var w = tex.width / slices;
            return new Vector3(w, slices * slices, w);
        }

        public static void SetVolumeTexture(this IEnumerable<Material> materials, VolumeTexture vt) {
            if (!vt)
                return;
            var imd = vt.ImageMeta;

            if (imd == null)
                return;

            imd.isAVolumeTexture = true;

            var pnS = vt.PosSize4Shader;
            var vhS = vt.Slices4Shader;
            var tex = vt.TextureInShaderProperty;
            
            foreach (var m in materials)
                if (m)
                {
                    VolumePaintingCameraModule.VOLUME_POSITION_N_SIZE.SetOn(m, pnS);
                    VolumePaintingCameraModule.VOLUME_H_SLICES.SetOn(m, vhS);
                    tex.SetOn(m, imd.CurrentTexture());
                }
        }

        public static VolumeTexture GetVolumeTextureData(this Texture tex) => GetVolumeTextureData(tex.GetTextureMeta());
        
        private static VolumeTexture _lastFetchedVt;

        public static VolumeTexture GetVolumeTextureData(this TextureMeta id)
        {
            if (VolumePaintingCameraModule._inst == null || id == null)
                return null;

            if (_lastFetchedVt != null && _lastFetchedVt.ImageMeta != null && _lastFetchedVt.ImageMeta == id)
                return _lastFetchedVt;

            for (var i = 0; i < VolumeTexture.all.Count; i++)
            {
                var vt = VolumeTexture.all[i];
                if (!vt) {
                    VolumeTexture.all.RemoveAt(i);
                    i--;
                }
                else if (vt.ImageMeta != null && id == vt.ImageMeta)
                {
                    _lastFetchedVt = vt;
                    id.isAVolumeTexture = true;
                    return vt;
                }
            }

            return null;
        }

    }
}