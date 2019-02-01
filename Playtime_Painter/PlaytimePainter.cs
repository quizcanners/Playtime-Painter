using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using System.IO;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace Playtime_Painter {

    [AddComponentMenu("Mesh/Playtime Painter")]
    [HelpURL(WWW_Manual)]
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class PlaytimePainter : MonoBehaviour, ISTD, IPEGI
    {

        #region StaticGetters

#if PEGI
        public static pegi.CallDelegate pluginsComponentPEGI;
#endif

        public static PainterBoolPlugin pluginsGizmoDraw;

        public static bool IsCurrentTool { get { return PainterDataAndConfig.toolEnabled; } set { PainterDataAndConfig.toolEnabled = value; } }

        private static PainterDataAndConfig Cfg => PainterCamera.Data;

        private static BrushConfig GlobalBrush => Cfg.brushConfig;

        public BrushType GlobalBrushType => GlobalBrush.Type(ImgData.TargetIsTexture2D());

        private static PainterCamera TexMgmt => PainterCamera.Inst;

        private static MeshManager MeshMgmt => MeshManager.Inst;

        protected static GridNavigator Grid => GridNavigator.Inst();

        public string ToolName => PainterDataAndConfig.ToolName;

        private bool NeedsGrid => this.NeedsGrid();

        public Texture ToolIcon => icon.Painter.GetIcon();


        #endregion

        #region Dependencies

        public Renderer meshRenderer;
        public SkinnedMeshRenderer skinnedMeshRenderer;
        public Terrain terrain;
        public TerrainCollider terrainCollider;
        public MeshFilter meshFilter;
        public MeshCollider meshCollider;
        public Texture2D terrainHeightTexture;
        [NonSerialized] public Mesh colliderForSkinnedMesh;

        public bool meshEditing = false;

        public int selectedMeshProfile = 0;
        public MeshPackagingProfile MeshProfile
        {
            get { 
                selectedMeshProfile = Mathf.Max(0, Mathf.Min(selectedMeshProfile, Cfg.meshPackagingSolutions.Count - 1)); 
                return Cfg.meshPackagingSolutions[selectedMeshProfile]; 
            }
        }

        public string meshNameHolder;

        public string savedMeshData;
        public Mesh meshDataSavedFor;
        public string SavedEditableMesh
        {
            get
            {

                if (meshDataSavedFor != this.GetMesh())
                    savedMeshData = null;

                if ((savedMeshData != null) && (savedMeshData.Length == 0))
                    savedMeshData = null;

                return savedMeshData;
            }
            set { meshDataSavedFor = this.GetMesh(); savedMeshData = value; }

        }

        public int selectedSubmesh = 0;
        public Material Material
        {
            get { return GetMaterial(); }
            set
            {

                if (meshRenderer && selectedSubmesh < meshRenderer.sharedMaterials.Length)
                {
                    var mats = meshRenderer.sharedMaterials;
                    mats[selectedSubmesh] = value;
                    meshRenderer.materials = mats;
                }
                else if (terrain)
                {
                    terrain.materialTemplate = value;
                    terrain.materialType = value ? Terrain.MaterialType.Custom : Terrain.MaterialType.BuiltInStandard;
                }
            }
        }

        public MaterialData MatDta => Material.GetMaterialData();

        public ImageData ImgData => GetTextureOnMaterial().GetImgData();

        public string nameHolder = "unnamed";

        public int selectedAtlasedMaterial = -1;

        [NonSerialized] public List<PainterComponentPluginBase> plugins;

        [NonSerialized] private PainterComponentPluginBase _lastFetchedPlugin;
        public T GetPlugin<T>() where T : PainterComponentPluginBase
        {

            T returnPlug = null;

            if (_lastFetchedPlugin != null && _lastFetchedPlugin.GetType() == typeof(T))
                returnPlug = (T)_lastFetchedPlugin;
            else
                foreach (var p in plugins)
                    if (p.GetType() == typeof(T))
                        returnPlug = (T)p;

            _lastFetchedPlugin = returnPlug;

            return returnPlug;
        }

        private int SelectedTexture
        {
            get { var md = MatDta; return md == null ? 0 : md._selectedTexture; }
            set { var md = MatDta; if (md != null) md._selectedTexture = value; }
        }

        #endregion

        #region Painting

        public StrokeVector stroke = new StrokeVector();

        public static PlaytimePainter currentlyPaintedObjectPainter;
        private static PlaytimePainter _lastMouseOverObject;

        public PlaytimePainter Paint(StrokeVector stroke, BrushConfig brush) => brush.Paint(stroke, this);

#if BUILD_WITH_PAINTER
        private double _mouseBttnTime = 0;

        public void OnMouseOver() {

            if (pegi.MouseOverUI) {
                stroke.mouseDwn = false;
                return;
            }

            stroke.mouseUp = Input.GetMouseButtonUp(0);
            stroke.mouseDwn = Input.GetMouseButtonDown(0);

            if (Input.GetMouseButtonDown(1))
                _mouseBttnTime = Time.time;
            if ((Input.GetMouseButtonUp(1)) && ((Time.time - _mouseBttnTime) < 0.2f))
                FocusOnThisObject();

            if (!CanPaint())
                return;

            CheckPreviewShader();

            Vector3 mousePos = Input.mousePosition;

            if (!CastRayPlaytime(stroke, mousePos)) return;
            

                bool Cntr_Down = (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));

                ProcessMouseGrag(Cntr_Down);

                if (!((Input.GetMouseButton(0) || (stroke.mouseUp)) && (!Cntr_Down))) return;
            

                if (currentlyPaintedObjectPainter != this)
                {
                    currentlyPaintedObjectPainter = this;
                    stroke.SetPreviousValues();
                    FocusOnThisObject();
                }

                if ((!stroke.mouseDwn) || (CanPaintOnMouseDown(GlobalBrush)))
                    GlobalBrush.Paint(stroke, this);
                else RecordingMgmt();

                if (stroke.mouseUp)
                    currentlyPaintedObjectPainter = null;
            
            
        }

#endif

#if UNITY_EDITOR
        public void OnMouseOver_SceneView(RaycastHit hit, Event e)
        {

            if (!CanPaint())
                return;

            if (NeedsGrid)//globalBrush.type(this).needsGrid) 
                ProcessGridDrag();
            else
            if (!ProcessHit(hit, stroke))
                return;

            if ((currentlyPaintedObjectPainter != this) && (stroke.mouseDwn))
            {
                stroke.firstStroke = true;
                currentlyPaintedObjectPainter = this;
                FocusOnThisObject();
                stroke.uvFrom = stroke.uvTo;
            }

            var control = Event.current != null ? Event.current.control : false;

            ProcessMouseGrag(control);

            if ((currentlyPaintedObjectPainter == this))
            {

                if ((!stroke.mouseDwn) || CanPaintOnMouseDown(GlobalBrush))
                {

                    GlobalBrush.Paint(stroke, this);

                    Update();
                }
                else
                    RecordingMgmt();

            }

            if (currentlyPaintedObjectPainter != this)
                currentlyPaintedObjectPainter = null;

            stroke.mouseDwn = false;

        }
#endif

        private bool CanPaint()
        {

            if (!IsCurrentTool) return false;

            _lastMouseOverObject = this;

            if (LockTextureEditing)
                return false;

            if (IsTerrainHeightTexture && IsOriginalShader)
                return false;

            if ((stroke.mouseDwn) || (stroke.mouseUp))
                InitIfNotInitialized();

            if (ImgData != null) return true;
            
#if PEGI
            if (stroke.mouseDwn)
                "No texture to edit".showNotificationIn3D_Views();
#endif

                return false;
            
        }

        private readonly ChillLogger _logger = new ChillLogger("");

        private bool CastRayPlaytime(StrokeVector st, Vector3 mousePos)
        {

            if (!Camera.main)
            {
                _logger.Log_Interval(2, "No Main Camera to Raycast from", true, this);
                return false;
            }

            if (NeedsGrid)
            {
                ProcessGridDrag();
                return true;
            }
            
            RaycastHit hit;
            return Physics.Raycast(Camera.main.ScreenPointToRay(mousePos), out hit, float.MaxValue)
                ? ProcessHit(hit, st)
                : false;
        }

        private void ProcessGridDrag()
        {
            stroke.posTo = GridNavigator.onGridPos;
            PreviewShader_StrokePosition_Update();
        }

        private bool ProcessHit(RaycastHit hit, StrokeVector st)
        {

            var submesh = this.GetMesh().GetSubmeshNumber(hit.triangleIndex);
            if (submesh != selectedSubmesh)
            {
                if (autoSelectMaterialByNumberOfPointedSubMesh)
                {
                    SetOriginalShaderOnThis();

                    selectedSubmesh = submesh;
                    OnChangedTexture_OnMaterial();

                    CheckPreviewShader();
                }
            }

            if (ImgData == null) return false;

            st.posTo = hit.point;

            st.unRepeatedUV = OffsetAndTileUv(hit);
            st.uvTo = st.unRepeatedUV.To01Space();


            PreviewShader_StrokePosition_Update();

            return true;
        }

        private Vector2 OffsetAndTileUv(RaycastHit hit)
        {
            var id = ImgData;

            if (id == null) return hit.textureCoord;

            var uv = id.useTexcoord2 ? hit.textureCoord2 : hit.textureCoord;

            foreach (var p in plugins)
                if (p.OffsetAndTileUV(hit, this, ref uv))
                    return uv;

            uv.Scale(id.tiling);
            uv += id.offset;

            return uv;
        }

        private void ProcessMouseGrag(bool control)
        {

            if (stroke.mouseDwn)
            {
                stroke.firstStroke = true;
                stroke.SetPreviousValues();
            }

            if (control)
            {
                if (!stroke.mouseDwn) return;
                
                SampleTexture(stroke.uvTo);
                currentlyPaintedObjectPainter = null;
                
            }
            else
            {

                var id = ImgData;
                
                if (id == null) return;
                
                if (stroke.mouseDwn)
                    id.Backup();

                if (IsTerrainHeightTexture && stroke.mouseUp)
                    Preview_To_UnityTerrain();
                
            }
        }

        public void SampleTexture(Vector2 uv)
        {
            GlobalBrush.colorLinear.From(ImgData.SampleAT(uv), GlobalBrush.mask);
            Update_Brush_Parameters_For_Preview_Shader();
        }

        public void AfterStroke(StrokeVector st)
        {

            st.SetPreviousValues();
            st.firstStroke = false;
            st.mouseDwn = false;
#if UNITY_EDITOR || BUILD_WITH_PAINTER
            if (ImgData.TargetIsTexture2D())
                ImgData.pixelsDirty = true;
#endif
        }

        private bool CanPaintOnMouseDown(BrushConfig br) =>
             (ImgData.TargetIsTexture2D() || GlobalBrushType.StartPaintingTheMomentMouseIsDown);
        

        #endregion

        #region PreviewMGMT

        public static Material previewHolderMaterial;
        public static Shader previewHolderOriginalShader;

        public bool IsOriginalShader => !previewHolderMaterial || previewHolderMaterial != Material; 

        private  void CheckPreviewShader()
        {
            if (MatDta == null)
                return;
            if ((!IsCurrentTool) || (LockTextureEditing && !IsEditingThisMesh))
                SetOriginalShaderOnThis();
            else if (MatDta.usePreviewShader && IsOriginalShader)
                SetPreviewShader();
        }

        private  void SetPreviewShader()
        {
            var mat = Material;

            if (previewHolderMaterial)
            {
                if (previewHolderMaterial != mat)
                    SetOriginalShader();
                else
                    return;
            }

            if ((meshEditing) && (MeshMgmt.target != this))
                return;

            var tex = ImgData.CurrentTexture();

            if (!tex && !meshEditing)
            {
                MatDta.usePreviewShader = false;
                return;
            }

            if (!mat)
            {
                InstantiateMaterial(false);
                return;
            }

            Shader shd = null;

            if (meshEditing)
                shd = Cfg.mesh_Preview;
            else
            {
                if (terrain) shd = Cfg.TerrainPreview;
                else
                {

                    foreach (var pl in TexMgmt.Plugins)
                    {
                        var ps = pl.GetPreviewShader(this);
                        if (!ps) continue;
                         
                        shd = ps; 
                        break; 
                    }

                    if (!shd)
                        shd = Cfg.br_Preview;
                }
            }

            if (!shd)
                Debug.Log("Preview shader not found");
            else
            {
                previewHolderOriginalShader = mat.shader;
                previewHolderMaterial = mat;

                mat.shader = shd;

                if (tex && !meshEditing)
                    SetTextureOnPreview(tex);

                MatDta.usePreviewShader = true;

                Update_Brush_Parameters_For_Preview_Shader();
            }
        }

        public void SetOriginalShaderOnThis()
        {
            if (previewHolderMaterial && previewHolderMaterial == Material)
                SetOriginalShader();
        }

        public static void SetOriginalShader()
        {
            if (!previewHolderMaterial) return;
            
            previewHolderMaterial.shader = previewHolderOriginalShader;
            previewHolderOriginalShader = null;
            previewHolderMaterial = null;
            
        }

        #endregion

        #region  Texture MGMT 

        private void UpdateTylingFromMaterial()
        {

            var id = ImgData;

            var fieldName = GetMaterialTexturePropertyName;
            var mat = Material;
            if (!IsOriginalShader && !terrain)
            {
                id.tiling = mat.GetTextureScale(PainterDataAndConfig.previewTexture);
                id.offset = mat.GetTextureOffset(PainterDataAndConfig.previewTexture);
                return;
            }

            foreach (var nt in plugins)
                if (nt.UpdateTylingFromMaterial(fieldName, this))
                    return;

            if (!mat || fieldName == null || id == null) return;
            id.tiling = mat.GetTextureScale(fieldName);
            id.offset = mat.GetTextureOffset(fieldName);
        }

        public void UpdateTylingToMaterial()
        {
            var id = ImgData;
            var fieldName = GetMaterialTexturePropertyName;
            var mat = Material;
            if (!IsOriginalShader && !terrain)
            {
                mat.SetTextureScale(PainterDataAndConfig.previewTexture, id.tiling);
                mat.SetTextureOffset(PainterDataAndConfig.previewTexture, id.offset);
                return;
            }

            foreach (var nt in plugins)
                if (nt.UpdateTylingToMaterial(fieldName, this))
                    return;

            if (!mat || fieldName == null || id == null) return;
            mat.SetTextureScale(fieldName, id.tiling);
            mat.SetTextureOffset(fieldName, id.offset);
        }

        private  void OnChangedTexture_OnMaterial()
        {
            if (IsOriginalShader || !terrain)
                ChangeTexture(GetTextureOnMaterial());
        }

        public void ChangeTexture(ImageData id) => ChangeTexture(id.CurrentTexture());
        

        private  void ChangeTexture(Texture texture)
        {

            textureWasChanged = false;

#if UNITY_EDITOR
            if ((texture) && (texture.GetType() == typeof(Texture2D)))
            {
                var t2D = (Texture2D)texture;
                var imp = t2D.GetTextureImporter();
                if (imp)
                {

                    var assetPath = AssetDatabase.GetAssetPath(texture);
                    var extension = assetPath.Substring(assetPath.LastIndexOf(".") + 1);

                    if (extension != "png")
                    {
                        #if PEGI
                        "Converting {0} to .png".F(assetPath).showNotificationIn3D_Views();
                        #endif
                        texture = t2D.CreatePngSameDirectory(texture.name);
                    }

                }
            }
            #endif

            var field = GetMaterialTexturePropertyName;

            if (!texture)
            {
                RemoveTextureFromMaterial(); //SetTextureOnMaterial((Texture)null);
                return;
            }

            var id = texture.GetImgDataIfExists();

            if (id == null)
            {
                id = new ImageData().Init(texture);
                id.useTexcoord2 = Material.DisplayNameContains(field, PainterDataAndConfig.isUV2DisaplyNameTag);
            }

            SetTextureOnMaterial(texture);

            UpdateOrSetTexTarget(id.destination);

            UpdateTylingFromMaterial();

        }

        public PlaytimePainter SetTexTarget(BrushConfig br)
        {
            if (ImgData.TargetIsTexture2D() != br.TargetIsTex2D)
                UpdateOrSetTexTarget(br.TargetIsTex2D ? TexTarget.Texture2D : TexTarget.RenderTexture);

            return this;
        }

        public void UpdateOrSetTexTarget(TexTarget dst)
        {

            InitIfNotInitialized();

            var id = ImgData;

            if (id == null)
                return;

            if (id.destination == dst)
                return;

            id.ChangeDestination(dst, GetMaterial(true).GetMaterialData(), GetMaterialTexturePropertyName, this);
            CheckPreviewShader();

        }

        private  void ReEnableRenderTexture()
        {
            if (LockTextureEditing) return;
            
            OnEnable();

            OnChangedTexture_OnMaterial();

            if (ImgData != null)
                UpdateOrSetTexTarget(TexTarget.RenderTexture); // set it to Render Texture
            
        }

        private  void CreateTerrainHeightTexture(string newName)
        {

            var field = GetMaterialTexturePropertyName;

            if (field != PainterDataAndConfig.terrainHeight)
            {
                Debug.Log("Terrain height is not currently selected.");
                return;
            }

            var size = terrain.terrainData.heightmapResolution - 1;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);

            var id = ImgData;

            if (id != null)
                id.From(texture);
            else
                ChangeTexture(texture);

            id = ImgData;

            id.SaveName = newName;
            texture.name = id.SaveName;
            texture.Apply(true, false);

            SetTextureOnMaterial(texture);

            Unity_To_Preview();
            id.SetAndApply(false);

            texture.wrapMode = TextureWrapMode.Repeat;

#if UNITY_EDITOR
            SaveTextureAsAsset(false);

            var importer = id.texture2D.GetTextureImporter();
            var needReimport = importer.WasNotReadable();
            needReimport |= importer.WasWrongIsColor(false);
            if (needReimport) importer.SaveAndReimport();
#endif

            SetTextureOnMaterial(id.texture2D);
            UpdateShaderGlobals();
        }

        private  void CreateTexture2D(int size, string textureName, bool isColor)
        {

            var id = ImgData;

            var gotRenderTextureData = id != null && size == id.width && size == id.width && id.TargetIsRenderTexture();

            var texture = new Texture2D(size, size, TextureFormat.ARGB32, true, !isColor);

            if (gotRenderTextureData && (!id.texture2D || textureName.SameAs(id.SaveName)))
                id.texture2D = texture;

            texture.wrapMode = TextureWrapMode.Repeat;

            ChangeTexture(texture);

            id = ImgData;

            id.SaveName = textureName;
            texture.name = textureName;

            var needsFullUpdate = false;

            var needsReColorizing = false;

            var colorData = isColor ? Cfg.newTextureClearNonColorValue : Cfg.newTextureClearColor;

            if (gotRenderTextureData)
                id.RenderTexture_To_Texture2D();
            else
            {
                 needsReColorizing |= id.Colorize(colorData, true);
                 needsFullUpdate = true;
            }

            if (needsFullUpdate)
                id.SetPixelsInRAM();

#if UNITY_EDITOR
            SaveTextureAsAsset(true);

            var importer = id.texture2D.GetTextureImporter();

            var needReimport = importer.WasNotReadable();
            needReimport |= importer.WasWrongIsColor(isColor);

            if (needReimport) importer.SaveAndReimport();

            if (needsReColorizing) {
                id.Colorize(colorData);
                id.SetAndApply();
            }
#endif

            ImgData.Apply_ToGPU();

        }

        private  void CreateRenderTexture(int size, string name)
        {
            var previous = ImgData;

            var nt = new ImageData().Init(size);

            nt.SaveName = name;

            ChangeTexture(nt.renderTexture);

            PainterCamera.Inst.Render(previous.CurrentTexture(), nt);

            UpdateOrSetTexTarget(TexTarget.RenderTexture);

        }

        #endregion

        #region Material MGMT

        public Material[] GetMaterials()
        {

            if (!terrain)   return meshRenderer.sharedMaterials;
            
            var mat = Material;

            return mat ? new Material[] {mat} : null;

        }

        public List<string> GetMaterialsNames()
        {

            var ms = new List<string>();

            var mats = GetMaterials();

            for (var i = 0; i < mats.Length; i++) {
                var mt = mats[i];
                 ms.Add(mt ? mt.name : "Null material {0}".F(i));
            }
            
            return ms;
        }

        private List<string> GetMaterialTextureNames()
        {

            #if UNITY_EDITOR

            if (MatDta == null)
                return new List<string>();

            if (!IsOriginalShader)
                return MatDta.materials_TextureFields;

            MatDta.materials_TextureFields.Clear();

            if (plugins!= null)
            foreach (var nt in plugins)
                nt.GetNonMaterialTextureNames(this, ref MatDta.materials_TextureFields);

            if (!terrain)
                MatDta.materials_TextureFields = Material.MyGetTextureProperties();
            else
            {
                var tmp = Material.MyGetTextureProperties();

                foreach (var t in tmp)
                    if ((!t.Contains("_Splat")) && (!t.Contains("_Normal")))
                        MatDta.materials_TextureFields.Add(t);
                
            }
#endif

            return MatDta.materials_TextureFields;
        }

        public string GetMaterialTexturePropertyName => GetMaterialTextureNames().TryGet(SelectedTexture);

        private Texture GetTextureOnMaterial()
        {

            if (!IsOriginalShader)
            {
                if (meshEditing) return null;
                if (!terrain)
                {
                    var m = Material;
                    return m ? Material.GetTexture(PainterDataAndConfig.previewTexture) : null;
                }
            }

            var fieldName = GetMaterialTexturePropertyName;

            if (fieldName == null)
                return null;

            if (!plugins.IsNullOrEmpty())
                foreach (var t in plugins)
                {
                    Texture tex = null;
                    if (t.GetTexture(fieldName, ref tex, this))
                        return tex;
                }

            return Material.GetTexture(fieldName);
        }

        private Material GetMaterial(bool original = false)
        {

            Material result = null;

            if (original)
                SetOriginalShader();

            if (!meshRenderer)
                result = terrain ? terrain.materialTemplate : null;
            else
                if (meshRenderer.sharedMaterials.ClampIndexToLength(ref selectedSubmesh))
                result = meshRenderer.sharedMaterials[selectedSubmesh];

            return result;
        }

        private void RemoveTextureFromMaterial() => SetTextureOnMaterial(GetMaterialTexturePropertyName, null);

        public void SetTextureOnMaterial(ImageData id) => SetTextureOnMaterial(GetMaterialTexturePropertyName, id.CurrentTexture());

        public ImageData SetTextureOnMaterial(Texture tex) => SetTextureOnMaterial(GetMaterialTexturePropertyName, tex);

        private ImageData SetTextureOnMaterial(string fieldName, Texture tex)
        {
            var id = SetTextureOnMaterial(fieldName, tex, GetMaterial(true));
            CheckPreviewShader();
            return id;
        }

        public ImageData SetTextureOnMaterial(string fieldName, Texture tex, Material mat)
        {

            var id = tex.GetImgData();

            if (fieldName != null)
            {
                if (id != null)
                    Cfg.recentTextures.AddIfNew(fieldName, id);

                foreach (var nt in plugins)
                    if (nt.SetTextureOnMaterial(fieldName, id, this))
                        return id;
            }

            if (mat) {

                if (fieldName != null)
                    mat.SetTexture(fieldName, id.CurrentTexture());

                if (!IsOriginalShader && (!terrain))
                    SetTextureOnPreview(id.CurrentTexture());
            }
            
            return id;
        }

        private void SetTextureOnPreview(Texture tex)
        {

            if (meshEditing) return;
        
            var mat = Material;
            var id = tex.GetImgData();

            mat.SetTexture(PainterDataAndConfig.previewTexture, id.CurrentTexture());
            
            if (id == null) return;
            
            mat.SetTextureOffset(PainterDataAndConfig.previewTexture, id.offset);
            mat.SetTextureScale(PainterDataAndConfig.previewTexture, id.tiling);
               
        }

        public Material InstantiateMaterial(bool saveIt)
        {

            SetOriginalShader();

            if (ImgData != null && Material)
                UpdateOrSetTexTarget(TexTarget.Texture2D);

            if (!TexMgmt.defaultMaterial) InitIfNotInitialized();

            var mat = GetMaterial(true);

            if (!mat && terrain)
            {
                mat = new Material(Cfg.TerrainPreview);

                terrain.materialTemplate = mat;
                terrain.materialType = Terrain.MaterialType.Custom;
                gameObject.name += "_Terrain material";
            }
            else
            {
                if (mat)
                    gameObject.name = mat.name;

                Material = Instantiate(mat ? mat : TexMgmt.defaultMaterial);
                CheckPreviewShader();
            }

            Material.name = gameObject.name;

            if (saveIt)
            {
#if UNITY_EDITOR
                var fullPath = Path.Combine(Application.dataPath, Cfg.materialsFolderName);
                Directory.CreateDirectory(fullPath);

                var material = Material;
                var path = material.SetUniqueObjectName(Cfg.materialsFolderName, ".mat");

                if (material)
                {
                    AssetDatabase.CreateAsset(material, path);
                    AssetDatabase.Refresh();
                    CheckPreviewShader();
                }
#endif
            }

            OnChangedTexture_OnMaterial();

            var id = ImgData;

            if (id != null && Material)
                UpdateOrSetTexTarget(id.destination);
#if PEGI
            "Instantiating Material on {0}".F(gameObject.name).showNotificationIn3D_Views();
#endif
            return Material;


        }

        #endregion

        #region Terrain_MGMT

        private float _tilingY = 8;

        public void UpdateShaderGlobals()
        {

            foreach (var nt in plugins)
                nt.OnUpdate(this);

            if (!terrain) return;

#if UNITY_2018_3_OR_NEWER
            var sp = terrain.terrainData.terrainLayers;
#else
            SplatPrototype[] sp = terrain.terrainData.splatPrototypes;
#endif

            var td = terrain.terrainData;
            var tds = td.size;
            
            if (sp.Length != 0)
            {
                float tilingX = tds.x / sp[0].tileSize.x;
                float tilingZ = tds.z / sp[0].tileSize.y;
                Shader.SetGlobalVector(PainterDataAndConfig.terrainTiling, new Vector4(tilingX, tilingZ, sp[0].tileOffset.x, sp[0].tileOffset.y));

                _tilingY = td.size.y / sp[0].tileSize.x;
            }
            
            Shader.SetGlobalVector(PainterDataAndConfig.terrainScale, new Vector4(tds.x, tds.y, tds.z, 0.5f / ((float)td.heightmapResolution)));

            UpdateTerrainPosition();

            Texture[] alphamapTextures = td.alphamapTextures;
            if (alphamapTextures.Length > 0)
                Shader.SetGlobalTexture(PainterDataAndConfig.terrainControl, alphamapTextures[0].GetDestinationTexture());

        }

        private void UpdateTerrainPosition() => Shader.SetGlobalVector(PainterDataAndConfig.terrainPosition, transform.position.ToVector4(_tilingY));

        private void Preview_To_UnityTerrain()
        {

            var id = ImgData;

            if (id == null)
                return;

            var rendTex = id.TargetIsRenderTexture();

            if (rendTex)
                UpdateOrSetTexTarget(TexTarget.Texture2D);

            var td = terrain.terrainData;

            var res = td.heightmapResolution - 1;

            var conversion = ((float)id.width / (float)res);

            var heights = td.GetHeights(0, 0, res + 1, res + 1);

            var cols = id.Pixels;

            if (conversion != 1)
                for (var y = 0; y < res; y++)
                {
                    var yind = id.width * Mathf.FloorToInt((y * conversion));
                    for (var x = 0; x < res; x++)
                        heights[y, x] = cols[yind + (int)(x * conversion)].a;

                }
            else
                for (var y = 0; y < res; y++)
                {
                    var yind = id.width * y;

                    for (var x = 0; x < res; x++)
                        heights[y, x] = cols[yind + x].a;
                }

            for (var y = 0; y < res - 1; y++)
                heights[y, res] = heights[y, res - 1];
            for (var x = 0; x < res; x++)
                heights[res, x] = heights[res - 1, x];

            terrain.terrainData.SetHeights(0, 0, heights);

            UpdateShaderGlobals();

            if (rendTex)
                UpdateOrSetTexTarget(TexTarget.RenderTexture);
        }

        private void Unity_To_Preview()
        {

            var oid = ImgData;

            var id = terrainHeightTexture.GetImgData();

            var current = id == oid;
            var rendTex = current && oid.TargetIsRenderTexture();

            if (rendTex)
                UpdateOrSetTexTarget(TexTarget.Texture2D);

            var textureSize = terrain.terrainData.heightmapResolution - 1;

            if (id.width != textureSize)
            {
                Debug.Log("Wrong size: {0} textureSize {1}".F(id.width, id.texture2D.width));
                if (current)
                {
                    CreateTerrainHeightTexture(oid.SaveName);
                    id = oid;
                }
                else Debug.Log("Is not current");

                return;
            }

            terrainHeightTexture = id.texture2D;
            var col = id.Pixels;

            var height = 1f / ((float)terrain.terrainData.size.y);

            var td = terrain.terrainData;
            
            for (var y = 0; y < textureSize; y++)
            {
                var fromY = y * textureSize;

                for (var x = 0; x < textureSize; x++)
                {
                    var tmpcol = new Color();

                    var dx = ((float)x) / textureSize;
                    var dy = ((float)y) / textureSize;

                    var v3 = td.GetInterpolatedNormal(dx, dy);// + Vector3.one * 0.5f;

                    tmpcol.r = v3.x + 0.5f;
                    tmpcol.g = v3.y + 0.5f;
                    tmpcol.b = v3.z + 0.5f;
                    tmpcol.a = td.GetHeight(x, y) * height;

                    col[fromY + x] = tmpcol;
                }
            }

            terrainHeightTexture.SetPixels(col);
            terrainHeightTexture.Apply(true, false);

            if (current)
                OnChangedTexture_OnMaterial();

            if (rendTex)
                UpdateOrSetTexTarget(TexTarget.RenderTexture);

            UpdateShaderGlobals();
        }

        public bool IsTerrainHeightTexture
        {
            get
            {
                if (!terrain)
                    return false;
                var name = GetMaterialTexturePropertyName;
                return name == null ? false :
                 name.Contains(PainterDataAndConfig.terrainHeight);
            }
        }

        public TerrainHeight GetTerrainHeight()
        {

            foreach (var nt in plugins)
                if (nt.GetType() == typeof(TerrainHeight))
                    return ((TerrainHeight)nt);
            
            return null;

        }

        public bool IsTerrainControlTexture => ImgData != null && terrain && GetMaterialTexturePropertyName.Contains(PainterDataAndConfig.terrainControl);

        #endregion

        #region Playback & Recoding

        public static readonly List<PlaytimePainter> playbackPainters = new List<PlaytimePainter>();

        public List<string> playbackVectors = new List<string>();

        public static StdDecoder cody = new StdDecoder("");

        private void PlayByFilename(string recordingName)
        {
            if (!playbackPainters.Contains(this))
                playbackPainters.Add(this);
            StrokeVector.PausePlayback = false;
            playbackVectors.AddRange(Cfg.StrokeRecordingsFromFile(recordingName));
        }

        public void PlaybackVectors()
        {
            if (cody.GotData)
                Decode(cody.GetTag(), cody.GetData());
            else
            {
                if (playbackVectors.Count > 0)
                {
                    cody = new StdDecoder(playbackVectors[0]);
                    playbackVectors.RemoveAt(0);
                }
                else
                    playbackPainters.Remove(this);
            }

        }

        private Vector2 _prevDir;
        private Vector2 _lastUv;
        private Vector3 _prevPosDir;
        private Vector3 _lastPos;

        private float _strokeDistance;

        public void RecordingMgmt()
        {
            var curImgData = ImgData;

            if (!curImgData.recording) return;
            

                if (stroke.mouseDwn)
                {
                    _prevDir = Vector2.zero;
                    _prevPosDir = Vector3.zero;
                }

                var canRecord = stroke.mouseDwn || stroke.mouseUp;

                var worldSpace = GlobalBrush.IsA3Dbrush(this);

                if (!canRecord)
                {

                    var size = GlobalBrush.Size(worldSpace);

                    if (worldSpace)
                    {
                        var dir = stroke.posTo - _lastPos;

                        var dot = Vector3.Dot(dir.normalized, _prevPosDir);

                        canRecord |= (_strokeDistance > size * 10) ||
                            ((dir.magnitude > size * 0.01f) && (_strokeDistance > size) && (dot < 0.9f));

                        var fullDist = _strokeDistance + dir.magnitude;

                        _prevPosDir = (_prevPosDir * _strokeDistance + dir).normalized;

                        _strokeDistance = fullDist;

                    }
                    else
                    {

                        size /= ((float)curImgData.width);

                        var dir = stroke.uvTo - _lastUv;

                        var dot = Vector2.Dot(dir.normalized, _prevDir);

                        canRecord |= (_strokeDistance > size * 5) || (_strokeDistance * (float)curImgData.width > 10) ||
                            ((dir.magnitude > size * 0.01f) && (dot < 0.8f));


                        var fullDist = _strokeDistance + dir.magnitude;

                        _prevDir = (_prevDir * _strokeDistance + dir).normalized;

                        _strokeDistance = fullDist;

                    }
                }

                if (canRecord)
                {

                    var hold = stroke.uvTo;
                    var holdv3 = stroke.posTo;

                    if (!stroke.mouseDwn)
                    {
                        stroke.uvTo = _lastUv;
                        stroke.posTo = _lastPos;
                    }

                    _strokeDistance = 0;

                    string data = Encode().ToString();
                    curImgData.recordedStrokes.Add(data);
                    curImgData.recordedStrokes_forUndoRedo.Add(data);

                    if (!stroke.mouseDwn)
                    {
                        stroke.uvTo = hold;
                        stroke.posTo = holdv3;
                    }

                }

                _lastUv = stroke.uvTo;
                _lastPos = stroke.posTo;

            
        }

        public StdEncoder Encode()
        {
            var encoder = new StdEncoder();

            var id = ImgData;

            if (stroke.mouseDwn)
            {
                encoder.Add("brush", GlobalBrush.EncodeStrokeFor(this)) // Brush is unlikely to change mid stroke
                .Add_String("trg", id.TargetIsTexture2D() ? "C" : "G");
            }

            encoder.Add("s", stroke.Encode(id.TargetIsRenderTexture() && GlobalBrush.IsA3Dbrush(this)));

            return encoder;
        }

        public bool Decode(string tag, string data)
        {

            switch (tag)
            {
                case "trg": UpdateOrSetTexTarget(data.Equals("C") ? TexTarget.Texture2D : TexTarget.RenderTexture); break;
                case "brush":
                    var id = ImgData;
                    GlobalBrush.Decode(data);
                    GlobalBrush.Brush2D_Radius *= id == null ? 256 : id.width; break;
                case "s":
                    stroke.Decode(data);
                    GlobalBrush.Paint(stroke, this);
                    break;
                default: return false;
            }
            return true;
        }

        public void Decode(string data) => data.DecodeTagsFor(this);

        #endregion

        #region Saving

        #if UNITY_EDITOR

        private void ForceReimportMyTexture(string path)
        {

            TextureImporter importer = AssetImporter.GetAtPath("Assets{0}".F(path)) as TextureImporter;
            if (importer == null)
            {
                Debug.Log("No importer for {0}".F(path));
                return;
            }

            var id = ImgData;

            importer.SaveAndReimport();
            if (id.TargetIsRenderTexture())
                id.TextureToRenderTexture(id.texture2D);
            else
                if (id.texture2D)
                id.PixelsFromTexture2D(id.texture2D);

            SetTextureOnMaterial(id);
        }

        private bool TextureExistsAtDestinationPath()
        {
            TextureImporter importer = AssetImporter.GetAtPath("Assets{0}".F(GenerateTextureSavePath())) as TextureImporter;
            return importer != null;
        }

        private string GenerateTextureSavePath() =>
             "/{0}{1}.png".F(Cfg.texturesFolderName.AddPostSlashIfNotEmpty(), ImgData.SaveName);

        public string GenerateMeshSavePath() =>
             meshFilter.sharedMesh ? "/{0}/{1}.asset".F(Cfg.meshesFolderName, meshNameHolder) : "None";

        private bool OnBeforeSaveTexture(ImageData id)
        {
            if (id.TargetIsRenderTexture()) 
                id.RenderTexture_To_Texture2D();

            var tex = id.texture2D;

            if (id. preserveTransparency && !tex.TextureHasAlpha()) {
                
                ChangeTexture(id.NewTexture2D());
                
                Debug.Log("Old Texture had no Alpha channel, creating new");

                id.texture2D = id.texture2D.SaveTextureAsAsset(Cfg.texturesFolderName, ref id.SaveName, false);

                id.texture2D.CopyImportSettingFrom(tex).Reimport_IfNotReadale();

                return false;
            }

            id.SetAlphaSavePixel();

            return true;
        }

        private void OnPostSaveTexture(ImageData id)
        {
            SetTextureOnMaterial(id);
            UpdateOrSetTexTarget(id.destination);
            UpdateShaderGlobals();

            id.UnsetAlphaSavePixel();
        }

        private void RewriteOriginalTexture_Rename(string texName) {

            var id = ImgData;

            if (!OnBeforeSaveTexture(id)) return;
            
            id.texture2D = id.texture2D.RewriteOriginalTexture_NewName(texName);

            OnPostSaveTexture(id);
            
        }

        private void RewriteOriginalTexture() {
            var id = ImgData;

            if (!OnBeforeSaveTexture(id)) return;
            
            id.texture2D = id.texture2D.RewriteOriginalTexture();
            OnPostSaveTexture(id);
            
        }

        private void SaveTextureAsAsset(bool asNew) {

            var id = ImgData;

            if (OnBeforeSaveTexture(id)) {
                id.texture2D = id.texture2D.SaveTextureAsAsset(Cfg.texturesFolderName, ref id.SaveName, asNew);

                id.texture2D.Reimport_IfNotReadale();
            }

            OnPostSaveTexture(id);
        }

        public void SaveMesh()
        {

            var m = this.GetMesh();
            var path = AssetDatabase.GetAssetPath(m);

            var lastPart = "/{0}/".F(Cfg.meshesFolderName);
            var folderPath = Application.dataPath + lastPart;
            Directory.CreateDirectory(folderPath);

            try
            {

                if (path.Length > 0)
                    meshFilter.sharedMesh = Instantiate(meshFilter.sharedMesh);


                if (meshNameHolder.Length == 0)
                    meshNameHolder = meshFilter.sharedMesh.name;
                else
                    meshFilter.sharedMesh.name = meshNameHolder;

                AssetDatabase.CreateAsset(meshFilter.sharedMesh, "Assets{0}".F(GenerateMeshSavePath()));

                AssetDatabase.SaveAssets();
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
            }
        }

        #endif

        #endregion

        #region COMPONENT MGMT 

        public bool LockTextureEditing
        {
            get
            {
                if (meshEditing) return true;
                var i = ImgData; 
                return i == null ? true : i.lockEditing || i.other;


            }
            set { var i = ImgData; if (i != null) i.lockEditing = value; }
        }
        public bool forcedMeshCollider;
        [NonSerialized]
        public bool inited = false;
        public bool autoSelectMaterialByNumberOfPointedSubMesh = true;

        public const string WWW_Manual = "https://docs.google.com/document/d/170k_CE-rowVW9nsAo3EUqVIAWlZNahC0ua11t1ibMBo/edit?usp=sharing";

        private static readonly List<string> TextureEditorIgnore = new List<string> { "VertexEd", "toolComponent", "o" };

        public static bool CanEditWithTag(string tag)
        {
            foreach (var x in TextureEditorIgnore)
                if (tag.Contains(x))
                    return false;
            return true;
        }

