using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuizCanners.Inspect;
using PainterTool.CameraModules;
using QuizCanners.Utils;
using UnityEngine;
using QuizCanners.Migration;

namespace PainterTool.MeshEditing
{
    #pragma warning disable IDE0018 // Inline variable declaration

    internal class MeshEditorManager : PainterClassCfg , IPEGI
    {

        #region Getters Setters

        public static MeshToolBase MeshTool => Painter.Data.MeshTool;

        public static PainterMesh.Vertex SelectedUv 
        { 
            get => editedMesh.selectedUv;
            set => editedMesh.selectedUv = value;
        }

        private static PainterMesh.LineData SelectedLine {
            set => editedMesh.selectedLine = value;
        }
        public static PainterMesh.Triangle SelectedTriangle { get => editedMesh.selectedTriangle;
            private set => editedMesh.selectedTriangle = value;
        }
        public static PainterMesh.Vertex PointedUv { get => editedMesh.pointedUv;
            private set => editedMesh.pointedUv = value;
        }
        public static PainterMesh.LineData PointedLine { get => editedMesh.pointedLine;
            private set => editedMesh.pointedLine = value;
        }
        public static PainterMesh.Triangle PointedTriangle { get => editedMesh.pointedTriangle;
            private set => editedMesh.pointedTriangle = value;
        }
        public static int TriVertices { get => editedMesh.triVertices;
            set => editedMesh.triVertices = value;
        }

        private static UvSetIndex _editedUv;
        public int EditedUV
        {
            get { return _editedUv.index; }
            set { _editedUv.index = value; QcUnity.SetShaderKeyword(PainterShaderVariables.MESH_PREVIEW_UV2, _editedUv.index == 1); }
        }

        #endregion
        
       
       

        private static readonly List<string> UndoMoves = new();

        private static readonly List<string> RedoMoves = new();

        internal static MeshData editedMesh = new();
        internal static MeshData previewEdMesh;

        private int _currentUv;
        private bool _selectingUVbyNumber;
        public int verticesShowMax = 8;


        #region Encode & Decode

        public override CfgEncoder Encode() => new CfgEncoder()//this.EncodeUnrecognized()
            .Add_IfTrue("byUV", _selectingUVbyNumber);

        public override void DecodeTag(string key, CfgData data)
        {
            switch (key)
            {
                case "byUV": _selectingUVbyNumber = data.ToBool(); break;
            }
        }

        #endregion
        


        public void EditMesh(PainterComponent painter, bool editCopy)
        {
            if (!painter || painter == MeshPainting.target)
                return;

            if (MeshPainting.target)
                StopEditingMesh();

            MeshPainting.target = painter;
            MeshPainting.targetTransform = painter.transform;
            editedMesh = new MeshData(painter);

            if (editCopy)
                painter.SharedMesh = new Mesh();

            Redraw();
            
            InitGridIfNull();

            UndoMoves.Clear();
            RedoMoves.Clear();

            UndoMoves.Add(editedMesh.Encode().ToString());

            MeshTool.OnSelectTool();

        }

        public void StopEditingMesh()
        {

            if (MeshPainting.target) {
                MeshTool.OnDeSelectTool();
                MeshPainting.target.SavedEditableMesh = new CfgData(editedMesh.Encode().ToString());
                MeshPainting.target = null;
                MeshPainting.targetTransform = null;
            }
            Grid.DeactivateVertices();
            MeshPainting.Grid.SetEnabled(false, false);
            UndoMoves.Clear();
            RedoMoves.Clear();
        }

        public void Redraw() {

            previewEdMesh = null;

            if (MeshPainting.target) {

                editedMesh.Dirty = false;

                var mc = new MeshConstructor(editedMesh, MeshPainting.target.MeshProfile, MeshPainting.target.SharedMesh);

                if (!editedMesh.dirtyVertexIndexes && EditedMesh.Dirty) {

                    if (EditedMesh.dirtyPosition)
                        mc.UpdateMesh<VertexDataTypes.VertexPos>();

                    if (editedMesh.dirtyColor)
                        mc.UpdateMesh<VertexDataTypes.VertexColor>();

                    if (editedMesh.dirtyUvs)
                        mc.UpdateMesh<VertexDataTypes.VertexUv>();

                }  else {
                    var m = mc.Construct();
                    MeshPainting.target.SharedMesh = m;
                    MeshPainting.target.UpdateMeshCollider(m); 
                }
            }
        }

