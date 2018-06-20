using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;

using SharedTools_Stuff;

namespace Playtime_Painter
{

    public abstract class MeshToolBase : PainterStuff_STD {

        public delegate bool meshToolPlugBool(MeshToolBase tool, out bool val);
        public static meshToolPlugBool showVerticesPlugs;

        protected static bool dirty { get { return editedMesh.dirty; } set { editedMesh.dirty = value; } }

        static List<MeshToolBase> _allTools;

        public static List<MeshToolBase> allTools
        { get {
                if (_allTools == null && !applicationIsQuitting) {
                    _allTools = new List<MeshToolBase>();

                    var tps = CsharpFuncs.GetAllChildTypesOf<MeshToolBase>();

                    foreach (var t in tps)
                        _allTools.Add((MeshToolBase)Activator.CreateInstance(t));

                    /*_allTools.Add(new VertexPositionTool());
                    _allTools.Add(new SharpFacesTool());
                    _allTools.Add(new VertexColorTool());
                    _allTools.Add(new VertexEdgeTool());
                    _allTools.Add(new TriangleAtlasTool());
                    _allTools.Add(new TriangleSubmeshTool());
                    _allTools.Add(new VertexShadowTool());*/
                }
                return _allTools;
            } }
        
        //protected MeshManager mgmt { get { return MeshManager.inst; } }
       // protected PainterConfig cfg { get { return PainterConfig.inst; } }
        protected LineData pointedLine { get { return meshMGMT.PointedLine; } }
        protected Triangle pointedTris { get { return meshMGMT.PointedTris; } }
        protected Triangle selectedTris { get { return meshMGMT.SelectedTris; } }
        protected Vertex pointedUV { get { return meshMGMT.PointedUV; } }
        protected Vertex selectedUV { get { return meshMGMT.SelectedUV; } }
        protected MeshPoint pointedVertex { get { return meshMGMT.PointedUV.meshPoint; } }
        protected EditableMesh freshPreviewMesh {  get { if (meshMGMT.previewMesh == null)  {
                    meshMGMT.previewEdMesh = new EditableMesh();
                    meshMGMT.previewEdMesh.Decode(meshMGMT.edMesh.Encode());
                }
                return meshMGMT.previewEdMesh;
            }
        }


        public virtual bool showVerticesDefault { get { return true; } }

        public bool showVertices { get  {
                if (showVerticesPlugs != null)  {
                    bool val;
                    foreach (meshToolPlugBool p in showVerticesPlugs.GetInvocationList())
                        if (p(this, out val))
                            return val;
                }
                return showVerticesDefault;
            }
        }

        public virtual bool showLines { get { return true; } }
        public virtual bool showTriangles { get { return true; } }

        public virtual bool showGrid { get { return false; } }

        public virtual bool showSelectedVerticle { get { return false; } }
        public virtual bool showSelectedLine { get { return false; } }
        public virtual bool showSelectedTriangle { get { return false; } }

        public virtual string tooltip { get { return "No tooltippp"; } }

        public virtual Color vertColor {get { return Color.gray;  }  }

        public virtual void OnSelectTool() { }

        public virtual void OnDeSelectTool() { }

        public virtual void OnGridChange() { }

        public virtual void AssignText(MarkerWithText mrkr, MeshPoint vpoint) {
           mrkr.textm.text = "";
        }

        public virtual bool MouseEventPointedVertex() { return false; }

        public virtual bool MouseEventPointedLine() { return false; }

        public virtual bool MouseEventPointedTriangle() { return false; }

        public virtual void MouseEventPointedNothing() {  }

        public virtual void KeysEventPointedVertex()  {  }

        public virtual void KeysEventDragging() { }

        public virtual void KeysEventPointedLine()  { }

        public virtual void KeysEventPointedTriangle()  { }

        public virtual void ManageDragging() { }

        public override string GetDefaultTagName() {
            return ToString();
        }

        public override StdEncoder Encode() {
            return null;
        }

        public override bool Decode(string tag, string data) {
            return true;
        }
    }

    public class VertexPositionTool : MeshToolBase
    {
        public static VertexPositionTool inst;

        public override string ToString()  {return "ADD & MOVE"; }

        public VertexPositionTool() {
            inst = this;
        }

        //  Vector3 displace = new Vector3();

        //enum meshElement { vertex, triangle, line }

        public bool addToTrianglesAndLines = false;

        List<MeshPoint> draggedVertices = new List<MeshPoint>();
        Vector3 originalPosition;

        Vector3 offset;
        
        public override bool showGrid { get { return true; } }
  
