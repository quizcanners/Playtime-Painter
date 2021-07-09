using System;
using System.Collections.Generic;
using QuizCanners.Inspect;
using PlaytimePainter.CameraModules;
using PlaytimePainter.MeshEditing;
using QuizCanners.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using QuizCanners.Migration;
using QuizCanners.Lerp;

namespace PlaytimePainter {
    
    [HelpURL(PlaytimePainter.OnlineManual)]
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class PainterCamera : PainterSystemMono {

        public static PlaytimePainter_DepthProjectorCamera depthProjectorCamera;

        public static PlaytimePainter_DepthProjectorCamera GetOrCreateProjectorCamera()
        {
          
                if (depthProjectorCamera)
                    return depthProjectorCamera;

                if (!PlaytimePainter_DepthProjectorCamera.Instance)
                    depthProjectorCamera = QcUnity.Instantiate<PlaytimePainter_DepthProjectorCamera>();

                return depthProjectorCamera;
            
        }

        public static readonly PlaytimePainter_BrushMeshGenerator BrushMeshGenerator = new PlaytimePainter_BrushMeshGenerator();

        public static readonly MeshEditorManager MeshManager = new MeshEditorManager();

        public static readonly TextureDownloadManager DownloadManager = new TextureDownloadManager();
        
        #region Painter Data
        [SerializeField] private PainterDataAndConfig dataHolder;

        [NonSerialized] public bool triedToFindPainterData;

        public static PainterDataAndConfig Data  {
            get  {

                if (!_inst && !Inst)
                    return null;

                if (!_inst.triedToFindPainterData && !_inst.dataHolder) {
                  
                     var allConfigs = Resources.LoadAll<PainterDataAndConfig>("");

                    _inst.dataHolder = allConfigs.TryGet(0);

                    if (!_inst.dataHolder)
                        _inst.triedToFindPainterData = true;
                }

                return _inst.dataHolder;
            }
        }
        #endregion

        #region Camera Singleton
        private static PainterCamera _inst;

        public static PainterCamera Inst {
            get
            {
                if (_inst) return _inst;

                _inst = null;

                _inst = FindObjectOfType<PainterCamera>();
              
                if (!PainterClass.applicationIsQuitting) {

                    if (!_inst)
                    {
                        var go = Resources.Load(PainterDataAndConfig.PREFABS_RESOURCE_FOLDER + "/" + PainterDataAndConfig.PainterCameraName) as GameObject;
                        _inst = Instantiate(go).GetComponent<PainterCamera>();
                        _inst.name = PainterDataAndConfig.PainterCameraName;
                        CameraModuleBase.RefreshModules();
                    }
                }
     
                if (_inst)
                    _inst.gameObject.SetActive(true);

                return _inst;
            }

            private set
            {
                _inst = value;

            }
        }
        #endregion
        
        public bool disableSecondBufferUpdateDebug;

        public PlaytimePainter FocusedPainter => PlaytimePainter.selectedInPlaytime;
        
        public bool IsLinearColorSpace
        {
            get
            {
                #if UNITY_EDITOR
                      return UnityEditor.PlayerSettings.colorSpace == ColorSpace.Linear;
                #else
                      return Data.isLineraColorSpace;
                #endif
            }
            set
            {
                if (Data)
                    Data.isLineraColorSpace = value;
            }
        }

        #region Modules
      
        private static readonly CollectionMetaData _modulesMeta = new CollectionMetaData("Modules", true, true, true, false);

        public static T GetModule<T>() where T : CameraModuleBase 
        {
            if (CameraModuleBase.modules == null)
                CameraModuleBase.RefreshModules();

            var mod = CameraModuleBase.modules.Find(m => m.GetType() == typeof(T));
            
            return (T)mod;
        }
        
        #endregion

        #region Painting Layer
        
        public int LayerFlag => (1 << (Data ? Data.playtimePainterLayer : 30));

        private void UpdateCullingMask() {

            var l = (Data ? Data.playtimePainterLayer : 30);

            var flag = (1 << l);

            if (_mainCamera)
                _mainCamera.cullingMask &= ~flag;

            if (painterCamera)
                painterCamera.cullingMask = flag;

            QcUnity.RenamingLayer(l, "Playtime Painter's Layer");

            if (brushRenderer)
                brushRenderer.gameObject.layer = l;

#if UNITY_EDITOR

            var vis = UnityEditor.Tools.visibleLayers & flag;
            if (vis>0) {
                Debug.Log("Editor, hiding Layer {0}".F(l));
                UnityEditor.Tools.visibleLayers &= ~flag;
            }
#endif

        }
        
        [SerializeField] private Camera _mainCamera;

        public Camera MainCamera {
            get { return _mainCamera; }
            set {
                if (value && painterCamera && value == painterCamera) {
                    pegi.GameView.ShowNotification("Can't use Painter Camera as Main Camera");
                    return;
                }

                _mainCamera = value;

                UpdateCullingMask();
            }
        }
        
        [SerializeField] private Camera painterCamera;

        public RenderTexture TargetTexture
        {
            get { return painterCamera.targetTexture; }
            set { painterCamera.targetTexture = value; }
        }

        public Shader CurrentShader
        {
            set => brushRenderer.Set(value); 
        }

        public PlaytimePainter_RenderBrush brushPrefab;
        public const float OrthographicSize = 128; 

        public PlaytimePainter_RenderBrush brushRenderer;
        #endregion
        
        public static float _previewAlpha = 1;

        #region Encode & Decode

        public override CfgEncoder Encode() => base.Encode()//this.EncodeUnrecognized()
            .Add("mm", MeshManager)
            .Add_Abstract("pl", CameraModuleBase.modules)
            .Add("rts", PlaytimePainter_RenderTextureBuffersManager.renderBuffersSize);

        public override void Decode(string key, CfgData data) {
            switch (key) {
                case "pl":
                    data.ToList(out CameraModuleBase.modules, CameraModuleBase.all);
                    CameraModuleBase.RefreshModules();
                    break;
                case "mm": MeshManager.DecodeFull(data); break;
                case "rts": PlaytimePainter_RenderTextureBuffersManager.renderBuffersSize = data.ToInt(); break;
            }
        }

        #endregion

        #region Buffers MGMT

        [NonSerialized] private TextureMeta alphaBufferDataTarget;
        [NonSerialized] private Shader alphaBufferDataShader;

        [SerializeField] internal TextureMeta imgMetaUsingRendTex;
        [SerializeField] internal List<MaterialMeta> materialsUsingRenderTexture = new List<MaterialMeta>();
        public PlaytimePainter autodisabledBufferTarget;

        public void EmptyBufferTarget()
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
            PlaytimePainter_RenderTextureBuffersManager.DiscardPaintingBuffersContents();
        }

