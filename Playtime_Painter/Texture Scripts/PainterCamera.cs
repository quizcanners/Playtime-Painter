using System.Collections.Generic;
using UnityEngine;
using System;
using QuizCannersUtilities;
using PlayerAndEditorGUI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Playtime_Painter {

    [HelpURL(PlaytimePainter.OnlineManual)]
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class PainterCamera : PainterStuffMono {

        public static readonly BrushMeshGenerator BrushMeshGenerator = new BrushMeshGenerator();

        public static readonly MeshManager MeshManager = new MeshManager();

        public static readonly TextureDownloadManager DownloadManager = new TextureDownloadManager();
        
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

        private static bool _triedToFindCamera;

        public static PainterCamera Inst {
            get
            {
                if (_inst) return _inst;

                _inst = null;

                if (!_triedToFindCamera) {
                    _inst = FindObjectOfType<PainterCamera>();
                    if (!_inst) _triedToFindCamera = true;
                }

                if (!PainterStuff.applicationIsQuitting) {

                    if (!_inst) {
                        
                        #if UNITY_EDITOR
                            var go = Resources.Load("prefabs/" + PainterDataAndConfig.PainterCameraName) as GameObject;
                            _inst = Instantiate(go).GetComponent<PainterCamera>();
                            _inst.name = PainterDataAndConfig.PainterCameraName;
                            PainterManagerPluginBase.RefreshPlugins();
                        #endif

                        _triedToFindCamera = false;
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

        public PlaytimePainter focusedPainter;

        public List<ImageMeta> blitJobsActive = new List<ImageMeta>();

        public bool isLinearColorSpace;

        #region Plugins
      
        private ListMetaData _pluginsMeta = new ListMetaData("Plugins", true, true, true, false, icon.Link);

        public IEnumerable<PainterManagerPluginBase> Plugins
        {
            get {

                if (PainterManagerPluginBase.plugins == null)
                    PainterManagerPluginBase.RefreshPlugins();

                return PainterManagerPluginBase.plugins;
            }
        }


        #endregion

        public Camera theCamera;

        public RenderBrush brushPrefab;
        public const float OrthographicSize = 128; 

        public RenderBrush brushRenderer;

        public Material defaultMaterial;

        private static Vector3 _prevPosPreview;
        private static float _previewAlpha = 1;

        #region Encode & Decode

        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add("mm", MeshManager)
            .Add_Abstract("pl", PainterManagerPluginBase.plugins, _pluginsMeta);

        public override bool Decode(string tg, string data)
        {
            switch (tg) {
                case "pl": data.Decode_List(out PainterManagerPluginBase.plugins, ref _pluginsMeta, PainterManagerPluginBase.all); break;
                case "mm": MeshManager.Decode(data); break;
                default: return false;
            }

            return true;
        }

        #endregion

        #region Double Buffer Painting

        public const int RenderTextureSize = 2048;
        
        public RenderTexture[] bigRtPair;
        public int bigRtVersion;

        public MeshRenderer secondBufferDebug;

        #endregion

        #region Buffer Scaling
        [NonSerialized] private readonly RenderTexture[] _squareBuffers = new RenderTexture[10];

        public RenderTexture GetSquareBuffer(int width)
        {
            int no = 9;
            switch (width)
            {
                case 8: no = 0; break;
                case 16: no = 1; break;
                case 32: no = 2; break;
                case 64: no = 3; break;
                case 128: no = 4; break;
                case 256: no = 5; break;
                case 512: no = 6; break;
                case 1024: no = 7; break;
                case 2048: no = 8; break;
                case 4096: no = 9; break;
                default: Debug.Log(width + " is not in range "); break;
            }

            if (!_squareBuffers[no])
                _squareBuffers[no] = new RenderTexture(width, width, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);

            return _squareBuffers[no];
        }

        [NonSerialized]
        List<RenderTexture> nonSquareBuffers = new List<RenderTexture>();
        public RenderTexture GetNonSquareBuffer(int width, int height)
        {
            foreach (RenderTexture r in nonSquareBuffers)
                if ((r.width == width) && (r.height == height)) return r;

            RenderTexture rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            nonSquareBuffers.Add(rt);
            return rt;
        }

        public RenderTexture GetDownscaledBigRt(int width, int height) => Downscale_ToBuffer(bigRtPair[0], width, height);

        public RenderTexture Downscale_ToBuffer(Texture tex, int width, int height, Material mat) => Downscale_ToBuffer(tex, width, height, mat, null);

        public RenderTexture Downscale_ToBuffer(Texture tex, int width, int height, Shader shade) => Downscale_ToBuffer(tex, width, height, null, shade);

        public RenderTexture Downscale_ToBuffer(Texture tex, int width, int height) => Downscale_ToBuffer(tex, width, height, null, Data.pixPerfectCopy);// brushRendy_bufferCopy);

        public RenderTexture Downscale_ToBuffer(Texture tex, int width, int height, Material material, Shader shader)
        {

            if (!tex)
                return null;

            if (!shader) shader = Data.brushBufferCopy;

            bool square = (width == height);
            if ((!square) || (!Mathf.IsPowerOfTwo(width)))
                return Render(tex, GetNonSquareBuffer(width, height), shader);
            else
            {
                int tmpWidth = Mathf.Max(tex.width / 2, width);

                RenderTexture from = material ? Render(tex, GetSquareBuffer(tmpWidth), material) : Render(tex, GetSquareBuffer(tmpWidth), shader);

                while (tmpWidth > width)
                {
                    tmpWidth /= 2;
                    from = material ? Render(from, GetSquareBuffer(tmpWidth), material) : Render(from, GetSquareBuffer(tmpWidth), shader);
                }

                return from;
            }
        }
        #endregion

        #region Buffers MGMT
        public ImageMeta imgMetaUsingRendTex;
        public List<MaterialMeta> materialsUsingTendTex = new List<MaterialMeta>();
        public PlaytimePainter autodisabledBufferTarget;

        public void EmptyBufferTarget()
        {

            if (imgMetaUsingRendTex == null)
                return;

            if (imgMetaUsingRendTex.texture2D) //&& (Application.isPlaying == false))
                imgMetaUsingRendTex.RenderTexture_To_Texture2D();

            imgMetaUsingRendTex.destination = TexTarget.Texture2D;

            foreach (var m in materialsUsingTendTex)
                m.SetTextureOnLastTarget(imgMetaUsingRendTex);

            materialsUsingTendTex.Clear();
            imgMetaUsingRendTex = null;
        }

        public void ChangeBufferTarget(ImageMeta newTarget, MaterialMeta mat, ShaderProperty.TextureValue parameter, PlaytimePainter painter)
        {

            if (newTarget != imgMetaUsingRendTex)
            {

                if (materialsUsingTendTex.Count > 0)
                    PlaytimePainter.SetOriginalShader();

                if (imgMetaUsingRendTex != null)
                {
                    if (imgMetaUsingRendTex.texture2D)
                        imgMetaUsingRendTex.RenderTexture_To_Texture2D();

                    imgMetaUsingRendTex.destination = TexTarget.Texture2D;

                    foreach (var m in materialsUsingTendTex)
                        m.SetTextureOnLastTarget(imgMetaUsingRendTex);
                }
                materialsUsingTendTex.Clear();
                autodisabledBufferTarget = null;
                imgMetaUsingRendTex = newTarget;
            }

            mat.bufferParameterTarget = parameter;
            mat.painterTarget = painter;
            materialsUsingTendTex.Add(mat);
        }

        public void UpdateBuffersState()
        {

            var cfg = TexMGMTdata;

            if (!cfg)
                return;

            theCamera.cullingMask = 1 << cfg.myLayer;

            if (!GotBuffers)
            {
                bigRtPair = new RenderTexture[2];
                bigRtPair[0] = new RenderTexture(RenderTextureSize, RenderTextureSize, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default);
                bigRtPair[1] = new RenderTexture(RenderTextureSize, RenderTextureSize, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default);
                bigRtPair[0].wrapMode = TextureWrapMode.Repeat;
                bigRtPair[1].wrapMode = TextureWrapMode.Repeat;
                bigRtPair[0].name = "Painter Buffer 0 _ " + RenderTextureSize;
                bigRtPair[1].name = "Painter Buffer 1 _ " + RenderTextureSize;

            }

            if (secondBufferDebug)
            {
                secondBufferDebug.sharedMaterial.mainTexture = bigRtPair[1];
                var cmp = secondBufferDebug.GetComponent<PlaytimePainter>();
                if (cmp)
                    cmp.DestroyWhatever_Component();
            }

            var cam = Camera.main;
            
            if (cam)
                cam.cullingMask &= ~(1 << Data.myLayer);
        }

        public static bool GotBuffers => Inst && Inst.bigRtPair != null && _inst.bigRtPair.Length > 0 && _inst.bigRtPair[0];
        #endregion

        #region Brush Shader MGMT

        public static void Shader_PerFrame_Update(StrokeVector st, bool hidePreview, float size)
        {

            if (hidePreview && Math.Abs(_previewAlpha) < float.Epsilon)
                return;

            MyMath.isLerping_bySpeed(ref _previewAlpha, hidePreview ? 0 : 1, 0.1f);

            PainterDataAndConfig.BRUSH_POINTED_UV.GlobalValue = st.uvTo.ToVector4(0, _previewAlpha);
            PainterDataAndConfig.BRUSH_WORLD_POS_FROM.GlobalValue = _prevPosPreview.ToVector4(size);
            PainterDataAndConfig.BRUSH_WORLD_POS_TO.GlobalValue = st.posTo.ToVector4((st.posTo - _prevPosPreview).magnitude); //new Vector4(st.posTo.x, st.posTo.y, st.posTo.z, (st.posTo - prevPosPreview).magnitude));
            _prevPosPreview = st.posTo;
        }

        ShaderProperty.TextureValue decal_HeightProperty =      new ShaderProperty.TextureValue("_VolDecalHeight");
        ShaderProperty.TextureValue decal_OverlayProperty =     new ShaderProperty.TextureValue("_VolDecalOverlay");
        ShaderProperty.VectorValue decal_ParametersProperty =   new ShaderProperty.VectorValue("_DecalParameters");

        public void Shader_UpdateDecal(BrushConfig brush)
        {

            VolumetricDecal vd = Data.decals.TryGet(brush.selectedDecal);

            if (vd != null)
            {
                decal_HeightProperty.GlobalValue = vd.heightMap;
                decal_OverlayProperty.GlobalValue = vd.overlay;
                decal_ParametersProperty.GlobalValue = new Vector4(brush.decalAngle * Mathf.Deg2Rad, (vd.type == VolumetricDecalType.Add) ? 1 : -1,
                        Mathf.Clamp01(brush.speed / 10f), 0);
            }

        }

        ShaderProperty.VectorValue brushColor_Property =        new ShaderProperty.VectorValue("_brushColor");
        ShaderProperty.VectorValue brushMask_Property =         new ShaderProperty.VectorValue("_brushMask");
        ShaderProperty.TextureValue sourceMask_Property =       new ShaderProperty.TextureValue("_SourceMask");
        ShaderProperty.VectorValue maskDynamics_Property =      new ShaderProperty.VectorValue("_maskDynamics");
        ShaderProperty.VectorValue maskOffset_Property =        new ShaderProperty.VectorValue("_maskOffset");
        ShaderProperty.VectorValue brushForm_Property =         new ShaderProperty.VectorValue("_brushForm");
        ShaderProperty.TextureValue sourceTexture_Property =    new ShaderProperty.TextureValue("_SourceTexture");

        public void Shader_UpdateBrushConfig(BrushConfig brush = null, float brushAlpha = 1, ImageMeta id = null, PlaytimePainter painter = null)
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

            var brushType = brush.Type(!rendTex);

            var is3DBrush = brush.IsA3Dbrush(painter);
            var isDecal = rendTex && brushType.IsUsingDecals;

            brushColor_Property.GlobalValue = brush.Color;

            brushMask_Property.GlobalValue = new Vector4(
                BrushExtensions.HasFlag(brush.mask, BrushMask.R) ? 1 : 0,
                BrushExtensions.HasFlag(brush.mask, BrushMask.G) ? 1 : 0,
                BrushExtensions.HasFlag(brush.mask, BrushMask.B) ? 1 : 0,
                BrushExtensions.HasFlag(brush.mask, BrushMask.A) ? 1 : 0);

            if (isDecal) Shader_UpdateDecal(brush);

            if (brush.useMask && rendTex)
                sourceMask_Property.GlobalValue = Data.masks.TryGet(brush.selectedSourceMask);

            maskDynamics_Property.GlobalValue = new Vector4(
                brush.maskTiling,
                rendTex ? brush.Hardness : 0,       // y - Hardness is 0 to do correct preview for Texture2D brush 
                (brush.flipMaskAlpha ? 0 : 1)
                , 0);

            maskOffset_Property.GlobalValue = brush.maskOffset.ToVector4();
                
            brushForm_Property.GlobalValue = new Vector4(
                brushAlpha, // x - transparency
                brush.Size(is3DBrush), // y - scale for sphere
                brush.Size(is3DBrush) / textureWidth, // z - scale for uv space
                brush.blurAmount); // w - blur amount

            brushType.SetKeyword(id.useTexcoord2);

            UnityHelperFunctions.SetShaderKeyword(PainterDataAndConfig.BRUSH_TEXCOORD_2, id.useTexcoord2);

            if (brush.BlitMode.SupportsTransparentLayer)
                UnityHelperFunctions.SetShaderKeyword(PainterDataAndConfig.TARGET_TRANSPARENT_LAYER, id.isATransparentLayer);

            brush.BlitMode.SetKeyword(id).SetGlobalShaderParameters();

            if (rendTex && brush.BlitMode.UsingSourceTexture)
                sourceTexture_Property.GlobalValue = Data.sourceTextures.TryGet(brush.selectedSourceTexture);

        }

        public void Shader_UpdateStrokeSegment(BrushConfig bc, float brushAlpha, ImageMeta id, StrokeVector stroke, PlaytimePainter pntr)
        {
            if (bigRtPair == null) UpdateBuffersState();

            bool isDoubleBuffer = !id.renderTexture;

            bool useSingle = !isDoubleBuffer || bc.IsSingleBufferBrush();

            if (!useSingle && !secondBufferUpdated)
                UpdateBufferTwo();

            if (stroke.firstStroke)
                Shader_UpdateBrushConfig(bc, brushAlpha, id, pntr);

            theCamera.targetTexture = id.CurrentRenderTexture();

            if (isDoubleBuffer)
                PainterDataAndConfig.DESTINATION_BUFFER.GlobalValue = bigRtPair[1];

            Shader shd = null;
            if (pntr)
                foreach (var pl in PainterManagerPluginBase.brushPlugins) {
                    Shader bs = useSingle ? pl.GetBrushShaderSingleBuffer(pntr) : pl.GetBrushShaderDoubleBuffer(pntr);
                    if (bs) {
                        shd = bs;
                        break;
                    }
                }

            if (!shd) shd = useSingle ? bc.BlitMode.ShaderForSingleBuffer : bc.BlitMode.ShaderForDoubleBuffer;

            brushRenderer.Set(shd);

        }

        #endregion

        #region Blit Textures
        public void Blit(Texture tex, ImageMeta id)
        {
            if (!tex || id == null)
                return;
            brushRenderer.Set(Data.pixPerfectCopy);
            Graphics.Blit(tex, id.CurrentRenderTexture(), brushRenderer.meshRenderer.sharedMaterial);

            AfterRenderBlit(id.CurrentRenderTexture());

        }

        public void Blit(Texture from, RenderTexture to) =>  Blit(from, to, Data.pixPerfectCopy);
        
        public void Blit(Texture from, RenderTexture to, Shader blitShader)
        {

            if (!from)
                return;
            brushRenderer.Set(blitShader);
            Graphics.Blit(from, to, brushRenderer.meshRenderer.sharedMaterial);
            AfterRenderBlit(to);
        }
        #endregion

        #region Render

        ShaderProperty.VectorValue cameraPosition_Property = new ShaderProperty.VectorValue("_RTcamPosition");

        public void Render()
        {
            transform.rotation = Quaternion.identity;
            cameraPosition_Property.GlobalValue = transform.position.ToVector4();

            brushRenderer.gameObject.SetActive(true);
            theCamera.Render();
            brushRenderer.gameObject.SetActive(false);

            secondBufferUpdated = false;

            if (brushRenderer.deformedBounds)
                brushRenderer.RestoreBounds();

        }

        public RenderTexture Render(Texture from, RenderTexture to, Shader shade)
        {
            brushRenderer.CopyBuffer(from, to, shade);
            return to;
        }

        public RenderTexture Render(Texture from, RenderTexture to, Material mat)
        {
            brushRenderer.CopyBuffer(from, to, mat);
            return to;
        }

        public RenderTexture Render(Texture from, RenderTexture to) => Render(from, to, Data.brushBufferCopy);

        public RenderTexture Render(ImageMeta from, RenderTexture to) => Render(from.CurrentTexture(), to, Data.brushBufferCopy);

        public RenderTexture Render(Texture from, ImageMeta to) => Render(from, to.CurrentRenderTexture(), Data.brushBufferCopy);

        public void Render(Color col, RenderTexture to)
        {
            theCamera.targetTexture = to;
            brushRenderer.PrepareColorPaint(col);
            Render();
            AfterRenderBlit(to);
        }

        void AfterRenderBlit(Texture target) {
            if (bigRtPair.Length > 0 && bigRtPair[0] && bigRtPair[0] == target)
                secondBufferUpdated = false;
        }

        public void UpdateBufferTwo()
        {
            brushRenderer.Set(Data.pixPerfectCopy);
            Graphics.Blit(bigRtPair[0], bigRtPair[1]);
            secondBufferUpdated = true;
            bigRtVersion++;
        }

        public bool secondBufferUpdated;
        public void UpdateBufferSegment()
        {
            if (!Data.disableSecondBufferUpdateDebug)
            {
                brushRenderer.Set(bigRtPair[0]);
                theCamera.targetTexture = bigRtPair[1];
                brushRenderer.Set(Data.brushBufferCopy);
                Render();
                secondBufferUpdated = true;
                bigRtVersion++;
            }
        }
        #endregion

        #region Component MGMT
        private void OnEnable() {

            PainterStuff.applicationIsQuitting = false;

            Inst = this;

            if (!Data)
                dataHolder = Resources.Load("Painter_Data") as PainterDataAndConfig;

            MeshManager.OnEnable();

            if (!theCamera)
                theCamera = GetComponent<Camera>();

            if (Data && theCamera)
                theCamera.cullingMask = 1 << Data.myLayer;
#if BUILD_WITH_PAINTER
            if (!PainterDataAndConfig.toolEnabled && !Application.isEditor)
                    PainterDataAndConfig.toolEnabled = true;
#endif
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
            if (!UnityHelperFunctions.ApplicationIsAboutToEnterPlayMode())
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

            if (Data)
                UnityHelperFunctions.RenamingLayer(Data.myLayer, "Painter Layer");

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
            if (Data)
                brushRenderer.gameObject.layer = Data.myLayer;

#if BUILD_WITH_PAINTER || UNITY_EDITOR


            transform.position = Vector3.up * 3000;
            if (!theCamera)
            {
                theCamera = GetComponent<Camera>();
                if (!theCamera)
                    theCamera = gameObject.AddComponent<Camera>();
            }

            theCamera.orthographic = true;
            theCamera.orthographicSize = OrthographicSize;
            theCamera.clearFlags = CameraClearFlags.Nothing;
            theCamera.enabled = Application.isPlaying;

#if UNITY_EDITOR
            EditorApplication.update -= CombinedUpdate;
            if (EditorApplication.isPlayingOrWillChangePlaymode == false)
                EditorApplication.update += CombinedUpdate;
#endif

            UpdateBuffersState();

#endif

            autodisabledBufferTarget = null;

            PainterManagerPluginBase.RefreshPlugins();

            foreach (var p in PainterManagerPluginBase.plugins)
                if (p != null) p.Enable();

            Data.ManagedOnEnable();

        }

        private void OnDisable() {
            PainterStuff.applicationIsQuitting = true;
            
            DownloadManager.Dispose();

            _triedToFindCamera = false ;
            
            BeforeClosing();

            if (PainterManagerPluginBase.plugins!= null)
                foreach (var p in PainterManagerPluginBase.plugins)
                    if (p != null) p.Disable();

            if (Data)
                Data.ManagedOnDisable();

        }
        
        void BeforeClosing()
        {
            #if UNITY_EDITOR
            if (PlaytimePainter.previewHolderMaterial)
                PlaytimePainter.previewHolderMaterial.shader = PlaytimePainter.previewHolderOriginalShader;

            if (materialsUsingTendTex.Count > 0)
                autodisabledBufferTarget = materialsUsingTendTex[0].painterTarget;
            EmptyBufferTarget();
            #endif

        }
#if UNITY_EDITOR
        public void OnSceneOpening(string path, OpenSceneMode mode)
        {
            // Debug.Log("On Scene Opening");
        }

        public void BeforeSceneSaved(UnityEngine.SceneManagement.Scene scene, string path)
        {
            //public delegate void SceneSavingCallback(Scene scene, string path);


            BeforeClosing();
            // Debug.Log("Before Scene saved");

        }
        #endif

#if UNITY_EDITOR || BUILD_WITH_PAINTER

        public void Update() {
            if (Application.isPlaying)
                CombinedUpdate();
        }

        public static GameObject refocusOnThis;
        #if UNITY_EDITOR
        private static int _scipframes = 3;
        #endif
      

        public void CombinedUpdate() {

            if (!Data)
                return;

            if (GlobalBrush.previewDirty)
                Shader_UpdateBrushConfig();

            PlaytimePainter uiPainter = null;

           // if (Application.isPlaying)
             //   uiPainter = PlaytimePainter.RaycastUI();

            MeshManager.EditingUpdate();

#if UNITY_2018_1_OR_NEWER
            foreach( var j in blitJobsActive) 
                if (j.jobHandle.IsCompleted)
                    j.CompleteJob();
#endif

            Data.RemoteUpdate();

            List<PlaytimePainter> l = PlaytimePainter.PlaybackPainters;

            if (l.Count > 0 && !StrokeVector.pausePlayback)
            {
                if (!l.Last())
                    l.RemoveLast(1);
                else
                    l.Last().PlaybackVectors();
            }

#if UNITY_EDITOR
            if (refocusOnThis) {
                _scipframes--;
                if (_scipframes == 0) {
                    UnityHelperFunctions.FocusOn(refocusOnThis);
                    refocusOnThis = null;
                    _scipframes = 3;
                }
            }
#endif

            if (Application.isPlaying && Data && Data.disableNonMeshColliderInPlayMode && Camera.main) {
                RaycastHit hit;
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
                {
                    var c = hit.collider;
                    if (c.GetType() != typeof(MeshCollider) && PlaytimePainter.CanEditWithTag(c.tag)) c.enabled = false;
                }
            }

            if (!uiPainter || !uiPainter.CanPaint())
            {

                var p = PlaytimePainter.currentlyPaintedObjectPainter;

                if (p && !Application.isPlaying)
                {
                    if (p.ImgMeta == null)
                        PlaytimePainter.currentlyPaintedObjectPainter = null;
                    else
                    {
                        TexMGMTdata.brushConfig.Paint(p.stroke, p);
                        p.Update();
                    }
                }
            }

            var needRefresh = false;
            if (PainterManagerPluginBase.plugins!= null)
                foreach (var pl in PainterManagerPluginBase.plugins)
                    if (pl != null)
                        pl.Update();
                    else needRefresh = true;

            if (needRefresh)
            {
                Debug.Log("Refreshing plugins");
                PainterManagerPluginBase.RefreshPlugins();
            }

        }

        #endif

        public static void CancelAllPlaybacks()
        {
            foreach (var p in PlaytimePainter.PlaybackPainters)
                p.playbackVectors.Clear();

            PlaytimePainter.cody = new StdDecoder(null);
        }

        #endregion

        #region Inspector
        #if PEGI

        public override bool Inspect() {

            "Active Jobs: {0}".F(blitJobsActive.Count).nl();

            #if UNITY_EDITOR
            if (!Data) {
                "No data Holder detected".edit(ref dataHolder);
                if ("Create".Click().nl())
                {
                    AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<PainterDataAndConfig>(), "Assets/Tools/Playtime_Painter/Resources/Painter_Data.asset");
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }
            #endif

            pegi.nl();

            (((bigRtPair == null) || (bigRtPair.Length == 0)) ? "No buffers" : "Using HDR buffers " + ((!bigRtPair[0]) ? "uninitialized" : "inited")).nl();

            if (!theCamera) {
                "no camera".nl();
                return false;
            }
            
            if (Data)
                Data.Nested_Inspect().nl();
            
            return false;
        }

        public bool PluginsInspect() {

            bool changed = false;

            if (!PainterStuff.IsNowPlaytimeAndDisabled)
            {

                changed |= _pluginsMeta.edit_List(ref PainterManagerPluginBase.plugins, PainterManagerPluginBase.all);

                if (!_pluginsMeta.Inspecting)
                {

                    if ("Find Plugins".Click())
                        PainterManagerPluginBase.RefreshPlugins();

                    if ("Delete Plugins".Click().nl())
                        PainterManagerPluginBase.plugins = null;

                }
            }
            else _pluginsMeta.Inspecting = false;

            return changed;
        }

        #endif
        #endregion

    }
}