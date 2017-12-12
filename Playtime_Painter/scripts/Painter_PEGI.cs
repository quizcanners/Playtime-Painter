using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;

namespace TextureEditor {

    public static class PainterPEGI_Extensions {

        static painterConfig cfg { get { return painterConfig.inst(); } }
        static RenderTexturePainter rtp { get { return RenderTexturePainter.inst; } }


        public static bool Mode_Type_PEGI(this BrushConfig brush, bool cpuBlit) {
            bool changed = false;
            pegi.newLine();

            pegi.write(msg.BlitMode.Get(), "How final color will be calculated", 80);

            brush.currentBlitMode();

            changed |= pegi.select(ref brush.bliTMode, BlitMode.allModes.ToArray(), true);

            pegi.newLine();
            pegi.Space();
            pegi.newLine();

            if (!cpuBlit) {

                pegi.write(msg.BrushType.Get(), 80);
                changed |= pegi.select<BrushType>(ref brush.brushType_rt, BrushType.allTypes);

                changed |= brush.currentBrushTypeRT().PEGI(brush);

            }

            pegi.newLine();

            return changed;
        }


        public static bool BrushIndependentTargets_PEGI(this BrushConfig brush) {
            bool changed = false;

            if (pegi.Click(brush.IndependentCPUblit ? icon.CPU.getIcon() : icon.GPU.getIcon(),
                brush.IndependentCPUblit ? "Render Texture Config" : "Texture2D Config", 45)) {
                brush.IndependentCPUblit = !brush.IndependentCPUblit;
                changed = true;
            }

            if (brush.IndependentCPUblit)
                changed |= pegi.toggle(ref brush.Smooth,
                        icon.Round.getIcon(), icon.Square.getIcon(), "Smooth/Pixels Brush", 45);

            return changed;
        }

        public static bool BrushParameters_PEGI(this BrushConfig brush, PlaytimePainter painter) {


            if ((painter.skinnedMeshRendy != null) && (pegi.Click("Update Collider from Skinned Mesh")))
                painter.UpdateColliderForSkinnedMesh();
            pegi.newLine();


            imgData id = painter.curImgData;

            bool changed = false;
            bool cpuBlit = id.destination == texTarget.Texture2D;


            pegi.newLine();




            changed |= painter.PreviewShaderToggle_PEGI();

            if ((RenderTexturePainter.GotBuffers() || (id.renderTexture != null)) && (id.texture2D != null)) {
                if (pegi.Click(cpuBlit ? icon.CPU.getIcon() : icon.GPU.getIcon(),
                    cpuBlit ? "Switch to Render Texture" : "Switch to Texture2D", 45)) {
                    painter.updateOrChangeDestination(cpuBlit ? texTarget.RenderTexture : texTarget.Texture2D);
                    changed = true;
                }
            }

            if (cpuBlit)
                changed |= pegi.toggle(ref brush.Smooth,
                        icon.Round.getIcon(), icon.Square.getIcon(), "Smooth/Pixels Brush", 45);

            pegi.newLine();
            if ((painter.originalShader != null) && (cfg.moreOptions))
                changed |= pegi.toggle(ref cfg.previewAlphaChanel, "Preview Enabled Chanels", 130);



            if (brush.Mode_Type_PEGI(cpuBlit)) {
                if (brush.currentBrushTypeRT().GetType() == typeof(BrushTypeDecal))
                    brush.MaskSet(BrushMask.A, true);
                changed = true;
            }


            BlitMode blitMode = BlitMode.getCurrentBlitModeForPainter(painter);

            if (painter.terrain != null) {

                if ((painter.curImgData != null) && ((painter.isTerrainHeightTexture())) && (painter.originalShader == null))
                    pegi.writeWarning(" You need to use Preview Shader to see changes");

                pegi.newLine();

                if ((painter.terrain != null) && (pegi.Click("Update Terrain")))
                    painter.UpdateShaderGlobalVariables();

                pegi.newLine();


            }




            changed |= blitMode.PEGI(brush, painter);

            BlitMode mode = brush.currentBlitMode();
            imgData image = painter.curImgData;


            if ((mode.usingSourceTexture) && (image.TargetIsRenderTexture())) {
                if (rtp.sourceTextures.Length > 0) {
                    brush.selectedSourceTexture = Mathf.Min(brush.selectedSourceTexture, rtp.sourceTextures.Length - 1);
                    pegi.write("Copy From:", 70);
                    pegi.selectOrAdd(ref brush.selectedSourceTexture, ref rtp.sourceTextures);
                } else
                    pegi.write("Add Textures to Render Camera to copy from");
            }

            pegi.newLine();






            return changed;
        }

