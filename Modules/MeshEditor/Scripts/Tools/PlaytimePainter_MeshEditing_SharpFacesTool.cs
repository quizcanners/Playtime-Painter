using QuizCanners.Migration;
using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;

namespace PainterTool.MeshEditing
{
    public class SharpFacesTool : MeshToolBase
    {
        public override string StdTag => "t_shF";

        public static SharpFacesTool inst;

        public SharpFacesTool()
        {
            inst = this;
        }

        public bool _setTo = true;

        #region Inspector

        public override string ToString() => "Dominant Faces";

        public override string Tooltip
        {

            get
            {
                return _detectionMode switch
                {
                    DetectionMode.Points => "Click on points to toggle smoothed normal",
                    DetectionMode.Lines => "Click on lines to set dominants",
                    _ => (
                                                ("Paint the DOMINANCE on triangles {0} It will affect how normal vector will be calculated {0}"
                                                +
                                                "Alt + N on Triangle to flip normals").F(pegi.EnvironmentNl)),
                };
            }

        }

        public override void Inspect()
        {
            "Will Set {0} On Click".F(_setTo).PL().ToggleIcon(ref _setTo).Nl();

            "Mode".ConstL().Edit_Enum(ref _detectionMode).Nl();

            /* if ("Auto Bevel".PegiLabel().Click())
                 AutoAssignDominantNormalsForBeveling();
             "Sensitivity".PegiLabel().edit(60, ref Cfg.bevelDetectionSensitivity, 3, 30).nl();

             if ("Sharp All".PegiLabel().Click())
             {
                 foreach (var vr in EditedMesh.meshPoints)
                     vr.smoothNormal = false;
                 EditedMesh.Dirty = true;
                 Cfg.newVerticesSmooth = false;
             }

             if ("Smooth All".PegiLabel().Click().nl())
             {
                 foreach (var vr in EditedMesh.meshPoints)
                     vr.smoothNormal = true;
                 EditedMesh.Dirty = true;
                 Cfg.newVerticesSmooth = true;
             }*/

        }

        #endregion

        public override bool MouseEventPointedTriangle()
        {
            if (PlaytimePainter_EditorInputManager.GetMouseButton(0))
                EditedMesh.Dirty |= PointedTriangle.SetSharpCorners(_setTo);

            return false;
        }

        public override bool MouseEventPointedLine()
        {
            if (PlaytimePainter_EditorInputManager.GetMouseButton(0))
            {
                foreach (var t in MeshEditorManager.PointedLine.TryGetBothTriangles())
                    EditedMesh.Dirty |= t.SetSharpCorners(_setTo);

            }

            return false;
        }

        public static void AutoAssignDominantNormalsForBeveling()
        {

            foreach (var vr in EditedMesh.meshPoints)
                vr.smoothNormal = true;

            foreach (var t in EditedMesh.triangles) t.SetSharpCorners(true);

            foreach (var t in EditedMesh.triangles)
            {
                var v3S = new Vector3[3];

                for (var i = 0; i < 3; i++)
                    v3S[i] = t.vertexes[i].LocalPos;

                var dist = new float[3];

                for (var i = 0; i < 3; i++)
                    dist[i] = (v3S[(i + 1) % 3] - v3S[(i + 2) % 3]).magnitude;

                for (var i = 0; i < 3; i++)
                {
                    var a = (i + 1) % 3;
                    var b = (i + 2) % 3;
                    if (!(dist[i] < dist[a] / Painter.Data.bevelDetectionSensitivity) ||
                        !(dist[i] < dist[b] / Painter.Data.bevelDetectionSensitivity)) continue;

                    t.SetSharpCorners(false);

                    var other = (new PainterMesh.LineData(t, t.vertexes[a], t.vertexes[b])).GetOtherTriangle();
                    other?.SetSharpCorners(false);
                }
            }

            EditedMesh.Dirty = true;
        }

        public override void KeysEventPointedTriangle()
        {

            if (KeyCode.N.IsDown())
            {
                if (!PlaytimePainter_EditorInputManager.Alt)
                {
                    var no = PointedTriangle.NumberOf(PointedTriangle.GetClosestTo(MeshPainting.collisionPosLocal));
                    PointedTriangle.isPointDominant[no] = !PointedTriangle.isPointDominant[no];
                    (PointedTriangle.isPointDominant[no] ? "Triangle edge's Normal is now dominant" : "Triangle edge Normal is NO longer dominant").TeachingNotification();
                }
                else
                {
                    PointedTriangle.InvertNormal();
                    "Flipping Normals".TeachingNotification();
                }

                EditedMesh.Dirty = true;
            }
        }

        public override bool MouseEventPointedVertex()
        {
            if (PlaytimePainter_EditorInputManager.GetMouseButton(0))
            {

                var mp = MeshEditorManager.PointedUv.meshPoint;

                if (mp.smoothNormal != PlaytimePainter_EditorInputManager.Alt)
                {
                    mp.smoothNormal = !mp.smoothNormal;
                    EditedMesh.Dirty = true;
                    "N - on Vertex - smooth Normal".TeachingNotification();
                }

                // m.PointedUV.meshPoint.smoothNormal = !m.PointedUV.meshPoint.smoothNormal;
                //EditedMesh.Dirty = true;
            }
            return false;
        }


        #region Encode & Decode

        public override CfgEncoder Encode() => new CfgEncoder()
            .Add("dm", (int)_detectionMode);

        public override void DecodeTag(string key, CfgData data)
        {
            switch (key)
            {
                case "dm": _detectionMode = (DetectionMode)data.ToInt(); break;
            }
        }

        #endregion

    }

}
