using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using StoryTriggerData;
using PlayerAndEditorGUI;
//using UnityEditor.SceneManagement;

namespace Playtime_Painter{

    [HelpURL(WWW_Manual)]
    [AddComponentMenu("Mesh/Playtime Painter")]
    [ExecuteInEditMode]
    public class PlaytimePainter : PlaytimeToolComponent, iSTD {

        public static bool isCurrent_Tool() { return enabledTool == typeof(PlaytimePainter); }

        public static PainterConfig cfg { get { return PainterConfig.inst; } }

        public static BrushConfig brush { get { return PainterConfig.inst.brushConfig; } }

        public static PainterManager texMGMT { get { return PainterManager.inst; } }

        public static MeshManager meshMGMT { get { return MeshManager.inst; } }

        public override string ToolName() { return PainterConfig.ToolName; }

        public override Texture ToolIcon() {
            return icon.Painter.getIcon();
        }

        public MeshPackagingProfile meshProfile { get { selectedMeshProfile = Mathf.Max(0, Mathf.Min(selectedMeshProfile, cfg.meshPackagingSolutions.Count - 1));  return cfg.meshPackagingSolutions[selectedMeshProfile]; } }

        public Renderer meshRenderer;
        public SkinnedMeshRenderer skinnedMeshRendy;
        public Terrain terrain;
        public TerrainCollider terrainCollider;
        public MeshFilter meshFilter;
        public MeshCollider meshCollider;
        public Texture2D terrainHeightTexture;
        [NonSerialized]
        public Mesh colliderForSkinnedMesh;

		// Auto-Atlasing

		public Material preAtlasingMaterial;
		public Mesh preAtlasingMesh;
		public string preAtlasingSavedMesh;
		public int inAtlasIndex;
		public int atlasRows = 1;
		public bool isAtlased { get { return  getMaterial(false).isAtlased(); } }
		public bool isProjected{ get { return getMaterial (false).isProjected(); } }

        // Mesh Editing

        [SerializeField]
        public string lastMeshSavedDta;

        [SerializeField]
        Mesh meshDataSavedFor;

        public String meshNameHolder;

        public string meshSaveData { get
            {
                if (meshEditEnabled)
                    MeshManager.inst.DisconnectMesh();

                if (meshDataSavedFor != this.getMesh())
                    lastMeshSavedDta = null;

                return lastMeshSavedDta; }
            set { meshDataSavedFor = this.getMesh(); lastMeshSavedDta = value; }

        }
       
        public bool gotMeshData() {
            return (lastMeshSavedDta != null) && (lastMeshSavedDta.Length > 0) && (meshDataSavedFor == this.getMesh());
        }

        // Config:

        public bool meshEditing = false;
        public bool usePreviewShader = false;
        public bool enableUNDO = false;
        public int numberOfTexture2Dbackups = 0;
        public int numberOfRenderTextureBackups = 0;
        public bool backupManually;
        public int selectedMeshProfile = 0;
      //  public bool useTexcoord2 = false;

        // Textures

        [NonSerialized]
        public static List<imgData> imgdatas = new List<imgData>();

        public List<string> materials_TextureFields;

        [NonSerialized]
        public imgData curImgData = null;

        public string nameHolder= "unnamed";

        public int selectedMaterial = 0;

		public int selectedAtlasedMaterial = -1;



        // To make sure only one object is using Preview shader at any given time.
        public static PlaytimePainter PreviewShaderUser = null;

        [NonSerialized]
        public List<Shader> originalShaders = new List<Shader>();

        public Shader originalShader {
            get { while (originalShaders.Count <= selectedMaterial) originalShaders.Add(null); return originalShaders[selectedMaterial]; }
            set { while (originalShaders.Count <= selectedMaterial) originalShaders.Add(null); originalShaders[selectedMaterial] = value; }
        }

        public static List<NonMaterialTexture> _nonMaterialTexes;

        List<int> _selectedTextures = new List<int>();
        public int selectedTexture {
            get { while (_selectedTextures.Count <= selectedMaterial) _selectedTextures.Add(0); return _selectedTextures[selectedMaterial]; }
            set { while (_selectedTextures.Count <= selectedMaterial) _selectedTextures.Add(0); _selectedTextures[selectedMaterial] = value; } }
        
        // ************************** PAINTING *****************************

        public PlaytimePainter Paint(StrokeVector st, BrushConfig br) {

                if (curImgData == null) {
                    InitIfNotInited();
                    if (curImgData == null) return this;
                }
            
                if (curImgData.destination == texTarget.Texture2D) {
                    RecordingMGMT();
                    PaintToTexture2D(st, br);
                }
                else {
                    if ((terrain == null) || (br.type.supportedForTerrain_RT)) {
                        RecordingMGMT();

                        br.type.Paint(this, br, st);
                    }
                }

                if ((br.useMask) && (st.mouseUp) && (br.randomMaskOffset))
                    br.maskOffset = new Vector2(UnityEngine.Random.Range(0f, 1f),UnityEngine.Random.Range(0f, 1f));

                return this;

        }

        public StrokeVector stroke = new StrokeVector();
	    public float avgBrushSpeed = 0;
	    double mouseBttnTime = 0;

        public static PlaytimePainter currently_Painted_Object;
	    public static PlaytimePainter last_MouseOver_Object;
   
#if BUILD_WITH_PAINTER 

	    public void OnMouseOver() {

            stroke.mouseUp = Input.GetMouseButtonUp(0);
            stroke.mouseDwn = Input.GetMouseButtonDown(0);

        if (Input.GetMouseButtonDown(1))
            mouseBttnTime = Time.time;
        if ((Input.GetMouseButtonUp(1)) && ((Time.time - mouseBttnTime) < 0.2f))
            FocusOnThisObject();

            if (!canPaint())
                return;

            CheckPreviewShader();

            Vector3 mousePos = Input.mousePosition;

		if (CastRayPlaytime(stroke, mousePos)) {

           bool Cntr_Down = (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));

                ProcessMouseGrag(Cntr_Down);

				if ((Input.GetMouseButton(0) || (stroke.mouseUp)) && (!Cntr_Down)) {

                if (currently_Painted_Object != this)  {
                    currently_Painted_Object = this;
                    stroke.SetPreviousValues();
                    FocusOnThisObject();
                }

                    if ((!stroke.mouseDwn) || (canPaintOnMouseDown(brush)))
                        Paint(stroke, brush);
                    else RecordingMGMT();

					if (stroke.mouseUp)
                        currently_Painted_Object = null;
            }
        }
    }

#endif

#if UNITY_EDITOR