        public override void AssignText(MarkerWithText mrkr, MeshPoint vpoint)
        {

            var pvrt = meshMGMT.GetSelectedVert();

            if ((vpoint.uvpoints.Count > 1) || (pvrt == vpoint))
            {

                //Texture tex = meshMGMT.target.GetTextureOnMaterial();//meshRenderer.sharedMaterial.mainTexture;

                if (pvrt == vpoint)
                {
                    mrkr.textm.text = (vpoint.uvpoints.Count > 1) ? ((vpoint.uvpoints.IndexOf(meshMGMT.SelectedUV) + 1).ToString() + "/" + vpoint.uvpoints.Count.ToString() +
                        (vpoint.SmoothNormal ? "s" : "")) : "";
                  

                }
                else
                    mrkr.textm.text = vpoint.uvpoints.Count.ToString() +
                        (vpoint.SmoothNormal ? "s" : "");
            }
            else mrkr.textm.text = "";
        }

        public override Color vertColor
        {
            get
            {
                return Color.white; 
            }
        }

        public override string tooltip
        {
            get
            {
                return

                    "Alt - Raycast To Grid" + Environment.NewLine +
                    "LMB - Add Vertices/Make Triangles (Go Clockwise), Drag" + Environment.NewLine +
                    "Scroll - Change Plane" + Environment.NewLine +
                    "U - make triengle unique." + Environment.NewLine+
                    "M - merge with nearest while dragging ";
            }
        }
        #if PEGI
        public override bool PEGI() {

            bool changed = false;

            MeshManager mgm = meshMGMT;

            PainterConfig sd = PainterConfig.inst;

            if (mgm.MeshTool.showGrid) {
                "Snap to grid:".toggle(100, ref sd.SnapToGrid);

                if (sd.SnapToGrid)
                    "size:".edit(40, ref sd.SnapToGridSize);
            }

            pegi.newLine();

            "Pixel-Perfect".toggle("New vertex will have UV coordinate rounded to half a pixel.",120, ref cfg.pixelPerfectMeshEditing).nl();

            "Add into mesh".toggle("Will split triangles and edges by inserting vertices", 120, ref addToTrianglesAndLines).nl();

            "Add Smooth:".toggle(70, ref cfg.newVerticesSmooth);
            if (pegi.Click("Sharp All")) {
                foreach (MeshPoint vr in meshMGMT.edMesh.vertices)
                    vr.SmoothNormal = false;
                mgm.edMesh.dirty = true;
                cfg.newVerticesSmooth = false;
            }

            if (pegi.Click("Smooth All").nl()) {
                foreach (MeshPoint vr in meshMGMT.edMesh.vertices)
                    vr.SmoothNormal = true;
                mgm.edMesh.dirty = true;
                cfg.newVerticesSmooth = true;
            }

            "Add Unique:".toggle(70, ref cfg.newVerticesUnique);
            if (pegi.Click("All shared")) {
                mgm.edMesh.AllVerticesShared();
                mgm.edMesh.dirty = true;
                cfg.newVerticesUnique = false;
            }

            if (pegi.Click("All unique"))
            {
                foreach (Triangle t in editedMesh.triangles)
                    mgm.edMesh.GiveTriangleUniqueVerticles(t);
                mgm.edMesh.dirty = true;
                cfg.newVerticesUnique = true;
            }
            pegi.newLine();


            if ("Auto Bevel".Click())
                SharpFacesTool.inst.AutoAssignDominantNormalsForBeveling();
            "Sensitivity".edit(60, ref cfg.bevelDetectionSensetivity, 3, 30).nl();

            if ("Offset".foldout())
            {
                "center".edit(ref offset).nl();
                if ("Modify".Click().nl())
                {
                    foreach (var v in editedMesh.vertices)
                        v.localPos += offset;

                    offset = -offset;

                    dirty = true;

                }

                if ("Auto Center".Click().nl())
                {
                    Vector3 avr = Vector3.zero;
                    foreach (var v in editedMesh.vertices)
                        avr += v.localPos;

                    offset = - avr / editedMesh.vertices.Count;
                }

            }
            /*
            if (pegi.Click("Mirror by Center")) {
                GridNavigator.onGridPos = mgm.target.transform.position;
                mgm.UpdateLocalSpaceV3s();
                mgm.edMesh.MirrorVerticlesAgainsThePlane(mgm.onGridLocal);
            }

            if (pegi.Click("Mirror by Plane")) {
                mgm.UpdateLocalSpaceV3s();
                mgm.edMesh.MirrorVerticlesAgainsThePlane(mgm.onGridLocal);
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
            pegi.newLine();
            */
            return changed;
        }
#endif
        public override void KeysEventDragging()
        {
            var m = meshMGMT;
            if ((KeyCode.M.IsDown()) && (m.SelectedUV != null))
            {
                m.SelectedUV.meshPoint.MergeWithNearest();
                m.Dragging = false;
                m.edMesh.dirty = true;
                #if PEGI
                "M - merge with nearest".TeachingNotification();
#endif
            }
        }

