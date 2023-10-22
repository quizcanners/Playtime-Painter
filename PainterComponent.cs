using System;
using System.Collections.Generic;
using QuizCanners.Inspect;
using PainterTool.CameraModules;
using PainterTool.ComponentModules;
using PainterTool.MeshEditing;
using QuizCanners.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using QuizCanners.Migration;

namespace PainterTool
{
  
    [ExecuteInEditMode]
    public partial class PainterComponent : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, ICfg
    {
        #region StaticGetters

        public static bool IsCurrentTool
        {
            get
            {

#if UNITY_EDITOR
                if (!Application.isPlaying)
                    return UnityEditor.EditorTools.ToolManager.activeToolType == typeof(PlaytimePainterEditorTool);
#endif

                return SO_PainterDataAndConfig.toolEnabled;
            }
            set { 
                SO_PainterDataAndConfig.toolEnabled = value;

#if UNITY_EDITOR
                if (value)
                {
                    UnityEditor.EditorTools.ToolManager.SetActiveTool(typeof(PlaytimePainterEditorTool));
                } else if (IsCurrentTool) 
                {
                    UnityEditor.EditorTools.ToolManager.RestorePreviousPersistentTool();
                }
#endif
            }
        }

        protected static GridNavigator Grid => MeshPainting.Grid;

        private static Brush GlobalBrush
        {
            get
            {
                if (Painter.Data.Brush == null) 
                {
                    QcLog.ChillLogger.LogErrorOnce("No Brush found in Config", key: "noBrush");
                }

                return Painter.Data.Brush;
            }
        }
        public BrushTypes.Base GlobalBrushType => GlobalBrush.GetBrushType(TexMeta != null ? TexMeta.Target : TexTarget.Texture2D);

        private bool NeedsGrid => this.NeedsGrid();

        #endregion

        #region Dependencies

        public Renderer meshRenderer;
        public SkinnedMeshRenderer skinnedMeshRenderer;
        public Graphic uiGraphic;
        //public Terrain terrain;

        [SerializeField] private MeshFilter meshFilter;
        public MeshCollider meshCollider;
       

        #endregion

        #region Modules

        private PainterModules _modulesContainer;

        internal PainterModules Modules
        {
            get
            {
                _modulesContainer ??= new PainterModules(this);

                return _modulesContainer;
            }
        }

        internal class PainterModules : TaggedModulesList<ComponentModuleBase>
        {
            public override void OnAfterInitialize()
            {
                for (int i=modules.Count-1; i>=0; i--) 
                {
                    if (modules[i] == null)
                        modules.RemoveAt(i);
                }

                foreach (var p in modules)
                    p.painter = painter;
            }

            public PainterComponent painter;

            public PainterModules(PainterComponent painter)
            {
                this.painter = painter;
            }
        }

        internal T GetModule<T>() where T : ComponentModuleBase => Modules.GetModule<T>();

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

            foreach (var pl in CameraModuleBase.BrushPlugins)
            {
                var isA3D = pl.IsA3DBrush(this, brush, ref overrideOther);
                if (overrideOther)
                    return isA3D;
            }

            return brush.Is3DBrush(TexMeta);
        }

        public bool invertRayCast;

        public Stroke stroke = new();
        private Painter.Command.ForPainterComponent _command;

        public Painter.Command.ForPainterComponent Command
        {
            get
            {
                if (_command == null)
                    _command = new Painter.Command.ForPainterComponent(stroke, GlobalBrush, this);
                else
                {
                    _command.TextureData = TexMeta;
                    _command.Brush = GlobalBrush;
                    _command.Stroke = stroke;
                }

                if (!_command.painter)
                    Debug.LogError(QcLog.IsNull(_command.painter, nameof(Painter.Command)));//"Painter inside a command is zero");

                return _command;
            }
        }

        public static PainterComponent currentlyPaintedObjectPainter;
        private static PainterComponent _lastMouseOverObject;

        private double _mouseButtonTime;