#if UNITY_EDITOR

        [MenuItem("Tools/" + PainterDataAndConfig.ToolName + "/Attach Painter To Selected")]
        private static void GivePainterToSelected()
        {
            foreach (var go in Selection.gameObjects)
                IterateAssignToChildren(go.transform);
        }

        private static void IterateAssignToChildren(Transform tf)
        {

            if ((!tf.GetComponent<PlaytimePainter>())
                && (tf.GetComponent<Renderer>())
                && (!tf.GetComponent<RenderBrush>()) && (CanEditWithTag(tf.tag)))
                tf.gameObject.AddComponent<PlaytimePainter>();

            for (var i = 0; i < tf.childCount; i++)
                IterateAssignToChildren(tf.GetChild(i));

        }

        [MenuItem("Tools/" + PainterDataAndConfig.ToolName + "/Remove Painters From the Scene")]
        private static void TakePainterFromAll()
        {
            var allObjects = UnityEngine.Object.FindObjectsOfType<Renderer>();
            foreach (var mr in allObjects)
            {
                var ip = mr.GetComponent<PlaytimePainter>();
                if (ip)
                    DestroyImmediate(ip);
            }

            var rtp = FindObjectsOfType<PainterCamera>();
            if (rtp != null)
                foreach (var rt in rtp)
                    rt.gameObject.DestroyWhatever();

            PainterStuff.applicationIsQuitting = false;
        }

        [MenuItem("Tools/" + PainterDataAndConfig.ToolName + "/Instantiate Painter Camera")]
        private static void InstantiatePainterCamera()
        {
            PainterStuff.applicationIsQuitting = false;
            var r = PainterCamera.Inst;
        }

        [MenuItem("Tools/" + PainterDataAndConfig.ToolName + "/Join Discord")]
        public static void Open_Discord() => Application.OpenURL("https://discord.gg/rF7yXq3");

        [MenuItem("Tools/" + PainterDataAndConfig.ToolName + "/Open Manual")]
        public static void OpenWWW_Documentation() => Application.OpenURL(WWW_Manual);

        public static void OpenWWW_Forum() => Application.OpenURL("https://www.quizcanners.com/forum/texture-editor");

        [MenuItem("Tools/" + PainterDataAndConfig.ToolName + "/Send an Email")]
        public static void Open_Email() => UnityHelperFunctions.SendEmail("quizcanners@gmail.com", "About your Playtime Painter",
            "Hello Yuri, we need to talk. I purchased your asset and expect an excellent quality, but ...");