        public override void KeysEventPointedVertex()
        {

            var m = meshMGMT;

            if (KeyCode.Delete.IsDown())
            {
                //Debug.Log("Deleting");
                if (!EditorInputManager.getControlKey())
                {
                    if (m.PointedUV.meshPoint.uvpoints.Count == 1)
                    {
                        if (!m.DeleteVertHEAL(meshMGMT.PointedUV.meshPoint))
                            m.DeleteUv(meshMGMT.PointedUV);
                    }
                    else
                        while (m.PointedUV.meshPoint.uvpoints.Count > 1)
                            m.edMesh.MoveTris(m.PointedUV.meshPoint.uvpoints[1], m.PointedUV.meshPoint.uvpoints[0]); 
                    

                }
                else
                    m.DeleteUv(m.PointedUV);
                m.edMesh.dirty = true;
            }
        }

        public override void KeysEventPointedTriangle()
        {
            if (KeyCode.Backspace.IsDown() || KeyCode.Delete.IsDown())
            {
                meshMGMT.edMesh.triangles.Remove(pointedTris);
                foreach (var uv in pointedTris.vertexes)
                    if (uv.meshPoint.uvpoints.Count == 1 && uv.tris.Count == 1)
                        editedMesh.vertices.Remove(uv.meshPoint);

                meshMGMT.edMesh.dirty = true;
                return;
            }

            if (KeyCode.U.IsDown())
                pointedTris.MakeTriangleVertUnique(pointedUV);

          /*  if (KeyCode.N.isDown())
            {

                if (!EditorInputManager.getAltKey())
                {
                    int no = pointedTris.NumberOf(pointedTris.GetClosestTo(meshMGMT.collisionPosLocal));
                    pointedTris.SharpCorner[no] = !pointedTris.SharpCorner[no];

                    (pointedTris.SharpCorner[no] ? "Triangle edge's Normal is now dominant" : "Triangle edge Normal is NO longer dominant").TeachingNotification();
                }
                else
                {
                    pointedTris.InvertNormal();
                    "Inverting Normals".TeachingNotification();
                }

                meshMGMT.edMesh.dirty = true;

            }*/


        }

        public override bool MouseEventPointedVertex() {
           
            if (EditorInputManager.GetMouseButtonDown(0)) {
                var m = meshMGMT;

                if ((m.TrisVerts < 3) && (m.PointedUV != null) && (!m.IsInTrisSet(m.PointedUV.meshPoint)))
                    m.AddToTrisSet(m.PointedUV);

                if ((EditorInputManager.getAltKey() && (m.SelectedUV.meshPoint.uvpoints.Count > 1)))
                    m.DisconnectDragged();

                m.Dragging = true;
                m.AssignSelected(m.PointedUV);
                originalPosition = pointedUV.meshPoint.worldPos;
                draggedVertices.Clear();
                draggedVertices.Add(pointedUV.meshPoint);

          
            }

            return false;

        }

        public override bool MouseEventPointedLine() {

            var m = meshMGMT;

            if (EditorInputManager.GetMouseButtonDown(0))
            {
                m.Dragging = true;
                originalPosition = GridNavigator.collisionPos;//uvPoint.vert.worldPos;
                GridNavigator.onGridPos = GridNavigator.collisionPos;
                draggedVertices.Clear();
                foreach (var uv in pointedLine.pnts)
                    draggedVertices.Add(uv.meshPoint);
            }

            if (addToTrianglesAndLines && EditorInputManager.GetMouseButtonUp(0) && m.dragDelay>0 && draggedVertices.Contains(pointedLine))
                    meshMGMT.edMesh.insertIntoLine(meshMGMT.PointedLine.pnts[0].meshPoint, meshMGMT.PointedLine.pnts[1].meshPoint, meshMGMT.collisionPosLocal);
            

            return false;
        }

        public override bool MouseEventPointedTriangle() {
            var m = meshMGMT;

            if (EditorInputManager.GetMouseButtonDown(0))  {
                

              

                    m.Dragging = true;
                    originalPosition = GridNavigator.collisionPos;
                    GridNavigator.onGridPos = GridNavigator.collisionPos;
                    draggedVertices.Clear();
                    foreach (var uv in pointedTris.vertexes)
                    draggedVertices.Add(uv.meshPoint);
                
            }

            if (addToTrianglesAndLines && EditorInputManager.GetMouseButtonUp(0) && m.dragDelay > 0 && draggedVertices.Contains(pointedTris))
            {
                if (cfg.newVerticesUnique)
                    m.edMesh.insertIntoTriangleUniqueVerticles(m.PointedTris, m.collisionPosLocal);
                else
                    m.edMesh.insertIntoTriangle(m.PointedTris, m.collisionPosLocal);
            }
       


                return false;
        }

