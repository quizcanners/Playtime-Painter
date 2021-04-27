using QuizCanners.Migration;
using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using UnityEngine;
using static PainterTool.MeshEditing.PainterMesh;
using System.Runtime.CompilerServices;

namespace PainterTool.MeshEditing
{


    public class VertexEdgeTool : MeshToolBase
    {

        public override string StdTag => "t_edgs";

        private static bool _alsoDoColor;
       // private static bool _editingFlexibleEdge;
        private static float _edgeValue = 1;

        private static float EdgeValue => PlaytimePainter_EditorInputManager.Shift ? 1f - _edgeValue : _edgeValue;

        public override bool ShowTriangles => false;

        public override bool ShowVertices => _detectionMode == DetectionMode.Points;

        public override bool ShowLines => _detectionMode == DetectionMode.Lines;


        #region Inspector

        public override string ToString() => "vertex Edge";

        public override string Tooltip =>
             "[HOLD] Shift - inverts edge value (1-edge)" + Environment.NewLine +
             "This tool allows editing value if edge.w" + Environment.NewLine + "edge.xyz is used to store edge information about the triangle. In Example shaders edge.w is used" +
             " to mask texture color with vertex color. All vertices should be set as Unique for this to work." +
             " Line is drawn between vertices marked with line strength 1. A triangle can't have only 2 sides with Edge: it's ether side, or all 3 (2 points marked to create a line, or 3 points to create 3 lines).";

        public override void Inspect()
        {
            InspectDetectionMode();
            pegi.Nl();

            "Weight to paint: ".PegiLabel().Edit(ref _edgeValue);

            if (_edgeValue == 0 && "Set 1".PegiLabel().Click().UnfocusOnChange())
                _edgeValue = 1;

            if (_edgeValue == 1 && "Set 0".PegiLabel().Click().UnfocusOnChange())
                _edgeValue = 0;

            pegi.Nl();

            "Also do color".PegiLabel().ToggleIcon(ref _alsoDoColor).Nl();

            if (_alsoDoColor)
                GlobalBrush.ColorSliders();
            pegi.Nl();

         //   "Flexible Edge".PegiLabel("Edge type can be seen in Packaging profile (if any). Bevel shader doesn't have a Flexible edge.").ToggleIcon(                
            //     ref _editingFlexibleEdge).Nl();

            if ("Clear".PegiLabel().ClickConfirm("Edge Clear").Nl())
                FillAll(0);

            pegi.Click(AddEdgesToEdgeFall);
            pegi.Click(AddEdgesToHideSeams);

            pegi.Nl();
        }

        #endregion

