using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using PlayerAndEditorGUI;
#if UNITY_EDITOR

namespace Painter {

    [CustomEditor(typeof(PlaytimePainter))]
    public class PlaytimePainterClassDrawer : SceneViewEditable<PlaytimePainter> {

        static painterConfig cfg { get { return painterConfig.inst(); } }

        public override bool AllowEditing(PlaytimePainter targ) {
            return (targ != null) && (targ.LockEditing == false) && ((targ.curImgData != null) || (targ.meshPainting));
        }

        public override bool OnEditorRayHit(RaycastHit hit, Ray ray) {

            Transform tf = hit.transform;
            PlaytimePainter p = tf == null ? null : tf.GetComponent<PlaytimePainter>();
            Event e = Event.current;

            if ((p!= null) && (p.meshPainting)) 
                return OnEditorRayHitMesh(hit, ray, p.meshPainter);

            if (L_mouseDwn) PlaytimePainter.currently_Painted_Object = null;

            if (p != null) {
                StrokeVector st = p.stroke;
                st.mouseUp = L_mouseUp;
                st.mouseDwn = L_mouseDwn;
                st.uvTo = hit.textureCoord;
                st.posTo = hit.point;

                p.OnMouseOver_SceneView(hit, e);
            }

            if (L_mouseUp) PlaytimePainter.currently_Painted_Object = null;

            if (((e.button == 1) || (e.button == 2)) && ((e.type == EventType.mouseDown) || (e.type == EventType.mouseDrag) || (e.type == EventType.mouseUp)))
                navigating = true;

            return true;
        }

        public override void getEvents(Event e, Ray ray) {
            if ((painter!= null) && (painter.meshPainting))
            MeshManager.inst().UpdateInputEditorTime(e, ray, L_mouseUp, L_mouseDwn);
        

            if ((e.type == EventType.keyDown) && (painter != null) && (painter.meshPainting == false)) {
                imgData id = painter.curImgData;
                if (id != null) {
                    if ((e.keyCode == KeyCode.Z) && (id.cache.undo.gotData()))
                        id.cache.undo.ApplyTo(id);
                    else if ((e.keyCode == KeyCode.X) && (id.cache.redo.gotData()))
                        id.cache.redo.ApplyTo(id);
                }
            }
        }

        public bool OnEditorRayHitMesh(RaycastHit hit, Ray ray, MeshPainter pm)
        {
            Transform tf = hit.transform;
            MeshPainter edited = MeshManager.inst()._target;

            Event e = Event.current;

            bool allowRefocusing = false;

            if ((pm != edited) && (pm.p.LockEditing == false) && L_mouseDwn && (e.button == 0))
            {
                MeshManager.inst().EditMesh(pm);
                allowRefocusing = true;
            }

            if ((edited == null) || (edited != pm))
                allowRefocusing = true;

            // if ((tf != null) && (tf.tag != "VertexEd"))
            //   allowRefocusing = true;

            if ((((e.button == 1) && (!MeshManager.inst().draggingSelected)) || (e.button == 2)) && ((e.type == EventType.mouseDown) || (e.type == EventType.mouseDrag) || (e.type == EventType.mouseUp)))
                navigating = true;

            return allowRefocusing;
        }



        public override void GridUpdate(SceneView sceneview) {

            base.GridUpdate(sceneview);

            if (!isCurrentTool()) return;

            if ((painter != null) && (painter.textureWasChanged))
                painter.Update();

        }

        static string[] texSizes;
        const int range = 9;
        const int minPow = 4;

        public int selectedSize() {
            return (int)Mathf.Pow(2, painterConfig.inst().selectedSize + minPow);
        }

        public static bool textureSizeSelect() {

            if ((texSizes == null) || (texSizes.Length != range)) {
                texSizes = new string[range];
                for (int i = 0; i < range; i++)
                    texSizes[i] = Mathf.Pow(2, i + minPow).ToString();
            }

            return ef.select(ref painterConfig.inst().selectedSize, texSizes, 60);


        }