        public override void MouseEventPointedNothing() {
            if (EditorInputManager.GetMouseButtonDown(0))
                meshMGMT.AddPoint(meshMGMT.onGridLocal);
        }

        public override void ManageDragging()
        {
            var m = meshMGMT;

            if ((EditorInputManager.GetMouseButtonUp(0)) || (EditorInputManager.GetMouseButton(0) == false)) {
                m.Dragging = false;
                if (m.dragDelay<0)
                    editedMesh.dirty_Position = true;
            } else {
                m.dragDelay -= Time.deltaTime;
                if ((m.dragDelay < 0) || (Application.isPlaying == false))
                {

                    if ((GridNavigator.inst().angGridToCamera(GridNavigator.onGridPos) < 82)) {

                        Vector3 delta = GridNavigator.onGridPos - originalPosition;

                        if (delta.magnitude > 0)
                        {

                            m.TrisVerts = 0;

                            foreach (var v in draggedVertices)
                                v.worldPos += delta; // m.onGridLocal;

                            originalPosition = GridNavigator.onGridPos;
                        }
                    }

                }
            }
        }

        public override StdEncoder Encode() {
            var cody = new StdEncoder();
            cody.Add_Bool("inM", addToTrianglesAndLines); 
            return cody;
        }

        public override bool Decode(string tag, string data) {
            switch (tag) {
                case "inM": addToTrianglesAndLines = data.ToBool(); break;
                default: return false;
            }
            return true;
        }
    }

    public class SharpFacesTool : MeshToolBase
    {
        public static SharpFacesTool inst;

        public override string ToString() { return "Dominant Faces"; }

        public SharpFacesTool()
        {
            inst = this;
        }

        public bool SetTo = true;

        public override bool showVerticesDefault  { get { return false; } }

        public override string tooltip
        {
            get
            {
                return "Paint the DOMINANCE on triangles" + Environment.NewLine +
                    "It will affect how normal vector will be calculated" + Environment.NewLine +
                    "N - smooth verticle, detect edge" + Environment.NewLine +
                    "N on triangle near vertex - replace smooth normal of this vertex with This triangle's normal" + Environment.NewLine +
                    "N on line to ForceNormal on connected triangles. (Alt - unforce)"
                    ;
            }
        }
#if PEGI
        public override bool PEGI()
        {

            MeshManager m = meshMGMT;

            PainterConfig sd = PainterConfig.inst;

            "Dominance True".toggle(ref SetTo).nl();
       
            pegi.ClickToEditScript();

            if ("Auto Bevel".Click())
                AutoAssignDominantNormalsForBeveling();
            "Sensitivity".edit(60, ref cfg.bevelDetectionSensetivity, 3, 30).nl();

            if ("Sharp All".Click()) {
                foreach (MeshPoint vr in meshMGMT.edMesh.vertices)
                    vr.SmoothNormal = false;
                m.edMesh.dirty = true;
                cfg.newVerticesSmooth = false;
            }

            if ("Smooth All".Click().nl()) {
                foreach (MeshPoint vr in meshMGMT.edMesh.vertices)
                    vr.SmoothNormal = true;
                m.edMesh.dirty = true;
                cfg.newVerticesSmooth = true;
            }


            return false;
            
        }
#endif
        public override bool MouseEventPointedTriangle() {

           

            if (EditorInputManager.GetMouseButton(0)) 
                editedMesh.dirty |= pointedTris.SetSharpCorners(SetTo);

            return false;
        }

        public void AutoAssignDominantNormalsForBeveling()
        {

            foreach (MeshPoint vr in meshMGMT.edMesh.vertices)
                vr.SmoothNormal = true;

            foreach (var t in editedMesh.triangles) t.SetSharpCorners(true);

            foreach (var t in editedMesh.triangles)
            {
                Vector3[] v3s = new Vector3[3];

                for (int i = 0; i < 3; i++)
                    v3s[i] = t.vertexes[i].pos;

                float[] dist = new float[3];

                for (int i = 0; i < 3; i++)
                    dist[i] = (v3s[(i + 1) % 3] - v3s[(i + 2) % 3]).magnitude;

                for (int i = 0; i < 3; i++)
                {
                    var a = (i + 1) % 3;
                    var b = (i + 2) % 3;
                    if (dist[i] < dist[a] / cfg.bevelDetectionSensetivity && dist[i] < dist[b] / cfg.bevelDetectionSensetivity)
                    {
                        t.SetSharpCorners(false);

                        var other = (new LineData(t, t.vertexes[a], t.vertexes[b])).getOtherTriangle();
                        if (other != null)
                            other.SetSharpCorners(false);
                    }
                }
            }

            editedMesh.dirty = true;
        }

