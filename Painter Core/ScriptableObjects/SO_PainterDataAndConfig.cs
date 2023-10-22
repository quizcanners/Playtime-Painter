using System;
using System.Collections.Generic;
using System.Globalization;
using QuizCanners.Inspect;
using PainterTool.ComponentModules;
using PainterTool.MeshEditing;
using PainterTool.TexturePacking;
using QuizCanners.Migration;
using QuizCanners.Utils;
using UnityEngine;
using Object = UnityEngine.Object;
using static QuizCanners.Utils.ShaderProperty;

namespace PainterTool
{

#pragma warning disable IDE0018 // Inline variable declaration


    [CreateAssetMenu(fileName = "Painter Config", menuName = "Playtime Painter/Painter Config")]
    public class SO_PainterDataAndConfig : ScriptableObject, ICfgCustom, IPEGI
    {
        public int playtimePainterLayer = 30; // this layer is used by camera that does painting. Make your other cameras ignore this layer.

        public const string PREFABS_RESOURCE_FOLDER = "Playtime_Painter_Prefabs";

        public bool isLineraColorSpace;

        public static bool toolEnabled;

        public List<PlaytimePainter_BlitModeCustom> customBlitModes = new();

        public int selectedCustomBlitMode;

        public Material defaultMaterial;
        
        #region Shaders
        public bool dontIncludeShaderInBuild;

        [SerializeField] private List<Shader> shadersToBuldWith = new();

        [NonSerialized] public Shader additiveAlphaOutput;
        [NonSerialized] public Shader additiveAlphaAndUVOutput;
        [NonSerialized] public Shader multishadeBufferBlit;
        [NonSerialized] public Shader blurAndSmudgeBufferBlit;
        [NonSerialized] public Shader projectorBrushBufferBlit;

        [NonSerialized] public ShaderName brushBlit = new ("Playtime Painter/Editor/Brush/Blit");
        [NonSerialized] public ShaderName brushAdd = new ("Playtime Painter/Editor/Brush/Add");
        [NonSerialized] public ShaderName brushCopy = new ("Playtime Painter/Editor/Brush/Copy");
        [NonSerialized] public ShaderName pixPerfectCopy = new ("Playtime Painter/Buffer Blit/Pixel Perfect Copy");
        [NonSerialized] public ShaderName brushBufferCopy = new ("Playtime Painter/Editor/Buffer Blit/Copier");
        [NonSerialized] public ShaderName brushBlitSmoothed = new ("Playtime Painter/Buffer Blit/Smooth");
        [NonSerialized] public ShaderName brushDoubleBuffer = new ("Playtime Painter/Editor/Brush/DoubleBuffer");
        [NonSerialized] public ShaderName brushDoubleBufferProjector = new ("Playtime Painter/Editor/Brush/DoubleBuffer_Projector");
        [NonSerialized] public ShaderName brushBlurAndSmudge = new ("Playtime Painter/Editor/Brush/BlurN_Smudge");
        [NonSerialized] public ShaderName inkColorSpread = new ("Playtime Painter/Editor/Brush/Double Buffered/Spread");

        [NonSerialized] public Shader bufferColorFill;
        [NonSerialized] public Shader bufferCopyR;
        [NonSerialized] public Shader bufferCopyG;
        [NonSerialized] public Shader bufferCopyB;
        [NonSerialized] public Shader bufferCopyA;
        [NonSerialized] public Shader bufferBlendRGB;
        [NonSerialized] public Shader bufferCopyDownscaleX4;
        [NonSerialized] public Shader bufferCopyDownscaleX8;
        [NonSerialized] public Shader bufferCopyDownscaleX16_Approx;
        [NonSerialized] public Shader bufferCopyDownscaleX32_Approx;
        [NonSerialized] public Shader bufferCopyDownscaleX64_Approx;

        [NonSerialized] public Shader rayTraceOutput;

        public Shader GetShaderToWriteInto(ColorChanel chan)  {
            return chan switch
            {
                ColorChanel.R => bufferCopyR,
                ColorChanel.G => bufferCopyG,
                ColorChanel.B => bufferCopyB,
                ColorChanel.A => bufferCopyA,
                _ => null,
            };
        }

        [NonSerialized] public Shader previewMesh;
        [NonSerialized] public Shader previewBrush;

