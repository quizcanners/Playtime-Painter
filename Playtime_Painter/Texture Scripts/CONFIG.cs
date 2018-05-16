using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using PlayerAndEditorGUI;
using StoryTriggerData;
using SharedTools_Stuff;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Playtime_Painter{



    [Serializable]
    public class PainterConfig : PainterStuff  {
        static PainterConfig _inst;
        public static PainterConfig inst {
            get
            {
                if (_inst == null && !applicationIsQuitting)
                    LoadOrInit();

                return _inst;
            }
    }

        public static PlaytimePainter painter { get { return PlaytimePainter.inspectedPainter; } }

        public const string PainterCameraName = "PainterCamera";
        public const string ToolName = "Playtime_Painter";
        public const string enablePainterForBuild = "BUILD_WITH_PAINTER";

        // Terrain Global Shader namings:
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
        //public const string BRUSH_IS_ATLASED = "BRUSH_IS_ATLASED";
        // Preview Constants
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

        public const string vertexColorRole = "VertexColorRole_"; // + R, G, B, A

        public string meshToolsSTD = null;
        
        public static int DamAnimRendtexSize = 128;
        public bool allowEditingInFinalBuild;
        public bool MakeVericesUniqueOnEdgeColoring;
      //  public int MaxDistanceForTransformPosition = 100;
        public int SnapToGridSize = 1;
        public bool SnapToGrid = false;
        public bool newVerticesUnique = false;
        public bool newVerticesSmooth = true;
        public bool pixelPerfectMeshEditing = false;
        public bool useGridForBrush = false;

        public List<MeshPackagingProfile> meshPackagingSolutions;

        public int _meshTool;
        public MeshToolBase meshTool { get { _meshTool = Mathf.Min(_meshTool, MeshToolBase.allTools.Count - 1);  return MeshToolBase.allTools[_meshTool];} }
        public float bevelDetectionSensetivity = 6;

        public static string ToolPath() {
		return PlaytimeToolComponent.ToolsFolder + "/" + ToolName;
    }

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


        public myIntVec2 samplingMaskSize;

        public int selectedSize = 4;

        public List<string> recordingNames;

        public int browsedRecord;

        public static Dictionary<string, string> recordings = new Dictionary<string, string>();

        public string GetRecordingData(string name) {

            string data;

            if (recordings.TryGetValue(name, out data)) return data;

            data = ResourceLoader.LoadStoryFromResource(_inst.texturesFolderName, _inst.vectorsFolderName, name);

            recordings.Add(name, data);

            return data;
        }

        public void RemoveRecord() {
            RemoveRecord(recordingNames[browsedRecord]);
        }

        public void RemoveRecord(string name) {
            recordingNames.Remove(name);
            recordings.Remove(name);
            UnityHelperFunctions.DeleteResource(texturesFolderName, vectorsFolderName + "/" + name);
        }

        public void Init() {

            _inst = this;

            if ((meshPackagingSolutions == null) || (meshPackagingSolutions.Count == 0)) {
                meshPackagingSolutions = new List<MeshPackagingProfile>();
				meshPackagingSolutions.Add((new MeshPackagingProfile()).LoadFromResources(MeshPackagingProfile.folderName, "Simple"));
                meshPackagingSolutions.Add((new MeshPackagingProfile()).LoadFromResources(MeshPackagingProfile.folderName, "Bevel"));
				meshPackagingSolutions.Add((new MeshPackagingProfile()).LoadFromResources(MeshPackagingProfile.folderName, "AtlasedProjected"));
                meshPackagingSolutions.Add((new MeshPackagingProfile()).LoadFromResources(MeshPackagingProfile.folderName, "Standard_Atlased"));
            }

            if (samplingMaskSize == null) samplingMaskSize = new myIntVec2(4);

            if (materialsFolderName == null)
                materialsFolderName = "Materials";
            if (texturesFolderName == null)
                texturesFolderName = "Textures";
            if (vectorsFolderName == null)
                vectorsFolderName = "Vectors";
            if (meshesFolderName == null)
                meshesFolderName = "Models";
            if (atlasFolderName == null)
                atlasFolderName = "ATLASES";
            if (recordingNames == null)
                recordingNames = new List<string>();

        }

        public static void LoadOrInit() {

            if (texMGMT.painterCfg != null)
                _inst = texMGMT.painterCfg;

            if (_inst == null){
                _inst = new PainterConfig();
                texMGMT.painterCfg = _inst;
            }
            
            _inst.Init();
 
            _inst.recordingNames.AddResourceIfNew(_inst.texturesFolderName,_inst.vectorsFolderName);

            var encody = new stdDecoder(_inst.meshToolsSTD);
            foreach (var tag in encody.enumerator) {
                var d = encody.getData();
                foreach (var m in MeshToolBase.allTools)
                if (m.ToString().SameAs(tag)) {
                    m.Decode(d);
                    break;
                }
            }
        }
    
        public static void SaveChanges() {
            stdEncoder cody = new stdEncoder();
            if (!applicationIsQuitting) {
                foreach (var mt in MeshToolBase.allTools)
                    cody.Add(mt);
                _inst.meshToolsSTD = cody.ToString();
            }
        }
        
        public bool PEGI()
        {
            PainterManager rtp = PainterManager.inst;
            BrushConfig brush = PainterConfig.inst.brushConfig;
            bool changed = false;

            if (!isNowPlaytimeAndDisabled)
            {

              
                rtp.browsedPlugin = Mathf.Clamp(rtp.browsedPlugin, -1, rtp.plugins.Count - 1);

          
                if (rtp.browsedPlugin != -1)
                {
                    if (icon.Back.Click().nl())
                        rtp.browsedPlugin = -1;
                    else  {
                        var pl = rtp.plugins[rtp.browsedPlugin];
                        if (pl.ConfigTab_PEGI().nl())
                            pl.SetToDirty();

                        return changed;
                    }
                }
                else
                    for (int p = 0; p < rtp.plugins.Count; p++)
                    {
                        rtp.plugins[p].ToString().write();
                        if (icon.Edit.Click().nl()) rtp.browsedPlugin = p;
                    }

                if ("Find New Plugins".Click())
                    rtp.RefreshPlugins();

                if ("Clear Data".Click().nl())  {
                    rtp.DeletePlugins();
                    rtp.RefreshPlugins();
                }
                
            }
            pegi.newLine();

            bool gotDefine = pegi.GetDefine(PainterConfig.enablePainterForBuild);

            if ("Enable Painter for Playtime & Build".toggle(ref gotDefine).nl())
                pegi.SetDefine(PainterConfig.enablePainterForBuild, gotDefine);

            if (gotDefine) {
                if ("Enable PlayTime UI".toggle(ref cfg.enablePainterUIonPlay).nl())
                    MeshManager.inst.DisconnectMesh();
            }

            if (!isNowPlaytimeAndDisabled) {

                if (painter.meshEditing == false) {
                    if ("More options".toggle(80, ref moreOptions).nl())
                        showConfig = false;

                    "CPU blit repaint delay".nl("Delay for video memory update when painting to Texture2D", 140);

                    changed |= pegi.edit(ref brush.repaintDelay, 0.01f, 0.5f).nl();

                    changed |= "Don't update mipmaps:".toggle("May increase performance, but your changes may not disaplay if you are far from texture.", 150,
                        ref brush.DontRedoMipmaps).nl();

                    var id = painter.imgData;

                    if (id != null)
                        changed |= id.PEGI();


                    "Disable Non-Mesh Colliders in Play Mode:".toggle(ref disableNonMeshColliderInPlayMode).nl();



                  /*  "Camera".write(rtp.rtcam);
                    pegi.newLine();

                    "Brush".write(rtp.brushPrefab);
                    pegi.newLine();

                    "Renderer to Debug second buffer".edit(ref rtp.secondBufferDebug).nl();*/
                }

                "Teaching Notifications".toggle("will show whatever you ae pressing on the screen.",140, ref ShowTeachingNotifications).nl();

                "Save Textures To:".edit(110, ref texturesFolderName).nl();

                "_Atlas Textures Sub folder".edit(150, ref atlasFolderName).nl(); 

                "Save Materials To:".edit(110, ref materialsFolderName).nl();

                "Save Meshes To:".edit(110, ref meshesFolderName).nl();
            }
#if UNITY_EDITOR

            if (icon.Discord.Click("Join Discord", 64))
                PlaytimePainter.open_Discord();

            if (icon.Docs.Click("Open Asset Documentation", 64))
                PlaytimePainter.openWWW_Documentation();

            if (icon.Email.Click("Report a bug / send suggestion / ask question.", 64))
                PlaytimePainter.open_Email();

#endif

            return changed;
        }

        public PainterConfig() {
            brushConfig = new BrushConfig();
        }

    }
}