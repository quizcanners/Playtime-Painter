using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using PlayerAndEditorGUI;

using QuizCannersUtilities;

namespace Playtime_Painter
{

    public interface IMeshToolWithPerMeshData {
        CfgEncoder EncodePerMeshData();
    }

    #region Base
    public class MeshToolBase : PainterSystemCfg, IPEGI, IGotDisplayName
    {

        protected enum DetectionMode { Points, Lines, Triangles }

        public virtual string stdTag => "t_noStd";

        public delegate bool MeshToolPlugBool(MeshToolBase tool, out bool val);

        protected static bool Dirty { get { return EditedMesh.Dirty; } set { EditedMesh.Dirty = value; } }

        protected virtual void SetShaderKeyword(bool enablePart) { }

        public virtual void SetShaderKeywords()  {

            foreach (var t in AllTools)
                t.SetShaderKeyword(false);

            SetShaderKeyword(true);

        }

        public static List<IMeshToolWithPerMeshData> allToolsWithPerMeshData;

        private static List<MeshToolBase> _allTools;

        public static List<MeshToolBase> AllTools
        {
            get
            {
                if (!_allTools.IsNullOrEmpty() || applicationIsQuitting) return _allTools;
                
                _allTools = new List<MeshToolBase>();
                allToolsWithPerMeshData = new List<IMeshToolWithPerMeshData>();
                /* 
                  var tps = CsharpUtils.GetAllChildTypesOf<MeshToolBase>();

                  foreach (var t in tps)
                      _allTools.Add((MeshToolBase)Activator.CreateInstance(t));*/
                      
                _allTools.Add(new VertexPositionTool());
                _allTools.Add(new SharpFacesTool());
                _allTools.Add(new VertexColorTool());
                _allTools.Add(new VertexEdgeTool());
                _allTools.Add(new TriangleAtlasTool());
                _allTools.Add(new TriangleSubMeshTool());
                _allTools.Add(new VertexShadowTool());
                _allTools.Add(new VertexUVTool());

                foreach (var t in _allTools)
                    allToolsWithPerMeshData.TryAdd(t as IMeshToolWithPerMeshData);
                
                return _allTools;
            }
        }
        
        protected static LineData PointedLine => MeshMGMT.PointedLine;
        protected static Triangle PointedTriangle => MeshMGMT.PointedTriangle;
        protected Triangle SelectedTriangle => MeshMGMT.SelectedTriangle;
        protected static Vertex PointedUv => MeshMGMT.PointedUv;
        protected static Vertex SelectedUv => MeshMGMT.SelectedUv;
        protected static MeshPoint PointedVertex => MeshMGMT.PointedUv.meshPoint;
        protected static EditableMesh GetPreviewMesh
        {
            get
            {
                if (MeshManager.previewEdMesh == null)
                {
                    MeshManager.previewEdMesh = new EditableMesh();
                    MeshManager.previewEdMesh.Decode(EditedMesh.Encode().ToString());
                    //Debug.Log("Recreating preview");
                }
                return MeshManager.previewEdMesh;
            }
        }
        
        public virtual bool ShowVertices => true;
        public virtual bool ShowLines => true;
        public virtual bool ShowTriangles => true;

        public virtual bool ShowGrid => false;

        public virtual bool ShowSelectedVertex => false;
        public virtual bool ShowSelectedLine => false;
        public virtual bool ShowSelectedTriangle => false;

        public virtual Color VertexColor => Color.gray;

        public virtual void OnSelectTool() { }

        public virtual void OnDeSelectTool() { }

        public virtual void OnGridChange() { }

        public virtual void AssignText(MarkerWithText markers, MeshPoint point) => markers.textm.text = "";

        public virtual bool MouseEventPointedVertex() => false;

        public virtual bool MouseEventPointedLine() => false;

        public virtual bool MouseEventPointedTriangle() => false;

        public virtual void MouseEventPointedNothing() { }

        public virtual void KeysEventPointedVertex() { }

        public virtual void KeysEventDragging() { }

        public virtual void KeysEventPointedLine() { }

        public virtual void KeysEventPointedTriangle() { }

        public virtual void KeysEventPointedWhatever() { }
        
        public virtual void ManageDragging() { }

        #region Encode & Decode
        public override CfgEncoder Encode() => null;

        public override bool Decode(string tg, string data) => false;
        #endregion

        #region Inspector
        public virtual string Tooltip => "No toolTip";

        public virtual string NameForDisplayPEGI => " No Name ";

        public virtual bool Inspect() => false;
        #endregion
    }
    #endregion

    #region Position

    public class VertexPositionTool : MeshToolBase
    {
        public override string stdTag => "t_pos";

