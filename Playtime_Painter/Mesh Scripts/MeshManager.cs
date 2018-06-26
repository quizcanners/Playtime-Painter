using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using PlayerAndEditorGUI;
using SharedTools_Stuff;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Playtime_Painter {

	[Serializable]
    [ExecuteInEditMode]
    public class MeshManager : PainterStuff  {
        
        public static MeshManager Inst { get
            {
                return PainterManager.inst.meshManager;
            }
        }

        public static Transform Transform { get { return PainterManager.inst.transform; } }

        public static float animTextureSize = 128;

        public const string ToolName = "Mesh_Editor";

        public static string ToolPath()
        {
			return PlaytimeToolComponent.ToolsFolder + "/" + ToolName;
        }
        
        public MeshToolBase MeshTool { get { return PainterConfig.inst.meshTool; } }

        public int editedUV = 0;
        public static Vector3 editorMousePos;


        [NonSerialized]
        public PlaytimePainter target;
        public PlaytimePainter previouslyEdited;


        [NonSerialized]
        List<string> undoMoves = new List<string>();
        [NonSerialized]
        List<string> redoMoves = new List<string>();

        [NonSerialized]
        public EditableMesh edMesh = new EditableMesh();
        [NonSerialized]
        public EditableMesh previewEdMesh = new EditableMesh();
        [NonSerialized]
        public Mesh previewMesh;
   
        public AddCubeCfg tmpCubeCfg = new AddCubeCfg();
      

        int currentUV = 0;
        bool SelectingUVbyNumber = false;
        
        public Vertex SelectedUV { get { return edMesh.selectedUV; } set { edMesh.selectedUV = value; } }
        public LineData SelectedLine { get { return edMesh.selectedLine; } set { edMesh.selectedLine = value; } }
        public Triangle SelectedTris { get { return edMesh.selectedTris; } set { edMesh.selectedTris = value; } }
        public Vertex PointedUV { get { return edMesh.pointedUV; } set { edMesh.pointedUV = value; } }
        public LineData PointedLine { get { return edMesh.pointedLine; } set { edMesh.pointedLine = value; } }
        public Triangle PointedTris { get { return edMesh.pointedTris; } set { edMesh.pointedTris = value; } }
        Vertex[] TrisSet { get { return edMesh.TrisSet; } set { edMesh.TrisSet = value; } }
        public int TrisVerts { get { return edMesh.trisVerts; } set { edMesh.trisVerts = value; } }

        [NonSerialized]
        public int vertsShowMax = 8;

        [NonSerialized]
        public Vector3 onGridLocal = new Vector3();
        [NonSerialized]
        public Vector3 collisionPosLocal = new Vector3();

        public void UpdateLocalSpaceV3s() {
            if (target != null) {
                onGridLocal = target.transform.InverseTransformPoint(GridNavigator.onGridPos);
                collisionPosLocal = target.transform.InverseTransformPoint(GridNavigator.collisionPos);
            }
        }
        
        public void EditMesh(PlaytimePainter pntr, bool EditCopy) {
            if ((pntr == null) || (pntr == target))
                return;

            if (target != null)
                DisconnectMesh();

            target = pntr;

            edMesh = new EditableMesh();

            edMesh.Edit(pntr);

            if (EditCopy)
                pntr.meshFilter.sharedMesh = new Mesh();
            
            Redraw();

            pntr.meshNameHolder = edMesh.meshName;

            InitVertsIfNUll();

            SelectedLine = null;
            SelectedTris = null;
            SelectedUV = null;

            undoMoves.Clear();
            redoMoves.Clear();

            undoMoves.Add(edMesh.Encode().ToString());

            MeshTool.OnSelectTool();

        }

        public void DisconnectMesh() {
            
            if (target != null)  {
                MeshTool.OnDeSelectTool();
                target.savedEditableMesh = edMesh.Encode().ToString();
                target = null;
            }
            grid.Deactivateverts();
            GridNavigator.inst().SetEnabled(false, false);
            undoMoves.Clear();
            redoMoves.Clear();
        }

#if UNITY_EDITOR
        public void SaveGeneratedMeshAsAsset()
        {
            AssetDatabase.CreateAsset(target.meshFilter.mesh, "Assets/Models/" + target.gameObject.name + "_export.asset");
            AssetDatabase.SaveAssets();
        }
#endif

        public void Redraw() {
 
            if (target != null) {

                MeshConstructor mc = new MeshConstructor(edMesh, target.meshProfile, target.meshFilter.sharedMesh);

                if (!edMesh.dirty_Vertices && !edMesh.dirty_Normals && editedMesh.Dirty) {

                    if (editedMesh.dirty_Position)
                        mc.UpdateMesh<MeshSolutions.vertexPos>();

                    if (edMesh.dirty_Color)
                        mc.UpdateMesh<MeshSolutions.vertexColor>();

                } else {
                    target.meshFilter.sharedMesh = mc.Construct();
                    mc.AssignMeshAsCollider(target.meshCollider);
                }

            }

            edMesh.Dirty = false;

            //  if (_meshTool == MeshTool.VertexAnimation)
            //{
            //UpdateAnimations(SaveAbleMesh sbm);
            /*  int curFrame = _target.GetAnimationUVy();//getBaseDependencies().stretch_Monitor.curUVy;
              MeshConstructionData svm = _target.saveMeshDta;
              vertexAnimationFrame vaf = svm.anims[curFrame];
              if (vaf != null)
              {
                  foreach (vertexpointDta vp in _Mesh.vertices)
                      vaf.verts[ vp.index] = vp.anim[curFrame];

                  svm.updateAnimation(curFrame);
              }
*/
            //}

            //  Debug.Log("Redraw ");
        }

        public static Vector2 RoundUVs(Vector2 source, float accuracy)
        {
            Vector2 uv = source * accuracy;
            uv.x = Mathf.Round(uv.x);
            uv.y = Mathf.Round(uv.y);
            uv /= accuracy;
            return uv;
        }

        public void AddToTrisSet(Vertex nuv) {

            TrisSet[TrisVerts] = nuv;
            TrisVerts++;

            if (TrisVerts == 3)
            {
                foreach (Triangle t in edMesh.triangles)
                {
                    if (t.IsSamePoints(TrisSet))
                    {
                        t.Change(TrisSet);
                        edMesh.Dirty = true;
                        TrisVerts = 0;
                        return;
                    }
                }
            }

            if (TrisVerts >= 3)
            {
                Triangle td = new Triangle(TrisSet);
                edMesh.triangles.Add(td);

                if (!EditorInputManager.getControlKey())
                {
                    MakeTriangleVertUnique(td, TrisSet[0]);
                    MakeTriangleVertUnique(td, TrisSet[1]);
                    MakeTriangleVertUnique(td, TrisSet[2]);
                }

                TrisVerts = 0;
                edMesh.Dirty = true;
            }
        }

        [NonSerialized]
        public float dragDelay;

        [NonSerialized]
        bool _dragging;
        public bool Dragging { get { return _dragging; } set { _dragging = value; if (value) dragDelay = 0.2f; } }

        public void DisconnectDragged()
        {
            Debug.Log("Disconnecting dragged");

            MeshPoint temp = new MeshPoint(SelectedUV.pos);
            edMesh.vertices.Add(temp);

            SelectedUV.AssignToNewVertex(temp);

            edMesh.Dirty = true;//_Mesh.GenerateMesh(_targetPiece);
        }

        public void MoveVertexToGrid(MeshPoint vp)
        {
            UpdateLocalSpaceV3s();
            Vector3 diff = onGridLocal - vp.localPos;

            diff.Scale(GridNavigator.inst().getGridPerpendicularVector());
            vp.localPos += diff;
        }
        public void AssignSelected(Vertex newpnt)
        {
            SelectedUV = newpnt;
            if (Input.GetKey(KeyCode.LeftAlt))
            {
                MoveVertexToGrid(SelectedUV.meshPoint);
                edMesh.Dirty = true;
            }
            else
                if (!EditorInputManager.getControlKey())
            {
                GridNavigator.onGridPos = SelectedUV.meshPoint.worldPos;
                //  Debug.Log("Moving grid pos to " + GridNavigator.onGridPos);
               // UpdateLocalSpaceV3s();
                grid.UpdatePositions();
                // Debug.Log("Result: "+GridNavigator.onGridPos);
            }

            //trisVerts = 0;

            if (UVnavigator._inst != null)
                UVnavigator._inst.CenterOnUV(SelectedUV.editedUV);
        }

        public bool DeleteVertHEAL(MeshPoint vert)
        {

           

            Triangle[] trs = new Triangle[3];

            int cnt = 0;

            for (int i = 0; i < edMesh.triangles.Count; i++)
            {
                if (edMesh.triangles[i].includes(vert))
                {
                    if (cnt == 3) return false;
                    trs[cnt] = edMesh.triangles[i];
                    cnt++;
                }
            }

            if (cnt != 3) return false;


            trs[0].MergeAround(trs[1], vert); //Consume(trs[1]);
            edMesh.triangles.Remove(trs[1]);
            edMesh.triangles.Remove(trs[2]);

           // if ((selectedLine != null) && (selectedLine.includes(vert))) selectedLine = null;
            //if ((selectedUV != null) && (selectedUV.vert == vert)) selectedUV = null;

            edMesh.vertices.Remove(vert);


            TrisVerts = 0;

            NullPoinedSelected();

            return true;
        }

        public void SwapLine(MeshPoint a, MeshPoint b)
        {
            NullPoinedSelected();

            Triangle[] trs = new Triangle[2];
            int cnt = 0;
            for (int i = 0; i < edMesh.triangles.Count; i++)
            {
                Triangle tmp = edMesh.triangles[i];
                if (tmp.includes(a, b))
                {
                    if (cnt == 2) return;
                    trs[cnt] = tmp;
                    cnt++;
                }
            }
            if (cnt != 2) return;

            Vertex nol0 = trs[0].NotOnLine(a, b);
            Vertex nol1 = trs[1].NotOnLine(a, b);

            trs[0].Replace(trs[0].GetByVert(a), nol1);
            trs[1].Replace(trs[1].GetByVert(b), nol0);

            TrisVerts = 0;
        }

        public void DeleteLine(LineData ld)
        {
            NullPoinedSelected();

            edMesh.RemoveLine(ld);

            if (IsInTrisSet(ld.pnts[0]) || IsInTrisSet(ld.pnts[1]))
                TrisVerts = 0;

        }

        public void DeleteUv(Vertex uv)
        {
            MeshPoint vrt = uv.meshPoint;

            NullPoinedSelected();
            /*  if (pointedUV == uv) pointedUV = null;
              if (selectedUV == uv) selectedUV = null;
              if ((selectedTris != null) && selectedTris.includes(uv.vert)) selectedTris = null;
              if ((selectedLine != null) && (selectedLine.includes(uv))) selectedLine = null;
              if ((pointedTris != null) && pointedTris.includes(uv.vert)) pointedTris = null;
              if ((pointedLine != null) && (pointedLine.includes(uv))) pointedLine = null;
              */

            for (int i = 0; i < edMesh.triangles.Count; i++)
            {
                if (edMesh.triangles[i].includes(uv))
                {
                    edMesh.triangles.RemoveAt(i);
                    i--;
                }
            }

            if (IsInTrisSet(uv))
                TrisVerts = 0;


            vrt.uvpoints.Remove(uv);


            if (vrt.uvpoints.Count == 0)
            {

                edMesh.vertices.Remove(vrt);
            }



            edMesh.Dirty = true;
        }

        public void NullPoinedSelected()
        {
            PointedUV = null;
            PointedLine = null;
            PointedTris = null;
            SelectedUV = null;
            SelectedLine = null;
            SelectedTris = null;
        }

        public bool IsInTrisSet(MeshPoint vert)
        { // Only one Unique coordinate per triangle

            for (int i = 0; i < TrisVerts; i++)
                if (TrisSet[i].meshPoint == vert) return true;
            return false;
        }

        public bool IsInTrisSet(Vertex uv)
        { // Only one Unique coordinate per triangle
            for (int i = 0; i < TrisVerts; i++)
                if (TrisSet[i] == uv) return true;
            return false;
        }

        public MeshPoint AddPoint(Vector3 pos)
        {
            MeshPoint hold = new MeshPoint(pos);
            // hold.uv.Add(
            new Vertex(hold);
            edMesh.vertices.Add(hold);

            // if (m_CapsLock)
            if (!EditorInputManager.getControlKey())
                AddToTrisSet(hold.uvpoints[0]);

            if (cfg.pixelPerfectMeshEditing)
                hold.PixPerfect();

            return hold;
        }

        public void MakeTriangleVertUnique(Triangle tris, Vertex pnt)
        {
            // bool duplicant = false;

            /* for (int i = 0; i < _Mesh.triangles.Count; i++) {
                 trisDta other = _Mesh.triangles[i];
                 if ((other.includes(pnt)) && (other != tris))   {
                     duplicant = true;
                     break;
                 }
             }*/
            //if (!duplicant) return;

            if (pnt.tris.Count == 1) return;

            Vertex nuv = new Vertex(pnt.meshPoint, pnt);

            tris.Replace(pnt, nuv);

            edMesh.Dirty = true;


        }
        
        bool ProcessLinesOnTriangle(Triangle t)
        {
            t.wasProcessed = true;
            const float percision = 0.05f;

            float acc =(target.transform.InverseTransformPoint(Transform.gameObject.TryGetCameraTransform().position)  -collisionPosLocal).magnitude;

            //if (meshTool.showTriangles)
              //  acc = Mathf.Min( acc, t.ShortestLine().localLength * 0.5f
                //    );

            acc *= percision;

            if (MyMath.isPointOnLine(t.vertexes[0].meshPoint.distanceToPointed, t.vertexes[1].meshPoint.distanceToPointed, Vector3.Distance(t.vertexes[0].pos, t.vertexes[1].pos), acc))
            {
                ProcessPointOnALine(t.vertexes[0], t.vertexes[1], t);
                return true;
            }

            if (MyMath.isPointOnLine(t.vertexes[1].meshPoint.distanceToPointed, t.vertexes[2].meshPoint.distanceToPointed, Vector3.Distance(t.vertexes[1].pos, t.vertexes[2].pos), acc))
            {
                ProcessPointOnALine(t.vertexes[1], t.vertexes[2], t);
                return true;
            }

            if (MyMath.isPointOnLine(t.vertexes[2].meshPoint.distanceToPointed, t.vertexes[0].meshPoint.distanceToPointed, Vector3.Distance(t.vertexes[2].pos, t.vertexes[0].pos), acc))
            {
                ProcessPointOnALine(t.vertexes[2], t.vertexes[0], t);
                return true;
            }


            return false;
        }

        void GetPointedTRIANGLESorLINE()
        {

            edMesh.TagTrianglesUnprocessed();

            UpdateLocalSpaceV3s();

            for (int i = 0; i < edMesh.vertices.Count; i++)
                foreach (Vertex uv in edMesh.vertices[i].uvpoints)
                    foreach (Triangle t in uv.tris)
                        if (!t.wasProcessed)
                        {
                            //	Debug.Log ("Browsing");
                            t.wasProcessed = true;
                            if (t.PointOnTriangle()) {

                               if (EditorInputManager.GetMouseButtonDown(0)) {
                                    SelectedTris = t;
                                    AssignSelected(t.GetClosestTo(collisionPosLocal));
                                }

                                PointedTris = t;

                                if (MeshTool.showLines)
                                    ProcessLinesOnTriangle(PointedTris);

                                return;
                            }

                        }


        }
      
        bool Raycast_VertexIsPointed()
        {
            RaycastHit hit;
            PointedUV = null;
            bool VertexIsPointed = false;
            if (edMesh.vertices.Count > 0)
            {
                bool alt = EditorInputManager.getAltKey();

                if (alt)
                    GridNavigator.collisionPos = GridNavigator.onGridPos;

                if (Physics.Raycast(EditorInputManager.GetScreenRay(), out hit))
                {

                    VertexIsPointed = (hit.transform.tag == "VertexEd");

                    if (!alt)
                    {

                        if (VertexIsPointed)
                        {
                            GridNavigator.collisionPos = hit.transform.position;
                            UpdateLocalSpaceV3s();
                            edMesh.SortAround(collisionPosLocal, true);

                        }
                        else
                        {
                            GridNavigator.collisionPos = hit.point;
                            UpdateLocalSpaceV3s();
                            edMesh.SortAround(collisionPosLocal, true);
                            GetPointedTRIANGLESorLINE();
                        }
                    }
                }

               

                UpdateLocalSpaceV3s();
            }
            return VertexIsPointed;
        }

        void ProcessPointOnALine(Vertex a, Vertex b, Triangle t)
        {

            if (EditorInputManager.GetMouseButtonDown(1))
            {
                SelectedLine = new LineData(t, a, b);
                UpdateLocalSpaceV3s();
            }

            PointedLine = new LineData(t, new Vertex[] { a, b });

        }

        void PROCESS_KEYS()
        {

            MeshToolBase t = MeshTool;
            if (_dragging)
                t.KeysEventDragging();

            if ((t.showVertices) && (PointedUV != null))
                t.KeysEventPointedVertex();
            else if ((t.showLines) && (PointedLine != null))
                t.KeysEventPointedLine();
            else if ((t.showTriangles) && (PointedTris != null))
                t.KeysEventPointedTriangle();
        }

        void RAYCAST_SELECT_MOUSEedit()
        {

            PointedTris = null;
            PointedLine = null;

            bool pointingUV = Raycast_VertexIsPointed();

            if (_dragging)
                MeshTool.ManageDragging();
            else  {

                if ((pointingUV) && (currentUV <= edMesh.vertices[0].uvpoints.Count))   {

                    var pointedVX = edMesh.vertices[0];

                    if (currentUV == pointedVX.uvpoints.Count) currentUV--;

                    if ((SelectedUV != null) && (SelectedUV.meshPoint == pointedVX) && (!SelectingUVbyNumber))
                        PointedUV = SelectedUV;
                    else
                        PointedUV = pointedVX.uvpoints[currentUV];

                    if (EditorInputManager.GetMouseButtonDown(0))
                        AssignSelected(PointedUV);
                }

                MeshToolBase t = MeshTool;

                if ((t.showVertices) && (PointedUV != null))
                {
                    if (t.MouseEventPointedVertex())
                        editedMesh.SetLastPointed(PointedUV);
                    else editedMesh.ClearLastPointed();
                }
                else if ((t.showLines) && (PointedLine != null))
                {
                    if (t.MouseEventPointedLine())
                        editedMesh.SetLastPointed(PointedLine);
                    else editedMesh.ClearLastPointed();
                }
                else if ((t.showTriangles) && (PointedTris != null))
                {
                    if (t.MouseEventPointedTriangle())
                        editedMesh.SetLastPointed(PointedTris);
                    else editedMesh.ClearLastPointed();
                }
                else
                {
                    t.MouseEventPointedNothing();
                    editedMesh.ClearLastPointed();
                }

              

            }
        }

        void SORT_AND_UPDATE_UI() {

            if (grid == null)
                return;

            if (grid.verts[0].go == null)
                InitVertsIfNUll();

           // if (_meshTool == MeshTool.vertices)
             //   DrowLinesAroundTargetPiece();

            UpdateLocalSpaceV3s();

            edMesh.SortAround(collisionPosLocal, false);

            float scaling = 16;

            grid.selectedVertex.go.SetActiveTo(false);
            grid.pointedVertex.go.SetActiveTo(false);

            for (int i = 0; i < vertsShowMax; i++)
                grid.verts[i].go.SetActiveTo(false);

            if (MeshTool.showVertices)
            for (int i = 0; i < vertsShowMax; i++)
                if (edMesh.vertices.Count > i)  {
                    MarkerWithText mrkr = grid.verts[i];
                    MeshPoint vpoint = edMesh.vertices[i];

                    Vector3 worldPos = vpoint.worldPos;
                    float tmpScale;
                    tmpScale = Vector3.Distance(worldPos,
                        Transform.gameObject.TryGetCameraTransform().position) / scaling;
                        
                    if (GetPointedVert() == vpoint)
                    {
                        mrkr = grid.pointedVertex; tmpScale *= 2;
                    }
                    else if (GetSelectedVert() == edMesh.vertices[i])
                    {
                        mrkr = grid.selectedVertex;
                            tmpScale *= 1.5f;
                    }

                    mrkr.go.SetActiveTo(true);
                    mrkr.go.transform.position = worldPos;
                    mrkr.go.transform.rotation = Transform.gameObject.TryGetCameraTransform().rotation;
                    mrkr.go.transform.localScale = new Vector3((IsInTrisSet(vpoint) ? 1.5f : 1) * tmpScale, tmpScale, tmpScale);

                    Ray tmpRay = new Ray();
                    RaycastHit hit;
                    tmpRay.origin = Transform.gameObject.TryGetCameraTransform().position;
                    tmpRay.direction = mrkr.go.transform.position - tmpRay.origin;

                    if ((Physics.Raycast(tmpRay, out hit, 1000)) && (!PlaytimeToolComponent.MeshEditorIgnore.Contains(hit.transform.tag)))
                        mrkr.go.SetActiveTo(false);

                    if (SameTrisAsPointed(vpoint))           
                        mrkr.textm.color = Color.white;
                    else
                        mrkr.textm.color = Color.gray;


                    MeshTool.AssignText(mrkr, vpoint);

                }


        }
        
        public void DRAW_Lines(bool isGizmoCall)
        {

            GizmoLines = isGizmoCall;

            if (target == null) return;

			//Gizmos.DrawSphere (_target.transform.InverseTransformPoint(collisionPosLocal), _Mesh.distanceLimit*_target.transform.lossyScale.x);

            if (MeshTool.showTriangles)
            {
                if ((PointedTris != null) && ((PointedTris != SelectedTris) || (!MeshTool.showSelectedTriangle)))
                    OutlineTriangle(PointedTris, Color.cyan, Color.gray);

                if ((SelectedTris != null) && (MeshTool.showSelectedTriangle))
                    OutlineTriangle(SelectedTris, Color.blue, Color.white);
            }

            if (MeshTool.showLines)
            {
                if (PointedLine != null)
                    Line(PointedLine.pnts[0].meshPoint, PointedLine.pnts[1].meshPoint, Color.green );

                for (int i = 0; i < Mathf.Min(vertsShowMax, edMesh.vertices.Count); i++)
                {
                    MeshPoint vp = edMesh.vertices[i];
                    if (SameTrisAsPointed(vp))
                        Line(vp, PointedUV.meshPoint, Color.yellow);
                }
            }

            if (MeshTool.showVertices)
            {

                if (PointedUV != null)
                {
                    for (int i = 0; i < edMesh.triangles.Count; i++)
                    {
                        Triangle td = edMesh.triangles[i];
                        if (td.includes(PointedUV))
                        {

                            Line(td.vertexes[1].meshPoint, td.vertexes[0].meshPoint, Color.yellow);
                            Line(td.vertexes[1].meshPoint, td.vertexes[2].meshPoint, Color.yellow);
                            Line(td.vertexes[2].meshPoint, td.vertexes[0].meshPoint, Color.yellow);
                        }
                    }
                    //Vector3 selPos = pointedUV.vert.worldPos; //.pos.ToV3 (false);
                    //Gizmos.color = Color.green;
                    //Gizmos.DrawLine(selPos, GridNavigator.inst().ProjectToGrid(selPos));
                    //Line(selPos, GridNavigator.inst().ProjectToGrid(selPos), Color.green);
                }

                /*if (selectedUV != null)
                {
                    Vector3 selPos = selectedUV.vert.getWorldPos();//.pos.ToV3 (false);
                    Debug.DrawLine(selPos, GridNavigator.inst().ProjectToGrid(selPos), Color.green);
                }*/
            }
        }

        public void CombinedUpdate() {

            if (target == null)
                return;

			if (!target.enabled)  {
                DisconnectMesh();
                return;
            }

            int no = EditorInputManager.GetNumberKeyDown();
            SelectingUVbyNumber = false;
            if (no != -1) { currentUV = no - 1; SelectingUVbyNumber = true; } else currentUV = 0;
            
            if (Application.isPlaying)
                UpdateInputPlaytime();

             grid.UpdatePositions();
            
            if (Application.isPlaying)
                SORT_AND_UPDATE_UI();

            if (edMesh.Dirty) {
                redoMoves.Clear();
                undoMoves.Add(edMesh.Encode().ToString());
                if (undoMoves.Count > 10)
                    undoMoves.RemoveAt(0);
                Redraw();
                previewMesh = null;
            }

            if (justLoaded >= 0)
            justLoaded --;

        }
        
#if UNITY_EDITOR
        public void UpdateInputEditorTime(Event e, bool up, bool dwn)
        {

            if (target == null || justLoaded > 0)
                return;

           

            if (e.type == EventType.KeyDown) {
               
                PROCESS_KEYS();

                switch (e.keyCode)
                {

                    case KeyCode.Delete: //Debug.Log("Use Backspace to delete vertices"); goto case KeyCode.Backspace;
                    case KeyCode.Backspace: e.Use(); break;
                }
            }
            
            if (e.isMouse || (e.type == EventType.ScrollWheel)) 
                RAYCAST_SELECT_MOUSEedit();
            
            SORT_AND_UPDATE_UI();

            return;
        }
#endif

        public void UpdateInputPlaytime()
        {
            #if PEGI
            if (pegi.mouseOverUI)
                return;
            #endif

            RAYCAST_SELECT_MOUSEedit();
            PROCESS_KEYS();           
        }

        // Not redirected yet
        public void EditingUpdate()
        {
            if ((Application.isPlaying == false)) // && (_target != null ) && (UnityHelperFunctions.getFocused() == _target))
                CombinedUpdate();
        }

        public void Update()
        {
            if (Application.isPlaying)
                CombinedUpdate();
        }

        public MeshPoint GetPointedVert()
        {
            if (PointedUV != null) return PointedUV.meshPoint;
            return null;
        }
        public MeshPoint GetSelectedVert()
        {
            if (SelectedUV != null) return SelectedUV.meshPoint;
            return null;
        }
        bool SameTrisAsPointed(MeshPoint uvi)
        {
            if (PointedUV == null) return false;
            foreach (Triangle t in edMesh.triangles)
            {
                if (t.includes(uvi) && t.includes(PointedUV)) return true;
            }
            return false;
        }
        
        void OutlineTriangle(Triangle t, Color colA, Color colB)
        {
            //bool vrt = tool == VertexPositionTool.inst;
            Line(t.vertexes[0], t.vertexes[1], t.DominantCourner[0] ? colA : colB, t.DominantCourner[1] ? colA : colB);
            Line(t.vertexes[1], t.vertexes[2], t.DominantCourner[1] ? colA : colB, t.DominantCourner[2] ? colA : colB);
            Line(t.vertexes[0], t.vertexes[2], t.DominantCourner[0] ? colA : colB, t.DominantCourner[2] ? colA : colB);
        }
        
        void Line(Vertex  a, Vertex b, Color col, Color colb) {
            Line(a.meshPoint, b.meshPoint, col, colb);
        }

        void Line(MeshPoint a, MeshPoint b, Color col, Color colb) {

            Vector3 v3a = a.worldPos;
            Vector3 v3b = b.worldPos;
            Vector3 diff = (v3b - v3a) / 2;
            Line(v3a, v3a + diff, col);
            Line(v3b, v3b - diff, colb);
        }

        void Line(MeshPoint a, MeshPoint b, Color col) {
          
            Line(a.worldPos, b.worldPos, col);
        }
        
        public bool GizmoLines = false;
        void Line(Vector3 from, Vector3 to, Color col) {
            if (GizmoLines) {
                Gizmos.color = col;
                Gizmos.DrawLine(from, to);

            } else 
                Debug.DrawLine(from, to, col);
        }

        public void DrowLinesAroundTargetPiece()
        {

            Vector3 piecePos = target.transform.TransformPoint(-Vector3.one / 2);//PositionScripts.PosUpdate(_target.getpos(), false);


            Vector3 projected = GridNavigator.inst().ProjectToGrid(piecePos); // piecePos * getGridMaskVector() + ptdPos.ToV3(false)*getGridPerpendicularVector();
            Vector3 GridMask = GridNavigator.inst().getGridMaskVector() * 128 + projected;



            Debug.DrawLine(new Vector3(projected.x, projected.y, projected.z), new Vector3(GridMask.x, projected.y, projected.z), Color.red);
            Debug.DrawLine(new Vector3(projected.x, projected.y, projected.z), new Vector3(projected.x, GridMask.y, projected.z), Color.red);
            Debug.DrawLine(new Vector3(projected.x, projected.y, projected.z), new Vector3(projected.x, projected.y, GridMask.z), Color.red);

            Debug.DrawLine(new Vector3(projected.x, GridMask.y, GridMask.z), new Vector3(GridMask.x, GridMask.y, GridMask.z), Color.red);
            Debug.DrawLine(new Vector3(GridMask.x, projected.y, GridMask.z), new Vector3(GridMask.x, GridMask.y, GridMask.z), Color.red);
            Debug.DrawLine(new Vector3(GridMask.x, GridMask.y, projected.z), new Vector3(GridMask.x, GridMask.y, GridMask.z), Color.red);

            MyDebugClasses.DrawTransformedCubeDebug(target.transform, Color.blue);


        }
        
        void InitVertsIfNUll()
        {
            if (grid == null)
                return;
            
            if (grid.vertPrefab == null)
                grid.vertPrefab = Resources.Load("prefabs/vertex") as GameObject;

            if ((grid.verts == null) || (grid.verts.Length == 0) || (grid.verts[0].go == null))
            {
                grid.verts = new MarkerWithText[vertsShowMax];

                for (int i = 0; i < vertsShowMax; i++)
                {
                    MarkerWithText v = new MarkerWithText();
                    grid.verts[i] = v;
                    v.go = GameObject.Instantiate(grid.vertPrefab);
                    v.go.transform.parent = grid.transform;
                    v.Init();
                }
            }

            grid.pointedVertex.Init();
            grid.selectedVertex.Init();

#if UNITY_EDITOR
            EditorApplication.update -= EditingUpdate;
            if (!PainterManager.inst.ApplicationIsAboutToEnterPlayMode())
                EditorApplication.update += EditingUpdate;
#endif
        }

        public void OnEnable() {
            InitVertsIfNUll();

            if ((previouslyEdited != null) && (target == null)) {
                DisconnectMesh();
                EditMesh(previouslyEdited, false);
                justLoaded = 5;
            }

            previouslyEdited = null;
            TrisVerts = 0;
        }


        int justLoaded;

#if PEGI
         List<PlaytimePainter> selectedPainters = new List<PlaytimePainter>();
        bool showReferences = false;
      
        bool showTooltip;
        bool showCopyOptions;
        public bool PEGI()  {

            bool changed = false;
            EditableMesh.inspected = edMesh;

            pegi.newLine();
            pegi.Space();

            pegi.newLine();

            changed |= target.PreviewShaderToggle_PEGI();

            if ((!target.isOriginalShader) && ("preview".select(45, ref meshSHaderMode.selected, meshSHaderMode.allModes).nl()))
                meshSHaderMode.selected.Apply();

            pegi.Space();

            pegi.newLine();

            var previousTool = MeshTool;

            if ("tool".select(70, ref cfg._meshTool, MeshToolBase.allTools).nl()) {
                grid.vertexPointMaterial.SetColor("_Color", MeshTool.vertColor);
                previousTool.OnDeSelectTool();
                MeshTool.OnSelectTool();
            }

            pegi.Space();

            pegi.newLine();

            "Mesh Name:".edit(70, ref target.meshNameHolder);

#if UNITY_EDITOR
            if (((AssetDatabase.GetAssetPath(target.getMesh()).Length==0) || (String.Compare(target.meshNameHolder, target.getMesh().name)!=0))  && 
                (icon.Save.Click("Save Mesh As "+target.GenerateMeshSavePath(),25).nl())) target.SaveMesh();
#endif

            pegi.newLine();

            pegi.nl();

            MeshTool.PEGI();

            pegi.newLine();

            if ("Hint".foldout(ref showTooltip).nl())
                pegi.writeHint(MeshTool.tooltip);
            
            if ("Merge Meshes".foldout(ref showCopyOptions).nl()) {

                if (!selectedPainters.Contains(target)) {
                    if ("Copy Mesh".Click("Add Mesh to the list of meshes to be merged").nl())
                        selectedPainters.Add(target);

                    if (selectedPainters.Count > 0) {

                        if (edMesh.UV2distributeRow < 2 && "Enable EV2 Distribution".toggleInt("Each mesh's UV2 will be modified to use a unique portion of a texture.", ref edMesh.UV2distributeRow).nl())
                            edMesh.UV2distributeRow = Mathf.Max(2, (int)Mathf.Sqrt(selectedPainters.Count));
                        else
                        {
                            if (edMesh.UV2distributeCurrent > 0)
                            {
                                ("All added meshes will be distributed in " + edMesh.UV2distributeRow + " by " + edMesh.UV2distributeRow + " grid. By cancelling this added" +
                                    "meshes will have UVs unchanged and may use the same portion of Texture (sampled with UV2) as other meshes.").writeHint();
                                if ("Cancel Distribution".Click().nl())
                                    edMesh.UV2distributeRow = 0;
                            }
                            else {
                                "Row:".edit("Will change UV2 so that every mesh will have it's own portion of a texture.", 25, ref edMesh.UV2distributeRow, 2, 16).nl();
                                "Start from".edit(ref edMesh.UV2distributeCurrent).nl();
                            }
                            pegi.write("Using " + (edMesh.UV2distributeCurrent + selectedPainters.Count + 1) + " out of " + (edMesh.UV2distributeRow * edMesh.UV2distributeRow).ToString() + " spots");
                            pegi.newLine();
                        }

                        "Will Merge with the following:".nl();
                        for (int i = 0; i < selectedPainters.Count; i++)
                        {
                            if (selectedPainters[i] == null)
                            {
                                selectedPainters.RemoveAt(i);
                                i--;
                            }
                            else
                            {
                                if (icon.Delete.Click(25))
                                {
                                    selectedPainters.RemoveAt(i);
                                    i--;
                                }
                                else
                                    selectedPainters[i].gameObject.name.nl();

                            }
                        }

                        if ("Merge!".Click().nl())
                        {

                            foreach (var p in selectedPainters)
                                edMesh.MergeWith(p);

                            edMesh.Dirty = true;

                        }
                    }

                }
                else
                {
                    if ("Remove from Copy Selection".Click().nl())
                        selectedPainters.Remove(target);
                }
            }



            pegi.newLine();

            if (edMesh != null)
                edMesh.PEGI().nl();

            grid.vertexPointMaterial.SetColor("_Color", MeshTool.vertColor);

            if ((!Application.isPlaying) && ("references".foldout(ref showReferences).nl()))  {

                "vertexPointMaterial".write(grid.vertexPointMaterial);
                pegi.newLine();

                "vertexPrefab".edit(ref grid.vertPrefab).nl();
                "Max Vert Markers ".edit(ref vertsShowMax).nl();
                "pointedVertex".edit(ref grid.pointedVertex.go).nl();
                "SelectedVertex".edit(ref grid.selectedVertex.go).nl();
            }

            EditableMesh.inspected = null;

            return changed;
        }

        public bool Undo_redo_PEGI()
        {
            bool changed = false;

            if (undoMoves.Count > 1) {
                if (pegi.Click(icon.Undo.getIcon(), 25)) {
                    redoMoves.Add(undoMoves.RemoveLast());
                    undoMoves.Last().DecodeInto(out edMesh);
                    Redraw();
                    changed = true;
                }
            }
            else
                pegi.Click(icon.UndoDisabled.getIcon(), "Nothing to Undo (set number of undo frames in config)", 25);

            if (redoMoves.Count > 0) {
                if (pegi.Click(icon.Redo.getIcon(),  25)) {
                    changed = true;
                    redoMoves.Last().DecodeInto(out edMesh);
                    undoMoves.Add(redoMoves.RemoveLast());
                    Redraw();
                }
            }
            else
                pegi.Click(icon.RedoDisabled.getIcon(), "Nothing to Redo", 25);

            pegi.newLine();

            return changed;
        }

#endif

    }
}