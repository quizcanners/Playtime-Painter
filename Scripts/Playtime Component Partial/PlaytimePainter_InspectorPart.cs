using PlayerAndEditorGUI;
using PlaytimePainter.CameraModules;
using PlaytimePainter.MeshEditing;
using QuizCannersUtilities;
using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif
using UnityEngine;
using UnityEngine.UI;

namespace PlaytimePainter
{

#pragma warning disable IDE0018 // Inline variable declaration

    [DisallowMultipleComponent]
    [AddComponentMenu("Mesh/Playtime Painter")]
    [HelpURL(OnlineManual)]
    public partial class PlaytimePainter : IPEGI
    {
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
                    pegi.EditorView.Lock_UnlockClick(gameObject);

                MsgPainter.PleaseSelect.GetText().writeHint();

                TrySetOriginalTexture();

                SetOriginalShaderOnThis();

                return false;
            }
#endif

            if (currentlyPaintedObjectPainter)
                pegi.EditorView.RefocusIfLocked(this, currentlyPaintedObjectPainter);
#if UNITY_EDITOR
            else if (Selection.objects.Length == 1)
            {
                var go = Selection.objects[0] as GameObject;
                if (go && go != gameObject)
                    pegi.EditorView.RefocusIfLocked(this, go.GetComponent<PlaytimePainter>());
            }
#endif

            inspected = this;

            var changed = false;

            if (!TexMgmt && "Find camera".Click())
                PainterClass.applicationIsQuitting = false;

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

                pegi.EditorView.Lock_UnlockClick(gameObject);

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
                else if (sinceUpdate > 100)
                {
                    "It's been {0} seconds since the last managed update".F(sinceUpdate).writeWarning();

                    if ("Resubscribe camera to updates".Click())
                        TexMgmt.SubscribeToEditorUpdates();
                    
                    return changed;
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

                pegi.EditorView.Lock_UnlockClick(gameObject);

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

                        if (SavedEditableMesh.IsEmpty == false)
                            MeshMgmt.EditMesh(this, false);
                    }
                }


                pegi.toggle(ref cfg.showConfig, meshEditing ? icon.Mesh : icon.Painter, icon.Config,
                    "Tool Configuration");

                if (!PainterDataAndConfig.hideDocumentation)
                    pegi.FullWindowService.DocumentationClickOpen(LazyLocalization.InspectPainterDocumentation,
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

                            pegi.FullWindowService.WarningDocumentationClickOpen(
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
                                    if (SavedEditableMesh.IsEmpty == false)
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
                                                !SavedEditableMesh.IsEmpty
                                                    ? "This will erase existing editable mesh. Proceed?"
                                                    : "Create a mesh?"))
                                            {
                                                Mesh = new Mesh();
                                                SavedEditableMesh = new CfgData();
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
                                                "Mesh profile not found, using default one".writeWarning();

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

                                    if (icon.Add.Click(25))
                                    {
                                        var sol = new MeshPackagingProfile();
                                        cfg.meshPackagingSolutions.Add(sol);
                                        selectedMeshProfile = sol.name;
                                        //MeshProfile.name = "New Profile {0}".F(selectedMeshProfile);
                                    }

                                    if (icon.Refresh.Click("Refresh Mesh Packaging Solutions"))
                                        PainterCamera.Data.ResetMeshPackagingProfiles();

                                    pegi.nl();
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
                                            pegi.FullWindowService.DocumentationClick("Why Update Collider from skinned mesh?"))
                                            pegi.FullWindowService.DocumentationOpen(
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

                                    texMeta.FillWithColor(texMeta.clearColor);

                                    //texMeta.SetPixels(texMeta.clearColor);
                                    //texMeta.SetApplyUpdateRenderTexture();
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
                                {
                                    if (cfg.colorSchemes.Count == 0)
                                        cfg.colorSchemes.Add(new ColorScheme { paletteName = "New Color Scheme" });

                                    pegi.edit_List(ref cfg.colorSchemes, ref cfg.inspectedColorScheme);
                                }

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
                            if (IsUiGraphicPainter)
                            {

                                var uiImage = uiGraphic as Image;
                                if (uiImage && !uiImage.sprite)
                                {
                                    pegi.nl();
                                    "Sprite is null. Convert image to sprite and assign it".writeWarning();
                                    pegi.nl();
                                }

                                if (texMeta.updateTex2DafterStroke == false && texMeta.TargetIsRenderTexture())
                                {
                                    "Update Original Texture after every Stroke"
                                        .toggleIcon(ref texMeta.updateTex2DafterStroke).nl();
                                }

                            }

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
                                    _nameHolder =  "New {0}_{1}".F(gameObject.name, GetMaterialTextureProperty);
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
                                    "Name (New Texture):".edit("Name for new texture", 120, ref _nameHolder).nl();

                            }

                            pegi.nl();

                            if (!tex) 
                                "No texture. Drag and drop or click on the plus icon to create New".writeHint();

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

            PainterClass.applicationIsQuitting = false;
        }

        [MenuItem("Tools/" + PainterDataAndConfig.ToolName + "/Join Discord")]
        public static void Open_Discord() => Application.OpenURL(pegi.FullWindowService.DiscordServer);

        [MenuItem("Tools/" + PainterDataAndConfig.ToolName + "/Send an Email")]
        public static void Open_Email() => QcUnity.SendEmail(pegi.FullWindowService.SupportEmail,
            "About your Playtime Painter",
            "Hello Yuri, we need to talk. I purchased your asset and expect an excellent quality, but ...");

        [MenuItem("Tools/" + PainterDataAndConfig.ToolName + "/Open Manual")]
        public static void OpenWWW_Documentation() => Application.OpenURL(OnlineManual);

#endif
    }
}