        private bool _addToTrianglesAndLines;

        private DetectionMode _detectionMode;

        public override bool ShowVertices => _detectionMode == DetectionMode.Points;

        public override bool ShowLines => _detectionMode == DetectionMode.Lines;

        public override bool ShowTriangles => _detectionMode == DetectionMode.Triangles;

        private static List<MeshPoint> _draggedVertices => EditedMesh._draggedVertices; // new List<MeshPoint>();

        private Vector3 _originalPosition;

        public override bool ShowGrid => true;

        public override void AssignText(MarkerWithText markers, MeshPoint point)
        {

            var selected = MeshMGMT.GetSelectedVertex();

            if (point.vertices.Count > 1 || selected == point)
            {
                if (selected == point)
                {
                    markers.textm.text = (point.vertices.Count > 1) ? (
                        Path.Combine((point.vertices.IndexOf(MeshMGMT.SelectedUv) + 1).ToString(), point.vertices.Count +
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
        public override string Tooltip =>

                    ("Alt - Project Vertex To Grid {0}" +
                    "LMB - Add Vertices/Make Triangles (Go Clockwise), Drag {0}"+
                    "Scroll - Change Plane {0}"+
                    "U - make Triangle unique. {0}" + 
                    "M - merge with nearest while dragging {0}" +
                    "This tool also contains functionality related to smoothing and sharpening of the edges.").F(Environment.NewLine);

        public override string NameForDisplayPEGI => "ADD & MOVE";

        #if PEGI
        public override bool Inspect()
        {

            var changed = false;

            var mgm = MeshMGMT;

            var sd = TexMGMTdata;

            var em = EditedMesh;

            "Mode".editEnum(40, ref _detectionMode).nl(ref changed);

            if (mgm.MeshTool.ShowGrid) {
                "Snap to grid:".toggleIcon(ref sd.snapToGrid).nl(ref changed);

                if (sd.snapToGrid)
                    "size:".edit(40, ref sd.gridSize).changes(ref changed);
            }

            pegi.nl();

            "Pixel-Perfect".toggleIcon("New vertex will have UV coordinate rounded to half a pixel.", ref Cfg.pixelPerfectMeshEditing).nl(ref changed);

            "Add into mesh".toggleIcon("Will split triangles and edges by inserting vertices", ref _addToTrianglesAndLines).nl(ref changed);

            "Add Smooth:".toggleIcon( ref Cfg.newVerticesSmooth).nl(ref changed);

            if ("Sharp All".Click(ref changed))
            {
                foreach (var vr in EditedMesh.meshPoints)
                    vr.smoothNormal = false;

                EditedMesh.Dirty = true;
                Cfg.newVerticesSmooth = false;
            }

            if ("Smooth All".Click().nl())
            {
                foreach (var vr in EditedMesh.meshPoints)
                    vr.smoothNormal = true;
                EditedMesh.Dirty = true;
                Cfg.newVerticesSmooth = true;
            }

            "Add Unique:".toggleIcon( ref Cfg.newVerticesUnique).nl();

            if ("All shared if same UV".Click("Will only merge vertices if they have same UV").nl()) {
                em.AllVerticesSharedIfSameUV();
                em.Dirty = true;

            }

            if ("All shared".Click()) {
                em.AllVerticesShared();
                em.Dirty = true;
                Cfg.newVerticesUnique = false;
            }

            if ("All unique".Click().nl())
            {
                foreach (var t in EditedMesh.triangles)
                    em.GiveTriangleUniqueVertices(t);

                Cfg.newVerticesUnique = true;
            }

            if ("Auto Bevel".Click())
                SharpFacesTool.AutoAssignDominantNormalsForBeveling();
            "Sensitivity".edit(60, ref Cfg.bevelDetectionSensitivity, 3, 30).nl();
            
            pegi.newLine();

            return changed;
        }
        #endif
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
            var m = MeshMGMT;
            if (KeyCode.M.IsDown() && m.SelectedUv != null)
            {
                m.SelectedUv.meshPoint.MergeWithNearest(EditedMesh);
                m.Dragging = false;
                EditedMesh.Dirty = true;
#if PEGI
                "M - merge with nearest".TeachingNotification();
#endif
            }
        }

        public override void KeysEventPointedVertex()
        {

            var m = MeshMGMT;

            if (KeyCode.Delete.IsDown())
            {
                if (!EditorInputManager.Control)
                {
                    if (m.PointedUv.meshPoint.vertices.Count == 1)
                    {
                        if (!m.DeleteVertexHeal(MeshMGMT.PointedUv.meshPoint))
                            EditedMesh.DeleteUv(MeshMGMT.PointedUv);
                    }
                    else
                        while (m.PointedUv.meshPoint.vertices.Count > 1)
                            EditedMesh.MoveTriangle(m.PointedUv.meshPoint.vertices[1], m.PointedUv.meshPoint.vertices[0]);


                }
                else
                    EditedMesh.DeleteUv(m.PointedUv);

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

            if (EditorInputManager.GetMouseButtonDown(0))
            {
                var m = MeshMGMT;

                m.Dragging = true;
                m.AssignSelected(m.PointedUv);
                _originalPosition = PointedUv.meshPoint.WorldPos;
                _draggedVertices.Clear();
                _draggedVertices.Add(PointedUv.meshPoint);


            }

            return false;

        }

        public override bool MouseEventPointedLine()
        {

            var m = MeshMGMT;

            if (EditorInputManager.GetMouseButtonDown(0))
            {
                m.Dragging = true;
                _originalPosition = GridNavigator.collisionPos;
                GridNavigator.onGridPos = GridNavigator.collisionPos;
                _draggedVertices.Clear();
                foreach (var uv in PointedLine.points)
                    _draggedVertices.Add(uv.meshPoint);
            }

            if (EditorInputManager.GetMouseButtonUp(0))
            {
                if (_addToTrianglesAndLines && m.DragDelay > 0 && _draggedVertices.Contains(PointedLine))
                    EditedMesh.InsertIntoLine(MeshMGMT.PointedLine.points[0].meshPoint, MeshMGMT.PointedLine.points[1].meshPoint, MeshMGMT.collisionPosLocal);
            }

            return false;
        }

        public override bool MouseEventPointedTriangle()
        {
            var m = MeshMGMT;

            if (EditorInputManager.GetMouseButtonDown(0))
            {

                m.Dragging = true;
                _originalPosition = GridNavigator.collisionPos;
                GridNavigator.onGridPos = GridNavigator.collisionPos;
                _draggedVertices.Clear();
                foreach (var uv in PointedTriangle.vertexes)
                    _draggedVertices.Add(uv.meshPoint);

            }

            if (!_addToTrianglesAndLines || !EditorInputManager.GetMouseButtonUp(0) || !(m.DragDelay > 0) ||
                !_draggedVertices.Contains(PointedTriangle)) return false;
            
            if (Cfg.newVerticesUnique)
                EditedMesh.InsertIntoTriangleUniqueVertices(m.PointedTriangle, m.collisionPosLocal);
            else
                EditedMesh.InsertIntoTriangle(m.PointedTriangle, m.collisionPosLocal);

            return false;
        }

        public override void MouseEventPointedNothing()
        {
            if (EditorInputManager.GetMouseButtonDown(0))
                MeshMGMT.AddPoint(MeshMGMT.onGridLocal);
        }

        public override void ManageDragging()
        {
            var m = MeshMGMT;

            var beforeCouldDrag = m.DragDelay <= 0;

            if (EditorInputManager.GetMouseButtonUp(0) || !EditorInputManager.GetMouseButton(0))
            {
                m.Dragging = false;

                if (beforeCouldDrag)
                    EditedMesh.dirtyPosition = true;
                else if (m.TriVertices < 3 && m.SelectedUv != null && !EditedMesh.IsInTriangleSet(m.SelectedUv.meshPoint))
                    EditedMesh.AddToTrisSet(m.SelectedUv);

            }
            else
            {
               
                var canDrag = m.DragDelay <= 0;

                if (beforeCouldDrag != canDrag && EditorInputManager.Alt && m.SelectedUv.meshPoint.vertices.Count > 1)
                    m.DisconnectDragged();

                if (!canDrag || !(GridNavigator.Inst().AngGridToCamera(GridNavigator.onGridPos) < 82)) return;
                
                var delta = GridNavigator.onGridPos - _originalPosition;

                if (delta.magnitude == 0)
                    return;
                
                m.TriVertices = 0;

                foreach (var v in _draggedVertices)
                    v.WorldPos += delta;

                _originalPosition = GridNavigator.onGridPos;
            }
        }
        #endregion

        #region Encode & Decode
        public override CfgEncoder Encode() => new CfgEncoder()
            .Add("dm",(int)_detectionMode)
            .Add_Bool("inM", _addToTrianglesAndLines);
        
        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "inM": _addToTrianglesAndLines = data.ToBool(); break;
                case "dm": _detectionMode = (DetectionMode) data.ToInt(); break;
                default: return false;
            }
            return true;
        }
        #endregion

    }

    #endregion

    #region Sharp Faces


    public class SharpFacesTool : MeshToolBase
    {
        public override string stdTag => "t_shF";
        
        public static SharpFacesTool inst;

        public SharpFacesTool()
        {
            inst = this;
        }

        private bool _setTo = true;

        private DetectionMode _detectionMode;
        
        public override bool ShowVertices => _detectionMode == DetectionMode.Points;

        public override bool ShowLines => _detectionMode == DetectionMode.Lines;

        public override bool ShowTriangles => _detectionMode == DetectionMode.Triangles;

        #region Inspector

        public override string NameForDisplayPEGI => "Dominant Faces";

        public override string Tooltip {

            get
            {
                switch (_detectionMode)
                {
                    case DetectionMode.Points: return "Click on points to toggle smoothed normal";
                    case DetectionMode.Lines: return "Click on lines to set dominants";
                    default:
                        return (
                            ("Paint the DOMINANCE on triangles {0} It will affect how normal vector will be calculated {0}"
                            +
                            "Alt + N on Triangle to flip normals").F(pegi.EnvironmentNl));
                }

            }
            
    }

#if PEGI
        public override bool Inspect()
        {

            var changed = false;

            var m = MeshMGMT;

            "Will Set {0} On Click".F(_setTo).toggleIcon(ref _setTo).nl(ref changed);

            "Mode".editEnum(50, ref _detectionMode).nl( ref changed);

           /* if ("Auto Bevel".Click())
                AutoAssignDominantNormalsForBeveling();
            "Sensitivity".edit(60, ref Cfg.bevelDetectionSensitivity, 3, 30).nl();

            if ("Sharp All".Click())
            {
                foreach (var vr in EditedMesh.meshPoints)
                    vr.smoothNormal = false;
                EditedMesh.Dirty = true;
                Cfg.newVerticesSmooth = false;
            }

            if ("Smooth All".Click().nl())
            {
                foreach (var vr in EditedMesh.meshPoints)
                    vr.smoothNormal = true;
                EditedMesh.Dirty = true;
                Cfg.newVerticesSmooth = true;
            }*/


            return changed;

        }
        #endif
        #endregion

        public override bool MouseEventPointedTriangle()
        {
            if (EditorInputManager.GetMouseButton(0))
                EditedMesh.Dirty |= PointedTriangle.SetSharpCorners(_setTo);

            return false;
        }

        public override bool MouseEventPointedLine()
        {
            if (EditorInputManager.GetMouseButton(0)) {
                foreach (var t in MeshMGMT.PointedLine.GetAllTriangles())
                    EditedMesh.Dirty |= t.SetSharpCorners(_setTo);
              
            }

            return false;
        }

        public static void AutoAssignDominantNormalsForBeveling()
        {

            foreach (var vr in EditedMesh.meshPoints)
                vr.smoothNormal = true;

            foreach (var t in EditedMesh.triangles) t.SetSharpCorners(true);

            foreach (var t in EditedMesh.triangles)
            {
                var v3S = new Vector3[3];

                for (var i = 0; i < 3; i++)
                    v3S[i] = t.vertexes[i].LocalPos;

                var dist = new float[3];

                for (var i = 0; i < 3; i++)
                    dist[i] = (v3S[(i + 1) % 3] - v3S[(i + 2) % 3]).magnitude;

                for (var i = 0; i < 3; i++)
                {
                    var a = (i + 1) % 3;
                    var b = (i + 2) % 3;
                    if (!(dist[i] < dist[a] / Cfg.bevelDetectionSensitivity) ||
                        !(dist[i] < dist[b] / Cfg.bevelDetectionSensitivity)) continue;
                    
                    t.SetSharpCorners(false);

                    var other = (new LineData(t, t.vertexes[a], t.vertexes[b])).GetOtherTriangle();
                    other?.SetSharpCorners(false);
                }
            }

            EditedMesh.Dirty = true;
        }

        public override void KeysEventPointedTriangle()
        {

            if (KeyCode.N.IsDown())
            {

                if (!EditorInputManager.Alt)
                {
                    var no = PointedTriangle.NumberOf(PointedTriangle.GetClosestTo(MeshMGMT.collisionPosLocal));
                    PointedTriangle.dominantCorner[no] = !PointedTriangle.dominantCorner[no];
                    (PointedTriangle.dominantCorner[no] ? "Triangle edge's Normal is now dominant" : "Triangle edge Normal is NO longer dominant").TeachingNotification();
                }
                else
                {
                    PointedTriangle.InvertNormal();
                    "Flipping Normals".TeachingNotification();
                }

                EditedMesh.Dirty = true;

            }


        }

        public override bool MouseEventPointedVertex()
        {
            if (EditorInputManager.GetMouseButton(0)) {
                var m = MeshMGMT;

                var mp = m.PointedUv.meshPoint;

                if (mp.smoothNormal != EditorInputManager.Alt)
                {
                    mp.smoothNormal = !mp.smoothNormal;
                    EditedMesh.Dirty = true;
                    "N - on Vertex - smooth Normal".TeachingNotification();
                }

               // m.PointedUV.meshPoint.smoothNormal = !m.PointedUV.meshPoint.smoothNormal;
                //EditedMesh.Dirty = true;
               
            }

            return false;
        }


        #region Encode & Decode

        public override CfgEncoder Encode() => new CfgEncoder()
            .Add("dm", (int)_detectionMode);

        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "dm": _detectionMode = (DetectionMode) data.ToInt(); break;
                default: return false;
            }

            return true;
        }

        #endregion

    }