        public override void KeysEventPointedTriangle()
        {
  
            if (KeyCode.N.IsDown())
            {

                if (!EditorInputManager.getAltKey())
                {
                    int no = pointedTris.NumberOf(pointedTris.GetClosestTo(meshMGMT.collisionPosLocal));
                    pointedTris.DominantCourner[no] = !pointedTris.DominantCourner[no];
                    #if PEGI
                    (pointedTris.DominantCourner[no] ? "Triangle edge's Normal is now dominant" : "Triangle edge Normal is NO longer dominant").TeachingNotification();
#endif
                }
                else
                {
                    pointedTris.InvertNormal();
#if PEGI
                    "Inverting Normals".TeachingNotification();
#endif
                }

                meshMGMT.edMesh.dirty = true;

            }


        }

        public override void KeysEventPointedLine()
        {
            if (KeyCode.N.IsDown())
            {
                foreach (var t in meshMGMT.PointedLine.getAllTriangles_USES_Tris_Listing())
                    t.SetSharpCorners(!EditorInputManager.getAltKey());
#if PEGI
                "N ON A LINE - Make triangle normals Dominant".TeachingNotification();
#endif
                meshMGMT.edMesh.dirty = true;
            }
        }

        public override void KeysEventPointedVertex()
        {

         

            if (KeyCode.N.IsDown())
            {
                var m = meshMGMT;

                m.PointedUV.meshPoint.SmoothNormal = !m.PointedUV.meshPoint.SmoothNormal;
                m.edMesh.dirty = true;
#if PEGI
                "N - on Vertex - smooth Normal".TeachingNotification();
#endif
            }
            
        }

    }

    public class SmoothingTool : MeshToolBase
     {

            public bool MergeUnmerge = false;

            public static SmoothingTool inst;

            public override string ToString() { return "Vertex Smoothing"; }

            public SmoothingTool()
            {
                inst = this;
            }

            public override bool showVerticesDefault { get { return true; } }

            public override bool showLines { get { return true; } }

            public override string tooltip
            {
                get
                {
                    return "Click to set vertex as smooth/sharp" + Environment.NewLine;
                }
            }
#if PEGI
            public override bool PEGI()
            {

                MeshManager m = meshMGMT;

                PainterConfig sd = PainterConfig.inst;
                pegi.write("OnClick:", 60);
                if ((MergeUnmerge ? "Merging (Shift: Unmerge)" : "Smoothing (Shift: Unsmoothing)").Click().nl())
                    MergeUnmerge = !MergeUnmerge;

                if ("Sharp All".Click())
                {
                    foreach (MeshPoint vr in meshMGMT.edMesh.vertices)
                        vr.SmoothNormal = false;
                    m.edMesh.dirty = true;
                    cfg.newVerticesSmooth = false;
                }

                if ("Smooth All".Click().nl())
                {
                    foreach (MeshPoint vr in meshMGMT.edMesh.vertices)
                        vr.SmoothNormal = true;
                    m.edMesh.dirty = true;
                    cfg.newVerticesSmooth = true;
                }


                if ("All shared".Click())
                {
                    m.edMesh.AllVerticesShared();
                    m.edMesh.dirty = true;
                    cfg.newVerticesUnique = false;
                }

                if ("All unique".Click().nl())
                {
                    foreach (Triangle t in editedMesh.triangles)
                        m.edMesh.dirty |= m.edMesh.GiveTriangleUniqueVerticles(t);
                    cfg.newVerticesUnique = true;
                }



                return false;

            }
#endif
        public override bool MouseEventPointedTriangle()
            {

                if (EditorInputManager.GetMouseButton(0))
                {
                    if (MergeUnmerge)
                    {
                        if (EditorInputManager.getShiftKey())
                            editedMesh.dirty |= pointedTris.SetAllVerticesShared();
                        else
                            editedMesh.dirty |= editedMesh.GiveTriangleUniqueVerticles(pointedTris);
                    }
                    else
                        editedMesh.dirty |= pointedTris.SetSmoothVertices(!EditorInputManager.getShiftKey());
                }

                return false;
            }

