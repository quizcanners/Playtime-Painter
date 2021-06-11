using QuizCanners.CfgDecode;
using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlaytimePainter.MeshEditing
{


    public class VertexEdgeTool : MeshToolBase
    {

        public override string StdTag => "t_edgs";

        private static bool _alsoDoColor;
        private static bool _editingFlexibleEdge;
        private static float _edgeValue = 1;

        private static float EdgeValue => PlaytimePainter_EditorInputManager.Shift ? 1f - _edgeValue : _edgeValue;

        public override bool ShowTriangles => false;

        public override bool ShowVertices => !_editingFlexibleEdge;

        public override bool ShowLines => true;

        #region Inspector

        public override string NameForDisplayPEGI() => "vertex Edge";

        public override string Tooltip =>
             "[HOLD] Shift - inverts edge value (1-edge)" + Environment.NewLine +
             "This tool allows editing value if edge.w" + Environment.NewLine + "edge.xyz is used to store edge information about the triangle. In Example shaders edge.w is used" +
             " to mask texture color with vertex color. All vertices should be set as Unique for this to work." +
             " Line is drawn between vertices marked with line strength 1. A triangle can't have only 2 sides with Edge: it's ether side, or all 3 (2 points marked to create a line, or 3 points to create 3 lines).";

        public override void Inspect()
        {
            "Weight to paint: ".edit(ref _edgeValue);

            if (_edgeValue == 0 && "Set 1".Click())
                _edgeValue = 1;

            if (_edgeValue == 1 && "Set 0".Click())
                _edgeValue = 0;

            pegi.nl();

            "Also do color".toggleIcon(ref _alsoDoColor).nl();

            if (_alsoDoColor)
                GlobalBrush.ColorSliders().nl();

            "Flexible Edge".toggleIcon(
                "Edge type can be seen in Packaging profile (if any). Bevel shader doesn't have a Flexible edge."
                , ref _editingFlexibleEdge).nl();

            if ("FILL ALL".ClickConfirm("EdgeFill").nl())
                FillAll(_edgeValue);

            if ("Add Edges to Seams".Click().nl())
                AddEdgesToHideSeams();
        }

        #endregion

        public void FillAll(float edgeStrength)
        {
            foreach (var p in MeshEditorManager.editedMesh.meshPoints)
            {
                p.edgeStrength = edgeStrength;
                foreach (var triangle in p.Triangles())
                {
                    for (int i = 0; i < 3; i++)
                        triangle.edgeWeight[i] = edgeStrength;
                }
            }

            if (_alsoDoColor)
            {
                var col = GlobalBrush.Color;

                foreach (var p in MeshEditorManager.editedMesh.meshPoints)
                {
                    foreach (var uvi in p.vertices)
                        GlobalBrush.mask.SetValuesOn(ref uvi.color, col);
                }
            }

            EditedMesh.Dirty = true;
        }

        public override bool MouseEventPointedVertex()
        {
            if ((!PlaytimePainter_EditorInputManager.GetMouseButton(0))) return false;

            if (!PointedUv.meshPoint.AllPointsUnique())
                pegi.GameView.ShowNotification("Shared points found, Edge requires All Unique");

            if (PlaytimePainter_EditorInputManager.Control)
            {
                _edgeValue = MeshEditorManager.PointedUv.meshPoint.edgeStrength;
                if (_alsoDoColor) GlobalBrush.Color = PointedUv.color;
            }
            else
            {

                if (PointedUv.SameAsLastFrame)
                    return true;

                MeshEditorManager.PointedUv.meshPoint.edgeStrength = EdgeValue;
                if (_alsoDoColor)
                {
                    var col = GlobalBrush.Color;
                    foreach (var uvi in PointedUv.meshPoint.vertices)
                        GlobalBrush.mask.SetValuesOn(ref uvi.color, col);
                }
                EditedMesh.Dirty = true;

                return true;
            }
            return false;
        }

        public override bool MouseEventPointedLine()
        {
            if (!PlaytimePainter_EditorInputManager.GetMouseButton(0)) return false;

            var vrtA = PointedLine.vertexes[0].meshPoint;
            var vrtB = PointedLine.vertexes[1].meshPoint;

            if (PlaytimePainter_EditorInputManager.Control)
                _edgeValue = (vrtA.edgeStrength + vrtB.edgeStrength) * 0.5f;
            else
            {
                if (PointedLine.SameAsLastFrame)
                    return true;


                PutEdgeOnLine(PointedLine);

                return true;
            }
            return false;
        }

        public static void AddEdgesToHideSeams()
        {
            _edgeValue = 1;

            foreach (var tri in EditedMesh.triangles)
            {
                foreach (var line in tri.Lines())
                {
                    if (line[0].GotSeam() && line[1].GotSeam())
                        PutEdgeOnLine(line);
                }
            }

            EditedMesh.Dirty = true;
        }

        public static void PutEdgeOnLine(PainterMesh.LineData ld)
        {

            PainterMesh.MeshPoint pointA = ld.vertexes[0].meshPoint;
            PainterMesh.MeshPoint pointB = ld.vertexes[1].meshPoint;

            var tris = ld.GetAllTriangles();

            foreach (var t in tris)
                t.SetEdgeWeight(ld, EdgeValue);

            var edValA = EdgeValue;
            var edValB = EdgeValue;

            if (_editingFlexibleEdge)
            {
                foreach (var uv in pointA.vertices)
                    foreach (var t in uv.triangles)
                    {
                        var opposite = t.NumberOf(uv);
                        for (var i = 0; i < 3; i++)
                            if (opposite != i)
                                edValA = Mathf.Max(edValA, t.edgeWeight[i]);
                    }

                foreach (var uv in pointB.vertices)
                    foreach (var t in uv.triangles)
                    {
                        var opposite = t.NumberOf(uv);
                        for (var i = 0; i < 3; i++)
                            if (opposite != i)
                                edValB = Mathf.Max(edValB, t.edgeWeight[i]);
                    }
            }

            pointA.edgeStrength = edValA;
            pointB.edgeStrength = edValB;

            if (_alsoDoColor)
            {
                var col = GlobalBrush.Color;
                foreach (var vertex in pointA.vertices)
                    GlobalBrush.mask.SetValuesOn(ref vertex.color, col);
                foreach (var vertex in pointB.vertices)
                    GlobalBrush.mask.SetValuesOn(ref vertex.color, col);
            }

            EditedMesh.Dirty = true;
        }

        #region Encode & Decode
        public override void Decode(string key, CfgData data)
        {
            switch (key)
            {
                case "v": _edgeValue = data.ToFloat(); break;
                case "doCol": _alsoDoColor = data.ToBool(); break;
                case "fe": _editingFlexibleEdge = data.ToBool(); break;
            }
        }

        public override CfgEncoder Encode() => new CfgEncoder()
            .Add("v", _edgeValue)
            .Add_Bool("doCol", _alsoDoColor)
            .Add_Bool("fe", _editingFlexibleEdge);
        #endregion

    }

}