    #endregion

    #region Vertex Smoothing
    public class SmoothingTool : MeshToolBase
    {
        public override string stdTag => "t_vSm";

        private bool _mergeUnMerge;

        private static SmoothingTool _inst;
        
        public SmoothingTool()
        {
            _inst = this;
        }

        public override bool ShowVertices => true;

        public override bool ShowLines => true;

        #region Inspector
        public override string Tooltip => "Click to set vertex as smooth/sharp" + Environment.NewLine;

        public override string NameForDisplayPEGI => "Vertex Smoothing";

#if PEGI
        public override bool Inspect()
        {

            var m = MeshMGMT;

            "OnClick:".write(60);
            if ((_mergeUnMerge ? "Merging (Shift: Unmerge)" : "Smoothing (Shift: Unsmoothing)").Click().nl())
                _mergeUnMerge = !_mergeUnMerge;

            if ("Sharp All".Click())
            {
                foreach (MeshPoint vr in EditedMesh.meshPoints)
                    vr.smoothNormal = false;
                EditedMesh.Dirty = true;
                Cfg.newVerticesSmooth = false;
            }

            if ("Smooth All".Click().nl())
            {
                foreach (var vr in EditedMesh.meshPoints)
                    vr.smoothNormal = true;
                EditedMesh.Dirty = true;
                Cfg.newVerticesSmooth = true;
            }


            if ("All shared".Click())
            {
                EditedMesh.AllVerticesShared();
                EditedMesh.Dirty = true;
                Cfg.newVerticesUnique = false;
            }

            if ("All unique".Click().nl())
            {
                foreach (var t in EditedMesh.triangles)
                    EditedMesh.GiveTriangleUniqueVertices(t);
                Cfg.newVerticesUnique = true;
            }



            return false;

        }
#endif
        #endregion