            public override bool MouseEventPointedVertex()
            {
                if (EditorInputManager.GetMouseButton(0))
                {
                    if (MergeUnmerge)
                    {
                        if (EditorInputManager.getShiftKey())
                            editedMesh.dirty |= pointedVertex.SetAllUVsShared(); // .SetAllVerticesShared();
                        else
                            editedMesh.dirty |= pointedVertex.AllPointsUnique(); //editedMesh.GiveTriangleUniqueVerticles(pointedTris);
                    }
                    else
                        editedMesh.dirty |= pointedVertex.SetSmoothNormal(!EditorInputManager.getShiftKey());
                }

                return false;
            }

            public override bool MouseEventPointedLine()
            {
                if (EditorInputManager.GetMouseButton(0))
                {
                    if (MergeUnmerge)
                    {
                        if (!EditorInputManager.getShiftKey())
                            dirty |= pointedLine.AllVerticesShared();
                        else
                        {
                            dirty |= pointedLine.GiveUniqueVerticesToTriangles();

                        }
                    }
                    else
                    {
                        for (int i = 0; i < 2; i++)
                            dirty |= pointedLine[i].SetSmoothNormal(!EditorInputManager.getShiftKey());
                    }
                }

                return false;
            }

        }
    


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

    public class VertexColorTool : MeshToolBase
    {
        public static VertexColorTool inst;

        // public override MeshTool myTool { get { return MeshTool.VertColor; } }
        public override string ToString() { return "vertex Color"; }

        public override Color vertColor
        {
            get
            {
                return Color.white;
            }
        }

        public override string tooltip
        {
            get
            {
                return " 1234 on Line - apply RGBA for Border.";
            }
        }
#if PEGI
        public override bool PEGI() {
            if (("Paint All with Brush Color").Click())
                meshMGMT.edMesh.PaintAll(cfg.brushConfig.colorLinear);

            "Make Vertices Unique On coloring".toggle(60, ref cfg.MakeVericesUniqueOnEdgeColoring).nl();
            
            cfg.brushConfig.ColorSliders_PEGI().nl();

           // pegi.writeHint("Ctrl+LMB on Vertex - to paint only selected uv");
            return false;
        }
#endif
        public override bool MouseEventPointedVertex()  {
            MeshManager m = meshMGMT;

            BrushConfig bcf = globalBrush;

            //if (EditorInputManager.GetMouseButtonDown(1))
              //  m.pointedUV.vert.clearColor(cfg.brushConfig.mask);

            if ((EditorInputManager.GetMouseButtonDown(0))) {
                if (EditorInputManager.getControlKey())
                
                    bcf.mask.Transfer(ref m.PointedUV._color, bcf.colorLinear.ToGamma());
                
                else
                    foreach (Vertex uvi in m.PointedUV.meshPoint.uvpoints)
                        bcf.mask.Transfer(ref uvi._color, cfg.brushConfig.colorLinear.ToGamma());

                m.edMesh.dirty_Color = true;
            }

            return false;
        }

        public override bool MouseEventPointedLine()
        {
            if (EditorInputManager.GetMouseButton(0))  {
                if (pointedLine.sameAsLastFrame)
                    return true;

                BrushConfig bcf = cfg.brushConfig;

                Vertex a = pointedLine.pnts[0];
                Vertex b = pointedLine.pnts[1];
            
                Color c = bcf.colorLinear.ToGamma();
               
                a.meshPoint.SetColorOnLine(c, bcf.mask, b.meshPoint);//setColor(glob.colorSampler.color, glob.colorSampler.mask);
                b.meshPoint.SetColorOnLine(c, bcf.mask, a.meshPoint);
                meshMGMT.edMesh.dirty_Color = true;
                return true;
            }
            return false;
        }

        public override bool MouseEventPointedTriangle() {
            if (EditorInputManager.GetMouseButton(0))  {

                if (pointedTris.sameAsLastFrame)
                    return true;

                BrushConfig bcf = cfg.brushConfig;

                Color c = bcf.colorLinear.ToGamma();

                foreach (var u in pointedTris.vertexes)
                    foreach (var vuv in u.meshPoint.uvpoints)
                        bcf.mask.Transfer(ref  vuv._color, c);

              //  a.vert.SetColorOnLine(c, bcf.mask, b.vert);//setColor(glob.colorSampler.color, glob.colorSampler.mask);
               // b.vert.SetColorOnLine(c, bcf.mask, a.vert);
                meshMGMT.edMesh.dirty_Color = true;
                return true;
            }
            return false;
        }

        public override void KeysEventPointedLine()
        {
            Vertex a = pointedLine.pnts[0];
            Vertex b = pointedLine.pnts[1];

            int ind = Event.current.NumericKeyDown();

            if ((ind > 0) && (ind < 5))
            {
                a.meshPoint.FlipChanelOnLine((ColorChanel)(ind - 1), b.meshPoint);
                meshMGMT.edMesh.dirty = true;
                Event.current.Use();
            }
          
        }

