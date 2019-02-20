using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Playtime_Painter
{

    public class MeshManager : PainterStuffKeepUnrecognized_STD
    {

        public static MeshManager Inst => PainterCamera.MeshManager;

        public static Transform Transform => PainterCamera.Inst?.transform;

        public static float animTextureSize = 128;

        public MeshToolBase MeshTool => PainterCamera.Data.MeshTool;

        int _editedUV = 0;

        public int EditedUV {
            get { return _editedUV; }
            set { _editedUV = value;  UnityHelperFunctions.SetShaderKeyword(PainterDataAndConfig._MESH_PREVIEW_UV2, _editedUV == 1);  }

        }
        public static Vector3 editorMousePos;

        public PlaytimePainter target;
        public PlaytimePainter previouslyEdited;

        List<string> undoMoves = new List<string>();

        List<string> redoMoves = new List<string>();

        public EditableMesh editedMesh = new EditableMesh();

        public EditableMesh previewEdMesh = new EditableMesh();

        public Mesh previewMesh;

        public AddCubeCfg tmpCubeCfg = new AddCubeCfg();
        
        #region Encode & Decode

        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add_IfTrue("byUV", SelectingUVbyNumber);

        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "byUV": SelectingUVbyNumber = data.ToBool(); break;
                default: return false;
            }
            return true;
        }

        #endregion

        int currentUV = 0;
        bool SelectingUVbyNumber = false;

        public Vertex SelectedUV { get { return editedMesh.selectedUv; } set { editedMesh.selectedUv = value; } }
        public LineData SelectedLine { get { return editedMesh.selectedLine; } set { editedMesh.selectedLine = value; } }
        public Triangle SelectedTris { get { return editedMesh.selectedTris; } set { editedMesh.selectedTris = value; } }
        public Vertex PointedUV { get { return editedMesh.pointedUv; } set { editedMesh.pointedUv = value; } }
        public LineData PointedLine { get { return editedMesh.pointedLine; } set { editedMesh.pointedLine = value; } }
        public Triangle PointedTris { get { return editedMesh.pointedTris; } set { editedMesh.pointedTris = value; } }
        Vertex[] TrisSet { get { return editedMesh.trisSet; } set { editedMesh.trisSet = value; } }
        public int TriVertices { get { return editedMesh.triVertices; } set { editedMesh.triVertices = value; } }

        [NonSerialized]
        public int vertsShowMax = 8;

        [NonSerialized]
        public Vector3 onGridLocal = new Vector3();
        [NonSerialized]
        public Vector3 collisionPosLocal = new Vector3();

        public void UpdateLocalSpaceV3s()
        {
            if (target != null)
            {
                onGridLocal = target.transform.InverseTransformPoint(GridNavigator.onGridPos);
                collisionPosLocal = target.transform.InverseTransformPoint(GridNavigator.collisionPos);
            }
        }

        public void EditMesh(PlaytimePainter pntr, bool EditCopy)
        {
            if ((!pntr) || (pntr == target))
                return;

            if (target != null)
                DisconnectMesh();

            target = pntr;

            editedMesh = new EditableMesh();

            editedMesh.Edit(pntr);

            if (EditCopy)
                pntr.SharedMesh = new Mesh();

            Redraw();

            pntr.meshNameField = editedMesh.meshName;

            InitVertsIfNUll();

            SelectedLine = null;
            SelectedTris = null;
            SelectedUV = null;

            undoMoves.Clear();
            redoMoves.Clear();

            undoMoves.Add(editedMesh.Encode().ToString());

            MeshTool.OnSelectTool();

        }

        public void DisconnectMesh()
        {

            if (target != null)
            {
                MeshTool.OnDeSelectTool();
                target.SavedEditableMesh = editedMesh.Encode().ToString();
                target = null;
            }
            Grid.DeactivateVertices();
            GridNavigator.Inst().SetEnabled(false, false);
            undoMoves.Clear();
            redoMoves.Clear();
        }

#if UNITY_EDITOR
        public void SaveGeneratedMeshAsAsset()
        {
            AssetDatabase.CreateAsset(target.SharedMesh, "Assets/Models/" + target.gameObject.name + "_export.asset");
            AssetDatabase.SaveAssets();
        }