        public override bool MouseEventPointedTriangle()
        {

            if (EditorInputManager.GetMouseButton(0))
            {
                if (_mergeUnMerge)
                {
                    if (EditorInputManager.Shift)
                        EditedMesh.SetAllVerticesShared(PointedTriangle);
                    else
                        EditedMesh.GiveTriangleUniqueVertices(PointedTriangle);
                }
                else
                    EditedMesh.Dirty |= PointedTriangle.SetSmoothVertices(!EditorInputManager.Shift);
            }

            return false;
        }

        public override bool MouseEventPointedVertex()
        {
            if (EditorInputManager.GetMouseButton(0))
            {
                if (_mergeUnMerge)
                {
                    if (EditorInputManager.Shift)
                        EditedMesh.SetAllUVsShared(PointedVertex); // .SetAllVerticesShared();
                    else
                        EditedMesh.Dirty |= PointedVertex.AllPointsUnique(); //editedMesh.GiveTriangleUniqueVertices(pointedTriangle);
                }
                else
                    EditedMesh.Dirty |= PointedVertex.SetSmoothNormal(!EditorInputManager.Shift);
            }

            return false;
        }

        public override bool MouseEventPointedLine()
        {
            if (EditorInputManager.GetMouseButton(0))
            {
                if (_mergeUnMerge) {
                    if (!EditorInputManager.Shift)
                        EditedMesh.AllVerticesShared(PointedLine);
                    else
                        Dirty |= PointedLine.GiveUniqueVerticesToTriangles();
                }
                else
                    for (var i = 0; i < 2; i++)
                        Dirty |= PointedLine[i].SetSmoothNormal(!EditorInputManager.Shift);
                
            }

            return false;
        }
    }

