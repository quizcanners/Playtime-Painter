using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;

namespace Playtime_Painter
{

    public class MeshOperations : PainterStuff
    {

        public string[] toolsHints = new string[]
 {
        "", "Press go on edge to grow outline", "Select a line ang press g to add a road segment", "Auto assign tris color", "Add vertex to the center of the line", "Delete triangle and all vertices", "I need to check what this does"
 };

        public Vector2 uvChangeSpeed;
        public bool updated = false;
        public float width;
        public gtoolPathConfig mode;
        public Vector3 PrevDirection;
        public float outlineWidth = 1;

        public void SetPathStart()
        {
            var selectedLine = meshMGMT.selectedLine;

            if (selectedLine == null) return;

            List<trisDta> td = selectedLine.getAllTriangles_USES_Tris_Listing();

            if (td.Count != 1) return;



            UVpoint third = td[0].NotOnLine(selectedLine);


            var alltris = third.vert.triangles();

            if (alltris.Count == 1)
            {
                Debug.Log("Only one tris in third");
                return;
            }

            float MinDist = -1;
            UVpoint fourth = null;
            trisDta secondTris = null;

            foreach (trisDta tris in alltris)
            {
                if (tris.includes(selectedLine.pnts[0].vert) != tris.includes(selectedLine.pnts[1].vert))
                {
                    UVpoint otherUV = tris.NotOneOf(new UVpoint[] { selectedLine.pnts[0], selectedLine.pnts[1], third });

                    float sumDist;
                    float dist;
                    dist = Vector3.Distance(selectedLine.pnts[0].pos, otherUV.pos);
                    sumDist = dist * dist;
                    dist = Vector3.Distance(otherUV.pos, selectedLine.pnts[1].pos);
                    sumDist += dist * dist;
                    dist = Vector3.Distance(otherUV.pos, third.pos);
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

            Vector3 frontCenter = (selectedLine.pnts[0].pos + selectedLine.pnts[1].pos) / 2;
            Vector3 backCenter = (third.pos + fourth.pos) / 2;

            PrevDirection = frontCenter - backCenter;

            float distance = (frontCenter - backCenter).magnitude;

            Vector2 frontCenterUV = (selectedLine.pnts[0].editedUV + selectedLine.pnts[1].editedUV) / 2;
            Vector2 backCenterUV = (third.editedUV + fourth.editedUV) / 2;

            uvChangeSpeed = (frontCenterUV - backCenterUV) / distance;
            width = selectedLine.Vector().magnitude;

            Debug.Log("Path is: " + width + " wight and " + uvChangeSpeed + " uv change per square");

            if (Mathf.Abs(uvChangeSpeed.x) > Mathf.Abs(uvChangeSpeed.y))
                uvChangeSpeed.y = 0;
            else
                uvChangeSpeed.x = 0;

            updated = true;
        }
        void ExtendPath()
        {
            var selectedLine = meshMGMT.selectedLine;
            var mm = meshMGMT;

            if (updated == false) return;
            if (selectedLine == null) { updated = false; return; }

            meshMGMT.UpdateLocalSpaceV3s();

            Vector3 previousCenterPos = selectedLine.pnts[0].pos;

            Vector3 previousAB = selectedLine.pnts[1].pos - selectedLine.pnts[0].pos;

            previousCenterPos += (previousAB / 2);



            Vector3 vector = mm.onGridLocal - previousCenterPos;
            float distance = vector.magnitude;

            vertexpointDta a = new vertexpointDta(selectedLine.pnts[0].pos);
            vertexpointDta b = new vertexpointDta(selectedLine.pnts[1].pos);

            editedMesh.vertices.Add(a);
            editedMesh.vertices.Add(b);

            UVpoint aUV = new UVpoint(a, selectedLine.pnts[0].editedUV + uvChangeSpeed * distance);
            UVpoint bUV = new UVpoint(b, selectedLine.pnts[1].editedUV + uvChangeSpeed * distance);




            editedMesh.triangles.Add(new trisDta(new UVpoint[] { selectedLine.pnts[0], bUV, selectedLine.pnts[1] }));
            trisDta headTris = new trisDta(new UVpoint[] { selectedLine.pnts[0], aUV, bUV });

            editedMesh.triangles.Add(headTris);

            //  

            switch (mode)
            {
                case gtoolPathConfig.ToPlanePerpendicular:
                    //vector = previousCenterPos.DistanceV3To(ptdPos);

                    a.localPos = mm.onGridLocal;
                    b.localPos = mm.onGridLocal;


                    Vector3 cross = Vector3.Cross(vector, GridNavigator.inst().getGridPerpendicularVector()).normalized * width / 2;
                    a.localPos += cross;
                    b.localPos += -cross;



                    break;
                case gtoolPathConfig.Rotate:
                    // Vector3 ab = a.pos.DistanceV3To(b.pos).normalized * gtoolPath.width;

                    a.localPos = mm.onGridLocal;
                    b.localPos = mm.onGridLocal;



                    Quaternion rot = Quaternion.FromToRotation(previousAB, vector);
                    Vector3 rotv3 = (rot * vector).normalized * width / 2;
                    a.localPos += rotv3;
                    b.localPos += -rotv3;


                    break;

                case gtoolPathConfig.AsPrevious:
                    a.localPos += vector;
                    b.localPos += vector;
                    break;
            }

            PrevDirection = vector;

            selectedLine = new LineData(headTris, aUV, bUV);

            mm.edMesh.dirty = true;
        }

        public void QUICK_G_Functions()
        {

            var pointedTris = meshMGMT.pointedTris;
            var pointedLine = meshMGMT.pointedLine;
            var pointedUV = meshMGMT.pointedUV;

            switch (quickMeshFunctionsExtensions.current)
            {
                case QuickMeshFunctions.DeleteTrianglesFully:
                    if ((Input.GetKey(KeyCode.G)) && (pointedTris != null))
                    {
                        foreach (UVpoint uv in pointedTris.uvpnts)
                        {
                            if ((uv.vert.uvpoints.Count == 1) && (uv.tris.Count == 1))
                                editedMesh.vertices.Remove(uv.vert);
                        }

                        editedMesh.triangles.Remove(pointedTris);
                        /*pointedTris = null;
                        pointedUV = null;
                        selectedUV = null;
                        pointedLine = null;*/
                        editedMesh.dirty = true;
                    }
                    break;
                case QuickMeshFunctions.Line_Center_Vertex_Add:
                    if ((Input.GetKeyDown(KeyCode.G)) && (pointedLine != null))
                    {
                        Vector3 tmp = pointedLine.pnts[0].pos;
                        tmp += (pointedLine.pnts[1].pos - pointedLine.pnts[0].pos) / 2;
                        editedMesh.insertIntoLine(pointedLine.pnts[0].vert, pointedLine.pnts[1].vert, tmp);

                    }
                    break;
                case QuickMeshFunctions.TrisColorForBorderDetection:
                    if (Input.GetKeyDown(KeyCode.G))
                    {
                        Debug.Log("Pointed Line null: " + (pointedLine == null));

                        if (pointedTris != null)
                        {
                            for (int i = 0; i < 3; i++)
                                pointedTris.uvpnts[i].tmpMark = false;
                            bool[] found = new bool[3];
                            Color[] cols = new Color[3];
                            cols[0] = new Color(0, 1, 1, 1);
                            cols[1] = new Color(1, 0, 1, 1);
                            cols[2] = new Color(1, 1, 0, 1);

                            for (int j = 0; j < 3; j++)
                            {
                                for (int i = 0; i < 3; i++)
                                    if ((!found[j]) && (pointedTris.uvpnts[i]._color == cols[j]))
                                    {
                                        pointedTris.uvpnts[i].tmpMark = true;
                                        found[j] = true;
                                    }
                            }

                            for (int j = 0; j < 3; j++)
                            {
                                for (int i = 0; i < 3; i++)
                                    if ((!found[j]) && (!pointedTris.uvpnts[i].tmpMark))
                                    {
                                        pointedTris.uvpnts[i].tmpMark = true;
                                        pointedTris.uvpnts[i]._color = cols[j];
                                        found[j] = true;
                                    }
                            }


                            editedMesh.dirty = true;
                        }
                        else if (pointedLine != null)
                        {
                            UVpoint a = pointedLine.pnts[0];
                            UVpoint b = pointedLine.pnts[1];
                            UVpoint lessTris = (a.tris.Count < b.tris.Count) ? a : b;

                            if ((a._color.r > 0.9f) && (b._color.r > 0.9f))
                                lessTris._color.r = 0;
                            else if ((a._color.g > 0.9f) && (b._color.g > 0.9f))
                                lessTris._color.g = 0;
                            else if ((a._color.b > 0.9f) && (b._color.b > 0.9f))
                                lessTris._color.b = 0;

                            editedMesh.dirty = true;

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
                        List<LineData> AllLines = pointedUV.vert.GetAllLines_USES_Tris_Listing();


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

                            vertexpointDta hold = new vertexpointDta(pointedUV.pos);

                            if (meshMGMT.selectedUV != null)
                                new UVpoint(hold, meshMGMT.selectedUV.GetUV(0), meshMGMT.selectedUV.GetUV(1));
                            else
                                new UVpoint(hold);

                            meshMGMT.edMesh.vertices.Add(hold);
                            meshMGMT.MoveVertexToGrid(hold);
                            hold.localPos += norm * outlineWidth;

                            UVpoint[] tri = new UVpoint[3];

                            for (int i = 0; i < 2; i++)
                            {
                                tri[0] = hold.uvpoints[0];
                                tri[1] = lines[i].pnts[1];
                                tri[2] = lines[i].pnts[0];

                                meshMGMT.edMesh.triangles.Add(new trisDta(tri));
                            }

                            meshMGMT.edMesh.dirty = true;
                        }




                    }

                    break;
            }

        }
#if PEGI
        void SomeOtherPathStuff()
        {
            var mm = meshMGMT;
            var selectedLine = mm.selectedLine;


            if (QuickMeshFunctions.Path.selected())
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
                            mode = (gtoolPathConfig)pegi.editEnum(mode);
                            "G to extend".nl();


                        }
                    }
                }
            }



        }

#endif

        void ProcessPointOnALine(UVpoint a, UVpoint b, trisDta t)
        {

            if (EditorInputManager.GetMouseButtonDown(1))
            {
              
                updated = false;

                if (QuickMeshFunctions.Path.selected())
                    SetPathStart();

            }
        }

    }

    public static class quickMeshFunctionsExtensions
    {
        public static QuickMeshFunctions current;


        public static bool selected(this QuickMeshFunctions funk)
        {
            return current == funk;
        }
    }

    public enum QuickMeshFunctions { Nothing, MakeOutline, Path, TrisColorForBorderDetection, Line_Center_Vertex_Add, DeleteTrianglesFully, RemoveBordersFromLine }
    // Ctrl + delete: merge this verticle into main verticle
    // Ctrl + move : disconnect this verticle from others
    public enum gtoolPathConfig { ToPlanePerpendicular, AsPrevious, Rotate }


    //#endif
}