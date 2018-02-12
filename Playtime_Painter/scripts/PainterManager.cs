using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using System;
using PlayerAndEditorGUI;
using UnityEngine.EventSystems;
namespace Painter{

#if UNITY_EDITOR
    [CustomEditor(typeof(PainterManager))]
    public class RenderTexturePainterEditor : Editor {

        PainterManager rtp;

        public static int testValue = 1;

        public override void OnInspectorGUI() {
            ef.start(serializedObject);
            ((PainterManager)target).PEGI();
        }

    }

#endif


    [ExecuteInEditMode]
public class PainterManager : MonoBehaviour {

        [SerializeField]
        PainterConfig painterCfg;

    static PainterConfig cfg { get { return PainterConfig.inst; } }

    public static PainterManager _inst;
        public static PainterManager inst {
            get {
                if (_inst == null)
                {
                    _inst = GameObject.FindObjectOfType<PainterManager>();
                    if (_inst == null) {
                        if (_inst != null)
                            _inst.gameObject.SetActive(true);
                        else {
#if UNITY_EDITOR
                            GameObject go = Resources.Load("prefabs/" + PainterConfig.PainterCameraName) as GameObject;
                            _inst = Instantiate(go).GetComponent<PainterManager>();
                            _inst.name = PainterConfig.PainterCameraName;
#endif
                        }
                    }
                    if (_inst.meshManager == null)
                        _inst.meshManager = new MeshManager();

                }

                return _inst;
            }
        }

	public bool isLinearColorSpace;

    /* Should be power of 2. 
     *After changing this value, you need to destroy "Render Texture Painter in your scenes" for everything to be reinitialized.*/

    public const int renderTextureSize = 2048;//4096;

    public Texture[] sourceTextures;

    public Texture getSourceTexture(int ind)
    {
        return ind < sourceTextures.Length ? sourceTextures[ind] : null;
    }

    public Texture[] masks;

    public List<channelSetsForCombinedMaps> forCombinedMaps;

    public List<AtlasTextureCreator> atlases;

	public List<MaterialAtlases> atlasedMaterials;

    public VolumetricDecal[] decals;

    public VolumetricDecal GetDecal(int no)
    {
		return ((decals.Length > no) && (decals[no].showInDropdown())) ? decals[no] : null;
    }
        
    [NonSerialized]
	public Dictionary<string, List<Texture>> recentTextures = new Dictionary<string, List<Texture>> ();

    public Camera rtcam;
    public int myLayer = 30; // this layer is used by camera that does painting. Make your other cameras ignore this layer.
    public RenderBrush brushPrefab;
    public static float orthoSize = 128; // Orthographic size of the camera. 
	//[NonSerialized]
	public bool DebugDisableSecondBufferUpdate;
    public RenderBrush brushRendy = null;
        
    public MeshManager meshManager = new MeshManager();
        
    // Brush shaders
	public Shader br_Blit = null;
	public Shader br_Add = null;
	public Shader br_Copy = null;
	public Shader pixPerfectCopy = null;
	public Shader bufferCopy = null;
	public Shader br_Multishade = null;
	public Shader br_BlurN_SmudgeBrush = null;

    public Shader mesh_Preview = null;
    public Shader br_Preview = null;
    public Shader TerrainPreview = null;

	public Material defaultMaterial;

    static Vector3 prevPosPreview;
    static float previewAlpha = 1;

        public string testString = "_test";

    // ******************* Buffers MGMT

