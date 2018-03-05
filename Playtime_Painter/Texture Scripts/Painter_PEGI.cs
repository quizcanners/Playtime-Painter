using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;

namespace Playtime_Painter {

    public static class PainterPEGI_Extensions {

        static PainterConfig cfg { get { return PainterConfig.inst; } }
        static PainterManager rtp { get { return PainterManager.inst; } }

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
                            painter.Unity_To_Preview(); 

                            painter.usePreviewShader = false;
                            painter.SetOriginalShader();

                            changed = true;
                        }
                        PainterConfig.inst.brushConfig.MaskSet(BrushMask.A, true);
                    
                    if (tht.getImgData() != null)
                        if ((painter.originalShader == null) && (pegi.Click(icon.OriginalShader.getIcon(),  "Applies changes made in Unity terrain Editor", 45))) {
                            painter.Unity_To_Preview();
                            
                            painter.SetPreviewShader();

                            changed = true;
                        }
                }  
            } else {
                
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
                        "Recording resumed".showNotification();
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
                    "Recording started".showNotification();
                }

            }

        pegi.newLine();
            pegi.Space();
            pegi.newLine();
        return changed;
    }

        public static void TeachingNotification (this string text) {
            if (cfg.ShowTeachingNotifications)
                text.showNotification();
        }

}
}