using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Painter{

[Serializable]
public class painterConfig  {
    static painterConfig _inst;
    public static painterConfig inst() {
        if (_inst == null)
            LoadOrInit();

        return _inst;
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

    public static string ToolPath() {
		return PlaytimeToolComponent.ToolsFolder + "/" + ToolName;
    }

    public string materialsFolderName;
    public string texturesFolderName;
    public string vectorsFolderName;


    public bool disableGodMode = false;
    public BrushConfig brushConfig;
    public float GodWalkSpeed = 100f;
    public float GodLookSpeed = 10f;
    public bool dontCreateDefaultRenderTexture;
    public bool allowEditingInFinalBuild;
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

    public static void LoadOrInit() {

        ResourceLoader<painterConfig> ld = new ResourceLoader<painterConfig>();

        if (!ld.LoadFrom(Application.persistentDataPath,SaveName, ref _inst))
            _inst = new painterConfig();
        

            _inst.texturesFolderName = "Textures";
            _inst.vectorsFolderName = "Vectors";

        GodMode gm = GodMode.inst;
        if (gm != null) {
            gm.speed = _inst.GodWalkSpeed;
            gm.sensitivity = _inst.GodLookSpeed;
        }

            if (_inst.recordingNames == null)
                _inst.recordingNames = new List<string>();
            
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