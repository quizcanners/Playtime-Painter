using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace Playtime_Painter {

    #if !NO_PEGI

    public static class PainterPEGI_Extensions {

        static PainterDataAndConfig Cfg { get { return PainterCamera.Data; } }
        static PainterCamera TexMGMT { get { return PainterCamera.Inst; } }

        public static bool SelectTexture_PEGI(this PlaytimePainter p) {
        int ind = p.SelectedTexture;
        if (pegi.select(ref ind, p.GetMaterialTextureNames())) {
            p.SetOriginalShaderOnThis();
            p.SelectedTexture = ind;
            p.OnChangedTexture_OnMaterial();
            p.CheckPreviewShader();
            return true;
        }
        return false;
    }

        public static bool NewTextureOptions_PEGI(this PlaytimePainter p) {
            bool changes = false;

            if (p.ImgData != null) return changes;

        if (p.MaterialTexturePropertyName == null) {
            pegi.write("This material has no textures");
            pegi.newLine();
                return changes;
        }

        bool color = pegi.Click(icon.NewTexture.getIcon(), "New Texture" ,25);
        if (pegi.Click("Create Mask") || color) {
            List<string> texes = p.GetMaterialTextureNames();
                if (texes.Count > 0) {
                    p.CreateTexture2D(256, "New " + p.MaterialTexturePropertyName, color);
                    changes = true;
                }
            
        }

            return changes;
    }

        public static bool PreviewShaderToggle_PEGI(this PlaytimePainter painter) {

            bool changed = false;
            if (painter.IsTerrainHeightTexture())
            {
                Texture tht = painter.terrainHeightTexture;

                if (tht != null) {
                  
                        if ((!painter.IsOriginalShader) && (pegi.Click(icon.PreviewShader.getIcon(), "Applies changes made on Texture to Actual physical Unity Terrain.", 45)))
                        {
                            painter.Preview_To_UnityTerrain();
                            painter.Unity_To_Preview(); 

                            painter.MatDta.usePreviewShader = false;
                            painter.SetOriginalShaderOnThis();

                            changed = true;
                        }
                    PainterCamera.Data.brushConfig.MaskSet(BrushMask.A, true);
                    
                    if (tht.GetImgData() != null)
                        if ((painter.IsOriginalShader) && (pegi.Click(icon.OriginalShader.getIcon(),  "Applies changes made in Unity terrain Editor", 45))) {
                            painter.Unity_To_Preview();
                            
                            painter.SetPreviewShader();

                            changed = true;
                        }
                }  
            } else {
                
                if ((painter.IsOriginalShader ) && (pegi.Click(icon.OriginalShader.getIcon(), "Switch To Preview Shader", 45)))
                {
                    
                    painter.SetPreviewShader();
                    changed = true;
                }

                if ((!painter.IsOriginalShader) && (pegi.Click(icon.PreviewShader.getIcon(), "Return to Original Shader", 45)))
                {
                    painter.MatDta.usePreviewShader = false;
                    painter.SetOriginalShaderOnThis();
                    changed = true;
                }
            }
			return changed;

	}

        public static bool Playback_PEGI(this PlaytimePainter trg) {
        bool changed = false;
        pegi.newLine();

            if (PlaytimePainter.cody.GotData) {
                "Playback In progress".nl();

                if (icon.Close.Click("Cancel All Playbacks",20))
                    TexMGMT.CancelAllPlaybacks();

                if (StrokeVector.PausePlayback) {
                    if (icon.Play.Click("Continue Playback", 20))
                        StrokeVector.PausePlayback = false;
                } else if (icon.Pause.Click("Pause Playback",20))
                        StrokeVector.PausePlayback = true;
                
            } else {


                var id = trg.ImgData;

                bool gotVectors = Cfg.recordingNames.Count > 0;

                Cfg.browsedRecord = Mathf.Max(0, Mathf.Min(Cfg.browsedRecord, Cfg.recordingNames.Count - 1));

                if (gotVectors) {
                    pegi.select(ref Cfg.browsedRecord, Cfg.recordingNames);
                    if (icon.Play.Click("Play stroke vectors on current mesh", 18)) {
                        trg.PlayByFilename(Cfg.recordingNames[Cfg.browsedRecord]);
                        changed = true;
                    }
                    if (icon.Record.Click("Continue Recording", 18)) {
                        id.SaveName = Cfg.recordingNames[Cfg.browsedRecord];
                        id.ContinueRecording();
                        "Recording resumed".showNotification();
                    }

                    if (icon.Delete.Click("Delete", 18)) {
                        changed = true;
                        Cfg.RemoveRecord();
                    }
                }

                if ((gotVectors && icon.Add.Click("Start new Vector recording", 18)) || 
                    (!gotVectors && "New Vector Recording".Click("Start New recording")
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
            if (Cfg.ShowTeachingNotifications)
                text.showNotification();
        }

}
#endif
}