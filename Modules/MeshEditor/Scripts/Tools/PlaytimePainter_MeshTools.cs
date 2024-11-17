using System;
using System.Collections.Generic;
using QuizCanners.Inspect;
using QuizCanners.Migration;
using QuizCanners.Utils;
using UnityEngine;

namespace PainterTool.MeshEditing {

    public interface IMeshToolWithPerMeshData {
        CfgEncoder EncodePerMeshData();
    }

    #region Base
    public class MeshToolBase : PainterClassCfg, IPEGI
    {
        protected enum DetectionMode { All, Points, Lines, Triangles }

        protected DetectionMode _detectionMode = DetectionMode.All;

        protected void InspectDetectionMode()
        {
            Option(DetectionMode.Points, Icon.State);
            Option(DetectionMode.Lines, Icon.Link);
            Option(DetectionMode.Triangles, Icon.StateMachine);

            void Option(DetectionMode mode, Icon icon)
            {
                if (_detectionMode == mode)
                    icon.Draw(mode.ToString());
                else if (icon.Click(mode.ToString()))
                    _detectionMode = mode;
            }
        }

        public virtual string StdTag => "t_noStd";

        protected static bool Dirty { get { return EditedMesh.Dirty; } set { EditedMesh.Dirty = value; } }

        protected virtual void SetShaderKeyword(bool enablePart) { }

        public virtual void SetShaderKeywords()  {

            foreach (var t in AllTools)
                t.SetShaderKeyword(false);

            SetShaderKeyword(true);

        }

        public static List<IMeshToolWithPerMeshData> allToolsWithPerMeshData;

        private static List<MeshToolBase> _allTools;


        public static void InitIfNotInited()
        {
            if (!_allTools.IsNullOrEmpty()) 
                return;
                
            _allTools = new List<MeshToolBase>();
            allToolsWithPerMeshData = new List<IMeshToolWithPerMeshData>();
                      
            _allTools.Add(new VertexPositionTool());
            _allTools.Add(new SharpFacesTool());
            _allTools.Add(new VertexColorTool());
            _allTools.Add(new VertexEdgeTool());
            _allTools.Add(new TriangleAtlasTool());
            _allTools.Add(new TriangleSubMeshTool());
            _allTools.Add(new VertexUVTool());

            foreach (var t in _allTools)
                allToolsWithPerMeshData.TryAdd(t as IMeshToolWithPerMeshData);
        }
        public static List<MeshToolBase> AllTools
        {
            get
            {
                InitIfNotInited();
                return _allTools;
            }
        }
        
        protected static PainterMesh.LineData PointedLine => MeshEditorManager.PointedLine;
        protected static PainterMesh.Triangle PointedTriangle => MeshEditorManager.PointedTriangle;
        protected PainterMesh.Triangle SelectedTriangle => MeshEditorManager.SelectedTriangle;
        protected static PainterMesh.Vertex PointedUv => MeshEditorManager.PointedUv;
        protected static PainterMesh.Vertex SelectedUv => MeshEditorManager.SelectedUv;
        protected static PainterMesh.MeshPoint PointedVertex => MeshEditorManager.PointedUv.meshPoint;
        internal static MeshData GetPreviewMesh
        {
            get
            {
                if (MeshEditorManager.previewEdMesh == null)
                {
                    MeshEditorManager.previewEdMesh = new MeshData();
                    MeshEditorManager.previewEdMesh.Decode(new CfgData(EditedMesh.Encode().ToString()));
                    //Debug.Log("Recreating preview");
                }
                return MeshEditorManager.previewEdMesh;
            }
        }


        public virtual bool ShowVertices => _detectionMode == DetectionMode.Points || _detectionMode == DetectionMode.All;
        public virtual bool ShowLines => _detectionMode == DetectionMode.Lines || _detectionMode == DetectionMode.All;
        public virtual bool ShowTriangles => _detectionMode == DetectionMode.Triangles || _detectionMode == DetectionMode.All;

        public virtual bool ShowGrid => false;
        public virtual bool ShowSelectedVertex => false;
        public virtual bool ShowSelectedLine => false;
        public virtual bool ShowSelectedTriangle => false;

        public virtual Color VertexColor => Color.gray;

        public virtual void OnSelectTool() { }
        public virtual void OnDeSelectTool() { }
        public virtual void OnGridChange() { }

        public virtual void AssignText(MarkerWithText markers, PainterMesh.MeshPoint point) => markers.textm.text = "";

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

        public override void DecodeTag(string key, CfgData data)
        { }
        #endregion

        #region Inspector
        public virtual string Tooltip => "No toolTip";

        public override string ToString() => " No Name ";

        public virtual void Inspect() { }
        #endregion
    }
    #endregion

    #region Vertex Smoothing
    public class SmoothingTool : MeshToolBase
    {
        public override string StdTag => "t_vSm";

        public bool _mergeUnMerge;

        public override bool ShowVertices => true;

        public override bool ShowLines => true;

        #region Inspector
        public override string Tooltip => "Click to set vertex as smooth/sharp" + Environment.NewLine;

        public override string ToString() => "Vertex Smoothing";
        
