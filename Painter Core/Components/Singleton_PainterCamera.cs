using System;
using System.Collections.Generic;
using QuizCanners.Inspect;
using PainterTool.CameraModules;
using QuizCanners.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using QuizCanners.Migration;
using QuizCanners.Lerp;
using UnityEngine.Rendering;

namespace PainterTool {

    [HelpURL(PainterComponent.OnlineManual)]
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    [AddComponentMenu("Playtime Painter/Painter Camera")]
    public class Singleton_PainterCamera : PainterSystemMono 
    {   
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private Camera painterCamera;
        [SerializeField] private PlaytimePainter_RenderBrush brushPrefab;
        [SerializeField] internal PlaytimePainter_RenderBrush brushRenderer;
        [SerializeField] private Transform _destinationBuffer;
        internal const float OrthographicSize = 128;
        
        internal static float _previewAlpha = 1;
        private bool disableSecondBufferUpdateDebug;

        [SerializeField] internal SO_PainterDataAndConfig dataHolder;

        private static readonly List<TextureMeta> _texturesNeedUpdate = new();

        internal bool _triedToFindPainterData;
        
        internal void RequestLateUpdate(TextureMeta meta) 
        {
            if (!_texturesNeedUpdate.Contains(meta))
                _texturesNeedUpdate.Add(meta);
        }

        #region Painting Layer

        private void UpdateCullingMask() 
        {
            var l = (Painter.Data ? Painter.Data.playtimePainterLayer : 30);

            if (_mainCamera)
                _mainCamera.SetMask(layerIndex: l, value: false); //cullingMask &= ~flag;

            if (painterCamera)
                painterCamera.SetMaskRemoveOthers(layerIndex: l); //cullingMask = flag;

            QcUnity.RenamingLayer(l, "Playtime Painter's Layer");
            QcUnity.SetLayerMaskForSceneView(l, false);

            if (brushRenderer)
                brushRenderer.gameObject.layer = l;
        }

        internal Camera MainCamera {
            get => _mainCamera; 
            set {
                if (value && painterCamera && value == painterCamera) {
                    pegi.GameView.ShowNotification("Can't use Painter Camera as Main Camera");
                    return;
                }

                _mainCamera = value;

                UpdateCullingMask();
            }
        }

        internal RenderTexture TargetTexture
        {
            get => painterCamera.targetTexture;
            set => painterCamera.targetTexture = value; 
        }

        private Shader CurrentShader
        {
            set => brushRenderer.Set(value);
        }

        #endregion

        #region Encode & Decode

        public override CfgEncoder Encode() => base.Encode()//this.EncodeUnrecognized()
            .Add("mm", Painter.MeshManager)
            .Add_Abstract("pl", CameraModuleBase.modules)
            .Add("rts", RenderTextureBuffersManager.renderBuffersSize);

        public override void DecodeTag(string key, CfgData data) {
            switch (key) {
                case "pl":
                    data.ToList(out CameraModuleBase.modules, CameraModuleBase.all);
                    CameraModuleBase.RefreshModules();
                    break;
                case "mm": Painter.MeshManager.Decode(data); break;
                case "rts": RenderTextureBuffersManager.renderBuffersSize = data.ToInt(); break;
            }
        }

        #endregion

        #region Buffers MGMT

        [NonSerialized] private TextureMeta alphaBufferDataTarget;
        [NonSerialized] private Shader alphaBufferDataShader;

        [SerializeField] internal TextureMeta imgMetaUsingRendTex;
        [SerializeField] internal List<MaterialMeta> materialsUsingRenderTexture = new();
        [SerializeField] internal PainterComponent autodisabledBufferTarget;

        internal void EmptyBufferTarget()
        {
            if (imgMetaUsingRendTex == null)
                return;

            if (imgMetaUsingRendTex.Texture2D)
                imgMetaUsingRendTex.RenderTexture_To_Texture2D();

            imgMetaUsingRendTex.Target = TexTarget.Texture2D;

            foreach (var m in materialsUsingRenderTexture)
                m.SetTextureOnLastTarget(imgMetaUsingRendTex);

            materialsUsingRenderTexture.Clear();
            imgMetaUsingRendTex = null;
            RenderTextureBuffersManager.DiscardPaintingBuffersContents();
        }