        public void CheckShaders(bool forceReload = false)
        {
            if (Application.isEditor) 
            {
                shadersToBuldWith.Clear();
            }

            CheckShader(ref pixPerfectCopy, forceReload);

            CheckShader(ref brushBlitSmoothed, forceReload);

            CheckShader(ref brushBufferCopy, forceReload);

            CheckShader(ref bufferColorFill, "Playtime Painter/Buffer Blit/Color Fill", forceReload);

            CheckShader(ref bufferCopyR, "Playtime Painter/Buffer Blit/Copy Red", forceReload);

            CheckShader(ref bufferCopyG, "Playtime Painter/Buffer Blit/Copy Green", forceReload);

            CheckShader(ref bufferCopyB, "Playtime Painter/Buffer Blit/Copy Blue", forceReload);

            CheckShader(ref bufferCopyA, "Playtime Painter/Buffer Blit/Copy Alpha", forceReload);

            CheckShader(ref bufferBlendRGB, "Playtime Painter/Editor/Buffer Blit/Blend", forceReload);

            CheckShader(ref multishadeBufferBlit, "Playtime Painter/Editor/Buffer Blit/Multishade", forceReload);

            CheckShader(ref blurAndSmudgeBufferBlit, "Playtime Painter/Editor/Buffer Blit/BlurN_Smudge", forceReload);

            CheckShader(ref projectorBrushBufferBlit, "Playtime Painter/Editor/Buffer Blit/Projector Brush", forceReload);

            CheckShader(ref brushBlit, forceReload);

            CheckShader(ref brushAdd, forceReload);

            CheckShader(ref brushCopy, forceReload);

            CheckShader(ref brushDoubleBuffer, forceReload);

            CheckShader(ref brushDoubleBufferProjector, forceReload);

            CheckShader(ref brushBlurAndSmudge, forceReload);

            CheckShader(ref inkColorSpread, forceReload);

            CheckShader(ref additiveAlphaOutput, "Playtime Painter/Editor/Brush/AdditiveAlphaOutput", forceReload);

            CheckShader(ref additiveAlphaAndUVOutput, "Playtime Painter/Editor/Brush/AdditiveUV_Alpha", forceReload);

            CheckShader(ref previewBrush, "Playtime Painter/Editor/Preview/Brush", forceReload);

            CheckShader(ref previewMesh, "Playtime Painter/Editor/Preview/Mesh", forceReload);

            CheckShader(ref bufferCopyDownscaleX4, "Playtime Painter/Buffer Blit/DownScaleX4", forceReload);

            CheckShader(ref bufferCopyDownscaleX8, "Playtime Painter/Buffer Blit/DownScaleX8", forceReload);

            CheckShader(ref bufferCopyDownscaleX16_Approx, "Playtime Painter/Buffer Blit/DownScaleX16_Approx", forceReload);

            CheckShader(ref bufferCopyDownscaleX32_Approx, "Playtime Painter/Buffer Blit/DownScaleX32_Approx", forceReload);

            CheckShader(ref bufferCopyDownscaleX64_Approx, "Playtime Painter/Buffer Blit/DownScaleX64_Approx", forceReload);

            CheckShader(ref rayTraceOutput, "Playtime Painter/Editor/Replacement/ShadowDataOutput", forceReload);

            this.SetToDirty();

        }

        private void CheckShader(ref Shader shade, string path, bool forceReload = false)
        {
            if (forceReload || !shade)
            {
                shade = Shader.Find(path);
                if (!shade)
                    Debug.LogError(QcLog.IsNull(shade, "{0} | {1}".F(path, nameof(CheckShader))));//"Could not find {0}".F(path));
            }
#if UNITY_EDITOR
            if (!dontIncludeShaderInBuild && shade)
                shadersToBuldWith.Add(shade);
#endif
        }

        private void CheckShader(ref ShaderName shade, bool forceReload = false)
        {

#if UNITY_EDITOR
            if (forceReload)
                shade.Reload();
            
            if (!dontIncludeShaderInBuild)
                shadersToBuldWith.Add(shade.Shader);
#endif
        }

        #endregion

        #region Constants

        public const string PainterCameraName = "PainterCamera";
        public const string ToolName = "Playtime Painter";

        #endregion

        #region Texture Data Lists



#if !UNITY_EDITOR
        [NonSerialized]
#endif
        internal List<TextureMapCombineProfile> texturePackagingSolutions = new();


        public List<AtlasTextureCreator> atlases = new();

#if !UNITY_EDITOR
        [NonSerialized]
#endif
        public List<MaterialAtlases> atlasedMaterials = new();

