using QuizCannersUtilities;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using System;
using System.Globalization;
using PlaytimePainter.MeshEditing;

namespace PlaytimePainter
{

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration


    public class PainterDataAndConfig : CfgReferencesHolder, IKeepMyCfg
    {
        private static PlaytimePainter Painter => PlaytimePainter.inspected;
        public int playtimePainterLayer = 30; // this layer is used by camera that does painting. Make your other cameras ignore this layer.

        [SerializeField] public bool isLineraColorSpace;

        public static bool toolEnabled;
        
        #region Shaders

        public Shader additiveAlphaOutput;
        public Shader additiveAlphaAndUVOutput;
        public Shader multishadeBufferBlit;
        public Shader blurAndSmudgeBufferBlit;
        public Shader projectorBrushBufferBlit;

        public Shader brushBlit;
        public Shader brushAdd;
        public Shader brushCopy;
        public Shader pixPerfectCopy;
        public Shader brushBufferCopy;
        public Shader brushBlitSmoothed;
        public Shader brushDoubleBuffer;
        public Shader brushDoubleBufferProjector;
        public Shader brushBlurAndSmudge;
        public Shader inkColorSpread;

        public Shader bufferColorFill;
        public Shader bufferCopyR;
        public Shader bufferCopyG;
        public Shader bufferCopyB;
        public Shader bufferCopyA;
        public Shader bufferBlendRGB;
        public Shader bufferCopyDownscaleX4;
        public Shader bufferCopyDownscaleX8;
        public Shader bufferCopyDownscaleX16_Approx;
        public Shader bufferCopyDownscaleX32_Approx;
        public Shader bufferCopyDownscaleX64_Approx;
        
        public Shader rayTraceOutput;

        public Shader GetShaderToWriteInto(ColorChanel chan)  {
            switch (chan) {
                case ColorChanel.R: return bufferCopyR;
                case ColorChanel.G: return bufferCopyG;
                case ColorChanel.B: return bufferCopyB;
                case ColorChanel.A: return bufferCopyA;
            }

            return null;
        }

        public Shader previewMesh;
        public Shader previewBrush;
        public Shader previewTerrain;
#endregion

        #region Constants
        
        public const string PainterCameraName = "PainterCamera";
        public const string ToolName = "Playtime Painter";
        
        #endregion

#region DataLists

        public List<string> playtimeSavedTextures = new List<string>();
        
        public List<TextureMeta> imgMetas = new List<TextureMeta>();

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
        public readonly Dictionary<ShaderProperty.TextureValue, List<TextureMeta>> recentTextures = new Dictionary<ShaderProperty.TextureValue, List<TextureMeta>>();

        public List<Texture> sourceTextures = new List<Texture>();

        public List<Texture> masks = new List<Texture>();

        public List<BrushTypes.VolumetricDecal> decals = new List<BrushTypes.VolumetricDecal>();

        public List<MeshPackagingProfile> meshPackagingSolutions = new List<MeshPackagingProfile>();

        public MeshPackagingProfile GetMeshPackagingProfile(string name)
        {
            foreach (var profile in meshPackagingSolutions)
            {
                if (profile.name.Equals(name))
                    return profile;
            }

            return meshPackagingSolutions[0];
        }

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
        public bool showVolumeDetailsInPainter;

        public string materialsFolderName = "Materials";
        public string texturesFolderName = "Textures";
        public string meshesFolderName = "Models";
        public string vectorsFolderName = "Vectors";
        public string atlasFolderName = "Atlases";

        public bool enablePainterUIonPlay;
        public BrushConfig brushConfig;
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
                data = QcFile.LoadUtils.LoadFromPersistentPath(vectorsFolderName, filename);
                Recordings.Add(filename, data);
            }