        internal void ChangeBufferTarget(TextureMeta newTarget, MaterialMeta mat, ShaderProperty.TextureValue parameter, PainterComponent painter)
        {
            if (newTarget != imgMetaUsingRendTex) 
            {
                if (materialsUsingRenderTexture.Count > 0)
                    PainterComponent.CheckSetOriginalShader();

                if (imgMetaUsingRendTex != null) {

                    if (imgMetaUsingRendTex.Texture2D)
                        imgMetaUsingRendTex.RenderTexture_To_Texture2D();

                    imgMetaUsingRendTex.Target = TexTarget.Texture2D;

                    foreach (var m in materialsUsingRenderTexture)
                        m.SetTextureOnLastTarget(imgMetaUsingRendTex);
                }

                materialsUsingRenderTexture.Clear();
                autodisabledBufferTarget = null;
                imgMetaUsingRendTex = newTarget;
            }

            if (mat != null)
            {
                mat.bufferParameterTarget = parameter;
                mat.painterTarget = painter;
                materialsUsingRenderTexture.Add(mat);
            }
        }

        internal static RenderTexture FrontBuffer => RenderTextureBuffersManager.GetOrCreatePaintingBuffers()[0];
        private static RenderTexture BackBuffer => RenderTextureBuffersManager.GetOrCreatePaintingBuffers()[1];
        private static RenderTexture AlphaBuffer => RenderTextureBuffersManager.alphaBufferTexture;

        internal static bool GotBuffers => Painter.Camera && RenderTextureBuffersManager.GotPaintingBuffers;
        #endregion

        #region Brush Shader MGMT

        internal void SHADER_BRUSH_UPDATE(Painter.Command.Base command)
        {
            Brush brush = command.Brush;

            TextureMeta id = command.TextureData;

            bool rendTex = id.TargetIsRenderTexture();

            BrushTypes.Base brushType = brush.GetBrushType(id.Target);

            bool is3DBrush = command.Is3DBrush;

            brush.previewDirty = false;

            PainterShaderVariables.BrushColorProperty.GlobalValue = brush.Color;

            PainterShaderVariables.BrushMaskProperty.GlobalValue = brush.mask.ToVector4();

            brushType.OnShaderBrushUpdate(brush);

            if (rendTex)
                PainterShaderVariables.SourceMaskProperty.GlobalValue = brush.useMask ? Painter.Data.masks.TryGet(brush.selectedSourceMask) : null;

            PainterShaderVariables.MaskDynamicsProperty.GlobalValue = new Vector4(
                brush.maskTiling,
                rendTex ? brush.hardness * brush.hardness : 0,       // y - Hardness is 0 to do correct preview for Texture2D brush 
                ((brush.flipMaskAlpha && brush.useMask) ? 0 : 1),  // z - flip mask if any
                (brush.maskFromGreyscale && brush.useMask) ? 1 : 0);

            PainterShaderVariables.MaskOffsetProperty.GlobalValue = brush.maskOffset.ToVector4();

            float brushSizeUvSpace = brush.Size(is3DBrush) / (id == null ? 256 : Mathf.Min(id.Width, id.Height));

            PainterShaderVariables.BrushFormProperty.GlobalValue = new Vector4(
                command.strokeAlphaPortion, // x - transparency
                brush.Size(is3DBrush), // y - scale for sphere
                brushSizeUvSpace, // z - scale for uv space
                brush.blurAmount); // w - blur amount

            if (id == null)
                return;

            var blitMode = brush.GetBlitMode(id.Target);

            var useAlphaBuffer = (brush.useAlphaBuffer && blitMode.SupportsAlphaBufferPainting && rendTex);

            float useTransparentLayerBackground = 0;

            PainterShaderVariables.OriginalTextureTexelSize.GlobalValue = new Vector4(
                1f / id.Width,
                1f / id.Height,
                id.Width,
                id.Height
                );

            var painter = command.TryGetPainter();

            if (id[TextureCfgFlags.TransparentLayer] && painter) 
            {
                var md = painter.MatDta;
                var mat = md.material;
                if (md != null && md.usePreviewShader && mat) 
                {
                    var mt = mat.mainTexture;
                    PainterShaderVariables.TransparentLayerUnderProperty.GlobalValue = mt;
                    useTransparentLayerBackground = (mt && (id != mt.GetImgDataIfExists())) ? 1 : 0;
                }
            }

            PainterShaderVariables.AlphaBufferConfigProperty.GlobalValue = new Vector4(
                brush.alphaLimitForAlphaBuffer,
                brush.worldSpaceBrushPixelJitter ? 1 : 0,
                useAlphaBuffer ? 1 : 0,
                0);

            PainterShaderVariables.AlphaPaintingBuffer.GlobalValue = AlphaBuffer;

            var uv2 = id[TextureCfgFlags.Texcoord2];

            brushType.SetKeyword(uv2); //UseTexCoord2);

            QcUnity.SetShaderKeyword(PainterShaderVariables.BRUSH_TEXCOORD_2, uv2);
            QcUnity.SetShaderKeyword(PainterShaderVariables.TARGET_TRANSPARENT_LAYER, id[TextureCfgFlags.TransparentLayer]);

            blitMode.SetKeyword(id).SetGlobalShaderParameters();

            if (rendTex && blitMode.UsingSourceTexture)
            {
                PainterShaderVariables.SourceTextureProperty.GlobalValue = Painter.Data.sourceTextures.TryGet(brush.selectedSourceTexture);
                PainterShaderVariables.TextureSourceParameters.GlobalValue = new Vector4(
                    (float)brush.srcColorUsage,
                    brush.clampSourceTexture ? 1f : 0f,
                    useTransparentLayerBackground,
                    brush.ignoreSrcTextureTransparency ? 1f : 0f
                    );
            }
        }

