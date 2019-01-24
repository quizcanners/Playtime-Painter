using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;

namespace Playtime_Painter
{

    public class MeshOperations : PainterStuff
    {

        public string[] toolsHints = new string[] {
            "", "Press go on edge to grow outline",
            "Select a line ang press g to add a road segment",
            "Auto assign tris color",
            "Add vertex to the center of the line",
            "Delete triangle and all vertices", "I need to check what this does"
        };

        public Vector2 uvChangeSpeed;
        public bool updated = false;
        public float width;
        public GtoolPathConfig mode;
        public Vector3 PrevDirection;
        public float outlineWidth = 1;

        public void SetPathStart()
        {
            var selectedLine = MeshMGMT.SelectedLine;

            if (selectedLine == null) return;

            List<Triangle> td = selectedLine.GetAllTriangles_USES_Tris_Listing();

            if (td.Count != 1) return;



            Vertex third = td[0].GetNotOneIn(selectedLine);


            var alltris = third.meshPoint.Triangles();

            if (alltris.Count == 1)
            {
                Debug.Log("Only one tris in third");
                return;
            }

            float MinDist = -1;
            Vertex fourth = null;
            Triangle secondTris = null;

            foreach (Triangle tris in alltris)
            {
                if (tris.Includes(selectedLine.pnts[0].meshPoint) != tris.Includes(selectedLine.pnts[1].meshPoint))
                {
                    Vertex otherUV = tris.NotOneOf(new Vertex[] { selectedLine.pnts[0], selectedLine.pnts[1], third });

                    float sumDist;
                    float dist;
                    dist = Vector3.Distance(selectedLine.pnts[0].Pos, otherUV.Pos);
                    sumDist = dist * dist;
                    dist = Vector3.Distance(otherUV.Pos, selectedLine.pnts[1].Pos);
                    sumDist += dist * dist;
                    dist = Vector3.Distance(otherUV.Pos, third.Pos);
                    sumDist += dist * dist;


                    if ((MinDist == -1) || (MinDist > sumDist))
                    {
                        secondTris = tris;
                        fourth = otherUV;
                        MinDist = sumDist;
                    }
                }
            }

            if (secondTris == null)
            {
                Debug.Log("Third tris not discovered");
                return;
            }

            Vector3 frontCenter = (selectedLine.pnts[0].Pos + selectedLine.pnts[1].Pos) / 2;
            Vector3 backCenter = (third.Pos + fourth.Pos) / 2;

            PrevDirection = frontCenter - backCenter;

            float distance = (frontCenter - backCenter).magnitude;

            Vector2 frontCenterUV = (selectedLine.pnts[0].EditedUV + selectedLine.pnts[1].EditedUV) / 2;
            Vector2 backCenterUV = (third.EditedUV + fourth.EditedUV) / 2;

            uvChangeSpeed = (frontCenterUV - backCenterUV) / distance;
            width = selectedLine.Vector.magnitude;

            Debug.Log("Path is: " + width + " wight and " + uvChangeSpeed + " uv change per square");

            if (Mathf.Abs(uvChangeSpeed.x) > Mathf.Abs(uvChangeSpeed.y))
                uvChangeSpeed.y = 0;
            else
                uvChangeSpeed.x = 0;

            updated = true;
        }
        void ExtendPath()
        {
            var selectedLine = MeshMGMT.SelectedLine;
            var mm = MeshMGMT;

            if (updated == false) return;
            if (selectedLine == null) { updated = false; return; }

            MeshMGMT.UpdateLocalSpaceV3s();

            Vector3 previousCenterPos = selectedLine.pnts[0].Pos;

            Vector3 previousAB = selectedLine.pnts[1].Pos - selectedLine.pnts[0].Pos;

            previousCenterPos += (previousAB / 2);



            Vector3 vector = mm.onGridLocal - previousCenterPos;
            float distance = vector.magnitude;

            MeshPoint a = new MeshPoint(selectedLine.pnts[0].Pos);
            MeshPoint b = new MeshPoint(selectedLine.pnts[1].Pos);

            EditedMesh.meshPoints.Add(a);
            EditedMesh.meshPoints.Add(b);

            Vertex aUV = new Vertex(a, selectedLine.pnts[0].EditedUV + uvChangeSpeed * distance);
            Vertex bUV = new Vertex(b, selectedLine.pnts[1].EditedUV + uvChangeSpeed * distance);




            EditedMesh.triangles.Add(new Triangle(new Vertex[] { selectedLine.pnts[0], bUV, selectedLine.pnts[1] }));
            Triangle headTris = new Triangle(new Vertex[] { selectedLine.pnts[0], aUV, bUV });

            EditedMesh.triangles.Add(headTris);

            //  

            switch (mode)
            {
                case GtoolPathConfig.ToPlanePerpendicular:
                    //vector = previousCenterPos.DistanceV3To(ptdPos);

                    a.localPos = mm.onGridLocal;
                    b.localPos = mm.onGridLocal;


                    Vector3 cross = Vector3.Cross(vector, GridNavigator.Inst().GetGridPerpendicularVector()).normalized * width / 2;
                    a.localPos += cross;
                    b.localPos += -cross;



                    break;
                case GtoolPathConfig.Rotate:
                    // Vector3 ab = a.pos.DistanceV3To(b.pos).normalized * gtoolPath.width;

                    a.localPos = mm.onGridLocal;
                    b.localPos = mm.onGridLocal;



                    Quaternion rot = Quaternion.FromToRotation(previousAB, vector);
                    Vector3 rotv3 = (rot * vector).normalized * width / 2;
                    a.localPos += rotv3;
                    b.localPos += -rotv3;


                    break;

                case GtoolPathConfig.AsPrevious:
                    a.localPos += vector;
                    b.localPos += vector;
                    break;
            }

            PrevDirection = vector;

            selectedLine = new LineData(headTris, aUV, bUV);

            mm.edMesh.Dirty = true;
        }

