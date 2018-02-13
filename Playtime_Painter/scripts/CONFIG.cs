using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using PlayerAndEditorGUI;
using StoryTriggerData;

namespace Painter{



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

    public const string PainterCameraName = "PainterCamera";
    public const string ToolName = "Playtime_Painter";
    public const string enablePainterForBuild = "BUILD_WITH_PAINTER";

        // Terrain Global Shader namings:
    public const string terrainPosition = "_mergeTeraPosition";
    public const string terrainTiling = "_mergeTerrainTiling";
    public const string terrainScale = "_mergeTerrainScale";
    public const string terrainHeight = "_mergeTerrainHeight"; // Used in custom shaders
    public const string terrainControl = "_mergeControl"; // Used in Custom Terrain Shaders
    public const string terrainTexture = "_mergeSplat_";
    public const string terrainNormalMap = "_mergeSplatN_";
    public const string terrainLight = "_TerrainColors";
	public const string previewTexture = "_PreviewTex";

        

        // Preview Constants
        public const string UV_NORMAL = "UV_NORMAL";
        public const string UV_ATLASED = "UV_ATLASED";
        public const string UV_PROJECTED = "UV_PROJECTED";
        public const string UV_PIXELATED = "UV_PIXELATED";
        public const string EDGE_WIDTH_FROM_COL_A = "EDGE_WIDTH_FROM_COL_A";

        public const string atlasedTexturesInARow = "_AtlasTextures";

    static string SaveName = "PainterConfig";


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
        public bool pixelPerfectMeshEditing = false;

        public List<MeshSolutionProfile> meshProfileSolutions;

        public MeshTool _meshTool;


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

