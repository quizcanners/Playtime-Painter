using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using System.IO;
using PlayerAndEditorGUI;
using SharedTools_Stuff;


namespace Playtime_Painter
{

    [AddComponentMenu("Mesh/Playtime Painter")]
    [HelpURL(WWW_Manual)]
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class PlaytimePainter : PlaytimeToolComponent, ISTD, IPEGI
    {

        #region StaticGetters

#if PEGI
        public static pegi.CallDelegate plugins_ComponentPEGI;
#endif
        public static PainterBoolPlugin plugins_GizmoDraw;

        public static bool IsCurrent_Tool() => enabledTool == typeof(PlaytimePainter);

        protected static PainterDataAndConfig Cfg => PainterCamera.Data;

        protected static BrushConfig GlobalBrush => Cfg.brushConfig;

        public BrushType GlobalBrushType => GlobalBrush.Type(ImgData.TargetIsTexture2D());

        protected static PainterCamera TexMGMT => PainterCamera.Inst;

        protected static MeshManager MeshMGMT => MeshManager.Inst;

        protected static GridNavigator Grid => GridNavigator.Inst();

        public override string ToolName() => PainterDataAndConfig.ToolName;

        private bool NeedsGrid => this.NeedsGrid();

        public override Texture ToolIcon()
        {
            return icon.Painter.getIcon();
        }

        #endregion

        #region Dependencies

        public Renderer meshRenderer;
        public SkinnedMeshRenderer skinnedMeshRendy;
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
            get { selectedMeshProfile = Mathf.Max(0, Mathf.Min(selectedMeshProfile, Cfg.meshPackagingSolutions.Count - 1)); return Cfg.meshPackagingSolutions[selectedMeshProfile]; }
        }

        public string meshNameHolder;

        public string _savedMeshData;
        public Mesh meshDataSavedFor;
        public string SavedEditableMesh
        {
            get
            {

                if (meshDataSavedFor != this.GetMesh())
                    _savedMeshData = null;

                if ((_savedMeshData != null) && (_savedMeshData.Length == 0))
                    _savedMeshData = null;

                return _savedMeshData;
            }
            set { meshDataSavedFor = this.GetMesh(); _savedMeshData = value; }

        }

        public int selectedSubmesh = 0;
        public Material Material
        {
            get { return GetMaterial(false); }
            set
            {

                if (meshRenderer != null && selectedSubmesh < meshRenderer.sharedMaterials.Length)
                {
                    var mats = meshRenderer.sharedMaterials;
                    mats[selectedSubmesh] = value;
                    meshRenderer.materials = mats;
                }
                else if (terrain != null)
                {
                    terrain.materialTemplate = value;
                    terrain.materialType = value ? Terrain.MaterialType.Custom : Terrain.MaterialType.BuiltInStandard;
                }
            }
        }

        public MaterialData MatDta { get { return Material.GetMaterialData(); } }

        public ImageData ImgData { get { return GetTextureOnMaterial().GetImgData(); } }

        public string nameHolder = "unnamed";

        public int selectedAtlasedMaterial = -1;

        public List<PainterPluginBase> plugins;
        PainterPluginBase lastFetchedPlugin;
        public T GetPlugin<T>() where T : PainterPluginBase
        {

            T returnPlug = null;

            if (lastFetchedPlugin != null && lastFetchedPlugin.GetType() == typeof(T))
                returnPlug = (T)lastFetchedPlugin;
            else
                foreach (var p in plugins)
                    if (p.GetType() == typeof(T))
                        returnPlug = (T)p;

#if UNITY_EDITOR
            if (returnPlug != null)
                Undo.RecordObject(returnPlug, "Added new Item");
#endif

            lastFetchedPlugin = returnPlug;

            return returnPlug;
        }

        public int SelectedTexture
        {
            get { var md = MatDta; return md == null ? 0 : md._selectedTexture; }
            set { var md = MatDta; if (md != null) md._selectedTexture = value; }
        }

        #endregion

        #region Painting

        public StrokeVector stroke = new StrokeVector();

        public static PlaytimePainter currently_Painted_Object;
        public static PlaytimePainter last_MouseOver_Object;

        public PlaytimePainter Paint(StrokeVector stroke, BrushConfig brush)
        {
            return brush.Paint(stroke, this);
        }

#if BUILD_WITH_PAINTER
        double mouseBttnTime = 0;

        public void OnMouseOver()
        {

            stroke.mouseUp = Input.GetMouseButtonUp(0);
            stroke.mouseDwn = Input.GetMouseButtonDown(0);

            if (Input.GetMouseButtonDown(1))
                mouseBttnTime = Time.time;
            if ((Input.GetMouseButtonUp(1)) && ((Time.time - mouseBttnTime) < 0.2f))
                FocusOnThisObject();

            if (!CanPaint())
                return;

            CheckPreviewShader();

            Vector3 mousePos = Input.mousePosition;

            if (CastRayPlaytime(stroke, mousePos))
            {

                bool Cntr_Down = (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));

                ProcessMouseGrag(Cntr_Down);

                if ((Input.GetMouseButton(0) || (stroke.mouseUp)) && (!Cntr_Down))
                {

                    if (currently_Painted_Object != this)
                    {
                        currently_Painted_Object = this;
                        stroke.SetPreviousValues();
                        FocusOnThisObject();
                    }

                    if ((!stroke.mouseDwn) || (CanPaintOnMouseDown(GlobalBrush)))
                        GlobalBrush.Paint(stroke, this);
                    else RecordingMGMT();

                    if (stroke.mouseUp)
                        currently_Painted_Object = null;
                }
            }
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

            if ((currently_Painted_Object != this) && (stroke.mouseDwn))
            {
                stroke.firstStroke = true;
                currently_Painted_Object = this;
                FocusOnThisObject();
                stroke.uvFrom = stroke.uvTo;
            }

            bool control = Event.current != null ? (Event.current.control) : false;

            ProcessMouseGrag(control);

            if ((currently_Painted_Object == this))
            {

                if ((!stroke.mouseDwn) || CanPaintOnMouseDown(GlobalBrush))
                {

                    GlobalBrush.Paint(stroke, this);

                    Update();
                }
                else
                    RecordingMGMT();

            }

            if (currently_Painted_Object != this)
                currently_Painted_Object = null;

            stroke.mouseDwn = false;

        }
