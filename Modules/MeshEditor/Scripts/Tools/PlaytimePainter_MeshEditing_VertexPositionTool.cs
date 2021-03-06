using QuizCanners.Migration;
using QuizCanners.Inspect;
using QuizCanners.Utils;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PlaytimePainter.MeshEditing
{
    public class VertexPositionTool : MeshToolBase
    {
        public override string StdTag => "t_pos";

        private bool _addToTrianglesAndLines;

        private DetectionMode _detectionMode;

        public override bool ShowVertices => _detectionMode == DetectionMode.Points;

        public override bool ShowLines => _detectionMode == DetectionMode.Lines || (_addToTrianglesAndLines && _detectionMode == DetectionMode.Points);

        public override bool ShowTriangles => _detectionMode == DetectionMode.Triangles || (_addToTrianglesAndLines && _detectionMode == DetectionMode.Points);

        private static List<PainterMesh.MeshPoint> _draggedVertices => EditedMesh._draggedVertices; // new List<MeshPoint>();

        private Vector3 _originalPosition;

        public override bool ShowGrid => true;

        public override void AssignText(MarkerWithText markers, PainterMesh.MeshPoint point)
        {

            var selected = MeshMGMT.GetSelectedVertex();

            if (point.vertices.Count > 1 || selected == point)
            {
                if (selected == point)
                {
                    markers.textm.text = (point.vertices.Count > 1) ? (
                        Path.Combine((point.vertices.IndexOf(MeshEditorManager.SelectedUv) + 1).ToString(), point.vertices.Count +
                        (point.smoothNormal ? "s" : ""))

                        ) : "";


                }
                else
                    markers.textm.text = point.vertices.Count +
                        (point.smoothNormal ? "s" : "");
            }
            else markers.textm.text = "";
        }

        public override Color VertexColor => Color.white;

        #region Inspector

        public override string Tooltip => MsgPainter.MeshPointPositionTool.GetDescription();

        /*  ("Alt - Project Vertex To Grid {0}" +
          "LMB - Drag {0} " +
        // "Alt + LMB - Disconnect dragged Vertex from point" +
          "Scroll - Change Plane {0}"+
          "U - make Triangle unique. {0}" + 
          "M - merge with nearest while dragging {0}" +
          "This tool also contains functionality related to smoothing and sharpening of the edges.").F(Environment.NewLine);*/

        public override string GetNameForInspector() => MsgPainter.MeshPointPositionTool.GetText(); // "ADD & MOVE";

        public override void Inspect()
        {

            var changed = pegi.ChangeTrackStart();

            // var mgm = MeshMGMT;

            var sd = Cfg;

            var em = EditedMesh;

            "Mode".editEnum(40, ref _detectionMode).nl();

            if (_detectionMode != DetectionMode.Points)
            {
                "Gizmos needs to be toggled on (if they are off)".writeHint();
            }

            if (MeshEditorManager.MeshTool.ShowGrid)
            {
                "Snap to grid (Use XZ to toggle grid orientation)".toggleIcon(ref sd.snapToGrid).nl();

                if (sd.snapToGrid)
                    "size:".edit(40, ref sd.gridSize);
            }

            pegi.nl();

            "Pixel-Perfect".toggleIcon("New vertex will have UV coordinate rounded to half a pixel.", ref Cfg.pixelPerfectMeshEditing).nl();

            "Insert vertices".toggleIcon("Will split triangles and edges by inserting vertices", ref _addToTrianglesAndLines).nl();

            "Add Unique:".toggleIcon(ref Cfg.newVerticesUnique).nl();

            if ("All shared if same UV".Click("Will only merge vertices if they have same UV").nl())
            {
                em.AllVerticesSharedIfSameUV();
                em.Dirty = true;

            }

            if ("All shared".ClickConfirm(confirmationTag: "AllShrd", toolTip: "Vertices will be merged if they share the position. This may result in loss of UV data."))
            {
                em.AllVerticesShared();
                em.Dirty = true;
                Cfg.newVerticesUnique = false;
            }

            if ("All unique".ClickConfirm(confirmationTag: "AllUnq", toolTip: "If vertex belongs to more then one triangle, a new vertex will be created for each of the extra triangle.").nl())
            {
                foreach (var t in EditedMesh.triangles)
                    em.GiveTriangleUniqueVertices(t);

                Cfg.newVerticesUnique = true;
            }

            "Add Smooth:".toggleIcon(ref Cfg.newVerticesSmooth).nl();

            if ("Sharp All".ClickConfirm(confirmationTag: "AllSrp", toolTip: "Normal vectors will be different for each triangle when possible (when vertex/normal data is not shared with another triangle)."))
            {
                foreach (var vr in EditedMesh.meshPoints)
                    vr.smoothNormal = false;

                EditedMesh.Dirty = true;
                Cfg.newVerticesSmooth = false;
            }

            if ("Smooth All".ClickConfirm(confirmationTag: "AllSmth", toolTip: "All edge normals will be smooth even when a triangle can afford to have a different one.").nl())
            {
                foreach (var vr in EditedMesh.meshPoints)
                    vr.smoothNormal = true;
                EditedMesh.Dirty = true;
                Cfg.newVerticesSmooth = true;
            }


            if ("Auto Bevel".Click())
                SharpFacesTool.AutoAssignDominantNormalsForBeveling();
            "Sensitivity".edit(60, ref Cfg.bevelDetectionSensitivity, 3, 30).nl();

            pegi.nl();
        }

        #endregion

        #region Kyboard

        public override void KeysEventPointedWhatever()
        {
            if (KeyCode.N.IsDown() && PointedTriangle != null)
            {
                PointedTriangle.InvertNormal();
                Dirty = true;
            }
        }

        public override void KeysEventDragging()
        {
            var m = MeshMGMT;
            if (KeyCode.M.IsDown() && MeshEditorManager.SelectedUv != null)
            {
                MeshEditorManager.SelectedUv.meshPoint.MergeWithNearest(EditedMesh);
                m.Dragging = false;
                EditedMesh.Dirty = true;

                "M - merge with nearest".TeachingNotification();

            }
        }

        public override void KeysEventPointedVertex()
        {

            var m = MeshMGMT;

            if (KeyCode.Delete.IsDown())
            {
                if (!PlaytimePainter_EditorInputManager.Control)
                {
                    if (MeshEditorManager.PointedUv.meshPoint.vertices.Count == 1)
                    {
                        if (!m.DeleteVertexHeal(MeshEditorManager.PointedUv.meshPoint))
                            EditedMesh.DeleteUv(MeshEditorManager.PointedUv);
                    }
                    else
                        while (MeshEditorManager.PointedUv.meshPoint.vertices.Count > 1)
                            EditedMesh.MoveTriangle(MeshEditorManager.PointedUv.meshPoint.vertices[1], MeshEditorManager.PointedUv.meshPoint.vertices[0]);


                }
                else
                    EditedMesh.DeleteUv(MeshEditorManager.PointedUv);

                EditedMesh.Dirty = true;
            }



        }

        public override void KeysEventPointedTriangle()
        {
            if (KeyCode.Backspace.IsDown() || KeyCode.Delete.IsDown())
            {
                EditedMesh.triangles.Remove(PointedTriangle);
                foreach (var uv in PointedTriangle.vertexes)
                    if (uv.meshPoint.vertices.Count == 1 && uv.triangles.Count == 1)
                        EditedMesh.meshPoints.Remove(uv.meshPoint);

                Dirty = true;
                return;
            }

            if (KeyCode.U.IsDown())
            {
                PointedTriangle.MakeTriangleVertexUnique(PointedUv);
                Dirty = true;
            }

            /*  if (KeyCode.N.isDown())
              {

                  if (!EditorInputManager.getAltKey())
                  {
                      int no = pointedTriangle.NumberOf(pointedTriangle.GetClosestTo(meshMGMT.collisionPosLocal));
                      pointedTriangle.SharpCorner[no] = !pointedTriangle.SharpCorner[no];

                      (pointedTriangle.SharpCorner[no] ? "Triangle edge's Normal is now dominant" : "Triangle edge Normal is NO longer dominant").TeachingNotification();
                  }
                  else
                  {
                      pointedTriangle.InvertNormal();
                      "Inverting Normals".TeachingNotification();
                  }

                  meshMGMT.edMesh.dirty = true;

              }*/


        }
        #endregion

        #region Mouse
        public override bool MouseEventPointedVertex()
        {
            if (PlaytimePainter_EditorInputManager.GetMouseButtonDown(0))
            {
                var m = MeshMGMT;

                m.Dragging = true;
                m.AssignSelected(MeshEditorManager.PointedUv);
                _originalPosition = PointedUv.meshPoint.WorldPos;
                _draggedVertices.Clear();
                _draggedVertices.Add(PointedUv.meshPoint);
            }
            return false;
        }

        public override bool MouseEventPointedLine()
        {

            var m = MeshMGMT;

            if (PlaytimePainter_EditorInputManager.GetMouseButtonDown(0))
            {
                m.Dragging = true;
                _originalPosition = PlaytimePainter_GridNavigator.LatestMouseRaycastHit;
                PlaytimePainter_GridNavigator.LatestMouseToGridProjection = PlaytimePainter_GridNavigator.LatestMouseRaycastHit;
                _draggedVertices.Clear();
                foreach (var uv in PointedLine.vertexes)
                    _draggedVertices.Add(uv.meshPoint);
            }

            if (PlaytimePainter_EditorInputManager.GetMouseButtonUp(0))
            {
                if (_addToTrianglesAndLines && m.DragDelay > 0 && _draggedVertices.Contains(PointedLine))
                    EditedMesh.InsertIntoLine(MeshEditorManager.PointedLine.vertexes[0].meshPoint, MeshEditorManager.PointedLine.vertexes[1].meshPoint, MeshMGMT.collisionPosLocal);
            }

            return false;
        }

        public override bool MouseEventPointedTriangle()
        {
            var m = MeshMGMT;

            if (PlaytimePainter_EditorInputManager.GetMouseButtonDown(0))
            {

                m.Dragging = true;
                _originalPosition = PlaytimePainter_GridNavigator.LatestMouseRaycastHit;
                PlaytimePainter_GridNavigator.LatestMouseToGridProjection = PlaytimePainter_GridNavigator.LatestMouseRaycastHit;
                _draggedVertices.Clear();
                foreach (var uv in PointedTriangle.vertexes)
                    _draggedVertices.Add(uv.meshPoint);

            }

            if (!_addToTrianglesAndLines || !PlaytimePainter_EditorInputManager.GetMouseButtonUp(0) || !(m.DragDelay > 0) ||
                !_draggedVertices.Contains(PointedTriangle)) return false;

            if (Cfg.newVerticesUnique)
                EditedMesh.InsertIntoTriangleUniqueVertices(MeshEditorManager.PointedTriangle, m.collisionPosLocal);
            else
                EditedMesh.InsertIntoTriangle(MeshEditorManager.PointedTriangle, m.collisionPosLocal);

            return false;
        }

        public override void MouseEventPointedNothing()
        {
            if (_addToTrianglesAndLines && PlaytimePainter_EditorInputManager.GetMouseButtonDown(0))
                MeshMGMT.CreatePointAndFocus(MeshMGMT.onGridLocal);
        }

        private void OnClickDetected()
        {
            if (ShowVertices && MeshEditorManager.TriVertices < 3 && MeshEditorManager.SelectedUv != null && !EditedMesh.IsInTriangleSet(MeshEditorManager.SelectedUv.meshPoint))
                EditedMesh.AddToTrisSet(MeshEditorManager.SelectedUv);
        }

        public override void ManageDragging()
        {
            var m = MeshMGMT;

            var beforeCouldDrag = m.DragDelay <= 0;

            if (PlaytimePainter_EditorInputManager.GetMouseButtonUp(0) || !PlaytimePainter_EditorInputManager.GetMouseButton(0))
            {

                m.Dragging = false;

                if (beforeCouldDrag)
                    EditedMesh.dirtyPosition = true;
                else
                    OnClickDetected();
            }
            else
            {

                var canDrag = m.DragDelay <= 0;

                if (beforeCouldDrag != canDrag && PlaytimePainter_EditorInputManager.Alt && MeshEditorManager.SelectedUv.meshPoint.vertices.Count > 1)
                    m.DisconnectDragged();

                if (!canDrag || !(PlaytimePainter_GridNavigator.Instance.AngGridToCamera(PlaytimePainter_GridNavigator.LatestMouseToGridProjection) < 82)) return;

                var delta = PlaytimePainter_GridNavigator.LatestMouseToGridProjection - _originalPosition;

                if (delta.magnitude == 0)
                    return;

                MeshEditorManager.TriVertices = 0;

                foreach (var v in _draggedVertices)
                    v.WorldPos += delta;

                _originalPosition = PlaytimePainter_GridNavigator.LatestMouseToGridProjection;
            }
        }
        #endregion

        #region Encode & Decode
        public override CfgEncoder Encode() => new CfgEncoder()
            .Add("dm", (int)_detectionMode)
            .Add_Bool("inM", _addToTrianglesAndLines);

        public override void Decode(string key, CfgData data)
        {
            switch (key)
            {
                case "inM": _addToTrianglesAndLines = data.ToBool(); break;
                case "dm": _detectionMode = (DetectionMode)data.ToInt(); break;
            }
        }
        #endregion

    }


}