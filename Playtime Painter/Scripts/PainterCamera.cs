using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections;
using QuizCannersUtilities;
using PlayerAndEditorGUI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

#endif

namespace PlaytimePainter {
    
    using VectorValue = ShaderProperty.VectorValue;
    using TextureValue = ShaderProperty.TextureValue;
    using FloatValue = ShaderProperty.FloatValue;

    [HelpURL(PlaytimePainter.OnlineManual)]
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class PainterCamera : PainterSystemMono {

        public static DepthProjectorCamera depthProjectorCamera;

        public static DepthProjectorCamera GetProjectorCamera()
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

                       /* var go = new GameObject(PainterDataAndConfig.PainterCameraName);
                        _inst = go.AddComponent<PainterCamera>();
                        PainterSystemManagerModuleBase.RefreshPlugins();
                        */
                        //#if UNITY_EDITOR
                            var go = Resources.Load("prefabs/" + PainterDataAndConfig.PainterCameraName) as GameObject;
                            _inst = Instantiate(go).GetComponent<PainterCamera>();
                            _inst.name = PainterDataAndConfig.PainterCameraName;
                            PainterSystemManagerModuleBase.RefreshPlugins();

                            //#endif

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

        public PlaytimePainter focusedPainter;

        public List<TextureMeta> blitJobsActive = new List<TextureMeta>();

        public bool isLinearColorSpace;

        #region Modules
      
        private ListMetaData _modulesMeta = new ListMetaData("Modules", true, true, true, false);

        public IEnumerable<PainterSystemManagerModuleBase> Plugins
        {
            get {

                if (PainterSystemManagerModuleBase.modules == null)
                    PainterSystemManagerModuleBase.RefreshPlugins();

                return PainterSystemManagerModuleBase.modules;
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
                    "Can't use Painter Camera as Main Camera".showNotificationIn3D_Views();
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
            .Add_Abstract("pl", PainterSystemManagerModuleBase.modules, _modulesMeta)
            .Add("rts", RenderTextureBuffersManager.renderBuffersSize);

        public override bool Decode(string tg, string data) {
            switch (tg) {
                case "pl":
                    data.Decode_List(out PainterSystemManagerModuleBase.modules, ref _modulesMeta, PainterSystemManagerModuleBase.all);
                    PainterSystemManagerModuleBase.RefreshPlugins();
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

            imgMetaUsingRendTex.destination = TexTarget.Texture2D;

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

                    imgMetaUsingRendTex.destination = TexTarget.Texture2D;

                    foreach (var m in materialsUsingRenderTexture)
                        m.SetTextureOnLastTarget(imgMetaUsingRendTex);
                }

                materialsUsingRenderTexture.Clear();
                autodisabledBufferTarget = null;
                imgMetaUsingRendTex = newTarget;
            }

            mat.bufferParameterTarget = parameter;
            mat.painterTarget = painter;
            materialsUsingRenderTexture.Add(mat);
        }

 
        public static RenderTexture FrontBuffer => RenderTextureBuffersManager.GetOrCreatePaintingBuffers()[0];

        public static RenderTexture BackBuffer => RenderTextureBuffersManager.GetOrCreatePaintingBuffers()[1];

        public static RenderTexture AlphaBuffer => RenderTextureBuffersManager.alphaBufferTexture;

        public static bool GotBuffers => Inst && RenderTextureBuffersManager.GotPaintingBuffers;
        #endregion

        #region Brush Shader MGMT

 
      


        private readonly FloatValue _copyChannelTransparency = new FloatValue("_pp_CopyBlitAlpha");

        private static readonly VectorValue ChannelCopySourceMask =          new VectorValue("_ChannelSourceMask");
        public static readonly VectorValue BrushColorProperty =              new VectorValue("_brushColor");
        private static readonly VectorValue BrushMaskProperty =              new VectorValue("_brushMask");
        private static readonly VectorValue MaskDynamicsProperty =           new VectorValue("_maskDynamics");
        private static readonly VectorValue MaskOffsetProperty =             new VectorValue("_maskOffset");
        private static readonly VectorValue BrushFormProperty =              new VectorValue("_brushForm");
        private static readonly VectorValue TextureSourceParameters =        new VectorValue("_srcTextureUsage");
        private static readonly VectorValue cameraPosition_Property =        new VectorValue("_RTcamPosition");
        private static readonly VectorValue AlphaBufferConfigProperty =      new VectorValue("_pp_AlphaBufferCfg");
        private static readonly VectorValue OriginalTextureTexelSize =       new VectorValue("_TargetTexture_TexelSize");

        private static readonly TextureValue SourceMaskProperty =            new TextureValue("_SourceMask");
        private static readonly TextureValue SourceTextureProperty =         new TextureValue("_SourceTexture");
        private static readonly TextureValue TransparentLayerUnderProperty = new TextureValue("_TransparentLayerUnderlay");
        private static readonly TextureValue AlphaPaintingBuffer =           new TextureValue("_pp_AlphaBuffer");

        public void SHADER_BRUSH_UPDATE(BrushConfig brush = null, float brushAlpha = 1, TextureMeta id = null, PlaytimePainter painter = null)
        {
            if (brush == null)
                brush = GlobalBrush;

            if (id == null && painter)
                id = painter.TexMeta;
            
            brush.previewDirty = false;

            if (id == null)
                return;

            float textureWidth = id.width;
            var rendTex = id.TargetIsRenderTexture();
            var brushType = brush.GetBrushType(!rendTex);
            var blitMode = brush.GetBlitMode(!rendTex);
            var is3DBrush = brush.IsA3DBrush(painter);
            var useAlphaBuffer = (brush.useAlphaBuffer && blitMode.SupportsAlphaBufferPainting && rendTex);

            BrushColorProperty.GlobalValue = brush.Color;

            BrushMaskProperty.GlobalValue = brush.mask.ToVector4(); 

            float useTransparentLayerBackground = 0;

            OriginalTextureTexelSize.GlobalValue = new Vector4(
                1f/id.width,
                1f/id.height,
                id.width,
                id.height
                );

            if (id.isATransparentLayer) {

                var md = painter.MatDta;
                var mat = md.material;
                if (md != null && md.usePreviewShader && mat) {
                    var mt = mat.mainTexture;
                    TransparentLayerUnderProperty.GlobalValue = mt;
                    useTransparentLayerBackground = (mt && (id != mt.GetImgDataIfExists())) ? 1 : 0;
                }
            }
            
            brushType.OnShaderBrushUpdate(brush);

            if (rendTex)
                SourceMaskProperty.GlobalValue = brush.useMask ? Data.masks.TryGet(brush.selectedSourceMask) : null;

            MaskDynamicsProperty.GlobalValue = new Vector4(
                brush.maskTiling,
                rendTex ? brush.hardness * brush.hardness : 0,       // y - Hardness is 0 to do correct preview for Texture2D brush 
                ((brush.flipMaskAlpha && brush.useMask) ? 0 : 1) ,  // z - flip mask if any
                (brush.maskFromGreyscale && brush.useMask) ? 1 : 0);

            MaskOffsetProperty.GlobalValue = brush.maskOffset.ToVector4();
                
            BrushFormProperty.GlobalValue = new Vector4(
                brushAlpha, // x - transparency
                brush.Size(is3DBrush), // y - scale for sphere
                brush.Size(is3DBrush) / textureWidth, // z - scale for uv space
                brush.blurAmount); // w - blur amount

            AlphaBufferConfigProperty.GlobalValue = new Vector4(
                brush.alphaLimitForAlphaBuffer,
                brush.worldSpaceBrushPixelJitter ? 1 : 0,
                useAlphaBuffer ? 1 : 0,
                0);

            AlphaPaintingBuffer.GlobalValue = AlphaBuffer;

            brushType.SetKeyword(id.useTexCoord2);

            QcUnity.SetShaderKeyword(PainterDataAndConfig.BRUSH_TEXCOORD_2, id.useTexCoord2);

            //if (blitMode.SupportsTransparentLayer)
            QcUnity.SetShaderKeyword(PainterDataAndConfig.TARGET_TRANSPARENT_LAYER, id.isATransparentLayer);

            blitMode.SetKeyword(id).SetGlobalShaderParameters();

            if (rendTex && blitMode.UsingSourceTexture)
            {
                SourceTextureProperty.GlobalValue = Data.sourceTextures.TryGet(brush.selectedSourceTexture);
                TextureSourceParameters.GlobalValue = new Vector4(
                    (float)brush.srcColorUsage, 
                    brush.clampSourceTexture ? 1f : 0f,
                    useTransparentLayerBackground,
                    brush.ignoreSrcTextureTransparency ? 1f : 0f
                    );
            }
        }

        public void SHADER_STROKE_SEGMENT_UPDATE(BrushConfig bc, float brushAlpha, TextureMeta id, StrokeVector stroke, out bool alphaBuffer, PlaytimePainter pntr = null)
        {
            CheckPaintingBuffers();
            
            var isDoubleBuffer = !id.renderTexture;

            var useSingle = !isDoubleBuffer || bc.IsSingleBufferBrush();

            var blitMode = bc.GetBlitMode(false);

            alphaBuffer = !useSingle && bc.useAlphaBuffer && bc.GetBrushType(false).SupportsAlphaBufferPainting && blitMode.SupportsAlphaBufferPainting;

            Shader shd = null;
            if (pntr)
                foreach (var pl in PainterSystemManagerModuleBase.BrushPlugins) {
                    var bs = useSingle ? pl.GetBrushShaderSingleBuffer(pntr) : pl.GetBrushShaderDoubleBuffer(pntr);
                    if (!bs) continue;
                    shd = bs;
                    break;
                }

            if (!shd) {

                if (alphaBuffer) {
                    shd = blitMode.ShaderForAlphaOutput; 
                    AlphaBufferSetDirtyBeforeRender(id, blitMode.ShaderForAlphaBufferBlit);
                }
                else
                    shd = useSingle ? blitMode.ShaderForSingleBuffer : blitMode.ShaderForDoubleBuffer;
            }


            if (!useSingle && !RenderTextureBuffersManager.secondBufferUpdated)
                RenderTextureBuffersManager.UpdateBufferTwo();

            //if (stroke.firstStroke)
            SHADER_BRUSH_UPDATE(bc, brushAlpha, id, pntr);

            TargetTexture = alphaBuffer ? AlphaBuffer : id.CurrentRenderTexture();

            if (isDoubleBuffer)
                PainterDataAndConfig.DESTINATION_BUFFER.GlobalValue = BackBuffer;
            
            CurrentShader = shd;
        }

        public static void SHADER_POSITION_AND_PREVIEW_UPDATE(StrokeVector st, bool hidePreview, float size)
        {

            PainterDataAndConfig.BRUSH_POINTED_UV.GlobalValue = st.uvTo.ToVector4(0, _previewAlpha);

            if (hidePreview && Math.Abs(_previewAlpha) < float.Epsilon)
                return;

            LerpUtils.IsLerpingBySpeed(ref _previewAlpha, hidePreview ? 0 : 1, 4f);

            PainterDataAndConfig.BRUSH_WORLD_POS_FROM.GlobalValue = _prevPosPreview.ToVector4(size);
            PainterDataAndConfig.BRUSH_WORLD_POS_TO.GlobalValue = st.posTo.ToVector4((st.posTo - _prevPosPreview).magnitude); 

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
                AlphaPaintingBuffer.GlobalValue = AlphaBuffer;
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

            //Debug.Log("Render call");

            transform.rotation = Quaternion.identity;
            cameraPosition_Property.GlobalValue = transform.position.ToVector4();

            brushRenderer.gameObject.SetActive(true);
            painterCamera.Render();
            brushRenderer.gameObject.SetActive(false);

            var trg = TargetTexture;

            if (trg == FrontBuffer)
                RenderTextureBuffersManager.secondBufferUpdated = false;

            lastPainterCall = Time.time;
            
            brushRenderer.AfterRender();
        }
        
        void SetSourceMaskToCopyAChannel(ColorChanel sourceChannel) 
            => ChannelCopySourceMask.GlobalValue = new Vector4(
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

            _copyChannelTransparency.GlobalValue = alpha;

            return Render(from, to, Data.bufferBlendRGB);

        }

        public RenderTexture Render(Texture from, RenderTexture to, Shader shade) => brushRenderer.CopyBuffer(from, to, shade);
        
        public RenderTexture Render(Texture from, RenderTexture to, Material mat) =>  brushRenderer.CopyBuffer(from, to, mat);
         
        public RenderTexture Render(Texture from, RenderTexture to) => Render(from, to, Data.brushBufferCopy);

        public RenderTexture Render(TextureMeta from, RenderTexture to) => Render(from.CurrentTexture(), to, Data.brushBufferCopy);

        public RenderTexture Render(Texture from, TextureMeta to) => Render(from, to.CurrentRenderTexture(), Data.brushBufferCopy);

        public void Render(Color col, RenderTexture to)
        {
            TargetTexture = to;
            brushRenderer.PrepareColorPaint(col);
            Render();
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

        private void OnEnable() {

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

            isLinearColorSpace = PlayerSettings.colorSpace == ColorSpace.Linear;

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
                    //brushRenderer.transform.parent = transform;
                
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

            PainterSystemManagerModuleBase.RefreshPlugins();

            foreach (var p in PainterSystemManagerModuleBase.modules)
                p?.Enable();
            
            if (Data)
                Data.ManagedOnEnable();

            UpdateCullingMask();

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

            if (!PainterSystemManagerModuleBase.modules.IsNullOrEmpty())
                foreach (var p in PainterSystemManagerModuleBase.modules)
                    p?.Disable();
            
            if (Data)
                Data.ManagedOnDisable();

            RenderTextureBuffersManager.OnDisable();
        }

        #if UNITY_EDITOR
        
        public void BeforeSceneSaved(UnityEngine.SceneManagement.Scene scene, string path) => BeforeClosing(); 
        #endif

        public void Update() {
            if (Application.isPlaying)
                CombinedUpdate();
        }

        public static GameObject refocusOnThis;
        #if UNITY_EDITOR
        private static int _scipFrames = 3;
        #endif

        public static float lastPainterCall = 0;

        public static float lastManagedUpdate = 0;

        public void CombinedUpdate() {

            if (!this || !Data)
                return;

            lastManagedUpdate = Time.time;

            if (PlaytimePainter.IsCurrentTool && focusedPainter)
                focusedPainter.ManagedUpdate();
            
            if (GlobalBrush.previewDirty)
                SHADER_BRUSH_UPDATE();

            QcAsync.UpdateManagedCoroutines();

            PlaytimePainter uiPainter = null;

            MeshManager.CombinedUpdate();

            if (!Application.isPlaying && depthProjectorCamera)
                depthProjectorCamera.ManagedUpdate();

#if UNITY_2018_1_OR_NEWER
            foreach ( var j in blitJobsActive) 
                if (j.jobHandle.IsCompleted)
                    j.CompleteJob();
#endif
            
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

            if (!uiPainter || !uiPainter.CanPaint()) {

                var p = PlaytimePainter.currentlyPaintedObjectPainter;

                if (p && !Application.isPlaying && ((Time.time - lastPainterCall)>0.016f)) {

                    if (p.TexMeta == null)
                        PlaytimePainter.currentlyPaintedObjectPainter = null;
                    else {
                        TexMgmtData.brushConfig.Paint(p.stroke, p);
                        p.ManagedUpdate();
                    }
                }
            }

            var needRefresh = false;
            if (PainterSystemManagerModuleBase.modules!= null)
                foreach (var pl in PainterSystemManagerModuleBase.modules)
                    if (pl != null)
                        pl.Update();
                    else needRefresh = true;

            if (needRefresh) {
                Debug.Log("Refreshing modules");
                PainterSystemManagerModuleBase.RefreshPlugins();
            }

            lastPainterCall = Time.time;


        }
        
        #endregion

        #region Inspector

        readonly QcUtils.ChillLogger logger = new QcUtils.ChillLogger("error");

        public AnimationCurve InspectAnimationCurve(string role) {
            role.edit_Property(() => tmpCurve, this);

            return tmpCurve;
        }

        public override bool Inspect() {

            var changed = false;

            if (Data)
                Data.Nested_Inspect().nl(ref changed);
            
            return changed;
        }

        private int _inspectedDependecy = -1;
        
        public bool DependenciesInspect(bool showAll = false) {

            var changed = false;

          

            if (showAll)
            {

                pegi.nl();

                "Download Manager".enter_Inspect(DownloadManager, ref _inspectedDependecy, 0).changes(ref changed);

                if (_inspectedDependecy == -1)
                    "You can enable URL field in the Optional UI elements to get texture directly from web"
                        .fullWindowDocumentationClickOpen();

                pegi.nl();

                if ("Buffers".enter(ref _inspectedDependecy, 1).nl())
                {

                    RenderTextureBuffersManager.Inspect().nl(ref changed);

#if UNITY_EDITOR
                    "Disable Second Buffer Update (Debug Mode)".toggleIcon(ref disableSecondBufferUpdateDebug).nl();
#endif

                    return changed;
                }


                if ("Inspector & Debug".enter(ref _inspectedDependecy, 2).nl())
                {
                    QcUtils.InspectInspector();

                   // if ("Test Coroutine".Click())
                     //   QcAsync.TestCoroutine().StartTimedCoroutine(this, (string value) => Debug.Log("Coroutine returned {0}".F(value)));

                }

                if (_inspectedDependecy == -1)
                {

                    "Main Directional Light".edit(ref mainDirectionalLight).nl(ref changed);

#if UNITY_EDITOR
                    if ("Refresh Brush Shaders".Click().nl())
                    {
                        Data.CheckShaders(true);
                        "Shaders Refreshed".showNotificationIn3D_Views();
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

            if (showOthers || !RenderTextureBuffersManager.GotPaintingBuffers)
                (RenderTextureBuffersManager.GotPaintingBuffers ? "No buffers" : "Using HDR buffers " + ((!FrontBuffer) ? "uninitialized" : "initialized")).nl();
            
            if (!painterCamera) {
                pegi.nl();
                "no painter camera".writeWarning();
                pegi.nl();
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

                if ("Main Camera".select(60, ref cam, cams).changes(ref changed))
                    MainCamera = cam;
                
                if (icon.Refresh.Click("Try to find camera tagged as Main Camera", ref changed)) {
                    MainCamera = Camera.main;
                    if (!MainCamera)
                        "No camera is tagged as main".showNotificationIn3D_Views();
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
            
            _modulesMeta.edit_List(ref PainterSystemManagerModuleBase.modules, PainterSystemManagerModuleBase.all).changes(ref changed);

            if (!_modulesMeta.Inspecting) {

                if ("Find Modules".Click())
                    PainterSystemManagerModuleBase.RefreshPlugins();

                if ("Delete Modules".Click().nl())
                    PainterSystemManagerModuleBase.modules = null;

            }
       
            return changed;
        }
        
        #endregion

    }
}