        internal void SHADER_STROKE_SEGMENT_UPDATE(Painter.Command.Base command)
        {
            Brush brush = command.Brush;
            TextureMeta textureMeta = command.TextureData;

            var isDoubleBuffer = GraphicsSettings.defaultRenderPipeline || !textureMeta.RenderTexture;

            var useSingle = !isDoubleBuffer || brush.IsSingleBufferBrush();

            var blitMode = brush.GetBlitMode(command.TextureData.Target);

            command.usedAlphaBuffer = //!useSingle && // Not sure
                brush.useAlphaBuffer && brush.GetBrushType(command.TextureData.Target).SupportsAlphaBufferPainting && blitMode.SupportsAlphaBufferPainting;

            var painter = command.TryGetPainter();

            Shader shd = null;
            if (painter)
                foreach (var pl in CameraModuleBase.BrushPlugins)
                {
                    if (pl.IsEnabledFor(painter, textureMeta, brush))
                    {
                        var bs = useSingle ? pl.GetBrushShaderSingleBuffer(painter) : pl.GetBrushShaderDoubleBuffer(painter);
                        if (!bs)
                            continue;
                        shd = bs;
                        break;
                    }
                }

            if (!shd) 
            {
                if (command.usedAlphaBuffer) 
                {
                    shd = blitMode.ShaderForAlphaOutput;
                    AlphaBufferSetDirtyBeforeRender(textureMeta, blitMode.ShaderForAlphaBufferBlit);
                }
                else
                    shd = useSingle ? blitMode.ShaderForSingleBuffer : blitMode.ShaderForDoubleBuffer;
            }


            if (!useSingle)
                RenderTextureBuffersManager.UpdateSecondBuffer();

            SHADER_BRUSH_UPDATE(command);

            TargetTexture = command.usedAlphaBuffer ? AlphaBuffer : textureMeta.CurrentRenderTexture();

            if (isDoubleBuffer)
            {
                PainterShaderVariables.DESTINATION_BUFFER.GlobalValue = BackBuffer;
                _destinationBuffer.gameObject.SetActive(true);
            }
            else
                _destinationBuffer.gameObject.SetActive(false);


            _latestPaintShaderDebug = shd;

            CurrentShader = shd;
        }

