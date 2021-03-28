using System;
using System.Collections.Generic;
using System.IO;
using PlayerAndEditorGUI;
using PlaytimePainter.CameraModules;
using PlaytimePainter.ComponentModules;
using PlaytimePainter.MeshEditing;
using QuizCannersUtilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#if UNITY_2019_1_OR_NEWER
using UnityEditor.EditorTools;
#endif
#endif

namespace PlaytimePainter
{

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration


   
    [ExecuteInEditMode]
    public partial class PlaytimePainter : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, ICfg
    {


        #region StaticGetters

        public static bool IsCurrentTool
        {
            get
            {

#if UNITY_EDITOR && UNITY_2019_1_OR_NEWER
                if (!Application.isPlaying)
                    return ToolManager.activeToolType == typeof(PainterAsIntegratedCustomTool);

#endif

                return PainterDataAndConfig.toolEnabled;
            }
            set { PainterDataAndConfig.toolEnabled = value; }
        }

        private static PainterDataAndConfig Cfg => PainterCamera.Data;

        private static PainterCamera TexMgmt => PainterCamera.Inst;

        private static MeshEditorManager MeshMgmt => MeshEditorManager.Inst;

        protected static GridNavigator Grid => GridNavigator.Inst();

        private static Brush GlobalBrush => Cfg.Brush;

        public BrushTypes.Base GlobalBrushType => GlobalBrush.GetBrushType(TexMeta.TargetIsTexture2D());

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

        private bool HasMaterialSource => meshRenderer || terrain || uiGraphic;

        #endregion

        #region Modules

        PainterModules _modulesContainer;

        public PainterModules Modules
        {
            get
            {
                if (_modulesContainer == null)
                    _modulesContainer = new PainterModules(this);

                return _modulesContainer;
            }
        }

        public class PainterModules : TaggedModulesList<ComponentModuleBase>
        {

            public override void OnInitialize()
            {
                foreach (var p in modules)
                    p.painter = painter;
            }

            public PlaytimePainter painter;

            public PainterModules(PlaytimePainter painter)
            {
                this.painter = painter;
            }
        }

        public T GetModule<T>() where T : ComponentModuleBase => Modules.GetModule<T>();

        public void UpdateModules()
        {
            foreach (var nt in Modules)
            {
                if (!nt.painter)
                {
                    Debug.LogError("Parnt Component in {0} is not assigned".F(nt.GetType().ToString().SimplifyTypeName()));
                    nt.painter = this;
                }
                nt.OnComponentDirty();
            }
        }

        #endregion

        #region Painting

        public bool Is3DBrush() => Is3DBrush(GlobalBrush);

        public bool Is3DBrush(Brush brush)
        {
            var overrideOther = false;

            var isA3D = false;

            foreach (var pl in CameraModuleBase.BrushPlugins)
            {
                isA3D = pl.IsA3DBrush(this, brush, ref overrideOther);
                if (overrideOther)
                    return isA3D;
            }

            return brush.Is3DBrush(TexMeta);
        }

        public bool invertRayCast;

        public Stroke stroke = new Stroke();
        private PaintCommand.ForPainterComponent _paintCommand;

        public PaintCommand.ForPainterComponent PaintCommand
        {
            get
            {
                if (_paintCommand == null)
                    _paintCommand = new PaintCommand.ForPainterComponent(stroke, GlobalBrush, this);
                else
                {
                    _paintCommand.TextureData = TexMeta;
                    _paintCommand.Brush = GlobalBrush;
                    _paintCommand.Stroke = stroke;
                }

                if (!_paintCommand.painter)
                    Debug.LogError("Painter inside a command is zero");

                return _paintCommand;
            }
        }

        public static PlaytimePainter currentlyPaintedObjectPainter;
        private static PlaytimePainter _lastMouseOverObject;

        private double _mouseButtonTime;

        private void StrokeStateFromInputs()
        {
            stroke.MouseUpEvent = Input.GetMouseButtonUp(0);
            stroke.MouseDownEvent = Input.GetMouseButtonDown(0);
        }

        private void StrokeFromGrid()
        {
            stroke.posTo = GridNavigator.onGridPos;
            PreviewShader_StrokePosition_Update();
        }

