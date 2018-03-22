using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
namespace Playtime_Painter
{

    public abstract class MeshToolBase {
        
        static List<MeshToolBase> _allTools;

        public static List<MeshToolBase> allTools
        { get {
                if (_allTools == null) {
                    _allTools = new List<MeshToolBase>();

                    _allTools.Add(new VertexPositionTool());
                    _allTools.Add(new SharpFacesTool());
                    _allTools.Add(new VertexColorTool());
                    _allTools.Add(new VertexEdgeTool());
                    _allTools.Add(new TriangleAtlasTool());
                    _allTools.Add(new TriangleSubmeshTool());
                }
                return _allTools;
            } }
        
        protected MeshManager mgmt { get { return MeshManager.inst; } }
        protected EditableMesh mesh { get { return MeshManager.inst._Mesh; } }
        protected PainterConfig cfg { get { return PainterConfig.inst; } }
        protected LineData line { get { return mgmt.pointedLine; } }
        protected trisDta triangle { get { return mgmt.pointedTris; } }
        protected UVpoint vertex { get { return mgmt.pointedUV; } }

        public virtual bool showVertices { get { return true; } }
        public virtual bool showLines { get { return true; } }
        public virtual bool showTriangles { get { return true; } }

        public virtual bool showGrid { get { return false; } }

        public virtual bool showSelectedVerticle { get { return false; } }
        public virtual bool showSelectedLine { get { return false; } }
        public virtual bool showSelectedTriangle { get { return false; } }

        public virtual string tooltip { get { return "No tooltip"; } }

        public virtual Color vertColor {get { return Color.gray;  }  }

        public virtual void PEGI() {
        }

        public virtual void AssignText(MarkerWithText mrkr, vertexpointDta vpoint) {
           mrkr.textm.text = "";
        }

        public virtual void MouseEventPointedVertex() { }

        public virtual void MouseEventPointedLine() { }

        public virtual void MouseEventPointedTriangle() { }

        public virtual void MouseEventPointedNothing() {  }

        public virtual void KeysEventPointedVertex()  { }

        public virtual void KeysEventPointedLine()  { }

        public virtual void KeysEventPointedTriangle()  { }

        public virtual void ManageDragging() { }
    }

    public class VertexPositionTool : MeshToolBase
    {
        public static VertexPositionTool inst;

        public override string ToString()  {return "vertex Position"; }

        public VertexPositionTool() {
            inst = this;
        }

        Vector3 displace = new Vector3();

        public override bool showGrid { get { return true; } }
  
        public override void AssignText(MarkerWithText mrkr, vertexpointDta vpoint)
        {

            var pvrt = mgmt.GetSelectedVert();

            if ((vpoint.uv.Count > 1) || (pvrt == vpoint))
            {

                Texture tex = mgmt.target.meshRenderer.sharedMaterial.mainTexture;

                if (pvrt == vpoint)
                {
                    mrkr.textm.text = (vpoint.uv.Count > 1) ? ((vpoint.uv.IndexOf(mgmt.selectedUV) + 1).ToString() + "/" + vpoint.uv.Count.ToString() +
                        (vpoint.SmoothNormal ? "s" : "")) : "";
                  

                }
                else
                    mrkr.textm.text = vpoint.uv.Count.ToString() +
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
                return  "LMB - Add Vertices, Make Triangles (Go Clockwise)" + Environment.NewLine +
                    "Alt + LMB - Add On Grid" + Environment.NewLine +
                    "RMB - Drag Vertices" + Environment.NewLine +
                    "Scroll - Change Plane" + Environment.NewLine +
                    "U - make triengle unique." + Environment.NewLine+
                    "M - merge with nearest while dragging " + Environment.NewLine + 
                    "N - smooth verticle, detect edge" + Environment.NewLine + 
                    "N on triangle near vertex - replace smooth normal of this vertex with This triangle's normal" + Environment.NewLine +
                    "N on line to ForceNormal on connected triangles. (Alt - unforce)";
            }
        }