        internal static void SHADER_POSITION_AND_PREVIEW_UPDATE(Stroke st, bool hidePreview)
        {
            PainterShaderVariables.PREVIEW_BRUSH_UV_POS_FROM.GlobalValue = st.uvFrom.ToVector4(0, _previewAlpha);
            PainterShaderVariables.PREVIEW_BRUSH_UV_POS_TO.GlobalValue = st.uvTo.ToVector4(0, _previewAlpha);

            if (hidePreview && Math.Abs(_previewAlpha) < float.Epsilon)
                return;

            QcLerp.IsLerpingBySpeed(ref _previewAlpha, hidePreview ? 0 : 1, 4f, unscaledTime: true);

            st.FeedWorldPosInShader();
            if (!hidePreview)
            {
                st.TrySetPreviousValues();
            }
        }

        #endregion

        #region Alpha Buffer 

        internal void AlphaBufferSetDirtyBeforeRender(TextureMeta id, Shader shade) {

            if (alphaBufferDataTarget != null && (alphaBufferDataTarget != id || alphaBufferDataShader != shade))
                UpdateFromAlphaBuffer(alphaBufferDataTarget.CurrentRenderTexture(), alphaBufferDataShader);

            alphaBufferDataTarget = id;
            alphaBufferDataShader = shade;

        }

        internal void DiscardAlphaBuffer() {
            RenderTextureBuffersManager.ClearAlphaBuffer();
            alphaBufferDataTarget = null;
        }

        internal void UpdateFromAlphaBuffer(RenderTexture rt, Shader shader)
        {
            if (rt) 
            {
                PainterShaderVariables.AlphaPaintingBuffer.GlobalValue = AlphaBuffer;
                Render(AlphaBuffer, rt, shader);
            }

            DiscardAlphaBuffer();
            RenderTextureBuffersManager.UpdateSecondBuffer();
        }

        internal void FinalizePreviousAlphaDataTarget()
        {
            if (alphaBufferDataTarget != null)
            {
                UpdateFromAlphaBuffer(alphaBufferDataTarget.CurrentRenderTexture(), alphaBufferDataShader);
            }
        }

        #endregion

        #region Render

        public ConfiguredRender Prepare(RenderTexture target, Shader shader) 
        {
            CurrentShader = shader;
            return new ConfiguredRender(target);
        }

        public ConfiguredRender Prepare(Painter.Command.WorldSpaceBase command) 
        {
            brushRenderer.PrepareWorldSpace(command);
            return new ConfiguredRender(command.TextureData.CurrentRenderTexture());
        }

        public ConfiguredRender Prepare(Color col, RenderTexture target)
        {
            brushRenderer.PrepareColorPaint(col);
            return new ConfiguredRender(target);
        }

        public RenderTexture Render(Texture from, RenderTexture to, Shader shader) => brushRenderer.CopyBuffer(from, to, shader);
        
        public RenderTexture Render(Texture from, RenderTexture to, Material mat) =>  brushRenderer.CopyBuffer(from, to, mat);
         
        internal RenderTexture Render(Texture from, TextureMeta to) => Render(from, to.CurrentRenderTexture(), Painter.Data.brushBufferCopy.Shader);

        internal void Render()
        {
            transform.rotation = Quaternion.identity;
            PainterShaderVariables.cameraPosition_Property.GlobalValue = transform.position.ToVector4();

            brushRenderer.gameObject.SetActive(true);
            painterCamera.Render();

            if (!disableSecondBufferUpdateDebug)
                brushRenderer.gameObject.SetActive(false);

            _destinationBuffer.gameObject.SetActive(false);

            var trg = TargetTexture;

            if (trg == FrontBuffer)
                RenderTextureBuffersManager.secondBufferUpdated = false;

            lastPainterCall = QcUnity.TimeSinceStartup();
            brushRenderer.AfterRender();
        }

        public void UpdateBufferSegment()
        {
            if (!disableSecondBufferUpdateDebug)
            {
                brushRenderer.Set(FrontBuffer);
                TargetTexture = BackBuffer;
                CurrentShader = Painter.Data.brushBufferCopy.Shader;
                Render();
                RenderTextureBuffersManager.secondBufferUpdated = true;
                RenderTextureBuffersManager.bigRtVersion++;
            }
        }