        public void OnMouseOver_SceneView(RaycastHit hit, Event e) {

            if (!canPaint())
                return;

            if (!ProcessHit(hit, stroke))
                return;

            if ((currently_Painted_Object != this) && (stroke.mouseDwn)) {
                stroke.firstStroke = true;
                currently_Painted_Object = this;
                FocusOnThisObject();
                stroke.uvFrom = stroke.uvTo;
            }

            bool control = Event.current != null ? (Event.current.control) : false;

            ProcessMouseGrag(control);

            if ((currently_Painted_Object == this)){

                if ((!stroke.mouseDwn) || canPaintOnMouseDown(brush)) {

                    Paint(stroke, brush);

                    Update();
                } else
                    RecordingMGMT();

			} 
				
			if (currently_Painted_Object!= this)
            	currently_Painted_Object = null;
        
        stroke.mouseDwn = false;

    }

#endif

        bool canPaint()
        {

            if (!isCurrentTool()) return false;

            last_MouseOver_Object = this;

            if (meshEditing || LockEditing)
                return false;

            if (isTerrainHeightTexture() && originalShader == null)
            {
                if (stroke.mouseDwn)
                    "Can't edit without Preview".showNotification();

                return false;
            }

            if ((stroke.mouseDwn) || (stroke.mouseUp))
                InitIfNotInited();

            if (curImgData == null)
            {
                if (stroke.mouseDwn)
                    "No texture to edit".showNotification();

                return false;
            }



            return true;

        }