        public void ProcessStrokeState()
        {

            CheckPreviewShader();

            var control = Application.isPlaying
                ? (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                : (Event.current != null && Event.current.control);

            if (control)
            {

                if (!stroke.MouseDownEvent)
                    return;

                if (NeedsGrid)
                    GridNavigator.MoveToPointedPosition();
                else
                    SampleTexture(stroke.uvTo);

                currentlyPaintedObjectPainter = null;

            }
            else
            {
                if (stroke.MouseDownEvent)
                {
                    stroke.firstStroke = true;
                    stroke.SetPreviousValues();

                    if (currentlyPaintedObjectPainter != this)
                    {
                        currentlyPaintedObjectPainter = this;
                        FocusOnThisObject();
                    }
                }

                if (currentlyPaintedObjectPainter == this)
                {

                    var id = TexMeta;

                    if (id == null) return;

                    if (stroke.MouseDownEvent)
                        id.OnStrokeMouseDown_CheckBackup();

                    if (IsTerrainHeightTexture && stroke.MouseUpEvent)
                        TerrainHeightTexture_To_UnityTerrain();

                    if (!stroke.MouseDownEvent || CanPaintOnMouseDown())
                        GlobalBrush.Paint(PaintCommand);
                    else
                        foreach (var module in TexMeta.Modules)
                            module.OnPaintingDrag(this);
                }
            }

            stroke.MouseDownEvent = false;

            if (stroke.MouseUpEvent)
                currentlyPaintedObjectPainter = null;
        }

        public void OnMouseOver()
        {

            if (NeedsGrid)
                return;

            if ((pegi.GameView.MouseOverUI ||
                 (_mouseOverPaintableGraphicElement && this != _mouseOverPaintableGraphicElement)) ||
                (!IsUiGraphicPainter && EventSystem.current && EventSystem.current.IsPointerOverGameObject()))
            {
                stroke.MouseDownEvent = false;
                return;
            }

            StrokeStateFromInputs();

            if (Input.GetMouseButtonDown(1))
                _mouseButtonTime = QcUnity.TimeSinceStartup();

            if (!CanPaint())
                return;

            if (Input.GetMouseButtonUp(1) && ((QcUnity.TimeSinceStartup() - _mouseButtonTime) < 0.2f))
                FocusOnThisObject();

            if (uiGraphic)
            {
                if (!CastRayPlaytime_UI())
                    return;
            }
            else if (!CastRayPlaytime(stroke, Input.mousePosition)) return;

            ProcessStrokeState();
        }

#if UNITY_EDITOR
        public void OnMouseOverSceneView(RaycastHit hit, Event e)
        {

            if (!CanPaint())
                return;

            if (NeedsGrid)
                StrokeFromGrid();
            else if (!ProcessHit(hit, stroke))
                return;

            ProcessStrokeState();

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

            if (MeshEditorManager.target)
                return false;

            if (stroke.MouseDownEvent || stroke.MouseUpEvent)
                InitIfNotInitialized();

            if (TexMeta != null) return true;

            if (stroke.MouseDownEvent)
                pegi.GameView.ShowNotification("No texture to edit");

            return false;

        }

        private readonly QcUnity.ChillLogger _logger = new QcUnity.ChillLogger("");

        private bool CastRayPlaytime(Stroke st, Vector3 mousePos)
        {

            var cam = TexMgmt.MainCamera;

            if (!cam)
            {
                _logger.Log_Interval(2, "No Main Camera to RayCast from", true, this);
                return false;
            }

            var ray = PrepareRay(cam.ScreenPointToRay(mousePos));

            if (invertRayCast && meshRenderer)
            {
                ray.origin = ray.GetPoint(meshRenderer.bounds.max.magnitude * 1.5f);
                ray.direction = -ray.direction;
            }

            RaycastHit hit;
            return Physics.Raycast(ray, out hit, float.MaxValue) && ProcessHit(hit, st);
        }

        public Ray PrepareRay(Ray ray)
        {
            var id = TexMeta;

            var br = GlobalBrush;
            if (br.showBrushDynamics)
                GlobalBrush.brushDynamic.OnPrepareRay(this, br, ref ray);

            if (id == null || !invertRayCast || !meshRenderer || IsUiGraphicPainter) return ray;

            ray.origin = ray.GetPoint(meshRenderer.bounds.max.magnitude * 1.5f);
            ray.direction = -ray.direction;

            return ray;
        }

        private bool ProcessHit(RaycastHit hit, Stroke st)
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

            if (TexMeta == null)
                return false;

            st.posTo = hit.point;
            st.collisionNormal = hit.normal;
            st.unRepeatedUv = OffsetAndTileUv(hit);
            st.uvTo = st.unRepeatedUv.To01Space();

            PreviewShader_StrokePosition_Update();

            return true;
        }

