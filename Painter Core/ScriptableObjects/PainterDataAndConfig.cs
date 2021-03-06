﻿using System;
using System.Collections.Generic;
using System.Globalization;
using QuizCanners.Inspect;
using PlaytimePainter.ComponentModules;
using PlaytimePainter.MeshEditing;
using PlaytimePainter.TexturePacking;
using QuizCanners.Migration;
using QuizCanners.Utils;
using UnityEngine;
using Object = UnityEngine.Object;
using static QuizCanners.Utils.ShaderProperty;

namespace PlaytimePainter
{

#pragma warning disable IDE0018 // Inline variable declaration


    [CreateAssetMenu(fileName = "Painter Config", menuName = "Playtime Painter/Painter Config")]
    public class PainterDataAndConfig : ScriptableObject, ICfgCustom, IPEGI
    {
        public int playtimePainterLayer = 30; // this layer is used by camera that does painting. Make your other cameras ignore this layer.

        public const string PREFABS_RESOURCE_FOLDER = "Playtime_Painter_Prefabs";

        public bool isLineraColorSpace;

        public static bool toolEnabled;

        public List<PlaytimePainter_BlitModeCustom> customBlitModes = new List<PlaytimePainter_BlitModeCustom>();

        public int selectedCustomBlitMode;

        public Material defaultMaterial;
        
        #region Shaders
        public bool dontIncludeShaderInBuild;

        [SerializeField] private List<Shader> shadersToBuldWith = new List<Shader>();

        [NonSerialized] public Shader additiveAlphaOutput;
        [NonSerialized] public Shader additiveAlphaAndUVOutput;
        [NonSerialized] public Shader multishadeBufferBlit;
        [NonSerialized] public Shader blurAndSmudgeBufferBlit;
        [NonSerialized] public Shader projectorBrushBufferBlit;

        [NonSerialized] public ShaderName brushBlit = new ShaderName("Playtime Painter/Editor/Brush/Blit");
        [NonSerialized] public ShaderName brushAdd = new ShaderName("Playtime Painter/Editor/Brush/Add");
        [NonSerialized] public ShaderName brushCopy = new ShaderName("Playtime Painter/Editor/Brush/Copy");
        [NonSerialized] public ShaderName pixPerfectCopy = new ShaderName("Playtime Painter/Buffer Blit/Pixel Perfect Copy");
        [NonSerialized] public ShaderName brushBufferCopy = new ShaderName("Playtime Painter/Editor/Buffer Blit/Copier");
        [NonSerialized] public ShaderName brushBlitSmoothed = new ShaderName("Playtime Painter/Buffer Blit/Smooth");
        [NonSerialized] public ShaderName brushDoubleBuffer = new ShaderName("Playtime Painter/Editor/Brush/DoubleBuffer");
        [NonSerialized] public ShaderName brushDoubleBufferProjector = new ShaderName("Playtime Painter/Editor/Brush/DoubleBuffer_Projector");
        [NonSerialized] public ShaderName brushBlurAndSmudge = new ShaderName("Playtime Painter/Editor/Brush/BlurN_Smudge");
        [NonSerialized] public ShaderName inkColorSpread = new ShaderName("Playtime Painter/Editor/Brush/Double Buffered/Spread");

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
            switch (chan) {
                case ColorChanel.R: return bufferCopyR;
                case ColorChanel.G: return bufferCopyG;
                case ColorChanel.B: return bufferCopyB;
                case ColorChanel.A: return bufferCopyA;
            }

            return null;
        }

        [NonSerialized] public Shader previewMesh;
        [NonSerialized] public Shader previewBrush;
        [NonSerialized] public Shader previewTerrain;

