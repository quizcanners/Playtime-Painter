
using System.Collections.Generic;
using QuizCanners.Inspect;
using PainterTool.CameraModules;
using PainterTool.ComponentModules;
using QuizCanners.Migration;
using QuizCanners.Utils;
using UnityEngine;
using static QuizCanners.Utils.ShaderProperty;

namespace PainterTool {

    namespace CameraModules
    {
        
        [TaggedTypes.Tag(CLASS_KEY)]
        internal class VolumePaintingCameraModule : CameraModuleBase, IPEGI,
            IPainterManagerModuleComponentPEGI, IPainterManagerModuleBrush, IPainterManagerModuleGizmos,
            IUseDepthProjector //, IUseReplacementCamera
        {
            private const string CLASS_KEY = "VolumePntng";
            public override string ClassTag => CLASS_KEY;

            public static VectorValue VOLUME_H_SLICES = new ("VOLUME_H_SLICES");
            public static VectorValue VOLUME_POSITION_N_SIZE = new ("VOLUME_POSITION_N_SIZE");

            public static VectorValue VOLUME_H_SLICES_BRUSH = new ("VOLUME_H_SLICES_BRUSH");
            public static VectorValue VOLUME_POSITION_N_SIZE_BRUSH = new ("VOLUME_POSITION_N_SIZE_BRUSH");

            public static VectorValue VOLUME_BRUSH_DIRECTION = new ("VOLUME_BRUSH_DYRECTION");

            public static GlobalFeature UseSmoothing = new ("_SMOOTHING");

            private float smoothing;

            private static readonly ShaderName _preview = new ("Playtime Painter/Editor/Preview/Volume");
            private static readonly ShaderName _brushDoubleBuffer = new ("Playtime Painter/Editor/Brush/Volume");
            private static readonly ShaderName _brushSingleBuffer = new ("Playtime Painter/Editor/Brush/Single Buffer/Volume");
            private static readonly ShaderName _brushShaderForRayTrace = new ("Playtime Painter/Editor/Brush/Volume_RayTrace");

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

            public override void DecodeTag(string key, CfgData data)
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
                        rayTraceCameraConfiguration.Decode(data);
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

            public Shader GetPreviewShader(PainterComponent p) => p.TexMeta.IsAVolumeTexture ? _preview.Shader : null;

            public Shader GetBrushShaderDoubleBuffer(PainterComponent p) =>
                p.TexMeta.IsAVolumeTexture ? (_enableRayTracing ? _brushShaderForRayTrace.Shader : _brushDoubleBuffer.Shader) : null;

            public Shader GetBrushShaderSingleBuffer(PainterComponent p) => p.TexMeta.IsAVolumeTexture ? _brushSingleBuffer.Shader : null;

            public bool IsA3DBrush(PainterComponent painter, Brush bc, ref bool overrideOther)
            {
                if (!painter.TexMeta.IsAVolumeTexture) return false;
                overrideOther = true;
                return true;
            }

            public void PaintPixelsInRam(Painter.Command.Base command)
            {

                Stroke stroke = command.Stroke;
                float brushAlpha = command.strokeAlphaPortion;
                TextureMeta image = command.TextureData;
                Brush bc = command.Brush;

                var volume = image.Texture2D.GetVolumeTextureData();

                if (!volume) return;

                bc.brush3DRadius = Mathf.Min(BrushScaleMaxForCpu(volume), bc.brush3DRadius);

                var volumeScale = volume.size;

                var pos = (stroke.posFrom - volume.transform.position) / volumeScale + 0.5f * Vector3.one;

                var height = volume.TextureHeight;
                var texWidth = image.Width;

                BlitFunctions.brAlpha = brushAlpha;
                bc.PrepareCpuBlit(image);
                BlitFunctions.half = bc.Size(true) / volumeScale;

                var type = command.TextureData.Target;

                var pixels = image.Pixels;

                var iHalf = (int) (BlitFunctions.half - 0.5f);
                var smooth = bc.GetBrushType(type) != BrushTypes.Pixel.Inst;
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

            public bool IsEnabledFor(PainterComponent painter, TextureMeta img, Brush cfg) =>
                img.GetVolumeTextureData();

            public void PaintRenderTextureUvSpace(Painter.Command.Base command) 
            {
                Stroke stroke = command.Stroke;
                TextureMeta image = command.TextureData;
                Brush bc = command.Brush;

                var vt = image.GetVolumeTextureData();

                if (!vt)
                {
                    Debug.LogError(QcLog.IsNull(vt, context: nameof(PaintRenderTextureUvSpace))); 
                    return;
                }

                if (_enableRayTracing)
                {

                    rayTraceCameraConfiguration.From(stroke);

                    bc.useAlphaBuffer = false;

                    delayedPaintingConfiguration = new Painter.Command.UV(stroke, image, bc);

                    Painter.GetOrCreateProjectorCamera().RenderRightNow(this);
                }
                else
                    PaintRenderTextureInternal(command); // Maybe wrong

            }

            public bool PaintRenderTextureInternal(Painter.Command.Base cfg)
            {
                var stroke = cfg.Stroke;
                var image = cfg.TextureData;
                var bc = cfg.Brush;

                var vt = cfg.TextureData.GetVolumeTextureData(); //painter.GetVolumeTexture();
                stroke.posFrom = stroke.posTo;

                BrushTypes.Sphere.Inst.BeforeStroke(cfg); //bc, stroke, painter);

                VOLUME_POSITION_N_SIZE_BRUSH.GlobalValue = vt.UpdateShaderVariables();//PosSize4Shader;
                VOLUME_H_SLICES_BRUSH.GlobalValue = vt.GetSlices4Shader();
                VOLUME_BRUSH_DIRECTION.GlobalValue = stroke.collisionNormal.ToVector4(smoothing);

                UseSmoothing.Enabled = smoothing > 0;

                image[TextureCfgFlags.Texcoord2] = false;
                cfg.strokeAlphaPortion = Mathf.Clamp01(bc.Flow * 0.05f);
                Painter.Camera.SHADER_STROKE_SEGMENT_UPDATE(cfg); 

                stroke.FeedWorldPosInShader();

                RenderTextureBuffersManager.Blit(null, image.CurrentRenderTexture(),
                    Painter.Camera.brushRenderer.GetMaterial().shader);

                BrushTypes.Sphere.Inst.AfterStroke(cfg); 

                return true;
            }

            #region Ray Tracing

            private bool _enableRayTracing;

            private float minFov = 60;

            private float maxFov = 170;

            //private float arbitraryBrightnessIncrease = 1.5f;

            private Painter.Command.UV delayedPaintingConfiguration;

            private static readonly ProjectorCameraConfiguration rayTraceCameraConfiguration = new ();

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

                Painter.GetOrCreateProjectorCamera();

                GlobalBrush.Color = avg / pixelsCount;

                PainterShaderVariables.BrushColorProperty.GlobalValue = GlobalBrush.Color;

                PaintRenderTextureUvSpace(delayedPaintingConfiguration);

                delayedPaintingConfiguration = null;
            }

            public RenderTexture GetTargetTexture() => RenderTextureBuffersManager.GetRenderTextureWithDepth();

            public Singleton_DepthProjectorCamera.Mode GetMode() => Singleton_DepthProjectorCamera.Mode.ReplacementShader;

            public string ProjectorTagToReplace() => "RenderType";

         //   public Shader ProjectorShaderToReplaceWith() => Painter.Data.rayTraceOutput;

            public Color CameraReplacementClearColor() => new (0, 0, 1, 1);

            #endregion

            #region Inspector

            private float BrushScaleMaxForCpu(C_VolumeTexture volTex) => volTex.size * volTex.SliceWidth * 0.025f;

            public override string ToString() => "Volume Painting";

            public bool ComponentInspector()
            {
                if (!InspectedPainter)
                {
                    "No inspected Painter found".PL().WriteWarning();
                    return false;
                }

                var vt = InspectedPainter.GetModule<VolumeTextureComponentModule>().volumeTexture;

                if (!vt)
                    return false;

                if (!vt.Texture)
                {
                    "Volume has no texture".PL().WriteWarning();
                    return false;
                }

                return true;
            }

            private bool _exploreVolumeData;

            //private bool _exploreRayTaceCamera;

            private readonly pegi.EnterExitContext contex = new();

            public void BrushConfigPEGI(Brush br)
            {

                using (contex.StartContext())
                {
                    var changed = pegi.ChangeTrackStart();

                    var p = InspectedPainter;

                    var volTex = p.TexMeta.GetVolumeTextureData();

                    if (volTex)
                    {

                        var tex = volTex.Texture;

                        if (tex)
                        {

                            "Volume is a {0} texture".F(tex.IsColorTexture() ? "Color" : "Non-Color Data").PL().Write();

#if UNITY_EDITOR
                            if (tex.IsColorTexture())
                            {
                                pegi.Nl();
                                var imp = tex.GetTextureImporter_Editor();

                                if ((imp != null) && "FIX texture".PL().Click() && (imp.WasWrongIsColor_Editor(false)))
                                    imp.SaveAndReimport();
                            }
#endif


                            pegi.Nl();
                        }
                        else
                            "Volume has no texture".PL().WriteWarning();

                        var id = p.TexMeta;

                        var cpuBlit = id.TargetIsTexture2D();
                        pegi.Nl();

                        br.showingSize = !_enableRayTracing || cpuBlit;

                        if (!cpuBlit)
                        {

                            if (Brush.showAdvanced || _enableRayTracing)
                            {
                                "Ray-Tracing".PL().ToggleIcon(ref _enableRayTracing, true);

                                if (br.useAlphaBuffer)
                                    Icon.Warning.Draw(
                                        "Ray Tracing doesn't use Alpha buffer. Alpha buffer will be automatically disabled");

                            }

                            if ("Ray Trace Camera".PL().IsConditionally_Entered(
                                canEnter: _enableRayTracing && Singleton.Get<Singleton_DepthProjectorCamera>()
                                ).Nl_ifEntered())
                            {
                                "Min".ConstL().Edit(ref minFov, 60, maxFov - 1).Nl();

                                "Max".ConstL().Edit(ref maxFov, minFov + 1, 170).Nl();

                                rayTraceCameraConfiguration.Nested_Inspect().Nl();
                            }

                            if (smoothing > 0 || Brush.showAdvanced)
                            {
                                pegi.Nl();
                                "Smoothing".ConstL().Edit(ref smoothing, 0, 1);
                                pegi.FullWindow.DocumentationClickOpen("Best used in the end");

                                pegi.Nl();
                            }

                            if (!contex.IsAnyEntered && _enableRayTracing)
                            {
                                var dp = Singleton.Get<Singleton_DepthProjectorCamera>(); //PainterCamera.depthProjectorCamera;

                                if (!dp)
                                {
                                    if ("Create Projector Camera".PL().Click().Nl())
                                        Painter.GetOrCreateProjectorCamera();
                                }
                                else if (dp.pauseAutoUpdates)
                                {
                                    pegi.Nl();
                                    "Light Projectors paused".PL().ToggleIcon(ref dp.pauseAutoUpdates).Nl();
                                }

                                pegi.Nl();

                            }
                        }

                        if (!cpuBlit)
                        {
                            pegi.Nl();

                            if (!br.GetBrushType(TexTarget.RenderTexture).IsAWorldSpaceBrush)
                            {
                                "Only World space brush can edit volumes".PL().Write_Hint();
                                pegi.Nl();
                                if ("Change to Sphere brush".PL().Click())
                                    br.SetBrushType(TexTarget.RenderTexture, BrushTypes.Sphere.Inst);
                            }
                        }

                        pegi.Nl();


                        if (!contex.IsAnyEntered && Painter.Data.showVolumeDetailsInPainter &&
                            (volTex.name + " " + VolumeEditingExtensions.VolumeSize(id.Texture2D, volTex.hSlices)).PL()
                            .IsFoldout(ref _exploreVolumeData).Nl())
                            volTex.Nested_Inspect();

                        if (!cpuBlit)
                            MsgPainter.Sharpness.GetText().PL(MsgPainter.Sharpness.GetDescription(), 70)
                                .Edit(ref br.hardness, 1f, 5f).Nl();

                        var tmpSpeed = br._dFlow.Value;
                        if (MsgPainter.Flow.GetText().PL(40).Edit(ref tmpSpeed, 0.01f, 4.5f).Nl())
                            br._dFlow.Value = tmpSpeed;

                        if (br.showingSize)
                        {
                            var maxScale = volTex.size * volTex.SliceWidth * 4;

                            "Scale".PL(toolTip: "Scale For Volume painting", 40).Edit(ref br.brush3DRadius, 0.001f * maxScale, maxScale * 0.5f);

                            if (cpuBlit && !_brushShaderForRayTrace.Shader && br.brush3DRadius > BrushScaleMaxForCpu(volTex))
                                Icon.Warning.Draw(
                                    "Size will be reduced when panting due to low performance of the CPU brush for volumes");

                        }

                        pegi.Nl();
                    }
                }

            }

            private int _exploredVolume;

            void IPEGI.Inspect()
            {
                "Volumes".PL().Edit_List(C_VolumeTexture.all, ref _exploredVolume);
            }

            public void PlugIn_PainterGizmos(PainterComponent painter)
            {
                var volume = painter.TexMeta.GetVolumeTextureData();

                if (volume && !painter.TextureEditingBlocked)
                    volume.DrawGizmosOnPainter(painter);
            }


            #endregion

        }
    }