        public List<string> playtimeSavedTextures = new();

#if UNITY_EDITOR
        [SerializeField]
#else
        [NonSerialized]
#endif

        internal List<TextureMeta> imgMetas = new();

#if UNITY_EDITOR
        [SerializeField]
#else
        [NonSerialized]
#endif
        internal List<MaterialMeta> matMetas = new();
        public List<BrushTypes.VolumetricDecal> decals = new();

        public List<Texture> sourceTextures = new();

#if UNITY_EDITOR
        [SerializeField]
#else
        [NonSerialized]
#endif
        public List<Texture> masks = new();

        public int selectedColorScheme;
        public int inspectedColorScheme = -1;
        public bool showUrlField;
        public bool showRecentTextures;
        public bool showColorSchemes;

        internal MaterialMeta GetMaterialDataFor(Material mat)
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

                matMetas.RemoveAt(i); i--;
            }

            if (meta != null) return meta;
            
            meta = new MaterialMeta(mat);
            matMetas.Add(meta);
            
            return meta;
        }

   
        [NonSerialized]
        internal readonly Dictionary<TextureValue, List<TextureMeta>> recentTextures = new();

        public MeshPackagingProfile GetMeshPackagingProfile(string packageName)
        {
            foreach (var profile in meshPackagingSolutions)
            {
                if (profile.name.Equals(packageName))
                    return profile;
            }

            return meshPackagingSolutions[0];
        }

        public List<ColorPicker> colorSchemes = new();



        #endregion

        #region Mesh Editing

        public List<MeshPackagingProfile> meshPackagingSolutions = new();
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
        public bool showVolumeDetailsInPainter;

        public string materialsFolderName = "Materials";
        public string texturesFolderName = "Textures";
        public string meshesFolderName = "Models";
        public string vectorsFolderName = "Vectors";
        public string atlasFolderName = "Atlases";

        public bool enablePainterUIonPlay;
        public SO_BrushConfigScriptableObject BrushConfig;

        [NonSerialized] private readonly Brush _defaultBrush = new();
        public Brush Brush => BrushConfig ? BrushConfig.brush : _defaultBrush;
        
        public bool showColorSliders = true;
        public bool disableNonMeshColliderInPlayMode;

        public bool previewAlphaChanel;

        public bool allowExclusiveRenderTextures;
        public bool showConfig;
        public bool showTeachingNotifications;
        public Vector2Int samplingMaskSize;
        public bool useDepthForProjector;

        #endregion

        #region New Texture Config

        public bool useFloatForScalingBuffers;

        public bool newTextureIsColor = true;

        public Color newTextureClearColor = Color.black;

        public Color newTextureClearNonColorValue = new(0.5f, 0.5f, 0.5f, 0.5f);

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

        public List<string> recordingNames = new();

        public int browsedRecord;

        private static readonly Dictionary<string, string> Recordings = new();

        public IEnumerable<string> StrokeRecordingsFromFile(string filename)
        {
            string data;

            if (!Recordings.TryGetValue(filename, out data))
            {
                data = QcFile.Load.FromPersistentPath.String(vectorsFolderName, filename);
                Recordings.Add(filename, data);
            }

            var cody = new CfgDecoder(data);
            var strokes = new List<string>();
            foreach (var t in cody)
            {
                var d = cody.GetData();
                switch (t)
                {
                    case "strokes": d.ToList(out strokes); break;
                }
            }

            return strokes;
        }

        #endregion

        #region Encode/Decode

        [SerializeField] private CfgData stdData;

        [NonSerialized] private bool cfgLoaded;

        public void DecodeInternal(CfgData data)
        {
            cfgLoaded = true;
            this.DecodeTagsFrom(data);
        }

        private readonly LoopLock _encodeDecodeLock = new();

        public CfgEncoder Encode()
        {
            if (_encodeDecodeLock.Unlocked)
            {
                using (_encodeDecodeLock.Lock())
                {
                    for (var i = 0; i < imgMetas.Count; i++)
                    {
                        var id = imgMetas[i];
                        if (id != null && id.NeedsToBeSaved) 
                            continue;
                        
                        imgMetas.RemoveAt(i); 
                        i--; 
                    }

                    for (var index = 0; index < matMetas.Count; index++)
                    {
                        var md = matMetas[index];
                        if (QcUnity.IsSavedAsAsset(md.material)) continue;
                        
                        matMetas.Remove(md);
                        index--;
                    }
                    

                    var cody = new CfgEncoder()
                        .Add("sch", selectedColorScheme)
                        .Add("pals", colorSchemes)
                        .Add("cam", Singleton.Get<Singleton_PainterCamera>())
                        .Add("Vpck", meshPackagingSolutions)
                        .Add_IfTrue("hd", hideDocumentation)
                        .Add_IfNotNegative("im", _inspectedMaterial)
                        .Add_IfNotNegative("id", _inspectedDecal)
                        .Add_IfTrue("e", toolEnabled);

                    return cody;
                }
            }

            Debug.LogError("Loop in Encoding");

            return null;
        }

        public void DecodeTag(string key, CfgData data)
        {
            switch (key)
            {
                case "sch": data.ToInt(ref selectedColorScheme); break;
                case "pals": data.ToList(out colorSchemes); break;
                case "cam": if (Singleton.Get<Singleton_PainterCamera>()) Singleton.Get<Singleton_PainterCamera>().Decode(data); break;
                case "Vpck": data.ToList(out meshPackagingSolutions); break;
                case "hd": hideDocumentation = data.ToBool(); break;
                case "im": data.ToInt(ref _inspectedMaterial); break;
                case "id": data.ToInt(ref _inspectedDecal); break;
                case "e": toolEnabled = data.ToBool(); break;
            }
        }

        #endregion

        #region Inspector
        
      

        public static bool hideDocumentation;

        [SerializeField] private pegi.EnterExitContext _context = new();
        [SerializeField] private pegi.EnterExitContext _contextLists = new();
        [SerializeField] private pegi.CollectionInspectorMeta _imagesInspectorMeta = new("Img Metas");

     //   private int _inspectedImgData = -1;
       // private int _inspectedList = -1;
        private int _inspectedMaterial = -1;
        private int _inspectedDecal = -1;
        private int _inspectedMeshPackSol = -1;

        public void Inspect()
        {
            using (_context.StartContext())
            {
                pegi.Nl();

                if ("Data Lists".PegiLabel().IsEntered().Nl())
                    InspectLists();

                if ("Settings".PegiLabel().IsEntered().Nl())
                {
                    if ("Don't Build with Painter Shaders".PegiLabel().ToggleIcon(ref dontIncludeShaderInBuild).Nl())
                        CheckShaders(forceReload: true);

#if UNITY_EDITOR

                    if ("Enable PlayTime UI".PegiLabel().ToggleIcon(ref enablePainterUIonPlay).Nl())
                        Painter.MeshManager.StopEditingMesh();

                    "Hide documentation".PegiLabel().ToggleIcon(ref hideDocumentation);
                    MsgPainter.aboutDisableDocumentation.DocumentationClick();
                    pegi.Nl();

                    "Teaching Notifications".PegiLabel("Will show some notifications on the screen").ToggleIcon(ref showTeachingNotifications).Nl();

                    "Where to save content".PegiLabel(style: pegi.Styles.ListLabel).Nl();

                    "Textures".PegiLabel(60).Edit(ref texturesFolderName).Nl();

                    "Atlases: {0}/".F(texturesFolderName).PegiLabel(120).Edit(ref atlasFolderName).Nl();

                    "Materials".PegiLabel(60).Edit(ref materialsFolderName).Nl();

                    "Default for New Material".PegiLabel().Edit(ref defaultMaterial).Nl();

                    "Meshes".PegiLabel(60).Edit(ref meshesFolderName).Nl();

#endif
                }

                if (!BrushConfig)
                    "Brush Config not found, create {0} from context menu".F(SO_BrushConfigScriptableObject.FILE_NAME).PegiLabel().WriteWarning();

                "Shaders".PegiLabel().Enter_List_UObj(shadersToBuldWith).Nl();

                if (!BrushConfig || _context.IsCurrentEntered)
                    "Brush Config".PegiLabel().Edit(ref BrushConfig).Nl();

                if (_context.IsAnyEntered == false)
                {

                    if (!cfgLoaded)
                    {
                        if ("Initialize Object (Load Cfg)".PegiLabel().Click())
                            this.Decode(stdData);
                    }
                    else
                    if ("Painter Data Encode / Decode Test".PegiLabel().Click())
                    {
                        stdData = Encode().CfgData;
                        //this.SaveCfgData();

                        matMetas.Clear();

                        this.Decode(stdData);
                        //this.LoadCfgData();
                    }

                    pegi.Nl();

                    if (Icon.Discord.Click("Join Discord", 64))
                        PainterComponent.Open_Discord();

                    if (Icon.Docs.Click("Open Asset Documentation", 64))
                        PainterComponent.OpenWWW_Documentation();

                    if (Icon.Email.Click("Report a bug / send suggestion / ask question.", 64))
                        PainterComponent.Open_Email();

                    pegi.Nl();

                }
            }
        }

        private bool InspectLists()
        {
            using (_contextLists.StartContext())
            {
                var changes = pegi.ChangeTrackStart();

                _imagesInspectorMeta.Enter_List(imgMetas).Nl();

                "Mat Metas".PegiLabel().Enter_List(matMetas, ref _inspectedMaterial).Nl();

                "Source Textures".PegiLabel().Enter_List_UObj(sourceTextures).Nl();

                "Masks".PegiLabel().Enter_List_UObj(masks).Nl();

                "Decals".PegiLabel().Enter_List(decals, ref _inspectedDecal).Nl();

                "Mesh Packaging solutions".PegiLabel().Enter_List(meshPackagingSolutions, ref _inspectedMeshPackSol).Nl();

                if (_contextLists.IsCurrentEntered)
                {
#if UNITY_EDITOR
                    Object newProfile = null;

                    if ("Drop New Profile Here:".PegiLabel().Edit(ref newProfile).Nl())
                    {
                        var mSol = new MeshPackagingProfile();
                        mSol.Decode(new CfgData(QcFile.Load.TryLoadAsTextAsset(newProfile)));
                        meshPackagingSolutions.Add(mSol);
                    }
#endif
                }

                return changes;
            }
        }
        
        #endregion
        
        public void ResetMeshPackagingProfiles()
        {
            meshPackagingSolutions = new List<MeshPackagingProfile>
            {
                Load("Simple"),
                Load("Bevel"),
                Load("Bevel Weighted"),
                Load("AtlasedProjected"),
                Load("Standard_Atlased"),
                Load("Bevel With Seam"),
            };

            static MeshPackagingProfile Load(string name) => new MeshPackagingProfile().LoadFromResources(MeshPackagingProfile.FolderName, name);
        }

        public void ManagedOnEnable()
        {
            this.Decode(stdData);

            if (meshPackagingSolutions.IsNullOrEmpty())
                ResetMeshPackagingProfiles();

            if (samplingMaskSize.x == 0)
                samplingMaskSize = new Vector2Int(4, 4);

            CheckShaders();

            var decoder = new CfgDecoder(meshToolsStd);

            foreach (var tag in decoder)
            {
                var d = decoder.GetData();
                for (int i = 0; i < MeshToolBase.AllTools.Count; i++) {
                    var m = MeshToolBase.AllTools[i];
                    if (m.StdTag.SameAs(tag))
                    {
                        d.DecodeOverride(ref m);
                        break;
                    }
                }
            }
        }

        public void ManagedOnDisable() {

            var cody = new CfgEncoder();

            var at = MeshToolBase.AllTools;
            if (!at.IsNullOrEmpty())  {
                foreach (var t in at)
                    cody.Add(t.StdTag, t.Encode());

                meshToolsStd = cody.ToString();
            }

            stdData = Encode().CfgData;
            
#if UNITY_EDITOR
            if (!defaultMaterial)
                defaultMaterial = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
            
#endif

        }
    }

    #region Shader Tags
    public static class ShaderTags
    {
        public static readonly ShaderTag LayerType = new("_LayerType");
        public static class LayerTypes
        {
            public static readonly ShaderTagValue Transparent =
                new("Transparent", LayerType);
        }

        public static readonly ShaderTag SamplingMode = new ("_TextureSampling");
        public static class SamplingModes
        {
            public static readonly ShaderTagValue Uv1 = new ("UV1", SamplingMode);
            public static readonly ShaderTagValue Uv2 = new ("UV2", SamplingMode);
            public static readonly ShaderTagValue TriplanarProjection = new ("TriplanarProjection", SamplingMode);
        }

        public static readonly ShaderTag VertexColorRole = new ("_VertexColorRole");

        public static readonly ShaderTag MeshSolution = new ("Solution");

        public static class MeshSolutions
        {
            public static readonly ShaderTagValue Bevel = new ("Bevel", MeshSolution);
            public static readonly ShaderTagValue AtlasedProjected = new ("AtlasedProjected", MeshSolution);
        }
    }

    #endregion

    [PEGI_Inspector_Override(typeof(SO_PainterDataAndConfig))] internal class PainterDataAndConfigDrawer : PEGI_Inspector_Override { }
}