#endif

        public void Redraw()
        {

            if (target != null)
            {

                MeshConstructor mc = new MeshConstructor(editedMesh, target.MeshProfile, target.SharedMesh);

                if (!editedMesh.dirtyNormals && EditedMesh.Dirty)
                {

                    if (EditedMesh.dirtyPosition)
                        mc.UpdateMesh<MeshSolutions.VertexPos>();

                    if (editedMesh.dirtyColor)
                        mc.UpdateMesh<MeshSolutions.VertexColor>();

                }
                else
                {
                    var m = mc.Construct();
                    target.SharedMesh = m;
                    target.meshCollider.AssignMeshAsCollider(m);
                }

            }

            editedMesh.Dirty = false;
        }

        [NonSerialized]
        double dragStartTime;
        public double DragDelay {
            get
            {
                return dragStartTime - UnityHelperFunctions.TimeSinceStartup();   
            }

            set {

                dragStartTime = UnityHelperFunctions.TimeSinceStartup() + value;

            }

        }

        [NonSerialized]
        bool _dragging;
        public bool Dragging { get { return _dragging; } set { _dragging = value; if (value) DragDelay = 0.4f; } }

        #region Vertex Operations

        public static Vector2 RoundUVs(Vector2 source, float accuracy)
        {
            Vector2 uv = source * accuracy;
            uv.x = Mathf.Round(uv.x);
            uv.y = Mathf.Round(uv.y);
            uv /= accuracy;
            return uv;
        }

        public void AddToTrisSet(Vertex nuv)
        {

            TrisSet[TriVertices] = nuv;
            TriVertices++;

            if (TriVertices == 3)
                foreach (Triangle t in editedMesh.triangles)
                    if (t.IsSamePoints(TrisSet))
                    {
                        t.Set(TrisSet);
                        editedMesh.Dirty = true;
                        TriVertices = 0;
                        return;
                    }
                
            

            if (TriVertices >= 3)
            {
                Triangle td = new Triangle(TrisSet);
                editedMesh.triangles.Add(td);

                if (!EditorInputManager.Control)
                {
                    MakeTriangleVertUnique(td, TrisSet[0]);
                    MakeTriangleVertUnique(td, TrisSet[1]);
                    MakeTriangleVertUnique(td, TrisSet[2]);
                }

                TriVertices = 0;
                editedMesh.Dirty = true;
            }
        }


        public void DisconnectDragged()
        {
            Debug.Log("Disconnecting dragged");

            MeshPoint temp = new MeshPoint(SelectedUV.Pos);
            editedMesh.meshPoints.Add(temp);

            SelectedUV.AssignToNewVertex(temp);

            editedMesh.Dirty = true;//_Mesh.GenerateMesh(_targetPiece);
        }

        public void MoveVertexToGrid(MeshPoint vp)
        {
            UpdateLocalSpaceV3s();
            Vector3 diff = onGridLocal - vp.localPos;

            diff.Scale(GridNavigator.Inst().GetGridPerpendicularVector());
            vp.localPos += diff;
        }
        public void AssignSelected(Vertex newpnt)
        {
            SelectedUV = newpnt;
            if (Input.GetKey(KeyCode.LeftAlt))
            {
                MoveVertexToGrid(SelectedUV.meshPoint);
                editedMesh.Dirty = true;
            }
            else
                if (!EditorInputManager.Control)
            {
                GridNavigator.onGridPos = SelectedUV.meshPoint.WorldPos;
                Grid.UpdatePositions();
            }
        }

        public bool DeleteVertHEAL(MeshPoint vert)
        {



            var trs = new Triangle[3];

            var cnt = 0;

            foreach (var t in editedMesh.triangles)
            {
                if (!t.Includes(vert)) continue;
                if (cnt == 3) return false;
                trs[cnt] = t;
                cnt++;
            }

            if (cnt != 3) return false;

            trs[0].MergeAround(trs[1], vert); 
            editedMesh.triangles.Remove(trs[1]);
            editedMesh.triangles.Remove(trs[2]);

            editedMesh.meshPoints.Remove(vert);

            TriVertices = 0;

            NullPointedSelected();

            return true;
        }

        public void SwapLine(MeshPoint a, MeshPoint b)
        {
            NullPointedSelected();

            var trs = new Triangle[2];
            var cnt = 0;
            foreach (var tmp in editedMesh.triangles)
            {
                if (!tmp.Includes(a, b)) continue;
                
                if (cnt == 2) return;
                trs[cnt] = tmp;
                cnt++;
            }
            if (cnt != 2) return;

            var nol0 = trs[0].GetNotOneOf(a, b);
            var nol1 = trs[1].GetNotOneOf(a, b);

            trs[0].Replace(trs[0].GetByVert(a), nol1);
            trs[1].Replace(trs[1].GetByVert(b), nol0);

            TriVertices = 0;
        }

        public void DeleteLine(LineData ld)
        {
            NullPointedSelected();

            editedMesh.RemoveLine(ld);

            if (IsInTrisSet(ld.pnts[0]) || IsInTrisSet(ld.pnts[1]))
                TriVertices = 0;

        }

        public void DeleteUv(Vertex uv)
        {
            var vrt = uv.meshPoint;

            NullPointedSelected();
            /*  if (pointedUV == uv) pointedUV = null;
              if (selectedUV == uv) selectedUV = null;
              if ((selectedTris != null) && selectedTris.includes(uv.vert)) selectedTris = null;
              if ((selectedLine != null) && (selectedLine.includes(uv))) selectedLine = null;
              if ((pointedTris != null) && pointedTris.includes(uv.vert)) pointedTris = null;
              if ((pointedLine != null) && (pointedLine.includes(uv))) pointedLine = null;
              */

            for (var i = 0; i < editedMesh.triangles.Count; i++)
            {
                if (!editedMesh.triangles[i].Includes(uv)) continue;
                
                editedMesh.triangles.RemoveAt(i);
                i--;
            }

            if (IsInTrisSet(uv))
                TriVertices = 0;


            vrt.uvpoints.Remove(uv);


            if (vrt.uvpoints.Count == 0)
            {

                editedMesh.meshPoints.Remove(vrt);
            }



            editedMesh.Dirty = true;
        }

        private void NullPointedSelected()
        {
            PointedUV = null;
            PointedLine = null;
            PointedTris = null;
            SelectedUV = null;
            SelectedLine = null;
            SelectedTris = null;
        }

        public bool IsInTrisSet(MeshPoint vert)
        { 
            for (var i = 0; i < TriVertices; i++)
                if (TrisSet[i].meshPoint == vert) return true;
            return false;
        }

        private bool IsInTrisSet(Vertex uv)
        { 
            for (var i = 0; i < TriVertices; i++)
                if (TrisSet[i] == uv) return true;
            return false;
        }

        public MeshPoint AddPoint(Vector3 pos)
        {
            var hold = new MeshPoint(pos);
            new Vertex(hold);
            editedMesh.meshPoints.Add(hold);
            
            if (!EditorInputManager.Control)
                AddToTrisSet(hold.uvpoints[0]);

            if (Cfg.pixelPerfectMeshEditing)
                hold.PixPerfect();

            return hold;
        }

        public void MakeTriangleVertUnique(Triangle tris, Vertex pnt)
        {

            if (pnt.tris.Count == 1) return;

            Vertex nuv = new Vertex(pnt.meshPoint, pnt);

            tris.Replace(pnt, nuv);

            editedMesh.Dirty = true;

        }
        #endregion

        #region Tool MGMT
        bool ProcessLinesOnTriangle(Triangle t)
        {
            t.wasProcessed = true;
            const float percision = 0.05f;

            float acc = (target.transform.InverseTransformPoint(Transform.gameObject.TryGetCameraTransform().position) - collisionPosLocal).magnitude;

            acc *= percision;

            if (MyMath.IsPointOnLine(t.vertexes[0].meshPoint.distanceToPointed, t.vertexes[1].meshPoint.distanceToPointed, Vector3.Distance(t.vertexes[0].Pos, t.vertexes[1].Pos), acc))
            {
                ProcessPointOnALine(t.vertexes[0], t.vertexes[1], t);
                return true;
            }

            if (MyMath.IsPointOnLine(t.vertexes[1].meshPoint.distanceToPointed, t.vertexes[2].meshPoint.distanceToPointed, Vector3.Distance(t.vertexes[1].Pos, t.vertexes[2].Pos), acc))
            {
                ProcessPointOnALine(t.vertexes[1], t.vertexes[2], t);
                return true;
            }

            if (MyMath.IsPointOnLine(t.vertexes[2].meshPoint.distanceToPointed, t.vertexes[0].meshPoint.distanceToPointed, Vector3.Distance(t.vertexes[2].Pos, t.vertexes[0].Pos), acc))
            {
                ProcessPointOnALine(t.vertexes[2], t.vertexes[0], t);
                return true;
            }


            return false;
        }

        void GetPointedTRIANGLESorLINE()
        {

            editedMesh.TagTrianglesUnprocessed();

            UpdateLocalSpaceV3s();

            for (int i = 0; i < editedMesh.meshPoints.Count; i++)
                foreach (Vertex uv in editedMesh.meshPoints[i].uvpoints)
                    foreach (Triangle t in uv.tris)
                        if (!t.wasProcessed)
                        {
                            //	Debug.Log ("Browsing");
                            t.wasProcessed = true;
                            if (t.PointOnTriangle())
                            {

                                if (EditorInputManager.GetMouseButtonDown(0))
                                {
                                    SelectedTris = t;
                                    AssignSelected(t.GetClosestTo(collisionPosLocal));
                                }

                                PointedTris = t;

                                if (MeshTool.ShowLines)
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
            if (editedMesh.meshPoints.Count > 0)
            {
                bool alt = EditorInputManager.Alt;

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
                            editedMesh.SortAround(collisionPosLocal, true);

                        }
                        else
                        {
                            GridNavigator.collisionPos = hit.point;
                            UpdateLocalSpaceV3s();
                            editedMesh.SortAround(collisionPosLocal, true);
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

            if ((t.ShowVertices) && (PointedUV != null))
                t.KeysEventPointedVertex();
            else if ((t.ShowLines) && (PointedLine != null))
                t.KeysEventPointedLine();
            else if ((t.ShowTriangles) && (PointedTris != null))
                t.KeysEventPointedTriangle();
        }

        void RAYCAST_SELECT_MOUSEedit()
        {

            PointedTris = null;
            PointedLine = null;

            bool pointingUV = Raycast_VertexIsPointed();

            if (_dragging)
                MeshTool.ManageDragging();

            if (!_dragging)
            {

                if (pointingUV && currentUV <= editedMesh.meshPoints[0].uvpoints.Count)
                {

                    var pointedVX = editedMesh.meshPoints[0];

                    if (currentUV == pointedVX.uvpoints.Count) currentUV--;

                    if ((SelectedUV != null) && (SelectedUV.meshPoint == pointedVX) && (!SelectingUVbyNumber))
                        PointedUV = SelectedUV;
                    else
                        PointedUV = pointedVX.uvpoints[currentUV];

                    if (EditorInputManager.GetMouseButtonDown(0))
                        AssignSelected(PointedUV);
                }

                MeshToolBase t = MeshTool;

                if (t.ShowVertices && PointedUV != null)
                {
                    if (t.MouseEventPointedVertex())
                        EditedMesh.SetLastPointed(PointedUV);
                    else EditedMesh.ClearLastPointed();
                }
                else if (t.ShowLines && PointedLine != null)
                {
                    if (t.MouseEventPointedLine())
                        EditedMesh.SetLastPointed(PointedLine);
                    else EditedMesh.ClearLastPointed();
                }
                else if (t.ShowTriangles && PointedTris != null)
                {
                    if (t.MouseEventPointedTriangle())
                        EditedMesh.SetLastPointed(PointedTris);
                    else EditedMesh.ClearLastPointed();
                }
                else
                {
                    t.MouseEventPointedNothing();
                    EditedMesh.ClearLastPointed();
                }



            }
        }

        void SORT_AND_UPDATE_UI()
        {

            if (!Grid)
                return;

            if (!Grid.vertices[0].go)
                InitVertsIfNUll();

            UpdateLocalSpaceV3s();

            editedMesh.SortAround(collisionPosLocal, false);

            float scaling = 16;

            Grid.selectedVertex.go.SetActiveTo(false);
            Grid.pointedVertex.go.SetActiveTo(false);

            for (int i = 0; i < vertsShowMax; i++)
                Grid.vertices[i].go.SetActiveTo(false);

            if (MeshTool.ShowVertices)
                for (int i = 0; i < vertsShowMax; i++)
                    if (editedMesh.meshPoints.Count > i)
                    {
                        MarkerWithText mrkr = Grid.vertices[i];
                        MeshPoint vpoint = editedMesh.meshPoints[i];

                        Vector3 worldPos = vpoint.WorldPos;
                        float tmpScale;
                        tmpScale = Vector3.Distance(worldPos,
                            Transform.gameObject.TryGetCameraTransform().position) / scaling;

                        if (GetPointedVert() == vpoint)
                        {
                            mrkr = Grid.pointedVertex; tmpScale *= 2;
                        }
                        else if (GetSelectedVert() == editedMesh.meshPoints[i])
                        {
                            mrkr = Grid.selectedVertex;
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

                        if ((Physics.Raycast(tmpRay, out hit, 1000)) && (!MeshEditorIgnore.Contains(hit.transform.tag)))
                            mrkr.go.SetActiveTo(false);

                        if (SameTrisAsPointed(vpoint))
                            mrkr.textm.color = Color.white;
                        else
                            mrkr.textm.color = Color.gray;


                        MeshTool.AssignText(mrkr, vpoint);

                    }


        }
        #endregion

        public static List<string> MeshEditorIgnore = new List<string> { "VertexEd", "toolComponent" };

        public static bool MesherCanEditWithTag(string tag)
        {
            foreach (string x in MeshEditorIgnore)
                if (tag.Contains(x))
                    return false;
            return true;
        }
        
        public void CombinedUpdate()
        {

            if (!target)
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
            
            Grid?.UpdatePositions();

            if (Application.isPlaying)
                SORT_AND_UPDATE_UI();

            if (editedMesh.Dirty)
            {
                redoMoves.Clear();
                undoMoves.Add(editedMesh.Encode().ToString());
                if (undoMoves.Count > 10)
                    undoMoves.RemoveAt(0);
                Redraw();
                previewMesh = null;
            }

            if (justLoaded >= 0)
                justLoaded--;

        }

        #if UNITY_EDITOR
        public void UpdateInputEditorTime(Event e, bool up, bool dwn)
        {

            if (!target || justLoaded > 0)
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
           /* else if (_dragging) {
                Debug.Log("Setting dirty manually");
                editedMesh.dirty_Position = true;
                _dragging = false;
            }*/
            
            SORT_AND_UPDATE_UI();

            return;
        }
        #endif

        public void UpdateInputPlaytime()
        {
            #if PEGI
            if (pegi.MouseOverPlaytimePainterUI)
                return;
            #endif

            RAYCAST_SELECT_MOUSEedit();
            PROCESS_KEYS();
        }

        public void EditingUpdate()
        {
            if ((!Application.isPlaying))
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
            foreach (Triangle t in editedMesh.triangles)
            {
                if (t.Includes(uvi) && t.Includes(PointedUV)) return true;
            }
            return false;
        }
        
        void InitVertsIfNUll()
        {
            if (!Grid)
                return;

            if (!Grid.vertPrefab)
                Grid.vertPrefab = Resources.Load("prefabs/vertex") as GameObject;

            if ((Grid.vertices == null) || (Grid.vertices.Length == 0) || (!Grid.vertices[0].go))
            {
                Grid.vertices = new MarkerWithText[vertsShowMax];

                for (int i = 0; i < vertsShowMax; i++)
                {
                    MarkerWithText v = new MarkerWithText();
                    Grid.vertices[i] = v;
                    v.go = GameObject.Instantiate(Grid.vertPrefab);
                    v.go.transform.parent = Grid.transform;
                    v.Init();
                }
            }

            Grid.pointedVertex.Init();
            Grid.selectedVertex.Init();
        }

        public void OnEnable()
        {
            InitVertsIfNUll();

            if ((previouslyEdited != null) && (!target))
            {
                DisconnectMesh();
                EditMesh(previouslyEdited, false);
                justLoaded = 5;
            }

            previouslyEdited = null;
            TriVertices = 0;
            EditedUV = EditedUV;

        }
        
        int justLoaded;

        #region Inspector
#if PEGI
        List<PlaytimePainter> selectedPainters = new List<PlaytimePainter>();
        bool showReferences = false;
        bool inspectMesh = false;
        bool showTooltip;
        bool showCopyOptions;
        public override bool Inspect()  {

            bool changed = false;
            EditableMesh.inspected = editedMesh;

            pegi.newLine();
            
            if (editedMesh != null && "Mesh ".foldout(ref inspectMesh).nl())
                changed |= editedMesh.Nested_Inspect().nl();

            pegi.space();
            pegi.nl();

            target.PreviewShaderToggle_PEGI().changes(ref changed);

            if (!target.IsOriginalShader && "preview".select(45, ref MeshSHaderMode.selected, MeshSHaderMode.AllModes).nl(ref changed))
                MeshSHaderMode.ApplySelected();

            pegi.space();
            pegi.nl();

            var previousTool = MeshTool;

            if ("tool".select(70, ref Cfg.meshTool, MeshToolBase.AllTools).nl(ref changed)) {
                Grid.vertexPointMaterial.SetColor("_Color", MeshTool.VertColor);
                previousTool.OnDeSelectTool();
                MeshTool.OnSelectTool();
            }

            pegi.space();
            pegi.newLine();

            "Mesh Name:".edit(70, ref target.meshNameField).changes(ref changed);

#if UNITY_EDITOR
            var mesh = target.GetMesh();

            bool exists = !AssetDatabase.GetAssetPath(mesh).IsNullOrEmpty();
            
            if ((exists ? icon.Save : icon.SaveAsNew).Click("Save Mesh As {0}".F(target.GenerateMeshSavePath()), 25).nl())
                target.SaveMesh();
#endif
            
            pegi.nl();

            var mt = MeshTool;

            mt.Inspect().nl(ref changed);

            foreach (var p in PainterManagerPluginBase.VertexEdgePlugins)
                p.MeshToolInspection(mt).nl(ref changed);

            if ("Hint".foldout(ref showTooltip).nl())
                MeshTool.Tooltip.writeHint();
            
            if ("Merge Meshes".foldout(ref showCopyOptions).nl()) {

                if (!selectedPainters.Contains(target)) {
                    if ("Copy Mesh".Click("Add Mesh to the list of meshes to be merged").nl(ref changed))
                        selectedPainters.Add(target);

                    if (!selectedPainters.IsNullOrEmpty()) {

                        if (editedMesh.uv2DistributeRow < 2 && "Enable EV2 Distribution".toggleInt("Each mesh's UV2 will be modified to use a unique portion of a texture.", ref editedMesh.uv2DistributeRow).nl(ref changed))
                            editedMesh.uv2DistributeRow = Mathf.Max(2, (int)Mathf.Sqrt(selectedPainters.Count));
                        else
                        {
                            if (editedMesh.uv2DistributeCurrent > 0)
                            {
                                ("All added meshes will be distributed in " + editedMesh.uv2DistributeRow + " by " + editedMesh.uv2DistributeRow + " grid. By cancelling this added" +
                                    "meshes will have UVs unchanged and may use the same portion of Texture (sampled with UV2) as other meshes.").writeHint();
                                if ("Cancel Distribution".Click().nl())
                                    editedMesh.uv2DistributeRow = 0;
                            }
                            else {
                                "Row:".edit("Will change UV2 so that every mesh will have it's own portion of a texture.", 25, ref editedMesh.uv2DistributeRow, 2, 16).nl(ref changed);
                                "Start from".edit(ref editedMesh.uv2DistributeCurrent).nl(ref changed);
                            }

                            "Using {0} out of {1} spots".F(editedMesh.uv2DistributeCurrent + selectedPainters.Count + 1, editedMesh.uv2DistributeRow * editedMesh.uv2DistributeRow).nl();
                          
                        }

                        "Will Merge with the following:".nl();
                        for (int i = 0; i < selectedPainters.Count; i++)
                            if (!selectedPainters[i] || icon.Delete.Click(25)) {
                                selectedPainters.RemoveAt(i);
                                i--;
                            }
                        else
                                selectedPainters[i].gameObject.name.nl();
                        
                        if ("Merge!".Click().nl(ref changed)) {

                            foreach (var p in selectedPainters)
                                editedMesh.MergeWith(p);

                            editedMesh.Dirty = true;

                        }
                    }
                }
                else
                    if ("Remove from Copy Selection".Click().nl(ref changed))
                        selectedPainters.Remove(target);
                
            }
            
            pegi.nl();

            Grid.vertexPointMaterial.SetColor("_Color", MeshTool.VertColor);

            if (!Application.isPlaying && "references".foldout(ref showReferences).nl())  {

                "vertexPointMaterial".write_obj(Grid.vertexPointMaterial);
                pegi.newLine();

                "vertexPrefab".edit(ref Grid.vertPrefab).nl();
                "Max Vert Markers ".edit(ref vertsShowMax).nl();
                "pointedVertex".edit(ref Grid.pointedVertex.go).nl();
                "SelectedVertex".edit(ref Grid.selectedVertex.go).nl();
            }

            EditableMesh.inspected = null;
            
            if (changed)
                MeshTool.SetShaderKeywords();

            return changed;
        }

        public bool Undo_redo_PEGI()
        {
            bool changed = false;

            if (undoMoves.Count > 1) {
                if (pegi.Click(icon.Undo.GetIcon(), 25).changes(ref changed)) {
                    redoMoves.Add(undoMoves.RemoveLast());
                    undoMoves.Last().DecodeInto(out editedMesh);
                    Redraw();
                }
            }
            else
                pegi.Click(icon.UndoDisabled.GetIcon(), "Nothing to Undo (set number of undo frames in config)", 25);

            if (redoMoves.Count > 0) {
                if (pegi.Click(icon.Redo.GetIcon(),  25).changes(ref changed)) {
                    redoMoves.Last().DecodeInto(out editedMesh);
                    undoMoves.Add(redoMoves.RemoveLast());
                    Redraw();
                }
            }
            else
                pegi.Click(icon.RedoDisabled.GetIcon(), "Nothing to Redo", 25);

            pegi.newLine();

            return changed;
        }


        #endif

        #endregion

        #region Editor Gizmos

        void OutlineTriangle(Triangle t, Color colA, Color colB)
        {
            //bool vrt = tool == VertexPositionTool.inst;
            Line(t.vertexes[0], t.vertexes[1], t.DominantCourner[0] ? colA : colB, t.DominantCourner[1] ? colA : colB);
            Line(t.vertexes[1], t.vertexes[2], t.DominantCourner[1] ? colA : colB, t.DominantCourner[2] ? colA : colB);
            Line(t.vertexes[0], t.vertexes[2], t.DominantCourner[0] ? colA : colB, t.DominantCourner[2] ? colA : colB);
        }

        void Line(Vertex a, Vertex b, Color col, Color colb)
        {
            Line(a.meshPoint, b.meshPoint, col, colb);
        }

        void Line(MeshPoint a, MeshPoint b, Color col, Color colb)
        {

            Vector3 v3a = a.WorldPos;
            Vector3 v3b = b.WorldPos;
            Vector3 diff = (v3b - v3a) / 2;
            Line(v3a, v3a + diff, col);
            Line(v3b, v3b - diff, colb);
        }

        void Line(MeshPoint a, MeshPoint b, Color col)
        {

            Line(a.WorldPos, b.WorldPos, col);
        }

        public bool GizmoLines = false;
        void Line(Vector3 from, Vector3 to, Color col)
        {
            if (GizmoLines)
            {
                Gizmos.color = col;
                Gizmos.DrawLine(from, to);

            }
            else
                Debug.DrawLine(from, to, col);
        }

        public void DrowLinesAroundTargetPiece()
        {

            Vector3 piecePos = target.transform.TransformPoint(-Vector3.one / 2);//PositionScripts.PosUpdate(_target.getpos(), false);


            Vector3 projected = GridNavigator.Inst().ProjectToGrid(piecePos); // piecePos * getGridMaskVector() + ptdPos.ToV3(false)*getGridPerpendicularVector();
            Vector3 GridMask = GridNavigator.Inst().GetGridMaskVector() * 128 + projected;



            Debug.DrawLine(new Vector3(projected.x, projected.y, projected.z), new Vector3(GridMask.x, projected.y, projected.z), Color.red);
            Debug.DrawLine(new Vector3(projected.x, projected.y, projected.z), new Vector3(projected.x, GridMask.y, projected.z), Color.red);
            Debug.DrawLine(new Vector3(projected.x, projected.y, projected.z), new Vector3(projected.x, projected.y, GridMask.z), Color.red);

            Debug.DrawLine(new Vector3(projected.x, GridMask.y, GridMask.z), new Vector3(GridMask.x, GridMask.y, GridMask.z), Color.red);
            Debug.DrawLine(new Vector3(GridMask.x, projected.y, GridMask.z), new Vector3(GridMask.x, GridMask.y, GridMask.z), Color.red);
            Debug.DrawLine(new Vector3(GridMask.x, GridMask.y, projected.z), new Vector3(GridMask.x, GridMask.y, GridMask.z), Color.red);

            DrawTransformedCubeDebug(target.transform, Color.blue);


        }
        
        public void DRAW_Lines(bool isGizmoCall)
        {

            GizmoLines = isGizmoCall;

            if (!target) return;

            //Gizmos.DrawSphere (_target.transform.InverseTransformPoint(collisionPosLocal), _Mesh.distanceLimit*_target.transform.lossyScale.x);

            if (MeshTool.ShowTriangles)
            {
                if ((PointedTris != null) && ((PointedTris != SelectedTris) || (!MeshTool.ShowSelectedTriangle)))
                    OutlineTriangle(PointedTris, Color.cyan, Color.gray);

                if ((SelectedTris != null) && (MeshTool.ShowSelectedTriangle))
                    OutlineTriangle(SelectedTris, Color.blue, Color.white);
            }

            if (MeshTool.ShowLines)
            {
                if (PointedLine != null)
                    Line(PointedLine.pnts[0].meshPoint, PointedLine.pnts[1].meshPoint, Color.green);

                for (int i = 0; i < Mathf.Min(vertsShowMax, editedMesh.meshPoints.Count); i++)
                {
                    MeshPoint vp = editedMesh.meshPoints[i];
                    if (SameTrisAsPointed(vp))
                        Line(vp, PointedUV.meshPoint, Color.yellow);
                }
            }

            if (MeshTool.ShowVertices)
            {

                if (PointedUV != null)
                {
                    for (int i = 0; i < editedMesh.triangles.Count; i++)
                    {
                        Triangle td = editedMesh.triangles[i];
                        if (td.Includes(PointedUV))
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
        
        public static void DrawCubeDebug(Color col, Vector3 piecePos, Vector3 dest)
        {
            Debug.DrawLine(new Vector3(piecePos.x, piecePos.y, piecePos.z), new Vector3(dest.x, piecePos.y, piecePos.z), col);
            Debug.DrawLine(new Vector3(piecePos.x, piecePos.y, piecePos.z), new Vector3(piecePos.x, piecePos.y, dest.z), col);
            Debug.DrawLine(new Vector3(dest.x, piecePos.y, dest.z), new Vector3(piecePos.x, piecePos.y, dest.z), col);
            Debug.DrawLine(new Vector3(dest.x, piecePos.y, dest.z), new Vector3(dest.x, piecePos.y, piecePos.z), col);

            Debug.DrawLine(new Vector3(dest.x, piecePos.y, piecePos.z), new Vector3(dest.x, dest.y, piecePos.z), col);
            Debug.DrawLine(new Vector3(piecePos.x, piecePos.y, dest.z), new Vector3(piecePos.x, dest.y, dest.z), col);
            Debug.DrawLine(new Vector3(piecePos.x, piecePos.y, piecePos.z), new Vector3(piecePos.x, dest.y, piecePos.z), col);
            Debug.DrawLine(new Vector3(dest.x, piecePos.y, dest.z), new Vector3(dest.x, dest.y, dest.z), col);

            piecePos.y = dest.y;

            Debug.DrawLine(new Vector3(piecePos.x, piecePos.y, piecePos.z), new Vector3(dest.x, piecePos.y, piecePos.z), col);
            Debug.DrawLine(new Vector3(piecePos.x, piecePos.y, piecePos.z), new Vector3(piecePos.x, piecePos.y, dest.z), col);
            Debug.DrawLine(new Vector3(dest.x, piecePos.y, dest.z), new Vector3(piecePos.x, piecePos.y, dest.z), col);
            Debug.DrawLine(new Vector3(dest.x, piecePos.y, dest.z), new Vector3(dest.x, piecePos.y, piecePos.z), col);

        }

        public static void DrawTransformedLine(Transform tf, Vector3 from, Vector3 to, Color col)
        {
            from = tf.TransformPoint(from);
            to = tf.TransformPoint(to);
            Debug.DrawLine(from, to, col);
        }

        public static void DrawTransformedCubeDebug(Transform tf, Color col)
        {
            Vector3 dlb = new Vector3(-0.5f, -0.5f, -0.5f);
            Vector3 dlf = new Vector3(-0.5f, -0.5f, 0.5f);
            Vector3 drb = new Vector3(-0.5f, 0.5f, -0.5f);
            Vector3 drf = new Vector3(-0.5f, 0.5f, 0.5f);

            Vector3 ulb = new Vector3(0.5f, -0.5f, -0.5f);
            Vector3 ulf = new Vector3(0.5f, -0.5f, 0.5f);
            Vector3 urb = new Vector3(0.5f, 0.5f, -0.5f);
            Vector3 urf = new Vector3(0.5f, 0.5f, 0.5f);

            DrawTransformedLine(tf, dlb, ulb, col);
            DrawTransformedLine(tf, dlf, ulf, col);
            DrawTransformedLine(tf, drb, urb, col);
            DrawTransformedLine(tf, drf, urf, col);

            DrawTransformedLine(tf, dlb, dlf, col);
            DrawTransformedLine(tf, dlf, drf, col);
            DrawTransformedLine(tf, drf, drb, col);
            DrawTransformedLine(tf, drb, dlb, col);

            DrawTransformedLine(tf, ulb, ulf, col);
            DrawTransformedLine(tf, ulf, urf, col);
            DrawTransformedLine(tf, urf, urb, col);
            DrawTransformedLine(tf, urb, ulb, col);

        }

        public static void DrawTransformedCubeGizmo(Transform tf, Color col)
        {

            Vector3 dlb = tf.TransformPoint(new Vector3(-0.5f, -0.5f, -0.5f));
            Vector3 dlf = tf.TransformPoint(new Vector3(-0.5f, -0.5f, 0.5f));
            Vector3 drb = tf.TransformPoint(new Vector3(-0.5f, 0.5f, -0.5f));
            Vector3 drf = tf.TransformPoint(new Vector3(-0.5f, 0.5f, 0.5f));

            Vector3 ulb = tf.TransformPoint(new Vector3(0.5f, -0.5f, -0.5f));
            Vector3 ulf = tf.TransformPoint(new Vector3(0.5f, -0.5f, 0.5f));
            Vector3 urb = tf.TransformPoint(new Vector3(0.5f, 0.5f, -0.5f));
            Vector3 urf = tf.TransformPoint(new Vector3(0.5f, 0.5f, 0.5f));

            Gizmos.color = col;

            Gizmos.DrawLine(dlb, ulb);
            Gizmos.DrawLine(dlf, ulf);
            Gizmos.DrawLine(drb, urb);
            Gizmos.DrawLine(drf, urf);

            Gizmos.DrawLine(dlb, dlf);
            Gizmos.DrawLine(dlf, drf);
            Gizmos.DrawLine(drf, drb);
            Gizmos.DrawLine(drb, dlb);

            Gizmos.DrawLine(ulb, ulf);
            Gizmos.DrawLine(ulf, urf);
            Gizmos.DrawLine(urf, urb);
            Gizmos.DrawLine(urb, ulb);

        }
        #endregion

    }


    public class MeshSHaderMode {

        private static List<MeshSHaderMode> _allModes = new List<MeshSHaderMode>();

        public static List<MeshSHaderMode> AllModes => _allModes;

        private MeshSHaderMode(string value) { _value = value; _allModes.Add(this); }

        public static MeshSHaderMode lit = new          MeshSHaderMode(PainterDataAndConfig.MESH_PREVIEW_LIT);
        public static MeshSHaderMode normVector = new   MeshSHaderMode(PainterDataAndConfig.MESH_PREVIEW_NORMAL);
        public static MeshSHaderMode vertColor = new    MeshSHaderMode(PainterDataAndConfig.MESH_PREVIEW_VERTCOLOR);
        public static MeshSHaderMode projection = new   MeshSHaderMode(PainterDataAndConfig.MESH_PREVIEW_PROJECTION);

        public static MeshSHaderMode selected;

        public string _value;

        public override string ToString() => _value;

        public static void ApplySelected() {
            if (selected == null)
                selected = _allModes[0];

            foreach (MeshSHaderMode s in _allModes)
                UnityHelperFunctions.SetShaderKeyword(s._value, selected == s);

        }
    }
}