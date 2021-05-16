
using System.Collections.Generic;
using QuizCanners.Inspect;
using PlaytimePainter.CameraModules;
using PlaytimePainter.ComponentModules;
using QuizCanners.CfgDecode;
using QuizCanners.Utils;
using UnityEngine;
using static QuizCanners.Utils.ShaderProperty;

namespace PlaytimePainter {

    namespace CameraModules
    {
        
        [TaggedType(tag)]
        public class VolumePaintingCameraModule : CameraModuleBase, 
            IPainterManagerModuleComponentPEGI, IPainterManagerModuleBrush, IPainterManagerModuleGizmos,
            IUseDepthProjector, IUseReplacementCamera
        {
            private const string tag = "VolumePntng";
            public override string ClassTag => tag;

            public static VectorValue VOLUME_H_SLICES = new VectorValue("VOLUME_H_SLICES");
            public static VectorValue VOLUME_POSITION_N_SIZE = new VectorValue("VOLUME_POSITION_N_SIZE");

            public static VectorValue VOLUME_H_SLICES_BRUSH = new VectorValue("VOLUME_H_SLICES_BRUSH");
            public static VectorValue VOLUME_POSITION_N_SIZE_BRUSH = new VectorValue("VOLUME_POSITION_N_SIZE_BRUSH");

            public static VectorValue VOLUME_BRUSH_DIRECTION = new VectorValue("VOLUME_BRUSH_DYRECTION");

            public static ShaderKeyword UseSmoothing = new ShaderKeyword("_SMOOTHING");

            private float smoothing;

            private static readonly ShaderName _preview = new ShaderName("Playtime Painter/Editor/Preview/Volume");
            private static readonly ShaderName _brushDoubleBuffer = new ShaderName("Playtime Painter/Editor/Brush/Volume");
            private static readonly ShaderName _brushSingleBuffer = new ShaderName("Playtime Painter/Editor/Brush/Single Buffer/Volume");
            private static readonly ShaderName _brushShaderForRayTrace = new ShaderName("Playtime Painter/Editor/Brush/Volume_RayTrace");

            #region Encode & Decode

            public override CfgEncoder Encode()
            {
                var cody = base.Encode() //this.EncodeUnrecognized()
                    .Add_IfTrue("rtr", _enableRayTracing)
                    .Add("mFiv", minFov)
                    .Add("mFov", maxFov)
                    .Add_IfNotEpsilon("smth", smoothing)
                    .Add("cam", rayTraceCameraConfiguration);

                return cody;
            }

            public override void Decode(string key, CfgData data)
            {
                switch (key)
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
                        rayTraceCameraConfiguration.DecodeFull(data);
                        break;
                }
            }

            #endregion

            public static VolumePaintingCameraModule _inst;

            public override void Enable()
            {
                base.Enable();
                _inst = this;
            }

            public Shader GetPreviewShader(PlaytimePainter p) => p.TexMeta.isAVolumeTexture ? _preview.Shader : null;

            public Shader GetBrushShaderDoubleBuffer(PlaytimePainter p) =>
                p.TexMeta.isAVolumeTexture ? (_enableRayTracing ? _brushShaderForRayTrace.Shader : _brushDoubleBuffer.Shader) : null;

            public Shader GetBrushShaderSingleBuffer(PlaytimePainter p) => p.TexMeta.isAVolumeTexture ? _brushSingleBuffer.Shader : null;

            public bool IsA3DBrush(PlaytimePainter painter, Brush bc, ref bool overrideOther)
            {
                if (!painter.TexMeta.isAVolumeTexture) return false;
                overrideOther = true;
                return true;
            }

            public void PaintPixelsInRam(PaintCommand.UV command)
            {

                Stroke stroke = command.Stroke;
                float brushAlpha = command.strokeAlphaPortion;
                TextureMeta image = command.TextureData;
                Brush bc = command.Brush;

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
                Stroke stroke = command.Stroke;
                TextureMeta image = command.TextureData;
                Brush bc = command.Brush;

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
                    PaintRenderTextureInternal(command); // Maybe wrong

            }

            public bool PaintRenderTextureInternal(PaintCommand.UV cfg)
            {
                var stroke = cfg.Stroke;
                var image = cfg.TextureData;
                var bc = cfg.Brush;

                var vt = cfg.TextureData.GetVolumeTextureData(); //painter.GetVolumeTexture();
                stroke.posFrom = stroke.posTo;

                BrushTypes.Sphere.Inst.BeforeStroke(cfg); //bc, stroke, painter);

                VOLUME_POSITION_N_SIZE_BRUSH.GlobalValue = vt.PosSize4Shader;
                VOLUME_H_SLICES_BRUSH.GlobalValue = vt.Slices4Shader;
                VOLUME_BRUSH_DIRECTION.GlobalValue = stroke.collisionNormal.ToVector4(smoothing);

                UseSmoothing.Enabled = smoothing > 0;

                image.useTexCoord2 = false;
                cfg.strokeAlphaPortion = Mathf.Clamp01(bc.Flow * 0.05f);
                TexMGMT.SHADER_STROKE_SEGMENT_UPDATE(cfg); 

                stroke.SetWorldPosInShader();

                RenderTextureBuffersManager.Blit(null, image.CurrentRenderTexture(),
                    TexMGMT.brushRenderer.GetMaterial().shader);

                BrushTypes.Sphere.Inst.AfterStroke(cfg); 

                return true;
            }

