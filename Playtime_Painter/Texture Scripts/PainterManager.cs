using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using System;
#if PEGI
using PlayerAndEditorGUI;
#endif
using UnityEngine.EventSystems;
using System.Linq;
using SharedTools_Stuff;

namespace Playtime_Painter
{



    [ExecuteInEditMode]
    public class PainterManager : PainterStuffMono
#if PEGI
        , IPEGI
#endif
    {

        [SerializeField]
        public PainterConfig painterCfg;

        public MeshManager meshManager = new MeshManager();

        public PlaytimePainter focusedPainter;

        public static PainterManager _inst;
        public static PainterManager inst
        {
            get
            {
                if (_inst == null) {
                    if (!PainterStuff.applicationIsQuitting)
                    {


                        _inst = GameObject.FindObjectOfType<PainterManager>();
                        if (_inst == null)
                        {
                            if (_inst != null)
                                _inst.gameObject.SetActive(true);
                            else
                            {
#if UNITY_EDITOR
                                GameObject go = Resources.Load("prefabs/" + PainterConfig.PainterCameraName) as GameObject;
                                _inst = Instantiate(go).GetComponent<PainterManager>();
                                _inst.name = PainterConfig.PainterCameraName;
                                _inst.RefreshPlugins();
#endif
                            }
                        }
                        if (_inst.meshManager == null)
                            _inst.meshManager = new MeshManager();

                    }
                    else { _inst = null;}
                }
                return _inst;
            }
        }

        public bool isLinearColorSpace;

        public const int renderTextureSize = 2048;

        [NonSerialized] public WebCamTexture webCamTexture;
        
        public List<ImageData> imgDatas = new List<ImageData>();

        public List<MaterialData> matDatas = new List<MaterialData>();

        public MaterialData getMaterialDataFor(Material mat)
        {
            if (mat == null)
                return null;

            MaterialData data = null;
            
            for (int i=0; i<matDatas.Count; i++) {
                var md = matDatas[i];
                if (md != null && md.material!= null) {
                    if (md.material == mat) {
                        data = md;

                        if (i > 3)
                            matDatas.Move(i, 0);
 
                        break;
                    }
                        
                } else {
                    matDatas.RemoveAt(i); i--;
                }
            }

            if ( data == null) {
                data = new MaterialData(mat);
                matDatas.Add(data);
               // Debug.Log("Creating material data for "+mat.ToString());
            }

       

            return data;
        }

        [NonSerialized]
        public Dictionary<string, List<ImageData>> recentTextures = new Dictionary<string, List<ImageData>>();

        public Texture[] sourceTextures;

        public Texture getSourceTexture(int ind)
        {
            return ind < sourceTextures.Length ? sourceTextures[ind] : null;
        }

        public Texture[] masks;

        public VolumetricDecal[] decals;

        public VolumetricDecal GetDecal(int no)
        {
            return ((decals.Length > no) && (decals[no].showInDropdown())) ? decals[no] : null;
        }

        [SerializeField]
        private List<PainterManagerPluginBase> _plugins;
        public int browsedPlugin;

        public List<PainterManagerPluginBase> plugins
        {
            get
            {

                if (_plugins == null)
                    RefreshPlugins();
                else
                    for (int i = 0; i < _plugins.Count; i++)
                        if (_plugins[i] == null) { _plugins.RemoveAt(i); i--; }
         

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


            browsedPlugin = -1;
            List<Type> allTypes = CsharpFuncs.GetAllChildTypesOf<PainterManagerPluginBase>();
            foreach (var t in allTypes)
            {
                bool contains = false;

                foreach (var p in _plugins)
                    if (p.GetType() == t) { contains = true; break; }

                if (!contains)
                {
                    var c = (PainterManagerPluginBase)gameObject.GetComponent(t);
                    if (c == null)
                        c = (PainterManagerPluginBase)gameObject.AddComponent(t);
                    _plugins.Add(c);
                    #if PEGI
                    ("Painter Plugin " + c.ToPEGIstring() + " added").showNotification();
                    #endif
                }
            }
            
                for (int i = 0; i < _plugins.Count; i++)
                    if (_plugins[i] != null && !_plugins[i].enabled)
                        _plugins[i].enabled = true;

        }
        public void DeletePlugins()
        {
            for (int i = 0; i < _plugins.Count; i++)
                _plugins[i].DestroyWhatever();

            _plugins = null;
        }

        public Camera rtcam;
        public int myLayer = 30; // this layer is used by camera that does painting. Make your other cameras ignore this layer.
        public RenderBrush brushPrefab;
        public static float orthoSize = 128; // Orthographic size of the camera. 
        public bool DebugDisableSecondBufferUpdate;
        public RenderBrush brushRendy = null;

        // Brush shaders
        public Shader br_Blit = null;
        public Shader br_Add = null;
        public Shader br_Copy = null;
        public Shader pixPerfectCopy = null;
        public Shader brushRendy_bufferCopy = null;
        public Shader Blit_Smoothed = null;
        public Shader br_Multishade = null;
        public Shader br_BlurN_SmudgeBrush = null;
        public Shader br_ColorFill = null;

        public Shader mesh_Preview = null;
        public Shader br_Preview = null;
        public Shader TerrainPreview = null;

        public Material defaultMaterial;

        static Vector3 prevPosPreview;
        static float previewAlpha = 1;

        // ******************* Buffers MGMT

        [NonSerialized] RenderTexture[] squareBuffers = new RenderTexture[10];

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

            if (squareBuffers[no] == null)
                squareBuffers[no] = new RenderTexture(width, width, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            
            return squareBuffers[no];
        }

        // Main Render Textures used for painting
        public RenderTexture[] BigRT_pair;
        public int bigRTversion = 0;


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

        public RenderTexture Downscale_ToBuffer(Texture tex, int width, int height) => Downscale_ToBuffer(tex, width, height, null, brushRendy_bufferCopy);

        public RenderTexture Downscale_ToBuffer(Texture tex, int width, int height, Material material, Shader shader) {

            if (tex == null)
                return null;

            if (!shader) shader = brushRendy_bufferCopy;

            bool square = (width == height);
            if ((!square) || (!Mathf.IsPowerOfTwo(width)))
                return Render(tex, GetNonSquareBuffer(width, height), shader);
            else
            {
                int tmpWidth = Mathf.Max(tex.width / 2, width);
                
                RenderTexture from = material ? Render(tex, GetSquareBuffer(tmpWidth), material) : Render(tex, GetSquareBuffer(tmpWidth),  shader);

                while (tmpWidth > width)
                {
                    tmpWidth /= 2;
                    from = material ? Render(from, GetSquareBuffer(tmpWidth), material) : Render(from, GetSquareBuffer(tmpWidth), shader);
                }

                return from;
            }
        }

        public MeshRenderer secondBufferDebug;

        // This are used in case user started to use Render Texture on another target. Data is moved to previous Material's Texture2D so that Render Texture Buffer can be used with new target. 
        [NonSerialized]
        public ImageData imgDataUsingRendTex;
        [NonSerialized]
        public List<MaterialData> materialsUsingTendTex = new List<MaterialData>();
        public PlaytimePainter autodisabledBufferTarget;

        public void EmptyBufferTarget()
        {

            if (imgDataUsingRendTex == null)
                return;

            if (imgDataUsingRendTex.texture2D != null) //&& (Application.isPlaying == false))
                imgDataUsingRendTex.RenderTexture_To_Texture2D();

            imgDataUsingRendTex.destination = texTarget.Texture2D;

            foreach (var m in materialsUsingTendTex)
                m.SetTextureOnLastTarget(imgDataUsingRendTex);

            materialsUsingTendTex.Clear();
            imgDataUsingRendTex = null;
        }

        public void changeBufferTarget(ImageData newTarget, MaterialData mat, string parameter, PlaytimePainter painter)
        {

            if (newTarget != imgDataUsingRendTex)
            {

                if (materialsUsingTendTex.Count > 0)
                    PlaytimePainter.SetOriginalShader();

                if (imgDataUsingRendTex != null)
                {
                    if (imgDataUsingRendTex.texture2D != null)
                        imgDataUsingRendTex.RenderTexture_To_Texture2D();

                    imgDataUsingRendTex.destination = texTarget.Texture2D;

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

            PainterConfig cfg = PainterConfig.inst;

            rtcam.cullingMask = 1 << myLayer;
            
            if (!GotBuffers())
            {
                // Debug.Log("Initing buffers");
                BigRT_pair = new RenderTexture[2];
                BigRT_pair[0] = new RenderTexture(renderTextureSize, renderTextureSize, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default);
                BigRT_pair[1] = new RenderTexture(renderTextureSize, renderTextureSize, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default);
                BigRT_pair[0].wrapMode = TextureWrapMode.Repeat;
                BigRT_pair[1].wrapMode = TextureWrapMode.Repeat;
                BigRT_pair[0].name = "Painter Buffer 0 _ " + renderTextureSize;
                BigRT_pair[1].name = "Painter Buffer 1 _ " + renderTextureSize;

            }


            if (secondBufferDebug != null)
            {
                secondBufferDebug.sharedMaterial.mainTexture = BigRT_pair[1];
                if (secondBufferDebug.GetComponent<PlaytimePainter>() != null)
                    DestroyImmediate(secondBufferDebug.GetComponent<PlaytimePainter>());
            }

            if (Camera.main != null)
                Camera.main.cullingMask &= ~(1 << myLayer);
        }

        public static bool GotBuffers()
        {
            return ((inst.BigRT_pair != null) && (_inst.BigRT_pair.Length > 0) && (_inst.BigRT_pair[0] != null));
        }


        // ******************* Brush Shader MGMT

        public static void Shader_PerFrame_Update(StrokeVector st, bool hidePreview, float size)
        {

            if ((hidePreview) && (previewAlpha == 0)) return;

            previewAlpha = Mathf.Lerp(previewAlpha, hidePreview ? 0 : 1, 0.1f);

            Shader.SetGlobalVector(PainterConfig.BRUSH_POINTED_UV, new Vector4(st.uvTo.x, st.uvTo.y, 0, previewAlpha));
            Shader.SetGlobalVector(PainterConfig.BRUSH_WORLD_POS_FROM, new Vector4(prevPosPreview.x, prevPosPreview.y, prevPosPreview.z, size));
            Shader.SetGlobalVector(PainterConfig.BRUSH_WORLD_POS_TO, new Vector4(st.posTo.x, st.posTo.y, st.posTo.z, (st.posTo - prevPosPreview).magnitude));
            prevPosPreview = st.posTo;
        }

        public void Shader_UpdateDecal(BrushConfig brush)
        {

            VolumetricDecal vd = GetDecal(brush.selectedDecal);
            if (vd != null)
            {
                Shader.SetGlobalTexture("_VolDecalHeight", vd.heightMap);
                Shader.SetGlobalTexture("_VolDecalOverlay", vd.overlay);
                Shader.SetGlobalVector("_DecalParameters", new Vector4(brush.decalAngle * Mathf.Deg2Rad, (vd.type == VolumetricDecalType.Add) ? 1 : -1,
                        Mathf.Clamp01(brush.speed / 10f), 0));
            }

        }

        public void Shader_BrushCFG_Update(BrushConfig brush, float brushAlpha, float textureWidth, bool RendTex, bool texcoord2, PlaytimePainter pntr)
        {

            var brushType = brush.type(!RendTex);

            bool is3Dbrush = brush.IsA3Dbrush(pntr);
            bool isDecal = (RendTex) && (brushType.isUsingDecals);

            Color c = brush.colorLinear.ToGamma();

#if UNITY_EDITOR
            //      if (isLinearColorSpace) c = c.linear;
#endif

            Shader.SetGlobalVector("_brushColor", c);

            Shader.SetGlobalVector("_brushMask", new Vector4(
                brush.mask.GetFlag(BrushMask.R) ? 1 : 0,
                brush.mask.GetFlag(BrushMask.G) ? 1 : 0,
                brush.mask.GetFlag(BrushMask.B) ? 1 : 0,
                brush.mask.GetFlag(BrushMask.A) ? 1 : 0));

            if (isDecal) Shader_UpdateDecal(brush);

            int ind = brush.selectedSourceMask;
            Shader.SetGlobalTexture("_SourceMask", ((ind < masks.Length) && (ind >= 0) && (brush.useMask) && (RendTex)) ? masks[ind] : null);

            Shader.SetGlobalVector("_maskDynamics", new Vector4(
                brush.maskTiling,
                RendTex ? brush.Hardness : 0,       // y - Hardness is 0 to do correct preview for Texture2D brush 
                (brush.flipMaskAlpha ? 0 : 1)
                , 0));

            Shader.SetGlobalVector("_maskOffset", new Vector4(
                brush.maskOffset.x,
                brush.maskOffset.y,
                0,
                0));

            Shader.SetGlobalVector("_brushForm", new Vector4(
                brushAlpha // x - transparency
                , brush.Size(is3Dbrush) // y - scale for sphere
                , brush.Size(is3Dbrush) / textureWidth // z - scale for uv space
                , brush.blurAmount)); // w - blur amount

            brushType.setKeyword(texcoord2);

            if (texcoord2) Shader.EnableKeyword(PainterConfig.BRUSH_TEXCOORD_2);
            else Shader.DisableKeyword(PainterConfig.BRUSH_TEXCOORD_2);

            brush.blitMode.setKeyword().SetGlobalShaderParameters();

            if (brush.blitMode.GetType() == typeof(BlitModeSamplingOffset))
            {
                Shader.EnableKeyword("PREVIEW_SAMPLING_DISPLACEMENT");

                Shader.DisableKeyword("PREVIEW_ALPHA");
                Shader.DisableKeyword("PREVIEW_RGB");
            }
            else
            {
                Shader.DisableKeyword("PREVIEW_SAMPLING_DISPLACEMENT");
                BlitModeExtensions.SetShaderToggle(PainterConfig.inst.previewAlphaChanel, "PREVIEW_ALPHA", "PREVIEW_RGB");
            }

            if ((RendTex) && (brush.blitMode.usingSourceTexture))
                Shader.SetGlobalTexture("_SourceTexture", getSourceTexture(brush.selectedSourceTexture));

        }

        public void ShaderPrepareStroke(BrushConfig bc, float brushAlpha, ImageData id, StrokeVector stroke, PlaytimePainter pntr)
        {
            if (BigRT_pair == null) UpdateBuffersState();

            bool isDoubleBuffer = (id.renderTexture == null);

            bool useSingle = (!isDoubleBuffer) || bc.isSingleBufferBrush();

            if ((!useSingle) && (!secondBufferUpdated))
                UpdateBufferTwo();

            if (stroke.firstStroke)
                Shader_BrushCFG_Update(bc, brushAlpha, id.width, id.TargetIsRenderTexture(), stroke.useTexcoord2, pntr);

            rtcam.targetTexture = id.currentRenderTexture();

            if (isDoubleBuffer)
                Shader.SetGlobalTexture(PainterConfig.DESTINATION_BUFFER, BigRT_pair[1]);

            Shader shd = null;
            if (pntr != null)
                foreach (var pl in plugins)
                {
                    Shader bs = useSingle ? pl.GetBrushShaderSingleBuffer(pntr) : pl.GetBrushShaderDoubleBuffer(pntr);
                    if (bs != null)
                    {
                        shd = bs;
                        break;
                    }
                }

            if (shd == null) shd = useSingle ? bc.blitMode.shaderForSingleBuffer : bc.blitMode.shaderForDoubleBuffer;

            brushRendy.Set(shd);

        }

        public void Blit(Texture tex, ImageData id)
        {
            if (tex == null || id == null)
                return;
            brushRendy.Set(pixPerfectCopy);
            Graphics.Blit(tex, id.currentRenderTexture(), brushRendy.meshRendy.sharedMaterial);

            AfterRenderBlit(id.currentRenderTexture());


        }
        
        public void Blit(Texture from, RenderTexture to)
        {
           Blit(from, to, pixPerfectCopy);

           /* if (from == null) return;
            brushRendy.Set(pixPerfectCopy);
            Graphics.Blit(from, to, brushRendy.meshRendy.sharedMaterial);
            AfterRenderBlit(to);*/
        }

        public void Blit(Texture from, RenderTexture to, Shader blitShader)
        {
            // Render(from, to, pixPerfectCopy);

            if (from == null) return;
            brushRendy.Set(blitShader);
            Graphics.Blit(from, to, brushRendy.meshRendy.sharedMaterial);
            AfterRenderBlit(to);
        }
        
        public void Render()
        {
            transform.rotation = Quaternion.identity;
            Shader.SetGlobalVector("_RTcamPosition", transform.position);

            brushRendy.gameObject.SetActive(true);
            rtcam.Render();
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

        public RenderTexture Render(Texture from, RenderTexture to) => Render(from, to , brushRendy_bufferCopy);

        public RenderTexture Render(ImageData from, RenderTexture to) => Render(from.currentTexture(), to, brushRendy_bufferCopy);

        public RenderTexture Render(Texture from, ImageData to) => Render(from, to.currentRenderTexture(), brushRendy_bufferCopy);

        public void Render(Color col, RenderTexture to)
        {
            rtcam.targetTexture = to;
            brushRendy.PrepareColorPaint(col);
            Render();
            AfterRenderBlit(to);
        }

        void AfterRenderBlit (Texture target)
        {
            if (BigRT_pair.Length > 0 && BigRT_pair[0] != null && BigRT_pair[0] == target)
            {
               // bigRTversion++;
                secondBufferUpdated = false;
            }
        }

        public void UpdateBufferTwo()
        {
            brushRendy.Set(pixPerfectCopy);
            Graphics.Blit(BigRT_pair[0], BigRT_pair[1]);
            secondBufferUpdated = true;
            bigRTversion++;
        }

        public bool secondBufferUpdated = false;
        public void UpdateBufferSegment()
        {
            if (!DebugDisableSecondBufferUpdate)
            {
                brushRendy.Set(BigRT_pair[0]);
                rtcam.targetTexture = BigRT_pair[1];
                brushRendy.Set(brushRendy_bufferCopy);
                Render();
                secondBufferUpdated = true;
                bigRTversion++;
            }
        }


        // *******************  Component MGMT

        public void ClearEmptyDatas()
        {
            for (int i = 0; i < imgDatas.Count; i++)
            {
                var id = imgDatas[i];
                if (id == null || (!id.needsToBeSaved)) { imgDatas.RemoveAt(i); id.DestroyWhatever(); i--; }
            }


            for (int index = 0; index < matDatas.Count; index++)
            {
                var md = matDatas[index];
                if (md.material == null || !md.material.SavedAsAsset()) matDatas.Remove(md);
            }
        }

        private void OnEnable()
        {

            PainterStuff.applicationIsQuitting = false;

            _inst = this;

       
       

            if (meshManager == null)
                meshManager = new MeshManager();

            ClearEmptyDatas();

            meshManager.OnEnable();

            rtcam.cullingMask = 1 << myLayer;

            if (PlaytimeToolComponent.enabledTool == null)
            {
                if (!Application.isEditor)
                {
#if BUILD_WITH_PAINTER
				PlaytimeToolComponent.enabledTool = typeof(PlaytimePainter);
#else
                    PlaytimeToolComponent.GetPrefs();
#endif
                }
                else
                    PlaytimeToolComponent.GetPrefs();
            }
#if UNITY_EDITOR

            EditorSceneManager.sceneSaving -= BeforeSceneSaved;
            EditorSceneManager.sceneSaving += BeforeSceneSaved;
            // Debug.Log("Adding scene saving delegate");
            EditorSceneManager.sceneOpening -= OnSceneOpening;
            EditorSceneManager.sceneOpening += OnSceneOpening;
            
            if (defaultMaterial == null)
                defaultMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");

            if (defaultMaterial == null) Debug.Log("Default Material not found.");

            isLinearColorSpace = UnityEditor.PlayerSettings.colorSpace == ColorSpace.Linear;

            EditorApplication.update -= combinedUpdate;
            if (!this.ApplicationIsAboutToEnterPlayMode())
                EditorApplication.update += combinedUpdate;

            //  EditorApplication.playModeStateChanged -= PlayModeStateChanged; // painterConfig.SaveChanges;
            //  EditorApplication.playModeStateChanged += PlayModeStateChanged; // painterConfig.SaveChanges;

            if (brushPrefab == null)
            {
                //string assetName = "Assets/" + painterConfig.ToolPath() + "/" + painterConfig.DependenciesFolder + "/prefabs/RenderCameraBrush.prefab";
                GameObject go = Resources.Load("prefabs/RenderCameraBrush") as GameObject; //(GameObject)AssetDatabase.LoadAssetAtPath(assetName, typeof(GameObject));
                brushPrefab = go.GetComponent<RenderBrush>();
                if (brushPrefab == null)
                {
                    Debug.Log("Couldn't find brush prefab.");
                }
            }

            UnityHelperFunctions.RenamingLayer(myLayer, "Painter Layer");

#endif

            if (brushRendy == null)
            {
                brushRendy = GetComponentInChildren<RenderBrush>();
                if (brushRendy == null)
                {
                    brushRendy = Instantiate(brushPrefab.gameObject).GetComponent<RenderBrush>();
                    brushRendy.transform.parent = this.transform;
                }
            }
            brushRendy.gameObject.layer = myLayer;
            
#if BUILD_WITH_PAINTER || UNITY_EDITOR
            if (sourceTextures == null) sourceTextures = new Texture[0];
            if (masks == null) masks = new Texture[0];

            if (pixPerfectCopy == null) pixPerfectCopy = Shader.Find("Editor/PixPerfectCopy");
            
            if (Blit_Smoothed == null) Blit_Smoothed = Shader.Find("Editor/BufferBlit_Smooth");

            if (brushRendy_bufferCopy == null) brushRendy_bufferCopy = Shader.Find("Editor/BufferCopier");

            if (br_Blit == null) br_Blit = Shader.Find("Editor/br_Blit");

            if (br_Add == null) br_Add = Shader.Find("Editor/br_Add");

            if (br_Copy == null) br_Copy = Shader.Find("Editor/br_Copy");

            if (br_Multishade == null) br_Multishade = Shader.Find("Editor/br_Multishade");

            if (br_BlurN_SmudgeBrush == null) br_BlurN_SmudgeBrush = Shader.Find("Editor/BlurN_SmudgeBrush");

            if (br_ColorFill == null) br_ColorFill = Shader.Find("Editor/br_ColorFill");

            if (br_Preview == null) br_Preview = Shader.Find("Editor/br_Preview");

            if (mesh_Preview == null) mesh_Preview = Shader.Find("Editor/MeshEditorAssist");

            //if (br_TerrainPreview == null) 
            TerrainPreview = Shader.Find("Editor/TerrainPreview");

            transform.position = Vector3.up * 3000;
            if (rtcam == null)
            {
                rtcam = GetComponent<Camera>();
                if (rtcam == null)
                    rtcam = gameObject.AddComponent<Camera>();
            }

            rtcam.orthographic = true;
            rtcam.orthographicSize = orthoSize;
            rtcam.clearFlags = CameraClearFlags.Nothing;
            rtcam.enabled = Application.isPlaying;

#if UNITY_EDITOR
            EditorApplication.update -= combinedUpdate;
            if (EditorApplication.isPlayingOrWillChangePlaymode == false)
                EditorApplication.update += combinedUpdate;
#endif

            UpdateBuffersState();

#endif

            autodisabledBufferTarget = null;

            RefreshPlugins();


        }

        private void OnDisable()
        {
            PainterStuff.applicationIsQuitting = true;

            StopCamera();

            if (PlaytimePainter.isCurrent_Tool())
                PlaytimeToolComponent.SetPrefs();

            ClearEmptyDatas();



#if UNITY_EDITOR
            BeforeClosing();
#endif

#if UNITY_EDITOR
            EditorApplication.update -= meshManager.EditingUpdate;
#endif


        }

        private void OnDestroy()
        {
            if (!Application.isPlaying)
            {
                for (int i = 0; i < imgDatas.Count; i++)
                {
                    var id = imgDatas[i];
                    id.DestroyWhatever();
                }
            }
        }

#if UNITY_EDITOR

        void BeforeClosing()
        {

            if (PlaytimePainter.previewHolderMaterial != null)
                PlaytimePainter.previewHolderMaterial.shader = PlaytimePainter.previewHolderOriginalShader;

            PainterConfig.SaveChanges();
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

        public void Update()
        {
            if (Application.isPlaying)
                combinedUpdate();

            meshManager.Update();
        }

        public void StopCamera()
        {
            if (webCamTexture != null)
            {
                webCamTexture.Stop();
                webCamTexture.DestroyWhatever();
                webCamTexture = null;
            }
        }

        float cameraUnusedTime = 0f;
        public Texture GetWebCamTexture()
        {
            cameraUnusedTime = 0;


            if (webCamTexture == null)
                webCamTexture = new WebCamTexture(WebCamTexture.devices[0].name, 512, 512, 30);

            if (!webCamTexture.isPlaying)
                webCamTexture.Play();


            return webCamTexture;
        }
        
        public void combinedUpdate()
        {

            if (webCamTexture && webCamTexture.isPlaying)
            {
                cameraUnusedTime += Time.deltaTime;

                if (cameraUnusedTime > 10f)
                    webCamTexture.Stop();
            }

            List<PlaytimePainter> l = PlaytimePainter.playbackPainters;

            if ((l.Count > 0) && (!StrokeVector.PausePlayback))
            {
                if (l.Last() == null)
                    l.RemoveLast(1);
                else
                    l.Last().PlaybeckVectors();
            }

            PlaytimeToolComponent.CheckRefocus();

            if ((PainterConfig.inst.disableNonMeshColliderInPlayMode) && (Application.isPlaying))
            {
                RaycastHit hit;
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
                {
                    Collider c = hit.collider;
                    if ((c.GetType() != typeof(MeshCollider)) && (PlaytimeToolComponent.PainterCanEditWithTag(c.tag))) c.enabled = false;
                }
            }

            PlaytimePainter p = PlaytimePainter.currently_Painted_Object;

            if ((p != null) && (Application.isPlaying == false))
            {
                if ((p.imgData == null))
                {
                    PlaytimePainter.currently_Painted_Object = null;
                }
                else
                {
                    PainterConfig.inst.brushConfig.Paint(p.stroke, p);
                    p.Update();
                }
                

            }

        }

#endif

        public void CancelAllPlaybacks()
        {
            foreach (var p in PlaytimePainter.playbackPainters)
                p.playbackVectors.Clear();

            PlaytimePainter.cody = new StdDecoder(null);
        }

        void OnApplicationQuit()
        {
#if !UNITY_EDITOR && BUILD_WITH_PAINTER
       //     painterConfig.SaveChanges();
#endif
        }

#if PEGI
        [SerializeField] bool showImgDatas;
        [SerializeField] int inspectedImgData = -1;
        public bool PEGI()
        {

            pegi.nl();

            (((BigRT_pair == null) || (BigRT_pair.Length == 0)) ? "No buffers" : "Using HDR buffers " + ((BigRT_pair[0] == null) ? "uninitialized" : "inited")).nl();

            if (rtcam == null) { "no camera".nl(); return false; }

            if (("Img datas: " + imgDatas.Count + "").foldout(ref showImgDatas).nl())
                "Image Datas".edit_List_SO(imgDatas, ref inspectedImgData);

            if (inspectedImgData == -1)
            {
                if (("Mat datas: " + matDatas.Count + "").foldout(ref MaterialData.showMatDatas).nl())
                    matDatas.edit_List(ref MaterialData.inspectedMaterial, true);

#if UNITY_EDITOR
                "Using layer:".nl();
                myLayer = EditorGUILayout.LayerField(myLayer);
#endif
                pegi.newLine();
                "Disable Second Buffer Update (Debug Mode)".toggle(ref DebugDisableSecondBufferUpdate).nl();

                "Textures to copy from".edit(() => sourceTextures, this).nl();
                "Masks".edit(() => masks, this).nl();
                "Decals".edit(() => decals, this).nl();
            }

            return false;
        }

#endif
    }
}