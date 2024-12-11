using QuizCanners.Migration;
using QuizCanners.Inspect;
using QuizCanners.Utils;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PainterTool.MeshEditing
{
    public class VertexPositionTool : MeshToolBase
    {
        public override string StdTag => "t_pos";

        private bool _addToTrianglesAndLines;

        

        public override bool ShowVertices => _detectionMode == DetectionMode.Points;

        public override bool ShowLines => _detectionMode == DetectionMode.Lines || (_addToTrianglesAndLines && _detectionMode == DetectionMode.Points);

        public override bool ShowTriangles => _detectionMode == DetectionMode.Triangles || (_addToTrianglesAndLines && _detectionMode == DetectionMode.Points);

        private static List<PainterMesh.MeshPoint> DraggedVertices => EditedMesh._draggedVertices; // new List<MeshPoint>();

        private Vector3 _originalPosition;

        public override bool ShowGrid => true;

        public override void AssignText(MarkerWithText markers, PainterMesh.MeshPoint point)
        {

            var selected = Painter.MeshManager.GetSelectedVertex();

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

        public override string ToString() => MsgPainter.MeshPointPositionTool.GetText(); // "ADD & MOVE";

     

        public override void Inspect()
        {
            var sd = Painter.Data;
            var em = EditedMesh;

            // "Mode".PegiLabel(40).Edit_Enum(ref _detectionMode).Nl();

            InspectDetectionMode();
            pegi.Nl();

            if (_detectionMode != DetectionMode.Points)
            {
                "Gizmos needs to be toggled on (if they are off)".PL().Write_Hint();
            }

            if (MeshEditorManager.MeshTool.ShowGrid)
            {
                "Snap to grid (Use XZ to toggle grid orientation)".PL().ToggleIcon(ref sd.snapToGrid).Nl();

                if (sd.snapToGrid)
                    "Size".ConstL().Edit(ref sd.gridSize);
            }

            pegi.Nl();

            "Pixel-Perfect".PL("New vertex will have UV coordinate rounded to half a pixel.").ToggleIcon(ref Painter.Data.pixelPerfectMeshEditing).Nl();

            "Insert vertices".PL("Will split triangles and edges by inserting vertices").ToggleIcon(ref _addToTrianglesAndLines).Nl();

            "Add Unique:".PL().ToggleIcon(ref Painter.Data.newVerticesUnique).Nl();

            if ("All shared if same UV".PL("Will only merge vertices if they have same UV").Click().Nl())
            {
                em.AllVerticesSharedIfSameUV();
                em.Dirty = true;

            }

            if ("All shared".PL(toolTip: "Vertices will be merged if they share the position. This may result in loss of UV data.").ClickConfirm(confirmationTag: "AllShrd"))
            {
                em.AllVerticesShared();
                em.Dirty = true;
                Painter.Data.newVerticesUnique = false;
            }

            if ("All unique".PL(toolTip: "If vertex belongs to more then one triangle, a new vertex will be created for each of the extra triangle.").ClickConfirm(confirmationTag: "AllUnq").Nl())
            {
                foreach (var t in EditedMesh.triangles)
                    em.GiveTriangleUniqueVertices(t);

                Painter.Data.newVerticesUnique = true;
            }

            "Add Smooth:".PL().ToggleIcon(ref Painter.Data.newVerticesSmooth).Nl();

            if ("Sharp All".PL(toolTip: "Normal vectors will be different for each triangle when possible (when vertex/normal data is not shared with another triangle).").ClickConfirm(confirmationTag: "AllSrp"))
            {
                foreach (var vr in EditedMesh.meshPoints)
                    vr.smoothNormal = false;

                EditedMesh.Dirty = true;
                Painter.Data.newVerticesSmooth = false;
            }

            if ("Smooth All".PL(toolTip: "All edge normals will be smooth even when a triangle can afford to have a different one.").ClickConfirm(confirmationTag: "AllSmth").Nl())
            {
                foreach (var vr in EditedMesh.meshPoints)
                    vr.smoothNormal = true;
                EditedMesh.Dirty = true;
                Painter.Data.newVerticesSmooth = true;
            }


            if ("Auto Bevel".PL().Click())
                SharpFacesTool.AutoAssignDominantNormalsForBeveling();
            "Sensitivity".ConstL().Edit(ref Painter.Data.bevelDetectionSensitivity, 3, 30).Nl();

            pegi.Nl();
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
            var m = Painter.MeshManager;
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

            var m = Painter.MeshManager;

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
                var m = Painter.MeshManager;

                m.Dragging = true;
                m.AssignSelected(MeshEditorManager.PointedUv);
                _originalPosition = PointedUv.meshPoint.WorldPos;
                DraggedVertices.Clear();
                DraggedVertices.Add(PointedUv.meshPoint);
            }
            return false;
        }

        public override bool MouseEventPointedLine()
        {

            var m = Painter.MeshManager;

            if (PlaytimePainter_EditorInputManager.GetMouseButtonDown(0))
            {
                m.Dragging = true;
                _originalPosition = MeshPainting.LatestMouseRaycastHit;
                MeshPainting.LatestMouseToGridProjection = MeshPainting.LatestMouseRaycastHit;
                DraggedVertices.Clear();
                foreach (var uv in PointedLine.vertexes)
                    DraggedVertices.Add(uv.meshPoint);
            }

            if (PlaytimePainter_EditorInputManager.GetMouseButtonUp(0))
            {
                if (_addToTrianglesAndLines && m.DragDelay > 0 && DraggedVertices.Contains(PointedLine))
                    EditedMesh.InsertIntoLine(MeshEditorManager.PointedLine.vertexes[0].meshPoint, MeshEditorManager.PointedLine.vertexes[1].meshPoint, MeshPainting.collisionPosLocal);
            }

            return false;
        }

        public override bool MouseEventPointedTriangle()
        {
            var m = Painter.MeshManager;

            if (PlaytimePainter_EditorInputManager.GetMouseButtonDown(0))
            {

                m.Dragging = true;
                _originalPosition = MeshPainting.LatestMouseRaycastHit;
                MeshPainting.LatestMouseToGridProjection = MeshPainting.LatestMouseRaycastHit;
                DraggedVertices.Clear();
                foreach (var uv in PointedTriangle.vertexes)
                    DraggedVertices.Add(uv.meshPoint);

            }

            if (!_addToTrianglesAndLines || !PlaytimePainter_EditorInputManager.GetMouseButtonUp(0) || !(m.DragDelay > 0) ||
                !DraggedVertices.Contains(PointedTriangle)) return false;

            if (Painter.Data.newVerticesUnique)
                EditedMesh.InsertIntoTriangleUniqueVertices(MeshEditorManager.PointedTriangle, MeshPainting.collisionPosLocal);
            else
                EditedMesh.InsertIntoTriangle(MeshEditorManager.PointedTriangle, MeshPainting.collisionPosLocal);

            return false;
        }

        public override void MouseEventPointedNothing()
        {
            if (_addToTrianglesAndLines && PlaytimePainter_EditorInputManager.GetMouseButtonDown(0))
                Painter.MeshManager.CreatePointAndFocus(MeshPainting.onGridLocal);
        }

        private void OnClickDetected()
        {
            if (ShowVertices && MeshEditorManager.TriVertices < 3 && MeshEditorManager.SelectedUv != null && !EditedMesh.IsInTriangleSet(MeshEditorManager.SelectedUv.meshPoint))
                EditedMesh.AddToTrisSet(MeshEditorManager.SelectedUv);
        }

        public override void ManageDragging()
        {
            var m = Painter.MeshManager;

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

                if (!canDrag || !(MeshPainting.Grid.AngGridToCamera(MeshPainting.LatestMouseToGridProjection) < 82)) return;

                var delta = MeshPainting.LatestMouseToGridProjection - _originalPosition;

                if (delta.magnitude == 0)
                    return;

                MeshEditorManager.TriVertices = 0;

                foreach (var v in DraggedVertices)
                    v.WorldPos += delta;

                _originalPosition = MeshPainting.LatestMouseToGridProjection;
            }
        }
        #endregion

        #region Encode & Decode
        public override CfgEncoder Encode() => new CfgEncoder()
            .Add("dm", (int)_detectionMode)
            .Add_Bool("inM", _addToTrianglesAndLines);

        public override void DecodeTag(string key, CfgData data)
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