            #region Ray Tracing

            private bool _enableRayTracing;

            private float minFov = 60;

            private float maxFov = 170;

            //private float arbitraryBrightnessIncrease = 1.5f;

            private PaintCommand.UV delayedPaintingConfiguration;

            private static readonly ProjectorCameraConfiguration rayTraceCameraConfiguration = new ProjectorCameraConfiguration();

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

                PainterCamera.GetOrCreateProjectorCamera();

                GlobalBrush.Color = avg / pixelsCount;

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

            private float BrushScaleMaxForCpu(VolumeTexture volTex) => volTex.size * volTex.Width * 0.025f;

            public override string NameForDisplayPEGI() => "Volume Painting";

            public bool ComponentInspector()
            {
                if (!InspectedPainter)
                {
                    "No inspected Painter found".writeWarning();
                    return false;
                }

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

            public bool BrushConfigPEGI(Brush br)
            {

                var changed = pegi.ChangeTrackStart();

                var p = InspectedPainter;

                var volTex = p.TexMeta.GetVolumeTextureData();

                if (volTex)
                {

                    var tex = volTex.Texture;

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

                    var id = p.TexMeta;

                    var cpuBlit = id.TargetIsTexture2D().nl();

                    br.showingSize = !_enableRayTracing || cpuBlit;

                    if (!cpuBlit)
                    {

                        if (Brush.showAdvanced || _enableRayTracing)
                        {
                            "Ray-Tracing".toggleIcon(ref _enableRayTracing, true);

                            if (br.useAlphaBuffer)
                                icon.Warning.draw(
                                    "Ray Tracing doesn't use Alpha buffer. Alpha buffer will be automatically disabled");

                        }

                        if ("Ray Trace Camera".isConditionally_Entered(
                            _enableRayTracing && PainterCamera.depthProjectorCamera,
                            ref _exploreRayTaceCamera).nl_ifFoldedOut())
                        {

                            "Min".edit(40, ref minFov, 60, maxFov - 1).nl();

                            "Max".edit(40, ref maxFov, minFov + 1, 170).nl();

                            rayTraceCameraConfiguration.Nested_Inspect().nl();

                        }

                        if (smoothing > 0 || Brush.showAdvanced)
                        {
                            pegi.nl();
                            "Smoothing".edit(70, ref smoothing, 0, 1);
                            pegi.FullWindow.DocumentationClickOpen("Best used in the end");

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
                                "Light Projectors paused".toggleIcon(ref dp.pauseAutoUpdates).nl();
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
                        .isFoldout(ref _exploreVolumeData).nl())
                        volTex.Nested_Inspect();

                    if (!cpuBlit)
                        MsgPainter.Hardness.GetText()
                            .edit(MsgPainter.Hardness.GetDescription(), 70, ref br.hardness, 1f, 5f).nl();

                    var tmpSpeed = br._dFlow.Value;
                    if (MsgPainter.Flow.GetText().edit(40, ref tmpSpeed, 0.01f, 4.5f).nl())
                        br._dFlow.Value = tmpSpeed;

                    if (br.showingSize)
                    {

                        var maxScale = volTex.size * volTex.Width * 4;

                        "Scale:".edit(40, ref br.brush3DRadius, 0.001f * maxScale, maxScale * 0.5f);

                        if (cpuBlit && !_brushShaderForRayTrace.Shader && br.brush3DRadius > BrushScaleMaxForCpu(volTex))
                            icon.Warning.draw(
                                "Size will be reduced when panting due to low performance of the CPU brush for volumes");

                    }

                    pegi.nl();
                }

                return changed;
            }

            private int _exploredVolume;

            public void Inspect()
            {
                "Volumes".edit_List(VolumeTexture.all, ref _exploredVolume);
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

            public override bool SetTextureOnMaterial(ShaderProperty.TextureValue field, TextureMeta id)
            {
                if (!volumeTexture)
                    return false;

                if (field.ToString() == volumeTexture.name)
                {
                    volumeTexture.ImageMeta = id;
                    return true;
                }

                return false;
            }
            
            public override void GetNonMaterialTextureNames(ref List<TextureValue> dest)
            {
                if (expectingAVolume && !volumeTexture)
                {
                    volumeTexture = painter.gameObject.GetComponent<VolumeTexture>();
                    expectingAVolume = false;
                }

                if (volumeTexture)
                    dest.Add(volumeTexture.TextureInShaderProperty);
            }

            public override bool GetTexture(TextureValue field, ref Texture tex)
            {
                if (volumeTexture && field.Equals(volumeTexture.TextureInShaderProperty))
                {
                    tex = volumeTexture.Texture;
                    return true;
                }

                return false;
            }

            public override bool UpdateTilingFromMaterial(TextureValue fieldName)
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

         

            #region Encode & Decode

            public override CfgEncoder Encode() => new CfgEncoder()
                .Add("b", base.Encode)
                .Add_IfTrue("gotVol", volumeTexture);

            public override void Decode(string key, CfgData data)
            {
                switch (key)
                {
                    case "b": data.Decode(base.Decode); break;//data.DecodeInto(base.Decode); break;
                    case "gotVol": expectingAVolume = data.ToBool(); break;
                }
            }
            #endregion


            public override void Inspect()
            {
                "Volume Texture:".edit(ref volumeTexture).nl();
            }


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