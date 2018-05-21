using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace Playtime_Painter {

    #if !NO_PEGI

    public static class PainterPEGI_Extensions {

        static PainterConfig cfg { get { return PainterConfig.inst; } }
        static PainterManager rtp { get { return PainterManager.inst; } }

        public static bool SelectTexture_PEGI(this PlaytimePainter p) {
        int ind = p.selectedTexture;
        if (pegi.select(ref ind, p.GetMaterialTextureNames())) {
            p.SetOriginalShaderOnThis();
            p.selectedTexture = ind;
            p.OnChangedTexture_OnMaterial();
            p.CheckPreviewShader();
            return true;
        }
        return false;
    }

        public static bool NewTextureOptions_PEGI(this PlaytimePainter p) {
            bool changes = false;

            if (p.imgData != null) return changes;

        if (p.MaterialTexturePropertyName == null) {
            pegi.write("This material has no textures");
            pegi.newLine();
                return changes;
        }

        bool color = pegi.Click(icon.NewTexture.getIcon(), "New Texture" ,25);
        if (pegi.Click("Create Mask") || color) {
            List<string> texes = p.GetMaterialTextureNames();
                if (texes.Count > 0) {
                    p.createTexture2D(256, "New " + p.MaterialTexturePropertyName, color);
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
                  
                        if ((!painter.isOriginalShader) && (pegi.Click(icon.PreviewShader.getIcon(), "Applies changes made on Texture to Actual physical Unity Terrain.", 45)))
                        {
                            painter.Preview_To_UnityTerrain();
                            painter.Unity_To_Preview(); 

                            painter.matDta.usePreviewShader = false;
                            painter.SetOriginalShaderOnThis();

                            changed = true;
                        }
                        PainterConfig.inst.brushConfig.MaskSet(BrushMask.A, true);
                    
                    if (tht.getImgData() != null)
                        if ((painter.isOriginalShader) && (pegi.Click(icon.OriginalShader.getIcon(),  "Applies changes made in Unity terrain Editor", 45))) {
                            painter.Unity_To_Preview();
                            
                            painter.SetPreviewShader();

                            changed = true;
                        }
                }  
            } else {
                
                if ((painter.isOriginalShader ) && (pegi.Click(icon.OriginalShader.getIcon(), "Switch To Preview Shader", 45)))
                {
                    
                    painter.SetPreviewShader();
                    changed = true;
                }

                if ((!painter.isOriginalShader) && (pegi.Click(icon.PreviewShader.getIcon(), "Return to Original Shader", 45)))
                {
                    painter.matDta.usePreviewShader = false;
                    painter.SetOriginalShaderOnThis();
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


                var id = trg.imgData;

                bool gotVectors = cfg.recordingNames.Count > 0;

                cfg.browsedRecord = Mathf.Max(0, Mathf.Min(cfg.browsedRecord, cfg.recordingNames.Count - 1));

                if (gotVectors) {
                    pegi.select(ref cfg.browsedRecord, cfg.recordingNames);
                    if (pegi.Click(icon.Play, "Play stroke vectors on current mesh", 18)) {
                        trg.PlayByFilename(cfg.recordingNames[cfg.browsedRecord]);
                        changed = true;
                    }
                    if (pegi.Click(icon.Record, "Continue Recording", 18)) {
                        id.SaveName = cfg.recordingNames[cfg.browsedRecord];
                        id.ContinueRecording();
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
                    id.SaveName = "Unnamed";
                    id.StartRecording();
                    "Recording started".showNotification();
                }

            }

        pegi.newLine();
            pegi.Space();
            pegi.newLine();
        return changed;
    }

        public static void TeachingNotification (this string text) {
#if !NO_PEGI
            if (cfg.ShowTeachingNotifications)
                text.showNotification();
#endif
        }

}
#endif
}