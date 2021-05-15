using System;
using System.Collections.Generic;
using System.IO;
using QuizCanners.Inspect;
using QuizCanners.CfgDecode;
using QuizCanners.Utils;
using UnityEngine;

namespace PlaytimePainter.MeshEditing {

    public interface IMeshToolWithPerMeshData {
        CfgEncoder EncodePerMeshData();
    }

    #region Base
    public class MeshToolBase : PainterClassCfg, IPEGI, IGotDisplayName
    {

        protected enum DetectionMode { Points, Lines, Triangles }

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
            if (!_allTools.IsNullOrEmpty() || applicationIsQuitting) 
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
        protected static PP_MeshData GetPreviewMesh
        {
            get
            {
                if (MeshEditorManager.previewEdMesh == null)
                {
                    MeshEditorManager.previewEdMesh = new PP_MeshData();
                    MeshEditorManager.previewEdMesh.Decode(new CfgData(EditedMesh.Encode().ToString()));
                    //Debug.Log("Recreating preview");
                }
                return MeshEditorManager.previewEdMesh;
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

        public override void Decode(string key, CfgData data)
        { }
        #endregion

        #region Inspector
        public virtual string Tooltip => "No toolTip";

        public virtual string NameForDisplayPEGI()=> " No Name ";

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

        public override string NameForDisplayPEGI()=> "Vertex Smoothing";
        
       public override void Inspect()
        {

           // var m = MeshMGMT;

            "OnClick:".write(60);
            if ((_mergeUnMerge ? "Merging (Shift: Unmerge)" : "Smoothing (Shift: Unsmoothing)").Click().nl())
                _mergeUnMerge = !_mergeUnMerge;

            if ("Sharp All".Click())
            {
                foreach (PainterMesh.MeshPoint vr in EditedMesh.meshPoints)
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

        }

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


    #region Submesh
    public class TriangleSubMeshTool : MeshToolBase
    {
        public override string StdTag => "t_sbmsh";

        private int _curSubMesh;
        
        public override bool ShowVertices => false;

        public override bool ShowLines => false;
        
        public override bool MouseEventPointedTriangle()
        {

            if (EditorInputManager.GetMouseButtonDown(0) && EditorInputManager.Control)
            {
                _curSubMesh = MeshEditorManager.PointedTriangle.subMeshIndex;
                pegi.GameView.ShowNotification("SubMesh " + _curSubMesh);
            }

            if (!EditorInputManager.GetMouseButton(0) || EditorInputManager.Control ||
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
        public override string NameForDisplayPEGI()=> "triangle Sub Mesh index";

       public override void Inspect() {
            "Sub Mesh: ".select(60, ref _curSubMesh, 0, EditedMesh.subMeshCount).nl();
            
            if ("Make all 0".Click().nl())
                EditedMesh.SubMeshIndex = 0;

            if ("Delete Sub Mesh".Click())
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

        public override void Decode(string key, CfgData data)
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

        public override string NameForDisplayPEGI()=> "Vertex Group";
    }
}