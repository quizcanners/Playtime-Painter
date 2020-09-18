using System.Collections.Generic;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using UnityEngine;

namespace PlaytimePainter
{
    [TaggedType(tag)]
    public class ImageMetaPaintingRecording : ImageMetaModuleBase
    {
        const string tag = "Plbk";

        public override string ClassTag => tag;

        public static readonly List<TextureMeta> playbackMetas = new List<TextureMeta>();

        public static CfgDecoder cody = new CfgDecoder("");

        public static List<string> playbackVectors = new List<string>();

        private void PlayByFilename(string recordingName) {
            if (!playbackMetas.Contains(parentMeta))
                playbackMetas.Add(parentMeta);
            Stroke.pausePlayback = false;
            playbackVectors.AddRange(PainterCamera.Data.StrokeRecordingsFromFile(recordingName));

        }

        public List<string> recordedStrokes = new List<string>();
        public List<string> recordedStrokesForUndoRedo = new List<string>(); // to sync strokes recording with Undo Redo
        public bool recording;

        public void StartRecording()
        {
            recordedStrokes = new List<string>();
            recordedStrokesForUndoRedo = new List<string>();
            recording = true;
        }

        public void ContinueRecording()
        {
            StartRecording();
            recordedStrokes.AddRange(Cfg.StrokeRecordingsFromFile(parentMeta.saveName));
        }

        public void SaveRecording() {

            var allStrokes = new CfgEncoder().Add("strokes", recordedStrokes).ToString();

            QcFile.Save.ToPersistentPath(Cfg.vectorsFolderName, parentMeta.saveName, allStrokes);

            Cfg.recordingNames.Add(parentMeta.saveName);

            recording = false;

        }
        
        public bool showRecording;

        private Vector2 _prevDir;
        private Vector2 _lastUv;
        private Vector3 _prevPosDir;
        private Vector3 _lastPos;

        private float _strokeDistance;

        public static void CancelAllPlaybacks()
        {
            foreach (var p in playbackMetas)
                playbackVectors.Clear();

            playbackMetas.Clear();

            cody = new CfgDecoder(null);
        }

        public override void OnPainting(PlaytimePainter painter) => OnPaintingDrag(painter);
        
        public override void ManagedUpdate() {

            var l = playbackMetas;

            if (playbackMetas.Count > 0 && !Stroke.pausePlayback) {

                if (playbackMetas.TryGetLast() == null)
                    playbackMetas.RemoveLast(1);
                else
                {
                   var last = playbackMetas.TryGetLast().GetModule<ImageMetaPaintingRecording>();

                   if (last != null) {
                       if (cody.GotData)
                           DecodeStroke(cody.GetNextTag(), cody.GetData());
                       else {
                           if (playbackVectors.Count > 0) {
                               cody = new CfgDecoder(playbackVectors[0]);
                               playbackVectors.RemoveAt(0);
                           }
                           else
                               playbackMetas.Remove(parentMeta);
                       }
                   }
                }

            }
        }

        public override void OnPaintingDrag(PlaytimePainter painter)
        {

            if (!recording)
                return;

            var stroke = painter.stroke;

            if (stroke.MouseDownEvent)
            {
                _prevDir = Vector2.zero;
                _prevPosDir = Vector3.zero;
            }

            var canRecord = stroke.MouseDownEvent || stroke.MouseUpEvent;

            var worldSpace = painter.Is3DBrush();

            if (!canRecord)
            {

                var size = GlobalBrush.Size(worldSpace);

                if (worldSpace)
                {
                    var dir = stroke.posTo - _lastPos;

                    var dot = Vector3.Dot(dir.normalized, _prevPosDir);

                    canRecord |= (_strokeDistance > size * 10) ||
                        ((dir.magnitude > size * 0.01f) && (_strokeDistance > size) && (dot < 0.9f));

                    var fullDist = _strokeDistance + dir.magnitude;

                    _prevPosDir = (_prevPosDir * _strokeDistance + dir).normalized;

                    _strokeDistance = fullDist;

                }
                else
                {

                    size /= parentMeta.width;

                    var dir = stroke.uvTo - _lastUv;

                    var dot = Vector2.Dot(dir.normalized, _prevDir);

                    canRecord |= (_strokeDistance > size * 5) || 
                                 (_strokeDistance * parentMeta.width > 10) ||
                        ((dir.magnitude > size * 0.01f) && (dot < 0.8f));


                    var fullDist = _strokeDistance + dir.magnitude;

                    _prevDir = (_prevDir * _strokeDistance + dir).normalized;

                    _strokeDistance = fullDist;

                }
            }

            if (canRecord) {

                var hold = stroke.uvTo;
                var holdV3 = stroke.posTo;

                if (!stroke.MouseDownEvent)
                {
                    stroke.uvTo = _lastUv;
                    stroke.posTo = _lastPos;
                }

                _strokeDistance = 0;

                var data = EncodeStroke(painter).ToString();
                recordedStrokes.Add(data);
                recordedStrokesForUndoRedo.Add(data);

                if (!stroke.MouseDownEvent)
                {
                    stroke.uvTo = hold;
                    stroke.posTo = holdV3;
                }

            }

            _lastUv = stroke.uvTo;
            _lastPos = stroke.posTo;


        }
        