        public void CheckShaders(bool forceReload = false)
        {
#if !UNITY_EDITOR
                return;
#else
            shadersToBuldWith.Clear();

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

            CheckShader(ref previewTerrain, "Playtime Painter/Editor/Preview/Terrain", forceReload);

            CheckShader(ref bufferCopyDownscaleX4, "Playtime Painter/Buffer Blit/DownScaleX4", forceReload);

            CheckShader(ref bufferCopyDownscaleX8, "Playtime Painter/Buffer Blit/DownScaleX8", forceReload);

            CheckShader(ref bufferCopyDownscaleX16_Approx, "Playtime Painter/Buffer Blit/DownScaleX16_Approx", forceReload);

            CheckShader(ref bufferCopyDownscaleX32_Approx, "Playtime Painter/Buffer Blit/DownScaleX32_Approx", forceReload);

            CheckShader(ref bufferCopyDownscaleX64_Approx, "Playtime Painter/Buffer Blit/DownScaleX64_Approx", forceReload);

            CheckShader(ref rayTraceOutput, "Playtime Painter/Editor/Replacement/ShadowDataOutput", forceReload);

            this.SetToDirty();

#endif
        }

        private void CheckShader(ref Shader shade, string path, bool forceReload = false)
        {

#if UNITY_EDITOR
            if (forceReload || !shade)
            {
                shade = Shader.Find(path);
                if (!shade)
                    Debug.LogError(QcLog.IsNull(shade, nameof(CheckShader)));//"Could not find {0}".F(path));
            }

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

        [SerializeField] internal List<TextureMapCombineProfile> texturePackagingSolutions = new List<TextureMapCombineProfile>();
        public List<AtlasTextureCreator> atlases = new List<AtlasTextureCreator>();
        public List<MaterialAtlases> atlasedMaterials = new List<MaterialAtlases>();
        public List<string> playtimeSavedTextures = new List<string>();
        [SerializeField] internal List<TextureMeta> imgMetas = new List<TextureMeta>();
        [SerializeField] internal List<MaterialMeta> matMetas = new List<MaterialMeta>();
        public List<BrushTypes.VolumetricDecal> decals = new List<BrushTypes.VolumetricDecal>();

        public List<Texture> sourceTextures = new List<Texture>();
        public List<Texture> masks = new List<Texture>();

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
        internal readonly Dictionary<ShaderProperty.TextureValue, List<TextureMeta>> recentTextures = new Dictionary<ShaderProperty.TextureValue, List<TextureMeta>>();

        public MeshPackagingProfile GetMeshPackagingProfile(string packageName)
        {
            foreach (var profile in meshPackagingSolutions)
            {
                if (profile.name.Equals(packageName))
                    return profile;
            }

            return meshPackagingSolutions[0];
        }

        public List<ColorScheme> colorSchemes = new List<ColorScheme>();



        #endregion

        #region Mesh Editing

        public List<MeshPackagingProfile> meshPackagingSolutions = new List<MeshPackagingProfile>();
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
        public PlaytimePainter_BrushConfigScriptableObject BrushConfig;

        [NonSerialized] private readonly Brush _defaultBrush = new Brush();
        public Brush Brush => BrushConfig ? BrushConfig.brush : _defaultBrush;
        
        public bool showColorSliders = true;
        public bool disableNonMeshColliderInPlayMode;

        public bool previewAlphaChanel;

        public bool moreOptions;
        public bool allowExclusiveRenderTextures;
        public bool showConfig;
        public bool showTeachingNotifications;
        public MyIntVec2 samplingMaskSize;
        public bool useDepthForProjector;

        #endregion

        #region New Texture Config

        public bool useFloatForScalingBuffers;

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

        public void Decode(CfgData data)
        {
            cfgLoaded = true;
            this.DecodeTagsFrom(data);
        }

        private readonly LoopLock _encodeDecodeLock = new LoopLock();

        public CfgEncoder Encode()
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
                        if (QcUnity.IsSavedAsAsset(md.material)) continue;
                        
                        matMetas.Remove(md);
                        index--;
                        
                    }
                    

                    var cody = new CfgEncoder()//this.EncodeUnrecognized()
                        //.Add("imgs", imgMetas, this)
                        //.Add("mats", matMetas, this)
                        .Add("sch", selectedColorScheme)
                        
