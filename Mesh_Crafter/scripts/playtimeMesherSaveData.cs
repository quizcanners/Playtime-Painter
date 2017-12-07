using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace MeshEditingTools {

    [Serializable]
    public class playtimeMesherSaveData
    {
        static playtimeMesherSaveData _inst;
        public static playtimeMesherSaveData inst()  {
            if (_inst == null)
                LoadOrInit();
            return _inst;
        }

		public static string ToolName = "Mesh_Crafter";
        static string SaveName = "MehserConfig";
        public static string SavedMeshesName = "SavedMeshes";
        public static int DamAnimRendtexSize = 128;

        [NonSerialized]
        public BrushConfig brushConfig;
        public bool allowEditingInFinalBuild;
        public bool MakeVericesUniqueOnEdgeColoring;
        public int MaxDistanceForTransformPosition = 100;
        public int SnapToGridSize = 1;
        public int MeshUVprojectionSize = 1;
        public bool SnapToGrid = false;
        public int curAtlasTexture = 0;
        public bool newVerticesSmooth = false;    

        [NonSerialized]
        public List<MeshSolutionProfile> meshProfiles;

    public  MeshTool _meshTool;

        public static void LoadOrInit() {
       
            ResourceLoader<playtimeMesherSaveData> ld = new ResourceLoader<playtimeMesherSaveData>();
            ld.LoadFrom(Application.persistentDataPath, SaveName, ref _inst);

        if (_inst == null) {
            _inst = new playtimeMesherSaveData();
            Debug.Log("Creating Mesh config file");
        }

        if (_inst.meshProfiles == null)  {
            _inst.meshProfiles = new List<MeshSolutionProfile>();
            _inst.meshProfiles.Add(new MeshSolutionProfile());
        }

        }

        public static void SaveChanges() {
            ResourceSaver.Save<playtimeMesherSaveData>(Application.persistentDataPath, SaveName, _inst);
    }

        public playtimeMesherSaveData(){
            brushConfig = new BrushConfig();
            _meshTool = MeshTool.vertices;
        }

    }
}