        private Vector2 OffsetAndTileUv(RaycastHit hit)
        {
            var id = TexMeta;

            if (id == null) return hit.textureCoord;

            var uv = id.useTexCoord2 ? hit.textureCoord2 : hit.textureCoord;

            foreach (var p in Modules)
                if (p.OffsetAndTileUv(hit, ref uv))
                    return uv;

            uv.Scale(id.tiling);
            uv += id.offset;

            return uv;
        }

        public void SampleTexture(Vector2 uv)
        {
            TexMgmt.OnBeforeBlitConfigurationChange();
            GlobalBrush.mask.SetValuesOn(ref GlobalBrush.Color, TexMeta.SampleAt(uv));
            Update_Brush_Parameters_For_Preview_Shader();
        }

        private bool CanPaintOnMouseDown() =>
            TexMeta.TargetIsTexture2D() || GlobalBrushType.StartPaintingTheMomentMouseIsDown;

        #endregion

        #region Preview

        public static Material previewHolderMaterial;
        public static Shader previewHolderOriginalShader;

        public bool NotUsingPreview => !previewHolderMaterial || previewHolderMaterial != Material;

        public bool IsUsingPreview => !NotUsingPreview;

        private void CheckPreviewShader()
        {
            if (MatDta == null)
                return;
            if (!IsCurrentTool || (LockTextureEditing && !IsEditingThisMesh))
                SetOriginalShaderOnThis();
            else if (MatDta.usePreviewShader && NotUsingPreview)
                SetPreviewShader();
        }

