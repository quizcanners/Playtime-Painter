using QuizCannersUtilities;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using System;
using System.Globalization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Playtime_Painter
{

    public class PainterDataAndConfig : StdReferencesHolder, IKeepMyStd
    {
        private static PlaytimePainter Painter => PlaytimePainter.inspected;
        public int myLayer = 30; // this layer is used by camera that does painting. Make your other cameras ignore this layer.
        
        public static bool toolEnabled;

        #region Shaders
        public Shader brushBlit;
        public Shader brushAdd;
        public Shader brushCopy;
        public Shader pixPerfectCopy;
        public Shader brushBufferCopy;
        public Shader brushBlitSmoothed;
        public Shader brushDoubleBuffer;
        public Shader brushBlurAndSmudge;
        public Shader brushColorFill;

        public Shader previewMesh;
        public Shader previewBrush;
        public Shader previewTerrain;
        #endregion

        #region Constants
        public const string PainterCameraName = "PainterCamera";
        public const string ToolName = "Playtime_Painter";
        private const string EnablePainterForBuild = "BUILD_WITH_PAINTER";
        
        #region Material Preperties

        public const string GlobalPropertyPrefix = "g_";

        public static readonly ShaderProperty.VectorValue TerrainPosition = new ShaderProperty.VectorValue("_mergeTeraPosition");
        public static readonly ShaderProperty.VectorValue TerrainTiling = new ShaderProperty.VectorValue("_mergeTerrainTiling");
        public static readonly ShaderProperty.VectorValue TerrainScale = new ShaderProperty.VectorValue("_mergeTerrainScale");
        private const string TERRAIN_HEIGHT_TEXTURE = "_mergeTerrainHeight";
        public static readonly ShaderProperty.TextureValue TerrainHeight = new ShaderProperty.TextureValue (TERRAIN_HEIGHT_TEXTURE);
        public const string TERRAIN_CONTROL_TEXTURE = "_mergeControl";
        public static readonly ShaderProperty.TextureValue TerrainControlMain = new ShaderProperty.TextureValue(TERRAIN_CONTROL_TEXTURE);
        public const string TERRAIN_SPLAT_DIFFUSE = "_mergeSplat_";
        public const string TERRAIN_NORMAL_MAP = "_mergeSplatN_";
        private const string TERRAIN_LIGHT_TEXTURE = "_TerrainColors";
        public static readonly ShaderProperty.TextureValue TerrainLight = new ShaderProperty.TextureValue(TERRAIN_LIGHT_TEXTURE);
        public static readonly ShaderProperty.TextureValue PreviewTexture = new ShaderProperty.TextureValue("_PreviewTex");

        public static readonly ShaderProperty.VectorValue BRUSH_WORLD_POS_FROM = new ShaderProperty.VectorValue("_brushWorldPosFrom");
        public static readonly ShaderProperty.VectorValue BRUSH_WORLD_POS_TO = new ShaderProperty.VectorValue("_brushWorldPosTo");
        public static readonly ShaderProperty.VectorValue BRUSH_POINTED_UV = new ShaderProperty.VectorValue("_brushPointedUV");
        public static readonly ShaderProperty.VectorValue BRUSH_EDITED_UV_OFFSET = new ShaderProperty.VectorValue("_brushEditedUVoffset");
        public static readonly ShaderProperty.VectorValue BRUSH_ATLAS_SECTION_AND_ROWS = new ShaderProperty.VectorValue("_brushAtlasSectionAndRows");
        public static readonly ShaderProperty.VectorValue BRUSH_SAMPLING_DISPLACEMENT = new ShaderProperty.VectorValue("_brushSamplingDisplacement");
        public static readonly ShaderProperty.TextureValue DESTINATION_BUFFER = new ShaderProperty.TextureValue("_DestBuffer");
        public static readonly ShaderProperty.TextureValue SOURCE_TEXTURE = new ShaderProperty.TextureValue("_SourceTexture");
        public const string ATLASED_TEXTURES = "_AtlasTextures";
        public static readonly ShaderProperty.FloatValue TexturesInAtlasRow = new ShaderProperty.FloatValue(ATLASED_TEXTURES);
        public static readonly ShaderProperty.FloatValue BufferCopyAspectRatio = new ShaderProperty.FloatValue("_BufferCopyAspectRatio");
        #endregion


        #region Material Keywords
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


        public const string _MESH_PREVIEW_UV2 = "_MESH_PREVIEW_UV2";
        public const string MESH_PREVIEW_LIT = "MESH_PREVIEW_LIT";
        public const string MESH_PREVIEW_NORMAL = "MESH_PREVIEW_NORMAL";
        public const string MESH_PREVIEW_VERTCOLOR = "MESH_PREVIEW_VERTCOLOR";
        public const string MESH_PREVIEW_PROJECTION = "MESH_PREVIEW_PROJECTION";
        #endregion


        public const string TransparentLayerExpected = "TransparentLayerExpected";
        public const string TextureSampledWithUv2 = "TextureSampledWithUV2";
        public const string VertexColorRole = "VertexColorRole_";
      
        #endregion

        #region WebCamStuff
        [NonSerialized] public WebCamTexture webCamTexture;

        public void RemoteUpdate()
        {
            if (!webCamTexture || !webCamTexture.isPlaying) return;
            
            _cameraUnusedTime += Time.deltaTime;

            if (_cameraUnusedTime > 10f)
                webCamTexture.Stop();
        }

        public void StopCamera()
        {
            if (webCamTexture == null) return;
            
            webCamTexture.Stop();
            webCamTexture.DestroyWhatever();
            webCamTexture = null;
            
        }

        private float _cameraUnusedTime;
        public Texture GetWebCamTexture()
        {
            _cameraUnusedTime = 0;

            if (!webCamTexture && WebCamTexture.devices.Length > 0)
                webCamTexture = new WebCamTexture(WebCamTexture.devices[0].name, 512, 512, 30);

            if (webCamTexture && !webCamTexture.isPlaying)
                webCamTexture.Play();

            return webCamTexture;
        }
        #endregion

        #region DataLists

        public List<string> playtimeSavedTextures = new List<string>();
        
        public List<ImageMeta> imgMetas = new List<ImageMeta>();

        public List<MaterialMeta> matMetas = new List<MaterialMeta>();

        public MaterialMeta GetMaterialDataFor(Material mat)
        {
            if (!mat)
                return null;

            MaterialMeta meta = null;

            for (var i = 0; i < matMetas.Count; i++)
            {
                var md = matMetas[i];
                
                if (md != null && md.material)
                {
                    if (md.material != mat) continue;
                    
                    meta = md;

                    if (i > 3)
                        matMetas.Move(i, 0);

                    break;
                    
                }
                else
                {
                    matMetas.RemoveAt(i); i--;
                }
            }

            if (meta != null) return meta;
            
            meta = new MaterialMeta(mat);
            matMetas.Add(meta);
            
            return meta;
        }

        public bool showRecentTextures;

        public bool showColorSchemes;
        [NonSerialized]
        public readonly Dictionary<ShaderProperty.TextureValue, List<ImageMeta>> recentTextures = new Dictionary<ShaderProperty.TextureValue, List<ImageMeta>>();

        public List<Texture> sourceTextures = new List<Texture>();

        public List<Texture> masks = new List<Texture>();

        public List<VolumetricDecal> decals = new List<VolumetricDecal>();

        public List<MeshPackagingProfile> meshPackagingSolutions = new List<MeshPackagingProfile>();

        public List<ColorScheme> colorSchemes = new List<ColorScheme>();

        public int selectedColorScheme;

        public int inspectedColorScheme = -1;

        public bool showUrlField;

        #endregion

        #region Mesh Editing

        public int meshTool;
        public MeshToolBase MeshTool { get { meshTool = Mathf.Min(meshTool, MeshToolBase.AllTools.Count - 1); return MeshToolBase.AllTools[meshTool]; } }
        public float bevelDetectionSensitivity = 6;
        public string meshToolsStd;
        public bool saveMeshUndos;
        #endregion

        #region User Settings
        public bool makeVerticesUniqueOnEdgeColoring;
        public int gridSize = 1;
        public bool snapToGrid;
        public bool newVerticesUnique;
        public bool newVerticesSmooth = true;
        public bool pixelPerfectMeshEditing;
        public bool useGridForBrush;

        public string materialsFolderName = "Materials";
        public string texturesFolderName = "Textures";
        public string meshesFolderName = "Models";
        public string vectorsFolderName = "Vectors";
        public string atlasFolderName = "Textures/Atlases";

        public bool enablePainterUIonPlay;
        public BrushConfig brushConfig;
        public bool showColorSliders = true;
        public bool disableNonMeshColliderInPlayMode;

        public bool previewAlphaChanel;

        public bool moreOptions;
        public bool allowExclusiveRenderTextures;
        public bool showConfig;
        public bool showTeachingNotifications;
        public bool disableSecondBufferUpdateDebug;
        public MyIntVec2 samplingMaskSize;
        #endregion

        #region New Texture Config

        public bool newTextureIsColor = true;

        public Color newTextureClearColor = Color.black;

        public Color newTextureClearNonColorValue = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        public int selectedWidthIndex = 4;

        public int selectedHeightIndex = 4;

        private static string[] _texSizes;

        private const int TexSizesRange = 11;
        private const int MinPowerOfSize = 2;

        public static string[] NewTextureSizeOptions {
            get {
                if (_texSizes != null && _texSizes.Length == TexSizesRange) return _texSizes;
                
                _texSizes = new string[TexSizesRange];
                for (var i = 0; i < TexSizesRange; i++)
                    _texSizes[i] = Mathf.Pow(2, i + MinPowerOfSize).ToString(CultureInfo.InvariantCulture);
                
                return _texSizes;
            }
        }

        public int SelectedWidthForNewTexture() => SizeIndexToSize(selectedWidthIndex);

        public int SelectedHeightForNewTexture() => SizeIndexToSize(selectedHeightIndex);

        private static int SizeIndexToSize(int ind) => (int)Mathf.Pow(2, ind + MinPowerOfSize);
        #endregion

        #region BrushStrokeRecordings
        public List<string> recordingNames = new List<string>();

        public int browsedRecord;

        private static readonly Dictionary<string, string> Recordings = new Dictionary<string, string>();

        public IEnumerable<string> StrokeRecordingsFromFile(string filename)
        {
            string data;

            if (!Recordings.TryGetValue(filename, out data))
            {
                data = StuffLoader.LoadFromPersistentPath(vectorsFolderName, filename);
                Recordings.Add(filename, data);
            }

            var cody = new StdDecoder(data);
            var strokes = new List<string>();
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

        [SerializeField] private string stdData = "";
        public string ConfigStd
        {
            get { return stdData; }
            set { stdData = value; }
        }

        private readonly LoopLock _encodeDecodeLock = new LoopLock();

        public override StdEncoder Encode()
        {
            if (_encodeDecodeLock.Unlocked)
            {
                using (_encodeDecodeLock.Lock())
                {

                    for (var i = 0; i < imgMetas.Count; i++)
                    {
                        var id = imgMetas[i];
                        if (id != null && id.NeedsToBeSaved) continue;
                        
                        imgMetas.RemoveAt(i); 
                        i--; 
                    
                    }

                    for (var index = 0; index < matMetas.Count; index++)
                    {
                        var md = matMetas[index];
                        if (md.material && md.material.SavedAsAsset()) continue;
                        
                        matMetas.Remove(md);
                        index--;
                        
                    }
                    

                    var cody = this.EncodeUnrecognized()
                        .Add("imgs", imgMetas, this)
                        .Add("sch", selectedColorScheme)
                        .Add("mats", matMetas, this)
                        .Add("pals", colorSchemes)
                        .Add("cam", PainterCamera.Inst)
                        .Add("Vpck", meshPackagingSolutions)

#if PEGI
                        .Add_IfNotNegative("iid", _inspectedImgData)
                        .Add_IfNotNegative("isfs", _inspectedStuffs)
                        .Add_IfNotNegative("im", _inspectedMaterial)
                        .Add_IfNotNegative("id", _inspectedDecal)
                        .Add_IfNotNegative("is", inspectedStuff)
#endif
                        .Add_IfTrue("e", toolEnabled);

                    return cody;
                }
            }
            else Debug.LogError("Loop in Encoding");

            return null;
        }

        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "imgs": data.Decode_List(out imgMetas, this); break;
                case "sch": selectedColorScheme = data.ToInt(); break;
                case "mats": data.Decode_List(out matMetas, this); break;
                case "pals": data.Decode_List(out colorSchemes); break;
                case "cam": if (PainterCamera.Inst) PainterCamera.Inst.Decode(data); break;
                case "Vpck": data.Decode_List(out meshPackagingSolutions); break;
                #if PEGI
                case "iid": _inspectedImgData = data.ToInt(); break;
                case "isfs": _inspectedStuffs = data.ToInt(); break;
                case "im": _inspectedMaterial = data.ToInt(); break;
                case "id": _inspectedDecal = data.ToInt(); break;
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
           private int _inspectedImgData = -1;
           private int _inspectedStuffs = -1;
           private int _inspectedMaterial = -1;
           private int _inspectedDecal = -1;

        private bool InspectData() {
            var changes = false;
            
            "Img Metas".enter_List(ref imgMetas, ref _inspectedImgData, ref _inspectedStuffs, 0).nl(ref changes);

            "Mat Metas".enter_List(ref matMetas, ref _inspectedMaterial, ref _inspectedStuffs, 1).nl(ref changes);

            "Source Textures".enter_List_UObj(ref sourceTextures, ref _inspectedStuffs, 2).nl(ref changes);

            "Masks".enter_List_UObj(ref masks, ref _inspectedStuffs, 3).nl(ref changes);

            "Decals".enter_List(ref decals, ref _inspectedDecal, ref _inspectedStuffs, 4).nl(ref changes);

            if (_inspectedStuffs != -1) return changes;

            #if UNITY_EDITOR
            if ("Refresh Brush Shaders".Click(14).nl()) {
                CheckShaders(true);
                "Shaders Refreshed".showNotificationIn3D_Views();
            }
            "Using layer:".write(80);
            myLayer = EditorGUILayout.LayerField(myLayer);

            pegi.nl();
            "Disable Second Buffer Update (Debug Mode)".toggleIcon(ref disableSecondBufferUpdateDebug).nl();
            #endif

            return changes;
        }

        public override bool Inspect()
        {
            var changed = false; 

            var rtp = PainterCamera.Inst;

            if (!PainterStuff.IsPlaytimeNowDisabled) {
                if ("Plugins".enter(ref inspectedStuff, 10).nl_ifNotEntered() && rtp.PluginsInspect().nl(ref changed))
                    rtp.SetToDirty();

                if ("Lists".enter(ref inspectedStuff, 11).nl(ref changed))
                    changed |= InspectData();

                changed |= "Downloads".enter_Inspect(PainterCamera.DownloadManager, ref inspectedStuff, 12).nl(ref changed);
            }
            else
                inspectedStuff = -1;

            if (inspectedStuff == -1) {

                #if UNITY_EDITOR

                var gotDefine = EnablePainterForBuild.GetDefine();

                    if ("Enable Painter for Playtime & Build".toggleIcon(ref gotDefine).nl())
                        EnablePainterForBuild.SetDefine(gotDefine);

                if (gotDefine)
                    ("In Tools->Playtime_Painter the folder Shaders should be moved into folder Resources so all the " +
                        "painting shaders will be build with the player. Ignore if you already done this.").writeHint();

                if (gotDefine && "Enable PlayTime UI".toggleIcon(ref enablePainterUIonPlay).nl())
                    MeshManager.Inst.DisconnectMesh();

                if (!PainterStuff.IsPlaytimeNowDisabled) {

                    if (Painter && Painter.meshEditing == false)
                        "Disable Non-Mesh Colliders in Play Mode".toggleIcon(ref disableNonMeshColliderInPlayMode).nl();

                    "Teaching Notifications".toggleIcon("Will show some notifications on the screen", ref showTeachingNotifications).nl();

                    "Save Textures To".edit(110, ref texturesFolderName).nl();

                    "_Atlas Textures Sub folder".edit(150, ref atlasFolderName).nl();

                    "Save Materials To".edit(110, ref materialsFolderName).nl();

                    "Save Meshes To".edit(110, ref meshesFolderName).nl();
                }

                if (icon.Discord.Click("Join Discord", 64))
                    PlaytimePainter.Open_Discord();

                if (icon.Docs.Click("Open Asset Documentation", 64))
                    PlaytimePainter.OpenWWW_Documentation();

                if (icon.Email.Click("Report a bug / send suggestion / ask question.", 64))
                    PlaytimePainter.Open_Email();
#endif

            }

            base.Inspect().nl(ref changed);

            return changed;
        }

        #endif
        #endregion

        private void Init() {

            if (brushConfig == null)
                brushConfig = new BrushConfig();

            if (meshPackagingSolutions.IsNullOrEmpty())
            {
                Debug.Log("Recreating mash packaging solutions");
                meshPackagingSolutions = new List<MeshPackagingProfile>
                {
                    (new MeshPackagingProfile()).LoadFromResources(MeshPackagingProfile.FolderName, "Simple"),
                    (new MeshPackagingProfile()).LoadFromResources(MeshPackagingProfile.FolderName, "Bevel"),
                    (new MeshPackagingProfile()).LoadFromResources(MeshPackagingProfile.FolderName, "AtlasedProjected"),
                    (new MeshPackagingProfile()).LoadFromResources(MeshPackagingProfile.FolderName, "Standard_Atlased")
                };
            }
            if (samplingMaskSize.x == 0)
                samplingMaskSize = new MyIntVec2(4);
            
            CheckShaders();

            var decoder = new StdDecoder(meshToolsStd);
            foreach (var tag in decoder) {
                var d = decoder.GetData();
                foreach (var m in MeshToolBase.AllTools)
                    if (m.ToString().SameAs(tag))
                    {
                        m.Decode(d);
                        break;
                    }
            }

        }

        private void CheckShaders(bool forceReload = false)
        {
            #if !UNITY_EDITOR
                return;
            #endif

            CheckShader(ref pixPerfectCopy,         "Playtime Painter/Buffer Blit/Pixel Perfect Copy",  forceReload);

            CheckShader(ref brushBlitSmoothed,          "Playtime Painter/Buffer Blit/Smooth",              forceReload);

            CheckShader(ref brushBufferCopy,  "Playtime Painter/Buffer Blit/Copier",              forceReload);

            CheckShader(ref brushBlit,                "Playtime Painter/Editor/Brush/Blit",               forceReload);

            CheckShader(ref brushAdd,                 "Playtime Painter/Editor/Brush/Add",                forceReload);

            CheckShader(ref brushCopy,                "Playtime Painter/Editor/Brush/Copy",               forceReload);

            CheckShader(ref brushDoubleBuffer,          "Playtime Painter/Editor/Brush/DoubleBuffer",       forceReload);

            CheckShader(ref brushBlurAndSmudge,   "Playtime Painter/Editor/Brush/BlurN_Smudge",       forceReload);

            CheckShader(ref brushColorFill,           "Playtime Painter/Buffer Blit/Color Fill",          forceReload);

            CheckShader(ref previewBrush,             "Playtime Painter/Editor/Preview/Brush",            forceReload);

            CheckShader(ref previewMesh,           "Playtime Painter/Editor/Preview/Mesh",             forceReload);

            CheckShader(ref previewTerrain,         "Playtime Painter/Editor/Preview/Terrain",          forceReload);
        }

        private static void CheckShader(ref Shader shade, string path, bool forceReload = false) {

            #if UNITY_EDITOR

            if (forceReload || !shade)
                shade = Shader.Find(path);

            #endif
        }

        public void ManagedOnEnable()
        {
            Decode(stdData);
            Init();
        }

        public void ManagedOnDisable()
        {
            StopCamera();

            if (!PainterStuff.applicationIsQuitting)
                meshToolsStd = new StdEncoder().Add(StdDecoder.ListElementTag, MeshToolBase.AllTools).ToString();
            
            stdData = Encode().ToString();
        }
    }
}