        public void FillAll(float edgeStrength)
        {
            foreach (MeshPoint p in MeshEditorManager.editedMesh.meshPoints)
            {
                if (edgeStrength == 0)
                    p.edgeStrength = Vector4.zero;

                p.edgeStrength.x = edgeStrength;
                foreach (var triangle in p.FindAllTriangles())
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
                _edgeValue = MeshEditorManager.PointedUv.meshPoint.edgeStrength.x;
                if (_alsoDoColor) GlobalBrush.Color = PointedUv.color;
            }
            else
            {

                if (PointedUv.SameAsLastFrame)
                    return true;

                MeshEditorManager.PointedUv.meshPoint.edgeStrength.x = EdgeValue;
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
            if (!PlaytimePainter_EditorInputManager.GetMouseButton(0)) 
                return false;

           // var vrtA = PointedLine.vertexes[0].meshPoint;
           // var vrtB = PointedLine.vertexes[1].meshPoint;

            /*if (PlaytimePainter_EditorInputManager.Control)
                _edgeValue = (vrtA.edgeStrength.x + vrtB.edgeStrength.x) * 0.5f;
            else
            {*/
            if (PointedLine.SameAsLastFrame)
                return true;

            PutEdgeOnLine(PointedLine);

            return true;
           // }
           // return false;
        }

        public void AddEdgesToEdgeFall()
        {
            
            _edgeValue = 1;

            EditedMesh.RunDebug();

            foreach (var tri in EditedMesh.triangles)
            {
                foreach (LineData line in tri.Lines())
                {
                    if (line.TryGetBothTriangles().Count<2) //trianglesCount_Cahced == 1)
                        PutEdgeOnLine(line);
                }
            }

            EditedMesh.Dirty = true;
        }

        public void AddEdgesToHideSeams()
        {
            //FillAll(0);

            _edgeValue = 1;

            EditedMesh.RunDebug();

            foreach (var tri in EditedMesh.triangles)
            {
                foreach (LineData line in tri.Lines())
                {
                    if (line.IsASeam())
                        PutEdgeOnLine(line);
                }
            }

            EditedMesh.Dirty = true;
        }

        public static void PutEdgeOnLine(PainterMesh.LineData ld)
        {
         

            var tris = ld.TryGetBothTriangles();

            foreach (var t in tris)
                t.SetEdgeWeight(ld, EdgeValue);

           // var edValA = EdgeValue;
            //var edValB = EdgeValue;

            MeshPoint pointA = ld.vertexes[0].meshPoint;
            MeshPoint pointB = ld.vertexes[1].meshPoint;

            if (EdgeValue>0.9f)
            {
                /*foreach (var uv in pointA.vertices)
                    foreach (var t in uv.triangles)
                    {
                        int opposite = t.NumberOf(uv);
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
                    }*/

                var scale = pointA.edgeStrength;
                scale.Scale(pointB.edgeStrength);

             

                if (scale.magnitude<0.5f) 
                {
                    //No common edges

                    int freeIndex = 0;
                    while (freeIndex < 4)
                    {
                        /*
                        if (pointA.edgeStrength[freeIndex] > 0.9f)
                        {
                            freeIndex++;
                        }

                        if (pointB.edgeStrength[freeIndex] > 0.9)
                        {
                            freeIndex++;
                            continue;
                        }*/

                        if (TryFindSameInNeighbors(pointA, other: pointB)) 
                        {
                            freeIndex++;
                            continue;
                        }

                        if (TryFindSameInNeighbors(pointB, other : pointA))
                        {
                            freeIndex++;
                            continue;
                        }

                        bool TryFindSameInNeighbors(MeshPoint point, MeshPoint other) 
                        {
                            if (point.edgeStrength[freeIndex] > 0.9f)
                                return false;

                            foreach (var vert in point.vertices)
                                foreach (var tri in vert.triangles)
                                {
                                    for (var i = 0; i < 3; i++)
                                    {
                                        var testing = tri[i];

                                        if (testing == other)
                                            continue;

                                        if (tri[i].edgeStrength[freeIndex] > 0.9f)
                                        {
                                            return true;
                                        }
                                    }
                                }

                            return false;
                        }

                        break;
                    }

                    if (freeIndex < 4) 
                    {
                        pointA.edgeStrength[freeIndex] = 1;
                        pointB.edgeStrength[freeIndex] = 1;
                    } else
                    {
                    }
                }
            }
            else
            {

                for (int i=0; i<4; i++)
                {
                    if (pointA.edgeStrength[i] > 0.9f && pointB.edgeStrength[i] > 0.9f)
                    {
                        //TODO: Also check nabours to see if one may get disrupted
                        pointA.edgeStrength[i] = 0;
                        pointB.edgeStrength[i] = 0;
                    }
                }

                //pointA.edgeStrength.x = edValA;
                //pointB.edgeStrength.x = edValB;
            }

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
        public override void DecodeTag(string key, CfgData data)
        {
            switch (key)
            {
                case "v": _edgeValue = data.ToFloat(); break;
                case "doCol": _alsoDoColor = data.ToBool(); break;
               // case "fe": _editingFlexibleEdge = data.ToBool(); break;
            }
        }

        public override CfgEncoder Encode() => new CfgEncoder()
            .Add("v", _edgeValue)
            .Add_Bool("doCol", _alsoDoColor)
            //.Add_Bool("fe", _editingFlexibleEdge)
            ;
        #endregion

    }

}