        public void SetPreviewShader()
        {
            var mat = Material;

            if (previewHolderMaterial)
            {
                if (previewHolderMaterial != mat)
                    CheckSetOriginalShader();
                else
                    return;
            }

            if (meshEditing && (MeshEditorManager.target != this))
                return;

            var id = TexMeta;

            var tex = id.CurrentTexture();

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
                if (terrain)
                    shd = Cfg.previewTerrain;
                else
                {

                    foreach (var pl in CameraModuleBase.BrushPlugins)
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
                Debug.LogError("Preview shader not found");
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

        public TextureMeta TexMeta => GetTextureOnMaterial().GetTextureMeta();

        private int SelectedTexture
        {
            get
            {
                var md = MatDta;
                return md?.selectedTexture ?? 0;
            }
            set
            {
                var md = MatDta;
                if (md != null) md.selectedTexture = value;
            }
        }

        private void UpdateTilingFromMaterial()
        {

            var id = TexMeta;

            var fieldName = GetMaterialTextureProperty();
            var mat = Material;
            if (IsUsingPreview && !terrain)
            {
                id.tiling = mat.GetTiling(PainterShaderVariables.PreviewTexture);
                id.offset = mat.GetOffset(PainterShaderVariables.PreviewTexture);
                return;
            }


            if (fieldName != null)
                foreach (var nt in Modules)
                    if (nt.UpdateTilingFromMaterial(fieldName))
                        return;

            if (!mat || fieldName == null || id == null) return;

            id.tiling = mat.GetTiling(fieldName);
            id.offset = mat.GetOffset(fieldName);
        }

        public void UpdateTilingToMaterial()
        {
            var id = TexMeta;
            var fieldName = GetMaterialTextureProperty();
            var mat = Material;
            if (IsUsingPreview && !terrain)
            {
                mat.SetTiling(PainterShaderVariables.PreviewTexture, id.tiling);
                mat.SetOffset(PainterShaderVariables.PreviewTexture, id.offset);
                return;
            }

            if (!mat || fieldName == null || id == null) return;
            mat.SetTiling(fieldName, id.tiling);
            mat.SetOffset(fieldName, id.offset);
        }

        private void OnChangedTexture_OnMaterial()
        {
            if (NotUsingPreview || !terrain)
                ChangeTexture(GetTextureOnMaterial());
        }

        public void ChangeTexture(TextureMeta id) => ChangeTexture(id.CurrentTexture());

        private void ChangeTexture(Texture texture)
        {

            textureWasChanged = false;

#if UNITY_EDITOR

            var t2D = texture as Texture2D;

            if (t2D)
            {
                var imp = t2D.GetTextureImporter();
                if (imp)
                {

                    var assetPath = AssetDatabase.GetAssetPath(texture);
                    var extension = assetPath.Substring(assetPath.LastIndexOf(".", StringComparison.Ordinal) + 1);

                    if (extension != "png")
                    {
                        pegi.GameView.ShowNotification("Converting {0} to .png".F(assetPath));

                        texture = t2D.CreatePngSameDirectory(t2D.name);
                    }

                }
            }
#endif
            
            if (!texture)
            {
                RemoveTextureFromMaterial(); //SetTextureOnMaterial((Texture)null);
                return;
            }

            var id = texture.GetImgDataIfExists();

            if (id == null)
            {
                id = new TextureMeta().Init(texture);

                var field = GetMaterialTextureProperty();

                if (field == null)
                    Debug.LogError("Field is null");
                else
                    id.useTexCoord2 = field.NameForDisplayPEGI().Contains(PainterShaderVariables.isUV2DisaplyNameTag);
            }

            SetTextureOnMaterial(texture);

            UpdateOrSetTexTarget(id.target);

            UpdateTilingFromMaterial();

        }

        public PlaytimePainter SetTexTarget(Brush br)
        {
            if (TexMeta.TargetIsTexture2D() != br.targetIsTex2D)
                UpdateOrSetTexTarget(br.targetIsTex2D ? TexTarget.Texture2D : TexTarget.RenderTexture);

            return this;
        }

        public void UpdateOrSetTexTarget(TexTarget dst)
        {

            InitIfNotInitialized();

            var id = TexMeta;

            if (id == null)
                return;

            if (id.target == dst)
                return;

            id.ChangeDestination(dst, GetMaterial(true).GetMaterialPainterMeta(), GetMaterialTextureProperty(), this);
            CheckPreviewShader();

        }

        private void ReEnableRenderTexture()
        {
            if (LockTextureEditing) return;

            OnEnable();

            OnChangedTexture_OnMaterial();

            if (TexMeta != null)
                UpdateOrSetTexTarget(TexTarget.RenderTexture); // set it to Render Texture

        }

        private void CreateTerrainHeightTexture(string newName)
        {

            var field = GetMaterialTextureProperty();

            if (!field.Equals(PainterShaderVariables.TerrainHeight))
            {
                Debug.Log("Terrain height is not currently selected.");
                return;
            }

            var size = terrain.terrainData.heightmapResolution - 1;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);

            var id = TexMeta;

            if (id != null)
                id.From(texture);
            else
                ChangeTexture(texture);

            id = TexMeta;

            id.saveName = newName;
            texture.name = id.saveName;
            texture.Apply(true, false);

            SetTextureOnMaterial(texture);

            UnityTerrain_To_HeightTexture();
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
            UpdateModules();
        }

        private void CreateTexture2D(int size, string textureName, bool isColor)
        {

            var id = TexMeta;

            var gotRenderTextureData = id != null && size == id.width && size == id.width && id.TargetIsRenderTexture();

            var texture = new Texture2D(size, size, TextureFormat.ARGB32, true, !isColor);

            if (gotRenderTextureData && (!id.texture2D || textureName.SameAs(id.saveName)))
                id.texture2D = texture;

            texture.wrapMode = TextureWrapMode.Repeat;

            ChangeTexture(texture);

            id = TexMeta;

            id.saveName = textureName;
            texture.name = textureName;

            var needsFullUpdate = false;

#if UNITY_EDITOR

            var needsReColorizing = false;
#endif

            var colorData = isColor ? Cfg.newTextureClearNonColorValue : Cfg.newTextureClearColor;

            if (gotRenderTextureData)
                id.RenderTexture_To_Texture2D();
            else
            {
                // When first creating texture Alpha value should not be 1 otherwise texture will be encoded to RGB and not RGBA 

                #if UNITY_EDITOR

                if (Math.Abs(colorData.a - 1) < float.Epsilon)
                {
                    needsReColorizing = true;
                    colorData.a = 0.5f;
                }

                #endif

                id.SetPixels(colorData);
                
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

            if (needsReColorizing)
            {
                id.SetPixels(colorData);
                id.SetAndApply();
            }
#endif

            TexMeta.ApplyToTexture2D();

        }

        private void CreateRenderTexture(int size, string renderTextureName)
        {
            var previous = TexMeta;

            var nt = new TextureMeta().Init(size);

            nt.saveName = renderTextureName;

            ChangeTexture(nt.renderTexture);

            PainterCamera.Inst.Render(previous.CurrentTexture(), nt);

            UpdateOrSetTexTarget(TexTarget.RenderTexture);

        }

        #endregion

        #region Material MGMT

        public int selectedAtlasedMaterial = -1;

        public int selectedSubMesh;
        [SerializeField] private Material _fallbackMaterial;

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
                }
                else if (uiGraphic)
                    uiGraphic.material = value;
                else
                    _fallbackMaterial = value;
            }
        }