    namespace ComponentModules
    {
        [TaggedTypes.Tag(CLASS_KEY)]
        internal class VolumeTextureComponentModule : ComponentModuleBase
        {
            private const string CLASS_KEY = "VolTexM";
            public override string ClassTag => CLASS_KEY;

            public C_VolumeTexture volumeTexture;

            private bool expectingAVolume;

            public override string ToString() => "Volume Texture";

            public override void OnComponentDirty()
            {

            }

            public override bool SetTextureOnMaterial(TextureValue field, TextureMeta id)
            {
                if (!volumeTexture)
                    return false;

                if (field.ToString() == volumeTexture.name)
                {
                    volumeTexture.Texture = id.CurrentTexture();
                    return true;
                }

                return false;
            }
            
            public override void GetNonMaterialTextureNames(ref List<TextureValue> dest)
            {
                if (expectingAVolume && !volumeTexture)
                {
                    volumeTexture = painter.gameObject.GetComponent<C_VolumeTexture>();
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
                if (id == null || !id.IsAVolumeTexture)
                    return false;
                id.Tiling = Vector2.one;
                id.Offset = Vector2.zero;
                return true;
            }

         

            #region Encode & Decode

            public override CfgEncoder Encode() => new CfgEncoder()
                .Add("b", base.Encode)
                .Add_IfTrue("gotVol", volumeTexture);

            public override void DecodeTag(string key, CfgData data)
            {
                switch (key)
                {
                    case "b": data.ToDelegate(base.DecodeTag); break;//data.DecodeInto(base.Decode); break;
                    case "gotVol": expectingAVolume = data.ToBool(); break;
                }
            }
            #endregion


            public override void Inspect()
            {
                "Volume Texture:".PL().Edit(ref volumeTexture).Nl();
            }


        }
    }