#endif

        public void OnDestroy()
        {


            var colliders = GetComponents<Collider>();

            foreach (var c in colliders)
                if (c.GetType() != typeof(MeshCollider)) c.enabled = true;

            colliders = GetComponentsInChildren<Collider>();

            foreach (var c in colliders)
                if (c.GetType() != typeof(MeshCollider)) c.enabled = true;

            if (forcedMeshCollider && (meshCollider))
                meshCollider.enabled = false;
        }

        private void OnDisable()
        {

            SetOriginalShader();

            var id = GetTextureOnMaterial().GetImgDataIfExists();

            inited = false; // Should be before restoring to texture2D to avoid Clear to black.

            if (id != null && id.CurrentTexture().IsBigRenderTexturePair())
                UpdateOrSetTexTarget(TexTarget.Texture2D);

            if (!TexMgmt || MeshManager.Inst.target != this) return;
            
            MeshManager.Inst.DisconnectMesh();
            MeshManager.Inst.previouslyEdited = this;
            
        }

        public void OnEnable()
        {

            PainterStuff.applicationIsQuitting = false;

            if (plugins == null)
                plugins = new List<PainterComponentPluginBase>();


            PainterComponentPluginBase.UpdateList(this);

            if (terrain)
                UpdateShaderGlobals();

            if (!meshRenderer)
                meshRenderer = GetComponent<MeshRenderer>();

        }

        public void UpdateColliderForSkinnedMesh()
        {

            if (!colliderForSkinnedMesh) colliderForSkinnedMesh = new Mesh();
            skinnedMeshRenderer.BakeMesh(colliderForSkinnedMesh);
            if (meshCollider)
                meshCollider.sharedMesh = colliderForSkinnedMesh;

        }

        public void InitIfNotInitialized()
        {
            //  return;

            if (!(!inited || ((!meshCollider || !meshRenderer) && (!terrain || !terrainCollider)))) return;
            
                inited = true;

                nameHolder = gameObject.name;

                if (!meshRenderer)
                    meshRenderer = GetComponent<Renderer>();

                if (meshRenderer)
                {

                    var collis = GetComponents<Collider>();

                    foreach (var c in collis)
                        if (c.GetType() != typeof(MeshCollider)) c.enabled = false;

                    collis = GetComponentsInChildren<Collider>();

                    foreach (var c in collis)
                        if (c.GetType() != typeof(MeshCollider)) c.enabled = false;

                    meshCollider = GetComponent<MeshCollider>();
                    meshFilter = GetComponent<MeshFilter>();

                    if (!meshCollider)
                    {
                        meshCollider = meshRenderer.gameObject.AddComponent<MeshCollider>();
                        forcedMeshCollider = true;
                    }
                    else if (meshCollider.enabled == false)
                    {
                        meshCollider.enabled = true;
                        forcedMeshCollider = true;
                    }

                }

                if ((meshRenderer) && (meshRenderer.GetType() == typeof(SkinnedMeshRenderer)))
                {
                    skinnedMeshRenderer = (SkinnedMeshRenderer)meshRenderer;
                    UpdateColliderForSkinnedMesh();
                }
                else skinnedMeshRenderer = null;

                if (!meshRenderer)
                {
                    terrain = GetComponent<Terrain>();
                    if (terrain)
                        terrainCollider = GetComponent<TerrainCollider>();

                }

                if ((this == TexMgmt.autodisabledBufferTarget) && (!LockTextureEditing) && (!UnityHelperFunctions.ApplicationIsAboutToEnterPlayMode()))
                    ReEnableRenderTexture();

            
        }

        private void FocusOnThisObject()
        {

#if UNITY_EDITOR
            UnityHelperFunctions.FocusOn(this.gameObject);
#endif
            _selectedInPlaytime = this;
            Update_Brush_Parameters_For_Preview_Shader();
            InitIfNotInitialized();
        }

        #endregion

        #region Inspector 

        public void OnGUI()
        {

#if BUILD_WITH_PAINTER
            if (!Cfg || !Cfg.enablePainterUIonPlay) return;
            
            if (!_selectedInPlaytime)
                _selectedInPlaytime = this;
#if PEGI
            if (_selectedInPlaytime == this)
                WindowPosition.Render(Inspect, "{0} {1}".F(gameObject.name, GetMaterialTexturePropertyName));
#endif
      
            #endif

        }

        public static PlaytimePainter inspectedPainter;

        [NonSerialized] public readonly Dictionary<int, string> loadingOrder = new Dictionary<int, string>();

        private static PlaytimePainter _selectedInPlaytime = null;

        #if PEGI
        private static readonly pegi.WindowPositionData_PEGI_GUI WindowPosition = new pegi.WindowPositionData_PEGI_GUI();

        private const string defaultImageLoadURL = "https://picsbuffet.com/pixabay/";

        private static string _tmpUrl = defaultImageLoadURL;

        private static int _inspectedFancyStuff = -1;

        public bool Inspect()
        {
            inspectedPainter = this;

            var changed = false;

            TexMgmt.focusedPainter = this;

            if (gameObject.IsPrefab())
            {
                "Inspecting a prefab.".nl();
                return changed;
            }

            if (!Cfg)
            {
                "No Config Detected".nl();
                if (icon.Refresh.Click())
                {
                    PainterStuff.applicationIsQuitting = false;
                    if (PainterCamera.Inst)
                        PainterCamera.Inst.triedToFindPainterData = false;
                }
                return changed;
            }

            if (!IsCurrentTool)
            {
                if (icon.Off.Click("Click to Enable Tool", 25))
                {
                    PainterDataAndConfig.toolEnabled = true;

#if UNITY_EDITOR
                    enabled = true;
                    var cs = GetComponents(typeof(Component));

                    foreach (Component c in cs)
                        if (c.GetType() != typeof(PlaytimePainter))
                            UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(c, false);

                    UnityHelperFunctions.FocusOn(null);
                    PainterCamera.refocusOnThis = gameObject;
                    UnityHelperFunctions.HideUnityTool();
#endif

                    CheckPreviewShader();
                }

                pegi.Lock_UnlockWindowClick(gameObject);

                return changed;

            }
            else
            {

                if ((IsCurrentTool && terrain && !Application.isPlaying && UnityEditorInternal.InternalEditorUtility.GetIsInspectorExpanded(terrain)) ||
                    (icon.On.Click("Click to Disable Tool", 25)))
                {
                    PainterDataAndConfig.toolEnabled = false;
                    WindowPosition.Collapse();
                    MeshManager.Inst.DisconnectMesh();
                    SetOriginalShaderOnThis();
                    UpdateOrSetTexTarget(TexTarget.Texture2D);
                    UnityHelperFunctions.RestoreUnityTool();
                }

                pegi.Lock_UnlockWindowClick(gameObject);
            }



            InitIfNotInitialized();

            var image = ImgData;

            var tex = GetTextureOnMaterial();
            if (!meshEditing && ((tex && image == null) || (image != null && !tex) || (image != null && tex != image.texture2D && tex != image.CurrentTexture())))
                textureWasChanged = true;

            #region Top Buttons

            if (!PainterStuff.IsNowPlaytimeAndDisabled)
            {

                if ((MeshManager.target) && (MeshManager.target != this))
                    MeshManager.DisconnectMesh();

                if (!Cfg.showConfig)
                {
                    if (meshEditing)
                    {
                        if (icon.Painter.Click("Edit Texture", ref changed, 25))
                        {
                            SetOriginalShader();
                            meshEditing = false;
                            CheckPreviewShader();
                            MeshMgmt.DisconnectMesh();
                            Cfg.showConfig = false;
                            "Editing Texture".showNotificationIn3D_Views();
                        }
                    }
                    else
                    {
                        if (icon.Mesh.Click("Edit Mesh", ref changed, 25))
                        {
                            meshEditing = true;

                            SetOriginalShader();
                            UpdateOrSetTexTarget(TexTarget.Texture2D);
                            Cfg.showConfig = false;
                            "Editing Mesh".showNotificationIn3D_Views();

                            if (SavedEditableMesh != null)
                                MeshMgmt.EditMesh(this, false);

                        }
                    }
                }

                pegi.toggle(ref Cfg.showConfig, meshEditing ? icon.Mesh : icon.Painter, icon.Config, "Settings", 25);
            }

            #endregion

            if ((Cfg.showConfig) || (PainterStuff.IsNowPlaytimeAndDisabled))
            {
                pegi.newLine();
                Cfg.Nested_Inspect();
            }
            else
            {

                #region Mesh Editing

                if (meshEditing)
                {

                    var mg = MeshMgmt;
                    mg.Undo_redo_PEGI().nl(ref changed);

                    if (meshFilter)
                    {

                        if (this != mg.target)
                        {
                            if (SavedEditableMesh != null)
                                "Got saved mesh data".nl();
                            else
                                "No saved data found".nl();
                        }

                        "Warning, this will change (or mess up) your model.".writeOneTimeHint("MessUpMesh");
                        
                        if (mg.target != this)
                        {

                            var ent = gameObject.GetComponent("pb_Entity");
                            var obj = gameObject.GetComponent("pb_Object");

                            if (ent || obj)
                                "PRO builder detected. Strip it using Actions in the Tools->ProBuilder menu.".writeHint();
                            else
                            {
                                if (Application.isPlaying)
                                    "Playtime Changes will be reverted once you try to edit the mesh again."
                                        .writeWarning();

                                pegi.newLine();

                                if ("Edit Copy".Click())
                                    mg.EditMesh(this, true);

                                if ("New Mesh".Click())
                                {
                                    meshFilter.mesh = new Mesh();
                                    SavedEditableMesh = null;
                                    mg.EditMesh(this, false);
                                }

                                if (icon.Edit.ClickUnfocus("Edit Mesh", 25).nl())
                                    mg.EditMesh(this, false);
                            }
                        }

                    }
                    else if ("Add Mesh Filter/Renderer".Click().nl())
                    {
                        meshFilter = gameObject.AddComponent<MeshFilter>();
                        if (!meshRenderer)
                            meshRenderer = gameObject.AddComponent<MeshRenderer>();
                    }

                    if (this && (MeshMgmt.target == this))
                    {

                        if ("Profile".foldout())
                        {

                            if ((Cfg.meshPackagingSolutions.Count > 1) && (icon.Delete.Click(25)))
                                Cfg.meshPackagingSolutions.RemoveAt(selectedMeshProfile);
                            else
                            {

                                pegi.newLine();
                                if (MeshProfile.Inspect().nl())
                                    MeshMgmt.edMesh.Dirty = true;

                                if ("Hint".foldout(ref VertexSolution.showHint).nl())
                                {
                                    "If using projected UV, place sharpNormal in TANGENT.".writeHint();
                                    "Vectors should be placed in normal and tangent slots to batch correctly.".writeHint();
                                    "Keep uv1 as is for baked light and damage shaders.".writeHint();
                                    "I place Shadows in UV2".nl();
                                    "I place Edge in UV3.".nl();

                                }

                            }
                        }
                        else
                        {
                            if ((" : ".select(20, ref selectedMeshProfile, Cfg.meshPackagingSolutions)) && (IsEditingThisMesh))
                                MeshMgmt.edMesh.Dirty = true;

                            if (icon.Add.Click(25).nl())
                            {
                                Cfg.meshPackagingSolutions.Add(new MeshPackagingProfile());
                                selectedMeshProfile = Cfg.meshPackagingSolutions.Count - 1;
                                MeshProfile.name = "New Profile {0}".F(selectedMeshProfile);
                            }

                            MeshMgmt.PEGI().nl();
                        }
                    }
                    pegi.newLine();

                }

                #endregion

                #region Texture Editing

                else
                {

                    var id = ImgData;

                    if (!LockTextureEditing)
                    {

                        #region Undo/Redo & Recording
                        id.Undo_redo_PEGI();

                        if (id.showRecording && !id.recording)
                        {

                            pegi.newLine();

                            if (playbackPainters.Count > 0)
                            {
                                "Playback In progress".nl();

                                if (icon.Close.Click("Cancel All Playbacks", 20))
                                    TexMgmt.CancelAllPlaybacks();

                                if (StrokeVector.PausePlayback)
                                {
                                    if (icon.Play.Click("Continue Playback", 20))
                                        StrokeVector.PausePlayback = false;
                                }
                                else if (icon.Pause.Click("Pause Playback", 20))
                                    StrokeVector.PausePlayback = true;

                            }
                            else
                            {
                                var gotVectors = Cfg.recordingNames.Count > 0;

                                Cfg.browsedRecord = Mathf.Max(0, Mathf.Min(Cfg.browsedRecord, Cfg.recordingNames.Count - 1));

                                if (gotVectors)
                                {
                                    pegi.select(ref Cfg.browsedRecord, Cfg.recordingNames);
                                    if (icon.Play.Click("Play stroke vectors on current mesh", ref changed, 18))
                                        PlayByFilename(Cfg.recordingNames[Cfg.browsedRecord]);


                                    if (icon.Record.Click("Continue Recording", 18))
                                    {
                                        id.SaveName = Cfg.recordingNames[Cfg.browsedRecord];
                                        id.ContinueRecording();
                                        "Recording resumed".showNotificationIn3D_Views();
                                    }

                                    if (icon.Delete.Click("Delete", ref changed, 18))
                                        Cfg.recordingNames.RemoveAt(Cfg.browsedRecord);

                                }

                                if ((gotVectors && icon.Add.Click("Start new Vector recording", 18)) ||
                                    (!gotVectors && "New Vector Recording".Click("Start New recording")))
                                {
                                    id.SaveName = "Unnamed";
                                    id.StartRecording();
                                    "Recording started".showNotificationIn3D_Views();
                                }
                            }

                            pegi.newLine();
                            pegi.space();
                            pegi.newLine();
                        }

                        pegi.nl();

                        var CPU = id.TargetIsTexture2D();

                        var mat = Material;
                        if (mat.IsProjected())
                        {

                            pegi.writeWarning("Projected UV Shader detected. Painting may not work properly");
                            if ("Undo".Click(40).nl())
                                mat.DisableKeyword(PainterDataAndConfig.UV_PROJECTED);
                        }

                        if (!CPU && id.texture2D && id.width != id.height)
                            "Non-square texture detected! Every switch between GPU and CPU mode will result in loss of quality.".writeWarning();

                        #endregion

                        #region Brush

                        if (Application.isPlaying && !Camera.main)
                        {
                            "No Camera tagged as 'Main' detected. Tag one to enable raycasts".writeWarning();
                            pegi.nl();
                        }

                        changed |= GlobalBrush.Inspect();

                        var mode = GlobalBrush.BlitMode;
                        var col = GlobalBrush.colorLinear.ToGamma();

                        if ((CPU || (!mode.UsingSourceTexture)) && !IsTerrainHeightTexture && !pegi.paintingPlayAreaGUI)
                        {
                            if (pegi.edit(ref col).changes(ref changed))
                                GlobalBrush.colorLinear.From(col);

                        }

                        pegi.nl();

                        if (!Cfg.moreOptions)
                        {

                            changed |= GlobalBrush.ColorSliders_PEGI().nl();

                            if (Cfg.showColorSchemes)
                            {

                                var scheme = Cfg.colorSchemes.TryGet(Cfg.selectedColorScheme);

                                scheme?.PickerPEGI();

                                if (Cfg.showColorSchemes)
                                    changed |= "Scheme".select(40, ref Cfg.selectedColorScheme, Cfg.colorSchemes).nl();

                            }
                        }

                        #endregion

                    }
                    else
                        if (!IsOriginalShader)
                        this.PreviewShaderToggle_PEGI();

                    id = ImgData;

                    #region Fancy Options
                    pegi.nl();
                    "Fancy options".foldout(ref Cfg.moreOptions).nl();

                    var inspSt = id != null ? id.inspectedStuff : _inspectedFancyStuff;

                    if (Cfg.moreOptions)
                    {

                        if (icon.Show.enter("Show/Hide stuff", ref inspSt, 7).nl())
                        {

                            "Show Previous Textures (if any) ".toggleVisibilityIcon("Will show textures previously used for this material property.", ref Cfg.showRecentTextures, true).nl();

                            "Exclusive Render Textures".toggleVisibilityIcon("Allow creation of simple Render Textures - the have limited editing capabilities.", ref Cfg.allowExclusiveRenderTextures, true).nl();

                            "Color Sliders ".toggleVisibilityIcon("Should the color slider be shown ", ref Cfg.showColorSliders, true).nl(ref changed);

                            if (id != null)
                                "Recording/Playback".toggleVisibilityIcon("Show options for brush recording", ref id.showRecording, true).nl(ref changed);

                            "Brush Dynamics".toggleVisibilityIcon("Will modify scale and other values based on movement.", ref GlobalBrush.showBrushDynamics, true).nl(ref changed);

                            "URL field".toggleVisibilityIcon("Option to load images by URL", ref Cfg.showURLfield, true).changes(ref changed);
                        }

                        if ("New Texture Config ".conditional_enter(!IsTerrainHeightTexture, ref inspSt, 4).nl())
                        {

                            if (Cfg.newTextureIsColor)
                                "Clear Color".edit(ref Cfg.newTextureClearColor).nl(ref changed);
                            else
                                "Clear Value".edit(ref Cfg.newTextureClearNonColorValue).nl(ref changed);

                            "Color Texture".toggleIcon("Will the new texture be a Color Texture", ref Cfg.newTextureIsColor).nl(ref changed);

                            "Size:".select("Size of the new Texture", 40, ref PainterCamera.Data.selectedWidthIndex, PainterDataAndConfig.NewTextureSizeOptions).nl();
                        }

                        if (id != null)
                        {
                            id.inspectedStuff = inspSt;
                            changed |= id.Inspect();
                            inspSt = id.inspectedStuff;
                        }
                        else _inspectedFancyStuff = inspSt;

                    }


                    if (id != null)
                    {
                        bool showToggles = (id.inspectedStuff == -1 && Cfg.moreOptions);

                        changed |= id.ComponentDependent_PEGI(showToggles, this);

                        if (showToggles || (!IsOriginalShader && Cfg.previewAlphaChanel))
                            changed |= "Preview Shows Only Enabled Chanels".toggleIcon(ref Cfg.previewAlphaChanel).nl();


                        if (showToggles)
                        {
                            var mats = GetMaterials();
                            if (autoSelectMaterialByNumberOfPointedSubMesh || !mats.IsNullOrEmpty())
                                "Auto Select Material".toggleIcon("Material will be changed based on the submesh you are painting on",
                                                               ref autoSelectMaterialByNumberOfPointedSubMesh).nl();
                        }


                        if (Cfg.moreOptions)
                            pegi.line(Color.red);

                        if (id.enableUndoRedo && id.backupManually && "Backup for UNDO".Click())
                            id.Backup();

                        if (GlobalBrush.DontRedoMipmaps && "Redo Mipmaps".Click().nl())
                            id.SetAndApply();
                    }

                    #endregion

                    #region Save Load Options

                    if (!PainterStuff.IsNowPlaytimeAndDisabled && (meshRenderer || terrain) && !Cfg.showConfig)
                    {
                        #region Material Clonning Options

                        pegi.nl();

                        var mats = GetMaterials();
                        if (!mats.IsNullOrEmpty())
                        {
                            int sm = selectedSubmesh;
                            if (pegi.select(ref sm, mats))
                            {
                                SetOriginalShaderOnThis();
                                selectedSubmesh = sm;
                                OnChangedTexture_OnMaterial();
                                id = ImgData;
                                CheckPreviewShader();
                            }
                        }

                        Material mater = Material;

                        if (pegi.edit(ref mater).changes(ref changed))
                            Material = mater;

                        if (icon.NewMaterial.Click("Instantiate Material", 25).nl(ref changed))
                            InstantiateMaterial(true);

                        pegi.nl();
                        pegi.space();
                        pegi.nl();
                        #endregion

                        #region Texture Instantiation Options

                        if (Cfg.showURLfield)
                        {

                            "URL".edit(40, ref _tmpUrl);
                            if (_tmpUrl.Length > 5 && icon.Download.Click())
                            {
                                loadingOrder.Add(PainterCamera.downloadManager.StartDownload(_tmpUrl), GetMaterialTexturePropertyName);
                                _tmpUrl = defaultImageLoadURL;
                                "Loading for {0}".F(GetMaterialTexturePropertyName).showNotificationIn3D_Views();
                            }

                            pegi.nl();
                            if (loadingOrder.Count > 0)
                                "Loading {0} texture{1}".F(loadingOrder.Count, loadingOrder.Count > 1 ? "s" : "").nl();

                            pegi.nl();

                        }


                        int ind = SelectedTexture;
                        if (pegi.select(ref ind, GetMaterialTextureNames()).changes(ref changed)) {
                            SetOriginalShaderOnThis();
                            SelectedTexture = ind;
                            OnChangedTexture_OnMaterial();
                            CheckPreviewShader();
                            id = ImgData;
                            if (id == null)
                                nameHolder = gameObject.name + "_" + GetMaterialTexturePropertyName;
                        }
                       
                        if (id != null)
                            UpdateTylingFromMaterial();
                        
                        if (id != null && pegi.toggle(ref id.lockEditing, icon.Lock.GetIcon(), icon.Unlock.GetIcon(), "Lock/Unlock editing of {0} Texture.".F(id.ToPEGIstring()), 25))
                        {
                            CheckPreviewShader();
                            if (LockTextureEditing)
                                UpdateOrSetTexTarget(TexTarget.Texture2D);

                            #if UNITY_EDITOR
                            if (id.lockEditing)
                                UnityHelperFunctions.RestoreUnityTool();
                            else
                                UnityHelperFunctions.HideUnityTool();
                            #endif
                        }
                        
                        tex = GetTextureOnMaterial();

                        if (pegi.edit(ref tex).changes(ref changed))
                            ChangeTexture(tex);

                        if (!IsTerrainControlTexture) {

                            bool isTerrainHeight = IsTerrainHeightTexture;

                            int texScale = !isTerrainHeight ? Cfg.SelectedWidthForNewTexture() : (terrain.terrainData.heightmapResolution - 1);

                            List<string> texNames = GetMaterialTextureNames();

                            if (texNames.Count > SelectedTexture)
                            {
                                string param = GetMaterialTexturePropertyName;

                                if (icon.NewTexture.Click((id == null) ?
                                    "Create new texture2D for " + param : "Replace " + param + " with new Texture2D " + texScale + "*" + texScale, 25).nl(ref changed))
                                {
                                    if (isTerrainHeight)
                                        CreateTerrainHeightTexture(nameHolder);
                                    else
                                        CreateTexture2D(texScale, nameHolder, Cfg.newTextureIsColor);
                                }

                                if (Cfg.showRecentTextures)
                                {
                                   
                                    string texName = GetMaterialTexturePropertyName;

                                    List<ImageData> recentTexs;
                                    if (texName != null && PainterCamera.Data.recentTextures.TryGetValue(texName, out recentTexs)
                                        && (recentTexs.Count > 0 || (id == null))) {

                                        if ("Recent Textures:".select(100, ref id, recentTexs).nl(ref changed))
                                            ChangeTexture(id.ExclusiveTexture());
                                    }
                                }

                                if (id == null && Cfg.allowExclusiveRenderTextures && "Create Render Texture".Click(ref changed))
                                    CreateRenderTexture(texScale, nameHolder);

                                if (id != null && Cfg.allowExclusiveRenderTextures)
                                {
                                    if (!id.renderTexture && "Add Render Tex".Click(ref changed))
                                        id.AddRenderTexture();

                                    if (id.renderTexture)
                                    {

                                        if ("Replace RendTex".Click("Replace " + param + " with Rend Tex size: " + texScale, ref changed))
                                            CreateRenderTexture(texScale, nameHolder);

                                        if ("Remove RendTex".Click().nl(ref changed))
                                        {

                                            if (id.texture2D)
                                            {
                                                UpdateOrSetTexTarget(TexTarget.Texture2D);
                                                id.renderTexture = null;
                                            }
                                            else
                                                RemoveTextureFromMaterial();

                                        }
                                    }
                                }
                            }
                            else
                                "No Material's Texture selected".nl();

                            pegi.nl();

                            if (id == null)
                                "_Name:".edit("Name for new texture", 40, ref nameHolder).nl();

                        }

                        pegi.newLine();
                        pegi.space();
                        pegi.newLine();

                        #endregion

                        #region Texture Saving/Loading

                        if (!LockTextureEditing)
                        {
                            pegi.nl();
                            if (!IsTerrainControlTexture)
                            {

                                id = ImgData;

                                #if UNITY_EDITOR
                                string orig = null;
                                if (id.texture2D)
                                {
                                    orig = id.texture2D.GetPathWithout_Assets_Word();
                                    if (orig != null && icon.Load.ClickUnfocus("Will reload " + orig, 25))
                                    {
                                        ForceReimportMyTexture(orig);
                                        id.SaveName = id.texture2D.name;
                                        if (terrain)
                                            UpdateShaderGlobals();
                                    }
                                }

                                pegi.edit(ref id.SaveName);

                                if (id.texture2D)
                                {

                                    if (!id.SaveName.SameAs(id.texture2D.name) && icon.Refresh.Click("Use current texture name ({0})".F(id.texture2D.name)))
                                        id.SaveName = id.texture2D.name;

                                    var DestPath =              GenerateTextureSavePath();
                                    var existsAtDestination =   TextureExistsAtDestinationPath();
                                    var originalExists =        !orig.IsNullOrEmpty();
                                    var sameTarget =            originalExists && orig.Equals(DestPath);
                                    var sameTextureName =       originalExists && id.texture2D.name.Equals(id.SaveName);


                                    if (!existsAtDestination || sameTextureName)
                                    {
                                        if ((sameTextureName ? icon.Save : icon.SaveAsNew).Click(sameTextureName ? "Will Update " + orig : "Will save as " + DestPath, 25))
                                        {
                                            if (sameTextureName)
                                                RewriteOriginalTexture();
                                            else
                                                SaveTextureAsAsset(false);

                                            OnChangedTexture_OnMaterial();
                                        }
                                    }
                                    else if (existsAtDestination && icon.Save.Click("Will replace " + DestPath, 25))
                                        SaveTextureAsAsset(false);

                                    if (!sameTarget && !sameTextureName && originalExists && !existsAtDestination && icon.Replace.Click("Will replace {0} with {1} ".F(orig, DestPath)))
                                        RewriteOriginalTexture_Rename(id.SaveName);

                                    pegi.nl();

                                }
                                #endif

                            }
                            pegi.nl();
                        }
                        pegi.nl();

                        pegi.space();
                        pegi.nl();
                        #endregion
                    }

                    #endregion

                }
                pegi.nl();

                #endregion

                #region Plugins
                if (pluginsComponentPEGI != null)
                    foreach (pegi.CallDelegate p in pluginsComponentPEGI.GetInvocationList())
                        changed |= p().nl();
                #endregion
            }

            pegi.newLine();

            if (changed)
                Update_Brush_Parameters_For_Preview_Shader();
            
            inspectedPainter = null;
            return changed;
        }
        
        public bool PreviewShaderToggle_PEGI()
        {

            var changed = false;
            if (IsTerrainHeightTexture)
            {
                Texture tht = terrainHeightTexture;

                if (!IsOriginalShader && icon.PreviewShader.Click("Applies changes made on Texture to Actual physical Unity Terrain.", 45).changes(ref changed))
                {
                    Preview_To_UnityTerrain();
                    Unity_To_Preview();

                    MatDta.usePreviewShader = false;
                    SetOriginalShaderOnThis();

                }
                PainterCamera.Data.brushConfig.MaskSet(BrushMask.A, true);

                if (tht.GetImgData() != null && IsOriginalShader && icon.OriginalShader.Click("Applies changes made in Unity terrain Editor", 45).changes(ref changed))
                {
                    Unity_To_Preview();
                    SetPreviewShader();
                }
            }
            else
            {

                if (IsOriginalShader && icon.OriginalShader.Click("Switch To Preview Shader", 45).changes(ref changed))
                    SetPreviewShader();

                if (!IsOriginalShader && icon.PreviewShader.Click("Return to Original Shader", 45).changes(ref changed))
                {
                    MatDta.usePreviewShader = false;
                    SetOriginalShaderOnThis();
                }
            }
            return changed;

        }
        #endif