        public void SafeInit()
        {

            _inst = this;

            if ((meshProfileSolutions == null) || (meshProfileSolutions.Count == 0)) {
                meshProfileSolutions = new List<MeshSolutionProfile>();

				meshProfileSolutions.Add((new MeshSolutionProfile()).LoadFromResources(MeshSolutionProfile.folderName, "Standard"));
                meshProfileSolutions.Add((new MeshSolutionProfile()).LoadFromResources(MeshSolutionProfile.folderName, "Bevel"));
                //meshProfileSolutions.Add((new MeshSolutionProfile()).LoadFromResources(MeshSolutionProfile.folderName, "Atlased"));
				//meshProfileSolutions.Add((new MeshSolutionProfile()).LoadFromResources(MeshSolutionProfile.folderName, "AtlasedProjected"));
               
            }

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
            Debug.Log("Loading config");
        ResourceLoader<PainterConfig> ld = new ResourceLoader<PainterConfig>();

            if (!ld.LoadFrom(Application.persistentDataPath, SaveName, ref _inst))  {
                _inst = new PainterConfig();
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
        ResourceSaver.Save<PainterConfig>(Application.persistentDataPath,SaveName, _inst);
    }

        [SerializeField]
          bool showAtlasedMaterial = false;
        [SerializeField]
        bool showAtlases = false;
        [SerializeField]
        int browsedAtlas = -1;

        public bool PEGI(PlaytimePainter painter)
        {
            PainterManager rtp = PainterManager.inst;
            BrushConfig brush = PainterConfig.inst.brushConfig;
            imgData id = painter.curImgData;
            bool changed = false;

            if (painter.isAtlased)
            {

                "***** Atlased *****".nl();

                if ("Undo Atlasing".Click())
                {
					painter.getRenderer ().sharedMaterial = painter.preAtlasingMaterial;
					//painter.getRenderer ().sharedMaterial.DisableKeyword (PainterConfig.UV_ATLASED);

					if (painter.preAtlasingMesh != null)
                        painter.meshFilter.mesh = painter.preAtlasingMesh;
                    painter.meshSaveData = painter.preAtlasingSavedMesh;

                    painter.preAtlasingMaterial = null;
                    painter.preAtlasingMesh = null;

                }

                if ("Not Atlased".Click().nl()) {
                    painter.preAtlasingMaterial = null;
                }

                "In Atlas No:".edit(ref painter.inAtlasIndex).nl();
                "Textures In A Row".edit(ref painter.atlasRows).nl();
                "Original Material:".write(painter.preAtlasingMaterial);
                pegi.newLine();


            }
            else if ("Atlased Materials".foldout(ref showAtlasedMaterial).nl())
            {

               

                showAtlases = false;

                List<MaterialAtlases> am = rtp.atlasedMaterials;
                int sctdAM = painter.selectedAtlasedMaterial;

                if ((sctdAM > -1) && (sctdAM >= am.Count))
                    painter.selectedAtlasedMaterial = sctdAM = -1;

                if (sctdAM > -1)
                {
                    pegi.write(am[sctdAM].name);
                    if (icon.Close.Click(25).nl())
                        painter.selectedAtlasedMaterial = sctdAM = -1;
                    else
                        am[sctdAM].PEGI(painter);
                }
                else
                {
                    pegi.newLine();
                    for (int i = 0; i < rtp.atlasedMaterials.Count; i++)
                    {
                        if (icon.Delete.Click(25))
                            rtp.atlasedMaterials.RemoveAt(i);
                        else
                        {
                            pegi.edit(ref rtp.atlasedMaterials[i].name);
                            if (icon.Edit.Click(25).nl())
                                painter.selectedAtlasedMaterial = i;
                        }
                    }

                    if (icon.Add.Click(30))
                    {
                        var mat = new MaterialAtlases("new");
                        rtp.atlasedMaterials.Add(mat);
                        mat.originalMaterial = painter.getMaterial(true);
                        painter.usePreviewShader = false;
                        mat.OnChangeMaterial(painter);

                    }
                }





                return changed;
            }
            if ("Atlases".foldout(ref showAtlases))
            {

                if ((browsedAtlas > -1) && (browsedAtlas >= rtp.atlases.Count))
                    browsedAtlas = -1;

                pegi.newLine();

                if (browsedAtlas > -1)
                {
                    if (icon.Back.Click(25))
                        browsedAtlas = -1;
                    else
                        rtp.atlases[browsedAtlas].PEGI(painter);
                }
                else
                {
                    pegi.newLine();
                    for (int i = 0; i < rtp.atlases.Count; i++)
                    {
                        if (icon.Delete.Click(25))
                            rtp.atlases.RemoveAt(i);
                        else
                        {
                            pegi.edit(ref rtp.atlases[i].name);
                            if (icon.Edit.Click(25).nl())
                                browsedAtlas = i;
                        }
                    }

                    if (icon.Add.Click(30))
                        rtp.atlases.Add(new AtlasTextureCreator("new"));

                }




                return changed;
            }


            pegi.newLine();

            bool gotDefine = pegi.GetDefine(PainterConfig.enablePainterForBuild);

            if ("Enable Painter in Playtime for build".toggle(ref gotDefine).nl())
                pegi.SetDefine(PainterConfig.enablePainterForBuild, gotDefine);

            if (gotDefine)
                "Disable PlayTime UI".toggle(ref PainterConfig.inst.disablePainterUIonPlay).nl();


            (rtp.isLinearColorSpace ? "Project is Linear Color Space" : "Project is in Gamma Color Space").nl("Go to Build Settings to change. Linear gives more natural look");

            if (painter.meshEditing == false)
            {
                if ("More options".toggle(80, ref moreOptions).nl())
                    showConfig = false;

                "repaint delay".nl("Delay for video memory update when painting to Texture2D", 100);

                changed |= pegi.edit(ref brush.repaintDelay, 0.01f, 0.5f).nl();

                changed |= "Don't update mipmaps:".toggle("May increase performance, but your changes may not disaplay if you are far from texture.", 150,
                    ref brush.DontRedoMipmaps).nl();

                if ((id != null) && (brush.DontRedoMipmaps) && ("Redo Mipmaps".Click().nl()))
                    id.SetAndApply(true);

                bool gotBacups = (painter.numberOfTexture2Dbackups + painter.numberOfRenderTextureBackups) > 0;

                if (gotBacups) {
                    pegi.writeOneTimeHint("Creating more backups will eat more memory", "backupIsMem");
                    pegi.writeOneTimeHint("This are not connected to Unity's " +
                    "Undo/Redo because when you run out of backups you will by accident start undoing other stuff.", "noNativeUndo");
                    pegi.writeOneTimeHint("Use Z/X to undo/redo", "ZXundoRedo");

                    changed |= 
                        "texture2D UNDOs:".edit(150, ref painter.numberOfTexture2Dbackups).nl() ||
                        "renderTex UNDOs:".edit(150, ref painter.numberOfRenderTextureBackups).nl() ||
                        "backup manually:".toggle(150, ref painter.backupManually).nl();
                }
                else if ("Enable Undo/Redo".Click().nl()) {
                    painter.numberOfTexture2Dbackups = 10;
                    painter.numberOfRenderTextureBackups = 10;
                }

            /*    if ("Don't create render texture buffer:".toggle(ref dontCreateDefaultRenderTexture).nl()) {
                    PainterConfig.SaveChanges();
                    rtp.UpdateBuffersState();
                }*/

               

                "Disable Non-Mesh Colliders in Play Mode:".toggle(ref disableNonMeshColliderInPlayMode).nl();

             

                "Camera".write(rtp.rtcam);
                pegi.newLine();

                "Brush".write(rtp.brushPrefab);
                pegi.newLine();

                "Renderer to Debug second buffer".edit(ref rtp.secondBufferDebug).nl();
            }

            "Save Textures To:".edit(110, ref texturesFolderName).nl();

            "Save Materials To:".edit(110, ref materialsFolderName).nl();

            "Save Meshes To:".edit(110, ref meshesFolderName).nl();


            if (icon.Discord.Click("Join Discord", 64))
                PlaytimePainter.open_Discord();

            if (icon.Docs.Click("Open Asset Documentation", 64))
                PlaytimePainter.openWWW_Documentation();

            if (icon.Email.Click("Report a bug / send suggestion / ask question.", 64))
                PlaytimePainter.open_Email();
            
            return changed;
        }



        public PainterConfig() {
        brushConfig = new BrushConfig();
        materialsFolderName = "Materials";
            newTextureIsColor = true;
                

}

}
}