        public bool CastRayPlaytime(StrokeVector st, Vector3 mousePos)
        {
            //v2 = new Vector2();
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(mousePos), out hit, float.MaxValue))
                return ProcessHit(hit, st);



            return false;
        }

        bool ProcessHit(RaycastHit hit, StrokeVector st)
        {
            int submesh = MeshAnaliser.GetSubmeshNumber(this.getMesh(), hit.triangleIndex);
            if (submesh != selectedMaterial)
            {
                if (autoSelectMaterial_byNumberOfPointedSubmesh)
                {
                    selectedMaterial = submesh;
                    OnChangedTexture();
                }
                // else
                //   if ((getRenderer() != null) && (getRenderer().materials.Length>submesh))
                // return false;
            }


            if (curImgData == null) return false;

            st.posTo = hit.point;

            st.uvTo = offsetAndTileUV(hit).To01Space();

            Update_MousePosition_Check_Preview_Shader(st.uvTo, st.posTo, Input.GetMouseButton(0));

            return true;
        }

        public Vector2 GetAtlasedSection()
        {

            float atY = inAtlasIndex / atlasRows;
            float atX = inAtlasIndex - atY * atlasRows;

            return new Vector2(atX, atY);
        }

        public Vector2 offsetAndTileUV(RaycastHit hit)
        {
            var uv = stroke.texcoord2 ? hit.textureCoord2 : hit.textureCoord;

            if (curImgData == null) return uv;
            if (isAtlased)
            {

                uv.x = uv.x % 1;
                uv.y = uv.y % 1;

                //inAtlasIndex

                var m = this.getMesh();

                int vert = m.triangles[hit.triangleIndex * 3];
                List<Vector4> v4l = new List<Vector4>();
                m.GetUVs(0, v4l);
                if (v4l.Count > vert)
                    inAtlasIndex = (int)v4l[vert].z;

                atlasRows = Mathf.Max(atlasRows, 1);

                uv = (GetAtlasedSection() + uv) / (float)atlasRows;

            }
            else
            {
                uv.Scale(curImgData.tiling);
                uv += curImgData.offset;
            }
            return uv;
        }
        
        void ProcessMouseGrag(bool control) {

            if (stroke.mouseDwn) {
                stroke.firstStroke = true;
                stroke.SetPreviousValues();
            }

            if (control) {
                if ((stroke.mouseDwn) && control) {
                    SampleTexture(stroke.uvTo);
                    currently_Painted_Object = null;
                }
            } else {


                if (stroke.mouseDwn) 
                    if  (!backupManually)
                        Backup();
                    
                    if (isTerrainHeightTexture() && stroke.mouseUp) {
                           Preview_To_UnityTerrain();
                    }  
            }
        } 

        public void SampleTexture(Vector2 uv) {
		    brush.color.From(curImgData.SampleAT(uv));
            Update_Brush_Parameters_For_Preview_Shader();
        }

    	public void Backup (){
          
			imgData id = curImgData;
			if (id != null) {
                if (curImgData.destination == texTarget.RenderTexture) {
                    if (numberOfRenderTextureBackups > 0)
                        id.cache.undo.backupRenderTexture(numberOfRenderTextureBackups, id);
                }
                else if (numberOfTexture2Dbackups > 0)
                    id.cache.undo.backupTexture2D(numberOfRenderTextureBackups, id);

				id.cache.redo.Clear ();
			}
	}

	    void PaintToTexture2D (StrokeVector st, BrushConfig br) {
			Vector2 delta_uv = st.uvTo - st.uvFrom;

        if (delta_uv.magnitude > (0.2f + avgBrushSpeed * 3)) delta_uv = Vector2.zero; // This is made to avoid glitch strokes on seams
			else st.avgBrushSpeed = (st.avgBrushSpeed + delta_uv.magnitude) / 2;

        float dist = (int)(delta_uv.magnitude * curImgData.width * 8 / br.Size(false));

        dist = Mathf.Max(dist, 1);
        delta_uv /= dist;
        float alpha = Mathf.Clamp01(br.speed * (Application.isPlaying ? Time.deltaTime : 0.1f));

        for (float i = 0; i < dist; i++) {
            st.uvFrom += delta_uv;
				if (isAtlased)
					Blit_Functions.PaintAtlased (st.uvFrom, alpha, curImgData, br, atlasRows, GetAtlasedSection());
				else
            		Blit_Functions.Paint(st.uvFrom, alpha, curImgData, br);
        }
       
		AfterStroke(st);
    }

	    public void AfterStroke(StrokeVector st) {
          
            st.SetPreviousValues();
        	st.firstStroke = false;
            st.mouseDwn = false;
        if (curImgData.TargetIsTexture2D())
            texture2DDataWasChanged = true;
            //rtp.rtcam.targetTexture = null;
    }

	    bool canPaintOnMouseDown(BrushConfig br) {
			    return ((curImgData.TargetIsTexture2D()) || (br.type.startPaintingTheMomentMouseIsDown));
        }

	    public bool isPaintingInWorldSpace(BrushConfig br) {
			    return ((curImgData != null) && (curImgData.TargetIsRenderTexture()) && (br.type.isA3Dbrush));
        }

        // ************************** TEXTURE MGMT *************************

        public void UpdateTyling() {

		    string fieldName = getMaterialTextureName();
		    Material mat = getMaterial(false);
		    if (isPreviewShader () && (terrain==null)) {
			    curImgData.tiling = mat.GetTextureScale (PainterConfig.previewTexture);
			    curImgData.offset = mat.GetTextureOffset (PainterConfig.previewTexture);
			    return;
		    }
			
            foreach (NonMaterialTexture nt in _nonMaterialTexes)
                if (nt.UpdateTyling(fieldName, this))
                    return;

            if ((mat == null) || (fieldName == null) || (curImgData == null)) return;
            curImgData.tiling = mat.GetTextureScale(fieldName);
            curImgData.offset = mat.GetTextureOffset(fieldName);
        }

	    public void OnChangedTexture() {
                if ((originalShader == null) || (terrain == null))

		    ChangeTexture(getTexture());
        }

        public void ChangeTexture(Texture texture) {
		
            textureWasChanged = false;

		    string field = getMaterialTextureName();

            curImgData = null;

		    if ((texture == null) || (field == null)) {
			    setTextureOnMaterial ();
			    return;
		    }

		    if (texture.isBigRenderTexturePair()) {

                curImgData = texMGMT.bufferTarget;
                if ((curImgData != null) && (curImgData.texture2D != null)) { 
					    curImgData.destination = texTarget.RenderTexture; texture = curImgData.texture2D;
            
                } else {
                    curImgData = null;
				    setTextureOnMaterial ();
                    Debug.Log("Nulling");
                    return;
                }
		    } 

		    curImgData = texture.getImgData();

		    texMGMT.recentTextures.AddIfNew (field, texture);

            if (curImgData == null) 
                curImgData = new imgData(texture);

            UpdateOrSetTexTarget(curImgData.destination);

            UpdateTyling();
            setTextureOnMaterial(field);
        }

        public PlaytimePainter SetTexTarget(BrushConfig br) {
                if (curImgData.TargetIsTexture2D() != br.TargetIsTex2D)
                    UpdateOrSetTexTarget(br.TargetIsTex2D ? texTarget.Texture2D : texTarget.RenderTexture);

                return this;
        }

        public void UpdateOrSetTexTarget(texTarget dst) {

            InitIfNotInited();

            if (curImgData == null) 
               return;
            
            curImgData.updateDestination(dst, getMaterial(true), getMaterialTextureName(), this);
            CheckPreviewShader();

        }

        public void reanableRenderTexture() {
                if ((!meshEditing) && (!LockEditing)) {

                    OnEnable();

                    OnChangedTexture();

                    if (curImgData != null)
                        UpdateOrSetTexTarget(texTarget.RenderTexture); // set it to Render Texture
                }
        }

        public void createTerrainHeightTexture (string NewName) {

            string field = getMaterialTextureName();

            if (field != PainterConfig.terrainHeight) {
                Debug.Log("Terrain height is not currently selected.");
                return;
            }

            int size = terrain.terrainData.heightmapResolution - 1;

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);

            if (curImgData != null)
                curImgData.From(texture);
            else
                ChangeTexture(texture);

            curImgData.SaveName = NewName;
            texture.name = curImgData.SaveName;
            texture.Apply(true, false);
        
            setTextureOnMaterial();

            Unity_To_Preview();
            curImgData.SetAndApply(false);

		    texture.wrapMode = TextureWrapMode.Repeat;

    #if UNITY_EDITOR
            SaveTextureAsAsset(false);

            TextureImporter importer = curImgData.texture2D.getTextureImporter();
            bool needReimport = importer.wasNotReadable();
            needReimport |= importer.wasWrongIsColor(false);
            if (needReimport) importer.SaveAndReimport();
    #endif

            setTextureOnMaterial();
            UpdateShaderGlobalVariables();
        }

        public void createTexture2D(int size, string TextureName, bool isColor) {

			    bool gotRenderTextureData = ((curImgData != null) && ((size == curImgData.width) && (size == curImgData.width)) && (curImgData.TargetIsRenderTexture()));

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true, !isColor);

		    if (gotRenderTextureData && ((curImgData.texture2D == null) || (TextureName == curImgData.SaveName))) 
			    curImgData.texture2D = texture;
		
            ChangeTexture( texture);

		    texture.wrapMode = TextureWrapMode.Repeat;

            curImgData.SaveName = TextureName;
            texture.name = TextureName;

            if (gotRenderTextureData)
            {
                curImgData.RenderTexture_To_Texture2D(curImgData.texture2D);

            }
            else
                if (!isColor)
            
                curImgData.Colorize(new Color(0.5f, 0.5f, 0.5f, 0.99f));
               
            

            texture.Apply(true, false);

    #if UNITY_EDITOR
            SaveTextureAsAsset(true);

            TextureImporter importer = curImgData.texture2D.getTextureImporter();

            bool needReimport = importer.wasNotReadable();
            needReimport |= importer.wasWrongIsColor(isColor);

            if (needReimport) importer.SaveAndReimport();
    #endif

        }

	    public void CreateRenderTexture (int size, string name) {
		    imgData previous = curImgData;
            curImgData = new imgData(size);

            curImgData.SaveName = name;

		    ChangeTexture( curImgData.renderTexture);

		    if (curImgData == null)
			    Debug.Log ("Change texture destroyed curigdata");
		    if (previous!= null) 
			    PainterManager.inst.Render (previous.currentTexture(), curImgData);

		    UpdateOrSetTexTarget (texTarget.RenderTexture);

        }

        // ************************* Material MGMT

        public List<string> getMaterials()  {

            List<string> ms = new List<string>();

            if (terrain != null) {
                Material mat = getMaterial(false);

                if (mat != null)
                    ms.Add(mat.name);

                return ms;
            }

            Material[] mats = meshRenderer.sharedMaterials;

            for (int i=0; i<mats.Length; i++) {
                Material mt = mats[i];
                if (mt != null)
                    ms.Add(mt.name);
                else
                    ms.Add("Null material "+i);
            }
            return ms;
        }

        public List<String> getMaterialTextureNames() {
    #if UNITY_EDITOR
     
		    if (isPreviewShader())
			    return materials_TextureFields;

		    materials_TextureFields = new List<string>();

            foreach (NonMaterialTexture nt in _nonMaterialTexes)
			    nt.GetNonMaterialTextureNames(this, ref materials_TextureFields);

 
            if (terrain == null) {
                materials_TextureFields = getMaterial(false).getTextures();
    
            } else {
                List<string> tmp = getMaterial(false).getTextures();

                foreach (string t in tmp) {
                    if ((!t.Contains("_Splat")) && (!t.Contains("_Normal")))
					    materials_TextureFields.Add(t);
                }
            }
   
        
    #endif
		    return materials_TextureFields;
        }

        public string getMaterialTextureName() {
            List<string> list = getMaterialTextureNames();
            return selectedTexture < list.Count ? list[selectedTexture] : null;
        }

        public Texture getTexture() {

                if (originalShader != null)
                {
                    if ((meshEditing) || (terrain != null)) return null;
                    Material mat = getMaterial(false);

                    return mat == null ? null : mat.GetTexture(PainterConfig.previewTexture);
                }

            string fieldName = getMaterialTextureName();

            if (fieldName == null)
                return null;

            foreach(NonMaterialTexture t in _nonMaterialTexes) {
                Texture tex = null;
                if (t.getTexture(fieldName, ref tex, this))
                    return tex;
            }

            return getMaterial(false).GetTexture(fieldName);
        }

        public Material getMaterial(bool original) {

                Material result = null;

                if (original)
                  SetOriginalShader();

                if (meshRenderer == null){
                    if (terrain != null)
                        result = terrain.materialTemplate;
                    else
                        result = null;
                }
                else {
                    int cnt = meshRenderer.sharedMaterials.Length;
                    if (cnt != 0) {
                        selectedMaterial = Mathf.Clamp(selectedMaterial, 0, cnt - 1);
                        result = meshRenderer.sharedMaterials[selectedMaterial];
                    }
                }

                return result;
        }

        public void setTextureOnMaterial() {
            setTextureOnMaterial(getMaterialTextureName());
        }

        public void setTextureOnMaterial(string fieldName) {
            setTextureOnMaterial( fieldName, curImgData.currentTexture());
        }

        public void setTextureOnMaterial(string fieldName, Texture tex) {
            setTextureOnMaterial( fieldName,  tex, getMaterial(true));
            CheckPreviewShader();
        }

        public void setTextureOnMaterial(string fieldName, Texture tex, Material mat) {
		    
		    if (fieldName != null) {

			    foreach (NonMaterialTexture nt in _nonMaterialTexes) {
                        if (nt.setTextureOnMaterial(fieldName, curImgData, this)) {
					    return;
				    }
			    }
		    }

    
		    if (mat != null) {
			    if (fieldName!= null)
				    mat.SetTexture (fieldName, tex);

			    if (isPreviewShader () && (terrain == null)) 
                        SetTextureOnPreview( tex);
		    }

        }
		
        void SetTextureOnPreview(Texture tex) {
               Material mat = getMaterial(false);
                if (!meshEditing)
                {
              
                    mat.SetTexture(PainterConfig.previewTexture, curImgData.currentTexture());
                    if (curImgData != null)
                    {
                        mat.SetTextureOffset(PainterConfig.previewTexture, curImgData.offset);
                        mat.SetTextureScale(PainterConfig.previewTexture, curImgData.tiling);
                    }
                }
        }

	    public bool isPreviewShader(){
		    return originalShader != null;
	    }

        public void CheckPreviewShader() {

           

            if ((!isCurrentTool ()) || (LockEditing))
			    SetOriginalShader ();
		    else if ((usePreviewShader) && (originalShader == null))
                SetPreviewShader();
        }

        public void SetPreviewShader()
            {

                if ((PreviewShaderUser != null) && (PreviewShaderUser != this))
                    PreviewShaderUser.SetOriginalShader();

                if ((meshEditing) && (meshMGMT.target != this))
                        return;

                Texture tex = curImgData.currentTexture();

                if ((tex == null) && (!meshEditing)) {
                    usePreviewShader = false;
                    return;
                }

                Material m = getMaterial(false);

                if (m == null) {
                    InstantiateMaterial(false);
                    return;
                }

                Shader shd = meshEditing ? texMGMT.mesh_Preview : ((terrain == null) ? texMGMT.br_Preview : texMGMT.TerrainPreview);

                if (shd == null)
                    Debug.Log("Preview shader not found");
                else
                {
                    if (originalShader == null)
                        originalShader = m.shader;

                    m.shader = shd;
                    PreviewShaderUser = this;

                    if ((tex!= null) && (meshEditing == false))
                        SetTextureOnPreview(tex);
                }

                usePreviewShader = true;

                Update_Brush_Parameters_For_Preview_Shader();
        }

        public void SetOriginalShader() {
                //Debug.Log("Un setting");
                Material mat = getMaterial(false);
			    if ((originalShader != null) && (mat != null))
                    mat.shader = originalShader;
			
                originalShader = null;
        }

        public void InstantiateMaterial(bool saveIt) {
		
            SetOriginalShader();

                if ((curImgData != null) && (getMaterial(false) != null))
                UpdateOrSetTexTarget(texTarget.Texture2D);

		    if ( texMGMT.defaultMaterial == null) InitIfNotInited();

            Material mat = getMaterial(true);

            if ((mat == null) && (terrain != null))
            {

                mat = new Material(texMGMT.TerrainPreview);

                terrain.materialTemplate = mat;
                terrain.materialType = Terrain.MaterialType.Custom;
            }
            else
            {
			    Material hold = (mat == null ? texMGMT.defaultMaterial : mat);
                meshRenderer.material = Instantiate(hold);
                getMaterial(true).CopyPropertiesFromMaterial(hold);
                CheckPreviewShader();
            }

                if (saveIt) {
    #if UNITY_EDITOR
                    string fullPath = Application.dataPath + "/" + cfg.materialsFolderName;
                    Directory.CreateDirectory(fullPath);

                    string name = gameObject.name;

                    string path = AssetDatabase.GenerateUniqueAssetPath("Assets/" + cfg.materialsFolderName + "/" + name + ".mat");
                    if (getMaterial(false) != null) {
                        AssetDatabase.CreateAsset(getMaterial(false), path);
                        AssetDatabase.Refresh();
                        CheckPreviewShader();
                    }
    #endif
                }

            OnChangedTexture();

                if ((curImgData != null) && (getMaterial(false) != null))
                UpdateOrSetTexTarget(curImgData.destination);
    
	    }

        public Renderer getRenderer() {

                if (meshRenderer != null)
                    return meshRenderer;
                else if (skinnedMeshRendy != null)
                    return skinnedMeshRendy;
                else 
                    return null;

            }

            // *************************** Terrain MGMT

        float tilingY = 8;

        public void UpdateShaderGlobalVariables() {

            foreach (NonMaterialTexture nt in _nonMaterialTexes) 
                nt.OnUpdate(this);
        
            if (terrain == null) return;
        
            SplatPrototype[] sp = terrain.terrainData.splatPrototypes;

            if (sp.Length != 0) {
                float tilingX = terrain.terrainData.size.x / sp[0].tileSize.x;
                float tilingZ = terrain.terrainData.size.z / sp[0].tileSize.y;
                Shader.SetGlobalVector(PainterConfig.terrainTiling, new Vector4(tilingX, tilingZ, sp[0].tileOffset.x, sp[0].tileOffset.y));

                tilingY = terrain.terrainData.size.y / sp[0].tileSize.x;
            }
            Shader.SetGlobalVector(PainterConfig.terrainScale, new Vector4(terrain.terrainData.size.x, terrain.terrainData.size.y, terrain.terrainData.size.z, 0.5f / ((float)terrain.terrainData.heightmapResolution)));

            UpdateTerrainPosition();

            Texture[] alphamaps = terrain.terrainData.alphamapTextures;
            if (alphamaps.Length > 0)
                Shader.SetGlobalTexture(PainterConfig.terrainControl, alphamaps[0].getDestinationTexture());

        }

        public void UpdateTerrainPosition()
            {
                Vector3 pos = transform.position;
                Shader.SetGlobalVector(PainterConfig.terrainPosition, new Vector4(pos.x, pos.y, pos.z, tilingY));
            }

        public void Preview_To_UnityTerrain() {

            bool rendTex = (curImgData.TargetIsRenderTexture());
            if (rendTex)  UpdateOrSetTexTarget(texTarget.Texture2D);

            TerrainData td = terrain.terrainData;

            int res = td.heightmapResolution - 1;

            float conversion = ((float)curImgData.width / (float)res);

            float[,] heights = td.GetHeights(0, 0, res + 1, res + 1);

            Color[] cols = curImgData.pixels;

            if (conversion != 1)
            {

                for (int y = 0; y < res; y++)
                {
                    int yind = curImgData.width * Mathf.FloorToInt((y * conversion));
                    for (int x = 0; x < res; x++)
                    {



                        heights[y, x] = cols[yind + (int)(x * conversion)].a;
                    }
                }
            } else {
				
                for (int y = 0; y < res; y++)
                {
                    int yind = curImgData.width * y;

                    for (int x = 0; x < res; x++) 
                        heights[y, x] = cols[yind + x].a;
                
                }


            }

            for (int y = 0; y < res - 1; y++)
                heights[y, res] = heights[y, res - 1];
            for (int x = 0; x < res; x++)
                heights[res, x] = heights[res - 1, x];

            terrain.terrainData.SetHeights(0, 0, heights);

            UpdateShaderGlobalVariables();

            if (rendTex) UpdateOrSetTexTarget(texTarget.RenderTexture);
        }

        public void Unity_To_Preview() {

            imgData id = terrainHeightTexture.getImgData();

            bool current = (id == curImgData);
            bool rendTex = current && (curImgData.TargetIsRenderTexture());
            if (rendTex) UpdateOrSetTexTarget(texTarget.Texture2D);


            int textureSize = terrain.terrainData.heightmapResolution-1;

            if (id.width != textureSize) {
                Debug.Log("Wrong size: "+ id.width+ " textureSize " +id.texture2D.width);
                if (current)
                {
                    createTerrainHeightTexture(curImgData.SaveName);
                    id = curImgData;
                }
                else Debug.Log("Is not current");

                return;
            }
			
            terrainHeightTexture = id.texture2D;
            Color[] col = id.pixels;
       
            float height = 1f / ((float)terrain.terrainData.size.y);

            for (int y = 0; y < textureSize; y++) {
                int fromY = y * textureSize;

                for (int x = 0; x < textureSize; x++) {
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
                OnChangedTexture();

            if (rendTex)
                UpdateOrSetTexTarget(texTarget.RenderTexture);

            UpdateShaderGlobalVariables();
        }

        public bool isTerrainHeightTexture() {
		
            if (terrain == null)
                return false;
            string name = getMaterialTextureName();
            if (name == null)
                return false;
            return name.Contains(PainterConfig.terrainHeight);
        }

        public TerrainHeight getTerrainHeight() {

           foreach (NonMaterialTexture nt in _nonMaterialTexes) {
                if (nt.GetType() == typeof(TerrainHeight))
                    return ((TerrainHeight)nt);
           }
			
            return null;
 
        }

        public bool isTerrainControlTexture() {
            return ((curImgData != null) && (terrain != null) && (getMaterialTextureName().Contains(PainterConfig.terrainControl)));
        }


        // ************************** RECORDING & PLAYBACK ****************************
        
        public static List<PlaytimePainter> playbackPainters = new List<PlaytimePainter>();

        public List<string> playbackVectors = new List<string>();

        public static stdDecoder cody = new stdDecoder("");

        public void PlayStrokeData(string strokeData)
        {
            if (!playbackPainters.Contains(this))
                playbackPainters.Add(this);
            StrokeVector.PausePlayback = false;
            playbackVectors.Add(strokeData);
        }

        public void PlayByFilename(string recordingName)
        {
            if (!playbackPainters.Contains(this))
                playbackPainters.Add(this);
            StrokeVector.PausePlayback = false;
            playbackVectors.Add(cfg.GetRecordingData(recordingName));
        }

        public void PlaybeckVectors()
        {

            if (cody.gotData)
            {
                // string tag = cody.getTag();
                //string data = cody.getData();
                //Debug.Log("TAG: "+tag + " DATA: "+data);

                Decode(cody.getTag(), cody.getData());
            }
            else
            {
                if (playbackVectors.Count > 0)
                {
                    cody = new stdDecoder(playbackVectors.last());
                    playbackVectors.RemoveLast(1);
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

        void RecordingMGMT()
        {
            if (curImgData.recording)
            {

                if (stroke.mouseDwn)
                {
                    prevDir = Vector2.zero;
                    prevPOSDir = Vector3.zero;
                }

                bool canRecord = stroke.mouseDwn || stroke.mouseUp;

                bool rt = curImgData.TargetIsRenderTexture();
                bool worldSpace = rt && brush.type.isA3Dbrush;

                if (!canRecord)
                {


                    float size = brush.Size(worldSpace);

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

        public stdEncoder Encode()
        {
            stdEncoder cody = new stdEncoder();

            if (stroke.mouseDwn)
            {
                cody.Add(BrushConfig.storyTag, brush.EncodeStrokeFor(this)); // Brush is unlikely to change mid stroke
                cody.AddText("trg", curImgData.TargetIsTexture2D() ? "C" : "G");
            }

            cody.Add(StrokeVector.storyTag, stroke.Encode(curImgData.TargetIsRenderTexture() && brush.type.isA3Dbrush));

            return cody;
        }

        public void Decode(string tag, string data)
        {
            switch (tag)
            {
                case "trg": UpdateOrSetTexTarget(data.Equals("C") ? texTarget.Texture2D : texTarget.RenderTexture); break;
                case BrushConfig.storyTag:
                    InitIfNotInited();
                    brush.Reboot(data);
                    brush.Brush2D_Radius *= curImgData == null ? 256 : curImgData.width; break;
                case StrokeVector.storyTag:
                    stroke.Reboot(data);
                    Paint(stroke, brush);
                    break;
            }
        }
        
        public iSTD Reboot(string data)
        {
            new stdDecoder(data).DecodeTagsFor(this);
            return this;
        }

        public const string storyTag = "painter";

        public string getDefaultTagName()
        {
            return storyTag;
        }

        // ************************** SAVING *******************************

#if UNITY_EDITOR

        public void ForceReimportMyTexture(string path) {
		
        TextureImporter importer = AssetImporter.GetAtPath("Assets" + path) as TextureImporter;
        if (importer == null) {
            Debug.Log("No importer for "+path);
            return;
        }

        importer.SaveAndReimport();
        if (curImgData.TargetIsRenderTexture())
            curImgData.TextureToRenderTexture(curImgData.texture2D);
        else
            if (curImgData.texture2D!= null)
            curImgData.PixelsFromTexture2D(curImgData.texture2D);

        setTextureOnMaterial();
    }

        public bool textureExistsAtDestinationPath() {
            TextureImporter importer = AssetImporter.GetAtPath("Assets" + GenerateTextureSavePath()) as TextureImporter;
            return importer != null;
        }

        public string GenerateTextureSavePath() {
            return ("/"+ cfg.texturesFolderName + "/"+ curImgData.SaveName + ".png");
        }

        public string GenerateMeshSavePath() {
                if (meshFilter.sharedMesh == null)
                    return "None";

            return ("/" + cfg.meshesFolderName + "/" + meshNameHolder + ".asset");
        }

        void OnBeforeSaveTexture(){
		    if (curImgData.TargetIsRenderTexture()) {
			    curImgData.RenderTexture_To_Texture2D (curImgData.texture2D);
		    }
	    }

	    void OnPostSaveTexture(){
		    UpdateOrSetTexTarget(curImgData.destination);
		    UpdateShaderGlobalVariables();
		    setTextureOnMaterial ();
	    }

	    public void RewriteOriginalTexture_Rename(string name) {
		
		    OnBeforeSaveTexture ();

		    curImgData.texture2D = curImgData.texture2D.rewriteOriginalTexture_NewName(name);

		    OnPostSaveTexture ();
	    }

        public void RewriteOriginalTexture() {
		
		    OnBeforeSaveTexture ();

            curImgData.texture2D.rewriteOriginalTexture();

		    OnPostSaveTexture ();
        }

        public void SaveTextureAsAsset(bool asNew)  {

		    OnBeforeSaveTexture ();

            curImgData.texture2D = curImgData.texture2D.saveTextureAsAsset(cfg.texturesFolderName, ref curImgData.SaveName, asNew);

            curImgData.texture2D.Reimport_IfNotReadale();

		    OnPostSaveTexture ();

        }

        public void SaveMesh() {

                Mesh m = this.getMesh();
                string path = AssetDatabase.GetAssetPath(m);

   
            
                    string lastPart = "/" + cfg.meshesFolderName + "/";
                    string folderPath = Application.dataPath + lastPart;
                    Directory.CreateDirectory(folderPath);

            try
            {

                if (path.Length > 0)
                    meshFilter.sharedMesh = (Mesh)Instantiate(meshFilter.sharedMesh);


                if (meshNameHolder.Length == 0)
                    meshNameHolder = meshFilter.sharedMesh.name;
                else
                    meshFilter.sharedMesh.name = meshNameHolder;

                    AssetDatabase.CreateAsset(meshFilter.sharedMesh, "Assets" + GenerateMeshSavePath());
                
                        AssetDatabase.SaveAssets();
                    } catch (Exception ex) {
                        Debug.Log(ex);
                    }
            }
    
#endif

        //*************************** COMPONENT MGMT ****************************

        public bool LockEditing;
        public bool forcedMeshCollider;
        bool inited = false;
        public bool autoSelectMaterial_byNumberOfPointedSubmesh = true;


        public const string WWW_Manual = "https://docs.google.com/document/d/170k_CE-rowVW9nsAo3EUqVIAWlZNahC0ua11t1ibMBo/edit?usp=sharing";

#if UNITY_EDITOR

        [MenuItem("Tools/" + PainterConfig.ToolName + "/Attach Painter To Selected")]
        static void givePainterToSelected() {
        foreach (GameObject go in Selection.gameObjects) 
            IterateAssignToChildren(go.transform);
    }

        static void IterateAssignToChildren(Transform tf) {

        if ((tf.GetComponent<PlaytimePainter>() == null)
            && (tf.GetComponent<Renderer>() != null) 

            && (tf.GetComponent<RenderBrush>() == null) && (PlaytimeToolComponent.PainterCanEditWithTag(tf.tag)))
            tf.gameObject.AddComponent<PlaytimePainter>();

        for (int i = 0; i < tf.childCount; i++)
            IterateAssignToChildren(tf.GetChild(i));

    }

	    [MenuItem("Tools/" + PainterConfig.ToolName + "/Remove Painters From the Scene")]
	    static void takePainterFromAll() {
		Renderer[] allObjects = UnityEngine.Object.FindObjectsOfType<Renderer>();
		foreach (Renderer mr in allObjects) {
			PlaytimePainter ip = mr.GetComponent<PlaytimePainter>();
			if (ip != null) 
				DestroyImmediate(ip);
		}

		PainterManager rtp = UnityEngine.Object.FindObjectOfType<PainterManager>();
		if (rtp != null)
			DestroyImmediate(rtp.gameObject);

	}

        [MenuItem("Tools/" + PainterConfig.ToolName + "/Instantiate Painter Camera")]
        static void InstantiatePainterCamera() {
        PainterManager r = PainterManager.inst;
    }
      
        [MenuItem("Tools/" + PainterConfig.ToolName + "/Join Discord")]
        public static void open_Discord() {
            Application.OpenURL("https://discord.gg/rF7yXq3");
        }
        
        [MenuItem("Tools/" + PainterConfig.ToolName + "/Open Manual")]
        public static void openWWW_Documentation() {
		Application.OpenURL(WWW_Manual);
	}
        
        public static void openWWW_Forum() {
        Application.OpenURL("https://www.quizcanners.com/forum/texture-editor");
    }
        [MenuItem("Tools/" + PainterConfig.ToolName + "/Send an Email")]

        public static void open_Email() {
        Application.OpenURL("mailto:quizcanners@gmail.com");
    }

#endif

        public override void OnDestroy() {

         
          

		base.OnDestroy ();

		Collider[] collis = GetComponents<Collider>();

		foreach (Collider c in collis)
			if (c.GetType() != typeof(MeshCollider)) c.enabled = true;

		collis = GetComponentsInChildren<Collider>();

		foreach (Collider c in collis)
			if (c.GetType() != typeof(MeshCollider)) c.enabled = true;

		if (forcedMeshCollider && (meshCollider != null))
			meshCollider.enabled = false;
    }

        void OnDisable() {
            SetOriginalShader();
                if ((curImgData!= null) && (curImgData.texture2D != null))
                    UpdateOrSetTexTarget(texTarget.Texture2D);
                inited = false;

                if ((PainterManager._inst != null) && (MeshManager.inst.target == this)) {
                    MeshManager.inst.DisconnectMesh();
                    MeshManager.inst.previouslyEdited = this;
                }
            }

        public override void OnEnable() {
		
		    base.OnEnable ();
            if (_nonMaterialTexes == null) {
                _nonMaterialTexes = new List<NonMaterialTexture>();
                NonMaterialTexture.updateList(ref _nonMaterialTexes);
            }

            if (terrain != null) 
                UpdateShaderGlobalVariables();

                if (meshRenderer == null)
                    meshRenderer = GetComponent<MeshRenderer>();

                if ((curImgData != null) && (curImgData.texture2D == null))
                    curImgData = null;

    #if BUILD_WITH_PAINTER
            materials_TextureFields = getMaterialTextureNames();
    #endif

            }

        public void UpdateColliderForSkinnedMesh() {

            if (colliderForSkinnedMesh == null) colliderForSkinnedMesh = new Mesh();
            skinnedMeshRendy.BakeMesh(colliderForSkinnedMesh);
            if (meshCollider != null)
                meshCollider.sharedMesh = colliderForSkinnedMesh;

        }

        public void InitIfNotInited() {
              //  return;

            if ((!inited) || 
                (((meshCollider == null) || (meshRenderer == null)) 
                && ((terrain == null) || (terrainCollider == null)) ) ) {
                inited = true;

                    nameHolder = gameObject.name;

                if (meshRenderer == null) 
                    meshRenderer = GetComponent<Renderer>();

                if (curImgData == null) 
                    OnChangedTexture();
            
                if (meshRenderer != null) {
                    
                    Collider[] collis = GetComponents<Collider>();

                    foreach (Collider c in collis)
                        if (c.GetType() != typeof(MeshCollider)) c.enabled = false;

                    collis = GetComponentsInChildren<Collider>();

                    foreach (Collider c in collis)
                        if (c.GetType() != typeof(MeshCollider)) c.enabled = false;

                    meshCollider = GetComponent<MeshCollider>();
                    meshFilter = GetComponent<MeshFilter>();

                    if (meshCollider == null) {
                        meshCollider = meshRenderer.gameObject.AddComponent<MeshCollider>();
                        forcedMeshCollider = true;
                    } else if (meshCollider.enabled == false) {
                        meshCollider.enabled = true;
                        forcedMeshCollider = true;
                    }
					
                }

                if ((meshRenderer != null) && (meshRenderer.GetType() == typeof(SkinnedMeshRenderer))) {
                    skinnedMeshRendy = (SkinnedMeshRenderer)meshRenderer;
                    UpdateColliderForSkinnedMesh();
                }
                else skinnedMeshRendy = null;

                if (meshRenderer == null) {
                    terrain = GetComponent<Terrain>();
                    if (terrain != null)  
                        terrainCollider = GetComponent<TerrainCollider>();
                
                }
            }
        }
 
        public void FocusOnThisObject() {
		
    #if UNITY_EDITOR
            UnityHelperFunctions.FocusOn(this.gameObject);
    #endif
            selectedInPlaytime = this;
            Update_Brush_Parameters_For_Preview_Shader();
            InitIfNotInited();
        }

  

        //************************** UPDATES  **************************

        public bool textureWasChanged = false;
        bool texture2DDataWasChanged;
        float repaintTimer;
  
        public void Update() {
    #if UNITY_EDITOR || BUILD_WITH_PAINTER

                if ((!LockEditing) && (meshEditEnabled ) && (Application.isPlaying))
                        MeshManager.inst.DRAW_Lines(false);


                    if (textureWasChanged) 
                OnChangedTexture();
     
            repaintTimer -= (Application.isPlaying) ?  Time.deltaTime : 0.016f;

			    if (texture2DDataWasChanged && ((repaintTimer < 0) || (stroke.mouseUp))) {
                   // Debug.Log("repainting delay");
                texture2DDataWasChanged = false;
                if ((curImgData != null) && (curImgData.texture2D!= null))
                    curImgData.SetAndApply(!brush.DontRedoMipmaps);
                repaintTimer = brush.repaintDelay;
            }
    #endif
        }

        public void Update_MousePosition_Check_Preview_Shader(Vector3 uv, Vector3 hitPos, bool hide) {
                    CheckPreviewShader();
                if (originalShader!= null)
                    PainterManager.Shader_Pos_Update(uv, hitPos, hide, brush.Size(isPaintingInWorldSpace(brush)));
        }

        public void Update_Brush_Parameters_For_Preview_Shader() {
                if ((curImgData != null) && (originalShader != null))
                {
                    texMGMT.Shader_BrushCFG_Update(brush, 1, curImgData.width, curImgData.TargetIsRenderTexture(), stroke.texcoord2);
                    BlitModeExtensions.SetShaderToggle(!isAtlased, PainterConfig.UV_NORMAL , PainterConfig.UV_ATLASED);
                }
        }
    
        // ********************* PEGI **********************************

        public override void OnGUI() {
    #if !BUILD_WITH_PAINTER
                //Debug.Log("Not building with painter");
    #endif

    #if BUILD_WITH_PAINTER
                if (!cfg.disablePainterUIonPlay)
                        base.OnGUI();
    #endif

            }

        public override string playtimeWindowName {
		    get {
			    return gameObject.name+" "+getMaterialTextureName();
		    }
	    }

        public bool management_PEGI() {
                bool changed = false;

                if (!isNowPlaytimeAndDisabled())
                {

                    if ((meshManager.target != null) && (meshManager.target != this))
                        meshManager.DisconnectMesh();

                    if (!cfg.showConfig)
                    {
                        if (meshEditing)
                        {
                            if (icon.Painter.Click("Edit Texture", 25))
                            {
                                SetOriginalShader();
                                meshEditing = false;
                                CheckPreviewShader();
                                meshMGMT.DisconnectMesh();
                                changed = true;
                                cfg.showConfig = false;
                                "Editing Texture".showNotification();
                            }
                        }
                        else
                        {
                            if (icon.mesh.Click("Edit Mesh", 25))
                            {
                                meshEditing = true;
                                LockEditing = false;
                                SetOriginalShader();
                                UpdateOrSetTexTarget(texTarget.Texture2D);
                                cfg.showConfig = false;
                                "Editing Mesh".showNotification();

                                if (gotMeshData())
                                    meshMGMT.EditMesh(this, false);

                                return true;
                            }

                            if (pegi.toggle(ref LockEditing, icon.Lock.getIcon(), icon.Unlock.getIcon(), "Lock/Unlock editing of this abject.", 25))
                            {
                                CheckPreviewShader();
                                if (LockEditing) UpdateOrSetTexTarget(texTarget.Texture2D);

#if UNITY_EDITOR
                           else  Tools.current = Tool.None;
#endif

                        }

                        }
                    }

                    pegi.toggle(ref cfg.showConfig, meshEditing ? icon.mesh : icon.Painter, icon.Config, "Settings", 25);
                }

                if ((cfg.showConfig) || (isNowPlaytimeAndDisabled()))
                {

                    pegi.newLine();

                    PainterConfig.inst.PEGI(this);//this.config_PEGI();

                }
                else
                {
                    if (meshEditing)
                        meshPEGI();
                    else if ((curImgData != null) && (!LockEditing)) {

                        curImgData.undo_redo_PEGI();
                        if (!curImgData.recording)
                            this.Playback_PEGI();

                        pegi.newLine();

                        bool toTexture2D = curImgData.TargetIsTexture2D();

                        if (isAtlased) {
                            if (originalShader == null) {
                                var m = getMaterial(false);
                                if (m.HasProperty(PainterConfig.atlasedTexturesInARow))
                                atlasRows = getMaterial(false).GetInt(PainterConfig.atlasedTexturesInARow);
                            }

						    ("Atlased Texture "+atlasRows+"*"+atlasRows).write("Shader has _ATLASED define");
						    if ("Undo".Click (40).nl())
							    getMaterial (false).DisableKeyword (PainterConfig.UV_ATLASED);

						    if (curImgData.TargetIsRenderTexture ())
							    pegi.writeOneTimeHint ("Watch out, Render Texture Brush can change neighboring textures on the Atlas.", "rtOnAtlas");

                            if (!toTexture2D)
                            {
                                pegi.writeWarning("Render Texture painting does not yet support Atlas Editing");
                                pegi.newLine();
                            }
                        }

					    if (isProjected) {
						    pegi.writeWarning ("Projected UV Shader detected. Painting may not work properly");
						    if ("Undo".Click (40).nl())
							    getMaterial (false).DisableKeyword (PainterConfig.UV_PROJECTED);
						    pegi.newLine ();
					    }

					    if ((!toTexture2D) && (brush.type.isA3Dbrush) && (curImgData.offset!= Vector2.zero) && (curImgData.tiling!= Vector2.one) ) {
						    pegi.writeHint ("World space painting may not work properly when tiling and/or offset is applied.");	
						    pegi.newLine ();
					    }
						

                        changed |= brush.PEGI(this);

                        BlitMode mode = brush.blitMode;
                        Color col = brush.color.ToColor();

                    


                        if ((toTexture2D || (!mode.usingSourceTexture)) && (isTerrainHeightTexture() == false))
                        {
                            if (pegi.edit(ref col))
                            {
                                changed = true;
                                brush.color.From(col);
                            }
                        }

                        pegi.newLine();

                        changed |= brush.ColorSliders(this).nl();

                        if ((backupManually) && ("Backup for UNDO".nl()))
                            Backup();

                        if (cfg.moreOptions || stroke.texcoord2)
                            changed |= "Use Texcoord 2".toggle(ref stroke.texcoord2).nl();

                    }
                }
                pegi.newLine();
                return changed;
            }

        public static PlaytimePainter inspectedPainter;

        public static bool isNowPlaytimeAndDisabled() {
    #if !BUILD_WITH_PAINTER
                if (Application.isPlaying)
                    return true;

    #endif
                return false;
            }

        public override bool PEGI () {
                bool changed = false;
                inspectedPainter = this;
                ToolManagementPEGI (); 

		    if (!isCurrentTool())
                    return changed;
        
                changed |= management_PEGI().nl();


                if (isNowPlaytimeAndDisabled()) 
                    return changed;
            
                if ((LockEditing) || (meshEditing))
                    return changed;

                changed |= this.SelectTexture_PEGI();

                pegi.newLine();
                this.NewTextureOptions_PEGI().nl();

                if ((curImgData != null) && (changed))
                        Update_Brush_Parameters_For_Preview_Shader();

                return changed;
            }


        public bool meshPEGI()
            {
                bool changed = false;
                inspectedPainter = this;
                pegi.newLine();

                MeshManager m = MeshManager.inst;


                if (meshFilter != null)
                {

                    if (this != m.target)
                    {
                        if ((lastMeshSavedDta != null) && (lastMeshSavedDta.Length > 0))
                            "Got saved mesh data".nl();
                        else "No profile data found".nl();
                    }

                    // if ((m.target != null) && (m.target != this))
                    //   ("Editing " + m.target.gameObject.name).nl();

                    pegi.writeOneTimeHint("Warning, this will change (or mess up) your model.", "MessUpMesh");

                    if (m.target != this) {

                        if (Application.isPlaying)
                            pegi.writeWarning("Playtime Changes will be reverted once you try to edit the mesh again.");
                        pegi.newLine();

					    if ("Edit Copy".Click ()) {
						    meshMGMT.EditMesh (this, true);

					    }
                        if ("New Mesh".Click()) {
                            meshFilter.mesh = new Mesh();
                            lastMeshSavedDta = null;
                            meshMGMT.EditMesh(this, false);
                        }
                  
                        if (icon.Edit.Click("Edit Mesh", 25).nl())
                            meshMGMT.EditMesh(this, false);
                    }

                }
                else if ("Add Mesh Filter".Click().nl())
                {
                    meshFilter = gameObject.AddComponent<MeshFilter>();
                    if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();
                }

                if ((this == null) || (m.target != this))
                    return changed;

                if ("Profile".foldout())
                {
                    if ((cfg.meshPackagingSolutions.Count > 1) && (icon.Delete.Click(25)))
                        cfg.meshPackagingSolutions.RemoveAt(selectedMeshProfile);
                    else
                    {
                        pegi.newLine();
                        if (meshProfile.PEGI().nl())
                            m._Mesh.Dirty = true;

                        if ("Hint".foldout(ref VertexSolution.showHint).nl()) {
                        "If using projected UV, place sharpNormal in TANGENT".nl();
                        "Vectors should be placed in normal and tangent slots to batch correctly".nl();
                        "keep uv1 as is for baked light and damage shaders".nl();
                        "I usually place edge in UV3".nl();
                    }


                    }
                }
                else
                {

                    if ((" : ".select(20, ref selectedMeshProfile, cfg.meshPackagingSolutions)) && (meshEditEnabled))
                        meshMGMT._Mesh.Dirty = true;
                    if (icon.Add.Click(25).nl())
                    {
                        cfg.meshPackagingSolutions.Add(new MeshPackagingProfile());
                        selectedMeshProfile = cfg.meshPackagingSolutions.Count - 1;
                        meshProfile.name = "New Profile " + selectedMeshProfile;
                    }
                    pegi.newLine();



                    m.PEGI().nl();
                }

                pegi.newLine();

                //if (changed) pegi.SaveChanges();

                return changed;
            }

#if UNITY_EDITOR

        void OnDrawGizmosSelected()  {

            if (!LockEditing) {
                if (meshEditing)
                {
                    if  (!Application.isPlaying)
                    MeshManager.inst.DRAW_Lines(true);
                }
                else
                if ((originalShader == null) && (last_MouseOver_Object == this) && isCurrentTool() && isPaintingInWorldSpace(brush))
                    Gizmos.DrawWireSphere(stroke.posTo, brush.Size(true) * 0.5f);
                
            }
        
    }

#endif

        // **********************************   Mesh Editing *****************************


        public bool meshEditEnabled { get { return isCurrentTool() && meshEditing && (MeshManager.inst.target == this); } }

        public MeshManager meshManager { get { return MeshManager.inst; } }

        public EditableMesh editedMesh
        {
            get
            {
                if (meshManager.target == this) return meshManager._Mesh;
                Debug.Log(name + " call Edit before accessing edited mesh."); return null;
            }
        }

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

        public bool TryLoadMesh(string data) {
            if ((data != null) || (data.Length > 0)) {
                meshSaveData = data;

                meshManager.EditMesh(this, true);

                meshManager.DisconnectMesh();

               

                return true;
            }
            return false;
        }

    }
}