using System;
using System.Collections.Generic;
using System.IO;
using PlayerAndEditorGUI;
using PlaytimePainter.CameraModules;
using PlaytimePainter.ComponentModules;
using PlaytimePainter.MeshEditing;
using QuizCannersUtilities;
using UnityEditorInternal;
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

    [AddComponentMenu("Mesh/Playtime Painter")]
    [HelpURL(OnlineManual)]
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class PlaytimePainter : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler,
        IKeepMyCfg, IPEGI
    {

        #region StaticGetters

        public static bool IsCurrentTool
        {
            get
            {

#if UNITY_EDITOR && UNITY_2019_1_OR_NEWER
                if (!Application.isPlaying)
                    return EditorTools.activeToolType == typeof(PainterAsIntegratedCustomTool);

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
        private PaintCommand.Painter _paintCommand;

        public PaintCommand.Painter PaintCommand
        {
            get
            {
                if (_paintCommand == null)
                    _paintCommand = new PaintCommand.Painter(stroke, GlobalBrush, this);
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
                        Preview_To_UnityTerrain();

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

        private readonly QcUtils.ChillLogger _logger = new QcUtils.ChillLogger("");

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

        private void CheckPreviewShader()
        {
            if (MatDta == null)
                return;
            if (!IsCurrentTool || (LockTextureEditing && !IsEditingThisMesh))
                SetOriginalShaderOnThis();
            else if (MatDta.usePreviewShader && NotUsingPreview)
                SetPreviewShader();
        }

        private void SetPreviewShader()
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

            var fieldName = GetMaterialTextureProperty;
            var mat = Material;
            if (!NotUsingPreview && !terrain)
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
            var fieldName = GetMaterialTextureProperty;
            var mat = Material;
            if (!NotUsingPreview && !terrain)
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

            var field = GetMaterialTextureProperty;

            if (!texture)
            {
                RemoveTextureFromMaterial(); //SetTextureOnMaterial((Texture)null);
                return;
            }

            var id = texture.GetImgDataIfExists();

            if (id == null)
            {
                id = new TextureMeta().Init(texture);
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

            id.ChangeDestination(dst, GetMaterial(true).GetMaterialPainterMeta(), GetMaterialTextureProperty, this);
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

            var field = GetMaterialTextureProperty;

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

            var needsReColorizing = false;

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

        public ShaderProperty.TextureValue GetMaterialTextureProperty => GetAllTextureNames().TryGet(SelectedTexture);

        private Texture GetTextureOnMaterial()
        {

            if (!NotUsingPreview)
            {
                if (meshEditing) return null;
                if (!terrain)
                {
                    var m = Material;
                    return m ? Material.Get(PainterShaderVariables.PreviewTexture) : null;
                }
            }

            var fieldName = GetMaterialTextureProperty;

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

        private void RemoveTextureFromMaterial() => SetTextureOnMaterial(GetMaterialTextureProperty, null);

        public void SetTextureOnMaterial(TextureMeta id) =>
            SetTextureOnMaterial(GetMaterialTextureProperty, id.CurrentTexture());

        public TextureMeta SetTextureOnMaterial(Texture tex) => SetTextureOnMaterial(GetMaterialTextureProperty, tex);

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

            if (!NotUsingPreview && (!terrain))
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

            if (!TexMgmt.defaultMaterial) InitIfNotInitialized();

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

        [SerializeField] private string savedMeshData;
        [SerializeField] private Mesh meshDataSavedFor;

        public string SavedEditableMesh
        {
            get
            {
                if ((savedMeshData != null) && (savedMeshData.Length == 0 || (meshDataSavedFor != this.GetMesh())))
                    savedMeshData = null;

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

        public void UpdateModules()
        {

            foreach (var nt in Modules)
            {
                if (!nt.painter)
                {
                    Debug.LogError(
                        "Parnt Component in {0} is not assigned".F(nt.GetType().ToString().SimplifyTypeName()));
                    nt.painter = this;
                }

                nt.OnComponentDirty();
            }
        }

        public void UpdateTerrainPosition() =>
            PainterShaderVariables.TerrainPosition.GlobalValue = transform.position.ToVector4(tilingY);

        private void Preview_To_UnityTerrain()
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

        private void Unity_To_Preview()
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

                var propName = GetMaterialTextureProperty;
                return propName?.Equals(PainterShaderVariables.TerrainHeight) ?? false;
            }
        }

        public TerrainHeightModule GetTerrainHeight() => GetModule<TerrainHeightModule>();

        public bool IsTerrainControlTexture => TexMeta != null && terrain &&
                                               GetMaterialTextureProperty.HasUsageTag(PainterShaderVariables
                                                   .TERRAIN_CONTROL_TEXTURE);

        #endregion

        #region UI Canvas MGMT

        private static PlaytimePainter _mouseOverPaintableGraphicElement;
        private Vector2 _uiUv;
        [NonSerialized] private Camera _clickCamera;

        public bool IsUiGraphicPainter => !meshRenderer && !terrain && uiGraphic;

        public void OnPointerDown(PointerEventData eventData) => _mouseOverPaintableGraphicElement =
            DataUpdate(eventData) ? this : _mouseOverPaintableGraphicElement;

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

        #region Saving & Loading

        [SerializeField] private string _pluginStd;

        public string ConfigStd
        {
            get { return _pluginStd; }
            set { _pluginStd = value; }
        }

        public CfgEncoder Encode() => new CfgEncoder()
            .Add("mdls", Modules)
            .Add_IfTrue("invCast", invertRayCast);

        public CfgEncoder EncodeMeshStuff()
        {
            if (IsEditingThisMesh)
                MeshEditorManager.Inst.StopEditingMesh();

            return new CfgEncoder()
                .Add_String("m", SavedEditableMesh)
                .Add_String("prn", selectedMeshProfile);
        }

        public bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "mdls":
                    Modules.Decode(data);
                    break;
                case "invCast":
                    invertRayCast = data.ToBool();
                    break;
                case "m":
                    SavedEditableMesh = data;
                    break;
                case "prn":
                    selectedMeshProfile = data;
                    break;
                default: return true;
            }

            return false;
        }

        public void Decode(string data) => this.DecodeTagsFrom(data);

#if UNITY_EDITOR

        private void ForceReimportMyTexture(string path)
        {

            var importer = AssetImporter.GetAtPath("Assets{0}".F(path)) as TextureImporter;
            if (importer == null)
            {
                Debug.Log("No importer for {0}".F(path));
                return;
            }

            var id = TexMeta;

            TexMgmt.TryDiscardBufferChangesTo(id);

            importer.SaveAndReimport();
            if (id.TargetIsRenderTexture())
                id.TextureToRenderTexture(id.texture2D);
            else if (id.texture2D)
                id.PixelsFromTexture2D(id.texture2D);

            SetTextureOnMaterial(id);
        }

        private bool TextureExistsAtDestinationPath() =>
            AssetImporter.GetAtPath(Path.Combine("Assets", GenerateTextureSavePath())) as TextureImporter != null;

        private string GenerateTextureSavePath() =>
            Path.Combine(Cfg.texturesFolderName, TexMeta.saveName + ".png");

        LoopLock _loopLock = new LoopLock();

        private bool OnBeforeSaveTexture(TextureMeta id)
        {

            if (id.TargetIsRenderTexture())
                id.RenderTexture_To_Texture2D();

            var tex = id.texture2D;

            if (id.preserveTransparency && !tex.TextureHasAlpha())
            {


                if (_loopLock.Unlocked)
                    using (_loopLock.Lock())
                    {
                        //ChangeTexture(id.NewTexture2D());

                        //id.texture2D.name = id.texture2D.name + "_A";

                        Debug.Log("Old Texture had no Alpha channel, creating new");

                        string tname = id.texture2D.name + "_A";

                        id.texture2D = id.texture2D.CreatePngSameDirectory(tname);

                        id.saveName = tname;

                        id.texture2D.CopyImportSettingFrom(tex).Reimport_IfNotReadale();

                        SetTextureOnMaterial(id);
                    }


                return false;
            }

            id.SetAlphaSavePixel();

            return true;
        }

        private void OnPostSaveTexture(TextureMeta id)
        {
            SetTextureOnMaterial(id);
            UpdateOrSetTexTarget(id.target);
            UpdateModules();

            id.UnsetAlphaSavePixel();
        }

        private void RewriteOriginalTexture_Rename(string texName)
        {

            var id = TexMeta;

            if (!OnBeforeSaveTexture(id)) return;

            id.texture2D = id.texture2D.RewriteOriginalTexture_NewName(texName);

            OnPostSaveTexture(id);

        }

        private void RewriteOriginalTexture()
        {
            var id = TexMeta;

            if (!OnBeforeSaveTexture(id)) return;

            id.texture2D = id.texture2D.RewriteOriginalTexture();
            OnPostSaveTexture(id);

        }

        private void SaveTextureAsAsset(bool asNew)
        {

            var id = TexMeta;

            if (OnBeforeSaveTexture(id))
            {
                id.texture2D = id.texture2D.SaveTextureAsAsset(Cfg.texturesFolderName, ref id.saveName, asNew);

                id.texture2D.Reimport_IfNotReadale();
            }

            OnPostSaveTexture(id);
        }

        public void SaveMesh()
        {
            var m = this.GetMesh();
            var path = AssetDatabase.GetAssetPath(m);

            var folderPath = Path.Combine(Application.dataPath, Cfg.meshesFolderName);
            Directory.CreateDirectory(folderPath);

            try
            {
                if (path.Length > 0)
                    SharedMesh = Instantiate(SharedMesh);

                var sm = SharedMesh;

                Directory.CreateDirectory(Path.Combine("Assets", Cfg.meshesFolderName));

                AssetDatabase.CreateAsset(sm, Path.Combine("Assets", MeshEditorManager.GenerateMeshSavePath()));

                AssetDatabase.SaveAssets();

                UpdateMeshCollider();

                //if (meshCollider && !meshCollider.sharedMesh && sm)
                //  meshCollider.sharedMesh = sm;

            }
            catch (Exception ex)
            {
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

#if UNITY_EDITOR

        [MenuItem("Tools/" + PainterDataAndConfig.ToolName + "/Add Painters To Selected")]
        private static void AddPainterToSelected()
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

        [MenuItem("Tools/" + PainterDataAndConfig.ToolName + "/Join Discord")]
        public static void Open_Discord() => Application.OpenURL(pegi.PopUpService.DiscordServer);

        [MenuItem("Tools/" + PainterDataAndConfig.ToolName + "/Send an Email")]
        public static void Open_Email() => QcUnity.SendEmail(pegi.PopUpService.SupportEmail,
            "About your Playtime Painter",
            "Hello Yuri, we need to talk. I purchased your asset and expect an excellent quality, but ...");

        [MenuItem("Tools/" + PainterDataAndConfig.ToolName + "/Open Manual")]
        public static void OpenWWW_Documentation() => Application.OpenURL(OnlineManual);

#endif

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

            this.SaveCfgData();

            if (!TexMgmt || MeshEditorManager.target != this) return;

            MeshEditorManager.Inst.StopEditingMesh();

        }

        private void OnEnable()
        {

            isBeingDisabled = false;

            PainterSystem.applicationIsQuitting = false;

            if (terrain)
                UpdateModules();

            if (!meshRenderer)
                meshRenderer = GetComponent<MeshRenderer>();

            this.LoadCfgData();

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

            _nameHolder = gameObject.name;

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
                MeshEditorManager.Inst.DRAW_Lines(false);

            if (GlobalBrush.previewDirty)
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

            TexMgmt.SHADER_BRUSH_UPDATE(PaintCommand.Reset());

            foreach (var p in Modules)
                p.Update_Brush_Parameters_For_Preview_Shader();

            PainterCamera._previewAlpha = 1;

        }

        #endregion

        #region Inspector 

        private string _nameHolder = "unnamed";
        private static string _tmpUrl = "";
        public static PlaytimePainter selectedInPlaytime;

        public static PlaytimePainter inspected;

        [NonSerialized]
        public readonly Dictionary<int, ShaderProperty.TextureValue> loadingOrder =
            new Dictionary<int, ShaderProperty.TextureValue>();

        private static int _inspectedFancyItems = -1;

        public static int _inspectedMeshEditorItems = -1;

        private static int inspectedShowOptionsSubitem = -1;

        public bool Inspect()
        {

#if UNITY_2019_1_OR_NEWER && UNITY_EDITOR
            if (!Application.isPlaying && !IsCurrentTool)
            {
                if (ActiveEditorTracker.sharedTracker.isLocked)
                    pegi.Lock_UnlockWindowClick(gameObject);

                MsgPainter.PleaseSelect.GetText().writeHint();

                TrySetOriginalTexture();

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
            else if (!Cfg)
            {

                TexMgmt.DependenciesInspect().changes(ref changed);

                canInspect = false;
            }

            if (canInspect && QcUnity.IsPrefab(gameObject))
            {
                "Inspecting a prefab.".nl();
                canInspect = false;
            }

            if (canInspect && !IsCurrentTool)
            {

                if (icon.Off.Click("Click to Enable Tool").changes(ref changed))
                {
                    IsCurrentTool = true;
                    enabled = true;

#if UNITY_EDITOR
                    var cs = GetComponents(typeof(Component));

                    foreach (var c in cs)
                        if (c.GetType() != typeof(PlaytimePainter))
                            InternalEditorUtility.SetIsInspectorExpanded(c, false);

                    QcUnity.FocusOn(null);
                    PainterCamera.refocusOnThis = gameObject;
#endif

                    CheckPreviewShader();
                }

                pegi.Lock_UnlockWindowClick(gameObject);

                canInspect = false;
            }

            double sinceUpdate = QcUnity.TimeSinceStartup() - PainterCamera.lastManagedUpdate;

            if (canInspect)
            {

                if (!TexMgmt.enabled)
                {
                    "Painter Camera is disabled".writeWarning();
                    if ("Enable".Click())
                        TexMgmt.enabled = true;
                }
                else if (!TexMgmt.gameObject.activeSelf)
                {
                    "Painter Camera Game Object is disabled".writeWarning();
                    if ("Enable".Click())
                        TexMgmt.gameObject.SetActive(true);
                }
                else if (sinceUpdate > 1)
                {
                    "It's been {0} seconds since the last managed update".F(sinceUpdate).writeWarning();

                    if ("Resubscribe camera to updates".Click())
                        TexMgmt.SubscribeToEditorUpdates();

                    return false;
                }

                selectedInPlaytime = this;

                if (
#if UNITY_2019_1_OR_NEWER
                    Application.isPlaying && (
#else
                    (
#endif

#if UNITY_EDITOR
                        (IsCurrentTool && terrain && !Application.isPlaying &&
                         InternalEditorUtility.GetIsInspectorExpanded(terrain)) ||
#endif
                        icon.On.Click("Click to Disable Tool")))
                {
                    IsCurrentTool = false;
                    MeshEditorManager.Inst.StopEditingMesh();
                    SetOriginalShaderOnThis();
                    UpdateOrSetTexTarget(TexTarget.Texture2D);

                }

                pegi.Lock_UnlockWindowClick(gameObject);

                InitIfNotInitialized();

                var image = TexMeta;

                var texMgmt = TexMgmt;

                var cfg = Cfg;

                var tex = GetTextureOnMaterial();
                if (!meshEditing && ((tex && image == null) || (image != null && !tex) ||
                                     (image != null && tex != image.texture2D && tex != image.CurrentTexture())))
                    textureWasChanged = true;

                #region Top Buttons

                if (MeshEditorManager.target && (MeshEditorManager.target != this))
                    MeshManager.StopEditingMesh();


                if (meshEditing)
                {
                    if (icon.Painter.Click("Edit Texture", ref changed))
                    {
                        CheckSetOriginalShader();
                        meshEditing = false;
                        CheckPreviewShader();
                        MeshMgmt.StopEditingMesh();
                        cfg.showConfig = false;
                        pegi.GameView.ShowNotification("Editing Texture");
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
                        pegi.GameView.ShowNotification("Editing Mesh");

                        if (SavedEditableMesh != null)
                            MeshMgmt.EditMesh(this, false);
                    }
                }


                pegi.toggle(ref cfg.showConfig, meshEditing ? icon.Mesh : icon.Painter, icon.Config,
                    "Tool Configuration");

                if (!PainterDataAndConfig.hideDocumentation)
                    pegi.PopUpService.fullWindowDocumentationClickOpen(LazyLocalization.InspectPainterDocumentation,
                        MsgPainter.AboutPlaytimePainter.GetText());

                #endregion

                if (cfg.showConfig)
                {

                    pegi.nl();

                    cfg.Nested_Inspect();

                }
                else
                {

                    if (meshCollider)
                    {
                        if (!meshCollider.sharedMesh)
                        {
                            pegi.nl();
                            "Mesh Collider has no mesh".writeWarning();
                            if (meshFilter && meshFilter.sharedMesh &&
                                "Assign".Click("Will assign {0}".F(meshFilter.sharedMesh)))
                                meshCollider.sharedMesh = meshFilter.sharedMesh;

                            pegi.nl();
                        }
                        else if (meshFilter && meshFilter.sharedMesh &&
                                 meshFilter.sharedMesh != meshCollider.sharedMesh)
                        {
                            
                                pegi.PopUpService.fullWindowWarningDocumentationClickOpen(
                                    "Collider and filter have different meshes. Painting may not be able to obtain a correct UV coordinates.",
                                    "Mesh collider mesh is different");
                        }

                    }

                    #region Mesh Editing

                    if (meshEditing)
                    {

                        if (terrain)
                        {
                            pegi.nl();
                            "Mesh Editor can't edit Terrain mesh".writeHint();

                        }
                        else
                        {

                            var mg = MeshMgmt;
                            mg.UndoRedoInspect().nl(ref changed);

                            var sm = SharedMesh;

                            if (sm)
                            {

                                if (this != MeshEditorManager.target)
                                    if (SavedEditableMesh != null)
                                        "Component has saved mesh data.".nl();

                                "Warning, this will change (or mess up) your model.".writeOneTimeHint("MessUpMesh");

                                if (MeshEditorManager.target != this)
                                {

                                    var ent = gameObject.GetComponent("pb_Entity");
                                    var obj = gameObject.GetComponent("pb_Object");

                                    if (ent || obj)
                                        "PRO builder detected. Strip it using Actions in the Tools/ProBuilder menu."
                                            .writeHint();
                                    else
                                    {
                                        if (Application.isPlaying)
                                            "Playtime Changes will be reverted once you try to edit the mesh again."
                                                .writeWarning();

                                        pegi.nl();

                                        "Mesh has {0} vertices".F(sm.vertexCount).nl();

                                        pegi.nl();

                                        const string confirmTag = "pp_EditThisMesh";

                                        if (!pegi.ConfirmationDialogue.IsRequestedFor(confirmTag))
                                        {

                                            if ("New Mesh".ClickConfirm("newMesh",
                                                SavedEditableMesh == null
                                                    ? "This will erase existing editable mesh. Proceed?"
                                                    : "Create a mesh?"))
                                            {
                                                Mesh = new Mesh();
                                                SavedEditableMesh = null;
                                                mg.EditMesh(this, false);
                                            }
                                        }

                                        if (SharedMesh && SharedMesh.vertexCount == 0)
                                        {
                                            pegi.nl();
                                            "Shared Mesh has no vertices, you may want to create a new mesh"
                                                .writeHint();
                                        }
                                        else
                                        {
                                            if (!pegi.ConfirmationDialogue.IsRequestedFor(confirmTag) && "Copy & Edit".Click())
                                                mg.EditMesh(this, true);

                                            if ("Edit this".ClickConfirm(confirmTag,
                                                    "Are you sure you want to edit the original one instead of editing a copy(safer)?")
                                                .nl())
                                                mg.EditMesh(this, false);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                gameObject.edit_ifNull(ref meshFilter).nl(ref changed);

                                gameObject.edit_ifNull(ref meshRenderer).nl(ref changed);

                                if (!sm && "Create Mesh".Click())
                                    Mesh = new Mesh();

                            }

                            if (IsEditingThisMesh)
                            {



                                if (_inspectedMeshEditorItems == -1)
                                    MeshMgmt.Inspect().nl();

                                if ("Profile".enter(ref _inspectedMeshEditorItems, 0))
                                {

                                    MsgPainter.MeshProfileUsage.DocumentationClick();


                                    if ((cfg.meshPackagingSolutions.Count > 1) && icon.Delete.Click(25))
                                    {
                                        for (int i = 0; i < cfg.meshPackagingSolutions.Count; i++)
                                        {
                                            var pr = cfg.meshPackagingSolutions[i];
                                            if (pr.name.Equals(selectedMeshProfile))
                                            {
                                                cfg.meshPackagingSolutions.RemoveAt(i);
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {

                                        pegi.nl();
                                        var mpf = MeshProfile;
                                        if (mpf == null)
                                            "There are no Mesh packaging profiles in the PainterDataObject"
                                                .writeWarning();
                                        else
                                        {
                                            if (!mpf.name.Equals(selectedMeshProfile))
                                                "Mesh profile {0} not found, using default one".writeWarning();

                                            pegi.nl();


                                            if (mpf.Inspect().nl())
                                            {
                                                selectedMeshProfile = mpf.name;
                                                MeshEditorManager.editedMesh.Dirty = true;
                                            }
                                        }

                                    }
                                }
                                else if (_inspectedMeshEditorItems == -1)
                                {
                                    if (pegi.select_iGotName(ref selectedMeshProfile,
                                            cfg.meshPackagingSolutions) &&
                                        IsEditingThisMesh)
                                        MeshEditorManager.editedMesh.Dirty = true;

                                    if (icon.Add.Click(25).nl())
                                    {
                                        var sol = new MeshPackagingProfile();
                                        cfg.meshPackagingSolutions.Add(sol);
                                        selectedMeshProfile = sol.name;
                                        //MeshProfile.name = "New Profile {0}".F(selectedMeshProfile);
                                    }
                                }

                                MeshManager.MeshOptionsInspect();
                            }
                        }

                        pegi.nl();

                    }

                    #endregion

                    #region Texture Editing

                    else
                    {

                        var texMeta = TexMeta;

                        var painterNotUiOrPlaying = Application.isPlaying || !IsUiGraphicPainter;

                        if (!LockTextureEditing && painterNotUiOrPlaying && !texMeta.errorWhileReading)
                        {

                            if (texMeta.ProcessEnumerator != null)
                            {
                                if (!cfg.moreOptions)
                                {
                                    pegi.nl();
                                    "Processing Texture".nl();
                                    texMeta.ProcessEnumerator.Inspect_AsInList().nl();
                                }
                            }
                            else
                            {

                                texMgmt.DependenciesInspect().changes(ref changed);

                                #region Undo/Redo & Recording

                                texMeta.Undo_redo_PEGI();

                                pegi.nl();

                                var cpu = texMeta.TargetIsTexture2D();

                                var mat = Material;
                                if (mat.IsProjected())
                                {
                                    "Projected UV Shader detected. Painting may not work properly".writeWarning();

                                    if ("Undo".Click().nl())
                                        mat.DisableKeyword(PainterShaderVariables.UV_PROJECTED);
                                }

                                #endregion

                                #region Brush

                                if (!cfg.moreOptions)
                                {

                                    pegi.nl();

                                    if (skinnedMeshRenderer)
                                    {
                                        if ("Update Collider from Skinned Mesh".Click())
                                            UpdateMeshCollider();

                                        if (!PainterDataAndConfig.hideDocumentation &&
                                            pegi.PopUpService.DocumentationClick("Why Update Collider from skinned mesh?"))
                                            pegi.PopUpService.FullWindwDocumentationOpen(
                                                ("To paint an object a collision detection is needed. Mesh Collider is not being animated. To paint it, update Mesh Collider with Update Collider button." +
                                                 " For ingame painting it is preferable to use simple colliders like Speheres to avoid per frame updates for collider mesh."
                                                ));

                                        pegi.nl();
                                    }

                                    if (texMeta != null)
                                        PreviewShaderToggleInspect().changes(ref changed);

                                    //if (!PainterCamera.GotBuffers && icon.Refresh.Click("Refresh Main Camera Buffers"))
                                    //  RenderTextureBuffersManager.RefreshPaintingBuffers();


                                    GlobalBrush.Nested_Inspect().changes(ref changed);

                                    if (!cpu && texMeta.texture2D && texMeta.width != texMeta.height)
                                        icon.Warning.write(
                                            "Non-square texture detected! Every switch between GPU and CPU mode will result in loss of quality.");

                                    var mode = GlobalBrush.GetBlitMode(cpu);
                                    var col = GlobalBrush.Color;

                                    if ((cpu || !mode.UsingSourceTexture || GlobalBrush.srcColorUsage !=
                                         Brush.SourceTextureColorUsage.Unchanged)
                                        && !IsTerrainHeightTexture && !pegi.PaintingGameViewUI)
                                    {
                                        if (pegi.edit(ref col).changes(ref changed))
                                            GlobalBrush.Color = col;

                                        MsgPainter.SampleColor.DocumentationClick();

                                    }

                                    pegi.nl();

                                    GlobalBrush.ColorSliders().nl(ref changed);

                                    if (cfg.showColorSchemes)
                                    {

                                        var scheme = cfg.colorSchemes.TryGet(cfg.selectedColorScheme);

                                        scheme?.PickerPEGI();

                                        if (cfg.showColorSchemes)
                                            "Scheme".select_Index(60, ref cfg.selectedColorScheme, cfg.colorSchemes)
                                                .nl(ref changed);

                                    }
                                }

                                #endregion
                            }
                        }
                        else
                        {
                            if (!NotUsingPreview)
                                PreviewShaderToggleInspect();

                            if (!painterNotUiOrPlaying)
                            {
                                pegi.nl();
                                "UI Element editing only works in Game View during Play.".writeWarning();
                            }
                        }

                        texMeta = TexMeta;

                        if (meshCollider && meshCollider.convex)
                        {
                            "Convex mesh collider detected. Most brushes will not work".writeWarning();
                            if ("Disable convex".Click())
                                meshCollider.convex = false;
                        }

                        #region Fancy Options

                        pegi.nl();
                        MsgPainter.TextureSettings.GetText().foldout(ref cfg.moreOptions);

                        if (cfg.moreOptions)
                            pegi.Indent();

                        if (texMeta != null && !cfg.moreOptions)
                        {

                            pegi.edit(ref texMeta.clearColor, 50);
                            if (icon.Clear.Click("Clear channels which are not ignored").changes(ref changed))
                            {
                                if (GlobalBrush.PaintingAllChannels)
                                {
                                    PainterCamera.Inst.DiscardAlphaBuffer();

                                    texMeta.SetPixels(texMeta.clearColor);
                                    texMeta.SetApplyUpdateRenderTexture();
                                }
                                else
                                {
                                    var wasRt = texMeta.target == TexTarget.RenderTexture;

                                    if (wasRt)
                                        UpdateOrSetTexTarget(TexTarget.Texture2D);

                                    texMeta.SetPixels(texMeta.clearColor, GlobalBrush.mask).SetAndApply();

                                    if (wasRt)
                                        UpdateOrSetTexTarget(TexTarget.RenderTexture);
                                }
                            }
                        }

                        pegi.nl();

                        var inspectionIndex = texMeta?.inspectedItems ?? _inspectedFancyItems;

                        if (cfg.moreOptions)
                        {

                            if (icon.Show.enter("Optional UI Elements", ref inspectionIndex, 7).nl())
                            {

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

                                if ("Color Schemes".toggle_enter(ref cfg.showColorSchemes,
                                    ref inspectedShowOptionsSubitem, 5, ref changed).nl_ifFolded())
                                    cfg.InspectColorSchemes();

                                if (texMeta != null)
                                {

                                    foreach (var module in texMeta.Modules)
                                        module.ShowHideSectionInspect().nl(ref changed);

                                    if (texMeta.isAVolumeTexture)
                                        "Show Volume Data in Painter"
                                            .toggleIcon(ref PainterCamera.Data.showVolumeDetailsInPainter)
                                            .nl(ref changed);

                                }

                                "Brush Dynamics"
                                    .toggleVisibilityIcon("Will modify scale and other values based on movement.",
                                        ref GlobalBrush.showBrushDynamics, true).nl(ref changed);

                                "URL field".toggleVisibilityIcon("Option to load images by URL", ref cfg.showUrlField,
                                    true).changes(ref changed);
                            }

                            if ("New Texture ".conditional_enter(!IsTerrainHeightTexture, ref inspectionIndex, 4).nl())
                            {

                                if (cfg.newTextureIsColor)
                                    "Clear Color".edit(ref cfg.newTextureClearColor).nl(ref changed);
                                else
                                    "Clear Value".edit(ref cfg.newTextureClearNonColorValue).nl(ref changed);

                                "Color Texture".toggleIcon("Will the new texture be a Color Texture",
                                    ref cfg.newTextureIsColor).nl(ref changed);

                                "Size:".select_Index("Size of the new Texture", 40,
                                    ref PainterCamera.Data.selectedWidthIndex,
                                    PainterDataAndConfig.NewTextureSizeOptions).nl();

                                "Click + next to texture field below to create texture using this parameters"
                                    .writeHint();

                                pegi.nl();

                            }

                            if ("Painter Modules (Debug)".enter(ref inspectionIndex, 5).nl())
                                Modules.Nested_Inspect().nl();

                            if (texMeta != null)
                            {
                                texMeta.inspectedItems = inspectionIndex;
                                texMeta.Inspect().changes(ref changed);
                            }
                            else _inspectedFancyItems = inspectionIndex;

                        }

                        if (texMeta != null)
                        {
                            var showToggles = (texMeta.inspectedItems == -1 && cfg.moreOptions);

                            texMeta.ComponentDependent_PEGI(showToggles, this).changes(ref changed);

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



                            }

                            if (showToggles || invertRayCast)
                            {

                                if (!IsUiGraphicPainter)
                                    "Invert RayCast".toggleIcon(
                                        "Will rayCast into the camera (for cases when editing from inside a sphere, mask for 360 video for example.)",
                                        ref invertRayCast).nl(ref changed);
                                else
                                    invertRayCast = false;
                            }

                            if (cfg.moreOptions)
                                pegi.line(Color.red);

                            if (texMeta.enableUndoRedo && texMeta.backupManually && "Backup for UNDO".Click())
                                texMeta.OnStrokeMouseDown_CheckBackup();

                            if (texMeta.dontRedoMipMaps && icon.Refresh.Click("Update Mipmaps now").nl())
                                texMeta.SetAndApply();
                        }

                        pegi.UnIndent();

                        #endregion

                        #region Save Load Options

                        if (!cfg.showConfig)
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
                                    texMeta = TexMeta;
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

                            #region Texture 

                            if (cfg.showUrlField)
                            {

                                "URL".edit(40, ref _tmpUrl);
                                if (_tmpUrl.Length > 5 && icon.Download.Click())
                                {
                                    loadingOrder.Add(PainterCamera.DownloadManager.StartDownload(_tmpUrl),
                                        GetMaterialTextureProperty);
                                    _tmpUrl = "";
                                    pegi.GameView.ShowNotification("Loading for {0}".F(GetMaterialTextureProperty));
                                }

                                pegi.nl();
                                if (loadingOrder.Count > 0)
                                    "Loading {0} texture{1}".F(loadingOrder.Count, loadingOrder.Count > 1 ? "s" : "")
                                        .nl();

                                pegi.nl();

                            }


                            var ind = SelectedTexture;
                            if (pegi.select_Index(ref ind, GetAllTextureNames()).changes(ref changed))
                            {
                                SetOriginalShaderOnThis();
                                SelectedTexture = ind;
                                OnChangedTexture_OnMaterial();
                                CheckPreviewShader();
                                texMeta = TexMeta;
                                if (texMeta == null)
                                    _nameHolder = gameObject.name + "_" + GetMaterialTextureProperty;
                            }

                            if (texMeta != null)
                            {
                                UpdateTilingFromMaterial();

                                if (texMeta.errorWhileReading)
                                {

                                    icon.Warning.write(
                                        "THere was error while reading texture. (ProBuilder's grid texture is not readable, some others may be to)");

                                    if (texMeta.texture2D && icon.Refresh.Click("Retry reading the texture"))
                                        texMeta.From(texMeta.texture2D, true);

                                }
                                else if (pegi.toggle(ref texMeta.lockEditing, icon.Lock, icon.Unlock,
                                    "Lock/Unlock editing of {0} Texture.".F(texMeta.GetNameForInspector()), 25))
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

                                var texNames = GetAllTextureNames();

                                if (texNames.Count > SelectedTexture)
                                {
                                    var param = GetMaterialTextureProperty;

                                    const string newTexConfirmTag = "pp_nTex";

                                    if ((((texMeta == null) &&
                                          icon.NewTexture.Click("Create new texture2D for " + param)) ||
                                         (texMeta != null && icon.NewTexture.ClickConfirm(newTexConfirmTag, texMeta,
                                              "Replace " + param + " with new Texture2D " + texScale + "*" + texScale)))
                                        .nl(ref changed))
                                    {
                                        if (isTerrainHeight)
                                            CreateTerrainHeightTexture(_nameHolder);
                                        else
                                            CreateTexture2D(texScale, _nameHolder, cfg.newTextureIsColor);
                                    }

                                    if (!pegi.ConfirmationDialogue.IsRequestedFor(newTexConfirmTag))
                                    {

                                        if (cfg.showRecentTextures)
                                        {

                                            var texName = GetMaterialTextureProperty;

                                            List<TextureMeta> recentTexs;
                                            if (texName != null &&
                                                PainterCamera.Data.recentTextures.TryGetValue(texName,
                                                    out recentTexs) &&
                                                (recentTexs.Count > 0)
                                                && (texMeta == null || (recentTexs.Count > 1) ||
                                                    (texMeta != recentTexs[0].texture2D.GetImgDataIfExists()))
                                                && "Recent Textures:".select(100, ref texMeta, recentTexs)
                                                    .nl(ref changed))
                                                ChangeTexture(texMeta.ExclusiveTexture());

                                        }

                                        if (texMeta == null && cfg.allowExclusiveRenderTextures &&
                                            "Create Render Texture".Click(ref changed))
                                            CreateRenderTexture(texScale, _nameHolder);

                                        if (texMeta != null && cfg.allowExclusiveRenderTextures)
                                        {
                                            if (!texMeta.renderTexture && "Add Render Tex".Click(ref changed))
                                                texMeta.AddRenderTexture();

                                            if (texMeta.renderTexture)
                                            {

                                                if ("Replace RendTex".Click(
                                                    "Replace " + param + " with Rend Tex size: " + texScale,
                                                    ref changed))
                                                    CreateRenderTexture(texScale, _nameHolder);

                                                if ("Remove RendTex".Click().nl(ref changed))
                                                {

                                                    if (texMeta.texture2D)
                                                    {
                                                        UpdateOrSetTexTarget(TexTarget.Texture2D);
                                                        texMeta.renderTexture = null;
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

                                if (texMeta == null)
                                    "_Name:".edit("Name for new texture", 40, ref _nameHolder).nl();

                            }

                            pegi.nl();
                            pegi.space();
                            pegi.nl();

                            #endregion

                            #region Texture Saving/Loading

                            if (!LockTextureEditing)
                            {
                                pegi.nl();
                                if (!IsTerrainControlTexture)
                                {
                                    if (Application.isEditor)
                                        "Unless saved, the texture will loose all changes when scene is offloaded or Unity Editor closed."
                                            .writeOneTimeHint("_pp_hint_saveTex").nl();

                                    texMeta = TexMeta;

#if UNITY_EDITOR
                                    string orig = null;
                                    if (texMeta.texture2D)
                                    {
                                        orig = texMeta.texture2D.GetPathWithout_Assets_Word();

                                        if (orig != null && icon.Load.ClickUnFocus("Will reload " + orig))
                                        {
                                            ForceReimportMyTexture(orig);
                                            texMeta.saveName = texMeta.texture2D.name;
                                            if (terrain)
                                                UpdateModules();
                                        }
                                    }

                                    pegi.edit(ref texMeta.saveName);

                                    if (texMeta.texture2D)
                                    {

                                        if (!texMeta.saveName.SameAs(texMeta.texture2D.name) &&
                                            icon.Refresh.Click(
                                                "Use current texture name ({0})".F(texMeta.texture2D.name)))
                                            texMeta.saveName = texMeta.texture2D.name;

                                        var destPath = GenerateTextureSavePath();
                                        var existsAtDestination = TextureExistsAtDestinationPath();
                                        var originalExists = !orig.IsNullOrEmpty();
                                        var sameTarget = originalExists && orig.Equals(destPath);
                                        var sameTextureName =
                                            originalExists && texMeta.texture2D.name.Equals(texMeta.saveName);

                                        if (!existsAtDestination || sameTextureName)
                                        {
                                            if ((sameTextureName ? icon.Save : icon.SaveAsNew).Click(sameTextureName
                                                ? "Will Update " + orig
                                                : "Will save as " + destPath))
                                            {

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
                                            RewriteOriginalTexture_Rename(texMeta.saveName);

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

                    foreach (var p in CameraModuleBase.ComponentInspectionPlugins)
                        p.ComponentInspector().nl(ref changed);

                }

                pegi.nl();

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

                if (!NotUsingPreview && icon.PreviewShader
                        .Click("Applies changes made on Texture to Actual physical Unity Terrain.", 45)
                        .changes(ref changed))
                {
                    Preview_To_UnityTerrain();
                    Unity_To_Preview();

                    MatDta.usePreviewShader = false;
                    SetOriginalShaderOnThis();

                }

                PainterCamera.Data.Brush.MaskSet(ColorMask.A, true);

                if (tht.GetTextureMeta() != null && NotUsingPreview && icon.OriginalShader
                        .Click("Applies changes made in Unity terrain Editor", 45).changes(ref changed))
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

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!TexMgmt || this != TexMgmt.FocusedPainter) return;

            if (meshEditing && !Application.isPlaying)
                MeshEditorManager.Inst.DRAW_Lines(true);

            var br = GlobalBrush;

            if (NotUsingPreview && !LockTextureEditing && _lastMouseOverObject == this && IsCurrentTool &&
                Is3DBrush() && br.showingSize && !Cfg.showConfig)
                Gizmos.DrawWireSphere(stroke.posTo, br.Size(true) * 0.5f
                );

            foreach (var p in CameraModuleBase.GizmoPlugins)
                p.PlugIn_PainterGizmos(this);
        }
#endif

        #endregion

    }

    public static partial class PaintCommand
    {
        public class Painter : WorldSpace
        {

            public PlaytimePainter painter;

            public override bool Is3DBrush =>  painter.Is3DBrush(Brush);
            
            public override GameObject GameObject
            {
                get { return painter.gameObject; }
                set { }
            }

            public override SkinnedMeshRenderer SkinnedMeshRenderer
            {
                get { return painter.skinnedMeshRenderer; }
                set { }
            }

            public override Mesh Mesh
            {
                get { return painter.GetMesh(); }
                set { }
            }
            
            public override List<int> SelectedSubmeshes
            {
                get { return new List<int> {painter.selectedSubMesh}; }
                set { }
            }

            public override int SubMeshIndexFirst
            {
                get { return painter.selectedSubMesh; }
                set { if (painter)
                    painter.selectedSubMesh = value; }
            }

            public Painter(Stroke stroke, Brush brush, PlaytimePainter painter) : base(stroke, painter.TexMeta, brush,
                painter.skinnedMeshRenderer, 0, painter.gameObject)
            {
                SkinnedMeshRenderer = painter.skinnedMeshRenderer;
                Mesh = painter.GetMesh();
                SubMeshIndexFirst = painter.selectedSubMesh;
                GameObject = painter.gameObject;
                this.painter = painter;
            }
        }
    }
}