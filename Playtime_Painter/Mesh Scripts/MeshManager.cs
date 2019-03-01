using UnityEngine;
using System.Collections.Generic;
using System;
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

        private static Transform CameraTransform => Transform.gameObject.TryGetCameraTransform();

        public MeshToolBase MeshTool => PainterCamera.Data.MeshTool;

        private static int _editedUv;

        public int EditedUV {
            get { return _editedUv; }
            set { _editedUv = value;  UnityHelperFunctions.SetShaderKeyword(PainterDataAndConfig._MESH_PREVIEW_UV2, _editedUv == 1);  }

        }
        public static Vector3 editorMousePos;

        public PlaytimePainter target;
        public Transform targetTransform;
        public PlaytimePainter previouslyEdited;

        readonly List<string> _undoMoves = new List<string>();

        readonly List<string> _redoMoves = new List<string>();

        public EditableMesh editedMesh = new EditableMesh();

        public EditableMesh previewEdMesh = new EditableMesh();

        public Mesh previewMesh;

        public AddCubeCfg tmpCubeCfg = new AddCubeCfg();
        
        #region Encode & Decode

        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add_IfTrue("byUV", _selectingUVbyNumber);

        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "byUV": _selectingUVbyNumber = data.ToBool(); break;
                default: return false;
            }
            return true;
        }

        #endregion

        private int _currentUv;
        private bool _selectingUVbyNumber;

        public Vertex SelectedUV { get { return editedMesh.selectedUv; } set { editedMesh.selectedUv = value; } }
        public LineData SelectedLine { get { return editedMesh.selectedLine; } set { editedMesh.selectedLine = value; } }
        public Triangle SelectedTriangle { get { return editedMesh.selectedTriangle; } set { editedMesh.selectedTriangle = value; } }
        public Vertex PointedUV { get { return editedMesh.pointedUv; } set { editedMesh.pointedUv = value; } }
        public LineData PointedLine { get { return editedMesh.pointedLine; } set { editedMesh.pointedLine = value; } }
        public Triangle PointedTriangle { get { return editedMesh.pointedTriangle; } set { editedMesh.pointedTriangle = value; } }
        private Vertex[] TriangleSet { get { return editedMesh.triangleSet; } set { editedMesh.triangleSet = value; } }
        public int TriVertices { get { return editedMesh.triVertices; } set { editedMesh.triVertices = value; } }

        [NonSerialized]
        public int verticesShowMax = 8;

        [NonSerialized]
        public Vector3 onGridLocal;
        [NonSerialized]
        public Vector3 collisionPosLocal;

        public void UpdateLocalSpaceV3S()
        {
            if (!target) return;
            
            onGridLocal = targetTransform.InverseTransformPoint(GridNavigator.onGridPos);
            collisionPosLocal = targetTransform.InverseTransformPoint(GridNavigator.collisionPos);
        }

        public void EditMesh(PlaytimePainter painter, bool editCopy)
        {
            if (!painter || painter == target)
                return;

            if (target)
                DisconnectMesh();

            target = painter;
            targetTransform = painter.transform;
            editedMesh = new EditableMesh();

            editedMesh.Edit(painter);

            if (editCopy)
                painter.SharedMesh = new Mesh();

            Redraw();

            painter.meshNameField = editedMesh.meshName;

            InitVerticesIfNull();

            SelectedLine = null;
            SelectedTriangle = null;
            SelectedUV = null;

            _undoMoves.Clear();
            _redoMoves.Clear();

            _undoMoves.Add(editedMesh.Encode().ToString());

            MeshTool.OnSelectTool();

        }

        public void DisconnectMesh()
        {

            if (target) {
                MeshTool.OnDeSelectTool();
                target.SavedEditableMesh = editedMesh.Encode().ToString();
                target = null;
                targetTransform = null;
            }
            Grid.DeactivateVertices();
            GridNavigator.Inst().SetEnabled(false, false);
            _undoMoves.Clear();
            _redoMoves.Clear();
        }

