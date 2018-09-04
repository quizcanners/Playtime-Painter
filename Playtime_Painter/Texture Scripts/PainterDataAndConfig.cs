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
    public class PainterDataAndConfig : STD_ReferancesHolder, IKeepMySTD
    {
        public static PlaytimePainter Painter { get { return PlaytimePainter.inspectedPainter; } }

        public string STDdata = "";
        public string Config_STD { get { return STDdata; }
            set { STDdata = value; } }

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

        public const string isAtlasedProperty = "_ATLASED";
        public const string isAtlasableDisaplyNameTag = "_ATL";
        public const string isUV2DisaplyNameTag = "_UV2";
        public const string atlasedTexturesInARow = "_AtlasTextures";

        public const string vertexColorRole = "VertexColorRole_";
        public const string bufferCopyAspectRatio = "_BufferCopyAspectRatio";

        [NonSerialized] public WebCamTexture webCamTexture;

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

        [NonSerialized]
        public Dictionary<string, List<ImageData>> recentTextures = new Dictionary<string, List<ImageData>>();

        public List<Texture> sourceTextures = new List<Texture>();

        public List<Texture> masks = new List<Texture>();

        public List<VolumetricDecal> decals = new List<VolumetricDecal>();

        public List<MeshPackagingProfile> meshPackagingSolutions;

        public int _meshTool;
        public MeshToolBase MeshTool { get { _meshTool = Mathf.Min(_meshTool, MeshToolBase.AllTools.Count - 1); return MeshToolBase.AllTools[_meshTool]; } }
        public float bevelDetectionSensetivity = 6;

        public static string ToolPath() => PlaytimeToolComponent.ToolsFolder + "/" + ToolName;

        public string meshToolsSTD = null;

        public static int DamAnimRendtexSize = 128;
        public bool allowEditingInFinalBuild;
        public bool MakeVericesUniqueOnEdgeColoring;
        public int SnapToGridSize = 1;
        public bool SnapToGrid = false;
        public bool newVerticesUnique = false;
        public bool newVerticesSmooth = true;
        public bool pixelPerfectMeshEditing = false;
        public bool useGridForBrush = false;

        public string materialsFolderName;
        public string texturesFolderName;
        public string meshesFolderName;
        public string vectorsFolderName;
        public string atlasFolderName;

        public bool enablePainterUIonPlay = false;
        public BrushConfig brushConfig;
        public bool disableNonMeshColliderInPlayMode;

        public bool previewAlphaChanel;
        public bool newTextureIsColor = true;

        public bool moreOptions = false;
        public bool showConfig = false;
        public bool ShowTeachingNotifications = false;
        public bool DebugDisableSecondBufferUpdate;
      
      
        public int myLayer = 30; // this layer is used by camera that does painting. Make your other cameras ignore this layer.
        
        public MyIntVec2 samplingMaskSize;

        public int selectedSize = 4;

        public List<string> recordingNames = new List<string>();

        public int browsedRecord;

        public static Dictionary<string, string> recordings = new Dictionary<string, string>();

        public List<string> StrokeRecordingsFromFile (string filename)
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
                    case "strokes": d.DecodeInto(out strokes); break;
                }
            }

            return strokes;
        }

    
        public override StdEncoder Encode() 
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
                .Add("mats", matDatas, this);

            return cody;
        }

        public override bool Decode(string tag, string data)
        {
            switch (tag)
            {
                case "imgs": data.DecodeInto(out imgDatas, this); break;
                case "mats": data.DecodeInto(out matDatas, this); break;
                default: return false;
            }
            return true;
        }