        public static string GenerateMeshSavePath() => Path.Combine(Painter.Data.meshesFolderName, editedMesh.meshName + ".asset");

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

            var temp = new PainterMesh.MeshPoint(SelectedUv.meshPoint);

            editedMesh.meshPoints.Add(temp);

            SelectedUv.AssignToNewVertex(temp);

            editedMesh.Dirty = true;//_Mesh.GenerateMesh(_targetPiece);
        }

        public void MoveVertexToGrid(PainterMesh.MeshPoint vp)
        {
            MeshPainting.UpdateLocalSpaceMousePosition();

            var diff = MeshPainting.onGridLocal - vp.localPos;

            diff.Scale(MeshPainting.Grid.GetGridPerpendicularVector());
            vp.localPos += diff;
        }

        public void AssignSelected(PainterMesh.Vertex newPnt)
        {
            SelectedUv = newPnt;
            if (Input.GetKey(KeyCode.LeftAlt))
            {
                MoveVertexToGrid(SelectedUv.meshPoint);
                editedMesh.Dirty = true;
            }
            else
                if (!PlaytimePainter_EditorInputManager.Control)
            {
                MeshPainting.LatestMouseToGridProjection = SelectedUv.meshPoint.WorldPos;
                Grid.UpdatePositions();
            }
        }

        public bool DeleteVertexHeal(PainterMesh.MeshPoint vertex)
        {

            var trs = new PainterMesh.Triangle[3];

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

        public PainterMesh.MeshPoint CreatePointAndFocus(Vector3 pos)
        {
            var hold = new PainterMesh.MeshPoint(pos, true);
            
            var vertex = new PainterMesh.Vertex(hold);

            editedMesh.meshPoints.Add(hold);
            
            if (!PlaytimePainter_EditorInputManager.Control)
                EditedMesh.AddToTrisSet(vertex);

            if (Painter.Data.pixelPerfectMeshEditing)
                hold.PixPerfect();

            MeshPainting.LatestMouseRaycastHit = pos;

            MeshPainting.UpdateLocalSpaceMousePosition();

            return hold;
        }

        #endregion

        #region Tool MGMT

        public const string VertexEditorUiElementTag = "VertexEd";

        public const string ToolComponentTag = "toolComponent";

        public static List<string> meshEditorIgnore = new() { VertexEditorUiElementTag, ToolComponentTag };

        private void ProcessLinesOnTriangle(PainterMesh.Triangle t)
        {
            t.wasProcessed = true;
            const float precision = 0.05f;

            var acc = (MeshPainting.targetTransform.InverseTransformPoint(CurrentViewTransform().position) - MeshPainting.collisionPosLocal).magnitude;

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
                return;
            }

            if (QcMath.IsPointOnLine(v1p, v2p, Vector3.Distance(v1.LocalPos, v2.LocalPos), acc))
            {
                ProcessPointOnALine(v1, v2, t);
                return;
            }

            if (QcMath.IsPointOnLine(v2p, v0p, Vector3.Distance(v2.LocalPos, v0.LocalPos), acc))
            {
                ProcessPointOnALine(v2, v0, t);
            }
        }