        #endregion

        #region Updates

        internal void TryApplyBufferChangesTo(TextureMeta id) {

            if ((id != null) && (id == alphaBufferDataTarget))
                FinalizePreviousAlphaDataTarget();
            
        }

        internal void TryDiscardBufferChangesTo(TextureMeta id) {

            if (id != null && id == alphaBufferDataTarget)
                DiscardAlphaBuffer();
            
        }

        internal void OnBeforeBlitConfigurationChange() {
            FinalizePreviousAlphaDataTarget();
        }

        internal void SubscribeToEditorUpdates()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.update += ManagedUpdate;
            #endif
        }
        
        protected override void OnAfterEnable() 
        {

            if (!MainCamera)
                MainCamera = Camera.main;

            if (!Painter.Data)
                dataHolder = Resources.Load("Painter_Data") as SO_PainterDataAndConfig;

            Painter.MeshManager.OnEnable();

            if (!painterCamera)
                painterCamera = GetComponent<Camera>();
            
            if (!SO_PainterDataAndConfig.toolEnabled && !Application.isEditor)
                    SO_PainterDataAndConfig.toolEnabled = true;

#if UNITY_EDITOR

            UnityEditor.SceneManagement.EditorSceneManager.sceneSaving -= BeforeSceneSaved;
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaving += BeforeSceneSaved;

            Painter.IsLinearColorSpace = UnityEditor.PlayerSettings.colorSpace == ColorSpace.Linear;

            UnityEditor.EditorApplication.update -= ManagedUpdate;
            if (!QcUnity.ApplicationIsAboutToEnterPlayMode())
                SubscribeToEditorUpdates();

            if (!brushPrefab) {
                var go = Resources.Load(SO_PainterDataAndConfig.PREFABS_RESOURCE_FOLDER +"/RenderCameraBrush") as GameObject;
                if (go) {
                    brushPrefab = go.GetComponent<PlaytimePainter_RenderBrush>();
                    if (!brushPrefab)
                        Debug.Log("Couldn't find brush prefab.");
                }
                else
                    Debug.LogError("Couldn't load brush Prefab");
            }
            
            #endif

            if (!brushRenderer){
                brushRenderer = GetComponentInChildren<PlaytimePainter_RenderBrush>();
                if (!brushRenderer)
                    brushRenderer = Instantiate(brushPrefab.gameObject, transform).GetComponent<PlaytimePainter_RenderBrush>();
            }

            var tf = transform;
            tf.position = Vector3.up * 3000;
            tf.localScale = Vector3.one;
            tf.rotation = Quaternion.identity;

            if (!painterCamera) {
                painterCamera = GetComponent<Camera>();
                if (!painterCamera)
                    painterCamera = gameObject.AddComponent<Camera>();
            }

            painterCamera.orthographic = true;
            painterCamera.orthographicSize = OrthographicSize;
            painterCamera.clearFlags = CameraClearFlags.Nothing;
            painterCamera.enabled = false; //Application.isPlaying;
            painterCamera.allowHDR = false;
            painterCamera.allowMSAA = false;
            painterCamera.allowDynamicResolution = false;
            painterCamera.depth = 0;
            painterCamera.renderingPath = RenderingPath.Forward;
            painterCamera.nearClipPlane = 0.1f;
            painterCamera.farClipPlane = 1000f;
            painterCamera.rect = Rect.MinMaxRect(0,0,1,1);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= ManagedUpdate;
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode == false)
                UnityEditor.EditorApplication.update += ManagedUpdate;
#endif

            /*
            CheckHDRP();

            void CheckHDRP()
            {
                if (!GraphicsSettings.defaultRenderPipeline)
                    return;

                if (GraphicsSettings.defaultRenderPipeline.GetType().Name.Contains("HDRenderPipelineAsset"))
                {
                    Debug.Log("Configuring for HDRP");
                    var data = painterCamera.gameObject.GetComponent<HDAdditionalCameraData>();
                    data.volumeLayerMask = (1 << Painter.Data.playtimePainterLayer);
                }
            }

            */

            autodisabledBufferTarget = null;

            CameraModuleBase.RefreshModules();

