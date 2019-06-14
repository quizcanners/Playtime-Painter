using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#if UNITY_2019_1_OR_NEWER
using UnityEditor.EditorTools;
#endif
#endif

using System;
using System.IO;
using System.Linq;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace PlaytimePainter {
    
    #pragma warning disable IDE0034 // Simplify 'default' expression
    #pragma warning disable IDE0019 // Use pattern matching
    #pragma warning disable IDE0018 // Inline variable declaration
    
    [AddComponentMenu("Mesh/Playtime Painter")]
    [HelpURL(OnlineManual)]
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class PlaytimePainter : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IKeepMyCfg, IPEGI
    {

        #region StaticGetters
        
        public static bool IsCurrentTool
        {
            get {

                #if UNITY_EDITOR && UNITY_2019_1_OR_NEWER
                if (!Application.isPlaying) 
                    return EditorTools.activeToolType == typeof(PainterAsIntegratedCustomTool);
                
                #endif

                return PainterDataAndConfig.toolEnabled;
            }
            set
            {
                PainterDataAndConfig.toolEnabled = value;
            }
        }

        private static PainterDataAndConfig Cfg => PainterCamera.Data;

        private static PainterCamera TexMgmt => PainterCamera.Inst;

        private static MeshManager MeshMgmt => MeshManager.Inst;

        protected static GridNavigator Grid => GridNavigator.Inst();

        private static BrushConfig GlobalBrush => Cfg.brushConfig;

        public BrushType GlobalBrushType => GlobalBrush.GetBrushType(ImgMeta.TargetIsTexture2D());

        private bool NeedsGrid => this.NeedsGrid();
        
        #endregion

        #region Dependencies

        public Renderer meshRenderer;
        public SkinnedMeshRenderer skinnedMeshRenderer;
        public Graphic uiGraphic;
        public Terrain terrain;

        [SerializeField] private MeshFilter meshFilter;
        public TerrainCollider terrainCollider;
        public MeshCollider meshCollider;
        public Texture2D terrainHeightTexture;

        [NonSerialized] public Mesh colliderForSkinnedMesh;

        public Mesh SharedMesh {

            get { return meshFilter ? meshFilter.sharedMesh : (skinnedMeshRenderer ? skinnedMeshRenderer.sharedMesh : null); }
            set {
                if (meshFilter) meshFilter.sharedMesh = value;
                if (skinnedMeshRenderer) skinnedMeshRenderer.sharedMesh = value;
            }
        }

        public Mesh Mesh { set { if (meshFilter) meshFilter.mesh = value; if (skinnedMeshRenderer) skinnedMeshRenderer.sharedMesh = value; } }
        
        public bool meshEditing;

        public int selectedMeshProfile;
        public MeshPackagingProfile MeshProfile
        {
            get { 
                selectedMeshProfile = Mathf.Max(0, Mathf.Min(selectedMeshProfile, Cfg.meshPackagingSolutions.Count - 1)); 
                return Cfg.meshPackagingSolutions[selectedMeshProfile]; 
            }
        }

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

        public int selectedSubMesh;
        public Material Material
        {
            get { return GetMaterial(); }
            set
            {

                if (meshRenderer && selectedSubMesh < meshRenderer.sharedMaterials.Length)
                {
                    var mats = meshRenderer.sharedMaterials;
                    mats[selectedSubMesh] = value;
                    meshRenderer.materials = mats;
                }
                else if (terrain)
                {
                    terrain.materialTemplate = value;
                    terrain.materialType = value ? Terrain.MaterialType.Custom : Terrain.MaterialType.BuiltInStandard;
                }
                else if (uiGraphic)
                    uiGraphic.material = value;
            }
        }

        public MaterialMeta MatDta => Material.GetMaterialPainterMeta();

        public ImageMeta ImgMeta => GetTextureOnMaterial().GetImgData();

        private bool HasMaterialSource => meshRenderer || terrain || uiGraphic;

        public bool IsUiGraphicPainter => !meshRenderer && !terrain && uiGraphic;

        public string nameHolder = "unnamed";

        public int selectedAtlasedMaterial = -1;
        
        public bool invertRayCast;

        PainterModules modulesContainer = new PainterModules();

        public List<PainterComponentModuleBase> Modules
        {
            get
            {
                modulesContainer.painter = this;
                return modulesContainer.Modules;
            }
        }

        public class PainterModules : TaggedModulesList<PainterComponentModuleBase> {

            protected override void OnInitialize() {
                foreach (var p in modules)
                  p.parentComponent = painter;
            }

            public PlaytimePainter painter;

            public PainterModules() { }
        }

        /*[NonSerialized] private List<PainterComponentModuleBase> modules;
        
        public List<PainterComponentModuleBase> Modules {
            get {

                if (!modules.IsNullOrEmpty())
                    return modules;
                
                modules = new List<PainterComponentModuleBase>();
                
                for (var i = modules.Count - 1; i >= 0; i--)
                    if (modules[i] == null) 
                        modules.RemoveAt(i);
                
                if (modules.Count < PainterComponentModuleBase.all.Types.Count)
                    foreach (var t in PainterComponentModuleBase.all)
                        if (!modules.ContainsInstanceOfType(t))
                            modules.Add((PainterComponentModuleBase)Activator.CreateInstance(t));

                foreach (var p in modules)
                    p.parentComponent = this;

                return modules;
            }
        }*/

        [NonSerialized] private PainterComponentModuleBase _lastFetchedModule;

        public T GetModule<T>() where T : PainterComponentModuleBase
        {

            T returnPlug = null;

            if (_lastFetchedModule != null && _lastFetchedModule.GetType() == typeof(T))
                returnPlug = (T) _lastFetchedModule;
            else
                returnPlug = Modules.GetInstanceOf<T>();
            
            _lastFetchedModule = returnPlug;

            return returnPlug;
        }

        private int SelectedTexture
        {
            get { var md = MatDta; return md?.selectedTexture ?? 0; }
            set { var md = MatDta; if (md != null) md.selectedTexture = value; }
        }

        #endregion

        #region Painting

        public StrokeVector stroke = new StrokeVector();

        public static PlaytimePainter currentlyPaintedObjectPainter;
        private static PlaytimePainter _lastMouseOverObject;
        
        private double _mouseButtonTime;
        
        public void OnMouseOver() {

            if ((pegi.MouseOverPlaytimePainterUI || (_mouseOverPaintableGraphicElement && this!=_mouseOverPaintableGraphicElement)) ||
                (!IsUiGraphicPainter && EventSystem.current && EventSystem.current.IsPointerOverGameObject())) {
                stroke.mouseDwn = false;
                return;
            }

            

            stroke.mouseUp = Input.GetMouseButtonUp(0);
            stroke.mouseDwn = Input.GetMouseButtonDown(0);
            var mouseButton = Input.GetMouseButton(0);

            if (Input.GetMouseButtonDown(1))
                _mouseButtonTime = Time.time;

            if (!CanPaint())
                return;

            if (Input.GetMouseButtonUp(1) && ((Time.time - _mouseButtonTime) < 0.2f))
                FocusOnThisObject();
            
            CheckPreviewShader();

            var mousePos = Input.mousePosition;
            
            if (uiGraphic) {
                if (!CastRayPlaytime_UI())
                    return;
            } else 
                if (!CastRayPlaytime(stroke, mousePos)) return;
            
            var control = (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));

            ProcessMouseDrag(control);

            if ((!mouseButton && !stroke.mouseUp) || control) return;
            
            if (currentlyPaintedObjectPainter != this) {
                currentlyPaintedObjectPainter = this;
                stroke.SetPreviousValues();
                FocusOnThisObject();
            }

            if (!stroke.mouseDwn || CanPaintOnMouseDown())
                GlobalBrush.Paint(stroke, this);
            else foreach (var module in ImgMeta.Modules)
                    module.OnPaintingDrag(this);
            
            if (stroke.mouseUp)
                currentlyPaintedObjectPainter = null;
 
        }

        #if UNITY_EDITOR
        public void OnMouseOverSceneView(RaycastHit hit, Event e) {
            
            if (!CanPaint())
                return;

            if (NeedsGrid)
                ProcessGridDrag();
            else
            if (!ProcessHit(hit, stroke))
                return;

            if (currentlyPaintedObjectPainter != this && stroke.mouseDwn) {
                stroke.firstStroke = true;
                currentlyPaintedObjectPainter = this;
                FocusOnThisObject();
                stroke.uvFrom = stroke.uvTo;
            }

            var control = Event.current != null && Event.current.control;

            ProcessMouseDrag(control);
            
            if (this == currentlyPaintedObjectPainter) {

                if (!stroke.mouseDwn || CanPaintOnMouseDown()) {
                    GlobalBrush.Paint(stroke, this);

                    ManagedUpdate();
                }
                else
                    foreach (var module in ImgMeta.Modules)
                        module.OnPaintingDrag(this);

            }

            stroke.mouseDwn = false;

        }
        #endif

        public bool CanPaint()
        {

            if (!IsCurrentTool) return false;

            _lastMouseOverObject = this;

            if (LockTextureEditing)
                return false;

            if (IsTerrainHeightTexture && NotUsingPreview)
                return false;

            if (MeshManager.target)
                return false;

            if (stroke.mouseDwn || stroke.mouseUp)
                InitIfNotInitialized();

            if (ImgMeta != null) return true;
            
#if !NO_PEGI
            if (stroke.mouseDwn)
                "No texture to edit".showNotificationIn3D_Views();
#endif

                return false;
            
        }

        private readonly QcUtils.ChillLogger _logger = new QcUtils.ChillLogger("");

        private bool CastRayPlaytime(StrokeVector st, Vector3 mousePos) {

            var cam = TexMgmt.MainCamera; 
            
            if (!cam)
            {
                _logger.Log_Interval(2, "No Main Camera to RayCast from", true, this);
                return false;
            }

            if (NeedsGrid)
            {
                ProcessGridDrag();
                return true;
            }
            
            var ray = PrepareRay(cam.ScreenPointToRay(mousePos));

            if (invertRayCast && meshRenderer) {
                ray.origin = ray.GetPoint(meshRenderer.bounds.max.magnitude * 1.5f);
                ray.direction = -ray.direction;
            }

            RaycastHit hit;
            return Physics.Raycast(ray, out hit, float.MaxValue) && ProcessHit(hit, st);
        }

        public Ray PrepareRay(Ray ray)
        {
            var id = ImgMeta;

            var br = GlobalBrush;
            if (br.showBrushDynamics)
                GlobalBrush.brushDynamic.OnPrepareRay(this, br, ref ray);

            if (id == null || !invertRayCast || !meshRenderer || IsUiGraphicPainter) return ray;

            ray.origin = ray.GetPoint(meshRenderer.bounds.max.magnitude * 1.5f);
            ray.direction = -ray.direction;

            return ray;
        }

        private void ProcessGridDrag()
        {
            stroke.posTo = GridNavigator.onGridPos;
            PreviewShader_StrokePosition_Update();
        }

        private bool ProcessHit(RaycastHit hit, StrokeVector st)
        {
    

            var subMesh = this.GetMesh().GetSubMeshNumber(hit.triangleIndex);
            if (subMesh != selectedSubMesh)
            {
                if (autoSelectMaterialByNumberOfPointedSubMesh)
                {
                    SetOriginalShaderOnThis();

                    selectedSubMesh = subMesh;
                    OnChangedTexture_OnMaterial();

                    CheckPreviewShader();
                }
            }

            if (ImgMeta == null) return false;

            st.posTo = hit.point;
            st.collisionNormal = hit.normal;
            st.unRepeatedUv = OffsetAndTileUv(hit);
            st.uvTo = st.unRepeatedUv.To01Space();
            
            PreviewShader_StrokePosition_Update();

            return true;
        }

        private Vector2 OffsetAndTileUv(RaycastHit hit)
        {
            var id = ImgMeta;

            if (id == null) return hit.textureCoord;

            var uv = id.useTexCoord2 ? hit.textureCoord2 : hit.textureCoord;

            foreach (var p in Modules)
                if (p.OffsetAndTileUv(hit, this, ref uv))
                    return uv;

            uv.Scale(id.tiling);
            uv += id.offset;

            return uv;
        }
        
        private void ProcessMouseDrag(bool control)
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

                var id = ImgMeta;
                
                if (id == null) return;
                
                if (stroke.mouseDwn)
                    id.Backup();

                if (IsTerrainHeightTexture && stroke.mouseUp)
                    Preview_To_UnityTerrain();
                
            }
        }

        public void SampleTexture(Vector2 uv)
        {
            TexMgmt.OnBeforeBlitConfigurationChange();
            GlobalBrush.mask.SetValuesOn(ref GlobalBrush.Color, ImgMeta.SampleAt(uv));
            Update_Brush_Parameters_For_Preview_Shader();
        }

        public void AfterStroke(StrokeVector st) {
            st.SetPreviousValues();
            st.firstStroke = false;
            st.mouseDwn = false;
            if (ImgMeta.TargetIsTexture2D())
                ImgMeta.pixelsDirty = true;
        }

        private bool CanPaintOnMouseDown() =>  ImgMeta.TargetIsTexture2D() || GlobalBrushType.StartPaintingTheMomentMouseIsDown;
  
        #endregion

        #region PreviewMGMT

        public static Material previewHolderMaterial;
        public static Shader previewHolderOriginalShader;

        public bool NotUsingPreview => !previewHolderMaterial || previewHolderMaterial != Material; 

        private  void CheckPreviewShader()
        {
            if (MatDta == null)
                return;
            if (!IsCurrentTool || (LockTextureEditing && !IsEditingThisMesh))
                SetOriginalShaderOnThis();
            else if (MatDta.usePreviewShader && NotUsingPreview)
                SetPreviewShader();
        }

        private  void SetPreviewShader()
        {
            var mat = Material;

            if (previewHolderMaterial)
            {
                if (previewHolderMaterial != mat)
                    CheckSetOriginalShader();
                else
                    return;
            }

            if (meshEditing && (MeshManager.target != this))
                return;

            var tex = ImgMeta.CurrentTexture();

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
                shd = Cfg.previewMesh;
            else
            {
                if (terrain) shd = Cfg.previewTerrain;
                else
                {

                    foreach (var pl in PainterSystemManagerModuleBase.BrushPlugins)
                    {
                        var ps = pl.GetPreviewShader(this);
                        if (!ps) continue;
                         
                        shd = ps; 
                        break; 
                    }

                    if (!shd)
                        shd = Cfg.previewBrush;
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
                CheckSetOriginalShader();
        }

        public static void CheckSetOriginalShader()
        {
            if (!previewHolderMaterial)
                return;
            
            previewHolderMaterial.shader = previewHolderOriginalShader;
            previewHolderOriginalShader = null;
            previewHolderMaterial = null;
            
            if (TexMgmt)
                TexMgmt.FinalizePreviousAlphaDataTarget();

        }

        #endregion

        #region  Texture MGMT 

        private void UpdateTilingFromMaterial()
        {

            var id = ImgMeta;

            var fieldName = GetMaterialTextureProperty;
            var mat = Material;
            if (!NotUsingPreview && !terrain)
            {
                id.tiling = mat.GetTiling(PainterDataAndConfig.PreviewTexture);
                id.offset = mat.GetOffset(PainterDataAndConfig.PreviewTexture);
                return;
            }

            
            if (fieldName != null)
             foreach (var nt in Modules)
                if (nt.UpdateTilingFromMaterial(fieldName, this))
                    return;

             if (!mat || fieldName == null || id == null) return;

            id.tiling = mat.GetTiling(fieldName);
            id.offset = mat.GetOffset(fieldName);
        }

        public void UpdateTilingToMaterial()
        {
            var id = ImgMeta;
            var fieldName = GetMaterialTextureProperty;
            var mat = Material;
            if (!NotUsingPreview && !terrain)
            {
                mat.SetTiling(PainterDataAndConfig.PreviewTexture, id.tiling);
                mat.SetOffset(PainterDataAndConfig.PreviewTexture, id.offset);
                return;
            }
            
                foreach (var nt in Modules)
                    if (nt.UpdateTilingToMaterial(fieldName, this))
                        return;

            if (!mat || fieldName == null || id == null) return;
            mat.SetTiling(fieldName, id.tiling);
            mat.SetOffset(fieldName, id.offset);
        }

        private  void OnChangedTexture_OnMaterial()
        {
            if (NotUsingPreview || !terrain)
                ChangeTexture(GetTextureOnMaterial());
        }

        public void ChangeTexture(ImageMeta id) => ChangeTexture(id.CurrentTexture());
        
        private  void ChangeTexture(Texture texture)
        {

            textureWasChanged = false;

#if UNITY_EDITOR

            var t2D = texture as Texture2D;

            if (t2D) {
                var imp = t2D.GetTextureImporter();
                if (imp)
                {

                    var assetPath = AssetDatabase.GetAssetPath(texture);
                    var extension = assetPath.Substring(assetPath.LastIndexOf(".", StringComparison.Ordinal) + 1);

                    if (extension != "png")
                    {
                        #if !NO_PEGI
                        "Converting {0} to .png".F(assetPath).showNotificationIn3D_Views();
                        #endif
                        texture = t2D.CreatePngSameDirectory(t2D.name);
                    }

                }
            }
            #endif

            var field = GetMaterialTextureProperty;

            if (!texture)
            {
                RemoveTextureFromMaterial(); //SetTextureOnMaterial((Texture)null);
                return;
            }

            var id = texture.GetImgDataIfExists();

            if (id == null)
            {
                id = new ImageMeta().Init(texture);
                id.useTexCoord2 = field.NameForDisplayPEGI.Contains(PainterDataAndConfig.isUV2DisaplyNameTag);
            }

            SetTextureOnMaterial(texture);

            UpdateOrSetTexTarget(id.destination);

            UpdateTilingFromMaterial();

        }

        public PlaytimePainter SetTexTarget(BrushConfig br)
        {
            if (ImgMeta.TargetIsTexture2D() != br.targetIsTex2D)
                UpdateOrSetTexTarget(br.targetIsTex2D ? TexTarget.Texture2D : TexTarget.RenderTexture);

            return this;
        }

        public void UpdateOrSetTexTarget(TexTarget dst)
        {

            InitIfNotInitialized();

            var id = ImgMeta;

            if (id == null)
                return;

            if (id.destination == dst)
                return;

            id.ChangeDestination(dst, GetMaterial(true).GetMaterialPainterMeta(), GetMaterialTextureProperty, this);
            CheckPreviewShader();

        }

        private  void ReEnableRenderTexture()
        {
            if (LockTextureEditing) return;
            
            OnEnable();

            OnChangedTexture_OnMaterial();

            if (ImgMeta != null)
                UpdateOrSetTexTarget(TexTarget.RenderTexture); // set it to Render Texture
            
        }

        private  void CreateTerrainHeightTexture(string newName)
        {

            var field = GetMaterialTextureProperty;

            if (!field.Equals(PainterDataAndConfig.TerrainHeight))
            {
                Debug.Log("Terrain height is not currently selected.");
                return;
            }

            var size = terrain.terrainData.heightmapResolution - 1;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);

            var id = ImgMeta;

            if (id != null)
                id.From(texture);
            else
                ChangeTexture(texture);

            id = ImgMeta;

            id.saveName = newName;
            texture.name = id.saveName;
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

            var id = ImgMeta;

            var gotRenderTextureData = id != null && size == id.width && size == id.width && id.TargetIsRenderTexture();

            var texture = new Texture2D(size, size, TextureFormat.ARGB32, true, !isColor);

            if (gotRenderTextureData && (!id.texture2D || textureName.SameAs(id.saveName)))
                id.texture2D = texture;

            texture.wrapMode = TextureWrapMode.Repeat;

            ChangeTexture(texture);

            id = ImgMeta;

            id.saveName = textureName;
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
                id.SetPixelsInRam();

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

            ImgMeta.ApplyToTexture2D();

        }

        private  void CreateRenderTexture(int size, string renderTextureName)
        {
            var previous = ImgMeta;

            var nt = new ImageMeta().Init(size);

            nt.saveName = renderTextureName;

            ChangeTexture(nt.renderTexture);

            PainterCamera.Inst.Render(previous.CurrentTexture(), nt);

            UpdateOrSetTexTarget(TexTarget.RenderTexture);

        }

        #endregion

        #region Material MGMT

        public Material[] Materials {
            get
            {

                if (!terrain && !uiGraphic)
                    return meshRenderer.sharedMaterials;

                var mat = Material;

                return mat ? new[] {mat} : null;
            }
            set
            {
                if (meshRenderer)
                    meshRenderer.sharedMaterials = value;
                else if (uiGraphic)
                    uiGraphic.material = value.TryGet(0);
                else if (terrain)
                    terrain.materialTemplate = value.TryGet(0);
            }
        }

        public List<string> GetMaterialsNames() => Materials.Select((mt, i) => mt ? mt.name : "Null material {0}".F(i)).ToList();
        
        private List<ShaderProperty.TextureValue> GetMaterialTextureNames()
        {

            #if UNITY_EDITOR

            if (MatDta == null)
                return new List<ShaderProperty.TextureValue>();

            if (!NotUsingPreview)
                return MatDta.materialsTextureFields;

            MatDta.materialsTextureFields.Clear();
            
            foreach (var nt in Modules)
                if (nt != null)
                nt.GetNonMaterialTextureNames(this, ref MatDta.materialsTextureFields);

            if (!terrain)
                MatDta.materialsTextureFields.AddRange(Material.MyGetTextureProperties());
            else
            {
                var tmp = Material.MyGetTextureProperties();

                foreach (var t in tmp)
                    if ((!t.NameForDisplayPEGI.Contains("_Splat")) && (!t.NameForDisplayPEGI.Contains("_Normal")))
                        MatDta.materialsTextureFields.Add(t);
                
            }
#endif

            return MatDta.materialsTextureFields;
        }

        public ShaderProperty.TextureValue GetMaterialTextureProperty => GetMaterialTextureNames().TryGet(SelectedTexture);

        private Texture GetTextureOnMaterial()
        {

            if (!NotUsingPreview)
            {
                if (meshEditing) return null;
                if (!terrain)
                {
                    var m = Material;
                    return m ? Material.Get(PainterDataAndConfig.PreviewTexture) : null;
                }
            }

            var fieldName = GetMaterialTextureProperty;

            if (fieldName == null)
                return null;
            
            foreach (var t in Modules) {
                Texture tex = null;
                if (t.GetTexture(fieldName, ref tex, this))
                    return tex;
            }

            return Material.Get(fieldName);
        }

        private Material GetMaterial(bool original = false)
        {

            Material result = null;

            if (original)
                CheckSetOriginalShader();

            if (meshRenderer) {
                if (meshRenderer.sharedMaterials.ClampIndexToLength(ref selectedSubMesh))
                    result = meshRenderer.sharedMaterials[selectedSubMesh];
            }
            else if (uiGraphic)
                result = uiGraphic.material;
            else
                result = terrain ? terrain.materialTemplate : null;


            return result;
        }

        private void RemoveTextureFromMaterial() => SetTextureOnMaterial(GetMaterialTextureProperty, null);

        public void SetTextureOnMaterial(ImageMeta id) => SetTextureOnMaterial(GetMaterialTextureProperty, id.CurrentTexture());

        public ImageMeta SetTextureOnMaterial(Texture tex) => SetTextureOnMaterial(GetMaterialTextureProperty, tex);

        private ImageMeta SetTextureOnMaterial(ShaderProperty.TextureValue property, Texture tex)
        {
            var id = SetTextureOnMaterial(property, tex, GetMaterial(true));
            CheckPreviewShader();
            return id;
        }

        public ImageMeta SetTextureOnMaterial(ShaderProperty.TextureValue property, Texture tex, Material mat)
        {

            var id = tex.GetImgData();

            if (property != null)
            {
                if (id != null)
                    Cfg.recentTextures.AddIfNew(property, id);
                
                foreach (var nt in Modules)
                    if (nt.SetTextureOnMaterial(property, id, this))
                        return id;
            }

            if (!mat) return id;
            if (property != null)
                mat.Set(property, id.CurrentTexture());

            if (!NotUsingPreview && (!terrain))
                SetTextureOnPreview(id.CurrentTexture());

            return id;
        }

        private void SetTextureOnPreview(Texture tex)
        {

            if (meshEditing) return;
        
            var mat = Material;
            var id = tex.GetImgData();

            PainterDataAndConfig.PreviewTexture.SetOn(mat, id.CurrentTexture());
            
            if (id == null) return;
            
            mat.SetOffset(PainterDataAndConfig.PreviewTexture, id.offset);
            mat.SetTiling(PainterDataAndConfig.PreviewTexture, id.tiling);
               
        }

        public Material InstantiateMaterial(bool saveIt)
        {

            CheckSetOriginalShader();

            if (ImgMeta != null && Material)
                UpdateOrSetTexTarget(TexTarget.Texture2D);

            if (!TexMgmt.defaultMaterial) InitIfNotInitialized();

            var mat = GetMaterial(true);

            if (!mat && terrain)
            {
                mat = new Material(Cfg.previewTerrain);

                terrain.materialTemplate = mat;
                terrain.materialType = Terrain.MaterialType.Custom;
                mat.name += "_Terrain material";
            }
            else
            {
                Material = Instantiate(mat ? mat : TexMgmt.defaultMaterial);
                CheckPreviewShader();
            }

            var material = Material;

            if (material)
            {
                material.name = gameObject.name;

                if (saveIt)
                {
                    #if UNITY_EDITOR
                    material.SaveAsset(Cfg.materialsFolderName, ".mat", true);
                    CheckPreviewShader();
                    #endif
                }
            }

            OnChangedTexture_OnMaterial();

            var id = ImgMeta;

            if (id != null && Material)
                UpdateOrSetTexTarget(id.destination);
#if !NO_PEGI
            "Instantiating Material on {0}".F(gameObject.name).showNotificationIn3D_Views();
#endif
            return Material;


        }

        #endregion

        #region Terrain_MGMT

        public float tilingY = 8;

        public void UpdateShaderGlobals() {
       
            foreach (var nt in Modules)
                nt.OnUpdate(this);
        }

        public void UpdateTerrainPosition() => PainterDataAndConfig.TerrainPosition.GlobalValue = transform.position.ToVector4(tilingY);

        private void Preview_To_UnityTerrain()
        {

            var id = ImgMeta;

            if (id == null)
                return;

            var rendTex = id.TargetIsRenderTexture();

            if (rendTex)
                UpdateOrSetTexTarget(TexTarget.Texture2D);

            var td = terrain.terrainData;

            var res = td.heightmapResolution - 1;

            var conversion = (id.width / (float)res);

            var heights = td.GetHeights(0, 0, res + 1, res + 1);

            var cols = id.Pixels;

            if (Math.Abs(conversion - 1) > float.Epsilon)
                for (var y = 0; y < res; y++)
                {
                    var yInd = id.width * Mathf.FloorToInt((y * conversion));
                    for (var x = 0; x < res; x++)
                        heights[y, x] = cols[yInd + (int)(x * conversion)].a;

                }
            else
                for (var y = 0; y < res; y++)
                {
                    var yInd = id.width * y;

                    for (var x = 0; x < res; x++)
                        heights[y, x] = cols[yInd + x].a;
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

            var oid = ImgMeta;

            var id = terrainHeightTexture.GetImgData();

            var current = id == oid;
            var rendTex = current && oid.TargetIsRenderTexture();

            if (rendTex)
                UpdateOrSetTexTarget(TexTarget.Texture2D);

            var td = terrain.terrainData;
            
            var textureSize = td.heightmapResolution - 1;

            if (id.width != textureSize)
            {
                Debug.Log("Wrong size: {0} textureSize {1}".F(id.width, id.texture2D.width));
                if (current)
                    CreateTerrainHeightTexture(oid.saveName);
                else Debug.Log("Is not current");

                return;
                
            }

            terrainHeightTexture = id.texture2D;
            var col = id.Pixels;

            var height = 1f / td.size.y;

            for (var y = 0; y < textureSize; y++)
            {
                var fromY = y * textureSize;

                for (var x = 0; x < textureSize; x++)
                {
                    var tmpCol = new Color();

                    var dx = ((float)x) / textureSize;
                    var dy = ((float)y) / textureSize;

                    var v3 = td.GetInterpolatedNormal(dx, dy);// + Vector3.one * 0.5f;

                    tmpCol.r = v3.x + 0.5f;
                    tmpCol.g = v3.y + 0.5f;
                    tmpCol.b = v3.z + 0.5f;
                    tmpCol.a = td.GetHeight(x, y) * height;

                    col[fromY + x] = tmpCol;
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
                
                var propName = GetMaterialTextureProperty;
                return propName?.Equals(PainterDataAndConfig.TerrainHeight) ?? false;
            }
        }

        public TerrainHeightModule GetTerrainHeight() => Modules.GetInstanceOf<TerrainHeightModule>();
       
        public bool IsTerrainControlTexture => ImgMeta != null && terrain && GetMaterialTextureProperty.HasUsageTag(PainterDataAndConfig.TERRAIN_CONTROL_TEXTURE);

        #endregion

        #region Saving

        #if UNITY_EDITOR

        private void ForceReimportMyTexture(string path)
        {

            var importer = AssetImporter.GetAtPath("Assets{0}".F(path)) as TextureImporter;
            if (importer == null)
            {
                Debug.Log("No importer for {0}".F(path));
                return;
            }

            var id = ImgMeta;

            TexMgmt.TryDiscardBufferChangesTo(id);

            importer.SaveAndReimport();
            if (id.TargetIsRenderTexture())
                id.TextureToRenderTexture(id.texture2D);
            else if (id.texture2D)
                id.PixelsFromTexture2D(id.texture2D);

            SetTextureOnMaterial(id);
        }

        private bool TextureExistsAtDestinationPath() =>
            AssetImporter.GetAtPath(Path.Combine("Assets",GenerateTextureSavePath())) as TextureImporter != null;

        private string GenerateTextureSavePath() =>
            Path.Combine(Cfg.texturesFolderName, ImgMeta.saveName + ".png");
        
        private bool OnBeforeSaveTexture(ImageMeta id) {
          
            if (id.TargetIsRenderTexture()) 
                id.RenderTexture_To_Texture2D();

            var tex = id.texture2D;

            if (id. preserveTransparency && !tex.TextureHasAlpha()) {
                
                ChangeTexture(id.NewTexture2D());
                
                Debug.Log("Old Texture had no Alpha channel, creating new");

                id.texture2D = id.texture2D.SaveTextureAsAsset(Cfg.texturesFolderName, ref id.saveName, false);

                id.texture2D.CopyImportSettingFrom(tex).Reimport_IfNotReadale();

                return false;
            }

            id.SetAlphaSavePixel();

            return true;
        }

        private void OnPostSaveTexture(ImageMeta id)
        {
            SetTextureOnMaterial(id);
            UpdateOrSetTexTarget(id.destination);
            UpdateShaderGlobals();

            id.UnsetAlphaSavePixel();
        }

        private void RewriteOriginalTexture_Rename(string texName) {

            var id = ImgMeta;

            if (!OnBeforeSaveTexture(id)) return;
            
            id.texture2D = id.texture2D.RewriteOriginalTexture_NewName(texName);

            OnPostSaveTexture(id);
            
        }

        private void RewriteOriginalTexture() {
            var id = ImgMeta;

            if (!OnBeforeSaveTexture(id)) return;
            
            id.texture2D = id.texture2D.RewriteOriginalTexture();
            OnPostSaveTexture(id);
            
        }

        private void SaveTextureAsAsset(bool asNew) {

            var id = ImgMeta;

            if (OnBeforeSaveTexture(id)) {
                id.texture2D = id.texture2D.SaveTextureAsAsset(Cfg.texturesFolderName, ref id.saveName, asNew);

                id.texture2D.Reimport_IfNotReadale();
            }

            OnPostSaveTexture(id);
        }

        public void SaveMesh() {
            var m = this.GetMesh();
            var path = AssetDatabase.GetAssetPath(m);

            var folderPath = Path.Combine(Application.dataPath, Cfg.meshesFolderName);
            Directory.CreateDirectory(folderPath);

            try {
                if (path.Length > 0)
                    SharedMesh = Instantiate(SharedMesh);

                var sm = SharedMesh;
                
                Directory.CreateDirectory(Path.Combine("Assets", Cfg.meshesFolderName));

                AssetDatabase.CreateAsset(sm, Path.Combine("Assets",MeshManager.GenerateMeshSavePath()));

                AssetDatabase.SaveAssets();

                UpdateMeshCollider();

                //if (meshCollider && !meshCollider.sharedMesh && sm)
                  //  meshCollider.sharedMesh = sm;

            }
            catch (Exception ex)  {
                Debug.LogError(ex);
            }
        }

        #endif

        #endregion

        #region COMPONENT MGMT 

        public bool LockTextureEditing
        {
            get
            {
                if (meshEditing || !TexMgmt)
                    return true;
                var i = ImgMeta; 
                return i == null || i.lockEditing || i.other;
            }
            set { var i = ImgMeta; if (i != null) i.lockEditing = value; }
        }
        public bool forcedMeshCollider;
        [NonSerialized]
        public bool initialized;
        public bool autoSelectMaterialByNumberOfPointedSubMesh = true;

        public const string OnlineManual = "https://docs.google.com/document/d/170k_CE-rowVW9nsAo3EUqVIAWlZNahC0ua11t1ibMBo/edit?usp=sharing";

        private static readonly List<string> TextureEditorIgnore = new List<string> { MeshManager.VertexEditorUiElementTag, MeshManager.ToolComponentTag, "o" };

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
            var allObjects = FindObjectsOfType<Renderer>();
            foreach (var mr in allObjects)
            {
                var ip = mr.GetComponent<PlaytimePainter>();
                if (ip)
                    DestroyImmediate(ip);
            }

            var rtp = FindObjectsOfType<PainterCamera>();

            if (!rtp.IsNullOrEmpty())
                foreach (var rt in rtp)
                    rt.gameObject.DestroyWhatever();

            var dc = FindObjectsOfType<DepthProjectorCamera>();

            if (!dc.IsNullOrEmpty())
                foreach (var d in dc)
                    d.gameObject.DestroyWhatever();

            PainterSystem.applicationIsQuitting = false;
        }

#if !NO_PEGI
        [MenuItem("Tools/" + PainterDataAndConfig.ToolName + "/Join Discord")]
        public static void Open_Discord() => Application.OpenURL(pegi.PopUpService.DiscordServer);
        
        [MenuItem("Tools/" + PainterDataAndConfig.ToolName + "/Send an Email")]
        public static void Open_Email() => QcUnity.SendEmail(pegi.PopUpService.SupportEmail, "About your Playtime Painter",
            "Hello Yuri, we need to talk. I purchased your asset and expect an excellent quality, but ...");
#endif

        [MenuItem("Tools/" + PainterDataAndConfig.ToolName + "/Open Manual")]
        public static void OpenWWW_Documentation() => Application.OpenURL(OnlineManual);

#endif

        private void OnDestroy() {

            var colliders = GetComponents<Collider>();

            foreach (var c in colliders)
                if (c.GetType() != typeof(MeshCollider)) c.enabled = true;

            colliders = GetComponentsInChildren<Collider>();

            foreach (var c in colliders)
                if (c.GetType() != typeof(MeshCollider)) c.enabled = true;

            if (forcedMeshCollider && meshCollider)
            {
                meshCollider.enabled = false; //DestroyWhateverComponent();
                forcedMeshCollider = false;
            }
        }


        private void OnDisable()
        {

      

            CheckSetOriginalShader();
            
            initialized = false; // Should be before restoring to texture2D to avoid Clear to black.
            
            if (Application.isPlaying) {

                var id = GetTextureOnMaterial().GetImgDataIfExists();

                if (id != null && id.CurrentTexture().IsBigRenderTexturePair())
                    UpdateOrSetTexTarget(TexTarget.Texture2D);
            }
            
            this.SaveStdData();
            
            if (!TexMgmt || MeshManager.target != this) return;
            
            MeshManager.Inst.DisconnectMesh();
            
        }

        public void OnEnable() {

            PainterSystem.applicationIsQuitting = false;
            
            if (terrain)
                UpdateShaderGlobals();

            if (!meshRenderer)
                meshRenderer = GetComponent<MeshRenderer>();

            this.LoadStdData();
            
        }

        private void UpdateColliderForSkinnedMesh() {

            if (!colliderForSkinnedMesh)
            {
                colliderForSkinnedMesh = new Mesh
                {
                    name = "Generated Collider for " + name
                };
            }

            skinnedMeshRenderer.BakeMesh(colliderForSkinnedMesh);

            try
            {
                if (meshCollider)
                {
                    meshCollider.sharedMesh = null;
                    meshCollider.sharedMesh = colliderForSkinnedMesh;
                }
            }
            catch (Exception ex) {
                _logger.Log_Interval(1000, ex.ToString(), true, this);
            }
        }

        public void InitIfNotInitialized()
        {

            if (!(!initialized || ((!meshCollider || !meshRenderer) && (!terrain || !terrainCollider)))) return;
            
            initialized = true;

            nameHolder = gameObject.name;

            if (!meshRenderer)
                meshRenderer = GetComponent<Renderer>();

            if (!uiGraphic)
                uiGraphic = GetComponent<Graphic>();
            
            if (meshRenderer)
            {

                var colliders = GetComponents<Collider>();

                foreach (var c in colliders)
                    if (c.GetType() != typeof(MeshCollider)) c.enabled = false;

                colliders = GetComponentsInChildren<Collider>();

                foreach (var c in colliders)
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

            if (meshRenderer && (meshRenderer.GetType() == typeof(SkinnedMeshRenderer))) {
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

            if ((this == TexMgmt.autodisabledBufferTarget) && (!LockTextureEditing) && (!QcUnity.ApplicationIsAboutToEnterPlayMode()))
                ReEnableRenderTexture();

        }

        private void FocusOnThisObject()
        {

#if UNITY_EDITOR
            QcUnity.FocusOn(gameObject);
#endif
            selectedInPlaytime = this;
            //TexMgmt.OnBeforeBlitConfigurationChange();
            Update_Brush_Parameters_For_Preview_Shader();
            InitIfNotInitialized();
        }

        #endregion

        #region Inspector 

        public void OnGUI()
        {
            
            if (!Cfg || !Cfg.enablePainterUIonPlay) return;
            
            if (!selectedInPlaytime)
                selectedInPlaytime = this;
            #if !NO_PEGI
            if (selectedInPlaytime == this)  {
                WindowPosition.Render(this, Inspect, "{0} {1}".F(gameObject.name, GetMaterialTextureProperty));

                foreach (var p in PainterSystemManagerModuleBase.GuiPlugins)
                    p.OnGUI();
            }
            #endif
      

        }

        public static PlaytimePainter inspected;

        [NonSerialized] public readonly Dictionary<int, ShaderProperty.TextureValue> loadingOrder = new Dictionary<int, ShaderProperty.TextureValue>();

        public static PlaytimePainter selectedInPlaytime;

        #if !NO_PEGI
        private static readonly pegi.WindowPositionData_PEGI_GUI WindowPosition = new pegi.WindowPositionData_PEGI_GUI();

        private const string DefaultImageLoadUrl = "https://picsbuffet.com/pixabay/";

        private static string _tmpUrl = DefaultImageLoadUrl;

        private static int _inspectedFancyItems = -1;

        public static int _inspectedMeshEditorItems = -1;

        private static int inspectedShowOptionsSubitem = -1;

        public bool Inspect() {
            
            #if UNITY_2019_1_OR_NEWER && UNITY_EDITOR
            if (!Application.isPlaying && !IsCurrentTool)
            {
                if (ActiveEditorTracker.sharedTracker.isLocked) 
                    pegi.Lock_UnlockWindowClick(gameObject);

                MsgPainter.PleaseSelect.GetText().writeHint();

                SetOriginalShaderOnThis();

                return false;
            }
            #endif

            inspected = this;
           
             var changed = false;

            if (!TexMgmt && "Find camera".Click())
                    PainterSystem.applicationIsQuitting = false;

            var canInspect = true;

            if (!TexMgmt)
                canInspect = false;
            else if (!Cfg) {
                
                TexMgmt.DependenciesInspect().changes(ref changed);
                
                canInspect = false;
            }

            if (canInspect && QcUnity.IsPrefab(gameObject)) {
                "Inspecting a prefab.".nl();
                canInspect = false;
            }

            if (canInspect && !IsCurrentTool)
            {


                if ( icon.Off.Click("Click to Enable Tool").changes(ref changed))
                {
                    IsCurrentTool = true;
                    enabled = true;

                    #if UNITY_EDITOR
                    var cs = GetComponents(typeof(Component));

                    foreach (var c in cs)
                        if (c.GetType() != typeof(PlaytimePainter))
                            UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(c, false);

                    QcUnity.FocusOn(null);
                    PainterCamera.refocusOnThis = gameObject;
                    #endif

                    CheckPreviewShader();
                }

                pegi.Lock_UnlockWindowClick(gameObject);

                canInspect = false;
            }
            
            if (canInspect) {

                TexMgmt.focusedPainter = this;
                
                if (
                    #if UNITY_2019_1_OR_NEWER
                    Application.isPlaying && (
                    #else 
                    (
                    #endif
                    
                    #if UNITY_EDITOR
                    (IsCurrentTool && terrain && !Application.isPlaying &&
                     UnityEditorInternal.InternalEditorUtility.GetIsInspectorExpanded(terrain)) ||
                    #endif
                    icon.On.Click("Click to Disable Tool")))
                {
                    IsCurrentTool = false;
                    WindowPosition.Collapse();
                    MeshManager.Inst.DisconnectMesh();
                    SetOriginalShaderOnThis();
                    UpdateOrSetTexTarget(TexTarget.Texture2D);
                   
                }

                pegi.Lock_UnlockWindowClick(gameObject);

                InitIfNotInitialized();

                var image = ImgMeta;

                var texMgmt = TexMgmt;

                var cfg = Cfg;

                var tex = GetTextureOnMaterial();
                if (!meshEditing && ((tex && image == null) || (image != null && !tex) ||
                                     (image != null && tex != image.texture2D && tex != image.CurrentTexture())))
                    textureWasChanged = true;

                #region Top Buttons
                
                if (MeshManager.target && (MeshManager.target != this))
                    MeshManager.DisconnectMesh();

                if (!cfg.showConfig)
                {
                    if (meshEditing)
                    {
                        if (icon.Painter.Click("Edit Texture", ref changed))
                        {
                            CheckSetOriginalShader();
                            meshEditing = false;
                            CheckPreviewShader();
                            MeshMgmt.DisconnectMesh();
                            cfg.showConfig = false;
                            "Editing Texture".showNotificationIn3D_Views();
                        }
                    }
                    else
                    {
                        if (icon.Mesh.Click("Edit Mesh", ref changed))
                        {
                            meshEditing = true;

                            CheckSetOriginalShader();
                            UpdateOrSetTexTarget(TexTarget.Texture2D);
                            cfg.showConfig = false;
                            "Editing Mesh".showNotificationIn3D_Views();

                            if (SavedEditableMesh != null)
                                MeshMgmt.EditMesh(this, false);
                        }
                    }
                }

                pegi.toggle(ref cfg.showConfig, meshEditing ? icon.Mesh : icon.Painter, icon.Config, "Tool Configuration");
                
                if (!PainterDataAndConfig.hideDocumentation)
                    pegi.fullWindowDocumentationClickOpen(PainterLazyTranslations.InspectPainterDocumentation, MsgPainter.AboutPlaytimePainter.GetText());
    
                #endregion
                
                if (cfg.showConfig) {

                    pegi.newLine();
                    
                      cfg.Nested_Inspect();
                    
                }
                else
                {

                    if (meshCollider)
                    {
                        if (!meshCollider.sharedMesh){
                            pegi.nl();
                            "Mesh Collider has no mesh".writeWarning();
                            if (meshFilter && meshFilter.sharedMesh &&
                                "Assign".Click("Will assign {0}".F(meshFilter.sharedMesh)))
                                meshCollider.sharedMesh = meshFilter.sharedMesh;

                            pegi.nl();
                        } else if (meshFilter && meshFilter.sharedMesh &&
                                   meshFilter.sharedMesh != meshCollider.sharedMesh) {
                            "Collider and filter have different meshes. Painting may not be able to obtain a correct UV coordinates."
                                .fullWindowWarningDocumentationClickOpen("Mesh collider mesh is different");
                        }
                        
                    }

                #region Mesh Editing

                    if (meshEditing) {

                        if (terrain) {
                            pegi.nl();
                            "Mesh Editor can't edit Terrain mesh".writeHint();

                        } else {

                            var mg = MeshMgmt;
                            mg.UndoRedoInspect().nl(ref changed);

                            var sm = SharedMesh;

                            if (sm) {

                                if (this != MeshManager.target)
                                    if (SavedEditableMesh != null)
                                        "Component has saved mesh data.".nl();
                                
                                "Warning, this will change (or mess up) your model.".writeOneTimeHint("MessUpMesh");

                                if (MeshManager.target != this) {

                                    var ent = gameObject.GetComponent($"pb_Entity");
                                    var obj = gameObject.GetComponent($"pb_Object");

                                    if (ent || obj)
                                        "PRO builder detected. Strip it using Actions in the Tools/ProBuilder menu."
                                            .writeHint();
                                    else {
                                        if (Application.isPlaying)
                                            "Playtime Changes will be reverted once you try to edit the mesh again."
                                                .writeWarning();

                                        pegi.newLine();

                                        "Mesh has {0} vertices".F(sm.vertexCount).nl();

                                        if (sm.vertexCount > 2000)
                                            "The mesh is really complex, mesh editor may be really slow at times".writeWarning();


                                        pegi.nl();

                                        const string confirmTag = "pp_EditThisMesh";

                                        if (!pegi.IsConfirmingRequestedFor(confirmTag)) {
                                            if ("Copy & Edit".Click())
                                                mg.EditMesh(this, true);

                                            if ("New Mesh".Click()) {
                                                Mesh = new Mesh();
                                                SavedEditableMesh = null;
                                                mg.EditMesh(this, false);
                                            }
                                        }

                                        if ("Edit this".ClickConfirm(confirmTag, "It is recommended to edit a copy of a mesh and then save it as new mesh. Are you sure you want to edit the original one?").nl())
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

                            if (IsEditingThisMesh)
                            {

                                if (_inspectedMeshEditorItems == -1)
                                    MeshMgmt.Inspect().nl();

                                if ("Profile".enter( ref _inspectedMeshEditorItems, 0)) {

                                    MsgPainter.MeshProfileUsage.DocumentationClick();


                                    if ((cfg.meshPackagingSolutions.Count > 1) && icon.Delete.Click(25))
                                        cfg.meshPackagingSolutions.RemoveAt(selectedMeshProfile);
                                    else {

                                        pegi.newLine();
                                        if (MeshProfile.Inspect().nl())
                                            MeshManager.editedMesh.Dirty = true;

                                       
                                    }
                                }
                                else if (_inspectedMeshEditorItems == -1)
                                {
                                    if (" : ".select_Index(20, ref selectedMeshProfile, cfg.meshPackagingSolutions, true) &&
                                        IsEditingThisMesh)
                                        MeshManager.editedMesh.Dirty = true;

                                    if (icon.Add.Click(25).nl())
                                    {
                                        cfg.meshPackagingSolutions.Add(new MeshPackagingProfile());
                                        selectedMeshProfile = cfg.meshPackagingSolutions.Count - 1;
                                        MeshProfile.name = "New Profile {0}".F(selectedMeshProfile);
                                    }
                                }

                                MeshManager.MeshOptionsInspect();
                            }
                        }

                        pegi.newLine();

                    }

                #endregion
                    
                #region Texture Editing

                    else
                    {

                        var id = ImgMeta;

                        var painterWorks = Application.isPlaying || !IsUiGraphicPainter;

                        if (!LockTextureEditing && painterWorks && !id.errorWhileReading)
                        {

                            texMgmt.DependenciesInspect().changes(ref changed);

                #region Undo/Redo & Recording

                            id.Undo_redo_PEGI();
                            
                            pegi.nl();

                            var cpu = id.TargetIsTexture2D();

                            var mat = Material;
                            if (mat.IsProjected())
                            {

                                "Projected UV Shader detected. Painting may not work properly".writeWarning();
                                if ("Undo".Click().nl())
                                    mat.DisableKeyword(PainterDataAndConfig.UV_PROJECTED);
                            }
                            

                #endregion

                #region Brush

                            GlobalBrush.Nested_Inspect().changes(ref changed);

                            if (!cpu && id.texture2D && id.width != id.height)
                                icon.Warning.write("Non-square texture detected! Every switch between GPU and CPU mode will result in loss of quality.");

                            var mode = GlobalBrush.GetBlitMode(cpu);
                            var col = GlobalBrush.Color;

                            if ((cpu || !mode.UsingSourceTexture || GlobalBrush.srcColorUsage != SourceTextureColorUsage.Unchanged) && !IsTerrainHeightTexture &&
                                !pegi.paintingPlayAreaGui) {
                                if (pegi.edit(ref col).changes(ref changed))
                                    GlobalBrush.Color = col;

                                MsgPainter.SampleColor.DocumentationClick();

                            }
                            
                            pegi.nl();

                            if (!cfg.moreOptions)  {

                                GlobalBrush.ColorSliders().nl(ref changed);

                                if (cfg.showColorSchemes)
                                {

                                    var scheme = cfg.colorSchemes.TryGet(cfg.selectedColorScheme);

                                    scheme?.PickerPEGI();

                                    if (cfg.showColorSchemes)
                                        "Scheme".select_Index(60, ref cfg.selectedColorScheme, cfg.colorSchemes) .nl(ref changed);

                                }
                            }
                            
                #endregion

                        }
                        else
                        {
                            if (!NotUsingPreview)
                                PreviewShaderToggleInspect();

                            if (!painterWorks) {
                                pegi.nl();
                                "UI Element editing only works in Game View during Play.".writeWarning();
                            }
                        }
                        
                        id = ImgMeta;

                        if (meshCollider && meshCollider.convex)
                        {
                            "Convex mesh collider detected. Most brushes will not work".writeWarning();
                            if ("Disable convex".Click())
                                meshCollider.convex = false;
                        }


                #region Fancy Options

                        pegi.nl();
                        MsgPainter.TextureSettings.GetText().foldout(ref cfg.moreOptions);

                        if (id != null && !cfg.moreOptions)
                        {

                           pegi.edit(ref id.clearColor, 50);
                            if (icon.Clear.Click("Clear channels which are not ignored").changes(ref changed))
                            {
                                if (GlobalBrush.PaintingAllChannels)
                                {
                                    PainterCamera.Inst.DiscardAlphaBuffer();
                                    
                                    id.Colorize(id.clearColor);
                                    id.SetApplyUpdateRenderTexture();
                                }
                                else
                                {
                                    var wasRt = id.destination == TexTarget.RenderTexture;

                                    if (wasRt)
                                        UpdateOrSetTexTarget(TexTarget.Texture2D);

                                    id.SetPixels(id.clearColor, GlobalBrush.mask).SetAndApply();
                                    
                                    if (wasRt)
                                        UpdateOrSetTexTarget(TexTarget.RenderTexture);
                                }
                            }
                        }

                        pegi.nl();

                        var inspectionIndex = id?.inspectedItems ?? _inspectedFancyItems;

                        if (cfg.moreOptions)
                        {

                            if (icon.Show.enter("Optional UI Elements", ref inspectionIndex, 7).nl()) {

                                "Show Previous Textures (if any) "
                                    .toggleVisibilityIcon(
                                        "Will show textures previously used for this material property.",
                                        ref cfg.showRecentTextures, true).nl();

                                "Exclusive Render Textures"
                                    .toggleVisibilityIcon(
                                        "Allow creation of simple Render Textures - the have limited editing capabilities.",
                                        ref cfg.allowExclusiveRenderTextures, true).nl();

                                "Color Sliders ".toggleVisibilityIcon("Should the color slider be shown ",
                                    ref cfg.showColorSliders, true).nl(ref changed);

                                if ("Color Schemes".toggle_enter(ref cfg.showColorSchemes, ref inspectedShowOptionsSubitem, 5, ref changed).nl_ifFolded())
                                    cfg.InspectColorSchemes();
                                
                                if (id != null)  {

                                    foreach (var module in id.Modules)
                                        module.ShowHideSectionInspect().nl(ref changed);

                                    if (id.isAVolumeTexture)
                                        "Show Volume Data in Painter".toggleIcon(ref PainterCamera.Data.showVolumeDetailsInPainter).nl(ref changed);

                                }

                                "Brush Dynamics"
                                    .toggleVisibilityIcon("Will modify scale and other values based on movement.",
                                        ref GlobalBrush.showBrushDynamics, true).nl(ref changed);

                                "URL field".toggleVisibilityIcon("Option to load images by URL", ref cfg.showUrlField,
                                    true).changes(ref changed);
                            }

                            if ("New Texture ".conditional_enter(!IsTerrainHeightTexture, ref inspectionIndex, 4).nl()) {

                                if (cfg.newTextureIsColor)
                                    "Clear Color".edit(ref cfg.newTextureClearColor).nl(ref changed);
                                else
                                    "Clear Value".edit(ref cfg.newTextureClearNonColorValue).nl(ref changed);

                                "Color Texture".toggleIcon("Will the new texture be a Color Texture",
                                    ref cfg.newTextureIsColor).nl(ref changed);

                                "Size:".select_Index("Size of the new Texture", 40, ref PainterCamera.Data.selectedWidthIndex,
                                    PainterDataAndConfig.NewTextureSizeOptions).nl();

                                "Click + next to texture field below to create texture using this parameters".writeHint();

                                pegi.nl();

                            }

                            if (id != null)
                            {
                                id.inspectedItems = inspectionIndex;
                                id.Inspect().changes(ref changed);
                            }
                            else _inspectedFancyItems = inspectionIndex;

                        }


                        if (id != null)
                        {
                            var showToggles = (id.inspectedItems == -1 && cfg.moreOptions);

                            id.ComponentDependent_PEGI(showToggles, this).changes(ref changed);

                            if (showToggles || (!NotUsingPreview && cfg.previewAlphaChanel))
                            {
                                "Preview Edited RGBA".toggleIcon(ref cfg.previewAlphaChanel)
                                    .changes(ref changed);

                                MsgPainter.previewRGBA.DocumentationClick().nl();
                            }

                            if (showToggles)
                            {
                                var mats = Materials;
                                if (autoSelectMaterialByNumberOfPointedSubMesh || !mats.IsNullOrEmpty())
                                {
                                    "Auto Select Material".toggleIcon(
                                        "Material will be changed based on the subMesh you are painting on",
                                        ref autoSelectMaterialByNumberOfPointedSubMesh).changes(ref changed);
                                    
                                    MsgPainter.AutoSelectMaterial.DocumentationClick().nl();
                                }
                                
                                if (!IsUiGraphicPainter)
                                    "Invert RayCast" .toggleIcon("Will rayCast into the camera (for cases when editing from inside a sphere, mask for 360 video for example.)",
                                            ref invertRayCast).nl(ref changed);
                                else
                                    invertRayCast = false;

                            }

                            if (cfg.moreOptions)
                                pegi.line(Color.red);

                            if (id.enableUndoRedo && id.backupManually && "Backup for UNDO".Click())
                                id.Backup();

                            if (id.dontRedoMipMaps && icon.Refresh.Click("Update Mipmaps now").nl())
                                id.SetAndApply();
                        }

                #endregion

                     
                #region Save Load Options

                        if (HasMaterialSource && !cfg.showConfig)
                        {
                            #region Material Clonning Options

                            pegi.nl();

                            var mats = Materials;
                            if (!mats.IsNullOrEmpty())
                            {
                                var sm = selectedSubMesh;
                                if (pegi.select_Index(ref sm, mats))
                                {
                                    SetOriginalShaderOnThis();
                                    selectedSubMesh = sm;
                                    OnChangedTexture_OnMaterial();
                                    id = ImgMeta;
                                    CheckPreviewShader();
                                }
                            }

                            var mater = Material;

                            if (Application.isEditor && mater && mater.shader)
                                mater.shader.ClickHighlight("Highlight Shader");

                            if (pegi.edit(ref mater).changes(ref changed))
                                Material = mater;

                            if (icon.NewMaterial.Click("Instantiate Material").nl(ref changed))
                                InstantiateMaterial(true);

                            pegi.nl();
                            pegi.space();
                            pegi.nl();

                #endregion

                #region Texture Instantiation Options

                            if (cfg.showUrlField)
                            {

                                "URL".edit(40, ref _tmpUrl);
                                if (_tmpUrl.Length > 5 && icon.Download.Click())
                                {
                                    loadingOrder.Add(PainterCamera.DownloadManager.StartDownload(_tmpUrl),
                                        GetMaterialTextureProperty);
                                    _tmpUrl = DefaultImageLoadUrl;
                                    "Loading for {0}".F(GetMaterialTextureProperty).showNotificationIn3D_Views();
                                }

                                pegi.nl();
                                if (loadingOrder.Count > 0)
                                    "Loading {0} texture{1}".F(loadingOrder.Count, loadingOrder.Count > 1 ? "s" : "")
                                        .nl();

                                pegi.nl();

                            }


                            var ind = SelectedTexture;
                            if (pegi.select_Index(ref ind, GetMaterialTextureNames()).changes(ref changed))
                            {
                                SetOriginalShaderOnThis();
                                SelectedTexture = ind;
                                OnChangedTexture_OnMaterial();
                                CheckPreviewShader();
                                id = ImgMeta;
                                if (id == null)
                                    nameHolder = gameObject.name + "_" + GetMaterialTextureProperty;
                            }

                            if (id != null)
                            {
                                UpdateTilingFromMaterial();

                                if (id.errorWhileReading)
                                {

                                    icon.Warning.write(
                                        "THere was error while reading texture. (ProBuilder's grid texture is not readable, some others may be to)");

                                    if (id.texture2D && icon.Refresh.Click("Retry reading the texture"))
                                        id.From(id.texture2D, true);

                                }
                                else if (pegi.toggle(ref id.lockEditing, icon.Lock.GetIcon(), icon.Unlock.GetIcon(),
                                    "Lock/Unlock editing of {0} Texture.".F(id.GetNameForInspector()), 25))
                                {
                                    CheckPreviewShader();
                                    if (LockTextureEditing)
                                        UpdateOrSetTexTarget(TexTarget.Texture2D);
                                }
                            }

                            tex = GetTextureOnMaterial();

                            if (pegi.edit(ref tex).changes(ref changed))
                                ChangeTexture(tex);

                            if (!IsTerrainControlTexture)
                            {

                                var isTerrainHeight = IsTerrainHeightTexture;

                                var texScale = !isTerrainHeight
                                    ? cfg.SelectedWidthForNewTexture()
                                    : (terrain.terrainData.heightmapResolution - 1);

                                var texNames = GetMaterialTextureNames();

                                if (texNames.Count > SelectedTexture)
                                {
                                    var param = GetMaterialTextureProperty;

                                    const string newTexConfirmTag = "pp_nTex";

                                    if ((((id == null) && icon.NewTexture.Click("Create new texture2D for " + param)) || 
                                        (id != null && icon.NewTexture.ClickConfirm(newTexConfirmTag, id ,"Replace " + param + " with new Texture2D " + texScale + "*" + texScale) )).nl(ref changed))
                                    {
                                        if (isTerrainHeight)
                                            CreateTerrainHeightTexture(nameHolder);
                                        else
                                            CreateTexture2D(texScale, nameHolder, cfg.newTextureIsColor);
                                    }

                                    if (!pegi.IsConfirmingRequestedFor(newTexConfirmTag))
                                    {

                                        if (cfg.showRecentTextures) {

                                            var texName = GetMaterialTextureProperty;

                                            List<ImageMeta> recentTexs;
                                            if (texName != null &&
                                                PainterCamera.Data.recentTextures.TryGetValue(texName,
                                                    out recentTexs) &&
                                                (recentTexs.Count > 0)
                                                && (id == null || (recentTexs.Count > 1) ||
                                                    (id != recentTexs[0].texture2D.GetImgDataIfExists()))
                                                && "Recent Textures:".select(100, ref id, recentTexs).nl(ref changed))
                                                ChangeTexture(id.ExclusiveTexture());

                                        }

                                        if (id == null && cfg.allowExclusiveRenderTextures &&
                                            "Create Render Texture".Click(ref changed))
                                            CreateRenderTexture(texScale, nameHolder);

                                        if (id != null && cfg.allowExclusiveRenderTextures)
                                        {
                                            if (!id.renderTexture && "Add Render Tex".Click(ref changed))
                                                id.AddRenderTexture();

                                            if (id.renderTexture)
                                            {

                                                if ("Replace RendTex".Click(
                                                    "Replace " + param + " with Rend Tex size: " + texScale,
                                                    ref changed))
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

                                }
                                else
                                    icon.Warning.nl("No Texture property selected");

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

                                    id = ImgMeta;

#if UNITY_EDITOR
                                    string orig = null;
                                    if (id.texture2D)
                                    {
                                        orig = id.texture2D.GetPathWithout_Assets_Word();
                                        if (orig != null && icon.Load.ClickUnFocus("Will reload " + orig))
                                        {
                                            ForceReimportMyTexture(orig);
                                            id.saveName = id.texture2D.name;
                                            if (terrain)
                                                UpdateShaderGlobals();
                                        }
                                    }

                                    pegi.edit(ref id.saveName);

                                    if (id.texture2D)
                                    {

                                        if (!id.saveName.SameAs(id.texture2D.name) &&
                                            icon.Refresh.Click("Use current texture name ({0})".F(id.texture2D.name)))
                                            id.saveName = id.texture2D.name;

                                        var destPath = GenerateTextureSavePath();
                                        var existsAtDestination = TextureExistsAtDestinationPath();
                                        var originalExists = !orig.IsNullOrEmpty();
                                        var sameTarget = originalExists && orig.Equals(destPath);
                                        var sameTextureName = originalExists && id.texture2D.name.Equals(id.saveName);
                                        
                                        if (!existsAtDestination || sameTextureName)
                                        {
                                            if ((sameTextureName ? icon.Save : icon.SaveAsNew).Click(sameTextureName
                                                ? "Will Update " + orig
                                                : "Will save as " + destPath)) {

                                                if (sameTextureName)
                                                    RewriteOriginalTexture();
                                                else
                                                    SaveTextureAsAsset(false);

                                                OnChangedTexture_OnMaterial();
                                            }
                                        }
                                        else if (existsAtDestination && icon.Save.Click("Will replace {0}".F(destPath)))
                                            SaveTextureAsAsset(false);

                                        if (!sameTarget && !sameTextureName && originalExists && !existsAtDestination &&
                                            icon.Replace.Click("Will replace {0} with {1} ".F(orig, destPath)))
                                            RewriteOriginalTexture_Rename(id.saveName);

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
                    
                    foreach (var p in PainterSystemManagerModuleBase.ComponentInspectionPlugins)
                        p.ComponentInspector().nl(ref changed);
                    
                }

                pegi.newLine();

                if (changed)
                {
                    texMgmt.OnBeforeBlitConfigurationChange();
                    Update_Brush_Parameters_For_Preview_Shader();
                }


            }

            inspected = null;

            pegi.nl();

            return changed;
        }
        
        public bool PreviewShaderToggleInspect()
        {

            var changed = false;
            if (IsTerrainHeightTexture)
            {
                Texture tht = terrainHeightTexture;

                if (!NotUsingPreview && icon.PreviewShader.Click("Applies changes made on Texture to Actual physical Unity Terrain.", 45).changes(ref changed))
                {
                    Preview_To_UnityTerrain();
                    Unity_To_Preview();

                    MatDta.usePreviewShader = false;
                    SetOriginalShaderOnThis();

                }
                PainterCamera.Data.brushConfig.MaskSet(ColorMask.A, true);

                if (tht.GetImgData() != null && NotUsingPreview && icon.OriginalShader.Click("Applies changes made in Unity terrain Editor", 45).changes(ref changed))
                {
                    Unity_To_Preview();
                    SetPreviewShader();
                }
            }
            else
            {

                if (NotUsingPreview && icon.OriginalShader.Click("Switch To Preview Shader", 45).changes(ref changed))
                    SetPreviewShader();

                if (!NotUsingPreview && icon.PreviewShader.Click("Return to Original Shader", 45).changes(ref changed))
                {
                    MatDta.usePreviewShader = false;
                    SetOriginalShaderOnThis();
                }
            }
            return changed;

        }
        #endif

        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!TexMgmt || this != TexMgmt.focusedPainter) return;

            if (meshEditing && !Application.isPlaying)
                    MeshManager.Inst.DRAW_Lines(true);

            var br = GlobalBrush;

            if (NotUsingPreview && !LockTextureEditing && _lastMouseOverObject == this && IsCurrentTool &&
                br.IsA3DBrush(this) && br.showingSize && !Cfg.showConfig)
                Gizmos.DrawWireSphere(stroke.posTo, br.Size(true) * 0.5f
                                                    );
            
            foreach (var p in PainterSystemManagerModuleBase.GizmoPlugins)
                p.PlugIn_PainterGizmos(this);
        }
        #endif

        #endregion

        #region UPDATES  

        public bool textureWasChanged;
        
        #if UNITY_EDITOR
        public void FeedEvents(Event e)
        {
            var id = ImgMeta;

            if (e.type != EventType.KeyDown || meshEditing || id == null) return;

            switch (e.keyCode)
            {
                case KeyCode.Z:
                    if (id.cache.undo.GotData) id.cache.undo.ApplyTo(id);
                    break;
                case KeyCode.X:
                    if (id.cache.redo.GotData) id.cache.redo.ApplyTo(id);
                    break;
            }

        }
        #endif
        
        public void ManagedUpdate() {
            
            if (this == _mouseOverPaintableGraphicElement) {
                if (!Input.GetMouseButton(0) || !DataUpdate(Input.mousePosition, _clickCamera))
                    _mouseOverPaintableGraphicElement = null;

                OnMouseOver();
            }
      
            #region URL Loading
            if (loadingOrder.Count > 0)
            {

                var extracted = new List<int>();

                foreach (var l in loadingOrder)
                {
                    Texture tex;
                    if (!PainterCamera.DownloadManager.TryGetTexture(l.Key, out tex, true)) continue;
                    
                    if (tex)
                    {
                        var texMeta = SetTextureOnMaterial(l.Value, tex);
                        if (texMeta != null)
                        {
                            texMeta.url = PainterCamera.DownloadManager.GetURL(l.Key);
                            texMeta.saveName = "Loaded Texture {0}".F(l.Key);
                        }
                    }
                    
                    extracted.Add(l.Key);
                    
                }

                foreach (var e in extracted)
                    loadingOrder.Remove(e);
            }
            #endregion

            if (Application.isPlaying && IsEditingThisMesh)
                MeshManager.Inst.DRAW_Lines(false);

            if (textureWasChanged)
                OnChangedTexture_OnMaterial();
            
            var id = ImgMeta;
                id?.ManagedUpdate(this);


            
        }

        private void PreviewShader_StrokePosition_Update()
        {
            CheckPreviewShader();

            if (NotUsingPreview)
                return;
            
            var hide = Application.isPlaying ? Input.GetMouseButton(0) : currentlyPaintedObjectPainter == this;
            PainterCamera.SHADER_POSITION_AND_PREVIEW_UPDATE(stroke, hide, GlobalBrush.Size(this));

            if (!Application.isPlaying)
            {
                QcUnity.SetToDirty(this);
                //EditorUtility.SetDirty(target);
            }
            
        }

        private void Update_Brush_Parameters_For_Preview_Shader()
        {
            var id = ImgMeta;

            if (id == null || NotUsingPreview) return;
            
            TexMgmt.SHADER_BRUSH_UPDATE(GlobalBrush, 1, id, this);

            foreach (var p in Modules)
                p.Update_Brush_Parameters_For_Preview_Shader(this);

            
        }

        #endregion

        #region Mesh Editing 

        public void UpdateMeshCollider(Mesh mesh = null) {

            if (skinnedMeshRenderer)
                UpdateColliderForSkinnedMesh();
            else if (mesh)
                meshCollider?.AssignMeshAsCollider(mesh);

        }

        public bool IsEditingThisMesh => IsCurrentTool && meshEditing && (MeshManager.target == this); 

        private static MeshManager MeshManager => MeshManager.Inst; 

        #endregion

        #region UI Elements Painting

        private static PlaytimePainter _mouseOverPaintableGraphicElement;
        private Vector2 _uiUv;
        [NonSerialized]private Camera _clickCamera;

        public void OnPointerDown(PointerEventData eventData) => _mouseOverPaintableGraphicElement = DataUpdate(eventData) ? this : _mouseOverPaintableGraphicElement;

        public void OnPointerUp(PointerEventData eventData) => _mouseOverPaintableGraphicElement = null;

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_mouseOverPaintableGraphicElement == this)
                DataUpdate(eventData);
        }

        private bool DataUpdate(PointerEventData eventData)
        {

            if (DataUpdate(eventData.position, eventData.pressEventCamera))
                _clickCamera = eventData.pressEventCamera;
            else
                return false;

            return true;
        }

        private bool DataUpdate(Vector2 position, Camera cam)
        {
            Vector2 localCursor;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(uiGraphic.rectTransform, position, cam, out localCursor))
                return false;

            _uiUv = (localCursor / uiGraphic.rectTransform.rect.size) + Vector2.one * 0.5f;

            return true;
        }

        private bool CastRayPlaytime_UI()  {
            
            var id = ImgMeta;

            if (id == null)
                return false;

            var uvClick = _uiUv;

            uvClick.Scale(id.tiling);
            uvClick += id.offset;
            stroke.unRepeatedUv = uvClick + id.offset;
            stroke.uvTo = stroke.unRepeatedUv.To01Space();
            PreviewShader_StrokePosition_Update();
            return true;
        }

        #endregion

        #region Encode & Decode

        [SerializeField] private string _pluginStd;

        public string ConfigStd
        {
            get { return _pluginStd;}
            set { _pluginStd = value; }
        }

        public CfgEncoder Encode() => new CfgEncoder()
            .Add("mdls", modulesContainer)
            .Add_IfTrue("invCast", invertRayCast);

        public void Decode(string data) => new CfgDecoder(data).DecodeTagsFor(this);

        public bool Decode(string tg, string data) {
            switch (tg) {
                case "mdls": modulesContainer.Decode(data); break;
                case "invCast": invertRayCast = data.ToBool(); break;
                default: return true;
            }

            return false;
        }
        #endregion

    }
}