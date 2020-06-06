using System;
using System.Collections.Generic;
using PlayerAndEditorGUI;
using PlaytimePainter.CameraModules;
using PlaytimePainter.MeshEditing;
using QuizCannersUtilities;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace PlaytimePainter {
    
    [HelpURL(PlaytimePainter.OnlineManual)]
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class PainterCamera : PainterSystemMono {

        public static DepthProjectorCamera depthProjectorCamera;

        public static DepthProjectorCamera GetOrCreateProjectorCamera()
        {
          
                if (depthProjectorCamera)
                    return depthProjectorCamera;

                if (!DepthProjectorCamera.Instance)
                    depthProjectorCamera = QcUnity.Instantiate<DepthProjectorCamera>();

                return depthProjectorCamera;
            
        }

        public static readonly BrushMeshGenerator BrushMeshGenerator = new BrushMeshGenerator();

        public static readonly MeshEditorManager MeshManager = new MeshEditorManager();

        public static readonly TextureDownloadManager DownloadManager = new TextureDownloadManager();
        
        public AnimationCurve tmpCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 0.5f), new Keyframe(1, 1));
        
        #region Painter Data
        [SerializeField] private PainterDataAndConfig dataHolder;

        [NonSerialized] public bool triedToFindPainterData;

        public static PainterDataAndConfig Data  {
            get  {

                if (!_inst && !Inst)
                    return null;

                if (!_inst.triedToFindPainterData && !_inst.dataHolder) {
                  
                    _inst.dataHolder = Resources.Load<PainterDataAndConfig>("");

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
              
                if (!PainterSystem.applicationIsQuitting) {

                    if (!_inst)
                    {
                        var go = Resources.Load("prefabs/" + PainterDataAndConfig.PainterCameraName) as GameObject;
                        _inst = Instantiate(go).GetComponent<PainterCamera>();
                        _inst.name = PainterDataAndConfig.PainterCameraName;
                        CameraModuleBase.RefreshPlugins();
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

        public Light mainDirectionalLight;

        public PlaytimePainter FocusedPainter => PlaytimePainter.selectedInPlaytime;
        
        public bool IsLinearColorSpace
        {
            get
            {
                #if UNITY_EDITOR
                      return PlayerSettings.colorSpace == ColorSpace.Linear;
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
      
        private ListMetaData _modulesMeta = new ListMetaData("Modules", true, true, true, false);

        public IEnumerable<CameraModuleBase> Plugins
        {
            get {

                if (CameraModuleBase.modules == null)
                    CameraModuleBase.RefreshPlugins();

                return CameraModuleBase.modules;
            }
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

            var vis = Tools.visibleLayers & flag;
            if (vis>0) {
                Debug.Log("Editor, hiding Layer {0}".F(l));
                Tools.visibleLayers &= ~flag;
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
            set { brushRenderer.Set(value); }
        }

        public Material CurrentMaterial
        {
            get
            {
               return brushRenderer.GetMaterial();
            }
        }

        public RenderBrush brushPrefab;
        public const float OrthographicSize = 128; 

        public RenderBrush brushRenderer;
        #endregion

        public Material defaultMaterial;

        private static Vector3 _prevPosPreview;

        public static float _previewAlpha = 1;

        #region Encode & Decode

        public override CfgEncoder Encode() => this.EncodeUnrecognized()
            .Add("mm", MeshManager)
            .Add_Abstract("pl", CameraModuleBase.modules, _modulesMeta)
            .Add("rts", RenderTextureBuffersManager.renderBuffersSize);

        public override bool Decode(string tg, string data) {
            switch (tg) {
                case "pl":
                    data.Decode_List(out CameraModuleBase.modules, ref _modulesMeta, CameraModuleBase.all);
                    CameraModuleBase.RefreshPlugins();
                    break;
                case "mm": MeshManager.Decode(data); break;
                case "rts": RenderTextureBuffersManager.renderBuffersSize = data.ToInt(); break;
                default: return false;
            }

            return true;
        }

        #endregion

        #region Buffer Scaling

        #endregion

        #region Buffers MGMT

        [NonSerialized] private TextureMeta alphaBufferDataTarget;
        [NonSerialized] private Shader alphaBufferDataShader;

        public MeshRenderer secondBufferDebug;

        public MeshRenderer alphaBufferDebug;

        public TextureMeta imgMetaUsingRendTex;
        public List<MaterialMeta> materialsUsingRenderTexture = new List<MaterialMeta>();
        public PlaytimePainter autodisabledBufferTarget;

        public void CheckPaintingBuffers()
        {
            if (!RenderTextureBuffersManager.GotPaintingBuffers)
                RecreateBuffersIfDestroyed();
        }

        public void RecreateBuffersIfDestroyed()
        {

            var cfg = TexMgmtData;

            if (!cfg)
                return;

            if (secondBufferDebug)
                secondBufferDebug.sharedMaterial.mainTexture = BackBuffer; 

            if (alphaBufferDebug)
                alphaBufferDebug.sharedMaterial.mainTexture = RenderTextureBuffersManager.alphaBufferTexture;

        }

        public void EmptyBufferTarget()
        {

            if (imgMetaUsingRendTex == null)
                return;

            if (imgMetaUsingRendTex.texture2D)
                imgMetaUsingRendTex.RenderTexture_To_Texture2D();

            imgMetaUsingRendTex.target = TexTarget.Texture2D;

            foreach (var m in materialsUsingRenderTexture)
                m.SetTextureOnLastTarget(imgMetaUsingRendTex);

            materialsUsingRenderTexture.Clear();
            imgMetaUsingRendTex = null;
            RenderTextureBuffersManager.DiscardPaintingBuffersContents();
        }

        public void ChangeBufferTarget(TextureMeta newTarget, MaterialMeta mat, ShaderProperty.TextureValue parameter, PlaytimePainter painter)
        {

            if (newTarget != imgMetaUsingRendTex)  {

                if (materialsUsingRenderTexture.Count > 0)
                    PlaytimePainter.CheckSetOriginalShader();

                if (imgMetaUsingRendTex != null) {

                    if (imgMetaUsingRendTex.texture2D)
                        imgMetaUsingRendTex.RenderTexture_To_Texture2D();

                    imgMetaUsingRendTex.target = TexTarget.Texture2D;

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
        
        public static RenderTexture FrontBuffer => RenderTextureBuffersManager.GetOrCreatePaintingBuffers()[0];

        public static RenderTexture BackBuffer => RenderTextureBuffersManager.GetOrCreatePaintingBuffers()[1];

        public static RenderTexture AlphaBuffer => RenderTextureBuffersManager.alphaBufferTexture;

        public static bool GotBuffers => Inst && RenderTextureBuffersManager.GotPaintingBuffers;
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

            float brushSizeUvSpace = brush.Size(is3DBrush) / (id == null ? 256 : Mathf.Min(id.width, id.height));

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
                1f/id.width,
                1f/id.height,
                id.width,
                id.height
                );

            var painter = command.TryGetPainter();

            if (id.isATransparentLayer && painter) {

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

            brushType.SetKeyword(id.useTexCoord2);

            QcUnity.SetShaderKeyword(PainterShaderVariables.BRUSH_TEXCOORD_2, id.useTexCoord2);

            //if (blitMode.SupportsTransparentLayer)
            QcUnity.SetShaderKeyword(PainterShaderVariables.TARGET_TRANSPARENT_LAYER, id.isATransparentLayer);

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
            Brush bc = command.Brush;
            TextureMeta id = command.TextureData;
            Stroke stroke = command.Stroke;
            
            CheckPaintingBuffers();
            
            var isDoubleBuffer = !id.renderTexture;

            var useSingle = !isDoubleBuffer || bc.IsSingleBufferBrush();

            var blitMode = bc.GetBlitMode(false);

            command.usedAlphaBuffer = !useSingle && bc.useAlphaBuffer && bc.GetBrushType(false).SupportsAlphaBufferPainting && blitMode.SupportsAlphaBufferPainting;

            var painter = command.TryGetPainter();

            Shader shd = null;
            if (painter)
                foreach (var pl in CameraModuleBase.BrushPlugins) {
                    var bs = useSingle ? pl.GetBrushShaderSingleBuffer(painter) : pl.GetBrushShaderDoubleBuffer(painter);
                    if (!bs) continue;
                    shd = bs;
                    break;
                }

            if (!shd) {

                if (command.usedAlphaBuffer) {
                    shd = blitMode.ShaderForAlphaOutput; 
                    AlphaBufferSetDirtyBeforeRender(id, blitMode.ShaderForAlphaBufferBlit);
                }
                else
                    shd = useSingle ? blitMode.ShaderForSingleBuffer : blitMode.ShaderForDoubleBuffer;
            }


            if (!useSingle && !RenderTextureBuffersManager.secondBufferUpdated)
                RenderTextureBuffersManager.UpdateBufferTwo();
            
            SHADER_BRUSH_UPDATE(command); 

            TargetTexture = command.usedAlphaBuffer ? AlphaBuffer : id.CurrentRenderTexture();

            if (isDoubleBuffer)
                PainterShaderVariables.DESTINATION_BUFFER.GlobalValue = BackBuffer;
            
            CurrentShader = shd;
        }

        public static void SHADER_POSITION_AND_PREVIEW_UPDATE(Stroke st, bool hidePreview, float size)
        {

            PainterShaderVariables.BRUSH_UV_POS_FROM.GlobalValue = st.uvFrom.ToVector4(0, _previewAlpha);
            PainterShaderVariables.BRUSH_UV_POS_TO.GlobalValue = st.uvTo.ToVector4(0, _previewAlpha);

            if (hidePreview && Math.Abs(_previewAlpha) < float.Epsilon)
                return;

            LerpUtils.IsLerpingBySpeed(ref _previewAlpha, hidePreview ? 0 : 1, 4f);

            PainterShaderVariables.BRUSH_WORLD_POS_FROM.GlobalValue = _prevPosPreview.ToVector4(size);
            PainterShaderVariables.BRUSH_WORLD_POS_TO.GlobalValue = st.posTo.ToVector4((st.posTo - _prevPosPreview).magnitude); 

            _prevPosPreview = st.posTo;
        }
        
        #endregion

        #region Alpha Buffer 

        public void AlphaBufferSetDirtyBeforeRender(TextureMeta id, Shader shade) {

            if (alphaBufferDataTarget != null && (alphaBufferDataTarget != id || alphaBufferDataShader != shade))
                UpdateFromAlphaBuffer(alphaBufferDataTarget.CurrentRenderTexture(), alphaBufferDataShader);
            
            alphaBufferDataTarget = id;
            alphaBufferDataShader = shade;

        }

        public void DiscardAlphaBuffer() {
            RenderTextureBuffersManager.ClearAlphaBuffer(); 
            alphaBufferDataTarget = null;
        }

        public void UpdateFromAlphaBuffer(RenderTexture rt, Shader shader)
        {

            if (rt) {
                PainterShaderVariables.AlphaPaintingBuffer.GlobalValue = AlphaBuffer;
                Render(AlphaBuffer, rt, shader);
            }

            DiscardAlphaBuffer();

            if (!RenderTextureBuffersManager.secondBufferUpdated)
                RenderTextureBuffersManager.UpdateBufferTwo();
            
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
                RenderTextureBuffersManager.secondBufferUpdated = false;

            lastPainterCall = QcUnity.TimeSinceStartup();
            
            brushRenderer.AfterRender();
        }
        
        void SetSourceMaskToCopyAChannel(ColorChanel sourceChannel) 
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
         
        public RenderTexture Render(Texture from, RenderTexture to) => Render(from, to, Data.brushBufferCopy);

        public RenderTexture Render(TextureMeta from, RenderTexture to) => Render(from.CurrentTexture(), to, Data.brushBufferCopy);

        public RenderTexture Render(Texture from, TextureMeta to) => Render(from, to.CurrentRenderTexture(), Data.brushBufferCopy);

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
                CurrentShader = Data.brushBufferCopy;
                Render();
                RenderTextureBuffersManager.secondBufferUpdated = true;
                RenderTextureBuffersManager.bigRtVersion++;
            }
        }
        #endregion

        #region Updates

        public void TryApplyBufferChangesTo(TextureMeta id) {

            if ((id != null) && (id == alphaBufferDataTarget))
                FinalizePreviousAlphaDataTarget();
            
        }

        public void TryDiscardBufferChangesTo(TextureMeta id) {

            if (id != null && id == alphaBufferDataTarget)
                DiscardAlphaBuffer();
            
        }

        public void OnBeforeBlitConfigurationChange() {
            FinalizePreviousAlphaDataTarget();
        }

        public void SubscribeToEditorUpdates()
        {
            #if UNITY_EDITOR
            EditorApplication.update += CombinedUpdate;
            #endif
        }
        
        public void OnEnable() {

            if (!MainCamera)
                MainCamera = Camera.main;

            DepthProjectorCamera.triedToFindDepthCamera = false;

            PainterSystem.applicationIsQuitting = false;

            Inst = this;

            if (!Data)
                dataHolder = Resources.Load("Painter_Data") as PainterDataAndConfig;

            MeshManager.OnEnable();

            if (!painterCamera)
                painterCamera = GetComponent<Camera>();
            
            if (!PainterDataAndConfig.toolEnabled && !Application.isEditor)
                    PainterDataAndConfig.toolEnabled = true;
        
            #if UNITY_EDITOR

            EditorSceneManager.sceneSaving -= BeforeSceneSaved;
            EditorSceneManager.sceneSaving += BeforeSceneSaved;

            if (!defaultMaterial)
                defaultMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");

            if (!defaultMaterial) Debug.Log("Default Material not found.");

            IsLinearColorSpace = PlayerSettings.colorSpace == ColorSpace.Linear;

            EditorApplication.update -= CombinedUpdate;
            if (!QcUnity.ApplicationIsAboutToEnterPlayMode())
                SubscribeToEditorUpdates();


            if (!brushPrefab) {
                var go = Resources.Load("prefabs/RenderCameraBrush") as GameObject;
                if (go) {
                    brushPrefab = go.GetComponent<RenderBrush>();
                    if (!brushPrefab)
                        Debug.Log("Couldn't find brush prefab.");
                }
                else
                    Debug.LogError("Couldn't load brush Prefab");
            }
            
            #endif

            if (!brushRenderer){
                brushRenderer = GetComponentInChildren<RenderBrush>();
                if (!brushRenderer)
                    brushRenderer = Instantiate(brushPrefab.gameObject, transform).GetComponent<RenderBrush>();
            }
         
            transform.position = Vector3.up * 3000;
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;

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
            EditorApplication.update -= CombinedUpdate;
            if (EditorApplication.isPlayingOrWillChangePlaymode == false)
                EditorApplication.update += CombinedUpdate;
#endif

            RecreateBuffersIfDestroyed();
            
            autodisabledBufferTarget = null;

            CameraModuleBase.RefreshPlugins();

            foreach (var p in CameraModuleBase.modules)
                p?.Enable();
            
            if (Data)
                Data.ManagedOnEnable();

            UpdateCullingMask();

            PainterShaderVariables.BrushColorProperty.ConvertToLinear = IsLinearColorSpace;

        }

        private void OnDisable() {
            
            PainterSystem.applicationIsQuitting = true;
            
            BeforeClosing();
            
        }

        private void BeforeClosing()
        {
            DownloadManager.Dispose();
            
            #if UNITY_EDITOR
            EditorApplication.update -= CombinedUpdate;

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

            RenderTextureBuffersManager.OnDisable();
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

        public void CombinedUpdate() {

            if (!this || !Data)
                return;

            lastManagedUpdate = QcUnity.TimeSinceStartup();

            if (PlaytimePainter.IsCurrentTool && FocusedPainter)
                FocusedPainter.ManagedUpdateOnFocused();
            
            QcAsync.UpdateManagedCoroutines();

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
                CameraModuleBase.RefreshPlugins();
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
                OnGUIWindow.Render(FocusedPainter, "{0} {1}".F(FocusedPainter.name, FocusedPainter.GetMaterialTextureProperty));

                foreach (var p in CameraModuleBase.GuiPlugins)
                    p.OnGUI();
            }

            if (!PlaytimePainter.IsCurrentTool)
                OnGUIWindow.Collapse();

        }

        readonly QcUtils.ChillLogger logger = new QcUtils.ChillLogger("error");

        public AnimationCurve InspectAnimationCurve(string role) {
            role.edit_Property(() => tmpCurve, this);

            return tmpCurve;
        }

        private int _inspectedDependecy = -1;
        
        public override bool Inspect()
        {

            pegi.toggleDefaultInspector(this).nl();

            var changed = false;

            if (Data)
                Data.Nested_Inspect().nl(ref changed);
            
            return changed;
        }
        
        public bool DependenciesInspect(bool showAll = false) {

            var changed = false;
            
            if (showAll)
            {

                pegi.nl();

                if ("Buffers".enter(ref _inspectedDependecy, 1).nl())
                {

                    RenderTextureBuffersManager.Inspect().nl(ref changed);

#if UNITY_EDITOR
                    "Disable Second Buffer Update (Debug Mode)".toggleIcon(ref disableSecondBufferUpdateDebug).nl();
#endif

                    return changed;
                }
                
                pegi.nl();

                "Download Manager".enter_Inspect(DownloadManager, ref _inspectedDependecy, 0).nl(ref changed);

                if (_inspectedDependecy == -1)
                        pegi.PopUpService.fullWindowDocumentationClickOpen("You can enable URL field in the Optional UI elements to get texture directly from web");


                if (_inspectedDependecy == -1)
                {

                    (IsLinearColorSpace ? "Linear" : "Gamma").nl();
                 
                    "Main Directional Light".edit(ref mainDirectionalLight).nl(ref changed);

#if UNITY_EDITOR
                    if ("Refresh Brush Shaders".Click().nl())
                    {
                        Data.CheckShaders(true);
                        pegi.GameView.ShowNotification("Shaders Refreshed");
                    }
#endif

                    "Using layer:".editLayerMask(ref Data.playtimePainterLayer).nl(ref changed);
                }
                
            }

            bool showOthers = showAll && _inspectedDependecy == -1;

            #if UNITY_EDITOR
            if (!Data)  {
                pegi.nl();
                "No data Holder".edit(60, ref dataHolder).nl(ref changed);

                if (icon.Refresh.Click("Try to find it")) {
                    PainterSystem.applicationIsQuitting = false;
                    triedToFindPainterData = false;
                }

                if ("Create".Click().nl()) {
                    
                    PainterSystem.applicationIsQuitting = false;
                    triedToFindPainterData = false;

                    if (!Data) {
                        dataHolder = ScriptableObject.CreateInstance<PainterDataAndConfig>();

                        AssetDatabase.CreateAsset(dataHolder,
                            "Assets/Tools/Playtime Painter/Resources/Painter_Data.asset");
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
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

                if ("Main Camera".select(90, ref cam, cams).changes(ref changed))
                    MainCamera = cam;
                
                if (icon.Refresh.Click("Try to find camera tagged as Main Camera", ref changed)) {
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

        public bool ModulsInspect() {

            var changed = false;
            
            _modulesMeta.edit_List(ref CameraModuleBase.modules, CameraModuleBase.all).changes(ref changed);

            if (!_modulesMeta.Inspecting) {

                if ("Find Modules".Click())
                    CameraModuleBase.RefreshPlugins();

                if ("Delete Modules".Click().nl())
                    CameraModuleBase.modules = null;

            }
       
            return changed;
        }
        
        #endregion

    }



#if UNITY_EDITOR
    [CustomEditor(typeof(PainterCamera))]
    public class PainterCameraDrawer : PEGI_Inspector_Mono<PainterCamera> { }
#endif

}