#endif

        bool CanPaint()
        {

            if (!IsCurrentTool()) return false;

            last_MouseOver_Object = this;

            if (LockTextureEditing)
                return false;

            if (IsTerrainHeightTexture() && IsOriginalShader)
            {
#if PEGI
                if (stroke.mouseDwn)
                    "Can't edit without Preview".showNotification();
#endif

                return false;
            }

            if ((stroke.mouseDwn) || (stroke.mouseUp))
                InitIfNotInited();

            if (ImgData == null)
            {
#if PEGI
                if (stroke.mouseDwn)
                    "No texture to edit".showNotification();
#endif

                return false;
            }

            return true;

        }

        public bool CastRayPlaytime(StrokeVector st, Vector3 mousePos)
        {

            if (NeedsGrid)//globalBrush.type(this).needsGrid)
            {
                ProcessGridDrag();
                return true;
            }
            else
            {
                RaycastHit hit;
                if (Physics.Raycast(Camera.main.ScreenPointToRay(mousePos), out hit, float.MaxValue))
                    return ProcessHit(hit, st);

                return false;
            }
        }

        void ProcessGridDrag()
        {
            stroke.posTo = GridNavigator.onGridPos;
            PreviewShader_StrokePosition_Update();
        }

        bool ProcessHit(RaycastHit hit, StrokeVector st)
        {

            int submesh = this.GetMesh().GetSubmeshNumber(hit.triangleIndex);
            if (submesh != selectedSubmesh)
            {
                if (autoSelectMaterial_byNumberOfPointedSubmesh)
                {
                    SetOriginalShaderOnThis();

                    selectedSubmesh = submesh;
                    OnChangedTexture_OnMaterial();

                    CheckPreviewShader();
                }
            }

            if (ImgData == null) return false;

            st.posTo = hit.point;

            st.unRepeatedUV = OffsetAndTileUV(hit);
            st.uvTo = st.unRepeatedUV.To01Space();


            PreviewShader_StrokePosition_Update();

            return true;
        }

        public Vector2 OffsetAndTileUV(RaycastHit hit)
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

        void ProcessMouseGrag(bool control)
        {

            if (stroke.mouseDwn)
            {
                stroke.firstStroke = true;
                stroke.SetPreviousValues();
            }

            if (control)
            {
                if ((stroke.mouseDwn) && control)
                {
                    SampleTexture(stroke.uvTo);
                    currently_Painted_Object = null;
                }
            }
            else
            {

                var id = ImgData;
                if (id != null)
                {

                    if (stroke.mouseDwn)
                        id.Backup();

                    if (IsTerrainHeightTexture() && stroke.mouseUp)
                        Preview_To_UnityTerrain();
                }
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
                texture2DDataWasChanged = true;
#endif
        }

        bool CanPaintOnMouseDown(BrushConfig br)
        {
            return ((ImgData.TargetIsTexture2D()) || (GlobalBrushType.StartPaintingTheMomentMouseIsDown));
        }

        #endregion

        #region previewMGMT

        public static Material previewHolderMaterial;
        public static Shader previewHolderOriginalShader;

        public bool IsOriginalShader { get { return (previewHolderMaterial == null || previewHolderMaterial != Material); } }

        public void CheckPreviewShader()
        {
            if (MatDta == null)
                return;
            if ((!IsCurrentTool()) || (LockTextureEditing && !IsEditingThisMesh))
                SetOriginalShaderOnThis();
            else if ((MatDta.usePreviewShader) && (IsOriginalShader))
                SetPreviewShader();
        }

        public void SetPreviewShader()
        {
            var mat = Material;

            if (previewHolderMaterial != null)
            {
                if (previewHolderMaterial != mat)
                    SetOriginalShader();
                else
                    return;
            }

            if ((meshEditing) && (MeshMGMT.target != this))
                return;

            Texture tex = ImgData.CurrentTexture();//materialTexture;

            if ((tex == null) && (!meshEditing))
            {
                MatDta.usePreviewShader = false;
                return;
            }

            if (mat == null)
            {
                InstantiateMaterial(false);
                return;
            }

            Shader shd = null;

            if (meshEditing)
                shd = Cfg.mesh_Preview;
            else
            {
                if (terrain != null) shd = Cfg.TerrainPreview;
                else
                {

                    foreach (var pl in TexMGMT.Plugins)
                    {
                        var ps = pl.GetPreviewShader(this);
                        if (ps != null) { shd = ps; break; }
                    }

                    if (shd == null)
                        shd = Cfg.br_Preview;
                }
            }

            if (shd == null)
                Debug.Log("Preview shader not found");
            else
            {
                previewHolderOriginalShader = mat.shader;
                previewHolderMaterial = mat;

                mat.shader = shd;

                if ((tex != null) && (meshEditing == false))
                    SetTextureOnPreview(tex);

                MatDta.usePreviewShader = true;

                Update_Brush_Parameters_For_Preview_Shader();
            }
        }

        public void SetOriginalShaderOnThis()
        {
            if (previewHolderMaterial != null && previewHolderMaterial == Material)
                SetOriginalShader();
        }

        public static void SetOriginalShader()
        {
            if (previewHolderMaterial != null)
            {
                previewHolderMaterial.shader = previewHolderOriginalShader;
                previewHolderOriginalShader = null;
                previewHolderMaterial = null;
            }
        }

        #endregion

        #region  Texture MGMT 

        public void UpdateTylingFromMaterial()
        {

            var id = ImgData;

            string fieldName = GetMaterialTexturePropertyName;
            Material mat = GetMaterial(false);
            if (!IsOriginalShader && (terrain == null))
            {
                id.tiling = mat.GetTextureScale(PainterDataAndConfig.previewTexture);
                id.offset = mat.GetTextureOffset(PainterDataAndConfig.previewTexture);
                return;
            }

            foreach (PainterPluginBase nt in plugins)
                if (nt.UpdateTylingFromMaterial(fieldName, this))
                    return;

            if ((mat == null) || (fieldName == null) || (id == null)) return;
            id.tiling = mat.GetTextureScale(fieldName);
            id.offset = mat.GetTextureOffset(fieldName);
        }

        public void UpdateTylingToMaterial()
        {
            var id = ImgData;
            string fieldName = GetMaterialTexturePropertyName;
            Material mat = GetMaterial(false);
            if (!IsOriginalShader && (terrain == null))
            {
                mat.SetTextureScale(PainterDataAndConfig.previewTexture, id.tiling);
                mat.SetTextureOffset(PainterDataAndConfig.previewTexture, id.offset);
                return;
            }

            foreach (PainterPluginBase nt in plugins)
                if (nt.UpdateTylingToMaterial(fieldName, this))
                    return;

            if ((mat == null) || (fieldName == null) || (id == null)) return;
            mat.SetTextureScale(fieldName, id.tiling);
            mat.SetTextureOffset(fieldName, id.offset);
        }

        public void OnChangedTexture_OnMaterial()
        {
            if ((IsOriginalShader) || (terrain == null))
                ChangeTexture(GetTextureOnMaterial());
        }

        public void ChangeTexture(ImageData id)
        {
            ChangeTexture(id.CurrentTexture());
        }

        public void ChangeTexture(Texture texture)
        {

            textureWasChanged = false;

#if UNITY_EDITOR
            if ((texture != null) && (texture.GetType() == typeof(Texture2D)))
            {
                var t2d = (Texture2D)texture;
                var imp = t2d.GetTextureImporter();
                if (imp != null)
                {

                    var name = AssetDatabase.GetAssetPath(texture);
                    var extension = name.Substring(name.LastIndexOf(".") + 1);

                    if (extension != "png")
                    {
#if PEGI
                        "Converting {0} to .png".F(name).showNotification();
#endif
                        texture = t2d.CreatePngSameDirectory(texture.name);
                    }

                }
            }
#endif

            string field = GetMaterialTexturePropertyName;

            if (texture == null)
            {
                RemoveTextureFromMaterial(); //SetTextureOnMaterial((Texture)null);
                return;
            }

            var id = texture.GetImgDataIfExists();

            if (id == null)
            {
                id = new ImageData().Init(texture);
                id.useTexcoord2 = GetMaterial(false).DisplayNameContains(field, PainterDataAndConfig.isUV2DisaplyNameTag);
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

            InitIfNotInited();

            var id = ImgData;

            if (id == null)
                return;

            if (id.destination == dst)
                return;

            id.ChangeDestination(dst, GetMaterial(true).GetMaterialData(), GetMaterialTexturePropertyName, this);
            CheckPreviewShader();

        }

        public void ReanableRenderTexture()
        {
            if (!LockTextureEditing)
            {

                OnEnable();

                OnChangedTexture_OnMaterial();

                if (ImgData != null)
                    UpdateOrSetTexTarget(TexTarget.RenderTexture); // set it to Render Texture
            }
        }

        public void CreateTerrainHeightTexture(string NewName)
        {

            string field = GetMaterialTexturePropertyName;

            if (field != PainterDataAndConfig.terrainHeight)
            {
                Debug.Log("Terrain height is not currently selected.");
                return;
            }

            int size = terrain.terrainData.heightmapResolution - 1;

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);

            var id = ImgData;

            if (id != null)
                id.From(texture);
            else
                ChangeTexture(texture);

            id = ImgData;

            id.SaveName = NewName;
            texture.name = id.SaveName;
            texture.Apply(true, false);

            SetTextureOnMaterial(texture);

            Unity_To_Preview();
            id.SetAndApply(false);

            texture.wrapMode = TextureWrapMode.Repeat;

#if UNITY_EDITOR
            SaveTextureAsAsset(false);

            TextureImporter importer = id.texture2D.GetTextureImporter();
            bool needReimport = importer.WasNotReadable();
            needReimport |= importer.WasWrongIsColor(false);
            if (needReimport) importer.SaveAndReimport();
#endif

            SetTextureOnMaterial(id.texture2D);
            UpdateShaderGlobals();
        }

        public void CreateTexture2D(int size, string TextureName, bool isColor)
        {

            var id = ImgData;

            bool gotRenderTextureData = ((id != null) && ((size == id.width) && (size == id.width)) && (id.TargetIsRenderTexture()));

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true, !isColor);

            if (gotRenderTextureData && ((id.texture2D == null) || (TextureName == id.SaveName)))
                id.texture2D = texture;

            texture.wrapMode = TextureWrapMode.Repeat;

            ChangeTexture(texture);

            id = ImgData;

            id.SaveName = TextureName;
            texture.name = TextureName;

            bool needFullUpdate = false;

            if (gotRenderTextureData)
                id.RenderTexture_To_Texture2D();
            else
            {
                if (!isColor)
                {
                    if (Cfg.newTextureClearNonColorValue != Color.white)
                        id.Colorize(Cfg.newTextureClearNonColorValue);
                    needFullUpdate = true;
                }
                else
                {
                    if (Cfg.newTextureClearColor != Color.white)
                        id.Colorize(Cfg.newTextureClearColor);
                    needFullUpdate = true;
                }
            }

            if (needFullUpdate)
                id.SetAndApply(true);
            else
                texture.Apply(true, false);

#if UNITY_EDITOR
            SaveTextureAsAsset(true);

            TextureImporter importer = id.texture2D.GetTextureImporter();

            bool needReimport = importer.WasNotReadable();
            needReimport |= importer.WasWrongIsColor(isColor);

            if (needReimport) importer.SaveAndReimport();


#endif

        }

        public void CreateRenderTexture(int size, string name)
        {
            ImageData previous = ImgData;

            var nt = new ImageData().Init(size);

            nt.SaveName = name;

            ChangeTexture(nt.renderTexture);

            if (nt == null)
                Debug.Log("Change texture destroyed curigdata");

            PainterCamera.Inst.Render(previous.CurrentTexture(), nt);

            UpdateOrSetTexTarget(TexTarget.RenderTexture);

        }

        #endregion

        #region Material MGMT

        public Material[] GetMaterials()
        {

            if (terrain != null)
            {
                Material mat = GetMaterial(false);

                if (mat != null)
                    return new Material[] { mat };
                //ms.Add(mat.name);

                return null;
            }

            return meshRenderer.sharedMaterials;

        }

        public List<string> GetMaterialsNames()
        {

            List<string> ms = new List<string>();

            Material[] mats = GetMaterials();

            for (int i = 0; i < mats.Length; i++)
            {
                Material mt = mats[i];
                if (mt != null)
                    ms.Add(mt.name);
                else
                    ms.Add("Null material {0}".F(i));
            }
            return ms;
        }

        public List<String> GetMaterialTextureNames()
        {
#if UNITY_EDITOR

            if (MatDta == null)
                return new List<string>();

            if (!IsOriginalShader)
                return MatDta.materials_TextureFields;

            MatDta.materials_TextureFields.Clear();

            foreach (PainterPluginBase nt in plugins)
                nt.GetNonMaterialTextureNames(this, ref MatDta.materials_TextureFields);

            if (terrain == null)
            {
                MatDta.materials_TextureFields = GetMaterial(false).MyGetTextureProperties();

            }
            else
            {
                List<string> tmp = GetMaterial(false).MyGetTextureProperties();

                foreach (string t in tmp)
                {
                    if ((!t.Contains("_Splat")) && (!t.Contains("_Normal")))
                        MatDta.materials_TextureFields.Add(t);
                }
            }
#endif
            return MatDta.materials_TextureFields;
        }

        public string GetMaterialTexturePropertyName
        {
            get
            {
                List<string> list = GetMaterialTextureNames();
                return SelectedTexture < list.Count ? list[SelectedTexture] : null;
            }
        }

        public Texture GetTextureOnMaterial()
        {

            if (!IsOriginalShader)
            {
                if (meshEditing) return null;
                if (terrain == null) {
                    Material mat = GetMaterial(false);
                    return mat?.GetTexture(PainterDataAndConfig.previewTexture);
                }
            }

            string fieldName = GetMaterialTexturePropertyName;

            if (fieldName == null)
                return null;

            foreach (PainterPluginBase t in plugins)
            {
                Texture tex = null;
                if (t.GetTexture(fieldName, ref tex, this))
                    return tex;
            }

            return GetMaterial(false).GetTexture(fieldName);
        }

        public Material GetMaterial(bool original)
        {

            Material result = null;

            if (original)
                SetOriginalShader();

            if (meshRenderer == null)
            {
                if (terrain != null)
                    result = terrain.materialTemplate;
                else
                    result = null;
            }
            else
            {
                int cnt = meshRenderer.sharedMaterials.Length;
                if (cnt != 0)
                {
                    selectedSubmesh = selectedSubmesh.ClampZeroTo(cnt);
                    result = meshRenderer.sharedMaterials[selectedSubmesh];
                }
            }

            return result;
        }

        public void RemoveTextureFromMaterial()
        {
            SetTextureOnMaterial(GetMaterialTexturePropertyName, null);
        }

        public void SetTextureOnMaterial(ImageData id)
        {
            SetTextureOnMaterial(GetMaterialTexturePropertyName, id.CurrentTexture());
        }

        public void SetTextureOnMaterial(Texture tex)
        {
            SetTextureOnMaterial(GetMaterialTexturePropertyName, tex);
        }

        public void SetTextureOnMaterial(string fieldName, Texture tex)
        {
            SetTextureOnMaterial(fieldName, tex, GetMaterial(true));
            CheckPreviewShader();
        }

        public void SetTextureOnMaterial(string fieldName, Texture tex, Material mat)
        {

            var id = tex.GetImgData();

            if (fieldName != null)
            {
                if (id != null)
                    Cfg.recentTextures.AddIfNew(fieldName, id);

                foreach (PainterPluginBase nt in plugins)
                    if (nt.SetTextureOnMaterial(fieldName, id, this))
                        return;
            }

            if (mat != null)
            {
                if (fieldName != null)
                    mat.SetTexture(fieldName, id.CurrentTexture());

                if (!IsOriginalShader && (terrain == null))
                    SetTextureOnPreview(id.CurrentTexture());
            }
        }

        void SetTextureOnPreview(Texture tex)
        {
            Material mat = GetMaterial(false);
            if (!meshEditing)
            {

                var id = tex.GetImgData();

                mat.SetTexture(PainterDataAndConfig.previewTexture, id.CurrentTexture());
                if (id != null)
                {
                    mat.SetTextureOffset(PainterDataAndConfig.previewTexture, id.offset);
                    mat.SetTextureScale(PainterDataAndConfig.previewTexture, id.tiling);
                }
            }
        }

        public Material InstantiateMaterial(bool saveIt)
        {

            SetOriginalShader();

            if ((ImgData != null) && (GetMaterial(false) != null))
                UpdateOrSetTexTarget(TexTarget.Texture2D);

            if (TexMGMT.defaultMaterial == null) InitIfNotInited();

            Material mat = GetMaterial(true);

            string Mname = gameObject.name;

            if ((mat == null) && (terrain != null))
            {

                mat = new Material(Cfg.TerrainPreview);

                terrain.materialTemplate = mat;
                terrain.materialType = Terrain.MaterialType.Custom;
                Mname += "_Terrain material";
            }
            else
            {
                if (mat)
                    Mname = mat.name;

                Material = Instantiate((mat ?? TexMGMT.defaultMaterial));
                CheckPreviewShader();
            }

            Material.name = Mname;

            if (saveIt)
            {
#if UNITY_EDITOR
                string fullPath = Path.Combine(Application.dataPath, Cfg.materialsFolderName);
                Directory.CreateDirectory(fullPath);

                var material = GetMaterial(false);
                string path = material.SetUniqueObjectName(Cfg.materialsFolderName, ".mat");

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

            if ((id != null) && (GetMaterial(false) != null))
                UpdateOrSetTexTarget(id.destination);
#if PEGI
            "Instantiating Material on {0}".F(gameObject.name).showNotification();
#endif
            return GetMaterial(false);


        }

        #endregion

        #region Terrain_MGMT

        float tilingY = 8;

        public void UpdateShaderGlobals()
        {

            foreach (PainterPluginBase nt in plugins)
                nt.OnUpdate(this);

            if (terrain == null) return;

#if UNITY_2018_3_OR_NEWER
            var sp = terrain.terrainData.terrainLayers;
#else
            SplatPrototype[] sp = terrain.terrainData.splatPrototypes;
#endif

            if (sp.Length != 0)
            {
                float tilingX = terrain.terrainData.size.x / sp[0].tileSize.x;
                float tilingZ = terrain.terrainData.size.z / sp[0].tileSize.y;
                Shader.SetGlobalVector(PainterDataAndConfig.terrainTiling, new Vector4(tilingX, tilingZ, sp[0].tileOffset.x, sp[0].tileOffset.y));

                tilingY = terrain.terrainData.size.y / sp[0].tileSize.x;
            }
            Shader.SetGlobalVector(PainterDataAndConfig.terrainScale, new Vector4(terrain.terrainData.size.x, terrain.terrainData.size.y, terrain.terrainData.size.z, 0.5f / ((float)terrain.terrainData.heightmapResolution)));

            UpdateTerrainPosition();

            Texture[] alphamaps = terrain.terrainData.alphamapTextures;
            if (alphamaps.Length > 0)
                Shader.SetGlobalTexture(PainterDataAndConfig.terrainControl, alphamaps[0].GetDestinationTexture());

        }

        public void UpdateTerrainPosition()
        {
            Vector3 pos = transform.position;
            Shader.SetGlobalVector(PainterDataAndConfig.terrainPosition, new Vector4(pos.x, pos.y, pos.z, tilingY));
        }

        public void Preview_To_UnityTerrain()
        {

            var id = ImgData;

            if (id == null)
                return;

            bool rendTex = (id.TargetIsRenderTexture());
            if (rendTex) UpdateOrSetTexTarget(TexTarget.Texture2D);

            TerrainData td = terrain.terrainData;

            int res = td.heightmapResolution - 1;

            float conversion = ((float)id.width / (float)res);

            float[,] heights = td.GetHeights(0, 0, res + 1, res + 1);

            Color[] cols = id.Pixels;

            if (conversion != 1)
            {

                for (int y = 0; y < res; y++)
                {
                    int yind = id.width * Mathf.FloorToInt((y * conversion));
                    for (int x = 0; x < res; x++)
                    {



                        heights[y, x] = cols[yind + (int)(x * conversion)].a;
                    }
                }
            }
            else
            {

                for (int y = 0; y < res; y++)
                {
                    int yind = id.width * y;

                    for (int x = 0; x < res; x++)
                        heights[y, x] = cols[yind + x].a;

                }


            }

            for (int y = 0; y < res - 1; y++)
                heights[y, res] = heights[y, res - 1];
            for (int x = 0; x < res; x++)
                heights[res, x] = heights[res - 1, x];

            terrain.terrainData.SetHeights(0, 0, heights);

            UpdateShaderGlobals();

            if (rendTex) UpdateOrSetTexTarget(TexTarget.RenderTexture);
        }

        public void Unity_To_Preview()
        {

            var oid = ImgData;

            ImageData id = terrainHeightTexture.GetImgData();

            bool current = (id == oid);
            bool rendTex = current && (oid.TargetIsRenderTexture());
            if (rendTex) UpdateOrSetTexTarget(TexTarget.Texture2D);


            int textureSize = terrain.terrainData.heightmapResolution - 1;

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
            Color[] col = id.Pixels;

            float height = 1f / ((float)terrain.terrainData.size.y);

            for (int y = 0; y < textureSize; y++)
            {
                int fromY = y * textureSize;

                for (int x = 0; x < textureSize; x++)
                {
                    Color tmpcol = new Color();

                    float dx = ((float)(x)) / textureSize;
                    float dy = ((float)y) / textureSize;

                    Vector3 v3 = terrain.terrainData.GetInterpolatedNormal(dx, dy);// + Vector3.one * 0.5f;

                    tmpcol.r = v3.x + 0.5f;
                    tmpcol.g = v3.y + 0.5f;
                    tmpcol.b = v3.z + 0.5f;
                    tmpcol.a = terrain.terrainData.GetHeight(x, y) * height;

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

        public bool IsTerrainHeightTexture()
        {

            if (terrain == null)
                return false;
            string name = GetMaterialTexturePropertyName;
            if (name == null)
                return false;
            return name.Contains(PainterDataAndConfig.terrainHeight);
        }

        public TerrainHeight GetTerrainHeight()
        {

            foreach (PainterPluginBase nt in plugins)
            {
                if (nt.GetType() == typeof(TerrainHeight))
                    return ((TerrainHeight)nt);
            }

            return null;

        }

        public bool IsTerrainControlTexture()
        {
            return ((ImgData != null) && (terrain != null) && (GetMaterialTexturePropertyName.Contains(PainterDataAndConfig.terrainControl)));
        }

        #endregion

        #region Playback & Recoding

        public static List<PlaytimePainter> playbackPainters = new List<PlaytimePainter>();

        public List<string> playbackVectors = new List<string>();

        public static StdDecoder cody = new StdDecoder("");

        public void PlayByFilename(string recordingName)
        {
            if (!playbackPainters.Contains(this))
                playbackPainters.Add(this);
            StrokeVector.PausePlayback = false;
            playbackVectors.AddRange(Cfg.StrokeRecordingsFromFile(recordingName));
        }

        public void PlaybeckVectors()
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

        Vector2 prevDir;
        Vector2 lastUV;
        Vector3 prevPOSDir;
        Vector3 lastPOS;

        float strokeDistance;

        public void RecordingMGMT()
        {
            var curImgData = ImgData;

            if (curImgData.recording)
            {

                if (stroke.mouseDwn)
                {
                    prevDir = Vector2.zero;
                    prevPOSDir = Vector3.zero;
                }

                bool canRecord = stroke.mouseDwn || stroke.mouseUp;

                bool worldSpace = GlobalBrush.IsA3Dbrush(this);

                if (!canRecord)
                {

                    float size = GlobalBrush.Size(worldSpace);

                    if (worldSpace)
                    {
                        Vector3 dir = stroke.posTo - lastPOS;

                        float dot = Vector3.Dot(dir.normalized, prevPOSDir);

                        canRecord |= (strokeDistance > size * 10) ||
                            ((dir.magnitude > size * 0.01f) && (strokeDistance > size) && (dot < 0.9f));

                        float fullDist = strokeDistance + dir.magnitude;

                        prevPOSDir = (prevPOSDir * strokeDistance + dir).normalized;

                        strokeDistance = fullDist;

                    }
                    else
                    {

                        size /= ((float)curImgData.width);

                        Vector2 dir = stroke.uvTo - lastUV;

                        float dot = Vector2.Dot(dir.normalized, prevDir);

                        canRecord |= (strokeDistance > size * 5) || (strokeDistance * (float)curImgData.width > 10) ||
                            ((dir.magnitude > size * 0.01f) && (dot < 0.8f));


                        float fullDist = strokeDistance + dir.magnitude;

                        prevDir = (prevDir * strokeDistance + dir).normalized;

                        strokeDistance = fullDist;

                    }
                }

                if (canRecord)
                {

                    Vector2 hold = stroke.uvTo;
                    Vector3 holdv3 = stroke.posTo;

                    if (!stroke.mouseDwn)
                    {
                        stroke.uvTo = lastUV;
                        stroke.posTo = lastPOS;
                    }

                    strokeDistance = 0;

                    string data = Encode().ToString();
                    curImgData.recordedStrokes.Add(data);
                    curImgData.recordedStrokes_forUndoRedo.Add(data);

                    if (!stroke.mouseDwn)
                    {
                        stroke.uvTo = hold;
                        stroke.posTo = holdv3;
                    }

                }

                lastUV = stroke.uvTo;
                lastPOS = stroke.posTo;

            }
        }

        public StdEncoder Encode()
        {
            StdEncoder cody = new StdEncoder();

            var id = ImgData;

            if (stroke.mouseDwn)
            {
                cody.Add("brush", GlobalBrush.EncodeStrokeFor(this)) // Brush is unlikely to change mid stroke
                .Add_String("trg", id.TargetIsTexture2D() ? "C" : "G");
            }

            cody.Add("s", stroke.Encode(id.TargetIsRenderTexture() && GlobalBrush.IsA3Dbrush(this)));

            return cody;
        }

        public bool Decode(string tag, string data)
        {

            switch (tag)
            {
                case "trg": UpdateOrSetTexTarget(data.Equals("C") ? TexTarget.Texture2D : TexTarget.RenderTexture); break;
                case "brush":
                    //InitIfNotInited();
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

        public ISTD Decode(string data) => data.DecodeTagsFor(this);

        #endregion

        #region Saving

#if UNITY_EDITOR

        public void ForceReimportMyTexture(string path)
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
                if (id.texture2D != null)
                id.PixelsFromTexture2D(id.texture2D);

            SetTextureOnMaterial(id);
        }

        public bool TextureExistsAtDestinationPath()
        {
            TextureImporter importer = AssetImporter.GetAtPath("Assets{0}".F(GenerateTextureSavePath())) as TextureImporter;
            return importer != null;
        }

        public string GenerateTextureSavePath() =>
             "/{0}{1}.png".F(Cfg.texturesFolderName.AddPostSlashIfNotEmpty(), ImgData.SaveName);

        public string GenerateMeshSavePath()
        {
            if (meshFilter.sharedMesh == null)
                return "None";

            return ("/{0}/{1}.asset".F(Cfg.meshesFolderName, meshNameHolder));
        }

        void OnBeforeSaveTexture()
        {
            var id = ImgData;

            if (id.TargetIsRenderTexture())
            {
                id.RenderTexture_To_Texture2D();
            }
        }

        void OnPostSaveTexture(ImageData id)
        {
            SetTextureOnMaterial(id);
            UpdateOrSetTexTarget(id.destination);
            UpdateShaderGlobals();
        }

        public void RewriteOriginalTexture_Rename(string name)
        {

            OnBeforeSaveTexture();
            var id = ImgData;
            id.texture2D = id.texture2D.RewriteOriginalTexture_NewName(name);

            OnPostSaveTexture(id);
        }

        public void RewriteOriginalTexture()
        {

            OnBeforeSaveTexture();
            var id = ImgData;
            id.texture2D = id.texture2D.RewriteOriginalTexture();

            OnPostSaveTexture(id);
        }

        public void SaveTextureAsAsset(bool asNew)
        {

            OnBeforeSaveTexture();

            var id = ImgData;
            id.texture2D = id.texture2D.SaveTextureAsAsset(Cfg.texturesFolderName, ref id.SaveName, asNew);

            id.texture2D.Reimport_IfNotReadale();

            OnPostSaveTexture(id);

        }

        public void SaveMesh()
        {

            Mesh m = this.GetMesh();
            string path = AssetDatabase.GetAssetPath(m);



            string lastPart = "/{0}/".F(Cfg.meshesFolderName);
            string folderPath = Application.dataPath + lastPart;
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
                var i = ImgData; return i == null ? true :

                    i.lockEditing || i.other != null;


            }
            set { var i = ImgData; if (i != null) i.lockEditing = value; }
        }
        public bool forcedMeshCollider;
        [NonSerialized]
        public bool inited = false;
        public bool autoSelectMaterial_byNumberOfPointedSubmesh = true;

        public const string WWW_Manual = "https://docs.google.com/document/d/170k_CE-rowVW9nsAo3EUqVIAWlZNahC0ua11t1ibMBo/edit?usp=sharing";

#if UNITY_EDITOR

        [MenuItem("Tools/" + PainterDataAndConfig.ToolName + "/Attach Painter To Selected")]
        static void GivePainterToSelected()
        {
            foreach (GameObject go in Selection.gameObjects)
                IterateAssignToChildren(go.transform);
        }

        static void IterateAssignToChildren(Transform tf)
        {

            if ((tf.GetComponent<PlaytimePainter>() == null)
                && (tf.GetComponent<Renderer>() != null)

                && (tf.GetComponent<RenderBrush>() == null) && (PlaytimeToolComponent.PainterCanEditWithTag(tf.tag)))
                tf.gameObject.AddComponent<PlaytimePainter>();

            for (int i = 0; i < tf.childCount; i++)
                IterateAssignToChildren(tf.GetChild(i));

        }

        [MenuItem("Tools/" + PainterDataAndConfig.ToolName + "/Remove Painters From the Scene")]
        static void TakePainterFromAll()
        {
            Renderer[] allObjects = UnityEngine.Object.FindObjectsOfType<Renderer>();
            foreach (Renderer mr in allObjects)
            {
                PlaytimePainter ip = mr.GetComponent<PlaytimePainter>();
                if (ip != null)
                    DestroyImmediate(ip);
            }

            var rtp = FindObjectsOfType<PainterCamera>();
            if (rtp != null)
                foreach (var rt in rtp)
                    rt.gameObject.DestroyWhatever();

            PainterStuff.applicationIsQuitting = false;
        }

        [MenuItem("Tools/" + PainterDataAndConfig.ToolName + "/Instantiate Painter Camera")]
        static void InstantiatePainterCamera()
        {
            PainterStuff.applicationIsQuitting = false;
            PainterCamera r = PainterCamera.Inst;
        }

        [MenuItem("Tools/" + PainterDataAndConfig.ToolName + "/Join Discord")]
        public static void Open_Discord()
        {
            Application.OpenURL("https://discord.gg/rF7yXq3");
        }

        [MenuItem("Tools/" + PainterDataAndConfig.ToolName + "/Open Manual")]
        public static void OpenWWW_Documentation()
        {
            Application.OpenURL(WWW_Manual);
        }

        public static void OpenWWW_Forum()
        {
            Application.OpenURL("https://www.quizcanners.com/forum/texture-editor");
        }
        [MenuItem("Tools/" + PainterDataAndConfig.ToolName + "/Send an Email")]

        public static void Open_Email()
        {
            Application.OpenURL("mailto:quizcanners@gmail.com");
        }

#endif

        public override void OnDestroy()
        {




            base.OnDestroy();

            Collider[] collis = GetComponents<Collider>();

            foreach (Collider c in collis)
                if (c.GetType() != typeof(MeshCollider)) c.enabled = true;

            collis = GetComponentsInChildren<Collider>();

            foreach (Collider c in collis)
                if (c.GetType() != typeof(MeshCollider)) c.enabled = true;

            if (forcedMeshCollider && (meshCollider != null))
                meshCollider.enabled = false;
        }

        void OnDisable()
        {

            SetOriginalShader();

            var id = GetTextureOnMaterial().GetImgDataIfExists();

            inited = false; // Should be before restoring to texture2D to avoid Clear to black.

            if ((id != null) && (id.CurrentTexture().IsBigRenderTexturePair()))
                UpdateOrSetTexTarget(TexTarget.Texture2D);

            if ((TexMGMT) && (MeshManager.Inst.target == this))
            {
                MeshManager.Inst.DisconnectMesh();
                MeshManager.Inst.previouslyEdited = this;
            }
        }

        public override void OnEnable()
        {

            PainterStuff.applicationIsQuitting = false;

            base.OnEnable();
            if (plugins == null)
                plugins = new List<PainterPluginBase>();


            PainterPluginBase.UpdateList(this);

            if (terrain != null)
                UpdateShaderGlobals();

            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();

#if BUILD_WITH_PAINTER
            //materials_TextureFields = getMaterialTextureNames();
#endif

        }

        public void UpdateColliderForSkinnedMesh()
        {

            if (colliderForSkinnedMesh == null) colliderForSkinnedMesh = new Mesh();
            skinnedMeshRendy.BakeMesh(colliderForSkinnedMesh);
            if (meshCollider != null)
                meshCollider.sharedMesh = colliderForSkinnedMesh;

        }

        public void InitIfNotInited()
        {
            //  return;

            if ((!inited) ||
                (((meshCollider == null) || (meshRenderer == null))
                && ((terrain == null) || (terrainCollider == null))))
            {
                inited = true;

                nameHolder = gameObject.name;

                if (meshRenderer == null)
                    meshRenderer = GetComponent<Renderer>();

                if (meshRenderer != null)
                {

                    Collider[] collis = GetComponents<Collider>();

                    foreach (Collider c in collis)
                        if (c.GetType() != typeof(MeshCollider)) c.enabled = false;

                    collis = GetComponentsInChildren<Collider>();

                    foreach (Collider c in collis)
                        if (c.GetType() != typeof(MeshCollider)) c.enabled = false;

                    meshCollider = GetComponent<MeshCollider>();
                    meshFilter = GetComponent<MeshFilter>();

                    if (meshCollider == null)
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

                if ((meshRenderer != null) && (meshRenderer.GetType() == typeof(SkinnedMeshRenderer)))
                {
                    skinnedMeshRendy = (SkinnedMeshRenderer)meshRenderer;
                    UpdateColliderForSkinnedMesh();
                }
                else skinnedMeshRendy = null;

                if (meshRenderer == null)
                {
                    terrain = GetComponent<Terrain>();
                    if (terrain != null)
                        terrainCollider = GetComponent<TerrainCollider>();

                }

                if ((this == TexMGMT.autodisabledBufferTarget) && (!LockTextureEditing) && (!this.ApplicationIsAboutToEnterPlayMode()))
                    ReanableRenderTexture();

            }
        }

        public void FocusOnThisObject()
        {

#if UNITY_EDITOR
            UnityHelperFunctions.FocusOn(this.gameObject);
#endif
            selectedInPlaytime = this;
            Update_Brush_Parameters_For_Preview_Shader();
            InitIfNotInited();
        }

        #endregion

        #region UPDATES  

        public bool textureWasChanged = false;

        float repaintTimer;

#if UNITY_EDITOR
        public void FeedEvents(Event e)
        {


            var id = ImgData;

            if ((e.type == EventType.KeyDown) && !meshEditing && id != null)
            {

                if ((e.keyCode == KeyCode.Z) && (id.cache.undo.gotData()))
                    id.cache.undo.ApplyTo(id);
                else if ((e.keyCode == KeyCode.X) && (id.cache.redo.gotData()))
                    id.cache.redo.ApplyTo(id);
            }
        }
#endif

#if UNITY_EDITOR || BUILD_WITH_PAINTER
        bool texture2DDataWasChanged;
        public void Update()
        {

            if (IsEditingThisMesh && (Application.isPlaying))
                MeshManager.Inst.DRAW_Lines(false);

            if (textureWasChanged)
                OnChangedTexture_OnMaterial();

            repaintTimer -= (Application.isPlaying) ? Time.deltaTime : 0.016f;

            if (texture2DDataWasChanged && ((repaintTimer < 0) || (stroke.mouseUp)))
            {
                // Debug.Log("repainting delay");
                texture2DDataWasChanged = false;
                var id = ImgData;

                if ((id != null) && (id.texture2D != null))
                    id.SetAndApply(!GlobalBrush.DontRedoMipmaps);
                repaintTimer = GlobalBrush.repaintDelay;
            }

        }
#endif

        public void PreviewShader_StrokePosition_Update()
        {
            CheckPreviewShader();
            if (!IsOriginalShader)
            {
                bool hide = Application.isPlaying ? Input.GetMouseButton(0) : currently_Painted_Object == this;
                PainterCamera.Shader_PerFrame_Update(stroke, hide, GlobalBrush.Size(this));
            }
        }

        public void Update_Brush_Parameters_For_Preview_Shader()
        {
            var id = ImgData;

            if ((id != null) && (!IsOriginalShader))
            {
                TexMGMT.Shader_UpdateBrush(GlobalBrush, 1, id, this);

                foreach (var p in plugins)
                    p.Update_Brush_Parameters_For_Preview_Shader(this);

            }
        }

        #endregion

        #region Inspector 

        public override void OnGUI()
        {
#if !BUILD_WITH_PAINTER
            //Debug.Log("Not building with painter");
#endif

#if BUILD_WITH_PAINTER
            if (Cfg && Cfg.enablePainterUIonPlay)
                base.OnGUI();
#endif

        }

        public override string PlaytimeWindowName => "{0} {1}".F(gameObject.name, GetMaterialTexturePropertyName);

        public static PlaytimePainter inspectedPainter;

        #if PEGI
        public bool PEGI_MAIN()
        {

            TexMGMT.focusedPainter = this;

            if (!Cfg)
            {
                "No Painter Config Detected".nl();
                return false;
            }

            inspectedPainter = this;


            bool changed = false;

            #region Top Buttons

            if (!PainterStuff.IsNowPlaytimeAndDisabled)
            {

                if ((MeshManager.target != null) && (MeshManager.target != this))
                    MeshManager.DisconnectMesh();

                if (!Cfg.showConfig)
                {
                    if (meshEditing)
                    {
                        if (icon.Painter.Click("Edit Texture", 25))
                        {
                            SetOriginalShader();
                            meshEditing = false;
                            CheckPreviewShader();
                            MeshMGMT.DisconnectMesh();
                            changed = true;
                            Cfg.showConfig = false;
                            "Editing Texture".showNotification();
                        }
                    }
                    else
                    {
                        if (icon.Mesh.Click("Edit Mesh", 25))
                        {
                            meshEditing = true;

                            SetOriginalShader();
                            UpdateOrSetTexTarget(TexTarget.Texture2D);
                            Cfg.showConfig = false;
                            "Editing Mesh".showNotification();

                            if (SavedEditableMesh != null)
                                MeshMGMT.EditMesh(this, false);

                            changed = true;
                        }

        

                    }
                }

                pegi.toggle(ref Cfg.showConfig, meshEditing ? icon.Mesh : icon.Painter, icon.Config, "Settings", 25);
            }

            #endregion

            #region Config 

            if ((Cfg.showConfig) || (PainterStuff.IsNowPlaytimeAndDisabled))
            {

                pegi.newLine();

                Cfg.Nested_Inspect();

            }
            else
            {

                #endregion

                #region Mesh Editing

                if (meshEditing)
                {

                    MeshManager mg = MeshMGMT;
                    mg.Undo_redo_PEGI();

                    pegi.newLine();

                    if (meshFilter != null)
                    {

                        if (this != mg.target)
                        {
                            if (SavedEditableMesh != null)
                                "Got saved mesh data".nl();
                            else "No saved data found".nl();
                        }

                        pegi.writeOneTimeHint("Warning, this will change (or mess up) your model.", "MessUpMesh");

                        if (mg.target != this)
                        {

                            var ent = gameObject.GetComponent("pb_Entity");
                            var obj = gameObject.GetComponent("pb_Object");

                            if (ent || obj)
                                "PRO builder detected. Strip it using Actions in the Tools->ProBuilder menu.".writeHint();
                            else
                            {
                                if (Application.isPlaying)
                                    pegi.writeWarning("Playtime Changes will be reverted once you try to edit the mesh again.");
                                pegi.newLine();

                                if ("Edit Copy".Click())
                                    mg.EditMesh(this, true);
                                if ("New Mesh".Click())
                                {
                                    meshFilter.mesh = new Mesh();
                                    SavedEditableMesh = null;
                                    mg.EditMesh(this, false);
                                }

                                if (icon.Edit.Click("Edit Mesh", 25).nl())
                                    mg.EditMesh(this, false);
                            }
                        }

                    }
                    else if ("Add Mesh Filter".Click().nl())
                    {
                        meshFilter = gameObject.AddComponent<MeshFilter>();
                        if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();
                    }

                    if ((this != null) && (MeshMGMT.target == this))
                    {

                        if ("Profile".foldout())
                        {
                            if ((Cfg.meshPackagingSolutions.Count > 1) && (icon.Delete.Click(25)))
                                Cfg.meshPackagingSolutions.RemoveAt(selectedMeshProfile);
                            else
                            {
                                pegi.newLine();
                                if (MeshProfile.PEGI().nl())
                                    MeshMGMT.edMesh.Dirty = true;

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
                                PlaytimePainter.MeshMGMT.edMesh.Dirty = true;
                            if (icon.Add.Click(25).nl())
                            {
                                Cfg.meshPackagingSolutions.Add(new MeshPackagingProfile());
                                selectedMeshProfile = Cfg.meshPackagingSolutions.Count - 1;
                                MeshProfile.name = "New Profile {0}".F(selectedMeshProfile);
                            }
                            pegi.newLine();

                            MeshMGMT.PEGI().nl();
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

                        if (id.showRecording)
                            if (!id.recording)
                                this.Playback_PEGI();


                        pegi.newLine();

                        bool CPU = id.TargetIsTexture2D();

                        var mat = GetMaterial(false);
                        if (mat.IsProjected())
                        {
                            pegi.writeWarning("Projected UV Shader detected. Painting may not work properly");
                            if ("Undo".Click(40).nl())
                                mat.DisableKeyword(PainterDataAndConfig.UV_PROJECTED);
                            pegi.newLine();
                        }

                        if (!CPU && id.texture2D != null && id.width != id.height)
                            "Non-square texture detected! Every switch between GPU and CPU mode will result in loss of quality.".writeWarning();

                        #endregion

                        #region Brush

                        changed |= GlobalBrush.PEGI();

                        BlitMode mode = GlobalBrush.BlitMode;
                        Color col = GlobalBrush.colorLinear.ToGamma();

                        if ((CPU || (!mode.UsingSourceTexture)) && (IsTerrainHeightTexture() == false))
                        {
                            if (pegi.edit(ref col))
                            {
                                changed = true;
                                GlobalBrush.colorLinear.From(col);
                            }
                        }

                        pegi.nl();

                        if (!Cfg.moreOptions) {

                            changed |= GlobalBrush.ColorSliders_PEGI().nl();

                            if (Cfg.showColorSchemes) {

                                var scheme = Cfg.colorSchemes.TryGet(Cfg.selectedColorScheme);

                                if (scheme != null)
                                    scheme.PickerPEGI();

                                if (Cfg.showColorSchemes)
                                    changed |= "Scheme".select(40, ref Cfg.selectedColorScheme, Cfg.colorSchemes).nl();

                            }
                        }

                        #endregion

                        #region Fancy Options
                        "Fancy options".foldout(ref Cfg.moreOptions).nl();

                        if (Cfg.moreOptions)  {

                            if ("Show/Hide stuff".fold_enter_exit(ref id.inspectedStuff, 7).nl()) {

                                "Show Previous Textures (if any) ".toggleIcon("Will show textures previously used for this material property.", ref Cfg.showRecentTextures, true).nl();

                                var mats = GetMaterials();
                                if ((mats != null) && (mats.Length > 1))
                                    "Auto Select Material".toggleIcon("Material will be changed based on the submesh you are painting on",
                                                                   ref autoSelectMaterial_byNumberOfPointedSubmesh).nl();

                                "Exclusive Render Textures".toggleIcon("Allow creation of simple Render Textures - the have limited editing capabilities.", ref Cfg.allowExclusiveRenderTextures, true).nl();

                                "Color Sliders ".toggleIcon("Should the color slider be shown ", ref Cfg.showColorSliders, true).nl();

                                changed |= "Show Recording/Playback".toggleIcon(ref id.showRecording, true).nl();
                            }

                            if ("New Texture Config ".conditional_enter_exit(!IsTerrainHeightTexture(), ref id.inspectedStuff, 4).nl()) {

                                if (Cfg.newTextureIsColor)
                                    "Clear Color".edit(ref Cfg.newTextureClearColor).nl();
                                else
                                    "Clear Value".edit(ref Cfg.newTextureClearNonColorValue).nl();

                                "Color Texture".toggleIcon("Will the new texture be a Color Texture", ref Cfg.newTextureIsColor, true).nl();

                                "Size:".select("Size of the new Texture", 40, ref PainterCamera.Data.selectedSize, PainterDataAndConfig.NewTextureSizeOptions).nl();
                            }


                            changed |= id.PEGI();
                        }

                        bool showToggles = (id.inspectedStuff == -1 && Cfg.moreOptions);

                        changed |= id.ComponentDependent_PEGI(showToggles, this);
                        
                        if (Cfg.moreOptions)
                            pegi.Line(Color.red);



                        if ((id.enableUndoRedo) && (id.backupManually) && ("Backup for UNDO".Click()))
                            id.Backup();

                        if ((GlobalBrush.DontRedoMipmaps) && ("Redo Mipmaps".Click().nl()))
                            id.SetAndApply(true);
                        #endregion
                    } else 
                        if (!IsOriginalShader)
                        this.PreviewShaderToggle_PEGI();

                    id = ImgData;

                    if (!PainterStuff.IsNowPlaytimeAndDisabled)
                    {
                        #region Save Load Options

                        if ((meshRenderer != null || terrain != null) && !Cfg.showConfig)
                        {
                            #region Material Clonning Options

                            pegi.nl();

                            var mats = GetMaterials();
                            if ((mats != null) && (mats.Length > 0))
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

                            if (pegi.edit(ref mater))
                                Material = mater;

                            if (icon.NewMaterial.Click("Instantiate Material", 25).nl())
                            {
                                changed = true;
                                InstantiateMaterial(true);
                            }


                            pegi.nl();
                            pegi.Space();
                            pegi.nl();
                            #endregion

                            #region Texture Instantiation Options

                            if (this.SelectTexture_PEGI())
                            {
                                id = ImgData;
                                if (id == null) nameHolder = gameObject.name + "_" + GetMaterialTexturePropertyName;
                            }

                            if (id != null)
                                UpdateTylingFromMaterial();



                            if (id != null && pegi.toggle(ref id.lockEditing, icon.Lock.getIcon(), icon.Unlock.getIcon(), "Lock/Unlock editing of selected Texture.", 25))
                            {
                                CheckPreviewShader();
                                if (LockTextureEditing)
                                    UpdateOrSetTexTarget(TexTarget.Texture2D);

#if UNITY_EDITOR
                                if (id.lockEditing)
                                    RestoreUnityTool();
                                else
                                    HideUnityTool();
#endif

                            }


                            var tex = GetTextureOnMaterial();

                            if (pegi.edit(ref tex))
                            {
                                changed = true;
                                ChangeTexture(tex);
                            }

                            if ((IsTerrainControlTexture() == false))
                            {

                                bool isTerrainHeight = IsTerrainHeightTexture();

                                int texScale = (!isTerrainHeight) ? (PainterDataAndConfig.SelectedSizeForNewTexture(PainterCamera.Data.selectedSize))

                                    : (terrain.terrainData.heightmapResolution - 1);

                                List<string> texNames = GetMaterialTextureNames();

                                if (texNames.Count > SelectedTexture)
                                {
                                    string param = GetMaterialTexturePropertyName;

                                    if (icon.NewTexture.Click((id == null) ? "Create new texture2D for " + param : "Replace " + param + " with new Texture2D " + texScale + "*" + texScale, 25).nl())
                                    {
                                        changed = true;
                                        if (isTerrainHeight)
                                            CreateTerrainHeightTexture(nameHolder);
                                        else
                                            CreateTexture2D(texScale, nameHolder, Cfg.newTextureIsColor);
                                    }

                                    if (Cfg.showRecentTextures)
                                    {
                                        List<ImageData> recentTexs;

                                        string texName = GetMaterialTexturePropertyName;

                                        if ((texName != null) && (PainterCamera.Data.recentTextures.TryGetValue(texName, out recentTexs))
                                            && ((recentTexs.Count > 0) || (id == null))) {

                                            if ("Recent Textures:".select(100, ref id, recentTexs).nl())
                                            {
                                                ChangeTexture(id.ExclusiveTexture());
                                                changed = true;
                                            }
                                        }
                                    }


                                    if ((id == null) && (Cfg.allowExclusiveRenderTextures) && ("Create Render Texture".Click()))
                                    {
                                        changed = true;
                                        CreateRenderTexture(texScale, nameHolder);
                                    }

                                    if ((id != null) && (Cfg.allowExclusiveRenderTextures))
                                    {
                                        if ((id.renderTexture == null) && ("Add Render Tex".Click()))
                                        {
                                            changed = true;
                                            id.AddRenderTexture();
                                        }

                                        if (id.renderTexture != null)
                                        {

                                            if ("Replace RendTex".Click("Replace " + param + " with Rend Tex size: " + texScale))
                                            {
                                                changed = true;
                                                CreateRenderTexture(texScale, nameHolder);
                                            }
                                            if ("Remove RendTex".Click().nl())
                                            {
                                                changed = true;

                                                if (id.texture2D != null)
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
                            pegi.Space();
                            pegi.newLine();

                            #endregion

                            #region Texture Saving/Loading

                       


                            if (!LockTextureEditing)
                            {
                                pegi.nl();

                                if (!IsTerrainControlTexture())
                                {

                                    string Orig = null;

                                    id = ImgData;

#if UNITY_EDITOR
                                    if (id.texture2D != null) {
                                        Orig = id.texture2D.GetPathWithout_Assets_Word();
                                        if (Orig!= null && icon.Load.Click("Will reload " + Orig, 25))
                                        {
                                            ForceReimportMyTexture(Orig);
                                            id.SaveName = id.texture2D.name;
                                            pegi.DropFocus();
                                            if (terrain != null)
                                                UpdateShaderGlobals();
                                        }
                                    }

                                    pegi.edit(ref id.SaveName);

                                    if (id.texture2D != null)
                                    {

                                        if (!id.SaveName.SameAs(id.texture2D.name) && icon.Refresh.Click("Use current texture name ({0})".F(id.texture2D.name)))
                                            id.SaveName = id.texture2D.name;

                                        string DestPath = GenerateTextureSavePath();
                                        bool existsAtDestination = TextureExistsAtDestinationPath();
                                        bool originalExists = (Orig != null);
                                        bool sameTarget = originalExists && (Orig.Equals(DestPath));
                                        bool sameTextureName = originalExists && id.texture2D.name.Equals(id.SaveName);


                                        if ((existsAtDestination == false) || sameTextureName)
                                        {
                                            if ((sameTextureName ? icon.Save : icon.SaveAsNew).Click(sameTextureName ? "Will Update " + Orig : "Will save as " + DestPath, 25))
                                            {
                                                if (sameTextureName)
                                                    RewriteOriginalTexture();
                                                else
                                                    SaveTextureAsAsset(false);

                                                OnChangedTexture_OnMaterial();
                                            }
                                        }
                                        else if (existsAtDestination && (icon.Save.Click("Will replace " + DestPath, 25)))
                                            SaveTextureAsAsset(false);

                                        if (!sameTarget && !sameTextureName && !string.IsNullOrEmpty(Orig) && !existsAtDestination && (icon.Replace.Click("Will replace {0} with {1} ".F(Orig, DestPath))))
                                            RewriteOriginalTexture_Rename(id.SaveName);

                                        pegi.nl();

                                    }
#endif

                                }
                                pegi.nl();
                            }
                            pegi.nl();

                            pegi.Space();

                            #endregion

                            pegi.nl();


                        }

                        #endregion
                    }


                }
                pegi.nl();

                #endregion

                #region Plugins
                if (plugins_ComponentPEGI != null)
                    foreach (pegi.CallDelegate p in plugins_ComponentPEGI.GetInvocationList())
                        changed |= p().nl();
                #endregion
            }

            pegi.newLine();

            if (changed)
                Update_Brush_Parameters_For_Preview_Shader();

            inspectedPainter = null;
            return changed;
        }

        public override bool PEGI()
        {
            bool changed = false;

            ToolManagementPEGI();

            if (IsCurrentTool())
            {

                changed |= PEGI_MAIN().nl();

                if (!PainterStuff.IsNowPlaytimeAndDisabled && (!meshEditing))
                {
                    changed |= this.SelectTexture_PEGI().nl();
                    changed |= this.NewTextureOptions_PEGI().nl();
                }
            }

            if ((ImgData != null) && (changed))
                Update_Brush_Parameters_For_Preview_Shader();

            return changed;
        }

#endif

#if UNITY_EDITOR
        static Tool previousEditorTool = Tool.None;
        public static void RestoreUnityTool()
        {
            if (previousEditorTool != Tool.None && Tools.current == Tool.None)
                Tools.current = previousEditorTool;
        }

        public static void HideUnityTool()
        {
            if (Tools.current != Tool.None)
            {
                previousEditorTool = Tools.current;
                Tools.current = Tool.None;
            }
        }

        void OnDrawGizmosSelected()
        {

            if (meshEditing)
            {
                if (!Application.isPlaying)
                    MeshManager.Inst.DRAW_Lines(true);
            }

            if ((IsOriginalShader) && (!LockTextureEditing) && (last_MouseOver_Object == this) && IsCurrentTool() && GlobalBrush.IsA3Dbrush(this) && !Cfg.showConfig)
                Gizmos.DrawWireSphere(stroke.posTo, GlobalBrush.Size(true) * 0.5f);

            if (plugins_GizmoDraw != null)
                foreach (PainterBoolPlugin gp in plugins_GizmoDraw.GetInvocationList())
                    gp(this);

        }
#endif

        #endregion

        #region Mesh Editing 

        public bool IsEditingThisMesh { get { return IsCurrentTool() && meshEditing && (MeshManager.Inst.target == this); } }

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
            if ((data != null) || (data.Length > 0))
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