        public override void PEGI() {

            MeshManager mgm = mgmt;

            PainterConfig sd = PainterConfig.inst;

            if (MeshManager.tool.showGrid)
            {
                "Snap to grid:".toggle(100, ref sd.SnapToGrid);

                if (sd.SnapToGrid)
                    "size:".edit(40, ref sd.SnapToGridSize);
            }

            pegi.ClickToEditScript();

            pegi.newLine();

            if ("Auto Bevel".Click())
                SharpFacesTool.inst.AutoAssignDominantNormalsForBeveling();
            "Sensitivity".edit(60,ref cfg.bevelDetectionSensetivity, 3, 30).nl();

            "Pixel-Perfect".toggle("New vertex will have UV coordinate rounded to half a pixel.",120, ref cfg.pixelPerfectMeshEditing).nl();

            "Add Smooth:".toggle(70, ref MeshManager.cfg.newVerticesSmooth);
            if (pegi.Click("Sharp All")) {
                foreach (vertexpointDta vr in mgmt._Mesh.vertices)
                    vr.SmoothNormal = false;
                mgm._Mesh.dirty = true;
                MeshManager.cfg.newVerticesSmooth = false;
            }

            if (pegi.Click("Smooth All").nl()) {
                foreach (vertexpointDta vr in mgmt._Mesh.vertices)
                    vr.SmoothNormal = true;
                mgm._Mesh.dirty = true;
                MeshManager.cfg.newVerticesSmooth = true;
            }

            "Add Unique:".toggle(70, ref MeshManager.cfg.newVerticesUnique);
            if (pegi.Click("All shared")) {
                mgm._Mesh.AllVerticesShared();
                mgm._Mesh.dirty = true;
                MeshManager.cfg.newVerticesUnique = false;
            }

            if (pegi.Click("All unique"))
            {
                foreach (trisDta t in mesh.triangles)
                    mgm._Mesh.GiveTriangleUniqueVerticles(t);
                mgm._Mesh.dirty = true;
                MeshManager.cfg.newVerticesUnique = true;
            }
            pegi.newLine();

         
            if (pegi.Click("Mirror by Center")) {
                GridNavigator.onGridPos = mgm.target.transform.position;
                mgm.UpdateLocalSpaceV3s();
                mgm._Mesh.MirrorVerticlesAgainsThePlane(mgm.onGridLocal);
            }

            if (pegi.Click("Mirror by Plane")) {
                mgm.UpdateLocalSpaceV3s();
                mgm._Mesh.MirrorVerticlesAgainsThePlane(mgm.onGridLocal);
            }
            pegi.newLine();

            pegi.edit(ref displace);
            pegi.newLine();

            if (pegi.Click("Cancel")) displace = Vector3.zero;

            if (pegi.Click("Apply")) {
                mgm._Mesh.Displace(displace);
                mgm._Mesh.dirty = true;
                displace = Vector3.zero;
            }
            pegi.newLine();
        }

        public override void KeysEventPointedVertex()
        {
  
            if (mgmt.pointedUV == null) return;

            if ((KeyCode.M.isDown()) && (mgmt.draggingSelected))
            {
                mgmt.selectedUV.vert.MergeWithNearest();
                mgmt.draggingSelected = false;
                mgmt._Mesh.dirty = true;
                "M - merge with nearest".TeachingNotification();
            }

            if (KeyCode.N.isDown()) {
                mgmt.pointedUV.vert.SmoothNormal = !mgmt.pointedUV.vert.SmoothNormal;
                mgmt._Mesh.dirty = true;
                "N - on Vertex - smooth Normal".TeachingNotification();
            }

            if (KeyCode.Delete.isDown())
            {
                //Debug.Log("Deleting");
                if (!EditorInputManager.getControlKey())
                {
                    if (mgmt.pointedUV.vert.uv.Count == 1)
                    {
                        if (!mgmt.DeleteVertHEAL(mgmt.pointedUV.vert))
                            mgmt.DeleteUv(mgmt.pointedUV);
                    }
                    else
                    {
                        while (mgmt.pointedUV.vert.uv.Count > 1)
                        {
                            mgmt._Mesh.MoveTris(mgmt.pointedUV.vert.uv[1], mgmt.pointedUV.vert.uv[0]); //DeleteUv(pointedAt);
                        }
                        //Debug.Log("Healing");
                    }

                }
                else
                    mgmt.DeleteUv(mgmt.pointedUV);
                mgmt._Mesh.dirty = true;
            }
        }