                        .Add("pals", colorSchemes)
                        .Add("cam", PainterCamera.Inst)
                        .Add("Vpck", meshPackagingSolutions)
                        .Add_IfTrue("hd", hideDocumentation)
                        .Add_IfNotNegative("iid", _inspectedImgData)
                        .Add_IfNotNegative("isfs", _inspectedList)
                        .Add_IfNotNegative("im", _inspectedMaterial)
                        .Add_IfNotNegative("id", _inspectedDecal)
                        .Add_IfNotNegative("is", inspectedItems)
                        .Add_IfTrue("e", toolEnabled);

                    return cody;
                }
            }

            Debug.LogError("Loop in Encoding");

            return null;
        }

        public void Decode(string key, CfgData data)
        {
            switch (key)
            {
                //case "imgs": data.Decode_List(out imgMetas, this); break;
                case "sch": data.ToInt(ref selectedColorScheme); break;
                //case "mats": data.Decode_List(out matMetas, this); break;
                case "pals": data.ToList(out colorSchemes); break;
                case "cam": if (PainterCamera.Inst) PainterCamera.Inst.DecodeFull(data); break;
                case "Vpck": data.ToList(out meshPackagingSolutions); break;
                case "hd": hideDocumentation = data.ToBool(); break;
                case "iid": data.ToInt(ref _inspectedImgData); break;
                case "isfs": data.ToInt(ref _inspectedList); break;
                case "im": data.ToInt(ref _inspectedMaterial); break;
                case "id": data.ToInt(ref _inspectedDecal); break;
                case "is": data.ToInt(ref inspectedItems); break;
                case "e": toolEnabled = data.ToBool(); break;
            }
        }

        #endregion

        #region Inspector
        
      

        public static bool hideDocumentation;

        private int inspectedItems = -1;
        private int _inspectedImgData = -1;
        private int _inspectedList = -1;
        private int _inspectedMaterial = -1;
        private int _inspectedDecal = -1;
        private int _inspectedMeshPackSol = -1;

        public void Inspect()
        {
            pegi.nl();

            if ("Data Lists".isEntered(ref inspectedItems, 11).nl())
                InspectLists();
            
            if ("Settings".isEntered(ref inspectedItems, 12).nl())
            {
                if ("Don't Build with Painter Shaders".toggleIcon(ref dontIncludeShaderInBuild).nl())
                    CheckShaders(forceReload: true);

                #if UNITY_EDITOR

                if ("Enable PlayTime UI".toggleIcon(ref enablePainterUIonPlay).nl())
                    MeshEditorManager.Inst.StopEditingMesh();

                "Hide documentation".toggleIcon(ref hideDocumentation);
                MsgPainter.aboutDisableDocumentation.DocumentationClick();
                pegi.nl();

                "Teaching Notifications".toggleIcon("Will show some notifications on the screen", ref showTeachingNotifications).nl();

                "Where to save content".nl(PEGI_Styles.ListLabel);

                "Textures".edit(60, ref texturesFolderName).nl();

                "Atlases: {0}/".F(texturesFolderName).edit(120, ref atlasFolderName).nl();

                "Materials".edit(60, ref materialsFolderName).nl();

                "Default for New Material".edit(ref defaultMaterial).nl();

                "Meshes".edit(60, ref meshesFolderName).nl();

                #endif
            }

            if (!BrushConfig || inspectedItems == 12)
            {
                "Brush Config not found, create {0} from context menu".F(PlaytimePainter_BrushConfigScriptableObject.FILE_NAME).writeWarning();

                "Brush Config".edit(ref BrushConfig).nl();
            }

            if (inspectedItems == -1) {

                if (!cfgLoaded)
                {
                    if ("Initialize Object (Load Cfg)".Click())
                        Decode(stdData);
                }
                else 
                if ("Painter Data Encode / Decode Test".Click())
                {
                    stdData = Encode().CfgData;
                    //this.SaveCfgData();

                    matMetas.Clear();

                    Decode(stdData);
                    //this.LoadCfgData();
                }

                pegi.nl();

                if (icon.Discord.Click("Join Discord", 64))
                    PlaytimePainter.Open_Discord();

                if (icon.Docs.Click("Open Asset Documentation", 64))
                    PlaytimePainter.OpenWWW_Documentation();

                if (icon.Email.Click("Report a bug / send suggestion / ask question.", 64))
                    PlaytimePainter.Open_Email();

                pegi.nl();

            }
            
        }

        private bool InspectLists()
        {
            var changes = pegi.ChangeTrackStart();

            "Img Metas".enter_List(imgMetas, ref _inspectedImgData, ref _inspectedList, 0).nl();

            "Mat Metas".enter_List(matMetas, ref _inspectedMaterial, ref _inspectedList, 1).nl();

            "Source Textures".enter_List_UObj(sourceTextures, ref _inspectedList, 2).nl();

            "Masks".enter_List_UObj(masks, ref _inspectedList, 3).nl();

            "Decals".enter_List(decals, ref _inspectedDecal, ref _inspectedList, 4).nl();

            "Mesh Packaging solutions".enter_List(meshPackagingSolutions, ref _inspectedMeshPackSol, ref _inspectedList, 5).nl();
            if (_inspectedList == 5)
            {
#if UNITY_EDITOR
                Object newProfile = null;

                if ("Drop New Profile Here:".edit(ref newProfile).nl())
                {
                    var mSol = new MeshPackagingProfile();
                    mSol.Decode(QcFile.Load.TryLoadAsTextAsset(newProfile));
                    meshPackagingSolutions.Add(mSol);
                }
#endif
            }

            return changes;
        }
        
        #endregion
        
        public void ResetMeshPackagingProfiles()
        {
            meshPackagingSolutions = new List<MeshPackagingProfile>
            {
                (new MeshPackagingProfile()).LoadFromResources(MeshPackagingProfile.FolderName, "Simple"),
                (new MeshPackagingProfile()).LoadFromResources(MeshPackagingProfile.FolderName, "Bevel"),
                (new MeshPackagingProfile()).LoadFromResources(MeshPackagingProfile.FolderName, "Bevel Weighted"),
                (new MeshPackagingProfile()).LoadFromResources(MeshPackagingProfile.FolderName, "AtlasedProjected"),
                (new MeshPackagingProfile()).LoadFromResources(MeshPackagingProfile.FolderName, "Standard_Atlased")
            };
        }

        public void ManagedOnEnable()
        {
            Decode(stdData);

            if (meshPackagingSolutions.IsNullOrEmpty())
                ResetMeshPackagingProfiles();

            if (samplingMaskSize.x == 0)
                samplingMaskSize = new MyIntVec2(4);

            CheckShaders();

            var decoder = new CfgDecoder(meshToolsStd);

            foreach (var tag in decoder)
            {
                var d = decoder.GetData();
                for (int i = 0; i < MeshToolBase.AllTools.Count; i++) {
                    var m = MeshToolBase.AllTools[i];
                    if (m.StdTag.SameAs(tag))
                    {
                        d.DecodeFull(ref m);
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

        public static readonly ShaderTag LayerType = new ShaderTag("_LayerType");
        public static class LayerTypes
        {
            public static readonly ShaderTagValue Transparent =
                new ShaderTagValue("Transparent", LayerType);
        }

        public static readonly ShaderTag SamplingMode = new ShaderTag("_TextureSampling");
        public static class SamplingModes
        {
            public static readonly ShaderTagValue Uv1 = new ShaderTagValue("UV1", SamplingMode);
            public static readonly ShaderTagValue Uv2 = new ShaderTagValue("UV2", SamplingMode);
            public static readonly ShaderTagValue TriplanarProjection = new ShaderTagValue("TriplanarProjection", SamplingMode);
        }

        public static readonly ShaderTag VertexColorRole = new ShaderTag("_VertexColorRole");

        public static readonly ShaderTag MeshSolution = new ShaderTag("Solution");

        public static class MeshSolutions
        {
            public static readonly ShaderTagValue Bevel = new ShaderTagValue("Bevel", MeshSolution);
            public static readonly ShaderTagValue AtlasedProjected = new ShaderTagValue("AtlasedProjected", MeshSolution);
        }


    }

    #endregion


    [PEGI_Inspector_Override(typeof(PainterDataAndConfig))] internal class PainterDataAndConfigDrawer : PEGI_Inspector_Override { }

}