        public VertexColorTool()
        {
            inst = this;
        }

    }

    public class VertexEdgeTool : MeshToolBase
    {

        public override string ToString() { return "vertex Edge"; }
        // public override MeshTool myTool { get { return MeshTool.VertexEdge; } }
#if PEGI
        public static pegi.CallDelegate PEGIdelegates;
#endif

        static bool AlsoDoColor = false;
        static bool editingFlexibleEdge = false;
        public static float edgeValue = 1;
        
        public static float shiftInvertedVelue { get { return EditorInputManager.getShiftKey() ? 1f - edgeValue : edgeValue;  } }

        public override string tooltip { get { return
                    "Shift - invert edge value" + Environment.NewLine +
                    "This tool allows editing value if edge.w" + Environment.NewLine + "edge.xyz is used to store border information about the triendle. ANd the edge.w in Example shaders is used" +
                    "to mask texture color with vertex color. All vertices should be set as Unique for this to work." +
                    "Line is drawn between vertices marked with line strength 1. A triangle can't have only 2 sides with Edge: it's eather 1 side, or all 3 (2 points marked to create a line, or 3 points to create 3 lines)."; } }

        public override bool showTriangles { get { return false; } }

        public override bool showVerticesDefault { get { return !editingFlexibleEdge; } }
#if PEGI
        public override bool PEGI() {
            bool changed = false;
            changed |= "Edge Strength: ".edit(ref edgeValue).nl();
            changed |= "Also do color".toggle(ref AlsoDoColor).nl();

            if (AlsoDoColor)
                changed|= globalBrush.ColorSliders_PEGI().nl();

            if (editedMesh.submeshCount > 1 && "Apply To Lines between submeshes".Click().nl())
                LinesBetweenSubmeshes();

            if (PEGIdelegates != null)
            foreach (pegi.CallDelegate d in PEGIdelegates.GetInvocationList())
                changed|= d();

            //if (meshMGMT.target != null && meshMGMT.target.isAtlased())

            "Flexible Edge".toggle("Edge type can be seen in Packaging profile (if any). Only Bevel shader doesn't have a Flexible edge.",90,ref editingFlexibleEdge);

            return changed;
        }
#endif
        void LinesBetweenSubmeshes() {

        }

        public override bool MouseEventPointedVertex()
        {
          

            if ((EditorInputManager.GetMouseButton(0)))
            {
#if PEGI
                if (!pointedUV.meshPoint.AllPointsUnique())
                    "Shared points found, Edge requires All Unique".showNotification();
#endif
                if (EditorInputManager.getControlKey()) {
                    edgeValue = meshMGMT.PointedUV.meshPoint.edgeStrength;
                    if (AlsoDoColor) globalBrush.colorLinear.From(pointedUV._color);
                       
                            // foreach (UVpoint uvi in m.pointedUV.vert.uvpoints)
                         //   bcf.mask.Transfer(ref uvi._color, cfg.brushConfig.colorLinear.ToGamma());
                }
                else
                {

                    if (pointedUV.sameAsLastFrame)
                        return true;

                    meshMGMT.PointedUV.meshPoint.edgeStrength = shiftInvertedVelue;
                    if (AlsoDoColor)
                    {
                        var col = globalBrush.colorLinear.ToGamma();
                        foreach (Vertex uvi in pointedUV.meshPoint.uvpoints)
                            globalBrush.mask.Transfer(ref uvi._color, col);
                    }
                    meshMGMT.edMesh.dirty = true;

                    return true;
                }


              
            }
            return false;
        }

        public override bool MouseEventPointedLine()
        {
            if (EditorInputManager.GetMouseButton(0)) {

                var vrtA = pointedLine.pnts[0].meshPoint;
                var vrtB = pointedLine.pnts[1].meshPoint;

                if (EditorInputManager.getControlKey()) 
                    edgeValue = (vrtA.edgeStrength + vrtB.edgeStrength ) *0.5f;
                else {
                    if (pointedLine.sameAsLastFrame)
                        return true;


                    putEdgeOnLine(pointedLine);

                    return true;
                   /* vrtA.edgeStrength = edgeValue;
                    vrtB.edgeStrength = edgeValue;

                    var tris = pointedLine.getAllTriangles_USES_Tris_Listing();

                    foreach (var t in tris)
                        t.edgeWeight[t.NotOnLineIndex(pointedLine)] = edgeValue;// true;


                    if (AlsoDoColor) {
                        var col = globalBrush.colorLinear.ToGamma();
                        foreach (UVpoint uvi in vrtA.uvpoints)
                            globalBrush.mask.Transfer(ref uvi._color, col);
                        foreach (UVpoint uvi in vrtB.uvpoints)
                            globalBrush.mask.Transfer(ref uvi._color, col);
                    }

                    meshMGMT.edMesh.dirty = true;*/
                }
            }
            return false;
        }