    #endregion

    /*
    public class VertexAnimationTool : MeshToolBase
    {
        public override string ToString() { return "vertex Animation"; }

        public override void AssignText(MarkerWithText mrkr, vertexpointDta vpoint)
        {

          
                mrkr.textm.text = vpoint.index.ToString();
             
        }

          public void TextureAnim_ToCollider() {
            _PreviewMeshGen.CopyFrom(_Mesh);
            _PreviewMeshGen.AddTextureAnimDisplacement();
            MeshConstructor con = new MeshConstructor(_Mesh, target.meshProfile, null);
            con.AssignMeshAsCollider(target.meshCollider );
        }

        public override void MouseEventPointedVertex()
        {
          
            if (meshMGMT.pointedUV == null) return;
            if (EditorInputManager.GetMouseButtonDown(1))
            {
                meshMGMT.draggingSelected = true;
                meshMGMT.dragDelay = 0.2f;
            }
        }

        public override bool showGrid { get { return true; } }

        public override void ManageDragging()
        {
            if ((EditorInputManager.GetMouseButtonUp(1)) || (EditorInputManager.GetMouseButton(1) == false))
            {
                meshMGMT.draggingSelected = false;
                mesh.dirty = true;

            }
            else
            {
                meshMGMT.dragDelay -= Time.deltaTime;
                if ((meshMGMT.dragDelay < 0) || (Application.isPlaying == false))
                {
                    if (meshMGMT.selectedUV == null) { meshMGMT.draggingSelected = false; Debug.Log("no selected"); return; }
                    if ((GridNavigator.inst().angGridToCamera(GridNavigator.onGridPos) < 82) &&
                               meshMGMT.target.AnimatedVertices())
                                    meshMGMT.selectedUV.vert.AnimateTo(meshMGMT.onGridLocal);
                                
                    
                        
                    
                }
            }
        }

    }
    */