        public override void KeysEventPointedLine()
        {
            if (KeyCode.N.isDown()) {
                foreach (var t in mgmt.pointedLine.getAllTriangles_USES_Tris_Listing())
                    t.SetSharpCorners(!EditorInputManager.getAltKey());

                "N ON A LINE - Make triangle normals Dominant".TeachingNotification();

                mgmt._Mesh.dirty = true;
            }
        }

        public override void KeysEventPointedTriangle()
        {
            if (KeyCode.Backspace.isDown())
            {
                mgmt._Mesh.triangles.Remove(triangle);
                mgmt._Mesh.dirty = true;
                return;
            }

            if (KeyCode.U.isDown())
                triangle.MakeTriangleVertUnique(vertex);

            if (KeyCode.N.isDown())
            {

                if (!EditorInputManager.getAltKey())
                {
                    int no = triangle.NumberOf(triangle.GetClosestTo(mgmt.collisionPosLocal));
                    triangle.SharpCorner[no] = !triangle.SharpCorner[no];

                    (triangle.SharpCorner[no] ? "Triangle edge's Normal is now dominant" : "Triangle edge Normal is NO longer dominant").TeachingNotification();
                }
                else
                {
                    triangle.InvertNormal();
                    "Inverting Normals".TeachingNotification();
                }

                mgmt._Mesh.dirty = true;

            }


        }

        public override void MouseEventPointedVertex()
        {
         

            //Debug.Log("Mouse event pointed vertex");

            if (EditorInputManager.GetMouseButtonDown(0))
            {
                if ((mgmt.trisVerts < 3) && (mgmt.pointedUV != null) && (!mgmt.isInTrisSet(mgmt.pointedUV.vert)))
                    mgmt.AddToTrisSet(mgmt.pointedUV);

              
            }

            if ((EditorInputManager.GetMouseButtonDown(1)))
            {
                if ((EditorInputManager.getAltKey() && (mgmt.selectedUV.vert.uv.Count>1)))
                    mgmt.DisconnectDragged();
                mgmt.draggingSelected = true;
                mgmt.dragDelay = 0.2f;
            }

        }

        public override void MouseEventPointedLine() {
            if (EditorInputManager.GetMouseButtonDown(0)) 
                mgmt._Mesh.insertIntoLine(mgmt.pointedLine.pnts[0].vert, mgmt.pointedLine.pnts[1].vert, mgmt.collisionPosLocal);
            
        }

        public override void MouseEventPointedTriangle() {
            if (EditorInputManager.GetMouseButtonDown(0))  {
                if (MeshManager.cfg.newVerticesUnique)
                    mgmt._Mesh.insertIntoTriangleUniqueVerticles(mgmt.pointedTris, mgmt.collisionPosLocal);
                else
                    mgmt._Mesh.insertIntoTriangle(mgmt.pointedTris, mgmt.collisionPosLocal);
            }
        }

        public override void MouseEventPointedNothing() {
            if (EditorInputManager.GetMouseButtonDown(0))
                mgmt.AddPoint(mgmt.onGridLocal);
        }

        public override void ManageDragging()
        {
            if ((EditorInputManager.GetMouseButtonUp(1)) || (EditorInputManager.GetMouseButton(1) == false))
            {
                mgmt.draggingSelected = false;
                mesh.dirty = true;

            }
            else
            {
                mgmt.dragDelay -= Time.deltaTime;
                if ((mgmt.dragDelay < 0) || (Application.isPlaying == false))
                {
                    if (mgmt.selectedUV == null) { mgmt.draggingSelected = false; Debug.Log("no selected"); return; }
                    if ((GridNavigator.inst().angGridToCamera(GridNavigator.onGridPos) < 82))
                                mgmt.selectedUV.vert.pos = mgmt.onGridLocal;
                }
            }
        }

    }

    public class SharpFacesTool : MeshToolBase
    {
        public static SharpFacesTool inst;

        public override string ToString() { return "Sharp Faces"; }

        public SharpFacesTool()
        {
            inst = this;
        }

        public bool SetTo = true;

        public override bool showVertices  { get { return false; } }

        public override string tooltip
        {
            get
            {
                return "Paint the DOMINANCE on triangles" + Environment.NewLine +
                    "It will affect how normal vector will be calculated";
            }
        }

