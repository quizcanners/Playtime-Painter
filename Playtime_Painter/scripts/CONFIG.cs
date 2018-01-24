using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Painter{

[Serializable]
public class painterConfig  {
    static painterConfig _inst;
    public static painterConfig inst {
            get
            {
                if (_inst == null)
                    LoadOrInit();

                return _inst;
            }
    }

    public const string PainterCameraName = "PainterCamera";
    public const string ToolName = "Playtime_Painter";
    public const string enablePainterForBuild = "BUILD_WITH_PAINTER";

    // Terrain namings:
    public const string terrainHeight = "_mergeTerrainHeight"; // Used in custom shaders
    public const string terrainControl = "_mergeControl"; // Used in Custom Terrain Shaders
    public const string terrainTexture = "_mergeSplat_";
    public const string terrainNormalMap = "_mergeSplatN_";
    public const string terrainLight = "_TerrainColors";
	public const string previewTexture = "_PreviewTex";


    static string SaveName = "PainterConfig";


 
        public static string SavedMeshesName = "SavedMeshes";
        public static int DamAnimRendtexSize = 128;
        public bool allowEditingInFinalBuild;
        public bool MakeVericesUniqueOnEdgeColoring;
        public int MaxDistanceForTransformPosition = 100;
        public int SnapToGridSize = 1;
        public int MeshUVprojectionSize = 1;
        public bool SnapToGrid = false;
        public int curAtlasTexture = 0;
        public int curAtlasChanel = 0;
        public bool newVerticesUnique = false;
        public bool newVerticesSmooth = true;
        public bool atlasEdgeAsChanel2 = true;

        public List<MeshSolutionProfile> meshProfiles;

        public MeshTool _meshTool;


        public static string ToolPath() {
		return PlaytimeToolComponent.ToolsFolder + "/" + ToolName;
    }

    public string materialsFolderName;
    public string texturesFolderName;
    public string meshesFolderName;
    public string vectorsFolderName;
    public string atlasFolderName;

    public bool disableGodMode = false;
    public BrushConfig brushConfig;
    public float GodWalkSpeed = 100f;
    public float GodLookSpeed = 10f;
    public bool dontCreateDefaultRenderTexture;
    public bool disableNonMeshColliderInPlayMode;
    public bool previewAlphaChanel;
        public bool newTextureIsColor = true;

        public bool moreOptions = false;
        public bool showConfig = false;

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

        void SafeInit()
        {

            if (meshProfiles == null) meshProfiles = new List<MeshSolutionProfile>();



            meshProfiles.Add((MeshSolutionProfile)(new MeshSolutionProfile().Reboot(
                ResourceLoader.LoadStoryFromResource(MeshSolutionProfile.folderName, "Standard"))));
            meshProfiles.Add((MeshSolutionProfile)(new MeshSolutionProfile().Reboot(ResourceLoader.LoadStoryFromResource(MeshSolutionProfile.folderName, "Atlased"))));


            //if (meshProfiles.Count == 0) meshProfiles.Add(new MeshSolutionProfile());
            if (texturesFolderName == null)
                texturesFolderName = "Textures";
            if (vectorsFolderName == null)
                vectorsFolderName = "Vectors";
            if (meshesFolderName == null)
                meshesFolderName = "Meshes";
            if (atlasFolderName == null)
                atlasFolderName = "ATLASES";
            if (recordingNames == null)
                recordingNames = new List<string>();
        }

    public static void LoadOrInit() {

        ResourceLoader<painterConfig> ld = new ResourceLoader<painterConfig>();

            if (!ld.LoadFrom(Application.persistentDataPath, SaveName, ref _inst))  {
                _inst = new painterConfig();
                _inst._meshTool = MeshTool.vertices;


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
        ResourceSaver.Save<painterConfig>(Application.persistentDataPath,SaveName, _inst);
    }

    public painterConfig() {
        brushConfig = new BrushConfig();
        materialsFolderName = "Materials";
            newTextureIsColor = true;
                

}

}
}