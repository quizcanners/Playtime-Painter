using System.Collections.Generic;
using UnityEngine;
using System;
using QuizCannersUtilities;
using PlayerAndEditorGUI;

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

        public static DepthProjectorCamera GetProjectorCamera()
        {
          
                if (depthProjectorCamera)
                    return depthProjectorCamera;

                if (!DepthProjectorCamera.Instance)
                    depthProjectorCamera = UnityUtils.Instantiate<DepthProjectorCamera>();

                return depthProjectorCamera;
            
        }

        public static readonly BrushMeshGenerator BrushMeshGenerator = new BrushMeshGenerator();

        public static readonly MeshManager MeshManager = new MeshManager();

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

        public List<ImageMeta> blitJobsActive = new List<ImageMeta>();

        public bool isLinearColorSpace;

        #region Modules
      
        private ListMetaData _pluginsMeta = new ListMetaData("Modules", true, true, true, false);

        public IEnumerable<PainterSystemManagerModuleBase> Plugins
        {
            get {

                if (PainterSystemManagerModuleBase.plugins == null)
                    PainterSystemManagerModuleBase.RefreshPlugins();

                return PainterSystemManagerModuleBase.plugins;
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

            UnityUtils.RenamingLayer(l, "Playtime Painter's Layer");

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
               return brushRenderer.meshRenderer.sharedMaterial;
            }
        }

        public RenderBrush brushPrefab;
        public const float OrthographicSize = 128; 

        public RenderBrush brushRenderer;
        #endregion

        public Material defaultMaterial;

        private static Vector3 _prevPosPreview;
        private static float _previewAlpha = 1;

        #region Encode & Decode

        public override CfgEncoder Encode() => this.EncodeUnrecognized()
            .Add("mm", MeshManager)
            .Add_Abstract("pl", PainterSystemManagerModuleBase.plugins, _pluginsMeta)
            .Add("rts", RenderTextureBuffersManager.renderBuffersSize);

        public override bool Decode(string tg, string data) {
            switch (tg) {
                case "pl":
                    data.Decode_List(out PainterSystemManagerModuleBase.plugins, ref _pluginsMeta, PainterSystemManagerModuleBase.all);
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

        [NonSerialized] private ImageMeta alphaBufferDataTarget;
        [NonSerialized] private Shader alphaBufferDataShader;

        public MeshRenderer secondBufferDebug;

        public MeshRenderer alphaBufferDebug;

        public ImageMeta imgMetaUsingRendTex;
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

        public void ChangeBufferTarget(ImageMeta newTarget, MaterialMeta mat, ShaderProperty.TextureValue parameter, PlaytimePainter painter)
        {

            if (newTarget != imgMetaUsingRendTex)  {

                if (materialsUsingRenderTexture.Count > 0)
                    PlaytimePainter.SetOriginalShader();

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

 
      


        private readonly ShaderProperty.FloatValue _copyChannelTransparency = new ShaderProperty.FloatValue("_pp_CopyBlitAlpha");

        private static readonly ShaderProperty.VectorValue ChannelCopySourceMask =          new ShaderProperty.VectorValue("_ChannelSourceMask");
        public static readonly ShaderProperty.VectorValue BrushColorProperty =              new ShaderProperty.VectorValue("_brushColor");
        private static readonly ShaderProperty.VectorValue BrushMaskProperty =              new ShaderProperty.VectorValue("_brushMask");
        private static readonly ShaderProperty.VectorValue MaskDynamicsProperty =           new ShaderProperty.VectorValue("_maskDynamics");
        private static readonly ShaderProperty.VectorValue MaskOffsetProperty =             new ShaderProperty.VectorValue("_maskOffset");
        private static readonly ShaderProperty.VectorValue BrushFormProperty =              new ShaderProperty.VectorValue("_brushForm");
        private static readonly ShaderProperty.VectorValue TextureSourceParameters =        new ShaderProperty.VectorValue("_srcTextureUsage");
        private static readonly ShaderProperty.VectorValue cameraPosition_Property =        new ShaderProperty.VectorValue("_RTcamPosition");
        private static readonly ShaderProperty.VectorValue AlphaBufferConfigProperty =      new ShaderProperty.VectorValue("_pp_AlphaBufferCfg");

        private static readonly ShaderProperty.TextureValue SourceMaskProperty =            new ShaderProperty.TextureValue("_SourceMask");
        private static readonly ShaderProperty.TextureValue SourceTextureProperty =         new ShaderProperty.TextureValue("_SourceTexture");
        private static readonly ShaderProperty.TextureValue TransparentLayerUnderProperty = new ShaderProperty.TextureValue("_TransparentLayerUnderlay");
        private static readonly ShaderProperty.TextureValue AlphaPaintingBuffer =           new ShaderProperty.TextureValue("_pp_AlphaBuffer");

        public void SHADER_BRUSH_UPDATE(BrushConfig brush = null, float brushAlpha = 1, ImageMeta id = null, PlaytimePainter painter = null)
        {
            if (brush == null)
                brush = GlobalBrush;

            if (!painter)
                painter = PlaytimePainter.selectedInPlaytime;
            
            if (id == null && painter)
                id = painter.ImgMeta;
            
            brush.previewDirty = false;

            if (id == null)
                return;

            float textureWidth = id.width;
            var rendTex = id.TargetIsRenderTexture();

            var brushType = brush.GetBrushType(!rendTex);
            var blitMode = brush.GetBlitMode(!rendTex);

            var is3DBrush = brush.IsA3DBrush(painter);
            //var isDecal = rendTex && brushType.IsUsingDecals;

            var useAlphaBuffer = (brush.useAlphaBuffer && blitMode.SupportsAlphaBufferPainting && rendTex);

            BrushColorProperty.GlobalValue = brush.Color;

            BrushMaskProperty.GlobalValue = new Vector4(
                BrushExtensions.HasFlag(brush.mask, BrushMask.R) ? 1 : 0,
                BrushExtensions.HasFlag(brush.mask, BrushMask.G) ? 1 : 0,
                BrushExtensions.HasFlag(brush.mask, BrushMask.B) ? 1 : 0,
                BrushExtensions.HasFlag(brush.mask, BrushMask.A) ? 1 : 0);

            float useTransparentLayerBackground = 0;

            if (id.isATransparentLayer)
            {
                var md = painter.MatDta;
                if (md != null && md.usePreviewShader && md.material) {
                    var mt = md.material.mainTexture;
                    TransparentLayerUnderProperty.GlobalValue = mt;
                    useTransparentLayerBackground = (mt && (id != mt.GetImgDataIfExists())) ? 1 : 0;
                }
            }


            brushType.OnShaderBrushUpdate(brush);

            //if (isDecal)
              //  SHADER_DECAL_UPDATE(brush);

            if (rendTex)
                SourceMaskProperty.GlobalValue = brush.useMask ? Data.masks.TryGet(brush.selectedSourceMask) : null;

            MaskDynamicsProperty.GlobalValue = new Vector4(
                brush.maskTiling,
                rendTex ? brush.hardness * brush.hardness : 0,       // y - Hardness is 0 to do correct preview for Texture2D brush 
                ((brush.flipMaskAlpha || brush.useMask) ? 0 : 1) ,
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

            UnityUtils.SetShaderKeyword(PainterDataAndConfig.BRUSH_TEXCOORD_2, id.useTexCoord2);

            if (blitMode.SupportsTransparentLayer)
                UnityUtils.SetShaderKeyword(PainterDataAndConfig.TARGET_TRANSPARENT_LAYER, id.isATransparentLayer);

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

        public void SHADER_STROKE_SEGMENT_UPDATE(BrushConfig bc, float brushAlpha, ImageMeta id, StrokeVector stroke, PlaytimePainter pntr, out bool alphaBuffer)
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

            if (stroke.firstStroke)
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

            QcMath.IsLerpingBySpeed(ref _previewAlpha, hidePreview ? 0 : 1, 4f);

            PainterDataAndConfig.BRUSH_WORLD_POS_FROM.GlobalValue = _prevPosPreview.ToVector4(size);
            PainterDataAndConfig.BRUSH_WORLD_POS_TO.GlobalValue = st.posTo.ToVector4((st.posTo - _prevPosPreview).magnitude); //new Vector4(st.posTo.x, st.posTo.y, st.posTo.z, (st.posTo - prevPosPreview).magnitude));
            _prevPosPreview = st.posTo;
        }


        #endregion

        #region Alpha Buffer 

        public void AlphaBufferSetDirtyBeforeRender(ImageMeta id, Shader shade) {

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

            sinceLastPainterCall = 0;
            
            brushRenderer.AfterRender();
        }
        
        void SetChannelCopySourceMask(ColorChanel sourceChannel) => ChannelCopySourceMask.GlobalValue = new Vector4(
                sourceChannel == ColorChanel.R ? 1 : 0,
                sourceChannel == ColorChanel.G ? 1 : 0,
                sourceChannel == ColorChanel.B ? 1 : 0,
                sourceChannel == ColorChanel.A ? 1 : 0
        );

        public RenderTexture Render(Texture from, RenderTexture to, ColorChanel sourceChannel, ColorChanel intoChannel) {
            SetChannelCopySourceMask(sourceChannel);
            Render(from, to, Data.CopyIntoTargetChannelShader(intoChannel));
            return to;
        }

        public RenderTexture RenderDepth(Texture from, RenderTexture to, ColorChanel intoChannel) => Render(from, to, ColorChanel.R, intoChannel);

        public RenderTexture Render(Texture from, RenderTexture to, float alpha) {

            _copyChannelTransparency.GlobalValue = alpha;

            return Render(from, to, Data.bufferBlendRGB);

        }

        public RenderTexture Render(Texture from, RenderTexture to, Shader shade) => brushRenderer.CopyBuffer(from, to, shade);
        
        public RenderTexture Render(Texture from, RenderTexture to, Material mat) =>  brushRenderer.CopyBuffer(from, to, mat);
         
        public RenderTexture Render(Texture from, RenderTexture to) => Render(from, to, Data.brushBufferCopy);

        public RenderTexture Render(ImageMeta from, RenderTexture to) => Render(from.CurrentTexture(), to, Data.brushBufferCopy);

        public RenderTexture Render(Texture from, ImageMeta to) => Render(from, to.CurrentRenderTexture(), Data.brushBufferCopy);

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

        public void ApplyAllChangesTo(ImageMeta id) {

            if (id != null) {
                if (id == alphaBufferDataTarget)
                    FinalizePreviousAlphaDataTarget();
            } 

        }

        public void DiscardChanges(ImageMeta id) {

            if (id != null && id == alphaBufferDataTarget)
                DiscardAlphaBuffer();
            
        }

        public void OnBeforeBlitConfigurationChange() {
            FinalizePreviousAlphaDataTarget();
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

            EditorSceneManager.sceneOpening -= OnSceneOpening;
            EditorSceneManager.sceneOpening += OnSceneOpening;

            if (!defaultMaterial)
                defaultMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");

            if (!defaultMaterial) Debug.Log("Default Material not found.");

            isLinearColorSpace = PlayerSettings.colorSpace == ColorSpace.Linear;

            EditorApplication.update -= CombinedUpdate;
            if (!UnityUtils.ApplicationIsAboutToEnterPlayMode())
                EditorApplication.update += CombinedUpdate;


            if (!brushPrefab) {
                var go = Resources.Load("prefabs/RenderCameraBrush") as GameObject;
                if (go)
                {
                    brushPrefab = go.GetComponent<RenderBrush>();
                    if (!brushPrefab)
                        Debug.Log("Couldn't find brush prefab.");
                }
                else
                    Debug.LogError("Couldn't load brush Prefab");
            }

           

            #endif

            if (!brushRenderer)
            {
                brushRenderer = GetComponentInChildren<RenderBrush>();
                if (!brushRenderer)
                {
                    brushRenderer = Instantiate(brushPrefab.gameObject).GetComponent<RenderBrush>();
                    brushRenderer.transform.parent = transform;
                }
            }
         



            transform.position = Vector3.up * 3000;
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;

            if (!painterCamera)
            {
                painterCamera = GetComponent<Camera>();
                if (!painterCamera)
                    painterCamera = gameObject.AddComponent<Camera>();
            }

            painterCamera.orthographic = true;
            painterCamera.orthographicSize = OrthographicSize;
            painterCamera.clearFlags = CameraClearFlags.Nothing;
            painterCamera.enabled = Application.isPlaying;
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

            foreach (var p in PainterSystemManagerModuleBase.plugins)
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
                PlaytimePainter.previewHolderMaterial.shader = PlaytimePainter.previewHolderOriginalShader;

            if (materialsUsingRenderTexture.Count > 0)
                autodisabledBufferTarget = materialsUsingRenderTexture[0].painterTarget;
            EmptyBufferTarget();
            #endif

            if (PainterSystemManagerModuleBase.plugins != null)
                foreach (var p in PainterSystemManagerModuleBase.plugins)
                    p?.Disable();
            
            if (Data)
                Data.ManagedOnDisable();

            RenderTextureBuffersManager.OnDisable();
        }

        #if UNITY_EDITOR
        public void OnSceneOpening(string path, OpenSceneMode mode)
        {
            // Debug.Log("On Scene Opening");
        }

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

        public static float sinceLastPainterCall = 0;

        public void CombinedUpdate() {

            if (!this || !Data)
                return;
            
            if (PlaytimePainter.IsCurrentTool && focusedPainter)
                focusedPainter.ManagedUpdate();
            
            if (GlobalBrush.previewDirty)
                SHADER_BRUSH_UPDATE();

            PlaytimePainter uiPainter = null;

            MeshManager.CombinedUpdate();

            if (!Application.isPlaying && depthProjectorCamera)
                depthProjectorCamera.ManagedUpdate();

#if UNITY_2018_1_OR_NEWER
            foreach ( var j in blitJobsActive) 
                if (j.jobHandle.IsCompleted)
                    j.CompleteJob();
#endif

            var l = PlaytimePainter.PlaybackPainters;

            if (l.Count > 0 && !StrokeVector.pausePlayback)
            {
                if (!l.Last())
                    l.RemoveLast(1);
                else
                    l.Last().PlaybackVectors();
            }

#if UNITY_EDITOR
            if (refocusOnThis) {
                _scipFrames--;
                if (_scipFrames == 0) {
                    UnityUtils.FocusOn(refocusOnThis);
                    refocusOnThis = null;
                    _scipFrames = 3;
                }
            }
#endif

            if (!uiPainter || !uiPainter.CanPaint()) {

                var p = PlaytimePainter.currentlyPaintedObjectPainter;

                if (p && !Application.isPlaying && sinceLastPainterCall>0.016f)
                {
                    if (p.ImgMeta == null)
                        PlaytimePainter.currentlyPaintedObjectPainter = null;
                    else {
                        TexMgmtData.brushConfig.Paint(p.stroke, p);
                        p.ManagedUpdate();
                    }
                }
            }

            var needRefresh = false;
            if (PainterSystemManagerModuleBase.plugins!= null)
                foreach (var pl in PainterSystemManagerModuleBase.plugins)
                    if (pl != null)
                        pl.Update();
                    else needRefresh = true;

            if (needRefresh) {
                Debug.Log("Refreshing modules");
                PainterSystemManagerModuleBase.RefreshPlugins();
            }

            sinceLastPainterCall += Time.deltaTime;


        }


        public static void CancelAllPlaybacks()
        {
            foreach (var p in PlaytimePainter.PlaybackPainters)
                p.playbackVectors.Clear();

            PlaytimePainter.cody = new CfgDecoder(null);
        }

        #endregion

        #region Inspector
        
        ChillLogger logger = new ChillLogger("error");

        #if PEGI

        public AnimationCurve InspectAnimationCurve(string role) {
            role.edit_Property(() => tmpCurve, this);

            return tmpCurve;
        }

        public override bool Inspect()
        {

            var changed = false;

            if (Data)
                Data.Nested_Inspect().nl(ref changed);


            return changed;
        }

        private bool showBuffers;

        public bool DependenciesInspect(bool showAll = false) {

            var changed = false;

            if (showAll)
            {
                pegi.nl();

                if (!showBuffers)
                {

                    "Main Directional Light".edit(ref mainDirectionalLight).nl(ref changed);

                    #if UNITY_EDITOR
                    if ("Refresh Brush Shaders".Click(14).nl())
                    {
                        Data.CheckShaders(true);
                        "Shaders Refreshed".showNotificationIn3D_Views();
                    }
                    #endif

                    "Using layer:".editLayerMask(ref Data.playtimePainterLayer).nl(ref changed);
                }
            }

            if (showAll && "Buffers".enter(ref showBuffers).nl())  {
             
                RenderTextureBuffersManager.Inspect().nl(ref changed);


#if UNITY_EDITOR
                "Disable Second Buffer Update (Debug Mode)".toggleIcon(ref disableSecondBufferUpdateDebug).nl();
#endif






                return changed;
            }
            

#if UNITY_EDITOR
            if (!Data)
            {
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

            if (showAll || !RenderTextureBuffersManager.GotPaintingBuffers)
                (RenderTextureBuffersManager.GotPaintingBuffers ? "No buffers" : "Using HDR buffers " + ((!FrontBuffer) ? "uninitialized" : "initialized")).nl();
            
            if (!painterCamera) {
                pegi.nl();
                "no painter camera".writeWarning();
                pegi.nl();
            }
            
            if (showAll || !MainCamera) {
                pegi.nl();

                var cam = MainCamera;

                if (!cam)
                    icon.Warning.write("No Main Camera found. Playtime Painting will not be possible");

                var cams = new List<Camera>(FindObjectsOfType<Camera>());

                if (painterCamera && cams.Contains(painterCamera))
                    cams.Remove(painterCamera);

                if ("Main Camera".select(60, ref cam, cams).changes(ref changed))
                    MainCamera = cam;
                
                if (icon.Refresh.Click("Try to find camera tagged as Main Camera", ref changed)) {
                    MainCamera = Camera.main;
                    if (!MainCamera)
                        "No camera is tagged as main".showNotificationIn3D_Views();
                }

                pegi.nl();
            }

            return changed;
        }

        public bool PluginsInspect() {

            var changed = false;
            
            _pluginsMeta.edit_List(ref PainterSystemManagerModuleBase.plugins, PainterSystemManagerModuleBase.all).changes(ref changed);

            if (!_pluginsMeta.Inspecting)
            {

                if ("Find Modules".Click())
                    PainterSystemManagerModuleBase.RefreshPlugins();

                if ("Delete Modules".Click().nl())
                    PainterSystemManagerModuleBase.plugins = null;

            }
       

            return changed;
        }

        #endif
        #endregion

    }
}