#if UNITY_EDITOR

        void OnDrawGizmosSelected()
        {

            if (meshEditing)
            {
                if (!Application.isPlaying)
                    MeshManager.Inst.DRAW_Lines(true);
            }

            if (IsOriginalShader && !LockTextureEditing && _lastMouseOverObject == this && IsCurrentTool && GlobalBrush.IsA3Dbrush(this) && !Cfg.showConfig)
                Gizmos.DrawWireSphere(stroke.posTo, GlobalBrush.Size(true) * 0.5f);

            if (pluginsGizmoDraw != null)
                foreach (PainterBoolPlugin gp in pluginsGizmoDraw.GetInvocationList())
                    gp(this);

        }
#endif

        #endregion

        #region UPDATES  

        public bool textureWasChanged = false;



#if UNITY_EDITOR
        public void FeedEvents(Event e)
        {
            var id = ImgData;

            if (e.type == EventType.KeyDown && !meshEditing && id != null)
            {
                if (e.keyCode == KeyCode.Z && id.cache.undo.GotData)
                    id.cache.undo.ApplyTo(id);
                else if (e.keyCode == KeyCode.X && id.cache.redo.GotData)
                    id.cache.redo.ApplyTo(id);
            }
        }
#endif

#if UNITY_EDITOR || BUILD_WITH_PAINTER

        public void Update()
        {

            #region URL Loading
            if (loadingOrder.Count > 0)
            {

                List<int> extracted = new List<int>();

                foreach (var l in loadingOrder)
                {
                    Texture tmp;
                    if (PainterCamera.downloadManager.TryGetTexture(l.Key, out tmp, true))
                    {
                        if (tmp)
                        {
                            var idtom = SetTextureOnMaterial(l.Value, tmp);
                            if (idtom != null)
                            {
                                idtom.URL = PainterCamera.downloadManager.GetURL(l.Key);
                                idtom.SaveName = "Loaded Texture {0}".F(l.Key);
                            }
                        }
                        extracted.Add(l.Key);
                    }
                }

                foreach (var e in extracted)
                    loadingOrder.Remove(e);
            }
            #endregion

            if (IsEditingThisMesh && Application.isPlaying)
                MeshManager.Inst.DRAW_Lines(false);

            if (textureWasChanged)
                OnChangedTexture_OnMaterial();


            var id = ImgData;

            if (id != null)
                id.Update(stroke.mouseUp);
        }
