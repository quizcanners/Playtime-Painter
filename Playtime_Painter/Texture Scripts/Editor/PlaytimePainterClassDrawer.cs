using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

#if UNITY_EDITOR

namespace Playtime_Painter {

    [CustomEditor(typeof(PlaytimePainter))]
    public class PlaytimePainterClassDrawer : SceneViewEditable<PlaytimePainter> {

        static PainterDataAndConfig Cfg { get { return PainterCamera.Data; } }

        public override bool AllowEditing(PlaytimePainter targ) {
            return (targ) && ((!targ.LockTextureEditing) || targ.IsEditingThisMesh);
        }

        public override bool OnEditorRayHit(RaycastHit hit, Ray ray) {

            Transform tf = hit.transform;
            PlaytimePainter pointedPainter = tf?.GetComponent<PlaytimePainter>();
            Event e = Event.current;

            bool allowRefocusing = true;

            if (painter != null)
            {
                if (painter.meshEditing)
                {

                    PlaytimePainter edited = MeshManager.Inst.target;

                    allowRefocusing = false;

                    if (pointedPainter != null) {

                        if ((pointedPainter != edited) && (pointedPainter.meshEditing) 
                            && (pointedPainter.SavedEditableMesh != null ) && L_mouseDwn && (e.button == 0)) {
                            MeshManager.Inst.EditMesh(pointedPainter, false);
                            allowRefocusing = true;
                        }

                        //if ((edited == null) || (edited != pointedPainter))
                          //  allowRefocusing = true;
                    }

                    if ((((e.button == 1) && (!MeshManager.Inst.Dragging))
                        || (e.button == 2)) && ((e.type == EventType.MouseDown) || (e.type == EventType.MouseDrag) || (e.type == EventType.MouseUp)))

                        navigating = true;

                    return allowRefocusing;
                }
                else
                {
                    if (L_mouseDwn) PlaytimePainter.currently_Painted_Object = null;

                    if (painter.NeedsGrid()) { pointedPainter = painter; allowRefocusing = false; }
                    
                    if (pointedPainter != null)
                    {
                        StrokeVector st = pointedPainter.stroke;
                        st.mouseUp = L_mouseUp;
                        st.mouseDwn = L_mouseDwn;

                        pointedPainter.OnMouseOver_SceneView(hit, e);
                    }

                }
            }
            if (L_mouseUp) PlaytimePainter.currently_Painted_Object = null;

            if (((e.button == 1) || (e.button == 2)) && ((e.type == EventType.MouseDown) || (e.type == EventType.MouseDrag) || (e.type == EventType.MouseUp)))
                navigating = true;


            return allowRefocusing;
        }

        public override void FeedEvents(Event e) {
            
            GridNavigator.Inst().FeedEvent(e);

            if (painter != null) {

                painter.FeedEvents(e);

                if (painter.meshEditing)
                MeshManager.Inst.UpdateInputEditorTime(e,  L_mouseUp, L_mouseDwn);

                
            }
        }
        
        public override void GridUpdate(SceneView sceneview) {

            base.GridUpdate(sceneview);

            if (!IsCurrentTool()) return;

            if ((painter != null) && (painter.textureWasChanged))
                painter.Update();

        }

     
        const int range = 9;
        const int minPow = 2;
        
        public static Tool previousTool;

#if PEGI
        static string[] texSizes;

#endif