        public override void OnInspectorGUI() {

            ef.start(serializedObject);
            painter = (PlaytimePainter)target;

            if (!PlaytimePainter.isCurrent_Tool()) {
                if (pegi.Click(icon.Off, "Click to Enable Tool", 25)) {
                    PlaytimeToolComponent.enabledTool = typeof(PlaytimePainter);//  customTools.Painter;
                    CloseAllButThis(painter);
                    painter.CheckPreviewShader();
                }
                pegi.newLine();
                return;
            } else {

                if ((isCurrentTool() && (painter.terrain != null) && (Application.isPlaying == false) && (UnityEditorInternal.InternalEditorUtility.GetIsInspectorExpanded(painter.terrain) == true)) ||
                    (pegi.Click(icon.On.getIcon(), "Click to Disable Tool", 25))) {
                    PlaytimeToolComponent.enabledTool = null; //customTools.Disabled;
                    painter.SetOriginalShader();
                }
            }

            painter.InitIfNotInited();



            if (painter.meshPainting)
            {
                if ("Texture".Click())
                    painter.meshPainting = false;
                else
                    painter.meshPainter.PEGI();

                return;
            }
            else if ("Mesh".Click())
                painter.meshPainting = true;


            PainterManager rtp = PainterManager.inst;

            if ((painter.meshRenderer != null) || (painter.terrain != null)) {

                BrushConfig brush = PlaytimePainter.brush;
                imgData image = painter.curImgData;

                Texture tex = painter.getTexture();
                if (((tex != null) && (image == null)) || ((image != null) && (tex == null)) || ((image != null) && (tex != image.texture2D) && (tex != image.currentTexture())))
                    painter.textureWasChanged = true;

                if (ef.toggle(ref painter.LockEditing, icon.Lock.getIcon(), icon.Unlock.getIcon(), "Lock/Unlock editing of this abject.", 25))
                    painter.CheckPreviewShader();

                if (painter.LockEditing) {
                    ef.newLine();
                    return;
                }

                bool brushChanged_RT = painter.ifGotTexture_PEGI();

                if (!cfg.showConfig) {
                    if (image != null) {

                        ef.newLine();

                        if (!painter.isTerrainControlTexture()) {

                            string Orig = "";

                            if (image.texture2D != null) {
                                Orig = painter.curImgData.texture2D.GetPathWithout_Assets_Word();
                                if ((pegi.Click(icon.Load, "Will reload " + Orig, 25))) {
                                    painter.ForceReimportMyTexture(Orig);
                                    painter.curImgData.SaveName = painter.curImgData.texture2D.name;
                                    GUI.FocusControl("dummy");
                                }
                            }


                            "Texture Name: ".edit(70, ref painter.curImgData.SaveName);

                            if (image.texture2D != null) {

                                string Dest = painter.GenerateSavePath();
                                bool existsAtDestination = painter.textureExistsAtDestinationPath();
                                bool originalExists = (Orig != null);
                                bool sameTarget = originalExists && (Orig.Equals(Dest));
                                bool sameTextureName = originalExists && image.texture2D.name.Equals(image.SaveName);


                                if ((existsAtDestination == false) || sameTextureName) {
                                    if (ef.Click(Icons_MGMT.getIcon(sameTextureName ? icon.save : icon.saveAsNew), (sameTextureName ? "Will Update " + Orig : "Will save as " + Dest), 25)) {
                                        if (sameTextureName)
                                            painter.RewriteOriginalTexture();
                                        else
                                            painter.SaveTextureAsAsset(false);

                                        painter.OnChangedTexture();
                                    }
                                } else if (existsAtDestination && (pegi.Click(icon.save, "Will replace " + Dest, 25)))
                                    painter.SaveTextureAsAsset(false);


                                ef.newLine();

                                if ((!sameTarget) && (!sameTextureName) && (string.IsNullOrEmpty(Orig) == false) && (ef.Click("Replace", "Will replace " + Orig + " with " + Dest)))
                                    painter.RewriteOriginalTexture_Rename(image.SaveName);

                            }
                        }
                        ef.newLine();


                        if (brushChanged_RT)
                            painter.Update_Brush_Parameters_For_Preview_Shader();

                    }
                    ef.newLine();

                    pegi.Space();
                    pegi.newLine();

                    ef.write("Mat:", 30);
                    List<string> mats = painter.getMaterials();
                    if ((mats != null) && (mats.Count > 0)) {
                        if (ef.select(ref painter.selectedMaterial, mats.ToArray())) {
                            painter.OnChangedTexture();
                            image = painter.curImgData;
                        }
                    }

                    pegi.write(painter.getMaterial());
                    // EditorGUILayout.ObjectField(painter.getMaterial(), typeof(Material), false);

                    Material mater = painter.getMaterial();


                    if ((cfg.moreOptions || (mater == null) || (mater == rtp.defaultMaterial) || (image == null))
                        && (pegi.Click(icon.NewMaterial, "Instantiate Material", 25).nl()))
                        painter.InstantiateMaterial(true);



                    if ((mats != null) && (mats.Count > 1)) {
                        "Auto Select Material:".toggle("Material will be changed based on the submesh you are painting on", 120,
                                                       ref painter.autoSelectMaterial_byNumberOfPointedSubmesh).nl();
                    }

                    pegi.nl();
                    ef.Space();
                    ef.newLine();

                    pegi.write("Tex:", "Texture field on the material", 30);

                    if (painter.SelectTexture_PEGI()) {
                        image = painter.curImgData;
                        if (image == null) painter.nameHolder = painter.gameObject.name + "_" + painter.getMaterialTextureName();
                    }

                    if (image != null)
                        painter.UpdateTyling();

                    textureSetterField();

                    if (((image == null) || (cfg.moreOptions)) &&
                            (painter.isTerrainControlTexture() == false)) {

                        bool isTerrainHeight = painter.isTerrainHeightTexture();

                        int texScale = (!isTerrainHeight) ?
                            selectedSize() : (painter.terrain.terrainData.heightmapResolution - 1);

                        List<string> texNames = painter.getMaterialTextureNames();

                        if (texNames.Count > painter.selectedTexture) {
                            string param = painter.getMaterialTextureName();

                            if (pegi.Click(icon.NewTexture, (image == null) ? "Create new texture2D for " + param : "Replace " + param + " with new Texture2D " + texScale + "*" + texScale, 25).nl())
                                if (isTerrainHeight)
                                    painter.createTerrainHeightTexture(painter.nameHolder);
                                else
                                    painter.createTexture2D(texScale, painter.nameHolder, cfg.newTextureIsColor);



                            if ((image == null) && (cfg.moreOptions) && ("Create Render Texture".Click()))
                                painter.CreateRenderTexture(texScale, painter.nameHolder);

                            if ((image != null) && (cfg.moreOptions)) {
                                if ((image.renderTexture == null) && ("Add Render Tex".Click()))
                                    image.AddRenderTexture();
                                if (image.renderTexture != null) {

                                    if ("Replace RendTex".Click("Replace " + param + " with Rend Tex size: " + texScale))
                                        painter.CreateRenderTexture(texScale, painter.nameHolder);
                                    if ("Remove RendTex".Click().nl()) {
                                        if (image.texture2D != null) {
                                            painter.updateOrChangeDestination(texTarget.Texture2D);
                                            image.renderTexture = null;
                                        } else {
                                            painter.curImgData = null;
                                            painter.setTextureOnMaterial();
                                        }

                                    }
                                }
                            }
                        } else
                            "No Material's Texture selected".nl();

                        pegi.nl();

                        if (image == null)
                            "_Name:".edit("Name for new texture", 40, ref painter.nameHolder);


                        if (!isTerrainHeight) {
                            "Color:".toggle("Will the new texture be a Color Texture", 40, ref cfg.newTextureIsColor);
                            ef.write("Size:", "Size of the new Texture", 40);
                            textureSizeSelect();
                        }
                        ef.newLine();
                    }

                    ef.newLine();
                    ef.tab();
                    ef.newLine();

                    List<Texture> recentTexs;

                    string texName = painter.getMaterialTextureName();

                    if ((texName != null) && (rtp.recentTextures.TryGetValue(texName, out recentTexs))
                        && ((recentTexs.Count > 1) || (painter.curImgData == null))) {
                        ef.write("Recent Texs:", 60);
                        Texture tmp = painter.curImgData.exclusiveTexture();
                        if (pegi.select(ref tmp, recentTexs))
                            painter.ChangeTexture(tmp);
                    }

                    ef.Space();
                    ef.newLine();
                    ef.Space();
                }

            } else {
                painter.meshRenderer = (Renderer)EditorGUILayout.ObjectField(painter.meshRenderer, typeof(Renderer), true);
                if ((painter.meshRenderer != null) && (painter.meshRenderer.gameObject != painter.gameObject)) {
                    painter.meshRenderer = null;
                    Debug.Log("Attach directly to GameObject with Renderer");
                }

            }
            ef.newLine();
        }

        bool textureSetterField() {
            string field = painter.getMaterialTextureName();
            if ((field == null) || (field.Length == 0)) return false;

            Texture tex = painter.getTexture();
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

