using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using QuizCannersUtilities;
using PlayerAndEditorGUI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Playtime_Painter {

    [HelpURL(PlaytimePainter.WWW_Manual)]
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class PainterCamera : PainterStuffMono, IPEGI, IKeepUnrecognizedSTD {

        public static BrushMeshGenerator brushMeshGenerator = new BrushMeshGenerator();

        public static MeshManager meshManager = new MeshManager();

        public static TextureDownloadManager downloadManager = new TextureDownloadManager();
        
        #region Painter Data
        [SerializeField] PainterDataAndConfig dataHolder;

        [NonSerialized] public bool triedToFindPainterData = false;

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
        static PainterCamera _inst;

        static bool triedToFindCamera = false;

        public static PainterCamera Inst {
            get {
                if (!_inst) {

                    _inst = null;

                    if (!triedToFindCamera) {
                        _inst = FindObjectOfType<PainterCamera>();
                        if (!_inst) triedToFindCamera = true;
                    }

                    if (!PainterStuff.applicationIsQuitting) {

                        if (!_inst) {
                            
                            #if UNITY_EDITOR
                                GameObject go = Resources.Load("prefabs/" + PainterDataAndConfig.PainterCameraName) as GameObject;
                                _inst = Instantiate(go).GetComponent<PainterCamera>();
                                _inst.name = PainterDataAndConfig.PainterCameraName;
                                _inst.RefreshPlugins();
                            #endif

                            triedToFindCamera = false;
                        }
                    }
         

                    _inst?.gameObject.SetActive(true);
                }
                return _inst;
            }

            set
            {
                _inst = value;

            }
        }
        #endregion

        public PlaytimePainter focusedPainter;

        public List<ImageData> blitJobsActive = new List<ImageData>();

        public bool isLinearColorSpace;

        #region Plugins
        private List<PainterManagerPluginBase> _plugins;
        List_Data plauginsMeta = new List_Data("Plugins", true, true, true, false, icon.Link);

        public List<PainterManagerPluginBase> Plugins
        {
            get {

                if (_plugins == null)
                    RefreshPlugins();

                return _plugins;
            }
        }

        public void RefreshPlugins()
        {

            if (_plugins == null)
                _plugins = new List<PainterManagerPluginBase>();
            else
                for (int i = 0; i < _plugins.Count; i++)
                    if (_plugins[i] == null) { _plugins.RemoveAt(i); i--; }

            plauginsMeta.inspected = -1;

            foreach (Type t in PainterManagerPluginBase.all) {
                bool contains = false;

                foreach (var p in _plugins)
                    if (p.GetType() == t) { contains = true; break; }

                if (!contains)
                    _plugins.Add((PainterManagerPluginBase)Activator.CreateInstance(t));

            }
        }
        #endregion

        public Camera theCamera;

        public RenderBrush brushPrefab;
        public const float orthoSize = 128; 

        public RenderBrush brushRendy = null;

        public Material defaultMaterial;

        static Vector3 prevPosPreview;
        static float previewAlpha = 1;

        #region Encode & Decode

        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add("mm", meshManager)
            .Add_Abstract("pl", _plugins, plauginsMeta);

        public override bool Decode(string tag, string data)
        {
            switch (tag) {
                case "pl": data.Decode_List(out _plugins, ref plauginsMeta, PainterManagerPluginBase.all); break;
                case "mm": meshManager.Decode(data); break;
                default: return false;
            }

            return true;
        }

        #endregion

        #region Double Buffer Painting

        public const int renderTextureSize = 2048;
        
        public RenderTexture[] BigRT_pair;
        public int bigRTversion = 0;

        public MeshRenderer secondBufferDebug;

        #endregion

        #region Buffer Scaling
        [NonSerialized] readonly RenderTexture[] squareBuffers = new RenderTexture[10];

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

            if (!squareBuffers[no])
                squareBuffers[no] = new RenderTexture(width, width, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);

            return squareBuffers[no];
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

        public RenderTexture GetDownscaledBigRT(int width, int height) => Downscale_ToBuffer(BigRT_pair[0], width, height);

        public RenderTexture Downscale_ToBuffer(Texture tex, int width, int height, Material mat) => Downscale_ToBuffer(tex, width, height, mat, null);

        public RenderTexture Downscale_ToBuffer(Texture tex, int width, int height, Shader shade) => Downscale_ToBuffer(tex, width, height, null, shade);

        public RenderTexture Downscale_ToBuffer(Texture tex, int width, int height) => Downscale_ToBuffer(tex, width, height, null, Data.pixPerfectCopy);// brushRendy_bufferCopy);

        public RenderTexture Downscale_ToBuffer(Texture tex, int width, int height, Material material, Shader shader)
        {

            if (!tex)
                return null;

            if (!shader) shader = Data.brushRendy_bufferCopy;

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
        public ImageData imgDataUsingRendTex;
        public List<MaterialData> materialsUsingTendTex = new List<MaterialData>();
        public PlaytimePainter autodisabledBufferTarget;

        public void EmptyBufferTarget()
        {

            if (imgDataUsingRendTex == null)
                return;

            if (imgDataUsingRendTex.texture2D) //&& (Application.isPlaying == false))
                imgDataUsingRendTex.RenderTexture_To_Texture2D();

            imgDataUsingRendTex.destination = TexTarget.Texture2D;

            foreach (var m in materialsUsingTendTex)
                m.SetTextureOnLastTarget(imgDataUsingRendTex);

            materialsUsingTendTex.Clear();
            imgDataUsingRendTex = null;
        }

        public void ChangeBufferTarget(ImageData newTarget, MaterialData mat, string parameter, PlaytimePainter painter)
        {

            if (newTarget != imgDataUsingRendTex)
            {

                if (materialsUsingTendTex.Count > 0)
                    PlaytimePainter.SetOriginalShader();

                if (imgDataUsingRendTex != null)
                {
                    if (imgDataUsingRendTex.texture2D)
                        imgDataUsingRendTex.RenderTexture_To_Texture2D();

                    imgDataUsingRendTex.destination = TexTarget.Texture2D;

                    foreach (var m in materialsUsingTendTex)
                        m.SetTextureOnLastTarget(imgDataUsingRendTex);
                }
                materialsUsingTendTex.Clear();
                autodisabledBufferTarget = null;
                imgDataUsingRendTex = newTarget;
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
                BigRT_pair = new RenderTexture[2];
                BigRT_pair[0] = new RenderTexture(renderTextureSize, renderTextureSize, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default);
                BigRT_pair[1] = new RenderTexture(renderTextureSize, renderTextureSize, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default);
                BigRT_pair[0].wrapMode = TextureWrapMode.Repeat;
                BigRT_pair[1].wrapMode = TextureWrapMode.Repeat;
                BigRT_pair[0].name = "Painter Buffer 0 _ " + renderTextureSize;
                BigRT_pair[1].name = "Painter Buffer 1 _ " + renderTextureSize;

            }

            if (secondBufferDebug)
            {
                secondBufferDebug.sharedMaterial.mainTexture = BigRT_pair[1];
                var cmp = secondBufferDebug.GetComponent<PlaytimePainter>();
                if (cmp)
                    cmp.DestroyWhatever_Component();
            }

            if (Camera.main)
                Camera.main.cullingMask &= ~(1 << Data.myLayer);
        }

        public static bool GotBuffers => Inst && Inst.BigRT_pair != null && _inst.BigRT_pair.Length > 0 && _inst.BigRT_pair[0];
        #endregion

        #region Brush Shader MGMT

        public static void Shader_PerFrame_Update(StrokeVector st, bool hidePreview, float size)
        {

            if (hidePreview && previewAlpha == 0)
                return;

            MyMath.isLerping_bySpeed(ref previewAlpha, hidePreview ? 0 : 1, 0.1f);

            //previewAlpha = Mathf.Lerp(previewAlpha, hidePreview ? 0 : 1, 0.1f);

            Shader.SetGlobalVector(PainterDataAndConfig.BRUSH_POINTED_UV, st.uvTo.ToVector4(0, previewAlpha));
            Shader.SetGlobalVector(PainterDataAndConfig.BRUSH_WORLD_POS_FROM, prevPosPreview.ToVector4(size));
            Shader.SetGlobalVector(PainterDataAndConfig.BRUSH_WORLD_POS_TO, st.posTo.ToVector4((st.posTo - prevPosPreview).magnitude)); //new Vector4(st.posTo.x, st.posTo.y, st.posTo.z, (st.posTo - prevPosPreview).magnitude));
            prevPosPreview = st.posTo;
        }

        public void Shader_UpdateDecal(BrushConfig brush)
        {

            VolumetricDecal vd = Data.decals.TryGet(brush.selectedDecal);

            if (vd != null)
            {
                Shader.SetGlobalTexture("_VolDecalHeight", vd.heightMap);
                Shader.SetGlobalTexture("_VolDecalOverlay", vd.overlay);
                Shader.SetGlobalVector("_DecalParameters", new Vector4(brush.decalAngle * Mathf.Deg2Rad, (vd.type == VolumetricDecalType.Add) ? 1 : -1,
                        Mathf.Clamp01(brush.speed / 10f), 0));
            }

        }

        public void Shader_UpdateBrush(BrushConfig brush, float brushAlpha, ImageData id, PlaytimePainter pntr)
        {
            float textureWidth = id.width;
            bool RendTex = id.TargetIsRenderTexture();

            var brushType = brush.Type(!RendTex);

            bool is3Dbrush = brush.IsA3Dbrush(pntr);
            bool isDecal = (RendTex) && (brushType.IsUsingDecals);

            Color c = brush.colorLinear.ToGamma();

            Shader.SetGlobalVector("_brushColor", c);

            Shader.SetGlobalVector("_brushMask", new Vector4(
                brush.mask.GetFlag(BrushMask.R) ? 1 : 0,
                brush.mask.GetFlag(BrushMask.G) ? 1 : 0,
                brush.mask.GetFlag(BrushMask.B) ? 1 : 0,
                brush.mask.GetFlag(BrushMask.A) ? 1 : 0));

            if (isDecal) Shader_UpdateDecal(brush);

            if (brush.useMask && RendTex)
                Shader.SetGlobalTexture("_SourceMask", Data.masks.TryGet(brush.selectedSourceMask));

            Shader.SetGlobalVector("_maskDynamics", new Vector4(
                brush.maskTiling,
                RendTex ? brush.Hardness : 0,       // y - Hardness is 0 to do correct preview for Texture2D brush 
                (brush.flipMaskAlpha ? 0 : 1)
                , 0));

            Shader.SetGlobalVector("_maskOffset", brush.maskOffset.ToVector4());/*new Vector4(
                
            brush.maskOffset.x,
                brush.maskOffset.y,
                0,
                0));*/

            Shader.SetGlobalVector("_brushForm", new Vector4(
                brushAlpha, // x - transparency
                brush.Size(is3Dbrush), // y - scale for sphere
                brush.Size(is3Dbrush) / textureWidth, // z - scale for uv space
                brush.blurAmount)); // w - blur amount

            brushType.SetKeyword(id.useTexcoord2);

            UnityHelperFunctions.SetShaderKeyword(PainterDataAndConfig.BRUSH_TEXCOORD_2, id.useTexcoord2);
           // if (id.useTexcoord2) Shader.EnableKeyword(PainterDataAndConfig.BRUSH_TEXCOORD_2);
           // else Shader.DisableKeyword(PainterDataAndConfig.BRUSH_TEXCOORD_2);

            if (brush.BlitMode.supportsTransparentLayer)
                UnityHelperFunctions.SetShaderKeyword(PainterDataAndConfig.TARGET_TRANSPARENT_LAYER, id.isATransparentLayer);
                //if (id.isATransparentLayer) Shader.EnableKeyword(PainterDataAndConfig.TARGET_TRANSPARENT_LAYER);
                //else Shader.DisableKeyword(PainterDataAndConfig.TARGET_TRANSPARENT_LAYER);
            

            brush.BlitMode.SetKeyword(id).SetGlobalShaderParameters();

            if (RendTex && brush.BlitMode.UsingSourceTexture)
                Shader.SetGlobalTexture("_SourceTexture", Data.sourceTextures.TryGet(brush.selectedSourceTexture));

        }

        public void Shader_UpdateStrokeSegment(BrushConfig bc, float brushAlpha, ImageData id, StrokeVector stroke, PlaytimePainter pntr)
        {
            if (BigRT_pair == null) UpdateBuffersState();

            bool isDoubleBuffer = !id.renderTexture;

            bool useSingle = !isDoubleBuffer || bc.IsSingleBufferBrush();

            if (!useSingle && !secondBufferUpdated)
                UpdateBufferTwo();

            if (stroke.firstStroke)
                Shader_UpdateBrush(bc, brushAlpha, id, pntr);

            theCamera.targetTexture = id.CurrentRenderTexture();

            if (isDoubleBuffer)
                Shader.SetGlobalTexture(PainterDataAndConfig.DESTINATION_BUFFER, BigRT_pair[1]);

            Shader shd = null;
            if (pntr)
                foreach (var pl in Plugins) {
                    Shader bs = useSingle ? pl.GetBrushShaderSingleBuffer(pntr) : pl.GetBrushShaderDoubleBuffer(pntr);
                    if (bs) {
                        shd = bs;
                        break;
                    }
                }

            if (!shd) shd = useSingle ? bc.BlitMode.ShaderForSingleBuffer : bc.BlitMode.ShaderForDoubleBuffer;

            brushRendy.Set(shd);

        }

        #endregion

        #region Blit
        public void Blit(Texture tex, ImageData id)
        {
            if (!tex || id == null)
                return;
            brushRendy.Set(Data.pixPerfectCopy);
            Graphics.Blit(tex, id.CurrentRenderTexture(), brushRendy.meshRendy.sharedMaterial);

            AfterRenderBlit(id.CurrentRenderTexture());

        }

        public void Blit(Texture from, RenderTexture to) =>  Blit(from, to, Data.pixPerfectCopy);
        
        public void Blit(Texture from, RenderTexture to, Shader blitShader)
        {

            if (!from)
                return;
            brushRendy.Set(blitShader);
            Graphics.Blit(from, to, brushRendy.meshRendy.sharedMaterial);
            AfterRenderBlit(to);
        }
        #endregion

        #region Render
        public void Render()
        {
            transform.rotation = Quaternion.identity;
            Shader.SetGlobalVector("_RTcamPosition", transform.position);

            brushRendy.gameObject.SetActive(true);
            theCamera.Render();
            brushRendy.gameObject.SetActive(false);

            secondBufferUpdated = false;

            if (brushRendy.deformedBounds)
                brushRendy.RestoreBounds();

        }

        public RenderTexture Render(Texture from, RenderTexture to, Shader shade)
        {
            brushRendy.CopyBuffer(from, to, shade);
            return to;
        }

        public RenderTexture Render(Texture from, RenderTexture to, Material mat)
        {
            brushRendy.CopyBuffer(from, to, mat);
            return to;
        }

        public RenderTexture Render(Texture from, RenderTexture to) => Render(from, to, Data.brushRendy_bufferCopy);

        public RenderTexture Render(ImageData from, RenderTexture to) => Render(from.CurrentTexture(), to, Data.brushRendy_bufferCopy);

        public RenderTexture Render(Texture from, ImageData to) => Render(from, to.CurrentRenderTexture(), Data.brushRendy_bufferCopy);

        public void Render(Color col, RenderTexture to)
        {
            theCamera.targetTexture = to;
            brushRendy.PrepareColorPaint(col);
            Render();
            AfterRenderBlit(to);
        }

        void AfterRenderBlit(Texture target) {
            if (BigRT_pair.Length > 0 && BigRT_pair[0] && BigRT_pair[0] == target)
                secondBufferUpdated = false;
        }

        public void UpdateBufferTwo()
        {
            brushRendy.Set(Data.pixPerfectCopy);
            Graphics.Blit(BigRT_pair[0], BigRT_pair[1]);
            secondBufferUpdated = true;
            bigRTversion++;
        }

        public bool secondBufferUpdated = false;
        public void UpdateBufferSegment()
        {
            if (!Data.DebugDisableSecondBufferUpdate)
            {
                brushRendy.Set(BigRT_pair[0]);
                theCamera.targetTexture = BigRT_pair[1];
                brushRendy.Set(Data.brushRendy_bufferCopy);
                Render();
                secondBufferUpdated = true;
                bigRTversion++;
            }
        }
        #endregion

        #region Component MGMT
        private void OnEnable() {

            PainterStuff.applicationIsQuitting = false;

            Inst = this;

            if (!Data)
                dataHolder = Resources.Load("Painter_Data") as PainterDataAndConfig;

            meshManager.OnEnable();

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

            isLinearColorSpace = UnityEditor.PlayerSettings.colorSpace == ColorSpace.Linear;

            EditorApplication.update -= CombinedUpdate;
            if (!UnityHelperFunctions.ApplicationIsAboutToEnterPlayMode())
                EditorApplication.update += CombinedUpdate;


            if (!brushPrefab) {
                GameObject go = Resources.Load("prefabs/RenderCameraBrush") as GameObject;
                brushPrefab = go.GetComponent<RenderBrush>();
                if (!brushPrefab)
                    Debug.Log("Couldn't find brush prefab.");
                
            }

            if (Data)
                UnityHelperFunctions.RenamingLayer(Data.myLayer, "Painter Layer");

#endif

            if (!brushRendy)
            {
                brushRendy = GetComponentInChildren<RenderBrush>();
                if (!brushRendy)
                {
                    brushRendy = Instantiate(brushPrefab.gameObject).GetComponent<RenderBrush>();
                    brushRendy.transform.parent = this.transform;
                }
            }
            if (Data)
                brushRendy.gameObject.layer = Data.myLayer;

#if BUILD_WITH_PAINTER || UNITY_EDITOR


            transform.position = Vector3.up * 3000;
            if (!theCamera)
            {
                theCamera = GetComponent<Camera>();
                if (!theCamera)
                    theCamera = gameObject.AddComponent<Camera>();
            }

            theCamera.orthographic = true;
            theCamera.orthographicSize = orthoSize;
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

            RefreshPlugins();

            foreach (var p in _plugins)
                if (p != null) p.Enable();

        }

        private void OnDisable() {
            PainterStuff.applicationIsQuitting = true;

            if (Data)
                Data.OnDisable();

            downloadManager.Dispose();

            triedToFindCamera = false ;

#if UNITY_EDITOR
            BeforeClosing();
#endif

            if (_plugins!= null)
                foreach (var p in _plugins)
                    if (p != null) p.Disable();

        }

#if UNITY_EDITOR
        void BeforeClosing()
        {

            if (PlaytimePainter.previewHolderMaterial)
                PlaytimePainter.previewHolderMaterial.shader = PlaytimePainter.previewHolderOriginalShader;

            if (materialsUsingTendTex.Count > 0)
                autodisabledBufferTarget = materialsUsingTendTex[0].painterTarget;
            EmptyBufferTarget();

        }

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
        static int scipframes = 3;
        #endif
      

        public void CombinedUpdate() {

            if (!Data)
                return;

            meshManager.EditingUpdate();

#if UNITY_2018_1_OR_NEWER
            foreach( var j in blitJobsActive) 
                if (j.jobHandle.IsCompleted)
                    j.CompleteJob();
#endif

            Data.RemoteUpdate();

            List<PlaytimePainter> l = PlaytimePainter.playbackPainters;

            if ((l.Count > 0) && (!StrokeVector.PausePlayback))
            {
                if (!l.Last())
                    l.RemoveLast(1);
                else
                    l.Last().PlaybeckVectors();
            }

#if UNITY_EDITOR
            if (refocusOnThis) {
                scipframes--;
                if (scipframes == 0) {
                    UnityHelperFunctions.FocusOn(refocusOnThis);
                    refocusOnThis = null;
                    scipframes = 3;
                }
            }
#endif

            if (Data && (Data.disableNonMeshColliderInPlayMode) && (Application.isPlaying))
            {
                RaycastHit hit;
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
                {
                    Collider c = hit.collider;
                    if ((c.GetType() != typeof(MeshCollider)) && (PlaytimePainter.CanEditWithTag(c.tag))) c.enabled = false;
                }
            }

            PlaytimePainter p = PlaytimePainter.currently_Painted_Object;

            if (p && !Application.isPlaying)
            {
                if (p.ImgData == null)
                    PlaytimePainter.currently_Painted_Object = null;
                else
                {
                    TexMGMTdata.brushConfig.Paint(p.stroke, p);
                    p.Update();
                }
            }

            foreach (var pl in _plugins)
                pl.Update();

        }

        #endif

        public void CancelAllPlaybacks()
        {
            foreach (var p in PlaytimePainter.playbackPainters)
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

            (((BigRT_pair == null) || (BigRT_pair.Length == 0)) ? "No buffers" : "Using HDR buffers " + ((!BigRT_pair[0]) ? "uninitialized" : "inited")).nl();

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

                changed |= plauginsMeta.edit_List(ref _plugins, PainterManagerPluginBase.all);

                if (!plauginsMeta.Inspecting)
                {

                    if ("Find Plugins".Click())
                        RefreshPlugins();

                    if ("Delete Plugins".Click().nl())
                        _plugins = null;

                }
            }
            else plauginsMeta.Inspecting = false;

            return changed;
        }

        #endif
        #endregion

    }
}