       public override void Inspect()
        {

           // var m = MeshMGMT;

            "OnClick:".ConstLabel().Write();
            if ((_mergeUnMerge ? "Merging (Shift: Unmerge)" : "Smoothing (Shift: Unsmoothing)").PegiLabel().Click().Nl())
                _mergeUnMerge = !_mergeUnMerge;

            if ("Sharp All".PegiLabel().Click())
            {
                foreach (PainterMesh.MeshPoint vr in EditedMesh.meshPoints)
                    vr.smoothNormal = false;
                EditedMesh.Dirty = true;
                Painter.Data.newVerticesSmooth = false;
            }

            if ("Smooth All".PegiLabel().Click().Nl())
            {
                foreach (var vr in EditedMesh.meshPoints)
                    vr.smoothNormal = true;
                EditedMesh.Dirty = true;
                Painter.Data.newVerticesSmooth = true;
            }


            if ("All shared".PegiLabel().Click())
            {
                EditedMesh.AllVerticesShared();
                EditedMesh.Dirty = true;
                Painter.Data.newVerticesUnique = false;
            }

            if ("All unique".PegiLabel().Click().Nl())
            {
                foreach (var t in EditedMesh.triangles)
                    EditedMesh.GiveTriangleUniqueVertices(t);
                Painter.Data.newVerticesUnique = true;
            }

        }

        #endregion

        public override bool MouseEventPointedTriangle()
        {

            if (PlaytimePainter_EditorInputManager.GetMouseButton(0))
            {
                if (_mergeUnMerge)
                {
                    if (PlaytimePainter_EditorInputManager.Shift)
                        EditedMesh.SetAllVerticesShared(PointedTriangle);
                    else
                        EditedMesh.GiveTriangleUniqueVertices(PointedTriangle);
                }
                else
                    EditedMesh.Dirty |= PointedTriangle.SetSmoothVertices(!PlaytimePainter_EditorInputManager.Shift);
            }

            return false;
        }

        public override bool MouseEventPointedVertex()
        {
            if (PlaytimePainter_EditorInputManager.GetMouseButton(0))
            {
                if (_mergeUnMerge)
                {
                    if (PlaytimePainter_EditorInputManager.Shift)
                        EditedMesh.SetAllUVsShared(PointedVertex); // .SetAllVerticesShared();
                    else
                        EditedMesh.Dirty |= PointedVertex.AllPointsUnique(); //editedMesh.GiveTriangleUniqueVertices(pointedTriangle);
                }
                else
                    EditedMesh.Dirty |= PointedVertex.TryChangeSmoothNormal(!PlaytimePainter_EditorInputManager.Shift);
            }

            return false;
        }

        public override bool MouseEventPointedLine()
        {
            if (PlaytimePainter_EditorInputManager.GetMouseButton(0))
            {
                if (_mergeUnMerge) {
                    if (!PlaytimePainter_EditorInputManager.Shift)
                        EditedMesh.AllVerticesShared(PointedLine);
                    else
                        Dirty |= PointedLine.GiveUniqueVerticesToTriangles();
                }
                else
                    for (var i = 0; i < 2; i++)
                        Dirty |= PointedLine[i].TryChangeSmoothNormal(!PlaytimePainter_EditorInputManager.Shift);
                
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


    #region Submesh
    public class TriangleSubMeshTool : MeshToolBase
    {
        public override string StdTag => "t_sbmsh";

        private int _curSubMesh;
        
        public override bool ShowVertices => false;

        public override bool ShowLines => false;
        
        public override bool MouseEventPointedTriangle()
        {

            if (PlaytimePainter_EditorInputManager.GetMouseButtonDown(0) && PlaytimePainter_EditorInputManager.Control)
            {
                _curSubMesh = MeshEditorManager.PointedTriangle.subMeshIndex;
                pegi.GameView.ShowNotification("SubMesh " + _curSubMesh);
            }

            if (!PlaytimePainter_EditorInputManager.GetMouseButton(0) || PlaytimePainter_EditorInputManager.Control ||
                (MeshEditorManager.PointedTriangle.subMeshIndex == _curSubMesh)) return false;
            
            if (PointedTriangle.SameAsLastFrame)
                return true;
            
            MeshEditorManager.PointedTriangle.subMeshIndex = _curSubMesh;
            EditedMesh.subMeshCount = Mathf.Max(MeshEditorManager.PointedTriangle.subMeshIndex + 1, EditedMesh.subMeshCount);
            EditedMesh.Dirty = true;
            
            return true;
        }

        #region Inspector

        public override string Tooltip => "Ctrl+LMB - sample" + Environment.NewLine + "LMB on triangle - set sub mesh";
        public override string ToString() => "triangle Sub Mesh index";

       public override void Inspect() {
            "Sub Mesh".ConstLabel().Select(ref _curSubMesh, 0, EditedMesh.subMeshCount).Nl();
            
            if ("Make all 0".PegiLabel().Click().Nl())
                EditedMesh.SubMeshIndex = 0;

            if ("Delete Sub Mesh".PegiLabel().Click())
            {
                EditedMesh.Dirty = true;
            }
        }

        #endregion

        #region Encode & Decode
        public override CfgEncoder Encode()
        {
            var cody = new CfgEncoder();
            if (_curSubMesh != 0)
                cody.Add("sm", _curSubMesh);
            return cody;
        }

        public override void DecodeTag(string key, CfgData data)
        {
            switch (key)
            {
                case "sm": data.ToInt(ref _curSubMesh); break;
            }
        }
        #endregion
    }
    #endregion
    
    public class VertexGroupTool : MeshToolBase
    {

        public override string StdTag => "t_vrtGr";

        public override string ToString() => "Vertex Group";
    }
}