#if UNITY_EDITOR
        public void SaveGeneratedMeshAsAsset()
        {
            AssetDatabase.CreateAsset(target.SharedMesh, "Assets/Models/" + target.gameObject.name + "_export.asset");
            AssetDatabase.SaveAssets();
        }
#endif

        public void Redraw() {

            if (target) {

                var mc = new MeshConstructor(editedMesh, target.MeshProfile, target.SharedMesh);

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

        [NonSerialized] private double _dragStartTime;
        public double DragDelay {
            get
            {
                return _dragStartTime - UnityHelperFunctions.TimeSinceStartup();   
            }

            set {

                _dragStartTime = UnityHelperFunctions.TimeSinceStartup() + value;

            }

        }

        [NonSerialized] private bool _dragging;

        public bool Dragging { get { return _dragging; } set { _dragging = value; if (value) DragDelay = 0.4f; } }

        #region Vertex Operations

        public static Vector2 RoundUVs(Vector2 source, float accuracy)
        {
            var uv = source * accuracy;
            uv.x = Mathf.Round(uv.x);
            uv.y = Mathf.Round(uv.y);
            uv /= accuracy;
            return uv;
        }

        public void AddToTrisSet(Vertex nuv)
        {

            TriangleSet[TriVertices] = nuv;
            TriVertices++;

            if (TriVertices == 3)
                foreach (var t in editedMesh.triangles)
                    if (t.IsSamePoints(TriangleSet))
                    {
                        t.Set(TriangleSet);
                        editedMesh.Dirty = true;
                        TriVertices = 0;
                        return;
                    }


            if (TriVertices < 3) return;

            var td = new Triangle(TriangleSet);

            editedMesh.triangles.Add(td);

            if (!EditorInputManager.Control)
            {
                MakeTriangleVertUnique(td, TriangleSet[0]);
                MakeTriangleVertUnique(td, TriangleSet[1]);
                MakeTriangleVertUnique(td, TriangleSet[2]);
            }

            TriVertices = 0;
            editedMesh.Dirty = true;
        }
        
        public void DisconnectDragged()
        {
            Debug.Log("Disconnecting dragged");

            var temp = new MeshPoint(SelectedUV.Pos);

            editedMesh.meshPoints.Add(temp);

            SelectedUV.AssignToNewVertex(temp);

            editedMesh.Dirty = true;//_Mesh.GenerateMesh(_targetPiece);
        }

        public void MoveVertexToGrid(MeshPoint vp)
        {
            UpdateLocalSpaceV3S();

            var diff = onGridLocal - vp.localPos;

            diff.Scale(GridNavigator.Inst().GetGridPerpendicularVector());
            vp.localPos += diff;
        }
        public void AssignSelected(Vertex newPnt)
        {
            SelectedUV = newPnt;
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

        public bool DeleteVertexHeal(MeshPoint vertex)
        {

            var trs = new Triangle[3];

            var cnt = 0;

            foreach (var t in editedMesh.triangles)
            {
                if (!t.Includes(vertex)) continue;
                if (cnt == 3) return false;
                trs[cnt] = t;
                cnt++;
            }

            if (cnt != 3) return false;

            trs[0].MergeAround(trs[1], vertex); 
            editedMesh.triangles.Remove(trs[1]);
            editedMesh.triangles.Remove(trs[2]);

            editedMesh.meshPoints.Remove(vertex);

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

            for (var i = 0; i < editedMesh.triangles.Count; i++)
            {
                if (!editedMesh.triangles[i].Includes(uv))
                    continue;
                
                editedMesh.triangles.RemoveAt(i);
                i--;
            }

            if (IsInTrisSet(uv))
                TriVertices = 0;


            vrt.vertices.Remove(uv);


            if (vrt.vertices.Count == 0)
                editedMesh.meshPoints.Remove(vrt);
            



            editedMesh.Dirty = true;
        }

        private void NullPointedSelected()
        {
            PointedUV = null;
            PointedLine = null;
            PointedTriangle = null;
            SelectedUV = null;
            SelectedLine = null;
            SelectedTriangle = null;
        }

        public bool IsInTrisSet(MeshPoint vertex)
        { 
            for (var i = 0; i < TriVertices; i++)
                if (TriangleSet[i].meshPoint == vertex) return true;
            return false;
        }

        private bool IsInTrisSet(Vertex uv)
        { 
            for (var i = 0; i < TriVertices; i++)
                if (TriangleSet[i] == uv) return true;
            return false;
        }

        public MeshPoint AddPoint(Vector3 pos)
        {
            var hold = new MeshPoint(pos);

            new Vertex(hold);

            editedMesh.meshPoints.Add(hold);
            
            if (!EditorInputManager.Control)
                AddToTrisSet(hold.vertices[0]);

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

        private bool ProcessLinesOnTriangle(Triangle t)
        {
            t.wasProcessed = true;
            const float precision = 0.05f;

            var acc = (targetTransform.InverseTransformPoint(CameraTransform.position) - collisionPosLocal).magnitude;

            acc *= precision;

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

            UpdateLocalSpaceV3S();

            for (int i = 0; i < editedMesh.meshPoints.Count; i++)
                foreach (Vertex uv in editedMesh.meshPoints[i].vertices)
                    foreach (Triangle t in uv.tris)
                        if (!t.wasProcessed)
                        {
                            //	Debug.Log ("Browsing");
                            t.wasProcessed = true;
                            if (t.PointOnTriangle())
                            {

                                if (EditorInputManager.GetMouseButtonDown(0))
                                {
                                    SelectedTriangle = t;
                                    AssignSelected(t.GetClosestTo(collisionPosLocal));
                                }

                                PointedTriangle = t;

                                if (MeshTool.ShowLines)
                                    ProcessLinesOnTriangle(PointedTriangle);

                                return;
                            }

                        }


        }

        bool Raycast_VertexIsPointed()
        {
            PointedUV = null;
            if (editedMesh.meshPoints.Count <= 0) return false;
            var alt = EditorInputManager.Alt;

            if (alt)
                GridNavigator.collisionPos = GridNavigator.onGridPos;


            RaycastHit hit;
            var vertexIsPointed = false;

            if (Physics.Raycast(EditorInputManager.GetScreenRay(TexMGMT.MainCamera), out hit))
            {

                vertexIsPointed = (hit.transform.tag == "VertexEd");

                if (!alt)
                {

                    if (vertexIsPointed)
                    {
                        GridNavigator.collisionPos = hit.transform.position;
                        UpdateLocalSpaceV3S();
                        editedMesh.SortAround(collisionPosLocal, true);

                    }
                    else
                    {
                        GridNavigator.collisionPos = hit.point;
                        UpdateLocalSpaceV3S();
                        editedMesh.SortAround(collisionPosLocal, true);
                        GetPointedTRIANGLESorLINE();
                    }
                }
            }



            UpdateLocalSpaceV3S();
            return vertexIsPointed;
        }

        private void ProcessPointOnALine(Vertex a, Vertex b, Triangle t)
        {

            if (EditorInputManager.GetMouseButtonDown(1))
            {
                SelectedLine = new LineData(t, a, b);
                UpdateLocalSpaceV3S();
            }

            PointedLine = new LineData(t, new Vertex[] { a, b });

        }

        private void PROCESS_KEYS()
        {

            var t = MeshTool;
            if (_dragging)
                t.KeysEventDragging();

            if (t.ShowVertices && PointedUV != null)
                t.KeysEventPointedVertex();
            else if (t.ShowLines && PointedLine != null)
                t.KeysEventPointedLine();
            else if (t.ShowTriangles && PointedTriangle != null)
                t.KeysEventPointedTriangle();

            t.KeysEventPointedWhatever();
        }

        private void RAYCAST_SELECT_MOUSEedit()
        {

            PointedTriangle = null;
            PointedLine = null;

            bool pointingUV = Raycast_VertexIsPointed();

            if (_dragging)
                MeshTool.ManageDragging();

            if (!_dragging)
            {

                if (pointingUV && _currentUv <= editedMesh.meshPoints[0].vertices.Count)
                {

                    var pointedVX = editedMesh.meshPoints[0];

                    if (_currentUv == pointedVX.vertices.Count) _currentUv--;

                    if ((SelectedUV != null) && (SelectedUV.meshPoint == pointedVX) && (!_selectingUVbyNumber))
                        PointedUV = SelectedUV;
                    else
                        PointedUV = pointedVX.vertices[_currentUv];

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
                else if (t.ShowTriangles && PointedTriangle != null)
                {
                    if (t.MouseEventPointedTriangle())
                        EditedMesh.SetLastPointed(PointedTriangle);
                    else EditedMesh.ClearLastPointed();
                }
                else
                {
                    t.MouseEventPointedNothing();
                    EditedMesh.ClearLastPointed();
                }



            }
        }

        private void SORT_AND_UPDATE_UI()
        {

            if (!Grid)
                return;

            if (!Grid.vertices[0].go)
                InitVerticesIfNull();

            UpdateLocalSpaceV3S();

            editedMesh.SortAround(collisionPosLocal, false);

            const float scaling = 16;

            Grid.selectedVertex.go.SetActiveTo(false);
            Grid.pointedVertex.go.SetActiveTo(false);

            for (var i = 0; i < verticesShowMax; i++)
                Grid.vertices[i].go.SetActiveTo(false);

            if (!MeshTool.ShowVertices) return;

            var camTf = CameraTransform;

            for (var i = 0; i < verticesShowMax; i++)
            {
                RaycastHit hit;

                if (editedMesh.meshPoints.Count <= i) continue;

                var mark = Grid.vertices[i];
                var point = editedMesh.meshPoints[i];

                var worldPos = point.WorldPos;
                var tmpScale = Vector3.Distance(worldPos, camTf.position) / scaling;

                if (GetPointedVertex() == point)
                {
                    mark = Grid.pointedVertex; tmpScale *= 2;
                }
                else if (GetSelectedVertex() == editedMesh.meshPoints[i])
                {
                    mark = Grid.selectedVertex;
                    tmpScale *= 1.5f;
                }

                mark.go.SetActiveTo(true);
                mark.go.transform.position = worldPos;
                mark.go.transform.rotation = camTf.rotation;
                mark.go.transform.localScale = new Vector3((IsInTrisSet(point) ? 1.5f : 1) * tmpScale, tmpScale, tmpScale);

                var tmpRay = new Ray();

                tmpRay.origin = camTf.position;
                tmpRay.direction = mark.go.transform.position - tmpRay.origin;

                if ((Physics.Raycast(tmpRay, out hit, 1000)) && (!meshEditorIgnore.Contains(hit.transform.tag)))
                    mark.go.SetActiveTo(false);

                mark.textm.color = SameTriangleAsPointed(point) ? Color.white : Color.gray;


                MeshTool.AssignText(mark, point);
            }
        }
        #endregion

        public static List<string> meshEditorIgnore = new List<string> { "VertexEd", "toolComponent" };

        private float delayUpdate;

        public void CombinedUpdate()
        {

            if (!target)
                return;

            if (!target.enabled)  {
                DisconnectMesh();
                return;
            }

            int no = EditorInputManager.GetNumberKeyDown();
            _selectingUVbyNumber = false;
            if (no != -1) { _currentUv = no - 1; _selectingUVbyNumber = true; } else _currentUv = 0;

            if (Application.isPlaying)
                UpdateInputPlaytime();
            
            Grid?.UpdatePositions();

            if (Application.isPlaying)
                SORT_AND_UPDATE_UI();

            delayUpdate -= Time.deltaTime;

            if (editedMesh.Dirty && delayUpdate<0) {

                _redoMoves.Clear();

                if (Cfg.saveMeshUndos) {
                    _undoMoves.Add(editedMesh.Encode().ToString());

                    if (_undoMoves.Count > 10)
                        _undoMoves.RemoveAt(0);
                }

                Redraw();
                previewMesh = null;
                delayUpdate = 0.25f;
            }

            if (_justLoaded >= 0)
                _justLoaded--;

        }

        #if UNITY_EDITOR
        public void UpdateInputEditorTime(Event e, bool up, bool dwn)
        {

            if (!target || _justLoaded > 0)
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

        public MeshPoint GetPointedVertex()
        {
            if (PointedUV != null) return PointedUV.meshPoint;
            return null;
        }
        public MeshPoint GetSelectedVertex()
        {
            if (SelectedUV != null) return SelectedUV.meshPoint;
            return null;
        }

        private bool SameTriangleAsPointed(MeshPoint uvi)
        {
            if (PointedUV == null) return false;
            foreach (Triangle t in editedMesh.triangles)
            {
                if (t.Includes(uvi) && t.Includes(PointedUV)) return true;
            }
            return false;
        }

        private void InitVerticesIfNull()
        {
            if (!Grid)
                return;

            if (!Grid.vertPrefab)
                Grid.vertPrefab = Resources.Load("prefabs/vertex") as GameObject;

            if ((Grid.vertices == null) || (Grid.vertices.Length == 0) || (!Grid.vertices[0].go))
            {
                Grid.vertices = new MarkerWithText[verticesShowMax];

                for (int i = 0; i < verticesShowMax; i++)
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
            InitVerticesIfNull();

            if (previouslyEdited && !target)
            {
                DisconnectMesh();
                EditMesh(previouslyEdited, false);
                _justLoaded = 5;
            }

            previouslyEdited = null;
            TriVertices = 0;
            EditedUV = EditedUV;

        }

        private int _justLoaded;

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

            if ("tool".select(70, ref Cfg.meshTool, MeshToolBase.AllTools).changes(ref changed)) {
                Grid.vertexPointMaterial.SetColor("_Color", MeshTool.VertexColor);
                previousTool.OnDeSelectTool();
                MeshTool.OnSelectTool();
            }

            MeshTool.Tooltip.fullWindowDocumentationClick("About this tool.");

            pegi.nl();

            pegi.space();
            pegi.newLine();

            "Mesh Name:".edit(70, ref target.meshNameField).changes(ref changed);

#if UNITY_EDITOR
            var mesh = target.GetMesh();

            var exists = !AssetDatabase.GetAssetPath(mesh).IsNullOrEmpty();
            
            if ((exists ? icon.Save : icon.SaveAsNew).Click("Save Mesh As {0}".F(target.GenerateMeshSavePath()), 25).nl())
                target.SaveMesh();
#endif
            
            pegi.nl();

            var mt = MeshTool;

            mt.Inspect().nl(ref changed);

            foreach (var p in PainterManagerPluginBase.VertexEdgePlugins)
                p.MeshToolInspection(mt).nl(ref changed);
            
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

            Grid.vertexPointMaterial.SetColor("_Color", MeshTool.VertexColor);

            if (!Application.isPlaying && "Advanced".foldout(ref showReferences).nl()) {

                "Save Undos".toggleIcon(ref Cfg.saveMeshUndos).nl(ref changed);

                "vertexPointMaterial".write_obj(Grid.vertexPointMaterial);
                pegi.newLine();

                "vertexPrefab".edit(ref Grid.vertPrefab).nl();
                "Max Vert Markers ".edit(ref verticesShowMax).nl();
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

            if (_undoMoves.Count > 1) {
                if (pegi.Click(icon.Undo.GetIcon(), 25).changes(ref changed)) {
                    _redoMoves.Add(_undoMoves.RemoveLast());
                    _undoMoves.Last().DecodeInto(out editedMesh);
                    Redraw();
                }
            }
            else
                pegi.Click(icon.UndoDisabled.GetIcon(), "Nothing to Undo (set number of undo frames in config)", 25);

            if (_redoMoves.Count > 0) {
                if (pegi.Click(icon.Redo.GetIcon(),  25).changes(ref changed)) {
                    _redoMoves.Last().DecodeInto(out editedMesh);
                    _undoMoves.Add(_redoMoves.RemoveLast());
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

            var piecePos = targetTransform.TransformPoint(-Vector3.one / 2);//PositionScripts.PosUpdate(_target.getpos(), false);


            var projected = GridNavigator.Inst().ProjectToGrid(piecePos); // piecePos * getGridMaskVector() + ptdPos.ToV3(false)*getGridPerpendicularVector();
            var gridMask = GridNavigator.Inst().GetGridMaskVector() * 128 + projected;
            
            Debug.DrawLine(new Vector3(projected.x, projected.y, projected.z), new Vector3(gridMask.x, projected.y, projected.z), Color.red);
            Debug.DrawLine(new Vector3(projected.x, projected.y, projected.z), new Vector3(projected.x, gridMask.y, projected.z), Color.red);
            Debug.DrawLine(new Vector3(projected.x, projected.y, projected.z), new Vector3(projected.x, projected.y, gridMask.z), Color.red);

            Debug.DrawLine(new Vector3(projected.x, gridMask.y, gridMask.z), new Vector3(gridMask.x, gridMask.y, gridMask.z), Color.red);
            Debug.DrawLine(new Vector3(gridMask.x, projected.y, gridMask.z), new Vector3(gridMask.x, gridMask.y, gridMask.z), Color.red);
            Debug.DrawLine(new Vector3(gridMask.x, gridMask.y, projected.z), new Vector3(gridMask.x, gridMask.y, gridMask.z), Color.red);

            DrawTransformedCubeDebug(targetTransform, Color.blue);


        }
        
        public void DRAW_Lines(bool isGizmoCall)
        {

            GizmoLines = isGizmoCall;

            if (!target) return;

            //Gizmos.DrawSphere (_target.transform.InverseTransformPoint(collisionPosLocal), _Mesh.distanceLimit*_target.transform.lossyScale.x);

            if (MeshTool.ShowTriangles)
            {
                if ((PointedTriangle != null) && ((PointedTriangle != SelectedTriangle) || (!MeshTool.ShowSelectedTriangle)))
                    OutlineTriangle(PointedTriangle, Color.cyan, Color.gray);

                if ((SelectedTriangle != null) && (MeshTool.ShowSelectedTriangle))
                    OutlineTriangle(SelectedTriangle, Color.blue, Color.white);
            }

            if (MeshTool.ShowLines)
            {
                if (PointedLine != null)
                    Line(PointedLine.pnts[0].meshPoint, PointedLine.pnts[1].meshPoint, Color.green);

                for (int i = 0; i < Mathf.Min(verticesShowMax, editedMesh.meshPoints.Count); i++)
                {
                    MeshPoint vp = editedMesh.meshPoints[i];
                    if (SameTriangleAsPointed(vp))
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
                    //Vector3 selPos = pointedUV.vertex.worldPos; //.pos.ToV3 (false);
                    //Gizmos.color = Color.green;
                    //Gizmos.DrawLine(selPos, GridNavigator.inst().ProjectToGrid(selPos));
                    //Line(selPos, GridNavigator.inst().ProjectToGrid(selPos), Color.green);
                }

                /*if (selectedUV != null)
                {
                    Vector3 selPos = selectedUV.vertex.getWorldPos();//.pos.ToV3 (false);
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