        public override void PEGI()
        {

            MeshManager mgm = mgmt;

            PainterConfig sd = PainterConfig.inst;

            "Dominance True".toggle(ref SetTo).nl();
       
            pegi.ClickToEditScript();

            if ("Auto Bevel".Click())
                AutoAssignDominantNormalsForBeveling();
            "Sensitivity".edit(60, ref cfg.bevelDetectionSensetivity, 3, 30).nl();

            if ("Sharp All".Click()) {
                foreach (vertexpointDta vr in mgmt._Mesh.vertices)
                    vr.SmoothNormal = false;
                mgm._Mesh.dirty = true;
                MeshManager.cfg.newVerticesSmooth = false;
            }

            if ("Smooth All".Click().nl()) {
                foreach (vertexpointDta vr in mgmt._Mesh.vertices)
                    vr.SmoothNormal = true;
                mgm._Mesh.dirty = true;
                MeshManager.cfg.newVerticesSmooth = true;
            }


        
            
        }

        public override void MouseEventPointedTriangle() {
            if (EditorInputManager.GetMouseButton(0))
                mesh.dirty |= triangle.SetSharpCorners(SetTo);
        }

        public void AutoAssignDominantNormalsForBeveling()
        {

            foreach (vertexpointDta vr in mgmt._Mesh.vertices)
                vr.SmoothNormal = true;

            foreach (var t in mesh.triangles) t.SetSharpCorners(true);

            foreach (var t in mesh.triangles)
            {
                Vector3[] v3s = new Vector3[3];

                for (int i = 0; i < 3; i++)
                    v3s[i] = t.uvpnts[i].pos;

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

                        var other = (new LineData(t, t.uvpnts[a], t.uvpnts[b])).getOtherTriangle();
                        if (other != null)
                            other.SetSharpCorners(false);
                    }
                }
            }

