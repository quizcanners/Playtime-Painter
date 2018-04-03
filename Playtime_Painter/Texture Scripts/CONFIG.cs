using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using PlayerAndEditorGUI;
using StoryTriggerData;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Playtime_Painter{



[Serializable]
public class PainterConfig  {
    static PainterConfig _inst;
    public static PainterConfig inst {
            get
            {
                if (_inst == null)
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

    static string SaveName = "PainterConfig";


        public static int DamAnimRendtexSize = 128;
        public bool allowEditingInFinalBuild;
        public bool MakeVericesUniqueOnEdgeColoring;
      //  public int MaxDistanceForTransformPosition = 100;
        public int SnapToGridSize = 1;
        public int MeshUVprojectionSize = 1;
        public bool SnapToGrid = false;
        public int curAtlasTexture = 0;
        public int curSubmesh = 0;
        public int curAtlasChanel = 0;
        public bool newVerticesUnique = false;
        public bool newVerticesSmooth = true;
        public bool atlasEdgeAsChanel2 = true;
        public bool pixelPerfectMeshEditing = false;

        public List<MeshPackagingProfile> meshPackagingSolutions;
      

        public int _meshTool;
        public MeshToolBase meshTool { get {return MeshToolBase.allTools[_meshTool];} }
        public float bevelDetectionSensetivity = 6;

        public static string ToolPath() {
		return PlaytimeToolComponent.ToolsFolder + "/" + ToolName;
    }

    public string materialsFolderName;
    public string texturesFolderName;
    public string meshesFolderName;
    public string vectorsFolderName;
    public string atlasFolderName;

    public bool disablePainterUIonPlay = false;
    public bool disableGodMode = false;
    public BrushConfig brushConfig;
    public float GodWalkSpeed = 100f;
    public float GodLookSpeed = 10f;
   // public bool dontCreateDefaultRenderTexture;
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

    public void SafeInit() {

            _inst = this;

           // meshPackagingSolutions = null;

            if ((meshPackagingSolutions == null) || (meshPackagingSolutions.Count == 0)) {
                meshPackagingSolutions = new List<MeshPackagingProfile>();

				meshPackagingSolutions.Add((new MeshPackagingProfile()).LoadFromResources(MeshPackagingProfile.folderName, "Standard_Atlased"));
                meshPackagingSolutions.Add((new MeshPackagingProfile()).LoadFromResources(MeshPackagingProfile.folderName, "Bevel"));
				meshPackagingSolutions.Add((new MeshPackagingProfile()).LoadFromResources(MeshPackagingProfile.folderName, "AtlasedProjected"));
            }

            if (samplingMaskSize == null) samplingMaskSize = new myIntVec2(4);

           
           
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
          //  Debug.Log("Loading config ");
        ResourceLoader<PainterConfig> ld = new ResourceLoader<PainterConfig>();

            if (!ld.LoadFrom(Application.persistentDataPath, SaveName, ref _inst))  {
                _inst = new PainterConfig();
            }

            _inst.SafeInit();
            
        GodMode gm = GodMode.inst;
        if (gm != null) {
            gm.speed = _inst.GodWalkSpeed;
            gm.sensitivity = _inst.GodLookSpeed;
        }
        
        _inst.recordingNames.AddResourceIfNew(_inst.texturesFolderName,_inst.vectorsFolderName);

    }

    public static void SaveChanges() {
        ResourceSaver.Save<PainterConfig>(Application.persistentDataPath,SaveName, _inst);
    }

    

        public bool PEGI()
        {
            PainterManager rtp = PainterManager.inst;
            BrushConfig brush = PainterConfig.inst.brushConfig;
            bool changed = false;

            if (!PlaytimePainter.isNowPlaytimeAndDisabled())
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
                if ("Disable PlayTime UI".toggle(ref PainterConfig.inst.disablePainterUIonPlay).nl())
                    MeshManager.inst.DisconnectMesh();
            }

            if (!PlaytimePainter.isNowPlaytimeAndDisabled()) {

                if (painter.meshEditing == false) {
                    if ("More options".toggle(80, ref moreOptions).nl())
                        showConfig = false;

                    "CPU blit repaint delay".nl("Delay for video memory update when painting to Texture2D", 100);

                    changed |= pegi.edit(ref brush.repaintDelay, 0.01f, 0.5f).nl();

                    changed |= "Don't update mipmaps:".toggle("May increase performance, but your changes may not disaplay if you are far from texture.", 150,
                        ref brush.DontRedoMipmaps).nl();

                    bool gotBacups = (painter.numberOfTexture2Dbackups + painter.numberOfRenderTextureBackups) > 0;

                    if (gotBacups)
                    {
                        pegi.writeOneTimeHint("Creating more backups will eat more memory", "backupIsMem");
                        pegi.writeOneTimeHint("This are not connected to Unity's " +
                        "Undo/Redo because when you run out of backups you will by accident start undoing other stuff.", "noNativeUndo");
                        pegi.writeOneTimeHint("Use Z/X to undo/redo", "ZXundoRedo");

                        changed |=
                            "texture2D UNDOs:".edit(150, ref painter.numberOfTexture2Dbackups).nl() ||
                            "renderTex UNDOs:".edit(150, ref painter.numberOfRenderTextureBackups).nl() ||
                            "backup manually:".toggle(150, ref painter.backupManually).nl();
                    }
                    else if ("Enable Undo/Redo".Click().nl())
                    {
                        painter.numberOfTexture2Dbackups = 10;
                        painter.numberOfRenderTextureBackups = 10;
                    }

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
        materialsFolderName = "Materials";
            newTextureIsColor = true;
                

}

}
}