        public static bool config_PEGI(this PlaytimePainter painter) {

            BrushConfig brush = painterConfig.inst().brushConfig;
            imgData id = painter.curImgData;


            if ("More options".toggle(80, ref cfg.moreOptions).nl())
                cfg.showConfig = false;

            (rtp.isLinearColorSpace ? "Project is Linear Color Space" : "Project is in Gamma Color Space").nl();

            bool changed = false;

            "repaint delay".nl("Delay for video memory update when painting to Texture2D", 100);

            changed |= pegi.edit(ref brush.repaintDelay, 0.01f, 0.5f).nl();

            changed |= "Don't update mipmaps:".toggle("May increase performance, but your changes may not disaplay if you are far from texture.", 150,
                                                       ref brush.DontRedoMipmaps).nl();

            if ((id != null) && (brush.DontRedoMipmaps) && ("Redo Mipmaps".Click().nl()))
                id.SetAndApply(true);

            bool gotBacups = (painter.numberOfTexture2Dbackups + painter.numberOfRenderTextureBackups) > 0;

                if (gotBacups) {

                pegi.writeOneTimeHint("Creating more backups will eat more memory", "backupIsMem");
                pegi.writeOneTimeHint("This are not connected to Unity's " +
                                          "Undo/Redo because when you run out of backups you will by accident start undoing other stuff.", "noNativeUndo");
                pegi.writeOneTimeHint("Use Z/X to undo/redo", "ZXundoRedo");

                changed |=
                "texture2D UNDOs:".edit(150, ref painter.numberOfTexture2Dbackups).nl() ||
                "renderTex UNDOs:".edit(150, ref painter.numberOfRenderTextureBackups).nl() ||

                "backup manually:".toggle(150, ref painter.backupManually).nl();
                } else if ("Enable Undo/Redo".Click().nl()) {
                    
                    painter.numberOfTexture2Dbackups = 10;
                    painter.numberOfRenderTextureBackups = 10;
                    
                }

          


            if ("Don't create render texture buffer:".toggle(ref cfg.dontCreateDefaultRenderTexture).nl()) {
                    painterConfig.SaveChanges();
                    rtp.UpdateBuffersState();
                }
        
                bool gotDefine = pegi.GetDefine(painterConfig.enablePainterForBuild);
            if ("Enable texture editor for build".toggle(ref gotDefine).nl())
                pegi.SetDefine(painterConfig.enablePainterForBuild, gotDefine);
            


            "Disable Non-Mesh Colliders in Play Mode:".toggle(ref cfg.disableNonMeshColliderInPlayMode).nl();
              
            "Save Textures To:".edit(110, ref cfg.texturesFolderName).nl();

            "Save Materials To:".edit(110, ref cfg.materialsFolderName).nl();
                                      

             
            if ("Edit scene light".foldout().nl()) {
                    
                    Color tmp = RenderSettings.ambientLight / RenderSettings.ambientIntensity;

                    "ambient".edit(ref tmp).nl();


                 
                    float intensity = RenderSettings.ambientIntensity;
                if ("Intensity:".edit(ref intensity, 0.1f, 10f).nl()) RenderSettings.ambientIntensity = intensity;

                    RenderSettings.ambientLight = tmp * RenderSettings.ambientIntensity;
                }

            "Camera".write(rtp.rtcam); pegi.newLine();

            "Brush".write(rtp.brushPrefab); pegi.newLine();

            "Renderer to Debug second buffer".edit(ref rtp.secondBufferDebug).nl();

            if ("Report a bug".Click())
                PlaytimePainter.openEmail();
            if ("Forum".Click())
                PlaytimePainter.openForum();
            if ("Manual".Click().nl())
                PlaytimePainter.openDocumentation();



		return changed;
	}

    public static bool SelectTexture_PEGI(this PlaytimePainter CurrentTarget) {
        int ind = CurrentTarget.selectedTexture;
        if (pegi.select(ref ind, CurrentTarget.getMaterialTextureNames())) {
            CurrentTarget.selectedTexture = ind;
            CurrentTarget.OnChangedTexture();
            return true;
        }
        return false;
    }