    [NonSerialized]
    RenderTexture[] squareBuffers = new RenderTexture[10];
    public RenderTexture GetSquareBuffer(int width)
    {
        int no = 9;
        switch (width)
        {
            case 8: no = 0; break;
            case 16: no = 1; break;
            case 32: no = 2; break;
            case 64: no = 3; break;
            case 128: no = 4; break;
            case 256: no = 5; break;
            case 512: no = 6; break;
            case 1024: no = 7; break;
            case 2048: no = 8; break;
            case 4096: no = 9; break;
            default: Debug.Log(width + " is not in range "); break;
        }

        if (squareBuffers[no] == null) {
            squareBuffers[no] = new RenderTexture(width, width, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
        }
        return squareBuffers[no];
    }

    // Main Render Textures used for painting
    public RenderTexture[] BigRT_pair;

    // Buffers used to resize texture to smaller size
    [NonSerialized]
    List<RenderTexture> nonSquareBuffers = new List<RenderTexture>();
    public RenderTexture GetNonSquareBuffer(int width, int height)
    {
        foreach (RenderTexture r in nonSquareBuffers)
        {
            if ((r.width == width) && (r.height == height)) return r;
        }

        RenderTexture rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
        nonSquareBuffers.Add(rt);
        //Debug.Log("Created new conversion buffer for "+width+" / "+height);
        return rt;
    }

    public RenderTexture painterRT_toBuffer(int width, int height)
    {
          

        bool square = (width == height);
        if ((!square) || (!Mathf.IsPowerOfTwo(width)))
        {
            RenderTexture rt = GetNonSquareBuffer(width, height);
            rtcam.targetTexture = rt;
            PrepareFullCopyBrush(BigRT_pair[0]);

            Render();
            return rt;
        }
        else
        {

            int tmpWidth = Mathf.Max(renderTextureSize / 2, width);
            RenderTexture from = GetSquareBuffer(tmpWidth);
            rtcam.targetTexture = from;
            PrepareFullCopyBrush(BigRT_pair[0]);
            brushRendy.Set(bufferCopy);

            Render();

            while (tmpWidth > width)
            {
                tmpWidth /= 2;
                RenderTexture to = GetSquareBuffer(tmpWidth);
                rtcam.targetTexture = to;
                PrepareFullCopyBrush(from);
                brushRendy.Set(bufferCopy);

                Render();
                from = to;
            }

            return from;

        }

    }

    // Assign some quad to this to see second buffer on it
    public MeshRenderer secondBufferDebug;

    // This are used in case user started to use Render Texture on another target. Data is moved to previous Material's Texture2D so that Render Texture Buffer can be used with new target. 
    [NonSerialized]
    public imgData bufferTarget;
    [NonSerialized]
    public Material materialTarget;
    public string parameterTarget;
    public PlaytimePainter painterTarget;
    public PlaytimePainter autodisabledBufferTarget;

    public void UpdateBuffersState() {

        PainterConfig cfg = PainterConfig.inst;

        rtcam.cullingMask = 1 << myLayer;

            //if ((cfg.dontCreateDefaultRenderTexture == false) && 
            if (!GotBuffers())
        {
           // Debug.Log("Initing buffers");
            BigRT_pair = new RenderTexture[2];
            BigRT_pair[0] = new RenderTexture(renderTextureSize, renderTextureSize, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default);
            BigRT_pair[1] = new RenderTexture(renderTextureSize, renderTextureSize, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default);
            BigRT_pair[0].wrapMode = TextureWrapMode.Repeat;
            BigRT_pair[1].wrapMode = TextureWrapMode.Repeat;

        }
       /* else if ((cfg.dontCreateDefaultRenderTexture) && GotBuffers())
        {
			EmptyBufferTarget ();

            DestroyImmediate (BigRT_pair[0]);
            DestroyImmediate (BigRT_pair[1]);
            BigRT_pair = null;
        }*/

      /*  if (cfg.dontCreateDefaultRenderTexture == false)
        {*/
            
            if (secondBufferDebug != null)
            {
                secondBufferDebug.sharedMaterial.mainTexture = BigRT_pair[1];
                if (secondBufferDebug.GetComponent<PlaytimePainter>() != null)
                    DestroyImmediate (secondBufferDebug.GetComponent<PlaytimePainter>());
            }
       // }


        if (Camera.main != null)
            Camera.main.cullingMask &= ~(1 << myLayer);
    }

    public static bool GotBuffers() {
        return ((inst.BigRT_pair != null) && (_inst.BigRT_pair.Length > 0) && (_inst.BigRT_pair[0] != null));
    }

	public void EmptyBufferTarget(){
        //Debug.Log("Setting Empty Buffer");

        if (bufferTarget == null)
			return;

        //Debug.Log("Target not null");

        if ((bufferTarget.texture2D != null) && (Application.isPlaying == false))
        {
           // Debug.Log("Texture not null");
            bufferTarget.RenderTexture_To_Texture2D(bufferTarget.texture2D);
        }

      //  if (materialTarget == null)
        //    Debug.Log("No material target");

		bufferTarget.destination = texTarget.Texture2D;
        if (materialTarget != null)
        {
          //  Debug.Log("Setting previous texture");
            materialTarget.SetTexture(parameterTarget, bufferTarget.currentTexture());
        }

		bufferTarget = null;
	}

    public void changeBufferTarget(imgData newTarget, Material mat, string parameter, PlaytimePainter painter)
    {
        if (bufferTarget != newTarget) {

            if (painterTarget != null) 
                    painterTarget.SetOriginalShader();
                  
                       
            

			if (bufferTarget != null) {
				if (bufferTarget.texture2D != null)
					bufferTarget.RenderTexture_To_Texture2D (bufferTarget.texture2D);

				bufferTarget.destination = texTarget.Texture2D;
                if (painterTarget != null)
                    painterTarget.setTextureOnMaterial(parameterTarget, bufferTarget.texture2D, materialTarget);
                materialTarget.SetTexture (parameterTarget, bufferTarget.texture2D);
			}

                materialTarget = mat;
                parameterTarget = parameter;
                painterTarget = painter;
                autodisabledBufferTarget = null;
                bufferTarget = newTarget;
        }
       
    }

    // ******************* Brush Shader MGMT

    public static void Shader_Pos_Update(Vector2 uv, Vector3 pos, bool hidePreview, float size) {

        if ((hidePreview) && (previewAlpha == 0)) return;

        previewAlpha = Mathf.Lerp(previewAlpha, hidePreview ? 0 : 1, 0.1f);

        Shader.SetGlobalVector("_brushPointedUV", new Vector4(uv.x, uv.y, 0, previewAlpha));

        Shader.SetGlobalVector("_brushWorldPosFrom", new Vector4(prevPosPreview.x, prevPosPreview.y, prevPosPreview.z, size));
        Shader.SetGlobalVector("_brushWorldPosTo", new Vector4(pos.x, pos.y, pos.z, (pos - prevPosPreview).magnitude));
        prevPosPreview = pos;
    }

    public void Shader_BrushCFG_Update(BrushConfig brush, float brushAlpha, float textureWidth, bool RendTex) {

		BrushType brushType = brush.currentBrushTypeRT();

		BlitMode blitMode = brush.currentBlitMode ();

		bool is3Dbrush =  (RendTex) && (brushType.isA3Dbrush);
		bool isDecal =  (RendTex) && (brushType.isUsingDecals);

		Color c = brush.color.ToColor();

#if UNITY_EDITOR
        if (PlayerSettings.colorSpace == ColorSpace.Linear) c *= c;
#endif

        Shader.SetGlobalVector("_brushColor", c);
        
        Shader.SetGlobalVector("_brushMask", new Vector4(
            brush.GetMask(BrushMask.R) ? 1 : 0,
            brush.GetMask(BrushMask.G) ? 1 : 0,
            brush.GetMask(BrushMask.B) ? 1 : 0,
            brush.GetMask(BrushMask.A) ? 1 : 0));

        if (isDecal) {
            VolumetricDecal vd = GetDecal(brush.selectedDecal);
            if (vd != null) {
                Shader.SetGlobalTexture("_VolDecalHeight", vd.heightMap);
                Shader.SetGlobalTexture("_VolDecalOverlay", vd.overlay);
                Shader.SetGlobalVector("_DecalParameters", new Vector4(brush.decalAngle*Mathf.Deg2Rad, (vd.type == VolumetricDecalType.Add) ? 1 : -1, 
                        Mathf.Clamp01(brush.speed/10f), 0));
            }
        }

        int ind = brush.selectedSourceMask;
		Shader.SetGlobalTexture("_SourceMask", ((ind < masks.Length) && (ind >= 0) && (brush.useMask) && (RendTex)) ? masks[ind] : null);

        Shader.SetGlobalVector("_maskDynamics", new Vector4(
            brush.maskTiling,
            RendTex ? brush.Hardness : 0,       // y - Hardness is 0 to do correct preview for Texture2D brush 
            (brush.flipMaskAlpha ? 0 : 1)
            , 0));
        
            Shader.SetGlobalVector("_maskOffset", new Vector4(
                brush.maskOffset.x,
                brush.maskOffset.y,
                0,
                0));

			Shader.SetGlobalVector("_brushForm", new Vector4(
				brushAlpha // x - transparency
				, brush.Size(is3Dbrush) // y - scale for sphere
				, brush.Size(is3Dbrush) / textureWidth // z - scale for uv space
				, brush.blurAmount)); // w - blur amount


            if (RendTex)
                brushType.setKeyword();
            else
                BrushTypeNormal.inst.setKeyword();


        blitMode.setKeyword ();
		blitMode.SetGlobalShaderParameters ();

		BlitModeExtensions.SetShaderToggle (PainterConfig.inst.previewAlphaChanel, "PREVIEW_ALPHA", "PREVIEW_RGB");

		BlitModeExtensions.SetShaderToggle ((brush.Smooth || RendTex), "PREVIEW_FILTER_SMOOTH", PainterConfig.UV_PIXELATED);
		
		

		if ((RendTex) && (blitMode.usingSourceTexture))
			Shader.SetGlobalTexture ("_SourceTexture", getSourceTexture (brush.selectedSourceTexture));
		
    }

    public void ShaderPrepareStroke(BrushConfig bc, float brushAlpha, imgData id) {

		BlitMode blitMode = bc.currentBlitMode ();

		bool isDoubleBuffer = (id.renderTexture == null);

		Shader_BrushCFG_Update (bc, brushAlpha, id.width, id.TargetIsRenderTexture());

		ProvideCameraPositionToShader ();

		rtcam.targetTexture = id.currentRenderTexture ();

		if (isDoubleBuffer)
				Shader.SetGlobalTexture ("_DestBuffer", (isDoubleBuffer ? (Texture)BigRT_pair[1] : (Texture)id.texture2D));

		brushRendy.Set (isDoubleBuffer ? blitMode.shaderForDoubleBuffer : blitMode.shaderForSingleBuffer);

	}
			

    public void ProvideCameraPositionToShader()
    {
        transform.rotation = Quaternion.identity;
        Shader.SetGlobalVector("_RTcamPosition", transform.position);
    }

    // **************************   Rendering calls

    public Vector2 uvToPosition(Vector2 uv) {
        return (uv - Vector2.one * 0.5f) * orthoSize * 2;

			//Vector2 meshPos = ((st.uvFrom + st.uvTo) - Vector2.one) * rtp.orthoSize;
    }



    public Vector2 to01space(Vector2 from) {
        from.x %= 1;
        from.y %= 1;
        if (from.x < 0) from.x += 1;
        if (from.y < 0) from.y += 1;
        return from;
    }

    //Vector2 previousTo = Vector2.zero;
    //Vector2 previousDir = Vector2.zero;
    //Vector3 hitPosPrevious;

        public void Render()  {
            //Debug.Log("Rendering to "+rtcam.targetTexture);
            rtcam.Render();
        }

    public void PrepareFullCopyBrush(Texture source) {

        ProvideCameraPositionToShader();
        RenderTexture targ = rtcam.targetTexture;
		brushRendy.PrepareForFullCopyOf (source);
			/*
		float aspectRatio = (float)targ.width / (float)targ.height;
        brushRendy.transform.localScale = new Vector3(size * aspectRatio, size, 0);
        brushRendy.transform.localPosition = Vector3.forward * 10;
        brushRendy.transform.localRotation = Quaternion.identity;
        brushRendy.SetShader(pixPerfectCopy);
        brushRendy.SetTexture(source);
        brushRendy.meshFilter.mesh = brushMeshGenerator.inst().GetQuad();*/
    }
			

    public void Render(Texture tex, imgData id) {
            if (tex == null)
                return;
        rtcam.targetTexture = id.currentRenderTexture();
		PrepareFullCopyBrush(tex);
		Render_UpdateSecondBufferIfUsing(id);
	}

	public void Render(Texture from, RenderTexture to) {
            if (from == null) return;
			rtcam.targetTexture = to;
            PrepareFullCopyBrush(from);
            Render();
        }

    public void Render_UpdateSecondBufferIfUsing(imgData id) {
        Render();

        if ((GotBuffers()) && (id.currentRenderTexture() == BigRT_pair[0]))
            UpdateBufferTwo();
    }


    public void UpdateBufferTwo() {
		if (!DebugDisableSecondBufferUpdate) {
			PrepareFullCopyBrush (BigRT_pair [0]);
			rtcam.targetTexture = BigRT_pair [1];
			brushRendy.Set (bufferCopy);
			rtcam.Render ();
		}
    }


    // *******************  Component MGMT

    void PlayModeStateChanged() {
        PainterConfig.SaveChanges();
        autodisabledBufferTarget = null;

    }

    private void OnEnable() {


            _inst = this;

            if (meshManager == null)
                meshManager = new MeshManager();

            if (painterCfg == null)
                painterCfg = PainterConfig.inst;
            else painterCfg.SafeInit();

            meshManager.OnEnable();


        rtcam.cullingMask = 1 << myLayer;

			if (atlases == null)
				atlases = new List<AtlasTextureCreator> ();

			if (atlasedMaterials == null)
				atlasedMaterials = new List<MaterialAtlases> ();


		if (PlaytimeToolComponent.enabledTool == null)
        {
            if (!Application.isEditor)
            {
#if BUILD_WITH_PAINTER
				PlaytimeToolComponent.enabledTool = typeof(PlaytimePainter);
#else
                    PlaytimeToolComponent.GetPrefs();
#endif
            }
            else
				PlaytimeToolComponent.GetPrefs();
        }
#if UNITY_EDITOR

            EditorSceneManager.sceneSaving -= BeforeSceneSaved;
            EditorSceneManager.sceneSaving += BeforeSceneSaved;
           // Debug.Log("Adding scene saving delegate");
            EditorSceneManager.sceneOpening -= OnSceneOpening;
            EditorSceneManager.sceneOpening += OnSceneOpening;

          

        

                if (defaultMaterial == null)
			defaultMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");

		if (defaultMaterial == null) Debug.Log("Default Material not found.");

		isLinearColorSpace = UnityEditor.PlayerSettings.colorSpace == ColorSpace.Linear;

        EditorApplication.update -= combinedUpdate;
        if (!this.ApplicationIsAboutToEnterPlayMode())
            EditorApplication.update += combinedUpdate;

        EditorApplication.playmodeStateChanged -= PlayModeStateChanged; // painterConfig.SaveChanges;
        EditorApplication.playmodeStateChanged += PlayModeStateChanged; // painterConfig.SaveChanges;

        if (brushPrefab == null) {
            //string assetName = "Assets/" + painterConfig.ToolPath() + "/" + painterConfig.DependenciesFolder + "/prefabs/RenderCameraBrush.prefab";
            GameObject go = Resources.Load("prefabs/RenderCameraBrush") as GameObject; //(GameObject)AssetDatabase.LoadAssetAtPath(assetName, typeof(GameObject));
            brushPrefab = go.GetComponent<RenderBrush>();
            if (brushPrefab == null)
            {
                Debug.Log("Couldn't find brush prefab.");
            }
        }

        UnityHelperFunctions.RenamingLayer(myLayer, "Painter Layer");

#endif

        if (brushRendy == null)
        {
            brushRendy = GetComponentInChildren<RenderBrush>();
            if (brushRendy == null)
            {
                brushRendy = Instantiate(brushPrefab.gameObject).GetComponent<RenderBrush>();
                brushRendy.transform.parent = this.transform;
            }
        }
        brushRendy.gameObject.layer = myLayer;



#if BUILD_WITH_PAINTER || UNITY_EDITOR
        if (sourceTextures == null) sourceTextures = new Texture[0];
        if (masks == null) masks = new Texture[0];
        
		if (pixPerfectCopy == null) pixPerfectCopy = Shader.Find("Editor/PixPerfectCopy");
        
		if (bufferCopy == null) bufferCopy = Shader.Find("Editor/BufferCopier");

        if (br_Blit == null) br_Blit = Shader.Find("Editor/br_Blit");

        if (br_Add == null) br_Add = Shader.Find("Editor/br_Add");

        if (br_Copy == null) br_Copy = Shader.Find("Editor/br_Copy");

        if (br_Multishade == null) br_Multishade = Shader.Find("Editor/br_Multishade");

		if (br_BlurN_SmudgeBrush == null) br_BlurN_SmudgeBrush = Shader.Find("Editor/BlurN_SmudgeBrush");

        if (br_Preview == null) br_Preview = Shader.Find("Editor/br_Preview");
        
        if (mesh_Preview == null) mesh_Preview = Shader.Find("Editor/MeshEditorAssist");

            //if (br_TerrainPreview == null) 
            TerrainPreview = Shader.Find("Editor/TerrainPreview");

        transform.position = Vector3.up * 3000;
        if (rtcam == null)
        {
            rtcam = GetComponent<Camera>();
            if (rtcam == null)
                rtcam = gameObject.AddComponent<Camera>();
        }

        rtcam.orthographic = true;
        rtcam.orthographicSize = orthoSize;
        rtcam.clearFlags = CameraClearFlags.Nothing;
        rtcam.enabled = false;

#if UNITY_EDITOR
        EditorApplication.update -= combinedUpdate;
        if (EditorApplication.isPlayingOrWillChangePlaymode == false)
            EditorApplication.update += combinedUpdate;
#endif

        UpdateBuffersState();

#endif

        if ((autodisabledBufferTarget != null) && (!autodisabledBufferTarget.LockEditing) && (!this.ApplicationIsAboutToEnterPlayMode())) 
            autodisabledBufferTarget.reanableRenderTexture();
    
        autodisabledBufferTarget = null;

    }

    private void OnDisable() {
		if (PlaytimePainter.isCurrent_Tool())
			PlaytimeToolComponent.SetPrefs();

		recentTextures.RemoveEmpty();
#if UNITY_EDITOR
        TakeAwayRT();
#endif
            
#if UNITY_EDITOR
            EditorApplication.update -= meshManager.editingUpdate;
#endif


        }

#if UNITY_EDITOR

        void TakeAwayRT() {

            PlaytimePainter p = PlaytimePainter.PreviewShaderUser;
            if (p != null)
                p.SetOriginalShader();

            PainterConfig.SaveChanges();
            autodisabledBufferTarget = painterTarget;
            EmptyBufferTarget();

        }

        public  void OnSceneOpening(string path, OpenSceneMode mode) {
           // Debug.Log("On Scene Opening");
        }

        public void BeforeSceneSaved(UnityEngine.SceneManagement.Scene scene, string path) {
            //public delegate void SceneSavingCallback(Scene scene, string path);
           
            
            TakeAwayRT();
           // Debug.Log("Before Scene saved");

        }

    public void Update() {
        if (Application.isPlaying)
            combinedUpdate();

            meshManager.Update();
    }


    

    public void combinedUpdate() {

        List<PlaytimePainter> l = PlaytimePainter.playbackPainters;

            if ((l.Count > 0) && (!StrokeVector.PausePlayback)){
            if (l.last() == null) 
                l.RemoveLast(1);
            else 
                l.last().PlaybeckVectors();
        }

        forCombinedMaps.UpdateBumpGloss();

 

		PlaytimeToolComponent.CheckRefocus();
        if ((PainterConfig.inst.disableNonMeshColliderInPlayMode) && (Application.isPlaying)) {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit)) {
                Collider c = hit.collider;
				if ((c.GetType() != typeof(MeshCollider)) && (PlaytimeToolComponent.PainterCanEditWithTag(c.tag))) c.enabled = false;
            }
        }
			PlaytimePainter p = PlaytimePainter.currently_Painted_Object;