        public override void OnInspectorGUI() {


#if PEGI

            

              bool changes = false;

            painter = (PlaytimePainter)target;

            if  (painter.gameObject.IsPrefab()) {
                "Inspecting a prefab.".nl();
                return;
            }

            PainterCamera rtp = PainterCamera.Inst;

            if (!Cfg)
            {
                "No Config Detected".nl();
                return;
            }

           

            ef.start(serializedObject);
         



            if (!PlaytimePainter.IsCurrent_Tool()) {
                if (pegi.Click(icon.Off, "Click to Enable Tool", 25)) {
                    PlaytimeToolComponent.enabledTool = typeof(PlaytimePainter);//  customTools.Painter;
                    CloseAllButThis(painter);
                    painter.CheckPreviewShader();
                    PlaytimePainter.HideUnityTool();
                }
                painter.gameObject.end();
                return;
            } else {

                if ((IsCurrentTool() && (painter.terrain != null) && (Application.isPlaying == false) && (UnityEditorInternal.InternalEditorUtility.GetIsInspectorExpanded(painter.terrain) == true)) ||
                    (pegi.Click(icon.On.getIcon(), "Click to Disable Tool", 25))) {
                    PlaytimeToolComponent.enabledTool = null; //customTools.Disabled;
                    MeshManager.Inst.DisconnectMesh();
                    painter.SetOriginalShaderOnThis();
                    painter.UpdateOrSetTexTarget(TexTarget.Texture2D);
                    PlaytimePainter.RestoreUnityTool();
                }
            }

            painter.InitIfNotInited();

            ImageData image = painter.ImgData;

            Texture tex = painter.GetTextureOnMaterial();
            if ((!painter.meshEditing) && ((tex != null) && (image == null)) || ((image != null) && (tex == null)) || ((image != null) && (tex != image.texture2D) && (tex != image.CurrentTexture())))
                painter.textureWasChanged = true;


            changes = painter.PEGI_MAIN();
            image = painter.ImgData;


            if (painter.meshEditing || (PainterStuff.IsNowPlaytimeAndDisabled)) { painter.gameObject.end();  return; } 

            if ((painter.meshRenderer != null || painter.terrain != null) && !Cfg.showConfig) {

             
                    if (!painter.LockTextureEditing) {
                        ef.newLine();

                    if (!painter.IsTerrainControlTexture()) {

                        string Orig = "";

                        if (image.texture2D != null) {
                                Orig = image.texture2D.GetPathWithout_Assets_Word();
                                if ((pegi.Click(icon.Load, "Will reload " + Orig, 25))) {
                                    painter.ForceReimportMyTexture(Orig);
                                    image.SaveName = image.texture2D.name;
                                    GUI.FocusControl("dummy");
                                    if (painter.terrain != null)
                                        painter.UpdateShaderGlobals();
                                }
                            }


                        "Texture Name: ".edit(70, ref image.SaveName);

                        if (image.texture2D != null)
                        {

                            string Dest = painter.GenerateTextureSavePath();
                            bool existsAtDestination = painter.TextureExistsAtDestinationPath();
                            bool originalExists = (Orig != null);
                            bool sameTarget = originalExists && (Orig.Equals(Dest));
                            bool sameTextureName = originalExists && image.texture2D.name.Equals(image.SaveName);


                            if ((existsAtDestination == false) || sameTextureName)
                            {
                                if (ef.Click(Icons_MGMT.getIcon(sameTextureName ? icon.Save : icon.SaveAsNew), (sameTextureName ? "Will Update " + Orig : "Will save as " + Dest), 25))
                                {
                                    if (sameTextureName)
                                        painter.RewriteOriginalTexture();
                                    else
                                        painter.SaveTextureAsAsset(false);

                                    painter.OnChangedTexture_OnMaterial();
                                }
                            }
                            else if (existsAtDestination && (pegi.Click(icon.Save, "Will replace " + Dest, 25)))
                                painter.SaveTextureAsAsset(false);


                            ef.newLine();

                            if ((!sameTarget) && (!sameTextureName) && (string.IsNullOrEmpty(Orig) == false) && (ef.Click("Replace", "Will replace " + Orig + " with " + Dest)))
                                painter.RewriteOriginalTexture_Rename(image.SaveName);

                        }
                    }
                    ef.newLine();




                }
                ef.newLine();

                    pegi.Space();
                    pegi.newLine();

               

                    var mats = painter.GetMaterials();
                    if ((mats != null) && (mats.Length > 0))
                    {
                        int sm = painter.selectedSubmesh;
                        if (pegi.select(ref sm, mats))
                        {
                            painter.SetOriginalShaderOnThis();
                            painter.selectedSubmesh = sm;
                            painter.OnChangedTexture_OnMaterial();
                            image = painter.ImgData;
                            painter.CheckPreviewShader();
                        }
                    }


                    Material mater = painter.Material;

                    if (pegi.edit(ref mater))
                        painter.Material = mater;



                    if (icon.NewMaterial.Click("Instantiate Material", 25).nl())
                    {
                        changes = true;
                        painter.InstantiateMaterial(true);
                    }

                    // pegi.newLine();

                    if ((mats != null) && (mats.Length > 1))

                        "Auto Select Material:".toggle("Material will be changed based on the submesh you are painting on", 120,
                                                       ref painter.autoSelectMaterial_byNumberOfPointedSubmesh).nl();


                    pegi.nl();
                    ef.Space();
                    ef.newLine();

                    //      pegi.write("Tex:", "Texture field on the material", 30);

                    if (painter.SelectTexture_PEGI())
                    {

                        image = painter.ImgData;
                        if (image == null) painter.nameHolder = painter.gameObject.name + "_" + painter.MaterialTexturePropertyName;
                    }

                    if (image != null)
                        painter.UpdateTylingFromMaterial();

                    TextureSetterField();

                    if ((painter.IsTerrainControlTexture() == false))
                    {

                        bool isTerrainHeight = painter.IsTerrainHeightTexture();

                        int texScale = (!isTerrainHeight) ?
                             ((int)Mathf.Pow(2, PainterCamera.Data.selectedSize + minPow))

                            : (painter.terrain.terrainData.heightmapResolution - 1);

                        List<string> texNames = painter.GetMaterialTextureNames();

                        if (texNames.Count > painter.SelectedTexture)
                        {
                            string param = painter.MaterialTexturePropertyName;

                            if (pegi.Click(icon.NewTexture, (image == null) ? "Create new texture2D for " + param : "Replace " + param + " with new Texture2D " + texScale + "*" + texScale, 25).nl())
                            {
                                changes = true;
                                if (isTerrainHeight)
                                    painter.CreateTerrainHeightTexture(painter.nameHolder);
                                else
                                    painter.CreateTexture2D(texScale, painter.nameHolder, Cfg.newTextureIsColor);
                            }



                            if ((image == null) && (Cfg.moreOptions) && ("Create Render Texture".Click()))
                            {
                                changes = true;
                                painter.CreateRenderTexture(texScale, painter.nameHolder);
                            }

                            if ((image != null) && (Cfg.moreOptions))
                            {
                                if ((image.renderTexture == null) && ("Add Render Tex".Click()))
                                {
                                    changes = true;
                                    image.AddRenderTexture();
                                }
                                if (image.renderTexture != null)
                                {

                                    if ("Replace RendTex".Click("Replace " + param + " with Rend Tex size: " + texScale))
                                    {
                                        changes = true;
                                        painter.CreateRenderTexture(texScale, painter.nameHolder);
                                    }
                                    if ("Remove RendTex".Click().nl())
                                    {
                                        changes = true;
                                        if (image.texture2D != null)
                                        {
                                            painter.UpdateOrSetTexTarget(TexTarget.Texture2D);
                                            image.renderTexture = null;
                                        }
                                        else
                                        {

                                            painter.RemoveTextureFromMaterial(); //SetTextureOnMaterial(null);
                                        }

                                    }
                                }
                            }
                        }
                        else
                            "No Material's Texture selected".nl();

                        pegi.nl();

                        if (image == null)
                            "_Name:".edit("Name for new texture", 40, ref painter.nameHolder).nl();



                        if (!isTerrainHeight)
                        {
                            "Color:".toggle("Will the new texture be a Color Texture", 40, ref Cfg.newTextureIsColor);
                            ef.write("Size:", "Size of the new Texture", 40);
                            if ((texSizes == null) || (texSizes.Length != range))
                            {
                                texSizes = new string[range];
                                for (int i = 0; i < range; i++)
                                    texSizes[i] = Mathf.Pow(2, i + minPow).ToString();
                            }

                            ef.select(ref PainterCamera.Data.selectedSize, texSizes, 60);
                        }
                        ef.newLine();
                    }

                    ef.newLine();
                    ef.tab();
                    ef.newLine();

                    List<ImageData> recentTexs;

                    string texName = painter.MaterialTexturePropertyName;

                    if ((texName != null) && (PainterCamera.Data.recentTextures.TryGetValue(texName, out recentTexs))
                        && ((recentTexs.Count > 1) || (painter.ImgData == null)))
                    {
                        ef.write("Recent Texs:", 60);
                        ImageData tmp = painter.ImgData;//.exclusiveTexture();
                        if (pegi.select(ref tmp, recentTexs))
                        {
                            painter.ChangeTexture(tmp.ExclusiveTexture());
                            changes = true;
                        }
                    }

                

                ef.Space();
                ef.newLine();
                ef.Space();


            }
           
            if (changes)
                painter.Update_Brush_Parameters_For_Preview_Shader();


            painter.gameObject.end();
#endif
        }

        bool TextureSetterField() {
            string field = painter.MaterialTexturePropertyName;
            if ((field == null) || (field.Length == 0)) return false;

            Texture tex = painter.GetTextureOnMaterial();
            Texture after = (Texture)EditorGUILayout.ObjectField(tex, typeof(Texture), true);

            if (tex != after) {
                painter.ChangeTexture(after);
                return true;
            }

            return false;

        }

    }
#endif
}

