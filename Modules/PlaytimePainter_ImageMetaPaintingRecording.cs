using System.Collections.Generic;
using QuizCanners.Inspect;
using QuizCanners.Migration;
using QuizCanners.Utils;
using UnityEngine;

namespace PainterTool
{
    [TaggedTypes.Tag(CLASS_KEY)]
    internal class ImageMetaPaintingRecording : ImageMetaModuleBase
    {
        private const string CLASS_KEY = "Plbk";

        public override string ClassTag => CLASS_KEY;

        internal static readonly List<TextureMeta> playbackMetas = new();

        public static CfgDecoder cody = new("");

        public static List<string> playbackVectors = new();

        private void PlayByFilename(string recordingName) {
            if (!playbackMetas.Contains(parentMeta))
                playbackMetas.Add(parentMeta);
            Stroke.pausePlayback = false;
            playbackVectors.AddRange(Painter.Data.StrokeRecordingsFromFile(recordingName));

        }

        public List<string> recordedStrokes = new();
        public List<string> recordedStrokesForUndoRedo = new(); // to sync strokes recording with Undo Redo
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
            recordedStrokes.AddRange(Painter.Data.StrokeRecordingsFromFile(parentMeta.saveName));
        }

        public void SaveRecording() {

            var allStrokes = new CfgEncoder().Add("strokes", recordedStrokes).ToString();

            QcFile.Save.ToPersistentPath.String(subPath: Painter.Data.vectorsFolderName, fileName: parentMeta.saveName, data: allStrokes);

            Painter.Data.recordingNames.Add(parentMeta.saveName);

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
            foreach (var _ in playbackMetas)
                playbackVectors.Clear();

            playbackMetas.Clear();

            cody = new CfgDecoder(null);
        }

        public override void OnPainting(PainterComponent painter) => OnPaintingDrag(painter);
        
        public override void ManagedUpdate()
        {
            if (!playbackMetas.TryGetLast(out var el) || Stroke.pausePlayback) return;

            if (el == null)
            {
                playbackMetas.RemoveLast(1);
                return;
            }
           
            var last = el.GetModule<ImageMetaPaintingRecording>();

            if (last == null) return;

            if (cody.GotData)
                DecodeStroke(cody.GetNextTag(), cody.GetData());
            else
            {
                if (playbackVectors.Count > 0)
                {
                    cody = new CfgDecoder(playbackVectors[0]);
                    playbackVectors.RemoveAt(0);
                }
                else
                    playbackMetas.Remove(parentMeta);
            }
        }

        public override void OnPaintingDrag(PainterComponent painter)
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

                    size /= parentMeta.Width;

                    var dir = stroke.uvTo - _lastUv;

                    var dot = Vector2.Dot(dir.normalized, _prevDir);

                    canRecord |= (_strokeDistance > size * 5) || 
                                 (_strokeDistance * parentMeta.Width > 10) ||
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
            recordedStrokes.AddRange(backup.strokeRecord);
            recordedStrokesForUndoRedo = backup.strokeRecord;
        }

        public override void OnTextureBackup(PaintingUndoRedo.TextureBackup backup) {

            backup.strokeRecord = recordedStrokesForUndoRedo;
            recordedStrokesForUndoRedo = new List<string>();
            
        }

        #region Inspect

        public override void ShowHideSectionInspect()
        {
            "Recording/Playback".PL("Show options for brush recording").ToggleIcon(
                ref showRecording).Nl();
        }

        public override void BrushConfigPEGI(PainterComponent painter)
        {

            if (showRecording && !recording)
            {
                var cfg = Painter.Data;

                if (!cfg)
                    return;


                pegi.Nl();

                if (playbackMetas.Count > 0)
                {
                    "Playback In progress".PL().Nl();

                    if (Icon.Close.Click("Cancel All Playbacks", 20))
                        CancelAllPlaybacks();

                    if (Stroke.pausePlayback)
                    {
                        if (Icon.Play.Click("Continue Playback", 20))
                            Stroke.pausePlayback = false;
                    }
                    else if (Icon.Pause.Click("Pause Playback", 20))
                        Stroke.pausePlayback = true;

                }
                else
                {
                    var gotVectors = cfg.recordingNames.Count > 0;

                    cfg.browsedRecord = Mathf.Max(0,
                        Mathf.Min(cfg.browsedRecord, cfg.recordingNames.Count - 1));

                    if (gotVectors)
                    {
                        pegi.Select(ref cfg.browsedRecord, cfg.recordingNames);
                        if (Icon.Play.Click("Play stroke vectors on current mesh", 18))
                            PlayByFilename(cfg.recordingNames[cfg.browsedRecord]);

                        if (Icon.Record.Click("Continue Recording", 18))
                        {
                            parentMeta.saveName = cfg.recordingNames[cfg.browsedRecord];
                            ContinueRecording();
                            pegi.GameView.ShowNotification("Recording resumed");
                        }

                        if (Icon.Delete.Click("Delete", 18))
                            cfg.recordingNames.RemoveAt(cfg.browsedRecord);

                    }

                    if ((gotVectors && Icon.Add.Click("Start new Vector recording", 18)) ||
                        (!gotVectors && "New Vector Recording".PL("Start New recording").Click()))
                    {
                        parentMeta.saveName = "Unnamed";
                        StartRecording();
                        pegi.GameView.ShowNotification("Recording started");
                    }
                }

                pegi.Nl();
                pegi.Space();
                pegi.Nl();
            }


            if (recording)
            {
                ("Recording... " + recordedStrokes.Count + " vectors").PL().Nl();
                "Will Save As".ConstL().Edit(ref parentMeta.saveName);

                if (Icon.Close.Click("Stop, don't save"))
                    recording = false;
                if (Icon.Done.Click("Finish & Save"))
                    SaveRecording();

                pegi.Nl();
            }
        }

#endregion

#region Encoding


        public override void DecodeTag(string key, CfgData data)
        {
            switch (key)
            {
                case "rec": showRecording = data.ToBool(); break;
            }
        }

        public override CfgEncoder Encode()
            => new CfgEncoder().Add_IfTrue("rec", showRecording);

        
        public CfgEncoder EncodeStroke(PainterComponent painter) {
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
        
        public void DecodeStroke(string data, PainterComponent painter)
        {
            currentlyDecodedPainter = painter;

            new CfgDecoder(data).DecodeTagsFor(DecodeStroke);
        }

        private PainterComponent currentlyDecodedPainter;

        private void DecodeStroke(string tg, CfgData data) {

            switch (tg) {
                case "trg": 
                    currentlyDecodedPainter.UpdateOrSetTexTarget(data.ToString().Equals("C") ? TexTarget.Texture2D : TexTarget.RenderTexture); break;
                case "brush":
                    GlobalBrush.Decode(data);
                    GlobalBrush.brush2DRadius *= parentMeta?.Width ?? 256; break;
                case "s":
                    currentlyDecodedPainter.stroke.Decode(data);
                    GlobalBrush.Paint(currentlyDecodedPainter.Command);
                    break;
            }
        }


#endregion
    }
}