    internal static class VolumeEditingExtensions
    {
        
        public static Vector3 VolumeSize(Texture2D tex, int slices)
        {
            var w = tex.width / slices;
            return new Vector3(w, slices * slices, w);
        }

        /*
        public static void SetVolumeTexture(this IEnumerable<Material> materials, C_VolumeTexture vt) {
            if (!vt)
                return;
            var imd = vt.Texture.GetImgDataIfExists(); //ImageMeta;

            if (imd == null)
                return;

            imd.IsAVolumeTexture = true;

            var pnS = vt.UpdateShaderVariables();//PosSize4Shader;
            var vhS = vt.GetSlices4Shader();
            var tex = vt.TextureInShaderProperty;
            
            foreach (var m in materials)
                if (m)
                {
                    VolumePaintingCameraModule.VOLUME_POSITION_N_SIZE.SetOn(m, pnS);
                    VolumePaintingCameraModule.VOLUME_H_SLICES.SetOn(m, vhS);
                    tex.SetOn(m, imd.CurrentTexture());
                }
        }*/

        public static C_VolumeTexture GetVolumeTextureData(this Texture tex) => GetVolumeTextureData(tex.GetTextureMeta());
        
        private static C_VolumeTexture _lastFetchedVt;

        public static C_VolumeTexture GetVolumeTextureData(this TextureMeta id)
        {
            if (VolumePaintingCameraModule._inst == null || id == null)
                return null;

            if (_lastFetchedVt != null && _lastFetchedVt.Texture && _lastFetchedVt.Texture.GetImgDataIfExists() == id)
                return _lastFetchedVt;

            for (var i = 0; i < C_VolumeTexture.all.Count; i++)
            {
                var vt = C_VolumeTexture.all[i];
                if (!vt) {
                    C_VolumeTexture.all.RemoveAt(i);
                    i--;
                }
                else if (vt.Texture != null && id == vt.Texture.GetImgDataIfExists())
                {
                    _lastFetchedVt = vt;
                    id.IsAVolumeTexture = true;
                    return vt;
                }
            }

            return null;
        }

    }
}