        public MaterialMeta MatDta => Material.GetMaterialPainterMeta();

        private Material GetMaterial(bool original = false)
        {

            Material result = null;

            if (original)
                CheckSetOriginalShader();

            if (meshRenderer)
            {
                if (meshRenderer.sharedMaterials.ClampIndexToCount(ref selectedSubMesh))
                    result = meshRenderer.sharedMaterials[selectedSubMesh];
            }
            else if (uiGraphic)
                result = uiGraphic.material;
            else
                result = terrain ? terrain.materialTemplate : null;


            return result;
        }
        
        public Material[] Materials
        {
            get
            {

                if (!terrain && !uiGraphic)
                    return meshRenderer ? meshRenderer.sharedMaterials : new[] {_fallbackMaterial};

                var mat = Material;

                return mat ? new[] {mat} : new[] {_fallbackMaterial};
            }
            set
            {
                if (meshRenderer)
                    meshRenderer.sharedMaterials = value;
                else if (uiGraphic)
                    uiGraphic.material = value.TryGet(0);
                else if (terrain)
                    terrain.materialTemplate = value.TryGet(0);
                else
                    _fallbackMaterial = value.TryGet(0);
            }
        }

        private MaterialMeta _lastFetchedTextureNamesFor;
        private List<ShaderProperty.TextureValue> _lastTextureNames = new List<ShaderProperty.TextureValue>();

        private List<ShaderProperty.TextureValue> GetAllTextureNames()
        {

            var materialData = MatDta;

            if (!Application.isEditor)
                return materialData?.materialsTextureFields;

            //  #if UNITY_EDITOR

            bool sameAsBefore = _lastFetchedTextureNamesFor == materialData && _lastTextureNames.Count > 0;

            if (NotUsingPreview && (Application.isEditor || !sameAsBefore))
            {

                _lastTextureNames.Clear();

                if (!terrain)
                    _lastTextureNames.AddRange(Material.MyGetTextureProperties_Editor());
                else
                {
                    var tmp = Material.MyGetTextureProperties_Editor();

                    foreach (var t in tmp)
                        if ((!t.NameForDisplayPEGI().Contains("_Splat")) &&
                            (!t.NameForDisplayPEGI().Contains("_Normal")))
                            _lastTextureNames.Add(t);

                }

                foreach (var nt in Modules)
                    if (nt != null)
                        nt.GetNonMaterialTextureNames(ref _lastTextureNames);

                _lastFetchedTextureNamesFor = materialData;

                if (materialData != null)
                    materialData.materialsTextureFields = _lastTextureNames;


            }
            else if (!sameAsBefore)
                _lastTextureNames.Clear();
            // #endif

            return _lastTextureNames;
        }

        public ShaderProperty.TextureValue GetMaterialTextureProperty() => GetAllTextureNames().TryGet(SelectedTexture);

        private Texture GetTextureOnMaterial()
        {

            if (IsUsingPreview)
            {
                if (meshEditing) return null;
                if (!terrain)
                {
                    var m = Material;
                    return m ? Material.Get(PainterShaderVariables.PreviewTexture) : null;
                }
            }

            var fieldName = GetMaterialTextureProperty();

            if (fieldName == null)
                return null;

            Texture tex = null;

            foreach (var t in Modules)
            {
                if (t.GetTexture(fieldName, ref tex))
                    return tex;
            }

            return Material.Get(fieldName);
        }

        private void RemoveTextureFromMaterial() => SetTextureOnMaterial(GetMaterialTextureProperty(), null);

        public void SetTextureOnMaterial(TextureMeta id) =>
            SetTextureOnMaterial(GetMaterialTextureProperty(), id.CurrentTexture());

        public TextureMeta SetTextureOnMaterial(Texture tex) => SetTextureOnMaterial(GetMaterialTextureProperty(), tex);

        private TextureMeta SetTextureOnMaterial(ShaderProperty.TextureValue property, Texture tex)
        {
            var id = SetTextureOnMaterial(property, tex, GetMaterial(true));
            CheckPreviewShader();
            return id;
        }

        public TextureMeta SetTextureOnMaterial(ShaderProperty.TextureValue property, Texture tex, Material mat)
        {

            var id = tex.GetTextureMeta();

            if (property != null)
            {
                if (id != null)
                    Cfg.recentTextures.AddIfNew(property, id);

                foreach (var nt in Modules)
                    if (nt.SetTextureOnMaterial(property, id))
                        return id;
            }

            if (!mat) return id;
            if (property != null)
                mat.Set(property, id.CurrentTexture());

            if (IsUsingPreview && (!terrain))
                SetTextureOnPreview(id.CurrentTexture());

            return id;
        }