        internal void ChangeBufferTarget(TextureMeta newTarget, MaterialMeta mat, ShaderProperty.TextureValue parameter, PlaytimePainter painter)
        {

            if (newTarget != imgMetaUsingRendTex)  {

                if (materialsUsingRenderTexture.Count > 0)
                    PlaytimePainter.CheckSetOriginalShader();

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
        
        public static RenderTexture FrontBuffer => PlaytimePainter_RenderTextureBuffersManager.GetOrCreatePaintingBuffers()[0];

        public static RenderTexture BackBuffer => PlaytimePainter_RenderTextureBuffersManager.GetOrCreatePaintingBuffers()[1];

        public static RenderTexture AlphaBuffer => PlaytimePainter_RenderTextureBuffersManager.alphaBufferTexture;

        public static bool GotBuffers => Inst && PlaytimePainter_RenderTextureBuffersManager.GotPaintingBuffers;
        #endregion

        #region Brush Shader MGMT
        
        public void SHADER_BRUSH_UPDATE(PaintCommand.UV command) 
        {

            Brush brush = command.Brush;

            var id = command.TextureData;
            
            var rendTex = id.TargetIsRenderTexture();

            var brushType = brush.GetBrushType(!rendTex);

            var is3DBrush = command.Is3DBrush;

            #region Brush

            brush.previewDirty = false;

            PainterShaderVariables.BrushColorProperty.GlobalValue = brush.Color;

            PainterShaderVariables.BrushMaskProperty.GlobalValue = brush.mask.ToVector4();
            
            brushType.OnShaderBrushUpdate(brush);

            if (rendTex)
                PainterShaderVariables.SourceMaskProperty.GlobalValue = brush.useMask ? Data.masks.TryGet(brush.selectedSourceMask) : null;

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

            #endregion


            if (id == null)
                return;
            
            var blitMode = brush.GetBlitMode(!rendTex);
            
            var useAlphaBuffer = (brush.useAlphaBuffer && blitMode.SupportsAlphaBufferPainting && rendTex);
            
            float useTransparentLayerBackground = 0;

            PainterShaderVariables.OriginalTextureTexelSize.GlobalValue = new Vector4(
                1f/id.Width,
                1f/id.Height,
                id.Width,
                id.Height
                );

            var painter = command.TryGetPainter();

            if (id.IsATransparentLayer && painter) {

                var md = painter.MatDta;
                var mat = md.material;
                if (md != null && md.usePreviewShader && mat) {
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

            brushType.SetKeyword(id.UseTexCoord2);

            QcUnity.SetShaderKeyword(PainterShaderVariables.BRUSH_TEXCOORD_2, id.UseTexCoord2);

            //if (blitMode.SupportsTransparentLayer)
            QcUnity.SetShaderKeyword(PainterShaderVariables.TARGET_TRANSPARENT_LAYER, id.IsATransparentLayer);

            blitMode.SetKeyword(id).SetGlobalShaderParameters();

            if (rendTex && blitMode.UsingSourceTexture)
            {
                PainterShaderVariables.SourceTextureProperty.GlobalValue = Data.sourceTextures.TryGet(brush.selectedSourceTexture);
                PainterShaderVariables.TextureSourceParameters.GlobalValue = new Vector4(
                    (float)brush.srcColorUsage, 
                    brush.clampSourceTexture ? 1f : 0f,
                    useTransparentLayerBackground,
                    brush.ignoreSrcTextureTransparency ? 1f : 0f
                    );
            }
        }

        public void SHADER_STROKE_SEGMENT_UPDATE(PaintCommand.UV command)
        {
            Brush brush = command.Brush;
            TextureMeta textureMeta = command.TextureData;
  
            var isDoubleBuffer = !textureMeta.RenderTexture;

            var useSingle = !isDoubleBuffer || brush.IsSingleBufferBrush();

            var blitMode = brush.GetBlitMode(false);

            command.usedAlphaBuffer = //!useSingle && // Not sure
                brush.useAlphaBuffer && brush.GetBrushType(false).SupportsAlphaBufferPainting && blitMode.SupportsAlphaBufferPainting;

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

            if (!shd) {

                if (command.usedAlphaBuffer) {
                    shd = blitMode.ShaderForAlphaOutput; 
                    AlphaBufferSetDirtyBeforeRender(textureMeta, blitMode.ShaderForAlphaBufferBlit);
                }
                else
                    shd = useSingle ? blitMode.ShaderForSingleBuffer : blitMode.ShaderForDoubleBuffer;
            }


            if (!useSingle && !PlaytimePainter_RenderTextureBuffersManager.secondBufferUpdated)
                PlaytimePainter_RenderTextureBuffersManager.UpdateBufferTwo();
            
            SHADER_BRUSH_UPDATE(command); 

            TargetTexture = command.usedAlphaBuffer ? AlphaBuffer : textureMeta.CurrentRenderTexture();

            if (isDoubleBuffer)
                PainterShaderVariables.DESTINATION_BUFFER.GlobalValue = BackBuffer;

            _latestPaintShaderDebug = shd;

            CurrentShader = shd;
        }

        public static void SHADER_POSITION_AND_PREVIEW_UPDATE(Stroke st, bool hidePreview)
        {

            PainterShaderVariables.PREVIEW_BRUSH_UV_POS_FROM.GlobalValue = st.uvFrom.ToVector4(0, _previewAlpha);
            PainterShaderVariables.PREVIEW_BRUSH_UV_POS_TO.GlobalValue = st.uvTo.ToVector4(0, _previewAlpha);

            if (hidePreview && Math.Abs(_previewAlpha) < float.Epsilon)
                return;

            LerpUtils.IsLerpingBySpeed(ref _previewAlpha, hidePreview ? 0 : 1, 4f);

           // PainterShaderVariables.BRUSH_WORLD_POS_FROM.GlobalValue = _prevPosPreview.ToVector4(size);
          //  PainterShaderVariables.BRUSH_WORLD_POS_TO.GlobalValue = st.posTo.ToVector4((st.posTo - _prevPosPreview).magnitude);

            st.SetWorldPosInShader();
            if (!hidePreview)
            {
                st.SetPreviousValues();
            }

           // _prevPosPreview = st.posTo;
        }

        #endregion

        #region Alpha Buffer 

        internal void AlphaBufferSetDirtyBeforeRender(TextureMeta id, Shader shade) {

            if (alphaBufferDataTarget != null && (alphaBufferDataTarget != id || alphaBufferDataShader != shade))
                UpdateFromAlphaBuffer(alphaBufferDataTarget.CurrentRenderTexture(), alphaBufferDataShader);
            
            alphaBufferDataTarget = id;
            alphaBufferDataShader = shade;

        }

        public void DiscardAlphaBuffer() {
            PlaytimePainter_RenderTextureBuffersManager.ClearAlphaBuffer(); 
            alphaBufferDataTarget = null;
        }

        public void UpdateFromAlphaBuffer(RenderTexture rt, Shader shader)
        {

            if (rt) {
                PainterShaderVariables.AlphaPaintingBuffer.GlobalValue = AlphaBuffer;
                Render(AlphaBuffer, rt, shader);
            }

            DiscardAlphaBuffer();

            if (!PlaytimePainter_RenderTextureBuffersManager.secondBufferUpdated)
                PlaytimePainter_RenderTextureBuffersManager.UpdateBufferTwo();
            
        }

        public void FinalizePreviousAlphaDataTarget()
        {
            if (alphaBufferDataTarget != null)
            {
                UpdateFromAlphaBuffer(alphaBufferDataTarget.CurrentRenderTexture(), alphaBufferDataShader);
            }
        }

        #endregion

        #region Render

        public void Render()  {

            transform.rotation = Quaternion.identity;
            PainterShaderVariables.cameraPosition_Property.GlobalValue = transform.position.ToVector4();

            brushRenderer.gameObject.SetActive(true);
            painterCamera.Render();

            if (!disableSecondBufferUpdateDebug)
                brushRenderer.gameObject.SetActive(false);

            var trg = TargetTexture;

            if (trg == FrontBuffer)
                PlaytimePainter_RenderTextureBuffersManager.secondBufferUpdated = false;

            lastPainterCall = QcUnity.TimeSinceStartup();
            
            brushRenderer.AfterRender();
        }

        private void SetSourceMaskToCopyAChannel(ColorChanel sourceChannel) 
            => PainterShaderVariables.ChannelCopySourceMask.GlobalValue = new Vector4(
                sourceChannel == ColorChanel.R ? 1 : 0,
                sourceChannel == ColorChanel.G ? 1 : 0,
                sourceChannel == ColorChanel.B ? 1 : 0,
                sourceChannel == ColorChanel.A ? 1 : 0
        );

        public RenderTexture Render(Texture from, RenderTexture to, ColorChanel sourceChannel, ColorChanel intoChannel) {
            SetSourceMaskToCopyAChannel(sourceChannel);
            Render(from, to, Data.GetShaderToWriteInto(intoChannel));
            return to;
        }

        public RenderTexture RenderDepth(Texture from, RenderTexture to, ColorChanel intoChannel) 
            => Render(from, to, ColorChanel.R, intoChannel);

        public RenderTexture Render(Texture from, RenderTexture to, float alpha) {

            PainterShaderVariables.CopyColorTransparency.GlobalValue = alpha;

            return Render(from, to, Data.bufferBlendRGB);

        }

        public RenderTexture Render(Texture from, RenderTexture to, Shader shader) => brushRenderer.CopyBuffer(from, to, shader);
        
        public RenderTexture Render(Texture from, RenderTexture to, Material mat) =>  brushRenderer.CopyBuffer(from, to, mat);
         
        public RenderTexture Render(Texture from, RenderTexture to) => Render(from, to, Data.brushBufferCopy.Shader);

        internal RenderTexture Render(TextureMeta from, RenderTexture to) => Render(from.CurrentTexture(), to, Data.brushBufferCopy.Shader);

        internal RenderTexture Render(Texture from, TextureMeta to) => Render(from, to.CurrentRenderTexture(), Data.brushBufferCopy.Shader);

        public RenderTexture Render(Color col, RenderTexture to)
        {
            TargetTexture = to;
            brushRenderer.PrepareColorPaint(col);
            Render();

            return to;
        }
        
        public void UpdateBufferSegment()
        {
            if (!disableSecondBufferUpdateDebug)
            {
                //BackBuffer.DiscardContents();
                brushRenderer.Set(FrontBuffer);
                TargetTexture = BackBuffer;
                CurrentShader = Data.brushBufferCopy.Shader;
                Render();
                PlaytimePainter_RenderTextureBuffersManager.secondBufferUpdated = true;
                PlaytimePainter_RenderTextureBuffersManager.bigRtVersion++;
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

        public void OnBeforeBlitConfigurationChange() {
            FinalizePreviousAlphaDataTarget();
        }

        public void SubscribeToEditorUpdates()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.update += CombinedUpdate;
            #endif
        }
        
        public void OnEnable() {

            if (!MainCamera)
                MainCamera = Camera.main;

            PlaytimePainter_DepthProjectorCamera.triedToFindDepthCamera = false;

            PainterClass.applicationIsQuitting = false;

            Inst = this;

            if (!Data)
                dataHolder = Resources.Load("Painter_Data") as PainterDataAndConfig;

            MeshManager.OnEnable();

            if (!painterCamera)
                painterCamera = GetComponent<Camera>();
            
            if (!PainterDataAndConfig.toolEnabled && !Application.isEditor)
                    PainterDataAndConfig.toolEnabled = true;

#if UNITY_EDITOR

            UnityEditor.SceneManagement.EditorSceneManager.sceneSaving -= BeforeSceneSaved;
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaving += BeforeSceneSaved;
            
            IsLinearColorSpace = UnityEditor.PlayerSettings.colorSpace == ColorSpace.Linear;

            UnityEditor.EditorApplication.update -= CombinedUpdate;
            if (!QcUnity.ApplicationIsAboutToEnterPlayMode())
                SubscribeToEditorUpdates();

            if (!brushPrefab) {
                var go = Resources.Load(PainterDataAndConfig.PREFABS_RESOURCE_FOLDER +"/RenderCameraBrush") as GameObject;
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
            UnityEditor.EditorApplication.update -= CombinedUpdate;
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode == false)
                UnityEditor.EditorApplication.update += CombinedUpdate;
#endif

            autodisabledBufferTarget = null;

            CameraModuleBase.RefreshModules();

            foreach (var p in CameraModuleBase.modules)
                p?.Enable();
            
            if (Data)
                Data.ManagedOnEnable();

            UpdateCullingMask();

            PainterShaderVariables.BrushColorProperty.ConvertToLinear = IsLinearColorSpace;

        }

        private void OnDisable() {
            
            PainterClass.applicationIsQuitting = true;
            
            BeforeClosing();
            
        }

        private void BeforeClosing()
        {
            DownloadManager.Dispose();

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= CombinedUpdate;

            if (PlaytimePainter.previewHolderMaterial)
                PlaytimePainter.CheckSetOriginalShader();
            
            if (materialsUsingRenderTexture.Count > 0)
                autodisabledBufferTarget = materialsUsingRenderTexture[0].painterTarget;

            EmptyBufferTarget();

            #endif

            if (!CameraModuleBase.modules.IsNullOrEmpty())
                foreach (var p in CameraModuleBase.modules)
                    p?.Disable();
            
            if (Data)
                Data.ManagedOnDisable();

            PlaytimePainter_RenderTextureBuffersManager.OnDisable();
        }

        #if UNITY_EDITOR
        
        public void BeforeSceneSaved(Scene scene, string path) => BeforeClosing(); 
        #endif

        public void Update() {
            if (Application.isPlaying)
                CombinedUpdate();
        }

        public static GameObject refocusOnThis;
        #if UNITY_EDITOR
        private static int _scipFrames = 3;
        #endif

        public static double lastPainterCall;

        public static double lastManagedUpdate;

        private readonly Gate.Frame _frameGate = new Gate.Frame();

        public void CombinedUpdate() {

            if (_frameGate.TryEnter() == false)
               return; 

            if (!this || !Data)
                return;

            lastManagedUpdate = QcUnity.TimeSinceStartup();

            if (PlaytimePainter.IsCurrentTool && FocusedPainter)
                FocusedPainter.ManagedUpdateOnFocused();
            
            QcAsync.DefaultCoroutineManager.UpdateManagedCoroutines();

            MeshManager.CombinedUpdate();

            if (!Application.isPlaying && depthProjectorCamera)
                depthProjectorCamera.ManagedUpdate();
            
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
            
            var p = PlaytimePainter.currentlyPaintedObjectPainter;

            if (p && !Application.isPlaying && ((QcUnity.TimeSinceStartup() - lastPainterCall)>0.016f)) {

                if (p.TexMeta == null)
                    PlaytimePainter.currentlyPaintedObjectPainter = null;
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

        private static readonly pegi.GameView.Window OnGUIWindow = new pegi.GameView.Window();
        
        public void OnGUI()
        {

            if (!Cfg || !Cfg.enablePainterUIonPlay) return;
            
            if (FocusedPainter)
            {
                OnGUIWindow.Render(FocusedPainter, "{0} {1}".F(FocusedPainter.name, FocusedPainter.GetMaterialTextureProperty()));

               /* foreach (var p in CameraModuleBase.GuiPlugins)
                    p.OnGUI();*/
            }

            if (!PlaytimePainter.IsCurrentTool)
                OnGUIWindow.Collapse();

        }


        public AnimationCurve tmpCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 0.5f), new Keyframe(1, 1));
        
        public AnimationCurve InspectAnimationCurve(string role) {
            role.edit_Property(() => tmpCurve, this);
            return tmpCurve;
        }

        private int _inspectedDependecy = -1;
        private int _inspectedStuff = -1;
        private Shader _latestPaintShaderDebug;

       public override void Inspect()
        {
            pegi.nl();

            if ("Data && Settings".isEntered(ref _inspectedStuff, 0))
            {
                pegi.nl();
                "Painter Data".edit(ref dataHolder).nl();

                if (Data)
                    Data.Nested_Inspect().nl();
                else
                {
                    "NO CONFIG Scriptable Object".writeWarning();
                    pegi.nl();
                    DependenciesInspect(true);
                }
            }

            if (_inspectedStuff == -1 && Data)
                Data.ClickHighlight();

            pegi.nl();

            if ("Painter Camera".isEntered(ref _inspectedStuff, 1))
                DependenciesInspect(true);
          
            pegi.nl();

            if ("Depth Projector Camera".isEntered(ref _inspectedStuff, 2).nl())
            {
                if (PlaytimePainter_DepthProjectorCamera.Instance)
                {
                    PlaytimePainter_DepthProjectorCamera.Instance.Nested_Inspect().nl();
                }
                else if ("Instantiate".Click())
                    GetOrCreateProjectorCamera();
            }

            if ("Painter Camera Modules".isEntered(ref _inspectedStuff, 3, false).nl_ifNotEntered())
            {
                _modulesMeta.edit_List(CameraModuleBase.modules, CameraModuleBase.all);

                if (!_modulesMeta.IsInspectingElement)
                {
                    if ("Find Modules".Click())
                        CameraModuleBase.RefreshModules();

                    if ("Delete Modules".Click().nl())
                        CameraModuleBase.modules = null;
                }

                pegi.nl();
            }

            if ("Global Shader Variables".isEntered(ref _inspectedStuff, 4).nl())
                PainterShaderVariables.Inspect();

            pegi.nl();

            if ("Debug".isEntered(ref _inspectedStuff, 5).nl())
                QcUtils.InspectDebug();

            pegi.nl();

            if (_inspectedStuff == -1) 
            {
                "Latest Paint Shader (For Debug)".edit(ref _latestPaintShaderDebug).nl();
            }

        }
        
        public bool DependenciesInspect(bool showAll = false) {

            var changed = pegi.ChangeTrackStart();
            
            if (showAll)
            {

                pegi.nl();

                if ("Buffers".isEntered(ref _inspectedDependecy, 1).nl())
                {

                    PlaytimePainter_RenderTextureBuffersManager.Inspect().nl();

#if UNITY_EDITOR
                    "Disable Second Buffer Update (Debug Mode)".toggleIcon(ref disableSecondBufferUpdateDebug).nl();
#endif

                    return changed;
                }
                
                pegi.nl();

                "Download Manager".enter_Inspect(DownloadManager, ref _inspectedDependecy, 0).nl();

                if (_inspectedDependecy == -1)
                        pegi.FullWindow.DocumentationClickOpen("You can enable URL field in the Optional UI elements to get texture directly from web");


                if (_inspectedDependecy == -1)
                {

                    (IsLinearColorSpace ? "Linear" : "Gamma").nl();
                 
                   

#if UNITY_EDITOR
                    if ("Refresh Brush Shaders".Click().nl())
                    {
                        Data.CheckShaders(true);
                        pegi.GameView.ShowNotification("Shaders Refreshed");
                    }
#endif

                    "Using layer:".editLayerMask(ref Data.playtimePainterLayer).nl();

                }
                
            }

            bool showOthers = showAll && _inspectedDependecy == -1;

            #if UNITY_EDITOR
            if (!Data)  {
                pegi.nl();
                "No data Holder".edit(60, ref dataHolder).nl();

                if (icon.Refresh.Click("Try to find it")) {
                    PainterClass.applicationIsQuitting = false;
                    triedToFindPainterData = false;
                }

                if ("Create".Click().nl()) {
                    
                    PainterClass.applicationIsQuitting = false;
                    triedToFindPainterData = false;

                    if (!Data) {
                        dataHolder = ScriptableObject.CreateInstance<PainterDataAndConfig>();


                        UnityEditor.AssetDatabase.CreateAsset(dataHolder,
                                "Assets/Playtime-Painter/Resources/Painter_Data.asset");
                        UnityEditor.AssetDatabase.SaveAssets();
                        UnityEditor.AssetDatabase.Refresh();
                        
                    }
                }
            }
            #endif

           
            if (!painterCamera) {
                pegi.nl();
                "no painter camera".writeWarning();
                pegi.nl();
            }
            else
            {
                if (painterCamera.clearFlags != CameraClearFlags.Nothing)
                {
                    pegi.nl();
                    "Painter camera is not set to DontClear".writeWarning();
                    if ("Set to DontClear".Click().nl())
                    {
                        painterCamera.clearFlags = CameraClearFlags.Nothing;
                        painterCamera.SetToDirty();
                    }
                }
            }

            Camera depthCamera = depthProjectorCamera ? depthProjectorCamera._projectorCamera : null;

            bool depthAsMain = depthCamera && (depthCamera == MainCamera);

            if (showOthers || !MainCamera || depthAsMain) {
                pegi.nl();

                var cam = MainCamera;
                
                var cams = new List<Camera>(FindObjectsOfType<Camera>());

                if (painterCamera && cams.Contains(painterCamera))
                    cams.Remove(painterCamera);

                if (depthCamera && cams.Contains(depthCamera))
                    cams.Remove(depthCamera);

                if ("Main Camera".select(90, ref cam, cams))
                    MainCamera = cam;
                
                if (icon.Refresh.Click("Try to find camera tagged as Main Camera")) {
                    MainCamera = Camera.main;
                    if (!MainCamera)
                        pegi.GameView.ShowNotification("No camera is tagged as main");
                }

                pegi.nl();

                if (depthAsMain) {
                    "Depth projector camera is set as Main Camera - this is likely a mistake".writeWarning();
                    pegi.nl();
                }

                if (!cam)
                    "No Main Camera found. Playtime Painting will not be possible".writeWarning();

                pegi.nl();

            }

            return changed;
        }

    
        #endregion

    }


    [PEGI_Inspector_Override(typeof(PainterCamera))] internal class RenderTexturePainterEditor : PEGI_Inspector_Override { }


}