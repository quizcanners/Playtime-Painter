using QuizCanners.Inspect;
using PainterTool.CameraModules;
using PainterTool.MeshEditing;
using QuizCanners.Migration;
using QuizCanners.Utils;
using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using  UnityEditor;
using UnityEditorInternal;
#endif
using UnityEngine;
using UnityEngine.UI;

namespace PainterTool
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Playtime Painter/Playtime Painter Tool")]
    [HelpURL(OnlineManual)]
    public partial class PainterComponent : IPEGI
    {
        private string _nameHolder = "unnamed";
        private static string _tmpUrl = "";
        internal static PainterComponent selectedInPlaytime;

        internal static PainterComponent inspected;

        [NonSerialized] public readonly Dictionary<int, ShaderProperty.TextureValue> loadingOrder = new();


        public static pegi.EnterExitContext constet = new(); //int s_inspectedMeshEditorItems = -1;
        internal static bool s_showTextureOptions;

        private void Inspect_LockUnlock() 
        {
#if UNITY_2019_1_OR_NEWER && UNITY_EDITOR
            if (!Application.isPlaying && !IsCurrentTool)
            {
                if (ActiveEditorTracker.sharedTracker.isLocked)
                    pegi.EditorView.Lock_UnlockClick(gameObject);

                MsgPainter.PleaseSelect.GetText().PegiLabel().Write_Hint();

                TrySetOriginalTexture();

                SetOriginalShaderOnThis();
            }
#endif
        }

        private void Inspect_MeshPart() 
        {
            var cfg = Painter.Data;

            var mg = Painter.MeshManager;
            mg.UndoRedoInspect(); pegi.Nl();

            var sharedMesh = SharedMesh;

            if (sharedMesh)
            {
                if (this != MeshPainting.target)
                    if (SavedEditableMesh.IsEmpty == false)
                        "Component has saved mesh data.".PegiLabel().Nl();

                "Warning, this will change (or mess up) your model.".PegiLabel().WriteOneTimeHint("MessUpMesh");

                if (MeshPainting.target != this)
                {

                    var ent = gameObject.GetComponent("pb_Entity");
                    var obj = gameObject.GetComponent("pb_Object");

                    if (ent || obj)
                        "PRO builder detected. Strip it using Actions in the Tools/ProBuilder menu.".PegiLabel()
                            .Write_Hint();
                    else
                    {
                        if (Application.isPlaying)
                            "Playtime Changes will be reverted once you try to edit the mesh again.".PegiLabel()
                                .WriteWarning();

                        pegi.Nl();

                        "Mesh has {0} vertices".F(sharedMesh.vertexCount).PegiLabel().Nl();

                        pegi.Nl();

#if UNITY_EDITOR
                        if ("Generate UV2".PegiLabel().Click().Nl())
                            if (Unwrapping.GenerateSecondaryUVSet(sharedMesh) == false)
                                Debug.LogError("UV2 generation failed");
#endif

                        pegi.Nl();

                        const string confirmTag = "pp_EditThisMesh";

                        if (!pegi.ConfirmationDialogue.IsRequestedFor(confirmTag))
                        {

                            if ("New Mesh".PegiLabel(!SavedEditableMesh.IsEmpty
                                    ? "This will erase existing editable mesh. Proceed?"
                                    : "Create a mesh?").ClickConfirm("newMesh"))
                            {
                                Mesh = new Mesh();
                                SavedEditableMesh = new CfgData();
                                mg.EditMesh(this, false);
                            }
                        }

                        if (SharedMesh && SharedMesh.vertexCount == 0)
                        {
                            pegi.Nl();
                            "Shared Mesh has no vertices, you may want to create a new mesh".PegiLabel()
                                .Write_Hint();
                        }
                        else
                        {
                            if (!pegi.ConfirmationDialogue.IsRequestedFor(confirmTag) && "Copy & Edit".PegiLabel().Click())
                                mg.EditMesh(this, true);

                            if ("Edit this".PegiLabel("Are you sure you want to edit the original one instead of editing a copy(safer)? " +
                                    "Playtime Painter's Undo functionality can be limited.").ClickConfirm(confirmTag).Nl())
                                mg.EditMesh(this, false);
                        }
                    }
                }
            }
            else
            {
                pegi.Edit_IfNull(ref meshFilter, gameObject).Nl();

                pegi.Edit_IfNull(ref meshRenderer, gameObject).Nl();

                if (!sharedMesh && "Create Mesh".PegiLabel().Click())
                    Mesh = new Mesh();

            }

            if (IsEditingThisMesh)
            {
                using (constet.StartContext())
                {
                    if (!constet.IsAnyEntered)
                    {
                        Painter.MeshManager.Nested_Inspect();
                        pegi.Nl();
                    }

                    pegi.Space();

                    pegi.Line();

                    if ("Profile".PegiLabel().IsEntered())
                    {

                        MsgPainter.MeshProfileUsage.DocumentationClick();


                        if ((cfg.meshPackagingSolutions.Count > 1) && Icon.Delete.Click(25))
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

                            pegi.Nl();
                            var mpf = MeshProfile;
                            if (mpf == null)
                                "There are no Mesh packaging profiles in the PainterDataObject".PegiLabel()
                                    .WriteWarning();
                            else
                            {
                                if (!mpf.name.Equals(selectedMeshProfile))
                                    "Mesh profile not found, using default one".PegiLabel().WriteWarning();

                                pegi.Nl();


                                if (mpf.Nested_Inspect().Nl())
                                {
                                    selectedMeshProfile = mpf.name;
                                    MeshEditorManager.editedMesh.Dirty = true;
                                }
                            }

                        }
                    }
                    
                    if (!constet.IsAnyEntered)
                    {
                        if (pegi.Select_iGotName(ref selectedMeshProfile,
                                cfg.meshPackagingSolutions) &&
                            IsEditingThisMesh)
                            MeshEditorManager.editedMesh.Dirty = true;

                        if (Icon.Add.Click(25))
                        {
                            var sol = new MeshPackagingProfile();
                            cfg.meshPackagingSolutions.Add(sol);
                            selectedMeshProfile = sol.name;
                            //MeshProfile.name = "New Profile {0}".F(selectedMeshProfile);
                        }

                        if (Icon.Refresh.Click("Refresh Mesh Packaging Solutions"))
                            Painter.Data.ResetMeshPackagingProfiles();

                        pegi.Nl();
                    }

                    Painter.MeshManager.MeshOptionsInspect(context);
                }
            }
            

            pegi.Nl();
        }

        [SerializeField] private pegi.EnterExitContext context = new();

        private void Inspect_TexturePart() 
        {
            var cfg = Painter.Data;
            var texMgmt = Painter.Camera;
            var texMeta = TexMeta;
            var tex = GetTextureOnMaterial();

            if (!meshEditing && ((tex && texMeta == null) || (texMeta != null && !tex) ||
                                 (texMeta != null && tex != texMeta.Texture2D && tex != texMeta.CurrentTexture())))
                textureWasChanged = true;

            var painterNotUiOrPlaying = Application.isPlaying || !IsUiGraphicPainter;

            if (!TextureEditingBlocked && painterNotUiOrPlaying && !texMeta.errorWhileReading)
            {
                if (texMeta.ProcessEnumerator != null)
                {
                    if (!s_showTextureOptions)
                    {
                        pegi.Nl();
                        "Processing Texture".PegiLabel().Nl();
                        texMeta.ProcessEnumerator.InspectInList_Nested().Nl();
                    }
                }
                else
                {

                    texMgmt.DependenciesInspect();

                    #region Undo/Redo & Recording

                   
                    texMeta.Undo_redo_PEGI();

                    pegi.Nl();

                    var cpu = texMeta.TargetIsTexture2D();

                    var mat = Material;
                    if (mat.IsProjected())
                    {
                        "Projected UV Shader detected. Painting may not work properly".PegiLabel().WriteWarning();

                        if ("Undo".PegiLabel().Click().Nl())
                            mat.DisableKeyword(PainterShaderVariables.UV_PROJECTED);
                    }

                    #endregion

                    #region Brush

                    if (!s_showTextureOptions)
                    {

                        pegi.Nl();

                        if (skinnedMeshRenderer)
                        {
                            if ("Update Collider from Skinned Mesh".PegiLabel().Click())
                                UpdateMeshCollider();

                            if (!SO_PainterDataAndConfig.hideDocumentation)
                                pegi.FullWindow.DocumentationClickOpen(
                                    text: () => "To paint an object a collision detection is needed. Mesh Collider is not being animated. To paint it, update Mesh Collider with Update Collider button." +
                                     " For ingame painting it is preferable to use simple colliders like Speheres to avoid per frame updates for collider mesh.",
                                     toolTip: "Why Update Collider from skinned mesh?");


                            pegi.Nl();
                        }

                        var blocker = GetPaintingBlocker();

                        if (!blocker.IsNullOrEmpty())
                        {
                            "Can't paint because {0}".F(blocker).PegiLabel().WriteWarning();
                        }
                        else
                        {
                            if (texMeta != null)
                                Inspect_PreviewShaderToggle();

                            //if (!PainterCamera.GotBuffers && icon.Refresh.Click("Refresh Main Camera Buffers"))
                            //  RenderTextureBuffersManager.RefreshPaintingBuffers();


                            GlobalBrush.Nested_Inspect(fromNewLine: false);

                            if (!cpu && texMeta.Texture2D && texMeta.Width != texMeta.Height)
                                Icon.Warning.Draw(
                                    "Non-square texture detected! Every switch between GPU and CPU mode will result in loss of quality.");

                            var mode = GlobalBrush.GetBlitMode(cpu ? TexTarget.Texture2D : TexTarget.RenderTexture);
                            var col = GlobalBrush.Color;

                            if ((cpu || !mode.UsingSourceTexture || GlobalBrush.srcColorUsage !=
                                 Brush.SourceTextureColorUsage.Unchanged)
                                 && !pegi.PaintingGameViewUI)
                            {
                                if (pegi.Edit(ref col))
                                    GlobalBrush.Color = col;

                                MsgPainter.SampleColor.DocumentationClick();

                            }

                            pegi.Nl();

                            GlobalBrush.ColorSliders();
                            pegi.Nl();

                            if (cfg.showColorSchemes)
                            {

                                var scheme = cfg.colorSchemes.TryGet(cfg.selectedColorScheme);

                                scheme?.PickerPEGI();

                                if (cfg.showColorSchemes)
                                    "Scheme".PegiLabel(60).Select_Index(ref cfg.selectedColorScheme, cfg.colorSchemes)
                                        .Nl();

                            }
                        }
                    }

                    #endregion
                }
            }
            else
            {
                if (IsUsingPreview)
                    Inspect_PreviewShaderToggle();

                if (!painterNotUiOrPlaying)
                {
                    pegi.Nl();
                    "UI Element editing only works in Game View during Play.".PegiLabel().WriteWarning();
                }
            }

            texMeta = TexMeta;

            Inspect_ConvexMeshCheckWarning();

            Inspect_Texture_Options();

            #region Save Load Options

            if (!s_showTextureOptions)
            {
                #region Material Clonning Options

                pegi.Nl();

                var mats = Materials;
                if (!mats.IsNullOrEmpty())
                {
                    var sm = selectedSubMesh;
                    if (pegi.Select_Index(ref sm, mats))
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
                    pegi.ClickHighlight(mater.shader, "Highlight Shader");

                if (pegi.Edit(ref mater))
                    Material = mater;

                if (Icon.NewMaterial.Click("Instantiate Material").Nl())
                    InstantiateMaterial(true);

                pegi.Nl();
                pegi.Space();
                pegi.Nl();

                #endregion

                #region Texture 

                if (cfg.showUrlField)
                {

                    "URL".PegiLabel(40).Edit(ref _tmpUrl);
                    if (_tmpUrl.Length > 5 && Icon.Download.Click())
                    {
                        loadingOrder.Add(Painter.DownloadManager.StartDownload(_tmpUrl),
                            GetMaterialTextureProperty());
                        _tmpUrl = "";
                        pegi.GameView.ShowNotification("Loading for {0}".F(GetMaterialTextureProperty()));
                    }

                    pegi.Nl();
                    if (loadingOrder.Count > 0)
                        "Loading {0} texture{1}".F(loadingOrder.Count, loadingOrder.Count > 1 ? "s" : "").PegiLabel()
                            .Nl();

                    pegi.Nl();
                }


                var ind = SelectedTexture;
                if (pegi.Select_Index(ref ind, GetAllTextureNames()))
                {
                    SetOriginalShaderOnThis();
                    SelectedTexture = ind;
                    OnChangedTexture_OnMaterial();
                    CheckPreviewShader();
                    texMeta = TexMeta;
                    if (texMeta == null)
                        _nameHolder = "New {0}_{1}".F(gameObject.name, GetMaterialTextureProperty());
                }

                if (texMeta != null)
                {
                    texMeta.InspectConvestionOptions(this);

                    UpdateTilingFromMaterial();

                    if (texMeta.errorWhileReading)
                    {

                        Icon.Warning.Draw(
                            "THere was error while reading texture. (ProBuilder's grid texture is not readable, some others may be to)");

                        if (texMeta.Texture2D && Icon.Refresh.Click("Retry reading the texture"))
                            texMeta.From(texMeta.Texture2D, true);

                    }

                }

                tex = GetTextureOnMaterial();
                if (pegi.Edit(ref tex))
                    ChangeTexture(tex);

                    var texScale =  cfg.SelectedWidthForNewTexture();

                    var texNames = GetAllTextureNames();

                    if (texNames.Count > SelectedTexture)
                    {
                        var param = GetMaterialTextureProperty();

                        const string newTexConfirmTag = "pp_nTex";

                        if (((texMeta == null) &&
                              Icon.NewTexture.Click("Create new texture2D for " + param)) ||
                             (texMeta != null && Icon.NewTexture.ClickConfirm(newTexConfirmTag, texMeta,
                                  "Replace " + param + " with new Texture2D " + texScale + "*" + texScale)))
                           
                        {
                            pegi.Nl();
                          CreateTexture2D(texScale, _nameHolder, cfg.newTextureIsColor);
                        }
                        pegi.Nl();

                        if (!pegi.ConfirmationDialogue.IsRequestedFor(newTexConfirmTag))
                        {

                            if (cfg.showRecentTextures)
                            {

                                var texName = GetMaterialTextureProperty();

                                if (texName != null &&
                                    Painter.Data.recentTextures.TryGetValue(texName,
                                        out List<TextureMeta> recentTexs) &&
                                    (recentTexs.Count > 0)
                                    && (texMeta == null || (recentTexs.Count > 1) ||
                                        (texMeta != recentTexs[0].Texture2D.GetImgDataIfExists()))
                                    && "Recent Textures:".PegiLabel(100).Select(ref texMeta, recentTexs)
                                        .Nl())
                                    ChangeTexture(texMeta.ExclusiveTexture());

                            }

                            if (texMeta == null && cfg.allowExclusiveRenderTextures &&
                                "Create Render Texture".PegiLabel().Click())
                                CreateRenderTexture(texScale, _nameHolder);

                            if (texMeta != null && cfg.allowExclusiveRenderTextures)
                            {
                                if (!texMeta.RenderTexture && "Add Render Tex".PegiLabel().Click())
                                    texMeta.AddRenderTexture();

                                if (texMeta.RenderTexture)
                                {

                                    if ("Replace RendTex".PegiLabel("Replace " + param + " with Rend Tex size: " + texScale).Click())
                                        CreateRenderTexture(texScale, _nameHolder);

                                    if ("Remove RendTex".PegiLabel().Click().Nl())
                                    {

                                        if (texMeta.Texture2D)
                                        {
                                            UpdateOrSetTexTarget(TexTarget.Texture2D);
                                            texMeta.RenderTexture = null;
                                        }
                                        else
                                            RemoveTextureFromMaterial();

                                    }
                                }
                            }

                        }

                    }
                    else
                        Icon.Warning.Nl("No Texture property selected");

                    pegi.Nl();

                    if (texMeta == null)
                        "Name (New Texture):".PegiLabel("Name for new texture", 120).Edit( ref _nameHolder).Nl();

                

                pegi.Nl();

                if (!tex)
                    "No texture. Drag and drop or click on the plus icon to create New".PegiLabel().Write_Hint();

                pegi.Nl();

                pegi.Space();
                pegi.Nl();

                #endregion

                #region Texture Saving/Loading

                if (!TextureEditingBlocked)
                {
                    pegi.Nl();
              
                        if (Application.isEditor)
                            "Unless saved, the texture will loose all changes when scene is offloaded or Unity Editor closed.".PegiLabel()
                                .WriteOneTimeHint("_pp_hint_saveTex");

                        pegi.Nl();

                        texMeta = TexMeta;

#if UNITY_EDITOR
                        string orig = null;
                        if (texMeta.Texture2D)
                        {
                            orig = AssetDatabase.GetAssetPath(TexMeta.Texture2D);

                            if (orig != null && Icon.Load.ClickUnFocus("Will reload " + orig))
                            {
                                ForceReimportMyTexture();
                                texMeta.saveName = texMeta.Texture2D.name;
                            }
                        }

                        pegi.Edit(ref texMeta.saveName);

                        if (texMeta.Texture2D)
                        {

                            if (!texMeta.saveName.SameAs(texMeta.Texture2D.name) &&
                                Icon.Refresh.Click(
                                    "Use current texture name ({0})".F(texMeta.Texture2D.name)))
                                texMeta.saveName = texMeta.Texture2D.name;

                            var destPath = GenerateTextureSavePath();
                            var existsAtDestination = TextureExistsAtDestinationPath();
                            var originalExists = !orig.IsNullOrEmpty();
                            var sameTarget = originalExists && orig.Equals(destPath);
                            var sameTextureName =
                                originalExists && texMeta.Texture2D.name.Equals(texMeta.saveName);

                            if (!existsAtDestination || sameTextureName)
                            {
                                if ((sameTextureName ? Icon.Save : Icon.SaveAsNew).Click(sameTextureName
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
                            else if (existsAtDestination && Icon.Save.Click("Will replace {0}".F(destPath)))
                                SaveTextureAsAsset(false);

                            if (!sameTarget && !sameTextureName && originalExists && !existsAtDestination &&
                                Icon.Replace.Click("Will replace {0} with {1} ".F(orig, destPath)))
                                RewriteOriginalTexture_Rename(texMeta.saveName);

                            pegi.Nl();

                        }
#endif
                    

                    pegi.Nl();
                }

                pegi.Nl();
                pegi.Space();
                pegi.Nl();

                #endregion
            }

            #endregion
        }

        private static readonly pegi.EnterExitContext _textureOptions = new();

        private void Inspect_Texture_Options() 
        {
            var cfg = Painter.Data;
            var texMgmt = Painter.Camera;
            var texMeta = TexMeta;
            var tex = GetTextureOnMaterial();


            pegi.Nl();
            MsgPainter.TextureSettings.GetText().PegiLabel().IsFoldout(ref s_showTextureOptions);

            if (texMeta != null && !s_showTextureOptions)
            {

                pegi.Edit(ref texMeta.clearColor, 50);
                if (texMeta.CurrentTexture() && Icon.Clear.Click("Clear channels which are not ignored"))
                {
                    if (GlobalBrush.PaintingAllChannels)
                    {
                        Painter.Camera.DiscardAlphaBuffer();

                        texMeta.FillWithColor(texMeta.clearColor);

                        //texMeta.SetPixels(texMeta.clearColor);
                        //texMeta.SetApplyUpdateRenderTexture();
                    }
                    else
                    {
                        var wasRt = texMeta.Target == TexTarget.RenderTexture;

                        if (wasRt)
                            UpdateOrSetTexTarget(TexTarget.Texture2D);

                        texMeta.SetPixels(texMeta.clearColor, GlobalBrush.mask).SetAndApply();

                        if (wasRt)
                            UpdateOrSetTexTarget(TexTarget.RenderTexture);
                    }
                }
            }

            pegi.Nl();


            if (s_showTextureOptions)
            {
                pegi.Indent();

                using (context.StartContext())
                {
                    if ("Optional UI Elements".PegiLabel().IsEntered().Nl())
                    {

                        "Show Previous Textures (if any) ".PegiLabel("Will show textures previously used for this material property.")
                            .ToggleIcon(  ref cfg.showRecentTextures).Nl();

                        "Exclusive Render Textures".PegiLabel("Allow creation of simple Render Textures - the have limited editing capabilities.")
                            .ToggleIcon(
                                ref cfg.allowExclusiveRenderTextures).Nl();

                        "Color Sliders ".PegiLabel("Should the color slider be shown ").ToggleIcon(
                            ref cfg.showColorSliders).Nl();

                        if (texMeta != null)
                        {

                            foreach (var module in texMeta.Modules)
                            {
                                module.ShowHideSectionInspect();
                                pegi.Nl();
                            }

                            if (texMeta.IsAVolumeTexture)
                                "Show Volume Data in Painter".PegiLabel()
                                    .ToggleIcon(ref Painter.Data.showVolumeDetailsInPainter)
                                    .Nl();

                        }

                        "Brush Dynamics".PegiLabel("Will modify scale and other values based on movement.")
                            .ToggleIcon(
                                ref GlobalBrush.showBrushDynamics).Nl();

                        "URL field".PegiLabel("Option to load images by URL").ToggleIcon(ref cfg.showUrlField).Nl();
                    }

                    if ("Color Schemes".PegiLabel().Toggle_Enter(ref cfg.showColorSchemes).Nl_ifNotEntered().Nl())
                    {
                        if (cfg.colorSchemes.Count == 0)
                            cfg.colorSchemes.Add(new ColorPicker { paletteName = "New Color Scheme" });

                        pegi.Edit_List(cfg.colorSchemes, ref cfg.inspectedColorScheme);
                    }

                    if ("New Texture ".PegiLabel().IsEntered().Nl())
                    {

                        if (cfg.newTextureIsColor)
                            "Clear Color".PegiLabel().Edit(ref cfg.newTextureClearColor).Nl();
                        else
                            "Clear Value".PegiLabel().Edit(ref cfg.newTextureClearNonColorValue).Nl();

                        "Color Texture".PegiLabel("Will the new texture be a Color Texture").ToggleIcon(ref cfg.newTextureIsColor).Nl();

                        "Size:".PegiLabel("Size of the new Texture", 40).Select_Index(
                            ref Painter.Data.selectedWidthIndex,
                            SO_PainterDataAndConfig.NewTextureSizeOptions).Nl();

                        "Click + next to texture field below to create texture using this parameters".PegiLabel()
                            .Write_Hint();

                        pegi.Nl();

                    }

                    "Painter Modules (Debug)".PegiLabel().IsEntered().Nl().If_Entered(() => Modules.Nested_Inspect().Nl());

                    "Texture Meta".PegiLabel().Enter_Inspect(texMeta).Nl();
                }

                if (context.IsAnyEntered)
                    return;
            }
            

            if (texMeta != null)
            {
                if (IsUiGraphicPainter)
                {

                    var uiImage = uiGraphic as Image;
                    if (uiImage && !uiImage.sprite)
                    {
                        pegi.Nl();
                        "Sprite is null. Convert image to sprite and assign it".PegiLabel().WriteWarning();
                        pegi.Nl();
                    }

                    if (texMeta.updateTex2DafterStroke == false && texMeta.TargetIsRenderTexture())
                    {
                        "Update Original Texture after every Stroke".PegiLabel().ToggleIcon(ref texMeta.updateTex2DafterStroke).Nl();
                    }
                }


                texMeta.ComponentDependent_PEGI(s_showTextureOptions, this);

                if (s_showTextureOptions || (IsUsingPreview && cfg.previewAlphaChanel))
                {
                    "Preview Edited RGBA".PegiLabel().ToggleIcon(ref cfg.previewAlphaChanel);

                    MsgPainter.previewRGBA.DocumentationClick().Nl();
                }

                if (s_showTextureOptions)
                {
                    var mats = Materials;
                    if (autoSelectMaterialByNumberOfPointedSubMesh || !mats.IsNullOrEmpty())
                    {
                        "Auto Select Material".PegiLabel("Material will be changed based on the subMesh you are painting on").ToggleIcon(ref autoSelectMaterialByNumberOfPointedSubMesh);
                        MsgPainter.AutoSelectMaterial.DocumentationClick().Nl();
                    }
                }

                if (s_showTextureOptions || invertRayCast)
                {
                    if (!IsUiGraphicPainter)
                        "Invert RayCast".PegiLabel("Will rayCast into the camera (for cases when editing from inside a sphere, mask for 360 video for example.)").ToggleIcon(ref invertRayCast).Nl();
                    else
                        invertRayCast = false;
                }



                if (texMeta[TextureCfgFlags.EnableUndoRedo] && texMeta.backupManually && "Backup for UNDO".PegiLabel().Click())
                    texMeta.OnStrokeMouseDown_CheckBackup();

                if (texMeta.dontRedoMipMaps && Icon.Refresh.Click("Update Mipmaps now").Nl())
                    texMeta.SetAndApply();
            }
            
            pegi.UnIndent();
        }

        private void Inspect_Config() 
        {
            pegi.Nl();
            if (Icon.Exit.Click() | "Preferences".PegiLabel().ClickLabel())
                Painter.Data.showConfig = false;
            else
            {
                pegi.Nl();
                Painter.Camera.Nested_Inspect().Nl();
            }
        }

        private void Inspect_TopButtons() 
        {
            var cfg = Painter.Data;

            if (meshEditing)
            {
                if (Icon.Painter.Click("Edit Texture"))
                {
                    CheckSetOriginalShader();
                    meshEditing = false;
                    CheckPreviewShader();
                    Painter.MeshManager.StopEditingMesh();
                    cfg.showConfig = false;
                    pegi.GameView.ShowNotification("Editing Texture");
                }

                Icon.Mesh.Draw("Editing Mesh");

            }
            else
            {
                Icon.Painter.Draw("Editing Texture");

                if (Icon.Mesh.Click("Edit Mesh"))
                {
                    meshEditing = true;

                    CheckSetOriginalShader();
                    UpdateOrSetTexTarget(TexTarget.Texture2D);
                    cfg.showConfig = false;
                    pegi.GameView.ShowNotification("Editing Mesh");

                    if (SavedEditableMesh.IsEmpty == false)
                        Painter.MeshManager.EditMesh(this, false);
                }
            }

            if (Icon.Config.Click("Preferences"))
                cfg.showConfig = true;

            if (Icon.Book.Click(toolTip: "Documentation"))
                pegi.FullWindow.OpenDocumentation(Documentation.Inspect);
       
        }

        private bool Inspect_IsSetupCorrect() 
        {
            Inspect_LockUnlock();

            if (currentlyPaintedObjectPainter)
                pegi.EditorView.RefocusIfLocked(this, currentlyPaintedObjectPainter);
#if UNITY_EDITOR
            else if (Selection.objects.Length == 1)
            {
                var go = Selection.objects[0] as GameObject;
                if (go && go != gameObject)
                    pegi.EditorView.RefocusIfLocked(this, go.GetComponent<PainterComponent>());
            }
#endif

            if (!Painter.Camera && "Find camera".PegiLabel().Click())
            {
                Painter.TryInstanciateCamera(out var cam);
            }

            if (!Painter.Camera)
                return false;
            else if (!Painter.Data)
            {
                Painter.Camera.DependenciesInspect();

                return false;
            }

            if (QcUnity.IsPartOfAPrefab(gameObject))
            {
                "Inspecting a prefab.".PegiLabel().Nl();
                return false;
            }

            if (!IsCurrentTool)
            {
                if (Icon.Off.Click("Click to Enable Tool"))
                {
                    IsCurrentTool = true;
                    enabled = true;

#if UNITY_EDITOR
                    var cs = GetComponents<Component>();

                    foreach (var c in cs)
                        if (c.GetType() != typeof(PainterComponent))
                            InternalEditorUtility.SetIsInspectorExpanded(c, false);

                    QcUnity.FocusOn(null);
                    Singleton_PainterCamera.refocusOnThis = gameObject;
#endif

                    CheckPreviewShader();
                }

                pegi.EditorView.Lock_UnlockClick(gameObject);

                return false;
            }

            if (!Painter.Camera.enabled)
            {
                "Painter Camera is disabled".PegiLabel().WriteWarning();
       
                if ("Enable".PegiLabel().Click())
                    Painter.Camera.enabled = true;

                return false;
            }
            else if (!Painter.Camera.gameObject.activeInHierarchy)
            {
                if (Painter.Camera.gameObject.activeSelf == false)
                {
                    "Painter Camera Game Object or parent is disabled".PegiLabel().WriteWarning();
                    if ("Enable".PegiLabel().Click())
                        Painter.Camera.gameObject.SetActive(true);
                } else
                    "Painter Camera is child of disabled game object".PegiLabel().WriteWarning();

                return false;
            }
            else
            {
                double sinceUpdate = QcUnity.TimeSinceStartup() - Singleton_PainterCamera.lastManagedUpdate;

                if (sinceUpdate > 100)
                {
                    "It's been {0} seconds since the last managed update".F(sinceUpdate).PegiLabel().WriteWarning();

                    if ("Resubscribe camera to updates".PegiLabel().Click())
                        Painter.Camera.SubscribeToEditorUpdates();

                    return false;
                }
            }

            selectedInPlaytime = this;

            if (Application.isPlaying && (
#if UNITY_EDITOR
                    (IsCurrentTool && !Application.isPlaying) ||
#endif
                    Icon.On.Click("Click to Disable Tool")))
            {
                IsCurrentTool = false;
                Painter.MeshManager.StopEditingMesh();
                SetOriginalShaderOnThis();
                UpdateOrSetTexTarget(TexTarget.Texture2D);
            }

            pegi.EditorView.Lock_UnlockClick(gameObject);

            InitIfNotInitialized();

            if (MeshPainting.target && (MeshPainting.target != this))
                Painter.MeshManager.StopEditingMesh();

            return true;
        }

        internal void Inspect_ConvexMeshCheckWarning()
        {
            if (meshCollider)
            {
                if (meshCollider.convex)
                {
                    pegi.Nl();
                    "Convex mesh collider detected. Texture-space brushes and some mesh tools will not work".PegiLabel().WriteWarning();
                    if ("Disable convex".PegiLabel().Click())
                        meshCollider.convex = false;
                    pegi.Nl();
                } else if (!gameObject.isStatic && meshCollider.sharedMesh && meshFilter && meshFilter.sharedMesh != meshCollider.sharedMesh) 
                {
                    pegi.Nl();

                    "Mesh Collider & Mesh Filter are using different meshes. If it is Convex painting may not work properly".PegiLabel().WriteWarning();
                    if ("Replace Mesh on Mesh Collider".PegiLabel().Click().Nl())
                        meshCollider.sharedMesh = meshFilter.sharedMesh;
                }
            }
        }

        internal bool Inspect_PreviewShaderToggle()
        {

            var changed = pegi.ChangeTrackStart();
          
            if (NotUsingPreview && Icon.OriginalShader.Click("Switch To Preview Shader", 45))
                SetPreviewShader();

            if (IsUsingPreview && Icon.PreviewShader.Click("Return to Original Shader", 45))
            {
                MatDta.usePreviewShader = false;
                SetOriginalShaderOnThis();
            }
            
            return changed;

        }

        private void Inspect_MeshAndTextureCommons() 
        {
            if (meshCollider)
            {
                if (!meshCollider.sharedMesh)
                {
                    pegi.Nl();
                    "Mesh Collider has no mesh".PegiLabel().WriteWarning();
                    if (meshFilter && meshFilter.sharedMesh && "Assign".PegiLabel("Will assign {0}".F(meshFilter.sharedMesh)).Click())
                        meshCollider.sharedMesh = meshFilter.sharedMesh;

                    pegi.Nl();
                }
                else if (meshFilter && meshFilter.sharedMesh && meshFilter.sharedMesh != meshCollider.sharedMesh)
                {
                    pegi.FullWindow.WarningDocumentationClickOpen(
                        "Collider and filter have different meshes. Painting may not be able to obtain a correct UV coordinates.",
                        "Mesh collider mesh is different");
                }
            }
        }

        void IPEGI.Inspect()
        {
            using (QcSharp.SetTemporaryValueDisposable(this, p => inspected = p, () => null))
            {
                var changed = pegi.ChangeTrackStart();

                if (Inspect_IsSetupCorrect())
                {
                    var cfg = Painter.Data;

                    if (cfg.showConfig)
                        Inspect_Config();
                    else
                    {
                        Inspect_TopButtons();

                        Inspect_MeshAndTextureCommons();

                        if (meshEditing)
                            Inspect_MeshPart();
                        else
                            Inspect_TexturePart();

                        pegi.Nl();

                        foreach (var p in CameraModuleBase.ComponentInspectionPlugins)
                        {
                            p.ComponentInspector();
                            pegi.Nl();
                        }
                    }

                    if (changed)
                    {
                        Painter.Camera.OnBeforeBlitConfigurationChange();
                        Update_Brush_Parameters_For_Preview_Shader();
                    }

                    pegi.Nl();
                }
            }
            pegi.Nl();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!Painter.Camera || this != Painter.FocusedPainter) return;

            if (meshEditing && !Application.isPlaying)
                Painter.MeshManager.DRAW_Lines(true);

            var br = GlobalBrush;

            if (NotUsingPreview && !TextureEditingBlocked && _lastMouseOverObject == this && IsCurrentTool &&
                Is3DBrush() && br.showingSize && !Painter.Data.showConfig)
                Gizmos.DrawWireSphere(stroke.posTo, br.Size(true) * 0.5f
                );

            foreach (var p in CameraModuleBase.GizmoPlugins)
                p.PlugIn_PainterGizmos(this);
        }
#endif

#if UNITY_EDITOR
        [MenuItem("Tools/" + SO_PainterDataAndConfig.ToolName + "/Add Painters To Selected")]

        private static void AddPainterToSelected()
        {
            foreach (var go in Selection.gameObjects)
                IterateAssignToChildren(go.transform);
        }
#endif

        private static void IterateAssignToChildren(Transform tf)
        {
            if ((!tf.GetComponent<PainterComponent>())
                && (tf.GetComponent<Renderer>())
                && (!tf.GetComponent<PlaytimePainter_RenderBrush>()) && (CanEditWithTag(tf.tag)))
                tf.gameObject.AddComponent<PainterComponent>();

            for (var i = 0; i < tf.childCount; i++)
                IterateAssignToChildren(tf.GetChild(i));

        }

#if UNITY_EDITOR
        [MenuItem("Tools/" + SO_PainterDataAndConfig.ToolName + "/Remove Painters From the Scene")]
#endif
        private static void TakePainterFromAll()
        {
            var allObjects = FindObjectsByType<PainterComponent>(FindObjectsSortMode.None);//FindObjectsOfType<Renderer>();
            foreach (var mr in allObjects)
            {
                mr.DestroyWhateverComponent();
                //var ip = mr.GetComponent<PainterComponent>();
                //if (ip)
                  //  DestroyImmediate(ip);
            }

            var rtp = FindObjectsByType<Singleton_PainterCamera>(FindObjectsSortMode.None);

            if (!rtp.IsNullOrEmpty())
                foreach (var rt in rtp)
                    rt.gameObject.DestroyWhatever();

            var dc = FindObjectsByType<Singleton_DepthProjectorCamera>(FindObjectsSortMode.None);

            if (!dc.IsNullOrEmpty())
                foreach (var d in dc)
                    d.gameObject.DestroyWhatever();
        }

#if UNITY_EDITOR
        [MenuItem("Tools/" + SO_PainterDataAndConfig.ToolName + "/Join Discord")]
#endif
        internal static void Open_Discord() => Application.OpenURL(pegi.FullWindow.DISCORD_SERVER);

#if UNITY_EDITOR
        [MenuItem("Tools/" + SO_PainterDataAndConfig.ToolName + "/Send an Email")]
#endif
        internal static void Open_Email() => QcUnity.SendEmail(pegi.FullWindow.SUPPORT_EMAIL,
            "About your Playtime Painter",
            "Hello Yuri, we need to talk. I purchased your asset and expect an excellent quality, but ...");

#if UNITY_EDITOR
        [MenuItem("Tools/" + SO_PainterDataAndConfig.ToolName + "/Open Manual")]
#endif
        internal static void OpenWWW_Documentation() => Application.OpenURL(OnlineManual);

    }
}