        private void GetPointedTriangleOrLine()
        {

            editedMesh.TagTrianglesUnprocessed();

            MeshPainting.UpdateLocalSpaceMousePosition();

            foreach (var t1 in editedMesh.meshPoints)
            foreach (var uv in t1.vertices)
            foreach (var t in uv.triangles)
                if (!t.wasProcessed)
                {
                    t.wasProcessed = true;

                    if (!t.PointOnTriangle()) continue;

                    if (PlaytimePainter_EditorInputManager.GetMouseButtonDown(0))
                    {
                        SelectedTriangle = t;
                        AssignSelected(t.GetClosestTo(MeshPainting.collisionPosLocal));
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
            var alt = PlaytimePainter_EditorInputManager.Alt;

            if (alt)
                MeshPainting.LatestMouseRaycastHit = MeshPainting.LatestMouseToGridProjection;
            
            RaycastHit hit;
            var vertexIsPointed = false;

            if (GridNavigator.RaycastMouse(out hit))
            {

                vertexIsPointed = (hit.transform.CompareTag(VertexEditorUiElementTag));

                if (!alt)
                {

                    if (vertexIsPointed)
                    {
                        MeshPainting.LatestMouseRaycastHit = hit.transform.position;
                        MeshPainting.UpdateLocalSpaceMousePosition();
                        editedMesh.SortAround(MeshPainting.collisionPosLocal, true);

                    }
                    else
                    {
                        MeshPainting.LatestMouseRaycastHit = hit.point;
                        MeshPainting.UpdateLocalSpaceMousePosition();
                        editedMesh.SortAround(MeshPainting.collisionPosLocal, true);
                        GetPointedTriangleOrLine();
                    }
                }
            }
            
            MeshPainting.UpdateLocalSpaceMousePosition();
            return vertexIsPointed;
        }

        private void ProcessPointOnALine(PainterMesh.Vertex a, PainterMesh.Vertex b, PainterMesh.Triangle t)
        {

            if (PlaytimePainter_EditorInputManager.GetMouseButtonDown(1))
            {
                SelectedLine = new PainterMesh.LineData(t, a, b);
                MeshPainting.UpdateLocalSpaceMousePosition();
            }

            PointedLine = new PainterMesh.LineData(t, new[] { a, b });

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

                    if (PlaytimePainter_EditorInputManager.GetMouseButtonDown(0))
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
                InitGridIfNull();

            MeshPainting.UpdateLocalSpaceMousePosition();

            editedMesh.SortAround(MeshPainting.collisionPosLocal, false);

            const float scaling = 16;

            Grid.selectedVertex.go.SetActive(false);
            Grid.pointedVertex.go.SetActive(false);

            for (var i = 0; i < verticesShowMax; i++)
                Grid.vertices[i].go.SetActive(false);

            if (!MeshTool.ShowVertices) return;

            var camTf = CurrentViewTransform();

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
                tf.SetPositionAndRotation(worldPos, camTf.rotation);
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
            if (!MeshPainting.target)
                return;

            if (!MeshPainting.target.enabled)  
            {
                StopEditingMesh();
                return;
            }

            var no = PlaytimePainter_EditorInputManager.GetNumberKeyDown();
            _selectingUVbyNumber = false;
            if (no != -1) { _currentUv = no - 1; _selectingUVbyNumber = true; } else _currentUv = 0;

            if (Application.isPlaying)
                UpdateInputPlaytime();
            
            if (Grid)
                Grid.UpdatePositions();

            if (Application.isPlaying)
                SortAndUpdate();

            _delayUpdate -= Time.deltaTime;

            if (editedMesh.Dirty && _delayUpdate<0) {

                RedoMoves.Clear();

                if (Painter.Data.saveMeshUndos) {
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
        public void UpdateInputEditorTime(Event e)
        {

            if (!MeshPainting.target || _justLoaded > 0)
                return;

            if (e.type == EventType.KeyDown) {
               
                ProcessKeyInputs();

                switch (e.keyCode)
                {

                    case KeyCode.Delete: 
                    case KeyCode.Backspace: e.Use(); break;
                }
            }
            
            if (e.isMouse || (e.type == EventType.ScrollWheel)) 
                ProcessMouseActions();
            
            SortAndUpdate();
        }
        #endif

        public void UpdateInputPlaytime()
        {
            if (pegi.GameView.MouseOverUI)
                return;
      
            ProcessMouseActions();
            ProcessKeyInputs();
        }
        #endregion
        
        public PainterMesh.MeshPoint GetSelectedVertex()
        {
            if (SelectedUv != null) return SelectedUv.meshPoint;
            return null;
        }

        private bool SameTriangleAsPointed(PainterMesh.MeshPoint uvi)
        {
            if (PointedUv == null) return false;
            foreach (PainterMesh.Triangle t in editedMesh.triangles)
            {
                if (t.Includes(uvi) && t.Includes(PointedUv)) return true;
            }
            return false;
        }

        private void InitGridIfNull()
        {
            if (!Grid)
                return;

            Grid.InitializeIfNeeded(verticesShowMax);
        }

        public void OnEnable()
        {
            InitGridIfNull();

        
            TriVertices = 0;
            EditedUV = EditedUV;

        }

        private int _justLoaded;

        #region Mesh Merging

        private static readonly List<PainterComponent> SelectedForMergePainters = new();

        private static void MergeSelected() {

            var mats = MeshPainting.target.Materials.ToList();

            foreach (var p in SelectedForMergePainters)
                if (MeshPainting.target != p) {

                    var nms = p.Materials;

                    var em = new MeshData(p);
                    
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

            MeshPainting.target.Materials = mats.ToArray();
        }

        private static void MergeSubMeshes()
        {
            var mats = MeshPainting.target.Materials;

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

            QcSharp.Resize(ref mats, 1);

            MeshPainting.target.Materials = mats;
        }

        #endregion

        #region Inspector

        private readonly pegi.EnterExitContext contenxt = new(); 

        void IPEGI.Inspect()  {

                var changed = pegi.ChangeTrackStart();
                MeshData.inspected = editedMesh;


                MeshPainting.target.Inspect_ConvexMeshCheckWarning();

                pegi.Nl();

                MeshPainting.target.Inspect_PreviewShaderToggle();

                if (!MeshPainting.target.NotUsingPreview && "preview".PegiLabel(45).Select(ref MeshShaderMode.selected, MeshShaderMode.AllModes).Nl())
                    MeshShaderMode.ApplySelected();

                var previousTool = MeshTool;

                if ("tool:".PegiLabel(35).Select_Index(ref Painter.Data.meshTool, MeshToolBase.AllTools))
                {
                    Grid.vertexPointMaterial.color = MeshTool.VertexColor; //.SetColor("_Color", MeshTool.VertexColor);
                    previousTool.OnDeSelectTool();
                    MeshTool.OnSelectTool();
                }


                if (DocsEnabled)
                    pegi.FullWindow.DocumentationClickOpen(text: () => MeshTool.Tooltip + (MeshTool.ShowGrid ? GridNavigator.ToolTip : ""),
                        toolTip: "About {0} tool".F(MeshTool.ToString()));


                if (MeshPainting.target.skinnedMeshRenderer)
                    pegi.FullWindow.WarningDocumentationClickOpen
                    ("When using Skinned Mesh Renderer, the mesh will be transformed by it, so mesh points will not be in the correct position, and it is impossible to do any modifications on mesh with the mouse. It is still possible to do automatic processes like " +
                     "changing mesh profile and everything that doesn't require direct input from mouse over the object. It is recommended to edit the object separately from the skinned mesh."
                        , "Skinned mesh detected");

                pegi.Nl();

                var mt = MeshTool;

                mt.Inspect();

                pegi.Nl();

                foreach (var p in CameraModuleBase.MeshToolPlugins)
                    pegi.Nested_Inspect(() => p.MeshToolInspection(mt)).Nl();

                pegi.Nl();

                Grid.vertexPointMaterial.color = MeshTool.VertexColor;
                //Grid.vertexPointMaterial.SetColor("_Color", MeshTool.VertexColor);

                MeshData.inspected = null;

                if (changed)
                    MeshTool.SetShaderKeywords();
            

        }
        
        private Vector3 _offset;
        public bool MeshOptionsInspect(pegi.EnterExitContext parentContext)
        {
            var changed = pegi.ChangeTrackStart();

            if ("Mesh ".PegiLabel().IsConditionally_Entered(canEnter: editedMesh != null).Nl())
            {
                using (contenxt.StartContext())
                {
                    if (contenxt.IsAnyEntered == false)
                    {

#if UNITY_EDITOR
                        "Mesh Name:".PegiLabel(70).Edit(ref editedMesh.meshName);

                        Mesh mesh = MeshPainting.target.GetMesh();

                        if (editedMesh.meshName.Equals(mesh.name) == false && Icon.Refresh.Click("Reset Mesh Name"))
                            editedMesh.meshName = mesh.name;

                        

                        var exists = QcUnity.IsSavedAsAsset(mesh);

                        if ((exists ? Icon.Save : Icon.SaveAsNew)
                            .Click(exists ? "Override original" : "Save Mesh As {0}".F(GenerateMeshSavePath())).Nl())
                            MeshPainting.target.SaveMesh();
#endif

                        "Save Undo".PegiLabel().ToggleIcon(ref Painter.Data.saveMeshUndos);
                        if (Painter.Data.saveMeshUndos)
                            Icon.Warning.Draw("Can affect peformance");
                        pegi.Nl();
                    }

                    editedMesh.Nested_Inspect().Nl();

                    if ("Center".PegiLabel().IsEntered().Nl())
                    {
                        "Offset Center by:".PegiLabel().Edit(ref _offset).Nl();
                        if ("Modify".PegiLabel().Click().Nl())
                        {
                            foreach (var v in EditedMesh.meshPoints)
                                v.localPos += _offset;

                            _offset = -_offset;

                            editedMesh.Dirty = true;

                        }

                        if ("Auto Center".PegiLabel().Click().Nl())
                        {
                            var avr = Vector3.zero;
                            foreach (var v in EditedMesh.meshPoints)
                                avr += v.localPos;

                            _offset = -avr / EditedMesh.meshPoints.Count;
                        }

                    }

                    if ("Combining meshes".PegiLabel().IsEntered().Nl())
                    {

                        if (!SelectedForMergePainters.Contains(MeshPainting.target))
                        {
                            if ("Add To Group".PegiLabel("Add Mesh to the list of meshes to be merged").Click().Nl())
                                SelectedForMergePainters.Add(MeshPainting.target);

                            if (!SelectedForMergePainters.IsNullOrEmpty())
                            {

                                if (editedMesh.uv2DistributeRow < 2 && "Enable EV2 Distribution".PegiLabel("Each mesh's UV2 will be modified to use a unique portion of a texture.").ToggleInt(ref editedMesh.uv2DistributeRow).Nl())
                                    editedMesh.uv2DistributeRow = Mathf.Max(2, (int)Mathf.Sqrt(SelectedForMergePainters.Count));
                                else
                                {
                                    if (editedMesh.uv2DistributeCurrent > 0)
                                    {
                                        ("All added meshes will be distributed in " + editedMesh.uv2DistributeRow + " by " + editedMesh.uv2DistributeRow + " grid. By cancelling this added" +
                                            "meshes will have UVs unchanged and may use the same portion of Texture (sampled with UV2) as other meshes.").PegiLabel().Write_Hint();
                                        if ("Cancel Distribution".PegiLabel().Click().Nl())
                                            editedMesh.uv2DistributeRow = 0;
                                    }
                                    else
                                    {
                                        "Row:".PegiLabel("Will change UV2 so that every mesh will have it's own portion of a texture.", 25).Edit(ref editedMesh.uv2DistributeRow, 2, 16).Nl();
                                        "Start from".PegiLabel().Edit(ref editedMesh.uv2DistributeCurrent).Nl();
                                    }

                                    "Using {0} out of {1} spots".F(editedMesh.uv2DistributeCurrent + SelectedForMergePainters.Count + 1, editedMesh.uv2DistributeRow * editedMesh.uv2DistributeRow)
                                        .PegiLabel().Nl();

                                }
                            }
                        }
                        else
                        {
                            if (SelectedForMergePainters.Count > 1 && "Merge!".PegiLabel().Click().Nl())
                                MergeSelected();

                            if ("Remove from Merge Group".PegiLabel().Click().Nl())
                                SelectedForMergePainters.Remove(MeshPainting.target);

                        }

                        if (SelectedForMergePainters.Count > 1)
                        {
                            "Current merging group:".PegiLabel().Nl();
                            for (var i = 0; i < SelectedForMergePainters.Count; i++)
                                if (!SelectedForMergePainters[i] || Icon.Delete.Click(25))
                                {
                                    SelectedForMergePainters.RemoveAt(i);
                                    i--;
                                }
                                else
                                {
                                    SelectedForMergePainters[i].gameObject.name.PegiLabel().Nl();
                                }
                        }

                    }

                    var mats = MeshPainting.target.Materials;
                    if ("Combining Sub Meshes".PegiLabel().IsConditionally_Entered(mats.Length > 1).Nl())
                    {

                        "Select which materials should transfer color into vertex color".PegiLabel().Write_Hint();

                        var subMeshCount = MeshPainting.target.GetMesh().subMeshCount;

                        for (var i = 0; i < Mathf.Max(subMeshCount, mats.Length); i++)
                        {
                            var m = mats.TryGet(i);

                            if (!m)
                                "Null".PegiLabel().Nl();
                            else
                            {
                                var md = m.GetMaterialPainterMeta();

                                "{0} color to vertex color".F(m.name).PegiLabel().ToggleIcon(ref md.colorToVertexColorOnMerge, true);

                                if (md.colorToVertexColorOnMerge)
                                {
                                    var col = m.color;
                                    if (pegi.Edit(ref col))
                                        m.color = col;
                                }


                                if (i >= subMeshCount)
                                    Icon.Warning.Draw("There are more materials then sub meshes on the mesh");
                            }

                            pegi.Nl();
                        }

                        if ("Merge All Submeshes".PegiLabel().Click())
                            MergeSubMeshes();


                    }

                    pegi.Nl();

                    if (!Application.isPlaying && "Debug".PegiLabel().IsFoldout().Nl())
                    {
                        Grid.Nested_Inspect();
                        "Max Vertex Markers ".PegiLabel().Edit(ref verticesShowMax).Nl();
                    }
                }
            }
            

            return changed;
        }

        public void UndoRedoInspect()
        {
            if (UndoMoves.Count > 1) {
                if (Icon.Undo.Click(25)) {
                    RedoMoves.Add(UndoMoves.RemoveLast());
                    new CfgData(UndoMoves.TryGetLast()).Decode(out editedMesh);
                    Redraw();
                }
            }
            else
                Icon.UndoDisabled.Click("Nothing to Undo (set number of undo frames in config)", 25);

            if (RedoMoves.Count > 0) {
                if (Icon.Redo.Click(25)) {
                    new CfgData(RedoMoves.TryGetLast()).Decode(out editedMesh);
                    UndoMoves.Add(RedoMoves.RemoveLast());
                    Redraw();
                }
            }
            else
                Icon.RedoDisabled.Click("Nothing to Redo", 25);

            pegi.Nl();
        }
        
        #endregion

        #region Editor Gizmos

        private void OutlineTriangle(PainterMesh.Triangle t, Color colA, Color colB)
        {
            //bool vrt = tool == VertexPositionTool.inst;
            Line(t.vertexes[0], t.vertexes[1], t.isPointDominant[0] ? colA : colB, t.isPointDominant[1] ? colA : colB);
            Line(t.vertexes[1], t.vertexes[2], t.isPointDominant[1] ? colA : colB, t.isPointDominant[2] ? colA : colB);
            Line(t.vertexes[0], t.vertexes[2], t.isPointDominant[0] ? colA : colB, t.isPointDominant[2] ? colA : colB);
        }

        private void Line(PainterMesh.Vertex a, PainterMesh.Vertex b, Color colA, Color colB)
        {
            Line(a.meshPoint, b.meshPoint, colA, colB);
        }

        private void Line(PainterMesh.MeshPoint a, PainterMesh.MeshPoint b, Color colA, Color colB)
        {

            Vector3 v3a = a.WorldPos;
            Vector3 v3b = b.WorldPos;
            Vector3 diff = (v3b - v3a) / 2;
            Line(v3a, v3a + diff, colA);
            Line(v3b, v3b - diff, colB);
        }

        private void Line(PainterMesh.MeshPoint a, PainterMesh.MeshPoint b, Color colA) => Line(a.WorldPos, b.WorldPos, colA);
        
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

            var piecePos = MeshPainting.targetTransform.TransformPoint(-Vector3.one / 2);//PositionScripts.PosUpdate(_target.getpos(), false);


            var projected = MeshPainting.ProjectToGrid(piecePos); // piecePos * getGridMaskVector() + ptdPos.ToV3(false)*getGridPerpendicularVector();
            var gridMask = MeshPainting.Grid.GetGridMaskVector() * 128 + projected;
            
            Debug.DrawLine(new Vector3(projected.x, projected.y, projected.z), new Vector3(gridMask.x, projected.y, projected.z), Color.red);
            Debug.DrawLine(new Vector3(projected.x, projected.y, projected.z), new Vector3(projected.x, gridMask.y, projected.z), Color.red);
            Debug.DrawLine(new Vector3(projected.x, projected.y, projected.z), new Vector3(projected.x, projected.y, gridMask.z), Color.red);

            Debug.DrawLine(new Vector3(projected.x, gridMask.y, gridMask.z), new Vector3(gridMask.x, gridMask.y, gridMask.z), Color.red);
            Debug.DrawLine(new Vector3(gridMask.x, projected.y, gridMask.z), new Vector3(gridMask.x, gridMask.y, gridMask.z), Color.red);
            Debug.DrawLine(new Vector3(gridMask.x, gridMask.y, projected.z), new Vector3(gridMask.x, gridMask.y, gridMask.z), Color.red);

            DrawTransformedCubeDebug(MeshPainting.targetTransform, Color.blue);


        }
        
        public void DRAW_Lines(bool isGizmoCall)
        {

            GizmoLines = isGizmoCall;

            if (!MeshPainting.target) return;

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
                    Line(PointedLine.vertexes[0].meshPoint, PointedLine.vertexes[1].meshPoint, Color.green);

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
                    PainterMesh.MeshPoint vp = editedMesh.meshPoints[i];
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
                        PainterMesh.Triangle td = editedMesh.triangles[i];
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
            Vector3 dlb = new (-0.5f, -0.5f, -0.5f);
            Vector3 dlf = new (-0.5f, -0.5f, 0.5f);
            Vector3 drb = new (-0.5f, 0.5f, -0.5f);
            Vector3 drf = new (-0.5f, 0.5f, 0.5f);

            Vector3 ulb = new (0.5f, -0.5f, -0.5f);
            Vector3 ulf = new (0.5f, -0.5f, 0.5f);
            Vector3 urb = new (0.5f, 0.5f, -0.5f);
            Vector3 urf = new (0.5f, 0.5f, 0.5f);

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

        private static readonly List<MeshShaderMode> _allModes = new();

        public static List<MeshShaderMode> AllModes => _allModes;

        private MeshShaderMode(string value) { this.value = value; _allModes.Add(this); }

        public static MeshShaderMode lit = new          (PainterShaderVariables.MESH_PREVIEW_LIT);
        public static MeshShaderMode normVector = new   (PainterShaderVariables.MESH_PREVIEW_NORMAL);
        public static MeshShaderMode vertexColor = new  (PainterShaderVariables.MESH_PREVIEW_VERTCOLOR);
        public static MeshShaderMode projection = new   (PainterShaderVariables.MESH_PREVIEW_PROJECTION);

        public static MeshShaderMode selected;

        public string value;

        public override string ToString() => value;

        public static void ApplySelected() {
            selected ??= _allModes[0];

            foreach (MeshShaderMode s in _allModes)
                QcUnity.SetShaderKeyword(s.value, selected == s);

        }
    }
}