    #region Color
    public class VertexColorTool : MeshToolBase
    {
        public override string stdTag => "t_vCol";

        public static VertexColorTool inst;

        public override Color VertexColor => Color.white;

        public int selectedSubMesh = 0;

        #region Inspector
        public override string Tooltip => (" Vertex Color {0} 1. Make sure mesh Profile has Color enabled {0}" +
                                          "2. Only if shader outputs vertex color, changes will be visible. " +
                                          "1234 on Line - apply RGBA for Border.").F(pegi.EnvironmentNl);

        public override string NameForDisplayPEGI => "vertex Color";

        #if PEGI
        public override bool Inspect()
        {
            var changed = false;
            
            var em = EditedMesh;
            var br = Cfg.brushConfig;
            var col = br.Color;
            var p = InspectedPainter;

            if (("Paint All with Brush Color").Click().nl(ref changed))
                em.PaintAll(br.colorLinear);

            "Make Vertex Unique On Paint".toggleIcon(ref Cfg.makeVerticesUniqueOnEdgeColoring).nl(ref changed);
            
            if (em.subMeshCount > 1)
            {

                var cnt = em.subMeshCount;
                var mats = InspectedPainter.Materials;

                var nms = new string[cnt];
                for (var i = 0; i < cnt; i++)
                    nms[i] = "{0}: {1}".F(i, mats.TryGet(i));

                "Color Sub Mesh".select(90, ref selectedSubMesh, nms).changes(ref changed);

                if (selectedSubMesh < em.subMeshCount && "Apply".Click()) 
                    em.ColorSubMesh(selectedSubMesh, col);

                pegi.nl();

            }
            
            //if (pegi.edit(ref col).nl(ref changed))
              //  br.Color = col;
            
            br.ColorSliders().nl();
            
            if (!p.MeshProfile.UsesColor)
                "Selected Mesh Profile does not appear to be using Color".writeWarning();
            if (!p.MeshProfile.WritesColor)
                "Selected Mesh Profile doesn't write to Color.".writeHint();

            return changed;
        }
        #endif
        #endregion

        public override bool MouseEventPointedVertex()
        {
            var m = MeshMGMT;

            var bcf = GlobalBrush;

            //if (EditorInputManager.GetMouseButtonDown(1))
            //  m.pointedUV.vert.clearColor(cfg.brushConfig.mask);

            if ((EditorInputManager.GetMouseButtonDown(0)))
            {
                if (EditorInputManager.Control)

                    bcf.mask.Transfer(ref m.PointedUv.color, bcf.Color);

                else
                    foreach (var uvi in m.PointedUv.meshPoint.vertices)
                        bcf.mask.Transfer(ref uvi.color, Cfg.brushConfig.Color);

                EditedMesh.dirtyColor = true;
            }

            return false;
        }