        public static void putEdgeOnLine(LineData ld) {

            var vrtA = ld.pnts[0].meshPoint;
            var vrtB = ld.pnts[1].meshPoint;

            var tris = ld.getAllTriangles_USES_Tris_Listing();

            foreach (var t in tris)
                t.edgeWeight[t.NotOnLineIndex(ld)] = shiftInvertedVelue;// true;

            float edValA = shiftInvertedVelue;
            float edValB = shiftInvertedVelue;

            if (editingFlexibleEdge)
            {

                foreach (var uv in vrtA.uvpoints)
                    foreach (var t in uv.tris)
                    {
                        var opposite = t.NumberOf(uv);
                        for (int i = 0; i < 3; i++)
                            if (opposite != i)
                            edValA = Mathf.Max(edValA, t.edgeWeight[i]);
                    }

                foreach (var uv in vrtB.uvpoints)
                    foreach (var t in uv.tris)
                    {
                        var opposite = t.NumberOf(uv);
                        for (int i = 0; i < 3; i++)
                            if (opposite != i)
                                edValB = Mathf.Max(edValB, t.edgeWeight[i]);
                    }
            }

            vrtA.edgeStrength = edValA;
            vrtB.edgeStrength = edValB;

            if (AlsoDoColor)
            {
                var col = globalBrush.colorLinear.ToGamma();
                foreach (Vertex uvi in vrtA.uvpoints)
                    globalBrush.mask.Transfer(ref uvi._color, col);
                foreach (Vertex uvi in vrtB.uvpoints)
                    globalBrush.mask.Transfer(ref uvi._color, col);
            }

            meshMGMT.edMesh.dirty = true;

        }


        public override bool Decode(string tag, string data)
        {
            switch (tag) {
                case "v": edgeValue = data.ToFloat(); break;
                case "doCol": AlsoDoColor = data.ToBool(); break;
                case "fe": editingFlexibleEdge = data.ToBool(); break;
                default: return false;
            }
            return true;
        }

        public override StdEncoder Encode()
        {
            var cody = new StdEncoder();

            cody.Add("v", edgeValue);
            cody.Add_Bool("doCol", AlsoDoColor);
            cody.Add_Bool("fe", editingFlexibleEdge);

            return cody;
        }

    }

    public class TriangleSubmeshTool : MeshToolBase
    {
        public int curSubmesh = 0;

        public override string ToString() { return "triangle Submesh index"; }

        public override bool showVerticesDefault { get { return false; } }

        public override bool showLines { get { return false; } }
        
        public override string tooltip
        {
            get
            {
                return "Ctrl+LMB - sample" + Environment.NewLine + "LMB on triangle - set submesh";
            }
        }

        public override bool MouseEventPointedTriangle()
        {

            if (EditorInputManager.GetMouseButtonDown(0) && EditorInputManager.getControlKey())
            {
                curSubmesh = (int)meshMGMT.PointedTris.submeshIndex;
#if PEGI
                ("Submesh " + curSubmesh).showNotification();
#endif
            }

            if (EditorInputManager.GetMouseButton(0) && !EditorInputManager.getControlKey() && (meshMGMT.PointedTris.submeshIndex != curSubmesh))  {
                if (pointedTris.sameAsLastFrame)
                    return true;
                meshMGMT.PointedTris.submeshIndex = curSubmesh;
                    editedMesh.submeshCount = Mathf.Max(meshMGMT.PointedTris.submeshIndex + 1, editedMesh.submeshCount);
                    editedMesh.dirty = true;
                return true;
            }
            return false;
        }
#if PEGI
        public override bool PEGI()
        {
            ("Total Submeshes: " + editedMesh.submeshCount).nl();

            "Submesh: ".edit(60, ref curSubmesh).nl();
            return false;
        }
#endif
        public override StdEncoder Encode() {
            var cody = new StdEncoder();
            if (curSubmesh != 0)
                cody.Add("sm", curSubmesh);
            return cody;
        }

        public override bool Decode(string tag, string data) {
            switch (tag) {
                case "sm": curSubmesh = data.ToInt(); break;
                default: return false;
            }
            return true;

        }
    }

    public class VertexGroupTool : MeshToolBase {

        public override string ToString() {
            return "Vertex Group";
        }

    

    }

}