            var cody = new CfgDecoder(data);
            var strokes = new List<string>();
            foreach (var t in cody)
            {
                var d = cody.GetData();
                switch (t)
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

        public override CfgEncoder Encode()
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
                        if (QcUnity.SavedAsAsset(md.material)) continue;
                        
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
                case "hd": hideDocumentation = data.ToBool(); break;
                case "iid": _inspectedImgData = data.ToInt(); break;
                case "isfs": _inspectedList = data.ToInt(); break;
                case "im": _inspectedMaterial = data.ToInt(); break;
                case "id": _inspectedDecal = data.ToInt(); break;
                case "is": inspectedItems = data.ToInt(); break;
                case "e": toolEnabled = data.ToBool(); break;
                default: return false;
            }
            return true;
        }

        #endregion

        #region Inspector
        
        [SerializeField] private int systemLanguage = -1;

        public static bool hideDocumentation;
        
        private int _inspectedImgData = -1;
        private int _inspectedList = -1;
        private int _inspectedMaterial = -1;
        private int _inspectedDecal = -1;
        private int _inspectedMeshPackSol = -1;

        private bool InspectLists()
        {
            var changes = false;

            "Img Metas".enter_List(ref imgMetas, ref _inspectedImgData, ref _inspectedList, 0).nl(ref changes);

            "Mat Metas".enter_List(ref matMetas, ref _inspectedMaterial, ref _inspectedList, 1).nl(ref changes);

            "Source Textures".enter_List_UObj(ref sourceTextures, ref _inspectedList, 2).nl(ref changes);

            "Masks".enter_List_UObj(ref masks, ref _inspectedList, 3).nl(ref changes);

            "Decals".enter_List(ref decals, ref _inspectedDecal, ref _inspectedList, 4).nl(ref changes);

            "Mesh Packaging solutions".enter_List(ref meshPackagingSolutions, ref _inspectedMeshPackSol, ref _inspectedList, 5).nl(ref changes);
            if (_inspectedList == 5)
            {
#if UNITY_EDITOR
                UnityEngine.Object newProfile = null;

                if ("Drop New Profile Here:".edit(ref newProfile).nl())
                {
                    var mSol = new MeshPackagingProfile();
                    mSol.Decode(QcFile.LoadUtils.TryLoadAsTextAsset(newProfile));
                   meshPackagingSolutions.Add(mSol);
                }
#endif
            }

            return changes;
        }

        public override bool Inspect()
        {
            var changed = false; 

            var rtp = PainterCamera.Inst;
            
            if ("Modules".enter(ref inspectedItems, 10, false).nl_ifNotEntered() && rtp.ModulsInspect().nl(ref changed))
                rtp.SetToDirty();

            if ("Lists".enter(ref inspectedItems, 11).nl(ref changed))
                InspectLists().changes(ref changed);

            if ("Painter Camera".enter(ref inspectedItems, 14).nl_ifNotEntered())
                PainterCamera.Inst.DependenciesInspect(true);

            if ("Depth Projector Camera".enter(ref inspectedItems, 15).nl())
            {
                if (DepthProjectorCamera.Instance)
                {
                    DepthProjectorCamera.Instance.Nested_Inspect().nl();
                } else if ("Instantiate".Click())
                    PainterCamera.GetProjectorCamera();
            }

            if ("Inspector & Debug".enter(ref inspectedItems, 16).nl())
                QcUtils.InspectInspector();
            
            if (inspectedItems == -1) {

                #if UNITY_EDITOR

                if ("Enable PlayTime UI".toggleIcon(ref enablePainterUIonPlay).nl())
                    MeshEditorManager.Inst.StopEditingMesh();

                if (enablePainterUIonPlay) {
                    "To have icons in your build move PlaytimePainter->Scripts->quizcanners->Editor->Resources outside of Editor folder (should be quizcanners->Resources)".writeHint();
                    pegi.nl();
                }

                "Hide documentation".toggleIcon(ref hideDocumentation).changes(ref changed);
                MsgPainter.aboutDisableDocumentation.DocumentationClick();
                pegi.nl();

                "Teaching Notifications".toggleIcon("Will show some notifications on the screen", ref showTeachingNotifications).nl();

                "Where to save content".nl(PEGI_Styles.ListLabel);

                "Textures".edit(60, ref texturesFolderName).nl();

                "Atlases: {0}/".F(texturesFolderName).edit(120, ref atlasFolderName).nl();

                "Materials".edit(60, ref materialsFolderName).nl();

                "Meshes".edit(60, ref meshesFolderName).nl();
                
                if (icon.Discord.Click("Join Discord", 64))
                    PlaytimePainter.Open_Discord();

                if (icon.Docs.Click("Open Asset Documentation", 64))
                    PlaytimePainter.OpenWWW_Documentation();

                if (icon.Email.Click("Report a bug / send suggestion / ask question.", 64))
                    PlaytimePainter.Open_Email();

                pegi.nl();
                
                LazyTranslations.LanguageSelection().nl();
                #endif

            }

            base.Inspect().nl(ref changed);

            return changed;
        }

        public bool InspectColorSchemes()
        {
            if (colorSchemes.Count == 0)
                colorSchemes.Add(new ColorScheme() { paletteName = "New Color Scheme" });

            return pegi.edit_List(ref colorSchemes, ref inspectedColorScheme);
        }
        
        #endregion



        public void CheckShaders(bool forceReload = false)
        {
            #if !UNITY_EDITOR
                return;
            #else

            CheckShader(ref pixPerfectCopy,             "Playtime Painter/Buffer Blit/Pixel Perfect Copy",      forceReload);

            CheckShader(ref brushBlitSmoothed,          "Playtime Painter/Buffer Blit/Smooth",                  forceReload);

            CheckShader(ref brushBufferCopy,            "Playtime Painter/Buffer Blit/Copier",                  forceReload);

            CheckShader(ref bufferColorFill,            "Playtime Painter/Buffer Blit/Color Fill",              forceReload);

            CheckShader(ref bufferCopyR,                "Playtime Painter/Buffer Blit/Copy Red",                forceReload);

            CheckShader(ref bufferCopyG,                "Playtime Painter/Buffer Blit/Copy Green",              forceReload);

            CheckShader(ref bufferCopyB,                "Playtime Painter/Buffer Blit/Copy Blue",               forceReload);

            CheckShader(ref bufferCopyA,                "Playtime Painter/Buffer Blit/Copy Alpha",              forceReload);

            CheckShader(ref bufferBlendRGB,             "Playtime Painter/Editor/Buffer Blit/Blend",            forceReload);

            CheckShader(ref multishadeBufferBlit,       "Playtime Painter/Editor/Buffer Blit/Multishade",       forceReload);

            CheckShader(ref blurAndSmudgeBufferBlit,    "Playtime Painter/Editor/Buffer Blit/BlurN_Smudge",     forceReload);

            CheckShader(ref projectorBrushBufferBlit,   "Playtime Painter/Editor/Buffer Blit/Projector Brush",  forceReload);

            CheckShader(ref brushBlit,                  "Playtime Painter/Editor/Brush/Blit",                   forceReload);

            CheckShader(ref brushAdd,                   "Playtime Painter/Editor/Brush/Add",                    forceReload);

            CheckShader(ref brushCopy,                  "Playtime Painter/Editor/Brush/Copy",                   forceReload);

            CheckShader(ref brushDoubleBuffer,          "Playtime Painter/Editor/Brush/DoubleBuffer",           forceReload);

            CheckShader(ref brushDoubleBufferProjector, "Playtime Painter/Editor/Brush/DoubleBuffer_Projector", forceReload);

            CheckShader(ref brushBlurAndSmudge,         "Playtime Painter/Editor/Brush/BlurN_Smudge",           forceReload);

            CheckShader(ref inkColorSpread,             "Playtime Painter/Editor/Brush/Spread",                 forceReload);

            CheckShader(ref additiveAlphaOutput,        "Playtime Painter/Editor/Brush/AdditiveAlphaOutput",    forceReload);

            CheckShader(ref additiveAlphaAndUVOutput,   "Playtime Painter/Editor/Brush/AdditiveUV_Alpha",       forceReload);
            
            CheckShader(ref previewBrush,               "Playtime Painter/Editor/Preview/Brush",                forceReload);

            CheckShader(ref previewMesh,                "Playtime Painter/Editor/Preview/Mesh",                 forceReload);

            CheckShader(ref previewTerrain,             "Playtime Painter/Editor/Preview/Terrain",              forceReload);

            CheckShader(ref bufferCopyDownscaleX4,      "Playtime Painter/Buffer Blit/DownScaleX4",             forceReload);

            CheckShader(ref bufferCopyDownscaleX8,      "Playtime Painter/Buffer Blit/DownScaleX8",             forceReload);

            CheckShader(ref bufferCopyDownscaleX16_Approx, "Playtime Painter/Buffer Blit/DownScaleX16_Approx",  forceReload);

            CheckShader(ref bufferCopyDownscaleX32_Approx, "Playtime Painter/Buffer Blit/DownScaleX32_Approx",  forceReload);

            CheckShader(ref bufferCopyDownscaleX64_Approx, "Playtime Painter/Buffer Blit/DownScaleX64_Approx",  forceReload);
            
            CheckShader(ref rayTraceOutput,             "Playtime Painter/Editor/Replacement/ShadowDataOutput", forceReload);

            #endif
        }

        private static void CheckShader(ref Shader shade, string path, bool forceReload = false) {

            #if UNITY_EDITOR
            if (forceReload || !shade)
            {
                shade = Shader.Find(path);
                if (!shade)
                    Debug.LogError("Could not find {0}".F(path));
            }
            #endif
        }

        private void ResetMeshPackagingProfiles()
        {
            meshPackagingSolutions = new List<MeshPackagingProfile>
            {
                (new MeshPackagingProfile()).LoadFromResources(MeshPackagingProfile.FolderName, "Simple"),
                (new MeshPackagingProfile()).LoadFromResources(MeshPackagingProfile.FolderName, "Bevel"),
                (new MeshPackagingProfile()).LoadFromResources(MeshPackagingProfile.FolderName, "AtlasedProjected"),
                (new MeshPackagingProfile()).LoadFromResources(MeshPackagingProfile.FolderName, "Standard_Atlased")
            };
        }

        public void ManagedOnEnable()
        {
            Decode(stdData);

            if (brushConfig == null)
                brushConfig = new BrushConfig();

            if (meshPackagingSolutions.IsNullOrEmpty())
                ResetMeshPackagingProfiles();

            if (samplingMaskSize.x == 0)
                samplingMaskSize = new MyIntVec2(4);

            CheckShaders();

            var decoder = new CfgDecoder(meshToolsStd);

            foreach (var tag in decoder)
            {
                var d = decoder.GetData();
                foreach (var m in MeshToolBase.AllTools)
                    if (m.StdTag.SameAs(tag))
                    {
                        m.Decode(d);
                        break;
                    }
            }

            if (systemLanguage != -1)
                LazyTranslations._systemLanguage = systemLanguage;
        }

        public void ManagedOnDisable() {

            var cody = new CfgEncoder();

            var at = MeshToolBase.AllTools;
            if (!at.IsNullOrEmpty())  {
                foreach (var t in at)
                    cody.Add(t.StdTag, t.Encode());

                meshToolsStd = cody.ToString();
            }

            stdData = Encode().ToString();
            
            systemLanguage = LazyTranslations._systemLanguage;
        }
    }

    #region Shader Tags
    public static partial class ShaderTags
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

}