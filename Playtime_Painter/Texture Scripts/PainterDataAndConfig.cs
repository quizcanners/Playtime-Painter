using SharedTools_Stuff;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Playtime_Painter
{

    [ExecuteInEditMode]
    public class PainterDataAndConfig : STD_ReferencesHolder, IKeepMySTD
    {
        public static PlaytimePainter Painter { get { return PlaytimePainter.inspectedPainter; } }
        public int myLayer = 30; // this layer is used by camera that does painting. Make your other cameras ignore this layer.

        public static bool toolEnabled = false;

        #region Shaders
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
        #endregion

        #region Constants
        public const string PainterCameraName = "PainterCamera";
        public const string ToolName = "Playtime_Painter";
        public const string enablePainterForBuild = "BUILD_WITH_PAINTER";

        public const string terrainPosition = "_mergeTeraPosition";
        public const string terrainTiling = "_mergeTerrainTiling";
        public const string terrainScale = "_mergeTerrainScale";
        public const string terrainHeight = "_mergeTerrainHeight";
        public const string terrainControl = "_mergeControl";
        public const string terrainTexture = "_mergeSplat_";
        public const string terrainNormalMap = "_mergeSplatN_";
        public const string terrainLight = "_TerrainColors";
        public const string previewTexture = "_PreviewTex";

        public const string BRUSH_WORLD_POS_FROM = "_brushWorldPosFrom";
        public const string BRUSH_WORLD_POS_TO = "_brushWorldPosTo";
        public const string BRUSH_POINTED_UV = "_brushPointedUV";
        public const string BRUSH_EDITED_UV_OFFSET = "_brushEditedUVoffset";
        public const string BRUSH_ATLAS_SECTION_AND_ROWS = "_brushAtlasSectionAndRows";
        public const string BRUSH_SAMPLING_DISPLACEMENT = "_brushSamplingDisplacement";
        public const string DESTINATION_BUFFER = "_DestBuffer";
        public const string SOURCE_TEXTURE = "_SourceTexture";

        public const string UV_NORMAL = "UV_NORMAL";
        public const string UV_ATLASED = "UV_ATLASED";
        public const string UV_PROJECTED = "UV_PROJECTED";
        public const string UV_PIXELATED = "UV_PIXELATED";
        public const string EDGE_WIDTH_FROM_COL_A = "EDGE_WIDTH_FROM_COL_A";
        public const string WATER_FOAM = "WATER_FOAM";
        public const string BRUSH_TEXCOORD_2 = "BRUSH_TEXCOORD_2";
        public const string TARGET_TRANSPARENT_LAYER = "TARGET_TRANSPARENT_LAYER";

        public const string isAtlasedProperty = "_ATLASED";
        public const string isAtlasableDisaplyNameTag = "_ATL";
        public const string isUV2DisaplyNameTag = "_UV2";
        public const string atlasedTexturesInARow = "_AtlasTextures";

        public const string TransparentLayerExpected = "TransparentLayerExpected";
        public const string TextureSampledWithUV2 = "TextureSampledWithUV2";
        public const string vertexColorRole = "VertexColorRole_";
        public const string bufferCopyAspectRatio = "_BufferCopyAspectRatio";
        #endregion

        #region WebCamStuff
        [NonSerialized] public WebCamTexture webCamTexture;

        public void RemoteUpdate()
        {
            if (webCamTexture && webCamTexture.isPlaying)
            {
                cameraUnusedTime += Time.deltaTime;

                if (cameraUnusedTime > 10f)
                    webCamTexture.Stop();
            }

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


            if (webCamTexture == null && WebCamTexture.devices.Length > 0)
                webCamTexture = new WebCamTexture(WebCamTexture.devices[0].name, 512, 512, 30);

            if (webCamTexture && !webCamTexture.isPlaying)
                webCamTexture.Play();

            return webCamTexture;
        }
        #endregion

        #region DataLists
        public List<ImageData> imgDatas = new List<ImageData>();

        public List<MaterialData> matDatas = new List<MaterialData>();

        public MaterialData GetMaterialDataFor(Material mat)
        {
            if (!mat)
                return null;

            MaterialData data = null;

            for (int i = 0; i < matDatas.Count; i++)
            {
                var md = matDatas[i];
                if (md != null && md.material)
                {
                    if (md.material == mat)
                    {
                        data = md;

                        if (i > 3)
                            matDatas.Move(i, 0);

                        break;
                    }

                }
                else
                {
                    matDatas.RemoveAt(i); i--;
                }
            }

            if (data == null)
            {
                data = new MaterialData(mat);
                matDatas.Add(data);
            }



            return data;
        }

        public bool showRecentTextures = false;

        public bool showColorSchemes = false;
        [NonSerialized]
        public Dictionary<string, List<ImageData>> recentTextures = new Dictionary<string, List<ImageData>>();

        public List<Texture> sourceTextures = new List<Texture>();

        public List<Texture> masks = new List<Texture>();

        public List<VolumetricDecal> decals = new List<VolumetricDecal>();

        public List<MeshPackagingProfile> meshPackagingSolutions = new List<MeshPackagingProfile>();

        public List<ColorScheme> colorSchemes = new List<ColorScheme>();

        public int selectedColorScheme = 0;

        public int inspectedColorScheme = -1;

        public bool showURLfield = false;

        #endregion

        public int _meshTool;
        public MeshToolBase MeshTool { get { _meshTool = Mathf.Min(_meshTool, MeshToolBase.AllTools.Count - 1); return MeshToolBase.AllTools[_meshTool]; } }
        public float bevelDetectionSensetivity = 6;
        public string meshToolsSTD = null;

        #region User Settings
        public static int DamAnimRendtexSize = 128;
        public bool allowEditingInFinalBuild;
        public bool MakeVericesUniqueOnEdgeColoring;
        public int SnapToGridSize = 1;
        public bool SnapToGrid = false;
        public bool newVerticesUnique = false;
        public bool newVerticesSmooth = true;
        public bool pixelPerfectMeshEditing = false;
        public bool useGridForBrush = false;

        public bool useJobsForCPUpainting = true;

        public string materialsFolderName;
        public string texturesFolderName;
        public string meshesFolderName;
        public string vectorsFolderName;
        public string atlasFolderName;

        public bool enablePainterUIonPlay = false;
        public BrushConfig brushConfig;
        public bool showColorSliders = true;
        public bool disableNonMeshColliderInPlayMode;

        public bool previewAlphaChanel;

        public bool moreOptions = false;
        public bool allowExclusiveRenderTextures = false;
        public bool showConfig = false;
        public bool ShowTeachingNotifications = false;
        public bool DebugDisableSecondBufferUpdate;
        public MyIntVec2 samplingMaskSize;
        #endregion

        #region New Texture Config

        public bool newTextureIsColor = true;

        public Color newTextureClearColor = Color.black;

        public Color newTextureClearNonColorValue = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        public int selectedSize = 4;

        static string[] texSizes;

        const int texSizesRange = 9;
        const int minPowerOfSize = 2;

        public static string[] NewTextureSizeOptions
        {
            get
            {
                if ((texSizes == null) || (texSizes.Length != texSizesRange))
                {
                    texSizes = new string[texSizesRange];
                    for (int i = 0; i < texSizesRange; i++)
                        texSizes[i] = Mathf.Pow(2, i + minPowerOfSize).ToString();
                }
                return texSizes;
            }

        }

        public static int SelectedSizeForNewTexture(int ind) => (int)Mathf.Pow(2, ind + minPowerOfSize);
        #endregion

        #region BrushStrokeRecordings
        public List<string> recordingNames = new List<string>();

        public int browsedRecord;

        public static Dictionary<string, string> recordings = new Dictionary<string, string>();

        public List<string> StrokeRecordingsFromFile(string filename)
        {
            string data;

            if (!recordings.TryGetValue(filename, out data))
            {
                data = StuffLoader.LoadFromPersistantPath(vectorsFolderName, filename);
                recordings.Add(filename, data);
            }

            var cody = new StdDecoder(data);
            List<string> strokes = new List<string>();
            foreach (var tag in cody)
            {
                var d = cody.GetData();
                switch (tag)
                {
                    case "strokes": d.Decode_List(out strokes); break;
                }
            }

            return strokes;
        }

        #endregion

        #region Encode/Decode

        public string STDdata = "";
        public string Config_STD
        {
            get { return STDdata; }
            set { STDdata = value; }
        }

        LoopLock encodeDecodeLock = new LoopLock();

        public override StdEncoder Encode()
        {
            if (encodeDecodeLock.Unlocked)
            {
                using (encodeDecodeLock.Lock())
                {

                    for (int i = 0; i < imgDatas.Count; i++)
                    {
                        var id = imgDatas[i];
                        if (id == null || (!id.NeedsToBeSaved)) { imgDatas.RemoveAt(i); i--; }
                    }

                    for (int index = 0; index < matDatas.Count; index++)
                    {
                        var md = matDatas[index];
                        if (md.material == null || !md.material.SavedAsAsset()) matDatas.Remove(md);
                    }

                    var cody = this.EncodeUnrecognized()
                        .Add("imgs", imgDatas, this)
                        .Add("sch", selectedColorScheme)
                        .Add("mats", matDatas, this)
                        .Add("pals", colorSchemes)
                        .Add("cam", PainterCamera.Inst)
                        .Add("Vpck", meshPackagingSolutions)

#if PEGI
                 .Add_IfNotNegative("iid", inspectedImgData)
                      .Add_IfNotNegative("isfs", inspectedStuffs)
                      .Add_IfNotNegative("im", inspectedMaterial)
                      .Add_IfNotNegative("id", inspectedDecal)
                      .Add_IfNotNegative("is", inspectedStuff)
#endif
              .Add_IfTrue("e", toolEnabled);

                    return cody;
                }
            }
            else Debug.LogError("Loop in Encoding");

            return null;
        }

        public override bool Decode(string tag, string data)
        {
            switch (tag)
            {
                case "imgs": data.Decode_List(out imgDatas, this); break;
                case "sch": selectedColorScheme = data.ToInt(); break;
                case "mats": data.Decode_List(out matDatas, this); break;
                case "pals": data.Decode_List(out colorSchemes); break;
                case "cam": PainterCamera.Inst?.Decode(data); break;
                case "Vpck": data.Decode_List(out meshPackagingSolutions); break;
                #if PEGI
                case "iid": inspectedImgData = data.ToInt(); break;
                case "isfs": inspectedStuffs = data.ToInt(); break;
                case "im": inspectedMaterial = data.ToInt(); break;
                case "id": inspectedDecal = data.ToInt(); break;
                case "is": inspectedStuff = data.ToInt(); break;
                #endif
                case "e": toolEnabled = data.ToBool(); break;
                default: return false;
            }
            return true;
        }
        #endregion

        #region Inspector
#if PEGI
           int inspectedImgData = -1;
        int inspectedStuffs = -1;
        int inspectedMaterial = -1;
        int inspectedDecal = -1;
        public bool DatasPEGI()
        {
            bool changes = false;

            changes |= "Img datas".enter_List(ref imgDatas, ref inspectedImgData, ref inspectedStuffs, 0).nl();

            changes |= "Mat datas".enter_List(ref matDatas, ref inspectedMaterial, ref inspectedStuffs, 1).nl();

            changes |= "Source Textures".enter_List_UObj(ref sourceTextures, ref inspectedStuffs, 2).nl();

            changes |= "Masks".enter_List_UObj(ref masks, ref inspectedStuffs, 3).nl();

            changes |= "Decals".enter_List(ref decals, ref inspectedDecal, ref inspectedStuffs, 4).nl();

            if (inspectedStuffs == -1)
            {
                if ("Refresh Shaders".Click())
                    CheckShaders(true);

                #if UNITY_EDITOR
                "Using layer:".nl();
                myLayer = EditorGUILayout.LayerField(myLayer);
                #endif
                pegi.nl();
                "Disable Second Buffer Update (Debug Mode)".toggleIcon(ref DebugDisableSecondBufferUpdate).nl();
            }


            return changes;
        }

        public override bool Inspect()
        {
            bool changed = false; 

            PainterCamera rtp = PainterCamera.Inst;

            if ("Plugins".enter(ref inspectedStuff, 10).nl_ifNotEntered() && rtp.PluginsInspect().nl())
                rtp.SetToDirty();

            if ("Lists".enter (ref inspectedStuff, 11).nl())
                changed |= DatasPEGI();

            changed |= "Downloads".enter_Inspect(PainterCamera.downloadManager, ref inspectedStuff, 12).nl();


            if (inspectedStuff == -1) {

                    bool gotDefine = UnityHelperFunctions.GetDefine(enablePainterForBuild);

                    if ("Enable Painter for Playtime & Build".toggleIcon(ref gotDefine).nl())
                        UnityHelperFunctions.SetDefine(enablePainterForBuild, gotDefine);

                if (gotDefine)
                    "In Tools->Playtime_Painter the folder Shaders should be moved into folder Resources so all the painting shaders will be build with the player.".writeHint();

                    if (gotDefine && "Enable PlayTime UI".toggleIcon(ref enablePainterUIonPlay).nl())
                        MeshManager.Inst.DisconnectMesh();

                    if (!PainterStuff.IsNowPlaytimeAndDisabled) {

                        if (Painter && Painter.meshEditing == false)
                            "Disable Non-Mesh Colliders in Play Mode".toggleIcon(ref disableNonMeshColliderInPlayMode).nl();

                        "Teaching Notifications".toggleIcon("Will show some notifications on the screen", ref ShowTeachingNotifications).nl();

                        "Save Textures To".edit(110, ref texturesFolderName).nl();

                        "_Atlas Textures Sub folder".edit(150, ref atlasFolderName).nl();

                        "Save Materials To".edit(110, ref materialsFolderName).nl();

                        "Save Meshes To".edit(110, ref meshesFolderName).nl();
                    }
#if UNITY_EDITOR
                    if (icon.Discord.Click("Join Discord", 64))
                        PlaytimePainter.Open_Discord();

                    if (icon.Docs.Click("Open Asset Documentation", 64))
                        PlaytimePainter.OpenWWW_Documentation();

                    if (icon.Email.Click("Report a bug / send suggestion / ask question.", 64))
                        PlaytimePainter.Open_Email();
#endif

              

            }

            changed |= base.Inspect();

            return changed;
        }

#endif
        #endregion

        public void Init() {

            if (brushConfig == null)
                brushConfig = new BrushConfig();

            if (meshPackagingSolutions.IsNullOrEmpty())
                meshPackagingSolutions = new List<MeshPackagingProfile>
                {
                    (new MeshPackagingProfile()).LoadFromResources(MeshPackagingProfile.folderName, "Simple"),
                    (new MeshPackagingProfile()).LoadFromResources(MeshPackagingProfile.folderName, "Bevel"),
                    (new MeshPackagingProfile()).LoadFromResources(MeshPackagingProfile.folderName, "AtlasedProjected"),
                    (new MeshPackagingProfile()).LoadFromResources(MeshPackagingProfile.folderName, "Standard_Atlased")
                };

            if (samplingMaskSize.x == 0)
                samplingMaskSize = new MyIntVec2(4);

            if (atlasFolderName == null || atlasFolderName.Length == 0)
            {
                materialsFolderName = "Materials";
                texturesFolderName = "Textures";
                vectorsFolderName = "Vectors";
                meshesFolderName = "Models";
                atlasFolderName = "ATLASES";
                recordingNames = new List<string>();
            }

            CheckShaders();

            var encody = new StdDecoder(meshToolsSTD);
            foreach (var tag in encody)
            {
                var d = encody.GetData();
                foreach (var m in MeshToolBase.AllTools)
                    if (m.ToString().SameAs(tag))
                    {
                        m.Decode(d);
                        break;
                    }
            }

        }

        void CheckShaders(bool forceReload = false)
        {
#if !UNITY_EDITOR
            return;
#endif

            CheckShader(ref pixPerfectCopy, "Playtime Painter/Buffer Blit/Pixel Perfect Copy", forceReload);

            CheckShader(ref Blit_Smoothed, "Playtime Painter/Buffer Blit/Smooth", forceReload);

            CheckShader(ref brushRendy_bufferCopy, "Playtime Painter/Buffer Blit/Copier", forceReload);

            CheckShader(ref br_Blit, "Playtime Painter/Brush/Blit", forceReload);

            CheckShader(ref br_Add, "Playtime Painter/Brush/Add", forceReload);

            CheckShader(ref br_Copy, "Playtime Painter/Brush/Copy", forceReload);

            CheckShader(ref br_Multishade, "Playtime Painter/Brush/DoubleBuffer", forceReload);

            CheckShader(ref br_BlurN_SmudgeBrush, "Playtime Painter/Brush/BlurN_Smudge", forceReload);

            CheckShader(ref br_ColorFill, "Playtime Painter/Buffer Blit/Color Fill", forceReload);

            CheckShader(ref br_Preview, "Playtime Painter/Preview/Brush", forceReload);

            CheckShader(ref mesh_Preview, "Playtime Painter/Preview/Mesh", forceReload);

            CheckShader(ref TerrainPreview, "Playtime Painter/Preview/Terrain", forceReload);
        }

        void CheckShader(ref Shader shade, string path, bool forceReload = false) {
#if UNITY_EDITOR
            if (forceReload || !shade)
                shade = Shader.Find(path);
#endif
        }

        public void OnEnable()
        {
            Init();
            Decode(STDdata); //.DecodeTagsFor(this);
        }

        public void OnDisable()
        {
            StopCamera();

            StdEncoder cody = new StdEncoder();
            if (!PainterStuff.applicationIsQuitting)
            {
                cody.Add(StdDecoder.ListElementTag, MeshToolBase.AllTools);
                meshToolsSTD = cody.ToString();
            }

            STDdata = Encode().ToString();
        }

    }
}