    public static bool NewTextureOptions_PEGI(this PlaytimePainter trg) {
            bool changes = false;

            if (trg.curImgData != null) return changes;

        if (trg.getMaterialTextureName() == null) {
            pegi.write("This material has no textures");
            pegi.newLine();
                return changes;
        }

        bool color = pegi.Click(icon.NewTexture.getIcon(), "New Texture" ,25);
        if (pegi.Click("Create Mask") || color) {
            List<string> texes = trg.getMaterialTextureNames();
                if (texes.Count > 0) {
                    trg.createTexture2D(256, "New " + trg.getMaterialTextureName(), color);
                    changes = true;
                }
            
        }

            return changes;
    }

    public static bool PreviewShaderToggle_PEGI(this PlaytimePainter painter) {

            bool changed = false;
            if (painter.isTerrainHeightTexture())
            {
                Texture tht = painter.terrainHeightTexture;

                if (tht != null) {
                  
                        if ((painter.originalShader != null) && (pegi.Click(icon.PreviewShader.getIcon(), "Applies changes made on Texture to Actual physical Unity Terrain.", 45)))
                        {
                            painter.Preview_To_UnityTerrain();
                            painter.Unity_To_Preview(); // To update normals on Preview Texture

                            painter.usePreviewShader = false;
                            painter.SetOriginalShader();

                            changed = true;
                        }
                        painterConfig.inst().brushConfig.MaskSet(BrushMask.A, true);
                    
                    if (tht.getImgData() != null)
                        if ((painter.originalShader == null) && (pegi.Click(icon.OriginalShader.getIcon(),  "Applies changes made in Unity terrain Editor", 45)))
                        {
                            painter.Unity_To_Preview();

                            painter.usePreviewShader = true;
                            painter.SetPreviewShader();

                            changed = true;
                        }
                }  
            }
            else
            {



                if ((painter.originalShader == null) && (pegi.Click(icon.OriginalShader.getIcon(), "Switch To Preview Shader", 45)))
                {
                    painter.usePreviewShader = true;
                    painter.SetPreviewShader();
                    changed = true;
                }

                if ((painter.originalShader != null) && (pegi.Click(icon.PreviewShader.getIcon(), "Return to Original Shader", 45)))
                {
                    painter.usePreviewShader = false;
                    painter.SetOriginalShader();
                    changed = true;
                }
            }
			return changed;

	}

    public static bool Playback_PEGI(this PlaytimePainter trg) {
        bool changed = false;
        pegi.newLine();

            if (PlaytimePainter.cody.gotData) {
                "Playback In progress".nl();

                if (icon.Close.Click("Cancel All Playbacks",20))
                    rtp.CancelAllPlaybacks();

                if (StrokeVector.PausePlayback) {
                    if (icon.Play.Click("Continue Playback", 20))
                        StrokeVector.PausePlayback = false;
                } else {
                    if (icon.Pause.Click("Pause Playback",20))
                        StrokeVector.PausePlayback = true;
                }
                    

            } else {



                bool gotVectors = cfg.recordingNames.Count > 0;

                cfg.browsedRecord = Mathf.Max(0, Mathf.Min(cfg.browsedRecord, cfg.recordingNames.Count - 1));

                if (gotVectors) {
                    pegi.select(ref cfg.browsedRecord, cfg.recordingNames);
                    if (pegi.Click(icon.Play, "Play stroke vectors on current mesh", 18)) {
                        trg.PlayByFilename(cfg.recordingNames[cfg.browsedRecord]);
                        changed = true;
                    }
                    if (pegi.Click(icon.Record, "Continue Recording", 18)) {
                        trg.curImgData.SaveName = cfg.recordingNames[cfg.browsedRecord];
                        trg.curImgData.ContinueRecording();
                    }

                    if (pegi.Click(icon.Delete, "Delete", 18)) {
                        changed = true;
                        cfg.RemoveRecord();
                    }
                }

                if ((gotVectors && (pegi.Click(icon.Add, "Start new Vector recording", 18))) || 
                    ((!gotVectors) && ("New Vector Recording").Click("Start New recording")
                    )) {
                    trg.curImgData.SaveName = "Unnamed";
                    trg.curImgData.StartRecording();
                }

            }

        pegi.newLine();
            pegi.Space();
            pegi.newLine();
        return changed;
    }

}
}