        public override bool MouseEventPointedLine()
        {
            if (!EditorInputManager.GetMouseButton(0)) return false;
            
            if (PointedLine.SameAsLastFrame)
                return true;

            var bcf = Cfg.brushConfig;

            var a = PointedLine.points[0];
            var b = PointedLine.points[1];

            var c = bcf.Color;

            a.meshPoint.SetColorOnLine(c, bcf.mask, b.meshPoint);//setColor(glob.colorSampler.color, glob.colorSampler.mask);
            b.meshPoint.SetColorOnLine(c, bcf.mask, a.meshPoint);
            EditedMesh.dirtyColor = true;
            return true;
        }

        public override bool MouseEventPointedTriangle()
        {
            if (!EditorInputManager.GetMouseButton(0)) return false;
            
            if (PointedTriangle.SameAsLastFrame)
                return true;

            var bcf = Cfg.brushConfig;

            var c = bcf.Color;

            foreach (var u in PointedTriangle.vertexes)
            foreach (var vuv in u.meshPoint.vertices)
                bcf.mask.Transfer(ref vuv.color, c);

            EditedMesh.dirtyColor = true;
            return true;
        }

        public override void KeysEventPointedLine()
        {
            var a = PointedLine.points[0];
            var b = PointedLine.points[1];

            var ind = Event.current.NumericKeyDown();

            if ((ind > 0) && (ind < 5))
            {
                a.meshPoint.FlipChanelOnLine((ColorChanel)(ind - 1), b.meshPoint);
                EditedMesh.Dirty = true;
                Event.current.Use();
            }

        }

        public VertexColorTool()
        {
            inst = this;
        }

        #region Encode & Decode

        public override CfgEncoder Encode() => new CfgEncoder()
            .Add_IfNotZero("sm", selectedSubMesh);

        public override bool Decode(string tg, string data) {

            switch (tg) {
                case "sm": selectedSubMesh = data.ToInt(); break;
                default: return false;
            }

            return true;
        }

        #endregion

    }
    #endregion

    #region Edges (Experimental tech)
    public class VertexEdgeTool : MeshToolBase {

        public override string stdTag => "t_edgs";

        private static bool _alsoDoColor;
        private static bool _editingFlexibleEdge;
        private static float _edgeValue = 1;

        private static float ShiftInvertedValue => EditorInputManager.Shift ? 1f - _edgeValue : _edgeValue;
        
        public override bool ShowTriangles => false;

        public override bool ShowVertices => !_editingFlexibleEdge;

        #region Inspector
        
        public override string NameForDisplayPEGI => "vertex Edge";
        
        public override string Tooltip =>
             "Shift - invert edge value" + Environment.NewLine +
             "This tool allows editing value if edge.w" + Environment.NewLine + "edge.xyz is used to store border information about the triangle. ANd the edge.w in Example shaders is used" +
             "to mask texture color with vertex color. All vertices should be set as Unique for this to work." +
             "Line is drawn between vertices marked with line strength 1. A triangle can't have only 2 sides with Edge: it's ether side, or all 3 (2 points marked to create a line, or 3 points to create 3 lines).";


        #if PEGI

        public override bool Inspect()
        {
            var changed = false;
            "Edge Strength: ".edit(ref _edgeValue).nl(ref changed);
            "Also do color".toggle(ref _alsoDoColor).nl(ref changed);

            if (_alsoDoColor)
               GlobalBrush.ColorSliders().nl(ref changed);
            
            "Flexible Edge".toggleIcon("Edge type can be seen in Packaging profile (if any). Only Bevel shader doesn't have a Flexible edge.", ref _editingFlexibleEdge);

            return changed;
        }
        #endif
        #endregion

        public override bool MouseEventPointedVertex()
        {
            if ((!EditorInputManager.GetMouseButton(0))) return false;
            
            #if PEGI
            if (!PointedUv.meshPoint.AllPointsUnique())
                "Shared points found, Edge requires All Unique".showNotificationIn3D_Views();
            #endif
            
            if (EditorInputManager.Control)
            {
                _edgeValue = MeshMGMT.PointedUv.meshPoint.edgeStrength;
                if (_alsoDoColor) GlobalBrush.Color = PointedUv.color;
            }
            else
            {

                if (PointedUv.SameAsLastFrame)
                    return true;

                MeshMGMT.PointedUv.meshPoint.edgeStrength = ShiftInvertedValue;
                if (_alsoDoColor)
                {
                    var col = GlobalBrush.Color;
                    foreach (var uvi in PointedUv.meshPoint.vertices)
                        GlobalBrush.mask.Transfer(ref uvi.color, col);
                }
                EditedMesh.Dirty = true;

                return true;
            }
            return false;
        }