        public void QUICK_G_Functions()
        {

            var pointedTris = MeshMGMT.PointedTris;
            var pointedLine = MeshMGMT.PointedLine;
            var pointedUV = MeshMGMT.PointedUV;

            switch (QuickMeshFunctionsExtensions.current)
            {
                case QuickMeshFunctions.DeleteTrianglesFully:
                    if ((Input.GetKey(KeyCode.G)) && (pointedTris != null))
                    {
                        foreach (Vertex uv in pointedTris.vertexes)
                        {
                            if ((uv.meshPoint.uvpoints.Count == 1) && (uv.tris.Count == 1))
                                EditedMesh.meshPoints.Remove(uv.meshPoint);
                        }

                        EditedMesh.triangles.Remove(pointedTris);
                        /*pointedTris = null;
                        pointedUV = null;
                        selectedUV = null;
                        pointedLine = null;*/
                        EditedMesh.Dirty = true;
                    }
                    break;
                case QuickMeshFunctions.Line_Center_Vertex_Add:
                    if ((Input.GetKeyDown(KeyCode.G)) && (pointedLine != null))
                    {
                        Vector3 tmp = pointedLine.pnts[0].Pos;
                        tmp += (pointedLine.pnts[1].Pos - pointedLine.pnts[0].Pos) / 2;
                        EditedMesh.InsertIntoLine(pointedLine.pnts[0].meshPoint, pointedLine.pnts[1].meshPoint, tmp);

                    }
                    break;
                case QuickMeshFunctions.TrisColorForBorderDetection:
                    if (Input.GetKeyDown(KeyCode.G))
                    {
                        Debug.Log("Pointed Line null: " + (pointedLine == null));

                        if (pointedTris != null)
                        {
                            for (int i = 0; i < 3; i++)
                                pointedTris.vertexes[i].tmpMark = false;
                            bool[] found = new bool[3];
                            Color[] cols = new Color[3];
                            cols[0] = new Color(0, 1, 1, 1);
                            cols[1] = new Color(1, 0, 1, 1);
                            cols[2] = new Color(1, 1, 0, 1);

                            for (int j = 0; j < 3; j++)
                            {
                                for (int i = 0; i < 3; i++)
                                    if ((!found[j]) && (pointedTris.vertexes[i]._color == cols[j]))
                                    {
                                        pointedTris.vertexes[i].tmpMark = true;
                                        found[j] = true;
                                    }
                            }

                            for (int j = 0; j < 3; j++)
                            {
                                for (int i = 0; i < 3; i++)
                                    if ((!found[j]) && (!pointedTris.vertexes[i].tmpMark))
                                    {
                                        pointedTris.vertexes[i].tmpMark = true;
                                        pointedTris.vertexes[i]._color = cols[j];
                                        found[j] = true;
                                    }
                            }


                            EditedMesh.Dirty = true;
                        }
                        else if (pointedLine != null)
                        {
                            Vertex a = pointedLine.pnts[0];
                            Vertex b = pointedLine.pnts[1];
                            Vertex lessTris = (a.tris.Count < b.tris.Count) ? a : b;

                            if ((a._color.r > 0.9f) && (b._color.r > 0.9f))
                                lessTris._color.r = 0;
                            else if ((a._color.g > 0.9f) && (b._color.g > 0.9f))
                                lessTris._color.g = 0;
                            else if ((a._color.b > 0.9f) && (b._color.b > 0.9f))
                                lessTris._color.b = 0;

                            EditedMesh.Dirty = true;

                        }
                    }
                    break;

                case QuickMeshFunctions.Path:
                    // if (selectedLine != null)
                    //   VertexLine(selectedLine.pnts[0].vert, selectedLine.pnts[1].vert, new Color(0.7f, 0.8f, 0.5f, 1));
                    if (Input.GetKeyDown(KeyCode.G))
                    {
                        if (updated)
                            ExtendPath();
                        else
                            SetPathStart();
                    }




                    break;
                case QuickMeshFunctions.MakeOutline:
                    if ((Input.GetKeyDown(KeyCode.G)) && (pointedUV != null))
                    {

                        //	_Mesh.RefresVerticleTrisList();
                        List<LineData> AllLines = pointedUV.meshPoint.GetAllLines_USES_Tris_Listing();


                        int linesFound = 0;
                        LineData[] lines = new LineData[2];


                        for (int i = 0; i < AllLines.Count; i++)
                        {
                            if (AllLines[i].trianglesCount == 0)
                            {

                                if (linesFound < 2)
                                    lines[linesFound] = AllLines[i];
                                else return;
                                linesFound++;
                            }
                        }

                        if (linesFound == 2)
                        {
                            Vector3 norm = lines[0].HalfVectorToB(lines[1]);

                            MeshPoint hold = new MeshPoint(pointedUV.Pos);

                            if (MeshMGMT.SelectedUV != null)
                                new Vertex(hold, MeshMGMT.SelectedUV.GetUV(0), MeshMGMT.SelectedUV.GetUV(1));
                            else
                                new Vertex(hold);

                            MeshMGMT.edMesh.meshPoints.Add(hold);
                            MeshMGMT.MoveVertexToGrid(hold);
                            hold.localPos += norm * outlineWidth;

                            Vertex[] tri = new Vertex[3];

                            for (int i = 0; i < 2; i++)
                            {
                                tri[0] = hold.uvpoints[0];
                                tri[1] = lines[i].pnts[1];
                                tri[2] = lines[i].pnts[0];

                                MeshMGMT.edMesh.triangles.Add(new Triangle(tri));
                            }

                            MeshMGMT.edMesh.Dirty = true;
                        }




                    }

                    break;
            }

        }

#if PEGI
        void SomeOtherPathStuff()
        {
            var mm = MeshMGMT;
            var selectedLine = mm.SelectedLine;


            if (QuickMeshFunctions.Path.Selected())
            {

                if (selectedLine == null) "Select Line".nl();
                else
                {

                    if ("Set path start on selected".Click())
                        SetPathStart();

                    if (updated == false)
                        "Select must be a Quad with shared uvs".nl();
                    else
                    {
                        if (selectedLine == null)
                            updated = false;
                        else
                        {

                            "Mode".write();
                            mode = (GtoolPathConfig)pegi.editEnum(mode);
                            "G to extend".nl();


                        }
                    }
                }
            }
        }

#endif

        void ProcessPointOnALine(Vertex a, Vertex b, Triangle t)
        {

            if (EditorInputManager.GetMouseButtonDown(1))
            {
              
                updated = false;

                if (QuickMeshFunctions.Path.Selected())
                    SetPathStart();

            }
        }

    }

    public static class QuickMeshFunctionsExtensions
    {
        public static QuickMeshFunctions current;


        public static bool Selected(this QuickMeshFunctions funk)
        {
            return current == funk;
        }
    }

    public enum QuickMeshFunctions { Nothing, MakeOutline, Path, TrisColorForBorderDetection, Line_Center_Vertex_Add, DeleteTrianglesFully, RemoveBordersFromLine }
    // Ctrl + delete: merge this verticle into main verticle
    // Ctrl + move : disconnect this verticle from others
    public enum GtoolPathConfig { ToPlanePerpendicular, AsPrevious, Rotate }


    //#endif
}