        private void StrokeStateFromInputs()
        {
            stroke.MouseUpEvent = Input.GetMouseButtonUp(0);
            stroke.MouseDownEvent = Input.GetMouseButtonDown(0);
        }

        private void StrokeFromGrid()
        {
            stroke.posTo = MeshPainting.LatestMouseToGridProjection;
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
                    stroke.TrySetPreviousValues();

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

                    if (!stroke.MouseDownEvent || CanPaintOnMouseDown())
                        GlobalBrush.Paint(Command);
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

            var blocker = GetPaintingBlocker();

            if (blocker.IsNullOrEmpty() == false)
            {
                //if (stroke.MouseDownEvent)
                  //  pegi.GameView.ShowNotification("Can't Paint: " + blocker);

                return;
            }

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
        public void OnMouseOverSceneView(RaycastHit hit)
        {

            if (!GetPaintingBlocker().IsNullOrEmpty())
                return;

            if (NeedsGrid)
                StrokeFromGrid();
            else if (!ProcessHit(hit, stroke))
                return;

            ProcessStrokeState();

        }
#endif

        public string GetPaintingBlocker()
        {

            if (!IsCurrentTool) 
                return "Is Not Current Tool";

            _lastMouseOverObject = this;

            if (TextureEditingBlocked)
                return "Texture Editing Locked";

            if (!enabled)
                return "Component is disabled";

            if (MeshPainting.target)
                return "Is Editing mesh of this object";

            if (stroke.MouseDownEvent || stroke.MouseUpEvent)
                InitIfNotInitialized();

            if (TexMeta == null)
                return "No Texture to edit";

            return null;

        }

        private readonly QcLog.ChillLogger _chillLogger = new(nameof(PainterComponent));

        private bool CastRayPlaytime(Stroke st, Vector3 mousePos)
        {

            var cam = Painter.Camera.MainCamera;

            if (!cam)
            {
                _chillLogger.Log("No Main Camera to RayCast from", target: this);
                return false;
            }

            var ray = PrepareRay(cam.ScreenPointToRay(mousePos));

            if (invertRayCast && meshRenderer)
            {
                ray.origin = ray.GetPoint(meshRenderer.bounds.max.magnitude * 1.5f);
                ray.direction = -ray.direction;
            }

            return Physics.Raycast(ray, out RaycastHit hit, float.MaxValue) && ProcessHit(hit, st);
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
            // var subMesh =  //this.GetMesh().GetSubMeshNumber(hit.triangleIndex);
            if (autoSelectMaterialByNumberOfPointedSubMesh)
            {
                if (hit.TryGetSubMeshIndex_MAlloc(out var subMesh) && subMesh != selectedSubMesh)
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

            if (id == null) 
                return hit.textureCoord;

            var uv = id[TextureCfgFlags.Texcoord2] ? hit.textureCoord2 : hit.textureCoord;

            foreach (var p in Modules)
                if (p.OffsetAndTileUv(hit, ref uv))
                    return uv;

            uv.Scale(id.Tiling);
            uv += id.Offset;

            return uv;
        }

        public void SampleTexture(Vector2 uv)
        {
            Painter.Camera.OnBeforeBlitConfigurationChange();
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
            if (!IsCurrentTool || (TextureEditingBlocked && !IsEditingThisMesh))
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

            if (meshEditing && (MeshPainting.target != this))
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
                shd = Painter.Data.previewMesh;
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
                    shd = Painter.Data.previewBrush;
            }

            if (!shd)
                Debug.LogError(QcLog.IsNull(shd, nameof(SetPreviewShader)));
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

            if (Painter.Camera)
                Painter.Camera.FinalizePreviousAlphaDataTarget();

        }

        #endregion

        #region  Texture MGMT 

        public TextureMeta TexMeta => GetTextureOnMaterial().GetTextureMeta();

        private int SelectedTexture
        {
            get => MatDta?.selectedTexture ?? 0;
            
            set
            {
                var md = MatDta;
                if (md != null) 
                    md.selectedTexture = value;
            }
        }

        private void UpdateTilingFromMaterial()
        {
            var id = TexMeta;

            var fieldName = GetMaterialTextureProperty();
            var mat = Material;
            if (IsUsingPreview)
            {
                id.Tiling = mat.GetTiling(PainterShaderVariables.PreviewTexture);
                id.Offset = mat.GetOffset(PainterShaderVariables.PreviewTexture);
                return;
            }

            if (fieldName != null)
                foreach (var nt in Modules)
                    if (nt.UpdateTilingFromMaterial(fieldName))
                        return;

            if (!mat || fieldName == null || id == null) 
                return;

            id.Tiling = mat.GetTiling(fieldName);
            id.Offset = mat.GetOffset(fieldName);
        }

        public void UpdateTilingToMaterial()
        {
            var id = TexMeta;
            var fieldName = GetMaterialTextureProperty();
            var mat = Material;
            if (IsUsingPreview)
            {
                mat.SetTiling(PainterShaderVariables.PreviewTexture, id.Tiling);
                mat.SetOffset(PainterShaderVariables.PreviewTexture, id.Offset);
                return;
            }

            if (!mat || fieldName == null || id == null) return;
            mat.SetTiling(fieldName, id.Tiling);
            mat.SetOffset(fieldName, id.Offset);
        }

        private void OnChangedTexture_OnMaterial() => ChangeTexture(GetTextureOnMaterial());
        

        public void ChangeTexture(TextureMeta id) => ChangeTexture(id.CurrentTexture());

        private void ChangeTexture(Texture texture)
        {
            textureWasChanged = false;

            if (!texture)
            {
                RemoveTextureFromMaterial();
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
                    id[TextureCfgFlags.Texcoord2] = field.ToString().Contains(PainterShaderVariables.isUV2DisaplyNameTag);
            }

            SetTextureOnMaterial(texture);
            UpdateOrSetTexTarget(id.Target);
            UpdateTilingFromMaterial();
        }

        public PainterComponent SetTexTarget(Brush br)
        {
            if (TexMeta.Target != br.FallbackTarget)
                UpdateOrSetTexTarget(br.FallbackTarget);

            return this;
        }

        public void UpdateOrSetTexTarget(TexTarget dst)
        {
            InitIfNotInitialized();

            var id = TexMeta;

            if (id == null)
                return;

            if (id.Target == dst)
                return;

            id.ChangeDestination(dst, GetMaterial(true).GetMaterialPainterMeta(), GetMaterialTextureProperty(), this);
            CheckPreviewShader();
        }

        private void ReEnableRenderTexture()
        {
            if (TextureEditingBlocked) 
                return;

            OnEnable();
            OnChangedTexture_OnMaterial();

            if (TexMeta != null)
                UpdateOrSetTexTarget(TexTarget.RenderTexture); // set it to Render Texture
        }

        private void CreateTexture2D(int size, string textureName, bool isColor)
        {

            var id = TexMeta;

            var gotRenderTextureData = id != null && size == id.Width && size == id.Width && id.TargetIsRenderTexture();

            var texture = new Texture2D(size, size, TextureFormat.ARGB32, true, !isColor);

            if (gotRenderTextureData && (!id.Texture2D || textureName.SameAs(id.saveName)))
                id.Texture2D = texture;

            texture.wrapMode = TextureWrapMode.Repeat;

            ChangeTexture(texture);

            id = TexMeta;

            id.saveName = textureName;
            texture.name = textureName;

            var needsFullUpdate = false;

#if UNITY_EDITOR

            var needsReColorizing = false;
#endif

            var colorData = isColor ? Painter.Data.newTextureClearNonColorValue : Painter.Data.newTextureClearColor;

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

            var importer = id.Texture2D.GetTextureImporter_Editor();

            var needReimport = importer.WasNotReadable_Editor();
            needReimport |= importer.WasWrongIsColor_Editor(isColor);

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

            ChangeTexture(nt.RenderTexture);

            Painter.Camera.Render(previous.CurrentTexture(), nt);

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
                else if (uiGraphic)
                    uiGraphic.material = value;
                else
                    _fallbackMaterial = value;
            }
        }