			if ((p != null) && (Application.isPlaying == false)) {
				if ((p.curImgData == null)) {
					PlaytimePainter.currently_Painted_Object = null;
            }
            else {	
				p.Paint(p.stroke, PainterConfig.inst.brushConfig);
                p.Update();
            }
        }

    }
#endif

        public void CancelAllPlaybacks()
        {
            foreach (var p in PlaytimePainter.playbackPainters)
                p.playbackVectors.Clear();

            PlaytimePainter.cody = new StoryTriggerData.stdDecoder(null);
        }

        void OnApplicationQuit()
    {

#if !UNITY_EDITOR && BUILD_WITH_PAINTER
            painterConfig.SaveChanges();
#endif
        }


       // bool meshEditorConfig = false;
        public void PEGI() {

            (((BigRT_pair == null) || (BigRT_pair.Length == 0)) ? "No buffers" : "Using HDR buffers " + ((BigRT_pair[0] == null) ? "uninitialized" : "inited")).nl();

            if (rtcam == null) { "no camera".nl(); return; }

#if UNITY_EDITOR
            "Using layer:".nl();
            myLayer = EditorGUILayout.LayerField(myLayer);
#endif
            pegi.newLine();
            "Disable Second Buffer Update (Debug Mode)".toggle(ref DebugDisableSecondBufferUpdate).nl();

            "testValue: ".edit(() => testString).nl();

            //serializedObject.Update();

            "Textures to copy from".edit(() => sourceTextures).nl();
            "Masks".edit(() => masks).nl();
            "Decals".edit(() => decals).nl();
            "For combined maps".edit(() => forCombinedMaps).nl();

         //   if ("Mesh Painter".foldout(ref meshEditorConfig).nl()) 
           //     meshManager.PEGI();

          

        }

}
}