        public override void OnUndo(PaintingUndoRedo.TextureBackup backup)
        {
            var toClear = recordedStrokesForUndoRedo.Count;
            
            recordedStrokes.RemoveLast(toClear);

            recordedStrokesForUndoRedo = backup.strokeRecord;

        }

        public override void OnRedo(PaintingUndoRedo.TextureBackup backup) {

            var toClear = recordedStrokesForUndoRedo.Count;
            
            recordedStrokes.AddRange(backup.strokeRecord);
            
            recordedStrokesForUndoRedo = backup.strokeRecord;
        }

        public override void OnTextureBackup(PaintingUndoRedo.TextureBackup backup) {

            backup.strokeRecord = recordedStrokesForUndoRedo;
            recordedStrokesForUndoRedo = new List<string>();
            
        }

        #region Inspect

        public override bool ShowHideSectionInspect()
        {
            bool changed = false;
            
            "Recording/Playback".toggleVisibilityIcon("Show options for brush recording",
                ref showRecording, true).nl(ref changed);
            
            return changed;
        }

        public override bool BrushConfigPEGI(PlaytimePainter painter)
        {
            var changed = false;

            if (showRecording && !recording)
            {
                var cfg = Cfg;

                if (!cfg)
                    return false;


                pegi.nl();

                if (playbackMetas.Count > 0)
                {
                    "Playback In progress".nl();

                    if (icon.Close.Click("Cancel All Playbacks", 20))
                        CancelAllPlaybacks();

                    if (Stroke.pausePlayback)
                    {
                        if (icon.Play.Click("Continue Playback", 20))
                            Stroke.pausePlayback = false;
                    }
                    else if (icon.Pause.Click("Pause Playback", 20))
                        Stroke.pausePlayback = true;

                }
                else
                {
                    var gotVectors = cfg.recordingNames.Count > 0;

                    cfg.browsedRecord = Mathf.Max(0,
                        Mathf.Min(cfg.browsedRecord, cfg.recordingNames.Count - 1));

                    if (gotVectors)
                    {
                        pegi.select(ref cfg.browsedRecord, cfg.recordingNames);
                        if (icon.Play.Click("Play stroke vectors on current mesh", ref changed, 18))
                            PlayByFilename(cfg.recordingNames[cfg.browsedRecord]);

                        if (icon.Record.Click("Continue Recording", 18))
                        {
                            parentMeta.saveName = cfg.recordingNames[cfg.browsedRecord];
                            ContinueRecording();
                            pegi.GameView.ShowNotification("Recording resumed");
                        }

                        if (icon.Delete.Click("Delete", ref changed, 18))
                            cfg.recordingNames.RemoveAt(cfg.browsedRecord);

                    }

                    if ((gotVectors && icon.Add.Click("Start new Vector recording", 18)) ||
                        (!gotVectors && "New Vector Recording".Click("Start New recording")))
                    {
                        parentMeta.saveName = "Unnamed";
                        StartRecording();
                        pegi.GameView.ShowNotification("Recording started");
                    }
                }

                pegi.nl();
                pegi.space();
                pegi.nl();
            }


            if (recording)
            {
                ("Recording... " + recordedStrokes.Count + " vectors").nl();
                "Will Save As ".edit(70, ref parentMeta.saveName);

                if (icon.Close.Click("Stop, don't save"))
                    recording = false;
                if (icon.Done.Click("Finish & Save"))
                    SaveRecording();

                pegi.nl();
            }



            return changed;
        }

#endregion

#region Encoding


        public override bool Decode(string key, string data)
        {
            switch (key)
            {
                case "rec": showRecording = data.ToBool(); break;
                default: return false;
            }

            return true;
        }

        public override CfgEncoder Encode()
            => new CfgEncoder().Add_IfTrue("rec", showRecording);

        
        public CfgEncoder EncodeStroke(PlaytimePainter painter) {
            var encoder = new CfgEncoder();
            
            var stroke = painter.stroke;

            if (stroke.MouseDownEvent)
            {
                encoder.Add("brush", GlobalBrush.EncodeStrokeFor(painter)) // Brush is unlikely to change mid stroke
                .Add_String("trg", parentMeta.TargetIsTexture2D() ? "C" : "G");
            }

            encoder.Add("s", stroke.Encode(parentMeta.TargetIsRenderTexture() && painter.Is3DBrush()));

            return encoder;
        }
        
        public void DecodeStroke(string data, PlaytimePainter painter)
        {
            currentlyDecodedPainter = painter;

            new CfgDecoder(data).DecodeTagsFor(DecodeStroke);
        }

        private PlaytimePainter currentlyDecodedPainter;

        private bool DecodeStroke(string tg, string data) {

            switch (tg) {
                case "trg": currentlyDecodedPainter.UpdateOrSetTexTarget(data.Equals("C") ? TexTarget.Texture2D : TexTarget.RenderTexture); break;
                case "brush":
                    GlobalBrush.Decode(data);
                    GlobalBrush.brush2DRadius *= parentMeta?.width ?? 256; break;
                case "s":
                    currentlyDecodedPainter.stroke.Decode(data);
                    GlobalBrush.Paint(currentlyDecodedPainter.PaintCommand);
                    break;
                default: return false;
            }
            return true;
        }


#endregion
    }
}