        internal MaterialMeta MatDta => Material.GetMaterialPainterMeta();

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
                result = null;


            return result;
        }
        
        public Material[] Materials
        {
            get
            {

                if (!uiGraphic)
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
                else
                    _fallbackMaterial = value.TryGet(0);
            }
        }

        private MaterialMeta _lastFetchedTextureNamesFor;
        private List<ShaderProperty.TextureValue> _lastTextureNames = new();

        private List<ShaderProperty.TextureValue> GetAllTextureNames()
        {

            var materialData = MatDta;

            if (!Application.isEditor)
                return materialData?.materialsTextureFields;

            bool sameAsBefore = _lastFetchedTextureNamesFor == materialData && _lastTextureNames.Count > 0;

            if (NotUsingPreview && (Application.isEditor || !sameAsBefore))
            {

                _lastTextureNames.Clear();

                _lastTextureNames.AddRange(Material.MyGetTextureProperties_Editor());

                foreach (var nt in Modules)
                    nt?.GetNonMaterialTextureNames(ref _lastTextureNames);

                _lastFetchedTextureNamesFor = materialData;

                if (materialData != null)
                    materialData.materialsTextureFields = _lastTextureNames;


            }
            else if (!sameAsBefore)
                _lastTextureNames.Clear();

            return _lastTextureNames;
        }

        public ShaderProperty.TextureValue GetMaterialTextureProperty() => GetAllTextureNames().TryGet(SelectedTexture);

        private Texture GetTextureOnMaterial()
        {

            if (IsUsingPreview)
            {
                if (meshEditing) return null;
            
                var m = Material;
                return m ? Material.Get(PainterShaderVariables.PreviewTexture) : null;
                
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

        internal void SetTextureOnMaterial(TextureMeta id) =>
            SetTextureOnMaterial(GetMaterialTextureProperty(), id.CurrentTexture());

        internal TextureMeta SetTextureOnMaterial(Texture tex) => SetTextureOnMaterial(GetMaterialTextureProperty(), tex);

        private TextureMeta SetTextureOnMaterial(ShaderProperty.TextureValue property, Texture tex)
        {
            var id = SetTextureOnMaterial(property, tex, GetMaterial(true));
            CheckPreviewShader();
            return id;
        }

        internal TextureMeta SetTextureOnMaterial(ShaderProperty.TextureValue property, Texture tex, Material mat)
        {

            var id = tex.GetTextureMeta();

            if (property != null)
            {
                if (id != null)
                    Painter.Data.recentTextures.AddIfNew(property, id);

                foreach (var nt in Modules)
                    if (nt.SetTextureOnMaterial(property, id))
                        return id;
            }

            if (!mat) return id;
            if (property != null)
                mat.Set(property, id.CurrentTexture());

            if (IsUsingPreview)
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

            mat.SetOffset(PainterShaderVariables.PreviewTexture, id.Offset);
            mat.SetTiling(PainterShaderVariables.PreviewTexture, id.Tiling);

        }

        public Material InstantiateMaterial(bool saveIt)
        {

            CheckSetOriginalShader();

            if (TexMeta != null && Material)
                UpdateOrSetTexTarget(TexTarget.Texture2D);

            InitIfNotInitialized();

            var mat = GetMaterial(true);

            Material = new Material(mat ? mat : Painter.Data.defaultMaterial);
            CheckPreviewShader();
            
            var material = Material;

            if (material)
            {
                material.name = gameObject.name;

                if (saveIt)
                {
#if UNITY_EDITOR
                    QcFile.Save.Asset(material, Painter.Data.materialsFolderName, ".mat", true);
                    CheckPreviewShader();
#endif
                }
            }

            OnChangedTexture_OnMaterial();

            var id = TexMeta;

            if (id != null && Material)
                UpdateOrSetTexTarget(id.Target);

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
        public MeshPackagingProfile MeshProfile => Painter.Data.GetMeshPackagingProfile(selectedMeshProfile);

        [SerializeField] private CfgData savedMeshData;
        [SerializeField] private Mesh meshDataSavedFor;

        public CfgData SavedEditableMesh
        {
            get
            {
                var dta = savedMeshData.ToString();
                if (dta != null && (savedMeshData.ToString().Length == 0 || (meshDataSavedFor != this.GetMesh())))
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
            else if (mesh && meshCollider)
                 meshCollider.AssignMeshAsCollider(mesh);

        }

        public bool IsEditingThisMesh => IsCurrentTool && meshEditing && (MeshPainting.target == this);

        #endregion

        #region UI Canvas MGMT

        private static PainterComponent _mouseOverPaintableGraphicElement;
        private Vector2 _uiUv;
        [NonSerialized] private Camera _clickCamera;

        public bool IsUiGraphicPainter => !meshRenderer && uiGraphic;

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
            var rt = uiGraphic.rectTransform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, position, cam, out Vector2 localCursor))
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

            uvClick.Scale(id.Tiling);
            uvClick += id.Offset;
            stroke.unRepeatedUv = uvClick + id.Offset;
            stroke.uvTo = stroke.unRepeatedUv.To01Space();
            PreviewShader_StrokePosition_Update();
            return true;
        }

        #endregion

        #region COMPONENT MGMT 

        public bool TextureEditingBlocked
        {
            get
            {
                if (meshEditing || !Painter.Camera)
                    return true;
                var i = TexMeta;
                return i == null || i.OtherTexture || i.IsReadable == false;
            }
        }

        public bool forcedMeshCollider;
        [NonSerialized] public bool initialized;
        public bool autoSelectMaterialByNumberOfPointedSubMesh = false;

        public const string OnlineManual =
            "https://docs.google.com/document/d/170k_CE-rowVW9nsAo3EUqVIAWlZNahC0ua11t1ibMBo/edit?usp=sharing";

        private static readonly List<string> TextureEditorIgnore = new() { MeshEditorManager.VertexEditorUiElementTag, MeshEditorManager.ToolComponentTag, "o"};

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

            if (!Painter.Camera || MeshPainting.target != this) return;

            Painter.MeshManager.StopEditingMesh();

        }

        private void OnEnable()
        {
            isBeingDisabled = false;

            if (!meshRenderer)
                meshRenderer = GetComponent<MeshRenderer>();

            this.Decode(_cfgData);
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
                if (!meshCollider) return;
                meshCollider.sharedMesh = null;
                meshCollider.sharedMesh = colliderForSkinnedMesh;
            }
            catch (Exception ex)
            {
                _chillLogger.Log(ex, 1000, this);
            }
        }

        public void InitIfNotInitialized()
        {

            if (initialized && ((meshCollider && meshRenderer) || uiGraphic)) 
                return;

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

            if (Painter.Camera && (this == Painter.Camera.autodisabledBufferTarget) && (!TextureEditingBlocked) &&
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

        private readonly Gate.Frame _frameGate = new();
        public void ManagedUpdateOnFocused()
        {
            if (!_frameGate.TryEnter()) 
                return;
            
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
                    if (!Painter.DownloadManager.TryGetTexture(l.Key, out Texture tex, true)) continue;

                    if (tex)
                    {
                        var texMeta = SetTextureOnMaterial(l.Value, tex);
                        if (texMeta != null)
                        {
                            
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
                Painter.MeshManager.DRAW_Lines(false);

            if (GlobalBrush!=null && GlobalBrush.previewDirty)
                Painter.Camera.SHADER_BRUSH_UPDATE(_command);

            if (textureWasChanged)
                OnChangedTexture_OnMaterial();

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
            Singleton_PainterCamera.SHADER_POSITION_AND_PREVIEW_UPDATE(stroke, hide);

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

            Command.TextureData = id;

            Painter.Camera.SHADER_BRUSH_UPDATE(_command.Reset());

            foreach (var p in Modules)
                p.Update_Brush_Parameters_For_Preview_Shader();

            Singleton_PainterCamera._previewAlpha = 1;

        }

        #endregion
    }
}