        public override bool MouseEventPointedLine()
        {
            if (!EditorInputManager.GetMouseButton(0)) return false;
            
            var vrtA = PointedLine.points[0].meshPoint;
            var vrtB = PointedLine.points[1].meshPoint;

            if (EditorInputManager.Control)
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

        public static void PutEdgeOnLine(LineData ld)
        {

            var vrtA = ld.points[0].meshPoint;
            var vrtB = ld.points[1].meshPoint;

            var tris = ld.GetAllTriangles();

            foreach (var t in tris)
                t.edgeWeight[t.GetIndexOfNoOneIn(ld)] = ShiftInvertedValue;// true;

            var edValA = ShiftInvertedValue;
            var edValB = ShiftInvertedValue;

            if (_editingFlexibleEdge)
            {

                foreach (var uv in vrtA.vertices)
                    foreach (var t in uv.triangles)
                    {
                        var opposite = t.NumberOf(uv);
                        for (var i = 0; i < 3; i++)
                            if (opposite != i)
                                edValA = Mathf.Max(edValA, t.edgeWeight[i]);
                    }

                foreach (var uv in vrtB.vertices)
                    foreach (var t in uv.triangles)
                    {
                        var opposite = t.NumberOf(uv);
                        for (var i = 0; i < 3; i++)
                            if (opposite != i)
                                edValB = Mathf.Max(edValB, t.edgeWeight[i]);
                    }
            }

            vrtA.edgeStrength = edValA;
            vrtB.edgeStrength = edValB;

            if (_alsoDoColor)
            {
                var col = GlobalBrush.Color;
                foreach (var uvi in vrtA.vertices)
                    GlobalBrush.mask.Transfer(ref uvi.color, col);
                foreach (var uvi in vrtB.vertices)
                    GlobalBrush.mask.Transfer(ref uvi.color, col);
            }

            EditedMesh.Dirty = true;

        }

        #region Encode & Decode
        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "v": _edgeValue = data.ToFloat(); break;
                case "doCol": _alsoDoColor = data.ToBool(); break;
                case "fe": _editingFlexibleEdge = data.ToBool(); break;
                default: return false;
            }
            return true;
        }

        public override CfgEncoder Encode() => new CfgEncoder()
            .Add("v", _edgeValue)
            .Add_Bool("doCol", _alsoDoColor)
            .Add_Bool("fe", _editingFlexibleEdge);
        #endregion

    }
    #endregion

    #region Submesh
    public class TriangleSubMeshTool : MeshToolBase
    {
        public override string stdTag => "t_sbmsh";

        private int _curSubMesh;
        
        public override bool ShowVertices => false;

        public override bool ShowLines => false;
        
        public override bool MouseEventPointedTriangle()
        {

            if (EditorInputManager.GetMouseButtonDown(0) && EditorInputManager.Control)
            {
                _curSubMesh = MeshMGMT.PointedTriangle.subMeshIndex;
#if PEGI
                ("SubMesh " + _curSubMesh).showNotificationIn3D_Views();
#endif
            }

            if (!EditorInputManager.GetMouseButton(0) || EditorInputManager.Control ||
                (MeshMGMT.PointedTriangle.subMeshIndex == _curSubMesh)) return false;
            
            if (PointedTriangle.SameAsLastFrame)
                return true;
            
            MeshMGMT.PointedTriangle.subMeshIndex = _curSubMesh;
            EditedMesh.subMeshCount = Mathf.Max(MeshMGMT.PointedTriangle.subMeshIndex + 1, EditedMesh.subMeshCount);
            EditedMesh.Dirty = true;
            
            return true;
        }

        #region Inspector

        public override string Tooltip => "Ctrl+LMB - sample" + Environment.NewLine + "LMB on triangle - set sub mesh";
        public override string NameForDisplayPEGI => "triangle Sub Mesh index";
        
        #if PEGI

        public override bool Inspect() {
            "Sub Mesh: ".select(60, ref _curSubMesh, 0, EditedMesh.subMeshCount).nl();
            
            if ("Make all 0".Click().nl())
                EditedMesh.SubMeshIndex = 0;

            if ("Delete Sub Mesh".Click())
            {


                EditedMesh.Dirty = true;
            }

            return false;
        }
        #endif
        #endregion

        #region Encode & Decode
        public override CfgEncoder Encode()
        {
            var cody = new CfgEncoder();
            if (_curSubMesh != 0)
                cody.Add("sm", _curSubMesh);
            return cody;
        }

        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "sm": _curSubMesh = data.ToInt(); break;
                default: return false;
            }
            return true;

        }
        #endregion
    }
    #endregion
    
    public class VertexGroupTool : MeshToolBase
    {

        public override string stdTag => "t_vrtGr";

        public override string NameForDisplayPEGI => "Vertex Group";
    }
}