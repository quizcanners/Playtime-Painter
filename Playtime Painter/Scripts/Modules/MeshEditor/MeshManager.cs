using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PlaytimePainter
{

    using Vertex = PainterMesh.Vertex;
    using Triangle = PainterMesh.Triangle;
    using MeshPoint = PainterMesh.MeshPoint;
    using LineData = PainterMesh.LineData;

    #pragma warning disable IDE0034 // Simplify 'default' expression
    #pragma warning disable IDE0019 // Use pattern matching
    #pragma warning disable IDE0018 // Inline variable declaration


    public class MeshManager : PainterSystemKeepUnrecognizedCfg {

        #region Getters Setters
        public static MeshManager Inst => PainterCamera.MeshManager;

        public static Transform Transform => PainterCamera.Inst?.transform;

        private static Transform CameraTransform => Transform.gameObject.TryGetCameraTransform();

        public MeshToolBase MeshTool => PainterCamera.Data.MeshTool;

        public Vertex SelectedUv { get { return editedMesh.selectedUv; } set { editedMesh.selectedUv = value; } }
        public LineData SelectedLine { get { return editedMesh.selectedLine; } set { editedMesh.selectedLine = value; } }
        public Triangle SelectedTriangle { get { return editedMesh.selectedTriangle; } set { editedMesh.selectedTriangle = value; } }
        public Vertex PointedUv { get { return editedMesh.pointedUv; } set { editedMesh.pointedUv = value; } }
        public LineData PointedLine { get { return editedMesh.pointedLine; } set { editedMesh.pointedLine = value; } }
        public Triangle PointedTriangle { get { return editedMesh.pointedTriangle; } set { editedMesh.pointedTriangle = value; } }
        private static Vertex[] TriangleSet { get { return editedMesh.triangleSet; } set { editedMesh.triangleSet = value; } }
        public int TriVertices { get { return editedMesh.triVertices; } set { editedMesh.triVertices = value; } }
        public int EditedUV
        {
            get { return _editedUv; }
            set { _editedUv = value; QcUnity.SetShaderKeyword(PainterDataAndConfig._MESH_PREVIEW_UV2, _editedUv == 1); }
        }

        #endregion
        
        public static PlaytimePainter target;
        public static Transform targetTransform;
        private static int _editedUv;

        private static readonly List<string> UndoMoves = new List<string>();

        private static readonly List<string> RedoMoves = new List<string>();

        public static EditableMesh editedMesh = new EditableMesh();
        public static EditableMesh previewEdMesh;

       // public Mesh previewMesh;
        private int _currentUv;
        private bool _selectingUVbyNumber;
        public int verticesShowMax = 8;
        public Vector3 onGridLocal;
        public Vector3 collisionPosLocal;

        #region Encode & Decode

        public override CfgEncoder Encode() => this.EncodeUnrecognized()
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
        
        public void UpdateLocalSpaceMousePosition()
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
            editedMesh = new EditableMesh(painter);

            if (editCopy)
                painter.SharedMesh = new Mesh();

            Redraw();
            
            InitVerticesIfNull();

            UndoMoves.Clear();
            RedoMoves.Clear();

            UndoMoves.Add(editedMesh.Encode().ToString());

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
            UndoMoves.Clear();
            RedoMoves.Clear();
        }

        public void Redraw() {

            previewEdMesh = null;

            if (target) {

                editedMesh.Dirty = false;

                var mc = new MeshConstructor(editedMesh, target.MeshProfile, target.SharedMesh);

                if (!editedMesh.dirtyVertexIndexes && EditedMesh.Dirty) {

                    if (EditedMesh.dirtyPosition)
                        mc.UpdateMesh<VertexDataTypes.VertexPos>();

                    if (editedMesh.dirtyColor)
                        mc.UpdateMesh<VertexDataTypes.VertexColor>();

                    if (editedMesh.dirtyUvs)
                        mc.UpdateMesh<VertexDataTypes.VertexUv>();

                }  else {
                    var m = mc.Construct();
                    target.SharedMesh = m;
                    target.UpdateMeshCollider(m); 
                }
            }
        }

        public static string GenerateMeshSavePath() => Path.Combine(Cfg.meshesFolderName, editedMesh.meshName + ".asset");

        #region Dragging
        [NonSerialized] private double _dragStartTime;
        public double DragDelay {
            get
            {
                return _dragStartTime - QcUnity.TimeSinceStartup();   
            }

            set {

                _dragStartTime = QcUnity.TimeSinceStartup() + value;

            }

        }

        [NonSerialized] private bool _dragging;

        public bool Dragging { get { return _dragging; } set { _dragging = value; if (value) DragDelay = 0.2f; } }
        #endregion

        #region Vertex Operations

        public static Vector2 RoundUVs(Vector2 source, float accuracy)
        {
            var uv = source * accuracy;
            uv.x = Mathf.Round(uv.x);
            uv.y = Mathf.Round(uv.y);
            uv /= accuracy;
            return uv;
        }

        public void DisconnectDragged()
        {
            Debug.Log("Disconnecting dragged");

            var temp = new MeshPoint(SelectedUv.meshPoint);

            editedMesh.meshPoints.Add(temp);

            SelectedUv.AssignToNewVertex(temp);

            editedMesh.Dirty = true;//_Mesh.GenerateMesh(_targetPiece);
        }

        public void MoveVertexToGrid(MeshPoint vp)
        {
            UpdateLocalSpaceMousePosition();

            var diff = onGridLocal - vp.localPos;

            diff.Scale(GridNavigator.Inst().GetGridPerpendicularVector());
            vp.localPos += diff;
        }

        public void AssignSelected(Vertex newPnt)
        {
            SelectedUv = newPnt;
            if (Input.GetKey(KeyCode.LeftAlt))
            {
                MoveVertexToGrid(SelectedUv.meshPoint);
                editedMesh.Dirty = true;
            }
            else
                if (!EditorInputManager.Control)
            {
                GridNavigator.onGridPos = SelectedUv.meshPoint.WorldPos;
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

            editedMesh.NullPointedSelected();

            return true;
        }

        public MeshPoint CreatePointAndFocus(Vector3 pos)
        {
            var hold = new MeshPoint(pos, true);
            
            new Vertex(hold);

            editedMesh.meshPoints.Add(hold);
            
            if (!EditorInputManager.Control)
                EditedMesh.AddToTrisSet(hold.vertices[0]);

            if (Cfg.pixelPerfectMeshEditing)
                hold.PixPerfect();

            GridNavigator.collisionPos = pos;

            UpdateLocalSpaceMousePosition();

            return hold;
        }

        #endregion

        #region Tool MGMT

        public const string VertexEditorUiElementTag = "VertexEd";

        public const string ToolComponentTag = "toolComponent";

        public static List<string> meshEditorIgnore = new List<string> { VertexEditorUiElementTag, ToolComponentTag };

        private bool ProcessLinesOnTriangle(Triangle t)
        {
            t.wasProcessed = true;
            const float precision = 0.05f;

            var acc = (targetTransform.InverseTransformPoint(CameraTransform.position) - collisionPosLocal).magnitude;

            acc *= precision;

            var v0 = t.vertexes[0];
            var v1 = t.vertexes[1];
            var v2 = t.vertexes[2];

            var v0p = v0.meshPoint.distanceToPointed;
            var v1p = v1.meshPoint.distanceToPointed;
            var v2p = v2.meshPoint.distanceToPointed;

            if (QcMath.IsPointOnLine(v0p, v1p, Vector3.Distance(v0.LocalPos, v1.LocalPos), acc))
            {
                ProcessPointOnALine(v0, v1, t);
                return true;
            }

            if (QcMath.IsPointOnLine(v1p, v2p, Vector3.Distance(v1.LocalPos, v2.LocalPos), acc))
            {
                ProcessPointOnALine(v1, v2, t);
                return true;
            }

            if (QcMath.IsPointOnLine(v2p, v0p, Vector3.Distance(v2.LocalPos, v0.LocalPos), acc))
            {
                ProcessPointOnALine(v2, v0, t);
                return true;
            }


            return false;
        }

        private void GetPointedTriangleOrLine()
        {

            editedMesh.TagTrianglesUnprocessed();

            UpdateLocalSpaceMousePosition();

            foreach (var t1 in editedMesh.meshPoints)
            foreach (var uv in t1.vertices)
            foreach (var t in uv.triangles)
                if (!t.wasProcessed)
                {
                    t.wasProcessed = true;

                    if (!t.PointOnTriangle()) continue;

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

        private bool RayCastVertexIsPointed()
        {
            PointedUv = null;
            if (editedMesh.meshPoints.Count <= 0) return false;
            var alt = EditorInputManager.Alt;

            if (alt)
                GridNavigator.collisionPos = GridNavigator.onGridPos;


            RaycastHit hit;
            var vertexIsPointed = false;

            if (Physics.Raycast(EditorInputManager.GetScreenMousePositionRay(TexMGMT.MainCamera), out hit))
            {

                vertexIsPointed = (hit.transform.tag == VertexEditorUiElementTag);

                if (!alt)
                {

                    if (vertexIsPointed)
                    {
                        GridNavigator.collisionPos = hit.transform.position;
                        UpdateLocalSpaceMousePosition();
                        editedMesh.SortAround(collisionPosLocal, true);

                    }
                    else
                    {
                        GridNavigator.collisionPos = hit.point;
                        UpdateLocalSpaceMousePosition();
                        editedMesh.SortAround(collisionPosLocal, true);
                        GetPointedTriangleOrLine();
                    }
                }
            }
            
            UpdateLocalSpaceMousePosition();
            return vertexIsPointed;
        }

        private void ProcessPointOnALine(Vertex a, Vertex b, Triangle t)
        {

            if (EditorInputManager.GetMouseButtonDown(1))
            {
                SelectedLine = new LineData(t, a, b);
                UpdateLocalSpaceMousePosition();
            }

            PointedLine = new LineData(t, new Vertex[] { a, b });

        }

        private void ProcessKeyInputs()
        {

            var t = MeshTool;
            if (_dragging)
                t.KeysEventDragging();

            if (t.ShowVertices && PointedUv != null)
                t.KeysEventPointedVertex();
            else if (t.ShowLines && PointedLine != null)
                t.KeysEventPointedLine();
            else if (t.ShowTriangles && PointedTriangle != null)
                t.KeysEventPointedTriangle();

            t.KeysEventPointedWhatever();
        }

        private void ProcessMouseActions()
        {

            PointedTriangle = null;
            PointedLine = null;

            var pointingUv = RayCastVertexIsPointed();

            if (_dragging)
                MeshTool.ManageDragging();

            if (!_dragging)
            {

                if (pointingUv && _currentUv <= editedMesh.meshPoints[0].vertices.Count)
                {

                    var pointedVx = editedMesh.meshPoints[0];

                    if (_currentUv == pointedVx.vertices.Count) _currentUv--;

                    if ((SelectedUv != null) && (SelectedUv.meshPoint == pointedVx) && (!_selectingUVbyNumber))
                        PointedUv = SelectedUv;
                    else
                        PointedUv = pointedVx.vertices[_currentUv];

                    if (EditorInputManager.GetMouseButtonDown(0))
                        AssignSelected(PointedUv);
                }

                var t = MeshTool;

                if (t.ShowVertices && PointedUv != null)
                {
                    if (t.MouseEventPointedVertex())
                        EditedMesh.SetLastPointed(PointedUv);
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

        private void SortAndUpdate()
        {

            if (!Grid)
                return;

            if (!Grid.vertices[0].go)
                InitVerticesIfNull();

            UpdateLocalSpaceMousePosition();

            editedMesh.SortAround(collisionPosLocal, false);

            const float scaling = 16;

            Grid.selectedVertex.go.SetActive(false);
            Grid.pointedVertex.go.SetActive(false);

            for (var i = 0; i < verticesShowMax; i++)
                Grid.vertices[i].go.SetActive(false);

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

                if (point == editedMesh.PointedVertex)
                {
                    mark = Grid.pointedVertex; tmpScale *= 2;
                }
                else if (GetSelectedVertex() == editedMesh.meshPoints[i])
                {
                    mark = Grid.selectedVertex;
                    tmpScale *= 1.5f;
                }

                var go = mark.go;
                var tf = go.transform;


                go.SetActive(true);
                tf.position = worldPos;
                tf.rotation = camTf.rotation;
                tf.localScale = new Vector3((editedMesh.IsInTriangleSet(point) ? 1.5f : 1) * tmpScale, tmpScale, tmpScale);

                var tmpRay = new Ray {origin = camTf.position};

                tmpRay.direction = tf.position - tmpRay.origin;

                if (Physics.Raycast(tmpRay, out hit, 1000) && (!meshEditorIgnore.Contains(hit.transform.tag)))
                    mark.go.SetActive(false);

                mark.textm.color = SameTriangleAsPointed(point) ? Color.white : Color.gray;


                MeshTool.AssignText(mark, point);
            }
        }
        #endregion

        #region Undates
        private float _delayUpdate;

        public void CombinedUpdate()
        {

            if (!target)
                return;

            if (!target.enabled)  {
                DisconnectMesh();
                return;
            }

            var no = EditorInputManager.GetNumberKeyDown();
            _selectingUVbyNumber = false;
            if (no != -1) { _currentUv = no - 1; _selectingUVbyNumber = true; } else _currentUv = 0;

            if (Application.isPlaying)
                UpdateInputPlaytime();
            
            Grid?.UpdatePositions();

            if (Application.isPlaying)
                SortAndUpdate();

            _delayUpdate -= Time.deltaTime;

            if (editedMesh.Dirty && _delayUpdate<0) {

                RedoMoves.Clear();

                if (Cfg.saveMeshUndos) {
                    UndoMoves.Add(editedMesh.Encode().ToString());

                    if (UndoMoves.Count > 10)
                        UndoMoves.RemoveAt(0);
                }

                Redraw();
               
                _delayUpdate = 0.25f;
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
               
                ProcessKeyInputs();

                switch (e.keyCode)
                {

                    case KeyCode.Delete: //Debug.Log("Use Backspace to delete vertices"); goto case KeyCode.Backspace;
                    case KeyCode.Backspace: e.Use(); break;
                }
            }
            
            if (e.isMouse || (e.type == EventType.ScrollWheel)) 
                ProcessMouseActions();
           /* else if (_dragging) {
                Debug.Log("Setting dirty manually");
                editedMesh.dirty_Position = true;
                _dragging = false;
            }*/
            
            SortAndUpdate();

            return;
        }
        #endif

        public void UpdateInputPlaytime()
        {
            if (pegi.MouseOverPlaytimePainterUI)
                return;
      
            ProcessMouseActions();
            ProcessKeyInputs();
        }
        #endregion
        
        public MeshPoint GetSelectedVertex()
        {
            if (SelectedUv != null) return SelectedUv.meshPoint;
            return null;
        }

        private bool SameTriangleAsPointed(MeshPoint uvi)
        {
            if (PointedUv == null) return false;
            foreach (Triangle t in editedMesh.triangles)
            {
                if (t.Includes(uvi) && t.Includes(PointedUv)) return true;
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

        
            TriVertices = 0;
            EditedUV = EditedUV;

        }

        private int _justLoaded;

        #region Mesh Merging

        private static readonly List<PlaytimePainter> SelectedForMergePainters = new List<PlaytimePainter>();

        private static void MergeSelected() {

            var mats = target.Materials.ToList();

            foreach (var p in SelectedForMergePainters)
                if (target != p) {

                    var nms = p.Materials;

                    var em = new EditableMesh(p);
                    
                    for (var i = 0; i < nms.Length; i++) {
                        var nm = nms[i];
                        if (!nm || !mats.Contains(nm)) {
                           
                            em.ChangeSubMeshIndex(i, mats.Count);
                            mats.Add(nm);
                        }
                        else
                        {
                            var ind = mats.IndexOf(nm);
                            em.ChangeSubMeshIndex(i, ind);
                        }
                    }

                    em.AfterRemappingTriangleSubMeshIndexes();

                    editedMesh.MergeWith(em, p);
                    p.gameObject.SetActive(false);
                }

            editedMesh.Dirty = true;

            editedMesh.subMeshCount = mats.Count;

            SelectedForMergePainters.Clear();

            target.Materials = mats.ToArray();
        }

        private static void MergeSubMeshes()
        {
            var mats = target.Materials;

            for (var i = 0; i < mats.Length; i++) {

                var m = mats[i];

                if (!m) continue;

                var md = m.GetMaterialPainterMeta();

                if (!md.colorToVertexColorOnMerge) continue;

                var col = m.color;

                foreach (var t in editedMesh.triangles)
                    if (t.subMeshIndex == i)
                        t.SetColor(col);
            }

            editedMesh.SubMeshIndex = 0;

            target.Materials = mats.Resize(1);
        }

        #endregion

        #region Inspector

        private int _inspectedMeshItems = -1;

        public override bool Inspect()  {

            var changed = false;
            EditableMesh.inspected = editedMesh;

            pegi.newLine();
            
            target.PreviewShaderToggleInspect().changes(ref changed);

            if (!target.NotUsingPreview && "preview".select(45, ref MeshShaderMode.selected, MeshShaderMode.AllModes).changes(ref changed))
                MeshShaderMode.ApplySelected();

            var previousTool = MeshTool;
            
            if ("tool:".select_Index(35, ref Cfg.meshTool, MeshToolBase.AllTools).changes(ref changed)) {
                Grid.vertexPointMaterial.SetColor("_Color", MeshTool.VertexColor);
                previousTool.OnDeSelectTool();
                MeshTool.OnSelectTool();
            }

            if (DocsEnabled && pegi.DocumentationClick("About {0} tool".F(MeshTool.NameForDisplayPEGI())))
                pegi.FullWindwDocumentationOpen(MeshTool.Tooltip + (MeshTool.ShowGrid ? GridNavigator.ToolTip : ""));
            

    
            if (target.skinnedMeshRenderer) 
                ("When using Skinned Mesh Renderer, the mesh will be transformed by it, so mesh points will not be in the correct position, and it is impossible to do any modifications on mesh with the mouse. It is still possible to do automatic processes like " +
                 "changing mesh profile and everything that doesn't require direct input from mouse over the object. It is recommended to edit the object separately from the skinned mesh."
                    ).fullWindowWarningDocumentationClickOpen("Skinned mesh detected");
            
            pegi.nl();



            var mt = MeshTool;

            mt.Inspect().nl(ref changed);

            foreach (var p in PainterSystemManagerModuleBase.MeshToolPlugins)
                p.MeshToolInspection(mt).nl(ref changed);
            
            pegi.nl();

            Grid.vertexPointMaterial.SetColor("_Color", MeshTool.VertexColor);
            

            EditableMesh.inspected = null;
            
            if (changed)
                MeshTool.SetShaderKeywords();

            return changed;
        }
        
        private Vector3 _offset;
        public bool MeshOptionsInspect()
        {
            var changed = false;

            if (editedMesh != null && "Mesh ".enter(ref PlaytimePainter._inspectedMeshEditorItems, 2).nl()) {
                
                if (_inspectedMeshItems == -1) {
                    
                    #if UNITY_EDITOR
                    "Mesh Name:".edit(70, ref editedMesh.meshName).changes(ref changed);

                    var mesh = target.GetMesh();

                    var exists = !AssetDatabase.GetAssetPath(mesh).IsNullOrEmpty();

                    if ((exists ? icon.Save : icon.SaveAsNew)
                        .Click("Save Mesh As {0}".F(GenerateMeshSavePath()), 25).nl())
                        target.SaveMesh();
                        #endif
                    
                    "Save Undo".toggleIcon(ref Cfg.saveMeshUndos).changes(ref changed);
                    if (Cfg.saveMeshUndos)
                        icon.Warning.write("Can affect peformance");
                    pegi.nl();
                }

                editedMesh.Inspect().nl(ref changed);

                if ("Center".enter(ref _inspectedMeshItems, 2).nl())
                {
                    "center".edit(ref _offset).nl();
                    if ("Modify".Click().nl())
                    {
                        foreach (var v in EditedMesh.meshPoints)
                            v.localPos += _offset;

                        _offset = -_offset;

                        editedMesh.Dirty = true;

                    }

                    if ("Auto Center".Click().nl())
                    {
                        var avr = Vector3.zero;
                        foreach (var v in EditedMesh.meshPoints)
                            avr += v.localPos;

                        _offset = -avr / EditedMesh.meshPoints.Count;
                    }

                }

                /*  if ("Mirror by Center".Click()) {
                      GridNavigator.onGridPos = mgm.target.transform.position;
                      mgm.UpdateLocalSpaceV3s();
                      mgm.editedMesh.MirrorVerticlesAgainsThePlane(mgm.onGridLocal);
                  }

                  if (pegi.Click("Mirror by Plane")) {
                      mgm.UpdateLocalSpaceV3s();
                      mgm.editedMesh.MirrorVerticlesAgainsThePlane(mgm.onGridLocal);
                  }
                  pegi.newLine();

                  pegi.edit(ref displace);
                  pegi.newLine();

                  if (pegi.Click("Cancel")) displace = Vector3.zero;

                  if (pegi.Click("Apply")) {
                      mgm.edMesh.Displace(displace);
                      mgm.edMesh.dirty = true;
                      displace = Vector3.zero;
                  }
                  */

                if ("Combining meshes".enter(ref _inspectedMeshItems, 3).nl())
                {

                    if (!SelectedForMergePainters.Contains(target))
                    {
                        if ("Add To Group".Click("Add Mesh to the list of meshes to be merged").nl(ref changed))
                            SelectedForMergePainters.Add(target);

                        if (!SelectedForMergePainters.IsNullOrEmpty())
                        {

                            if (editedMesh.uv2DistributeRow < 2 && "Enable EV2 Distribution".toggleInt("Each mesh's UV2 will be modified to use a unique portion of a texture.", ref editedMesh.uv2DistributeRow).nl(ref changed))
                                editedMesh.uv2DistributeRow = Mathf.Max(2, (int)Mathf.Sqrt(SelectedForMergePainters.Count));
                            else
                            {
                                if (editedMesh.uv2DistributeCurrent > 0)
                                {
                                    ("All added meshes will be distributed in " + editedMesh.uv2DistributeRow + " by " + editedMesh.uv2DistributeRow + " grid. By cancelling this added" +
                                        "meshes will have UVs unchanged and may use the same portion of Texture (sampled with UV2) as other meshes.").writeHint();
                                    if ("Cancel Distribution".Click().nl())
                                        editedMesh.uv2DistributeRow = 0;
                                }
                                else
                                {
                                    "Row:".edit("Will change UV2 so that every mesh will have it's own portion of a texture.", 25, ref editedMesh.uv2DistributeRow, 2, 16).nl(ref changed);
                                    "Start from".edit(ref editedMesh.uv2DistributeCurrent).nl(ref changed);
                                }

                                "Using {0} out of {1} spots".F(editedMesh.uv2DistributeCurrent + SelectedForMergePainters.Count + 1, editedMesh.uv2DistributeRow * editedMesh.uv2DistributeRow).nl();

                            }
                        }
                    }
                    else
                    {
                        if (SelectedForMergePainters.Count > 1 && "Merge!".Click().nl(ref changed)) 
                                MergeSelected();
                            
                        if ("Remove from Merge Group".Click().nl(ref changed))
                                SelectedForMergePainters.Remove(target);

                    }

                    if (SelectedForMergePainters.Count > 1) {
                        "Current merging group:".nl();
                        for (var i = 0; i < SelectedForMergePainters.Count; i++)
                            if (!SelectedForMergePainters[i] || icon.Delete.Click(25))
                            {
                                SelectedForMergePainters.RemoveAt(i);
                                i--;
                            }  else  {
                                SelectedForMergePainters[i].gameObject.name.nl();
                            }
                    }

                }

                var mats = target.Materials;
                if ("Combining Sub Meshes".conditional_enter(mats.Length > 1, ref _inspectedMeshItems, 4).nl()) {

                    "Select which materials should transfer color into vertex color".writeHint();

                    var subMeshCount = target.GetMesh().subMeshCount;

                    for (var i=0; i < Mathf.Max(subMeshCount, mats.Length); i++) {
                        var m = mats.TryGet(i);

                        if (!m)
                            "Null".nl();
                        else {
                            var md = m.GetMaterialPainterMeta();
                            
                            "{0} color to vertex color".F(m.name).toggleIcon(ref md.colorToVertexColorOnMerge, true);

                            if (md.colorToVertexColorOnMerge) {
                                var col = m.color;
                                if (pegi.edit(ref col))
                                    m.color = col;
                            }
                            

                            if (i>=subMeshCount)
                                icon.Warning.write("There are more materials then sub meshes on the mesh");
                        }

                        pegi.nl();
                    }

                    if ("Merge All Submeshes".Click()) 
                        MergeSubMeshes();

                    
                }

                pegi.nl();

                if (!Application.isPlaying && "Debug".foldout(ref _inspectedMeshItems, 10).nl())
                {
                    "vertexPointMaterial".write(Grid.vertexPointMaterial);
                    pegi.nl();
                    "vertexPrefab".edit(ref Grid.vertPrefab).nl();
                    "Max Vertex Markers ".edit(ref verticesShowMax).nl();
                    "pointedVertex".edit(ref Grid.pointedVertex.go).nl();
                    "SelectedVertex".edit(ref Grid.selectedVertex.go).nl();
                }
            }


            return changed;
        }

        public bool UndoRedoInspect()
        {
            bool changed = false;

            if (UndoMoves.Count > 1) {
                if (icon.Undo.Click(25).changes(ref changed)) {
                    RedoMoves.Add(UndoMoves.RemoveLast());
                    UndoMoves.Last().DecodeInto(out editedMesh);
                    Redraw();
                }
            }
            else
                icon.UndoDisabled.Click("Nothing to Undo (set number of undo frames in config)", 25);

            if (RedoMoves.Count > 0) {
                if (icon.Redo.Click(25).changes(ref changed)) {
                    RedoMoves.Last().DecodeInto(out editedMesh);
                    UndoMoves.Add(RedoMoves.RemoveLast());
                    Redraw();
                }
            }
            else
                icon.RedoDisabled.Click("Nothing to Redo", 25);

            pegi.nl();

            return changed;
        }
        
        #endregion

        #region Editor Gizmos

        private void OutlineTriangle(Triangle t, Color colA, Color colB)
        {
            //bool vrt = tool == VertexPositionTool.inst;
            Line(t.vertexes[0], t.vertexes[1], t.dominantCorner[0] ? colA : colB, t.dominantCorner[1] ? colA : colB);
            Line(t.vertexes[1], t.vertexes[2], t.dominantCorner[1] ? colA : colB, t.dominantCorner[2] ? colA : colB);
            Line(t.vertexes[0], t.vertexes[2], t.dominantCorner[0] ? colA : colB, t.dominantCorner[2] ? colA : colB);
        }

        private void Line(Vertex a, Vertex b, Color colA, Color colB)
        {
            Line(a.meshPoint, b.meshPoint, colA, colB);
        }

        private void Line(MeshPoint a, MeshPoint b, Color colA, Color colB)
        {

            Vector3 v3a = a.WorldPos;
            Vector3 v3b = b.WorldPos;
            Vector3 diff = (v3b - v3a) / 2;
            Line(v3a, v3a + diff, colA);
            Line(v3b, v3b - diff, colB);
        }

        private void Line(MeshPoint a, MeshPoint b, Color colA) => Line(a.WorldPos, b.WorldPos, colA);
        
        public bool GizmoLines;

        private void Line(Vector3 from, Vector3 to, Color colA)
        {
            if (GizmoLines)
            {
                Gizmos.color = colA;
                Gizmos.DrawLine(from, to);

            }
            else
                Debug.DrawLine(from, to, colA);
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
                    Line(PointedLine.points[0].meshPoint, PointedLine.points[1].meshPoint, Color.green);

                var dv = EditedMesh._draggedVertices;

                if (dv.Count > 0) {

                    for (int i = 0; i < dv.Count; i++)
                    {
                        var a = dv[i];
                        for (int j = 0; j < dv.Count; j++)
                        {
                            Line(a, dv[j], Color.cyan); //var b = dv[j];

                        }
                    }

                }


                for (int i = 0; i < Mathf.Min(verticesShowMax, editedMesh.meshPoints.Count); i++)
                {
                    MeshPoint vp = editedMesh.meshPoints[i];
                    if (SameTriangleAsPointed(vp))
                        Line(vp, PointedUv.meshPoint, Color.yellow);
                }
            }

            if (MeshTool.ShowVertices)
            {

                if (PointedUv != null)
                {
                    for (int i = 0; i < editedMesh.triangles.Count; i++)
                    {
                        Triangle td = editedMesh.triangles[i];
                        if (td.Includes(PointedUv))
                        {

                            Line(td.vertexes[1].meshPoint, td.vertexes[0].meshPoint, Color.yellow);
                            Line(td.vertexes[1].meshPoint, td.vertexes[2].meshPoint, Color.yellow);
                            Line(td.vertexes[2].meshPoint, td.vertexes[0].meshPoint, Color.yellow);
                        }
                    }
                 
                }
                
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
        
        #endregion

    }
    
    public class MeshShaderMode {

        private static readonly List<MeshShaderMode> _allModes = new List<MeshShaderMode>();

        public static List<MeshShaderMode> AllModes => _allModes;

        private MeshShaderMode(string value) { this.value = value; _allModes.Add(this); }

        public static MeshShaderMode lit = new          MeshShaderMode(PainterDataAndConfig.MESH_PREVIEW_LIT);
        public static MeshShaderMode normVector = new   MeshShaderMode(PainterDataAndConfig.MESH_PREVIEW_NORMAL);
        public static MeshShaderMode vertexColor = new  MeshShaderMode(PainterDataAndConfig.MESH_PREVIEW_VERTCOLOR);
        public static MeshShaderMode projection = new   MeshShaderMode(PainterDataAndConfig.MESH_PREVIEW_PROJECTION);

        public static MeshShaderMode selected;

        public string value;

        public override string ToString() => value;

        public static void ApplySelected() {
            if (selected == null)
                selected = _allModes[0];

            foreach (MeshShaderMode s in _allModes)
                QcUnity.SetShaderKeyword(s.value, selected == s);

        }
    }
}