#if PEGI

        int inspectedImgData = -1;

          [SerializeField] bool showImgDatas;

        public bool DatasPEGI()
        {
            bool changes = false;

            if (("Img datas: " + imgDatas.Count + "").foldout(ref showImgDatas).nl())
                "Image Datas".edit_List(imgDatas, ref inspectedImgData, true);

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

                "Source Textures".edit_List_Obj(sourceTextures, true).nl();
                "Masks".edit_List_Obj(masks, true).nl();
                "Decals".edit(() => decals, this).nl();
               
            }

            return changes;
        }

        bool inspectLists = false;
        
        public override bool PEGI()
        {
            bool changed =  base.PEGI();

            PainterCamera rtp = PainterCamera.Inst;
            BrushConfig brush = brushConfig;
   
            if (!PainterStuff.IsNowPlaytimeAndDisabled)
            {
                "Plugins".write_List(rtp.Plugins, ref rtp.browsedPlugin);
       

                if ("Find New Plugins".Click())
                    rtp.RefreshPlugins();

                if ("Clear Data".Click().nl())
                {
                    rtp.DeletePlugins();
                    rtp.RefreshPlugins();
                }

            }
            pegi.newLine();

            bool gotDefine = UnityHelperFunctions.GetDefine(enablePainterForBuild);

            if ("Enable Painter for Playtime & Build".toggle(ref gotDefine).nl())
                UnityHelperFunctions.SetDefine(enablePainterForBuild, gotDefine);
            
            if (gotDefine && "Enable PlayTime UI".toggle(ref enablePainterUIonPlay).nl())
                MeshManager.Inst.DisconnectMesh();
            
            if (!PainterStuff.IsNowPlaytimeAndDisabled)
            {

                if (Painter && Painter.meshEditing == false)
                {
                    if ("More options".toggle(80, ref moreOptions).nl())
                        showConfig = false;

                    "CPU blit repaint delay".nl("Delay for video memory update when painting to Texture2D", 140);

                    changed |= pegi.edit(ref brush.repaintDelay, 0.01f, 0.5f).nl();

                    changed |= "Don't update mipmaps:".toggle("May increase performance, but your changes may not disaplay if you are far from texture.", 150,
                        ref brush.DontRedoMipmaps).nl();

                    var id = Painter.ImgData;

                    if (id != null)
                        changed |= id.PEGI();
                    
                    "Disable Non-Mesh Colliders in Play Mode:".toggle(ref disableNonMeshColliderInPlayMode).nl();

                }

                if ("Lists".foldout(ref inspectLists).nl())
                    changed |= DatasPEGI();
                
                "Teaching Notifications".toggle("will show whatever you ae pressing on the screen.", 140, ref ShowTeachingNotifications).nl();

                "Save Textures To:".edit(110, ref texturesFolderName).nl();

                "_Atlas Textures Sub folder".edit(150, ref atlasFolderName).nl();

                "Save Materials To:".edit(110, ref materialsFolderName).nl();

                "Save Meshes To:".edit(110, ref meshesFolderName).nl();
            }
#if UNITY_EDITOR
            if (icon.Discord.Click("Join Discord", 64))
                PlaytimePainter.Open_Discord();

            if (icon.Docs.Click("Open Asset Documentation", 64))
                PlaytimePainter.OpenWWW_Documentation();

            if (icon.Email.Click("Report a bug / send suggestion / ask question.", 64))
                PlaytimePainter.Open_Email();
#endif

            return changed;
        }

#endif

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


            if (webCamTexture == null && WebCamTexture.devices.Length>0)
                webCamTexture = new WebCamTexture(WebCamTexture.devices[0].name, 512, 512, 30);

            if (webCamTexture && !webCamTexture.isPlaying)
                webCamTexture.Play();

            return webCamTexture;
        }

        public void Init()
        {

            if (brushConfig == null)
                brushConfig = new BrushConfig();

            if ((meshPackagingSolutions == null) || (meshPackagingSolutions.Count == 0))
                meshPackagingSolutions = new List<MeshPackagingProfile>
                {
                    (new MeshPackagingProfile()).LoadFromResources(MeshPackagingProfile.folderName, "Simple"),
                    (new MeshPackagingProfile()).LoadFromResources(MeshPackagingProfile.folderName, "Bevel"),
                    (new MeshPackagingProfile()).LoadFromResources(MeshPackagingProfile.folderName, "AtlasedProjected"),
                    (new MeshPackagingProfile()).LoadFromResources(MeshPackagingProfile.folderName, "Standard_Atlased")
                };
            
            if (samplingMaskSize.x == 0) samplingMaskSize = new MyIntVec2(4);

            if (atlasFolderName == null || atlasFolderName.Length == 0)
            {
                materialsFolderName = "Materials";
                texturesFolderName = "Textures";
                vectorsFolderName = "Vectors";
                meshesFolderName = "Models";
                atlasFolderName = "ATLASES";
                recordingNames = new List<string>();
            }
            
#if BUILD_WITH_PAINTER || UNITY_EDITOR
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

            TerrainPreview = Shader.Find("Editor/TerrainPreview");
#endif

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
                cody.Add("e", MeshToolBase.AllTools);
                meshToolsSTD = cody.ToString();
            }

            STDdata = Encode().ToString();
        }
        
    }
}