        private void SetTextureOnPreview(Texture tex)
        {

            if (meshEditing) return;

            var mat = Material;
            var id = tex.GetTextureMeta();

            PainterShaderVariables.PreviewTexture.SetOn(mat, id.CurrentTexture());

            if (id == null) return;

            mat.SetOffset(PainterShaderVariables.PreviewTexture, id.offset);
            mat.SetTiling(PainterShaderVariables.PreviewTexture, id.tiling);

        }

        public Material InstantiateMaterial(bool saveIt)
        {

            CheckSetOriginalShader();

            if (TexMeta != null && Material)
                UpdateOrSetTexTarget(TexTarget.Texture2D);

           InitIfNotInitialized();

            var mat = GetMaterial(true);

            if (!mat && terrain)
            {
                mat = new Material(Cfg.previewTerrain);

                terrain.materialTemplate = mat;
                //terrain.materialType = Terrain.MaterialType.Custom;
                mat.name += "_Terrain material";
            }
            else
            {
                Material = new Material(mat ? mat : Cfg.defaultMaterial);
                CheckPreviewShader();
            }

            var material = Material;

            if (material)
            {
                material.name = gameObject.name;

                if (saveIt)
                {
#if UNITY_EDITOR
                    QcFile.Save.Asset(material, Cfg.materialsFolderName, ".mat", true);
                    CheckPreviewShader();
#endif
                }
            }

            OnChangedTexture_OnMaterial();

            var id = TexMeta;

            if (id != null && Material)
                UpdateOrSetTexTarget(id.target);

            pegi.GameView.ShowNotification("Instantiating Material on {0}".F(gameObject.name));

            return Material;


        }

        #endregion

        #region Mesh MGMT 

        [NonSerialized] public Mesh colliderForSkinnedMesh;

        public Mesh SharedMesh
        {

            get
            {
                return meshFilter
                    ? meshFilter.sharedMesh
                    : (skinnedMeshRenderer ? skinnedMeshRenderer.sharedMesh : null);
            }
            set
            {
                if (meshFilter)
                    meshFilter.sharedMesh = value;
                else if (skinnedMeshRenderer)
                    skinnedMeshRenderer.sharedMesh = value;
            }
        }

        public Mesh Mesh
        {
            set
            {
                if (meshFilter) meshFilter.mesh = value;
                if (skinnedMeshRenderer) skinnedMeshRenderer.sharedMesh = value;
            }
        }

        public bool meshEditing;

        public string selectedMeshProfile;
        public MeshPackagingProfile MeshProfile => Cfg.GetMeshPackagingProfile(selectedMeshProfile);

        [SerializeField] private CfgData savedMeshData;
        [SerializeField] private Mesh meshDataSavedFor;

        public CfgData SavedEditableMesh
        {
            get
            {
                if ((savedMeshData.ToString() != null) && (savedMeshData.ToString().Length == 0 || (meshDataSavedFor != this.GetMesh())))
                    savedMeshData = new CfgData();

                return savedMeshData;
            }
            set
            {
                meshDataSavedFor = this.GetMesh();
                if (!meshDataSavedFor)
                {
                    var m = new Mesh();
                    meshDataSavedFor = m;
                    Mesh = m;
                }

                savedMeshData = value;
            }
        }

        public void UpdateMeshCollider(Mesh mesh = null)
        {
            if (skinnedMeshRenderer)
                UpdateColliderForSkinnedMesh();
            else if (mesh)
                meshCollider?.AssignMeshAsCollider(mesh);

        }

        public bool IsEditingThisMesh => IsCurrentTool && meshEditing && (MeshEditorManager.target == this);

        private static MeshEditorManager MeshManager => MeshEditorManager.Inst;

        #endregion

        #region Terrain_MGMT

        public float tilingY = 8;
        
        public void UpdateTerrainPosition() =>
            PainterShaderVariables.TerrainPosition.GlobalValue = transform.position.ToVector4(tilingY);