            foreach (var p in CameraModuleBase.modules)
                p?.Enable();
            
            if (Painter.Data)
                Painter.Data.ManagedOnEnable();

            UpdateCullingMask();

            PainterShaderVariables.BrushColorProperty.ConvertToLinear = Painter.IsLinearColorSpace;
        }

        protected override void OnBeforeOnDisableOrEnterPlayMode(bool afterEnableCalled)
        {
            BeforeClosing();
            base.OnBeforeOnDisableOrEnterPlayMode(afterEnableCalled);
        }

        private void BeforeClosing()
        {
            Painter.DownloadManager.Dispose();

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= ManagedUpdate;

            if (PainterComponent.previewHolderMaterial)
                PainterComponent.CheckSetOriginalShader();
            
            if (materialsUsingRenderTexture.Count > 0)
                autodisabledBufferTarget = materialsUsingRenderTexture[0].painterTarget;

            EmptyBufferTarget();

            #endif

            if (!CameraModuleBase.modules.IsNullOrEmpty())
                foreach (var p in CameraModuleBase.modules)
                    p?.Disable();
            
            if (Painter.Data)
                Painter.Data.ManagedOnDisable();

            RenderTextureBuffersManager.OnDisable();
        }

#if UNITY_EDITOR

        internal void BeforeSceneSaved(Scene scene, string path) => BeforeClosing(); 
        #endif

        public void Update() 
        {
            if (Application.isPlaying)
                ManagedUpdate();
        }

        public static GameObject refocusOnThis;
        #if UNITY_EDITOR
        private static int _scipFrames = 3;
        #endif

        public static double lastPainterCall;

        public static double lastManagedUpdate;

        private readonly Gate.Frame _frameGate = new();

        public void ManagedUpdate() {

            if (!_frameGate.TryEnter())
                return; 

            if (!this || !Painter.Data)
                return;

            foreach (var t in _texturesNeedUpdate)
                t.SetAndApply(); 

            _texturesNeedUpdate.Clear();

            lastManagedUpdate = QcUnity.TimeSinceStartup();

            if (PainterComponent.IsCurrentTool && Painter.FocusedPainter)
                Painter.FocusedPainter.ManagedUpdateOnFocused();
            
            QcAsync.DefaultCoroutineManager.UpdateManagedCoroutines();

            Painter.MeshManager.CombinedUpdate();

            if (!Application.isPlaying)
                Singleton.Try<Singleton_DepthProjectorCamera>(cam => cam.ManagedUpdate(), logOnServiceMissing: false);
            
#if UNITY_EDITOR
            if (refocusOnThis) {
                _scipFrames--;
                if (_scipFrames == 0) {
                    QcUnity.FocusOn(refocusOnThis);
                    refocusOnThis = null;
                    _scipFrames = 3;
                }
            }
#endif
            
            var p = PainterComponent.currentlyPaintedObjectPainter;

            if (p && !Application.isPlaying && ((QcUnity.TimeSinceStartup() - lastPainterCall)>0.016f)) {

                if (p.TexMeta == null)
                    PainterComponent.currentlyPaintedObjectPainter = null;
                else {
                    p.ProcessStrokeState();
                }
            }
            
            var needRefresh = false;
            if (CameraModuleBase.modules!= null)
                foreach (var pl in CameraModuleBase.modules)
                    if (pl != null)
                        pl.Update();
                    else needRefresh = true;

            if (needRefresh) {
                Debug.Log("Refreshing modules");
                CameraModuleBase.RefreshModules();
            }

            lastPainterCall = QcUnity.TimeSinceStartup();


        }

        #endregion

        #region Inspector
        public override string InspectedCategory => nameof(PainterComponent);

        private static readonly pegi.GameView.Window OnGUIWindow = new(600, 800);
        
        public void OnGUI()
        {

            if (!Painter.Data || !Painter.Data.enablePainterUIonPlay) return;
            
            if (Painter.FocusedPainter)
            {
                OnGUIWindow.Render(Painter.FocusedPainter, "{0} {1}".F(Painter.FocusedPainter.name, Painter.FocusedPainter.GetMaterialTextureProperty()));

               /* foreach (var p in CameraModuleBase.GuiPlugins)
                    p.OnGUI();*/
            }

            if (!PainterComponent.IsCurrentTool)
                OnGUIWindow.Collapse();

        }

        public AnimationCurve tmpCurve = new (new Keyframe(0, 0), new Keyframe(0.5f, 0.5f), new Keyframe(1, 1));
        
        public AnimationCurve InspectAnimationCurve(string role) {
            role.PegiLabel().Edit_Property(() => tmpCurve, this);
            return tmpCurve;
        }

        [SerializeField] private pegi.EnterExitContext _dependencyContext = new();
        [SerializeField] private pegi.EnterExitContext _context = new();
        private static readonly pegi.CollectionInspectorMeta _modulesMeta = new("Modules", true, true, true, false);

        private Shader _latestPaintShaderDebug;

        public override void Inspect()
        {
            using (_context.StartContext())
            {
                pegi.Nl();

                if ("Data && Settings".PegiLabel().IsEntered())
                {
                    pegi.Nl();
                    "Painter Data".PegiLabel().Edit(ref dataHolder).Nl();

                    if (Painter.Data)
                        Painter.Data.Nested_Inspect().Nl();
                    else
                    {
                        "NO CONFIG Scriptable Object".PegiLabel().WriteWarning();
                        pegi.Nl();
                        DependenciesInspect(true);
                    }
                }

                if (_context.IsAnyEntered == false && Painter.Data)
                    pegi.ClickHighlight(Painter.Data);

                pegi.Nl();

                if ("Painter Camera".PegiLabel().IsEntered())
                    DependenciesInspect(true);

                pegi.Nl();

                if ("Depth Projector Camera".PegiLabel().IsEntered().Nl())
                {
                    if (!QuizCanners.Utils.Singleton.Try<Singleton_DepthProjectorCamera>(s => s.Nested_Inspect().Nl(), logOnServiceMissing: false)
                        && "Instantiate".PegiLabel().Click())
                        Painter.GetOrCreateProjectorCamera();

                }

                if ("Painter Camera Modules".PegiLabel().IsEntered(showLabelIfTrue: false).Nl_ifNotEntered())
                {
                    _modulesMeta.Edit_List(CameraModuleBase.modules, CameraModuleBase.all);

                    if (!_modulesMeta.IsAnyEntered)
                    {
                        if ("Find Modules".PegiLabel().Click())
                            CameraModuleBase.RefreshModules();

                        if ("Delete Modules".PegiLabel().Click().Nl())
                            CameraModuleBase.modules = null;
                    }

                    pegi.Nl();
                }

                if ("Global Shader Variables".PegiLabel().IsEntered().Nl())
                    PainterShaderVariables.Inspect();

                pegi.Nl();

                if ("Utils".PegiLabel().IsEntered().Nl())
                    QcUtils.InspectAllUtils();

                pegi.Nl();

                if (!_context.IsAnyEntered)
                    "Latest Paint Shader (For Debug)".PegiLabel().Edit(ref _latestPaintShaderDebug).Nl();
            }
        }

        internal bool DependenciesInspect(bool showAll = false) {

            var changed = pegi.ChangeTrackStart();

            bool showOthers = showAll;

            using (_dependencyContext.StartContext())
            {
                if (showAll)
                {
                    pegi.Nl();

                    if ("Buffers".PegiLabel().IsEntered().Nl())
                    {
                        RenderTextureBuffersManager.Inspect();
                        pegi.Nl();
#if UNITY_EDITOR
                        "Disable Second Buffer Update (Debug Mode)".PegiLabel().ToggleIcon(ref disableSecondBufferUpdateDebug).Nl();
#endif

                        return changed;
                    }

                    pegi.Nl();

                    "Download Manager".PegiLabel().Enter_Inspect(Painter.DownloadManager).Nl();

                    if (_dependencyContext.IsAnyEntered == false)
                      

                    if (_dependencyContext.IsAnyEntered == false)
                    {
                            pegi.FullWindow.DocumentationClickOpen("You can enable URL field in the Optional UI elements to get texture directly from web");

                            (Painter.IsLinearColorSpace ? "Linear" : "Gamma").PegiLabel().Nl();


#if UNITY_EDITOR
                        if ("Refresh Brush Shaders".PegiLabel().Click().Nl())
                        {
                                Painter.Data.CheckShaders(true);
                            pegi.GameView.ShowNotification("Shaders Refreshed");
                        }
#endif

                        "Using layer:".PegiLabel().Edit_LayerMask(ref Painter.Data.playtimePainterLayer).Nl();
                    }
                }

                showOthers &= _dependencyContext.IsAnyEntered == false;
            }

            #if UNITY_EDITOR
            if (!Painter.Data)  {
                pegi.Nl();
                "No data Holder".PegiLabel(60).Edit(ref dataHolder).Nl();

                if (Icon.Refresh.Click("Try to find it")) 
                {
                    _triedToFindPainterData = false;
                }

                if ("Create".PegiLabel().Click().Nl()) 
                {
                    _triedToFindPainterData = false;

                    if (!Painter.Data) {
                        dataHolder = ScriptableObject.CreateInstance<SO_PainterDataAndConfig>();

                        UnityEditor.AssetDatabase.CreateAsset(dataHolder,
                                "Assets/Playtime-Painter/Resources/Painter_Data.asset");
                        UnityEditor.AssetDatabase.SaveAssets();
                        UnityEditor.AssetDatabase.Refresh();
                        
                    }
                }
            }
            #endif

           
            if (!painterCamera) 
            {
                pegi.Nl();
                "no painter camera".PegiLabel().WriteWarning();
                pegi.Nl();
            }
            else
            {
                if (painterCamera.clearFlags != CameraClearFlags.Nothing)
                {
                    pegi.Nl();
                    "Painter camera is not set to DontClear".PegiLabel().WriteWarning();
                    if ("Set to DontClear".PegiLabel().Click().Nl())
                    {
                        painterCamera.clearFlags = CameraClearFlags.Nothing;
                        painterCamera.SetToDirty();
                    }
                }
            }

            var dc = QuizCanners.Utils.Singleton.Get<Singleton_DepthProjectorCamera>();

            Camera depthCamera = dc ? dc._projectorCamera : null;

            bool depthAsMain = depthCamera && (depthCamera == MainCamera);

            if (showOthers || !MainCamera || depthAsMain) {
                pegi.Nl();

                var cam = MainCamera;
                
                var cams = new List<Camera>(FindObjectsOfType<Camera>());

                if (painterCamera && cams.Contains(painterCamera))
                    cams.Remove(painterCamera);

                if (depthCamera && cams.Contains(depthCamera))
                    cams.Remove(depthCamera);

                "Main Camera".PegiLabel(90).Select(ref cam, cams).OnChanged(()=> MainCamera = cam);
                
                if (Icon.Refresh.Click("Try to find camera tagged as Main Camera")) {
                    MainCamera = Camera.main;
                    if (!MainCamera)
                        pegi.GameView.ShowNotification("No camera is tagged as main");
                }

                pegi.Nl();

                if (depthAsMain) {
                    "Depth projector camera is set as Main Camera - this is likely a mistake".PegiLabel().WriteWarning();
                    pegi.Nl();
                }

                if (!cam)
                    "No Main Camera found. Playtime Painting will not be possible".PegiLabel().WriteWarning();

                pegi.Nl();

            }

            return changed;
        }

        #endregion

    }

    public class ConfiguredRender
    {
        public RenderTexture Target;
        public ConfiguredRender(RenderTexture target) 
        {
            Target = target;
            Painter.Camera.TargetTexture = target;
        }
    }

    public class RenderResult 
    {
        public RenderTexture Result;
        public RenderResult(ConfiguredRender configured) 
        {
            Result = configured.Target;
        }
    }

    public static class PainterCameraExtensions 
    {
        public static RenderResult Render(this ConfiguredRender configuredRender) 
        {
            Singleton.Get<Singleton_PainterCamera>().Render();
            return new RenderResult(configuredRender);
        }
    }

    [PEGI_Inspector_Override(typeof(Singleton_PainterCamera))] internal class RenderTexturePainterEditor : PEGI_Inspector_Override { }
}