using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;

namespace Painter {

    public static class PainterPEGI_Extensions {

        static PainterConfig cfg { get { return PainterConfig.inst; } }
        static PainterManager rtp { get { return PainterManager.inst; } }


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


        public static bool BrushForTargets_PEGI(this BrushConfig brush) {
            bool changed = false;

            if (pegi.Click(brush.TargetIsTex2D ? icon.CPU.getIcon() : icon.GPU.getIcon(),
                brush.TargetIsTex2D ? "Render Texture Config" : "Texture2D Config", 45)) {
                brush.TargetIsTex2D = !brush.TargetIsTex2D;
                changed = true;
            }

            if (brush.TargetIsTex2D)
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

            if ((PainterManager.GotBuffers() || (id.renderTexture != null)) && (id.texture2D != null)) {
                if (pegi.Click(cpuBlit ? icon.CPU.getIcon() : icon.GPU.getIcon(),
                    cpuBlit ? "Switch to Render Texture" : "Switch to Texture2D", 45)) {
                    painter.UpdateOrSetTexTarget(cpuBlit ? texTarget.RenderTexture : texTarget.Texture2D);
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
                        PainterConfig.inst.brushConfig.MaskSet(BrushMask.A, true);
                    
                    if (tht.getImgData() != null)
                        if ((painter.originalShader == null) && (pegi.Click(icon.OriginalShader.getIcon(),  "Applies changes made in Unity terrain Editor", 45)))
                        {
                            painter.Unity_To_Preview();

                           
                            painter.SetPreviewShader();

                            changed = true;
                        }
                }  
            }
            else
            {



                if ((painter.originalShader == null) && (pegi.Click(icon.OriginalShader.getIcon(), "Switch To Preview Shader", 45)))
                {
                    
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