#endif

        private void PreviewShader_StrokePosition_Update()
        {
            CheckPreviewShader();
            if (!IsOriginalShader)
            {
                bool hide = Application.isPlaying ? Input.GetMouseButton(0) : currentlyPaintedObjectPainter == this;
                PainterCamera.Shader_PerFrame_Update(stroke, hide, GlobalBrush.Size(this));
            }
        }

        private void Update_Brush_Parameters_For_Preview_Shader()
        {
            var id = ImgData;

            if (id != null && !IsOriginalShader)
            {
                TexMgmt.Shader_UpdateBrush(GlobalBrush, 1, id, this);

                foreach (var p in plugins)
                    p.Update_Brush_Parameters_For_Preview_Shader(this);

            }
        }

        #endregion

        #region Mesh Editing 

        public bool IsEditingThisMesh { get { return IsCurrentTool && meshEditing && (MeshManager.Inst.target == this); } }

        public MeshManager MeshManager { get { return MeshManager.Inst; } }

        public int GetAnimationUVy()
        {
            return 0;
        }

        public bool AnimatedVertices()
        {
            return false;
        }

        public int GetVertexAnimationNumber()
        {
            return 0;
        }

        public bool TryLoadMesh(string data)
        {
            if (!data.IsNullOrEmpty())
            {
                SavedEditableMesh = data;

                MeshManager.EditMesh(this, true);

                MeshManager.DisconnectMesh();

                return true;
            }
            return false;
        }

        #endregion
    }
}