            mesh.dirty = true;
        }

    }

    public class VertexUVTool : MeshToolBase
    {
        public override string ToString() { return "vertex UV"; }

        public static VertexUVTool _inst;

        public VertexUVTool() {
            _inst = this;
        }
        
        public override void PEGI()
        {
            MeshManager tmp = mgmt;

            pegi.write("Edit UV 1:", 70);
            pegi.toggleInt (ref MeshManager.editedUV);
            pegi.newLine();

            if (mgmt.selectedUV != null)
                if (pegi.Click("ALL from selected")) {
                    foreach (vertexpointDta vr in mgmt._Mesh.vertices)
                        foreach (UVpoint uv in vr.uv)
                            uv.editedUV = mgmt.selectedUV.editedUV;

                    mgmt._Mesh.dirty = true;
                }

            pegi.newLine();

            if (mgmt.selectedUV != null)
                pegi.write("UV: " + (tmp.selectedUV.editedUV.x) + "," + (tmp.selectedUV.editedUV.y));

            pegi.newLine();
            if (mgmt.GridToUVon) {
                pegi.write("Projection size");
                if (pegi.edit(ref MeshManager.cfg.MeshUVprojectionSize))
                    mgmt.ProcessScaleChange();
            }
            pegi.newLine();

           if  (pegi.toggle(ref tmp.GridToUVon, "Grid Painting ", 90)) {
                //if (!tmp.GridToUVon)
                    //cfg._meshTool = MeshTool.uv;
                tmp.UpdatePreviewIfGridedDraw();
            }

            pegi.newLine();

            if (tmp.selectedUV != null)
                if (pegi.Click("All vert UVs from selected"))  {
                    foreach (UVpoint uv in tmp.selectedUV.vert.uv)
                        uv.editedUV = tmp.selectedUV.editedUV;

                    tmp._Mesh.dirty = true;
                }


            pegi.newLine();


        }

        public override Color vertColor
        {
            get
            {
                return Color.magenta; 
            }
        }

        public override void AssignText(MarkerWithText mrkr, vertexpointDta vpoint)
        {

         

            var pvrt = mgmt.GetSelectedVert();

            if ((vpoint.uv.Count > 1) || (pvrt == vpoint))
            {

                Texture tex = mgmt.target.meshRenderer.sharedMaterial.mainTexture;

                if (pvrt == vpoint)
                {
                    mrkr.textm.text = (vpoint.uv.Count > 1) ? ((vpoint.uv.IndexOf(mgmt.selectedUV) + 1).ToString() + "/" + vpoint.uv.Count.ToString() +
                        (vpoint.SmoothNormal ? "s" : "")) : "";
                    float tsize = tex == null ? 128 : tex.width;
                       mrkr.textm.text +=
                        ("uv: " + (mgmt.selectedUV.editedUV.x * tsize) + "," + (mgmt.selectedUV.editedUV.y * tsize));
                }
                else
                    mrkr.textm.text = vpoint.uv.Count.ToString() +
                        (vpoint.SmoothNormal ? "s" : "");
            }
            else mrkr.textm.text = "";
        }

        public override void MouseEventPointedVertex()
        {
         

            if (EditorInputManager.GetMouseButtonDown(0))
            {
                if ((mgmt.selectedUV != null) && (mgmt.pointedUV != null))
                {
                    mgmt.pointedUV.editedUV = mgmt.selectedUV.editedUV;
                    mgmt._Mesh.dirty = true;
                }

              
            }

            if ((EditorInputManager.GetMouseButtonDown(1)) && (mgmt.pointedUV != null) && (UVnavigator.inst() != null))
                UVnavigator.inst().CenterOnUV(mgmt.pointedUV.editedUV);

        }

        public override void MouseEventPointedLine()
        {
            UVpoint a = line.pnts[0];
            UVpoint b = line.pnts[1];

            if (Vector3.Distance(mgmt.collisionPosLocal, a.pos) < Vector3.Distance(mgmt.collisionPosLocal, b.pos))
                mgmt.AssignSelected(mgmt._Mesh.GetUVpointAFromLine(a.vert, b.vert));
            else
                mgmt.AssignSelected(mgmt._Mesh.GetUVpointAFromLine(b.vert, a.vert));

        }

        public override void MouseEventPointedTriangle()  {

                if (EditorInputManager.GetMouseButtonDown(0))  {
                        if (mgmt.GridToUVon) {
                            if (mgmt.selectedUV == null) mgmt.selectedUV = mgmt._Mesh.vertices[0].uv[0];

                            for (int i = 0; i < 3; i++)
                                mgmt.pointedTris.uvpnts[i].editedUV = mgmt.PosToUV(mgmt.pointedTris.uvpnts[i].pos);
                            mgmt._Mesh.dirty = true;
                        }
            }
        }

        public override void KeysEventPointedLine()
        {
            if ((KeyCode.Backspace.isDown()))
            {
                UVpoint a = line.pnts[0];
                UVpoint b = line.pnts[1];

                if (!EditorInputManager.getControlKey())
                    mgmt.SwapLine(a.vert, b.vert);
                else
                    mgmt.DeleteLine(line);

                mgmt._Mesh.dirty = true;
            }
        }

    }

    public class VertexAnimationTool : MeshToolBase
    {
        public override string ToString() { return "vertex Animation"; }

        public override void AssignText(MarkerWithText mrkr, vertexpointDta vpoint)
        {

          
                mrkr.textm.text = vpoint.index.ToString();
             
        }
        public override void MouseEventPointedVertex()
        {
          
            if (mgmt.pointedUV == null) return;
            if (EditorInputManager.GetMouseButtonDown(1))
            {
                mgmt.draggingSelected = true;
                mgmt.dragDelay = 0.2f;
            }
        }

        public override bool showGrid { get { return true; } }

        public override void ManageDragging()
        {
            if ((EditorInputManager.GetMouseButtonUp(1)) || (EditorInputManager.GetMouseButton(1) == false))
            {
                mgmt.draggingSelected = false;
                mesh.dirty = true;

            }
            else
            {
                mgmt.dragDelay -= Time.deltaTime;
                if ((mgmt.dragDelay < 0) || (Application.isPlaying == false))
                {
                    if (mgmt.selectedUV == null) { mgmt.draggingSelected = false; Debug.Log("no selected"); return; }
                    if ((GridNavigator.inst().angGridToCamera(GridNavigator.onGridPos) < 82) &&
                               mgmt.target.AnimatedVertices())
                                    mgmt.selectedUV.vert.AnimateTo(mgmt.onGridLocal);
                                
                    
                        
                    
                }
            }
        }

    }

    public class VertexColorTool : MeshToolBase
    {
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

        public override void PEGI() {
            if (("Paint All with Brush Color").Click())
                mgmt._Mesh.PaintAll(cfg.brushConfig.colorLinear);

            "Make Vertices Unique On coloring".toggle(60, ref cfg.MakeVericesUniqueOnEdgeColoring).nl();

            Color col = cfg.brushConfig.colorLinear.ToColor();
            if (pegi.edit(ref col).nl())
                cfg.brushConfig.colorLinear.From(col);

            cfg.brushConfig.ColorSliders_PEGI().nl();

            pegi.writeHint("Ctrl+LMB on Vertex - to paint only selected uv");
        }

        public override void MouseEventPointedVertex()  {
            MeshManager m = mgmt;

            BrushConfig bcf = cfg.brushConfig;

            if (EditorInputManager.GetMouseButtonDown(1))
                m.pointedUV.vert.clearColor(cfg.brushConfig.mask);

            if ((EditorInputManager.GetMouseButtonDown(0))) {
                if (EditorInputManager.getControlKey())
                
                    bcf.mask.Transfer(ref m.pointedUV._color, bcf.colorLinear.ToColor());
                
                else
                    foreach (UVpoint uvi in m.pointedUV.vert.uv)
                        bcf.mask.Transfer(ref uvi._color, cfg.brushConfig.colorLinear.ToColor());

                m._Mesh.dirty = true;
            }
        }

        public override void MouseEventPointedLine()
        {
            if (EditorInputManager.GetMouseButtonDown(0))  {
                BrushConfig bcf = cfg.brushConfig;

                UVpoint a = line.pnts[0];
                UVpoint b = line.pnts[1];
            
                Color c = bcf.colorLinear.ToColor();
               
                a.vert.SetColorOnLine(c, bcf.mask, b.vert);//setColor(glob.colorSampler.color, glob.colorSampler.mask);
                b.vert.SetColorOnLine(c, bcf.mask, a.vert);
                mgmt._Mesh.dirty = true;
            }
        }

        public override void KeysEventPointedLine()
        {
            UVpoint a = line.pnts[0];
            UVpoint b = line.pnts[1];

            int ind = Event.current.NumericKeyDown();

            if ((ind > 0) && (ind < 5))
            {
                a.vert.FlipChanelOnLine((ColorChanel)(ind - 1), b.vert);
                mgmt._Mesh.dirty = true;
                Event.current.Use();
            }
          
        }

    }

    public class VertexEdgeTool : MeshToolBase
    {
        public override string ToString() { return "vertex Edge"; }
        // public override MeshTool myTool { get { return MeshTool.VertexEdge; } }

        public override bool showTriangles { get { return false; } }

        public static float edgeValue = 1;

        public override void PEGI() {
            "Edge Strength: ".edit(ref edgeValue).nl();

            pegi.writeHint("Strength usually used to show color on the edge and hide seam");
        }

        public override void MouseEventPointedVertex()
        {
          

            if ((EditorInputManager.GetMouseButtonDown(0)))
            {
                if (EditorInputManager.getControlKey())
                    edgeValue = mgmt.pointedUV.vert.edgeStrength;
                else
                    mgmt.pointedUV.vert.edgeStrength = edgeValue;


                mgmt._Mesh.dirty = true;
            }
        }

        public override void MouseEventPointedLine()
        {
            if (EditorInputManager.GetMouseButtonDown(0))
            {

                if (EditorInputManager.getControlKey()) 

                    edgeValue = (line.pnts[0].vert.edgeStrength + line.pnts[1].vert.edgeStrength ) *0.5f;

                
                else {
                   // Debug.Log("Assigning");
                    line.pnts[0].vert.edgeStrength = edgeValue;
                    line.pnts[1].vert.edgeStrength = edgeValue;


                    mgmt._Mesh.dirty = true;
                }
            }
        }
        
    }

    public class VertexShadowTool : MeshToolBase
    {
        public override string ToString() { return "vertex Shadow"; }
        // public override MeshTool myTool { get { return MeshTool.VertexShadow; } }

        public override void MouseEventPointedVertex()
        {
          

            if (mgmt.pointedUV == null) return;

            if ((EditorInputManager.GetMouseButtonDown(0)))
            {
                BrushConfig bcf = cfg.brushConfig;
                bcf.colorLinear.ToV4(ref mgmt.pointedUV.vert.shadowBake, bcf.mask);
                Debug.Log("Modified shadow to: " + mgmt.pointedUV.vert.shadowBake.ToString());

                mgmt._Mesh.dirty = true;
            }
        }

    }

    public class TriangleSubmeshTool : MeshToolBase
    {

        public override string ToString() { return "triangle Submesh index"; }

        public override bool showVertices { get { return false; } }

        public override bool showLines { get { return false; } }
        
        public override string tooltip
        {
            get
            {
                return "Ctrl+LMB - sample" + Environment.NewLine + "LMB on triangle - set submesh";
            }
        }

        public override void MouseEventPointedTriangle()
        {

            if (EditorInputManager.GetMouseButtonDown(0) && EditorInputManager.getControlKey())
            {
                cfg.curSubmesh = (int)mgmt.pointedTris.submeshIndex;
                ("Submesh " + cfg.curSubmesh).showNotification();
            }

            if (EditorInputManager.GetMouseButton(0) && !EditorInputManager.getControlKey() && (mgmt.pointedTris.submeshIndex != cfg.curSubmesh))  {
                    mgmt.pointedTris.submeshIndex = cfg.curSubmesh;
                    mesh.submeshCount = Mathf.Max(mgmt.pointedTris.submeshIndex + 1, mesh.submeshCount);
                    mesh.dirty = true;



                }
            
        }


        public override void PEGI()
        {
            ("Total Submeshes: " + mesh.submeshCount).nl();

            "Submesh: ".edit(60, ref cfg.curSubmesh).nl();
        }
    }

        public class TriangleAtlasTool : MeshToolBase
    {

        public override string ToString() { return "triangle Atlas Textures"; }

        public override bool showLines  {
            get
            {
                return cfg.atlasEdgeAsChanel2;
            }
        }

        public override bool showVertices {
            get
            {
                return false;
            }
        }

        static TriangleAtlasTool _inst;

		public static TriangleAtlasTool inst {get { if (_inst == null) {
                    var a = allTools;
                    return _inst;
                        }
                        return _inst;}}

        public TriangleAtlasTool()
        {
            _inst = this;
        }

        public override string tooltip
        {
            get
            {
                return "Select Texture and click on triangles";
            }
        }

        public override void PEGI() {

            "Edge Click as Chanel 2".toggle(ref cfg.atlasEdgeAsChanel2).nl();

            "Atlas Texture: ".edit(ref cfg.curAtlasTexture).nl();
            "Atlas Chanel: ".edit(ref cfg.curAtlasChanel).nl();

            if (mgmt.selectedTris != null)
            {
                ("Selected tris uses Atlas Texture " + mgmt.selectedTris.textureNo[0]).nl();
            }

            pegi.writeHint("Cntrl + LMB -> Sample Texture");
        }

        public override void MouseEventPointedTriangle()
        {
           
            if (EditorInputManager.GetMouseButton(0))
            {
                if (EditorInputManager.getControlKey())
                    cfg.curAtlasTexture = (int)mgmt.pointedTris.textureNo[cfg.curAtlasChanel];
                else
                {
                    mgmt.pointedTris.textureNo[cfg.curAtlasChanel] = cfg.curAtlasTexture;
                    mgmt._Mesh.dirty = true;
                }
            }
        }

        public override void MouseEventPointedLine() {
            if (EditorInputManager.GetMouseButtonDown(0))
            {

               foreach(var t in mgmt.pointedLine.getAllTriangles_USES_Tris_Listing()) 
                    t.textureNo.y = cfg.curAtlasTexture;
                mgmt._Mesh.dirty = true;

            }

        }

        public void SetAllTrianglesTextureTo(int no, int chanel) {

            foreach (trisDta t in mesh.triangles)
                t.textureNo[chanel] = no;

            mgmt._Mesh.dirty = true;
        }

        public void SetAllTrianglesTextureTo(int no, int chanel, int submesh)
        {

            foreach (trisDta t in mesh.triangles)
                if (t.submeshIndex == submesh)
                    t.textureNo[chanel] = no;

            mgmt._Mesh.dirty = true;
        }

        public override void KeysEventPointedTriangle()
        {


            int keyDown = Event.current.NumericKeyDown();
            
            if (keyDown!= -1) {
                mgmt.pointedTris.textureNo[cfg.curAtlasChanel] = keyDown;
                mgmt._Mesh.dirty = true;
                if (!Application.isPlaying) Event.current.Use();
            }
        
    }

    }

}