        private void TerrainHeightTexture_To_UnityTerrain()
        {

            var id = TexMeta;

            if (id == null)
                return;

            var rendTex = id.TargetIsRenderTexture();

            if (rendTex)
                UpdateOrSetTexTarget(TexTarget.Texture2D);

            var td = terrain.terrainData;

            var res = td.heightmapResolution - 1;

            var conversion = (id.width / (float) res);

            var heights = td.GetHeights(0, 0, res + 1, res + 1);

            var cols = id.Pixels;

            if (Math.Abs(conversion - 1) > float.Epsilon)
                for (var y = 0; y < res; y++)
                {
                    var yInd = id.width * Mathf.FloorToInt((y * conversion));
                    for (var x = 0; x < res; x++)
                        heights[y, x] = cols[yInd + (int) (x * conversion)].a;

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

            UpdateModules();

            if (rendTex)
                UpdateOrSetTexTarget(TexTarget.RenderTexture);
        }

        public void UnityTerrain_To_HeightTexture()
        {

            var oid = TexMeta;

            var id = terrainHeightTexture.GetTextureMeta();

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

                    var dx = ((float) x) / textureSize;
                    var dy = ((float) y) / textureSize;

                    var v3 = td.GetInterpolatedNormal(dx, dy); // + Vector3.one * 0.5f;

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

            UpdateModules();
        }

        public bool IsTerrainHeightTexture
        {
            get
            {
                if (!terrain)
                    return false;

                var propName = GetMaterialTextureProperty();
                return propName?.Equals(PainterShaderVariables.TerrainHeight) ?? false;
            }
        }

        public bool IsTerrainControlTexture => TexMeta != null && terrain &&
                                               GetMaterialTextureProperty().HasUsageTag(PainterShaderVariables
                                                   .TERRAIN_CONTROL_TEXTURE);

        #endregion

        #region UI Canvas MGMT

        private static PlaytimePainter _mouseOverPaintableGraphicElement;
        private Vector2 _uiUv;
        [NonSerialized] private Camera _clickCamera;

        public bool IsUiGraphicPainter => !meshRenderer && !terrain && uiGraphic;

        public void OnPointerDown(PointerEventData eventData) => _mouseOverPaintableGraphicElement =
            DataUpdate(eventData) ? this : _mouseOverPaintableGraphicElement;

        public void OnPointerUp(PointerEventData eventData)
        {
            _mouseOverPaintableGraphicElement =
                DataUpdate(eventData) ? this : _mouseOverPaintableGraphicElement;
        }

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
            var rt = uiGraphic.rectTransform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, position, cam, out localCursor))
                return false;

            _uiUv = (localCursor / rt.rect.size) + rt.pivot; //Vector2.one * 0.5f;

            return true;
        }

        private bool CastRayPlaytime_UI()
        {

            var id = TexMeta;

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

        #region COMPONENT MGMT 

        public bool LockTextureEditing
        {
            get
            {
                if (meshEditing || !TexMgmt)
                    return true;
                var i = TexMeta;
                return i == null || i.lockEditing || i.other;
            }
            set
            {
                var i = TexMeta;
                if (i != null) i.lockEditing = value;
            }
        }

        public bool forcedMeshCollider;
        [NonSerialized] public bool initialized;
        public bool autoSelectMaterialByNumberOfPointedSubMesh = true;

        public const string OnlineManual =
            "https://docs.google.com/document/d/170k_CE-rowVW9nsAo3EUqVIAWlZNahC0ua11t1ibMBo/edit?usp=sharing";

        private static readonly List<string> TextureEditorIgnore = new List<string>
            {MeshEditorManager.VertexEditorUiElementTag, MeshEditorManager.ToolComponentTag, "o"};

        public static bool CanEditWithTag(string tag)
        {
            foreach (var x in TextureEditorIgnore)
                if (tag.Contains(x))
                    return false;
            return true;
        }
        
        private void OnDestroy()
        {

            var colliders = GetComponents<Collider>();

            foreach (var c in colliders)
                if (c.GetType() != typeof(MeshCollider))
                    c.enabled = true;

            colliders = GetComponentsInChildren<Collider>();

            foreach (var c in colliders)
                if (c.GetType() != typeof(MeshCollider))
                    c.enabled = true;

            if (forcedMeshCollider && meshCollider)
            {
                meshCollider.enabled = false; //DestroyWhateverComponent();
                forcedMeshCollider = false;
            }
        }

        public void TrySetOriginalTexture()
        {

            var id = GetTextureOnMaterial().GetImgDataIfExists();

            if (id != null && id.CurrentTexture().IsBigRenderTexturePair())
                UpdateOrSetTexTarget(TexTarget.Texture2D);

        }

        public bool isBeingDisabled;

        private void OnDisable()
        {

            isBeingDisabled = true;

            CheckSetOriginalShader();

            initialized = false; // Should be before restoring to texture2D to avoid Clear to black.

            if (Application.isPlaying)
                TrySetOriginalTexture();

            _cfgData = Encode().CfgData;

            if (!TexMgmt || MeshEditorManager.target != this) return;

            MeshEditorManager.Inst.StopEditingMesh();

        }

        private void OnEnable()
        {

            isBeingDisabled = false;

            PainterClass.applicationIsQuitting = false;

            if (terrain)
                UpdateModules();

            if (!meshRenderer)
                meshRenderer = GetComponent<MeshRenderer>();

            this.DecodeFull(_cfgData);
            //this.LoadCfgData();

        }

        public void Reset()
        {
            InitIfNotInitialized();
        }

        private void UpdateColliderForSkinnedMesh()
        {

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
            catch (Exception ex)
            {
                _logger.Log_Interval(1000, ex.ToString(), true, this);
            }
        }

        public void InitIfNotInitialized()
        {

            if (!(!initialized || ((!meshCollider || !meshRenderer) && (!terrain || !terrainCollider)))) return;

            initialized = true;

            _nameHolder = "New "+ gameObject.name;

            if (!meshRenderer)
                meshRenderer = GetComponent<Renderer>();

            if (!uiGraphic)
                uiGraphic = GetComponent<Graphic>();

            if (meshRenderer)
            {

                var colliders = GetComponents<Collider>();

                foreach (var c in colliders)
                    if (c.GetType() != typeof(MeshCollider))
                        c.enabled = false;

                colliders = GetComponentsInChildren<Collider>();

                foreach (var c in colliders)
                    if (c.GetType() != typeof(MeshCollider))
                        c.enabled = false;

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

            if (meshRenderer && (meshRenderer.GetType() == typeof(SkinnedMeshRenderer)))
            {
                skinnedMeshRenderer = (SkinnedMeshRenderer) meshRenderer;
                UpdateColliderForSkinnedMesh();
            }
            else skinnedMeshRenderer = null;

            if (!meshRenderer)
            {

                terrain = GetComponent<Terrain>();
                if (terrain)
                    terrainCollider = GetComponent<TerrainCollider>();

            }

            if ((this == TexMgmt.autodisabledBufferTarget) && (!LockTextureEditing) &&
                (!QcUnity.ApplicationIsAboutToEnterPlayMode()))
                ReEnableRenderTexture();

        }

        private void FocusOnThisObject()
        {

#if UNITY_EDITOR
            QcUnity.FocusOn(gameObject);
#endif
            selectedInPlaytime = this;
            Update_Brush_Parameters_For_Preview_Shader();
            InitIfNotInitialized();
        }

        #endregion

        #region UPDATES  

        public bool textureWasChanged;

#if UNITY_EDITOR
        public void FeedEvents(Event e)
        {
            var id = TexMeta;

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

        private double _debugTimeOfLastUpdate;

        public void ManagedUpdateOnFocused()
        {

            if (this == _mouseOverPaintableGraphicElement)
            {

                var couldUpdate = DataUpdate(Input.mousePosition, _clickCamera);
                    
                if (couldUpdate)
                    OnMouseOver();

                if (!Input.GetMouseButton(0) || !couldUpdate)
                    _mouseOverPaintableGraphicElement = null;
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
                MeshEditorManager.Inst.DRAW_Lines(false);

            if (GlobalBrush!=null && GlobalBrush.previewDirty)
                TexMgmt.SHADER_BRUSH_UPDATE(PaintCommand);

            if (textureWasChanged)
                OnChangedTexture_OnMaterial();

            _debugTimeOfLastUpdate = QcUnity.TimeSinceStartup();

            var id = TexMeta;
            id?.ManagedUpdate(this);

            if (NeedsGrid && Application.isPlaying)
            {
                StrokeStateFromInputs();
                StrokeFromGrid();
                ProcessStrokeState();
            }

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
                this.SetToDirty();
                //EditorUtility.SetDirty(target);
            }

        }

        private void Update_Brush_Parameters_For_Preview_Shader()
        {
            var id = TexMeta;

            if (id == null || NotUsingPreview) return;

            PaintCommand.TextureData = id;

            TexMgmt.SHADER_BRUSH_UPDATE(PaintCommand.Reset());

            foreach (var p in Modules)
                p.Update_Brush_Parameters_For_Preview_Shader();

            PainterCamera._previewAlpha = 1;

        }

        #endregion
        
    }
}